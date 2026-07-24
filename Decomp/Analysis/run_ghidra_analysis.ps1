[CmdletBinding()]
param(
    [string] $ClientDir = (Join-Path $PSScriptRoot '..\Client64'),
    [string] $OutputDir = (Join-Path $PSScriptRoot 'exports'),
    [string] $ProjectDir = (Join-Path $PSScriptRoot 'ghidra_projects'),
    [string] $ToolRoot = "$env:USERPROFILE\.codex\tools\nexusforever-decomp",
    [string] $LabelMap = (Join-Path $PSScriptRoot 'function_labels.csv'),
    [string[]] $Targets = @('WildStar64.exe', 'Houston64.exe', 'StsConnLib64.MT.dll'),
    [int] $MaxDecompiledFunctions = 200,
    [ValidateSet('Auto', 'Force', 'Skip')]
    [string] $DecompileMode = 'Auto',
    [ValidateSet('Auto', 'Force', 'Skip')]
    [string] $AnalysisMode = 'Auto',
    [ValidateSet('Off', 'Incremental', 'Complete')]
    [string] $CacheWarmMode = 'Incremental',
    [ValidateRange(0, 1000000)]
    [int] $MaxWarmFunctionsPerRun = 100,
    [ValidateSet('Auto', 'Shared', 'PerTarget')]
    [string] $ProjectLayout = 'Auto',
    [ValidateRange(1, 3600)]
    [int] $ProjectLockRetryDelaySeconds = 15,
    [ValidateRange(0, 10080)]
    [int] $ProjectLockTimeoutMinutes = 0,
    [ValidateRange(1, 32)]
    [int] $MaxParallel = 1,
    [string] $RunId = '',
    [string] $SummaryPath = '',
    [switch] $SkipCoverage,
    [switch] $AllClientBinaries,
    [switch] $NoApplyLabels,
    [switch] $ExportOnly,
    [switch] $SkipDefaultExport,
    [string] $ExtraPostScript,
    [string[]] $ExtraPostScriptArgs = @(),
    [string] $GhidraMaxHeap = '2G'
)

$ErrorActionPreference = 'Stop'

$AnalysisManifestVersion = '1'

function Get-ArtifactFingerprint {
    param(
        [string] $Path
    )

    if ([string]::IsNullOrWhiteSpace($Path) -or -not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        return ''
    }

    return ('sha256:{0}' -f ((Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()))
}

function Read-KeyValuePropertiesFile {
    param(
        [string] $Path
    )

    if ([string]::IsNullOrWhiteSpace($Path) -or -not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        return $null
    }

    $properties = @{}
    foreach ($line in Get-Content -LiteralPath $Path) {
        if ([string]::IsNullOrWhiteSpace($line)) {
            continue
        }

        $trimmed = $line.Trim()
        if ($trimmed.StartsWith('#') -or $trimmed.StartsWith('!')) {
            continue
        }

        $separatorIndex = $trimmed.IndexOf('=')
        if ($separatorIndex -lt 0) {
            $separatorIndex = $trimmed.IndexOf(':')
        }

        if ($separatorIndex -lt 0) {
            continue
        }

        $key = $trimmed.Substring(0, $separatorIndex).Trim()
        if ([string]::IsNullOrWhiteSpace($key)) {
            continue
        }

        $value = $trimmed.Substring($separatorIndex + 1).Trim()
        $escapedBackslash = [string] [char] 0xE000
        $value = $value.Replace('\\', $escapedBackslash)
        $value = $value.Replace('\:', ':').Replace('\=', '=').Replace('\ ', ' ')
        $value = $value.Replace('\t', "`t").Replace('\n', "`n").Replace('\r', "`r").Replace('\f', [string] [char] 12)
        $value = $value.Replace($escapedBackslash, [string] [char] 92)
        $properties[$key] = $value
    }

    return $properties
}

function ConvertTo-NullableInt {
    param(
        [string] $Value
    )

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return $null
    }

    $parsed = 0
    if ([int]::TryParse($Value, [ref] $parsed)) {
        return $parsed
    }

    return $null
}

function ConvertTo-NullableBool {
    param(
        [string] $Value
    )

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return $null
    }

    $parsed = $false
    if ([bool]::TryParse($Value, [ref] $parsed)) {
        return $parsed
    }

    return $null
}

function Get-DecompileManifestSummary {
    param(
        [string] $ManifestPath,
        [string] $ExpectedBinaryFingerprint,
        [string] $ExpectedLabelFingerprint
    )

    $properties = Read-KeyValuePropertiesFile -Path $ManifestPath
    if ($null -eq $properties) {
        return $null
    }

    $manifestBinaryFingerprint = $properties['binary.fingerprint']
    $manifestLabelFingerprint = $properties['labels.fingerprint']
    return [ordered]@{
        fingerprint                = $properties['fingerprint']
        contextFingerprint         = $properties['context.fingerprint']
        scriptVersion              = $properties['script.version']
        labelsApplied              = ConvertTo-NullableBool -Value $properties['labels.applied']
        labelFingerprint           = $manifestLabelFingerprint
        labelFingerprintMatchesRun = if ([string]::IsNullOrWhiteSpace($ExpectedLabelFingerprint) -or [string]::IsNullOrWhiteSpace($manifestLabelFingerprint)) { $null } else { $manifestLabelFingerprint -eq $ExpectedLabelFingerprint }
        binaryFingerprint          = $manifestBinaryFingerprint
        binaryFingerprintMatchesRun = if ([string]::IsNullOrWhiteSpace($ExpectedBinaryFingerprint) -or [string]::IsNullOrWhiteSpace($manifestBinaryFingerprint)) { $null } else { $manifestBinaryFingerprint -eq $ExpectedBinaryFingerprint }
        maxDecompiledFunctions     = ConvertTo-NullableInt -Value $properties['max.decompiledFunctions']
        selectedCount              = ConvertTo-NullableInt -Value $properties['selected.count']
        outputExists               = ConvertTo-NullableBool -Value $properties['output.exists']
        reusedFragments            = ConvertTo-NullableInt -Value $properties['cache.reusedFragments']
        decompiledFragments        = ConvertTo-NullableInt -Value $properties['cache.decompiledFragments']
    }
}

function Get-DecompileCacheSummary {
    param(
        [string] $SummaryPath
    )

    $properties = Read-KeyValuePropertiesFile -Path $SummaryPath
    if ($null -eq $properties) {
        return $null
    }

    return [ordered]@{
        summaryPath                 = $SummaryPath
        scriptVersion               = $properties['script.version']
        mode                        = $properties['cache.mode']
        cacheDirectory              = $properties['cache.directory']
        binaryFingerprint           = $properties['binary.fingerprint']
        totalInternalFunctions      = ConvertTo-NullableInt -Value $properties['functions.totalInternal']
        canonicalCachedFragments    = ConvertTo-NullableInt -Value $properties['cache.canonicalCachedFragments']
        warmedThisRun               = ConvertTo-NullableInt -Value $properties['cache.warmedThisRun']
        reusedExistingFragments     = ConvertTo-NullableInt -Value $properties['cache.reusedExistingFragments']
        remainingUncached           = ConvertTo-NullableInt -Value $properties['cache.remainingUncached']
        legacyDuplicateFragments    = ConvertTo-NullableInt -Value $properties['cache.legacyDuplicateFragments']
        migratedLegacyFragments     = ConvertTo-NullableInt -Value $properties['cache.migratedLegacyFragments']
        skippedAlreadyExported      = ConvertTo-NullableInt -Value $properties['cache.skippedAlreadyExported']
    }
}

