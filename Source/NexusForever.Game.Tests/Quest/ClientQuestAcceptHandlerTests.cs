using System.Reflection;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.World.Message.Model;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Quest;

namespace NexusForever.Game.Tests.Quest;

public class ClientQuestAcceptHandlerTests
{
    [Fact]
    public void HandleMessage_NpcQuestAccept_EndsDialogAfterQuestAdd()
    {
        IWorldSession session = CreateSession(out RecordingDispatchProxy<IQuestManager> questManagerProxy, out RecordingDispatchProxy<IWorldSession> sessionProxy);
        SetActiveDialog(session, dialogUnitId: 790u);
        var handler = new ClientQuestAcceptHandler();

        handler.HandleMessage(session, CreateQuestAccept(4609, (InventoryLocation)300));

        RecordingDispatchProxy<IQuestManager>.Invocation questAdd = Assert.Single(
            questManagerProxy.GetInvocations(nameof(IQuestManager.QuestAdd)));
        Assert.Equal((ushort)4609, questAdd.Arguments[0]);
        Assert.Null(questAdd.Arguments[1]);

        RecordingDispatchProxy<IWorldSession>.Invocation sentMessage = Assert.Single(
            sessionProxy.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted)));
        var dialogEnd = Assert.IsType<ServerDialogEnd>(sentMessage.Arguments[0]);
        Assert.Equal(790u, dialogEnd.DialogUnitId);
    }

    [Fact]
    public void HandleMessage_ItemQuestAccept_DoesNotEndNpcDialog()
    {
        IItem item = RecordingDispatchProxy<IItem>.Create(out _);
        var inventory = RecordingDispatchProxy<IInventory>.Create(out RecordingDispatchProxy<IInventory> inventoryProxy);
        inventoryProxy.SetMethodHandler(nameof(IInventory.GetItem), _ => item);
        IWorldSession session = CreateSession(out RecordingDispatchProxy<IQuestManager> questManagerProxy, out RecordingDispatchProxy<IWorldSession> sessionProxy, inventory);
        var handler = new ClientQuestAcceptHandler();

        handler.HandleMessage(session, CreateQuestAccept(4609, InventoryLocation.Inventory));

        RecordingDispatchProxy<IQuestManager>.Invocation questAdd = Assert.Single(
            questManagerProxy.GetInvocations(nameof(IQuestManager.QuestAdd)));
        Assert.Equal((ushort)4609, questAdd.Arguments[0]);
        Assert.Same(item, questAdd.Arguments[1]);
        Assert.Empty(sessionProxy.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted)));
    }

    private static IWorldSession CreateSession(
        out RecordingDispatchProxy<IQuestManager> questManagerProxy,
        out RecordingDispatchProxy<IWorldSession> sessionProxy,
        IInventory inventory = null)
    {
        IQuestManager questManager = RecordingDispatchProxy<IQuestManager>.Create(out questManagerProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.QuestManager), questManager);
        if (inventory != null)
            playerProxy.SetProperty(nameof(IPlayer.Inventory), inventory);

        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out sessionProxy);
        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);
        return session;
    }

    private static void SetActiveDialog(IWorldSession session, uint dialogUnitId)
    {
        Type dialogSessionState = typeof(ClientQuestAcceptHandler).Assembly
            .GetType("NexusForever.WorldServer.Network.Message.Handler.DialogSessionState")!;
        MethodInfo setActiveDialog = dialogSessionState
            .GetMethod("SetActiveDialog", BindingFlags.Static | BindingFlags.Public)!;
        setActiveDialog.Invoke(null, [session, dialogUnitId]);
    }

    private static ClientQuestAccept CreateQuestAccept(ushort questId, InventoryLocation location)
    {
        var questAccept = new ClientQuestAccept();
        questAccept.ItemLocation.Location = location;
        SetAutoProperty(questAccept, nameof(ClientQuestAccept.QuestId), questId);
        return questAccept;
    }

    private static void SetAutoProperty(object instance, string propertyName, object value)
    {
        FieldInfo backingField = instance.GetType()
            .GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!;
        backingField.SetValue(instance, value);
    }
}
