using System.Globalization;
using System.Text.Json;
using NexusForever.Network.Message;
using NLog;

namespace NexusForever.Network.Diagnostics
{
    public static class PacketEvidenceRecorder
    {
        public const string EnabledEnvironmentVariable = "NEXUSFOREVER_PACKET_EVIDENCE";
        public const string OutputDirectoryEnvironmentVariable = "NEXUSFOREVER_PACKET_EVIDENCE_DIR";
        public const string OpcodeFilterEnvironmentVariable = "NEXUSFOREVER_PACKET_EVIDENCE_OPCODES";

        private static readonly ILogger log = LogManager.GetLogger("PacketEvidence");
        private static readonly object sync = new();
        private static readonly Lazy<PacketEvidenceSettings> settings = new(CreateSettings);
        private static readonly JsonSerializerOptions jsonOptions = new()
        {
            WriteIndented = false
        };

        public static string GetOutputPathHint()
        {
            return settings.Value.OutputPath;
        }

        public static void Record(
            string sessionId,
            string direction,
            GameMessageOpcode opcode,
            string messageType,
            byte[] plaintext,
            bool encryptedTransport)
        {
            Record(sessionId, direction, (uint)opcode, opcode.ToString(), messageType, plaintext, encryptedTransport);
        }

        public static void Record(
            string sessionId,
            string direction,
            uint opcode,
            string opcodeName,
            string messageType,
            byte[] plaintext,
            bool encryptedTransport)
        {
            PacketEvidenceSettings activeSettings = settings.Value;
            if (!activeSettings.Enabled || !activeSettings.ShouldRecord(opcode))
                return;

            try
            {
                Directory.CreateDirectory(activeSettings.OutputDirectory);

                var record = new PacketEvidenceRecord
                {
                    CreatedAtUtc = DateTime.UtcNow,
                    SessionId = sessionId,
                    Direction = direction,
                    Opcode = opcodeName,
                    OpcodeHex = $"0x{opcode:X4}",
                    MessageType = messageType,
                    EncryptedTransport = encryptedTransport,
                    PlaintextByteLength = plaintext?.Length ?? 0,
                    PlaintextHex = plaintext != null ? Convert.ToHexString(plaintext) : string.Empty
                };

                string line = JsonSerializer.Serialize(record, jsonOptions);
                lock (sync)
                    File.AppendAllText(activeSettings.OutputPath, line + Environment.NewLine);
            }
            catch (Exception exception)
            {
                log.Warn(exception,
                    "Failed to record packet evidence for session={0}, direction={1}, opcode=0x{2:X4}.",
                    sessionId,
                    direction,
                    opcode);
            }
        }

        private static PacketEvidenceSettings CreateSettings()
        {
            bool enabled = IsTruthy(Environment.GetEnvironmentVariable(EnabledEnvironmentVariable));
            string outputDirectory = ResolveOutputDirectory();
            string outputPath = Path.Combine(outputDirectory, $"{DateTime.UtcNow:yyyyMMdd-HHmmss}-packet-evidence.jsonl");
            HashSet<uint> opcodeFilter = ParseOpcodeFilter(Environment.GetEnvironmentVariable(OpcodeFilterEnvironmentVariable));

            return new PacketEvidenceSettings(enabled, outputDirectory, outputPath, opcodeFilter);
        }

        private static bool IsTruthy(string value)
        {
            return value != null
                && (string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(value, "on", StringComparison.OrdinalIgnoreCase));
        }

        private static HashSet<uint> ParseOpcodeFilter(string value)
        {
            var opcodes = new HashSet<uint>();
            if (string.IsNullOrWhiteSpace(value))
                return opcodes;

            foreach (string token in value.Split([',', ';', ' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (Enum.TryParse(token, true, out GameMessageOpcode namedOpcode))
                {
                    opcodes.Add((uint)namedOpcode);
                    continue;
                }

                if (TryParseUInt32(token, out uint opcode))
                    opcodes.Add(opcode);
                else
                    log.Warn("Ignoring invalid packet evidence opcode filter token '{0}'.", token);
            }

            return opcodes;
        }

        private static bool TryParseUInt32(string token, out uint value)
        {
            if (token.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                return uint.TryParse(token[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);

            return uint.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        private static string ResolveOutputDirectory()
        {
            string overrideDirectory = Environment.GetEnvironmentVariable(OutputDirectoryEnvironmentVariable);
            if (!string.IsNullOrWhiteSpace(overrideDirectory))
                return overrideDirectory;

            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null)
            {
                if (Directory.Exists(Path.Combine(directory.FullName, "Source"))
                    && File.Exists(Path.Combine(directory.FullName, "README.md")))
                    return Path.Combine(directory.FullName, "artifacts", "verify", "packet-evidence");

                directory = directory.Parent;
            }

            return Path.Combine(AppContext.BaseDirectory, "packet-evidence");
        }

        private sealed class PacketEvidenceSettings
        {
            public PacketEvidenceSettings(bool enabled, string outputDirectory, string outputPath, HashSet<uint> opcodeFilter)
            {
                Enabled = enabled;
                OutputDirectory = outputDirectory;
                OutputPath = outputPath;
                OpcodeFilter = opcodeFilter;
            }

            public bool Enabled { get; }
            public string OutputDirectory { get; }
            public string OutputPath { get; }
            private HashSet<uint> OpcodeFilter { get; }

            public bool ShouldRecord(uint opcode)
            {
                return OpcodeFilter.Count == 0 || OpcodeFilter.Contains(opcode);
            }
        }

        private sealed class PacketEvidenceRecord
        {
            public DateTime CreatedAtUtc { get; set; }
            public string SessionId { get; set; }
            public string Direction { get; set; }
            public string Opcode { get; set; }
            public string OpcodeHex { get; set; }
            public string MessageType { get; set; }
            public bool EncryptedTransport { get; set; }
            public int PlaintextByteLength { get; set; }
            public string PlaintextHex { get; set; }
        }
    }
}
