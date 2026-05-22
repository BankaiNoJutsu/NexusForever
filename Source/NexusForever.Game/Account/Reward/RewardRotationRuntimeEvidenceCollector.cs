using System.Reflection;
using System.Text.Json;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.Packet;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NLog;

namespace NexusForever.Game.Account.Reward
{
    public static class RewardRotationRuntimeEvidenceCollector
    {
        private const string OutputDirectoryOverrideEnvironmentVariable = "NEXUSFOREVER_REWARD_EVIDENCE_DIR";

        private static readonly ILogger log = LogManager.GetLogger("RewardRotationRuntimeEvidence");
        private static readonly JsonSerializerOptions jsonOptions = new()
        {
            WriteIndented = true
        };

        private static string outputDirectory;

        public static string GetOutputDirectoryHint()
        {
            return ResolveOutputDirectory();
        }

        public static void RecordRequestIfArmed(IPlayer player, uint requestedRewardRotationIndex, RewardRotationRefresh refresh, string detail = null)
        {
            if (!TryBeginCapture(player, requestedRewardRotationIndex, "client-request", out RewardRotationRuntimeEvidenceRecord record))
                return;

            PopulateRecord(record, refresh, detail ?? "Live reward rotation request captured without generating speculative schedule rows or entry-state transitions.");
            Export(record);
        }

        public static string ExportReport(IPlayer player, uint requestedRewardRotationIndex, string captureSource, string detail = null, IRewardRotationRefreshProvider provider = null)
        {
            if (player == null)
                return null;

            RewardRotationRefresh refresh = RewardRotationRefreshBuilder.Build(requestedRewardRotationIndex, provider);
            RewardRotationRuntimeEvidenceRecord record = CreateRecord(player, requestedRewardRotationIndex, captureSource);
            PopulateRecord(record, refresh, detail ?? "Manual reward rotation report exported using the current placeholder/diagnostic provider without generating speculative live rows.");
            return Export(record);
        }

        private static bool TryBeginCapture(IPlayer player, uint requestedRewardRotationIndex, string captureSource, out RewardRotationRuntimeEvidenceRecord record)
        {
            var session = player?.Session as IRewardRotationEvidenceCaptureSession;
            if (session == null || !session.TryConsumeNextRewardRotationEvidenceCapture())
            {
                record = null;
                return false;
            }

            record = CreateRecord(player, requestedRewardRotationIndex, captureSource);
            return true;
        }

        private static RewardRotationRuntimeEvidenceRecord CreateRecord(IPlayer player, uint requestedRewardRotationIndex, string captureSource)
        {
            return new RewardRotationRuntimeEvidenceRecord
            {
                OutputPath = Path.Combine(ResolveOutputDirectory(), BuildFileName(player, requestedRewardRotationIndex, captureSource)),
                CreatedAtUtc = DateTime.UtcNow,
                CaptureSource = captureSource,
                AccountId = player.Account?.Id ?? 0u,
                CharacterId = player.CharacterId,
                PlayerGuid = player.Guid,
                RequestedRewardRotationIndex = requestedRewardRotationIndex,
                RequestSupported = RewardRotationRefreshBuilder.IsSupported(requestedRewardRotationIndex),
                SupportedRewardRotationRange = $"0-{RewardRotationRefreshBuilder.MaxSupportedRewardRotationIndex}",
                ScheduleRowProtocol = new RewardRotationRowProtocol
                {
                    PacketName = nameof(ServerRewardRotationScheduleArray),
                    Fields =
                    [
                        "Count:uint32",
                        "Entry[n].ContentId:uint32",
                        "Entry[n].RewardKeyId:uint14",
                        "Entry[n].Duration:float32",
                        "Entry[n].RewardType:uint8",
                        "Entry[n].Value:uint32"
                    ],
                    Constraints =
                    [
                        "ContentId must stay within the 14-bit wire limit (0x3FFF).",
                        "Duration must be finite.",
                        "RewardType is currently evidence-backed only for values 1-3."
                    ]
                },
                EntryStateRowProtocol = new RewardRotationRowProtocol
                {
                    PacketName = nameof(ServerRewardRotationEntryStateArray),
                    Fields =
                    [
                        "Count:uint32",
                        "Entry[n].TypeId:uint3",
                        "Entry[n].ContentId:uint32",
                        "Entry[n].RewardTypeId:uint32",
                        "Entry[n].State:uint8",
                        "Entry[n].Value:uint32"
                    ],
                    Constraints =
                    [
                        "TypeId must stay within the 3-bit wire limit (0x7).",
                        "Entry-state array and upsert/update/remove packets share the same row payload shape.",
                        "State is the reward-type lane (1=item, 2=essence, 3=modifier); Value is a grant-flag bitmask (0x1 item/modifier, 0x80000000 essence)."
                    ]
                },
                EntryStateDeltaPackets =
                [
                    CreatePacketTemplateReference(
                        new ServerRewardRotationEntryStateUpsert { State = RewardRotationScheduleBuilder.RewardTypeItem },
                        "Shape-only delta template; sample lane 1 payload is diagnostic only and is not enqueued."),
                    CreatePacketTemplateReference(
                        new ServerRewardRotationEntryStateUpdate { State = RewardRotationScheduleBuilder.RewardTypeItem },
                        "Shape-only delta template; sample lane 1 payload is diagnostic only and is not enqueued."),
                    CreatePacketTemplateReference(
                        new ServerRewardRotationEntryStateRemove { State = RewardRotationScheduleBuilder.RewardTypeItem },
                        "Shape-only delta template; sample lane 1 payload is diagnostic only and is not enqueued.")
                ],
                Blockers =
                [
                    "Only the request index range below seven is evidence-backed for client-originated refresh requests.",
                    "Schedule rows from GameTableRewardRotationRefreshProvider use correlated global-catalog selection until per-content reward mapping is verified.",
                    "Account grant persistence is required before sending non-empty entry-state arrays; 0x07CD Flag apply helper remains unmapped."
                ],
                Notes =
                [
                    "Empty schedule/state arrays are intentional placeholders until live reward rotation evidence is captured.",
                    "If a future provider injects non-placeholder rows, this report automatically records packet payloads plus structured row diagnostics for review before runtime behavior changes."
                ]
            };
        }

