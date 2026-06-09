# NexusForever Full Missing-System Restoration Plan

Saved: 2026-06-04

This plan is the durable goal document for closing all server-side feature,
game-system, spell, content, protocol, and parity gaps tracked by
`CURRENT_STATUS.md`, `Decomp/Analysis/MISSING_FEATURE_MATRIX.md`, and the
focused subsystem trackers.

## Summary

- Use the completed `WildStar64.exe`, `Houston64.exe`, and
  `StsConnLib64.MT.dll` full decompile cache as lookup fuel, not as
  implementation proof. Every server change must still reach `Verified` via
  client consumer mapping, table/sniff/runtime evidence, or local smoke.
- Restore all missing server-side behavior by closing feature rows
  `F-001` through `F-036`, prioritizing gameplay blockers before exact parity
  polish.
- Keep each pass narrow: one feature row, packet cluster, spell family, or
  content fixture; end each pass as `Implemented`, `Mapped only`, `Rejected`,
  or `Blocked`.

## Milestone Roadmap

This roadmap turns the current blocked/partial state into an itemized closure
program. Milestones are ordered by dependency: protocol producer semantics first,
then shared gameplay systems, then social systems, then content smoke and final
retail-parity sweep.

### Milestone 0 - Closure Board And Evidence Hygiene

Goal: make every remaining task traceable to a feature row and an evidence state.

Items:

1. Reconcile `CURRENT_STATUS.md`, `MISSING_FEATURE_MATRIX.md`,
   `GAMEPLAY_ECONOMY_SOCIAL_STATUS.md`, `ENTITY_AUX_DECODE_ROADMAP.md`,
   `MATCHING_IMPLEMENTATION_STATUS.md`, and `BLOCKER_EVIDENCE_PLAN.md`.
2. Keep `F-001` through `F-036` as the top-level board. Do not track free-floating
   opcodes outside their feature row.
3. Classify every open gap as `Blocked`, `Mapped`, `Verified`, `Implementable`,
   `Implemented`, or `Rejected`.
4. Record one next action per blocked gap: native decompile pass, live-client
   harness bundle, table/query proof, source audit, or implementation.
5. After each pass, update this plan's progress log or the focused tracker, not
   both with duplicate prose.

Exit criteria:

- Every partial/blocked row has a named next evidence source.
- No item is described only as "needs decomp" without a target function,
  packet cluster, harness preset, or runtime question.

### Milestone 1 - Shared Protocol Producer Unblock

Goal: unblock the packet clusters that prevent several systems from becoming
runtime-complete.

Items:

1. `F-025`: map or capture entity-stat aux producer/consumer order for
   `0x0889`, `0x08CC`, `0x08F4`, `0x0939`, `0x093D`, and `0x093E`.
2. `F-025`: map map-tracked-unit producer timing, tracked id allocation,
   disable lifetime, and `TrackingSlot` selection for `0x0848`/`0x0849`.
3. `F-004`: decode `ServerHousingNeighborhoodEntry` and
   `ServerHousingNeighborhoodList` producer triggers for `0x0501`/`0x0506`.
4. `F-008`: decode crafting aux/current-craft producer intent for `0x084B`,
   `0x0855`, and current-craft cadence.
5. `F-016` through `F-020`: continue `SPELL_BROADCAST_ROADMAP.md` for spell
   wrapper, threshold, and broadcast packet producers.
6. Keep cluster aux packets diagnostic-only until a per-opcode consumer or
   sniff order witness exists.

Exit criteria:

- Each target packet is either implemented with producer tests or explicitly
  remains blocked with the missing consumer/sniff named.
- `ENTITY_AUX_DECODE_ROADMAP.md` and `CURRENT_STATUS.md` agree on which aux
  packets have production emitters.

### Milestone 2 - Auth And Diagnostic Closure

Goal: finish the only truly missing auth path and retire diagnostic-only packets
where safe.

Items:

1. `F-001`: map `/Auth/LoginTokenStart`, `/Auth/TokenKeyData`, RSA key material,
   signature validation, premaster derivation, server reply fields, and post-handshake
   crypto transition.
2. `F-001`: require startup STS capture or equivalent verified local proof before
   implementing token crypto.
3. `F-002`: group diagnostic client opcodes by likely feature cluster, then map
   native senders/consumers one cluster at a time.
4. `F-002`: promote diagnostic handlers only when the request intent and safe
   response behavior are known.
5. `F-030`: finish realm-transfer destination payload semantics, PTR copy queue
   timing, and transfer/copy handoff after the current packet boundaries are
   mapped.

Exit criteria:

- Token crypto is either implemented and verified or explicitly blocked by one
  named cryptographic primitive or capture gap.
- Each diagnostic opcode has a cluster owner, not just a numeric model name.

### Milestone 3 - Core Economy And Progression Systems

Goal: make the high-traffic gameplay economy systems durable and parity-ready.

Items:

1. `F-005`: finish persistent listing/search behavior, commodity partial-fill
   precision, cancel-failure/result semantics, CREDD owned-order rows, and
   marketplace aux producer semantics.
2. `F-006`: apply and smoke relevant auth migrations, then close remaining
   storefront gaps: `0x026A` owned-order tail, `0x0986`/`0x098F` live emit
   proof, non-zero `096A..096C` leading-field producers, unsupported offer
   item effects, and coupon native sender label.
3. `F-007`: capture or dynamically break on `0x07CD` apply/flag consumer behavior
   before naming the content-context `UInt*` fields, finish reward delivery after
   claim, and implement level/difficulty/content schedule filters.
4. `F-008`: implement discovery rolls/unlocks, complex/random output parity,
   material-source precision, non-success sigil/microchip mutator precision,
   and aux emit behavior only after proof.
5. `F-031`: keep current Fortune rarity-tier emulator pool, but capture retail
   `ServerFortuneRewards` or storefront rotation evidence before replacing
   per-item weights.

Exit criteria:

- Economy state survives restart where retail expects it.
- Every store/market/crafting mutation has tests for success, rejection, and
  reload where persistence is involved.

### Milestone 4 - Movement, Travel, And Match Runtime

Goal: close systems that move players between places, groups, and match states.

Items:

1. `F-009`: finish service-token bypass, global route state, taxi embark,
   taxi completion, charge/teleport rules, passenger/seat modes, and deployable
   vehicle semantics.
2. `F-010`: finish replacement backfill/merge sequencing, non-zero
   `ServerRaidQueueStatus`, durable raid locks, full requeue behavior, and
   cross-realm pool semantics.
3. `F-015`: finish PvP observer/reward/stat parity beyond current duel lifecycle
   and flag cooldown behavior.
4. Run live-client harness passes for rapid transport/flight, replacement queue,
   raid-info, duel/PvP, and PvP adventure entry/exit.

Exit criteria:

- Player travel and queue/match transitions can be smoked without WIP-only
  behavior for the common flows.
- Replacement queue and non-zero raid queue semantics are either implemented or
  remain blocked with capture requirements recorded in `MATCHING_IMPLEMENTATION_STATUS.md`.

### Milestone 5 - Social And Account-Adjacent Systems

Goal: finish the systems that require multi-character, multi-account, or
cross-service persistence.

Items:

1. `F-011`: implement guild bank economy, perks, recruitment subscriptions,
   standards/holomarks persistence, boss-token inventory, and warplot plug state
   where proof exists.
2. `F-012`: implement ICComm persistent channels, entitlement gating,
   throttling, auto-response persistence, friendship edge cases, and chat aux
   producers.
3. `F-026`: finish item context actions, decor item-use ordering, supply satchel
   producer precision, costume unlock/forget parity, pet flair/stance semantics,
   generic unlock persistence, and account/character unlock delta semantics.
4. `F-027`: close account-vs-character option split and client-facing option
   readback semantics if new proof appears.
5. `F-028`: decide whether support stays JSONL-only or gains a database-backed
   case lifecycle; map stuck-result signaling precision first.

Exit criteria:

- Multi-account smoke covers guild, ICComm/chat, friendship, item unlocks, and
  account/character option reload.
- Any admin/support workflow that remains emulator-only is explicitly labelled
  as such.

### Milestone 6 - Spell Runtime And Combat Semantics

Goal: close spell families with fixtures instead of broad speculative behavior.

Items:

1. `F-016`: finish damage, heal, shield, vital update, and combat-log packet
   parity.
2. `F-017`: finish stack groups, aura arbitration, buff/debuff persistence, and
   cleanup.
3. `F-018`: finish crowd control, forced movement, interrupt, cast-state, and
   movement-lock semantics.
4. `F-019`: finish proc dispatch, proc target data, trigger filters, cooldowns,
   and unsupported trigger diagnostics.
5. `F-020`: finish summons, traps, vehicles, ownership/controller state,
   `RavelSignal`, and scripted spell receiver graphs.
6. Use one fixture per captured spell family with `!spell inspect4`,
   `!spell capture4`, `!spell diag4`, `!spell procstates`, and
   `!spell procunsupported`.

Exit criteria:

- Each spell family has fixtures for at least one positive case, one rejection
  case, and one cleanup/reload case where state persists.
- Unknown `Spell4Effects` payload fields stay diagnostic-only unless mapped.

### Milestone 7 - Quest, Path, Public Event, And NPE Closure

Goal: move content from WIP-guessed scaffolds to verified playthroughs.

Items:

1. `F-023`: complete Rider's Reef Exile and Dominion manual smoke from creation
   through terminal handoff to the four starter destinations.
2. `F-022`: finish starter-zone smoke for Northern Wilds, Crimson Isle,
   Everstar Grove, and Levian Bay, including quest acceptance, turn-in,
   objective loops, rewards, respawn/re-entry, CSI, and VO timing where relevant.
3. `F-022`: close path mission gaps: active path persistence, reward history,
   Settler built-group/resource/avenue state, Soldier holdout runtime, Scientist
   scan-result packet, and path reward precision.
4. `F-022`: close public-event vote, scoreboard, objective notification,
   trigger, communicator, story panel, reward, and cleanup semantics with
   harness bundles.
5. Keep WIP-guessed branch-derived scripts marked until a bundle or decompile
   proof validates the behavior.

Exit criteria:

- Rider's Reef is promoted only after both faction flows pass client-visible
  smoke.
- Quest/path/public-event tracker notes distinguish generic table support,
  curated script support, live-smoked support, and blocked retail details.

### Milestone 8 - Instance, Dungeon, Raid, PvP Adventure, And Live Event Closure

Goal: turn imported map bindings and WIP event scaffolds into verified content.

Items:

1. `F-024`: smoke Evil from the Ether end-to-end: triggers, doors, medbay/upper
   deck/escape teleports, cinematics, crew logs, boss cadence, rewards, and
   cleanup.
2. Expeditions: smoke Outpost M-13, Space Madness, Fragment Zero, Gauntlet,
   Infestation, Evil from the Ether, and Deep Space Exploration.
3. Dungeons: smoke Coldblood Citadel, Protogames Academy, Ruins of Kel Voreth,
   Stormtalon's Lair, Skullcano, Sanctuary of the Swordmaiden, and Ultimate
   Protogames dungeon.
4. Raids/events: smoke Initialization Core Y-83, Red Moon Terror, Genetic
   Archives, Datascape, Ultimate Protogames raid, Shade's Eve, Protostar
   SuperMall, and Journey into OMNICore-1.
5. PvP/adventures: smoke Cryo-Plex, Daggerstone Pass, Halls of the Bloodsworn,
   Walatiki Temple, War of the Wilds, and Rage Logic.
6. Live events and LaughingWS overlays: close or reject Battle Chase, Dungeon
   Chase, Shades Eve, Space Chase, Starfall, Sim Chase 1, zPrix, Skyplot housing,
   Dust Stalker, Arcterra/Palaver source-only rows, broad new-zone rows, and
   sandbox/test-zone rows.

Exit criteria:

- Every WIP-guessed map/event script has one of: verified smoke, mapped-only
  blocker, or explicit rejection.
- No broad SQL overlay is promoted without row-level ownership, placement,
  negative-case, and cleanup proof.

### Milestone 9 - Final Parity Classification Sweep

Goal: replace broad "complete" language with precise parity categories.

Items:

1. Reclassify each `F-001` through `F-036` row as `Retail-complete`,
   `Emulator-complete`, `Mapped-only`, `Blocked`, or `Rejected`.
2. Run full game tests and full solution build.
3. Run client smoke for auth, character create/select, Rider's Reef, travel,
   housing, group/matching, crafting, combat, loot, mail, storefront, guild,
   ICComm/chat, Fortune, and realm/PTR compatibility paths.
4. Update `CURRENT_STATUS.md`, `MISSING_FEATURE_MATRIX.md`, and focused trackers
   to agree on counts and remaining blockers.
5. Archive or close stale ghost-gap claims where source audits prove the claim
   false.

Exit criteria:

- `CURRENT_STATUS.md` is internally consistent with focused trackers.
- Remaining gaps are intentional, named, and evidence-gated.

## Immediate Sprint Backlog

Start with a five-lane sprint so the work does not stall behind only decompile
or only live-client tasks.

1. `F-025`: continue entity-stat aux and map-tracked-unit producer mapping.
   - Output: updated `ENTITY_AUX_DECODE_ROADMAP.md`, labels/findings if mapped,
     and packet/runtime tests only if producer semantics are verified.
2. `F-004`: decode housing neighborhood list trigger and send timing.
   - Output: mapped trigger or blocked note with exact missing function/capture.
3. `F-008`: run crafting discovery/complex-craft evidence bundle.
   - Output: station/current-craft/discovery capture notes, then implementation
     only for proven behavior.
4. `F-023`: complete Rider's Reef Exile and Dominion client smoke.
   - Output: accepted evidence bundle, tracker update, and fixes for any observed
     quest/terminal/hoverboard/projector mismatch.
5. `F-010`: capture replacement queue and non-zero raid queue behavior.
   - Output: `MATCHING_IMPLEMENTATION_STATUS.md` update and implementation only
     for observed queue/status semantics.

## Key Changes

- Phase 1, unblock shared protocol emitters: map post-read consumers under the
  native `WorldSocket+0x15b0` handler chain, then implement only verified producers for entity-stat aux,
  map-tracked units, housing neighborhood list, crafting aux/current-craft, and
  spell broadcast/threshold packets.
- Phase 2, complete high-impact game systems: finish housing parity,
  marketplace/commodity/CREDD persistence precision, reward rotation
  delivery/filtering, crafting discovery/runes/sigil failures,
  transport/taxi/vehicles, group/matching/raid queues, guild
  bank/perks/recruitment, ICComm persistence, loot/master-loot/BoP, PvP/duel
  rewards/cooldowns, LAS async update, options/support/realm transfer/PTR copy.
- Phase 3, spell runtime closure: work family-by-family from the spell
  trackers, starting with proc validation, damage/heal/shield packet parity,
  stack groups, CC/forced movement, summons/traps/vehicles/AI ownership,
  `RavelSignal` receiver graphs, and spell wrapper/threshold follow-ups.
  Unknown `DataBits` remain diagnostic-only until fixtures prove them.
- Phase 4, content and world parity: run accepted evidence bundles for
  Rider's Reef, Evil from the Ether/world `3404`, expedition/dungeon/raid/
  public-event smoke rows, blocked LaughingWS residual rows, zone completion,
  achievements, datacubes/journals, challenges, leaderboards, Fortune weights,
  and reward-rotation quest rewards.
- Phase 5, blocked/security/external surfaces: address STS token crypto only
  after safe crypto semantics are mapped; keep real-money VC billing and exact
  Fortune retail weights blocked unless valid retail/catalog evidence is
  obtained.

## Interfaces And State

- Add or extend backing stores only inside the owning subsystem: marketplace/
  CREDD order tails, reward-rotation grants, rune item state, raid locks, guild
  bank/perks/recruitment, persistent ICComm/options/support cases, PvP
  cooldowns/stats, and PTR transfer/copy state.
- Add internal spell/runtime abstractions only where they reduce repeated
  logic: proc event context, spell broadcast lifecycle helpers, stack
  arbitration, spell-owned aura persistence, summon ownership/controller state,
  and `RavelSignal` receiver dispatch.
- Preserve existing packet models until consumer semantics are proven; rename
  neutral `ValueN` fields only after read/write order and business meaning are
  mapped.
- Runtime code must not query staging/reference databases. Promote any needed
  WildStar/Jabbithole data into runtime-owned tables, scripts, or verified
  generated assets first.

## Test Plan

- For every protocol change: packet shape tests plus at least one
  handler/runtime test proving emit timing or rejection behavior.
- For every persistence change: migration/model/snapshot consistency,
  save/reload tests, and live DB application notes when needed.
- For every spell family: fixture tests using `/spell inspect4`,
  `/spell capture4`, or `/spell diag4` evidence; compare diagnostics, combat
  logs, packets, state deltas, and cleanup.
