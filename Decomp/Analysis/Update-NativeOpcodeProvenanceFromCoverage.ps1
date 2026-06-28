[CmdletBinding()]
param(
    [string] $OpcodeCoveragePath,
    [string] $NativeProvenancePath,
    [string] $ReviewedDate
)

$ErrorActionPreference = 'Stop'

$scriptRoot = if ([string]::IsNullOrWhiteSpace($PSScriptRoot)) {
    Split-Path -Parent $MyInvocation.MyCommand.Path
} else {
    $PSScriptRoot
}

if ([string]::IsNullOrWhiteSpace($OpcodeCoveragePath)) {
    $OpcodeCoveragePath = Join-Path $scriptRoot 'coverage\opcode_coverage_inventory.csv'
}

if ([string]::IsNullOrWhiteSpace($NativeProvenancePath)) {
    $NativeProvenancePath = Join-Path $scriptRoot 'mapping_artifacts\native_opcode_provenance.csv'
}

if ([string]::IsNullOrWhiteSpace($ReviewedDate)) {
    $ReviewedDate = (Get-Date -Format 'yyyy-MM-dd')
}

function Normalize-Hex {
    param([string] $Value)

    $text = ([string] $Value).Trim()
    if ([string]::IsNullOrWhiteSpace($text)) {
        return ''
    }

    if ($text.StartsWith('0x', [StringComparison]::OrdinalIgnoreCase)) {
        $text = $text.Substring(2)
    }

    return ('0x{0}' -f $text.ToUpperInvariant().PadLeft(4, '0'))
}

function Get-SafeToken {
    param([string] $Value)

    $token = (([string] $Value) -replace '[^A-Za-z0-9]+', '').ToUpperInvariant()
    if ([string]::IsNullOrWhiteSpace($token)) {
        return 'UNKNOWN'
    }

    return $token
}

function Get-EvidenceStatus {
    param($CoverageRow)

    if ([string] $CoverageRow.placeholderModeled -eq 'True' -or [string] $CoverageRow.decodeState -eq 'placeholder') {
        return 'Blocked'
    }

    if (-not [string]::IsNullOrWhiteSpace($CoverageRow.comment)) {
        return 'Correlated'
    }

    return 'Blocked'
}

function Get-BlockerText {
    param($CoverageRow)

    $comment = ([string] $CoverageRow.comment).Trim()
    if ([string] $CoverageRow.placeholderModeled -eq 'True' -or [string] $CoverageRow.decodeState -eq 'placeholder') {
        if ([string]::IsNullOrWhiteSpace($comment)) {
            return 'Placeholder packet lacks native semantic owner; keep diagnostic or output-only boundary until mapped.'
        }

        return 'Placeholder packet has partial native evidence but lacks safe semantic owner; do not rename or mutate behavior without a direct witness.'
    }

    if ([string]::IsNullOrWhiteSpace($comment)) {
        return 'Native registration and payload evidence have not yet been recorded in coverage; run a focused decompile pass before semantic use.'
    }

    return 'Promote to Mapped only after native registration and payload functions are copied into this row from focused evidence.'
}

function New-ProvenanceRow {
    param($CoverageRow)

    $hex = Normalize-Hex $CoverageRow.hex
    $direction = ([string] $CoverageRow.direction).Trim()
    $opcodeName = ([string] $CoverageRow.opcode).Trim()
    $comment = ([string] $CoverageRow.comment).Trim()
    $registrationEvidence = if ([string]::IsNullOrWhiteSpace($comment)) {
        'No native provenance comment yet in opcode coverage inventory.'
    } else {
        $comment
    }

    return [pscustomobject]@{
        opcodeEvidenceId = 'OPC-{0}-{1}-{2}' -f (Get-SafeToken $direction), (Get-SafeToken $opcodeName), $hex.Substring(2)
        opcodeName = $opcodeName
        opcodeHex = $hex
        direction = $direction
        binary = $(if ($direction -eq 'Client' -or $direction -eq 'Server') { 'WildStar64.exe' } else { '' })
        nativeRegistrationFunction = ''
        nativeRegistrationAddress = ''
        registrationEvidence = $registrationEvidence
        payloadReaderFunction = ''
        payloadReaderAddress = ''
        payloadWriterFunction = ''
        payloadWriterAddress = ''
        sendOrApplyOwnerFunction = ''
        sendOrApplyOwnerAddress = ''
        payloadShape = ''
        nexusModel = $CoverageRow.modelTypes
        nexusModelPath = $CoverageRow.modelPaths
        nexusHandler = $CoverageRow.handlerTypes
        nexusHandlerPath = $CoverageRow.handlerPaths
        evidenceStatus = Get-EvidenceStatus -CoverageRow $CoverageRow
        findingPath = 'Decomp/Analysis/coverage/opcode_coverage_inventory.csv'
        blockers = Get-BlockerText -CoverageRow $CoverageRow
        lastReviewed = $ReviewedDate
    }
}

if (-not (Test-Path -LiteralPath $OpcodeCoveragePath)) {
    throw "Opcode coverage inventory not found: $OpcodeCoveragePath"
}

if (-not (Test-Path -LiteralPath $NativeProvenancePath)) {
    throw "Native opcode provenance ledger not found: $NativeProvenancePath"
}

$coverageRows = @(Import-Csv -LiteralPath $OpcodeCoveragePath)
$existingRows = @(Import-Csv -LiteralPath $NativeProvenancePath)

$knownNames = @{}
$knownHexes = @{}
foreach ($row in $existingRows) {
    if (-not [string]::IsNullOrWhiteSpace($row.opcodeName)) {
        $knownNames[[string] $row.opcodeName] = $true
    }

    $hex = Normalize-Hex $row.opcodeHex
    if (-not [string]::IsNullOrWhiteSpace($hex)) {
        $knownHexes[$hex] = $true
    }
}

$rows = New-Object 'System.Collections.Generic.List[object]'
foreach ($row in $existingRows) {
    $rows.Add($row)
}

$added = 0
foreach ($coverageRow in $coverageRows) {
    $name = ([string] $coverageRow.opcode).Trim()
    $hex = Normalize-Hex $coverageRow.hex
    if ($knownNames.ContainsKey($name) -or $knownHexes.ContainsKey($hex)) {
        continue
    }

    $newRow = New-ProvenanceRow -CoverageRow $coverageRow
    $rows.Add($newRow)
    $knownNames[$name] = $true
    $knownHexes[$hex] = $true
    $added++
}

$rows |
    Sort-Object -Property direction, opcodeHex, opcodeName |
    Export-Csv -LiteralPath $NativeProvenancePath -NoTypeInformation -Encoding utf8

Write-Host "Native opcode provenance rows: existing=$($existingRows.Count) added=$added total=$($rows.Count)"
