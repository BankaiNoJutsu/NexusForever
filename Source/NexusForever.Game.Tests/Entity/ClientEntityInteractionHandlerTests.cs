using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.RealmBank;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.World.Message.Model;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Entity;

namespace NexusForever.Game.Tests.Entity;

public class ClientEntityInteractionHandlerTests
{
    [Fact]
    public void HandleMessage_ClientSideInteractionSuccessWithZeroTarget_DoesNotCreditObjectives()
    {
        IWorldSession session = CreateSession(out RecordingDispatchProxy<IQuestManager> questProxy, out _);
        var handler = new ClientEntityInteractionHandler(NullLogger<ClientEntityInteractionHandler>.Instance, assetManager: null, new RealmBankManager());

        handler.HandleMessage(session, CreateEntityInteract(guid: 0u, eventId: 101));

        Assert.Empty(questProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)));
    }

    private static IWorldSession CreateSession(
        out RecordingDispatchProxy<IQuestManager> questProxy,
        out RecordingDispatchProxy<IPlayer> playerProxy)
    {
        IQuestManager questManager = RecordingDispatchProxy<IQuestManager>.Create(out questProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.QuestManager), questManager);

        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out RecordingDispatchProxy<IWorldSession> sessionProxy);
        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);
        return session;
    }

    private static ClientEntityInteract CreateEntityInteract(uint guid, byte eventId)
    {
        var interaction = new ClientEntityInteract();
        SetAutoProperty(interaction, nameof(ClientEntityInteract.Guid), guid);
        SetAutoProperty(interaction, nameof(ClientEntityInteract.Event), eventId);
        return interaction;
    }

    private static void SetAutoProperty(object instance, string propertyName, object value)
    {
        FieldInfo backingField = instance.GetType()
            .GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!;
        backingField.SetValue(instance, value);
    }
}
