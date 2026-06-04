# DataMapping SQL Imports

These SQL scripts import safe, reviewed mapping data into the real
`nexus_forever_world` runtime tables.

The staging/source databases are development-only. World/Auth runtime code must
not query `wildstar_client`, `jabbithole`, or `nf_map_*`; it should consume only
the runtime-owned tables populated by these imports.

Fresh machines should use the checked-in runtime seed SQL. It contains the
already-promoted runtime rows and does not require running the mapper or loading
`nf_map_*` staging:

```powershell
Get-Content -Raw Tools\DataMapping\sql\runtime_world_seed.sql |
  & "C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe" `
  --host=127.0.0.1 --user=bankai --password=bankai nexus_forever_world
```

`Tools\Setup\Initialize-NexusForever.ps1` imports this seed automatically after
the official world database import. It then imports
`laughingws_map_entrance_seed.sql`, `laughingws_city_content_seed.sql`,
`laughingws_quest_instance_wip_seed.sql`,
`laughingws_small_world_wip_seed.sql`,
`laughingws_instance_entity_wip_seed.sql`,
`laughingws_live_event_wip_seed.sql`,
`laughingws_housing_skyplot_wip_seed.sql`,
`laughingws_store_catalog_seed.sql`, and `laughingws_quest_loot_seed.sql` when
present. Use
`-SkipRuntimeWorldSeedImport` to skip these runtime seed overlays, or
`-RuntimeWorldSeedPath` to point setup at a different primary seed.

Auth-database seed/cleanup SQL is intentionally kept outside this world-data
folder. `Tools\Setup\sql\runtime_auth_seed.sql` is imported into
`nexus_forever_auth` by setup after migrations and local account creation.

`laughingws_map_entrance_seed.sql` is generated from LaughingWS map SQL by
keeping only `map_entrance` rows missing from the official world database:

```powershell
python Tools\DataMapping\extract_laughingws_map_entrances.py `
  --laughingws-path "I:\GIT\NexusForever.WorldDatabase.New-Zones-and-more" `
  --official-worlddb-path "I:\GIT\NexusForever.WorldDatabase"
```

`laughingws_city_content_seed.sql` is generated from a curated subset of placed
LaughingWS city/museum/quest-terminal entity rows: Illium Museum relic/projector
rows, the Museum exit pad, Illium/Thayd Halon Ring outbound pads, Halon Ring
return ship pads, WIP/GUESSED Illium Ringo Hax placement, and the Skull's Eye
escape pod terminal for quest `3770`. It also applies `8` coordinate-keyed
WIP/GUESSED `questChecklistIdx` updates to existing official Thayd/Illium
housing intro props, with WIP/GUESSED fallback inserts for older local imports
that do not yet contain those official prop rows. It skips zero-coordinate
placeholders and the rest of the unfinished Halon Ring / broad Thayd/Illium
zone dumps, and marks the Skull's Eye, Ringo Hax, and Thayd/Illium housing
checklist placements WIP/GUESSED pending manual quest/client smoke:

```powershell
python Tools\DataMapping\extract_laughingws_city_content.py `
  --laughingws-path "I:\GIT\NexusForever.WorldDatabase.New-Zones-and-more" `
  --official-worlddb-path "I:\GIT\NexusForever.WorldDatabase"
```

`laughingws_quest_instance_wip_seed.sql` is generated from the branch's small
Dust Stalker quest-instance SQL. It keeps Boss Xagg, the bridge controls, and
the source-only exit panel with deterministic entity IDs, strips the source
world-replacement delete and `@GUID` allocation, replaces source Boss Xagg
health `1` with DataMapping-backed stats, and marks the exit panel WIP/GUESSED:

```powershell
python Tools\DataMapping\extract_laughingws_quest_instances.py `
  --laughingws-path "I:\GIT\NexusForever.WorldDatabase.New-Zones-and-more"
```

