using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Reputation;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model;
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

    [Fact]
    public void ConvertResource_CurrencyToCurrencyWithKnownTablesMutatesCurrency()
    {
        IWorldSession session = CreateSession(
            out RecordingDispatchProxy<IInventory> inventoryProxy,
            out RecordingDispatchProxy<ICurrencyManager> currencyProxy,
            out RecordingDispatchProxy<IReputationManager> reputationProxy,
            out RecordingDispatchProxy<IWorldSession> sessionProxy);
        currencyProxy.SetMethodReturn(nameof(ICurrencyManager.CanAfford), true);
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

    private static ClientConvertResource CreateConvertResource(uint conversionId)
    {
        var request = (ClientConvertResource)RuntimeHelpers.GetUninitializedObject(typeof(ClientConvertResource));
        SetProperty(request, nameof(ClientConvertResource.ConversionId), conversionId);
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
