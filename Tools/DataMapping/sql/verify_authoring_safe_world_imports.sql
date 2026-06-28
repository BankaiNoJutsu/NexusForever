-- Authoring verification for safe mapped world-data imports.
-- Requires nf_map_* staging tables in nexus_forever_mapping plus the
-- Jabbithole reference database.

USE `nexus_forever_world`;

SET @nf_entity_id_base := IFNULL(@nf_entity_id_base, 1000000000);
SET @nf_entity_id_max := @nf_entity_id_base + 999999999;
SET @laughingws_city_content_entity_id_base := IFNULL(@laughingws_city_content_entity_id_base, 1100000000);
SET @laughingws_city_content_entity_id_max := IFNULL(@laughingws_city_content_entity_id_max, 1100099999);
SET @laughingws_quest_instance_entity_id_base := IFNULL(@laughingws_quest_instance_entity_id_base, 1100100000);
SET @laughingws_quest_instance_entity_id_max := IFNULL(@laughingws_quest_instance_entity_id_max, 1100199999);
SET @laughingws_small_world_entity_id_base := IFNULL(@laughingws_small_world_entity_id_base, 1100200000);
SET @laughingws_small_world_entity_id_max := IFNULL(@laughingws_small_world_entity_id_max, 1100299999);
SET @laughingws_instance_entity_id_base := IFNULL(@laughingws_instance_entity_id_base, 1100300000);
SET @laughingws_instance_entity_id_max := IFNULL(@laughingws_instance_entity_id_max, 1100399999);
SET @laughingws_housing_skyplot_entity_id_base := IFNULL(@laughingws_housing_skyplot_entity_id_base, 2000000000);
SET @laughingws_housing_skyplot_entity_id_max := IFNULL(@laughingws_housing_skyplot_entity_id_max, 2099999999);
SET @laughingws_live_event_entity_id_base := IFNULL(@laughingws_live_event_entity_id_base, 2100000000);
SET @laughingws_live_event_entity_id_max := IFNULL(@laughingws_live_event_entity_id_max, 2147483647);

