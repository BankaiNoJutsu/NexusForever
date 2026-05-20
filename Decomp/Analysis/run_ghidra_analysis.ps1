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
    [string] $ProjectLayout = 'Auto',
    [ValidateRange(1, 3600)]
    [int] $ProjectLockRetryDelaySeconds = 15,
    [ValidateRange(0, 10080)]
    [int] $ProjectLockTimeoutMinutes = 0,
    [string] $RunId = '',
    [string] $SummaryPath = '',
    [switch] $SkipCoverage,
    [switch] $AllClientBinaries,
    [switch] $NoApplyLabels,
    [switch] $ExportOnly,
    [string] $ExtraPostScript,
    [string[]] $ExtraPostScriptArgs = @()
)

$ErrorActionPreference = 'Stop'

function Get-ArtifactFingerprint {
    param(
        [string] $Path
    )

    if ([string]::IsNullOrWhiteSpace($Path) -or -not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        return ''
    }

    return ('sha256:{0}' -f ((Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()))
}

function Read-KeyValuePropertiesFile {
    param(
        [string] $Path
    )

    if ([string]::IsNullOrWhiteSpace($Path) -or -not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        return $null
    }

    $properties = @{}
    foreach ($line in Get-Content -LiteralPath $Path) {
        if ([string]::IsNullOrWhiteSpace($line)) {
            continue
        }

        $trimmed = $line.Trim()
        if ($trimmed.StartsWith('#') -or $trimmed.StartsWith('!')) {
            continue
        }

        $separatorIndex = $trimmed.IndexOf('=')
        if ($separatorIndex -lt 0) {
            $separatorIndex = $trimmed.IndexOf(':')
        }

        if ($separatorIndex -lt 0) {
            continue
        }

        $key = $trimmed.Substring(0, $separatorIndex).Trim()
        if ([string]::IsNullOrWhiteSpace($key)) {
            continue
        }

        $value = $trimmed.Substring($separatorIndex + 1).Trim()
        $value = $value.Replace('\:', ':').Replace('\=', '=').Replace('\ ', ' ')
        $value = $value.Replace('\t', "`t").Replace('\n', "`n").Replace('\r', "`r").Replace('\f', [string] [char] 12)
        $value = $value.Replace('\\', [string] [char] 92)
        $properties[$key] = $value
    }

    return $properties
}

function ConvertTo-NullableInt {
    param(
        [string] $Value
    )

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return $null
    }

    $parsed = 0
    if ([int]::TryParse($Value, [ref] $parsed)) {
        return $parsed
    }

    return $null
}

function ConvertTo-NullableBool {
    param(
        [string] $Value
    )

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return $null
    }

    $parsed = $false
    if ([bool]::TryParse($Value, [ref] $parsed)) {
        return $parsed
    }

    return $null
}

function Get-DecompileManifestSummary {
    param(
        [string] $ManifestPath,
        [string] $ExpectedBinaryFingerprint,
        [string] $ExpectedLabelFingerprint
    )

    $properties = Read-KeyValuePropertiesFile -Path $ManifestPath
    if ($null -eq $properties) {
        return $null
    }

    $manifestBinaryFingerprint = $properties['binary.fingerprint']
    $manifestLabelFingerprint = $properties['labels.fingerprint']
    return [ordered]@{
        fingerprint                = $properties['fingerprint']
        contextFingerprint         = $properties['context.fingerprint']
        scriptVersion              = $properties['script.version']
        labelsApplied              = ConvertTo-NullableBool -Value $properties['labels.applied']
        labelFingerprint           = $manifestLabelFingerprint
        labelFingerprintMatchesRun = if ([string]::IsNullOrWhiteSpace($ExpectedLabelFingerprint) -or [string]::IsNullOrWhiteSpace($manifestLabelFingerprint)) { $null } else { $manifestLabelFingerprint -eq $ExpectedLabelFingerprint }
        binaryFingerprint          = $manifestBinaryFingerprint
        binaryFingerprintMatchesRun = if ([string]::IsNullOrWhiteSpace($ExpectedBinaryFingerprint) -or [string]::IsNullOrWhiteSpace($manifestBinaryFingerprint)) { $null } else { $manifestBinaryFingerprint -eq $ExpectedBinaryFingerprint }
        maxDecompiledFunctions     = ConvertTo-NullableInt -Value $properties['max.decompiledFunctions']
        selectedCount              = ConvertTo-NullableInt -Value $properties['selected.count']
        outputExists               = ConvertTo-NullableBool -Value $properties['output.exists']
        reusedFragments            = ConvertTo-NullableInt -Value $properties['cache.reusedFragments']
        decompiledFragments        = ConvertTo-NullableInt -Value $properties['cache.decompiledFragments']
    }
}

