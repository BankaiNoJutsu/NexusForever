-- Import safe mapped WildStar/Jabbithole data from nf_map_* staging tables.
--
-- Prerequisite:
--   python Tools\DataMapping\load_mapping_staging_tables.py --apply
--   This loads nf_map_* into the authoring database nexus_forever_mapping.
--
-- Run:
--   Get-Content -Raw Tools\DataMapping\sql\apply_safe_world_imports_from_staging.sql |
--     mysql --host=127.0.0.1 --user=bankai --password=bankai nexus_forever_world
--
-- Optional refresh mode for owned rows before import:
--   SET @nf_safe_import_replace_existing = 1;
--   SOURCE Tools/DataMapping/sql/apply_safe_world_imports_from_staging.sql;

USE `nexus_forever_world`;

SET @nf_safe_import_replace_existing := IFNULL(@nf_safe_import_replace_existing, 0);
SET @nf_loot_group_base := IFNULL(@nf_loot_group_base, 100000000000);
SET @nf_loot_group_max := @nf_loot_group_base + 4294967295;
SET @nf_item_loot_group_base := IFNULL(@nf_item_loot_group_base, 110000000000);
SET @nf_item_loot_group_max := @nf_item_loot_group_base + 4294967295;
SET @nf_safe_import_creature_loot_counts_from_aggregates := IFNULL(@nf_safe_import_creature_loot_counts_from_aggregates, 0);
SET @nf_safe_import_entity_spawns := IFNULL(@nf_safe_import_entity_spawns, 0);
SET @nf_safe_import_entity_spawn_world := IFNULL(@nf_safe_import_entity_spawn_world, 0);
SET @nf_safe_import_entity_spawn_area := IFNULL(@nf_safe_import_entity_spawn_area, 0);
SET @nf_safe_import_entity_spawn_include_ambiguous_exact_name := IFNULL(@nf_safe_import_entity_spawn_include_ambiguous_exact_name, 0);
SET @nf_safe_import_entity_spawn_match_radius := IFNULL(@nf_safe_import_entity_spawn_match_radius, 10.0);
SET @nf_safe_import_jabbithole_creature_coordinates := IFNULL(@nf_safe_import_jabbithole_creature_coordinates, 0);
SET @nf_safe_import_jabbithole_creature_coordinate_zone := IFNULL(@nf_safe_import_jabbithole_creature_coordinate_zone, 0);
SET @nf_safe_import_jabbithole_creature_coordinate_area_match_radius := IFNULL(@nf_safe_import_jabbithole_creature_coordinate_area_match_radius, 75.0);
SET @nf_safe_import_allow_unsafe_northern_wilds_spawns := IFNULL(@nf_safe_import_allow_unsafe_northern_wilds_spawns, 0);
SET @nf_entity_id_base := IFNULL(@nf_entity_id_base, 1000000000);
SET @nf_entity_id_max := @nf_entity_id_base + 999999999;

DROP TEMPORARY TABLE IF EXISTS tmp_nf_required_runtime_tables;
CREATE TEMPORARY TABLE tmp_nf_required_runtime_tables (
  table_name VARCHAR(128) NOT NULL PRIMARY KEY
) ENGINE=Memory;

INSERT INTO tmp_nf_required_runtime_tables (table_name) VALUES
  ('creature_loot'),
  ('loot_group'),
  ('entity_loot'),
  ('item_loot'),
  ('loot_item'),
  ('item_salvage'),
  ('creature_info_property'),
  ('creature_info_stat'),
  ('entity'),
  ('entity_stats'),
  ('entity_vendor'),
  ('entity_vendor_category'),
  ('entity_vendor_item');

SET @nf_missing_runtime_tables := (
  SELECT GROUP_CONCAT(r.table_name ORDER BY r.table_name SEPARATOR ', ')
  FROM tmp_nf_required_runtime_tables r
  LEFT JOIN information_schema.tables t
    ON t.table_schema = DATABASE()
   AND t.table_name = r.table_name
  WHERE t.table_name IS NULL
);
SET @nf_missing_runtime_tables_sql := IF(
  @nf_missing_runtime_tables IS NULL,
  'DO 0',
  CONCAT('SIGNAL SQLSTATE ''45000'' SET MESSAGE_TEXT = ', QUOTE(LEFT(CONCAT('Missing runtime table(s). Run EF migrations first: ', @nf_missing_runtime_tables), 128)))
);
PREPARE nfMissingRuntimeTablesStatement FROM @nf_missing_runtime_tables_sql;
EXECUTE nfMissingRuntimeTablesStatement;
DEALLOCATE PREPARE nfMissingRuntimeTablesStatement;

DROP TEMPORARY TABLE IF EXISTS tmp_nf_required_mapping_tables;
CREATE TEMPORARY TABLE tmp_nf_required_mapping_tables (
  table_name VARCHAR(128) NOT NULL PRIMARY KEY
) ENGINE=Memory;

INSERT INTO tmp_nf_required_mapping_tables (table_name) VALUES
  ('nf_map_world_entity_candidate'),
  ('nf_map_world_entity_stats_candidate'),
  ('nf_map_creature'),
  ('nf_map_vendor_item'),
  ('nf_map_creature_loot'),
  ('nf_map_item_container'),
  ('nf_map_item_salvage'),
  ('nf_map_client_source_salvage');

SET @nf_missing_mapping_tables := (
  SELECT GROUP_CONCAT(r.table_name ORDER BY r.table_name SEPARATOR ', ')
  FROM tmp_nf_required_mapping_tables r
  LEFT JOIN information_schema.tables t
    ON t.table_schema = 'nexus_forever_mapping'
   AND t.table_name = r.table_name
  WHERE t.table_name IS NULL
);
SET @nf_missing_mapping_tables_sql := IF(
  @nf_missing_mapping_tables IS NULL,
  'DO 0',
  CONCAT('SIGNAL SQLSTATE ''45000'' SET MESSAGE_TEXT = ', QUOTE(LEFT(CONCAT('Missing authoring staging table(s). Run load_mapping_staging_tables.py --apply: ', @nf_missing_mapping_tables), 128)))
);
PREPARE nfMissingMappingTablesStatement FROM @nf_missing_mapping_tables_sql;
EXECUTE nfMissingMappingTablesStatement;
DEALLOCATE PREPARE nfMissingMappingTablesStatement;

DROP TEMPORARY TABLE IF EXISTS tmp_nf_world_entity_coordinate_reject;
CREATE TEMPORARY TABLE tmp_nf_world_entity_coordinate_reject (
  source_coordinate_id BIGINT UNSIGNED NOT NULL PRIMARY KEY,
  jabbithole_creature_id BIGINT UNSIGNED NOT NULL,
  creature2_id BIGINT UNSIGNED NOT NULL,
  world SMALLINT UNSIGNED NOT NULL,
  area SMALLINT UNSIGNED NOT NULL,
  reason VARCHAR(255) NOT NULL
) ENGINE=Memory;

INSERT INTO tmp_nf_world_entity_coordinate_reject (
  source_coordinate_id, jabbithole_creature_id, creature2_id, world, area, reason
) VALUES
  (1556666, 1086, 54640, 51, 23, 'Dodger stale Jabbithole coordinate superseded by official/current coordinate 4460');

SET @nf_truncate_loot_sql := IF(
  @nf_safe_import_replace_existing = 1,
  'TRUNCATE TABLE creature_loot',
  'DO 0'
);
PREPARE nfTruncateLootStatement FROM @nf_truncate_loot_sql;
EXECUTE nfTruncateLootStatement;
DEALLOCATE PREPARE nfTruncateLootStatement;

START TRANSACTION;

