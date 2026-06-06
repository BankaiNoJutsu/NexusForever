param(
    [string] $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path,
    [string] $ArtifactDirectory = '',
    [string] $ConnectorPath = 'I:\WildStar\Client64\NexusForever.ClientConnector.exe',
    [string] $OutputPath = '',
    [string] $CommandPath = ''
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($ArtifactDirectory)) {
    $ArtifactDirectory = Join-Path $RepoRoot 'artifacts\live-prereq-debug'
}

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $ArtifactDirectory ("group_raid_matching_cdb_{0}.log" -f (Get-Date -Format 'yyyyMMdd_HHmmss'))
}

if ([string]::IsNullOrWhiteSpace($CommandPath)) {
    $CommandPath = Join-Path $ArtifactDirectory 'group_raid_matching_cdb_commands.txt'
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

$breakpoints = @(
    @{
        Offset = 0x76c830
        Name = 'Matching_QueueDispatchFromUi'
        Command = 'r; u @rip L12; dq @rsp L10; k'
    },
    @{
        Offset = 0x98a70
        Name = 'ClientMatchingQueue_WritePayload_05EF_05F3'
        Command = 'r; u @rip L8; dq @rsp L10; k'
    },
    @{
        Offset = 0x98c00
        Name = 'ClientMatchingQueueRandom_WritePayload_05F8_05F9'
        Command = 'r; u @rip L8; dq @rsp L10; k'
    },
    @{
        Offset = 0x98e10
        Name = 'ClientMatchingRoleCheckResponse_WritePayload_05B2'
        Command = 'r; u @rip L8; dq @rsp L10; k'
    },
    @{
        Offset = 0x76a9f0
        Name = 'Matching_SendMatchLeave_05DA'
        Command = 'r; u @rip L10; dq @rsp L10; k'
    },
    @{
        Offset = 0x76aa30
        Name = 'MatchingReplacement_SendStartLookingForReplacements_05D5'
        Command = 'r; u @rip L10; dq @rsp L10; k'
    },
    @{
        Offset = 0x76abb0
        Name = 'MatchingReplacement_SendStopLookingForReplacements_0602'
        Command = 'r; u @rip L10; dq @rsp L10; k'
    },
    @{
        Offset = 0x76ac30
        Name = 'Matching_SendTransferIntoMatch_0606'
        Command = 'r; u @rip L10; dq @rsp L10; k'
    },
    @{
        Offset = 0x5c3500
        Name = 'MatchingManager_SendClientGameReadyResponse_05C8'
        Command = 'r; u @rip L10; dq @rsp L10; k'
    },
    @{
        Offset = 0x743f30
        Name = 'Lua_GroupLib_SetInstanceDifficulty_0412'
        Command = 'r; u @rip L12; dq @rsp L10; k'
    },
    @{
        Offset = 0x743ff0
        Name = 'Lua_GroupLib_SetLootRules'
        Command = 'r; u @rip L12; dq @rsp L10; k'
    },
    @{
        Offset = 0x744610
        Name = 'Lua_GroupLib_GotoGroupInstance'
        Command = 'r; u @rip L12; dq @rsp L10; k'
    },
    @{
        Offset = 0x8bf80
        Name = 'ServerRaidQueueStatus_ReadPayload_0718'
        Command = 'r; u @rip L10; dq @rsp L10; k'
    },
    @{
        Offset = 0x8c010
        Name = 'ServerRaidInfoResponse_ReadPayload_071A'
        Command = 'r; u @rip L10; dq @rsp L10; k'
    },
    @{
        Offset = 0x6042b0
        Name = 'Group_DispatchRaidInfoResponse'
        Command = 'r; u @rip L10; dq @rsp L10; k'
    },
    @{
        Offset = 0x5c41c0
        Name = 'MatchingManager_ApplyManagerUInt32Field0xA0_05CF_candidate'
        Command = 'r; u @rip L10; dq @rsp L10; k'
    },
    @{
        Offset = 0x5c0e90
        Name = 'MatchingManager_ApplyMatchingRoleCheckStarted_candidate'
        Command = 'r; u @rip L10; dq @rsp L10; k'
    },
    @{
        Offset = 0x5c39f0
        Name = 'MatchingManager_ApplyGameReady_candidate'
        Command = 'r; u @rip L10; dq @rsp L10; k'
    },
    @{
        Offset = 0x5c3af0
        Name = 'MatchingManager_ApplyGameReadyPending_candidate'
        Command = 'r; u @rip L10; dq @rsp L10; k'
    },
    @{
        Offset = 0x5c1cb0
        Name = 'MatchingManager_ApplyMatchLookingForReplacements_candidate'
        Command = 'r; u @rip L10; dq @rsp L10; k'
    },
    @{
        Offset = 0x5c1fc0
        Name = 'MatchingManager_ApplyMatchStoppedLookingForReplacements_candidate'
        Command = 'r; u @rip L10; dq @rsp L10; k'
    }
)

$commands = New-Object System.Collections.Generic.List[string]
$commands.Add('sxi 406d1388')
$commands.Add('.echo Nexus group/raid/matching CDB probe attached')
$commands.Add('.echo Trigger group finder queue/leave, raid info UI, and group settings if available. Press Ctrl+C and enter qd to detach cleanly.')

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
Write-Host 'Trigger these UI actions while CDB is attached: group finder queue/leave, raid info UI, and group settings if available.'
Write-Host 'When done, press Ctrl+C in this console and enter qd to detach without killing the client.'

& $cdb.Source -p $wildStar.Id -logo $OutputPath -cf $CommandPath