- For every content row: harness bundle with transcript, server logs,
  client-visible result, negative case, and JSON artifact where available.
- Verification cadence: focused owning tests first, then
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj -v minimal --nologo`;
  run full solution build when shared contracts, DB projects, or multi-service
  behavior changes.

## Assumptions

- "All missing" means full retail-parity closure of matrix rows `F-001` through
  `F-036`, not just opcode/model coverage.
- Default priority is visible gameplay and broad system correctness first,
  exact retail edge parity second, and unsafe/external surfaces last.
- Existing trackers remain canonical: update `CURRENT_STATUS.md`,
  `MISSING_FEATURE_MATRIX.md`, focused subsystem trackers, and decompile
  labels/findings after each implemented or blocked pass.

## First Pass

Start with Phase 1 shared protocol emitters, because several feature rows are
blocked by the same missing consumer evidence. The first narrow target is the
F-025 entity/cluster aux dispatch path:

```text
Which inserted handler-node vtables under the world socket handler list
consume shape-mapped entity aux packets, and can any blocked entity-stat aux
packet be promoted from shape-only to mapped producer semantics?
```

Use `Decomp/Analysis/ENTITY_AUX_DECODE_ROADMAP.md` as the pass tracker.

## Progress Log

2026-06-09 - F-007 reward rotation `0x07CD` cached-export/source recheck:

- Result: `Mapped only / Blocked apply semantics` for
  `ServerRewardRotationContentContext` field meaning, `Flag`, and dynamic
  throttle-slot assignment; no runtime behavior changed.
- Ghidra MCP discovery found no running instances, so no labels or exports were
  refreshed. The pass used cached `WildStar64.exe` fragments under
  `selected_decompiled_cache/functions/sha256_231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5`.
- Cached `ServerRewardRotationContentContext_ReadPayload` (`14008fcb0`) still
  proves the `0x07CD` wire shape only. Cached `RewardRotation_ManagerInit`
  (`140635840`) initializes seven request-throttle slots, cached
  `Reward_SendRewardUpdateRequest` (`140636ba0`) sends only the content-type
  index through `0x07CC`, and cached
  `RewardRotation_GetLoadedScheduleForContent` (`140636c40`) proves
  refresh-by-index / loaded-schedule lookup rather than `0x07CD` apply.
- Keep `ServerRewardRotationContentContext.UInt0`/`UInt1`/`UInt3`/`Flag`
  neutral until a retail `0x07CD` capture, accepted live reward-rotation
  bundle, or dynamic runtime apply-dispatch breakpoint proves assignment and
  consumer semantics.

2026-06-04 - F-007 reward rotation `0x07CD` mapped-only recheck:

- Result: `Mapped only / Blocked` for `ServerRewardRotationContentContext`
  apply semantics; no runtime packet behavior changed.
- Ghidra MCP rechecked `ServerRewardRotationContentContext_ReadPayload`
  (`14008fcb0`), `ServerRewardRotationContentContextArray_ReadPayload`
  (`14008fdc0`), `RewardRotation_ManagerInit` (`140635840`),
  `Reward_SendRewardUpdateRequest` (`140636ba0`),
  `RewardRotation_GetLoadedScheduleForContent` (`140636c40`), and the Lua
  request/getter callbacks (`1407091e0`, `140709210`, `140709370`).
- `0x07CD` still registers only the row reader and no static apply handler.
  `Reward_SendRewardUpdateRequest` sends `0x07CC` with only the content-type
  index and reads request-throttle slots at `manager + 0x150/+0x158/+0x160`.
  This maps the throttle correlation but not the server context row assignment.
- Kept `ServerRewardRotationContentContext.UInt0`/`UInt1`/`UInt3`/`Flag`
  neutral and updated comments/trackers to require a retail `0x07CD` capture or
  dynamic apply-dispatch breakpoint before renaming or changing flag behavior.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~RewardRotation|FullyQualifiedName~RewardUpdate|FullyQualifiedName~RewardProperty" -v minimal --nologo -m:1 -p:UseSharedCompilation=false -p:OutDir=I:\GIT\NexusForever\artifacts\testbin\f007-reward\`
  passed 55/55.

2026-06-04 - F-004 housing neighborhood Ghidra MCP recheck:

- Result: `Blocked` for F-004 `ServerHousingNeighborhoodEntry` (`0x0501`) and
  `ServerHousingNeighborhoodList` (`0x0506`) production emitters; no runtime
  behavior changed.
- Direct Ghidra MCP decompile rechecked `ServerHousingNeighborhoodEntry_ReadPayload`
  (`14009cbe0`), `ServerHousingNeighborhoodList_ReadPayload` (`14009ebf0`), and
  `Housing_HandleNeighborhoodList` (`1404ba4f0`). The consumer still clears the
  client neighborhood cache, copies `0x30` rows, duplicates the row name string,
  and dispatches `HousingNeighborhoodRecieved`.
- Adjacent native senders were rechecked and rejected as the missing trigger:
  `Housing_SendClientVisitResidenceIdentity` (`1405b2390`) sends `0x052F`,
  `Housing_SendClientCommunityPlacement` (`1404b6b90`) sends `0x052B`, and
  `Housing_SendClientCommunityPlotReservation` (`14057fe90`) sends guild-operation
  `0x04B1`.
- `RequestJoinNeighborhood()` / `RequestLeaveNeighborhood()` strings remain
  observed-only because the current exports provide no usable xref. `HousingNeighborhoodInfo.tbl`
  has client metadata but no verified mapping to the packet row tail fields.
- Implementation gate remains a live housing UI/realm-login sniff or native
  server-push path proving `0x0506` ordering plus backing data for
  `NeighborhoodId`, both 14-bit realm fields, the second uint64, name, and the
  three trailing uint32 fields.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-build --filter "FullyQualifiedName~HousingPacketShapeTests|FullyQualifiedName~HousingAuxiliaryPacketEmitterTests" -v minimal --nologo`
  passed 24/24.

2026-06-04 - F-025 map-tracked unit producer Ghidra MCP recheck:

- Result: `Blocked` for F-025 map-tracked unit production emitters; no runtime
  behavior changed.
- Direct Ghidra MCP decompile rechecked `ServerMapTrackedUnitUpdate_ReadPayload`
  (`1400a6c10`), `MapTrackedUnitUpdate_ApplyAndDispatch` (`1403f4170`),
  `MapTrackedUnitDisable_ApplyAndDispatch` (`1403f4200`),
  `ClientEvent_MapTrackedUnitUpdate_Dispatch` (`140430f80`),
  `Lua_GameLib_GetMapTrackedUnitData` (`140511c80`),
  `Lua_PublicEvent_GetTrackedUnits` (`14068adb0`), and
  `Lua_PublicEventObjective_GetTrackedUnits` (`140690500`).
- The decompile confirms client cache update/remove plus Lua enumeration only:
  no server tracked-unit id allocation, update cadence, disable lifetime, or
  `TrackingSlotId` selection rule was exposed.
- Read-only `TrackingSlot` DataMapping evidence shows duplicate objective
  groups (`PublicEventObjectiveId` `5010` has 20 rows; `5138` has 16 rows), so
  objective-only slot selection is rejected.
- Implementation gate remains a native server send site, accepted live
  public-event marker capture, or equivalent evidence proving id allocation,
  cadence, disable lifetime, and slot selection.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-build --filter "FullyQualifiedName~EntityAuxiliaryPacketShapeTests|FullyQualifiedName~EntityCreateAuxiliaryEmissionTests" -v minimal --nologo`
  passed 19/19.

2026-06-04 - master closure workboard + F-025 entity-stat cached export recheck:

- Result: `Blocked` for F-025 entity-stat aux production emitters; no runtime
  behavior changed.
- Added `FULL_MISSING_SYSTEM_CLOSURE_WORKBOARD.md` as the master routing board
  for feature rows `F-001` through `F-036`, consolidated runtime blockers,
  diagnostic client opcodes, shape-mapped aux/spell packets, WIP-guessed
  content, placeholder names, TODO/FIXME/NotImplemented inventory, and
  ghost-gap reconciliation.
- Rechecked the current `WildStar64.exe` cached export artifacts for
  `0x0889`, `0x08CC`, `0x08F4`, `0x0939`, `0x093D`, and `0x093E`. The hits
  still resolve to registration/reader evidence and known `WorldSocket+0x15b0`
  chain walkers; no new nontrivial `vtable+0x58` apply handler or producer
  witness was found.
- `ENTITY_AUX_DECODE_ROADMAP.md` now records the 2026-06-04 cached-export
  recheck and keeps the implementation gate closed until a per-opcode apply
  handler, new handler-node family, apply-table/data-ref classification, or
  live sniff/order witness exists.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore --filter "FullyQualifiedName~EntityAuxiliaryPacketShapeTests|FullyQualifiedName~EntityCreateAuxiliaryEmissionTests" -v minimal --nologo`
  and the same command with an isolated `BaseOutputPath` were blocked by the
  running `NexusForever.WorldServer` process holding default WorldServer output
  DLLs. The no-build focused gate
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-build --filter "FullyQualifiedName~EntityAuxiliaryPacketShapeTests|FullyQualifiedName~EntityCreateAuxiliaryEmissionTests" -v minimal --nologo`
  passed 19/19.

2026-06-02 - F-025 first pass:

- Result: `Mapped only` / false leads rejected. Entity-stat aux remains blocked
  for runtime emission.
- `FUN_140001a50` is `ActorFixedWindow_Ctor`, not an entity aux or socket
  handler anchor.
- The `WorldZone_* +0x14b0` cluster is spatial/visibility maintenance using
  `node+0x450` backlinks, not the `WorldSocket` opcode-handler chain using
  `node+0x20`.
- `FindImmediateInstructions 0x14b0` and entity-stat `FindOpcodeComparisons`
  did not expose a safe producer/consumer mapping beyond registration.
- Next target: catalog nontrivial consumers under the socket-owned
  callback-view `+0x14b0` / native `WorldSocket+0x15b0` handler chain, then
  prove slot `+0x50` filters and slot `+0x58` apply implementers for blocked
  aux opcodes.

2026-06-02 - F-025 second pass:

- Result: `Mapped only`. The chain offset and append helpers are now mapped,
  but entity-stat aux still has no safe producer/consumer semantics for runtime
  emission.
- Corrected the handler-chain owner: `WorldSocket_FilterServerMessageHandlers`
  and `WorldSocket_ProcessServerMessage` receive the callback-table subobject
  installed at native `WorldSocket+0x100`, so their decompiled `param_1+0x14b0`
  field is native `WorldSocket+0x15b0`.
- `FindImmediateInstructions 0x15b0` returned 33 matches and exposed the native
  chain lifecycle: ctor initialization, teardown, update/render walkers, and
  append/toggle helpers.
- Mapped appenders include diagnostic message nodes (`140015ec0` /
  `PTR_FUN_140b55540`), the persistent console node at `socket+0x1598`
  (`1400163d0` / `PTR_FUN_140b55430`), options/addons node at `socket+0x15a0`
  (`140016560`, `1400166a0` / `PTR_FUN_140b558c0`), and a larger persistent
  `socket+0x15a8` node (`PTR_FUN_140b690f0`) that was classified in the
  follow-up as Fortune.
- Slot tables show the diagnostic, console, and options/addons node filter/apply
  style slots return `1` via `14001d310`; the `+0x15a8` node has a nontrivial
  apply-style slot for Fortune opcodes, not aux packets.

2026-06-02 - F-025/F-031 Fortune chain follow-up:

- Result: `Implemented` small naming closure and `Mapped only` for F-025. The
  `socket+0x15a8` apply slot is a Fortune consumer family, so entity-stat aux
  remains blocked.
- `FortuneNode_ApplyServerFortunePackets` (`1404d60f0`) dispatches
  `ServerFortuneCardUpdate` (`0x03CF`), `ServerFortuneReset` (`0x03D0`),
  `ServerFortuneCards` (`0x03D1`), and `ServerFortuneRewards` (`0x03D2`).
- `Fortune_ApplyCardUpdate` (`1407290a0`) returns without applying the
  operation/card flags when the first payload bool is false. NexusForever now
  names that field `ServerFortuneCardUpdate.HasUpdate`.

2026-06-02 - F-016/F-020 spell wrapper 0x0818 field closure:

- Result: `Implemented` naming closure only. Wire shape is unchanged and no
  production spell-wrapper send path was enabled.
- Promoted the leading `ServerSpellWrapperTierEntry` field from `SlotOrIndex`
  to `SpellWrapperId` based on dispatcher `1403ee403` resolving payload field
  zero through `SpellService_LookupSpellWrapperByWrapperId` before applying the
  nested tier-entry payload.
- Earlier ability-book and subtype-`4` slot/index probes are now recorded as
  historical false leads for the leading field. Remaining blocker is a sniffed
  wrapper lifecycle witness and selector-tail semantic correlation before
  server emission can be safely implemented.

2026-06-02 - F-030 PTR copy packet boundary:

- Result: `Implemented` packet/test boundary and `Mapped only` for PTR queue
  handoff. No queue notice emit or copy mutation was enabled.
- Added focused tests for empty `ClientPtrCopy` (`0x06E8`) and
  `ServerPtrCharacterCopyQueued` (`0x06EA`) payloads, plus a diagnostic-only
  handler guard for `ClientPtrCopyHandler`.
- Recorded that native consumer `140020ea0` fires Lua `PTRCharacterCopyQueued`
  for `0x06EA`; remaining blocker is server producer timing and copy handoff
  semantics after `ClientInitiatePTRCharacterCopy` / `ClientPtrCopy`.

2026-06-02 - F-030 realm-transfer destinations aux envelope:

- Result: `Implemented` packet-contract closure only. No realm-transfer or
  account-gift runtime emit path was enabled.
- Replaced `ServerRealmTransferDestinationsAux` fixed raw `0x10` payload with
  the mapped native reader envelope: leading `uint32`, `uint32` byte count, and
  counted raw data bytes. Payload contents and producer semantics remain
  blocked.

2026-06-02 - F-009 vehicle embark aux packet contract:

- Result: `Implemented` packet-contract closure only. No vehicle runtime emit
  or passenger/seat behavior was enabled.
- Replaced `ServerVehicleEmbarkAux` fixed raw payload with the mapped reader
  shape from `14008ff20`: `flag`, 2-bit value, `uint64`, and two `uint32`
  fields. Field semantics and producer timing remain blocked.

2026-06-02 - F-026 item aux packet contracts:

- Result: `Implemented` packet-contract closure only. No supply-satchel,
  costume, item-unlock, or emote runtime emit path was enabled.
- Replaced `ServerSupplySatchelAux` and `ServerCostumeItemAux` fixed raw
  payloads with their mapped reader-backed shapes: `0x019A` uses the shared
  `14007fcf0` 6-bit value plus `uint32` payload, while `0x037F` uses the
  `1400874a0` 14-bit value, three `uint32` fields, and two flags. Producer
  semantics remain blocked.

2026-06-02 - size-1 aux empty packet contracts:

- Result: `Implemented` packet-contract closure only. No datacube, duel,
  resurrection, appearance, loot, path-mission, or entity-select runtime emit
  path was enabled.
- Corrected seven native size-1 registrations that bind to
  `ServerEmpty_ReadPayload` (`14007d8e0`) so NexusForever writes empty payloads
  instead of one raw byte: `0x00DF`, `0x00EE`, `0x0101`, `0x0143`, `0x014D`,
  `0x0160`, and `0x0187`. Managed names now use `...Empty`; feature semantics
  remain blocked.

2026-06-02 - scalar aux packet contracts:

- Result: `Implemented` packet-contract closure only. No path/scientist,
  entity-select, reputation, time-of-day, map-tracked-unit, or path-XP runtime
  emit path was enabled.
- Promoted `0x0181`, `0x01A6`, and `0x0846` from fixed raw 4-byte payloads to
  direct uint32 scalar models. Corrected `0x0186` from `ServerEntitySelectAuxUInt32`
  to `ServerEntitySelectAuxUInt14` after `InspectCodeAddress 14007a530` proved
  the reader consumes only 14 bits. Labeled `14006c1c0` as
  `NetworkBitReader_ReadUInt32Fast`.

2026-06-02 - reputation aux packet contracts:

- Result: `Implemented` packet-contract closure only. No reputation/path-XP
  runtime emit path was enabled.
- `Network_RegisterServerOpcode_0351` shows `0x01A7` uses reader `14008ef80`
  (`uint64 + uint32`), `0x01A8` shares `ServerHousingResidenceKeyedUpdate_ReadPayload`
  (`uint64 + uint32`), and `0x01A9` uses `14008d430` (`uint14 + uint32`).
  Added durable labels for `14008ef80` and `14008d430`; corrected managed
  `0x01A9` naming away from the misleading `ServerReputationAuxUInt64`.

2026-06-02 - F-012 chat aux notification packet contract:

- Result: `Implemented` packet-contract closure only. No ICComm/chat/cinematic
  runtime emit path was enabled.
- `0x01EF` now writes the native `ServerChatAuxNotification_ReadPayload`
  (`1400a0890`) shape: 8-bit row count, counted rows of `uint32`, 14-bit value,
  `uint64`, two `uint32`, two bytes, and counted subrows of 5-bit value plus
  two `uint32` fields. Added durable labels for `1400a0890` and row reader
  `1400a06d0`. `0x01B8`, `0x01C1`, and `0x01C4` were left for the next
  `PTR_LAB_140c1ec90` variant-table pass; see the 2026-06-03 closure below.

2026-06-03 - F-012 chat aux row/envelope packet contract:

- Result: `Implemented` packet-contract closure only. No ICComm/chat/cinematic
  runtime emit path was enabled.
- Recovered `PTR_LAB_140c1ec90` and writer sibling `PTR_LAB_140c1ec30` for
  `0x01B8` row variants. `ServerChatAuxRow_ReadPayload` (`140085ca0`) reads a
  4-bit variant, two `uint16` header fields, then dispatches: variants `0/2/3`
  read one bool, `1/7/11` read one `uint32`, `4` reads an 18-bit scalar, `5`
  reads a 15-bit scalar, `6` reads a 14-bit scalar, `8` reads the complex row
  with two counted `uint32` arrays, `9` reads one `uint64`, and `10` reads a
  14-bit value plus two `uint32` fields.
- `0x01B8` now writes one `ServerChatAuxRow`; `0x01C1` writes
  wide string + 5-bit row count + rows + bool footer + `uint16` footer from
  `ServerChatAuxPayload_ReadPayload` (`140086410`); `0x01C4` writes wide
  string + 5-bit row count + rows + `uint16` footer from
  `ServerChatAuxPayloadAlt_ReadPayload` (`140085fe0`).
- Added durable labels for row/envelope readers and writers. Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj -v minimal --nologo`
  passed 2483/2483.
