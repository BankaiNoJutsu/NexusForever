#requires -Version 5.1
<#
.SYNOPSIS
Creates the local NexusForever databases and imports optional data dumps.

.DESCRIPTION
This script follows the standalone server setup flow from the NexusForever
server guide:

* create the MySQL/MariaDB user used by the server configs
* create the auth, character, world, group, chat, and friendship databases
* create/import the split SQL dump folders into standalone reference DBs
* copy default server configuration files
* build the solution and run EF Core migrations
* create or verify the local player, gm, and admin login accounts
* import the NexusForever.WorldDatabase SQL files into nexus_forever_world
* import the checked-in DataMapping runtime world seed SQL
* optionally configure RabbitMQ and start the standalone server processes

By default, the world database import searches for a sibling checkout named
NexusForever.WorldDatabase next to this NexusForever repo, then falls back to
the guide's C:\nexusforever-database path. Pass -WorldDatabasePath to override.

If mysql is not on PATH, the script searches common MariaDB and MySQL install
locations under Program Files before asking you to pass -MySqlExe explicitly.

The reference SQL imports are hash-marked in each target database so repeated
runs skip already imported files. If you need a clean re-import, pass
-RecreateLargeDumpDatabases.

Official world SQL files are imported only when their target tables and columns
exist in the migrated world schema. This keeps branch-specific world data from
being forced into a server checkout that does not support it yet.

After the official world import, the setup imports
Tools\DataMapping\sql\runtime_world_seed.sql by default. That seed contains the
promoted reviewed DataMapping runtime rows and does not require the Jabbithole,
WildStar client, or nf_map_* staging databases on a fresh machine. Pass
-SkipRuntimeWorldSeedImport to skip it, or -RuntimeWorldSeedPath to use a
different seed file.

The split folders jabbithole_mysql and wildstar_client_mysql are preferred over
the older all_jabbithole_mysql.sql and all_wildstar_client_mysql.sql files. The
single-file dumps are used only as fallbacks when a split folder is missing or
empty.

The reference SQL files are exported with SQLite-style INTEGER columns. MySQL maps
INTEGER to a 32-bit signed type, but the WildStar client dump contains larger
values. By default the importer streams the reference SQL files through a
compatibility normalizer that changes INTEGER columns to BIGINT and over-wide
VARCHAR columns to TEXT in CREATE TABLE blocks before they reach MySQL. It also
quotes generated column identifiers so names such as maxValue do not collide
with MySQL syntax.

.EXAMPLE
.\Tools\Setup\Initialize-NexusForever.ps1 -PromptForRootPassword -InstallDotNetEf

Runs the full setup against a local MySQL/MariaDB server, asks for the root
password, and installs dotnet-ef if needed. Already imported reference SQL
files are skipped when their hash marker matches.

.EXAMPLE
.\Tools\Setup\Initialize-NexusForever.ps1 -SkipLargeDumpImports -SkipWorldDatabaseImport

Creates the emulator databases, copies configs, builds, and runs migrations
without importing optional SQL data.

.EXAMPLE
.\Tools\Setup\Initialize-NexusForever.ps1 -PromptForRootPassword -RepairPartialLargeDumpDatabases

Skips completed reference SQL imports, but recreates any reference database that
has tables without a matching import marker from a previous failed import.

.EXAMPLE
.\Tools\Setup\Initialize-NexusForever.ps1 -MySqlExe "C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe" -PromptForRootPassword

Runs setup with an explicit MySQL client path.
#>

