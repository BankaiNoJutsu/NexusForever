#requires -Version 5.1
<#
.SYNOPSIS
Starts the local auth/world stack for blocker evidence passes with Trace logging.

.DESCRIPTION
Thin wrapper around Restart-NexusForeverAuthWorldLocal.ps1 that applies the defaults
from Decomp/Analysis/BLOCKER_EVIDENCE_PLAN.md and prints log-tail commands for the
evidence bundle workflow.

.EXAMPLE
.\Decomp\Analysis\Start-BlockerEvidenceHarness.ps1 -ClientDirectory "I:\WildStar" -PromptForRootPassword
#>

[CmdletBinding()]
param(
    [string] $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path,
    [string] $ClientDirectory = 'I:\WildStar',
    [string] $BundleName = 'blocker-evidence',
    [string] $OutputRoot = '',
    [string[]] $PlayerIds = @(),
    [int[]] $WorldIds = @(),
    [int[]] $PublicEventIds = @(),
    [int[]] $ObjectiveIds = @(),
    [string[]] $NegativeCases = @(),
    [string] $Notes = '',
    [ValidateSet('Trace', 'Debug', 'Info', 'Warn', 'Error', 'Fatal', 'Off')]
    [string] $LogLevel = 'Trace',
    [switch] $PromptForRootPassword,
    [switch] $CreateBundleOnly,
    [switch] $SkipServerLaunch,
    [switch] $SkipClientLaunch
)

$restartScript = Join-Path $RepoRoot 'Tools\Setup\Restart-NexusForeverAuthWorldLocal.ps1'
if (-not (Test-Path -LiteralPath $restartScript)) {
    throw "Restart script not found: $restartScript"
}

$timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$safeBundleName = ($BundleName -replace '[^A-Za-z0-9_.-]+', '-').Trim('-')
if ([string]::IsNullOrWhiteSpace($safeBundleName)) {
    $safeBundleName = 'blocker-evidence'
}

if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $RepoRoot 'artifacts\blocker_evidence'
}

$bundleDirectory = Join-Path $OutputRoot "$timestamp-$safeBundleName"
$screenshotsDirectory = Join-Path $bundleDirectory 'screenshots'
$videoDirectory = Join-Path $bundleDirectory 'video'
$logsDirectory = Join-Path $bundleDirectory 'logs'
New-Item -ItemType Directory -Force -Path $bundleDirectory, $screenshotsDirectory, $videoDirectory, $logsDirectory | Out-Null

$relativePlanPath = 'Decomp/Analysis/BLOCKER_EVIDENCE_PLAN.md'
$manifest = [ordered]@{
    createdAt       = (Get-Date).ToString('o')
    bundleName      = $safeBundleName
    repoRoot        = $RepoRoot
    clientDirectory = $ClientDirectory
    logLevel        = $LogLevel
    plan            = $relativePlanPath
    playerIds       = $PlayerIds
    worldIds        = $WorldIds
    publicEventIds  = $PublicEventIds
    objectiveIds    = $ObjectiveIds
    negativeCases   = $NegativeCases
    notes           = $Notes
    requiredFiles   = @(
        'manifest.json',
        'commands.md',
        'server-log-notes.md',
        'client-observations.md',
        'negative-cases.md',
        'screenshots/',
        'video/',
        'logs/'
    )
}
$manifest | ConvertTo-Json -Depth 5 | Set-Content -Path (Join-Path $bundleDirectory 'manifest.json') -Encoding UTF8

$commandsTemplate = @"
# Command And Action Transcript

Bundle: $safeBundleName
Created: $timestamp

Record every command, UI action, and client-visible result in order.

| Time | Client/account | Command or action | Expected result | Observed result | Evidence file |
| --- | --- | --- | --- | --- | --- |
| | | `!help` | | | |

Baseline commands:

~~~text
!help
!help teleport
!character level 50
!currency character add Credits 10000000
!character save
!teleport name Thayd
!teleport name Illium
!teleport coordinates <x> <y> <z> [worldId]
~~~
"@
$commandsTemplate | Set-Content -Path (Join-Path $bundleDirectory 'commands.md') -Encoding UTF8

$serverLogTemplate = @"
# Server Log Notes

Copy relevant log excerpts into `logs/` and summarize the exact matching lines
here. Include player/request ids, world ids, public event ids, objective ids,
entity ids, spell ids, packet names, and rejection reasons where applicable.

| Time | Log file | Component | Matching line or summary | Related command/action |
| --- | --- | --- | --- | --- |
| | | | | |
"@
$serverLogTemplate | Set-Content -Path (Join-Path $bundleDirectory 'server-log-notes.md') -Encoding UTF8

$clientTemplate = @"
# Client Observations

Attach screenshots in `screenshots/` and video in `video/`. Reference file names
from the table below.