- Remaining blocker: producer/consumer semantics and runtime emit conditions for
  the chat/cinematic auxiliary rows are still unmapped.

2026-06-03 - F-012 ICComm scoped-membership revalidation (R3-IC1):

- Result: `Implemented` transient membership bug fix. No persistent channel,
  entitlement, throttling, auto-response, friendship, chat aux producer, or
  durable social-option behavior changed in this pass.
- `ICCommManager.SendMessage` now revalidates scoped group/guild membership
  before accepting a sender. Stale senders are removed from the transient
  channel and receive `NotInChannel` without echo/result packets.
- `SendMessage` also prunes stale scoped recipients before delivery, and
  `Update` removes offline or no-longer-affiliated group/guild members from
  transient channels.
- `ICCommManagerTests` cover stale group senders, stale guild recipients, and
  update-time guild leave cleanup.
- Remaining blocker: F-012 still needs persistent ICComm channels, entitlement
  gates, retail delivery precision, exact leave/logout signaling, throttling,
  durable auto-response/ignore-strangers behavior, and chat aux producer
  semantics.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~ICComm" -v minimal --nologo`
  passed 8/8, and
  `dotnet build Source\NexusForever.WorldServer\NexusForever.WorldServer.csproj --no-restore -v minimal --nologo`
  passed with 0 warnings and 0 errors.

2026-06-03 - story/recruitment boundary packet contracts:

- Result: `Implemented` packet-contract closure only. No story, recruitment,
  pet, or flight runtime emit path was enabled.
- `0x074A` (`ServerStoryCommunicatorAux`) is registered in
  `Network_RegisterServerOpcode_0351` (`14006c290`) with reader `140080c80`.
  The reader consumes five `uint32` fields and one trailing `uint16` field.
  NexusForever now writes that typed shape instead of a fixed raw `0x18`
  payload.
- `0x077E` shares `ServerFlightPathUpdate_ReadPayload` (`14008eaa0`): one
  `uint32` count followed by a counted uint32 array. The former fixed
  four-uint32 model/name was misleading; NexusForever now exposes
  `ServerRecruitmentAuxUInt32List`.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~PacketPlaceholderNamingTests" -v minimal --nologo`
  passed 70/70.
- Remaining blocker: story communicator field semantics, recruitment/pet alias
  producer intent, and runtime emit timing remain unmapped.

2026-06-03 - marketplace aux packet contracts:

- Result: `Implemented` packet-contract closure only. No auction, commodity,
  CREDD, or marketplace runtime emit path was enabled.
- `0x06DF` (`ServerAuctionPostAux`) is registered in
  `Network_RegisterServerOpcode_0351` (`14006c290`) with reader `140090090`.
  The reader consumes a `uint32` count, a counted `uint32` array, a counted byte
  array using the same count, and one trailing `uint32` field.
- `0x07D5` (`ServerAuctionsByFilterAux`) is registered with reader
  `14008fe80`, which consumes one 14-bit value, three `uint32` fields, and one
  flag.
- 2026-06-04 MCP/export recheck: `0x06DF` has only data xref `140dcf0e8`
  and registration call-site refs `140073421`/`140073433`; `0x07D5` has only
  data xref `140dcf0ac` and registration call-site refs
  `14007335d`/`14007336f`. The registration rows in
  `selected_decompiled_cache/functions/.../14006c290.fragment.c` bind
  `0x06DF` to size `0x20` and `0x07D5` to size `0x14`, with no static apply
  callback, producer xref, or emit timing proof found.
- Remaining blocker: auction-post and auction-filter producer/consumer
  semantics remain unmapped; do not emit these packets from marketplace runtime
  paths until a client apply path, live sniff, or server producer witness proves
  the field meanings and timing.

2026-06-03 - option/keybind aux packet contracts:

- Result: `Implemented` packet-contract closure only. No option, keybinding,
  item, unlock, or runtime emit path was enabled.
- `0x056B` (`ServerOptionAuxPayload`) is registered in
  `Network_RegisterServerOpcode_0351` (`14006c290`) with reader `1400a3ce0`.
  The reader consumes two `uint64` fields, one `uint32` field, and one trailing
  `uint64` field.
- `0x056C` (`ServerOptionAuxPayloadLarge`) is registered with reader
  `1400a3d50`, which consumes three `uint64` fields, one 18-bit field, a 3-bit
  count, and a counted `uint32` array.
- `0x056D` (`ServerOptionAuxPayloadMedium`) is registered with reader
  `1400a3e40`, which consumes one `uint64` field, one `uint32` field, a 4-bit
  count, and a counted `uint32` array.
- Remaining blocker: exact option readback, item/options producer timing, and
  field semantics remain unmapped; keep these packets structural-only until a
  client apply path, live sniff, or producer witness proves ownership.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~PacketPlaceholderNamingTests" -v minimal --nologo`
  passed 70/70, and full
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj -v minimal --nologo`
  passed 2483/2483.

2026-06-03 - item context / instance-reset aux packet contracts:

- Result: `Implemented` packet-contract closure only. No item-context,
  friendship, instance-reset, repair, survey, or character-admin runtime emit
  path was enabled.
- `0x00B7` (`ServerItemContextActionAck`) is registered in
  `Network_RegisterServerOpcode_0351` (`14006c290`) as a size-1 object with
  shared `ServerEmpty_ReadPayload` (`14007d8e0`). NexusForever now writes no
  payload bits for this packet instead of one raw byte.
- `0x0157` (`ServerInstanceResetAux`) is registered with reader `14008e030`,
  which consumes one 4-bit value followed by one `uint32` field. NexusForever
  now writes that mapped structural shape.
- Remaining blocker: producer/consumer semantics for the empty item-context ack
  and instance-reset scalar pair remain unmapped; keep both structural-only
  until a client apply path, live sniff, or producer witness proves ownership
  and timing.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~PacketPlaceholderNamingTests" -v minimal --nologo`
  passed 70/70, full
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj -v minimal --nologo`
  passed 2483/2483, and `InspectCodeAddress` confirmed
  `ServerInstanceResetAux_ReadPayload@14008e030`.

2026-06-03 - public-event aux packet contract naming:

- Result: `Implemented` packet-contract closure only. No public-event vote,
  scoreboard, objective, bomb, or UI runtime emit path was enabled.
- `0x0139` is registered with `ServerPublicEventAux_ReadPayload`
  (`WildStar64.exe` `14007b930`), which consumes one `uint32`, a 5-bit count,
  and a counted `uint32` array. NexusForever now names the modeled packet
  `ServerPublicEventAux` instead of carrying the stale `Raw` suffix and guards
  the 5-bit count boundary.
- `0x06F7` remains modeled as `ServerPublicEventVoteAux`: one 15-bit value plus
  one flag from reader `14007c0c0`.
- Remaining blocker: public-event producer/UI semantics, vote lifecycle timing,
  and scoreboard interaction remain unmapped; keep both packets
  structural-only until a client apply path, live sniff, or producer witness
  proves ownership and timing.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~PacketPlaceholderNamingTests" -v minimal --nologo`
  passed 70/70, and `Get-DecompCoverageSnapshot.ps1` refreshed the opcode
  coverage inventory with `ServerPublicEventAux` / `ServerPublicEventVoteAux`.

2026-06-03 - decompile coverage placeholder snapshot repair (R3-N1):

- Result: `Implemented` tooling correctness fix. No packet model, handler, or
  runtime behavior changed in this pass.
- `Get-DecompCoverageSnapshot.ps1` now classifies both `ServerNNNN` /
  `ClientNNNN` and current `Server0xNNNN` / `Client0xNNNN` opcode names as
  placeholder models.
- The coverage snapshot now reports unresolved numeric packet models with
  `decodeState=placeholder` and `placeholderModeled=True`, restoring the
  placeholder decode queue instead of folding those models into the named
  implemented counts.
- Added `test_decomp_coverage_snapshot.py`, which runs the PowerShell snapshot
  script against a synthetic opcode/model fixture and asserts the generated
  JSON and CSV classifications.
- Verification:
  `python Decomp\Analysis\test_decomp_coverage_snapshot.py`
  passed 1/1, and
  `pwsh -NoProfile -ExecutionPolicy Bypass -File Decomp\Analysis\Get-DecompCoverageSnapshot.ps1`
  refreshed the real coverage snapshot with 1056 total opcodes.

2026-06-03 - quest implementation audit block scoping (R3-Q1):

- Result: `Implemented` tooling correctness fix. No quest runtime, script, or
  objective behavior changed in this pass.
- `quest_implementation_audit.py` now parses `ScriptFilterOwnerId` owner blocks
  and evaluates script quality within each block instead of concatenating the
  entire source file for every quest id.
- The same owner-block parser now feeds script-id discovery, so multi-owner
  attributes are handled consistently by both script presence and script quality
  classification.
- Added `test_quest_implementation_audit.py`, which covers a mixed file with a
  stub-only quest class, a sibling `FollowUpQuestScript` class, and a
  multi-owner objective-update script.
- The regenerated `QUEST_IMPLEMENTATION_AUDIT.md` now reports 9
  `curated_partial_stub_script` quests, 3846 generic table-driven quests, and
  3894/3903 objective-bearing quests as completable/generic+curated.
- Verification:
  `python Tools\WikiArchiveAudit\test_quest_implementation_audit.py`
  passed 1/1, and
  `python Tools\WikiArchiveAudit\quest_implementation_audit.py`
  regenerated `Decomp\Analysis\coverage\QUEST_IMPLEMENTATION_AUDIT.md`.

2026-06-03 - quest follow-up grant refactor (R3-Q2):

- Result: `Implemented` refactor. Follow-up grant behavior is unchanged.
- `Q3671QuestScript` now inherits `FollowUpQuestScript<Q3671QuestScript>`
  instead of owning local logger/global quest manager fields and a hand-rolled
  `GrantNext` method.
- `Q3673ContactWithThaydQuestScript` now inherits
  `FollowUpQuestScript<Q3673ContactWithThaydQuestScript>`, preserves its
  `QuestState.Achieved` cinematic queue, and delegates completed-state grant
  handling to the shared base.
- `Q3480QuestScript` was already on `FollowUpQuestScript<T>`, and
  `Q5604TacticalDemolitionsQuestScript` already uses the Crimson Isle shared
  multi-quest grant helper for the `5580` / `5583` branch.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~NorthernWildsQuestChainTests|FullyQualifiedName~CrimsonIsleQuestChainTests|FullyQualifiedName~CrimsonIsleBranchInteractionTests" -v minimal --nologo`
  passed 30/30.

2026-06-03 - quest behavior coverage tracker closure (R3-Q3):

- Result: `Implemented` tracker/test-status closure. No runtime or test code was
  added in this pass because the requested behavior suites already exist in the
  current tree.
- Verified quest behavior coverage now includes follow-up grants in
  `NorthernWildsQuestChainTests`, `CrimsonIsleQuestChainTests`, and
  `TutorialQuestChainRoutingTests`.
- Achievement repeat guards are covered by `NorthernWildsUltrabotScriptTests`
  and `CrimsonIsleQuestChainTests`; cinematic-complete objective hooks are
  covered by `CrimsonIsleBranchInteractionTests` and
  `TutorialCommunicatorCheckpointTests`; targeted quest/objective gates are
  covered by `EarlyZoneEntityObjectiveCreditTests` and branch interaction tests.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~Quests" -v minimal --nologo`
  passed 149/149.

2026-06-03 - quest tracker evidence corrections (D-Q3/D-Q4):

- Result: `Mapped only` tracker correction. No runtime behavior changed.
- D-Q3 is resolved with client SQL evidence: `Quest2.tbl.sql:583` links
  objective `4770` to quest `3667`, and `QuestObjective.tbl.sql:615` defines
  objective `4770` as type `5` (`ActivateEntity`) for creature `11194`.
- `Q3667TheTower.cs` now carries that specific objective/quest evidence in its
  source comment instead of the vaguer old Quest2-only note.
- D-Q4 is resolved as stale: current `Q5594WarbotEntityScript` grants
  achievement `1730`, checks `HasCompletedAchievement` before grant, and still
  credits final-warbot objective `8249`.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~EarlyZoneEntityObjectiveCreditTests|FullyQualifiedName~CrimsonIsleQuestChainTests" -v minimal --nologo`
  passed 35/35.

2026-06-03 - support / character-admin cluster tracker reconciliation:

- Result: `Mapped only` tracker correction. Source already had typed packet
  contracts and focused packet tests for the support cluster; no new support,
  stuck, survey, or character-admin emit path was enabled.
- `0x0347` uses shared `ServerUInt32WideString_ReadPayload` (`1400980f0`);
  `0x0348`, `0x034C`, `0x034D`, `0x034E`, `0x034F`, and `0x0351` use named
  support readers at `1400a40c0`, `1400a4040`, `1400a4150`, `1400a4260`,
  `1400a3f70`, and `1400a3fd0`; `0x0349`/`0x034B` are empty shared-reader
  rows, and `0x034A`/`0x0350` are shared uint32 rows.
- Remaining blocker: support-case result/readback semantics, stuck-result
  signaling, character-admin result timing, and runtime emit ownership remain
  unmapped beyond the packet contracts.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~SupportPacketShapeTests" -v minimal --nologo`
  passed 17/17.

2026-06-03 - realm-info / mail aux tracker reconciliation:

- Result: `Mapped only` tracker correction. Source already had the typed
  `ServerRealmAuxUInt32TripletList` packet and packet-shape coverage; no realm
  info, mail-result, or account-gift emit path was enabled.
- `0x05A1` uses `ServerRealmAuxUInt32TripletList_ReadPayload`
  (`WildStar64.exe` `140080b00`): one `uint32` row count followed by counted
  rows of three `uint32` fields, sharing the triplet-row wire used by the spell
  triplet list.
- Remaining blocker: producer/consumer semantics for the realm-info/mail bridge
  remain unmapped.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~SupportPacketShapeTests.ServerRealmAuxUInt32TripletList" -v minimal --nologo`
  passed 1/1.

2026-06-03 - F-010 group 0x0438 / 0x0468 mistake correction:

- Result: `Implemented` packet-contract and emit-safety correction only. No
  broader group, raid, queue, matching, or prime-level runtime behavior was
  enabled.
- `0x0438` (`140083990`) is now modeled as
  `ServerGroupIdentityListAndUInt32Array`: group id, one leading `uint32`, a
  counted identity array, and a parallel `uint32` array. The existing
  group-member flag-change bridge emits this mapped shape, but the leading and
  parallel value semantics remain provisional until the retail consumer is
  proven.