SELECT 'entity_vendor' AS table_name, COUNT(*) AS row_count FROM entity_vendor
UNION ALL SELECT 'entity_vendor_category', COUNT(*) FROM entity_vendor_category
UNION ALL SELECT 'entity_vendor_item', COUNT(*) FROM entity_vendor_item
UNION ALL SELECT 'map_entrance_laughingws_overlay_rows', COUNT(*) FROM map_entrance WHERE (`mapId`, `team`, `worldLocationId`) IN ((382, 0, 1275), (797, 0, 7188), (797, 1, 7189), (1149, 0, 13390), (1181, 0, 13662), (1233, 0, 37409), (1263, 0, 17726), (1271, 0, 16348), (1323, 0, 32423), (1336, 0, 18557), (1393, 0, 38048), (2166, 0, 38485), (2166, 1, 38486), (2980, 0, 42236), (3009, 0, 50843), (3044, 0, 46354), (3045, 0, 45947), (3094, 0, 47085), (3173, 0, 48284), (3176, 0, 49019), (3449, 0, 51426), (3449, 1, 51427), (3522, 0, 53153))
UNION ALL SELECT 'expected_map_entrance_laughingws_overlay_rows_23_mismatch', IF(COUNT(*) = 23, 0, 1) FROM map_entrance WHERE (`mapId`, `team`, `worldLocationId`) IN ((382, 0, 1275), (797, 0, 7188), (797, 1, 7189), (1149, 0, 13390), (1181, 0, 13662), (1233, 0, 37409), (1263, 0, 17726), (1271, 0, 16348), (1323, 0, 32423), (1336, 0, 18557), (1393, 0, 38048), (2166, 0, 38485), (2166, 1, 38486), (2980, 0, 42236), (3009, 0, 50843), (3044, 0, 46354), (3045, 0, 45947), (3094, 0, 47085), (3173, 0, 48284), (3176, 0, 49019), (3449, 0, 51426), (3449, 1, 51427), (3522, 0, 53153))
UNION ALL SELECT 'entity_spawns_mapped', COUNT(*) FROM entity WHERE id BETWEEN @nf_entity_id_base AND @nf_entity_id_max
UNION ALL SELECT 'entity_spawn_stats_mapped', COUNT(*) FROM entity_stats es JOIN entity e ON e.id = es.id WHERE e.id BETWEEN @nf_entity_id_base AND @nf_entity_id_max
UNION ALL SELECT 'entity_laughingws_city_content', COUNT(*) FROM entity WHERE id BETWEEN @laughingws_city_content_entity_id_base AND @laughingws_city_content_entity_id_max AND NOT (id BETWEEN @laughingws_city_content_entity_id_base + 10001 AND @laughingws_city_content_entity_id_base + 10008)
UNION ALL SELECT 'expected_entity_laughingws_city_content_17_mismatch', IF(COUNT(*) = 17, 0, 1) FROM entity WHERE id BETWEEN @laughingws_city_content_entity_id_base AND @laughingws_city_content_entity_id_max AND NOT (id BETWEEN @laughingws_city_content_entity_id_base + 10001 AND @laughingws_city_content_entity_id_base + 10008)
UNION ALL SELECT 'entity_laughingws_illium_ringo_hax_wip', COUNT(*) FROM entity WHERE id BETWEEN @laughingws_city_content_entity_id_base AND @laughingws_city_content_entity_id_max AND world = 22 AND creature = 44961 AND ABS(x - (-3450.17)) < 0.001 AND ABS(y - (-890.5897)) < 0.001 AND ABS(z - (-739.757)) < 0.001
UNION ALL SELECT 'expected_entity_laughingws_illium_ringo_hax_wip_1_mismatch', IF(COUNT(*) = 1, 0, 1) FROM entity WHERE id BETWEEN @laughingws_city_content_entity_id_base AND @laughingws_city_content_entity_id_max AND world = 22 AND creature = 44961 AND ABS(x - (-3450.17)) < 0.001 AND ABS(y - (-890.5897)) < 0.001 AND ABS(z - (-739.757)) < 0.001
UNION ALL SELECT 'entity_laughingws_illium_housing_intro_fallback_rows', COUNT(*) FROM entity WHERE id BETWEEN @laughingws_city_content_entity_id_base + 10001 AND @laughingws_city_content_entity_id_base + 10004
UNION ALL SELECT 'expected_entity_laughingws_illium_housing_intro_fallback_rows_0_or_4_mismatch', IF(COUNT(*) IN (0, 4), 0, 1) FROM entity WHERE id BETWEEN @laughingws_city_content_entity_id_base + 10001 AND @laughingws_city_content_entity_id_base + 10004
UNION ALL SELECT 'entity_laughingws_thayd_housing_intro_fallback_rows', COUNT(*) FROM entity WHERE id BETWEEN @laughingws_city_content_entity_id_base + 10005 AND @laughingws_city_content_entity_id_base + 10008
UNION ALL SELECT 'expected_entity_laughingws_thayd_housing_intro_fallback_rows_0_or_4_mismatch', IF(COUNT(*) IN (0, 4), 0, 1) FROM entity WHERE id BETWEEN @laughingws_city_content_entity_id_base + 10005 AND @laughingws_city_content_entity_id_base + 10008
UNION ALL SELECT 'entity_laughingws_illium_housing_intro_quest_checklist_updates', COUNT(*) FROM entity WHERE world = 22 AND area = 3014 AND (
  (creature = 65296 AND questChecklistIdx = 1 AND activePropId = 2619935741074 AND ABS(x - (-3309.436)) < 0.001 AND ABS(y - (-903.7229)) < 0.001 AND ABS(z - (-859.9409)) < 0.001)
  OR (creature = 65297 AND questChecklistIdx = 2 AND activePropId = 2602755871890 AND ABS(x - (-3307.637)) < 0.001 AND ABS(y - (-903.723)) < 0.001 AND ABS(z - (-901.6152)) < 0.001)
  OR (creature = 65298 AND questChecklistIdx = 3 AND activePropId = 283473532050 AND ABS(x - (-3329.727)) < 0.001 AND ABS(y - (-903.7228)) < 0.001 AND ABS(z - (-879.0459)) < 0.001)
  OR (creature = 65299 AND questChecklistIdx = 4 AND activePropId = 2791734432914 AND ABS(x - (-3287.171)) < 0.001 AND ABS(y - (-903.7228)) < 0.001 AND ABS(z - (-882.6173)) < 0.001)
)
UNION ALL SELECT 'expected_entity_laughingws_illium_housing_intro_quest_checklist_updates_4_mismatch', IF(COUNT(*) = 4, 0, 1) FROM entity WHERE world = 22 AND area = 3014 AND (
  (creature = 65296 AND questChecklistIdx = 1 AND activePropId = 2619935741074 AND ABS(x - (-3309.436)) < 0.001 AND ABS(y - (-903.7229)) < 0.001 AND ABS(z - (-859.9409)) < 0.001)
  OR (creature = 65297 AND questChecklistIdx = 2 AND activePropId = 2602755871890 AND ABS(x - (-3307.637)) < 0.001 AND ABS(y - (-903.723)) < 0.001 AND ABS(z - (-901.6152)) < 0.001)
  OR (creature = 65298 AND questChecklistIdx = 3 AND activePropId = 283473532050 AND ABS(x - (-3329.727)) < 0.001 AND ABS(y - (-903.7228)) < 0.001 AND ABS(z - (-879.0459)) < 0.001)
  OR (creature = 65299 AND questChecklistIdx = 4 AND activePropId = 2791734432914 AND ABS(x - (-3287.171)) < 0.001 AND ABS(y - (-903.7228)) < 0.001 AND ABS(z - (-882.6173)) < 0.001)
)
UNION ALL SELECT 'entity_laughingws_thayd_housing_intro_quest_checklist_updates', COUNT(*) FROM entity WHERE world = 51 AND area = 3015 AND (
  (creature = 54400 AND questChecklistIdx = 1 AND activePropId = 2619935753624 AND ABS(x - (4078.783)) < 0.001 AND ABS(y - (-818.9699)) < 0.001 AND ABS(z - (-1653.37)) < 0.001)
  OR (creature = 54401 AND questChecklistIdx = 2 AND activePropId = 2602755884440 AND ABS(x - (4066.41)) < 0.001 AND ABS(y - (-818.9699)) < 0.001 AND ABS(z - (-1613.534)) < 0.001)
  OR (creature = 54403 AND questChecklistIdx = 3 AND activePropId = 283473544600 AND ABS(x - (4093.528)) < 0.001 AND ABS(y - (-818.9697)) < 0.001 AND ABS(z - (-1629.719)) < 0.001)
  OR (creature = 54404 AND questChecklistIdx = 4 AND activePropId = 2791734445464 AND ABS(x - (4051.468)) < 0.001 AND ABS(y - (-818.9697)) < 0.001 AND ABS(z - (-1637.125)) < 0.001)
)
UNION ALL SELECT 'expected_entity_laughingws_thayd_housing_intro_quest_checklist_updates_4_mismatch', IF(COUNT(*) = 4, 0, 1) FROM entity WHERE world = 51 AND area = 3015 AND (
  (creature = 54400 AND questChecklistIdx = 1 AND activePropId = 2619935753624 AND ABS(x - (4078.783)) < 0.001 AND ABS(y - (-818.9699)) < 0.001 AND ABS(z - (-1653.37)) < 0.001)
  OR (creature = 54401 AND questChecklistIdx = 2 AND activePropId = 2602755884440 AND ABS(x - (4066.41)) < 0.001 AND ABS(y - (-818.9699)) < 0.001 AND ABS(z - (-1613.534)) < 0.001)
  OR (creature = 54403 AND questChecklistIdx = 3 AND activePropId = 283473544600 AND ABS(x - (4093.528)) < 0.001 AND ABS(y - (-818.9697)) < 0.001 AND ABS(z - (-1629.719)) < 0.001)
  OR (creature = 54404 AND questChecklistIdx = 4 AND activePropId = 2791734445464 AND ABS(x - (4051.468)) < 0.001 AND ABS(y - (-818.9697)) < 0.001 AND ABS(z - (-1637.125)) < 0.001)
)
UNION ALL SELECT 'entity_laughingws_quest_instance_wip', COUNT(*) FROM entity WHERE id BETWEEN @laughingws_quest_instance_entity_id_base AND @laughingws_quest_instance_entity_id_max
UNION ALL SELECT 'entity_stats_laughingws_quest_instance_wip', COUNT(*) FROM entity_stats WHERE id BETWEEN @laughingws_quest_instance_entity_id_base AND @laughingws_quest_instance_entity_id_max
UNION ALL SELECT 'expected_entity_laughingws_quest_instance_wip_3_mismatch', IF(COUNT(*) = 3, 0, 1) FROM entity WHERE id BETWEEN @laughingws_quest_instance_entity_id_base AND @laughingws_quest_instance_entity_id_max
UNION ALL SELECT 'expected_entity_stats_laughingws_quest_instance_wip_4_mismatch', IF(COUNT(*) = 4, 0, 1) FROM entity_stats WHERE id BETWEEN @laughingws_quest_instance_entity_id_base AND @laughingws_quest_instance_entity_id_max
UNION ALL SELECT 'entity_laughingws_small_world_wip', COUNT(*) FROM entity WHERE id BETWEEN @laughingws_small_world_entity_id_base AND @laughingws_small_world_entity_id_max AND NOT (id BETWEEN @laughingws_small_world_entity_id_base + 10001 AND @laughingws_small_world_entity_id_base + 10067) AND NOT (id BETWEEN @laughingws_small_world_entity_id_base + 20001 AND @laughingws_small_world_entity_id_base + 20024) AND NOT (id BETWEEN @laughingws_small_world_entity_id_base + 30001 AND @laughingws_small_world_entity_id_base + 30002) AND NOT (id BETWEEN @laughingws_small_world_entity_id_base + 40001 AND @laughingws_small_world_entity_id_base + 40014)
UNION ALL SELECT 'entity_stats_laughingws_small_world_wip', COUNT(*) FROM entity_stats WHERE id BETWEEN @laughingws_small_world_entity_id_base AND @laughingws_small_world_entity_id_max AND NOT (id BETWEEN @laughingws_small_world_entity_id_base + 10001 AND @laughingws_small_world_entity_id_base + 10067) AND NOT (id BETWEEN @laughingws_small_world_entity_id_base + 20001 AND @laughingws_small_world_entity_id_base + 20024) AND NOT (id BETWEEN @laughingws_small_world_entity_id_base + 30001 AND @laughingws_small_world_entity_id_base + 30002) AND NOT (id BETWEEN @laughingws_small_world_entity_id_base + 40001 AND @laughingws_small_world_entity_id_base + 40014)
UNION ALL SELECT 'expected_entity_laughingws_small_world_wip_16_mismatch', IF(COUNT(*) = 16, 0, 1) FROM entity WHERE id BETWEEN @laughingws_small_world_entity_id_base AND @laughingws_small_world_entity_id_max AND NOT (id BETWEEN @laughingws_small_world_entity_id_base + 10001 AND @laughingws_small_world_entity_id_base + 10067) AND NOT (id BETWEEN @laughingws_small_world_entity_id_base + 20001 AND @laughingws_small_world_entity_id_base + 20024) AND NOT (id BETWEEN @laughingws_small_world_entity_id_base + 30001 AND @laughingws_small_world_entity_id_base + 30002) AND NOT (id BETWEEN @laughingws_small_world_entity_id_base + 40001 AND @laughingws_small_world_entity_id_base + 40014)
UNION ALL SELECT 'expected_entity_stats_laughingws_small_world_wip_59_mismatch', IF(COUNT(*) = 59, 0, 1) FROM entity_stats WHERE id BETWEEN @laughingws_small_world_entity_id_base AND @laughingws_small_world_entity_id_max AND NOT (id BETWEEN @laughingws_small_world_entity_id_base + 10001 AND @laughingws_small_world_entity_id_base + 10067) AND NOT (id BETWEEN @laughingws_small_world_entity_id_base + 20001 AND @laughingws_small_world_entity_id_base + 20024) AND NOT (id BETWEEN @laughingws_small_world_entity_id_base + 30001 AND @laughingws_small_world_entity_id_base + 30002) AND NOT (id BETWEEN @laughingws_small_world_entity_id_base + 40001 AND @laughingws_small_world_entity_id_base + 40014)
UNION ALL SELECT 'entity_laughingws_northern_wilds_wip', COUNT(*) FROM entity WHERE id BETWEEN @laughingws_small_world_entity_id_base AND @laughingws_small_world_entity_id_max AND world = 426 AND creature IN (11063, 12484, 12521, 12737, 12959, 13150, 13151)
UNION ALL SELECT 'expected_entity_laughingws_northern_wilds_wip_7_mismatch', IF(COUNT(*) = 7, 0, 1) FROM entity WHERE id BETWEEN @laughingws_small_world_entity_id_base AND @laughingws_small_world_entity_id_max AND world = 426 AND creature IN (11063, 12484, 12521, 12737, 12959, 13150, 13151)
UNION ALL SELECT 'entity_laughingws_northern_wilds_q3673_signal_flares_wip', COUNT(*) FROM entity WHERE id BETWEEN @laughingws_small_world_entity_id_base AND @laughingws_small_world_entity_id_max AND world = 426 AND ((creature = 12521 AND questChecklistIdx = 1) OR (creature = 13150 AND questChecklistIdx = 2) OR (creature = 13151 AND questChecklistIdx = 3))
UNION ALL SELECT 'expected_entity_laughingws_northern_wilds_q3673_signal_flares_wip_3_mismatch', IF(COUNT(*) = 3, 0, 1) FROM entity WHERE id BETWEEN @laughingws_small_world_entity_id_base AND @laughingws_small_world_entity_id_max AND world = 426 AND ((creature = 12521 AND questChecklistIdx = 1) OR (creature = 13150 AND questChecklistIdx = 2) OR (creature = 13151 AND questChecklistIdx = 3))
UNION ALL SELECT 'northern_wilds_deadeye_brightland_wl7726_spline_rows', COUNT(*) FROM entity e JOIN entity_spline es ON es.id = e.id WHERE e.world = 426 AND e.area = 596 AND e.creature = 11063 AND ABS(e.x - 4185.583) < 5.0 AND ABS(e.y - (-722.691)) < 5.0 AND ABS(e.z - (-5695.496)) < 5.0
UNION ALL SELECT 'expected_northern_wilds_deadeye_brightland_wl7726_spline_rows_0_mismatch', IF(COUNT(*) = 0, 0, 1) FROM entity e JOIN entity_spline es ON es.id = e.id WHERE e.world = 426 AND e.area = 596 AND e.creature = 11063 AND ABS(e.x - 4185.583) < 5.0 AND ABS(e.y - (-722.691)) < 5.0 AND ABS(e.z - (-5695.496)) < 5.0
UNION ALL SELECT 'entity_laughingws_levian_bay_quest_checklist_updates', COUNT(*) FROM entity WHERE world = 1387 AND (
  (creature = 26016 AND questChecklistIdx = 1 AND area = 1411 AND displayInfo = 24366 AND ABS(x - (-3518.27)) < 0.001 AND ABS(y - (-979.4003)) < 0.001 AND ABS(z - (-6106.71)) < 0.001)
  OR (creature = 26016 AND questChecklistIdx = 2 AND area = 1411 AND displayInfo = 24366 AND ABS(x - (-3548.95)) < 0.001 AND ABS(y - (-978.6245)) < 0.001 AND ABS(z - (-6129.68)) < 0.001)
  OR (creature = 26016 AND questChecklistIdx = 3 AND area = 1411 AND displayInfo = 24366 AND ABS(x - (-3514.5)) < 0.001 AND ABS(y - (-979.1523)) < 0.001 AND ABS(z - (-6123.66)) < 0.001)
  OR (creature = 26016 AND questChecklistIdx = 4 AND area = 1305 AND displayInfo = 24366 AND ABS(x - (-3674.854)) < 0.001 AND ABS(y - (-982.6992)) < 0.001 AND ABS(z - (-6136.635)) < 0.001)
  OR (creature = 26016 AND questChecklistIdx = 5 AND area = 1411 AND displayInfo = 24366 AND ABS(x - (-3728.567)) < 0.001 AND ABS(y - (-985.5303)) < 0.001 AND ABS(z - (-6099.625)) < 0.001)
  OR (creature = 26016 AND questChecklistIdx = 6 AND area = 1411 AND displayInfo = 24366 AND ABS(x - (-3793.806)) < 0.001 AND ABS(y - (-980.3337)) < 0.001 AND ABS(z - (-6055.686)) < 0.001)
  OR (creature = 26016 AND questChecklistIdx = 7 AND area = 1410 AND displayInfo = 24366 AND ABS(x - (-3667.516)) < 0.001 AND ABS(y - (-981.785)) < 0.001 AND ABS(z - (-6232.354)) < 0.001)
  OR (creature = 26016 AND questChecklistIdx = 8 AND area = 1410 AND displayInfo = 24366 AND ABS(x - (-3697.605)) < 0.001 AND ABS(y - (-983.1788)) < 0.001 AND ABS(z - (-6269.042)) < 0.001)
  OR (creature = 26016 AND questChecklistIdx = 9 AND area = 1410 AND displayInfo = 24366 AND ABS(x - (-3635.4)) < 0.001 AND ABS(y - (-982.1549)) < 0.001 AND ABS(z - (-6169.38)) < 0.001)
  OR (creature = 26016 AND questChecklistIdx = 10 AND area = 1410 AND displayInfo = 24366 AND ABS(x - (-3610.506)) < 0.001 AND ABS(y - (-983.6466)) < 0.001 AND ABS(z - (-6206.322)) < 0.001)
  OR (creature = 26016 AND questChecklistIdx = 11 AND area = 1305 AND displayInfo = 24366 AND ABS(x - (-3566.357)) < 0.001 AND ABS(y - (-983.3401)) < 0.001 AND ABS(z - (-6163.922)) < 0.001)
  OR (creature = 26016 AND questChecklistIdx = 12 AND area = 1410 AND displayInfo = 24366 AND ABS(x - (-3760.217)) < 0.001 AND ABS(y - (-992.5969)) < 0.001 AND ABS(z - (-6282.145)) < 0.001)
  OR (creature = 26417 AND questChecklistIdx = 1 AND area = 1307 AND activePropId = 1132444 AND displayInfo = 22603 AND ABS(x - (-2451.047)) < 0.001 AND ABS(y - (-987.081)) < 0.001 AND ABS(z - (-5128.363)) < 0.001)
  OR (creature = 26417 AND questChecklistIdx = 2 AND area = 1307 AND activePropId = 1132445 AND displayInfo = 22603 AND ABS(x - (-2529.2)) < 0.001 AND ABS(y - (-990.8531)) < 0.001 AND ABS(z - (-5120.009)) < 0.001)
)
UNION ALL SELECT 'expected_entity_laughingws_levian_bay_quest_checklist_updates_14_mismatch', IF(COUNT(*) = 14, 0, 1) FROM entity WHERE world = 1387 AND (
  (creature = 26016 AND questChecklistIdx = 1 AND area = 1411 AND displayInfo = 24366 AND ABS(x - (-3518.27)) < 0.001 AND ABS(y - (-979.4003)) < 0.001 AND ABS(z - (-6106.71)) < 0.001)
  OR (creature = 26016 AND questChecklistIdx = 2 AND area = 1411 AND displayInfo = 24366 AND ABS(x - (-3548.95)) < 0.001 AND ABS(y - (-978.6245)) < 0.001 AND ABS(z - (-6129.68)) < 0.001)
  OR (creature = 26016 AND questChecklistIdx = 3 AND area = 1411 AND displayInfo = 24366 AND ABS(x - (-3514.5)) < 0.001 AND ABS(y - (-979.1523)) < 0.001 AND ABS(z - (-6123.66)) < 0.001)
  OR (creature = 26016 AND questChecklistIdx = 4 AND area = 1305 AND displayInfo = 24366 AND ABS(x - (-3674.854)) < 0.001 AND ABS(y - (-982.6992)) < 0.001 AND ABS(z - (-6136.635)) < 0.001)
  OR (creature = 26016 AND questChecklistIdx = 5 AND area = 1411 AND displayInfo = 24366 AND ABS(x - (-3728.567)) < 0.001 AND ABS(y - (-985.5303)) < 0.001 AND ABS(z - (-6099.625)) < 0.001)
  OR (creature = 26016 AND questChecklistIdx = 6 AND area = 1411 AND displayInfo = 24366 AND ABS(x - (-3793.806)) < 0.001 AND ABS(y - (-980.3337)) < 0.001 AND ABS(z - (-6055.686)) < 0.001)
  OR (creature = 26016 AND questChecklistIdx = 7 AND area = 1410 AND displayInfo = 24366 AND ABS(x - (-3667.516)) < 0.001 AND ABS(y - (-981.785)) < 0.001 AND ABS(z - (-6232.354)) < 0.001)
  OR (creature = 26016 AND questChecklistIdx = 8 AND area = 1410 AND displayInfo = 24366 AND ABS(x - (-3697.605)) < 0.001 AND ABS(y - (-983.1788)) < 0.001 AND ABS(z - (-6269.042)) < 0.001)
  OR (creature = 26016 AND questChecklistIdx = 9 AND area = 1410 AND displayInfo = 24366 AND ABS(x - (-3635.4)) < 0.001 AND ABS(y - (-982.1549)) < 0.001 AND ABS(z - (-6169.38)) < 0.001)
  OR (creature = 26016 AND questChecklistIdx = 10 AND area = 1410 AND displayInfo = 24366 AND ABS(x - (-3610.506)) < 0.001 AND ABS(y - (-983.6466)) < 0.001 AND ABS(z - (-6206.322)) < 0.001)
  OR (creature = 26016 AND questChecklistIdx = 11 AND area = 1305 AND displayInfo = 24366 AND ABS(x - (-3566.357)) < 0.001 AND ABS(y - (-983.3401)) < 0.001 AND ABS(z - (-6163.922)) < 0.001)
  OR (creature = 26016 AND questChecklistIdx = 12 AND area = 1410 AND displayInfo = 24366 AND ABS(x - (-3760.217)) < 0.001 AND ABS(y - (-992.5969)) < 0.001 AND ABS(z - (-6282.145)) < 0.001)
  OR (creature = 26417 AND questChecklistIdx = 1 AND area = 1307 AND activePropId = 1132444 AND displayInfo = 22603 AND ABS(x - (-2451.047)) < 0.001 AND ABS(y - (-987.081)) < 0.001 AND ABS(z - (-5128.363)) < 0.001)
  OR (creature = 26417 AND questChecklistIdx = 2 AND area = 1307 AND activePropId = 1132445 AND displayInfo = 22603 AND ABS(x - (-2529.2)) < 0.001 AND ABS(y - (-990.8531)) < 0.001 AND ABS(z - (-5120.009)) < 0.001)
)
UNION ALL SELECT 'entity_laughingws_levian_bay_checklist_fallback_rows', COUNT(*) FROM entity WHERE id BETWEEN @laughingws_small_world_entity_id_base + 40001 AND @laughingws_small_world_entity_id_base + 40014
UNION ALL SELECT 'expected_entity_laughingws_levian_bay_checklist_fallback_rows_0_to_14_mismatch', IF(COUNT(*) BETWEEN 0 AND 14, 0, 1) FROM entity WHERE id BETWEEN @laughingws_small_world_entity_id_base + 40001 AND @laughingws_small_world_entity_id_base + 40014
UNION ALL SELECT 'entity_laughingws_wilderrun_quest_checklist_updates', COUNT(*) FROM entity WHERE world = 22 AND (
  (creature = 38051 AND questChecklistIdx BETWEEN 1 AND 30 AND activePropId IN (5642431, 5642417, 5642421, 5642423, 5642432, 5642430, 5642420, 5642424, 5642418, 5642426, 5642425, 5642439, 5642442, 5642444, 5642445, 5642429, 5642437, 5642575, 5642422, 5642574, 5642446, 5642438, 5642443, 5642573, 5642571, 5642572, 5642582, 5642436, 5642583, 5642435))
  OR (creature = 40353 AND questChecklistIdx = 1 AND ABS(x - (2609.117)) < 0.001 AND ABS(y - (-765.4273)) < 0.001 AND ABS(z - (-2688.797)) < 0.001)
  OR (creature = 40362 AND questChecklistIdx = 2 AND ABS(x - (2534.907)) < 0.001 AND ABS(y - (-764.6382)) < 0.001 AND ABS(z - (-2642.744)) < 0.001)
  OR (creature = 40365 AND questChecklistIdx = 3 AND ABS(x - (2590.223)) < 0.001 AND ABS(y - (-770.4839)) < 0.001 AND ABS(z - (-2644.115)) < 0.001)
)
UNION ALL SELECT 'expected_entity_laughingws_wilderrun_quest_checklist_updates_33_mismatch', IF(COUNT(*) = 33, 0, 1) FROM entity WHERE world = 22 AND (
  (creature = 38051 AND questChecklistIdx BETWEEN 1 AND 30 AND activePropId IN (5642431, 5642417, 5642421, 5642423, 5642432, 5642430, 5642420, 5642424, 5642418, 5642426, 5642425, 5642439, 5642442, 5642444, 5642445, 5642429, 5642437, 5642575, 5642422, 5642574, 5642446, 5642438, 5642443, 5642573, 5642571, 5642572, 5642582, 5642436, 5642583, 5642435))
  OR (creature = 40353 AND questChecklistIdx = 1 AND ABS(x - (2609.117)) < 0.001 AND ABS(y - (-765.4273)) < 0.001 AND ABS(z - (-2688.797)) < 0.001)
  OR (creature = 40362 AND questChecklistIdx = 2 AND ABS(x - (2534.907)) < 0.001 AND ABS(y - (-764.6382)) < 0.001 AND ABS(z - (-2642.744)) < 0.001)
  OR (creature = 40365 AND questChecklistIdx = 3 AND ABS(x - (2590.223)) < 0.001 AND ABS(y - (-770.4839)) < 0.001 AND ABS(z - (-2644.115)) < 0.001)
)
UNION ALL SELECT 'entity_laughingws_auroria_quest_checklist_updates', COUNT(*) FROM entity WHERE world = 22 AND (
  (creature = 12996 AND questChecklistIdx BETWEEN 1 AND 5 AND activePropId IN (6403796558468, 6575595250308, 6493990871684, 6395206623876, 6489695904388))
  OR (creature = 13652 AND questChecklistIdx BETWEEN 1 AND 7 AND area IN (72, 1326))
  OR (creature = 24053 AND questChecklistIdx BETWEEN 1 AND 4 AND area = 385)
  OR (creature = 24451 AND questChecklistIdx = 1 AND area = 38)
  OR (creature = 24459 AND questChecklistIdx BETWEEN 2 AND 4 AND area = 38)
  OR (creature = 24460 AND questChecklistIdx BETWEEN 5 AND 8 AND area = 38)
  OR (creature = 24462 AND questChecklistIdx = 9 AND area = 38)
  OR (creature = 24463 AND questChecklistIdx BETWEEN 10 AND 15 AND area = 38)
  OR (creature = 25265 AND questChecklistIdx BETWEEN 1 AND 3 AND area = 1229)
  OR (creature = 25266 AND questChecklistIdx = 4 AND area = 1229)
  OR (creature = 25267 AND questChecklistIdx BETWEEN 5 AND 7 AND area = 1229)
  OR (creature = 26337 AND questChecklistIdx BETWEEN 1 AND 7 AND activePropId IN (1146282, 1146281, 1141747, 1141744, 1141746, 1141743, 1141742))
  OR (creature = 27496 AND questChecklistIdx BETWEEN 1 AND 2 AND activePropId IN (1214155, 1214149))
  OR (creature = 27667 AND questChecklistIdx BETWEEN 1 AND 5 AND activePropId IN (1262429, 1262434, 1262430, 1262427, 1262432))
  OR (creature = 30752 AND questChecklistIdx BETWEEN 1 AND 8 AND area = 41)
  OR (creature = 35583 AND questChecklistIdx BETWEEN 9 AND 15 AND area IN (41, 849))
)
UNION ALL SELECT 'expected_entity_laughingws_auroria_quest_checklist_updates_67_mismatch', IF(COUNT(*) = 67, 0, 1) FROM entity WHERE world = 22 AND (
  (creature = 12996 AND questChecklistIdx BETWEEN 1 AND 5 AND activePropId IN (6403796558468, 6575595250308, 6493990871684, 6395206623876, 6489695904388))
  OR (creature = 13652 AND questChecklistIdx BETWEEN 1 AND 7 AND area IN (72, 1326))
  OR (creature = 24053 AND questChecklistIdx BETWEEN 1 AND 4 AND area = 385)
  OR (creature = 24451 AND questChecklistIdx = 1 AND area = 38)
  OR (creature = 24459 AND questChecklistIdx BETWEEN 2 AND 4 AND area = 38)
  OR (creature = 24460 AND questChecklistIdx BETWEEN 5 AND 8 AND area = 38)
  OR (creature = 24462 AND questChecklistIdx = 9 AND area = 38)
  OR (creature = 24463 AND questChecklistIdx BETWEEN 10 AND 15 AND area = 38)
  OR (creature = 25265 AND questChecklistIdx BETWEEN 1 AND 3 AND area = 1229)
  OR (creature = 25266 AND questChecklistIdx = 4 AND area = 1229)
  OR (creature = 25267 AND questChecklistIdx BETWEEN 5 AND 7 AND area = 1229)
  OR (creature = 26337 AND questChecklistIdx BETWEEN 1 AND 7 AND activePropId IN (1146282, 1146281, 1141747, 1141744, 1141746, 1141743, 1141742))
  OR (creature = 27496 AND questChecklistIdx BETWEEN 1 AND 2 AND activePropId IN (1214155, 1214149))
  OR (creature = 27667 AND questChecklistIdx BETWEEN 1 AND 5 AND activePropId IN (1262429, 1262434, 1262430, 1262427, 1262432))
  OR (creature = 30752 AND questChecklistIdx BETWEEN 1 AND 8 AND area = 41)
  OR (creature = 35583 AND questChecklistIdx BETWEEN 9 AND 15 AND area IN (41, 849))
)
UNION ALL SELECT 'entity_laughingws_auroria_checklist_fallback_rows', COUNT(*) FROM entity WHERE id BETWEEN @laughingws_small_world_entity_id_base + 10001 AND @laughingws_small_world_entity_id_base + 10067
UNION ALL SELECT 'expected_entity_laughingws_auroria_checklist_fallback_rows_0_to_67_mismatch', IF(COUNT(*) BETWEEN 0 AND 67, 0, 1) FROM entity WHERE id BETWEEN @laughingws_small_world_entity_id_base + 10001 AND @laughingws_small_world_entity_id_base + 10067
UNION ALL SELECT 'entity_stats_laughingws_auroria_checklist_fallback_rows', COUNT(*) FROM entity_stats WHERE id BETWEEN @laughingws_small_world_entity_id_base + 10001 AND @laughingws_small_world_entity_id_base + 10067
UNION ALL SELECT 'expected_entity_stats_laughingws_auroria_checklist_fallback_rows_0_to_213_mismatch', IF(COUNT(*) BETWEEN 0 AND 213, 0, 1) FROM entity_stats WHERE id BETWEEN @laughingws_small_world_entity_id_base + 10001 AND @laughingws_small_world_entity_id_base + 10067
UNION ALL SELECT 'entity_laughingws_crimson_isle_quest_checklist_updates', COUNT(*) FROM entity WHERE world = 870 AND (
  (creature = 24225 AND questChecklistIdx BETWEEN 1 AND 2 AND activePropId IN (1132353, 1132352))
  OR (creature = 24298 AND questChecklistIdx BETWEEN 1 AND 3 AND activePropId IN (1085356, 1085429, 1081959))
  OR (creature = 24312 AND questChecklistIdx BETWEEN 1 AND 8 AND activePropId IN (993753, 975120, 975118, 1123137, 975131, 975117, 972458, 975119))
  OR (creature = 24703 AND questChecklistIdx BETWEEN 1 AND 6 AND area IN (1227, 1611) AND displayInfo IN (25936, 25922, 25941, 25946, 25944, 25915))
  OR (creature = 24999 AND questChecklistIdx BETWEEN 1 AND 3 AND activePropId IN (5708188, 5708196, 5708174))
  OR (creature = 26559 AND questChecklistIdx BETWEEN 1 AND 2 AND activePropId IN (1137353, 1137404))
)
UNION ALL SELECT 'expected_entity_laughingws_crimson_isle_quest_checklist_updates_24_mismatch', IF(COUNT(*) = 24, 0, 1) FROM entity WHERE world = 870 AND (
  (creature = 24225 AND questChecklistIdx BETWEEN 1 AND 2 AND activePropId IN (1132353, 1132352))
  OR (creature = 24298 AND questChecklistIdx BETWEEN 1 AND 3 AND activePropId IN (1085356, 1085429, 1081959))
  OR (creature = 24312 AND questChecklistIdx BETWEEN 1 AND 8 AND activePropId IN (993753, 975120, 975118, 1123137, 975131, 975117, 972458, 975119))
  OR (creature = 24703 AND questChecklistIdx BETWEEN 1 AND 6 AND area IN (1227, 1611) AND displayInfo IN (25936, 25922, 25941, 25946, 25944, 25915))
  OR (creature = 24999 AND questChecklistIdx BETWEEN 1 AND 3 AND activePropId IN (5708188, 5708196, 5708174))
  OR (creature = 26559 AND questChecklistIdx BETWEEN 1 AND 2 AND activePropId IN (1137353, 1137404))
)
UNION ALL SELECT 'entity_laughingws_crimson_isle_checklist_fallback_rows', COUNT(*) FROM entity WHERE id BETWEEN @laughingws_small_world_entity_id_base + 20001 AND @laughingws_small_world_entity_id_base + 20024
UNION ALL SELECT 'expected_entity_laughingws_crimson_isle_checklist_fallback_rows_0_to_24_mismatch', IF(COUNT(*) BETWEEN 0 AND 24, 0, 1) FROM entity WHERE id BETWEEN @laughingws_small_world_entity_id_base + 20001 AND @laughingws_small_world_entity_id_base + 20024
UNION ALL SELECT 'entity_laughingws_everstar_grove_quest_checklist_updates', COUNT(*) FROM entity WHERE world = 990 AND area = 1415 AND (
  (creature = 28454 AND questChecklistIdx = 1 AND activePropId = 1181542 AND displayInfo = 25526 AND ABS(x - (-210.7691)) < 0.001 AND ABS(y - (-942.2574)) < 0.001 AND ABS(z - (-3011.136)) < 0.001)
  OR (creature = 28454 AND questChecklistIdx = 2 AND activePropId = 51540831683 AND displayInfo = 25526 AND ABS(x - (-241.6909)) < 0.001 AND ABS(y - (-1123.169)) < 0.001 AND ABS(z - (-3012.247)) < 0.001)
)
UNION ALL SELECT 'expected_entity_laughingws_everstar_grove_quest_checklist_updates_2_mismatch', IF(COUNT(*) = 2, 0, 1) FROM entity WHERE world = 990 AND area = 1415 AND (
  (creature = 28454 AND questChecklistIdx = 1 AND activePropId = 1181542 AND displayInfo = 25526 AND ABS(x - (-210.7691)) < 0.001 AND ABS(y - (-942.2574)) < 0.001 AND ABS(z - (-3011.136)) < 0.001)
  OR (creature = 28454 AND questChecklistIdx = 2 AND activePropId = 51540831683 AND displayInfo = 25526 AND ABS(x - (-241.6909)) < 0.001 AND ABS(y - (-1123.169)) < 0.001 AND ABS(z - (-3012.247)) < 0.001)
)
UNION ALL SELECT 'entity_laughingws_everstar_grove_checklist_fallback_rows', COUNT(*) FROM entity WHERE id BETWEEN @laughingws_small_world_entity_id_base + 30001 AND @laughingws_small_world_entity_id_base + 30002
UNION ALL SELECT 'expected_entity_laughingws_everstar_grove_checklist_fallback_rows_0_to_2_mismatch', IF(COUNT(*) BETWEEN 0 AND 2, 0, 1) FROM entity WHERE id BETWEEN @laughingws_small_world_entity_id_base + 30001 AND @laughingws_small_world_entity_id_base + 30002
UNION ALL SELECT 'entity_stats_laughingws_everstar_grove_checklist_fallback_rows', COUNT(*) FROM entity_stats WHERE id BETWEEN @laughingws_small_world_entity_id_base + 30001 AND @laughingws_small_world_entity_id_base + 30002
UNION ALL SELECT 'expected_entity_stats_laughingws_everstar_grove_checklist_fallback_rows_0_to_6_mismatch', IF(COUNT(*) BETWEEN 0 AND 6, 0, 1) FROM entity_stats WHERE id BETWEEN @laughingws_small_world_entity_id_base + 30001 AND @laughingws_small_world_entity_id_base + 30002
UNION ALL SELECT 'entity_laughingws_instance_wip', COUNT(*) FROM entity WHERE id BETWEEN @laughingws_instance_entity_id_base AND @laughingws_instance_entity_id_max
UNION ALL SELECT 'entity_event_laughingws_instance_wip', COUNT(*) FROM entity_event WHERE id BETWEEN @laughingws_instance_entity_id_base AND @laughingws_instance_entity_id_max
UNION ALL SELECT 'entity_script_laughingws_instance_wip', COUNT(*) FROM entity_script WHERE id BETWEEN @laughingws_instance_entity_id_base AND @laughingws_instance_entity_id_max
UNION ALL SELECT 'entity_stats_laughingws_instance_wip', COUNT(*) FROM entity_stats WHERE id BETWEEN @laughingws_instance_entity_id_base AND @laughingws_instance_entity_id_max
UNION ALL SELECT 'expected_entity_laughingws_instance_wip_72_mismatch', IF(COUNT(*) = 72, 0, 1) FROM entity WHERE id BETWEEN @laughingws_instance_entity_id_base AND @laughingws_instance_entity_id_max
UNION ALL SELECT 'expected_entity_event_laughingws_instance_wip_64_mismatch', IF(COUNT(*) = 64, 0, 1) FROM entity_event WHERE id BETWEEN @laughingws_instance_entity_id_base AND @laughingws_instance_entity_id_max
UNION ALL SELECT 'expected_entity_script_laughingws_instance_wip_35_mismatch', IF(COUNT(*) = 35, 0, 1) FROM entity_script WHERE id BETWEEN @laughingws_instance_entity_id_base AND @laughingws_instance_entity_id_max
UNION ALL SELECT 'expected_entity_stats_laughingws_instance_wip_80_mismatch', IF(COUNT(*) = 80, 0, 1) FROM entity_stats WHERE id BETWEEN @laughingws_instance_entity_id_base AND @laughingws_instance_entity_id_max
UNION ALL SELECT 'entity_laughingws_instance_datascape_wip', COUNT(*) FROM entity WHERE id BETWEEN @laughingws_instance_entity_id_base AND @laughingws_instance_entity_id_max AND world = 1333
UNION ALL SELECT 'expected_entity_laughingws_instance_datascape_wip_17_mismatch', IF(COUNT(*) = 17, 0, 1) FROM entity WHERE id BETWEEN @laughingws_instance_entity_id_base AND @laughingws_instance_entity_id_max AND world = 1333
UNION ALL SELECT 'entity_laughingws_instance_shades_eve_redmoon_wip', COUNT(*) FROM entity WHERE id BETWEEN @laughingws_instance_entity_id_base AND @laughingws_instance_entity_id_max AND ((world = 3044 AND creature IN (62747, 64820)) OR (world = 3032 AND creature = 65997))
UNION ALL SELECT 'expected_entity_laughingws_instance_shades_eve_redmoon_wip_3_mismatch', IF(COUNT(*) = 3, 0, 1) FROM entity WHERE id BETWEEN @laughingws_instance_entity_id_base AND @laughingws_instance_entity_id_max AND ((world = 3044 AND creature IN (62747, 64820)) OR (world = 3032 AND creature = 65997))
UNION ALL SELECT 'entity_laughingws_instance_evil_drive_spark_wip', COUNT(*) FROM entity e JOIN entity_event ee ON ee.id = e.id WHERE e.id BETWEEN @laughingws_instance_entity_id_base AND @laughingws_instance_entity_id_max AND e.world = 3404 AND e.creature = 71847 AND ee.eventId = 781 AND ee.phase = 23
UNION ALL SELECT 'expected_entity_laughingws_instance_evil_drive_spark_wip_11_mismatch', IF(COUNT(*) = 11, 0, 1) FROM entity e JOIN entity_event ee ON ee.id = e.id WHERE e.id BETWEEN @laughingws_instance_entity_id_base AND @laughingws_instance_entity_id_max AND e.world = 3404 AND e.creature = 71847 AND ee.eventId = 781 AND ee.phase = 23
UNION ALL SELECT 'entity_script_laughingws_instance_protogames_extra_boss_wip', COUNT(*) FROM entity e JOIN entity_script es ON es.id = e.id WHERE e.id BETWEEN @laughingws_instance_entity_id_base AND @laughingws_instance_entity_id_max AND e.world = 3173 AND ((e.creature = 67668 AND es.scriptName = 'SeekNSlaughterEntityScript') OR (e.creature = 67757 AND es.scriptName = 'IceboxMk2EntityScript'))
UNION ALL SELECT 'expected_entity_script_laughingws_instance_protogames_extra_boss_wip_2_mismatch', IF(COUNT(*) = 2, 0, 1) FROM entity e JOIN entity_script es ON es.id = e.id WHERE e.id BETWEEN @laughingws_instance_entity_id_base AND @laughingws_instance_entity_id_max AND e.world = 3173 AND ((e.creature = 67668 AND es.scriptName = 'SeekNSlaughterEntityScript') OR (e.creature = 67757 AND es.scriptName = 'IceboxMk2EntityScript'))
UNION ALL SELECT 'entity_script_laughingws_instance_redmoon_laveka_wip', COUNT(*) FROM entity e JOIN entity_script es ON es.id = e.id WHERE e.id BETWEEN @laughingws_instance_entity_id_base AND @laughingws_instance_entity_id_max AND e.world = 3032 AND e.creature = 65997 AND es.scriptName = 'LavekaTheDarkHeartedEntityScript'
UNION ALL SELECT 'expected_entity_script_laughingws_instance_redmoon_laveka_wip_1_mismatch', IF(COUNT(*) = 1, 0, 1) FROM entity e JOIN entity_script es ON es.id = e.id WHERE e.id BETWEEN @laughingws_instance_entity_id_base AND @laughingws_instance_entity_id_max AND e.world = 3032 AND e.creature = 65997 AND es.scriptName = 'LavekaTheDarkHeartedEntityScript'
UNION ALL SELECT 'entity_event_official_arena_forcefields', COUNT(*) FROM entity e JOIN entity_event ee ON ee.id = e.id WHERE e.creature IN (35295, 35296) AND ((e.world = 1535 AND ee.eventId = 205) OR (e.world = 3022 AND ee.eventId = 581))
UNION ALL SELECT 'expected_entity_event_official_arena_forcefields_4_mismatch', IF(COUNT(*) = 4, 0, 1) FROM entity e JOIN entity_event ee ON ee.id = e.id WHERE e.creature IN (35295, 35296) AND ((e.world = 1535 AND ee.eventId = 205) OR (e.world = 3022 AND ee.eventId = 581))
UNION ALL SELECT 'entity_script_evil_from_ether_official_hooks', COUNT(*) FROM entity e JOIN entity_script es ON es.id = e.id WHERE e.world = 3404 AND es.scriptName IN ('KatjaZarkhovEntityScript', 'RavenousRefugeeEntityScript', 'RavenousReaperEntityScript', 'SecurityChiefKondovichEntityScript')
UNION ALL SELECT 'expected_entity_script_evil_from_ether_official_hooks_12_mismatch', IF(COUNT(*) = 12, 0, 1) FROM entity e JOIN entity_script es ON es.id = e.id WHERE e.world = 3404 AND es.scriptName IN ('KatjaZarkhovEntityScript', 'RavenousRefugeeEntityScript', 'RavenousReaperEntityScript', 'SecurityChiefKondovichEntityScript')
UNION ALL SELECT 'entity_laughingws_evil_from_ether_branch_area_reconciliation', COUNT(*) FROM entity WHERE world = 3404 AND (
  (creature = 70999 AND area = 4836 AND ABS(x - (-422.1375)) < 0.001 AND ABS(y - (-844.95026)) < 0.001 AND ABS(z - (122.07739)) < 0.001)
  OR (creature = 71003 AND area = 4836 AND ABS(x - (-396.43555)) < 0.001 AND ABS(y - (-840.7188)) < 0.001 AND ABS(z - (119.74138)) < 0.001)
  OR (creature = 71283 AND area = 4823 AND ABS(x - (54.22043)) < 0.001 AND ABS(y - (-848.04)) < 0.001 AND ABS(z - (-159.99998)) < 0.001)
  OR (creature = 71322 AND area = 4823 AND ABS(x - (92.72337)) < 0.001 AND ABS(y - (-852.9272)) < 0.001 AND ABS(z - (-110.27059)) < 0.001)
  OR (creature = 71322 AND area = 4823 AND ABS(x - (93.24837)) < 0.001 AND ABS(y - (-852.92975)) < 0.001 AND ABS(z - (-74.36311)) < 0.001)
  OR (creature = 71322 AND area = 4823 AND ABS(x - (65.68544)) < 0.001 AND ABS(y - (-852.9225)) < 0.001 AND ABS(z - (-140.38359)) < 0.001)
  OR (creature = 71323 AND area = 4823 AND ABS(x - (54.22043)) < 0.001 AND ABS(y - (-848.0354)) < 0.001 AND ABS(z - (-159.99869)) < 0.001)
)
UNION ALL SELECT 'expected_entity_laughingws_evil_from_ether_branch_area_reconciliation_7_mismatch', IF(COUNT(*) = 7, 0, 1) FROM entity WHERE world = 3404 AND (
  (creature = 70999 AND area = 4836 AND ABS(x - (-422.1375)) < 0.001 AND ABS(y - (-844.95026)) < 0.001 AND ABS(z - (122.07739)) < 0.001)
  OR (creature = 71003 AND area = 4836 AND ABS(x - (-396.43555)) < 0.001 AND ABS(y - (-840.7188)) < 0.001 AND ABS(z - (119.74138)) < 0.001)
  OR (creature = 71283 AND area = 4823 AND ABS(x - (54.22043)) < 0.001 AND ABS(y - (-848.04)) < 0.001 AND ABS(z - (-159.99998)) < 0.001)
  OR (creature = 71322 AND area = 4823 AND ABS(x - (92.72337)) < 0.001 AND ABS(y - (-852.9272)) < 0.001 AND ABS(z - (-110.27059)) < 0.001)
  OR (creature = 71322 AND area = 4823 AND ABS(x - (93.24837)) < 0.001 AND ABS(y - (-852.92975)) < 0.001 AND ABS(z - (-74.36311)) < 0.001)
  OR (creature = 71322 AND area = 4823 AND ABS(x - (65.68544)) < 0.001 AND ABS(y - (-852.9225)) < 0.001 AND ABS(z - (-140.38359)) < 0.001)
  OR (creature = 71323 AND area = 4823 AND ABS(x - (54.22043)) < 0.001 AND ABS(y - (-848.0354)) < 0.001 AND ABS(z - (-159.99869)) < 0.001)
)
UNION ALL SELECT 'entity_laughingws_evil_from_ether_crew_log_branch_indexes', COUNT(*) FROM entity WHERE world = 3404 AND creature = 71234 AND (
  (questChecklistIdx = 0 AND ABS(x - (31.47145)) < 0.001 AND ABS(y - (-849.1181)) < 0.001 AND ABS(z - (-152.4904)) < 0.001)
  OR (questChecklistIdx = 1 AND ABS(x - (-65.33878)) < 0.001 AND ABS(y - (-844.1473)) < 0.001 AND ABS(z - (-150.08818)) < 0.001)
  OR (questChecklistIdx = 2 AND ABS(x - (68.89046)) < 0.001 AND ABS(y - (-849.1848)) < 0.001 AND ABS(z - (-182.18137)) < 0.001)
  OR (questChecklistIdx = 3 AND ABS(x - (44.647617)) < 0.001 AND ABS(y - (-840.1598)) < 0.001 AND ABS(z - (184.907)) < 0.001)
  OR (questChecklistIdx = 4 AND ABS(x - (-32.94068)) < 0.001 AND ABS(y - (-843.9746)) < 0.001 AND ABS(z - (143.43298)) < 0.001)
  OR (questChecklistIdx = 5 AND ABS(x - (18.839134)) < 0.001 AND ABS(y - (-840.56036)) < 0.001 AND ABS(z - (-113.37005)) < 0.001)
  OR (questChecklistIdx = 6 AND ABS(x - (-17.037079)) < 0.001 AND ABS(y - (-843.8909)) < 0.001 AND ABS(z - (46.10919)) < 0.001)
)
UNION ALL SELECT 'expected_entity_laughingws_evil_from_ether_crew_log_branch_indexes_7_mismatch', IF(COUNT(*) = 7, 0, 1) FROM entity WHERE world = 3404 AND creature = 71234 AND (
  (questChecklistIdx = 0 AND ABS(x - (31.47145)) < 0.001 AND ABS(y - (-849.1181)) < 0.001 AND ABS(z - (-152.4904)) < 0.001)
  OR (questChecklistIdx = 1 AND ABS(x - (-65.33878)) < 0.001 AND ABS(y - (-844.1473)) < 0.001 AND ABS(z - (-150.08818)) < 0.001)
  OR (questChecklistIdx = 2 AND ABS(x - (68.89046)) < 0.001 AND ABS(y - (-849.1848)) < 0.001 AND ABS(z - (-182.18137)) < 0.001)
  OR (questChecklistIdx = 3 AND ABS(x - (44.647617)) < 0.001 AND ABS(y - (-840.1598)) < 0.001 AND ABS(z - (184.907)) < 0.001)
  OR (questChecklistIdx = 4 AND ABS(x - (-32.94068)) < 0.001 AND ABS(y - (-843.9746)) < 0.001 AND ABS(z - (143.43298)) < 0.001)
  OR (questChecklistIdx = 5 AND ABS(x - (18.839134)) < 0.001 AND ABS(y - (-840.56036)) < 0.001 AND ABS(z - (-113.37005)) < 0.001)
  OR (questChecklistIdx = 6 AND ABS(x - (-17.037079)) < 0.001 AND ABS(y - (-843.8909)) < 0.001 AND ABS(z - (46.10919)) < 0.001)
)
UNION ALL SELECT 'entity_laughingws_housing_skyplot_wip', COUNT(*) FROM entity WHERE id BETWEEN @laughingws_housing_skyplot_entity_id_base AND @laughingws_housing_skyplot_entity_id_max
UNION ALL SELECT 'entity_stats_laughingws_housing_skyplot_wip', COUNT(*) FROM entity_stats WHERE id BETWEEN @laughingws_housing_skyplot_entity_id_base AND @laughingws_housing_skyplot_entity_id_max
UNION ALL SELECT 'entity_vendor_laughingws_housing_skyplot_wip', COUNT(*) FROM entity_vendor WHERE id BETWEEN @laughingws_housing_skyplot_entity_id_base AND @laughingws_housing_skyplot_entity_id_max
UNION ALL SELECT 'entity_vendor_category_laughingws_housing_skyplot_wip', COUNT(*) FROM entity_vendor_category WHERE id BETWEEN @laughingws_housing_skyplot_entity_id_base AND @laughingws_housing_skyplot_entity_id_max
UNION ALL SELECT 'entity_vendor_item_laughingws_housing_skyplot_wip', COUNT(*) FROM entity_vendor_item WHERE id BETWEEN @laughingws_housing_skyplot_entity_id_base AND @laughingws_housing_skyplot_entity_id_max
UNION ALL SELECT 'expected_entity_laughingws_housing_skyplot_wip_3_mismatch', IF(COUNT(*) = 3, 0, 1) FROM entity WHERE id BETWEEN @laughingws_housing_skyplot_entity_id_base AND @laughingws_housing_skyplot_entity_id_max
UNION ALL SELECT 'expected_entity_stats_laughingws_housing_skyplot_wip_12_mismatch', IF(COUNT(*) = 12, 0, 1) FROM entity_stats WHERE id BETWEEN @laughingws_housing_skyplot_entity_id_base AND @laughingws_housing_skyplot_entity_id_max
UNION ALL SELECT 'expected_entity_vendor_laughingws_housing_skyplot_wip_2_mismatch', IF(COUNT(*) = 2, 0, 1) FROM entity_vendor WHERE id BETWEEN @laughingws_housing_skyplot_entity_id_base AND @laughingws_housing_skyplot_entity_id_max
UNION ALL SELECT 'expected_entity_vendor_category_laughingws_housing_skyplot_wip_72_mismatch', IF(COUNT(*) = 72, 0, 1) FROM entity_vendor_category WHERE id BETWEEN @laughingws_housing_skyplot_entity_id_base AND @laughingws_housing_skyplot_entity_id_max
UNION ALL SELECT 'expected_entity_vendor_item_laughingws_housing_skyplot_wip_322_mismatch', IF(COUNT(*) = 322, 0, 1) FROM entity_vendor_item WHERE id BETWEEN @laughingws_housing_skyplot_entity_id_base AND @laughingws_housing_skyplot_entity_id_max
UNION ALL SELECT 'entity_laughingws_live_event_wip', COUNT(*) FROM entity WHERE id BETWEEN @laughingws_live_event_entity_id_base AND @laughingws_live_event_entity_id_max
UNION ALL SELECT 'entity_stats_laughingws_live_event_wip', COUNT(*) FROM entity_stats WHERE id BETWEEN @laughingws_live_event_entity_id_base AND @laughingws_live_event_entity_id_max
UNION ALL SELECT 'entity_vendor_laughingws_live_event_wip', COUNT(*) FROM entity_vendor WHERE id BETWEEN @laughingws_live_event_entity_id_base AND @laughingws_live_event_entity_id_max
UNION ALL SELECT 'entity_vendor_category_laughingws_live_event_wip', COUNT(*) FROM entity_vendor_category WHERE id BETWEEN @laughingws_live_event_entity_id_base AND @laughingws_live_event_entity_id_max
UNION ALL SELECT 'entity_vendor_item_laughingws_live_event_wip', COUNT(*) FROM entity_vendor_item WHERE id BETWEEN @laughingws_live_event_entity_id_base AND @laughingws_live_event_entity_id_max
UNION ALL SELECT 'expected_entity_laughingws_live_event_wip_196_mismatch', IF(COUNT(*) = 196, 0, 1) FROM entity WHERE id BETWEEN @laughingws_live_event_entity_id_base AND @laughingws_live_event_entity_id_max
UNION ALL SELECT 'expected_entity_stats_laughingws_live_event_wip_68_mismatch', IF(COUNT(*) = 68, 0, 1) FROM entity_stats WHERE id BETWEEN @laughingws_live_event_entity_id_base AND @laughingws_live_event_entity_id_max
UNION ALL SELECT 'expected_entity_vendor_laughingws_live_event_wip_8_mismatch', IF(COUNT(*) = 8, 0, 1) FROM entity_vendor WHERE id BETWEEN @laughingws_live_event_entity_id_base AND @laughingws_live_event_entity_id_max
UNION ALL SELECT 'expected_entity_vendor_category_laughingws_live_event_wip_24_mismatch', IF(COUNT(*) = 24, 0, 1) FROM entity_vendor_category WHERE id BETWEEN @laughingws_live_event_entity_id_base AND @laughingws_live_event_entity_id_max
UNION ALL SELECT 'expected_entity_vendor_item_laughingws_live_event_wip_142_mismatch', IF(COUNT(*) = 142, 0, 1) FROM entity_vendor_item WHERE id BETWEEN @laughingws_live_event_entity_id_base AND @laughingws_live_event_entity_id_max
UNION ALL SELECT 'creature_loot', COUNT(*) FROM creature_loot
UNION ALL SELECT 'loot_group_mapped_creature', COUNT(*) FROM loot_group WHERE comment LIKE 'DataMapping creature_loot%'
UNION ALL SELECT 'entity_loot_mapped', COUNT(*) FROM entity_loot WHERE comment LIKE 'DataMapping creature_loot%'
UNION ALL SELECT 'loot_item_mapped_creature', COUNT(*) FROM loot_item li JOIN loot_group lg ON lg.id = li.id WHERE lg.comment LIKE 'DataMapping creature_loot%'
UNION ALL SELECT 'loot_group_mapped_item', COUNT(*) FROM loot_group WHERE comment LIKE 'DataMapping item_container%'
UNION ALL SELECT 'item_loot_mapped', COUNT(*) FROM item_loot WHERE comment LIKE 'DataMapping item_container%'
UNION ALL SELECT 'loot_item_mapped_item', COUNT(*) FROM loot_item li JOIN loot_group lg ON lg.id = li.id WHERE lg.comment LIKE 'DataMapping item_container%'
UNION ALL SELECT 'item_salvage_exact_mapped', COUNT(*) FROM item_salvage WHERE purpose = 0 AND comment LIKE 'DataMapping item_salvage%'
UNION ALL SELECT 'item_salvage_type_level_mapped', COUNT(*) FROM item_salvage WHERE purpose = 1 AND comment LIKE 'DataMapping item_salvage%'
UNION ALL SELECT 'item_salvage_mapped', COUNT(*) FROM item_salvage WHERE comment LIKE 'DataMapping item_salvage%'
UNION ALL SELECT 'loot_group_laughingws_quest', COUNT(*) FROM loot_group WHERE comment LIKE 'LaughingWS quest loot:%'
UNION ALL SELECT 'entity_loot_laughingws_quest', COUNT(*) FROM entity_loot WHERE comment LIKE 'LaughingWS quest loot:%'
UNION ALL SELECT 'loot_item_laughingws_quest', COUNT(*) FROM loot_item li JOIN loot_group lg ON lg.id = li.id WHERE lg.comment LIKE 'LaughingWS quest loot:%'
UNION ALL SELECT 'expected_loot_group_laughingws_quest_1174_mismatch', IF(COUNT(*) = 1174, 0, 1) FROM loot_group WHERE comment LIKE 'LaughingWS quest loot:%'
UNION ALL SELECT 'expected_entity_loot_laughingws_quest_5302_mismatch', IF(COUNT(*) = 5302, 0, 1) FROM entity_loot WHERE comment LIKE 'LaughingWS quest loot:%'
UNION ALL SELECT 'expected_loot_item_laughingws_quest_1174_mismatch', IF(COUNT(*) = 1174, 0, 1) FROM loot_item li JOIN loot_group lg ON lg.id = li.id WHERE lg.comment LIKE 'LaughingWS quest loot:%'
UNION ALL SELECT 'store_offer_group_laughingws_valentine', COUNT(*) FROM store_offer_group WHERE id IN (1827, 2089, 3049, 3050)
UNION ALL SELECT 'store_offer_group_laughingws_shades_eve', COUNT(*) FROM store_offer_group WHERE id IN (1724, 1726, 1727, 2093, 2100, 2915, 2916, 2917, 2918, 2919)
UNION ALL SELECT 'store_offer_group_laughingws_winterfest', COUNT(*) FROM store_offer_group WHERE id IN (1839, 1841, 1899, 2074, 2076, 2077, 2091, 2968, 2969, 2970, 2971, 2972, 2985, 3312, 3315, 3318, 3327, 3328, 3329, 3330, 3332)
UNION ALL SELECT 'expected_store_offer_group_laughingws_valentine_4_mismatch', IF(COUNT(*) = 4, 0, 1) FROM store_offer_group WHERE id IN (1827, 2089, 3049, 3050)
UNION ALL SELECT 'expected_store_offer_group_laughingws_shades_eve_10_mismatch', IF(COUNT(*) = 10, 0, 1) FROM store_offer_group WHERE id IN (1724, 1726, 1727, 2093, 2100, 2915, 2916, 2917, 2918, 2919)
UNION ALL SELECT 'expected_store_offer_group_laughingws_winterfest_21_mismatch', IF(COUNT(*) = 21, 0, 1) FROM store_offer_group WHERE id IN (1839, 1841, 1899, 2074, 2076, 2077, 2091, 2968, 2969, 2970, 2971, 2972, 2985, 3312, 3315, 3318, 3327, 3328, 3329, 3330, 3332)
UNION ALL SELECT 'creature_info_property', COUNT(*) FROM creature_info_property
UNION ALL SELECT 'creature_info_stat', COUNT(*) FROM creature_info_stat
UNION ALL SELECT 'entity_visible_duplicate_extras', COALESCE(SUM(duplicate_groups.row_count - 1), 0) FROM (
  SELECT COUNT(*) AS row_count
  FROM entity
  GROUP BY world, area, type, creature, ROUND(x, 3), ROUND(y, 3), ROUND(z, 3), displayInfo, outfitInfo, faction1, faction2, questChecklistIdx, activePropId, worldSocketId, mode
  HAVING COUNT(*) > 1
) duplicate_groups
UNION ALL SELECT 'expected_entity_visible_duplicate_extras_0_mismatch', IF(COALESCE(SUM(duplicate_groups.row_count - 1), 0) = 0, 0, 1) FROM (
  SELECT COUNT(*) AS row_count
  FROM entity
  GROUP BY world, area, type, creature, ROUND(x, 3), ROUND(y, 3), ROUND(z, 3), displayInfo, outfitInfo, faction1, faction2, questChecklistIdx, activePropId, worldSocketId, mode
  HAVING COUNT(*) > 1
) duplicate_groups
UNION ALL SELECT 'nf_map_tables', COUNT(*) FROM information_schema.TABLES WHERE TABLE_SCHEMA = 'nexus_forever_mapping' AND TABLE_NAME LIKE 'nf_map_%';

