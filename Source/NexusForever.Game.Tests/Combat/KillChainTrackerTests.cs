using NexusForever.Game.Combat;

namespace NexusForever.Game.Tests.Combat;

public class KillChainTrackerTests
{
    [Fact]
    public void RecordKill_WithinWindow_IncrementsKillCount()
    {
        var tracker = new KillChainTracker();
        DateTimeOffset start = DateTimeOffset.UtcNow;

        Assert.Equal(1u, tracker.RecordKill(start, KillChainTracker.DefaultWindow));
        Assert.Equal(2u, tracker.RecordKill(start.AddSeconds(1d), KillChainTracker.DefaultWindow));
        Assert.Equal(3u, tracker.RecordKill(start.AddSeconds(2d), KillChainTracker.DefaultWindow));
    }

    [Fact]
    public void RecordKill_AfterWindow_ResetsKillCount()
    {
        var tracker = new KillChainTracker();
        DateTimeOffset start = DateTimeOffset.UtcNow;

        Assert.Equal(1u, tracker.RecordKill(start, TimeSpan.FromSeconds(1d)));
        Assert.Equal(2u, tracker.RecordKill(start.AddMilliseconds(500d), TimeSpan.FromSeconds(1d)));
        Assert.Equal(1u, tracker.RecordKill(start.AddSeconds(2d), TimeSpan.FromSeconds(1d)));
    }
}