`laughingws_small_world_wip_seed.sql` is generated from small reviewed
LaughingWS world overlay files. It keeps `16` WIP/GUESSED placed `entity` rows
and `59` `entity_stats` rows for Tactical Uplink, Captain Darkstone, Commander
Durek, Cortex Prime Whitewater, Corrigan Doon, Star-Comm Basin The Caretaker,
source-only Arcterra Caretaker/Coldblood portal placements, source-only Palaver
Point Ish'amel, and the reviewed Northern Wilds Deadeye Brightland, Scientist
Lusk, Elder Bartol, and Q3673 Signal Flare placements. The extractor strips
source world deletes and `@GUID` allocation, reconciles DataMapping-backed
creature/area/display/outfit mismatches to DataMapping values, labels
source-only rows explicitly, and applies `140` coordinate-keyed WIP/GUESSED
`questChecklistIdx` updates to official rows: `2` Everstar Grove Exo-Lab 71
Eldan Teleporter rows, `33` Wilderrun Kel Ulgar Weapon Rack / Tattered Banner /
Intact Data Cache / Cassian Commonwealth Emblem rows plus `67` Auroria
objective rows for Black Hoods Alchemy Supplies, Cubig
Security Zapper Console, Seismic Thumper, Hycrest infected/plague-victim
clusters, Freebot Drills, Navigation Control Panels, Bingberry Stills, and
GL-04 Auto Turrets, plus `24` Crimson Isle objective rows for Megatech
Terminals, Exile Anti-Air Cannons, Dreg Tents, Dominion Demolitions Experts,
Power Regulators, and Tower Controls, plus `14` Levian Bay Signal Flare / Drop
Pod Landing Beacon rows for Lighting the Way and Lost in the Fog. When an older
local import lacks those official Everstar Grove, Auroria, Crimson Isle, or
Levian Bay rows, guarded WIP/GUESSED fallback inserts add `107` deterministic
`entity` rows and up to `219` fallback `entity_stats` rows in the small-world
owned range.
Dominion/Exile Arkship tutorial rows stay audit-only/rejected for current
build-16042 runtime seeds, branch Wilderrun Dorian rows stay blocked due source
creature-id identity mismatches, Northern Wilds branch spline-mode residuals
stay blocked pending movement proof, branch Supply Officer Windward vendor rows
are rejected in favor of the existing DataMapping vendor import, Everstar Grove,
Levian Bay, Auroria, and Crimson Isle branch-only residual rows stay blocked
pending row-level proof, and broad zone replacements stay blocked:

```powershell
python Tools\DataMapping\extract_laughingws_small_world_overlays.py `
  --laughingws-path "I:\GIT\NexusForever.WorldDatabase.New-Zones-and-more"
```

`laughingws_instance_entity_wip_seed.sql` is generated from selected
LaughingWS instance SQL where current WIP scripts or map bindings already exist.
It keeps `75` WIP/GUESSED instance-local `entity` rows, `67` `entity_event`
rows, `35` `entity_script` rows, and `80` `entity_stats` rows for Coldblood
Citadel, Protostar SuperMall, Space Madness, Gauntlet, Fragment Zero,
Infestation, Outpost M-13, Evil from the Ether drive-spark phase anchors,
Protogames Academy, Ruins of Kel Voreth,
Stormtalon's Lair, Skullcano, Sanctuary of the Swordmaiden, and Genetic
Archives, plus the map-bound Ultimate Protogames Bev-O-Rage row, the
source-only Red Moon Terror Laveka row, Shade's Eve Etty/fountain anchors, and
`17` placed Datascape boss/objective anchors for the current WIP Datascape chain,
while stripping source world deletes and `@GUID` allocation. It also applies
`14` coordinate-keyed WIP/GUESSED updates against the official Evil from the
Ether import: `7` branch area reconciliations for Captain Weir, the gather ring,
medbay controls, and spare-parts crates, plus `7` crew-log
`questChecklistIdx` reconciliations for the branch-derived
`CrewLogEntityScript`. Full encounter choreography, trigger timing, NPC
interaction, combat behavior, trigger cleanup, Datascape placeholder
rows/trash carpets/elemental mechanics, and cinematics remain blocked:

```powershell
python Tools\DataMapping\extract_laughingws_instance_entities.py `
  --laughingws-path "I:\GIT\NexusForever.WorldDatabase.New-Zones-and-more"
```

