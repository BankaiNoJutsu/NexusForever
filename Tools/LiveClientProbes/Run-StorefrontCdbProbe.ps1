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
    $OutputPath = Join-Path $ArtifactDirectory ("storefront_purchase_history_cdb_{0}.log" -f (Get-Date -Format 'yyyyMMdd_HHmmss'))
}

if ([string]::IsNullOrWhiteSpace($CommandPath)) {
    $CommandPath = Join-Path $ArtifactDirectory 'storefront_purchase_history_cdb_commands.txt'
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
        Offset = 0x4f1d50
        Name = 'StorefrontLib_RequestHistory_082E'
        Command = 'r; u @rip L12; dq @rsp L8; k'
    },
    @{
        Offset = 0x4f1d6b
        Name = 'StorefrontLib_RequestHistory_HelperCall_082E'
        Command = 'r; db @r8 L8; dq @rsp L8; k'
    }
)

$commands = New-Object System.Collections.Generic.List[string]
$commands.Add('sxi 406d1388')
$commands.Add('.echo Nexus storefront purchase-history 0x082E CDB probe attached')
$commands.Add('.echo Trigger StorefrontLib.RequestHistory or open purchase history once, then press Ctrl+C and enter qd to detach cleanly.')

foreach ($breakpoint in $breakpoints) {
    $address = Format-BreakpointAddress -Offset ([UInt64]$breakpoint.Offset)
    $commands.Add(('bp {0} ".echo HIT {1}; {2}; gc"' -f $address, $breakpoint.Name, $breakpoint.Command))
}

$helperAddress = Format-BreakpointAddress -Offset 0x0161d0
$commands.Add(('bp {0} ".if (@edx == 0x82e) {{ .echo HIT AccountItem_SendOpcodePayloadHelper_082E_RequestHistory; r; db @r8 L8; k; gc }} .else {{ gc }}"' -f $helperAddress))
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
Write-Host 'Trigger StorefrontLib.RequestHistory or open purchase history once while CDB is attached.'
Write-Host 'When done, press Ctrl+C in this console and enter qd to detach without killing the client.'

& $cdb.Source -p $wildStar.Id -logo $OutputPath -cf $CommandPath
