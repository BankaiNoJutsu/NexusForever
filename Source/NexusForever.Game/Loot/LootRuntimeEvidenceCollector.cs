using System.Reflection;
using System.Text.Json;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Loot;
using NLog;
using NetworkLootItem = NexusForever.Network.World.Message.Model.Loot.LootItem;

namespace NexusForever.Game.Loot
{
    public static class LootRuntimeEvidenceCollector
    {
        private const string OutputDirectoryOverrideEnvironmentVariable = "NEXUSFOREVER_LOOT_EVIDENCE_DIR";

        private static readonly ILogger log = LogManager.GetLogger("LootRuntimeEvidence");
        private static readonly JsonSerializerOptions jsonOptions = new()
        {
            WriteIndented = true
        };

        private static string outputDirectory;

        public static string GetOutputDirectoryHint()
        {
            return ResolveOutputDirectory();
        }

        internal static void RecordNotifyIfArmed(IPlayer player, ServerLootNotify notify, bool includeGrantedItems, IReadOnlyCollection<LootPacketDiagnostics.NotifyItemState> items)
        {
            if (!TryBeginCapture(player, out _, out LootRuntimeEvidenceRecord record))
                return;

            record.Status = "notify-exported";
            record.OwnerUnitId = notify.OwnerUnitId;
            record.ParentUnitId = notify.ParentUnitId;
            record.ParentUnitIdInterpretation = notify.ParentUnitId == notify.OwnerUnitId
                ? "mirrors-owner-pending-retail-confirmation"
                : "custom-parent";
            record.IncludeGrantedItems = includeGrantedItems;
            record.Explosion = notify.Explosion;
            record.NotifyPacket = CreatePacketReference(notify);
            record.Items = items
                .Select(item => CreateCapturedItem(item, player.Guid))
                .ToList();

            Export(record);
        }

        internal static void RecordSuppressedNotifyIfArmed(IPlayer player, uint ownerUnitId, bool includeGrantedItems, bool explosion, int trackedItemCount, int deliveredItemCount)
        {
            if (!TryBeginCapture(player, out _, out LootRuntimeEvidenceRecord record))
                return;

            record.Status = "suppressed-no-visible-items";
            record.OwnerUnitId = ownerUnitId;
            record.ParentUnitId = ownerUnitId;
            record.ParentUnitIdInterpretation = "notify-suppressed-no-visible-items";
            record.IncludeGrantedItems = includeGrantedItems;
            record.Explosion = explosion;
            record.TrackedItemCount = trackedItemCount;
            record.DeliveredItemCount = deliveredItemCount;

            Export(record);
        }

        private static bool TryBeginCapture(IPlayer player, out ILootEvidenceCaptureSession session, out LootRuntimeEvidenceRecord record)
        {
            session = player?.Session as ILootEvidenceCaptureSession;
            if (session == null || !session.TryConsumeNextLootEvidenceCapture())
            {
                record = null;
                return false;
            }

            record = CreateRecord(player);
            return true;
        }

        private static LootRuntimeEvidenceRecord CreateRecord(IPlayer player)
        {
            return new LootRuntimeEvidenceRecord
            {
                OutputPath = Path.Combine(ResolveOutputDirectory(), BuildFileName(player)),
                CreatedAtUtc = DateTime.UtcNow,
                CharacterId = player.CharacterId,
                PlayerGuid = player.Guid,
                LootItemFieldOrder = LootFieldLayout.LootItemFieldOrder.ToList(),
                LootItemBooleanOrder = LootFieldLayout.LootItemBooleanOrder.ToList(),
                ServerLootNotifyHeaderOrder = LootFieldLayout.ServerLootNotifyHeaderOrder.ToList(),
                UnusedPacketFieldOrders = LootFieldLayout.UnusedPacketFieldOrders
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToList()),
                Blockers =
                [
                    "LootItem boolean meaning/order still needs retail confirmation.",
                    "ServerLootNotify.ParentUnitId currently mirrors OwnerUnitId because runtime has no distinct parent source yet.",
                    "ServerLootNotification and ServerLootCanLoot remain evidence-only packet models and are exported here as packet-shape references without being enqueued. ServerLootBindOnPickup is emitted when a bind-on-pickup static item needs client confirmation before delivery."
                ]
            };
        }

