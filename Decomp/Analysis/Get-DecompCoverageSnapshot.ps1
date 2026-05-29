[CmdletBinding()]
param(
    [string] $RepoRoot = (Join-Path $PSScriptRoot '..\..'),
    [string] $OutputDir = (Join-Path $PSScriptRoot 'exports'),
    [string] $LogDir = (Join-Path $PSScriptRoot 'logs'),
    [string] $CoverageDir = (Join-Path $PSScriptRoot 'coverage'),
    [string] $RunSummaryPath = (Join-Path $LogDir 'LATEST_RUN_SUMMARY.json'),
    [string] $OpcodeFile = '',
    [string] $SourceDir = ''
)

$ErrorActionPreference = 'Stop'

$resolvedRepoRoot = (Resolve-Path -LiteralPath $RepoRoot).Path
$resolvedOutputDir = if (Test-Path -LiteralPath $OutputDir -PathType Container) {
    (Resolve-Path -LiteralPath $OutputDir).Path
}
else {
    $OutputDir
}

New-Item -ItemType Directory -Force -Path $LogDir | Out-Null
$resolvedLogDir = (Resolve-Path -LiteralPath $LogDir).Path

New-Item -ItemType Directory -Force -Path $CoverageDir | Out-Null
$resolvedCoverageDir = (Resolve-Path -LiteralPath $CoverageDir).Path

if ([string]::IsNullOrWhiteSpace($OpcodeFile)) {
    $OpcodeFile = Join-Path $resolvedRepoRoot 'Source\NexusForever.Network\Message\GameMessageOpcode.cs'
}

if ([string]::IsNullOrWhiteSpace($SourceDir)) {
    $SourceDir = Join-Path $resolvedRepoRoot 'Source'
}

$resolvedOpcodeFile = (Resolve-Path -LiteralPath $OpcodeFile).Path
$resolvedSourceDir = (Resolve-Path -LiteralPath $SourceDir).Path

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

function Get-RelativeRepoPath {
    param(
        [string] $Path
    )

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return ''
    }

    try {
        return [IO.Path]::GetRelativePath($resolvedRepoRoot, $Path).Replace('\', '/')
    }
    catch {
        return $Path.Replace('\', '/')
    }
}

function Get-SourceProjectName {
    param(
        [string] $Path
    )

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return ''
    }

    try {
        $relative = [IO.Path]::GetRelativePath($resolvedSourceDir, $Path)
        return ($relative -split '[\\/]', 2)[0]
    }
    catch {
        return ''
    }
}

function Import-CsvSafe {
    param(
        [string] $Path
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        return @()
    }

    return @(Import-Csv -LiteralPath $Path)
}

function Get-BoolCount {
    param(
        [object[]] $Rows,
        [string] $PropertyName
    )

    return @($Rows | Where-Object { [string] $_.$PropertyName -eq 'True' }).Count
}

function Get-Percent {
    param(
        [double] $Numerator,
        [double] $Denominator
    )

    if ($Denominator -le 0) {
        return 0
    }

    return [Math]::Round(($Numerator / $Denominator) * 100, 2)
}

function Test-IsDefaultGhidraFunctionName {
    param(
        [string] $Name
    )

    if ([string]::IsNullOrWhiteSpace($Name)) {
        return $true
    }

    return $Name -match '^(FUN|LAB)_[0-9A-Fa-f]+$' -or
        $Name -match '^thunk_(FUN|LAB)_[0-9A-Fa-f]+$' -or
        $Name -match '^thunk_[0-9A-Fa-f]+$'
}

function Get-LatestRunSummaryTarget {
    param(
        [object] $LatestRunSummary,
        [string] $Target
    )

    if ($null -eq $LatestRunSummary) {
        return $null
    }

    return @($LatestRunSummary.targets | Where-Object { $_.target -eq $Target } | Select-Object -First 1)[0]
}

