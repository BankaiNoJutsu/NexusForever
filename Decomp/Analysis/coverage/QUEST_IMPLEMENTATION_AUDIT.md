# Quest Implementation Audit

- Generated: 2026-06-12
- Generator: `Tools/WikiArchiveAudit/quest_implementation_audit.py`
- Client SQL: `wildstar_client_mysql`
- Server scripts: `Source\NexusForever.Script.Main`
- Status summary: `Decomp/Analysis/QUEST_IMPLEMENTATION_STATUS.md`

## Executive summary

| Metric | Count | % of total |
| --- | ---: | ---: |
| Total client quests (`Quest2`) | 5194 | 100% |
| Quests with at least one objective | 3903 | 75.1% |
| Quests with **no objectives** (shared accept-to-turn-in lifecycle, pending dialog/client smoke) | 1291 | 24.9% |
| **Fully curated** (hand-tuned script chains) | 49 | 0.9% |
| **Generic table-driven** (supported objective types) | 3854 | 74.2% |
| **Partial** (partial objective mix or stub path scripts) | 0 | 0.0% |
| **Blocked / missing gameplay** (unsupported objective types) | 0 | 0.0% |
| Current `Quest2` ids with `ScriptFilterOwnerId` | 67 | 1.3% |
| Distinct `ScriptFilterOwnerId` owner ids in `Script.Main` | 87 | 1.7% |

### Quests with objectives only

| Tier | Count | % of quests with objectives |
| --- | ---: | ---: |
| Completable (generic + curated) | 3903 | 100.0% |
| Partial | 0 | 0.0% |
| Blocked | 0 | 0.0% |

## Interpretation

- **Lifecycle** (accept, track, abandon, complete, rewards) is implemented for all `Quest2` rows via `GlobalQuestManager` / `QuestManager`.
- **Fully curated** means a dedicated `IQuestScript` chain was hand-built and verified (tutorial, Northern Wilds, Crimson Isle focus areas).
- **Generic table-driven** quests can progress through shared kill/talk/activate/CSI/enter-zone/virtual-collect handlers without a per-quest script.
- **Blocked** quests contain objective types with no mapped server trigger; no current client quest objectives use the remaining unsupported types.
- Counts are objective-type coverage, not in-game QA. Generic and no-objective quests still need world spawns, row-specific dialog/receiver validation, volumes, and loot.

## Bucket detail

| Bucket | Quests | Notes |
| --- | ---: | --- |
| `generic_table_driven` | 3840 | All objectives supported generically; no custom script |
| `no_objectives` | 1291 | No objective slots populated in Quest2; shared QuestManager accept and visible-receiver turn-in paths are test-backed pending row-specific receiver/dialog and reward smoke |
| `curated_full` | 49 | Hand-tuned quest script; verified starter/endgame focus chains |
| `generic_with_script` | 14 | Supported objectives plus extra script (often follow-up grant only) |

## Unsupported objective types (quest objective slots)

| Type | Slots |
| --- | ---: |

## Curated quest ids

```text
3479, 3480, 3486, 3487, 3667, 3668, 3670, 3671, 3673, 3797, 3886, 3963, 5573, 5575, 5580, 5583, 5584, 5593, 5594, 5595, 5596, 5597, 5604, 5610, 8855, 10510, 10513, 10518, 10519, 10520, 10521, 10522, 10523, 10524, 10525, 10526, 10527, 10528, 10530, 10532, 10540, 10541, 10544, 10545, 10547, 10548, 10550, 10551, 10553, 10554, 10556, 10558
```

## Custom script ids (all `ScriptFilterOwnerId` values)

```text
51, 154, 426, 870, 990, 1387, 1658, 2979, 2997, 3460, 3479, 3480, 3486, 3487, 3667, 3668, 3670, 3671, 3673, 3741, 3777, 3781, 3783, 3797, 3886, 3963, 4526, 4666, 4667, 4694, 4696, 5573, 5575, 5580, 5583, 5584, 5593, 5594, 5595, 5596, 5597, 5604, 5610, 8855, 10510, 10513, 10518, 10519, 10520, 10521, 10522, 10523, 10524, 10525, 10526, 10527, 10528, 10530, 10532, 10540, 10541, 10544, 10545, 10546, 10547, 10548, 10549, 10550, 10551, 10552, 10553, 10554, 10555, 10556, 10557, 10558, 10559, 26443, 37302, 56145, 56146, 56147, 56148, 56149, 56150, 56151, 56152
```