function Resolve-EffectiveMaxDecompiledFunctions {
    param(
        [string] $ManifestPath,
        [int] $RequestedMaxDecompiledFunctions,
        [bool] $MaxExplicitlySet,
        [bool] $ExportOnly,
        [string] $ExtraPostScript
    )

    if ($MaxExplicitlySet -or -not $ExportOnly -or [string]::IsNullOrWhiteSpace($ExtraPostScript)) {
        return $RequestedMaxDecompiledFunctions
    }

    $manifest = Get-DecompileManifestSummary -ManifestPath $ManifestPath -ExpectedBinaryFingerprint '' -ExpectedLabelFingerprint ''
    if ($null -eq $manifest -or $null -eq $manifest.maxDecompiledFunctions -or $manifest.maxDecompiledFunctions -le 0) {
        return $RequestedMaxDecompiledFunctions
    }

    return $manifest.maxDecompiledFunctions
}

function Finalize-TargetSummary {
    param(
        [System.Collections.IDictionary] $TargetSummary,
        [string] $ExpectedBinaryFingerprint,
        [string] $ExpectedLabelFingerprint
    )

    $TargetSummary.exported = Test-Path -LiteralPath $TargetSummary.exportDir
    $TargetSummary.manifestExists = Test-Path -LiteralPath $TargetSummary.manifestPath
    $TargetSummary.manifest = if ($TargetSummary.manifestExists) {
        Get-DecompileManifestSummary -ManifestPath $TargetSummary.manifestPath -ExpectedBinaryFingerprint $ExpectedBinaryFingerprint -ExpectedLabelFingerprint $ExpectedLabelFingerprint
    }
    else {
        $null
    }
}

function ConvertTo-ProjectToken {
    param(
        [string] $Value
    )

    $token = $Value -replace '[^A-Za-z0-9]+', '_'
    $token = $token.Trim('_')
    if ([string]::IsNullOrWhiteSpace($token)) {
        return 'Target'
    }

    return $token
}

function Test-GhidraProjectExists {
    param(
        [string] $Directory,
        [string] $ProjectName
    )

    return (Test-Path -LiteralPath (Join-Path $Directory ($ProjectName + '.gpr')) -PathType Leaf) -or
        (Test-Path -LiteralPath (Join-Path $Directory ($ProjectName + '.rep')))
}

function Get-GhidraProjectName {
    param(
        [string] $BaseProjectName,
        [string] $Target,
        [ValidateSet('Shared', 'PerTarget')]
        [string] $Layout
    )

    if ($Layout -eq 'Shared') {
        return $BaseProjectName
    }

    $targetToken = ConvertTo-ProjectToken -Value ([IO.Path]::GetFileNameWithoutExtension($Target))
    return ('{0}_{1}' -f $BaseProjectName, $targetToken)
}

function Test-ProjectLockWaitExpired {
    param(
        [datetime] $StartedUtc,
        [int] $TimeoutMinutes
    )

    if ($TimeoutMinutes -le 0) {
        return $false
    }

    return [datetime]::UtcNow -ge $StartedUtc.AddMinutes($TimeoutMinutes)
}

