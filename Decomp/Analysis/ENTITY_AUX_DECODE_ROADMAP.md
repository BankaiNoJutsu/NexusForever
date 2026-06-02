# Entity / Cluster Aux Opcode Decode Roadmap

Updated: 2026-05-23 (third pass: entity-create emit marked implemented in ladder table)

Tracks decompile progress to unblock **field semantics** and **runtime emitters** for the
shape-mapped server-output clusters that replaced `Server0xNNNN` placeholders.

## Baseline counts

| Bucket | Count | Notes |
| --- | ---: | --- |
| Server opcodes with wire models | **699** | Structural queue empty |
| Shape-mapped aux + spell aux (neutral `ValueN` / raw buffer) | **62** | **12/62** production emit paths (entity-create + housing); **50** still blocked |
| Entity-create aux (`0x025F`-`0x0264`) | **5** | Adjacent to `ServerEntityCreate` (`0x0262`) in registration order |
| Entity-stat aux (`0x0889`, `0x08CC`, `0x08F4`, `0x0939`, `0x093D`, `0x093E`) | **6** | Several readers shared with other opcodes |
| Cluster aux (`ServerClusterAuxPackets.cs`) | **35** | Item/options/path/chat/marketplace/PE/story/realm/recruitment bands |
| Housing aux (`ServerHousingAuxPackets.cs`) | **7** | Wire shapes mapped; consumer intent blocked |
| Spell aux (threshold/wrapper + triplet lists) | **9** | Readers mapped; gameplay emit blocked |

Partial semantics on **named** packets (group, housing community, reward rotation, store,
map-tracked-unit, etc.) remain tracked in `MISSING_FEATURE_MATRIX.md` feature rows.

## Evidence ladder status (entity-create aux)

| Opcode | Enum / model | Client reader | Object size (registration) | Direct code callers | Consumer / emit |
| --- | --- | --- | --- | --- | --- |
| `0x025F` | `ServerEntityCreateAuxRow` | `ServerEntityCreateAuxRow_ReadPayload` @ `140095c20` | `0x2C` | Registration table + `0x0260` list reader | **Emitted** (`BuildEntityCreateAuxPackets`; consumer names blocked) |
| `0x0260` | `ServerEntityCreateAuxRowList` | `ServerEntityCreateAuxRowList_ReadPayload` @ `140095ce0` | `0x10` (count + pointer) | Registration table only | **Emitted** (same builder) |
| `0x0261` | `ServerEntityCreateAuxBitPackedRowList` | `ServerEntityCreateAuxBitPackedRowList_ReadPayload` @ `140095a80` | `0x10` | Registration table only | **Emitted** (same builder) |
| `0x0263` | `ServerEntityCreateAuxBitPackedRow` | `ServerEntityCreateAuxBitPackedRow_ReadPayload` @ `1400959c0` | `0x24` | Registration table + `0x0261` list reader | **Emitted** (same builder) |
| `0x0264` | `ServerEntityCreateAuxScalarList` | `ServerEntityCreateAuxScalarList_ReadPayload` @ `140095b40` | `0x18` | Registration table only | **Emitted** (same builder) |

Registration evidence: `Network_RegisterServerOpcode_0351` @ `14006c290` (see
`selected_decompiled.c` registration block ~line 2313).

List readers allocate `count * rowSize` and call the row reader in a loop (`IMUL 0x2c` /
`IMUL 0x24`), matching NexusForever packet-shape tests.

### Shared helpers (newly labeled)

| Address | Label | Role |
| --- | --- | --- |
| `1400a8a30` | `ServerUInt32Triple_ReadPayload` | Reads three consecutive `uint32` values (used by `0x025F` / `0x0263` row readers). |
| `14006c090` | (existing) | Bit/network field reader used for sized fields. |

### Reader alias notes

| Reader | Opcodes sharing it |
| --- | --- |
| `ServerSpellUInt32TripletListRow_ReadPayload` @ `140080bf0` | `0x0889`, `0x0908`, `0x090A` (12-byte triplet; registration @ `140078cb0`, `1400751a3`, `1400751d4`) |
| `ServerEntityStatUInt32UInt5UInt32_ReadPayload` @ `140097620` | `0x08F4`, `0x938`, `0x93B` (registration table) |
| `ServerUInt32WideString_ReadPayload` @ `1400980f0` | `0x08CC` (`Network_RegisterServerOpcode_08CC` @ `140075236`) and others |

Do not assign one gameplay meaning per opcode until per-opcode consumer handlers are mapped.

