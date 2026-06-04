# Runtime blocker RE pass results

Updated: 2026-06-04 (F-004 housing, F-008 crafting, and F-031 Fortune Ghidra MCP rechecks recorded)

Companion to [RUNTIME_BLOCKER_RE_PLAN.md](RUNTIME_BLOCKER_RE_PLAN.md). Policy: **no** diagnostic emitters or `WorldConfig` flags; NF production paths only after **Verified**.

Ghidra logs: `Decomp/Analysis/logs/WildStar64.NexusForeverClient64_WildStar64.*.ghidra.log`, `Decomp/Analysis/logs/Houston64.NexusForeverClient64_Houston64.*.ghidra.log`

## Summary

| Pass | ID | Ladder after pass | NF emit |
| --- | --- | --- | --- |
| 1 | F-004 `0x0506` | **Mapped** (consumer + wire); trigger **Blocked** | No |
| 2 | F-025 map-tracked | **Mapped** (apply + Lua); producer **Blocked** | No |
| 3 | F-025 entity-stat aux | **Mapped** (readers); apply **Blocked** | No |
| 4 | F-008 crafting aux | **Mapped** (readers + finish/current-craft consumers); enqueue **Blocked**; durable rune bridge **Implemented** | Rune bridge yes; aux no |
| 5 | F-031 Fortune | **Mapped** (wire + UI); reset code **Implemented**; retail weights/rotation **Blocked** | Reset code yes; tier emulator only for weights |

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
- Pass 119 adds packet placeholder coverage so the `0x0501` row tail remains
  `NeighborhoodWireUInt64_AfterRealmIds` / `NeighborhoodWireUInt32_0..2` and is
  not renamed from `HousingNeighborhoodInfo.tbl` columns (`BaseCost`,
  `MaxPopulation`, `HousingMapInfoIdPrimary`) without producer/backing proof.
  Focused housing / placeholder verification passed `106/106`.
- 2026-06-04 runtime cleanup removed the unrelated hardcoded
  `ServerHousingProperties.Residence.NeighbourhoodId` placeholder
  (`0x190000000000000A`); `Residence.Build()` now emits `GuildOwnerId` when
  present and `0` otherwise. Focused housing coverage passed 32/32.
- **Next:** live sniff (housing UI open / realm login) for `0x0506` ordering; map row `NeighborhoodId` + tail fields to `HousingNeighborhoodInfo.tbl` + persisted realm data.

### 2026-06-04 Ghidra MCP recheck

- Direct decompile reconfirmed `ServerHousingNeighborhoodEntry_ReadPayload`
  (`14009cbe0`), `ServerHousingNeighborhoodList_ReadPayload` (`14009ebf0`), and
  `Housing_HandleNeighborhoodList` (`1404ba4f0`). The consumer clears the
  client cache at `DAT_140c659f0+0x110`, copies `0x30` rows, duplicates the row
  name string, and dispatches `HousingNeighborhoodRecieved`.
- Adjacent decompile of `Housing_SendClientVisitResidenceIdentity` (`1405b2390`),
  `Lua_HousingLib_RequestCommunityPlacement` (`140736950`),
  `Housing_SendClientCommunityPlacement` (`1404b6b90`),
  `Lua_HousingLib_ReserveCommunityPlot` (`140737350`), and
  `Housing_SendClientCommunityPlotReservation` (`14057fe90`) confirmed those
  flows send `0x052F`, `0x052B`, or guild-operation `0x04B1`; they do not
  request or explain `0x0506`.
- String/xref recheck found the live `HousingNeighborhoodRecieved` xref only in
  `Housing_HandleNeighborhoodList` at `1404ba634`. `HousingNeighborhoodInfo.tbl`
  has client-table metadata (`base_cost`, population, faction/playstyle/map ids),
  but no current evidence maps those table fields to the packet row tail.
- Export/cache refresh labelled WildStar64 `ClientDB_RegisterHousingNeighborhoodInfo`
  (`140205900`) from the `DB\HousingNeighborhoodInfo.tbl` loader path and verified
  it in `functions.csv`, `selected_decompiled.c`, and `function_label_inventory.csv`.
  This upgrades the WildStar64 table-loader evidence to **Mapped**, but does not
  prove `0x0506` producer timing or row backing fields.
