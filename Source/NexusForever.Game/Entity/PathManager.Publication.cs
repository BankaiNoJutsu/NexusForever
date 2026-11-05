using NexusForever.Game.Abstract;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.PlayerPath;
using NexusForever.Shared.Game.Events;
using NLog;
using Path = NexusForever.Game.Static.PlayerPath.Path;

namespace NexusForever.Game.Entity
{
    public partial class PathManager
    {
        private sealed class CompletionNotice(Mission mission)
        {
            internal Mission Mission { get; } = mission;
            internal bool AdvancedSent { get; set; }
        }

        private static readonly ILogger publicationLog = LogManager.GetCurrentClassLogger();
        private readonly object publicationGate = new();
        private readonly Queue<CompletionNotice> completionNotices = new();
        private HashSet<uint> confirmedCompletions = [];
        private Dictionary<Path, uint> confirmedXp;
        private Dictionary<Path, uint> publishedXp;
        private Path displayedPath;
        private int pathLogDepth;
        private uint pathLogRevision;
        private Identity publicationIdentity;
        private IGameSession publicationSession;
        private bool publicationScheduled;

        private void InitialisePathPublication()
        {
            publicationSession = player.Session;
            if (player.Identity != null)
                publicationIdentity = new() { Id = player.Identity.Id, RealmId = player.Identity.RealmId };
            confirmedXp = LoadedPaths.ToDictionary(row => (Path)row.Id, row => row.Xp);
            publishedXp = new(confirmedXp);
            displayedPath = player.Path;
        }

        private bool OwnsPathPublication => player is Player original && publicationIdentity != null
            && original.OwnsPathPublication(this, publicationIdentity, publicationSession);

        private void RetainMissionCompletion(Mission mission)
        {
            if (!OwnsPathPublication)
                return;

            lock (publicationGate)
                completionNotices.Enqueue(new CompletionNotice(mission));
            player.RequestSave();
            NotifyPathPublicationReady();
        }

        internal void AcknowledgePathPublication(ExperiencePersistence.PathState[] savedPaths, ExperiencePersistence.Mission[] missions)
        {
            lock (publicationGate)
            {
                confirmedXp = savedPaths.ToDictionary(row => (Path)row.Id, row => row.Xp);
                confirmedCompletions = missions.Where(row => row.Completed != 0).Select(row => (uint)row.Id).ToHashSet();
            }
        }

        private void SendConfirmedPathLog(ServerPathInitialise packet)
        {
            Dictionary<Path, uint> totals;
            uint revision;
            lock (publicationGate)
            {
                totals = confirmedXp;
                revision = ++pathLogRevision;
                pathLogDepth++;
            }

            try
            {
                for (int index = 0; index < packet.PathProgress.Length; index++)
                    packet.PathProgress[index] = totals.GetValueOrDefault((Path)index);

                player.Session.EnqueueMessageEncrypted(packet);
                lock (publicationGate)
                {
                    if (revision == pathLogRevision)
                    {
                        displayedPath = packet.ActivePath;
                        publishedXp = new(totals);
                    }
                }
            }
            finally
            {
                lock (publicationGate)
                    pathLogDepth--;
                NotifyPathPublicationReady();
            }
        }

        internal void NotifyPathPublicationReady() => SchedulePathPublication(Task.CompletedTask);

        private bool TryGetConfirmedCompletion(out CompletionNotice notice)
            => completionNotices.TryPeek(out notice) && confirmedCompletions.Contains(notice.Mission.PathMissionId);

        // The incremental packet has no Path ID. Use the full Path log on
        // selection changes, then publish committed totals for that Path only.
        private bool TryGetConfirmedXp(Path activePath, out uint xp)
        {
            xp = confirmedXp.GetValueOrDefault(activePath);
            return activePath == displayedPath && xp != publishedXp.GetValueOrDefault(activePath);
        }

        private void SchedulePathPublication(Task delay)
        {
            var events = publicationSession?.Events;
            if (events == null)
                return;

            lock (publicationGate)
            {
                if (publicationScheduled || (!TryGetConfirmedCompletion(out _) && !TryGetConfirmedXp(player.Path, out _)))
                    return;
                publicationScheduled = true;
            }

            bool CurrentActor() => OwnsPathPublication && ReferenceEquals(publicationSession.Events, events);
            bool CanSend()
            {
                if (!CurrentActor())
                    return true;
                lock (publicationGate)
                    return pathLogDepth == 0 && delay.IsCompleted && player is Player { IsNormalSaveInProgress: false };
            }
            events.EnqueueThreadSafe(new PredicateEvent(CanSend, () => PublishPathProgress(CurrentActor)));
        }

        private void PublishPathProgress(Func<bool> currentActor)
        {
            bool retry = false;
            try
            {
                while (currentActor())
                {
                    CompletionNotice notice;
                    Path activePath = player.Path;
                    uint xp = 0;
                    uint revision;
                    bool hasCompletion;
                    lock (publicationGate)
                    {
                        hasCompletion = TryGetConfirmedCompletion(out notice);
                        if (!hasCompletion && !TryGetConfirmedXp(activePath, out xp))
                            return;
                        revision = pathLogRevision;
                    }

                    if (!hasCompletion)
                    {
                        publicationSession.EnqueueMessageEncrypted(new ServerPathUpdateXP { TotalXP = xp });
                        lock (publicationGate)
                        {
                            if (revision == pathLogRevision)
                                publishedXp[activePath] = xp;
                        }
                        continue;
                    }

                    if (!notice.AdvancedSent)
                    {
                        publicationSession.EnqueueMessageEncrypted(new ServerPathMissionAdvanced
                        {
                            PathMissionId = (ushort)notice.Mission.PathMissionId
                        });
                        notice.AdvancedSent = true;
                        continue;
                    }

                    publicationSession.EnqueueMessageEncrypted(new ServerPathMissionUpdate { Mission = notice.Mission });
                    lock (publicationGate)
                        completionNotices.Dequeue();
                }

                lock (publicationGate)
                {
                    completionNotices.Clear();
                    publishedXp = new(confirmedXp);
                }
            }
            catch (Exception error)
            {
                retry = true;
                publicationLog.Warn(error, "Committed Path progress enqueue failed; retrying on the original actor.");
            }
            finally
            {
                lock (publicationGate)
                    publicationScheduled = false;
                SchedulePathPublication(retry ? Task.Delay(TimeSpan.FromSeconds(1)) : Task.CompletedTask);
            }
        }
    }
}
