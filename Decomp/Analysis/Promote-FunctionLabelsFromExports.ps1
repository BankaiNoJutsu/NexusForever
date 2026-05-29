[CmdletBinding()]
param(
    [string] $OutputDir,
    [string] $LabelMap,
    [switch] $WhatIf
)

$ErrorActionPreference = 'Stop'

$scriptRoot = if ([string]::IsNullOrWhiteSpace($PSScriptRoot)) {
    Split-Path -Parent $MyInvocation.MyCommand.Path
} else {
    $PSScriptRoot
}

if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $OutputDir = Join-Path $scriptRoot 'exports'
}

if ([string]::IsNullOrWhiteSpace($LabelMap)) {
    $LabelMap = Join-Path $scriptRoot 'function_labels.csv'
}

$allowedPrefixPatterns = @(
    '^ClientDB_',
    '^Client[A-Z]',
    '^Server[A-Z]',
    '^StsConn_',
    '^StsInet',
    '^StsCrypt',
    '^Network',
    '^NetworkSocket_',
    '^NetworkIOCP_',
    '^Lua_',
    '^Game',
    '^Spell',
    '^Marketplace_',
    '^Storefront_',
    '^Crafting_',
    '^Tradeskill_',
    '^Account',
    '^CREDD',
    '^Pvp',
    '^P2P',
    '^ICComm',
    '^Friendship',
    '^Interaction_',
    '^Loot_',
    '^CombatLog_',
    '^Map_',
    '^RapidTransport',
    '^FlightPath',
    '^WorldSocket_',
    '^ClientWorldOpcode',
    '^PublicEvent',
    '^Quest',
    '^Dialog_',
    '^DeferredAction',
    '^CSI',
    '^ActionSet_',
    '^ActivateUnit',
    '^Audio',
    '^DailyLogin',
    '^RuneCrafting',
    '^CodeEnum',
    '^Vendor',
    '^Matching',
    '^Guild',
    '^Mail',
    '^Housing',
    '^Path',
    '^Datacube',
    '^Fortune',
    '^Challenges',
    '^Expedition',
    '^Instance',
    '^Script',
    '^Entity',
    '^Unit',
    '^Vital',
    '^Buff',
    '^Proc',
    '^Ravel'
)

$denyNamePatterns = @(
    '^FUN_',
    '^LAB_',
    '^thunk_',
    '^~',
    '^DoDataExchange$',
    '^entry$',
    '^get_acc',
    '^put_acc',
    '^EnableWindow$',
    '^CanBeDocked$',
    '^AtlThrow',
    '^default_error_condition',
    '^operator',
    '^std::',
    '^boost::',
    '^CMFC',
    '^CWnd',
    '^CPane',
    '^CFormView',
    '^CImageList',
    '^CMFCTool'
)

function Test-IsPromotableName {
    param([string] $Name)

    if ([string]::IsNullOrWhiteSpace($Name)) {
        return $false
    }

    foreach ($deny in $denyNamePatterns) {
        if ($Name -match $deny) {
            return $false
        }
    }

    foreach ($allow in $allowedPrefixPatterns) {
        if ($Name -match $allow) {
            return $true
        }
    }

    return $false
}

function Get-NormalizedAddress {
    param([string] $Address)

    $normalized = $Address.Trim().ToLowerInvariant()
    if ($normalized.StartsWith('0x')) {
        $normalized = $normalized.Substring(2)
    }

    return $normalized
}

function Import-ExistingLabelRows {
    param([string] $Path)

    $rows = New-Object 'System.Collections.Generic.List[string]'
    $keys = @{}
    if (-not (Test-Path -LiteralPath $Path)) {
        return @{ lines = $rows; keys = $keys }
    }

    foreach ($line in Get-Content -LiteralPath $Path) {
        [void] $rows.Add($line)
        $trimmed = $line.Trim()
        if ($trimmed.StartsWith('#') -or [string]::IsNullOrWhiteSpace($trimmed) -or $trimmed.StartsWith('program,')) {
            continue
        }

        $parts = $trimmed -split ',', 4
        if ($parts.Count -lt 3) {
            continue
        }

        $key = ('{0}|{1}' -f $parts[0].Trim().ToLowerInvariant(), (Get-NormalizedAddress $parts[1]))
        $keys[$key] = @{
            lineIndex = $rows.Count - 1
            name = $parts[2].Trim()
            comment = if ($parts.Count -gt 3) { $parts[3].Trim() } else { '' }
        }
    }

    return @{ lines = $rows; keys = $keys }
}

