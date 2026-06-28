# GiveLootTableToPlayer Source Recovery

Date: 2026-06-20

Scope: spell effect type `44` (`GiveLootTableToPlayer`). Runtime handling is
implemented to roll `dataBits00` as a native `loot_group.id`. This file records
the data-source audit before promoting any rows into runtime loot tables.

## Current Blocker

The client `Spell4Effects` table has `29` effect rows across `21` distinct
`dataBits00` loot-group ids, but the local runtime world database has no matching
`loot_group` or `loot_item` rows. The handler must remain fail-closed until those
groups are sourced from a real server/world loot table or a verified capture.

Do not promote rows from namespace collisions. These ids also appear in client
and Jabbithole item, creature, achievement, store, and drop aggregate contexts.
Those matches do not prove spell loot contents.

## Effect Rows

| Spell4 | Description | LootGroupId | RollCount/DataBits03 |
| --- | --- | ---: | ---: |
| 3395 | `[TEST] Mickey's Give loot to player - Tier 1` | 7126 | 180 |
| 3649 | `Converts Red Mineral to Red Ore - Tier 1` | 2097 | 0 |
| 3650 | `Converts Yellow Mineral to Yellow Ore - Tier 1` | 2098 | 0 |
| 3651 | `Converts Green Mineral to Green Ore - Tier 1` | 2099 | 0 |
| 6080 | `Loot Spell - Tier 1` | 6409 | 0 |
| 6089 | `Loot Spell - Tier 2` | 6414 | 0 |
| 41065 | `Give Can to Player - PHC - Tier 1` | 16890 | 0 |
| 43364 | `Dreadmoor - 2 - Q8452 - Random Dreg Testing - Extracting Blood Sample - DY - Tier 1` | 17596 | 0 |
| 57061 | `Generic Quest Spell - Activating - Activate - Tier 1` | 19144 | 0 |
| 60332 | `Activate Exanite Sliver - World event - Grimvault T2 - RVN - Tier 1` | 19868 | 0 |
| 71899 | `Elder Gemstone - Mystery Box Item - SWC - Tier 1` | 26361 | 0 |
| 76978 | `Collect Soulrot Waste - W3009 - Side Mission 4 - JBN - Tier 1` | 39775 | 0 |
| 77268 | `[WF14] I49818 Give Batteries - MXP - Tier 1` | 46556 | 0 |
| 77733 | `[WF14] I50380 - Give Remote Control - MXP - Tier 1` | 46556 | 0 |
| 77735 | `[WF14] I50381 - Give Cell Phone Plan - MXP - Tier 1` | 46556 | 0 |
| 77736 | `[WF14] I50382 - Give Component D - MXP - Tier 1` | 46556 | 0 |
| 77737 | `[WF14] I50383 - Give Component E - MXP - Tier 1` | 46556 | 0 |
| 77739 | `[WF14] I50384 - Give Component F - MXP - Tier 1` | 46556 | 0 |
| 77740 | `[WF14] I50385 - Give Component G - MXP - Tier 1` | 46556 | 0 |
| 77741 | `[WF14] I50386 - Give Component H - MXP - Tier 1` | 46556 | 0 |
| 77742 | `[WF14] I50387 - Give Component I - MXP - Tier 1` | 46556 | 0 |
| 82833 | `Login - Toy - Ambush Disguise - Tier 1` | 50945 | 0 |
| 82834 | `Login - Toy - Chompy Disguise Kit - Tier 1` | 50950 | 0 |
| 82907 | `Login: Item Empowerment - Tier 1` | 51062 | 0 |
| 82999 | `Login - Toy - Sparkly Glitterboots - NH - Tier 1` | 50951 | 0 |
| 83012 | `Login Open - Reward Challenge - Tier 1` | 51076 | 0 |
| 83028 | `Login: Item Empowerment 2 - Tier 1` | 51147 | 0 |
| 83029 | `Login: Item Empowerment 3 - Tier 1` | 51148 | 0 |
| 88381 | `[DNT] Tutorial - Primal Essence Gain - Tier 1` | 57435 | 0 |

Distinct blocked group ids:

`2097, 2098, 2099, 6409, 6414, 7126, 16890, 17596, 19144, 19868, 26361, 39775, 46556, 50945, 50950, 50951, 51062, 51076, 51147, 51148, 57435`

## Sources Checked

