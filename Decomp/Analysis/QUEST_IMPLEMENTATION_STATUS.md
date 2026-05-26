# Quest Implementation Status

Last updated: 2026-05-25

Canonical quest coverage tracker for NexusForever. Pair with feature row **F-022**
in `CURRENT_STATUS.md` and the generated inventory in
`Decomp/Analysis/coverage/QUEST_IMPLEMENTATION_AUDIT.md`.

## Quick counts (client build 16042)

These counts measure table/objective handler support and curated script
coverage. They are not manual gameplay-complete claims; Rider's Reef remains
in progress until the client smoke items below are verified.

| Tier | Quests | % of 5,194 |
| --- | ---: | ---: |
| **Total `Quest2` rows** | 5,194 | 100% |
| Quest lifecycle only (no objectives in table) | 1,291 | 24.9% |
| **Fully curated** (hand-tuned `IQuestScript` chains) | 56 | 1.1% |
| **Generic table-driven** (supported objective types) | 3,853 | 74.2% |
| **Partial** (mixed partial objective types) | 0 | 0.0% |
| **Blocked** (unsupported objective types) | 0 | 0% |
| Any `ScriptFilterOwnerId` hook | 72 | 1.4% |

For the **3,903 quests with objectives**:

| Tier | Quests | % |
| --- | ---: | ---: |
| Completable via generic handlers or curated scripts | 3,903 | 100% |
| Partial | 0 | 0.0% |
| Blocked | 0 | 0% |

## Generic objective handlers (2026-05-23)

- `SpellSuccess` / `SpellSuccess2` / `SpellSuccess3` / `SpellSuccess4`: credited when a
  player-owned spell reaches execute (`SpellQuestObjectiveUpdater` in `Spell.Execute`).
- `CompleteQuest`: credited when another quest is turned in (`QuestManager` completion).
- `CollectItem`: `Data` = `Item2Id` (274/327 rows; client `QuestObjective.tbl` crosswalk).
  Synced from inventory on item create/stack increase and quest accept
  (`InventoryQuestObjectiveUpdater`).
- `CompleteEvent` / `Unknown31`: `Data` = `PublicEventObjective.Id` (111/142 and 206/257 rows
  vs `PublicEventObjective.tbl`). Credited when a public-event objective succeeds
  (`PublicEventQuestObjectiveUpdater`).
- `CraftSchematic`: `Data` = `TradeskillSchematic2.Id` for recipe crafts (36/171 rows) and
  overlaps public-event ids for event crafts. Credited on successful fixed-recipe craft
  (`CraftingQuestObjectiveUpdater`) plus the public-event path above.
- `ActivateTargetGroup`: credited on direct interaction paths (matches spell activate
  effect) via `InteractionObjectiveUpdater`.
- `GatheResource`: `Data` = `CreatureId`; credited on creature kill and interaction.
- `Unknown20` / `Unknown28` / `CombatMomentum`: `Data` = `PublicEventObjective.Id`
  (same crosswalk as types 25/31); credited on public-event objective success.
- `KillCreature2`: `Data` = `Creature2Difficulty.Id`; credited on kill using difficulty id.
- `EarnCurrency`: `Data` = `CurrencyType`; credited when currency is granted.
- `ParticipateInGroupContent`: `Data` = `MatchingGameType.Id`; credited on match enter.
- `PvPKills`: credited when a player kills another player (`Data` 0 or victim map id).
- `CompleteMaxLevelQuests`: credited when a level-50 player completes a quest.
- `BeginMatrix`: `Data` = 0; credited on account primal-essence grants (types 15-18) and
  quest-accept sync when essence is already held (`PrimalMatrixQuestObjectiveUpdater`).

## What is implemented everywhere

- All `Quest2` rows load through `GlobalQuestManager` / `QuestInfo`.
- Accept, abandon, retry, track, share, objective updates, completion, repeat
  resets, and standard reward grants (item, money, reputation, XP, tradeskill,
  account currency).
- Prerequisite validation: level, faction, class, prerequisite quests,
  `prerequisiteId`, items, exclusions, faction-level gates.
