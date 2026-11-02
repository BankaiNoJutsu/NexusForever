using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Static;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Item;

namespace NexusForever.Game.Tests.Item;

public partial class ClientItemUseHandlerTests
{
    [Theory]
    [InlineData(0u)]
    [InlineData(0x12345678u)]
    public void MissingItemRefusesItsParsedCastWithoutDisconnectOrConsumption(uint token)
    {
        var session = CreateSession(null, CastResult.Ok, [], out var inventory, out _);
        var messages = RecordRefusals(session);
        var request = CreateRequest(); SetAutoProperty(request, nameof(ClientItemUse.ContextToken), token);
        CreateHandler(777).HandleMessage(session, request);
        var reply = Assert.Single(messages); Assert.Equal(token, reply.ContextToken);
        Assert.Equal(0u, reply.Spell4Id); Assert.Equal(CastResult.ItemUnknown, reply.CastResult);
        Assert.Empty(inventory.GetInvocations(nameof(IInventory.ItemUse)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MissingSpecialOrActivationSpellRefusesWithoutConsuming(bool missingSpecial)
    {
        var session = CreateSession(CreateItem(1, 20), CastResult.Ok, [], out var inventory, out var player);
        var messages = RecordRefusals(session);
        var handler = new ClientItemUseHandler(CreateGameTableManager(0, includeItemSpecial: !missingSpecial));
        handler.HandleMessage(session, CreateRequest());
        var reply = Assert.Single(messages); Assert.Equal(0u, reply.Spell4Id);
        Assert.Equal(missingSpecial ? CastResult.ItemBad : CastResult.ItemNoSpellId, reply.CastResult);
        Assert.Empty(player.GetInvocations(nameof(IPlayer.TryCastSpell))); Assert.Empty(inventory.GetInvocations(nameof(IInventory.ItemUse)));
    }

    [Theory]
    [InlineData(0u, 20u, 0u, 0u)]
    [InlineData(1u, 1u, 3u, 0u)]
    public void EmptyStackOrExhaustedChargesRefusesTheActivationSpell(uint count, uint maxStack, uint maxCharges, uint charges)
    {
        var session = CreateSession(CreateItem(count, maxStack, maxCharges: maxCharges, charges: charges), CastResult.Ok, [], out var inventory, out var player);
        var messages = RecordRefusals(session);
        CreateHandler(777).HandleMessage(session, CreateRequest());
        var reply = Assert.Single(messages); Assert.Equal(777u, reply.Spell4Id); Assert.Equal(CastResult.ItemCharges, reply.CastResult);
        Assert.Empty(player.GetInvocations(nameof(IPlayer.TryCastSpell))); Assert.Empty(inventory.GetInvocations(nameof(IInventory.ItemUse)));
    }

    [Fact]
    public void FailedPrerequisiteAnswersTheCastWithoutGenericErrorOrConsumption()
    {
        var tables = CreateGameTableManager(777);
        tables.ItemSpecial.GetEntry(123).PrerequisiteIdGeneric00 = 10;
        var checks = RecordingDispatchProxy<IPrerequisiteManager>.Create(out var checkProxy);
        checkProxy.SetMethodReturn(nameof(IPrerequisiteManager.Meets), false);
        var session = CreateSession(CreateItem(1, 20), CastResult.Ok, [], out var inventory, out var player);
        var messages = RecordRefusals(session);
        new ClientItemUseHandler(tables, checks).HandleMessage(session, CreateRequest());
        var reply = Assert.Single(messages); Assert.Equal(777u, reply.Spell4Id); Assert.Equal(CastResult.SpellPreRequisites, reply.CastResult);
        Assert.Empty(player.GetInvocations(nameof(IPlayer.SendGenericError))); Assert.Empty(inventory.GetInvocations(nameof(IInventory.ItemUse)));
    }

    [Fact]
    public void CurrencyTreasureKeepsItsGrantAndRetiresTheParsedNonSpellRequest()
    {
        var session = CreateSession(CreateCurrencyTreasure(1), CastResult.Ok, [], out var inventory, out _, out var currency);
        var messages = RecordRefusals(session);
        CreateHandler(0).HandleMessage(session, CreateRequest());
        Assert.Single(inventory.GetInvocations(nameof(IInventory.ItemUse))); Assert.Single(currency.GetInvocations(nameof(ICurrencyManager.CurrencyAddAmount)));
        Assert.Equal(CastResult.ItemNoSpellId, Assert.Single(messages).CastResult);
    }

    [Fact]
    public void ContextActionWithoutACastTokenDoesNotInventAPendingRequest()
    {
        var session = CreateSession(CreateItem(1, 20), CastResult.Ok, [], out _, out var player);
        var messages = RecordRefusals(session);
        CreateContextActionHandler(777).HandleMessage(session, CreateContextActionRequest());
        var call = Assert.Single(player.GetInvocations(nameof(IPlayer.TryCastSpell)));
        var parameters = Assert.IsType<NexusForever.Game.Spell.SpellParameters>(call.Arguments[1]);
        Assert.False(parameters.HasClientCastRequest); Assert.Empty(messages);
    }

    [Fact]
    public void HeldInventoryPersistenceRefusesWithoutCastingOrConsuming()
    {
        var session = CreateSession(CreateItem(1, 20), CastResult.Ok, [], out var inventory, out var player);
        inventory.SetProperty(nameof(IInventory.SupportsTransactionalSoulboundDelivery), true);
        inventory.SetMethodReturn(nameof(IInventory.TryAcquireMutationLease), false);
        var messages = RecordRefusals(session);
        CreateHandler(777).HandleMessage(session, CreateRequest());
        Assert.Equal(CastResult.ItemBad, Assert.Single(messages).CastResult);
        Assert.Empty(player.GetInvocations(nameof(IPlayer.TryCastSpell))); Assert.Empty(inventory.GetInvocations(nameof(IInventory.ItemUse)));
    }

    [Fact]
    public void LoadingOwnerDoesNotReceiveAnEarlyItemRefusal()
    {
        var session = CreateSession(null, CastResult.Ok, [], out _, out var player);
        player.SetProperty(nameof(IPlayer.IsLoading), true);
        var messages = RecordRefusals(session);
        CreateHandler(777).HandleMessage(session, CreateRequest());
        Assert.Empty(messages);
    }

    private static List<ServerSpellCastResult> RecordRefusals(IWorldSession session)
    {
        var replies = new List<ServerSpellCastResult>();
        var gameSession = RecordingDispatchProxy<IGameSession>.Create(out var wire);
        wire.SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), args => { if (args[0] is ServerSpellCastResult result) replies.Add(result); return null; });
        ((RecordingDispatchProxy<IPlayer>)(object)session.Player).SetProperty(nameof(IPlayer.Session), gameSession);
        return replies;
    }
}
