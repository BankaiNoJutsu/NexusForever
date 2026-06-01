# WildStar Client Logging (Build 16042)

Evidence-backed notes for the retail `WildStar64.exe` logging systems and how
NexusForever setup scripts pass the mapped command-line switches.

This is separate from NexusForever server logging (`-LogLevel` on
`Start-NexusForeverLocal.ps1`, which configures NLog for auth/world only).

## Quick Start (NexusForever)

Enable the in-game dev console and verbose client file logging in one launch:

```powershell
.\Tools\Setup\Start-NexusForeverLocal.ps1 `
  -ClientDirectory "I:\WildStar" `
  -EnableClientConsole `
  -EnableClientLogging `
  -PromptForRootPassword
```

Fast local restart loop with the same client flags:

```powershell
.\Tools\Setup\Restart-NexusForeverLocal.ps1 `
  -ClientDirectory "I:\WildStar" `
  -EnableClientConsole `
  -EnableClientLogging `
  -PromptForRootPassword
```

Optional tuning:

| Launcher switch | Client effect |
|-----------------|---------------|
| `-EnableClientConsole` | Appends `-Console` (dev console UI; toggle with Alt+VK `0xC0`) |
| `-EnableClientLogging` | Appends `-logFile`, `-logFlush`, and `-logDefaultLevel` |
| `-ClientLogLevel Trace` | Sets `-logDefaultLevel` (`Error`=0 … `Trace`=4; default `Trace`) |
| `-ClientLogStdout` | Also appends `-logStdout` |
| `-ClientLogDir "D:\Logs"` | Appends `-logDir` (override default `<install>\Logs`) |
| `-ClientArguments …` | Pass any additional retail switches verbatim |

Staged arguments are written to `Client64\config.json` as `ExtraArguments` and
reused on later runs unless you pass an explicit override switch above.

Tail client output after launch:

```powershell
Get-ChildItem "I:\WildStar\Logs\*.txt" | Sort-Object LastWriteTime -Descending | Select-Object -First 1 | Get-Content -Wait -Tail 200
Get-ChildItem "I:\WildStar\Errors\WildStar64*.log" | Sort-Object LastWriteTime -Descending | Select-Object -First 1 | Get-Content -Wait -Tail 200
```

Manual retail launch equivalent:

```text
WildStar64.exe … -Console -logFile -logFlush -logDefaultLevel 4
```

## Systems Overview

The client has four related but independent mechanisms:

| System | Purpose | Controlled by |
|--------|---------|---------------|
| **`-Console`** | In-game dev console UI (`UI\ConsoleForm.xml`) | Only checked when toggling console (`FUN_1400149a0`); not log init |
| **`CLog`** | Main engine log (file/console/stdout/stderr) | `-log*` switches, registry `Flags`, init in `FUN_1401a1530` |
| **`DebugLogService`** | Category-filtered debug output in gameplay code | Separate from `-logDefaultLevel`; global `DAT_140c658d8` |
| **`Errors\WildStar64*.log`** | Fatal/client error reports | Written on crashes and severe decode failures (`FUN_1401909c0` path) |

Shipping builds set `DAT_140c63734 = 1`, which suppresses `OutputDebugString`
even with a debugger attached. File and CLI logging still work.

## CLog Initialization Chain

```
entry
  └─ FUN_1400086b0          early startup
       ├─ FUN_1401a3470
       ├─ FUN_14018cc40
       └─ FUN_140199e60
  └─ FUN_14000ac90          CommandLineToArgvW startup
       └─ FUN_1401a1530     CLog init (registry + argv parsing)
```

Write path:

```
FUN_1401a2e50 / FUN_1401a3130  →  FUN_1401a2460  (sink)
```

Key globals:

| Address | Role |
|---------|------|
| `DAT_140c674a4` | Output flags (file, ring buffer, stdout, stderr, flush, timestamps, …) |
| `DAT_140c674a8` | Per-facility severity thresholds (131 entries, default `2` = Info) |
| `DAT_140c676c8` | Log file handle |
| `HKLM\SOFTWARE\NCSOFT\WildStar\<subkey>` | Registry `Flags` DWORD merged before argv overrides |

Default directories (relative to `Client64`):

- Logs: `..\Logs`
- Errors: `..\Errors`

Log filename pattern (`DAT_1409e2144` / string `140a44e20`):