- Observed-only strings for `RequestJoinNeighborhood()` and
  `RequestLeaveNeighborhood()` have no usable xref in the current export, so
  they are not a trigger proof.
- Result remains **Mapped / Blocked**. Implementation still needs a live sniff or
  native server-push path proving send timing plus a backing-store rule for
  `NeighborhoodId`, both 14-bit realm fields, the second uint64, name, and the
  three trailing uint32 fields.

---

## Pass 2 - F-025 map-tracked (`0x0849` / `0x0848`)

### Ghidra

| Script | Result |
| --- | --- |
| `TraceFunctionCallers` @ `1403f4170` | 1 **DATA** ref `140e037ec` -> apply-table slot for `MapTrackedUnitUpdate_ApplyAndDispatch` |
| `InspectCodeAddresses` @ `1403f4170` | Caches id + XYZ + `TrackingSlotId` (`param_2[4]`); dispatches `MapTrackedUnitUpdate` via `ClientEvent_MapTrackedUnitUpdate_Dispatch` |

### NF

- `TrackingSlotId` on wire models; `TrackingSlotHelper` for `TrackingSlot.tbl` -> `PublicEventObjectiveId` (**table lookup only**) now null-guards missing tables and is covered by focused mask/objective lookup tests.
- Pass 118 adds packet/helper guards: `ServerMapTrackedUnitUpdate` keeps
  `TrackingSlotId` rather than `PublicEventObjectiveId`, and `TrackingSlotHelper`
  remains slot-to-objective lookup only when duplicate rows share one objective.
  Focused guard coverage passed `97/97`.
- **Blocked:** server `TrackedUnitId` allocation, update cadence, disable timing, objective->slot selection.

---

## Pass 3 - F-025 entity-stat aux (`0x0889`, `0x08CC`, `0x08F4`, `0x0939`, `0x093D`, `0x093E`)

### Evidence (existing + pass)

- Readers in `function_labels.csv`; shared payloads (`ServerUInt32WideString_ReadPayload` for `0x08CC`, etc.).
- `TraceFunctionCallers` / direct CALL: **none** for apply paths; only registration + DATA apply-table pattern (same as other aux opcodes in `INITIAL_FINDINGS` F-025 pass).
- `0x0889`: **Mapped** registration - `Network_RegisterServerOpcode_0889` @ `140078cb0`, 12-byte reader `ServerSpellUInt32TripletListRow_ReadPayload` @ `140080bf0` (same reader as `0x0908`/`0x090A`). Per-opcode apply semantics still **Blocked**.
- 2026-06-04 immediate-scan extension: `0x093D` registers at `140074cda`
  with `ServerEntityStatUInt32UInt5Pair_ReadPayload` (`140097690`), `0x0939`
  registers at `140074fb9` with
  `ServerEntityStatUInt32UInt14UInt18WideString_ReadPayload` (`140097ee0`),
  and `0x093E` registers at `14007501b` with
  `ServerEntityStatTwoUInt32UInt64_ReadPayload` (`140097f70`). Other immediate
  hits were GameFormula lookups or object offsets, not apply/producer evidence.

### NF

- Per-opcode XML on models; fields stay `ValueN` until apply-site proof.
- **No** production emit.

---

## Pass 4 - F-008 crafting aux (`0x084B`, `0x0855`)

### Ghidra

- `Network_RegisterServerOpcode_084B` / `_0855` -> readers `1400a3af0` (24 B) and `140081df0` (12 B).
- `InspectCodeAddresses` @ `1405e6690` (`Crafting_HandleServerCraftingFinish`): handles `0x0853` finish path and hot/cold discovery event only - **no** `0x084B`/`0x0855` enqueue in this consumer.
- 2026-06-04 Ghidra MCP recheck reconfirmed `0x084B` as four `uint32` values, one `float`, then one `uint32`, and `0x0855` as one `uint32` plus two `float` values. `Crafting_HandleServerCraftingCurrentCraft` @ `1405e6830` stores the decoded `0x0854` context and dispatches `CraftingUpdateCurrent`; it still does not prove aux enqueue.

### NF

