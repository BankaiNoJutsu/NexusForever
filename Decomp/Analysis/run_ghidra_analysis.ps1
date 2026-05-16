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
        $logPath = Join-Path $logDir ("{0}{1}{2}.ghidra.log" -f $targetName, $projectLogSuffix, $logSuffix)
        $exportDir = Join-Path $resolvedOutputDir $target
        $manifestPath = Join-Path $exportDir 'selected_decompiled.manifest'
        $targetSummary = [ordered]@{
            target = $target
            binaryPath = $binaryPath
            binaryFingerprint = $binaryFingerprint
            projectName = $projectName
            exportDir = $exportDir
            manifestPath = $manifestPath
            logPath = $logPath
            status = 'pending'
            exitCode = $null
            projectLockFailure = $false
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

        $targetSummary.exitCode = $LASTEXITCODE
        $projectLockFailure = Select-String -LiteralPath $logPath -SimpleMatch -Pattern 'Unable to lock project!' -Quiet
        $targetSummary.projectLockFailure = [bool]$projectLockFailure

        if ($LASTEXITCODE -ne 0) {
            $targetSummary.status = 'failed'
            Finalize-TargetSummary -TargetSummary $targetSummary -ExpectedBinaryFingerprint $binaryFingerprint -ExpectedLabelFingerprint $labelFingerprint
            $runSummaryTargets.Add([pscustomobject]$targetSummary)

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
        clientDir = $resolvedClientDir
        outputDir = $resolvedOutputDir
        projectDir = $resolvedProjectDir
        logDir = $logDir
        decompileMode = $DecompileMode
        projectLayout = $effectiveProjectLayout
        exportOnly = [bool]$ExportOnly
        noApplyLabels = [bool]$NoApplyLabels
        labelsApplied = [bool]$labelsApplied
        labelMap = if ($resolvedLabelMap) { $resolvedLabelMap } else { '' }
        labelFingerprint = $labelFingerprint
        maxDecompiledFunctions = $MaxDecompiledFunctions
        extraPostScript = if ($ExtraPostScript) { $ExtraPostScript } else { '' }
        extraPostScriptArgs = $ExtraPostScriptArgs
        targetCount = $Targets.Count
        targets = $runSummaryTargets
    }

    $summaryPath = Join-Path $logDir 'LATEST_RUN_SUMMARY.json'
    $runSummary | ConvertTo-Json -Depth 6 | Out-File -LiteralPath $summaryPath -Encoding utf8
    Write-Host "Run summary written to: $summaryPath"
}

Write-Host "Ghidra run complete. Exports: $OutputDir"
