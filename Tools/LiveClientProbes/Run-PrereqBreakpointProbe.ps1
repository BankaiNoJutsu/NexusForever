param(
    [int] $Seconds = 120,
    [string] $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path,
    [string] $ArtifactDirectory = '',
    [string] $ConnectorPath = 'I:\WildStar\Client64\NexusForever.ClientConnector.exe',
    [string] $OutputPath = ''
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($ArtifactDirectory)) {
    $ArtifactDirectory = Join-Path $RepoRoot 'artifacts\live-prereq-debug'
}

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $ArtifactDirectory 'prereq_probe_elevated.jsonl'
}

$harness = Join-Path $PSScriptRoot 'prereq_breakpoints.py'

if (!(Test-Path -LiteralPath $harness)) {
    throw "Breakpoint harness was not found: $harness"
}

$wildStar = Get-Process WildStar64 -ErrorAction SilentlyContinue | Select-Object -First 1
if ($null -eq $wildStar) {
    if (!(Test-Path -LiteralPath $ConnectorPath)) {
        throw "WildStar64 is not running and connector was not found: $ConnectorPath"
    }

    Start-Process -FilePath $ConnectorPath -WorkingDirectory (Split-Path -Parent $ConnectorPath) | Out-Null
    Start-Sleep -Seconds 10
    $wildStar = Get-Process WildStar64 -ErrorAction SilentlyContinue | Select-Object -First 1
}

if ($null -eq $wildStar) {
    throw 'WildStar64 is not running after connector launch.'
}

New-Item -ItemType Directory -Force -Path (Split-Path -Parent $OutputPath) | Out-Null

Write-Host "Attaching to WildStar64 PID $($wildStar.Id) for $Seconds seconds."
Write-Host "Output: $OutputPath"
Write-Host 'Trigger a dye account-item claim/use and a Holo-Wardrobe account-item claim/use while this runs.'

Push-Location $RepoRoot
try {
    python $harness --pid $wildStar.Id --seconds $Seconds --out $OutputPath
}
finally {
    Pop-Location
}