        private static void PopulateRecord(RewardRotationRuntimeEvidenceRecord record, RewardRotationRefresh refresh, string detail)
        {
            record.Detail = detail;
            record.PlaceholderResponse = refresh?.IsPlaceholder ?? false;
            record.ContentContextIdCount = refresh?.ContentContextIdCount ?? 0;
            record.ContentContextPacketCount = refresh?.ContentContextPacketCount ?? 0;
            record.ScheduleEntryCount = refresh?.ScheduleEntryCount ?? 0;
            record.EntryStateCount = refresh?.EntryStateCount ?? 0;
            record.ResponseSource = refresh?.ResponseSource ?? "unsupported-request";
            record.ContentContextPackets = refresh != null
                ? refresh.GetContentContextPackets().Select(CreatePacketReference).ToList()
                : [];
            record.ScheduleRows = refresh?.ScheduleArray.Entries.Select((row, index) => CreateScheduleRowDiagnostic(row, index)).ToList() ?? [];
            record.EntryStateRows = refresh?.EntryStateArray.Entries.Select((row, index) => CreateEntryStateRowDiagnostic(row, index)).ToList() ?? [];
            record.ScheduleArrayPacket = refresh != null
                ? CreatePacketReference(refresh.ScheduleArray)
                : null;
            record.EntryStateArrayPacket = refresh != null
                ? CreatePacketReference(refresh.EntryStateArray)
                : null;

            if (refresh == null)
            {
                record.Notes.Add("Unsupported request indices do not currently enqueue reward rotation schedule or entry-state packets.");
                return;
            }

            if (refresh.IsPlaceholder)
            {
                record.Notes.Add("Captured response is the current empty placeholder scaffold.");
                return;
            }

            record.Notes.Add("Captured response contained non-placeholder rows; inspect the structured row diagnostics before implementing any live semantics.");
        }

        private static RewardRotationPacketReference CreatePacketReference(IWritable packet)
        {
            MessageAttribute attribute = packet.GetType().GetCustomAttribute<MessageAttribute>();
            byte[] payload = SerializePayload(packet);
            return new RewardRotationPacketReference
            {
                PacketName = packet.GetType().Name,
                Opcode = attribute?.Opcode.ToString(),
                OpcodeHex = attribute != null ? $"0x{(uint)attribute.Opcode:X4}" : null,
                PayloadByteLength = payload.Length,
                PayloadHex = Convert.ToHexString(payload)
            };
        }

        private static RewardRotationPacketTemplateReference CreatePacketTemplateReference(IWritable packet, string notes)
        {
            RewardRotationPacketReference packetReference = CreatePacketReference(packet);
            return new RewardRotationPacketTemplateReference
            {
                PacketName = packetReference.PacketName,
                Opcode = packetReference.Opcode,
                OpcodeHex = packetReference.OpcodeHex,
                PayloadByteLength = packetReference.PayloadByteLength,
                PayloadHex = packetReference.PayloadHex,
                Notes = notes
            };
        }

        private static RewardRotationScheduleRowDiagnostic CreateScheduleRowDiagnostic(ServerRewardRotationScheduleArray.ScheduleRow row, int index)
        {
            return new RewardRotationScheduleRowDiagnostic
            {
                Index = index,
                RewardKeyId = row?.RewardKeyId ?? 0u,
                ContentId = row?.ContentId ?? 0u,
                Duration = row?.Duration ?? 0f,
                RewardType = row?.RewardType ?? 0,
                Value = row?.Value ?? 0u
            };
        }