function Enter-GhidraProjectGate {
    param(
        [string] $Directory,
        [string] $ProjectName,
        [int] $RetryDelaySeconds,
        [int] $TimeoutMinutes
    )

    $lockDir = Join-Path $Directory '.project_locks'
    New-Item -ItemType Directory -Force -Path $lockDir | Out-Null
    $lockPath = Join-Path $lockDir ('{0}.lock' -f (ConvertTo-ProjectToken -Value $ProjectName))
    $startedUtc = [datetime]::UtcNow
    $waited = $false

    while ($true) {
        try {
            $stream = [System.IO.File]::Open($lockPath, [System.IO.FileMode]::OpenOrCreate, [System.IO.FileAccess]::ReadWrite, [System.IO.FileShare]::None)
            $stream.SetLength(0)

            $content = @(
                'owner=run_ghidra_analysis.ps1'
                ('project={0}' -f $ProjectName)
                ('pid={0}' -f $PID)
                ('acquiredUtc={0:o}' -f [datetime]::UtcNow)
            ) -join [Environment]::NewLine
            $bytes = [System.Text.Encoding]::UTF8.GetBytes($content)
            $stream.Write($bytes, 0, $bytes.Length)
            $stream.Flush($true)

            if ($waited) {
                Write-Host ("Acquired Ghidra project gate for {0}" -f $ProjectName)
            }

            return [pscustomobject]@{
                projectName = $ProjectName
                lockPath    = $lockPath
                stream      = $stream
                waited      = $waited
            }
        }
        catch [System.IO.IOException] {
            if (Test-ProjectLockWaitExpired -StartedUtc $startedUtc -TimeoutMinutes $TimeoutMinutes) {
                throw ("Timed out waiting for Ghidra project gate {0}. Lock file: {1}" -f $ProjectName, $lockPath)
            }

            if (-not $waited) {
                Write-Host ("Waiting for another decompile session to release Ghidra project {0}. Lock file: {1}" -f $ProjectName, $lockPath)
                $waited = $true
            }
            Start-Sleep -Seconds $RetryDelaySeconds
        }
        catch [System.UnauthorizedAccessException] {
            if (Test-ProjectLockWaitExpired -StartedUtc $startedUtc -TimeoutMinutes $TimeoutMinutes) {
                throw ("Timed out waiting for Ghidra project gate {0}. Lock file: {1}" -f $ProjectName, $lockPath)
            }

            if (-not $waited) {
                Write-Host ("Waiting for another decompile session to release Ghidra project {0}. Lock file: {1}" -f $ProjectName, $lockPath)
                $waited = $true
            }
            Start-Sleep -Seconds $RetryDelaySeconds
        }
    }
}

function Exit-GhidraProjectGate {
    param(
        [object] $Gate
    )

    if ($null -eq $Gate -or $null -eq $Gate.stream) {
        return
    }

    $Gate.stream.Dispose()
}

function Invoke-GhidraHeadlessWithProjectRetry {
    param(
        [string] $AnalyzeHeadless,
        [object[]] $Arguments,
        [string] $LogPath,
        [string] $ProjectName,
        [int] $RetryDelaySeconds,
        [int] $TimeoutMinutes
    )

    if (Test-Path -LiteralPath $LogPath -PathType Leaf) {
        Remove-Item -LiteralPath $LogPath -Force
    }

    $startedUtc = [datetime]::UtcNow
    $attempt = 0

    while ($true) {
        $attempt++
        if ($attempt -gt 1) {
            Add-Content -LiteralPath $LogPath -Encoding utf8 -Value ("`n--- Retry {0} after Ghidra project lock at {1:o} ---" -f $attempt, [datetime]::UtcNow)
        }

        $lockSignals = New-Object 'System.Collections.Generic.List[string]'
        & $AnalyzeHeadless @Arguments 2>&1 | ForEach-Object {
            $line = [string] $_
            if ($line.Contains('Unable to lock project!')) {
                $lockSignals.Add($line)
            }

            $_
        } | Tee-Object -FilePath $LogPath -Append

        $exitCode = $LASTEXITCODE
        $projectLockFailure = $lockSignals.Count -gt 0
        if (-not $projectLockFailure) {
            return [pscustomobject]@{
                exitCode           = $exitCode
                projectLockFailure = $false
                attempts           = $attempt
            }
        }

        if (Test-ProjectLockWaitExpired -StartedUtc $startedUtc -TimeoutMinutes $TimeoutMinutes) {
            return [pscustomobject]@{
                exitCode           = $exitCode
                projectLockFailure = $true
                attempts           = $attempt
            }
        }

        Write-Warning ("Ghidra project {0} is locked by another process; retrying in {1} seconds. See {2}." -f $ProjectName, $RetryDelaySeconds, $LogPath)
        Start-Sleep -Seconds $RetryDelaySeconds
    }
}

