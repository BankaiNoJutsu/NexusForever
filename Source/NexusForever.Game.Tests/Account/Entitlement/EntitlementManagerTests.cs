using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database;
using NexusForever.Database.Auth.Model;
using NexusForever.Database.Character.Model;
using NexusForever.Database.Character;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Account.Entitlement;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;

namespace NexusForever.Game.Tests.Account.Entitlement;

public class EntitlementManagerTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HiddenEntitlementCanBeGrantedWithoutChangingDefaultsOrLimits(bool characterScoped)
    {
        var entry = new EntitlementEntry
        {
            Id = (uint)EntitlementType.Signature, MaxCount = 2,
            Flags = 0x2u | (characterScoped ? 0x1u : 0u)
        };
        var tables = CreateGameTableManager(CreateGameTable(entry));
        if (characterScoped)
        {
            var manager = new CharacterEntitlementManager(CreatePlayer(out _), new CharacterModel(), null, tables);
            Assert.Empty(manager); // Do not adopt upstream's seven automatic character defaults.
            manager.UpdateEntitlement(EntitlementType.Signature, 1);
            Assert.Equal(1u, manager.GetEntitlement(EntitlementType.Signature).Amount);
            Assert.Throws<InvalidOperationException>(() => manager.UpdateEntitlement(EntitlementType.Signature, 2));
            Assert.Throws<ArgumentException>(() => manager.UpdateEntitlement(EntitlementType.Signature, -2));
            Assert.Equal(1u, manager.GetEntitlement(EntitlementType.Signature).Amount);
        }
        else
        {
            var manager = new AccountEntitlementManager(CreateAccount(out _), new AccountModel(), null, tables);
            Assert.Empty(manager);
            manager.UpdateEntitlement(EntitlementType.Signature, 1);
            Assert.Equal(1u, manager.GetEntitlement(EntitlementType.Signature).Amount);
            Assert.Throws<InvalidOperationException>(() => manager.UpdateEntitlement(EntitlementType.Signature, 2));
            Assert.Throws<ArgumentException>(() => manager.UpdateEntitlement(EntitlementType.Signature, -2));
            Assert.Equal(1u, manager.GetEntitlement(EntitlementType.Signature).Amount);
        }
    }

    [Fact]
    public void NewCharacterEntitlementIsInsertedAndExistingEntitlementIsUpdated()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var context = new CharacterContext(new DbContextOptionsBuilder<CharacterContext>().UseSqlite(connection).Options);
        context.Database.EnsureCreated();
        context.Character.Add(new CharacterModel { Id = 42, Name = "EntitlementTest", OriginalName = "EntitlementTest" });
        context.SaveChanges();
        context.ChangeTracker.Clear();
        var entry = new EntitlementEntry { Id = (uint)EntitlementType.Signature, Flags = 1, MaxCount = 2 };
        var entitlement = new CharacterEntitlement(CreatePlayer(out _), entry, 1);

        entitlement.Save(context);
        Assert.Equal(EntityState.Added, Assert.Single(context.ChangeTracker.Entries<CharacterEntitlementModel>()).State);
        context.SaveChanges();
        context.ChangeTracker.Clear();

        var persisted = context.CharacterEntitlement.AsNoTracking().Single();
        Assert.Equal(1u, persisted.Amount);
        var loaded = new CharacterEntitlement(CreatePlayer(out _), persisted, entry);
        loaded.Save(context);
        Assert.Empty(context.ChangeTracker.Entries<CharacterEntitlementModel>());
        loaded.Amount = 2;
        loaded.Save(context);
        Assert.Equal(EntityState.Modified, Assert.Single(context.ChangeTracker.Entries<CharacterEntitlementModel>()).State);
        context.SaveChanges();
        Assert.Equal(2u, context.CharacterEntitlement.AsNoTracking().Single().Amount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HiddenFlagDoesNotBypassAccountCharacterScopeChecks(bool characterScoped)
    {
        var entry = new EntitlementEntry
        {
            Id = (uint)EntitlementType.Signature, MaxCount = 2,
            Flags = 0x2u | (characterScoped ? 0u : 0x1u)
        };
        var tables = CreateGameTableManager(CreateGameTable(entry));
        if (characterScoped)
        {
            var manager = new CharacterEntitlementManager(CreatePlayer(out _), new CharacterModel(), null, tables);
            Assert.Throws<InvalidOperationException>(() => manager.UpdateEntitlement(EntitlementType.Signature, 1));
            Assert.Empty(manager);
        }
        else
        {
            var manager = new AccountEntitlementManager(CreateAccount(out _), new AccountModel(), null, tables);
            Assert.Throws<InvalidOperationException>(() => manager.UpdateEntitlement(EntitlementType.Signature, 1));
            Assert.Empty(manager);
        }
    }

    [Fact]
    public void InvalidInitialAmountReportsEntryWithoutDereferencingUninitialisedState()
    {
        var entry = new EntitlementEntry { Id = (uint)EntitlementType.Signature, MaxCount = 2 };
        DatabaseDataException error = Assert.Throws<DatabaseDataException>(() => new CharacterEntitlement(CreatePlayer(out _), entry, 3));
        Assert.Contains($"entitlement {entry.Id}", error.Message);
        Assert.Contains("expected max is 2", error.Message);
    }

    [Fact]
    public void AccountConstructor_WithPersistedEntitlementAndMissingEntitlementTableThrowsDatabaseDataException()
    {
        GameTableManager gameTableManager = CreateGameTableManager();
        IAccount account = CreateAccount(out _);

        Assert.Throws<DatabaseDataException>(() => new AccountEntitlementManager(
            account,
            CreateAccountModelWithEntitlement(EntitlementType.Signature),
            assetManager: null,
            gameTableManager: gameTableManager));
    }

    [Fact]
    public void CharacterConstructor_WithPersistedEntitlementAndMissingEntitlementTableThrowsDatabaseDataException()
    {
        GameTableManager gameTableManager = CreateGameTableManager();
        IPlayer player = CreatePlayer(out _);

        Assert.Throws<DatabaseDataException>(() => new CharacterEntitlementManager(
            player,
            CreateCharacterModelWithEntitlement(EntitlementType.Signature),
            assetManager: null,
            gameTableManager: gameTableManager));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UpdateEntitlement_WithMissingStaticDataThrowsBeforeMutating(bool includeEmptyTable)
    {
        GameTableManager gameTableManager = CreateGameTableManager(includeEmptyTable ? CreateGameTable<EntitlementEntry>() : null);
        IAccount account = CreateAccount(out RecordingDispatchProxy<IGameSession> sessionProxy);
        var manager = new AccountEntitlementManager(account, new AccountModel
        {
            Id = 77u
        }, assetManager: null, gameTableManager);

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

    private static GameTableManager CreateGameTableManager(GameTable<EntitlementEntry> entitlementTable = null)
    {
        var gameTableManager = (GameTableManager)RuntimeHelpers.GetUninitializedObject(typeof(GameTableManager));
        if (entitlementTable != null)
            SetAutoProperty(gameTableManager, nameof(GameTableManager.Entitlement), entitlementTable);

        return gameTableManager;
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