[CmdletBinding()]
param(
    [string] $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path,

    [ValidateSet('Auto', 'ExternalOnly', 'PortableDocker')]
    [string] $DependencyMode = 'Auto',
    [string] $DockerCli = 'docker',
    [string] $PortableMariaDbImage = 'mariadb:11.8',
    [string] $PortableRabbitMqImage = 'rabbitmq:4.1-management',

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

    [string] $Configuration = 'Debug',
    [string] $TargetFramework = 'net10.0',
    [switch] $SkipBuild,
    [switch] $SkipConfigCopy,
    [switch] $OverwriteConfig,
    [switch] $SkipMigrations,
    [switch] $InstallDotNetEf,

    [string] $WorldDatabasePath = '',
    [switch] $SkipWorldDatabaseImport,
    [switch] $CreateWorldDatabaseCompatibilityTables,
    [string] $RuntimeWorldSeedPath = '',
    [switch] $SkipRuntimeWorldSeedImport,

    [string] $JabbitholeSqlDirectory = 'jabbithole_mysql',
    [string] $JabbitholeSql = 'all_jabbithole_mysql.sql',
    [string] $JabbitholeDatabase = 'jabbithole',
    [string] $WildstarClientSqlDirectory = 'wildstar_client_mysql',
    [string] $WildstarClientSql = 'all_wildstar_client_mysql.sql',
    [string] $WildstarClientDatabase = 'wildstar_client',
    [switch] $SkipLargeDumpImports,
    [switch] $ForceImport,
    [switch] $RecreateLargeDumpDatabases,
    [switch] $RepairPartialLargeDumpDatabases,
    [switch] $KeepLargeDumpIntegerColumns,

    [Alias('DefaultAccountUsername')]
    [string] $PlayerAccountUsername = 'player',
    [Alias('DefaultAccountPassword')]
    [string] $PlayerAccountPassword = 'player',
    [string] $GameMasterAccountUsername = 'gm',
    [string] $GameMasterAccountPassword = 'gm',
    [string] $AdministratorAccountUsername = 'admin',
    [string] $AdministratorAccountPassword = 'admin',

    [switch] $StartServers
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

$LocalAccountRoleIds = [ordered]@{
    Player        = 1u
    GameMaster    = 2u
    Administrator = 3u
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

function Get-AccountCreationConfigAccounts {
    @(
        Get-LocalLoginAccounts | ForEach-Object {
            [pscustomobject]@{
                UserName = $_.UserName
                Password = $_.Password
                RoleId   = $_.RoleId
            }
        }
    )
}

function Write-LocalAccountSummary {
    foreach ($account in Get-LocalLoginAccounts) {
        Write-Host ("  {0} / {1} ({2}, roleId={3})" -f $account.UserName, $account.Password, $account.RoleName, $account.RoleId) -ForegroundColor Green
    }
}

$ConfigFiles = @(
    @{ Project = 'NexusForever.AuthServer';                Example = 'AuthServer.example.json';        Target = 'AuthServer.json' },
    @{ Project = 'NexusForever.StsServer';                 Example = 'StsServer.example.json';         Target = 'StsServer.json' },
    @{ Project = 'NexusForever.WorldServer';               Example = 'WorldServer.example.json';       Target = 'WorldServer.json' },
    @{ Project = 'NexusForever.Server.ChatServer';         Example = 'ChatServer.example.json';        Target = 'ChatServer.json' },
    @{ Project = 'NexusForever.Server.GroupServer';        Example = 'GroupServer.example.json';       Target = 'GroupServer.json' },
    @{ Project = 'NexusForever.Server.Friendship';         Example = 'FriendshipServer.example.json';  Target = 'FriendshipServer.json' },
    @{ Project = 'NexusForever.Server.Character';          Example = 'CharacterServer.example.json';   Target = 'CharacterServer.json' },
    @{ Project = 'NexusForever.API.Account';               Example = 'AccountAPI.example.json';        Target = 'AccountAPI.json' },
    @{ Project = 'NexusForever.API.Character';             Example = 'CharacterAPI.example.json';      Target = 'CharacterAPI.json' },
    @{ Project = 'NexusForever.Aspire.Database.Migrations'; Example = 'AspireMigrations.example.json'; Target = 'AspireMigrations.json' }
)

$ServerProcesses = @(
    @{ Project = 'NexusForever.AuthServer';          Exe = 'NexusForever.AuthServer.exe';          Name = 'NexusForever Auth' },
    @{ Project = 'NexusForever.StsServer';           Exe = 'NexusForever.StsServer.exe';           Name = 'NexusForever STS' },
    @{ Project = 'NexusForever.API.Account';         Exe = 'NexusForever.API.Account.exe';         Name = 'NexusForever Account API' },
    @{ Project = 'NexusForever.API.Character';       Exe = 'NexusForever.API.Character.exe';       Name = 'NexusForever Character API' },
    @{ Project = 'NexusForever.Server.ChatServer';   Exe = 'NexusForever.Server.ChatServer.exe';   Name = 'NexusForever Chat' },
    @{ Project = 'NexusForever.Server.GroupServer';  Exe = 'NexusForever.Server.GroupServer.exe';  Name = 'NexusForever Group' },
    @{ Project = 'NexusForever.Server.Friendship';   Exe = 'NexusForever.Server.Friendship.exe';   Name = 'NexusForever Friendship' },
    @{ Project = 'NexusForever.Server.Character';    Exe = 'NexusForever.Server.Character.exe';    Name = 'NexusForever Character' },
    @{ Project = 'NexusForever.WorldServer';         Exe = 'NexusForever.WorldServer.exe';         Name = 'NexusForever World' }
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

$dependencyBootstrapScript = Join-Path $PSScriptRoot 'DependencyBootstrap.ps1'
if (!(Test-Path -LiteralPath $dependencyBootstrapScript -PathType Leaf)) {
    throw "Dependency bootstrap script was not found: $dependencyBootstrapScript"
}

. $dependencyBootstrapScript

$MySqlInvocationMode = 'Native'
$MySqlDockerContainerName = ''
$MySqlDockerClientHost = ''
$MySqlDockerImage = $PortableMariaDbImage
$RabbitMqCtlInvocationMode = 'Native'
$RabbitMqDockerContainerName = ''
$RabbitMqCtlResolved = $RabbitMqCtl
$DockerCliResolved = $DockerCli
$ShouldPromptForRootPassword = [bool] $PromptForRootPassword
$MySqlProvisionedThisRun = $false
$RabbitMqProvisionedThisRun = $false

function Assert-Command {
    param(
        [string] $Command,
        [string] $InstallHint
    )

    if (Get-Command $Command -ErrorAction SilentlyContinue) {
        return
    }

    throw "Required command '$Command' was not found. $InstallHint"
}

function Resolve-ExecutablePath {
    param([string] $Command)

    if ([string]::IsNullOrWhiteSpace($Command)) {
        return $null
    }

    if (Test-Path -LiteralPath $Command -PathType Leaf) {
        return (Resolve-Path -LiteralPath $Command).Path
    }

    $found = Get-Command $Command -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($found) {
        return $found.Source
    }

    return $null
}

function Resolve-MySqlClient {
    param([string] $Command)

    $resolved = Resolve-ExecutablePath -Command $Command
    if ($resolved) {
        return $resolved
    }

    $candidatePatterns = @()
    if ($env:ProgramFiles) {
        $candidatePatterns += Join-Path $env:ProgramFiles 'MariaDB*\bin\mysql.exe'
        $candidatePatterns += Join-Path $env:ProgramFiles 'MySQL\MySQL Server *\bin\mysql.exe'
        $candidatePatterns += Join-Path $env:ProgramFiles 'MySQL\MySQL Workbench *\mysql.exe'
    }

    $programFilesX86 = [Environment]::GetFolderPath('ProgramFilesX86')
    if ($programFilesX86) {
        $candidatePatterns += Join-Path $programFilesX86 'MariaDB*\bin\mysql.exe'
        $candidatePatterns += Join-Path $programFilesX86 'MySQL\MySQL Server *\bin\mysql.exe'
        $candidatePatterns += Join-Path $programFilesX86 'MySQL\MySQL Workbench *\mysql.exe'
    }

    $candidates = foreach ($pattern in $candidatePatterns) {
        Get-Item -Path $pattern -ErrorAction SilentlyContinue
    }

    $preferred = $candidates |
        Sort-Object `
            @{ Expression = { if ($_.FullName -match '\\bin\\mysql\.exe$') { 0 } else { 1 } } },
            FullName |
        Select-Object -First 1

    if ($preferred) {
        Write-Info "Auto-detected MySQL client: $($preferred.FullName)"
        return $preferred.FullName
    }

    throw "Required command '$Command' was not found. Install MariaDB/MySQL client tools or pass -MySqlExe with the full mysql.exe path."
}

function Resolve-WorldDatabasePath {
    param(
        [AllowNull()][string] $Path,
        [string] $RepositoryRoot
    )

    if (![string]::IsNullOrWhiteSpace($Path)) {
        if (Test-Path -LiteralPath $Path -PathType Container) {
            $resolvedPath = (Resolve-Path -LiteralPath $Path).Path
            Write-Info "Using configured world database path: $resolvedPath"
            return $resolvedPath
        }

        Write-Warning "Configured world database path does not exist yet: $Path"
        return $Path
    }

    $repoParent = Split-Path -Parent $RepositoryRoot
    $siblingPath = Join-Path $repoParent 'NexusForever.WorldDatabase'
    $guidePath = 'C:\nexusforever-database'

    foreach ($candidate in @($siblingPath, $guidePath)) {
        if (Test-Path -LiteralPath $candidate -PathType Container) {
            $resolvedPath = (Resolve-Path -LiteralPath $candidate).Path
            Write-Info "Auto-detected world database path: $resolvedPath"
            return $resolvedPath
        }
    }

    Write-Info "No official world database checkout detected. Checked $siblingPath and $guidePath."
    return $siblingPath
}

function Resolve-RuntimeWorldSeedPath {
    param(
        [AllowNull()][string] $Path,
        [string] $RepositoryRoot
    )

    if (![string]::IsNullOrWhiteSpace($Path)) {
        if (Test-Path -LiteralPath $Path -PathType Leaf) {
            $resolvedPath = (Resolve-Path -LiteralPath $Path).Path
            Write-Info "Using configured runtime world seed: $resolvedPath"
            return $resolvedPath
        }

        Write-Warning "Configured runtime world seed does not exist yet: $Path"
        return $Path
    }

    Join-Path $RepositoryRoot 'Tools\DataMapping\sql\runtime_world_seed.sql'
}

function ConvertFrom-SecureStringToPlainText {
    param([securestring] $SecureString)

    $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($SecureString)
    try {
        [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr)
    }
    finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
    }
}

function Quote-MySqlIdentifier {
    param([string] $Identifier)

    if ($Identifier -notmatch '^[A-Za-z0-9_]+$') {
        throw "Unsafe MySQL identifier '$Identifier'. Use letters, numbers, and underscores only."
    }

    "``$Identifier``"
}

function Quote-MySqlString {
    param([AllowNull()][string] $Value)

    if ($null -eq $Value) {
        return 'NULL'
    }

    "'" + ($Value -replace '\\', '\\' -replace "'", "''") + "'"
}

function Get-MySqlArguments {
    param([string] $Database)

    $effectiveHost = $MySqlHost
    if ($MySqlInvocationMode -eq 'DockerClient' -and ![string]::IsNullOrWhiteSpace($MySqlDockerClientHost)) {
        $effectiveHost = $MySqlDockerClientHost
    }

    $arguments = @(
        '--protocol=tcp',
        "--host=$effectiveHost",
        "--port=$MySqlPort",
        "--user=$RootUser",
        '--default-character-set=utf8mb4',
        '--binary-mode',
        '--max_allowed_packet=1G'
    )

    if ($Database) {
        $arguments += "--database=$Database"
    }

    $arguments
}

function Get-MySqlCommandInvocation {
    param([string[]] $Arguments)

    $effectiveArguments = @($Arguments)
    if ($MySqlInvocationMode -ne 'Native' -and $RootPassword) {
        $effectiveArguments += "--password=$RootPassword"
    }

    switch ($MySqlInvocationMode) {
        'DockerExec' {
            return [pscustomobject]@{
                FilePath  = $DockerCliResolved
                Arguments = @('exec', '-i', $MySqlDockerContainerName, 'mariadb') + $effectiveArguments
            }
        }
        'DockerClient' {
            return [pscustomobject]@{
                FilePath  = $DockerCliResolved
                Arguments = @('run', '--rm', '-i', $MySqlDockerImage, 'mariadb') + $effectiveArguments
            }
        }
        default {
            return [pscustomobject]@{
                FilePath  = $MySqlExe
                Arguments = $effectiveArguments
            }
        }
    }
}

function ConvertTo-ProcessArgumentString {
    param([string[]] $Arguments)

    (($Arguments | ForEach-Object {
        if ($_ -match '[\s"]') {
            '"' + ($_ -replace '"', '\"') + '"'
        }
        else {
            $_
        }
    }) -join ' ')
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

function Split-SqlTopLevelComma {
    param([string] $Text)

    $items = New-Object System.Collections.Generic.List[string]
    $start = 0
    $depth = 0
    $inSingleQuote = $false
    $inDoubleQuote = $false
    $inBacktick = $false

    for ($index = 0; $index -lt $Text.Length; $index++) {
        $char = $Text[$index]

        if ($char -eq "'" -and !$inDoubleQuote -and !$inBacktick) {
            $inSingleQuote = !$inSingleQuote
            continue
        }

        if ($char -eq '"' -and !$inSingleQuote -and !$inBacktick) {
            $inDoubleQuote = !$inDoubleQuote
            continue
        }

        if ($char -eq '`' -and !$inSingleQuote -and !$inDoubleQuote) {
            $inBacktick = !$inBacktick
            continue
        }

        if ($inSingleQuote -or $inDoubleQuote -or $inBacktick) {
            continue
        }

        if ($char -eq '(') {
            $depth++
            continue
        }

        if ($char -eq ')' -and $depth -gt 0) {
            $depth--
            continue
        }

        if ($char -eq ',' -and $depth -eq 0) {
            [void] $items.Add($Text.Substring($start, $index - $start))
            $start = $index + 1
        }
    }

    [void] $items.Add($Text.Substring($start))
    $items.ToArray()
}

function Convert-CreateTableColumnIdentifiers {
    param([string] $Block)

    $match = [regex]::Match($Block, '(?is)^(?<prefix>\s*CREATE\s+TABLE\s+(?:IF\s+NOT\s+EXISTS\s+)?(?:`[^`]+`|"[^"]+"|[A-Za-z0-9_]+)\s*\()(?<columns>.*)(?<suffix>\)\s*[^;]*;?\s*)$')
    if (!$match.Success) {
        return $Block
    }

    $definitions = @(Split-SqlTopLevelComma -Text $match.Groups['columns'].Value)
    $convertedDefinitions = New-Object System.Collections.Generic.List[string]
    foreach ($definition in $definitions) {
        $trimmed = $definition.TrimStart()
        if ($trimmed -match '^(?i)(PRIMARY|KEY|UNIQUE|INDEX|CONSTRAINT|FOREIGN|CHECK)\b') {
            [void] $convertedDefinitions.Add($definition)
            continue
        }

        if ($trimmed.StartsWith('`') -or $trimmed.StartsWith('"')) {
            [void] $convertedDefinitions.Add($definition)
            continue
        }

        if ($definition -match '^(\s*)([A-Za-z0-9_]+)(\s+.+)$') {
            [void] $convertedDefinitions.Add($Matches[1] + (Quote-MySqlIdentifier $Matches[2]) + $Matches[3])
            continue
        }

        [void] $convertedDefinitions.Add($definition)
    }

    $match.Groups['prefix'].Value + ($convertedDefinitions -join ',') + $match.Groups['suffix'].Value
}

function Convert-CreateTableBlockForMySqlCompatibility {
    param(
        [string[]] $Lines,
        [switch] $NormalizeIntegerColumns,
        [switch] $NormalizeWideVarcharColumns
    )

    $block = $Lines -join "`n"
    if ($NormalizeIntegerColumns) {
        $block = [regex]::Replace($block, '(?i)\bINTEGER\b', 'BIGINT')
    }

    if ($NormalizeWideVarcharColumns) {
        $varcharBytes = 0
        foreach ($match in [regex]::Matches($block, '(?i)\bVARCHAR\s*\(\s*(\d+)\s*\)')) {
            $varcharBytes += ([int] $match.Groups[1].Value) * 4
        }

        if ($varcharBytes -gt 60000) {
            $block = [regex]::Replace($block, '(?i)\bVARCHAR\s*\(\s*\d+\s*\)', 'TEXT')
        }
    }

    $block = Convert-CreateTableColumnIdentifiers -Block $block

    $block -split "`n"
}

function Convert-WorldDatabaseSqlLineForMySqlCompatibility {
    param([string] $Line)

    $columnMap = @{
        'activepropid'       = 'activePropId'
        'area'               = 'area'
        'creature'           = 'creature'
        'displayinfo'        = 'displayInfo'
        'eventid'            = 'eventId'
        'faction1'           = 'faction1'
        'faction2'           = 'faction2'
        'fx'                 = 'fx'
        'fy'                 = 'fy'
        'fz'                 = 'fz'
        'id'                 = 'id'
        'mapid'              = 'mapId'
        'mode'               = 'mode'
        'outfitinfo'         = 'outfitInfo'
        'phase'              = 'phase'
        'property'           = 'property'
        'questchecklistidx'  = 'questChecklistIdx'
        'rx'                 = 'rx'
        'ry'                 = 'ry'
        'rz'                 = 'rz'
        'scriptname'         = 'scriptName'
        'speed'              = 'speed'
        'splineid'           = 'splineId'
        'stat'               = 'stat'
        'team'               = 'team'
        'triggerid'          = 'triggerId'
        'type'               = 'type'
        'value'              = 'value'
        'world'              = 'world'
        'worldlocationid'    = 'worldLocationId'
        'worldsocketid'      = 'worldSocketId'
        'x'                  = 'x'
        'y'                  = 'y'
        'z'                  = 'z'
    }

    [regex]::Replace($Line, '`([A-Za-z0-9_]+)`', {
        param($match)

        $key = $match.Groups[1].Value.ToLowerInvariant()
        if ($columnMap.ContainsKey($key)) {
            return "``$($columnMap[$key])``"
        }

        $match.Value
    })
}

function Invoke-MySqlStreamImport {
    param(
        [string[]] $Arguments,
        [string] $InputFile,
        [string] $SelectedDatabase,
        [switch] $NormalizeIntegerColumns,
        [switch] $NormalizeWideVarcharColumns,
        [switch] $NormalizeWorldDatabaseColumns,
        [switch] $DisableIntegrityChecks
    )

    $streamArguments = @($Arguments)
    if ($SelectedDatabase) {
        $streamArguments += "--init-command=USE $(Quote-MySqlIdentifier $SelectedDatabase)"
    }

    $commandInvocation = Get-MySqlCommandInvocation -Arguments $streamArguments

    $process = New-Object System.Diagnostics.Process
    $process.StartInfo.FileName = $commandInvocation.FilePath

    $process.StartInfo.Arguments = ConvertTo-ProcessArgumentString -Arguments $commandInvocation.Arguments
    $process.StartInfo.UseShellExecute = $false
    $process.StartInfo.RedirectStandardInput = $true
    $process.StartInfo.RedirectStandardOutput = $true
    $process.StartInfo.RedirectStandardError = $true
    $process.StartInfo.CreateNoWindow = $true

    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    foreach ($propertyName in @('StandardInputEncoding', 'StandardOutputEncoding', 'StandardErrorEncoding')) {
        $encodingProperty = $process.StartInfo.GetType().GetProperty($propertyName)
        if ($encodingProperty) {
            $encodingProperty.SetValue($process.StartInfo, $utf8NoBom, $null)
        }
    }

    [void] $process.Start()
    $stdoutTask = $process.StandardOutput.ReadToEndAsync()
    $stderrTask = $process.StandardError.ReadToEndAsync()

    $writeError = $null
    $reader = New-Object System.IO.StreamReader($InputFile, [System.Text.Encoding]::UTF8, $true)
    try {
        try {
            if ($SelectedDatabase) {
                $quotedDatabase = Quote-MySqlIdentifier $SelectedDatabase
                $process.StandardInput.WriteLine("CREATE DATABASE IF NOT EXISTS $quotedDatabase CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;")
                $process.StandardInput.WriteLine("USE $quotedDatabase;")
            }

            $process.StandardInput.WriteLine("SET SESSION sql_mode='NO_ENGINE_SUBSTITUTION';")
            if ($DisableIntegrityChecks) {
                $process.StandardInput.WriteLine('SET FOREIGN_KEY_CHECKS=0;')
                $process.StandardInput.WriteLine('SET UNIQUE_CHECKS=0;')
            }
            else {
                $process.StandardInput.WriteLine('SET FOREIGN_KEY_CHECKS=1;')
                $process.StandardInput.WriteLine('SET UNIQUE_CHECKS=1;')
            }

            $createTableBlock = $null
            while (!$reader.EndOfStream) {
                $line = $reader.ReadLine()
                if (($NormalizeIntegerColumns -or $NormalizeWideVarcharColumns) -and $line -match '^\s*CREATE\s+TABLE\b') {
                    $createTableBlock = New-Object System.Collections.Generic.List[string]
                    [void] $createTableBlock.Add($line)

                    if ($line -match ';\s*$') {
                        foreach ($convertedLine in (Convert-CreateTableBlockForMySqlCompatibility -Lines $createTableBlock.ToArray() -NormalizeIntegerColumns:$NormalizeIntegerColumns -NormalizeWideVarcharColumns:$NormalizeWideVarcharColumns)) {
                            $process.StandardInput.WriteLine($convertedLine)
                        }
                        $createTableBlock = $null
                    }

                    continue
                }

                if ($null -ne $createTableBlock) {
                    [void] $createTableBlock.Add($line)
                    if ($line -match ';\s*$') {
                        foreach ($convertedLine in (Convert-CreateTableBlockForMySqlCompatibility -Lines $createTableBlock.ToArray() -NormalizeIntegerColumns:$NormalizeIntegerColumns -NormalizeWideVarcharColumns:$NormalizeWideVarcharColumns)) {
                            $process.StandardInput.WriteLine($convertedLine)
                        }
                        $createTableBlock = $null
                    }

                    continue
                }

                if ($NormalizeWorldDatabaseColumns) {
                    $line = Convert-WorldDatabaseSqlLineForMySqlCompatibility -Line $line
                }

                $process.StandardInput.WriteLine($line)
            }

            if ($null -ne $createTableBlock) {
                foreach ($convertedLine in (Convert-CreateTableBlockForMySqlCompatibility -Lines $createTableBlock.ToArray() -NormalizeIntegerColumns:$NormalizeIntegerColumns -NormalizeWideVarcharColumns:$NormalizeWideVarcharColumns)) {
                    $process.StandardInput.WriteLine($convertedLine)
                }
            }
        }
        catch {
            $writeError = $_
        }
    }
    finally {
        $reader.Dispose()
        try {
            $process.StandardInput.Close()
        }
        catch {
            $writeError = $_
        }
    }

    $process.WaitForExit()
    $stdoutTask.Wait()
    $stderrTask.Wait()

    $out = $stdoutTask.Result
    $err = $stderrTask.Result
    if ($process.ExitCode -ne 0) {
        throw "mysql failed with exit code $($process.ExitCode) while importing '$InputFile'.`n$err"
    }
    if ($writeError) {
        throw "mysql input stream failed while importing '$InputFile'.`n$writeError"
    }

    if ($out) {
        Write-Info $out
    }
    if ($err) {
        Write-Info $err
    }
}

function Invoke-MySql {
    param(
        [string[]] $Arguments,
        [string] $InputFile,
        [string] $SelectedDatabase,
        [switch] $NormalizeIntegerColumns,
        [switch] $NormalizeWideVarcharColumns,
        [switch] $NormalizeWorldDatabaseColumns,
        [switch] $DisableIntegrityChecks
    )

    $usePasswordEnvironment = $MySqlInvocationMode -eq 'Native'
    $oldMySqlPassword = $env:MYSQL_PWD
    if ($usePasswordEnvironment -and $RootPassword) {
        $env:MYSQL_PWD = $RootPassword
    }

    try {
        if ($InputFile) {
            Invoke-MySqlStreamImport `
                -Arguments $Arguments `
                -InputFile $InputFile `
                -SelectedDatabase $SelectedDatabase `
                -NormalizeIntegerColumns:$NormalizeIntegerColumns `
                -NormalizeWideVarcharColumns:$NormalizeWideVarcharColumns `
                -NormalizeWorldDatabaseColumns:$NormalizeWorldDatabaseColumns `
                -DisableIntegrityChecks:$DisableIntegrityChecks
        }
        else {
            $commandInvocation = Get-MySqlCommandInvocation -Arguments $Arguments
            $commandArguments = $commandInvocation.Arguments
            $output = & $commandInvocation.FilePath @commandArguments 2>&1
            if ($LASTEXITCODE -ne 0) {
                throw "mysql failed with exit code $LASTEXITCODE.`n$($output -join [Environment]::NewLine)"
            }

            $output = @($output | Where-Object { $_ -notmatch '^\s*mysql:\s+\[Warning\]\s+Using a password on the command line interface can be insecure\.' })
            $output
        }
    }
    finally {
        if ($usePasswordEnvironment) {
            if ($null -eq $oldMySqlPassword) {
                Remove-Item Env:MYSQL_PWD -ErrorAction SilentlyContinue
            }
            else {
                $env:MYSQL_PWD = $oldMySqlPassword
            }
        }
    }
}

