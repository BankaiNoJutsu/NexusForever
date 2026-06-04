[CmdletBinding()]
param(
    [string] $ProjectDir = (Join-Path $PSScriptRoot 'ghidra_projects'),
    [string] $OutputDir = (Join-Path $PSScriptRoot 'exports'),
    [string] $SummaryPath = (Join-Path $PSScriptRoot 'logs\LATEST_RUN_SUMMARY.json'),
    [string[]] $Targets = @(),
    [ValidateSet('Text', 'Json', 'Object')]
    [string] $Format = 'Text'
)

$ErrorActionPreference = 'Stop'

function ConvertTo-ProjectToken {
    param(
        [string] $Value
    )

    $token = $Value -replace '[^A-Za-z0-9]+', '_'
    $token = $token.Trim('_')
    if ([string]::IsNullOrWhiteSpace($token)) {
        return 'Target'
    }

    return $token
}

function Read-KeyValuePropertiesFile {
    param(
        [string] $Path
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        return $null
    }

    $properties = [ordered]@{}
    foreach ($line in Get-Content -LiteralPath $Path) {
        $trimmed = $line.Trim()
        if ([string]::IsNullOrWhiteSpace($trimmed) -or $trimmed.StartsWith('#')) {
            continue
        }

        $separator = $trimmed.IndexOf('=')
        if ($separator -lt 0) {
            continue
        }

        $name = $trimmed.Substring(0, $separator).Trim()
        $value = $trimmed.Substring($separator + 1).Trim()
        if (-not [string]::IsNullOrWhiteSpace($name)) {
            $properties[$name] = $value
        }
    }

    return $properties
}

function Read-RunSummary {
    param(
        [string] $Path
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        return $null
    }

    return Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
}

function Add-Candidate {
    param(
        [System.Collections.IDictionary] $Candidates,
        [string] $ProjectName,
        [string] $Target,
        [string] $Source,
        [string] $ProjectLayout = '',
        [string] $BinaryPath = '',
        [string] $ExportDir = '',
        [string] $LogPath = '',
        [string] $AnalysisManifestPath = ''
    )

    if ([string]::IsNullOrWhiteSpace($ProjectName)) {
        return
    }

    $candidateTarget = if ([string]::IsNullOrWhiteSpace($Target)) { '' } else { $Target }
    $key = '{0}|{1}' -f $ProjectName, $candidateTarget
    if (-not $Candidates.Contains($key)) {
        $Candidates[$key] = [ordered]@{
            projectName = $ProjectName
            target = $candidateTarget
            source = $Source
            projectLayout = $ProjectLayout
            binaryPath = $BinaryPath
            exportDir = $ExportDir
            logPath = $LogPath
            analysisManifestPath = $AnalysisManifestPath
        }
        return
    }

    $candidate = $Candidates[$key]
    foreach ($property in @('target', 'projectLayout', 'binaryPath', 'exportDir', 'logPath', 'analysisManifestPath')) {
        if ([string]::IsNullOrWhiteSpace([string]$candidate[$property]) -and -not [string]::IsNullOrWhiteSpace((Get-Variable -Name $property -ValueOnly -ErrorAction SilentlyContinue))) {
            $candidate[$property] = Get-Variable -Name $property -ValueOnly
        }
    }

    if ($candidate.source -notlike "*$Source*") {
        $candidate.source = '{0}; {1}' -f $candidate.source, $Source
    }
}

function Test-TargetFilterMatch {
    param(
        [System.Collections.IDictionary] $Candidate,
        [string[]] $TargetFilter
    )

    if ($TargetFilter.Count -eq 0) {
        return $true
    }

    foreach ($target in $TargetFilter) {
        if ([string]::IsNullOrWhiteSpace($target)) {
            continue
        }

        $targetToken = ConvertTo-ProjectToken -Value ([IO.Path]::GetFileNameWithoutExtension($target))
        if ($Candidate.target -eq $target -or
            [IO.Path]::GetFileNameWithoutExtension($Candidate.target) -eq [IO.Path]::GetFileNameWithoutExtension($target) -or
            $Candidate.projectName -eq $target -or
            $Candidate.projectName.EndsWith("_$targetToken")) {
            return $true
        }
    }

    return $false
}

