# Old Client EXE Decompile Sweep Results

Created: 2026-06-19

This file records narrow old-client comparison passes from
`OLD_CLIENT_EXE_DECOMPILE_SWEEP_GOAL_PROMPT.md`. Older WildStar executables are
used only as search accelerants. WildStar 16042 remains the authority for labels,
implementation, and completion claims.

## Inputs

- Prompt: `Decomp/Analysis/OLD_CLIENT_EXE_DECOMPILE_SWEEP_GOAL_PROMPT.md`
- Current authority binary: `Decomp/Client64/WildStar64.exe`
- Archive source: `I:/WildStar/ClientArchives/ArctiumClientData/Binaries`
- Local ignored extraction/output root: `artifacts/old-client-exe-sweep`
- Existing 16042 evidence sources:
  - `Decomp/Analysis/function_labels.csv`
  - `Decomp/Analysis/mapping_artifacts/function_evidence_inventory.csv`
  - `Decomp/Analysis/exports/WildStar64.exe/selected_decompiled_cache/functions/sha256_231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5`

## Current Scope

The initial opcode passes compare the closest near-neighbor executable, build
16029, against current 16042 for compact opcode-immediate blocker families. This
report also records stale-provenance checks from generated DataMapping and
content-retail evidence where the goal prompt calls them out. It does not mark
the broader old-client sweep goal complete. Remaining candidates include older
near-neighbor builds (`16028`, `16005`, `15996`, `15884`, `15866`),
public-event objective/vote/scoreboard/reward producer paths, and instance
producer paths.

## Slice 1 - F-025 Entity-Stat Aux Opcodes

Question: do near-neighbor old executables expose extra opcode-immediate or
apply-handler anchors for aux server opcodes `0x0889`, `0x08CC`, `0x08F4`,
`0x0939`, `0x093D`, or `0x093E`?

Disposition: rejected as an old-client accelerator. Build 16029 does not expose
any additional useful target beyond the existing 16042 registration and reader
evidence. The F-025 aux-opcode slice remains mapped-only and blocked pending
per-opcode apply/producer classification or live packet evidence.

### Binary Hashes

| Build | Path | SHA256 |
| --- | --- | --- |
| 16042 | `Decomp/Client64/WildStar64.exe` | `231BB2BB3FC6C37F3E8A43A0BA965CC3645287BBC6CCAD83D073A495C396B3E5` |
| 16029 | `artifacts/old-client-exe-sweep/16029/WildStar64.exe` | `F0C46D1517BC7E9E25D0DDA6C278CE99F9B3976B1742A87AE57E322F6C7A8BCC` |
| 16028 | `artifacts/old-client-exe-sweep/16028/WildStar64.exe` | `C02CAD4D73FA476FE7A5403E54FE2161C060A4CAAFD5B6B52899575474474C26` |
| 16005 | `artifacts/old-client-exe-sweep/16005/WildStar64.exe` | `C221C1313614B8BC17F6F0921FA1C162DD3797FE61821E2581264F2F54AE879D` |
| 15996 | `artifacts/old-client-exe-sweep/15996/WildStar64.exe` | `7FD8D5E33BAF36E96E5626D60525050A496AA10AF00D827A0C94D28A648DE260` |
| 15884 PTR | `artifacts/old-client-exe-sweep/15884/WildStar64.exe` | `31C2D4DCA819FFD86E3C555486C313C6137FF1C7644AA4DA634CC4394322BA13` |
| 15866 PTR | `artifacts/old-client-exe-sweep/15866/WildStar64.exe` | `586A6F617C45C0D33ACFB7375192C4B6C9F51C86B89FB80BF6A96DF11F273050` |
| 1.0.7.6682 Live | `artifacts/old-client-exe-sweep/1.0.7.6682/WildStar64.exe` | `320597EE0A6A81766E6608E28F5DE6634C78E3AD28EC977B9FDFC3B7A0617D3D` |
| 1.0.3.6576 Beta | `artifacts/old-client-exe-sweep/beta_1.0.3.6576/WildStar64.exe` | `0B052733970C16C92F241B39F44CC3ECB33B0B7B5CADA5F85787CE5EACFD6624` |
| 0.4.7.6072 Alpha | `artifacts/old-client-exe-sweep/alpha_0.4.7.6072/Client_x64.exe` | `36C60E5CED69C8EAC10384FF715830FA5B2DD7E1EA558FC3EAAFA555997C1CA7` |

### Commands

16029 near-neighbor helper pass:

```powershell
.\Decomp\Analysis\run_ghidra_analysis.ps1 `
  -ClientDir artifacts\old-client-exe-sweep\16029 `
  -OutputDir artifacts\old-client-exe-sweep\16029_exports `
  -ProjectDir artifacts\old-client-exe-sweep\ghidra_projects `
  -Targets WildStar64.exe `
  -ProjectLayout PerTarget `
  -NoApplyLabels `
  -SkipDefaultExport `
  -SkipCoverage `
  -ExtraPostScript FindOpcodeComparisons.java `
  -ExtraPostScriptArgs @('0x0889','0x08CC','0x08F4','0x0939','0x093D','0x093E','0x100') `
  -GhidraMaxHeap 4G
```

16042 authority comparison pass:

```powershell
.\Decomp\Analysis\run_ghidra_analysis.ps1 `
  -ClientDir Decomp\Client64 `
  -OutputDir artifacts\old-client-exe-sweep\16042_helper_exports `
  -ProjectDir Decomp\Analysis\ghidra_projects `
  -Targets WildStar64.exe `
  -ProjectLayout PerTarget `
  -AnalysisMode Skip `
  -NoApplyLabels `
  -SkipDefaultExport `
  -SkipCoverage `
  -ExtraPostScript FindOpcodeComparisons.java `
  -ExtraPostScriptArgs @('0x0889','0x08CC','0x08F4','0x0939','0x093D','0x093E','0x100') `
  -GhidraMaxHeap 4G
```

Normalized instruction-shape comparison:

```powershell
function Get-InsnCounts($path) {
  Get-Content $path |
    Where-Object { $_ -match 'match=' } |
    ForEach-Object {
      if ($_ -match 'insn=(.*) \(GhidraScript\)') { $matches[1] }
    } |
    Group-Object |
    Sort-Object Name |
    ForEach-Object { '{0} x{1}' -f $_.Name, $_.Count }
}

$old = Get-InsnCounts artifacts\old-client-exe-sweep\16029_opcode_matches.txt
$cur = Get-InsnCounts artifacts\old-client-exe-sweep\16042_opcode_matches.txt
Compare-Object -ReferenceObject $cur -DifferenceObject $old
```

The final `Compare-Object` produced no output.

### Evidence

Both 16029 and 16042 report `total_matches=50` for the six searched opcode
immediates. The first six opcode-registration hits are the same addresses and
instructions. The only material difference is symbol state: 16042 has the
durable label `Network_RegisterServerOpcode_0351`, while the old 16029 project
still names the containing function `FUN_14006c290`.

| Opcode | 16042 registration | 16042 reader | Reader shape | 16029 result |
| --- | --- | --- | --- | --- |
| `0x0889` | `140078cb0` | `ServerSpellUInt32TripletListRow_ReadPayload @140080bf0` | three `uint32` fields | Same registration immediate in `FUN_14006c290` |
| `0x08CC` | `140075236` | `ServerUInt32WideString_ReadPayload @1400980f0` | `uint32`, wide string | Same registration immediate in `FUN_14006c290` |
| `0x08F4` | `140075082` | `ServerEntityStatUInt32UInt5UInt32_ReadPayload @140097620` | `uint32`, 5-bit value, `uint32` | Same registration immediate in `FUN_14006c290` |
| `0x0939` | `140074fb9` | `ServerEntityStatUInt32UInt14UInt18WideString_ReadPayload @140097ee0` | `uint32`, 14-bit value, 18-bit value, wide string | Same registration immediate in `FUN_14006c290` |
| `0x093D` | `140074cda` | `ServerEntityStatUInt32UInt5Pair_ReadPayload @140097690` | `uint32`, 5-bit value, two `uint32` fields | Same registration immediate in `FUN_14006c290` |
| `0x093E` | `14007501b` | `ServerEntityStatTwoUInt32UInt64_ReadPayload @140097f70` | two `uint32` fields, `uint64` | Same registration immediate in `FUN_14006c290` |

The non-registration hits are also not new packet targets: after normalizing to
instruction text, 16029 and 16042 have the same multiset of offset/formula-like
matches. Several non-registration function starts are shifted by `0x20` between
16042 and 16029, but the instruction shapes and counts are unchanged.

Raw local outputs:

- `artifacts/old-client-exe-sweep/16029_opcode_matches.txt`
- `artifacts/old-client-exe-sweep/16042_opcode_matches.txt`
- `Decomp/Analysis/logs/WildStar64.NexusForeverClient64_WildStar64.FindOpcodeComparisons.ghidra.log`
- `Decomp/Analysis/logs/LATEST_RUN_SUMMARY.json`

### Stop State

Do not add labels or runtime behavior from this old-client comparison. The
authoritative 16042 state remains:

- registration and reader layouts are mapped;
- `FUN_140939650` remains rejected as a server-packet consumer candidate;
- aux opcode semantics are not implemented;
- completion remains blocked until 16042 apply/producer evidence or live-client
  packet evidence identifies per-opcode behavior.

## Slice 2 - F-016/F-020 Spell Aux Opcodes

Question: does build 16029 expose extra opcode-immediate or apply/producer
anchors for spell-aux opcodes `0x080F`, `0x0810`, and `0x0812`, with `0x07FC`
and shared four-uint opcode `0x078C` included as controls?

Disposition: rejected as an old-client accelerator. Build 16029 has the same
registration immediates and normalized instruction-shape multiset as current
16042. It does not reveal a new 16042 search target for the spell-aux producer,
apply owner, callback/table owner, or live-capture question.

### Commands

16029 helper pass:

```powershell
.\Decomp\Analysis\run_ghidra_analysis.ps1 `
  -ClientDir artifacts\old-client-exe-sweep\16029 `
  -OutputDir artifacts\old-client-exe-sweep\16029_exports `
  -ProjectDir artifacts\old-client-exe-sweep\ghidra_projects `
  -Targets WildStar64.exe `
  -ProjectLayout PerTarget `
  -AnalysisMode Skip `
  -NoApplyLabels `
  -SkipDefaultExport `
  -SkipCoverage `
  -ExtraPostScript FindOpcodeComparisons.java `
  -ExtraPostScriptArgs @('0x07FC','0x080F','0x0810','0x0812','0x078C','0x100') `
  -GhidraMaxHeap 4G
