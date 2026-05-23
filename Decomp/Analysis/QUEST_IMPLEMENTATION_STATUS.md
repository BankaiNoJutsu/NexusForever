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
| **Fully curated** (hand-tuned `IQuestScript` chains) | 37 | 0.7% |
| **Generic table-driven** (supported objective types) | 3,021 | 58.2% |
| **Partial** (stub path scripts or mixed partial types) | 212 | 4.1% |
| **Blocked** (unsupported objective types) | 633 | 12.2% |
| Any `ScriptFilterOwnerId` hook | 68 | 1.3% |

For the **3,903 quests with objectives**:

| Tier | Quests | % |
| --- | ---: | ---: |
| Completable via generic handlers or curated scripts | ~3,061 | ~78.4% |
| Partial | 212 | 5.4% |
| Blocked | 633 | 16.2% |

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
| Rider's Reef tutorial | 3460 | 10513–10532, 10540, 10541 (+ combat 10518/10524) |
| Northern Wilds | 426 | 3479, 3480, 3667–3671, 3486, 3487, 3797, 3886, 3963, 3673, 3670 |
| Crimson Isle | 870 | 5573, 5575, 5580, 5583, 5584, 5594–5597, 5604, 5610, 8855 |

Map scripts and entity hooks live under `Source/NexusForever.Script.Main/Quests/`.

## Blocked objective families (top gaps)

These types have client rows but no generic `ObjectiveUpdate` trigger in server code:

| Type | Enum | Objective slots |
| ---: | --- | ---: |
| 31 | `Unknown31` | 260 |
| 4 | `CollectItem` | 197 |
| 25 | `CompleteEvent` | 151 |
| 24 | `CompleteQuest` | 38 |
| 33 | `CraftSchematic` | 34 |

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