| Source | Result |
| --- | --- |
| `nexus_forever_world.loot_group` / `loot_item` | `0` rows for the `21` ids. |
| `wildstar_client.lootpinatainfo` / `lootspell` | `0` rows for the `21` ids. |
| `jabbithole.item_containers` by effect ids | `0` rows. |
| `jabbithole.item_containers` by known source items (`5870`, `5871`, `44346`, `49818`, `50380`-`50387`, `83624`, `83626`, `83627`, `83628`, `83663`, `83695`, `83697`) | `0` rows. |
| Local `SQL_Loot/*.sql` | No exact id hits for the `21` ids. |
| Official `I:\GIT\NexusForever.WorldDatabase` | No `loot_group`/`loot_item` source definitions. |
| `LaughingWS/NexusForever.WorldDatabase.New-Zones-and-more` at `c2d23295bdb8ab54cb249e87c44d3d3c155ad58e` | No spell loot-group definitions. Exact hits were unrelated: `QuestObjective 7126` in `Loot/CreatureQuestLoot.sql`, and store account-item ids `2097`-`2099` in `Store/StoreCatalog.sql`. |
| `Various SQL`, split `jabbithole_mysql`, split `wildstar_client_mysql`, combined dumps | Hits are namespace collisions in item, creature, achievement, store, and drop/aggregate tables; none prove effect-44 loot contents. |

## Online Source Search - 2026-06-20

Public source search did not find a promoteable spell loot table.

Primary sources checked:

- Official world database: <https://github.com/NexusForever/NexusForever.WorldDatabase>
- Open world-database pull requests: <https://github.com/NexusForever/NexusForever.WorldDatabase/pulls>
- `kirmmin` fork/branches: <https://github.com/kirmmin/NexusForever.WorldDatabase>
- `LaughingWS` world database branch: <https://github.com/LaughingWS/NexusForever.WorldDatabase.New-Zones-and-more>
- `DigitalHytop/WildStar-NF`: <https://github.com/DigitalHytop/WildStar-NF>

Direct mirrors were cloned under ignored `artifacts/source-recovery` and searched
across all heads/tags for the official world database and its public forks:
`NexusForever`, `nerddotdad`, `Musaji`, `Nikismyname`, `abarthen`, `kirmmin`,
`OBWANDO`, `Rynharok`, `Touyote`, `midnightnoon69`, `TheGhostGroup`, `Sadral`,
and `RichardRanft`.

Result:

- The only real public `loot_group` / `loot_item` SQL in those refs was
  `kirmmin/NexusForever.WorldDatabase` branch `quest_items`, commit `89e96b3`
  (`[Loot/Quest] Quest Item Loot Rewards`), also exposed as
  <https://github.com/NexusForever/NexusForever.WorldDatabase/pull/1>.
- That source is quest-objective virtual-item loot. It creates
  `loot_group.id = @GUID+n` rows with `conditionType = 8` and
  `condition = QuestObjective`, plus matching `loot_item` and `entity_loot`
  rows. It does not define native low-id spell loot groups matching
  `Spell4Effects.dataBits00`.
- No checked world-database ref contains direct `loot_group.id` definitions for
  the `21` effect-44 ids:
  `2097, 2098, 2099, 6409, 6414, 7126, 16890, 17596, 19144, 19868, 26361,
  39775, 46556, 50945, 50950, 50951, 51062, 51076, 51147, 51148, 57435`.
- Low-id hits such as `7126`, `16890`, and `19868` in official/fork zone SQL
  were `entity_spline.SplineId` values. Other hits were entity ids, coordinates,
  quest objective ids, account/store ids, or item ids.
- `DigitalHytop/WildStar-NF` only provided addon/API documentation for
  `GiveLootTableToPlayer`; it did not contain world loot SQL or reward-table
  data.

The public `quest_items` branch is useful for quest-objective loot work, but it
does not unblock `GiveLootTableToPlayer`. Importing it into effect-44 groups
would conflate quest objective conditions with spell loot-group ids.

## Rejected Collision Examples

- `2097`-`2099` appear as account items, achievements, challenge tiers, and
  store catalog rows, while the effect rows are mineral conversion loot groups.
- `46556` appears as client creature/item data and as the shared Winterfest
  component spell loot group, but no source says which item(s) group `46556`
  should award.
- `50945`, `50950`, `50951`, `51062`, `51076`, `51147`, and `51148` appear as
  ordinary item/creature/drop ids, while the source spells are login toy or item
  empowerment effects.

## Safe Promotion Criteria

Promote only when one of these is available:

1. A server/world SQL source with explicit `loot_group.id` values matching the
   `dataBits00` ids and child `loot_item` rows.
2. A verified retail/local capture showing the exact item, virtual item, account
   item, account currency, spell, count, and probability behavior for a source
   spell or source item.
3. A reviewed data-map source whose columns explicitly mean spell loot group id,
   contained reward id, reward type, count, and weight.

When that exists, add a runtime-owned seed (prefer a dedicated
`spell_effect_loot_seed.sql` or a clearly marked section in
`runtime_world_seed.sql`) that:

- Inserts `loot_group.id = Spell4Effects.dataBits00`.
- Inserts matching `loot_item` rows using the correct `LootItemType`.
- Preserves probabilities/counts from the source instead of normalizing guessed
  weights.
- Adds verification that all effect-44 `dataBits00` ids are either present in
  `nexus_forever_world.loot_group` or explicitly documented as blocked.

Until then, `GiveLootTableToPlayer` should stay implemented but data-blocked.
