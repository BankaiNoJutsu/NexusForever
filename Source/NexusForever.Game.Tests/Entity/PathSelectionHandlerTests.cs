using System.Reflection;
using System.Runtime.CompilerServices;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Account.Currency;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Account;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model.PlayerPath;
using NexusForever.Network.World.Message.Static;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Path;
using Path = NexusForever.Game.Static.PlayerPath.Path;

namespace NexusForever.Game.Tests.Entity;

public class PathSelectionHandlerTests
{
    [Fact]
    public void UnlockRequest_WithMissingCostFormulaSendsStaticDataErrorWithoutMutation()
    {
        IWorldSession session = CreateSession(
            out _,
            out RecordingDispatchProxy<IAccountCurrencyManager> currencyProxy,
            out RecordingDispatchProxy<IPathManager> pathManagerProxy);
        var handler = new ClientPathUnlockHandler(CreateGameTableManager());

        handler.HandleMessage(session, CreateUnlockRequest(Path.Settler));

        RecordingDispatchProxy<IPathManager>.Invocation result =
            Assert.Single(pathManagerProxy.GetInvocations(nameof(IPathManager.SendServerPathUnlockResult)));
        Assert.Equal(GenericError.ItemBadStaticData, result.Arguments[0]);
        Assert.Empty(currencyProxy.GetInvocations(nameof(IAccountCurrencyManager.CanAfford)));
        Assert.Empty(currencyProxy.GetInvocations(nameof(IAccountCurrencyManager.CurrencySubtractAmount)));
        Assert.Empty(pathManagerProxy.GetInvocations(nameof(IPathManager.UnlockPath)));
    }

    [Fact]
    public void ChangeRequest_WithMissingFormulaSendsStaticDataErrorWithoutMutation()
    {
        IWorldSession session = CreateSession(
            out _,
            out RecordingDispatchProxy<IAccountCurrencyManager> currencyProxy,
            out RecordingDispatchProxy<IPathManager> pathManagerProxy);
        var handler = new ClientPathChangeRequestHandler(CreateGameTableManager());

        handler.HandleMessage(session, CreateChangeRequest(Path.Scientist, onCooldown: true));

        RecordingDispatchProxy<IPathManager>.Invocation result =
            Assert.Single(pathManagerProxy.GetInvocations(nameof(IPathManager.SendServerPathActivateResult)));
        Assert.Equal(GenericError.ItemBadStaticData, result.Arguments[0]);
        Assert.Empty(currencyProxy.GetInvocations(nameof(IAccountCurrencyManager.CanAfford)));
        Assert.Empty(currencyProxy.GetInvocations(nameof(IAccountCurrencyManager.CurrencySubtractAmount)));
        Assert.Empty(pathManagerProxy.GetInvocations(nameof(IPathManager.ActivatePath)));
    }

    [Fact]
    public void UnlockRequest_WithCostFormulaKeepsExistingDebitAndUnlockPath()
    {
        IWorldSession session = CreateSession(
            out _,
            out RecordingDispatchProxy<IAccountCurrencyManager> currencyProxy,
            out RecordingDispatchProxy<IPathManager> pathManagerProxy);
        currencyProxy.SetMethodReturn(nameof(IAccountCurrencyManager.CanAfford), true);
        pathManagerProxy.SetMethodReturn(nameof(IPathManager.IsPathUnlocked), false);
        var handler = new ClientPathUnlockHandler(CreateGameTableManager(new GameFormulaEntry
        {
            Id = 2365u,
            Dataint0 = 7u
        }));

        handler.HandleMessage(session, CreateUnlockRequest(Path.Settler));

        RecordingDispatchProxy<IPathManager>.Invocation unlock =
            Assert.Single(pathManagerProxy.GetInvocations(nameof(IPathManager.UnlockPath)));
        Assert.Equal(Path.Settler, unlock.Arguments[0]);

        RecordingDispatchProxy<IAccountCurrencyManager>.Invocation debit =
            Assert.Single(currencyProxy.GetInvocations(nameof(IAccountCurrencyManager.CurrencySubtractAmount)));
        Assert.Equal(AccountCurrencyType.ServiceToken, debit.Arguments[0]);
        Assert.Equal(7ul, debit.Arguments[1]);
        Assert.Empty(pathManagerProxy.GetInvocations(nameof(IPathManager.SendServerPathUnlockResult)));
    }

