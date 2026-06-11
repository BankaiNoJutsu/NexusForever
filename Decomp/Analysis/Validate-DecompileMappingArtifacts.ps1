[CmdletBinding()]
param(
    [string] $ArtifactsDir
)

$ErrorActionPreference = 'Stop'

$scriptRoot = if ([string]::IsNullOrWhiteSpace($PSScriptRoot)) {
    Split-Path -Parent $MyInvocation.MyCommand.Path
} else {
    $PSScriptRoot
}

if ([string]::IsNullOrWhiteSpace($ArtifactsDir)) {
    $ArtifactsDir = Join-Path $scriptRoot 'mapping_artifacts'
}

$allowedStatuses = @(
    'Observed',
    'Correlated',
    'Mapped',
    'Verified',
    'Implemented',
    'Blocked'
)

$schemas = [ordered]@{
    'native_opcode_provenance.csv' = @(
        'opcodeEvidenceId',
        'opcodeName',
        'opcodeHex',
        'direction',
        'binary',
        'nativeRegistrationFunction',
        'nativeRegistrationAddress',
        'registrationEvidence',
        'payloadReaderFunction',
        'payloadReaderAddress',
        'payloadWriterFunction',
        'payloadWriterAddress',
        'sendOrApplyOwnerFunction',
        'sendOrApplyOwnerAddress',
        'payloadShape',
        'nexusModel',
        'nexusModelPath',
        'nexusHandler',
        'nexusHandlerPath',
        'evidenceStatus',
        'findingPath',
        'blockers',
        'lastReviewed'
    )
    'function_evidence_inventory.csv' = @(
        'functionEvidenceId',
        'binary',
        'address',
        'durableLabel',
        'labelRow',
        'evidenceStatus',
        'purpose',
        'anchors',
        'inputsOutputs',
        'callers',
        'callees',
        'relatedOpcodes',
        'relatedTables',
        'sourcePaths',
        'findingPath',
        'verification',
        'blockers',
        'lastReviewed'
    )
    'structure_offset_maps.csv' = @(
        'structureMapRowId',
        'mapKind',
        'structureName',
        'binary',
        'functionFamily',
        'evidenceStatus',
        'objectRole',
        'baseParameter',
        'offsetKind',
        'offset',
        'sizeBits',
        'accessMode',
        'fieldName',
        'inferredType',
        'evidence',
        'functions',
        'sourcePath',
        'blockers',
        'lastReviewed'
    )
    'dispatcher_registration_exports.csv' = @(
        'registrationExportId',
        'binary',
        'registrationFamily',
        'registrationFunction',
        'registrationAddress',
        'slotOrOpcode',
        'direction',
        'payloadFunction',
        'payloadAddress',
        'registeredSizeBytes',
        'tableOrFamilyAddress',
        'evidenceSource',
        'helperScript',
        'generatedOutputReference',
        'evidenceStatus',
        'blockers',
        'lastReviewed'
    )
    'source_traceability_map.csv' = @(
        'traceabilityId',
        'nativeEvidenceId',
        'binary',
        'nativeAddress',
        'nativeLabel',
        'nexusArea',
        'nexusArtifactKind',
        'nexusPath',
        'nexusSymbol',
        'implementationStatus',
        'tests',
        'findingPath',
        'residualRisk',
        'lastReviewed'
    )
    'live_evidence_bundle_manifest.csv' = @(
        'evidenceBundleId',
        'targetQuestion',
        'featureArea',
        'requiredRuntime',
        'clientBuild',
        'serverConfig',
        'captureRoot',
        'commands',
        'logs',
        'packetCapture',
        'relatedNativeEvidence',
        'resultStatus',
        'findingPath',
        'blockers',
        'lastReviewed'
    )
    'packet_fixture_plan.csv' = @(
        'fixturePlanId',
        'opcodeName',
        'opcodeHex',
        'direction',
        'modelPath',
        'fixturePath',
        'nativePayloadEvidence',
        'fieldsCovered',
        'testProjectPath',
        'fixtureStatus',
        'blockers',
        'lastReviewed'
    )
}

