#requires -Version 5.1
<#+
.SYNOPSIS
Builds NexusForever, prepares runtime assets, starts local servers, and launches the WildStar client.

.DESCRIPTION
This script wraps Initialize-NexusForever.ps1 so one command can:

* create or update the local MySQL and RabbitMQ setup used by NexusForever
* build the full solution and copy runtime configuration files
* import the optional world database through the existing setup workflow
* extract WildStar game tables and generate maps with NexusForever.MapGenerator
* rewrite the runtime JSON files to use shared absolute asset paths
* create or verify a local login account
* start the standalone server processes and wait for the local endpoints
* launch WildStar64.exe against localhost for immediate testing

By default, generated runtime assets are staged under
.nexusforever-runtime\assets in the repo root. Pass -ExistingTableDirectory and
-ExistingMapDirectory to reuse assets you already generated elsewhere.

.EXAMPLE
.\Tools\Setup\Start-NexusForeverLocal.ps1 -ClientDirectory "D:\Games\WildStar" -PromptForRootPassword

.EXAMPLE
.\Tools\Setup\Start-NexusForeverLocal.ps1 `
  -ExistingTableDirectory "D:\WildStarAssets\tbl" `
  -ExistingMapDirectory "D:\WildStarAssets\map" `
  -ClientDirectory "D:\Games\WildStar" `
  -SkipSetup
#>

[Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSAvoidUsingPlainTextForPassword', '')]
[CmdletBinding()]
param(
    [string] $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path,

    [string] $Configuration = 'Debug',
    [string] $TargetFramework = 'net10.0',

    [string] $ClientDirectory = '',
    [string] $ClientExecutable = '',
    [string] $PatchDirectory = '',
    [string] $ExistingTableDirectory = '',
    [string] $ExistingMapDirectory = '',
    [string] $AssetRoot = '',
    [switch] $RegenerateAssets,

    [switch] $SkipSetup,
    [switch] $SkipServerLaunch,
    [switch] $SkipClientLaunch,
    [switch] $OverwriteConfig,
    [bool] $RestartExistingServers = $true,

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

    [string] $DefaultAccountUsername = 'nexusforever',
    [string] $DefaultAccountPassword = 'nexusforever',

    [switch] $InstallDotNetEf
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$GameDatabases = [ordered]@{
    Auth       = 'nexus_forever_auth'
    Character  = 'nexus_forever_character'
    World      = 'nexus_forever_world'
    Group      = 'nexus_forever_group'
    Chat       = 'nexus_forever_chat'
    Friendship = 'nexus_forever_friendship'
}

$RuntimeConfigs = @(
    @{ Project = 'NexusForever.AuthServer';         File = 'AuthServer.json' },
    @{ Project = 'NexusForever.StsServer';          File = 'StsServer.json' },
    @{ Project = 'NexusForever.API.Account';        File = 'AccountAPI.json' },
    @{ Project = 'NexusForever.API.Character';      File = 'CharacterAPI.json' },
    @{ Project = 'NexusForever.Server.ChatServer';  File = 'ChatServer.json' },
    @{ Project = 'NexusForever.Server.GroupServer'; File = 'GroupServer.json' },
    @{ Project = 'NexusForever.Server.Friendship';  File = 'FriendshipServer.json' },
    @{ Project = 'NexusForever.Server.Character';   File = 'CharacterServer.json' },
    @{ Project = 'NexusForever.WorldServer';        File = 'WorldServer.json' }
)

$ServerProcesses = @(
    @{ Project = 'NexusForever.AuthServer';         Exe = 'NexusForever.AuthServer.exe';         Name = 'NexusForever Auth';        Port = 23115 },
    @{ Project = 'NexusForever.StsServer';          Exe = 'NexusForever.StsServer.exe';          Name = 'NexusForever STS';         Port = 6600 },
    @{ Project = 'NexusForever.API.Account';        Exe = 'NexusForever.API.Account.exe';        Name = 'NexusForever Account API'; Port = 4001 },
    @{ Project = 'NexusForever.API.Character';      Exe = 'NexusForever.API.Character.exe';      Name = 'NexusForever Character API'; Port = 4000 },
    @{ Project = 'NexusForever.Server.ChatServer';  Exe = 'NexusForever.Server.ChatServer.exe';  Name = 'NexusForever Chat';        Port = $null },
    @{ Project = 'NexusForever.Server.GroupServer'; Exe = 'NexusForever.Server.GroupServer.exe'; Name = 'NexusForever Group';       Port = $null },
    @{ Project = 'NexusForever.Server.Friendship';  Exe = 'NexusForever.Server.Friendship.exe';  Name = 'NexusForever Friendship';  Port = $null },
    @{ Project = 'NexusForever.Server.Character';   Exe = 'NexusForever.Server.Character.exe';   Name = 'NexusForever Character';   Port = $null },
    @{ Project = 'NexusForever.WorldServer';        Exe = 'NexusForever.WorldServer.exe';        Name = 'NexusForever World';       Port = 24000 }
)

function Write-Section {
    param([string] $Message)

    Write-Host ''
    Write-Host "== $Message ==" -ForegroundColor Cyan
}

function Write-Info {
    param([string] $Message)

    Write-Host $Message -ForegroundColor Gray
}

function Get-AbsolutePath {
    param(
        [string] $Path,
        [switch] $AllowMissing
    )

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return ''
    }

    $fullPath = [System.IO.Path]::GetFullPath($Path)
    if ($AllowMissing) {
        return $fullPath
    }

    if (!(Test-Path -LiteralPath $fullPath)) {
        throw "Path does not exist: $fullPath"
    }

    (Resolve-Path -LiteralPath $fullPath).Path
}

function Resolve-ClientExecutablePath {
    if (![string]::IsNullOrWhiteSpace($ClientExecutable)) {
        $resolvedClientExecutable = Get-AbsolutePath -Path $ClientExecutable
        if (!(Test-Path -LiteralPath $resolvedClientExecutable -PathType Leaf)) {
            throw "WildStar client executable was not found: $resolvedClientExecutable"
        }

        return $resolvedClientExecutable
    }

    $candidateRoots = @()
    if (![string]::IsNullOrWhiteSpace($ClientDirectory)) {
        $candidateRoots += Get-AbsolutePath -Path $ClientDirectory
    }
    elseif (![string]::IsNullOrWhiteSpace($PatchDirectory)) {
        $candidateRoots += Split-Path -Parent (Get-AbsolutePath -Path $PatchDirectory)
    }

    foreach ($root in $candidateRoots | Select-Object -Unique) {
        foreach ($candidate in @('WildStar64.exe', 'WildStar32.exe')) {
            $candidatePath = Join-Path $root $candidate
            if (Test-Path -LiteralPath $candidatePath -PathType Leaf) {
                return (Resolve-Path -LiteralPath $candidatePath).Path
            }
        }
    }

    throw 'WildStar client executable was not found. Pass -ClientDirectory or -ClientExecutable.'
}

function Resolve-PatchDirectoryPath {
    if (![string]::IsNullOrWhiteSpace($PatchDirectory)) {
        $resolvedPatchDirectory = Get-AbsolutePath -Path $PatchDirectory
        if (!(Test-Path -LiteralPath $resolvedPatchDirectory -PathType Container)) {
            throw "WildStar patch directory was not found: $resolvedPatchDirectory"
        }

        return $resolvedPatchDirectory
    }

    $candidateRoots = @()
    if (![string]::IsNullOrWhiteSpace($ClientDirectory)) {
        $candidateRoots += Get-AbsolutePath -Path $ClientDirectory
    }
    elseif (![string]::IsNullOrWhiteSpace($ClientExecutable)) {
        $candidateRoots += Split-Path -Parent (Get-AbsolutePath -Path $ClientExecutable)
    }

    foreach ($root in $candidateRoots | Select-Object -Unique) {
        $candidate = Join-Path $root 'Patch'
        if (Test-Path -LiteralPath $candidate -PathType Container) {
            return (Resolve-Path -LiteralPath $candidate).Path
        }
    }

    throw 'WildStar Patch directory was not found. Pass -PatchDirectory or -ClientDirectory.'
}

function Get-DatabaseConnectionString {
    param([string] $Database)

    "server=$MySqlHost;port=$MySqlPort;user=$DatabaseUser;password=$DatabasePassword;database=$Database"
}

function Get-BrokerConnectionString {
    "amqp://${BrokerUser}:${BrokerPassword}@${BrokerHost}:${BrokerPort}"
}

function Invoke-ExternalCommand {
    param(
        [string] $FilePath,
        [string[]] $Arguments = @(),
        [string] $WorkingDirectory = ''
    )

    if (!(Test-Path -LiteralPath $FilePath -PathType Leaf)) {
        throw "Command file was not found: $FilePath"
    }

    if ([string]::IsNullOrWhiteSpace($WorkingDirectory)) {
        $WorkingDirectory = Split-Path -Parent $FilePath
    }

    Push-Location $WorkingDirectory
    try {
        $commandLine = @($FilePath) + $Arguments
        Write-Info "Running: $($commandLine -join ' ')"
        & $FilePath @Arguments
        if ($LASTEXITCODE -ne 0) {
            throw "$([System.IO.Path]::GetFileName($FilePath)) failed with exit code $LASTEXITCODE."
        }
    }
    finally {
        Pop-Location
    }
}

function Invoke-WithTemporaryEnvironment {
    param(
        [hashtable] $Variables,
        [scriptblock] $ScriptBlock
    )

    $originalValues = @{}
    foreach ($key in $Variables.Keys) {
        if (Test-Path -LiteralPath "Env:$key") {
            $originalValues[$key] = (Get-Item -LiteralPath "Env:$key").Value
        }
        else {
            $originalValues[$key] = $null
        }

        Set-Item -LiteralPath "Env:$key" -Value $Variables[$key]
    }

    try {
        & $ScriptBlock
    }
    finally {
        foreach ($key in $Variables.Keys) {
            if ($null -eq $originalValues[$key]) {
                Remove-Item -LiteralPath "Env:$key" -ErrorAction SilentlyContinue
            }
            else {
                Set-Item -LiteralPath "Env:$key" -Value $originalValues[$key]
            }
        }
    }
}

function Update-ConnectionStringsInJsonNode {
    param([AllowNull()][object] $Node)

    if ($null -eq $Node) {
        return
    }

    if ($Node -is [System.Collections.IEnumerable] -and !($Node -is [string])) {
        foreach ($item in $Node) {
            Update-ConnectionStringsInJsonNode -Node $item
        }

        return
    }

    foreach ($property in $Node.PSObject.Properties) {
        if ($property.Name -eq 'ConnectionString' -and $property.Value -is [string]) {
            $value = [string] $property.Value
            if ($value -match '(?i)database=([^;]+)') {
                $database = $Matches[1]
                if ($GameDatabases.Values -contains $database) {
                    $property.Value = Get-DatabaseConnectionString -Database $database
                }
            }
            elseif ($value -like 'amqp://*') {
                $property.Value = Get-BrokerConnectionString
            }
        }
        else {
            Update-ConnectionStringsInJsonNode -Node $property.Value
        }
    }
}

function Update-RuntimeConfigFiles {
    param(
        [string] $GameTablePath,
        [string] $MapPath
    )

    $accountApiUrl = 'http://localhost:4001'
    $characterApiUrl = 'http://localhost:4000'

    foreach ($item in $RuntimeConfigs) {
        $configPath = Join-Path $RepoRoot "Source\$($item.Project)\bin\$Configuration\$TargetFramework\$($item.File)"
        if (!(Test-Path -LiteralPath $configPath -PathType Leaf)) {
            throw "Runtime config was not found: $configPath"
        }

        $json = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
        Update-ConnectionStringsInJsonNode -Node $json

        switch ($item.Project) {
            'NexusForever.AuthServer' {
                $json.Network.Host = '0.0.0.0'
                $json.Network.Port = 23115
            }
            'NexusForever.StsServer' {
                $json.Network.Host = '0.0.0.0'
                $json.Network.Port = 6600
            }
            'NexusForever.API.Account' {
                $json.urls = $accountApiUrl
            }
            'NexusForever.API.Character' {
                $json.urls = $characterApiUrl
            }
            'NexusForever.Server.ChatServer' {
                $json.API.Character.Host = $characterApiUrl
                $json.GameTable.GameTablePath = $GameTablePath
            }
            'NexusForever.Server.GroupServer' {
                $json.API.Character.Host = $characterApiUrl
            }
            'NexusForever.Server.Friendship' {
                $json.API.Account.Host = $accountApiUrl
                $json.API.Character.Host = $characterApiUrl
                $json.GameTable.GameTablePath = $GameTablePath
            }
            'NexusForever.Server.Character' {
                $json.API.Character.Host = $characterApiUrl
            }
            'NexusForever.WorldServer' {
                $json.Urls = 'http://0.0.0.0:5000'
                $json.Network.Host = '0.0.0.0'
                $json.Network.Port = 24000
                $json.GameTable.GameTablePath = $GameTablePath
                $json.Realm.Map.MapPath = $MapPath
            }
        }

        $json | ConvertTo-Json -Depth 100 | Set-Content -LiteralPath $configPath -Encoding utf8
        Write-Info "Updated runtime config $configPath"
    }
}

function Test-TableAssets {
    param([string] $Path)

    if (!(Test-Path -LiteralPath $Path -PathType Container)) {
        return $false
    }

    Test-Path -LiteralPath (Join-Path $Path 'World.tbl') -PathType Leaf
}

function Test-MapAssets {
    param([string] $Path)

    if (!(Test-Path -LiteralPath $Path -PathType Container)) {
        return $false
    }

    [bool] (Get-ChildItem -LiteralPath $Path -Filter '*.nfmap' -File -ErrorAction SilentlyContinue | Select-Object -First 1)
}

function Initialize-GameAssets {
    $sharedAssetRoot = if ([string]::IsNullOrWhiteSpace($AssetRoot)) {
        Join-Path $RepoRoot '.nexusforever-runtime\assets'
    }
    else {
        Get-AbsolutePath -Path $AssetRoot -AllowMissing
    }

    $resolvedExistingTableDirectory = if ([string]::IsNullOrWhiteSpace($ExistingTableDirectory)) {
        ''
    }
    else {
        Get-AbsolutePath -Path $ExistingTableDirectory
    }

    $resolvedExistingMapDirectory = if ([string]::IsNullOrWhiteSpace($ExistingMapDirectory)) {
        ''
    }
    else {
        Get-AbsolutePath -Path $ExistingMapDirectory
    }

    if (!(Test-Path -LiteralPath $sharedAssetRoot -PathType Container)) {
        New-Item -ItemType Directory -Path $sharedAssetRoot -Force | Out-Null
    }

    $tablePath = if ($resolvedExistingTableDirectory) {
        $resolvedExistingTableDirectory
    }
    else {
        Join-Path $sharedAssetRoot 'tbl'
    }

    $mapPath = if ($resolvedExistingMapDirectory) {
        $resolvedExistingMapDirectory
    }
    else {
        Join-Path $sharedAssetRoot 'map'
    }

    $needsTableGeneration = !$resolvedExistingTableDirectory -and ($RegenerateAssets -or !(Test-TableAssets -Path $tablePath))
    $needsMapGeneration = !$resolvedExistingMapDirectory -and ($RegenerateAssets -or !(Test-MapAssets -Path $mapPath))

    if ($needsTableGeneration -or $needsMapGeneration) {
        $patchPath = Resolve-PatchDirectoryPath
        $mapGeneratorExe = Join-Path $RepoRoot "Source\NexusForever.MapGenerator\bin\$Configuration\$TargetFramework\NexusForever.MapGenerator.exe"
        $arguments = @('--patchPath', $patchPath, '--output', $sharedAssetRoot)

        if ($needsTableGeneration) {
            $arguments += '--extract'
        }

        if ($needsMapGeneration) {
            $arguments += '--generate'
        }

        Write-Section 'WildStar assets'
        Invoke-ExternalCommand -FilePath $mapGeneratorExe -Arguments $arguments -WorkingDirectory (Split-Path -Parent $mapGeneratorExe)
    }
    else {
        Write-Section 'WildStar assets'
        Write-Info 'Reusing existing game tables and map assets.'
    }

    if (!(Test-TableAssets -Path $tablePath)) {
        throw "Game tables were not found at $tablePath"
    }

    if (!(Test-MapAssets -Path $mapPath)) {
        throw "Map assets were not found at $mapPath"
    }

    [pscustomobject]@{
        TablePath = (Resolve-Path -LiteralPath $tablePath).Path
        MapPath   = (Resolve-Path -LiteralPath $mapPath).Path
    }
}

function Invoke-NexusForeverSetup {
    $setupScript = Join-Path $RepoRoot 'Tools\Setup\Initialize-NexusForever.ps1'
    if (!(Test-Path -LiteralPath $setupScript -PathType Leaf)) {
        throw "Setup script was not found: $setupScript"
    }

    $setupParameters = @{
        RepoRoot               = $RepoRoot
        MySqlExe               = $MySqlExe
        MySqlHost              = $MySqlHost
        MySqlPort              = $MySqlPort
        RootUser               = $RootUser
        RootPassword           = $RootPassword
        DatabaseUser           = $DatabaseUser
        DatabasePassword       = $DatabasePassword
        BrokerUser             = $BrokerUser
        BrokerPassword         = $BrokerPassword
        BrokerHost             = $BrokerHost
        BrokerPort             = $BrokerPort
        RabbitMqCtl            = $RabbitMqCtl
        Configuration          = $Configuration
        TargetFramework        = $TargetFramework
        WorldDatabasePath      = $WorldDatabasePath
        DefaultAccountUsername = $DefaultAccountUsername
        DefaultAccountPassword = $DefaultAccountPassword
    }

    if ($PromptForRootPassword) {
        $setupParameters.PromptForRootPassword = $true
    }

    if ($SkipDatabaseUser) {
        $setupParameters.SkipDatabaseUser = $true
    }

    if ($SkipRabbitMqUser) {
        $setupParameters.SkipRabbitMqUser = $true
    }

    if ($OverwriteConfig) {
        $setupParameters.OverwriteConfig = $true
    }

    if ($SkipWorldDatabaseImport) {
        $setupParameters.SkipWorldDatabaseImport = $true
    }

    if ($CreateWorldDatabaseCompatibilityTables) {
        $setupParameters.CreateWorldDatabaseCompatibilityTables = $true
    }

    if ($InstallDotNetEf) {
        $setupParameters.InstallDotNetEf = $true
    }

    & $setupScript @setupParameters
}

function Invoke-AccountSeeder {
    $migrationExe = Join-Path $RepoRoot "Source\NexusForever.Aspire.Database.Migrations\bin\$Configuration\$TargetFramework\NexusForever.Aspire.Database.Migrations.exe"
    $skippedWorldDatabasePath = Join-Path $RepoRoot '.nexusforever-runtime\skip-world-database'

    if (!(Test-Path -LiteralPath $migrationExe -PathType Leaf)) {
        throw "Database migration executable was not found: $migrationExe"
    }

    $environmentOverrides = @{
        'ConnectionStrings__authdb'       = Get-DatabaseConnectionString -Database $GameDatabases.Auth
        'ConnectionStrings__characterdb'  = Get-DatabaseConnectionString -Database $GameDatabases.Character
        'ConnectionStrings__worlddb'      = Get-DatabaseConnectionString -Database $GameDatabases.World
        'ConnectionStrings__groupdb'      = Get-DatabaseConnectionString -Database $GameDatabases.Group
        'ConnectionStrings__chatdb'       = Get-DatabaseConnectionString -Database $GameDatabases.Chat
        'ConnectionStrings__friendshipdb' = Get-DatabaseConnectionString -Database $GameDatabases.Friendship
        'AccountCreation__UserName'       = $DefaultAccountUsername
        'AccountCreation__Password'       = $DefaultAccountPassword
        'WorldDatabase__Path'             = $skippedWorldDatabasePath
    }

    Write-Section 'Account'
    Invoke-WithTemporaryEnvironment -Variables $environmentOverrides -ScriptBlock {
        Invoke-ExternalCommand -FilePath $migrationExe -WorkingDirectory (Split-Path -Parent $migrationExe)
    }
}

function Stop-RunningProcessByPath {
    param(
        [string] $ExecutablePath,
        [string] $DisplayName
    )

    $processName = [System.IO.Path]::GetFileName($ExecutablePath)
    $runningProcesses = @(Get-CimInstance Win32_Process -Filter "Name = '$processName'" -ErrorAction SilentlyContinue | Where-Object {
        $_.ExecutablePath -and [string]::Equals($_.ExecutablePath, $ExecutablePath, [System.StringComparison]::OrdinalIgnoreCase)
    })

    foreach ($process in $runningProcesses) {
        Write-Info "Stopping existing $DisplayName process $($process.ProcessId)"
        Stop-Process -Id $process.ProcessId -Force -ErrorAction Stop

        $deadline = [DateTime]::UtcNow.AddSeconds(10)
        while ([DateTime]::UtcNow -lt $deadline) {
            if (!(Get-Process -Id $process.ProcessId -ErrorAction SilentlyContinue)) {
                break
            }

            Start-Sleep -Milliseconds 250
        }
    }
}

function Start-StandaloneServers {
    $startedProcesses = @()

    foreach ($server in $ServerProcesses) {
        $workingDirectory = Join-Path $RepoRoot "Source\$($server.Project)\bin\$Configuration\$TargetFramework"
        $exePath = Join-Path $workingDirectory $server.Exe

        if (!(Test-Path -LiteralPath $exePath -PathType Leaf)) {
            throw "Cannot start $($server.Name); executable was not found: $exePath"
        }

        if ($RestartExistingServers) {
            Stop-RunningProcessByPath -ExecutablePath $exePath -DisplayName $server.Name
        }

        Write-Info "Starting $($server.Name)"
        $process = Start-Process -FilePath $exePath -WorkingDirectory $workingDirectory -WindowStyle Hidden -PassThru

        Start-Sleep -Milliseconds 500
        $process.Refresh()
        if ($process.HasExited) {
            throw "$($server.Name) exited immediately with code $($process.ExitCode)."
        }

        $startedProcesses += [pscustomobject]@{
            Name    = $server.Name
            Port    = $server.Port
            Process = $process
        }
    }

    $startedProcesses
}

function Assert-ProcessesAlive {
    param([object[]] $Processes)

    foreach ($entry in $Processes) {
        if ($null -eq $entry.Process) {
            continue
        }

        $entry.Process.Refresh()
        if ($entry.Process.HasExited) {
            throw "$($entry.Name) exited before startup completed with code $($entry.Process.ExitCode)."
        }
    }
}

function Test-TcpEndpoint {
    param(
        [string] $Address,
        [int] $Port,
        [int] $TimeoutMilliseconds = 1000
    )

    $client = New-Object System.Net.Sockets.TcpClient
    $asyncResult = $null

    try {
        $asyncResult = $client.BeginConnect($Address, $Port, $null, $null)
        if (!$asyncResult.AsyncWaitHandle.WaitOne($TimeoutMilliseconds, $false)) {
            return $false
        }

        $client.EndConnect($asyncResult)
        return $true
    }
    catch {
        return $false
    }
    finally {
        if ($asyncResult) {
            $asyncResult.AsyncWaitHandle.Close()
        }

        $client.Close()
    }
}

function Wait-ForTcpEndpoint {
    param(
        [string] $Name,
        [string] $Address,
        [int] $Port,
        [int] $TimeoutSeconds,
        [object[]] $Processes = @()
    )

    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    while ([DateTime]::UtcNow -lt $deadline) {
        Assert-ProcessesAlive -Processes $Processes
        if (Test-TcpEndpoint -Address $Address -Port $Port) {
            Write-Info "${Name} is listening on ${Address}:${Port}"
            return
        }

        Start-Sleep -Milliseconds 500
    }

    Assert-ProcessesAlive -Processes $Processes
    throw "${Name} did not start listening on ${Address}:${Port} within $TimeoutSeconds seconds."
}

function Wait-ForRuntimeReadiness {
    param([object[]] $Processes = @())

    Write-Section 'Server readiness'
    Wait-ForTcpEndpoint -Name 'Auth server' -Address '127.0.0.1' -Port 23115 -TimeoutSeconds $WaitTimeoutSeconds -Processes $Processes
    Wait-ForTcpEndpoint -Name 'STS server' -Address '127.0.0.1' -Port 6600 -TimeoutSeconds $WaitTimeoutSeconds -Processes $Processes
    Wait-ForTcpEndpoint -Name 'Account API' -Address '127.0.0.1' -Port 4001 -TimeoutSeconds $WaitTimeoutSeconds -Processes $Processes
    Wait-ForTcpEndpoint -Name 'Character API' -Address '127.0.0.1' -Port 4000 -TimeoutSeconds $WaitTimeoutSeconds -Processes $Processes
    Wait-ForTcpEndpoint -Name 'World server' -Address '127.0.0.1' -Port 24000 -TimeoutSeconds $WaitTimeoutSeconds -Processes $Processes
    Assert-ProcessesAlive -Processes $Processes
}

function Start-WildStarClient {
    param([string] $ExecutablePath)

    $resolvedPatcherHost = if ([string]::IsNullOrWhiteSpace($PatcherHost)) {
        $AuthHost
    }
    else {
        $PatcherHost
    }

    $arguments = @(
        '/auth', $AuthHost,
        '/authNc', $AuthHost,
        '/lang', $ClientLanguage,
        '/patcher', $resolvedPatcherHost,
        '/SettingsKey', 'WildStar',
        '/realmDataCenterId', '9'
    )

    Write-Info "Launching WildStar client $ExecutablePath"
    Start-Process -FilePath $ExecutablePath -WorkingDirectory (Split-Path -Parent $ExecutablePath) -ArgumentList $arguments | Out-Null
}

$RepoRoot = Get-AbsolutePath -Path $RepoRoot

if (!$SkipSetup) {
    Write-Section 'Setup'
    Invoke-NexusForeverSetup
}

$assetInfo = Initialize-GameAssets

Write-Section 'Runtime configuration'
Update-RuntimeConfigFiles -GameTablePath $assetInfo.TablePath -MapPath $assetInfo.MapPath

Invoke-AccountSeeder

$startedProcesses = @()
if (!$SkipServerLaunch) {
    Write-Section 'Server launch'
    $startedProcesses = @(Start-StandaloneServers)
}

if (!$SkipServerLaunch -or !$SkipClientLaunch) {
    Wait-ForRuntimeReadiness -Processes $startedProcesses
}

if (!$SkipClientLaunch) {
    Write-Section 'Client'
    $resolvedClientExecutable = Resolve-ClientExecutablePath
    Start-WildStarClient -ExecutablePath $resolvedClientExecutable
}

Write-Section 'Done'
Write-Host 'NexusForever is prepared for local login testing.' -ForegroundColor Green
Write-Host "Account: $DefaultAccountUsername / $DefaultAccountPassword" -ForegroundColor Green