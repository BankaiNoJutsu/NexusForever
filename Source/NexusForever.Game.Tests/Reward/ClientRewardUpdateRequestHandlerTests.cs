using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using NexusForever.Database.Auth;
using NexusForever.Database.Auth.Model;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Account.Costume;
using NexusForever.Game.Abstract.Account.Currency;
using NexusForever.Game.Abstract.Account.Entitlement;
using NexusForever.Game.Abstract.Account.Inventory;
using NexusForever.Game.Abstract.Account.Option;
using NexusForever.Game.Abstract.Account.Reward;
using NexusForever.Game.Abstract.Account.Unlock;
using NexusForever.Game.Abstract.RBAC;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Storefront;
using NexusForever.Game.Tests.Storefront;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Account.Reward;
using NexusForever.Game.Static;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Reward;
using NexusForever.GameTable.Model;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.Packet;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Shared.Game.Events;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Fortune;
using NexusForever.WorldServer.Network.Message.Handler.Reward;

namespace NexusForever.Game.Tests.Reward;

public class ClientRewardUpdateRequestHandlerTests
{
    [Fact]
    public void HandleMessage_SupportedIndex_SkipsPlaceholderArrayPacketsAndLogsDiagnostics()
    {
        var rewardPropertyManager = new TestRewardPropertyManager();
        var session = new TestWorldSession(new TestAccount(rewardPropertyManager));
        var logger = new TestLogger<ClientRewardUpdateRequestHandler>();
        var handler = new ClientRewardUpdateRequestHandler(logger, new NoOpGlobalStorefrontManager(), EmptyRewardRotationRefreshProvider.Instance);

        handler.HandleMessage(session, BuildRequest(3u));

        Assert.Equal(1, rewardPropertyManager.SendInitialPacketsCallCount);
        Assert.Empty(session.EncryptedMessages);

        Assert.Contains(logger.Entries, entry =>
            entry.Level == LogLevel.Debug &&
            entry.Message.Contains("reward rotation index 3", StringComparison.Ordinal) &&
            entry.Message.Contains("schedule entries 0", StringComparison.Ordinal) &&
            entry.Message.Contains("entry-state entries 0", StringComparison.Ordinal) &&
            entry.Message.Contains("placeholder True", StringComparison.Ordinal));
        Assert.Contains(logger.Entries, entry =>
            entry.Level == LogLevel.Information &&
            entry.Message.Contains("empty placeholder", StringComparison.Ordinal) &&
            entry.Message.Contains("reward rotation index 3", StringComparison.Ordinal));
        Assert.Contains(logger.Entries, entry =>
            entry.Level == LogLevel.Information &&
            entry.Message.Contains("skipping empty ServerRewardRotationScheduleArray", StringComparison.Ordinal) &&
            entry.Message.Contains("reward rotation index 3", StringComparison.Ordinal));
        Assert.Contains(logger.Entries, entry =>
            entry.Level == LogLevel.Information &&
            entry.Message.Contains("skipping empty ServerRewardRotationEntryStateArray", StringComparison.Ordinal) &&
            entry.Message.Contains("reward rotation index 3", StringComparison.Ordinal));
    }

    [Fact]
    public void HandleMessage_WithNonEmptyRefresh_SendsRotationArrayPackets()
    {
        var rewardPropertyManager = new TestRewardPropertyManager();
        var session = new TestWorldSession(new TestAccount(rewardPropertyManager));
        var logger = new TestLogger<ClientRewardUpdateRequestHandler>();
        var handler = new ClientRewardUpdateRequestHandler(logger, new NoOpGlobalStorefrontManager(), NonEmptyRewardRotationRefreshProvider.Instance);

        handler.HandleMessage(session, BuildRequest(2u));

        Assert.Equal(1, rewardPropertyManager.SendInitialPacketsCallCount);
        Assert.Collection(session.EncryptedMessages,
            message => Assert.IsType<ServerRewardRotationScheduleArray>(message),
            message => Assert.IsType<ServerRewardRotationEntryStateArray>(message));
        Assert.DoesNotContain(logger.Entries, entry => entry.Message.Contains("skipping empty", StringComparison.Ordinal));
    }

