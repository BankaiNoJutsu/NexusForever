using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using NexusForever.Database;
using NexusForever.Database.Auth.Model;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Account.Entitlement;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Shared;

namespace NexusForever.Game.Tests.Account.Entitlement;

[Collection(LegacyServiceProviderCollection.Name)]
public class EntitlementManagerTests
{
    [Fact]
    public void AccountConstructor_WithPersistedEntitlementAndMissingEntitlementTableThrowsDatabaseDataException()
    {
        using var scope = new LegacyServiceProviderScope(BuildProvider());
        IAccount account = CreateAccount(out _);

        Assert.Throws<DatabaseDataException>(() => new AccountEntitlementManager(
            account,
            CreateAccountModelWithEntitlement(EntitlementType.Signature)));
    }

    [Fact]
    public void CharacterConstructor_WithPersistedEntitlementAndMissingEntitlementTableThrowsDatabaseDataException()
    {
        using var scope = new LegacyServiceProviderScope(BuildProvider());
        IPlayer player = CreatePlayer(out _);

        Assert.Throws<DatabaseDataException>(() => new CharacterEntitlementManager(
            player,
            CreateCharacterModelWithEntitlement(EntitlementType.Signature)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UpdateEntitlement_WithMissingStaticDataThrowsBeforeMutating(bool includeEmptyTable)
    {
        using var scope = new LegacyServiceProviderScope(BuildProvider(includeEmptyTable ? CreateGameTable<EntitlementEntry>() : null));
        IAccount account = CreateAccount(out RecordingDispatchProxy<IGameSession> sessionProxy);
        var manager = new AccountEntitlementManager(account, new AccountModel
        {
            Id = 77u
        });

        Assert.Throws<ArgumentException>(() => manager.UpdateEntitlement(EntitlementType.Signature, 1));

        Assert.Null(manager.GetEntitlement(EntitlementType.Signature));
        Assert.Empty(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
    }

    private static IAccount CreateAccount(out RecordingDispatchProxy<IGameSession> sessionProxy)
    {
        IAccount account = RecordingDispatchProxy<IAccount>.Create(out RecordingDispatchProxy<IAccount> accountProxy);
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out sessionProxy);

        accountProxy.SetProperty(nameof(IAccount.Id), 77u);
        accountProxy.SetProperty(nameof(IAccount.Session), session);
        return account;
    }

    private static IPlayer CreatePlayer(out RecordingDispatchProxy<IGameSession> sessionProxy)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out sessionProxy);

        playerProxy.SetProperty(nameof(IPlayer.CharacterId), 42ul);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);
        return player;
    }

    private static AccountModel CreateAccountModelWithEntitlement(EntitlementType entitlementType)
    {
        return new AccountModel
        {
            Id = 77u,
            AccountEntitlement =
            [
                new AccountEntitlementModel
                {
                    Id            = 77u,
                    EntitlementId = (byte)entitlementType,
                    Amount        = 1u
                }
            ]
        };
    }

    private static CharacterModel CreateCharacterModelWithEntitlement(EntitlementType entitlementType)
    {
        return new CharacterModel
        {
            Id = 42ul,
            Entitlement =
            [
                new CharacterEntitlementModel
                {
                    Id            = 42ul,
                    EntitlementId = (byte)entitlementType,
                    Amount        = 1u
                }
            ]
        };
    }

    private static IServiceProvider BuildProvider(GameTable<EntitlementEntry> entitlementTable = null)
    {
        var gameTableManager = (GameTableManager)RuntimeHelpers.GetUninitializedObject(typeof(GameTableManager));
        if (entitlementTable != null)
            SetAutoProperty(gameTableManager, nameof(GameTableManager.Entitlement), entitlementTable);

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
