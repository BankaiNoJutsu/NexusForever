# Remaining blocker inventory - 5 November 2026

This is a family-level inventory of the current roadmap, not a new completion
claim or a replay of every generated content row. The canonical roadmap's
latest linked milestones override older June snapshots. No Source, configuration,
database, analysis session or runtime was changed for this inventory.

Evidence labels below identify raw line numbers at inspection: **C** is
[CURRENT_STATUS.md](../../CURRENT_STATUS.md), **M** is
[MISSING_FEATURE_MATRIX.md](MISSING_FEATURE_MATRIX.md), and **R** is
[ASSESSMENT_REMAINING_WORK.md](ASSESSMENT_REMAINING_WORK.md). These files are
actively maintained; their line positions may move. Section names and linked
focused records remain the durable anchors.

## Scope and snapshot consistency

The overview covers all 36 areas, F-001 through F-036. Its actual section headings
expand F-016..F-020 into five areas and currently yield **11 implementation-COMPLETE,
23 partial/emulator-only, one diagnostic and one structurally closed**. The
Quick Stats table at C:20334-20338 originally said 13 complete/21 partial/two
other at the initial inspection. The canonical table has now been corrected to
11/23/two, matching those headings. Individual feature labels are unchanged;
this corrects an aggregate inconsistency rather than downgrading features.
`COMPLETE` explicitly means a working implementation boundary, not retail
parity (C:20363-20368 at the initial inspection).

