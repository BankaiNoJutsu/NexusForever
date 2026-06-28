# Entity / Cluster Aux Opcode Decode Roadmap

Updated: 2026-06-05 (entity-create main Faction1/Faction2 offsets mapped; aux `Value*` names remain blocked pending consumer/apply evidence)

Supplemental update: 2026-06-09 F-026/F-003 `ServerItemContextActionAck`
(`0x00B7`) cached-export/source recheck:
existing cached `WildStar64.exe` fragments were sufficient and no labels or
exports were changed. `Network_RegisterServerOpcode_0351` (`14006c290`)
registers `0x00B7` size `1` at `selected_decompiled.c:13226` to shared
`ServerEmpty_ReadPayload` (`14007d8e0`), whose cached fragment succeeds without
bit or field reads when a payload pointer is present. Exact selected scans found
`0x00B7` only at that registration, selected xrefs for `14007d8e0` are
label-only, selected call edges show no producer or post-read consumer, and
current source has only the neutral packet model plus packet-placeholder tests.
Positive controls stay separate: `ItemUse_SendClientItemUse` (`140398cc0`)
sends `0x0943`, `Friendship_SendClientFriendshipBlock` (`1405e39e0`) sends
`0x00B8`, and `Friendship_SetAutoResponseMessagesAndSend` (`1405de2e0`) uses
`ClientFriendshipSetAutoResponseMessage_WritePayload` (`14007e4b0`) for
`0x03B7`. Keep `ServerItemContextActionAck` zero-field and non-emitted until a
native server producer/send site, post-read apply owner, callback/table owner,
or accepted item-context/friendship capture proves intent and timing.

Supplemental update: 2026-06-09 F-005 marketplace aux `0x06DF` / `0x07D5`
producer recheck:
Ghidra MCP discovery found no running instances, and cached fragments still
prove only the reader shapes and registration rows. `ServerAuctionPostAux_ReadPayload`
(`140090090`) reads count, counted `uint32` array, counted byte array of the
same count, and trailing `uint32`; `ServerAuctionsByFilterAux_ReadPayload`
(`14008fe80`) reads one 14-bit value, three `uint32` fields, and one flag.
`Network_RegisterServerOpcode_0351` (`14006c290`) binds `0x06DF` at size
`0x20` and `0x07D5` at size `0x14`, but no marketplace apply helper, runtime
producer, or field consumer surfaced in cached exports or current source.

Supplemental update: 2026-06-09 F-003 `Server0x0015` shared-reader recheck:
Ghidra MCP discovery found no running instances, and cached fragments still
prove only the shared 5-bit plus `uint32` reader (`ServerUInt5UInt32_ReadPayload`
`140081f00`) registered for both `0x0015` and matching `0x0628`. The same
reader is also used as a `ServerFortuneRewards` money-reward row helper. The
positive matching apply path remains `MatchingManager_ApplyMatchingAverageWaitTimeUpdated`
(`1405c0e00`); no opcode-specific `0x0015` apply/producer/post-read consumer
surfaced. Keep `Server0x0015.Value0`/`Value1` neutral and non-emitted.

Supplemental update: 2026-06-09 F-016..F-021 spell auxiliary cached recheck:
existing cached `WildStar64.exe` fragments were sufficient and no export refresh
or label change was needed. `Network_RegisterServerOpcode_0351` (`14006c290`)
still binds `0x07FC` to `ServerSpellCastResult_ReadPayload` (`140094fb0`:
`uint32`, 18-bit `Spell4Id`, 9-bit `CastResult`), `0x080F`/`0x0810` to
`ServerSpellUInt32TripletList_ReadPayload` (`140095da0`: `uint32` count plus
triplet rows via `140080bf0`), and `0x0812` to `ServerSpellFourUInt32_ReadPayload`
(`14007fef0`: four `uint32` fields). Selected xrefs are label-only and selected
call edges stay inside bit readers, allocation, and row parsing; current source
has mixed `ServerSpellCastResult` emitters but no native proof for the leading
field name, and the triplet/four-uint aux packets still appear only in models
and packet-shape tests. Keep spell aux producer/apply semantics blocked pending
a native apply/producer path, callback/table owner, dynamic dispatch proof, or
accepted spell packet capture.

Supplemental update: 2026-06-09 F-026 item-data aux `0x056B`/`0x056C`/`0x056D`
cached recheck:
existing cached `WildStar64.exe` fragments were sufficient and no labels or
exports were changed. `Network_RegisterServerOpcode_0351` (`14006c290`) still
registers `0x056B` size `0x20` to `ServerItemModdableData_ReadPayload`
(`1400a3ce0`: item guid, threshold data, random glyph data, random circuit
data), `0x056C` size `0x28` to `ServerItemMicrochips_ReadPayload`
(`1400a3d50`: item guid, maker character id, random circuit data, 18-bit
power-core item id, 3-bit count, counted microchip ids), and `0x056D` size
`0x18` to `ServerItemGlyphs_ReadPayload` (`1400a3e40`: item guid, random glyph
data, 4-bit count, counted glyph ids). Selected xrefs are label-only and
selected call edges stay inside reader helpers. Client-side helpers
`1403b8540`/`14056aa20` and `1403b85a0`/`14056aba0` prove existing-slot
microchip/glyph apply and `ItemModified` dispatch, but no native server
producer/send timing surfaced. Keep all three packets non-emitted until a
server producer path, apply-table classification, callback/table owner, or
accepted item-replication capture proves semantics and timing.

