-- Reusable world spell context queries for nexus_spell_re.
--
-- Setup:
--   python Tools/SpellReverseEngineering/import_world_creature_context.py --world-db I:/GIT/NexusForever.WorldDatabase --database nexus_spell_re --user bankai --password bankai
--
-- Run an individual query by copying it into mysql, or use the helper views directly:
--   mysql -ubankai -pbankai --batch --raw --database=nexus_spell_re -e "SELECT * FROM world_nonplayer_effect_family_summary ORDER BY placement_effect_rows DESC LIMIT 20;"

-- 0. Source files with missing creature->spell coverage.
SELECT
  source_file,
  world_id,
  placements,
  creatures,
  creatures_with_spell_rows,
  creatures_without_spell_rows,
  placements_without_spell_rows
FROM world_creature_spell_coverage_summary
WHERE creatures_without_spell_rows > 0
ORDER BY placements_without_spell_rows DESC, source_file
LIMIT 30;

-- 0b. Placement rows missing creature->spell coverage.
SELECT
  world_id,
  source_file,
  creature_id,
  LEFT(creature_description, 120) AS creature,
  COUNT(*) AS placements_missing
FROM world_creature_spell_coverage_gaps
GROUP BY world_id, source_file, creature_id, creature_description
ORDER BY placements_missing DESC, world_id, creature_id
LIMIT 50;

-- 0c. Activate-spell coverage for placed entities.
SELECT
  world_id,
  source_file,
  creature_id,
  LEFT(creature_description, 120) AS creature,
  activate_slot,
  spell4_id,
  spell4_base_id,
  spell4_tier,
  LEFT(spell4_description, 120) AS spell,
  prerequisite_id_activate_spell,
  localized_text_id_activate_spell_text
FROM world_creature_activate_spell_context
ORDER BY world_id, creature_id, activate_slot
LIMIT 50;

-- 0d. Effect-family coverage for placed activate spells.
SELECT
  ac.world_id,
  ac.source_file,
  ac.creature_id,
  LEFT(ac.creature_description, 120) AS creature,
  ac.activate_slot,
  ac.spell4_id,
  LEFT(ac.spell4_description, 120) AS spell,
  GROUP_CONCAT(DISTINCT CONCAT(e.effectType, ':', et.effectName) ORDER BY e.effectType SEPARATOR ', ') AS effect_families
FROM world_creature_activate_spell_context ac
JOIN spell4effects e ON e.spellId = ac.spell4_id
LEFT JOIN spell_effect_type_names et ON et.effectType = e.effectType
GROUP BY
  ac.world_id,
  ac.source_file,
  ac.creature_id,
  ac.creature_description,
  ac.activate_slot,
  ac.spell4_id,
  ac.spell4_description
ORDER BY ac.world_id, ac.creature_id, ac.activate_slot
LIMIT 50;

-- 1. Effect-family coverage for placed NonPlayer entities.
SELECT *
FROM world_nonplayer_effect_family_summary
ORDER BY placement_effect_rows DESC
LIMIT 30;

-- 2. Broad placed NonPlayer candidates, with families collapsed per creature.
SELECT
    continent,
    zone,
    creature_id,
    LEFT(creature_description, 120) AS creature,
    placements,
    spells,
    placement_effect_rows,
    effect_families
FROM world_nonplayer_creature_family_summary
ORDER BY placements DESC, placement_effect_rows DESC
LIMIT 30;

-- 3. Ready-to-run placed NonPlayer spell candidates with family flags and commands.
SELECT
    continent,
    zone,
    creature_id,
    LEFT(creature_description, 90) AS creature,
    spell4_id,
    spell4_base_id,
    spell4_tier,
    LEFT(spell4_description, 100) AS spell,
    placements,
    placement_effect_rows,
    effect_families,
    inspect_command,
    cast_command
FROM world_runtime_spell_candidates
WHERE entity_type = 0
ORDER BY priority_score DESC, placements DESC, placement_effect_rows DESC
LIMIT 40;

-- 4. Multi-family candidates that exercise Damage + Proxy + UnitPropertyModifier.
SELECT
    wcsc.continent,
    wcsc.zone,
    wcsc.creature_id,
    LEFT(wcsc.creature_description, 120) AS creature,
    COUNT(DISTINCT wcsc.placement_id) AS placements,
    COUNT(DISTINCT wcsc.spell4_id) AS spells,
    SUM(e.effectType = 8) AS damage_rows,
    SUM(e.effectType IN (19, 25, 26, 94, 96)) AS proxy_rows,
    SUM(e.effectType = 11) AS property_rows,
    SUM(e.effectType = 4) AS cc_rows,
    SUM(e.effectType = 3) AS forced_move_rows
FROM world_creature_spell_context wcsc
JOIN spell4effects e ON e.spellId = wcsc.spell4_id
WHERE wcsc.entity_type = 0
GROUP BY wcsc.continent, wcsc.zone, wcsc.creature_id, wcsc.creature_description
HAVING damage_rows > 0
   AND proxy_rows > 0
   AND property_rows > 0
ORDER BY placements DESC, damage_rows + proxy_rows + property_rows DESC
LIMIT 30;

-- 5. Multi-family candidates that exercise Damage + Proxy.
SELECT
    wcsc.continent,
    wcsc.zone,
    wcsc.creature_id,
    LEFT(wcsc.creature_description, 120) AS creature,
    COUNT(DISTINCT wcsc.placement_id) AS placements,
    COUNT(DISTINCT wcsc.spell4_id) AS spells,
    SUM(e.effectType = 8) AS damage_rows,
    SUM(e.effectType IN (19, 25, 26, 94, 96)) AS proxy_rows,
    SUM(e.effectType = 4) AS cc_rows,
    SUM(e.effectType = 3) AS forced_move_rows
FROM world_creature_spell_context wcsc
JOIN spell4effects e ON e.spellId = wcsc.spell4_id
WHERE wcsc.entity_type = 0
GROUP BY wcsc.continent, wcsc.zone, wcsc.creature_id, wcsc.creature_description
HAVING damage_rows > 0
   AND proxy_rows > 0
ORDER BY placements DESC, damage_rows + proxy_rows DESC
LIMIT 30;

-- 6. Proxy chain rows, including chained Spell4 ids and timing.
SELECT
    continent,
    zone,
    creature_id,
    LEFT(creature_description, 90) AS creature,
    spell4_id,
    spell4_base_id,
    spell4_tier,
    LEFT(spell4_description, 100) AS spell,
    effectName,
    orderIndex,
    targetFlags,
    delayTime,
    tickTime,
    durationTime,
    chained_spell4_id,
    LEFT(chained_spell4_description, 100) AS chained,
    placements
FROM world_proxy_chain_context
WHERE entity_type = 0
ORDER BY placements DESC, spell4_id, orderIndex
LIMIT 50;

-- 7. UnitPropertyModifier rows with property names, duration, and persistence hooks.
SELECT
    continent,
    zone,
    creature_id,
    LEFT(creature_description, 90) AS creature,
    spell4_id,
    spell4_base_id,
    spell4_tier,
    LEFT(spell4_description, 100) AS spell,
    property_name,
    targetFlags,
    durationTime,
    prerequisiteIdCasterPersistence,
    prerequisiteIdTargetPersistence,
    placements
FROM world_unit_property_modifier_context
WHERE entity_type = 0
ORDER BY placements DESC, spell4_id, orderIndex
LIMIT 50;

-- 8. Damage/heal/transference-adjacent rows with coefficients, timing, and base spell ids.
SELECT
    continent,
    zone,
    creature_id,
    LEFT(creature_description, 90) AS creature,
    spell4_id,
    spell4_base_id,
    spell4_tier,
    LEFT(spell4_description, 100) AS spell,
    effectName,
    damageType,
    targetFlags,
    delayTime,
    tickTime,
    durationTime,
    base_value,
    parameterType00,
    parameterValue00,
    parameterType01,
    parameterValue01,
    parameterType02,
    parameterValue02,
    parameterType03,
    parameterValue03,
    placements
FROM world_damage_candidate_context
WHERE entity_type = 0
  AND (
      parameterType00 <> 0
      OR parameterType01 <> 0
      OR parameterType02 <> 0
      OR parameterType03 <> 0
      OR delayTime > 0
      OR tickTime > 0
      OR durationTime > 0
  )
ORDER BY placements DESC, spell4_id, spell4_effect_id
LIMIT 50;

-- 9. Duration-bounded tick scheduler candidates.
SELECT
    r.continent,
    r.zone,
    r.creature_id,
    LEFT(r.creature_description, 90) AS creature,
    r.spell4_id,
    r.spell4_base_id,
    r.spell4_tier,
    LEFT(r.spell4_description, 100) AS spell,
    e.effectName,
    e.delayTime,
    e.tickTime,
    e.durationTime,
    r.placements,
    r.inspect_command,
    r.cast_command
FROM world_runtime_spell_candidates r
JOIN world_damage_candidate_context e
  ON e.entity_type = r.entity_type
 AND e.creature_id = r.creature_id
 AND e.spell4_id = r.spell4_id
WHERE r.entity_type = 0
  AND e.tickTime > 0
  AND e.durationTime > 0
ORDER BY r.placements DESC, r.priority_score DESC, r.spell4_id
LIMIT 40;

-- 10. Placed heal candidates.
SELECT
    continent,
    zone,
    creature_id,
    LEFT(creature_description, 90) AS creature,
    spell4_id,
    spell4_base_id,
    spell4_tier,
    LEFT(spell4_description, 100) AS spell,
    effectName,
    targetFlags,
    delayTime,
    tickTime,
    durationTime,
    base_value,
    parameterType00,
    parameterValue00,
    placements,
    CONCAT('/spell inspect4 ', spell4_id) AS inspect_command,
    CONCAT('/spell cast4 ', spell4_id) AS cast_command