$setupScript = Join-Path $PSScriptRoot 'setup_decomp_tools.ps1'
$ghidraDir = Join-Path $ToolRoot 'ghidra_12.0.4_PUBLIC'
$javaHome = Join-Path $ToolRoot 'jdk-21.0.11+10'
$analyzeHeadless = Join-Path $ghidraDir 'support\analyzeHeadless.bat'
$ghidraVersion = [IO.Path]::GetFileName($ghidraDir)

if (-not (Test-Path -LiteralPath $analyzeHeadless) -or -not (Test-Path -LiteralPath (Join-Path $javaHome 'bin\java.exe'))) {
    & $setupScript -ToolRoot $ToolRoot | Format-List
}

$resolvedClientDir = (Resolve-Path -LiteralPath $ClientDir).Path
New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null
New-Item -ItemType Directory -Force -Path $ProjectDir | Out-Null
$resolvedOutputDir = (Resolve-Path -LiteralPath $OutputDir).Path
$resolvedProjectDir = (Resolve-Path -LiteralPath $ProjectDir).Path
$resolvedRepoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')).Path
$logDir = Join-Path $PSScriptRoot 'logs'
New-Item -ItemType Directory -Force -Path $logDir | Out-Null

if ($AllClientBinaries) {
    $Targets = Get-ChildItem -LiteralPath $resolvedClientDir -File |
        Where-Object { $_.Extension -in @('.exe', '.dll') } |
        Select-Object -ExpandProperty Name
}

$runToken = if ([string]::IsNullOrWhiteSpace($RunId)) { '' } else { ConvertTo-ProjectToken -Value $RunId }
$runLogDir = if ([string]::IsNullOrWhiteSpace($runToken)) {
    $logDir
}
else {
    Join-Path (Join-Path $logDir 'runs') $runToken
}
New-Item -ItemType Directory -Force -Path $runLogDir | Out-Null
$resolvedRunLogDir = (Resolve-Path -LiteralPath $runLogDir).Path

if ([string]::IsNullOrWhiteSpace($SummaryPath)) {
    $summaryFileName = if ([string]::IsNullOrWhiteSpace($runToken)) {
        'LATEST_RUN_SUMMARY.json'
    }
    elseif ($Targets.Count -eq 1) {
        ('{0}.run_summary.json' -f (ConvertTo-ProjectToken -Value ([IO.Path]::GetFileNameWithoutExtension($Targets[0]))))
    }
    else {
        'run_summary.json'
    }

    $SummaryPath = Join-Path $resolvedRunLogDir $summaryFileName
}

$summaryPathIsRooted = [IO.Path]::IsPathRooted($SummaryPath)
$effectiveSummaryPath = if ($summaryPathIsRooted) { $SummaryPath } else { Join-Path (Get-Location).Path $SummaryPath }
$summaryParent = Split-Path -Parent $effectiveSummaryPath
if (-not [string]::IsNullOrWhiteSpace($summaryParent)) {
    New-Item -ItemType Directory -Force -Path $summaryParent | Out-Null
}
$resolvedSummaryPath = if (Test-Path -LiteralPath $effectiveSummaryPath -PathType Leaf) {
    (Resolve-Path -LiteralPath $effectiveSummaryPath).Path
}
else {
    Join-Path (Resolve-Path -LiteralPath $summaryParent).Path (Split-Path -Leaf $effectiveSummaryPath)
}

