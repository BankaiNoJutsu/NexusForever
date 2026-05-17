# Decomp Coverage Snapshot

## Export Coverage

| Target | Functions | Durable labels | Interesting strings | Selected | Decompiled |
| --- | --- | --- | --- | --- | --- |
| `Houston64.exe` | 25129 | 7508 | 6317 | 0 | 0 |
| `StsConnLib64.MT.dll` | 4522 | 371 | 3406 | 863 | 40 |
| `WildStar64.exe` | 24983 | 959 | 11000 | 2181 | 200 |

## Opcode Coverage

| Direction | Total | Implemented | Partial | Missing |
| --- | --- | --- | --- | --- |
| Client | 326 | 326 | 0 | 0 |
| Server | 570 | 547 | 16 | 7 |
| Core | 3 | 1 | 0 | 2 |

## Queue: Client Opcodes Missing Models

None.

## Queue: Client Opcodes Missing Handlers

None.

## Queue: Server Opcodes Missing Models

| Opcode | Hex | Comment |
| --- | --- | --- |
| `ServerCharacterAppearanceResult` | 0x0145 |  |
| `ServerFlightPathUpdate` | 0x0188 |  |
| `ServerAttributePoints` | 0x019E |  |
| `ServerSpellList` | 0x0551 |  |
| `Server081A` | 0x081A | spline related |
| `Server081B` | 0x081B | spline related |
| `Server081C` | 0x081C | spline related |

## Queue: Placeholder Models To Decode

| Opcode | Hex | Model | Comment |
| --- | --- | --- | --- |
| `Server0237` | 0x0237 | `Server0237` | UI related, opens or closes different UI windows (bank, barber, ect...) |
| `Server0635` | 0x0635 | `Server0635` |  |
| `Server07F5` | 0x07F5 | `Server07F5` | spell broadcast family: opaque follow-up after 0x07F4/0x07FF |
| `Server07F6` | 0x07F6 | `Server07F6` | spell broadcast family: opaque follow-up after 0x07F4/0x07FF |
| `Server07F7` | 0x07F7 | `Server07F7` | spell broadcast family: opaque follow-up after 0x07F4/0x07FF |
| `Server07F8` | 0x07F8 | `Server07F8` | spell broadcast family: nested effect follow-up, payload still unnamed |
| `Server07F9` | 0x07F9 | `Server07F9` | spell broadcast family: live cast cancel notification |
| `Server07FA` | 0x07FA | `Server07FA` | spell broadcast family: opaque follow-up after 0x07F4/0x07FF |
| `Server07FB` | 0x07FB | `Server07FB` | spell broadcast family: likely miss or immunity target report |
| `Server07FD` | 0x07FD | `Server07FD` | spell broadcast family: position or telegraph sync follow-up |
| `Server0811` | 0x0811 | `Server0811` | spell broadcast family: likely partial target list or 0x07FF subset |
| `Server0814` | 0x0814 | `Server0814` | spell broadcast family: opaque spell trigger or follow-up flag |
| `Server0816` | 0x0816 | `Server0816` | spell broadcast family: root or parent spell hierarchy follow-up |
| `Server0817` | 0x0817 | `Server0817` | spell broadcast family: opaque spell event byte |
| `Server0818` | 0x0818 | `Server0818` | spell broadcast family: target-info follow-up seen near telegraph buffs |
| `Server089B` | 0x089B | `Server089B` | mount related |

Generated artifacts:

- ``Decomp/Analysis/coverage/LATEST_COVERAGE_SUMMARY.md``
- ``Decomp/Analysis/logs/LATEST_COVERAGE_SUMMARY.json``
- ``Decomp/Analysis/coverage/export_coverage_inventory.csv``
- ``Decomp/Analysis/coverage/opcode_coverage_inventory.csv``