DROP TEMPORARY TABLE IF EXISTS tmp_nf_world_entity_import;
CREATE TEMPORARY TABLE tmp_nf_world_entity_import ENGINE=InnoDB AS
SELECT
  CAST(@nf_entity_id_base + c.source_coordinate_id AS UNSIGNED) AS id,
  c.source_coordinate_id,
  CAST(IFNULL(c.`Type`, 0) AS UNSIGNED) AS `type`,
  CAST(c.Creature AS UNSIGNED) AS creature,
  CAST(c.World AS UNSIGNED) AS world,
  CAST(c.Area AS UNSIGNED) AS area,
  CAST(c.X AS DECIMAL(18,6)) AS x,
  CAST(c.Y AS DECIMAL(18,6)) AS y,
  CAST(c.Z AS DECIMAL(18,6)) AS z,
  CAST(IFNULL(c.RX, 0) AS DECIMAL(18,6)) AS rx,
  CAST(IFNULL(c.RY, 0) AS DECIMAL(18,6)) AS ry,
  CAST(IFNULL(c.RZ, 0) AS DECIMAL(18,6)) AS rz,
  CAST(IFNULL(c.DisplayInfo, 0) AS UNSIGNED) AS displayInfo,
  CAST(IFNULL(c.OutfitInfo, 0) AS UNSIGNED) AS outfitInfo,
  CAST(IFNULL(c.Faction1, 0) AS UNSIGNED) AS faction1,
  CAST(IFNULL(c.Faction2, 0) AS UNSIGNED) AS faction2,
  CAST(IFNULL(c.QuestChecklistIdx, 0) AS UNSIGNED) AS questChecklistIdx,
  CAST(IFNULL(c.ActivePropId, 0) AS UNSIGNED) AS activePropId,
  CAST(IFNULL(c.WorldSocketId, 0) AS UNSIGNED) AS worldSocketId,
  CAST(IFNULL(c.Mode, 0) AS UNSIGNED) AS mode
FROM nexus_forever_mapping.nf_map_world_entity_candidate c
WHERE @nf_safe_import_entity_spawns = 1
  AND (
    c.match_status IN ('unique_name', 'scored_name', 'reviewed')
    OR (
      @nf_safe_import_entity_spawn_include_ambiguous_exact_name = 1
      AND c.match_status = 'ambiguous_name'
      AND EXISTS (
        SELECT 1
        FROM nexus_forever_mapping.nf_map_creature m
        WHERE m.jabbithole_creature_id = c.jabbithole_creature_id
          AND m.creature2_id = c.Creature
          AND TRIM(LOWER(IFNULL(m.source_name, ''))) = TRIM(LOWER(IFNULL(m.client_name, '')))
      )
    )
  )
  AND @nf_safe_import_entity_spawn_world > 0
  AND c.World = @nf_safe_import_entity_spawn_world
  AND (@nf_safe_import_allow_unsafe_northern_wilds_spawns = 1 OR c.World <> 426)
  AND (@nf_safe_import_entity_spawn_area = 0 OR c.Area = @nf_safe_import_entity_spawn_area)
  AND IFNULL(c.source_coordinate_id, 0) > 0
  AND (@nf_entity_id_base + c.source_coordinate_id) BETWEEN @nf_entity_id_base AND @nf_entity_id_max
  AND IFNULL(c.Creature, 0) > 0
  AND IFNULL(c.World, 0) > 0
  AND IFNULL(c.Area, 0) > 0
  AND NOT EXISTS (
    SELECT 1
    FROM tmp_nf_world_entity_coordinate_reject rejected
    WHERE rejected.source_coordinate_id = c.source_coordinate_id
      AND rejected.jabbithole_creature_id = c.jabbithole_creature_id
      AND rejected.creature2_id = c.Creature
      AND rejected.world = c.World
      AND rejected.area = c.Area
  )
  AND c.X IS NOT NULL
  AND c.Y IS NOT NULL
  AND c.Z IS NOT NULL
  AND NOT EXISTS (
    SELECT 1
    FROM entity existing
    WHERE existing.creature = c.Creature
      AND existing.world = c.World
      AND existing.area = c.Area
      AND (
        POW(existing.x - c.X, 2)
        + POW(existing.y - c.Y, 2)
        + POW(existing.z - c.Z, 2)
      ) <= POW(@nf_safe_import_entity_spawn_match_radius, 2)
  );

ALTER TABLE tmp_nf_world_entity_import
  ADD PRIMARY KEY (id),
  ADD KEY ix_tmp_nf_world_entity_import_source (source_coordinate_id),
  ADD KEY ix_tmp_nf_world_entity_import_creature (creature),
  ADD KEY ix_tmp_nf_world_entity_import_world_area (world, area);