FROM world_damage_candidate_context
WHERE entity_type = 0
  AND effectType = 10
ORDER BY placements DESC, durationTime DESC, spell4_id
LIMIT 40;

-- 11. Placed CCStateSet candidates.
SELECT
    wcsc.continent,
    wcsc.zone,
    wcsc.creature_id,
    LEFT(wcsc.creature_description, 90) AS creature,
    wcsc.spell4_id,
    wcsc.spell4_base_id,
    wcsc.spell4_tier,
    LEFT(wcsc.spell4_description, 100) AS spell,
    e.ID AS spell4_effect_id,
    e.dataBits00 AS cc_state,
    e.dataBits03 AS apply_rules,
    e.dataBits07 AS cc_additional_data_id,
    e.targetFlags,
    e.delayTime,
    e.tickTime,
    e.durationTime,
    COUNT(DISTINCT wcsc.placement_id) AS placements,
    CONCAT('/spell inspect4 ', wcsc.spell4_id) AS inspect_command,
    CONCAT('/spell cast4 ', wcsc.spell4_id) AS cast_command
FROM world_creature_spell_context wcsc
JOIN spell4effects e
  ON e.spellId = wcsc.spell4_id
WHERE wcsc.entity_type = 0
  AND e.effectType = 4
GROUP BY
    wcsc.continent,
    wcsc.zone,
    wcsc.creature_id,
    wcsc.creature_description,
    wcsc.spell4_id,
    wcsc.spell4_base_id,
    wcsc.spell4_tier,
    wcsc.spell4_description,
    e.ID,
    e.dataBits00,
    e.dataBits03,
    e.dataBits07,
    e.targetFlags,
    e.delayTime,
    e.tickTime,
    e.durationTime
ORDER BY placements DESC, wcsc.spell4_id
LIMIT 40;

-- 12. Placed ForcedMove candidates.
SELECT
    wcsc.continent,
    wcsc.zone,
    wcsc.creature_id,
    LEFT(wcsc.creature_description, 90) AS creature,
    wcsc.spell4_id,
    wcsc.spell4_base_id,
    wcsc.spell4_tier,
    LEFT(wcsc.spell4_description, 100) AS spell,
    e.ID AS spell4_effect_id,
    e.dataBits00 AS movement_type,
    e.dataBits01 AS data_bits_01,
    e.dataBits02 AS data_bits_02,
    e.dataBits03 AS movement_time,
    e.dataBits05 AS flags,
    e.dataBits06 AS data_bits_06,
    e.dataBits07 AS data_bits_07,
    e.dataBits08 AS data_bits_08,
    e.targetFlags,
    e.delayTime,
    e.durationTime,
    COUNT(DISTINCT wcsc.placement_id) AS placements,
    CONCAT('/spell inspect4 ', wcsc.spell4_id) AS inspect_command,
    CONCAT('/spell cast4 ', wcsc.spell4_id) AS cast_command
FROM world_creature_spell_context wcsc
JOIN spell4effects e
  ON e.spellId = wcsc.spell4_id
WHERE wcsc.entity_type = 0
  AND e.effectType = 3
GROUP BY
    wcsc.continent,
    wcsc.zone,
    wcsc.creature_id,
    wcsc.creature_description,
    wcsc.spell4_id,
    wcsc.spell4_base_id,
    wcsc.spell4_tier,
    wcsc.spell4_description,
    e.ID,
    e.dataBits00,
    e.dataBits01,
    e.dataBits02,
    e.dataBits03,
    e.dataBits05,
    e.dataBits06,
    e.dataBits07,
    e.dataBits08,
    e.targetFlags,
    e.delayTime,
    e.durationTime
ORDER BY placements DESC, wcsc.spell4_id
LIMIT 40;

-- 13. Global CCStateBreak candidates. Current placed creature context has no direct rows.
SELECT
    e.spellId AS spell4_id,
    s.spell4BaseIdBaseSpell AS spell4_base_id,
    s.tierIndex AS spell4_tier,
    LEFT(s.description, 120) AS spell,
    e.ID AS spell4_effect_id,
    e.dataBits00 AS cc_state_mask,
    e.dataBits01,
    e.dataBits02,
    e.dataBits03,
    e.dataBits04,
    e.dataBits05,
    e.targetFlags,
    e.delayTime,
    CONCAT('/spell inspect4 ', e.spellId) AS inspect_command,
    CONCAT('/spell cast4 ', e.spellId) AS cast_command
FROM spell4effects e
JOIN spell4 s
  ON s.ID = e.spellId
WHERE e.effectType = 27
ORDER BY e.dataBits00 DESC, e.spellId
LIMIT 80;

-- 14. Placed SpellForceRemove candidates.
SELECT
    wcsc.continent,
    wcsc.zone,
    wcsc.creature_id,
    LEFT(wcsc.creature_description, 90) AS creature,
    wcsc.spell4_id,
    wcsc.spell4_base_id,
    wcsc.spell4_tier,
    LEFT(wcsc.spell4_description, 100) AS spell,
    e.ID AS spell4_effect_id,
    e.effectType,
    e.dataBits00 AS remove_type,
    e.dataBits01 AS remove_id,
    e.dataBits02,
    e.dataBits03,
    e.dataBits04,
    e.dataBits05,
    e.dataBits06,
    e.targetFlags,
    e.delayTime,
    e.tickTime,
    e.durationTime,
    COUNT(DISTINCT wcsc.placement_id) AS placements,
    CONCAT('/spell inspect4 ', wcsc.spell4_id) AS inspect_command,
    CONCAT('/spell cast4 ', wcsc.spell4_id) AS cast_command
FROM world_creature_spell_context wcsc
JOIN spell4effects e
  ON e.spellId = wcsc.spell4_id
WHERE wcsc.entity_type = 0
  AND e.effectType IN (51, 80)
GROUP BY
    wcsc.continent,
    wcsc.zone,
    wcsc.creature_id,
    wcsc.creature_description,
    wcsc.spell4_id,
    wcsc.spell4_base_id,
    wcsc.spell4_tier,
    wcsc.spell4_description,
    e.ID,
    e.effectType,
    e.dataBits00,
    e.dataBits01,
    e.dataBits02,
    e.dataBits03,
    e.dataBits04,
    e.dataBits05,
    e.dataBits06,
    e.targetFlags,
    e.delayTime,
    e.tickTime,
    e.durationTime
ORDER BY placements DESC, wcsc.spell4_id
LIMIT 80;

-- 15. Placed AOE target constraint candidates.
SELECT
    wcsc.continent,
    wcsc.zone,
    wcsc.creature_id,
    LEFT(wcsc.creature_description, 90) AS creature,
    wcsc.spell4_id,
    wcsc.spell4_base_id,
    wcsc.spell4_tier,
    LEFT(wcsc.spell4_description, 100) AS spell,
    a.targetCount,
    a.minRange,
    a.maxRange,
    a.angle,
    a.targetSelection,
    COUNT(DISTINCT wcsc.placement_id) AS placements,
    CONCAT('/spell inspect4 ', wcsc.spell4_id) AS inspect_command,
    CONCAT('/spell cast4 ', wcsc.spell4_id) AS cast_command
FROM world_creature_spell_context wcsc
JOIN spell4 s
  ON s.ID = wcsc.spell4_id
JOIN spell4aoetargetconstraints a
  ON a.ID = s.spell4AoeTargetConstraintsId
WHERE wcsc.entity_type = 0
GROUP BY
    wcsc.continent,
    wcsc.zone,
    wcsc.creature_id,
    wcsc.creature_description,
    wcsc.spell4_id,
    wcsc.spell4_base_id,
    wcsc.spell4_tier,
    wcsc.spell4_description,
    a.targetCount,
    a.minRange,
    a.maxRange,
    a.angle,
    a.targetSelection
ORDER BY placements DESC, wcsc.spell4_id
LIMIT 80;

-- 15b. Global smart AOE target selection witnesses.
-- targetSelection 4 and 5 have named test rows in client data:
-- 4 = lowest absolute health, 5 = missing the most health.
SELECT
    s.ID AS spell4_id,
    s.spell4BaseIdBaseSpell AS spell4_base_id,
    s.tierIndex AS spell4_tier,
    LEFT(s.description, 120) AS spell,
    a.ID AS aoe_target_constraints_id,
    a.targetSelection,
    a.targetCount,
    a.minRange,
    a.maxRange,
    a.angle,
    GROUP_CONCAT(DISTINCT CONCAT(e.effectType, '/', e.targetFlags) ORDER BY e.orderIndex SEPARATOR '; ') AS effects,
    CONCAT('/spell inspect4 ', s.ID) AS inspect_command,
    CONCAT('/spell cast4 ', s.ID) AS cast_command
FROM spell4 s
JOIN spell4aoetargetconstraints a
  ON a.ID = s.spell4AoeTargetConstraintsId
JOIN spell4effects e
  ON e.spellId = s.ID
WHERE a.targetSelection IN (4, 5)
GROUP BY
    s.ID,
    s.spell4BaseIdBaseSpell,
    s.tierIndex,
    s.description,
    a.ID,
    a.targetSelection,
    a.targetCount,
    a.minRange,
    a.maxRange,
    a.angle
ORDER BY a.targetSelection, s.ID
LIMIT 80;

