[CmdletBinding()]
param(
    [string] $ClientDir = (Join-Path $PSScriptRoot '..\Client64'),
    [string] $OutputDir = (Join-Path $PSScriptRoot 'exports'),
    [string] $ProjectDir = (Join-Path $PSScriptRoot 'ghidra_projects'),
    [string] $ToolRoot = "$env:USERPROFILE\.codex\tools\nexusforever-decomp",
    [string] $LabelMap = (Join-Path $PSScriptRoot 'function_labels.csv'),
    [string[]] $Targets = @('WildStar64.exe', 'Houston64.exe', 'StsConnLib64.MT.dll'),
    [int] $MaxDecompiledFunctions = 200,
    [switch] $AllClientBinaries,
    [switch] $NoApplyLabels,
    [switch] $ExportOnly,
    [string] $ExtraPostScript,
    [string[]] $ExtraPostScriptArgs = @()
)

$ErrorActionPreference = 'Stop'

$setupScript = Join-Path $PSScriptRoot 'setup_decomp_tools.ps1'
$ghidraDir = Join-Path $ToolRoot 'ghidra_12.0.4_PUBLIC'
$javaHome = Join-Path $ToolRoot 'jdk-21.0.11+10'
$analyzeHeadless = Join-Path $ghidraDir 'support\analyzeHeadless.bat'

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
$projectName = 'NexusForeverClient64'
$resolvedLabelMap = $null
if (-not $NoApplyLabels -and (Test-Path -LiteralPath $LabelMap)) {
    $resolvedLabelMap = (Resolve-Path -LiteralPath $LabelMap).Path
}

foreach ($target in $Targets) {
    $targetName = [IO.Path]::GetFileNameWithoutExtension($target)
    $logSuffix = if ($ExtraPostScript) { ".{0}" -f ([IO.Path]::GetFileNameWithoutExtension($ExtraPostScript)) } else { '' }
    $logPath = Join-Path $logDir ("{0}{1}.ghidra.log" -f $targetName, $logSuffix)
    $ghidraArgs = @(
        $ProjectDir,
        $projectName
    )

    if ($ExportOnly) {
        Write-Host "Exporting existing Ghidra program $target"
        $ghidraArgs += @(
            '-process', $target,
            '-noanalysis'
        )
    }
    else {
        $binaryPath = Join-Path $resolvedClientDir $target
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
        '-postScript', 'ExportNexusForeverAnalysis.java', $OutputDir, $MaxDecompiledFunctions
    )

    if ($ExtraPostScript) {
        $ghidraArgs += @('-postScript', $ExtraPostScript)
        $ghidraArgs += $ExtraPostScriptArgs
    }

    & $analyzeHeadless @ghidraArgs 2>&1 | Tee-Object -FilePath $logPath

    if ($LASTEXITCODE -ne 0) {
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
