using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Loot;
using NexusForever.Game.Static.Loot;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Loot;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Loot;
using NetworkIdentity = NexusForever.Network.World.Message.Model.Shared.Identity;

namespace NexusForever.Game.Tests.Loot;

public class LootRequestHandlerTests
{
    [Fact]
    public void ClientLootRollActionHandler_ForwardsDecodedRollToLootManager()
    {
        IWorldSession session = CreateSession(out IPlayer player);
        IGlobalLootManager lootManager = RecordingDispatchProxy<IGlobalLootManager>.Create(out RecordingDispatchProxy<IGlobalLootManager> lootProxy);
        var handler = new ClientLootRollActionHandler(NullLogger<ClientLootRollActionHandler>.Instance, lootManager);
        ClientLootRollAction message = BuildRollAction(ownerUnitId: 77u, lootUnitId: 88u, LootRollAction.Need);

        handler.HandleMessage(session, message);

        RecordingDispatchProxy<IGlobalLootManager>.Invocation invocation =
            Assert.Single(lootProxy.GetInvocations(nameof(IGlobalLootManager.RollLoot)));
        Assert.Same(player, invocation.Arguments[0]);
        Assert.Equal(77u, invocation.Arguments[1]);
        Assert.Equal(88u, invocation.Arguments[2]);
        Assert.Equal(LootRollAction.Need, invocation.Arguments[3]);
    }

    [Fact]
    public void ClientLootItemHandler_NotifyRequest_ForwardsToSendLootNotify()
    {
        IWorldSession session = CreateSession(out IPlayer player, out RecordingDispatchProxy<IWorldSession> sessionProxy);
        IGlobalLootManager lootManager = RecordingDispatchProxy<IGlobalLootManager>.Create(out RecordingDispatchProxy<IGlobalLootManager> lootProxy);
        var handler = new ClientLootItemHandler(NullLogger<ClientLootItemHandler>.Instance, lootManager);
        ClientLootItem message = BuildLootItem(ownerUnitId: player.Guid, lootUnitId: 88u, request: true);

        handler.HandleMessage(session, message);

        RecordingDispatchProxy<IGlobalLootManager>.Invocation invocation =
            Assert.Single(lootProxy.GetInvocations(nameof(IGlobalLootManager.SendLootNotify)));
        Assert.Same(player, invocation.Arguments[0]);
        Assert.Equal(player.Guid, invocation.Arguments[1]);
        Assert.Empty(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
    }

    [Fact]
    public void ClientLootItemHandler_CollectRequest_ForwardsToGiveLoot()
    {
        IWorldSession session = CreateSession(out IPlayer player, out _);
        IGlobalLootManager lootManager = RecordingDispatchProxy<IGlobalLootManager>.Create(out RecordingDispatchProxy<IGlobalLootManager> lootProxy);
        var handler = new ClientLootItemHandler(NullLogger<ClientLootItemHandler>.Instance, lootManager);
        ClientLootItem message = BuildLootItem(ownerUnitId: player.Guid, lootUnitId: 88u, request: false);

        handler.HandleMessage(session, message);

        RecordingDispatchProxy<IGlobalLootManager>.Invocation invocation =
            Assert.Single(lootProxy.GetInvocations(nameof(IGlobalLootManager.GiveLoot)));
        Assert.Same(player, invocation.Arguments[0]);
        Assert.Equal(player.Guid, invocation.Arguments[1]);
        Assert.Equal(88u, invocation.Arguments[2]);
    }

    [Fact]
    public void ClientLootVacuumHandler_ForwardsToGiveAllLootInRange()
    {
        IWorldSession session = CreateSession(out IPlayer player, out _);
        IGlobalLootManager lootManager = RecordingDispatchProxy<IGlobalLootManager>.Create(out RecordingDispatchProxy<IGlobalLootManager> lootProxy);
        var handler = new ClientLootVacuumHandler(NullLogger<ClientLootVacuumHandler>.Instance, lootManager);

        handler.HandleMessage(session, new ClientLootVacuum());

        RecordingDispatchProxy<IGlobalLootManager>.Invocation invocation =
            Assert.Single(lootProxy.GetInvocations(nameof(IGlobalLootManager.GiveAllLootInRange)));
        Assert.Same(player, invocation.Arguments[0]);
    }

    [Fact]
    public void ClientLootAssignMasterHandler_ForwardsDecodedAssigneeToLootManager()
    {
        IWorldSession session = CreateSession(out IPlayer player);
        IGlobalLootManager lootManager = RecordingDispatchProxy<IGlobalLootManager>.Create(out RecordingDispatchProxy<IGlobalLootManager> lootProxy);
        var handler = new ClientLootAssignMasterHandler(NullLogger<ClientLootAssignMasterHandler>.Instance, lootManager);
        ClientLootAssignMaster message = BuildAssignMaster(ownerUnitId: 99u, lootUnitId: 111u, realmId: 2, assigneeId: 12345ul);

        handler.HandleMessage(session, message);

        RecordingDispatchProxy<IGlobalLootManager>.Invocation invocation =
            Assert.Single(lootProxy.GetInvocations(nameof(IGlobalLootManager.AssignMasterLoot)));
        Assert.Same(player, invocation.Arguments[0]);
        Assert.Equal(99u, invocation.Arguments[1]);
        Assert.Equal(111u, invocation.Arguments[2]);
        Assert.Equal(new NexusForever.Game.Abstract.Identity
        {
            RealmId = 2,
            Id      = 12345ul
        }, invocation.Arguments[3]);
    }

    private static IWorldSession CreateSession(out IPlayer player)
    {
        return CreateSession(out player, out _);
    }

    private static IWorldSession CreateSession(out IPlayer player, out RecordingDispatchProxy<IWorldSession> sessionProxy)
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out sessionProxy);
        player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Guid), 77u);
        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);
        return session;
    }

    private static ClientLootItem BuildLootItem(uint ownerUnitId, uint lootUnitId, bool request)
    {
        var message = new ClientLootItem();
        SetPrivateProperty(message, nameof(ClientLootItem.OwnerUnitId), ownerUnitId);
        SetPrivateProperty(message, nameof(ClientLootItem.LootUnitId), lootUnitId);
        SetPrivateProperty(message, nameof(ClientLootItem.Request), request);
        return message;
    }

    private static ClientLootRollAction BuildRollAction(uint ownerUnitId, uint lootUnitId, LootRollAction action)
    {
        var message = new ClientLootRollAction();
        SetPrivateProperty(message, nameof(ClientLootRollAction.OwnerUnitId), ownerUnitId);
        SetPrivateProperty(message, nameof(ClientLootRollAction.LootUnitId), lootUnitId);
        SetPrivateProperty(message, nameof(ClientLootRollAction.Action), action);
        return message;
    }

    private static ClientLootAssignMaster BuildAssignMaster(uint ownerUnitId, uint lootUnitId, ushort realmId, ulong assigneeId)
    {
        var message = new ClientLootAssignMaster();
        SetPrivateProperty(message, nameof(ClientLootAssignMaster.OwnerUnitId), ownerUnitId);
        SetPrivateProperty(message, nameof(ClientLootAssignMaster.LootUnitId), lootUnitId);
        SetPrivateProperty(message, nameof(ClientLootAssignMaster.Assignee), new NetworkIdentity
        {
            RealmId = realmId,
            Id      = assigneeId
        });
        return message;
    }

    private static void SetPrivateProperty<T>(object instance, string propertyName, T value)
    {
        PropertyInfo property = instance.GetType().GetProperty(propertyName)!;
        property.GetSetMethod(true)!.Invoke(instance, [value]);
    }
}