`laughingws_live_event_wip_seed.sql` is generated from reviewed event SQL by
keeping additive `entity`, `entity_stats`, `entity_vendor`,
`entity_vendor_category`, and `entity_vendor_item` rows, replacing source
`@GUID` allocation with deterministic high entity IDs, and filtering official
duplicates and obvious coordinate placeholders. The generated SQL comments mark
the data as WIP/guessed because event lifecycle, spawn timing, cleanup, and
retail placement proof are not complete:

```powershell
python Tools\DataMapping\extract_laughingws_live_events.py `
  --laughingws-path "I:\GIT\NexusForever.WorldDatabase.New-Zones-and-more" `
  --official-worlddb-path "I:\GIT\NexusForever.WorldDatabase"
```

`laughingws_housing_skyplot_wip_seed.sql` is generated from the branch Skyplot
housing SQL by preserving the return pad, two housing vendors, and their
catalogue rows while replacing source `@GUID` allocation with deterministic high
entity IDs. The extractor strips the source world-replacement delete
(`DELETE FROM entity WHERE world = @WORLD`) and marks the generated SQL as
WIP/guessed because the Skyplot placement/catalogue has not been retail-smoked:

```powershell
python Tools\DataMapping\extract_laughingws_housing_skyplot.py `
  --laughingws-path "I:\GIT\NexusForever.WorldDatabase.New-Zones-and-more" `
  --official-worlddb-path "I:\GIT\NexusForever.WorldDatabase"
```

`laughingws_store_catalog_seed.sql` is generated from
`LaughingWS/NexusForever.WorldDatabase.New-Zones-and-more` by stripping the
store dump's DDL/deletes, merging the reviewed store-event overlays from
Valentine's Day, Shades Eve, and Winterfest, auditing the Dungeon Chase hidden
SMC placeholder, and keeping only idempotent upserts into the current store
tables. Dungeon Chase's legacy store aliases and one malformed `Field_7` column
list are canonicalised, but its type `0` item `86919` placeholder is skipped
with a generated WIP/GUESSED note because it exceeds the current 15-bit
storefront wire/schema proof. Regenerate it only after re-auditing the source
branch:

```powershell
python Tools\DataMapping\extract_laughingws_store_catalog.py `
  --source-sql "I:\GIT\NexusForever.WorldDatabase.New-Zones-and-more\Store\StoreCatalog.sql" `
  --source-sql "I:\GIT\NexusForever.WorldDatabase.New-Zones-and-more\Store\Store Events\Valentine's Day items.sql" `
  --source-sql "I:\GIT\NexusForever.WorldDatabase.New-Zones-and-more\Events\Dungeon Chase.sql" `
  --source-sql "I:\GIT\NexusForever.WorldDatabase.New-Zones-and-more\Events\Shades Eve.sql" `
  --source-sql "I:\GIT\NexusForever.WorldDatabase.New-Zones-and-more\Events\Winterfest.sql"
```

`laughingws_quest_loot_seed.sql` is generated from
`Loot\CreatureQuestLoot.sql` by stripping DDL/foreign-key toggles, shifting source
`loot_group` ids into a DataMapping-owned high range, and preserving the branch's
virtual-item quest objective loot:

```powershell
python Tools\DataMapping\extract_laughingws_quest_loot.py `
  --source-sql "I:\GIT\NexusForever.WorldDatabase.New-Zones-and-more\Loot\CreatureQuestLoot.sql"
```

Authoring workflow from the repository root, only when regenerating reviewed
mapping data:

```powershell
python Tools\DataMapping\map_wildstar_data.py --include-spline-candidates
python Tools\DataMapping\load_mapping_staging_tables.py --apply
Get-Content -Raw Tools\DataMapping\sql\apply_safe_world_imports_from_staging.sql |
  & "C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe" `
  --host=127.0.0.1 --user=bankai --password=bankai nexus_forever_world