## Why xrefs do not show "HandleServer*" consumers

`TraceFunctionCallers` on `ServerEntityCreateAuxRow_ReadPayload` (`140095c20`) finds only:

1. `ServerEntityCreateAuxRowList_ReadPayload` @ `140095ce0` (list composition)
2. `Network_RegisterServerOpcode_0351` @ `14006c290` (pointer stored in registration table)
3. Data table pointer (`140dcf6e8`)

There is **no** direct `Loot_Handle*` / `AccountItemAddToCache_HandleServer096A`-style function
in the xref graph. Consumers are reached through the **indirect** live dispatch path below,
not through static calls to the reader.

## Live server-message dispatch (mapped 2026-05-23)

| Stage | Address | Label | Role |
| --- | --- | --- | --- |
| Socket ctor | `14000a490` | `WorldSocket_Ctor` | Sets `object+0x100` -> callback table `PTR_LAB_140b55100` @ `140b55100`. |
| Filter phase | `140014d30` | `WorldSocket_FilterServerMessageHandlers` | Walks `*(socket+0x14b0)` linked list; each node `vtable+0x50(handler, conn, messageCategory)`. |
| Deserialize | `DAT_140c65808` | (network service) | `vtable+0x100` builds parsed payload pointer (`local_e0`) from opcode + raw buffer. |
| Fast paths | `140003fb0`, `140452670`, storefront | `AccountInventory_HandleServer0966To0980`, ... | Run before handler chain when enabled (`socket+0x14d0`, globals). |
| Apply phase | `140014f10` | `WorldSocket_ProcessServerMessage` | Walks same `socket+0x14b0` list; each node `vtable+0x58(handler, conn, opcode, parsedPayload)`. Returns `<0` error, `0` handled, `>0` try next. |
| Handler registration | `140356a30` | `WorldZone_InsertUnitHandlerIntoList` | Example head-insert into `parent+0x14b0` for visible units (not opcode-specific). |

Focused follow-up on `WorldZone_InsertUnitHandlerIntoList` changes the next anchor:

- `InspectCodeAddresses` on `140356a30` shows the helper does **not** insert the zone/world object itself. It walks visible-unit node lists rooted at `param_1+0x1488` plus the local spatial-bucket families and splices each `plVar4` node into `param_1+0x14b0` with back-links at `plVar4+0x450`; a sibling family is inserted into `param_1+0x13c0` with back-links at `plVar4+0x4d0`.
- The sole direct caller `FUN_14036dd50` is therefore the owning zone/world update method, not the handler consumer family. `FindPointerInData` on `14036dd50` hit `.rdata` cell `140b65a10`, but the adjacent code cell `14036dd30` is only a tiny getter from `this+0x12b0`; do **not** treat that recovered vtable as the `+0x50` / `+0x58` handler table.
- A `FindImmediateInstructions` scan for `0x14b0` also surfaced sibling zone methods `14035c650`, `140369f30`, `14036a460`, `14036a980`, and `14036b8d0`, each splicing or unlinking the same `+0x450` / `+0x458` node family around `param_1+0x1488` and `param_1+0x14b0`.
- Practical consequence: the next pass should recover the inserted `plVar4` node vtables themselves, using that sibling method cluster as the concrete next-address set, not keep chasing the caller's own vtable or already named loot consumer bodies.

Callback table @ `140b55100` (installed at socket `+0x100`):

| Index | Offset | Value | Notes |
| ---: | ---: | --- | --- |
| 0 | `+0x00` | `140014c40` | Data/thunk slot (not a standalone function). |
| 1 | `+0x08` | `140014d30` | Filter phase. |
| 2 | `+0x10` | `140014f10` | Apply phase (**post-read consumer entry**). |
| 3 | `+0x18` | `14001b000` | Next callback; not yet labeled. |

`FindCallsToTarget` reports **zero** direct `CALL` sites to `140014f10` / `140014d30`; only
`.rdata` pointer slots (`140b55108`, `140b55110`). `AccountInventory_HandleServer0966To0980`
signature matches the `+0x58` handler shape `(ctx, ?, opcode, payload*)` but is invoked from
the dedicated fast-path gate inside `WorldSocket_ProcessServerMessage`, not from the linked list.

**Still blocked:** which inserted `plVar4` handler-node vtables actually cover opcodes
`0x025F`-`0x0264` (and the other **62** shape-mapped aux packets). Loot handlers (`1403db050`, ...)
use parsed-object type tag `*(payload+0x80)==0x14` and are not registered as the linked-list
`vtable+0x58` handlers. The direct caller/vtable recovered from `14036dd50` is a zone/world
owner false lead, not the consumer family.