Supplemental update: 2026-06-09 F-026 `ServerSupplySatchelAux` (`0x019A`)
cached recheck:
existing cached `WildStar64.exe` fragments and selected exports were sufficient
and no labels or exports were changed. `Network_RegisterServerOpcode_0351`
(`14006c290`) registers `0x019A` size `8` at `selected_decompiled.c:12829` to
shared `MatchingQueueResultWaitTime_ReadPayload` (`14007fcf0`), whose cached
reader consumes one 6-bit value and one `uint32`. Selected xrefs for
`14007fcf0` are label-only and selected call edges stay reader-local through
`FUN_14006c090`. The same reader is reused by `ServerCharacterDeleteResult`
(`0x00E6`), `ServerMatchingMatchKickCooldownUpdate` (`0x0616`), and
`ServerMatchingMatchOperationResult` (`0x0623`); their managed emitters are
separate positive controls and do not prove supply-satchel semantics. Current
source emits regular `ServerSupplySatchelUpdate` (`0x0199`) for stack updates
and keeps `ServerSupplySatchelAux` model/test-only. Keep `UInt6Value` and
`Value` neutral and non-emitted until a native producer/apply owner,
callback/table owner, dynamic dispatch proof, accepted supply-satchel capture,
or broader item/supply readback evidence proves semantics and timing.

Supplemental update: 2026-06-09 F-026 `ServerCostumeItemAux` (`0x037F`)
cached recheck:
existing cached `WildStar64.exe` fragments were sufficient and no labels or
exports were changed. `Network_RegisterServerOpcode_0351` (`14006c290`)
registers `0x037F` size `0x18` at `selected_decompiled.c:13026` to
`ServerCostumeItemAux_ReadPayload` (`1400874a0`), whose cached reader consumes
one 14-bit value, three `uint32` fields, and two trailing flags. Selected xrefs
for `1400874a0` are label-only and selected call edges stay reader-local
through `FUN_14006c090`. Adjacent `ClientEmote` (`0x037E`) uses separate writer
`140087270`, and costume unlock/forget/save event helpers prove separate result
paths rather than a `0x037F` producer. Current source keeps
`ServerCostumeItemAux` model/test-only. Keep `UInt14Value`, `Value0..Value2`,
and `Flag0`/`Flag1` neutral and non-emitted until a native producer/apply
owner, callback/table owner, dynamic dispatch proof, or accepted costume/emote
capture proves semantics and timing.

Supplemental update: 2026-06-09 F-004 housing-basics follow-up `0x010D` /
`0x0110` cached recheck:
existing cached `WildStar64.exe` fragments were sufficient and no labels or
exports were changed. `Network_RegisterServerOpcode_0351` (`14006c290`)
registers `0x010D` size `1` to shared `ServerEmpty_ReadPayload` (`14007d8e0`)
and `0x0110` size `0x10` to `ServerHousingBasicsFollowup_ReadPayload`
(`14008de70`: `uint32`, 18-bit scalar, `uint32`, 8-bit scalar). Adjacent
`ServerHousingBasics` (`0x010E`) has separate apply evidence through
`1403cd6d0` dispatching `HousingBasicsUpdated` / `HousingPrivacyUpdated`, but
selected xrefs/call edges do not tie that apply owner to `0x010D` or `0x0110`.
Keep the existing residence-session compatibility bundle neutral/defaulted until
a follow-up producer/apply owner, callback/table owner, or accepted
housing-login/return capture proves field semantics and timing.

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

Pass 147 note: `ServerEntityCreate_ReadPayload` (`140096fa0`, opcode `0x0262`)
reads `+0xd4` and `+0xd8` as the main 14-bit `Faction1` and `Faction2`
fields. `FUN_140456960` stores `packet+0xd4` at entity `+0x120`, seeds the
base Faction2 component from `packet+0xd8`, then sets the active Faction2
component from `packet+0xd4`. The current NexusForever aux builder also
populates `0x0263` `Value4`/`Value5` from the same entity faction values, but
the native `ServerEntityCreateAuxBitPackedRow_ReadPayload` still exposes those
as generic 17-bit slots and no native apply handler or sniff/order witness ties
them to faction semantics. Keep `ServerEntityCreateAuxBitPackedRow.Value4` and
`Value5` neutral until that consumer evidence lands.

Pass 148 note: Ghidra MCP tools were exposed again, but both
`mcp__ghidra_mcp.list_instances` and `connect_instance` still failed with
`Transport closed`. Direct Ghidra plugin xrefs plus the current export cache
rechecked the entity-create aux readers and the known native
`WorldSocket+0x15b0` handler-chain families. `0x025F` and `0x0263` row readers
still have only list-composition plus registration/data refs; `0x0260`,
`0x0261`, and `0x0264` still have registration/data refs only. The known
inserted socket nodes are diagnostic/log, console, options/addons, and Fortune;
only Fortune has a nontrivial apply slot, and it handles `0x03CF`-`0x03D2`.
Keep `0x025F`-`0x0264` aux fields neutral until a new socket-chain consumer or
live sniff/order witness is recovered.

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

## Live server-message dispatch (mapped 2026-05-23; native chain corrected 2026-06-02)