function Invoke-MySqlSql {
    param(
        [string] $Sql,
        [string] $Database
    )

    $arguments = Get-MySqlArguments -Database $Database
    $arguments += "--execute=$Sql"
    Invoke-MySql -Arguments $arguments | Out-Null
}

function Invoke-MySqlScalar {
    param(
        [string] $Sql,
        [string] $Database
    )

    $arguments = Get-MySqlArguments -Database $Database
    $arguments += @('--batch', '--raw', '--skip-column-names', "--execute=$Sql")
    $output = Invoke-MySql -Arguments $arguments
    if (!$output) {
        return $null
    }

    ($output | Select-Object -First 1).ToString().Trim()
}

function Ensure-Database {
    param(
        [string] $Database,
        [switch] $DropFirst
    )

    $quotedDatabase = Quote-MySqlIdentifier $Database

    if ($DropFirst) {
        Write-Info "Dropping database $Database"
        Invoke-MySqlSql -Sql "DROP DATABASE IF EXISTS $quotedDatabase;"
    }

    Write-Info "Creating database $Database"
    Invoke-MySqlSql -Sql "CREATE DATABASE IF NOT EXISTS $quotedDatabase CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;"

    if (!$SkipDatabaseUser) {
        $quotedUser = Quote-MySqlString $DatabaseUser
        foreach ($hostName in @('localhost', '%')) {
            $quotedHost = Quote-MySqlString $hostName
            Invoke-MySqlSql -Sql "GRANT ALL PRIVILEGES ON $quotedDatabase.* TO $quotedUser@$quotedHost;"
        }
    }
}

