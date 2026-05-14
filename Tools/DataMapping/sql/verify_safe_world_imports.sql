-- Quick verification for the safe mapped world-data import.

USE `nexus_forever_world`;

SELECT 'entity_vendor' AS table_name, COUNT(*) AS row_count FROM entity_vendor
UNION ALL SELECT 'entity_vendor_category', COUNT(*) FROM entity_vendor_category
UNION ALL SELECT 'entity_vendor_item', COUNT(*) FROM entity_vendor_item
UNION ALL SELECT 'creature_loot', COUNT(*) FROM creature_loot
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
SELECT 'safe_creature_rows', COUNT(*)
FROM nf_map_creature
WHERE match_status IN ('unique_name', 'scored_name', 'reviewed')
  AND IFNULL(creature2_id, 0) > 0;