function Get-AnalysisManifestPath {
    param(
        [string] $ProjectDirectory,
        [string] $ProjectName,
        [string] $Target
    )

    $manifestDir = Join-Path $ProjectDirectory '.analysis_manifests'
    New-Item -ItemType Directory -Force -Path $manifestDir | Out-Null
    $manifestName = '{0}.{1}.analysis.properties' -f (ConvertTo-ProjectToken -Value $ProjectName), (ConvertTo-ProjectToken -Value $Target)
    return Join-Path $manifestDir $manifestName
}

function Test-AnalysisManifestMatches {
    param(
        [string] $ManifestPath,
        [string] $Target,
        [string] $ProjectName,
        [string] $ProjectLayout,
        [string] $BinaryFingerprint,
        [string] $GhidraVersion
    )

    $properties = Read-KeyValuePropertiesFile -Path $ManifestPath
    if ($null -eq $properties) {
        return $false
    }

    return $properties['manifest.version'] -eq $AnalysisManifestVersion -and
        $properties['target'] -eq $Target -and
        $properties['project.name'] -eq $ProjectName -and
        $properties['project.layout'] -eq $ProjectLayout -and
        $properties['binary.fingerprint'] -eq $BinaryFingerprint -and
        $properties['ghidra.version'] -eq $GhidraVersion
}

function Write-AnalysisManifest {
    param(
        [string] $ManifestPath,
        [string] $Target,
        [string] $ProjectName,
        [string] $ProjectLayout,
        [string] $BinaryFingerprint,
        [string] $GhidraVersion
    )

    $lines = @(
        ('manifest.version={0}' -f $AnalysisManifestVersion),
        ('target={0}' -f $Target),
        ('project.name={0}' -f $ProjectName),
        ('project.layout={0}' -f $ProjectLayout),
        ('binary.fingerprint={0}' -f $BinaryFingerprint),
        ('ghidra.version={0}' -f $GhidraVersion),
        ('completedUtc={0:o}' -f [datetime]::UtcNow)
    )
    $lines | Out-File -LiteralPath $ManifestPath -Encoding utf8
}

function Resolve-EffectiveMaxDecompiledFunctions {
    param(
        [string] $ManifestPath,
        [int] $RequestedMaxDecompiledFunctions,
        [bool] $MaxExplicitlySet,
        [bool] $ExportOnly,
        [string] $ExtraPostScript
    )

    if ($MaxExplicitlySet -or -not $ExportOnly -or [string]::IsNullOrWhiteSpace($ExtraPostScript)) {
        return $RequestedMaxDecompiledFunctions
    }

    $manifest = Get-DecompileManifestSummary -ManifestPath $ManifestPath -ExpectedBinaryFingerprint '' -ExpectedLabelFingerprint ''
    if ($null -eq $manifest -or $null -eq $manifest.maxDecompiledFunctions -or $manifest.maxDecompiledFunctions -le 0) {
        return $RequestedMaxDecompiledFunctions
    }

    return $manifest.maxDecompiledFunctions
}

function Finalize-TargetSummary {
    param(
        [System.Collections.IDictionary] $TargetSummary,
        [string] $ExpectedBinaryFingerprint,
        [string] $ExpectedLabelFingerprint
    )

    $TargetSummary.exported = Test-Path -LiteralPath $TargetSummary.exportDir
    $TargetSummary.manifestExists = Test-Path -LiteralPath $TargetSummary.manifestPath
    $TargetSummary.manifest = if ($TargetSummary.manifestExists) {
        Get-DecompileManifestSummary -ManifestPath $TargetSummary.manifestPath -ExpectedBinaryFingerprint $ExpectedBinaryFingerprint -ExpectedLabelFingerprint $ExpectedLabelFingerprint
    }
    else {
        $null
    }
    $TargetSummary.cacheSummaryExists = Test-Path -LiteralPath $TargetSummary.cacheSummaryPath -PathType Leaf
    $TargetSummary.cacheSummary = if ($TargetSummary.cacheSummaryExists) {
        Get-DecompileCacheSummary -SummaryPath $TargetSummary.cacheSummaryPath
    }
    else {
        $null
    }
}

function ConvertTo-ProjectToken {
    param(
        [string] $Value
    )

    $token = $Value -replace '[^A-Za-z0-9]+', '_'
    $token = $token.Trim('_')
    if ([string]::IsNullOrWhiteSpace($token)) {
        return 'Target'
    }

    return $token
}

function Test-GhidraProjectExists {
    param(
        [string] $Directory,
        [string] $ProjectName
    )

    return (Test-Path -LiteralPath (Join-Path $Directory ($ProjectName + '.gpr')) -PathType Leaf) -or
        (Test-Path -LiteralPath (Join-Path $Directory ($ProjectName + '.rep')))
}

function Get-GhidraProjectName {
    param(
        [string] $BaseProjectName,
        [string] $Target,
        [ValidateSet('Shared', 'PerTarget')]
        [string] $Layout
    )

    if ($Layout -eq 'Shared') {
        return $BaseProjectName
    }

    $targetToken = ConvertTo-ProjectToken -Value ([IO.Path]::GetFileNameWithoutExtension($Target))
    return ('{0}_{1}' -f $BaseProjectName, $targetToken)
}

function Test-ProjectLockWaitExpired {
    param(
        [datetime] $StartedUtc,
        [int] $TimeoutMinutes
    )

    if ($TimeoutMinutes -le 0) {
        return $false
    }

    return [datetime]::UtcNow -ge $StartedUtc.AddMinutes($TimeoutMinutes)
}

function Format-WaitElapsed {
    param(
        [datetime] $StartedUtc
    )

    $elapsed = [datetime]::UtcNow - $StartedUtc
    if ($elapsed.TotalHours -ge 1) {
        return ('{0:00}:{1:00}:{2:00}' -f [int]$elapsed.TotalHours, $elapsed.Minutes, $elapsed.Seconds)
    }

    return ('{0:00}:{1:00}' -f [int]$elapsed.TotalMinutes, $elapsed.Seconds)
}

function Format-LockTimeout {
    param(
        [int] $TimeoutMinutes
    )

    if ($TimeoutMinutes -le 0) {
        return 'none'
    }

    return ('{0}m' -f $TimeoutMinutes)
}

