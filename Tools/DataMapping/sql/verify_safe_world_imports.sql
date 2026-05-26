-- Quick verification for the safe mapped world-data import.

USE `nexus_forever_world`;

SET @nf_entity_id_base := IFNULL(@nf_entity_id_base, 1000000000);
SET @nf_entity_id_max := @nf_entity_id_base + 999999999;

SELECT 'entity_vendor' AS table_name, COUNT(*) AS row_count FROM entity_vendor
UNION ALL SELECT 'entity_vendor_category', COUNT(*) FROM entity_vendor_category
UNION ALL SELECT 'entity_vendor_item', COUNT(*) FROM entity_vendor_item
UNION ALL SELECT 'entity_spawns_mapped', COUNT(*) FROM entity WHERE id BETWEEN @nf_entity_id_base AND @nf_entity_id_max
UNION ALL SELECT 'entity_spawn_stats_mapped', COUNT(*) FROM entity_stats es JOIN entity e ON e.id = es.id WHERE e.id BETWEEN @nf_entity_id_base AND @nf_entity_id_max
UNION ALL SELECT 'creature_loot', COUNT(*) FROM creature_loot
UNION ALL SELECT 'loot_group_mapped_creature', COUNT(*) FROM loot_group WHERE comment LIKE 'DataMapping creature_loot%'
UNION ALL SELECT 'entity_loot_mapped', COUNT(*) FROM entity_loot WHERE comment LIKE 'DataMapping creature_loot%'
UNION ALL SELECT 'loot_item_mapped_creature', COUNT(*) FROM loot_item li JOIN loot_group lg ON lg.id = li.id WHERE lg.comment LIKE 'DataMapping creature_loot%'
UNION ALL SELECT 'loot_group_mapped_item', COUNT(*) FROM loot_group WHERE comment LIKE 'DataMapping item_container%'
UNION ALL SELECT 'item_loot_mapped', COUNT(*) FROM item_loot WHERE comment LIKE 'DataMapping item_container%'
UNION ALL SELECT 'loot_item_mapped_item', COUNT(*) FROM loot_item li JOIN loot_group lg ON lg.id = li.id WHERE lg.comment LIKE 'DataMapping item_container%'
UNION ALL SELECT 'creature_info_property', COUNT(*) FROM creature_info_property
UNION ALL SELECT 'creature_info_stat', COUNT(*) FROM creature_info_stat
UNION ALL SELECT 'nf_map_tables', COUNT(*) FROM information_schema.TABLES WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME LIKE 'nf_map_%';

SELECT match_status, COUNT(*) AS row_count
FROM nf_map_creature
GROUP BY match_status
ORDER BY match_status;

SELECT 'safe_vendor_rows' AS metric, COUNT(*) AS value
FROM nf_map_vendor_item
WHERE match_status IN ('unique_name', 'scored_name', 'reviewed')
  AND IFNULL(creature2_id, 0) > 0
  AND IFNULL(item2_id, 0) > 0
UNION ALL
SELECT 'safe_loot_rows', COUNT(*)
FROM nf_map_creature_loot
WHERE match_status IN ('unique_name', 'scored_name', 'reviewed')
  AND IFNULL(creature2_id, 0) > 0
  AND IFNULL(item2_id, 0) > 0
UNION ALL
SELECT 'runtime_loot_group_creatures', COUNT(DISTINCT creatureId)
FROM creature_loot
WHERE chance > 0
UNION ALL
SELECT 'safe_creature_rows', COUNT(*)
FROM nf_map_creature
WHERE match_status IN ('unique_name', 'scored_name', 'reviewed')
  AND IFNULL(creature2_id, 0) > 0
UNION ALL
SELECT 'safe_item_container_rows', COUNT(*)
FROM nf_map_item_container
WHERE IFNULL(container_item2_id, 0) > 0
  AND IFNULL(contained_item2_id, 0) > 0;

SELECT 'riders_reef_beacon_rows' AS metric, COUNT(*) AS value
FROM entity
WHERE world = 3460
  AND creature = 73665
UNION ALL
SELECT 'riders_reef_wrong_faction_turrets', COUNT(*)
FROM entity
WHERE world = 3460
  AND (
    (creature = 73494 AND (faction1 <> 1441 OR faction2 <> 1441))
    OR (creature = 74862 AND (faction1 <> 1442 OR faction2 <> 1442))
  )
UNION ALL
SELECT 'riders_reef_wrong_type_turrets', COUNT(*)
FROM entity
WHERE world = 3460
  AND (
    (creature = 73494 AND type <> 0)
    OR (creature = 74862 AND type <> 0)
  );

SELECT 'northern_wilds_unsafe_datamapping_spawns' AS metric, COUNT(*) AS value
FROM entity
WHERE world = 426
  AND id BETWEEN @nf_entity_id_base AND @nf_entity_id_max
UNION ALL
SELECT 'northern_wilds_legacy_manual_spawns', COUNT(*)
FROM entity
WHERE world = 426
  AND id NOT BETWEEN @nf_entity_id_base AND @nf_entity_id_max;

SELECT id, creature, type, faction1, faction2
FROM entity
WHERE world = 3460
  AND creature IN (73494, 74862, 73665)
ORDER BY creature, id;

SELECT 'northern_wilds_enabled_jabbithole_creatures' AS metric, COUNT(DISTINCT c.id) AS value
FROM jabbithole.creatures c
WHERE c.zone_id = 1
  AND c.enabled = 'true'
UNION ALL
SELECT 'northern_wilds_default_safe_mapped_creatures', COUNT(DISTINCT m.jabbithole_creature_id)
FROM nf_map_creature m
JOIN jabbithole.creatures c ON c.id = m.jabbithole_creature_id
WHERE c.zone_id = 1
  AND c.enabled = 'true'
  AND m.match_status IN ('unique_name', 'scored_name', 'reviewed')
  AND IFNULL(m.creature2_id, 0) > 0
UNION ALL
SELECT 'northern_wilds_resolved_creature_bridges', COUNT(DISTINCT c.id)
FROM jabbithole.creatures c
JOIN nf_map_creature m ON m.jabbithole_creature_id = c.id
WHERE c.zone_id = 1
  AND c.enabled = 'true'
  AND IFNULL(m.creature2_id, 0) > 0
UNION ALL
SELECT 'northern_wilds_runtime_creatures', COUNT(DISTINCT c.id)
FROM jabbithole.creatures c
JOIN nf_map_creature m ON m.jabbithole_creature_id = c.id
JOIN entity e ON e.world = 426 AND e.creature = m.creature2_id
WHERE c.zone_id = 1
  AND c.enabled = 'true'
  AND IFNULL(m.creature2_id, 0) > 0
UNION ALL
SELECT 'northern_wilds_runtime_missing_creatures', COUNT(DISTINCT c.id)
FROM jabbithole.creatures c
LEFT JOIN nf_map_creature m ON m.jabbithole_creature_id = c.id
LEFT JOIN entity e ON e.world = 426 AND e.creature = m.creature2_id
WHERE c.zone_id = 1
  AND c.enabled = 'true'
  AND e.id IS NULL
UNION ALL
SELECT 'northern_wilds_unmatched_creature_bridges', COUNT(DISTINCT c.id)
FROM jabbithole.creatures c
LEFT JOIN nf_map_creature m ON m.jabbithole_creature_id = c.id
WHERE c.zone_id = 1
  AND c.enabled = 'true'
  AND IFNULL(m.creature2_id, 0) = 0;
