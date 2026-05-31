function Test-NexusSetupTcpEndpoint {
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

function Test-NexusSetupLocalHostName {
    param([string] $HostName)

    if ([string]::IsNullOrWhiteSpace($HostName)) {
        return $true
    }

    switch ($HostName.Trim().ToLowerInvariant()) {
        'localhost' { return $true }
        '127.0.0.1' { return $true }
        '::1' { return $true }
        '0.0.0.0' { return $true }
        default { return $false }
    }
}

function Test-NexusSetupPortAvailable {
    param([int] $Port)

    $listener = New-Object System.Net.Sockets.TcpListener([System.Net.IPAddress]::Loopback, $Port)
    try {
        $listener.Start()
        return $true
    }
    catch {
        return $false
    }
    finally {
        try {
            $listener.Stop()
        }
        catch {
        }
    }
}

function Find-NexusSetupAvailablePort {
    param(
        [int] $PreferredPort,
        [string] $Label
    )

    $startPort = [Math]::Max($PreferredPort, 1024)
    if ($PreferredPort -gt 0 -and (Test-NexusSetupPortAvailable -Port $PreferredPort)) {
        return $PreferredPort
    }

    for ($candidate = $startPort; $candidate -lt ($startPort + 100); $candidate++) {
        if (Test-NexusSetupPortAvailable -Port $candidate) {
            if ($candidate -ne $PreferredPort) {
                Write-Info "Using ${Label} port $candidate because port $PreferredPort is unavailable."
            }

            return $candidate
        }
    }

    throw "Could not find an available TCP port for $Label near $PreferredPort."
}

function Resolve-NexusSetupExecutablePath {
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

function Invoke-NexusSetupExternalCommand {
    param(
        [string] $FilePath,
        [string[]] $Arguments = @(),
        [switch] $IgnoreExitCode
    )

    $previousErrorActionPreference = $ErrorActionPreference
    $previousNativeErrorActionPreference = $null
    if ($IgnoreExitCode) {
        $ErrorActionPreference = 'Continue'
        if (Get-Variable -Name PSNativeCommandUseErrorActionPreference -Scope Global -ErrorAction SilentlyContinue) {
            $previousNativeErrorActionPreference = $global:PSNativeCommandUseErrorActionPreference
            $global:PSNativeCommandUseErrorActionPreference = $false
        }
    }

    try {
        $output = & $FilePath @Arguments 2>&1 | ForEach-Object {
            if ($_ -is [System.Management.Automation.ErrorRecord]) {
                $_.Exception.Message
            }
            else {
                $_
            }
        }
        $exitCode = $LASTEXITCODE

        if ($exitCode -ne 0 -and !$IgnoreExitCode) {
            throw "$([System.IO.Path]::GetFileName($FilePath)) failed with exit code $exitCode.`n$($output -join [Environment]::NewLine)"
        }

        [pscustomobject]@{
            ExitCode = $exitCode
            Output   = @($output)
        }
    }
    finally {
        $ErrorActionPreference = $previousErrorActionPreference
        if ($null -ne $previousNativeErrorActionPreference) {
            $global:PSNativeCommandUseErrorActionPreference = $previousNativeErrorActionPreference
        }
    }
}

function Test-NexusSetupDockerDaemonReady {
    param([string] $DockerCli)

    $probe = Invoke-NexusSetupExternalCommand -FilePath $DockerCli -Arguments @('info', '--format', '{{.ServerVersion}}') -IgnoreExitCode
    return $probe.ExitCode -eq 0 -and $probe.Output.Count -gt 0 -and ![string]::IsNullOrWhiteSpace([string] $probe.Output[0])
}

function Find-NexusSetupDockerDesktopExecutable {
    $candidates = @(
        (Join-Path $env:ProgramFiles 'Docker\Docker\Docker Desktop.exe'),
        (Join-Path ${env:ProgramFiles(x86)} 'Docker\Docker\Docker Desktop.exe'),
        (Join-Path $env:LOCALAPPDATA 'Docker\Docker Desktop.exe')
    )

    foreach ($candidate in $candidates) {
        if ($candidate -and (Test-Path -LiteralPath $candidate)) {
            return (Resolve-Path -LiteralPath $candidate).Path
        }
    }

    return $null
}

function Wait-NexusSetupDockerDaemonReady {
    param(
        [string] $DockerCli,
        [int] $TimeoutSeconds = 180
    )

    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    while ([DateTime]::UtcNow -lt $deadline) {
        if (Test-NexusSetupDockerDaemonReady -DockerCli $DockerCli) {
            return $true
        }

        Start-Sleep -Seconds 3
    }

    return $false
}

function Start-NexusSetupDockerDesktopIfNeeded {
    param(
        [string] $DockerCli,
        [int] $TimeoutSeconds = 180
    )

    if (Test-NexusSetupDockerDaemonReady -DockerCli $DockerCli) {
        return $true
    }

    if ([System.Environment]::OSVersion.Platform -ne [System.PlatformID]::Win32NT) {
        return $false
    }

    $desktopExe = Find-NexusSetupDockerDesktopExecutable
    if (!$desktopExe) {
        return $false
    }

    Write-Info 'Docker daemon is not running; starting Docker Desktop.'
    Start-Process -FilePath $desktopExe -WindowStyle Hidden | Out-Null

    if (!(Wait-NexusSetupDockerDaemonReady -DockerCli $DockerCli -TimeoutSeconds $TimeoutSeconds)) {
        throw "Docker Desktop was started but the Docker daemon did not become ready within $TimeoutSeconds seconds."
    }

    Write-Info 'Docker daemon is ready.'
    return $true
}

function Get-NexusSetupDockerCli {
    param(
        [string] $Command,
        [switch] $StartDesktopIfNeeded
    )

    $resolved = Resolve-NexusSetupExecutablePath -Command $Command
    if (!$resolved) {
        return $null
    }

    if ($StartDesktopIfNeeded) {
        Start-NexusSetupDockerDesktopIfNeeded -DockerCli $resolved | Out-Null
    }

    if (!(Test-NexusSetupDockerDaemonReady -DockerCli $resolved)) {
        return $null
    }

    return $resolved
}

function Get-NexusSetupStableSuffix {
    param([string] $Text)

    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $hashBytes = $sha.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($Text.ToLowerInvariant()))
        (($hashBytes | Select-Object -First 6 | ForEach-Object { $_.ToString('x2') }) -join '')
    }
    finally {
        $sha.Dispose()
    }
}

