using System;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NexusForever.WorldServer.Network;

namespace NexusForever.WorldServer.Support
{
    internal sealed class FileSupportSubmissionStore : ISupportSubmissionStore
    {
        private static readonly object writeLock = new();
        private static readonly JsonSerializerOptions jsonOptions = new()
        {
            WriteIndented = false
        };

        private readonly ILogger<FileSupportSubmissionStore> log;

        public FileSupportSubmissionStore(ILogger<FileSupportSubmissionStore> log)
        {
            this.log = log;
        }

        public bool TryAppend(IWorldSession session, string type, object payload)
        {
            try
            {
                string directory = Path.Combine(AppContext.BaseDirectory, "support-submissions");
                Directory.CreateDirectory(directory);

                var record = new
                {
                    Type = type,
                    CreatedUtc = DateTime.UtcNow,
                    AccountId = session.Account?.Id,
                    PlayerGuid = session.Player?.Guid,
                    CharacterId = session.Player?.CharacterId,
                    CharacterName = session.Player?.Name,
                    Payload = payload
                };

                string line = JsonSerializer.Serialize(record, jsonOptions);
                string path = Path.Combine(directory, $"{DateTime.UtcNow:yyyyMMdd}.jsonl");
                lock (writeLock)
                    File.AppendAllText(path, line + Environment.NewLine);

                return true;
            }
            catch (Exception exception)
            {
                log.LogError(exception, "Failed to persist support submission {SubmissionType} for player {PlayerGuid}.",
                    type, session.Player?.Guid);
                return false;
            }
        }
    }
}