function Get-ExportCoverageRecord {
    param(
        [string] $ExportPath,
        [object] $LatestRunSummary
    )

    $target = Split-Path -Leaf $ExportPath
    $functions = Import-CsvSafe -Path (Join-Path $ExportPath 'functions.csv')
    $imports = Import-CsvSafe -Path (Join-Path $ExportPath 'imports.csv')
    $strings = Import-CsvSafe -Path (Join-Path $ExportPath 'strings.csv')
    $interestingStrings = Import-CsvSafe -Path (Join-Path $ExportPath 'interesting_strings.csv')
    $stringXrefs = Import-CsvSafe -Path (Join-Path $ExportPath 'string_xrefs.csv')
    $selectedReasons = Import-CsvSafe -Path (Join-Path $ExportPath 'selected_reasons_summary.csv')
    $manifestPath = Join-Path $ExportPath 'selected_decompiled.manifest'
    $manifest = Read-KeyValuePropertiesFile -Path $manifestPath
    $latestRunTarget = Get-LatestRunSummaryTarget -LatestRunSummary $LatestRunSummary -Target $target
    $selectionAuditSelectedForDecompile = Get-BoolCount -Rows $selectedReasons -PropertyName 'selected_for_decompile'
    $manifestSelectedCount = if ($null -eq $manifest) { $null } else { ConvertTo-NullableInt -Value $manifest['selected.count'] }
    $reusedFragments = if ($null -eq $manifest) { $null } else { ConvertTo-NullableInt -Value $manifest['cache.reusedFragments'] }
    $decompiledFragments = if ($null -eq $manifest) { $null } else { ConvertTo-NullableInt -Value $manifest['cache.decompiledFragments'] }

    $durableLabelCount = @($functions | Where-Object {
        [string] $_.external -ne 'True' -and -not (Test-IsDefaultGhidraFunctionName -Name ([string] $_.name))
    }).Count
    $functionCount = $functions.Count
    $cacheSummaryPath = Join-Path $ExportPath 'decompile_cache_summary.properties'
    $cacheSummary = Read-KeyValuePropertiesFile -Path $cacheSummaryPath
    $canonicalCacheFragments = if ($null -eq $cacheSummary) { $null } else { ConvertTo-NullableInt -Value $cacheSummary['cache.canonicalCachedFragments'] }
    $remainingUncachedFragments = if ($null -eq $cacheSummary) { $null } else { ConvertTo-NullableInt -Value $cacheSummary['cache.remainingUncached'] }
    $totalInternalFunctions = if ($null -eq $cacheSummary) { $null } else { ConvertTo-NullableInt -Value $cacheSummary['functions.totalInternal'] }
    if ($null -eq $canonicalCacheFragments -or $canonicalCacheFragments -eq 0) {
        $cacheRoot = Join-Path $ExportPath 'selected_decompiled_cache\functions'
        if (Test-Path -LiteralPath $cacheRoot -PathType Container) {
            $canonicalCacheFragments = @(
                Get-ChildItem -LiteralPath $cacheRoot -Recurse -File -Filter '*.fragment.properties' -ErrorAction SilentlyContinue
            ).Count
        }
    }

    if ($null -eq $totalInternalFunctions -or $totalInternalFunctions -eq 0) {
        $totalInternalFunctions = $functionCount
    }

    if ($null -eq $remainingUncachedFragments) {
        $remainingUncachedFragments = [Math]::Max(0, $totalInternalFunctions - [int]$canonicalCacheFragments)
    }

    $canonicalCachePercent = Get-Percent -Numerator $canonicalCacheFragments -Denominator $totalInternalFunctions
    $selectedFunctionCount = if ($null -ne $manifestSelectedCount -and $manifestSelectedCount -gt $selectedReasons.Count) { $manifestSelectedCount } else { $selectedReasons.Count }
    $selectedForDecompileCount = if ($null -ne $decompiledFragments -and $decompiledFragments -gt $selectionAuditSelectedForDecompile) { $decompiledFragments } else { $selectionAuditSelectedForDecompile }
    $defaultNamedFunctionCount = @($functions | Where-Object { Test-IsDefaultGhidraFunctionName -Name ([string] $_.name) }).Count

    [pscustomobject]@{
        target = $target
        latestRunStatus = if ($null -eq $latestRunTarget) { '' } else { [string] $latestRunTarget.status }
        projectName = if ($null -eq $latestRunTarget) { '' } else { [string] $latestRunTarget.projectName }
        exportDir = Get-RelativeRepoPath -Path $ExportPath
        functions = $functionCount
        durableLabels = $durableLabelCount
        namedFunctionCoveragePercent = Get-Percent -Numerator $durableLabelCount -Denominator $functionCount
        defaultNamedFunctions = $defaultNamedFunctionCount
        thunkFunctions = @($functions | Where-Object { [string] $_.thunk -eq 'True' }).Count
        externalFunctions = @($functions | Where-Object { [string] $_.external -eq 'True' }).Count
        imports = $imports.Count
        interestingImports = Get-BoolCount -Rows $imports -PropertyName 'interesting'
        strings = $strings.Count
        interestingStrings = $interestingStrings.Count
        stringXrefs = $stringXrefs.Count
        selectedFunctions = $selectedFunctionCount
        unselectedFunctions = [Math]::Max(0, $functionCount - $selectedFunctionCount)
        selectedForDecompile = $selectedForDecompileCount
        fullFunctionDecompilePercent = Get-Percent -Numerator $selectedForDecompileCount -Denominator $functionCount
        canonicalCacheFragments = $canonicalCacheFragments
        canonicalCachePercent = $canonicalCachePercent
        remainingUncachedFragments = $remainingUncachedFragments
        totalInternalFunctions = $totalInternalFunctions
        selectionAuditRows = $selectedReasons.Count
        selectionAuditSelectedForDecompile = $selectionAuditSelectedForDecompile
        labelAnchoredSelections = @($selectedReasons | Where-Object { [string] $_.reasons -match '(^| \|\| )label:' }).Count
        targetAnchoredSelections = @($selectedReasons | Where-Object { [string] $_.reasons -match '(^| \|\| )target:' }).Count
        importAnchoredSelections = @($selectedReasons | Where-Object { [string] $_.reasons -match '(^| \|\| )import:' }).Count
        stringAnchoredSelections = @($selectedReasons | Where-Object { [string] $_.reasons -match '(^| \|\| )string:' }).Count
        manifestSelectedCount = $manifestSelectedCount
        reusedFragments = $reusedFragments
        decompiledFragments = $decompiledFragments
    }
}