        private static LootRuntimeEvidenceCapturedItem CreateCapturedItem(LootPacketDiagnostics.NotifyItemState item, uint fallbackLooterUnitId)
        {
            return new LootRuntimeEvidenceCapturedItem
            {
                State = new LootRuntimeEvidenceItemState
                {
                    SourceLootUnitId = item.SourceLootUnitId,
                    SourceDelivered = item.SourceDelivered,
                    WinnerCharacterId = item.WinnerCharacterId,
                    WinnerGuid = item.WinnerGuid,
                    LootUnitId = item.LootUnitId,
                    Type = item.Type.ToString(),
                    ItemId = item.ItemId,
                    Amount = item.Amount,
                    CanLoot = item.CanLoot,
                    RequiresRoll = item.RequiresRoll,
                    OnlyMasterLootable = item.OnlyMasterLootable,
                    Explosion = item.Explosion,
                    Granted = item.Granted,
                    RollTime = item.RollTime,
                    RandomCircuitData = item.RandomCircuitData,
                    RandomGlyphData = item.RandomGlyphData,
                    ItemQuality2Id = item.ItemQuality2Id,
                    MasterListCount = item.MasterListCount
                },
                LootItemPayload = CreatePayloadReference(new NetworkLootItem
                {
                    LootUnitId = item.LootUnitId,
                    Type = item.Type,
                    ItemId = item.ItemId,
                    Amount = item.Amount,
                    CanLoot = item.CanLoot,
                    RequiresRoll = item.RequiresRoll,
                    OnlyMasterLootable = item.OnlyMasterLootable,
                    Explosion = item.Explosion,
                    Granted = item.Granted,
                    RollTime = item.RollTime,
                    RandomCircuitData = item.RandomCircuitData,
                    RandomGlyphData = item.RandomGlyphData,
                    ItemQuality2Id = item.ItemQuality2Id,
                    MasterList =
                    [
                        ..Enumerable.Repeat(new NexusForever.Network.World.Message.Model.Shared.Identity(), item.MasterListCount)
                    ]
                }, "LootItem"),
                FeedbackPacketTemplates = CreateFeedbackPacketTemplates(item, fallbackLooterUnitId)
            };
        }

        private static LootRuntimeEvidenceFeedbackPacketTemplates CreateFeedbackPacketTemplates(LootPacketDiagnostics.NotifyItemState item, uint fallbackLooterUnitId)
        {
            uint notificationLooterUnitId = item.WinnerGuid != 0u ? item.WinnerGuid : fallbackLooterUnitId;

            var templates = new LootRuntimeEvidenceFeedbackPacketTemplates
            {
                Notification = CreatePacketReference(new ServerLootNotification
                {
                    LootUnitId = item.LootUnitId,
                    ItemId = item.ItemId,
                    Amount = item.Amount,
                    LooterUnitId = notificationLooterUnitId,
                    Type = item.Type,
                    RandomCircuitData = item.RandomCircuitData,
                    RandomGlyphData = item.RandomGlyphData,
                    ItemQuality2Id = item.ItemQuality2Id
                }),
                NotificationTemplateNotes = item.WinnerGuid != 0u
                    ? "LooterUnitId uses the current winner guid."
                    : "LooterUnitId uses the capture player's guid as a live-testing placeholder because no winner guid is resolved yet.",
                CanLoot = item.LootUnitId != 0u
                    ? CreatePacketReference(new ServerLootCanLoot
                    {
                        LootUnitId = item.LootUnitId
                    })
                    : null,
                CanLootTemplateNotes = item.LootUnitId != 0u
                    ? "Shape-only reference for the currently unused can-loot feedback opcode."
                    : "Skipped because granted shower entries use LootUnitId 0.",
                BindOnPickup = item.Type == NexusForever.Game.Static.Loot.LootItemType.StaticItem && item.LootUnitId != 0u
                    ? CreatePacketReference(new ServerLootBindOnPickup
                    {
                        OwnerUnitId = fallbackLooterUnitId,
                        LootUnitId  = item.LootUnitId
                    })
                    : null,
                BindOnPickupTemplateNotes = item.Type == NexusForever.Game.Static.Loot.LootItemType.StaticItem && item.LootUnitId != 0u
                    ? "Template mirrors runtime bindcheck emission: OwnerUnitId plus LootUnitId before the second collect delivers the item."
                    : "Skipped because bind-on-pickup only applies to real static item loot entries with a nonzero LootUnitId."
            };

            return templates;
        }