- `0x0437` (`1400838c0`) remains `ServerGroupMemberFlagsChanged`: group id,
  32-bit member index, target identity, 32-bit changed flags, and one trailing
  promotion/source bit. `GroupPacketShapeTests` now pins this field-level
  shape in addition to the runtime fan-out coverage in `GroupFlagHandlerTests`.
- `0x0468` (`140084280`) is now modeled as
  `ServerGroupTargetIdentityPrimeLevelList`: group id, target identity, and
  counted `PrimeLevelInfo` rows via `MatchingPrimeLevelInfo_ReadPayload`
  (`1400ad150`, 15-bit world id plus 16-bit prime level). The previous
  stat/detail refresh emit was removed because it did not match the native
  reader.
- Remaining blocker: prove exact `0x0438` value semantics and `0x0468`
  producer ownership/timing through a client apply path, call-site evidence,
  live sniff, or addon/UI observation before widening group detail or role
  behavior.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~GroupPacketShapeTests|FullyQualifiedName~GroupFlagHandlerTests" -v minimal --nologo`
  passed 25/25.

2026-06-03 - F-010 matching 0x0600 structural-fit reconciliation:

- Result: `Mapped only` source/tracker correction. No matching role-selection,
  queue lifecycle, or group member role runtime behavior was enabled.
- `0x0600` (`ServerMatchingGroupMemberRoleSelection`) is registered to the same
  identity-plus-`uint32` reader as housing `0x051F`
  (`ServerHousingCommunityPlotReservation_ReadPayload`, `140086e70`). The
  managed matching wrapper is a byte-for-byte structural fit for that reader,
  and focused packet coverage compares it directly against the housing packet
  shape.
- Remaining blocker: prove the matching-specific meaning and producer/consumer
  timing of the trailing `uint32` before treating it as an authoritative role or
  selection value in runtime behavior.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~MatchingPacketShapeTests.ServerMatchingGroupMemberRoleSelection" -v minimal --nologo`
  passed 1/1.

2026-06-03 - F-014 loot sync D-L2 test-backed correction:

- Result: `Verified` tracker/test correction. Runtime behavior was already
  wired; this pass did not widen loot delivery, roll, master-loot, bindcheck, or
  auxiliary packet behavior.
- The stale backlog claim that `ServerLootItemUpdate` was never enqueued is now
  rejected. `LootInstance.BroadcastLootItemUpdate` is exercised after roll
  selection/finalisation, master assignment, and successful/deferred delivery,
  while `BroadcastLootNotification` covers remote-looter delivery feedback.
- `LootInstanceResolutionTests` now registers the provider-backed
  `IPlayerManager` used by the runtime player lookup path and asserts
  `ServerLootItemUpdate` / `ServerLootNotification` broadcasts around offline
  roll winner and deferred master-loot delivery scenarios.
- Remaining blocker: this does not resolve bind-on-pickup confirmation policy,
  exact roll/master eligibility, standalone `ServerLootCanLoot` timing, or exact
  retail loot chat/floater text.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~LootInstanceResolutionTests" -v minimal --nologo`
  passed 2/2.

2026-06-03 - F-014 loot crash-guard D-L7 non-looter boundary:

- Result: `Implemented` runtime boundary guard. `GlobalLootManager.GiveLoot`,
  `RollLoot`, and `AssignMasterLoot` now reject resolved loot instances when
  the requesting player is not a tracked looter, before owner/range checks and
  before lower-level `LootInstance` invariant methods can throw.
- `LootInstance` still keeps its `InvalidOperationException` checks for
  internal misuse; the world-session request path now logs and returns instead
  of exposing those invariant throws through handlers.
- `GlobalLootManagerTests.LootRequestBoundaries_NonLooterReturnsWithoutThrowing`
  seeds an active loot instance, drives collect/roll/master-assignment with a
  non-looter requester whose unit id matches the owner id, and asserts no
  throw, no delivery, and no outbound session packet.
- Remaining blocker: this does not resolve bind-on-pickup confirmation policy,
  exact roll/master eligibility, standalone `ServerLootCanLoot` timing,
  master-loot UI packet parity, or exact loot chat/floater/source policy.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~GlobalLootManagerTests|FullyQualifiedName~LootRequestHandlerTests|FullyQualifiedName~LootInstanceResolutionTests" -v minimal --nologo`
  passed 13/13.

2026-06-03 - F-014 offline master-loot assignee correction:

- Result: `Implemented` result/mutation correction. `LootInstance.AssignMasterLoot`
  now checks that the assignee is online through `TryGetPlayer(assignee)` before
  resolving the winner, broadcasting `ServerLootWinner`, or broadcasting
  `ServerLootItemUpdate`.
- Offline assignees now log and return `false`, leaving the item in
  `OnlyMasterLootable` state with no winner ids so the master-looter can choose
  a valid candidate instead of silently assigning a corpse item to an offline
  player.
- The existing online bag-full assignee path still resolves the winner and
  leaves the item lootable for deferred delivery once inventory space exists.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~LootInstanceResolutionTests" -v minimal --nologo`
  passed 3/3.

2026-06-03 - F-026 item-use consume ordering (R3-L1):

- Result: `Implemented` source-backed bug closure. No native labels, packet
  contracts, item-context action behavior, or unlock lifecycle semantics changed
  in this pass.
- `ClientItemUseHandler` now preflights normal activated-item stack/charge
  availability, calls `TryCastSpell`, and consumes the item only after
  `CastResult.Ok`; failed/dead/disabled casts return without deleting the
  consumable.
- `ClientItemUseHandlerTests` cover failed-cast/no-consume,
  successful-cast-before-consume ordering, and empty-stack no-cast/no-consume.
- Remaining blocker: broader item/error aux producer semantics remain blocked by
  F-026 evidence gates.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~ClientItemUseHandlerTests|FullyQualifiedName~ClientItemUseLootBagHandlerTests|FullyQualifiedName~LootBagUsageTests|FullyQualifiedName~SupportStuckHandlerTests|FullyQualifiedName~ClientActivateUnitCastHandlerTests" -v minimal --nologo`
  passed 25/25, and full
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj -v minimal --nologo`
  passed 2489/2489.

2026-06-03 - F-026 decor item-use consume ordering (R3-L2):

- Result: `Implemented` source-backed bug closure. No native labels, packet
  contracts, item-context action behavior, normal spell item-use behavior, or
  unlock lifecycle semantics changed in this pass.
- `ClientItemUseDecorHandler` now preflights stack/charge availability and
  residence access before consuming, catches `HousingException` before mutation,
  and calls `DecorCreate` only after `Inventory.ItemUse` succeeds.
- `ClientItemUseDecorHandlerTests` cover blocked residence/no-consume,
  empty-stack no-op, item-use failure/no-decor, and successful
  residence-before-consume-before-decor ordering.
- Remaining blocker: broader item/error aux producer semantics, item eligibility
  precision, and account/character unlock deltas remain blocked by separate
  F-026 evidence gates.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~ClientItemUseDecorHandlerTests" -v minimal --nologo`
  passed 4/4, broader
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~ClientItemUseDecorHandlerTests|FullyQualifiedName~ClientItemUseHandlerTests|FullyQualifiedName~ClientHousingNeighborHandlerTests|FullyQualifiedName~ResidenceManagerTests|FullyQualifiedName~ResidenceTests" -v minimal --nologo`
  passed 22/22, and full
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj -v minimal --nologo`
  passed 2493/2493.

2026-06-03 - F-013 mail return sender guard (R3-M7):

- Result: `Implemented` source-backed bug closure. No native labels, mail packet
  contracts, marketplace settlement behavior, or mail content-type persistence
  behavior changed in this pass.
- `MailManager.ReturnMail` now rejects mail whose sender is not a player/GM with
  a non-zero `SenderId`, returning `MailCannotReturn` without removing it from
  available mail, queueing outgoing mail, calling `IMailItem.ReturnMail`, or
  sending `ServerMailUnavailable`.
- `MailItem.ReturnMail` enforces the same invariant before mutating
  `RecipientId`, subject, or flags.
- `MailManagerDeliveryTests` cover non-player marketplace sender rejection, and
  `MailItemTransactionTests` cover the direct model no-mutation invariant.
- Remaining blocker: final marketplace settlement atomicity and broader
  F-005/F-013 marketplace/offline settlement flows remain open.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~MailManagerDeliveryTests|FullyQualifiedName~MailItemTransactionTests|FullyQualifiedName~MarketplaceMailSettlementTests" -v minimal --nologo`
  passed 21/21, and full
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj -v minimal --nologo`
  passed 2495/2495.

2026-06-03 - F-005/F-013 marketplace settlement delivery guards (R3-M4/R3-M5):

- Result: `Implemented` source-backed delivery-failure guard. No native labels,
  packet contracts, CREDD behavior, marketplace aux producer semantics, or mail
  result packet contracts changed in this pass.
- `GlobalMarketplaceManager` now checks item delivery before destructive
  order/seller-credit mutations for auction expiry, commodity sell-order expiry,
  commodity cross-match fill, and commodity sell-order cancel where item
  delivery must return to an owner/buyer.
- Auction and commodity item delivery helpers now return `bool`, restore
  temporary item ownership on failed delivery, and gate mail fallback on
  `MarketplaceMailDelivery.IsAvailable`, leaving listings/orders active in
  no-mail delivery-failure contexts.
- Commodity in-inventory delivery uses max-stack empty-slot preflight from the
  handler-provided `IItemManager`. This is intentionally conservative and does
  not yet model exact retail partial-stack routing.
- Remaining blocker: final marketplace settlement DB transaction/save
  atomicity, exact commodity multi-order partial-fill/partial-stack semantics,
  and marketplace mail content persistence remain separate F-005/F-013 work.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~MarketplaceAuctionHandlerTests" -v minimal --nologo`
  passed 15/15, broader
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~MarketplaceAuctionHandlerTests|FullyQualifiedName~MarketplaceOwnedListHandlerTests|FullyQualifiedName~MarketplaceMailSettlementTests" -v minimal --nologo`
  passed 21/21, and full
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj -v minimal --nologo`
  passed 2502/2502.

2026-06-03 - F-005 marketplace persisted microchip overflow guard (#13):

- Result: `Implemented` source-backed corrupt-row guard. No native labels,
  packet contracts, CREDD behavior, marketplace aux producer semantics, or item
  microchip gameplay semantics changed in this pass.
- Current source names the stale backlog `UnknownArray` field as
  `MicrochipIds`; `GlobalMarketplaceManager.DeserializeMicrochipIds` now treats
  both `FormatException` and `OverflowException` from persisted DB text as
  corrupt input, logs the value, and returns an empty list rather than throwing
  during marketplace auction load.
- Remaining blocker: this does not change final marketplace settlement DB
  transaction/save atomicity or item microchip install gameplay. Broader
  persisted commodity row validation was closed later under R3-M9.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~MarketplaceAuctionHandlerTests" -v minimal --nologo`
  passed 16/16, broader
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~MarketplaceAuctionHandlerTests|FullyQualifiedName~MarketplaceOwnedListHandlerTests|FullyQualifiedName~MarketplaceMailSettlementTests" -v minimal --nologo`
  passed 22/22, and full
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj -v minimal --nologo`
  passed 2503/2503.

2026-06-03 - F-005 auction search page overflow guard (R3-M8):

- Result: `Implemented` source-backed paging guard. No native labels, packet
  contracts, auction filter semantics, sort semantics, or marketplace aux
  producer semantics changed in this pass.
- `GlobalMarketplaceManager.SearchAuctions` now computes the requested skip as
  `ulong` and returns an empty page when the skip exceeds the `int` boundary
  required by `Enumerable.Skip`, preventing huge client `Page` values from
  throwing through the handler.
- `MarketplaceAuctionHandlerTests.SearchAuctions_HugePage_ReturnsEmptyPageWithoutOverflow`
  pins `uint.MaxValue` paging with an empty result and echoed `CurrentPage`.
- Remaining blocker: accepted property/rune/equippable filters, property sort,
  persistent listing/search storage, and marketplace producer semantics remain
  separate F-005 work.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~MarketplaceAuctionHandlerTests" -v minimal --nologo`
  passed 17/17, broader
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~MarketplaceAuctionHandlerTests|FullyQualifiedName~MarketplaceOwnedListHandlerTests|FullyQualifiedName~MarketplaceMailSettlementTests" -v minimal --nologo`
  passed 23/23, and full
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj -v minimal --nologo`
  passed 2504/2504.

2026-06-03 - F-025 immediate sprint guard:

- Result: `Implemented` evidence-boundary guard only. No entity-stat aux,
  map-tracked-unit, visibility, public-event, or CSI producer semantics were
  promoted.
- `EntityCreateAuxiliaryEmissionTests` now asserts
  `BuildEntityCreateAuxPackets()` emits the mapped entity-create aux family
  without adding blocked entity-stat aux packets (`0x0889`, `0x08CC`,
  `0x08F4`, `0x0939`, `0x093D`, `0x093E`) or map-tracked-unit packets
  (`0x0848`, `0x0849`).
- `ENTITY_AUX_DECODE_ROADMAP.md` records this as a pre-create boundary guard;
  the producer blocker remains the same: native send-site or live sniff/order
  proof for tracked-unit id allocation, update cadence, disable timing,
  `TrackingSlotId` selection, and per-opcode entity-stat aux consumers.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore -m:1 -v minimal --nologo -p:UseSharedCompilation=false --filter "FullyQualifiedName~EntityCreateAuxiliaryEmissionTests|FullyQualifiedName~EntityAuxiliaryPacketShapeTests"`
  passed 19/19.

2026-06-03 - F-025 WorldSocket chain catalog refresh:

- Result: `Mapped only`. No entity-stat aux producer, map-tracked-unit producer,
  or runtime emitter was promoted.
- Rechecked `FindImmediateInstructions 0x15b0`, the cached `WorldSocket`
  fragments, `function_pointer_family_slots.csv`, and
  `InspectCodeAddresses 140042370 1404d56b0 1404d60f0`.
- Confirmed the known native `WorldSocket+0x15b0` families remain diagnostic,
  console, options/addons, and Fortune. `140012780` is a `WorldSocket` object
  vtable path (`PTR_FUN_140b55120` slot `+0x08`), not a newly inserted
  handler-node source.
- Tightened `function_labels.csv` comments for `WorldSocketOptionsNode_Ctor`
  and `WorldSocketPersistentNode15A8_Ctor` to distinguish installed vtable
  pointers (`PTR_FUN_140b558c0`, `PTR_FUN_140b690f0`) from their larger family
  blocks (`140b558b0`, `140b690c0`).
- Remaining blocker: static chain discovery still has no nontrivial
  `vtable+0x58` entity-stat aux consumer; F-025 still needs a new linked-list
  consumer or live sniff/order witness before server emitters can be widened.

2026-06-03 - F-004 immediate sprint guard:

- Result: `Implemented` evidence-boundary guard only. No housing neighborhood
  production emitter, session trigger, random-community flow, or residence aux
  ordering behavior was promoted.
- `HousingAuxiliaryPacketEmitterTests` now asserts
  `BuildResidenceSessionPackets()` emits only the seven mapped residence/basics
  aux packets and does not add blocked housing neighborhood packets (`0x0501`,
  `0x0506`).
- `RUNTIME_BLOCKER_RE_RESULTS.md` records this as the current F-004 NF
  boundary. The producer blocker remains the same: live sniff/order proof or a
  native send-site for `ServerHousingNeighborhoodList` trigger timing, row
  source, `NeighborhoodId`, and tail-field semantics.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore -m:1 -v minimal --nologo -p:UseSharedCompilation=false --filter "FullyQualifiedName~HousingAuxiliaryPacketEmitterTests|FullyQualifiedName~HousingPacketShapeTests"`
  passed 24/24.

2026-06-03 - F-004 housing visit/decor create hardening (R3-H1/R3-H2):

- Result: `Implemented` source-backed bug closure. No neighborhood-list
  producer, housing aux emitter, decor ownership/unlock, refund, entitlement,
  or community-session semantics changed in this pass.
- Private residence visits now return `Visit_Private` only when the visitor
  lacks modify access; owners and authorized modifiers continue to the mapped
  residence entrance teleport path.
- `ClientHousingDecorUpdate` create handling now validates paid decor rows
  before mutation: unsupported colour shift and negative scale still reject the
  packet, and non-crate creates reuse the move-path plot-position validation.
  Currency debit and `HousingDecorPurchase` achievement updates happen only
  after validation and `DecorCreate`.
- `ClientHousingVisitResidenceHandlerTests` pins private-with-modify teleport
  behavior. `ResidenceMapInstanceInteriorWallpaperTests` now also pins invalid
  colour-shift and invalid plot-position creates with no currency debit and no
  residence decor creation.
- Remaining blockers: F-004 still needs exact neighborhood-list producer
  trigger/timing, deeper ownership/unlock/refund precision, entitlement checks,
  and community/session edge proof before claiming retail-complete housing.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~Housing" -v minimal --nologo`
  passed 84/84, and
  `dotnet build Source\NexusForever.WorldServer\NexusForever.WorldServer.csproj --no-restore -v minimal --nologo`
  passed with 0 warnings and 0 errors.

