#requires -Version 5.1
<#
.SYNOPSIS
Rebuilds Auth and World, reuses the rest of the local runtime, and launches WildStar.

.DESCRIPTION
Use this for the fast local auth/world edit loop after the repo has already been
initialized once. The script:

* rebuilds NexusForever.AuthServer and NexusForever.WorldServer
* kills any running NexusForever.AuthServer and NexusForever.WorldServer processes before rebuilding
* reuses other running local server processes and starts any missing ones
* launches the WildStar client through the existing local launcher flow only when the client is not already running

.EXAMPLE
.\Tools\Setup\Restart-NexusForeverAuthWorldLocal.ps1 -ClientDirectory "D:\Games\WildStar"
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
    [string] $LogLevel = 'Trace',

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
    [string] $ClientLanguage = 'en',
    [string] $AuthHost = '127.0.0.1',
    [string] $PatcherHost = '',
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

function Write-Section {
    param([string] $Message)

    Write-Host ''
    Write-Host "== $Message ==" -ForegroundColor Cyan
}

function Write-Info {
    param([string] $Message)

    Write-Host $Message -ForegroundColor Gray
}

function Invoke-DotNet {
    param([string[]] $Arguments)

    $output = & dotnet @Arguments 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet $($Arguments -join ' ') failed with exit code $LASTEXITCODE.`n$($output -join [Environment]::NewLine)"
    }

    if ($output) {
        Write-Info ($output -join [Environment]::NewLine)
    }
}

function Get-RunningProcessesByPath {
    param([string] $ExecutablePath)

    $processName = [System.IO.Path]::GetFileName($ExecutablePath)
    $runningProcessIds = @(Get-CimInstance Win32_Process -Filter "Name = '$processName'" -ErrorAction SilentlyContinue | Where-Object {
        $_.ExecutablePath -and [string]::Equals($_.ExecutablePath, $ExecutablePath, [System.StringComparison]::OrdinalIgnoreCase)
    } | Select-Object -ExpandProperty ProcessId)

    $runningProcesses = foreach ($processId in $runningProcessIds) {
        $process = Get-Process -Id $processId -ErrorAction SilentlyContinue
        if ($process) {
            $process
        }
    }

    @($runningProcesses)
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

function Stop-RunningProcessByPath {
    param(
        [string] $ExecutablePath,
        [string] $DisplayName
    )

    foreach ($process in @(Get-RunningProcessesByPath -ExecutablePath $ExecutablePath)) {
        Write-Info "Stopping existing $DisplayName process $($process.Id)"
        Stop-Process -Id $process.Id -Force -ErrorAction Stop

        $deadline = [DateTime]::UtcNow.AddSeconds(10)
        while ([DateTime]::UtcNow -lt $deadline) {
            if (!(Get-Process -Id $process.Id -ErrorAction SilentlyContinue)) {
                break
            }

            Start-Sleep -Milliseconds 250
        }
    }
}

function Invoke-ProjectBuild {
    param([string] $ProjectPath)

    Invoke-DotNet -Arguments @('build', $ProjectPath, '--configuration', $Configuration, '--framework', $TargetFramework)
}

$RepoRoot = [System.IO.Path]::GetFullPath($RepoRoot)
$launcherScript = Join-Path $RepoRoot 'Tools\Setup\Start-NexusForeverLocal.ps1'
if (!(Test-Path -LiteralPath $launcherScript -PathType Leaf)) {
    throw "Local launcher script was not found: $launcherScript"
}

if (!(Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw 'Required command ''dotnet'' was not found. Install the .NET SDK required by this repository.'
}

$authServerExecutable = Join-Path $RepoRoot "Source\NexusForever.AuthServer\bin\$Configuration\$TargetFramework\NexusForever.AuthServer.exe"
$worldServerExecutable = Join-Path $RepoRoot "Source\NexusForever.WorldServer\bin\$Configuration\$TargetFramework\NexusForever.WorldServer.exe"

Write-Section 'Process restart prep'
Stop-RunningProcessByPath -ExecutablePath $authServerExecutable -DisplayName 'NexusForever Auth'
Stop-RunningProcessByPath -ExecutablePath $worldServerExecutable -DisplayName 'NexusForever World'

Write-Section 'Build'
Invoke-ProjectBuild -ProjectPath (Join-Path $RepoRoot 'Source\NexusForever.AuthServer\NexusForever.AuthServer.csproj')
Invoke-ProjectBuild -ProjectPath (Join-Path $RepoRoot 'Source\NexusForever.WorldServer\NexusForever.WorldServer.csproj')

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
    RestartAuthWorldOnly                = $true
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