Parallel **queued replay** path (already mapped elsewhere, not yet tied to `0x025F`):

- `QueuedStateReplay_FlushPendingRecords` @ `1405cd070`
- `QueuedStateReplay_DispatchRecord` @ `1405cd200` - switch on **replay record type** (`*param_2`), case `0x12` -> `FUN_1405cef50` -> `FUN_1405caf20` -> `FUN_140456960` (`UnitCreated` string cluster)
- Do **not** assume `0x025F` maps 1:1 to a replay case without proof.

## NexusForever implementation gate

Production emitters are **partial** (2026-05-23):

| Cluster | Count | Production emit site | Status |
| --- | ---: | --- | --- |
| Entity-create aux | 5 | `Player.AddVisible` -> `BuildEntityCreateAuxPackets()` before `ServerEntityCreate` | **Implemented** (fields correlated to `Guid`/placement; consumer names blocked) |
| Housing aux | 7 | `ResidenceManager.SendHousingBasics` -> `HousingAuxiliaryPacketEmitter` | **Implemented** (empty/scalar wire shapes; consumer intent blocked) |
| Entity-stat aux | 6 | — | **Blocked** |
| Cluster aux | 35 | — | **Blocked** |
| Spell aux | 9 | — | **Blocked** (client threshold handlers mapped; server threshold runtime not wired) |

Do **not** widen the remaining **50** packets until per-opcode `vtable+0x58` consumers or sniff order is mapped.

Safe always: packet-shape tests, enum/model names, registration notes in `GameMessageOpcode.cs`.

## Next decompile passes (priority)

1. **Inserted handler-node `vtable+0x58` implementers** - Start from the node families that
   `WorldZone_InsertUnitHandlerIntoList` actually splices into `param_1+0x14b0` / `param_1+0x13c0`
   (`plVar4` with back-links at `+0x450` / `+0x4d0`), then recover their vtables and catalog
   the `+0x58` opcode switches. Do not anchor on the direct caller `14036dd50` again. Use
   `FindPointerInData.java`, `DumpNearbyData.java`, and targeted `InspectCodeAddress(es).java`
   passes once a concrete node-family method is found.
2. **Entity-create aux consumer** - Prove whether `0x025F`-`0x0264` are live packets,
   replay-only, or both; correlate with `ServerEntityCreate` (`0x0262`) send order via sniff.
3. **Entity-stat aux cluster** - Map consumers for `0x0889` / `0x08CC` / `0x08F4` / `0x0939`
   / `0x093D` / `0x093E` separately despite shared readers.
4. **Cluster aux bands** - One band per session (chat `0x01B8`..., item/options `0x056B`..., etc.)
   using the same dispatch discovery approach.
5. **Spell aux** - Follow `SPELL_BROADCAST_ROADMAP.md`; map row semantics for
   `ServerSpellUInt32TripletList` before emit.

## Tooling

```powershell
# Registration + reader inspect (single address per TraceFunctionCallers invocation)
.\Decomp\Analysis\run_ghidra_analysis.ps1 -ExportOnly -Targets WildStar64.exe `
  -MaxDecompiledFunctions 500 -ExtraPostScript InspectCodeAddresses.java `
  -ExtraPostScriptArgs @('140095c20')

.\Decomp\Analysis\run_ghidra_analysis.ps1 -ExportOnly -Targets WildStar64.exe `
  -MaxDecompiledFunctions 500 -ExtraPostScript TraceFunctionCallers.java `
  -ExtraPostScriptArgs @('140095c20')

# Full export refresh (avoid combining TraceFunctionCallers with low MaxDecompiled in one run)
.\Decomp\Analysis\run_ghidra_analysis.ps1 -ExportOnly -Targets WildStar64.exe -MaxDecompiledFunctions 2400
```

`FindEntityAuxCallers.java` only finds **direct** xrefs; expect registration + list readers,
not gameplay handlers.

Additional scripts (2026-05-23): `FindCallsToTarget.java`, `FindPointerInData.java`,
`FindDataReferences.java`, `FindVtableSlotReferences.java` (scan `.rdata` for slot pointers;
skip unreadable regions).

## Verification

- `rg Server0x` over `Source/` -> **0** (placeholder elimination)
- `EntityAuxiliaryPacketShapeTests` + `PacketPlaceholderNamingTests` -> wire shapes
- Production emitter grep -> **62** types with tests-only instantiation
