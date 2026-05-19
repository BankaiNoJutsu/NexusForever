[CmdletBinding()]
param(
    [string] $ClientDir = (Join-Path $PSScriptRoot '..\Client64'),
    [string] $OutputDir = (Join-Path $PSScriptRoot 'exports'),
    [string] $ProjectDir = (Join-Path $PSScriptRoot 'ghidra_projects'),
    [string] $ToolRoot = "$env:USERPROFILE\.codex\tools\nexusforever-decomp",
    [string] $LabelMap = (Join-Path $PSScriptRoot 'function_labels.csv'),
    [string[]] $Targets = @('WildStar64.exe', 'Houston64.exe', 'StsConnLib64.MT.dll'),
    [int] $MaxDecompiledFunctions = 200,
    [ValidateSet('Auto', 'Force', 'Skip')]
    [string] $DecompileMode = 'Auto',
    [ValidateSet('Auto', 'Shared', 'PerTarget')]
    [string] $ProjectLayout = 'PerTarget',
    [ValidateRange(1, 32)]
    [int] $MaxParallel = 3,
    [string] $RunId = '',
    [switch] $Analyze,
    [switch] $AllClientBinaries,
    [switch] $NoApplyLabels,
    [switch] $SkipCoverage,
    [switch] $NoLatestSummary,
    [string] $ExtraPostScript,
    [string[]] $ExtraPostScriptArgs = @()
)

$ErrorActionPreference = 'Stop'

function ConvertTo-PathToken {
    param(
        [string] $Value
    )

    $token = $Value -replace '[^A-Za-z0-9]+', '_'
    $token = $token.Trim('_')
    if ([string]::IsNullOrWhiteSpace($token)) {
        return 'Run'
    }

    return $token
}

function Resolve-OrCreateDirectory {
    param(
        [string] $Path
    )

    New-Item -ItemType Directory -Force -Path $Path | Out-Null
    return (Resolve-Path -LiteralPath $Path).Path
}

function Resolve-OptionalFilePath {
    param(
        [string] $Path
    )

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return ''
    }

    if (Test-Path -LiteralPath $Path -PathType Leaf) {
        return (Resolve-Path -LiteralPath $Path).Path
    }

    return $Path
}

if ($ProjectLayout -eq 'Shared' -and $MaxParallel -gt 1) {
    throw 'Shared project layout cannot be used safely with MaxParallel greater than 1. Use -ProjectLayout PerTarget, -ProjectLayout Auto, or -MaxParallel 1.'
}

$runnerPath = Join-Path $PSScriptRoot 'run_ghidra_analysis.ps1'
if (-not (Test-Path -LiteralPath $runnerPath -PathType Leaf)) {
    throw "Runner script not found: $runnerPath"
}

$resolvedRepoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')).Path
$resolvedClientDir = (Resolve-Path -LiteralPath $ClientDir).Path
$resolvedOutputDir = Resolve-OrCreateDirectory -Path $OutputDir
$resolvedProjectDir = Resolve-OrCreateDirectory -Path $ProjectDir
$resolvedLabelMap = Resolve-OptionalFilePath -Path $LabelMap
$baseLogDir = Resolve-OrCreateDirectory -Path (Join-Path $PSScriptRoot 'logs')

if ($AllClientBinaries) {
    $Targets = Get-ChildItem -LiteralPath $resolvedClientDir -File |
        Where-Object { $_.Extension -in @('.exe', '.dll') } |
        Select-Object -ExpandProperty Name
}

if ($Targets.Count -eq 0) {
    throw 'No decompile targets were supplied.'
}

if ([string]::IsNullOrWhiteSpace($RunId)) {
    $RunId = Get-Date -Format 'yyyyMMdd-HHmmss'
}

$runToken = ConvertTo-PathToken -Value $RunId
$runDir = Resolve-OrCreateDirectory -Path (Join-Path (Join-Path $baseLogDir 'runs') $runToken)
$batchSummaryPath = Join-Path $runDir 'batch_summary.json'
$latestSummaryPath = Join-Path $baseLogDir 'LATEST_RUN_SUMMARY.json'

