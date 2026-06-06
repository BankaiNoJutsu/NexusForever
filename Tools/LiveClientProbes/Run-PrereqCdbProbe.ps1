param(
    [string] $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path,
    [string] $ArtifactDirectory = '',
    [string] $ConnectorPath = 'I:\WildStar\Client64\NexusForever.ClientConnector.exe',
    [string] $OutputPath = '',
    [string] $CommandPath = '',
    [switch] $AccountItemOnly,
    [switch] $AccountItemAndTradeOnly
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($ArtifactDirectory)) {
    $ArtifactDirectory = Join-Path $RepoRoot 'artifacts\live-prereq-debug'
}

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $ArtifactDirectory ("prereq_probe_cdb_{0}.log" -f (Get-Date -Format 'yyyyMMdd_HHmmss'))
}

if ([string]::IsNullOrWhiteSpace($CommandPath)) {
    $CommandPath = Join-Path $ArtifactDirectory 'prereq_probe_cdb_commands.txt'
}

$cdb = Get-Command cdbX64.exe -ErrorAction Stop

function Get-LiveWildStarProcess {
    Get-Process WildStar64 -ErrorAction SilentlyContinue |
        Where-Object {
            try {
                -not $_.HasExited
            }
            catch {
                $true
            }
        } |
        Sort-Object StartTime -Descending |
        Select-Object -First 1
}

$wildStar = Get-LiveWildStarProcess
if ($null -eq $wildStar) {
    if (!(Test-Path -LiteralPath $ConnectorPath)) {
        throw "WildStar64 is not running and connector was not found: $ConnectorPath"
    }

    Start-Process -FilePath $ConnectorPath -WorkingDirectory (Split-Path -Parent $ConnectorPath) | Out-Null
    Start-Sleep -Seconds 10
    $wildStar = Get-LiveWildStarProcess
}

if ($null -eq $wildStar) {
    throw 'WildStar64 is not running after connector launch.'
}

try {
    $baseAddress = [UInt64]$wildStar.MainModule.BaseAddress.ToInt64()
}
catch {
    $baseAddress = $null
}

function Format-BreakpointAddress {
    param([UInt64] $Offset)

    if ($null -ne $baseAddress) {
        return ('0x{0:x}' -f ($baseAddress + $Offset))
    }

    return ('WildStar64+0x{0:x}' -f $Offset)
}

New-Item -ItemType Directory -Force -Path (Split-Path -Parent $OutputPath) | Out-Null
New-Item -ItemType Directory -Force -Path (Split-Path -Parent $CommandPath) | Out-Null

$accountItemBreakpoints = @(
    @{ Offset = 0x006ba0; Name = 'AccountItem_SendClientClaimPendingItemGroup'; Command = 'r; k' },
    @{ Offset = 0x006d00; Name = 'AccountItem_SendClientAccountItemTake'; Command = 'r; k' },
    @{ Offset = 0x518b70; Name = 'AccountItemUi_ClaimSelectedPendingItemGroup'; Command = 'r; k' },
    @{ Offset = 0x518be0; Name = 'AccountItemUi_TakeSelectedAccountItem'; Command = 'r; k' },
    @{ Offset = 0x4e50f0; Name = 'Lua_AccountItemLib_TakeAccountItem'; Command = 'r; k' }
)

$prerequisiteBreakpoints = @(
    @{ Offset = 0x4a2100; Name = 'PrerequisiteManager_EvaluateTypeSlot'; Command = 'r; dd @r8 L4; k' },
    @{ Offset = 0x4a1790; Name = 'Prerequisite_CheckItemTradeSkill'; Command = 'r; dd @rsi L4; dd @rdx+80 L1; k' },
    @{ Offset = 0x4a17e0; Name = 'Prerequisite_CheckItemTradeSkillKnown'; Command = 'r; dd @rsi L4; dd @rdx+80 L1; k' }
)

$tradeHandlerBreakpoints = @(
    @{ Offset = 0x4a1790; Name = 'Prerequisite_CheckItemTradeSkill'; Command = 'r; dd @rsi L4; dd @rdx+80 L1; k' },
    @{ Offset = 0x4a17e0; Name = 'Prerequisite_CheckItemTradeSkillKnown'; Command = 'r; dd @rsi L4; dd @rdx+80 L1; k' }
)

$breakpoints = if ($AccountItemOnly) {
    $accountItemBreakpoints
}
elseif ($AccountItemAndTradeOnly) {
    $accountItemBreakpoints + $tradeHandlerBreakpoints
}
else {
    $accountItemBreakpoints + $prerequisiteBreakpoints
}

$commands = New-Object System.Collections.Generic.List[string]
$commands.Add('sxi 406d1388')
$commands.Add('.echo Nexus prerequisite CDB probe attached')
if ($AccountItemOnly) {
    $commands.Add('.echo Account-item-only mode: trigger dye and Holo-Wardrobe claim/use paths, then press Ctrl+C and enter qd to detach cleanly.')
}
elseif ($AccountItemAndTradeOnly) {
    $commands.Add('.echo Account-item-and-trade-handler mode: trigger pending claim, account-item take, and dye/Holo-Wardrobe use paths, then press Ctrl+C and enter qd to detach cleanly.')
}
else {
    $commands.Add('.echo Trigger dye and Holo-Wardrobe account-item claim/use paths, then press Ctrl+C and enter qd to detach cleanly.')
}

foreach ($breakpoint in $breakpoints) {
    $address = Format-BreakpointAddress -Offset ([UInt64]$breakpoint.Offset)
    $commands.Add(('bp {0} ".echo HIT {1}; {2}; gc"' -f $address, $breakpoint.Name, $breakpoint.Command))
}

$commands.Add('g')
$commands | Set-Content -LiteralPath $CommandPath -Encoding ASCII

Write-Host "Attaching cdbX64 to WildStar64 PID $($wildStar.Id)."
if ($null -ne $baseAddress) {
    Write-Host ('WildStar64 base address: 0x{0:x}' -f $baseAddress)
}
else {
    Write-Host 'WildStar64 base address was not readable; using module-relative breakpoint expressions.'
}
Write-Host "CDB commands: $CommandPath"
Write-Host "Output: $OutputPath"
if ($AccountItemOnly) {
    Write-Host 'Account-item-only mode: this will not break on the noisy prerequisite dispatcher.'
}
elseif ($AccountItemAndTradeOnly) {
    Write-Host 'Account-item-and-trade-handler mode: this skips the noisy prerequisite dispatcher but keeps the trade alias handlers.'
}
Write-Host 'Trigger a dye account-item claim/use and a Holo-Wardrobe account-item claim/use while CDB is attached.'
Write-Host 'When done, press Ctrl+C in this console and enter qd to detach without killing the client.'

& $cdb.Source -p $wildStar.Id -logo $OutputPath -cf $CommandPath
