using System.Collections;
using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using NexusForever.Database;
using NexusForever.Database.Configuration.Model;
using NexusForever.Database.Auth;
using NexusForever.Database.Auth.Model;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Account.Costume;
using NexusForever.Game.Abstract.Account.Currency;
using NexusForever.Game.Abstract.Account.Entitlement;
using NexusForever.Game.Abstract.Account.Inventory;
using NexusForever.Game.Abstract.Account.Option;
using NexusForever.Game.Abstract.Account.Reward;
using NexusForever.Game.Abstract.Account.Unlock;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entitlement;
using NexusForever.Game.Abstract.RBAC;
using NexusForever.Game.Abstract.Storefront;
using NexusForever.Game.Static;
using NexusForever.Game.Static.Account;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.GenericUnlock;
using NexusForever.Game.Static.Reward;
using NexusForever.GameTable.Model;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.Packet;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.GenericUnlock;
using NexusForever.Network.World.Message.Model.Pregame;
using NexusForever.Network.World.Message.Model.Reward;
using NexusForever.Network.World.Message.Model.Shared;
using NexusForever.Shared.Game.Events;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Character;

namespace NexusForever.Game.Tests.Pregame;

public class CharacterListManagerTests
{
    [Theory]
    [InlineData(-1, false, uint.MaxValue)]
    [InlineData(0, true, 123u)]
    [InlineData(0, false, uint.MaxValue)]
    public void CharacterListIncludesLoadedCharacterWithoutALivePlayer(int active, bool hasCostume, uint expectedMask)
    {
        if (AssetManager.InventoryLocationCapacities == null)
            typeof(AssetManager).GetMethod("CacheInventoryBagCapacities", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(new AssetManager(), null);
        var session = new TestWorldSession();
        session.Account = new TestAccount(session);
        var model = new CharacterModel { Id = 75, Name = "LoadedPreview", ActiveCostumeIndex = (sbyte)active, Level = 1 };
        model.Appearance.Add(new CharacterAppearanceModel { Id = model.Id, Slot = 2, DisplayId = 321 });
        model.Bone.Add(new CharacterBoneModel { Id = model.Id, BoneIndex = 0, Bone = 1.25f });
        model.Stat.Add(new CharacterStatModel { Id = model.Id, Stat = (byte)Stat.Level, Value = 17 });
        if (hasCostume)
        {
            var costume = new CharacterCostumeModel { Id = model.Id, Index = 0, VisibilityMask = 123 };
            for (byte slot = 0; slot < 7; slot++)
                costume.CostumeItem.Add(new CharacterCostumeItemModel { Id = model.Id, Index = 0, Slot = slot });
            model.Costume.Add(costume);
        }
        var logger = new PreviewLogger();
        var manager = new CharacterListManager(logger,
            new UnusedDatabaseManager(), new TestRealmContext(), new NoOpGlobalStorefrontManager());

        manager.QueueCharacterListPackets(session, Task.FromResult(new List<CharacterModel> { model }));
        session.Events.Update(0d);

        Assert.Null(session.Player);
        Assert.Empty(logger.Errors);
        var row = Assert.Single(Assert.Single(session.EncryptedMessages.OfType<ServerCharacterList>()).Characters);
        Assert.Equal(model.Id, row.Id); Assert.Equal(model.Name, row.Name);
        Assert.Equal(expectedMask, row.GearMask); Assert.Equal(17u, row.Level);
        Assert.Equal((ushort)321, Assert.Single(row.Appearance).DisplayId);
        Assert.Equal(1.25f, Assert.Single(row.Bones));
        Assert.Empty(row.Gear);
        Assert.Equal((sbyte)active, model.ActiveCostumeIndex);
        Assert.Equal(hasCostume ? 1 : 0, model.Costume.Count);
        Assert.Same(model, Assert.Single(session.Characters));
    }

    private sealed class PreviewLogger : ILogger<CharacterListHandler>
    {
        internal List<Exception> Errors { get; } = [];
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        { if (exception != null) Errors.Add(exception); }
    }

    [Fact]
    public void QueueCharacterListPackets_SendsAccountStateBeforeCharacterLoadCompletes()
    {
        var session = new TestWorldSession();
        var account = new TestAccount(session);
        session.Account = account;

        var manager = new CharacterListManager(
            NullLogger<CharacterListHandler>.Instance,
            new UnusedDatabaseManager(),
            new TestRealmContext(),
            new NoOpGlobalStorefrontManager());
        var characterLoad = new TaskCompletionSource<List<CharacterModel>>();

        manager.QueueCharacterListPackets(session, characterLoad.Task);

        Assert.Equal(
        [
            typeof(ServerAccountCurrencySet),
            typeof(ServerGenericUnlockAccountList),
            typeof(ServerAccountEntitlements),
            typeof(ServerAccountTier)
        ],
        session.EncryptedMessages.Select(message => message.GetType()).ToArray());

        var entitlementsPacket = Assert.IsType<ServerAccountEntitlements>(session.EncryptedMessages[2]);
        Assert.Collection(entitlementsPacket.Entitlements,
            entitlement =>
            {
                Assert.Equal(EntitlementType.Signature, entitlement.Entitlement);
                Assert.Equal(2u, entitlement.Count);
            });
        Assert.Equal(AccountTier.Signature, Assert.IsType<ServerAccountTier>(session.EncryptedMessages[3]).Tier);
        Assert.True(session.HasSentPregameAccountPackets);
        Assert.False(session.HasSentCharacterListPackets);
        Assert.True(session.Events.PendingEvents);

        characterLoad.SetResult([]);
        session.Events.Update(0d);

        Assert.Equal(
        [
            typeof(ServerAccountCurrencySet),
            typeof(ServerGenericUnlockAccountList),
            typeof(ServerAccountEntitlements),
            typeof(ServerAccountTier),
            typeof(ServerMaxCharacterLevelAchieved),
            typeof(ServerCharacterList)
        ],
        session.EncryptedMessages.Select(message => message.GetType()).ToArray());
        Assert.Equal((byte)1, Assert.IsType<ServerMaxCharacterLevelAchieved>(session.EncryptedMessages[4]).Level);
        Assert.Empty(Assert.IsType<ServerCharacterList>(session.EncryptedMessages[5]).Characters);
        Assert.True(session.HasSentCharacterListPackets);
    }

    private sealed class UnusedDatabaseManager : IDatabaseManager
    {
        public void Initialise(IDatabaseConfig config)
        {
        }

        public void Migrate()
        {
        }

        public T GetDatabase<T>() where T : IDatabase
        {
            return default;
        }
    }

    private sealed class TestRealmContext : IRealmContext
    {
        public ushort RealmId => 17;
        public string RealmName => "Test Realm";
        public string Motd { get; set; } = string.Empty;

        public void Initialise()
        {
        }

        public ulong GetServerTime()
        {
            return 123456789ul;
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
        public string Id { get; private set; } = "character-list-test";
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

        public void OnAccept(System.Net.Sockets.Socket newSocket)
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

    private sealed class TestAccount : IAccount
    {
        public TestAccount(TestWorldSession session)
        {
            Session = session;
            CurrencyManager = new TestAccountCurrencyManager(session);
            GenericUnlockManager = new TestGenericUnlockManager(session);
            EntitlementManager = new TestAccountEntitlementManager(
            [
                new TestAccountEntitlement(EntitlementType.Signature, 2u)
            ]);
            RewardPropertyManager = new TestRewardPropertyManager();
        }

        public uint Id => 42u;
        public string Email => "character-list@test.invalid";
        public IAccountRBACManager RbacManager => null;
        public IGenericUnlockManager GenericUnlockManager { get; }
        public IAccountCurrencyManager CurrencyManager { get; }
        public IAccountEntitlementManager EntitlementManager { get; }
        public IAccountInventoryManager InventoryManager => null;
        public IAccountCostumeManager CostumeManager => null;
        public IRewardPropertyManager RewardPropertyManager { get; }
        public IAccountRewardRotationGrantManager RewardRotationGrantManager => null;
        public IAccountKeybindingManager KeybindingManager => null;
        public AccountTier AccountTier => AccountTier.Signature;
        public IGameSession Session { get; }

        public uint GetCREDDPendingOrderState() => 0u;

        public void Initialise(AccountModel model, IGameSession session)
        {
        }

        public void Save(AuthContext context)
        {
        }
    }

    private sealed class TestAccountCurrencyManager(TestWorldSession session) : IAccountCurrencyManager
    {
        public bool CanAfford(AccountCurrencyType currencyType, ulong amount)
        {
            return true;
        }

        public ulong GetCurrencyAmount(AccountCurrencyType currencyType)
        {
            return 0ul;
        }

        public void CurrencyAddAmount(AccountCurrencyType currencyType, ulong amount, ulong reason = 0)
        {
        }

        public void CurrencySubtractAmount(AccountCurrencyType currencyType, ulong amount, ulong reason = 0)
        {
        }

        public void SendCharacterListPacket()
        {
            session.EnqueueMessageEncrypted(new ServerAccountCurrencySet
            {
                AccountCurrencies = []
            });
        }

        public void SendInitialPackets()
        {
        }

        public void Save(AuthContext context)
        {
        }
    }

    private sealed class TestGenericUnlockManager(TestWorldSession session) : IGenericUnlockManager
    {
        public void Unlock(ushort genericUnlockEntryId)
        {
        }

        public void UnlockAll(GenericUnlockType type)
        {
        }

        public bool IsUnlocked(GenericUnlockType type, uint objectId)
        {
            return false;
        }

        public bool IsDyeUnlocked(uint dyeColourRampId)
        {
            return false;
        }

        public void SendUnlock(ushort genericUnlockEntryId)
        {
        }

        public void SendUnlockResult(GenericUnlockResult result)
        {
        }

        public void SendUnlockList()
        {
            session.EnqueueMessageEncrypted(new ServerGenericUnlockAccountList
            {
                GenericUnlockEntryIds = [11u]
            });
        }

        public void SendCharacterUnlockSync()
        {
        }

        public void Save(AuthContext context)
        {
        }

        public IEnumerator<IGenericUnlock> GetEnumerator()
        {
            return Enumerable.Empty<IGenericUnlock>().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    private sealed class TestAccountEntitlementManager(IEnumerable<IAccountEntitlement> entitlements) : IAccountEntitlementManager
    {
        private readonly List<IAccountEntitlement> entitlements = entitlements.ToList();

        public IAccountEntitlement GetEntitlement(EntitlementType type)
        {
            return entitlements.FirstOrDefault(entitlement => entitlement.Type == type);
        }

        public void UpdateEntitlement(EntitlementType type, int value)
        {
        }

        public void Save(AuthContext context)
        {
        }

        public IEnumerator<IAccountEntitlement> GetEnumerator()
        {
            return entitlements.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    private sealed class TestAccountEntitlement(EntitlementType type, uint amount) : IAccountEntitlement
    {
        public uint Amount { get; set; } = amount;
        public EntitlementEntry Entry => null;
        public EntitlementType Type { get; } = type;

        public ServerAccountEntitlement Build()
        {
            return new ServerAccountEntitlement
            {
                Entitlement = Type,
                Count = Amount
            };
        }

        public void Save(AuthContext context)
        {
        }
    }

    private sealed class TestRewardPropertyManager : IRewardPropertyManager
    {
        public void Initialise(IPlayer player)
        {
        }

        public void SendInitialPackets()
        {
        }

        public IRewardProperty GetRewardProperty(RewardPropertyType type)
        {
            return new TestRewardProperty();
        }

        public void UpdateRewardProperty(RewardPropertyType type, float value, uint data = 0)
        {
        }

        public void UpdateRewardProperty(RewardPropertyEntry entry, float value, uint data = 0)
        {
        }
    }

    private sealed class TestRewardProperty : IRewardProperty
    {
        public RewardPropertyEntry Entry => null;

        public IEnumerable<ServerPremiumRewards.RewardProperty> Build()
        {
            return [];
        }

        public void UpdateValue(uint data, float value)
        {
        }

        public void SetValue(uint data, float value)
        {
        }

        public float? GetValue(uint data)
        {
            return 2u;
        }
    }

    private sealed class NoOpGlobalStorefrontManager : IGlobalStorefrontManager
    {
        public void Initialise()
        {
        }

        public IOfferItem GetStoreOfferItem(uint offerId) => null;

        public void HandleCatalogRequest(IGameSession session, uint accountId)
        {
        }

        public void SendCatalogPackets(IGameSession session, uint accountId = 0)
        {
        }

        public void MarkAccountCatalogRequestedBeforeWorldLogin(uint accountId)
        {
        }

        public void SendBootstrapCatalogPacketsIfNeeded(IGameSession session, uint accountId)
        {
        }

        public void ClearCatalogDeliveryState(string sessionId, uint accountId = 0)
        {
        }
    }
}
