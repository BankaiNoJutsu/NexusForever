using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Entity;
using NexusForever.Game.Map;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Misc;

namespace NexusForever.Game.Tests.Map;

public sealed class PlayerMapAdmissionStateTests
{
    [Fact]
    public void PreAdmissionRequestIsIgnoredAndDoesNotLeakIntoLaterBootstrap()
    {
        var state = new PlayerMapAdmissionState();
        state.Queue();
        Assert.False(state.RequestEntry(false));
        ulong generation = state.BeginBootstrap()!.Value;
        Assert.False(state.FinishBootstrap(generation, true));
        Assert.True(state.RequestEntry(true));
        Assert.False(state.RequestEntry(true));
    }

    [Fact]
    public void AssignedMapRequestIsCoalescedUntilEntireBootstrapFinishes()
    {
        var state = new PlayerMapAdmissionState();
        state.Queue();
        ulong generation = state.BeginBootstrap()!.Value;
        Assert.False(state.RequestEntry(true));
        Assert.False(state.RequestEntry(true));
        Assert.True(state.FinishBootstrap(generation, true));
        Assert.False(state.RequestEntry(true));
        Assert.False(state.FinishBootstrap(generation, true));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CancellationInvalidatesQueuedAndDeferredRequests(bool bootstrap)
    {
        var state = new PlayerMapAdmissionState();
        ulong old = state.Queue();
        if (bootstrap)
        {
            state.BeginBootstrap();
            state.RequestEntry(true);
        }
        state.Cancel();
        Assert.False(state.IsCurrent(old));
        Assert.False(state.RequestEntry(true));
        Assert.Null(state.BeginBootstrap());
        if (bootstrap)
        {
            Assert.Equal(old, state.BootstrapGeneration);
            Assert.False(state.FinishBootstrap(old, true));
            Assert.Null(state.BootstrapGeneration);
        }
    }

    [Fact]
    public void RecoveryTeleportKeepsNewAdmissionAndRejectsOldDeferredRequest()
    {
        var state = new PlayerMapAdmissionState();
        ulong first = state.Queue();
        state.BeginBootstrap();
        state.RequestEntry(true);
        ulong next = state.Queue();
        Assert.False(state.FinishBootstrap(first, true));
        Assert.True(state.Queued);
        Assert.Equal(next, state.BeginBootstrap());
        Assert.False(state.FinishBootstrap(next, true));
        Assert.True(state.RequestEntry(true));
        Assert.False(state.RequestEntry(true));
    }

    [Fact]
    public void FailedBootstrapNeverAcceptsDeferredOrLaterEntry()
    {
        var state = new PlayerMapAdmissionState();
        ulong generation = state.Queue();
        state.BeginBootstrap();
        state.RequestEntry(true);
        Assert.False(state.FinishBootstrap(generation, false));
        Assert.False(state.RequestEntry(true));
    }

    [Fact]
    public void FailedTeleportInvalidatesItsQueueWithoutPoisoningFutureAdmissions()
    {
        var state = new PlayerMapAdmissionState();
        ulong failed = state.Queue();
        state.AbortQueue();
        Assert.False(state.IsCurrent(failed));
        Assert.False(state.RequestEntry(true));
        ulong retry = state.Queue();
        Assert.Equal(retry, state.BeginBootstrap());
        state.FinishBootstrap(retry, true);
        Assert.True(state.RequestEntry(true));
    }

    [Fact]
    public void ReconnectObjectIsIndependentOfCanceledOldObject()
    {
        var old = new PlayerMapAdmissionState();
        old.Queue();
        old.Cancel();
        var replacement = new PlayerMapAdmissionState();
        ulong generation = replacement.Queue();
        replacement.BeginBootstrap();
        replacement.FinishBootstrap(generation, true);
        Assert.True(replacement.RequestEntry(true));
        Assert.False(old.RequestEntry(true));
    }

    [Fact]
    public void RealPlayerHandlerDefersAssignedMapAcknowledgementUntilReady()
    {
        var player = (Player)RuntimeHelpers.GetUninitializedObject(typeof(Player));
        var state = new PlayerMapAdmissionState();
        typeof(Player).GetField("mapAdmissionState", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(player, state);
        typeof(GridEntity).GetProperty(nameof(GridEntity.Map))!.SetValue(player,
            RecordingDispatchProxy<IBaseMap>.Create(out _));
        typeof(GridEntity).GetProperty(nameof(GridEntity.Guid))!.SetValue(player, 37u);
        IGameSession gameSession = RecordingDispatchProxy<IGameSession>.Create(out var gameSessionProxy);
        typeof(Player).GetProperty(nameof(Player.Session))!.SetValue(player, gameSession);
        player.IsLoading = true;
        ulong generation = state.Queue();
        state.BeginBootstrap();
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out var proxy);
        proxy.SetProperty(nameof(IWorldSession.Player), player);
        var handler = new ClientEnteredWorldHandler();

        handler.HandleMessage(session, new ClientEnteredWorld());
        handler.HandleMessage(session, new ClientEnteredWorld());

        Assert.Empty(gameSessionProxy.Invocations);
        Assert.True(state.FinishBootstrap(generation, true));
        Assert.False(state.RequestEntry(true));
    }
    [Theory]
    [InlineData("achievement")]
    [InlineData("quest")]
    [InlineData("path")]
    public void ReentrantWorldZoneCancellationStopsPathAndTutorialContinuations(string boundary)
    {
        var player = (Player)RuntimeHelpers.GetUninitializedObject(typeof(Player));
        var state = new PlayerMapAdmissionState();
        typeof(Player).GetField("mapAdmissionState", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(player, state);
        typeof(GridEntity).GetField("visibleEntities", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(player, new Dictionary<uint, IGridEntity>());
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out var mapProxy);
        mapProxy.SetProperty(nameof(IBaseMap.Entry), new WorldEntry { Id = 3460 });
        typeof(GridEntity).GetProperty(nameof(GridEntity.Map))!.SetValue(player, map);
        typeof(GridEntity).GetProperty(nameof(GridEntity.Guid))!.SetValue(player, 37u);
        typeof(WorldEntity).GetProperty(nameof(WorldEntity.Zone))!.SetValue(player, new WorldZoneEntry { Id = 12 });
        typeof(Player).GetProperty(nameof(Player.Session))!.SetValue(player, RecordingDispatchProxy<IGameSession>.Create(out _));
        var achievement = RecordingDispatchProxy<ICharacterAchievementManager>.Create(out var achievementProxy);
        var quest = RecordingDispatchProxy<IQuestManager>.Create(out var questProxy);
        var path = RecordingDispatchProxy<IPathManager>.Create(out var pathProxy);
        typeof(Player).GetProperty(nameof(Player.AchievementManager))!.SetValue(player, achievement);
        typeof(Player).GetProperty(nameof(Player.QuestManager))!.SetValue(player, quest);
        typeof(Player).GetProperty(nameof(Player.PathManager))!.SetValue(player, path);
        typeof(Player).GetField("deferredLoadingWorldZoneRuntimeSync", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(player, true);
        Action cancel = () => typeof(Player).GetMethod("CancelMapAdmission", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(player, null);
        if (boundary == "achievement")
            achievementProxy.SetMethodHandler(nameof(ICharacterAchievementManager.CheckAchievements), _ => { cancel(); return null; });
        if (boundary == "quest")
            questProxy.SetMethodHandler(nameof(IQuestManager.ObjectiveUpdate), _ => { cancel(); return null; });
        if (boundary == "teleport")
        {
            IMapFactory factory = RecordingDispatchProxy<IMapFactory>.Create(out _);
            typeof(Player).GetField("mapManager", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(player, new MapManager(factory));
            typeof(Player).GetProperty(nameof(Player.Identity))!.SetValue(player, new NexusForever.Game.Abstract.Identity { RealmId = 1, Id = 37 });
            typeof(WorldEntity).GetProperty(nameof(WorldEntity.MovementManager))!.SetValue(player, RecordingDispatchProxy<IMovementManager>.Create(out _));
            questProxy.SetMethodHandler(nameof(IQuestManager.ObjectiveUpdate), _ =>
            {
                player.TeleportTo(new MapPosition { Info = new MapInfo { Entry = new WorldEntry { Id = 51 } } });
                return null;
            });
        }
        if (boundary == "path")
            pathProxy.SetMethodHandler(nameof(IPathManager.TryActivateCurrentZoneEpisode), _ => { cancel(); return false; });
        player.IsLoading = true;
        ulong generation = state.Queue();
        state.BeginBootstrap();
        state.FinishBootstrap(generation, true);

        player.OnEnteredWorld();

        if (boundary == "teleport")
        {
            Assert.False(state.Canceled);
            Assert.True(state.Generation > generation);
            Assert.True(state.Queued);
            Assert.True(player.IsLoading);
            Assert.False(player.CanTeleport());
            Assert.NotNull(typeof(Player).GetField("pendingTeleport", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(player));
        }
        else
            Assert.True(state.Canceled);
        Assert.Empty(questProxy.GetInvocations(nameof(IQuestManager.GetQuestState)));
        Assert.Empty(pathProxy.GetInvocations(nameof(IPathManager.CompleteCurrentExplorerExploreZoneMission)));
        if (boundary != "path")
            Assert.Empty(pathProxy.GetInvocations(nameof(IPathManager.TryActivateCurrentZoneEpisode)));
        if (boundary == "achievement")
            Assert.Empty(questProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)));
    }

    [Fact]
    public void RecoveryTeleportFromEnteredWorldKeepsPendingTransitionAndLoadingState() =>
        ReentrantWorldZoneCancellationStopsPathAndTutorialContinuations("teleport");

}