function Build-CommentFromReasons {
    param(
        [string] $Name,
        [string] $Reasons
    )

    if ($Name -match '^ClientDB_Register') {
        $table = $Name -replace '^ClientDB_Register', ''
        return "Registers client DB table loader for $table; export name and registration xref pattern."
    }

    if ($Name -match '_WritePayload$') {
        return "Serialises world message payload; see selected_reasons_summary.csv for opcode and field evidence."
    }

    if ($Name -match '^Lua_') {
        return "Lua binding or enum registration surfaced in export selection."
    }

    if ($Name -match '^StsConn_|^StsInet|^StsCrypt') {
        return "STS client transaction or socket helper named in export selection."
    }

    if (-not [string]::IsNullOrWhiteSpace($Reasons)) {
        $trimmed = $Reasons -replace '\s+', ' '
        if ($trimmed.Length -gt 240) {
            $trimmed = $trimmed.Substring(0, 237) + '...'
        }
        return "Export selection anchors: $trimmed"
    }

    return "Promoted from Ghidra export functions.csv with evidence-backed name $Name."
}

function Import-ReasonsLookup {
    param([string] $ExportPath)

    $path = Join-Path $ExportPath 'selected_reasons_summary.csv'
    $lookup = @{}
    if (-not (Test-Path -LiteralPath $path)) {
        return $lookup
    }

    foreach ($row in Import-Csv -LiteralPath $path) {
        $entry = if ($row.entry) { $row.entry } else { $row.address }
        $key = Get-NormalizedAddress $entry
        $lookup[$key] = [string] $row.reasons
    }

    return $lookup
}

function Escape-CsvField {
    param([string] $Value)

    if ($null -eq $Value) {
        return ''
    }

    if ($Value.Contains(',') -or $Value.Contains('"')) {
        return '"' + ($Value.Replace('"', '""')) + '"'
    }

    return $Value
}

function Add-LabelRow {
    param(
        [string] $Program,
        [string] $Address,
        [string] $Name,
        [string] $Comment,
        [hashtable] $Keys,
        [System.Collections.Generic.List[string]] $Lines,
        [ref] $Added,
        [ref] $Enriched
    )

    $key = ('{0}|{1}' -f $Program.ToLowerInvariant(), (Get-NormalizedAddress $Address))
    if ($Keys.ContainsKey($key)) {
        $existingRow = $Keys[$key]
        if ($existingRow.name -eq $Name -and [string]::IsNullOrWhiteSpace($existingRow.comment) -and -not [string]::IsNullOrWhiteSpace($Comment)) {
            if (-not $WhatIf) {
                $Lines[$existingRow.lineIndex] = ('{0},{1},{2},{3}' -f $Program, (Get-NormalizedAddress $Address), $Name, (Escape-CsvField $Comment))
            }
            $Enriched.Value++
        }
        return $false
    }

    $line = ('{0},{1},{2},{3}' -f $Program, (Get-NormalizedAddress $Address), $Name, (Escape-CsvField $Comment))
    if (-not $WhatIf) {
        [void] $Lines.Add($line)
    }

    $Keys[$key] = @{
        lineIndex = $Lines.Count - 1
        name      = $Name
        comment   = $Comment
    }
    $Added.Value++
    return $true
}

function Get-ClientDbRegisterName {
    param([string] $TablePath)

    if ($TablePath -notmatch '^DB\\(.+)\.tbl$') {
        return $null
    }

    $table = $Matches[1]
    $leaf = ($table -split '\\')[-1]
    if ([string]::IsNullOrWhiteSpace($leaf)) {
        return $null
    }

    return 'ClientDB_Register' + $leaf
}

$existing = Import-ExistingLabelRows -Path $LabelMap
$lines = $existing.lines
$keys = $existing.keys
$added = 0
$enriched = 0
$perBinary = @{}

