using NexusForever.Game.Abstract;
using NexusForever.Game.Entity;
using NexusForever.Network.Session;
using NexusForever.Shared.Game.Events;
using NLog;
using Row = NexusForever.Game.Entity.ExperiencePersistence.ExploredHexgroup;

namespace NexusForever.Game.Map
{
    public partial class ZoneMapManager
    {
        private static readonly ILogger publicationLog = LogManager.GetCurrentClassLogger();
        private readonly object publicationGate = new();
        private readonly HashSet<ushort> pendingMaps = [];
        private Dictionary<ushort, ushort[]> confirmedGroups;
        private Identity publicationIdentity;
        private IGameSession publicationSession;
        private bool publicationScheduled, sending;

        private void InitialisePublication()
        {
            publicationSession = player.Session;
            if (player.Identity != null)
                publicationIdentity = new() { Id = player.Identity.Id, RealmId = player.Identity.RealmId };
            confirmedGroups = GroupRows(LoadedExploration);
        }

        private bool OwnsPublication => publicationIdentity != null && player.OwnsExplorationPublication(this, publicationIdentity, publicationSession);

        private static Dictionary<ushort, ushort[]> GroupRows(Row[] rows) => rows.GroupBy(row => row.Zone)
            .ToDictionary(group => group.Key, group => group.Select(row => row.Group).Order().ToArray());

        internal void AcknowledgePublication(Row[] stored)
        {
            lock (publicationGate)
                foreach (var (map, groups) in GroupRows(stored))
                {
                    if (confirmedGroups.TryGetValue(map, out var previous) && previous.SequenceEqual(groups)) continue;
                    confirmedGroups[map] = groups;
                    pendingMaps.Add(map);
                }
        }

        internal void SendConfirmedMap(ZoneMap map)
        {
            if (!OwnsPublication || !Owns(map)) return;
            lock (publicationGate) pendingMaps.Add(map.MapId);
            PublishCommitted(() => OwnsPublication);
        }

        internal void NotifyPublicationReady() => SchedulePublication(Task.CompletedTask);

        private void SchedulePublication(Task delay)
        {
            var events = publicationSession?.Events;
            if (events == null) return;
            lock (publicationGate)
            {
                if (publicationScheduled || sending || pendingMaps.Count == 0) return;
                publicationScheduled = true;
            }
            bool CurrentActor() => OwnsPublication && ReferenceEquals(publicationSession.Events, events);
            bool CanSend()
            {
                lock (publicationGate)
                    if (sending) return false;
                return !CurrentActor() || delay.IsCompleted && !player.IsNormalSaveInProgress;
            }
            events.EnqueueThreadSafe(new PredicateEvent(CanSend, () =>
            {
                lock (publicationGate) publicationScheduled = false;
                PublishCommitted(CurrentActor);
            }));
        }

        private void PublishCommitted(Func<bool> currentActor)
        {
            ushort[] maps;
            lock (publicationGate)
            {
                if (sending) return;
                sending = true;
                maps = pendingMaps.Order().ToArray();
            }
            bool retry = false;
            try
            {
                foreach (ushort id in maps)
                {
                    if (!currentActor())
                    {
                        lock (publicationGate) pendingMaps.Clear();
                        return;
                    }
                    ushort[] groups;
                    lock (publicationGate) groups = GetGroups(id);
                    if (zoneMaps.TryGetValue(id, out var current) && current is ZoneMap map)
                        publicationSession.EnqueueMessageEncrypted(map.BuildConfirmed(groups));
                    lock (publicationGate)
                        if (ReferenceEquals(groups, GetGroups(id))) pendingMaps.Remove(id);
                }
            }
            catch (Exception error)
            {
                retry = true;
                publicationLog.Warn(error, "Committed exploration mask failed; retrying on the original actor.");
            }
            finally
            {
                lock (publicationGate) sending = false;
                SchedulePublication(retry ? Task.Delay(TimeSpan.FromSeconds(1)) : Task.CompletedTask);
            }
        }

        private ushort[] GetGroups(ushort id) => confirmedGroups.GetValueOrDefault(id) ?? Array.Empty<ushort>();
    }
}
