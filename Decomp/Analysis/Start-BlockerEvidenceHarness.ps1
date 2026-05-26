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
    [ValidateSet('Trace', 'Debug', 'Info', 'Warn', 'Error', 'Fatal', 'Off')]
    [string] $LogLevel = 'Trace',
    [switch] $PromptForRootPassword,
    [switch] $SkipServerLaunch,
    [switch] $SkipClientLaunch
)

$restartScript = Join-Path $RepoRoot 'Tools\Setup\Restart-NexusForeverAuthWorldLocal.ps1'
if (-not (Test-Path -LiteralPath $restartScript)) {
    throw "Restart script not found: $restartScript"
}

Write-Host 'Starting blocker evidence harness (Trace logging, client console enabled).' -ForegroundColor Cyan
Write-Host "Plan: Decomp/Analysis/BLOCKER_EVIDENCE_PLAN.md" -ForegroundColor DarkGray

& $restartScript `
    -RepoRoot $RepoRoot `
    -ClientDirectory $ClientDirectory `
    -EnableClientConsole `
    -LogLevel $LogLevel `
    -PromptForRootPassword:$PromptForRootPassword `
    -SkipServerLaunch:$SkipServerLaunch `
    -SkipClientLaunch:$SkipClientLaunch

Write-Host ''
Write-Host 'Evidence log tails (run in separate terminals):' -ForegroundColor Yellow
Write-Host "  Get-Content -Wait -Tail 200 $(Join-Path $RepoRoot '.nexusforever-runtime\logs\NexusForever_World.stdout.log')"
Write-Host "  Get-Content -Wait -Tail 200 $(Join-Path $RepoRoot 'Source\NexusForever.WorldServer\bin\Debug\net10.0\logs\NexusForever.WorldServer_*.log')"
Write-Host "  Get-Content -Wait -Tail 200 $(Join-Path $RepoRoot '.nexusforever-runtime\logs\NexusForever_Group.stdout.log')"
Write-Host "  Get-Content -Wait -Tail 200 $(Join-Path $RepoRoot '.nexusforever-runtime\logs\NexusForever_Friendship.stdout.log')"
