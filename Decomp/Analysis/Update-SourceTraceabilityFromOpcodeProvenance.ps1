[CmdletBinding()]
param(
    [string] $NativeProvenancePath,
    [string] $SourceTracePath,
    [string] $ReviewedDate
)

$ErrorActionPreference = 'Stop'

$scriptRoot = if ([string]::IsNullOrWhiteSpace($PSScriptRoot)) {
    Split-Path -Parent $MyInvocation.MyCommand.Path
} else {
    $PSScriptRoot
}

if ([string]::IsNullOrWhiteSpace($NativeProvenancePath)) {
    $NativeProvenancePath = Join-Path $scriptRoot 'mapping_artifacts\native_opcode_provenance.csv'
}

if ([string]::IsNullOrWhiteSpace($SourceTracePath)) {
    $SourceTracePath = Join-Path $scriptRoot 'mapping_artifacts\source_traceability_map.csv'
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

function Get-SafeToken {
    param([string] $Value)

    $token = (([string] $Value) -replace '[^A-Za-z0-9]+', '').ToUpperInvariant()
    if ([string]::IsNullOrWhiteSpace($token)) {
        return 'UNKNOWN'
    }

    return $token
}

function New-TraceRow {
    param($Provenance)

    $paths = @(
        ([string] $Provenance.nexusModelPath).Trim(),
        ([string] $Provenance.nexusHandlerPath).Trim()
    ) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }

    $symbols = @(
        ([string] $Provenance.nexusModel).Trim(),
        ([string] $Provenance.nexusHandler).Trim()
    ) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }

    return [pscustomobject]@{
        traceabilityId = 'TRACE-{0}' -f (Get-SafeToken $Provenance.opcodeEvidenceId)
        nativeEvidenceId = $Provenance.opcodeEvidenceId
        binary = $Provenance.binary
        nativeAddress = Normalize-Address $(if ($Provenance.sendOrApplyOwnerAddress) { $Provenance.sendOrApplyOwnerAddress } elseif ($Provenance.payloadWriterAddress) { $Provenance.payloadWriterAddress } elseif ($Provenance.payloadReaderAddress) { $Provenance.payloadReaderAddress } else { $Provenance.nativeRegistrationAddress })
        nativeLabel = $(if ($Provenance.sendOrApplyOwnerFunction) { $Provenance.sendOrApplyOwnerFunction } elseif ($Provenance.payloadWriterFunction) { $Provenance.payloadWriterFunction } elseif ($Provenance.payloadReaderFunction) { $Provenance.payloadReaderFunction } elseif ($Provenance.nativeRegistrationFunction) { $Provenance.nativeRegistrationFunction } else { $Provenance.opcodeName })
        nexusArea = ('{0} opcode coverage' -f $Provenance.direction).Trim()
        nexusArtifactKind = 'CoverageModelHandler'
        nexusPath = ($paths -join '; ')
        nexusSymbol = ($symbols -join '; ')
        implementationStatus = $Provenance.evidenceStatus
        tests = ''
        findingPath = $Provenance.findingPath
        residualRisk = $Provenance.blockers
        lastReviewed = $ReviewedDate
    }
}

if (-not (Test-Path -LiteralPath $NativeProvenancePath)) {
    throw "Native opcode provenance ledger not found: $NativeProvenancePath"
}

if (-not (Test-Path -LiteralPath $SourceTracePath)) {
    throw "Source traceability map not found: $SourceTracePath"
}

$provenanceRows = @(Import-Csv -LiteralPath $NativeProvenancePath)
$existingRows = @(Import-Csv -LiteralPath $SourceTracePath)

$knownEvidenceIds = @{}
foreach ($row in $existingRows) {
    if (-not [string]::IsNullOrWhiteSpace($row.nativeEvidenceId)) {
        $knownEvidenceIds[[string] $row.nativeEvidenceId] = $true
    }
}

$rows = New-Object 'System.Collections.Generic.List[object]'
foreach ($row in $existingRows) {
    $rows.Add($row)
}

$added = 0
foreach ($provenance in $provenanceRows) {
    if ($knownEvidenceIds.ContainsKey([string] $provenance.opcodeEvidenceId)) {
        continue
    }

    $rows.Add((New-TraceRow -Provenance $provenance))
    $knownEvidenceIds[[string] $provenance.opcodeEvidenceId] = $true
    $added++
}

$rows |
    Sort-Object -Property nativeEvidenceId, traceabilityId |
    Export-Csv -LiteralPath $SourceTracePath -NoTypeInformation -Encoding utf8

Write-Host "Source traceability rows: existing=$($existingRows.Count) added=$added total=$($rows.Count)"
