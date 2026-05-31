#requires -Version 5.1
<#
Shared helpers for WildStar client command-line arguments used by NexusForever setup scripts.

Evidence-backed switch names and behavior are documented in Decomp/Analysis/CLIENT_LOGGING.md.
#>

Set-StrictMode -Version Latest

function ConvertTo-WildStarClientLogLevelNumber {
    param(
        [Parameter(Mandatory = $true)]
        [ValidateSet('Error', 'Warn', 'Info', 'Debug', 'Trace')]
        [string] $Level
    )

    switch ($Level) {
        'Error' { return 0 }
        'Warn' { return 1 }
        'Info' { return 2 }
        'Debug' { return 3 }
        'Trace' { return 4 }
    }

    throw "Unsupported client log level: $Level"
}

function Get-WildStarClientLoggingArguments {
    param(
        [switch] $EnableClientLogging,
        [ValidateSet('Error', 'Warn', 'Info', 'Debug', 'Trace')]
        [string] $ClientLogLevel = 'Trace',
        [switch] $ClientLogStdout,
        [string] $ClientLogDir = ''
    )

    if (-not $EnableClientLogging) {
        return @()
    }

    $arguments = @(
        '-logFile',
        '-logFlush',
        '-logDefaultLevel',
        (ConvertTo-WildStarClientLogLevelNumber -Level $ClientLogLevel).ToString()
    )

    if ($ClientLogStdout) {
        $arguments += '-logStdout'
    }

    if (![string]::IsNullOrWhiteSpace($ClientLogDir)) {
        $arguments += @('-logDir', $ClientLogDir)
    }

    return $arguments
}

function Get-WildStarConfiguredClientArguments {
    param([string] $ClientDirectory)

    if ([string]::IsNullOrWhiteSpace($ClientDirectory)) {
        return @()
    }

    $configPath = Join-Path $ClientDirectory 'config.json'
    if (!(Test-Path -LiteralPath $configPath -PathType Leaf)) {
        return @()
    }

    try {
        $config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json -ErrorAction Stop
    }
    catch {
        Write-Warning "Failed to read staged client arguments from $configPath. $($_.Exception.Message)"
        return @()
    }

    if (($null -eq $config) -or ($null -eq $config.ExtraArguments)) {
        return @()
    }

    @($config.ExtraArguments | Where-Object { ![string]::IsNullOrWhiteSpace($_) } | Select-Object -Unique)
}

function Get-WildStarRequestedClientArguments {
    param(
        [string[]] $ClientArguments = @(),
        [switch] $EnableClientConsole,
        [switch] $EnableClientLogging,
        [ValidateSet('Error', 'Warn', 'Info', 'Debug', 'Trace')]
        [string] $ClientLogLevel = 'Trace',
        [switch] $ClientLogStdout,
        [string] $ClientLogDir = ''
    )

    $effectiveArguments = @($ClientArguments | Where-Object { ![string]::IsNullOrWhiteSpace($_) })

    if ($EnableClientConsole) {
        $effectiveArguments += '-Console'
    }

    $effectiveArguments += @(Get-WildStarClientLoggingArguments `
        -EnableClientLogging:$EnableClientLogging `
        -ClientLogLevel $ClientLogLevel `
        -ClientLogStdout:$ClientLogStdout `
        -ClientLogDir $ClientLogDir)

    @($effectiveArguments | Select-Object -Unique)
}

function Test-WildStarClientArgumentOverrideRequested {
    param([hashtable] $BoundParameters)

    foreach ($parameterName in @(
            'ClientArguments',
            'EnableClientConsole',
            'EnableClientLogging',
            'ClientLogLevel',
            'ClientLogStdout',
            'ClientLogDir')) {
        if ($BoundParameters.ContainsKey($parameterName)) {
            return $true
        }
    }

    return $false
}

function Get-WildStarEffectiveClientArguments {
    param(
        [string] $ClientDirectory,
        [string[]] $ClientArguments = @(),
        [switch] $EnableClientConsole,
        [switch] $EnableClientLogging,
        [ValidateSet('Error', 'Warn', 'Info', 'Debug', 'Trace')]
        [string] $ClientLogLevel = 'Trace',
        [switch] $ClientLogStdout,
        [string] $ClientLogDir = '',
        [bool] $ExplicitOverrideRequested = $false
    )

    $requestedArguments = @(Get-WildStarRequestedClientArguments `
        -ClientArguments $ClientArguments `
        -EnableClientConsole:$EnableClientConsole `
        -EnableClientLogging:$EnableClientLogging `
        -ClientLogLevel $ClientLogLevel `
        -ClientLogStdout:$ClientLogStdout `
        -ClientLogDir $ClientLogDir)

    if ($ExplicitOverrideRequested) {
        return $requestedArguments
    }

    $configuredArguments = @(Get-WildStarConfiguredClientArguments -ClientDirectory $ClientDirectory)
    if ($configuredArguments.Count -gt 0) {
        return $configuredArguments
    }

    return $requestedArguments
}

function Get-WildStarClientLogPaths {
    param([string] $ClientDirectory)

    if ([string]::IsNullOrWhiteSpace($ClientDirectory)) {
        return [pscustomobject]@{
            InstallRoot = ''
            Logs        = ''
            Errors      = ''
        }
    }

    $installRoot = Split-Path -Parent $ClientDirectory
    [pscustomobject]@{
        InstallRoot = $installRoot
        Logs        = Join-Path $installRoot 'Logs'
        Errors      = Join-Path $installRoot 'Errors'
    }
}

function Write-WildStarClientLoggingHint {
    param([string] $ClientDirectory)

    $logPaths = Get-WildStarClientLogPaths -ClientDirectory $ClientDirectory
    if ([string]::IsNullOrWhiteSpace($logPaths.InstallRoot)) {
        return
    }

    Write-Host "Client CLog files: $($logPaths.Logs)" -ForegroundColor Gray
    Write-Host "Client error reports: $($logPaths.Errors)" -ForegroundColor Gray
}