function Add-ValidationError {
    param(
        [System.Collections.Generic.List[string]] $Errors,
        [string] $Message
    )

    $Errors.Add($Message) | Out-Null
}

function Get-HeaderColumns {
    param([string] $Path)

    $firstLine = [System.IO.File]::ReadLines($Path) | Select-Object -First 1
    if ($null -eq $firstLine) {
        return @()
    }

    return $firstLine.TrimStart([char]0xfeff) -split ','
}

$errors = New-Object 'System.Collections.Generic.List[string]'
$totalRows = 0

if (-not (Test-Path -LiteralPath $ArtifactsDir)) {
    throw "Mapping artifact directory not found: $ArtifactsDir"
}

foreach ($entry in $schemas.GetEnumerator()) {
    $fileName = $entry.Key
    $expectedHeader = $entry.Value
    $path = Join-Path $ArtifactsDir $fileName

    if (-not (Test-Path -LiteralPath $path)) {
        Add-ValidationError -Errors $errors -Message "$fileName is missing."
        continue
    }

    $actualHeader = @(Get-HeaderColumns -Path $path)
    $expectedJoined = $expectedHeader -join ','
    $actualJoined = $actualHeader -join ','
    if ($actualJoined -ne $expectedJoined) {
        Add-ValidationError -Errors $errors -Message "$fileName header mismatch. Expected '$expectedJoined' but found '$actualJoined'."
        continue
    }

    $rows = @(Import-Csv -LiteralPath $path)
    $totalRows += $rows.Count
    $keyColumn = $expectedHeader[0]
    $seenKeys = @{}

    if ($rows.Count -eq 0) {
        Add-ValidationError -Errors $errors -Message "$fileName must contain at least one evidence-backed example row."
    }

    foreach ($row in $rows) {
        $key = ([string] $row.$keyColumn).Trim()
        if ([string]::IsNullOrWhiteSpace($key)) {
            Add-ValidationError -Errors $errors -Message "$fileName contains a row with an empty $keyColumn."
        } elseif ($seenKeys.ContainsKey($key)) {
            Add-ValidationError -Errors $errors -Message "$fileName contains duplicate $keyColumn '$key'."
        } else {
            $seenKeys[$key] = $true
        }

        foreach ($statusColumn in @('evidenceStatus', 'implementationStatus', 'resultStatus', 'fixtureStatus')) {
            if ($actualHeader -notcontains $statusColumn) {
                continue
            }

            $status = ([string] $row.$statusColumn).Trim()
            if ([string]::IsNullOrWhiteSpace($status)) {
                Add-ValidationError -Errors $errors -Message "$fileName row '$key' has an empty $statusColumn."
            } elseif ($allowedStatuses -notcontains $status) {
                Add-ValidationError -Errors $errors -Message "$fileName row '$key' has invalid $statusColumn '$status'."
            }
        }

        $lastReviewed = ([string] $row.lastReviewed).Trim()
        if ([string]::IsNullOrWhiteSpace($lastReviewed)) {
            Add-ValidationError -Errors $errors -Message "$fileName row '$key' has an empty lastReviewed."
        } elseif (-not [regex]::IsMatch($lastReviewed, '^\d{4}-\d{2}-\d{2}$')) {
            Add-ValidationError -Errors $errors -Message "$fileName row '$key' has malformed lastReviewed '$lastReviewed'. Use YYYY-MM-DD."
        } else {
            $parsedDate = [datetime]::MinValue
            if (-not [datetime]::TryParseExact(
                $lastReviewed,
                'yyyy-MM-dd',
                [Globalization.CultureInfo]::InvariantCulture,
                [Globalization.DateTimeStyles]::None,
                [ref] $parsedDate)) {
                Add-ValidationError -Errors $errors -Message "$fileName row '$key' has invalid lastReviewed date '$lastReviewed'. Use YYYY-MM-DD."
            }
        }
    }
}

if ($errors.Count -gt 0) {
    foreach ($validationError in $errors) {
        Write-Host "ERROR: $validationError" -ForegroundColor Red
    }

    throw "Decompile mapping artifact validation failed with $($errors.Count) error(s)."
}

Write-Host "Validated $totalRows mapping artifact row(s) across $($schemas.Count) file(s)."