function Get-MessageModelRecords {
    param(
        [string] $Root
    )

    $records = New-Object 'System.Collections.Generic.List[object]'
    $files = Get-ChildItem -LiteralPath $Root -Recurse -File -Filter '*.cs' |
        Where-Object { $_.FullName -match '\\Message\\Model\\' }

    foreach ($file in $files) {
        $lines = Get-Content -LiteralPath $file.FullName
        for ($index = 0; $index -lt $lines.Count; $index++) {
            $line = $lines[$index]
            if ($line -notmatch '\[Message\(GameMessageOpcode\.(?<opcode>[A-Za-z0-9_]+)\)\]') {
                continue
            }

            $opcodeName = [string] $Matches.opcode

            $signatureBuilder = New-Object System.Text.StringBuilder
            for ($scan = $index + 1; $scan -lt [Math]::Min($index + 10, $lines.Count); $scan++) {
                [void] $signatureBuilder.Append(' ')
                [void] $signatureBuilder.Append($lines[$scan].Trim())
                if ($lines[$scan] -match '\{') {
                    break
                }
            }

            $signature = $signatureBuilder.ToString()
            if ($signature -notmatch '(?:public|internal)\s+(?:sealed\s+|abstract\s+|partial\s+)*class\s+(?<class>[A-Za-z0-9_]+)\s*(?::\s*(?<interfaces>[^\{]+))?') {
                continue
            }

            $interfaces = [string] $Matches.interfaces
            $records.Add([pscustomobject]@{
                opcode = $opcodeName
                modelType = [string] $Matches.class
                modelPath = Get-RelativeRepoPath -Path $file.FullName
                modelProject = Get-SourceProjectName -Path $file.FullName
                readable = $interfaces -match '\bIReadable\b'
                writable = $interfaces -match '\bIWritable\b'
            })
        }
    }

    return $records
}