$env:JAVA_HOME = (Resolve-Path -LiteralPath $javaHome).Path
$env:PATH = "$env:JAVA_HOME\bin;$env:PATH"

$scriptPath = Join-Path $PSScriptRoot 'scripts'
$sharedProjectName = 'NexusForeverClient64'
$resolvedLabelMap = $null
if (-not $NoApplyLabels -and (Test-Path -LiteralPath $LabelMap)) {
    $resolvedLabelMap = (Resolve-Path -LiteralPath $LabelMap).Path
}

$labelsApplied = $null -ne $resolvedLabelMap
$labelFingerprint = if ($labelsApplied) { Get-ArtifactFingerprint -Path $resolvedLabelMap } else { '' }
$maxDecompiledFunctionsExplicitlySet = $PSBoundParameters.ContainsKey('MaxDecompiledFunctions')
$effectiveProjectLayout = if ($ProjectLayout -eq 'Auto') {
    if ($AllClientBinaries -or $Targets.Count -gt 1) { 'Shared' } else { 'PerTarget' }
}
else {
    $ProjectLayout
}

$runSummaryTargets = New-Object 'System.Collections.Generic.List[object]'

try {
    foreach ($target in $Targets) {
        $targetName = [IO.Path]::GetFileNameWithoutExtension($target)
        $binaryPath = Join-Path $resolvedClientDir $target
        $binaryFingerprint = Get-ArtifactFingerprint -Path $binaryPath
        $projectName = Get-GhidraProjectName -BaseProjectName $sharedProjectName -Target $target -Layout $effectiveProjectLayout
        if ($effectiveProjectLayout -eq 'PerTarget' -and $ExportOnly -and -not (Test-GhidraProjectExists -Directory $ProjectDir -ProjectName $projectName)) {
            if ($ProjectLayout -eq 'Auto' -and (Test-GhidraProjectExists -Directory $ProjectDir -ProjectName $sharedProjectName)) {
                Write-Warning ("Per-target project {0} not found for {1}. Falling back to legacy shared project {2} for export-only. Run once without -ExportOnly to create the split project." -f $projectName, $target, $sharedProjectName)
                $projectName = $sharedProjectName
            }
            else {
                throw ("Export-only requested for {0}, but per-target Ghidra project {1} does not exist under {2}. Run once without -ExportOnly to create it, or use -ProjectLayout Shared." -f $target, $projectName, $ProjectDir)
            }
        }

        $projectLogSuffix = if ($projectName -ne $sharedProjectName) { ".{0}" -f $projectName } else { '' }
        $logSuffix = if ($ExtraPostScript) { ".{0}" -f ([IO.Path]::GetFileNameWithoutExtension($ExtraPostScript)) } else { '' }
        $logPath = Join-Path $resolvedRunLogDir ("{0}{1}{2}.ghidra.log" -f $targetName, $projectLogSuffix, $logSuffix)
        $exportDir = Join-Path $resolvedOutputDir $target
        $manifestPath = Join-Path $exportDir 'selected_decompiled.manifest'
        $effectiveMaxDecompiledFunctions = Resolve-EffectiveMaxDecompiledFunctions `
            -ManifestPath $manifestPath `
            -RequestedMaxDecompiledFunctions $MaxDecompiledFunctions `
            -MaxExplicitlySet $maxDecompiledFunctionsExplicitlySet `
            -ExportOnly ([bool] $ExportOnly) `
            -ExtraPostScript $ExtraPostScript
        $targetSummary = [ordered]@{
            target = $target
            binaryPath = $binaryPath
            binaryFingerprint = $binaryFingerprint
            projectName = $projectName
            exportDir = $exportDir
            manifestPath = $manifestPath
            logPath = $logPath
            requestedMaxDecompiledFunctions = $MaxDecompiledFunctions
            effectiveMaxDecompiledFunctions = $effectiveMaxDecompiledFunctions
            status = 'pending'
            exitCode = $null
            projectLockFailure = $false
            projectLockAttempts = 0
            projectGateLockPath = ''
            projectGateWaited = $false
            scriptFailure = $false
            exported = $false
            manifestExists = $false
            manifest = $null
        }

        $ghidraArgs = @(
            $ProjectDir,
            $projectName
        )

        Write-Host ("Using Ghidra project {0}" -f $projectName)
        if ($effectiveMaxDecompiledFunctions -ne $MaxDecompiledFunctions) {
            Write-Host ("Preserving manifest max decompile depth {0} for {1} because -ExtraPostScript was used without an explicit -MaxDecompiledFunctions override." -f $effectiveMaxDecompiledFunctions, $target)
        }

        if ($ExportOnly) {
            Write-Host "Exporting existing Ghidra program $target"
            $ghidraArgs += @(
                '-process', $target,
                '-noanalysis'
            )
        }
        else {
            if (-not (Test-Path -LiteralPath $binaryPath)) {
                throw "Target binary not found: $binaryPath"
            }

            Write-Host "Analyzing $binaryPath"
            $ghidraArgs += @(
                '-import', $binaryPath,
                '-overwrite',
                '-analysisTimeoutPerFile', '3600'
            )
        }

        $ghidraArgs += @('-scriptPath', $scriptPath)
        if ($resolvedLabelMap) {
            $ghidraArgs += @('-preScript', 'ApplyNexusForeverLabels.java', $resolvedLabelMap)
        }

        $ghidraArgs += @(
            '-postScript', 'ExportNexusForeverAnalysis.java', $OutputDir, $effectiveMaxDecompiledFunctions,
            $DecompileMode, $ghidraVersion, $binaryFingerprint, $labelFingerprint,
            $labelsApplied.ToString().ToLowerInvariant()
        )

        if ($ExtraPostScript) {
            $ghidraArgs += @('-postScript', $ExtraPostScript)
            $ghidraArgs += $ExtraPostScriptArgs
        }

        $projectGate = $null
        try {
            $projectGate = Enter-GhidraProjectGate `
                -Directory $resolvedProjectDir `
                -ProjectName $projectName `
                -RetryDelaySeconds $ProjectLockRetryDelaySeconds `
                -TimeoutMinutes $ProjectLockTimeoutMinutes
            $targetSummary.projectGateLockPath = $projectGate.lockPath
            $targetSummary.projectGateWaited = [bool] $projectGate.waited

            $ghidraResult = Invoke-GhidraHeadlessWithProjectRetry `
                -AnalyzeHeadless $analyzeHeadless `
                -Arguments $ghidraArgs `
                -LogPath $logPath `
                -ProjectName $projectName `
                -RetryDelaySeconds $ProjectLockRetryDelaySeconds `
                -TimeoutMinutes $ProjectLockTimeoutMinutes
        }
        finally {
            Exit-GhidraProjectGate -Gate $projectGate
        }

        $targetSummary.exitCode = $ghidraResult.exitCode
        $targetSummary.projectLockFailure = [bool]$ghidraResult.projectLockFailure
        $targetSummary.projectLockAttempts = $ghidraResult.attempts

        if ($ghidraResult.exitCode -ne 0) {
            $targetSummary.status = 'failed'
            Finalize-TargetSummary -TargetSummary $targetSummary -ExpectedBinaryFingerprint $binaryFingerprint -ExpectedLabelFingerprint $labelFingerprint
            $runSummaryTargets.Add([pscustomobject]$targetSummary)

            if ($targetSummary.projectLockFailure) {
                throw ("Ghidra project {0} stayed locked after {1} attempt(s). Updated decompile runners wait for script-owned project gates automatically; close other Ghidra sessions or increase -ProjectLockTimeoutMinutes. See {2}." -f $projectName, $targetSummary.projectLockAttempts, $logPath)
            }
            throw "Ghidra run failed for $target. See $logPath."
        }

        $scriptFailure = Select-String -LiteralPath $logPath -SimpleMatch -Pattern @(
            'SCRIPT ERROR',
            'The class could not be found',
            'Exception running script'
        ) -Quiet
        $targetSummary.scriptFailure = [bool]$scriptFailure
        if ($scriptFailure) {
            $targetSummary.status = 'failed'
            Finalize-TargetSummary -TargetSummary $targetSummary -ExpectedBinaryFingerprint $binaryFingerprint -ExpectedLabelFingerprint $labelFingerprint
            $runSummaryTargets.Add([pscustomobject]$targetSummary)
            throw "Ghidra post-script failed for $target. See $logPath."
        }

        $targetSummary.status = 'success'
        Finalize-TargetSummary -TargetSummary $targetSummary -ExpectedBinaryFingerprint $binaryFingerprint -ExpectedLabelFingerprint $labelFingerprint
        $runSummaryTargets.Add([pscustomobject]$targetSummary)
    }
}
finally {
    $runSummary = [ordered]@{
        timestampUtc = (Get-Date).ToUniversalTime().ToString('o')
        runId = $RunId
        clientDir = $resolvedClientDir
        outputDir = $resolvedOutputDir
        projectDir = $resolvedProjectDir
        logDir = $resolvedRunLogDir
        summaryPath = $resolvedSummaryPath
        decompileMode = $DecompileMode
        projectLayout = $effectiveProjectLayout
        projectLockRetryDelaySeconds = $ProjectLockRetryDelaySeconds
        projectLockTimeoutMinutes = $ProjectLockTimeoutMinutes
        exportOnly = [bool]$ExportOnly
        skipCoverage = [bool]$SkipCoverage
        noApplyLabels = [bool]$NoApplyLabels
        labelsApplied = [bool]$labelsApplied
        labelMap = if ($resolvedLabelMap) { $resolvedLabelMap } else { '' }
        labelFingerprint = $labelFingerprint
        requestedMaxDecompiledFunctions = $MaxDecompiledFunctions
        maxDecompiledFunctions = $MaxDecompiledFunctions
        extraPostScript = if ($ExtraPostScript) { $ExtraPostScript } else { '' }
        extraPostScriptArgs = $ExtraPostScriptArgs
        targetCount = $Targets.Count
        targets = $runSummaryTargets
    }

    $runSummary | ConvertTo-Json -Depth 6 | Out-File -LiteralPath $resolvedSummaryPath -Encoding utf8
    Write-Host "Run summary written to: $resolvedSummaryPath"

    $coverageScript = Join-Path $PSScriptRoot 'Get-DecompCoverageSnapshot.ps1'
    if ($SkipCoverage) {
        Write-Host "Coverage snapshot skipped."
    }
    elseif (Test-Path -LiteralPath $coverageScript -PathType Leaf) {
        try {
            $coverageSummary = & $coverageScript -RepoRoot $resolvedRepoRoot -OutputDir $resolvedOutputDir -LogDir $resolvedRunLogDir -RunSummaryPath $resolvedSummaryPath
            if ($null -ne $coverageSummary) {
                $runSummary.coverage = [ordered]@{
                    summaryPath = $coverageSummary.summaryPath
                    markdownPath = $coverageSummary.markdownPath
                    exportInventoryPath = $coverageSummary.exportInventoryPath
                    opcodeInventoryPath = $coverageSummary.opcodeInventoryPath
                    exportTargetCount = $coverageSummary.exportTargetCount
                    totalOpcodes = $coverageSummary.totalOpcodes
                }

                $runSummary | ConvertTo-Json -Depth 8 | Out-File -LiteralPath $resolvedSummaryPath -Encoding utf8
                Write-Host ("Coverage summary written to: {0}" -f $coverageSummary.summaryPath)
            }
        }
        catch {
            Write-Warning ("Coverage snapshot generation failed: {0}" -f $_.Exception.Message)
        }
    }
}

Write-Host "Ghidra run complete. Exports: $OutputDir"