```text
%s\%s_%d_%s_%s%0.2u%0.2u%0.2u_%0.2u%0.2u%0.2u.%04x.txt
```

## Command-Line Switches

Parsed in `FUN_1401a1530` (strings at `140a44a98`–`140a44c90`):

| Switch | Effect |
|--------|--------|
| `-logFile` / `-logNoFile` | Enable/disable file sink |
| `-logFlush` / `-logNoFlush` | Flush after each write |
| `-logTimestamp` / `-logNoTimestamp` | Timestamp prefix |
| `-logFacility` / `-logNoFacility` | Facility name prefix |
| `-logDefaultLevel <N>` | Set all facility thresholds to `<N>` |
| `-log<FacilityName> <N>` | Per-facility threshold |
| `-logNo<FacilityName>` | Disable facility (`0xffffffff`) |
| `-logFileName <path>` | Explicit log file path |
| `-logDir <path>` | Log directory (default `..\Logs`) |
| `-errDir <path>` | Error report directory (default `..\Errors`) |

Accepted forms use `-` or `/` prefix (both are recognized in the argv scanner).

## Output Flags (`DAT_140c674a4`)

| Bit | Meaning |
|-----|---------|
| `0x1` | File |
| `0x2` | In-game console ring buffer |
| `0x4` | stdout |
| `0x8` | stderr |
| `0x10` | Flush |
| `0x20` | Timestamps |
| `0x40` | Suppress facility prefix |
| `0x100` | PID in filename |

## Severity Levels

Facility thresholds and message severity both use numeric levels `0`–`4`.
A message is emitted when `messageSeverity <= facilityThreshold`.

| Level | Name | Numeric |
|-------|------|---------|
| Error | `Error` | 0 |
| Warn | `Warn` | 1 |
| Info | `Info` | 2 (default per-facility threshold) |
| Debug | `Debug` | 3 |
| Trace | `Trace` | 4 |

Severity label strings: `140a44a98` (Trace) through `140a44b18` (Warn), indexed
through `PTR_u_Error_140a44ec8` in `FUN_1401a2460`.

Recommended verbose profile: `-logDefaultLevel 4` (Trace).

## Facilities

Facility names come from pointer table `PTR_u_None_140c2ce70` (131 entries,
indices `0`–`0x82`). Index `0` is `"None"`. Per-facility switches are built as
`-log<Name>` / `-logNo<Name>` during init (`FUN_1401a1530`).

The full facility name list has not been dumped from the binary yet; export the
data section at `140c2ce70` in Ghidra when you need exact names such as
`-logNetwork`.

## `-Console` vs Logging

`-Console` is referenced only in `FUN_1400149a0`: the client checks argv for
`-Console` (or `/Console`) when Alt+VK `0xC0` is pressed, then toggles the
console UI. It does not participate in `FUN_1401a1530` log initialization.

Use `.\Tools\Setup\Send-WildStarConsoleToggle.ps1` if the physical key for VK
`0xC0` is awkward on your keyboard layout.

## DebugLogService

Separate from `CLog`:

| Address | Role |
|---------|------|
| `DAT_140c658d8` | DebugLogService global |
| `FUN_140436fb0` | Creates the service |
| `FUN_140437a10` | Category-filtered write helper used across gameplay code |

Not controlled by `-logDefaultLevel`. See `INITIAL_FINDINGS.md` (`DebugLogService`
row) for related entity debug flags.

## Evidence Sources

Decompile fragments under
`Decomp/Analysis/exports/WildStar64.exe/selected_decompiled_cache/functions/…/`:

| Address | Label / role |
|---------|--------------|
| `1401a1530` | CLog init (registry + argv) |
| `1401a2460` | CLog sink |
| `1401a2e50` | Formatted ASCII log write |
| `1401a3130` | Wide / fatal log path |
| `1400149a0` | Console toggle (`-Console` gate) |
| `140436fb0` / `140437a10` | DebugLogService |

Strings: `Decomp/Analysis/exports/WildStar64.exe/strings.csv` around
`140a44a98`–`140a44ef0`.

Setup integration: `Tools/Setup/WildStarClientLaunch.ps1`,
`Tools/Setup/Start-NexusForeverLocal.ps1`,
`Tools/Setup/Restart-NexusForeverLocal.ps1`.