- Wiki/runtime audit for lifecycle surfaces: `Tools/WikiArchiveAudit/reports/quests-wiki-audit.md`.

## Hand-curated focus areas

| Zone | World id | Quest ids (representative) | Gameplay status |
| --- | ---: | --- | --- |
| Rider's Reef tutorial | 3460 | 10513-10532, 10540, 10541 (+ combat 10518/10524) | In progress, not complete |
| Northern Wilds | 426 | 3479, 3480, 3486, 3487, 3667, 3668, 3670, 3671, 3673, 3741, 3777, 3781, 3783, 3797, 3886, 3963, 4526, 4666, 4667, 4696 | Script-chain covered; local spawn promotion verified to 161/164 enabled Jabbithole creatures; manual smoke still pending |
| Crimson Isle | 870 | 5573, 5575, 5580, 5583, 5584, 5593-5597, 5604, 5610, 8855 | Script-chain covered |

Map scripts and entity hooks live under `Source/NexusForever.Script.Main/Quests/`.

## Build 16042 start model

For the NexusForever build 16042 target, **Rider's Reef** (`world 3460`) is the
Novice new-player experience for both factions. Do not replace it with the older
faction arkship zones when working toward 16042 parity.

| Character creation path | Exile destination | Dominion destination |
| --- | --- | --- |
| Novice (`CreationStart = 4`) | Rider's Reef (`world 3460`) | Rider's Reef (`world 3460`) |
| Veteran (`CreationStart = 3`) | Human/Granok: Northern Wilds (`world 426`); Aurin/Mordesh: Everstar Grove (`world 990`) | Chua/Draken: Crimson Isle (`world 870`); Cassian/Mechari: Levian Bay (`world 1387`) |
| Level 50 (`CreationStart = 5`) | Thayd capital start | Illium capital start |

The Rider's Reef finale uses departure terminals for the same four surface
starter zones: Exile terminals route to Everstar Grove or Northern Wilds, while
Dominion terminals route to Crimson Isle or Levian Bay. The routing helper also
pins the destination welcome quests (`9113`, `9112`, `9127`, `9126`) that unlock
from final Rider's Reef quest completion. Public archived
WildStar docs describe **Gambler's Ruin** (Exile) and **Destiny** (Dominion) as
older level 1-3 arkship training zones with exits to those planet starters, and
note that the arkship tutorial could be skipped after the free-to-play/Reloaded
starting-flow changes. Treat Gambler's Ruin and Destiny as historical/older
patch restoration targets unless a task explicitly asks for pre-16042 arkship
parity.

## Northern Wilds current state (2026-05-25)

Table-backed chain coverage now pins the 20-quest Human/Granok Veteran block:
`3479`, `3480`, `3486`, `3487`, `3667`, `3668`, `3670`, `3671`, `3673`,
`3741`, `3777`, `3781`, `3783`, `3797`, `3886`, `3963`, `4526`, `4666`,
`4667`, and `4696`. The pass added scripts for the previously missing
`3741`, `3777`, `3781`, `3783`, `4526`, `4666`, `4667`, and `4696` rows,
including Jabbithole/objective-table-backed credit for Exile Supply Crates
(`4813`), Loftite Crystals (`5076` plus item `6998` collection credit), and
Captive Exile Soldiers (`4880`). Focused regressions cover the Q3486 and Q3673
cinematic/quest-handoff boundaries in
`Source/NexusForever.Game.Tests/Quests/NorthernWildsQuestChainTests.cs`, and
`Source/NexusForever.Game.Tests/Quests/EarlyZoneEntityObjectiveCreditTests.cs`
pins Q3667 control-panel objective `4770`, Q3487 cannon checklist credit,
Q3741 crate credit, Q3777 crystal credit, and Q3781 captive rescue credit.

Northern Wilds event/challenge/path state from the 2026-05-25 Veteran log pass:

- Public event: Jabbithole zone `1` maps one event, `154` Dominion Ultrabot,
  with objective `371` ("Defeat the Dominion Ultrabot in Camp Icefury"). The
  Northern Wilds map script now creates/joins the event in Camp Icefury
  (`WorldZone 602`), and `DominionUltrabotPublicEventScript` credits objective
  `371` when creature `12526` dies.
- Challenges: Jabbithole zone `1` maps `103` Skeech Slayer and `105`
  Rootbrute Slayer. Client `Challenge.target` values for these are target-group
  IDs (`8851`, `1852`), so `ChallengeManager` now resolves direct and nested
  `TargetGroup.tbl` creature lists before advancing combat challenges. The
  logged `ChallengeRequirement` prerequisite warning is now backed by a
  completion-count check against `IChallengeManager.GetCompletionCount`.
- Path missions: client `PathMission.pathEpisodeId` yields the 13 Northern
  Wilds missions (`33`, `34`, `156`, `35`, `36`, `158`, `1254`, `42`, `160`,
  `648`, `650`, `651`, `652`). The map script activates the correct episode
  and Jabbithole XP value (`25`) for the player's active path, while Explorer
  progress and power-map client reports can now complete active missions through
  `PathManager`. Soldier tower-defense build packets now resolve
  `PathSoldierTowerDefense -> PathSoldierEvent -> PathMission`, and Settler
  build-tier packets resolve `PathSettlerImprovementGroup -> PathSettlerHub ->
  PathMission`. Jabbithole `path_mission_creatures` now backs the Northern
  Wilds Soldier control-point hook:
  `Source/NexusForever.Script.Main/Quests/NorthernWilds/NorthernWildsSoldierPathMissionScripts.cs`
  completes missions `33`, `34`, and `156` from Creature2 control points
  `11139`, `11141`, and `12508` and emits `ServerPathSoldierHoldoutEnd`
  success. Jabbithole `path_mission_creatures` also backs the Northern
  Wilds Scientist runtime hook: `Source/NexusForever.Script.Main/Quests/NorthernWilds/NorthernWildsScientistPathMissionScripts.cs`
  emits `ServerPathScientistAddCreatureInfoToCreature` and
  `ServerPathScientistUnitScanParameters` for mapped mission creatures and
  completes missions `42`, `160`, and `648` on activation success. Generic
  Soldier wave simulation and the generic Scientist scan-result packet remain
  mapped-only pending packet/result evidence.
- NPC data: Jabbithole has `164` enabled Northern Wilds creatures. The staging
  bridge now maps `161` of them to client Creature2 IDs and leaves `3`
  unmatched (`Ability Training Kiosk`, `Invisible Channel Unit`, and `Unknown`).
  `Tools/DataMapping/sql/apply_safe_world_imports_from_staging.sql` now has
  opt-in entity-spawn promotion from `nf_map_world_entity_candidate` plus a
  targeted direct Jabbithole coordinate fallback for source rows whose
  `worldid` is blank but whose coordinates resolve near staged world geometry.
  Local verification promoted `4,375` mapped entity rows (`1000000000 +
  source_coordinate_id`) and `15,423` per-spawn stat rows across world `426`
  plus the quest-relevant world `51` subareas `23`, `973`, and `976`. Northern
  Wilds runtime world `426` coverage rose from `90` to `161` enabled
  Jabbithole creatures. The reviewed local bridge aliases were `1055`
  Quartermaster Windward -> `11705` Supply Officer Windward, `3351` Basic
  Critical Strike Station -> `25876` Basic Critical Hit Station, and `11313`
  Hacked Dominion Turret -> `18680` Inactive Dominion Turret. The remaining `3`
  enabled zone-1 entries need stronger Creature2 evidence before runtime
  promotion; this is the accepted safe stop boundary for the Veteran Northern
  Wilds pass.

## Crimson Isle current state (2026-05-25)

