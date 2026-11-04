using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Reputation;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Reputation;
using NexusForever.Shared.Game.Events;
using NLog;
using Row = NexusForever.Game.Entity.ExperiencePersistence.ReputationState;
using WireReputation = NexusForever.Network.World.Message.Model.ServerPlayerCreate.Faction.FactionReputation;

namespace NexusForever.Game.Reputation
{
    public partial class ReputationManager
    {
        private sealed record Notice(long Sequence, Faction Faction, float Delta, float? Before);
        private static readonly ILogger publicationLog = LogManager.GetCurrentClassLogger();
        private readonly object publicationGate = new();
        private readonly Queue<Notice> pending = new();
        private readonly Queue<Notice> ready = new();
        private Dictionary<Faction, float> confirmed;
        private Identity publicationIdentity;
        private IGameSession publicationSession;
        private long sequence;
        private bool sending, scheduled;

        private void InitialisePublication()
        {
            publicationSession = owner.Session;
            if (owner.Identity != null)
                publicationIdentity = new() { Id = owner.Identity.Id, RealmId = owner.Identity.RealmId };
            confirmed = LoadedReputations.ToDictionary(row => (Faction)row.Faction, row => row.Amount);
        }

        private bool OwnsPublication => owner is Player original && publicationIdentity != null
            && original.OwnsReputationPublication(this, publicationIdentity, publicationSession);

        internal void AcknowledgePublication(Row[] rows, long through)
        {
            lock (publicationGate)
            {
                confirmed = rows.ToDictionary(row => (Faction)row.Faction, row => row.Amount);
                while (pending.TryPeek(out var notice) && notice.Sequence <= through)
                    ready.Enqueue(pending.Dequeue());
            }
        }

        internal void SendInitialStanding(Action<List<WireReputation>> send)
        {
            if (owner is not IPlayerMapAdmission admission) return;
            // Keep map admission synchronous. Publication shares its lock
            // and must also yield to reentrant event ticks during the burst.
            lock (admission.MapAdmissionSyncRoot)
            {
                if (!OwnsPublication) return;
                bool wasSending;
                lock (publicationGate)
                {
                    wasSending = sending;
                    sending = true;
                }
                try { SendInitial(send); }
                finally
                {
                    lock (publicationGate) sending = wasSending;
                    NotifyPublicationReady();
                }
            }
        }

        private void SendInitial(Action<List<WireReputation>> send)
        {
            Dictionary<Faction, float> snapshot;
            lock (publicationGate)
            {
                snapshot = new(confirmed);
                // An initial snapshot is the base on which queued deltas
                // apply. An unrelated quiet write can be included immediately.
                foreach (var first in ready.DistinctBy(notice => notice.Faction))
                {
                    if (first.Before.HasValue) snapshot[first.Faction] = first.Before.Value;
                    else snapshot.Remove(first.Faction);
                }
            }
            send(snapshot.OrderBy(pair => pair.Key).Select(pair => new WireReputation
                { FactionId = pair.Key, ReputationAmount = pair.Value }).ToList());
        }

        internal void NotifyPublicationReady() => Schedule(Task.CompletedTask);

        private void Schedule(Task delay)
        {
            var events = publicationSession?.Events;
            if (events == null || owner is not IPlayerMapAdmission admission) return;
            lock (publicationGate)
            {
                if (sending || scheduled || ready.Count == 0) return;
                scheduled = true;
            }
            bool CurrentActor() => OwnsPublication && ReferenceEquals(publicationSession.Events, events);
            bool CanSend()
            {
                lock (publicationGate)
                    if (sending) return false;
                return !CurrentActor() || delay.IsCompleted && owner is Player { IsNormalSaveInProgress: false };
            }
            events.EnqueueThreadSafe(new PredicateEvent(CanSend, () =>
            {
                lock (admission.MapAdmissionSyncRoot)
                    PublishCommitted(CurrentActor);
            }));
        }

        private void PublishCommitted(Func<bool> currentActor)
        {
            bool retry = false;
            lock (publicationGate) sending = true;
            try
            {
                while (currentActor())
                {
                    Notice notice;
                    lock (publicationGate)
                        if (!ready.TryPeek(out notice)) return;
                    publicationSession.EnqueueMessageEncrypted(new ServerReputationUpdate
                        { FactionId = notice.Faction, ReputationDelta = notice.Delta });
                    lock (publicationGate) ready.Dequeue();
                }
                lock (publicationGate) { ready.Clear(); pending.Clear(); }
            }
            catch (Exception error)
            {
                retry = true;
                publicationLog.Warn(error, "Reputation publication failed on the original actor.");
            }
            finally
            {
                lock (publicationGate) { scheduled = false; sending = false; }
                Schedule(retry ? Task.Delay(TimeSpan.FromSeconds(1)) : Task.CompletedTask);
            }
        }
    }
}