function Ensure-DatabaseUser {
    if ($SkipDatabaseUser) {
        Write-Info 'Skipping MySQL/MariaDB application user creation.'
        return
    }

    $quotedUser = Quote-MySqlString $DatabaseUser
    $quotedPassword = Quote-MySqlString $DatabasePassword

    foreach ($hostName in @('localhost', '%')) {
        $quotedHost = Quote-MySqlString $hostName
        Invoke-MySqlSql -Sql @"
CREATE USER IF NOT EXISTS $quotedUser@$quotedHost IDENTIFIED BY $quotedPassword;
ALTER USER $quotedUser@$quotedHost IDENTIFIED BY $quotedPassword;
GRANT ALL PRIVILEGES ON *.* TO $quotedUser@$quotedHost;
"@
    }

    Invoke-MySqlSql -Sql 'FLUSH PRIVILEGES;'
}

function Ensure-ImportMarkerTable {
    param([string] $Database)

    Invoke-MySqlSql -Database $Database -Sql @"
CREATE TABLE IF NOT EXISTS __nexusforever_imports (
    file_name varchar(255) NOT NULL,
    file_hash char(64) NOT NULL,
    imported_at_utc datetime NOT NULL,
    PRIMARY KEY (file_name)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
"@
}

function Get-ImportMarkerHash {
    param(
        [string] $Database,
        [string] $FileName
    )

    $quotedFileName = Quote-MySqlString $FileName
    Invoke-MySqlScalar -Database $Database -Sql "SELECT file_hash FROM __nexusforever_imports WHERE file_name = $quotedFileName LIMIT 1;"
}

function Test-ImportMarkerExists {
    param(
        [string] $Database,
        [string] $FileName
    )

    $quotedFileName = Quote-MySqlString $FileName
    $count = Invoke-MySqlScalar -Database $Database -Sql "SELECT COUNT(*) FROM __nexusforever_imports WHERE file_name = $quotedFileName;"
    [int] $count -gt 0
}

function Get-ImportMarkerCountByPrefix {
    param(
        [string] $Database,
        [string] $Prefix
    )

    $quotedPrefix = Quote-MySqlString $Prefix
    $count = Invoke-MySqlScalar -Database $Database -Sql "SELECT COUNT(*) FROM __nexusforever_imports WHERE LEFT(file_name, CHAR_LENGTH($quotedPrefix)) = $quotedPrefix;"
    [int] $count
}

function Get-NonMarkerTableCount {
    param([string] $Database)

    $quotedDatabase = Quote-MySqlString $Database
    $count = Invoke-MySqlScalar -Sql "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = $quotedDatabase AND table_name <> '__nexusforever_imports';"
    [int] $count
}

function Test-MySqlTableExists {
    param(
        [string] $Database,
        [string] $TableName
    )

    $quotedDatabase = Quote-MySqlString $Database
    $quotedTableName = Quote-MySqlString $TableName
    $count = Invoke-MySqlScalar -Sql "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = $quotedDatabase AND table_name = $quotedTableName;"
    [int] $count -gt 0
}

function Get-SqlFileCreateTableNames {
    param([string] $SqlPath)

    $tableNames = New-Object System.Collections.Generic.List[string]
    $reader = New-Object System.IO.StreamReader($SqlPath, [System.Text.Encoding]::UTF8, $true)
    try {
        while (!$reader.EndOfStream) {
            $line = $reader.ReadLine()
            if ($line -match '^\s*CREATE\s+TABLE\s+(?:IF\s+NOT\s+EXISTS\s+)?(?:`([^`]+)`|"([^"]+)"|([A-Za-z0-9_]+))') {
                $tableName = $null
                foreach ($groupIndex in 1..3) {
                    if ($Matches.ContainsKey($groupIndex) -and ![string]::IsNullOrWhiteSpace($Matches[$groupIndex])) {
                        $tableName = $Matches[$groupIndex]
                        break
                    }
                }

                if ($tableName -and !$tableNames.Contains($tableName)) {
                    [void] $tableNames.Add($tableName)
                }
            }

            if ($tableNames.Count -gt 0 -and $line -match ';\s*$') {
                break
            }
        }
    }
    finally {
        $reader.Dispose()
    }

    $tableNames.ToArray()
}

function Repair-PartialSqlFileImport {
    param(
        [string] $SqlPath,
        [string] $Database,
        [string] $MarkerName
    )

    $tableNames = @(Get-SqlFileCreateTableNames -SqlPath $SqlPath)
    if ($tableNames.Count -eq 0) {
        return $false
    }

    $existingTableNames = @($tableNames | Where-Object { Test-MySqlTableExists -Database $Database -TableName $_ })
    if ($existingTableNames.Count -eq 0) {
        return $false
    }

    $quotedTableNames = @($existingTableNames | ForEach-Object { Quote-MySqlIdentifier $_ })
    Write-Info "Repairing partial import for $MarkerName by dropping existing table(s): $($existingTableNames -join ', ')"
    Invoke-MySqlSql -Database $Database -Sql "SET FOREIGN_KEY_CHECKS=0; DROP TABLE IF EXISTS $($quotedTableNames -join ', '); SET FOREIGN_KEY_CHECKS=1;"
    return $true
}

function Resolve-SetupPath {
    param([string] $Path)

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return $Path
    }

    Join-Path $RepoRoot $Path
}