function Get-NexusSetupPortableNames {
    param([string] $RepoRoot)

    $suffix = Get-NexusSetupStableSuffix -Text $RepoRoot

    [pscustomobject]@{
        MySqlContainer    = "nexusforever-mysql-$suffix"
        MySqlVolume       = "nexusforever-mysql-data-$suffix"
        RabbitMqContainer = "nexusforever-rabbitmq-$suffix"
        RabbitMqVolume    = "nexusforever-rabbitmq-data-$suffix"
    }
}

function Get-NexusSetupPortableMySqlRootPassword {
    'nexusforever-root'
}

function Get-NexusSetupDockerInspect {
    param(
        [string] $DockerCli,
        [string] $ContainerName
    )

    $inspect = Invoke-NexusSetupExternalCommand -FilePath $DockerCli -Arguments @('inspect', $ContainerName) -IgnoreExitCode
    if ($inspect.ExitCode -ne 0) {
        return $null
    }

    $json = $inspect.Output -join [Environment]::NewLine
    $objects = $json | ConvertFrom-Json
    if ($objects -is [System.Array]) {
        return $objects[0]
    }

    return $objects
}

function Get-NexusSetupDockerPublishedPort {
    param(
        [object] $Inspect,
        [int] $ContainerPort
    )

    if ($null -eq $Inspect -or $null -eq $Inspect.NetworkSettings -or $null -eq $Inspect.NetworkSettings.Ports) {
        return $null
    }

    $property = $Inspect.NetworkSettings.Ports.PSObject.Properties | Where-Object {
        $_.Name -eq "$ContainerPort/tcp"
    } | Select-Object -First 1

    if ($null -eq $property -or $null -eq $property.Value -or $property.Value.Count -eq 0) {
        return $null
    }

    [int] $property.Value[0].HostPort
}

function Wait-NexusSetupMariaDbReady {
    param(
        [string] $DockerCli,
        [string] $ContainerName,
        [string] $RootUser,
        [string] $RootPassword,
        [int] $TimeoutSeconds = 120
    )

    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    while ([DateTime]::UtcNow -lt $deadline) {
        $probe = Invoke-NexusSetupExternalCommand -FilePath $DockerCli -Arguments @(
            'exec',
            $ContainerName,
            'mariadb-admin',
            'ping',
            '--host=127.0.0.1',
            "--user=$RootUser",
            "--password=$RootPassword",
            '--silent'
        ) -IgnoreExitCode

        if ($probe.ExitCode -eq 0) {
            return
        }

        Start-Sleep -Seconds 1
    }

    throw "Portable MariaDB container '$ContainerName' did not become ready within $TimeoutSeconds seconds."
}