function Limit-ProcessCommandLine {
    param(
        [string] $CommandLine,
        [int] $MaxLength = 220
    )

    if ([string]::IsNullOrWhiteSpace($CommandLine)) {
        return ''
    }

    $singleLine = $CommandLine -replace '\s+', ' '
    if ($singleLine.Length -le $MaxLength) {
        return $singleLine
    }

    return $singleLine.Substring(0, $MaxLength - 3) + '...'
}

function Format-CimProcessSummary {
    param(
        [object] $Process
    )

    if ($null -eq $Process) {
        return ''
    }

    $started = ''
    if ($Process.CreationDate) {
        $started = (' started={0}' -f $Process.CreationDate)
    }

    $command = Limit-ProcessCommandLine -CommandLine $Process.CommandLine
    $commandPart = if ($command) { ' command="' + $command + '"' } else { '' }
    return ('pid={0} name={1}{2}{3}' -f $Process.ProcessId, $Process.Name, $started, $commandPart)
}

function Get-ProcessSummary {
    param(
        [string] $ProcessId
    )

    if ([string]::IsNullOrWhiteSpace($ProcessId)) {
        return ''
    }

    [int] $numericProcessId = 0
    if (-not [int]::TryParse($ProcessId, [ref]$numericProcessId)) {
        return ('pid={0}' -f $ProcessId)
    }

    try {
        $process = Get-CimInstance Win32_Process -Filter ('ProcessId = {0}' -f $numericProcessId) -ErrorAction Stop
        if ($process) {
            return Format-CimProcessSummary -Process $process
        }
    }
    catch {
    }

    try {
        $process = Get-Process -Id $numericProcessId -ErrorAction Stop
        return ('pid={0} name={1} started={2}' -f $process.Id, $process.ProcessName, $process.StartTime)
    }
    catch {
        return ('pid={0} (process exited or inaccessible)' -f $numericProcessId)
    }
}

