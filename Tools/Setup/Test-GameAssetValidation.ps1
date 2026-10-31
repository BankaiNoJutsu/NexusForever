#requires -Version 5.1
[CmdletBinding()]
param(
    [string] $RepoRoot = '',
    [string] $ExistingTableDirectory = '',
    [string] $Configuration = 'Debug',
    [string] $TargetFramework = 'net10.0'
)

$ErrorActionPreference = 'Stop'
if (!$RepoRoot) { $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path }
$source = Join-Path $PSScriptRoot 'Start-NexusForeverLocal.ps1'
$tokens = $null
$parseErrors = $null
$ast = [System.Management.Automation.Language.Parser]::ParseFile($source, [ref]$tokens, [ref]$parseErrors)
if ($parseErrors.Count) { throw ($parseErrors | Out-String) }

# Import only these pure validation functions, never the launcher's executable
# top-level setup/service/client flow.
foreach ($name in @('Test-GeneratedGameAssets', 'Test-TableAssets', 'Test-MapAssets')) {
    $definition = $ast.Find({ param($node) $node -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq $name }, $true)
    if (!$definition) { throw "Missing function $name" }
    Invoke-Expression $definition.Extent.Text
}

$temporaryParent = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\', '/')
$fixtureRoot = Join-Path $temporaryParent ('nf-asset-setup-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $fixtureRoot | Out-Null
try {
    $tablePath = Join-Path $fixtureRoot 'tbl'
    $mapPath = Join-Path $fixtureRoot 'map'
    New-Item -ItemType Directory -Path $tablePath, $mapPath | Out-Null
    if (Test-TableAssets -Path $tablePath) { throw 'Empty table cache was accepted.' }
    [IO.File]::WriteAllBytes((Join-Path $tablePath 'World.tbl'), [byte[]]@(0, 1, 2))
    if (Test-TableAssets -Path $tablePath) { throw 'World.tbl alone certified an incomplete cache.' }
    if (Test-MapAssets -Path $mapPath) { throw 'Empty map cache was accepted.' }

    foreach ($name in @('Eastern', 'NewCentral', 'NewPlayerExperience', 'Western')) {
        $writer = New-Object IO.BinaryWriter([IO.File]::Create((Join-Path $mapPath ($name + '.nfmap'))))
        try {
            $writer.Write([uint32]0x504D464E)
            $writer.Write([uint32]4)
            $writer.Write([uint32]16042)
            $writer.Write([string]$name)
            $writer.Write([uint32]0)
        }
        finally { $writer.Dispose() }
    }
    if (!(Test-MapAssets -Path $mapPath)) { throw 'Structurally valid required maps were rejected.' }
    $badMap = Join-Path $mapPath 'Western.nfmap'
    $bytes = [IO.File]::ReadAllBytes($badMap)
    $bytes[8] = 0
    [IO.File]::WriteAllBytes($badMap, $bytes)
    if (Test-MapAssets -Path $mapPath) { throw 'Wrong-build map was accepted merely because it exists.' }

    if ($ExistingTableDirectory -and !(Test-TableAssets -Path $ExistingTableDirectory)) {
        throw 'Supplied real runtime tables failed validation.'
    }
    if ($ErrorActionPreference -ne 'Stop') { throw 'Validator changed caller error preferences.' }
    Write-Output 'Game asset setup validation regression smoke passed.'
}
finally {
    $resolvedFixture = [IO.Path]::GetFullPath($fixtureRoot)
    if (!$resolvedFixture.StartsWith($temporaryParent + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) -or
        [IO.Path]::GetFileName($resolvedFixture) -notlike 'nf-asset-setup-*') {
        throw 'Refusing to remove a fixture outside the owned temporary directory.'
    }
    Remove-Item -LiteralPath $resolvedFixture -Recurse -Force
}
