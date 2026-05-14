# Client64 Initial Reverse-Engineering Findings

Generated from the Ghidra headless project in `Decomp\Analysis\ghidra_projects`
and exports in `Decomp\Analysis\exports`.

## Export Coverage

| Binary | Functions | Strings | Interesting strings | Selected xrefs |
| --- | ---: | ---: | ---: | ---: |
| `WildStar64.exe` | 24,968 | 45,034 | 10,816 | 3,062 |
| `Houston64.exe` | 25,129 | 29,460 | 6,288 | 1,944 |
| `StsConnLib64.MT.dll` | 4,522 | 10,893 | 3,294 | 2,181 |

The selected C exports are biased toward protocol/data anchors, so they are a
starting point rather than a complete native source reconstruction.

## High-Value Anchors

### STS auth and transaction flow

`StsConnLib64.MT.dll` contains direct strings for the STS auth transaction names:
`KeyData`, `LoginStart`, `LoginFinish`, `RequestGameToken`, and `TokenKeyData`.
The selected decompile puts them together near
`exports\StsConnLib64.MT.dll\selected_decompiled.c:8`.

This lines up with NexusForever's current STS handlers:

- `Source\NexusForever.StsServer\Network\Message\Handler\AuthenticationHandler.cs`
- `Source\NexusForever.Network.Sts\Model\ClientLoginStartMessage.cs`
- `Source\NexusForever.Network.Sts\Model\RequestGameTokenMessage.cs`

The same binary also exposes source-path strings for the socket layer:

- `Services\StsInetSocket\StsInetSocket.cpp` around `selected_decompiled.c:620`
- `Services\SocketCrypt\SocketCrypt.cpp` around `selected_decompiled.c:1793`

Those are the best first targets for auth framing, transaction response codes,
timeouts, and STS socket crypto behavior.

### World network send path

`WildStar64.exe` includes `Network_SendMessageById`, selected at
`exports\WildStar64.exe\selected_decompiled.c:3437`. The same export includes
the surrounding socket/WorldSocket registration area at
`selected_decompiled.c:3548`.

The imports also show the expected WinSock path in `WildStar64.exe`:
`WSAStartup`, `WSASocketW`, `recv`, `send`, `WSARecv`, `WSASend`, `connect`,
`ioctlsocket`, `socket`, `closesocket`, `gethostbyname`, and `select`.

### Client DB table registration

Both `WildStar64.exe` and `Houston64.exe` contain table-registration patterns
that call into a common loader with the display table name and `DB\*.tbl` path.

Useful examples:

- `WildStar64.exe`: `RealmDataCenter` / `DB\RealmDataCenter.tbl` at `selected_decompiled.c:3247`.
- `WildStar64.exe`: `WorldSocket` / `DB\WorldSocket.tbl` at `selected_decompiled.c:3342`.
- `Houston64.exe`: `RealmDataCenter` / `DB\RealmDataCenter.tbl` at `selected_decompiled.c:103`.
- `Houston64.exe`: `WorldSocket` / `DB\WorldSocket.tbl` at `selected_decompiled.c:198`.

This supports using the decompile to validate `Tools\DataMapping\map_wildstar_data.py`
for table names and field consumers, especially `WorldSocket.tbl.sql` and
`RealmDataCenter.tbl.sql`.

### Public event and spell/data names

`WildStar64.exe` includes public-event enum/name registration around
`exports\WildStar64.exe\selected_decompiled.c:6989`, including names such as
`PublicEventObjectiveType_Script`, `PublicEventObjectiveType_ParticipantsInTriggerVolume`,
and `PublicEventObjectiveType_KillEventObjectiveUnit`.

This is useful evidence for checking:

- `Source\NexusForever.Game.Static\PublicEvent\PublicEventObjectiveType.cs`
- `Source\NexusForever.Game\PublicEvent\*.cs`
- current expedition scripts that update public-event objectives

The selected export also captures `tSpell4IdAbility` at
`exports\WildStar64.exe\selected_decompiled.c:10260` and `tUnitProperty` at
`selected_decompiled.c:10983`, which are good anchors for spell effect and unit
property interpretation work.

## Practical Next Steps

1. Rename the high-value functions in the Ghidra project, starting with the STS
   transaction selector, `StsInetSocket` handlers, `Network_SendMessageById`,
   and the DB table registration functions.
2. Re-run `.\Decomp\Analysis\run_ghidra_analysis.ps1 -ExportOnly` after renames
   so the exported C becomes progressively easier to read.
3. Cross-check constants and field reads in the selected C against
   `GameMessageOpcode.cs`, the STS models, and generated `GameTable` models.
4. Add narrower high-value patterns to `ExportNexusForeverAnalysis.java` when a
   new subsystem becomes the focus.
