# Runtime blocker unblock plan (RE-only, no diagnostic emit)

Updated: 2026-06-02

**Pass status:** Ghidra passes 1-5 executed 2026-06-02; all five rows remain **Blocked** for NF production emit. See [RUNTIME_BLOCKER_RE_RESULTS.md](RUNTIME_BLOCKER_RE_RESULTS.md).

Policy: **no** `WorldConfig` diagnostic emit flags and **no** opt-in packet emitters.
Unblock each `CURRENT_STATUS` Remaining Blocked row only when decompilation or sniff reaches
**Verified**, then add normal `WorldServer`/`Game` production paths.

## Evidence ladder (unchanged)

Observed -> Correlated -> Mapped -> **Verified** -> Implemented

- **Mapped**: rename/XML/shape tests in `Source/**` only.
- **Verified**: native producer or apply-handler proof.
- **Implemented**: NF handler/emitter with tests; no config-gated hacks.

## Pass order

### 1. F-004 - `0x0506` client trigger

**Question:** Which client->server opcode causes the server to send `ServerHousingNeighborhoodList`?

**Ghidra (WildStar64.exe):**

```powershell
.\Decomp\Analysis\run_ghidra_analysis.ps1 -ExportOnly -Targets WildStar64.exe `
  -MaxDecompiledFunctions 500 -ExtraPostScript TraceFunctionCallers.java `
  -ExtraPostScriptArgs @('1404ba4f0')

.\Decomp\Analysis\run_ghidra_analysis.ps1 -ExportOnly -Targets WildStar64.exe `
  -MaxDecompiledFunctions 500 -ExtraPostScript InspectCodeAddresses.java `
  -ExtraPostScriptArgs @('1404ba4f0','14009ebf0')
```

- String xref: `HousingNeighborhoodRecieved` -> Lua/UI open path.
- Distinguish from `ClientHousingRandomCommunityList` (`0x052C`) -> `ServerHousingRandomCommunityList` (`0x0526`).
- If trigger is server-driven only: analyze **Houston64.exe** realm/housing service for `0x0506` enqueue. *(2026-06-02 scan: Houston `0x506` hits are MFC dialog control IDs only - not world opcodes; see [RUNTIME_BLOCKER_RE_RESULTS.md](RUNTIME_BLOCKER_RE_RESULTS.md).)*

**NF when Verified:** `ClientHousing*Handler` + `ResidenceManager`/`GlobalResidenceManager` emit `0x0506` on that trigger only.

### 2. F-025 - map-tracked producer (`0x0849` / `0x0848`)

**Question:** Who allocates `TrackedUnitId` and emits updates on the **server**?

Client binary maps **consumers** only (`1403f4170`, `1403f4200`). Producer is not a direct xref from readers.

**Ghidra:**

```powershell
.\Decomp\Analysis\run_ghidra_analysis.ps1 -ExportOnly -Targets WildStar64.exe `
  -MaxDecompiledFunctions 500 -ExtraPostScript TraceFunctionCallers.java `
  -ExtraPostScriptArgs @('1403f4170')
```

- Follow `WorldZone_InsertUnitHandlerIntoList` handler-node `vtable+0x58` path ([ENTITY_AUX_DECODE_ROADMAP.md](ENTITY_AUX_DECODE_ROADMAP.md)).
- Correlate `TrackingSlot.tbl` via `ClientDB_RegisterTrackingSlot` @ `1402426a0`.
- Optional sniff: public event with map markers (opcode order vs `ServerPublicEvent*`).

**NF when Verified:** emit from public-event/objective code with proven id/slot/cadence; use `TrackingSlotHelper` for slot->objective mapping.

### 3. F-025 - entity-stat aux (`0x0889`, `0x08CC`, `0x08F4`, `0x0939`, `0x093D`, `0x093E`)

Per-opcode **apply** handlers via indirect dispatch (`Network_RegisterServerOpcode_0351` @ `14006c290`).
Rename fields only after apply-site proof. Shared readers stay `ValueN` with per-opcode XML.

### 4. F-008 - crafting aux (`0x084B`, `0x0855`)

Find native **enqueue** beside `ServerCraftingFinish` / `ServerCraftingCurrentCraft` (`1400a3af0`, `140081df0`).
Readers labeled; no apply handler in `function_labels.csv` yet.

### 5. F-031 - Fortune weights

Retail `ServerFortuneRewards.RewardItemProbabilities` capture or storefront rotation dump
([FORTUNE_WEIGHT_AUDIT.md](FORTUNE_WEIGHT_AUDIT.md)). No guessed `AccountItem.tbl` weights.

## Out of scope

- `Enable*DiagnosticEmit` WorldConfig keys
- `IMapTrackedUnitEmitter` / `IHousingNeighborhoodListEmitter`
- Emitting aux/housing/map packets without **Verified** producer rules
