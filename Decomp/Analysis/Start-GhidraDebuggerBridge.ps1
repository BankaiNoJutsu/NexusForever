[CmdletBinding()]
param(
    [string] $GhidraRoot = 'I:\ghidra_12.1_PUBLIC',
    [string] $GhidraMcpSourceRoot,
    [string] $Python,
    [string[]] $PythonArgs = @(),
    [string] $VenvPath,
    [string] $HostAddress = '127.0.0.1',
    [int] $Port = 8099,
    [string] $ExportsDir,
    [ValidateSet('DEBUG', 'INFO', 'WARNING', 'ERROR')]
    [string] $LogLevel = 'INFO',
    [switch] $UseVenv,
    [switch] $CreateVenv,
    [switch] $InstallRequirements,
    [switch] $ValidateOnly,
    [switch] $AllowNonLoopback
)

$ErrorActionPreference = 'Stop'

function Resolve-Directory {
    param(
        [Parameter(Mandatory)] [string] $Path,
        [Parameter(Mandatory)] [string] $Name
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Container)) {
        throw "$Name directory not found: $Path"
    }

    return (Resolve-Path -LiteralPath $Path).Path
}

function Add-ExistingPath {
    param(
        [System.Collections.Generic.List[string]] $Paths,
        [string] $Path
    )

    if (-not $Path) {
        return
    }

    if (Test-Path -LiteralPath $Path -PathType Container) {
        $resolved = (Resolve-Path -LiteralPath $Path).Path
        if (-not $Paths.Contains($resolved)) {
            $Paths.Add($resolved) | Out-Null
        }
    }
}

function Get-DebuggerSourceRoot {
    param(
        [Parameter(Mandatory)] [string] $Root
    )

    $directPackage = Join-Path $Root 'debugger\__main__.py'
    if (Test-Path -LiteralPath $directPackage -PathType Leaf) {
        return $Root
    }

    $directModule = Join-Path $Root 'debugger.py'
    if (Test-Path -LiteralPath $directModule -PathType Leaf) {
        return $Root
    }

    if ((Split-Path -Leaf $Root) -eq 'debugger') {
        $packageMain = Join-Path $Root '__main__.py'
        if (Test-Path -LiteralPath $packageMain -PathType Leaf) {
            return (Resolve-Path -LiteralPath (Split-Path -Parent $Root)).Path
        }
    }

    return $null
}

function Find-DebuggerSourceRoot {
    param(
        [string] $ExplicitRoot,
        [Parameter(Mandatory)] [string] $InstallRoot
    )

    $candidates = New-Object 'System.Collections.Generic.List[string]'
    if ($ExplicitRoot) {
        $candidates.Add($ExplicitRoot) | Out-Null
    }

    @(
        $InstallRoot,
        (Join-Path $InstallRoot 'ghidra-mcp'),
        (Join-Path $InstallRoot 'GhidraMCP'),
        'I:\ghidra-mcp',
        'I:\GIT\ghidra-mcp',
        (Join-Path $env:USERPROFILE 'ghidra-mcp'),
        (Join-Path $env:USERPROFILE 'source\repos\ghidra-mcp')
    ) | ForEach-Object {
        if ($_ -and -not $candidates.Contains($_)) {
            $candidates.Add($_) | Out-Null
        }
    }

    foreach ($candidate in $candidates) {
        if (-not (Test-Path -LiteralPath $candidate -PathType Container)) {
            continue
        }

        $root = Get-DebuggerSourceRoot -Root ((Resolve-Path -LiteralPath $candidate).Path)
        if ($root) {
            return $root
        }
    }

    $found = Get-ChildItem -LiteralPath $InstallRoot -Recurse -Filter __main__.py -ErrorAction SilentlyContinue |
        Where-Object { $_.Directory -and $_.Directory.Name -eq 'debugger' } |
        Select-Object -First 1

    if ($found) {
        return (Resolve-Path -LiteralPath (Split-Path -Parent $found.DirectoryName)).Path
    }

    return $null
}

function Invoke-SelectedPython {
    param(
        [Parameter(Mandatory)] [string[]] $Arguments
    )

    $allArguments = @()
    $allArguments += $script:SelectedPythonArgs
    $allArguments += $Arguments
    & $script:SelectedPython @allArguments
}

function Test-TcpPort {
    param(
        [Parameter(Mandatory)] [string] $HostName,
        [Parameter(Mandatory)] [int] $PortNumber
    )

    $client = [System.Net.Sockets.TcpClient]::new()
    try {
        $connect = $client.BeginConnect($HostName, $PortNumber, $null, $null)
        if (-not $connect.AsyncWaitHandle.WaitOne(500)) {
            return $false
        }

        $client.EndConnect($connect)
        return $true
    }
    catch {
        return $false
    }
    finally {
        $client.Close()
    }
}

