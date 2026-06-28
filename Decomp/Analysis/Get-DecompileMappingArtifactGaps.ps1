[CmdletBinding()]
param(
    [string] $RepoRoot,
    [string] $ArtifactsDir,
    [string] $CoverageDir,
    [string] $LabelMap,
    [switch] $WriteReports,
    [switch] $FailOnGap
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

if ([string]::IsNullOrWhiteSpace($ArtifactsDir)) {
    $ArtifactsDir = Join-Path $scriptRoot 'mapping_artifacts'
}

if ([string]::IsNullOrWhiteSpace($CoverageDir)) {
    $CoverageDir = Join-Path $scriptRoot 'coverage'
}

if ([string]::IsNullOrWhiteSpace($LabelMap)) {
    $LabelMap = Join-Path $scriptRoot 'function_labels.csv'
}

$opcodeCoveragePath = Join-Path $CoverageDir 'opcode_coverage_inventory.csv'
$nativeProvenancePath = Join-Path $ArtifactsDir 'native_opcode_provenance.csv'
$functionEvidencePath = Join-Path $ArtifactsDir 'function_evidence_inventory.csv'
$structureMapPath = Join-Path $ArtifactsDir 'structure_offset_maps.csv'
$dispatcherPath = Join-Path $ArtifactsDir 'dispatcher_registration_exports.csv'
$sourceTracePath = Join-Path $ArtifactsDir 'source_traceability_map.csv'
$liveEvidencePath = Join-Path $ArtifactsDir 'live_evidence_bundle_manifest.csv'
$fixturePlanPath = Join-Path $ArtifactsDir 'packet_fixture_plan.csv'

foreach ($requiredPath in @(
    $opcodeCoveragePath,
    $nativeProvenancePath,
    $functionEvidencePath,
    $structureMapPath,
    $dispatcherPath,
    $sourceTracePath,
    $liveEvidencePath,
    $fixturePlanPath,
    $LabelMap
)) {
    if (-not (Test-Path -LiteralPath $requiredPath)) {
        throw "Required input not found: $requiredPath"
    }
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

function Normalize-Address {
    param([string] $Value)

    $text = ([string] $Value).Trim().ToLowerInvariant()
    if ($text.StartsWith('0x')) {
        $text = $text.Substring(2)
    }

    return $text
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

function Add-Key {
    param(
        [hashtable] $Set,
        [string] $Key
    )

    if (-not [string]::IsNullOrWhiteSpace($Key)) {
        $Set[$Key] = $true
    }
}

$opcodeCoverage = @(Import-Csv -LiteralPath $opcodeCoveragePath)
$nativeProvenance = @(Import-Csv -LiteralPath $nativeProvenancePath)
$functionEvidence = @(Import-Csv -LiteralPath $functionEvidencePath)
$structureMaps = @(Import-Csv -LiteralPath $structureMapPath)
$dispatcherRows = @(Import-Csv -LiteralPath $dispatcherPath)
$sourceTraceRows = @(Import-Csv -LiteralPath $sourceTracePath)
$liveRows = @(Import-Csv -LiteralPath $liveEvidencePath)
$fixtureRows = @(Import-Csv -LiteralPath $fixturePlanPath)
$labelRows = @(Import-LabelRows -Path $LabelMap)

$provenanceOpcodeKeys = @{}
foreach ($row in $nativeProvenance) {
    Add-Key -Set $provenanceOpcodeKeys -Key ([string] $row.opcodeName)
    Add-Key -Set $provenanceOpcodeKeys -Key (Normalize-Hex $row.opcodeHex)
}

$missingOpcodeProvenance = foreach ($row in $opcodeCoverage) {
    $nameKey = ([string] $row.opcode).Trim()
    $hexKey = Normalize-Hex $row.hex
    if (-not $provenanceOpcodeKeys.ContainsKey($nameKey) -and -not $provenanceOpcodeKeys.ContainsKey($hexKey)) {
        [pscustomobject]@{
            opcode = $row.opcode
            hex = $row.hex
            direction = $row.direction
            coverageStatus = $row.coverageStatus
            decodeState = $row.decodeState
            placeholderModeled = $row.placeholderModeled
            modelPaths = $row.modelPaths
            handlerPaths = $row.handlerPaths
            comment = $row.comment
        }
    }
}

$functionEvidenceKeys = @{}
foreach ($row in $functionEvidence) {
    Add-Key -Set $functionEvidenceKeys -Key ('{0}|{1}' -f ([string] $row.binary).ToLowerInvariant(), (Normalize-Address $row.address))
    Add-Key -Set $functionEvidenceKeys -Key ('label|{0}' -f ([string] $row.durableLabel).ToLowerInvariant())
}

$missingFunctionEvidence = foreach ($row in $labelRows) {
    $addressKey = '{0}|{1}' -f $row.program.ToLowerInvariant(), (Normalize-Address $row.address)
    $labelKey = 'label|{0}' -f $row.name.ToLowerInvariant()
    if (-not $functionEvidenceKeys.ContainsKey($addressKey) -and -not $functionEvidenceKeys.ContainsKey($labelKey)) {
        [pscustomobject]@{
            program = $row.program
            address = $row.address
            name = $row.name
            labelRow = $row.labelRow
            comment = $row.comment
        }
    }
}

$structureKeys = @{}
foreach ($row in $structureMaps) {
    Add-Key -Set $structureKeys -Key ([string] $row.structureName)
    Add-Key -Set $structureKeys -Key ([string] $row.functionFamily)
}

$fixtureKeys = @{}
foreach ($row in $fixtureRows) {
    Add-Key -Set $fixtureKeys -Key ([string] $row.opcodeName)
    Add-Key -Set $fixtureKeys -Key (Normalize-Hex $row.opcodeHex)
}

$dispatcherKeys = @{}
foreach ($row in $dispatcherRows) {
    Add-Key -Set $dispatcherKeys -Key ('{0}|{1}' -f ([string] $row.registrationFunction), (Normalize-Hex $row.slotOrOpcode))
}

$sourceTraceKeys = @{}
foreach ($row in $sourceTraceRows) {
    Add-Key -Set $sourceTraceKeys -Key ([string] $row.nativeEvidenceId)
}

$liveKeys = @{}
foreach ($row in $liveRows) {
    Add-Key -Set $liveKeys -Key ([string] $row.relatedNativeEvidence)
}

$provenanceMissingStructure = foreach ($row in $nativeProvenance) {
    if ([string]::IsNullOrWhiteSpace($row.payloadShape)) {
        continue
    }

    if (-not $structureKeys.ContainsKey($row.payloadWriterFunction) -and -not $structureKeys.ContainsKey($row.payloadReaderFunction)) {
        [pscustomobject]@{
            opcodeEvidenceId = $row.opcodeEvidenceId
            opcodeName = $row.opcodeName
            opcodeHex = $row.opcodeHex
            payloadReaderFunction = $row.payloadReaderFunction
            payloadWriterFunction = $row.payloadWriterFunction
            payloadShape = $row.payloadShape
        }
    }
}

$provenanceMissingFixture = foreach ($row in $nativeProvenance) {
    if ([string]::IsNullOrWhiteSpace($row.payloadShape)) {
        continue
    }

    $hexKey = Normalize-Hex $row.opcodeHex
    if (-not $fixtureKeys.ContainsKey($row.opcodeName) -and -not $fixtureKeys.ContainsKey($hexKey)) {
        [pscustomobject]@{
            opcodeEvidenceId = $row.opcodeEvidenceId
            opcodeName = $row.opcodeName
            opcodeHex = $row.opcodeHex
            payloadShape = $row.payloadShape
        }
    }
}

$provenanceMissingDispatcher = foreach ($row in $nativeProvenance) {
    if ([string]::IsNullOrWhiteSpace($row.nativeRegistrationFunction) -or [string]::IsNullOrWhiteSpace($row.opcodeHex)) {
        continue
    }

    $key = '{0}|{1}' -f $row.nativeRegistrationFunction, (Normalize-Hex $row.opcodeHex)
    if (-not $dispatcherKeys.ContainsKey($key)) {
        [pscustomobject]@{
            opcodeEvidenceId = $row.opcodeEvidenceId
            opcodeName = $row.opcodeName
            opcodeHex = $row.opcodeHex
            nativeRegistrationFunction = $row.nativeRegistrationFunction
            nativeRegistrationAddress = $row.nativeRegistrationAddress
        }
    }
}

$provenanceMissingSourceTrace = foreach ($row in $nativeProvenance) {
    if (-not $sourceTraceKeys.ContainsKey($row.opcodeEvidenceId)) {
        [pscustomobject]@{
            opcodeEvidenceId = $row.opcodeEvidenceId
            opcodeName = $row.opcodeName
            opcodeHex = $row.opcodeHex
            nexusModelPath = $row.nexusModelPath
            nexusHandlerPath = $row.nexusHandlerPath
        }
    }
}

$blockedProvenanceMissingLive = foreach ($row in $nativeProvenance) {
    if ($row.evidenceStatus -ne 'Blocked') {
        continue
    }

    if ($row.blockers -notmatch '(?i)\b(live|sniff|capture|runtime|client scenario)\b') {
        continue
    }

    if (-not $liveKeys.ContainsKey($row.opcodeEvidenceId)) {
        [pscustomobject]@{
            opcodeEvidenceId = $row.opcodeEvidenceId
            opcodeName = $row.opcodeName
            opcodeHex = $row.opcodeHex
            blockers = $row.blockers
        }
    }
}

$reports = [ordered]@{
    missing_opcode_provenance = @($missingOpcodeProvenance)
    missing_function_evidence = @($missingFunctionEvidence)
    provenance_missing_structure = @($provenanceMissingStructure)
    provenance_missing_dispatcher = @($provenanceMissingDispatcher)
    provenance_missing_source_trace = @($provenanceMissingSourceTrace)
    blocked_provenance_missing_live = @($blockedProvenanceMissingLive)
    provenance_missing_fixture = @($provenanceMissingFixture)
}

$summary = [pscustomobject]@{
    opcodeCoverageRows = $opcodeCoverage.Count
    nativeOpcodeProvenanceRows = $nativeProvenance.Count
    missingOpcodeProvenanceRows = @($missingOpcodeProvenance).Count
    durableFunctionLabelRows = $labelRows.Count
    functionEvidenceRows = $functionEvidence.Count
    missingFunctionEvidenceRows = @($missingFunctionEvidence).Count
    provenanceRowsMissingStructureRows = @($provenanceMissingStructure).Count
    provenanceRowsMissingDispatcherRows = @($provenanceMissingDispatcher).Count
    provenanceRowsMissingSourceTraceRows = @($provenanceMissingSourceTrace).Count
    blockedProvenanceRowsMissingLiveRows = @($blockedProvenanceMissingLive).Count
    provenanceRowsMissingFixtureRows = @($provenanceMissingFixture).Count
}

$gapCount = @(
    @($missingOpcodeProvenance).Count,
    @($missingFunctionEvidence).Count,
    @($provenanceMissingStructure).Count,
    @($provenanceMissingDispatcher).Count,
    @($provenanceMissingSourceTrace).Count,
    @($blockedProvenanceMissingLive).Count,
    @($provenanceMissingFixture).Count
) | Measure-Object -Sum | Select-Object -ExpandProperty Sum

if ($WriteReports) {
    $reportDir = Join-Path $ArtifactsDir 'gap_reports'
    New-Item -ItemType Directory -Force -Path $reportDir | Out-Null

    foreach ($entry in $reports.GetEnumerator()) {
        $reportPath = Join-Path $reportDir "$($entry.Key).csv"
        @($entry.Value) | Export-Csv -LiteralPath $reportPath -NoTypeInformation -Encoding utf8
    }

    $summary | Export-Csv -LiteralPath (Join-Path $reportDir 'gap_summary.csv') -NoTypeInformation -Encoding utf8
    Write-Host "Wrote mapping gap reports to $reportDir"
}

$summary | Format-List

if ($FailOnGap -and $gapCount -gt 0) {
    throw "Decompile mapping artifact gap audit found $gapCount gap(s)."
}
