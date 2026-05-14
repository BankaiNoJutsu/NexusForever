[CmdletBinding()]
param(
    [string] $ToolRoot = "$env:USERPROFILE\.codex\tools\nexusforever-decomp",
    [switch] $ForceDownload
)

$ErrorActionPreference = 'Stop'

$downloads = Join-Path $ToolRoot 'downloads'
New-Item -ItemType Directory -Force -Path $downloads | Out-Null

function Save-Download {
    param(
        [Parameter(Mandatory)] [string] $Url,
        [Parameter(Mandatory)] [string] $Path,
        [Parameter(Mandatory)] [string] $Sha256
    )

    if ($ForceDownload -and (Test-Path -LiteralPath $Path)) {
        Remove-Item -LiteralPath $Path -Force
    }

    if (-not (Test-Path -LiteralPath $Path)) {
        Write-Host "Downloading $Url"
        curl.exe -L --fail --output $Path $Url
    }

    $actual = (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($actual -ne $Sha256.ToLowerInvariant()) {
        throw "SHA256 mismatch for $Path. Expected $Sha256, got $actual."
    }
}

function Expand-ArchiveIfMissing {
    param(
        [Parameter(Mandatory)] [string] $ArchivePath,
        [Parameter(Mandatory)] [string] $MarkerPath,
        [Parameter(Mandatory)] [string] $Destination
    )

    if (-not (Test-Path -LiteralPath $MarkerPath)) {
        Write-Host "Extracting $ArchivePath"
        tar.exe -xf $ArchivePath -C $Destination
    }
}

$jdkZip = Join-Path $downloads 'OpenJDK21U-jdk_x64_windows_hotspot_21.0.11_10.zip'
$jdkUrl = 'https://github.com/adoptium/temurin21-binaries/releases/download/jdk-21.0.11%2B10/OpenJDK21U-jdk_x64_windows_hotspot_21.0.11_10.zip'
$jdkSha = 'd3625e7cadf23787ea540229544b6e2ab494b3b54da1801879e583e1dfee0a64'
$jdkDir = Join-Path $ToolRoot 'jdk-21.0.11+10'

$ghidraZip = Join-Path $downloads 'ghidra_12.0.4_PUBLIC_20260303.zip'
$ghidraUrl = 'https://github.com/NationalSecurityAgency/ghidra/releases/download/Ghidra_12.0.4_build/ghidra_12.0.4_PUBLIC_20260303.zip'
$ghidraSha = 'c3b458661d69e26e203d739c0c82d143cc8a4a29d9e571f099c2cf4bda62a120'
$ghidraDir = Join-Path $ToolRoot 'ghidra_12.0.4_PUBLIC'

Save-Download -Url $jdkUrl -Path $jdkZip -Sha256 $jdkSha
Expand-ArchiveIfMissing -ArchivePath $jdkZip -MarkerPath (Join-Path $jdkDir 'bin\java.exe') -Destination $ToolRoot

Save-Download -Url $ghidraUrl -Path $ghidraZip -Sha256 $ghidraSha
Expand-ArchiveIfMissing -ArchivePath $ghidraZip -MarkerPath (Join-Path $ghidraDir 'support\analyzeHeadless.bat') -Destination $ToolRoot

[PSCustomObject]@{
    ToolRoot        = (Resolve-Path -LiteralPath $ToolRoot).Path
    JavaHome        = (Resolve-Path -LiteralPath $jdkDir).Path
    Ghidra          = (Resolve-Path -LiteralPath $ghidraDir).Path
    AnalyzeHeadless = (Resolve-Path -LiteralPath (Join-Path $ghidraDir 'support\analyzeHeadless.bat')).Path
}
