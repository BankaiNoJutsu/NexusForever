using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Service;

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
        private readonly IBackgroundTaskRunner backgroundTaskRunner;

        public FileSupportSubmissionStore(
            ILogger<FileSupportSubmissionStore> log,
            IBackgroundTaskRunner backgroundTaskRunner)
        {
            this.log                  = log;
            this.backgroundTaskRunner = backgroundTaskRunner;
        }

        public bool TryAppend(IWorldSession session, string type, object payload)
        {
            try
            {
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
                string directory = Path.Combine(AppContext.BaseDirectory, "support-submissions");
                string path = Path.Combine(directory, $"{DateTime.UtcNow:yyyyMMdd}.jsonl");

                backgroundTaskRunner.Queue(() =>
                {
                    Directory.CreateDirectory(directory);
                    lock (writeLock)
                        File.AppendAllText(path, line + Environment.NewLine);

                    return Task.CompletedTask;
                }, exception => log.LogError(exception, "Failed to persist support submission {SubmissionType} for player {PlayerGuid}.",
                    type, session.Player?.Guid));

                return true;
            }
            catch (Exception exception)
            {
                log.LogError(exception, "Failed to queue support submission {SubmissionType} for player {PlayerGuid}.",
                    type, session.Player?.Guid);
                return false;
            }
        }
    }
}
