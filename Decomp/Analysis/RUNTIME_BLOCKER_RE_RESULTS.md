# Runtime blocker RE pass results

Updated: 2026-06-03 (F-004/F-025 boundary guards recorded)

Companion to [RUNTIME_BLOCKER_RE_PLAN.md](RUNTIME_BLOCKER_RE_PLAN.md). Policy: **no** diagnostic emitters or `WorldConfig` flags; NF production paths only after **Verified**.

Ghidra logs: `Decomp/Analysis/logs/WildStar64.NexusForeverClient64_WildStar64.*.ghidra.log`, `Decomp/Analysis/logs/Houston64.NexusForeverClient64_Houston64.*.ghidra.log`

## Summary

| Pass | ID | Ladder after pass | NF emit |
| --- | --- | --- | --- |
| 1 | F-004 `0x0506` | **Mapped** (consumer + wire); trigger **Blocked** | No |
| 2 | F-025 map-tracked | **Mapped** (apply + Lua); producer **Blocked** | No |
| 3 | F-025 entity-stat aux | **Mapped** (readers); apply **Blocked** | No |
| 4 | F-008 crafting aux | **Mapped** (readers + finish handler); enqueue **Blocked** | No |
| 5 | F-031 Fortune | **Mapped** (wire + UI); retail weights **Blocked** | No (tier emulator) |

Focused tests: `HousingPacketShapeTests` + `EntityAuxiliaryPacketShapeTests` + `FortunePacketShapeTests` - **42/42** passed.

---

## Pass 1 - F-004 `ServerHousingNeighborhoodList` (`0x0506`)

### Ghidra

| Script | Result |
| --- | --- |
| `TraceFunctionCallers` @ `1404ba4f0` | 3 **DATA** refs (`140e0ec28`, `140bd97b0`, `140bd97c0`); no instruction callers -> indirect apply-table dispatch |
| `FindPointerInData` @ `1404ba4f0` | 0 matches (handler reached via table slots, not as a bare 64-bit data pointer) |
| `InspectCodeAddresses` @ `1404ba4f0`, `14009ebf0` | Consumer copies `0x30` rows into housing cache @ `DAT_140c659f0+0x110`, then `ClientEvent_DispatchNamedEvent(..., "HousingNeighborhoodRecieved", ...)` |
| `TraceStringReferences` `HousingNeighborhoodRecieved` | 0 refs at guessed string VA `1409d2ee0`; event name is an **inline literal** in the dispatch call (decomp shows `1409d0f86`) |
| `ParseRegistrationTable` | Script run incomplete for full table (only spurious `0x4009F4F0` entry); use existing `function_labels.csv` registration notes |

### Opcode / enum correlation

- `GameMessageOpcode`: **no** `ClientHousing*` in `0x0502`-`0x0509` (gap between `ClientHousingCommunityDonate` `0x04F5` and `ClientHousingRemodel` `0x050A`).
- `ClientHousingRandomCommunityList` (`0x052C`) -> `ServerHousingRandomCommunityList` (`0x0526`) is a **different** flow (random community picker, not neighborhood list).
- **Registration (Verified wire hookup):** `FindImmediateInstructions 0x506` -> `Network_RegisterServerOpcode_0351` @ `140077730`, reader `ServerHousingNeighborhoodList_ReadPayload` @ `14009ebf0`, object size `0x10` (label `Network_RegisterServerOpcode_0506`).
- **False-positive `0x506` immediates on client:** `FUN_14041deb0` uses `GameFormula_GetEntryById` (`140200220`) with formula id `0x506` (1286), not the housing packet. `FUN_14055c2c0` / `ServerReader_0xe5c80` pass `0x506` into a UI vtable slot (`+0x248`), not `Network_SendMessageById`.
- **`Housing_BuildNeighborLuaTable`:** direct `CALL` only from `FUN_1406f9fe0` and `FUN_140735cc0` (neighbor list / Lua paths). **Not** referenced from `Housing_HandleNeighborhoodList` - neighborhood list UI is `HousingNeighborhoodRecieved`, separate from neighbor-row builder.
- **Houston64:** `FindOpcodeComparisons` / `FindImmediateInstructions` for `0x506` hit only MFC `CWnd::GetDlgItem(0x506)` dialog-control IDs (`FUN_1402e8f60`, `FUN_1402e9190`), not world opcodes. No Houston enqueue path for `ServerHousingNeighborhoodList` found in this scan.
- **Correlated / not Verified:** `0x0506` remains server-pushed; retail trigger is session/realm rules or an unmapped request outside the `0x0502`-`0x0509` client opcode gap.

### NF

