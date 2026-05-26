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
the official world database import. Use `-SkipRuntimeWorldSeedImport` to skip it
or `-RuntimeWorldSeedPath` to point setup at a different seed.

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