2026-06-03 - F-008 immediate sprint guard:

- Result: `Implemented` evidence-boundary guard only. No crafting aux,
  current-craft cadence, discovery-unlock mutation, random-output parity, or
  station service-key behavior was promoted.
- `CraftingSimpleCraftHandlerTests` now asserts complex craft requests with
  non-zero `CraftStats`, `ApSpSplitDelta`, and `ChargeCounts` still produce the
  verified fixed-recipe `ServerCraftingFinish` path without adding blocked
  `ServerCraftingCurrentCraft` (`0x0854`) or crafting aux packets (`0x084B`,
  `0x0855`).
- `RUNTIME_BLOCKER_RE_RESULTS.md` records this as the current F-008 NF
  boundary. The producer blocker remains the same: native enqueue path or live
  capture proof for current-craft timing, aux packet intent, field semantics,
  and discovery/hot-cold mutation behavior beyond `ServerCraftingFinish`.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore -m:1 -v minimal --nologo -p:UseSharedCompilation=false --filter "FullyQualifiedName~CraftingSimpleCraftHandlerTests|FullyQualifiedName~CraftingPacketShapeTests|FullyQualifiedName~CraftingDiscoveryEvaluatorTests|FullyQualifiedName~CraftingAdditiveHandlerTests|FullyQualifiedName~CraftingLootIdCraftHandlerTests"`
  passed 45/45.

2026-06-04 - F-008 crafting aux/rune bridge recheck:

- Result: mixed closure. `0x084B` and `0x0855` remain `Mapped / Blocked`;
  durable rune socket/install persistence is `Implemented`.
- Ghidra MCP rechecked `ServerCraftingAuxFourUInt32FloatUInt32_ReadPayload`
  (0x1400a3af0), `ServerUInt32AndTwoFloats_ReadPayload` (0x140081df0),
  `Crafting_HandleServerCraftingFinish` (0x1405e6690), and
  `Crafting_HandleServerCraftingCurrentCraft` (0x1405e6830). The aux opcodes
  still have reader/registration evidence only; the finish/current-craft
  handlers do not prove an enqueue site or field semantics.
- NF source now closes the stale durable-rune bridge blocker through
  `item.runeSlots`, `ItemRuneSlotsCodec`, `ItemRuneNetworkWire`,
  `RandomGlyphData`/`Glyphs`, and migration
  `20260531224115_ItemMicrochipIdsAndRuneSlots`. The remaining rune blockers
  are non-success sigil result precision and any separate microchip install
  mutator that is not already covered by persisted microchip ids.
- Added `ItemRuneSocketTests.RuneSlotsCodec_RoundTripsSlotTypesAndInstalledRuneItemIds`
  so installed rune Item2 ids and slot types stay pinned through the durable
  codec.
- Verification: default `dotnet test` hit a local `NexusForever.WorldServer`
  DLL lock (PID 48388), so the focused run used an isolated output path:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~Crafting|FullyQualifiedName~ItemRuneSocketTests" -v minimal --nologo -m:1 -p:UseSharedCompilation=false -p:OutDir=I:\GIT\NexusForever\artifacts\testbin\f008-rune\`
  passed 95/95.

2026-06-03 - F-010 immediate sprint guard:

- Result: `Implemented` evidence-boundary guard only. No replacement backfill,
  accepted-replacement merge, non-zero raid queue status, queue anchor timing,
  or teleport-into-running-instance behavior was promoted.
- `MatchingLookingForReplacementsValidationTests` now asserts
  `ClientMatchingMatchInitiateLookingForReplacementsHandler` and
  `ClientMatchingStopLookingForReplacementsHandler` accept an in-progress match
  surface without enqueueing blocked replacement/backfill/status packets.
- `MATCHING_IMPLEMENTATION_STATUS.md` records this as the current F-010
  replacement boundary. The producer blocker remains the same: retail sniff or
  native evidence for replacement queue anchor timing, multi-replacement
  sequencing, accepted replacement merge, non-zero `ServerRaidQueueStatus`, and
  teleport into the running instance.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore -m:1 -v minimal --nologo -p:UseSharedCompilation=false --filter "FullyQualifiedName~MatchingLookingForReplacementsValidationTests|FullyQualifiedName~MatchingPacketShapeTests|FullyQualifiedName~ClientRaidInfoRequestHandlerTests"`
  passed 40/40 after MSBuild retried a transient locked testhost DLL.

Combined immediate-sprint guard verification:

- `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore -m:1 -v minimal --nologo -p:UseSharedCompilation=false --filter "FullyQualifiedName~EntityCreateAuxiliaryEmissionTests|FullyQualifiedName~EntityAuxiliaryPacketShapeTests|FullyQualifiedName~HousingAuxiliaryPacketEmitterTests|FullyQualifiedName~HousingPacketShapeTests|FullyQualifiedName~CraftingSimpleCraftHandlerTests|FullyQualifiedName~CraftingPacketShapeTests|FullyQualifiedName~CraftingDiscoveryEvaluatorTests|FullyQualifiedName~CraftingAdditiveHandlerTests|FullyQualifiedName~CraftingLootIdCraftHandlerTests|FullyQualifiedName~MatchingLookingForReplacementsValidationTests|FullyQualifiedName~MatchingPacketShapeTests|FullyQualifiedName~ClientRaidInfoRequestHandlerTests"`
  passed 128/128.

2026-06-03 - F-016/F-020 spell follow-up guard:

- Result: `Implemented` evidence-boundary guard only. No standalone spell
  damage, nested damage, target-report/sync/list, threshold, wrapper-tier, or
  wrapper-node production send path was promoted.
- `TutorialCombatMineEntityScriptTests` now asserts a normal delayed spell
  lifecycle emits the verified `ServerSpellStart`, `ServerSpellGo`, and
  `ServerSpellFinish` packets without adding blocked follow-ups from the
  `0x07F5..0x0819` family.
- `SPELL_BROADCAST_ROADMAP.md` records this as the current spell-follow-up
  runtime boundary. The producer blocker remains the same: fixture/sniff
  evidence for lifecycle timing and field/list semantics before enabling any
  production send path beyond the proven lifecycle packets.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore -m:1 -v minimal --nologo -p:UseSharedCompilation=false --filter "FullyQualifiedName~TutorialCombatMineEntityScriptTests|FullyQualifiedName~SpellBroadcastPacketShapeTests|FullyQualifiedName~SpellDamageTrailingRowShapeTests|FullyQualifiedName~CombatLogPacketShapeTests"`
  passed 21/21.

2026-06-03 - F-031 Fortune catalog boundary guard:

- Result: `Implemented` evidence-boundary guard only. No exact retail
  per-item weight, active rotation, storefront synchronisation, money-reward,
  or probability producer behavior was promoted.
- `FortuneSessionManagerTests` now asserts `SendStatus()` forwards the mapped
  `ServerFortuneRewards` item/probability catalog while keeping money reward
  and money probability arrays empty.
- `FORTUNE_WEIGHT_AUDIT.md` records this as the current F-031 runtime emitter
  boundary. The producer blocker remains the same: retail `ServerFortuneRewards`
  capture or storefront/catalog rotation evidence for item ids, money rows,
  probability arrays, and active rotation context before changing emulator
  rarity-tier weights.
- Verification:
 `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore -m:1 -v minimal --nologo -p:UseSharedCompilation=false --filter "FullyQualifiedName~Fortune"`
  passed 20/20.

2026-06-04 - F-031 Fortune reset-code / active-rotation recheck:

- Result: mixed closure. `ServerFortuneReset.Unknown` is now
  `ServerFortuneReset.ResetCode`; exact Madame Fay per-item weights and active
  rotation remain `Mapped / Blocked`.
- Ghidra MCP rechecked `Fortune_ApplyRewards` (0x1407292a0),
  `Fortune_ApplyReset` (0x1407291f0), `FortunesLib_GetFortunesLootList`
  (0x140766370), `ServerFortuneRewards_ReadPayload` (0x140081f60), and the
  `FortuneNode_ApplyServerFortunePackets` dispatch node (0x1404d60f0).
  Rewards are still client-side application of server-provided item/money arrays
  plus parallel probabilities; no native active-rotation source was found.
- `Fortune_ApplyReset` maps reset code `3` to the click-empty reset path, so NF
  renamed the packet field and the session-manager/test constants from reset
  value to reset code without changing behavior.
- Remaining blocker: retail `ServerFortuneRewards` capture or storefront-server
  catalog dump with active account-item ids, weights/probabilities, and any
  money rows before replacing rarity-tier emulator weights.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~Fortune" -v minimal --nologo -m:1 -p:UseSharedCompilation=false -p:OutDir=I:\GIT\NexusForever\artifacts\testbin\f031-fortune\`
  passed 20/20.

2026-06-03 - F-023 Rider's Reef smoke harness preset:

- Result: `Implemented` evidence-harness progress only. No Rider's Reef quest,
  terminal, hoverboard, mine, CSI, destination welcome quest, or route behavior
  was promoted.
- `Start-BlockerEvidenceHarness.ps1` now has `-RidersReefSmoke`, creating an
  `F-023-riders-reef-smoke` bundle with default Rider's Reef/destination world
  ids, faction-aware negative cases, and
  `f-023-riders-reef-smoke-targets.md`.
- The worksheet captures both Exile and Dominion character creation through
  final terminal handoff into Everstar Grove, Northern Wilds, Crimson Isle, and
  Levian Bay, plus re-entry/recovery and negative-case evidence.
- Verification:
  `python Decomp\Analysis\test_blocker_evidence_harness_presets.py`
  passed 1/1.

2026-06-03 - F-014 duplicate loot amount overflow guard (R3-L4):

- Result: `Implemented` source-backed crash/data-corruption guard. No loot
  eligibility, roll, bind-on-pickup, parent-source, `ServerLootCanLoot`, or
  packet producer semantics changed in this pass.
- `LootInstanceItem.TryAddToAmount` now computes duplicate-row amount merges
  as `ulong`, rejects overflow with a warning, and preserves the existing loot
  row amount instead of wrapping `uint`.
- `LootInstanceResolutionTests` covers normal duplicate-row merging and the
  overflow rejection boundary.
- Remaining blocker: F-014 still needs retail proof for `ServerLootCanLoot`
  emit timing and exact parent source tracking.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore -m:1 -v minimal --nologo -p:UseSharedCompilation=false --filter "FullyQualifiedName~LootInstanceResolutionTests|FullyQualifiedName~LootInstanceDeliveryTests|FullyQualifiedName~GlobalLootManagerTests"`
  passed 13/13.

2026-06-03 - F-005 same-bidder auction delta escrow guard (R3-M1):

- Result: `Implemented` source-backed marketplace accounting guard. No auction
  filter/sort semantics, commodity matching, CREDD owned-order row, marketplace
  aux producer, mail content persistence, or DB transaction atomicity behavior
  changed in this pass.
- `GlobalMarketplaceManager.BuyAuction` now treats an existing top bid from the
  same player as held escrow, checks/subtracts only the bid delta, and avoids
  refunding the bidder to themselves before increasing the accepted bid.
- `MarketplaceAuctionHandlerTests.AuctionBid_SameTopBidderIncreaseChargesOnlyDelta`
  covers the first bid plus same-bidder increase path and asserts the second
  affordability check/debit is only the delta.
- Remaining blocker: F-005 still needs retail property/rune/equippable filter
  implementation, corrupt commodity row quarantine, CREDD owned-order tail
  proof, and final settlement transaction/save atomicity work.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore -m:1 -v minimal --nologo -p:UseSharedCompilation=false --filter "FullyQualifiedName~MarketplaceAuctionHandlerTests"`
  passed 18/18.
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore -m:1 -v minimal --nologo -p:UseSharedCompilation=false --filter "FullyQualifiedName~MarketplaceAuctionHandlerTests|FullyQualifiedName~MarketplaceOwnedListHandlerTests|FullyQualifiedName~MarketplaceMailSettlementTests"`
  passed 24/24.

2026-06-03 - F-005 ForceImmediate commodity guard (R3-M2):

- Result: `Implemented` source-backed commodity immediate-order guard. No
  auction filter/sort semantics, CREDD owned-order row, marketplace aux
  producer, mail content persistence, corrupt commodity DB-row quarantine, or
  DB transaction atomicity behavior changed in this pass.
- `GlobalMarketplaceManager.PostCommodityOrder` now treats
  `CommodityOrder.ForceImmediate` as a non-resting immediate match: it skips
  active commodity order slot checks, does not add/persist the incoming order,
  attempts the normal price-prioritised cross-match, then refunds unmatched buy
  escrow or returns unmatched sell items through inventory/mail.
- Commodity match persistence now updates/deletes only resting orders, so
  immediate remainders cannot throw on missing DB rows. Buy partial-fill refund
  math now subtracts the filled purchase cost before refunding price improvement
  or unmatched escrow.
- `MarketplaceAuctionHandlerTests` now covers an unmatched immediate buy at the
  retail free slot cap and a partial immediate buy fill at a lower sell price,
  asserting no resting buyer order remains and the buyer receives only the
  price-improvement plus unmatched-escrow refunds. It also covers a full
  immediate sell fill where the seller has no free return slots, proving the
  sell-side preflight is based on unmatched remainder rather than submitted
  quantity.
- Remaining blocker: F-005 still needs retail property/rune/equippable filter
  implementation, CREDD owned-order tail proof, exact multi-order partial-stack
  routing beyond current guard tests, and final settlement transaction/save
  atomicity work.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~MarketplaceAuctionHandlerTests" -v minimal --nologo`
  passed 21/21.
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~MarketplaceAuctionHandlerTests|FullyQualifiedName~MailManagerDeliveryTests|FullyQualifiedName~MailItemTransactionTests" -v minimal --nologo`
  passed 38/38.
  `git diff --check`
  passed with only existing LF-to-CRLF working-copy warnings.

2026-06-03 - F-005 persisted commodity row quarantine (R3-M9):

- Result: `Implemented` source-backed marketplace load guard. No auction
  filter/sort semantics, commodity runtime match rules, CREDD owned-order row,
  marketplace aux producer, mail content persistence, or DB transaction
  atomicity behavior changed in this pass.
- `GlobalMarketplaceManager.Initialise` now validates persisted commodity rows
  before adding them to the in-memory order book. It rejects zero ids/owners,
  zero or missing Item2 ids when game tables are available, zero quantity/price
  fields, legacy persisted `ForceImmediate` rows, escrow multiplication
  overflow/mismatch, and list/expiration ordering where both values are present.
- Invalid rows are skipped with a warning before expiration, cancel, or match
  paths can refund credits or deliver items from corrupt DB state.
- `MarketplaceAuctionHandlerTests.PersistedCommodityOrderValidation_RejectsUnsafeRows`
  covers valid rows, escrow mismatch, escrow overflow, legacy immediate rows,
  missing Item2 ids, invalid duration ordering, zero owner, and zero quantity.
- Remaining blocker: F-005 still needs retail property/rune/equippable filter
  implementation, CREDD owned-order tail proof, exact multi-order partial-stack
  routing beyond current guard tests, and final settlement transaction/save
  atomicity work.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~MarketplaceAuctionHandlerTests" -v minimal --nologo`
  passed 22/22.
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~MarketplaceAuctionHandlerTests|FullyQualifiedName~MailManagerDeliveryTests|FullyQualifiedName~MailItemTransactionTests" -v minimal --nologo`
  passed 39/39.
  `dotnet build Source\NexusForever.Game\NexusForever.Game.csproj --no-restore -v minimal --nologo`
  passed with 0 warnings and 0 errors.

2026-06-03 - F-015 PvP pending toggle-off cooldown (R3-P1):

- Result: `Implemented` transient PvP toggle-off latch. No duel observer,
  battleground/arena scoring, forced-map PvP, reward, stat, or durable cooldown
  persistence behavior changed in that pass; durable cooldown persistence was
  closed by the later 2026-06-03 F-015 persistence pass below.
- `ClientPvpToggleFlagsHandler` no longer clears `PvPFlag.Enabled` directly for
  client toggle-off. It delegates to `IPlayer.RequestPvPFlagDisable`, matching
  the packet comment that toggle-off starts a cooldown latch while toggle-on can
  cancel it.
- `Player` now owns the pending disable timer. While the timer runs, PvP remains
  enabled and the client receives `ServerPvpCooldownUpdate`; on expiry the player
  clears only the non-forced PvP-enabled bit and receives
  `ServerPvpCooldownClear`. Toggle-on cancels the pending disable and keeps or
  sets `PvPFlag.Enabled`.