| Stage | Address | Label | Role |
| --- | --- | --- | --- |
| Socket ctor | `14000a490` | `WorldSocket_Ctor` | Sets `object+0x100` -> callback table `PTR_LAB_140b55100` @ `140b55100`; initializes native handler-chain head at `object+0x15b0`. |
| Filter phase | `140014d30` | `WorldSocket_FilterServerMessageHandlers` | Receives callback-table subobject (`WorldSocket+0x100`) and walks `*(callbackView+0x14b0)` == native `*(WorldSocket+0x15b0)`; each node `vtable+0x50(handler, conn, messageCategory)`. |
| Deserialize | `DAT_140c65808` | (network service) | `vtable+0x100` builds parsed payload pointer (`local_e0`) from opcode + raw buffer. |
| Fast paths | `140003fb0`, `140452670`, storefront | `AccountInventory_HandleServer0966To0980`, ... | Run before handler chain when enabled (`socket+0x14d0`, globals). |
| Apply phase | `140014f10` | `WorldSocket_ProcessServerMessage` | Walks the same callback-view `+0x14b0` / native `WorldSocket+0x15b0` list; each node `vtable+0x58(handler, conn, opcode, parsedPayload)`. Returns `<0` error, `0` handled, `>0` try next. |
| Spatial false lead | `140356a30` cluster | `WorldZone_*Spatial*` helpers | Uses a different `+0x14b0` owner/list shape: nodes backlink at `+0x450` / `+0x458` and `vtable+0x50` supplies spatial bounds. Not the native `WorldSocket+0x15b0` opcode-handler chain, whose next pointer is `node+0x20`. |

Focused follow-up on the `WorldZone_*` `+0x14b0` cluster rejects that anchor:

- `InspectCodeAddresses` on `140356a30` shows the helper does **not** insert the zone/world object itself. It walks visible-unit node lists rooted at `param_1+0x1488` plus the local spatial-bucket families and splices each `plVar4` node into `param_1+0x14b0` with back-links at `plVar4+0x450`; a sibling family is inserted into `param_1+0x13c0` with back-links at `plVar4+0x4d0`.
- The sole direct caller `FUN_14036dd50` is therefore the owning zone/world update method, not the handler consumer family. `FindPointerInData` on `14036dd50` hit `.rdata` cell `140b65a10`, but the adjacent code cell `14036dd30` is only a tiny getter from `this+0x12b0`; do **not** treat that recovered vtable as the `+0x50` / `+0x58` handler table.
- A `FindImmediateInstructions` scan for `0x14b0` also surfaced sibling zone methods `14035c650`, `140369f30`, `14036a460`, `14036a980`, and `14036b8d0`, each splicing or unlinking the same `+0x450` / `+0x458` node family around `param_1+0x1488` and `param_1+0x14b0`.
- Practical consequence: stop treating that sibling method cluster as a `WorldSocket` handler-node source. The live socket chain is proven at `WorldSocket_FilterServerMessageHandlers` / `WorldSocket_ProcessServerMessage` as callback-view `+0x14b0` / native `WorldSocket+0x15b0`, where nodes advance via `node+0x20` and slot `+0x50` / `+0x58` receives connection/message arguments.

Full-restoration first pass (2026-06-02):

- `FindImmediateInstructions 0x14b0` returned 37 matches. The only live network readers remain
  `WorldSocket_FilterServerMessageHandlers` (`140014d30`) and `WorldSocket_ProcessServerMessage`
  (`140014f10`). The other inspected hits were spatial/world/UI helpers and do not reveal the
  network handler-chain insertion source.
- The inspected `0x14b0` spatial/grid family functions (`14035c650`, `140369f30`, `14036a460`,
  `14036a980`, `14036b8d0`) are enumeration/splice helpers, not per-opcode payload consumers.
- `FUN_140001a50` was rejected as an aux/socket anchor. It is registered from
  `Apollo_RegisterActorFixedWindow` (`1400016d0`) as the `Apollo.ActorFixedWindow`
  constructor, allocates a `0x430`-byte window object, calls base window init
  `FUN_1400c5920`, and installs an interior refcount/destructor vtable
  `PTR_FUN_140b54e10`.
- `FindOpcodeComparisons` over entity-stat aux opcodes `0x0889`, `0x08CC`, `0x08F4`,
  `0x0939`, `0x093D`, and `0x093E` found actual opcode literals only in the registration
  function (`14006c290`). Other apparent hits were GameFormula ids or structure offsets, not
  producer/consumer semantics.
- Result: entity-stat aux remains blocked; no NexusForever runtime emitter should be widened
  from this pass.

WorldSocket native chain correction (2026-06-02):

- `WorldSocket_FilterServerMessageHandlers` and `WorldSocket_ProcessServerMessage` are reached
  through the callback table installed at native `WorldSocket+0x100`. Their decompiled
  `param_1+0x14b0` field is therefore native `WorldSocket+0x15b0`, not native
  `WorldSocket+0x14b0`.
- A fresh `FindImmediateInstructions 0x15b0` pass returned 33 matches. The socket-owned hits
  map ctor initialization, teardown, update/render walkers, and append/toggle helpers around
  native chain head `+0x15b0`, active node `+0x15b8`, and pending/removal state `+0x15c0`.
- Native chain node layout is `node+0x18` backlink pointer-to-link and `node+0x20` next.
  Filter and apply dispatch still use node `vtable+0x50` and `vtable+0x58`.
- Mapped appenders:
  - `140015ec0` allocates a diagnostic message/log node with `PTR_FUN_140b55540` and appends it
    to native `WorldSocket+0x15b0`; it is called from `WorldSocket_LogZoneMessageById` and
    error paths inside `WorldSocket_ProcessServerMessage`.
  - `1400163d0` toggles the persistent console node at native `socket+0x1598`; constructor
    `14002a220` installs `PTR_FUN_140b55430`.
  - `140016560` and `1400166a0` append or trigger the persistent options/addons node at native
    `socket+0x15a0`; constructor `140042370` installs `PTR_FUN_140b558c0`.
  - `socket+0x15a8` is a larger persistent node built by `1404d56b0` with
    `PTR_FUN_140b690f0`; follow-up classified its nontrivial apply slot as the Fortune packet
    consumer family.
