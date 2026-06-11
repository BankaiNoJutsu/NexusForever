using System.Collections.Immutable;
using System.Net.Sockets;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NexusForever.Database.Auth.Model;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Account.Inventory;
using NexusForever.Game.Abstract.Character;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Account.Inventory;
using NexusForever.Game.Character;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Account;
using NexusForever.Game.Static;
using NexusForever.Game.Static.RBAC;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Configuration.Model;
using NexusForever.GameTable.Model;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.Packet;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Shared.Game.Events;
using NexusForever.WorldServer.Command.Context;
using NexusForever.WorldServer.Command.Handler;
using NexusForever.WorldServer.Network;
using GameIdentity = NexusForever.Game.Abstract.Identity;
using NetworkIdentity = NexusForever.Network.World.Message.Model.Shared.Identity;

namespace NexusForever.Game.Tests.Account.Inventory;

public class AccountRuntimeEvidenceTests
{
    private const ushort RealmId = 1;
    private const uint AccountItemId = 77u;

    [Fact]
    public void GiftPendingItemGroupToCharacter_OfflineTarget_ArmedCaptureExportsBlockedTransferArtifact()
    {
        using var output = new AccountEvidenceDirectoryScope();

        TestEnvironment environment = CreateEnvironmentWithOnlineAccounts([1001u],
            CreateCharacter(accountId: 1001u, characterId: 101ul, name: "Source"),
            CreateCharacter(accountId: 2002u, characterId: 202ul, name: "OfflineTarget"));

        environment.Source.Session.ArmNextAccountRuntimeEvidenceCapture();
        string group = environment.Source.Manager.AddPendingItemGroup([AccountItemId], notify: false);

        AccountOperationResult result = environment.Source.Manager.GiftPendingItemGroupToCharacter(
            environment.Source.Player,
            group,
            environment.Target.Identity);

        Assert.Equal(AccountOperationResult.Ok, result);

        InMemoryAccountPendingItemRepository.StoredPendingItem stored = Assert.Single(
            InMemoryAccountPendingItemRepository.GetPendingItems(environment.Target.AccountId));
        Assert.Equal(group, stored.GroupName);
        Assert.Equal(AccountItemId, stored.AccountItemId);
        Assert.Equal(1001u, stored.SenderAccountId);
        if (Directory.Exists(output.DirectoryPath))
            Assert.Empty(Directory.GetFiles(output.DirectoryPath, "*.json"));
    }

    [Fact]
    public void HandleAccountCaptureNext_ArmsWorldSessionAndReportsOutputHint()
    {
        var category = new AccountCommandCategory();
        var session = new TestWorldSession();

        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);

        var context = new TestCommandContext(
            player,
            null,
            [Permission.Account]);

        category.HandleAccountCaptureNext(context);

        Assert.True(session.TryConsumeNextAccountRuntimeEvidenceCapture());
        Assert.Single(context.Messages);
        Assert.Contains("account-evidence", context.Messages[0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void HandleAccountCouponBlockers_ExportsCouponReport()
    {
        using var output = new AccountEvidenceDirectoryScope();

        var category = new AccountCommandCategory();
        var session = new TestWorldSession();
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IAccount account = RecordingDispatchProxy<IAccount>.Create(out var accountProxy);

        accountProxy.SetProperty(nameof(IAccount.Id), 4004u);
        playerProxy.SetProperty(nameof(IPlayer.Account), account);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), 404ul);
        playerProxy.SetProperty("Guid", 4040u);

        var context = new TestCommandContext(
            player,
            null,
            [Permission.Account]);

        category.HandleAccountCouponBlockers(context);

        string artifactPath = Assert.Single(Directory.GetFiles(output.DirectoryPath, "*.json"));
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(artifactPath));
        JsonElement root = document.RootElement;

        Assert.Equal("coupon-blocked-report", root.GetProperty("Status").GetString());
        Assert.Equal("RedeemCoupon", root.GetProperty("Operation").GetString());
        Assert.Equal("InvalidCoupon", root.GetProperty("Result").GetString());
        Assert.Contains("opcode", string.Join(" ", root.GetProperty("Blockers").EnumerateArray().Select(element => element.GetString())), StringComparison.OrdinalIgnoreCase);
        Assert.Single(context.Messages);
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

