# Spell Broadcast Roadmap

This document tracks the still-opaque spell broadcast family around opcodes `0x07F5` through `0x0819`.
The goal is not to guess packet meaning from field names. The goal is to preserve the current structural models,
record where each message fits in the spell lifecycle, and define the evidence required before any runtime send path is enabled.

Status update (2026-05-19): this family is now the active next decomp slice.
The target-helper chain is no longer the lead blocker, so focused WildStar64
passes should prefer this roadmap until the follow-up lifecycle packets are
either mapped or explicitly blocked.

Status update (2026-05-20): the current-client dispatcher, wrapper-node, and
runtime-listener paths are now mapped far enough that the older spellbook-first
`0x0818` interpretation below should be treated as historical context unless it
matches the evidence in the next section.

Status update (2026-06-07): `0x0816`/`0x0817`/`0x0814` now have a narrow
production send path for `SpellCastMethod.ChargeRelease` spells with
`Spell4Thresholds`. Local Charged Shot logs showed `34718` was executing as a
Fluff-only charge shell and finishing immediately; the runtime now keeps the
shell waiting, emits threshold start/update/clear, and casts the table-selected
threshold child damage spell on release. Other spell follow-up packets in this
roadmap remain blocked until they reach the same evidence bar.

Status update (2026-06-09): a cached-export/source recheck reconfirmed the
`0x0818`/`0x0819` wrapper follow-up boundary without widening runtime behavior.
`Network_RegisterServerOpcode_0351` (`14006c290`) registers `0x0819` size `8`
at `selected_decompiled.c:13429` to `ServerTwoUInt32_ReadPayload` (`14007a040`)
and `0x0818` size `0x20` at `selected_decompiled.c:13430` to
`ServerOpcode0818_ReadSpellWrapperIdAndTierEntry` (`140095e60`). Durable
dispatcher labels still map `0x0818` case `1403ee403` to wrapper-id lookup
(`140561c30`) and `SpellWrapper_ApplyEntityVariantTierEntryAndBroadcast`
(`14053f710`), and map `0x0819` case `1403ee3af` to
`SpellWrapperNode_PruneChildrenAndRefresh` (`140718af0`). Source still has no
runtime producer for either packet, so keep them non-emitted until a wrapper
lifecycle capture, producer timing proof, or selector-tail semantic correlation
appears.

## Latest Hard Evidence