function Get-HandlerRecords {
    param(
        [string] $Root
    )

    $records = New-Object 'System.Collections.Generic.List[object]'
    $files = Get-ChildItem -LiteralPath $Root -Recurse -File -Filter '*.cs' |
        Where-Object { $_.FullName -match '\\Message\\Handler\\' }

    foreach ($file in $files) {
        $content = Get-Content -LiteralPath $file.FullName -Raw
        $handlerMatches = [regex]::Matches(
            $content,
            '(?:public|internal)\s+(?:sealed\s+|abstract\s+|partial\s+)*class\s+(?<handler>[A-Za-z0-9_]+)\s*:\s*IMessageHandler<[^,>]+,\s*(?<model>[A-Za-z0-9_]+)>',
            [System.Text.RegularExpressions.RegexOptions]::Singleline)

        foreach ($match in $handlerMatches) {
            $records.Add([pscustomobject]@{
                modelType = $match.Groups['model'].Value
                handlerType = $match.Groups['handler'].Value
                handlerPath = Get-RelativeRepoPath -Path $file.FullName
                handlerProject = Get-SourceProjectName -Path $file.FullName
            })
        }
    }

    return $records
}

function Get-OpcodeEntries {
    param(
        [string] $Path
    )

    $records = New-Object 'System.Collections.Generic.List[object]'
    $lineNumber = 0
    foreach ($line in Get-Content -LiteralPath $Path) {
        $lineNumber++
        if ($line -notmatch '^\s*(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*=\s*0x(?<hex>[0-9A-Fa-f]+),\s*(?://\s*(?<comment>.*))?$') {
            continue
        }

        $name = [string] $Matches.name
        $value = [uint32]::Parse(
            [string] $Matches.hex,
            [System.Globalization.NumberStyles]::HexNumber,
            [System.Globalization.CultureInfo]::InvariantCulture)
        $direction = if ($name.StartsWith('Client')) {
            'Client'
        }
        elseif ($name.StartsWith('Server')) {
            'Server'
        }
        else {
            'Core'
        }

        $comment = [string] $Matches.comment
        $isPlaceholderName = $name -match '^(Client|Server)[0-9A-F]{4}$'
        $isPlaceholderComment = $comment -match '(?i)opaque|unknown|unnamed|placeholder'

        $records.Add([pscustomobject]@{
            opcode = $name
            hex = ('0x{0:X4}' -f $value)
            numericValue = $value
            direction = $direction
            comment = $comment
            enumPath = Get-RelativeRepoPath -Path $Path
            enumLine = $lineNumber
            placeholderHint = ($isPlaceholderName -or $isPlaceholderComment)
        })
    }

    return $records.ToArray()
}

