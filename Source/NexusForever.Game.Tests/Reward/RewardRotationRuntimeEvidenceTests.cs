using System.Collections.Immutable;
using System.Net.Sockets;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
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
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.RBAC;
using NexusForever.Game.Account.Reward;
using NexusForever.Game.Static;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.RBAC;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.Packet;
using NexusForever.Network.World.Message.Model;
using NexusForever.Shared.Game.Events;
using NexusForever.WorldServer.Command.Context;
using NexusForever.WorldServer.Command.Handler;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Reward;

namespace NexusForever.Game.Tests.Reward;

public class RewardRotationRuntimeEvidenceTests
{
    [Fact]
    public void HandleMessage_ArmedCapture_ExportsPlaceholderArtifact()
    {
        using var output = new RewardEvidenceDirectoryScope();

        var rewardPropertyManager = new TestRewardPropertyManager();
        var session = CreateSession(rewardPropertyManager, accountId: 4404u, characterId: 404ul, playerGuid: 4040u);
        session.ArmNextRewardRotationEvidenceCapture();

        var handler = new ClientRewardUpdateRequestHandler(
            NullLogger<ClientRewardUpdateRequestHandler>.Instance,
            EmptyRewardRotationRefreshProvider.Instance);

        handler.HandleMessage(session, BuildRequest(3u));

        string artifactPath = Assert.Single(Directory.GetFiles(output.DirectoryPath, "*.json"));
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(artifactPath));
        JsonElement root = document.RootElement;

        Assert.True(root.GetProperty("RequestSupported").GetBoolean());
        Assert.Equal(3u, root.GetProperty("RequestedRewardRotationIndex").GetUInt32());
        Assert.True(root.GetProperty("PlaceholderResponse").GetBoolean());
        Assert.Equal("empty-placeholder-provider", root.GetProperty("ResponseSource").GetString());
        Assert.Equal(0, root.GetProperty("ScheduleEntryCount").GetInt32());
        Assert.Equal(0, root.GetProperty("EntryStateCount").GetInt32());
        Assert.Empty(root.GetProperty("ScheduleRows").EnumerateArray().ToArray());
        Assert.Empty(root.GetProperty("EntryStateRows").EnumerateArray().ToArray());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("ScheduleArrayPacket").ValueKind);
        Assert.Equal(JsonValueKind.Null, root.GetProperty("EntryStateArrayPacket").ValueKind);
        Assert.Equal(3, root.GetProperty("EntryStateDeltaPackets").GetArrayLength());
        Assert.Empty(session.EncryptedMessages);
        Assert.Equal(1, rewardPropertyManager.SendInitialPacketsCallCount);
    }

    [Fact]
    public void HandleMessage_UnsupportedIndex_ArmedCapture_ExportsUnsupportedArtifact()
    {
        using var output = new RewardEvidenceDirectoryScope();

        var rewardPropertyManager = new TestRewardPropertyManager();
        var session = CreateSession(rewardPropertyManager, accountId: 5505u, characterId: 505ul, playerGuid: 5050u);
        session.ArmNextRewardRotationEvidenceCapture();

        var handler = new ClientRewardUpdateRequestHandler(
            NullLogger<ClientRewardUpdateRequestHandler>.Instance,
            EmptyRewardRotationRefreshProvider.Instance);

        handler.HandleMessage(session, BuildRequest(RewardRotationRefreshBuilder.ContentTypeCount));

        string artifactPath = Assert.Single(Directory.GetFiles(output.DirectoryPath, "*.json"));
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(artifactPath));
        JsonElement root = document.RootElement;

        Assert.False(root.GetProperty("RequestSupported").GetBoolean());
        Assert.Equal(RewardRotationRefreshBuilder.ContentTypeCount, root.GetProperty("RequestedRewardRotationIndex").GetUInt32());
        Assert.False(root.GetProperty("PlaceholderResponse").GetBoolean());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("ScheduleArrayPacket").ValueKind);
        Assert.Equal(JsonValueKind.Null, root.GetProperty("EntryStateArrayPacket").ValueKind);
        Assert.Contains("Unsupported request indices", string.Join(" ", root.GetProperty("Notes").EnumerateArray().Select(element => element.GetString())), StringComparison.OrdinalIgnoreCase);
        Assert.Empty(session.EncryptedMessages);
        Assert.Equal(1, rewardPropertyManager.SendInitialPacketsCallCount);
    }

    [Fact]
    public void HandleRewardCaptureNext_ArmsSessionAndReportsOutputHint()
    {
        var category = new RewardCommandCategory();
        var rewardPropertyManager = new TestRewardPropertyManager();
        var session = CreateSession(rewardPropertyManager, accountId: 6606u, characterId: 606ul, playerGuid: 6060u);
        var context = new TestCommandContext(session.Player, null, [Permission.Account]);

        category.HandleRewardCaptureNext(context);

        Assert.True(session.TryConsumeNextRewardRotationEvidenceCapture());
        Assert.Single(context.Messages);
        Assert.Contains("reward-evidence", context.Messages[0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void HandleRewardReport_ExportsManualReport()
    {
        using var output = new RewardEvidenceDirectoryScope();

        var category = new RewardCommandCategory();
        var rewardPropertyManager = new TestRewardPropertyManager();
        var session = CreateSession(rewardPropertyManager, accountId: 7707u, characterId: 707ul, playerGuid: 7070u);
        var context = new TestCommandContext(session.Player, null, [Permission.Account]);

        category.HandleRewardReport(context, 2u);

        string artifactPath = Assert.Single(Directory.GetFiles(output.DirectoryPath, "*.json"));
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(artifactPath));
        JsonElement root = document.RootElement;

        Assert.Equal("manual-command", root.GetProperty("CaptureSource").GetString());
        Assert.Equal(2u, root.GetProperty("RequestedRewardRotationIndex").GetUInt32());
        Assert.True(root.GetProperty("RequestSupported").GetBoolean());
        Assert.True(root.GetProperty("PlaceholderResponse").GetBoolean());
        Assert.Single(context.Messages);
        Assert.Contains("supported placeholder response recorded", context.Messages[0], StringComparison.OrdinalIgnoreCase);
    }

    private static TestWorldSession CreateSession(TestRewardPropertyManager rewardPropertyManager, uint accountId, ulong characterId, uint playerGuid)
    {
        var session = new TestWorldSession();
        IAccount account = RecordingDispatchProxy<IAccount>.Create(out var accountProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);

        accountProxy.SetProperty(nameof(IAccount.Id), accountId);
        accountProxy.SetProperty(nameof(IAccount.RewardPropertyManager), rewardPropertyManager);
        accountProxy.SetProperty(nameof(IAccount.Session), session);

        playerProxy.SetProperty(nameof(IPlayer.Account), account);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), characterId);
        playerProxy.SetProperty("Guid", playerGuid);

        session.Account = account;
        session.Player = player;
        return session;
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

    private sealed class RewardEvidenceDirectoryScope : IDisposable
    {
        private const string EnvironmentVariableName = "NEXUSFOREVER_REWARD_EVIDENCE_DIR";
        private static readonly object syncRoot = new();

        public string DirectoryPath { get; }

        public RewardEvidenceDirectoryScope()
        {
            Monitor.Enter(syncRoot);
            DirectoryPath = Path.Combine(AppContext.BaseDirectory, "reward-evidence-tests", Guid.NewGuid().ToString("N"));
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
        public string Id { get; private set; } = "reward-runtime-evidence-test";
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
}