function Resolve-Candidate {
    param(
        [System.Collections.IDictionary] $Candidate,
        [string] $ResolvedProjectDir,
        [string] $ResolvedOutputDir
    )

    $target = [string]$Candidate.target
    $projectName = [string]$Candidate.projectName
    $exportDir = if ([string]::IsNullOrWhiteSpace([string]$Candidate.exportDir) -and -not [string]::IsNullOrWhiteSpace($target)) {
        Join-Path $ResolvedOutputDir $target
    }
    else {
        [string]$Candidate.exportDir
    }

    $projectFile = Join-Path $ResolvedProjectDir ($projectName + '.gpr')
    $projectRepository = Join-Path $ResolvedProjectDir ($projectName + '.rep')
    $labelMap = Join-Path $PSScriptRoot 'function_labels.csv'
    $promptAdapter = Join-Path $PSScriptRoot 'GHIDRA_MCP_PROMPT_ADAPTER.md'

    [pscustomobject]@{
        target = $target
        programName = $target
        projectName = $projectName
        projectLayout = [string]$Candidate.projectLayout
        projectFile = $projectFile
        projectFileExists = Test-Path -LiteralPath $projectFile -PathType Leaf
        projectRepository = $projectRepository
        projectRepositoryExists = Test-Path -LiteralPath $projectRepository
        mcpProjectQuery = $projectName
        binaryPath = [string]$Candidate.binaryPath
        exportDir = $exportDir
        functionsCsv = if ($exportDir) { Join-Path $exportDir 'functions.csv' } else { '' }
        selectedReasonsCsv = if ($exportDir) { Join-Path $exportDir 'selected_reasons_summary.csv' } else { '' }
        selectedCallEdgesCsv = if ($exportDir) { Join-Path $exportDir 'selected_call_edges.csv' } else { '' }
        selectedDecompiled = if ($exportDir) { Join-Path $exportDir 'selected_decompiled.c' } else { '' }
        functionPointerFamiliesCsv = if ($exportDir) { Join-Path $exportDir 'function_pointer_families.csv' } else { '' }
        labelMap = $labelMap
        promptAdapter = $promptAdapter
        analysisManifestPath = [string]$Candidate.analysisManifestPath
        latestLogPath = [string]$Candidate.logPath
        source = [string]$Candidate.source
        mcpSequence = @(
            'Open the .gpr project in Ghidra and open the listed program in CodeBrowser.',
            'Call mcp__ghidra_mcp.list_instances and confirm the project is visible.',
            ('Call mcp__ghidra_mcp.connect_instance with project="{0}".' -f $projectName),
            'Call mcp__ghidra_mcp.list_tool_groups, then load the function/datatype groups if the bridge is lazy-loaded.',
            'Use MCP for focused inspection, comments, and tentative labels; mirror durable labels into function_labels.csv.',
            'Re-export the affected binary and verify labels in functions.csv and selected_decompiled.c.'
        )
    }
}

$resolvedProjectDir = if (Test-Path -LiteralPath $ProjectDir) {
    (Resolve-Path -LiteralPath $ProjectDir).Path
}
else {
    $ProjectDir
}

$resolvedOutputDir = if (Test-Path -LiteralPath $OutputDir) {
    (Resolve-Path -LiteralPath $OutputDir).Path
}
else {
    $OutputDir
}

