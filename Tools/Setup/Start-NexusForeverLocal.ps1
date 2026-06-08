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
* create or verify the local player, gm, and admin login accounts
* start the standalone server processes and wait for the local endpoints
* launch WildStar64.exe against localhost for immediate testing

By default, generated runtime assets are staged under
.nexusforever-runtime\assets in the repo root. Pass -ExistingTableDirectory and
-ExistingMapDirectory to reuse assets you already generated elsewhere.
Map generation and table extraction use a bounded parallel worker count by
default. Pass -MapGeneratorParallelism to override it.

.EXAMPLE
.\Tools\Setup\Start-NexusForeverLocal.ps1 -ClientDirectory "D:\Games\WildStar" -PromptForRootPassword

.EXAMPLE
.\Tools\Setup\Start-NexusForeverLocal.ps1 `
  -ExistingTableDirectory "D:\WildStarAssets\tbl" `
  -ExistingMapDirectory "D:\WildStarAssets\map" `
  -ClientDirectory "D:\Games\WildStar" `
  -SkipSetup

.EXAMPLE
.\Tools\Setup\Start-NexusForeverLocal.ps1 `
  -ClientDirectory "D:\Games\WildStar" `
  -EnableClientConsole `
  -EnableClientLogging `
  -PromptForRootPassword
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

    [switch] $SkipSetup,
    [switch] $SkipServerLaunch,
    [switch] $SkipClientLaunch,
    [switch] $OverwriteConfig,
    [bool] $RestartExistingServers = $true,
    [switch] $RestartAuthWorldOnly,

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
    [ValidateSet('MySql', 'Sqlite')]
    [string] $DatabaseProvider = 'MySql',
    [string] $SqliteDirectory = '',
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
    [switch] $EnableDataMappingAuthoring,

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

$wildStarClientLaunchScript = Join-Path $PSScriptRoot 'WildStarClientLaunch.ps1'
if (!(Test-Path -LiteralPath $wildStarClientLaunchScript -PathType Leaf)) {
    throw "WildStar client launch helper was not found: $wildStarClientLaunchScript"
}

. $wildStarClientLaunchScript

$ExplicitClientArgumentsOverrideRequested = Test-WildStarClientArgumentOverrideRequested -BoundParameters $PSBoundParameters

$GameDatabases = [ordered]@{
    Auth       = 'nexus_forever_auth'
    Character  = 'nexus_forever_character'
    World      = 'nexus_forever_world'
    Group      = 'nexus_forever_group'
    Chat       = 'nexus_forever_chat'
    Friendship = 'nexus_forever_friendship'
}

$LocalAccountRoleIds = [ordered]@{
    Player        = 1
    GameMaster    = 2
    Administrator = 3
}

function Get-LocalLoginAccounts {
    $accounts = @(
        [pscustomobject]@{
            UserName = $PlayerAccountUsername
            Password = $PlayerAccountPassword
            RoleId   = $LocalAccountRoleIds['Player']
            RoleName = 'Player'
        },
        [pscustomobject]@{
            UserName = $GameMasterAccountUsername
            Password = $GameMasterAccountPassword
            RoleId   = $LocalAccountRoleIds['GameMaster']
            RoleName = 'GameMaster'
        },
        [pscustomobject]@{
            UserName = $AdministratorAccountUsername
            Password = $AdministratorAccountPassword
            RoleId   = $LocalAccountRoleIds['Administrator']
            RoleName = 'Administrator'
        }
    )

    foreach ($account in $accounts) {
        if ([string]::IsNullOrWhiteSpace($account.UserName)) {
            throw "Local account username for role '$($account.RoleName)' cannot be empty."
        }

        if ([string]::IsNullOrWhiteSpace($account.Password)) {
            throw "Local account password for role '$($account.RoleName)' cannot be empty."
        }
    }

    $duplicateUserNames = @($accounts | Group-Object UserName | Where-Object Count -gt 1)
    if ($duplicateUserNames.Count -gt 0) {
        throw "Local account usernames must be unique. Duplicates: $($duplicateUserNames.Name -join ', ')"
    }

    $accounts
}

function Write-LocalAccountSummary {
    foreach ($account in Get-LocalLoginAccounts) {
        Write-Host ("  {0} / {1} ({2}, roleId={3})" -f $account.UserName, $account.Password, $account.RoleName, $account.RoleId) -ForegroundColor Green
    }
}

function Test-StoreBannerHostsRedirectEnabled {
    $StoreBannerMode -eq 'HostsRedirect' -or $StoreBannerMode -eq 'Both'
}