SELECT match_status, COUNT(*) AS row_count
FROM nexus_forever_mapping.nf_map_creature
GROUP BY match_status
ORDER BY match_status;

SELECT 'safe_vendor_rows' AS metric, COUNT(*) AS value
FROM nexus_forever_mapping.nf_map_vendor_item
WHERE match_status IN ('unique_name', 'scored_name', 'reviewed')
  AND IFNULL(creature2_id, 0) > 0
  AND IFNULL(item2_id, 0) > 0
UNION ALL
SELECT 'safe_loot_rows', COUNT(*)
FROM nexus_forever_mapping.nf_map_creature_loot
WHERE match_status IN ('unique_name', 'scored_name', 'reviewed')
  AND IFNULL(creature2_id, 0) > 0
  AND IFNULL(item2_id, 0) > 0
UNION ALL
SELECT 'runtime_loot_group_creatures', COUNT(DISTINCT creatureId)
FROM creature_loot
WHERE chance > 0
UNION ALL
SELECT 'safe_creature_rows', COUNT(*)
FROM nexus_forever_mapping.nf_map_creature
WHERE match_status IN ('unique_name', 'scored_name', 'reviewed')
  AND IFNULL(creature2_id, 0) > 0
UNION ALL
SELECT 'safe_item_container_rows', COUNT(*)
FROM nexus_forever_mapping.nf_map_item_container
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
FROM nexus_forever_mapping.nf_map_creature m
JOIN jabbithole.creatures c ON c.id = m.jabbithole_creature_id
WHERE c.zone_id = 1
  AND c.enabled = 'true'
  AND m.match_status IN ('unique_name', 'scored_name', 'reviewed')
  AND IFNULL(m.creature2_id, 0) > 0
