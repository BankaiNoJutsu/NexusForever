using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Account.Currency;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Guild;
using NexusForever.Game.Guild;
using NexusForever.Game.Static.Account;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Guild;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Guild;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Guild;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace NexusForever.Game.Tests.Guild;

public class ClientGuildRegisterHandlerTests
{
    [Theory]
    [InlineData(GuildType.Guild, false)]
    [InlineData(GuildType.Community, false)]
    [InlineData(GuildType.Community, true)]
    public void HandleMessage_WhenCreateCostFormulaTableMissing_SendsUnableToProcessWithoutMutation(
        GuildType guildType,
        bool alternateCost)
    {
        ClientGuildRegisterHandler handler = CreateHandler(out RecordingDispatchProxy<IGameTableManager> gameTableProxy);
        gameTableProxy.SetProperty(nameof(IGameTableManager.GameFormula), null);

        IWorldSession session = CreateSession(
            out RecordingDispatchProxy<IWorldSession> sessionProxy,
            out RecordingDispatchProxy<IGuildManager> guildManagerProxy,
            out RecordingDispatchProxy<ICurrencyManager> characterCurrencyProxy,
            out RecordingDispatchProxy<IAccountCurrencyManager> accountCurrencyProxy);

        handler.HandleMessage(session, CreateRequest(guildType, alternateCost));

        ServerGuildResult result = Assert.Single(GetEncryptedMessages(sessionProxy).OfType<ServerGuildResult>());
        Assert.Equal(GuildResult.UnableToProcess, result.Result);
        Assert.Empty(characterCurrencyProxy.GetInvocations(nameof(ICurrencyManager.CanAfford)));
        Assert.Empty(characterCurrencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencySubtractAmount)));
        Assert.Empty(accountCurrencyProxy.GetInvocations(nameof(IAccountCurrencyManager.CanAfford)));
        Assert.Empty(accountCurrencyProxy.GetInvocations(nameof(IAccountCurrencyManager.CurrencySubtractAmount)));
        Assert.Empty(guildManagerProxy.GetInvocations(nameof(IGuildManager.CanRegisterGuild)));
        Assert.Empty(guildManagerProxy.GetInvocations(nameof(IGuildManager.RegisterGuild)));
    }

    [Fact]
    public void HandleMessage_WithGuildCreditCost_RegistersAndDebitsCredits()
    {
        ClientGuildRegisterHandler handler = CreateHandler(out RecordingDispatchProxy<IGameTableManager> gameTableProxy);
        gameTableProxy.SetProperty(nameof(IGameTableManager.GameFormula), CreateGameTable(new GameFormulaEntry
        {
            Id       = 764u,
            Dataint0 = 321u
        }));

        IWorldSession session = CreateSession(
            out RecordingDispatchProxy<IWorldSession> sessionProxy,
            out RecordingDispatchProxy<IGuildManager> guildManagerProxy,
            out RecordingDispatchProxy<ICurrencyManager> characterCurrencyProxy,
            out RecordingDispatchProxy<IAccountCurrencyManager> accountCurrencyProxy);
        characterCurrencyProxy.SetMethodReturn(nameof(ICurrencyManager.CanAfford), true);
        guildManagerProxy.SetMethodReturn(nameof(IGuildManager.CanRegisterGuild), new GuildResultInfo(GuildResult.Success));

        ClientGuildRegister request = CreateRequest(GuildType.Guild, false);
        handler.HandleMessage(session, request);

        Assert.Empty(GetEncryptedMessages(sessionProxy));

        RecordingDispatchProxy<ICurrencyManager>.Invocation canAfford =
            Assert.Single(characterCurrencyProxy.GetInvocations(nameof(ICurrencyManager.CanAfford)));
        Assert.Equal(CurrencyType.Credits, canAfford.Arguments[0]);
        Assert.Equal(321ul, Convert.ToUInt64(canAfford.Arguments[1]));

        RecordingDispatchProxy<ICurrencyManager>.Invocation subtract =
            Assert.Single(characterCurrencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencySubtractAmount)));
        Assert.Equal(CurrencyType.Credits, subtract.Arguments[0]);
        Assert.Equal(321ul, Convert.ToUInt64(subtract.Arguments[1]));

        Assert.Empty(accountCurrencyProxy.GetInvocations(nameof(IAccountCurrencyManager.CanAfford)));
        Assert.Empty(accountCurrencyProxy.GetInvocations(nameof(IAccountCurrencyManager.CurrencySubtractAmount)));
        Assert.Same(request, Assert.Single(guildManagerProxy.GetInvocations(nameof(IGuildManager.CanRegisterGuild))).Arguments[0]);
        Assert.Same(request, Assert.Single(guildManagerProxy.GetInvocations(nameof(IGuildManager.RegisterGuild))).Arguments[0]);
    }

    [Fact]
    public void HandleMessage_WithCommunityAlternateCost_RegistersAndDebitsServiceTokens()
    {
        ClientGuildRegisterHandler handler = CreateHandler(out RecordingDispatchProxy<IGameTableManager> gameTableProxy);
        gameTableProxy.SetProperty(nameof(IGameTableManager.GameFormula), CreateGameTable(new GameFormulaEntry
        {
            Id        = 1159u,
            Dataint01 = 7u
        }));

        IWorldSession session = CreateSession(
            out RecordingDispatchProxy<IWorldSession> sessionProxy,
            out RecordingDispatchProxy<IGuildManager> guildManagerProxy,
            out RecordingDispatchProxy<ICurrencyManager> characterCurrencyProxy,
            out RecordingDispatchProxy<IAccountCurrencyManager> accountCurrencyProxy);
        accountCurrencyProxy.SetMethodReturn(nameof(IAccountCurrencyManager.CanAfford), true);
        guildManagerProxy.SetMethodReturn(nameof(IGuildManager.CanRegisterGuild), new GuildResultInfo(GuildResult.Success));

        ClientGuildRegister request = CreateRequest(GuildType.Community, true);
        handler.HandleMessage(session, request);

        Assert.Empty(GetEncryptedMessages(sessionProxy));
        Assert.Empty(characterCurrencyProxy.GetInvocations(nameof(ICurrencyManager.CanAfford)));
        Assert.Empty(characterCurrencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencySubtractAmount)));

        RecordingDispatchProxy<IAccountCurrencyManager>.Invocation canAfford =
            Assert.Single(accountCurrencyProxy.GetInvocations(nameof(IAccountCurrencyManager.CanAfford)));
        Assert.Equal(AccountCurrencyType.ServiceToken, canAfford.Arguments[0]);
        Assert.Equal(7ul, Convert.ToUInt64(canAfford.Arguments[1]));

        RecordingDispatchProxy<IAccountCurrencyManager>.Invocation subtract =
            Assert.Single(accountCurrencyProxy.GetInvocations(nameof(IAccountCurrencyManager.CurrencySubtractAmount)));
        Assert.Equal(AccountCurrencyType.ServiceToken, subtract.Arguments[0]);
        Assert.Equal(7ul, Convert.ToUInt64(subtract.Arguments[1]));

        Assert.Same(request, Assert.Single(guildManagerProxy.GetInvocations(nameof(IGuildManager.CanRegisterGuild))).Arguments[0]);
        Assert.Same(request, Assert.Single(guildManagerProxy.GetInvocations(nameof(IGuildManager.RegisterGuild))).Arguments[0]);
    }

    private static ClientGuildRegisterHandler CreateHandler(out RecordingDispatchProxy<IGameTableManager> gameTableProxy)
    {
        IGameTableManager gameTableManager = RecordingDispatchProxy<IGameTableManager>.Create(out gameTableProxy);
        return new ClientGuildRegisterHandler(gameTableManager);
    }

    private static IWorldSession CreateSession(
        out RecordingDispatchProxy<IWorldSession> sessionProxy,
        out RecordingDispatchProxy<IGuildManager> guildManagerProxy,
        out RecordingDispatchProxy<ICurrencyManager> characterCurrencyProxy,
        out RecordingDispatchProxy<IAccountCurrencyManager> accountCurrencyProxy)
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        IAccount account = RecordingDispatchProxy<IAccount>.Create(out RecordingDispatchProxy<IAccount> accountProxy);
        IGuildManager guildManager = RecordingDispatchProxy<IGuildManager>.Create(out guildManagerProxy);
        ICurrencyManager characterCurrency = RecordingDispatchProxy<ICurrencyManager>.Create(out characterCurrencyProxy);
        IAccountCurrencyManager accountCurrency = RecordingDispatchProxy<IAccountCurrencyManager>.Create(out accountCurrencyProxy);

        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);
        sessionProxy.SetProperty(nameof(IWorldSession.Account), account);
        playerProxy.SetProperty(nameof(IPlayer.GuildManager), guildManager);
        playerProxy.SetProperty(nameof(IPlayer.CurrencyManager), characterCurrency);
        accountProxy.SetProperty(nameof(IAccount.CurrencyManager), accountCurrency);

        return session;
    }

    private static ClientGuildRegister CreateRequest(GuildType guildType, bool alternateCost)
    {
        var request = new ClientGuildRegister();
        SetPacketProperty(request, nameof(ClientGuildRegister.GuildType), guildType);
        SetPacketProperty(request, nameof(ClientGuildRegister.GuildName), "New Guild");
        SetPacketProperty(request, nameof(ClientGuildRegister.MasterTitle), "Leader");
        SetPacketProperty(request, nameof(ClientGuildRegister.CouncilTitle), "Council");
        SetPacketProperty(request, nameof(ClientGuildRegister.MemberTitle), "Member");
        SetPacketProperty(request, nameof(ClientGuildRegister.AlternateCost), alternateCost);
        return request;
    }

    private static IEnumerable<object> GetEncryptedMessages(RecordingDispatchProxy<IWorldSession> sessionProxy)
    {
        return sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(invocation => invocation.Arguments[0]);
    }

    private static GameTable<T> CreateGameTable<T>(params T[] entries) where T : class, new()
    {
        var table = (GameTable<T>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<T>));
        SetAutoProperty(table, nameof(GameTable<T>.Entries), entries);
        SetPrivateField(table, "header", new GameTableHeader
        {
            MaxId = entries.Length == 0 ? 0u : entries.Max(GetEntryId) + 1u
        });
        SetPrivateField(table, "lookup", BuildLookup(entries));
        return table;
    }

    private static int[] BuildLookup<T>(IReadOnlyList<T> entries)
    {
        if (entries.Count == 0)
            return [];

        int[] lookup = Enumerable.Repeat(-1, (int)(entries.Max(GetEntryId) + 1u)).ToArray();
        for (int i = 0; i < entries.Count; i++)
            lookup[GetEntryId(entries[i])] = i;

        return lookup;
    }

    private static uint GetEntryId<T>(T entry)
    {
        return (uint)typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public)[0].GetValue(entry)!;
    }

    private static void SetPacketProperty(object packet, string propertyName, object value)
    {
        PropertyInfo property = packet.GetType().GetProperty(propertyName)!;
        property.GetSetMethod(true)!.Invoke(packet, [value]);
    }

    private static void SetAutoProperty(object instance, string propertyName, object value)
    {
        FieldInfo backingField = instance.GetType()
            .GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!;
        backingField.SetValue(instance, value);
    }

    private static void SetPrivateField(object instance, string fieldName, object value)
    {
        FieldInfo field = instance.GetType()
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!;
        field.SetValue(instance, value);
    }
}
