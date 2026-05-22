using System.Reflection;
using System.Runtime.CompilerServices;
using NexusForever.Database.Auth.Model;
using NexusForever.Game.Account.Reward;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Tests.Account.Reward;

public class RewardRotationEntryStateBuilderTests
{
    [Fact]
    public void GetGrantFlagsForRewardType_MatchesClientBitmaskSemantics()
    {
        Assert.Equal(RewardRotationEntryStateBuilder.GrantFlagItemOrModifier,
            RewardRotationEntryStateBuilder.GetGrantFlagsForRewardType(RewardRotationScheduleBuilder.RewardTypeItem));
        Assert.Equal(RewardRotationEntryStateBuilder.GrantFlagEssence,
            RewardRotationEntryStateBuilder.GetGrantFlagsForRewardType(RewardRotationScheduleBuilder.RewardTypeEssence));
        Assert.Equal(RewardRotationEntryStateBuilder.GrantFlagItemOrModifier,
            RewardRotationEntryStateBuilder.GetGrantFlagsForRewardType(RewardRotationScheduleBuilder.RewardTypeModifier));
    }

    [Fact]
    public void BuildGrantedRows_EmitsOneGrantRowPerScheduleEntryForMatchingContentType()
    {
        RewardRotationContentContextSources sources = CreateSources();
        SetEntries(sources.RewardRotationContent, new RewardRotationContentEntry
        {
            Id = 10u,
            ContentTypeEnum = 2u
        });

        var schedule = new ServerRewardRotationScheduleArray
        {
            Entries =
            {
                new ServerRewardRotationScheduleArray.ScheduleRow
                {
                    ContentId = 10u,
                    RewardKeyId = 99u,
                    Duration = 2f,
                    RewardType = RewardRotationScheduleBuilder.RewardTypeEssence,
                    Value = 500u
                }
            }
        };

        ServerRewardRotationEntryStateArray entryState = RewardRotationEntryStateBuilder.BuildGrantedRows(2u, schedule, sources);

        Assert.Single(entryState.Entries);
        ServerRewardRotationEntryStateArray.EntryStateRow row = entryState.Entries[0];
        Assert.Equal(2u, row.TypeId);
        Assert.Equal(10u, row.ContentId);
        Assert.Equal(99u, row.RewardTypeId);
        Assert.Equal(RewardRotationScheduleBuilder.RewardTypeEssence, row.State);
        Assert.Equal(RewardRotationEntryStateBuilder.GrantFlagEssence, row.Value);
    }

    [Fact]
    public void BuildFromPersistedGrants_EmitsStoredGrantRows()
    {
        ServerRewardRotationEntryStateArray entryState = RewardRotationEntryStateBuilder.BuildFromPersistedGrants(
            new[]
            {
                new AccountRewardRotationGrantModel
                {
                    RewardRotationIndex = 3u,
                    ContentId = 40u,
                    RewardKeyId = 50u,
                    RewardType = RewardRotationScheduleBuilder.RewardTypeModifier,
                    GrantFlags = RewardRotationEntryStateBuilder.GrantFlagItemOrModifier
                }
            },
            3u);

        Assert.Single(entryState.Entries);
        Assert.Equal(50u, entryState.Entries[0].RewardTypeId);
        Assert.Equal(RewardRotationScheduleBuilder.RewardTypeModifier, entryState.Entries[0].State);
    }

    [Fact]
    public void BuildEmpty_ReturnsZeroEntryStateRows()
    {
        ServerRewardRotationEntryStateArray entryState = RewardRotationEntryStateBuilder.BuildEmpty();

        Assert.Empty(entryState.Entries);
    }

    private static void SetEntries<T>(GameTable<T> table, params T[] entries) where T : class, new()
    {
        PropertyInfo property = typeof(GameTable<T>).GetProperty(nameof(GameTable<T>.Entries), BindingFlags.Instance | BindingFlags.Public);
        property?.SetValue(table, entries);
    }

    private static RewardRotationContentContextSources CreateSources()
    {
        return new RewardRotationContentContextSources
        {
            RewardRotationContent = CreateTable<RewardRotationContentEntry>()
        };
    }

    private static GameTable<T> CreateTable<T>() where T : class, new()
    {
        return (GameTable<T>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<T>));
    }
}