```

16042 authority comparison used the same `FindOpcodeComparisons.java` arguments
against `Decomp/Client64` with `-AnalysisMode Skip`. The same normalized
instruction-shape comparison shown in Slice 1 produced no output.

### Evidence

Both 16029 and 16042 report `total_matches=75` for the searched immediates.
The registration-immediate hits are identical by address and instruction:

| Opcode | Registration hit | 16042 reader evidence | 16029 result |
| --- | --- | --- | --- |
| `0x078C` | `14006dba5` | `ServerSpellFourUInt32_ReadPayload @14007fef0`; shared housing/community consumer already mapped | Same registration immediate in `FUN_14006c290` |
| `0x07FC` | `1400743d9` | `ServerSpellCastResult_ReadPayload @140094fb0`; leading field semantics still blocked | Same registration immediate in `FUN_14006c290` |
| `0x0810` | `14007480f` | `ServerSpellUInt32TripletList_ReadPayload @140095da0`; counted triplet rows via `140080bf0` | Same registration immediate in `FUN_14006c290` |
| `0x080F` | `140074840` | `ServerSpellUInt32TripletList_ReadPayload @140095da0`; counted triplet rows via `140080bf0` | Same registration immediate in `FUN_14006c290` |
| `0x0812` | `1400748a2` | `ServerSpellFourUInt32_ReadPayload @14007fef0`; four `uint32` fields | Same registration immediate in `FUN_14006c290` |

The raw scan includes many `0x810`/`0x812` offset-looking structure accesses.
Those are not useful spell packet targets here: after normalizing by instruction
text, 16029 and 16042 both have 49 unique instruction-shape/count entries and
no differences.

Raw local outputs:

- `artifacts/old-client-exe-sweep/16029_spell_aux_opcode_matches.txt`
- `artifacts/old-client-exe-sweep/16042_spell_aux_opcode_matches.txt`

### Stop State

Do not add labels or runtime behavior from this old-client comparison. The
authoritative 16042 state remains:

- `0x080F`/`0x0810` are mapped only as counted triplet-list payloads;
- `0x0812` is mapped only as a four-`uint32` spell-aux payload;
- `0x07FC` leading-field semantics remain blocked except for already-recorded
  source-local compatibility/context-token observations;
- spell-aux emit timing and field semantics remain blocked until a native
  apply/producer path, callback/table owner, dynamic dispatch proof, or accepted
  live spell packet capture proves them.

## Slice 3 - F-005 Marketplace Aux Opcodes

Question: does build 16029 expose extra opcode-immediate, apply, or producer
anchors for marketplace aux server opcodes `0x06DF` (`ServerAuctionPostAux`) or
`0x07D5` (`ServerAuctionsByFilterAux`)?

Disposition: rejected as an old-client accelerator. Build 16029 exposes only
the same two registration-immediate hits as current 16042. No old-client-only
marketplace apply helper, producer, callback/table owner, or sharper 16042
search target surfaced.

### Commands

16029 and 16042 helper passes used `FindOpcodeComparisons.java` with:

```powershell
-ExtraPostScriptArgs @('0x06DF','0x07D5','0x100')
```

The same normalized instruction-shape comparison shown in Slice 1 produced no
output.

### Evidence

Both 16029 and 16042 report `total_matches=2`.

| Opcode | Immediate hit | 16042 reader evidence | 16029 result |
| --- | --- | --- | --- |
| `0x07D5` | `14007337b` | `ServerAuctionsByFilterAux_ReadPayload @14008fe80`; one 14-bit value, three `uint32` fields, one flag | Same registration immediate in `FUN_14006c290` |
| `0x06DF` | `14007343f` | `ServerAuctionPostAux_ReadPayload @140090090`; count, counted `uint32` array, counted byte array, trailing `uint32` | Same registration immediate in `FUN_14006c290` |

Raw local outputs:

- `artifacts/old-client-exe-sweep/16029_marketplace_aux_opcode_matches.txt`
- `artifacts/old-client-exe-sweep/16042_marketplace_aux_opcode_matches.txt`

### Stop State

Do not add labels or runtime behavior from this old-client comparison. The
authoritative 16042 state remains:

- marketplace aux packets are packet-contract/model/test surfaces only;
- `0x06DF` and `0x07D5` remain non-emitted with neutral fields;
- producer semantics remain blocked until native marketplace apply/producer
  proof, a server producer witness, or accepted live marketplace packet capture
  ties the fields to visible UI state and emit timing.

## Slice 4 - F-004 Housing Neighborhood Opcodes

Question: does build 16029 expose extra opcode-immediate or producer-trigger
anchors for housing neighborhood server opcodes `0x0501`
(`ServerHousingNeighborhoodEntry`) or `0x0506`
(`ServerHousingNeighborhoodList`)?

Disposition: mapped/blocked unchanged. Unlike the purely reader-only aux
slices, 16042 already has mapped client apply-consumer evidence for `0x0506`
through `Housing_HandleNeighborhoodList @1404ba4f0`, plus apply-table data
pointers. The missing proof is still the server-push/send-site trigger and row
backing. Build 16029 does not add a new 16042 producer search target.

### Commands

16029 and 16042 helper passes used `FindOpcodeComparisons.java` with:

```powershell
-ExtraPostScriptArgs @('0x0501','0x0506','0x100')
```

The same normalized instruction-shape comparison shown in Slice 1 produced no
output.

### Evidence

Both 16029 and 16042 report `total_matches=14`, with 8 unique normalized
instruction-shape/count entries.

| Opcode | Immediate hit | 16042 evidence | 16029 result |
| --- | --- | --- | --- |
| `0x0501` | `1400774d8` | `ServerHousingNeighborhoodEntry_ReadPayload @14009cbe0`; one `uint64`, two 14-bit fields, another `uint64`, wide string, three `uint32` fields | Same registration immediate in `FUN_14006c290` |
| `0x0506` | `140077730` | `ServerHousingNeighborhoodList_ReadPayload @14009ebf0`; realm, count, `0x30`-byte neighborhood rows; applied by `Housing_HandleNeighborhoodList @1404ba4f0` | Same registration immediate in `FUN_14006c290` |

The non-registration hits are not old-only producer clues. They normalize to the
same instruction shapes in both builds, and the current 16042 labels already
classify adjacent evidence as consumer/table-loader or unrelated constant use,
not a safe server-push trigger.

Raw local outputs:

- `artifacts/old-client-exe-sweep/16029_housing_neighborhood_opcode_matches.txt`
- `artifacts/old-client-exe-sweep/16042_housing_neighborhood_opcode_matches.txt`

### Stop State

Do not add labels or runtime behavior from this old-client comparison. The
authoritative 16042 state remains:

- `0x0501`/`0x0506` row and list readers are mapped;
- `Housing_HandleNeighborhoodList @1404ba4f0` remains mapped as the client
  cache apply consumer for `0x0506`;
- `ClientDB_RegisterHousingNeighborhoodInfo @140205900` remains table-loader
  evidence only;
- producer timing, backing rows, field names, and any NexusForever emitter
  remain blocked until a native server-push path or accepted live housing
  UI/realm-login capture proves trigger, population, and timing.

## Slice 5 - F-010 Matching And Raid Queue Cluster

Question: does build 16029 expose extra opcode-immediate, sender, apply, or
queue-state anchors for the remaining F-010 matching/raid cluster:
`0x05CF`, `0x0600`, `0x0718`, `0x071A`, `0x05D5`, `0x0602`, `0x062A`, or
`0x0634`?

Disposition: mapped/blocked unchanged. Build 16029 has the same normalized
instruction-shape multiset as current 16042. Current 16042 already has the
useful durable labels for the replacement-search senders and shared readers;
16029 does not reveal an extra queue-state, non-zero raid queue, diagnostic
sender-intent, or `0x05CF` apply-index target.

### Commands

16029 and 16042 helper passes used `FindOpcodeComparisons.java` with:

```powershell
-ExtraPostScriptArgs @('0x05CF','0x0600','0x0718','0x071A','0x05D5','0x0602','0x062A','0x0634','0x100')
```

The same normalized instruction-shape comparison shown in Slice 1 produced no
output.

### Evidence

Both 16029 and 16042 report `total_matches=145`, with 107 unique normalized
instruction-shape/count entries.

| Opcode | Representative immediate hit | 16042 evidence | 16029 result |
| --- | --- | --- | --- |
| `0x0718` | `1400717e2` | `ServerRaidQueueStatus_ReadPayload @14008bf80`; shared raid-info row, standalone non-zero timing still blocked | Same registration immediate in `FUN_14006c290` |
| `0x071A` | `14007180c` | `ServerRaidInfoResponse_ReadPayload @14008c010`; counted raid-info rows | Same registration immediate in `FUN_14006c290` |
| `0x05D5` | `140075918`, `14076ab61` | shared `ClientTradeskillResetTalents_WritePayload @14007d010`; current sender label `MatchingReplacement_SendStartLookingForReplacements @14076aa30` | Same registration and sender-like immediates, old project unlabeled |
| `0x0602` | `14007594f`, `14076ac09` | zero-payload registration plus current sender label `MatchingReplacement_SendStopLookingForReplacements @14076abb0` | Same registration and sender-like immediates, old project unlabeled |
| `0x05CF` | `140075a13` | shared raw-`uint32` reader `ServerUInt32_LocalReadThunk @140099110`; apply-index/field semantics still blocked | Same registration immediate in `FUN_14006c290` |
| `0x0600` | `140075de7` | shared identity-plus-`uint32` reader evidence; matching trailing semantics blocked | Same registration immediate in `FUN_14006c290` |
| `0x062A` | `1400a82b6` | shared uint32 client diagnostic registration in `ClientWorldOpcodeRegister_MovementSpline @1400a8190`; sender intent still blocked | Same registration immediate, old project unlabeled |
| `0x0634` | `1400a872c` | shared uint32 client diagnostic registration in `ClientWorldOpcodeRegister_MovementSpline @1400a8190`; sender intent still blocked | Same registration immediate, old project unlabeled |

Raw local outputs:

- `artifacts/old-client-exe-sweep/16029_matching_raid_opcode_matches.txt`
- `artifacts/old-client-exe-sweep/16042_matching_raid_opcode_matches.txt`

### Stop State

Do not add labels or runtime behavior from this old-client comparison. The
authoritative 16042 state remains:

- replacement-search `0x05D5`/`0x0602` sender labels are already mapped;
- diagnostic `0x062A`/`0x0634` stay log-only until sender intent or live UI
  context is proven;
- `0x05CF` remains mapped-only until apply-table index or live payload context
  proves field semantics;
- standalone non-zero `0x0718` raid queue timing remains blocked until a live
  non-zero payload or producer proof exists.

## Slice 6 - F-007 Reward Rotation Content Context

Question: does build 16029 expose extra opcode-immediate, apply, or throttle
anchors for reward rotation opcodes `0x07CD`, `0x07D3`, `0x07CA`, `0x07CC`,
`0x07C8`, `0x07C7`, `0x07C9`, or `0x07CB`?

Disposition: mapped/blocked unchanged. Build 16029 has the same normalized
instruction-shape multiset as current 16042. The only sender-style hit in this
query is the already-labeled 16042 `Reward_SendRewardUpdateRequest @140636ba0`
for the index-only client refresh request `0x07CC`. No old-client-only target
surfaced for `0x07CD` field assignment, trailing `Flag` semantics, dynamic
throttle-slot mapping, or content-context apply behavior.

### Commands

16029 and 16042 helper passes used `FindOpcodeComparisons.java` with:

```powershell
-ExtraPostScriptArgs @('0x07CD','0x07D3','0x07CA','0x07CC','0x07C8','0x07C7','0x07C9','0x07CB','0x100')
```

The same normalized instruction-shape comparison shown in Slice 1 produced no
output.

### Evidence

Both 16029 and 16042 report `total_matches=48`, with 36 unique normalized
instruction-shape/count entries.

| Opcode | Representative immediate hit | 16042 evidence | 16029 result |
| --- | --- | --- | --- |
| `0x07CD` | `140073319` | `ServerRewardRotationContentContext_ReadPayload @14008fcb0`; 14-bit index, four `uint32` fields, counted content-id array, trailing flag | Same registration immediate in `FUN_14006c290` |
| `0x07D3` | `14007334a` | `ServerRewardRotationContentContextArray_ReadPayload @14008fdc0`; counted `0x28` content-context rows | Same registration immediate in `FUN_14006c290` |
| `0x07CA` | `140078429` | `ServerRewardRotationScheduleArray_ReadPayload @1400a1e20`; schedule rows with content/reward lookup data | Same registration immediate in `FUN_14006c290` |
| `0x07CC` | `140078460`, `140636c31` | `Reward_SendRewardUpdateRequest @140636ba0` sends the index-only refresh request | Same registration and sender-like immediates, old project unlabeled |
| `0x07C8` | `140078491` | `ServerRewardRotationEntryStateArray_ReadPayload @1400a1f80`; counted entry-state rows | Same registration immediate in `FUN_14006c290` |
| `0x07C7`/`0x07C9`/`0x07CB` | `140078524`/`1400784f3`/`1400784c2` | `RewardRotation_EntryStateDelta_ReadPayload @1400a2040`; entry-state delta reader family | Same registration immediates in `FUN_14006c290` |

Raw local outputs:

- `artifacts/old-client-exe-sweep/16029_reward_rotation_opcode_matches.txt`
- `artifacts/old-client-exe-sweep/16042_reward_rotation_opcode_matches.txt`

### Stop State

Do not add labels or runtime behavior from this old-client comparison. The
authoritative 16042 state remains:

- `0x07CA` schedule and `0x07C8`/`0x07C7`/`0x07C9`/`0x07CB` entry-state surfaces
  are mapped through existing labels;
- `0x07CD`/`0x07D3` content-context packet shapes are mapped and may be emitted
  conservatively by current source, but field names and `Flag` semantics remain
  neutral/correlated;
- exact content-context apply behavior, dynamic throttle-slot assignment, and
  retail schedule/content selection remain blocked until live/retail packet
  evidence or dynamic apply-dispatch proof exists.

## Slice 7 - F-025 Map-Tracked Unit Producer

Question: does build 16029 expose extra opcode-immediate or server producer
anchors for map-tracked unit opcodes `0x0849`
(`ServerMapTrackedUnitUpdate`) or `0x0848`
(`ServerMapTrackedUnitDisable`)?

Disposition: mapped/blocked unchanged. Current 16042 already has reader, apply,
client-event, Lua, and `TrackingSlot.tbl` loader evidence. The missing proof is
still server producer timing, tracked-unit id allocation, disable lifetime, and
safe `TrackingSlotId` selection. Build 16029 does not reveal a new 16042
producer target.

### Commands

16029 and 16042 helper passes used `FindOpcodeComparisons.java` with:

```powershell
-ExtraPostScriptArgs @('0x0849','0x0848','0x100')
```

The same normalized instruction-shape comparison shown in Slice 1 produced no
output.

### Evidence

Both 16029 and 16042 report `total_matches=64`, with 39 unique normalized
instruction-shape/count entries.

| Opcode | Registration hit | 16042 evidence | 16029 result |
| --- | --- | --- | --- |
| `0x0849` | `140079ea4` | `ServerMapTrackedUnitUpdate_ReadPayload @1400a6c10`; `MapTrackedUnitUpdate_ApplyAndDispatch @1403f4170`; Lua/UI lookup path mapped | Same registration immediate in `FUN_14006c290` |
| `0x0848` | `140079ed5` | shared `ServerUInt32_ReadPayload`; `MapTrackedUnitDisable_ApplyAndDispatch @1403f4200` removes cached state | Same registration immediate in `FUN_14006c290` |

The raw `0x0848` scan contains many offset-like structure accesses. They are
not useful producer clues here: the normalized instruction shapes match between
16029 and 16042, and the current labels already identify the actual 16042
apply/UI evidence.

Raw local outputs:

- `artifacts/old-client-exe-sweep/16029_map_tracked_opcode_matches.txt`
- `artifacts/old-client-exe-sweep/16042_map_tracked_opcode_matches.txt`

### Stop State

Do not add labels or runtime behavior from this old-client comparison. The
authoritative 16042 state remains:

- `0x0849`/`0x0848` reader and apply consumers are mapped;
- `TrackingSlotHelper` remains a one-way table lookup and does not prove
  objective-to-slot selection;
- production emits, update cadence, disable lifetime, and tracked-unit id
  allocation remain blocked until a native send site or accepted live
  public-event marker capture proves timing and fields.

## Slice 8 - F-008 Crafting Current-Craft And Aux

Question: does build 16029 expose extra opcode-immediate, producer, or
enqueue-timing anchors for `ServerCraftingCurrentCraft` (`0x0854`), crafting aux
`0x084B`/`0x0855`, or `ServerItemMicrochips` (`0x056C`)?

Disposition: mapped/blocked unchanged. Build 16029 exposes the same
registration immediates and normalized instruction-shape multiset as current
16042. It does not add a new producer/send-site target for current-craft,
crafting aux, or item microchip patch timing.

### Commands

16029 and 16042 helper passes used `FindOpcodeComparisons.java` with:

```powershell
-ExtraPostScriptArgs @('0x084B','0x0855','0x0854','0x056C','0x100')
```

The same normalized instruction-shape comparison shown in Slice 1 produced no
output.

### Evidence

Both 16029 and 16042 report `total_matches=15`, with 9 unique normalized
instruction-shape/count entries.

| Opcode | Registration hit | 16042 evidence | 16029 result |
| --- | --- | --- | --- |
| `0x0855` | `140078e4a` | `ServerUInt32AndTwoFloats_ReadPayload @140081df0`; one `uint32` and two floats | Same registration immediate in `FUN_14006c290` |
| `0x084B` | `140078eac` | `ServerCraftingAuxFourUInt32FloatUInt32_ReadPayload @1400a3af0`; four `uint32`, one float, one `uint32` | Same registration immediate in `FUN_14006c290` |
| `0x056C` | `140078fd2` | `ServerItemMicrochips_ReadPayload @1400a3d50`; item microchip patch transport; `Inventory_UpdateItemMicrochipsFromWire @1403b8540` apply evidence | Same registration immediate in `FUN_14006c290` |
| `0x0854` | `1400794fd` | `ServerCraftingCurrentCraft_ReadPayload @1400a46b0`; `Crafting_HandleServerCraftingCurrentCraft @1405e6830` dispatches `CraftingUpdateCurrent` | Same registration immediate in `FUN_14006c290` |

Raw local outputs:

- `artifacts/old-client-exe-sweep/16029_crafting_aux_opcode_matches.txt`
- `artifacts/old-client-exe-sweep/16042_crafting_aux_opcode_matches.txt`

### Stop State

Do not add labels or runtime behavior from this old-client comparison. The
authoritative 16042 state remains:

- current-craft, crafting aux, and item-microchip payload shapes are mapped;
- current source must keep `ServerCraftingCurrentCraft`, `0x084B`, `0x0855`,
  and `0x056C` producer behavior blocked;
- the next unlocker is still an interactive Ghidra producer/send-site pass or
  accepted live crafting/item-replication capture proving enqueue cadence,
  field population, result-state boundaries, and item patch timing.

## Slice 9 - Stale Zone And Reward Provenance IDs

Question: do old or generated provenance ids `WorldZone` `1`, `2`, `6`, or
`Quest2Reward` `3668` provide current build-16042 implementation evidence?

Disposition: rejected as current implementation evidence; mapped only as stale
or archived provenance. None of the three `WorldZone` ids exists in the current
16042 `WorldZone.tbl`/generated client map, and `Quest2Reward` id `3668` is not
a current Q3668 reward row. These ids must remain provenance aliases or blockers
until current `WorldZone.tbl`, MapZone/QuestDirection/decompile evidence, or
accepted live-client route/reward evidence identifies a current replacement.

### Commands

Current `WorldZone` id check:

```powershell
$rows = Import-Csv Tools\DataMapping\output\world_zone_client_map.csv
foreach ($id in '1','2','6','36') {
  $match = $rows | Where-Object { $_.world_zone_id -eq $id }
  if ($match) { $match | ConvertTo-Csv -NoTypeInformation }
  else { "world_zone_id=$id absent in world_zone_client_map.csv" }
}