$loopbackHosts = @('127.0.0.1', 'localhost', '::1')
if (-not $AllowNonLoopback -and -not $loopbackHosts.Contains($HostAddress)) {
    throw "Refusing to bind debugger bridge to non-loopback host '$HostAddress'. Re-run with -AllowNonLoopback if this is intentional."
}

$resolvedGhidraRoot = Resolve-Directory -Path $GhidraRoot -Name 'Ghidra root'
$bridgePath = Join-Path $resolvedGhidraRoot 'bridge_mcp_ghidra.py'
if (-not (Test-Path -LiteralPath $bridgePath -PathType Leaf)) {
    Write-Warning "bridge_mcp_ghidra.py was not found under $resolvedGhidraRoot. The debugger server can still start, but the MCP bridge must be configured separately."
}

if (-not $UseVenv -and
    -not $CreateVenv -and
    -not $PSBoundParameters.ContainsKey('Python') -and
    -not $PSBoundParameters.ContainsKey('PythonArgs')) {
    $defaultVenvPath = Join-Path $resolvedGhidraRoot '.venv-ghidra-debugger'
    $defaultVenvPython = Join-Path $defaultVenvPath 'Scripts\python.exe'
    if (Test-Path -LiteralPath $defaultVenvPython -PathType Leaf) {
        $VenvPath = $defaultVenvPath
        $UseVenv = $true
    }
}

if (-not $Python) {
    if (Get-Command py -ErrorAction SilentlyContinue) {
        $Python = 'py'
        if (-not $PSBoundParameters.ContainsKey('PythonArgs')) {
            $PythonArgs = @('-3')
        }
    }
    else {
        $Python = 'python'
    }
}

$script:SelectedPython = $Python
$script:SelectedPythonArgs = $PythonArgs

$resolvedVenvPath = $null
if ($UseVenv -or $CreateVenv) {
    if (-not $VenvPath) {
        $VenvPath = Join-Path $resolvedGhidraRoot '.venv-ghidra-debugger'
    }

    if ($CreateVenv -and -not (Test-Path -LiteralPath $VenvPath -PathType Container)) {
        Write-Host "Creating debugger venv: $VenvPath"
        Invoke-SelectedPython -Arguments @('-m', 'venv', $VenvPath)
    }

    $venvPython = Join-Path $VenvPath 'Scripts\python.exe'
    if (-not (Test-Path -LiteralPath $venvPython -PathType Leaf)) {
        throw "Debugger venv Python not found: $venvPython. Re-run with -CreateVenv or choose a different -VenvPath."
    }

    $resolvedVenvPath = (Resolve-Path -LiteralPath $VenvPath).Path
    $script:SelectedPython = (Resolve-Path -LiteralPath $venvPython).Path
    $script:SelectedPythonArgs = @()
}

$debuggerSourceRoot = Find-DebuggerSourceRoot -ExplicitRoot $GhidraMcpSourceRoot -InstallRoot $resolvedGhidraRoot

$pythonPathEntries = New-Object 'System.Collections.Generic.List[string]'
Add-ExistingPath -Paths $pythonPathEntries -Path $debuggerSourceRoot
Add-ExistingPath -Paths $pythonPathEntries -Path (Join-Path $resolvedGhidraRoot 'Ghidra\Debug\Debugger-rmi-trace\pypkg\src')
Add-ExistingPath -Paths $pythonPathEntries -Path (Join-Path $resolvedGhidraRoot 'Ghidra\Debug\Debugger-agent-dbgeng\pypkg\src')

$previousPythonPath = $env:PYTHONPATH
if ($pythonPathEntries.Count -gt 0) {
    $env:PYTHONPATH = ($pythonPathEntries + @($previousPythonPath | Where-Object { $_ })) -join [System.IO.Path]::PathSeparator
}

@(
    (Join-Path ${env:ProgramFiles(x86)} 'Windows Kits\10\Debuggers\x64'),
    (Join-Path $env:ProgramFiles 'Windows Kits\10\Debuggers\x64')
) | ForEach-Object {
    if ($_ -and (Test-Path -LiteralPath $_ -PathType Container)) {
        $env:PATH = "$_;$env:PATH"
    }
}

$env:GHIDRA_HOME = $resolvedGhidraRoot
$env:GHIDRA_INSTALL_DIR = $resolvedGhidraRoot
$env:GHIDRA_DEBUGGER_HOST = $HostAddress
$env:GHIDRA_DEBUGGER_PORT = [string] $Port
$env:GHIDRA_DEBUGGER_URL = ('http://{0}:{1}' -f $HostAddress, $Port)