- Fields remain `ValueN` / `FloatValue*`; craft handlers log blocked station service keys at Debug (`0x2C`/`0x4F`/`0x57`).
- **No** aux emit.
- `CraftingSimpleCraftHandlerTests.ComplexCraft_WithCraftStatsAndChargeCounts_DoesNotEmitBlockedCurrentCraftOrAuxPackets`
  guards the fixed-recipe complex-craft path so `0x0854`, `0x084B`, and
  `0x0855` stay modeled-only until current-craft cadence or aux enqueue evidence
  is verified.
- `CraftingAdditiveHandlerTests.Additive_WithValidStation_RecordsModifierStateWithoutBlockedCurrentCraftOrAuxPackets`
  and `Abandon_ClearsModifierStateWithoutBlockedCurrentCraftOrAuxPackets` pin the
  additive bridge as state-only: valid station requests record additive/catalyst
  Item2 modifiers, abandon clears them, and neither path emits `0x0854`,
  `0x084B`, or `0x0855`.
- Durable rune state is no longer a blocker: `item.runeSlots`,
  `ItemRuneSlotsCodec`, `ItemRuneNetworkWire`, `RandomGlyphData`/`Glyphs`, and
  migration `20260531224115_ItemMicrochipIdsAndRuneSlots` preserve socket types
  and installed rune Item2 ids, with focused rune tests covering the codec and
  shared-item wire.
- 2026-06-04 taxonomy cleanup rejects a distinct client microchip-install
  mutator: `RuneCrafting_SendClientRuneInstall` (`14059d250`) / `0x085B` is the
  mapped C2S install route. `ServerItemMicrochips` (`0x056C`) and
  `Inventory_UpdateItemMicrochipsFromWire` (`1403b8540`) remain server-side item
  patch/update evidence with blocked producer timing.
- Verification: focused crafting/rune/additive test run with isolated output
  path passed 96/96 after the default output was previously blocked by a
  running `NexusForever.WorldServer` process holding debug DLLs.
- Verification addendum: focused microchip taxonomy guard passed 149/149 for
  `CraftingRuneHandlerTests`, `ItemRuneSocketTests`, `CraftingPacketShapeTests`,
  and `PacketPlaceholderNamingTests` with
  `artifacts/testbin/f008-microchip-taxonomy/`.

---

## Pass 5 - F-031 Fortune weights

- `ServerFortuneRewards_ReadPayload` @ `140081f60` (`0x03D2`); `FortunesLib_GetFortunesLootList` uses parallel float probabilities x 100 for UI.
- [FORTUNE_WEIGHT_AUDIT.md](FORTUNE_WEIGHT_AUDIT.md): retail per-item weights **Blocked**; NF uses rarity tiers + optional debug catalog probability samples.
- 2026-06-04 Ghidra MCP recheck: `Fortune_ApplyRewards` @ `1407292a0`
  copies server-provided item/money reward arrays plus parallel probabilities
  into Fortune UI state and exposes no active-rotation source. `Fortune_ApplyReset`
  @ `1407291f0` maps the 3-bit `ServerFortuneReset` value `3` to the
  click-empty reset path; NF renamed the field to `ResetCode`.
- NF payout grants now pass the current realm/character identity to
  `IAccountInventoryManager.AddItem` with `hasTargetPlayerIdentity` set when a
  flipped card awards an account item; this is test-pinned and does not alter
  card selection or probability weights.
- False-source disposition: F-007 `RewardRotation*` table labels,
  `Lua_GameLib_BuildRewardRotations`, `RewardRotation_BuildLuaRotationRewards`,
  and `0x07CD`/`0x07D3` content-context packets map the separate
  reward-rotation/storefront schedule surface. They are rejected as F-031
  active-rotation evidence because Fortune catalog probabilities still flow only
  through `ServerFortuneRewards` -> `Fortune_ApplyRewards` ->
  `FortunesLib_GetFortunesLootList`.
- Verification: focused `FullyQualifiedName~Fortune` test run with isolated
  output at `artifacts/testbin/f031-fortune-target` passed 20/20.
- Verification addendum: focused Fortune/reward-rotation boundary run passed
  32/32 with isolated output at
  `artifacts/testbin/f031-fortune-rewardrotation-falsesource/`.

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
