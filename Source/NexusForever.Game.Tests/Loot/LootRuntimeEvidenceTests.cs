using System.Collections.Immutable;
using System.Net.Sockets;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NexusForever.Database.Auth.Model;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Group;
using NexusForever.Game.Abstract.Loot;
using NexusForever.Game.Loot;
using NexusForever.Game.Static;
using NexusForever.Game.Static.Loot;
using NexusForever.Game.Static.RBAC;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Configuration.Model;
using NexusForever.GameTable.Model;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.Packet;
using NexusForever.Network.Session;
using NexusForever.Shared;
using NexusForever.Shared.Game.Events;
using NexusForever.WorldServer.Command.Context;
using NexusForever.WorldServer.Command.Handler;
using NexusForever.WorldServer.Network;

namespace NexusForever.Game.Tests.Loot;

[Collection(LegacyServiceProviderCollection.Name)]
public class LootRuntimeEvidenceTests
{
    [Fact]
    public void SendLootNotify_ArmedEvidenceCaptureExportsNotifyArtifact()
    {
        using var output = new LootEvidenceDirectoryScope();

        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        IGroupStateManager groupStateManager = RecordingDispatchProxy<IGroupStateManager>.Create(out _);
        LegacyServiceProvider.Provider = BuildLootProvider(groupStateManager, 123u);

        try
        {
            var session = new TestWorldSession();
            session.ArmNextLootEvidenceCapture();

            IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
            playerProxy.SetProperty(nameof(IPlayer.CharacterId), 42ul);
            playerProxy.SetProperty(nameof(IPlayer.Session), session);
            playerProxy.SetProperty("Guid", 4242u);

            var lootInstance = new LootInstance(
                ownerUnitId: 99u,
                looterIds: new Dictionary<ulong, uint> { [42ul] = 4242u },
                looterType: LooterType.Player,
                lootEntityType: LootEntityType.Creature)
            {
                Explosion = true
            };

            lootInstance.AddLootItem(123u, LootItemType.StaticItem, 2u);
            lootInstance.SendLootNotify(player);

            string artifactPath = Assert.Single(Directory.GetFiles(output.DirectoryPath, "*.json"));
            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(artifactPath));
            JsonElement root = document.RootElement;

            Assert.Equal("notify-exported", root.GetProperty("Status").GetString());
            Assert.Equal(99u, root.GetProperty("OwnerUnitId").GetUInt32());
            Assert.Equal(99u, root.GetProperty("ParentUnitId").GetUInt32());
            Assert.Contains("mirrors-owner", root.GetProperty("ParentUnitIdInterpretation").GetString());

            JsonElement booleanOrder = root.GetProperty("LootItemBooleanOrder");
            Assert.Equal("CanLoot", booleanOrder[0].GetString());
            Assert.Equal("RequiresRoll", booleanOrder[1].GetString());
            Assert.Equal("OnlyMasterLootable", booleanOrder[2].GetString());
            Assert.Equal("Explosion", booleanOrder[3].GetString());
            Assert.Equal("Granted", booleanOrder[4].GetString());

            JsonElement notifyPacket = root.GetProperty("NotifyPacket");
            Assert.Equal("ServerLootNotify", notifyPacket.GetProperty("PacketName").GetString());
            Assert.NotEmpty(notifyPacket.GetProperty("PayloadHex").GetString());

            JsonElement item = Assert.Single(root.GetProperty("Items").EnumerateArray().ToArray());
            Assert.Equal("LootItem", item.GetProperty("LootItemPayload").GetProperty("PacketName").GetString());
            Assert.Equal("ServerLootNotification", item.GetProperty("FeedbackPacketTemplates").GetProperty("Notification").GetProperty("PacketName").GetString());
            Assert.Equal("ServerLootCanLoot", item.GetProperty("FeedbackPacketTemplates").GetProperty("CanLoot").GetProperty("PacketName").GetString());
            Assert.Equal("ServerLootBindOnPickup", item.GetProperty("FeedbackPacketTemplates").GetProperty("BindOnPickup").GetProperty("PacketName").GetString());
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void SendLootNotify_ArmedEvidenceCaptureExportsSuppressedArtifact()
    {
        using var output = new LootEvidenceDirectoryScope();

        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        IGroupStateManager groupStateManager = RecordingDispatchProxy<IGroupStateManager>.Create(out _);
        LegacyServiceProvider.Provider = BuildLootProvider(groupStateManager, 123u);

        try
        {
            var session = new TestWorldSession();
            session.ArmNextLootEvidenceCapture();

            IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
            ICurrencyManager currencyManager = RecordingDispatchProxy<ICurrencyManager>.Create(out _);
            playerProxy.SetProperty(nameof(IPlayer.CharacterId), 42ul);
            playerProxy.SetProperty(nameof(IPlayer.CurrencyManager), currencyManager);
            playerProxy.SetProperty(nameof(IPlayer.Session), session);
            playerProxy.SetProperty("Guid", 4242u);

            var lootInstance = new LootInstance(
                ownerUnitId: 77u,
                looterIds: new Dictionary<ulong, uint> { [42ul] = 4242u },
                looterType: LooterType.Player,
                lootEntityType: LootEntityType.Creature);

            LootInstanceItem delivered = lootInstance.AddLootItem(1u, LootItemType.Cash, 5u);
            delivered.SetWinner(player);
            Assert.True(delivered.DeliverItem(player, sendAsGrant: false));

            lootInstance.SendLootNotify(player);

            string artifactPath = Assert.Single(Directory.GetFiles(output.DirectoryPath, "*.json"));
            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(artifactPath));
            JsonElement root = document.RootElement;

            Assert.Equal("suppressed-no-visible-items", root.GetProperty("Status").GetString());
            Assert.Equal(77u, root.GetProperty("OwnerUnitId").GetUInt32());
            Assert.Equal(1, root.GetProperty("TrackedItemCount").GetInt32());
            Assert.Equal(1, root.GetProperty("DeliveredItemCount").GetInt32());
            Assert.Equal(JsonValueKind.Null, root.GetProperty("NotifyPacket").ValueKind);
            Assert.Empty(root.GetProperty("Items").EnumerateArray().ToArray());
            Assert.IsType<NexusForever.Network.World.Message.Model.Loot.ServerLootRemove>(Assert.Single(session.EncryptedMessages));
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void HandleLootCaptureNext_ArmsWorldSessionAndReportsOutputHint()
    {
        IGlobalLootManager lootManager = RecordingDispatchProxy<IGlobalLootManager>.Create(out _);
        var category = new LootCommandCategory(lootManager);
        var session = new TestWorldSession();

        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);

        var context = new TestCommandContext(
            player,
            null,
            [Permission.Item, Permission.ItemInfo]);

        category.HandleLootCaptureNext(context);

        Assert.True(session.TryConsumeNextLootEvidenceCapture());
        Assert.Single(context.Messages);
        Assert.Contains("loot-evidence", context.Messages[0], StringComparison.OrdinalIgnoreCase);
    }

    private static IServiceProvider BuildLootProvider(IGroupStateManager groupStateManager, uint staticItemId)
    {
        var lootManager = new GlobalLootManager(groupStateManager);
        var gameTableManager = new GameTableManager(Options.Create(new GameTableConfig
        {
            GameTablePath = string.Empty
        }));

        SetAutoProperty(gameTableManager, nameof(GameTableManager.Item), CreateGameTable(new Item2Entry
        {
            Id = staticItemId
        }));

        return new ServiceCollection()
            .AddSingleton(lootManager)
            .AddSingleton(gameTableManager)
            .BuildServiceProvider();
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

    private sealed class LootEvidenceDirectoryScope : IDisposable
    {
        private const string EnvironmentVariableName = "NEXUSFOREVER_LOOT_EVIDENCE_DIR";
        private static readonly object syncRoot = new();

        public string DirectoryPath { get; }

        public LootEvidenceDirectoryScope()
        {
            Monitor.Enter(syncRoot);
            DirectoryPath = Path.Combine(AppContext.BaseDirectory, "loot-evidence-tests", Guid.NewGuid().ToString("N"));
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

        public IAccount Account => null;
        public IPlayer Player { get; set; }
        public List<CharacterModel> Characters { get; } = [];
        public bool? IsQueued { get; set; }
        public bool CanProcessIncomingPackets { get; set; }
        public bool CanProcessOutgoingPackets { get; set; }
        public string Id { get; private set; } = "loot-runtime-evidence-test";
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
}
