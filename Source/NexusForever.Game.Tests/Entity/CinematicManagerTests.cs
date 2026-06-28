using NexusForever.Game.Abstract.Cinematic;
using NexusForever.Game.Abstract.Cinematic.Cinematics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Cinematic;
using NexusForever.Game.Tests.TestSupport;

namespace NexusForever.Game.Tests.Entity;

public class CinematicManagerTests
{
    [Fact]
    public void QueueCinematic_NorthernWildsIntroQueuedTwice_SuppressesDuplicatePlayback()
    {
        AssertDuplicateSurfaceStarterIntroSuppressed<INorthernWildsOnCreate>();
    }

    [Fact]
    public void QueueCinematic_CrimsonIsleIntroQueuedTwice_SuppressesDuplicatePlayback()
    {
        AssertDuplicateSurfaceStarterIntroSuppressed<ICrimsonIsleOnCreate>();
    }

    [Fact]
    public void QueueCinematic_EverstarGroveIntroQueuedTwice_SuppressesDuplicatePlayback()
    {
        AssertDuplicateSurfaceStarterIntroSuppressed<IEverstarGroveOnCreate>();
    }

    [Fact]
    public void QueueCinematic_LevianBayIntroQueuedTwice_SuppressesDuplicatePlayback()
    {
        AssertDuplicateSurfaceStarterIntroSuppressed<ILevianBayOnCreate>();
    }

    private static void AssertDuplicateSurfaceStarterIntroSuppressed<TCinematic>()
        where TCinematic : class, ICinematicBase
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        var manager = new CinematicManager(player);
        TCinematic first = RecordingDispatchProxy<TCinematic>.Create(out RecordingDispatchProxy<TCinematic> firstProxy);
        TCinematic duplicate = RecordingDispatchProxy<TCinematic>.Create(out RecordingDispatchProxy<TCinematic> duplicateProxy);

        manager.QueueCinematic(first);
        manager.QueueCinematic(duplicate);

        Assert.Single(firstProxy.GetInvocations(nameof(ICinematicBase.StartPlayback)));
        Assert.Empty(duplicateProxy.GetInvocations(nameof(ICinematicBase.StartPlayback)));

        manager.HandleClientCinematicState(CinematicState.Ended);

        Assert.Single(firstProxy.GetInvocations(nameof(ICinematicBase.StartPlayback)));
        Assert.Empty(duplicateProxy.GetInvocations(nameof(ICinematicBase.StartPlayback)));
    }
}