- Slot-table result: `PTR_FUN_140b55540`, `PTR_FUN_140b55430`, and `PTR_FUN_140b558c0` have
  filter/apply-style slots that return `1` via `14001d310`, so those nodes do not consume aux
  packets. The installed options pointer `PTR_FUN_140b558c0` sits inside family block
  `140b558b0`; its `vtable+0x50`/`+0x58` slots are `140b55910`/`140b55918`, both
  `WorldSocketChain_ReturnOne`.
- `PTR_FUN_140b690f0` sits inside family block `140b690c0`; its `vtable+0x50`
  slot is `14001d310` and its `vtable+0x58` apply-style slot `1404d60f0` consumes Fortune
  opcodes `0x03CF`-`0x03D2`, not entity-stat aux.
- Result: entity-stat aux remains blocked for runtime emission. The next evidence target is a
  nontrivial `WorldSocket+0x15b0` node `vtable+0x58` consumer or a live sniff/order witness for
  each blocked aux opcode.

Fortune chain classification follow-up (2026-06-02):

- `FortuneNode_ApplyServerFortunePackets` (`1404d60f0`) is the `PTR_FUN_140b690f0`
  apply-style slot. It dispatches `ServerFortuneCardUpdate` (`0x03CF`),
  `ServerFortuneReset` (`0x03D0`), `ServerFortuneCards` (`0x03D1`), and
  `ServerFortuneRewards` (`0x03D2`) into the Fortune UI state object at node `+0xC8`.
- `Fortune_ApplyCardUpdate` (`1407290a0`) returns without applying the operation/card flags
  when the first payload bool is false. NexusForever renamed that field from `Unknown` to
  `HasUpdate`; the wire shape is unchanged.
- Result: the `socket+0x15a8` family is a proven Fortune consumer and should not be revisited
  as an entity-stat aux candidate.

WorldSocket chain catalog refresh (2026-06-03):

- Rechecked `FindImmediateInstructions 0x15b0` and the cached `WorldSocket` decompile fragments
  after the full export pass. Socket-owned hits still resolve to ctor/copy-init, teardown,
  update/render/window walkers, `WorldSocket_AppendDiagnosticMessageNode`, console toggle,
  options/addons append/trigger helpers, and the `socket+0x15a8` Fortune family.
- `140012780` is not a new inserted handler-node source. The function-pointer family table
  places it on the `WorldSocket` object vtable (`PTR_FUN_140b55120` slot `+0x08`), and its
  `0x15b0` hit is part of the object runtime-init/splice path around
  `WorldSocket_InitPersistentRuntimeNodes`, not a separate aux consumer family.
- Other cached `0x15b0` hits such as `140357620`, `1403638c0`, `140393280`, `1404a1370`,
  and `140658610` are same-offset fields on non-socket owners or rendering/world helpers, not
  the native `WorldSocket+0x15b0` packet-handler chain.
- `InspectCodeAddresses 140042370 1404d56b0 1404d60f0` reconfirmed the installed vtable
  pointers and Fortune `+0x58` dispatch. Result: static chain discovery remains exhausted for
  entity-stat aux; runtime emission still needs a new linked-list consumer or live sniff/order
  witness.

Cached export recheck (2026-06-04):

- Rechecked the current `WildStar64.exe` export top-level CSV/C artifacts without touching
  generated caches: `selected_decompiled.c`, `functions.csv`, `selected_reasons_summary.csv`,
  `selected_xrefs.csv`, `selected_call_edges.csv`, `function_pointer_families.csv`, and
  `function_pointer_family_slots.csv`.
- Entity-stat opcode searches for `0x0889`, `0x08CC`, `0x08F4`, `0x0939`, `0x093D`, and
  `0x093E` still resolve to registration/reader evidence, not a nontrivial producer or
  per-opcode apply handler.
- `WorldSocket_ProcessServerMessage` (`140014f10`) still deserializes the payload, runs known
  account/storefront fast paths, then walks the native `WorldSocket+0x15b0` handler list via
  `node+0x20` and `vtable+0x58`; the named persistent node families remain diagnostic,
  console/options, or Fortune.
- Result: no NexusForever production emitter is safe to add for the entity-stat aux cluster.
  The next evidence source remains a new nontrivial `WorldSocket+0x15b0` handler-node family,
  a per-opcode apply-table/data-ref classification, or a live sniff/order witness.

F-025 false-lead cleanup (2026-06-04):

- Rejected stale label candidate `FUN_140939650` as a server-packet consumer. The cached
  fragment at `exports/WildStar64.exe/selected_decompiled.c:200460` has no
  packet/opcode/payload arguments and only recalculates floating-point viewport/grid globals
  (`DAT_140c79a*`, `_DAT_140c79b*`) from display-scale globals. `selected_xrefs.csv` has only
  the self label xref for `140939650`, and the selected call-edge rows are intra-function
  jumps created by the decompiler, not callers.
- Result: do not use `140939650` as a `WorldSocket+0x15b0` or aux apply-candidate anchor.
  Entity-stat aux emitters and map-tracked producers remain blocked on the same evidence gate:
  a nontrivial socket handler-node family, a per-opcode apply table/data-ref classification,
  or an accepted live sniff/order witness.

Entity-stat registration-anchor pass (2026-06-04):

