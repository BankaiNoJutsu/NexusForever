using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Entity;
using NexusForever.Game.Map;
using NexusForever.Game.Matching;
using NexusForever.Game.Matching.Match;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Network.Message;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Shared.Game.Events;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Misc;
using Path = NexusForever.Game.Static.PlayerPath.Path;
using Role = NexusForever.Game.Static.Matching.Role;

namespace NexusForever.Game.Tests.Network;

public class PlayerWorldEntryOrderingTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MatchExitWhileAttachingReturnsOnceAfterClientEntry(bool bootstrapInProgress)
    {
        var order = new List<string>();
        var messages = new List<IWritable>();
        Player player = CreatePlayer(order, messages);
        var session = (IWorldSession)player.Session;
        SetBackingField(player, nameof(Player.Identity), new Identity { RealmId = 1, Id = 42 });
        var movement = RecordingDispatchProxy<IMovementManager>.Create(out var facing);
        facing.SetMethodReturn(nameof(IMovementManager.GetRotation), Vector3.UnitX);
        typeof(WorldEntity).GetProperty(nameof(WorldEntity.MovementManager))!.SetValue(player, movement);
        var maps = RecordingDispatchProxy<IMapManager>.Create(out var mapQueue);
        var admission = (IPlayerMapAdmission)player;
        mapQueue.SetMethodHandler(nameof(IMapManager.AddToMap), _ =>
        {
            order.Add("return queued");
            admission.BeginMapAdmission();
            return null;
        });
        SetField(player, typeof(Player), "mapManager", maps);
        var players = RecordingDispatchProxy<IPlayerManager>.Create(out var registry);
        registry.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);
        var member = new MatchTeamMember(players);
        var home = player.Map.Entry;
        var homePosition = new Vector3(2, 3, 4);
        typeof(GridEntity).GetProperty(nameof(GridEntity.Position))!.SetValue(player, homePosition);
        member.Initialise(player.Identity, Role.DPS);

        var instance = RecordingDispatchProxy<IBaseMap>.Create(out var matchMap);
        var world = new WorldEntry { Id = 77 };
        matchMap.SetProperty(nameof(IBaseMap.Entry), world);
        typeof(GridEntity).GetProperty(nameof(GridEntity.Map))!.SetValue(player, instance);
        SetField(player, typeof(Player), "pendingTeleport", new PendingTeleport
        {
            MapPosition = new MapPosition { Info = new MapInfo { Entry = world } }
        });
        ulong generation = admission.MapAdmissionGeneration;
        if (bootstrapInProgress)
        {
            var state = new PlayerMapAdmissionState();
            generation = state.Queue();
            state.BeginBootstrap();
            SetField(player, typeof(Player), "mapAdmissionState", state);
        }
        var matchingMap = new MatchingMap { GameMapEntry = new MatchingGameMapEntry { Id = 7 } };
        member.MatchEnter(matchingMap);
        member.MatchExit(teleport: true);

        Assert.False(member.InMatch);
        Assert.Empty(mapQueue.GetInvocations(nameof(IMapManager.AddToMap)));
        Assert.DoesNotContain(messages, packet => packet is ServerPlayerEnteredWorld);
        var acknowledge = new ClientEnteredWorldHandler();
        acknowledge.HandleMessage(session, new ClientEnteredWorld());
        if (bootstrapInProgress)
        {
            Assert.Empty(mapQueue.GetInvocations(nameof(IMapManager.AddToMap)));
            admission.CompleteMapAdmission(generation, successful: true);
        }
        acknowledge.HandleMessage(session, new ClientEnteredWorld());

        var returned = Assert.IsAssignableFrom<IMapPosition>(
            Assert.Single(mapQueue.GetInvocations(nameof(IMapManager.AddToMap))).Arguments[1]);
        Assert.Same(home, returned.Info.Entry);
        Assert.Equal(homePosition, returned.Position);
        Assert.Single(messages.OfType<ServerPlayerEnteredWorld>());
        Assert.True(order.IndexOf(nameof(ServerPlayerEnteredWorld)) < order.IndexOf("return queued"));
        Assert.True(player.IsLoading);
        Assert.False(admission.IsMapAdmissionCurrent(generation));
        Assert.Equal(Vector3.UnitX, Assert.Single(facing.GetInvocations(nameof(IMovementManager.SetRotation))).Arguments[0]);
    }

    [Fact]
    public void OnEnteredWorld_SendsQuestRuntimeBeforePathReplay()
    {
        var order = new List<string>();
        Player player = CreatePlayer(order);

        player.OnEnteredWorld();

        Assert.Equal(
            [
                nameof(ServerPlayerEnteredWorld),
                nameof(IQuestManager.SendInitialPackets),
                nameof(IPathManager.SendInitialPackets)
            ],
            order.Take(3));
    }

    [Fact]
    public void OnEnteredWorld_RefreshesVisibleRemotePlayersBeforeRuntimeManagers()
    {
        var order = new List<string>();
        var messages = new List<IWritable>();
        Player player = CreatePlayer(order, messages);
        IPlayer nearbyPlayer = CreateVisiblePlayer(77u, 0x800000000000002Au);

        SetField(player, typeof(GridEntity), "visibleEntities", new Dictionary<uint, IGridEntity>
        {
            [nearbyPlayer.Guid] = nearbyPlayer
        });

        player.OnEnteredWorld();

        Assert.Equal(
            [
                nameof(ServerPlayerEnteredWorld),
                nameof(ServerEntityDestroy),
                nameof(ServerEntityCreate),
                nameof(ServerSetUnitPathType),
                nameof(ServerEntityGroupAssociation),
                nameof(IQuestManager.SendInitialPackets),
                nameof(IPathManager.SendInitialPackets)
            ],
            order.Take(7));

        ServerEntityDestroy destroy = Assert.IsType<ServerEntityDestroy>(messages[1]);
        Assert.Equal(nearbyPlayer.Guid, destroy.Guid);
        Assert.True(destroy.Flag);

        ServerEntityGroupAssociation groupAssociation = Assert.IsType<ServerEntityGroupAssociation>(messages[4]);
        Assert.Equal(nearbyPlayer.Guid, groupAssociation.UnitId);
        Assert.Equal(nearbyPlayer.ClientGroupAssociation, groupAssociation.GroupId);
    }

    private static Player CreatePlayer(List<string> order, List<IWritable> messages = null)
    {
        Player player = (Player)RuntimeHelpers.GetUninitializedObject(typeof(Player));

        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out var sessionProxy);
        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);
        sessionProxy.SetProperty(nameof(IGameSession.Events), new EventQueue());
        sessionProxy.SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), args =>
        {
            if (args is [IWritable message])
            {
                order.Add(message.GetType().Name);
                messages?.Add(message);
            }

            return null;
        });

        IPathManager pathManager = RecordingDispatchProxy<IPathManager>.Create(out RecordingDispatchProxy<IPathManager> pathManagerProxy);
        pathManagerProxy.SetMethodHandler(nameof(IPathManager.SendInitialPackets), _ =>
        {
            order.Add(nameof(IPathManager.SendInitialPackets));
            return null;
        });

        IQuestManager questManager = RecordingDispatchProxy<IQuestManager>.Create(out RecordingDispatchProxy<IQuestManager> questManagerProxy);
        questManagerProxy.SetMethodHandler(nameof(IQuestManager.SendInitialPackets), _ =>
        {
            order.Add(nameof(IQuestManager.SendInitialPackets));
            return null;
        });

        SetField(player, typeof(GridEntity), "visibleEntities", new Dictionary<uint, IGridEntity>());
        SetBackingField<IGameSession>(player, nameof(Player.Session), session);
        SetBackingField(player, nameof(Player.PathManager), pathManager);
        SetBackingField(player, nameof(Player.QuestManager), questManager);
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out var mapProxy);
        mapProxy.SetProperty(nameof(IBaseMap.Entry), new WorldEntry { Id = 51 });
        typeof(GridEntity).GetProperty(nameof(GridEntity.Map))!.SetValue(player, map);
        typeof(GridEntity).GetProperty(nameof(GridEntity.Guid))!.SetValue(player, 37u);
        // These ordering witnesses represent a completed server bootstrap. The
        // separate admission regressions cover missing and in-progress maps.
        var admission = new PlayerMapAdmissionState();
        ulong generation = admission.Queue();
        admission.BeginBootstrap();
        admission.FinishBootstrap(generation, true);
        SetField(player, typeof(Player), "mapAdmissionState", admission);
        player.IsLoading = true;

        return player;
    }

    private static IPlayer CreateVisiblePlayer(uint guid, ulong clientGroupAssociation)
    {
        IMovementManager movementManager = RecordingDispatchProxy<IMovementManager>.Create(out RecordingDispatchProxy<IMovementManager> movementProxy);
        movementProxy.SetProperty(nameof(IMovementManager.RequiresSynchronisation), false);

        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> proxy);
        proxy.SetProperty(nameof(IPlayer.Guid), guid);
        proxy.SetProperty(nameof(IPlayer.Path), Path.Soldier);
        proxy.SetProperty(nameof(IPlayer.ClientGroupAssociation), clientGroupAssociation);
        proxy.SetProperty(nameof(IPlayer.MovementManager), movementManager);
        proxy.SetMethodHandler(nameof(IWorldEntity.BuildEntityCreateAuxPackets), _ => Array.Empty<IWritable>());
        proxy.SetMethodHandler(nameof(IWorldEntity.BuildCreatePacket), _ => new ServerEntityCreate
        {
            Guid = guid,
            Type = EntityType.Player
        });

        return player;
    }

    private static void SetBackingField<T>(Player player, string propertyName, T value)
    {
        SetField(player, typeof(Player), $"<{propertyName}>k__BackingField", value);
    }

    private static void SetField<T>(Player player, Type declaringType, string fieldName, T value)
    {
        FieldInfo field = declaringType.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Missing field {declaringType.Name}.{fieldName}.");

        field.SetValue(player, value);
    }
}