function Get-SqlImportSource {
    param(
        [string] $SqlDirectory,
        [string] $FallbackSqlPath,
        [string] $Label
    )

    $directoryPath = Resolve-SetupPath $SqlDirectory
    if (![string]::IsNullOrWhiteSpace($SqlDirectory) -and (Test-Path -LiteralPath $directoryPath -PathType Container)) {
        $allFiles = @(Get-ChildItem -LiteralPath $directoryPath -File -Filter '*.sql' | Sort-Object Name)
        $files = @($allFiles | Where-Object { $_.Name -ne '.sql' })
        $skippedFiles = @($allFiles | Where-Object { $_.Name -eq '.sql' })

        if ($skippedFiles.Count -gt 0) {
            $skippedNames = ($skippedFiles | Select-Object -ExpandProperty Name) -join ', '
            Write-Warning "Skipping non table-specific SQL file(s) in $directoryPath`: $skippedNames"
        }

        if ($files.Count -gt 0) {
            return [pscustomobject]@{
                Mode            = 'Directory'
                Directory       = $directoryPath
                FallbackSqlPath = Resolve-SetupPath $FallbackSqlPath
                Files           = $files
            }
        }

        Write-Warning "$Label SQL directory exists but contains no importable .sql files: $directoryPath"
    }

    $fallbackPath = Resolve-SetupPath $FallbackSqlPath
    if (Test-Path -LiteralPath $fallbackPath -PathType Leaf) {
        return [pscustomobject]@{
            Mode            = 'File'
            Directory       = $null
            FallbackSqlPath = $fallbackPath
            Files           = @(Get-Item -LiteralPath $fallbackPath)
        }
    }

    throw "$Label SQL directory was not found or empty, and fallback SQL file was not found. Directory: $directoryPath. Fallback: $fallbackPath"
}

function Import-SqlFileWithMarker {
    param(
        [string] $SqlPath,
        [string] $Database,
        [string] $Label,
        [string] $MarkerName
    )

    if (!(Test-Path -LiteralPath $SqlPath -PathType Leaf)) {
        throw "$Label SQL file was not found: $SqlPath"
    }

    $file = Get-Item -LiteralPath $SqlPath
    Write-Info "Hashing $MarkerName"
    $hash = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    $existingHash = Get-ImportMarkerHash -Database $Database -FileName $MarkerName

    if ($existingHash -eq $hash -and !$ForceImport) {
        Write-Info "$MarkerName already imported into $Database; skipping."
        return
    }

    $needsImport = $ForceImport -or ($existingHash -ne $hash)
    if ($needsImport) {
        if ($RepairPartialLargeDumpDatabases -or $ForceImport) {
            [void] (Repair-PartialSqlFileImport -SqlPath $file.FullName -Database $Database -MarkerName $MarkerName)
        }
        else {
            $tableNames = @(Get-SqlFileCreateTableNames -SqlPath $file.FullName)
            $existingTableNames = @($tableNames | Where-Object { Test-MySqlTableExists -Database $Database -TableName $_ })
            if ($existingTableNames.Count -gt 0) {
                throw "SQL file '$MarkerName' has no matching import marker, but table(s) already exist in '$Database': $($existingTableNames -join ', '). Re-run with -RepairPartialLargeDumpDatabases to drop only those table(s) and re-import this file."
            }
        }
    }

    $normaliseIntegerColumns = !$KeepLargeDumpIntegerColumns
    $normaliseWideVarcharColumns = $true
    if ($normaliseIntegerColumns) {
        Write-Info "Importing $MarkerName into $Database with SQL compatibility normalisers. This can take a while."
    }
    else {
        Write-Info "Importing $MarkerName into $Database with wide VARCHAR compatibility normalised. This can take a while."
    }

    Invoke-MySql `
        -Arguments (Get-MySqlArguments) `
        -InputFile $file.FullName `
        -SelectedDatabase $Database `
        -NormalizeIntegerColumns:$normaliseIntegerColumns `
        -NormalizeWideVarcharColumns:$normaliseWideVarcharColumns `
        -DisableIntegrityChecks

    $quotedName = Quote-MySqlString $MarkerName
    $quotedHash = Quote-MySqlString $hash
    Invoke-MySqlSql -Database $Database -Sql "REPLACE INTO __nexusforever_imports (file_name, file_hash, imported_at_utc) VALUES ($quotedName, $quotedHash, UTC_TIMESTAMP());"
}

function Import-SqlFilesWithMarkers {
    param(
        [string] $SqlDirectory,
        [string] $FallbackSqlPath,
        [string] $Database,
        [string] $Label
    )

    $source = Get-SqlImportSource -SqlDirectory $SqlDirectory -FallbackSqlPath $FallbackSqlPath -Label $Label
    $files = @($source.Files)
    if ($source.Mode -eq 'Directory') {
        Write-Info "$Label import source: $($files.Count) SQL files from $($source.Directory)"
    }
    else {
        Write-Info "$Label import source: fallback SQL file $($source.FallbackSqlPath)"
    }

    Ensure-Database -Database $Database -DropFirst:$RecreateLargeDumpDatabases
    Ensure-ImportMarkerTable -Database $Database

    $markerPrefix = ''
    $existingImportMarkerCount = 0
    if ($source.Mode -eq 'Directory') {
        $markerPrefix = "$(Split-Path -Leaf $source.Directory)/"
        $existingImportMarkerCount = Get-ImportMarkerCountByPrefix -Database $Database -Prefix $markerPrefix
    }
    else {
        $singleMarkerName = (Split-Path -Leaf $source.FallbackSqlPath)
        if (Test-ImportMarkerExists -Database $Database -FileName $singleMarkerName) {
            $existingImportMarkerCount = 1
        }
    }

    $tableCount = Get-NonMarkerTableCount -Database $Database
    if ($tableCount -gt 0 -and !$ForceImport -and !$RecreateLargeDumpDatabases) {
        if ($existingImportMarkerCount -gt 0) {
            Write-Info "Database '$Database' already has $tableCount tables and $existingImportMarkerCount matching import marker(s); continuing file-by-file."
        }
        elseif ($source.Mode -eq 'Directory' -and (Test-ImportMarkerExists -Database $Database -FileName (Split-Path -Leaf $source.FallbackSqlPath))) {
            Write-Info "Database '$Database' was previously imported from $($source.FallbackSqlPath); leaving it intact. Use -RecreateLargeDumpDatabases once to rebuild it from $($source.Directory)."
            return
        }
        elseif ($RepairPartialLargeDumpDatabases) {
            Write-Info "Database '$Database' has $tableCount tables without matching import markers; recreating only this database."
            Ensure-Database -Database $Database -DropFirst
            Ensure-ImportMarkerTable -Database $Database
        }
        else {
            throw "Database '$Database' already has $tableCount tables and no matching import marker. Use -RepairPartialLargeDumpDatabases to recreate only incomplete reference DBs, -RecreateLargeDumpDatabases for a full clean import, or -ForceImport to import anyway."
        }
    }

    foreach ($file in $files) {
        $markerName = $file.Name
        if ($source.Mode -eq 'Directory') {
            $markerName = "$markerPrefix$($file.Name)"
        }

        Import-SqlFileWithMarker `
            -SqlPath $file.FullName `
            -Database $Database `
            -Label $Label `
            -MarkerName $markerName
    }
}

function Get-DatabaseConnectionString {
    param([string] $Database)

    "server=$MySqlHost;port=$MySqlPort;user=$DatabaseUser;password=$DatabasePassword;database=$Database"
}

function Get-BrokerConnectionString {
    $escapedUser = [Uri]::EscapeDataString($BrokerUser)
    $escapedPassword = [Uri]::EscapeDataString($BrokerPassword)
    "amqp://$escapedUser`:$escapedPassword@$BrokerHost`:$BrokerPort"
}

function Update-JsonNode {
    param([object] $Node)

    if ($null -eq $Node -or $Node -is [string]) {
        return
    }

    if ($Node -is [System.Array]) {
        foreach ($item in $Node) {
            Update-JsonNode -Node $item
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
            Update-JsonNode -Node $property.Value
        }
    }
}

function Update-ConfigJson {
    param(
        [string] $Path,
        [bool] $IsAspireMigrations
    )

    $json = Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
    Update-JsonNode -Node $json

    if ($IsAspireMigrations) {
        $json.DatabaseMigration = [pscustomobject]@{
            Skip = $false
        }
        $json.AccountCreation = [pscustomobject]@{
            Accounts = @(Get-AccountCreationConfigAccounts)
        }
        $json.WorldDatabase.Path = $WorldDatabasePath
    }

    $json | ConvertTo-Json -Depth 100 | Set-Content -LiteralPath $Path -Encoding utf8
}

