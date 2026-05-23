# Quest Implementation Audit

- Generated: 2026-05-23
- Generator: `Tools/WikiArchiveAudit/quest_implementation_audit.py`
- Client SQL: `wildstar_client_mysql`
- Server scripts: `Source\NexusForever.Script.Main`
- Status summary: `Decomp/Analysis/QUEST_IMPLEMENTATION_STATUS.md`

## Executive summary

| Metric | Count | % of total |
| --- | ---: | ---: |
| Total client quests (`Quest2`) | 5194 | 100% |
| Quests with at least one objective | 3903 | 75.1% |
| Quests with **no objectives** (mention/placeholder rows) | 1291 | 24.9% |
| **Fully curated** (hand-tuned script chains) | 37 | 0.7% |
| **Generic table-driven** (supported objective types; no custom script) | 3021 | 58.2% |
| **Partial** (partial objective mix or stub path scripts) | 212 | 4.1% |
| **Blocked / missing gameplay** (unsupported objective types) | 633 | 12.2% |
| Distinct quest ids with `ScriptFilterOwnerId` | 68 | 1.3% |

### Quests with objectives only

| Tier | Count | % of quests with objectives |
| --- | ---: | ---: |
| Completable (generic + curated) | 3058 | 78.3% |
| Partial | 212 | 5.4% |
| Blocked | 633 | 16.2% |

## Interpretation

- **Lifecycle** (accept, track, abandon, complete, rewards) is implemented for all `Quest2` rows via `GlobalQuestManager` / `QuestManager`.
- **Fully curated** means a dedicated `IQuestScript` chain was hand-built and verified (tutorial, Northern Wilds, Crimson Isle focus areas).
- **Generic table-driven** quests can progress through shared kill/talk/activate/CSI/enter-zone/virtual-collect handlers without a per-quest script.
- **Blocked** quests contain objective types with no mapped server trigger (for example `CollectItem`, `CompleteEvent`, `CraftSchematic`, `Unknown31`).
- Counts are objective-type coverage, not in-game QA. Generic quests still need world spawns, volumes, and loot.

## Bucket detail

| Bucket | Quests | Notes |
| --- | ---: | --- |
| `generic_table_driven` | 3021 | All objectives supported generically; no custom script |
| `no_objectives` | 1291 | No objective slots populated in Quest2 |
| `blocked_objectives` | 633 | Contains unsupported objective types; will not progress without new handlers |
| `partial_objectives_only` | 190 | Mix of supported + partial objective types (e.g. SpellSuccess) |
| `curated_full` | 37 | Hand-tuned quest script; verified starter/endgame focus chains |
| `curated_partial_stub_script` | 22 | Has quest script file but only logging / path-tutorial stubs |

## Unsupported objective types (quest objective slots)

| Type | Slots |
| --- | ---: |
| 31 | 260 |
| 4 | 197 |
| 25 | 151 |
| 24 | 38 |
| 33 | 34 |
| 20 | 11 |
| 21 | 9 |
| 44 | 7 |
| 28 | 4 |
| 40 | 2 |
| 39 | 2 |
| 41 | 2 |
| 42 | 2 |
| 48 | 2 |
| 46 | 1 |

## Curated quest ids

```text
3479, 3480, 3486, 3487, 3667, 3668, 3670, 3671, 3673, 3797, 3886, 3963, 5573, 5575, 5580, 5583, 5584, 5594, 5595, 5596, 5597, 5604, 5610, 8855, 10513, 10518, 10519, 10520, 10521, 10522, 10523, 10524, 10525, 10526, 10527, 10528, 10530, 10532, 10540, 10541
```

## Custom script ids (all `ScriptFilterOwnerId` values)

```text
426, 870, 1658, 2979, 2997, 3460, 3479, 3480, 3486, 3487, 3667, 3668, 3670, 3671, 3673, 3797, 3886, 3963, 5573, 5575, 5580, 5583, 5584, 5594, 5595, 5596, 5597, 5604, 5610, 8855, 10510, 10513, 10518, 10519, 10520, 10521, 10522, 10523, 10524, 10525, 10526, 10527, 10528, 10530, 10532, 10540, 10541, 10544, 10545, 10546, 10547, 10548, 10549, 10550, 10551, 10552, 10553, 10554, 10555, 10556, 10557, 10558, 10559, 10563, 10566, 10568, 10569, 10570
```
