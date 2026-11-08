using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Prerequisite.Check;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Game.Tests.TestSupport;

namespace NexusForever.Game.Tests.Prerequisite;

// Prerequisite 199 (native FUN_1404a0b70): flag = the public event exists and its +0x1d0 == 1, where +0x1d0 is the Reason of the last ServerPublicEventEnd the client received
// for that event (default 2 = failure; written only by the event's init and by the End handler FUN_1405f30a0). Equal returns the flag, NotEqual its negation, any other comparison
// is false. Value is not read (always 1 in the 14 retail slots). Decomp/Analysis/PUBLIC_EVENT_NATIVE_READ_20261108.md.
public class PublicEventLastEndPrerequisiteTests
{
    private static readonly IPrerequisiteParameters None = new PrerequisiteParameters();

    [Theory]
    [InlineData(PublicEventRemoveReason.Success, true)]
    [InlineData(PublicEventRemoveReason.Failure, false)]
    [InlineData(PublicEventRemoveReason.LeftArea, false)]
    [InlineData(PublicEventRemoveReason.Logout, false)]
    public void EqualMeetsOnlyWhenTheLastEndWasASuccess(PublicEventRemoveReason last, bool expected)
    {
        IPlayer player = Player((40u, last));
        var check = new PrerequisiteCheckPublicEvent199();

        Assert.Equal(expected, check.Meets(player, PrerequisiteComparison.Equal, 1u, 40u, None));
        Assert.Equal(!expected, check.Meets(player, PrerequisiteComparison.NotEqual, 1u, 40u, None));
        Assert.False(check.Meets(player, PrerequisiteComparison.GreaterThan, 1u, 40u, None));
    }

    [Fact]
    public void AnEventWithNoEndYetIsNotASuccessAndTheValueIsIgnored()
    {
        IPlayer player = Player((40u, PublicEventRemoveReason.Success));
        var check = new PrerequisiteCheckPublicEvent199();

        Assert.False(check.Meets(player, PrerequisiteComparison.Equal, 1u, 41u, None));
        Assert.True(check.Meets(player, PrerequisiteComparison.NotEqual, 1u, 41u, None));
        Assert.True(check.Meets(player, PrerequisiteComparison.Equal, 0u, 40u, None)); // Value is not part of the test
    }

    [Fact]
    public void ObjectIdsAboveTheEventIdRangeNeverMeet()
    {
        IPlayer player = Player((40u, PublicEventRemoveReason.Success));

        Assert.False(new PrerequisiteCheckPublicEvent199().Meets(player, PrerequisiteComparison.Equal, 1u, uint.MaxValue, None));
    }

    private static IPlayer Player(params (uint EventId, PublicEventRemoveReason Reason)[] ends)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var proxy);
        proxy.SetMethodHandler(nameof(IPlayer.GetPublicEventEndReason),
            args => ends.Where(e => e.EventId == (uint)args[0]).Select(e => (PublicEventRemoveReason?)e.Reason).LastOrDefault());
        return player;
    }
}