```

After an authoring import is verified, refresh the checked-in runtime seed:

```powershell
python Tools\DataMapping\export_runtime_world_seed.py
```

The exporter intentionally excludes deterministic LaughingWS overlay entity ID
ranges. Keep those rows in the separate `laughingws_*_seed.sql` files so a
primary DataMapping seed refresh cannot absorb or delete overlay-owned data
when setup imports unchanged overlay seeds.

Creature-backed imports only use rows whose creature bridge is one of:

- `unique_name`
- `scored_name`
- `reviewed`

It currently covers:

- `entity_vendor`
- `entity_vendor_category`
- `entity_vendor_item`
- `entity`
- `entity_stats`
- `creature_loot`
- `loot_group`
- `entity_loot`
- `item_loot`
- `loot_item`
- `creature_info_property`
- `creature_info_stat`

It also applies a small set of tracked world-data cleanups after import where
the reviewed source still needs a runtime-owned correction. Current example:
Rider's Reef tutorial combat rows in world `3460`, where stray `73665` Beacon
Arrow entities are deleted and tutorial turrets `73494`/`74862` are normalized
to runtime combat rows by coercing them to `type = 0` (`NonPlayer`) with combat
factions `1441`/`1442`.

`verify_safe_world_imports.sql` also prints explicit mismatch metrics for the
`23` LaughingWS map-entrance overlay rows, the curated city-content seed (`17`
`entity`, optional `4` Illium housing fallback rows, optional `4` Thayd housing
fallback rows for older local imports, and `8` Thayd/Illium housing intro
`questChecklistIdx` updates), the WIP quest-instance seed (`3` `entity`, `4`
`entity_stats`), the
WIP small-world seed (`16` `entity`, `59` `entity_stats`, `7` Northern Wilds
WIP quest/objective placement rows, `3` Northern Wilds Q3673 signal flare rows,
`2` official Everstar Grove `questChecklistIdx` updates, `14` official Levian
Bay `questChecklistIdx` updates, `33` official Wilderrun `questChecklistIdx`
updates, `67` official Auroria `questChecklistIdx` updates, and `24` official
Crimson Isle `questChecklistIdx` updates with optional `107` Everstar Grove/
Levian Bay/Auroria/Crimson Isle fallback `entity` rows and up to `219` fallback
`entity_stats` rows on older local imports), the WIP instance seed
(`75` `entity`, `67` `entity_event`, `35` `entity_script`, `80`
`entity_stats`), the WIP Skyplot housing seed (`3` `entity`, `12`
`entity_stats`, `2` `entity_vendor`, `72` `entity_vendor_category`, `322`
`entity_vendor_item`), the WIP live-event seed (`198` `entity`, `68`
`entity_stats`, `8` `entity_vendor`, `24` `entity_vendor_category`, `142`
`entity_vendor_item`), the LaughingWS quest-loot overlay (`1,174` `loot_group`,
`5,302` `entity_loot`, `1,174` `loot_item`), the checked Valentine's
Day/Shades Eve/Winterfest store offer-group ids, the Shade's Eve / Red Moon
Terror anchor rows, the WIP Evil from the Ether drive-spark phase anchors, the official Slaughterdome/Cryo-Plex arena forcefields used
by the WIP PvP scripts, and the official Evil from the Ether import's `12`
script-hook rows for Katja, Ravenous Refugees/Reaper, and Security Chief
Kondovich plus the `7` coordinate-keyed branch area updates and `7` crew-log
checklist updates from the WIP instance seed. Those official script rows are
not duplicated by `laughingws_instance_entity_wip_seed.sql`. The audit now
counts only active, non-commented SQL inserts and marks Evil from the Ether and
Datascape remaining active rows as blocked instance-entity residuals rather
than generic additive overlay candidates. It also marks the broad Dreadmoor,
Halon Ring, and Murkmire new-zone rows as blocked rather than additive because
they are hand-authored placeholder/TODO rows without enough current script,
placement, or client-smoke proof. Illium's remaining branch-only vendor stubs
are rejected as orphan vendor rows without catalog data, and Wilderrun's
remaining Dorian rows are blocked because the branch creature ids do not match
current DataMapping identity. Northern Wilds residual spline-mode rows remain
blocked pending movement proof, while the branch Supply Officer Windward vendor
rows are rejected because the same placed NPC already has DataMapping-backed
vendor rows. Everstar Grove residual branch-only rows remain blocked after
promoting the useful official-row Exo-Lab 71 quest checklist indices, Auroria
residual branch-only rows remain blocked after promoting the useful official-row
quest checklist indices, and Crimson Isle residual branch-only rows remain
blocked after promoting its useful official-row quest checklist indices.

The default mode is additive/idempotent: vendor rows use `INSERT IGNORE`,
optional mapped entity-spawn imports use `INSERT IGNORE` with deterministic
DataMapping-owned IDs, creature loot upserts by `(creatureId, itemId)`, mapped
creature loot is also materialised into flat `loot_group`/`entity_loot`/`loot_item` rows,
`item_container_map.csv` is materialised into weighted `item_loot` groups for
loot bags, and creature-info overrides insert missing rows after skipping
conflicting template values. Creature `BaseHealth` imports use the mapper's
`template_base_health` column only. Source health ranges are preserved for
review, but are not promoted as global `creature_info_property` rows because a
range such as `353-844` belongs to a level band while a runtime creature property
applies to every level of that template.

Mapped entity spawns are opt-in because they can add many runtime rows. To
import one reviewed world from `nf_map_world_entity_candidate`, set these
variables before sourcing the script:

```sql
SET @nf_safe_import_entity_spawns = 1;
SET @nf_safe_import_entity_spawn_world = 51;
SOURCE Tools/DataMapping/sql/apply_safe_world_imports_from_staging.sql;
```

Set `@nf_safe_import_entity_spawn_area` to narrow a large world import to one
world area; leave it unset or `0` to import the selected world.

Imported entity IDs default to `1000000000 + source_coordinate_id`, and the
import skips a candidate when the same creature already has a runtime entity
within `@nf_safe_import_entity_spawn_match_radius` units in the same world/area
(default `10.0`). The matching `nf_map_world_entity_stats_candidate` rows are
inserted into `entity_stats` for imported entities; generated health stats are
also limited to fixed/single-sided health observations.

Ambiguous creature bridges remain excluded by default. For targeted zone repair
work, `@nf_safe_import_entity_spawn_include_ambiguous_exact_name = 1` also
allows `ambiguous_name` rows when the selected client creature name exactly
matches the Jabbithole source name; use this only after reviewing the affected
world/area slice.

Some Jabbithole coordinate rows have useful `x/y/z` values but a blank source
`worldid`, so they do not appear in `nf_map_world_entity_candidate`. For a
targeted source-zone repair, set `@nf_safe_import_jabbithole_creature_coordinates = 1`
and `@nf_safe_import_jabbithole_creature_coordinate_zone` together with the
target world. The fallback imports only resolved creature bridges from that
Jabbithole zone, infers area from explicit source `worldzoneid` or nearby staged
world candidates within `@nf_safe_import_jabbithole_creature_coordinate_area_match_radius`
(default `75.0`), and still applies the normal duplicate radius check.

Northern Wilds (`world = 426`) source coordinates are currently treated as unsafe
for bulk spawn promotion. The raw Jabbithole/source observations overpopulate
small map grids, for example Granok Mercenary and Yeti Snowstalker rows, so the
apply script blocks new DataMapping-owned Northern Wilds entity imports by
default, removes any older DataMapping-owned rows, and keeps only legacy/manual
rows plus script-owned quest spawns. Set
`@nf_safe_import_allow_unsafe_northern_wilds_spawns = 1` only during focused
review work, not for normal local world imports.

The reviewed Northern Wilds `Yeti Snowstalker` bridge is tracked in
`Tools/DataMapping/creature_bridge_overrides.csv`, not as a SQL special-case.
Regenerate and reload staging before applying the runtime import when tracked or
local review overrides change.

For a refresh of owned mapped rows, run the SQL script from an interactive MySQL
session and set:

```sql
SET @nf_safe_import_replace_existing = 1;
SOURCE Tools/DataMapping/sql/apply_safe_world_imports_from_staging.sql;
```

Refresh mode removes vendor rows for mapped vendor creatures, truncates the
owned `creature_loot` table, removes owned DataMapping creature/item runtime
loot groups, removes owned DataMapping entity rows for the selected spawn-import
world, and removes mapped creature-info property/stat IDs before reimporting.
Use it when the staging maps were regenerated and you want the runtime tables to
mirror the latest safe mapping output.

By default generated runtime loot rows use `minCount = maxCount = 1`, matching
the first live runtime import. To experiment with aggregate-derived stack sizes,
set `@nf_safe_import_creature_loot_counts_from_aggregates = 1` before sourcing
the apply script.

Generated item-container loot groups use `minDrop = maxDrop = 1` and item
probabilities weighted by `nf_map_item_container.drop_times`.

Verification:

```powershell
Get-Content -Raw Tools\DataMapping\sql\verify_safe_world_imports.sql |
  & "C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe" `
  --host=127.0.0.1 --user=bankai --password=bankai nexus_forever_world
```