Table-backed chain coverage now pins the branch roots and merge gates:
Q5595 grants Q5575; Q5593 grants Q5573 and Q8855; Q5573 or Q5575 can unlock
Q5596 via `preq_flags = 1`; Q5596 grants Q5597; Q5597 grants Q5604; Q5604
grants both Q5580 and Q5583; Q5594 is only granted after both Q5580 and Q5583
are complete. Q5594's final warbot kill credits objective `8249` and guards
achievement `1730` if the character already has it. Focused regressions live in
`Source/NexusForever.Game.Tests/Quests/CrimsonIsleQuestChainTests.cs`.
`Source/NexusForever.Game.Tests/Quests/EarlyZoneEntityObjectiveCreditTests.cs`
also pins Q8855 Dominion soldier activation/despawn objective credit.

## Rider's Reef current state (2026-05-25)

Rider's Reef is not complete. Verified code paths now include tutorial
communicator checkpoint ordering, combat-projector quest delta recovery,
map-cache fallback anchoring, delta-only tutorial quest initialisation, build 16042
character-create start rows, tutorial quest follow-up grants, terminal-to-world
routing, terminal fallback behavior, destination welcome-quest grants, terminal
final quest completion before visible surface receiver, no-duplicate/no-teleport
guards, terminal recording on spell/direct activation,
direct activation checklist credit for the actual departure checklist
interactables, per-player terminal storage,
known-terminal filtering, cross-faction/missing terminal default routing,
both-faction starter quest grants and follow-up re-entry root-skip behavior,
logout/re-entry recovery boundary excluding the final departure quests from
auto-completion,
table-backed reward coverage for cryopod Omnibit and final-departure item
rewards,
fallback-spawned departure terminals/checklist
interactables, combat lane faction overrides, and
faction-specific projector text. Focused tests live
under
`Source/NexusForever.Game.Tests/Map` and
`Source/NexusForever.Game.Tests/Quests`, with the spell-activate hook pinned in
`Source/NexusForever.Game.Tests/Spell`.

Timestamped evidence matrix:
`Decomp/Analysis/coverage/RIDERS_REEF_EVIDENCE_MATRIX_2026-05-25.md`.

Remaining gameplay verification:

- Exile and Dominion client playthroughs for quest acceptance, objective
  updates, turn-in UI, rewards, and chain handoff.
- Combat and hoverboard loops for respawn/re-entry, kill credit, CSI/projector
  reliability, and final terminal/checklist behavior.
- Retail VO/timing comparison against tutorial video evidence and runtime logs.
- Read-only local DB/log audit on 2026-05-25 proved only partial early NPE
  coverage: 28 local characters were still on `worldId=3460`; one local
  character was on `426` at the Human/Granok Veteran start coordinates; no
  character rows were on `990`, `870`, or `1387`, and no persisted `10528`,
  `10530`, `9112`, `9113`, `9126`, or `9127` rows were found.
- Initial-chain correction on 2026-05-25 now grants only the movement root
  (`10513` Exile / `10521` Dominion) on fresh Rider's Reef entry; the paired
  hoverboard quest (`10527` / `10532`) is recovered only after the movement root
  is completed. The focused Rider's Reef filter passed 79/79 after the change,
  and the narrow map/chain slice passed 17/17. Auth/World restart was clean, but
  full Exile/Dominion client playthrough smoke remains blocked for native-client
  automation safety.
- Follow-up correction from live UI smoke on 2026-05-25 removed the mid-play
  `ServerQuestInit` refresh after the movement root auto-advances into the
  hoverboard quest. `QuestAdd` already emits the live state delta; avoiding the
  extra quest snapshot keeps the "three points -> platform -> projector" opening
  beat to one client-visible tutorial chain. `QuestManager.SendInitialPackets`
  still suppresses the completed movement root during login snapshots when the
  paired hoverboard quest is active. The latest focused quest/map/chain/projector
  filter passed 35/35 and the broader `FullyQualifiedName~Tutorial` filter
  passed 74/74.