    [Fact]
    public void HandleMessage_WithContentContextOnlyRefresh_SkipsEmptyRotationArrays()
    {
        var rewardPropertyManager = new TestRewardPropertyManager();
        var session = new TestWorldSession(new TestAccount(rewardPropertyManager));
        var logger = new TestLogger<ClientRewardUpdateRequestHandler>();
        var handler = new ClientRewardUpdateRequestHandler(logger, new NoOpGlobalStorefrontManager(), ContentContextOnlyRewardRotationRefreshProvider.Instance);

        handler.HandleMessage(session, BuildRequest(2u));

        Assert.Equal(1, rewardPropertyManager.SendInitialPacketsCallCount);
        Assert.Collection(session.EncryptedMessages,
            message => Assert.IsType<ServerRewardRotationContentContext>(message));
        Assert.Contains(logger.Entries, entry =>
            entry.Level == LogLevel.Information &&
            entry.Message.Contains("skipping empty ServerRewardRotationScheduleArray", StringComparison.Ordinal) &&
            entry.Message.Contains("reward rotation index 2", StringComparison.Ordinal));
        Assert.Contains(logger.Entries, entry =>
            entry.Level == LogLevel.Information &&
            entry.Message.Contains("skipping empty ServerRewardRotationEntryStateArray", StringComparison.Ordinal) &&
            entry.Message.Contains("reward rotation index 2", StringComparison.Ordinal));
    }

