using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NexusForever.Database.Auth;
using NexusForever.Database.Auth.Model;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Account.Unlock;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.GenericUnlock;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Configuration.Model;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.GenericUnlock;
using NexusForever.Shared;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

namespace NexusForever.Game.Tests.Account.Inventory;

[Collection(LegacyServiceProviderCollection.Name)]
public class GenericUnlockManagerTests
{
    [Fact]
    public void Unlock_WithMissingUnlockEntryTableSendsInvalid()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildProvider();

        try
        {
            GenericUnlockManager manager = CreateManager(out RecordingDispatchProxy<IGameSession> sessionProxy);

            manager.Unlock(11);

            ServerGenericUnlockResult result = Assert.Single(sessionProxy
                .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
                .Select(i => i.Arguments[0])
                .OfType<ServerGenericUnlockResult>());
            Assert.Equal(GenericUnlockResult.Invalid, result.Result);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void UnlockAll_WithMissingUnlockEntryTableDoesNotEmit()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildProvider();

        try
        {
            GenericUnlockManager manager = CreateManager(out RecordingDispatchProxy<IGameSession> sessionProxy);

            manager.UnlockAll(GenericUnlockType.Dye);

            Assert.Empty(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void Constructor_WithPersistedUnlockAndMissingUnlockEntryTableSkipsUnlock()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildProvider();

        try
        {
            GenericUnlockManager manager = CreateManager(CreateAccountModelWithUnlock(11u), out RecordingDispatchProxy<IGameSession> sessionProxy);

            Assert.False(manager.IsDyeUnlocked(22u));

            manager.SendUnlockList();

            ServerGenericUnlockAccountList list = Assert.Single(sessionProxy
                .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
                .Select(i => i.Arguments[0])
                .OfType<ServerGenericUnlockAccountList>());
            Assert.Empty(list.GenericUnlockEntryIds);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void Constructor_WithPersistedUnlockAndKnownUnlockEntryLoadsUnlock()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildProvider(CreateGameTable(new GenericUnlockEntryEntry
        {
            Id                    = 11u,
            GenericUnlockTypeEnum = GenericUnlockType.Dye,
            UnlockObject          = 22u
        }));

        try
        {
            GenericUnlockManager manager = CreateManager(CreateAccountModelWithUnlock(11u), out RecordingDispatchProxy<IGameSession> sessionProxy);

            Assert.True(manager.IsDyeUnlocked(22u));

            manager.SendUnlockList();

            ServerGenericUnlockAccountList list = Assert.Single(sessionProxy
                .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
                .Select(i => i.Arguments[0])
                .OfType<ServerGenericUnlockAccountList>());
            Assert.Equal([11u], list.GenericUnlockEntryIds);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void Save_WithPersistedUnlockLoadedFromTableDoesNotTrackAddedRow()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildProvider(CreateGameTable(new GenericUnlockEntryEntry
        {
            Id                    = 11u,
            GenericUnlockTypeEnum = GenericUnlockType.Dye,
            UnlockObject          = 22u
        }));

        try
        {
            GenericUnlockManager manager = CreateManager(CreateAccountModelWithUnlock(11u), out _);
            using AuthContext context = CreateContext();

            manager.Save(context);

            Assert.Empty(context.ChangeTracker.Entries<AccountGenericUnlockModel>());
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void Unlock_WithKnownUnlockEntrySendsUnlockAndGranted()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildProvider(CreateGameTable(new GenericUnlockEntryEntry
        {
            Id                    = 11u,
            GenericUnlockTypeEnum = GenericUnlockType.Dye,
            UnlockObject          = 22u
        }));

        try
        {
            GenericUnlockManager manager = CreateManager(out RecordingDispatchProxy<IGameSession> sessionProxy);

            manager.Unlock(11);

            object[] messages = sessionProxy
                .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
                .Select(i => i.Arguments[0])
                .ToArray();

            ServerGenericUnlock unlock = Assert.IsType<ServerGenericUnlock>(messages[0]);
            Assert.Equal((ushort)11, unlock.GenericUnlockEntryId);

            ServerGenericUnlockResult result = Assert.IsType<ServerGenericUnlockResult>(messages[1]);
            Assert.Equal(GenericUnlockResult.Granted, result.Result);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    private static GenericUnlockManager CreateManager(out RecordingDispatchProxy<IGameSession> sessionProxy)
    {
        return CreateManager(new AccountModel
        {
            Id = 1u
        }, out sessionProxy);
    }

    private static GenericUnlockManager CreateManager(AccountModel model, out RecordingDispatchProxy<IGameSession> sessionProxy)
    {
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out sessionProxy);
        IAccount account = RecordingDispatchProxy<IAccount>.Create(out RecordingDispatchProxy<IAccount> accountProxy);
        accountProxy.SetProperty(nameof(IAccount.Session), session);

        return new GenericUnlockManager(account, model);
    }

    private static AccountModel CreateAccountModelWithUnlock(uint entryId)
    {
        return new AccountModel
        {
            Id = 1u,
            AccountGenericUnlock =
            [
                new AccountGenericUnlockModel
                {
                    Id    = 1u,
                    Entry = entryId
                }
            ]
        };
    }

    private static AuthContext CreateContext()
    {
        DbContextOptions<AuthContext> options = new DbContextOptionsBuilder<AuthContext>()
            .UseMySql(
                "Server=127.0.0.1;Database=nexus_forever_auth;User ID=nexus_forever;Password=nexus_forever;",
                new MySqlServerVersion(new Version(8, 0, 0)))
            .Options;

        return new AuthContext(options);
    }

    private static IServiceProvider BuildProvider(GameTable<GenericUnlockEntryEntry> genericUnlockEntryTable = null)
    {
        var gameTableManager = new GameTableManager(Options.Create(new GameTableConfig
        {
            GameTablePath = string.Empty
        }));

        if (genericUnlockEntryTable != null)
            SetAutoProperty(gameTableManager, nameof(GameTableManager.GenericUnlockEntry), genericUnlockEntryTable);

        return new ServiceCollection()
            .AddSingleton(gameTableManager)
            .BuildServiceProvider();
    }

    private static GameTable<T> CreateGameTable<T>(params T[] entries) where T : class, new()
    {
        var table = (GameTable<T>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<T>));
        SetAutoProperty(table, nameof(GameTable<T>.Entries), entries);

        FieldInfo idField = typeof(T).GetField("Id", BindingFlags.Instance | BindingFlags.Public)!;
        uint maxId = entries.Select(entry => (uint)idField.GetValue(entry)!).DefaultIfEmpty().Max();
        var lookup = Enumerable.Repeat(-1, (int)maxId + 1).ToArray();
        for (int i = 0; i < entries.Length; i++)
            lookup[(int)(uint)idField.GetValue(entries[i])!] = i;

        SetField(table, "lookup", lookup);
        SetField(table, "header", new GameTableHeader
        {
            MaxId = (ulong)lookup.Length
        });

        return table;
    }

    private static void SetAutoProperty(object instance, string propertyName, object value)
    {
        FieldInfo backingField = instance.GetType().GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!;
        backingField.SetValue(instance, value);
    }

    private static void SetField(object instance, string fieldName, object value)
    {
        FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!;
        field.SetValue(instance, value);
    }
}