-- 15c. Global valid-target dead/corpse bit witnesses.
SELECT
    vt.ID AS valid_target_id,
    vt.targetBitmask,
    s.ID AS spell4_id,
    s.spell4BaseIdBaseSpell AS spell4_base_id,
    s.tierIndex AS spell4_tier,
    LEFT(s.description, 120) AS spell,
    GROUP_CONCAT(DISTINCT CONCAT(e.effectType, '/', e.targetFlags) ORDER BY e.orderIndex SEPARATOR '; ') AS effects,
    CONCAT('/spell inspect4 ', s.ID) AS inspect_command,
    CONCAT('/spell cast4 ', s.ID) AS cast_command
FROM spell4 s
JOIN spell4base b
  ON b.ID = s.spell4BaseIdBaseSpell
JOIN spell4validtargets vt
  ON vt.ID = b.spell4ValidTargetId
JOIN spell4effects e
  ON e.spellId = s.ID
WHERE (vt.targetBitmask & 8) <> 0
GROUP BY
    vt.ID,
    vt.targetBitmask,
    s.ID,
    s.spell4BaseIdBaseSpell,
    s.tierIndex,
    s.description
ORDER BY vt.targetBitmask, s.ID
LIMIT 80;

-- 15d. Global explicit target-angle witnesses.
SELECT
    ta.ID AS target_angle_id,
    ta.targetAngle,
    s.ID AS spell4_id,
    s.spell4BaseIdBaseSpell AS spell4_base_id,
    s.tierIndex AS spell4_tier,
    LEFT(s.description, 120) AS spell,
    tm.targetType,
    tm.flags AS target_mechanic_flags,
    CONCAT('/spell inspect4 ', s.ID) AS inspect_command,
    CONCAT('/spell cast4 ', s.ID) AS cast_command
FROM spell4 s
JOIN spell4base b
  ON b.ID = s.spell4BaseIdBaseSpell
JOIN spell4targetangle ta
  ON ta.ID = b.spell4TargetAngleId
JOIN spell4targetmechanics tm
  ON tm.ID = b.spell4TargetMechanicId
WHERE ta.targetAngle <> 0
ORDER BY ta.targetAngle, s.ID
LIMIT 80;

-- 16. Placed DespawnUnit candidates.
SELECT
    wcsc.continent,
    wcsc.zone,
    wcsc.creature_id,
    LEFT(wcsc.creature_description, 90) AS creature,
    wcsc.spell4_id,
    wcsc.spell4_base_id,
    wcsc.spell4_tier,
    LEFT(wcsc.spell4_description, 100) AS spell,
    e.ID AS spell4_effect_id,
    e.dataBits00,
    e.dataBits01,
    e.dataBits02,
    e.dataBits03,
    e.dataBits04,
    e.targetFlags,
    e.delayTime,
    e.tickTime,
    e.durationTime,
    COUNT(DISTINCT wcsc.placement_id) AS placements,
    CONCAT('/spell inspect4 ', wcsc.spell4_id) AS inspect_command,
    CONCAT('/spell cast4 ', wcsc.spell4_id) AS cast_command
FROM world_creature_spell_context wcsc
JOIN spell4effects e
  ON e.spellId = wcsc.spell4_id
WHERE wcsc.entity_type = 0
  AND e.effectType = 97
GROUP BY
    wcsc.continent,
    wcsc.zone,
    wcsc.creature_id,
    wcsc.creature_description,
    wcsc.spell4_id,
    wcsc.spell4_base_id,
    wcsc.spell4_tier,
    wcsc.spell4_description,
    e.ID,
    e.dataBits00,
    e.dataBits01,
    e.dataBits02,
    e.dataBits03,
    e.dataBits04,
    e.targetFlags,
    e.delayTime,
    e.tickTime,
    e.durationTime
ORDER BY placements DESC, wcsc.spell4_id
LIMIT 80;

-- 17. Global Activate candidates.
-- Current placed NonPlayer context has no Activate rows, so this uses global spell data.
SELECT
    s.ID AS spell4_id,
    s.spell4BaseIdBaseSpell AS spell4_base_id,
    s.tierIndex AS spell4_tier,
    LEFT(s.description, 100) AS spell,
    e.ID AS spell4_effect_id,
    e.dataBits00,
    e.dataBits01,
    e.dataBits02,
    e.dataBits03,
    e.dataBits04,
    e.dataBits05,
    e.targetFlags,
    e.delayTime,
    e.tickTime,
    e.durationTime,
    CONCAT('/spell inspect4 ', s.ID) AS inspect_command,
    CONCAT('/spell cast4 ', s.ID) AS cast_command
FROM spell4 s
JOIN spell4effects e
  ON e.spellId = s.ID
WHERE e.effectType = 7
ORDER BY
    (e.dataBits00 <> 0 OR e.dataBits01 <> 0 OR e.dataBits02 <> 0 OR
     e.dataBits03 <> 0 OR e.dataBits04 <> 0 OR e.dataBits05 <> 0) DESC,
    s.ID
LIMIT 120;

-- 18. Global state-toggle candidates: AggroImmune, Stealth, RemoveStealth.
SELECT
    e.effectType,
    n.effectName,
    s.ID AS spell4_id,
    s.spell4BaseIdBaseSpell AS spell4_base_id,
    s.tierIndex AS spell4_tier,
    LEFT(s.description, 100) AS spell,
    e.ID AS spell4_effect_id,
    e.dataBits00,
    e.dataBits01,
    e.dataBits02,
    e.dataBits03,
    e.dataBits04,
    e.dataBits05,
    e.targetFlags,
    e.delayTime,
    e.tickTime,
    e.durationTime,
    CONCAT('/spell inspect4 ', s.ID) AS inspect_command,
    CONCAT('/spell cast4 ', s.ID) AS cast_command
FROM spell4 s
JOIN spell4effects e
  ON e.spellId = s.ID
LEFT JOIN spell_effect_type_names n
  ON n.effectType = e.effectType
WHERE e.effectType IN (77, 83, 84)
ORDER BY e.effectType, e.durationTime DESC, s.ID
LIMIT 160;

-- 19. Global VitalModifier candidates.
-- Placed NonPlayer context has a small direct vital surface, so this uses global spell data.
SELECT
    s.ID AS spell4_id,
    s.spell4BaseIdBaseSpell AS spell4_base_id,
    s.tierIndex AS spell4_tier,
    LEFT(s.description, 120) AS spell,
    e.ID AS spell4_effect_id,
    e.dataBits00 AS vital_id,
    e.dataBits01,
    e.dataBits02,
    e.dataBits03,
    e.dataBits04,
    e.dataBits05,
    e.parameterType00,
    e.parameterValue00,
    e.parameterType01,
    e.parameterValue01,
    e.parameterType02,
    e.parameterValue02,
    e.parameterType03,
    e.parameterValue03,
    e.targetFlags,
    e.delayTime,
    e.tickTime,
    e.durationTime,
    CONCAT('/spell inspect4 ', s.ID) AS inspect_command,
    CONCAT('/spell cast4 ', s.ID) AS cast_command
FROM spell4 s
JOIN spell4effects e
  ON e.spellId = s.ID
WHERE e.effectType = 1
ORDER BY
    e.dataBits00,
    (e.dataBits01 = e.dataBits02 AND e.dataBits01 > 0 AND e.dataBits01 < 2147483647) DESC,
    (e.dataBits05 <> 0) DESC,
    (e.parameterType00 <> 0 OR e.parameterType01 <> 0 OR e.parameterType02 <> 0 OR e.parameterType03 <> 0) DESC,
    (e.dataBits01 = 2147483647 OR e.dataBits02 = 2147483647 OR e.dataBits05 = 2147483647) DESC,
    s.ID
LIMIT 180;

-- 20. Global SummonCreature candidates.
-- DataBits00 is the summoned Creature2 id. Duration-heavy rows are the clearest
-- fixtures for entity creation plus timed cleanup.
SELECT
    s.ID AS spell4_id,
    s.spell4BaseIdBaseSpell AS spell4_base_id,
    s.tierIndex AS spell4_tier,
    LEFT(s.description, 120) AS spell,
    e.ID AS spell4_effect_id,
    e.dataBits00 AS creature_id,
    e.dataBits01,
    e.dataBits02,
    e.dataBits03 AS placement_min_radius,
    e.dataBits04 AS placement_max_radius,
    e.dataBits05 AS placement_angle,
    e.dataBits06,
    e.dataBits07,
    e.dataBits08,
    e.dataBits09,
    e.targetFlags,
    e.delayTime,
    e.tickTime,
    e.durationTime,
    CONCAT('/spell inspect4 ', s.ID) AS inspect_command,
    CONCAT('/spell cast4 ', s.ID) AS cast_command
FROM spell4 s
JOIN spell4effects e
  ON e.spellId = s.ID
WHERE e.effectType = 21
ORDER BY
    (e.durationTime > 0) DESC,
    e.durationTime DESC,
    (e.dataBits08 <> 0) DESC,
    s.ID
LIMIT 180;

-- 21. Global NpcExecutionDelay candidates.
-- Mostly zero-payload duration rows. Use these to validate spell lifetime holds
-- and identify the minority of non-zero payload modes.
SELECT
    s.ID AS spell4_id,
    s.spell4BaseIdBaseSpell AS spell4_base_id,
    s.tierIndex AS spell4_tier,
    LEFT(s.description, 140) AS spell,
    e.ID AS spell4_effect_id,
    e.targetFlags,
    e.delayTime,
    e.tickTime,
    e.durationTime,
    e.dataBits00,
    e.dataBits01,
    e.dataBits02,
    e.dataBits03,
    e.dataBits04,
    e.dataBits05,
    e.dataBits06,
    e.dataBits07,
    e.dataBits08,
    e.dataBits09,
    CONCAT('/spell inspect4 ', s.ID) AS inspect_command,
    CONCAT('/spell cast4 ', s.ID) AS cast_command
