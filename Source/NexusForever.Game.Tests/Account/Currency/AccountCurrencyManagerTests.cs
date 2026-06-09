using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using NexusForever.Database.Auth.Model;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Account.Currency;
using NexusForever.Game.Static.Account;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.Message;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Shared;

namespace NexusForever.Game.Tests.Account.Currency;

[Collection(LegacyServiceProviderCollection.Name)]
public class AccountCurrencyManagerTests
{
    [Fact]
    public void Constructor_WithPersistedCurrencyAndMissingCurrencyTableKeepsBalanceForReadback()
    {
        using var scope = new LegacyServiceProviderScope(BuildProvider());

        AccountCurrencyManager manager = CreateManager(
            CreateAccountModelWithCurrency(AccountCurrencyType.Omnibit, 123ul),
            out RecordingDispatchProxy<IGameSession> sessionProxy);

        Assert.Equal(123ul, manager.GetCurrencyAmount(AccountCurrencyType.Omnibit));

        manager.SendCharacterListPacket();

        ServerAccountCurrencySet set = Assert.Single(GetMessages<ServerAccountCurrencySet>(sessionProxy));
        NexusForever.Network.World.Message.Model.Shared.AccountCurrency currency = Assert.Single(set.AccountCurrencies);
        Assert.Equal((byte)AccountCurrencyType.Omnibit, currency.AccountCurrencyType);
        Assert.Equal(123ul, currency.Amount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CurrencyAddAmount_WithMissingCurrencyStaticDataThrowsBeforeMutating(bool includeEmptyTable)
    {
        using var scope = new LegacyServiceProviderScope(BuildProvider(includeEmptyTable ? CreateGameTable<AccountCurrencyTypeEntry>() : null));
        AccountCurrencyManager manager = CreateManager(out RecordingDispatchProxy<IGameSession> sessionProxy);

        Assert.Throws<ArgumentNullException>(() => manager.CurrencyAddAmount(AccountCurrencyType.ServiceToken, 5ul, 7ul));

        Assert.Equal(0ul, manager.GetCurrencyAmount(AccountCurrencyType.ServiceToken));
        Assert.Empty(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
    }

    [Fact]
    public void CurrencySubtractAmount_WithMissingCurrencyTableThrowsBeforeMutating()
    {
        using var scope = new LegacyServiceProviderScope(BuildProvider());
        AccountCurrencyManager manager = CreateManager(out RecordingDispatchProxy<IGameSession> sessionProxy);

        Assert.Throws<ArgumentNullException>(() => manager.CurrencySubtractAmount(AccountCurrencyType.ServiceToken, 5ul, 7ul));

        Assert.Equal(0ul, manager.GetCurrencyAmount(AccountCurrencyType.ServiceToken));
        Assert.Empty(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
    }

    private static AccountCurrencyManager CreateManager(out RecordingDispatchProxy<IGameSession> sessionProxy)
    {
        return CreateManager(new AccountModel
        {
            Id = 77u
        }, out sessionProxy);
    }

    private static AccountCurrencyManager CreateManager(
        AccountModel model,
        out RecordingDispatchProxy<IGameSession> sessionProxy)
    {
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out sessionProxy);
        IAccount account = RecordingDispatchProxy<IAccount>.Create(out RecordingDispatchProxy<IAccount> accountProxy);
        accountProxy.SetProperty(nameof(IAccount.Id), model.Id);
        accountProxy.SetProperty(nameof(IAccount.Session), session);

        return new AccountCurrencyManager(account, model);
    }

    private static AccountModel CreateAccountModelWithCurrency(AccountCurrencyType currencyType, ulong amount)
    {
        return new AccountModel
        {
            Id = 77u,
            AccountCurrency =
            [
                new AccountCurrencyModel
                {
                    Id         = 77u,
                    CurrencyId = (byte)currencyType,
                    Amount     = amount
                }
            ]
        };
    }

    private static IServiceProvider BuildProvider(GameTable<AccountCurrencyTypeEntry> accountCurrencyTypeTable = null)
    {
        var gameTableManager = (GameTableManager)RuntimeHelpers.GetUninitializedObject(typeof(GameTableManager));
        if (accountCurrencyTypeTable != null)
            SetAutoProperty(gameTableManager, nameof(GameTableManager.AccountCurrencyType), accountCurrencyTypeTable);

        return new ServiceCollection()
            .AddSingleton(gameTableManager)
            .BuildServiceProvider();
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

    private static IReadOnlyList<T> GetMessages<T>(RecordingDispatchProxy<IGameSession> sessionProxy)
        where T : class, IWritable
    {
        return sessionProxy
            .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Where(i => i.Arguments.Length == 1)
            .Select(i => i.Arguments[0])
            .OfType<T>()
            .ToList();
    }

    private static void SetAutoProperty(object instance, string propertyName, object value)
    {
        FieldInfo backingField = instance.GetType()
            .GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        backingField.SetValue(instance, value);
    }

    private static void SetPrivateField(object instance, string fieldName, object value)
    {
        FieldInfo field = instance.GetType()
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        field.SetValue(instance, value);
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
        FieldInfo idField = typeof(T).GetField("Id", BindingFlags.Instance | BindingFlags.Public);
        return (uint)idField.GetValue(entry);
    }
}