- Packet models and shape tests remain **Mapped**.
- **No** `ServerHousingNeighborhoodList` production emitter (blocked).
- `HousingAuxiliaryPacketEmitterTests.BuildResidenceSessionPackets_DoesNotEmitBlockedNeighborhoodListPackets`
  guards the current residence-session aux boundary so `0x0501`/`0x0506` are
  not silently promoted without producer-trigger evidence.
- **Next:** live sniff (housing UI open / realm login) for `0x0506` ordering; map row `NeighborhoodId` + tail fields to `HousingNeighborhoodInfo.tbl` + persisted realm data.

---

## Pass 2 - F-025 map-tracked (`0x0849` / `0x0848`)

### Ghidra

| Script | Result |
| --- | --- |
| `TraceFunctionCallers` @ `1403f4170` | 1 **DATA** ref `140e037ec` -> apply-table slot for `MapTrackedUnitUpdate_ApplyAndDispatch` |
| `InspectCodeAddresses` @ `1403f4170` | Caches id + XYZ + `TrackingSlotId` (`param_2[4]`); dispatches `MapTrackedUnitUpdate` via `ClientEvent_MapTrackedUnitUpdate_Dispatch` |

### NF

- `TrackingSlotId` on wire models; `TrackingSlotHelper` for `TrackingSlot.tbl` -> `PublicEventObjectiveId` (**table lookup only**).
- **Blocked:** server `TrackedUnitId` allocation, update cadence, disable timing, objective->slot selection.

---

## Pass 3 - F-025 entity-stat aux (`0x0889`, `0x08CC`, `0x08F4`, `0x0939`, `0x093D`, `0x093E`)

### Evidence (existing + pass)

- Readers in `function_labels.csv`; shared payloads (`ServerUInt32WideString_ReadPayload` for `0x08CC`, etc.).
- `TraceFunctionCallers` / direct CALL: **none** for apply paths; only registration + DATA apply-table pattern (same as other aux opcodes in `INITIAL_FINDINGS` F-025 pass).
- `0x0889`: **Mapped** registration - `Network_RegisterServerOpcode_0889` @ `140078cb0`, 12-byte reader `ServerSpellUInt32TripletListRow_ReadPayload` @ `140080bf0` (same reader as `0x0908`/`0x090A`). Per-opcode apply semantics still **Blocked**.

### NF

- Per-opcode XML on models; fields stay `ValueN` until apply-site proof.
- **No** production emit.

---

## Pass 4 - F-008 crafting aux (`0x084B`, `0x0855`)

### Ghidra

- `Network_RegisterServerOpcode_084B` / `_0855` -> readers `1400a3af0` (24 B) and `140081df0` (12 B).
- `InspectCodeAddresses` @ `1405e6690` (`Crafting_HandleServerCraftingFinish`): handles `0x0853` finish path and hot/cold discovery event only - **no** `0x084B`/`0x0855` enqueue in this consumer.

### NF

- Fields remain `ValueN` / `FloatValue*`; craft handlers log blocked station service keys at Debug (`0x2C`/`0x4F`/`0x57`).
- **No** aux emit.
- `CraftingSimpleCraftHandlerTests.ComplexCraft_WithCraftStatsAndChargeCounts_DoesNotEmitBlockedCurrentCraftOrAuxPackets`
  guards the fixed-recipe complex-craft path so `0x0854`, `0x084B`, and
  `0x0855` stay modeled-only until current-craft cadence or aux enqueue evidence
  is verified.

---

## Pass 5 - F-031 Fortune weights

- `ServerFortuneRewards_ReadPayload` @ `140081f60` (`0x03D2`); `FortunesLib_GetFortunesLootList` uses parallel float probabilities x 100 for UI.
- [FORTUNE_WEIGHT_AUDIT.md](FORTUNE_WEIGHT_AUDIT.md): retail per-item weights **Blocked**; NF uses rarity tiers + optional debug catalog probability samples.

---

## Labels added (this pass)

| Binary | Address | Name |
| --- | --- | --- |
| WildStar64.exe | `140bd97b0` | `HousingNeighborhoodList_ApplyDispatchSlot_DataRef` |
| WildStar64.exe | `140bd97c0` | `HousingNeighborhoodList_ApplyDispatchSlot_DataRef_Alt` |
| WildStar64.exe | `140e0ec28` | `HousingNeighborhoodList_ApplyDispatchSlot_DataRef_Table` |
| WildStar64.exe | `140e037ec` | `MapTrackedUnitUpdate_ApplyDispatchSlot_DataRef` |

---

## Out of scope (confirmed)

- `IMapTrackedUnitEmitter`, `IHousingNeighborhoodListEmitter`, `WorldConfig.Enable*DiagnosticEmit` - removed; not reintroduced.