- `FindImmediateInstructions` rechecked the unlabeled entity-stat opcodes against the current
  `WildStar64.exe` Ghidra project. The real registration literals in
  `Network_RegisterServerOpcode_0351` (`14006c290`) are:
  - `0x093D` at `140074cda`, registering size `0x10` with
    `ServerEntityStatUInt32UInt5Pair_ReadPayload` (`140097690`).
  - `0x0939` at `140074fb9`, registering size `0x18` with
    `ServerEntityStatUInt32UInt14UInt18WideString_ReadPayload` (`140097ee0`).
  - `0x093E` at `14007501b`, registering size `0x10` with
    `ServerEntityStatTwoUInt32UInt64_ReadPayload` (`140097f70`).
- Non-registration immediate hits for those values are GameFormula lookups or object offsets,
  not packet producers or apply consumers. `ghidra-mcp` was unavailable for this pass, so the
  evidence source is the repeatable headless helper output in
  `WildStar64.NexusForeverClient64_WildStar64.FindImmediateInstructions.ghidra.log`.
- Tooling correction: `ApplyNexusForeverLabels.java` now applies labels inside an existing
  function as local labels/comments instead of renaming or splitting the containing function.
  The follow-up force + export-only refresh leaves `14006c290` named
  `Network_RegisterServerOpcode_0351` while preserving call-site anchors.
- Result: the six entity-stat aux opcodes now have durable reader and registration anchors;
  runtime emission remains blocked on a per-opcode `vtable+0x58` apply handler, apply-table
  classification, or live sniff/order witness.

Entity-stat production-emitter source audit (2026-06-04):

- `rg` over `Source` found `ServerEntityStatUInt32Triplet`, `ServerEntityStatUInt32WideString`,
  `ServerEntityStatUInt32UInt5UInt32`, `ServerEntityStatUInt32UInt14UInt18WideString`,
  `ServerEntityStatUInt32UInt5Pair`, and `ServerEntityStatTwoUInt32UInt64` only in opcode/model
  definitions and focused tests. Runtime production sends are limited to regular
  `ServerEntityStatUpdateFloat` / `ServerEntityStatUpdateInteger` in `WorldEntity`.
- Result: the current NexusForever source tree has no production emitter for the six entity-stat
  aux opcodes. The old matrix wording that all 62 shape-mapped aux/spell packets had no
  emitters was a tracker ghost gap; entity-create and selected housing paths account for the
  proven 12/62 controlled emitters, while this F-025 entity-stat cluster remains in the 50
  blocked emitters.
