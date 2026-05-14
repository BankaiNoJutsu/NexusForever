# DataMapping SQL Imports

These SQL scripts import the safe, reviewed mapping outputs from the `nf_map_*`
staging tables into the real `nexus_forever_world` runtime tables.

Recommended workflow from the repository root:

```powershell
python Tools\DataMapping\map_wildstar_data.py --include-spline-candidates
python Tools\DataMapping\load_mapping_staging_tables.py --apply
Get-Content -Raw Tools\DataMapping\sql\apply_safe_world_imports_from_staging.sql |
  & "C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe" `
  --host=127.0.0.1 --user=bankai --password=bankai nexus_forever_world
```

The apply script imports only rows whose creature bridge is one of:

- `unique_name`
- `scored_name`
- `reviewed`

It currently covers:

- `entity_vendor`
- `entity_vendor_category`
- `entity_vendor_item`
- `creature_loot`
- `creature_info_property`
- `creature_info_stat`

The default mode is additive/idempotent: vendor rows use `INSERT IGNORE`,
creature loot upserts by `(creatureId, itemId)`, and creature-info overrides
insert missing rows after skipping conflicting template values.

For a refresh of owned mapped rows, run the SQL script from an interactive MySQL
session and set:

```sql
SET @nf_safe_import_replace_existing = 1;
SOURCE Tools/DataMapping/sql/apply_safe_world_imports_from_staging.sql;
```

Refresh mode removes vendor rows for mapped vendor creatures, truncates the
owned `creature_loot` table, and removes mapped creature-info property/stat IDs
before reimporting. Use it when the staging maps were regenerated and you want
the runtime tables to mirror the latest safe mapping output.

Verification:

```powershell
Get-Content -Raw Tools\DataMapping\sql\verify_safe_world_imports.sql |
  & "C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe" `
  --host=127.0.0.1 --user=bankai --password=bankai nexus_forever_world
```