function Get-OpcodeCoverageRecords {
    param(
        [object[]] $OpcodeEntries,
        [object[]] $ModelRecords,
        [object[]] $HandlerRecords
    )

    $modelsByOpcode = @{}
    foreach ($model in $ModelRecords) {
        if (-not $modelsByOpcode.ContainsKey($model.opcode)) {
            $modelsByOpcode[$model.opcode] = New-Object 'System.Collections.Generic.List[object]'
        }
        $modelsByOpcode[$model.opcode].Add($model)
    }

    $handlersByModel = @{}
    foreach ($handler in $HandlerRecords) {
        if (-not $handlersByModel.ContainsKey($handler.modelType)) {
            $handlersByModel[$handler.modelType] = New-Object 'System.Collections.Generic.List[object]'
        }
        $handlersByModel[$handler.modelType].Add($handler)
    }

    $entriesByAliasKey = @{}
    foreach ($entry in $OpcodeEntries) {
        $aliasKey = '{0}:{1}' -f $entry.direction, $entry.numericValue
        if (-not $entriesByAliasKey.ContainsKey($aliasKey)) {
            $entriesByAliasKey[$aliasKey] = New-Object 'System.Collections.Generic.List[object]'
        }
        $entriesByAliasKey[$aliasKey].Add($entry)
    }

    $records = foreach ($entry in $OpcodeEntries | Sort-Object -Property numericValue, opcode) {
        $aliasKey = '{0}:{1}' -f $entry.direction, $entry.numericValue
        $aliasEntries = if ($entriesByAliasKey.ContainsKey($aliasKey)) {
            $entriesByAliasKey[$aliasKey].ToArray()
        }
        else {
            @($entry)
        }

        $models = foreach ($aliasEntry in $aliasEntries) {
            if ($modelsByOpcode.ContainsKey($aliasEntry.opcode)) {
                $modelsByOpcode[$aliasEntry.opcode].ToArray()
            }
        }

        $models = @($models | Sort-Object -Property modelType -Unique)
        $handlers = foreach ($model in $models) {
            if ($handlersByModel.ContainsKey($model.modelType)) {
                $handlersByModel[$model.modelType].ToArray()
            }
        }

        $handlers = @($handlers | Sort-Object -Property handlerType -Unique)
        $coverageStatus = switch ($entry.direction) {
            'Client' {
                if ($handlers.Count -gt 0) { 'handled' }
                elseif ($models.Count -gt 0) { 'model_only' }
                else { 'enum_only' }
            }
            'Server' {
                if ($models.Count -gt 0) { 'modeled' }
                else { 'enum_only' }
            }
            default {
                if ($models.Count -gt 0 -or $handlers.Count -gt 0) { 'core_modeled' }
                else { 'core_enum_only' }
            }
        }

        $placeholderModeled = $false
        if ($entry.placeholderHint -and $models.Count -gt 0) {
            $placeholderModeled = $true
        }

        [pscustomobject]@{
            opcode = $entry.opcode
            hex = $entry.hex
            numericValue = $entry.numericValue
            direction = $entry.direction
            coverageStatus = $coverageStatus
            decodeState = if ($entry.placeholderHint) { 'placeholder' } else { 'named' }
            comment = $entry.comment
            enumPath = $entry.enumPath
            enumLine = $entry.enumLine
            aliasCount = $aliasEntries.Count
            aliasOpcodes = ($aliasEntries | ForEach-Object { $_.opcode } | Sort-Object -Unique) -join '; '
            modelCount = $models.Count
            modelTypes = ($models | ForEach-Object { $_.modelType } | Sort-Object -Unique) -join '; '
            modelPaths = ($models | ForEach-Object { $_.modelPath } | Sort-Object -Unique) -join '; '
            modelProjects = ($models | ForEach-Object { $_.modelProject } | Sort-Object -Unique) -join '; '
            readableModelCount = @($models | Where-Object { $_.readable }).Count
            writableModelCount = @($models | Where-Object { $_.writable }).Count
            handlerCount = $handlers.Count
            handlerTypes = ($handlers | ForEach-Object { $_.handlerType } | Sort-Object -Unique) -join '; '
            handlerPaths = ($handlers | ForEach-Object { $_.handlerPath } | Sort-Object -Unique) -join '; '
            handlerProjects = ($handlers | ForEach-Object { $_.handlerProject } | Sort-Object -Unique) -join '; '
            placeholderModeled = $placeholderModeled
        }
    }

    return $records
}

function Get-MarkdownTable {
    param(
        [string[]] $Headers,
        [object[]] $Rows,
        [scriptblock] $Selector
    )

    $builder = New-Object System.Text.StringBuilder
    [void] $builder.AppendLine('| ' + ($Headers -join ' | ') + ' |')
    [void] $builder.AppendLine('| ' + (($Headers | ForEach-Object { '---' }) -join ' | ') + ' |')
    foreach ($row in $Rows) {
        $cells = @(& $Selector $row)
        [void] $builder.AppendLine('| ' + (($cells | ForEach-Object { [string] $_ }) -join ' | ') + ' |')
    }

    return $builder.ToString().TrimEnd()
}

$latestRunSummary = if (Test-Path -LiteralPath $RunSummaryPath -PathType Leaf) {
    Get-Content -LiteralPath $RunSummaryPath -Raw | ConvertFrom-Json
}
else {
    $null
}

$exportDirectories = New-Object 'System.Collections.Generic.List[string]'
if ($null -ne $latestRunSummary) {
    foreach ($target in $latestRunSummary.targets) {
        if (-not [string]::IsNullOrWhiteSpace([string] $target.exportDir) -and (Test-Path -LiteralPath ([string] $target.exportDir) -PathType Container)) {
            $exportDirectories.Add([string] $target.exportDir)
        }
    }
}

if (Test-Path -LiteralPath $resolvedOutputDir -PathType Container) {
    foreach ($directory in Get-ChildItem -LiteralPath $resolvedOutputDir -Directory | Sort-Object -Property Name) {
        if ($exportDirectories -notcontains $directory.FullName) {
            $exportDirectories.Add($directory.FullName)
        }
    }
}

