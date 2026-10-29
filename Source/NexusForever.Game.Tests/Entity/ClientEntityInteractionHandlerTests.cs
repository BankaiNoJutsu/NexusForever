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

    [Theory]
    [InlineData(45, false, 1f, true)]
    [InlineData(46, false, 1f, true)]
    [InlineData(47, false, 1f, true)]
    [InlineData(48, false, 1f, true)]
    [InlineData(48, true, 1f, false)]
    [InlineData(48, false, 100f, false)]
    public void TaxiInteractionDiscoversNodeOnlyAfterExistingGuards(byte eventId, bool busy, float distance, bool expected)
    {
        var session = CreateSession(out _, out var pp);
        var entity = RecordingDispatchProxy<IWorldEntity>.Create(out var ep);
        ep.SetProperty(nameof(IWorldEntity.CreatureEntry), new NexusForever.GameTable.Model.Creature2Entry { TaxiNodeId = 10 });
        ep.SetProperty(nameof(IWorldEntity.IsBusy), busy);
        ep.SetProperty(nameof(IWorldEntity.Position), new System.Numerics.Vector3(distance, 0, 0));
        pp.SetMethodReturn(nameof(IPlayer.GetVisible), entity);
        var handler = new ClientEntityInteractionHandler(NullLogger<ClientEntityInteractionHandler>.Instance, null, new RealmBankManager());

        handler.HandleMessage(session, CreateEntityInteract(123, eventId));

        var calls = pp.GetInvocations(nameof(IPlayer.DiscoverTaxiNode));
        Assert.Equal(expected ? 1 : 0, calls.Count);
        if (expected) Assert.Equal(10u, calls[0].Arguments[0]);
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
