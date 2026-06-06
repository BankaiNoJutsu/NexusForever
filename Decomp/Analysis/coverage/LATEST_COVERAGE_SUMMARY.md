# Decomp Coverage Snapshot

## Export Coverage

| Target | Functions | Named % | Cache % | Remaining | Selected | Focused % |
| --- | --- | --- | --- | --- | --- | --- |
| `Houston64.exe` | 25129 | 31.39% | 100% | 0 | 1631 | 0.2% |
| `StsConnLib64.MT.dll` | 4522 | 8.31% | 100% | 0 | 864 | 19.11% |
| `WildStar64.exe` | 25050 | 8.1% | 100.4% | 0 | 3738 | 2% |

## Full Function Backlog

| Scope | Functions | Named | Selected | Unselected | Selected % |
| --- | --- | --- | --- | --- | --- |
| Default targets | 54701 | 10293 | 6233 | 48468 | 11.39% |

## Opcode Coverage

| Direction | Total | Implemented | Partial | Missing |
| --- | --- | --- | --- | --- |
| Client | 351 | 351 | 0 | 0 |
| Server | 702 | 701 | 1 | 0 |
| Core | 3 | 3 | 0 | 0 |

## Queue: Client Opcodes Missing Models

None.

## Queue: Client Opcodes Missing Handlers

None.

## Queue: Server Opcodes Missing Models

None.

## Queue: Placeholder Models To Decode

| Opcode | Hex | Model | Comment |
| --- | --- | --- | --- |
| `Server0x0015` | 0x0015 | `Server0x0015` | native 140081f00 reads one 5-bit field plus one uint32; shared with matching opcode 0x0628 and Fortune reward rows; 2026-06-05 cache/xref recheck found no 0x0015 apply owner equivalent to MatchingManager_ApplyMatchingAverageWaitTimeUpdated for 0x0628, so semantics remain unresolved |
| `Client0x00C8` | 0x00C8 | `Client0x00C8` | Network_RegisterServerOpcode_0351 @ 14006c290 binds decimal 200 to shared ClientMatchType_ReadPayload 14008a140 / WritePayload 14008a150 with size 4; TraceFunctionCallers 14008a150 finds registration refs only; send_helper and Network_SendMessageById filters blocked — do not alias to queue-leave or challenge (0x00C5 owns challenge sends) |
| `Client0x00ED` | 0x00ED | `Client0x00ED` | uint64 + uint32 + uint64 + three trailing bits; TraceFunctionCallers 1400a6200 finds registration/data refs only; exact 0xED immediate hits resolve to mail-side GameFormula lookups, not packet ownership |
| `Client0x011B` | 0x011B | `Client0x011B` | native client registration binds 0x011B to shared zero-payload ClientCraftingAbandon_WritePayload 140001ba0; TraceFunctionCallers finds shared registration/data refs only; pass 94 non-registration hits are Lua token ids / GameFormula, not senders |
| `Client0x011D` | 0x011D | `Client0x011D` | native client registration binds 0x011D to shared ClientTradeskillResetTalents_WritePayload 14007d010; TraceFunctionCallers found 67 shared refs; pass 94 spell-cast hit is CastResult.IllegalSpellCast, not a sender |
| `Client0x012D` | 0x012D | `Client0x012D` | native client registration binds 0x012D to shared ClientSuggest_WritePayload 14007ae80; TraceFunctionCallers finds registration/data refs only; exact scan finds Support_SendClientSuggest for 0x0833, not this opcode |
| `Client0x0550` | 0x0550 | `Client0x0550` | ClientWorldOpcodeRegister_MovementSpline 1400a8190 @ 1400a824e: 4-byte uint32 14007d010; exact opcode scan finds 0x0550 only here, TraceFunctionCallers finds no owner, and message-name slot 140c247e0 is undefined; not ICComm, spell-list, 0x05D5, 0x0635, or 0x0858 |
| `Client0x062A` | 0x062A | `Client0x062A` | ClientWorldOpcodeRegister_MovementSpline 1400a8190 @ 1400a82b6 + ClientTradeskillResetTalents_WritePayload 14007d010; 2026-06-05 MCP/cache recheck found selected export 0x62A only in registration; not 0x05D5 or 0x0635; sender blocked - F-010 live sniff recommended |
| `Client0x0634` | 0x0634 | `Client0x0634` | ClientWorldOpcodeRegister_MovementSpline 1400a8190 @ 1400a872c + same writer as 0x062A; 2026-06-05 MCP/cache recheck found selected export 0x634 only in registration; not 0x05D5 or 0x0635; sender blocked - F-010 live sniff recommended |
| `Client0x063E` | 0x063E | `Client0x063E` | ClientWorldOpcodeRegister_MovementSpline 1400a8190 @ 1400a821a binds 0x063E to ClientSuggest_WritePayload 14007ae80; TraceFunctionCallers found only shared registration/data refs, and sibling 0x0833/0x0233/0x07C6 senders are not evidence — sender blocked |
| `Client0x0701` | 0x0701 | `Client0x0701` | Network_RegisterServerOpcode_0351 @ 14006c290 (not ClientWorldOpcodeRegister_MovementSpline 1400a8190); writer ClientUInt2UInt32_WritePayload @ 1400a69d0 (2-bit + uint32); sole PE MOV EDX,0x701 @ 140079e05 (registration harness batch with 0x0942/0x0602 — not gameplay); semantics unresolved |
| `Client0x07E3` | 0x07E3 | `Client0x07E3` | ClientWorldOpcodeRegister_MovementSpline 1400a8190 @ 1400a8282 + ClientTradeskillResetTalents_WritePayload 14007d010; PE mov eax,0x7E3 count=1 (registration only); not 0x05D5; current model stays one raw uint32 diagnostic field |
| `Client0x0928` | 0x0928 | `Client0x0928` | ClientWorldOpcodeRegister_MovementSpline @ 1400a8190 registers 8-byte slot with shared ClientUInt32UInt5_WritePayload @ 1400898b0 and shared ServerUInt32UInt5_ReadPayload @ 14008ce80 (also ServerPetStanceChanged 0x068F); PE send for 0x928 only in registration harness @ 1400a8524 — gameplay sender blocked; do not alias to ClientPetSetStance without second witness |

Generated artifacts:

- ``Decomp/Analysis/coverage/LATEST_COVERAGE_SUMMARY.md``
- ``Decomp/Analysis/logs/LATEST_COVERAGE_SUMMARY.json``
- ``Decomp/Analysis/coverage/export_coverage_inventory.csv``
- ``Decomp/Analysis/coverage/opcode_coverage_inventory.csv``
