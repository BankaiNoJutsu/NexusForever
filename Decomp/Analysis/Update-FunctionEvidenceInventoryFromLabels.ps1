[CmdletBinding()]
param(
    [string] $LabelMap,
    [string] $FunctionEvidencePath,
    [string] $ReviewedDate
)

$ErrorActionPreference = 'Stop'

$scriptRoot = if ([string]::IsNullOrWhiteSpace($PSScriptRoot)) {
    Split-Path -Parent $MyInvocation.MyCommand.Path
} else {
    $PSScriptRoot
}

if ([string]::IsNullOrWhiteSpace($LabelMap)) {
    $LabelMap = Join-Path $scriptRoot 'function_labels.csv'
}

if ([string]::IsNullOrWhiteSpace($FunctionEvidencePath)) {
    $FunctionEvidencePath = Join-Path $scriptRoot 'mapping_artifacts\function_evidence_inventory.csv'
}

if ([string]::IsNullOrWhiteSpace($ReviewedDate)) {
    $ReviewedDate = (Get-Date -Format 'yyyy-MM-dd')
}

function Normalize-Address {
    param([string] $Value)

    $text = ([string] $Value).Trim().ToLowerInvariant()
    if ($text.StartsWith('0x')) {
        $text = $text.Substring(2)
    }

    return $text
}

function Get-ProgramToken {
    param([string] $Program)

    $name = [IO.Path]::GetFileNameWithoutExtension($Program)
    $token = ($name -replace '[^A-Za-z0-9]+', '').ToUpperInvariant()
    if ([string]::IsNullOrWhiteSpace($token)) {
        return 'UNKNOWN'
    }

    return $token
}

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

function New-BackfillRow {
    param($Label)

    $programToken = Get-ProgramToken -Program $Label.program
    $address = Normalize-Address $Label.address
    $comment = ([string] $Label.comment).Trim()
    if ([string]::IsNullOrWhiteSpace($comment)) {
        $comment = 'Durable function label row without an inline comment.'
    }

    return [pscustomobject]@{
        functionEvidenceId = 'FUN-{0}-{1}' -f $programToken, $address.ToUpperInvariant()
        binary = $Label.program
        address = $address
        durableLabel = $Label.name
        labelRow = $Label.labelRow
        evidenceStatus = 'Mapped'
        purpose = $comment
        anchors = $comment
        inputsOutputs = ''
        callers = ''
        callees = ''
        relatedOpcodes = ''
        relatedTables = ''
        sourcePaths = ''
        findingPath = 'Decomp/Analysis/function_labels.csv'
        verification = 'Imported from durable function_labels.csv; label application is verified by the decompile manifest gate.'
        blockers = 'Source correlation and focused verification require a targeted mapping pass before implementation use.'
        lastReviewed = $ReviewedDate
    }
}

if (-not (Test-Path -LiteralPath $LabelMap)) {
    throw "Label map not found: $LabelMap"
}

if (-not (Test-Path -LiteralPath $FunctionEvidencePath)) {
    throw "Function evidence inventory not found: $FunctionEvidencePath"
}

$labelRows = @(Import-LabelRows -Path $LabelMap)
$existingRows = @(Import-Csv -LiteralPath $FunctionEvidencePath)

$evidenceByAddress = @{}
$evidenceByLabel = @{}
foreach ($row in $existingRows) {
    $addressKey = '{0}|{1}' -f ([string] $row.binary).ToLowerInvariant(), (Normalize-Address $row.address)
    $labelKey = 'label|{0}' -f ([string] $row.durableLabel).ToLowerInvariant()
    if (-not [string]::IsNullOrWhiteSpace($row.address)) {
        $evidenceByAddress[$addressKey] = $true
    }

    if (-not [string]::IsNullOrWhiteSpace($row.durableLabel)) {
        $evidenceByLabel[$labelKey] = $true
    }
}

$rows = New-Object 'System.Collections.Generic.List[object]'
foreach ($row in $existingRows) {
    $rows.Add($row)
}

$added = 0
foreach ($label in $labelRows) {
    $addressKey = '{0}|{1}' -f $label.program.ToLowerInvariant(), (Normalize-Address $label.address)
    $labelKey = 'label|{0}' -f $label.name.ToLowerInvariant()
    if ($evidenceByAddress.ContainsKey($addressKey) -or $evidenceByLabel.ContainsKey($labelKey)) {
        continue
    }

    $newRow = New-BackfillRow -Label $label
    $rows.Add($newRow)
    $evidenceByAddress[$addressKey] = $true
    $evidenceByLabel[$labelKey] = $true
    $added++
}

$rows |
    Sort-Object -Property binary, address, durableLabel |
    Export-Csv -LiteralPath $FunctionEvidencePath -NoTypeInformation -Encoding utf8

Write-Host "Function evidence rows: existing=$($existingRows.Count) added=$added total=$($rows.Count)"