FROM spell4 s
JOIN spell4effects e
  ON e.spellId = s.ID
WHERE e.effectType = 35
ORDER BY
    (e.dataBits00 <> 0 OR e.dataBits01 <> 0 OR e.dataBits02 <> 0 OR
     e.dataBits03 <> 0 OR e.dataBits04 <> 0 OR e.dataBits05 <> 0 OR
     e.dataBits06 <> 0 OR e.dataBits07 <> 0 OR e.dataBits08 <> 0 OR
     e.dataBits09 <> 0) DESC,
    e.durationTime DESC,
    e.delayTime DESC,
    s.ID
LIMIT 180;

-- 22. Global shield heal/damage candidates.
SELECT
    e.effectType,
    n.effectName,
    s.ID AS spell4_id,
    s.spell4BaseIdBaseSpell AS spell4_base_id,
    s.tierIndex AS spell4_tier,
    LEFT(s.description, 130) AS spell,
    e.ID AS spell4_effect_id,
    e.targetFlags,
    e.delayTime,
    e.tickTime,
    e.durationTime,
    e.dataBits00,
    e.dataBits01,
    e.parameterType00,
    e.parameterValue00,
    e.parameterType01,
    e.parameterValue01,
    e.parameterType02,
    e.parameterValue02,
    e.parameterType03,
    e.parameterValue03,
    CONCAT('/spell inspect4 ', s.ID) AS inspect_command,
    CONCAT('/spell cast4 ', s.ID) AS cast_command
FROM spell4 s
JOIN spell4effects e
  ON e.spellId = s.ID
LEFT JOIN spell_effect_type_names n
  ON n.effectType = e.effectType
WHERE e.effectType IN (118, 138)
ORDER BY
    e.effectType,
    (e.tickTime > 0 OR e.durationTime > 0) DESC,
    e.durationTime DESC,
    e.tickTime DESC,
    s.ID
LIMIT 180;

-- 23. Global ModifyInterruptArmor candidates.
-- DataBits00 behaves as the interrupt armor amount. durationTime supplies
-- temporary lifetime. DataBits01 distinguishes the named remove/no-remove
-- test pair but needs sniff validation before using it as runtime cleanup.
SELECT
    s.ID AS spell4_id,
    s.spell4BaseIdBaseSpell AS spell4_base_id,
    s.tierIndex AS spell4_tier,
    LEFT(s.description, 140) AS spell,
    e.ID AS spell4_effect_id,
    e.targetFlags,
    e.delayTime,
    e.tickTime,
    e.durationTime,
    e.dataBits00 AS interrupt_armor_amount,
    e.dataBits01 AS consume_or_remove_mode,
    e.dataBits02,
    e.dataBits03,
    e.dataBits04,
    e.dataBits05,
    CONCAT('/spell inspect4 ', s.ID) AS inspect_command,
    CONCAT('/spell cast4 ', s.ID) AS cast_command
FROM spell4 s
JOIN spell4effects e
  ON e.spellId = s.ID
WHERE e.effectType = 52
ORDER BY
    e.dataBits00 DESC,
    e.dataBits01 DESC,
    e.durationTime DESC,
    s.ID;

-- 24. Global Absorption candidates.
-- DataBits00/01 are float-like amount fields. DataBits04 appears to select
-- absorb category/damage type; current runtime records it but applies the pool
-- to all damage until sniff/client evidence confirms filtering.
SELECT
    s.ID AS spell4_id,
    s.spell4BaseIdBaseSpell AS spell4_base_id,
    s.tierIndex AS spell4_tier,
    LEFT(s.description, 140) AS spell,
    e.ID AS spell4_effect_id,
    e.targetFlags,
    e.delayTime,
    e.tickTime,
    e.durationTime,
    e.dataBits00,
    e.dataBits01,
    e.dataBits02,
    e.dataBits03,
    e.dataBits04 AS absorption_type,
    e.dataBits05,
    e.parameterType00,
    e.parameterValue00,
    e.parameterType01,
    e.parameterValue01,
    e.parameterType02,
    e.parameterValue02,
    e.parameterType03,
    e.parameterValue03,
    CONCAT('/spell inspect4 ', s.ID) AS inspect_command,
    CONCAT('/spell cast4 ', s.ID) AS cast_command
FROM spell4 s
JOIN spell4effects e
  ON e.spellId = s.ID
WHERE e.effectType = 29
ORDER BY
    e.durationTime DESC,
    (e.parameterType00 <> 0 OR e.parameterType01 <> 0 OR e.parameterType02 <> 0 OR e.parameterType03 <> 0) DESC,
    e.dataBits04,
    s.ID
LIMIT 220;

-- 25. Global ThreatModification and ThreatTransfer candidates.
-- ThreatModification mode evidence is name-driven: mode 0 add/reduce-like,
-- mode 1 detaunt/reduce, mode 2/6 drop or wipe, mode 4 set, mode 5 fixate.
-- ThreatTransfer is decoded but needs source/destination validation.
SELECT
    e.effectType,
    n.effectName,
    s.ID AS spell4_id,
    s.spell4BaseIdBaseSpell AS spell4_base_id,
    s.tierIndex AS spell4_tier,
    LEFT(s.description, 140) AS spell,
    e.ID AS spell4_effect_id,
    e.targetFlags,
    e.delayTime,
    e.tickTime,
    e.durationTime,
    e.dataBits00 AS mode,
    e.dataBits01 AS ratio_or_percent_bits,
    e.dataBits02,
    e.dataBits03 AS threat_value,
    e.dataBits04,
    e.dataBits05,
    CONCAT('/spell inspect4 ', s.ID) AS inspect_command,
    CONCAT('/spell cast4 ', s.ID) AS cast_command
FROM spell4 s
JOIN spell4effects e
  ON e.spellId = s.ID
LEFT JOIN spell_effect_type_names n
  ON n.effectType = e.effectType
WHERE e.effectType IN (46, 47)
ORDER BY
    e.effectType,
    e.dataBits00,
    e.durationTime DESC,
    e.dataBits03 DESC,
    s.ID
LIMIT 220;

-- 26. Global SpellDispel candidates.
-- DataBits03 strongly matches SpellClass values:
-- 36 BuffDispellable, 38 DebuffDispellable, and occasional 39 special-case
-- cleanse rows. DataBits00/01 are count-like; DataBits05 looks priority-like.
SELECT
    s.ID AS spell4_id,
    s.spell4BaseIdBaseSpell AS spell4_base_id,
    s.tierIndex AS spell4_tier,
    LEFT(s.description, 140) AS spell,
    e.ID AS spell4_effect_id,
    e.targetFlags,
    e.delayTime,
    e.tickTime,
    e.durationTime,
    e.dataBits00 AS count_a,
    e.dataBits01 AS count_b,
    e.dataBits02,
    e.dataBits03 AS spell_class,
    e.dataBits04,
    e.dataBits05 AS priority_or_category,
    CONCAT('/spell inspect4 ', s.ID) AS inspect_command,
    CONCAT('/spell cast4 ', s.ID) AS cast_command
FROM spell4 s
JOIN spell4effects e
  ON e.spellId = s.ID
WHERE e.effectType = 53
ORDER BY
    e.dataBits03,
    e.dataBits05 DESC,
    e.dataBits00 DESC,
    s.ID
LIMIT 220;

-- 27. Cooldown and ability charge candidates.
-- CooldownReset has many zero-payload reset-all rows and a few targeted
-- payload rows where DataBits01 resolves to a concrete Spell4 id.
-- ModifyAbilityCharges uses DataBits00 as the target Spell4 id, DataBits01
-- as count, and DataBits02 as the current add/set mode hypothesis.
SELECT
    e.effectType,
    CASE e.effectType
        WHEN 73 THEN 'ModifySpellCooldown'
        WHEN 90 THEN 'ModifyAbilityCharges'
        WHEN 108 THEN 'CooldownReset'
        WHEN 146 THEN 'ActivateSpellCooldown'
        ELSE CONCAT('EffectType ', e.effectType)
    END AS effect_name,
    s.ID AS spell4_id,
    s.spell4BaseIdBaseSpell AS spell4_base_id,
    s.tierIndex AS spell4_tier,
    LEFT(s.description, 140) AS spell,
    e.ID AS spell4_effect_id,
    e.targetFlags,
    e.delayTime,
    e.tickTime,
    e.durationTime,
    e.dataBits00,
    e.dataBits01,
    e.dataBits02,
    e.dataBits03,
    e.dataBits04,
    e.dataBits05,
    target0.spell4BaseIdBaseSpell AS dataBits00_spell4_base_id,
    target1.spell4BaseIdBaseSpell AS dataBits01_spell4_base_id,
    CONCAT('/spell inspect4 ', s.ID) AS inspect_command,
    CONCAT('/spell cast4 ', s.ID) AS cast_command
FROM spell4 s
JOIN spell4effects e
  ON e.spellId = s.ID
LEFT JOIN spell4 target0
  ON target0.ID = e.dataBits00
LEFT JOIN spell4 target1
  ON target1.ID = e.dataBits01
WHERE e.effectType IN (73, 90, 108, 146)
ORDER BY
    e.effectType,
    e.dataBits02,
    e.dataBits00,
    e.dataBits01,
    s.ID
LIMIT 220;