$candidates = [ordered]@{}
$runSummary = Read-RunSummary -Path $SummaryPath
if ($null -ne $runSummary -and $null -ne $runSummary.targets) {
    foreach ($targetSummary in @($runSummary.targets)) {
        Add-Candidate `
            -Candidates $candidates `
            -ProjectName ([string]$targetSummary.projectName) `
            -Target ([string]$targetSummary.target) `
            -Source 'latest-run-summary' `
            -ProjectLayout ([string]$runSummary.projectLayout) `
            -BinaryPath ([string]$targetSummary.binaryPath) `
            -ExportDir ([string]$targetSummary.exportDir) `
            -LogPath ([string]$targetSummary.logPath) `
            -AnalysisManifestPath ([string]$targetSummary.analysisManifestPath)
    }
}

$manifestDir = Join-Path $resolvedProjectDir '.analysis_manifests'
if (Test-Path -LiteralPath $manifestDir -PathType Container) {
    foreach ($manifest in Get-ChildItem -LiteralPath $manifestDir -Filter '*.analysis.properties' -File) {
        $properties = Read-KeyValuePropertiesFile -Path $manifest.FullName
        if ($null -eq $properties) {
            continue
        }

        Add-Candidate `
            -Candidates $candidates `
            -ProjectName ([string]$properties['project.name']) `
            -Target ([string]$properties['target']) `
            -Source 'analysis-manifest' `
            -ProjectLayout ([string]$properties['project.layout']) `
            -AnalysisManifestPath $manifest.FullName
    }
}

if (Test-Path -LiteralPath $resolvedProjectDir -PathType Container) {
    foreach ($projectFile in Get-ChildItem -LiteralPath $resolvedProjectDir -Filter '*.gpr' -File) {
        $projectName = [IO.Path]::GetFileNameWithoutExtension($projectFile.Name)
        $hasResolvedProjectTarget = $false
        foreach ($key in $candidates.Keys) {
            if ($key.StartsWith("$projectName|") -and -not $key.EndsWith('|')) {
                $hasResolvedProjectTarget = $true
                break
            }
        }

        if ($hasResolvedProjectTarget) {
            continue
        }

        Add-Candidate `
            -Candidates $candidates `
            -ProjectName $projectName `
            -Target '' `
            -Source 'project-file'
    }
}

$resolvedCandidates = @(
    foreach ($candidate in $candidates.Values) {
        if (Test-TargetFilterMatch -Candidate $candidate -TargetFilter $Targets) {
            Resolve-Candidate -Candidate $candidate -ResolvedProjectDir $resolvedProjectDir -ResolvedOutputDir $resolvedOutputDir
        }
    }
) | Sort-Object projectName, target

if ($Format -eq 'Json') {
    [pscustomobject]@{
        summaryPath = $SummaryPath
        summaryExists = Test-Path -LiteralPath $SummaryPath -PathType Leaf
        projectDir = $resolvedProjectDir
        outputDir = $resolvedOutputDir
        candidateCount = $resolvedCandidates.Count
        candidates = $resolvedCandidates
    } | ConvertTo-Json -Depth 6
    return
}

if ($Format -eq 'Object') {
    $resolvedCandidates
    return
}

Write-Host 'Ghidra MCP workflow hints'
Write-Host ('Summary: {0} ({1})' -f $SummaryPath, $(if (Test-Path -LiteralPath $SummaryPath -PathType Leaf) { 'found' } else { 'missing' }))
Write-Host ('Project directory: {0}' -f $resolvedProjectDir)
Write-Host ('Prompt adapter: {0}' -f (Join-Path $PSScriptRoot 'GHIDRA_MCP_PROMPT_ADAPTER.md'))
Write-Host ''

if ($resolvedCandidates.Count -eq 0) {
    Write-Host 'No Ghidra projects were found. Seed a per-target project first, for example:'
    Write-Host '.\Decomp\Analysis\run_ghidra_analysis.ps1 -Targets WildStar64.exe -ProjectLayout PerTarget'
    return
}

foreach ($candidate in $resolvedCandidates) {
    $displayTarget = if ([string]::IsNullOrWhiteSpace($candidate.target)) { '(unknown target)' } else { $candidate.target }
    Write-Host ("Target: {0}" -f $displayTarget)
    Write-Host ("  Project: {0}" -f $candidate.projectName)
    Write-Host ("  Program: {0}" -f $(if ($candidate.programName) { $candidate.programName } else { '(open matching program manually)' }))
    Write-Host ("  Ghidra project: {0}" -f $candidate.projectFile)
    Write-Host ("  Project exists: {0}" -f ($candidate.projectFileExists -or $candidate.projectRepositoryExists))
    Write-Host ("  MCP connect query: {0}" -f $candidate.mcpProjectQuery)
    Write-Host ("  Prompt adapter: {0}" -f $candidate.promptAdapter)
    if (-not [string]::IsNullOrWhiteSpace($candidate.exportDir)) {
        Write-Host ("  Export folder: {0}" -f $candidate.exportDir)
        Write-Host ("  Evidence CSVs: functions.csv, selected_reasons_summary.csv, selected_call_edges.csv")
    }
    if (-not [string]::IsNullOrWhiteSpace($candidate.latestLogPath)) {
        Write-Host ("  Latest Ghidra log: {0}" -f $candidate.latestLogPath)
    }

    Write-Host '  MCP sequence:'
    foreach ($step in $candidate.mcpSequence) {
        Write-Host ("    - {0}" -f $step)
    }
    Write-Host ''
}
