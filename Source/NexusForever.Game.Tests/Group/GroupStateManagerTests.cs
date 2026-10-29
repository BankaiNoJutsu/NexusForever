using NexusForever.Game;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Group;
using NexusForever.Game.Group;
using NexusForever.Game.Static.Group;
using NexusForever.Game.Static.Setting;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.WorldServer.Network.Internal.Handler.Group;
using InternalGroup = NexusForever.Network.Internal.Message.Group.Shared.Group;
using InternalGroupMember = NexusForever.Network.Internal.Message.Group.Shared.GroupMember;
using InternalIdentity = NexusForever.Network.Internal.Message.Shared.Identity;

namespace NexusForever.Game.Tests.Group;

public class GroupStateManagerTests
{
    [Fact]
    public void SecondarySnapshotsAndRemovalDoNotReplacePrimaryAssociation()
    {
        var manager = new GroupStateManager();
        GroupLootState primary = BuildGroup(HarvestLootRule.FirstTagger);
        var secondary = new GroupLootState
        {
            GroupId = 66, Leader = primary.Leader, Members = primary.Members,
            NormalRule = LootRule.FreeForAll
        };
        manager.UpdateGroup(primary);
        manager.UpdateGroup(secondary);
        Assert.True(manager.TryGetGroupForCharacter(primary.Leader, out var inferred));
        Assert.Equal(primary.GroupId, inferred.GroupId);

        manager.UpdateGroupAssociation(primary, primary.Leader);
        manager.UpdateGroup(secondary);
        manager.RemoveMember(secondary.GroupId, primary.Leader);
        manager.RemoveGroup(secondary.GroupId);

        Assert.True(manager.TryGetGroupForCharacter(primary.Leader, out var selected));
        Assert.Equal(primary.GroupId, selected.GroupId);
    }

    [Fact]
    public void AssociationRemovalIsNotUndoneByMembershipRefresh()
    {
        var manager = new GroupStateManager();
        GroupLootState group = BuildGroup(HarvestLootRule.RoundRobin);
        manager.UpdateGroupAssociation(group, group.Leader);
        manager.RemoveCharacter(group.Leader);
        manager.UpdateGroup(group);

        Assert.False(manager.TryGetGroupForCharacter(group.Leader, out _));
        Assert.True(manager.TryGetGroup(group.GroupId, out var membership));
        Assert.True(membership.HasMember(group.Leader));
    }

    [Fact]
    public void CachedSnapshotCannotBeMutatedByReaders()
    {
        var manager = new GroupStateManager();
        GroupLootState group = BuildGroup(HarvestLootRule.FirstTagger);
        manager.UpdateGroup(group);
        manager.TryGetGroup(group.GroupId, out var snapshot);

        Assert.Throws<NotSupportedException>(() => ((IList<GroupLootMember>)snapshot.Members).Clear());
        Assert.Equal(2, snapshot.Members.Count);

        manager.RemoveMember(group.GroupId, group.Leader);
        Assert.True(manager.TryGetGroup(group.GroupId, out var afterRemoval));
        Assert.Throws<NotSupportedException>(() => ((IList<GroupLootMember>)afterRemoval.Members).Clear());
        Assert.Single(afterRemoval.Members);
        Assert.Equal(2, snapshot.Members.Count);
    }

    [Fact]
    public void ConcurrentSnapshotsKeepPrimaryAssociationAndStableReaders()
    {
        var manager = new GroupStateManager();
        GroupLootState group = BuildGroup(HarvestLootRule.RoundRobin);
        manager.UpdateGroupAssociation(group, group.Leader);
        Parallel.For(0, 500, _ =>
        {
            manager.UpdateGroup(group);
            Assert.True(manager.TryGetGroupForCharacter(group.Leader, out var snapshot));
            Assert.Equal(2, snapshot.Members.Count);
        });
    }

    [Fact]
    public void UpdateGroup_PreservesHarvestLootState()
    {
        var manager = new GroupStateManager();
        GroupLootState group = BuildGroup(HarvestLootRule.RoundRobin);

        manager.UpdateGroup(group);

        Assert.True(manager.TryGetGroup(group.GroupId, out GroupLootState stored));
        Assert.Equal(HarvestLootRule.RoundRobin, stored.HarvestRule);
        Assert.Equal(WorldDifficulty.Veteran, stored.InstanceDifficulty);
        Assert.Equal(GroupFlags.Raid, stored.Flags);
    }

    [Fact]
    public void WithoutMember_PreservesGroupLootState()
    {
        GroupLootState group = BuildGroup(HarvestLootRule.FirstTagger);

        GroupLootState updated = group.WithoutMember(new Identity
        {
            RealmId = 1,
            Id      = 20ul
        });

        Assert.Equal(HarvestLootRule.FirstTagger, updated.HarvestRule);
        Assert.Equal(WorldDifficulty.Veteran, updated.InstanceDifficulty);
        Assert.Equal(GroupFlags.Raid, updated.Flags);
        Assert.Single(updated.Members);
    }

    [Fact]
    public void ToGroupLootState_PreservesGroupLootState()
    {
        var group = new InternalGroup
        {
            Id               = 77ul,
            Flags            = GroupFlags.Raid,
            NormalRule       = LootRule.NeedBeforeGreed,
            ThresholdRule    = LootRule.Master,
            ThresholdQuality = LootThreshold.Superb,
            HarvestRule      = HarvestLootRule.RoundRobin,
            InstanceDifficulty = WorldDifficulty.Veteran,
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
        Assert.Equal(WorldDifficulty.Veteran, state.InstanceDifficulty);
        Assert.Equal(GroupFlags.Raid, state.Flags);
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
            InstanceDifficulty = WorldDifficulty.Veteran,
            Flags            = GroupFlags.Raid,
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