-- 28. Action bar, immunity, scale, faction, visual, disguise, disembark, quest, reward, and small utility families.
-- These are conservative handlers backed by strong obvious payloads:
-- SpellEffectImmunity.DataBits00 = blocked effect type,
-- ActionBarSet.DataBits00 = ActionBarShortcutSet id,
-- Scale.DataBits00 = target scale float bits, DataBits01/02 = transition ms,
-- FactionSet.DataBits00 = raw Faction2 id,
-- ItemVisualSwap.DataBits00 = raw visual slot, DataBits01 = display id, DataBits04/05 = colour/dye data,
-- DisguiseOutfit.DataBits00 = Creature2OutfitInfo id, DataBits01 = primary ItemDisplay id, DataBits02 = secondary ItemDisplay id or flag,
-- Disembark.DataBits00 = dismount/eject mode,
-- QuestAdvanceObjective.DataBits00 = objective id, DataBits02 = progress,
-- AchievementAdvance.DataBits00 = achievement id,
-- ReputationModify.DataBits00 = faction id, DataBits01 = float reputation delta,
-- GiveItemToPlayer.DataBits00 = item id, DataBits01 = count,
-- GiveSchematic.DataBits00 = TradeskillSchematic2 id,
-- RewardPropertyModifier.DataBits00 = RewardPropertyType, DataBits01 = data, DataBits02/03 = value candidates,
-- AddSpell.DataBits00 = concrete Spell4 id, GrantXP.DataBits00 = flat XP.
SELECT
    e.effectType,
    CASE e.effectType
        WHEN 9 THEN 'FactionSet'
        WHEN 18 THEN 'Scale'
        WHEN 23 THEN 'ActionBarSet'
        WHEN 37 THEN 'ReputationModify'
        WHEN 38 THEN 'GiveSchematic'
        WHEN 42 THEN 'QuestAdvanceObjective'
        WHEN 43 THEN 'GiveItemToPlayer'
        WHEN 50 THEN 'RewardPropertyModifier'
        WHEN 57 THEN 'AddSpell'
        WHEN 76 THEN 'ItemVisualSwap'
        WHEN 79 THEN 'SpellEffectImmunity'
        WHEN 92 THEN 'AchievementAdvance'
        WHEN 101 THEN 'GrantXP'
        WHEN 112 THEN 'DisguiseOutfit'
        WHEN 132 THEN 'Disembark'
        WHEN 149 THEN 'Kill'
        ELSE CONCAT('EffectType ', e.effectType)
    END AS effect_name,
    s.ID AS spell4_id,
    s.spell4BaseIdBaseSpell AS spell4_base_id,
    s.tierIndex AS spell4_tier,
    LEFT(s.description, 140) AS spell,
    e.ID AS spell4_effect_id,
    e.targetFlags,
    e.delayTime,
    e.tickTime,
    e.durationTime,
    e.dataBits00,
    e.dataBits01,
    e.dataBits02,
    e.dataBits03,
    e.dataBits04,
    e.dataBits05,
    n0.effectName AS dataBits00_effect_type,
    target0.spell4BaseIdBaseSpell AS dataBits00_spell4_base_id,
    CONCAT('/spell inspect4 ', s.ID) AS inspect_command,
    CONCAT('/spell cast4 ', s.ID) AS cast_command
FROM spell4 s
JOIN spell4effects e
  ON e.spellId = s.ID
LEFT JOIN spell_effect_type_names n0
  ON n0.effectType = e.dataBits00
LEFT JOIN spell4 target0
  ON target0.ID = e.dataBits00
WHERE e.effectType IN (9, 18, 23, 37, 38, 42, 43, 50, 57, 76, 79, 92, 101, 112, 132, 149)
ORDER BY
    e.effectType,
    e.durationTime DESC,
    e.dataBits00,
    s.ID
LIMIT 360;

-- 29. Personal damage/heal modifier rows.
-- PersonalDmgHealMod.DataBits00 selector:
--   3/4/5 = outgoing physical/tech/magic damage multiplier,
--   6/7/8 = incoming physical/tech/magic damage multiplier,
--   12 = incoming healing multiplier,
--   13 = outgoing healing multiplier.
-- DataBits01 is preserved as priority and DataBits02 is the float multiplier.
SELECT
    s.ID AS spell4_id,
    s.spell4BaseIdBaseSpell AS spell4_base_id,
    s.tierIndex AS spell4_tier,
    LEFT(s.description, 140) AS spell,
    e.ID AS spell4_effect_id,
    e.targetFlags,
    e.delayTime,
    e.tickTime,
    e.durationTime,
    e.dataBits00 AS modifier_selector,
    CASE e.dataBits00
        WHEN 3 THEN 'OutgoingPhysicalDamage'
        WHEN 4 THEN 'OutgoingTechDamage'
        WHEN 5 THEN 'OutgoingMagicDamage'
        WHEN 6 THEN 'IncomingPhysicalDamage'
        WHEN 7 THEN 'IncomingTechDamage'
        WHEN 8 THEN 'IncomingMagicDamage'
        WHEN 12 THEN 'IncomingHealing'
        WHEN 13 THEN 'OutgoingHealing'
        ELSE 'Unsupported'
    END AS modifier_name,
    e.dataBits01 AS priority,
    e.dataBits02,
    e.dataBits03,
    e.dataBits04,
    e.dataBits05,
    CAST(e.dataBits02 AS UNSIGNED) AS dataBits02_raw,
    CONCAT('/spell inspect4 ', s.ID) AS inspect_command,
    CONCAT('/spell cast4 ', s.ID) AS cast_command
FROM spell4 s
JOIN spell4effects e
  ON e.spellId = s.ID
WHERE e.effectType = 151
ORDER BY
    e.durationTime DESC,
    e.dataBits00,
    s.ID
LIMIT 180;

-- 30. SummonVehicle rows.
-- SummonVehicle.DataBits00 = Creature2 id, DataBits01 = UnitVehicle id,
-- DataBits02 is treated conservatively as auto-board mode when non-zero.
SELECT
    s.ID AS spell4_id,
    s.spell4BaseIdBaseSpell AS spell4_base_id,
    s.tierIndex AS spell4_tier,
    LEFT(s.description, 140) AS spell,
    e.ID AS spell4_effect_id,
    e.targetFlags,
    e.dataBits00 AS creature2_id,
    LEFT(c.description, 120) AS creature,
    e.dataBits01 AS unit_vehicle_id,
    e.dataBits02 AS board_mode,
    e.dataBits03,
    e.dataBits04,
    e.dataBits05,
    e.delayTime,
    e.tickTime,
    e.durationTime,
    CONCAT('/spell inspect4 ', s.ID) AS inspect_command,
    CONCAT('/spell cast4 ', s.ID) AS cast_command
FROM spell4 s
JOIN spell4effects e
  ON e.spellId = s.ID
LEFT JOIN creature2 c
  ON c.ID = e.dataBits00
WHERE e.effectType = 6
ORDER BY
    s.ID,
    e.ID;

-- 31. HealingAbsorption rows.
-- HealingAbsorption.DataBits00/DataBits01 use the same float amount
-- shape as Absorption, DataBits02 is preserved as a mode flag, and
-- durationTime is the temporary anti-heal lifetime.
SELECT
    s.ID AS spell4_id,
    s.spell4BaseIdBaseSpell AS spell4_base_id,
    s.tierIndex AS spell4_tier,
    LEFT(s.description, 140) AS spell,
    e.ID AS spell4_effect_id,
    e.damageType,
    e.targetFlags,
    e.dataBits00 AS amount_multiplier_bits,
    e.dataBits01 AS amount_base_bits,
    e.dataBits02 AS mode,
    e.dataBits03,
    e.dataBits04,
    e.dataBits05,
    e.parameterType00,
    e.parameterValue00,
    e.parameterType01,
    e.parameterValue01,
    e.delayTime,
    e.tickTime,
    e.durationTime,
    CONCAT('/spell inspect4 ', s.ID) AS inspect_command,
    CONCAT('/spell cast4 ', s.ID) AS cast_command
FROM spell4 s
JOIN spell4effects e
  ON e.spellId = s.ID
WHERE e.effectType = 144
ORDER BY
    e.durationTime DESC,
    s.ID,
    e.ID;

-- 32. SpellImmunity rows.
-- SpellImmunity.DataBits00 is a mode. Mode 0 maps DataBits01 to a
-- concrete blocked Spell4 id in the current runtime; modes 1 and 2
-- remain evidence-only category/class candidates.
SELECT
    s.ID AS spell4_id,
    s.spell4BaseIdBaseSpell AS spell4_base_id,
    s.tierIndex AS spell4_tier,
    LEFT(s.description, 140) AS spell,
    e.ID AS spell4_effect_id,
    e.targetFlags,
    e.dataBits00 AS immunity_mode,
    e.dataBits01 AS immunity_payload,
    blocked.ID AS blocked_spell4_id,
    blocked.spell4BaseIdBaseSpell AS blocked_spell4_base_id,
    LEFT(blocked.description, 140) AS blocked_spell,
    e.dataBits02,
    e.dataBits03,
    e.dataBits04,
    e.dataBits05,
    e.delayTime,
    e.tickTime,
    e.durationTime,
    CONCAT('/spell inspect4 ', s.ID) AS inspect_command,
    CONCAT('/spell cast4 ', s.ID) AS cast_command
FROM spell4 s
JOIN spell4effects e
  ON e.spellId = s.ID
LEFT JOIN spell4 blocked
  ON e.dataBits00 = 0
 AND blocked.ID = e.dataBits01
WHERE e.effectType = 32
ORDER BY
    e.dataBits00,
    e.durationTime DESC,
    s.ID,
    e.ID;