$exportCoverage = @($exportDirectories | ForEach-Object {
    Get-ExportCoverageRecord -ExportPath $_ -LatestRunSummary $latestRunSummary
} | Sort-Object -Property target)
$totalFunctions = ($exportCoverage | Measure-Object -Property functions -Sum).Sum
$totalDurableLabels = ($exportCoverage | Measure-Object -Property durableLabels -Sum).Sum
$totalDefaultNamedFunctions = ($exportCoverage | Measure-Object -Property defaultNamedFunctions -Sum).Sum
$totalSelectedFunctions = ($exportCoverage | Measure-Object -Property selectedFunctions -Sum).Sum
$totalSelectedForDecompile = ($exportCoverage | Measure-Object -Property selectedForDecompile -Sum).Sum
$totalUnselectedFunctions = ($exportCoverage | Measure-Object -Property unselectedFunctions -Sum).Sum
$totalCanonicalCacheFragments = ($exportCoverage | Measure-Object -Property canonicalCacheFragments -Sum).Sum
$totalInternalFunctionCount = ($exportCoverage | Measure-Object -Property totalInternalFunctions -Sum).Sum
$totalRemainingUncachedFragments = ($exportCoverage | Measure-Object -Property remainingUncachedFragments -Sum).Sum

$opcodeEntries = Get-OpcodeEntries -Path $resolvedOpcodeFile
$messageModels = Get-MessageModelRecords -Root $resolvedSourceDir
$handlerRecords = Get-HandlerRecords -Root $resolvedSourceDir
$opcodeCoverage = Get-OpcodeCoverageRecords -OpcodeEntries $opcodeEntries -ModelRecords $messageModels -HandlerRecords $handlerRecords

$clientCoverage = @($opcodeCoverage | Where-Object { $_.direction -eq 'Client' })
$serverCoverage = @($opcodeCoverage | Where-Object { $_.direction -eq 'Server' })
$coreCoverage = @($opcodeCoverage | Where-Object { $_.direction -eq 'Core' })

$clientHandled = @($clientCoverage | Where-Object { $_.coverageStatus -eq 'handled' }).Count
$clientModelOnly = @($clientCoverage | Where-Object { $_.coverageStatus -eq 'model_only' }).Count
$clientEnumOnly = @($clientCoverage | Where-Object { $_.coverageStatus -eq 'enum_only' }).Count

$serverModeled = @($serverCoverage | Where-Object { $_.coverageStatus -eq 'modeled' }).Count
$serverEnumOnly = @($serverCoverage | Where-Object { $_.coverageStatus -eq 'enum_only' }).Count

$placeholderModeled = @($opcodeCoverage | Where-Object { $_.placeholderModeled }).Count
$serverPlaceholderModeled = @($serverCoverage | Where-Object { $_.placeholderModeled }).Count
$serverNamedModeled = @($serverCoverage | Where-Object { $_.coverageStatus -eq 'modeled' -and -not $_.placeholderModeled }).Count

$missingClientModels = @($clientCoverage | Where-Object { $_.coverageStatus -eq 'enum_only' } | Sort-Object -Property numericValue | Select-Object -First 25)
$missingClientHandlers = @($clientCoverage | Where-Object { $_.coverageStatus -eq 'model_only' } | Sort-Object -Property numericValue | Select-Object -First 25)
$missingServerModels = @($serverCoverage | Where-Object { $_.coverageStatus -eq 'enum_only' } | Sort-Object -Property numericValue | Select-Object -First 25)
$placeholderQueue = @($opcodeCoverage | Where-Object { $_.placeholderModeled } | Sort-Object -Property numericValue | Select-Object -First 25)

$exportInventoryPath = Join-Path $resolvedCoverageDir 'export_coverage_inventory.csv'
$opcodeInventoryPath = Join-Path $resolvedCoverageDir 'opcode_coverage_inventory.csv'
$markdownPath = Join-Path $resolvedCoverageDir 'LATEST_COVERAGE_SUMMARY.md'
$jsonPath = Join-Path $resolvedLogDir 'LATEST_COVERAGE_SUMMARY.json'

$exportCoverage |
    Export-Csv -LiteralPath $exportInventoryPath -NoTypeInformation -Encoding utf8

$opcodeCoverage |
    Select-Object opcode, hex, direction, coverageStatus, decodeState, aliasCount, aliasOpcodes, modelCount, handlerCount, readableModelCount, writableModelCount, placeholderModeled, comment, modelTypes, modelPaths, handlerTypes, handlerPaths |
    Export-Csv -LiteralPath $opcodeInventoryPath -NoTypeInformation -Encoding utf8