- `PvpFlagCooldownTests` covers handler off/on routing, pending-disable expiry,
  and pending-disable cancellation.
- Remaining blocker: F-015 still needs observer/reward parity, forced-map PvP
  semantics, and battleground/arena score/reward parity.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~Pvp" -v minimal --nologo`
  passed 32/32.
  `dotnet build Source\NexusForever.WorldServer\NexusForever.WorldServer.csproj --no-restore -v minimal --nologo`
  passed with 0 warnings and 0 errors.

2026-06-03 - F-010 matching leave/cleanup snapshot hardening (R3-Match1/R3-Match2):

- Result: `Implemented` state-lifecycle bug fix. No replacement backfill,
  queue-role semantics, raid-lock persistence, group packet producer semantics,
  or cross-realm matching behavior changed in this pass.
- `MatchingCharacter.GetMatchingCharacterQueues()` now returns a materialized
  snapshot rather than live dictionary values, so all-queue leave/requeue/status
  paths cannot observe collection mutation while queue removal proceeds.
- `Match.MatchCleanup()` now snapshots each team's members before invoking
  `MatchLeave`, which removes the same member from the team and character-team
  indexes.
- `MatchingCharacterStatusTests.GetMatchingCharacterQueues_ReturnsSnapshotWhenQueuesMutate`
  covers the queue snapshot behavior after one queue is removed.
  `MatchCleanupSnapshotTests.MatchCleanup_SnapshotsTeamMembersBeforeLeaveMutatesTeam`
  covers cleanup against a live-removing team fixture that would fail on the
  old live enumeration.
- Remaining blocker: F-010 still needs replacement server backfill/merge proof,
  durable raid-lock semantics, full cross-realm pool semantics, and exact
  `0x0438`, `0x0468`, `0x0600`, and `0x0718` producer/value semantics.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~Matching" -v minimal --nologo`
  passed 90/90.

2026-06-03 - F-010 group instance difficulty authority (R3-G1):

- Result: `Implemented` server-state bug fix. No durable raid-lock, non-zero
  raid queue, replacement backfill/merge, prime-level, rally, or deeper group
  producer semantics changed in this pass.
- `ClientGroupSetInstanceDifficultyHandler` now publishes
  `GroupInstanceDifficultyUpdateMessage` instead of mutating only the requesting
  player's `InstanceDifficulty`. The group server handles the request through
  `Group.SetInstanceDifficultyAsync`, requiring the updater to be the group
  leader and returning the normal `GroupActionResultMessage`.
- `GroupInstanceDifficultyUpdatedHandler` caches the authoritative group
  `InstanceDifficulty`, syncs online members' `Player.InstanceDifficulty`, and
  broadcasts `ServerGroupInstanceDifficultyResponse` with the setter unit guid.
  The new difficulty is runtime-only group state; no group DB schema migration
  was added.
- `GroupInstanceDifficultyHandlerTests` cover request publication without local
  mutation and authoritative update/broadcast/member sync. `GroupStateManagerTests`
  now pins difficulty preservation through update/member-removal/mapping paths.
- Remaining blocker: F-010 still needs replacement server backfill/merge proof,
  durable raid-lock semantics, full cross-realm pool semantics, non-zero
  `ServerRaidQueueStatus`, and exact group/matching producer/value semantics.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~Group" -v minimal --nologo`
  passed 155/155,
  `dotnet build Source\NexusForever.WorldServer\NexusForever.WorldServer.csproj --no-restore -v minimal --nologo`
  passed with 0 warnings and 0 errors, and
  `dotnet build Source\NexusForever.Server.GroupServer\NexusForever.Server.GroupServer.csproj --no-restore -v minimal --nologo`
  passed with 0 warnings and 0 errors.

2026-06-03 - F-005 unsupported auction filter rejection (R3-M3):

- Result: `Implemented` validation guard by rejection. No retail property,
  rune-slot, equippable-by, or property-sort search semantics were invented.
- `GlobalMarketplaceManager.ValidateAuctionSearch` now rejects
  `AuctionSort.Property`, and `ValidateAuctionFilter` rejects property min/max,
  rune-slot, and equippable-by filters. This closes the bug where these
  client-supplied filters were accepted but ignored or defaulted to min-bid
  sorting.
- `MarketplaceAuctionHandlerTests` covers unsupported auction filters and
  property sort validation rejection.
- Remaining blocker: F-005 still needs retail property/rune/equippable filter
  implementation, CREDD owned-order tail proof, exact multi-order partial-stack
  routing beyond current guard tests, and final settlement transaction/save
  atomicity work.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~MarketplaceAuctionHandlerTests" -v minimal --nologo`
  passed 24/24.
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~MarketplaceAuctionHandlerTests|FullyQualifiedName~MailManagerDeliveryTests|FullyQualifiedName~MailItemTransactionTests" -v minimal --nologo`
  passed 41/41.
  `dotnet build Source\NexusForever.Game\NexusForever.Game.csproj --no-restore -v minimal --nologo`
  passed with 0 warnings and 0 errors.

2026-06-03 - F-005/F-013 marketplace mail content persistence (R3-M6):

- Result: `Implemented` schema-backed mail content-type persistence. No
  marketplace transaction/save atomicity, delete-state parity, or exact
  expiration timing behavior changed in this pass.
- Added character DB migration `20260603120000_MailContentType` for
  `character_mail.contentType` and updated `CharacterMailModel`,
  `CharacterContext`, and the model snapshot.
- `MailItem` now persists `ContentType` on create and reloads explicit non-zero
  stored values. Legacy rows with `contentType = 0` still fall back to sender
  type, so older item/commodity auction mail continues to display as
  `AuctionWon` instead of becoming player-message mail.
- `MarketplaceMailSettlementTests` now covers persisted marketplace mail
  preserving `AuctionExpired` and legacy marketplace mail falling back to
  sender-type defaults.
- Remaining blocker: F-005/F-013 still need final marketplace/offline DB
  transaction/save atomicity; F-013 also keeps broader delete-state parity and
  exact expiration timing open.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~MarketplaceMailSettlementTests" -v minimal --nologo`
  passed 6/6.
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~Mail" -v minimal --nologo`
  passed 27/27.
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~MarketplaceAuctionHandlerTests|FullyQualifiedName~MailManagerDeliveryTests|FullyQualifiedName~MailItemTransactionTests|FullyQualifiedName~MarketplaceMailSettlementTests" -v minimal --nologo`
 passed 47/47.
 `dotnet build Source\NexusForever.Database.Character\NexusForever.Database.Character.csproj --no-restore -v minimal --nologo`
  passed with 0 warnings and 0 errors.

2026-06-03 - F-005/F-013 marketplace DB failure rollback guards:

- Result: `Implemented` for the next non-blocked DB/save failure paths. This
  pass does not claim full cross-resource transaction parity.
- `GlobalMarketplaceManager.Persist` now returns success/failure and logs failed
  character DB saves instead of letting marketplace mutations throw after
  in-memory state changes.
- Auction post insert failures remove the transient auction, restore the listed
  item to the seller inventory when it had already been removed, and roll back
  the generated auction id when safe.
- Commodity order insert failures remove the transient order, refund buy-order
  escrow or recreate sell-order items, and roll back the generated order id when
  safe.
- Non-buyout auction bid update failures restore the previous bid/top-bidder
  state and refund the bidder's charged amount.
- Marketplace mail persistence failures now restore attached item owner state
  before returning `false`; auction buyout/expiry paths then leave the auction
  active and avoid buyer debit/seller credit/order removal.
- Settled auction delete persistence now removes the auction row while saving
  the moved item state; `SaveSettledAuctionItem_SavesWithoutEnqueueingDelete`
  pins that this path must not enqueue item deletion.
- Remaining blocker: final settlement delete/update failures still need an
  architectural transaction boundary spanning auction/order rows, mail rows,
  item state, and online currency/inventory mutations. Current item listing
  removal uses the same item row that delivery later needs, so simply moving the
  delete save before delivery would split listing removal from delivery state.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~MarketplaceAuctionHandlerTests.PostAuction_WhenPersistFails|FullyQualifiedName~MarketplaceAuctionHandlerTests.AuctionBid_WhenPersistFails|FullyQualifiedName~MarketplaceAuctionHandlerTests.CommodityBuyOrderSubmit_WhenPersistFails|FullyQualifiedName~MarketplaceAuctionHandlerTests.CommoditySellOrderSubmit_WhenPersistFails|FullyQualifiedName~MarketplaceAuctionHandlerTests.AuctionBuyout_WhenMailPersistFails" -v minimal --nologo`
  passed 5/5.
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~MarketplaceAuctionHandlerTests|FullyQualifiedName~MarketplaceAccountLimitsTests|FullyQualifiedName~MarketplaceOwnedListHandlerTests|FullyQualifiedName~MailManagerDeliveryTests|FullyQualifiedName~MailItemTransactionTests|FullyQualifiedName~MarketplaceMailSettlementTests" -v minimal --nologo`
  passed 62/62.
  `dotnet build Source\NexusForever.Game\NexusForever.Game.csproj --no-restore -v minimal --nologo`
  passed with 0 warnings and 0 errors.

2026-06-03 - F-004 housing edit-mode server state:

- Result: `Implemented` server-side state for `ClientHousingEditModeHandler`
  without inventing an unmapped ack or broadcast packet.
- `ClientHousingEditModeHandler` still validates residence-map context and
  `CanModifyResidence`, then delegates to `IResidenceMapInstance.SetEditMode`.
- `ResidenceMapInstance` now records the residence currently being edited per
  player character id, validates that the target residence is loaded on the
  current residence map, exposes `TryGetEditModeResidence`, and clears edit
  state on disable, player removal, and residence removal.
- Remaining blocker: retail ack/broadcast packet semantics remain unmapped;
  no new housing output packet is emitted until client consumer/sniff evidence
  identifies the correct producer behavior.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~ClientHousingEditModeHandlerTests|FullyQualifiedName~ResidenceMapInstanceEditModeTests" -v minimal --nologo`
  passed 9/9.
  `dotnet build Source\NexusForever.Game\NexusForever.Game.csproj --no-restore -v minimal --nologo`
  passed with 0 warnings and 0 errors.

2026-06-03 - F-014 harvest recipient range/preflight (R3-L3):

- Result: `Implemented` group harvest delivery hardening. No corpse-loot rule,
  bind-on-pickup confirmation, loot feedback, or retail harvest split semantics
  changed in this pass.
- `GlobalLootManager.TryDeliverHarvestLoot` now filters same-map group members
  by loot range from the harvester, preflights each candidate with generated
  loot validation and static-item delivery checks, and handles the single valid
  recipient case before applying round-robin selection.
- `GlobalLootManagerTests` covers out-of-range same-map members being excluded
  from round-robin harvest delivery and inventory-full members being skipped
  before the round-robin cursor advances.
- Remaining blocker: F-014 still needs retail proof for `ServerLootCanLoot`,
  vacuum feedback semantics, all-pass winner/no-delivery UX, and exact
  bind-on-pickup confirmation timing.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~GlobalLootManagerTests" -v minimal --nologo`
  passed 8/8.
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~Loot" -v minimal --nologo`
  passed 61/61.

2026-06-03 - F-014 loot vacuum pending-state feedback (D-L8):

- Result: `Implemented` conservative feedback for vacuum no-op on pending loot.
  No unproven item-error code, roll resolution, or master-loot assignment policy
  was added.
- `GlobalLootManager.GiveAllLootInRange` now tracks when every in-range item
  remained undeliverable because it is pending roll/master state. If vacuum
  delivers nothing and the loot instance remains active, the server refreshes
  `ServerLootNotify` for the requester so the client sees the pending state
  rather than a silent no-op.
- `GlobalLootManagerTests.GiveAllLootInRange_PendingRollOnlyRefreshesLootNotify`
  pins the no-delivery/no-grant behavior and verifies the refreshed notify keeps
  `RequiresRoll` with `CanLoot=false`.
- Remaining blocker: F-014 still needs retail proof for all-pass roll feedback,
  exact `ServerLootCanLoot` producer timing, and bind-on-pickup confirmation
  timing.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~GlobalLootManagerTests" -v minimal --nologo`
  passed 9/9.
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~Loot" -v minimal --nologo`
  passed 62/62.

2026-06-03 - F-014 all-pass roll policy coverage (D-L9):

- Result: `Implemented` focused test coverage for the current all-pass roll
  policy. Runtime behavior was left unchanged.
- `LootInstanceResolutionTests.RollAllPasses_MarksDeliveredWithoutGrantingItem`
  verifies that all-pass rolls send a zero-value `ServerLootWinner`, clear the
  pending roll state through `MarkDeliveredWithoutWinner`, emit the final item
  update/remove sequence, and do not create inventory grants for any participant.
- Remaining blocker: this pins emulator policy, not retail parity. F-014 still
  needs live/client proof for whether all-pass outcomes should be recoverable,
  visible as a distinct feedback state, or destroyed exactly as the current
  winner packet path does.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~LootInstanceResolutionTests" -v minimal --nologo`
  passed 5/5.
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~Loot" -v minimal --nologo`
  passed 63/63.

2026-06-03 - WorldServer publish failure observation (R3-X1):

- Result: `Implemented` async publish hardening for the remaining WorldServer
  message handlers with raw `PublishAsync` calls from `void HandleMessage`.
- Added `FireAndForgetAsync` wrappers to `ClientChatJoinHandler`,
  `ClientChatModeratorHandler`, `ClientGroupInviteResponseHandler`, and both
  `ClientGroupLeaveHandler` publish branches. Adjacent chat/group/friendship
  handlers already used this pattern.
- A handler-surface scan over `Source/NexusForever.WorldServer/Network/Message/Handler`
  and `Source/NexusForever.WorldServer/Network/Internal/Handler` now finds no
  `PublishAsync` statements without `FireAndForgetAsync`.
- Remaining blocker: this observes and suppresses fire-and-forget exceptions
  through the existing shared task helper; it does not add per-handler logging
  callbacks or make the network handler pipeline await asynchronous internal
  publishes.
- Verification:
  raw-publish scan returned no unwrapped handler calls.
  `dotnet build Source\NexusForever.WorldServer\NexusForever.WorldServer.csproj --no-restore -v minimal --nologo`
  passed with 0 warnings and 0 errors.

2026-06-03 - F-022/F-025 Q3487 activate-cast objective ownership (D-Q1/D-Q2):

- Result: `Implemented` narrow quest/interaction boundary cleanup for the
  scripted Dominion cannon. No generic activate-cast quest semantics, spell
  effect routing, or retail type-14 producer behavior was widened in this pass.
- Local client SQL resolves D-Q1: `Quest2.tbl.sql:493` links quest `3487` to
  objectives `4489`, `4825`, and `4535`; `QuestObjective.tbl.sql:514` defines
  objective `4489` as type `10` (`ScriptedTargetGroupChecklist`) with target
  group data `1322`. The current `Q3487DominionCannonEntityScript` type matches
  the quest data, so the type-14 cannon hypothesis is rejected for this quest.
- `InteractionObjectiveUpdater` now treats creature `11251` as a script-handled
  activate-cast objective target, suppressing generic `SucceedCSI`, `TalkTo`,
  target-group, checklist, and gather update attempts after the cannon script
  handles accepted-quest checklist credit.
- `InteractionObjectiveUpdaterTests.UpdateActivateSuccessObjectives_Q3487CannonActivateCast_SuppressesGenericObjectiveUpdates`
  pins the no-generic-update boundary while existing generic activate-cast and
  direct-interaction tests continue to cover the normal fan-out paths.
- Remaining blocker: exact retail spell-effect producer timing for other
  activate-target-group checklist rows remains under F-022/F-025 evidence gates;
  this pass only closes the table-backed Q3487 cannon ownership conflict.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~InteractionObjectiveUpdaterTests|FullyQualifiedName~EarlyZoneEntityObjectiveCreditTests|FullyQualifiedName~ClientActivateUnitCastHandlerTests" -v minimal --nologo`
  passed 37/37.
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~Quests|FullyQualifiedName~InteractionObjectiveUpdaterTests|FullyQualifiedName~ClientActivateUnitCastHandlerTests" -v minimal --nologo`
  passed 162/162.

2026-06-03 - F-005 auction post tradeability and economy status (D-M4/D-M5):

- Result: `Implemented` marketplace posting guard and tracker cleanup. No
  unsupported property/rune/equippable search filter semantics, CREDD behavior,
  or DB save/transaction atomicity was widened in this pass.
- `GlobalMarketplaceManager.PostAuction` now validates that the listed item is
  owned by the seller, is in `InventoryLocation.Inventory`, is not soulbound,
  and is not an equippable bag before removing it from inventory or creating an
  auction row.