-- 33. MimicDisguise rows.
-- MimicDisguise rows are clone/hologram/player-mimic spells with
-- mostly zero/one payloads. Runtime currently copies caster display,
-- outfit, and item visuals onto the target clone.
SELECT
    s.ID AS spell4_id,
    s.spell4BaseIdBaseSpell AS spell4_base_id,
    s.tierIndex AS spell4_tier,
    LEFT(s.description, 140) AS spell,
    e.ID AS spell4_effect_id,
    e.targetFlags,
    e.dataBits00,
    e.dataBits01,
    e.dataBits02,
    e.dataBits03,
    e.dataBits04,
    e.dataBits05,
    e.delayTime,
    e.tickTime,
    e.durationTime,
    CONCAT('/spell inspect4 ', s.ID) AS inspect_command,
    CONCAT('/spell cast4 ', s.ID) AS cast_command
FROM spell4 s
JOIN spell4effects e
  ON e.spellId = s.ID
WHERE e.effectType = 100
ORDER BY
    e.durationTime DESC,
    s.ID,
    e.ID;

-- 34. SummonTrap rows.
-- SummonTrap.DataBits00 = Creature2 trap/probe id,
-- DataBits01 = trigger/follow-up Spell4 id, DataBits02 often arms time,
-- DataBits04 is float radius-like data, and durationTime is lifetime.
SELECT
    s.ID AS spell4_id,
    s.spell4BaseIdBaseSpell AS spell4_base_id,
    s.tierIndex AS spell4_tier,
    LEFT(s.description, 140) AS spell,
    e.ID AS spell4_effect_id,
    e.targetFlags,
    e.dataBits00 AS creature2_id,
    LEFT(c.description, 120) AS creature,
    e.dataBits01 AS trigger_spell4_id,
    triggerSpell.spell4BaseIdBaseSpell AS trigger_spell4_base_id,
    LEFT(triggerSpell.description, 140) AS trigger_spell,
    e.dataBits02 AS arm_time_ms,
    e.dataBits03,
    e.dataBits04 AS radius_bits,
    e.dataBits05,
    e.dataBits06,
    e.dataBits07,
    e.delayTime,
    e.tickTime,
    e.durationTime,
    CONCAT('/spell inspect4 ', s.ID) AS inspect_command,
    CONCAT('/spell cast4 ', s.ID) AS cast_command
FROM spell4 s
JOIN spell4effects e
  ON e.spellId = s.ID
LEFT JOIN creature2 c
  ON c.ID = e.dataBits00
LEFT JOIN spell4 triggerSpell
  ON triggerSpell.ID = e.dataBits01
WHERE e.effectType = 106
ORDER BY
    e.durationTime DESC,
    s.ID,
    e.ID;

-- 35. ForceFacing and NpcForceFacing rows.
-- ForceFacing has all-zero payloads and is resolved from spell target/position
-- context. NpcForceFacing.DataBits00 is a float-bitcast degree offset; common
-- decoded values are included here because MySQL does not reinterpret uint bits
-- as single-precision floats portably.
SELECT
    s.ID AS spell4_id,
    s.spell4BaseIdBaseSpell AS spell4_base_id,
    s.tierIndex AS spell4_tier,
    LEFT(s.description, 140) AS spell,
    e.ID AS spell4_effect_id,
    e.effectType,
    CASE e.effectType
        WHEN 28 THEN 'ForceFacing'
        WHEN 89 THEN 'NpcForceFacing'
        ELSE 'Unknown'
    END AS effect_name,
    e.targetFlags,
    e.delayTime,
    e.tickTime,
    e.durationTime,
    e.dataBits00 AS angle_bits,
    CASE e.dataBits00
        WHEN 1110704128 THEN 45.0
        WHEN 1123024896 THEN 120.0
        WHEN 1124532224 THEN 135.0
        WHEN 1127481344 THEN 180.0
        WHEN 1132920832 THEN 270.0
        WHEN 1135869952 THEN 360.0
        WHEN 3258187776 THEN -45.0
        WHEN 3266576384 THEN -90.0
        WHEN 3272015872 THEN -135.0
        ELSE NULL
    END AS decoded_angle_degrees,
    e.dataBits01,
    e.dataBits02,
    e.dataBits03 AS turn_duration_ms,
    e.dataBits04,
    e.dataBits05,
    e.dataBits06,
    e.dataBits07,
    e.dataBits08,
    e.dataBits09,
    CONCAT('/spell inspect4 ', s.ID) AS inspect_command,
    CONCAT('/spell cast4 ', s.ID) AS cast_command
FROM spell4 s
JOIN spell4effects e
  ON e.spellId = s.ID
WHERE e.effectType IN (28, 89)
ORDER BY
    e.effectType,
    s.ID,
    e.ID;

-- 36. DelayDeath rows.
-- DelayDeath.DataBits01 resolves to a trigger Spell4 id for nearly all rows,
-- DataBits02 is treated as trigger delay in milliseconds, and durationTime is
-- the active prevent-death window when populated.
SELECT
    s.ID AS spell4_id,
    s.spell4BaseIdBaseSpell AS spell4_base_id,
    s.tierIndex AS spell4_tier,
    LEFT(s.description, 140) AS spell,
    e.ID AS spell4_effect_id,
    e.targetFlags,
    e.delayTime,
    e.tickTime,
    e.durationTime,
    e.dataBits00 AS delay_death_mode,
    e.dataBits01 AS trigger_spell4_id,
    triggerSpell.spell4BaseIdBaseSpell AS trigger_spell4_base_id,
    LEFT(triggerSpell.description, 140) AS trigger_spell,
    e.dataBits02 AS trigger_delay_ms,
    e.dataBits03,
    e.dataBits04,
    e.dataBits05,
    e.dataBits06,
    e.dataBits07,
    e.dataBits08,
    e.dataBits09,
    CONCAT('/spell inspect4 ', s.ID) AS inspect_command,
    CONCAT('/spell cast4 ', s.ID) AS cast_command
FROM spell4 s
JOIN spell4effects e
  ON e.spellId = s.ID
LEFT JOIN spell4 triggerSpell
  ON triggerSpell.ID = e.dataBits01
WHERE e.effectType = 121
ORDER BY
    e.durationTime DESC,
    s.ID,
    e.ID;

-- 37. ClampVital rows.
-- ClampVital.DataBits02 is a float-bitcast ceiling ratio in the imported rows.
-- Current runtime treats these rows as conservative current-health caps until
-- DataBits00/DataBits01/DataBits04 mode semantics are validated.
SELECT
    s.ID AS spell4_id,
    s.spell4BaseIdBaseSpell AS spell4_base_id,
    s.tierIndex AS spell4_tier,
    LEFT(s.description, 140) AS spell,
    e.ID AS spell4_effect_id,
    e.targetFlags,
    e.delayTime,
    e.tickTime,
    e.durationTime,
    e.dataBits00 AS clamp_mode,
    e.dataBits01 AS vital_mode,
    e.dataBits02 AS ratio_bits,
    CASE e.dataBits02
        WHEN 1065353216 THEN '1'
        WHEN 1063675494 THEN '0.9'
        WHEN 1061997773 THEN '0.8'
        WHEN 1060320051 THEN '0.7'
        WHEN 1058642330 THEN '0.6'
        WHEN 1056964608 THEN '0.5'
        WHEN 1053609165 THEN '0.4'
        WHEN 1050253722 THEN '0.3'
        WHEN 1045220557 THEN '0.2'
        WHEN 1036831949 THEN '0.1'
        WHEN 1028443341 THEN '0.05'
        WHEN 1017370378 THEN '0.02'
        WHEN 1008981770 THEN '0.01'
        WHEN 981668463 THEN '0.001'
        ELSE NULL
    END AS decoded_ratio,
    e.dataBits03,
    e.dataBits04,
    e.dataBits05,
    e.dataBits06,
    e.dataBits07,
    e.dataBits08,
    e.dataBits09,
    CONCAT('/spell inspect4 ', s.ID) AS inspect_command,
    CONCAT('/spell cast4 ', s.ID) AS cast_command
FROM spell4 s
JOIN spell4effects e
  ON e.spellId = s.ID
WHERE e.effectType = 143
ORDER BY
    e.durationTime DESC,
    s.ID,
    e.ID;