$pendingTargets = [System.Collections.Generic.Queue[string]]::new()
foreach ($target in $Targets) {
    $pendingTargets.Enqueue($target)
}

$activeJobs = New-Object 'System.Collections.Generic.List[System.Management.Automation.Job]'
$jobInfoById = @{}
$jobResults = New-Object 'System.Collections.Generic.List[object]'

function Start-DecompileTargetJob {
    param(
        [string] $Target
    )

    $targetToken = ConvertTo-PathToken -Value ([IO.Path]::GetFileNameWithoutExtension($Target))
    $summaryPath = Join-Path $runDir ('{0}.run_summary.json' -f $targetToken)
    $jobOutputPath = Join-Path $runDir ('{0}.job_output.log' -f $targetToken)

    $runnerParameters = @{
        ClientDir               = $resolvedClientDir
        OutputDir               = $resolvedOutputDir
        ProjectDir              = $resolvedProjectDir
        ToolRoot                = $ToolRoot
        LabelMap                = $resolvedLabelMap
        Targets                 = @($Target)
        MaxDecompiledFunctions  = $MaxDecompiledFunctions
        DecompileMode           = $DecompileMode
        ProjectLayout           = $ProjectLayout
        RunId                   = $RunId
        SummaryPath             = $summaryPath
        SkipCoverage            = $true
    }

    if (-not $Analyze) {
        $runnerParameters.ExportOnly = $true
    }

    if ($NoApplyLabels) {
        $runnerParameters.NoApplyLabels = $true
    }

    if (-not [string]::IsNullOrWhiteSpace($ExtraPostScript)) {
        $runnerParameters.ExtraPostScript = $ExtraPostScript
        $runnerParameters.ExtraPostScriptArgs = $ExtraPostScriptArgs
    }

    $job = Start-Job -Name ('decompile-{0}' -f $targetToken) -ScriptBlock {
        param(
            [string] $RunnerPath,
            [hashtable] $RunnerParameters
        )

        & $RunnerPath @RunnerParameters
    } -ArgumentList $runnerPath, $runnerParameters

    $activeJobs.Add($job)
    $jobInfoById[$job.Id] = [pscustomobject]@{
        target = $Target
        targetToken = $targetToken
        summaryPath = $summaryPath
        jobOutputPath = $jobOutputPath
    }

    Write-Host ("Started {0} as job {1}; summary: {2}" -f $Target, $job.Id, $summaryPath)
}

while ($pendingTargets.Count -gt 0 -or $activeJobs.Count -gt 0) {
    while ($pendingTargets.Count -gt 0 -and $activeJobs.Count -lt $MaxParallel) {
        Start-DecompileTargetJob -Target $pendingTargets.Dequeue()
    }

    if ($activeJobs.Count -eq 0) {
        continue
    }

    Wait-Job -Job $activeJobs.ToArray() -Any | Out-Null
    $finishedJobs = @($activeJobs.ToArray() | Where-Object { $_.State -in @('Completed', 'Failed', 'Stopped') })
    foreach ($job in $finishedJobs) {
        $info = $jobInfoById[$job.Id]
        $jobOutput = Receive-Job -Job $job -Keep 2>&1
        $jobOutput | Out-String | Out-File -LiteralPath $info.jobOutputPath -Encoding utf8

        $jobResults.Add([pscustomobject]@{
            target = $info.target
            jobId = $job.Id
            state = [string] $job.State
            summaryPath = $info.summaryPath
            jobOutputPath = $info.jobOutputPath
        })

        Write-Host ("Finished {0} as job {1}: {2}" -f $info.target, $job.Id, $job.State)
        Remove-Job -Job $job -Force
        [void] $activeJobs.Remove($job)
    }
}

$workerSummaries = New-Object 'System.Collections.Generic.List[object]'
foreach ($result in $jobResults) {
    if (Test-Path -LiteralPath $result.summaryPath -PathType Leaf) {
        $workerSummaries.Add((Get-Content -LiteralPath $result.summaryPath -Raw | ConvertFrom-Json))
    }
}

$mergedTargets = @($workerSummaries | ForEach-Object { $_.targets } | ForEach-Object { $_ })
$firstSummary = @($workerSummaries | Select-Object -First 1)[0]