- Hoverboard presentation/recovery pass on 2026-05-25 added table-backed
  objective-ring lightning FX (`82460`), projector-completion trail start
  (`82298`) when `10527`/`10532` grants the hoverboard, booster speed/visual casts
  (`85424`/`82298`) plus a stronger forward boost, finish snap to
  `WorldLocation2 51734`, direct remount recovery through hoverboard equip
  `85562` while the ride objective is incomplete, and mounted-vehicle
  ring/booster pilot targeting regressions. A follow-up removes the wrong
  opening step-pad `81662` scan cast, keeps a narrow suppression for only
  `81662`'s server-applied `CCState.Disable` effect after live smoke showed it
  presenting "Disable" and stranding movement, and leaves the exact opening pad
  player pose/SFX blocked pending direct producer evidence. Follow-up live
  validation narrowed ring contact back to `82460` only and leaves `84387`
  Power Boost pulse/trail/speed mapped-only until the exact retail producer is
  proven; it also makes projector activation robust by casting/falling back to
  hoverboard equip `85562` from both direct and cast activation handlers. A
  follow-up finish-snap correction
  now also snaps to `51734` when the ride objective was completed by the normal
  area-objective sync before recovery sees it, as long as the finish
  interactable objective is still incomplete. Focused effect/communicator tests
  passed 20/20 after rebuilding `Game`, `Script.Main`, and `Game.Tests`; the
  finish-snap/hoverboard/opening-chain filter passed 58/58, the broader
  `FullyQualifiedName~Tutorial` filter passed 66/66, the scan-disable focused
  filter passed 22/22, the ring Power Boost remap focused filter passed 23/23,
  the projector/ring correction filter passed 11/11, the broader correction
  slice passed 82/82, the rebuilt tutorial filter passed 78/78, and the owning
  `Game`/`Script.Main` builds succeeded in the earlier same-day pass.

## Rider's Reef Codex boundary (2026-05-25)

- Tutorial quests `10513..10541` can be active, tracked, and objective-synced, but they are not ordinary browseable quest-log/Codex rows in the stock client.
- Probe result: after moving `ServerQuestInit` to the post-`ClientEnteredWorld` bootstrap and replaying quest state/objective deltas, an injected comparison quest (`6708`) appeared in the Codex while the Rider's Reef chain still did not.
- Client data cause: the Rider's Reef rows in `wildstar_client.Quest2` have `groupId = 0`, `questCategoryId = 0`, `questContentFinderTypeEnum = 0`, and no `EpisodeQuest` rows. Treat them as HUD task-style tutorial content unless client tables or client UI logic are patched.

## Blocked objective families (remaining)

No quest objectives use unsupported types 19, 27, or 29 in client data. Primal Matrix
UI open packets remain unmapped; `BeginMatrix` (type 48) uses essence-grant sync above.

Reward gap: **86** `RotationEssence` (`Quest2Reward` type 10) rows need active
reward-rotation schedule support.

## Regenerate reports

From the repository root, with `wildstar_client_mysql` present:

```powershell
# Quest coverage buckets (this tracker + coverage/QUEST_IMPLEMENTATION_AUDIT.md)
python Tools\WikiArchiveAudit\quest_implementation_audit.py

# Wiki + lifecycle/objective/reward surface audit (optional wiki archive)
python Tools\WikiArchiveAudit\audit_wildstar_wiki.py `
  --domain quests `
  --archive-dir artifacts\wildstar-fandom-wiki-2026-05-18 `
  --output Tools\WikiArchiveAudit\reports\quests-wiki-audit.md
```

## `artifacts/` vs tracked files

| Location | Git | Keep? |
| --- | --- | --- |
| `artifacts/` | ignored | Local scratch: wiki dump, load-test JSON, build `verify-obj`, spell/loot capture JSON |
| `Tools/WikiArchiveAudit/reports/` | tracked | Committed audit snapshots (wiki/lifecycle) |
| `Decomp/Analysis/coverage/QUEST_IMPLEMENTATION_AUDIT.md` | tracked | Generated quest bucket inventory |
| `Tools/WikiArchiveAudit/quest_implementation_audit.py` | tracked | Generator for quest bucket inventory |

Do **not** commit `artifacts/wildstar-fandom-wiki-*` (tens of thousands of pages),
`artifacts/verify-obj`, `artifacts/review-bin`, or per-run load-test output.
