-- Type 102: read-only imported-reference census, 2026-11-14.
-- Run against wildstar_client with the configured MySQL client; no runtime writes.
-- Evidence tier: reference data / dependency reach, not affected casts or retail parity.
-- Direct counts each table-row/column once; weighted counts every matching slot.
-- The historical 352 is reproduced by the full weighted count. Excluding the
-- two effect-target columns reproduces the earlier incomplete 102/121 result;
-- why the original query returned that incomplete result is not established.
WITH slots AS (
    SELECT ID AS prerequisite_id, 0 AS slot_index, prerequisiteComparisonId0 AS comparison, objectId0 AS object_id, value0 AS operand_value FROM prerequisite WHERE prerequisiteTypeId0 = 102
    UNION ALL
    SELECT ID AS prerequisite_id, 1 AS slot_index, prerequisiteComparisonId1 AS comparison, objectId1 AS object_id, value1 AS operand_value FROM prerequisite WHERE prerequisiteTypeId1 = 102
    UNION ALL
    SELECT ID AS prerequisite_id, 2 AS slot_index, prerequisiteComparisonId2 AS comparison, objectId2 AS object_id, value2 AS operand_value FROM prerequisite WHERE prerequisiteTypeId2 = 102
), multiplicity AS (
    SELECT prerequisite_id, COUNT(*) AS slot_count FROM slots GROUP BY prerequisite_id
), refs AS (
    SELECT 'spell4' AS table_name, 'prerequisiteIdRunner00' AS column_name, ID AS source_id, prerequisiteIdRunner00 AS prerequisite_id FROM spell4 WHERE prerequisiteIdRunner00 <> 0
    UNION ALL
    SELECT 'spell4' AS table_name, 'prerequisiteIdRunner01' AS column_name, ID AS source_id, prerequisiteIdRunner01 AS prerequisite_id FROM spell4 WHERE prerequisiteIdRunner01 <> 0
    UNION ALL
    SELECT 'spell4' AS table_name, 'prerequisiteIdCasterCast' AS column_name, ID AS source_id, prerequisiteIdCasterCast AS prerequisite_id FROM spell4 WHERE prerequisiteIdCasterCast <> 0
    UNION ALL
    SELECT 'spell4' AS table_name, 'prerequisiteIdTargetCast' AS column_name, ID AS source_id, prerequisiteIdTargetCast AS prerequisite_id FROM spell4 WHERE prerequisiteIdTargetCast <> 0
    UNION ALL
    SELECT 'spell4' AS table_name, 'prerequisiteIdCasterPersistence' AS column_name, ID AS source_id, prerequisiteIdCasterPersistence AS prerequisite_id FROM spell4 WHERE prerequisiteIdCasterPersistence <> 0
    UNION ALL
    SELECT 'spell4' AS table_name, 'prerequisiteIdTargetPersistence' AS column_name, ID AS source_id, prerequisiteIdTargetPersistence AS prerequisite_id FROM spell4 WHERE prerequisiteIdTargetPersistence <> 0
    UNION ALL
    SELECT 'spell4' AS table_name, 'prerequisiteIdAoeTarget' AS column_name, ID AS source_id, prerequisiteIdAoeTarget AS prerequisite_id FROM spell4 WHERE prerequisiteIdAoeTarget <> 0
    UNION ALL
    SELECT 'spell4' AS table_name, 'prerequisiteIdAoePreferredTarget' AS column_name, ID AS source_id, prerequisiteIdAoePreferredTarget AS prerequisite_id FROM spell4 WHERE prerequisiteIdAoePreferredTarget <> 0
    UNION ALL
    SELECT 'spell4effects' AS table_name, 'prerequisiteIdCasterApply' AS column_name, ID AS source_id, prerequisiteIdCasterApply AS prerequisite_id FROM spell4effects WHERE prerequisiteIdCasterApply <> 0
    UNION ALL
    SELECT 'spell4effects' AS table_name, 'prerequisiteIdTargetApply' AS column_name, ID AS source_id, prerequisiteIdTargetApply AS prerequisite_id FROM spell4effects WHERE prerequisiteIdTargetApply <> 0
    UNION ALL
    SELECT 'spell4effects' AS table_name, 'prerequisiteIdCasterPersistence' AS column_name, ID AS source_id, prerequisiteIdCasterPersistence AS prerequisite_id FROM spell4effects WHERE prerequisiteIdCasterPersistence <> 0
    UNION ALL
    SELECT 'spell4effects' AS table_name, 'prerequisiteIdTargetPersistence' AS column_name, ID AS source_id, prerequisiteIdTargetPersistence AS prerequisite_id FROM spell4effects WHERE prerequisiteIdTargetPersistence <> 0
    UNION ALL
    SELECT 'spell4effects' AS table_name, 'prerequisiteIdTargetSuspend' AS column_name, ID AS source_id, prerequisiteIdTargetSuspend AS prerequisite_id FROM spell4effects WHERE prerequisiteIdTargetSuspend <> 0
), reach AS (
    SELECT refs.*, multiplicity.slot_count FROM refs JOIN multiplicity USING (prerequisite_id)
)
SELECT 'ALL' AS scope, COUNT(*) AS direct_references, SUM(slot_count) AS slot_weighted_references,
       COUNT(DISTINCT table_name, source_id) AS distinct_source_rows,
       COUNT(DISTINCT prerequisite_id) AS referenced_prerequisite_rows,
       (SELECT COUNT(*) FROM slots) AS type_slots,
       (SELECT COUNT(*) FROM multiplicity) AS prerequisite_rows