function Wait-NexusSetupRabbitMqReady {
    param(
        [string] $DockerCli,
        [string] $ContainerName,
        [int] $TimeoutSeconds = 120
    )

    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    while ([DateTime]::UtcNow -lt $deadline) {
        $probe = Invoke-NexusSetupExternalCommand -FilePath $DockerCli -Arguments @(
            'exec',
            $ContainerName,
            'rabbitmq-diagnostics',
            '-q',
            'ping'
        ) -IgnoreExitCode

        if ($probe.ExitCode -eq 0) {
            return
        }

        Start-Sleep -Seconds 1
    }

    throw "Portable RabbitMQ container '$ContainerName' did not become ready within $TimeoutSeconds seconds."
}

function Repair-NexusSetupRabbitMqVolumePermissions {
    param(
        [string] $DockerCli,
        [string] $ContainerName,
        [string] $VolumeName,
        [string] $Image
    )

    Write-Info "Repairing portable RabbitMQ volume permissions for $ContainerName"

    $inspect = Get-NexusSetupDockerInspect -DockerCli $DockerCli -ContainerName $ContainerName
    if ($inspect -and $inspect.State.Running) {
        Invoke-NexusSetupExternalCommand -FilePath $DockerCli -Arguments @('stop', $ContainerName) | Out-Null
    }

    Invoke-NexusSetupExternalCommand -FilePath $DockerCli -Arguments @(
        'run',
        '--rm',
        '--volume', "${VolumeName}:/var/lib/rabbitmq",
        '--user', 'root',
        '--entrypoint', 'sh',
        $Image,
        '-lc',
        'chown -R rabbitmq:rabbitmq /var/lib/rabbitmq && if [ -f /var/lib/rabbitmq/.erlang.cookie ]; then chmod 600 /var/lib/rabbitmq/.erlang.cookie; fi'
    ) | Out-Null
}

function Ensure-NexusSetupPortableMariaDb {
    param(
        [string] $DockerCli,
        [string] $ContainerName,
        [string] $VolumeName,
        [string] $Image,
        [int] $HostPort,
        [string] $RootUser,
        [string] $RootPassword
    )

    $created = $false
    $inspect = Get-NexusSetupDockerInspect -DockerCli $DockerCli -ContainerName $ContainerName

    if ($inspect) {
        $publishedPort = Get-NexusSetupDockerPublishedPort -Inspect $inspect -ContainerPort 3306
        if ($publishedPort) {
            $HostPort = $publishedPort
        }

        if (!$inspect.State.Running) {
            Write-Info "Starting portable MariaDB container $ContainerName"
            Invoke-NexusSetupExternalCommand -FilePath $DockerCli -Arguments @('start', $ContainerName) | Out-Null
        }
        else {
            Write-Info "Reusing portable MariaDB container $ContainerName on 127.0.0.1:$HostPort"
        }
    }
    else {
        Write-Info "Starting portable MariaDB container $ContainerName on 127.0.0.1:$HostPort"
        Invoke-NexusSetupExternalCommand -FilePath $DockerCli -Arguments @(
            'run',
            '--detach',
            '--name', $ContainerName,
            '--publish', "${HostPort}:3306",
            '--env', "MARIADB_ROOT_PASSWORD=$RootPassword",
            '--volume', "${VolumeName}:/var/lib/mysql",
            $Image,
            '--character-set-server=utf8mb4',
            '--collation-server=utf8mb4_unicode_ci'
        ) | Out-Null
        $created = $true
    }

    Wait-NexusSetupMariaDbReady -DockerCli $DockerCli -ContainerName $ContainerName -RootUser $RootUser -RootPassword $RootPassword

    [pscustomobject]@{
        ContainerName     = $ContainerName
        HostPort          = $HostPort
        ProvisionedThisRun = $created
    }
}