function Copy-ConfigurationFiles {
    foreach ($item in $ConfigFiles) {
        $projectPath = Join-Path $RepoRoot "Source\$($item.Project)"
        $examplePath = Join-Path $projectPath $item.Example
        $targetPath = Join-Path $projectPath $item.Target

        if (!(Test-Path -LiteralPath $examplePath -PathType Leaf)) {
            Write-Warning "Missing example config: $examplePath"
            continue
        }

        if ((Test-Path -LiteralPath $targetPath -PathType Leaf) -and !$OverwriteConfig) {
            Write-Info "Keeping existing config $targetPath"
        }
        else {
            Copy-Item -LiteralPath $examplePath -Destination $targetPath -Force
            Update-ConfigJson -Path $targetPath -IsAspireMigrations:($item.Project -eq 'NexusForever.Aspire.Database.Migrations')
            Write-Info "Wrote config $targetPath"
        }

        $outputDirectory = Join-Path $projectPath "bin\$Configuration\$TargetFramework"
        if (Test-Path -LiteralPath $outputDirectory -PathType Container) {
            $outputPath = Join-Path $outputDirectory $item.Target
            if ((Test-Path -LiteralPath $outputPath -PathType Leaf) -and !$OverwriteConfig) {
                Write-Info "Keeping existing runtime config $outputPath"
            }
            else {
                Copy-Item -LiteralPath $targetPath -Destination $outputPath -Force
                Write-Info "Wrote runtime config $outputPath"
            }
        }
    }
}

function Invoke-AccountSeeder {
    $migrationExe = Join-Path $RepoRoot "Source\NexusForever.Aspire.Database.Migrations\bin\$Configuration\$TargetFramework\NexusForever.Aspire.Database.Migrations.exe"
    $skippedWorldDatabasePath = Join-Path $RepoRoot '.nexusforever-runtime\skip-world-database'

    if (!(Test-Path -LiteralPath $migrationExe -PathType Leaf)) {
        if ($SkipBuild) {
            Write-Warning "Database migration executable was not found, so local account creation was skipped: $migrationExe"
            return
        }

        throw "Database migration executable was not found: $migrationExe"
    }

    $environmentOverrides = @{
        'ConnectionStrings__authdb'       = Get-DatabaseConnectionString -Database $GameDatabases.Auth
        'ConnectionStrings__characterdb'  = Get-DatabaseConnectionString -Database $GameDatabases.Character
        'ConnectionStrings__worlddb'      = Get-DatabaseConnectionString -Database $GameDatabases.World
        'ConnectionStrings__groupdb'      = Get-DatabaseConnectionString -Database $GameDatabases.Group
        'ConnectionStrings__chatdb'       = Get-DatabaseConnectionString -Database $GameDatabases.Chat
        'ConnectionStrings__friendshipdb' = Get-DatabaseConnectionString -Database $GameDatabases.Friendship
        'DatabaseMigration__Skip'         = 'true'
        'WorldDatabase__Path'             = $skippedWorldDatabasePath
    }

    $accounts = @(Get-LocalLoginAccounts)
    for ($index = 0; $index -lt $accounts.Count; $index++) {
        $account = $accounts[$index]
        $environmentOverrides["AccountCreation__Accounts__${index}__UserName"] = $account.UserName
        $environmentOverrides["AccountCreation__Accounts__${index}__Password"] = $account.Password
        $environmentOverrides["AccountCreation__Accounts__${index}__RoleId"] = [string] $account.RoleId
    }

    Write-Section 'Local accounts'
    Invoke-WithTemporaryEnvironment -Variables $environmentOverrides -ScriptBlock {
        Push-Location (Split-Path -Parent $migrationExe)
        try {
            Write-Info "Running: $migrationExe"
            & $migrationExe
            if ($LASTEXITCODE -ne 0) {
                throw "$([System.IO.Path]::GetFileName($migrationExe)) failed with exit code $LASTEXITCODE."
            }
        }
        finally {
            Pop-Location
        }
    }
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

function Ensure-DotNetEf {
    $output = & dotnet ef --version 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Info "dotnet-ef available: $($output | Select-Object -First 1)"
        return
    }

    if (!$InstallDotNetEf) {
        throw "dotnet-ef is not available. Re-run with -InstallDotNetEf or install it manually with: dotnet tool install dotnet-ef --global"
    }

    Invoke-DotNet -Arguments @('tool', 'install', 'dotnet-ef', '--global')
}

function Invoke-EfMigration {
    param(
        [string] $ProjectDirectory,
        [string] $Context
    )

    Push-Location $ProjectDirectory
    try {
        $arguments = @('ef', 'database', 'update', '--configuration', $Configuration)
        if ($Context) {
            $arguments += @('--context', $Context)
        }

        Write-Info "Running dotnet $($arguments -join ' ') in $ProjectDirectory"
        Invoke-DotNet -Arguments $arguments
    }
    finally {
        Pop-Location
    }
}

function Test-DatabaseColumnExists {
    param(
        [string] $Database,
        [string] $Table,
        [string] $Column
    )

    $quotedTable = Quote-MySqlString $Table
    $quotedColumn = Quote-MySqlString $Column
    $count = Invoke-MySqlScalar -Database $Database -Sql "SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = $quotedTable AND column_name = $quotedColumn;"

    [int] $count -gt 0
}

function Repair-WorldDatabaseCompatibilityColumn {
    param(
        [string] $Table,
        [string] $OldColumn,
        [string] $NewColumn
    )

    $hasOldColumn = Test-DatabaseColumnExists -Database $GameDatabases.World -Table $Table -Column $OldColumn
    $hasNewColumn = Test-DatabaseColumnExists -Database $GameDatabases.World -Table $Table -Column $NewColumn
    if (!$hasOldColumn) {
        return
    }

    $quotedTable = Quote-MySqlIdentifier $Table
    $quotedOldColumn = Quote-MySqlIdentifier $OldColumn
    $quotedNewColumn = Quote-MySqlIdentifier $NewColumn
    if (!$hasNewColumn) {
        Write-Info "Repairing compatibility column $Table.$OldColumn to $NewColumn"
        Invoke-MySqlSql -Database $GameDatabases.World -Sql "ALTER TABLE $quotedTable CHANGE COLUMN $quotedOldColumn $quotedNewColumn float NOT NULL DEFAULT 0;"
        return
    }

    Write-Info "Dropping obsolete compatibility column $Table.$OldColumn"
    Invoke-MySqlSql -Database $GameDatabases.World -Sql "ALTER TABLE $quotedTable DROP COLUMN $quotedOldColumn;"
}

