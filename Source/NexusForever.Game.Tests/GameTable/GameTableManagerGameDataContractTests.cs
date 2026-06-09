using System.Reflection;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Tests.TableContracts;

public class GameTableManagerGameDataContractTests
{
    [Theory]
    [InlineData(nameof(GameTableManager.ActionBarShortcutSet), typeof(ActionBarShortcutSetEntry), "ActionBarShortcutSet.tbl")]
    [InlineData(nameof(GameTableManager.ArchiveArticle), typeof(ArchiveArticleEntry), "ArchiveArticle.tbl")]
    [InlineData(nameof(GameTableManager.ArchiveEntry), typeof(ArchiveEntryEntry), "ArchiveEntry.tbl")]
    [InlineData(nameof(GameTableManager.ArchiveEntryUnlockRule), typeof(ArchiveEntryUnlockRuleEntry), "ArchiveEntryUnlockRule.tbl")]
    [InlineData(nameof(GameTableManager.ArchiveLink), typeof(ArchiveLinkEntry), "ArchiveLink.tbl")]
    [InlineData(nameof(GameTableManager.CCStates), typeof(CCStatesEntry), "CCStates.tbl")]
    [InlineData(nameof(GameTableManager.Challenge), typeof(ChallengeEntry), "Challenge.tbl")]
    [InlineData(nameof(GameTableManager.ChallengeTier), typeof(ChallengeTierEntry), "ChallengeTier.tbl")]
    [InlineData(nameof(GameTableManager.DailyLoginReward), typeof(DailyLoginRewardEntry), "DailyLoginReward.tbl")]
    [InlineData(nameof(GameTableManager.EldanAugmentationCategory), typeof(EldanAugmentationCategoryEntry), "EldanAugmentationCategory.tbl")]
    [InlineData(nameof(GameTableManager.EpisodeQuest), typeof(EpisodeQuestEntry), "EpisodeQuest.tbl")]
    [InlineData(nameof(GameTableManager.GenericMapNode), typeof(GenericMapNodeEntry), "GenericMapNode.tbl")]
    [InlineData(nameof(GameTableManager.GenericUnlockSet), typeof(GenericUnlockSetEntry), "GenericUnlockSet.tbl")]
    [InlineData(nameof(GameTableManager.HousingContributionInfo), typeof(HousingContributionInfoEntry), "HousingContributionInfo.tbl")]
    [InlineData(nameof(GameTableManager.ItemRandomStat), typeof(ItemRandomStatEntry), "ItemRandomStat.tbl")]
    [InlineData(nameof(GameTableManager.ItemRandomStatGroup), typeof(ItemRandomStatGroupEntry), "ItemRandomStatGroup.tbl")]
    [InlineData(nameof(GameTableManager.MatchTypeRewardRotationContent), typeof(MatchTypeRewardRotationContentEntry), "MatchTypeRewardRotationContent.tbl")]
    [InlineData(nameof(GameTableManager.PathScientistCreatureInfo), typeof(PathScientistCreatureInfoEntry), "PathScientistCreatureInfo.tbl")]
    [InlineData(nameof(GameTableManager.PathSettlerInfrastructure), typeof(PathSettlerInfrastructureEntry), "PathSettlerInfrastructure.tbl")]
    [InlineData(nameof(GameTableManager.PeriodicQuestGroup), typeof(PeriodicQuestGroupEntry), "PeriodicQuestGroup.tbl")]
    [InlineData(nameof(GameTableManager.QuestDirection), typeof(QuestDirectionEntry), "QuestDirection.tbl")]
    [InlineData(nameof(GameTableManager.QuestDirectionEntry), typeof(QuestDirectionEntryEntry), "QuestDirectionEntry.tbl")]
    [InlineData(nameof(GameTableManager.ResourceConversion), typeof(ResourceConversionEntry), "ResourceConversion.tbl")]
    [InlineData(nameof(GameTableManager.RewardRotationContent), typeof(RewardRotationContentEntry), "RewardRotationContent.tbl")]
    [InlineData(nameof(GameTableManager.RewardRotationEssence), typeof(RewardRotationEssenceEntry), "RewardRotationEssence.tbl")]
    [InlineData(nameof(GameTableManager.RewardRotationItem), typeof(RewardRotationItemEntry), "RewardRotationItem.tbl")]
    [InlineData(nameof(GameTableManager.RewardRotationModifier), typeof(RewardRotationModifierEntry), "RewardRotationModifier.tbl")]
    [InlineData(nameof(GameTableManager.Spell4EffectGroupList), typeof(Spell4EffectGroupListEntry), "Spell4EffectGroupList.tbl")]
    [InlineData(nameof(GameTableManager.Spell4GroupList), typeof(Spell4GroupListEntry), "Spell4GroupList.tbl")]
    [InlineData(nameof(GameTableManager.Spell4Thresholds), typeof(Spell4ThresholdsEntry), "Spell4Thresholds.tbl")]
    [InlineData(nameof(GameTableManager.Tradeskill), typeof(TradeskillEntry), "Tradeskill.tbl")]
    [InlineData(nameof(GameTableManager.TradeskillAchievementReward), typeof(TradeskillAchievementRewardEntry), "TradeskillAchievementReward.tbl")]
    [InlineData(nameof(GameTableManager.TradeskillAdditive), typeof(TradeskillAdditiveEntry), "TradeskillAdditive.tbl")]
    [InlineData(nameof(GameTableManager.TradeskillBonus), typeof(TradeskillBonusEntry), "TradeskillBonus.tbl")]
    [InlineData(nameof(GameTableManager.TradeskillCatalyst), typeof(TradeskillCatalystEntry), "TradeskillCatalyst.tbl")]
    [InlineData(nameof(GameTableManager.TradeskillSchematic2), typeof(TradeskillSchematic2Entry), "TradeskillSchematic2.tbl")]
    [InlineData(nameof(GameTableManager.TradeskillTalentTier), typeof(TradeskillTalentTierEntry), "TradeskillTalentTier.tbl")]
    [InlineData(nameof(GameTableManager.TradeskillTier), typeof(TradeskillTierEntry), "TradeskillTier.tbl")]
    [InlineData(nameof(GameTableManager.ZoneCompletion), typeof(ZoneCompletionEntry), "ZoneCompletion.tbl")]
    public void RuntimeRequiredTables_LoadThroughDefaultInitialise(string propertyName, Type entryType, string expectedDefaultFileName)
    {
        PropertyInfo property = typeof(GameTableManager).GetProperty(propertyName);

        Assert.NotNull(property);
        Assert.Equal(typeof(GameTable<>).MakeGenericType(entryType), property.PropertyType);

        GameDataAttribute attribute = property.GetCustomAttribute<GameDataAttribute>();
        Assert.NotNull(attribute);
        Assert.True(string.IsNullOrWhiteSpace(attribute.FileName));
        Assert.Equal(expectedDefaultFileName, $"{property.Name}.tbl");
    }
}
