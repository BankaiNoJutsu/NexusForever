[CmdletBinding()]
param(
    [string] $RepoRoot,
    [string] $OutputDir,
    [string] $LabelMap,
    [string] $CoverageDir
)

$ErrorActionPreference = 'Stop'

$scriptRoot = if ([string]::IsNullOrWhiteSpace($PSScriptRoot)) {
    Split-Path -Parent $MyInvocation.MyCommand.Path
} else {
    $PSScriptRoot
}

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = Join-Path $scriptRoot '..\..'
}

if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $OutputDir = Join-Path $scriptRoot 'exports'
}

if ([string]::IsNullOrWhiteSpace($LabelMap)) {
    $LabelMap = Join-Path $scriptRoot 'function_labels.csv'
}

if ([string]::IsNullOrWhiteSpace($CoverageDir)) {
    $CoverageDir = Join-Path $scriptRoot 'coverage'
}

New-Item -ItemType Directory -Force -Path $CoverageDir | Out-Null
$inventoryPath = Join-Path $CoverageDir 'function_label_inventory.csv'

function Import-LabelRows {
    param([string] $Path)

    $rows = New-Object 'System.Collections.Generic.List[object]'
    $lineNumber = 0
    foreach ($line in Get-Content -LiteralPath $Path) {
        $lineNumber++
        $trimmed = $line.Trim()
        if ([string]::IsNullOrWhiteSpace($trimmed) -or $trimmed.StartsWith('#') -or $trimmed.StartsWith('program,')) {
            continue
        }

        $parts = $trimmed -split ',', 4
        if ($parts.Count -lt 3) {
            continue
        }

        $rows.Add([pscustomobject]@{
            program = $parts[0].Trim()
            address = $parts[1].Trim()
            name = $parts[2].Trim()
            comment = if ($parts.Count -gt 3) { $parts[3].Trim() } else { '' }
            labelRow = $lineNumber
        })
    }

    return $rows
}

$labels = Import-LabelRows -Path $LabelMap
$labelLookup = @{}
foreach ($label in $labels) {
    $address = $label.address.Trim().ToLowerInvariant()
    if ($address.StartsWith('0x')) {
        $address = $address.Substring(2)
    }

    $key = ('{0}|{1}' -f $label.program.ToLowerInvariant(), $address)
    if (-not $labelLookup.ContainsKey($key)) {
        $labelLookup[$key] = $label
    }
}

$inventory = New-Object 'System.Collections.Generic.List[object]'
$exportDirs = Get-ChildItem -LiteralPath $OutputDir -Directory | Sort-Object -Property Name
foreach ($exportDir in $exportDirs) {
    $target = $exportDir.Name
    $functionsPath = Join-Path $exportDir.FullName 'functions.csv'
    if (-not (Test-Path -LiteralPath $functionsPath)) {
        continue
    }

    foreach ($function in Import-Csv -LiteralPath $functionsPath) {
        if ([string] $function.external -eq 'True') {
            continue
        }

        $address = ([string] $(if ($function.entry) { $function.entry } else { $function.address })).Trim().ToLowerInvariant()
        if ($address.StartsWith('0x')) {
            $address = $address.Substring(2)
        }

        $key = ('{0}|{1}' -f $target.ToLowerInvariant(), $address)
        $label = $labelLookup[$key]
        $inventory.Add([pscustomobject]@{
            program = $target
            address = $(if ($function.entry) { $function.entry } else { $function.address })
            ghidraName = $function.name
            durableLabel = if ($null -eq $label) { '' } else { $label.name }
            labelComment = if ($null -eq $label) { '' } else { $label.comment }
            labelRow = if ($null -eq $label) { '' } else { $label.labelRow }
            thunk = $function.thunk
            hasDurableLabel = [bool] $label
            exportDir = "Decomp/Analysis/exports/$target"
            cacheFragmentGlob = "selected_decompiled_cache/functions/*/*.fragment.properties"
        })
    }
}

$inventory |
    Sort-Object -Property program, address |
    Export-Csv -LiteralPath $inventoryPath -NoTypeInformation -Encoding utf8

Write-Host "Wrote $($inventory.Count) rows to $inventoryPath"
