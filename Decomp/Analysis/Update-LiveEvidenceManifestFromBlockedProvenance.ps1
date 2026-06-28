[CmdletBinding()]
param(
    [string] $NativeProvenancePath,
    [string] $LiveEvidencePath,
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

if ([string]::IsNullOrWhiteSpace($LiveEvidencePath)) {
    $LiveEvidencePath = Join-Path $scriptRoot 'mapping_artifacts\live_evidence_bundle_manifest.csv'
}

if ([string]::IsNullOrWhiteSpace($ReviewedDate)) {
    $ReviewedDate = (Get-Date -Format 'yyyy-MM-dd')
}

function Get-SafeToken {
    param([string] $Value)

    $token = (([string] $Value) -replace '[^A-Za-z0-9]+', '').ToUpperInvariant()
    if ([string]::IsNullOrWhiteSpace($token)) {
        return 'UNKNOWN'
    }

    return $token
}

function New-LiveEvidenceRow {
    param($Provenance)

    return [pscustomobject]@{
        evidenceBundleId = 'LIVE-{0}' -f (Get-SafeToken $Provenance.opcodeEvidenceId)
        targetQuestion = 'What runtime action proves or rejects the semantic owner for {0} ({1})?' -f $Provenance.opcodeName, $Provenance.opcodeHex
        featureArea = 'Opcode semantic blocker'
        requiredRuntime = 'Local NexusForever runtime with Trace logging and WildStar client when static evidence is insufficient'
        clientBuild = '16042'
        serverConfig = 'Tools/Setup/Start-NexusForeverLocal.ps1'
        captureRoot = 'artifacts/live-evidence/{0}' -f (Get-SafeToken $Provenance.opcodeEvidenceId).ToLowerInvariant()
        commands = 'Start-BlockerEvidenceHarness.ps1; run focused client scenario; compare logs/captures with native provenance row'
        logs = ''
        packetCapture = ''
        relatedNativeEvidence = $Provenance.opcodeEvidenceId
        resultStatus = 'Blocked'
        findingPath = $Provenance.findingPath
        blockers = $Provenance.blockers
        lastReviewed = $ReviewedDate
    }
}

if (-not (Test-Path -LiteralPath $NativeProvenancePath)) {
    throw "Native opcode provenance ledger not found: $NativeProvenancePath"
}

if (-not (Test-Path -LiteralPath $LiveEvidencePath)) {
    throw "Live evidence manifest not found: $LiveEvidencePath"
}

$provenanceRows = @(Import-Csv -LiteralPath $NativeProvenancePath)
$existingRows = @(Import-Csv -LiteralPath $LiveEvidencePath)

$knownEvidenceIds = @{}
foreach ($row in $existingRows) {
    if (-not [string]::IsNullOrWhiteSpace($row.relatedNativeEvidence)) {
        $knownEvidenceIds[[string] $row.relatedNativeEvidence] = $true
    }
}

$rows = New-Object 'System.Collections.Generic.List[object]'
foreach ($row in $existingRows) {
    $rows.Add($row)
}

$added = 0
foreach ($provenance in $provenanceRows | Where-Object { $_.evidenceStatus -eq 'Blocked' }) {
    if ($knownEvidenceIds.ContainsKey([string] $provenance.opcodeEvidenceId)) {
        continue
    }

    $rows.Add((New-LiveEvidenceRow -Provenance $provenance))
    $knownEvidenceIds[[string] $provenance.opcodeEvidenceId] = $true
    $added++
}

$rows |
    Sort-Object -Property relatedNativeEvidence, evidenceBundleId |
    Export-Csv -LiteralPath $LiveEvidencePath -NoTypeInformation -Encoding utf8

Write-Host "Live evidence rows: existing=$($existingRows.Count) added=$added total=$($rows.Count)"
