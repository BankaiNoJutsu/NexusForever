using System.Reflection;
using System.Runtime.CompilerServices;
using NexusForever.Game.Account.Reward;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Tests.Account.Reward;

public class RewardRotationContentContextBuilderTests
{
    [Fact]
    public void CollectContentIds_MergesDirectAndReferencedContentForContentType()
    {
        RewardRotationContentContextSources sources = CreateSources();
        SetEntries(sources.RewardRotationContent, new RewardRotationContentEntry
        {
            Id = 100u,
            ContentTypeEnum = 2u
        }, new RewardRotationContentEntry
        {
            Id = 200u,
            ContentTypeEnum = 3u
        });
        SetEntries(sources.WorldZone, new WorldZoneEntry
        {
            RewardRotationContentId = 100u
        });
        SetEntries(sources.World, new WorldEntry
        {
            RewardRotationContentId = 100u
        });
        SetEntries(sources.PublicEvent, new PublicEventEntry
        {
            RewardRotationContentId = 100u
        });
        SetEntries(sources.MatchTypeRewardRotationContent, new MatchTypeRewardRotationContentEntry
        {
            RewardRotationContentIdRandomNormal = 100u,
            RewardRotationContentIdRandomVeteran = 0u
        });

        List<uint> contentIds = RewardRotationContentContextBuilder.CollectContentIds(sources, 2u);

        Assert.Equal(new[] { 100u }, contentIds);
    }

    [Fact]
    public void BuildContexts_ChunksContentIdsToWireBudget()
    {
        RewardRotationContentContextSources sources = CreateSources();
        SetEntries(sources.RewardRotationContent,
            new RewardRotationContentEntry { Id = 1u, ContentTypeEnum = 0u },
            new RewardRotationContentEntry { Id = 2u, ContentTypeEnum = 0u },
            new RewardRotationContentEntry { Id = 3u, ContentTypeEnum = 0u },
            new RewardRotationContentEntry { Id = 4u, ContentTypeEnum = 0u },
            new RewardRotationContentEntry { Id = 5u, ContentTypeEnum = 0u },
            new RewardRotationContentEntry { Id = 6u, ContentTypeEnum = 0u },
            new RewardRotationContentEntry { Id = 7u, ContentTypeEnum = 0u });

        List<ServerRewardRotationContentContext> contexts = RewardRotationContentContextBuilder.BuildContexts(sources, 0u);

        Assert.Equal(2, contexts.Count);
        Assert.Equal(new[] { 1u, 2u, 3u, 4u, 5u }, contexts[0].ContentIds);
        Assert.Equal(new[] { 6u, 7u }, contexts[1].ContentIds);
    }

    [Fact]
    public void Build_AssignsRewardRotationIndexAndCorrelatedThrottleMetadata()
    {
        RewardRotationContentContextSources sources = CreateSources();
        SetEntries(sources.RewardRotationContent, new RewardRotationContentEntry
        {
            Id = 42u,
            ContentTypeEnum = 1u
        });

        ServerRewardRotationContentContext packet = RewardRotationContentContextBuilder.Build(sources, 1u);

        Assert.Equal(1u, packet.RewardRotationIndex);
        Assert.Equal(new[] { 42u }, packet.ContentIds);
        Assert.NotEqual(0u, packet.UInt0);
        Assert.Equal(1000u, packet.UInt1);
        Assert.Equal(1000u, packet.UInt3);
        Assert.False(packet.Flag);
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
            RewardRotationContent = CreateTable<RewardRotationContentEntry>(),
            WorldZone = CreateTable<WorldZoneEntry>(),
            World = CreateTable<WorldEntry>(),
            PublicEvent = CreateTable<PublicEventEntry>(),
            MatchTypeRewardRotationContent = CreateTable<MatchTypeRewardRotationContentEntry>()
        };
    }

    private static GameTable<T> CreateTable<T>() where T : class, new()
    {
        return (GameTable<T>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<T>));
    }
}