UNION ALL
SELECT 'northern_wilds_resolved_creature_bridges', COUNT(DISTINCT c.id)
FROM jabbithole.creatures c
JOIN nexus_forever_mapping.nf_map_creature m ON m.jabbithole_creature_id = c.id
WHERE c.zone_id = 1
  AND c.enabled = 'true'
  AND IFNULL(m.creature2_id, 0) > 0
UNION ALL
SELECT 'northern_wilds_runtime_creatures', COUNT(DISTINCT c.id)
FROM jabbithole.creatures c
JOIN nexus_forever_mapping.nf_map_creature m ON m.jabbithole_creature_id = c.id
JOIN entity e ON e.world = 426 AND e.creature = m.creature2_id
WHERE c.zone_id = 1
  AND c.enabled = 'true'
  AND IFNULL(m.creature2_id, 0) > 0
UNION ALL
SELECT 'northern_wilds_runtime_missing_creatures', COUNT(DISTINCT c.id)
FROM jabbithole.creatures c
LEFT JOIN nexus_forever_mapping.nf_map_creature m ON m.jabbithole_creature_id = c.id
LEFT JOIN entity e ON e.world = 426 AND e.creature = m.creature2_id
WHERE c.zone_id = 1
  AND c.enabled = 'true'
  AND e.id IS NULL
UNION ALL
SELECT 'northern_wilds_unmatched_creature_bridges', COUNT(DISTINCT c.id)
FROM jabbithole.creatures c
LEFT JOIN nexus_forever_mapping.nf_map_creature m ON m.jabbithole_creature_id = c.id
WHERE c.zone_id = 1
  AND c.enabled = 'true'
  AND IFNULL(m.creature2_id, 0) = 0;
