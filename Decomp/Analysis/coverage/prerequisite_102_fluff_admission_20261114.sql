-- Read-only imported-reference census. Run against wildstar_client.
-- Evidence tier: reference-data dependency reach, not observed casts or retail parity.
-- Native/shipped witnesses and field binding:
-- Decomp/Analysis/PREREQUISITE_102_FLUFF_ADMISSION_20261114.md.
SELECT ID, flags FROM spelleffecttype WHERE ID = 17;

WITH slots AS (
    SELECT ID AS prerequisite_id, 0 AS slot_index, objectId0 AS effect_id
    FROM prerequisite WHERE prerequisiteTypeId0 = 102
    UNION ALL
    SELECT ID, 1, objectId1 FROM prerequisite WHERE prerequisiteTypeId1 = 102
    UNION ALL
    SELECT ID, 2, objectId2 FROM prerequisite WHERE prerequisiteTypeId2 = 102
), resolved AS (
    SELECT s.prerequisite_id, s.slot_index, e.ID AS effect_id,
        e.spellId, e.flags, e.delayTime, e.tickTime, e.durationTime,
        b.ID AS base_id, b.castMethod
    FROM slots s
    JOIN spell4effects e ON e.ID = s.effect_id AND e.effectType = 17
    LEFT JOIN spell4 sp ON sp.ID = e.spellId
    LEFT JOIN spell4base b ON b.ID = sp.spell4BaseIdBaseSpell
)
SELECT castMethod,
    CASE WHEN (flags & 2) <> 0 THEN 'flag-2'
         WHEN durationTime > 0 THEN 'finite'
         ELSE 'zero-duration' END AS row_shape,
    COUNT(*) AS prerequisite_slots,
    COUNT(DISTINCT effect_id) AS effect_rows
FROM resolved
GROUP BY castMethod, row_shape
ORDER BY castMethod, row_shape;

-- Anchors, including the missing-parent control and a delayed finite row.
SELECT e.ID AS effect_id, e.spellId, e.flags, e.delayTime, e.tickTime,
    e.durationTime, b.ID AS base_id, b.castMethod, e.dataBits00, e.dataBits01
FROM spell4effects e
LEFT JOIN spell4 sp ON sp.ID = e.spellId
LEFT JOIN spell4base b ON b.ID = sp.spell4BaseIdBaseSpell
WHERE e.ID IN (36886, 65808, 44148, 200070, 103206)
ORDER BY e.ID;