$lines = Get-Content wildstar_client_mysql\WorldZone.tbl.sql
foreach ($id in 1,2,6) {
  $pattern = "INSERT INTO WorldZone VALUES \($id,"
  $match = $lines | Select-String -Pattern $pattern
  if ($match) { $match.Line } else { "WorldZone raw id $id absent" }
}
```

Generated relation counts:

```powershell
$rows = Import-Csv Tools\DataMapping\output\quest_zone_map.csv
foreach ($id in '1','2','6') {
  $subset = $rows | Where-Object { $_.world_zone_id -eq $id }
  $byStatus = $subset | Group-Object match_status |
    ForEach-Object { "$($_.Name)=$($_.Count)" }
  "world_zone_id=$id count=$($subset.Count) statuses=$($byStatus -join ';')"
}

$evidence = Import-Csv Decomp\Analysis\coverage\content_retail_completeness_quest_zone_evidence.csv
foreach ($id in '1','2','6') {
  $subset = $evidence | Where-Object { $_.world_zone_id -eq $id }
  $distinctQuests = ($subset | Select-Object -ExpandProperty quest_id -Unique).Count
  "world_zone_id=$id evidence_rows=$($subset.Count) distinct_quests=$distinctQuests"
}
```

Current Q3668 reward check:

```powershell
rg -n "(^|,)3668(,|$)" `
  Tools\DataMapping\output\quest_reward_client_map.csv `
  Tools\DataMapping\output\quest_reward_item_map.csv `
  Decomp\Analysis\coverage\content_retail_completeness_quest_reward_evidence.csv `
  Decomp\Analysis\coverage\content_retail_completeness_quest_rewards.csv

rg -n "INSERT INTO Quest2Reward VALUES \((2190|2191|4768),3668," `
  wildstar_client_mysql\Quest2Reward.tbl.sql
rg -n "INSERT INTO Quest2Reward VALUES \(3668," `
  wildstar_client_mysql\Quest2Reward.tbl.sql
```

### Evidence

Current 16042 `WorldZone` evidence:

| ID | Current 16042 state | Generated relation impact |
| --- | --- | --- |
| `1` | Absent from `world_zone_client_map.csv`; absent from raw `WorldZone.tbl.sql` | `40` `quest_zone_map.csv` rows across `30` distinct content-retail quest-zone evidence quests, all `partial` / `datamapping_quest_zone_partial_match_pending_zone_review` |
| `2` | Absent from `world_zone_client_map.csv`; absent from raw `WorldZone.tbl.sql` | `633` `quest_zone_map.csv` rows across `488` distinct content-retail quest-zone evidence quests; `629` partial and `4` unmatched / blocked |
| `6` | Absent from `world_zone_client_map.csv`; absent from raw `WorldZone.tbl.sql` | `399` `quest_zone_map.csv` rows across `272` distinct content-retail quest-zone evidence quests; `398` partial and `1` unmatched / blocked |

The current build does contain `WorldZone` `36` (`Auroria`) in both the
generated client map and raw table (`INSERT INTO WorldZone VALUES
(36,7768,0,1,4227327,84,0,0,6,0);`). That confirms old Auroria provenance such
as `WorldZone` `6` must not be treated as a current id without a reviewed
replacement bridge. `MISSING_FEATURE_MATRIX.md` and
`QUEST_IMPLEMENTATION_STATUS.md` already record this boundary for the Q5597
Auroria call-zone row: generated evidence says `WorldZone` `6` has no current
build-16042 row/name and must stay blocked pending replacement evidence.

Current Q3668 reward evidence:

| Source | Current result |
| --- | --- |
| `wildstar_client_mysql/Quest2Reward.tbl.sql` | Q3668 has current 16042 rows `2190`, `2191`, and `4768`, all item rewards for quest id `3668`; no row starts with reward id `3668` |
| `Tools/DataMapping/output/quest_reward_client_map.csv` | Q3668 rows are `2190` -> Item2 `81334`, `2191` -> Item2 `81331`, and `4768` -> Item2 `81329` |
| `Tools/DataMapping/output/quest_reward_item_map.csv` | Old/Jabbithole Q3668 source rows `5111`/`5112`/`5113` map to current game reward ids `2190`/`4768`/`2191`; source row `5114` maps to `4769`, which is not a current Q3668 reward row in `content_retail_completeness_quest_rewards.csv` |
| `Decomp/Analysis/coverage/content_retail_completeness_quest_reward_evidence.csv` | `game_reward_id=3668` appears only for quest `5864` (`Unwelcome Guests`) source reward `6073`, and is `canonical_tuple_unmatched`; it is not Q3668 implementation evidence |
| `Decomp/Analysis/coverage/content_retail_completeness_quest_rewards.csv` | Q3668 runtime reward evidence is limited to rows `2190`, `2191`, `4768`, plus pushed item `6912`; row status stays `not_retail_complete` pending client-visible smoke |

### Stop State

Do not add runtime behavior, labels, or completion claims from `WorldZone`
`1`/`2`/`6` or `Quest2Reward` `3668` as old-client/current-client shortcuts.
The authoritative 16042 state remains:

- archived `WorldZone` `1`, `2`, and `6` relations are blockers, not current
  route ids;
- Q3668 rewards are the current 16042 rows `2190`, `2191`, and `4768`;
- Q3668 source reward `5114`/game reward `4769` and quest `5864`
  `game_reward_id=3668` remain provenance-only or blocked tuple evidence;
- Q3668 stays `not_retail_complete` until live/client evidence covers dialog,
  objective timing, reward UI/inventory persistence, achievements/progression,
  pushed item `6912`, prerequisite chain behavior, and end-to-end smoke.

## Slice 10 - Public Event Vote And Scoreboard Aux

Question: does build 16029 expose extra opcode-immediate, sender, apply, or
producer anchors for public-event aux/vote/scoreboard opcodes `0x0139`,
`0x06F7`, or `0x06FA`?

Disposition: rejected as an old-client accelerator. Builds 16029 and 16028 have
the same five focused immediate hits and the same normalized instruction-shape
multiset as current 16042. They do not reveal a new 16042 target for vote
lifecycle timing, scoreboard population, reward/result ordering, or
default-choice proof.

### Commands

16029 helper pass:

```powershell
.\Decomp\Analysis\run_ghidra_analysis.ps1 `
  -ClientDir artifacts\old-client-exe-sweep\16029 `
  -OutputDir artifacts\old-client-exe-sweep\16029_exports `
  -ProjectDir artifacts\old-client-exe-sweep\ghidra_projects `
  -Targets WildStar64.exe `
  -ProjectLayout PerTarget `
  -AnalysisMode Skip `
  -NoApplyLabels `
  -SkipDefaultExport `
  -SkipCoverage `
  -ExtraPostScript FindOpcodeComparisons.java `
  -ExtraPostScriptArgs @('0x0139','0x06F7','0x06FA','0x100') `
  -GhidraMaxHeap 4G