- `Entity_DispatchHighRangeMessage` gives the direct post-decode edges for this follow-up cluster: `0x0814 -> 1403ef384 -> 1403bee40`, `0x0815 -> 1403ef372 -> SpellThreshold_HandleClear`, `0x0816 -> 1403ef34e -> SpellThreshold_HandleStartWrapper`, `0x0817 -> 1403ef360 -> SpellThreshold_HandleUpdate`, `0x0818 -> 1403ee403`, and `0x0819 -> 1403ee3af`.
- `0x07F6` post-parse path is mapped through the generic world-socket apply chain, not a registration apply slot: `WorldSocket_ProcessServerMessage` @ `140014f10` -> handler list `socket+0x14b0` `vtable+0x58` -> `Entity_DispatchHighRangeMessage` @ `1403ec6a0` (vtable `PTR_FUN_140b66880` `+0x58`) -> jump case `1403ee268` -> `SpellWrapper_ApplyServerEffectDamage` @ `1405a5800` -> `SpellWrapper_DispatchEffectDamageCombatLog` @ `14053f3f0` -> `CombatLog_MaybeDispatchFloatersForSourceAndTarget` @ `14060b2b0`. Primary-row damage fields only; trailing rows remain blocked at `14053f3f0`.
- `0x0818` now resolves through the wrapper runtime, not through a spellbook-only consumer. Case `1403ee403` resolves payload `[0]` as a spell-wrapper id through `SpellService_LookupSpellWrapperByWrapperId`, then forwards the nested tier-entry payload into `SpellWrapper_ApplyEntityVariantTierEntryAndBroadcast`. The nested tier-entry `dword @ +0x8` is the wrapper-local entity id used to create or update nodes under `spellWrapper + 0x48`, and the selector-specific variant tails feed `SpellWrapperNode_ApplyVariantEntry`.
- `0x0819` is the paired wrapper-node cleanup path. Case `1403ee3af` resolves payload `[1]` as a spell-wrapper id, matches payload `[0]` against the node entity id under `spellWrapper + 0x48`, and then calls `SpellWrapperNode_PruneChildrenAndRefresh`.
- Runtime listener materialization now maps to `DAT_140c65b70 + 0x650` with key `[type8Flag, routeKey, listenerCategory, listenerSubtype, slotOrBitIndex]` and route-descriptor payload at node `+0x38`. `DAT_140c65b70 + 0x658` is the auxiliary queued or deferred tree for the subset of listeners whose descriptor carries a non-zero deferred-queue value at `+0x84`. `SpellWrapper_BroadcastRouteListeners` (`140540430`) now shows that this queued tree is walked across listener categories `0..7` except `2` and slot or bit indices `0..31`; for matching descriptors it computes a wrapper progress scalar through `SpellWrapper_ComputeRouteProgressScalarByBitIndex` (`140540b30`) and stores the non-negative delta against descriptor `+0x84` into wrapper-local queued work records at `spellWrapper + 0x1b8/+0x1c0`. `spellWrapper + 0x358` is not the global listener registry; it is a wrapper-local expansion tree used only when descriptor flag `0x200` fans one listener out across multiple local expansion nodes.
- The wrapper route key at `spellWrapper + 0x348` is now sourced from the spell-service tuning tree at `DAT_140c65b70 + 0x598`, not from the listener registry. The safest current boundary for that field is a per-spell route channel rather than a raw spell id. `SpellService_LookupSpellInnerTuningRecord` (`1407a1680`) matches a primary spell id plus one of four inner ids, and the same returned record also supplies minimum range, maximum range, and cast-time overrides. Wrapper setup `14053d1f0` copies record field `+0x28` into `spellWrapper + 0x348`, falling back to `innerData + 0x1c` when no override route key is present.
- Listener payloads at node `+0x38` are now better modeled as route-event descriptors rather than final packet objects. `SpellRouteEvent_EmitOrQueue` (`140578460`) either serializes a descriptor-kind-specific route event immediately or queues it on the entity when the entity cannot accept it yet, `SpellRouteEvent_DispatchByKind` (`140543630`) selects the per-kind materializer path, and `SpellRouteEvent_InitEntityPayload` (`14054eb60`), `SpellRouteEvent_InitExtendedVectorPayload` (`14054ee30`), and `SpellRouteEvent_InitCommonPayload` (`14054e9f0`) materialize the concrete outbound event payload shapes from descriptor fields. Full switch coverage is now recovered for kinds `0..0xc`: kinds `7`, `8`, `9`, and `0xc` all build payloads and then either attach `0x40`-byte deferred nodes to the target entity or hand them to dedicated sink helpers (`140576f90`, `1405770f0`, `140577250`, `140577510`), kind `0xa` is the direct `Spell4ClientMissile` path through `SpellService_LookupSpell4ClientMissileRecord` (`140237680`) and `SpellRouteEvent_HandleClientMissileRecord` (`1405458e0`), and case `0xb` is an explicit no-op in the current body.

## Current Export Status

- `Decomp/Analysis/function_labels.csv` now contains durable labels for the direct client readers currently wired to `0x0814`, `0x0816`, `0x0817`, and `0x0818`, plus the shared `0x0815` Spell4-id stub and the spell-list helper family.
- The full WildStar64 registration-block decompile at `FUN_14006c290` now gives direct opcode wiring for this cluster: `0x0814 -> 140096000`, `0x0815 -> 140080d30`, `0x0816 -> 140095f30`, `0x0817 -> 140095fb0`, `0x0818 -> 140095e60`, and the previously conflicted `ServerSpellList_ReadPayload` actually sits at `0x0551 -> 140096060`.
- The 2026-06-09 selected-export recheck pins the wrapper pair at
  `selected_decompiled.c:13429` (`0x0819`, size `8`, shared two-uint reader
  `14007a040`) and `selected_decompiled.c:13430` (`0x0818`, size `0x20`,
  reader `140095e60`). Selected xrefs for `14007a040`, `140095e60`,
  `14053f710`, and `140718af0` remain label-only or reader/apply-local, and no
  current NexusForever emitter exists outside packet-shape tests and negative
  tutorial guards.
