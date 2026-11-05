# Crafting auxiliary web/UI corroboration - 5 November 2026

Status: historical current-craft/discovery flows corroborated and all three
station service-key names now pinned by native follow-through;
`084B/0855` producer intent remains open. This note supplies bounded native
callback seeds. It does not authorize an emitter or assign packet fields from
similar-looking Lua tables.

## Scope and provenance

The primary content is Carbine/NCsoft-authored addon source and extracted
Houston API help, preserved in the third-party `wildstarnasa/NASADocs` mirror.
The Lua files carry NCsoft copyright headers. The immutable revision used here
is [`5a154a6f1ec384ef852f216ed07f5ae0ed174126`](https://github.com/wildstarnasa/NASADocs/commit/5a154a6f1ec384ef852f216ed07f5ae0ed174126),
dated **19 June 2015, 21:43:41 UTC**. Its commit message concerns adding the
extracted Houston help file; it does not establish when every addon was built.
This is historical original authored content, not an official GitHub account
or a demonstrated build16042 UI snapshot.

Web searches and exact source reads were performed on 5 November 2026. The
following raw-file hashes were computed from the bytes fetched at that revision.
Source line references below are the original LF file lines, not the web
reader's normalized line numbers.

| File beneath the revision | Bytes | SHA256 |
| --- | ---: | --- |
| `Carbine Addons/Crafting/Crafting.lua` | 13080 | `7b3e05b59162b6e60aa91979f907e70846a1b52cb81bde1fbd0cfde8a63136f7` |
| `Carbine Addons/Crafting/CircuitBoardSchematic.lua` | 44305 | `f760c13badffd8fe51139d801fb07cb341751faf7d7393e627f48dc44042ce58` |
| `Carbine Addons/CraftingGrid/CraftingGrid.lua` | 48345 | `42ad6f0ce4fcd377932ab5ee693e8f61726e83bc942c267e777e8ae98814ca6e` |
| `Carbine Addons/CraftingResume/CraftingResume.lua` | 7194 | `4bb6be4d26569ca6d175a621b974332f5df6b2f9b6b6eb76f58261d166c430fa` |
| `Carbine Addons/CraftingSummaryScreen/CraftingSummaryScreen.lua` | 15051 | `6152335f4c8e5e71f90dfda28280f45210e152e33937a59728eec2145024cdb8` |
| `Carbine Addons/Runecrafting/Runecrafting.lua` | 54816 | `f8f89901bc085ccdd006a01c7c2c974532c2b33125c815b9cd899fe445146c93` |
| `Carbine Addons/Tradeskills/TradeskillSchematics.lua` | 34512 | `8f3b9a1ffa786db857affe98945598eb181bbe81d996fb0eeb0e706ae0885dbd` |
| `Help File Extracted/LuaAPI/Classes/CraftingLib.htm` | 6218 | `6148e37e346b84dd0810807fcd908ee52aec823d0eaa0faa5022543dbf98ccf7` |
| `Help File Extracted/LuaAPI/Global/Global.htm` | 269074 | `0556bff27f746eb984c0f73ac4c7b8fdbe59008a58cb1d7d195d0449e1512d24` |

## What the authored UI actually consumes

### Current craft is persistent UI state, not just a completion result

`CraftingResume` checks `GetCurrentCraft().nSchematicId` when either a normal
crafting station or engraving station opens. An existing nonzero schematic
routes back to that craft; otherwise it opens the appropriate list. Its
`HelperStartCraft` chooses coordinate or circuit-board UI from
`GetTradeskillInfo(...).bIsCoordinateCrafting` / `bIsCircuitBoardCrafting`.
The station-capability filter is an authored TODO, not an implemented rule in
this source. [CraftingResume.lua:57-95,166-184](https://github.com/wildstarnasa/NASADocs/blob/5a154a6f1ec384ef852f216ed07f5ae0ed174126/Carbine%20Addons/CraftingResume/CraftingResume.lua#L57-L95)

The circuit-board component listens for `CraftingUpdateCurrent`, rereads
`GetCurrentCraft`, and consumes `tMicrochips[idx].itemChip`. Its system-socket
lookup prefers that current chip over the schematic's static `itemDefaultChip`.
Authored comments attribute the refreshed chip to the server. The user-selection
path separately returns chip item IDs and zero-based threshold selections.
This corroborates state ownership and a request/result distinction; it does not
make every chip random or identify an auxiliary opcode.
[CircuitBoardSchematic.lua:138-143,793-810,854-897](https://github.com/wildstarnasa/NASADocs/blob/5a154a6f1ec384ef852f216ed07f5ae0ed174126/Carbine%20Addons/Crafting/CircuitBoardSchematic.lua#L854-L897)

`Crafting.lua` starts a craft through `CraftItem` when no active craft
exists, and later submits the circuit component's selections through
`CompleteCraft(tMicrochips, tThresholds)`.
[Crafting.lua:176-222](https://github.com/wildstarnasa/NASADocs/blob/5a154a6f1ec384ef852f216ed07f5ae0ed174126/Carbine%20Addons/Crafting/Crafting.lua#L176-L222)

### Discovery feedback and learned recipes are distinct event consumers

The grid subscribes separately to `CraftingDiscoveryHotCold`,
`CraftingSchematicLearned` and `CraftingUpdateCurrent`. Its state reads include
`nSchematicId`, `nSubSchematicId`, `nAdditiveCount`, `fVectorX` and `fVectorY`.
The non-success hot/cold handler combines pending local marker data with the
current craft's schematic identity. It returns immediately for the symbolic
`Success` result. The learned-schematic handler instead clears markers and
rebuilds the grid. Historical non-success presentation therefore expects a
usable current-craft identity when consumed. This constrains a future replay
investigation, but does not prove native reset or network ordering.
[CraftingGrid.lua:136-150,205-239,365-489,592-630](https://github.com/wildstarnasa/NASADocs/blob/5a154a6f1ec384ef852f216ed07f5ae0ed174126/Carbine%20Addons/CraftingGrid/CraftingGrid.lua#L592-L630)

The summary addon also subscribes separately to completion, hot/cold and learned
events. Completion receives the original schematic, success flag, XP, returned
materials, resulting schematic and crafted item. The resulting schematic may
represent a variant. Hot/cold presentation rereads current craft and schematic
axis names; learned recipes add a separate summary entry. These are UI result
contracts, not transaction commit or packet publication policy.
[CraftingSummaryScreen.lua:48-59,141-170,276-313](https://github.com/wildstarnasa/NASADocs/blob/5a154a6f1ec384ef852f216ed07f5ae0ed174126/Carbine%20Addons/CraftingSummaryScreen/CraftingSummaryScreen.lua#L276-L313)

The Houston help explicitly describes `CraftingUpdateCurrent` as covering
start/resume, additives/catalysts, microchip changes and completion. Hot/cold
feedback describes proximity and direction relative to a discoverable recipe;
learned-schematic carries tradeskill and schematic identity. The documented
completion signature agrees with the summary addon. These independent authored
descriptions establish different API-level notifications, not a one-to-one
mapping to wire messages.
[Global.htm:1119-1165](https://github.com/wildstarnasa/NASADocs/blob/5a154a6f1ec384ef852f216ed07f5ae0ed174126/Help%20File%20Extracted/LuaAPI/Global/Global.htm#L1119-L1165)

`CraftingLib.htm` names `CodeEnumCraftingDiscoveryHotCold` members
`Cold/Warm/Hot/Success` and direction members
`None/N/NE/E/SE/S/SW/W/NW`. It declares `GetCurrentCraft`,
`GetCurrentOverchargeInfo`, `CraftItem`, `AddAdditive`, `CompleteCraft`,
`GetPreviewInfo`, `GetSchematicInfo` and `IsAtCraftingStation`. Most declarations
lack parameter or return descriptions. Member order is not numeric enum proof.
[CraftingLib.htm:7-25,70-109](https://github.com/wildstarnasa/NASADocs/blob/5a154a6f1ec384ef852f216ed07f5ae0ed174126/Help%20File%20Extracted/LuaAPI/Classes/CraftingLib.htm#L7-L25)

### Independent station-role corroboration

The ordinary circuit UI shows `NoStationBlocker` when
`IsAtCraftingStation()` is false. Its station check is a local repeating UI
timer, not an observed server cadence.
[Crafting.lua:51-87](https://github.com/wildstarnasa/NASADocs/blob/5a154a6f1ec384ef852f216ed07f5ae0ed174126/Carbine%20Addons/Crafting/Crafting.lua#L51-L87)

The schematic browser separately allows the autocraft button only when
`IsAtCraftingStation()` is true; otherwise it displays the localized
not-near-station message. This supports the predicate's general crafting-station
role independently of its name.
[TradeskillSchematics.lua:325-346](https://github.com/wildstarnasa/NASADocs/blob/5a154a6f1ec384ef852f216ed07f5ae0ed174126/Carbine%20Addons/Tradeskills/TradeskillSchematics.lua#L325-L346)

The rune UI calls `IsAtEngravingStation()` to control its rune-creation
no-station/no-signal blockers. The help describes `TradeskillEngravingStationOpen`
as interaction with an engraving NPC, and close as leaving/interacting with
another NPC. These distinguish engraving from the normal predicate. Neither
source supplies numeric service keys. No `IsAtMasterCraftsman` consumer was found
by the exact-name repository search; that bounded negative does not establish
API absence or prove what a master craftsman service permits.
[Runecrafting.lua:246-253](https://github.com/wildstarnasa/NASADocs/blob/5a154a6f1ec384ef852f216ed07f5ae0ed174126/Carbine%20Addons/Runecrafting/Runecrafting.lua#L246-L253),
[Global.htm:3738-3743](https://github.com/wildstarnasa/NASADocs/blob/5a154a6f1ec384ef852f216ed07f5ae0ed174126/Help%20File%20Extracted/LuaAPI/Global/Global.htm#L3738-L3743)

## Exact-build native seeds recovered without a shared session

A read-only PE section-mapped literal/pointer scan used
`C:\GIT\_archive\cluster-3d\NexusForever\Decomp\Client64\WildStar64.exe`,
SHA256 `231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5`.
The parent REA identity anchor is
`ev_142ebc012c9fce11df9c67703c5d9e335b4810c52c1d752b63ed2828030231d4`.
The observations below are a separate ordinary static byte scan, not REA
decompilation output. The adjacent registration entry is a name pointer and
function pointer; the following addresses include the image base.

| API name | Name-string VA | Registration pair VA | Function pointer |
| --- | --- | --- | --- |
| `GetSchematicInfo` | `140b20b30` | `140b6cd90` | `140599010` |
| `CraftItem` | `140b20ac0` | `140b6cde0` | `14059bb20` |
| `GetPreviewInfo` | `140b20a60` | `140b6cdf0` | `14059ada0` |
| `GetCurrentCraft` | `140b20a50` | `140b6ce00` | `14059b2f0` |
| `AddAdditive` | `140b20a80` | `140b6ce10` | `14059b7c0` |
| `CompleteCraft` | `140b20a70` | `140b6ce20` | `14059bb60` |
| `IsAtMasterCraftsman` | `140b20a18` | `140b6ce60` | `14059b230` |
| `IsAtCraftingStation` | `140b20c68` | `140b6ce70` | `14059b270` |
| `IsAtEngravingStation` | `140b20c50` | `140b6ce80` | `14059b2b0` |

Reproduction: map each PE section's `VirtualAddress` / `PointerToRawData`,
locate the NUL-terminated API ASCII string, search its little-endian 64-bit
image VA in the mapped image, and decode that registration entry's next qword.
The callback association is direct pointer evidence. Its behavior still needs
instruction or decompiler inspection; the table is not a send-order proof.

The old circuit source calls `GetCurrentOverchargeInfo`, but that exact ASCII
name was not found in this pinned executable or the sampled CraftingLib method
table `140b6ccc0..140b6cea0`. This is a concrete historical-version warning, not
proof that every possible registration/string encoding was searched. Do not
port the old overcharge computation into build16042 from the UI alone.

Useful event/enum string seeds are `CraftingUpdateCurrent` at `140afaa50`,
`CraftingSchematicLearned` at `140afa9e8`, and `CraftingDiscoveryHotCold` at
`140afaab8`. There are multiple copies. `140b20d88` starts
`CodeEnumCraftingDiscoveryHotCold`; its `140b20d90` suffix is therefore not a
separate event binding. `CodeEnumTradeskillResult` is at `140b20d00`. Adjacent
qwords around event strings are not function callbacks merely because a
pointer scan finds them.

### Native follow-through: all three station keys pinned

The subsequent [native contract report](DEEPER_ROADMAP_CONTRACTS_2026_11_05.md#crafting-station-key-names-resolved)
has inspected the callbacks above. Their shared predicate `1405a4850` searches
the local service cache at context `+0x66C8` and returns whether the exact key's
stored unit-ID value at node `+0x24` is nonzero.

| Named API | Confirmed callback constant | Service name supported at this boundary |
| --- | --- | --- |
| `IsAtCraftingStation` / `14059b270` | `0x2C` | Ordinary crafting station |
| `IsAtEngravingStation` / `14059b2b0` | `0x4F` | Engraving station |
| `IsAtMasterCraftsman` / `14059b230` | `0x57` | Master craftsman |

The exact constants are visible in
[role-craft-api-console.log](../../artifacts/continuation-20261105/deeper-roadmap/role-craft-api-console.log)
and [service-role-helpers-console.log](../../artifacts/continuation-20261105/deeper-roadmap/service-role-helpers-console.log).
The independent [cache writer/selector inspection](../../artifacts/continuation-20261105/deeper-roadmap/service-cache-writers-console.log)
and [entity eligibility/range inspection](../../artifacts/continuation-20261105/deeper-roadmap/station-eligibility-console.log)
bind these keys to completion selection and per-entity service candidates. The
range helper includes geometry adjustments; the default range value of `5.0`
does not establish a universal center-distance rule.

This closes the F-008 **service-key naming** question. The historical UI above
independently corroborates ordinary crafting versus engraving roles; the native
API binding is the naming witness for master craftsman. A nonzero cached local
candidate does not establish server eligibility, cache lifetime or how competing
candidates are selected. None of these findings associates `084B/0855` with an
apply path.

## What this narrows, and what remains blocked

The [indexed dispatcher evidence](BLOCKER_UNBLOCK_RESEARCH_2026_11_05.md#indexed-housing-crafting-and-tracked-unit-dispatch)
and [portable dispatch record](coverage/runtime-blocker-dispatch-20261105.json)
already distinguish the `0853` finish and `0854` current-craft paths. Both
`084B` and `0855` take this dispatcher's `1403f12d2` default/reporting path,
which returns one. This does not make them globally unused, and the web/UI
events do not supply their missing handler owner.

The [neutral Source models](../../Source/NexusForever.Network.World/Message/Model/Crafting/ServerCraftingAuxPackets.cs)
remain four `uint32`, one float, one `uint32` for `084B` and one `uint32`, two
floats for `0855`. A pair of floats is insufficient evidence for discovery
coordinates, overcharge, hot/cold proximity, or any other tempting name.

New bounded work is now concrete: inspect `14059b2f0` to bind Lua field names to
native current-state offsets; follow writers of those exact offsets through
the known `0854` consumer and any additional handler registration. The native
follow-through above has completed the three numeric station-key names; cache
clearing and competing-candidate behavior remain separate investigations.
Inspect `14059bb60` for the current request shape, then
compare finish event construction with the separate learned-schematic and
current-state writers. These are independent edges, not an assumption that
one undocumented auxiliary packet implements them all.

Still required before an `084B/0855` production emitter: an exact-build apply
or equivalent accepted-traffic association, each field's native meaning,
recipient and lifecycle gating, ordering/reset rules, and item-patch cadence.
No Source change, client launch, shared REA/Ghidra access, archive mutation,
commit or push was performed for this note. Code tests are not applicable to
this documentation-only pass.
