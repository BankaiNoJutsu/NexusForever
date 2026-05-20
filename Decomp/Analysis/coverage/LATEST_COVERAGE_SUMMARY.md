# Decomp Coverage Snapshot

## Export Coverage

| Target | Functions | Named % | Default-name backlog | Selected | Decompiled | Full % |
| --- | --- | --- | --- | --- | --- | --- |
| `Houston64.exe` | 25129 | 29.96% | 17600 | 1597 | 1597 | 6.36% |
| `StsConnLib64.MT.dll` | 4522 | 8.2% | 4151 | 863 | 863 | 19.08% |
| `WildStar64.exe` | 24993 | 4.88% | 23773 | 2401 | 200 | 0.8% |

## Full Function Backlog

| Scope | Functions | Named | Selected | Unselected | Selected % |
| --- | --- | --- | --- | --- | --- |
| Default targets | 54644 | 9120 | 4861 | 49783 | 4.87% |

## Opcode Coverage

| Direction | Total | Implemented | Partial | Missing |
| --- | --- | --- | --- | --- |
| Client | 347 | 330 | 0 | 17 |
| Server | 692 | 572 | 0 | 120 |
| Core | 3 | 3 | 0 | 0 |

## Queue: Client Opcodes Missing Models

| Opcode | Hex | Comment |
| --- | --- | --- |
| `Client0x003D` | 0x003D | 0x18-byte payload; semantics unresolved |
| `Client0x00C8` | 0x00C8 | 0x4-byte payload; semantics unresolved |
| `Client0x00ED` | 0x00ED | 0x20-byte payload; semantics unresolved |
| `Client0x011B` | 0x011B | 0x1-byte payload; semantics unresolved |
| `Client0x011D` | 0x011D | 0x4-byte payload; semantics unresolved |
| `Client0x012D` | 0x012D | 0x8-byte payload; semantics unresolved |
| `Client0x0142` | 0x0142 | 0x10-byte payload; semantics unresolved |
| `Client0x0550` | 0x0550 | single 32-bit payload field; semantics unresolved |
| `Client0x062A` | 0x062A | single 32-bit payload field; semantics unresolved |
| `Client0x0634` | 0x0634 | single 32-bit payload field; semantics unresolved |
| `Client0x063E` | 0x063E | one wide-string payload field; semantics unresolved |
| `Client0x0701` | 0x0701 | 0x8-byte payload; semantics unresolved |
| `Client0x0760` | 0x0760 | 0x58-byte payload; semantics unresolved |
| `Client0x0762` | 0x0762 | 0x10-byte payload; semantics unresolved |
| `Client0x07B6` | 0x07B6 | 0x20-byte payload; semantics unresolved |
| `Client0x07E3` | 0x07E3 | single 32-bit payload field; semantics unresolved |
| `Client0x0928` | 0x0928 | 8-byte payload; semantics unresolved |

## Queue: Client Opcodes Missing Handlers

None.

## Queue: Server Opcodes Missing Models

| Opcode | Hex | Comment |
| --- | --- | --- |
| `Server0x00B0` | 0x00B0 | 18-bit scalar payload; semantics unresolved |
| `Server0x00B7` | 0x00B7 | 0x1-byte payload; semantics unresolved |
| `Server0x00CA` | 0x00CA | 0x10-byte payload; semantics unresolved |
| `Server0x00CB` | 0x00CB | 0x1-byte payload; semantics unresolved |
| `Server0x00CC` | 0x00CC | 0x4-byte payload; semantics unresolved |
| `Server0x00CD` | 0x00CD | 0x4-byte payload; semantics unresolved |
| `Server0x00CE` | 0x00CE | 0x8-byte payload; semantics unresolved |
| `Server0x00D1` | 0x00D1 | 0x1-byte payload; semantics unresolved |
| `Server0x00DF` | 0x00DF | 0x1-byte payload; semantics unresolved |
| `Server0x00EE` | 0x00EE | 0x1-byte payload; semantics unresolved |
| `Server0x0101` | 0x0101 | 0x1-byte payload; semantics unresolved |
| `Server0x010D` | 0x010D | 0x1-byte payload; semantics unresolved |
| `Server0x0110` | 0x0110 | 0x10-byte payload; semantics unresolved |
| `Server0x0139` | 0x0139 | 0x10-byte payload; semantics unresolved |
| `Server0x0143` | 0x0143 | 0x1-byte payload; semantics unresolved |
| `Server0x014D` | 0x014D | 0x1-byte payload; semantics unresolved |
| `Server0x0157` | 0x0157 | 0x8-byte payload; semantics unresolved |
| `Server0x0160` | 0x0160 | 0x1-byte payload; semantics unresolved |
| `Server0x016B` | 0x016B | 0x20-byte payload; semantics unresolved |
| `Server0x016D` | 0x016D | 0x4-byte payload; semantics unresolved |
| `Server0x016E` | 0x016E | 0x4-byte payload; semantics unresolved |
| `Server0x0181` | 0x0181 | 0x4-byte payload; semantics unresolved |
| `Server0x0183` | 0x0183 | uint32 + flag payload; semantics unresolved |
| `Server0x0186` | 0x0186 | 0x4-byte payload; semantics unresolved |
| `Server0x0187` | 0x0187 | 0x1-byte payload; semantics unresolved |

## Queue: Placeholder Models To Decode

None.

Generated artifacts:

- ``Decomp/Analysis/coverage/LATEST_COVERAGE_SUMMARY.md``
- ``Decomp/Analysis/logs/runs/inspect_1405770f0/LATEST_COVERAGE_SUMMARY.json``
- ``Decomp/Analysis/coverage/export_coverage_inventory.csv``
- ``Decomp/Analysis/coverage/opcode_coverage_inventory.csv``