        var playerManager = new PlayerManager(Microsoft.Extensions.Logging.Abstractions.NullLogger<PlayerManager>.Instance, characterManager);
        InMemoryAccountPendingItemRepository.Clear(characters[0].AccountId);
        if (characters.Length > 1)
            InMemoryAccountPendingItemRepository.Clear(characters[1].AccountId);

        IServiceProvider provider = new ServiceCollection()
            .AddSingleton(gameTableManager)
            .AddSingleton(realmContext)
            .AddSingleton(characterManager)
            .AddSingleton(playerManager)
            .AddSingleton<IAccountPendingItemRepository, InMemoryAccountPendingItemRepository>()
            .AddSingleton<IPendingAccountItemGroupDelivery, RetailPendingAccountItemGroupDelivery>()
            .AddSingleton(typeof(Microsoft.Extensions.Logging.ILogger<>), typeof(Microsoft.Extensions.Logging.Abstractions.NullLogger<>))
            .BuildServiceProvider();

        IPendingAccountItemGroupDelivery pendingDelivery = provider.GetRequiredService<IPendingAccountItemGroupDelivery>();
        TestAccount source = CreateAccount(characters[0].AccountId, characters[0].Identity, pendingDelivery, characterManager, gameTableManager);
        TestAccount target = characters.Length > 1 ? CreateAccount(characters[1].AccountId, characters[1].Identity, pendingDelivery, characterManager, gameTableManager) : null;

        if (onlineAccountIds.Contains(source.AccountId))
            playerManager.AddPlayer(source.Player);
        if (target != null && onlineAccountIds.Contains(target.AccountId))
            playerManager.AddPlayer(target.Player);

