#!/usr/bin/env python3
"""
Build cross-reference maps from the split WildStar client and Jabbithole dumps.

The client dump is the source of stable game IDs and localized text. Jabbithole is
the source of observed relationships such as spawns, loot, vendors, quests, path
missions, public events, and creature spell usage.
"""

from __future__ import annotations

import argparse
import csv
import difflib
import json
import math
import re
import subprocess
import sys
import time
from collections import Counter, defaultdict
from pathlib import Path
from typing import Dict, Iterable, Iterator, List, Optional, Sequence, Tuple


DEFAULT_MYSQL = r"C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe"
REVIEWED_MATCH_STATUS = "reviewed"
APPROVED_REVIEW_DECISIONS = {"approve", "approved", "use", "map", "mapped", "reviewed"}
TRACKED_CREATURE_BRIDGE_OVERRIDES = Path("Tools/DataMapping/creature_bridge_overrides.csv")

CREATURE_BRIDGE_OVERRIDE_FIELDS = [
    "source_table",
    "source_id",
    "source_name",
    "chosen_creature2_id",
    "decision",
    "reason",
    "reviewer",
    "reviewed_at",
]

CREATURE_BRIDGE_REVIEW_FIELDS = [
    "source_table",
    "source_id",
    "source_name",
    "current_status",
    "current_creature2_id",
    "current_client_name",
    "candidate_reason",
    "candidate_rank",
    "candidate_creature2_id",
    "candidate_client_name",
    "candidate_score",
    "score_delta_from_best",
    "name_similarity",
    "source_faction",
    "candidate_faction",
    "faction_match",
    "source_level_min",
    "source_level_max",
    "candidate_min_level",
    "candidate_max_level",
    "level_overlap",
    "exact_level_match",
    "source_difficulty",
    "candidate_difficulty",
    "difficulty_match",
    "source_datacube_id",
    "candidate_datacube_id",
    "datacube_match",
    "source_zone_id",
    "source_worldid",
    "source_worldzoneid",
    "decision",
    "reason",
    "reviewer",
    "reviewed_at",
]

CREATURE_FIELDS = [
    "ID",
    "description",
    "localizedTextIdName",
    "creature2DifficultyId",
    "creature2ArcheTypeId",
    "creature2TierId",
    "creature2ModelInfoId",
    "creature2DisplayGroupId",
    "creature2OutfitGroupId",
    "factionId",
    "minLevel",
    "maxLevel",
    "creature2AffiliationId",
    "creature2ActionSetId",
    "creature2ActionTextId",
    "pathMissionIdSoldier",
    "datacubeId",
    "bindPointId",
    "taxiNodeId",
    "instancePortalId",
    "unitVehicleId",
    "tradeskillHarvestingInfoId",
]

ITEM_FIELDS = [
    "ID",
    "localizedTextIdName",
    "itemQualityId",
    "item2FamilyId",
    "item2CategoryId",
    "item2TypeId",
    "requiredLevel",
    "maxStackCount",
    "buyFromVendorStackCount",
    "currencyTypeId0",
    "currencyTypeId1",
    "currencyAmount0",
    "currencyAmount1",
    "currencyTypeId0SellToVendor",
    "currencyTypeId1SellToVendor",
    "currencyAmount0SellToVendor",
    "currencyAmount1SellToVendor",
]

ITEM_FAMILY_FIELDS = [
    "ID",
    "localizedTextId",
    "flags",
    "vendorMultiplier",
    "turninMultiplier",
]

ITEM_CATEGORY_FIELDS = [
    "ID",
    "localizedTextId",
    "itemProficiencyId",
    "flags",
    "tradeSkillId",
    "vendorMultiplier",
    "turninMultiplier",
    "armorModifier",
    "armorBase",
    "weaponPowerModifier",
    "weaponPowerBase",
    "item2FamilyId",
]

ITEM_TYPE_FIELDS = [
    "ID",
    "localizedTextId",
    "itemSlotId",
    "flags",
    "vendorMultiplier",
    "turninMultiplier",
    "Item2CategoryId",
]

ITEM_SLOT_FIELDS = [
    "ID",
    "EnumName",
    "equippedSlotFlags",
    "armorModifier",
    "itemLevelModifier",
    "slotBonus",
    "glyphSlotBonus",
    "minLevel",
]

CLASS_FIELDS = [
    "ID",
    "enumName",
    "localizedTextId",
    "localizedTextIdDescription",
    "mechanic",
    "spell4IdInnateAbilityActive00",
    "spell4IdInnateAbilityActive01",
    "spell4IdInnateAbilityActive02",
    "spell4IdInnateAbilityPassive00",
    "spell4IdInnateAbilityPassive01",
    "spell4IdInnateAbilityPassive02",
    "spell4IdAttackPrimary00",
    "spell4IdAttackPrimary01",
    "spell4IdAttackUnarmed00",
    "spell4IdAttackUnarmed01",
    "spell4IdResAbility",
]

UNIT_PROPERTY_FIELDS = [
    "ID",
    "description",
    "enumName",
    "localizedTextId",
    "valuePerPoint",
    "flags",
    "tooltipDisplayOrder",
    "equippedSlotFlags",
]

QUEST_FIELDS = [
    "ID",
    "localizedTextIdTitle",
    "worldZoneId",
    "type",
    "conLevel",
    "preq_level",
    "reward_xpOverride",
    "reward_cashOverride",
    "quest2DifficultyId",
    "quest2SubTypeId",
    "subMissionPathType",
    "objective0",
    "objective01",
    "objective02",
    "objective03",
    "objective04",
    "objective05",
]

QUEST_OBJECTIVE_FIELDS = [
    "ID",
    "type",
    "flags",
    "data",
    "count",
    "localizedTextIdFull",
    "worldLocationsIdIndicator00",
    "worldLocationsIdIndicator01",
    "worldLocationsIdIndicator02",
    "worldLocationsIdIndicator03",
    "maxTimeAllowedMS",
    "localizedTextIdShort",
    "targetGroupIdRewardPane",
    "questDirectionId",
]

QUEST_REWARD_FIELDS = [
    "ID",
    "quest2Id",
    "quest2RewardTypeId",
    "objectId",
    "objectAmount",
    "flags",
]

NAMED_ID_FIELDS = [
    "ID",
    "localizedTextIdName",
]

PATH_MISSION_FIELDS = [
    "ID",
    "creature2IdUnlock",
    "pathTypeEnum",
    "pathMissionTypeEnum",
    "pathMissionDisplayTypeEnum",
    "objectId",
    "localizedTextIdName",
    "localizedTextIdSummary",
    "pathEpisodeId",
    "worldLocation2Id00",
    "worldLocation2Id01",
    "worldLocation2Id02",
    "worldLocation2Id03",
    "pathMissionFlags",
    "pathMissionFactionEnum",
    "creature2IdContactOverride",
]

PATH_EPISODE_FIELDS = [
    "ID",
    "localizedTextIdName",
    "localizedTextIdSummary",
    "worldId",
    "worldZoneId",
    "pathTypeEnum",
]

PATH_REWARD_FIELDS = [
    "ID",
    "pathRewardTypeEnum",
    "objectId",
    "spell4Id",
    "item2Id",
    "quest2Id",
    "characterTitleId",
    "prerequisiteId",
    "count",
    "pathRewardFlags",
    "pathScientistScanBotProfileId",
]

SPELL4_FIELDS = [
    "ID",
    "description",
    "spell4BaseIdBaseSpell",
    "tierIndex",
    "castTime",
    "spellDuration",
    "spellCoolDown",
    "targetMinRange",
    "targetMaxRange",
    "innateCostType0",
    "innateCostType1",
    "innateCost0",
    "innateCost1",
    "localizedTextIdActionBarTooltip",
    "abilityChargeCount",
    "abilityRechargeTime",
    "abilityRechargeCount",
    "abilityPointCost",
    "trainingCost",
    "spell4IdMechanicAlternateSpell",
    "localizedTextIdTooltipCastInfo",
    "localizedTextIdTooltipCostInfo",
    "spellCoolDownIdGlobal",
    "spellCoolDownId00",
    "spellCoolDownId01",
    "globalCooldownEnum",
    "propertyFlags",
    "uiFlags",
]

SPELL4_BASE_FIELDS = [
    "ID",
    "localizedTextIdName",
    "spell4HitResultId",
    "spell4TargetMechanicId",
    "spell4TargetAngleId",
    "spell4PrerequisiteId",
    "spell4ValidTargetId",
    "targetGroupIdCastGroup",
    "creature2IdPositionalAoe",
    "parameterAEAngle",
    "parameterAEMaxAngle",
    "parameterAEDistance",
    "parameterAEMaxDistance",
    "targetGroupIdAoeGroup",
    "spell4BaseIdPrerequisiteSpell",
    "worldZoneIdZoneRequired",
    "spell4SpellTypesIdSpellType",
    "icon",
    "castMethod",
    "school",
    "spellClass",
    "weaponSlot",
    "castBarType",
    "classIdPlayer",
    "targetingFlags",
    "telegraphFlagsEnum",
]

SPELL4_EFFECT_FIELDS = [
    "ID",
    "spellId",
    "targetFlags",
    "effectType",
    "damageType",
    "delayTime",
    "tickTime",
    "durationTime",
    "flags",
    "dataBits00",
    "dataBits01",
    "dataBits02",
    "dataBits03",
    "dataBits04",
    "dataBits05",
    "dataBits06",
    "dataBits07",
    "dataBits08",
    "dataBits09",
    "threatMultiplier",
    "parameterType00",
    "parameterType01",
    "parameterType02",
    "parameterType03",
    "parameterValue00",
    "parameterValue01",
    "parameterValue02",
    "parameterValue03",
    "phaseFlags",
    "orderIndex",
]

SPELL_EFFECT_TYPE_FIELDS = [
    "ID",
    "flags",
    "dataType00",
    "dataType01",
    "dataType02",
    "dataType03",
    "dataType04",
    "dataType05",
    "dataType06",
    "dataType07",
    "dataType08",
    "dataType09",
]

SPELL_LEVEL_FIELDS = [
    "ID",
    "classId",
    "characterLevel",
    "prerequisiteId",
    "spell4Id",
    "costMultiplier",
]

SPELL_TIER_REQUIREMENT_FIELDS = [
    "ID",
    "tierIndex",
    "levelRequirement",
]

SPELL_TYPE_FIELDS = [
    "ID",
    "typeName",
    "enumName",
]

PATH_LEVEL_FIELDS = [
    "ID",
    "pathTypeEnum",
    "pathLevel",
    "pathXP",
]

CHARACTER_TITLE_FIELDS = [
    "ID",
    "characterTitleCategoryId",
    "localizedTextIdName",
    "localizedTextIdTitle",
    "spell4IdActivate",
    "lifeTimeSeconds",
    "playerTitleFlagsEnum",
]

CHARACTER_TITLE_CATEGORY_FIELDS = [
    "ID",
    "localizedTextId",
]

ACHIEVEMENT_FIELDS = [
    "ID",
    "achievementTypeId",
    "achievementCategoryId",
    "flags",
    "worldZoneId",
    "localizedTextIdTitle",
    "localizedTextIdDesc",
    "localizedTextIdProgress",
    "percCompletionToShow",
    "objectId",
    "objectIdAlt",
    "value",
    "characterTitleId",
    "prerequisiteId",
    "prerequisiteIdServer",
    "prerequisiteIdObjective",
    "prerequisiteIdObjectiveAlt",
    "achievementIdParentTier",
    "orderIndex",
    "achievementGroupId",
    "achievementSubGroupId",
    "achievementPointEnum",
    "steamAchievementName",
]

ACHIEVEMENT_CHECKLIST_FIELDS = [
    "ID",
    "achievementId",
    "bit",
    "objectId",
    "objectIdAlt",
    "prerequisiteId",
    "prerequisiteIdAlt",
]

ACHIEVEMENT_GROUP_FIELDS = [
    "ID",
    "localizedTextId",
    "tradeSkillId",
]

ACHIEVEMENT_CATEGORY_FIELDS = [
    "ID",
    "localizedTextId",
    "localizedTextIdFullName",
    "achievementCategoryIdParent",
]

ACHIEVEMENT_SUBGROUP_FIELDS = [
    "ID",
    "localizedTextId",
    "tier",
]

ACHIEVEMENT_TEXT_FIELDS = [
    "ID",
    "localizedTextId",
]

HOUSING_DECOR_TYPE_FIELDS = [
    "ID",
    "localizedTextId",
    "luaString",
]

HOUSING_DECOR_INFO_FIELDS = [
    "ID",
    "housingDecorTypeId",
    "hookTypeId",
    "localizedTextIdName",
    "flags",
    "hookAssetId",
    "cost",
    "costCurrencyTypeId",
    "creature2IdActiveProp",
    "prerequisiteIdUnlock",
    "spell4IdInteriorBuff",
    "housingDecorLimitCategoryId",
    "altPreviewAsset",
    "altEditAsset",
    "minScale",
    "maxScale",
]

HOUSING_DECOR_LIMIT_CATEGORY_FIELDS = [
    "ID",
    "decorLimit",
]

HOUSING_PLUG_ITEM_FIELDS = [
    "ID",
    "localizedTextIdName",
    "housingPlotTypeId",
    "localizedTextIdTooltip",
    "worldIdPlug00",
    "worldIdPlug01",
    "worldIdPlug02",
    "worldIdPlug03",
    "flags",
    "housingResourceIdProvided00",
    "housingResourceIdProvided01",
    "housingResourceIdProvided02",
    "housingResourceIdProvided03",
    "housingResourceIdProvided04",
    "housingResourceIdPrerequisite00",
    "housingResourceIdPrerequisite01",
    "housingResourceIdPrerequisite02",
    "housingFeatureTypeFlags",
    "housingContributionInfoId00",
    "housingContributionInfoId01",
    "housingContributionInfoId02",
    "housingContributionInfoId03",
    "housingContributionInfoId04",
    "housingPlugItemIdNextUpgrade",
    "prerequisiteIdUnlock",
    "housingBuildId",
    "housingUpkeepTypeEnum",
    "upkeepCharges",
    "upkeepTime",
    "housingPlugTypeEnum",
    "accountItemIdUpsell",
    "screenshotSprite00",
    "screenshotSprite01",
    "screenshotSprite02",
    "screenshotSprite03",
    "screenshotSprite04",
]

HOUSING_WALLPAPER_FIELDS = [
    "ID",
    "localizedTextId",
    "cost",
    "costCurrencyTypeId",
    "replaceableMaterialInfoId",
    "worldSkyId",
    "flags",
    "prerequisiteIdUnlock",
    "prerequisiteIdUse",
    "unlockIndex",
    "soundZoneKitId",
    "accountItemIdUpsell",
]

HOUSING_RESIDENCE_FIELDS = [
    "ID",
    "housingDecorInfoIdDefaultRoof",
    "housingDecorInfoIdDefaultEntryway",
    "housingDecorInfoIdDefaultDoor",
    "housingWallpaperInfoIdDefault",
    "worldLocation2IdInside00",
    "worldLocation2IdOutside00",
]

HOUSING_BUILD_FIELDS = [
    "ID",
    "description",
    "assetPath",
    "constructionEffectsId",
    "buildPreDelayTimeMS",
    "buildPostDelayTimeMS",
    "buildTime00",
    "buildTime01",
    "buildTime02",
    "buildTime03",
]

HOUSING_RESOURCE_FIELDS = [
    "ID",
    "localizedTextIdName",
]

HOUSING_CONTRIBUTION_TYPE_FIELDS = [
    "ID",
    "description",
    "enumName",
]

HOUSING_CONTRIBUTION_INFO_FIELDS = [
    "ID",
    "housingContributionTypeId",
    "contributionPointRequirement",
    "item2IdTier00",
    "item2IdTier01",
    "item2IdTier02",
    "item2IdTier03",
    "item2IdTier04",
    "contributionPointValueTier00",
    "contributionPointValueTier01",
    "contributionPointValueTier02",
    "contributionPointValueTier03",
    "contributionPointValueTier04",
]

HOUSING_PROPERTY_FIELDS = [
    "ID",
    "localizedTextIdName",
    "worldId",
    "housingMapInfoId",
    "cost",
    "housingFacingEnum",
    "worldLocation2Id",
    "worldZoneId",
    "housingPropertyTypeId",
]

HOUSING_NEIGHBORHOOD_FIELDS = [
    "ID",
    "baseCost",
    "maxPopulation",
    "populationThreshold",
    "housingFactionEnum",
    "housingFeatureTypeEnum",
    "housingPlaystyleTypeEnum",
    "housingMapInfoIdPrimary",
    "housingMapInfoIdSecondary",
]

HOUSING_MAP_FIELDS = [
    "ID",
    "worldId",
    "privatePropertyCount",
    "publicPropertyCount",
]

HOUSING_PLOT_TYPE_FIELDS = [
    "ID",
    "maxPlacedDecor",
]

HOUSING_PLOT_INFO_FIELDS = [
    "ID",
    "worldSocketId",
    "plotType",
    "housingPropertyInfoId",
    "housingPropertyPlotIndex",
    "housingPlugItemIdDefault",
]

HOUSING_MANNEQUIN_POSE_FIELDS = [
    "ID",
    "enumName",
    "localizedTextId",
    "modelSequenceId",
]

HOUSING_WARPLOT_BOSS_TOKEN_FIELDS = [
    "ID",
    "spell4IdSummon",
    "minimumUpgradeTierEnum",
    "housingPlugItemIdLinked",
]

HOUSING_WARPLOT_PLUG_INFO_FIELDS = [
    "ID",
    "housingPlugItemId",
    "maintenanceCost",
    "upgradeCost00",
    "upgradeCost01",
    "upgradeCost02",
    "spell4IdAbility00",
    "spell4IdAbility01",
    "spell4IdAbility02",
    "spell4IdAbility03",
    "spell4IdAbility04",
    "spell4IdAbility05",
    "spell4IdAbility06",
    "spell4IdAbility07",
    "spell4IdAbility08",
    "spell4IdAbility09",
    "spell4IdAbility10",
    "spell4IdAbility11",
]

DYE_COLOR_RAMP_FIELDS = [
    "ID",
    "flags",
    "localizedTextIdName",
    "rampIndex",
    "costMultiplier",
    "componentMapEnum",
    "prerequisiteId",
]

PET_FLAIR_FIELDS = [
    "ID",
    "unlockBitIndex00",
    "unlockBitIndex01",
    "type",
    "spell4Id",
    "localizedTextIdTooltip",
    "itemDisplayId00",
    "itemDisplayId01",
    "prerequisiteId",
]

TRADESKILL_FIELDS = [
    "ID",
    "localizedTextIdName",
    "localizedTextIdDescription",
    "flags",
    "tutorialId",
    "achievementCategoryId",
    "maxAdditives",
    "localizedTextIdAxisName00",
    "localizedTextIdAxisName01",
    "localizedTextIdAxisName02",
    "localizedTextIdAxisName03",
]

TRADESKILL_SCHEMATIC_FIELDS = [
    "ID",
    "localizedTextIdName",
    "tradeSkillId",
    "item2IdOutput",
    "item2IdOutputFail",
    "outputCount",
    "lootId",
    "tier",
    "flags",
    "item2IdMaterial00",
    "item2IdMaterial01",
    "item2IdMaterial02",
    "item2IdMaterial03",
    "item2IdMaterial04",
    "materialCost00",
    "materialCost01",
    "materialCost02",
    "materialCost03",
    "materialCost04",
    "tradeskillSchematic2IdParent",
    "vectorX",
    "vectorY",
    "radius",
    "critRadius",
    "item2IdOutputCrit",
    "outputCountCritBonus",
    "priority",
    "maxAdditives",
    "discoverableQuadrant",
    "discoverableRadius",
    "discoverableAngle",
    "tradeskillCatalystOrderingId",
]

TRADESKILL_MATERIAL_FIELDS = [
    "ID",
    "item2IdStatRevolution",
    "item2Id",
    "displayIndex",
    "tradeskillMaterialCategoryId",
]

TRADESKILL_MATERIAL_CATEGORY_FIELDS = [
    "ID",
    "localizedTextIdName",
]

TRADESKILL_TALENT_TIER_FIELDS = [
    "ID",
    "tradeSkillId",
    "pointsToUnlock",
    "respecCost",
    "tradeSkillBonusId00",
    "tradeSkillBonusId01",
    "tradeSkillBonusId02",
    "tradeSkillBonusId03",
    "tradeSkillBonusId04",
]

TRADESKILL_TIER_FIELDS = [
    "ID",
    "tradeSkillId",
    "tier",
    "requiredXp",
    "learnXp",
    "craftXp",
    "firstCraftXp",
    "questXp",
    "failXp",
    "itemLevelMin",
    "maxAdditives",
    "relearnCost",
    "achievementCategoryId",
]

TRADESKILL_ADDITIVE_FIELDS = [
    "ID",
    "tradeSkillId",
    "tier",
    "vectorX",
    "vectorY",
    "radius",
]

TRADESKILL_CATALYST_FIELDS = [
    "ID",
    "tradeSkillId",
    "tier",
    "tradeskillCatalystEnum00",
    "tradeskillCatalystEnum01",
    "tradeskillCatalystEnum02",
    "tradeskillCatalystEnum03",
    "tradeskillCatalystEnum04",
    "value00",
    "value01",
    "value02",
    "value03",
    "value04",
]

TRADESKILL_CATALYST_ORDERING_FIELDS = [
    "ID",
    "unlockLevel00",
    "unlockLevel01",
    "unlockLevel02",
    "unlockLevel03",
    "unlockLevel04",
]

TRADESKILL_BONUS_FIELDS = [
    "ID",
    "tradeSkillTierId",
    "achievementId",
    "iconPath",
    "localizedTextIdName",
    "localizedTextIdTooltip",
    "tradeskillBonusEnum00",
    "tradeskillBonusEnum01",
    "tradeskillBonusEnum02",
    "objectIdPrimary00",
    "objectIdPrimary01",
    "objectIdPrimary02",
    "objectIdSecondary00",
    "objectIdSecondary01",
    "objectIdSecondary02",
    "objectIdTertiary00",
    "objectIdTertiary01",
    "objectIdTertiary02",
    "value00",
    "value01",
    "value02",
    "valueInt00",
    "valueInt01",
    "valueInt02",
]

TRADESKILL_ACHIEVEMENT_LAYOUT_FIELDS = [
    "ID",
    "achievementId",
    "achievementIdParent00",
    "achievementIdParent01",
    "achievementIdParent02",
    "achievementIdParent03",
    "achievementIdParent04",
    "gridX",
    "gridY",
]

TRADESKILL_ACHIEVEMENT_REWARD_FIELDS = [
    "ID",
    "achievementId",
    "faction2Id",
    "factionIdAmount",
    "talentPoints",
    "tradeSkillSchematicId00",
    "tradeSkillSchematicId01",
    "tradeSkillSchematicId02",
    "tradeSkillSchematicId03",
    "tradeSkillSchematicId04",
    "tradeSkillSchematicId05",
    "tradeSkillSchematicId06",
    "tradeSkillSchematicId07",
]

PUBLIC_EVENT_FIELDS = [
    "ID",
    "worldId",
    "worldZoneId",
    "localizedTextIdName",
    "failureTimeMs",
    "worldLocation2Id",
    "publicEventTypeEnum",
    "publicEventIdParent",
    "minPlayerLevel",
    "publicEventFlags",
]

PUBLIC_EVENT_OBJECTIVE_FIELDS = [
    "ID",
    "publicEventId",
    "publicEventObjectiveFlags",
    "publicEventObjectiveTypeSpecificFlags",
    "worldLocation2Id",
    "publicEventTeamId",
    "localizedTextId",
    "localizedTextIdShort",
    "publicEventObjectiveTypeEnum",
    "count",
    "objectId",
    "failureTimeMs",
    "targetGroupIdRewardPane",
    "publicEventObjectiveCategoryEnum",
    "publicEventObjectiveIdParent",
    "questDirectionId",
    "medalPointValue",
    "displayOrder",
]

PUBLIC_EVENT_DEPOT_FIELDS = [
    "ID",
    "creature2Id",
    "item2Id",
]

PUBLIC_EVENT_VIRTUAL_ITEM_DEPOT_FIELDS = [
    "ID",
    "creature2Id",
    "virtualItemId00",
    "virtualItemId01",
    "virtualItemId02",
    "virtualItemId03",
    "virtualItemId04",
    "virtualItemId05",
]

VIRTUAL_ITEM_FIELDS = [
    "ID",
    "buttonIcon",
    "item2TypeId",
    "localizedTextIdName",
    "itemQualityId",
]

CHALLENGE_FIELDS = [
    "ID",
    "challengeTypeEnum",
    "target",
    "challengeFlags",
    "worldZoneIdRestriction",
    "triggerVolume2IdRestriction",
    "worldZoneId",
    "worldLocation2IdIndicator",
    "worldLocation2IdStartLocation",
    "completionCount",
    "challengeTierId00",
    "challengeTierId01",
    "challengeTierId02",
    "localizedTextIdName",
    "localizedTextIdProgress",
    "localizedTextIdAreaRestriction",
    "localizedTextIdLocation",
    "virtualItemIdDisplay",
    "targetGroupIdRewardPane",
    "questDirectionIdActive",
    "questDirectionIdInactive",
    "rewardTrackId",
]

CHALLENGE_TIER_FIELDS = [
    "ID",
    "count",
]

WORLD_FIELDS = [
    "ID",
    "assetPath",
    "flags",
    "type",
    "localizedTextIdName",
    "minItemLevel",
    "maxItemLevel",
]

WORLD_ZONE_FIELDS = [
    "ID",
    "localizedTextIdName",
    "parentZoneId",
    "allowAccess",
    "flags",
    "zonePvpRulesEnum",
]

MAP_CONTINENT_FIELDS = [
    "ID",
    "localizedTextIdName",
    "assetPath",
    "imagePath",
    "imageWidth",
    "imageHeight",
    "imageOffsetX",
    "imageOffsetY",
    "hexMinX",
    "hexMinY",
    "hexLimX",
    "hexLimY",
    "flags",
]

MAP_ZONE_FIELDS = [
    "ID",
    "localizedTextIdName",
    "mapContinentId",
    "folder",
    "hexMinX",
    "hexMinY",
    "hexLimX",
    "hexLimY",
    "version",
    "mapZoneIdParent",
    "worldZoneId",
    "flags",
    "prerequisiteIdVisibility",
    "rewardTrackId",
]

MAP_ZONE_POI_FIELDS = [
    "ID",
    "mapZoneId",
    "pos0",
    "pos1",
    "pos2",
    "localizedTextId",
    "mapZoneSpriteId",
]

MAP_ZONE_SPRITE_FIELDS = [
    "ID",
    "spriteName",
]

MAP_ZONE_WORLD_JOIN_FIELDS = [
    "ID",
    "mapZoneId",
    "worldId",
]

FACTION_FIELDS = [
    "ID",
    "faction2IdParent",
    "flags",
    "localizedTextIdName",
    "localizedTextIdToolTip",
    "orderIndex",
    "archiveArticleId",
]

FACTION_RELATIONSHIP_FIELDS = [
    "ID",
    "factionId0",
    "factionId1",
    "factionLevel",
]

QUEST_CATEGORY_FIELDS = [
    "ID",
    "description",
    "localizedTextIdTitle",
    "questCategoryTypeEnum",
]

EPISODE_FIELDS = [
    "ID",
    "localizedTextIdName",
    "localizedTextIdBriefing",
    "localizedTextIdEndSummary",
    "flags",
    "worldZoneId",
    "percentToDisplay",
    "questHubIdExile",
    "questHubIdDominion",
]

EPISODE_QUEST_FIELDS = [
    "ID",
    "episodeId",
    "questId",
    "orderIdx",
    "flags",
]

WORLD_LOCATION_FIELDS = [
    "ID",
    "radius",
    "maxVerticalDistance",
    "position0",
    "position1",
    "position2",
    "facing0",
    "facing1",
    "facing2",
    "facing3",
    "worldId",
    "worldZoneId",
    "phases",
]

BIND_POINT_FIELDS = [
    "ID",
    "bindPointFactionEnum",
    "localizedTextId",
]

TAXI_NODE_FIELDS = [
    "ID",
    "localizedTextId",
    "taxiNodeTypeEnum",
    "flags",
    "flightPathTypeEnum",
    "taxiNodeFactionEnum",
    "worldLocation2Id",
    "contentTier",
    "autoUnlockLevel",
    "recommendedMinLevel",
    "recommendedMaxLevel",
]

TAXI_ROUTE_FIELDS = [
    "ID",
    "taxiNodeIdSource",
    "taxiNodeIdDestination",
    "price",
]

CITY_DIRECTION_FIELDS = [
    "ID",
    "cityDirectionTypeEnum",
    "localizedTextIdName",
    "worldZoneId",
    "worldLocation2Id00",
    "worldLocation2Id01",
    "worldLocation2Id02",
    "worldLocation2Id03",
]

QUEST_DIRECTION_FIELDS = [
    "ID",
    "questDirectionFlags",
    "questDirectionEntryId00",
    "questDirectionEntryId01",
    "questDirectionEntryId02",
    "questDirectionEntryId03",
    "questDirectionEntryId04",
    "questDirectionEntryId05",
    "questDirectionEntryId06",
    "questDirectionEntryId07",
    "questDirectionEntryId08",
    "questDirectionEntryId09",
    "questDirectionEntryId10",
    "questDirectionEntryId11",
    "questDirectionEntryId12",
    "questDirectionEntryId13",
    "questDirectionEntryId14",
    "questDirectionEntryId15",
    "worldZoneIdExcludedZone",
]

QUEST_DIRECTION_ENTRY_FIELDS = [
    "ID",
    "worldLocation2Id",
    "worldLocation2IdInactive",
    "worldZoneId",
    "questDirectionEntryFlags",
    "questDirectionFactionEnum",
]

QUEST_HUB_FIELDS = [
    "ID",
    "worldLocation2Id",
    "localizedTextIdName",
]

GENERIC_MAP_FIELDS = [
    "ID",
    "mapZoneId",
]

GENERIC_MAP_NODE_FIELDS = [
    "ID",
    "genericMapId",
    "worldLocation2Id",
    "localizedTextIdName",
    "localizedTextIdDescription",
    "spritePath",
    "genericMapNodeTypeEnum",
    "flags",
]

MAP_ZONE_HEX_FIELDS = [
    "ID",
    "mapZoneId",
    "pos0",
    "pos1",
    "flags",
]

MAP_ZONE_HEX_GROUP_FIELDS = [
    "ID",
    "mapZoneId",
]

MAP_ZONE_HEX_GROUP_ENTRY_FIELDS = [
    "ID",
    "mapZoneHexGroupId",
    "hexX",
    "hexY",
]

MAP_ZONE_LEVEL_BAND_FIELDS = [
    "ID",
    "mapZoneHexGroupId",
    "levelMin",
    "levelMax",
    "labelX",
    "labelZ",
]

MAP_ZONE_NEMESIS_REGION_FIELDS = [
    "ID",
    "mapZoneHexGroupId",
    "localizedTextIdDescription",
    "faction2Id",
]

ZONE_COMPLETION_FIELDS = [
    "ID",
    "mapZoneId",
    "zoneCompletionFactionEnum",
    "episodeQuestCount",
    "taskQuestCount",
    "challengeCount",
    "datacubeCount",
    "taleCount",
    "journalCount",
    "characterTitleIdReward",
]

SOUND_ZONE_KIT_FIELDS = [
    "ID",
    "soundZoneKitIdParent",
    "worldZoneId",
    "inheritFlags",
    "propertyFlags",
    "soundMusicSetId",
    "soundEventIdIntro",
    "introReplayWait",
    "soundEventIdMusicMood",
    "soundEventIdAmbientDay",
    "soundEventIdAmbientNight",
    "soundEventIdAmbientUnderwater",
    "soundEventIdAmbientStop",
    "soundEventIdAmbientPreStopOverride",
    "soundEnvironmentId00",
    "soundEnvironmentId01",
    "environmentDry",
    "environmentWet00",
    "environmentWet01",
]

WORLD_SOCKET_FIELDS = [
    "ID",
    "worldId",
    "bounds0",
    "bounds1",
    "bounds2",
    "bounds3",
    "averageHeight",
]

WORLD_LAYER_FIELDS = [
    "ID",
    "Description",
    "HeightScale",
    "HeightOffset",
    "ParallaxScale",
    "ParallaxOffset",
    "MetersPerTextureTile",
    "ColorMapPath",
    "NormalMapPath",
    "AverageColor",
    "Projection",
    "materialType",
    "worldClutterId00",
    "worldClutterId01",
    "worldClutterId02",
    "worldClutterId03",
    "specularPower",
    "emissiveGlow",
    "scrollSpeed00",
    "scrollSpeed01",
]

WORLD_CLUTTER_FIELDS = [
    "ID",
    "Description",
    "density",
    "clutterFlags",
    "assetPath0",
    "assetPath01",
    "assetPath02",
    "assetPath03",
    "assetPath04",
    "assetPath05",
    "assetWeight0",
    "assetWeight01",
    "assetWeight02",
    "assetWeight03",
    "assetWeight04",
    "assetWeight05",
]

WORLD_SKY_FIELDS = [
    "ID",
    "assetPath",
    "assetPathInFlux",
    "color",
]

WORLD_WATER_ENVIRONMENT_FIELDS = [
    "ID",
    "LandMapPath",
]

WORLD_WATER_FOG_FIELDS = [
    "ID",
    "fogStart",
    "fogEnd",
    "fogStartUW",
    "fogEndUW",
    "modStart",
    "modEnd",
    "modStartUW",
    "modEndUW",
    "skyColorIndex",
]

WORLD_WATER_LAYER_FIELDS = [
    "ID",
    "description",
    "RippleColorTex",
    "RippleNormalTex",
    "Scale",
    "Rotation",
    "Speed",
    "OscFrequency",
    "OscMagnitude",
    "OscRotation",
    "OscPhase",
    "OscMinLayerWeight",
    "OscMaxLayerWeight",
    "OscLayerWeightPhase",
    "materialBlend",
]

WORLD_WATER_TYPE_FIELDS = [
    "ID",
    "worldWaterFogId",
    "SurfaceType",
    "particleFile",
    "soundDirectionalAmbienceId",
]

WORLD_WATER_WAKE_FIELDS = [
    "ID",
    "flags",
    "colorTexture",
    "normalTexture",
    "distortionTexture",
    "durationMin",
    "durationMax",
    "scaleStart",
    "scaleEnd",
    "alphaStart",
    "alphaEnd",
    "distortionWeight",
    "distortionScaleStart",
    "distortionScaleEnd",
    "distortionSpeedU",
    "distortionSpeedV",
    "positionOffsetX",
    "positionOffsetY",
]

ACTION_FIELDS = [
    "ID",
    "description",
    "creatureActionSetId",
    "state",
    "event",
    "orderIndex",
    "delayMS",
    "action",
    "actionData00",
    "actionData01",
    "visualEffectId",
    "prerequisiteId",
]

ENTITY_TYPE_BY_CREATURE_TYPE = {
    "nonplayer": 0,
    "creature": 0,
    "npc": 0,
    "chest": 1,
    "destructible": 2,
    "vehicle": 3,
    "door": 4,
    "harvestunit": 5,
    "harvest": 5,
    "corpseunit": 6,
    "mount": 7,
    "collectableunit": 8,
    "collectible": 8,
    "taxi": 9,
    "simple": 10,
    "platform": 11,
    "mailbox": 12,
    "mail": 12,
    "aiturret": 13,
    "turret": 13,
    "instanceportal": 14,
    "plug": 15,
    "residence": 16,
    "structuredplug": 17,
    "pinataloot": 18,
    "bindpoint": 19,
    "hidden": 21,
    "trigger": 22,
    "ghost": 23,
    "pet": 24,
    "esperpet": 25,
    "worldunit": 26,
    "scannerunit": 27,
    "camera": 28,
    "trap": 29,
    "destructibledoor": 30,
    "pickup": 31,
    "simplecollidable": 32,
    "housingmannequin": 33,
    "housingharvestplug": 34,
    "housingplant": 35,
    "lockbox": 36,
}

ENTITY_FLAG_OVERRIDES = [
    ("is_mail", 12),
    ("is_bindpoint", 19),
    ("is_taxi", 9),
    ("is_instanceportal", 14),
    ("is_harvestable", 5),
    ("is_collectible", 8),
    ("is_vehicle", 3),
]

STAT_IDS = {
    "Health": 0,
    "Level": 10,
    "Shield": 20,
    "InterruptArmour": 21,
}

CLIENT_FILE_STATUS = {
    "en-US.bin.sql": ("mapped", "Localized English bridge used by all named client maps."),
    "Creature2.tbl.sql": ("mapped", "Creature IDs, factions, levels, display/outfit groups, action sets."),
    "Creature2Action.tbl.sql": ("mapped", "AI/action rows keyed through non-zero creature action sets."),
    "Creature2ActionSet.tbl.sql": ("partial", "Used to validate that action set 0 is not real; descriptions not emitted yet."),
    "Creature2DisplayGroupEntry.tbl.sql": ("mapped", "Default display info selection."),
    "Creature2OutfitGroupEntry.tbl.sql": ("mapped", "Default outfit info selection."),
    "Item2.tbl.sql": ("mapped", "Item names and item metadata for loot/vendor/reward maps."),
    "Item2Family.tbl.sql": ("mapped", "Item family bridge and item taxonomy names."),
    "Item2Category.tbl.sql": ("mapped", "Item category bridge and item taxonomy names."),
    "Item2Type.tbl.sql": ("mapped", "Item type bridge and item taxonomy names."),
    "ItemSlot.tbl.sql": ("mapped", "Item slot bridge for Jabbithole item slot data."),
    "Class.tbl.sql": ("mapped", "Class names, descriptions, innate abilities, and class requirement bridges."),
    "UnitProperty2.tbl.sql": ("mapped", "Unit property names for item attribute maps."),
    "Spell4.tbl.sql": ("mapped", "Spell reference map and Jabbithole spell/class bridges."),
    "Spell4Base.tbl.sql": ("mapped", "Spell base names and targeting metadata."),
    "Spell4Effects.tbl.sql": ("mapped", "Spell effect rows keyed to Spell4."),
    "SpellEffectType.tbl.sql": ("mapped", "Spell effect type metadata for effect rows."),
    "SpellLevel.tbl.sql": ("mapped", "Class/level spell unlock reference rows."),
    "Spell4TierRequirements.tbl.sql": ("mapped", "Tier-to-level requirement reference rows."),
    "Spell4SpellTypes.tbl.sql": ("mapped", "Spell type names for spell reference rows."),
    "Quest2.tbl.sql": ("mapped", "Quest reference map and Jabbithole quest bridge."),
    "QuestObjective.tbl.sql": ("mapped", "Quest objective bridge from Quest2 objective slots."),
    "Quest2Reward.tbl.sql": ("mapped", "Client reward reference rows for quest reward comparisons."),
    "CurrencyType.tbl.sql": ("mapped", "Client currency names for quest currency rewards."),
    "Faction2.tbl.sql": ("mapped", "Client faction names, hierarchy, and Jabbithole faction bridges."),
    "Faction2Relationship.tbl.sql": ("mapped", "Client faction relationship/reference rows."),
    "QuestCategory.tbl.sql": ("mapped", "Client quest category names and Jabbithole category bridge."),
    "Episode.tbl.sql": ("mapped", "Client episode rows and Jabbithole quest episode bridge."),
    "EpisodeQuest.tbl.sql": ("mapped", "Client episode-to-quest join rows."),
    "Tradeskill.tbl.sql": ("mapped", "Client tradeskill names, descriptions, and achievement categories."),
    "TradeskillSchematic2.tbl.sql": ("mapped", "Client schematic reference rows and Jabbithole schematic bridge."),
    "TradeskillMaterial.tbl.sql": ("mapped", "Client tradeskill material item references."),
    "TradeskillMaterialCategory.tbl.sql": ("mapped", "Client tradeskill material category names."),
    "TradeskillTalentTier.tbl.sql": ("mapped", "Client tradeskill talent tier bonus slots."),
    "TradeskillTier.tbl.sql": ("mapped", "Client tradeskill XP tier reference rows."),
    "TradeskillAdditive.tbl.sql": ("mapped", "Client tradeskill additive geometry rows."),
    "TradeskillCatalyst.tbl.sql": ("mapped", "Client tradeskill catalyst reference rows."),
    "TradeskillCatalystOrdering.tbl.sql": ("mapped", "Client catalyst unlock ordering rows."),
    "TradeskillBonus.tbl.sql": ("mapped", "Client tradeskill bonus/talent rows."),
    "TradeskillAchievementLayout.tbl.sql": ("mapped", "Client tradeskill achievement grid rows."),
    "TradeskillAchievementReward.tbl.sql": ("mapped", "Client tradeskill achievement reward rows."),
    "PathMission.tbl.sql": ("mapped", "Path mission reference map and Jabbithole path bridge."),
    "PathEpisode.tbl.sql": ("mapped", "Path episode reference map and Jabbithole path episode bridge."),
    "PathReward.tbl.sql": ("mapped", "Path reward reference rows for path unlock comparisons."),
    "PathLevel.tbl.sql": ("mapped", "Path level XP reference rows."),
    "CharacterTitle.tbl.sql": ("mapped", "Character title names for path unlock rewards."),
    "CharacterTitleCategory.tbl.sql": ("mapped", "Character title category names for title references."),
    "Achievement.tbl.sql": ("mapped", "Achievement reference map and Jabbithole achievement bridge."),
    "AchievementChecklist.tbl.sql": ("mapped", "Achievement checklist/objective reference rows."),
    "AchievementGroup.tbl.sql": ("mapped", "Achievement group names and tradeskill links."),
    "AchievementCategory.tbl.sql": ("mapped", "Achievement category names and hierarchy."),
    "AchievementSubGroup.tbl.sql": ("mapped", "Achievement subgroup names and tiers."),
    "AchievementText.tbl.sql": ("mapped", "Achievement text reference rows."),
    "HousingDecorType.tbl.sql": ("mapped", "Housing decor type names and Jabbithole decor type bridge."),
    "HousingDecorInfo.tbl.sql": ("mapped", "Housing decor reference rows and Jabbithole decor bridge."),
    "HousingDecorLimitCategory.tbl.sql": ("mapped", "Housing decor placement limit reference rows."),
    "HousingPlugItem.tbl.sql": ("mapped", "Housing plug/enhancement reference rows and Jabbithole enhancement bridge."),
    "HousingWallpaperInfo.tbl.sql": ("mapped", "Housing wallpaper/sky reference rows and Jabbithole sky bridge."),
    "HousingResidenceInfo.tbl.sql": ("mapped", "Housing residence default decor/wallpaper reference rows."),
    "HousingBuild.tbl.sql": ("mapped", "Housing build animation and timing reference rows."),
    "HousingResource.tbl.sql": ("mapped", "Housing resource names for plug requirements."),
    "HousingContributionType.tbl.sql": ("mapped", "Housing contribution type reference rows."),
    "HousingContributionInfo.tbl.sql": ("mapped", "Housing contribution item/cost reference rows."),
    "HousingPropertyInfo.tbl.sql": ("mapped", "Housing property/world reference rows."),
    "HousingNeighborhoodInfo.tbl.sql": ("mapped", "Housing neighborhood reference rows."),
    "HousingMapInfo.tbl.sql": ("mapped", "Housing map/world reference rows."),
    "HousingPlotType.tbl.sql": ("mapped", "Housing plot type reference rows."),
    "HousingPlotInfo.tbl.sql": ("mapped", "Housing plot-to-property/plug reference rows."),
    "HousingMannequinPose.tbl.sql": ("mapped", "Housing mannequin pose reference rows."),
    "HousingWarplotBossToken.tbl.sql": ("mapped", "Warplot boss token spell reference rows."),
    "HousingWarplotPlugInfo.tbl.sql": ("mapped", "Warplot plug ability reference rows."),
    "DyeColorRamp.tbl.sql": ("mapped", "Dye color ramp names and Jabbithole dye bridge."),
    "PetFlair.tbl.sql": ("mapped", "Pet/mount flair reference rows and Jabbithole flair bridge."),
    "PublicEvent.tbl.sql": ("mapped", "Public event reference map and Jabbithole public-event bridge."),
    "PublicEventObjective.tbl.sql": ("mapped", "Public event objective bridge from Jabbithole objective game IDs."),
    "PublicEventTeam.tbl.sql": ("mapped", "Public event team names for objective maps."),
    "PublicEventDepot.tbl.sql": ("mapped", "Client public-event creature/item depot relations."),
    "PublicEventVirtualItemDepot.tbl.sql": ("mapped", "Client public-event creature/virtual-item depot relations."),
    "VirtualItem.tbl.sql": ("mapped", "Virtual item names for public-event depot maps."),
    "World.tbl.sql": ("mapped", "World reference map."),
    "WorldZone.tbl.sql": ("mapped", "Zone/world-zone bridge."),
    "WorldLocation2.tbl.sql": ("mapped", "World location coordinates, facing quaternions, and world/zone references."),
    "BindPoint.tbl.sql": ("mapped", "Bind point names and faction enums."),
    "TaxiNode.tbl.sql": ("mapped", "Taxi node names, locations, factions, tiers, and level ranges."),
    "TaxiRoute.tbl.sql": ("mapped", "Taxi route source/destination node references and prices."),
    "CityDirection.tbl.sql": ("mapped", "City direction rows mapped to named world locations."),
    "QuestDirection.tbl.sql": ("mapped", "Quest direction entry lists and excluded zones."),
    "QuestDirectionEntry.tbl.sql": ("mapped", "Quest direction entries mapped to world locations and zones."),
    "QuestHub.tbl.sql": ("mapped", "Quest hub names and world locations."),
    "GenericMap.tbl.sql": ("mapped", "Generic map rows mapped to MapZone."),
    "GenericMapNode.tbl.sql": ("mapped", "Generic map nodes mapped to locations, sprites, and localized labels."),
    "MapContinent.tbl.sql": ("mapped", "Client map continent reference rows and Jabbithole continent bridge."),
    "MapZone.tbl.sql": ("mapped", "Client map zone reference rows and Jabbithole map zone/POI bridge."),
    "MapZonePOI.tbl.sql": ("mapped", "Client map POI reference rows and Jabbithole map POI bridge."),
    "MapZoneSprite.tbl.sql": ("mapped", "Map POI sprite names and icon bridge."),
    "MapZoneWorldJoin.tbl.sql": ("mapped", "Map zone to world join reference rows."),
    "MapZoneHex.tbl.sql": ("mapped", "Map zone hex occupancy/reference rows."),
    "MapZoneHexGroup.tbl.sql": ("mapped", "Map zone hex group reference rows."),
    "MapZoneHexGroupEntry.tbl.sql": ("mapped", "Map zone hex group coordinate entries."),
    "MapZoneLevelBand.tbl.sql": ("mapped", "Map zone level-band metadata; this dump contains no data rows."),
    "MapZoneNemesisRegion.tbl.sql": ("mapped", "Nemesis region descriptions mapped through hex groups and Faction2."),
    "ZoneCompletion.tbl.sql": ("mapped", "Zone completion target counts and title rewards."),
    "SoundZoneKit.tbl.sql": ("mapped", "Sound zone kit references keyed to WorldZone."),
    "WorldSocket.tbl.sql": ("mapped", "World socket bounds keyed to World."),
    "WorldLayer.tbl.sql": ("mapped", "Terrain layer material/clutter references."),
    "WorldClutter.tbl.sql": ("mapped", "Terrain clutter asset reference rows."),
    "WorldSky.tbl.sql": ("mapped", "World sky asset reference rows."),
    "WorldWaterEnvironment.tbl.sql": ("mapped", "World water environment map asset rows."),
    "WorldWaterFog.tbl.sql": ("mapped", "World water fog parameter rows."),
    "WorldWaterLayer.tbl.sql": ("mapped", "World water layer texture/oscillation rows."),
    "WorldWaterType.tbl.sql": ("mapped", "World water type rows mapped to water fog."),
    "WorldWaterWake.tbl.sql": ("mapped", "World water wake texture/easing rows."),
    "Spline2.tbl.sql": ("mapped", "Optional proximity candidates for movement paths."),
    "Spline2Node.tbl.sql": ("mapped", "Optional proximity candidates for movement paths."),
    "Challenge.tbl.sql": ("mapped", "Challenge reference and Jabbithole challenge bridge."),
    "ChallengeTier.tbl.sql": ("mapped", "Challenge tier thresholds."),
}

JABBITHOLE_FILE_STATUS = {
    "creatures.sql": ("mapped", "Creature source rows, stats, flags, and name bridge."),
    "coordinates.sql": ("mapped", "Creature spawn coordinates and spline candidate source."),
    "versioned_item_drops.sql": ("mapped", "Creature loot map."),
    "versioned_creature_drop_aggregates.sql": ("mapped", "Loot probability denominator."),
    "versioned_vendor_items.sql": ("mapped", "Vendor stock map."),
    "creature_spells.sql": ("mapped", "Creature spell relation map."),
    "spells.sql": ("mapped", "Global Jabbithole spell to client Spell4 bridge."),
    "player_classes.sql": ("mapped", "Player class bridge to client Class rows and innate abilities."),
    "class_abilities.sql": ("mapped", "Class ability tiers and costs mapped to Spell4."),
    "class_amps.sql": ("mapped", "AMP tree nodes mapped to Spell4 and item rewards where present."),
    "class_unlocks.sql": ("mapped", "Level unlock rewards mapped to class and Spell4 where present."),
    "rune_set_spells.sql": ("mapped", "Rune set item-to-spell relations."),
    "achievement_groups.sql": ("mapped", "Achievement group bridge to client AchievementGroup."),
    "achievements.sql": ("mapped", "Achievement bridge to client Achievement."),
    "achievement_titles.sql": ("mapped", "Achievement title rewards mapped to CharacterTitle."),
    "character_achievements.sql": ("mapped", "Observed character achievement unlock rows mapped to Achievement."),
    "contracts.sql": ("mapped", "Contract quest rows mapped to Quest2."),
    "contract_rewards.sql": ("mapped", "Contract rewards mapped to Item2 where possible."),
    "contract_creatures.sql": ("mapped", "Contract quest creature rows mapped through the creature bridge."),
    "housing_decor_types.sql": ("mapped", "Housing decor type bridge to client HousingDecorType."),
    "housing_decors.sql": ("mapped", "Housing decor bridge to client HousingDecorInfo."),
    "housing_enhancements.sql": ("mapped", "Housing plug/enhancement bridge to client HousingPlugItem."),
    "housing_skies.sql": ("mapped", "Housing sky bridge to client HousingWallpaperInfo."),
    "dyes.sql": ("mapped", "Dye bridge to client DyeColorRamp."),
    "flairs.sql": ("mapped", "Flair bridge to client PetFlair and Spell4."),
    "mounts.sql": ("mapped", "Mount rows mapped to summon Spell4 and preview creatures/items."),
    "pets.sql": ("mapped", "Pet rows mapped to summon Spell4 and preview creatures."),
    "titles.sql": ("mapped", "Title rows mapped to client CharacterTitle."),
    "quest_starter_creatures.sql": ("mapped", "Quest starter creature relation."),
    "quest_finisher_creatures.sql": ("mapped", "Quest finisher creature relation."),
    "quest_creatures.sql": ("mapped", "Quest objective creature relation."),
    "quest_objectives.sql": ("mapped", "Quest objective text and ordering."),
    "quest_reward_items.sql": ("mapped", "Quest item rewards."),
    "quest_reward_currencies.sql": ("mapped", "Quest currency rewards."),
    "quest_reward_reputations.sql": ("mapped", "Quest reputation rewards."),
    "quest_reward_tradeskills.sql": ("mapped", "Quest tradeskill rewards."),
    "currencies.sql": ("mapped", "Currency bridge for quest rewards."),
    "reputation_rewards.sql": ("mapped", "Reputation bridge for quest rewards."),
    "tradeskill_rewards.sql": ("mapped", "Tradeskill bridge for quest rewards."),
    "tradeskills.sql": ("mapped", "Tradeskill names for quest and item requirement maps."),
    "schematics.sql": ("mapped", "Schematic bridge to client TradeskillSchematic2 where current IDs exist."),
    "schematic_materials.sql": ("mapped", "Schematic material item relations."),
    "schematic_circuits.sql": ("mapped", "Schematic circuit item relations."),
    "schematic_microchips.sql": ("mapped", "Schematic microchip item relations."),
    "tradeskill_talent_tiers.sql": ("mapped", "Tradeskill talent tier bridge to client TradeskillTalentTier."),
    "tradeskill_talents.sql": ("mapped", "Tradeskill talents mapped to client TradeskillBonus."),
    "tradeskill_techtree_groups.sql": ("mapped", "Tradeskill tech-tree groups mapped to AchievementCategory."),
    "tradeskill_techtree_items.sql": ("mapped", "Tradeskill tech-tree items mapped to Achievement."),
    "tradeskill_techtree_schematics.sql": ("mapped", "Tradeskill tech-tree schematic unlock relations."),
    "quests.sql": ("mapped", "Quest2 bridge for Jabbithole quest relations."),
    "path_mission_creatures.sql": ("mapped", "Path mission creature relation."),
    "path_missions.sql": ("mapped", "PathMission bridge for Jabbithole path relations."),
    "player_paths.sql": ("mapped", "Player path reference rows."),
    "path_episodes.sql": ("mapped", "Path episode bridge to client PathEpisode."),
    "path_episode_missions.sql": ("mapped", "Path episode to mission relation."),
    "path_episode_rewards.sql": ("mapped", "Path episode item rewards."),
    "path_episode_zones.sql": ("mapped", "Path episode to zone relation."),
    "path_unlocks.sql": ("mapped", "Path level unlock rewards."),
    "path_abilities.sql": ("mapped", "Path ability spell tiers."),
    "public_event_creatures.sql": ("mapped", "Public event creature relation."),
    "public_event_objectives.sql": ("mapped", "Public event objective text and client objective bridge."),
    "public_event_missions.sql": ("mapped", "Public event mission parent/child relation."),
    "public_event_zones.sql": ("mapped", "Public event to zone relation."),
    "public_events.sql": ("mapped", "PublicEvent bridge for Jabbithole public-event relations."),
    "zones.sql": ("mapped", "Zone/world-zone bridge."),
    "challenges.sql": ("mapped", "Challenge bridge and metadata."),
    "challenge_creatures.sql": ("mapped", "Challenge creature relation."),
    "challenge_reward_items.sql": ("mapped", "Direct challenge item rewards."),
    "challenge_rewards.sql": ("mapped", "Reward track reward metadata."),
    "challenge_item_rewards.sql": ("mapped", "Reward-track item contents."),
    "challenge_reward_track_challenge_rewards.sql": ("mapped", "Challenge reward track to reward join."),
    "_attributes_.sql": ("mapped", "Attribute bridge for item instance attributes."),
    "item_categories.sql": ("mapped", "Item category bridge to client Item2Category."),
    "item_families.sql": ("mapped", "Item family bridge to client Item2Family."),
    "item_types.sql": ("mapped", "Item type bridge to client Item2Type."),
    "item_slots.sql": ("mapped", "Item slot bridge to client ItemSlot where IDs align."),
    "item_drops.sql": ("mapped", "Legacy item drop source map."),
    "item_drop4_drops.sql": ("mapped", "Legacy item drop source map."),
    "item_drop5_drops.sql": ("mapped", "Legacy item drop source map."),
    "item_effects.sql": ("mapped", "Item-to-spell effect map."),
    "item_imbuements.sql": ("mapped", "Item-to-spell imbuement map."),
    "item_containers.sql": ("mapped", "Container item contents map."),
    "item_salvages.sql": ("mapped", "Item salvage output map."),
    "item_class_requirements.sql": ("mapped", "Item class requirement map."),
    "item_tradeskill_requirements.sql": ("mapped", "Item tradeskill requirement map."),
    "item_chip_spells.sql": ("mapped", "Item chip spell map."),
    "item_circuits.sql": ("mapped", "Item circuit relation map."),
    "item_microchips.sql": ("mapped", "Item microchip relation map."),
    "item_instance_attributes.sql": ("mapped", "Item stat attribute map."),
    "attribute_contributions.sql": ("mapped", "Primary-to-secondary stat contribution rows mapped through attributes and UnitProperty2."),
    "attribute_milestones.sql": ("mapped", "Primary stat milestone rows mapped through attributes and UnitProperty2."),
    "continents.sql": ("mapped", "Jabbithole continents bridged to client MapContinent."),
    "factions.sql": ("mapped", "Jabbithole faction rows bridged to client Faction2 by ID or normalized name."),
    "faction_reward_items.sql": ("mapped", "Faction vendor/reward item rows mapped to Faction2 and Item2."),
    "item_instance_sigils.sql": ("mapped", "Item instance sigil slots mapped to Item2 where item IDs align."),
    "mapzone_pois.sql": ("mapped", "Map POIs bridged to client MapZonePOI by zone, position, and name."),
    "quest_call_zones.sql": ("mapped", "Quest call-zone relations mapped to Quest2 and WorldZone through zones."),
    "quest_categories.sql": ("mapped", "Quest categories bridged to client QuestCategory."),
    "quest_episodes.sql": ("mapped", "Quest episodes bridged to client Episode."),
    "quest_zones.sql": ("mapped", "Quest-zone relations mapped to Quest2 and WorldZone through zones."),
    "vendor_items.sql": ("mapped", "Legacy vendor item rows mapped to resolved creatures and Item2."),
    "versioned_item_drop_aggregates.sql": ("mapped", "Versioned item drop aggregate rows mapped to Item2."),
    "_characters_.sql": ("mapped", "Character snapshots mapped to class, path, faction, equipment items, and build spells."),
    ".sql": ("mapped", "Unknown-table loot scrape parsed as raw loot rows and mapped to Item2/creatures where possible."),
    "items.sql": ("mapped", "Split file is empty in this dump; emitted as metadata."),
    "schema_migrations.sql": ("mapped", "Schema migration version metadata emitted for traceability."),
}

TEXT_TOKEN_RE = re.compile(r"\{[^}]*\}|<[^>]*>")
NON_NAME_RE = re.compile(r"[^0-9a-z]+")


def log(message: str) -> None:
    now = time.strftime("%H:%M:%S")
    print(f"[{now}] {message}", file=sys.stderr, flush=True)


def sql_to_python_value(token: str):
    raw = token.strip()
    if raw == "" or raw.upper() == "NULL":
        return None
    try:
        if re.search(r"[.eE]", raw):
            value = float(raw)
            if math.isfinite(value):
                return value
            return raw
        return int(raw)
    except ValueError:
        return raw


def iter_sql_statements(path: Path) -> Iterator[str]:
    text = path.read_text(encoding="utf-8", errors="replace")
    start = 0
    in_string = False
    i = 0
    n = len(text)
    while i < n:
        ch = text[i]
        if in_string:
            if ch == "\\":
                i += 2
                continue
            if ch == "'":
                if i + 1 < n and text[i + 1] == "'":
                    i += 2
                    continue
                in_string = False
        else:
            if ch == "'":
                in_string = True
            elif ch == ";":
                statement = text[start:i].strip()
                if statement:
                    yield statement
                start = i + 1
        i += 1

    tail = text[start:].strip()
    if tail:
        yield tail


def split_top_level(value: str, delimiter: str = ",") -> List[str]:
    parts: List[str] = []
    start = 0
    depth = 0
    in_string = False
    i = 0
    while i < len(value):
        ch = value[i]
        if in_string:
            if ch == "\\":
                i += 2
                continue
            if ch == "'":
                if i + 1 < len(value) and value[i + 1] == "'":
                    i += 2
                    continue
                in_string = False
        else:
            if ch == "'":
                in_string = True
            elif ch == "(":
                depth += 1
            elif ch == ")":
                depth -= 1
            elif ch == delimiter and depth == 0:
                parts.append(value[start:i].strip())
                start = i + 1
        i += 1
    parts.append(value[start:].strip())
    return parts


def parse_create_columns(statement: str) -> List[str]:
    open_idx = statement.find("(")
    close_idx = statement.rfind(")")
    if open_idx < 0 or close_idx < open_idx:
        raise ValueError("Could not parse CREATE TABLE columns.")

    column_defs = split_top_level(statement[open_idx + 1 : close_idx])
    columns: List[str] = []
    for column_def in column_defs:
        if not column_def:
            continue
        token = column_def.split(None, 1)[0].strip("`")
        if token.upper() in {"PRIMARY", "KEY", "UNIQUE", "INDEX", "CONSTRAINT"}:
            continue
        columns.append(token)
    return columns


def read_columns(path: Path) -> List[str]:
    for statement in iter_sql_statements(path):
        if statement.upper().startswith("CREATE TABLE"):
            return parse_create_columns(statement)
    raise ValueError(f"No CREATE TABLE statement found in {path}")


def read_create_statement_header(path: Path) -> str:
    buffer: List[str] = []
    with path.open("r", encoding="utf-8", errors="replace") as handle:
        for line in handle:
            buffer.append(line.rstrip("\r\n"))
            if ";" in line:
                break
    statement = "\n".join(buffer).strip().rstrip(";")
    return statement


def parse_table_name(statement: str) -> str:
    match = re.search(
        r"CREATE\s+TABLE\s+(?:IF\s+NOT\s+EXISTS\s+)?`?([A-Za-z0-9_.-]+)`?",
        statement,
        re.IGNORECASE,
    )
    return match.group(1) if match else ""


def parse_string_literal(values: str, index: int) -> Tuple[str, int]:
    assert values[index] == "'"
    index += 1
    result: List[str] = []
    while index < len(values):
        ch = values[index]
        if ch == "\\":
            if index + 1 >= len(values):
                result.append("\\")
                index += 1
                continue
            nxt = values[index + 1]
            result.append(
                {
                    "0": "\0",
                    "b": "\b",
                    "n": "\n",
                    "r": "\r",
                    "t": "\t",
                    "Z": "\x1a",
                    "\\": "\\",
                    "'": "'",
                    '"': '"',
                }.get(nxt, nxt)
            )
            index += 2
            continue
        if ch == "'":
            if index + 1 < len(values) and values[index + 1] == "'":
                result.append("'")
                index += 2
                continue
            return "".join(result), index + 1
        result.append(ch)
        index += 1
    return "".join(result), index


def parse_insert_rows(statement: str) -> Iterator[List[object]]:
    values_index = statement.upper().find("VALUES")
    if values_index < 0:
        return

    values = statement[values_index + len("VALUES") :]
    index = 0
    n = len(values)
    while index < n:
        while index < n and values[index] in " \t\r\n,":
            index += 1
        if index >= n:
            break
        if values[index] != "(":
            index += 1
            continue
        index += 1

        row: List[object] = []
        while index < n:
            while index < n and values[index].isspace():
                index += 1
            if index < n and values[index] == "'":
                parsed, index = parse_string_literal(values, index)
                row.append(parsed)
            else:
                start = index
                while index < n and values[index] not in ",)":
                    index += 1
                row.append(sql_to_python_value(values[start:index]))

            while index < n and values[index].isspace():
                index += 1
            if index < n and values[index] == ",":
                index += 1
                continue
            if index < n and values[index] == ")":
                index += 1
                yield row
                break


def iter_table_rows(path: Path, fields: Optional[Sequence[str]] = None) -> Iterator[Dict[str, object]]:
    columns = read_columns(path)
    if fields is None:
        selected = columns
    else:
        selected = [field for field in fields if field in columns]
    selected_indexes = [(field, columns.index(field)) for field in selected]

    for statement in iter_sql_statements(path):
        if not statement.upper().startswith("INSERT"):
            continue
        for row in parse_insert_rows(statement):
            yield {
                field: row[index] if index < len(row) else None
                for field, index in selected_indexes
            }


def to_int(value, default: Optional[int] = None) -> Optional[int]:
    if value is None:
        return default
    if isinstance(value, int):
        return value
    if isinstance(value, float):
        return int(value)
    value = str(value).strip()
    if value == "" or value.upper() in {"NULL", "\\N"}:
        return default
    try:
        return int(float(value))
    except ValueError:
        return default


def to_float(value, default: Optional[float] = None) -> Optional[float]:
    if value is None:
        return default
    if isinstance(value, (int, float)):
        return float(value)
    value = str(value).strip()
    if value == "" or value.upper() in {"NULL", "\\N"}:
        return default
    try:
        return float(value)
    except ValueError:
        return default


def signed32(value) -> Optional[int]:
    number = to_int(value)
    if number is None:
        return None
    if number >= 2**31:
        return number - 2**32
    return number


def clean_cell(value) -> str:
    if value is None:
        return ""
    if isinstance(value, float):
        return f"{value:g}"
    if isinstance(value, str) and value.strip().upper() in {"NULL", "\\N"}:
        return ""
    return str(value)


def template_health_value(health_min_value, health_max_value) -> Optional[float]:
    health_min = to_float(health_min_value)
    health_max = to_float(health_max_value)
    if health_min is not None and health_min <= 0:
        health_min = None
    if health_max is not None and health_max <= 0:
        health_max = None
    if health_min is not None and health_max is not None:
        if health_min == health_max:
            return health_min
        return None
    return health_min if health_min is not None else health_max


def display_text(value) -> str:
    if value is None:
        return ""
    return " ".join(TEXT_TOKEN_RE.sub(" ", str(value)).split())


def truthy(value) -> bool:
    if value is None:
        return False
    return str(value).strip().lower() in {"1", "true", "yes", "y"}


def normalize_name(value) -> str:
    if value is None:
        return ""
    text = TEXT_TOKEN_RE.sub(" ", str(value).lower())
    text = NON_NAME_RE.sub(" ", text)
    return " ".join(text.split())


def mysql_command(args: argparse.Namespace, query: str) -> List[str]:
    return [
        str(args.mysql_exe),
        f"--host={args.host}",
        f"--port={args.port}",
        f"--user={args.user}",
        f"--password={args.password}",
        "--default-character-set=utf8mb4",
        "--batch",
        "--raw",
        "--skip-column-names",
        "--quick",
        args.jabbithole_db,
        "-e",
        query,
    ]


def mysql_rows(
    args: argparse.Namespace,
    query: str,
    columns: Sequence[str],
) -> Iterator[Dict[str, str]]:
    process = subprocess.Popen(
        mysql_command(args, query),
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        text=True,
        encoding="utf-8",
        errors="replace",
    )
    assert process.stdout is not None
    for line in process.stdout:
        line = line.rstrip("\r\n")
        if not line:
            continue
        values = line.split("\t")
        if len(values) < len(columns):
            values.extend([""] * (len(columns) - len(values)))
        yield {column: values[index] for index, column in enumerate(columns)}

    stderr = process.stderr.read() if process.stderr is not None else ""
    return_code = process.wait()
    if return_code != 0:
        raise RuntimeError(f"MySQL query failed with code {return_code}: {stderr.strip()}")


def load_client_table(sql_dir: Path, filename: str, fields: Sequence[str]) -> Dict[int, Dict[str, object]]:
    path = sql_dir / filename
    log(f"Loading {filename}")
    rows: Dict[int, Dict[str, object]] = {}
    for row in iter_table_rows(path, fields):
        row_id = to_int(row.get("ID"))
        if row_id is not None:
            rows[row_id] = row
    return rows


def load_strings(sql_dir: Path) -> Dict[int, str]:
    path = sql_dir / "en-US.bin.sql"
    log("Loading en-US localized strings")
    strings: Dict[int, str] = {}
    for index, row in enumerate(iter_table_rows(path, ["ID", "LocalizedText"]), start=1):
        text_id = to_int(row.get("ID"))
        if text_id is not None:
            strings[text_id] = clean_cell(row.get("LocalizedText"))
        if index % 100000 == 0:
            log(f"  loaded {index:,} strings")
    return strings


def add_name_from_strings(rows: Dict[int, Dict[str, object]], strings: Dict[int, str], text_field: str, name_field: str) -> None:
    for row in rows.values():
        row[name_field] = display_text(strings.get(to_int(row.get(text_field)), ""))
        row[f"{name_field}Normalized"] = normalize_name(row[name_field])


def load_group_default(sql_dir: Path, filename: str, group_field: str, value_field: str) -> Dict[int, int]:
    path = sql_dir / filename
    log(f"Loading default group entries from {filename}")
    defaults: Dict[int, Tuple[int, int, int]] = {}
    fields = ["ID", "id", group_field, value_field, "weight"]
    for row in iter_table_rows(path, fields):
        group_id = to_int(row.get(group_field))
        value_id = to_int(row.get(value_field))
        if group_id is None or value_id is None:
            continue
        weight = to_int(row.get("weight"), 0) or 0
        row_id = to_int(row.get("ID"), to_int(row.get("id"), 0)) or 0
        current = defaults.get(group_id)
        if current is None or weight > current[0] or (weight == current[0] and row_id < current[2]):
            defaults[group_id] = (weight, value_id, row_id)
    return {group_id: value[1] for group_id, value in defaults.items()}


def load_client_sources(args: argparse.Namespace):
    strings = load_strings(args.client_sql_dir)
    creatures = load_client_table(args.client_sql_dir, "Creature2.tbl.sql", CREATURE_FIELDS)
    items = load_client_table(args.client_sql_dir, "Item2.tbl.sql", ITEM_FIELDS)
    item_families = load_client_table(args.client_sql_dir, "Item2Family.tbl.sql", ITEM_FAMILY_FIELDS)
    item_categories = load_client_table(args.client_sql_dir, "Item2Category.tbl.sql", ITEM_CATEGORY_FIELDS)
    item_types = load_client_table(args.client_sql_dir, "Item2Type.tbl.sql", ITEM_TYPE_FIELDS)
    item_slots = load_client_table(args.client_sql_dir, "ItemSlot.tbl.sql", ITEM_SLOT_FIELDS)
    classes = load_client_table(args.client_sql_dir, "Class.tbl.sql", CLASS_FIELDS)
    unit_properties = load_client_table(args.client_sql_dir, "UnitProperty2.tbl.sql", UNIT_PROPERTY_FIELDS)
    spells = load_client_table(args.client_sql_dir, "Spell4.tbl.sql", SPELL4_FIELDS)
    spell_bases = load_client_table(args.client_sql_dir, "Spell4Base.tbl.sql", SPELL4_BASE_FIELDS)
    spell_effects = load_client_table(args.client_sql_dir, "Spell4Effects.tbl.sql", SPELL4_EFFECT_FIELDS)
    spell_effect_types = load_client_table(args.client_sql_dir, "SpellEffectType.tbl.sql", SPELL_EFFECT_TYPE_FIELDS)
    spell_levels = load_client_table(args.client_sql_dir, "SpellLevel.tbl.sql", SPELL_LEVEL_FIELDS)
    spell_tier_requirements = load_client_table(args.client_sql_dir, "Spell4TierRequirements.tbl.sql", SPELL_TIER_REQUIREMENT_FIELDS)
    spell_types = load_client_table(args.client_sql_dir, "Spell4SpellTypes.tbl.sql", SPELL_TYPE_FIELDS)
    quests = load_client_table(args.client_sql_dir, "Quest2.tbl.sql", QUEST_FIELDS)
    quest_objectives = load_client_table(args.client_sql_dir, "QuestObjective.tbl.sql", QUEST_OBJECTIVE_FIELDS)
    quest_rewards = load_client_table(args.client_sql_dir, "Quest2Reward.tbl.sql", QUEST_REWARD_FIELDS)
    quest_categories = load_client_table(args.client_sql_dir, "QuestCategory.tbl.sql", QUEST_CATEGORY_FIELDS)
    episodes = load_client_table(args.client_sql_dir, "Episode.tbl.sql", EPISODE_FIELDS)
    episode_quests = load_client_table(args.client_sql_dir, "EpisodeQuest.tbl.sql", EPISODE_QUEST_FIELDS)
    path_missions = load_client_table(args.client_sql_dir, "PathMission.tbl.sql", PATH_MISSION_FIELDS)
    path_episodes = load_client_table(args.client_sql_dir, "PathEpisode.tbl.sql", PATH_EPISODE_FIELDS)
    path_rewards = load_client_table(args.client_sql_dir, "PathReward.tbl.sql", PATH_REWARD_FIELDS)
    path_levels = load_client_table(args.client_sql_dir, "PathLevel.tbl.sql", PATH_LEVEL_FIELDS)
    character_titles = load_client_table(args.client_sql_dir, "CharacterTitle.tbl.sql", CHARACTER_TITLE_FIELDS)
    character_title_categories = load_client_table(args.client_sql_dir, "CharacterTitleCategory.tbl.sql", CHARACTER_TITLE_CATEGORY_FIELDS)
    achievements = load_client_table(args.client_sql_dir, "Achievement.tbl.sql", ACHIEVEMENT_FIELDS)
    achievement_checklists = load_client_table(args.client_sql_dir, "AchievementChecklist.tbl.sql", ACHIEVEMENT_CHECKLIST_FIELDS)
    achievement_groups = load_client_table(args.client_sql_dir, "AchievementGroup.tbl.sql", ACHIEVEMENT_GROUP_FIELDS)
    achievement_categories = load_client_table(args.client_sql_dir, "AchievementCategory.tbl.sql", ACHIEVEMENT_CATEGORY_FIELDS)
    achievement_subgroups = load_client_table(args.client_sql_dir, "AchievementSubGroup.tbl.sql", ACHIEVEMENT_SUBGROUP_FIELDS)
    achievement_texts = load_client_table(args.client_sql_dir, "AchievementText.tbl.sql", ACHIEVEMENT_TEXT_FIELDS)
    housing_decor_types = load_client_table(args.client_sql_dir, "HousingDecorType.tbl.sql", HOUSING_DECOR_TYPE_FIELDS)
    housing_decors = load_client_table(args.client_sql_dir, "HousingDecorInfo.tbl.sql", HOUSING_DECOR_INFO_FIELDS)
    housing_decor_limit_categories = load_client_table(
        args.client_sql_dir,
        "HousingDecorLimitCategory.tbl.sql",
        HOUSING_DECOR_LIMIT_CATEGORY_FIELDS,
    )
    housing_plug_items = load_client_table(args.client_sql_dir, "HousingPlugItem.tbl.sql", HOUSING_PLUG_ITEM_FIELDS)
    housing_wallpapers = load_client_table(args.client_sql_dir, "HousingWallpaperInfo.tbl.sql", HOUSING_WALLPAPER_FIELDS)
    housing_residences = load_client_table(args.client_sql_dir, "HousingResidenceInfo.tbl.sql", HOUSING_RESIDENCE_FIELDS)
    housing_builds = load_client_table(args.client_sql_dir, "HousingBuild.tbl.sql", HOUSING_BUILD_FIELDS)
    housing_resources = load_client_table(args.client_sql_dir, "HousingResource.tbl.sql", HOUSING_RESOURCE_FIELDS)
    housing_contribution_types = load_client_table(args.client_sql_dir, "HousingContributionType.tbl.sql", HOUSING_CONTRIBUTION_TYPE_FIELDS)
    housing_contribution_info = load_client_table(args.client_sql_dir, "HousingContributionInfo.tbl.sql", HOUSING_CONTRIBUTION_INFO_FIELDS)
    housing_properties = load_client_table(args.client_sql_dir, "HousingPropertyInfo.tbl.sql", HOUSING_PROPERTY_FIELDS)
    housing_neighborhoods = load_client_table(args.client_sql_dir, "HousingNeighborhoodInfo.tbl.sql", HOUSING_NEIGHBORHOOD_FIELDS)
    housing_maps = load_client_table(args.client_sql_dir, "HousingMapInfo.tbl.sql", HOUSING_MAP_FIELDS)
    housing_plot_types = load_client_table(args.client_sql_dir, "HousingPlotType.tbl.sql", HOUSING_PLOT_TYPE_FIELDS)
    housing_plots = load_client_table(args.client_sql_dir, "HousingPlotInfo.tbl.sql", HOUSING_PLOT_INFO_FIELDS)
    housing_mannequin_poses = load_client_table(args.client_sql_dir, "HousingMannequinPose.tbl.sql", HOUSING_MANNEQUIN_POSE_FIELDS)
    housing_warplot_boss_tokens = load_client_table(
        args.client_sql_dir,
        "HousingWarplotBossToken.tbl.sql",
        HOUSING_WARPLOT_BOSS_TOKEN_FIELDS,
    )
    housing_warplot_plug_info = load_client_table(
        args.client_sql_dir,
        "HousingWarplotPlugInfo.tbl.sql",
        HOUSING_WARPLOT_PLUG_INFO_FIELDS,
    )
    dye_color_ramps = load_client_table(args.client_sql_dir, "DyeColorRamp.tbl.sql", DYE_COLOR_RAMP_FIELDS)
    pet_flairs = load_client_table(args.client_sql_dir, "PetFlair.tbl.sql", PET_FLAIR_FIELDS)
    public_events = load_client_table(args.client_sql_dir, "PublicEvent.tbl.sql", PUBLIC_EVENT_FIELDS)
    public_event_objectives = load_client_table(args.client_sql_dir, "PublicEventObjective.tbl.sql", PUBLIC_EVENT_OBJECTIVE_FIELDS)
    public_event_teams = load_client_table(args.client_sql_dir, "PublicEventTeam.tbl.sql", NAMED_ID_FIELDS)
    public_event_depots = load_client_table(args.client_sql_dir, "PublicEventDepot.tbl.sql", PUBLIC_EVENT_DEPOT_FIELDS)
    public_event_virtual_item_depots = load_client_table(
        args.client_sql_dir,
        "PublicEventVirtualItemDepot.tbl.sql",
        PUBLIC_EVENT_VIRTUAL_ITEM_DEPOT_FIELDS,
    )
    challenges = load_client_table(args.client_sql_dir, "Challenge.tbl.sql", CHALLENGE_FIELDS)
    challenge_tiers = load_client_table(args.client_sql_dir, "ChallengeTier.tbl.sql", CHALLENGE_TIER_FIELDS)
    virtual_items = load_client_table(args.client_sql_dir, "VirtualItem.tbl.sql", VIRTUAL_ITEM_FIELDS)
    currencies = load_client_table(args.client_sql_dir, "CurrencyType.tbl.sql", NAMED_ID_FIELDS)
    factions = load_client_table(args.client_sql_dir, "Faction2.tbl.sql", FACTION_FIELDS)
    faction_relationships = load_client_table(
        args.client_sql_dir,
        "Faction2Relationship.tbl.sql",
        FACTION_RELATIONSHIP_FIELDS,
    )
    tradeskills = load_client_table(args.client_sql_dir, "Tradeskill.tbl.sql", TRADESKILL_FIELDS)
    tradeskill_schematics = load_client_table(args.client_sql_dir, "TradeskillSchematic2.tbl.sql", TRADESKILL_SCHEMATIC_FIELDS)
    tradeskill_materials = load_client_table(args.client_sql_dir, "TradeskillMaterial.tbl.sql", TRADESKILL_MATERIAL_FIELDS)
    tradeskill_material_categories = load_client_table(
        args.client_sql_dir,
        "TradeskillMaterialCategory.tbl.sql",
        TRADESKILL_MATERIAL_CATEGORY_FIELDS,
    )
    tradeskill_talent_tiers = load_client_table(args.client_sql_dir, "TradeskillTalentTier.tbl.sql", TRADESKILL_TALENT_TIER_FIELDS)
    tradeskill_tiers = load_client_table(args.client_sql_dir, "TradeskillTier.tbl.sql", TRADESKILL_TIER_FIELDS)
    tradeskill_additives = load_client_table(args.client_sql_dir, "TradeskillAdditive.tbl.sql", TRADESKILL_ADDITIVE_FIELDS)
    tradeskill_catalysts = load_client_table(args.client_sql_dir, "TradeskillCatalyst.tbl.sql", TRADESKILL_CATALYST_FIELDS)
    tradeskill_catalyst_ordering = load_client_table(
        args.client_sql_dir,
        "TradeskillCatalystOrdering.tbl.sql",
        TRADESKILL_CATALYST_ORDERING_FIELDS,
    )
    tradeskill_bonuses = load_client_table(args.client_sql_dir, "TradeskillBonus.tbl.sql", TRADESKILL_BONUS_FIELDS)
    tradeskill_achievement_layouts = load_client_table(
        args.client_sql_dir,
        "TradeskillAchievementLayout.tbl.sql",
        TRADESKILL_ACHIEVEMENT_LAYOUT_FIELDS,
    )
    tradeskill_achievement_rewards = load_client_table(
        args.client_sql_dir,
        "TradeskillAchievementReward.tbl.sql",
        TRADESKILL_ACHIEVEMENT_REWARD_FIELDS,
    )
    worlds = load_client_table(args.client_sql_dir, "World.tbl.sql", WORLD_FIELDS)
    world_zones = load_client_table(args.client_sql_dir, "WorldZone.tbl.sql", WORLD_ZONE_FIELDS)
    world_locations = load_client_table(args.client_sql_dir, "WorldLocation2.tbl.sql", WORLD_LOCATION_FIELDS)
    bind_points = load_client_table(args.client_sql_dir, "BindPoint.tbl.sql", BIND_POINT_FIELDS)
    taxi_nodes = load_client_table(args.client_sql_dir, "TaxiNode.tbl.sql", TAXI_NODE_FIELDS)
    taxi_routes = load_client_table(args.client_sql_dir, "TaxiRoute.tbl.sql", TAXI_ROUTE_FIELDS)
    city_directions = load_client_table(args.client_sql_dir, "CityDirection.tbl.sql", CITY_DIRECTION_FIELDS)
    quest_directions = load_client_table(args.client_sql_dir, "QuestDirection.tbl.sql", QUEST_DIRECTION_FIELDS)
    quest_direction_entries = load_client_table(args.client_sql_dir, "QuestDirectionEntry.tbl.sql", QUEST_DIRECTION_ENTRY_FIELDS)
    quest_hubs = load_client_table(args.client_sql_dir, "QuestHub.tbl.sql", QUEST_HUB_FIELDS)
    generic_maps = load_client_table(args.client_sql_dir, "GenericMap.tbl.sql", GENERIC_MAP_FIELDS)
    generic_map_nodes = load_client_table(args.client_sql_dir, "GenericMapNode.tbl.sql", GENERIC_MAP_NODE_FIELDS)
    map_continents = load_client_table(args.client_sql_dir, "MapContinent.tbl.sql", MAP_CONTINENT_FIELDS)
    map_zones = load_client_table(args.client_sql_dir, "MapZone.tbl.sql", MAP_ZONE_FIELDS)
    map_zone_pois = load_client_table(args.client_sql_dir, "MapZonePOI.tbl.sql", MAP_ZONE_POI_FIELDS)
    map_zone_sprites = load_client_table(args.client_sql_dir, "MapZoneSprite.tbl.sql", MAP_ZONE_SPRITE_FIELDS)
    map_zone_world_joins = load_client_table(args.client_sql_dir, "MapZoneWorldJoin.tbl.sql", MAP_ZONE_WORLD_JOIN_FIELDS)
    map_zone_hexes = load_client_table(args.client_sql_dir, "MapZoneHex.tbl.sql", MAP_ZONE_HEX_FIELDS)
    map_zone_hex_groups = load_client_table(args.client_sql_dir, "MapZoneHexGroup.tbl.sql", MAP_ZONE_HEX_GROUP_FIELDS)
    map_zone_hex_group_entries = load_client_table(
        args.client_sql_dir,
        "MapZoneHexGroupEntry.tbl.sql",
        MAP_ZONE_HEX_GROUP_ENTRY_FIELDS,
    )
    map_zone_level_bands = load_client_table(args.client_sql_dir, "MapZoneLevelBand.tbl.sql", MAP_ZONE_LEVEL_BAND_FIELDS)
    map_zone_nemesis_regions = load_client_table(
        args.client_sql_dir,
        "MapZoneNemesisRegion.tbl.sql",
        MAP_ZONE_NEMESIS_REGION_FIELDS,
    )
    zone_completions = load_client_table(args.client_sql_dir, "ZoneCompletion.tbl.sql", ZONE_COMPLETION_FIELDS)
    sound_zone_kits = load_client_table(args.client_sql_dir, "SoundZoneKit.tbl.sql", SOUND_ZONE_KIT_FIELDS)
    world_sockets = load_client_table(args.client_sql_dir, "WorldSocket.tbl.sql", WORLD_SOCKET_FIELDS)
    world_layers = load_client_table(args.client_sql_dir, "WorldLayer.tbl.sql", WORLD_LAYER_FIELDS)
    world_clutters = load_client_table(args.client_sql_dir, "WorldClutter.tbl.sql", WORLD_CLUTTER_FIELDS)
    world_skies = load_client_table(args.client_sql_dir, "WorldSky.tbl.sql", WORLD_SKY_FIELDS)
    world_water_environments = load_client_table(
        args.client_sql_dir,
        "WorldWaterEnvironment.tbl.sql",
        WORLD_WATER_ENVIRONMENT_FIELDS,
    )
    world_water_fogs = load_client_table(args.client_sql_dir, "WorldWaterFog.tbl.sql", WORLD_WATER_FOG_FIELDS)
    world_water_layers = load_client_table(args.client_sql_dir, "WorldWaterLayer.tbl.sql", WORLD_WATER_LAYER_FIELDS)
    world_water_types = load_client_table(args.client_sql_dir, "WorldWaterType.tbl.sql", WORLD_WATER_TYPE_FIELDS)
    world_water_wakes = load_client_table(args.client_sql_dir, "WorldWaterWake.tbl.sql", WORLD_WATER_WAKE_FIELDS)

    add_name_from_strings(creatures, strings, "localizedTextIdName", "clientName")
    add_name_from_strings(items, strings, "localizedTextIdName", "itemName")
    add_name_from_strings(item_families, strings, "localizedTextId", "itemFamilyName")
    add_name_from_strings(item_categories, strings, "localizedTextId", "itemCategoryName")
    add_name_from_strings(item_types, strings, "localizedTextId", "itemTypeName")
    add_name_from_strings(classes, strings, "localizedTextId", "className")
    add_name_from_strings(classes, strings, "localizedTextIdDescription", "classDescription")
    add_name_from_strings(unit_properties, strings, "localizedTextId", "unitPropertyName")
    add_name_from_strings(spell_bases, strings, "localizedTextIdName", "spellBaseName")
    add_name_from_strings(spells, strings, "localizedTextIdActionBarTooltip", "spellTooltip")
    add_name_from_strings(spells, strings, "localizedTextIdTooltipCastInfo", "spellCastInfo")
    add_name_from_strings(spells, strings, "localizedTextIdTooltipCostInfo", "spellCostInfo")
    for row in item_slots.values():
        row["itemSlotName"] = display_text(row.get("EnumName", ""))
        row["itemSlotNameNormalized"] = normalize_name(row.get("EnumName", ""))
    add_name_from_strings(quests, strings, "localizedTextIdTitle", "questName")
    add_name_from_strings(quest_objectives, strings, "localizedTextIdFull", "questObjectiveText")
    add_name_from_strings(quest_categories, strings, "localizedTextIdTitle", "questCategoryName")
    add_name_from_strings(episodes, strings, "localizedTextIdName", "episodeName")
    add_name_from_strings(episodes, strings, "localizedTextIdBriefing", "episodeBriefing")
    add_name_from_strings(episodes, strings, "localizedTextIdEndSummary", "episodeEndSummary")
    add_name_from_strings(path_missions, strings, "localizedTextIdName", "pathMissionName")
    add_name_from_strings(path_episodes, strings, "localizedTextIdName", "pathEpisodeName")
    add_name_from_strings(path_episodes, strings, "localizedTextIdSummary", "pathEpisodeSummary")
    add_name_from_strings(character_titles, strings, "localizedTextIdName", "characterTitleName")
    add_name_from_strings(character_titles, strings, "localizedTextIdTitle", "characterTitleText")
    add_name_from_strings(character_title_categories, strings, "localizedTextId", "characterTitleCategoryName")
    add_name_from_strings(achievements, strings, "localizedTextIdTitle", "achievementTitle")
    add_name_from_strings(achievements, strings, "localizedTextIdDesc", "achievementDescription")
    add_name_from_strings(achievements, strings, "localizedTextIdProgress", "achievementProgress")
    add_name_from_strings(achievement_groups, strings, "localizedTextId", "achievementGroupName")
    add_name_from_strings(achievement_categories, strings, "localizedTextId", "achievementCategoryName")
    add_name_from_strings(achievement_categories, strings, "localizedTextIdFullName", "achievementCategoryFullName")
    add_name_from_strings(achievement_subgroups, strings, "localizedTextId", "achievementSubGroupName")
    add_name_from_strings(achievement_texts, strings, "localizedTextId", "achievementText")
    add_name_from_strings(housing_decor_types, strings, "localizedTextId", "housingDecorTypeName")
    add_name_from_strings(housing_decors, strings, "localizedTextIdName", "housingDecorName")
    add_name_from_strings(housing_plug_items, strings, "localizedTextIdName", "housingPlugName")
    add_name_from_strings(housing_plug_items, strings, "localizedTextIdTooltip", "housingPlugTooltip")
    add_name_from_strings(housing_wallpapers, strings, "localizedTextId", "housingWallpaperName")
    add_name_from_strings(housing_resources, strings, "localizedTextIdName", "housingResourceName")
    add_name_from_strings(housing_properties, strings, "localizedTextIdName", "housingPropertyName")
    add_name_from_strings(housing_mannequin_poses, strings, "localizedTextId", "housingMannequinPoseName")
    add_name_from_strings(dye_color_ramps, strings, "localizedTextIdName", "dyeName")
    add_name_from_strings(pet_flairs, strings, "localizedTextIdTooltip", "petFlairTooltip")
    add_name_from_strings(public_events, strings, "localizedTextIdName", "publicEventName")
    add_name_from_strings(public_event_objectives, strings, "localizedTextId", "publicEventObjectiveText")
    add_name_from_strings(public_event_teams, strings, "localizedTextIdName", "publicEventTeamName")
    add_name_from_strings(challenges, strings, "localizedTextIdName", "challengeName")
    add_name_from_strings(virtual_items, strings, "localizedTextIdName", "virtualItemName")
    add_name_from_strings(currencies, strings, "localizedTextIdName", "currencyName")
    add_name_from_strings(factions, strings, "localizedTextIdName", "factionName")
    add_name_from_strings(factions, strings, "localizedTextIdToolTip", "factionTooltip")
    add_name_from_strings(tradeskills, strings, "localizedTextIdName", "tradeskillName")
    add_name_from_strings(tradeskills, strings, "localizedTextIdDescription", "tradeskillDescription")
    add_name_from_strings(tradeskills, strings, "localizedTextIdAxisName00", "tradeskillAxisName0")
    add_name_from_strings(tradeskills, strings, "localizedTextIdAxisName01", "tradeskillAxisName1")
    add_name_from_strings(tradeskills, strings, "localizedTextIdAxisName02", "tradeskillAxisName2")
    add_name_from_strings(tradeskills, strings, "localizedTextIdAxisName03", "tradeskillAxisName3")
    add_name_from_strings(tradeskill_schematics, strings, "localizedTextIdName", "tradeskillSchematicName")
    add_name_from_strings(tradeskill_material_categories, strings, "localizedTextIdName", "tradeskillMaterialCategoryName")
    add_name_from_strings(tradeskill_bonuses, strings, "localizedTextIdName", "tradeskillBonusName")
    add_name_from_strings(tradeskill_bonuses, strings, "localizedTextIdTooltip", "tradeskillBonusTooltip")
    add_name_from_strings(worlds, strings, "localizedTextIdName", "worldName")
    add_name_from_strings(world_zones, strings, "localizedTextIdName", "worldZoneName")
    add_name_from_strings(bind_points, strings, "localizedTextId", "bindPointName")
    add_name_from_strings(taxi_nodes, strings, "localizedTextId", "taxiNodeName")
    add_name_from_strings(city_directions, strings, "localizedTextIdName", "cityDirectionName")
    add_name_from_strings(quest_hubs, strings, "localizedTextIdName", "questHubName")
    add_name_from_strings(generic_map_nodes, strings, "localizedTextIdName", "genericMapNodeName")
    add_name_from_strings(generic_map_nodes, strings, "localizedTextIdDescription", "genericMapNodeDescription")
    add_name_from_strings(map_zone_nemesis_regions, strings, "localizedTextIdDescription", "nemesisRegionDescription")
    add_name_from_strings(map_continents, strings, "localizedTextIdName", "mapContinentName")
    add_name_from_strings(map_zones, strings, "localizedTextIdName", "mapZoneName")
    add_name_from_strings(map_zone_pois, strings, "localizedTextId", "mapZonePoiName")

    display_defaults = load_group_default(
        args.client_sql_dir,
        "Creature2DisplayGroupEntry.tbl.sql",
        "creature2DisplayGroupId",
        "creature2DisplayInfoId",
    )
    outfit_defaults = load_group_default(
        args.client_sql_dir,
        "Creature2OutfitGroupEntry.tbl.sql",
        "creature2OutfitGroupId",
        "creature2OutfitInfoId",
    )

    for creature in creatures.values():
        creature["defaultDisplayInfo"] = display_defaults.get(to_int(creature.get("creature2DisplayGroupId")), "")
        creature["defaultOutfitInfo"] = outfit_defaults.get(to_int(creature.get("creature2OutfitGroupId")), "")

    return {
        "strings": strings,
        "creatures": creatures,
        "items": items,
        "item_families": item_families,
        "item_categories": item_categories,
        "item_types": item_types,
        "item_slots": item_slots,
        "classes": classes,
        "unit_properties": unit_properties,
        "spells": spells,
        "spell_bases": spell_bases,
        "spell_effects": spell_effects,
        "spell_effect_types": spell_effect_types,
        "spell_levels": spell_levels,
        "spell_tier_requirements": spell_tier_requirements,
        "spell_types": spell_types,
        "quests": quests,
        "quest_objectives": quest_objectives,
        "quest_rewards": quest_rewards,
        "quest_categories": quest_categories,
        "episodes": episodes,
        "episode_quests": episode_quests,
        "path_missions": path_missions,
        "path_episodes": path_episodes,
        "path_rewards": path_rewards,
        "path_levels": path_levels,
        "character_titles": character_titles,
        "character_title_categories": character_title_categories,
        "achievements": achievements,
        "achievement_checklists": achievement_checklists,
        "achievement_groups": achievement_groups,
        "achievement_categories": achievement_categories,
        "achievement_subgroups": achievement_subgroups,
        "achievement_texts": achievement_texts,
        "housing_decor_types": housing_decor_types,
        "housing_decors": housing_decors,
        "housing_decor_limit_categories": housing_decor_limit_categories,
        "housing_plug_items": housing_plug_items,
        "housing_wallpapers": housing_wallpapers,
        "housing_residences": housing_residences,
        "housing_builds": housing_builds,
        "housing_resources": housing_resources,
        "housing_contribution_types": housing_contribution_types,
        "housing_contribution_info": housing_contribution_info,
        "housing_properties": housing_properties,
        "housing_neighborhoods": housing_neighborhoods,
        "housing_maps": housing_maps,
        "housing_plot_types": housing_plot_types,
        "housing_plots": housing_plots,
        "housing_mannequin_poses": housing_mannequin_poses,
        "housing_warplot_boss_tokens": housing_warplot_boss_tokens,
        "housing_warplot_plug_info": housing_warplot_plug_info,
        "dye_color_ramps": dye_color_ramps,
        "pet_flairs": pet_flairs,
        "public_events": public_events,
        "public_event_objectives": public_event_objectives,
        "public_event_teams": public_event_teams,
        "public_event_depots": public_event_depots,
        "public_event_virtual_item_depots": public_event_virtual_item_depots,
        "challenges": challenges,
        "challenge_tiers": challenge_tiers,
        "virtual_items": virtual_items,
        "currencies": currencies,
        "factions": factions,
        "faction_relationships": faction_relationships,
        "tradeskills": tradeskills,
        "tradeskill_schematics": tradeskill_schematics,
        "tradeskill_materials": tradeskill_materials,
        "tradeskill_material_categories": tradeskill_material_categories,
        "tradeskill_talent_tiers": tradeskill_talent_tiers,
        "tradeskill_tiers": tradeskill_tiers,
        "tradeskill_additives": tradeskill_additives,
        "tradeskill_catalysts": tradeskill_catalysts,
        "tradeskill_catalyst_ordering": tradeskill_catalyst_ordering,
        "tradeskill_bonuses": tradeskill_bonuses,
        "tradeskill_achievement_layouts": tradeskill_achievement_layouts,
        "tradeskill_achievement_rewards": tradeskill_achievement_rewards,
        "worlds": worlds,
        "world_zones": world_zones,
        "world_locations": world_locations,
        "bind_points": bind_points,
        "taxi_nodes": taxi_nodes,
        "taxi_routes": taxi_routes,
        "city_directions": city_directions,
        "quest_directions": quest_directions,
        "quest_direction_entries": quest_direction_entries,
        "quest_hubs": quest_hubs,
        "generic_maps": generic_maps,
        "generic_map_nodes": generic_map_nodes,
        "map_continents": map_continents,
        "map_zones": map_zones,
        "map_zone_pois": map_zone_pois,
        "map_zone_sprites": map_zone_sprites,
        "map_zone_world_joins": map_zone_world_joins,
        "map_zone_hexes": map_zone_hexes,
        "map_zone_hex_groups": map_zone_hex_groups,
        "map_zone_hex_group_entries": map_zone_hex_group_entries,
        "map_zone_level_bands": map_zone_level_bands,
        "map_zone_nemesis_regions": map_zone_nemesis_regions,
        "zone_completions": zone_completions,
        "sound_zone_kits": sound_zone_kits,
        "world_sockets": world_sockets,
        "world_layers": world_layers,
        "world_clutters": world_clutters,
        "world_skies": world_skies,
        "world_water_environments": world_water_environments,
        "world_water_fogs": world_water_fogs,
        "world_water_layers": world_water_layers,
        "world_water_types": world_water_types,
        "world_water_wakes": world_water_wakes,
    }


def limited(query: str, limit: Optional[int]) -> str:
    if limit is None or limit <= 0:
        return query
    return f"{query}\nLIMIT {int(limit)}"


def load_jabbithole_creatures(args: argparse.Namespace) -> List[Dict[str, str]]:
    columns = [
        "jabbithole_creature_id",
        "zone_id",
        "datacube_id",
        "source_name",
        "slug",
        "affiliation",
        "health_min",
        "health_max",
        "shield",
        "level_min",
        "level_max",
        "faction",
        "difficulty",
        "creature_type",
        "is_bindpoint",
        "is_taxi",
        "is_instanceportal",
        "is_collectible",
        "is_vehicle",
        "is_mail",
        "is_vendor",
        "is_scannable",
        "is_harvestable",
        "worldid",
        "worldzoneid",
        "assault_power",
        "support_power",
        "crit_chance",
        "crit_severity",
        "deflect_chance",
        "deflect_critical_chance",
        "interrupt_armor_max",
        "enabled",
        "last_seen_in",
    ]
    query = """
SELECT
    id,
    zone_id,
    datacube_id,
    name,
    slug,
    affiliation,
    health_min,
    health_max,
    shield,
    level_min,
    level_max,
    faction,
    difficulty,
    creature_type,
    is_bindpoint,
    is_taxi,
    is_instanceportal,
    is_collectible,
    is_vehicle,
    is_mail,
    is_vendor,
    is_scannable,
    is_harvestable,
    worldid,
    worldzoneid,
    assault_power,
    support_power,
    crit_chance,
    crit_severity,
    deflect_chance,
    deflect_critical_chance,
    interrupt_armor_max,
    enabled,
    last_seen_in
FROM creatures
ORDER BY id
""".strip()
    log("Querying Jabbithole creatures")
    return list(mysql_rows(args, limited(query, args.limit_creatures), columns))


def map_entity_type(row: Dict[str, object]) -> int:
    for field, entity_type in ENTITY_FLAG_OVERRIDES:
        if truthy(row.get(field)):
            return entity_type
    creature_type = normalize_name(row.get("creature_type")).replace(" ", "")
    return ENTITY_TYPE_BY_CREATURE_TYPE.get(creature_type, 0)


def score_creature_match(jabbit: Dict[str, str], client: Dict[str, object]) -> int:
    score = 100
    if str(jabbit.get("source_name", "")).strip().lower() == str(client.get("clientName", "")).strip().lower():
        score += 20

    j_faction = to_int(jabbit.get("faction"))
    c_faction = to_int(client.get("factionId"))
    if j_faction is not None and c_faction is not None and j_faction == c_faction:
        score += 12

    j_min = to_int(jabbit.get("level_min"))
    j_max = to_int(jabbit.get("level_max"))
    c_min = to_int(client.get("minLevel"))
    c_max = to_int(client.get("maxLevel"))
    if None not in (j_min, j_max, c_min, c_max):
        if max(j_min, c_min) <= min(j_max, c_max):
            score += 8
        if j_min == c_min and j_max == c_max:
            score += 5

    j_difficulty = to_int(jabbit.get("difficulty"))
    c_difficulty = to_int(client.get("creature2DifficultyId"))
    if j_difficulty is not None and c_difficulty is not None and j_difficulty == c_difficulty:
        score += 4

    j_datacube = to_int(jabbit.get("datacube_id"))
    c_datacube = to_int(client.get("datacubeId"))
    if j_datacube and c_datacube and j_datacube == c_datacube:
        score += 4
    return score


def bool_text(value: bool) -> str:
    return "true" if value else "false"


def level_overlap_text(jabbit: Dict[str, str], client: Dict[str, object]) -> str:
    j_min = to_int(jabbit.get("level_min"))
    j_max = to_int(jabbit.get("level_max"))
    c_min = to_int(client.get("minLevel"))
    c_max = to_int(client.get("maxLevel"))
    if None in (j_min, j_max, c_min, c_max):
        return ""
    return bool_text(max(j_min, c_min) <= min(j_max, c_max))


def exact_level_match_text(jabbit: Dict[str, str], client: Dict[str, object]) -> str:
    j_min = to_int(jabbit.get("level_min"))
    j_max = to_int(jabbit.get("level_max"))
    c_min = to_int(client.get("minLevel"))
    c_max = to_int(client.get("maxLevel"))
    if None in (j_min, j_max, c_min, c_max):
        return ""
    return bool_text(j_min == c_min and j_max == c_max)


def id_match_text(left, right) -> str:
    left_id = to_int(left)
    right_id = to_int(right)
    if left_id is None or right_id is None:
        return ""
    return bool_text(left_id == right_id)


def name_similarity(source_name: object, client_name: object) -> str:
    source = normalize_name(source_name)
    candidate = normalize_name(client_name)
    if not source or not candidate:
        return ""
    return f"{difflib.SequenceMatcher(a=source, b=candidate).ratio():.4f}"


def creature_review_row(
    jabbit: Dict[str, str],
    current: Dict[str, object],
    candidate: Dict[str, object],
    reason: str,
    rank: object,
    best_score: int,
) -> Dict[str, object]:
    candidate_score = score_creature_match(jabbit, candidate) if candidate else 0
    return {
        "source_table": "creatures",
        "source_id": jabbit.get("jabbithole_creature_id", ""),
        "source_name": jabbit.get("source_name", ""),
        "current_status": current.get("match_status", ""),
        "current_creature2_id": current.get("creature2_id", ""),
        "current_client_name": current.get("client_name", ""),
        "candidate_reason": reason,
        "candidate_rank": rank,
        "candidate_creature2_id": candidate.get("ID", "") if candidate else "",
        "candidate_client_name": candidate.get("clientName", "") if candidate else "",
        "candidate_score": candidate_score or "",
        "score_delta_from_best": (best_score - candidate_score) if candidate and best_score else "",
        "name_similarity": name_similarity(jabbit.get("source_name"), candidate.get("clientName", "")) if candidate else "",
        "source_faction": jabbit.get("faction", ""),
        "candidate_faction": candidate.get("factionId", "") if candidate else "",
        "faction_match": id_match_text(jabbit.get("faction"), candidate.get("factionId")) if candidate else "",
        "source_level_min": jabbit.get("level_min", ""),
        "source_level_max": jabbit.get("level_max", ""),
        "candidate_min_level": candidate.get("minLevel", "") if candidate else "",
        "candidate_max_level": candidate.get("maxLevel", "") if candidate else "",
        "level_overlap": level_overlap_text(jabbit, candidate) if candidate else "",
        "exact_level_match": exact_level_match_text(jabbit, candidate) if candidate else "",
        "source_difficulty": jabbit.get("difficulty", ""),
        "candidate_difficulty": candidate.get("creature2DifficultyId", "") if candidate else "",
        "difficulty_match": id_match_text(jabbit.get("difficulty"), candidate.get("creature2DifficultyId")) if candidate else "",
        "source_datacube_id": jabbit.get("datacube_id", ""),
        "candidate_datacube_id": candidate.get("datacubeId", "") if candidate else "",
        "datacube_match": id_match_text(jabbit.get("datacube_id"), candidate.get("datacubeId")) if candidate else "",
        "source_zone_id": jabbit.get("zone_id", ""),
        "source_worldid": jabbit.get("worldid", ""),
        "source_worldzoneid": jabbit.get("worldzoneid", ""),
        "decision": "",
        "reason": "",
        "reviewer": "",
        "reviewed_at": "",
    }


def ensure_review_templates(args: argparse.Namespace) -> None:
    args.review_dir.mkdir(parents=True, exist_ok=True)
    override_path = args.review_dir / "creature_bridge_overrides.csv"
    if not override_path.exists():
        write_csv(override_path, CREATURE_BRIDGE_OVERRIDE_FIELDS, [])


def load_creature_bridge_overrides(
    args: argparse.Namespace,
    client_creatures: Dict[int, Dict[str, object]],
) -> Tuple[Dict[int, Dict[str, object]], List[Dict[str, object]]]:
    ensure_review_templates(args)
    paths = [
        args.repo_root / TRACKED_CREATURE_BRIDGE_OVERRIDES,
        args.review_dir / "creature_bridge_overrides.csv",
    ]
    overrides: Dict[int, Dict[str, object]] = {}
    audit_rows: List[Dict[str, object]] = []
    seen_paths = set()
    for path in paths:
        resolved_path = path.resolve()
        if resolved_path in seen_paths or not resolved_path.exists():
            continue
        seen_paths.add(resolved_path)
        try:
            source_file = str(resolved_path.relative_to(args.repo_root))
        except ValueError:
            source_file = str(resolved_path)
        with resolved_path.open("r", encoding="utf-8", newline="") as handle:
            reader = csv.DictReader(handle)
            for row_number, row in enumerate(reader, start=2):
                source_table = clean_cell(row.get("source_table") or "creatures")
                source_id = to_int(row.get("source_id"))
                chosen_id = to_int(row.get("chosen_creature2_id") or row.get("candidate_creature2_id"))
                decision = clean_cell(row.get("decision")).lower()
                status = "skipped"
                note = ""
                if source_table and source_table != "creatures":
                    note = "source_table is not creatures"
                elif source_id is None:
                    note = "missing source_id"
                elif chosen_id is None:
                    note = "missing chosen_creature2_id"
                elif decision not in APPROVED_REVIEW_DECISIONS:
                    note = "decision is not approved"
                elif chosen_id not in client_creatures:
                    note = "chosen_creature2_id not found in Creature2"
                else:
                    status = "applied"
                    note = "reviewed creature bridge override"
                    overrides[source_id] = {
                        "chosen_creature2_id": chosen_id,
                        "decision": row.get("decision", ""),
                        "reason": row.get("reason", ""),
                        "reviewer": row.get("reviewer", ""),
                        "reviewed_at": row.get("reviewed_at", ""),
                        "row_number": row_number,
                    }
                audit_rows.append(
                    {
                        "source_file": source_file,
                        "row_number": row_number,
                        "source_table": source_table,
                        "source_id": row.get("source_id", ""),
                        "chosen_creature2_id": row.get("chosen_creature2_id") or row.get("candidate_creature2_id", ""),
                        "decision": row.get("decision", ""),
                        "status": status,
                        "note": note,
                        "reason": row.get("reason", ""),
                        "reviewer": row.get("reviewer", ""),
                        "reviewed_at": row.get("reviewed_at", ""),
                    }
                )
    return overrides, audit_rows


def fuzzy_creature_candidates(
    source_name: object,
    name_keys_by_first_char: Dict[str, Sequence[str]],
    by_name: Dict[str, List[Dict[str, object]]],
    limit: int,
) -> List[Dict[str, object]]:
    normalized = normalize_name(source_name)
    if not normalized:
        return []
    first_char = normalized[0]
    length_window = max(6, len(normalized) // 2)
    candidate_names = [
        name
        for name in name_keys_by_first_char.get(first_char, [])
        if abs(len(name) - len(normalized)) <= length_window
    ]
    matches = difflib.get_close_matches(normalized, candidate_names, n=limit, cutoff=0.55)
    candidates: List[Dict[str, object]] = []
    for match in matches:
        candidates.extend(by_name.get(match, []))
        if len(candidates) >= limit:
            break
    return candidates[:limit]


def build_creature_map(
    args: argparse.Namespace,
    jabbithole_creatures: List[Dict[str, str]],
    client_creatures: Dict[int, Dict[str, object]],
    overrides: Optional[Dict[int, Dict[str, object]]] = None,
) -> Tuple[List[Dict[str, object]], Dict[int, Dict[str, object]], List[Dict[str, object]]]:
    overrides = overrides or {}
    by_name: Dict[str, List[Dict[str, object]]] = defaultdict(list)
    for creature in client_creatures.values():
        normalized = clean_cell(creature.get("clientNameNormalized"))
        if normalized:
            by_name[normalized].append(creature)

    rows: List[Dict[str, object]] = []
    by_jabbit_id: Dict[int, Dict[str, object]] = {}
    review_rows: List[Dict[str, object]] = []
    name_keys = sorted(by_name)
    name_keys_by_first_char: Dict[str, List[str]] = defaultdict(list)
    for name in name_keys:
        if name:
            name_keys_by_first_char[name[0]].append(name)
    for jabbit in jabbithole_creatures:
        normalized = normalize_name(jabbit.get("source_name"))
        candidates = by_name.get(normalized, [])
        scored: List[Tuple[int, int, Dict[str, object]]] = []
        for candidate in candidates:
            client_id = to_int(candidate.get("ID"), 0) or 0
            scored.append((score_creature_match(jabbit, candidate), client_id, candidate))
        scored.sort(key=lambda item: (-item[0], item[1]))

        best = scored[0][2] if scored else {}
        best_score = scored[0][0] if scored else 0
        second_score = scored[1][0] if len(scored) > 1 else None
        if not candidates:
            status = "unmatched"
        elif len(candidates) == 1:
            status = "unique_name"
        elif second_score == best_score:
            status = "ambiguous_name"
        else:
            status = "scored_name"

        row: Dict[str, object] = {
            "jabbithole_creature_id": jabbit.get("jabbithole_creature_id"),
            "creature2_id": best.get("ID", ""),
            "source_name": jabbit.get("source_name", ""),
            "client_name": best.get("clientName", ""),
            "match_status": status,
            "match_score": best_score or "",
            "candidate_count": len(candidates),
            "entity_type": map_entity_type(jabbit),
            "zone_id": jabbit.get("zone_id", ""),
            "worldid": jabbit.get("worldid", ""),
            "worldzoneid": jabbit.get("worldzoneid", ""),
            "source_faction": jabbit.get("faction", ""),
            "client_faction": best.get("factionId", ""),
            "level_min": jabbit.get("level_min", ""),
            "level_max": jabbit.get("level_max", ""),
            "client_min_level": best.get("minLevel", ""),
            "client_max_level": best.get("maxLevel", ""),
            "difficulty": jabbit.get("difficulty", ""),
            "client_difficulty": best.get("creature2DifficultyId", ""),
            "health_min": jabbit.get("health_min", ""),
            "health_max": jabbit.get("health_max", ""),
            "template_base_health": template_health_value(jabbit.get("health_min"), jabbit.get("health_max")),
            "shield": jabbit.get("shield", ""),
            "interrupt_armor_max": jabbit.get("interrupt_armor_max", ""),
            "creature_type": jabbit.get("creature_type", ""),
            "is_vendor": jabbit.get("is_vendor", ""),
            "is_scannable": jabbit.get("is_scannable", ""),
            "is_harvestable": jabbit.get("is_harvestable", ""),
            "datacube_id": jabbit.get("datacube_id", ""),
            "client_datacube_id": best.get("datacubeId", ""),
            "creature2_action_set_id": best.get("creature2ActionSetId", ""),
            "creature2_display_group_id": best.get("creature2DisplayGroupId", ""),
            "default_display_info": best.get("defaultDisplayInfo", ""),
            "creature2_outfit_group_id": best.get("creature2OutfitGroupId", ""),
            "default_outfit_info": best.get("defaultOutfitInfo", ""),
            "last_seen_in": jabbit.get("last_seen_in", ""),
            "original_creature2_id": "",
            "original_client_name": "",
            "original_match_status": "",
            "review_decision": "",
            "review_reason": "",
            "reviewer": "",
            "reviewed_at": "",
        }
        jabbit_id = to_int(jabbit.get("jabbithole_creature_id"))
        override = overrides.get(jabbit_id) if jabbit_id is not None else None
        if override:
            chosen_id = to_int(override.get("chosen_creature2_id"))
            chosen = client_creatures.get(chosen_id, {}) if chosen_id is not None else {}
            row["original_creature2_id"] = row["creature2_id"]
            row["original_client_name"] = row["client_name"]
            row["original_match_status"] = row["match_status"]
            row["creature2_id"] = chosen.get("ID", "")
            row["client_name"] = chosen.get("clientName", "")
            row["match_status"] = REVIEWED_MATCH_STATUS
            row["client_faction"] = chosen.get("factionId", "")
            row["client_min_level"] = chosen.get("minLevel", "")
            row["client_max_level"] = chosen.get("maxLevel", "")
            row["client_difficulty"] = chosen.get("creature2DifficultyId", "")
            row["client_datacube_id"] = chosen.get("datacubeId", "")
            row["creature2_action_set_id"] = chosen.get("creature2ActionSetId", "")
            row["creature2_display_group_id"] = chosen.get("creature2DisplayGroupId", "")
            row["default_display_info"] = chosen.get("defaultDisplayInfo", "")
            row["creature2_outfit_group_id"] = chosen.get("creature2OutfitGroupId", "")
            row["default_outfit_info"] = chosen.get("defaultOutfitInfo", "")
            row["review_decision"] = override.get("decision", "")
            row["review_reason"] = override.get("reason", "")
            row["reviewer"] = override.get("reviewer", "")
            row["reviewed_at"] = override.get("reviewed_at", "")
        elif status in {"ambiguous_name", "unmatched"}:
            if scored:
                best_candidate_score = scored[0][0]
                for rank, (candidate_score, _, candidate) in enumerate(scored[: args.review_candidate_limit], start=1):
                    review_rows.append(creature_review_row(jabbit, row, candidate, "exact_name", rank, best_candidate_score))
            else:
                fuzzy_candidates = fuzzy_creature_candidates(
                    jabbit.get("source_name"),
                    name_keys_by_first_char,
                    by_name,
                    args.review_candidate_limit,
                )
                if fuzzy_candidates:
                    fuzzy_scored = sorted(
                        (
                            score_creature_match(jabbit, candidate),
                            to_int(candidate.get("ID"), 0) or 0,
                            candidate,
                        )
                        for candidate in fuzzy_candidates
                    )
                    fuzzy_scored.sort(key=lambda item: (-item[0], item[1]))
                    best_candidate_score = fuzzy_scored[0][0]
                    for rank, (candidate_score, _, candidate) in enumerate(fuzzy_scored, start=1):
                        review_rows.append(creature_review_row(jabbit, row, candidate, "fuzzy_name", rank, best_candidate_score))
                else:
                    review_rows.append(creature_review_row(jabbit, row, {}, "no_candidate", "", 0))
        rows.append(row)
        if jabbit_id is not None:
            by_jabbit_id[jabbit_id] = row
    return rows, by_jabbit_id, review_rows


def write_csv(path: Path, fieldnames: Sequence[str], rows: Iterable[Dict[str, object]]) -> int:
    path.parent.mkdir(parents=True, exist_ok=True)
    count = 0
    with path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=fieldnames, extrasaction="ignore")
        writer.writeheader()
        for row in rows:
            writer.writerow({field: clean_cell(row.get(field, "")) for field in fieldnames})
            count += 1
    return count


def replace_file_with_retry(source: Path, target: Path, attempts: int = 20, delay_seconds: float = 0.5) -> None:
    last_error: Optional[Exception] = None
    for _ in range(attempts):
        try:
            source.replace(target)
            return
        except OSError as exc:
            last_error = exc
            time.sleep(delay_seconds)
    if last_error:
        raise last_error


def item_name(items: Dict[int, Dict[str, object]], item_id) -> str:
    item = items.get(to_int(item_id, -1) or -1)
    return clean_cell(item.get("itemName")) if item else ""


def lookup_name(rows: Dict[int, Dict[str, object]], row_id, field: str) -> str:
    row = rows.get(to_int(row_id, -1) or -1)
    return clean_cell(row.get(field)) if row else ""


def spell_name(client, spell4_id) -> str:
    spell = client["spells"].get(to_int(spell4_id, -1) or -1)
    if not spell:
        return ""
    base = client["spell_bases"].get(to_int(spell.get("spell4BaseIdBaseSpell"), -1) or -1, {})
    return clean_cell(base.get("spellBaseName") or spell.get("description") or spell.get("spellTooltip"))


CLIENT_GENERIC_SOURCE_NOTE = (
    "Generic client source map emitted with all source columns, localized text expansions, "
    "and known foreign-key labels."
)

CLIENT_GENERIC_REFERENCE_PREFIXES = [
    ("publiceventobjectiveid", "public_event_objective"),
    ("publiceventteamid", "public_event_team"),
    ("publiceventid", "public_event"),
    ("achievementcategoryid", "achievement_category"),
    ("achievementsubcategoryid", "achievement_subgroup"),
    ("achievementsubgroupid", "achievement_subgroup"),
    ("achievementgroupid", "achievement_group"),
    ("achievementid", "achievement"),
    ("characterTitlecategoryid", "character_title_category"),
    ("charactertitlecategoryid", "character_title_category"),
    ("charactertitleid", "character_title"),
    ("tradeskillschematic2id", "tradeskill_schematic"),
    ("tradeskillmaterialcategoryid", "tradeskill_material_category"),
    ("tradeskillid", "tradeskill"),
    ("housingdecorlimitcategoryid", "housing_decor_limit_category"),
    ("housingdecorinfoid", "housing_decor"),
    ("housingdecortypeid", "housing_decor_type"),
    ("housingplugitemid", "housing_plug_item"),
    ("housingwallpaperinfoid", "housing_wallpaper"),
    ("housingpropertyinfoid", "housing_property"),
    ("housingneighborhoodinfoid", "housing_neighborhood"),
    ("housingmapinfoid", "housing_map"),
    ("housingplottypeid", "housing_plot_type"),
    ("housingplotinfoid", "housing_plot"),
    ("housingresourceid", "housing_resource"),
    ("housingmannequinposeid", "housing_mannequin_pose"),
    ("mapzonenemesisregionid", "map_zone_nemesis_region"),
    ("mapzonehexgroupid", "map_zone_hex_group"),
    ("mapzonespriteid", "map_zone_sprite"),
    ("mapzonepoiid", "map_zone_poi"),
    ("mapcontinentid", "map_continent"),
    ("mapzoneid", "map_zone"),
    ("worldlocation2id", "world_location"),
    ("worldlocationsid", "world_location"),
    ("worldzoneid", "world_zone"),
    ("worldid", "world"),
    ("questcategoryid", "quest_category"),
    ("questdirectionid", "quest_direction"),
    ("questhubid", "quest_hub"),
    ("questobjectiveid", "quest_objective"),
    ("quest2id", "quest"),
    ("pathepisodeid", "path_episode"),
    ("pathmissionid", "path_mission"),
    ("currencytypeid", "currency"),
    ("creature2id", "creature"),
    ("item2categoryid", "item_category"),
    ("item2familyid", "item_family"),
    ("item2typeid", "item_type"),
    ("itemslotid", "item_slot"),
    ("item2id", "item"),
    ("spell4baseid", "spell_base"),
    ("spell4id", "spell"),
    ("spelllevelid", "spell_level"),
    ("spelleffecttypeid", "spell_effect_type"),
    ("faction2relationshipid", "faction_relationship"),
    ("faction2id", "faction"),
    ("factionid", "faction"),
    ("unitproperty2id", "unit_property"),
    ("unitpropertyid", "unit_property"),
    ("classid", "class"),
    ("bindpointid", "bind_point"),
    ("taxinodeid", "taxi_node"),
    ("taxirouteid", "taxi_route"),
    ("citydirectionid", "city_direction"),
    ("genericmapnodeid", "generic_map_node"),
    ("genericmapid", "generic_map"),
    ("challengeid", "challenge"),
    ("challengetierid", "challenge_tier"),
    ("virtualitemid", "virtual_item"),
    ("petflairid", "pet_flair"),
]

CLIENT_GENERIC_LOOKUPS = {
    "achievement": ("achievements", "achievementTitle"),
    "achievement_category": ("achievement_categories", "achievementCategoryName"),
    "achievement_group": ("achievement_groups", "achievementGroupName"),
    "achievement_subgroup": ("achievement_subgroups", "achievementSubGroupName"),
    "bind_point": ("bind_points", "bindPointName"),
    "challenge": ("challenges", "challengeName"),
    "character_title": ("character_titles", "characterTitleText"),
    "character_title_category": ("character_title_categories", "characterTitleCategoryName"),
    "city_direction": ("city_directions", "cityDirectionName"),
    "class": ("classes", "className"),
    "creature": ("creatures", "clientName"),
    "currency": ("currencies", "currencyName"),
    "faction": ("factions", "factionName"),
    "generic_map_node": ("generic_map_nodes", "genericMapNodeName"),
    "housing_decor": ("housing_decors", "housingDecorName"),
    "housing_decor_type": ("housing_decor_types", "housingDecorTypeName"),
    "housing_mannequin_pose": ("housing_mannequin_poses", "housingMannequinPoseName"),
    "housing_plug_item": ("housing_plug_items", "housingPlugName"),
    "housing_property": ("housing_properties", "housingPropertyName"),
    "housing_resource": ("housing_resources", "housingResourceName"),
    "housing_wallpaper": ("housing_wallpapers", "housingWallpaperName"),
    "item": ("items", "itemName"),
    "item_category": ("item_categories", "itemCategoryName"),
    "item_family": ("item_families", "itemFamilyName"),
    "item_slot": ("item_slots", "itemSlotName"),
    "item_type": ("item_types", "itemTypeName"),
    "map_continent": ("map_continents", "mapContinentName"),
    "map_zone": ("map_zones", "mapZoneName"),
    "map_zone_nemesis_region": ("map_zone_nemesis_regions", "nemesisRegionDescription"),
    "map_zone_poi": ("map_zone_pois", "mapZonePoiName"),
    "map_zone_sprite": ("map_zone_sprites", "spriteName"),
    "path_episode": ("path_episodes", "pathEpisodeName"),
    "path_mission": ("path_missions", "pathMissionName"),
    "pet_flair": ("pet_flairs", "petFlairTooltip"),
    "public_event": ("public_events", "publicEventName"),
    "public_event_objective": ("public_event_objectives", "publicEventObjectiveText"),
    "public_event_team": ("public_event_teams", "publicEventTeamName"),
    "quest": ("quests", "questName"),
    "quest_category": ("quest_categories", "questCategoryName"),
    "quest_hub": ("quest_hubs", "questHubName"),
    "quest_objective": ("quest_objectives", "questObjectiveText"),
    "taxi_node": ("taxi_nodes", "taxiNodeName"),
    "tradeskill": ("tradeskills", "tradeskillName"),
    "tradeskill_material_category": ("tradeskill_material_categories", "tradeskillMaterialCategoryName"),
    "tradeskill_schematic": ("tradeskill_schematics", "tradeskillSchematicName"),
    "unit_property": ("unit_properties", "unitPropertyName"),
    "virtual_item": ("virtual_items", "virtualItemName"),
    "world": ("worlds", "worldName"),
    "world_zone": ("world_zones", "worldZoneName"),
}


def compact_field_name(value: str) -> str:
    return re.sub(r"[^a-z0-9]", "", value.lower())


def generic_client_table_name(path: Path) -> str:
    try:
        create_statement = read_create_statement_header(path)
        table_name = parse_table_name(create_statement) or path.stem
    except Exception:
        table_name = path.stem
    return table_name


def generic_client_map_filename(path: Path) -> str:
    table_name = generic_client_table_name(path)
    safe_name = re.sub(r"[^A-Za-z0-9]+", "_", table_name).strip("_").lower()
    if not safe_name:
        safe_name = re.sub(r"[^A-Za-z0-9]+", "_", path.stem).strip("_").lower()
    return f"client_source_{safe_name}_map.csv"


def should_write_generic_client_source_map(filename: str) -> bool:
    status = CLIENT_FILE_STATUS.get(filename, ("unmapped", ""))[0]
    return status != "mapped"


def client_coverage_status(filename: str) -> Tuple[str, str]:
    status_notes = CLIENT_FILE_STATUS.get(filename)
    if status_notes is None:
        return "mapped", CLIENT_GENERIC_SOURCE_NOTE
    status, notes = status_notes
    if status == "partial":
        return "mapped", f"{notes} {CLIENT_GENERIC_SOURCE_NOTE}"
    return status, notes


def localized_text_output_columns(columns: Sequence[str]) -> List[Tuple[str, str]]:
    pairs: List[Tuple[str, str]] = []
    for column in columns:
        if compact_field_name(column).startswith("localizedtextid"):
            pairs.append((column, f"{column}_text"))
    return pairs


def generic_reference_kind(column: str) -> str:
    compact = compact_field_name(column)
    for prefix, kind in CLIENT_GENERIC_REFERENCE_PREFIXES:
        normalized_prefix = compact_field_name(prefix)
        if compact.startswith(normalized_prefix) or compact.endswith(normalized_prefix):
            return kind
    return ""


def generic_reference_output_columns(columns: Sequence[str]) -> List[Tuple[str, str, str]]:
    pairs: List[Tuple[str, str, str]] = []
    for column in columns:
        kind = generic_reference_kind(column)
        if kind:
            pairs.append((column, f"{column}_label", kind))
    return pairs


def world_location_label(client, world_location2_id) -> str:
    location = client["world_locations"].get(to_int(world_location2_id, -1) or -1)
    if not location:
        return ""
    world_name = lookup_name(client["worlds"], location.get("worldId"), "worldName")
    zone_name = lookup_name(client["world_zones"], location.get("worldZoneId"), "worldZoneName")
    coordinates = [
        clean_cell(location.get("position0")),
        clean_cell(location.get("position1")),
        clean_cell(location.get("position2")),
    ]
    label_parts = [part for part in [world_name, zone_name] if part]
    if any(coordinates):
        label_parts.append(f"({', '.join(coordinates)})")
    return " / ".join(label_parts)


def generic_reference_label(client, kind: str, value) -> str:
    if kind == "spell":
        return spell_name(client, value)
    if kind == "spell_base":
        return lookup_name(client["spell_bases"], value, "spellBaseName")
    if kind == "world_location":
        return world_location_label(client, value)
    lookup = CLIENT_GENERIC_LOOKUPS.get(kind)
    if not lookup:
        return ""
    table_key, name_field = lookup
    return lookup_name(client.get(table_key, {}), value, name_field)


def write_generic_client_source_maps(args: argparse.Namespace, client) -> Dict[str, int]:
    counts: Dict[str, int] = {}
    inventory_rows: List[Dict[str, object]] = []

    paths = [
        path
        for path in sorted(args.client_sql_dir.glob("*.sql"), key=lambda item: item.name.lower())
        if should_write_generic_client_source_map(path.name)
    ]

    for path in paths:
        output_name = generic_client_map_filename(path)
        log(f"Writing {output_name}")
        columns = read_columns(path)
        text_columns = localized_text_output_columns(columns)
        reference_columns = generic_reference_output_columns(columns)

        fieldnames: List[str] = []
        derived_by_source: Dict[str, List[str]] = defaultdict(list)
        for column in columns:
            fieldnames.append(column)
            for source_column, output_column in text_columns:
                if source_column == column:
                    fieldnames.append(output_column)
                    derived_by_source[column].append(output_column)
            for source_column, output_column, _ in reference_columns:
                if source_column == column and output_column not in fieldnames:
                    fieldnames.append(output_column)
                    derived_by_source[column].append(output_column)

        def rows():
            for row in iter_table_rows(path):
                enriched = dict(row)
                for source_column, output_column in text_columns:
                    enriched[output_column] = display_text(client["strings"].get(to_int(row.get(source_column)), ""))
                for source_column, output_column, kind in reference_columns:
                    enriched[output_column] = generic_reference_label(client, kind, row.get(source_column))
                yield enriched

        row_count = write_csv(args.output_dir / output_name, fieldnames, rows())
        counts[output_name] = row_count
        inventory_rows.append(
            {
                "filename": path.name,
                "table_name": generic_client_table_name(path),
                "output_file": output_name,
                "row_count": row_count,
                "column_count": len(columns),
                "localized_text_columns": len(text_columns),
                "reference_label_columns": len(reference_columns),
            }
        )

    counts["client_source_map_inventory.csv"] = write_csv(
        args.output_dir / "client_source_map_inventory.csv",
        [
            "filename",
            "table_name",
            "output_file",
            "row_count",
            "column_count",
            "localized_text_columns",
            "reference_label_columns",
        ],
        inventory_rows,
    )
    return counts


def apply_creature_map(row: Dict[str, object], creature_map: Dict[int, Dict[str, object]], source_field: str = "jabbithole_creature_id") -> Dict[str, object]:
    mapped = creature_map.get(to_int(row.get(source_field), -1) or -1, {})
    row["creature2_id"] = mapped.get("creature2_id", "")
    row["creature2_name"] = mapped.get("client_name", "")
    row["source_name"] = mapped.get("source_name", row.get("source_name", ""))
    row["match_status"] = mapped.get("match_status", "")
    row["entity_type"] = mapped.get("entity_type", "")
    return row


def query_relation(
    args: argparse.Namespace,
    name: str,
    query: str,
    columns: Sequence[str],
    fieldnames: Sequence[str],
    creature_map: Dict[int, Dict[str, object]],
    transform,
    limit: Optional[int],
) -> Tuple[str, int]:
    output = args.output_dir / f"{name}.csv"
    log(f"Writing {output.name}")

    def rows():
        for row in mysql_rows(args, limited(query.strip(), limit), columns):
            row = apply_creature_map(row, creature_map)
            yield transform(row)

    count = write_csv(output, fieldnames, rows())
    return output.name, count


def write_reference_maps(args: argparse.Namespace, client) -> Dict[str, int]:
    counts: Dict[str, int] = {}

    counts["item_client_map.csv"] = write_csv(
        args.output_dir / "item_client_map.csv",
        [
            "item2_id",
            "item_name",
            "item_quality_id",
            "item2_family_id",
            "item2_category_id",
            "item2_type_id",
            "required_level",
            "max_stack_count",
            "buy_currency_0",
            "buy_amount_0",
            "sell_currency_0",
            "sell_amount_0",
        ],
        (
            {
                "item2_id": item_id,
                "item_name": row.get("itemName", ""),
                "item_quality_id": row.get("itemQualityId", ""),
                "item2_family_id": row.get("item2FamilyId", ""),
                "item2_category_id": row.get("item2CategoryId", ""),
                "item2_type_id": row.get("item2TypeId", ""),
                "required_level": row.get("requiredLevel", ""),
                "max_stack_count": row.get("maxStackCount", ""),
                "buy_currency_0": row.get("currencyTypeId0", ""),
                "buy_amount_0": row.get("currencyAmount0", ""),
                "sell_currency_0": row.get("currencyTypeId0SellToVendor", ""),
                "sell_amount_0": row.get("currencyAmount0SellToVendor", ""),
            }
            for item_id, row in sorted(client["items"].items())
        ),
    )

    counts["item_family_client_map.csv"] = write_csv(
        args.output_dir / "item_family_client_map.csv",
        ["item2_family_id", "item_family_name", "flags", "vendor_multiplier", "turnin_multiplier"],
        (
            {
                "item2_family_id": family_id,
                "item_family_name": row.get("itemFamilyName", ""),
                "flags": row.get("flags", ""),
                "vendor_multiplier": row.get("vendorMultiplier", ""),
                "turnin_multiplier": row.get("turninMultiplier", ""),
            }
            for family_id, row in sorted(client["item_families"].items())
        ),
    )

    counts["item_category_client_map.csv"] = write_csv(
        args.output_dir / "item_category_client_map.csv",
        [
            "item2_category_id",
            "item_category_name",
            "item2_family_id",
            "item_family_name",
            "item_proficiency_id",
            "tradeskill_id",
            "flags",
            "vendor_multiplier",
            "turnin_multiplier",
            "armor_modifier",
            "armor_base",
            "weapon_power_modifier",
            "weapon_power_base",
        ],
        (
            {
                "item2_category_id": category_id,
                "item_category_name": row.get("itemCategoryName", ""),
                "item2_family_id": row.get("item2FamilyId", ""),
                "item_family_name": lookup_name(client["item_families"], row.get("item2FamilyId"), "itemFamilyName"),
                "item_proficiency_id": row.get("itemProficiencyId", ""),
                "tradeskill_id": row.get("tradeSkillId", ""),
                "flags": row.get("flags", ""),
                "vendor_multiplier": row.get("vendorMultiplier", ""),
                "turnin_multiplier": row.get("turninMultiplier", ""),
                "armor_modifier": row.get("armorModifier", ""),
                "armor_base": row.get("armorBase", ""),
                "weapon_power_modifier": row.get("weaponPowerModifier", ""),
                "weapon_power_base": row.get("weaponPowerBase", ""),
            }
            for category_id, row in sorted(client["item_categories"].items())
        ),
    )

    counts["item_type_client_map.csv"] = write_csv(
        args.output_dir / "item_type_client_map.csv",
        [
            "item2_type_id",
            "item_type_name",
            "item2_category_id",
            "item_category_name",
            "item_slot_id",
            "item_slot_name",
            "flags",
            "vendor_multiplier",
            "turnin_multiplier",
        ],
        (
            {
                "item2_type_id": type_id,
                "item_type_name": row.get("itemTypeName", ""),
                "item2_category_id": row.get("Item2CategoryId", ""),
                "item_category_name": lookup_name(client["item_categories"], row.get("Item2CategoryId"), "itemCategoryName"),
                "item_slot_id": row.get("itemSlotId", ""),
                "item_slot_name": lookup_name(client["item_slots"], row.get("itemSlotId"), "itemSlotName"),
                "flags": row.get("flags", ""),
                "vendor_multiplier": row.get("vendorMultiplier", ""),
                "turnin_multiplier": row.get("turninMultiplier", ""),
            }
            for type_id, row in sorted(client["item_types"].items())
        ),
    )

    counts["item_slot_client_map.csv"] = write_csv(
        args.output_dir / "item_slot_client_map.csv",
        [
            "item_slot_id",
            "item_slot_name",
            "equipped_slot_flags",
            "armor_modifier",
            "item_level_modifier",
            "slot_bonus",
            "glyph_slot_bonus",
            "min_level",
        ],
        (
            {
                "item_slot_id": slot_id,
                "item_slot_name": row.get("itemSlotName", ""),
                "equipped_slot_flags": row.get("equippedSlotFlags", ""),
                "armor_modifier": row.get("armorModifier", ""),
                "item_level_modifier": row.get("itemLevelModifier", ""),
                "slot_bonus": row.get("slotBonus", ""),
                "glyph_slot_bonus": row.get("glyphSlotBonus", ""),
                "min_level": row.get("minLevel", ""),
            }
            for slot_id, row in sorted(client["item_slots"].items())
        ),
    )

    counts["class_client_map.csv"] = write_csv(
        args.output_dir / "class_client_map.csv",
        [
            "class_id",
            "class_name",
            "enum_name",
            "description",
            "mechanic",
            "innate_active_spell_0",
            "innate_active_spell_0_name",
            "innate_active_spell_1",
            "innate_active_spell_1_name",
            "innate_active_spell_2",
            "innate_active_spell_2_name",
            "innate_passive_spell_0",
            "innate_passive_spell_0_name",
            "innate_passive_spell_1",
            "innate_passive_spell_1_name",
            "innate_passive_spell_2",
            "innate_passive_spell_2_name",
            "primary_attack_spell_0",
            "primary_attack_spell_0_name",
            "res_spell4_id",
            "res_spell_name",
        ],
        (
            {
                "class_id": class_id,
                "class_name": row.get("className", ""),
                "enum_name": row.get("enumName", ""),
                "description": row.get("classDescription", ""),
                "mechanic": row.get("mechanic", ""),
                "innate_active_spell_0": row.get("spell4IdInnateAbilityActive00", ""),
                "innate_active_spell_0_name": spell_name(client, row.get("spell4IdInnateAbilityActive00")),
                "innate_active_spell_1": row.get("spell4IdInnateAbilityActive01", ""),
                "innate_active_spell_1_name": spell_name(client, row.get("spell4IdInnateAbilityActive01")),
                "innate_active_spell_2": row.get("spell4IdInnateAbilityActive02", ""),
                "innate_active_spell_2_name": spell_name(client, row.get("spell4IdInnateAbilityActive02")),
                "innate_passive_spell_0": row.get("spell4IdInnateAbilityPassive00", ""),
                "innate_passive_spell_0_name": spell_name(client, row.get("spell4IdInnateAbilityPassive00")),
                "innate_passive_spell_1": row.get("spell4IdInnateAbilityPassive01", ""),
                "innate_passive_spell_1_name": spell_name(client, row.get("spell4IdInnateAbilityPassive01")),
                "innate_passive_spell_2": row.get("spell4IdInnateAbilityPassive02", ""),
                "innate_passive_spell_2_name": spell_name(client, row.get("spell4IdInnateAbilityPassive02")),
                "primary_attack_spell_0": row.get("spell4IdAttackPrimary00", ""),
                "primary_attack_spell_0_name": spell_name(client, row.get("spell4IdAttackPrimary00")),
                "res_spell4_id": row.get("spell4IdResAbility", ""),
                "res_spell_name": spell_name(client, row.get("spell4IdResAbility")),
            }
            for class_id, row in sorted(client["classes"].items())
        ),
    )

    counts["spell_client_map.csv"] = write_csv(
        args.output_dir / "spell_client_map.csv",
        [
            "spell4_id",
            "spell_name",
            "spell_description",
            "spell_tooltip",
            "spell4_base_id",
            "tier_index",
            "class_id",
            "class_name",
            "spell_type_id",
            "spell_type_name",
            "icon",
            "cast_time_ms",
            "duration_ms",
            "cooldown_ms",
            "target_min_range",
            "target_max_range",
            "innate_cost_type_0",
            "innate_cost_0",
            "innate_cost_type_1",
            "innate_cost_1",
            "ability_point_cost",
            "training_cost",
            "ability_charge_count",
            "ability_recharge_time",
            "global_cooldown",
            "property_flags",
            "ui_flags",
        ],
        (
            {
                "spell4_id": spell_id,
                "spell_name": base.get("spellBaseName", ""),
                "spell_description": row.get("description", ""),
                "spell_tooltip": row.get("spellTooltip", ""),
                "spell4_base_id": base_id,
                "tier_index": row.get("tierIndex", ""),
                "class_id": base.get("classIdPlayer", ""),
                "class_name": lookup_name(client["classes"], base.get("classIdPlayer"), "className"),
                "spell_type_id": base.get("spell4SpellTypesIdSpellType", ""),
                "spell_type_name": lookup_name(client["spell_types"], base.get("spell4SpellTypesIdSpellType"), "typeName"),
                "icon": base.get("icon", ""),
                "cast_time_ms": row.get("castTime", ""),
                "duration_ms": row.get("spellDuration", ""),
                "cooldown_ms": row.get("spellCoolDown", ""),
                "target_min_range": row.get("targetMinRange", ""),
                "target_max_range": row.get("targetMaxRange", ""),
                "innate_cost_type_0": row.get("innateCostType0", ""),
                "innate_cost_0": row.get("innateCost0", ""),
                "innate_cost_type_1": row.get("innateCostType1", ""),
                "innate_cost_1": row.get("innateCost1", ""),
                "ability_point_cost": row.get("abilityPointCost", ""),
                "training_cost": row.get("trainingCost", ""),
                "ability_charge_count": row.get("abilityChargeCount", ""),
                "ability_recharge_time": row.get("abilityRechargeTime", ""),
                "global_cooldown": row.get("globalCooldownEnum", ""),
                "property_flags": row.get("propertyFlags", ""),
                "ui_flags": row.get("uiFlags", ""),
            }
            for spell_id, row in sorted(client["spells"].items())
            for base_id in [to_int(row.get("spell4BaseIdBaseSpell"), 0) or 0]
            for base in [client["spell_bases"].get(base_id, {})]
        ),
    )

    counts["spell_effect_client_map.csv"] = write_csv(
        args.output_dir / "spell_effect_client_map.csv",
        [
            "spell4_effect_id",
            "spell4_id",
            "spell_name",
            "effect_type",
            "effect_type_flags",
            "damage_type",
            "target_flags",
            "delay_time_ms",
            "tick_time_ms",
            "duration_time_ms",
            "flags",
            "data_bits_00",
            "data_bits_01",
            "data_bits_02",
            "data_bits_03",
            "parameter_type_00",
            "parameter_value_00",
            "parameter_type_01",
            "parameter_value_01",
            "parameter_type_02",
            "parameter_value_02",
            "parameter_type_03",
            "parameter_value_03",
            "threat_multiplier",
            "phase_flags",
            "order_index",
        ],
        (
            {
                "spell4_effect_id": effect_id,
                "spell4_id": row.get("spellId", ""),
                "spell_name": spell_name(client, row.get("spellId")),
                "effect_type": row.get("effectType", ""),
                "effect_type_flags": client["spell_effect_types"].get(to_int(row.get("effectType"), -1) or -1, {}).get("flags", ""),
                "damage_type": row.get("damageType", ""),
                "target_flags": row.get("targetFlags", ""),
                "delay_time_ms": row.get("delayTime", ""),
                "tick_time_ms": row.get("tickTime", ""),
                "duration_time_ms": row.get("durationTime", ""),
                "flags": row.get("flags", ""),
                "data_bits_00": row.get("dataBits00", ""),
                "data_bits_01": row.get("dataBits01", ""),
                "data_bits_02": row.get("dataBits02", ""),
                "data_bits_03": row.get("dataBits03", ""),
                "parameter_type_00": row.get("parameterType00", ""),
                "parameter_value_00": row.get("parameterValue00", ""),
                "parameter_type_01": row.get("parameterType01", ""),
                "parameter_value_01": row.get("parameterValue01", ""),
                "parameter_type_02": row.get("parameterType02", ""),
                "parameter_value_02": row.get("parameterValue02", ""),
                "parameter_type_03": row.get("parameterType03", ""),
                "parameter_value_03": row.get("parameterValue03", ""),
                "threat_multiplier": row.get("threatMultiplier", ""),
                "phase_flags": row.get("phaseFlags", ""),
                "order_index": row.get("orderIndex", ""),
            }
            for effect_id, row in sorted(client["spell_effects"].items())
        ),
    )

    counts["spell_level_client_map.csv"] = write_csv(
        args.output_dir / "spell_level_client_map.csv",
        [
            "spell_level_id",
            "class_id",
            "class_name",
            "character_level",
            "prerequisite_id",
            "spell4_id",
            "spell_name",
            "cost_multiplier",
        ],
        (
            {
                "spell_level_id": row_id,
                "class_id": row.get("classId", ""),
                "class_name": lookup_name(client["classes"], row.get("classId"), "className"),
                "character_level": row.get("characterLevel", ""),
                "prerequisite_id": row.get("prerequisiteId", ""),
                "spell4_id": row.get("spell4Id", ""),
                "spell_name": spell_name(client, row.get("spell4Id")),
                "cost_multiplier": row.get("costMultiplier", ""),
            }
            for row_id, row in sorted(client["spell_levels"].items())
        ),
    )

    counts["spell_tier_requirement_client_map.csv"] = write_csv(
        args.output_dir / "spell_tier_requirement_client_map.csv",
        ["spell_tier_requirement_id", "tier_index", "level_requirement"],
        (
            {
                "spell_tier_requirement_id": row_id,
                "tier_index": row.get("tierIndex", ""),
                "level_requirement": row.get("levelRequirement", ""),
            }
            for row_id, row in sorted(client["spell_tier_requirements"].items())
        ),
    )

    counts["quest_client_map.csv"] = write_csv(
        args.output_dir / "quest_client_map.csv",
        [
            "quest2_id",
            "quest_name",
            "world_zone_id",
            "type",
            "sub_type",
            "path_type",
            "con_level",
            "preq_level",
            "reward_xp",
            "reward_cash",
        ],
        (
            {
                "quest2_id": quest_id,
                "quest_name": row.get("questName", ""),
                "world_zone_id": row.get("worldZoneId", ""),
                "type": row.get("type", ""),
                "sub_type": row.get("quest2SubTypeId", ""),
                "path_type": row.get("subMissionPathType", ""),
                "con_level": row.get("conLevel", ""),
                "preq_level": row.get("preq_level", ""),
                "reward_xp": row.get("reward_xpOverride", ""),
                "reward_cash": row.get("reward_cashOverride", ""),
            }
            for quest_id, row in sorted(client["quests"].items())
        ),
    )

    objective_slot_fields = ["objective0", "objective01", "objective02", "objective03", "objective04", "objective05"]
    counts["quest_objective_client_map.csv"] = write_csv(
        args.output_dir / "quest_objective_client_map.csv",
        [
            "quest2_id",
            "quest_name",
            "objective_order",
            "quest_objective_id",
            "objective_text",
            "type",
            "flags",
            "data",
            "count",
            "world_location_0",
            "world_location_1",
            "world_location_2",
            "world_location_3",
            "max_time_allowed_ms",
            "quest_direction_id",
        ],
        (
            {
                "quest2_id": quest_id,
                "quest_name": quest.get("questName", ""),
                "objective_order": order,
                "quest_objective_id": objective_id,
                "objective_text": client["quest_objectives"].get(objective_id, {}).get("questObjectiveText", ""),
                "type": client["quest_objectives"].get(objective_id, {}).get("type", ""),
                "flags": client["quest_objectives"].get(objective_id, {}).get("flags", ""),
                "data": client["quest_objectives"].get(objective_id, {}).get("data", ""),
                "count": client["quest_objectives"].get(objective_id, {}).get("count", ""),
                "world_location_0": client["quest_objectives"].get(objective_id, {}).get("worldLocationsIdIndicator00", ""),
                "world_location_1": client["quest_objectives"].get(objective_id, {}).get("worldLocationsIdIndicator01", ""),
                "world_location_2": client["quest_objectives"].get(objective_id, {}).get("worldLocationsIdIndicator02", ""),
                "world_location_3": client["quest_objectives"].get(objective_id, {}).get("worldLocationsIdIndicator03", ""),
                "max_time_allowed_ms": client["quest_objectives"].get(objective_id, {}).get("maxTimeAllowedMS", ""),
                "quest_direction_id": client["quest_objectives"].get(objective_id, {}).get("questDirectionId", ""),
            }
            for quest_id, quest in sorted(client["quests"].items())
            for order, field in enumerate(objective_slot_fields)
            for objective_id in [to_int(quest.get(field))]
            if objective_id
        ),
    )

    counts["quest_reward_client_map.csv"] = write_csv(
        args.output_dir / "quest_reward_client_map.csv",
        [
            "quest2_reward_id",
            "quest2_id",
            "quest_name",
            "reward_type_id",
            "object_id",
            "object_amount",
            "flags",
        ],
        (
            {
                "quest2_reward_id": reward_id,
                "quest2_id": row.get("quest2Id", ""),
                "quest_name": lookup_name(client["quests"], row.get("quest2Id"), "questName"),
                "reward_type_id": row.get("quest2RewardTypeId", ""),
                "object_id": row.get("objectId", ""),
                "object_amount": row.get("objectAmount", ""),
                "flags": row.get("flags", ""),
            }
            for reward_id, row in sorted(client["quest_rewards"].items())
        ),
    )

    counts["path_mission_client_map.csv"] = write_csv(
        args.output_dir / "path_mission_client_map.csv",
        [
            "path_mission_id",
            "path_mission_name",
            "path_type",
            "mission_type",
            "display_type",
            "object_id",
            "creature2_id_unlock",
            "creature2_id_contact_override",
            "world_location_0",
            "world_location_1",
            "world_location_2",
            "world_location_3",
            "faction_enum",
        ],
        (
            {
                "path_mission_id": mission_id,
                "path_mission_name": row.get("pathMissionName", ""),
                "path_type": row.get("pathTypeEnum", ""),
                "mission_type": row.get("pathMissionTypeEnum", ""),
                "display_type": row.get("pathMissionDisplayTypeEnum", ""),
                "object_id": row.get("objectId", ""),
                "creature2_id_unlock": row.get("creature2IdUnlock", ""),
                "creature2_id_contact_override": row.get("creature2IdContactOverride", ""),
                "world_location_0": row.get("worldLocation2Id00", ""),
                "world_location_1": row.get("worldLocation2Id01", ""),
                "world_location_2": row.get("worldLocation2Id02", ""),
                "world_location_3": row.get("worldLocation2Id03", ""),
                "faction_enum": row.get("pathMissionFactionEnum", ""),
            }
            for mission_id, row in sorted(client["path_missions"].items())
        ),
    )

    counts["path_episode_client_map.csv"] = write_csv(
        args.output_dir / "path_episode_client_map.csv",
        [
            "path_episode_id",
            "path_episode_name",
            "path_episode_summary",
            "path_type",
            "world_id",
            "world_zone_id",
            "world_zone_name",
        ],
        (
            {
                "path_episode_id": episode_id,
                "path_episode_name": row.get("pathEpisodeName", ""),
                "path_episode_summary": row.get("pathEpisodeSummary", ""),
                "path_type": row.get("pathTypeEnum", ""),
                "world_id": row.get("worldId", ""),
                "world_zone_id": row.get("worldZoneId", ""),
                "world_zone_name": lookup_name(client["world_zones"], row.get("worldZoneId"), "worldZoneName"),
            }
            for episode_id, row in sorted(client["path_episodes"].items())
        ),
    )

    counts["path_reward_client_map.csv"] = write_csv(
        args.output_dir / "path_reward_client_map.csv",
        [
            "path_reward_id",
            "reward_type",
            "object_id",
            "spell4_id",
            "item2_id",
            "item_name",
            "quest2_id",
            "quest_name",
            "character_title_id",
            "character_title_name",
            "prerequisite_id",
            "count",
            "flags",
            "scientist_scan_bot_profile_id",
        ],
        (
            {
                "path_reward_id": reward_id,
                "reward_type": row.get("pathRewardTypeEnum", ""),
                "object_id": row.get("objectId", ""),
                "spell4_id": row.get("spell4Id", ""),
                "item2_id": row.get("item2Id", ""),
                "item_name": item_name(client["items"], row.get("item2Id")),
                "quest2_id": row.get("quest2Id", ""),
                "quest_name": lookup_name(client["quests"], row.get("quest2Id"), "questName"),
                "character_title_id": row.get("characterTitleId", ""),
                "character_title_name": lookup_name(client["character_titles"], row.get("characterTitleId"), "characterTitleName"),
                "prerequisite_id": row.get("prerequisiteId", ""),
                "count": row.get("count", ""),
                "flags": row.get("pathRewardFlags", ""),
                "scientist_scan_bot_profile_id": row.get("pathScientistScanBotProfileId", ""),
            }
            for reward_id, row in sorted(client["path_rewards"].items())
        ),
    )

    counts["path_level_client_map.csv"] = write_csv(
        args.output_dir / "path_level_client_map.csv",
        ["path_level_row_id", "path_type", "path_level", "path_xp"],
        (
            {
                "path_level_row_id": row_id,
                "path_type": row.get("pathTypeEnum", ""),
                "path_level": row.get("pathLevel", ""),
                "path_xp": row.get("pathXP", ""),
            }
            for row_id, row in sorted(client["path_levels"].items())
        ),
    )

    counts["character_title_client_map.csv"] = write_csv(
        args.output_dir / "character_title_client_map.csv",
        [
            "character_title_id",
            "character_title_name",
            "character_title_text",
            "category_id",
            "category_name",
            "activate_spell4_id",
            "lifetime_seconds",
            "flags",
        ],
        (
            {
                "character_title_id": title_id,
                "character_title_name": row.get("characterTitleName", ""),
                "character_title_text": row.get("characterTitleText", ""),
                "category_id": row.get("characterTitleCategoryId", ""),
                "category_name": lookup_name(client["character_title_categories"], row.get("characterTitleCategoryId"), "characterTitleCategoryName"),
                "activate_spell4_id": row.get("spell4IdActivate", ""),
                "lifetime_seconds": row.get("lifeTimeSeconds", ""),
                "flags": row.get("playerTitleFlagsEnum", ""),
            }
            for title_id, row in sorted(client["character_titles"].items())
        ),
    )

    counts["achievement_client_map.csv"] = write_csv(
        args.output_dir / "achievement_client_map.csv",
        [
            "achievement_id",
            "achievement_title",
            "description",
            "progress_text",
            "achievement_type_id",
            "achievement_category_id",
            "achievement_category_name",
            "achievement_group_id",
            "achievement_group_name",
            "achievement_subgroup_id",
            "achievement_subgroup_name",
            "world_zone_id",
            "world_zone_name",
            "object_id",
            "object_id_alt",
            "value",
            "character_title_id",
            "character_title_name",
            "parent_tier_id",
            "points_enum",
            "flags",
            "order_index",
        ],
        (
            {
                "achievement_id": achievement_id,
                "achievement_title": row.get("achievementTitle", ""),
                "description": row.get("achievementDescription", ""),
                "progress_text": row.get("achievementProgress", ""),
                "achievement_type_id": row.get("achievementTypeId", ""),
                "achievement_category_id": row.get("achievementCategoryId", ""),
                "achievement_category_name": lookup_name(client["achievement_categories"], row.get("achievementCategoryId"), "achievementCategoryName"),
                "achievement_group_id": row.get("achievementGroupId", ""),
                "achievement_group_name": lookup_name(client["achievement_groups"], row.get("achievementGroupId"), "achievementGroupName"),
                "achievement_subgroup_id": row.get("achievementSubGroupId", ""),
                "achievement_subgroup_name": lookup_name(client["achievement_subgroups"], row.get("achievementSubGroupId"), "achievementSubGroupName"),
                "world_zone_id": row.get("worldZoneId", ""),
                "world_zone_name": lookup_name(client["world_zones"], row.get("worldZoneId"), "worldZoneName"),
                "object_id": row.get("objectId", ""),
                "object_id_alt": row.get("objectIdAlt", ""),
                "value": row.get("value", ""),
                "character_title_id": row.get("characterTitleId", ""),
                "character_title_name": lookup_name(client["character_titles"], row.get("characterTitleId"), "characterTitleName"),
                "parent_tier_id": row.get("achievementIdParentTier", ""),
                "points_enum": row.get("achievementPointEnum", ""),
                "flags": row.get("flags", ""),
                "order_index": row.get("orderIndex", ""),
            }
            for achievement_id, row in sorted(client["achievements"].items())
        ),
    )

    counts["achievement_checklist_client_map.csv"] = write_csv(
        args.output_dir / "achievement_checklist_client_map.csv",
        [
            "achievement_checklist_id",
            "achievement_id",
            "achievement_title",
            "bit",
            "object_id",
            "object_id_alt",
            "prerequisite_id",
            "prerequisite_id_alt",
        ],
        (
            {
                "achievement_checklist_id": checklist_id,
                "achievement_id": row.get("achievementId", ""),
                "achievement_title": lookup_name(client["achievements"], row.get("achievementId"), "achievementTitle"),
                "bit": row.get("bit", ""),
                "object_id": row.get("objectId", ""),
                "object_id_alt": row.get("objectIdAlt", ""),
                "prerequisite_id": row.get("prerequisiteId", ""),
                "prerequisite_id_alt": row.get("prerequisiteIdAlt", ""),
            }
            for checklist_id, row in sorted(client["achievement_checklists"].items())
        ),
    )

    counts["achievement_group_client_map.csv"] = write_csv(
        args.output_dir / "achievement_group_client_map.csv",
        ["achievement_group_id", "achievement_group_name", "tradeskill_id", "tradeskill_name"],
        (
            {
                "achievement_group_id": group_id,
                "achievement_group_name": row.get("achievementGroupName", ""),
                "tradeskill_id": row.get("tradeSkillId", ""),
                "tradeskill_name": lookup_name(client["tradeskills"], row.get("tradeSkillId"), "tradeskillName"),
            }
            for group_id, row in sorted(client["achievement_groups"].items())
        ),
    )

    counts["achievement_category_client_map.csv"] = write_csv(
        args.output_dir / "achievement_category_client_map.csv",
        ["achievement_category_id", "achievement_category_name", "achievement_category_full_name", "parent_category_id", "parent_category_name"],
        (
            {
                "achievement_category_id": category_id,
                "achievement_category_name": row.get("achievementCategoryName", ""),
                "achievement_category_full_name": row.get("achievementCategoryFullName", ""),
                "parent_category_id": row.get("achievementCategoryIdParent", ""),
                "parent_category_name": lookup_name(client["achievement_categories"], row.get("achievementCategoryIdParent"), "achievementCategoryName"),
            }
            for category_id, row in sorted(client["achievement_categories"].items())
        ),
    )

    counts["achievement_subgroup_client_map.csv"] = write_csv(
        args.output_dir / "achievement_subgroup_client_map.csv",
        ["achievement_subgroup_id", "achievement_subgroup_name", "tier"],
        (
            {
                "achievement_subgroup_id": subgroup_id,
                "achievement_subgroup_name": row.get("achievementSubGroupName", ""),
                "tier": row.get("tier", ""),
            }
            for subgroup_id, row in sorted(client["achievement_subgroups"].items())
        ),
    )

    counts["achievement_text_client_map.csv"] = write_csv(
        args.output_dir / "achievement_text_client_map.csv",
        ["achievement_text_id", "achievement_text"],
        (
            {
                "achievement_text_id": text_id,
                "achievement_text": row.get("achievementText", ""),
            }
            for text_id, row in sorted(client["achievement_texts"].items())
        ),
    )

    counts["public_event_client_map.csv"] = write_csv(
        args.output_dir / "public_event_client_map.csv",
        [
            "public_event_id",
            "public_event_name",
            "world_id",
            "world_zone_id",
            "world_location_id",
            "event_type",
            "parent_public_event_id",
            "min_player_level",
            "flags",
        ],
        (
            {
                "public_event_id": event_id,
                "public_event_name": row.get("publicEventName", ""),
                "world_id": row.get("worldId", ""),
                "world_zone_id": row.get("worldZoneId", ""),
                "world_location_id": row.get("worldLocation2Id", ""),
                "event_type": row.get("publicEventTypeEnum", ""),
                "parent_public_event_id": row.get("publicEventIdParent", ""),
                "min_player_level": row.get("minPlayerLevel", ""),
                "flags": row.get("publicEventFlags", ""),
            }
            for event_id, row in sorted(client["public_events"].items())
        ),
    )

    counts["public_event_objective_client_map.csv"] = write_csv(
        args.output_dir / "public_event_objective_client_map.csv",
        [
            "public_event_objective_id",
            "public_event_id",
            "public_event_name",
            "objective_text",
            "objective_type",
            "objective_flags",
            "objective_type_specific_flags",
            "objective_count",
            "objective_object_id",
            "world_location_id",
            "public_event_team_id",
            "public_event_team_name",
            "objective_category",
            "parent_public_event_objective_id",
            "quest_direction_id",
            "display_order",
            "medal_point_value",
            "failure_time_ms",
            "target_group_id_reward_pane",
        ],
        (
            {
                "public_event_objective_id": objective_id,
                "public_event_id": row.get("publicEventId", ""),
                "public_event_name": lookup_name(client["public_events"], row.get("publicEventId"), "publicEventName"),
                "objective_text": row.get("publicEventObjectiveText", ""),
                "objective_type": row.get("publicEventObjectiveTypeEnum", ""),
                "objective_flags": row.get("publicEventObjectiveFlags", ""),
                "objective_type_specific_flags": row.get("publicEventObjectiveTypeSpecificFlags", ""),
                "objective_count": row.get("count", ""),
                "objective_object_id": row.get("objectId", ""),
                "world_location_id": row.get("worldLocation2Id", ""),
                "public_event_team_id": row.get("publicEventTeamId", ""),
                "public_event_team_name": lookup_name(client["public_event_teams"], row.get("publicEventTeamId"), "publicEventTeamName"),
                "objective_category": row.get("publicEventObjectiveCategoryEnum", ""),
                "parent_public_event_objective_id": row.get("publicEventObjectiveIdParent", ""),
                "quest_direction_id": row.get("questDirectionId", ""),
                "display_order": row.get("displayOrder", ""),
                "medal_point_value": row.get("medalPointValue", ""),
                "failure_time_ms": row.get("failureTimeMs", ""),
                "target_group_id_reward_pane": row.get("targetGroupIdRewardPane", ""),
            }
            for objective_id, row in sorted(client["public_event_objectives"].items())
        ),
    )

    counts["public_event_depot_client_map.csv"] = write_csv(
        args.output_dir / "public_event_depot_client_map.csv",
        [
            "public_event_depot_id",
            "creature2_id",
            "creature2_name",
            "item2_id",
            "item_name",
        ],
        (
            {
                "public_event_depot_id": depot_id,
                "creature2_id": row.get("creature2Id", ""),
                "creature2_name": lookup_name(client["creatures"], row.get("creature2Id"), "clientName"),
                "item2_id": row.get("item2Id", ""),
                "item_name": item_name(client["items"], row.get("item2Id")),
            }
            for depot_id, row in sorted(client["public_event_depots"].items())
        ),
    )

    virtual_item_fields = [
        "virtualItemId00",
        "virtualItemId01",
        "virtualItemId02",
        "virtualItemId03",
        "virtualItemId04",
        "virtualItemId05",
    ]

    def public_event_virtual_item_depot_rows():
        for depot_id, row in sorted(client["public_event_virtual_item_depots"].items()):
            for slot, field in enumerate(virtual_item_fields):
                virtual_item_id = to_int(row.get(field), 0) or 0
                if virtual_item_id <= 0:
                    continue
                virtual_item = client["virtual_items"].get(virtual_item_id, {})
                yield {
                    "public_event_virtual_item_depot_id": depot_id,
                    "creature2_id": row.get("creature2Id", ""),
                    "creature2_name": lookup_name(client["creatures"], row.get("creature2Id"), "clientName"),
                    "virtual_item_slot": slot,
                    "virtual_item_id": virtual_item_id,
                    "virtual_item_name": virtual_item.get("virtualItemName", ""),
                    "button_icon": virtual_item.get("buttonIcon", ""),
                    "item2_type_id": virtual_item.get("item2TypeId", ""),
                    "item_quality_id": virtual_item.get("itemQualityId", ""),
                }

    counts["public_event_virtual_item_depot_client_map.csv"] = write_csv(
        args.output_dir / "public_event_virtual_item_depot_client_map.csv",
        [
            "public_event_virtual_item_depot_id",
            "creature2_id",
            "creature2_name",
            "virtual_item_slot",
            "virtual_item_id",
            "virtual_item_name",
            "button_icon",
            "item2_type_id",
            "item_quality_id",
        ],
        public_event_virtual_item_depot_rows(),
    )

    counts["challenge_client_map.csv"] = write_csv(
        args.output_dir / "challenge_client_map.csv",
        [
            "challenge_id",
            "challenge_name",
            "challenge_type",
            "target",
            "flags",
            "world_zone_id",
            "world_zone_restriction_id",
            "world_location_indicator_id",
            "world_location_start_id",
            "completion_count",
            "tier_0_id",
            "tier_0_count",
            "tier_1_id",
            "tier_1_count",
            "tier_2_id",
            "tier_2_count",
            "reward_track_id",
            "virtual_item_display_id",
        ],
        (
            {
                "challenge_id": challenge_id,
                "challenge_name": row.get("challengeName", ""),
                "challenge_type": row.get("challengeTypeEnum", ""),
                "target": row.get("target", ""),
                "flags": row.get("challengeFlags", ""),
                "world_zone_id": row.get("worldZoneId", ""),
                "world_zone_restriction_id": row.get("worldZoneIdRestriction", ""),
                "world_location_indicator_id": row.get("worldLocation2IdIndicator", ""),
                "world_location_start_id": row.get("worldLocation2IdStartLocation", ""),
                "completion_count": row.get("completionCount", ""),
                "tier_0_id": row.get("challengeTierId00", ""),
                "tier_0_count": client["challenge_tiers"].get(to_int(row.get("challengeTierId00"), -1) or -1, {}).get("count", ""),
                "tier_1_id": row.get("challengeTierId01", ""),
                "tier_1_count": client["challenge_tiers"].get(to_int(row.get("challengeTierId01"), -1) or -1, {}).get("count", ""),
                "tier_2_id": row.get("challengeTierId02", ""),
                "tier_2_count": client["challenge_tiers"].get(to_int(row.get("challengeTierId02"), -1) or -1, {}).get("count", ""),
                "reward_track_id": row.get("rewardTrackId", ""),
                "virtual_item_display_id": row.get("virtualItemIdDisplay", ""),
            }
            for challenge_id, row in sorted(client["challenges"].items())
        ),
    )

    counts["world_client_map.csv"] = write_csv(
        args.output_dir / "world_client_map.csv",
        ["world_id", "world_name", "asset_path", "type", "flags", "min_item_level", "max_item_level"],
        (
            {
                "world_id": world_id,
                "world_name": row.get("worldName", ""),
                "asset_path": row.get("assetPath", ""),
                "type": row.get("type", ""),
                "flags": row.get("flags", ""),
                "min_item_level": row.get("minItemLevel", ""),
                "max_item_level": row.get("maxItemLevel", ""),
            }
            for world_id, row in sorted(client["worlds"].items())
        ),
    )

    counts["world_zone_client_map.csv"] = write_csv(
        args.output_dir / "world_zone_client_map.csv",
        ["world_zone_id", "world_zone_name", "parent_zone_id", "parent_zone_name", "allow_access", "flags", "pvp_rules"],
        (
            {
                "world_zone_id": zone_id,
                "world_zone_name": row.get("worldZoneName", ""),
                "parent_zone_id": row.get("parentZoneId", ""),
                "parent_zone_name": lookup_name(client["world_zones"], row.get("parentZoneId"), "worldZoneName"),
                "allow_access": row.get("allowAccess", ""),
                "flags": row.get("flags", ""),
                "pvp_rules": row.get("zonePvpRulesEnum", ""),
            }
            for zone_id, row in sorted(client["world_zones"].items())
        ),
    )

    counts["unit_property_client_map.csv"] = write_csv(
        args.output_dir / "unit_property_client_map.csv",
        [
            "unit_property2_id",
            "unit_property_name",
            "enum_name",
            "description",
            "value_per_point",
            "flags",
            "tooltip_display_order",
            "equipped_slot_flags",
        ],
        (
            {
                "unit_property2_id": property_id,
                "unit_property_name": row.get("unitPropertyName", ""),
                "enum_name": row.get("enumName", ""),
                "description": row.get("description", ""),
                "value_per_point": row.get("valuePerPoint", ""),
                "flags": row.get("flags", ""),
                "tooltip_display_order": row.get("tooltipDisplayOrder", ""),
                "equipped_slot_flags": row.get("equippedSlotFlags", ""),
            }
            for property_id, row in sorted(client["unit_properties"].items())
        ),
    )

    counts["faction_client_map.csv"] = write_csv(
        args.output_dir / "faction_client_map.csv",
        [
            "faction2_id",
            "faction_name",
            "parent_faction2_id",
            "parent_faction_name",
            "flags",
            "tooltip",
            "order_index",
            "archive_article_id",
        ],
        (
            {
                "faction2_id": faction_id,
                "faction_name": row.get("factionName", ""),
                "parent_faction2_id": row.get("faction2IdParent", ""),
                "parent_faction_name": lookup_name(client["factions"], row.get("faction2IdParent"), "factionName"),
                "flags": row.get("flags", ""),
                "tooltip": row.get("factionTooltip", ""),
                "order_index": row.get("orderIndex", ""),
                "archive_article_id": row.get("archiveArticleId", ""),
            }
            for faction_id, row in sorted(client["factions"].items())
        ),
    )

    counts["faction_relationship_client_map.csv"] = write_csv(
        args.output_dir / "faction_relationship_client_map.csv",
        ["faction_relationship_id", "faction2_id_0", "faction_name_0", "faction2_id_1", "faction_name_1", "faction_level"],
        (
            {
                "faction_relationship_id": row_id,
                "faction2_id_0": row.get("factionId0", ""),
                "faction_name_0": lookup_name(client["factions"], row.get("factionId0"), "factionName"),
                "faction2_id_1": row.get("factionId1", ""),
                "faction_name_1": lookup_name(client["factions"], row.get("factionId1"), "factionName"),
                "faction_level": row.get("factionLevel", ""),
            }
            for row_id, row in sorted(client["faction_relationships"].items())
        ),
    )

    counts["quest_category_client_map.csv"] = write_csv(
        args.output_dir / "quest_category_client_map.csv",
        ["quest_category_id", "quest_category_name", "description", "category_type"],
        (
            {
                "quest_category_id": category_id,
                "quest_category_name": row.get("questCategoryName", ""),
                "description": row.get("description", ""),
                "category_type": row.get("questCategoryTypeEnum", ""),
            }
            for category_id, row in sorted(client["quest_categories"].items())
        ),
    )

    counts["episode_client_map.csv"] = write_csv(
        args.output_dir / "episode_client_map.csv",
        [
            "episode_id",
            "episode_name",
            "world_zone_id",
            "world_zone_name",
            "quest_hub_exile_id",
            "quest_hub_dominion_id",
            "percent_to_display",
            "flags",
            "briefing",
            "end_summary",
        ],
        (
            {
                "episode_id": episode_id,
                "episode_name": row.get("episodeName", ""),
                "world_zone_id": row.get("worldZoneId", ""),
                "world_zone_name": lookup_name(client["world_zones"], row.get("worldZoneId"), "worldZoneName"),
                "quest_hub_exile_id": row.get("questHubIdExile", ""),
                "quest_hub_dominion_id": row.get("questHubIdDominion", ""),
                "percent_to_display": row.get("percentToDisplay", ""),
                "flags": row.get("flags", ""),
                "briefing": row.get("episodeBriefing", ""),
                "end_summary": row.get("episodeEndSummary", ""),
            }
            for episode_id, row in sorted(client["episodes"].items())
        ),
    )

    counts["episode_quest_client_map.csv"] = write_csv(
        args.output_dir / "episode_quest_client_map.csv",
        ["episode_quest_id", "episode_id", "episode_name", "quest2_id", "quest_name", "order_index", "flags"],
        (
            {
                "episode_quest_id": row_id,
                "episode_id": row.get("episodeId", ""),
                "episode_name": lookup_name(client["episodes"], row.get("episodeId"), "episodeName"),
                "quest2_id": row.get("questId", ""),
                "quest_name": lookup_name(client["quests"], row.get("questId"), "questName"),
                "order_index": row.get("orderIdx", ""),
                "flags": row.get("flags", ""),
            }
            for row_id, row in sorted(client["episode_quests"].items())
        ),
    )

    counts["map_continent_client_map.csv"] = write_csv(
        args.output_dir / "map_continent_client_map.csv",
        [
            "map_continent_id",
            "map_continent_name",
            "asset_path",
            "image_path",
            "image_width",
            "image_height",
            "image_offset_x",
            "image_offset_y",
            "hex_min_x",
            "hex_min_y",
            "hex_lim_x",
            "hex_lim_y",
            "flags",
        ],
        (
            {
                "map_continent_id": continent_id,
                "map_continent_name": row.get("mapContinentName", ""),
                "asset_path": row.get("assetPath", ""),
                "image_path": row.get("imagePath", ""),
                "image_width": row.get("imageWidth", ""),
                "image_height": row.get("imageHeight", ""),
                "image_offset_x": row.get("imageOffsetX", ""),
                "image_offset_y": row.get("imageOffsetY", ""),
                "hex_min_x": row.get("hexMinX", ""),
                "hex_min_y": row.get("hexMinY", ""),
                "hex_lim_x": row.get("hexLimX", ""),
                "hex_lim_y": row.get("hexLimY", ""),
                "flags": row.get("flags", ""),
            }
            for continent_id, row in sorted(client["map_continents"].items())
        ),
    )

    counts["map_zone_client_map.csv"] = write_csv(
        args.output_dir / "map_zone_client_map.csv",
        [
            "map_zone_id",
            "map_zone_name",
            "map_continent_id",
            "map_continent_name",
            "folder",
            "world_zone_id",
            "world_zone_name",
            "parent_map_zone_id",
            "version",
            "hex_min_x",
            "hex_min_y",
            "hex_lim_x",
            "hex_lim_y",
            "flags",
            "prerequisite_id_visibility",
            "reward_track_id",
        ],
        (
            {
                "map_zone_id": zone_id,
                "map_zone_name": row.get("mapZoneName", ""),
                "map_continent_id": row.get("mapContinentId", ""),
                "map_continent_name": lookup_name(client["map_continents"], row.get("mapContinentId"), "mapContinentName"),
                "folder": row.get("folder", ""),
                "world_zone_id": row.get("worldZoneId", ""),
                "world_zone_name": lookup_name(client["world_zones"], row.get("worldZoneId"), "worldZoneName"),
                "parent_map_zone_id": row.get("mapZoneIdParent", ""),
                "version": row.get("version", ""),
                "hex_min_x": row.get("hexMinX", ""),
                "hex_min_y": row.get("hexMinY", ""),
                "hex_lim_x": row.get("hexLimX", ""),
                "hex_lim_y": row.get("hexLimY", ""),
                "flags": row.get("flags", ""),
                "prerequisite_id_visibility": row.get("prerequisiteIdVisibility", ""),
                "reward_track_id": row.get("rewardTrackId", ""),
            }
            for zone_id, row in sorted(client["map_zones"].items())
        ),
    )

    counts["map_zone_sprite_client_map.csv"] = write_csv(
        args.output_dir / "map_zone_sprite_client_map.csv",
        ["map_zone_sprite_id", "sprite_name"],
        (
            {"map_zone_sprite_id": sprite_id, "sprite_name": row.get("spriteName", "")}
            for sprite_id, row in sorted(client["map_zone_sprites"].items())
        ),
    )

    counts["map_zone_poi_client_map.csv"] = write_csv(
        args.output_dir / "map_zone_poi_client_map.csv",
        [
            "map_zone_poi_id",
            "map_zone_id",
            "map_zone_name",
            "poi_name",
            "x",
            "y",
            "z",
            "raw_pos0",
            "raw_pos1",
            "raw_pos2",
            "map_zone_sprite_id",
            "sprite_name",
        ],
        (
            {
                "map_zone_poi_id": poi_id,
                "map_zone_id": row.get("mapZoneId", ""),
                "map_zone_name": lookup_name(client["map_zones"], row.get("mapZoneId"), "mapZoneName"),
                "poi_name": row.get("mapZonePoiName", ""),
                "x": signed32(row.get("pos0")),
                "y": signed32(row.get("pos1")),
                "z": signed32(row.get("pos2")),
                "raw_pos0": row.get("pos0", ""),
                "raw_pos1": row.get("pos1", ""),
                "raw_pos2": row.get("pos2", ""),
                "map_zone_sprite_id": row.get("mapZoneSpriteId", ""),
                "sprite_name": lookup_name(client["map_zone_sprites"], row.get("mapZoneSpriteId"), "spriteName"),
            }
            for poi_id, row in sorted(client["map_zone_pois"].items())
        ),
    )

    counts["map_zone_world_join_client_map.csv"] = write_csv(
        args.output_dir / "map_zone_world_join_client_map.csv",
        ["map_zone_world_join_id", "map_zone_id", "map_zone_name", "world_id", "world_name"],
        (
            {
                "map_zone_world_join_id": row_id,
                "map_zone_id": row.get("mapZoneId", ""),
                "map_zone_name": lookup_name(client["map_zones"], row.get("mapZoneId"), "mapZoneName"),
                "world_id": row.get("worldId", ""),
                "world_name": lookup_name(client["worlds"], row.get("worldId"), "worldName"),
            }
            for row_id, row in sorted(client["map_zone_world_joins"].items())
        ),
    )

    zone_columns = [
        "jabbithole_zone_id",
        "world_zone_id",
        "jabbithole_zone_name",
        "client_zone_name",
        "map_asset",
        "continent_id",
        "is_dungeon",
        "is_adventure",
        "is_pvp",
        "creatures_count",
        "path_missions_count",
        "public_events_count",
        "quest_zones_count",
        "enabled",
        "last_seen_in",
    ]
    zone_query = """
SELECT
    id,
    game_id,
    name,
    mapasset,
    continent_id,
    is_dungeon,
    is_adventure,
    is_pvp,
    creatures_count,
    real_path_missions_count,
    real_public_events_count,
    quest_zones_count,
    enabled,
    last_seen_in
FROM zones
ORDER BY id
""".strip()

    zone_query_columns = [
        "jabbithole_zone_id",
        "world_zone_id",
        "jabbithole_zone_name",
        "map_asset",
        "continent_id",
        "is_dungeon",
        "is_adventure",
        "is_pvp",
        "creatures_count",
        "path_missions_count",
        "public_events_count",
        "quest_zones_count",
        "enabled",
        "last_seen_in",
    ]

    def zone_rows():
        for row in mysql_rows(args, zone_query, zone_query_columns):
            world_zone = client["world_zones"].get(to_int(row.get("world_zone_id"), -1) or -1, {})
            row["client_zone_name"] = world_zone.get("worldZoneName", "")
            yield row

    counts["zone_map.csv"] = write_csv(args.output_dir / "zone_map.csv", zone_columns, zone_rows())
    return counts


def write_world_location_reference_maps(args: argparse.Namespace, client) -> Dict[str, int]:
    counts: Dict[str, int] = {}

    def location_summary(location_id) -> Dict[str, object]:
        location = client["world_locations"].get(to_int(location_id, -1) or -1, {})
        return {
            "world_location2_id": clean_cell(location_id) if location else clean_cell(location_id),
            "world_id": location.get("worldId", ""),
            "world_name": lookup_name(client["worlds"], location.get("worldId"), "worldName"),
            "world_zone_id": location.get("worldZoneId", ""),
            "world_zone_name": lookup_name(client["world_zones"], location.get("worldZoneId"), "worldZoneName"),
            "x": location.get("position0", ""),
            "y": location.get("position1", ""),
            "z": location.get("position2", ""),
            "radius": location.get("radius", ""),
            "max_vertical_distance": location.get("maxVerticalDistance", ""),
            "facing_0": location.get("facing0", ""),
            "facing_1": location.get("facing1", ""),
            "facing_2": location.get("facing2", ""),
            "facing_3": location.get("facing3", ""),
            "phases": location.get("phases", ""),
        }

    def id_list(row: Dict[str, object], fields: Sequence[str]) -> str:
        values = [to_int(row.get(field)) for field in fields]
        return ";".join(str(value) for value in values if value is not None and value > 0)

    def asset_list(row: Dict[str, object], fields: Sequence[str]) -> str:
        values = [clean_cell(row.get(field)) for field in fields]
        return ";".join(value for value in values if value)

    counts["world_location_client_map.csv"] = write_csv(
        args.output_dir / "world_location_client_map.csv",
        [
            "world_location2_id",
            "world_id",
            "world_name",
            "world_zone_id",
            "world_zone_name",
            "x",
            "y",
            "z",
            "radius",
            "max_vertical_distance",
            "facing_0",
            "facing_1",
            "facing_2",
            "facing_3",
            "phases",
        ],
        (
            {"world_location2_id": location_id, **location_summary(location_id)}
            for location_id in sorted(client["world_locations"])
        ),
    )

    counts["bind_point_client_map.csv"] = write_csv(
        args.output_dir / "bind_point_client_map.csv",
        ["bind_point_id", "bind_point_name", "bind_point_faction"],
        (
            {
                "bind_point_id": bind_id,
                "bind_point_name": row.get("bindPointName", ""),
                "bind_point_faction": row.get("bindPointFactionEnum", ""),
            }
            for bind_id, row in sorted(client["bind_points"].items())
        ),
    )

    counts["taxi_node_client_map.csv"] = write_csv(
        args.output_dir / "taxi_node_client_map.csv",
        [
            "taxi_node_id",
            "taxi_node_name",
            "taxi_node_type",
            "flight_path_type",
            "taxi_node_faction",
            "flags",
            "content_tier",
            "auto_unlock_level",
            "recommended_min_level",
            "recommended_max_level",
            "world_location2_id",
            "world_id",
            "world_name",
            "world_zone_id",
            "world_zone_name",
            "x",
            "y",
            "z",
        ],
        (
            {
                "taxi_node_id": node_id,
                "taxi_node_name": row.get("taxiNodeName", ""),
                "taxi_node_type": row.get("taxiNodeTypeEnum", ""),
                "flight_path_type": row.get("flightPathTypeEnum", ""),
                "taxi_node_faction": row.get("taxiNodeFactionEnum", ""),
                "flags": row.get("flags", ""),
                "content_tier": row.get("contentTier", ""),
                "auto_unlock_level": row.get("autoUnlockLevel", ""),
                "recommended_min_level": row.get("recommendedMinLevel", ""),
                "recommended_max_level": row.get("recommendedMaxLevel", ""),
                **location_summary(row.get("worldLocation2Id")),
            }
            for node_id, row in sorted(client["taxi_nodes"].items())
        ),
    )

    counts["taxi_route_client_map.csv"] = write_csv(
        args.output_dir / "taxi_route_client_map.csv",
        [
            "taxi_route_id",
            "source_taxi_node_id",
            "source_taxi_node_name",
            "destination_taxi_node_id",
            "destination_taxi_node_name",
            "price",
        ],
        (
            {
                "taxi_route_id": route_id,
                "source_taxi_node_id": row.get("taxiNodeIdSource", ""),
                "source_taxi_node_name": lookup_name(client["taxi_nodes"], row.get("taxiNodeIdSource"), "taxiNodeName"),
                "destination_taxi_node_id": row.get("taxiNodeIdDestination", ""),
                "destination_taxi_node_name": lookup_name(client["taxi_nodes"], row.get("taxiNodeIdDestination"), "taxiNodeName"),
                "price": row.get("price", ""),
            }
            for route_id, row in sorted(client["taxi_routes"].items())
        ),
    )

    city_location_fields = ["worldLocation2Id00", "worldLocation2Id01", "worldLocation2Id02", "worldLocation2Id03"]
    counts["city_direction_client_map.csv"] = write_csv(
        args.output_dir / "city_direction_client_map.csv",
        ["city_direction_id", "city_direction_name", "city_direction_type", "world_zone_id", "world_zone_name", "world_location2_ids"],
        (
            {
                "city_direction_id": direction_id,
                "city_direction_name": row.get("cityDirectionName", ""),
                "city_direction_type": row.get("cityDirectionTypeEnum", ""),
                "world_zone_id": row.get("worldZoneId", ""),
                "world_zone_name": lookup_name(client["world_zones"], row.get("worldZoneId"), "worldZoneName"),
                "world_location2_ids": id_list(row, city_location_fields),
            }
            for direction_id, row in sorted(client["city_directions"].items())
        ),
    )

    counts["quest_direction_entry_client_map.csv"] = write_csv(
        args.output_dir / "quest_direction_entry_client_map.csv",
        [
            "quest_direction_entry_id",
            "world_zone_id",
            "world_zone_name",
            "entry_flags",
            "faction_enum",
            "active_world_location2_id",
            "inactive_world_location2_id",
            "world_id",
            "world_name",
            "x",
            "y",
            "z",
        ],
        (
            {
                "quest_direction_entry_id": entry_id,
                "world_zone_id": row.get("worldZoneId", ""),
                "world_zone_name": lookup_name(client["world_zones"], row.get("worldZoneId"), "worldZoneName"),
                "entry_flags": row.get("questDirectionEntryFlags", ""),
                "faction_enum": row.get("questDirectionFactionEnum", ""),
                "active_world_location2_id": row.get("worldLocation2Id", ""),
                "inactive_world_location2_id": row.get("worldLocation2IdInactive", ""),
                **location_summary(row.get("worldLocation2Id")),
            }
            for entry_id, row in sorted(client["quest_direction_entries"].items())
        ),
    )

    direction_entry_fields = [f"questDirectionEntryId{index:02d}" for index in range(16)]
    counts["quest_direction_client_map.csv"] = write_csv(
        args.output_dir / "quest_direction_client_map.csv",
        ["quest_direction_id", "quest_direction_flags", "entry_ids", "entry_count", "excluded_world_zone_id", "excluded_world_zone_name"],
        (
            {
                "quest_direction_id": direction_id,
                "quest_direction_flags": row.get("questDirectionFlags", ""),
                "entry_ids": id_list(row, direction_entry_fields),
                "entry_count": len([field for field in direction_entry_fields if to_int(row.get(field), 0)]),
                "excluded_world_zone_id": row.get("worldZoneIdExcludedZone", ""),
                "excluded_world_zone_name": lookup_name(client["world_zones"], row.get("worldZoneIdExcludedZone"), "worldZoneName"),
            }
            for direction_id, row in sorted(client["quest_directions"].items())
        ),
    )

    counts["quest_hub_client_map.csv"] = write_csv(
        args.output_dir / "quest_hub_client_map.csv",
        ["quest_hub_id", "quest_hub_name", "world_location2_id", "world_id", "world_name", "world_zone_id", "world_zone_name", "x", "y", "z"],
        (
            {
                "quest_hub_id": hub_id,
                "quest_hub_name": row.get("questHubName", ""),
                **location_summary(row.get("worldLocation2Id")),
            }
            for hub_id, row in sorted(client["quest_hubs"].items())
        ),
    )

    counts["generic_map_client_map.csv"] = write_csv(
        args.output_dir / "generic_map_client_map.csv",
        ["generic_map_id", "map_zone_id", "map_zone_name"],
        (
            {
                "generic_map_id": map_id,
                "map_zone_id": row.get("mapZoneId", ""),
                "map_zone_name": lookup_name(client["map_zones"], row.get("mapZoneId"), "mapZoneName"),
            }
            for map_id, row in sorted(client["generic_maps"].items())
        ),
    )

    counts["generic_map_node_client_map.csv"] = write_csv(
        args.output_dir / "generic_map_node_client_map.csv",
        [
            "generic_map_node_id",
            "generic_map_id",
            "map_zone_id",
            "map_zone_name",
            "node_name",
            "description",
            "sprite_path",
            "node_type",
            "flags",
            "world_location2_id",
            "world_id",
            "world_name",
            "world_zone_id",
            "world_zone_name",
            "x",
            "y",
            "z",
        ],
        (
            {
                "generic_map_node_id": node_id,
                "generic_map_id": row.get("genericMapId", ""),
                "map_zone_id": client["generic_maps"].get(to_int(row.get("genericMapId"), -1) or -1, {}).get("mapZoneId", ""),
                "map_zone_name": lookup_name(
                    client["map_zones"],
                    client["generic_maps"].get(to_int(row.get("genericMapId"), -1) or -1, {}).get("mapZoneId", ""),
                    "mapZoneName",
                ),
                "node_name": row.get("genericMapNodeName", ""),
                "description": row.get("genericMapNodeDescription", ""),
                "sprite_path": row.get("spritePath", ""),
                "node_type": row.get("genericMapNodeTypeEnum", ""),
                "flags": row.get("flags", ""),
                **location_summary(row.get("worldLocation2Id")),
            }
            for node_id, row in sorted(client["generic_map_nodes"].items())
        ),
    )

    counts["map_zone_hex_client_map.csv"] = write_csv(
        args.output_dir / "map_zone_hex_client_map.csv",
        ["map_zone_hex_id", "map_zone_id", "map_zone_name", "hex_x", "hex_y", "flags"],
        (
            {
                "map_zone_hex_id": hex_id,
                "map_zone_id": row.get("mapZoneId", ""),
                "map_zone_name": lookup_name(client["map_zones"], row.get("mapZoneId"), "mapZoneName"),
                "hex_x": row.get("pos0", ""),
                "hex_y": row.get("pos1", ""),
                "flags": row.get("flags", ""),
            }
            for hex_id, row in sorted(client["map_zone_hexes"].items())
        ),
    )

    counts["map_zone_hex_group_client_map.csv"] = write_csv(
        args.output_dir / "map_zone_hex_group_client_map.csv",
        ["map_zone_hex_group_id", "map_zone_id", "map_zone_name"],
        (
            {
                "map_zone_hex_group_id": group_id,
                "map_zone_id": row.get("mapZoneId", ""),
                "map_zone_name": lookup_name(client["map_zones"], row.get("mapZoneId"), "mapZoneName"),
            }
            for group_id, row in sorted(client["map_zone_hex_groups"].items())
        ),
    )

    counts["map_zone_hex_group_entry_client_map.csv"] = write_csv(
        args.output_dir / "map_zone_hex_group_entry_client_map.csv",
        ["map_zone_hex_group_entry_id", "map_zone_hex_group_id", "map_zone_id", "map_zone_name", "hex_x", "hex_y"],
        (
            {
                "map_zone_hex_group_entry_id": entry_id,
                "map_zone_hex_group_id": row.get("mapZoneHexGroupId", ""),
                "map_zone_id": client["map_zone_hex_groups"].get(to_int(row.get("mapZoneHexGroupId"), -1) or -1, {}).get("mapZoneId", ""),
                "map_zone_name": lookup_name(
                    client["map_zones"],
                    client["map_zone_hex_groups"].get(to_int(row.get("mapZoneHexGroupId"), -1) or -1, {}).get("mapZoneId", ""),
                    "mapZoneName",
                ),
                "hex_x": row.get("hexX", ""),
                "hex_y": row.get("hexY", ""),
            }
            for entry_id, row in sorted(client["map_zone_hex_group_entries"].items())
        ),
    )

    counts["map_zone_level_band_client_map.csv"] = write_csv(
        args.output_dir / "map_zone_level_band_client_map.csv",
        ["map_zone_level_band_id", "map_zone_hex_group_id", "level_min", "level_max", "label_x", "label_z"],
        (
            {
                "map_zone_level_band_id": band_id,
                "map_zone_hex_group_id": row.get("mapZoneHexGroupId", ""),
                "level_min": row.get("levelMin", ""),
                "level_max": row.get("levelMax", ""),
                "label_x": row.get("labelX", ""),
                "label_z": row.get("labelZ", ""),
            }
            for band_id, row in sorted(client["map_zone_level_bands"].items())
        ),
    )

    counts["map_zone_nemesis_region_client_map.csv"] = write_csv(
        args.output_dir / "map_zone_nemesis_region_client_map.csv",
        [
            "map_zone_nemesis_region_id",
            "map_zone_hex_group_id",
            "map_zone_id",
            "map_zone_name",
            "description",
            "faction2_id",
            "faction_name",
        ],
        (
            {
                "map_zone_nemesis_region_id": region_id,
                "map_zone_hex_group_id": row.get("mapZoneHexGroupId", ""),
                "map_zone_id": client["map_zone_hex_groups"].get(to_int(row.get("mapZoneHexGroupId"), -1) or -1, {}).get("mapZoneId", ""),
                "map_zone_name": lookup_name(
                    client["map_zones"],
                    client["map_zone_hex_groups"].get(to_int(row.get("mapZoneHexGroupId"), -1) or -1, {}).get("mapZoneId", ""),
                    "mapZoneName",
                ),
                "description": row.get("nemesisRegionDescription", ""),
                "faction2_id": row.get("faction2Id", ""),
                "faction_name": lookup_name(client["factions"], row.get("faction2Id"), "factionName"),
            }
            for region_id, row in sorted(client["map_zone_nemesis_regions"].items())
        ),
    )

    counts["zone_completion_client_map.csv"] = write_csv(
        args.output_dir / "zone_completion_client_map.csv",
        [
            "zone_completion_id",
            "map_zone_id",
            "map_zone_name",
            "faction_enum",
            "episode_quest_count",
            "task_quest_count",
            "challenge_count",
            "datacube_count",
            "tale_count",
            "journal_count",
            "character_title_reward_id",
            "character_title_reward_name",
        ],
        (
            {
                "zone_completion_id": completion_id,
                "map_zone_id": row.get("mapZoneId", ""),
                "map_zone_name": lookup_name(client["map_zones"], row.get("mapZoneId"), "mapZoneName"),
                "faction_enum": row.get("zoneCompletionFactionEnum", ""),
                "episode_quest_count": row.get("episodeQuestCount", ""),
                "task_quest_count": row.get("taskQuestCount", ""),
                "challenge_count": row.get("challengeCount", ""),
                "datacube_count": row.get("datacubeCount", ""),
                "tale_count": row.get("taleCount", ""),
                "journal_count": row.get("journalCount", ""),
                "character_title_reward_id": row.get("characterTitleIdReward", ""),
                "character_title_reward_name": lookup_name(client["character_titles"], row.get("characterTitleIdReward"), "characterTitleName"),
            }
            for completion_id, row in sorted(client["zone_completions"].items())
        ),
    )

    counts["sound_zone_kit_client_map.csv"] = write_csv(
        args.output_dir / "sound_zone_kit_client_map.csv",
        [
            "sound_zone_kit_id",
            "parent_sound_zone_kit_id",
            "world_zone_id",
            "world_zone_name",
            "inherit_flags",
            "property_flags",
            "sound_music_set_id",
            "sound_event_intro",
            "sound_event_music_mood",
            "ambient_day",
            "ambient_night",
            "ambient_underwater",
            "ambient_stop",
        ],
        (
            {
                "sound_zone_kit_id": kit_id,
                "parent_sound_zone_kit_id": row.get("soundZoneKitIdParent", ""),
                "world_zone_id": row.get("worldZoneId", ""),
                "world_zone_name": lookup_name(client["world_zones"], row.get("worldZoneId"), "worldZoneName"),
                "inherit_flags": row.get("inheritFlags", ""),
                "property_flags": row.get("propertyFlags", ""),
                "sound_music_set_id": row.get("soundMusicSetId", ""),
                "sound_event_intro": row.get("soundEventIdIntro", ""),
                "sound_event_music_mood": row.get("soundEventIdMusicMood", ""),
                "ambient_day": row.get("soundEventIdAmbientDay", ""),
                "ambient_night": row.get("soundEventIdAmbientNight", ""),
                "ambient_underwater": row.get("soundEventIdAmbientUnderwater", ""),
                "ambient_stop": row.get("soundEventIdAmbientStop", ""),
            }
            for kit_id, row in sorted(client["sound_zone_kits"].items())
        ),
    )

    counts["world_socket_client_map.csv"] = write_csv(
        args.output_dir / "world_socket_client_map.csv",
        ["world_socket_id", "world_id", "world_name", "bounds_0", "bounds_1", "bounds_2", "bounds_3", "average_height"],
        (
            {
                "world_socket_id": socket_id,
                "world_id": row.get("worldId", ""),
                "world_name": lookup_name(client["worlds"], row.get("worldId"), "worldName"),
                "bounds_0": row.get("bounds0", ""),
                "bounds_1": row.get("bounds1", ""),
                "bounds_2": row.get("bounds2", ""),
                "bounds_3": row.get("bounds3", ""),
                "average_height": row.get("averageHeight", ""),
            }
            for socket_id, row in sorted(client["world_sockets"].items())
        ),
    )

    clutter_fields = ["worldClutterId00", "worldClutterId01", "worldClutterId02", "worldClutterId03"]
    counts["world_layer_client_map.csv"] = write_csv(
        args.output_dir / "world_layer_client_map.csv",
        [
            "world_layer_id",
            "description",
            "material_type",
            "color_map_path",
            "normal_map_path",
            "clutter_ids",
            "height_scale",
            "height_offset",
            "meters_per_texture_tile",
            "average_color",
            "projection",
        ],
        (
            {
                "world_layer_id": layer_id,
                "description": row.get("Description", ""),
                "material_type": row.get("materialType", ""),
                "color_map_path": row.get("ColorMapPath", ""),
                "normal_map_path": row.get("NormalMapPath", ""),
                "clutter_ids": id_list(row, clutter_fields),
                "height_scale": row.get("HeightScale", ""),
                "height_offset": row.get("HeightOffset", ""),
                "meters_per_texture_tile": row.get("MetersPerTextureTile", ""),
                "average_color": row.get("AverageColor", ""),
                "projection": row.get("Projection", ""),
            }
            for layer_id, row in sorted(client["world_layers"].items())
        ),
    )

    clutter_asset_fields = ["assetPath0", "assetPath01", "assetPath02", "assetPath03", "assetPath04", "assetPath05"]
    clutter_weight_fields = ["assetWeight0", "assetWeight01", "assetWeight02", "assetWeight03", "assetWeight04", "assetWeight05"]
    counts["world_clutter_client_map.csv"] = write_csv(
        args.output_dir / "world_clutter_client_map.csv",
        ["world_clutter_id", "description", "density", "clutter_flags", "asset_paths", "asset_weights"],
        (
            {
                "world_clutter_id": clutter_id,
                "description": row.get("Description", ""),
                "density": row.get("density", ""),
                "clutter_flags": row.get("clutterFlags", ""),
                "asset_paths": asset_list(row, clutter_asset_fields),
                "asset_weights": asset_list(row, clutter_weight_fields),
            }
            for clutter_id, row in sorted(client["world_clutters"].items())
        ),
    )

    counts["world_sky_client_map.csv"] = write_csv(
        args.output_dir / "world_sky_client_map.csv",
        ["world_sky_id", "asset_path", "asset_path_in_flux", "color"],
        (
            {
                "world_sky_id": sky_id,
                "asset_path": row.get("assetPath", ""),
                "asset_path_in_flux": row.get("assetPathInFlux", ""),
                "color": row.get("color", ""),
            }
            for sky_id, row in sorted(client["world_skies"].items())
        ),
    )

    counts["world_water_environment_client_map.csv"] = write_csv(
        args.output_dir / "world_water_environment_client_map.csv",
        ["world_water_environment_id", "land_map_path"],
        (
            {"world_water_environment_id": environment_id, "land_map_path": row.get("LandMapPath", "")}
            for environment_id, row in sorted(client["world_water_environments"].items())
        ),
    )

    counts["world_water_fog_client_map.csv"] = write_csv(
        args.output_dir / "world_water_fog_client_map.csv",
        ["world_water_fog_id", "fog_start", "fog_end", "fog_start_underwater", "fog_end_underwater", "sky_color_index"],
        (
            {
                "world_water_fog_id": fog_id,
                "fog_start": row.get("fogStart", ""),
                "fog_end": row.get("fogEnd", ""),
                "fog_start_underwater": row.get("fogStartUW", ""),
                "fog_end_underwater": row.get("fogEndUW", ""),
                "sky_color_index": row.get("skyColorIndex", ""),
            }
            for fog_id, row in sorted(client["world_water_fogs"].items())
        ),
    )

    counts["world_water_layer_client_map.csv"] = write_csv(
        args.output_dir / "world_water_layer_client_map.csv",
        [
            "world_water_layer_id",
            "description",
            "ripple_color_texture",
            "ripple_normal_texture",
            "scale",
            "rotation",
            "speed",
            "osc_frequency",
            "osc_magnitude",
            "material_blend",
        ],
        (
            {
                "world_water_layer_id": layer_id,
                "description": row.get("description", ""),
                "ripple_color_texture": row.get("RippleColorTex", ""),
                "ripple_normal_texture": row.get("RippleNormalTex", ""),
                "scale": row.get("Scale", ""),
                "rotation": row.get("Rotation", ""),
                "speed": row.get("Speed", ""),
                "osc_frequency": row.get("OscFrequency", ""),
                "osc_magnitude": row.get("OscMagnitude", ""),
                "material_blend": row.get("materialBlend", ""),
            }
            for layer_id, row in sorted(client["world_water_layers"].items())
        ),
    )

    counts["world_water_type_client_map.csv"] = write_csv(
        args.output_dir / "world_water_type_client_map.csv",
        ["world_water_type_id", "world_water_fog_id", "surface_type", "particle_file", "sound_directional_ambience_id"],
        (
            {
                "world_water_type_id": water_type_id,
                "world_water_fog_id": row.get("worldWaterFogId", ""),
                "surface_type": row.get("SurfaceType", ""),
                "particle_file": row.get("particleFile", ""),
                "sound_directional_ambience_id": row.get("soundDirectionalAmbienceId", ""),
            }
            for water_type_id, row in sorted(client["world_water_types"].items())
        ),
    )

    counts["world_water_wake_client_map.csv"] = write_csv(
        args.output_dir / "world_water_wake_client_map.csv",
        [
            "world_water_wake_id",
            "flags",
            "color_texture",
            "normal_texture",
            "distortion_texture",
            "duration_min",
            "duration_max",
            "scale_start",
            "scale_end",
            "alpha_start",
            "alpha_end",
        ],
        (
            {
                "world_water_wake_id": wake_id,
                "flags": row.get("flags", ""),
                "color_texture": row.get("colorTexture", ""),
                "normal_texture": row.get("normalTexture", ""),
                "distortion_texture": row.get("distortionTexture", ""),
                "duration_min": row.get("durationMin", ""),
                "duration_max": row.get("durationMax", ""),
                "scale_start": row.get("scaleStart", ""),
                "scale_end": row.get("scaleEnd", ""),
                "alpha_start": row.get("alphaStart", ""),
                "alpha_end": row.get("alphaEnd", ""),
            }
            for wake_id, row in sorted(client["world_water_wakes"].items())
        ),
    )

    return counts


def write_creature_metadata(args: argparse.Namespace, creature_rows: Sequence[Dict[str, object]], client_creatures) -> int:
    fieldnames = [
        "jabbithole_creature_id",
        "creature2_id",
        "source_name",
        "client_name",
        "match_status",
        "creature2_action_set_id",
        "creature2_action_text_id",
        "creature2_display_group_id",
        "default_display_info",
        "creature2_outfit_group_id",
        "default_outfit_info",
        "creature2_model_info_id",
        "creature2_archetype_id",
        "creature2_tier_id",
        "client_faction",
        "client_min_level",
        "client_max_level",
        "client_datacube_id",
        "path_mission_id_soldier",
        "bind_point_id",
        "taxi_node_id",
        "instance_portal_id",
        "unit_vehicle_id",
        "tradeskill_harvesting_info_id",
    ]

    def rows():
        for row in creature_rows:
            client_id = to_int(row.get("creature2_id"))
            client = client_creatures.get(client_id, {}) if client_id is not None else {}
            yield {
                **row,
                "creature2_action_text_id": client.get("creature2ActionTextId", ""),
                "creature2_model_info_id": client.get("creature2ModelInfoId", ""),
                "creature2_archetype_id": client.get("creature2ArcheTypeId", ""),
                "creature2_tier_id": client.get("creature2TierId", ""),
                "path_mission_id_soldier": client.get("pathMissionIdSoldier", ""),
                "bind_point_id": client.get("bindPointId", ""),
                "taxi_node_id": client.get("taxiNodeId", ""),
                "instance_portal_id": client.get("instancePortalId", ""),
                "unit_vehicle_id": client.get("unitVehicleId", ""),
                "tradeskill_harvesting_info_id": client.get("tradeskillHarvestingInfoId", ""),
            }

    return write_csv(args.output_dir / "creature_client_metadata_map.csv", fieldnames, rows())


def write_spawn_maps(
    args: argparse.Namespace,
    creature_map: Dict[int, Dict[str, object]],
    client_creatures: Dict[int, Dict[str, object]],
) -> Dict[str, int]:
    columns = [
        "source_coordinate_id",
        "jabbithole_creature_id",
        "source_name",
        "zone_id",
        "worldid",
        "worldzoneid",
        "creature_type",
        "level_min",
        "level_max",
        "health_min",
        "health_max",
        "shield",
        "interrupt_armor_max",
        "faction",
        "x",
        "y",
        "z",
        "coordinate_last_seen_in",
    ]
    query = """
SELECT
    co.id,
    co.location_id,
    c.name,
    c.zone_id,
    c.worldid,
    c.worldzoneid,
    c.creature_type,
    c.level_min,
    c.level_max,
    c.health_min,
    c.health_max,
    c.shield,
    c.interrupt_armor_max,
    c.faction,
    co.x,
    co.y,
    co.z,
    co.last_seen_in
FROM coordinates co
JOIN creatures c ON c.id = co.location_id
WHERE co.location_type = 'Creature'
  AND c.worldid IS NOT NULL
  AND c.worldid <> 0
ORDER BY co.id
""".strip()

    spawn_fields = [
        "source_coordinate_id",
        "jabbithole_creature_id",
        "creature2_id",
        "source_name",
        "creature2_name",
        "match_status",
        "entity_type",
        "zone_id",
        "worldid",
        "worldzoneid",
        "x",
        "y",
        "z",
        "level_min",
        "level_max",
        "health_min",
        "health_max",
        "shield",
        "interrupt_armor_max",
        "faction",
        "coordinate_last_seen_in",
    ]
    entity_fields = [
        "source_coordinate_id",
        "jabbithole_creature_id",
        "source_name",
        "entity_id",
        "Type",
        "Creature",
        "World",
        "Area",
        "X",
        "Y",
        "Z",
        "RX",
        "RY",
        "RZ",
        "DisplayInfo",
        "OutfitInfo",
        "Faction1",
        "Faction2",
        "QuestChecklistIdx",
        "Mode",
        "ActivePropId",
        "WorldSocketId",
        "match_status",
    ]
    stat_fields = [
        "source_coordinate_id",
        "jabbithole_creature_id",
        "source_name",
        "entity_id",
        "Stat",
        "StatName",
        "Value",
    ]

    counts = Counter()
    args.output_dir.mkdir(parents=True, exist_ok=True)
    spawn_path = args.output_dir / "creature_spawn_map.csv"
    entity_path = args.output_dir / "world_entity_candidate.csv"
    stat_path = args.output_dir / "world_entity_stats_candidate.csv"
    temp_paths = [
        spawn_path.with_suffix(spawn_path.suffix + ".tmp"),
        entity_path.with_suffix(entity_path.suffix + ".tmp"),
        stat_path.with_suffix(stat_path.suffix + ".tmp"),
    ]
    try:
        with temp_paths[0].open("w", encoding="utf-8", newline="") as spawn_handle, temp_paths[1].open(
            "w", encoding="utf-8", newline=""
        ) as entity_handle, temp_paths[2].open("w", encoding="utf-8", newline="") as stat_handle:
            spawn_writer = csv.DictWriter(spawn_handle, fieldnames=spawn_fields)
            entity_writer = csv.DictWriter(entity_handle, fieldnames=entity_fields)
            stat_writer = csv.DictWriter(stat_handle, fieldnames=stat_fields)
            spawn_writer.writeheader()
            entity_writer.writeheader()
            stat_writer.writeheader()

            for row in mysql_rows(args, limited(query, args.limit_spawns), columns):
                row = apply_creature_map(row, creature_map)
                spawn_writer.writerow({field: clean_cell(row.get(field, "")) for field in spawn_fields})
                counts["creature_spawn_map.csv"] += 1

                client_id = to_int(row.get("creature2_id"))
                client = client_creatures.get(client_id, {}) if client_id is not None else {}
                faction = client.get("factionId", row.get("faction", ""))
                entity_row = {
                    "source_coordinate_id": row.get("source_coordinate_id", ""),
                    "jabbithole_creature_id": row.get("jabbithole_creature_id", ""),
                    "source_name": row.get("source_name", ""),
                    "entity_id": "",
                    "Type": row.get("entity_type", ""),
                    "Creature": row.get("creature2_id", ""),
                    "World": row.get("worldid", ""),
                    "Area": row.get("worldzoneid", ""),
                    "X": row.get("x", ""),
                    "Y": row.get("y", ""),
                    "Z": row.get("z", ""),
                    "RX": 0,
                    "RY": 0,
                    "RZ": 0,
                    "DisplayInfo": client.get("defaultDisplayInfo", ""),
                    "OutfitInfo": client.get("defaultOutfitInfo", ""),
                    "Faction1": faction,
                    "Faction2": faction,
                    "QuestChecklistIdx": 0,
                    "Mode": 0,
                    "ActivePropId": 0,
                    "WorldSocketId": 0,
                    "match_status": row.get("match_status", ""),
                }
                entity_writer.writerow({field: clean_cell(entity_row.get(field, "")) for field in entity_fields})
                counts["world_entity_candidate.csv"] += 1

                stats = [
                    ("Health", template_health_value(row.get("health_min"), row.get("health_max"))),
                    ("Level", row.get("level_max") or row.get("level_min")),
                    ("Shield", row.get("shield")),
                    ("InterruptArmour", row.get("interrupt_armor_max")),
                ]
                for stat_name, stat_value in stats:
                    numeric = to_float(stat_value)
                    if numeric is None:
                        continue
                    stat_writer.writerow(
                        {
                            "source_coordinate_id": row.get("source_coordinate_id", ""),
                            "jabbithole_creature_id": row.get("jabbithole_creature_id", ""),
                            "source_name": row.get("source_name", ""),
                            "entity_id": "",
                            "Stat": STAT_IDS[stat_name],
                            "StatName": stat_name,
                            "Value": numeric,
                        }
                    )
                    counts["world_entity_stats_candidate.csv"] += 1
        replace_file_with_retry(temp_paths[0], spawn_path)
        replace_file_with_retry(temp_paths[1], entity_path)
        replace_file_with_retry(temp_paths[2], stat_path)
    finally:
        for temp_path in temp_paths:
            if temp_path.exists():
                temp_path.unlink()

    return dict(counts)


def write_relation_maps(
    args: argparse.Namespace,
    creature_map: Dict[int, Dict[str, object]],
    client,
) -> Dict[str, int]:
    counts: Dict[str, int] = {}

    loot_columns = [
        "source_drop_id",
        "jabbithole_item_id",
        "jabbithole_creature_id",
        "versioned_item_drop_aggregate_id",
        "versioned_creature_drop_aggregate_id",
        "game_version",
        "drop_times",
        "enabled",
        "last_seen_in",
        "item2_id",
        "aggregate_drop_sum",
        "aggregate_drop_count",
    ]
    loot_query = """
SELECT
    d.id,
    d.item_id,
    d.creature_id,
    d.versioned_item_drop_aggregate_id,
    d.versioned_creature_drop_aggregate_id,
    d.game_version,
    d.drop_times,
    d.enabled,
    d.last_seen_in,
    d.game_id_item,
    COALESCE(a.versioned_item_drops_sum, 0),
    COALESCE(a.versioned_item_drops_count, 0)
FROM versioned_item_drops d
LEFT JOIN versioned_creature_drop_aggregates a
    ON a.id = d.versioned_creature_drop_aggregate_id
WHERE d.enabled = 'true'
ORDER BY d.creature_id, d.game_version, d.id
"""

    def transform_loot(row):
        row["item_name"] = item_name(client["items"], row.get("item2_id"))
        drops = to_float(row.get("drop_times"), 0) or 0
        total = to_float(row.get("aggregate_drop_sum"), 0) or 0
        row["drop_probability"] = f"{drops / total:.8f}" if total > 0 else ""
        return row

    name, count = query_relation(
        args,
        "creature_loot_map",
        loot_query,
        loot_columns,
        [
            "source_drop_id",
            "jabbithole_creature_id",
            "creature2_id",
            "source_name",
            "creature2_name",
            "match_status",
            "jabbithole_item_id",
            "item2_id",
            "item_name",
            "game_version",
            "drop_times",
            "aggregate_drop_sum",
            "aggregate_drop_count",
            "drop_probability",
            "versioned_item_drop_aggregate_id",
            "versioned_creature_drop_aggregate_id",
            "last_seen_in",
        ],
        creature_map,
        transform_loot,
        args.limit_relation_rows,
    )
    counts[name] = count

    vendor_columns = [
        "vendor_item_source_id",
        "jabbithole_creature_id",
        "jabbithole_item_id",
        "main_currency_id",
        "alt_currency_id",
        "game_version",
        "special_item",
        "stack_size",
        "stock_size",
        "main_price",
        "alt_price",
        "prerequisite",
        "enabled",
        "last_seen_in",
        "item2_id",
    ]
    vendor_query = """
SELECT
    id,
    creature_id,
    item_id,
    main_currency_id,
    alt_currency_id,
    game_version,
    special_item,
    stack_size,
    stock_size,
    main_price,
    alt_price,
    prerequisite,
    enabled,
    last_seen_in,
    game_id_item
FROM versioned_vendor_items
WHERE enabled = 'true'
ORDER BY creature_id, game_version, id
"""

    def transform_vendor(row):
        row["item_name"] = item_name(client["items"], row.get("item2_id"))
        return row

    name, count = query_relation(
        args,
        "vendor_item_map",
        vendor_query,
        vendor_columns,
        [
            "vendor_item_source_id",
            "jabbithole_creature_id",
            "creature2_id",
            "source_name",
            "creature2_name",
            "match_status",
            "jabbithole_item_id",
            "item2_id",
            "item_name",
            "game_version",
            "main_currency_id",
            "main_price",
            "alt_currency_id",
            "alt_price",
            "stack_size",
            "stock_size",
            "special_item",
            "prerequisite",
            "last_seen_in",
        ],
        creature_map,
        transform_vendor,
        args.limit_relation_rows,
    )
    counts[name] = count

    spell_columns = [
        "source_creature_spell_id",
        "jabbithole_creature_id",
        "jabbithole_spell_id",
        "spell4_id",
        "spell_name",
        "tier",
        "base_spell4_id",
        "last_seen_in",
    ]
    spell_query = """
SELECT
    cs.id,
    cs.creature_id,
    cs.spell_id,
    s.game_id,
    s.name,
    s.tier,
    s.base_spell_id_game,
    cs.last_seen_in
FROM creature_spells cs
LEFT JOIN spells s ON s.id = cs.spell_id
WHERE cs.enabled = 'true'
ORDER BY cs.creature_id, cs.id
"""
    name, count = query_relation(
        args,
        "creature_spell_map",
        spell_query,
        spell_columns,
        [
            "source_creature_spell_id",
            "jabbithole_creature_id",
            "creature2_id",
            "source_name",
            "creature2_name",
            "match_status",
            "jabbithole_spell_id",
            "spell4_id",
            "spell_name",
            "tier",
            "base_spell4_id",
            "last_seen_in",
        ],
        creature_map,
        lambda row: row,
        args.limit_relation_rows,
    )
    counts[name] = count

    quest_columns = [
        "relation_type",
        "source_relation_id",
        "jabbithole_creature_id",
        "jabbithole_quest_id",
        "quest2_id",
        "quest_name",
        "quest_level",
        "quest_min_level",
        "quest_type",
        "path_quest_type",
        "last_seen_in",
    ]
    quest_query = """
SELECT *
FROM (
SELECT 'starter' AS relation_type, qsc.id AS source_relation_id, qsc.creature_id AS jabbithole_creature_id, qsc.quest_id AS jabbithole_quest_id, q.game_id AS quest2_id, q.name AS quest_name, q.level AS quest_level, q.min_level AS quest_min_level, q.quest_type AS quest_type, q.path_quest_type AS path_quest_type, qsc.last_seen_in AS last_seen_in
FROM quest_starter_creatures qsc
LEFT JOIN quests q ON q.id = qsc.quest_id
WHERE qsc.enabled = 'true'
UNION ALL
SELECT 'finisher' AS relation_type, qfc.id AS source_relation_id, qfc.creature_id AS jabbithole_creature_id, qfc.quest_id AS jabbithole_quest_id, q.game_id AS quest2_id, q.name AS quest_name, q.level AS quest_level, q.min_level AS quest_min_level, q.quest_type AS quest_type, q.path_quest_type AS path_quest_type, qfc.last_seen_in AS last_seen_in
FROM quest_finisher_creatures qfc
LEFT JOIN quests q ON q.id = qfc.quest_id
WHERE qfc.enabled = 'true'
UNION ALL
SELECT 'objective' AS relation_type, qc.id AS source_relation_id, qc.creature_id AS jabbithole_creature_id, qc.quest_id AS jabbithole_quest_id, q.game_id AS quest2_id, q.name AS quest_name, q.level AS quest_level, q.min_level AS quest_min_level, q.quest_type AS quest_type, q.path_quest_type AS path_quest_type, qc.last_seen_in AS last_seen_in
FROM quest_creatures qc
LEFT JOIN quests q ON q.id = qc.quest_id
WHERE qc.enabled = 'true'
) q
ORDER BY jabbithole_creature_id, relation_type, source_relation_id
"""
    name, count = query_relation(
        args,
        "creature_quest_map",
        quest_query,
        quest_columns,
        [
            "relation_type",
            "source_relation_id",
            "jabbithole_creature_id",
            "creature2_id",
            "source_name",
            "creature2_name",
            "match_status",
            "jabbithole_quest_id",
            "quest2_id",
            "quest_name",
            "quest_level",
            "quest_min_level",
            "quest_type",
            "path_quest_type",
            "last_seen_in",
        ],
        creature_map,
        lambda row: row,
        args.limit_relation_rows,
    )
    counts[name] = count

    path_columns = [
        "source_relation_id",
        "jabbithole_creature_id",
        "jabbithole_path_mission_id",
        "path_mission_id",
        "path_mission_name",
        "player_path_id",
        "mission_type",
        "mission_subtype",
        "needed",
        "real_zone_id",
        "datamined_zone_id",
        "last_seen_in",
    ]
    path_query = """
SELECT
    pmc.id,
    pmc.creature_id,
    pmc.path_mission_id,
    pm.game_id,
    pm.name,
    pm.player_path_id,
    pm.mission_type,
    pm.mission_subtype,
    pm.needed,
    pm.real_zone_id,
    pm.datamined_zone_id,
    pmc.last_seen_in
FROM path_mission_creatures pmc
LEFT JOIN path_missions pm ON pm.id = pmc.path_mission_id
WHERE pmc.enabled = 'true'
ORDER BY pmc.creature_id, pmc.id
"""
    name, count = query_relation(
        args,
        "creature_path_mission_map",
        path_query,
        path_columns,
        [
            "source_relation_id",
            "jabbithole_creature_id",
            "creature2_id",
            "source_name",
            "creature2_name",
            "match_status",
            "jabbithole_path_mission_id",
            "path_mission_id",
            "path_mission_name",
            "player_path_id",
            "mission_type",
            "mission_subtype",
            "needed",
            "real_zone_id",
            "datamined_zone_id",
            "last_seen_in",
        ],
        creature_map,
        lambda row: row,
        args.limit_relation_rows,
    )
    counts[name] = count

    public_columns = [
        "source_relation_id",
        "jabbithole_creature_id",
        "jabbithole_public_event_id",
        "public_event_id",
        "public_event_name",
        "event_type",
        "reward_type",
        "real_zone_id",
        "parent_public_event_id",
        "last_seen_in",
    ]
    public_query = """
SELECT
    pec.id,
    pec.creature_id,
    pec.public_event_id,
    pe.game_id,
    pe.name,
    pe.event_type,
    pe.reward_type,
    pe.real_zone_id,
    pe.parent_id,
    pec.last_seen_in
FROM public_event_creatures pec
LEFT JOIN public_events pe ON pe.id = pec.public_event_id
WHERE pec.enabled = 'true'
ORDER BY pec.creature_id, pec.id
"""
    name, count = query_relation(
        args,
        "creature_public_event_map",
        public_query,
        public_columns,
        [
            "source_relation_id",
            "jabbithole_creature_id",
            "creature2_id",
            "source_name",
            "creature2_name",
            "match_status",
            "jabbithole_public_event_id",
            "public_event_id",
            "public_event_name",
            "event_type",
            "reward_type",
            "real_zone_id",
            "parent_public_event_id",
            "last_seen_in",
        ],
        creature_map,
        lambda row: row,
        args.limit_relation_rows,
    )
    counts[name] = count
    return counts


def write_spell_detail_maps(args: argparse.Namespace, client) -> Dict[str, int]:
    counts: Dict[str, int] = {}

    def class_spell_status(class_id, spell4_id) -> str:
        has_class = bool(client["classes"].get(to_int(class_id, -1) or -1))
        has_spell = bool(client["spells"].get(to_int(spell4_id, -1) or -1))
        if has_class and has_spell:
            return "matched"
        if has_class:
            return "unmatched_spell"
        if has_spell:
            return "unmatched_class"
        return "unmatched"

    spell_columns = [
        "jabbithole_spell_id",
        "spell4_id",
        "jabbithole_spell_name",
        "slug",
        "tier",
        "flavor",
        "cast_info",
        "range_min",
        "range_max",
        "cast_cost",
        "target",
        "cooldown",
        "mobility",
        "icon",
        "class_id_raw",
        "class_power",
        "combat_mode",
        "school",
        "target_angle",
        "base_jabbithole_spell_id",
        "base_spell4_id",
        "charges",
        "gcd_time",
        "enabled",
        "first_seen_in",
        "last_seen_in",
    ]
    spell_query = """
SELECT
    id,
    game_id,
    name,
    slug,
    tier,
    flavor,
    castinfo,
    rangemin,
    rangemax,
    castcost,
    target,
    cooldown,
    mobility,
    icon,
    classid,
    classpower,
    combatmode,
    school,
    targetangle,
    base_spell_id,
    base_spell_id_game,
    charges,
    gcd_time,
    enabled,
    first_seen_in,
    last_seen_in
FROM spells
ORDER BY game_id, id
"""

    def spell_rows():
        for row in mysql_rows(args, limited(spell_query, args.limit_relation_rows), spell_columns):
            row["client_spell_name"] = spell_name(client, row.get("spell4_id"))
            row["client_base_spell_name"] = spell_name(client, row.get("base_spell4_id"))
            row["match_status"] = "matched" if row["client_spell_name"] else "unmatched"
            yield row

    counts["spell_map.csv"] = write_csv(
        args.output_dir / "spell_map.csv",
        [
            "jabbithole_spell_id",
            "spell4_id",
            "jabbithole_spell_name",
            "client_spell_name",
            "match_status",
            "slug",
            "tier",
            "flavor",
            "cast_info",
            "range_min",
            "range_max",
            "cast_cost",
            "target",
            "cooldown",
            "mobility",
            "icon",
            "class_id_raw",
            "class_power",
            "combat_mode",
            "school",
            "target_angle",
            "base_jabbithole_spell_id",
            "base_spell4_id",
            "client_base_spell_name",
            "charges",
            "gcd_time",
            "enabled",
            "first_seen_in",
            "last_seen_in",
        ],
        spell_rows(),
    )

    class_columns = [
        "jabbithole_class_id",
        "class_id",
        "jabbithole_class_name",
        "description",
        "slug",
        "icon",
        "innate_jabbithole_spell_id",
        "innate_spell4_id",
        "innate_spell_name",
        "enabled",
    ]
    class_query = """
SELECT
    pc.id,
    pc.game_id,
    pc.name,
    pc.description,
    pc.slug,
    pc.icon,
    pc.innate_ability_id,
    s.game_id,
    s.name,
    pc.enabled
FROM player_classes pc
LEFT JOIN spells s ON s.id = pc.innate_ability_id
ORDER BY pc.game_id, pc.id
"""

    def class_rows():
        for row in mysql_rows(args, class_query, class_columns):
            client_class = client["classes"].get(to_int(row.get("class_id"), -1) or -1, {})
            row["client_class_name"] = client_class.get("className", "")
            row["client_class_description"] = client_class.get("classDescription", "")
            row["client_innate_spell_name"] = spell_name(client, row.get("innate_spell4_id"))
            row["match_status"] = "matched" if client_class else "unmatched"
            yield row

    counts["class_map.csv"] = write_csv(
        args.output_dir / "class_map.csv",
        [
            "jabbithole_class_id",
            "class_id",
            "jabbithole_class_name",
            "client_class_name",
            "match_status",
            "description",
            "client_class_description",
            "slug",
            "icon",
            "innate_jabbithole_spell_id",
            "innate_spell4_id",
            "innate_spell_name",
            "client_innate_spell_name",
            "enabled",
        ],
        class_rows(),
    )

    ability_columns = [
        "source_ability_id",
        "jabbithole_spell_id",
        "spell4_id",
        "jabbithole_spell_name",
        "base_jabbithole_spell_id",
        "base_spell4_id",
        "class_id",
        "jabbithole_class_name",
        "game_ability_id",
        "ability_type",
        "tier",
        "level",
        "cost_cash",
        "cost_point",
        "is_amp",
        "tier_bonus",
        "tier_4_bonus",
        "tier_8_bonus",
        "enabled",
    ]
    ability_query = """
SELECT
    ca.id,
    ca.spell_id,
    s.game_id,
    s.name,
    ca.base_spell_id,
    bs.game_id,
    pc.game_id,
    pc.name,
    ca.game_ability_id,
    ca.ability_type,
    ca.tier,
    ca.level,
    ca.cost_cash,
    ca.cost_point,
    ca.is_amp,
    ca.tier_bonus,
    ca.tier_4_bonus,
    ca.tier_8_bonus,
    ca.enabled
FROM class_abilities ca
LEFT JOIN player_classes pc ON pc.id = ca.player_class_id
LEFT JOIN spells s ON s.id = ca.spell_id
LEFT JOIN spells bs ON bs.id = ca.base_spell_id
ORDER BY pc.game_id, ca.level, ca.tier, ca.id
"""

    def ability_rows():
        for row in mysql_rows(args, limited(ability_query, args.limit_relation_rows), ability_columns):
            row["client_class_name"] = lookup_name(client["classes"], row.get("class_id"), "className")
            row["client_spell_name"] = spell_name(client, row.get("spell4_id"))
            row["client_base_spell_name"] = spell_name(client, row.get("base_spell4_id"))
            row["match_status"] = class_spell_status(row.get("class_id"), row.get("spell4_id"))
            yield row

    counts["class_ability_map.csv"] = write_csv(
        args.output_dir / "class_ability_map.csv",
        [
            "source_ability_id",
            "class_id",
            "jabbithole_class_name",
            "client_class_name",
            "jabbithole_spell_id",
            "spell4_id",
            "jabbithole_spell_name",
            "client_spell_name",
            "match_status",
            "base_jabbithole_spell_id",
            "base_spell4_id",
            "client_base_spell_name",
            "game_ability_id",
            "ability_type",
            "tier",
            "level",
            "cost_cash",
            "cost_point",
            "is_amp",
            "tier_bonus",
            "tier_4_bonus",
            "tier_8_bonus",
            "enabled",
        ],
        ability_rows(),
    )

    amp_columns = [
        "source_amp_id",
        "class_id",
        "jabbithole_class_name",
        "jabbithole_spell_id",
        "spell4_id",
        "jabbithole_spell_name",
        "parent_class_amp_id",
        "parent_amp_title",
        "amp_game_id",
        "title",
        "description",
        "category",
        "tier",
        "column_index",
        "row_index",
        "cost",
        "item2_id",
        "child_class_amps_count",
        "enabled",
    ]
    amp_query = """
SELECT
    ca.id,
    pc.game_id,
    pc.name,
    ca.spell_id,
    s.game_id,
    s.name,
    ca.parent_class_amp_id,
    parent.title,
    ca.game_id,
    ca.title,
    ca.description,
    ca.category,
    ca.tier,
    ca.`column`,
    ca.`row`,
    ca.cost,
    COALESCE(ca.item_id, NULLIF(ca.itemid, 0)),
    ca.child_class_amps_count,
    ca.enabled
FROM class_amps ca
LEFT JOIN player_classes pc ON pc.id = ca.player_class_id
LEFT JOIN spells s ON s.id = ca.spell_id
LEFT JOIN class_amps parent ON parent.id = ca.parent_class_amp_id
ORDER BY pc.game_id, ca.tier, ca.`row`, ca.`column`, ca.id
"""

    def amp_rows():
        for row in mysql_rows(args, limited(amp_query, args.limit_relation_rows), amp_columns):
            row["client_class_name"] = lookup_name(client["classes"], row.get("class_id"), "className")
            row["client_spell_name"] = spell_name(client, row.get("spell4_id"))
            row["item_name"] = item_name(client["items"], row.get("item2_id"))
            row["match_status"] = class_spell_status(row.get("class_id"), row.get("spell4_id"))
            yield row

    counts["class_amp_map.csv"] = write_csv(
        args.output_dir / "class_amp_map.csv",
        [
            "source_amp_id",
            "class_id",
            "jabbithole_class_name",
            "client_class_name",
            "jabbithole_spell_id",
            "spell4_id",
            "jabbithole_spell_name",
            "client_spell_name",
            "match_status",
            "parent_class_amp_id",
            "parent_amp_title",
            "amp_game_id",
            "title",
            "description",
            "category",
            "tier",
            "column_index",
            "row_index",
            "cost",
            "item2_id",
            "item_name",
            "child_class_amps_count",
            "enabled",
        ],
        amp_rows(),
    )

    unlock_columns = [
        "source_unlock_id",
        "class_id",
        "jabbithole_class_name",
        "level",
        "title",
        "description",
        "icon",
        "reward_type",
        "raw_game_id",
        "jabbithole_spell_id",
        "spell4_id",
        "jabbithole_spell_name",
        "side",
        "enabled",
    ]
    unlock_query = """
SELECT
    cu.id,
    pc.game_id,
    pc.name,
    cu.level,
    cu.title,
    cu.description,
    cu.icon,
    cu.reward_type,
    cu.game_id,
    cu.spell_id,
    s.game_id,
    s.name,
    cu.side,
    cu.enabled
FROM class_unlocks cu
LEFT JOIN player_classes pc ON pc.id = cu.player_class_id
LEFT JOIN spells s ON s.id = cu.spell_id
ORDER BY pc.game_id, cu.level, cu.id
"""

    def unlock_rows():
        for row in mysql_rows(args, limited(unlock_query, args.limit_relation_rows), unlock_columns):
            row["client_class_name"] = lookup_name(client["classes"], row.get("class_id"), "className")
            row["client_spell_name"] = spell_name(client, row.get("spell4_id"))
            spell4_id = to_int(row.get("spell4_id"))
            if row["client_class_name"] and spell4_id is None:
                row["match_status"] = "non_spell_reward"
            elif row["client_class_name"] and row["client_spell_name"]:
                row["match_status"] = "matched"
            else:
                row["match_status"] = class_spell_status(row.get("class_id"), row.get("spell4_id"))
            yield row

    counts["class_unlock_map.csv"] = write_csv(
        args.output_dir / "class_unlock_map.csv",
        [
            "source_unlock_id",
            "class_id",
            "jabbithole_class_name",
            "client_class_name",
            "level",
            "title",
            "description",
            "icon",
            "reward_type",
            "raw_game_id",
            "jabbithole_spell_id",
            "spell4_id",
            "jabbithole_spell_name",
            "client_spell_name",
            "match_status",
            "side",
            "enabled",
        ],
        unlock_rows(),
    )

    rune_columns = [
        "source_rune_set_spell_id",
        "item2_id",
        "jabbithole_spell_id",
        "spell4_id",
        "jabbithole_spell_name",
        "origin_create",
        "last_seen_in",
    ]
    rune_query = """
SELECT
    rss.id,
    rss.item_id,
    rss.spell_id,
    s.game_id,
    s.name,
    rss.origin_create,
    rss.last_seen_in
FROM rune_set_spells rss
LEFT JOIN spells s ON s.id = rss.spell_id
ORDER BY rss.item_id, rss.id
"""

    def rune_rows():
        for row in mysql_rows(args, limited(rune_query, args.limit_relation_rows), rune_columns):
            row["item_name"] = item_name(client["items"], row.get("item2_id"))
            row["client_spell_name"] = spell_name(client, row.get("spell4_id"))
            row["match_status"] = "matched" if row["client_spell_name"] else "unmatched"
            yield row

    counts["rune_set_spell_map.csv"] = write_csv(
        args.output_dir / "rune_set_spell_map.csv",
        [
            "source_rune_set_spell_id",
            "item2_id",
            "item_name",
            "jabbithole_spell_id",
            "spell4_id",
            "jabbithole_spell_name",
            "client_spell_name",
            "match_status",
            "origin_create",
            "last_seen_in",
        ],
        rune_rows(),
    )

    return counts


def write_achievement_contract_maps(
    args: argparse.Namespace,
    creature_map: Dict[int, Dict[str, object]],
    client,
) -> Dict[str, int]:
    counts: Dict[str, int] = {}

    group_columns = [
        "jabbithole_achievement_group_id",
        "achievement_category_id",
        "jabbithole_group_name",
        "parent_jabbithole_group_id",
        "parent_group_name",
        "achievements_count",
        "achievements_recursive_count",
        "child_groups_count",
        "enabled",
        "last_seen_in",
    ]
    group_query = """
SELECT
    ag.id,
    ag.game_id,
    ag.name,
    ag.parent_id,
    parent.name,
    ag.achievements_count,
    ag.achievements_recursive_count,
    ag.achievement_groups_count,
    ag.enabled,
    ag.last_seen_in
FROM achievement_groups ag
LEFT JOIN achievement_groups parent ON parent.id = ag.parent_id
ORDER BY ag.game_id, ag.id
"""

    def achievement_group_rows():
        for row in mysql_rows(args, group_query, group_columns):
            client_category = client["achievement_categories"].get(to_int(row.get("achievement_category_id"), -1) or -1, {})
            row["client_category_name"] = client_category.get("achievementCategoryName", "")
            row["client_category_full_name"] = client_category.get("achievementCategoryFullName", "")
            row["client_parent_category_id"] = client_category.get("achievementCategoryIdParent", "")
            row["client_parent_category_name"] = lookup_name(client["achievement_categories"], row.get("client_parent_category_id"), "achievementCategoryName")
            row["match_status"] = "matched" if client_category else "unmatched"
            yield row

    counts["achievement_group_map.csv"] = write_csv(
        args.output_dir / "achievement_group_map.csv",
        [
            "jabbithole_achievement_group_id",
            "achievement_category_id",
            "jabbithole_group_name",
            "client_category_name",
            "client_category_full_name",
            "match_status",
            "parent_jabbithole_group_id",
            "parent_group_name",
            "client_parent_category_id",
            "client_parent_category_name",
            "achievements_count",
            "achievements_recursive_count",
            "child_groups_count",
            "enabled",
            "last_seen_in",
        ],
        achievement_group_rows(),
    )

    achievement_columns = [
        "jabbithole_achievement_id",
        "achievement_id",
        "jabbithole_achievement_group_id",
        "jabbithole_achievement_category_id",
        "jabbithole_top_group_id",
        "jabbithole_subgroup_id",
        "parent_jabbithole_achievement_id",
        "tier_root_jabbithole_achievement_id",
        "jabbithole_title",
        "slug",
        "jabbithole_description",
        "jabbithole_description_done",
        "num_required",
        "checklist",
        "is_guild",
        "points",
        "tier",
        "is_dominion",
        "is_exile",
        "world_zone_id",
        "child_achievements_count",
        "achievement_titles_count",
        "enabled",
        "game_version",
        "first_seen_in",
        "last_seen_in",
    ]
    achievement_query = """
SELECT
    id,
    game_id,
    achievement_group_id,
    achievement_category_id,
    achievement_topgroup_id,
    achievement_subgroup_id,
    parent_id,
    tier_root_id,
    name,
    slug,
    description,
    description_done,
    num_required,
    checklist,
    is_guild,
    points,
    tier,
    is_dominion,
    is_exile,
    world_zone_id,
    child_achievements_count,
    achievement_titles_count,
    enabled,
    game_version,
    first_seen_in,
    last_seen_in
FROM achievements
ORDER BY game_id, id
"""

    def achievement_rows():
        for row in mysql_rows(args, limited(achievement_query, args.limit_relation_rows), achievement_columns):
            client_achievement = client["achievements"].get(to_int(row.get("achievement_id"), -1) or -1, {})
            row["client_title"] = client_achievement.get("achievementTitle", "")
            row["client_description"] = client_achievement.get("achievementDescription", "")
            row["client_progress_text"] = client_achievement.get("achievementProgress", "")
            row["client_achievement_type_id"] = client_achievement.get("achievementTypeId", "")
            row["client_category_id"] = client_achievement.get("achievementCategoryId", "")
            row["client_category_name"] = lookup_name(client["achievement_categories"], row.get("client_category_id"), "achievementCategoryName")
            row["client_group_id"] = client_achievement.get("achievementGroupId", "")
            row["client_group_name"] = lookup_name(client["achievement_groups"], row.get("client_group_id"), "achievementGroupName")
            row["client_subgroup_id"] = client_achievement.get("achievementSubGroupId", "")
            row["client_subgroup_name"] = lookup_name(client["achievement_subgroups"], row.get("client_subgroup_id"), "achievementSubGroupName")
            row["client_world_zone_id"] = client_achievement.get("worldZoneId", "")
            row["client_world_zone_name"] = lookup_name(client["world_zones"], row.get("client_world_zone_id"), "worldZoneName")
            row["client_character_title_id"] = client_achievement.get("characterTitleId", "")
            row["client_character_title_name"] = lookup_name(client["character_titles"], row.get("client_character_title_id"), "characterTitleName")
            row["match_status"] = "matched" if client_achievement else "unmatched"
            yield row

    counts["achievement_map.csv"] = write_csv(
        args.output_dir / "achievement_map.csv",
        [
            "jabbithole_achievement_id",
            "achievement_id",
            "jabbithole_title",
            "client_title",
            "match_status",
            "slug",
            "jabbithole_description",
            "client_description",
            "jabbithole_description_done",
            "client_progress_text",
            "num_required",
            "checklist",
            "is_guild",
            "points",
            "tier",
            "is_dominion",
            "is_exile",
            "jabbithole_achievement_group_id",
            "client_group_id",
            "client_group_name",
            "jabbithole_achievement_category_id",
            "client_category_id",
            "client_category_name",
            "jabbithole_top_group_id",
            "jabbithole_subgroup_id",
            "client_subgroup_id",
            "client_subgroup_name",
            "parent_jabbithole_achievement_id",
            "tier_root_jabbithole_achievement_id",
            "world_zone_id",
            "client_world_zone_id",
            "client_world_zone_name",
            "client_achievement_type_id",
            "client_character_title_id",
            "client_character_title_name",
            "child_achievements_count",
            "achievement_titles_count",
            "enabled",
            "game_version",
            "first_seen_in",
            "last_seen_in",
        ],
        achievement_rows(),
    )

    achievement_title_columns = [
        "source_relation_id",
        "jabbithole_achievement_id",
        "achievement_id",
        "jabbithole_achievement_title",
        "character_title_id",
        "jabbithole_title_name",
        "jabbithole_title_format",
        "enabled",
    ]
    achievement_title_query = """
SELECT
    at.id,
    at.achievement_id,
    a.game_id,
    a.name,
    at.title_id,
    t.name,
    t.format,
    at.enabled
FROM achievement_titles at
LEFT JOIN achievements a ON a.id = at.achievement_id
LEFT JOIN titles t ON t.id = at.title_id
ORDER BY a.game_id, at.id
"""

    def achievement_title_rows():
        for row in mysql_rows(args, limited(achievement_title_query, args.limit_relation_rows), achievement_title_columns):
            row["client_achievement_title"] = lookup_name(client["achievements"], row.get("achievement_id"), "achievementTitle")
            row["client_character_title_name"] = lookup_name(client["character_titles"], row.get("character_title_id"), "characterTitleName")
            row["client_character_title_text"] = lookup_name(client["character_titles"], row.get("character_title_id"), "characterTitleText")
            row["match_status"] = "matched" if row["client_achievement_title"] and row["client_character_title_name"] else "unmatched"
            yield row

    counts["achievement_title_map.csv"] = write_csv(
        args.output_dir / "achievement_title_map.csv",
        [
            "source_relation_id",
            "jabbithole_achievement_id",
            "achievement_id",
            "jabbithole_achievement_title",
            "client_achievement_title",
            "character_title_id",
            "jabbithole_title_name",
            "jabbithole_title_format",
            "client_character_title_name",
            "client_character_title_text",
            "match_status",
            "enabled",
        ],
        achievement_title_rows(),
    )

    character_achievement_columns = [
        "source_character_achievement_id",
        "character_id",
        "jabbithole_achievement_id",
        "achievement_id",
        "jabbithole_achievement_title",
        "unlocked_at",
    ]
    character_achievement_query = """
SELECT
    ca.id,
    ca.character_id,
    ca.achievement_id,
    a.game_id,
    a.name,
    ca.unlocked_at
FROM character_achievements ca
LEFT JOIN achievements a ON a.id = ca.achievement_id
ORDER BY ca.character_id, ca.id
"""

    def character_achievement_rows():
        for row in mysql_rows(args, limited(character_achievement_query, args.limit_relation_rows), character_achievement_columns):
            row["client_achievement_title"] = lookup_name(client["achievements"], row.get("achievement_id"), "achievementTitle")
            row["match_status"] = "matched" if row["client_achievement_title"] else "unmatched"
            yield row

    counts["character_achievement_map.csv"] = write_csv(
        args.output_dir / "character_achievement_map.csv",
        [
            "source_character_achievement_id",
            "character_id",
            "jabbithole_achievement_id",
            "achievement_id",
            "jabbithole_achievement_title",
            "client_achievement_title",
            "match_status",
            "unlocked_at",
        ],
        character_achievement_rows(),
    )

    contract_columns = [
        "jabbithole_contract_id",
        "contract_game_id",
        "jabbithole_quest_id",
        "quest2_id",
        "jabbithole_quest_name",
        "quality",
        "contract_type",
        "reward_track_type",
        "xp",
        "is_exile",
        "is_dominion",
        "enabled",
        "first_seen_in",
        "last_seen_in",
    ]
    contract_query = """
SELECT
    c.id,
    c.game_id,
    c.quest_id,
    q.game_id,
    q.name,
    c.quality,
    c.contract_type,
    c.reward_track_type,
    c.xp,
    c.is_exile,
    c.is_dominion,
    c.enabled,
    c.first_seen_in,
    c.last_seen_in
FROM contracts c
LEFT JOIN quests q ON q.id = c.quest_id
ORDER BY c.game_id, c.id
"""

    def contract_rows():
        for row in mysql_rows(args, limited(contract_query, args.limit_relation_rows), contract_columns):
            row["client_quest_name"] = lookup_name(client["quests"], row.get("quest2_id"), "questName")
            row["match_status"] = "matched" if row["client_quest_name"] else "unmatched"
            yield row

    counts["contract_map.csv"] = write_csv(
        args.output_dir / "contract_map.csv",
        [
            "jabbithole_contract_id",
            "contract_game_id",
            "jabbithole_quest_id",
            "quest2_id",
            "jabbithole_quest_name",
            "client_quest_name",
            "match_status",
            "quality",
            "contract_type",
            "reward_track_type",
            "xp",
            "is_exile",
            "is_dominion",
            "enabled",
            "first_seen_in",
            "last_seen_in",
        ],
        contract_rows(),
    )

    contract_reward_columns = [
        "source_reward_id",
        "contract_reward_game_id",
        "item2_id",
        "tier",
        "raw_item_id",
        "asset",
        "contract_type",
        "is_casque",
        "cost",
        "amount",
        "choice_idx",
        "trid",
        "is_exile",
        "is_dominion",
        "enabled",
        "last_seen_in",
    ]
    contract_reward_query = """
SELECT
    id,
    game_id,
    game_item_id,
    tier,
    item_id,
    asset,
    contract_type,
    is_casque,
    cost,
    amount,
    choice_idx,
    trid,
    is_exile,
    is_dominion,
    enabled,
    last_seen_in
FROM contract_rewards
ORDER BY contract_type, tier, game_id, id
"""

    def contract_reward_rows():
        for row in mysql_rows(args, limited(contract_reward_query, args.limit_relation_rows), contract_reward_columns):
            row["item_name"] = item_name(client["items"], row.get("item2_id"))
            row["match_status"] = "matched" if row["item_name"] else "unmatched"
            yield row

    counts["contract_reward_map.csv"] = write_csv(
        args.output_dir / "contract_reward_map.csv",
        [
            "source_reward_id",
            "contract_reward_game_id",
            "item2_id",
            "item_name",
            "match_status",
            "tier",
            "raw_item_id",
            "asset",
            "contract_type",
            "is_casque",
            "cost",
            "amount",
            "choice_idx",
            "trid",
            "is_exile",
            "is_dominion",
            "enabled",
            "last_seen_in",
        ],
        contract_reward_rows(),
    )

    contract_creature_columns = [
        "source_relation_id",
        "jabbithole_quest_id",
        "quest2_id",
        "jabbithole_quest_name",
        "jabbithole_creature_id",
        "enabled",
        "last_seen_in",
    ]
    contract_creature_query = """
SELECT
    cc.id,
    cc.quest_id,
    q.game_id,
    q.name,
    cc.creature_id,
    cc.enabled,
    cc.last_seen_in
FROM contract_creatures cc
LEFT JOIN quests q ON q.id = cc.quest_id
ORDER BY q.game_id, cc.creature_id, cc.id
"""

    def contract_creature_rows():
        for row in mysql_rows(args, limited(contract_creature_query, args.limit_relation_rows), contract_creature_columns):
            row = apply_creature_map(row, creature_map)
            row["client_quest_name"] = lookup_name(client["quests"], row.get("quest2_id"), "questName")
            has_creature = bool(to_int(row.get("creature2_id")))
            if row["client_quest_name"] and has_creature:
                row["relation_match_status"] = "matched"
            elif row["client_quest_name"]:
                row["relation_match_status"] = "unmatched_creature"
            elif has_creature:
                row["relation_match_status"] = "unmatched_quest"
            else:
                row["relation_match_status"] = "unmatched"
            yield row

    counts["contract_creature_map.csv"] = write_csv(
        args.output_dir / "contract_creature_map.csv",
        [
            "source_relation_id",
            "jabbithole_quest_id",
            "quest2_id",
            "jabbithole_quest_name",
            "client_quest_name",
            "jabbithole_creature_id",
            "creature2_id",
            "source_name",
            "creature2_name",
            "match_status",
            "relation_match_status",
            "enabled",
            "last_seen_in",
        ],
        contract_creature_rows(),
    )

    return counts


def write_housing_cosmetic_maps(args: argparse.Namespace, client) -> Dict[str, int]:
    counts: Dict[str, int] = {}

    counts["housing_decor_type_client_map.csv"] = write_csv(
        args.output_dir / "housing_decor_type_client_map.csv",
        ["housing_decor_type_id", "housing_decor_type_name", "lua_string"],
        (
            {
                "housing_decor_type_id": type_id,
                "housing_decor_type_name": row.get("housingDecorTypeName", ""),
                "lua_string": row.get("luaString", ""),
            }
            for type_id, row in sorted(client["housing_decor_types"].items())
        ),
    )

    counts["housing_decor_client_map.csv"] = write_csv(
        args.output_dir / "housing_decor_client_map.csv",
        [
            "housing_decor_info_id",
            "housing_decor_name",
            "housing_decor_type_id",
            "housing_decor_type_name",
            "hook_type_id",
            "hook_asset_id",
            "cost",
            "currency_type_id",
            "active_prop_creature2_id",
            "active_prop_creature_name",
            "unlock_prerequisite_id",
            "interior_buff_spell4_id",
            "interior_buff_spell_name",
            "decor_limit_category_id",
            "decor_limit",
            "flags",
            "min_scale",
            "max_scale",
            "alt_preview_asset",
            "alt_edit_asset",
        ],
        (
            {
                "housing_decor_info_id": decor_id,
                "housing_decor_name": row.get("housingDecorName", ""),
                "housing_decor_type_id": row.get("housingDecorTypeId", ""),
                "housing_decor_type_name": lookup_name(client["housing_decor_types"], row.get("housingDecorTypeId"), "housingDecorTypeName"),
                "hook_type_id": row.get("hookTypeId", ""),
                "hook_asset_id": row.get("hookAssetId", ""),
                "cost": row.get("cost", ""),
                "currency_type_id": row.get("costCurrencyTypeId", ""),
                "active_prop_creature2_id": row.get("creature2IdActiveProp", ""),
                "active_prop_creature_name": lookup_name(client["creatures"], row.get("creature2IdActiveProp"), "clientName"),
                "unlock_prerequisite_id": row.get("prerequisiteIdUnlock", ""),
                "interior_buff_spell4_id": row.get("spell4IdInteriorBuff", ""),
                "interior_buff_spell_name": spell_name(client, row.get("spell4IdInteriorBuff")),
                "decor_limit_category_id": row.get("housingDecorLimitCategoryId", ""),
                "decor_limit": client["housing_decor_limit_categories"].get(to_int(row.get("housingDecorLimitCategoryId"), -1) or -1, {}).get("decorLimit", ""),
                "flags": row.get("flags", ""),
                "min_scale": row.get("minScale", ""),
                "max_scale": row.get("maxScale", ""),
                "alt_preview_asset": row.get("altPreviewAsset", ""),
                "alt_edit_asset": row.get("altEditAsset", ""),
            }
            for decor_id, row in sorted(client["housing_decors"].items())
        ),
    )

    counts["housing_decor_limit_category_client_map.csv"] = write_csv(
        args.output_dir / "housing_decor_limit_category_client_map.csv",
        ["housing_decor_limit_category_id", "decor_limit"],
        (
            {
                "housing_decor_limit_category_id": category_id,
                "decor_limit": row.get("decorLimit", ""),
            }
            for category_id, row in sorted(client["housing_decor_limit_categories"].items())
        ),
    )

    counts["housing_plug_item_client_map.csv"] = write_csv(
        args.output_dir / "housing_plug_item_client_map.csv",
        [
            "housing_plug_item_id",
            "housing_plug_name",
            "tooltip",
            "housing_plot_type_id",
            "flags",
            "world_id_plug_0",
            "world_name_plug_0",
            "world_id_plug_1",
            "world_id_plug_2",
            "world_id_plug_3",
            "provided_resource_0",
            "provided_resource_0_name",
            "prerequisite_resource_0",
            "prerequisite_resource_0_name",
            "feature_type_flags",
            "contribution_info_0",
            "next_upgrade_plug_item_id",
            "next_upgrade_plug_name",
            "unlock_prerequisite_id",
            "housing_build_id",
            "housing_build_description",
            "upkeep_type",
            "upkeep_charges",
            "upkeep_time",
            "plug_type",
            "account_item_upsell_id",
            "screenshot_0",
            "screenshot_1",
            "screenshot_2",
        ],
        (
            {
                "housing_plug_item_id": plug_id,
                "housing_plug_name": row.get("housingPlugName", ""),
                "tooltip": row.get("housingPlugTooltip", ""),
                "housing_plot_type_id": row.get("housingPlotTypeId", ""),
                "flags": row.get("flags", ""),
                "world_id_plug_0": row.get("worldIdPlug00", ""),
                "world_name_plug_0": lookup_name(client["worlds"], row.get("worldIdPlug00"), "worldName"),
                "world_id_plug_1": row.get("worldIdPlug01", ""),
                "world_id_plug_2": row.get("worldIdPlug02", ""),
                "world_id_plug_3": row.get("worldIdPlug03", ""),
                "provided_resource_0": row.get("housingResourceIdProvided00", ""),
                "provided_resource_0_name": lookup_name(client["housing_resources"], row.get("housingResourceIdProvided00"), "housingResourceName"),
                "prerequisite_resource_0": row.get("housingResourceIdPrerequisite00", ""),
                "prerequisite_resource_0_name": lookup_name(client["housing_resources"], row.get("housingResourceIdPrerequisite00"), "housingResourceName"),
                "feature_type_flags": row.get("housingFeatureTypeFlags", ""),
                "contribution_info_0": row.get("housingContributionInfoId00", ""),
                "next_upgrade_plug_item_id": row.get("housingPlugItemIdNextUpgrade", ""),
                "next_upgrade_plug_name": lookup_name(client["housing_plug_items"], row.get("housingPlugItemIdNextUpgrade"), "housingPlugName"),
                "unlock_prerequisite_id": row.get("prerequisiteIdUnlock", ""),
                "housing_build_id": row.get("housingBuildId", ""),
                "housing_build_description": lookup_name(client["housing_builds"], row.get("housingBuildId"), "description"),
                "upkeep_type": row.get("housingUpkeepTypeEnum", ""),
                "upkeep_charges": row.get("upkeepCharges", ""),
                "upkeep_time": row.get("upkeepTime", ""),
                "plug_type": row.get("housingPlugTypeEnum", ""),
                "account_item_upsell_id": row.get("accountItemIdUpsell", ""),
                "screenshot_0": row.get("screenshotSprite00", ""),
                "screenshot_1": row.get("screenshotSprite01", ""),
                "screenshot_2": row.get("screenshotSprite02", ""),
            }
            for plug_id, row in sorted(client["housing_plug_items"].items())
        ),
    )

    counts["housing_wallpaper_client_map.csv"] = write_csv(
        args.output_dir / "housing_wallpaper_client_map.csv",
        [
            "housing_wallpaper_info_id",
            "housing_wallpaper_name",
            "cost",
            "currency_type_id",
            "world_sky_id",
            "replaceable_material_info_id",
            "flags",
            "unlock_prerequisite_id",
            "use_prerequisite_id",
            "unlock_index",
            "sound_zone_kit_id",
            "account_item_upsell_id",
        ],
        (
            {
                "housing_wallpaper_info_id": wallpaper_id,
                "housing_wallpaper_name": row.get("housingWallpaperName", ""),
                "cost": row.get("cost", ""),
                "currency_type_id": row.get("costCurrencyTypeId", ""),
                "world_sky_id": row.get("worldSkyId", ""),
                "replaceable_material_info_id": row.get("replaceableMaterialInfoId", ""),
                "flags": row.get("flags", ""),
                "unlock_prerequisite_id": row.get("prerequisiteIdUnlock", ""),
                "use_prerequisite_id": row.get("prerequisiteIdUse", ""),
                "unlock_index": row.get("unlockIndex", ""),
                "sound_zone_kit_id": row.get("soundZoneKitId", ""),
                "account_item_upsell_id": row.get("accountItemIdUpsell", ""),
            }
            for wallpaper_id, row in sorted(client["housing_wallpapers"].items())
        ),
    )

    counts["housing_residence_client_map.csv"] = write_csv(
        args.output_dir / "housing_residence_client_map.csv",
        [
            "housing_residence_info_id",
            "default_roof_decor_id",
            "default_roof_decor_name",
            "default_entryway_decor_id",
            "default_entryway_decor_name",
            "default_door_decor_id",
            "default_door_decor_name",
            "default_wallpaper_id",
            "default_wallpaper_name",
            "inside_world_location_0",
            "outside_world_location_0",
        ],
        (
            {
                "housing_residence_info_id": residence_id,
                "default_roof_decor_id": row.get("housingDecorInfoIdDefaultRoof", ""),
                "default_roof_decor_name": lookup_name(client["housing_decors"], row.get("housingDecorInfoIdDefaultRoof"), "housingDecorName"),
                "default_entryway_decor_id": row.get("housingDecorInfoIdDefaultEntryway", ""),
                "default_entryway_decor_name": lookup_name(client["housing_decors"], row.get("housingDecorInfoIdDefaultEntryway"), "housingDecorName"),
                "default_door_decor_id": row.get("housingDecorInfoIdDefaultDoor", ""),
                "default_door_decor_name": lookup_name(client["housing_decors"], row.get("housingDecorInfoIdDefaultDoor"), "housingDecorName"),
                "default_wallpaper_id": row.get("housingWallpaperInfoIdDefault", ""),
                "default_wallpaper_name": lookup_name(client["housing_wallpapers"], row.get("housingWallpaperInfoIdDefault"), "housingWallpaperName"),
                "inside_world_location_0": row.get("worldLocation2IdInside00", ""),
                "outside_world_location_0": row.get("worldLocation2IdOutside00", ""),
            }
            for residence_id, row in sorted(client["housing_residences"].items())
        ),
    )

    counts["housing_build_client_map.csv"] = write_csv(
        args.output_dir / "housing_build_client_map.csv",
        [
            "housing_build_id",
            "description",
            "asset_path",
            "construction_effects_id",
            "build_pre_delay_ms",
            "build_post_delay_ms",
            "build_time_0",
            "build_time_1",
            "build_time_2",
            "build_time_3",
        ],
        (
            {
                "housing_build_id": build_id,
                "description": row.get("description", ""),
                "asset_path": row.get("assetPath", ""),
                "construction_effects_id": row.get("constructionEffectsId", ""),
                "build_pre_delay_ms": row.get("buildPreDelayTimeMS", ""),
                "build_post_delay_ms": row.get("buildPostDelayTimeMS", ""),
                "build_time_0": row.get("buildTime00", ""),
                "build_time_1": row.get("buildTime01", ""),
                "build_time_2": row.get("buildTime02", ""),
                "build_time_3": row.get("buildTime03", ""),
            }
            for build_id, row in sorted(client["housing_builds"].items())
        ),
    )

    counts["housing_resource_client_map.csv"] = write_csv(
        args.output_dir / "housing_resource_client_map.csv",
        ["housing_resource_id", "housing_resource_name"],
        (
            {
                "housing_resource_id": resource_id,
                "housing_resource_name": row.get("housingResourceName", ""),
            }
            for resource_id, row in sorted(client["housing_resources"].items())
        ),
    )

    counts["housing_contribution_type_client_map.csv"] = write_csv(
        args.output_dir / "housing_contribution_type_client_map.csv",
        ["housing_contribution_type_id", "description", "enum_name"],
        (
            {
                "housing_contribution_type_id": contribution_type_id,
                "description": row.get("description", ""),
                "enum_name": row.get("enumName", ""),
            }
            for contribution_type_id, row in sorted(client["housing_contribution_types"].items())
        ),
    )

    counts["housing_contribution_info_client_map.csv"] = write_csv(
        args.output_dir / "housing_contribution_info_client_map.csv",
        [
            "housing_contribution_info_id",
            "housing_contribution_type_id",
            "housing_contribution_type_name",
            "point_requirement",
            "item2_tier_0",
            "item_tier_0_name",
            "item2_tier_1",
            "item_tier_1_name",
            "point_value_tier_0",
            "point_value_tier_1",
        ],
        (
            {
                "housing_contribution_info_id": info_id,
                "housing_contribution_type_id": row.get("housingContributionTypeId", ""),
                "housing_contribution_type_name": lookup_name(client["housing_contribution_types"], row.get("housingContributionTypeId"), "description"),
                "point_requirement": row.get("contributionPointRequirement", ""),
                "item2_tier_0": row.get("item2IdTier00", ""),
                "item_tier_0_name": item_name(client["items"], row.get("item2IdTier00")),
                "item2_tier_1": row.get("item2IdTier01", ""),
                "item_tier_1_name": item_name(client["items"], row.get("item2IdTier01")),
                "point_value_tier_0": row.get("contributionPointValueTier00", ""),
                "point_value_tier_1": row.get("contributionPointValueTier01", ""),
            }
            for info_id, row in sorted(client["housing_contribution_info"].items())
        ),
    )

    counts["housing_property_client_map.csv"] = write_csv(
        args.output_dir / "housing_property_client_map.csv",
        [
            "housing_property_info_id",
            "housing_property_name",
            "world_id",
            "world_name",
            "housing_map_info_id",
            "cost",
            "facing",
            "world_location_id",
            "world_zone_id",
            "world_zone_name",
            "property_type_id",
        ],
        (
            {
                "housing_property_info_id": property_id,
                "housing_property_name": row.get("housingPropertyName", ""),
                "world_id": row.get("worldId", ""),
                "world_name": lookup_name(client["worlds"], row.get("worldId"), "worldName"),
                "housing_map_info_id": row.get("housingMapInfoId", ""),
                "cost": row.get("cost", ""),
                "facing": row.get("housingFacingEnum", ""),
                "world_location_id": row.get("worldLocation2Id", ""),
                "world_zone_id": row.get("worldZoneId", ""),
                "world_zone_name": lookup_name(client["world_zones"], row.get("worldZoneId"), "worldZoneName"),
                "property_type_id": row.get("housingPropertyTypeId", ""),
            }
            for property_id, row in sorted(client["housing_properties"].items())
        ),
    )

    counts["housing_neighborhood_client_map.csv"] = write_csv(
        args.output_dir / "housing_neighborhood_client_map.csv",
        [
            "housing_neighborhood_info_id",
            "base_cost",
            "max_population",
            "population_threshold",
            "faction_enum",
            "feature_type_enum",
            "playstyle_type_enum",
            "primary_map_info_id",
            "secondary_map_info_id",
        ],
        (
            {
                "housing_neighborhood_info_id": neighborhood_id,
                "base_cost": row.get("baseCost", ""),
                "max_population": row.get("maxPopulation", ""),
                "population_threshold": row.get("populationThreshold", ""),
                "faction_enum": row.get("housingFactionEnum", ""),
                "feature_type_enum": row.get("housingFeatureTypeEnum", ""),
                "playstyle_type_enum": row.get("housingPlaystyleTypeEnum", ""),
                "primary_map_info_id": row.get("housingMapInfoIdPrimary", ""),
                "secondary_map_info_id": row.get("housingMapInfoIdSecondary", ""),
            }
            for neighborhood_id, row in sorted(client["housing_neighborhoods"].items())
        ),
    )

    counts["housing_map_client_map.csv"] = write_csv(
        args.output_dir / "housing_map_client_map.csv",
        ["housing_map_info_id", "world_id", "world_name", "private_property_count", "public_property_count"],
        (
            {
                "housing_map_info_id": map_id,
                "world_id": row.get("worldId", ""),
                "world_name": lookup_name(client["worlds"], row.get("worldId"), "worldName"),
                "private_property_count": row.get("privatePropertyCount", ""),
                "public_property_count": row.get("publicPropertyCount", ""),
            }
            for map_id, row in sorted(client["housing_maps"].items())
        ),
    )

    counts["housing_plot_type_client_map.csv"] = write_csv(
        args.output_dir / "housing_plot_type_client_map.csv",
        ["housing_plot_type_id", "max_placed_decor"],
        (
            {
                "housing_plot_type_id": plot_type_id,
                "max_placed_decor": row.get("maxPlacedDecor", ""),
            }
            for plot_type_id, row in sorted(client["housing_plot_types"].items())
        ),
    )

    counts["housing_plot_client_map.csv"] = write_csv(
        args.output_dir / "housing_plot_client_map.csv",
        [
            "housing_plot_info_id",
            "world_socket_id",
            "plot_type",
            "housing_property_info_id",
            "housing_property_name",
            "property_plot_index",
            "default_plug_item_id",
            "default_plug_item_name",
        ],
        (
            {
                "housing_plot_info_id": plot_id,
                "world_socket_id": row.get("worldSocketId", ""),
                "plot_type": row.get("plotType", ""),
                "housing_property_info_id": row.get("housingPropertyInfoId", ""),
                "housing_property_name": lookup_name(client["housing_properties"], row.get("housingPropertyInfoId"), "housingPropertyName"),
                "property_plot_index": row.get("housingPropertyPlotIndex", ""),
                "default_plug_item_id": row.get("housingPlugItemIdDefault", ""),
                "default_plug_item_name": lookup_name(client["housing_plug_items"], row.get("housingPlugItemIdDefault"), "housingPlugName"),
            }
            for plot_id, row in sorted(client["housing_plots"].items())
        ),
    )

    counts["housing_mannequin_pose_client_map.csv"] = write_csv(
        args.output_dir / "housing_mannequin_pose_client_map.csv",
        ["housing_mannequin_pose_id", "enum_name", "pose_name", "model_sequence_id"],
        (
            {
                "housing_mannequin_pose_id": pose_id,
                "enum_name": row.get("enumName", ""),
                "pose_name": row.get("housingMannequinPoseName", ""),
                "model_sequence_id": row.get("modelSequenceId", ""),
            }
            for pose_id, row in sorted(client["housing_mannequin_poses"].items())
        ),
    )

    counts["housing_warplot_boss_token_client_map.csv"] = write_csv(
        args.output_dir / "housing_warplot_boss_token_client_map.csv",
        ["housing_warplot_boss_token_id", "summon_spell4_id", "summon_spell_name", "minimum_upgrade_tier", "linked_plug_item_id", "linked_plug_item_name"],
        (
            {
                "housing_warplot_boss_token_id": token_id,
                "summon_spell4_id": row.get("spell4IdSummon", ""),
                "summon_spell_name": spell_name(client, row.get("spell4IdSummon")),
                "minimum_upgrade_tier": row.get("minimumUpgradeTierEnum", ""),
                "linked_plug_item_id": row.get("housingPlugItemIdLinked", ""),
                "linked_plug_item_name": lookup_name(client["housing_plug_items"], row.get("housingPlugItemIdLinked"), "housingPlugName"),
            }
            for token_id, row in sorted(client["housing_warplot_boss_tokens"].items())
        ),
    )

    def warplot_ability_names(row: Dict[str, object]) -> str:
        spell_ids = []
        for index in range(12):
            spell_id = to_int(row.get(f"spell4IdAbility{index:02d}"))
            if spell_id and spell_id not in spell_ids:
                spell_ids.append(spell_id)
        return "; ".join(f"{spell_id}:{spell_name(client, spell_id)}" for spell_id in spell_ids)

    counts["housing_warplot_plug_info_client_map.csv"] = write_csv(
        args.output_dir / "housing_warplot_plug_info_client_map.csv",
        [
            "housing_warplot_plug_info_id",
            "housing_plug_item_id",
            "housing_plug_name",
            "maintenance_cost",
            "upgrade_cost_0",
            "upgrade_cost_1",
            "upgrade_cost_2",
            "ability_spells",
        ],
        (
            {
                "housing_warplot_plug_info_id": info_id,
                "housing_plug_item_id": row.get("housingPlugItemId", ""),
                "housing_plug_name": lookup_name(client["housing_plug_items"], row.get("housingPlugItemId"), "housingPlugName"),
                "maintenance_cost": row.get("maintenanceCost", ""),
                "upgrade_cost_0": row.get("upgradeCost00", ""),
                "upgrade_cost_1": row.get("upgradeCost01", ""),
                "upgrade_cost_2": row.get("upgradeCost02", ""),
                "ability_spells": warplot_ability_names(row),
            }
            for info_id, row in sorted(client["housing_warplot_plug_info"].items())
        ),
    )

    counts["dye_color_ramp_client_map.csv"] = write_csv(
        args.output_dir / "dye_color_ramp_client_map.csv",
        ["dye_color_ramp_id", "dye_name", "ramp_index", "cost_multiplier", "component_map_enum", "flags", "prerequisite_id"],
        (
            {
                "dye_color_ramp_id": dye_id,
                "dye_name": row.get("dyeName", ""),
                "ramp_index": row.get("rampIndex", ""),
                "cost_multiplier": row.get("costMultiplier", ""),
                "component_map_enum": row.get("componentMapEnum", ""),
                "flags": row.get("flags", ""),
                "prerequisite_id": row.get("prerequisiteId", ""),
            }
            for dye_id, row in sorted(client["dye_color_ramps"].items())
        ),
    )

    counts["pet_flair_client_map.csv"] = write_csv(
        args.output_dir / "pet_flair_client_map.csv",
        [
            "pet_flair_id",
            "pet_flair_tooltip",
            "flair_type",
            "spell4_id",
            "spell_name",
            "unlock_bit_0",
            "unlock_bit_1",
            "item_display_0",
            "item_display_1",
            "prerequisite_id",
        ],
        (
            {
                "pet_flair_id": flair_id,
                "pet_flair_tooltip": row.get("petFlairTooltip", ""),
                "flair_type": row.get("type", ""),
                "spell4_id": row.get("spell4Id", ""),
                "spell_name": spell_name(client, row.get("spell4Id")),
                "unlock_bit_0": row.get("unlockBitIndex00", ""),
                "unlock_bit_1": row.get("unlockBitIndex01", ""),
                "item_display_0": row.get("itemDisplayId00", ""),
                "item_display_1": row.get("itemDisplayId01", ""),
                "prerequisite_id": row.get("prerequisiteId", ""),
            }
            for flair_id, row in sorted(client["pet_flairs"].items())
        ),
    )

    decor_type_columns = [
        "jabbithole_decor_type_id",
        "housing_decor_type_id",
        "jabbithole_decor_type_name",
        "housing_decors_count",
        "enabled",
    ]
    decor_type_query = """
SELECT
    id,
    game_id,
    name,
    housing_decors_count,
    enabled
FROM housing_decor_types
ORDER BY game_id, id
"""

    def decor_type_rows():
        for row in mysql_rows(args, decor_type_query, decor_type_columns):
            client_row = client["housing_decor_types"].get(to_int(row.get("housing_decor_type_id"), -1) or -1, {})
            row["client_decor_type_name"] = client_row.get("housingDecorTypeName", "")
            row["client_lua_string"] = client_row.get("luaString", "")
            row["match_status"] = "matched" if client_row else "unmatched"
            yield row

    counts["housing_decor_type_map.csv"] = write_csv(
        args.output_dir / "housing_decor_type_map.csv",
        [
            "jabbithole_decor_type_id",
            "housing_decor_type_id",
            "jabbithole_decor_type_name",
            "client_decor_type_name",
            "client_lua_string",
            "match_status",
            "housing_decors_count",
            "enabled",
        ],
        decor_type_rows(),
    )

    decor_columns = [
        "jabbithole_decor_id",
        "housing_decor_info_id",
        "jabbithole_decor_name",
        "slug",
        "jabbithole_decor_type_id",
        "housing_decor_type_id",
        "jabbithole_decor_type_name",
        "currency_id",
        "cost",
        "jabbithole_spell_id",
        "spell4_id",
        "jabbithole_spell_name",
        "item2_id",
        "hook_type",
        "flags",
        "hook_asset_id",
        "enabled",
        "last_seen_in",
    ]
    decor_query = """
SELECT
    hd.id,
    hd.game_id,
    hd.name,
    hd.slug,
    hd.housing_decor_type_id,
    hdt.game_id,
    hdt.name,
    hd.currency_id,
    hd.cost,
    hd.spell_id,
    s.game_id,
    s.name,
    hd.item_id,
    hd.hooktype,
    hd.flags,
    hd.hookassetid,
    hd.enabled,
    hd.last_seen_in
FROM housing_decors hd
LEFT JOIN housing_decor_types hdt ON hdt.id = hd.housing_decor_type_id
LEFT JOIN spells s ON s.id = hd.spell_id
ORDER BY hd.game_id, hd.id
"""

    def decor_rows():
        for row in mysql_rows(args, limited(decor_query, args.limit_relation_rows), decor_columns):
            client_decor = client["housing_decors"].get(to_int(row.get("housing_decor_info_id"), -1) or -1, {})
            row["client_decor_name"] = client_decor.get("housingDecorName", "")
            row["client_decor_type_name"] = lookup_name(client["housing_decor_types"], row.get("housing_decor_type_id"), "housingDecorTypeName")
            row["client_spell_name"] = spell_name(client, row.get("spell4_id"))
            row["item_name"] = item_name(client["items"], row.get("item2_id"))
            row["active_prop_creature2_id"] = client_decor.get("creature2IdActiveProp", "")
            row["active_prop_creature_name"] = lookup_name(client["creatures"], row.get("active_prop_creature2_id"), "clientName")
            row["match_status"] = "matched" if client_decor else "unmatched"
            yield row

    counts["housing_decor_map.csv"] = write_csv(
        args.output_dir / "housing_decor_map.csv",
        [
            "jabbithole_decor_id",
            "housing_decor_info_id",
            "jabbithole_decor_name",
            "client_decor_name",
            "match_status",
            "slug",
            "jabbithole_decor_type_id",
            "housing_decor_type_id",
            "jabbithole_decor_type_name",
            "client_decor_type_name",
            "currency_id",
            "cost",
            "jabbithole_spell_id",
            "spell4_id",
            "jabbithole_spell_name",
            "client_spell_name",
            "item2_id",
            "item_name",
            "active_prop_creature2_id",
            "active_prop_creature_name",
            "hook_type",
            "flags",
            "hook_asset_id",
            "enabled",
            "last_seen_in",
        ],
        decor_rows(),
    )

    enhancement_columns = [
        "jabbithole_enhancement_id",
        "housing_plug_item_id",
        "jabbithole_enhancement_name",
        "slug",
        "description",
        "enhancement_type",
        "parent_jabbithole_enhancement_id",
        "prerequisites",
        "screenshots",
        "costs",
        "enabled",
        "last_seen_in",
    ]
    enhancement_query = """
SELECT
    id,
    game_id,
    name,
    slug,
    description,
    etype,
    parent_id,
    prerequisites,
    screenshots,
    costs,
    enabled,
    last_seen_in
FROM housing_enhancements
ORDER BY game_id, id
"""

    def enhancement_rows():
        for row in mysql_rows(args, limited(enhancement_query, args.limit_relation_rows), enhancement_columns):
            client_plug = client["housing_plug_items"].get(to_int(row.get("housing_plug_item_id"), -1) or -1, {})
            row["client_plug_name"] = client_plug.get("housingPlugName", "")
            row["client_tooltip"] = client_plug.get("housingPlugTooltip", "")
            row["world_id_plug_0"] = client_plug.get("worldIdPlug00", "")
            row["world_name_plug_0"] = lookup_name(client["worlds"], row.get("world_id_plug_0"), "worldName")
            row["next_upgrade_plug_item_id"] = client_plug.get("housingPlugItemIdNextUpgrade", "")
            row["next_upgrade_plug_name"] = lookup_name(client["housing_plug_items"], row.get("next_upgrade_plug_item_id"), "housingPlugName")
            row["match_status"] = "matched" if client_plug else "unmatched"
            yield row

    counts["housing_enhancement_map.csv"] = write_csv(
        args.output_dir / "housing_enhancement_map.csv",
        [
            "jabbithole_enhancement_id",
            "housing_plug_item_id",
            "jabbithole_enhancement_name",
            "client_plug_name",
            "match_status",
            "slug",
            "description",
            "client_tooltip",
            "enhancement_type",
            "parent_jabbithole_enhancement_id",
            "world_id_plug_0",
            "world_name_plug_0",
            "next_upgrade_plug_item_id",
            "next_upgrade_plug_name",
            "prerequisites",
            "screenshots",
            "costs",
            "enabled",
            "last_seen_in",
        ],
        enhancement_rows(),
    )

    sky_columns = [
        "jabbithole_sky_id",
        "housing_wallpaper_info_id",
        "jabbithole_sky_name",
        "slug",
        "sky_type",
        "currency_id",
        "cost",
        "enabled",
    ]
    sky_query = """
SELECT
    id,
    game_id,
    name,
    slug,
    etype,
    currency_id,
    cost,
    enabled
FROM housing_skies
ORDER BY game_id, id
"""

    def sky_rows():
        for row in mysql_rows(args, sky_query, sky_columns):
            client_wallpaper = client["housing_wallpapers"].get(to_int(row.get("housing_wallpaper_info_id"), -1) or -1, {})
            row["client_wallpaper_name"] = client_wallpaper.get("housingWallpaperName", "")
            row["client_world_sky_id"] = client_wallpaper.get("worldSkyId", "")
            row["client_cost"] = client_wallpaper.get("cost", "")
            row["client_currency_type_id"] = client_wallpaper.get("costCurrencyTypeId", "")
            row["match_status"] = "matched" if client_wallpaper else "unmatched"
            yield row

    counts["housing_sky_map.csv"] = write_csv(
        args.output_dir / "housing_sky_map.csv",
        [
            "jabbithole_sky_id",
            "housing_wallpaper_info_id",
            "jabbithole_sky_name",
            "client_wallpaper_name",
            "match_status",
            "slug",
            "sky_type",
            "currency_id",
            "cost",
            "client_currency_type_id",
            "client_cost",
            "client_world_sky_id",
            "enabled",
        ],
        sky_rows(),
    )

    dye_columns = [
        "jabbithole_dye_id",
        "dye_color_ramp_id",
        "jabbithole_dye_name",
        "slug",
        "ramp_id",
        "multiplier",
        "enabled",
        "first_seen_in",
    ]
    dye_query = """
SELECT
    id,
    game_id,
    name,
    slug,
    ramp_id,
    multiplier,
    enabled,
    first_seen_in
FROM dyes
ORDER BY game_id, id
"""

    def dye_rows():
        for row in mysql_rows(args, dye_query, dye_columns):
            client_dye = client["dye_color_ramps"].get(to_int(row.get("dye_color_ramp_id"), -1) or -1, {})
            row["client_dye_name"] = client_dye.get("dyeName", "")
            row["client_ramp_index"] = client_dye.get("rampIndex", "")
            row["client_cost_multiplier"] = client_dye.get("costMultiplier", "")
            row["match_status"] = "matched" if client_dye else "unmatched"
            yield row

    counts["dye_map.csv"] = write_csv(
        args.output_dir / "dye_map.csv",
        [
            "jabbithole_dye_id",
            "dye_color_ramp_id",
            "jabbithole_dye_name",
            "client_dye_name",
            "match_status",
            "slug",
            "ramp_id",
            "client_ramp_index",
            "multiplier",
            "client_cost_multiplier",
            "enabled",
            "first_seen_in",
        ],
        dye_rows(),
    )

    flair_columns = [
        "jabbithole_flair_id",
        "pet_flair_id",
        "jabbithole_flair_name",
        "slug",
        "flair_type",
        "game_spell4_id",
        "jabbithole_spell_id",
        "jabbithole_spell_name",
        "description",
        "enabled",
        "first_seen_in",
    ]
    flair_query = """
SELECT
    f.id,
    f.game_id,
    f.name,
    f.slug,
    f.flair_type,
    f.game_spell_id,
    f.spell_id,
    s.name,
    f.description,
    f.enabled,
    f.first_seen_in
FROM flairs f
LEFT JOIN spells s ON s.id = f.spell_id
ORDER BY f.game_id, f.id
"""

    def flair_rows():
        for row in mysql_rows(args, limited(flair_query, args.limit_relation_rows), flair_columns):
            client_flair = client["pet_flairs"].get(to_int(row.get("pet_flair_id"), -1) or -1, {})
            row["client_flair_tooltip"] = client_flair.get("petFlairTooltip", "")
            row["client_spell4_id"] = client_flair.get("spell4Id", "")
            row["client_spell_name"] = spell_name(client, row.get("client_spell4_id") or row.get("game_spell4_id"))
            row["match_status"] = "matched" if client_flair else "unmatched"
            yield row

    counts["flair_map.csv"] = write_csv(
        args.output_dir / "flair_map.csv",
        [
            "jabbithole_flair_id",
            "pet_flair_id",
            "jabbithole_flair_name",
            "client_flair_tooltip",
            "match_status",
            "slug",
            "flair_type",
            "game_spell4_id",
            "client_spell4_id",
            "jabbithole_spell_id",
            "jabbithole_spell_name",
            "client_spell_name",
            "description",
            "enabled",
            "first_seen_in",
        ],
        flair_rows(),
    )

    mount_columns = [
        "jabbithole_mount_id",
        "base_spell4_id",
        "mount_name",
        "description",
        "slug",
        "can_customize",
        "is_hoverboard",
        "is_rental_mount",
        "preview_creature2_id",
        "preview_hoverboard_item2_id",
        "secondary_bars",
        "summon_spell4_id",
        "jabbithole_spell_id",
        "enabled",
        "first_seen_in",
    ]
    mount_query = """
SELECT
    id,
    game_id,
    name,
    description,
    slug,
    can_customize,
    is_hoverboard,
    is_rental_mount,
    game_preview_creature_id,
    game_preview_hoverboard_item_id,
    can_use_on_secondary_bars,
    game_spell_id,
    spell_id,
    enabled,
    first_seen_in
FROM mounts
ORDER BY game_id, id
"""

    def mount_rows():
        for row in mysql_rows(args, limited(mount_query, args.limit_relation_rows), mount_columns):
            row["base_spell_name"] = lookup_name(client["spell_bases"], row.get("base_spell4_id"), "spellBaseName")
            row["summon_spell_name"] = spell_name(client, row.get("summon_spell4_id"))
            row["preview_creature_name"] = lookup_name(client["creatures"], row.get("preview_creature2_id"), "clientName")
            row["preview_hoverboard_item_name"] = item_name(client["items"], row.get("preview_hoverboard_item2_id"))
            row["match_status"] = "matched" if row["summon_spell_name"] else "unmatched_spell"
            yield row

    counts["mount_map.csv"] = write_csv(
        args.output_dir / "mount_map.csv",
        [
            "jabbithole_mount_id",
            "base_spell4_id",
            "base_spell_name",
            "mount_name",
            "description",
            "slug",
            "match_status",
            "can_customize",
            "is_hoverboard",
            "is_rental_mount",
            "preview_creature2_id",
            "preview_creature_name",
            "preview_hoverboard_item2_id",
            "preview_hoverboard_item_name",
            "secondary_bars",
            "summon_spell4_id",
            "summon_spell_name",
            "jabbithole_spell_id",
            "enabled",
            "first_seen_in",
        ],
        mount_rows(),
    )

    pet_columns = [
        "jabbithole_pet_id",
        "base_spell4_id",
        "pet_name",
        "description",
        "slug",
        "preview_creature2_id",
        "secondary_bars",
        "summon_spell4_id",
        "jabbithole_spell_id",
        "enabled",
        "first_seen_in",
    ]
    pet_query = """
SELECT
    id,
    game_id,
    name,
    description,
    slug,
    game_preview_creature_id,
    can_use_on_secondary_bars,
    game_spell_id,
    spell_id,
    enabled,
    first_seen_in
FROM pets
ORDER BY game_id, id
"""

    def pet_rows():
        for row in mysql_rows(args, limited(pet_query, args.limit_relation_rows), pet_columns):
            row["base_spell_name"] = lookup_name(client["spell_bases"], row.get("base_spell4_id"), "spellBaseName")
            row["summon_spell_name"] = spell_name(client, row.get("summon_spell4_id"))
            row["preview_creature_name"] = lookup_name(client["creatures"], row.get("preview_creature2_id"), "clientName")
            row["match_status"] = "matched" if row["summon_spell_name"] else "unmatched_spell"
            yield row

    counts["pet_map.csv"] = write_csv(
        args.output_dir / "pet_map.csv",
        [
            "jabbithole_pet_id",
            "base_spell4_id",
            "base_spell_name",
            "pet_name",
            "description",
            "slug",
            "match_status",
            "preview_creature2_id",
            "preview_creature_name",
            "secondary_bars",
            "summon_spell4_id",
            "summon_spell_name",
            "jabbithole_spell_id",
            "enabled",
            "first_seen_in",
        ],
        pet_rows(),
    )

    title_columns = [
        "character_title_id",
        "title_name",
        "slug",
        "title_group",
        "title_format",
        "jabbithole_path_unlock_id",
        "achievement_titles_count",
        "path_unlocks_count",
        "is_dominion",
        "is_exile",
        "enabled",
        "first_seen_in",
        "last_seen_in",
    ]
    title_query = """
SELECT
    id,
    name,
    slug,
    `group`,
    format,
    path_unlock_id,
    achievement_titles_count,
    path_unlocks_count,
    is_dominion,
    is_exile,
    enabled,
    first_seen_in,
    last_seen_in
FROM titles
ORDER BY id
"""

    def title_rows():
        for row in mysql_rows(args, title_query, title_columns):
            client_title = client["character_titles"].get(to_int(row.get("character_title_id"), -1) or -1, {})
            row["client_title_name"] = client_title.get("characterTitleName", "")
            row["client_title_text"] = client_title.get("characterTitleText", "")
            row["client_title_category_id"] = client_title.get("characterTitleCategoryId", "")
            row["client_title_category_name"] = lookup_name(client["character_title_categories"], row.get("client_title_category_id"), "characterTitleCategoryName")
            row["match_status"] = "matched" if client_title else "unmatched"
            yield row

    counts["title_map.csv"] = write_csv(
        args.output_dir / "title_map.csv",
        [
            "character_title_id",
            "title_name",
            "client_title_name",
            "client_title_text",
            "match_status",
            "slug",
            "title_group",
            "title_format",
            "client_title_category_id",
            "client_title_category_name",
            "jabbithole_path_unlock_id",
            "achievement_titles_count",
            "path_unlocks_count",
            "is_dominion",
            "is_exile",
            "enabled",
            "first_seen_in",
            "last_seen_in",
        ],
        title_rows(),
    )

    return counts


def write_tradeskill_schematic_maps(args: argparse.Namespace, client) -> Dict[str, int]:
    counts: Dict[str, int] = {}

    def is_real_id(value) -> bool:
        value_int = to_int(value)
        return value_int is not None and value_int != 0

    def joined_ids(values: Iterable[object]) -> str:
        return ";".join(str(to_int(value)) for value in values if is_real_id(value))

    def joined_names(rows: Dict[int, Dict[str, object]], values: Iterable[object], field: str) -> str:
        names = [lookup_name(rows, value, field) for value in values if is_real_id(value)]
        return ";".join(name for name in names if name)

    def schematic_name(schematic_id) -> str:
        return lookup_name(client["tradeskill_schematics"], schematic_id, "tradeskillSchematicName")

    def bonus_name(bonus_id) -> str:
        return lookup_name(client["tradeskill_bonuses"], bonus_id, "tradeskillBonusName")

    def status_from_parts(*matched: bool) -> str:
        if all(matched):
            return "matched"
        if any(matched):
            return "partial"
        return "unmatched"

    talent_tiers_by_trade_points: Dict[Tuple[int, int], List[Dict[str, object]]] = defaultdict(list)
    for row in client["tradeskill_talent_tiers"].values():
        trade_skill_id = to_int(row.get("tradeSkillId"))
        points = to_int(row.get("pointsToUnlock"))
        if trade_skill_id is not None and points is not None:
            talent_tiers_by_trade_points[(trade_skill_id, points)].append(row)

    counts["tradeskill_client_map.csv"] = write_csv(
        args.output_dir / "tradeskill_client_map.csv",
        [
            "trade_skill_id",
            "tradeskill_name",
            "description",
            "flags",
            "tutorial_id",
            "achievement_category_id",
            "achievement_category_name",
            "max_additives",
            "axis_name_0",
            "axis_name_1",
            "axis_name_2",
            "axis_name_3",
        ],
        (
            {
                "trade_skill_id": tradeskill_id,
                "tradeskill_name": row.get("tradeskillName", ""),
                "description": row.get("tradeskillDescription", ""),
                "flags": row.get("flags", ""),
                "tutorial_id": row.get("tutorialId", ""),
                "achievement_category_id": row.get("achievementCategoryId", ""),
                "achievement_category_name": lookup_name(client["achievement_categories"], row.get("achievementCategoryId"), "achievementCategoryName"),
                "max_additives": row.get("maxAdditives", ""),
                "axis_name_0": row.get("tradeskillAxisName0", ""),
                "axis_name_1": row.get("tradeskillAxisName1", ""),
                "axis_name_2": row.get("tradeskillAxisName2", ""),
                "axis_name_3": row.get("tradeskillAxisName3", ""),
            }
            for tradeskill_id, row in sorted(client["tradeskills"].items())
        ),
    )

    def client_schematic_rows():
        for schematic_id, row in sorted(client["tradeskill_schematics"].items()):
            material_ids = [row.get(f"item2IdMaterial{i:02d}") for i in range(5)]
            material_costs = [row.get(f"materialCost{i:02d}") for i in range(5)]
            material_parts = []
            for item_id, cost in zip(material_ids, material_costs):
                if is_real_id(item_id):
                    material_parts.append(f"{item_id}:{item_name(client['items'], item_id)}x{cost}")
            yield {
                "tradeskill_schematic2_id": schematic_id,
                "schematic_name": row.get("tradeskillSchematicName", ""),
                "trade_skill_id": row.get("tradeSkillId", ""),
                "tradeskill_name": lookup_name(client["tradeskills"], row.get("tradeSkillId"), "tradeskillName"),
                "item2_id_output": row.get("item2IdOutput", ""),
                "output_item_name": item_name(client["items"], row.get("item2IdOutput")),
                "item2_id_output_fail": row.get("item2IdOutputFail", ""),
                "output_fail_item_name": item_name(client["items"], row.get("item2IdOutputFail")),
                "output_count": row.get("outputCount", ""),
                "loot_id": row.get("lootId", ""),
                "tier": row.get("tier", ""),
                "flags": row.get("flags", ""),
                "materials": ";".join(material_parts),
                "parent_schematic2_id": row.get("tradeskillSchematic2IdParent", ""),
                "parent_schematic_name": schematic_name(row.get("tradeskillSchematic2IdParent")),
                "vector_x": row.get("vectorX", ""),
                "vector_y": row.get("vectorY", ""),
                "radius": row.get("radius", ""),
                "crit_radius": row.get("critRadius", ""),
                "crit_output_item2_id": row.get("item2IdOutputCrit", ""),
                "crit_output_item_name": item_name(client["items"], row.get("item2IdOutputCrit")),
                "crit_bonus_count": row.get("outputCountCritBonus", ""),
                "priority": row.get("priority", ""),
                "max_additives": row.get("maxAdditives", ""),
                "discoverable_quadrant": row.get("discoverableQuadrant", ""),
                "discoverable_radius": row.get("discoverableRadius", ""),
                "discoverable_angle": row.get("discoverableAngle", ""),
                "catalyst_ordering_id": row.get("tradeskillCatalystOrderingId", ""),
            }

    counts["tradeskill_schematic_client_map.csv"] = write_csv(
        args.output_dir / "tradeskill_schematic_client_map.csv",
        [
            "tradeskill_schematic2_id",
            "schematic_name",
            "trade_skill_id",
            "tradeskill_name",
            "item2_id_output",
            "output_item_name",
            "item2_id_output_fail",
            "output_fail_item_name",
            "output_count",
            "loot_id",
            "tier",
            "flags",
            "materials",
            "parent_schematic2_id",
            "parent_schematic_name",
            "vector_x",
            "vector_y",
            "radius",
            "crit_radius",
            "crit_output_item2_id",
            "crit_output_item_name",
            "crit_bonus_count",
            "priority",
            "max_additives",
            "discoverable_quadrant",
            "discoverable_radius",
            "discoverable_angle",
            "catalyst_ordering_id",
        ],
        client_schematic_rows(),
    )

    counts["tradeskill_material_client_map.csv"] = write_csv(
        args.output_dir / "tradeskill_material_client_map.csv",
        [
            "tradeskill_material_id",
            "item2_id",
            "item_name",
            "stat_revolution_item2_id",
            "stat_revolution_item_name",
            "display_index",
            "material_category_id",
            "material_category_name",
        ],
        (
            {
                "tradeskill_material_id": material_id,
                "item2_id": row.get("item2Id", ""),
                "item_name": item_name(client["items"], row.get("item2Id")),
                "stat_revolution_item2_id": row.get("item2IdStatRevolution", ""),
                "stat_revolution_item_name": item_name(client["items"], row.get("item2IdStatRevolution")),
                "display_index": row.get("displayIndex", ""),
                "material_category_id": row.get("tradeskillMaterialCategoryId", ""),
                "material_category_name": lookup_name(
                    client["tradeskill_material_categories"],
                    row.get("tradeskillMaterialCategoryId"),
                    "tradeskillMaterialCategoryName",
                ),
            }
            for material_id, row in sorted(client["tradeskill_materials"].items())
        ),
    )

    counts["tradeskill_material_category_client_map.csv"] = write_csv(
        args.output_dir / "tradeskill_material_category_client_map.csv",
        ["tradeskill_material_category_id", "material_category_name"],
        (
            {
                "tradeskill_material_category_id": category_id,
                "material_category_name": row.get("tradeskillMaterialCategoryName", ""),
            }
            for category_id, row in sorted(client["tradeskill_material_categories"].items())
        ),
    )

    counts["tradeskill_talent_tier_client_map.csv"] = write_csv(
        args.output_dir / "tradeskill_talent_tier_client_map.csv",
        [
            "tradeskill_talent_tier_id",
            "trade_skill_id",
            "tradeskill_name",
            "points_to_unlock",
            "respec_cost",
            "bonus_ids",
            "bonus_names",
        ],
        (
            {
                "tradeskill_talent_tier_id": tier_id,
                "trade_skill_id": row.get("tradeSkillId", ""),
                "tradeskill_name": lookup_name(client["tradeskills"], row.get("tradeSkillId"), "tradeskillName"),
                "points_to_unlock": row.get("pointsToUnlock", ""),
                "respec_cost": row.get("respecCost", ""),
                "bonus_ids": joined_ids(row.get(f"tradeSkillBonusId{i:02d}") for i in range(5)),
                "bonus_names": joined_names(client["tradeskill_bonuses"], (row.get(f"tradeSkillBonusId{i:02d}") for i in range(5)), "tradeskillBonusName"),
            }
            for tier_id, row in sorted(client["tradeskill_talent_tiers"].items())
        ),
    )

    counts["tradeskill_tier_client_map.csv"] = write_csv(
        args.output_dir / "tradeskill_tier_client_map.csv",
        [
            "tradeskill_tier_id",
            "trade_skill_id",
            "tradeskill_name",
            "tier",
            "required_xp",
            "learn_xp",
            "craft_xp",
            "first_craft_xp",
            "quest_xp",
            "fail_xp",
            "item_level_min",
            "max_additives",
            "relearn_cost",
            "achievement_category_id",
            "achievement_category_name",
        ],
        (
            {
                "tradeskill_tier_id": tier_id,
                "trade_skill_id": row.get("tradeSkillId", ""),
                "tradeskill_name": lookup_name(client["tradeskills"], row.get("tradeSkillId"), "tradeskillName"),
                "tier": row.get("tier", ""),
                "required_xp": row.get("requiredXp", ""),
                "learn_xp": row.get("learnXp", ""),
                "craft_xp": row.get("craftXp", ""),
                "first_craft_xp": row.get("firstCraftXp", ""),
                "quest_xp": row.get("questXp", ""),
                "fail_xp": row.get("failXp", ""),
                "item_level_min": row.get("itemLevelMin", ""),
                "max_additives": row.get("maxAdditives", ""),
                "relearn_cost": row.get("relearnCost", ""),
                "achievement_category_id": row.get("achievementCategoryId", ""),
                "achievement_category_name": lookup_name(client["achievement_categories"], row.get("achievementCategoryId"), "achievementCategoryName"),
            }
            for tier_id, row in sorted(client["tradeskill_tiers"].items())
        ),
    )

    counts["tradeskill_additive_client_map.csv"] = write_csv(
        args.output_dir / "tradeskill_additive_client_map.csv",
        ["tradeskill_additive_id", "trade_skill_id", "tradeskill_name", "tier", "vector_x", "vector_y", "radius"],
        (
            {
                "tradeskill_additive_id": additive_id,
                "trade_skill_id": row.get("tradeSkillId", ""),
                "tradeskill_name": lookup_name(client["tradeskills"], row.get("tradeSkillId"), "tradeskillName"),
                "tier": row.get("tier", ""),
                "vector_x": row.get("vectorX", ""),
                "vector_y": row.get("vectorY", ""),
                "radius": row.get("radius", ""),
            }
            for additive_id, row in sorted(client["tradeskill_additives"].items())
        ),
    )

    counts["tradeskill_catalyst_client_map.csv"] = write_csv(
        args.output_dir / "tradeskill_catalyst_client_map.csv",
        [
            "tradeskill_catalyst_id",
            "trade_skill_id",
            "tradeskill_name",
            "tier",
            "catalyst_enums",
            "values",
        ],
        (
            {
                "tradeskill_catalyst_id": catalyst_id,
                "trade_skill_id": row.get("tradeSkillId", ""),
                "tradeskill_name": lookup_name(client["tradeskills"], row.get("tradeSkillId"), "tradeskillName"),
                "tier": row.get("tier", ""),
                "catalyst_enums": ";".join(str(row.get(f"tradeskillCatalystEnum{i:02d}", "")) for i in range(5)),
                "values": ";".join(str(row.get(f"value{i:02d}", "")) for i in range(5)),
            }
            for catalyst_id, row in sorted(client["tradeskill_catalysts"].items())
        ),
    )

    counts["tradeskill_catalyst_ordering_client_map.csv"] = write_csv(
        args.output_dir / "tradeskill_catalyst_ordering_client_map.csv",
        ["tradeskill_catalyst_ordering_id", "unlock_levels"],
        (
            {
                "tradeskill_catalyst_ordering_id": ordering_id,
                "unlock_levels": ";".join(str(row.get(f"unlockLevel{i:02d}", "")) for i in range(5)),
            }
            for ordering_id, row in sorted(client["tradeskill_catalyst_ordering"].items())
        ),
    )

    counts["tradeskill_bonus_client_map.csv"] = write_csv(
        args.output_dir / "tradeskill_bonus_client_map.csv",
        [
            "tradeskill_bonus_id",
            "bonus_name",
            "tooltip",
            "tradeskill_tier_id",
            "achievement_id",
            "achievement_title",
            "icon_path",
            "bonus_enums",
            "primary_objects",
            "secondary_objects",
            "tertiary_objects",
            "values",
            "integer_values",
        ],
        (
            {
                "tradeskill_bonus_id": bonus_id,
                "bonus_name": row.get("tradeskillBonusName", ""),
                "tooltip": row.get("tradeskillBonusTooltip", ""),
                "tradeskill_tier_id": row.get("tradeSkillTierId", ""),
                "achievement_id": row.get("achievementId", ""),
                "achievement_title": lookup_name(client["achievements"], row.get("achievementId"), "achievementTitle"),
                "icon_path": row.get("iconPath", ""),
                "bonus_enums": ";".join(str(row.get(f"tradeskillBonusEnum{i:02d}", "")) for i in range(3)),
                "primary_objects": ";".join(str(row.get(f"objectIdPrimary{i:02d}", "")) for i in range(3)),
                "secondary_objects": ";".join(str(row.get(f"objectIdSecondary{i:02d}", "")) for i in range(3)),
                "tertiary_objects": ";".join(str(row.get(f"objectIdTertiary{i:02d}", "")) for i in range(3)),
                "values": ";".join(str(row.get(f"value{i:02d}", "")) for i in range(3)),
                "integer_values": ";".join(str(row.get(f"valueInt{i:02d}", "")) for i in range(3)),
            }
            for bonus_id, row in sorted(client["tradeskill_bonuses"].items())
        ),
    )

    counts["tradeskill_achievement_layout_client_map.csv"] = write_csv(
        args.output_dir / "tradeskill_achievement_layout_client_map.csv",
        [
            "tradeskill_achievement_layout_id",
            "achievement_id",
            "achievement_title",
            "parent_achievement_ids",
            "parent_achievement_titles",
            "grid_x",
            "grid_y",
        ],
        (
            {
                "tradeskill_achievement_layout_id": layout_id,
                "achievement_id": row.get("achievementId", ""),
                "achievement_title": lookup_name(client["achievements"], row.get("achievementId"), "achievementTitle"),
                "parent_achievement_ids": joined_ids(row.get(f"achievementIdParent{i:02d}") for i in range(5)),
                "parent_achievement_titles": joined_names(
                    client["achievements"],
                    (row.get(f"achievementIdParent{i:02d}") for i in range(5)),
                    "achievementTitle",
                ),
                "grid_x": row.get("gridX", ""),
                "grid_y": row.get("gridY", ""),
            }
            for layout_id, row in sorted(client["tradeskill_achievement_layouts"].items())
        ),
    )

    counts["tradeskill_achievement_reward_client_map.csv"] = write_csv(
        args.output_dir / "tradeskill_achievement_reward_client_map.csv",
        [
            "tradeskill_achievement_reward_id",
            "achievement_id",
            "achievement_title",
            "faction2_id",
            "faction_name",
            "faction_amount",
            "talent_points",
            "schematic_ids",
            "schematic_names",
        ],
        (
            {
                "tradeskill_achievement_reward_id": reward_id,
                "achievement_id": row.get("achievementId", ""),
                "achievement_title": lookup_name(client["achievements"], row.get("achievementId"), "achievementTitle"),
                "faction2_id": row.get("faction2Id", ""),
                "faction_name": lookup_name(client["factions"], row.get("faction2Id"), "factionName"),
                "faction_amount": row.get("factionIdAmount", ""),
                "talent_points": row.get("talentPoints", ""),
                "schematic_ids": joined_ids(row.get(f"tradeSkillSchematicId{i:02d}") for i in range(8)),
                "schematic_names": joined_names(
                    client["tradeskill_schematics"],
                    (row.get(f"tradeSkillSchematicId{i:02d}") for i in range(8)),
                    "tradeskillSchematicName",
                ),
            }
            for reward_id, row in sorted(client["tradeskill_achievement_rewards"].items())
        ),
    )

    schematic_columns = [
        "jabbithole_schematic_id",
        "tradeskill_schematic2_id",
        "jabbithole_schematic_name",
        "slug",
        "jabbithole_tradeskill_id",
        "trade_skill_id",
        "jabbithole_tradeskill_name",
        "jabbithole_output_item_id",
        "parent_jabbithole_schematic_id",
        "client_parent_schematic2_id",
        "taught_by_item_id",
        "tier",
        "xp_learn",
        "xp_craft",
        "xp_fail",
        "create_count",
        "create_crit_count",
        "max_additives",
        "max_catalysts",
        "sockets",
        "schematic_materials_count",
        "child_schematics_count",
        "techtree_schematics_count",
        "is_autocraft",
        "is_autolearn",
        "is_oneuse",
        "slots_resistor",
        "slots_capacitor",
        "slots_inductor",
        "slots_rnd_spec",
        "slots_lck_spec",
        "slots_rnd_attr",
        "slots_lck_attr",
        "schematic_microchips_count",
        "schematic_circuits_count",
        "material_cache",
        "is_universal",
        "stats",
        "enabled",
        "first_seen_in",
        "last_seen_in",
    ]
    schematic_query = """
SELECT
    s.id,
    s.game_id,
    s.name,
    s.slug,
    s.tradeskill_id,
    ts.game_id,
    ts.name,
    s.item_id,
    s.parent_schematic_id,
    ps.game_id,
    s.taught_by_id,
    s.tier,
    s.xp_learn,
    s.xp_craft,
    s.xp_fail,
    s.create_count,
    s.create_crit_count,
    s.max_additives,
    s.max_catalysts,
    s.sockets,
    s.schematic_materials_count,
    s.child_schematics_count,
    s.tradeskill_techtree_schematics_count,
    s.is_autocraft,
    s.is_autolearn,
    s.is_oneuse,
    s.slots_resistor,
    s.slots_capacitor,
    s.slots_inductor,
    s.slots_rnd_spec,
    s.slots_lck_spec,
    s.slots_rnd_attr,
    s.slots_lck_attr,
    s.schematic_microchips_count,
    s.schematic_circuits_count,
    s.material_cache,
    s.is_universal,
    s.stats,
    s.enabled,
    s.first_seen_in,
    s.last_seen_in
FROM schematics s
LEFT JOIN tradeskills ts ON ts.id = s.tradeskill_id
LEFT JOIN schematics ps ON ps.id = s.parent_schematic_id
ORDER BY s.game_id, s.id
"""

    def schematic_rows():
        for row in mysql_rows(args, limited(schematic_query, args.limit_relation_rows), schematic_columns):
            client_schematic = client["tradeskill_schematics"].get(to_int(row.get("tradeskill_schematic2_id"), -1) or -1, {})
            row["client_schematic_name"] = client_schematic.get("tradeskillSchematicName", "")
            row["client_trade_skill_id"] = client_schematic.get("tradeSkillId", "")
            row["client_tradeskill_name"] = lookup_name(client["tradeskills"], row.get("client_trade_skill_id") or row.get("trade_skill_id"), "tradeskillName")
            row["jabbithole_output_item_name"] = item_name(client["items"], row.get("jabbithole_output_item_id"))
            row["client_output_item2_id"] = client_schematic.get("item2IdOutput", "")
            row["client_output_item_name"] = item_name(client["items"], row.get("client_output_item2_id"))
            row["client_fail_item2_id"] = client_schematic.get("item2IdOutputFail", "")
            row["client_fail_item_name"] = item_name(client["items"], row.get("client_fail_item2_id"))
            row["client_parent_schematic_name"] = schematic_name(row.get("client_parent_schematic2_id") or client_schematic.get("tradeskillSchematic2IdParent"))
            row["taught_by_item_name"] = item_name(client["items"], row.get("taught_by_item_id"))
            row["match_status"] = "matched" if client_schematic else "unmatched"
            yield row

    counts["schematic_map.csv"] = write_csv(
        args.output_dir / "schematic_map.csv",
        [
            "jabbithole_schematic_id",
            "tradeskill_schematic2_id",
            "jabbithole_schematic_name",
            "client_schematic_name",
            "match_status",
            "slug",
            "jabbithole_tradeskill_id",
            "trade_skill_id",
            "jabbithole_tradeskill_name",
            "client_trade_skill_id",
            "client_tradeskill_name",
            "jabbithole_output_item_id",
            "jabbithole_output_item_name",
            "client_output_item2_id",
            "client_output_item_name",
            "client_fail_item2_id",
            "client_fail_item_name",
            "parent_jabbithole_schematic_id",
            "client_parent_schematic2_id",
            "client_parent_schematic_name",
            "taught_by_item_id",
            "taught_by_item_name",
            "tier",
            "xp_learn",
            "xp_craft",
            "xp_fail",
            "create_count",
            "create_crit_count",
            "max_additives",
            "max_catalysts",
            "sockets",
            "schematic_materials_count",
            "child_schematics_count",
            "techtree_schematics_count",
            "is_autocraft",
            "is_autolearn",
            "is_oneuse",
            "slots_resistor",
            "slots_capacitor",
            "slots_inductor",
            "slots_rnd_spec",
            "slots_lck_spec",
            "slots_rnd_attr",
            "slots_lck_attr",
            "schematic_microchips_count",
            "schematic_circuits_count",
            "material_cache",
            "is_universal",
            "stats",
            "enabled",
            "first_seen_in",
            "last_seen_in",
        ],
        schematic_rows(),
    )

    material_columns = [
        "source_relation_id",
        "jabbithole_schematic_id",
        "tradeskill_schematic2_id",
        "jabbithole_schematic_name",
        "jabbithole_tradeskill_id",
        "trade_skill_id",
        "jabbithole_tradeskill_name",
        "item2_id",
        "amount",
        "enabled",
        "last_seen_in",
    ]
    material_query = """
SELECT
    sm.id,
    sm.schematic_id,
    s.game_id,
    s.name,
    s.tradeskill_id,
    ts.game_id,
    ts.name,
    sm.item_id,
    sm.amount,
    sm.enabled,
    sm.last_seen_in
FROM schematic_materials sm
LEFT JOIN schematics s ON s.id = sm.schematic_id
LEFT JOIN tradeskills ts ON ts.id = s.tradeskill_id
ORDER BY sm.schematic_id, sm.id
"""

    def material_rows():
        for row in mysql_rows(args, limited(material_query, args.limit_relation_rows), material_columns):
            client_schematic = client["tradeskill_schematics"].get(to_int(row.get("tradeskill_schematic2_id"), -1) or -1, {})
            row["client_schematic_name"] = client_schematic.get("tradeskillSchematicName", "")
            row["client_tradeskill_name"] = lookup_name(client["tradeskills"], row.get("trade_skill_id"), "tradeskillName")
            row["item_name"] = item_name(client["items"], row.get("item2_id"))
            row["schematic_match_status"] = "matched" if client_schematic else "unmatched"
            row["item_match_status"] = "matched" if row["item_name"] else "unmatched"
            row["match_status"] = status_from_parts(bool(client_schematic), bool(row["item_name"]))
            yield row

    counts["schematic_material_map.csv"] = write_csv(
        args.output_dir / "schematic_material_map.csv",
        [
            "source_relation_id",
            "jabbithole_schematic_id",
            "tradeskill_schematic2_id",
            "jabbithole_schematic_name",
            "client_schematic_name",
            "jabbithole_tradeskill_id",
            "trade_skill_id",
            "jabbithole_tradeskill_name",
            "client_tradeskill_name",
            "item2_id",
            "item_name",
            "amount",
            "match_status",
            "schematic_match_status",
            "item_match_status",
            "enabled",
            "last_seen_in",
        ],
        material_rows(),
    )

    def write_schematic_component_map(table_name: str, output_name: str, component_label: str) -> int:
        component_columns = [
            "source_relation_id",
            "jabbithole_schematic_id",
            "tradeskill_schematic2_id",
            "jabbithole_schematic_name",
            "jabbithole_tradeskill_id",
            "trade_skill_id",
            "jabbithole_tradeskill_name",
            "item2_id",
            "origin_create",
            "last_seen_in",
        ]
        component_query = f"""
SELECT
    sc.id,
    sc.schematic_id,
    s.game_id,
    s.name,
    s.tradeskill_id,
    ts.game_id,
    ts.name,
    sc.item_id,
    sc.origin_create,
    sc.last_seen_in
FROM {table_name} sc
LEFT JOIN schematics s ON s.id = sc.schematic_id
LEFT JOIN tradeskills ts ON ts.id = s.tradeskill_id
ORDER BY sc.schematic_id, sc.id
"""

        def component_rows():
            for row in mysql_rows(args, limited(component_query, args.limit_relation_rows), component_columns):
                client_schematic = client["tradeskill_schematics"].get(to_int(row.get("tradeskill_schematic2_id"), -1) or -1, {})
                row["component_type"] = component_label
                row["client_schematic_name"] = client_schematic.get("tradeskillSchematicName", "")
                row["client_tradeskill_name"] = lookup_name(client["tradeskills"], row.get("trade_skill_id"), "tradeskillName")
                row["item_name"] = item_name(client["items"], row.get("item2_id"))
                row["schematic_match_status"] = "matched" if client_schematic else "unmatched"
                row["item_match_status"] = "matched" if row["item_name"] else "unmatched"
                row["match_status"] = status_from_parts(bool(client_schematic), bool(row["item_name"]))
                yield row

        return write_csv(
            args.output_dir / output_name,
            [
                "source_relation_id",
                "component_type",
                "jabbithole_schematic_id",
                "tradeskill_schematic2_id",
                "jabbithole_schematic_name",
                "client_schematic_name",
                "jabbithole_tradeskill_id",
                "trade_skill_id",
                "jabbithole_tradeskill_name",
                "client_tradeskill_name",
                "item2_id",
                "item_name",
                "match_status",
                "schematic_match_status",
                "item_match_status",
                "origin_create",
                "last_seen_in",
            ],
            component_rows(),
        )

    counts["schematic_circuit_map.csv"] = write_schematic_component_map("schematic_circuits", "schematic_circuit_map.csv", "circuit")
    counts["schematic_microchip_map.csv"] = write_schematic_component_map("schematic_microchips", "schematic_microchip_map.csv", "microchip")

    tier_columns = [
        "jabbithole_talent_tier_id",
        "jabbithole_tradeskill_id",
        "trade_skill_id",
        "jabbithole_tradeskill_name",
        "level",
        "points_required",
        "tradeskill_talents_count",
        "enabled",
    ]
    tier_query = """
SELECT
    tt.id,
    tt.tradeskill_id,
    ts.game_id,
    ts.name,
    tt.level,
    tt.points_required,
    tt.tradeskill_talents_count,
    tt.enabled
FROM tradeskill_talent_tiers tt
LEFT JOIN tradeskills ts ON ts.id = tt.tradeskill_id
ORDER BY ts.game_id, tt.level, tt.id
"""

    def talent_tier_rows():
        for row in mysql_rows(args, tier_query, tier_columns):
            matches = talent_tiers_by_trade_points.get((to_int(row.get("trade_skill_id"), -1) or -1, to_int(row.get("points_required"), -1) or -1), [])
            client_tier = matches[0] if matches else {}
            row["client_tradeskill_talent_tier_id"] = client_tier.get("ID", "")
            row["client_tradeskill_name"] = lookup_name(client["tradeskills"], row.get("trade_skill_id"), "tradeskillName")
            row["client_points_to_unlock"] = client_tier.get("pointsToUnlock", "")
            row["client_respec_cost"] = client_tier.get("respecCost", "")
            row["client_bonus_ids"] = joined_ids(client_tier.get(f"tradeSkillBonusId{i:02d}") for i in range(5)) if client_tier else ""
            row["client_bonus_names"] = joined_names(
                client["tradeskill_bonuses"],
                (client_tier.get(f"tradeSkillBonusId{i:02d}") for i in range(5)),
                "tradeskillBonusName",
            ) if client_tier else ""
            row["match_status"] = "ambiguous" if len(matches) > 1 else ("matched" if client_tier else "unmatched")
            yield row

    counts["tradeskill_talent_tier_map.csv"] = write_csv(
        args.output_dir / "tradeskill_talent_tier_map.csv",
        [
            "jabbithole_talent_tier_id",
            "client_tradeskill_talent_tier_id",
            "jabbithole_tradeskill_id",
            "trade_skill_id",
            "jabbithole_tradeskill_name",
            "client_tradeskill_name",
            "level",
            "points_required",
            "client_points_to_unlock",
            "client_respec_cost",
            "client_bonus_ids",
            "client_bonus_names",
            "tradeskill_talents_count",
            "match_status",
            "enabled",
        ],
        talent_tier_rows(),
    )

    talent_columns = [
        "jabbithole_talent_id",
        "tradeskill_bonus_id",
        "jabbithole_talent_name",
        "icon",
        "description",
        "talent_order",
        "enabled",
        "jabbithole_talent_tier_id",
        "level",
        "points_required",
        "jabbithole_tradeskill_id",
        "trade_skill_id",
        "jabbithole_tradeskill_name",
    ]
    talent_query = """
SELECT
    t.id,
    t.game_id,
    t.name,
    t.icon,
    t.description,
    t.talent_order,
    t.enabled,
    tt.id,
    tt.level,
    tt.points_required,
    ts.id,
    ts.game_id,
    ts.name
FROM tradeskill_talents t
LEFT JOIN tradeskill_talent_tiers tt ON tt.id = t.tradeskill_talent_tier_id
LEFT JOIN tradeskills ts ON ts.id = tt.tradeskill_id
ORDER BY ts.game_id, tt.level, t.talent_order, t.id
"""

    def talent_rows():
        for row in mysql_rows(args, talent_query, talent_columns):
            client_bonus = client["tradeskill_bonuses"].get(to_int(row.get("tradeskill_bonus_id"), -1) or -1, {})
            matches = talent_tiers_by_trade_points.get((to_int(row.get("trade_skill_id"), -1) or -1, to_int(row.get("points_required"), -1) or -1), [])
            client_tier = matches[0] if matches else {}
            row["client_bonus_name"] = client_bonus.get("tradeskillBonusName", "")
            row["client_bonus_tooltip"] = client_bonus.get("tradeskillBonusTooltip", "")
            row["client_icon"] = client_bonus.get("iconPath", "")
            row["client_achievement_id"] = client_bonus.get("achievementId", "")
            row["client_achievement_title"] = lookup_name(client["achievements"], row.get("client_achievement_id"), "achievementTitle")
            row["client_tradeskill_talent_tier_id"] = client_tier.get("ID", "")
            row["client_tradeskill_name"] = lookup_name(client["tradeskills"], row.get("trade_skill_id"), "tradeskillName")
            row["bonus_enums"] = ";".join(str(client_bonus.get(f"tradeskillBonusEnum{i:02d}", "")) for i in range(3)) if client_bonus else ""
            row["primary_objects"] = ";".join(str(client_bonus.get(f"objectIdPrimary{i:02d}", "")) for i in range(3)) if client_bonus else ""
            row["secondary_objects"] = ";".join(str(client_bonus.get(f"objectIdSecondary{i:02d}", "")) for i in range(3)) if client_bonus else ""
            row["tertiary_objects"] = ";".join(str(client_bonus.get(f"objectIdTertiary{i:02d}", "")) for i in range(3)) if client_bonus else ""
            row["values"] = ";".join(str(client_bonus.get(f"value{i:02d}", "")) for i in range(3)) if client_bonus else ""
            row["integer_values"] = ";".join(str(client_bonus.get(f"valueInt{i:02d}", "")) for i in range(3)) if client_bonus else ""
            row["match_status"] = "matched" if client_bonus else "unmatched"
            yield row

    counts["tradeskill_talent_map.csv"] = write_csv(
        args.output_dir / "tradeskill_talent_map.csv",
        [
            "jabbithole_talent_id",
            "tradeskill_bonus_id",
            "jabbithole_talent_name",
            "client_bonus_name",
            "client_bonus_tooltip",
            "match_status",
            "jabbithole_talent_tier_id",
            "client_tradeskill_talent_tier_id",
            "jabbithole_tradeskill_id",
            "trade_skill_id",
            "jabbithole_tradeskill_name",
            "client_tradeskill_name",
            "level",
            "points_required",
            "talent_order",
            "icon",
            "client_icon",
            "description",
            "client_achievement_id",
            "client_achievement_title",
            "bonus_enums",
            "primary_objects",
            "secondary_objects",
            "tertiary_objects",
            "values",
            "integer_values",
            "enabled",
        ],
        talent_rows(),
    )

    group_columns = [
        "jabbithole_techtree_group_id",
        "achievement_category_id",
        "jabbithole_group_name",
        "jabbithole_tradeskill_id",
        "trade_skill_id",
        "jabbithole_tradeskill_name",
        "level",
        "techtree_items_count",
        "side",
        "enabled",
    ]
    group_query = """
SELECT
    g.id,
    g.game_id,
    g.name,
    g.tradeskill_id,
    ts.game_id,
    ts.name,
    g.level,
    g.tradeskill_techtree_items_count,
    g.side,
    g.enabled
FROM tradeskill_techtree_groups g
LEFT JOIN tradeskills ts ON ts.id = g.tradeskill_id
ORDER BY ts.game_id, g.level, g.side, g.id
"""

    def techtree_group_rows():
        for row in mysql_rows(args, group_query, group_columns):
            category = client["achievement_categories"].get(to_int(row.get("achievement_category_id"), -1) or -1, {})
            row["client_category_name"] = category.get("achievementCategoryName", "")
            row["client_category_full_name"] = category.get("achievementCategoryFullName", "")
            row["client_parent_category_id"] = category.get("achievementCategoryIdParent", "")
            row["client_parent_category_name"] = lookup_name(client["achievement_categories"], row.get("client_parent_category_id"), "achievementCategoryName")
            row["client_tradeskill_name"] = lookup_name(client["tradeskills"], row.get("trade_skill_id"), "tradeskillName")
            row["match_status"] = "matched" if category else "unmatched"
            yield row

    counts["tradeskill_techtree_group_map.csv"] = write_csv(
        args.output_dir / "tradeskill_techtree_group_map.csv",
        [
            "jabbithole_techtree_group_id",
            "achievement_category_id",
            "jabbithole_group_name",
            "client_category_name",
            "client_category_full_name",
            "match_status",
            "client_parent_category_id",
            "client_parent_category_name",
            "jabbithole_tradeskill_id",
            "trade_skill_id",
            "jabbithole_tradeskill_name",
            "client_tradeskill_name",
            "level",
            "side",
            "techtree_items_count",
            "enabled",
        ],
        techtree_group_rows(),
    )

    item_columns = [
        "jabbithole_techtree_item_id",
        "achievement_id",
        "jabbithole_techtree_item_name",
        "progress_description",
        "description",
        "num_needed",
        "achievement_points",
        "location_parents",
        "location_x",
        "location_y",
        "bonuses",
        "bonus_talent_points",
        "checklist",
        "techtree_schematics_count",
        "enabled",
        "jabbithole_techtree_group_id",
        "achievement_category_id",
        "jabbithole_group_name",
        "jabbithole_tradeskill_id",
        "trade_skill_id",
        "jabbithole_tradeskill_name",
    ]
    item_query = """
SELECT
    i.id,
    i.game_id,
    i.name,
    i.progress_description,
    i.description,
    i.num_needed,
    i.achievement_points,
    i.location_parents,
    i.location_x,
    i.location_y,
    i.bonuses,
    i.bonus_talent_points,
    i.checklist,
    i.tradeskill_techtree_schematics_count,
    i.enabled,
    g.id,
    g.game_id,
    g.name,
    g.tradeskill_id,
    ts.game_id,
    ts.name
FROM tradeskill_techtree_items i
LEFT JOIN tradeskill_techtree_groups g ON g.id = i.tradeskill_techtree_group_id
LEFT JOIN tradeskills ts ON ts.id = g.tradeskill_id
ORDER BY ts.game_id, g.level, i.location_y, i.location_x, i.id
"""

    def techtree_item_rows():
        for row in mysql_rows(args, limited(item_query, args.limit_relation_rows), item_columns):
            achievement = client["achievements"].get(to_int(row.get("achievement_id"), -1) or -1, {})
            row["client_achievement_title"] = achievement.get("achievementTitle", "")
            row["client_achievement_description"] = achievement.get("achievementDescription", "")
            row["client_achievement_progress"] = achievement.get("achievementProgress", "")
            row["client_achievement_category_id"] = achievement.get("achievementCategoryId", "")
            row["client_achievement_category_name"] = lookup_name(client["achievement_categories"], row.get("client_achievement_category_id"), "achievementCategoryName")
            row["client_group_category_name"] = lookup_name(client["achievement_categories"], row.get("achievement_category_id"), "achievementCategoryName")
            row["client_tradeskill_name"] = lookup_name(client["tradeskills"], row.get("trade_skill_id"), "tradeskillName")
            row["match_status"] = "matched" if achievement else "unmatched"
            yield row

    counts["tradeskill_techtree_item_map.csv"] = write_csv(
        args.output_dir / "tradeskill_techtree_item_map.csv",
        [
            "jabbithole_techtree_item_id",
            "achievement_id",
            "jabbithole_techtree_item_name",
            "client_achievement_title",
            "match_status",
            "progress_description",
            "client_achievement_progress",
            "description",
            "client_achievement_description",
            "num_needed",
            "achievement_points",
            "client_achievement_category_id",
            "client_achievement_category_name",
            "jabbithole_techtree_group_id",
            "achievement_category_id",
            "jabbithole_group_name",
            "client_group_category_name",
            "jabbithole_tradeskill_id",
            "trade_skill_id",
            "jabbithole_tradeskill_name",
            "client_tradeskill_name",
            "location_parents",
            "location_x",
            "location_y",
            "bonuses",
            "bonus_talent_points",
            "checklist",
            "techtree_schematics_count",
            "enabled",
        ],
        techtree_item_rows(),
    )

    techtree_schematic_columns = [
        "source_relation_id",
        "jabbithole_techtree_item_id",
        "achievement_id",
        "jabbithole_techtree_item_name",
        "jabbithole_techtree_group_id",
        "achievement_category_id",
        "jabbithole_group_name",
        "jabbithole_tradeskill_id",
        "trade_skill_id",
        "jabbithole_tradeskill_name",
        "jabbithole_schematic_id",
        "tradeskill_schematic2_id",
        "jabbithole_schematic_name",
        "jabbithole_output_item_id",
        "enabled",
    ]
    techtree_schematic_query = """
SELECT
    tts.id,
    tts.tradeskill_techtree_item_id,
    i.game_id,
    i.name,
    g.id,
    g.game_id,
    g.name,
    g.tradeskill_id,
    ts.game_id,
    ts.name,
    tts.schematic_id,
    s.game_id,
    s.name,
    s.item_id,
    tts.enabled
FROM tradeskill_techtree_schematics tts
LEFT JOIN tradeskill_techtree_items i ON i.id = tts.tradeskill_techtree_item_id
LEFT JOIN tradeskill_techtree_groups g ON g.id = i.tradeskill_techtree_group_id
LEFT JOIN tradeskills ts ON ts.id = g.tradeskill_id
LEFT JOIN schematics s ON s.id = tts.schematic_id
ORDER BY ts.game_id, g.level, i.id, tts.id
"""

    def techtree_schematic_rows():
        for row in mysql_rows(args, limited(techtree_schematic_query, args.limit_relation_rows), techtree_schematic_columns):
            client_schematic = client["tradeskill_schematics"].get(to_int(row.get("tradeskill_schematic2_id"), -1) or -1, {})
            achievement = client["achievements"].get(to_int(row.get("achievement_id"), -1) or -1, {})
            row["client_achievement_title"] = achievement.get("achievementTitle", "")
            row["client_schematic_name"] = client_schematic.get("tradeskillSchematicName", "")
            row["client_group_category_name"] = lookup_name(client["achievement_categories"], row.get("achievement_category_id"), "achievementCategoryName")
            row["client_tradeskill_name"] = lookup_name(client["tradeskills"], row.get("trade_skill_id"), "tradeskillName")
            row["jabbithole_output_item_name"] = item_name(client["items"], row.get("jabbithole_output_item_id"))
            row["client_output_item2_id"] = client_schematic.get("item2IdOutput", "")
            row["client_output_item_name"] = item_name(client["items"], row.get("client_output_item2_id"))
            row["achievement_match_status"] = "matched" if achievement else "unmatched"
            row["schematic_match_status"] = "matched" if client_schematic else "unmatched"
            row["match_status"] = status_from_parts(bool(achievement), bool(client_schematic))
            yield row

    counts["tradeskill_techtree_schematic_map.csv"] = write_csv(
        args.output_dir / "tradeskill_techtree_schematic_map.csv",
        [
            "source_relation_id",
            "jabbithole_techtree_item_id",
            "achievement_id",
            "jabbithole_techtree_item_name",
            "client_achievement_title",
            "jabbithole_techtree_group_id",
            "achievement_category_id",
            "jabbithole_group_name",
            "client_group_category_name",
            "jabbithole_tradeskill_id",
            "trade_skill_id",
            "jabbithole_tradeskill_name",
            "client_tradeskill_name",
            "jabbithole_schematic_id",
            "tradeskill_schematic2_id",
            "jabbithole_schematic_name",
            "client_schematic_name",
            "jabbithole_output_item_id",
            "jabbithole_output_item_name",
            "client_output_item2_id",
            "client_output_item_name",
            "match_status",
            "achievement_match_status",
            "schematic_match_status",
            "enabled",
        ],
        techtree_schematic_rows(),
    )

    return counts


def write_world_faction_maps(
    args: argparse.Namespace,
    creature_map: Dict[int, Dict[str, object]],
    client,
) -> Dict[str, int]:
    counts: Dict[str, int] = {}

    def status_from_bools(*parts: bool) -> str:
        if all(parts):
            return "matched"
        if any(parts):
            return "partial"
        return "unmatched"

    def name_variants(value) -> List[str]:
        normalized = normalize_name(value)
        if not normalized:
            return []
        variants = {normalized}
        if normalized.endswith(" reputation"):
            variants.add(normalized[: -len(" reputation")].strip())
        for prefix in ("operation ", "mission "):
            if normalized.startswith(prefix):
                variants.add(normalized[len(prefix) :].strip())
        return [variant for variant in variants if variant]

    faction_name_index: Dict[str, List[int]] = defaultdict(list)
    for faction_id, faction in client["factions"].items():
        for variant in name_variants(faction.get("factionName")):
            faction_name_index[variant].append(faction_id)

    def resolve_faction(game_id, name) -> Tuple[str, str, str]:
        direct_id = to_int(game_id)
        if direct_id is not None and direct_id in client["factions"]:
            return clean_cell(direct_id), lookup_name(client["factions"], direct_id, "factionName"), "matched"
        seen: List[int] = []
        for variant in name_variants(name):
            for faction_id in faction_name_index.get(variant, []):
                if faction_id not in seen:
                    seen.append(faction_id)
        if len(seen) == 1:
            faction_id = seen[0]
            return clean_cell(faction_id), lookup_name(client["factions"], faction_id, "factionName"), "name_matched"
        if len(seen) > 1:
            faction_id = seen[0]
            return clean_cell(faction_id), lookup_name(client["factions"], faction_id, "factionName"), "ambiguous_name"
        return "", "", "unmatched"

    def parse_json_list(value) -> List[object]:
        if not value:
            return []
        try:
            parsed = json.loads(str(value))
        except (json.JSONDecodeError, TypeError):
            return []
        return parsed if isinstance(parsed, list) else []

    def parse_insert_column_names(statement: str) -> List[str]:
        values_index = statement.upper().find(" VALUES")
        header = statement[:values_index] if values_index >= 0 else statement
        open_idx = header.find("(")
        close_idx = header.rfind(")")
        if open_idx < 0 or close_idx < open_idx:
            return []
        return [part.strip().strip("`") for part in split_top_level(header[open_idx + 1 : close_idx])]

    def get_by_column(columns: Sequence[str], row: Sequence[object], column: str, occurrence: int = 0):
        seen = 0
        for index, name in enumerate(columns):
            if name == column:
                if seen == occurrence:
                    return row[index] if index < len(row) else None
                seen += 1
        return None

    continent_columns = [
        "jabbithole_continent_id",
        "map_continent_id",
        "jabbithole_continent_name",
        "slug",
        "map_asset",
        "show",
        "zones_count",
        "enabled",
        "last_seen_in",
    ]
    continent_query = """
SELECT id, game_id, name, slug, mapasset, `show`, zones_count, enabled, last_seen_in
FROM continents
ORDER BY id
"""

    def continent_rows():
        for row in mysql_rows(args, continent_query.strip(), continent_columns):
            continent = client["map_continents"].get(to_int(row.get("map_continent_id"), -1) or -1, {})
            row["client_continent_name"] = continent.get("mapContinentName", "")
            row["client_asset_path"] = continent.get("assetPath", "")
            row["client_image_path"] = continent.get("imagePath", "")
            row["match_status"] = "matched" if continent else "unmatched"
            yield row

    counts["continent_map.csv"] = write_csv(
        args.output_dir / "continent_map.csv",
        [
            "jabbithole_continent_id",
            "map_continent_id",
            "jabbithole_continent_name",
            "client_continent_name",
            "match_status",
            "slug",
            "map_asset",
            "client_asset_path",
            "client_image_path",
            "show",
            "zones_count",
            "enabled",
            "last_seen_in",
        ],
        continent_rows(),
    )

    sprite_by_name: Dict[str, List[int]] = defaultdict(list)
    for sprite_id, sprite in client["map_zone_sprites"].items():
        sprite_by_name[normalize_name(sprite.get("spriteName"))].append(sprite_id)

    poi_by_full: Dict[Tuple[int, Optional[int], Optional[int], Optional[int], str], List[int]] = defaultdict(list)
    poi_by_zone_name: Dict[Tuple[int, str], List[int]] = defaultdict(list)
    for poi_id, poi in client["map_zone_pois"].items():
        zone_id = to_int(poi.get("mapZoneId"), -1) or -1
        name_key = normalize_name(poi.get("mapZonePoiName"))
        full_key = (zone_id, signed32(poi.get("pos0")), signed32(poi.get("pos1")), signed32(poi.get("pos2")), name_key)
        poi_by_full[full_key].append(poi_id)
        poi_by_zone_name[(zone_id, name_key)].append(poi_id)

    poi_columns = ["source_poi_id", "map_zone_id", "jabbithole_poi_name", "icon", "x", "y", "z", "created_at", "updated_at"]
    poi_query = """
SELECT id, zone_game_id, name, icon, x, y, z, created_at, updated_at
FROM mapzone_pois
ORDER BY zone_game_id, id
"""

    def poi_rows():
        for row in mysql_rows(args, limited(poi_query.strip(), args.limit_relation_rows), poi_columns):
            zone_id = to_int(row.get("map_zone_id"), -1) or -1
            name_key = normalize_name(row.get("jabbithole_poi_name"))
            full_matches = poi_by_full.get((zone_id, to_int(row.get("x")), to_int(row.get("y")), to_int(row.get("z")), name_key), [])
            name_matches = poi_by_zone_name.get((zone_id, name_key), [])
            matches = full_matches or name_matches
            poi_id = matches[0] if matches else None
            poi = client["map_zone_pois"].get(poi_id or -1, {})
            sprite_matches = sprite_by_name.get(normalize_name(row.get("icon")), [])
            sprite_id = sprite_matches[0] if sprite_matches else poi.get("mapZoneSpriteId")
            row["map_zone_name"] = lookup_name(client["map_zones"], zone_id, "mapZoneName")
            row["map_zone_poi_id"] = poi_id or ""
            row["client_poi_name"] = poi.get("mapZonePoiName", "")
            row["client_x"] = signed32(poi.get("pos0")) if poi else ""
            row["client_y"] = signed32(poi.get("pos1")) if poi else ""
            row["client_z"] = signed32(poi.get("pos2")) if poi else ""
            row["map_zone_sprite_id"] = sprite_id or ""
            row["client_sprite_name"] = lookup_name(client["map_zone_sprites"], sprite_id, "spriteName")
            if full_matches and len(full_matches) == 1:
                row["match_status"] = "matched"
            elif full_matches:
                row["match_status"] = "ambiguous_position"
            elif name_matches:
                row["match_status"] = "name_only"
            else:
                row["match_status"] = "unmatched"
            yield row

    counts["mapzone_poi_map.csv"] = write_csv(
        args.output_dir / "mapzone_poi_map.csv",
        [
            "source_poi_id",
            "map_zone_id",
            "map_zone_name",
            "map_zone_poi_id",
            "match_status",
            "jabbithole_poi_name",
            "client_poi_name",
            "icon",
            "map_zone_sprite_id",
            "client_sprite_name",
            "x",
            "y",
            "z",
            "client_x",
            "client_y",
            "client_z",
            "created_at",
            "updated_at",
        ],
        poi_rows(),
    )

    quest_category_columns = ["jabbithole_category_id", "quest_category_id", "jabbithole_category_name", "slug", "quests_count", "enabled", "last_seen_in"]
    quest_category_query = """
SELECT id, game_id, name, slug, quests_count, enabled, last_seen_in
FROM quest_categories
ORDER BY game_id, id
"""

    def quest_category_rows():
        for row in mysql_rows(args, quest_category_query.strip(), quest_category_columns):
            category = client["quest_categories"].get(to_int(row.get("quest_category_id"), -1) or -1, {})
            row["client_category_name"] = category.get("questCategoryName", "")
            row["client_description"] = category.get("description", "")
            row["category_type"] = category.get("questCategoryTypeEnum", "")
            row["match_status"] = "matched" if category else "unmatched"
            yield row

    counts["quest_category_map.csv"] = write_csv(
        args.output_dir / "quest_category_map.csv",
        [
            "jabbithole_category_id",
            "quest_category_id",
            "jabbithole_category_name",
            "client_category_name",
            "match_status",
            "client_description",
            "category_type",
            "slug",
            "quests_count",
            "enabled",
            "last_seen_in",
        ],
        quest_category_rows(),
    )

    quest_episode_columns = ["jabbithole_episode_id", "episode_id", "jabbithole_episode_name", "slug", "quests_count", "enabled", "last_seen_in"]
    quest_episode_query = """
SELECT id, game_id, name, slug, quests_count, enabled, last_seen_in
FROM quest_episodes
ORDER BY game_id, id
"""

    def quest_episode_rows():
        for row in mysql_rows(args, quest_episode_query.strip(), quest_episode_columns):
            episode = client["episodes"].get(to_int(row.get("episode_id"), -1) or -1, {})
            row["client_episode_name"] = episode.get("episodeName", "")
            row["client_world_zone_id"] = episode.get("worldZoneId", "")
            row["client_world_zone_name"] = lookup_name(client["world_zones"], row.get("client_world_zone_id"), "worldZoneName")
            row["match_status"] = "matched" if episode else "unmatched"
            yield row

    counts["quest_episode_map.csv"] = write_csv(
        args.output_dir / "quest_episode_map.csv",
        [
            "jabbithole_episode_id",
            "episode_id",
            "jabbithole_episode_name",
            "client_episode_name",
            "match_status",
            "client_world_zone_id",
            "client_world_zone_name",
            "slug",
            "quests_count",
            "enabled",
            "last_seen_in",
        ],
        quest_episode_rows(),
    )

    quest_zone_columns = [
        "source_relation_id",
        "relation_source",
        "jabbithole_quest_id",
        "quest2_id",
        "jabbithole_quest_name",
        "jabbithole_zone_id",
        "world_zone_id",
        "jabbithole_zone_name",
        "enabled",
        "last_seen_in",
    ]
    quest_zone_query = """
SELECT
    qz.id,
    'quest_zones',
    qz.quest_id,
    q.game_id,
    q.name,
    qz.zone_id,
    z.game_id,
    z.name,
    qz.enabled,
    qz.last_seen_in
FROM quest_zones qz
LEFT JOIN quests q ON q.id = qz.quest_id
LEFT JOIN zones z ON z.id = qz.zone_id
UNION ALL
SELECT
    qcz.id,
    'quest_call_zones',
    qcz.quest_id,
    q.game_id,
    q.name,
    qcz.zone_id,
    z.game_id,
    z.name,
    qcz.enabled,
    qcz.last_seen_in
FROM quest_call_zones qcz
LEFT JOIN quests q ON q.id = qcz.quest_id
LEFT JOIN zones z ON z.id = qcz.zone_id
ORDER BY 3, 2, 1
"""

    def quest_zone_rows():
        for row in mysql_rows(args, limited(quest_zone_query.strip(), args.limit_relation_rows), quest_zone_columns):
            quest = client["quests"].get(to_int(row.get("quest2_id"), -1) or -1, {})
            zone = client["world_zones"].get(to_int(row.get("world_zone_id"), -1) or -1, {})
            row["client_quest_name"] = quest.get("questName", "")
            row["client_world_zone_name"] = zone.get("worldZoneName", "")
            row["match_status"] = status_from_bools(bool(quest), bool(zone))
            yield row

    counts["quest_zone_map.csv"] = write_csv(
        args.output_dir / "quest_zone_map.csv",
        [
            "source_relation_id",
            "relation_source",
            "jabbithole_quest_id",
            "quest2_id",
            "jabbithole_quest_name",
            "client_quest_name",
            "jabbithole_zone_id",
            "world_zone_id",
            "jabbithole_zone_name",
            "client_world_zone_name",
            "match_status",
            "enabled",
            "last_seen_in",
        ],
        quest_zone_rows(),
    )

    faction_columns = [
        "jabbithole_faction_id",
        "source_game_id",
        "jabbithole_faction_name",
        "slug",
        "faction_group",
        "parent_name",
        "is_dominion",
        "is_exile",
        "faction_reward_items_count",
        "enabled",
        "first_seen_in",
        "last_seen_in",
    ]
    faction_query = """
SELECT
    id,
    game_id,
    name,
    slug,
    `group`,
    parent,
    is_dominion,
    is_exile,
    faction_reward_items_count,
    enabled,
    first_seen_in,
    last_seen_in
FROM factions
ORDER BY id
"""
    source_faction_rows = list(mysql_rows(args, faction_query.strip(), faction_columns))
    faction_by_jabbithole_id: Dict[int, Dict[str, str]] = {}

    def faction_rows():
        for row in source_faction_rows:
            faction_id, faction_name, match_status = resolve_faction(row.get("source_game_id"), row.get("jabbithole_faction_name"))
            parent_id, parent_name, parent_status = resolve_faction(None, row.get("parent_name"))
            row["faction2_id"] = faction_id
            row["client_faction_name"] = faction_name
            row["parent_faction2_id"] = parent_id
            row["client_parent_faction_name"] = parent_name
            row["parent_match_status"] = parent_status
            row["match_status"] = match_status
            source_id = to_int(row.get("jabbithole_faction_id"))
            if source_id is not None:
                faction_by_jabbithole_id[source_id] = row
            yield row

    counts["faction_map.csv"] = write_csv(
        args.output_dir / "faction_map.csv",
        [
            "jabbithole_faction_id",
            "source_game_id",
            "faction2_id",
            "jabbithole_faction_name",
            "client_faction_name",
            "match_status",
            "slug",
            "faction_group",
            "parent_name",
            "parent_faction2_id",
            "client_parent_faction_name",
            "parent_match_status",
            "is_dominion",
            "is_exile",
            "faction_reward_items_count",
            "enabled",
            "first_seen_in",
            "last_seen_in",
        ],
        faction_rows(),
    )

    faction_reward_columns = [
        "source_relation_id",
        "jabbithole_faction_id",
        "source_faction_game_id",
        "jabbithole_faction_name",
        "jabbithole_item_id",
        "item2_id",
        "required_reputation",
        "enabled",
        "last_seen_in",
    ]
    faction_reward_query = """
SELECT
    fri.id,
    fri.faction_id,
    f.game_id,
    f.name,
    fri.item_id,
    fri.game_id_item,
    fri.required_reputation,
    fri.enabled,
    fri.last_seen_in
FROM faction_reward_items fri
LEFT JOIN factions f ON f.id = fri.faction_id
ORDER BY fri.faction_id, fri.required_reputation, fri.id
"""

    def faction_reward_rows():
        for row in mysql_rows(args, limited(faction_reward_query.strip(), args.limit_relation_rows), faction_reward_columns):
            mapped_faction = faction_by_jabbithole_id.get(to_int(row.get("jabbithole_faction_id"), -1) or -1, {})
            faction_id = mapped_faction.get("faction2_id", "")
            faction_name = mapped_faction.get("client_faction_name", "")
            if not faction_id:
                faction_id, faction_name, _ = resolve_faction(row.get("source_faction_game_id"), row.get("jabbithole_faction_name"))
            row["faction2_id"] = faction_id
            row["client_faction_name"] = faction_name
            row["item_name"] = item_name(client["items"], row.get("item2_id"))
            row["match_status"] = status_from_bools(bool(faction_id), bool(row["item_name"]))
            yield row

    counts["faction_reward_item_map.csv"] = write_csv(
        args.output_dir / "faction_reward_item_map.csv",
        [
            "source_relation_id",
            "jabbithole_faction_id",
            "source_faction_game_id",
            "faction2_id",
            "jabbithole_faction_name",
            "client_faction_name",
            "jabbithole_item_id",
            "item2_id",
            "item_name",
            "required_reputation",
            "match_status",
            "enabled",
            "last_seen_in",
        ],
        faction_reward_rows(),
    )

    attribute_contribution_columns = [
        "source_relation_id",
        "primary_jabbithole_attribute_id",
        "primary_unit_property2_id",
        "primary_attribute_name",
        "secondary_jabbithole_attribute_id",
        "secondary_unit_property2_id",
        "secondary_attribute_name",
        "jabbithole_player_class_id",
        "class_id",
        "class_name",
        "contribution",
        "enabled",
    ]
    attribute_contribution_query = """
SELECT
    ac.id,
    ac.primary_attribute_id,
    pa.game_id,
    pa.name,
    ac.secondary_attribute_id,
    sa.game_id,
    sa.name,
    ac.player_class_id,
    pc.game_id,
    pc.name,
    ac.contribution,
    ac.enabled
FROM attribute_contributions ac
LEFT JOIN attributes pa ON pa.id = ac.primary_attribute_id
LEFT JOIN attributes sa ON sa.id = ac.secondary_attribute_id
LEFT JOIN player_classes pc ON pc.id = ac.player_class_id
ORDER BY pc.game_id, ac.primary_attribute_id, ac.secondary_attribute_id, ac.id
"""

    def attribute_contribution_rows():
        for row in mysql_rows(args, attribute_contribution_query.strip(), attribute_contribution_columns):
            row["primary_client_attribute_name"] = lookup_name(client["unit_properties"], row.get("primary_unit_property2_id"), "unitPropertyName")
            row["primary_client_attribute_enum"] = lookup_name(client["unit_properties"], row.get("primary_unit_property2_id"), "enumName")
            row["secondary_client_attribute_name"] = lookup_name(client["unit_properties"], row.get("secondary_unit_property2_id"), "unitPropertyName")
            row["secondary_client_attribute_enum"] = lookup_name(client["unit_properties"], row.get("secondary_unit_property2_id"), "enumName")
            row["client_class_name"] = lookup_name(client["classes"], row.get("class_id"), "className")
            row["match_status"] = status_from_bools(
                bool(row["primary_client_attribute_name"] or row["primary_client_attribute_enum"]),
                bool(row["secondary_client_attribute_name"] or row["secondary_client_attribute_enum"]),
                bool(row["client_class_name"]),
            )
            yield row

    counts["attribute_contribution_map.csv"] = write_csv(
        args.output_dir / "attribute_contribution_map.csv",
        [
            "source_relation_id",
            "primary_jabbithole_attribute_id",
            "primary_unit_property2_id",
            "primary_attribute_name",
            "primary_client_attribute_name",
            "primary_client_attribute_enum",
            "secondary_jabbithole_attribute_id",
            "secondary_unit_property2_id",
            "secondary_attribute_name",
            "secondary_client_attribute_name",
            "secondary_client_attribute_enum",
            "jabbithole_player_class_id",
            "class_id",
            "class_name",
            "client_class_name",
            "contribution",
            "match_status",
            "enabled",
        ],
        attribute_contribution_rows(),
    )

    attribute_milestone_columns = [
        "source_relation_id",
        "primary_jabbithole_attribute_id",
        "primary_unit_property2_id",
        "primary_attribute_name",
        "secondary_jabbithole_attribute_id",
        "secondary_unit_property2_id",
        "secondary_attribute_name",
        "jabbithole_player_class_id",
        "class_id",
        "class_name",
        "required_amount",
        "contribution",
        "enabled",
    ]
    attribute_milestone_query = """
SELECT
    am.id,
    am.primary_attribute_id,
    pa.game_id,
    pa.name,
    am.secondary_attribute_id,
    sa.game_id,
    sa.name,
    am.player_class_id,
    pc.game_id,
    pc.name,
    am.required_amount,
    am.contribution,
    am.enabled
FROM attribute_milestones am
LEFT JOIN attributes pa ON pa.id = am.primary_attribute_id
LEFT JOIN attributes sa ON sa.id = am.secondary_attribute_id
LEFT JOIN player_classes pc ON pc.id = am.player_class_id
ORDER BY pc.game_id, am.primary_attribute_id, am.secondary_attribute_id, am.required_amount, am.id
"""

    def attribute_milestone_rows():
        for row in mysql_rows(args, attribute_milestone_query.strip(), attribute_milestone_columns):
            row["primary_client_attribute_name"] = lookup_name(client["unit_properties"], row.get("primary_unit_property2_id"), "unitPropertyName")
            row["primary_client_attribute_enum"] = lookup_name(client["unit_properties"], row.get("primary_unit_property2_id"), "enumName")
            row["secondary_client_attribute_name"] = lookup_name(client["unit_properties"], row.get("secondary_unit_property2_id"), "unitPropertyName")
            row["secondary_client_attribute_enum"] = lookup_name(client["unit_properties"], row.get("secondary_unit_property2_id"), "enumName")
            row["client_class_name"] = lookup_name(client["classes"], row.get("class_id"), "className")
            row["match_status"] = status_from_bools(
                bool(row["primary_client_attribute_name"] or row["primary_client_attribute_enum"]),
                bool(row["secondary_client_attribute_name"] or row["secondary_client_attribute_enum"]),
                bool(row["client_class_name"]),
            )
            yield row

    counts["attribute_milestone_map.csv"] = write_csv(
        args.output_dir / "attribute_milestone_map.csv",
        [
            "source_relation_id",
            "primary_jabbithole_attribute_id",
            "primary_unit_property2_id",
            "primary_attribute_name",
            "primary_client_attribute_name",
            "primary_client_attribute_enum",
            "secondary_jabbithole_attribute_id",
            "secondary_unit_property2_id",
            "secondary_attribute_name",
            "secondary_client_attribute_name",
            "secondary_client_attribute_enum",
            "jabbithole_player_class_id",
            "class_id",
            "class_name",
            "client_class_name",
            "required_amount",
            "contribution",
            "match_status",
            "enabled",
        ],
        attribute_milestone_rows(),
    )

    sigil_columns = ["source_relation_id", "item2_id", "sigil", "sigil_order", "enabled", "created_at", "updated_at"]
    sigil_query = """
SELECT id, item_id, sigil, `order`, enabled, created_at, updated_at
FROM item_instance_sigils
ORDER BY item_id, `order`, id
"""

    def sigil_rows():
        for row in mysql_rows(args, limited(sigil_query.strip(), args.limit_relation_rows), sigil_columns):
            row["item_name"] = item_name(client["items"], row.get("item2_id"))
            row["match_status"] = "matched" if row["item_name"] else "unmatched"
            yield row

    counts["item_instance_sigil_map.csv"] = write_csv(
        args.output_dir / "item_instance_sigil_map.csv",
        ["source_relation_id", "item2_id", "item_name", "sigil", "sigil_order", "match_status", "enabled", "created_at", "updated_at"],
        sigil_rows(),
    )

    legacy_vendor_columns = [
        "source_relation_id",
        "jabbithole_creature_id",
        "source_name",
        "raw_item_id",
        "item2_id",
        "main_jabbithole_currency_id",
        "main_currency_type_id",
        "main_currency_name",
        "alt_jabbithole_currency_id",
        "alt_currency_type_id",
        "alt_currency_name",
        "special_item",
        "stack_size",
        "stock_size",
        "main_price",
        "alt_price",
        "prerequisite",
        "enabled",
        "last_seen_in",
    ]
    legacy_vendor_query = """
SELECT
    vi.id,
    vi.creature_id,
    c.name,
    vi.item_id,
    vi.game_id_item,
    vi.main_currency_id,
    mc.game_id,
    mc.name,
    vi.alt_currency_id,
    ac.game_id,
    ac.name,
    vi.special_item,
    vi.stack_size,
    vi.stock_size,
    vi.main_price,
    vi.alt_price,
    vi.prerequisite,
    vi.enabled,
    vi.last_seen_in
FROM vendor_items vi
LEFT JOIN creatures c ON c.id = vi.creature_id
LEFT JOIN currencies mc ON mc.id = vi.main_currency_id
LEFT JOIN currencies ac ON ac.id = vi.alt_currency_id
ORDER BY vi.creature_id, vi.id
"""

    def legacy_vendor_rows():
        for row in mysql_rows(args, limited(legacy_vendor_query.strip(), args.limit_relation_rows), legacy_vendor_columns):
            row = apply_creature_map(row, creature_map)
            row["item_name"] = item_name(client["items"], row.get("item2_id"))
            row["client_main_currency_name"] = lookup_name(client["currencies"], row.get("main_currency_type_id"), "currencyName")
            row["client_alt_currency_name"] = lookup_name(client["currencies"], row.get("alt_currency_type_id"), "currencyName")
            yield row

    counts["legacy_vendor_item_map.csv"] = write_csv(
        args.output_dir / "legacy_vendor_item_map.csv",
        [
            "source_relation_id",
            "jabbithole_creature_id",
            "creature2_id",
            "source_name",
            "creature2_name",
            "match_status",
            "raw_item_id",
            "item2_id",
            "item_name",
            "main_jabbithole_currency_id",
            "main_currency_type_id",
            "main_currency_name",
            "client_main_currency_name",
            "alt_jabbithole_currency_id",
            "alt_currency_type_id",
            "alt_currency_name",
            "client_alt_currency_name",
            "special_item",
            "stack_size",
            "stock_size",
            "main_price",
            "alt_price",
            "prerequisite",
            "enabled",
            "last_seen_in",
        ],
        legacy_vendor_rows(),
    )

    aggregate_columns = [
        "source_aggregate_id",
        "raw_item_id",
        "item2_id",
        "versioned_item_drops_count",
        "game_version",
        "last_seen_in",
        "origin_create",
        "origin_update",
        "origin_before",
        "created_at",
        "updated_at",
    ]
    aggregate_query = """
SELECT
    id,
    item_id,
    game_id_item,
    versioned_item_drops_count,
    game_version,
    last_seen_in,
    origin_create,
    origin_update,
    origin_before,
    created_at,
    updated_at
FROM versioned_item_drop_aggregates
ORDER BY game_id_item, game_version, id
"""

    def aggregate_rows():
        for row in mysql_rows(args, limited(aggregate_query.strip(), args.limit_relation_rows), aggregate_columns):
            row["item_name"] = item_name(client["items"], row.get("item2_id"))
            row["match_status"] = "matched" if row["item_name"] else "unmatched"
            yield row

    counts["item_drop_aggregate_map.csv"] = write_csv(
        args.output_dir / "item_drop_aggregate_map.csv",
        [
            "source_aggregate_id",
            "raw_item_id",
            "item2_id",
            "item_name",
            "match_status",
            "versioned_item_drops_count",
            "game_version",
            "last_seen_in",
            "origin_create",
            "origin_update",
            "origin_before",
            "created_at",
            "updated_at",
        ],
        aggregate_rows(),
    )

    character_columns = [
        "character_id",
        "character_name",
        "fullname",
        "realm_name",
        "guild_name",
        "jabbithole_player_class_id",
        "class_id",
        "class_level",
        "jabbithole_player_path_id",
        "path_type",
        "path_name",
        "path_level",
        "faction2_id",
        "gender",
        "race",
        "gear_score",
        "items_json",
        "build_json",
        "amps",
        "unified_build_key",
        "unified_abilities_key",
        "achievement_points",
        "enabled",
        "created_at",
        "updated_at",
        "ability_points_total",
        "amp_points_total",
    ]
    character_query = """
SELECT
    ch.id,
    ch.name,
    ch.fullname,
    ch.realm_name,
    ch.guild_name,
    ch.player_class_id,
    pc.game_id,
    ch.class_level,
    ch.player_path_id,
    pp.game_id,
    pp.name,
    ch.path_level,
    ch.faction,
    ch.gender,
    ch.race,
    ch.gear_score,
    ch.items,
    ch.build,
    ch.amps,
    ch.unified_build_key,
    ch.unified_abilities_key,
    ch.achievement_points,
    ch.enabled,
    ch.created_at,
    ch.updated_at,
    ch.ability_points_total,
    ch.amp_points_total
FROM characters ch
LEFT JOIN player_classes pc ON pc.id = ch.player_class_id
LEFT JOIN player_paths pp ON pp.id = ch.player_path_id
ORDER BY ch.id
"""
    character_rows_raw = list(mysql_rows(args, limited(character_query.strip(), args.limit_relation_rows), character_columns))

    def character_rows():
        for row in character_rows_raw:
            row["client_class_name"] = lookup_name(client["classes"], row.get("class_id"), "className")
            row["client_faction_name"] = lookup_name(client["factions"], row.get("faction2_id"), "factionName")
            item_ids = [to_int(value) for value in parse_json_list(row.get("items_json")) if to_int(value) is not None]
            build_entries = [entry for entry in parse_json_list(row.get("build_json")) if isinstance(entry, dict)]
            row["equipped_item_count"] = len(item_ids)
            row["build_ability_count"] = len(build_entries)
            row["match_status"] = status_from_bools(bool(row["client_class_name"]), bool(row["client_faction_name"]))
            yield {key: value for key, value in row.items() if key not in {"items_json", "build_json"}}

    counts["character_snapshot_map.csv"] = write_csv(
        args.output_dir / "character_snapshot_map.csv",
        [
            "character_id",
            "character_name",
            "fullname",
            "realm_name",
            "guild_name",
            "jabbithole_player_class_id",
            "class_id",
            "client_class_name",
            "class_level",
            "jabbithole_player_path_id",
            "path_type",
            "path_name",
            "path_level",
            "faction2_id",
            "client_faction_name",
            "gender",
            "race",
            "gear_score",
            "equipped_item_count",
            "build_ability_count",
            "amps",
            "unified_build_key",
            "unified_abilities_key",
            "achievement_points",
            "ability_points_total",
            "amp_points_total",
            "match_status",
            "enabled",
            "created_at",
            "updated_at",
        ],
        character_rows(),
    )

    def character_item_rows():
        for character in character_rows_raw:
            for slot, value in enumerate(parse_json_list(character.get("items_json"))):
                item_id = to_int(value)
                if item_id is None:
                    continue
                yield {
                    "character_id": character.get("character_id"),
                    "character_name": character.get("character_name"),
                    "equipment_slot_index": slot,
                    "item2_id": item_id,
                    "item_name": item_name(client["items"], item_id),
                    "match_status": "matched" if item_name(client["items"], item_id) else "unmatched",
                }

    counts["character_item_map.csv"] = write_csv(
        args.output_dir / "character_item_map.csv",
        ["character_id", "character_name", "equipment_slot_index", "item2_id", "item_name", "match_status"],
        character_item_rows(),
    )

    def character_build_rows():
        for character in character_rows_raw:
            for slot, entry in enumerate(parse_json_list(character.get("build_json"))):
                if not isinstance(entry, dict):
                    continue
                spell_id = to_int(entry.get("a"))
                tier = to_int(entry.get("t"))
                yield {
                    "character_id": character.get("character_id"),
                    "character_name": character.get("character_name"),
                    "build_slot_index": slot,
                    "spell4_id": spell_id or "",
                    "spell_name": spell_name(client, spell_id),
                    "tier": tier if tier is not None else "",
                    "match_status": "matched" if spell_name(client, spell_id) else "unmatched",
                }

    counts["character_build_ability_map.csv"] = write_csv(
        args.output_dir / "character_build_ability_map.csv",
        ["character_id", "character_name", "build_slot_index", "spell4_id", "spell_name", "tier", "match_status"],
        character_build_rows(),
    )

    schema_migration_query = "SELECT version FROM schema_migrations ORDER BY version"
    counts["schema_migration_map.csv"] = write_csv(
        args.output_dir / "schema_migration_map.csv",
        ["version"],
        mysql_rows(args, schema_migration_query, ["version"]),
    )

    counts["empty_items_split_map.csv"] = write_csv(
        args.output_dir / "empty_items_split_map.csv",
        ["source_file", "row_count", "notes"],
        [{"source_file": "items.sql", "row_count": 0, "notes": "The split file contains only a semicolon in this dump."}],
    )

    unknown_path = args.jabbithole_sql_dir / ".sql"

    def unknown_loot_rows():
        if not unknown_path.exists():
            return
        emitted = 0
        with unknown_path.open("r", encoding="utf-8", errors="replace") as handle:
            for line in handle:
                if args.limit_relation_rows is not None and emitted >= args.limit_relation_rows:
                    break
                if not line.startswith("INSERT INTO"):
                    continue
                statement = line.strip().rstrip(";")
                columns = parse_insert_column_names(statement)
                for raw_row in parse_insert_rows(statement):
                    emitted += 1
                    item_id = get_by_column(columns, raw_row, "ItemID")
                    source_creature_id = get_by_column(columns, raw_row, "id")
                    row = {
                        "source_relation_id": emitted,
                        "item2_id": item_id,
                        "item_name": item_name(client["items"], item_id),
                        "jabbithole_creature_id": source_creature_id,
                        "source_name": get_by_column(columns, raw_row, "NPC_Name") or get_by_column(columns, raw_row, "name"),
                        "npc_type": get_by_column(columns, raw_row, "NPC_Type"),
                        "zone_name": get_by_column(columns, raw_row, "Zone"),
                        "drop_percent": get_by_column(columns, raw_row, "Drop_Percent"),
                        "drop_count": get_by_column(columns, raw_row, "Drop_Count"),
                        "npc_url": get_by_column(columns, raw_row, "NPC_URL"),
                        "version": get_by_column(columns, raw_row, "Version"),
                        "raw_item_name": get_by_column(columns, raw_row, "Item_Name"),
                        "raw_item_family": get_by_column(columns, raw_row, "Item_Family"),
                        "raw_item_category": get_by_column(columns, raw_row, "Item_Category"),
                        "raw_item_slot": get_by_column(columns, raw_row, "Item_Slot"),
                    }
                    row = apply_creature_map(row, creature_map)
                    if row.get("match_status") and row.get("item_name"):
                        row["row_match_status"] = "matched"
                    elif row.get("match_status") or row.get("item_name"):
                        row["row_match_status"] = "partial"
                    else:
                        row["row_match_status"] = "unmatched"
                    yield row
                    if args.limit_relation_rows is not None and emitted >= args.limit_relation_rows:
                        break

    counts["unknown_table_loot_map.csv"] = write_csv(
        args.output_dir / "unknown_table_loot_map.csv",
        [
            "source_relation_id",
            "item2_id",
            "item_name",
            "jabbithole_creature_id",
            "creature2_id",
            "source_name",
            "creature2_name",
            "match_status",
            "row_match_status",
            "npc_type",
            "zone_name",
            "drop_percent",
            "drop_count",
            "npc_url",
            "version",
            "raw_item_name",
            "raw_item_family",
            "raw_item_category",
            "raw_item_slot",
        ],
        unknown_loot_rows(),
    )

    return counts


def write_challenge_maps(
    args: argparse.Namespace,
    creature_map: Dict[int, Dict[str, object]],
    client,
) -> Dict[str, int]:
    counts: Dict[str, int] = {}

    challenge_columns = [
        "jabbithole_challenge_id",
        "zone_id",
        "challenge_id",
        "jabbithole_challenge_name",
        "description",
        "goals",
        "is_timed",
        "challenge_type",
        "is_dominion",
        "is_exile",
        "challenge_reward_items_count",
        "challenge_creatures_count",
        "enabled",
        "last_seen_in",
        "zone_restriction_subzone",
        "start_location",
        "challenge_reward_track_id",
    ]
    challenge_query = """
SELECT
    id,
    zone_id,
    game_id,
    name,
    description,
    goals,
    is_timed,
    challenge_type,
    is_dominion,
    is_exile,
    challenge_reward_items_count,
    challenge_creatures_count,
    enabled,
    last_seen_in,
    zone_restriction_subzone,
    start_location,
    challenge_reward_track_id
FROM challenges
ORDER BY id
"""

    def challenge_rows():
        for row in mysql_rows(args, challenge_query, challenge_columns):
            client_row = client["challenges"].get(to_int(row.get("challenge_id"), -1) or -1, {})
            row["client_challenge_name"] = client_row.get("challengeName", "")
            row["client_challenge_type"] = client_row.get("challengeTypeEnum", "")
            row["client_target"] = client_row.get("target", "")
            row["client_completion_count"] = client_row.get("completionCount", "")
            row["client_world_zone_id"] = client_row.get("worldZoneId", "")
            row["client_reward_track_id"] = client_row.get("rewardTrackId", "")
            row["match_status"] = "matched" if client_row else "unmatched"
            return_row = row
            yield return_row

    counts["challenge_map.csv"] = write_csv(
        args.output_dir / "challenge_map.csv",
        [
            "jabbithole_challenge_id",
            "challenge_id",
            "jabbithole_challenge_name",
            "client_challenge_name",
            "match_status",
            "description",
            "goals",
            "is_timed",
            "challenge_type",
            "client_challenge_type",
            "zone_id",
            "client_world_zone_id",
            "zone_restriction_subzone",
            "start_location",
            "challenge_reward_track_id",
            "client_reward_track_id",
            "client_target",
            "client_completion_count",
            "challenge_reward_items_count",
            "challenge_creatures_count",
            "is_dominion",
            "is_exile",
            "enabled",
            "last_seen_in",
        ],
        challenge_rows(),
    )

    creature_columns = [
        "source_relation_id",
        "jabbithole_challenge_id",
        "challenge_id",
        "challenge_name",
        "challenge_type",
        "goals",
        "zone_id",
        "jabbithole_creature_id",
        "last_seen_in",
    ]
    creature_query = """
SELECT
    cc.id,
    cc.challenge_id,
    c.game_id,
    c.name,
    c.challenge_type,
    c.goals,
    c.zone_id,
    cc.creature_id,
    cc.last_seen_in
FROM challenge_creatures cc
LEFT JOIN challenges c ON c.id = cc.challenge_id
WHERE cc.enabled = 'true'
ORDER BY cc.challenge_id, cc.creature_id, cc.id
"""

    def transform_challenge_creature(row):
        row = apply_creature_map(row, creature_map)
        client_challenge = client["challenges"].get(to_int(row.get("challenge_id"), -1) or -1, {})
        row["client_challenge_name"] = client_challenge.get("challengeName", "")
        return row

    name, count = query_relation(
        args,
        "challenge_creature_map",
        creature_query,
        creature_columns,
        [
            "source_relation_id",
            "jabbithole_challenge_id",
            "challenge_id",
            "challenge_name",
            "client_challenge_name",
            "challenge_type",
            "goals",
            "zone_id",
            "jabbithole_creature_id",
            "creature2_id",
            "source_name",
            "creature2_name",
            "match_status",
            "last_seen_in",
        ],
        creature_map,
        transform_challenge_creature,
        args.limit_relation_rows,
    )
    counts[name] = count

    reward_columns = [
        "relation_source",
        "source_relation_id",
        "jabbithole_challenge_id",
        "challenge_id",
        "challenge_name",
        "challenge_reward_track_id",
        "jabbithole_challenge_reward_id",
        "challenge_reward_id",
        "reward_index",
        "reward_type",
        "cost",
        "tier",
        "amount",
        "jabbithole_item_id",
        "item2_id",
        "last_seen_in",
    ]
    reward_query = """
SELECT *
FROM (
SELECT
    'direct_challenge_reward_items' AS relation_source,
    cri.id AS source_relation_id,
    c.id AS jabbithole_challenge_id,
    c.game_id AS challenge_id,
    c.name AS challenge_name,
    c.challenge_reward_track_id AS challenge_reward_track_id,
    NULL AS jabbithole_challenge_reward_id,
    cri.game_reward_id AS challenge_reward_id,
    NULL AS reward_index,
    NULL AS reward_type,
    NULL AS cost,
    cri.tier AS tier,
    cri.amount AS amount,
    cri.item_id AS jabbithole_item_id,
    cri.game_id_item AS item2_id,
    cri.last_seen_in AS last_seen_in
FROM challenge_reward_items cri
LEFT JOIN challenges c ON c.id = cri.challenge_id
WHERE cri.enabled = 'true'
UNION ALL
SELECT
    'reward_track_challenge_item' AS relation_source,
    cir.id AS source_relation_id,
    c.id AS jabbithole_challenge_id,
    c.game_id AS challenge_id,
    c.name AS challenge_name,
    c.challenge_reward_track_id AS challenge_reward_track_id,
    cr.id AS jabbithole_challenge_reward_id,
    cr.game_id AS challenge_reward_id,
    COALESCE(cir.reward_index, cr.reward_index) AS reward_index,
    cir.rewardtype AS reward_type,
    cr.cost AS cost,
    cr.reward_index AS tier,
    cir.amount AS amount,
    cir.item_id AS jabbithole_item_id,
    cir.item_id AS item2_id,
    cir.last_seen_in AS last_seen_in
FROM challenges c
JOIN challenge_reward_track_challenge_rewards crtcr
    ON crtcr.challenge_reward_track_id = c.challenge_reward_track_id
JOIN challenge_rewards cr
    ON cr.id = crtcr.challenge_reward_id
JOIN challenge_item_rewards cir
    ON cir.challenge_reward_id = cr.id
) r
ORDER BY jabbithole_challenge_id, relation_source, source_relation_id
"""

    def reward_rows():
        for row in mysql_rows(args, limited(reward_query, args.limit_relation_rows), reward_columns):
            client_challenge = client["challenges"].get(to_int(row.get("challenge_id"), -1) or -1, {})
            row["client_challenge_name"] = client_challenge.get("challengeName", "")
            row["item_name"] = item_name(client["items"], row.get("item2_id"))
            yield row

    counts["challenge_reward_item_map.csv"] = write_csv(
        args.output_dir / "challenge_reward_item_map.csv",
        [
            "relation_source",
            "source_relation_id",
            "jabbithole_challenge_id",
            "challenge_id",
            "challenge_name",
            "client_challenge_name",
            "challenge_reward_track_id",
            "jabbithole_challenge_reward_id",
            "challenge_reward_id",
            "reward_index",
            "reward_type",
            "cost",
            "tier",
            "amount",
            "jabbithole_item_id",
            "item2_id",
            "item_name",
            "last_seen_in",
        ],
        reward_rows(),
    )
    return counts


def write_quest_detail_maps(args: argparse.Namespace, client) -> Dict[str, int]:
    counts: Dict[str, int] = {}
    objective_slot_fields = ["objective0", "objective01", "objective02", "objective03", "objective04", "objective05"]

    objective_columns = [
        "jabbithole_quest_objective_id",
        "jabbithole_quest_id",
        "quest2_id",
        "quest_name",
        "objective_order",
        "jabbithole_objective_text",
        "last_seen_in",
    ]
    objective_query = """
SELECT
    qo.id,
    qo.quest_id,
    q.game_id,
    q.name,
    qo.`order`,
    qo.text,
    qo.last_seen_in
FROM quest_objectives qo
LEFT JOIN quests q ON q.id = qo.quest_id
WHERE qo.enabled = 'true'
ORDER BY qo.quest_id, qo.`order`, qo.id
"""

    def objective_rows():
        for row in mysql_rows(args, limited(objective_query, args.limit_relation_rows), objective_columns):
            quest = client["quests"].get(to_int(row.get("quest2_id"), -1) or -1, {})
            order = to_int(row.get("objective_order"), -1) or -1
            objective_id = ""
            if 0 <= order < len(objective_slot_fields):
                objective_id = quest.get(objective_slot_fields[order], "")
            objective = client["quest_objectives"].get(to_int(objective_id, -1) or -1, {})
            row["client_quest_name"] = quest.get("questName", "")
            row["quest_objective_id"] = objective_id
            row["client_objective_text"] = objective.get("questObjectiveText", "")
            row["objective_type"] = objective.get("type", "")
            row["objective_data"] = objective.get("data", "")
            row["objective_count"] = objective.get("count", "")
            row["quest_direction_id"] = objective.get("questDirectionId", "")
            row["world_location_0"] = objective.get("worldLocationsIdIndicator00", "")
            row["world_location_1"] = objective.get("worldLocationsIdIndicator01", "")
            row["world_location_2"] = objective.get("worldLocationsIdIndicator02", "")
            row["world_location_3"] = objective.get("worldLocationsIdIndicator03", "")
            row["jabbithole_objective_text"] = display_text(row.get("jabbithole_objective_text"))
            row["match_status"] = "matched" if objective else "unmatched"
            yield row

    counts["quest_objective_map.csv"] = write_csv(
        args.output_dir / "quest_objective_map.csv",
        [
            "jabbithole_quest_objective_id",
            "jabbithole_quest_id",
            "quest2_id",
            "quest_name",
            "client_quest_name",
            "objective_order",
            "quest_objective_id",
            "match_status",
            "jabbithole_objective_text",
            "client_objective_text",
            "objective_type",
            "objective_data",
            "objective_count",
            "quest_direction_id",
            "world_location_0",
            "world_location_1",
            "world_location_2",
            "world_location_3",
            "last_seen_in",
        ],
        objective_rows(),
    )

    item_columns = [
        "source_reward_id",
        "jabbithole_quest_id",
        "quest2_id",
        "quest_name",
        "jabbithole_item_id",
        "item2_id",
        "game_reward_id",
        "amount",
        "fixed_reward",
        "last_seen_in",
    ]
    item_query = """
SELECT
    qri.id,
    qri.quest_id,
    q.game_id,
    q.name,
    qri.item_id,
    qri.game_id_item,
    qri.game_reward_id,
    qri.amount,
    qri.fixed_reward,
    qri.last_seen_in
FROM quest_reward_items qri
LEFT JOIN quests q ON q.id = qri.quest_id
WHERE qri.enabled = 'true'
ORDER BY qri.quest_id, qri.id
"""

    def item_rows():
        for row in mysql_rows(args, limited(item_query, args.limit_relation_rows), item_columns):
            row["client_quest_name"] = lookup_name(client["quests"], row.get("quest2_id"), "questName")
            row["item_name"] = item_name(client["items"], row.get("item2_id"))
            yield row

    counts["quest_reward_item_map.csv"] = write_csv(
        args.output_dir / "quest_reward_item_map.csv",
        [
            "source_reward_id",
            "jabbithole_quest_id",
            "quest2_id",
            "quest_name",
            "client_quest_name",
            "jabbithole_item_id",
            "item2_id",
            "item_name",
            "game_reward_id",
            "amount",
            "fixed_reward",
            "last_seen_in",
        ],
        item_rows(),
    )

    currency_columns = [
        "source_reward_id",
        "jabbithole_quest_id",
        "quest2_id",
        "quest_name",
        "jabbithole_currency_id",
        "currency_type_id",
        "currency_name",
        "game_reward_id",
        "amount",
        "fixed_reward",
        "last_seen_in",
    ]
    currency_query = """
SELECT
    qrc.id,
    qrc.quest_id,
    q.game_id,
    q.name,
    qrc.currency_id,
    c.game_id,
    c.name,
    qrc.game_reward_id,
    qrc.amount,
    qrc.fixed_reward,
    qrc.last_seen_in
FROM quest_reward_currencies qrc
LEFT JOIN quests q ON q.id = qrc.quest_id
LEFT JOIN currencies c ON c.id = qrc.currency_id
WHERE qrc.enabled = 'true'
ORDER BY qrc.quest_id, qrc.id
"""

    def currency_rows():
        for row in mysql_rows(args, limited(currency_query, args.limit_relation_rows), currency_columns):
            row["client_quest_name"] = lookup_name(client["quests"], row.get("quest2_id"), "questName")
            row["client_currency_name"] = lookup_name(client["currencies"], row.get("currency_type_id"), "currencyName")
            yield row

    counts["quest_reward_currency_map.csv"] = write_csv(
        args.output_dir / "quest_reward_currency_map.csv",
        [
            "source_reward_id",
            "jabbithole_quest_id",
            "quest2_id",
            "quest_name",
            "client_quest_name",
            "jabbithole_currency_id",
            "currency_type_id",
            "currency_name",
            "client_currency_name",
            "game_reward_id",
            "amount",
            "fixed_reward",
            "last_seen_in",
        ],
        currency_rows(),
    )

    reputation_columns = [
        "source_reward_id",
        "jabbithole_quest_id",
        "quest2_id",
        "quest_name",
        "jabbithole_reputation_reward_id",
        "faction2_id",
        "reputation_name",
        "amount",
        "fixed_reward",
        "last_seen_in",
    ]
    reputation_query = """
SELECT
    qrr.id,
    qrr.quest_id,
    q.game_id,
    q.name,
    qrr.reputation_reward_id,
    rr.game_id,
    rr.name,
    qrr.amount,
    qrr.fixed_reward,
    qrr.last_seen_in
FROM quest_reward_reputations qrr
LEFT JOIN quests q ON q.id = qrr.quest_id
LEFT JOIN reputation_rewards rr ON rr.id = qrr.reputation_reward_id
WHERE qrr.enabled = 'true'
ORDER BY qrr.quest_id, qrr.id
"""

    def reputation_rows():
        for row in mysql_rows(args, limited(reputation_query, args.limit_relation_rows), reputation_columns):
            row["client_quest_name"] = lookup_name(client["quests"], row.get("quest2_id"), "questName")
            row["client_faction_name"] = lookup_name(client["factions"], row.get("faction2_id"), "factionName")
            yield row

    counts["quest_reward_reputation_map.csv"] = write_csv(
        args.output_dir / "quest_reward_reputation_map.csv",
        [
            "source_reward_id",
            "jabbithole_quest_id",
            "quest2_id",
            "quest_name",
            "client_quest_name",
            "jabbithole_reputation_reward_id",
            "faction2_id",
            "reputation_name",
            "client_faction_name",
            "amount",
            "fixed_reward",
            "last_seen_in",
        ],
        reputation_rows(),
    )

    tradeskill_columns = [
        "source_reward_id",
        "jabbithole_quest_id",
        "quest2_id",
        "quest_name",
        "jabbithole_tradeskill_reward_id",
        "tradeskill_id",
        "tradeskill_name",
        "game_reward_id",
        "amount",
        "fixed_reward",
        "last_seen_in",
    ]
    tradeskill_query = """
SELECT
    qrt.id,
    qrt.quest_id,
    q.game_id,
    q.name,
    qrt.tradeskill_reward_id,
    tr.game_id,
    tr.name,
    qrt.game_reward_id,
    qrt.amount,
    qrt.fixed_reward,
    qrt.last_seen_in
FROM quest_reward_tradeskills qrt
LEFT JOIN quests q ON q.id = qrt.quest_id
LEFT JOIN tradeskill_rewards tr ON tr.id = qrt.tradeskill_reward_id
WHERE qrt.enabled = 'true'
ORDER BY qrt.quest_id, qrt.id
"""

    def tradeskill_rows():
        for row in mysql_rows(args, limited(tradeskill_query, args.limit_relation_rows), tradeskill_columns):
            row["client_quest_name"] = lookup_name(client["quests"], row.get("quest2_id"), "questName")
            row["client_tradeskill_name"] = lookup_name(client["tradeskills"], row.get("tradeskill_id"), "tradeskillName")
            yield row

    counts["quest_reward_tradeskill_map.csv"] = write_csv(
        args.output_dir / "quest_reward_tradeskill_map.csv",
        [
            "source_reward_id",
            "jabbithole_quest_id",
            "quest2_id",
            "quest_name",
            "client_quest_name",
            "jabbithole_tradeskill_reward_id",
            "tradeskill_id",
            "tradeskill_name",
            "client_tradeskill_name",
            "game_reward_id",
            "amount",
            "fixed_reward",
            "last_seen_in",
        ],
        tradeskill_rows(),
    )
    return counts


def write_public_event_detail_maps(args: argparse.Namespace, client) -> Dict[str, int]:
    counts: Dict[str, int] = {}

    objective_columns = [
        "jabbithole_public_event_objective_id",
        "jabbithole_public_event_id",
        "public_event_id",
        "jabbithole_public_event_name",
        "public_event_objective_id",
        "jabbithole_objective_text",
        "jabbithole_objective_type",
        "jabbithole_objective_category",
        "last_seen_in",
    ]
    objective_query = """
SELECT
    peo.id,
    peo.public_event_id,
    pe.game_id,
    pe.name,
    peo.game_id,
    peo.text,
    peo.objective_type,
    peo.category,
    peo.last_seen_in
FROM public_event_objectives peo
LEFT JOIN public_events pe ON pe.id = peo.public_event_id
WHERE peo.enabled = 'true'
ORDER BY peo.public_event_id, peo.id
"""

    def objective_rows():
        for row in mysql_rows(args, limited(objective_query, args.limit_relation_rows), objective_columns):
            event_id = to_int(row.get("public_event_id"), -1) or -1
            objective_id = to_int(row.get("public_event_objective_id"), -1) or -1
            event = client["public_events"].get(event_id, {})
            objective = client["public_event_objectives"].get(objective_id, {})
            objective_event_id = to_int(objective.get("publicEventId"))

            if not event and not objective:
                match_status = "unmatched"
            elif not event:
                match_status = "unmatched_event"
            elif not objective:
                match_status = "unmatched_objective"
            elif objective_event_id is not None and objective_event_id != event_id:
                match_status = "event_mismatch"
            else:
                match_status = "matched"

            row["client_public_event_name"] = event.get("publicEventName", "")
            row["match_status"] = match_status
            row["public_event_id_from_objective"] = objective.get("publicEventId", "")
            row["client_objective_text"] = objective.get("publicEventObjectiveText", "")
            row["objective_type"] = objective.get("publicEventObjectiveTypeEnum", "")
            row["objective_flags"] = objective.get("publicEventObjectiveFlags", "")
            row["objective_type_specific_flags"] = objective.get("publicEventObjectiveTypeSpecificFlags", "")
            row["objective_count"] = objective.get("count", "")
            row["objective_object_id"] = objective.get("objectId", "")
            row["world_location_id"] = objective.get("worldLocation2Id", "")
            row["public_event_team_id"] = objective.get("publicEventTeamId", "")
            row["public_event_team_name"] = lookup_name(client["public_event_teams"], objective.get("publicEventTeamId"), "publicEventTeamName")
            row["objective_category"] = objective.get("publicEventObjectiveCategoryEnum", "")
            row["parent_public_event_objective_id"] = objective.get("publicEventObjectiveIdParent", "")
            row["quest_direction_id"] = objective.get("questDirectionId", "")
            row["display_order"] = objective.get("displayOrder", "")
            row["medal_point_value"] = objective.get("medalPointValue", "")
            row["failure_time_ms"] = objective.get("failureTimeMs", "")
            row["target_group_id_reward_pane"] = objective.get("targetGroupIdRewardPane", "")
            row["jabbithole_objective_text"] = display_text(row.get("jabbithole_objective_text"))
            yield row

    counts["public_event_objective_map.csv"] = write_csv(
        args.output_dir / "public_event_objective_map.csv",
        [
            "jabbithole_public_event_objective_id",
            "jabbithole_public_event_id",
            "public_event_id",
            "jabbithole_public_event_name",
            "client_public_event_name",
            "public_event_objective_id",
            "match_status",
            "public_event_id_from_objective",
            "jabbithole_objective_text",
            "client_objective_text",
            "jabbithole_objective_type",
            "objective_type",
            "jabbithole_objective_category",
            "objective_category",
            "objective_flags",
            "objective_type_specific_flags",
            "objective_count",
            "objective_object_id",
            "world_location_id",
            "public_event_team_id",
            "public_event_team_name",
            "parent_public_event_objective_id",
            "quest_direction_id",
            "display_order",
            "medal_point_value",
            "failure_time_ms",
            "target_group_id_reward_pane",
            "last_seen_in",
        ],
        objective_rows(),
    )

    mission_columns = [
        "source_public_event_mission_id",
        "mission_name",
        "description",
        "parent_jabbithole_public_event_id",
        "parent_public_event_id",
        "parent_public_event_name",
        "child_jabbithole_public_event_id",
        "child_public_event_id",
        "child_public_event_name",
        "last_seen_in",
    ]
    mission_query = """
SELECT
    pem.id,
    pem.name,
    pem.description,
    pem.parent_id,
    parent.game_id,
    parent.name,
    pem.public_event_id,
    child.game_id,
    child.name,
    pem.last_seen_in
FROM public_event_missions pem
LEFT JOIN public_events parent ON parent.id = pem.parent_id
LEFT JOIN public_events child ON child.id = pem.public_event_id
WHERE pem.enabled = 'true'
ORDER BY COALESCE(pem.parent_id, pem.public_event_id), pem.id
"""

    def mission_rows():
        for row in mysql_rows(args, limited(mission_query, args.limit_relation_rows), mission_columns):
            row["mission_name"] = display_text(row.get("mission_name"))
            row["description"] = display_text(row.get("description"))
            row["client_parent_public_event_name"] = lookup_name(client["public_events"], row.get("parent_public_event_id"), "publicEventName")
            row["client_child_public_event_name"] = lookup_name(client["public_events"], row.get("child_public_event_id"), "publicEventName")
            yield row

    counts["public_event_mission_map.csv"] = write_csv(
        args.output_dir / "public_event_mission_map.csv",
        [
            "source_public_event_mission_id",
            "mission_name",
            "description",
            "parent_jabbithole_public_event_id",
            "parent_public_event_id",
            "parent_public_event_name",
            "client_parent_public_event_name",
            "child_jabbithole_public_event_id",
            "child_public_event_id",
            "child_public_event_name",
            "client_child_public_event_name",
            "last_seen_in",
        ],
        mission_rows(),
    )

    zone_columns = [
        "source_public_event_zone_id",
        "jabbithole_public_event_id",
        "public_event_id",
        "public_event_name",
        "jabbithole_zone_id",
        "world_zone_id",
        "jabbithole_zone_name",
        "map_asset",
        "continent_id",
        "last_seen_in",
    ]
    zone_query = """
SELECT
    pez.id,
    pez.public_event_id,
    pe.game_id,
    pe.name,
    pez.zone_id,
    z.game_id,
    z.name,
    z.mapasset,
    z.continent_id,
    pez.last_seen_in
FROM public_event_zones pez
LEFT JOIN public_events pe ON pe.id = pez.public_event_id
LEFT JOIN zones z ON z.id = pez.zone_id
WHERE pez.enabled = 'true'
ORDER BY pez.public_event_id, pez.id
"""

    def zone_rows():
        for row in mysql_rows(args, limited(zone_query, args.limit_relation_rows), zone_columns):
            row["client_public_event_name"] = lookup_name(client["public_events"], row.get("public_event_id"), "publicEventName")
            row["client_zone_name"] = lookup_name(client["world_zones"], row.get("world_zone_id"), "worldZoneName")
            yield row

    counts["public_event_zone_map.csv"] = write_csv(
        args.output_dir / "public_event_zone_map.csv",
        [
            "source_public_event_zone_id",
            "jabbithole_public_event_id",
            "public_event_id",
            "public_event_name",
            "client_public_event_name",
            "jabbithole_zone_id",
            "world_zone_id",
            "jabbithole_zone_name",
            "client_zone_name",
            "map_asset",
            "continent_id",
            "last_seen_in",
        ],
        zone_rows(),
    )
    return counts


def write_path_detail_maps(args: argparse.Namespace, client) -> Dict[str, int]:
    counts: Dict[str, int] = {}

    player_path_columns = [
        "jabbithole_player_path_id",
        "path_type",
        "path_name",
        "description",
        "slug",
        "icon",
        "path_missions_count",
        "path_episodes_count",
    ]
    player_path_query = """
SELECT
    id,
    game_id,
    name,
    description,
    slug,
    icon,
    path_missions_count,
    path_episodes_count
FROM player_paths
WHERE enabled = 'true'
ORDER BY game_id
"""

    counts["player_path_map.csv"] = write_csv(
        args.output_dir / "player_path_map.csv",
        player_path_columns,
        mysql_rows(args, player_path_query.strip(), player_path_columns),
    )

    episode_columns = [
        "jabbithole_path_episode_id",
        "path_episode_id",
        "jabbithole_player_path_id",
        "path_type",
        "path_name",
        "jabbithole_name",
        "jabbithole_summary",
        "world_zone_text",
        "datamined_zone_id",
        "world_zone_id",
        "zone_name",
        "path_episode_rewards_count",
        "path_episode_zones_count",
        "path_episode_missions_count",
        "is_dominion",
        "is_exile",
        "last_seen_in",
    ]
    episode_query = """
SELECT
    pe.id,
    pe.game_id,
    pe.player_path_id,
    pp.game_id,
    pp.name,
    pe.name,
    pe.summary,
    pe.world_zone,
    pe.datamined_zone_id,
    z.game_id,
    z.name,
    pe.path_episode_rewards_count,
    pe.path_episode_zones_count,
    pe.path_episode_missions_count,
    pe.is_dominion,
    pe.is_exile,
    pe.last_seen_in
FROM path_episodes pe
LEFT JOIN player_paths pp ON pp.id = pe.player_path_id
LEFT JOIN zones z ON z.id = pe.datamined_zone_id
WHERE pe.enabled = 'true'
ORDER BY pe.player_path_id, pe.id
"""

    def episode_rows():
        for row in mysql_rows(args, episode_query.strip(), episode_columns):
            episode = client["path_episodes"].get(to_int(row.get("path_episode_id"), -1) or -1, {})
            row["client_path_episode_name"] = episode.get("pathEpisodeName", "")
            row["client_path_episode_summary"] = episode.get("pathEpisodeSummary", "")
            row["client_path_type"] = episode.get("pathTypeEnum", "")
            row["client_world_id"] = episode.get("worldId", "")
            row["client_world_zone_id"] = episode.get("worldZoneId", "")
            row["client_world_zone_name"] = lookup_name(client["world_zones"], episode.get("worldZoneId"), "worldZoneName")
            row["match_status"] = "matched" if episode else "unmatched"
            row["jabbithole_name"] = display_text(row.get("jabbithole_name"))
            row["jabbithole_summary"] = display_text(row.get("jabbithole_summary"))
            yield row

    counts["path_episode_map.csv"] = write_csv(
        args.output_dir / "path_episode_map.csv",
        [
            "jabbithole_path_episode_id",
            "path_episode_id",
            "match_status",
            "jabbithole_player_path_id",
            "path_type",
            "client_path_type",
            "path_name",
            "jabbithole_name",
            "client_path_episode_name",
            "jabbithole_summary",
            "client_path_episode_summary",
            "world_zone_text",
            "datamined_zone_id",
            "world_zone_id",
            "zone_name",
            "client_world_id",
            "client_world_zone_id",
            "client_world_zone_name",
            "path_episode_rewards_count",
            "path_episode_zones_count",
            "path_episode_missions_count",
            "is_dominion",
            "is_exile",
            "last_seen_in",
        ],
        episode_rows(),
    )

    mission_columns = [
        "source_relation_id",
        "jabbithole_path_episode_id",
        "path_episode_id",
        "path_episode_name",
        "jabbithole_path_mission_id",
        "path_mission_id",
        "path_mission_name",
        "player_path_id",
        "mission_type",
        "mission_subtype",
        "needed",
        "last_seen_in",
    ]
    mission_query = """
SELECT
    pem.id,
    pem.path_episode_id,
    pe.game_id,
    pe.name,
    pem.path_mission_id,
    pm.game_id,
    pm.name,
    pm.player_path_id,
    pm.mission_type,
    pm.mission_subtype,
    pm.needed,
    pem.last_seen_in
FROM path_episode_missions pem
LEFT JOIN path_episodes pe ON pe.id = pem.path_episode_id
LEFT JOIN path_missions pm ON pm.id = pem.path_mission_id
WHERE pem.enabled = 'true'
ORDER BY pem.path_episode_id, pem.id
"""

    def mission_rows():
        for row in mysql_rows(args, limited(mission_query, args.limit_relation_rows), mission_columns):
            row["client_path_episode_name"] = lookup_name(client["path_episodes"], row.get("path_episode_id"), "pathEpisodeName")
            row["client_path_mission_name"] = lookup_name(client["path_missions"], row.get("path_mission_id"), "pathMissionName")
            yield row

    counts["path_episode_mission_map.csv"] = write_csv(
        args.output_dir / "path_episode_mission_map.csv",
        [
            "source_relation_id",
            "jabbithole_path_episode_id",
            "path_episode_id",
            "path_episode_name",
            "client_path_episode_name",
            "jabbithole_path_mission_id",
            "path_mission_id",
            "path_mission_name",
            "client_path_mission_name",
            "player_path_id",
            "mission_type",
            "mission_subtype",
            "needed",
            "last_seen_in",
        ],
        mission_rows(),
    )

    reward_columns = [
        "source_relation_id",
        "jabbithole_path_episode_id",
        "path_episode_id",
        "path_episode_name",
        "item2_id",
        "amount",
        "last_seen_in",
    ]
    reward_query = """
SELECT
    per.id,
    per.path_episode_id,
    pe.game_id,
    pe.name,
    per.item_id,
    per.amount,
    per.last_seen_in
FROM path_episode_rewards per
LEFT JOIN path_episodes pe ON pe.id = per.path_episode_id
WHERE per.enabled = 'true'
ORDER BY per.path_episode_id, per.id
"""

    def reward_rows():
        for row in mysql_rows(args, limited(reward_query, args.limit_relation_rows), reward_columns):
            row["client_path_episode_name"] = lookup_name(client["path_episodes"], row.get("path_episode_id"), "pathEpisodeName")
            row["item_name"] = item_name(client["items"], row.get("item2_id"))
            yield row

    counts["path_episode_reward_map.csv"] = write_csv(
        args.output_dir / "path_episode_reward_map.csv",
        [
            "source_relation_id",
            "jabbithole_path_episode_id",
            "path_episode_id",
            "path_episode_name",
            "client_path_episode_name",
            "item2_id",
            "item_name",
            "amount",
            "last_seen_in",
        ],
        reward_rows(),
    )

    zone_columns = [
        "source_relation_id",
        "jabbithole_path_episode_id",
        "path_episode_id",
        "path_episode_name",
        "jabbithole_zone_id",
        "world_zone_id",
        "zone_name",
        "map_asset",
        "continent_id",
        "last_seen_in",
    ]
    zone_query = """
SELECT
    pez.id,
    pez.path_episode_id,
    pe.game_id,
    pe.name,
    pez.zone_id,
    z.game_id,
    z.name,
    z.mapasset,
    z.continent_id,
    pez.last_seen_in
FROM path_episode_zones pez
LEFT JOIN path_episodes pe ON pe.id = pez.path_episode_id
LEFT JOIN zones z ON z.id = pez.zone_id
WHERE pez.enabled = 'true'
ORDER BY pez.path_episode_id, pez.id
"""

    def zone_rows():
        for row in mysql_rows(args, limited(zone_query, args.limit_relation_rows), zone_columns):
            row["client_path_episode_name"] = lookup_name(client["path_episodes"], row.get("path_episode_id"), "pathEpisodeName")
            row["client_zone_name"] = lookup_name(client["world_zones"], row.get("world_zone_id"), "worldZoneName")
            yield row

    counts["path_episode_zone_map.csv"] = write_csv(
        args.output_dir / "path_episode_zone_map.csv",
        [
            "source_relation_id",
            "jabbithole_path_episode_id",
            "path_episode_id",
            "path_episode_name",
            "client_path_episode_name",
            "jabbithole_zone_id",
            "world_zone_id",
            "zone_name",
            "client_zone_name",
            "map_asset",
            "continent_id",
            "last_seen_in",
        ],
        zone_rows(),
    )

    unlock_columns = [
        "source_unlock_id",
        "jabbithole_player_path_id",
        "path_type",
        "path_name",
        "path_level",
        "title",
        "description",
        "icon",
        "reward_type",
        "game_id",
        "extra_data",
        "item2_id",
        "jabbithole_spell_id",
        "spell4_id",
        "spell_name",
        "side",
        "character_title_id",
    ]
    unlock_query = """
SELECT
    pu.id,
    pu.player_path_id,
    pp.game_id,
    pp.name,
    pu.level,
    pu.title,
    pu.description,
    pu.icon,
    pu.reward_type,
    pu.game_id,
    pu.extra_data,
    pu.item_id,
    pu.spell_id,
    s.game_id,
    s.name,
    pu.side,
    pu.player_title_id
FROM path_unlocks pu
LEFT JOIN player_paths pp ON pp.id = pu.player_path_id
LEFT JOIN spells s ON s.id = pu.spell_id
WHERE pu.enabled = 'true'
ORDER BY pu.player_path_id, pu.level, pu.id
"""

    def unlock_rows():
        for row in mysql_rows(args, limited(unlock_query, args.limit_relation_rows), unlock_columns):
            row["item_name"] = item_name(client["items"], row.get("item2_id"))
            row["character_title_name"] = lookup_name(client["character_titles"], row.get("character_title_id"), "characterTitleName")
            row["character_title_text"] = lookup_name(client["character_titles"], row.get("character_title_id"), "characterTitleText")
            yield row

    counts["path_unlock_map.csv"] = write_csv(
        args.output_dir / "path_unlock_map.csv",
        [
            "source_unlock_id",
            "jabbithole_player_path_id",
            "path_type",
            "path_name",
            "path_level",
            "title",
            "description",
            "icon",
            "reward_type",
            "game_id",
            "extra_data",
            "item2_id",
            "item_name",
            "jabbithole_spell_id",
            "spell4_id",
            "spell_name",
            "side",
            "character_title_id",
            "character_title_name",
            "character_title_text",
        ],
        unlock_rows(),
    )

    ability_columns = [
        "source_ability_id",
        "jabbithole_player_path_id",
        "path_type",
        "path_name",
        "jabbithole_spell_id",
        "spell4_id",
        "spell_name",
        "jabbithole_base_spell_id",
        "base_spell4_id",
        "game_ability_id",
        "ability_type",
        "tier",
        "path_level",
        "cost_cash",
        "cost_point",
        "tier_bonus",
        "tier_4_bonus",
        "tier_8_bonus",
    ]
    ability_query = """
SELECT
    pa.id,
    pa.player_path_id,
    pp.game_id,
    pp.name,
    pa.spell_id,
    s.game_id,
    s.name,
    pa.base_spell_id,
    bs.game_id,
    pa.game_ability_id,
    pa.ability_type,
    pa.tier,
    pa.level,
    pa.cost_cash,
    pa.cost_point,
    pa.tier_bonus,
    pa.tier_4_bonus,
    pa.tier_8_bonus
FROM path_abilities pa
LEFT JOIN player_paths pp ON pp.id = pa.player_path_id
LEFT JOIN spells s ON s.id = pa.spell_id
LEFT JOIN spells bs ON bs.id = pa.base_spell_id
WHERE pa.enabled = 'true'
ORDER BY pa.player_path_id, pa.level, pa.id
"""

    counts["path_ability_map.csv"] = write_csv(
        args.output_dir / "path_ability_map.csv",
        ability_columns,
        mysql_rows(args, limited(ability_query, args.limit_relation_rows), ability_columns),
    )
    return counts


def write_item_detail_maps(
    args: argparse.Namespace,
    creature_map: Dict[int, Dict[str, object]],
    client,
) -> Dict[str, int]:
    counts: Dict[str, int] = {}

    def taxonomy_rows(table_name: str, client_key: str, client_name_field: str, extra_client_fields: Sequence[str] = ()):
        columns = [
            "jabbithole_id",
            "client_id",
            "jabbithole_name",
            "items_count",
            "enabled",
            "last_seen_in",
        ]
        query = f"""
SELECT
    id,
    game_id,
    name,
    items_count,
    enabled,
    last_seen_in
FROM {table_name}
ORDER BY game_id, id
"""
        for row in mysql_rows(args, query.strip(), columns):
            client_row = client[client_key].get(to_int(row.get("client_id"), -1) or -1, {})
            row["client_name"] = client_row.get(client_name_field, "")
            row["match_status"] = "matched" if client_row else "unmatched"
            for field in extra_client_fields:
                row[field] = client_row.get(field, "")
            yield row

    counts["item_family_map.csv"] = write_csv(
        args.output_dir / "item_family_map.csv",
        [
            "jabbithole_id",
            "client_id",
            "jabbithole_name",
            "client_name",
            "match_status",
            "items_count",
            "flags",
            "vendorMultiplier",
            "turninMultiplier",
            "enabled",
            "last_seen_in",
        ],
        taxonomy_rows("item_families", "item_families", "itemFamilyName", ["flags", "vendorMultiplier", "turninMultiplier"]),
    )

    counts["item_category_map.csv"] = write_csv(
        args.output_dir / "item_category_map.csv",
        [
            "jabbithole_id",
            "client_id",
            "jabbithole_name",
            "client_name",
            "match_status",
            "items_count",
            "item2FamilyId",
            "item_family_name",
            "itemProficiencyId",
            "tradeSkillId",
            "flags",
            "vendorMultiplier",
            "turninMultiplier",
            "armorModifier",
            "armorBase",
            "weaponPowerModifier",
            "weaponPowerBase",
            "enabled",
            "last_seen_in",
        ],
        (
            {
                **row,
                "item_family_name": lookup_name(client["item_families"], row.get("item2FamilyId"), "itemFamilyName"),
            }
            for row in taxonomy_rows(
                "item_categories",
                "item_categories",
                "itemCategoryName",
                [
                    "item2FamilyId",
                    "itemProficiencyId",
                    "tradeSkillId",
                    "flags",
                    "vendorMultiplier",
                    "turninMultiplier",
                    "armorModifier",
                    "armorBase",
                    "weaponPowerModifier",
                    "weaponPowerBase",
                ],
            )
        ),
    )

    counts["item_type_map.csv"] = write_csv(
        args.output_dir / "item_type_map.csv",
        [
            "jabbithole_id",
            "client_id",
            "jabbithole_name",
            "client_name",
            "match_status",
            "items_count",
            "Item2CategoryId",
            "item_category_name",
            "itemSlotId",
            "item_slot_name",
            "flags",
            "vendorMultiplier",
            "turninMultiplier",
            "enabled",
            "last_seen_in",
        ],
        (
            {
                **row,
                "item_category_name": lookup_name(client["item_categories"], row.get("Item2CategoryId"), "itemCategoryName"),
                "item_slot_name": lookup_name(client["item_slots"], row.get("itemSlotId"), "itemSlotName"),
            }
            for row in taxonomy_rows(
                "item_types",
                "item_types",
                "itemTypeName",
                ["Item2CategoryId", "itemSlotId", "flags", "vendorMultiplier", "turninMultiplier"],
            )
        ),
    )

    counts["item_slot_map.csv"] = write_csv(
        args.output_dir / "item_slot_map.csv",
        [
            "jabbithole_id",
            "client_id",
            "jabbithole_name",
            "client_name",
            "match_status",
            "items_count",
            "equippedSlotFlags",
            "armorModifier",
            "itemLevelModifier",
            "slotBonus",
            "glyphSlotBonus",
            "minLevel",
            "enabled",
            "last_seen_in",
        ],
        taxonomy_rows(
            "item_slots",
            "item_slots",
            "itemSlotName",
            ["equippedSlotFlags", "armorModifier", "itemLevelModifier", "slotBonus", "glyphSlotBonus", "minLevel"],
        ),
    )

    effect_columns = [
        "relation_source",
        "source_relation_id",
        "item2_id",
        "jabbithole_spell_id",
        "spell4_id",
        "spell_name",
        "tier",
        "base_spell4_id",
        "effect_name",
        "on_equip",
        "on_use",
        "on_proc",
        "last_seen_in",
    ]
    effect_query = """
SELECT
    'item_effects' AS relation_source,
    ie.id AS source_relation_id,
    ie.item_id AS item2_id,
    ie.spell_id AS jabbithole_spell_id,
    s.game_id AS spell4_id,
    s.name AS spell_name,
    s.tier AS tier,
    s.base_spell_id_game AS base_spell4_id,
    ie.name AS effect_name,
    ie.on_equip AS on_equip,
    ie.on_use AS on_use,
    ie.on_proc AS on_proc,
    ie.last_seen_in AS last_seen_in
FROM item_effects ie
LEFT JOIN spells s ON s.id = ie.spell_id
WHERE ie.enabled = 'true'
UNION ALL
SELECT
    'item_imbuements',
    ii.id,
    ii.item_id,
    ii.spell_id,
    s.game_id,
    s.name,
    s.tier,
    s.base_spell_id_game,
    ii.name,
    ii.on_equip,
    ii.on_use,
    ii.on_proc,
    ii.last_seen_in
FROM item_imbuements ii
LEFT JOIN spells s ON s.id = ii.spell_id
WHERE ii.enabled = 'true'
ORDER BY item2_id, relation_source, source_relation_id
"""

    def effect_rows():
        for row in mysql_rows(args, limited(effect_query, args.limit_relation_rows), effect_columns):
            row["item_name"] = item_name(client["items"], row.get("item2_id"))
            yield row

    counts["item_spell_effect_map.csv"] = write_csv(
        args.output_dir / "item_spell_effect_map.csv",
        [
            "relation_source",
            "source_relation_id",
            "item2_id",
            "item_name",
            "jabbithole_spell_id",
            "spell4_id",
            "spell_name",
            "tier",
            "base_spell4_id",
            "effect_name",
            "on_equip",
            "on_use",
            "on_proc",
            "last_seen_in",
        ],
        effect_rows(),
    )

    drop_columns = [
        "relation_source",
        "source_relation_id",
        "raw_item_id",
        "jabbithole_creature_id",
        "drop_times",
        "last_seen_in",
        "item2_id",
    ]
    drop_query = """
SELECT
    'item_drops' AS relation_source,
    id AS source_relation_id,
    item_id AS raw_item_id,
    creature_id AS jabbithole_creature_id,
    drop_times AS drop_times,
    last_seen_in AS last_seen_in,
    game_id_item AS item2_id
FROM item_drops
WHERE enabled = 'true'
UNION ALL
SELECT
    'item_drop4_drops',
    id,
    item_id,
    creature_id,
    drop_times,
    last_seen_in,
    game_id_item
FROM item_drop4_drops
WHERE enabled = 'true'
UNION ALL
SELECT
    'item_drop5_drops',
    id,
    item_id,
    creature_id,
    drop_times,
    last_seen_in,
    game_id_item
FROM item_drop5_drops
WHERE enabled = 'true'
ORDER BY jabbithole_creature_id, relation_source, source_relation_id
"""

    def drop_rows():
        for row in mysql_rows(args, limited(drop_query, args.limit_relation_rows), drop_columns):
            row = apply_creature_map(row, creature_map)
            row["item_name"] = item_name(client["items"], row.get("item2_id"))
            yield row

    counts["item_drop_source_map.csv"] = write_csv(
        args.output_dir / "item_drop_source_map.csv",
        [
            "relation_source",
            "source_relation_id",
            "raw_item_id",
            "item2_id",
            "item_name",
            "jabbithole_creature_id",
            "creature2_id",
            "source_name",
            "creature2_name",
            "match_status",
            "drop_times",
            "last_seen_in",
        ],
        drop_rows(),
    )

    container_columns = [
        "source_relation_id",
        "raw_container_item_id",
        "container_item2_id",
        "raw_contained_item_id",
        "contained_item2_id",
        "drop_times",
        "last_seen_in",
    ]
    container_query = """
SELECT
    id,
    container_item_id,
    game_id_item_container,
    contained_item_id,
    game_id_item_contained,
    drop_times,
    last_seen_in
FROM item_containers
WHERE enabled = 'true'
ORDER BY game_id_item_container, id
"""

    def container_rows():
        for row in mysql_rows(args, limited(container_query, args.limit_relation_rows), container_columns):
            row["container_item_name"] = item_name(client["items"], row.get("container_item2_id"))
            row["contained_item_name"] = item_name(client["items"], row.get("contained_item2_id"))
            yield row

    counts["item_container_map.csv"] = write_csv(
        args.output_dir / "item_container_map.csv",
        [
            "source_relation_id",
            "raw_container_item_id",
            "container_item2_id",
            "container_item_name",
            "raw_contained_item_id",
            "contained_item2_id",
            "contained_item_name",
            "drop_times",
            "last_seen_in",
        ],
        container_rows(),
    )

    salvage_columns = [
        "source_relation_id",
        "raw_original_item_id",
        "original_item2_id",
        "raw_salvaged_item_id",
        "salvaged_item2_id",
        "drop_times",
        "last_seen_in",
    ]
    salvage_query = """
SELECT
    id,
    original_item_id,
    game_id_item_original,
    salvaged_item_id,
    game_id_item_salvaged,
    drop_times,
    last_seen_in
FROM item_salvages
WHERE enabled = 'true'
ORDER BY game_id_item_original, id
"""

    def salvage_rows():
        for row in mysql_rows(args, limited(salvage_query, args.limit_relation_rows), salvage_columns):
            row["original_item_name"] = item_name(client["items"], row.get("original_item2_id"))
            row["salvaged_item_name"] = item_name(client["items"], row.get("salvaged_item2_id"))
            yield row

    counts["item_salvage_map.csv"] = write_csv(
        args.output_dir / "item_salvage_map.csv",
        [
            "source_relation_id",
            "raw_original_item_id",
            "original_item2_id",
            "original_item_name",
            "raw_salvaged_item_id",
            "salvaged_item2_id",
            "salvaged_item_name",
            "drop_times",
            "last_seen_in",
        ],
        salvage_rows(),
    )

    class_columns = [
        "source_relation_id",
        "item2_id",
        "jabbithole_player_class_id",
        "class_id",
        "class_name",
        "last_seen_in",
    ]
    class_query = """
SELECT
    icr.id,
    icr.item_id,
    icr.player_class_id,
    pc.game_id,
    pc.name,
    icr.last_seen_in
FROM item_class_requirements icr
LEFT JOIN player_classes pc ON pc.id = icr.player_class_id
WHERE icr.enabled = 'true'
ORDER BY icr.item_id, icr.id
"""

    def class_rows():
        for row in mysql_rows(args, limited(class_query, args.limit_relation_rows), class_columns):
            row["item_name"] = item_name(client["items"], row.get("item2_id"))
            row["client_class_name"] = lookup_name(client["classes"], row.get("class_id"), "className")
            yield row

    counts["item_class_requirement_map.csv"] = write_csv(
        args.output_dir / "item_class_requirement_map.csv",
        [
            "source_relation_id",
            "item2_id",
            "item_name",
            "jabbithole_player_class_id",
            "class_id",
            "class_name",
            "client_class_name",
            "last_seen_in",
        ],
        class_rows(),
    )

    tradeskill_columns = [
        "source_relation_id",
        "item2_id",
        "jabbithole_tradeskill_id",
        "tradeskill_id",
        "tradeskill_name",
        "required_tier",
        "last_seen_in",
    ]
    tradeskill_query = """
SELECT
    itr.id,
    itr.item_id,
    itr.tradeskill_id,
    t.game_id,
    t.name,
    itr.tier,
    itr.last_seen_in
FROM item_tradeskill_requirements itr
LEFT JOIN tradeskills t ON t.id = itr.tradeskill_id
WHERE itr.enabled = 'true'
ORDER BY itr.item_id, itr.id
"""

    def tradeskill_rows():
        for row in mysql_rows(args, limited(tradeskill_query, args.limit_relation_rows), tradeskill_columns):
            row["item_name"] = item_name(client["items"], row.get("item2_id"))
            row["client_tradeskill_name"] = lookup_name(client["tradeskills"], row.get("tradeskill_id"), "tradeskillName")
            yield row

    counts["item_tradeskill_requirement_map.csv"] = write_csv(
        args.output_dir / "item_tradeskill_requirement_map.csv",
        [
            "source_relation_id",
            "item2_id",
            "item_name",
            "jabbithole_tradeskill_id",
            "tradeskill_id",
            "tradeskill_name",
            "client_tradeskill_name",
            "required_tier",
            "last_seen_in",
        ],
        tradeskill_rows(),
    )

    chip_columns = [
        "source_relation_id",
        "item2_id",
        "jabbithole_spell_id",
        "spell4_id",
        "spell_name",
        "tier",
        "base_spell4_id",
        "last_seen_in",
    ]
    chip_query = """
SELECT
    ics.id,
    ics.item_id,
    ics.spell_id,
    s.game_id,
    s.name,
    s.tier,
    s.base_spell_id_game,
    ics.last_seen_in
FROM item_chip_spells ics
LEFT JOIN spells s ON s.id = ics.spell_id
ORDER BY ics.item_id, ics.id
"""

    def chip_rows():
        for row in mysql_rows(args, limited(chip_query, args.limit_relation_rows), chip_columns):
            row["item_name"] = item_name(client["items"], row.get("item2_id"))
            yield row

    counts["item_chip_spell_map.csv"] = write_csv(
        args.output_dir / "item_chip_spell_map.csv",
        [
            "source_relation_id",
            "item2_id",
            "item_name",
            "jabbithole_spell_id",
            "spell4_id",
            "spell_name",
            "tier",
            "base_spell4_id",
            "last_seen_in",
        ],
        chip_rows(),
    )

    item_item_relation_columns = [
        "relation_source",
        "source_relation_id",
        "item2_id",
        "related_item2_id",
        "last_seen_in",
    ]
    item_item_relation_query = """
SELECT
    'item_circuits' AS relation_source,
    id AS source_relation_id,
    item_id AS item2_id,
    circuit_id AS related_item2_id,
    last_seen_in AS last_seen_in
FROM item_circuits
UNION ALL
SELECT
    'item_microchips',
    id,
    item_id,
    microchip_id,
    last_seen_in
FROM item_microchips
ORDER BY item2_id, relation_source, source_relation_id
"""

    def item_item_relation_rows():
        for row in mysql_rows(args, limited(item_item_relation_query, args.limit_relation_rows), item_item_relation_columns):
            row["item_name"] = item_name(client["items"], row.get("item2_id"))
            row["related_item_name"] = item_name(client["items"], row.get("related_item2_id"))
            yield row

    counts["item_component_map.csv"] = write_csv(
        args.output_dir / "item_component_map.csv",
        [
            "relation_source",
            "source_relation_id",
            "item2_id",
            "item_name",
            "related_item2_id",
            "related_item_name",
            "last_seen_in",
        ],
        item_item_relation_rows(),
    )

    attribute_columns = [
        "source_relation_id",
        "item2_id",
        "jabbithole_attribute_id",
        "unit_property2_id",
        "attribute_name",
        "attribute_order",
        "attribute_bonus",
        "is_innate",
    ]
    attribute_query = """
SELECT
    iia.id,
    iia.item_id,
    iia.attribute_id,
    a.game_id,
    a.name,
    iia.`order`,
    iia.attribute_bonus,
    iia.is_innate
FROM item_instance_attributes iia
LEFT JOIN attributes a ON a.id = iia.attribute_id
WHERE iia.enabled = 'true'
ORDER BY iia.item_id, iia.`order`, iia.id
"""

    def attribute_rows():
        for row in mysql_rows(args, limited(attribute_query, args.limit_relation_rows), attribute_columns):
            row["item_name"] = item_name(client["items"], row.get("item2_id"))
            row["client_unit_property_name"] = lookup_name(client["unit_properties"], row.get("unit_property2_id"), "unitPropertyName")
            row["client_unit_property_enum"] = lookup_name(client["unit_properties"], row.get("unit_property2_id"), "enumName")
            yield row

    counts["item_instance_attribute_map.csv"] = write_csv(
        args.output_dir / "item_instance_attribute_map.csv",
        [
            "source_relation_id",
            "item2_id",
            "item_name",
            "jabbithole_attribute_id",
            "unit_property2_id",
            "attribute_name",
            "client_unit_property_name",
            "client_unit_property_enum",
            "attribute_order",
            "attribute_bonus",
            "is_innate",
        ],
        attribute_rows(),
    )
    return counts


def write_action_map(args: argparse.Namespace, creature_rows: Sequence[Dict[str, object]], client_creatures: Dict[int, Dict[str, object]]) -> int:
    log("Loading Creature2Action.tbl.sql")
    actions_by_set: Dict[int, List[Dict[str, object]]] = defaultdict(list)
    for row in iter_table_rows(args.client_sql_dir / "Creature2Action.tbl.sql", ACTION_FIELDS):
        set_id = to_int(row.get("creatureActionSetId"))
        if set_id is not None and set_id > 0:
            actions_by_set[set_id].append(row)

    source_ids_by_client: Dict[int, List[str]] = defaultdict(list)
    for row in creature_rows:
        client_id = to_int(row.get("creature2_id"))
        if client_id is not None:
            source_ids_by_client[client_id].append(clean_cell(row.get("jabbithole_creature_id")))

    fieldnames = [
        "creature2_id",
        "creature2_name",
        "jabbithole_creature_ids",
        "creature_action_set_id",
        "action_id",
        "state",
        "event",
        "order_index",
        "delay_ms",
        "action",
        "action_data_00",
        "action_data_01",
        "visual_effect_id",
        "prerequisite_id",
        "description",
    ]

    def rows():
        for client_id, source_ids in sorted(source_ids_by_client.items()):
            creature = client_creatures.get(client_id, {})
            set_id = to_int(creature.get("creature2ActionSetId"))
            if set_id is None or set_id <= 0:
                continue
            for action in sorted(actions_by_set.get(set_id, []), key=lambda item: (to_int(item.get("orderIndex"), 0), to_int(item.get("ID"), 0))):
                yield {
                    "creature2_id": client_id,
                    "creature2_name": creature.get("clientName", ""),
                    "jabbithole_creature_ids": "|".join(sorted(set(source_ids), key=lambda item: int(item) if item.isdigit() else 0)),
                    "creature_action_set_id": set_id,
                    "action_id": action.get("ID", ""),
                    "state": action.get("state", ""),
                    "event": action.get("event", ""),
                    "order_index": action.get("orderIndex", ""),
                    "delay_ms": action.get("delayMS", ""),
                    "action": action.get("action", ""),
                    "action_data_00": action.get("actionData00", ""),
                    "action_data_01": action.get("actionData01", ""),
                    "visual_effect_id": action.get("visualEffectId", ""),
                    "prerequisite_id": action.get("prerequisiteId", ""),
                    "description": action.get("description", ""),
                }

    return write_csv(args.output_dir / "creature_ai_action_map.csv", fieldnames, rows())


def load_spline_first_nodes(sql_dir: Path) -> Dict[int, List[Tuple[int, float, float, float]]]:
    splines = load_client_table(sql_dir, "Spline2.tbl.sql", ["ID", "worldId", "splineType"])
    nodes_by_world: Dict[int, List[Tuple[int, float, float, float]]] = defaultdict(list)
    log("Loading first nodes from Spline2Node.tbl.sql")
    for row in iter_table_rows(sql_dir / "Spline2Node.tbl.sql", ["splineId", "ordinal", "position0", "position1", "position2"]):
        if to_int(row.get("ordinal"), -1) != 0:
            continue
        spline_id = to_int(row.get("splineId"))
        if spline_id is None:
            continue
        spline = splines.get(spline_id)
        if not spline:
            continue
        world_id = to_int(spline.get("worldId"))
        x = to_float(row.get("position0"))
        y = to_float(row.get("position1"))
        z = to_float(row.get("position2"))
        if world_id is None or x is None or y is None or z is None:
            continue
        nodes_by_world[world_id].append((spline_id, x, y, z))
    return nodes_by_world


def write_spline_candidates(args: argparse.Namespace, creature_map: Dict[int, Dict[str, object]]) -> int:
    nodes_by_world = load_spline_first_nodes(args.client_sql_dir)
    cell_size = args.spline_cell_size
    grids: Dict[int, Dict[Tuple[int, int], List[Tuple[int, float, float, float]]]] = {}
    for world_id, nodes in nodes_by_world.items():
        grid: Dict[Tuple[int, int], List[Tuple[int, float, float, float]]] = defaultdict(list)
        for node in nodes:
            _, x, _, z = node
            grid[(math.floor(x / cell_size), math.floor(z / cell_size))].append(node)
        grids[world_id] = grid

    columns = [
        "source_coordinate_id",
        "jabbithole_creature_id",
        "source_name",
        "worldid",
        "x",
        "y",
        "z",
    ]
    query = """
SELECT
    co.id,
    co.location_id,
    c.name,
    c.worldid,
    co.x,
    co.y,
    co.z
FROM coordinates co
JOIN creatures c ON c.id = co.location_id
WHERE co.location_type = 'Creature'
  AND c.worldid IS NOT NULL
  AND c.worldid <> 0
ORDER BY co.id
""".strip()
    fields = [
        "source_coordinate_id",
        "jabbithole_creature_id",
        "creature2_id",
        "source_name",
        "creature2_name",
        "worldid",
        "x",
        "y",
        "z",
        "spline2_id",
        "spline_start_x",
        "spline_start_y",
        "spline_start_z",
        "distance",
        "candidate_note",
    ]

    def rows():
        radius = args.spline_radius
        for row in mysql_rows(args, limited(query, args.limit_spawns), columns):
            row = apply_creature_map(row, creature_map)
            world_id = to_int(row.get("worldid"))
            x = to_float(row.get("x"))
            y = to_float(row.get("y"))
            z = to_float(row.get("z"))
            if world_id is None or x is None or y is None or z is None:
                continue
            grid = grids.get(world_id)
            if not grid:
                continue
            cx = math.floor(x / cell_size)
            cz = math.floor(z / cell_size)
            best = None
            for dx in (-1, 0, 1):
                for dz in (-1, 0, 1):
                    for spline_id, sx, sy, sz in grid.get((cx + dx, cz + dz), []):
                        distance = math.sqrt((x - sx) ** 2 + (y - sy) ** 2 + (z - sz) ** 2)
                        if distance <= radius and (best is None or distance < best[-1]):
                            best = (spline_id, sx, sy, sz, distance)
            if best:
                row["spline2_id"], row["spline_start_x"], row["spline_start_y"], row["spline_start_z"], row["distance"] = best
                row["candidate_note"] = "nearest_spline_start_within_radius"
                yield row

    target = args.output_dir / "creature_spline_candidate_map.csv"
    temp_path = target.with_suffix(target.suffix + ".tmp")
    try:
        count = write_csv(temp_path, fields, rows())
        replace_file_with_retry(temp_path, target)
        return count
    finally:
        if temp_path.exists():
            temp_path.unlink()


def write_coverage_inventory(args: argparse.Namespace) -> Dict[str, int]:
    fieldnames = [
        "source",
        "filename",
        "table_name",
        "file_size_bytes",
        "column_count",
        "coverage_status",
        "notes",
    ]
    rows: List[Dict[str, object]] = []

    for source, folder, status_map in [
        ("wildstar_client_mysql", args.client_sql_dir, CLIENT_FILE_STATUS),
        ("jabbithole_mysql", args.jabbithole_sql_dir, JABBITHOLE_FILE_STATUS),
    ]:
        for path in sorted(folder.glob("*.sql"), key=lambda item: item.name.lower()):
            if source == "wildstar_client_mysql":
                status, notes = client_coverage_status(path.name)
            else:
                status, notes = status_map.get(path.name, ("unmapped", "Not yet consumed by generated maps."))
            table_name = ""
            column_count = ""
            try:
                create_statement = read_create_statement_header(path)
                table_name = parse_table_name(create_statement) or path.stem
                column_count = len(parse_create_columns(create_statement))
            except Exception as exc:  # inventory should never break a mapping run
                table_name = path.stem
                notes = f"{notes} Inventory parse warning: {type(exc).__name__}: {exc}"
            rows.append(
                {
                    "source": source,
                    "filename": path.name,
                    "table_name": table_name,
                    "file_size_bytes": path.stat().st_size,
                    "column_count": column_count,
                    "coverage_status": status,
                    "notes": notes,
                }
            )

    csv_count = write_csv(args.output_dir / "table_coverage_inventory.csv", fieldnames, rows)
    status_counts = Counter((row["source"], row["coverage_status"]) for row in rows)
    lines = [
        "# Table Coverage Inventory",
        "",
        f"Generated: {time.strftime('%Y-%m-%d %H:%M:%S')}",
        "",
        "## Summary",
        "",
    ]
    for source in ["wildstar_client_mysql", "jabbithole_mysql"]:
        source_total = sum(1 for row in rows if row["source"] == source)
        lines.append(f"### {source}")
        lines.append("")
        lines.append(f"- Total SQL files: {source_total:,}")
        for status in ["mapped", "partial", "unmapped"]:
            lines.append(f"- {status}: {status_counts.get((source, status), 0):,}")
        lines.append("")

    lines.extend(
        [
            "## Detail",
            "",
            "| Source | File | Table | Status | Notes |",
            "|---|---|---|---|---|",
        ]
    )
    for row in rows:
        lines.append(
            f"| {row['source']} | `{row['filename']}` | `{row['table_name']}` | {row['coverage_status']} | {row['notes']} |"
        )
    (args.output_dir / "table_coverage_inventory.md").write_text("\n".join(lines) + "\n", encoding="utf-8")
    return {
        "table_coverage_inventory.csv": csv_count,
        "table_coverage_inventory.md": len(rows),
    }


def write_summary(args: argparse.Namespace, counts: Dict[str, int], creature_rows: Sequence[Dict[str, object]]) -> None:
    statuses = Counter(clean_cell(row.get("match_status")) for row in creature_rows)
    summary = {
        "generated_at": time.strftime("%Y-%m-%d %H:%M:%S"),
        "client_sql_dir": str(args.client_sql_dir),
        "jabbithole_sql_dir": str(args.jabbithole_sql_dir),
        "jabbithole_db": args.jabbithole_db,
        "limits": {
            "limit_creatures": args.limit_creatures,
            "limit_relation_rows": args.limit_relation_rows,
            "limit_spawns": args.limit_spawns,
            "include_spline_candidates": args.include_spline_candidates,
        },
        "match_status_counts": dict(statuses),
        "output_counts": counts,
    }
    (args.output_dir / "run_manifest.json").write_text(json.dumps(summary, indent=2), encoding="utf-8")

    lines = [
        "# WildStar Data Mapping Summary",
        "",
        f"- Client dump: `{args.client_sql_dir}`",
        f"- Jabbithole dump: `{args.jabbithole_sql_dir}`",
        f"- Jabbithole DB used for relation queries: `{args.jabbithole_db}`",
        "",
        "## Creature Name Bridge",
        "",
    ]
    for status, count in sorted(statuses.items()):
        lines.append(f"- `{status}`: {count:,}")
    lines.extend(["", "## Outputs", ""])
    for filename, count in sorted(counts.items()):
        lines.append(f"- `{filename}`: {count:,} rows")
    lines.extend(
        [
            "",
            "## Notes",
            "",
            "- Jabbithole creature IDs are not Creature2 IDs. The bridge is Jabbithole creature name -> `StringsenUS.LocalizedText` -> `Creature2.localizedTextIdName`, then scored by faction, level range, difficulty, and datacube when available.",
            "- `world_entity_candidate.csv` and `world_entity_stats_candidate.csv` are import candidates. Entity IDs are blank because the target world database must assign non-conflicting IDs.",
            "- `creature_spline_candidate_map.csv`, when enabled, is proximity-based only. The source data does not expose a direct creature-to-spline foreign key.",
            "- `client_source_*_map.csv` files provide complete client-table coverage for rows that do not yet need curated semantic bridge logic.",
        ]
    )
    (args.output_dir / "mapping_summary.md").write_text("\n".join(lines) + "\n", encoding="utf-8")


def validate_sources(args: argparse.Namespace) -> None:
    required_client = [
        "en-US.bin.sql",
        "Creature2.tbl.sql",
        "Creature2Action.tbl.sql",
        "Creature2DisplayGroupEntry.tbl.sql",
        "Creature2OutfitGroupEntry.tbl.sql",
        "Item2.tbl.sql",
        "Item2Family.tbl.sql",
        "Item2Category.tbl.sql",
        "Item2Type.tbl.sql",
        "ItemSlot.tbl.sql",
        "Class.tbl.sql",
        "UnitProperty2.tbl.sql",
        "Spell4.tbl.sql",
        "Spell4Base.tbl.sql",
        "Spell4Effects.tbl.sql",
        "SpellEffectType.tbl.sql",
        "SpellLevel.tbl.sql",
        "Spell4TierRequirements.tbl.sql",
        "Spell4SpellTypes.tbl.sql",
        "Quest2.tbl.sql",
        "QuestObjective.tbl.sql",
        "Quest2Reward.tbl.sql",
        "QuestCategory.tbl.sql",
        "Episode.tbl.sql",
        "EpisodeQuest.tbl.sql",
        "CurrencyType.tbl.sql",
        "Faction2.tbl.sql",
        "Faction2Relationship.tbl.sql",
        "Tradeskill.tbl.sql",
        "TradeskillSchematic2.tbl.sql",
        "TradeskillMaterial.tbl.sql",
        "TradeskillMaterialCategory.tbl.sql",
        "TradeskillTalentTier.tbl.sql",
        "TradeskillTier.tbl.sql",
        "TradeskillAdditive.tbl.sql",
        "TradeskillCatalyst.tbl.sql",
        "TradeskillCatalystOrdering.tbl.sql",
        "TradeskillBonus.tbl.sql",
        "TradeskillAchievementLayout.tbl.sql",
        "TradeskillAchievementReward.tbl.sql",
        "PathMission.tbl.sql",
        "PathEpisode.tbl.sql",
        "PathReward.tbl.sql",
        "PathLevel.tbl.sql",
        "CharacterTitle.tbl.sql",
        "CharacterTitleCategory.tbl.sql",
        "Achievement.tbl.sql",
        "AchievementChecklist.tbl.sql",
        "AchievementGroup.tbl.sql",
        "AchievementCategory.tbl.sql",
        "AchievementSubGroup.tbl.sql",
        "AchievementText.tbl.sql",
        "HousingDecorType.tbl.sql",
        "HousingDecorInfo.tbl.sql",
        "HousingDecorLimitCategory.tbl.sql",
        "HousingPlugItem.tbl.sql",
        "HousingWallpaperInfo.tbl.sql",
        "HousingResidenceInfo.tbl.sql",
        "HousingBuild.tbl.sql",
        "HousingResource.tbl.sql",
        "HousingContributionType.tbl.sql",
        "HousingContributionInfo.tbl.sql",
        "HousingPropertyInfo.tbl.sql",
        "HousingNeighborhoodInfo.tbl.sql",
        "HousingMapInfo.tbl.sql",
        "HousingPlotType.tbl.sql",
        "HousingPlotInfo.tbl.sql",
        "HousingMannequinPose.tbl.sql",
        "HousingWarplotBossToken.tbl.sql",
        "HousingWarplotPlugInfo.tbl.sql",
        "DyeColorRamp.tbl.sql",
        "PetFlair.tbl.sql",
        "PublicEvent.tbl.sql",
        "PublicEventObjective.tbl.sql",
        "PublicEventTeam.tbl.sql",
        "PublicEventDepot.tbl.sql",
        "PublicEventVirtualItemDepot.tbl.sql",
        "VirtualItem.tbl.sql",
        "Challenge.tbl.sql",
        "ChallengeTier.tbl.sql",
        "World.tbl.sql",
        "WorldZone.tbl.sql",
        "WorldLocation2.tbl.sql",
        "BindPoint.tbl.sql",
        "TaxiNode.tbl.sql",
        "TaxiRoute.tbl.sql",
        "CityDirection.tbl.sql",
        "QuestDirection.tbl.sql",
        "QuestDirectionEntry.tbl.sql",
        "QuestHub.tbl.sql",
        "GenericMap.tbl.sql",
        "GenericMapNode.tbl.sql",
        "MapContinent.tbl.sql",
        "MapZone.tbl.sql",
        "MapZonePOI.tbl.sql",
        "MapZoneSprite.tbl.sql",
        "MapZoneWorldJoin.tbl.sql",
        "MapZoneHex.tbl.sql",
        "MapZoneHexGroup.tbl.sql",
        "MapZoneHexGroupEntry.tbl.sql",
        "MapZoneLevelBand.tbl.sql",
        "MapZoneNemesisRegion.tbl.sql",
        "ZoneCompletion.tbl.sql",
        "SoundZoneKit.tbl.sql",
        "WorldSocket.tbl.sql",
        "WorldLayer.tbl.sql",
        "WorldClutter.tbl.sql",
        "WorldSky.tbl.sql",
        "WorldWaterEnvironment.tbl.sql",
        "WorldWaterFog.tbl.sql",
        "WorldWaterLayer.tbl.sql",
        "WorldWaterType.tbl.sql",
        "WorldWaterWake.tbl.sql",
    ]
    required_jabbit = [
        "creatures.sql",
        "coordinates.sql",
        "versioned_item_drops.sql",
        "versioned_vendor_items.sql",
        "creature_spells.sql",
        "spells.sql",
        "player_classes.sql",
        "class_abilities.sql",
        "class_amps.sql",
        "class_unlocks.sql",
        "rune_set_spells.sql",
        "achievement_groups.sql",
        "achievements.sql",
        "achievement_titles.sql",
        "character_achievements.sql",
        "contracts.sql",
        "contract_rewards.sql",
        "contract_creatures.sql",
        "housing_decor_types.sql",
        "housing_decors.sql",
        "housing_enhancements.sql",
        "housing_skies.sql",
        "dyes.sql",
        "flairs.sql",
        "mounts.sql",
        "pets.sql",
        "titles.sql",
        "quest_starter_creatures.sql",
        "quest_finisher_creatures.sql",
        "quest_creatures.sql",
        "quest_objectives.sql",
        "quest_reward_items.sql",
        "quest_reward_currencies.sql",
        "quest_reward_reputations.sql",
        "quest_reward_tradeskills.sql",
        "currencies.sql",
        "reputation_rewards.sql",
        "tradeskill_rewards.sql",
        "tradeskills.sql",
        "schematics.sql",
        "schematic_materials.sql",
        "schematic_circuits.sql",
        "schematic_microchips.sql",
        "tradeskill_talent_tiers.sql",
        "tradeskill_talents.sql",
        "tradeskill_techtree_groups.sql",
        "tradeskill_techtree_items.sql",
        "tradeskill_techtree_schematics.sql",
        "path_mission_creatures.sql",
        "player_paths.sql",
        "path_episodes.sql",
        "path_episode_missions.sql",
        "path_episode_rewards.sql",
        "path_episode_zones.sql",
        "path_unlocks.sql",
        "path_abilities.sql",
        "public_event_creatures.sql",
        "public_event_objectives.sql",
        "public_event_missions.sql",
        "public_event_zones.sql",
        "challenges.sql",
        "challenge_creatures.sql",
        "challenge_reward_items.sql",
        "challenge_rewards.sql",
        "challenge_item_rewards.sql",
        "challenge_reward_track_challenge_rewards.sql",
        "_attributes_.sql",
        "item_categories.sql",
        "item_families.sql",
        "item_types.sql",
        "item_slots.sql",
        "item_drops.sql",
        "item_drop4_drops.sql",
        "item_drop5_drops.sql",
        "item_effects.sql",
        "item_imbuements.sql",
        "item_containers.sql",
        "item_salvages.sql",
        "item_class_requirements.sql",
        "item_tradeskill_requirements.sql",
        "item_chip_spells.sql",
        "item_circuits.sql",
        "item_microchips.sql",
        "item_instance_attributes.sql",
        "zones.sql",
        "attribute_contributions.sql",
        "attribute_milestones.sql",
        "continents.sql",
        "factions.sql",
        "faction_reward_items.sql",
        "item_instance_sigils.sql",
        "mapzone_pois.sql",
        "quest_call_zones.sql",
        "quest_categories.sql",
        "quest_episodes.sql",
        "quest_zones.sql",
        "vendor_items.sql",
        "versioned_item_drop_aggregates.sql",
        "_characters_.sql",
        ".sql",
        "items.sql",
        "schema_migrations.sql",
    ]
    missing = []
    for filename in required_client:
        if not (args.client_sql_dir / filename).exists():
            missing.append(str(args.client_sql_dir / filename))
    for filename in required_jabbit:
        if not (args.jabbithole_sql_dir / filename).exists():
            missing.append(str(args.jabbithole_sql_dir / filename))
    if missing:
        raise FileNotFoundError("Missing required split dump files:\n" + "\n".join(missing))
    if not Path(args.mysql_exe).exists():
        raise FileNotFoundError(f"MySQL client not found: {args.mysql_exe}")


def parse_args(argv: Optional[Sequence[str]] = None) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo-root", type=Path, default=Path.cwd())
    parser.add_argument("--client-sql-dir", type=Path)
    parser.add_argument("--jabbithole-sql-dir", type=Path)
    parser.add_argument("--output-dir", type=Path)
    parser.add_argument("--review-dir", type=Path)
    parser.add_argument("--mysql-exe", type=Path, default=Path(DEFAULT_MYSQL))
    parser.add_argument("--host", default="127.0.0.1")
    parser.add_argument("--port", type=int, default=3306)
    parser.add_argument("--user", default="bankai")
    parser.add_argument("--password", default="bankai")
    parser.add_argument("--jabbithole-db", default="jabbithole")
    parser.add_argument("--limit-creatures", type=int, default=None)
    parser.add_argument("--limit-relation-rows", type=int, default=None)
    parser.add_argument("--limit-spawns", type=int, default=None)
    parser.add_argument("--include-spline-candidates", action="store_true")
    parser.add_argument("--spline-radius", type=float, default=25.0)
    parser.add_argument("--spline-cell-size", type=float, default=50.0)
    parser.add_argument("--review-candidate-limit", type=int, default=8)
    args = parser.parse_args(argv)

    args.repo_root = args.repo_root.resolve()
    args.client_sql_dir = (args.client_sql_dir or args.repo_root / "wildstar_client_mysql").resolve()
    args.jabbithole_sql_dir = (args.jabbithole_sql_dir or args.repo_root / "jabbithole_mysql").resolve()
    args.output_dir = (args.output_dir or args.repo_root / "Tools" / "DataMapping" / "output").resolve()
    args.review_dir = (args.review_dir or args.repo_root / "Tools" / "DataMapping" / "review").resolve()
    return args


def main(argv: Optional[Sequence[str]] = None) -> int:
    args = parse_args(argv)
    validate_sources(args)
    args.output_dir.mkdir(parents=True, exist_ok=True)

    client = load_client_sources(args)
    jabbithole_creatures = load_jabbithole_creatures(args)
    creature_overrides, creature_override_audit_rows = load_creature_bridge_overrides(args, client["creatures"])
    creature_rows, creature_map, creature_review_rows = build_creature_map(
        args,
        jabbithole_creatures,
        client["creatures"],
        creature_overrides,
    )

    counts: Dict[str, int] = {}
    counts["creature_map.csv"] = write_csv(
        args.output_dir / "creature_map.csv",
        list(creature_rows[0].keys()) if creature_rows else [],
        creature_rows,
    )
    counts["creature_bridge_review.csv"] = write_csv(
        args.output_dir / "creature_bridge_review.csv",
        CREATURE_BRIDGE_REVIEW_FIELDS,
        creature_review_rows,
    )
    counts["creature_bridge_override_audit.csv"] = write_csv(
        args.output_dir / "creature_bridge_override_audit.csv",
        [
            "source_file",
            "row_number",
            "source_table",
            "source_id",
            "chosen_creature2_id",
            "decision",
            "status",
            "note",
            "reason",
            "reviewer",
            "reviewed_at",
        ],
        creature_override_audit_rows,
    )
    counts["creature_client_metadata_map.csv"] = write_creature_metadata(args, creature_rows, client["creatures"])
    counts.update(write_reference_maps(args, client))
    counts.update(write_world_location_reference_maps(args, client))
    counts.update(write_spawn_maps(args, creature_map, client["creatures"]))
    counts.update(write_relation_maps(args, creature_map, client))
    counts.update(write_spell_detail_maps(args, client))
    counts.update(write_achievement_contract_maps(args, creature_map, client))
    counts.update(write_housing_cosmetic_maps(args, client))
    counts.update(write_tradeskill_schematic_maps(args, client))
    counts.update(write_world_faction_maps(args, creature_map, client))
    counts.update(write_quest_detail_maps(args, client))
    counts.update(write_public_event_detail_maps(args, client))
    counts.update(write_path_detail_maps(args, client))
    counts.update(write_item_detail_maps(args, creature_map, client))
    counts.update(write_challenge_maps(args, creature_map, client))
    counts.update(write_generic_client_source_maps(args, client))
    counts["creature_ai_action_map.csv"] = write_action_map(args, creature_rows, client["creatures"])

    if args.include_spline_candidates:
        counts["creature_spline_candidate_map.csv"] = write_spline_candidates(args, creature_map)

    counts.update(write_coverage_inventory(args))
    write_summary(args, counts, creature_rows)
    log(f"Done. Wrote mapping outputs to {args.output_dir}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
