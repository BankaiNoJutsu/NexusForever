using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Group;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Prerequisite.Check;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Tests.TestSupport;

namespace NexusForever.Game.Tests.Prerequisite;

// Native evidence (Decomp/Analysis/PREREQUISITE_GROUP_SIZE_20261107.md): prerequisite type 20 (evaluator case 0x14 -> FUN_14049e820) compares the member count of the local player's group (the count at +0x60
// of the group manager, the same manager type 42 reads) with Value through the integer helper (count first); with no group manager the count is 0. ObjectId is not read (0 in all 8 retail slots). Retail:
// comparisons > x4, == 1, >= 1, < 1, <= 1 with Value 0 to 4. Status: Implemented (native body read).
public class PrerequisiteCheckGroupSizeTests
{
    [Theory]
    [InlineData(3, PrerequisiteComparison.GreaterThan, 2u, true)]
    [InlineData(3, PrerequisiteComparison.GreaterThan, 3u, false)]
    [InlineData(3, PrerequisiteComparison.Equal, 3u, true)]
    [InlineData(3, PrerequisiteComparison.GreaterThanOrEqual, 4u, false)]
    [InlineData(3, PrerequisiteComparison.LessThan, 4u, true)]
    [InlineData(3, PrerequisiteComparison.LessThanOrEqual, 2u, false)]
    [InlineData(3, PrerequisiteComparison.NotEqual, 3u, false)]
    [InlineData(3, (PrerequisiteComparison)0, 3u, false)]
    public void ComparesTheMemberCountOfThePlayersGroup(int members, PrerequisiteComparison comparison, uint value, bool expected)
    {
        IPlayer player = Player(groupAssociation: 99ul);

        Assert.Equal(expected, new PrerequisiteCheckGroupSize(Groups(members)).Meets(player, comparison, value, 0u, new PrerequisiteParameters()));
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, 0u, true)]        // no group: size 0
    [InlineData(PrerequisiteComparison.GreaterThan, 0u, false)]
    [InlineData(PrerequisiteComparison.LessThan, 1u, true)]
    [InlineData(PrerequisiteComparison.GreaterThanOrEqual, 1u, false)]
    public void APlayerWithoutAGroupHasSizeZero(PrerequisiteComparison comparison, uint value, bool expected)
    {
        IPlayer player = Player(groupAssociation: 0ul);

        Assert.Equal(expected, new PrerequisiteCheckGroupSize(Groups(members: 5)).Meets(player, comparison, value, 0u, new PrerequisiteParameters()));
    }

    [Fact]
    public void AGroupTheStateManagerDoesNotKnowHasSizeZero()
    {
        IPlayer player = Player(groupAssociation: 99ul);
        IGroupStateManager manager = RecordingDispatchProxy<IGroupStateManager>.Create(out var proxy);
        proxy.SetMethodHandler(nameof(IGroupStateManager.TryGetGroup), _ => false);

        Assert.True(new PrerequisiteCheckGroupSize(manager).Meets(player, PrerequisiteComparison.Equal, 0u, 0u, new PrerequisiteParameters()));
    }

    private static IPlayer Player(ulong groupAssociation)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var proxy);
        proxy.SetProperty(nameof(IPlayer.GroupAssociation), groupAssociation);
        return player;
    }

    private static IGroupStateManager Groups(int members)
    {
        IGroupStateManager manager = RecordingDispatchProxy<IGroupStateManager>.Create(out var proxy);
        proxy.SetMethodHandler(nameof(IGroupStateManager.TryGetGroup), args =>
        {
            args[1] = new GroupLootState
            {
                GroupId = (ulong)args[0],
                Leader  = new Identity { RealmId = 1, Id = 1ul },
                Members = Enumerable.Range(1, members)
                    .Select(i => new GroupLootMember { Identity = new Identity { RealmId = 1, Id = (ulong)i } })
                    .ToList()
            };
            return true;
        });
        return manager;
    }
}