FROM reach
UNION ALL
SELECT CONCAT(table_name, '.', column_name), COUNT(*), SUM(slot_count),
       COUNT(DISTINCT source_id), COUNT(DISTINCT prerequisite_id), NULL, NULL
FROM reach GROUP BY table_name, column_name
UNION ALL
SELECT 'EXCLUDING_EFFECT_TARGET_APPLY_AND_PERSISTENCE', COUNT(*), SUM(slot_count),
       COUNT(DISTINCT table_name, source_id), COUNT(DISTINCT prerequisite_id), NULL, NULL
FROM reach WHERE NOT (table_name = 'spell4effects' AND column_name IN
    ('prerequisiteIdTargetApply', 'prerequisiteIdTargetPersistence'))
ORDER BY scope;

WITH slots AS (
    SELECT ID AS prerequisite_id, 0 AS slot_index, prerequisiteComparisonId0 AS comparison, objectId0 AS object_id, value0 AS operand_value FROM prerequisite WHERE prerequisiteTypeId0 = 102
    UNION ALL
    SELECT ID AS prerequisite_id, 1 AS slot_index, prerequisiteComparisonId1 AS comparison, objectId1 AS object_id, value1 AS operand_value FROM prerequisite WHERE prerequisiteTypeId1 = 102
    UNION ALL
    SELECT ID AS prerequisite_id, 2 AS slot_index, prerequisiteComparisonId2 AS comparison, objectId2 AS object_id, value2 AS operand_value FROM prerequisite WHERE prerequisiteTypeId2 = 102
), multiplicity AS (
    SELECT prerequisite_id, COUNT(*) AS slot_count FROM slots GROUP BY prerequisite_id
), refs AS (
    SELECT 'spell4' AS table_name, 'prerequisiteIdRunner00' AS column_name, ID AS source_id, prerequisiteIdRunner00 AS prerequisite_id FROM spell4 WHERE prerequisiteIdRunner00 <> 0
    UNION ALL
    SELECT 'spell4' AS table_name, 'prerequisiteIdRunner01' AS column_name, ID AS source_id, prerequisiteIdRunner01 AS prerequisite_id FROM spell4 WHERE prerequisiteIdRunner01 <> 0
    UNION ALL
    SELECT 'spell4' AS table_name, 'prerequisiteIdCasterCast' AS column_name, ID AS source_id, prerequisiteIdCasterCast AS prerequisite_id FROM spell4 WHERE prerequisiteIdCasterCast <> 0
    UNION ALL
    SELECT 'spell4' AS table_name, 'prerequisiteIdTargetCast' AS column_name, ID AS source_id, prerequisiteIdTargetCast AS prerequisite_id FROM spell4 WHERE prerequisiteIdTargetCast <> 0
    UNION ALL
    SELECT 'spell4' AS table_name, 'prerequisiteIdCasterPersistence' AS column_name, ID AS source_id, prerequisiteIdCasterPersistence AS prerequisite_id FROM spell4 WHERE prerequisiteIdCasterPersistence <> 0
    UNION ALL
    SELECT 'spell4' AS table_name, 'prerequisiteIdTargetPersistence' AS column_name, ID AS source_id, prerequisiteIdTargetPersistence AS prerequisite_id FROM spell4 WHERE prerequisiteIdTargetPersistence <> 0
    UNION ALL
    SELECT 'spell4' AS table_name, 'prerequisiteIdAoeTarget' AS column_name, ID AS source_id, prerequisiteIdAoeTarget AS prerequisite_id FROM spell4 WHERE prerequisiteIdAoeTarget <> 0
    UNION ALL
    SELECT 'spell4' AS table_name, 'prerequisiteIdAoePreferredTarget' AS column_name, ID AS source_id, prerequisiteIdAoePreferredTarget AS prerequisite_id FROM spell4 WHERE prerequisiteIdAoePreferredTarget <> 0
    UNION ALL
    SELECT 'spell4effects' AS table_name, 'prerequisiteIdCasterApply' AS column_name, ID AS source_id, prerequisiteIdCasterApply AS prerequisite_id FROM spell4effects WHERE prerequisiteIdCasterApply <> 0
    UNION ALL
    SELECT 'spell4effects' AS table_name, 'prerequisiteIdTargetApply' AS column_name, ID AS source_id, prerequisiteIdTargetApply AS prerequisite_id FROM spell4effects WHERE prerequisiteIdTargetApply <> 0
    UNION ALL
    SELECT 'spell4effects' AS table_name, 'prerequisiteIdCasterPersistence' AS column_name, ID AS source_id, prerequisiteIdCasterPersistence AS prerequisite_id FROM spell4effects WHERE prerequisiteIdCasterPersistence <> 0
    UNION ALL
    SELECT 'spell4effects' AS table_name, 'prerequisiteIdTargetPersistence' AS column_name, ID AS source_id, prerequisiteIdTargetPersistence AS prerequisite_id FROM spell4effects WHERE prerequisiteIdTargetPersistence <> 0
    UNION ALL
    SELECT 'spell4effects' AS table_name, 'prerequisiteIdTargetSuspend' AS column_name, ID AS source_id, prerequisiteIdTargetSuspend AS prerequisite_id FROM spell4effects WHERE prerequisiteIdTargetSuspend <> 0
), reach AS (
    SELECT refs.*, multiplicity.slot_count FROM refs JOIN multiplicity USING (prerequisite_id)
)
SELECT table_name, column_name, source_id, prerequisite_id, slot_index, comparison, object_id, operand_value
FROM reach JOIN slots USING (prerequisite_id)
WHERE table_name = 'spell4' AND column_name IN ('prerequisiteIdCasterCast', 'prerequisiteIdTargetCast')
ORDER BY column_name, source_id, slot_index;