- `0x0815` now has stronger hard evidence than a single registration row: the same shared `ServerUInt18_ReadPayload` stub is also registered for `0x00B0`, `0x0129` `ServerUnlockMount`, and `0x01AE` `ServerUnlockVanityPet`, which confirms that this reader family is a generic one-field `18-bit` payload shape rather than a spell-specific trigger packet.
- `TraceFunctionCallers` on `ServerSpellList_ReadTierEntry` (`140094aa0`) shows `ServerOpcode0818_ReadSpellWrapperIdAndTierEntry` as a direct caller alongside spell-list readers, which ties `0x0818` into the spell-list tier-entry helper family instead of the current `TargetInfo` helper assumption.
- `ServerSpellList_ReadTierEntry` now has a field-level consumer overlap as well: its first 12 bytes are `Spell4Id @ +0`, a tier byte `@ +4`, a spec or action-set byte `@ +5`, and a 4-bit field stored at `@ +8`, which exactly matches the compact record consumed by `AbilityBook_ApplySingleTierEntryDelta` (`1403b9410`). That handler drives `SpellBook_MarkSpellLearned`, `SpellBook_MarkSpellUnlearned`, and `SpellBook_SetCurrentTierRank`, refreshes cached wrapper ids, and dispatches `AbilityBookChange`. `ServerSpellList_ReadVariantEntry` also now has two selector-specific tails behind its shared prefix, so the source-side spell-list placeholder should no longer stop at the selector. No direct `0x0818 -> 1403b9410` edge has been recovered yet, so treat this as the strongest current-client overlap rather than final proof.
- The nearby `0x017B` spellbook surface is now separately accounted for. Current-client reader `14008ee90` matches the tracked `ServerSpellUpdate` wire shape exactly: `18-bit Spell4Id`, `4-bit tier`, `3-bit spec`, and one trailing activation bit. That explains spell activation or LAS toggle updates and removes `0x017B` as an alternative explanation for `0x0818`.
- The same ability-book family now reaches visible Lua and request surfaces. `Lua_AbilityBook_UpdateSpellTier` reads the cached pending tier-budget field at `+0x6ddc`, falls back to committed budget `+0x6dd8`, walks tier costs, and calls `SpellBook_SendTierUpdateRequest`; `Lua_AbilityBook_ClearCachedLASUpdates` clears the pending spell-tier queue at `+0x1458` and resets `+0x6ddc`. `SpellBook_UpdateTierPointBudget` (`1403b95c0`) updates the same budget block and dispatches `AbilityBookChange`, which tightens the `0x0818` alignment to visible spellbook or ability-book state rather than a generic `TargetInfo` payload.
- `SpellBook_SendTierUpdateRequest` and `ActionSet_SendPendingActionSetChanges` show that the shared `+0x1458/+0x1460` cache is client-local pending spell update state keyed by `Spell4Id`, not a server-issued token table. This rules out a client-token echo hypothesis for the old ability-book-only investigation, but direct dispatcher evidence now supersedes that path for the leading `0x0818` field.
- The adjacent bulk row path `FUN_1403b77d0` still has a spellbook-facing subtype-`4` branch: it refreshes spell-tier state through `1403ba550` using the row `Spell4Id @ +0x10`, dispatches `AbilityBookChange` unless the trailing state at `+0xa8` is `0x31`, and forwards the row `+0x18` index plus an optional resolved spell object through `140608c60`.
- `1406089a0`, the sink underneath that subtype-`4` follow-up, stores or clears raw object pointers by the supplied `uint32` index inside an indexed pointer array and maintains sorted side lists from the same key. This remains useful context for the adjacent bulk-row path, but it is now a historical false lead for the leading `0x0818` packet field.
- The richer `140569c90 -> 140569d30` constructor chain is now ruled out as a direct `0x0818` bridge. The same helpers are also called by guild-bank tab loaders `14057cdc0` and `14057d190`, so that materializer is reusable client infrastructure rather than packet-specific spell-tier evidence.
- A second independent evidence source now exists for `0x0814`, `0x0816`, and `0x0817`: the local historical compare snapshot under `.nexusforever-runtime/compare-kirmmin-latest` defines `ServerSpellThresholdClear`, `ServerSpellThresholdStart`, and `ServerSpellThresholdUpdate` with payload shapes that exactly match the current client-proven readers, and sends them from the old `SpellThreshold.cs` runtime. Treat the snapshot as correlated legacy evidence; the current production runtime now re-enables only a narrower `ChargeRelease` threshold subset.
- Hard client-consumer evidence now backs the threshold interpretation as well: `FUN_1403be940` forwards the parsed `0x0816` payload into `SpellThreshold_HandleStart`, which allocates active threshold state, resolves stage metadata through the spell-service threshold cache, and dispatches the named client event `StartSpellThreshold`.
- `SpellThreshold_HandleUpdate` (`1403bea90`) consumes the parsed `0x0817` payload, stores the trailing byte into the active threshold entry, dispatches `UpdateSpellThreshold`, and can emit `StartSpellThreshold` again when threshold presentation begins.
- The sibling `SpellThreshold_HandleClear` / `SpellThreshold_ClearActiveState` path (`1403bed60 -> 1403bef30`) removes active threshold state for the same `Spell4Id`, which is the first direct client-consumer evidence that `0x0814` belongs to the same threshold lifecycle family.
- Threshold state is confirmed to surface into UI and Lua read paths: `SpellWrapper_GetIconPathString` consults the active threshold cache to pick alternate icon assets, and `Lua_GameSpell_GetThresholdTime` sums per-stage threshold durations from the same spell-service threshold map.
- This validates the current structural models for `0x0816` and `0x0817`, keeps the trigger-flag-like `18-bit Spell4 id + 1-bit flag` shape on `0x0814`, confirms `0x0815` as a plain `18-bit Spell4 id` follow-up, and leaves `0x0818` mapped as a wrapper-runtime follow-up that still lacks safe production timing evidence.
- The only live production send path in this follow-up family is the narrow `ChargeRelease` threshold lifecycle (`ServerSpellThresholdStart`, `ServerSpellThresholdUpdate`, `ServerSpellThresholdClear`) in `Source/NexusForever.Game/Spell/Spell.cs`; outside that, the diagnostic-only `ServerSpellCastTargetReport` emission in `Source/NexusForever.Game/Spell/Spell.RuntimeEvidence.cs` remains the only runtime witness.
- `TutorialCombatMineEntityScriptTests.Update_AfterDangerZoneCastTimeElapsed_DetonatesTelegraph`
  now guards a normal `ServerSpellStart` -> `ServerSpellGo` -> `ServerSpellFinish`
  runtime path so blocked follow-ups in `0x07F5..0x0819` are not silently
  promoted without fixture/sniff evidence. The guard includes standalone damage,
  nested damage, target-report/sync/list, threshold, and wrapper-node follow-up
  packets.

