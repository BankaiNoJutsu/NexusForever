using System.Reflection;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Tests.TableContracts;

public class GameTableManagerGameDataContractTests
{
    [Theory]
    [InlineData(nameof(GameTableManager.ArchiveArticle), typeof(ArchiveArticleEntry), "ArchiveArticle.tbl")]
    [InlineData(nameof(GameTableManager.ArchiveEntry), typeof(ArchiveEntryEntry), "ArchiveEntry.tbl")]
    [InlineData(nameof(GameTableManager.ArchiveEntryUnlockRule), typeof(ArchiveEntryUnlockRuleEntry), "ArchiveEntryUnlockRule.tbl")]
    [InlineData(nameof(GameTableManager.DailyLoginReward), typeof(DailyLoginRewardEntry), "DailyLoginReward.tbl")]
    [InlineData(nameof(GameTableManager.HousingContributionInfo), typeof(HousingContributionInfoEntry), "HousingContributionInfo.tbl")]
    [InlineData(nameof(GameTableManager.ItemRandomStat), typeof(ItemRandomStatEntry), "ItemRandomStat.tbl")]
    [InlineData(nameof(GameTableManager.ItemRandomStatGroup), typeof(ItemRandomStatGroupEntry), "ItemRandomStatGroup.tbl")]
    [InlineData(nameof(GameTableManager.QuestDirection), typeof(QuestDirectionEntry), "QuestDirection.tbl")]
    [InlineData(nameof(GameTableManager.QuestDirectionEntry), typeof(QuestDirectionEntryEntry), "QuestDirectionEntry.tbl")]
    [InlineData(nameof(GameTableManager.MatchTypeRewardRotationContent), typeof(MatchTypeRewardRotationContentEntry), "MatchTypeRewardRotationContent.tbl")]
    [InlineData(nameof(GameTableManager.RewardRotationContent), typeof(RewardRotationContentEntry), "RewardRotationContent.tbl")]
    [InlineData(nameof(GameTableManager.RewardRotationEssence), typeof(RewardRotationEssenceEntry), "RewardRotationEssence.tbl")]
    [InlineData(nameof(GameTableManager.RewardRotationItem), typeof(RewardRotationItemEntry), "RewardRotationItem.tbl")]
    [InlineData(nameof(GameTableManager.RewardRotationModifier), typeof(RewardRotationModifierEntry), "RewardRotationModifier.tbl")]
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