function Ensure-NexusSetupPortableRabbitMq {
    param(
        [string] $DockerCli,
        [string] $ContainerName,
        [string] $VolumeName,
        [string] $Image,
        [int] $HostPort,
        [string] $BrokerUser,
        [string] $BrokerPassword
    )

    $created = $false
    $inspect = Get-NexusSetupDockerInspect -DockerCli $DockerCli -ContainerName $ContainerName

    if ($inspect) {
        $publishedPort = Get-NexusSetupDockerPublishedPort -Inspect $inspect -ContainerPort 5672
        if ($publishedPort) {
            $HostPort = $publishedPort
        }

        if (!$inspect.State.Running) {
            Write-Info "Starting portable RabbitMQ container $ContainerName"
            Invoke-NexusSetupExternalCommand -FilePath $DockerCli -Arguments @('start', $ContainerName) | Out-Null
        }
        else {
            Write-Info "Reusing portable RabbitMQ container $ContainerName on localhost:$HostPort"
        }
    }
    else {
        Write-Info "Starting portable RabbitMQ container $ContainerName on localhost:$HostPort"
        Invoke-NexusSetupExternalCommand -FilePath $DockerCli -Arguments @(
            'run',
            '--detach',
            '--name', $ContainerName,
            '--hostname', $ContainerName,
            '--publish', "${HostPort}:5672",
            '--env', "RABBITMQ_DEFAULT_USER=$BrokerUser",
            '--env', "RABBITMQ_DEFAULT_PASS=$BrokerPassword",
            '--volume', "${VolumeName}:/var/lib/rabbitmq",
            $Image
        ) | Out-Null
        $created = $true
    }

    Repair-NexusSetupRabbitMqVolumePermissions -DockerCli $DockerCli -ContainerName $ContainerName -VolumeName $VolumeName -Image $Image
    $inspect = Get-NexusSetupDockerInspect -DockerCli $DockerCli -ContainerName $ContainerName
    if (!$inspect.State.Running) {
        Write-Info "Starting portable RabbitMQ container $ContainerName after volume permission repair"
        Invoke-NexusSetupExternalCommand -FilePath $DockerCli -Arguments @('start', $ContainerName) | Out-Null
    }

    Wait-NexusSetupRabbitMqReady -DockerCli $DockerCli -ContainerName $ContainerName

    [pscustomobject]@{
        ContainerName     = $ContainerName
        HostPort          = $HostPort
        ProvisionedThisRun = $created
    }
}

function Convert-NexusSetupHostForDockerClient {
    param([string] $HostName)

    if (Test-NexusSetupLocalHostName -HostName $HostName) {
        return 'host.docker.internal'
    }

    return $HostName
}

