using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Shared.Game.Events;
using NLog;
using Row = NexusForever.Game.Entity.ExperiencePersistence.DatacubeState;
using WireDatacube = NexusForever.Network.World.Message.Model.Shared.Datacube;

namespace NexusForever.Game.Entity
{
    public partial class DatacubeManager
    {
        private sealed record Notice(Row Row, bool Volume);
        private static readonly ILogger publicationLog = LogManager.GetCurrentClassLogger();
        private readonly object publicationGate = new();
        private readonly Queue<Notice> pendingNotices = new();
        private readonly Queue<Notice> readyNotices = new();
        private Dictionary<uint, Row> confirmed, published;
        private Identity publicationIdentity;
        private IGameSession publicationSession;
        private bool publicationScheduled, sending, initialRequested;

        private void InitialisePublication()
        {
            publicationSession = player.Session;
            if (player.Identity != null)
                publicationIdentity = new() { Id = player.Identity.Id, RealmId = player.Identity.RealmId };
            confirmed = datacubes.ToDictionary(pair => pair.Key, pair => Capture(pair.Value));
            published = new(confirmed);
        }

        private bool OwnsPublication => player is Player original && publicationIdentity != null
            && original.OwnsDatacubePublication(this, publicationIdentity, publicationSession);

        private Row Capture(IDatacube datacube) => new(CollectionCharacter, (byte)datacube.Type, datacube.Id, datacube.Progress);
        private static uint Key(Row row) => DatacubeHash(row.Id, (DatacubeType)row.Type);
        private static WireDatacube Build(Row row) => new() { DatacubeId = row.Id, Progress = row.Progress };

        private void RetainNotice(IDatacube datacube, bool volume)
        {
            if (!OwnsPublication || !Owns(datacube)) return;
            lock (publicationGate)
            {
                pendingNotices.Enqueue(new(Capture(datacube), volume));
                ConfirmNotices();
            }
            NotifyPublicationReady();
        }

        private void ConfirmNotices()
        {
            // Creature activations add bits. Retain each accepted snapshot,
            // including duplicate interaction responses and later frozen intent.
            while (pendingNotices.TryPeek(out var notice)
                && confirmed.TryGetValue(Key(notice.Row), out var row)
                && row.Owner == notice.Row.Owner && (row.Progress & notice.Row.Progress) == notice.Row.Progress)
                readyNotices.Enqueue(pendingNotices.Dequeue());
        }

        internal void AcknowledgePublication(Row[] stored)
        {
            lock (publicationGate)
            {
                confirmed = stored.ToDictionary(Key);
                ConfirmNotices();
            }
        }

        private void SendConfirmedInitial()
        {
            if (!OwnsPublication) return;
            Row[] rows;
            lock (publicationGate)
            {
                if (sending) { initialRequested = true; return; }
                sending = true;
                initialRequested = false;
                rows = CaptureInitial();
            }
            try
            {
                if (OwnsPublication) SendInitialSnapshot(rows);
            }
            finally
            {
                lock (publicationGate) sending = false;
                NotifyPublicationReady();
            }
        }

        private Row[] CaptureInitial()
        {
            // Preserve earlier visible values only for rows whose accepted
            // notices still need to reach the client in order.
            var held = readyNotices.Concat(pendingNotices).Select(notice => Key(notice.Row)).ToHashSet();
            return confirmed.Select(pair => held.Contains(pair.Key) ? published.GetValueOrDefault(pair.Key) : pair.Value)
                .Where(row => row != null).ToArray();
        }

        private void SendInitialSnapshot(Row[] rows)
        {
            var packet = new ServerDatacubeUpdateList();
            foreach (var row in rows)
            {
                if ((DatacubeType)row.Type == DatacubeType.Datacube) packet.DatacubeData.Add(Build(row));
                else if ((DatacubeType)row.Type is DatacubeType.Chronicle or DatacubeType.Journal)
                    packet.DatacubeVolumeData.Add(Build(row));
            }
            publicationSession.EnqueueMessageEncrypted(packet);
            lock (publicationGate) published = rows.ToDictionary(Key);
        }

        internal void NotifyPublicationReady() => SchedulePublication(Task.CompletedTask);

        private void SchedulePublication(Task delay)
        {
            var events = publicationSession?.Events;
            if (events == null) return;
            lock (publicationGate)
            {
                if (publicationScheduled || sending || readyNotices.Count == 0 && !initialRequested) return;
                publicationScheduled = true;
            }
            bool CurrentActor() => OwnsPublication && ReferenceEquals(publicationSession.Events, events);
            bool CanSend()
            {
                if (!CurrentActor()) return true;
                lock (publicationGate) return !sending && delay.IsCompleted && player is Player { IsNormalSaveInProgress: false };
            }
            events.EnqueueThreadSafe(new PredicateEvent(CanSend, () => PublishCommitted(CurrentActor)));
        }

        private void PublishCommitted(Func<bool> currentActor)
        {
            bool retry = false;
            bool sendingInitial = false;
            lock (publicationGate) sending = true;
            try
            {
                while (currentActor())
                {
                    Notice notice;
                    Row[] initial = null;
                    lock (publicationGate)
                    {
                        if (!readyNotices.TryPeek(out notice))
                        {
                            if (!initialRequested) return;
                            initialRequested = false;
                            initial = CaptureInitial();
                        }
                    }
                    if (initial != null)
                    {
                        sendingInitial = true;
                        SendInitialSnapshot(initial);
                        sendingInitial = false;
                        continue;
                    }
                    publicationSession.EnqueueMessageEncrypted(notice.Volume
                        ? new ServerDatacubeVolumeUpdate { DatacubeVolumeData = Build(notice.Row) }
                        : new ServerDatacubeUpdate { DatacubeData = Build(notice.Row) });
                    lock (publicationGate)
                    {
                        readyNotices.Dequeue();
                        published[Key(notice.Row)] = notice.Row;
                    }
                }
                lock (publicationGate) { pendingNotices.Clear(); readyNotices.Clear(); initialRequested = false; }
            }
            catch (Exception error)
            {
                retry = true;
                publicationLog.Warn(error, "Committed datacube notice failed; retrying on the original actor.");
            }
            finally
            {
                lock (publicationGate)
                {
                    initialRequested |= sendingInitial;
                    publicationScheduled = false;
                    sending = false;
                }
                SchedulePublication(retry ? Task.Delay(TimeSpan.FromSeconds(1)) : Task.CompletedTask);
            }
        }
    }
}
