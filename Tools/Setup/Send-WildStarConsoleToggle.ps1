#requires -Version 5.1
<#+
.SYNOPSIS
Sends the WildStar built-in console toggle key directly to the game window.

.DESCRIPTION
WildStar toggles its built-in console on WM_SYSKEYDOWN for virtual key 0xC0
when the client is launched with -Console or /Console. Keyboard layouts can
make the physical key ambiguous, so this helper posts WM_SYSKEYDOWN and
WM_SYSKEYUP for that virtual key directly to the running WildStar window, or
injects Alt plus virtual key 0xC0 through SendInput.

.EXAMPLE
.\Tools\Setup\Send-WildStarConsoleToggle.ps1

.EXAMPLE
.\Tools\Setup\Send-WildStarConsoleToggle.ps1 -FocusWindow
#>

[CmdletBinding()]
param(
    [string[]] $ProcessNames = @('WildStar64', 'WildStar32'),
    [switch] $FocusWindow,
    [switch] $SkipElevationPrompt,
    [string] $ResultPath = '',
    [string] $ProcessNamesPayload = '',
    [ValidateSet('PostMessage', 'SendInput')]
    [string] $InputMethod = 'PostMessage'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (-not [string]::IsNullOrWhiteSpace($ProcessNamesPayload)) {
    $ProcessNames = @((ConvertFrom-Json -InputObject $ProcessNamesPayload))
}

function Test-CurrentProcessElevated {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($identity)
    $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Get-PowerShellExecutablePath {
    if ($PSVersionTable.PSEdition -eq 'Core') {
        return Join-Path $PSHOME 'pwsh.exe'
    }

    return Join-Path $PSHOME 'powershell.exe'
}

function Get-ProcessCommandLine {
    param([int] $ProcessId)

    try {
        $process = Get-CimInstance -ClassName Win32_Process -Filter "ProcessId = $ProcessId"
        return $process.CommandLine
    }
    catch {
        return $null
    }
}

function ConvertTo-PowerShellSingleQuotedLiteral {
    param([string] $Value)

    "'{0}'" -f ($Value -replace "'", "''")
}

function Write-ResultPayload {
    param(
        [string] $Status,
        [object] $Result,
        [string] $Message = ''
    )

    if ([string]::IsNullOrWhiteSpace($ResultPath)) {
        return
    }

    $payload = [ordered]@{
        Status  = $Status
        Result  = $Result
        Message = $Message
    }

    $payload |
        ConvertTo-Json -Depth 4 |
        Set-Content -LiteralPath $ResultPath -Encoding utf8
}

function Invoke-ElevatedHelper {
    $resolvedResultPath = Join-Path ([System.IO.Path]::GetTempPath()) ("wildstar-console-toggle-{0}.json" -f [Guid]::NewGuid().ToString('N'))
    $wrapperPath = Join-Path ([System.IO.Path]::GetTempPath()) ("wildstar-console-toggle-wrapper-{0}.ps1" -f [Guid]::NewGuid().ToString('N'))
    $powerShellPath = Get-PowerShellExecutablePath
    $scriptPath = (Resolve-Path -LiteralPath $PSCommandPath).ProviderPath
    $processNamesPayloadJson = ConvertTo-Json -InputObject @($ProcessNames) -Compress
    $scriptPathLiteral = ConvertTo-PowerShellSingleQuotedLiteral $scriptPath
    $resultPathLiteral = ConvertTo-PowerShellSingleQuotedLiteral $resolvedResultPath
    $processNamesPayloadLiteral = ConvertTo-PowerShellSingleQuotedLiteral $processNamesPayloadJson
    $inputMethodLiteral = ConvertTo-PowerShellSingleQuotedLiteral $InputMethod
    $focusWindowArgument = if ($FocusWindow) { ' -FocusWindow' } else { '' }
    $wrapperContent = @"
`$ErrorActionPreference = 'Stop'

try {
    & $scriptPathLiteral -SkipElevationPrompt -ResultPath $resultPathLiteral -ProcessNamesPayload $processNamesPayloadLiteral -InputMethod $inputMethodLiteral$focusWindowArgument

    if (!(Test-Path -LiteralPath $resultPathLiteral)) {
        `$payload = [ordered]@{
            Status  = 'Error'
            Result  = `$null
            Message = 'Elevated helper completed without writing a result payload.'
        }

        `$payload |
            ConvertTo-Json -Depth 4 |
            Set-Content -LiteralPath $resultPathLiteral -Encoding utf8
    }
}
catch {
    `$payload = [ordered]@{
        Status  = 'Error'
        Result  = `$null
        Message = `$_.Exception.Message
    }

    `$payload |
        ConvertTo-Json -Depth 4 |
        Set-Content -LiteralPath $resultPathLiteral -Encoding utf8

    throw
}
"@

    $wrapperContent | Set-Content -LiteralPath $wrapperPath -Encoding utf8
    $argumentList = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $wrapperPath)

    try {
        Start-Process -FilePath $powerShellPath -ArgumentList $argumentList -Verb RunAs -Wait | Out-Null
    }
    catch {
        throw 'Failed to relaunch the WildStar console helper with elevation. Approve the UAC prompt or rerun the helper from an elevated PowerShell session.'
    }
    finally {
        Remove-Item -LiteralPath $wrapperPath -Force -ErrorAction SilentlyContinue
    }

    if (!(Test-Path -LiteralPath $resolvedResultPath)) {
        throw 'The elevated WildStar console helper did not return a result payload.'
    }

    try {
        $payload = Get-Content -LiteralPath $resolvedResultPath -Raw | ConvertFrom-Json
    }
    finally {
        Remove-Item -LiteralPath $resolvedResultPath -Force -ErrorAction SilentlyContinue
    }

    if ($payload.Status -ne 'Success') {
        throw $payload.Message
    }

    return $payload.Result
}