```

16042 authority comparison used the same focused `FindOpcodeComparisons.java`
arguments against `Decomp/Client64` with `-AnalysisMode Skip`.

16028 cross-neighbor comparison used the same focused arguments with
`-ClientDir artifacts\old-client-exe-sweep\16028` and
`-ProjectDir artifacts\old-client-exe-sweep\ghidra_projects_16028`. This was
the first 16028 Ghidra import for the sweep.

Normalized instruction-shape comparison:

```powershell
function Get-InsnCounts($path) {
  Get-Content $path |
    Where-Object { $_ -match 'match=' } |
    ForEach-Object {
      if ($_ -match 'insn=(.*) \(GhidraScript\)') { $matches[1] }
    } |
    Group-Object |
    Sort-Object Name |
    ForEach-Object { '{0} x{1}' -f $_.Name, $_.Count }
}

$old = Get-InsnCounts artifacts\old-client-exe-sweep\16029_public_event_vote_scoreboard_opcode_matches.txt
$cur = Get-InsnCounts artifacts\old-client-exe-sweep\16042_public_event_vote_scoreboard_opcode_matches.txt
Compare-Object -ReferenceObject $cur -DifferenceObject $old
```

The final `Compare-Object` produced no output for 16029 vs 16042
(`old_shapes=4`, `cur_shapes=4`) or 16028 vs 16042 (`old16028_shapes=4`,
`cur16042_shapes=4`).

### Evidence

Builds 16029, 16028, and 16042 all report `total_matches=5` for the focused
opcode set.

| Opcode | Representative immediate hit | 16042 evidence | 16029 result |
| --- | --- | --- | --- |
| `0x0139` | `14006cd05`; shared non-registration state constant at `1403aff80` | `ServerPublicEventAux_ReadPayload @14007b930` reads one `uint32`, a 5-bit count, and counted `uint32` values; producer/UI semantics remain blocked | Same registration immediate in `FUN_14006c290`; same non-registration `0x139` state constant in both 16029 and 16028 |
| `0x06F7` | `14006cebe` | `ServerPublicEventVoteAux_ReadPayload @14007c0c0` reads a 15-bit value plus one flag; vote lifecycle semantics remain blocked | Same registration immediate in `FUN_14006c290` in both 16029 and 16028 |
| `0x06FA` | `14006d0b4`; sender-like hit `14068969a` | `ClientPublicEventRequestScoreboard_WritePayload @14007c620`; `Lua_PublicEvent_RequestScoreboard @140689580` calls the scoreboard request path | Same registration immediate and sender-like shape, old projects unlabeled (`1406896ba` due function-body alignment) |

The shared non-registration `0x0139` hit is not a packet target by itself. The
current 16042 cache for `FUN_1403afb10 @1403afb10` shows it as one case in a
large state/notification constant selection (`uVar17 = 0x139`) rather than a
send-site or reader. The old binary exposes the same instruction shape and does
not sharpen it into a safe public-event producer.

Existing current evidence already records the packet boundary:

- `INITIAL_FINDINGS.md` maps `0x0139`, `0x06F7`, and `0x06FA` packet shapes and
  notes that reward tier/type, thresholds, delivery rules, eligibility, and
  result ordering remain blocked.
- `MISSING_FEATURE_MATRIX.md` records LWS-071/LWS-072 as packet-shape progress
  with public-event vote/scoreboard lifecycle, reward delivery, result ordering,
  and tier semantics still blocked pending packet/UI sequence and client proof.

Raw local outputs:

- `artifacts/old-client-exe-sweep/16029_public_event_vote_scoreboard_opcode_matches.txt`
- `artifacts/old-client-exe-sweep/16028_public_event_vote_scoreboard_opcode_matches.txt`
- `artifacts/old-client-exe-sweep/16042_public_event_vote_scoreboard_opcode_matches.txt`
- `Decomp/Analysis/exports/WildStar64.exe/selected_decompiled_cache/functions/sha256_231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5/1403afb10.fragment.c`

### Stop State

Do not add labels or runtime behavior from this old-client comparison. The
authoritative 16042 state remains:

- `0x0139`, `0x06F7`, and `0x06FA` packet shapes are mapped;
- current public-event vote/scoreboard source and tests may cover the existing
  conservative packet/model paths, but retail lifecycle behavior remains
  blocked;
- vote UI sequence, timeout cadence, mismatch/default-choice behavior,
  scoreboard population, reward delivery, result ordering, and reward tier
  semantics still require accepted live-client packet/UI evidence or a stronger
  16042 producer/apply proof.

## Slice 11 - Instance Settings And Reset Server Aux

Question: does build 16029 expose extra opcode-immediate, apply, or producer
anchors for server-side instance settings/reset opcodes `0x00F1`
(`ServerInstanceSettings`) or `0x0157` (`ServerInstanceResetAux`)?

Disposition: mapped/blocked unchanged. Build 16029 has the same registration
immediates, the same two non-registration immediate shapes, and the same
normalized instruction-shape multiset as current 16042. It does not reveal a
new target for instance-reset result semantics, settings emit ownership, or
client-visible instance-settings timing.

### Commands

16029 helper pass:

```powershell
.\Decomp\Analysis\run_ghidra_analysis.ps1 `
  -ClientDir artifacts\old-client-exe-sweep\16029 `
  -OutputDir artifacts\old-client-exe-sweep\16029_exports `
  -ProjectDir artifacts\old-client-exe-sweep\ghidra_projects `
  -Targets WildStar64.exe `
  -ProjectLayout PerTarget `
  -AnalysisMode Skip `
  -NoApplyLabels `
  -SkipDefaultExport `
  -SkipCoverage `
  -ExtraPostScript FindOpcodeComparisons.java `
  -ExtraPostScriptArgs @('0x00F1','0x0157','0x100') `
  -GhidraMaxHeap 4G
```

16042 authority comparison used the same focused `FindOpcodeComparisons.java`
arguments against `Decomp/Client64` with `-AnalysisMode Skip`.

Normalized instruction-shape comparison:

```powershell
function Get-InsnCounts($path) {
  Get-Content $path |
    Where-Object { $_ -match 'match=' } |
    ForEach-Object {
      if ($_ -match 'insn=(.*) \(GhidraScript\)') { $matches[1] }
    } |
    Group-Object |
    Sort-Object Name |
    ForEach-Object { '{0} x{1}' -f $_.Name, $_.Count }
}

$old = Get-InsnCounts artifacts\old-client-exe-sweep\16029_instance_server_opcode_matches.txt
$cur = Get-InsnCounts artifacts\old-client-exe-sweep\16042_instance_server_opcode_matches.txt
Compare-Object -ReferenceObject $cur -DifferenceObject $old
```

The final `Compare-Object` produced no output (`old_shapes=15`,
`cur_shapes=15`).

### Evidence

Both 16029 and 16042 report `total_matches=28` for the focused opcode set.

| Opcode | Representative immediate hit | 16042 evidence | 16029 result |
| --- | --- | --- | --- |
| `0x00F1` | `140078d86`; shared non-registration `ADD EAX,0xf1` at `1403b3136` | `ServerInstanceSettings_ReadPayload @1400a37d0`; `InstanceSettings_ApplyServerSettings @1403b67e0` caches difficulty, prime level, flags, and update interval | Same registration immediate in `FUN_14006c290`; same non-registration shape |
| `0x0157` | `14007217d`; shared non-registration `MOV EDX,0x157` at `1403b7b72` | `ServerInstanceResetAux_ReadPayload @14008e030` reads one 4-bit field and one `uint32`; `Inventory_AddOrUpdateFromSharedPayload @1403b77d0` explains the non-registration `0x157` false lead | Same registration immediate in `FUN_14006c290`; same non-registration shape, old project unlabeled |

Current 16042 already maps the related client request side through
`Lua_GameLib_SetInstanceSettings @140701650` (`0x0163`),
`Lua_GameLib_ResetSingleInstance @1407017d0` (`0x0153`), and
`Lua_GameLib_OnClosedInstanceSettings @140701800` (`0x00D2`). The focused old
server-opcode comparison does not alter those labels or justify widening
runtime reset/settings behavior.

Raw local outputs:

- `artifacts/old-client-exe-sweep/16029_instance_server_opcode_matches.txt`
- `artifacts/old-client-exe-sweep/16042_instance_server_opcode_matches.txt`

### Stop State

Do not add labels or runtime behavior from this old-client comparison. The
authoritative 16042 state remains:

- `ServerInstanceSettings` (`0x00F1`) and `ServerInstanceResetAux` (`0x0157`)
  packet shapes are mapped;
- instance-settings client request labels for `0x0163`, `0x0153`, and `0x00D2`
  remain the current request-side evidence;
- reset result semantics, settings ownership, retail timing, UI presentation,
  and any broader instance producer behavior remain blocked pending current
  16042 producer/apply proof or accepted live-client/server evidence.

## Slice 12 - Release 1.0.7 Public Event Scoreboard Drift

Question: can a much older release-era client, especially release 1.0.7, expose
additional public-event vote/scoreboard evidence that the near-neighbor 1.7
clients did not expose?

Disposition: useful as stale-version chronology, rejected as current runtime
evidence. Live build 1.0.7.6682 still has the `RequestScoreboard` Lua method,
but its scoreboard request send path uses old opcode `0x0578`. Current 16042
uses `0x06FA` for the same Lua method and has the durable 16042 labels. The
old opcode helps explain protocol drift, but it must not be promoted into
current labels, packet behavior, or completion claims.

### Archive Inventory

The local archive source contains the requested older families:

- Live 1.0.7 candidates: `WildStarLive_1.0.7.6658.7z`,
  `WildStarLive_1.0.7.6670.7z`, `WildStarLive_1.0.7.6677.7z`,
  and `WildStarLive_1.0.7.6682.7z`.
- `WildStarLive_1.0.7.6670.7z` has a suspicious nested layout with
  `1.0.9.6760*` executable folders, so it was not selected as the first clean
  1.0.7 probe.
- Alpha candidates exist at `0.4.6.5906` and `0.4.7.6072`; both expose
  `Client_x64.exe` and `Client_x86.exe` rather than the later
  `WildStar64.exe` naming.
- Beta candidates include `0.5.x`, `1.0.0.x`, and `1.0.3.x` builds; the
  checked `WildStarBeta_1.0.3.6576.7z` archive contains
  `Client64\WildStar64.exe`.

The selected clean release-era x64 executable was:

```powershell
& "C:\Program Files\7-Zip\7z.exe" e `
  I:\WildStar\ClientArchives\ArctiumClientData\Binaries\Live\WildStarLive_1.0.7.6682.7z `
  WildStar64.exe `
  -oartifacts\old-client-exe-sweep\1.0.7.6682 `
  -y