if ($InstallRequirements) {
    $traceWheel = Get-ChildItem -LiteralPath (Join-Path $resolvedGhidraRoot 'Ghidra\Debug\Debugger-rmi-trace\pypkg\dist') -Filter 'ghidratrace-*.whl' -ErrorAction SilentlyContinue |
        Sort-Object Name -Descending |
        Select-Object -First 1

    if ($traceWheel) {
        Invoke-SelectedPython -Arguments @('-m', 'pip', 'install', '--force-reinstall', $traceWheel.FullName)
    }
    else {
        Write-Warning "No ghidratrace wheel found under $resolvedGhidraRoot."
    }

    $requirementsCandidates = @(
        $(if ($debuggerSourceRoot) { Join-Path $debuggerSourceRoot 'requirements-debugger.txt' }),
        (Join-Path $resolvedGhidraRoot 'requirements-debugger.txt')
    ) | Where-Object { $_ }

    $requirementsPath = $requirementsCandidates | Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } | Select-Object -First 1
    if ($requirementsPath) {
        Invoke-SelectedPython -Arguments @('-m', 'pip', 'install', '-r', $requirementsPath)
    }
    else {
        Write-Warning "requirements-debugger.txt was not found. Pass -GhidraMcpSourceRoot pointing at a ghidra-mcp checkout if optional debugger dependencies need installation."
    }
}

$probeCode = @'
import importlib.util
import sys

spec = importlib.util.find_spec('debugger')
if spec is None:
    print('MISSING_DEBUGGER_MODULE')
    sys.exit(2)
print(spec.origin or ','.join(str(p) for p in (spec.submodule_search_locations or [])))
'@

$previousErrorActionPreference = $ErrorActionPreference
$ErrorActionPreference = 'Continue'
try {
    $probeOutput = Invoke-SelectedPython -Arguments @('-c', $probeCode) 2>&1
    $probeExitCode = $LASTEXITCODE
}
finally {
    $ErrorActionPreference = $previousErrorActionPreference
}

if ($probeExitCode -ne 0) {
    $details = ($probeOutput | Out-String).Trim()
    throw @"
Could not import the Python module 'debugger'.

The MCP bridge at $bridgePath proxies debugger_* calls to $env:GHIDRA_DEBUGGER_URL and expects the standalone ghidra-mcp debugger package to be runnable with 'python -m debugger'.

Searched:
$($pythonPathEntries -join [Environment]::NewLine)

Install or clone ghidra-mcp so the directory containing 'debugger\__main__.py' is available, then re-run with -GhidraMcpSourceRoot <path>. If dependencies are missing, add -InstallRequirements.

Python reported:
$details
"@
}

$serverProbeCode = @'
import importlib

importlib.import_module('debugger.server')
print('debugger.server import ok')
'@

$previousErrorActionPreference = $ErrorActionPreference
$ErrorActionPreference = 'Continue'
try {
    $serverProbeOutput = Invoke-SelectedPython -Arguments @('-c', $serverProbeCode) 2>&1
    $serverProbeExitCode = $LASTEXITCODE
}
finally {
    $ErrorActionPreference = $previousErrorActionPreference
}

if ($serverProbeExitCode -ne 0) {
    $details = ($serverProbeOutput | Out-String).Trim()
    throw @"
Could not import 'debugger.server'.

This usually means the optional Windows debugger dependencies are missing from the selected Python. Re-run with -InstallRequirements, preferably with -CreateVenv so pybag/comtypes/protobuf are installed into a local debugger venv.

Python reported:
$details
"@
}

$url = $env:GHIDRA_DEBUGGER_URL
if (Test-TcpPort -HostName $HostAddress -PortNumber $Port) {
    Write-Host "A process is already listening on $url."
    try {
        $status = Invoke-WebRequest -UseBasicParsing -Uri "$url/debugger/status" -TimeoutSec 2
        Write-Host "Debugger status endpoint responded with HTTP $($status.StatusCode)."
    }
    catch {
        Write-Warning "The port is open, but /debugger/status did not respond as expected: $($_.Exception.Message)"
    }

    return
}

$serverArgs = @('-m', 'debugger', '--host', $HostAddress, '--port', [string] $Port, '--log-level', $LogLevel)
if ($ExportsDir) {
    $resolvedExportsDir = Resolve-Directory -Path $ExportsDir -Name 'Exports'
    $serverArgs += @('--exports-dir', $resolvedExportsDir)
}

Write-Host "Ghidra root: $resolvedGhidraRoot"
if ($resolvedVenvPath) {
    Write-Host "Debugger venv: $resolvedVenvPath"
}
Write-Host "Debugger package: $($probeOutput | Select-Object -First 1)"
Write-Host "Debugger URL: $url"

if ($HostAddress -ne '127.0.0.1' -or $Port -ne 8099) {
    Write-Warning "The running MCP bridge must have GHIDRA_DEBUGGER_URL set to $url before it starts."
}

if ($ValidateOnly) {
    Write-Host "Validation passed. Re-run without -ValidateOnly to start the foreground debugger bridge."
    return
}

Write-Host "Starting debugger bridge. Leave this PowerShell session open while attaching."
Invoke-SelectedPython -Arguments $serverArgs