-- 38. SapVital rows.
-- SapVital.DataBits00 maps to Vital, DataBits03 is the current mode field,
-- and clean DataBits01/DataBits02 float-bitcast payloads are treated as
-- conservative percent-of-max restore/drain amounts by the runtime.
SELECT
    s.ID AS spell4_id,
    s.spell4BaseIdBaseSpell AS spell4_base_id,
    s.tierIndex AS spell4_tier,
    LEFT(s.description, 140) AS spell,
    e.ID AS spell4_effect_id,
    e.targetFlags,
    e.damageType,
    e.delayTime,
    e.tickTime,
    e.durationTime,
    e.dataBits00 AS vital_id,
    e.dataBits01 AS amount_bits_01,
    CASE e.dataBits01
        WHEN 1048576000 THEN '0.25'
        WHEN 1056964608 THEN '0.5'
        WHEN 1065353216 THEN '1'
        WHEN 1069547520 THEN '1.5'
        WHEN 1077936128 THEN '3'
        WHEN 1082130432 THEN '4'
        WHEN 1084227584 THEN '5'
        WHEN 1092616192 THEN '10'
        WHEN 1120403456 THEN '100'
        WHEN 1137180672 THEN '400'
        ELSE NULL
    END AS decoded_amount_01,
    e.dataBits02 AS amount_bits_02,
    CASE e.dataBits02
        WHEN 1036831949 THEN '0.1'
        WHEN 1045220557 THEN '0.2'
        WHEN 1050253722 THEN '0.3'
        WHEN 1053609165 THEN '0.4'
        WHEN 1056964608 THEN '0.5'
        WHEN 1065353216 THEN '1'
        WHEN 1073741824 THEN '2'
        WHEN 1077936128 THEN '3'
        WHEN 1082130432 THEN '4'
        WHEN 1084227584 THEN '5'
        WHEN 1086324736 THEN '6'
        WHEN 1090519040 THEN '8'
        WHEN 1092616192 THEN '10'
        WHEN 1095237632 THEN '12.5'
        WHEN 1097859072 THEN '15'
        WHEN 1101004800 THEN '20'
        WHEN 1103626240 THEN '25'
        WHEN 1107558400 THEN '33'
        WHEN 1112014848 THEN '50'
        WHEN 1114636288 THEN '60'
        WHEN 1119092736 THEN '90'
        WHEN 1120272384 THEN '99'
        WHEN 1120403456 THEN '100'
        WHEN 1137180672 THEN '400'
        WHEN 1140457472 THEN '500'
        WHEN 1148846080 THEN '1000'
        WHEN 1176256512 THEN '10000'
        WHEN 3225419776 THEN '-3'
        WHEN 3233808384 THEN '-6'
        WHEN 3245342720 THEN '-15'
        WHEN 3248488448 THEN '-20'
        WHEN 3267887104 THEN '-100'
        ELSE NULL
    END AS decoded_amount_02,
    e.dataBits03 AS sap_mode,
    e.dataBits04,
    e.dataBits05,
    e.dataBits06,
    e.dataBits07,
    e.dataBits08,
    e.dataBits09,
    e.parameterType00,
    e.parameterValue00,
    e.parameterType01,
    e.parameterValue01,
    e.parameterType02,
    e.parameterValue02,
    e.parameterType03,
    e.parameterValue03,
    CONCAT('/spell inspect4 ', s.ID) AS inspect_command,
    CONCAT('/spell cast4 ', s.ID) AS cast_command
FROM spell4 s
JOIN spell4effects e
  ON e.spellId = s.ID
WHERE e.effectType = 30
ORDER BY
    e.dataBits00,
    e.dataBits03,
    s.ID,
    e.ID;

-- 39. ShieldOverload rows.
-- Dominant shape is all-zero payload plus durationTime. Runtime applies only
-- those simple rows as temporary shield shutdown; non-zero payload rows remain
-- diagnostic-only until sniff/client evidence names the mode fields.
SELECT
    s.ID AS spell4_id,
    s.spell4BaseIdBaseSpell AS spell4_base_id,
    s.tierIndex AS spell4_tier,
    LEFT(s.description, 140) AS spell,
    e.ID AS spell4_effect_id,
    e.targetFlags,
    e.damageType,
    e.delayTime,
    e.tickTime,
    e.durationTime,
    e.dataBits00,
    e.dataBits01,
    e.dataBits02,
    CASE e.dataBits02
        WHEN 1059481190 THEN '0.7'
        WHEN 1061997773 THEN '0.8'
        WHEN 1065353216 THEN '1'
        ELSE NULL
    END AS decoded_data_bits_02,
    e.dataBits03,
    e.dataBits04,
    e.dataBits05,
    e.dataBits06,
    e.dataBits07,
    e.dataBits08,
    e.dataBits09,
    CONCAT('/spell inspect4 ', s.ID) AS inspect_command,
    CONCAT('/spell cast4 ', s.ID) AS cast_command
FROM spell4 s
JOIN spell4effects e
  ON e.spellId = s.ID
WHERE e.effectType = 64
ORDER BY
    (e.dataBits00 = 0 AND e.dataBits01 = 0 AND e.dataBits02 = 0 AND e.dataBits03 = 0 AND e.dataBits04 = 0 AND e.dataBits05 = 0 AND e.dataBits06 = 0 AND e.dataBits07 = 0 AND e.dataBits08 = 0 AND e.dataBits09 = 0) DESC,
    e.durationTime DESC,
    s.ID,
    e.ID;

-- 40. UnitStateSet rows.
-- DataBits00 is the raw unit state id. Runtime now tracks non-zero state ids
-- by effect id and removes duration-backed states, but state-id-specific
-- combat behavior remains validation-only.
SELECT
    s.ID AS spell4_id,
    s.spell4BaseIdBaseSpell AS spell4_base_id,
    s.tierIndex AS spell4_tier,
    LEFT(s.description, 140) AS spell,
    e.ID AS spell4_effect_id,
    e.targetFlags,
    e.damageType,
    e.delayTime,
    e.tickTime,
    e.durationTime,
    e.dataBits00 AS unit_state_id,
    CASE e.dataBits00
        WHEN 1 THEN 'block_or_guard'
        WHEN 6 THEN 'invulnerability_or_frozen'
        WHEN 7 THEN 'all_spell_immunity_cluster'
        WHEN 8 THEN 'all_spell_immunity_cluster'
        WHEN 9 THEN 'all_spell_immunity_cluster'
        WHEN 16 THEN 'all_spell_immunity_cluster'
        WHEN 17 THEN 'all_spell_immunity_cluster'
        WHEN 18 THEN 'all_spell_immunity_cluster'
        WHEN 22 THEN 'barrier_or_invulnerability'
        WHEN 23 THEN 'burrow_or_ice_block'
        ELSE NULL
    END AS inferred_cluster,
    e.dataBits01,
    e.dataBits02,
    e.dataBits03,
    e.dataBits04,
    e.dataBits05,
    e.dataBits06,
    e.dataBits07,
    e.dataBits08,
    e.dataBits09,
    CONCAT('/spell inspect4 ', s.ID) AS inspect_command,
    CONCAT('/spell cast4 ', s.ID) AS cast_command
FROM spell4 s
JOIN spell4effects e
  ON e.spellId = s.ID
WHERE e.effectType = 15
ORDER BY
    e.dataBits00,
    e.durationTime DESC,
    s.ID,
    e.ID;

-- 41. SetBusy rows.
-- DataBits00 is the set/clear flag: 1 sets busy and 0 clears busy.
-- Many rows pair an immediate set with a delayed clear on the same Spell4 id.
SELECT
    s.ID AS spell4_id,
    s.spell4BaseIdBaseSpell AS spell4_base_id,
    s.tierIndex AS spell4_tier,
    LEFT(s.description, 140) AS spell,
    e.ID AS spell4_effect_id,
    e.orderIndex,
    e.targetFlags,
    e.damageType,
    e.delayTime,
    e.tickTime,
    e.durationTime,
    e.dataBits00 AS busy_flag,
    CASE e.dataBits00
        WHEN 0 THEN 'clear_busy'
        WHEN 1 THEN 'set_busy'
        ELSE 'unknown_busy_mode'
    END AS busy_mode,
    e.dataBits01 AS busy_context_id,
    e.dataBits02,
    e.dataBits03,
    e.dataBits04,
    e.dataBits05,
    e.dataBits06,
    e.dataBits07,
    e.dataBits08,
    e.dataBits09,
    CONCAT('/spell inspect4 ', s.ID) AS inspect_command,
    CONCAT('/spell cast4 ', s.ID) AS cast_command
FROM spell4 s
JOIN spell4effects e
  ON e.spellId = s.ID
WHERE e.effectType = 107
ORDER BY
    s.ID,
    e.orderIndex,
    e.ID;

-- 42. PathXpModify rows.
-- DataBits00 is the amount. DataBits01 is the runtime mode:
-- 0 = add raw path XP, 1 = add path levels through PathLevel thresholds.
SELECT
    s.ID AS spell4_id,
    s.spell4BaseIdBaseSpell AS spell4_base_id,
    s.tierIndex AS spell4_tier,
    LEFT(s.description, 140) AS spell,
    e.ID AS spell4_effect_id,
    e.orderIndex,
    e.targetFlags,
    e.delayTime,
    e.tickTime,
    e.durationTime,
    e.dataBits00 AS amount,
    e.dataBits01 AS mode,
    CASE e.dataBits01
        WHEN 0 THEN 'add_path_xp'
        WHEN 1 THEN 'add_path_levels'
        ELSE 'unknown_path_xp_mode'
    END AS interpreted_mode,
    e.dataBits02,
    e.dataBits03,
    e.dataBits04,
    e.dataBits05,
    CONCAT('/spell inspect4 ', s.ID) AS inspect_command,
    CONCAT('/spell cast4 ', s.ID) AS cast_command
FROM spell4 s
JOIN spell4effects e
  ON e.spellId = s.ID
WHERE e.effectType = 93
ORDER BY
    e.dataBits01,
    s.ID,
    e.ID;

-- 43. GrantLevelScaledXP rows.
-- DataBits00 is a float-bitcast percent of the current level XP span.
-- DataBits01 is the max-level cap, and DataBits02=1 is the supported
-- level-scaling mode observed in all rows.
SELECT
    s.ID AS spell4_id,
    s.spell4BaseIdBaseSpell AS spell4_base_id,
    s.tierIndex AS spell4_tier,
    LEFT(s.description, 140) AS spell,
    e.ID AS spell4_effect_id,
    e.orderIndex,
    e.targetFlags,
    e.delayTime,
    e.tickTime,
    e.durationTime,
    e.dataBits00 AS percent_raw,
    CASE e.dataBits00
        WHEN 1084227584 THEN '5'
        WHEN 1092091904 THEN '9.5'
        WHEN 1092616192 THEN '10'
        WHEN 1094189056 THEN '11.5'
        WHEN 1100218368 THEN '19'
        WHEN 1101004800 THEN '20'
        WHEN 1102315520 THEN '25'
        ELSE NULL
    END AS percent_of_level,
    e.dataBits01 AS max_level,
    e.dataBits02 AS mode,
    e.dataBits03,
    e.dataBits04,
    e.dataBits05,
    CONCAT('/spell inspect4 ', s.ID) AS inspect_command,
    CONCAT('/spell cast4 ', s.ID) AS cast_command
