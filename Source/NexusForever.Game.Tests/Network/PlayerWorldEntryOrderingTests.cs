using System.Reflection;
using System.Runtime.CompilerServices;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.Message;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Shared.Game.Events;
using Path = NexusForever.Game.Static.PlayerPath.Path;

namespace NexusForever.Game.Tests.Network;

public class PlayerWorldEntryOrderingTests
{
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

        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out RecordingDispatchProxy<IGameSession> sessionProxy);
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
        SetBackingField(player, nameof(Player.Session), session);
        SetBackingField(player, nameof(Player.PathManager), pathManager);
        SetBackingField(player, nameof(Player.QuestManager), questManager);
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