- Invalid auction posts return `GenericError.AuctionCannotFillOrder`, preserve
  inventory state, and leave owned auction lists empty.
- `MarketplaceAuctionHandlerTests` adds regressions for soulbound and
  equippable-bag auction posting, and older direct manager test helpers now set
  explicit owner/location/soulbound state for valid listing items.
- `GAMEPLAY_ECONOMY_SOCIAL_STATUS.md` now records the C1-C4 marketplace guard
  set, unsupported auction filter rejection, auction cancel rollback, delivery
  failure non-mutation guards, and this auction post tradeability boundary.
- Remaining blocker: F-005/F-013 still need final settlement DB
  transaction/save atomicity proof and exact retail property/rune/equippable
  filter implementation.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~MarketplaceAuctionHandlerTests|FullyQualifiedName~MarketplaceAccountLimitsTests|FullyQualifiedName~MarketplaceOwnedListHandlerTests" -v minimal --nologo`
  passed 33/33.
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~Marketplace|FullyQualifiedName~Mail" -v minimal --nologo`
  passed 57/57.

2026-06-03 - F-010/F-015 ready-check and PvP boundary cleanup (D-G3/D-G4):

- Result: `Implemented` tracker/status cleanup plus focused PvP boundary
  coverage. No group `0x0438` value semantics, replacement queue lifecycle,
  duel reward behavior, or broader open-world PvP rules were widened.
- D-G3 is resolved as stale: `Group.StartReadyCheckAsync` still clears
  `Ready`/`HasSetReady` in memory before `SetFlagAsync(Pending)` to avoid two
  member-flag messages, and the old stale-UI risk is already closed because
  `GroupMemberFlagsUpdatedHandler` always emits `ServerGroupReadyCheckStatusUpdate`
  including pending status `0`.
- D-G4 is now documented and test-pinned: `Player.CanAttack` rejects player
  targets unless `DuelManager.AreDueling` reports an active duel. Instanced PvP
  match combat remains a separate map/match path, and non-duel open-world PvP
  rules stay blocked pending retail evidence.
- `PvpCombatBoundaryTests.OpenWorldPlayerTargetWithoutActiveDuel_IsNotAttackable`
  covers the current no-duel player-target boundary.
- `CURRENT_STATUS.md` and `GAMEPLAY_ECONOMY_SOCIAL_STATUS.md` now record both
  boundaries for the high-level status snapshot.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~GroupFlagHandlerTests|FullyQualifiedName~PvpCombatBoundaryTests|FullyQualifiedName~PvpFlagCooldownTests" -v minimal --nologo`
  passed 11/11.
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~Group|FullyQualifiedName~Pvp" -v minimal --nologo`
  passed 190/190.

