using System.Net.Sockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using NexusForever.Shared.Game.Events;
using NexusForever.WorldServer.Network;
using NexusForever.Database.Auth;
using NexusForever.Database.Auth.Model;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Account.Costume;
using NexusForever.Game.Abstract.Account.Currency;
using NexusForever.Game.Abstract.Account.Entitlement;
using NexusForever.Game.Abstract.Account.Inventory;
using NexusForever.Game.Abstract.Account.Option;
using NexusForever.Game.Abstract.Account.Reward;
using NexusForever.Game.Abstract.Account.Unlock;
using NexusForever.Game.Abstract.RBAC;
using NexusForever.Game.Account.Reward;
using NexusForever.Game.Static;
using NexusForever.GameTable;
using NexusForever.Network.Session;
using NexusForever.GameTable.Model;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.Packet;
using NexusForever.Network.World.Message.Model;
using NexusForever.WorldServer.Network.Message.Handler.Reward;

namespace NexusForever.Game.Tests.Reward;

public class ClientRewardUpdateRequestClaimTests
{
    [Fact]
    public void Read_WithTrailingClaimFields_PopulatesClaimProperties()
    {
        byte[] packetData;
        using (var packetStream = new MemoryStream())
        {
            using (var writer = new GamePacketWriter(packetStream))
            {
                writer.Write(2u);
                writer.Write(15u);
                writer.Write((byte)RewardRotationScheduleBuilder.RewardTypeItem);
                writer.FlushBits();
            }

            packetData = packetStream.ToArray();
        }

        using var stream = new MemoryStream(packetData);
        using var reader = new GamePacketReader(stream);
        var request = new ClientRewardUpdateRequest();
        request.Read(reader);

        Assert.Equal(2u, request.RewardRotationIndex);
        Assert.Equal(15u, request.ClaimContentId);
        Assert.Equal(RewardRotationScheduleBuilder.RewardTypeItem, request.ClaimRewardType);
        Assert.True(request.HasClaimRequest);
    }

    [Fact]
    public void HandleMessage_WithClaimPayload_RecordsGrantAndSendsUpsert()
    {
        RewardRotationContentContextSources sources = CreateSources();
        SetEntries(sources.RewardRotationContent,
            new RewardRotationContentEntry { Id = 15u, ContentTypeEnum = 1u });
        SetEntries(sources.RewardRotationItem,
            new RewardRotationItemEntry { Id = 50u, Count = 1u, MinPlayerLevel = 1u });
        SetEntries(sources.RewardRotationEssence,
            new RewardRotationEssenceEntry { Id = 60u, MinPlayerLevel = 1u });
        SetEntries(sources.RewardRotationModifier,
            new RewardRotationModifierEntry { Id = 70u, Value = 1f, MinPlayerLevel = 1u });

        ServerRewardRotationScheduleArray schedule = RewardRotationScheduleBuilder.Build(sources, 1u, new List<uint> { 15u });

        const uint accountId = 42u;
        var account = new ClaimStubAccount(accountId);
        var grantManager = new AccountRewardRotationGrantManager(account, new AccountModel { Id = accountId });
        account.AttachGrantManager(grantManager);
        var session = new ClaimTestWorldSession(account);
        var handler = new ClientRewardUpdateRequestHandler(
            NullLogger<ClientRewardUpdateRequestHandler>.Instance,
            new FixedScheduleRefreshProvider(schedule));

        handler.HandleMessage(session, BuildClaimRequest(1u, 15u, RewardRotationScheduleBuilder.RewardTypeItem));

        Assert.Contains(session.EncryptedMessages, message => message is ServerRewardRotationEntryStateUpsert);
        ServerRewardRotationEntryStateArray entryState = session.EncryptedMessages
            .OfType<ServerRewardRotationEntryStateArray>()
            .Last();
        Assert.Single(entryState.Entries);
        Assert.Equal(15u, entryState.Entries[0].ContentId);
        Assert.Equal(50u, entryState.Entries[0].RewardTypeId);
    }

