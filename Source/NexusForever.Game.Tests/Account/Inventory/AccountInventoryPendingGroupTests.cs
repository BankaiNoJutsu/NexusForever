using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NexusForever.Database.Auth.Model;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Character;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Account.Inventory;
using NexusForever.Game.Character;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Account;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Configuration.Model;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Shared;
using GameIdentity = NexusForever.Game.Abstract.Identity;
using NetworkIdentity = NexusForever.Network.World.Message.Model.Shared.Identity;

namespace NexusForever.Game.Tests.Account.Inventory;

[Collection(LegacyServiceProviderCollection.Name)]
public class AccountInventoryPendingGroupTests
{
    private const ushort RealmId = 1;
    private const uint AccountItemId = 77u;

    [Fact]
    public void GiftPendingItemGroupToCharacter_MovesUngiftedGroupToOnlineTarget()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        var environment = CreateEnvironment(
            CreateCharacter(accountId: 1001u, characterId: 101ul, name: "Source"),
            CreateCharacter(accountId: 2002u, characterId: 202ul, name: "Target"));
        LegacyServiceProvider.Provider = environment.Provider;

        try
        {
            string group = environment.Source.Manager.AddPendingItemGroup([AccountItemId], notify: false);

            AccountOperationResult result = environment.Source.Manager.GiftPendingItemGroupToCharacter(
                environment.Source.Player,
                group,
                environment.Target.Identity);

            Assert.Equal(AccountOperationResult.Ok, result);
            Assert.Empty(GetPendingGroups(environment.Source.SessionProxy));

            ServerAccountItemsPending.PendingAccountItemGroup gifted = Assert.Single(GetPendingGroups(environment.Target.SessionProxy));
            Assert.Equal(AccountItemId, gifted.AccountItemId);
            Assert.Equal(environment.Source.Identity.Id, gifted.SenderIdentity.Id);
            Assert.Equal(environment.Source.Identity.RealmId, gifted.SenderIdentity.RealmId);
            Assert.Equal(environment.Target.Identity.Id, gifted.TargetIdentity.Id);
            Assert.Equal(environment.Target.Identity.RealmId, gifted.TargetIdentity.RealmId);

            ServerAccountOperationResult operationResult = GetLastMessage<ServerAccountOperationResult>(environment.Source.SessionProxy);
            Assert.Equal(AccountOperation.GiftItem, operationResult.Operation);
            Assert.Equal(AccountOperationResult.Ok, operationResult.Result);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void GiftPendingItemGroupToAccount_AlreadyGiftedGroupReturnsNoRegift()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        var environment = CreateEnvironment(
            CreateCharacter(accountId: 1001u, characterId: 101ul, name: "OriginalSender"),
            CreateCharacter(accountId: 2002u, characterId: 202ul, name: "Recipient"),
            CreateCharacter(accountId: 3003u, characterId: 303ul, name: "Third"));
        LegacyServiceProvider.Provider = environment.Provider;

        try
        {
            string group = environment.Target.Manager.AddPendingItemGroup(
                [AccountItemId],
                environment.Source.Identity,
                environment.Target.Identity,
                notify: false,
                senderAccountId: environment.Source.AccountId);

            AccountOperationResult result = environment.Target.Manager.GiftPendingItemGroupToAccount(
                environment.Target.Player,
                group,
                environment.Third.AccountId,
                environment.Target.Identity);

            Assert.Equal(AccountOperationResult.NoRegift, result);

            ServerAccountItemsPending.PendingAccountItemGroup preserved = Assert.Single(GetPendingGroups(environment.Target.SessionProxy));
            Assert.Equal(group, preserved.Group);
            Assert.Equal(environment.Source.Identity.Id, preserved.SenderIdentity.Id);
            Assert.Empty(GetPendingGroups(environment.Third.SessionProxy));

            ServerAccountOperationResult operationResult = GetLastMessage<ServerAccountOperationResult>(environment.Target.SessionProxy);
            Assert.Equal(AccountOperation.GiftItem, operationResult.Operation);
            Assert.Equal(AccountOperationResult.NoRegift, operationResult.Result);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void ReturnPendingItemGroup_GiftedGroupReturnsToRecordedSender()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        var environment = CreateEnvironment(
            CreateCharacter(accountId: 1001u, characterId: 101ul, name: "OriginalSender"),
            CreateCharacter(accountId: 2002u, characterId: 202ul, name: "Recipient"));
        LegacyServiceProvider.Provider = environment.Provider;

        try
        {
            string group = environment.Target.Manager.AddPendingItemGroup(
                [AccountItemId],
                environment.Source.Identity,
                environment.Target.Identity,
                notify: false,
                senderAccountId: environment.Source.AccountId);

            AccountOperationResult result = environment.Target.Manager.ReturnPendingItemGroup(environment.Target.Player, group);

            Assert.Equal(AccountOperationResult.Ok, result);
            Assert.Empty(GetPendingGroups(environment.Target.SessionProxy));

            ServerAccountItemsPending.PendingAccountItemGroup returned = Assert.Single(GetPendingGroups(environment.Source.SessionProxy));
            Assert.Equal(environment.Target.Identity.Id, returned.SenderIdentity.Id);
            Assert.Equal(environment.Source.Identity.Id, returned.TargetIdentity.Id);

            ServerAccountOperationResult operationResult = GetLastMessage<ServerAccountOperationResult>(environment.Target.SessionProxy);
            Assert.Equal(AccountOperation.ReturnPending, operationResult.Operation);
            Assert.Equal(AccountOperationResult.Ok, operationResult.Result);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void ReturnPendingItemGroup_WithoutRecordedSenderReturnsCannotReturn()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        var environment = CreateEnvironment(CreateCharacter(accountId: 2002u, characterId: 202ul, name: "Recipient"));
        LegacyServiceProvider.Provider = environment.Provider;

        try
        {
            string group = environment.Source.Manager.AddPendingItemGroup([AccountItemId], notify: false);

            AccountOperationResult result = environment.Source.Manager.ReturnPendingItemGroup(environment.Source.Player, group);

            Assert.Equal(AccountOperationResult.CannotReturn, result);

            ServerAccountItemsPending.PendingAccountItemGroup preserved = Assert.Single(GetPendingGroups(environment.Source.SessionProxy));
            Assert.Equal(group, preserved.Group);

            ServerAccountOperationResult operationResult = GetLastMessage<ServerAccountOperationResult>(environment.Source.SessionProxy);
            Assert.Equal(AccountOperation.ReturnPending, operationResult.Operation);
            Assert.Equal(AccountOperationResult.CannotReturn, operationResult.Result);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void GiftPendingItemGroupToCharacter_OfflineTargetReturnsNoConnectionAndPreservesGroup()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        var environment = CreateEnvironmentWithOnlineAccounts([1001u],
            CreateCharacter(accountId: 1001u, characterId: 101ul, name: "Source"),
            CreateCharacter(accountId: 2002u, characterId: 202ul, name: "OfflineTarget"));
        LegacyServiceProvider.Provider = environment.Provider;

        try
        {
            string group = environment.Source.Manager.AddPendingItemGroup([AccountItemId], notify: false);

            AccountOperationResult result = environment.Source.Manager.GiftPendingItemGroupToCharacter(
                environment.Source.Player,
                group,
                environment.Target.Identity);

            Assert.Equal(AccountOperationResult.NoConnection, result);

            ServerAccountItemsPending.PendingAccountItemGroup preserved = Assert.Single(GetPendingGroups(environment.Source.SessionProxy));
            Assert.Equal(group, preserved.Group);
            Assert.Equal(AccountItemId, preserved.AccountItemId);
            Assert.Empty(GetPendingGroups(environment.Target.SessionProxy));

            ServerAccountOperationResult operationResult = GetLastMessage<ServerAccountOperationResult>(environment.Source.SessionProxy);
            Assert.Equal(AccountOperation.GiftItem, operationResult.Operation);
            Assert.Equal(AccountOperationResult.NoConnection, operationResult.Result);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void ReturnPendingItemGroup_OfflineRecordedSenderReturnsNoConnectionAndPreservesGroup()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        var environment = CreateEnvironmentWithOnlineAccounts([2002u],
            CreateCharacter(accountId: 1001u, characterId: 101ul, name: "OfflineSender"),
            CreateCharacter(accountId: 2002u, characterId: 202ul, name: "Recipient"));
        LegacyServiceProvider.Provider = environment.Provider;

        try
        {
            string group = environment.Target.Manager.AddPendingItemGroup(
                [AccountItemId],
                environment.Source.Identity,
                environment.Target.Identity,
                notify: false,
                senderAccountId: environment.Source.AccountId);

            AccountOperationResult result = environment.Target.Manager.ReturnPendingItemGroup(environment.Target.Player, group);

            Assert.Equal(AccountOperationResult.NoConnection, result);

            ServerAccountItemsPending.PendingAccountItemGroup preserved = Assert.Single(GetPendingGroups(environment.Target.SessionProxy));
            Assert.Equal(group, preserved.Group);
            Assert.Equal(environment.Source.Identity.Id, preserved.SenderIdentity.Id);
            Assert.Empty(GetPendingGroups(environment.Source.SessionProxy));

            ServerAccountOperationResult operationResult = GetLastMessage<ServerAccountOperationResult>(environment.Target.SessionProxy);
            Assert.Equal(AccountOperation.ReturnPending, operationResult.Operation);
            Assert.Equal(AccountOperationResult.NoConnection, operationResult.Result);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    private static TestEnvironment CreateEnvironment(params TestCharacter[] characters)
    {
        return CreateEnvironmentWithOnlineAccounts(characters.Select(c => c.AccountId).ToArray(), characters);
    }

    private static TestEnvironment CreateEnvironmentWithOnlineAccounts(IReadOnlyCollection<uint> onlineAccountIds, params TestCharacter[] characters)
    {
        var gameTableManager = new GameTableManager(Options.Create(new GameTableConfig
        {
            GameTablePath = string.Empty
        }));
        var realmContext = (RealmContext)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(RealmContext));
        var characterManager = new CharacterManager();

        SetAutoProperty(gameTableManager, nameof(GameTableManager.AccountItem), CreateGameTable(new AccountItemEntry
        {
            Id = AccountItemId
        }));
        SetAutoProperty(gameTableManager, nameof(GameTableManager.AccountItemCooldownGroup), CreateGameTable<AccountItemCooldownGroupEntry>());
        SetAutoProperty(realmContext, nameof(RealmContext.RealmId), RealmId);
        SeedCharacterManager(characterManager, characters);

        var playerManager = new PlayerManager(NullLogger<PlayerManager>.Instance, characterManager);
        var provider = new ServiceCollection()
            .AddSingleton(gameTableManager)
            .AddSingleton(realmContext)
            .AddSingleton(characterManager)
            .AddSingleton(playerManager)
            .BuildServiceProvider();

        LegacyServiceProvider.Provider = provider;

        TestAccount source = CreateAccount(characters[0].AccountId, characters[0].Identity);
        TestAccount target = characters.Length > 1 ? CreateAccount(characters[1].AccountId, characters[1].Identity) : null;
        TestAccount third = characters.Length > 2 ? CreateAccount(characters[2].AccountId, characters[2].Identity) : null;

        if (onlineAccountIds.Contains(source.AccountId))
            playerManager.AddPlayer(source.Player);
        if (target != null && onlineAccountIds.Contains(target.AccountId))
            playerManager.AddPlayer(target.Player);
        if (third != null && onlineAccountIds.Contains(third.AccountId))
            playerManager.AddPlayer(third.Player);

        return new TestEnvironment(provider, source, target, third);
    }

    private static TestAccount CreateAccount(uint accountId, NetworkIdentity identity)
    {
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out var sessionProxy);
        IAccount account = RecordingDispatchProxy<IAccount>.Create(out var accountProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);

        accountProxy.SetProperty(nameof(IAccount.Id), accountId);
        accountProxy.SetProperty(nameof(IAccount.Session), session);

        playerProxy.SetProperty(nameof(IPlayer.Account), account);
        playerProxy.SetProperty(nameof(IPlayer.Identity), new GameIdentity
        {
            RealmId = identity.RealmId,
            Id      = identity.Id
        });
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), identity.Id);

        var manager = new AccountInventoryManager(account, new AccountModel
        {
            AccountInventory    = [],
            AccountItemCooldown = []
        });
        accountProxy.SetProperty(nameof(IAccount.InventoryManager), manager);

        return new TestAccount(accountId, identity, account, player, manager, sessionProxy);
    }

    private static TestCharacter CreateCharacter(uint accountId, ulong characterId, string name)
    {
        ICharacter character = RecordingDispatchProxy<ICharacter>.Create(out var proxy);
        proxy.SetProperty(nameof(ICharacter.AccountId), accountId);
        proxy.SetProperty(nameof(ICharacter.CharacterId), characterId);
        proxy.SetProperty(nameof(ICharacter.Name), name);

        return new TestCharacter(accountId, characterId, name, character);
    }

    private static void SeedCharacterManager(CharacterManager manager, IEnumerable<TestCharacter> characters)
    {
        SetPrivateField(manager, "characters", characters.ToDictionary(c => c.CharacterId, c => c.Character));
        SetPrivateField(manager, "characterNameToId", characters.ToDictionary(c => c.Name, c => c.CharacterId, StringComparer.OrdinalIgnoreCase));
    }

    private static IReadOnlyList<ServerAccountItemsPending.PendingAccountItemGroup> GetPendingGroups(RecordingDispatchProxy<IGameSession> sessionProxy)
    {
        return sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(invocation => invocation.Arguments[0])
            .OfType<ServerAccountItemsPending>()
            .LastOrDefault()
            ?.PendingGroups ?? [];
    }

    private static TMessage GetLastMessage<TMessage>(RecordingDispatchProxy<IGameSession> sessionProxy) where TMessage : class
    {
        return sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(invocation => invocation.Arguments[0])
            .OfType<TMessage>()
            .Last();
    }

    private static GameTable<T> CreateGameTable<T>(params T[] entries) where T : class, new()
    {
        var table = (GameTable<T>)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(GameTable<T>));
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

    private sealed record TestCharacter(uint AccountId, ulong CharacterId, string Name, ICharacter Character)
    {
        public NetworkIdentity Identity { get; } = new()
        {
            RealmId = RealmId,
            Id      = CharacterId
        };
    }

    private sealed record TestAccount(
        uint AccountId,
        NetworkIdentity Identity,
        IAccount Account,
        IPlayer Player,
        AccountInventoryManager Manager,
        RecordingDispatchProxy<IGameSession> SessionProxy);

    private sealed record TestEnvironment(
        IServiceProvider Provider,
        TestAccount Source,
        TestAccount Target,
        TestAccount Third);
}