SET @nfJabbitholeCoordinateImportSql := IF(
  @nf_safe_import_entity_spawns = 1
    AND @nf_safe_import_jabbithole_creature_coordinates = 1
    AND @nf_safe_import_entity_spawn_world > 0
    AND @nf_safe_import_jabbithole_creature_coordinate_zone > 0,
  'INSERT IGNORE INTO tmp_nf_world_entity_import (
      id, source_coordinate_id, type, creature, world, area, x, y, z, rx, ry, rz,
      displayInfo, outfitInfo, faction1, faction2, questChecklistIdx, activePropId,
      worldSocketId, mode
    )
    SELECT
      direct.id,
      direct.source_coordinate_id,
      direct.type,
      direct.creature,
      direct.world,
      direct.area,
      direct.x,
      direct.y,
      direct.z,
      direct.rx,
      direct.ry,
      direct.rz,
      direct.displayInfo,
      direct.outfitInfo,
      direct.faction1,
      direct.faction2,
      direct.questChecklistIdx,
      direct.activePropId,
      direct.worldSocketId,
      direct.mode
    FROM (
      SELECT
        CAST(@nf_entity_id_base + co.id AS UNSIGNED) AS id,
        co.id AS source_coordinate_id,
        CAST(IFNULL(m.entity_type, 0) AS UNSIGNED) AS type,
        CAST(m.creature2_id AS UNSIGNED) AS creature,
        CAST(@nf_safe_import_entity_spawn_world AS UNSIGNED) AS world,
        CAST(COALESCE(
          NULLIF(c.worldzoneid, 0),
          (
            SELECT cand.Area
            FROM nexus_forever_mapping.nf_map_world_entity_candidate cand
            WHERE cand.World = @nf_safe_import_entity_spawn_world
              AND cand.Area > 0
              AND cand.X BETWEEN co.x - @nf_safe_import_jabbithole_creature_coordinate_area_match_radius AND co.x + @nf_safe_import_jabbithole_creature_coordinate_area_match_radius
              AND cand.Y BETWEEN co.y - @nf_safe_import_jabbithole_creature_coordinate_area_match_radius AND co.y + @nf_safe_import_jabbithole_creature_coordinate_area_match_radius
              AND cand.Z BETWEEN co.z - @nf_safe_import_jabbithole_creature_coordinate_area_match_radius AND co.z + @nf_safe_import_jabbithole_creature_coordinate_area_match_radius
              AND cand.X IS NOT NULL
              AND cand.Y IS NOT NULL
              AND cand.Z IS NOT NULL
              AND (
                POW(cand.X - co.x, 2)
                + POW(cand.Y - co.y, 2)
                + POW(cand.Z - co.z, 2)
              ) <= POW(@nf_safe_import_jabbithole_creature_coordinate_area_match_radius, 2)
            ORDER BY POW(cand.X - co.x, 2) + POW(cand.Y - co.y, 2) + POW(cand.Z - co.z, 2)
            LIMIT 1
          ),
          NULLIF(@nf_safe_import_entity_spawn_area, 0)
        ) AS UNSIGNED) AS area,
        CAST(co.x AS DECIMAL(18,6)) AS x,
        CAST(co.y AS DECIMAL(18,6)) AS y,
        CAST(co.z AS DECIMAL(18,6)) AS z,
        CAST(0 AS DECIMAL(18,6)) AS rx,
        CAST(0 AS DECIMAL(18,6)) AS ry,
        CAST(0 AS DECIMAL(18,6)) AS rz,
        CAST(IFNULL(m.default_display_info, 0) AS UNSIGNED) AS displayInfo,
        CAST(IFNULL(m.default_outfit_info, 0) AS UNSIGNED) AS outfitInfo,
        CAST(IFNULL(m.client_faction, 0) AS UNSIGNED) AS faction1,
        CAST(IFNULL(m.client_faction, 0) AS UNSIGNED) AS faction2,
        CAST(0 AS UNSIGNED) AS questChecklistIdx,
        CAST(0 AS UNSIGNED) AS activePropId,
        CAST(0 AS UNSIGNED) AS worldSocketId,
        CAST(0 AS UNSIGNED) AS mode
      FROM jabbithole.coordinates co
      JOIN jabbithole.creatures c ON c.id = co.location_id
      JOIN nexus_forever_mapping.nf_map_creature m ON m.jabbithole_creature_id = c.id
      WHERE co.location_type = ''Creature''
        AND c.zone_id = @nf_safe_import_jabbithole_creature_coordinate_zone
        AND (IFNULL(c.worldid, 0) = 0 OR c.worldid = @nf_safe_import_entity_spawn_world)
        AND (@nf_safe_import_allow_unsafe_northern_wilds_spawns = 1 OR @nf_safe_import_entity_spawn_world <> 426)
        AND IFNULL(co.id, 0) > 0
        AND (@nf_entity_id_base + co.id) BETWEEN @nf_entity_id_base AND @nf_entity_id_max
        AND IFNULL(m.creature2_id, 0) > 0
        AND NOT EXISTS (
          SELECT 1
          FROM tmp_nf_world_entity_coordinate_reject rejected
          WHERE rejected.source_coordinate_id = co.id
            AND rejected.jabbithole_creature_id = c.id
            AND rejected.creature2_id = m.creature2_id
            AND rejected.world = @nf_safe_import_entity_spawn_world
            AND rejected.area = COALESCE(NULLIF(c.worldzoneid, 0), NULLIF(@nf_safe_import_entity_spawn_area, 0), rejected.area)
        )
        AND co.x IS NOT NULL
        AND co.y IS NOT NULL
        AND co.z IS NOT NULL
        AND (
          m.match_status IN (''unique_name'', ''scored_name'', ''reviewed'')
          OR (
            @nf_safe_import_entity_spawn_include_ambiguous_exact_name = 1
            AND m.match_status = ''ambiguous_name''
            AND TRIM(LOWER(IFNULL(m.source_name, ''''))) = TRIM(LOWER(IFNULL(m.client_name, '''')))
          )
        )
    ) direct
    WHERE direct.area > 0
      AND (@nf_safe_import_entity_spawn_area = 0 OR direct.area = @nf_safe_import_entity_spawn_area)
      AND (
        EXISTS (
          SELECT 1
          FROM entity existing_id
          WHERE existing_id.id = direct.id
        )
        OR NOT EXISTS (
          SELECT 1
          FROM entity existing
          WHERE existing.creature = direct.creature
            AND existing.world = direct.world
            AND (
              POW(existing.x - direct.x, 2)
              + POW(existing.y - direct.y, 2)
              + POW(existing.z - direct.z, 2)
            ) <= POW(@nf_safe_import_entity_spawn_match_radius, 2)
        )
      )',
  'DO 0'
);
PREPARE nfJabbitholeCoordinateImportStatement FROM @nfJabbitholeCoordinateImportSql;
EXECUTE nfJabbitholeCoordinateImportStatement;
DEALLOCATE PREPARE nfJabbitholeCoordinateImportStatement;

DELETE FROM entity
WHERE @nf_safe_import_replace_existing = 1
  AND @nf_safe_import_entity_spawns = 1
  AND @nf_safe_import_entity_spawn_world > 0
  AND id BETWEEN @nf_entity_id_base AND @nf_entity_id_max
  AND world = @nf_safe_import_entity_spawn_world
  AND (@nf_safe_import_entity_spawn_area = 0 OR area = @nf_safe_import_entity_spawn_area);

INSERT IGNORE INTO entity (
  id, type, creature, world, area, x, y, z, rx, ry, rz, displayInfo, outfitInfo,
  faction1, faction2, questChecklistIdx, activePropId, worldSocketId, mode
)
SELECT
  id, type, creature, world, area, x, y, z, rx, ry, rz, displayInfo, outfitInfo,
  faction1, faction2, questChecklistIdx, activePropId, worldSocketId, mode
FROM tmp_nf_world_entity_import;

INSERT INTO entity_stats (id, stat, value)
SELECT
  i.id,
  CAST(s.Stat AS UNSIGNED),
  s.Value
FROM tmp_nf_world_entity_import i
JOIN nexus_forever_mapping.nf_map_world_entity_stats_candidate s ON s.source_coordinate_id = i.source_coordinate_id
JOIN nexus_forever_mapping.nf_map_creature m ON m.jabbithole_creature_id = s.jabbithole_creature_id AND m.creature2_id = i.creature
WHERE s.Stat BETWEEN 0 AND 255
  AND (s.Stat <> 0 OR (IFNULL(m.template_base_health, 0) > 0 AND ABS(s.Value - m.template_base_health) < 0.01))
ON DUPLICATE KEY UPDATE
  value = VALUES(value);

SET @nfJabbitholeCoordinateStatsSql := IF(
  @nf_safe_import_entity_spawns = 1
    AND @nf_safe_import_jabbithole_creature_coordinates = 1
    AND @nf_safe_import_entity_spawn_world > 0
    AND @nf_safe_import_jabbithole_creature_coordinate_zone > 0,
  'INSERT INTO entity_stats (id, stat, value)
    SELECT
      i.id,
      stat_def.stat,
      CAST(
        CASE stat_def.stat
          WHEN 0 THEN m.template_base_health
          WHEN 10 THEN COALESCE(NULLIF(m.level_max, 0), NULLIF(m.level_min, 0))
          WHEN 20 THEN m.shield
          WHEN 21 THEN m.interrupt_armor_max
        END AS DECIMAL(18,6)
      )
    FROM tmp_nf_world_entity_import i
    JOIN jabbithole.coordinates co ON co.id = i.source_coordinate_id
    JOIN jabbithole.creatures c ON c.id = co.location_id
    JOIN nexus_forever_mapping.nf_map_creature m ON m.jabbithole_creature_id = c.id AND m.creature2_id = i.creature
    JOIN (
      SELECT 0 AS stat
      UNION ALL SELECT 10
      UNION ALL SELECT 20
      UNION ALL SELECT 21
    ) stat_def
    WHERE co.location_type = ''Creature''
      AND c.zone_id = @nf_safe_import_jabbithole_creature_coordinate_zone
      AND (IFNULL(c.worldid, 0) = 0 OR c.worldid = @nf_safe_import_entity_spawn_world)
      AND (
        CASE stat_def.stat
          WHEN 0 THEN m.template_base_health
          WHEN 10 THEN COALESCE(NULLIF(m.level_max, 0), NULLIF(m.level_min, 0))
          WHEN 20 THEN m.shield
          WHEN 21 THEN m.interrupt_armor_max
        END
      ) > 0
      AND (
        m.match_status IN (''unique_name'', ''scored_name'', ''reviewed'')
        OR (
          @nf_safe_import_entity_spawn_include_ambiguous_exact_name = 1
          AND m.match_status = ''ambiguous_name''
          AND TRIM(LOWER(IFNULL(m.source_name, ''''))) = TRIM(LOWER(IFNULL(m.client_name, '''')))
        )
      )
    ON DUPLICATE KEY UPDATE
      value = VALUES(value)',
  'DO 0'
);
PREPARE nfJabbitholeCoordinateStatsStatement FROM @nfJabbitholeCoordinateStatsSql;
EXECUTE nfJabbitholeCoordinateStatsStatement;
DEALLOCATE PREPARE nfJabbitholeCoordinateStatsStatement;

DROP TEMPORARY TABLE IF EXISTS tmp_nf_vendor_item_latest;
CREATE TEMPORARY TABLE tmp_nf_vendor_item_latest ENGINE=InnoDB AS
SELECT *
FROM (
  SELECT
    v.*,
    ROW_NUMBER() OVER (
      PARTITION BY v.creature2_id, v.item2_id
      ORDER BY IFNULL(v.game_version, 0) DESC, v.vendor_item_source_id DESC
    ) AS import_rank
  FROM nexus_forever_mapping.nf_map_vendor_item v
  WHERE v.match_status IN ('unique_name', 'scored_name', 'reviewed')
    AND IFNULL(v.creature2_id, 0) > 0
    AND IFNULL(v.item2_id, 0) > 0
) ranked
WHERE ranked.import_rank = 1;

DROP TEMPORARY TABLE IF EXISTS tmp_nf_vendor_item_import;
CREATE TEMPORARY TABLE tmp_nf_vendor_item_import ENGINE=InnoDB AS
SELECT
  creature2_id,
  ROW_NUMBER() OVER (
    PARTITION BY creature2_id
    ORDER BY IFNULL(source_name, ''), item2_id, vendor_item_source_id
  ) AS item_index,
  item2_id,
  IF(IFNULL(main_currency_id, 0) <> 0 AND IFNULL(main_price, 0) > 0, 2, 0) AS extraCost1Type,
  IF(IFNULL(main_currency_id, 0) <> 0 AND IFNULL(main_price, 0) > 0, IFNULL(main_price, 0), 0) AS extraCost1Quantity,
  IF(IFNULL(main_currency_id, 0) <> 0 AND IFNULL(main_price, 0) > 0, IFNULL(main_currency_id, 0), 0) AS extraCost1ItemOrCurrencyId,
  IF(IFNULL(alt_currency_id, 0) <> 0 AND IFNULL(alt_price, 0) > 0, 2, 0) AS extraCost2Type,
  IF(IFNULL(alt_currency_id, 0) <> 0 AND IFNULL(alt_price, 0) > 0, IFNULL(alt_price, 0), 0) AS extraCost2Quantity,
  IF(IFNULL(alt_currency_id, 0) <> 0 AND IFNULL(alt_price, 0) > 0, IFNULL(alt_currency_id, 0), 0) AS extraCost2ItemOrCurrencyId
FROM tmp_nf_vendor_item_latest;

ALTER TABLE tmp_nf_vendor_item_import
  ADD KEY ix_tmp_nf_vendor_item_import_creature (creature2_id),
  ADD KEY ix_tmp_nf_vendor_item_import_item (item2_id);

DELETE evi
FROM entity_vendor_item evi
JOIN entity e ON e.id = evi.id
JOIN (SELECT DISTINCT creature2_id FROM tmp_nf_vendor_item_import) mapped ON mapped.creature2_id = e.creature
WHERE @nf_safe_import_replace_existing = 1;

DELETE evc
FROM entity_vendor_category evc
JOIN entity e ON e.id = evc.id
JOIN (SELECT DISTINCT creature2_id FROM tmp_nf_vendor_item_import) mapped ON mapped.creature2_id = e.creature
WHERE @nf_safe_import_replace_existing = 1;

DELETE ev
FROM entity_vendor ev
JOIN entity e ON e.id = ev.id
JOIN (SELECT DISTINCT creature2_id FROM tmp_nf_vendor_item_import) mapped ON mapped.creature2_id = e.creature
WHERE @nf_safe_import_replace_existing = 1;

INSERT IGNORE INTO entity_vendor (id, buyPriceMultiplier, sellPriceMultiplier)
SELECT DISTINCT e.id, 1, 1
FROM entity e
JOIN tmp_nf_vendor_item_import m ON m.creature2_id = e.creature;

INSERT IGNORE INTO entity_vendor_category (id, `index`, localisedTextId)
SELECT DISTINCT e.id, 0, 0
FROM entity e
JOIN tmp_nf_vendor_item_import m ON m.creature2_id = e.creature;

INSERT IGNORE INTO entity_vendor_item (
  id, `index`, categoryIndex, itemId,
  extraCost1ItemOrCurrencyId, extraCost1Quantity, extraCost1Type,
  extraCost2ItemOrCurrencyId, extraCost2Quantity, extraCost2Type
)
SELECT
  e.id,
  m.item_index,
  0,
  m.item2_id,
  m.extraCost1ItemOrCurrencyId,
  m.extraCost1Quantity,
  m.extraCost1Type,
  m.extraCost2ItemOrCurrencyId,
  m.extraCost2Quantity,
  m.extraCost2Type
FROM entity e
JOIN tmp_nf_vendor_item_import m ON m.creature2_id = e.creature;

INSERT INTO creature_loot (
  creatureId,
  itemId,
  chance,
  dropTimes,
  aggregateDropSum,
  aggregateDropCount,
  gameVersion,
  sourceDropId,
  versionedItemDropAggregateId,
  versionedCreatureDropAggregateId,
  lastSeenIn,
  matchStatus,
  sourceName,
  itemName
)
SELECT
  creature2_id,
  item2_id,
  CAST(LEAST(1, GREATEST(0, IFNULL(drop_probability, 0))) AS DECIMAL(12,8)),
  IFNULL(drop_times, 0),
  IFNULL(aggregate_drop_sum, 0),
  IFNULL(aggregate_drop_count, 0),
  IFNULL(game_version, 0),
  source_drop_id,
  IFNULL(versioned_item_drop_aggregate_id, 0),
  IFNULL(versioned_creature_drop_aggregate_id, 0),
  IFNULL(last_seen_in, 0),
  IFNULL(match_status, ''),
  IFNULL(source_name, ''),
  IFNULL(item_name, '')
FROM (
  SELECT
    l.*,
    ROW_NUMBER() OVER (
      PARTITION BY l.creature2_id, l.item2_id
      ORDER BY IFNULL(l.game_version, 0) DESC, l.source_drop_id DESC
    ) AS import_rank
  FROM nexus_forever_mapping.nf_map_creature_loot l
  WHERE l.match_status IN ('unique_name', 'scored_name', 'reviewed')
    AND IFNULL(l.creature2_id, 0) > 0
    AND IFNULL(l.item2_id, 0) > 0
) ranked
WHERE ranked.import_rank = 1
ON DUPLICATE KEY UPDATE
  chance = VALUES(chance),
  dropTimes = VALUES(dropTimes),
  aggregateDropSum = VALUES(aggregateDropSum),
  aggregateDropCount = VALUES(aggregateDropCount),
  gameVersion = VALUES(gameVersion),
  sourceDropId = VALUES(sourceDropId),
  versionedItemDropAggregateId = VALUES(versionedItemDropAggregateId),
  versionedCreatureDropAggregateId = VALUES(versionedCreatureDropAggregateId),
  lastSeenIn = VALUES(lastSeenIn),
  matchStatus = VALUES(matchStatus),
  sourceName = VALUES(sourceName),
  itemName = VALUES(itemName);

DELETE li
FROM loot_item li
JOIN loot_group lg ON lg.id = li.id
WHERE @nf_safe_import_replace_existing = 1
  AND lg.id BETWEEN @nf_loot_group_base AND @nf_loot_group_max
  AND lg.comment LIKE 'DataMapping creature_loot%';

DELETE el
FROM entity_loot el
JOIN loot_group lg ON lg.id = el.lootGroupId
WHERE @nf_safe_import_replace_existing = 1
  AND lg.id BETWEEN @nf_loot_group_base AND @nf_loot_group_max
  AND lg.comment LIKE 'DataMapping creature_loot%';

DELETE FROM loot_group
WHERE @nf_safe_import_replace_existing = 1
  AND id BETWEEN @nf_loot_group_base AND @nf_loot_group_max
  AND comment LIKE 'DataMapping creature_loot%';

DROP TEMPORARY TABLE IF EXISTS tmp_nf_runtime_loot_groups;
CREATE TEMPORARY TABLE tmp_nf_runtime_loot_groups ENGINE=InnoDB AS
SELECT
  creatureId,
  CAST(@nf_loot_group_base + creatureId AS UNSIGNED) AS lootGroupId,
  LEFT(CONCAT('DataMapping creature_loot: ', IFNULL(MIN(NULLIF(sourceName, '')), CONCAT('Creature2 ', creatureId))), 200) AS comment
FROM creature_loot
WHERE chance > 0
GROUP BY creatureId;

ALTER TABLE tmp_nf_runtime_loot_groups
  ADD PRIMARY KEY (creatureId),
  ADD UNIQUE KEY ux_tmp_nf_runtime_loot_groups_group (lootGroupId);

DROP TEMPORARY TABLE IF EXISTS tmp_nf_runtime_loot_items;
CREATE TEMPORARY TABLE tmp_nf_runtime_loot_items ENGINE=InnoDB AS
SELECT
  CAST(@nf_loot_group_base + creatureId AS UNSIGNED) AS lootGroupId,
  itemId,
  CAST(LEAST(100, GREATEST(0, chance * 100)) AS DECIMAL(9,4)) AS probability,
  1 AS minCount,
  CAST(
    CASE
      WHEN @nf_safe_import_creature_loot_counts_from_aggregates = 1
        AND IFNULL(dropTimes, 0) > 0
        AND IFNULL(aggregateDropSum, 0) > IFNULL(dropTimes, 0)
        THEN LEAST(100, GREATEST(1, CEIL(aggregateDropSum / dropTimes)))
      ELSE 1
    END AS UNSIGNED
  ) AS maxCount,
  LEFT(CONCAT('DataMapping creature_loot: ', IFNULL(NULLIF(itemName, ''), CONCAT('Item2 ', itemId))), 200) AS comment
FROM creature_loot
WHERE chance > 0
  AND itemId > 0;

ALTER TABLE tmp_nf_runtime_loot_items
  ADD KEY ix_tmp_nf_runtime_loot_items_group (lootGroupId),
  ADD KEY ix_tmp_nf_runtime_loot_items_item (itemId);

INSERT INTO loot_group (`id`, `parentId`, `probability`, `minDrop`, `maxDrop`, `conditionType`, `condition`, `comment`)
SELECT lootGroupId, NULL, 100, 0, 0, 0, 0, comment
FROM tmp_nf_runtime_loot_groups
ON DUPLICATE KEY UPDATE
  `parentId` = VALUES(`parentId`),
  `probability` = VALUES(`probability`),
  `minDrop` = VALUES(`minDrop`),
  `maxDrop` = VALUES(`maxDrop`),
  `conditionType` = VALUES(`conditionType`),
  `condition` = VALUES(`condition`),
  `comment` = VALUES(`comment`);

INSERT INTO entity_loot (`id`, `lootGroupId`, `comment`)
SELECT creatureId, lootGroupId, comment
FROM tmp_nf_runtime_loot_groups
ON DUPLICATE KEY UPDATE
  `comment` = VALUES(`comment`);

INSERT INTO loot_item (`id`, `type`, `staticId`, `probability`, `minCount`, `maxCount`, `comment`)
SELECT lootGroupId, 0, itemId, probability, minCount, maxCount, comment
FROM tmp_nf_runtime_loot_items
ON DUPLICATE KEY UPDATE
  `probability` = VALUES(`probability`),
  `minCount` = VALUES(`minCount`),
  `maxCount` = VALUES(`maxCount`),
  `comment` = VALUES(`comment`);

DELETE li
FROM loot_item li
JOIN loot_group lg ON lg.id = li.id
WHERE @nf_safe_import_replace_existing = 1
  AND lg.id BETWEEN @nf_item_loot_group_base AND @nf_item_loot_group_max
  AND lg.comment LIKE 'DataMapping item_container%';

DELETE il
FROM item_loot il
JOIN loot_group lg ON lg.id = il.lootGroupId
WHERE @nf_safe_import_replace_existing = 1
  AND lg.id BETWEEN @nf_item_loot_group_base AND @nf_item_loot_group_max
  AND lg.comment LIKE 'DataMapping item_container%';

DELETE FROM loot_group
WHERE @nf_safe_import_replace_existing = 1
  AND id BETWEEN @nf_item_loot_group_base AND @nf_item_loot_group_max
  AND comment LIKE 'DataMapping item_container%';

DROP TEMPORARY TABLE IF EXISTS tmp_nf_runtime_item_loot_groups;
CREATE TEMPORARY TABLE tmp_nf_runtime_item_loot_groups ENGINE=InnoDB AS
SELECT
  container_item2_id AS itemId,
  CAST(@nf_item_loot_group_base + container_item2_id AS UNSIGNED) AS lootGroupId,
  LEFT(CONCAT('DataMapping item_container: ', IFNULL(MIN(NULLIF(container_item_name, '')), CONCAT('Item2 ', container_item2_id))), 200) AS comment
FROM nexus_forever_mapping.nf_map_item_container
WHERE IFNULL(container_item2_id, 0) > 0
  AND IFNULL(contained_item2_id, 0) > 0
GROUP BY container_item2_id;

ALTER TABLE tmp_nf_runtime_item_loot_groups
  ADD PRIMARY KEY (itemId),
  ADD UNIQUE KEY ux_tmp_nf_runtime_item_loot_groups_group (lootGroupId);

DROP TEMPORARY TABLE IF EXISTS tmp_nf_runtime_item_container_weights;
CREATE TEMPORARY TABLE tmp_nf_runtime_item_container_weights ENGINE=InnoDB AS
SELECT
  container_item2_id AS itemId,
  contained_item2_id AS containedItemId,
  IFNULL(NULLIF(MIN(contained_item_name), ''), CONCAT('Item2 ', contained_item2_id)) AS containedItemName,
  SUM(GREATEST(IFNULL(drop_times, 0), 1)) AS itemWeight
FROM nexus_forever_mapping.nf_map_item_container
WHERE IFNULL(container_item2_id, 0) > 0
  AND IFNULL(contained_item2_id, 0) > 0
GROUP BY container_item2_id, contained_item2_id;

ALTER TABLE tmp_nf_runtime_item_container_weights
  ADD KEY ix_tmp_nf_runtime_item_container_weights_item (itemId),
  ADD KEY ix_tmp_nf_runtime_item_container_weights_contained (containedItemId);

DROP TEMPORARY TABLE IF EXISTS tmp_nf_runtime_item_container_totals;
CREATE TEMPORARY TABLE tmp_nf_runtime_item_container_totals ENGINE=InnoDB AS
SELECT itemId, SUM(itemWeight) AS totalWeight
FROM tmp_nf_runtime_item_container_weights
GROUP BY itemId;

ALTER TABLE tmp_nf_runtime_item_container_totals
  ADD PRIMARY KEY (itemId);

DROP TEMPORARY TABLE IF EXISTS tmp_nf_runtime_item_loot_items;
CREATE TEMPORARY TABLE tmp_nf_runtime_item_loot_items ENGINE=InnoDB AS
SELECT
  g.lootGroupId,
  w.containedItemId AS itemId,
  CAST(LEAST(100, GREATEST(0, 100 * w.itemWeight / t.totalWeight)) AS DECIMAL(9,4)) AS probability,
  1 AS minCount,
  1 AS maxCount,
  LEFT(CONCAT('DataMapping item_container: ', w.containedItemName), 200) AS comment
FROM tmp_nf_runtime_item_container_weights w
JOIN tmp_nf_runtime_item_container_totals t ON t.itemId = w.itemId
JOIN tmp_nf_runtime_item_loot_groups g ON g.itemId = w.itemId
WHERE t.totalWeight > 0;

ALTER TABLE tmp_nf_runtime_item_loot_items
  ADD KEY ix_tmp_nf_runtime_item_loot_items_group (lootGroupId),
  ADD KEY ix_tmp_nf_runtime_item_loot_items_item (itemId);

INSERT INTO loot_group (`id`, `parentId`, `probability`, `minDrop`, `maxDrop`, `conditionType`, `condition`, `comment`)
SELECT lootGroupId, NULL, 100, 1, 1, 0, 0, comment
FROM tmp_nf_runtime_item_loot_groups
ON DUPLICATE KEY UPDATE
  `parentId` = VALUES(`parentId`),
  `probability` = VALUES(`probability`),
  `minDrop` = VALUES(`minDrop`),
  `maxDrop` = VALUES(`maxDrop`),
  `conditionType` = VALUES(`conditionType`),
  `condition` = VALUES(`condition`),
  `comment` = VALUES(`comment`);

INSERT INTO item_loot (`id`, `lootGroupId`, `comment`)
SELECT itemId, lootGroupId, comment
FROM tmp_nf_runtime_item_loot_groups
ON DUPLICATE KEY UPDATE
  `comment` = VALUES(`comment`);

INSERT INTO loot_item (`id`, `type`, `staticId`, `probability`, `minCount`, `maxCount`, `comment`)
SELECT lootGroupId, 0, itemId, probability, minCount, maxCount, comment
FROM tmp_nf_runtime_item_loot_items
ON DUPLICATE KEY UPDATE
  `probability` = VALUES(`probability`),
  `minCount` = VALUES(`minCount`),
  `maxCount` = VALUES(`maxCount`),
  `comment` = VALUES(`comment`);

DELETE li
FROM loot_item li
JOIN loot_group lg ON lg.id = li.id
WHERE lg.comment LIKE 'DataMapping item_salvage%';

DELETE il
FROM item_loot il
JOIN loot_group lg ON lg.id = il.lootGroupId
WHERE lg.comment LIKE 'DataMapping item_salvage%';

DELETE FROM loot_group
WHERE comment LIKE 'DataMapping item_salvage%';

DELETE FROM item_salvage
WHERE @nf_safe_import_replace_existing = 1
  AND comment LIKE 'DataMapping item_salvage%';

DROP TEMPORARY TABLE IF EXISTS tmp_nf_runtime_item_salvage_exact_weights;
CREATE TEMPORARY TABLE tmp_nf_runtime_item_salvage_exact_weights ENGINE=InnoDB AS
SELECT
  original_item2_id AS sourceItemId,
  salvaged_item2_id AS staticId,
  IFNULL(MIN(NULLIF(original_item_name, '')), CONCAT('Item2 ', original_item2_id)) AS sourceItemName,
  IFNULL(MIN(NULLIF(salvaged_item_name, '')), CONCAT('Item2 ', salvaged_item2_id)) AS salvagedItemName,
  SUM(GREATEST(IFNULL(drop_times, 0), 1)) AS itemWeight
FROM nexus_forever_mapping.nf_map_item_salvage
WHERE IFNULL(original_item2_id, 0) > 0
  AND IFNULL(salvaged_item2_id, 0) > 0
GROUP BY original_item2_id, salvaged_item2_id;

ALTER TABLE tmp_nf_runtime_item_salvage_exact_weights
  ADD KEY ix_tmp_nf_runtime_item_salvage_exact_weights_source (sourceItemId),
  ADD KEY ix_tmp_nf_runtime_item_salvage_exact_weights_static (staticId);

DROP TEMPORARY TABLE IF EXISTS tmp_nf_runtime_item_salvage_exact_totals;
CREATE TEMPORARY TABLE tmp_nf_runtime_item_salvage_exact_totals ENGINE=InnoDB AS
SELECT sourceItemId, SUM(itemWeight) AS totalWeight
FROM tmp_nf_runtime_item_salvage_exact_weights
GROUP BY sourceItemId;

ALTER TABLE tmp_nf_runtime_item_salvage_exact_totals
  ADD PRIMARY KEY (sourceItemId);

DROP TEMPORARY TABLE IF EXISTS tmp_nf_runtime_item_salvage_exact;
CREATE TEMPORARY TABLE tmp_nf_runtime_item_salvage_exact ENGINE=InnoDB AS
SELECT
  0 AS purpose,
  w.sourceItemId,
  0 AS sourceItem2TypeId,
  0 AS sourceLevel,
  0 AS type,
  w.staticId,
  CAST(LEAST(100, GREATEST(0, 100 * w.itemWeight / t.totalWeight)) AS DECIMAL(9,4)) AS probability,
  1 AS minCount,
  1 AS maxCount,
  LEFT(CONCAT('DataMapping item_salvage exact: ', w.sourceItemName, ' -> ', w.salvagedItemName), 200) AS comment
FROM tmp_nf_runtime_item_salvage_exact_weights w
JOIN tmp_nf_runtime_item_salvage_exact_totals t ON t.sourceItemId = w.sourceItemId
WHERE t.totalWeight > 0;

ALTER TABLE tmp_nf_runtime_item_salvage_exact
  ADD PRIMARY KEY (purpose, sourceItemId, sourceItem2TypeId, sourceLevel, type, staticId);

DROP TEMPORARY TABLE IF EXISTS tmp_nf_runtime_item_salvage_type_level;
CREATE TEMPORARY TABLE tmp_nf_runtime_item_salvage_type_level ENGINE=InnoDB AS
SELECT
  1 AS purpose,
  0 AS sourceItemId,
  item2TypeId AS sourceItem2TypeId,
  level AS sourceLevel,
  0 AS type,
  CASE
    WHEN level >= 45 THEN 16565
    WHEN level >= 35 THEN 14784
    WHEN level >= 25 THEN 14783
    WHEN level >= 15 THEN 14782
    ELSE 14781
  END AS staticId,
  100 AS probability,
  1 AS minCount,
  1 AS maxCount,
  LEFT(CONCAT('DataMapping item_salvage client type-level: ', IFNULL(MIN(NULLIF(item2TypeId_label, '')), CONCAT('Item2Type ', item2TypeId)), ' level ', level), 200) AS comment
FROM nexus_forever_mapping.nf_map_client_source_salvage
WHERE IFNULL(item2TypeId, 0) > 0
  AND IFNULL(level, 0) > 0
GROUP BY item2TypeId, level;

ALTER TABLE tmp_nf_runtime_item_salvage_type_level
  ADD PRIMARY KEY (purpose, sourceItemId, sourceItem2TypeId, sourceLevel, type, staticId);

INSERT INTO item_salvage (`purpose`, `sourceItemId`, `sourceItem2TypeId`, `sourceLevel`, `type`, `staticId`, `probability`, `minCount`, `maxCount`, `comment`)
SELECT purpose, sourceItemId, sourceItem2TypeId, sourceLevel, type, staticId, probability, minCount, maxCount, comment
FROM tmp_nf_runtime_item_salvage_exact
ON DUPLICATE KEY UPDATE
  `probability` = VALUES(`probability`),
  `minCount` = VALUES(`minCount`),
  `maxCount` = VALUES(`maxCount`),
  `comment` = VALUES(`comment`);

INSERT INTO item_salvage (`purpose`, `sourceItemId`, `sourceItem2TypeId`, `sourceLevel`, `type`, `staticId`, `probability`, `minCount`, `maxCount`, `comment`)
SELECT purpose, sourceItemId, sourceItem2TypeId, sourceLevel, type, staticId, probability, minCount, maxCount, comment
FROM tmp_nf_runtime_item_salvage_type_level
ON DUPLICATE KEY UPDATE
  `probability` = VALUES(`probability`),
  `minCount` = VALUES(`minCount`),
  `maxCount` = VALUES(`maxCount`),
  `comment` = VALUES(`comment`);

DELETE FROM creature_info_property
WHERE @nf_safe_import_replace_existing = 1
  AND `property` IN (7, 41);

DELETE FROM creature_info_stat
WHERE @nf_safe_import_replace_existing = 1
  AND `stat` IN (21);

-- Health ranges such as 353-844 are source observations across a level band.
-- A creature_info_property row is template-wide, so importing the range maximum
-- pins every level of that creature to the max-level health. Remove prior rows
-- created from that unsafe shape and only import collapsed/single health values
-- below.
DELETE p
FROM creature_info_property p
JOIN nexus_forever_mapping.nf_map_creature m ON m.creature2_id = p.id
WHERE p.`property` = 7
  AND m.match_status IN ('unique_name', 'scored_name', 'reviewed')
  AND IFNULL(m.creature2_id, 0) > 0
  AND IFNULL(m.health_min, 0) > 0
  AND IFNULL(m.health_max, 0) > 0
  AND m.health_min <> m.health_max
  AND ABS(p.`value` - m.health_max) < 0.01;

DROP TEMPORARY TABLE IF EXISTS tmp_nf_creature_info_property_candidate;
CREATE TEMPORARY TABLE tmp_nf_creature_info_property_candidate ENGINE=InnoDB AS
SELECT creature2_id AS id, 7 AS `property`, CAST(CASE
    WHEN IFNULL(template_base_health, 0) > 0 THEN template_base_health
  END AS DECIMAL(18,6)) AS `value`
FROM nexus_forever_mapping.nf_map_creature
WHERE match_status IN ('unique_name', 'scored_name', 'reviewed')
  AND IFNULL(creature2_id, 0) > 0
  AND IFNULL(template_base_health, 0) > 0
UNION ALL
SELECT creature2_id AS id, 41 AS `property`, CAST(shield AS DECIMAL(18,6)) AS `value`
FROM nexus_forever_mapping.nf_map_creature
WHERE match_status IN ('unique_name', 'scored_name', 'reviewed')
  AND IFNULL(creature2_id, 0) > 0
  AND IFNULL(shield, 0) > 0;

DROP TEMPORARY TABLE IF EXISTS tmp_nf_creature_info_stat_candidate;
CREATE TEMPORARY TABLE tmp_nf_creature_info_stat_candidate ENGINE=InnoDB AS
SELECT creature2_id AS id, 21 AS `stat`, CAST(interrupt_armor_max AS DECIMAL(18,6)) AS `value`
FROM nexus_forever_mapping.nf_map_creature
WHERE match_status IN ('unique_name', 'scored_name', 'reviewed')
  AND IFNULL(creature2_id, 0) > 0
  AND IFNULL(interrupt_armor_max, 0) > 0;

INSERT IGNORE INTO creature_info_property (`id`, `property`, `value`)
SELECT id, `property`, MAX(`value`)
FROM tmp_nf_creature_info_property_candidate
GROUP BY id, `property`
HAVING COUNT(DISTINCT `value`) = 1;

INSERT IGNORE INTO creature_info_stat (`id`, `stat`, `value`)
SELECT id, `stat`, MAX(`value`)
FROM tmp_nf_creature_info_stat_candidate
GROUP BY id, `stat`
HAVING COUNT(DISTINCT `value`) = 1;

-- Rider's Reef tutorial combat cleanup.
-- The official/imported world rows can include three stray Beacon Arrow entities
-- on the Exile mine anchors and turret rows imported as neutral AiTurretEntity
-- placeholders. Normalize the imported runtime rows here so fresh world
-- databases start from the intended combat lane data.
DELETE FROM entity
WHERE world = 3460
  AND creature = 73665;

UPDATE entity
SET type = 0,
    faction1 = CASE creature
        WHEN 73494 THEN 1441
        WHEN 74862 THEN 1442
        ELSE faction1
    END,
    faction2 = CASE creature
        WHEN 73494 THEN 1441
        WHEN 74862 THEN 1442
        ELSE faction2
    END
WHERE world = 3460
  AND creature IN (73494, 74862);

-- Official world visible duplicate cleanup.
DELETE es
FROM entity_spline es
JOIN entity e ON e.id = es.id
WHERE
  (e.id = 47922 AND e.world = 3404 AND e.creature = 71468 AND e.area = 4826 AND ABS(e.x - (-72.8101)) < 0.001 AND ABS(e.y - (-844.8)) < 0.001 AND ABS(e.z - 7.84299) < 0.001)
  OR (e.id = 47937 AND e.world = 3404 AND e.creature = 71628 AND e.area = 1622 AND ABS(e.x - 37.2705) < 0.001 AND ABS(e.y - (-840.065)) < 0.001 AND ABS(e.z - 173.363) < 0.001)
  OR (e.id = 29208 AND e.world = 3460 AND e.creature = 73690 AND e.area = 5969 AND ABS(e.x - 1290.97) < 0.001 AND ABS(e.y - (-495.031)) < 0.001 AND ABS(e.z - 354.958) < 0.001)
  OR (e.id = 29209 AND e.world = 3460 AND e.creature = 73690 AND e.area = 5969 AND ABS(e.x - 1294.33) < 0.001 AND ABS(e.y - (-495.328)) < 0.001 AND ABS(e.z - 337.609) < 0.001);

DELETE ep
FROM entity_property ep
JOIN entity e ON e.id = ep.id
WHERE
  (e.id = 47922 AND e.world = 3404 AND e.creature = 71468 AND e.area = 4826 AND ABS(e.x - (-72.8101)) < 0.001 AND ABS(e.y - (-844.8)) < 0.001 AND ABS(e.z - 7.84299) < 0.001)
  OR (e.id = 47937 AND e.world = 3404 AND e.creature = 71628 AND e.area = 1622 AND ABS(e.x - 37.2705) < 0.001 AND ABS(e.y - (-840.065)) < 0.001 AND ABS(e.z - 173.363) < 0.001)
  OR (e.id = 29208 AND e.world = 3460 AND e.creature = 73690 AND e.area = 5969 AND ABS(e.x - 1290.97) < 0.001 AND ABS(e.y - (-495.031)) < 0.001 AND ABS(e.z - 354.958) < 0.001)
  OR (e.id = 29209 AND e.world = 3460 AND e.creature = 73690 AND e.area = 5969 AND ABS(e.x - 1294.33) < 0.001 AND ABS(e.y - (-495.328)) < 0.001 AND ABS(e.z - 337.609) < 0.001);

DELETE ev
FROM entity_event ev
JOIN entity e ON e.id = ev.id
WHERE
  (e.id = 47922 AND e.world = 3404 AND e.creature = 71468 AND e.area = 4826 AND ABS(e.x - (-72.8101)) < 0.001 AND ABS(e.y - (-844.8)) < 0.001 AND ABS(e.z - 7.84299) < 0.001)
  OR (e.id = 47937 AND e.world = 3404 AND e.creature = 71628 AND e.area = 1622 AND ABS(e.x - 37.2705) < 0.001 AND ABS(e.y - (-840.065)) < 0.001 AND ABS(e.z - 173.363) < 0.001)
  OR (e.id = 29208 AND e.world = 3460 AND e.creature = 73690 AND e.area = 5969 AND ABS(e.x - 1290.97) < 0.001 AND ABS(e.y - (-495.031)) < 0.001 AND ABS(e.z - 354.958) < 0.001)
  OR (e.id = 29209 AND e.world = 3460 AND e.creature = 73690 AND e.area = 5969 AND ABS(e.x - 1294.33) < 0.001 AND ABS(e.y - (-495.328)) < 0.001 AND ABS(e.z - 337.609) < 0.001);

DELETE est
FROM entity_stats est
JOIN entity e ON e.id = est.id
WHERE
  (e.id = 47922 AND e.world = 3404 AND e.creature = 71468 AND e.area = 4826 AND ABS(e.x - (-72.8101)) < 0.001 AND ABS(e.y - (-844.8)) < 0.001 AND ABS(e.z - 7.84299) < 0.001)
  OR (e.id = 47937 AND e.world = 3404 AND e.creature = 71628 AND e.area = 1622 AND ABS(e.x - 37.2705) < 0.001 AND ABS(e.y - (-840.065)) < 0.001 AND ABS(e.z - 173.363) < 0.001)
  OR (e.id = 29208 AND e.world = 3460 AND e.creature = 73690 AND e.area = 5969 AND ABS(e.x - 1290.97) < 0.001 AND ABS(e.y - (-495.031)) < 0.001 AND ABS(e.z - 354.958) < 0.001)
  OR (e.id = 29209 AND e.world = 3460 AND e.creature = 73690 AND e.area = 5969 AND ABS(e.x - 1294.33) < 0.001 AND ABS(e.y - (-495.328)) < 0.001 AND ABS(e.z - 337.609) < 0.001);

DELETE FROM entity
WHERE
  (id = 47922 AND world = 3404 AND creature = 71468 AND area = 4826 AND ABS(x - (-72.8101)) < 0.001 AND ABS(y - (-844.8)) < 0.001 AND ABS(z - 7.84299) < 0.001)
  OR (id = 47937 AND world = 3404 AND creature = 71628 AND area = 1622 AND ABS(x - 37.2705) < 0.001 AND ABS(y - (-840.065)) < 0.001 AND ABS(z - 173.363) < 0.001)
  OR (id = 29208 AND world = 3460 AND creature = 73690 AND area = 5969 AND ABS(x - 1290.97) < 0.001 AND ABS(y - (-495.031)) < 0.001 AND ABS(z - 354.958) < 0.001)
  OR (id = 29209 AND world = 3460 AND creature = 73690 AND area = 5969 AND ABS(x - 1294.33) < 0.001 AND ABS(y - (-495.328)) < 0.001 AND ABS(z - 337.609) < 0.001);

-- Northern Wilds bulk coordinate cleanup.
-- Jabbithole/source coordinate observations for world 426 currently overpopulate
-- tiny map grids and are not safe retail spawn groups. Keep legacy/manual rows
-- and quest-script spawns, but remove DataMapping-owned coordinate imports unless
-- a review session explicitly opts back in.
DELETE FROM entity_loot
WHERE @nf_safe_import_allow_unsafe_northern_wilds_spawns = 0
  AND id IN (
    SELECT e.id
    FROM entity e
    WHERE e.world = 426
      AND e.id BETWEEN @nf_entity_id_base AND @nf_entity_id_max
  );

DELETE FROM entity
WHERE @nf_safe_import_allow_unsafe_northern_wilds_spawns = 0
  AND world = 426
  AND id BETWEEN @nf_entity_id_base AND @nf_entity_id_max;

-- Algoroc Dodger: reject stale source coordinate 1556666. The official/current
-- coordinate is source 4460, promoted as static entity 3894 at 3857,-1005,-4524.
DELETE FROM entity_stats
WHERE id = @nf_entity_id_base + 1556666;

DELETE FROM entity
WHERE id = @nf_entity_id_base + 1556666
  AND creature = 54640
  AND world = 51
  AND area = 23
  AND ABS(x - 3773) < 0.001
  AND ABS(y - (-999)) < 0.001
  AND ABS(z - (-4500)) < 0.001;

COMMIT;

SELECT 'entity_vendor' AS table_name, COUNT(*) AS row_count FROM entity_vendor
UNION ALL SELECT 'entity_vendor_category', COUNT(*) FROM entity_vendor_category
UNION ALL SELECT 'entity_vendor_item', COUNT(*) FROM entity_vendor_item
UNION ALL SELECT 'entity_spawns_mapped', COUNT(*) FROM entity WHERE id BETWEEN @nf_entity_id_base AND @nf_entity_id_max
UNION ALL SELECT 'entity_spawn_stats_mapped', COUNT(*) FROM entity_stats es JOIN entity e ON e.id = es.id WHERE e.id BETWEEN @nf_entity_id_base AND @nf_entity_id_max
UNION ALL SELECT 'creature_loot', COUNT(*) FROM creature_loot
UNION ALL SELECT 'loot_group_mapped', COUNT(*) FROM loot_group WHERE comment LIKE 'DataMapping creature_loot%'
UNION ALL SELECT 'entity_loot_mapped', COUNT(*) FROM entity_loot WHERE comment LIKE 'DataMapping creature_loot%'
UNION ALL SELECT 'loot_item_mapped', COUNT(*) FROM loot_item li JOIN loot_group lg ON lg.id = li.id WHERE lg.comment LIKE 'DataMapping creature_loot%'
UNION ALL SELECT 'item_loot_group_mapped', COUNT(*) FROM loot_group WHERE comment LIKE 'DataMapping item_container%'
UNION ALL SELECT 'item_loot_mapped', COUNT(*) FROM item_loot WHERE comment LIKE 'DataMapping item_container%'
UNION ALL SELECT 'item_loot_item_mapped', COUNT(*) FROM loot_item li JOIN loot_group lg ON lg.id = li.id WHERE lg.comment LIKE 'DataMapping item_container%'
UNION ALL SELECT 'item_salvage_exact_mapped', COUNT(*) FROM item_salvage WHERE purpose = 0 AND comment LIKE 'DataMapping item_salvage%'
UNION ALL SELECT 'item_salvage_type_level_mapped', COUNT(*) FROM item_salvage WHERE purpose = 1 AND comment LIKE 'DataMapping item_salvage%'
UNION ALL SELECT 'item_salvage_mapped', COUNT(*) FROM item_salvage WHERE comment LIKE 'DataMapping item_salvage%'
UNION ALL SELECT 'creature_info_property', COUNT(*) FROM creature_info_property
UNION ALL SELECT 'creature_info_stat', COUNT(*) FROM creature_info_stat
UNION ALL SELECT 'northern_wilds_unsafe_spawn_rows', COUNT(*) FROM entity WHERE world = 426 AND id BETWEEN @nf_entity_id_base AND @nf_entity_id_max;