    [Fact]
    public void HandleMessage_WithPlaceholderIndexZeroAndPlayer_SendsCatalogBeforeRotationPackets()
    {
        var rewardPropertyManager = new TestRewardPropertyManager();
        var session = new TestWorldSession(new TestAccount(rewardPropertyManager));
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Guid), 600u);
        session.Player = player;
        List<string> calls = [];

        IGlobalStorefrontManager storefront = RecordingDispatchProxy<IGlobalStorefrontManager>.Create(out RecordingDispatchProxy<IGlobalStorefrontManager> storefrontProxy);
        storefrontProxy.SetMethodHandler(nameof(IGlobalStorefrontManager.HandleCatalogRequest), _ =>
        {
            calls.Add("catalog");
            return null;
        });
        IFortuneSessionManager fortuneSessionManager = RecordingDispatchProxy<IFortuneSessionManager>.Create(out RecordingDispatchProxy<IFortuneSessionManager> fortuneSessionProxy);
        fortuneSessionProxy.SetMethodHandler(nameof(IFortuneSessionManager.SendStatus), _ =>
        {
            calls.Add("fortune-status");
            return null;
        });
        var handler = new ClientRewardUpdateRequestHandler(
            new TestLogger<ClientRewardUpdateRequestHandler>(),
            storefront,
            EmptyRewardRotationRefreshProvider.Instance,
            fortuneSessionManager);

        handler.HandleMessage(session, BuildRequest(0u));

        Assert.Single(storefrontProxy.GetInvocations(nameof(IGlobalStorefrontManager.HandleCatalogRequest)));
        Assert.Single(fortuneSessionProxy.GetInvocations(nameof(IFortuneSessionManager.SendStatus)));
        Assert.Equal(new[] { "catalog", "fortune-status" }, calls);
    }

    [Fact]
    public void HandleMessage_WithNonZeroIndex_DoesNotSendFortuneStatus()
    {
        var rewardPropertyManager = new TestRewardPropertyManager();
        var session = new TestWorldSession(new TestAccount(rewardPropertyManager));
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Guid), 600u);
        session.Player = player;

        IFortuneSessionManager fortuneSessionManager = RecordingDispatchProxy<IFortuneSessionManager>.Create(out RecordingDispatchProxy<IFortuneSessionManager> fortuneSessionProxy);
        var handler = new ClientRewardUpdateRequestHandler(
            new TestLogger<ClientRewardUpdateRequestHandler>(),
            new NoOpGlobalStorefrontManager(),
            EmptyRewardRotationRefreshProvider.Instance,
            fortuneSessionManager);

        handler.HandleMessage(session, BuildRequest(3u));

        Assert.Empty(fortuneSessionProxy.GetInvocations(nameof(IFortuneSessionManager.SendStatus)));
    }

    [Fact]
    public void HandleMessage_WithPlaceholderIndexZero_SendsEmptyRotationArrays()
    {
        var rewardPropertyManager = new TestRewardPropertyManager();
        var session = new TestWorldSession(new TestAccount(rewardPropertyManager));
        var logger = new TestLogger<ClientRewardUpdateRequestHandler>();
        var handler = new ClientRewardUpdateRequestHandler(logger, new NoOpGlobalStorefrontManager(), EmptyRewardRotationRefreshProvider.Instance);

        handler.HandleMessage(session, BuildRequest(0u));

        Assert.Equal(1, rewardPropertyManager.SendInitialPacketsCallCount);
        Assert.Collection(session.EncryptedMessages,
            message =>
            {
                var schedule = Assert.IsType<ServerRewardRotationScheduleArray>(message);
                Assert.Empty(schedule.Entries);
            },
            message =>
            {
                var entryState = Assert.IsType<ServerRewardRotationEntryStateArray>(message);
                Assert.Empty(entryState.Entries);
            });
        Assert.DoesNotContain(logger.Entries, entry => entry.Message.Contains("skipping empty ServerRewardRotationScheduleArray", StringComparison.Ordinal));
        Assert.DoesNotContain(logger.Entries, entry => entry.Message.Contains("skipping empty ServerRewardRotationEntryStateArray", StringComparison.Ordinal));
        Assert.Contains(logger.Entries, entry =>
            entry.Level == LogLevel.Information &&
            entry.Message.Contains("sending empty ServerRewardRotationScheduleArray", StringComparison.Ordinal) &&
            entry.Message.Contains("reward rotation index 0", StringComparison.Ordinal));
        Assert.Contains(logger.Entries, entry =>
            entry.Level == LogLevel.Information &&
            entry.Message.Contains("sending empty ServerRewardRotationEntryStateArray", StringComparison.Ordinal) &&
            entry.Message.Contains("reward rotation index 0", StringComparison.Ordinal));
    }

    [Fact]
    public void HandleMessage_UnsupportedIndex_LogsSupportedRangeAndSkipsRotationPackets()
    {
        var rewardPropertyManager = new TestRewardPropertyManager();
        var session = new TestWorldSession(new TestAccount(rewardPropertyManager));
        var logger = new TestLogger<ClientRewardUpdateRequestHandler>();
        var handler = new ClientRewardUpdateRequestHandler(logger, new NoOpGlobalStorefrontManager(), EmptyRewardRotationRefreshProvider.Instance);

        handler.HandleMessage(session, BuildRequest(RewardRotationRefreshBuilder.ContentTypeCount));

        Assert.Equal(1, rewardPropertyManager.SendInitialPacketsCallCount);
        Assert.Empty(session.EncryptedMessages);
        Assert.Contains(logger.Entries, entry =>
            entry.Level == LogLevel.Warning &&
            entry.Message.Contains("supported range is 0-6", StringComparison.Ordinal) &&
            entry.Message.Contains("reward rotation index 7", StringComparison.Ordinal));
    }

    private static ClientRewardUpdateRequest BuildRequest(uint rewardRotationIndex)
    {
        byte[] packetData;
        using (var packetStream = new MemoryStream())
        {
            using (var writer = new GamePacketWriter(packetStream))
            {
                writer.Write(rewardRotationIndex);
                writer.FlushBits();
            }

            packetData = packetStream.ToArray();
        }

        using var stream = new MemoryStream(packetData);
        using var reader = new GamePacketReader(stream);
        var request = new ClientRewardUpdateRequest();
        request.Read(reader);
        return request;
    }

    private sealed class NonEmptyRewardRotationRefreshProvider : IRewardRotationRefreshProvider
    {
        public static NonEmptyRewardRotationRefreshProvider Instance { get; } = new();

        private NonEmptyRewardRotationRefreshProvider()
        {
        }

        public RewardRotationRefresh Build(uint rewardRotationIndex)
        {
            return new RewardRotationRefresh(
                rewardRotationIndex,
                new ServerRewardRotationScheduleArray
                {
                    Entries =
                    {
                        new ServerRewardRotationScheduleArray.ScheduleRow
                        {
                            ContentId = 10u,
                            RewardKeyId = 20u,
                            Duration = 1f,
                            RewardType = RewardRotationScheduleBuilder.RewardTypeItem,
                            Value = 30u
                        }
                    }
                },
                new ServerRewardRotationEntryStateArray
                {
                    Entries =
                    {
                        new ServerRewardRotationEntryStateArray.EntryStateRow
                        {
                            TypeId = 1u,
                            ContentId = 10u,
                            RewardTypeId = 20u,
                            State = RewardRotationScheduleBuilder.RewardTypeItem,
                            Value = 1u
                        }
                    }
                },
                "test-non-empty-refresh");
        }
    }

    private sealed class ContentContextOnlyRewardRotationRefreshProvider : IRewardRotationRefreshProvider
    {
        public static ContentContextOnlyRewardRotationRefreshProvider Instance { get; } = new();

        private ContentContextOnlyRewardRotationRefreshProvider()
        {
        }

        public RewardRotationRefresh Build(uint rewardRotationIndex)
        {
            return new RewardRotationRefresh(
                rewardRotationIndex,
                new ServerRewardRotationContentContext
                {
                    RewardRotationIndex = rewardRotationIndex,
                    UInt0 = 1u,
                    UInt1 = 2u,
                    UInt3 = 3u,
                    ContentIds = { 10u },
                    Flag = false
                },
                new ServerRewardRotationScheduleArray(),
                new ServerRewardRotationEntryStateArray(),
                "test-content-context-only-refresh");
        }
    }

    private sealed class TestWorldSession(IAccount account) : IWorldSession
    {
        public IAccount Account { get; } = account;
        public IPlayer Player { get; set; }
        public bool HasSentCharacterListPackets { get; set; }
        public bool HasSentPregameAccountPackets { get; set; }
        public List<CharacterModel> Characters { get; } = [];
        public bool? IsQueued { get; set; }
        public bool CanProcessIncomingPackets { get; set; }
        public bool CanProcessOutgoingPackets { get; set; }
        public string Id { get; private set; } = "test-world-session";
        public EventQueue Events { get; } = new();
        public SocketHeartbeat Heartbeat { get; } = new();
        public List<IWritable> EncryptedMessages { get; } = [];
        private bool nextClientSpellEvidenceCapture;
        private bool nextClientSpellEmitDiagnosticBroadcasts;
        private bool nextLootEvidenceCapture;
        private bool nextAccountRuntimeEvidenceCapture;
        private bool nextRewardRotationEvidenceCapture;

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

        public void Initialise(AccountModel accountModel)
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

    private sealed class TestAccount(IRewardPropertyManager rewardPropertyManager) : IAccount
    {
        public uint Id => 42u;
        public string Email => "reward-rotation@test.invalid";
        public IAccountRBACManager RbacManager => null;
        public IGenericUnlockManager GenericUnlockManager => null;
        public IAccountCurrencyManager CurrencyManager => null;
        public IAccountEntitlementManager EntitlementManager => null;
        public IAccountInventoryManager InventoryManager => null;
        public IAccountCostumeManager CostumeManager => null;
        public IRewardPropertyManager RewardPropertyManager { get; } = rewardPropertyManager;
        public IAccountRewardRotationGrantManager RewardRotationGrantManager => null;
        public IAccountKeybindingManager KeybindingManager => null;
        public AccountTier AccountTier => default;
        public IGameSession Session => null;

        public uint GetCREDDPendingOrderState() => 0u;

        public void Initialise(AccountModel model, IGameSession session)
        {
        }

        public void Save(AuthContext context)
        {
        }
    }

    private sealed class TestRewardPropertyManager : IRewardPropertyManager
    {
        public int SendInitialPacketsCallCount { get; private set; }

        public void Initialise(IPlayer player)
        {
        }

        public void SendInitialPackets()
        {
            SendInitialPacketsCallCount++;
        }

        public IRewardProperty GetRewardProperty(RewardPropertyType type)
        {
            return null;
        }

        public void UpdateRewardProperty(RewardPropertyType type, float value, uint data = 0)
        {
        }

        public void UpdateRewardProperty(RewardPropertyEntry entry, float value, uint data = 0)
        {
        }
    }

    private sealed class TestLogger<T> : ILogger<T>
    {
        public List<LogEntry> Entries { get; } = [];

        public IDisposable BeginScope<TState>(TState state)
        {
            return NullScope.Instance;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            Entries.Add(new LogEntry(logLevel, formatter(state, exception)));
        }
    }

    private sealed record LogEntry(LogLevel Level, string Message);

    private sealed class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new();

        public void Dispose()
        {
        }
    }
}
