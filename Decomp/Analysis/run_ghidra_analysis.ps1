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
$logDir = Join-Path $PSScriptRoot 'logs'
New-Item -ItemType Directory -Force -Path $logDir | Out-Null

if ($AllClientBinaries) {
    $Targets = Get-ChildItem -LiteralPath $resolvedClientDir -File |
        Where-Object { $_.Extension -in @('.exe', '.dll') } |
        Select-Object -ExpandProperty Name
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
$effectiveProjectLayout = if ($ProjectLayout -eq 'Auto') {
    if ($AllClientBinaries -or $Targets.Count -gt 1) { 'Shared' } else { 'PerTarget' }
}
else {
    $ProjectLayout
}

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
    $logPath = Join-Path $logDir ("{0}{1}{2}.ghidra.log" -f $targetName, $projectLogSuffix, $logSuffix)
    $ghidraArgs = @(
        $ProjectDir,
        $projectName
    )

    Write-Host ("Using Ghidra project {0}" -f $projectName)

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
        '-postScript', 'ExportNexusForeverAnalysis.java', $OutputDir, $MaxDecompiledFunctions,
        $DecompileMode, $ghidraVersion, $binaryFingerprint, $labelFingerprint,
        $labelsApplied.ToString().ToLowerInvariant()
    )

    if ($ExtraPostScript) {
        $ghidraArgs += @('-postScript', $ExtraPostScript)
        $ghidraArgs += $ExtraPostScriptArgs
    }

    & $analyzeHeadless @ghidraArgs 2>&1 | Tee-Object -FilePath $logPath

    $projectLockFailure = Select-String -LiteralPath $logPath -SimpleMatch -Pattern 'Unable to lock project!' -Quiet

    if ($LASTEXITCODE -ne 0) {
        if ($projectLockFailure) {
            throw ("Ghidra project {0} is already in use. Different-target runs avoid this by using split per-target projects (-ProjectLayout PerTarget, or Auto after the per-target project exists). Same-target runs still need to wait for the current holder to finish. See {1}." -f $projectName, $logPath)
        }
        throw "Ghidra run failed for $target. See $logPath."
    }

    $scriptFailure = Select-String -LiteralPath $logPath -SimpleMatch -Pattern @(
        'SCRIPT ERROR',
        'The class could not be found',
        'Exception running script'
    ) -Quiet
    if ($scriptFailure) {
        throw "Ghidra post-script failed for $target. See $logPath."
    }
}

Write-Host "Ghidra run complete. Exports: $OutputDir"
