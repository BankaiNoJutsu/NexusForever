# Quest Implementation Status

Last updated: 2026-05-23

Canonical quest coverage tracker for NexusForever. Pair with feature row **F-022**
in `CURRENT_STATUS.md` and the generated inventory in
`Decomp/Analysis/coverage/QUEST_IMPLEMENTATION_AUDIT.md`.

## Quick counts (client build 16042)

| Tier | Quests | % of 5,194 |
| --- | ---: | ---: |
| **Total `Quest2` rows** | 5,194 | 100% |
| Quest lifecycle only (no objectives in table) | 1,291 | 24.9% |
| **Fully curated** (hand-tuned `IQuestScript` chains) | 48 | 0.9% |
| **Generic table-driven** (supported objective types) | 3,853 | 74.2% |
| **Partial** (mixed partial objective types) | 0 | 0.0% |
| **Blocked** (unsupported objective types) | 0 | 0% |
| Any `ScriptFilterOwnerId` hook | 63 | 1.2% |

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

## Fully curated zones (hand-built scripts)

| Zone | World id | Quest ids (representative) |
| --- | ---: | --- |
| Rider's Reef tutorial | 3460 | 10513-10532, 10540, 10541 (+ combat 10518/10524) |
| Northern Wilds | 426 | 3479, 3480, 3667-3671, 3486, 3487, 3797, 3886, 3963, 3673, 3670 |
| Crimson Isle | 870 | 5573, 5575, 5580, 5583, 5584, 5594-5597, 5604, 5610, 8855 |

Map scripts and entity hooks live under `Source/NexusForever.Script.Main/Quests/`.

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
