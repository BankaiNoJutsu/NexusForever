using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Reputation;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Static;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Misc;

namespace NexusForever.Game.Tests.Misc;

public class MappedRequestHandlerTests
{
    [Fact]
    public void ConvertResource_WithMissingConversionTableDoesNotMutate()
    {
        IWorldSession session = CreateSession(
            out RecordingDispatchProxy<IInventory> inventoryProxy,
            out RecordingDispatchProxy<ICurrencyManager> currencyProxy,
            out RecordingDispatchProxy<IReputationManager> reputationProxy,
            out RecordingDispatchProxy<IWorldSession> sessionProxy);
        var handler = new ClientConvertResourceHandler(
            NullLogger<ClientConvertResourceHandler>.Instance,
            CreateGameTableManager());

        handler.HandleMessage(session, CreateConvertResource(10u));

        AssertNoResourceMutation(inventoryProxy, currencyProxy, reputationProxy);
        Assert.Empty(sessionProxy.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted)));
    }

    [Fact]
    public void ConvertResource_ItemToItemWithMissingItemTableDoesNotMutate()
    {
        IWorldSession session = CreateSession(
            out RecordingDispatchProxy<IInventory> inventoryProxy,
            out RecordingDispatchProxy<ICurrencyManager> currencyProxy,
            out RecordingDispatchProxy<IReputationManager> reputationProxy,
            out _);
        var handler = new ClientConvertResourceHandler(
            NullLogger<ClientConvertResourceHandler>.Instance,
            CreateGameTableManager(resourceConversions: [new ResourceConversionEntry
            {
                Id                         = 10u,
                ResourceConversionTypeEnum = 0u,
                SourceId                   = 100u,
                SourceCount                = 2u,
                TargetId                   = 200u,
                TargetCount                = 1u
            }]));

        handler.HandleMessage(session, CreateConvertResource(10u));

        AssertNoResourceMutation(inventoryProxy, currencyProxy, reputationProxy);
    }

    [Theory]
    [InlineData(3u, 1u, false)]
    [InlineData(2u, 2u, false)]
    [InlineData(2u, 1u, true)]
    public void ConvertResource_OnlyConsumesSourceIfTheWholeResultFits(uint sourceStack, uint targetCount, bool fits)
    {
        const uint sourceId = 100u;
        const uint targetId = 200u;
        IItemInfo source = CreateItemInfo(sourceId, 10u);
        IItemInfo target = CreateItemInfo(targetId, 1u);
        IItemManager items = RecordingDispatchProxy<IItemManager>.Create(out var itemManager);
        itemManager.SetMethodHandler(nameof(IItemManager.GetItemInfo), args => (uint)args[0] == sourceId ? source : target);
        ulong nextId = 1000;
        itemManager.SetMethodReturnFactory("get_" + nameof(IItemManager.NextItemId), () => nextId++);

        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out var network);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var actor);
        network.SetProperty(nameof(IWorldSession.Player), player);
        actor.SetProperty(nameof(IPlayer.Session), session);
        actor.SetProperty(nameof(IPlayer.CharacterId), 42ul);
        actor.SetProperty(nameof(IPlayer.IsLoading), true);
        ISupplySatchelManager satchel = RecordingDispatchProxy<ISupplySatchelManager>.Create(out var materials);
        materials.SetMethodHandler(nameof(ISupplySatchelManager.AddAmount), args => args[1]);
        actor.SetProperty(nameof(IPlayer.SupplySatchelManager), satchel);
        var inventory = (Inventory)RuntimeHelpers.GetUninitializedObject(typeof(Inventory));
        var bag = new Bag(InventoryLocation.Inventory, 1, items);
        SetField("bags", new Dictionary<InventoryLocation, IBag> { [InventoryLocation.Inventory] = bag });
        SetField("deletedItems", new List<IItem>());
        SetField("itemManager", items);
        SetField("player", player);
        SetField("characterId", 42ul);
        actor.SetProperty(nameof(IPlayer.Inventory), inventory);
        inventory.ItemCreate(InventoryLocation.Inventory, source, sourceStack);
        actor.SetProperty(nameof(IPlayer.IsLoading), false);

        var conversion = new ResourceConversionEntry
        {
            Id = 10u, ResourceConversionTypeEnum = 0u,
            SourceId = sourceId, SourceCount = 2u, TargetId = targetId, TargetCount = targetCount
        };
        var handler = new ClientConvertResourceHandler(NullLogger<ClientConvertResourceHandler>.Instance,
            CreateGameTableManager(resourceConversions: [conversion], items: [source.Entry, target.Entry]));

        handler.HandleMessage(session, CreateConvertResource(conversion.Id));

        IItem remaining = Assert.Single(bag);
        Assert.Equal(fits ? targetId : sourceId, remaining.Id);
        Assert.Equal(fits ? targetCount : sourceStack, remaining.StackCount);
        object[] packets = network.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted)).Select(call => call.Arguments[0]).ToArray();
        Assert.Equal(fits ? 1 : 0, packets.OfType<ServerItemAdd>().Count());
        Assert.Equal(fits ? 1 : 0, packets.OfType<ServerItemDelete>().Count());
        if (!fits)
            Assert.Equal(GenericError.ItemInventoryFull, Assert.Single(packets.OfType<ServerItemError>()).ErrorCode);
        Assert.Empty(materials.GetInvocations(nameof(ISupplySatchelManager.AddAmount)));

        void SetField(string name, object value)
            => typeof(Inventory).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(inventory, value);
    }

    [Theory]
    [InlineData(30ul, 30ul, 0ul)]
    [InlineData(35ul, 0ul, 5ul)]
    public void ConvertResource_ChecksCombinedPriceWhenTheSurchargeUsesTheSourceWallet(ulong credits, ulong expectedCredits, ulong expectedRenown)
    {
        IWorldSession session = CreateSession(out _, out var wallet, out _, out _);
        ulong renown = 0;
        wallet.SetMethodHandler(nameof(ICurrencyManager.CanAfford), args => credits >= (ulong)args[1]);
        wallet.SetMethodReturn(nameof(ICurrencyManager.CanCreditFullAmount), true);
        wallet.SetMethodHandler(nameof(ICurrencyManager.CurrencySubtractAmount), args =>
        {
            Assert.Equal(CurrencyType.Credits, args[0]);
            credits = checked(credits - (ulong)args[1]);
            return null;
        });
        wallet.SetMethodHandler(nameof(ICurrencyManager.CurrencyAddAmount), args =>
        {
            Assert.Equal(CurrencyType.Renown, args[0]);
            renown += (ulong)args[1];
            return null;
        });
        ResourceConversionEntry conversion = CreateCurrencyConversion();
        conversion.SurchargeId = conversion.SourceId;
        conversion.SurchargeCount = 10u;
        var handler = new ClientConvertResourceHandler(NullLogger<ClientConvertResourceHandler>.Instance,
            CreateGameTableManager(resourceConversions: [conversion],
                currencies: [CreateCurrency(CurrencyType.Credits), CreateCurrency(CurrencyType.Renown)]));

        handler.HandleMessage(session, CreateConvertResource(conversion.Id));

        Assert.Equal(expectedCredits, credits);
        Assert.Equal(expectedRenown, renown);
        Assert.Equal(expectedRenown > 0 ? 1 : 0, wallet.GetInvocations(nameof(ICurrencyManager.CurrencySubtractAmount)).Count);
    }

    [Fact]
    public void ConvertResource_CurrencyToCurrencyWithMissingCurrencyTableDoesNotMutate()
    {
        IWorldSession session = CreateSession(
            out RecordingDispatchProxy<IInventory> inventoryProxy,
            out RecordingDispatchProxy<ICurrencyManager> currencyProxy,
            out RecordingDispatchProxy<IReputationManager> reputationProxy,
            out _);
        currencyProxy.SetMethodReturn(nameof(ICurrencyManager.CanAfford), true);
        var handler = new ClientConvertResourceHandler(
            NullLogger<ClientConvertResourceHandler>.Instance,
            CreateGameTableManager(resourceConversions: [CreateCurrencyConversion()]));

        handler.HandleMessage(session, CreateConvertResource(10u));

        AssertNoResourceMutation(inventoryProxy, currencyProxy, reputationProxy);
    }

    [Theory]
    [InlineData("partial-cap", 40ul, 98ul, false)]
    [InlineData("overflow", 40ul, ulong.MaxValue - 2ul, false)]
    [InlineData("exact-cap", 15ul, 100ul, true)]
    [InlineData("source-target", 20ul, 100ul, true)]
    [InlineData("target-surcharge-full", 40ul, 100ul, false)]
    [InlineData("target-surcharge-fits", 15ul, 100ul, true)]
    public void ConvertResource_RequiresTheFullCreditAfterProjectedDebits(string outcome, ulong expectedCredits, ulong expectedRenown, bool accepted)
    {
        var conversion = CreateCurrencyConversion();
        if (outcome == "source-target") conversion.TargetId = conversion.SourceId;
        if (outcome is "target-surcharge-full" or "target-surcharge-fits")
        {
            conversion.SurchargeId = conversion.TargetId;
            conversion.SurchargeCount = outcome == "target-surcharge-full" ? 3u : 5u;
        }

        var tables = CreateGameTableManager(resourceConversions: [conversion], currencies:
        [
            new() { Id = (uint)CurrencyType.Credits, CapAmount = 40 },
            new() { Id = (uint)CurrencyType.Renown, CapAmount = outcome == "overflow" ? 0ul : 100ul }
        ]);
        var session = CreateSession(out _, out _, out _, out var network);
        var player = (RecordingDispatchProxy<IPlayer>)(object)session.Player;
        player.SetProperty(nameof(IPlayer.CharacterId), 42ul);
        player.SetProperty(nameof(IPlayer.Session), session);
        player.SetProperty(nameof(IPlayer.AchievementManager), RecordingDispatchProxy<NexusForever.Game.Abstract.Achievement.ICharacterAchievementManager>.Create(out _));
        var model = new CharacterModel { Id = 42 };
        model.Currency.Add(new() { Id = 42, CurrencyId = (byte)CurrencyType.Credits, Amount = 40 });
        model.Currency.Add(new() { Id = 42, CurrencyId = (byte)CurrencyType.Renown, Amount = outcome switch
        {
            "partial-cap" => 98ul,
            "overflow" => ulong.MaxValue - 2ul,
            "exact-cap" => 95ul,
            _ => 100ul
        }});
        var wallet = new CurrencyManager(tables);
        wallet.Initialise(session.Player, model);
        player.SetProperty(nameof(IPlayer.CurrencyManager), wallet);

        new ClientConvertResourceHandler(NullLogger<ClientConvertResourceHandler>.Instance, tables)
            .HandleMessage(session, CreateConvertResource(conversion.Id));

        var amounts = wallet.ToDictionary(currency => currency.Id, currency => currency.Amount);
        Assert.Equal(expectedCredits, amounts[CurrencyType.Credits]);
        Assert.Equal(expectedRenown, amounts[CurrencyType.Renown]);
        Assert.Equal(accepted, network.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted)).Any());
    }

    [Fact]
    public void ConvertResource_CurrencyToCurrencyWithKnownTablesMutatesCurrency()
    {
        IWorldSession session = CreateSession(
            out RecordingDispatchProxy<IInventory> inventoryProxy,
            out RecordingDispatchProxy<ICurrencyManager> currencyProxy,
            out RecordingDispatchProxy<IReputationManager> reputationProxy,
            out RecordingDispatchProxy<IWorldSession> sessionProxy);
        currencyProxy.SetMethodReturn(nameof(ICurrencyManager.CanAfford), true);
        currencyProxy.SetMethodReturn(nameof(ICurrencyManager.CanCreditFullAmount), true);
        var handler = new ClientConvertResourceHandler(
            NullLogger<ClientConvertResourceHandler>.Instance,
            CreateGameTableManager(
                resourceConversions: [CreateCurrencyConversion()],
                currencies: [CreateCurrency(CurrencyType.Credits), CreateCurrency(CurrencyType.Renown)]));

        handler.HandleMessage(session, CreateConvertResource(10u));

        Assert.Empty(inventoryProxy.GetInvocations(nameof(IInventory.ItemDelete)));
        Assert.Empty(inventoryProxy.GetInvocations(nameof(IInventory.ItemCreate)));
        Assert.Empty(reputationProxy.GetInvocations(nameof(IReputationManager.UpdateReputation)));
        Assert.Empty(sessionProxy.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted)));

        RecordingDispatchProxy<ICurrencyManager>.Invocation afford =
            Assert.Single(currencyProxy.GetInvocations(nameof(ICurrencyManager.CanAfford)));
        Assert.Equal(CurrencyType.Credits, afford.Arguments[0]);
        Assert.Equal(25ul, afford.Arguments[1]);

        RecordingDispatchProxy<ICurrencyManager>.Invocation subtract =
            Assert.Single(currencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencySubtractAmount)));
        Assert.Equal(CurrencyType.Credits, subtract.Arguments[0]);
        Assert.Equal(25ul, subtract.Arguments[1]);

        RecordingDispatchProxy<ICurrencyManager>.Invocation add =
            Assert.Single(currencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencyAddAmount)));
        Assert.Equal(CurrencyType.Renown, add.Arguments[0]);
        Assert.Equal(5ul, add.Arguments[1]);
    }

    [Fact]
    public void Spline2Request_WithMissingSplineTableDoesNotEmit()
    {
        IWorldSession session = CreateSession(
            out _,
            out _,
            out _,
            out RecordingDispatchProxy<IWorldSession> sessionProxy);
        var handler = new ClientSpline2RequestHandler(
            NullLogger<ClientSpline2RequestHandler>.Instance,
            CreateGameTableManager());

        handler.HandleMessage(session, CreateSpline2Request(123u));

        Assert.Empty(sessionProxy.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted)));
    }

    private static IWorldSession CreateSession(
        out RecordingDispatchProxy<IInventory> inventoryProxy,
        out RecordingDispatchProxy<ICurrencyManager> currencyProxy,
        out RecordingDispatchProxy<IReputationManager> reputationProxy,
        out RecordingDispatchProxy<IWorldSession> sessionProxy)
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        IInventory inventory = RecordingDispatchProxy<IInventory>.Create(out inventoryProxy);
        ICurrencyManager currencyManager = RecordingDispatchProxy<ICurrencyManager>.Create(out currencyProxy);
        IReputationManager reputationManager = RecordingDispatchProxy<IReputationManager>.Create(out reputationProxy);

        playerProxy.SetProperty(nameof(IPlayer.Guid), 1234u);
        playerProxy.SetProperty(nameof(IPlayer.Inventory), inventory);
        playerProxy.SetProperty(nameof(IPlayer.CurrencyManager), currencyManager);
        playerProxy.SetProperty(nameof(IPlayer.ReputationManager), reputationManager);
        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);
        return session;
    }

    private static IGameTableManager CreateGameTableManager(
        IReadOnlyCollection<ResourceConversionEntry> resourceConversions = null,
        IReadOnlyCollection<Item2Entry> items = null,
        IReadOnlyCollection<CurrencyTypeEntry> currencies = null,
        IReadOnlyCollection<Spline2Entry> splines = null)
    {
        IGameTableManager gameTableManager = RecordingDispatchProxy<IGameTableManager>.Create(out RecordingDispatchProxy<IGameTableManager> proxy);

        if (resourceConversions != null)
            proxy.SetProperty(nameof(IGameTableManager.ResourceConversion), CreateGameTable(resourceConversions));

        if (items != null)
            proxy.SetProperty(nameof(IGameTableManager.Item), CreateGameTable(items));

        if (currencies != null)
            proxy.SetProperty(nameof(IGameTableManager.CurrencyType), CreateGameTable(currencies));

        if (splines != null)
            proxy.SetProperty(nameof(IGameTableManager.Spline2), CreateGameTable(splines));

        return gameTableManager;
    }

    private static ResourceConversionEntry CreateCurrencyConversion()
    {
        return new ResourceConversionEntry
        {
            Id                         = 10u,
            ResourceConversionTypeEnum = 4u,
            SourceId                   = (uint)CurrencyType.Credits,
            SourceCount                = 25u,
            TargetId                   = (uint)CurrencyType.Renown,
            TargetCount                = 5u
        };
    }

    private static CurrencyTypeEntry CreateCurrency(CurrencyType currencyType)
    {
        return new CurrencyTypeEntry
        {
            Id = (uint)currencyType
        };
    }

    private static IItemInfo CreateItemInfo(uint id, uint maxStack)
    {
        IItemInfo item = RecordingDispatchProxy<IItemInfo>.Create(out var info);
        info.SetProperty(nameof(IItemInfo.Id), id);
        info.SetProperty(nameof(IItemInfo.Entry), new Item2Entry { Id = id, MaxStackCount = maxStack });
        info.SetMethodReturn(nameof(IItemInfo.IsStackable), maxStack > 1);
        return item;
    }

    private static ClientConvertResource CreateConvertResource(uint conversionId)
    {
        using var stream = new MemoryStream();
        using (var writer = new GamePacketWriter(stream))
        {
            writer.Write(conversionId, 14u);
            writer.Write(0u);
            writer.Write(0ul);
            writer.FlushBits();
        }
        using var reader = new GamePacketReader(new MemoryStream(stream.ToArray()));
        var request = new ClientConvertResource();
        request.Read(reader);
        return request;
    }

    private static ClientSpline2Request CreateSpline2Request(uint spline2Id)
    {
        var request = (ClientSpline2Request)RuntimeHelpers.GetUninitializedObject(typeof(ClientSpline2Request));
        SetProperty(request, nameof(ClientSpline2Request.Spline2Id), spline2Id);
        return request;
    }

    private static void SetProperty(object target, string propertyName, object value)
    {
        target.GetType()
            .GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?.SetValue(target, value);
    }

    private static void AssertNoResourceMutation(
        RecordingDispatchProxy<IInventory> inventoryProxy,
        RecordingDispatchProxy<ICurrencyManager> currencyProxy,
        RecordingDispatchProxy<IReputationManager> reputationProxy)
    {
        Assert.Empty(inventoryProxy.GetInvocations(nameof(IInventory.HasItemCount)));
        Assert.Empty(inventoryProxy.GetInvocations(nameof(IInventory.ItemDelete)));
        Assert.Empty(inventoryProxy.GetInvocations(nameof(IInventory.ItemCreate)));
        Assert.Empty(currencyProxy.GetInvocations(nameof(ICurrencyManager.CanAfford)));
        Assert.Empty(currencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencySubtractAmount)));
        Assert.Empty(currencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencyAddAmount)));
        Assert.Empty(reputationProxy.GetInvocations(nameof(IReputationManager.UpdateReputation)));
    }

    private static GameTable<T> CreateGameTable<T>(IReadOnlyCollection<T> entries) where T : class, new()
    {
        var table = (GameTable<T>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<T>));

        typeof(GameTable<T>)
            .GetProperty(nameof(GameTable<T>.Entries), BindingFlags.Instance | BindingFlags.Public)
            ?.SetValue(table, entries.ToArray());

        FieldInfo idField = typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public)[0];
        ulong maxId = entries
            .Select(entry => Convert.ToUInt64(idField.GetValue(entry)))
            .DefaultIfEmpty(0ul)
            .Max() + 1ul;

        var lookup = Enumerable.Repeat(-1, (int)maxId).ToArray();
        int index = 0;
        foreach (T entry in entries)
        {
            ulong id = Convert.ToUInt64(idField.GetValue(entry));
            lookup[id] = index++;
        }

        typeof(GameTable<T>)
            .GetField("lookup", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(table, lookup);

        typeof(GameTable<T>)
            .GetField("header", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(table, new GameTableHeader { MaxId = maxId });

        return table;
    }
}