function Test-StoreBannerLooseDataEnabled {
    !$SkipLocalStoreBannerData -and ($StoreBannerMode -eq 'LooseData' -or $StoreBannerMode -eq 'Both')
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

$AuthWorldRestartProjects = @(
    'NexusForever.AuthServer',
    'NexusForever.WorldServer'
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

function Invoke-NexusForeverBuild {
    if (!(Get-Command dotnet -ErrorAction SilentlyContinue)) {
        throw 'Required command ''dotnet'' was not found. Install the .NET SDK required by this repository.'
    }

    if (!$SkipServerLaunch) {
        foreach ($server in $ServerProcesses) {
            $executablePath = Join-Path $RepoRoot "Source\$($server.Project)\bin\$Configuration\$TargetFramework\$($server.Exe)"
            if ((Test-Path -LiteralPath $executablePath -PathType Leaf) -and (Test-ShouldRestartServer -Server $server)) {
                Stop-RunningProcessByPath -ExecutablePath $executablePath -DisplayName $server.Name
            }
        }
    }

    $solutionPath = Join-Path $RepoRoot 'Source\NexusForever.slnx'
    if (!(Test-Path -LiteralPath $solutionPath -PathType Leaf)) {
        $solutionPath = Join-Path $RepoRoot 'Source\NexusForever.sln'
    }

    if (!(Test-Path -LiteralPath $solutionPath -PathType Leaf)) {
        throw "NexusForever solution was not found under $RepoRoot\Source."
    }

    Write-Info 'Building NexusForever because -SkipSetup skips Initialize-NexusForever.ps1 build phase.'
    & dotnet build $solutionPath --configuration $Configuration --nologo -v minimal
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet build failed with exit code $LASTEXITCODE."
    }
}

$dependencyBootstrapScript = Join-Path $PSScriptRoot 'DependencyBootstrap.ps1'
if (!(Test-Path -LiteralPath $dependencyBootstrapScript -PathType Leaf)) {
    throw "Dependency bootstrap script was not found: $dependencyBootstrapScript"
}

. $dependencyBootstrapScript

$ShouldPromptForRootPasswordInSetup = [bool] $PromptForRootPassword
$MySqlProvisionedThisRun = $false
$RabbitMqProvisionedThisRun = $false

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
        foreach ($candidateDirectory in @($root, (Join-Path $root 'Client64'), (Join-Path $root 'Client32'))) {
            if (!(Test-Path -LiteralPath $candidateDirectory -PathType Container)) {
                continue
            }

            foreach ($candidate in @('WildStar64.exe', 'WildStar32.exe')) {
                $candidatePath = Join-Path $candidateDirectory $candidate
                if (Test-Path -LiteralPath $candidatePath -PathType Leaf) {
                    return (Resolve-Path -LiteralPath $candidatePath).Path
                }
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
        $clientExecutableDirectory = Split-Path -Parent (Get-AbsolutePath -Path $ClientExecutable)
        $candidateRoots += $clientExecutableDirectory

        $clientInstallRoot = Split-Path -Parent $clientExecutableDirectory
        if (![string]::IsNullOrWhiteSpace($clientInstallRoot)) {
            $candidateRoots += $clientInstallRoot
        }
    }

    foreach ($root in $candidateRoots | Select-Object -Unique) {
        $candidate = Join-Path $root 'Patch'
        if (Test-Path -LiteralPath $candidate -PathType Container) {
            return (Resolve-Path -LiteralPath $candidate).Path
        }
    }

    throw 'WildStar Patch directory was not found. Pass -PatchDirectory or -ClientDirectory.'
}

function Resolve-SqliteDirectoryPath {
    param([string] $Path)

    if ([string]::IsNullOrWhiteSpace($Path)) {
        $Path = Join-Path $RepoRoot '.nexusforever-runtime\sqlite'
    }
    elseif (![System.IO.Path]::IsPathRooted($Path)) {
        $Path = Join-Path $RepoRoot $Path
    }

    New-Item -ItemType Directory -Path $Path -Force | Out-Null
    (Resolve-Path -LiteralPath $Path).Path
}

function Get-DatabaseKey {
    param([string] $Database)

    foreach ($entry in $GameDatabases.GetEnumerator()) {
        if ($entry.Value -eq $Database) {
            return $entry.Key
        }
    }

    throw "Unknown NexusForever database '$Database'."
}

function Get-SqliteDatabasePath {
    param([string] $Database)

    $databaseKey = (Get-DatabaseKey -Database $Database).ToLowerInvariant()
    Join-Path $SqliteDirectory "nexus_forever_$databaseKey.sqlite"
}

function Get-DatabaseConnectionString {
    param([string] $Database)

    if ($DatabaseProvider -eq 'Sqlite') {
        $databasePath = Get-SqliteDatabasePath -Database $Database
        return "Data Source=$databasePath"
    }

    "server=$MySqlHost;port=$MySqlPort;user=$DatabaseUser;password=$DatabasePassword;database=$Database"
}

function Get-BrokerConnectionString {
    "amqp://${BrokerUser}:${BrokerPassword}@${BrokerHost}:${BrokerPort}"
}

function Get-ProcessLogDirectory {
    $logDirectory = Join-Path $RepoRoot '.nexusforever-runtime\logs'
    New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null
    (Resolve-Path -LiteralPath $logDirectory).Path
}

function Set-NexusLogLevel {
    param(
        [string] $ProjectName,
        [string] $Level
    )

    $buildOutput = Join-Path $RepoRoot "Source\$ProjectName\bin\$Configuration\$TargetFramework"
    $nlogPath = Join-Path $buildOutput 'nlog.config'

    if (!(Test-Path -LiteralPath $nlogPath -PathType Leaf)) {
        Write-Info "No nlog.config found at $nlogPath; skipping log level override"
        return
    }

    $content = Get-Content -LiteralPath $nlogPath -Raw
    $variableMatch = [regex]::Match($content, '(<variable\b(?=[^>]*\bname="minLogLevel")[^>]*\bvalue=")([^"]*)(")')
    if (!$variableMatch.Success) {
        Write-Warning "Could not update minLogLevel variable in $nlogPath. The variable tag may be missing."
        return
    }

    $currentLevel = $variableMatch.Groups[2].Value
    if ($currentLevel -eq $Level) {
        Write-Info "Log level already $Level in $nlogPath"
        return
    }

    $updated = $content.Substring(0, $variableMatch.Groups[2].Index) +
        $Level +
        $content.Substring($variableMatch.Groups[2].Index + $variableMatch.Groups[2].Length)

    Set-Content -LiteralPath $nlogPath -Value $updated -Encoding utf8
    Write-Info "Set log level to $Level in $nlogPath"
}

function Get-ProcessLogPaths {
    param([string] $Name)

    $safeName = ($Name -replace '[^A-Za-z0-9_.-]', '_')
    $logDirectory = Get-ProcessLogDirectory

    [pscustomobject]@{
        StandardOutput = Join-Path $logDirectory "$safeName.stdout.log"
        StandardError  = Join-Path $logDirectory "$safeName.stderr.log"
    }
}

function Get-LogTail {
    param(
        [string] $Path,
        [int] $LineCount = 40
    )

    if ([string]::IsNullOrWhiteSpace($Path) -or !(Test-Path -LiteralPath $Path -PathType Leaf)) {
        return ''
    }

    try {
        $lines = @(Get-Content -LiteralPath $Path -Tail $LineCount -ErrorAction Stop)
        if ($lines.Count -eq 0) {
            return ''
        }

        ($lines -join [Environment]::NewLine).TrimEnd()
    }
    catch {
        ''
    }
}

function Get-ProcessStartupFailureDetails {
    param(
        [string] $StandardOutputPath,
        [string] $StandardErrorPath
    )

    $sections = @()

    $stdoutTail = Get-LogTail -Path $StandardOutputPath
    if ($stdoutTail) {
        $sections += "stdout ($StandardOutputPath):`n$stdoutTail"
    }

    $stderrTail = Get-LogTail -Path $StandardErrorPath
    if ($stderrTail) {
        $sections += "stderr ($StandardErrorPath):`n$stderrTail"
    }

    if ($sections.Count -eq 0) {
        return ''
    }

    "`n" + ($sections -join "`n`n")
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

function Set-JsonPropertyValue {
    param(
        [object] $Node,
        [string] $Name,
        [object] $Value
    )

    $property = $Node.PSObject.Properties[$Name]
    if ($property) {
        $property.Value = $Value
        return
    }

    $Node | Add-Member -NotePropertyName $Name -NotePropertyValue $Value
}

function Get-DatabaseFromConnectionNode {
    param(
        [object] $Node,
        [string] $NodeName
    )

    $connectionProperty = $Node.PSObject.Properties['ConnectionString']
    if (!$connectionProperty -or !($connectionProperty.Value -is [string])) {
        return $null
    }

    $value = [string] $connectionProperty.Value
    if ($value -match '(?i)(?:^|;)database=([^;]+)') {
        $database = $Matches[1]
        if ($GameDatabases.Values -contains $database) {
            return $database
        }
    }

    if (![string]::IsNullOrWhiteSpace($NodeName)) {
        $databaseKey = ($NodeName -split ':', 2)[0]
        if ($GameDatabases.Contains($databaseKey)) {
            return $GameDatabases[$databaseKey]
        }
    }

    $null
}

function Update-ConnectionStringsInJsonNode {
    param(
        [AllowNull()][object] $Node,
        [string] $NodeName = ''
    )

    if ($null -eq $Node) {
        return
    }

    if ($Node -is [System.Collections.IEnumerable] -and !($Node -is [string])) {
        foreach ($item in $Node) {
            Update-ConnectionStringsInJsonNode -Node $item -NodeName $NodeName
        }

        return
    }

    $connectionProperty = $Node.PSObject.Properties['ConnectionString']
    if ($connectionProperty -and $connectionProperty.Value -is [string]) {
        $database = Get-DatabaseFromConnectionNode -Node $Node -NodeName $NodeName
        if ($database) {
            $connectionProperty.Value = Get-DatabaseConnectionString -Database $database
            Set-JsonPropertyValue -Node $Node -Name 'Provider' -Value $DatabaseProvider
        }
        elseif ([string] $connectionProperty.Value -like 'amqp://*') {
            $connectionProperty.Value = Get-BrokerConnectionString
        }
    }

    foreach ($property in $Node.PSObject.Properties) {
        Update-ConnectionStringsInJsonNode -Node $property.Value -NodeName $property.Name
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
                $json.Urls = if (Test-StoreBannerHostsRedirectEnabled) {
                    'http://0.0.0.0:5000;http://0.0.0.0:80'
                }
                else {
                    'http://0.0.0.0:5000'
                }

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

    $requiredMapFiles = @(
        'Eastern.nfmap',
        'NewCentral.nfmap',
        'NewPlayerExperience.nfmap',
        'Western.nfmap'
    )

    foreach ($requiredMapFile in $requiredMapFiles) {
        if (!(Test-Path -LiteralPath (Join-Path $Path $requiredMapFile) -PathType Leaf)) {
            return $false
        }
    }

    $true
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

        if ($MapGeneratorParallelism -gt 0) {
            $arguments += @('--maxParallelism', $MapGeneratorParallelism)
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
        DependencyMode         = $DependencyMode
        DockerCli              = $DockerCli
        PortableMariaDbImage   = $PortableMariaDbImage
        PortableRabbitMqImage  = $PortableRabbitMqImage
        MySqlExe               = $MySqlExe
        MySqlHost              = $MySqlHost
        MySqlPort              = $MySqlPort
        RootUser               = $RootUser
        RootPassword           = $RootPassword
        DatabaseUser           = $DatabaseUser
        DatabasePassword       = $DatabasePassword
        DatabaseProvider       = $DatabaseProvider
        SqliteDirectory        = $SqliteDirectory
        BrokerUser             = $BrokerUser
        BrokerPassword         = $BrokerPassword
        BrokerHost             = $BrokerHost
        BrokerPort             = $BrokerPort
        RabbitMqCtl            = $RabbitMqCtl
        Configuration          = $Configuration
        TargetFramework        = $TargetFramework
        WorldDatabasePath      = $WorldDatabasePath
        RuntimeWorldSeedPath   = $RuntimeWorldSeedPath
        PlayerAccountUsername  = $PlayerAccountUsername
        PlayerAccountPassword  = $PlayerAccountPassword
        GameMasterAccountUsername = $GameMasterAccountUsername
        GameMasterAccountPassword = $GameMasterAccountPassword
        AdministratorAccountUsername = $AdministratorAccountUsername
        AdministratorAccountPassword = $AdministratorAccountPassword
    }

    if ($ShouldPromptForRootPasswordInSetup) {
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

    if ($SkipRuntimeWorldSeedImport) {
        $setupParameters.SkipRuntimeWorldSeedImport = $true
    }

    if ($EnableDataMappingAuthoring) {
        $setupParameters.EnableDataMappingAuthoring = $true
    }

    if ($InstallDotNetEf) {
        $setupParameters.InstallDotNetEf = $true
    }

    & $setupScript @setupParameters
}

function Get-DatabaseEnvironmentOverrides {
    $environmentOverrides = @{
        'ConnectionStrings__authdb'       = Get-DatabaseConnectionString -Database $GameDatabases.Auth
        'ConnectionStrings__characterdb'  = Get-DatabaseConnectionString -Database $GameDatabases.Character
        'ConnectionStrings__worlddb'      = Get-DatabaseConnectionString -Database $GameDatabases.World
        'ConnectionStrings__groupdb'      = Get-DatabaseConnectionString -Database $GameDatabases.Group
        'ConnectionStrings__chatdb'       = Get-DatabaseConnectionString -Database $GameDatabases.Chat
        'ConnectionStrings__friendshipdb' = Get-DatabaseConnectionString -Database $GameDatabases.Friendship
    }

    foreach ($entry in $GameDatabases.GetEnumerator()) {
        $environmentOverrides["Database__$($entry.Key)__Provider"] = $DatabaseProvider
        $environmentOverrides["Database__$($entry.Key)__ConnectionString"] = Get-DatabaseConnectionString -Database $entry.Value
    }

    $environmentOverrides
}

function Invoke-AccountSeeder {
    $migrationExe = Join-Path $RepoRoot "Source\NexusForever.Aspire.Database.Migrations\bin\$Configuration\$TargetFramework\NexusForever.Aspire.Database.Migrations.exe"
    $skippedWorldDatabasePath = Join-Path $RepoRoot '.nexusforever-runtime\skip-world-database'

    if (!(Test-Path -LiteralPath $migrationExe -PathType Leaf)) {
        throw "Database migration executable was not found: $migrationExe"
    }

    $environmentOverrides = Get-DatabaseEnvironmentOverrides
    $environmentOverrides['DatabaseMigration__Skip'] = 'true'
    $environmentOverrides['WorldDatabase__Path'] = $skippedWorldDatabasePath

    $accounts = @(Get-LocalLoginAccounts)
    for ($index = 0; $index -lt $accounts.Count; $index++) {
        $account = $accounts[$index]
        $environmentOverrides["AccountCreation__Accounts__${index}__UserName"] = $account.UserName
        $environmentOverrides["AccountCreation__Accounts__${index}__Password"] = $account.Password
        $environmentOverrides["AccountCreation__Accounts__${index}__RoleId"] = [string] $account.RoleId
    }

    Write-Section 'Local accounts'
    Invoke-WithTemporaryEnvironment -Variables $environmentOverrides -ScriptBlock {
        Invoke-ExternalCommand -FilePath $migrationExe -WorkingDirectory (Split-Path -Parent $migrationExe)
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

function Test-ShouldRestartServer {
    param([hashtable] $Server)

    if ($RestartAuthWorldOnly) {
        return $AuthWorldRestartProjects -contains $Server.Project
    }

    [bool] $RestartExistingServers
}

function Stop-RunningProcessByPath {
    param(
        [string] $ExecutablePath,
        [string] $DisplayName
    )

    $runningProcesses = @(Get-RunningProcessesByPath -ExecutablePath $ExecutablePath)

    foreach ($process in $runningProcesses) {
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

function Start-StandaloneServers {
    Assert-BrokerEndpointReady

    $startedProcesses = @()

    foreach ($server in $ServerProcesses) {
        $workingDirectory = Join-Path $RepoRoot "Source\$($server.Project)\bin\$Configuration\$TargetFramework"
        $exePath = Join-Path $workingDirectory $server.Exe
        $logPaths = Get-ProcessLogPaths -Name $server.Project

        if (!(Test-Path -LiteralPath $exePath -PathType Leaf)) {
            throw "Cannot start $($server.Name); executable was not found: $exePath"
        }

        $existingProcesses = @(Get-RunningProcessesByPath -ExecutablePath $exePath)
        if ($existingProcesses.Count -gt 0) {
            if (Test-ShouldRestartServer -Server $server) {
                Stop-RunningProcessByPath -ExecutablePath $exePath -DisplayName $server.Name
            }
            else {
                Write-Info "Reusing existing $($server.Name) process(es): $(@($existingProcesses | ForEach-Object Id) -join ', ')"
                foreach ($existingProcess in $existingProcesses) {
                    $startedProcesses += [pscustomobject]@{
                        Name               = $server.Name
                        Port               = $server.Port
                        Process            = $existingProcess
                        StandardOutputPath = ''
                        StandardErrorPath  = ''
                    }
                }

                continue
            }
        }

        Write-Info "Starting $($server.Name)"
        Set-NexusLogLevel -ProjectName $server.Project -Level $LogLevel
        Remove-Item -LiteralPath $logPaths.StandardOutput, $logPaths.StandardError -Force -ErrorAction SilentlyContinue
        $process = Start-Process -FilePath $exePath -WorkingDirectory $workingDirectory -WindowStyle Hidden -RedirectStandardOutput $logPaths.StandardOutput -RedirectStandardError $logPaths.StandardError -PassThru

        Start-Sleep -Milliseconds 500
        $process.Refresh()
        if ($process.HasExited) {
            $failureDetails = Get-ProcessStartupFailureDetails -StandardOutputPath $logPaths.StandardOutput -StandardErrorPath $logPaths.StandardError
            throw "$($server.Name) exited immediately with code $($process.ExitCode).$failureDetails"
        }

        $startedProcesses += [pscustomobject]@{
            Name               = $server.Name
            Port               = $server.Port
            Process            = $process
            StandardOutputPath = $logPaths.StandardOutput
            StandardErrorPath  = $logPaths.StandardError
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
            $failureDetails = Get-ProcessStartupFailureDetails -StandardOutputPath $entry.StandardOutputPath -StandardErrorPath $entry.StandardErrorPath
            throw "$($entry.Name) exited before startup completed with code $($entry.Process.ExitCode).$failureDetails"
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

function Assert-BrokerEndpointReady {
    $deadline = [DateTime]::UtcNow.AddSeconds([Math]::Min([Math]::Max($WaitTimeoutSeconds, 5), 30))
    while ([DateTime]::UtcNow -lt $deadline) {
        if (Test-TcpEndpoint -Address $BrokerHost -Port $BrokerPort) {
            Write-Info "RabbitMQ broker is reachable on ${BrokerHost}:${BrokerPort}"
            return
        }

        Start-Sleep -Milliseconds 500
    }

    throw "RabbitMQ broker is not reachable on ${BrokerHost}:${BrokerPort}. Start RabbitMQ or update -BrokerHost/-BrokerPort before launching the broker-backed NexusForever servers."
}

function Wait-ForRuntimeReadiness {
    param([object[]] $Processes = @())

    Write-Section 'Server readiness'
    Wait-ForTcpEndpoint -Name 'Auth server' -Address '127.0.0.1' -Port 23115 -TimeoutSeconds $WaitTimeoutSeconds -Processes $Processes
    Wait-ForTcpEndpoint -Name 'STS server' -Address '127.0.0.1' -Port 6600 -TimeoutSeconds $WaitTimeoutSeconds -Processes $Processes
    Wait-ForTcpEndpoint -Name 'Account API' -Address '127.0.0.1' -Port 4001 -TimeoutSeconds $WaitTimeoutSeconds -Processes $Processes
    Wait-ForTcpEndpoint -Name 'Character API' -Address '127.0.0.1' -Port 4000 -TimeoutSeconds $WaitTimeoutSeconds -Processes $Processes
    Wait-ForTcpEndpoint -Name 'World server' -Address '127.0.0.1' -Port 24000 -TimeoutSeconds $WaitTimeoutSeconds -Processes $Processes
    if (Test-StoreBannerHostsRedirectEnabled) {
        Wait-ForTcpEndpoint -Name 'Store banner web endpoint' -Address '127.0.0.1' -Port 80 -TimeoutSeconds $WaitTimeoutSeconds -Processes $Processes
    }

    Assert-ProcessesAlive -Processes $Processes
}

function Test-CurrentProcessElevated {
    try {
        $principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
        $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
    }
    catch {
        $false
    }
}

function Enable-StoreBannerHostsRedirect {
    $hostName = 'static.wildstar-online.com'
    $hostsPath = Join-Path $env:SystemRoot 'System32\drivers\etc\hosts'

    if (!(Test-CurrentProcessElevated)) {
        throw "Store banner hosts redirect needs an elevated PowerShell session so $hostsPath can map $hostName to 127.0.0.1. Re-run as Administrator, or use -StoreBannerMode LooseData with a custom-files client."
    }

    if (!(Test-Path -LiteralPath $hostsPath -PathType Leaf)) {
        throw "Windows hosts file was not found at $hostsPath."
    }

    $escapedHostName = [regex]::Escape($hostName)
    $lines = @(Get-Content -LiteralPath $hostsPath)
    $activeHostLines = @($lines | Where-Object {
        $_ -notmatch '^\s*#' -and $_ -match "(?i)(^|\s)$escapedHostName(\s|$)"
    })

    $localHostLines = @($activeHostLines | Where-Object {
        $_ -match '^\s*(127\.0\.0\.1|::1)\s+'
    })
    if ($localHostLines.Count -gt 0) {
        Write-Info "Store banner host $hostName already resolves locally in $hostsPath"
        return
    }

    if ($activeHostLines.Count -gt 0) {
        throw "The hosts file already maps $hostName to a non-local address. Update $hostsPath so it maps to 127.0.0.1 before using -StoreBannerMode HostsRedirect."
    }

    Add-Content -LiteralPath $hostsPath -Value ''
    Add-Content -LiteralPath $hostsPath -Value '# NexusForever local storefront banners'
    Add-Content -LiteralPath $hostsPath -Value "127.0.0.1`t$hostName"

    try {
        & ipconfig /flushdns | Out-Null
    }
    catch {
        Write-Warning "Failed to flush DNS cache after updating $hostsPath. $($_.Exception.Message)"
    }

    Write-Info "Mapped $hostName to 127.0.0.1 for stock-client storefront banners."
}

function Get-ClientConnectorExecutablePath {
    $clientConnectorPath = Join-Path $RepoRoot "Source\NexusForever.ClientConnector\bin\$Configuration\$TargetFramework\NexusForever.ClientConnector.exe"
    if (!(Test-Path -LiteralPath $clientConnectorPath -PathType Leaf)) {
        return ''
    }

    (Resolve-Path -LiteralPath $clientConnectorPath).Path
}

function Sync-ClientConnectorRuntime {
    param([string] $ClientDirectory)

    $clientConnectorExecutable = Get-ClientConnectorExecutablePath
    if ([string]::IsNullOrWhiteSpace($clientConnectorExecutable)) {
        return ''
    }

    $clientConnectorOutputDirectory = Split-Path -Parent $clientConnectorExecutable
    foreach ($runtimeFile in @(Get-ChildItem -LiteralPath $clientConnectorOutputDirectory -File -ErrorAction Stop)) {
        if ($runtimeFile.Name -ieq 'config.json') {
            continue
        }

        Copy-Item -LiteralPath $runtimeFile.FullName -Destination (Join-Path $ClientDirectory $runtimeFile.Name) -Force
    }

    (Resolve-Path -LiteralPath (Join-Path $ClientDirectory 'NexusForever.ClientConnector.exe')).Path
}

function Write-ClientConnectorConfig {
    param(
        [string] $ClientDirectory,
        [string] $HostName,
        [string] $Language,
        [int] $RealmDataCenterId = 6,
        [string[]] $ExtraArguments = @()
    )

    $configPath = Join-Path $ClientDirectory 'config.json'
    [pscustomobject]@{
        HostName       = $HostName
        Language       = $Language
        RealmDataCenterId = $RealmDataCenterId
        ExtraArguments = @($ExtraArguments | Where-Object { ![string]::IsNullOrWhiteSpace($_) })
    } | ConvertTo-Json -Depth 3 | Set-Content -LiteralPath $configPath -Encoding utf8

    (Resolve-Path -LiteralPath $configPath).Path
}

function Get-NullTerminatedUnicodeBytes {
    param([string] $Value)

    [Text.Encoding]::Unicode.GetBytes($Value + [char]0)
}

function Find-BytePattern {
    param(
        [byte[]] $Bytes,
        [byte[]] $Pattern,
        [int] $Start,
        [int] $End
    )

    if ($Pattern.Length -eq 0 -or $Start -lt 0 -or $End -gt $Bytes.Length -or $Start -ge $End) {
        return -1
    }

    $lastStart = $End - $Pattern.Length
    for ($i = $Start; $i -le $lastStart; $i++) {
        $matched = $true
        for ($j = 0; $j -lt $Pattern.Length; $j++) {
            if ($Bytes[$i + $j] -ne $Pattern[$j]) {
                $matched = $false
                break
            }
        }

        if ($matched) {
            return $i
        }
    }

    -1
}

function Set-UInt32LittleEndian {
    param(
        [byte[]] $Bytes,
        [int] $Offset,
        [uint32] $Value
    )

    [BitConverter]::GetBytes($Value).CopyTo($Bytes, $Offset)
}

function Update-RealmDataCenterStoreBannerUrl {
    param(
        [byte[]] $Bytes,
        [int] $RealmDataCenterId,
        [string] $StoreBannerUrl
    )

    $signature = [BitConverter]::ToUInt32($Bytes, 0)
    if ($signature -ne 0x4454424C) {
        throw 'RealmDataCenter.tbl has an invalid DBLT signature.'
    }

    $headerSize = 96
    $recordSize = [int][BitConverter]::ToUInt64($Bytes, 24)
    $recordCount = [int][BitConverter]::ToUInt64($Bytes, 48)
    $totalRecordSize = [int][BitConverter]::ToUInt64($Bytes, 56)
    $recordOffset = [int][BitConverter]::ToUInt64($Bytes, 64)
    $recordBase = $headerSize + $recordOffset
    $recordAreaSize = $recordSize * $recordCount
    $stringTableBase = $recordBase + $recordAreaSize
    $stringTableEnd = $stringTableBase + ($totalRecordSize - $recordAreaSize)

    if ($recordSize -ne 96) {
        throw "RealmDataCenter.tbl has unexpected record size $recordSize."
    }

    $localUrlBytes = Get-NullTerminatedUnicodeBytes -Value $StoreBannerUrl
    $existingUrls = @(
        'http://static.dev.wildstar-online.com/banners/',
        'http://static.qa.wildstar-online.com/banners/',
        'http://static.wildstar-online.com/banners/'
    )

    $storeBannerOffset = $null
    foreach ($existingUrl in $existingUrls) {
        $existingUrlBytes = Get-NullTerminatedUnicodeBytes -Value $existingUrl
        $matchOffset = Find-BytePattern -Bytes $Bytes -Pattern $existingUrlBytes -Start $stringTableBase -End $stringTableEnd
        if ($matchOffset -lt 0) {
            continue
        }

        if ($localUrlBytes.Length -gt $existingUrlBytes.Length) {
            throw "Local storefront banner URL is longer than the existing RealmDataCenter URL slot: $existingUrl"
        }

        [Array]::Clear($Bytes, $matchOffset, $existingUrlBytes.Length)
        $localUrlBytes.CopyTo($Bytes, $matchOffset)

        if ($null -eq $storeBannerOffset) {
            $storeBannerOffset = [uint32]($matchOffset - $stringTableBase + $recordAreaSize)
        }
    }

    if ($null -eq $storeBannerOffset) {
        throw 'No known StoreBannerDataUrlTemplate string was found in RealmDataCenter.tbl.'
    }

    $storeBannerFieldOffset = 84
    $rowOffset = -1
    for ($i = 0; $i -lt $recordCount; $i++) {
        $candidateOffset = $recordBase + $recordSize * $i
        $id = [BitConverter]::ToUInt32($Bytes, $candidateOffset)
        if ($id -eq [uint32]$RealmDataCenterId) {
            $rowOffset = $candidateOffset
            break
        }
    }

    if ($rowOffset -lt 0) {
        throw "RealmDataCenter.tbl does not contain row $RealmDataCenterId."
    }

    Set-UInt32LittleEndian -Bytes $Bytes -Offset ($rowOffset + $storeBannerFieldOffset) -Value 0
    Set-UInt32LittleEndian -Bytes $Bytes -Offset ($rowOffset + $storeBannerFieldOffset + 4) -Value $storeBannerOffset
    Set-UInt32LittleEndian -Bytes $Bytes -Offset ($rowOffset + $storeBannerFieldOffset + 8) -Value 0
}

function Write-LocalStoreBannerDataOverride {
    param(
        [string] $ClientExecutablePath,
        [string] $TableDirectory,
        [int] $RealmDataCenterId
    )

    $sourceTablePath = Join-Path $TableDirectory 'RealmDataCenter.tbl'
    if (!(Test-Path -LiteralPath $sourceTablePath -PathType Leaf)) {
        Write-Warning "RealmDataCenter.tbl was not found at $sourceTablePath; local store banner data will not be staged."
        return
    }

    $clientExecutableDirectory = Split-Path -Parent $ClientExecutablePath
    $clientRoot = Split-Path -Parent $clientExecutableDirectory
    $clientDirectoryName = Split-Path -Leaf $clientExecutableDirectory
    if ($clientDirectoryName -ine 'Client64' -and $clientDirectoryName -ine 'Client32') {
        $clientRoot = $clientExecutableDirectory
    }

    $bytes = [IO.File]::ReadAllBytes($sourceTablePath)
    Update-RealmDataCenterStoreBannerUrl `
        -Bytes $bytes `
        -RealmDataCenterId $RealmDataCenterId `
        -StoreBannerUrl 'http://localhost:5000/banners/'

    $targets = @(
        (Join-Path $clientRoot 'Data\RealmDataCenter.tbl'),
        (Join-Path $clientRoot 'Data\DB\RealmDataCenter.tbl')
    )

    foreach ($target in $targets) {
        $targetDirectory = Split-Path -Parent $target
        if (!(Test-Path -LiteralPath $targetDirectory -PathType Container)) {
            New-Item -ItemType Directory -Path $targetDirectory -Force | Out-Null
        }

        [IO.File]::WriteAllBytes($target, $bytes)
    }

    Write-Info "Staged local storefront banner RealmDataCenter override under $clientRoot\Data (custom-files client builds load this loose table)."
}

function Start-WildStarClient {
    param([string] $ExecutablePath)

    $runningWildStarProcesses = @(Get-RunningWildStarProcesses)
    if ($runningWildStarProcesses.Count -gt 0) {
        Write-Info "Skipping WildStar launch because the client is already running: $(@($runningWildStarProcesses | ForEach-Object Id) -join ', ')"
        return
    }

    $resolvedPatcherHost = if ([string]::IsNullOrWhiteSpace($PatcherHost)) {
        $AuthHost
    }
    else {
        $PatcherHost
    }

    $clientDirectoryPath = Split-Path -Parent $ExecutablePath
    $effectiveClientArguments = @(Get-WildStarEffectiveClientArguments `
        -ClientDirectory $clientDirectoryPath `
        -ClientArguments $ClientArguments `
        -EnableClientConsole:$EnableClientConsole `
        -EnableClientLogging:$EnableClientLogging `
        -ClientLogLevel $ClientLogLevel `
        -ClientLogStdout:$ClientLogStdout `
        -ClientLogDir $ClientLogDir `
        -ExplicitOverrideRequested $ExplicitClientArgumentsOverrideRequested)
    $sharedHost = [string]::Equals($resolvedPatcherHost, $AuthHost, [System.StringComparison]::OrdinalIgnoreCase)

    if ((-not $ExplicitClientArgumentsOverrideRequested) -and ($effectiveClientArguments.Count -gt 0)) {
        Write-Info "Reusing staged client arguments: $($effectiveClientArguments -join ' ')"
    }

    if ($effectiveClientArguments -contains '-logFile') {
        Write-WildStarClientLoggingHint -ClientDirectory $clientDirectoryPath
    }

    if ($sharedHost) {
        $clientConnectorExecutable = Sync-ClientConnectorRuntime -ClientDirectory $clientDirectoryPath
    }
    else {
        $clientConnectorExecutable = Get-ClientConnectorExecutablePath
    }

    if ($clientConnectorExecutable -and $sharedHost) {
        $configPath = Write-ClientConnectorConfig -ClientDirectory $clientDirectoryPath -HostName $AuthHost -Language $ClientLanguage -RealmDataCenterId $RealmDataCenterId -ExtraArguments $effectiveClientArguments
        $clientConnectorParameters = @{
            FilePath         = $clientConnectorExecutable
            WorkingDirectory = $clientDirectoryPath
        }

        Write-Info "Prepared NexusForever.ClientConnector config $configPath"

        if (!(Test-CurrentProcessElevated)) {
            Write-Warning 'Official client connection guidance expects NexusForever.ClientConnector to run as Administrator. Approve the UAC prompt if Windows asks.'
            $clientConnectorParameters.Verb = 'RunAs'
        }

        Write-Info "Launching WildStar client through NexusForever.ClientConnector"

        try {
            Start-Process @clientConnectorParameters | Out-Null
            return
        }
        catch {
            throw "Failed to launch NexusForever.ClientConnector from $clientConnectorExecutable. $($_.Exception.Message)"
        }
    }

    if (![string]::IsNullOrWhiteSpace($clientConnectorExecutable) -and !$sharedHost) {
        Write-Warning 'NexusForever.ClientConnector only supports one shared host for auth and patcher. Falling back to direct WildStar launch because -PatcherHost differs from -AuthHost.'
    }
    elseif ([string]::IsNullOrWhiteSpace($clientConnectorExecutable)) {
        Write-Warning 'NexusForever.ClientConnector executable was not found in the current build output. Falling back to direct WildStar launch.'
    }

    $arguments = @(
        '/auth', $AuthHost,
        '/authNc', $AuthHost,
        '/lang', $ClientLanguage,
        '/patcher', $resolvedPatcherHost,
        '/SettingsKey', 'WildStar',
        '/realmDataCenterId', $RealmDataCenterId.ToString()
    )
    $arguments += $effectiveClientArguments

    Write-Info "Launching WildStar client $ExecutablePath"
    Start-Process -FilePath $ExecutablePath -WorkingDirectory (Split-Path -Parent $ExecutablePath) -ArgumentList $arguments | Out-Null
}

$RepoRoot = Get-AbsolutePath -Path $RepoRoot
if ($DatabaseProvider -eq 'Sqlite') {
    $SqliteDirectory = Resolve-SqliteDirectoryPath -Path $SqliteDirectory
}

if ($DatabaseProvider -eq 'Sqlite' -and $EnableDataMappingAuthoring) {
    throw 'DataMapping authoring imports require MySQL/MariaDB reference databases. Re-run with -DatabaseProvider MySql, or omit -EnableDataMappingAuthoring for a runtime-only SQLite launch.'
}

if (Test-StoreBannerHostsRedirectEnabled -and !$PSBoundParameters.ContainsKey('RealmDataCenterId')) {
    $RealmDataCenterId = 9
    Write-Info 'Using RealmDataCenterId 9 because -StoreBannerMode HostsRedirect uses the stock client retail banner URL.'
}

$dependencyBootstrap = Resolve-NexusSetupDependencies `
    -RepoRoot $RepoRoot `
    -DependencyMode $DependencyMode `
    -DockerCli $DockerCli `
    -PortableMariaDbImage $PortableMariaDbImage `
    -PortableRabbitMqImage $PortableRabbitMqImage `
    -MySqlExe $MySqlExe `
    -MySqlHost $MySqlHost `
    -MySqlPort $MySqlPort `
    -RootUser $RootUser `
    -RootPassword $RootPassword `
    -PromptForRootPassword:$PromptForRootPassword `
    -SkipMySql:($DatabaseProvider -eq 'Sqlite') `
    -BrokerHost $BrokerHost `
    -BrokerPort $BrokerPort `
    -BrokerUser $BrokerUser `
    -BrokerPassword $BrokerPassword `
    -RabbitMqCtl $RabbitMqCtl

$DockerCli = $dependencyBootstrap.DockerCliResolved
$MySqlHost = $dependencyBootstrap.MySqlHost
$MySqlPort = $dependencyBootstrap.MySqlPort
$RootUser = $dependencyBootstrap.RootUser
$RootPassword = $dependencyBootstrap.RootPassword
$ShouldPromptForRootPasswordInSetup = $dependencyBootstrap.ShouldPromptForRootPassword
$MySqlProvisionedThisRun = $dependencyBootstrap.MySqlProvisionedThisRun
$BrokerHost = $dependencyBootstrap.BrokerHost
$BrokerPort = $dependencyBootstrap.BrokerPort
$RabbitMqProvisionedThisRun = $dependencyBootstrap.RabbitMqProvisionedThisRun

if ($SkipSetup -and ($MySqlProvisionedThisRun -or $RabbitMqProvisionedThisRun)) {
    throw 'Portable dependencies were created for the first time, but -SkipSetup was requested. Run without -SkipSetup once so the databases, users, migrations, and broker state can be initialized.'
}

if (!$SkipSetup) {
    Write-Section 'Setup'
    Invoke-NexusForeverSetup
}
else {
    Write-Section 'Build'
    Invoke-NexusForeverBuild
}

$assetInfo = Initialize-GameAssets

Write-Section 'Runtime configuration'
Update-RuntimeConfigFiles -GameTablePath $assetInfo.TablePath -MapPath $assetInfo.MapPath

if (Test-StoreBannerHostsRedirectEnabled -and !$SkipClientLaunch) {
    Write-Section 'Store banner redirect'
    Enable-StoreBannerHostsRedirect
}

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
    $runningWildStarProcesses = @(Get-RunningWildStarProcesses)
    if ($runningWildStarProcesses.Count -gt 0) {
        Write-Info "Skipping WildStar launch because the client is already running: $(@($runningWildStarProcesses | ForEach-Object Id) -join ', ')"
    }
    else {
        $resolvedClientExecutable = Resolve-ClientExecutablePath
        if (Test-StoreBannerLooseDataEnabled) {
            Write-LocalStoreBannerDataOverride `
                -ClientExecutablePath $resolvedClientExecutable `
                -TableDirectory $assetInfo.TablePath `
                -RealmDataCenterId $RealmDataCenterId
        }

        Start-WildStarClient -ExecutablePath $resolvedClientExecutable
    }
}

Write-Section 'Done'
Write-Host 'NexusForever is prepared for local login testing.' -ForegroundColor Green
Write-Host 'Local login accounts:' -ForegroundColor Green
Write-LocalAccountSummary
