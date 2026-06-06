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
    $OutputPath = Join-Path $ArtifactDirectory ("guild_cdb_{0}.log" -f (Get-Date -Format 'yyyyMMdd_HHmmss'))
}

if ([string]::IsNullOrWhiteSpace($CommandPath)) {
    $CommandPath = Join-Path $ArtifactDirectory 'guild_cdb_commands.txt'
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
        Offset = 0x584840
        Name = 'RecruitmentGuild_SendClientGetDetailedGuildInfo_076E'
        Command = 'r; u @rip L12; db @r8 L40; dq @rsp L10; k'
    },
    @{
        Offset = 0x69e5c0
        Name = 'Lua_GameRecruitmentGuild_GetDetailedGuildInfo'
        Command = 'r; u @rip L12; dq @rsp L10; k'
    },
    @{
        Offset = 0x3991b0
        Name = 'GuildBossToken_SendClientCastGuildBossToken_094F'
        Command = 'r; u @rip L12; db @r8 L40; dq @rsp L10; k'
    },
    @{
        Offset = 0x092580
        Name = 'ServerGuildBankInventoryAdd_ReadPayload_046F'
        Command = 'r; u @rip L10; dq @rsp L10; k'
    },
    @{
        Offset = 0x092470
        Name = 'ServerGuildBankTabInventory_ReadPayload_047A'
        Command = 'r; u @rip L10; dq @rsp L10; k'
    },
    @{
        Offset = 0x57cdc0
        Name = 'GuildBank_ApplyTabInventoryRows_047A'
        Command = 'r; u @rip L10; dq @rsp L10; k'
    },
    @{
        Offset = 0x57d190
        Name = 'GuildBank_ApplyInventoryAdd_046F'
        Command = 'r; u @rip L10; dq @rsp L10; k'
    },
    @{
        Offset = 0x4277d0
        Name = 'GuildBank_DispatchItemAddedClientEvent'
        Command = 'r; u @rip L10; dq @rsp L10; k'
    },
    @{
        Offset = 0x49e9e0
        Name = 'Prerequisite_CheckGuildMembership_183'
        Command = 'r; dd @rsi L4; dq @rdx L6; k'
    },
    @{
        Offset = 0x49ea30
        Name = 'Prerequisite_CheckGuildMembershipOnTarget_184'
        Command = 'r; dd @rsi L4; dq @rdx L6; k'
    },
    @{
        Offset = 0x49eae0
        Name = 'Prerequisite_CheckGuildPerk_185'
        Command = 'r; dd @rsi L4; dq @rdx L6; k'
    }
)

$guildOpcodeCondition = '((@edx == 0x010c) + (@edx == 0x0471) + (@edx == 0x0473) + (@edx == 0x0477) + (@edx == 0x0481) + (@edx == 0x04a6) + (@edx == 0x04a8) + (@edx == 0x04b1) + (@edx == 0x04b2) + (@edx == 0x04b3) + (@edx == 0x04cf) + (@edx == 0x076e) + (@edx == 0x076f) + (@edx == 0x0956))'

$commands = New-Object System.Collections.Generic.List[string]
$commands.Add('sxi 406d1388')
$commands.Add('.echo Nexus guild CDB probe attached')
$commands.Add('.echo Trigger guild holomark, guild bank tab/items/money, recruitment details/subscribe, and war-party boss-token UI if available. Press Ctrl+C and enter qd to detach cleanly.')

foreach ($breakpoint in $breakpoints) {
    $address = Format-BreakpointAddress -Offset ([UInt64]$breakpoint.Offset)
    $commands.Add(('bp {0} ".echo HIT {1}; {2}; gc"' -f $address, $breakpoint.Name, $breakpoint.Command))
}

$sendHelperAddress = Format-BreakpointAddress -Offset 0x3f4900
$commands.Add(('bp {0} ".if {1} {{ .echo HIT Network_SendOpcodePayloadHelper_GuildOpcode; r; db @r8 L40; dq @rsp L10; k; gc }} .else {{ gc }}"' -f $sendHelperAddress, $guildOpcodeCondition))

$sendOrPackedHelperAddress = Format-BreakpointAddress -Offset 0x3f4740
$commands.Add(('bp {0} ".if {1} {{ .echo HIT Network_SendOpcodePayloadOrPackedHelper_GuildOpcode; r; db @r8 L40; dq @rsp L10; k; gc }} .else {{ gc }}"' -f $sendOrPackedHelperAddress, $guildOpcodeCondition))
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
Write-Host 'Trigger these UI actions while CDB is attached: guild holomark, guild bank tab open/item or money moves, recruitment details/subscribe, and war-party boss tokens if available.'
Write-Host 'If you need a guild first, use chat: !guild register Guild Nexus'
Write-Host 'When done, press Ctrl+C in this console and enter qd to detach without killing the client.'

& $cdb.Source -p $wildStar.Id -logo $OutputPath -cf $CommandPath
