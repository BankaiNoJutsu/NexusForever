[CmdletBinding()]
param(
    [string] $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path,

    [Parameter(Mandatory = $true)]
    [string] $PatchPath,

    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Debug',

    [int] $Parallelism = 0,

    [string] $OutputRoot = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-FullPath {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Path
    )

    return [System.IO.Path]::GetFullPath($Path)
}

function Assert-ChildPath {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Parent,

        [Parameter(Mandatory = $true)]
        [string] $Child
    )

    $parentFull = (Get-FullPath $Parent).TrimEnd(@([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar))
    $childFull = Get-FullPath $Child
    $expectedPrefix = $parentFull + [System.IO.Path]::DirectorySeparatorChar

    if (-not $childFull.StartsWith($expectedPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to operate on '$childFull' because it is outside '$parentFull'."
    }
}

$repoRootPath = (Resolve-Path -LiteralPath $RepoRoot).Path
$patchRootPath = (Resolve-Path -LiteralPath $PatchPath).Path

if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $repoRootPath '.nexusforever-runtime\benchmarks\map-generator'
}

$outputRootPath = Get-FullPath $OutputRoot
New-Item -ItemType Directory -Path $outputRootPath -Force | Out-Null

$timestamp = Get-Date -Format 'yyyyMMdd_HHmmss'
$benchmarkRoot = Join-Path $outputRootPath $timestamp
Assert-ChildPath -Parent $outputRootPath -Child $benchmarkRoot
New-Item -ItemType Directory -Path $benchmarkRoot -Force | Out-Null

$projectPath = Join-Path $repoRootPath 'Source\NexusForever.MapGenerator\NexusForever.MapGenerator.csproj'
$executablePath = Join-Path $repoRootPath "Source\NexusForever.MapGenerator\bin\$Configuration\net10.0\NexusForever.MapGenerator.exe"

Write-Host "Building NexusForever.MapGenerator ($Configuration)..."
dotnet build $projectPath --configuration $Configuration --nologo -v minimal
if ($LASTEXITCODE -ne 0) {
    throw "MapGenerator build failed with exit code $LASTEXITCODE."
}

if (-not (Test-Path -LiteralPath $executablePath)) {
    throw "MapGenerator executable was not found: $executablePath"
}

function Invoke-MapGeneratorRun {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Name,

        [string[]] $ExtraArguments = @()
    )

    $runRoot = Join-Path $benchmarkRoot $Name
    Assert-ChildPath -Parent $benchmarkRoot -Child $runRoot

    if (Test-Path -LiteralPath $runRoot) {
        Remove-Item -LiteralPath $runRoot -Recurse -Force
    }

    New-Item -ItemType Directory -Path $runRoot -Force | Out-Null

    $logPath = Join-Path $benchmarkRoot "$Name.log"
    $arguments = @(
        '--patchPath', $patchRootPath,
        '--output', $runRoot,
        '--extract',
        '--generate'
    ) + $ExtraArguments

    Write-Host "Running $Name..."
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    & $executablePath @arguments *> $logPath
    $exitCode = $LASTEXITCODE
    $stopwatch.Stop()

    if ($exitCode -ne 0) {
        throw "MapGenerator run '$Name' failed with exit code $exitCode. See $logPath"
    }

    $mapPath = Join-Path $runRoot 'Map'
    $mapFiles = if (Test-Path -LiteralPath $mapPath) {
        @(Get-ChildItem -LiteralPath $mapPath -Filter '*.nfmap' -File -Recurse)
    } else {
        @()
    }

    [pscustomobject]@{
        Name        = $Name
        Seconds     = [Math]::Round($stopwatch.Elapsed.TotalSeconds, 3)
        MapFiles    = $mapFiles.Count
        Output      = $runRoot
        Log         = $logPath
        Arguments   = ($arguments -join ' ')
    }
}

$results = @()
$results += Invoke-MapGeneratorRun -Name 'single' -ExtraArguments @('--maxParallelism', '1')

if ($Parallelism -gt 0) {
    $parallelArguments = @('--maxParallelism', $Parallelism.ToString([System.Globalization.CultureInfo]::InvariantCulture))
    $parallelName = "parallel-$Parallelism"
} else {
    $parallelArguments = @()
    $parallelName = 'parallel-auto'
}

$results += Invoke-MapGeneratorRun -Name $parallelName -ExtraArguments $parallelArguments

$reportPath = Join-Path $benchmarkRoot 'summary.json'
$results | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $reportPath -Encoding UTF8

$results | Format-Table Name, Seconds, MapFiles, Output, Log -AutoSize
Write-Host "Summary: $reportPath"