$summary = [ordered]@{
    generatedUtc = (Get-Date).ToUniversalTime().ToString('o')
    repoRoot = $resolvedRepoRoot
    outputDir = $resolvedOutputDir
    logDir = $resolvedLogDir
    coverageDir = $resolvedCoverageDir
    runSummaryPath = if (Test-Path -LiteralPath $RunSummaryPath -PathType Leaf) { $RunSummaryPath } else { '' }
    outputFiles = [ordered]@{
        markdownPath = $markdownPath
        jsonPath = $jsonPath
        exportInventoryPath = $exportInventoryPath
        opcodeInventoryPath = $opcodeInventoryPath
    }
    exportSummary = [ordered]@{
        targetCount = $exportCoverage.Count
        totalFunctions = $totalFunctions
        totalDurableLabels = $totalDurableLabels
        totalNamedFunctionCoveragePercent = Get-Percent -Numerator $totalDurableLabels -Denominator $totalFunctions
        totalDefaultNamedFunctions = $totalDefaultNamedFunctions
        totalInterestingStrings = ($exportCoverage | Measure-Object -Property interestingStrings -Sum).Sum
        totalSelectedFunctions = $totalSelectedFunctions
        totalUnselectedFunctions = $totalUnselectedFunctions
        totalSelectedForDecompile = $totalSelectedForDecompile
        totalFullFunctionDecompilePercent = Get-Percent -Numerator $totalSelectedForDecompile -Denominator $totalFunctions
        totalCanonicalCacheFragments = $totalCanonicalCacheFragments
        totalCanonicalCachePercent = Get-Percent -Numerator $totalCanonicalCacheFragments -Denominator $totalInternalFunctionCount
        totalRemainingUncachedFragments = $totalRemainingUncachedFragments
    }
    opcodeSummary = [ordered]@{
        total = $opcodeCoverage.Count
        clientTotal = $clientCoverage.Count
        clientHandled = $clientHandled
        clientModelOnly = $clientModelOnly
        clientEnumOnly = $clientEnumOnly
        serverTotal = $serverCoverage.Count
        serverModeled = $serverModeled
        serverNamedModeled = $serverNamedModeled
        serverPlaceholderModeled = $serverPlaceholderModeled
        serverEnumOnly = $serverEnumOnly
        coreTotal = $coreCoverage.Count
        placeholderModeled = $placeholderModeled
    }
    exports = $exportCoverage
    queues = [ordered]@{
        missingClientModels = $missingClientModels
        missingClientHandlers = $missingClientHandlers
        missingServerModels = $missingServerModels
        placeholderModels = $placeholderQueue
    }
}

$summary | ConvertTo-Json -Depth 8 | Out-File -LiteralPath $jsonPath -Encoding utf8

$exportTable = if ($exportCoverage.Count -gt 0) {
    Get-MarkdownTable -Headers @('Target', 'Functions', 'Named %', 'Cache %', 'Remaining', 'Selected', 'Focused %') -Rows $exportCoverage -Selector {
        param($row)
        @(
            "``$($row.target)``",
            $row.functions,
            ('{0}%' -f $row.namedFunctionCoveragePercent),
            ('{0}%' -f $row.canonicalCachePercent),
            $row.remainingUncachedFragments,
            $row.selectedFunctions,
            ('{0}%' -f $row.fullFunctionDecompilePercent)
        )
    }
}
else {
    'No export directories were found.'
}

$fullFunctionBacklogTable = if ($exportCoverage.Count -gt 0) {
    Get-MarkdownTable -Headers @('Scope', 'Functions', 'Named', 'Selected', 'Unselected', 'Selected %') -Rows @(
        [pscustomobject]@{
            Scope = 'Default targets'
            Functions = $totalFunctions
            Named = $totalDurableLabels
            Selected = $totalSelectedFunctions
            Unselected = $totalUnselectedFunctions
            SelectedPercent = Get-Percent -Numerator $totalSelectedFunctions -Denominator $totalFunctions
        }
    ) -Selector {
        param($row)
        @(
            $row.Scope,
            $row.Functions,
            $row.Named,
            $row.Selected,
            $row.Unselected,
            ('{0}%' -f $row.SelectedPercent)
        )
    }
}
else {
    'No export directories were found.'
}

