-- Import safe mapped WildStar/Jabbithole data from nf_map_* staging tables.
--
-- Prerequisite:
--   python Tools\DataMapping\load_mapping_staging_tables.py --apply
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

CREATE TABLE IF NOT EXISTS creature_loot (
  creatureId INT UNSIGNED NOT NULL,
  itemId INT UNSIGNED NOT NULL,
  chance DECIMAL(12,8) NOT NULL DEFAULT 0,
  dropTimes INT UNSIGNED NOT NULL DEFAULT 0,
  aggregateDropSum INT UNSIGNED NOT NULL DEFAULT 0,
  aggregateDropCount INT UNSIGNED NOT NULL DEFAULT 0,
  gameVersion INT UNSIGNED NOT NULL DEFAULT 0,
  sourceDropId INT UNSIGNED NOT NULL DEFAULT 0,
  versionedItemDropAggregateId INT UNSIGNED NOT NULL DEFAULT 0,
  versionedCreatureDropAggregateId INT UNSIGNED NOT NULL DEFAULT 0,
  lastSeenIn INT UNSIGNED NOT NULL DEFAULT 0,
  matchStatus VARCHAR(32) NOT NULL DEFAULT '',
  sourceName VARCHAR(255) NOT NULL DEFAULT '',
  itemName VARCHAR(255) NOT NULL DEFAULT '',
  PRIMARY KEY (creatureId, itemId),
  KEY ix_creature_loot_item (itemId),
  KEY ix_creature_loot_chance (chance)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS loot_group (
  `id` BIGINT UNSIGNED NOT NULL DEFAULT 0,
  `parentId` BIGINT UNSIGNED NULL,
  `probability` FLOAT NOT NULL DEFAULT 100,
  `minDrop` INT UNSIGNED NOT NULL DEFAULT 0,
  `maxDrop` INT UNSIGNED NOT NULL DEFAULT 0,
  `conditionType` INT UNSIGNED NOT NULL DEFAULT 0,
  `condition` INT UNSIGNED NOT NULL DEFAULT 0,
  `comment` VARCHAR(200) NULL DEFAULT '',
  PRIMARY KEY (`id`),
  KEY `IX_loot_group_parentId` (`parentId`),
  CONSTRAINT `FK__loot_group_parentId__loot_group_id`
    FOREIGN KEY (`parentId`) REFERENCES loot_group (`id`)
    ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS entity_loot (
  `id` INT UNSIGNED NOT NULL DEFAULT 0,
  `lootGroupId` BIGINT UNSIGNED NOT NULL,
  `comment` VARCHAR(200) NULL DEFAULT '',
  PRIMARY KEY (`id`, `lootGroupId`),
  KEY `IX_entity_loot_lootGroupId` (`lootGroupId`),
  CONSTRAINT `FK_entity_loot_loot_group_lootGroupId`
    FOREIGN KEY (`lootGroupId`) REFERENCES loot_group (`id`)
    ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS item_loot (
  `id` INT UNSIGNED NOT NULL DEFAULT 0,
  `lootGroupId` BIGINT UNSIGNED NOT NULL,
  `comment` VARCHAR(200) NULL DEFAULT '',
  PRIMARY KEY (`id`, `lootGroupId`),
  KEY `IX_item_loot_lootGroupId` (`lootGroupId`),
  CONSTRAINT `FK_item_loot_loot_group_lootGroupId`
    FOREIGN KEY (`lootGroupId`) REFERENCES loot_group (`id`)
    ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS loot_item (
  `id` BIGINT UNSIGNED NOT NULL DEFAULT 0,
  `type` INT UNSIGNED NOT NULL DEFAULT 0,
  `staticId` INT UNSIGNED NOT NULL DEFAULT 0,
  `probability` FLOAT NOT NULL DEFAULT 100,
  `minCount` INT UNSIGNED NOT NULL DEFAULT 0,
  `maxCount` INT UNSIGNED NOT NULL DEFAULT 0,
  `comment` VARCHAR(200) NULL DEFAULT '',
  PRIMARY KEY (`id`, `type`, `staticId`),
  CONSTRAINT `FK__loot_item_id__loot_group_id`
    FOREIGN KEY (`id`) REFERENCES loot_group (`id`)
    ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS creature_info_property (
  `id` INT UNSIGNED NOT NULL,
  `property` TINYINT UNSIGNED NOT NULL,
  `value` FLOAT NOT NULL,
  PRIMARY KEY (`id`, `property`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

SET @hasOldCreatureInfoPropertyValue = (
  SELECT COUNT(*)
  FROM information_schema.columns
  WHERE table_schema = DATABASE()
    AND table_name = 'creature_info_property'
    AND column_name = 'alue'
);
SET @hasCreatureInfoPropertyValue = (
  SELECT COUNT(*)
  FROM information_schema.columns
  WHERE table_schema = DATABASE()
    AND table_name = 'creature_info_property'
    AND column_name = 'value'
);
SET @repairCreatureInfoPropertyValue = IF(
  @hasOldCreatureInfoPropertyValue > 0 AND @hasCreatureInfoPropertyValue = 0,
  'ALTER TABLE creature_info_property CHANGE COLUMN `alue` `value` float NOT NULL',
  'DO 0'
);
PREPARE repairCreatureInfoPropertyValueStatement FROM @repairCreatureInfoPropertyValue;
EXECUTE repairCreatureInfoPropertyValueStatement;
DEALLOCATE PREPARE repairCreatureInfoPropertyValueStatement;

CREATE TABLE IF NOT EXISTS creature_info_stat (
  `id` INT UNSIGNED NOT NULL,
  `stat` TINYINT UNSIGNED NOT NULL,
  `value` FLOAT NOT NULL,
  PRIMARY KEY (`id`, `stat`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

SET @nf_truncate_loot_sql := IF(
  @nf_safe_import_replace_existing = 1,
  'TRUNCATE TABLE creature_loot',
  'DO 0'
);
PREPARE nfTruncateLootStatement FROM @nf_truncate_loot_sql;
EXECUTE nfTruncateLootStatement;
DEALLOCATE PREPARE nfTruncateLootStatement;

START TRANSACTION;

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
  FROM nf_map_vendor_item v
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
  FROM nf_map_creature_loot l
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
FROM nf_map_item_container
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
FROM nf_map_item_container
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

DELETE FROM creature_info_property
WHERE @nf_safe_import_replace_existing = 1
  AND `property` IN (7, 41);

DELETE FROM creature_info_stat
WHERE @nf_safe_import_replace_existing = 1
  AND `stat` IN (21);

DROP TEMPORARY TABLE IF EXISTS tmp_nf_creature_info_property_candidate;
CREATE TEMPORARY TABLE tmp_nf_creature_info_property_candidate ENGINE=InnoDB AS
SELECT creature2_id AS id, 7 AS `property`, CAST(COALESCE(NULLIF(health_max, 0), NULLIF(health_min, 0)) AS DECIMAL(18,6)) AS `value`
FROM nf_map_creature
WHERE match_status IN ('unique_name', 'scored_name', 'reviewed')
  AND IFNULL(creature2_id, 0) > 0
  AND COALESCE(NULLIF(health_max, 0), NULLIF(health_min, 0)) > 0
UNION ALL
SELECT creature2_id AS id, 41 AS `property`, CAST(shield AS DECIMAL(18,6)) AS `value`
FROM nf_map_creature
WHERE match_status IN ('unique_name', 'scored_name', 'reviewed')
  AND IFNULL(creature2_id, 0) > 0
  AND IFNULL(shield, 0) > 0;

DROP TEMPORARY TABLE IF EXISTS tmp_nf_creature_info_stat_candidate;
CREATE TEMPORARY TABLE tmp_nf_creature_info_stat_candidate ENGINE=InnoDB AS
SELECT creature2_id AS id, 21 AS `stat`, CAST(interrupt_armor_max AS DECIMAL(18,6)) AS `value`
FROM nf_map_creature
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

COMMIT;

SELECT 'entity_vendor' AS table_name, COUNT(*) AS row_count FROM entity_vendor
UNION ALL SELECT 'entity_vendor_category', COUNT(*) FROM entity_vendor_category
UNION ALL SELECT 'entity_vendor_item', COUNT(*) FROM entity_vendor_item
UNION ALL SELECT 'creature_loot', COUNT(*) FROM creature_loot
UNION ALL SELECT 'loot_group_mapped', COUNT(*) FROM loot_group WHERE comment LIKE 'DataMapping creature_loot%'
UNION ALL SELECT 'entity_loot_mapped', COUNT(*) FROM entity_loot WHERE comment LIKE 'DataMapping creature_loot%'
UNION ALL SELECT 'loot_item_mapped', COUNT(*) FROM loot_item li JOIN loot_group lg ON lg.id = li.id WHERE lg.comment LIKE 'DataMapping creature_loot%'
UNION ALL SELECT 'item_loot_group_mapped', COUNT(*) FROM loot_group WHERE comment LIKE 'DataMapping item_container%'
UNION ALL SELECT 'item_loot_mapped', COUNT(*) FROM item_loot WHERE comment LIKE 'DataMapping item_container%'
UNION ALL SELECT 'item_loot_item_mapped', COUNT(*) FROM loot_item li JOIN loot_group lg ON lg.id = li.id WHERE lg.comment LIKE 'DataMapping item_container%'
UNION ALL SELECT 'creature_info_property', COUNT(*) FROM creature_info_property
UNION ALL SELECT 'creature_info_stat', COUNT(*) FROM creature_info_stat;