$batchSummary = [ordered]@{
    timestampUtc = (Get-Date).ToUniversalTime().ToString('o')
    runId = $RunId
    batch = $true
    clientDir = $resolvedClientDir
    outputDir = $resolvedOutputDir
    projectDir = $resolvedProjectDir
    logDir = $runDir
    summaryPath = $batchSummaryPath
    decompileMode = $DecompileMode
    projectLayout = $ProjectLayout
    exportOnly = -not [bool]$Analyze
    skipCoverage = [bool]$SkipCoverage
    noApplyLabels = [bool]$NoApplyLabels
    labelsApplied = if ($null -eq $firstSummary) { -not [bool]$NoApplyLabels } else { [bool]$firstSummary.labelsApplied }
    labelMap = if ($null -eq $firstSummary) { $resolvedLabelMap } else { [string]$firstSummary.labelMap }
    labelFingerprint = if ($null -eq $firstSummary) { '' } else { [string]$firstSummary.labelFingerprint }
    requestedMaxDecompiledFunctions = $MaxDecompiledFunctions
    maxDecompiledFunctions = $MaxDecompiledFunctions
    extraPostScript = if ($ExtraPostScript) { $ExtraPostScript } else { '' }
    extraPostScriptArgs = $ExtraPostScriptArgs
    targetCount = $Targets.Count
    targets = $mergedTargets
    workerSummaries = @($jobResults | Select-Object target, summaryPath, jobOutputPath, state)
}

$batchSummary | ConvertTo-Json -Depth 8 | Out-File -LiteralPath $batchSummaryPath -Encoding utf8
Write-Host "Batch summary written to: $batchSummaryPath"

$failedJobs = @($jobResults | Where-Object { $_.state -ne 'Completed' })
$failedTargets = @($mergedTargets | Where-Object { $_.status -ne 'success' })
$hasFailures = $failedJobs.Count -gt 0 -or $failedTargets.Count -gt 0 -or $workerSummaries.Count -ne $Targets.Count

if (-not $hasFailures -and -not $SkipCoverage) {
    $coverageScript = Join-Path $PSScriptRoot 'Get-DecompCoverageSnapshot.ps1'
    if (Test-Path -LiteralPath $coverageScript -PathType Leaf) {
        $coverageSummary = & $coverageScript -RepoRoot $resolvedRepoRoot -OutputDir $resolvedOutputDir -LogDir $baseLogDir -RunSummaryPath $batchSummaryPath
        if ($null -ne $coverageSummary) {
            $batchSummary.coverage = [ordered]@{
                summaryPath = $coverageSummary.summaryPath
                markdownPath = $coverageSummary.markdownPath
                exportInventoryPath = $coverageSummary.exportInventoryPath
                opcodeInventoryPath = $coverageSummary.opcodeInventoryPath
                exportTargetCount = $coverageSummary.exportTargetCount
                totalOpcodes = $coverageSummary.totalOpcodes
            }

            $batchSummary | ConvertTo-Json -Depth 8 | Out-File -LiteralPath $batchSummaryPath -Encoding utf8
            Write-Host ("Coverage summary written to: {0}" -f $coverageSummary.summaryPath)
        }
    }
}
elseif ($hasFailures -and -not $SkipCoverage) {
    Write-Warning 'Skipping coverage snapshot because one or more decompile jobs failed.'
}

if (-not $NoLatestSummary) {
    $batchSummary | ConvertTo-Json -Depth 8 | Out-File -LiteralPath $latestSummaryPath -Encoding utf8
    Write-Host "Latest run summary written to: $latestSummaryPath"
}

[pscustomobject]@{
    runId = $RunId
    summaryPath = $batchSummaryPath
    latestSummaryPath = if ($NoLatestSummary) { '' } else { $latestSummaryPath }
    targetCount = $Targets.Count
    completedTargets = @($mergedTargets | Where-Object { $_.status -eq 'success' }).Count
    failedJobs = $failedJobs.Count
    failedTargets = $failedTargets.Count
}

if ($hasFailures) {
    throw 'One or more decompile jobs failed. Inspect the batch summary and per-target job output logs.'
}
