#requires -Version 5.1
<#
.SYNOPSIS
Stops running NexusForever processes and launches the local restart flow.

.DESCRIPTION
Use this for the fast local edit loop after the repo has already been
initialized once. The script:

* kills any running NexusForever.* processes before the delegated launcher build
* delegates the single solution build and runtime preparation to Start-NexusForeverLocal.ps1
* starts the local standalone server processes
* launches the WildStar client through the existing local launcher flow only when the client is not already running

.EXAMPLE
.\Tools\Setup\Restart-NexusForeverLocal.ps1 -ClientDirectory "D:\Games\WildStar"
#>

[Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSAvoidUsingPlainTextForPassword', '')]
[CmdletBinding()]
param(
    [string] $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path,

    [ValidateSet('Auto', 'ExternalOnly', 'PortableDocker')]
    [string] $DependencyMode = 'Auto',
    [string] $DockerCli = 'docker',
    [string] $PortableMariaDbImage = 'mariadb:11.8',
    [string] $PortableRabbitMqImage = 'rabbitmq:4.1-management',

    [string] $Configuration = 'Debug',
    [string] $TargetFramework = 'net10.0',

    [ValidateSet('Trace', 'Debug', 'Info', 'Warn', 'Error', 'Fatal', 'Off')]
    [string] $LogLevel = 'Info',

    [string] $ClientDirectory = '',
    [string] $ClientExecutable = '',
    [string] $PatchDirectory = '',
    [string] $ExistingTableDirectory = '',
    [string] $ExistingMapDirectory = '',
    [string] $AssetRoot = '',
    [int] $MapGeneratorParallelism = 0,
    [switch] $RegenerateAssets,

    [switch] $SkipServerLaunch,
    [switch] $SkipClientLaunch,
    [switch] $OverwriteConfig,

    [string[]] $ClientArguments = @(),
    [switch] $EnableClientConsole,
    [switch] $EnableClientLogging,
    [ValidateSet('Error', 'Warn', 'Info', 'Debug', 'Trace')]
    [string] $ClientLogLevel = 'Trace',
    [switch] $ClientLogStdout,
    [string] $ClientLogDir = '',
    [string] $ClientLanguage = 'en',
    [string] $AuthHost = '127.0.0.1',
    [string] $PatcherHost = '',
    [int] $RealmDataCenterId = 6,
    [ValidateSet('None', 'LooseData', 'HostsRedirect', 'Both')]
    [string] $StoreBannerMode = 'LooseData',
    [switch] $SkipLocalStoreBannerData,
    [int] $WaitTimeoutSeconds = 120,

    [string] $MySqlExe = 'mysql',
    [string] $MySqlHost = '127.0.0.1',
    [int] $MySqlPort = 3306,
    [string] $RootUser = 'root',
    [string] $RootPassword = $env:MYSQL_PWD,
    [switch] $PromptForRootPassword,

    [string] $DatabaseUser = 'nexusforever',
    [string] $DatabasePassword = 'nexusforever',
    [switch] $SkipDatabaseUser,

    [string] $BrokerUser = 'nexusforever',
    [string] $BrokerPassword = 'nexusforever',
    [string] $BrokerHost = 'localhost',
    [int] $BrokerPort = 5672,
    [string] $RabbitMqCtl = 'rabbitmqctl',
    [switch] $SkipRabbitMqUser,

    [string] $WorldDatabasePath = '',
    [switch] $SkipWorldDatabaseImport,
    [switch] $CreateWorldDatabaseCompatibilityTables,
    [string] $RuntimeWorldSeedPath = '',
    [switch] $SkipRuntimeWorldSeedImport,

    [Alias('DefaultAccountUsername')]
    [string] $PlayerAccountUsername = 'player',
    [Alias('DefaultAccountPassword')]
    [string] $PlayerAccountPassword = 'player',
    [string] $GameMasterAccountUsername = 'gm',
    [string] $GameMasterAccountPassword = 'gm',
    [string] $AdministratorAccountUsername = 'admin',
    [string] $AdministratorAccountPassword = 'admin',

    [switch] $InstallDotNetEf
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (($StoreBannerMode -eq 'HostsRedirect' -or $StoreBannerMode -eq 'Both') -and !$PSBoundParameters.ContainsKey('RealmDataCenterId')) {
    $RealmDataCenterId = 9
}

function Write-Section {
    param([string] $Message)

    Write-Host ''
    Write-Host "== $Message ==" -ForegroundColor Cyan
}

function Write-Info {
    param([string] $Message)

    Write-Host $Message -ForegroundColor Gray
}

function Get-RunningWildStarProcesses {
    $runningProcessIds = @(
        Get-CimInstance Win32_Process -Filter "Name = 'WildStar64.exe' OR Name = 'WildStar32.exe'" -ErrorAction SilentlyContinue |
        Select-Object -ExpandProperty ProcessId
    )

    $runningProcesses = foreach ($processId in $runningProcessIds) {
        $process = Get-Process -Id $processId -ErrorAction SilentlyContinue
        if ($process) {
            $process
        }
    }

    @($runningProcesses)
}

function Get-RunningNexusForeverProcesses {
    $runningProcessInfos = @(
        Get-CimInstance Win32_Process -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -like 'NexusForever.*.exe' } |
        Sort-Object Name, ProcessId
    )

    $runningProcesses = foreach ($processInfo in $runningProcessInfos) {
        $process = Get-Process -Id $processInfo.ProcessId -ErrorAction SilentlyContinue
        if ($process) {
            [pscustomobject]@{
                Name           = $processInfo.Name
                ProcessId      = $processInfo.ProcessId
                ExecutablePath = $processInfo.ExecutablePath
                Process        = $process
            }
        }
    }

    @($runningProcesses)
}

function Stop-RunningNexusForeverProcesses {
    $runningProcesses = @(Get-RunningNexusForeverProcesses)
    if ($runningProcesses.Count -eq 0) {
        Write-Info 'No running NexusForever.* processes found.'
        return
    }

    foreach ($entry in $runningProcesses) {
        if ([string]::IsNullOrWhiteSpace($entry.ExecutablePath)) {
            Write-Info "Stopping existing $($entry.Name) process $($entry.ProcessId)"
        }
        else {
            Write-Info "Stopping existing $($entry.Name) process $($entry.ProcessId): $($entry.ExecutablePath)"
        }

        Stop-Process -Id $entry.ProcessId -Force -ErrorAction Stop

        $deadline = [DateTime]::UtcNow.AddSeconds(10)
        while ([DateTime]::UtcNow -lt $deadline) {
            if (!(Get-Process -Id $entry.ProcessId -ErrorAction SilentlyContinue)) {
                break
            }

            Start-Sleep -Milliseconds 250
        }
    }
}

$RepoRoot = [System.IO.Path]::GetFullPath($RepoRoot)
$launcherScript = Join-Path $RepoRoot 'Tools\Setup\Start-NexusForeverLocal.ps1'
if (!(Test-Path -LiteralPath $launcherScript -PathType Leaf)) {
    throw "Local launcher script was not found: $launcherScript"
}

Write-Section 'Process restart prep'
Stop-RunningNexusForeverProcesses

$skipClientLaunchBecauseRunning = $false
if (!$SkipClientLaunch) {
    $runningWildStarProcesses = @(Get-RunningWildStarProcesses)
    if ($runningWildStarProcesses.Count -gt 0) {
        Write-Info "Skipping WildStar launch because the client is already running: $(@($runningWildStarProcesses | ForEach-Object Id) -join ', ')"
        $skipClientLaunchBecauseRunning = $true
    }
}

$launcherParameters = @{
    RepoRoot                            = $RepoRoot
    DependencyMode                      = $DependencyMode
    DockerCli                           = $DockerCli
    PortableMariaDbImage                = $PortableMariaDbImage
    PortableRabbitMqImage               = $PortableRabbitMqImage
    Configuration                       = $Configuration
    TargetFramework                     = $TargetFramework
    LogLevel                            = $LogLevel
    ClientDirectory                     = $ClientDirectory
    ClientExecutable                    = $ClientExecutable
    PatchDirectory                      = $PatchDirectory
    ExistingTableDirectory              = $ExistingTableDirectory
    ExistingMapDirectory                = $ExistingMapDirectory
    AssetRoot                           = $AssetRoot
    MapGeneratorParallelism             = $MapGeneratorParallelism
    ClientLanguage                      = $ClientLanguage
    AuthHost                            = $AuthHost
    RealmDataCenterId                   = $RealmDataCenterId
    StoreBannerMode                     = $StoreBannerMode
    SkipLocalStoreBannerData            = $SkipLocalStoreBannerData
    WaitTimeoutSeconds                  = $WaitTimeoutSeconds
    MySqlExe                            = $MySqlExe
    MySqlHost                           = $MySqlHost
    MySqlPort                           = $MySqlPort
    RootUser                            = $RootUser
    RootPassword                        = $RootPassword
    DatabaseUser                        = $DatabaseUser
    DatabasePassword                    = $DatabasePassword
    BrokerUser                          = $BrokerUser
    BrokerPassword                      = $BrokerPassword
    BrokerHost                          = $BrokerHost
    BrokerPort                          = $BrokerPort
    RabbitMqCtl                         = $RabbitMqCtl
    WorldDatabasePath                   = $WorldDatabasePath
    RuntimeWorldSeedPath                = $RuntimeWorldSeedPath
    PlayerAccountUsername               = $PlayerAccountUsername
    PlayerAccountPassword               = $PlayerAccountPassword
    GameMasterAccountUsername           = $GameMasterAccountUsername
    GameMasterAccountPassword           = $GameMasterAccountPassword
    AdministratorAccountUsername        = $AdministratorAccountUsername
    AdministratorAccountPassword        = $AdministratorAccountPassword
    SkipSetup                           = $true
}

if ($PSBoundParameters.ContainsKey('ClientArguments')) {
    $launcherParameters.ClientArguments = $ClientArguments
}

if (![string]::IsNullOrWhiteSpace($PatcherHost)) {
    $launcherParameters.PatcherHost = $PatcherHost
}

if ($RegenerateAssets) {
    $launcherParameters.RegenerateAssets = $true
}

if ($SkipServerLaunch) {
    $launcherParameters.SkipServerLaunch = $true
}

if ($SkipClientLaunch -or $skipClientLaunchBecauseRunning) {
    $launcherParameters.SkipClientLaunch = $true
}

if ($OverwriteConfig) {
    $launcherParameters.OverwriteConfig = $true
}

if ($EnableClientConsole) {
    $launcherParameters.EnableClientConsole = $true
}

if ($EnableClientLogging) {
    $launcherParameters.EnableClientLogging = $true
}

if ($PSBoundParameters.ContainsKey('ClientLogLevel')) {
    $launcherParameters.ClientLogLevel = $ClientLogLevel
}

if ($ClientLogStdout) {
    $launcherParameters.ClientLogStdout = $true
}

if ($PSBoundParameters.ContainsKey('ClientLogDir')) {
    $launcherParameters.ClientLogDir = $ClientLogDir
}

if ($PromptForRootPassword) {
    $launcherParameters.PromptForRootPassword = $true
}

if ($SkipDatabaseUser) {
    $launcherParameters.SkipDatabaseUser = $true
}

if ($SkipRabbitMqUser) {
    $launcherParameters.SkipRabbitMqUser = $true
}

if ($SkipWorldDatabaseImport) {
    $launcherParameters.SkipWorldDatabaseImport = $true
}

if ($CreateWorldDatabaseCompatibilityTables) {
    $launcherParameters.CreateWorldDatabaseCompatibilityTables = $true
}

if ($SkipRuntimeWorldSeedImport) {
    $launcherParameters.SkipRuntimeWorldSeedImport = $true
}

if ($InstallDotNetEf) {
    $launcherParameters.InstallDotNetEf = $true
}

& $launcherScript @launcherParameters
