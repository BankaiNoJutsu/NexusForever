using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Service;

namespace NexusForever.WorldServer.Support
{
    internal sealed class FileSupportSubmissionStore : ISupportSubmissionStore
    {
        private static readonly object writeLock = new();
        private static readonly HashSet<string> damagedFiles = new();
        private static readonly JsonSerializerOptions jsonOptions = new()
        {
            WriteIndented = false
        };

        private readonly ILogger<FileSupportSubmissionStore> log;
        private readonly IBackgroundTaskRunner backgroundTaskRunner;
        private readonly string directory;
        private readonly Func<string, FileStream> openFile;

        public FileSupportSubmissionStore(
            ILogger<FileSupportSubmissionStore> log,
            IBackgroundTaskRunner backgroundTaskRunner)
            : this(log, backgroundTaskRunner, Path.Combine(AppContext.BaseDirectory, "support-submissions"))
        {
        }

        internal FileSupportSubmissionStore(ILogger<FileSupportSubmissionStore> log,
            IBackgroundTaskRunner backgroundTaskRunner, string directory, Func<string, FileStream> openFile = null)
        {
            this.log                  = log;
            this.backgroundTaskRunner = backgroundTaskRunner;
            this.directory            = Path.GetFullPath(directory);
            this.openFile             = openFile ?? (path => new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read));
        }

        public bool TryAppend(IWorldSession session, string type, object payload)
            => QueueAppend(session, type, payload, out _);

        public Task<bool> AppendAsync(IWorldSession session, string type, object payload)
        {
            QueueAppend(session, type, payload, out var completion);
            return completion;
        }

        private bool QueueAppend(IWorldSession session, string type, object payload, out Task<bool> completion)
        {
            var stored = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            completion = stored.Task;
            uint? playerGuid = session.Player?.Guid;
            void Failed(Exception error)
            {
                stored.TrySetResult(false);
                log.LogError(error, "Failed to persist support submission {SubmissionType} for player {PlayerGuid}.", type, playerGuid);
            }
            try
            {
                DateTime created = DateTime.UtcNow;
                var record = new
                {
                    Type = type,
                    CreatedUtc = created,
                    AccountId = session.Account?.Id,
                    PlayerGuid = session.Player?.Guid,
                    CharacterId = session.Player?.CharacterId,
                    CharacterName = session.Player?.Name,
                    Payload = payload
                };

                byte[] line = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(record, jsonOptions) + Environment.NewLine);
                string path = Path.Combine(directory, $"{created:yyyyMMdd}.jsonl");

                backgroundTaskRunner.Queue(() =>
                {
                    Directory.CreateDirectory(directory);
                    lock (writeLock)
                    {
                        if (damagedFiles.Contains(path)) throw new IOException("Support log requires repair before another submission can be accepted.");
                        using var file = openFile(path);
                        long previousLength = file.Length;
                        try
                        {
                            file.Write(line);
                            file.Flush(flushToDisk: true);
                        }
                        catch (Exception writeError)
                        {
                            // The exclusive writer owns only the new tail.
                            // Preserve all earlier records after a partial append.
                            try { file.SetLength(previousLength); file.Flush(flushToDisk: true); }
                            catch (Exception repairError)
                            {
                                damagedFiles.Add(path);
                                throw new AggregateException("Support log tail could not be repaired.", writeError, repairError);
                            }
                            throw;
                        }
                    }
                    stored.TrySetResult(true);
                    return Task.CompletedTask;
                }, Failed);

                return true;
            }
            catch (Exception exception)
            {
                Failed(exception);
                return false;
            }
        }
    }
}