        return new TestEnvironment(provider, source, target);
    }

    private static TestAccount CreateAccount(
        uint accountId,
        NetworkIdentity identity,
        IPendingAccountItemGroupDelivery pendingDelivery,
        ICharacterManager characterManager,
        IGameTableManager gameTableManager)
    {
        var session = new TestWorldSession();
        IAccount account = RecordingDispatchProxy<IAccount>.Create(out var accountProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);

        accountProxy.SetProperty(nameof(IAccount.Id), accountId);
        accountProxy.SetProperty(nameof(IAccount.Session), session);

        playerProxy.SetProperty(nameof(IPlayer.Account), account);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);
        playerProxy.SetProperty(nameof(IPlayer.Identity), new GameIdentity
        {
            RealmId = identity.RealmId,
            Id      = identity.Id
        });
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), identity.Id);
        playerProxy.SetProperty("Guid", (uint)identity.Id);

        var manager = new AccountInventoryManager(
            account,
            new AccountModel
        {
            AccountInventory    = [],
            AccountItemCooldown = []
        },
            pendingDelivery,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<AccountInventoryManager>.Instance,
            characterManager,
            gameTableManager: gameTableManager);
        accountProxy.SetProperty(nameof(IAccount.InventoryManager), manager);
        session.Account = account;
        session.Player = player;

        return new TestAccount(accountId, identity, account, player, manager, session);
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

    private sealed class AccountEvidenceDirectoryScope : IDisposable
    {
        private const string EnvironmentVariableName = "NEXUSFOREVER_ACCOUNT_EVIDENCE_DIR";
        private static readonly object syncRoot = new();

        public string DirectoryPath { get; }

        public AccountEvidenceDirectoryScope()
        {
            Monitor.Enter(syncRoot);
            DirectoryPath = Path.Combine(AppContext.BaseDirectory, "account-evidence-tests", Guid.NewGuid().ToString("N"));
            Environment.SetEnvironmentVariable(EnvironmentVariableName, DirectoryPath);
        }

        public void Dispose()
        {
            try
            {
                Environment.SetEnvironmentVariable(EnvironmentVariableName, null);
                if (Directory.Exists(DirectoryPath))
                    Directory.Delete(DirectoryPath, recursive: true);
            }
            finally
            {
                Monitor.Exit(syncRoot);
            }
        }
    }

    private sealed class TestCommandContext(
        IWorldEntity invoker,
        IWorldEntity target,
        ImmutableHashSet<Permission> permissions) : ICommandContext
    {
        public List<string> Messages { get; } = [];
        public List<string> Errors { get; } = [];

        public IWorldEntity Invoker { get; } = invoker;
        public IWorldEntity Target { get; } = target;
        public Language Language => Language.English;
        public ImmutableHashSet<Permission> Permissions { get; } = permissions;

        public void SendMessage(string message)
        {
            Messages.Add(message);
        }

        public void SendError(string message)
        {
            Errors.Add(message);
        }

        public T GetTargetOrInvoker<T>() where T : IWorldEntity
        {
            return (T)(Target ?? Invoker);
        }
    }

    private sealed class TestWorldSession : IWorldSession
    {
        private bool nextClientSpellEvidenceCapture;
        private bool nextClientSpellEmitDiagnosticBroadcasts;
        private bool nextLootEvidenceCapture;
        private bool nextAccountRuntimeEvidenceCapture;
        private bool nextRewardRotationEvidenceCapture;

        public IAccount Account { get; set; }
        public IPlayer Player { get; set; }
    public bool HasSentCharacterListPackets { get; set; }
    public bool HasSentPregameAccountPackets { get; set; }
        public List<CharacterModel> Characters { get; } = [];
        public bool? IsQueued { get; set; }
        public bool CanProcessIncomingPackets { get; set; }
        public bool CanProcessOutgoingPackets { get; set; }
        public string Id { get; private set; } = "account-runtime-evidence-test";
        public EventQueue Events { get; } = new();
        public SocketHeartbeat Heartbeat { get; } = new();
        public List<IWritable> EncryptedMessages { get; } = [];

        public void EnqueueMessage(IWritable message)
        {
        }

        public void EnqueueMessageEncrypted(IWritable message)
        {
            EncryptedMessages.Add(message);
        }

        public void EnqueueMessageEncrypted(uint opcode, string hex)
        {
        }

        public void HandlePacket(ClientGamePacket packet)
        {
        }

        public void FlushPackets()
        {
        }

        public bool CanDispose()
        {
            return false;
        }

        public void ForceDisconnect()
        {
        }

        public void OnAccept(Socket newSocket)
        {
        }

        public void UpdateId(string id)
        {
            Id = id;
        }

        public void Update(double lastTick)
        {
        }

        public void Initialise(AccountModel account)
        {
        }

        public void ArmNextClientSpellEvidenceCapture(bool emitDiagnosticSpellBroadcasts = false)
        {
            nextClientSpellEvidenceCapture = true;
            nextClientSpellEmitDiagnosticBroadcasts = emitDiagnosticSpellBroadcasts;
        }

        public bool TryConsumeNextClientSpellEvidenceCapture(out bool emitDiagnosticSpellBroadcasts)
        {
            emitDiagnosticSpellBroadcasts = nextClientSpellEmitDiagnosticBroadcasts;
            bool armed = nextClientSpellEvidenceCapture;
            nextClientSpellEvidenceCapture = false;
            nextClientSpellEmitDiagnosticBroadcasts = false;
            return armed;
        }

        public void ArmNextLootEvidenceCapture()
        {
            nextLootEvidenceCapture = true;
        }

        public bool TryConsumeNextLootEvidenceCapture()
        {
            bool armed = nextLootEvidenceCapture;
            nextLootEvidenceCapture = false;
            return armed;
        }

        public void ArmNextAccountRuntimeEvidenceCapture()
        {
            nextAccountRuntimeEvidenceCapture = true;
        }

        public bool TryConsumeNextAccountRuntimeEvidenceCapture()
        {
            bool armed = nextAccountRuntimeEvidenceCapture;
            nextAccountRuntimeEvidenceCapture = false;
            return armed;
        }

        public void ArmNextRewardRotationEvidenceCapture()
        {
            nextRewardRotationEvidenceCapture = true;
        }

        public bool TryConsumeNextRewardRotationEvidenceCapture()
        {
            bool armed = nextRewardRotationEvidenceCapture;
            nextRewardRotationEvidenceCapture = false;
            return armed;
        }

        public void SetEncryptionKey(byte[] sessionKey)
        {
        }
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
        TestWorldSession Session);

    private sealed record TestEnvironment(
        IServiceProvider Provider,
        TestAccount Source,
        TestAccount Target);
}