| Time | Client/account | World/coordinates | UI or world result | Screenshot/video |
| --- | --- | --- | --- | --- |
| | | | | |
"@
$clientTemplate | Set-Content -Path (Join-Path $bundleDirectory 'client-observations.md') -Encoding UTF8

$negativeCaseText = @"
# Negative Cases

Record at least one negative/rejection case for the blocker under test.

Configured negative cases:
$($NegativeCases | ForEach-Object { "- $_" } | Out-String)

| Time | Case | Command/action | Expected rejection | Observed rejection | Evidence file |
| --- | --- | --- | --- | --- | --- |
| | | | | | |
"@
$negativeCaseText | Set-Content -Path (Join-Path $bundleDirectory 'negative-cases.md') -Encoding UTF8

$tailScript = @"
`$RepoRoot = '$RepoRoot'
Get-Content -Wait -Tail 200 (Join-Path `$RepoRoot '.nexusforever-runtime\logs\NexusForever_World.stdout.log')
# Additional useful tails:
# Get-Content -Wait -Tail 200 (Join-Path `$RepoRoot 'Source\NexusForever.WorldServer\bin\Debug\net10.0\logs\NexusForever.WorldServer_*.log')
# Get-Content -Wait -Tail 200 (Join-Path `$RepoRoot '.nexusforever-runtime\logs\NexusForever_Group.stdout.log')
# Get-Content -Wait -Tail 200 (Join-Path `$RepoRoot '.nexusforever-runtime\logs\NexusForever_Friendship.stdout.log')
"@
$tailScript | Set-Content -Path (Join-Path $bundleDirectory 'Tail-BlockerEvidenceLogs.ps1') -Encoding UTF8

$collectScript = @"
param(
    [string] `$RepoRoot = '$RepoRoot',
    [string] `$BundleDirectory = '$bundleDirectory'
)

`$ErrorActionPreference = 'Stop'
`$logsDirectory = Join-Path `$BundleDirectory 'logs'
New-Item -ItemType Directory -Force -Path `$logsDirectory | Out-Null

`$logPatterns = @(
    '.nexusforever-runtime\logs\NexusForever_World.stdout.log',
    '.nexusforever-runtime\logs\NexusForever_Group.stdout.log',
    '.nexusforever-runtime\logs\NexusForever_Friendship.stdout.log',
    'Source\NexusForever.WorldServer\bin\Debug\net10.0\logs\NexusForever.WorldServer_*.log'
)

foreach (`$pattern in `$logPatterns) {
    Get-ChildItem -Path (Join-Path `$RepoRoot `$pattern) -ErrorAction SilentlyContinue |
        ForEach-Object {
            Copy-Item -LiteralPath `$_.FullName -Destination (Join-Path `$logsDirectory `$_.Name) -Force
        }
}

Write-Host "Collected logs into `$logsDirectory"
"@
$collectScript | Set-Content -Path (Join-Path $bundleDirectory 'Collect-BlockerEvidenceBundle.ps1') -Encoding UTF8

Write-Host 'Starting blocker evidence harness (Trace logging, client console enabled).' -ForegroundColor Cyan
Write-Host "Plan: Decomp/Analysis/BLOCKER_EVIDENCE_PLAN.md" -ForegroundColor DarkGray
Write-Host "Evidence bundle: $bundleDirectory" -ForegroundColor Green

if ($CreateBundleOnly) {
    Write-Host 'CreateBundleOnly was supplied; skipping server/client launch.' -ForegroundColor Yellow
} else {
    & $restartScript `
        -RepoRoot $RepoRoot `
        -ClientDirectory $ClientDirectory `
        -EnableClientConsole `
        -LogLevel $LogLevel `
        -PromptForRootPassword:$PromptForRootPassword `
        -SkipServerLaunch:$SkipServerLaunch `
        -SkipClientLaunch:$SkipClientLaunch
}

Write-Host ''
Write-Host 'Evidence log tails (run in separate terminals):' -ForegroundColor Yellow
Write-Host "  Get-Content -Wait -Tail 200 $(Join-Path $RepoRoot '.nexusforever-runtime\logs\NexusForever_World.stdout.log')"
Write-Host "  Get-Content -Wait -Tail 200 $(Join-Path $RepoRoot 'Source\NexusForever.WorldServer\bin\Debug\net10.0\logs\NexusForever.WorldServer_*.log')"
Write-Host "  Get-Content -Wait -Tail 200 $(Join-Path $RepoRoot '.nexusforever-runtime\logs\NexusForever_Group.stdout.log')"
Write-Host "  Get-Content -Wait -Tail 200 $(Join-Path $RepoRoot '.nexusforever-runtime\logs\NexusForever_Friendship.stdout.log')"
Write-Host ''
Write-Host 'Bundle helpers:' -ForegroundColor Yellow
Write-Host "  $(Join-Path $bundleDirectory 'Tail-BlockerEvidenceLogs.ps1')"
Write-Host "  $(Join-Path $bundleDirectory 'Collect-BlockerEvidenceBundle.ps1')"