        private static RewardRotationEntryStateRowDiagnostic CreateEntryStateRowDiagnostic(ServerRewardRotationEntryStateArray.EntryStateRow row, int index)
        {
            return new RewardRotationEntryStateRowDiagnostic
            {
                Index = index,
                TypeId = row?.TypeId ?? 0u,
                ContentId = row?.ContentId ?? 0u,
                RewardTypeId = row?.RewardTypeId ?? 0u,
                State = row?.State ?? 0,
                Value = row?.Value ?? 0u
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

        private static string Export(RewardRotationRuntimeEvidenceRecord record)
        {
            try
            {
                string directory = ResolveOutputDirectory();
                Directory.CreateDirectory(directory);
                File.WriteAllText(record.OutputPath, JsonSerializer.Serialize(record, jsonOptions));
                log.Info(
                    "RewardRotationRuntimeEvidence exported accountId={0} characterId={1} requestedIndex={2} supported={3} path={4}",
                    record.AccountId,
                    record.CharacterId,
                    record.RequestedRewardRotationIndex,
                    record.RequestSupported,
                    record.OutputPath);
                return record.OutputPath;
            }
            catch (Exception exception)
            {
                log.Warn(exception,
                    "Failed to export reward rotation runtime evidence for accountId={0}, characterId={1}, requestedIndex={2}.",
                    record.AccountId,
                    record.CharacterId,
                    record.RequestedRewardRotationIndex);
                return null;
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
                    outputDirectory = Path.Combine(directory.FullName, "artifacts", "verify", "reward-evidence");
                    return outputDirectory;
                }

                directory = directory.Parent;
            }

            outputDirectory = Path.Combine(AppContext.BaseDirectory, "reward-evidence");
            return outputDirectory;
        }

        private static string BuildFileName(IPlayer player, uint requestedRewardRotationIndex, string captureSource)
        {
            return $"{DateTime.UtcNow:yyyyMMdd-HHmmssfff}-reward-char-{player.CharacterId}-guid-{player.Guid}-index-{requestedRewardRotationIndex}-{SanitizePathPart(captureSource)}.json";
        }

        private static string SanitizePathPart(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "capture";

            return string.Concat(value.Select(c => Path.GetInvalidFileNameChars().Contains(c) || char.IsWhiteSpace(c) ? '-' : c))
                .Trim('-');
        }

        private sealed class RewardRotationRuntimeEvidenceRecord
        {
            public string OutputPath { get; set; }
            public DateTime CreatedAtUtc { get; set; }
            public string CaptureSource { get; set; }
            public uint AccountId { get; set; }
            public ulong CharacterId { get; set; }
            public uint PlayerGuid { get; set; }
            public uint RequestedRewardRotationIndex { get; set; }
            public bool RequestSupported { get; set; }
            public string SupportedRewardRotationRange { get; set; }
            public string Detail { get; set; }
            public bool PlaceholderResponse { get; set; }
            public string ResponseSource { get; set; }
            public int ContentContextIdCount { get; set; }
            public int ContentContextPacketCount { get; set; }
            public List<RewardRotationPacketReference> ContentContextPackets { get; set; } = [];
            public int ScheduleEntryCount { get; set; }
            public int EntryStateCount { get; set; }
            public RewardRotationPacketReference ScheduleArrayPacket { get; set; }
            public RewardRotationPacketReference EntryStateArrayPacket { get; set; }
            public RewardRotationRowProtocol ScheduleRowProtocol { get; set; }
            public RewardRotationRowProtocol EntryStateRowProtocol { get; set; }
            public List<RewardRotationPacketTemplateReference> EntryStateDeltaPackets { get; set; } = [];
            public List<RewardRotationScheduleRowDiagnostic> ScheduleRows { get; set; } = [];
            public List<RewardRotationEntryStateRowDiagnostic> EntryStateRows { get; set; } = [];
            public List<string> Blockers { get; set; } = [];
            public List<string> Notes { get; set; } = [];
        }

        private class RewardRotationPacketReference
        {
            public string PacketName { get; set; }
            public string Opcode { get; set; }
            public string OpcodeHex { get; set; }
            public int PayloadByteLength { get; set; }
            public string PayloadHex { get; set; }
        }

        private sealed class RewardRotationPacketTemplateReference : RewardRotationPacketReference
        {
            public string Notes { get; set; }
        }

        private sealed class RewardRotationRowProtocol
        {
            public string PacketName { get; set; }
            public List<string> Fields { get; set; } = [];
            public List<string> Constraints { get; set; } = [];
        }

        private sealed class RewardRotationScheduleRowDiagnostic
        {
            public int Index { get; set; }
            public uint RewardKeyId { get; set; }
            public uint ContentId { get; set; }
            public float Duration { get; set; }
            public byte RewardType { get; set; }
            public uint Value { get; set; }
        }

        private sealed class RewardRotationEntryStateRowDiagnostic
        {
            public int Index { get; set; }
            public uint TypeId { get; set; }
            public uint ContentId { get; set; }
            public uint RewardTypeId { get; set; }
            public byte State { get; set; }
            public uint Value { get; set; }
        }
    }
}