foreach ($exportDir in Get-ChildItem -LiteralPath $OutputDir -Directory | Sort-Object Name) {
    $program = $exportDir.Name
    $functionsPath = Join-Path $exportDir.FullName 'functions.csv'
    if (-not (Test-Path -LiteralPath $functionsPath)) {
        continue
    }

    $reasons = Import-ReasonsLookup -ExportPath $exportDir.FullName
    $binaryAdded = 0
    foreach ($function in Import-Csv -LiteralPath $functionsPath) {
        if ([string] $function.external -eq 'True') {
            continue
        }

        $name = [string] $function.name
        if (-not (Test-IsPromotableName -Name $name)) {
            continue
        }

        $entry = if ($function.entry) { $function.entry } else { $function.address }
        $address = Get-NormalizedAddress $entry
        $key = ('{0}|{1}' -f $program.ToLowerInvariant(), $address)
        $reasonText = $reasons[$address]
        $comment = Build-CommentFromReasons -Name $name -Reasons $reasonText

        if ($keys.ContainsKey($key)) {
            $existingRow = $keys[$key]
            if ([string]::IsNullOrWhiteSpace($existingRow.comment) -and -not [string]::IsNullOrWhiteSpace($comment)) {
                if (-not $WhatIf) {
                    $lines[$existingRow.lineIndex] = ('{0},{1},{2},{3}' -f $program, $address, $existingRow.name, (Escape-CsvField $comment))
                }
                $enriched++
            }
            continue
        }

        $line = ('{0},{1},{2},{3}' -f $program, $address, $name, (Escape-CsvField $comment))
        if (-not $WhatIf) {
            [void] $lines.Add($line)
        }

        $keys[$key] = @{
            lineIndex = $lines.Count - 1
            name = $name
            comment = $comment
        }
        $added++
        $binaryAdded++
    }

    $reasonsPath = Join-Path $exportDir.FullName 'selected_reasons_summary.csv'
    if (Test-Path -LiteralPath $reasonsPath) {
        foreach ($row in Import-Csv -LiteralPath $reasonsPath) {
            $name = [string] $row.name
            if ($name -match '^FUN_|^LAB_|^thunk') {
                continue
            }

            $entry = if ($row.entry) { $row.entry } else { $row.address }
            $comment = Build-CommentFromReasons -Name $name -Reasons ([string] $row.reasons)
            if (Add-LabelRow -Program $program -Address $entry -Name $name -Comment $comment -Keys $keys -Lines $lines -Added ([ref]$added) -Enriched ([ref]$enriched)) {
                $binaryAdded++
            }
        }
    }

    $xrefsPath = Join-Path $exportDir.FullName 'string_xrefs.csv'
    if (Test-Path -LiteralPath $xrefsPath) {
        foreach ($row in Import-Csv -LiteralPath $xrefsPath) {
            $value = [string] $row.value
            $registerName = Get-ClientDbRegisterName -TablePath $value
            if ($null -eq $registerName) {
                continue
            }

            $entry = [string] $row.function_entry
            if ([string]::IsNullOrWhiteSpace($entry)) {
                continue
            }

            $comment = "Registers client DB table loader for $value; table path string xref at $($row.string_address)."
            if (Add-LabelRow -Program $program -Address $entry -Name $registerName -Comment $comment -Keys $keys -Lines $lines -Added ([ref]$added) -Enriched ([ref]$enriched)) {
                $binaryAdded++
            }
        }
    }

    if ($binaryAdded -gt 0) {
        $perBinary[$program] = $binaryAdded
    }
}

foreach ($exportDir in Get-ChildItem -LiteralPath $OutputDir -Directory | Sort-Object Name) {
    $program = $exportDir.Name
    $reasons = Import-ReasonsLookup -ExportPath $exportDir.FullName
    foreach ($key in @($keys.Keys)) {
        $parts = $key -split '\|', 2
        if ($parts[0] -ne $program.ToLowerInvariant()) {
            continue
        }

        $address = $parts[1]
        if (-not $reasons.ContainsKey($address)) {
            continue
        }

        $row = $keys[$key]
        $newComment = Build-CommentFromReasons -Name $row.name -Reasons $reasons[$address]
        if ([string]::IsNullOrWhiteSpace($newComment) -or $newComment -eq $row.comment) {
            continue
        }

        if ($row.comment -match '^Promoted from Ghidra export' -or [string]::IsNullOrWhiteSpace($row.comment)) {
            if (-not $WhatIf) {
                $lines[$row.lineIndex] = ('{0},{1},{2},{3}' -f $program, $address, $row.name, (Escape-CsvField $newComment))
            }
            $row.comment = $newComment
            $enriched++
        }
    }
}

if (-not $WhatIf) {
    Set-Content -LiteralPath $LabelMap -Value $lines -Encoding utf8
}

[pscustomobject]@{
    added = $added
    enriched = $enriched
    totalRows = ($keys.Count)
    perBinary = $perBinary
    labelMap = $LabelMap
}