(Get-FileHash artifacts\old-client-exe-sweep\1.0.7.6682\WildStar64.exe `
  -Algorithm SHA256).Hash
```

SHA256: `320597EE0A6A81766E6608E28F5DE6634C78E3AD28EC977B9FDFC3B7A0617D3D`.

### Commands

Focused old release pass for current public-event aux/vote/scoreboard opcodes:

```powershell
.\Decomp\Analysis\run_ghidra_analysis.ps1 `
  -ClientDir artifacts\old-client-exe-sweep\1.0.7.6682 `
  -OutputDir artifacts\old-client-exe-sweep\1.0.7.6682_exports `
  -ProjectDir artifacts\old-client-exe-sweep\ghidra_projects_1.0.7.6682 `
  -Targets WildStar64.exe `
  -ProjectLayout PerTarget `
  -NoApplyLabels `
  -SkipDefaultExport `
  -SkipCoverage `
  -ExtraPostScript FindOpcodeComparisons.java `
  -ExtraPostScriptArgs @('0x0139','0x06F7','0x06FA','0x100') `
  -GhidraMaxHeap 4G
```

String and Lua-table probes for the old `RequestScoreboard` method:

```powershell
.\Decomp\Analysis\run_ghidra_analysis.ps1 `
  -ClientDir artifacts\old-client-exe-sweep\1.0.7.6682 `
  -OutputDir artifacts\old-client-exe-sweep\1.0.7.6682_exports `
  -ProjectDir artifacts\old-client-exe-sweep\ghidra_projects_1.0.7.6682 `
  -Targets WildStar64.exe `
  -ProjectLayout PerTarget `
  -NoApplyLabels `
  -SkipDefaultExport `
  -SkipCoverage `
  -ExtraPostScript TraceStringReferences.java `
  -ExtraPostScriptArgs @('RequestScoreboard','3') `
  -GhidraMaxHeap 4G

.\Decomp\Analysis\run_ghidra_analysis.ps1 `
  -ClientDir artifacts\old-client-exe-sweep\1.0.7.6682 `
  -OutputDir artifacts\old-client-exe-sweep\1.0.7.6682_exports `
  -ProjectDir artifacts\old-client-exe-sweep\ghidra_projects_1.0.7.6682 `
  -Targets WildStar64.exe `
  -ProjectLayout PerTarget `
  -NoApplyLabels `
  -SkipDefaultExport `
  -SkipCoverage `
  -ExtraPostScript DumpNearbyData.java `
  -ExtraPostScriptArgs @('140a7c9f0','8') `
  -GhidraMaxHeap 4G
```

Old opcode drift confirmation:

```powershell
.\Decomp\Analysis\run_ghidra_analysis.ps1 `
  -ClientDir artifacts\old-client-exe-sweep\1.0.7.6682 `
  -OutputDir artifacts\old-client-exe-sweep\1.0.7.6682_exports `
  -ProjectDir artifacts\old-client-exe-sweep\ghidra_projects_1.0.7.6682 `
  -Targets WildStar64.exe `
  -ProjectLayout PerTarget `
  -NoApplyLabels `
  -SkipDefaultExport `
  -SkipCoverage `
  -ExtraPostScript FindOpcodeComparisons.java `
  -ExtraPostScriptArgs @('0x0578','0x100') `
  -GhidraMaxHeap 4G
```

The same `0x0578` helper pass was then run against `Decomp\Client64` to verify
that `0x0578` is not the current 16042 scoreboard request opcode.

### Evidence

The current-opcode scan against 1.0.7.6682 reports `total_matches=5`, but all
matches are for `0x0139`. There are no `0x06F7` or `0x06FA` immediate hits in
that focused pass.

| Build | Current opcode scan result |
| --- | --- |
| 16042 | `MOV EDX,0x139` registration, `MOV EDX,0x6f7` registration, `MOV EDX,0x6fa` registration, shared `MOV EDI,0x139`, and `Lua_PublicEvent_RequestScoreboard @140689580` sender-like `MOV EDX,0x6fa` |
| 1.0.7.6682 | `0x0139`-only hits, including `FUN_140058130 @140058130`, `FUN_14036d560 @14036d560`, and `FUN_140434050 @140434050`; no `0x06F7` or `0x06FA` hits |

The raw binary still contains `RequestScoreboard`, `Scoreboard`, and
`PublicEvent` strings. Ghidra string tracing resolved `RequestScoreboard` at
`14097c4a0`, with a table cell at `140a7c9f0`. The adjacent Lua registration
table cell at `140a7c9f8` points to `FUN_140502280 @140502280`, making it the
1.0.7 `RequestScoreboard` callback candidate.

Inspecting only the call target and opcode constant, not copying decompiled
source, showed the protocol drift:

| Path | 1.0.7.6682 | 16042 |
| --- | --- | --- |
| Lua method | `RequestScoreboard` table entry at `140a7c9f0`/`140a7c9f8` -> `FUN_140502280 @140502280` | `Lua_PublicEvent_RequestScoreboard @140689580` |
| Scoreboard request send opcode | `MOV EDX,0x578` at `14050239a` inside `FUN_140502280` | `MOV EDX,0x6fa` at `14068969a` inside `Lua_PublicEvent_RequestScoreboard` |
| Server/current-opcode registration evidence | `FUN_140058130 @140058130` includes `MOV EDX,0x578`; current `0x06F7` and `0x06FA` are absent from the focused old scan | `Network_RegisterServerOpcode_0351 @14006c290` includes current `0x0139`, `0x06F7`, and `0x06FA` registration immediates |

A focused `0x0578` scan in 16042 produces many noisy structure-offset matches
(`total_matches=170`), but no current `Lua_PublicEvent_RequestScoreboard` or
`Network_RegisterServerOpcode_0351` match. That confirms `0x0578` is old
release-era scoreboard-request evidence, not a current 16042 opcode.

Raw local outputs:

- `artifacts/old-client-exe-sweep/1.0.7.6682_public_event_vote_scoreboard_opcode_matches.txt`
- `artifacts/old-client-exe-sweep/1.0.7.6682_request_scoreboard_trace.txt`
- `artifacts/old-client-exe-sweep/1.0.7.6682_request_scoreboard_nearby_data.txt`
- `artifacts/old-client-exe-sweep/1.0.7.6682_request_scoreboard_inspect_filtered.txt`
- `artifacts/old-client-exe-sweep/1.0.7.6682_scoreboard_old_opcode_0578_matches.txt`
- `artifacts/old-client-exe-sweep/16042_request_scoreboard_inspect_filtered.txt`
- `artifacts/old-client-exe-sweep/16042_old_scoreboard_opcode_0578_matches.txt`

### Stop State

Record `0x0578` as old-only/stale public-event scoreboard request evidence for
release 1.0.7.6682. Do not add current labels or runtime behavior from this
old opcode. Current 16042 remains authoritative:

- `0x06FA` is the current scoreboard request opcode for
  `Lua_PublicEvent_RequestScoreboard @140689580`;
- `0x0139` and `0x06F7` current server packet shapes remain mapped;
- public-event vote UI sequence, timeout cadence, scoreboard population, reward
  delivery, result ordering, and reward tier semantics remain blocked pending
  current 16042 producer/apply proof or accepted live-client evidence;
- alpha and beta clients are available for future stale-history probes, but
  any result from them must be revalidated against current 16042 before labels,
  runtime behavior, or completion status changes.

## Slice 13 - Alpha/Beta Public Event Scoreboard String Gate

Question: do available alpha or beta x64 clients expose the public-event
scoreboard surface early enough to provide extra context beyond release
1.0.7.6682?

Disposition: rejected for the current scoreboard target without a Ghidra pass.
The checked late beta and alpha binaries both contain public-event and
vote-related strings, but neither contains `RequestScoreboard` or `Scoreboard`.
That makes them poor candidates for the current public-event scoreboard request
slice. They may still be useful for a separate, explicitly vote-focused stale
history pass, but not for promoting current 16042 scoreboard behavior.

### Inputs

Selected candidates:

- `I:\WildStar\ClientArchives\ArctiumClientData\Binaries\Beta\WildStarBeta_1.0.3.6576.7z`
  -> `Client64\WildStar64.exe`
- `I:\WildStar\ClientArchives\ArctiumClientData\Binaries\Alpha\WildStarAlpha_0.4.7.6072.7z`
  -> `Client_x64.exe`

Extracted binaries:

| Build | Path | SHA256 |
| --- | --- | --- |
| 1.0.3.6576 Beta | `artifacts/old-client-exe-sweep/beta_1.0.3.6576/WildStar64.exe` | `0B052733970C16C92F241B39F44CC3ECB33B0B7B5CADA5F85787CE5EACFD6624` |
| 0.4.7.6072 Alpha | `artifacts/old-client-exe-sweep/alpha_0.4.7.6072/Client_x64.exe` | `36C60E5CED69C8EAC10384FF715830FA5B2DD7E1EA558FC3EAAFA555997C1CA7` |

### Evidence

ASCII string-gate output:

| Build | `RequestScoreboard` | `Scoreboard` | `PublicEvent` | Vote-related public-event strings |
| --- | --- | --- | --- | --- |
| 1.0.3.6576 Beta | absent | absent | present | `PublicEventInitiateVote`, `InitiateVote`, `Vote`, `PublicEventVote`, `PublicEventEnd`, `PublicEventCleared` present |
| 0.4.7.6072 Alpha | absent | absent | present | `PublicEventInitiateVote`, `InitiateVote`, `Vote`, `PublicEventVote`, and `PublicEventEnd` present; `PublicEventCleared` absent |

Raw local output:

- `artifacts/old-client-exe-sweep/beta_alpha_public_event_string_gate.txt`

### Stop State

Do not spend more decompile time on alpha/beta for the scoreboard-request
target unless a new evidence question needs their public-event vote history.
The current scoreboard evidence remains Slice 12 plus current 16042:

- release 1.0.7.6682 shows old-only scoreboard request opcode `0x0578`;
- current 16042 scoreboard request remains `0x06FA` at
  `Lua_PublicEvent_RequestScoreboard @140689580`;
- alpha/beta public-event vote strings are only stale-history lead material and
  do not justify runtime behavior, labels, or completion-status changes.

## Slice 14 - Public Event Objective/Reward Registration Drift

Question: do older clients expose extra anchors or opcode drift for the current
public-event objective/reward/status cluster: `0x0130`, `0x0132`, `0x0133`,
`0x0134`, `0x013B`, `0x00D6`, `0x06F9`, and `0x0112`?

Disposition: near-neighbor builds are unchanged; release 1.0.7.6682 is stale
for current `0x06F9`. Builds 16029 and 16028 have the same registration
immediates as current 16042 in the focused registration/reader window. Live
1.0.7.6682 preserves the current public-event start/objective update/stats/end
opcodes except `0x06F9`, which is absent from the focused current-opcode scan.
The old release still contains public-event objective update and notification
strings, but it does not expose an `ObjectiveStart` string and does not justify
changing current 16042 labels or runtime behavior.

### Commands

A first whole-text helper pass was rejected as too noisy because low values such
as `0x0130` and `0x0134` are common structure offsets. The durable comparison
uses the narrower registration/reader neighborhood:

```powershell
.\Decomp\Analysis\run_ghidra_analysis.ps1 `
  -ClientDir Decomp\Client64 `
  -OutputDir Decomp\Analysis\exports `
  -ProjectDir Decomp\Analysis\ghidra_projects `
  -Targets WildStar64.exe `
  -ProjectLayout PerTarget `
  -AnalysisMode Skip `
  -NoApplyLabels `
  -SkipDefaultExport `
  -SkipCoverage `
  -ExtraPostScript FindOpcodeComparisons.java `
  -ExtraPostScriptArgs @('0x0130','0x0132','0x0133','0x0134','0x013B','0x00D6','0x06F9','0x0112','0x140050000','0x140080000','0x200') `
  -GhidraMaxHeap 4G
```

The same focused command was run for:

- `artifacts\old-client-exe-sweep\16029\WildStar64.exe`
- `artifacts\old-client-exe-sweep\16028\WildStar64.exe`
- `artifacts\old-client-exe-sweep\1.0.7.6682\WildStar64.exe`

### Evidence

Registration-immediate summary:

| Build | Registration immediates found | Missing vs 16042 |
| --- | --- | --- |
| 16042 | `0x0112`, `0x0130`, `0x0132`, `0x0133`, `0x0134`, `0x013B`, `0x06F9`, `0x00D6` | none |
| 16029 | `0x0112`, `0x0130`, `0x0132`, `0x0133`, `0x0134`, `0x013B`, `0x06F9`, `0x00D6` | none |
| 16028 | `0x0112`, `0x0130`, `0x0132`, `0x0133`, `0x0134`, `0x013B`, `0x06F9`, `0x00D6` | none |
| 1.0.7.6682 | `0x0112`, `0x0130`, `0x0132`, `0x0133`, `0x0134`, `0x013B`, `0x00D6` | `0x06F9` |

Representative current 16042 registration hits are all inside
`Network_RegisterServerOpcode_0351 @14006c290`:

- `0x0134` at `14006cbae`
- `0x0133` at `14006cbdf`
- `0x0132` at `14006cc10`
- `0x0112` at `14006cc35`
- `0x013B` at `14006cd67`
- `0x00D6` at `14006cd98`
- `0x0130` at `14006cdc2`
- `0x06F9` at `14006d046`

Builds 16029 and 16028 show the same addresses and instruction shapes, with
the registration function still unlabeled as `FUN_14006c290`.

Release 1.0.7.6682 maps the older registration function at
`FUN_140058130 @140058130`. Its focused current-opcode hits include:

- `0x013B` at `14005b044`
- `0x0134` at `14005b070`
- `0x0112` at `14005b18d`
- `0x0130` at `14005bcbc`
- `0x0133` at `14005bd18`
- `0x0132` at `14005c2b9`
- `0x00D6` at `14005c87c`

The 1.0.7 string gate found `PublicEventObjectiveUpdate` and
`PublicEventObjectiveNotificationMode` strings, but did not find
`PublicEventObjectiveStart`, `ObjectiveStart`, or
`PublicEventObjectiveJoinedMessage`.

Current 16042 labels already contain the relevant packet readers and apply
anchors, including:

- `ServerPublicEventStatsUpdate_ReadPayload @14007bde0` for `0x0130`
- `ServerPublicEventEnd_ReadPayload @14007bb80` for `0x00D6`
- `ServerPublicEventPersonalStatUpdate_ReadPayload @14007bb10` for `0x013B`
- `ServerPublicEventObjectiveNotificationMode_ReadPayload @14007a3a0` for
  `0x0133`
- `ServerPublicEventObjectiveStatusUpdate_ReadPayload @14007b3c0` for
  `0x0134`
- `ServerPublicEventObjectiveUpdate_ReadPayload @14007b490` for `0x0132`
- `ServerPublicEventStart_ReadPayload @14007b620` for `0x0112`
- `PublicEventObjectiveUpdate_ApplyParsedPayload @1405f3520` for the current
  objective-update apply path

Raw local outputs:

- `artifacts/old-client-exe-sweep/16042_public_event_objective_reward_regwindow_matches.txt`
- `artifacts/old-client-exe-sweep/16029_public_event_objective_reward_regwindow_matches.txt`
- `artifacts/old-client-exe-sweep/16028_public_event_objective_reward_regwindow_matches.txt`
- `artifacts/old-client-exe-sweep/1.0.7.6682_public_event_objective_reward_regwindow_matches.txt`
- `artifacts/old-client-exe-sweep/1.0.7.6682_public_event_objective_string_gate.txt`
- `artifacts/old-client-exe-sweep/public_event_objective_reward_registration_summary.txt`

### Stop State

Do not add labels or runtime behavior from this comparison. The current 16042
state remains:

- public-event objective/reward/status packet readers are mapped at the labels
  listed above;
- near-neighbor 16028/16029 evidence does not sharpen the current 16042 target;
- release 1.0.7.6682 is old-only/stale for `0x06F9`;
- exact objective/update text, phase/completion notification audience and
  ordering, reward delivery, scoreboard population, result ordering, reward tier
  semantics, and quest-share behavior remain blocked pending current 16042
  producer/apply proof, packet/UI sequence evidence, or accepted live-client
  evidence.

## Slice 15 - Instance Request Producer Opcode Drift

Question: do older clients expose extra or contradictory evidence for the
current instance request producer Lua methods:
`GameLib.SetInstanceSettings`, `GameLib.ResetSingleInstance`, and
`GameLib.OnClosedInstanceSettings`?

Disposition: near-neighbor builds are unchanged; release 1.0.7.6682 is stale
for current instance request opcodes. Builds 16029 and 16028 retain the same
producer-window opcode constants as current 16042. Live 1.0.7.6682 still has
the same Lua method strings, but the paired callbacks send older opcode
constants `0x011B`, `0x010C`, and `0x009B` instead of current `0x0163`,
`0x0153`, and `0x00D2`. This is useful protocol-drift evidence only and does
not justify changing current labels, packet models, or runtime handlers.

### Commands

ASCII string gate:

```powershell
$terms = @(
  'SetInstanceSettings',
  'ResetSingleInstance',
  'OnClosedInstanceSettings',
  'InstanceSettings',
  'ResetInstance',
  'ClosedInstance'
)
```

The string gate was run against current 16042, 16029, 16028, and 1.0.7.6682.
All four binaries contain `SetInstanceSettings`, `ResetSingleInstance`,
`OnClosedInstanceSettings`, `InstanceSettings`, and `ClosedInstance`;
`ResetInstance` is absent in all four.

Focused current/near-neighbor producer window:

```powershell
.\Decomp\Analysis\run_ghidra_analysis.ps1 `
  -ClientDir Decomp\Client64 `
  -OutputDir artifacts\old-client-exe-sweep\instance_request_tmp_exports `
  -ProjectDir Decomp\Analysis\ghidra_projects `
  -Targets WildStar64.exe `
  -ProjectLayout PerTarget `
  -AnalysisMode Skip `
  -NoApplyLabels `
  -SkipDefaultExport `
  -SkipCoverage `
  -ExtraPostScript FindOpcodeComparisons.java `
  -ExtraPostScriptArgs @('0x0163','0x0153','0x00D2','0x140700000','0x140702000','0x80') `
  -GhidraMaxHeap 4G
```

The same command was run for 16029 and 16028.

Focused 1.0.7 callback discovery used `TraceStringReferences.java` and
`DumpNearbyData.java` against the `SetInstanceSettings` table cell
`1409bb8d0`. The paired callbacks are:

| 1.0.7 Lua table cell | Method | Callback |
| --- | --- | --- |
| `1409bb8d0` -> `140994530` | `SetInstanceSettings` | `FUN_1405fc750 @1405fc750` |
| `1409bb8e0` -> `140994548` | `ResetSingleInstance` | `FUN_1405fc890 @1405fc890` |
| `1409bb8f0` -> `140994490` | `OnClosedInstanceSettings` | `FUN_1405fc8c0 @1405fc8c0` |

Focused 1.0.7 opcode windows:

```powershell
.\Decomp\Analysis\run_ghidra_analysis.ps1 `
  -ClientDir artifacts\old-client-exe-sweep\1.0.7.6682 `
  -OutputDir artifacts\old-client-exe-sweep\1.0.7.6682_exports `
  -ProjectDir artifacts\old-client-exe-sweep\ghidra_projects_1.0.7.6682 `
  -Targets WildStar64.exe `
  -ProjectLayout PerTarget `
  -AnalysisMode Skip `
  -NoApplyLabels `
  -SkipDefaultExport `
  -SkipCoverage `
  -ExtraPostScript FindOpcodeComparisons.java `
  -ExtraPostScriptArgs @('0x0163','0x0153','0x00D2','0x1405fc700','0x1405fc920','0x80') `
  -GhidraMaxHeap 4G

.\Decomp\Analysis\run_ghidra_analysis.ps1 `
  -ClientDir artifacts\old-client-exe-sweep\1.0.7.6682 `
  -OutputDir artifacts\old-client-exe-sweep\1.0.7.6682_exports `
  -ProjectDir artifacts\old-client-exe-sweep\ghidra_projects_1.0.7.6682 `
  -Targets WildStar64.exe `
  -ProjectLayout PerTarget `
  -AnalysisMode Skip `
  -NoApplyLabels `
  -SkipDefaultExport `
  -SkipCoverage `
  -ExtraPostScript FindOpcodeComparisons.java `
  -ExtraPostScriptArgs @('0x011B','0x010C','0x009B','0x1405fc700','0x1405fc920','0x80') `
  -GhidraMaxHeap 4G
```

### Evidence

Current/near-neighbor producer-window results:

| Build | `SetInstanceSettings` | `ResetSingleInstance` | `OnClosedInstanceSettings` |
| --- | --- | --- | --- |
| 16042 | `Lua_GameLib_SetInstanceSettings @140701650` sends `0x0163` at `140701792` | `Lua_GameLib_ResetSingleInstance @1407017d0` sends `0x0153` at `1407017e0` | `Lua_GameLib_OnClosedInstanceSettings @140701800` sends `0x00D2` at `140701856` |
| 16029 | `FUN_140701670 @140701670` sends `0x0163` at `1407017b2` | `FUN_1407017f0 @1407017f0` sends `0x0153` at `140701800` | `FUN_140701820 @140701820` sends `0x00D2` at `140701876` |
| 16028 | `FUN_140701670 @140701670` sends `0x0163` at `1407017b2` | `FUN_1407017f0 @1407017f0` sends `0x0153` at `140701800` | `FUN_140701820 @140701820` sends `0x00D2` at `140701876` |

Release 1.0.7.6682 callback-window results:

| Method | Current opcode absent? | Old opcode present |
| --- | --- | --- |
| `SetInstanceSettings` | no `0x0163` hit in `1405fc700..1405fc920` | `0x011B` at `1405fc848` |
| `ResetSingleInstance` | no `0x0153` hit in `1405fc700..1405fc920` | `0x010C` at `1405fc8a0` |
| `OnClosedInstanceSettings` | no `0x00D2` hit in `1405fc700..1405fc920` | `0x009B` at `1405fc8d0` |

Raw local outputs:

- `artifacts/old-client-exe-sweep/instance_request_string_gate.txt`
- `artifacts/old-client-exe-sweep/16042_instance_request_producer_window_matches.txt`
- `artifacts/old-client-exe-sweep/16029_instance_request_producer_window_matches.txt`
- `artifacts/old-client-exe-sweep/16028_instance_request_producer_window_matches.txt`
- `artifacts/old-client-exe-sweep/1.0.7.6682_instance_SetInstanceSettings_trace.txt`
- `artifacts/old-client-exe-sweep/1.0.7.6682_instance_ResetSingleInstance_trace.txt`
- `artifacts/old-client-exe-sweep/1.0.7.6682_instance_OnClosedInstanceSettings_trace.txt`
- `artifacts/old-client-exe-sweep/1.0.7.6682_instance_request_nearby_data.txt`
- `artifacts/old-client-exe-sweep/1.0.7.6682_instance_current_opcode_window_matches.txt`
- `artifacts/old-client-exe-sweep/1.0.7.6682_instance_old_opcode_window_matches.txt`

### Stop State

Do not add labels or runtime behavior from 1.0.7.6682. The current 16042 state
remains:

- `Lua_GameLib_SetInstanceSettings @140701650` sends current opcode `0x0163`;
- `Lua_GameLib_ResetSingleInstance @1407017d0` sends current opcode `0x0153`;
- `Lua_GameLib_OnClosedInstanceSettings @140701800` sends current opcode
  `0x00D2`;
- server-side `ServerInstanceSettings` (`0x00F1`) and
  `ServerInstanceResetAux` (`0x0157`) remain the mapped server responses from
  Slice 11;
- reset result semantics, settings ownership, retail timing, UI presentation,
  and broader instance producer behavior remain blocked pending current 16042
  apply/producer proof, packet sequence evidence, or accepted live-client
  evidence.

## Slice 16 - Named Near-Neighbor Matching/Raid Stability

Question: do the remaining named near-neighbor/PTR builds 16005, 15996, 15884,
or 15866 expose any additional matching/raid queue opcode anchors beyond the
16029 comparison in Slice 5?

Disposition: rejected as an accelerator. Builds 16005, 15996, 15884 PTR, and
15866 PTR have the same focused matching/raid opcode-immediate shape set as
current 16042 and build 16029. They do not reveal an extra queue-state
consumer, non-zero raid queue timing clue, `0x05CF` apply target, or diagnostic
sender intent.

### Commands

The remaining named near-neighbor/PTR passes used the same compact matching/raid
opcode cluster as Slice 5:

```powershell
.\Decomp\Analysis\run_ghidra_analysis.ps1 `
  -ClientDir artifacts\old-client-exe-sweep\16005 `
  -OutputDir artifacts\old-client-exe-sweep\16005_exports `
  -ProjectDir artifacts\old-client-exe-sweep\ghidra_projects_16005 `
  -Targets WildStar64.exe `
  -ProjectLayout PerTarget `
  -NoApplyLabels `
  -SkipDefaultExport `
  -SkipCoverage `
  -ExtraPostScript FindOpcodeComparisons.java `
  -ExtraPostScriptArgs @('0x05CF','0x0600','0x0718','0x071A','0x05D5','0x0602','0x062A','0x0634','0x100') `
  -GhidraMaxHeap 4G
```

The same command was run for:

- `artifacts\old-client-exe-sweep\15996\WildStar64.exe`
- `artifacts\old-client-exe-sweep\15884\WildStar64.exe`
- `artifacts\old-client-exe-sweep\15866\WildStar64.exe`

### Evidence

Normalized instruction-shape summary:

| Build | Total matches | Unique instruction shapes | Shape diff vs 16042 |
| --- | ---: | ---: | --- |
| 16042 | 145 | 107 | authority |
| 16029 | 145 | 107 | none |
| 16005 | 145 | 107 | none |
| 15996 | 145 | 107 | none |
| 15884 PTR | 145 | 107 | none |
| 15866 PTR | 145 | 107 | none |

The representative registration immediates remain the same as Slice 5:

- `0x0718` and `0x071A` server raid queue/raid info registrations at
  `1400717e2` and `14007180c`
- `0x05D5`, `0x0602`, `0x05CF`, and `0x0600` server registrations under
  `FUN_14006c290`
- `0x062A` and `0x0634` client diagnostic registrations under
  `FUN_1400a8190`

Raw local outputs:

- `artifacts/old-client-exe-sweep/16005_matching_raid_opcode_matches.txt`
- `artifacts/old-client-exe-sweep/15996_matching_raid_opcode_matches.txt`
- `artifacts/old-client-exe-sweep/15884_matching_raid_opcode_matches.txt`
- `artifacts/old-client-exe-sweep/15866_matching_raid_opcode_matches.txt`
- `artifacts/old-client-exe-sweep/matching_raid_live_neighbor_summary.txt`
- `artifacts/old-client-exe-sweep/matching_raid_all_named_neighbor_summary.txt`

### Stop State

Do not add labels or runtime behavior from this deeper-neighbor comparison. The
authoritative 16042 state remains the Slice 5 state:

- replacement-search `0x05D5`/`0x0602` sender labels are already mapped;
- diagnostic `0x062A`/`0x0634` stay log-only until sender intent or live UI
  context is proven;
- `0x05CF` remains mapped-only until apply-table index or live payload context
  proves field semantics;
- standalone non-zero `0x0718` raid queue timing remains blocked until a live
  non-zero payload or producer proof exists.

The named near-neighbor/PTR list from the goal prompt is now covered for this
matching/raid cluster. None of those builds exposed a new 16042 search target.

## Slice 17 - Named Near-Neighbor Housing/Marketplace/Map-Tracked Drift

Question: do the named near-neighbor/PTR builds expose extra anchors for
marketplace aux `0x06DF`/`0x07D5`, housing neighborhood `0x0501`/`0x0506`, or
map-tracked unit `0x0849`/`0x0848` beyond the current 16042 evidence?

Disposition: rejected as current implementation evidence. Marketplace and
housing opcode-immediate shapes are stable across the named builds. The
map-tracked cluster is stable through 16005. Older 15996, 15884 PTR, and 15866
PTR builds expose one extra old-only `0x0849` sender-like candidate, but that
shape is absent in current 16042 and 16005, so it is stale comparative
archaeology rather than a safe current producer proof.

### Commands

The focused comparison used the same opcode-immediate helper across current
16042 and named old builds:

```powershell
.\Decomp\Analysis\run_ghidra_analysis.ps1 `
  -ClientDir artifacts\old-client-exe-sweep\15996 `
  -OutputDir artifacts\old-client-exe-sweep\15996_exports `
  -ProjectDir artifacts\old-client-exe-sweep\ghidra_projects_15996 `
  -Targets WildStar64.exe `
  -ProjectLayout PerTarget `
  -AnalysisMode Skip `
  -NoApplyLabels `
  -SkipDefaultExport `
  -SkipCoverage `
  -ExtraPostScript FindOpcodeComparisons.java `
  -ExtraPostScriptArgs @('0x06DF','0x07D5','0x0501','0x0506','0x0849','0x0848','0x200') `
  -GhidraMaxHeap 4G
```

The same opcode set was run for:

- current `Decomp\Client64\WildStar64.exe`
- `artifacts\old-client-exe-sweep\16029\WildStar64.exe`
- `artifacts\old-client-exe-sweep\16028\WildStar64.exe`
- `artifacts\old-client-exe-sweep\16005\WildStar64.exe`
- `artifacts\old-client-exe-sweep\15996\WildStar64.exe`
- `artifacts\old-client-exe-sweep\15884\WildStar64.exe`
- `artifacts\old-client-exe-sweep\15866\WildStar64.exe`

### Evidence

Normalized instruction-shape summary:

| Build | Total matches | Unique instruction shapes | Shape diff vs 16042 |
| --- | ---: | ---: | --- |
| 16042 | 80 | 49 | authority |
| 16029 | 80 | 49 | none |
| 16028 | 80 | 49 | none |
| 16005 | 80 | 49 | none |
| 15996 | 81 | 49 | old-only `MOV EDX,0x849` candidate; `0x0506` global-store absolute address shift |
| 15884 PTR | 81 | 49 | old-only `MOV EDX,0x849` candidate; `0x0506` global-store absolute address shift |
| 15866 PTR | 81 | 49 | old-only `MOV EDX,0x849` candidate; `0x0506` global-store absolute address shift |

The `0x0506` difference is only the absolute address of the global store moving
between builds:

- current: `MOV dword ptr [0x140c89730],0x506`
- old 15996/15884/15866: `MOV dword ptr [0x140c87710],0x506`

The old-only map-tracked candidate is:

| Build | Old-only candidate |
| --- | --- |
| 15996 | `FUN_14059a730 @14059a730`, immediate at `14059aa45` |
| 15884 PTR | `FUN_14059a730 @14059a730`, immediate at `14059aa45` |
| 15866 PTR | `FUN_14059a610 @14059a610`, immediate at `14059a925` |

The inspected 15996 function reaches an old helper call with opcode `0x0849`
after several local condition checks. Current 16042's combined scan has only
the `0x0849` registration under `Network_RegisterServerOpcode_0351`, not this
sender-like function shape.

Current durable labels already cover the authoritative 16042 map-tracked
consumer/apply side:

- `ServerMapTrackedUnitUpdate_ReadPayload @1400a6c10`
- `MapTrackedUnitUpdate_ApplyAndDispatch @1403f4170`
- `MapTrackedUnitDisable_ApplyAndDispatch @1403f4200`
- `Network_SendOpcodePayloadHelper @1403f4900`
- `Network_SendOpcodePayloadOrPackedHelper @1403f4740`
- `Lua_GameLib_GetMapTrackedUnitData @140511c80`
- `MapTrackedUnitUpdate_ApplyDispatchSlot_DataRef @140e037ec`

Raw local outputs:

- `artifacts/old-client-exe-sweep/16042_housing_marketplace_maptracked_opcode_matches.txt`
- `artifacts/old-client-exe-sweep/16029_housing_marketplace_maptracked_opcode_matches.txt`
- `artifacts/old-client-exe-sweep/16028_housing_marketplace_maptracked_opcode_matches.txt`
- `artifacts/old-client-exe-sweep/16005_housing_marketplace_maptracked_opcode_matches.txt`
- `artifacts/old-client-exe-sweep/15996_housing_marketplace_maptracked_opcode_matches.txt`
- `artifacts/old-client-exe-sweep/15884_housing_marketplace_maptracked_opcode_matches.txt`
- `artifacts/old-client-exe-sweep/15866_housing_marketplace_maptracked_opcode_matches.txt`
- `artifacts/old-client-exe-sweep/housing_marketplace_maptracked_neighbor_summary.txt`
- `artifacts/old-client-exe-sweep/housing_marketplace_maptracked_neighbor_findings.txt`
- `artifacts/old-client-exe-sweep/15996_maptracked_extra_0849_inspect_filtered.txt`

### Stop State

Do not add labels or runtime behavior from the old-only map-tracked candidate.
The current 16042 state remains:

- marketplace aux fields `0x06DF` and `0x07D5` are non-emitted neutral fields
  from Slice 3;
- housing neighborhood list/cache apply is mapped, but the server-push owner is
  still blocked from Slice 4;
- map-tracked reader/apply/UI lookup is mapped from Slice 7, but producer
  timing, tracked-unit id allocation, disable lifetime, and `TrackingSlotId`
  selection remain blocked pending a current 16042 native send site, packet
  sequence evidence, or accepted live-client evidence.

## Slice 18 - Named Near-Neighbor Spell/Entity-Stat Aux Drift

Question: do the remaining named near-neighbor/PTR builds expose extra anchors
for the combined spell aux and entity-stat aux opcode cluster beyond the 16029
comparison in Slices 1 and 2?

Disposition: rejected as a current accelerator. Builds 16029, 16028, and 16005
have the same normalized instruction-shape set as current 16042 for the searched
spell/entity aux opcodes. Builds 15996, 15884 PTR, and 15866 PTR are older than
the current `0x08CC` server-opcode registration; they still contain unrelated
object-offset `0x8cc` references, but not the current registration immediate.
That identifies protocol drift, not a safe current implementation target.

### Commands

The combined pass searched the current spell/entity aux opcode set:

```powershell
.\Decomp\Analysis\run_ghidra_analysis.ps1 `
  -ClientDir artifacts\old-client-exe-sweep\16028 `
  -OutputDir artifacts\old-client-exe-sweep\16028_exports `
  -ProjectDir artifacts\old-client-exe-sweep\ghidra_projects_16028 `
  -Targets WildStar64.exe `
  -ProjectLayout PerTarget `
  -AnalysisMode Skip `
  -NoApplyLabels `
  -SkipDefaultExport `
  -SkipCoverage `
  -ExtraPostScript FindOpcodeComparisons.java `
  -ExtraPostScriptArgs @('0x0889','0x08CC','0x08F4','0x0939','0x093D','0x093E','0x080F','0x0810','0x0812','0x07FC','0x078C','0x200') `
  -GhidraMaxHeap 4G
```

The same opcode set was run for current 16042 and named builds 16029, 16005,
15996, 15884 PTR, and 15866 PTR. The 16029 per-build project was seeded once
before the helper pass because its project directory previously only contained
analysis manifests.

### Evidence

Normalized instruction-shape summary:

| Build | Total matches | Unique instruction shapes | Shape diff vs 16042 |
| --- | ---: | ---: | --- |
| 16042 | 125 | 81 | authority |
| 16029 | 125 | 81 | none |
| 16028 | 125 | 81 | none |
| 16005 | 125 | 81 | none |
| 15996 | 124 | 80 | missing current registration `MOV EDX,0x8cc` |
| 15884 PTR | 124 | 80 | missing current registration `MOV EDX,0x8cc` |
| 15866 PTR | 124 | 80 | missing current registration `MOV EDX,0x8cc` |

Registration-immediate check:

| Build range | Current aux registrations present |
| --- | --- |
| 16042, 16029, 16028, 16005 | `0x078C`, `0x07FC`, `0x080F`, `0x0810`, `0x0812`, `0x0889`, `0x08CC`, `0x08F4`, `0x0939`, `0x093D`, `0x093E` |
| 15996, 15884 PTR, 15866 PTR | all searched registrations except current `0x08CC` |

The older 15996/15884/15866 logs still contain non-registration `0x8cc` offset
matches such as `MOV dword ptr [RDI + 0x8cc],0x1` and related object-field
checks. Those are not server-opcode registration anchors and do not replace the
current 16042 `ServerUInt32WideString_ReadPayload` mapping for opcode `0x08CC`.

Raw local outputs:

- `artifacts/old-client-exe-sweep/16042_spell_entity_aux_opcode_matches.txt`
- `artifacts/old-client-exe-sweep/16029_spell_entity_aux_opcode_matches.txt`
- `artifacts/old-client-exe-sweep/16028_spell_entity_aux_opcode_matches.txt`
- `artifacts/old-client-exe-sweep/16005_spell_entity_aux_opcode_matches.txt`
- `artifacts/old-client-exe-sweep/15996_spell_entity_aux_opcode_matches.txt`
- `artifacts/old-client-exe-sweep/15884_spell_entity_aux_opcode_matches.txt`
- `artifacts/old-client-exe-sweep/15866_spell_entity_aux_opcode_matches.txt`
- `artifacts/old-client-exe-sweep/spell_entity_aux_named_summary.csv`
- `artifacts/old-client-exe-sweep/spell_entity_aux_registration_hits.txt`

### Stop State

Do not add labels or runtime behavior from this comparison. The authoritative
16042 state from Slices 1 and 2 remains:

- entity-stat aux registration and reader layouts are mapped, but per-opcode
  apply/producer classification remains blocked;
- spell aux packet shapes are mapped only at packet-contract level;
- spell/entity aux emit timing and field semantics remain blocked until current
  16042 producer/apply evidence, live packet capture, or focused runtime proof
  identifies per-opcode behavior.

## Slice 19 - Named Near-Neighbor Public-Event/Instance Stability

Question: do the remaining named near-neighbor/PTR builds expose extra public
event or instance producer/server anchors beyond the 16029/16028 comparisons in
Slices 10, 11, 14, and 15?

Disposition: rejected as a current accelerator. The public-event/instance
registration-window set, the instance request producer window, and the compact
public-event vote/scoreboard set are shape-stable across 16042, 16029, 16028,
16005, 15996, 15884 PTR, and 15866 PTR. This extends the earlier 16029/16028
near-neighbor conclusions through the full named near-neighbor/PTR list without
adding labels or runtime behavior.

### Commands

Public-event and instance registration-window pass:

```powershell
.\Decomp\Analysis\run_ghidra_analysis.ps1 `
  -ClientDir artifacts\old-client-exe-sweep\16005 `
  -OutputDir artifacts\old-client-exe-sweep\16005_exports `
  -ProjectDir artifacts\old-client-exe-sweep\ghidra_projects_16005 `
  -Targets WildStar64.exe `
  -ProjectLayout PerTarget `
  -AnalysisMode Skip `
  -NoApplyLabels `
  -SkipDefaultExport `
  -SkipCoverage `
  -ExtraPostScript FindOpcodeComparisons.java `
  -ExtraPostScriptArgs @('0x0139','0x06F7','0x06FA','0x0130','0x0132','0x0133','0x0134','0x013B','0x00D6','0x06F9','0x0112','0x00F1','0x0157','0x140050000','0x140080000','0x200') `
  -GhidraMaxHeap 4G
```

Instance request producer-window pass:

```powershell
.\Decomp\Analysis\run_ghidra_analysis.ps1 `
  -ClientDir artifacts\old-client-exe-sweep\16005 `
  -OutputDir artifacts\old-client-exe-sweep\16005_exports `
  -ProjectDir artifacts\old-client-exe-sweep\ghidra_projects_16005 `
  -Targets WildStar64.exe `
  -ProjectLayout PerTarget `
  -AnalysisMode Skip `
  -NoApplyLabels `
  -SkipDefaultExport `
  -SkipCoverage `
  -ExtraPostScript FindOpcodeComparisons.java `
  -ExtraPostScriptArgs @('0x0163','0x0153','0x00D2','0x1406f0000','0x140704000','0x100') `
  -GhidraMaxHeap 4G
```

Public-event vote/scoreboard pass:

```powershell
.\Decomp\Analysis\run_ghidra_analysis.ps1 `
  -ClientDir artifacts\old-client-exe-sweep\16005 `
  -OutputDir artifacts\old-client-exe-sweep\16005_exports `
  -ProjectDir artifacts\old-client-exe-sweep\ghidra_projects_16005 `
  -Targets WildStar64.exe `
  -ProjectLayout PerTarget `
  -AnalysisMode Skip `
  -NoApplyLabels `
  -SkipDefaultExport `
  -SkipCoverage `
  -ExtraPostScript FindOpcodeComparisons.java `
  -ExtraPostScriptArgs @('0x0139','0x06F7','0x06FA','0x100') `
  -GhidraMaxHeap 4G
```

Each pass was run for current 16042 and named builds 16029, 16028, 16005,
15996, 15884 PTR, and 15866 PTR.

### Evidence

Normalized instruction-shape summaries:

| Family | Build coverage | Matches / shapes | Diff vs 16042 |
| --- | --- | --- | --- |
| Public-event plus instance registration window | 16042, 16029, 16028, 16005, 15996, 15884 PTR, 15866 PTR | `20` / `20` for every build | none |
| Instance request producer window | 16042, 16029, 16028, 16005, 15996, 15884 PTR, 15866 PTR | `4` / `4` for every build | none |
| Public-event vote/scoreboard compact set | 16042, 16029, 16028, 16005, 15996, 15884 PTR, 15866 PTR | `5` / `4` for every build | none |

The registration-window pass confirms all named builds still expose the same
current public-event and instance server registration immediates:

- public-event objective/status/vote/scoreboard: `0x0112`, `0x0130`,
  `0x0132`, `0x0133`, `0x0134`, `0x0139`, `0x013B`, `0x00D6`, `0x06F7`,
  `0x06F9`, and `0x06FA`;
- instance server responses: `0x00F1` and `0x0157`.

The instance request producer-window pass confirms the current request opcodes
remain present through 15866 PTR:

- `0x0163` for `GameLib.SetInstanceSettings`
- `0x0153` for `GameLib.ResetSingleInstance`
- `0x00D2` for `GameLib.OnClosedInstanceSettings`

The fourth stable shape in that window is the same unrelated `MOV EBP,0xd2`
immediate near the producer window. It is not a separate request producer.

The public-event vote/scoreboard compact set keeps the same current evidence
through 15866 PTR: server registrations for `0x0139`, `0x06F7`, and `0x06FA`,
plus the sender-like `0x06FA` scoreboard request path. The deeper named builds
shift function starts and keep old unlabeled names, but do not change the
instruction shapes.

Raw local outputs:

- `artifacts/old-client-exe-sweep/pe_instance_regwindow_named_summary.csv`
- `artifacts/old-client-exe-sweep/instance_request_window_named_summary.csv`
- `artifacts/old-client-exe-sweep/public_event_vote_scoreboard_named_summary.csv`
- `artifacts/old-client-exe-sweep/*_pe_instance_regwindow_named_matches.txt`
- `artifacts/old-client-exe-sweep/*_instance_request_window_named_matches.txt`
- `artifacts/old-client-exe-sweep/*_public_event_vote_scoreboard_named_matches.txt`

### Stop State

Do not add labels or runtime behavior from this comparison. The authoritative
16042 state remains:

- public-event packet readers and the scoreboard request path are mapped, but
  vote UI sequence, timeout/default-choice behavior, scoreboard population,
  reward delivery, result ordering, reward tiers, and objective notification
  ordering remain blocked;
- instance request opcodes and server response packet shapes are mapped, but
  reset result semantics, settings ownership, retail timing, and UI
  presentation remain blocked;
- release 1.0.7.6682 remains stale protocol-drift evidence for the older
  scoreboard request and instance request opcodes from Slices 12 and 15.

The named near-neighbor/PTR list from the goal prompt is now covered for the
public-event and instance families.

## Slice 20 - Named Near-Neighbor Group/Queue Placeholder Stability

Question: do the remaining named near-neighbor/PTR builds expose extra anchors
for the broader group/queue placeholder cluster beyond the matching/raid subset
covered in Slices 5 and 16?

Disposition: rejected as a current accelerator. The focused group/queue
opcode-immediate scan reached the helper cap in every build, but the capped
normalized instruction-shape set is identical across 16042, 16029, 16028,
16005, 15996, 15884 PTR, and 15866 PTR. The registration anchors for the
tracked group/queue server opcodes are also identical across current 16042 and
the deepest checked named build. No old build exposed a sharper current 16042
target for group value semantics, matching role selection, queue status timing,
or raid info producer ownership.

### Commands

The group/queue pass used the current unresolved opcode cluster from
`MISSING_FEATURE_MATRIX.md`:

```powershell
.\Decomp\Analysis\run_ghidra_analysis.ps1 `
  -ClientDir artifacts\old-client-exe-sweep\15866 `
  -OutputDir artifacts\old-client-exe-sweep\15866_exports `
  -ProjectDir artifacts\old-client-exe-sweep\ghidra_projects_15866 `
  -Targets WildStar64.exe `
  -ProjectLayout PerTarget `
  -AnalysisMode Skip `
  -NoApplyLabels `
  -SkipDefaultExport `
  -SkipCoverage `
  -ExtraPostScript FindOpcodeComparisons.java `
  -ExtraPostScriptArgs @('0x0414','0x042A','0x0431','0x0436','0x0438','0x0441','0x045A','0x0461','0x0468','0x05CF','0x0600','0x0718','0x071A','0x200') `
  -GhidraMaxHeap 4G
```

The same opcode set was run for current 16042 and named builds 16029, 16028,
16005, 15996, 15884 PTR, and 15866 PTR.

### Evidence

Normalized capped instruction-shape summary:

| Build | Total matches | Unique instruction shapes | Shape diff vs 16042 |
| --- | ---: | ---: | --- |
| 16042 | 512 | 310 | authority |
| 16029 | 512 | 310 | none |
| 16028 | 512 | 310 | none |
| 16005 | 512 | 310 | none |
| 15996 | 512 | 310 | none |
| 15884 PTR | 512 | 310 | none |
| 15866 PTR | 512 | 310 | none |

The `512` match count is the helper cap, not proof that the whole binary has
only 512 relevant immediates. This pass is useful because every named build
reaches the same cap with the same normalized instruction-shape multiset.

Representative registration-immediate hits are stable between current 16042
and 15866 PTR:

| Opcode | 16042 registration | 15866 PTR registration |
| --- | --- | --- |
| `0x0438` | `14006f156` | `14006f156` |
| `0x0414` | `14006f1b8` | `14006f1b8` |
| `0x0441` | `14006f21a` | `14006f21a` |
| `0x0468` | `14006f2de` | `14006f2de` |
| `0x0431` | `14006f4ca` | `14006f4ca` |
| `0x0461` | `14006f794` | `14006f794` |
| `0x045A` | `14006f7c0` | `14006f7c0` |
| `0x0718` | `1400717e2` | `1400717e2` |
| `0x071A` | `14007180c` | `14007180c` |
| `0x042A` | `140072892` | `140072892` |
| `0x05CF` | `140075a13` | `140075a13` |
| `0x0436` | `140075bfd` | `140075bfd` |
| `0x0600` | `140075de7` | `140075de7` |

Current 16042 labels and findings remain the authority for this family:
`0x0438`, `0x0468`, `0x05CF`, `0x0600`, `0x0718`, and `0x071A` retain the
mapped/blocker states recorded in `MISSING_FEATURE_MATRIX.md`; the old-client
scan did not produce a new apply-table index, producer owner, non-zero raid
queue timing witness, or group value semantic.

Raw local outputs:

- `artifacts/old-client-exe-sweep/16042_group_queue_named_opcode_matches.txt`
- `artifacts/old-client-exe-sweep/16029_group_queue_named_opcode_matches.txt`
- `artifacts/old-client-exe-sweep/16028_group_queue_named_opcode_matches.txt`
- `artifacts/old-client-exe-sweep/16005_group_queue_named_opcode_matches.txt`
- `artifacts/old-client-exe-sweep/15996_group_queue_named_opcode_matches.txt`
- `artifacts/old-client-exe-sweep/15884_group_queue_named_opcode_matches.txt`
- `artifacts/old-client-exe-sweep/15866_group_queue_named_opcode_matches.txt`
- `artifacts/old-client-exe-sweep/group_queue_named_summary.csv`

### Stop State

Do not add labels or runtime behavior from this comparison. The current 16042
state remains:

- group and raid packet reader shapes are mapped where current labels already
  support them;
- `0x05CF` dispatcher/index semantics, `0x0600` trailing-value semantics,
  `0x0468` producer ownership, non-zero `0x0718` queue timing, and broader
  group flag/detail/selection/queue emit behavior remain blocked pending current
  16042 producer/apply proof, packet sequence evidence, or accepted live-client
  evidence.

The named near-neighbor/PTR list from the goal prompt is now covered for the
broader group/queue placeholder family.

## Completion Audit

Goal prompt coverage state:

| Prompt target | Sweep result |
| --- | --- |
| Near-neighbor EXEs `16029`, `16028`, `16005`, `15996`, `15884`, `15866` | Covered for the prioritized blocker families in Slices 16 through 20, with earlier 16029/16028 slices retained for detailed current-vs-old evidence. |
| Spell aux and entity-stat aux producer/apply paths | Slices 1, 2, and 18 reject old-client acceleration. The only named-neighbor drift is missing current `0x08CC` registration in 15996/15884/15866, recorded as stale protocol drift. |
| Public event objective/vote/scoreboard/reward paths | Slices 10, 12, 13, 14, and 19 reject old-client acceleration. Release 1.0.7 and alpha/beta evidence is stale or string-only, not current implementation authority. |
| Instance producer/server paths | Slices 11, 15, and 19 reject old-client acceleration. Release 1.0.7 instance request opcodes are stale protocol drift. |
| Matching, raid queue, group queue, housing, marketplace, and map-tracking placeholder families | Slices 3, 4, 5, 7, 16, 17, and 20 reject old-client acceleration or keep mapped/blocked state unchanged. The old-only map-tracked `0x0849` sender-like candidate is stale and absent from current 16042. |
| Stale `WorldZone` `1`/`2`/`6` and `Quest2Reward` `3668` provenance | Slice 9 rejects these as current implementation evidence and keeps them as stale/archive provenance or blockers. |

Completion checklist:

- Every swept target ends in one of the allowed states: rejected as old-only or
  stale, mapped/blocked unchanged, or current 16042 evidence already mapped with
  a named blocker.
- No new labels were added to `Decomp/Analysis/function_labels.csv`; no
  re-export is required.
- No runtime behavior changed, so no focused C# tests were required.
- No tracker or finding claims were upgraded to retail complete from old EXE
  evidence.
- Generated and local Ghidra artifacts remain under ignored
  `artifacts/old-client-exe-sweep`; durable sweep results are recorded in this
  source-controlled report.

Final sweep disposition: complete for the first-target old-client EXE prompt.
The old clients were useful for rejecting stale protocol drift and confirming
that the named near-neighbor builds do not reveal sharper current 16042
implementation targets for these blocker families. Current implementation work
still requires build-16042 producer/apply proof, packet/UI sequence evidence,
live-client evidence, or focused runtime proof as named in each slice.