## Verified Run - 2026-05-25

Smoke mapping from the repository root:

```powershell
python Tools\DataMapping\map_wildstar_data.py --limit-creatures 200 --limit-relation-rows 500 --limit-spawns 500 --output-dir Tools\DataMapping\output_test
```

Result: completed successfully and wrote `Tools\DataMapping\output_test`
(`run_manifest.json` generated at `2026-05-25 09:12:48`). The smoke generated
`500` spawn/entity candidate rows, `913` entity-stat candidate rows, and `200`
creature bridge rows with match counts `reviewed=22`, `unique_name=106`,
`scored_name=18`, `ambiguous_name=39`, and `unmatched=15`.

Full staging refresh:

```powershell
python Tools\DataMapping\load_mapping_staging_tables.py --apply
```

Result: loaded `95/95` `nf_map_*` tables from `Tools\DataMapping\output` into
`nexus_forever_world` with `replace_existing=true`, including `732694`
`nf_map_world_entity_candidate` rows and `2442855`
`nf_map_world_entity_stats_candidate` rows. `local_infile` was restored after
the load.

Safe import and verification:

```powershell
Get-Content -Raw Tools\DataMapping\sql\apply_safe_world_imports_from_staging.sql |
  & "C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe" `
  --host=127.0.0.1 --user=bankai --password=bankai nexus_forever_world

