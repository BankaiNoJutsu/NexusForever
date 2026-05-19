using System.Reflection;
using System.Text.Json;
using NexusForever.Game.Abstract.Account.Inventory;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Account;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.Packet;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NLog;
using NetworkIdentity = NexusForever.Network.World.Message.Model.Shared.Identity;

namespace NexusForever.Game.Account.Inventory
{
    public static class AccountRuntimeEvidenceCollector
    {
        private const string OutputDirectoryOverrideEnvironmentVariable = "NEXUSFOREVER_ACCOUNT_EVIDENCE_DIR";

        private static readonly ILogger log = LogManager.GetLogger("AccountRuntimeEvidence");
        private static readonly JsonSerializerOptions jsonOptions = new()
        {
            WriteIndented = true
        };

        private static string outputDirectory;

        public static string GetOutputDirectoryHint()
        {
            return ResolveOutputDirectory();
        }

        public static void RecordPendingGroupTransferIfArmed(IPlayer player, PendingAccountItemGroupDeliveryRequest request, AccountOperationResult result)
        {
            if (!TryBeginCapture(player, $"{request.Operation}-{request.TransferKind}-{result}", out AccountRuntimeEvidenceRecord record))
                return;

            record.Status        = result == AccountOperationResult.NoConnection ? "blocked-offline-transfer" : "blocked-transfer";
            record.CaptureSource = "pending-group-transfer";
            record.BlockedReason = result == AccountOperationResult.NoConnection ? "recipient-offline" : $"delivery-returned-{result}";
            record.Operation     = request.Operation.ToString();
            record.OperationValue = (uint)request.Operation;
            record.Result        = result.ToString();
            record.ResultValue   = (uint)result;
            record.Detail        = "Blocked pending-group transfer context captured without changing unsupported offline or coupon policy.";
            record.PendingGroupTransfer = new AccountRuntimeEvidencePendingGroupTransferContext
            {
                TransferKind    = request.TransferKind.ToString(),
                SourceGroup     = request.SourceGroup,
                SourceAccountId = request.SourceAccountId,
                TargetAccountId = request.TargetAccountId,
                AccountItemIds  = request.AccountItemIds.ToList(),
                SenderIdentity  = CreateIdentity(request.SenderIdentity),
                TargetIdentity  = CreateIdentity(request.TargetIdentity)
            };
            record.ResultPacketTemplate = CreatePacketReference(new ServerAccountOperationResult
            {
                Operation = request.Operation,
                Result    = result
            });
            record.Blockers =
            [
                result == AccountOperationResult.NoConnection
                    ? "Recipient account is offline; offline pending-group persistence, TTL, mail fallback, and coupon-aware routing remain evidence-blocked."
                    : "Pending-group delivery returned a non-Ok result; runtime behavior remains bounded to evidence-backed online transfer paths.",
                "The authoritative source pending group is preserved when the transfer does not complete."
            ];
            record.Notes =
            [
                "Use this artifact to compare future live-client captures before implementing new pending-group or coupon policy.",
                "Future coupon packet handlers can reuse RecordCouponBlockerIfArmed to export the same report family."
            ];

            Export(record);
        }

        public static void RecordCouponBlockerIfArmed(IPlayer player, string captureSource, string detail = null)
        {
            if (!TryBeginCapture(player, "coupon-blocked-report", out AccountRuntimeEvidenceRecord record))
                return;

            PopulateCouponBlockerRecord(record, captureSource, detail);
            Export(record);
        }

        public static string ExportCouponBlockerReport(IPlayer player, string captureSource, string detail = null)
        {
            if (player == null)
                return null;

            AccountRuntimeEvidenceRecord record = CreateRecord(player, "coupon-blocked-report");
            PopulateCouponBlockerRecord(record, captureSource, detail);
            return Export(record);
        }

        private static void PopulateCouponBlockerRecord(AccountRuntimeEvidenceRecord record, string captureSource, string detail)
        {
            record.Status         = "coupon-blocked-report";
            record.CaptureSource  = captureSource;
            record.BlockedReason  = "coupon-request-unmapped";
            record.Operation      = AccountOperation.RedeemCoupon.ToString();
            record.OperationValue = (uint)AccountOperation.RedeemCoupon;
            record.Result         = AccountOperationResult.InvalidCoupon.ToString();
            record.ResultValue    = (uint)AccountOperationResult.InvalidCoupon;
            record.Detail         = string.IsNullOrWhiteSpace(detail)
                ? "Manual coupon blocker snapshot captured before any coupon packet mapping or policy implementation."
                : detail;
            record.ResultPacketTemplate = CreatePacketReference(new ServerAccountOperationResult
            {
                Operation = AccountOperation.RedeemCoupon,
                Result    = AccountOperationResult.InvalidCoupon
            });
            record.Blockers =
            [
                "Selected-client evidence currently proves only AccountOperation.RedeemCoupon and AccountOperationResult.InvalidCoupon enum values.",
                "No authoritative client coupon request opcode or payload mapping is implemented yet.",
                "No retail-backed coupon redemption success path, failure policy, or pending-item interaction is implemented."
            ];
            record.Notes =
            [
                "This report is a shape-only scaffold for future live-client evidence collection.",
                "When coupon request packets are mapped, call RecordCouponBlockerIfArmed before adding any runtime policy."
            ];
        }