function Test-NativeInteropTypeCurrent {
    $null -ne ('WildStarConsoleToggleNativeV20260516' -as [type])
}

if (-not (Test-NativeInteropTypeCurrent)) {
Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;

public static class WildStarConsoleToggleNativeV20260516
{
    [StructLayout(LayoutKind.Sequential)]
    public struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public UIntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public UIntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HARDWAREINPUT
    {
        public uint uMsg;
        public ushort wParamL;
        public ushort wParamH;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct INPUTUNION
    {
        [FieldOffset(0)]
        public MOUSEINPUT mi;

        [FieldOffset(0)]
        public KEYBDINPUT ki;

        [FieldOffset(0)]
        public HARDWAREINPUT hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct INPUT
    {
        public uint type;
        public INPUTUNION U;
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool PostMessage(IntPtr hWnd, uint msg, UIntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll")]
    public static extern uint MapVirtualKey(uint uCode, uint uMapType);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("kernel32.dll")]
    public static extern uint GetCurrentThreadId();

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool BringWindowToTop(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    public static extern IntPtr SetActiveWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern IntPtr SetFocus(IntPtr hWnd);
}
'@
}

try {
    $currentProcessElevated = Test-CurrentProcessElevated

    if ((-not $currentProcessElevated) -and (-not $SkipElevationPrompt)) {
        $elevatedResult = Invoke-ElevatedHelper
        Write-Output $elevatedResult
        return
    }

    $wildStarProcess = Get-Process -ErrorAction SilentlyContinue |
        Where-Object {
            $ProcessNames -contains $_.ProcessName -and $_.MainWindowHandle -ne 0
        } |
        Sort-Object StartTime -Descending |
        Select-Object -First 1

    if ($null -eq $wildStarProcess) {
        $processList = $ProcessNames -join ', '
        throw "No running WildStar window was found for process names: $processList. Launch the client first."
    }

    $windowHandle = [IntPtr]$wildStarProcess.MainWindowHandle
    $commandLine = Get-ProcessCommandLine -ProcessId $wildStarProcess.Id
    $consoleArgumentDetected = $false
    if (-not [string]::IsNullOrWhiteSpace($commandLine)) {
        $consoleArgumentDetected = $commandLine -match '(^|\s)[-/]Console(?=\s|$)'
    }

    $virtualKey = [uint32]0xC0
    $scanCode = [WildStarConsoleToggleNativeV20260516]::MapVirtualKey($virtualKey, 0)

    if ($scanCode -eq 0) {
        throw 'Failed to resolve a scan code for virtual key 0xC0.'
    }

    $keyDownMessage = [uint32]0x0104
    $keyUpMessage = [uint32]0x0105
    $systemKeyContextFlag = [uint32]0x20000000
    $keyUpFlags = [uint32]0xC0000000
    $keyDownLParamValue = [uint32](1 -bor ($scanCode -shl 16) -bor $systemKeyContextFlag)
    $keyUpLParamValue = [uint32]([uint64]$keyDownLParamValue -bor [uint64]$keyUpFlags)

    if ($FocusWindow) {
        $currentThreadId = [WildStarConsoleToggleNativeV20260516]::GetCurrentThreadId()
        $unusedProcessId = 0
        $targetThreadId = [WildStarConsoleToggleNativeV20260516]::GetWindowThreadProcessId($windowHandle, [ref] $unusedProcessId)
        $foregroundWindowHandle = [WildStarConsoleToggleNativeV20260516]::GetForegroundWindow()
        $foregroundProcessId = 0
        $foregroundThreadId = if ($foregroundWindowHandle -ne [IntPtr]::Zero) {
            [WildStarConsoleToggleNativeV20260516]::GetWindowThreadProcessId($foregroundWindowHandle, [ref] $foregroundProcessId)
        }
        else {
            0
        }

        $attachedToForeground = $false
        $attachedToTarget = $false

        try {
            [void][WildStarConsoleToggleNativeV20260516]::ShowWindow($windowHandle, 9)

            if (($foregroundThreadId -ne 0) -and ($foregroundThreadId -ne $currentThreadId)) {
                $attachedToForeground = [WildStarConsoleToggleNativeV20260516]::AttachThreadInput($currentThreadId, $foregroundThreadId, $true)
            }

            if (($targetThreadId -ne 0) -and ($targetThreadId -ne $currentThreadId)) {
                $attachedToTarget = [WildStarConsoleToggleNativeV20260516]::AttachThreadInput($currentThreadId, $targetThreadId, $true)
            }

            [void][WildStarConsoleToggleNativeV20260516]::BringWindowToTop($windowHandle)
            [void][WildStarConsoleToggleNativeV20260516]::SetForegroundWindow($windowHandle)
            [void][WildStarConsoleToggleNativeV20260516]::SetActiveWindow($windowHandle)
            [void][WildStarConsoleToggleNativeV20260516]::SetFocus($windowHandle)
        }
        finally {
            if ($attachedToTarget) {
                [void][WildStarConsoleToggleNativeV20260516]::AttachThreadInput($currentThreadId, $targetThreadId, $false)
            }

            if ($attachedToForeground) {
                [void][WildStarConsoleToggleNativeV20260516]::AttachThreadInput($currentThreadId, $foregroundThreadId, $false)
            }
        }
    }

    $foregroundWindowHandle = [WildStarConsoleToggleNativeV20260516]::GetForegroundWindow()
    $foregroundMatched = $foregroundWindowHandle -eq $windowHandle
    $suggestedNextStep = ''

    if ($InputMethod -eq 'PostMessage') {
        $keyDownPosted = [WildStarConsoleToggleNativeV20260516]::PostMessage(
            $windowHandle,
            $keyDownMessage,
            [UIntPtr]::new($virtualKey),
            [IntPtr]::new([int64]$keyDownLParamValue)
        )

        if (-not $keyDownPosted) {
            $lastError = [System.Runtime.InteropServices.Marshal]::GetLastWin32Error()
            throw "Failed to post WM_SYSKEYDOWN for virtual key 0xC0 to WildStar. Win32Error=$lastError"
        }

        $keyUpPosted = [WildStarConsoleToggleNativeV20260516]::PostMessage(
            $windowHandle,
            $keyUpMessage,
            [UIntPtr]::new($virtualKey),
            [IntPtr]::new([int64]$keyUpLParamValue)
        )

        if (-not $keyUpPosted) {
            $lastError = [System.Runtime.InteropServices.Marshal]::GetLastWin32Error()
            throw "Failed to post WM_SYSKEYUP for virtual key 0xC0 to WildStar. Win32Error=$lastError"
        }
    }
    else {
        $altVirtualKey = [uint32]0x12
        $altScanCode = [WildStarConsoleToggleNativeV20260516]::MapVirtualKey($altVirtualKey, 0)

        if ($altScanCode -eq 0) {
            throw 'Failed to resolve a scan code for virtual key 0x12.'
        }

        $keyEventScanCode = [uint32]0x0008
        $keyEventKeyUp = [uint32]0x0002
        $altDownInput = New-Object WildStarConsoleToggleNativeV20260516+INPUT
        $altDownInput.type = 1
        $altDownInput.U.ki.wVk = 0
        $altDownInput.U.ki.wScan = [uint16]$altScanCode
        $altDownInput.U.ki.dwFlags = $keyEventScanCode
        $altDownInput.U.ki.time = 0
        $altDownInput.U.ki.dwExtraInfo = [UIntPtr]::Zero

        $keyDownInput = New-Object WildStarConsoleToggleNativeV20260516+INPUT
        $keyDownInput.type = 1
        $keyDownInput.U.ki.wVk = 0
        $keyDownInput.U.ki.wScan = [uint16]$scanCode
        $keyDownInput.U.ki.dwFlags = $keyEventScanCode
        $keyDownInput.U.ki.time = 0
        $keyDownInput.U.ki.dwExtraInfo = [UIntPtr]::Zero

        $keyUpInput = New-Object WildStarConsoleToggleNativeV20260516+INPUT
        $keyUpInput.type = 1
        $keyUpInput.U.ki.wVk = 0
        $keyUpInput.U.ki.wScan = [uint16]$scanCode
        $keyUpInput.U.ki.dwFlags = $keyEventScanCode -bor $keyEventKeyUp
        $keyUpInput.U.ki.time = 0
        $keyUpInput.U.ki.dwExtraInfo = [UIntPtr]::Zero

        $altUpInput = New-Object WildStarConsoleToggleNativeV20260516+INPUT
        $altUpInput.type = 1
        $altUpInput.U.ki.wVk = 0
        $altUpInput.U.ki.wScan = [uint16]$altScanCode
        $altUpInput.U.ki.dwFlags = $keyEventScanCode -bor $keyEventKeyUp
        $altUpInput.U.ki.time = 0
        $altUpInput.U.ki.dwExtraInfo = [UIntPtr]::Zero

        $inputs = [WildStarConsoleToggleNativeV20260516+INPUT[]]@($altDownInput, $keyDownInput, $keyUpInput, $altUpInput)
        $inputSize = [System.Runtime.InteropServices.Marshal]::SizeOf([type] [WildStarConsoleToggleNativeV20260516+INPUT])
        $sentCount = [WildStarConsoleToggleNativeV20260516]::SendInput([uint32]$inputs.Length, $inputs, $inputSize)

        if ($sentCount -ne $inputs.Length) {
            $lastError = [System.Runtime.InteropServices.Marshal]::GetLastWin32Error()
            throw "Failed to inject Alt plus VK 0xC0 with SendInput. Sent=$sentCount Expected=$($inputs.Length) Win32Error=$lastError"
        }
    }

    if (($InputMethod -eq 'SendInput') -and $consoleArgumentDetected -and $foregroundMatched) {
        if ($currentProcessElevated) {
            $suggestedNextStep = 'If the console remains invisible, rerun with -InputMethod PostMessage to deliver the exact WM_SYSKEYDOWN path that WildStar checks for the console toggle.'
        }
        else {
            $suggestedNextStep = 'If the console remains invisible, rerun with -InputMethod PostMessage and omit -SkipElevationPrompt so the helper can self-elevate and deliver the exact WM_SYSKEYDOWN path that WildStar checks for the console toggle.'
        }
    }

    $result = [pscustomobject]@{
        ProcessName      = $wildStarProcess.ProcessName
        ProcessId        = $wildStarProcess.Id
        WindowTitle      = $wildStarProcess.MainWindowTitle
        MainWindowHandle = ('0x{0:X}' -f $wildStarProcess.MainWindowHandle)
        CommandLine      = $commandLine
        ConsoleArgFound  = $consoleArgumentDetected
        CurrentProcessElevated = $currentProcessElevated
        VirtualKey       = '0xC0'
        ScanCode         = ('0x{0:X}' -f $scanCode)
        FocusWindow      = $FocusWindow.IsPresent
        ForegroundHandle = ('0x{0:X}' -f $foregroundWindowHandle.ToInt64())
        ForegroundMatch  = $foregroundMatched
        InputMethod      = $InputMethod
        MessagePath      = if ($InputMethod -eq 'PostMessage') { 'WM_SYSKEYDOWN/WM_SYSKEYUP' } else { 'Alt + VK 0xC0 via SendInput' }
        SendInputSize    = [System.Runtime.InteropServices.Marshal]::SizeOf([type] [WildStarConsoleToggleNativeV20260516+INPUT])
        SuggestedNextStep = $suggestedNextStep
        Result           = if ($InputMethod -eq 'PostMessage') { 'Posted WM_SYSKEYDOWN/WM_SYSKEYUP to WildStar main window.' } else { 'Injected Alt plus VK 0xC0 through SendInput.' }
    }

    Write-ResultPayload -Status 'Success' -Result $result
    Write-Output $result
}
catch {
    Write-ResultPayload -Status 'Error' -Result $null -Message $_.Exception.Message
    throw
}