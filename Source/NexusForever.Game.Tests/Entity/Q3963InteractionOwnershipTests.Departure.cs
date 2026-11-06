using System.Collections;
using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Cinematic;
using NexusForever.Game.Abstract.Cinematic.Cinematics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Script.Main.Quests.NorthernWilds;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Collection;
using NexusForever.Shared.Game.Events;

namespace NexusForever.Game.Tests.Entity;

public partial class Q3963InteractionOwnershipTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Q3963ParsedCompletionRunsRealDepartureAndRetainsQuestAcrossSaveRetry(bool entityEvent)
    {
        using var f = new Fixture();
        var maps = ConnectDeparture(f);
        f.Pending(); f.Complete(entityEvent);
        Assert.Equal(QuestState.Achieved, f.Quest.State);
        Assert.Single(((RecordingDispatchProxy<ICinematicManager>)(object)f.Actor.Player.CinematicManager)
            .GetInvocations(nameof(ICinematicManager.QueueCinematic)));
        Assert.Empty(maps.GetInvocations(nameof(IMapManager.AddToMap)));
        await Assert.ThrowsAsync<IOException>(() => f.Persist(fail: true));
        var departure = DepartureEvent(f.Actor.Events);
        departure.Execute(); departure.Execute();
        var call = Assert.Single(maps.GetInvocations(nameof(IMapManager.AddToMap)));
        Assert.Same(f.Actor.Player, call.Arguments[0]);
        var target = Assert.IsAssignableFrom<IMapPosition>(call.Arguments[1]);
        Assert.Equal(51u, target.Info.Entry.Id);
        Assert.Equal(new System.Numerics.Vector3(3832.376f, -1001.4483f, -4497.1353f), target.Position);
        Assert.False(f.Actor.Player.CanTeleport());
        await f.Persist();
        using var reload = f.Reload();
        Assert.Equal(QuestState.Achieved, reload.State);
        Assert.Equal(1u, Assert.Single(reload).Progress);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Q3963RealPlayerAdmissionChangeCancelsDeparture(bool entityEvent)
    {
        using var f = new Fixture();
        var maps = ConnectDeparture(f);
        f.Pending(); f.Complete(entityEvent);
        ((IPlayerMapAdmission)f.Actor.Player).BeginMapAdmission();
        DepartureEvent(f.Actor.Events).Execute();
        Assert.Empty(maps.GetInvocations(nameof(IMapManager.AddToMap)));
        Assert.Equal(QuestState.Achieved, f.Quest.State);
    }

    private static RecordingDispatchProxy<IMapManager> ConnectDeparture(Fixture f)
    {
        var tables = (RecordingDispatchProxy<IGameTableManager>)(object)f.Storage.Source.Tables;
        tables.SetProperty(nameof(IGameTableManager.World), PlayerBindPointTests.Table(new WorldEntry { Id = 51u }));
        tables.SetProperty(nameof(IGameTableManager.WorldLocation2), PlayerBindPointTests.Table(new WorldLocation2Entry
        {
            Id = 9801u, WorldId = 51u, Position0 = 3832.376f, Position1 = -1001.4483f, Position2 = -4497.1353f,
            Facing0 = -1.0037917E-12f, Facing1 = -0.22710016f, Facing2 = 4.3046105E-12f, Facing3 = 0.9738714f
        }));
        const BindingFlags fields = BindingFlags.Instance | BindingFlags.NonPublic;
        typeof(Player).GetField("gameTableManager", fields)!.SetValue(f.Actor.Player, f.Storage.Source.Tables);
        var admissionGate = ((IPlayerMapAdmission)f.Actor.Player).MapAdmissionSyncRoot;
        ((RecordingDispatchProxy<IMovementManager>)(object)f.Actor.Player.MovementManager)
            .SetMethodHandler(nameof(IMovementManager.SetRotation), _ => { Assert.True(Monitor.IsEntered(admissionGate)); return null; });
        var mapManager = RecordingDispatchProxy<IMapManager>.Create(out var maps);
        typeof(Player).GetField("mapManager", fields)!.SetValue(f.Actor.Player, mapManager);
        var factory = RecordingDispatchProxy<ICinematicFactory>.Create(out var fp);
        fp.SetMethodReturn(nameof(ICinematicFactory.CreateCinematic), RecordingDispatchProxy<IQ3963MoreImportantThanRevengeDepartureCinematic>.Create(out _));
        var script = new Q3963MoreImportantThanRevengeQuestScript(NullLogger<Q3963MoreImportantThanRevengeQuestScript>.Instance, factory, f.Storage.Source.Tables);
        script.OnLoad(f.Quest);
        maps.SetMethodHandler(nameof(IMapManager.AddToMap), args =>
        { ((IPlayerMapAdmission)(IPlayer)args[0]).BeginMapAdmission(); script.OnUnload(); return null; });
        var collection = (IScriptCollection)typeof(NexusForever.Game.Quest.Quest).GetField("scriptCollection", fields)!.GetValue(f.Quest)!;
        ((RecordingDispatchProxy<IScriptCollection>)(object)collection).SetMethodHandler(nameof(IScriptCollection.Invoke), args =>
        { if (args[0] is Action<IQuestScript> callback) callback(script); return null; });
        return maps;
    }

    private static IEvent DepartureEvent(EventQueue events)
    {
        var queue = (IEnumerable)typeof(EventQueue).GetField("events", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(events)!;
        return Assert.Single(queue.Cast<object>().Select(p => p.GetType().GetProperty("Event")!.GetValue(p)).OfType<DelayEvent>());
    }
}