    [Fact]
    public void ChangeRequest_WithFormulaKeepsExistingActivatePath()
    {
        IWorldSession session = CreateSession(
            out _,
            out RecordingDispatchProxy<IAccountCurrencyManager> currencyProxy,
            out RecordingDispatchProxy<IPathManager> pathManagerProxy);
        pathManagerProxy.SetMethodReturn(nameof(IPathManager.IsPathUnlocked), true);
        pathManagerProxy.SetMethodReturn(nameof(IPathManager.IsPathActive), false);
        var handler = new ClientPathChangeRequestHandler(CreateGameTableManager(new GameFormulaEntry
        {
            Id = 2366u,
            Dataint0 = 0u,
            Dataint01 = 11u
        }));

        handler.HandleMessage(session, CreateChangeRequest(Path.Scientist, onCooldown: false));

        RecordingDispatchProxy<IPathManager>.Invocation activate =
            Assert.Single(pathManagerProxy.GetInvocations(nameof(IPathManager.ActivatePath)));
        Assert.Equal(Path.Scientist, activate.Arguments[0]);
        Assert.Empty(currencyProxy.GetInvocations(nameof(IAccountCurrencyManager.CurrencySubtractAmount)));
        Assert.Empty(pathManagerProxy.GetInvocations(nameof(IPathManager.SendServerPathActivateResult)));
    }

    private static IWorldSession CreateSession(
        out RecordingDispatchProxy<IWorldSession> sessionProxy,
        out RecordingDispatchProxy<IAccountCurrencyManager> currencyProxy,
        out RecordingDispatchProxy<IPathManager> pathManagerProxy)
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out sessionProxy);
        IAccount account = RecordingDispatchProxy<IAccount>.Create(out RecordingDispatchProxy<IAccount> accountProxy);
        IAccountCurrencyManager currencyManager = RecordingDispatchProxy<IAccountCurrencyManager>.Create(out currencyProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        IPathManager pathManager = RecordingDispatchProxy<IPathManager>.Create(out pathManagerProxy);

        accountProxy.SetProperty(nameof(IAccount.CurrencyManager), currencyManager);
        playerProxy.SetProperty(nameof(IPlayer.PathManager), pathManager);
        playerProxy.SetProperty(nameof(IPlayer.PathActivatedTime), DateTime.UtcNow.AddDays(-1d));
        sessionProxy.SetProperty(nameof(IWorldSession.Account), account);
        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);

        return session;
    }

    private static IGameTableManager CreateGameTableManager(params GameFormulaEntry[] formulas)
    {
        IGameTableManager gameTableManager = RecordingDispatchProxy<IGameTableManager>.Create(
            out RecordingDispatchProxy<IGameTableManager> gameTableProxy);
        gameTableProxy.SetProperty(nameof(IGameTableManager.GameFormula), CreateGameTable(formulas));
        return gameTableManager;
    }

    private static ClientPathUnlockRequest CreateUnlockRequest(Path path)
    {
        var request = (ClientPathUnlockRequest)RuntimeHelpers.GetUninitializedObject(typeof(ClientPathUnlockRequest));
        SetAutoProperty(request, nameof(ClientPathUnlockRequest.Path), path);
        return request;
    }

    private static ClientPathChangeRequest CreateChangeRequest(Path path, bool onCooldown)
    {
        var request = (ClientPathChangeRequest)RuntimeHelpers.GetUninitializedObject(typeof(ClientPathChangeRequest));
        SetAutoProperty(request, nameof(ClientPathChangeRequest.Path), path);
        SetAutoProperty(request, nameof(ClientPathChangeRequest.OnCooldown), onCooldown);
        return request;
    }

    private static GameTable<T> CreateGameTable<T>(params T[] entries) where T : class, new()
    {
        var table = (GameTable<T>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<T>));
        int maxId = (int)entries.Select(GetEntryId).DefaultIfEmpty(0u).Max();
        SetAutoProperty(table, nameof(GameTable<T>.Entries), entries);
        SetPrivateField(table, "header", new GameTableHeader
        {
            MaxId = (ulong)(maxId + 1)
        });

        int[] lookup = Enumerable.Repeat(-1, maxId + 1).ToArray();
        for (int i = 0; i < entries.Length; i++)
            lookup[(int)GetEntryId(entries[i])] = i;

        SetPrivateField(table, "lookup", lookup);
        return table;
    }

    private static uint GetEntryId<T>(T entry) where T : class, new()
    {
        return (uint)typeof(T)
            .GetField("Id", BindingFlags.Instance | BindingFlags.Public)!
            .GetValue(entry)!;
    }

    private static void SetAutoProperty(object instance, string propertyName, object value)
    {
        SetPrivateField(instance, $"<{propertyName}>k__BackingField", value);
    }

    private static void SetPrivateField(object instance, string fieldName, object value)
    {
        FieldInfo field = instance.GetType()
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!;
        field.SetValue(instance, value);
    }
}