        private static LootRuntimeEvidencePacketReference CreatePacketReference(IWritable packet)
        {
            MessageAttribute attribute = packet.GetType().GetCustomAttribute<MessageAttribute>();
            byte[] payload = SerializePayload(packet);
            return new LootRuntimeEvidencePacketReference
            {
                PacketName = packet.GetType().Name,
                Opcode = attribute?.Opcode.ToString(),
                OpcodeHex = attribute != null ? $"0x{(uint)attribute.Opcode:X4}" : null,
                PayloadByteLength = payload.Length,
                PayloadHex = Convert.ToHexString(payload)
            };
        }

        private static LootRuntimeEvidencePacketReference CreatePayloadReference(IWritable packet, string name)
        {
            byte[] payload = SerializePayload(packet);
            return new LootRuntimeEvidencePacketReference
            {
                PacketName = name,
                PayloadByteLength = payload.Length,
                PayloadHex = Convert.ToHexString(payload)
            };
        }

        private static byte[] SerializePayload(IWritable packet)
        {
            using var stream = new MemoryStream();
            using var writer = new GamePacketWriter(stream);
            packet.Write(writer);
            writer.FlushBits();
            return stream.ToArray();
        }

        private static void Export(LootRuntimeEvidenceRecord record)
        {
            try
            {
                string directory = ResolveOutputDirectory();
                Directory.CreateDirectory(directory);
                File.WriteAllText(record.OutputPath, JsonSerializer.Serialize(record, jsonOptions));
                log.Info(
                    "LootRuntimeEvidence exported characterId={0} ownerUnitId={1} status={2} path={3}",
                    record.CharacterId,
                    record.OwnerUnitId,
                    record.Status,
                    record.OutputPath);
            }
            catch (Exception exception)
            {
                log.Warn(exception,
                    "Failed to export loot runtime evidence for characterId={0}, ownerUnitId={1}.",
                    record.CharacterId,
                    record.OwnerUnitId);
            }
        }

        private static string ResolveOutputDirectory()
        {
            string overrideDirectory = Environment.GetEnvironmentVariable(OutputDirectoryOverrideEnvironmentVariable);
            if (!string.IsNullOrWhiteSpace(overrideDirectory))
                return overrideDirectory;

            if (!string.IsNullOrWhiteSpace(outputDirectory))
                return outputDirectory;

            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null)
            {
                if (Directory.Exists(Path.Combine(directory.FullName, "Source"))
                    && File.Exists(Path.Combine(directory.FullName, "README.md")))
                {
                    outputDirectory = Path.Combine(directory.FullName, "artifacts", "verify", "loot-evidence");
                    return outputDirectory;
                }

                directory = directory.Parent;
            }

