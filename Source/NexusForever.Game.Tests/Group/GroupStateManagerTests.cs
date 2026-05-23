using NexusForever.Game;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Group;
using NexusForever.Game.Group;
using NexusForever.Game.Static.Group;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.WorldServer.Network.Internal.Handler.Group;
using InternalGroup = NexusForever.Network.Internal.Message.Group.Shared.Group;
using InternalGroupMember = NexusForever.Network.Internal.Message.Group.Shared.GroupMember;
using InternalIdentity = NexusForever.Network.Internal.Message.Shared.Identity;

namespace NexusForever.Game.Tests.Group;

public class GroupStateManagerTests
{
    [Fact]
    public void UpdateGroup_PreservesHarvestLootRule()
    {
        var manager = new GroupStateManager();
        GroupLootState group = BuildGroup(HarvestLootRule.RoundRobin);

        manager.UpdateGroup(group);

        Assert.True(manager.TryGetGroup(group.GroupId, out GroupLootState stored));
        Assert.Equal(HarvestLootRule.RoundRobin, stored.HarvestRule);
    }

    [Fact]
    public void WithoutMember_PreservesHarvestLootRule()
    {
        GroupLootState group = BuildGroup(HarvestLootRule.FirstTagger);

        GroupLootState updated = group.WithoutMember(new Identity
        {
            RealmId = 1,
            Id      = 20ul
        });

        Assert.Equal(HarvestLootRule.FirstTagger, updated.HarvestRule);
        Assert.Single(updated.Members);
    }

    [Fact]
    public void ToGroupLootState_PreservesHarvestLootRule()
    {
        var group = new InternalGroup
        {
            Id               = 77ul,
            NormalRule       = LootRule.NeedBeforeGreed,
            ThresholdRule    = LootRule.Master,
            ThresholdQuality = LootThreshold.Superb,
            HarvestRule      = HarvestLootRule.RoundRobin,
            Leader           = new InternalIdentity
            {
                RealmId = 1,
                Id      = 10ul
            },
            Members =
            [
                new InternalGroupMember
                {
                    Identity = new InternalIdentity
                    {
                        RealmId = 1,
                        Id      = 10ul
                    },
                    GroupIndex = 0u
                }
            ]
        };

        GroupLootState state = group.ToGroupLootState();

        Assert.Equal(HarvestLootRule.RoundRobin, state.HarvestRule);
    }

    [Fact]
    public void ResolveHarvestLootRecipient_FirstTagger_PrefersHarvester()
    {
        var manager = new GroupStateManager();
        GroupLootState group = BuildGroup(HarvestLootRule.FirstTagger);
        manager.UpdateGroup(group);

        IPlayer harvester = CreatePlayer(20ul);
        GroupLootMember winner = manager.ResolveHarvestLootRecipient(
            group,
            harvester,
            group.Members);

        Assert.Equal(20ul, winner.Identity.Id);
    }

    [Fact]
    public void ResolveHarvestLootRecipient_RoundRobin_RotatesEligibleMembers()
    {
        var manager = new GroupStateManager();
        GroupLootState group = BuildGroup(HarvestLootRule.RoundRobin);
        manager.UpdateGroup(group);

        IPlayer firstHarvester = CreatePlayer(10ul);
        GroupLootMember firstWinner = manager.ResolveHarvestLootRecipient(group, firstHarvester, group.Members);
        Assert.Equal(10ul, firstWinner.Identity.Id);

        IPlayer secondHarvester = CreatePlayer(20ul);
        GroupLootMember secondWinner = manager.ResolveHarvestLootRecipient(group, secondHarvester, group.Members);
        Assert.Equal(20ul, secondWinner.Identity.Id);
    }

    private static IPlayer CreatePlayer(ulong characterId)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), characterId);
        playerProxy.SetProperty(nameof(IPlayer.Identity), new Identity
        {
            RealmId = 1,
            Id      = characterId
        });
        return player;
    }

    private static GroupLootState BuildGroup(HarvestLootRule harvestRule)
    {
        var leader = new Identity
        {
            RealmId = 1,
            Id      = 10ul
        };

        return new GroupLootState
        {
            GroupId          = 55ul,
            NormalRule       = LootRule.NeedBeforeGreed,
            ThresholdRule    = LootRule.Master,
            ThresholdQuality = LootThreshold.Excellent,
            HarvestRule      = harvestRule,
            Leader           = leader,
            Members =
            [
                new GroupLootMember
                {
                    Identity   = leader,
                    GroupIndex = 0u
                },
                new GroupLootMember
                {
                    Identity = new Identity
                    {
                        RealmId = 1,
                        Id      = 20ul
                    },
                    GroupIndex = 1u
                }
            ]
        };
    }
}
