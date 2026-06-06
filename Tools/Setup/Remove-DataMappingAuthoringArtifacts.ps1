#requires -Version 5.1
<#
.SYNOPSIS
Removes DataMapping authoring-only database artifacts from a local MySQL server.

.DESCRIPTION
Clean runtime deployments should not need nf_map_* tables, the DataMapping
authoring database, or the Jabbithole/WildStar reference databases. This helper
prints the affected artifacts by default and only removes them when -Apply is
passed.
#>

[Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSAvoidUsingPlainTextForPassword', '')]
[CmdletBinding()]
param(
    [string] $MySqlExe = 'mysql',
    [string] $MySqlHost = '127.0.0.1',
    [int] $MySqlPort = 3306,
    [string] $RootUser = 'root',
    [string] $RootPassword = $env:MYSQL_PWD,
    [switch] $PromptForRootPassword,

    [string] $WorldDatabase = 'nexus_forever_world',
    [string] $MappingDatabase = 'nexus_forever_mapping',
    [string] $JabbitholeDatabase = 'jabbithole',
    [string] $WildstarClientDatabase = 'wildstar_client',

    [switch] $Apply
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

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
    $arguments = @(
        "--host=$MySqlHost",
        "--port=$MySqlPort",
        "--user=$RootUser",
        '--default-character-set=utf8mb4',
        '--batch'
    )

    if (![string]::IsNullOrEmpty($RootPassword)) {
        $arguments += "--password=$RootPassword"
    }

    $arguments
}

function Invoke-MySqlSql {
    param([string] $Sql)

    $output = $Sql | & $MySqlExe @(Get-MySqlArguments) 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "mysql exited with code $LASTEXITCODE.`n$($output -join [Environment]::NewLine)"
    }

    $output
}

if ($PromptForRootPassword) {
    $RootPassword = ConvertFrom-SecureStringToPlainText (Read-Host 'MySQL/MariaDB root password' -AsSecureString)
}

$worldDatabaseString = Quote-MySqlString $WorldDatabase
$mappingDatabaseIdentifier = Quote-MySqlIdentifier $MappingDatabase
$jabbitholeDatabaseIdentifier = Quote-MySqlIdentifier $JabbitholeDatabase
$wildstarClientDatabaseIdentifier = Quote-MySqlIdentifier $WildstarClientDatabase

$previewSql = @"
SELECT 'world_nf_map_tables' AS artifact, COUNT(*) AS count
FROM information_schema.tables
WHERE table_schema = $worldDatabaseString
  AND table_name LIKE 'nf\_map\_%' ESCAPE '\\'
UNION ALL
SELECT 'mapping_database_present', COUNT(*)
FROM information_schema.schemata
WHERE schema_name = $(Quote-MySqlString $MappingDatabase)
UNION ALL
SELECT 'jabbithole_database_present', COUNT(*)
FROM information_schema.schemata
WHERE schema_name = $(Quote-MySqlString $JabbitholeDatabase)
UNION ALL
SELECT 'wildstar_client_database_present', COUNT(*)
FROM information_schema.schemata
WHERE schema_name = $(Quote-MySqlString $WildstarClientDatabase);
"@

Write-Host 'DataMapping authoring artifact preview:' -ForegroundColor Cyan
Invoke-MySqlSql -Sql $previewSql | ForEach-Object { Write-Host $_ }

if (!$Apply) {
    Write-Host 'Dry run only. Re-run with -Apply to remove these authoring artifacts.' -ForegroundColor Yellow
    return
}

$cleanupSql = @"
SET SESSION group_concat_max_len = 1048576;
SET @nf_world_database = $worldDatabaseString;
SET @drop_nf_map_tables = (
  SELECT IFNULL(
    CONCAT(
      'DROP TABLE ',
      GROUP_CONCAT(
        CONCAT(
          CHAR(96), REPLACE(table_schema, CHAR(96), CONCAT(CHAR(96), CHAR(96))), CHAR(96),
          '.',
          CHAR(96), REPLACE(table_name, CHAR(96), CONCAT(CHAR(96), CHAR(96))), CHAR(96)
        )
        ORDER BY table_name
        SEPARATOR ', '
      )
    ),
    'DO 0'
  )
  FROM information_schema.tables
  WHERE table_schema = @nf_world_database
    AND table_name LIKE 'nf\_map\_%' ESCAPE '\\'
);
PREPARE dropNfMapTablesStatement FROM @drop_nf_map_tables;
EXECUTE dropNfMapTablesStatement;
DEALLOCATE PREPARE dropNfMapTablesStatement;

DROP DATABASE IF EXISTS $mappingDatabaseIdentifier;
DROP DATABASE IF EXISTS $jabbitholeDatabaseIdentifier;
DROP DATABASE IF EXISTS $wildstarClientDatabaseIdentifier;
"@

Invoke-MySqlSql -Sql $cleanupSql | ForEach-Object { Write-Host $_ }
Write-Host 'DataMapping authoring artifacts removed.' -ForegroundColor Green