FROM spell4 s
JOIN spell4effects e
  ON e.spellId = s.ID
WHERE e.effectType = 120
ORDER BY
    s.ID,
    e.ID;

-- 44. Reward progression surfaces.
-- GiveAugmentPowerToPlayer is wired through ServerAmpPowerUpdate.
-- GiveAbilityPointsToPlayer is wired through the persisted ServerAbilityPoints
-- available/total budget. UnlockInlaidAugment remains evidence-only until its
-- unlock storage is identified.
SELECT
    e.effectType,
    n.effectName,
    s.ID AS spell4_id,
    s.spell4BaseIdBaseSpell AS spell4_base_id,
    s.tierIndex AS spell4_tier,
    LEFT(s.description, 140) AS spell,
    e.ID AS spell4_effect_id,
    e.orderIndex,
    e.targetFlags,
    e.dataBits00,
    e.dataBits01,
    e.dataBits02,
    e.dataBits03,
    e.dataBits04,
    e.dataBits05,
    CONCAT('/spell inspect4 ', s.ID) AS inspect_command,
    CONCAT('/spell cast4 ', s.ID) AS cast_command
FROM spell4 s
JOIN spell4effects e
  ON e.spellId = s.ID
LEFT JOIN spell_effect_type_names n
  ON n.effectType = e.effectType
WHERE e.effectType IN (78, 127, 128)
ORDER BY
    e.effectType,
    s.ID,
    e.ID
LIMIT 200;

-- 45. Global unresolved target-mechanic witnesses for target types 0, 6, and 7.
-- These are the current blocked client helper families; treat them as
-- inspect-first witnesses until the target-helper chain is mapped.
SELECT
  tm.ID AS target_mechanic_id,
  tm.targetType,
  tm.flags AS target_mechanic_flags,
  s.ID AS spell4_id,
  s.spell4BaseIdBaseSpell AS spell4_base_id,
  s.tierIndex AS spell4_tier,
  LEFT(s.description, 140) AS spell,
  GROUP_CONCAT(DISTINCT CONCAT(e.effectType, '/', e.targetFlags) ORDER BY e.orderIndex SEPARATOR '; ') AS effects,
  CONCAT('/spell inspect4 ', s.ID) AS inspect_command,
  CONCAT('/spell cast4 ', s.ID) AS cast_command
FROM spell4 s
JOIN spell4base b
  ON b.ID = s.spell4BaseIdBaseSpell
JOIN spell4targetmechanics tm
  ON tm.ID = b.spell4TargetMechanicId
LEFT JOIN spell4effects e
  ON e.spellId = s.ID
WHERE tm.targetType IN (0, 6, 7)
GROUP BY
  tm.ID,
  tm.targetType,
  tm.flags,
  s.ID,
  s.spell4BaseIdBaseSpell,
  s.tierIndex,
  s.description
ORDER BY tm.targetType, tm.ID, s.ID
LIMIT 220;

-- 46. RavelSignal witnesses with payload, timing, and optional placed context.
-- Placements are helpful for later receiver-graph work, but unplaced global
-- rows still matter because the receiver side is the real blocker.
SELECT
  s.ID AS spell4_id,
  s.spell4BaseIdBaseSpell AS spell4_base_id,
  s.tierIndex AS spell4_tier,
  LEFT(s.description, 140) AS spell,
  e.ID AS spell4_effect_id,
  e.targetFlags,
  e.delayTime,
  e.tickTime,
  e.durationTime,
  e.dataBits00 AS mode,
  e.dataBits01 AS signal_id,
  e.dataBits02,
  e.dataBits03,
  e.dataBits04,
  e.dataBits05,
  COUNT(DISTINCT wcsc.placement_id) AS placements,
  LEFT(GROUP_CONCAT(DISTINCT CONCAT(wcsc.continent, '/', wcsc.zone, '/', wcsc.creature_id) ORDER BY wcsc.continent, wcsc.zone, wcsc.creature_id SEPARATOR '; '), 240) AS creature_contexts,
  CONCAT('/spell inspect4 ', s.ID) AS inspect_command,
  CONCAT('/spell cast4 ', s.ID) AS cast_command
FROM spell4 s
JOIN spell4effects e
  ON e.spellId = s.ID
LEFT JOIN world_creature_spell_context wcsc
  ON wcsc.spell4_id = s.ID
 AND wcsc.entity_type = 0
WHERE e.effectType = 81
GROUP BY
  s.ID,
  s.spell4BaseIdBaseSpell,
  s.tierIndex,
  s.description,
  e.ID,
  e.targetFlags,
  e.delayTime,
  e.tickTime,
  e.durationTime,
  e.dataBits00,
  e.dataBits01,
  e.dataBits02,
  e.dataBits03,
  e.dataBits04,
  e.dataBits05
ORDER BY placements DESC, e.durationTime DESC, s.ID, e.ID
LIMIT 220;

-- 47. Activation-object world-target candidate families.
-- Target flag bit 0x02 is the proven interactable/object bucket. This query
-- highlights which object-target effect families fall outside the current
-- conservative runtime slice.
SELECT
  e.effectType,
  COALESCE(n.effectName, CONCAT('EffectType ', e.effectType)) AS effect_name,
  s.ID AS spell4_id,
  s.spell4BaseIdBaseSpell AS spell4_base_id,
  s.tierIndex AS spell4_tier,
  LEFT(s.description, 140) AS spell,
  e.ID AS spell4_effect_id,
  e.orderIndex,
  e.targetFlags,
  e.delayTime,
  e.tickTime,
  e.durationTime,
  COUNT(DISTINCT c.ID) AS activate_creature_rows,
  CASE
    WHEN e.effectType IN (7, 17, 26, 80, 81, 97, 107) THEN 'current-slice-or-diagnostics'
    ELSE 'outside-current-slice'
  END AS runtime_bucket,
  CONCAT('/spell inspect4 ', s.ID) AS inspect_command,
  CONCAT('/spell cast4 ', s.ID) AS cast_command
FROM spell4effects e
JOIN spell4 s
  ON s.ID = e.spellId
JOIN creature2 c
  ON c.spell4IdActivate00 = s.ID
  OR c.spell4IdActivate01 = s.ID
  OR c.spell4IdActivate02 = s.ID
  OR c.spell4IdActivate03 = s.ID
LEFT JOIN spell_effect_type_names n
  ON n.effectType = e.effectType
WHERE (e.targetFlags & 2) <> 0
GROUP BY
  e.effectType,
  effect_name,
  s.ID,
  s.spell4BaseIdBaseSpell,
  s.tierIndex,
  s.description,
  e.ID,
  e.orderIndex,
  e.targetFlags,
  e.delayTime,
  e.tickTime,
  e.durationTime,
  runtime_bucket
ORDER BY
  CASE
    WHEN e.effectType IN (7, 17, 26, 80, 81, 97, 107) THEN 1
    ELSE 0
  END,
  activate_creature_rows DESC,
  e.effectType,
  s.ID,
  e.ID
LIMIT 220;

-- 48. Service-token property-flag probe (local schema safe).
-- Decompile evidence proves a client/runtime gate on Spell4.PropertyFlags
-- 0x20000000, but both current SQL imports expose zero spell4 rows with that
-- bit set. Keep this query as a cheap regression probe for future data-import
-- changes; use 48b for the practical client-table witnesses that exist today.
SELECT
  s.ID AS spell4_id,
  s.spell4BaseIdBaseSpell AS spell4_base_id,
  s.tierIndex AS spell4_tier,
  LEFT(s.description, 140) AS spell,
  s.propertyFlags,
  CASE WHEN (s.propertyFlags & 536870912) <> 0 THEN 1 ELSE 0 END AS has_service_token_flag,
  CONCAT('/spell inspect4 ', s.ID) AS inspect_command,
  CONCAT('/spell cast4 ', s.ID) AS cast_command
FROM spell4 s
WHERE (s.propertyFlags & 536870912) <> 0
ORDER BY
  has_service_token_flag DESC,
  s.ID
LIMIT 120;

-- 48b. Service-token cost witnesses plus flag/cost mismatch check.
-- This version is schema-qualified so it can be run even when the current
-- mysql session database is nexus_spell_re. As of 2026-05-16 the extracted
-- client tables expose 6 Spell4ServiceTokenCost rows and 0 matching flagged
-- spell4 rows, so these cost rows are the durable SQL-side witness set.
-- SELECT
--   s.ID AS spell4_id,
--   s.spell4BaseIdBaseSpell AS spell4_base_id,
--   s.tierIndex AS spell4_tier,
--   LEFT(s.description, 140) AS spell,
--   s.propertyFlags,
--   CASE WHEN (s.propertyFlags & 536870912) <> 0 THEN 1 ELSE 0 END AS has_service_token_flag,
--   stc.serviceTokenCost,
--   CONCAT('/spell inspect4 ', s.ID) AS inspect_command,
--   CONCAT('/spell cast4 ', s.ID) AS cast_command
-- FROM wildstar_client.spell4 s
-- LEFT JOIN wildstar_client.Spell4ServiceTokenCost stc
--   ON stc.spell4Id = s.ID
-- WHERE stc.ID IS NOT NULL
--    OR (s.propertyFlags & 536870912) <> 0
-- ORDER BY
--   has_service_token_flag DESC,
--   stc.serviceTokenCost DESC,
--   s.ID
-- LIMIT 120;