The five-row list at C:24764-24803 is a **consolidated producer/emit backlog**,
not the total remaining scope. Implementation-COMPLETE families still carry
retail, data, runtime or deployment gates. The content tracker also states that
no generated inventory row is `retail_complete`; its generated status is dated
29 October and is not a count of independently blocking engineering tasks.
[Content milestone state](CONTENT_RETAIL_COMPLETENESS_TRACKER.md#current-milestone-state).

Gate types: **S** = Source behavior/integration still needed; **D** = unknown
semantics or authoritative data; **P** = provider, restart, broker, socket,
deployment or load qualification; **N** = native UI/retail capture or manual
content proof. A mapped consumer or passing model test does not close D/N.

## Five consolidated producer gates, reconciled

| Family | Current boundary | Still needed | Exact starting evidence |
| --- | --- | --- | --- |
| F-025 entity-stat aux | All six apply cases are mapped;08CC scanner-name publication is implemented.0939 name/title and093E tag-owner models are named. | D/N: eligibility, recipients and emission order for the other five producers0889/08F4/0939/093D/093E. | C:24766-24800; [current handler/proxy mapping](F025_STATIC_HANDLER_PROXY_MAPPING.md). |
| F-025 tracked markers0849/0848 | First 32-bit word is TrackingSlot row/UI key; final 15-bit value is neutral. The packet models and one-way helper now preserve the full-width ID. The separate Source milestone fixed four reproduced baseline failures and passed 118 affected cases/build/reviews; those are not audit tests. | D/N: trailing value, authoritative row selection, recipients, initial/update/disable/replay timing; provider/socket/native/retail qualification remains separate. | [Implemented identity correction](TRACKED_MARKER_IDENTITY.md), [native identity/lifecycle record](TRACKED_UNIT_AND_STATION_LIFECYCLE_2026_11_05.md); old C:24800 and M:18343 are superseded where they assert the old chain. |
| F-004 neighborhoods0501/0506 |0501 observed body replaces name text;0506 replaces cached rows and emits its legacy event. Modern Community browser is a distinct path. | D/N: actual legacy trigger, row backing/other field names and publication order; do not synthesize a trigger from Communities or stale Lua subscriptions. | [Neighborhood source/version comparison](HOUSING_NEIGHBORHOOD_WEB_CORROBORATION_2026_11_05.md), [native lifecycle follow-through](TRACKED_UNIT_AND_STATION_LIFECYCLE_2026_11_05.md). |
| F-008 crafting | Service names2C crafting/4F engraving/57 master craftsman are mapped; scene-path candidate reset/replacement is mapped.084B/0855 default on the inspected dispatcher. | D/N: discovery/unlock/complex-craft policy, other aux owner, current-craft and0860 order, non-success sigil rules and056C patch cadence. | [Station contracts](DEEPER_ROADMAP_CONTRACTS_2026_11_05.md), [lifecycle record](TRACKED_UNIT_AND_STATION_LIFECYCLE_2026_11_05.md), M:18307. |
| F-031 Fortune | Emulator rarity-tier payout and catalog transport exist; dated publisher rotation policy is bounded. | D/N: exact per-item weights and final-build active catalog from accepted catalog/packet/server evidence. | [Fortune audit](FORTUNE_WEIGHT_AUDIT.md); C:24803; M:18358. |

## All 36 feature-area families

The status column is the overview heading's implementation label. The remaining
column includes the narrower retail gaps explicitly retained by the matrix;
it is not an assertion that all older Source work is still unimplemented.

| Area / snapshot status | Remaining family and gate type | Concrete next artifact | Evidence at inspection |
| --- | --- | --- | --- |
| F-001 STS — Partial | D/N: optional RSA/token trust/private-key/padding, generic reply grammar, startup order and accepted crypto/session transition. Existing component envelope, validation and selected mode4 expansion are already mapped. | Matching optional-route transcript or independent native/parser/vector witness for each unresolved boundary; then scoped server integration. | C:20529; M:18278; R:36-53; [STS transition](STS_TOKEN_KEY_TRANSITION_EVIDENCE.md). |
| F-002 diagnostic requests — Diagnostic | D: request intent and response policy for 12 numeric and four structural diagnostic surfaces. | Opcode-specific sender/callback/indirect rail or accepted payload; retain log-only handling otherwise. | C:20566; M:18281; [blocker plan](BLOCKER_EVIDENCE_PLAN.md). |
| F-003 server output — Structural | D:0015 owner and neutral cluster field/producer semantics. Old713/712 and62-packet counts are historical snapshots, not coverage proof. | Exact dispatch/consumer or accepted0015 payload, plus one mapped cluster at a time. | C:20647; M:18293; [aux roadmap](ENTITY_AUX_DECODE_ROADMAP.md). |
| F-004 housing — Partial | D/N: edit-mode ack, invite/eviction/roommate timing and permissions, donation costs/transfer, ownership/entitlements, vendor/refund precision, session/privacy/return behavior, legacy neighborhoods. P: physical travel/restart. | Narrow native consumer/capture and actual reviewed rows for each remaining rule; physical qualification for already committed Source flows. | C:20749; M:18299-18303; R:131-145 and later linked housing milestones. |
| F-005 marketplace — Complete* | D/N: property/rune/equippable filters, commodity partial-stack/retail matching precision, non-empty CREDD rows/history and remaining real marketplace packet semantics. P: current-provider/migration/socket/restart qualification. | Non-empty CREDD/filter/partial-fill fixtures or retail captures, then configured-provider/public-flow proof.06DF/07D5 are not marketplace gaps. | C:20872; M:18304; [marketplace settlement](NORMAL_AUCTION_BID_TRANSACTION.md). |
| F-006 storefront/account inventory — Partial | D/S/N: Protobucks/VC request-confirm path, nonempty026A pointer tail,0986/098F consumers,096A-C leading fields, offer-item type1/2 effects,0790 coupon sender. P: current migration/provider qualification. | Native billing/offer/coupon flow plus non-empty owned-order fixture; prove exact current migrations on disposable copies. | C:20975; M:18305. |
| F-007 reward rotation/tracks — Partial | D/S/N: per-content authoritative mapping, remaining flags/dynamic throttle slots, exact schedule selection and claim delivery. General track routing/account Loyalty foundations are completed, not new blockers. | Actual content/reward rows and complete earning/claim/reload proof, especially track35 Zone earning. | C:21111; M:18306; R:2375-2390; [track35](ZONE_REWARD_TRACK_35_EVIDENCE.md). |
| F-008 crafting — Partial | D/S/N: discovery rolls/unlock, complex/random/material rules, current/finish0860 ordering,084B/0855, sigil failures and056C patches. Station names/reset are resolved static inputs. | Exact other-handler/producer association or accepted craft fixture plus authoritative table policy. | C:21166; M:18307; five-gate table above. |
| F-009 transport — Partial | D/N: service-token bypass, global taxi/flight snapshots, embark/completion, passenger/seats, deployables and uncaptured charge/teleport cases. P: physical travel. | One native producer/vehicle-state trace and its actual route/seat corpus; qualified physical journey. | C:21445; M:18308; [taxi/mount record](TAXI_MOUNT_UPSTREAM_FIXES.md). |
| F-010 matching/group/raid — Partial | D/S/N: backfill/merge, aura deserter, durable raid locks/cross-realm pools, nonzero0718,0438/0468 fields/timing,05B0/05CC/05F1 flags,05CF owner,062A/0634 senders,0600 notification policy. | Exact remaining indexed/indirect owner or accepted nonzero/transition capture; then real public backfill/lock flow. | C:21746; M:18310; [matching tracker](MATCHING_IMPLEMENTATION_STATUS.md). |
| F-011 guild — Partial | S/D: guild bank economy/influence/tabs/slots/limits/perks, recruitment subscription/persistence, boss-token inventory/warplot state. P/N: holomark/UI/broker/physical timing. Combined guild creation finance/membership/affiliation atomicity remains open. | One persistent bank/recruitment/token outcome with reviewed rules, original-owner settlement and public publication/provider proof. | C:21874; M:18317; R:15-24; [guild recovery](GUILD_DISBAND_RECOVERY.md). |
| F-012 ICComm/chat/friendship — Partial | S/D/N: entitlement, persistent channels, precise delivery/leave/logout/throttle, auto-response and ignore-strangers persistence, aux producers. | Named entitlement/restore/aux owner or capture and a two-client persistence/delivery trace. | C:21940; M:18320. |
| F-013 mail — Complete* | D/N: delete/result/expiry parity. P: current provider/restart/socket and physical publication. Legacy Source recovery gaps must use later milestones, not old TODOs. | Exact deletion/expiry capture and current-provider public mail lifecycle, including attachment index corroboration. | C:21977; M:18323; R:2330-2342; [attachment index](MAIL_ATTACHMENT_INDEX_EVIDENCE.md). |
| F-014 loot — Complete* | D/N: BOP confirmation and first scalar, roll/master eligibility/UI, corpse/source identity, standalone CanLoot and aux timing, currency/static-item chat/floater presentation. | Accepted BOP/roll/master capture or exact consumer; one corpse/generated-loot source fixture. | C:22009; M:18325. |
| F-015 PvP/duels — Complete* | D/S/N: observer notices, forced-map/open-world policy, arena/BG score/reward/stat parity and manual Cryo-Plex/Daggerstone/Halls/Walatiki journeys. | Observer/reward producer trace and one native queue-to-match/reward smoke bundle. | C:22057; M:18327. |
| F-016 procs — Partial | D/N: unsupported events/targetData tails, chance/cooldown/recursion ordering and school edges. Evidence-tool debt: clean holder334 report; empty reports on holders1/294 do not prove missing runtime behavior. | Holder-specific proc fixture and captured event/order witness through the existing evidence boundary. | C:22104; M:18328. |
| F-017 damage/heal/vitals — Partial | D/N: split/distribution, damage/shield/absorb emits, HoT targets/ticks, SapVital/ClampVital modes, rounding/weapon budgets, Focus/healing ownership and pairwise Armor Pierce order. | Exact family fixture, pairwise damage path and actual coefficients across the rating threshold. | C:22104; M:18333; R:2402-2409; [healing](HEALING_PERSONAL_MODIFIER_EVIDENCE.md), [pierce](NATIVE_STATIC_ARMOR_PIERCE_EVIDENCE.md). |
| F-018 buffs/stack/CC/movement — Partial | D/N: stack arbitration, aura persistence, DR/breakout, tether/CC tails, force-move/facing physics and runtime packet parity.0810 count identity/timer refresh is mapped, not a replay-safe producer. | One stack/CC or movement fixture and exact native child/timer rule. | C:22104; M:18335; [spell aux](SPELL_AUX_CONSUMER_CORROBORATION_2026_11_05.md). |
| F-019 summons/traps/vehicles/AI — Partial | D/S/N: ownership/controller, triggers/formations/services, turrets/deployables/seats, NPC kits/Creature2Action and social aggro/leash policy. | One placed fixture with authoritative controller/trigger and real lifecycle; visual rows alone remain diagnostic. | C:22104; M:18336. |
| F-020 RavelSignal — Partial | D: receiver graph, modes/payloads and script-state effects. | Exact receiver/callback graph before generic effects. | C:22104; M:18337. |
| F-021 LAS/AMP/attributes — Partial | D/S: async UpdateSpellInProgress transaction, authoritative attribute allocation/refund, ability/AMP unlock persistence, bar locks and broader AMP families. Primal wire is implemented; allocation policy/settlement/persistence is not. | Actual Primal catalog/rewards plus validated settlement/recovery; native async transaction witness for LAS. | C:22292; M:18338; R:3-12; [Primal wire boundary](PRIMAL_MATRIX_WIRE_CONTRACTS.md). |
| F-022 quests/paths/events — Partial | D/S/N: mission edge payloads/waves/scans, reward/overflow/flags/unlock, Settler built-group/cost/avenues, event votes/scoreboard/phases, sharing, cinematics/choreography and remaining reviewed creature bridges. Communicator609 zero-bound/table qualification and Q5597/Hycrest remain separate. | Actual table/trigger/capture for one remaining producer or content slice, then scripted flow/manual proof. | C:22322; M:18340; R:2343-2362; [communicator cross-check](NATIVE_STATIC_CONTRACT_COMMUNICATOR_CROSSCHECK.md). |
| F-023 NPE/Rider's Reef — Partial | N/D: Exile/Dominion full quest/kill/respawn/CSI/reward playthrough, poses/visuals/SFX/84387, hoverboard reliability, terminal/checklist and four destination handoffs/VO. | Full faction native smoke with terminal destination/welcome quests and negative/duplicate cases. | C:23291; M:18341. |
| F-024 Evil from the Ether — Partial/WIP | D/N: phase/trigger/teleport/cinematic, exact sparks/rods/medbay visibility and cleanup, exploding-portal4942, gold timer/rewards, spawn difficulty, Katja/organism mechanics/achievements. | One encounter-phase table/client trace replacing its specific WIP guess, followed by expedition smoke. | C:23356; M:18342. |
| F-025 entity/visibility/CSI — Partial | D/S/N: remaining aux producers, deferred/current-object/target CSI, minigame/phasing, threat/stat cadence,08A8 flags, tracked marker tail/producer and Realm Bank open/snapshot/relog retail parity. | Exact handler/recipient/order or CSI state graph and native snapshot; use the implemented full-width marker contract without inventing an allocator or producer. | C:23390; M:18343; [current aux mapping](F025_STATIC_HANDLER_PROXY_MAPPING.md), [marker identity milestone](TRACKED_MARKER_IDENTITY.md). |
| F-026 items/unlocks/costumes/pets/titles — Partial | D/S/N: item/error/repair/satchel/costume/pet lifecycle and validation, generic unlock list/delta semantics and00B7/019A/037F/056B/C/D owners. Existing committed title/Archive/Item recovery is not reopened. | One exact aux/pet/unlock producer plus actual ownership/row evidence and public lifecycle. | C:23463; M:18346; R:2 and later title/Archive recovery records. |
| F-027 options — Complete* | D/S/N: additional types, account-vs-character split and initial/readback/result semantics. | Native producer/consumer or packet-backed initialization/readback before broader option types. | C:24056; M:18351. |
| F-028 support/reports/stuck — Complete* | S/D/N: database case lifecycle/readback, moderation/admin tooling, stuck result signaling/cooldown display and exact destinations. Append/flush/close ticket persistence already has Source evidence. | Designed case backend plus mapped readback/result protocol; exact stuck destination/result fixture. | C:24069; M:18352; R:55-61; [ticket persistence](SUPPORT_TICKET_PERSISTENCE.md). |
| F-029 client DB/data mapping — Complete* | D/S/P/N: case-by-case registrations/EF renames/promotions/migrations; external New-Zones-and-more overlay corpus/review; broad WIP encounters, placements/cleanup, hidden SMC schema and final content parity. | Exact source checkout and row-level delta/contract proof, safe import/migration validation and a focused runtime/content slice. | C:24085; M:18353; [content tracker](CONTENT_RETAIL_COMPLETENESS_TRACKER.md). |
| F-030 realm/character transfer/PTR — Partial | D/S/N: transfer destinations/success/handoff, TransferFlag,06E8 vs06E7 steps,06EA queue notice, new realm/messages,03EF payload and admin results. | Accepted copy/transfer handshake or exact sender/consumer/queue owner before mutation or queue notices. | C:24405; M:18354. |
| F-031 Fortune — Emulator only | D/N: exact item weights/active catalog and storefront/game synchronization. Hardcoded arrays, rarity tiers and F-007 schedules are not retail proof. | Accepted FortuneRewards capture or storefront-server catalog weights. | C:24484; M:18358; five-gate table above. |
| F-032 leaderboards — Complete* | D/N: absent season/medal request selectors, exact caps/cadence and live ingestion. RewardedTier response data is not a request filter. | Selector-bearing capture or actual request rule; row-cap/cadence and score-ingestion fixture. | C:24657; M:18360. |
| F-033 challenges — Partial | D/N: time-tier flag8, producer-specific collect/item/general, achievement/share-init ownership, native reward UI, exact retail RNG/reset/medal chance. | Native scoring/producer fixture and reward UI trace; retain accepted upstream uniform/all-claims/carry policy within its stated scope. | C:24677; M:18361; R:2399-2401. |
| F-034 datacubes/Archive — Complete* | D/N: content hookups, journal/datacube progression, path rule parity and wider UX. Committed Archive publication is separately complete. | One actual pickup-to-progress/unlock chain with native UI and path-rule evidence. | C:24694; M:18362; [Archive publication](ARCHIVE_PUBLICATION.md). |
| F-035 achievements — Complete* | D/S/N: unowned/data-only event families, Steam ASCII grammar/ID correlation, exact realm-first/UI timing and edge cases. Source-owned arbitration/publication is completed within its qualification boundaries. | Proven server event source or Steam grammar/ID witness, then exact native realm-first/achievement UI case. | C:24714; M:18363; [realm-first arbitration](REALM_FIRST_ARBITRATION.md). |
| F-036 maps/completion — Complete* | D/N: non-title/path rewards beyond current mapped title-only surface, UX/category totals and generic-map content-context producer. | Authoritative extra reward source, path-specific rule and native totals/context trace before broader grants. | C:24740; M:18364. |

## Cross-feature requirements outside the five-row list

| Requirement | Gate type and current distinction | Next artifact |
| --- | --- | --- |
| Contract public07CE | S/D/P/N: internal selection/claim/Auth compensation/account drain/wallet/actor foundations have later Source/provider gates. Public handler still lacks Contract type0/1 routing; unclaimed/final-first/excess/reset and all 726 canonical choices require complete delivery/recovery evidence. | Exact remaining reset-edge witness and full actual-table choice delivery/recovery, then public routing qualification; no synthetic item metadata or partial catalog. R:2208-2226; [reset evidence](CONTRACT_RETAIL_RESET_EVIDENCE.md). |
| Cross-owner financial/recovery publication | S/P: combined guild creation finance and parent/membership/affiliation atomicity, cross-character notification atomicity, other financial settlement, legacy remaining action/publication, restart/outbox/broker/provider proof. Later committed housing, Item, mail, achievement/title/Archive/tradeskill and guild milestones supersede older defect lists. | One original-owner public-flow reproduction and bounded settlement/publication fix; fresh provider/physical proof for that outcome. R:15-24,2227-2342. |
| Communicator609 and actual corpus | D/N: mapped zero reputation bounds/selected claim branches still need independent table/runtime/sniff/packet corroboration. The 481759-byte raw table with SHA256 6a46e0e5d3fed29e375206a82707e3c519c2f1ecb7e4a606dc987566f515810c is absent from the bounded lean/archive lookup. | Recover exact corpus or independent matching evidence; full-table classification/delivery plus native timing/UI. R:2343-2356. |
| Current destination qualification | P: fresh disposable MySQL/configured-strategy, model/copied-upgrade, actual full-table delivery, socket/deployment/load evidence. Old I-drive PID/schema/default snapshots do not qualify new destination DLLs. | Reproducible current-source/current-binary provider/runtime bindings and controlled copied-upgrade/public-flow results. R:2338-2342; [destination provider record](DESTINATION_PROVIDER_QUALIFICATION.md). |
| Content corpus and manual proof | D/N: every remaining quest/instance/encounter inventory remains slice-specific. Covers the named dungeons/expeditions, unresolved three Northern Wilds bridges, Hycrest/Q5597, zones and overlay encounters; the family table does not collapse these into one completed task. | Use generated next-slice queue/blocker rows and exact bridge/trigger/action/cleanup/reward/native smoke artifacts. [Content queue](CONTENT_RETAIL_COMPLETENESS_TRACKER.md#next-restoration-slice-queue). |
| Native client and Computer Use | N: current continuation retains the stop; earlier visual entry is not qualification of current binaries or Q5597/quest scenarios. | Resume only under an applicable later user instruction, then capture the specific missing native scenario. R:2356,2414-2417. |
| Container-backed Aspire execution | P: recorded 5 November read-only host check found no Docker CLI/service/Desktop executable or matching engine pipe. Current command discovery found only dotnet among docker/podman/mysql/dotnet; PATH absence does not prove no remote/provider engine exists. | A usable configured engine and actual current Aspire container run; no installation/daemon launch was performed here. [Recorded host check](../../artifacts/continuation-20261105/blocker-unblock-research/docker-availability.json). |

## Stale claims that must not become repeat blockers

- Missing REA native provider is still a provider limitation, but standalone
  read-only Ghidra12.0.4/LLVM inspection is recovered. Old “no static decompilation
  possible” statements are superseded; no configuration repair is implied.
- Optional STS is not wholly unknown: RSA component envelope, selected validation,
  mode4 expansion/filter ownership are mapped. Trust/server inverse/padding,
  generic grammar and accepted optional-route traffic remain.
-06DF is Primal Matrix;07D5 is reward-track update. The Primal public wire decoder
  and snapshot registration are now Source-verified, while its handler is
  diagnostic and allocation gameplay remains open.
- Matching0600 has an indexed consumer; C2S role ordinals convert to bits before
  class filtering. Incoming numeric role-localization labels/notification policy
  and actual backfill remain separate.05CF's former candidate is not dispatch proof.
- Crafting keys 2C/4F/57 and observed cache reset/replacement are mapped. These
  do not close084B/0855 or discovery/complex-craft gates.
- The first 32-bit tracked field is TrackingSlot identity. Its final 15 bits are
  neutral. The [Source identity/mask correction](TRACKED_MARKER_IDENTITY.md) is now
  implemented and scoped verified; do not reopen it from historical field names
  or naming tests. Its 118 affected cases belong to that separate implementation
  milestone, not this audit. The producer, slot-selection and trailing-field
  gates remain open.
- Six entity-stat apply cases are mapped; scanner08CC production is implemented.
  Do not report all six as reader-only/no-emitter or extrapolate that producer
  to the other five.
- Broad Source TODO counts, registered opcode totals and large historical test
  counts are not retail qualification. The original 13/21 Quick Stats aggregate
  is now corrected to 11/23 with individual feature labels unchanged; old
  COMPLETE headings retain their explicit retail gaps.

## Inventory verification

Read the four requested canonical documents, their current November supplements,
the linked Primal/STS/tracking/station records, the content milestone state and
the recorded host prerequisite check. Counted 32 overview headings with F-016..020
expanded to 36 areas; all 36 are represented individually above. The five producer
rows remain a separate classification, and cross-feature public/recovery/provider/
content/native/container gates are included. Local links and whitespace were
checked. No C# build/test result, native execution, setup change or retail closure
is claimed by this documentation-only audit.
