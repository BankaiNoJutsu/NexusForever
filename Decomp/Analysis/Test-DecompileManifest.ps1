[CmdletBinding()]
param(
    [string] $OutputDir = (Join-Path $PSScriptRoot 'exports'),
    [string] $SummaryPath = (Join-Path $PSScriptRoot 'logs\LATEST_RUN_SUMMARY.json'),
    [string[]] $Targets = @(),
    [switch] $AsJson,
    [switch] $FailOnMismatch
)

$ErrorActionPreference = 'Stop'

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

function Get-ExpectedTargetSummary {
    param(
        [object] $LatestRunSummary,
        [string] $Target
    )

    if ($null -eq $LatestRunSummary) {
        return $null
    }

    return @($LatestRunSummary.targets | Where-Object { $_.target -eq $Target } | Select-Object -First 1)[0]
}

$latestRunSummary = if (Test-Path -LiteralPath $SummaryPath -PathType Leaf) {
    Get-Content -LiteralPath $SummaryPath -Raw | ConvertFrom-Json
}
else {
    $null
}

if ($Targets.Count -eq 0) {
    if ($null -ne $latestRunSummary) {
        $Targets = @($latestRunSummary.targets | ForEach-Object { $_.target })
    }
    elseif (Test-Path -LiteralPath $OutputDir -PathType Container) {
        $Targets = @(Get-ChildItem -LiteralPath $OutputDir -Directory | Select-Object -ExpandProperty Name)
    }
}

if ($Targets.Count -eq 0) {
    throw "No decompile targets were found. Supply -Targets explicitly, generate logs/LATEST_RUN_SUMMARY.json, or create exports under $OutputDir."
}

$records = foreach ($target in $Targets) {
    $expectedTargetSummary = Get-ExpectedTargetSummary -LatestRunSummary $latestRunSummary -Target $target
    $manifestPath = if ($null -ne $expectedTargetSummary -and -not [string]::IsNullOrWhiteSpace([string] $expectedTargetSummary.manifestPath)) {
        [string] $expectedTargetSummary.manifestPath
    }
    else {
        Join-Path (Join-Path $OutputDir $target) 'selected_decompiled.manifest'
    }

    $issues = New-Object 'System.Collections.Generic.List[string]'
    $properties = Read-KeyValuePropertiesFile -Path $manifestPath
    $cacheSummaryPath = Join-Path (Split-Path -Parent $manifestPath) 'decompile_cache_summary.properties'
    $cacheProperties = Read-KeyValuePropertiesFile -Path $cacheSummaryPath
    if ($null -eq $cacheProperties) {
        $cacheProperties = @{}
    }
    if ($null -eq $properties) {
        $issues.Add('missing-manifest')
        [pscustomobject]@{
            target                      = $target
            status                      = [string]::Join(',', $issues)
            selectedCount               = $null
            reusedFragments             = $null
            decompiledFragments         = $null
            canonicalCachedFragments    = ConvertTo-NullableInt -Value $cacheProperties['cache.canonicalCachedFragments']
            warmedThisRun               = ConvertTo-NullableInt -Value $cacheProperties['cache.warmedThisRun']
            remainingUncached           = ConvertTo-NullableInt -Value $cacheProperties['cache.remainingUncached']
            skippedAlreadyExported      = ConvertTo-NullableInt -Value $cacheProperties['cache.skippedAlreadyExported']
            outputExists                = $null
            binaryFingerprintMatchesRun = $null
            labelFingerprintMatchesRun  = $null
            manifestPath                = $manifestPath
            cacheSummaryPath            = $cacheSummaryPath
        }
        continue
    }

    $outputExists = ConvertTo-NullableBool -Value $properties['output.exists']
    if ($outputExists -eq $false) {
        $issues.Add('output-missing')
    }

    $binaryFingerprintMatchesRun = $null
    if ($null -ne $expectedTargetSummary -and -not [string]::IsNullOrWhiteSpace([string] $expectedTargetSummary.binaryFingerprint)) {
        $binaryFingerprintMatchesRun = [string] $expectedTargetSummary.binaryFingerprint -eq [string] $properties['binary.fingerprint']
        if (-not $binaryFingerprintMatchesRun) {
            $issues.Add('binary-fingerprint-mismatch')
        }
    }

    $labelFingerprintMatchesRun = $null
    if ($null -ne $latestRunSummary -and -not [string]::IsNullOrWhiteSpace([string] $latestRunSummary.labelFingerprint)) {
        $labelFingerprintMatchesRun = [string] $latestRunSummary.labelFingerprint -eq [string] $properties['labels.fingerprint']
        if (-not $labelFingerprintMatchesRun) {
            $issues.Add('label-fingerprint-mismatch')
        }
    }

    [pscustomobject]@{
        target                      = $target
        status                      = if ($issues.Count -eq 0) { 'ok' } else { [string]::Join(',', $issues) }
        selectedCount               = ConvertTo-NullableInt -Value $properties['selected.count']
        reusedFragments             = ConvertTo-NullableInt -Value $properties['cache.reusedFragments']
        decompiledFragments         = ConvertTo-NullableInt -Value $properties['cache.decompiledFragments']
        canonicalCachedFragments    = ConvertTo-NullableInt -Value $cacheProperties['cache.canonicalCachedFragments']
        warmedThisRun               = ConvertTo-NullableInt -Value $cacheProperties['cache.warmedThisRun']
        remainingUncached           = ConvertTo-NullableInt -Value $cacheProperties['cache.remainingUncached']
        skippedAlreadyExported      = ConvertTo-NullableInt -Value $cacheProperties['cache.skippedAlreadyExported']
        outputExists                = $outputExists
        binaryFingerprintMatchesRun = $binaryFingerprintMatchesRun
        labelFingerprintMatchesRun  = $labelFingerprintMatchesRun
        manifestPath                = $manifestPath
        cacheSummaryPath            = $cacheSummaryPath
    }
}

if ($AsJson) {
    $records | ConvertTo-Json -Depth 5
}
else {
    $records |
        Sort-Object -Property target |
        Format-Table -AutoSize target, status, selectedCount, reusedFragments, decompiledFragments, canonicalCachedFragments, warmedThisRun, remainingUncached, skippedAlreadyExported, outputExists, binaryFingerprintMatchesRun, labelFingerprintMatchesRun, manifestPath
}

if ($FailOnMismatch -and ($records | Where-Object { $_.status -ne 'ok' } | Measure-Object).Count -gt 0) {
    throw 'One or more decompile manifests failed validation. Re-run the export or inspect the manifest and latest run summary for mismatches.'
}