$opcodeTableRows = @(
    [pscustomobject]@{ Direction = 'Client'; Total = $clientCoverage.Count; Implemented = $clientHandled; Partial = $clientModelOnly; Missing = $clientEnumOnly },
    [pscustomobject]@{ Direction = 'Server'; Total = $serverCoverage.Count; Implemented = $serverNamedModeled; Partial = $serverPlaceholderModeled; Missing = $serverEnumOnly },
    [pscustomobject]@{ Direction = 'Core'; Total = $coreCoverage.Count; Implemented = @($coreCoverage | Where-Object { $_.coverageStatus -eq 'core_modeled' }).Count; Partial = 0; Missing = @($coreCoverage | Where-Object { $_.coverageStatus -eq 'core_enum_only' }).Count }
)

$opcodeTable = Get-MarkdownTable -Headers @('Direction', 'Total', 'Implemented', 'Partial', 'Missing') -Rows $opcodeTableRows -Selector {
    param($row)
    @($row.Direction, $row.Total, $row.Implemented, $row.Partial, $row.Missing)
}

$missingClientModelTable = if ($missingClientModels.Count -gt 0) {
    Get-MarkdownTable -Headers @('Opcode', 'Hex', 'Comment') -Rows $missingClientModels -Selector {
        param($row)
        @("``$($row.opcode)``", $row.hex, $row.comment)
    }
}
else {
    'None.'
}

$missingClientHandlerTable = if ($missingClientHandlers.Count -gt 0) {
    Get-MarkdownTable -Headers @('Opcode', 'Hex', 'Model', 'Comment') -Rows $missingClientHandlers -Selector {
        param($row)
        @("``$($row.opcode)``", $row.hex, "``$($row.modelTypes)``", $row.comment)
    }
}
else {
    'None.'
}

$missingServerModelTable = if ($missingServerModels.Count -gt 0) {
    Get-MarkdownTable -Headers @('Opcode', 'Hex', 'Comment') -Rows $missingServerModels -Selector {
        param($row)
        @("``$($row.opcode)``", $row.hex, $row.comment)
    }
}
else {
    'None.'
}

$placeholderTable = if ($placeholderQueue.Count -gt 0) {
    Get-MarkdownTable -Headers @('Opcode', 'Hex', 'Model', 'Comment') -Rows $placeholderQueue -Selector {
        param($row)
        @("``$($row.opcode)``", $row.hex, "``$($row.modelTypes)``", $row.comment)
    }
}
else {
    'None.'
}

$markdown = @(
    '# Decomp Coverage Snapshot',
    '',
    '## Export Coverage',
    '',
    $exportTable,
    '',
    '## Full Function Backlog',
    '',
    $fullFunctionBacklogTable,
    '',
    '## Opcode Coverage',
    '',
    $opcodeTable,
    '',
    '## Queue: Client Opcodes Missing Models',
    '',
    $missingClientModelTable,
    '',
    '## Queue: Client Opcodes Missing Handlers',
    '',
    $missingClientHandlerTable,
    '',
    '## Queue: Server Opcodes Missing Models',
    '',
    $missingServerModelTable,
    '',
    '## Queue: Placeholder Models To Decode',
    '',
    $placeholderTable,
    '',
    'Generated artifacts:',
    '',
    ('- ``{0}``' -f (Get-RelativeRepoPath -Path $markdownPath)),
    ('- ``{0}``' -f (Get-RelativeRepoPath -Path $jsonPath)),
    ('- ``{0}``' -f (Get-RelativeRepoPath -Path $exportInventoryPath)),
    ('- ``{0}``' -f (Get-RelativeRepoPath -Path $opcodeInventoryPath))
) -join [Environment]::NewLine

$markdown | Out-File -LiteralPath $markdownPath -Encoding utf8

[pscustomobject]@{
    summaryPath = $jsonPath
    markdownPath = $markdownPath
    exportInventoryPath = $exportInventoryPath
    opcodeInventoryPath = $opcodeInventoryPath
    exportTargetCount = $exportCoverage.Count
    totalOpcodes = $opcodeCoverage.Count
}