## Current Lifecycle Surface

The server already uses the proven spell lifecycle packets plus one narrow
threshold extension:

| Opcode | File | Current role |
| --- | --- | --- |
| `0x07F4` | `ServerSpellGo` | Main spell-go broadcast with target and effect results. |
| `0x07F9` | `Server07F9` | Live cancel-cast follow-up sent from `Spell.CancelCast()`. |
| `0x07FC` | `ServerSpellCastResult` | Validation failure result to the caster. |
| `0x07FE` | `ServerSpellFinish` | Spell completion signal. |
| `0x07FF` | `ServerSpellStart` | Spell-start broadcast before delayed execution. |
| `0x0814` | `ServerSpellThresholdClear` | Narrow `ChargeRelease` threshold cleanup for table-backed charge spells. |
| `0x0816` | `ServerSpellThresholdStart` | Narrow `ChargeRelease` threshold start for table-backed charge spells. |
| `0x0817` | `ServerSpellThresholdUpdate` | Narrow `ChargeRelease` threshold stage update for table-backed charge spells. |

Everything else in the `0x07F5..0x0819` range should currently be treated as a tracked but unimplemented follow-up surface.

## Opaque Follow-Up Cluster

| Opcode | File | Current understanding | Evidence still needed before implementation |
| --- | --- | --- | --- |
| `0x07F5` | `Source/NexusForever.Network.World/Message/Model/ServerSpellCastTargetUnit.cs` | Target-unit triplet follow-up after `ServerSpellGo` or `ServerSpellStart`. | Client parse path plus one sniffed example. |
| `0x07F6` | `Source/NexusForever.Network.World/Message/Model/ServerSpellEffectDamage.cs` | Mapped reader (`ServerSpellEffectDamage_ReadPayload` @ `140095910`, `0x48` bytes). Post-parse consumer: `WorldSocket_ProcessServerMessage` handler `vtable+0x58` -> `Entity_DispatchHighRangeMessage` case `1403ee268` -> `SpellWrapper_ApplyServerEffectDamage` @ `1405a5800` -> `SpellWrapper_DispatchEffectDamageCombatLog` @ `14053f3f0` (combat-log floaters; primary damage row only). Trailing rows read by `SpellDamageTrailingRow_ReadPayload` @ `1400945e0` but not consumed past `14053f3f0`. | Retail trailing-row witness; confirm which `+0x14b0` handler nodes return handled for this opcode; NF emit only if standalone `0x07F6` is required beyond `0x07F4` embedded damage. |
| `0x07F7` | `Source/NexusForever.Network.World/Message/Model/ServerSpellCastTargetUnit2.cs` | Second target-unit triplet follow-up after `ServerSpellGo` or `ServerSpellStart`. | Client parse path plus one sniffed example. |
| `0x07F8` | `Source/NexusForever.Network.World/Message/Model/ServerSpellEffectNestedTargets.cs` | Nested per-target follow-up: `ServerUniqueId`, 19-bit `Spell4EffectId`, `TargetId`, then a counted list of `SpellDamageDescription` rows (`ServerSpellEffectNestedTargets_ReadPayload` @ `140095810`, `0x38` bytes per row). Same damage/trailing wire as `0x07F6` but no `UnitId` and multiple rows per target. No dedicated client apply handler; NF does not emit yet. | Sniffed lifecycle witness (when the packet fires vs `0x07F4`/`0x07F6`) and any retail row with non-zero trailing count. |
| `0x07FA` | `Source/NexusForever.Network.World/Message/Model/ServerSpellCastTargetStatus.cs` | Target-status triplet in the same follow-up family. | Client parse path plus one sniffed example. |
| `0x07FB` | `Source/NexusForever.Network.World/Message/Model/ServerSpellCastTargetReport.cs` | Likely miss, immunity, or invalid-target report; currently emitted only through the diagnostic spell-broadcast path. | Sniffed miss or immune cast plus target-result correlation. |
| `0x07FD` | `Source/NexusForever.Network.World/Message/Model/ServerSpellCastPositionSync.cs` | Structurally mapped position and telegraph sync follow-up with counted initial-position and telegraph-position lists. | Client parse trigger plus one telegraph or movement-sensitive sniff. |
| `0x0811` | `Source/NexusForever.Network.World/Message/Model/ServerSpellCastTargetList.cs` | Casting-id target list follow-up that likely replays a subset of start or go target data. | Client parse path plus target-count correlation. |
| `0x0814` | `Source/NexusForever.Network.World/Message/Model/ServerSpellThresholdClear.cs` | Direct client registration maps this opcode to `140096000`, whose body reads one 18-bit `Spell4Id` plus one trailing 1-bit flag. The current source `ServerSpellTriggerFlag` model matches the client field shape, the historical compare snapshot used the same shape as `ServerSpellThresholdClear`, and the client-side `SpellThreshold_HandleClear -> SpellThreshold_ClearActiveState` path now shows the same Spell4-id family clearing active threshold state. Narrow runtime emission is enabled for `ChargeRelease` threshold shells. The exact meaning of the trailing flag is still not fully verified. | Sniff or client-side visual witness to validate the trailing flag semantics and any non-charge threshold cleanup cases. |
| `0x0815` | `Source/NexusForever.Network.World/Message/Model/ServerSpellThresholdSpell4.cs` | Direct client registration maps this opcode to `140080d30`, the shared `ServerUInt18_ReadPayload` stub reused by `ServerUnlockMount` and `ServerUnlockVanityPet`. This is strong evidence that the packet is only one `18-bit Spell4Id`, matching the current `ServerSpell0815` source model. | Sniffed witness plus lifecycle correlation. |
| `0x0816` | `Source/NexusForever.Network.World/Message/Model/ServerSpellThresholdStart.cs` | Direct client reader `140095f30` validates the current structure: three 18-bit `Spell4Id` values followed by one 32-bit casting id. The historical compare snapshot used the identical shape as `ServerSpellThresholdStart`, and the current client now has a direct consumer bridge: `FUN_1403be940` forwards the parsed payload into `SpellThreshold_HandleStart`, which creates threshold state and dispatches `StartSpellThreshold`. Narrow runtime emission is enabled for `ChargeRelease` threshold shells. | Client visual witness or sniff to validate exact transition timing for broader threshold spell families before widening server behavior. |
| `0x0817` | `Source/NexusForever.Network.World/Message/Model/ServerSpellThresholdUpdate.cs` | Direct client reader `140095fb0` validates the current structure: one 18-bit `Spell4Id` plus one trailing byte. The historical compare snapshot used the identical shape as `ServerSpellThresholdUpdate`, and the current client `SpellThreshold_HandleUpdate` path stores that byte into active threshold state, dispatches `UpdateSpellThreshold`, and can re-emit `StartSpellThreshold` when a threshold stage becomes active. Narrow runtime emission is enabled for `ChargeRelease` threshold shells. | Client visual witness or sniff to validate exact threshold-stage transitions for broader threshold spell families before widening server behavior. |
| `0x0819` | `Source/NexusForever.Network.World/Message/Model/ServerSpellWrapperNodeRemove.cs` | Direct client reader `ServerTwoUInt32_ReadPayload` @ `14007a040`; consumer path `1403ee3af` matches field zero against a wrapper node entity id and field one as the spell-wrapper id before `SpellWrapperNode_PruneChildrenAndRefresh`. | Sniffed witness plus wrapper lifecycle correlation before enabling server behavior. |
| `0x0818` | `Source/NexusForever.Network.World/Message/Model/ServerSpellWrapperTierEntry.cs` | Mapped wrapper-runtime follow-up. Direct client reader `140095e60` reads `SpellWrapperId` as a leading `uint32` and then `ServerSpellList_ReadTierEntry` at `+0x8`. Dispatcher case `1403ee403` resolves that id through `SpellService_LookupSpellWrapperByWrapperId` and forwards the nested tier-entry payload to `SpellWrapper_ApplyEntityVariantTierEntryAndBroadcast` (`14053f710`). The nested tier-entry `dword @ +0x8` is the wrapper-local node entity id; selector-specific variant tails feed `SpellWrapperNode_ApplyVariantEntry`. Earlier ability-book and subtype-`4` slot/index probes are now historical false leads for the leading field, though they still help explain the shared `ServerSpellList` helper layout. | Sniffed wrapper lifecycle witness and producer timing before enabling production server emission; selector-tail business semantics still need retail correlation. |

## Safe Near-Term Work

- Keep field widths and list counts accurate in the placeholder models.
- Improve comments and XML documentation when new structural evidence lands.
- Record every new hypothesis in terms of lifecycle position, not guessed retail names.
- Target the client message registration and parse-dispatch path in the next WildStar64 export before renaming or enabling any new runtime send sites.
- Prefer test-only or diagnostic-only send experiments over production runtime behavior.
- Update this document when one opcode advances from Observed to Correlated, Mapped, or Verified.

## Verification Loop

1. Start from one narrow spell fixture and record the expected lifecycle using `ServerSpellStart`, `ServerSpellGo`, `ServerSpellCastResult`, and `ServerSpellFinish`.
2. Compare the server placeholder model with `selected_decompiled.c`, `selected_xrefs.csv`, and `selected_reasons_summary.csv` for the matching client binary export.
3. Add sniff evidence only when the fixture clearly shows one extra packet in the `0x07F5..0x0818` range.
4. Do not enable a runtime send path until at least two evidence sources agree on when the packet fires and what its list counts or flag widths mean.
5. When a packet reaches Verified, document the exact enabling condition here before changing runtime code.