function Resolve-NexusSetupDependencies {
    param(
        [string] $RepoRoot,
        [ValidateSet('Auto', 'ExternalOnly', 'PortableDocker')]
        [string] $DependencyMode,
        [string] $DockerCli,
        [string] $PortableMariaDbImage,
        [string] $PortableRabbitMqImage,
        [string] $MySqlExe,
        [string] $MySqlHost,
        [int] $MySqlPort,
        [string] $RootUser,
        [string] $RootPassword,
        [switch] $PromptForRootPassword,
        [string] $BrokerHost,
        [int] $BrokerPort,
        [string] $BrokerUser,
        [string] $BrokerPassword,
        [string] $RabbitMqCtl
    )

    $portableNames = Get-NexusSetupPortableNames -RepoRoot $RepoRoot
    $dockerCliResolved = $null
    $dockerWasChecked = $false

    $resolved = [ordered]@{
        DependencyMode                = $DependencyMode
        DockerCliResolved             = $DockerCli
        MySqlHost                     = $MySqlHost
        MySqlPort                     = $MySqlPort
        MySqlExeResolved              = $MySqlExe
        RootUser                      = $RootUser
        RootPassword                  = $RootPassword
        ShouldPromptForRootPassword   = [bool] $PromptForRootPassword
        MySqlInvocationMode           = 'Native'
        MySqlDockerContainerName      = ''
        MySqlDockerClientHost         = ''
        MySqlDockerImage              = $PortableMariaDbImage
        MySqlProvisionedThisRun       = $false
        BrokerHost                    = $BrokerHost
        BrokerPort                    = $BrokerPort
        RabbitMqCtlResolved           = $RabbitMqCtl
        RabbitMqCtlInvocationMode     = 'Native'
        RabbitMqDockerContainerName   = ''
        RabbitMqProvisionedThisRun    = $false
    }

    function Get-ResolvedDockerCli {
        if (!$dockerWasChecked) {
            $script:dockerCliResolved = Get-NexusSetupDockerCli -Command $DockerCli -StartDesktopIfNeeded
            $script:dockerWasChecked = $true
        }

        $script:dockerCliResolved
    }

    $forcePortable = $DependencyMode -eq 'PortableDocker'
    $allowPortable = $DependencyMode -ne 'ExternalOnly'

    if ($forcePortable) {
        $resolved.MySqlHost = '127.0.0.1'
        $resolved.BrokerHost = 'localhost'
    }

    $mysqlReachable = $false
    if (!$forcePortable) {
        $mysqlReachable = Test-NexusSetupTcpEndpoint -Address $resolved.MySqlHost -Port $resolved.MySqlPort
    }

    if ($mysqlReachable) {
        Write-Info "MySQL/MariaDB server is reachable on $($resolved.MySqlHost):$($resolved.MySqlPort)"
    }
    elseif ($allowPortable -and (Test-NexusSetupLocalHostName -HostName $resolved.MySqlHost)) {
        $dockerCliResolved = Get-ResolvedDockerCli
        if (!$dockerCliResolved) {
            throw "MySQL/MariaDB is not reachable on $($resolved.MySqlHost):$($resolved.MySqlPort). Start an existing server there, or install Docker Desktop so the setup scripts can provision a portable MariaDB instance automatically."
        }

        $portablePort = Find-NexusSetupAvailablePort -PreferredPort $resolved.MySqlPort -Label 'portable MariaDB'
        $portableState = Ensure-NexusSetupPortableMariaDb `
            -DockerCli $dockerCliResolved `
            -ContainerName $portableNames.MySqlContainer `
            -VolumeName $portableNames.MySqlVolume `
            -Image $PortableMariaDbImage `
            -HostPort $portablePort `
            -RootUser 'root' `
            -RootPassword (Get-NexusSetupPortableMySqlRootPassword)

        $resolved.DockerCliResolved = $dockerCliResolved
        $resolved.MySqlHost = '127.0.0.1'
        $resolved.MySqlPort = $portableState.HostPort
        $resolved.RootUser = 'root'
        $resolved.RootPassword = Get-NexusSetupPortableMySqlRootPassword
        $resolved.ShouldPromptForRootPassword = $false
        $resolved.MySqlInvocationMode = 'DockerExec'
        $resolved.MySqlDockerContainerName = $portableState.ContainerName
        $resolved.MySqlProvisionedThisRun = $portableState.ProvisionedThisRun
    }
    else {
        throw "MySQL/MariaDB is not reachable on $($resolved.MySqlHost):$($resolved.MySqlPort). Portable provisioning only supports local endpoints; update -MySqlHost/-MySqlPort to a reachable server, or switch back to the default local host for portable mode."
    }

    $brokerReachable = $false
    if (!$forcePortable) {
        $brokerReachable = Test-NexusSetupTcpEndpoint -Address $resolved.BrokerHost -Port $resolved.BrokerPort
    }

    if ($brokerReachable) {
        Write-Info "RabbitMQ broker is reachable on $($resolved.BrokerHost):$($resolved.BrokerPort)"
        $resolved.RabbitMqCtlResolved = Resolve-NexusSetupExecutablePath -Command $RabbitMqCtl
    }
    elseif ($allowPortable -and (Test-NexusSetupLocalHostName -HostName $resolved.BrokerHost)) {
        $dockerCliResolved = Get-ResolvedDockerCli
        if (!$dockerCliResolved) {
            throw "RabbitMQ is not reachable on $($resolved.BrokerHost):$($resolved.BrokerPort). Start an existing broker there, or install Docker Desktop so the setup scripts can provision a portable RabbitMQ instance automatically."
        }

        $portablePort = Find-NexusSetupAvailablePort -PreferredPort $resolved.BrokerPort -Label 'portable RabbitMQ'
        $portableState = Ensure-NexusSetupPortableRabbitMq `
            -DockerCli $dockerCliResolved `
            -ContainerName $portableNames.RabbitMqContainer `
            -VolumeName $portableNames.RabbitMqVolume `
            -Image $PortableRabbitMqImage `
            -HostPort $portablePort `
            -BrokerUser $BrokerUser `
            -BrokerPassword $BrokerPassword

        $resolved.DockerCliResolved = $dockerCliResolved
        $resolved.BrokerHost = 'localhost'
        $resolved.BrokerPort = $portableState.HostPort
        $resolved.RabbitMqCtlInvocationMode = 'DockerExec'
        $resolved.RabbitMqDockerContainerName = $portableState.ContainerName
        $resolved.RabbitMqProvisionedThisRun = $portableState.ProvisionedThisRun
    }
    else {
        throw "RabbitMQ is not reachable on $($resolved.BrokerHost):$($resolved.BrokerPort). Portable provisioning only supports local endpoints; update -BrokerHost/-BrokerPort to a reachable broker, or switch back to the default local host for portable mode."
    }

    [pscustomobject] $resolved
}