function Read-ProjectGateMetadata {
    param(
        [string] $LockPath
    )

    $metadata = @{}
    if (-not (Test-Path -LiteralPath $LockPath -PathType Leaf)) {
        return $metadata
    }

    $stream = $null
    $reader = $null
    try {
        $stream = [System.IO.File]::Open($LockPath, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
        $reader = [System.IO.StreamReader]::new($stream, [System.Text.Encoding]::UTF8, $true)
        $content = $reader.ReadToEnd()
        foreach ($line in ($content -split "\r?\n")) {
            if ($line -match '^\s*([^=]+)=(.*)$') {
                $metadata[$matches[1].Trim()] = $matches[2].Trim()
            }
        }
    }
    catch {
        $metadata['readError'] = $_.Exception.Message
    }
    finally {
        if ($null -ne $reader) {
            $reader.Dispose()
        }
        elseif ($null -ne $stream) {
            $stream.Dispose()
        }
    }

    return $metadata
}

function Get-ProjectGateOwnerSummary {
    param(
        [string] $LockPath
    )

    $metadata = Read-ProjectGateMetadata -LockPath $LockPath
    if ($metadata.ContainsKey('readError')) {
        return ('metadata unavailable: {0}' -f $metadata['readError'])
    }

    $parts = New-Object 'System.Collections.Generic.List[string]'
    if ($metadata.ContainsKey('owner')) {
        $parts.Add(('owner={0}' -f $metadata['owner']))
    }
    if ($metadata.ContainsKey('host')) {
        $parts.Add(('host={0}' -f $metadata['host']))
    }
    if ($metadata.ContainsKey('pid')) {
        $parts.Add((Get-ProcessSummary -ProcessId $metadata['pid']))
    }
    if ($metadata.ContainsKey('acquiredUtc')) {
        $parts.Add(('acquiredUtc={0}' -f $metadata['acquiredUtc']))
    }

    if ($parts.Count -eq 0) {
        return 'owner unknown'
    }

    return ($parts -join ', ')
}

function Write-GhidraProjectGateWaitStatus {
    param(
        [string] $ProjectName,
        [string] $LockPath,
        [datetime] $StartedUtc,
        [int] $RetryDelaySeconds,
        [int] $TimeoutMinutes
    )

    $ownerSummary = Get-ProjectGateOwnerSummary -LockPath $LockPath
    Write-Host ("Waiting for Ghidra project gate {0}; owner: {1}; elapsed {2}; retrying in {3}s; timeout {4}. Lock file: {5}" -f $ProjectName, $ownerSummary, (Format-WaitElapsed -StartedUtc $StartedUtc), $RetryDelaySeconds, (Format-LockTimeout -TimeoutMinutes $TimeoutMinutes), $LockPath)
}

function Get-GhidraProjectProcessHints {
    param(
        [string] $ProjectName,
        [string] $ProjectDir
    )

    try {
        $ghidraProcesses = @(Get-CimInstance Win32_Process -ErrorAction Stop | Where-Object {
            $commandLine = [string]$_.CommandLine
            -not [string]::IsNullOrWhiteSpace($commandLine) -and
                ($commandLine -match '(?i)ghidra|analyzeHeadless')
        })
    }
    catch {
        return @()
    }

    if ($ghidraProcesses.Count -eq 0) {
        return @()
    }

    $matchingProcesses = @($ghidraProcesses | Where-Object {
        $commandLine = [string]$_.CommandLine
        ((-not [string]::IsNullOrWhiteSpace($ProjectName)) -and $commandLine.Contains($ProjectName)) -or
            ((-not [string]::IsNullOrWhiteSpace($ProjectDir)) -and $commandLine.Contains($ProjectDir))
    })

    $selectedProcesses = if ($matchingProcesses.Count -gt 0) { $matchingProcesses } else { $ghidraProcesses }
    return @($selectedProcesses |
        Sort-Object ProcessId -Unique |
        Select-Object -First 4 |
        ForEach-Object { Format-CimProcessSummary -Process $_ })
}

function Enter-GhidraProjectGate {
    param(
        [string] $Directory,
        [string] $ProjectName,
        [int] $RetryDelaySeconds,
        [int] $TimeoutMinutes
    )

    $lockDir = Join-Path $Directory '.project_locks'
    New-Item -ItemType Directory -Force -Path $lockDir | Out-Null
    $lockPath = Join-Path $lockDir ('{0}.lock' -f (ConvertTo-ProjectToken -Value $ProjectName))
    $startedUtc = [datetime]::UtcNow
    $waited = $false

    while ($true) {
        try {
            $stream = [System.IO.File]::Open($lockPath, [System.IO.FileMode]::OpenOrCreate, [System.IO.FileAccess]::ReadWrite, [System.IO.FileShare]::Read)
            $stream.SetLength(0)

            $content = @(
                'owner=run_ghidra_analysis.ps1'
                ('host={0}' -f $env:COMPUTERNAME)
                ('project={0}' -f $ProjectName)
                ('pid={0}' -f $PID)
                ('acquiredUtc={0:o}' -f [datetime]::UtcNow)
            ) -join [Environment]::NewLine
            $bytes = [System.Text.Encoding]::UTF8.GetBytes($content)
            $stream.Write($bytes, 0, $bytes.Length)
            $stream.Flush($true)

            if ($waited) {
                Write-Host ("Acquired Ghidra project gate for {0} after {1}" -f $ProjectName, (Format-WaitElapsed -StartedUtc $startedUtc))
            }

            return [pscustomobject]@{
                projectName = $ProjectName
                lockPath    = $lockPath
                stream      = $stream
                waited      = $waited
            }
        }
        catch [System.IO.IOException] {
            if (Test-ProjectLockWaitExpired -StartedUtc $startedUtc -TimeoutMinutes $TimeoutMinutes) {
                throw ("Timed out waiting for Ghidra project gate {0}. Owner: {1}. Lock file: {2}" -f $ProjectName, (Get-ProjectGateOwnerSummary -LockPath $lockPath), $lockPath)
            }

            Write-GhidraProjectGateWaitStatus -ProjectName $ProjectName -LockPath $lockPath -StartedUtc $startedUtc -RetryDelaySeconds $RetryDelaySeconds -TimeoutMinutes $TimeoutMinutes
            $waited = $true
            Start-Sleep -Seconds $RetryDelaySeconds
        }
        catch [System.UnauthorizedAccessException] {
            if (Test-ProjectLockWaitExpired -StartedUtc $startedUtc -TimeoutMinutes $TimeoutMinutes) {
                throw ("Timed out waiting for Ghidra project gate {0}. Owner: {1}. Lock file: {2}" -f $ProjectName, (Get-ProjectGateOwnerSummary -LockPath $lockPath), $lockPath)
            }

            Write-GhidraProjectGateWaitStatus -ProjectName $ProjectName -LockPath $lockPath -StartedUtc $startedUtc -RetryDelaySeconds $RetryDelaySeconds -TimeoutMinutes $TimeoutMinutes
            $waited = $true
            Start-Sleep -Seconds $RetryDelaySeconds
        }
    }
}

function Exit-GhidraProjectGate {
    param(
        [object] $Gate
    )

    if ($null -eq $Gate -or $null -eq $Gate.stream) {
        return
    }

    $Gate.stream.Dispose()
}

function Get-AnalyzeHeadlessLauncher {
    param(
        [string] $GhidraDir,
        [string] $MaxHeap
    )

    $defaultLauncher = Join-Path $GhidraDir 'support\analyzeHeadless.bat'
    if ([string]::IsNullOrWhiteSpace($MaxHeap) -or $MaxHeap -eq '2G') {
        return $defaultLauncher
    }

    $launcherDir = Join-Path $GhidraDir 'support'
    $heapToken = ($MaxHeap.ToUpperInvariant() -replace '[^0-9A-Z]', '')
    $customLauncher = Join-Path $launcherDir ('analyzeHeadless_{0}.bat' -f $heapToken)
    if (-not (Test-Path -LiteralPath $customLauncher) -or
        ((Get-Item -LiteralPath $defaultLauncher).LastWriteTimeUtc -gt (Get-Item -LiteralPath $customLauncher).LastWriteTimeUtc)) {
        $content = Get-Content -LiteralPath $defaultLauncher
        $content = $content -replace '^set MAXMEM=.*$', ('set MAXMEM={0}' -f $MaxHeap)
        Set-Content -LiteralPath $customLauncher -Value $content -Encoding ASCII
    }

    return $customLauncher
}

function Invoke-GhidraHeadlessWithProjectRetry {
    param(
        [string] $AnalyzeHeadless,
        [object[]] $Arguments,
        [string] $LogPath,
        [string] $ProjectName,
        [string] $ProjectDir,
        [int] $RetryDelaySeconds,
        [int] $TimeoutMinutes
    )

    if (Test-Path -LiteralPath $LogPath -PathType Leaf) {
        Remove-Item -LiteralPath $LogPath -Force
    }

    $startedUtc = [datetime]::UtcNow
    $attempt = 0

    while ($true) {
        $attempt++
        if ($attempt -gt 1) {
            Add-Content -LiteralPath $LogPath -Encoding utf8 -Value ("`n--- Retry {0} after Ghidra project lock at {1:o} ---" -f $attempt, [datetime]::UtcNow)
        }

        $lockSignals = New-Object 'System.Collections.Generic.List[string]'
        & $AnalyzeHeadless @Arguments 2>&1 | ForEach-Object {
            $line = [string] $_
            if ($line.Contains('Unable to lock project!')) {
                $lockSignals.Add($line)
            }

            $_
        } | Tee-Object -FilePath $LogPath -Append

        $exitCode = $LASTEXITCODE
        $projectLockFailure = $lockSignals.Count -gt 0
        if (-not $projectLockFailure) {
            return [pscustomobject]@{
                exitCode           = $exitCode
                projectLockFailure = $false
                attempts           = $attempt
            }
        }

        if (Test-ProjectLockWaitExpired -StartedUtc $startedUtc -TimeoutMinutes $TimeoutMinutes) {
            return [pscustomobject]@{
                exitCode           = $exitCode
                projectLockFailure = $true
                attempts           = $attempt
            }
        }

        $processHints = @(Get-GhidraProjectProcessHints -ProjectName $ProjectName -ProjectDir $ProjectDir)
        $processHintText = if ($processHints.Count -gt 0) {
            ' Candidate process(es): ' + ($processHints -join ' | ')
        }
        else {
            ' No live Ghidra/analyzeHeadless process hints found.'
        }
        $waitMessage = ("Ghidra project {0} is locked by another process after attempt {1}; elapsed {2}; retrying in {3}s; timeout {4}.{5} See {6}." -f $ProjectName, $attempt, (Format-WaitElapsed -StartedUtc $startedUtc), $RetryDelaySeconds, (Format-LockTimeout -TimeoutMinutes $TimeoutMinutes), $processHintText, $LogPath)
        Write-Warning $waitMessage
        Add-Content -LiteralPath $LogPath -Encoding utf8 -Value ("--- {0} ---" -f $waitMessage)
        Start-Sleep -Seconds $RetryDelaySeconds
    }
}

$setupScript = Join-Path $PSScriptRoot 'setup_decomp_tools.ps1'
$ghidraDir = Join-Path $ToolRoot 'ghidra_12.0.4_PUBLIC'
$javaHome = Join-Path $ToolRoot 'jdk-21.0.11+10'
$analyzeHeadless = Get-AnalyzeHeadlessLauncher -GhidraDir $ghidraDir -MaxHeap $GhidraMaxHeap
$ghidraVersion = [IO.Path]::GetFileName($ghidraDir)

if (-not (Test-Path -LiteralPath $analyzeHeadless) -or -not (Test-Path -LiteralPath (Join-Path $javaHome 'bin\java.exe'))) {
    & $setupScript -ToolRoot $ToolRoot | Format-List
}

$resolvedClientDir = (Resolve-Path -LiteralPath $ClientDir).Path
New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null
New-Item -ItemType Directory -Force -Path $ProjectDir | Out-Null
$resolvedOutputDir = (Resolve-Path -LiteralPath $OutputDir).Path
$resolvedProjectDir = (Resolve-Path -LiteralPath $ProjectDir).Path
$resolvedRepoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')).Path
$logDir = Join-Path $PSScriptRoot 'logs'
New-Item -ItemType Directory -Force -Path $logDir | Out-Null

if ($AllClientBinaries) {
    $Targets = Get-ChildItem -LiteralPath $resolvedClientDir -File |
        Where-Object { $_.Extension -in @('.exe', '.dll') } |
        Select-Object -ExpandProperty Name
}

if ($Targets.Count -eq 0) {
    throw 'No decompile targets were supplied.'
}

if ($SkipDefaultExport -and [string]::IsNullOrWhiteSpace($ExtraPostScript)) {
    throw '-SkipDefaultExport requires -ExtraPostScript so the run still has useful work to do.'
}

if ($ProjectLayout -eq 'Shared' -and $MaxParallel -gt 1 -and $Targets.Count -gt 1) {
    throw 'Shared project layout cannot be used safely with MaxParallel greater than 1. Use -ProjectLayout PerTarget, -ProjectLayout Auto, or -MaxParallel 1.'
}

if ([string]::IsNullOrWhiteSpace($RunId) -and $MaxParallel -gt 1 -and $Targets.Count -gt 1) {
    $RunId = '{0}-{1}' -f (Get-Date -Format 'yyyyMMdd-HHmmss-fff'), $PID
}

$runToken = if ([string]::IsNullOrWhiteSpace($RunId)) { '' } else { ConvertTo-ProjectToken -Value $RunId }
$runLogDir = if ([string]::IsNullOrWhiteSpace($runToken)) {
    $logDir
}
else {
    Join-Path (Join-Path $logDir 'runs') $runToken
}
New-Item -ItemType Directory -Force -Path $runLogDir | Out-Null
$resolvedRunLogDir = (Resolve-Path -LiteralPath $runLogDir).Path

if ([string]::IsNullOrWhiteSpace($SummaryPath)) {
    $summaryFileName = if ([string]::IsNullOrWhiteSpace($runToken)) {
        'LATEST_RUN_SUMMARY.json'
    }
    elseif ($Targets.Count -eq 1) {
        ('{0}.run_summary.json' -f (ConvertTo-ProjectToken -Value ([IO.Path]::GetFileNameWithoutExtension($Targets[0]))))
    }
    else {
        'run_summary.json'
    }

    $SummaryPath = Join-Path $resolvedRunLogDir $summaryFileName
}

$summaryPathIsRooted = [IO.Path]::IsPathRooted($SummaryPath)
$effectiveSummaryPath = if ($summaryPathIsRooted) { $SummaryPath } else { Join-Path (Get-Location).Path $SummaryPath }
$summaryParent = Split-Path -Parent $effectiveSummaryPath
if (-not [string]::IsNullOrWhiteSpace($summaryParent)) {
    New-Item -ItemType Directory -Force -Path $summaryParent | Out-Null
}
$resolvedSummaryPath = if (Test-Path -LiteralPath $effectiveSummaryPath -PathType Leaf) {
    (Resolve-Path -LiteralPath $effectiveSummaryPath).Path
}
else {
    Join-Path (Resolve-Path -LiteralPath $summaryParent).Path (Split-Path -Leaf $effectiveSummaryPath)
}

$env:JAVA_HOME = (Resolve-Path -LiteralPath $javaHome).Path
$env:PATH = "$env:JAVA_HOME\bin;$env:PATH"

$scriptPath = Join-Path $PSScriptRoot 'scripts'
$sharedProjectName = 'NexusForeverClient64'
$resolvedLabelMap = $null
if (-not $NoApplyLabels -and (Test-Path -LiteralPath $LabelMap)) {
    $resolvedLabelMap = (Resolve-Path -LiteralPath $LabelMap).Path
}

$labelsApplied = $null -ne $resolvedLabelMap
$labelFingerprint = if ($labelsApplied) { Get-ArtifactFingerprint -Path $resolvedLabelMap } else { '' }
$maxDecompiledFunctionsExplicitlySet = $PSBoundParameters.ContainsKey('MaxDecompiledFunctions')
$effectiveAnalysisMode = if ($ExportOnly) { 'Skip' } else { $AnalysisMode }
$effectiveProjectLayout = if ($ProjectLayout -eq 'Auto') {
    if ($MaxParallel -gt 1 -and $Targets.Count -gt 1) { 'PerTarget' }
    elseif ($AllClientBinaries -or $Targets.Count -gt 1) { 'Shared' } else { 'PerTarget' }
}
else {
    $ProjectLayout
}

if ($MaxParallel -gt 1 -and $Targets.Count -gt 1) {
    $pendingTargets = [System.Collections.Generic.Queue[string]]::new()
    foreach ($target in $Targets) {
        $pendingTargets.Enqueue($target)
    }

    $activeJobs = New-Object 'System.Collections.Generic.List[System.Management.Automation.Job]'
    $jobInfoById = @{}
    $jobResults = New-Object 'System.Collections.Generic.List[object]'

    function Start-RunnerTargetJob {
        param(
            [string] $Target
        )

        $targetToken = ConvertTo-ProjectToken -Value ([IO.Path]::GetFileNameWithoutExtension($Target))
        $targetSummaryPath = Join-Path $resolvedRunLogDir ('{0}.run_summary.json' -f $targetToken)
        $jobOutputPath = Join-Path $resolvedRunLogDir ('{0}.job_output.log' -f $targetToken)

        $runnerParameters = @{
            ClientDir                    = $resolvedClientDir
            OutputDir                    = $resolvedOutputDir
            ProjectDir                   = $resolvedProjectDir
            ToolRoot                     = $ToolRoot
            LabelMap                     = if ($resolvedLabelMap) { $resolvedLabelMap } else { $LabelMap }
            Targets                      = @($Target)
            MaxDecompiledFunctions       = $MaxDecompiledFunctions
            DecompileMode                = $DecompileMode
            AnalysisMode                 = $AnalysisMode
            CacheWarmMode                = $CacheWarmMode
            MaxWarmFunctionsPerRun       = $MaxWarmFunctionsPerRun
            GhidraMaxHeap                = $GhidraMaxHeap
            ProjectLayout                = $effectiveProjectLayout
            ProjectLockRetryDelaySeconds = $ProjectLockRetryDelaySeconds
            ProjectLockTimeoutMinutes    = $ProjectLockTimeoutMinutes
            MaxParallel                  = 1
            RunId                        = $RunId
            SummaryPath                  = $targetSummaryPath
            SkipCoverage                 = $true
        }

        if ($NoApplyLabels) {
            $runnerParameters.NoApplyLabels = $true
        }

        if ($ExportOnly) {
            $runnerParameters.ExportOnly = $true
        }

        if ($SkipDefaultExport) {
            $runnerParameters.SkipDefaultExport = $true
        }

        if (-not [string]::IsNullOrWhiteSpace($ExtraPostScript)) {
            $runnerParameters.ExtraPostScript = $ExtraPostScript
            $runnerParameters.ExtraPostScriptArgs = $ExtraPostScriptArgs
        }

        $job = Start-Job -Name ('ghidra-{0}' -f $targetToken) -ScriptBlock {
            param(
                [string] $RunnerPath,
                [hashtable] $RunnerParameters
            )

            & $RunnerPath @RunnerParameters
        } -ArgumentList $PSCommandPath, $runnerParameters

        $activeJobs.Add($job)
        $jobInfoById[$job.Id] = [pscustomobject]@{
            target = $Target
            summaryPath = $targetSummaryPath
            jobOutputPath = $jobOutputPath
        }

        Write-Host ("Started {0} as job {1}; summary: {2}" -f $Target, $job.Id, $targetSummaryPath)
    }

    while ($pendingTargets.Count -gt 0 -or $activeJobs.Count -gt 0) {
        while ($pendingTargets.Count -gt 0 -and $activeJobs.Count -lt $MaxParallel) {
            Start-RunnerTargetJob -Target $pendingTargets.Dequeue()
        }

        if ($activeJobs.Count -eq 0) {
            continue
        }

        Wait-Job -Job $activeJobs.ToArray() -Any | Out-Null
        $finishedJobs = @($activeJobs.ToArray() | Where-Object { $_.State -in @('Completed', 'Failed', 'Stopped') })
        foreach ($job in $finishedJobs) {
            $info = $jobInfoById[$job.Id]
            $jobOutput = Receive-Job -Job $job -Keep 2>&1
            $jobOutput | Out-String | Out-File -LiteralPath $info.jobOutputPath -Encoding utf8

            $jobResults.Add([pscustomobject]@{
                target = $info.target
                jobId = $job.Id
                state = [string] $job.State
                summaryPath = $info.summaryPath
                jobOutputPath = $info.jobOutputPath
            })

            Write-Host ("Finished {0} as job {1}: {2}" -f $info.target, $job.Id, $job.State)
            Remove-Job -Job $job -Force
            [void] $activeJobs.Remove($job)
        }
    }

    $workerSummaries = New-Object 'System.Collections.Generic.List[object]'
    foreach ($result in $jobResults) {
        if (Test-Path -LiteralPath $result.summaryPath -PathType Leaf) {
            $workerSummaries.Add((Get-Content -LiteralPath $result.summaryPath -Raw | ConvertFrom-Json))
        }
    }

    $mergedTargets = @($workerSummaries | ForEach-Object { $_.targets } | ForEach-Object { $_ })
    $failedJobs = @($jobResults | Where-Object { $_.state -ne 'Completed' })
    $failedTargets = @($mergedTargets | Where-Object { $_.status -ne 'success' })
    $hasFailures = $failedJobs.Count -gt 0 -or $failedTargets.Count -gt 0 -or $workerSummaries.Count -ne $Targets.Count

    $parallelSummary = [ordered]@{
        timestampUtc = (Get-Date).ToUniversalTime().ToString('o')
        runId = $RunId
        parallel = $true
        clientDir = $resolvedClientDir
        outputDir = $resolvedOutputDir
        projectDir = $resolvedProjectDir
        logDir = $resolvedRunLogDir
        summaryPath = $resolvedSummaryPath
        decompileMode = $DecompileMode
        analysisMode = $AnalysisMode
        effectiveAnalysisMode = $effectiveAnalysisMode
        cacheWarmMode = $CacheWarmMode
        maxWarmFunctionsPerRun = $MaxWarmFunctionsPerRun
        projectLayout = $effectiveProjectLayout
        maxParallel = $MaxParallel
        projectLockRetryDelaySeconds = $ProjectLockRetryDelaySeconds
        projectLockTimeoutMinutes = $ProjectLockTimeoutMinutes
        exportOnly = [bool]$ExportOnly
        skipDefaultExport = [bool]$SkipDefaultExport
        skipCoverage = [bool]$SkipCoverage
        noApplyLabels = [bool]$NoApplyLabels
        labelsApplied = [bool]$labelsApplied
        labelMap = if ($resolvedLabelMap) { $resolvedLabelMap } else { '' }
        labelFingerprint = $labelFingerprint
        requestedMaxDecompiledFunctions = $MaxDecompiledFunctions
        maxDecompiledFunctions = $MaxDecompiledFunctions
        extraPostScript = if ($ExtraPostScript) { $ExtraPostScript } else { '' }
        extraPostScriptArgs = $ExtraPostScriptArgs
        targetCount = $Targets.Count
        targets = $mergedTargets
        workerSummaries = @($jobResults | Select-Object target, summaryPath, jobOutputPath, state)
    }

    $parallelSummary | ConvertTo-Json -Depth 8 | Out-File -LiteralPath $resolvedSummaryPath -Encoding utf8
    Write-Host "Parallel run summary written to: $resolvedSummaryPath"

    $latestSummaryPath = Join-Path $logDir 'LATEST_RUN_SUMMARY.json'
    if ($resolvedSummaryPath -ne $latestSummaryPath) {
        $parallelSummary | ConvertTo-Json -Depth 8 | Out-File -LiteralPath $latestSummaryPath -Encoding utf8
        Write-Host "Latest run summary written to: $latestSummaryPath"
    }

    $coverageScript = Join-Path $PSScriptRoot 'Get-DecompCoverageSnapshot.ps1'
    if (-not $hasFailures -and -not $SkipCoverage -and (Test-Path -LiteralPath $coverageScript -PathType Leaf)) {
        $coverageSummary = & $coverageScript -RepoRoot $resolvedRepoRoot -OutputDir $resolvedOutputDir -LogDir $resolvedRunLogDir -RunSummaryPath $resolvedSummaryPath
        if ($null -ne $coverageSummary) {
            $parallelSummary.coverage = [ordered]@{
                summaryPath = $coverageSummary.summaryPath
                markdownPath = $coverageSummary.markdownPath
                exportInventoryPath = $coverageSummary.exportInventoryPath
                opcodeInventoryPath = $coverageSummary.opcodeInventoryPath
                exportTargetCount = $coverageSummary.exportTargetCount
                totalOpcodes = $coverageSummary.totalOpcodes
            }
            $parallelSummary | ConvertTo-Json -Depth 8 | Out-File -LiteralPath $resolvedSummaryPath -Encoding utf8
            $parallelSummary | ConvertTo-Json -Depth 8 | Out-File -LiteralPath $latestSummaryPath -Encoding utf8
            Write-Host ("Coverage summary written to: {0}" -f $coverageSummary.summaryPath)
        }
    }
    elseif ($hasFailures -and -not $SkipCoverage) {
        Write-Warning 'Skipping coverage snapshot because one or more decompile jobs failed.'
    }

    [pscustomobject]@{
        runId = $RunId
        summaryPath = $resolvedSummaryPath
        latestSummaryPath = $latestSummaryPath
        targetCount = $Targets.Count
        completedTargets = @($mergedTargets | Where-Object { $_.status -eq 'success' }).Count
        failedJobs = $failedJobs.Count
        failedTargets = $failedTargets.Count
    }

    if ($hasFailures) {
        throw 'One or more decompile jobs failed. Inspect the parallel summary and per-target job output logs.'
    }

    return
}

$runSummaryTargets = New-Object 'System.Collections.Generic.List[object]'

try {
    foreach ($target in $Targets) {
        $targetName = [IO.Path]::GetFileNameWithoutExtension($target)
        $binaryPath = Join-Path $resolvedClientDir $target
        $binaryFingerprint = Get-ArtifactFingerprint -Path $binaryPath
        $projectName = Get-GhidraProjectName -BaseProjectName $sharedProjectName -Target $target -Layout $effectiveProjectLayout
        $projectExists = Test-GhidraProjectExists -Directory $resolvedProjectDir -ProjectName $projectName
        $analysisManifestPath = Get-AnalysisManifestPath -ProjectDirectory $resolvedProjectDir -ProjectName $projectName -Target $target
        $analysisManifestMatches = Test-AnalysisManifestMatches `
            -ManifestPath $analysisManifestPath `
            -Target $target `
            -ProjectName $projectName `
            -ProjectLayout $effectiveProjectLayout `
            -BinaryFingerprint $binaryFingerprint `
            -GhidraVersion $ghidraVersion
        $analysisAction = if ($effectiveAnalysisMode -eq 'Skip') {
            'Process'
        }
        elseif ($effectiveAnalysisMode -eq 'Force') {
            'Import'
        }
        elseif ($projectExists -and $analysisManifestMatches) {
            'Process'
        }
        else {
            'Import'
        }

        if ($effectiveProjectLayout -eq 'PerTarget' -and $analysisAction -eq 'Process' -and -not $projectExists) {
            if ($ProjectLayout -eq 'Auto' -and (Test-GhidraProjectExists -Directory $ProjectDir -ProjectName $sharedProjectName)) {
                Write-Warning ("Per-target project {0} not found for {1}. Falling back to legacy shared project {2} for export-only. Run once without -ExportOnly to create the split project." -f $projectName, $target, $sharedProjectName)
                $projectName = $sharedProjectName
                $projectExists = $true
                $analysisManifestPath = Get-AnalysisManifestPath -ProjectDirectory $resolvedProjectDir -ProjectName $projectName -Target $target
                $analysisManifestMatches = Test-AnalysisManifestMatches `
                    -ManifestPath $analysisManifestPath `
                    -Target $target `
                    -ProjectName $projectName `
                    -ProjectLayout 'Shared' `
                    -BinaryFingerprint $binaryFingerprint `
                    -GhidraVersion $ghidraVersion
            }
            else {
                throw ("Export-only requested for {0}, but per-target Ghidra project {1} does not exist under {2}. Run once without -ExportOnly to create it, or use -ProjectLayout Shared." -f $target, $projectName, $ProjectDir)
            }
        }

        $projectLogSuffix = if ($projectName -ne $sharedProjectName) { ".{0}" -f $projectName } else { '' }
        $logSuffix = if ($ExtraPostScript) { ".{0}" -f ([IO.Path]::GetFileNameWithoutExtension($ExtraPostScript)) } else { '' }
        $logPath = Join-Path $resolvedRunLogDir ("{0}{1}{2}.ghidra.log" -f $targetName, $projectLogSuffix, $logSuffix)
        $exportDir = Join-Path $resolvedOutputDir $target
        $manifestPath = Join-Path $exportDir 'selected_decompiled.manifest'
        $cacheSummaryPath = Join-Path $exportDir 'decompile_cache_summary.properties'
        $effectiveMaxDecompiledFunctions = Resolve-EffectiveMaxDecompiledFunctions `
            -ManifestPath $manifestPath `
            -RequestedMaxDecompiledFunctions $MaxDecompiledFunctions `
            -MaxExplicitlySet $maxDecompiledFunctionsExplicitlySet `
            -ExportOnly ($effectiveAnalysisMode -eq 'Skip') `
            -ExtraPostScript $ExtraPostScript
        $targetSummary = [ordered]@{
            target = $target
            binaryPath = $binaryPath
            binaryFingerprint = $binaryFingerprint
            projectName = $projectName
            exportDir = $exportDir
            manifestPath = $manifestPath
            logPath = $logPath
            requestedMaxDecompiledFunctions = $MaxDecompiledFunctions
            effectiveMaxDecompiledFunctions = $effectiveMaxDecompiledFunctions
            analysisMode = $AnalysisMode
            effectiveAnalysisMode = $effectiveAnalysisMode
            analysisAction = $analysisAction
            analysisManifestPath = $analysisManifestPath
            analysisManifestMatches = [bool]$analysisManifestMatches
            analysisManifestUpdated = $false
            cacheWarmMode = $CacheWarmMode
            maxWarmFunctionsPerRun = $MaxWarmFunctionsPerRun
            cacheSummaryPath = $cacheSummaryPath
            cacheSummaryExists = $false
            cacheSummary = $null
            status = 'pending'
            exitCode = $null
            projectLockFailure = $false
            projectLockAttempts = 0
            projectGateLockPath = ''
            projectGateWaited = $false
            skipDefaultExport = [bool]$SkipDefaultExport
            scriptFailure = $false
            exported = $false
            manifestExists = $false
            manifest = $null
        }

        $ghidraArgs = @(
            $ProjectDir,
            $projectName
        )

        Write-Host ("Using Ghidra project {0}" -f $projectName)
        if (-not $SkipDefaultExport -and $effectiveMaxDecompiledFunctions -ne $MaxDecompiledFunctions) {
            Write-Host ("Preserving manifest max decompile depth {0} for {1} because -ExtraPostScript was used without an explicit -MaxDecompiledFunctions override." -f $effectiveMaxDecompiledFunctions, $target)
        }

        if ($analysisAction -eq 'Process') {
            if ($SkipDefaultExport) {
                Write-Host "Opening existing Ghidra program $target"
            }
            else {
                Write-Host "Exporting existing Ghidra program $target"
            }
            $ghidraArgs += @(
                '-process', $target,
                '-noanalysis'
            )
        }
        else {
            if (-not (Test-Path -LiteralPath $binaryPath)) {
                throw "Target binary not found: $binaryPath"
            }

            Write-Host "Analyzing $binaryPath"
            $ghidraArgs += @(
                '-import', $binaryPath,
                '-overwrite',
                '-analysisTimeoutPerFile', '3600'
            )
        }

        $ghidraArgs += @('-scriptPath', $scriptPath)
        if ($resolvedLabelMap) {
            $ghidraArgs += @('-preScript', 'ApplyNexusForeverLabels.java', $resolvedLabelMap)
        }

        if (-not $SkipDefaultExport) {
            $ghidraArgs += @(
                '-postScript', 'ExportNexusForeverAnalysis.java', $OutputDir, $effectiveMaxDecompiledFunctions,
                $DecompileMode, $ghidraVersion, $binaryFingerprint, $labelFingerprint,
                $labelsApplied.ToString().ToLowerInvariant(), $CacheWarmMode, $MaxWarmFunctionsPerRun
            )
        }

        if ($ExtraPostScript) {
            $ghidraArgs += @('-postScript', $ExtraPostScript)
            $ghidraArgs += $ExtraPostScriptArgs
        }

        $projectGate = $null
        try {
            $projectGate = Enter-GhidraProjectGate `
                -Directory $resolvedProjectDir `
                -ProjectName $projectName `
                -RetryDelaySeconds $ProjectLockRetryDelaySeconds `
                -TimeoutMinutes $ProjectLockTimeoutMinutes
            $targetSummary.projectGateLockPath = $projectGate.lockPath
            $targetSummary.projectGateWaited = [bool] $projectGate.waited

            $ghidraResult = Invoke-GhidraHeadlessWithProjectRetry `
                -AnalyzeHeadless $analyzeHeadless `
                -Arguments $ghidraArgs `
                -LogPath $logPath `
                -ProjectName $projectName `
                -ProjectDir $resolvedProjectDir `
                -RetryDelaySeconds $ProjectLockRetryDelaySeconds `
                -TimeoutMinutes $ProjectLockTimeoutMinutes
        }
        finally {
            Exit-GhidraProjectGate -Gate $projectGate
        }

        $targetSummary.exitCode = $ghidraResult.exitCode
        $targetSummary.projectLockFailure = [bool]$ghidraResult.projectLockFailure
        $targetSummary.projectLockAttempts = $ghidraResult.attempts

        if ($ghidraResult.exitCode -eq 0 -and $analysisAction -eq 'Import') {
            Write-AnalysisManifest `
                -ManifestPath $analysisManifestPath `
                -Target $target `
                -ProjectName $projectName `
                -ProjectLayout $effectiveProjectLayout `
                -BinaryFingerprint $binaryFingerprint `
                -GhidraVersion $ghidraVersion
            $targetSummary.analysisManifestUpdated = $true
        }

        if ($ghidraResult.exitCode -ne 0) {
            $targetSummary.status = 'failed'
            Finalize-TargetSummary -TargetSummary $targetSummary -ExpectedBinaryFingerprint $binaryFingerprint -ExpectedLabelFingerprint $labelFingerprint
            $runSummaryTargets.Add([pscustomobject]$targetSummary)

            if ($targetSummary.projectLockFailure) {
                throw ("Ghidra project {0} stayed locked after {1} attempt(s). Updated decompile runners wait for script-owned project gates automatically; close other Ghidra sessions or increase -ProjectLockTimeoutMinutes. See {2}." -f $projectName, $targetSummary.projectLockAttempts, $logPath)
            }
            throw "Ghidra run failed for $target. See $logPath."
        }

        $scriptFailure = Select-String -LiteralPath $logPath -SimpleMatch -Pattern @(
            'SCRIPT ERROR',
            'The class could not be found',
            'Exception running script'
        ) -Quiet
        $targetSummary.scriptFailure = [bool]$scriptFailure
        if ($scriptFailure) {
            $targetSummary.status = 'failed'
            Finalize-TargetSummary -TargetSummary $targetSummary -ExpectedBinaryFingerprint $binaryFingerprint -ExpectedLabelFingerprint $labelFingerprint
            $runSummaryTargets.Add([pscustomobject]$targetSummary)
            throw "Ghidra post-script failed for $target. See $logPath."
        }

        $targetSummary.status = 'success'
        Finalize-TargetSummary -TargetSummary $targetSummary -ExpectedBinaryFingerprint $binaryFingerprint -ExpectedLabelFingerprint $labelFingerprint
        $runSummaryTargets.Add([pscustomobject]$targetSummary)
    }
}
finally {
    $runSummary = [ordered]@{
        timestampUtc = (Get-Date).ToUniversalTime().ToString('o')
        runId = $RunId
        clientDir = $resolvedClientDir
        outputDir = $resolvedOutputDir
        projectDir = $resolvedProjectDir
        logDir = $resolvedRunLogDir
        summaryPath = $resolvedSummaryPath
        decompileMode = $DecompileMode
        analysisMode = $AnalysisMode
        effectiveAnalysisMode = $effectiveAnalysisMode
        cacheWarmMode = $CacheWarmMode
        maxWarmFunctionsPerRun = $MaxWarmFunctionsPerRun
        projectLayout = $effectiveProjectLayout
        maxParallel = $MaxParallel
        projectLockRetryDelaySeconds = $ProjectLockRetryDelaySeconds
        projectLockTimeoutMinutes = $ProjectLockTimeoutMinutes
        exportOnly = [bool]$ExportOnly
        skipDefaultExport = [bool]$SkipDefaultExport
        skipCoverage = [bool]$SkipCoverage
        noApplyLabels = [bool]$NoApplyLabels
        labelsApplied = [bool]$labelsApplied
        labelMap = if ($resolvedLabelMap) { $resolvedLabelMap } else { '' }
        labelFingerprint = $labelFingerprint
        requestedMaxDecompiledFunctions = $MaxDecompiledFunctions
        maxDecompiledFunctions = $MaxDecompiledFunctions
        extraPostScript = if ($ExtraPostScript) { $ExtraPostScript } else { '' }
        extraPostScriptArgs = $ExtraPostScriptArgs
        targetCount = $Targets.Count
        targets = $runSummaryTargets
    }

    $runSummary | ConvertTo-Json -Depth 6 | Out-File -LiteralPath $resolvedSummaryPath -Encoding utf8
    Write-Host "Run summary written to: $resolvedSummaryPath"

    $coverageScript = Join-Path $PSScriptRoot 'Get-DecompCoverageSnapshot.ps1'
    if ($SkipCoverage) {
        Write-Host "Coverage snapshot skipped."
    }
    elseif (Test-Path -LiteralPath $coverageScript -PathType Leaf) {
        try {
            $coverageSummary = & $coverageScript -RepoRoot $resolvedRepoRoot -OutputDir $resolvedOutputDir -LogDir $resolvedRunLogDir -RunSummaryPath $resolvedSummaryPath
            if ($null -ne $coverageSummary) {
                $runSummary.coverage = [ordered]@{
                    summaryPath = $coverageSummary.summaryPath
                    markdownPath = $coverageSummary.markdownPath
                    exportInventoryPath = $coverageSummary.exportInventoryPath
                    opcodeInventoryPath = $coverageSummary.opcodeInventoryPath
                    exportTargetCount = $coverageSummary.exportTargetCount
                    totalOpcodes = $coverageSummary.totalOpcodes
                }

                $runSummary | ConvertTo-Json -Depth 8 | Out-File -LiteralPath $resolvedSummaryPath -Encoding utf8
                Write-Host ("Coverage summary written to: {0}" -f $coverageSummary.summaryPath)
            }
        }
        catch {
            Write-Warning ("Coverage snapshot generation failed: {0}" -f $_.Exception.Message)
        }
    }
}

if ($SkipDefaultExport) {
    Write-Host 'Ghidra run complete. Default export skipped.'
}
else {
    Write-Host "Ghidra run complete. Exports: $OutputDir"
}