        private static bool TryBeginCapture(IPlayer player, string fileHint, out AccountRuntimeEvidenceRecord record)
        {
            var session = player?.Session as IAccountRuntimeEvidenceCaptureSession;
            if (session == null || !session.TryConsumeNextAccountRuntimeEvidenceCapture())
            {
                record = null;
                return false;
            }

            record = CreateRecord(player, fileHint);
            return true;
        }

        private static AccountRuntimeEvidenceRecord CreateRecord(IPlayer player, string fileHint)
        {
            return new AccountRuntimeEvidenceRecord
            {
                OutputPath   = Path.Combine(ResolveOutputDirectory(), BuildFileName(player, fileHint)),
                CreatedAtUtc = DateTime.UtcNow,
                Status       = "created",
                AccountId    = player.Account?.Id ?? 0u,
                CharacterId  = player.CharacterId,
                PlayerGuid   = player.Guid
            };
        }

        private static AccountRuntimeEvidenceIdentity CreateIdentity(NetworkIdentity identity)
        {
            if (identity == null)
                return null;

            return new AccountRuntimeEvidenceIdentity
            {
                RealmId = identity.RealmId,
                Id      = identity.Id
            };
        }

        private static AccountRuntimeEvidencePacketReference CreatePacketReference(IWritable packet)
        {
            MessageAttribute attribute = packet.GetType().GetCustomAttribute<MessageAttribute>();
            byte[] payload = SerializePayload(packet);
            return new AccountRuntimeEvidencePacketReference
            {
                PacketName        = packet.GetType().Name,
                Opcode            = attribute?.Opcode.ToString(),
                OpcodeHex         = attribute != null ? $"0x{(uint)attribute.Opcode:X4}" : null,
                PayloadByteLength = payload.Length,
                PayloadHex        = Convert.ToHexString(payload)
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

        private static string Export(AccountRuntimeEvidenceRecord record)
        {
            try
            {
                string directory = ResolveOutputDirectory();
                Directory.CreateDirectory(directory);
                File.WriteAllText(record.OutputPath, JsonSerializer.Serialize(record, jsonOptions));
                log.Info(
                    "AccountRuntimeEvidence exported accountId={0} characterId={1} status={2} operation={3} path={4}",
                    record.AccountId,
                    record.CharacterId,
                    record.Status,
                    record.Operation,
                    record.OutputPath);
                return record.OutputPath;
            }
            catch (Exception exception)
            {
                log.Warn(exception,
                    "Failed to export account runtime evidence for accountId={0}, characterId={1}, operation={2}.",
                    record.AccountId,
                    record.CharacterId,
                    record.Operation);
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
                    outputDirectory = Path.Combine(directory.FullName, "artifacts", "verify", "account-evidence");
                    return outputDirectory;
                }

                directory = directory.Parent;
            }

            outputDirectory = Path.Combine(AppContext.BaseDirectory, "account-evidence");
            return outputDirectory;
        }

        private static string BuildFileName(IPlayer player, string fileHint)
        {
            return $"{DateTime.UtcNow:yyyyMMdd-HHmmssfff}-account-char-{player.CharacterId}-guid-{player.Guid}-{SanitizePathPart(fileHint)}.json";
        }

        private static string SanitizePathPart(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "capture";

            return string.Concat(value.Select(c => Path.GetInvalidFileNameChars().Contains(c) || char.IsWhiteSpace(c) ? '-' : c))
                .Trim('-');
        }

        private sealed class AccountRuntimeEvidenceRecord
        {
            public string OutputPath { get; set; }
            public DateTime CreatedAtUtc { get; set; }
            public string Status { get; set; }
            public string CaptureSource { get; set; }
            public string BlockedReason { get; set; }
            public uint AccountId { get; set; }
            public ulong CharacterId { get; set; }
            public uint PlayerGuid { get; set; }
            public string Operation { get; set; }
            public uint OperationValue { get; set; }
            public string Result { get; set; }
            public uint ResultValue { get; set; }
            public string Detail { get; set; }
            public AccountRuntimeEvidencePendingGroupTransferContext PendingGroupTransfer { get; set; }
            public AccountRuntimeEvidencePacketReference ResultPacketTemplate { get; set; }
            public List<string> Blockers { get; set; } = [];
            public List<string> Notes { get; set; } = [];
        }

        private sealed class AccountRuntimeEvidencePendingGroupTransferContext
        {
            public string TransferKind { get; set; }
            public string SourceGroup { get; set; }
            public uint SourceAccountId { get; set; }
            public uint TargetAccountId { get; set; }
            public List<uint> AccountItemIds { get; set; } = [];
            public AccountRuntimeEvidenceIdentity SenderIdentity { get; set; }
            public AccountRuntimeEvidenceIdentity TargetIdentity { get; set; }
        }

        private sealed class AccountRuntimeEvidenceIdentity
        {
            public uint RealmId { get; set; }
            public ulong Id { get; set; }
        }

        private sealed class AccountRuntimeEvidencePacketReference
        {
            public string PacketName { get; set; }
            public string Opcode { get; set; }
            public string OpcodeHex { get; set; }
            public int PayloadByteLength { get; set; }
            public string PayloadHex { get; set; }
        }
    }
}
