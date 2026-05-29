# LaughingWS Quest Virtual Loot Review

Date: 2026-05-27

## Decision

LWS-055 remains mapped-only. The branch virtual-item quest loot remains preserved
as safe objective-gated loot data, but retail trigger cadence, probability, and
objective integration are not proven by the source SQL alone.

Do not widen quest-loot behavior, probability rules, or objective-side logic
from `Loot/CreatureQuestLoot.sql` without representative live-client capture.

## Preserved Data

`Tools/DataMapping/sql/laughingws_quest_loot_seed.sql` preserves `7,650` rows:

| Table | Rows | Notes |
| --- | ---: | --- |
| `loot_group` | 1,174 | Source ids shifted by `+1200000000`; all current rows are `QuestObjectiveActive` condition `8`. |
| `loot_item` | 1,174 | All current rows are `LootItemType.VirtualItem` (`6`). |
| `entity_loot` | 5,302 | Creature-to-loot-group links. |

The extractor validates that every `entity_loot`, `loot_item`, and child group
reference resolves after id shifting. The parsed seed currently spans `587`
distinct quest objective ids, `494` distinct virtual item ids, and `2,062`
distinct entity ids.

Representative parsed rows:

| Loot group | Objective | Virtual item | Example comment |
| ---: | ---: | ---: | --- |
| `1200000001` | `13011` | `265` | Cold Survival Kit for quest objective `13011`. |
| `1200000002` | `12605` | `190` | Torine Weapon Fragment for quest objective `12605`. |
| `1200000003` | `6700` | `340` | Cannon Activation Code for quest objective `6700`. |
| `1200000004` | `6313` | `335` | Koryn Surne's Datachron for quest objective `6313`. |
| `1200000005` | `21278` | `1221` | Conduit Core for quest objective `21278`. |

## Current Runtime Boundary

Current runtime support makes the preserved data usable but not retail-proven:

- `LootConditionType.QuestObjectiveActive` maps condition `8` to
  `player.QuestManager.IsActiveObjectiveId(condition)`.
- `LootItemType.VirtualItem` delivery calls
  `QuestManager.ObjectiveUpdate(QuestObjectiveType.VirtualCollect, StaticId,
  Amount)`.
- `GlobalLootManager` imports mapped `entity_loot` groups and skips older direct
  creature-loot rows only when a DataMapping flat group already covers that
  creature.

This proves the safe objective-gated delivery path. It does not prove retail
cadence, repeated drop attempts, probability semantics, source/corpse selection,
or exact objective integration for the branch's representative rows.

Regression coverage (2026-05-28): `ItemContainerLootTests` now pins the
preserved quest-virtual-loot group boundary by requiring condition `8` /
`QuestObjectiveActive` to call `IsActiveObjectiveId` before dropping the mapped
virtual item. `LootInstanceDeliveryTests` pins delivery of
`LootItemType.VirtualItem` as a `QuestObjectiveType.VirtualCollect` objective
update plus the mapped loot grant, without adding loot-window cadence,
probability, or source-selection behavior.

## Required Capture

Use the blocker evidence harness for a representative sample before changing
behavior:

```powershell
.\Decomp\Analysis\Start-BlockerEvidenceHarness.ps1 `
  -QuestVirtualLootSmoke `
  -ClientDirectory "I:\WildStar" `
  -PromptForRootPassword
```

The preset creates `lws-055-quest-virtual-loot-targets.md`, preloads objectives
`13011`, `12605`, `6700`, `6313`, and `21278`, and configures negative cases for
inactive objectives, kills before objective activation, repeated eligible kills
after objective completion, and declined/abandoned loot windows. The bundle must
capture active quest/objective state, target entity id, kill/activation count,
loot windows or immediate grants, `ServerLoot*` traffic where available, virtual
item grant feedback, objective progress before/after, server logs, and repeated
eligible attempts for probability/cadence evidence.

Dry-run guard (2026-05-28): `Decomp/Analysis/test_blocker_evidence_harness_presets.py`
now runs `-QuestVirtualLootSmoke -CreateBundleOnly` and verifies the generated
manifest, default representative objective ids, target worksheet, helper files,
and negative-case scaffold. This does not prove trigger cadence, probability,
source/corpse selection, loot-window behavior, or objective integration.

Only captured rows should be promoted from mapped-only data to implemented
retail behavior or tests. Until then, the current seed remains the safe
preservation boundary.