    private static ClientRewardUpdateRequest BuildClaimRequest(uint rewardRotationIndex, uint contentId, byte rewardType)
    {
        byte[] packetData;
        using (var packetStream = new MemoryStream())
        {
            using (var writer = new GamePacketWriter(packetStream))
            {
                writer.Write(rewardRotationIndex);
                writer.Write(contentId);
                writer.Write(rewardType);
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

    private static void SetEntries<T>(GameTable<T> table, params T[] entries) where T : class, new()
    {
        PropertyInfo property = typeof(GameTable<T>).GetProperty(nameof(GameTable<T>.Entries), BindingFlags.Instance | BindingFlags.Public);
        property?.SetValue(table, entries);
    }

    private static RewardRotationContentContextSources CreateSources()
    {
        return new RewardRotationContentContextSources
        {
            RewardRotationContent = CreateTable<RewardRotationContentEntry>(),
            RewardRotationItem = CreateTable<RewardRotationItemEntry>(),
            RewardRotationEssence = CreateTable<RewardRotationEssenceEntry>(),
            RewardRotationModifier = CreateTable<RewardRotationModifierEntry>(),
            WorldZone = CreateTable<WorldZoneEntry>(),
            World = CreateTable<WorldEntry>(),
            PublicEvent = CreateTable<PublicEventEntry>(),
            MatchTypeRewardRotationContent = CreateTable<MatchTypeRewardRotationContentEntry>()
        };
    }

    private static GameTable<T> CreateTable<T>() where T : class, new()
    {
        return (GameTable<T>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<T>));
    }

    private sealed class ClaimStubAccount(uint id) : IAccount
    {
        public uint Id => id;
        public string Email => "reward-claim@test.invalid";
        public IAccountRBACManager RbacManager => null;
        public IGenericUnlockManager GenericUnlockManager => null;
        public IAccountCurrencyManager CurrencyManager => null;
        public IAccountEntitlementManager EntitlementManager => null;
        public IAccountInventoryManager InventoryManager => null;
        public IAccountCostumeManager CostumeManager => null;
        public IRewardPropertyManager RewardPropertyManager { get; } = new NullRewardPropertyManager();
        public IAccountRewardRotationGrantManager RewardRotationGrantManager { get; private set; }
        public IAccountKeybindingManager KeybindingManager => null;
        public AccountTier AccountTier => default;
        public IGameSession Session => null;

        public void AttachGrantManager(IAccountRewardRotationGrantManager grantManager)
        {
            RewardRotationGrantManager = grantManager;
        }

        public void Initialise(AccountModel model, IGameSession session)
        {
        }

        public void Save(AuthContext context)
        {
        }
    }

    private sealed class ClaimTestWorldSession(IAccount account) : IWorldSession
    {
        public IAccount Account { get; } = account;
        public IPlayer Player { get; set; }
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

        public bool CanDispose() => false;

        public void ForceDisconnect()
        {
        }

        public void OnAccept(Socket newSocket)
        {
        }

        public void UpdateId(string id)
        {
        }

        public void Update(double lastTick)
        {
        }

        public void Initialise(NexusForever.Database.Auth.Model.AccountModel accountModel)
        {
        }

        public void ArmNextClientSpellEvidenceCapture(bool emitDiagnosticSpellBroadcasts = false)
        {
        }

        public bool TryConsumeNextClientSpellEvidenceCapture(out bool emitDiagnosticSpellBroadcasts)
        {
            emitDiagnosticSpellBroadcasts = false;
            return false;
        }

        public void ArmNextLootEvidenceCapture()
        {
        }

        public bool TryConsumeNextLootEvidenceCapture() => false;

        public void ArmNextAccountRuntimeEvidenceCapture()
        {
        }

        public bool TryConsumeNextAccountRuntimeEvidenceCapture() => false;

        public void ArmNextRewardRotationEvidenceCapture()
        {
        }

        public bool TryConsumeNextRewardRotationEvidenceCapture() => false;

        public void SetEncryptionKey(byte[] sessionKey)
        {
        }

        public List<NexusForever.Database.Character.Model.CharacterModel> Characters { get; } = [];
        public bool? IsQueued { get; set; }
        public bool CanProcessIncomingPackets { get; set; }
        public bool CanProcessOutgoingPackets { get; set; }
        public string Id { get; private set; } = "claim-test-session";
        public EventQueue Events { get; } = new();
        public SocketHeartbeat Heartbeat { get; } = new();
    }

    private sealed class NullRewardPropertyManager : IRewardPropertyManager
    {
        public void Initialise(IPlayer player)
        {
        }

        public void SendInitialPackets()
        {
        }

        public IRewardProperty GetRewardProperty(NexusForever.Game.Static.Entity.RewardPropertyType type) => null;

        public void UpdateRewardProperty(NexusForever.Game.Static.Entity.RewardPropertyType type, float value, uint data = 0)
        {
        }

        public void UpdateRewardProperty(NexusForever.GameTable.Model.RewardPropertyEntry entry, float value, uint data = 0)
        {
        }
    }

    private sealed class FixedScheduleRefreshProvider(ServerRewardRotationScheduleArray schedule) : IRewardRotationRefreshProvider
    {
        public RewardRotationRefresh Build(uint rewardRotationIndex)
        {
            return new RewardRotationRefresh(
                rewardRotationIndex,
                Array.Empty<ServerRewardRotationContentContext>(),
                schedule,
                new ServerRewardRotationEntryStateArray(),
                "test-claim-schedule");
        }
    }
}