2026-06-03 - F-014 corpse loot recipient range guard (#4/#5):

- Result: `Implemented` narrow corpse-loot recipient correction. Group corpse
  loot now keeps group context when a valid same-map multi-member group is
  present, but only in-range players become tracked looters, eligible
  recipients, or master-loot candidates. Empty in-range group contexts are
  skipped before creating a corpse loot instance.
- This closes the stale backlog pair where single-eligible group corpses either
  fell back to solo or leaked loot UI to same-map out-of-range members who
  could not pass the later collect/range guard.
- `GlobalLootManagerTests.CreateLootRecipientContext_GroupCorpseLootKeepsOnlyInRangeMembersEligible`
  pins the one-in-range / one-out-of-range same-map group edge.
- Remaining blocker: F-014 still needs retail proof for `ServerLootCanLoot`
  emit timing and exact parent/source tracking.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~GlobalLootManagerTests|FullyQualifiedName~LootInstanceResolutionTests|FullyQualifiedName~LootRequestHandlerTests" -v minimal --nologo`
  passed 20/20.

2026-06-03 - F-015 duel cancel/disconnect regressions (#19/#20):

- Result: `Verified` runtime/test closure for stale PvP review rows. Runtime
  behavior was already conservative: cancelled/declined duel results do not
  claim winner/loser unit ids, and `WorldSession.OnDisconnect` calls
  `DuelManager.OnPlayerDisconnect`.
- `DuelManagerTests.PendingChallenge_DeclineEmitsResultWithoutWinnerOrLoser`
  pins declined-request result ids, and
  `DuelManagerTests.ActiveDisconnectCancelsDuelWithoutWinnerOrLoser` pins
  active disconnect cleanup.
- Remaining blocker: F-015 still needs observer/reward/stat parity and retail
  proof before widening non-duel open-world PvP rules.
- Verification:
 `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~DuelManagerTests|FullyQualifiedName~PvpCombatBoundaryTests|FullyQualifiedName~PvpPacketShapeTests|FullyQualifiedName~PvpFlagCooldownTests" -v minimal --nologo`
  passed 16/16.

2026-06-03 - F-014 atomic loot-unit id allocation (#40):

- Result: `Implemented` latent allocator hardening. `GlobalLootManager.NextLootId`
  now uses atomic increment so future off-tick loot producers cannot duplicate
  transient loot-unit ids.
- The allocator keeps the existing high-bit server-owned id range
  (`0x80000000` first allocation) and preserves the zero-skip behavior across
  wraparound.
- `GlobalLootManagerTests.NextLootId_ConcurrentAllocation_ReturnsUniqueNonZeroIds`
  exercises concurrent allocation and pins unique, non-zero ids plus the first
  high-bit value.
- Remaining blocker: this does not change the F-014 retail gaps for
  `ServerLootCanLoot` emit timing or parent source tracking.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~GlobalLootManagerTests|FullyQualifiedName~LootInstanceResolutionTests|FullyQualifiedName~LootRequestHandlerTests" -v minimal --nologo`
  passed 21/21.
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~Loot" -v minimal --nologo`
  passed 65/65.

2026-06-03 - F-014 generated loot partial-delivery return contract (#11):

- Result: `Implemented` mixed-delivery contract hardening.
  `LootInstance.DeliverAllLoot` now returns `false` when any pending loot row
  fails delivery, while still applying successful rows and leaving failed rows
  retryable for the same loot instance.
- Generated loot with granted-notify now suppresses the complete
  `ServerLootNotify` reward presentation on partial delivery, so generated
  reward UI no longer claims a complete grant when inventory failure kept one
  or more rows pending.
- `LootInstanceDeliveryTests.DeliverAllLoot_MixedSuccessAndFailure_ReturnsFalseAndKeepsFailedItemRetryable`
  pins the mixed cash/static-item edge, and
  `LootInstanceDeliveryTests.GiveGeneratedLoot_WithGrantedNotify_WhenPartiallyDelivered_DoesNotSendGrantedNotify`
  pins the generated-notify suppression boundary.
- Remaining blocker: F-014 still needs retail proof for `ServerLootCanLoot`
  emit timing, exact parent/source tracking, roll/master UI packet parity, and
  BoP timing. Loot indexing and active-instance scan cost (#8) remain a
  separate performance backlog row.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~LootInstanceDeliveryTests" -v minimal --nologo -p:UseSharedCompilation=false`
  passed 5/5.
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~Loot" -v minimal --nologo -p:UseSharedCompilation=false`
  passed 67/67.

2026-06-03 - F-014 delivered loot instance immediate cleanup (#9):

- Result: `Implemented` delivered-instance sweep reduction. Final collect and
  loot vacuum now remove a fully delivered `LootInstance` from
  `GlobalLootManager` immediately after the all-looter remove packet path, so
  completed corpses no longer remain in the hot active list until the next
  expiry sweep.
- Roll and master-loot paths now drop expired instances immediately after their
  internal finalisation/remove broadcasts, avoiding a duplicate remove packet
  while still shrinking the active list in the same player action.
- `GlobalLootManagerTests.GiveLoot_WhenLastItemDelivered_RemovesInstanceImmediately`
  pins the direct collect edge and verifies the instance count reaches zero
  immediately after delivery.
- Remaining blocker: the broader #8 indexing/profiling work remains open for
  active pending instances and owner/looter lookup scans.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~GlobalLootManagerTests.GiveLoot_WhenLastItemDelivered_RemovesInstanceImmediately" -v minimal --nologo -p:UseSharedCompilation=false`
  passed 1/1.
 `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~Loot" -v minimal --nologo -p:UseSharedCompilation=false`
  passed 68/68.

2026-06-03 - F-014 active loot owner/looter indexing (#8):

- Result: `Partially implemented` active loot lookup indexing.
  `GlobalLootManager` now maintains owner-unit and looter indexes alongside the
  active instance list, and the notify, runtime snapshot, collect/roll/master
  lookup, and loot-vacuum paths use those indexes instead of scanning the full
  active list.
- Instance creation and immediate/sweep removal keep both indexes in sync, and
  `LootInstance.LooterCharacterIds` exposes a read-only looter id view for the
  manager index without leaking the mutable dictionary.
- The existing immediate-cleanup regression now asserts the owner and looter
  indexes are empty after final delivery, and the test injection helper uses
  the same private add path as production.
- Remaining blocker: the active per-tick `Update` walk remains intentionally
  unchanged until profiling proves a timing-wheel or zone/shard split is worth
  the extra state.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~GlobalLootManagerTests" -v minimal --nologo -p:UseSharedCompilation=false`
  passed 12/12.
 `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~Loot" -v minimal --nologo -p:UseSharedCompilation=false`
  passed 68/68.

2026-06-03 - F-005 price-indexed commodity matching (#15):

- Result: `Implemented` commodity order-book indexing for match candidate
  selection. `GlobalMarketplaceManager` now maintains per-`Item2Id` buy/sell
  price indexes alongside the existing order list, preserving the list for
  owned-order and info views while avoiding full commodity-list scans during
  cross-match and immediate sell preflight.
- Incoming buy orders now enumerate sell price levels in ascending price order
  up to the buyer limit; incoming sell and immediate-sell preflight enumerate
  buy price levels in descending price order down to the seller limit. Same
  price-level lists preserve the existing posting/load order.
- Post, persisted load, cancel, expiry, fill removal, and insert rollback paths
  keep the order-book indexes in sync through shared add/remove helpers.
- Remaining blocker: exact retail multi-order partial-fill/partial-stack
  precision remains semantic evidence work; this pass only removes the stale
  O(n^2) candidate-scan shape.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~Marketplace" -v minimal --nologo -p:UseSharedCompilation=false`
  passed 46/46.
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~Marketplace|FullyQualifiedName~Mail" -v minimal --nologo -p:UseSharedCompilation=false`
  passed 63/63.
 `dotnet build Source\NexusForever.Game\NexusForever.Game.csproj --no-restore -v minimal --nologo -p:UseSharedCompilation=false`
  passed with 0 warnings.

2026-06-03 - F-005 online-inventory auction buyout delete-save guard:

- Result: `Implemented` partial final-settlement hardening for the online
  buyer inventory path. This does not widen mail/offline settlement semantics.
- `GlobalMarketplaceManager.BuyAuction` now detects buyouts that can deliver
  directly to the online buyer inventory and persists the auction delete plus
  moved item state before buyer debit, seller credit, winner notification,
  auction removal, or `Inventory.AddItem`.
- If that delete/save fails, the code restores the prior bid/top-bidder and
  item owner state, returns `GenericError.DbFailure`, leaves the auction active,
  and performs no buyer currency debit, seller credit, inventory delivery, or
  `ServerAuctionWon` notify.
- `MarketplaceAuctionHandlerTests.AuctionBuyout_WhenInventoryDeliveryDeletePersistFails_DoesNotChargeCreditDeliverOrRemoveAuction`
  pins the rollback/avoidance behavior.
- Remaining blocker: F-005/F-013 still need the larger final settlement
  transaction boundary for mail/offline auction or commodity settlement,
  cross-context item state, and online/offline currency persistence.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore --filter "FullyQualifiedName~MarketplaceAuctionHandlerTests.AuctionBuyout_WhenInventoryDeliveryDeletePersistFails" -v minimal --nologo`
  passed 1/1.
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore --filter "FullyQualifiedName~Marketplace" -v minimal --nologo`
  passed 47/47.
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore --filter "FullyQualifiedName~Marketplace|FullyQualifiedName~Mail" -v minimal --nologo`
  passed 64/64.

2026-06-03 - F-005 online-inventory auction cancel delete-save guard:

- Result: `Implemented` partial final-settlement hardening for the online owner
  inventory-return cancel path. This does not widen mail/offline settlement
  semantics.
- `GlobalMarketplaceManager.CancelAuction` now persists the auction delete plus
  returned item state before removing the in-memory auction, refunding the top
  bidder, or calling owner `Inventory.AddItem` when the cancelled auction can
  return directly to the online owner's inventory.
- If that delete/save fails, the code restores the item owner state, returns
  `GenericError.DbFailure`, leaves the auction active, and performs no bidder
  refund or owner inventory return.
- `MarketplaceAuctionHandlerTests.AuctionCancel_WhenInventoryReturnDeletePersistFails_DoesNotRefundReturnOrRemoveAuction`
  pins the rollback/avoidance behavior with an active bidder.
- Remaining blocker: F-005/F-013 still need the larger final settlement
  transaction boundary for mail/offline auction or commodity settlement,
  cross-context item state, and online/offline currency persistence.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore --filter "FullyQualifiedName~MarketplaceAuctionHandlerTests.AuctionCancel_WhenInventoryReturnDeletePersistFails" -v minimal --nologo`
  passed 1/1.
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore --filter "FullyQualifiedName~Marketplace" -v minimal --nologo`
  passed 48/48.
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore --filter "FullyQualifiedName~Marketplace|FullyQualifiedName~Mail" -v minimal --nologo`
  passed 65/65.

2026-06-03 - F-005 direct commodity cancel delete-save guards:

- Result: `Implemented` partial final-settlement hardening for direct commodity
  buy-order escrow refunds and sell-order inventory returns. This does not
  widen mail/offline settlement semantics.
- `GlobalMarketplaceManager.CancelCommodityOrder` now persists the commodity
  order delete before removing the in-memory order, refunding buy-order escrow,
  or recreating sell-order items in the owner's inventory when the cancel does
  not require mail return.
- If that delete/save fails, the code returns `GenericError.DbFailure`, leaves
  the commodity order active, and performs no escrow refund or item recreation.
- `MarketplaceAuctionHandlerTests.CommodityBuyOrderCancel_WhenDeletePersistFails_DoesNotRefundOrRemoveOrder`
  and
  `MarketplaceAuctionHandlerTests.CommoditySellOrderCancel_WhenInventoryReturnDeletePersistFails_DoesNotReturnItemsOrRemoveOrder`
  pin both direct side-effect branches.
- Remaining blocker: F-005/F-013 still need the larger final settlement
  transaction boundary for mail/offline auction or commodity settlement,
  cross-context item state, and online/offline currency persistence.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore --filter "FullyQualifiedName~MarketplaceAuctionHandlerTests.CommodityBuyOrderCancel_WhenDeletePersistFails|FullyQualifiedName~MarketplaceAuctionHandlerTests.CommoditySellOrderCancel_WhenInventoryReturnDeletePersistFails" -v minimal --nologo`
  passed 2/2.
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore --filter "FullyQualifiedName~Marketplace" -v minimal --nologo`
  passed 50/50.
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore --filter "FullyQualifiedName~Marketplace|FullyQualifiedName~Mail" -v minimal --nologo`
  passed 67/67.

2026-06-03 - F-005 direct commodity expiration delete-save guards:

- Result: `Implemented` partial final-settlement hardening for direct online
  commodity buy-order expiration refunds and sell-order inventory returns. This
  does not widen mail/offline settlement semantics.
- `GlobalMarketplaceManager.ExpireCommodityOrder` now persists the commodity
  order delete before removing the in-memory order, refunding online buy-order
  escrow, recreating sell-order items in the owner's inventory, or sending
  `ServerCommodityAuctionRemoved` when expiration can settle directly through
  online currency/inventory.
- If that delete/save fails, the expiration sweep returns with the order active
  and performs no refund, item recreation, removal notification, or order
  removal.
- `MarketplaceAuctionHandlerTests.CommodityBuyOrderExpire_WhenDeletePersistFails_DoesNotRefundNotifyOrRemoveOrder`
  and
  `MarketplaceAuctionHandlerTests.CommoditySellOrderExpire_WhenInventoryReturnDeletePersistFails_DoesNotReturnNotifyOrRemoveOrder`
  pin both direct expiration side-effect branches.
- Remaining blocker: F-005/F-013 still need the larger final settlement
  transaction boundary for mail/offline auction or commodity settlement,
  cross-context item state, and online/offline currency persistence.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore --filter "FullyQualifiedName~MarketplaceAuctionHandlerTests.CommodityBuyOrderExpire_WhenDeletePersistFails|FullyQualifiedName~MarketplaceAuctionHandlerTests.CommoditySellOrderExpire_WhenInventoryReturnDeletePersistFails" -v minimal --nologo`
  passed 2/2.
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore --filter "FullyQualifiedName~Marketplace" -v minimal --nologo`
  passed 52/52.
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore --filter "FullyQualifiedName~Marketplace|FullyQualifiedName~Mail" -v minimal --nologo`
  passed 69/69.

2026-06-03 - F-005 online-inventory auction expiration delete-save guard:

- Result: `Implemented` partial final-settlement hardening for the online
  winning-bid inventory-delivery expiration path. This does not widen
  mail/offline settlement semantics.
- `GlobalMarketplaceManager.ExpireAuction` now persists the auction delete plus
  moved item state before removing the in-memory auction, crediting the seller,
  calling winner `Inventory.AddItem`, or sending `ServerAuctionWon` when an
  expired auction can deliver directly to the online top bidder's inventory.
- If that delete/save fails, the expiration sweep restores the item owner state,
  leaves the auction active, and performs no seller credit, winner inventory
  delivery, winner notification, or auction removal.
- `MarketplaceAuctionHandlerTests.AuctionExpire_WithOnlineWinnerInventoryDeliveryDeletePersistFails_DoesNotCreditDeliverNotifyOrRemoveAuction`
  pins the rollback/avoidance behavior with an active winning bidder.
- Remaining blocker: F-005/F-013 still need the larger final settlement
  transaction boundary for mail/offline auction or commodity settlement,
  cross-context item state, and online/offline currency persistence.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore --filter "FullyQualifiedName~MarketplaceAuctionHandlerTests.AuctionExpire_WithOnlineWinnerInventoryDeliveryDeletePersistFails" -v minimal --nologo`
  passed 1/1.
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore --filter "FullyQualifiedName~Marketplace" -v minimal --nologo`
  passed 53/53.
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore --filter "FullyQualifiedName~Marketplace|FullyQualifiedName~Mail" -v minimal --nologo`
  passed 70/70.

2026-06-03 - F-005/F-013 item-auction won-mail same-save auction deletion:

- Result: `Implemented` partial final-settlement transaction hardening for
  item-auction won-mail delivery. This covers auction buyout/expiration paths
  that deliver the won item through mail, not auction return mail or commodity
  mail.
- `MarketplaceMailDelivery.TrySendItemAuctionWonMail` now has a same-context
  save overload so marketplace callers can compose mail creation, attached item
  save, and an additional marketplace row mutation in one character DB save.
- `GlobalMarketplaceManager.CompleteAuctionSale` uses that overload for
  non-inventory item-auction win delivery, deleting the auction row in the same
  save that creates the won-item mail and saves the attached item owner state.
  The caller skips the later standalone `PersistAuctionDelete` when this
  composed save succeeds.
- If the composed save action fails, attached item owner state is restored and
  the settlement remains uncommitted before buyer debit, seller credit, winner
  notification, or in-memory auction removal.
- `MarketplaceMailSettlementTests.MarketplaceMailDelivery_AdditionalSaveFailure_RestoresAttachedItemOwner`
  pins the composed save failure/restoration behavior, alongside the existing
  `MarketplaceAuctionHandlerTests.AuctionBuyout_WhenMailPersistFails_DoesNotChargeBuyerOrRemoveAuction`.
- Remaining blocker: F-005/F-013 still need final settlement transaction
  boundaries for auction return mail, commodity fill/return mail, offline
  credit/currency updates, and any cross-context online currency persistence.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore --filter "FullyQualifiedName~MarketplaceAuctionHandlerTests.AuctionBuyout_WhenMailPersistFails|FullyQualifiedName~MarketplaceMailSettlementTests.MarketplaceMailDelivery_AdditionalSaveFailure" -v minimal --nologo`
  passed 2/2.
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore --filter "FullyQualifiedName~Marketplace" -v minimal --nologo`
  passed 54/54.
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore --filter "FullyQualifiedName~Marketplace|FullyQualifiedName~Mail" -v minimal --nologo`
  passed 71/71.

2026-06-03 - F-005/F-013 item-auction return-mail same-save auction deletion:

- Result: `Implemented` partial final-settlement transaction hardening for
  item-auction return-mail delivery. This covers auction cancel and expiration
  paths that return the listed item through mail, not commodity mail or
  offline currency settlement.
- `MarketplaceMailDelivery.TrySendItemAuctionReturnMail` now has the same
  same-context save overload as won mail, allowing marketplace callers to
  compose mail creation, attached item save, and auction row deletion in one
  character DB save.
- `GlobalMarketplaceManager.CancelAuction` uses that overload when an auction
  cancellation must return the listed item by mail. On success it removes the
  in-memory auction and skips the later standalone `PersistAuctionDelete`.
- `GlobalMarketplaceManager.ReturnExpiredAuctionToOwner` uses the same overload
  for no-bid auction expiration when owner return is by mail, reporting to the
  expiration sweep that the auction delete is already persisted with mail.
- If the composed save action fails, attached item owner state is restored and
  the settlement remains uncommitted before bidder refund, owner notification,
  in-memory auction removal, or any later standalone delete.
- `MarketplaceMailSettlementTests.MarketplaceMailDelivery_AuctionReturnAdditionalSaveFailure_RestoresAttachedItemOwner`
  pins the composed return-mail save failure/restoration behavior.
- Remaining blocker: F-005/F-013 still need final settlement transaction
  boundaries for commodity fill/return mail, offline credit/currency updates,
  and any cross-context online currency persistence.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore --filter "FullyQualifiedName~MarketplaceMailSettlementTests.MarketplaceMailDelivery_AdditionalSaveFailure|FullyQualifiedName~MarketplaceMailSettlementTests.MarketplaceMailDelivery_AuctionReturnAdditionalSaveFailure" -v minimal --nologo`
  passed 2/2.
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore --filter "FullyQualifiedName~Marketplace" -v minimal --nologo`
  passed 55/55.
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore --filter "FullyQualifiedName~Marketplace|FullyQualifiedName~Mail" -v minimal --nologo`
  passed 72/72.

2026-06-03 - F-028 support aux field-level coverage (#32):

- Result: `Implemented` test-only closure for the stale support packet-shape
  row. Runtime packet models were already writing the mapped fields.
- `SupportPacketShapeTests.ServerSupportUInt5AndSixUInt32_WritesMappedFields`
  now explicitly consumes the 27-bit zero tail after the five-bit value and six
  `uint32` fields.
- Backlog #32 is now closed together with the existing
  `ServerSupportUInt32AndFlags_WritesValueFlagsAndPadding` value/2-bit
  flags/30-bit padding coverage.
- Remaining blocker: F-028 still needs support-case result/readback semantics
  and any DB-backed support-case lifecycle decision.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~Support" -v minimal --nologo`
  passed 121/121.

2026-06-03 - F-010 group online-member fan-out cleanup (#24/#25):

- Result: `Implemented` internal group handler refactor without changing mapped
  packet semantics.
- `GroupMemberFanoutHelper` now centralizes online group-member resolution for
  internal broadcast handlers, including group flags, loot rules, markers, max
  size, promotion, realm update, ready-check start, member add/remove, stats,
  position, and instance difficulty update flows.
- `GroupMemberFlagsUpdatedHandler` now builds one immutable role/flag packet
  and one ready-check packet per update, then enqueues both to each online
  member in the same per-player order as before.
- Remaining blocker: this does not resolve `0x0438` leading/parallel uint32
  semantics, non-zero raid queue semantics, replacement backfill/merge
  lifecycle, or durable raid locks.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~Group" -v minimal --nologo`
  passed 158/158.

2026-06-03 - F-003/F-030 realm aux packet test placement (#33):

- Result: `Implemented` test organization cleanup. Packet behavior is
  unchanged.
- `ServerRealmAuxUInt32TripletList_WritesCountedRows` moved out of
  `SupportPacketShapeTests` into dedicated `RealmAuxPacketShapeTests`, matching
  the pregame/realm owner for opcode `0x05A1`.
- Remaining blocker: `0x05A1` producer/consumer semantics remain mapped-only;
  this pass only fixes the stale test-placement row.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~RealmAuxPacketShapeTests|FullyQualifiedName~SupportPacketShapeTests" -v minimal --nologo`
  passed 17/17.

2026-06-03 - F-003 triplet row/list consolidation (#34/#35):

- Result: `Implemented` shared packet-model refactor. Packet wire output is
  unchanged.
- `ServerSpellUInt32TripletListRow` now inherits the shared
  `ServerUInt32Triplet` writer instead of duplicating the three-field payload.
- Spell triplet lists (`0x080F`/`0x0810`) and realm aux triplet list (`0x05A1`)
  now reuse `ServerUInt32TripletListPayload` for count + row serialization.
- Remaining blocker: this does not implement any producer path for `0x05A1` or
  blocked aux packets; producer/consumer semantics remain evidence-gated.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~RealmAuxPacketShapeTests|FullyQualifiedName~PacketPlaceholderNamingTests|FullyQualifiedName~SupportPacketShapeTests" -v minimal --nologo`
  passed 87/87.

2026-06-03 - F-005 marketplace P2 stale-row reconciliation (#12/#14/#16/#17):

- Result: `Verified` tracker reconciliation against current source. No runtime
  behavior changed in this pass.
- #12 is already fixed: `GlobalMarketplaceManager.Initialise` and `Persist`
  warn when `CharacterDatabase` is unavailable.
- #14 is fixed/mitigated: auction search pages before cloning result rows and
  returns an empty page for huge skips; exact sorting still materializes the
  ordered match list to preserve count/order correctness.
- #16 is already fixed through `.ConfigureAwait(false).GetAwaiter().GetResult()`;
  #17 is already fixed through debug logging for persisted auction rows whose
  `model.Item` is null.
- Remaining blocker: #15 remains deferred because replacing
  `TryMatchCommodityOrder` with a price-indexed order book is a deeper
  optimization, not a safe parity change for this pass. Broader marketplace DB
  transaction/save atomicity also remains open under F-005/F-013.
- Verification:
  source audit against `GlobalMarketplaceManager`; no new test run required for
  documentation-only reconciliation.

2026-06-03 - P2 stale tracker cleanup (#36/#37/#39/#42/#43):

- Result: `Verified` tracker reconciliation against current source. No runtime
  behavior changed in this pass.
- #36 is fixed by the shared `ServerUInt32WideStringPayload` used by the
  entity-stat wide-string packet. #37 is fixed by documenting
  `ServerSupportSurveyList.RowCount` as diagnostic/log-only while `Write`
  serializes `Rows.Count`.
- #39 is fixed/documented by the `GlobalLootManager.lootInstances`
  world-thread-only mutation contract. #42 is fixed by
  `QuestScriptConstructionTests`, and #43 is fixed by the
  `NorthernWildsMapInfo` / `NorthernWildsMapPosition` nested type names.
- Remaining blocker: #38 stays open as evidence work because crafting aux emit
  semantics remain unmapped after packet-shape correction.
- Verification:
 source audit against the named files; no new test run required for
 documentation-only reconciliation.

2026-06-03 - F-015 durable PvP toggle-off cooldown persistence:

- Result: `Implemented` server-side persistence for the pending PvP toggle-off
  cooldown. This does not change observer/reward/stat parity, forced-map PvP, or
  broader open-world PvP rules.
- `character.pvpFlagDisableUntilUtc` now stores an active pending-disable expiry
  through `CharacterModel`, `CharacterContext`, migration
  `20260603153000_CharacterPvpFlagDisableCooldown`, and the character model
  snapshot.
- `Player` reloads an unexpired pending-disable expiry as `PvPFlag.Enabled`,
  recreates the remaining timer, resends `ServerPvpCooldownUpdate` on login, and
  clears the stored expiry when the player cancels the pending disable or the
  cooldown expires.
- Remaining blocker: F-015 still needs observer/reward/stat parity, forced-map
  PvP semantics, arena/battleground score/reward parity, and retail proof before
  widening non-duel open-world PvP rules.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~PvpFlagCooldownTests|FullyQualifiedName~DuelManagerTests|FullyQualifiedName~PvpCombatBoundaryTests" -v minimal --nologo`
  passed 14/14.
  `dotnet build Source\NexusForever.Database.Character\NexusForever.Database.Character.csproj --no-restore -v minimal --nologo`
  passed with 0 warnings and 0 errors.

2026-06-03 - F-005/F-013 commodity return-mail same-save order deletion:

- Result: `Implemented` partial final-settlement hardening for commodity
  sell-order return mail. This covers cancel and expiration paths that return
  listed commodity items by mail, not commodity fill mail, offline credit
  settlement, CREDD, marketplace aux producer semantics, or exact multi-order
  partial-fill parity.
- `MarketplaceMailDelivery.TrySendCommodityAuctionReturnMail` now has a
  same-context save overload so marketplace callers can compose mail creation,
  returned-item persistence, and an additional marketplace row mutation in one
  character DB save.
- `GlobalMarketplaceManager.CancelCommodityOrder` now uses that overload when a
  sell-order cancel must return items by mail, composing the commodity order
  delete with the return mail save before removing the in-memory order.
- `GlobalMarketplaceManager.ExpireCommodityOrder` now uses commodity return mail
  for sell-order mail fallback instead of the fill-mail helper, and composes the
  commodity order delete with that mail save before sending the removal notice.
- `MarketplaceMailSettlementTests` and `MarketplaceAuctionHandlerTests` pin the
  commodity return-mail composed-save failure path: the helper returns false
  when the additional save fails, and cancel/expiration leave the commodity
  order active without inventory recreation or removal notification.
- Remaining blocker: F-005/F-013 still need final settlement transaction/save
  boundaries for commodity fill mail, offline auction/commodity rows, item
  state, and online/offline currency persistence.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore -v minimal --nologo --filter "FullyQualifiedName~MarketplaceMailSettlementTests.MarketplaceMailDelivery_CommodityReturnAdditionalSaveFailure|FullyQualifiedName~MarketplaceAuctionHandlerTests.CommoditySellOrderCancel_WhenMailReturnDeletePersistFails|FullyQualifiedName~MarketplaceAuctionHandlerTests.CommoditySellOrderExpire_WhenMailReturnDeletePersistFails"`
  passed 3/3.
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore -v minimal --nologo --filter "FullyQualifiedName~Marketplace|FullyQualifiedName~Mail"`
  passed 75/75.

2026-06-03 - F-005/F-013 commodity fill-mail same-save order mutation:

- Result: `Implemented` partial final-settlement hardening for commodity
  fill-mail delivery. This covers buyer item mail plus resting commodity
  buy/sell order row update/delete composition, not seller proceeds, buyer
  price-improvement refunds, offline credit settlement, CREDD, marketplace aux
  producer semantics, or exact multi-order partial-fill parity.
- `MarketplaceMailDelivery.TrySendCommodityAuctionFillMail` now has the same
  additional-save overload as item-auction and commodity-return mail.
- `GlobalMarketplaceManager.TryMatchCommodityOrder` now computes the fill
  remainder before delivery and, when buyer item delivery falls back to mail,
  composes the buyer mail, created purchased item, and resting commodity order
  update/delete mutations in one character DB save.
- In-memory order removal/update now skips duplicate DB writes when the fill-mail
  save already persisted the corresponding order mutation.
- `MarketplaceMailSettlementTests` and `MarketplaceAuctionHandlerTests` pin the
  commodity fill-mail composed-save failure path: the helper returns false when
  the additional save fails, and a force-immediate buy that would fill through
  mail leaves the resting sell order unfilled when the composed save fails.
- Remaining blocker: F-005/F-013 still need final settlement transaction/save
  boundaries for seller proceeds, buyer price-improvement refunds, offline
  auction/commodity rows, item state, and online/offline currency persistence.
- Verification:
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore -v minimal --nologo --filter "FullyQualifiedName~MarketplaceMailSettlementTests.MarketplaceMailDelivery_CommodityReturnAdditionalSaveFailure|FullyQualifiedName~MarketplaceMailSettlementTests.MarketplaceMailDelivery_CommodityFillAdditionalSaveFailure|FullyQualifiedName~MarketplaceAuctionHandlerTests.CommoditySellOrderCancel_WhenMailReturnDeletePersistFails|FullyQualifiedName~MarketplaceAuctionHandlerTests.CommoditySellOrderExpire_WhenMailReturnDeletePersistFails|FullyQualifiedName~MarketplaceAuctionHandlerTests.CommodityForceImmediateBuy_WhenFillMailOrderPersistFails"`
  passed 5/5.
  `dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --no-restore -v minimal --nologo --filter "FullyQualifiedName~Marketplace|FullyQualifiedName~Mail"`
  passed 77/77.