            outputDirectory = Path.Combine(AppContext.BaseDirectory, "loot-evidence");
            return outputDirectory;
        }

        private static string BuildFileName(IPlayer player)
        {
            return $"{DateTime.UtcNow:yyyyMMdd-HHmmssfff}-loot-char-{player.CharacterId}-guid-{player.Guid}.json";
        }

        private static class LootFieldLayout
        {
            public static IReadOnlyList<string> LootItemFieldOrder { get; } =
            [
                "LootUnitId",
                "Type",
                "ItemId",
                "Amount",
                "CanLoot",
                "RequiresRoll",
                "OnlyMasterLootable",
                "Explosion",
                "Granted",
                "RollTime",
                "RandomCircuitData",
                "RandomGlyphData",
                "ItemQuality2Id",
                "MasterList"
            ];

            public static IReadOnlyList<string> LootItemBooleanOrder { get; } =
            [
                "CanLoot",
                "RequiresRoll",
                "OnlyMasterLootable",
                "Explosion",
                "Granted"
            ];

            public static IReadOnlyList<string> ServerLootNotifyHeaderOrder { get; } =
            [
                "OwnerUnitId",
                "ParentUnitId",
                "Explosion",
                "LootItems.Count"
            ];

            public static IReadOnlyDictionary<string, IReadOnlyList<string>> UnusedPacketFieldOrders { get; } =
                new Dictionary<string, IReadOnlyList<string>>
                {
                    ["ServerLootNotification"] =
                    [
                        "LootUnitId",
                        "ItemId",
                        "Amount",
                        "LooterUnitId",
                        "Type",
                        "RandomCircuitData",
                        "RandomGlyphData",
                        "ItemQuality2Id"
                    ],
                    ["ServerLootCanLoot"] =
                    [
                        "LootUnitId"
                    ],
                    ["ServerLootBindOnPickup"] =
                    [
                        "OwnerUnitId",
                        "LootUnitId"
                    ]
                };
        }

        internal sealed class LootRuntimeEvidenceRecord
        {
            public string OutputPath { get; set; }
            public DateTime CreatedAtUtc { get; set; }
            public string Status { get; set; }
            public ulong CharacterId { get; set; }
            public uint PlayerGuid { get; set; }
            public uint OwnerUnitId { get; set; }
            public uint ParentUnitId { get; set; }
            public string ParentUnitIdInterpretation { get; set; }
            public bool IncludeGrantedItems { get; set; }
            public bool Explosion { get; set; }
            public int TrackedItemCount { get; set; }
            public int DeliveredItemCount { get; set; }
            public List<string> LootItemFieldOrder { get; set; } = [];
            public List<string> LootItemBooleanOrder { get; set; } = [];
            public List<string> ServerLootNotifyHeaderOrder { get; set; } = [];
            public Dictionary<string, List<string>> UnusedPacketFieldOrders { get; set; } = [];
            public LootRuntimeEvidencePacketReference NotifyPacket { get; set; }
            public List<LootRuntimeEvidenceCapturedItem> Items { get; set; } = [];
            public List<string> Blockers { get; set; } = [];
        }

        internal sealed class LootRuntimeEvidenceCapturedItem
        {
            public LootRuntimeEvidenceItemState State { get; set; }
            public LootRuntimeEvidencePacketReference LootItemPayload { get; set; }
            public LootRuntimeEvidenceFeedbackPacketTemplates FeedbackPacketTemplates { get; set; }
        }

        internal sealed class LootRuntimeEvidenceItemState
        {
            public uint SourceLootUnitId { get; set; }
            public bool SourceDelivered { get; set; }
            public ulong WinnerCharacterId { get; set; }
            public uint WinnerGuid { get; set; }
            public uint LootUnitId { get; set; }
            public string Type { get; set; }
            public uint ItemId { get; set; }
            public uint Amount { get; set; }
            public bool CanLoot { get; set; }
            public bool RequiresRoll { get; set; }
            public bool OnlyMasterLootable { get; set; }
            public bool Explosion { get; set; }
            public bool Granted { get; set; }
            public uint RollTime { get; set; }
            public ulong RandomCircuitData { get; set; }
            public uint RandomGlyphData { get; set; }
            public uint ItemQuality2Id { get; set; }
            public int MasterListCount { get; set; }
        }

        internal sealed class LootRuntimeEvidenceFeedbackPacketTemplates
        {
            public LootRuntimeEvidencePacketReference Notification { get; set; }
            public string NotificationTemplateNotes { get; set; }
            public LootRuntimeEvidencePacketReference CanLoot { get; set; }
            public string CanLootTemplateNotes { get; set; }
            public LootRuntimeEvidencePacketReference BindOnPickup { get; set; }
            public string BindOnPickupTemplateNotes { get; set; }
        }

        internal sealed class LootRuntimeEvidencePacketReference
        {
            public string PacketName { get; set; }
            public string Opcode { get; set; }
            public string OpcodeHex { get; set; }
            public int PayloadByteLength { get; set; }
            public string PayloadHex { get; set; }
        }
    }
}
