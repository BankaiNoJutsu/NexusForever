using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Group;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Prerequisite.Check;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Abstract;

namespace NexusForever.Game.Tests.Prerequisite;

// Native evidence (Decomp/Analysis/PREREQUISITE_GROUP_LEADER_20261107.md): type 42 (evaluator case 0x2a, FUN_14049e850) looks the local player up in the group manager's member array (0 when there is
// no manager or no entry), reads the leader entry (0 when the group has no members), and returns comparison 1: the two are equal, 2: they differ, anything else false. Value is not read. With no group
// both entries are 0, so a player without a group counts as the leader. The single retail slot is Equal with Value 0.
public class PrerequisiteCheckIsGroupLeaderTests
{
    private static readonly Identity Me = new() { RealmId = 1, Id = 2ul };
    private static readonly Identity Other = new() { RealmId = 1, Id = 3ul };

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, 0u, true)]
    [InlineData(PrerequisiteComparison.Equal, 1u, true)]       // Value is not read
    [InlineData(PrerequisiteComparison.NotEqual, 0u, false)]
    [InlineData(PrerequisiteComparison.GreaterThan, 0u, false)]
    public void APlayerWithoutAGroupCountsAsTheLeader(PrerequisiteComparison comparison, uint value, bool expected)
    {
        Assert.Equal(expected, Check(Me, groupAssociation: 0ul, leader: null).Meets(Player(Me, 0ul), comparison, value, 0u, new PrerequisiteParameters()));
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, 0u, true)]
    [InlineData(PrerequisiteComparison.Equal, 1u, true)]
    [InlineData(PrerequisiteComparison.NotEqual, 0u, false)]
    public void TheGroupLeaderIsTheLeader(PrerequisiteComparison comparison, uint value, bool expected)
    {
        Assert.Equal(expected, Check(Me, groupAssociation: 99ul, leader: Me).Meets(Player(Me, 99ul), comparison, value, 0u, new PrerequisiteParameters()));
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, 0u, false)]
    [InlineData(PrerequisiteComparison.Equal, 1u, false)]
    [InlineData(PrerequisiteComparison.NotEqual, 0u, true)]
    [InlineData(PrerequisiteComparison.LessThan, 0u, false)]
    public void AGroupMemberWhoIsNotTheLeaderIsNotTheLeader(PrerequisiteComparison comparison, uint value, bool expected)
    {
        Assert.Equal(expected, Check(Me, groupAssociation: 99ul, leader: Other).Meets(Player(Me, 99ul), comparison, value, 0u, new PrerequisiteParameters()));
    }

    [Fact]
    public void AGroupTheStateManagerDoesNotKnowIsNotLedByThePlayer()
    {
        Assert.False(Check(Me, groupAssociation: 99ul, leader: null).Meets(Player(Me, 99ul), PrerequisiteComparison.Equal, 0u, 0u, new PrerequisiteParameters()));
    }

    private static IPlayer Player(Identity identity, ulong groupAssociation)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Identity), identity);
        playerProxy.SetProperty(nameof(IPlayer.GroupAssociation), groupAssociation);
        return player;
    }

    private static PrerequisiteCheckIsGroupLeader Check(Identity me, ulong groupAssociation, Identity leader)
    {
        IGroupStateManager groupStateManager = RecordingDispatchProxy<IGroupStateManager>.Create(out var groupProxy);
        groupProxy.SetMethodHandler(nameof(IGroupStateManager.TryGetGroup), args =>
        {
            if (leader == null)
                return false;

            args[1] = new GroupLootState { GroupId = (ulong)args[0], Leader = leader };
            return true;
        });

        return new PrerequisiteCheckIsGroupLeader(groupStateManager);
    }
}