Get-Content -Raw Tools\DataMapping\sql\verify_safe_world_imports.sql |
  & "C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe" `
  --host=127.0.0.1 --user=bankai --password=bankai nexus_forever_world
```

Verification reported `95` `nf_map_*` staging tables, `17520` safe vendor rows,
`460121` safe loot rows, `3795` mapped creature runtime loot groups, `17612`
safe creature rows, and `23865` safe item-container rows. Rider's Reef cleanup
checks reported `riders_reef_beacon_rows=0`,
`riders_reef_wrong_faction_turrets=0`, and
`riders_reef_wrong_type_turrets=0`; the remaining world `3460` turret rows were
two `73494` entities with `type=0`, `faction1=1441`, and `faction2=1441`.

Northern Wilds follow-up on `2026-05-25`: sourced the same script with
`@nf_safe_import_entity_spawns = 1` for world `426`, then for world `51` areas
`23`, `973`, and `976`, and finally reran world `426` with
`@nf_safe_import_entity_spawn_include_ambiguous_exact_name = 1`,
`@nf_safe_import_jabbithole_creature_coordinates = 1`, and
`@nf_safe_import_jabbithole_creature_coordinate_zone = 1` after reviewing the
affected exact-name/direct-coordinate slice. Verification reported `4375`
mapped entity rows, `15423` mapped entity stat rows, and Northern Wilds runtime
coverage of `161` enabled Jabbithole zone-1 creatures in world `426`; the
remaining `3` zone-1 creatures are unresolved Creature2 bridges.

Northern Wilds rollback on `2026-05-25`: live server log
`NexusForever.WorldServer_20260525_48236.log` showed world `426` loading `3976`
cached spawns across only `6` grids, with raw coordinate imports creating
overdense packs such as Granok Mercenary (`54` rows) and Yeti Snowstalker
(`111` rows). The coordinate import is therefore not a safe retail spawn-group
mapping. The apply script now removes DataMapping-owned world `426` entity rows
and blocks new world `426` coordinate imports unless
`@nf_safe_import_allow_unsafe_northern_wilds_spawns = 1` is explicitly set for
focused review. Local verification after cleanup reported
`northern_wilds_unsafe_datamapping_spawns=0`,
`northern_wilds_legacy_manual_spawns=738`, Granok Mercenary `4`, and Yeti
Snowstalker `18`.