function Ensure-WorldDatabaseCompatibilityTables {
    Invoke-MySqlSql -Database $GameDatabases.World -Sql @'
CREATE TABLE IF NOT EXISTS entity_property (
    `id` int(10) unsigned NOT NULL DEFAULT 0,
    `property` int(10) unsigned NOT NULL DEFAULT 0,
    `value` float NOT NULL DEFAULT 0,
    PRIMARY KEY (`id`, `property`),
    CONSTRAINT FK__entity_property_id__entity_id
        FOREIGN KEY (`id`) REFERENCES entity (`id`)
        ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS creature_info_property (
    `id` int(10) unsigned NOT NULL DEFAULT 0,
    `property` int(10) unsigned NOT NULL DEFAULT 0,
    `value` float NOT NULL DEFAULT 0,
    PRIMARY KEY (`id`, `property`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
'@

    Repair-WorldDatabaseCompatibilityColumn -Table 'entity_property' -OldColumn 'alue' -NewColumn 'value'
    Repair-WorldDatabaseCompatibilityColumn -Table 'creature_info_property' -OldColumn 'alue' -NewColumn 'value'
}

function Get-WorldDatabaseSchemaColumns {
    $arguments = Get-MySqlArguments -Database $GameDatabases.World
    $arguments += @(
        '--batch',
        '--raw',
        '--skip-column-names',
        "--execute=SELECT table_name, column_name FROM information_schema.columns WHERE table_schema = DATABASE();"
    )

    $schema = @{}
    $output = Invoke-MySql -Arguments $arguments
    foreach ($line in $output) {
        if (!$line) {
            continue
        }

        $parts = $line -split "`t", 2
        if ($parts.Count -lt 2) {
            continue
        }

        $table = $parts[0].ToLowerInvariant()
        $column = $parts[1].ToLowerInvariant()
        if (!$schema.ContainsKey($table)) {
            $schema[$table] = New-Object 'System.Collections.Generic.HashSet[string]'
        }

        [void] $schema[$table].Add($column)
    }

    $schema
}

function Get-UnsupportedWorldSqlTargets {
    param(
        [System.IO.FileInfo] $File,
        [hashtable] $Schema
    )

    $unsupported = New-Object 'System.Collections.Generic.SortedSet[string]'
    $reader = New-Object System.IO.StreamReader($File.FullName, [System.Text.Encoding]::UTF8, $true)
    try {
        while (!$reader.EndOfStream) {
            $line = Convert-WorldDatabaseSqlLineForMySqlCompatibility -Line $reader.ReadLine()
            if ($line -notmatch '(?i)^\s*(?:INSERT\s+(?:IGNORE\s+)?|REPLACE\s+)INTO\s+`?(?<table>[A-Za-z0-9_]+)`?\s*\((?<columns>[^)]*)\)') {
                continue
            }

            $tableName = $Matches['table']
            $tableKey = $tableName.ToLowerInvariant()
            if (!$Schema.ContainsKey($tableKey)) {
                [void] $unsupported.Add($tableName)
                continue
            }

            foreach ($column in (Split-SqlTopLevelComma -Text $Matches['columns'])) {
                $columnName = $column.Trim()
                if (!$columnName) {
                    continue
                }

                $columnName = $columnName.Trim('`')
                $columnKey = $columnName.ToLowerInvariant()
                if (!$Schema[$tableKey].Contains($columnKey)) {
                    [void] $unsupported.Add("$tableName.$columnName")
                }
            }
        }
    }
    finally {
        $reader.Dispose()
    }

    $unsupported
}

function Get-WorldSqlImportMetadata {
    param([string] $SqlPath)

    $worldId = $null
    $eventId = $null
    $reader = New-Object System.IO.StreamReader($SqlPath, [System.Text.Encoding]::UTF8, $true)
    try {
        while (!$reader.EndOfStream -and (!$worldId -or !$eventId)) {
            $line = $reader.ReadLine()
            if (!$worldId -and $line -match '(?i)^\s*SET\s+@WORLD\s*=\s*(\d+)\s*;') {
                $worldId = $Matches[1]
            }
            elseif (!$eventId -and $line -match '(?i)^\s*SET\s+@EVENTID\s*=\s*(\d+)\s*;') {
                $eventId = $Matches[1]
            }
        }
    }
    finally {
        $reader.Dispose()
    }

    [pscustomobject]@{
        WorldId = $worldId
        EventId = $eventId
    }
}

function Repair-PartialWorldSqlImport {
    param([System.IO.FileInfo] $File)

    $metadata = Get-WorldSqlImportMetadata -SqlPath $File.FullName
    if (!$metadata.WorldId -and !$metadata.EventId) {
        return
    }

    $statements = New-Object System.Collections.Generic.List[string]
    if ($metadata.WorldId) {
        Write-Info "Preparing world SQL retry for $($File.Name) by clearing prior partial rows for world $($metadata.WorldId)."
        [void] $statements.Add("DELETE ep FROM entity_property ep LEFT JOIN entity e ON e.id = ep.id WHERE e.id IS NULL OR e.world = $($metadata.WorldId);")
        [void] $statements.Add("DELETE es FROM entity_script es LEFT JOIN entity e ON e.id = es.id WHERE e.id IS NULL OR e.world = $($metadata.WorldId);")
        [void] $statements.Add("DELETE ee FROM entity_event ee LEFT JOIN entity e ON e.id = ee.id WHERE e.id IS NULL OR e.world = $($metadata.WorldId);")
        [void] $statements.Add("DELETE st FROM entity_stats st LEFT JOIN entity e ON e.id = st.id WHERE e.id IS NULL OR e.world = $($metadata.WorldId);")
        [void] $statements.Add("DELETE sp FROM entity_spline sp LEFT JOIN entity e ON e.id = sp.id WHERE e.id IS NULL OR e.world = $($metadata.WorldId);")
        [void] $statements.Add("DELETE FROM entity WHERE world = $($metadata.WorldId);")
        [void] $statements.Add("DELETE FROM map_entrance WHERE mapId = $($metadata.WorldId);")
    }

    if ($metadata.EventId) {
        [void] $statements.Add("DELETE FROM entity_event WHERE eventId = $($metadata.EventId);")
    }

    Invoke-MySqlSql -Database $GameDatabases.World -Sql ($statements -join [Environment]::NewLine)
}

function Import-WorldDatabaseSqlFiles {
    if (!(Test-Path -LiteralPath $WorldDatabasePath -PathType Container)) {
        Write-Warning "World database path does not exist; skipping official world import: $WorldDatabasePath"
        return $false
    }

    $files = Get-ChildItem -LiteralPath $WorldDatabasePath -Recurse -Filter '*.sql' -File | Sort-Object FullName
    if (!$files) {
        Write-Warning "World database path contains no .sql files: $WorldDatabasePath"
        return $false
    }

    if ($CreateWorldDatabaseCompatibilityTables) {
        Write-Info 'Creating optional world database compatibility tables for branch-specific SQL.'
        Ensure-WorldDatabaseCompatibilityTables
    }

    $worldSchema = Get-WorldDatabaseSchemaColumns

    foreach ($file in $files) {
        $hash = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
        $quotedName = Quote-MySqlString $file.Name
        $quotedHash = Quote-MySqlString $hash
        $existing = Invoke-MySqlScalar -Database $GameDatabases.World -Sql "SELECT COUNT(*) FROM version WHERE fileName = $quotedName AND fileHash = $quotedHash;"

        if ([int] $existing -gt 0 -and !$ForceImport) {
            Write-Info "Skipping already imported world SQL $($file.Name)"
            continue
        }

        $unsupportedTargets = @(Get-UnsupportedWorldSqlTargets -File $file -Schema $worldSchema)
        if ($unsupportedTargets.Count -gt 0 -and !$ForceImport) {
            Write-Warning "Skipping unsupported world SQL $($file.FullName). Missing current schema target(s): $($unsupportedTargets -join ', '). This usually means the SQL belongs to another NexusForever branch; switch to the matching branch and rerun migrations, or use -CreateWorldDatabaseCompatibilityTables if you intentionally want the known raw compatibility tables."
            Repair-PartialWorldSqlImport -File $file
            continue
        }

        if ($unsupportedTargets.Count -gt 0) {
            Write-Warning "Forcing unsupported world SQL $($file.FullName). Missing current schema target(s): $($unsupportedTargets -join ', ')."
        }

        Write-Info "Importing world SQL $($file.FullName)"
        Repair-PartialWorldSqlImport -File $file
        Invoke-MySql -Arguments (Get-MySqlArguments -Database $GameDatabases.World) -InputFile $file.FullName -NormalizeWorldDatabaseColumns
        Invoke-MySqlSql -Database $GameDatabases.World -Sql "INSERT IGNORE INTO version (fileName, fileHash, appliedOn) VALUES ($quotedName, $quotedHash, UTC_TIMESTAMP(6));"
    }

    return $true
}

function Import-RuntimeWorldSeedSqlFile {
    if (!(Test-Path -LiteralPath $RuntimeWorldSeedPath -PathType Leaf)) {
        Write-Warning "Runtime world seed SQL does not exist; skipping DataMapping runtime seed import: $RuntimeWorldSeedPath"
        return
    }

    $file = Get-Item -LiteralPath $RuntimeWorldSeedPath
    $hash = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    $markerName = "DataMapping/$($file.Name)"
    $quotedName = Quote-MySqlString $markerName
    $quotedHash = Quote-MySqlString $hash
    $existing = Invoke-MySqlScalar -Database $GameDatabases.World -Sql "SELECT COUNT(*) FROM version WHERE fileName = $quotedName AND fileHash = $quotedHash;"

    if ([int] $existing -gt 0 -and !$ForceImport) {
        Write-Info "Skipping already imported runtime world seed $($file.Name)"
        return
    }

    Write-Info "Importing DataMapping runtime world seed $($file.FullName)"
    Invoke-MySql -Arguments (Get-MySqlArguments -Database $GameDatabases.World) -InputFile $file.FullName
    Invoke-MySqlSql -Database $GameDatabases.World -Sql "INSERT IGNORE INTO version (fileName, fileHash, appliedOn) VALUES ($quotedName, $quotedHash, UTC_TIMESTAMP(6));"
}

function Get-RabbitMqCtlCommandInvocation {
    param([string[]] $Arguments)

    switch ($RabbitMqCtlInvocationMode) {
        'DockerExec' {
            return [pscustomobject]@{
                FilePath  = $DockerCliResolved
                Arguments = @('exec', $RabbitMqDockerContainerName, 'rabbitmqctl') + $Arguments
            }
        }
        default {
            if ([string]::IsNullOrWhiteSpace($RabbitMqCtlResolved)) {
                return $null
            }

            return [pscustomobject]@{
                FilePath  = $RabbitMqCtlResolved
                Arguments = $Arguments
            }
        }
    }
}

function Invoke-RabbitMqCtl {
    param(
        [string[]] $Arguments,
        [switch] $IgnoreExitCode
    )

    $commandInvocation = Get-RabbitMqCtlCommandInvocation -Arguments $Arguments
    if ($null -eq $commandInvocation) {
        if ($IgnoreExitCode) {
            return [pscustomobject]@{
                ExitCode = 1
                Output   = @()
            }
        }

        throw "RabbitMQ CLI '$RabbitMqCtl' was not found."
    }

    $commandArguments = $commandInvocation.Arguments
    $output = & $commandInvocation.FilePath @commandArguments 2>&1
    $exitCode = $LASTEXITCODE
    if ($exitCode -ne 0 -and !$IgnoreExitCode) {
        throw "RabbitMQ CLI command failed with exit code $exitCode.`n$($output -join [Environment]::NewLine)"
    }

    [pscustomobject]@{
        ExitCode = $exitCode
        Output   = @($output)
    }
}

function Ensure-RabbitMqUser {
    if ($SkipRabbitMqUser) {
        Write-Info 'Skipping RabbitMQ user creation.'
        return
    }

    if ($RabbitMqCtlInvocationMode -eq 'Native') {
        $RabbitMqCtlResolved = Resolve-NexusSetupExecutablePath -Command $RabbitMqCtlResolved
    }

    if ($null -eq (Get-RabbitMqCtlCommandInvocation -Arguments @('help'))) {
        Write-Warning "RabbitMQ command '$RabbitMqCtl' was not found. Install RabbitMQ or pass -SkipRabbitMqUser."
        return
    }

    $usersOutput = Invoke-RabbitMqCtl -Arguments @('list_users')
    if ($usersOutput.ExitCode -ne 0) {
        throw "Failed to list RabbitMQ users.`n$($usersOutput.Output -join [Environment]::NewLine)"
    }

    if (($usersOutput.Output -join [Environment]::NewLine) -notmatch "(?m)^$([regex]::Escape($BrokerUser))\s") {
        Write-Info "Creating RabbitMQ user $BrokerUser"
        Invoke-RabbitMqCtl -Arguments @('add_user', $BrokerUser, $BrokerPassword) | Out-Null
    }
    else {
        Write-Info "RabbitMQ user $BrokerUser already exists."
    }

    Invoke-RabbitMqCtl -Arguments @('set_user_tags', $BrokerUser, 'administrator') | Out-Null
    Invoke-RabbitMqCtl -Arguments @('set_permissions', '-p', '/', $BrokerUser, '.*', '.*', '.*') | Out-Null
}

function Start-StandaloneServers {
    foreach ($server in $ServerProcesses) {
        $workingDirectory = Join-Path $RepoRoot "Source\$($server.Project)\bin\$Configuration\$TargetFramework"
        $exePath = Join-Path $workingDirectory $server.Exe

        if (!(Test-Path -LiteralPath $exePath -PathType Leaf)) {
            throw "Cannot start $($server.Name); executable was not found: $exePath"
        }

        Write-Info "Starting $($server.Name)"
        Start-Process -FilePath $exePath -WorkingDirectory $workingDirectory -WindowStyle Hidden | Out-Null
    }
}

$RepoRoot = (Resolve-Path -LiteralPath $RepoRoot).Path
$WorldDatabasePath = Resolve-WorldDatabasePath -Path $WorldDatabasePath -RepositoryRoot $RepoRoot
$RuntimeWorldSeedPath = Resolve-RuntimeWorldSeedPath -Path $RuntimeWorldSeedPath -RepositoryRoot $RepoRoot

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
    -BrokerHost $BrokerHost `
    -BrokerPort $BrokerPort `
    -BrokerUser $BrokerUser `
    -BrokerPassword $BrokerPassword `
    -RabbitMqCtl $RabbitMqCtl

$DockerCliResolved = $dependencyBootstrap.DockerCliResolved
$MySqlHost = $dependencyBootstrap.MySqlHost
$MySqlPort = $dependencyBootstrap.MySqlPort
$RootUser = $dependencyBootstrap.RootUser
$RootPassword = $dependencyBootstrap.RootPassword
$ShouldPromptForRootPassword = $dependencyBootstrap.ShouldPromptForRootPassword
$MySqlInvocationMode = $dependencyBootstrap.MySqlInvocationMode
$MySqlDockerContainerName = $dependencyBootstrap.MySqlDockerContainerName
$MySqlDockerClientHost = $dependencyBootstrap.MySqlDockerClientHost
$MySqlDockerImage = $dependencyBootstrap.MySqlDockerImage
$MySqlProvisionedThisRun = $dependencyBootstrap.MySqlProvisionedThisRun
$BrokerHost = $dependencyBootstrap.BrokerHost
$BrokerPort = $dependencyBootstrap.BrokerPort
$RabbitMqCtlResolved = $dependencyBootstrap.RabbitMqCtlResolved
$RabbitMqCtlInvocationMode = $dependencyBootstrap.RabbitMqCtlInvocationMode
$RabbitMqDockerContainerName = $dependencyBootstrap.RabbitMqDockerContainerName
$RabbitMqProvisionedThisRun = $dependencyBootstrap.RabbitMqProvisionedThisRun

if ($ShouldPromptForRootPassword) {
    $RootPassword = ConvertFrom-SecureStringToPlainText (Read-Host 'MySQL/MariaDB root password' -AsSecureString)
}

if ($MySqlInvocationMode -eq 'Native') {
    try {
        $MySqlExe = Resolve-MySqlClient -Command $MySqlExe
    }
    catch {
        if ($DependencyMode -eq 'ExternalOnly') {
            throw
        }

        $DockerCliResolved = Get-NexusSetupDockerCli -Command $DockerCli
        if (!$DockerCliResolved) {
            throw
        }

        Write-Info 'mysql client was not found locally; using a transient MariaDB Docker client.'
        $MySqlInvocationMode = 'DockerClient'
        $MySqlDockerClientHost = Convert-NexusSetupHostForDockerClient -HostName $MySqlHost
        $MySqlDockerImage = $PortableMariaDbImage
    }
}

Write-Section 'MySQL/MariaDB user and databases'
Ensure-DatabaseUser
foreach ($database in $GameDatabases.Values) {
    Ensure-Database -Database $database
}

if (!$SkipLargeDumpImports) {
    Write-Section 'Reference SQL imports'
    Import-SqlFilesWithMarkers `
        -SqlDirectory $JabbitholeSqlDirectory `
        -FallbackSqlPath $JabbitholeSql `
        -Database $JabbitholeDatabase `
        -Label 'Jabbithole'

    Import-SqlFilesWithMarkers `
        -SqlDirectory $WildstarClientSqlDirectory `
        -FallbackSqlPath $WildstarClientSql `
        -Database $WildstarClientDatabase `
        -Label 'WildStar client'
}

Write-Section 'RabbitMQ user'
Ensure-RabbitMqUser

if (!$SkipConfigCopy) {
    Write-Section 'Configuration files'
    Copy-ConfigurationFiles
}

if (!$SkipBuild) {
    Write-Section 'Build'
    Assert-Command -Command 'dotnet' -InstallHint 'Install the .NET SDK required by this repository.'
    Invoke-DotNet -Arguments @('build', (Join-Path $RepoRoot 'Source\NexusForever.slnx'), '--configuration', $Configuration)

    if (!$SkipConfigCopy) {
        Copy-ConfigurationFiles
    }
}

if (!$SkipMigrations) {
    Write-Section 'Entity Framework migrations'
    Assert-Command -Command 'dotnet' -InstallHint 'Install the .NET SDK required by this repository.'
    Ensure-DotNetEf

    $worldServerProject = Join-Path $RepoRoot 'Source\NexusForever.WorldServer'
    Invoke-EfMigration -ProjectDirectory $worldServerProject -Context 'AuthContext'
    Invoke-EfMigration -ProjectDirectory $worldServerProject -Context 'CharacterContext'
    Invoke-EfMigration -ProjectDirectory $worldServerProject -Context 'WorldContext'

    Invoke-EfMigration -ProjectDirectory (Join-Path $RepoRoot 'Source\NexusForever.Server.ChatServer') -Context 'ChatContext'
    Invoke-EfMigration -ProjectDirectory (Join-Path $RepoRoot 'Source\NexusForever.Server.GroupServer') -Context 'GroupContext'
    Invoke-EfMigration -ProjectDirectory (Join-Path $RepoRoot 'Source\NexusForever.Server.Friendship') -Context 'FriendshipContext'
}

Invoke-AccountSeeder

if (!$SkipWorldDatabaseImport) {
    Write-Section 'Official world database import'
    $worldDatabaseSqlAvailable = Import-WorldDatabaseSqlFiles

    if (!$SkipRuntimeWorldSeedImport) {
        if ($worldDatabaseSqlAvailable) {
            Write-Section 'DataMapping runtime world seed import'
            Import-RuntimeWorldSeedSqlFile
        }
        else {
            Write-Warning 'Skipping DataMapping runtime world seed import because no official world SQL files were imported or available.'
        }
    }
}
elseif (!$SkipRuntimeWorldSeedImport) {
    Write-Warning 'Skipping DataMapping runtime world seed import because -SkipWorldDatabaseImport was set.'
}

if ($StartServers) {
    Write-Section 'Starting standalone servers'
    Start-StandaloneServers
}

Write-Section 'Done'
Write-Host 'NexusForever setup/import automation completed.' -ForegroundColor Green
Write-Host 'Local login accounts:' -ForegroundColor Green
Write-LocalAccountSummary