- Verification: `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj
  --no-restore --filter "FullyQualifiedName~EntityAuxiliaryPacketShapeTests|FullyQualifiedName~EntityCreateAuxiliaryEmissionTests"
  -v minimal --nologo -m:1 -p:UseSharedCompilation=false
  -p:OutDir=I:\GIT\NexusForever\artifacts\testbin\f025-entity-aux-audit\`
  passed `19/19`.

Entity-stat placeholder-name guard (2026-06-04 pass 117):

- Re-ran the current source audit for the six entity-stat aux model names. The
  only hits remain opcode/model definitions and focused tests; production stat
  sends still use regular `ServerEntityStatUpdateFloat` / `ServerEntityStatUpdateInteger`.
- `PacketPlaceholderNamingTests.ServerEntityStatAuxFieldsRemainNeutralUntilApplyHandlersAreProven`
  now pins the neutral `Value*` fields and the shared `Value` / `Text` payload names
  for `0x0889`, `0x08CC`, `0x08F4`, `0x0939`, `0x093D`, and `0x093E`.
- Result: no semantic rename or runtime emitter is safe yet. The blocker remains a
  per-opcode `WorldSocket+0x15b0` `vtable+0x58` apply handler, apply-table
  classification, or live sniff/order witness.
- Verification: `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj
  --no-restore --filter "FullyQualifiedName~PacketPlaceholderNamingTests|FullyQualifiedName~EntityAuxiliaryPacketShapeTests|FullyQualifiedName~EntityCreateAuxiliaryEmissionTests"
  -v minimal --nologo -m:1 -p:UseSharedCompilation=false
  -p:OutDir=I:\GIT\NexusForever\artifacts\testbin\f025-entity-stat-placeholder-guard\`
  passed `91/91`.

Entity-stat aux cached-export recheck (2026-06-09):

- Ghidra MCP discovery returned no running instances, so this pass did not add
  labels or refresh exports. It rechecked current tracked labels plus cached
  `WildStar64.exe` fragments under
  `selected_decompiled_cache/functions/sha256_231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5`.
- Cached `ServerSpellUInt32TripletListRow_ReadPayload` (`140080bf0`) still maps
  `0x0889` as three `uint32` fields, shared with other triplet-row packets.
  Cached `ServerUInt32WideString_ReadPayload` (`1400980f0`) still maps `0x08CC`
  as `uint32` plus wide string, shared with other UI/result packets.
- Cached `ServerEntityStatUInt32UInt5UInt32_ReadPayload` (`140097620`),
  `ServerEntityStatUInt32UInt14UInt18WideString_ReadPayload` (`140097ee0`),
  `ServerEntityStatUInt32UInt5Pair_ReadPayload` (`140097690`), and
  `ServerEntityStatTwoUInt32UInt64_ReadPayload` (`140097f70`) still prove only
  the `0x08F4`, `0x0939`, `0x093D`, and `0x093E` reader shapes.
- Source search still finds the six `ServerEntityStat*` aux packets only in
  packet models, packet-shape tests, placeholder naming guards, and negative
  entity-create emission guards. Runtime stat sends remain the ordinary
  `ServerEntityStatUpdateFloat` / `ServerEntityStatUpdateInteger` paths.
- Result: disposition remains **Mapped only / Blocked producer**. The next
  evidence source is unchanged: a per-opcode `WorldSocket+0x15b0`
  `vtable+0x58` apply handler, apply-table classification, or live sniff/order
  witness proving field semantics and emit timing.

Entity-stat aux blocked recheck (2026-06-17):

- `mcp__ghidra_mcp.list_instances` returned no running Ghidra instance, so this
  pass used cached `WildStar64.exe` fragments and current source/tests only.
- Cached fragments still prove only reader shapes:
  `ServerSpellUInt32TripletListRow_ReadPayload` (`140080bf0`) for `0x0889`,
  `ServerUInt32WideString_ReadPayload` (`1400980f0`) for `0x08CC`,
  `ServerEntityStatUInt32UInt5UInt32_ReadPayload` (`140097620`) for `0x08F4`,
  `ServerEntityStatUInt32UInt14UInt18WideString_ReadPayload` (`140097ee0`) for
  `0x0939`, `ServerEntityStatUInt32UInt5Pair_ReadPayload` (`140097690`) for
  `0x093D`, and `ServerEntityStatTwoUInt32UInt64_ReadPayload` (`140097f70`) for
  `0x093E`.
- Stale `FUN_140939650` remains rejected as an aux consumer/apply candidate:
  the cached fragment recalculates viewport/grid globals and has no packet or
  opcode-specific apply arguments.
- Source search still finds the six `ServerEntityStat*` aux packets only in
  packet models, packet-shape tests, placeholder naming guards, and negative
  entity-create emission guards. Runtime stat sends remain the ordinary
  `ServerEntityStatUpdateFloat` / `ServerEntityStatUpdateInteger` paths.
- Worksheet:
  `artifacts/blocker_evidence/20260617-224723-F025-entity-stat-aux-recheck`.
  Required unblocker: per-opcode `WorldSocket+0x15b0` `vtable+0x58` apply
  handler, apply-table classification, or accepted live combat/death/vehicle/
  emote/item-use/CSI sniff-order witness proving field semantics and emit timing.

Map-tracked unit producer recheck (2026-06-04):

- Direct Ghidra MCP decompile of `ServerMapTrackedUnitUpdate_ReadPayload` (`1400a6c10`),
  `MapTrackedUnitUpdate_ApplyAndDispatch` (`1403f4170`),
  `MapTrackedUnitDisable_ApplyAndDispatch` (`1403f4200`),
  `ClientEvent_MapTrackedUnitUpdate_Dispatch` (`140430f80`),
  `Lua_GameLib_GetMapTrackedUnitData` (`140511c80`),
  `Lua_PublicEvent_GetTrackedUnits` (`14068adb0`), and
  `Lua_PublicEventObjective_GetTrackedUnits` (`140690500`) reconfirmed the current
  client-side chain only.
- The update reader stores tracked-unit id, three position components, and payload
  `+0x10` into the client tracked-unit cache before dispatching `MapTrackedUnitUpdate`;
  disable removes cached tracked-unit state and dispatches `MapTrackedUnitDisable`.
- `GameLib.GetMapTrackedUnitData` accepts a `TrackingSlot` row id and returns label/icon
  UI data. `Game.PublicEvent.GetTrackedUnits` and `Game.PublicEventObjective.GetTrackedUnits`
  enumerate client-maintained tracked-unit collections; they do not expose server allocation
  or send timing.
- Read-only DataMapping evidence from `client_source_trackingslot_map.csv` shows duplicate
  `PublicEventObjectiveId` groups (`5010` has 20 rows, `5138` has 16 rows), so objective-only
  slot selection is explicitly rejected.
- Result: no NexusForever production emitter is safe to add for `0x0849`/`0x0848`.
  A focused source/test pass added a null-table guard to `TrackingSlotHelper`
  and tests for the one-way 15-bit `TrackingSlotId` lookup, but did not add a
  producer or objective-to-slot selector.
  The next evidence source remains a native server send site, a live public-event marker
  capture, or another source that proves tracked-unit id allocation, update cadence, disable
  lifetime, and slot selection.

Map-tracked unit selection guard (2026-06-04 pass 118):

- Current source still has no production emitter for `ServerMapTrackedUnitUpdate`
  (`0x0849`) or `ServerMapTrackedUnitDisable` (`0x0848`) outside packet-shape
  tests and the pre-create negative guard.
- `PacketPlaceholderNamingTests.ServerMapTrackedUnitUpdate_UsesTrackingSlotIdUntilProducerSelectionIsProven`
  now pins the packet field as `TrackingSlotId` and rejects a direct
  `PublicEventObjectiveId` property.
- `TrackingSlotHelperTests.TrackingSlotHelper_DoesNotSelectSlotFromObjectiveOnlyWhenRowsShareObjective`
  uses duplicate `TrackingSlot` rows for `PublicEventObjectiveId = 5010` and
  guards that `TrackingSlotHelper` remains slot-to-objective lookup only, without
  exposing an objective-only `TrackingSlotIdForPublicEventObjective` selector.
- Result: objective-only slot selection remains rejected, and no runtime producer
  is safe. The blocker is unchanged: native send site or accepted public-event
  marker capture proving tracked-unit id allocation, update cadence, disable
  lifetime, and `TrackingSlotId` selection.
- Verification: `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj
  --no-restore --filter "FullyQualifiedName~TrackingSlotHelperTests|FullyQualifiedName~EntityAuxiliaryPacketShapeTests|FullyQualifiedName~EntityCreateAuxiliaryEmissionTests|FullyQualifiedName~PacketPlaceholderNamingTests"
  -v minimal --nologo -m:1 -p:UseSharedCompilation=false
  -p:OutDir=I:\GIT\NexusForever\artifacts\testbin\f025-map-tracked-selection-guard\`
  passed `97/97`.

Housing neighborhood cached-export recheck (2026-06-09):

- Ghidra MCP discovery returned no running instances, so this pass did not add
  labels or refresh exports. It rechecked current tracked labels plus cached
  `WildStar64.exe` fragments under
  `selected_decompiled_cache/functions/sha256_231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5`.
- Cached `ServerHousingNeighborhoodEntry_ReadPayload` (`14009cbe0`) and
  `ServerHousingNeighborhoodList_ReadPayload` (`14009ebf0`) still map only the
  `0x0501` row reader and `0x0506` realm-plus-counted-row list reader.
- Cached `Housing_HandleNeighborhoodList` (`1404ba4f0`) clears/rebuilds the
  client housing neighborhood cache and dispatches `HousingNeighborhoodRecieved`;
  cached `ClientDB_RegisterHousingNeighborhoodInfo` (`140205900`) proves only
  client table-loader presence.
- Source search still finds `ServerHousingNeighborhoodEntry` and
  `ServerHousingNeighborhoodList` only in packet models, packet-shape tests,
  placeholder naming guards, and residence-session negative emission guards.
- Result: disposition remains **Mapped only / Blocked producer**. The next
  evidence source is a native server-push path or accepted live housing
  UI/realm-login capture proving trigger timing, row backing, row-tail field
  population, and realm/session scope for `0x0501` / `0x0506`.

Housing community donate cached-export/source recheck (2026-06-09):

- Cached `WildStar64.exe` fragments were sufficient and no labels or exports
  were changed. `Network_RegisterServerOpcode_0351` (`14006c290`) registers
  `0x04FE` size `0x18` at `selected_decompiled.c:13681` to
  `ServerHousingCommunityDonateUpdate_ReadPayload` (`14009e930`).
- The cached reader consumes a `uint32` count, allocates two count-sized
  buffers, and raw-copies two parallel `uint32` arrays. Selected xrefs for
  `14009e930` are label-only, and selected call edges stay inside bit-read,
  allocation, and raw-copy helpers.
- The separate client donate positive control is `0x04F5`:
  `ClientHousingCommunityDonate_WritePayload` (`14009da00`) writes count plus
  counted `DecorInfo` rows, and `Housing_SendClientCommunityDonate`
  (`1404b9ca0`) sends one selected decor row only after resolving
  `HousingDecorInfo` and rejecting rows with `Flags & 0x8`.
- Current source emits `ServerHousingCommunityDonateUpdate` from
  `ClientHousingCommunityDonateHandler` after copying donated crate decor to the
  community residence and deleting the source decor, but the model keeps
  `Entry.Value0`/`Value1` neutral.
- Result: disposition remains **Mapped only / Field-semantics blocked**. The
  next evidence source is a native apply/consumer owner, callback/table owner,
  accepted donate capture, or backing-store evidence proving the two array
  meanings, resource/contribution costs, exact transfer semantics, and broader
  ownership/unlock policy.

Housing community plot-reservation cached-export/source recheck (2026-06-09):

- Cached `WildStar64.exe` fragments were sufficient and no labels or exports
  were changed. `Network_RegisterServerOpcode_0351` (`14006c290`) registers
  `0x051F` size `0x18` at `selected_decompiled.c:13683` to
  `ServerHousingCommunityPlotReservation_ReadPayload` (`140086e70`).
- The cached reader consumes target residence identity (14-bit realm plus
  64-bit residence id) and one `uint32` plot index. Selected xrefs for
  `140086e70` are label-only; selected call edges are reader-local
  `FUN_14006c090` / `NetworkBitReader_ReadUInt64` edges plus
  `ServerLootWinner_ReadPayload` row-helper reuse.
- `Lua_HousingLib_GetReservedCommunityPlotIndex` (`140737470`) searches the
  type-7 community cache through `Housing_FindCommunityResidenceEntryByIdentity`
  (`14057ff90`) and returns `nPlotIndex` / `bHasReservation` only when the
  cached value is not `0xffffffff`.
- Client positive controls stay on guild operations: `Housing_SendClientCommunityPlotReservation`
  (`14057fe90`) sends `ClientGuildOperation` (`0x04B1`) operation `0x29` with
  the requested plot index, and `Housing_SendClientCommunityPlotReservationRemoval`
  (`14057ff10`) sends operation `0x2A` with the target name.
- Current source emits `ServerHousingCommunityPlotReservation` from
  `CommunityOperations.SendCommunityPlotReservation`, using
  `unchecked((uint)plotIndex)` so `-1` becomes the native `uint.MaxValue`
  sentinel pinned by packet-shape tests.
- Result: disposition remains **Mapped only / Field-level source alignment**.
  Do not widen plot-reservation permission, placement, teleport/unload,
  persistence, or broadcast semantics until a native apply/consumer owner,
  callback/table owner, accepted community-reservation capture, or backing-store
  evidence proves timing and scope.

Map-tracked unit cached-export recheck (2026-06-09):

- Ghidra MCP discovery returned no running instances, so this pass did not add
  labels or refresh exports. It rechecked the current tracked labels plus cached
  `WildStar64.exe` fragments under
  `selected_decompiled_cache/functions/sha256_231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5`.
- Cached `ServerMapTrackedUnitUpdate_ReadPayload` (`1400a6c10`) still maps
  `0x0849` as tracked-unit id, three 32-bit position components, and a 15-bit
  `TrackingSlotId` at payload `+0x10`.
- Cached `MapTrackedUnitUpdate_ApplyAndDispatch` (`1403f4170`),
  `MapTrackedUnitDisable_ApplyAndDispatch` (`1403f4200`),
  `ClientEvent_MapTrackedUnitUpdate_Dispatch` (`140430f80`),
  `Lua_GameLib_GetMapTrackedUnitData` (`140511c80`),
  `Lua_PublicEvent_GetTrackedUnits` (`14068adb0`), and
  `Lua_PublicEventObjective_GetTrackedUnits` (`140690500`) still show only
  client cache mutation, Lua event dispatch, and Lua enumeration of cached
  tracked-unit state.
- Source search still finds `ServerMapTrackedUnitUpdate` and
  `ServerMapTrackedUnitDisable` only in packet models, packet-shape tests, and
  the negative entity-create emission guard. No runtime producer was added.
- Result: disposition remains **Mapped only / Blocked producer**. The next
  evidence source is unchanged: native server send-site evidence or an accepted
  live public-event marker capture proving tracked-unit id allocation, update
  cadence, disable lifetime, and `TrackingSlotId` selection for `0x0849` /
  `0x0848`.

Map-tracked unit blocked recheck (2026-06-17):

- `mcp__ghidra_mcp.list_instances` returned no running Ghidra instance, so this
  pass used cached `WildStar64.exe` fragments and current source/tests only.
- Registration evidence is unchanged: `selected_decompiled.c` registers
  `0x0849` size `0x14` to `ServerMapTrackedUnitUpdate_ReadPayload`
  (`1400a6c10`) and `0x0848` size `4` to the shared `ServerUInt32_ReadPayload`.
- Cached fragments still prove only client consumption:
  `MapTrackedUnitUpdate_ApplyAndDispatch` (`1403f4170`),
  `MapTrackedUnitDisable_ApplyAndDispatch` (`1403f4200`),
  `ClientEvent_MapTrackedUnitUpdate_Dispatch` (`140430f80`),
  `Lua_GameLib_GetMapTrackedUnitData` (`140511c80`), and
  `ClientDB_RegisterTrackingSlot` (`1402426a0`) cover cache mutation, Lua
  dispatch, and `TrackingSlot.tbl` lookup, not server producer timing.
- Source search still finds no production emitter outside packet models,
  packet-shape tests, and `EntityCreateAuxiliaryEmissionTests` negative guards.
  `TrackingSlotHelper` remains a one-way 15-bit slot lookup; objective-only
  `TrackingSlotId` selection stays rejected because duplicate
  `TrackingSlot.PublicEventObjectiveId` groups exist.
- Worksheet:
  `artifacts/blocker_evidence/20260617-224146-F025-map-tracked-unit-producer-recheck`.
  Required unblocker: native server send-site evidence or accepted live
  public-event marker capture proving tracked-unit id allocation, update
  cadence, disable lifetime, and `TrackingSlotId` selection for `0x0849` /
  `0x0848`.

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
`EntityCreateAuxiliaryEmissionTests.BuildPreCreatePackets_DoesNotEmitBlockedEntityStatOrMapTrackedPackets`
now guards the current pre-create boundary so `BuildEntityCreateAuxPackets()` cannot
silently grow entity-stat aux or map-tracked-unit emits without evidence-backed
test changes.

Safe always: packet-shape tests, enum/model names, registration notes in `GameMessageOpcode.cs`.

## Next decompile passes (priority)

1. **WorldSocket native chain vtable catalog** - Continue from the corrected callback-view
   `+0x14b0` / native `WorldSocket+0x15b0` chain. Catalog inserted node families and focus on
   nontrivial slot `+0x50` filters and slot `+0x58` apply implementers for aux opcodes. Do not
   anchor on the `WorldZone_*` spatial list (`node+0x450` backlinks) or `FUN_140001a50` again.
   Search for dynamically allocated nodes not covered by the diagnostic/console/options/Fortune
   families; pass 148 reconfirmed those known families do not consume entity-create aux. Use live
   sniff/order witnesses when static chain discovery stays exhausted.
2. **Entity-create aux consumer** - Prove whether `0x025F`-`0x0264` are live packets,
   replay-only, or both; correlate with `ServerEntityCreate` (`0x0262`) send order via sniff.
3. **Entity-stat aux cluster** - Map consumers for `0x0889` / `0x08CC` / `0x08F4` / `0x0939`
   / `0x093D` / `0x093E` separately despite shared readers.
4. **Map-tracked unit producer** - Find native send-site or live public-event marker capture
   for `0x0849`/`0x0848`; do not synthesize from `TrackingSlot.PublicEventObjectiveId` because
   duplicate objective groups disprove objective-only slot selection.
5. **Cluster aux bands** - One band per session (chat `0x01B8`..., item/options `0x056B`..., etc.)
   using the same dispatch discovery approach.
6. **Spell aux** - Follow `SPELL_BROADCAST_ROADMAP.md`; pass 150 confirmed
   `ServerSpellUInt32TripletList` (`0x080F`/`0x0810`) and
   `ServerSpellFourUInt32` (`0x0812`) are still registration/data-only in the
   direct xref check. Map an apply owner or live correlated row semantics
   before emit.

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
- `EntityCreateAuxiliaryEmissionTests` -> entity-create pre-create boundary, including no
  entity-stat aux / map-tracked-unit emission without producer proof
- Production emitter grep -> **62** types with tests-only instantiation
