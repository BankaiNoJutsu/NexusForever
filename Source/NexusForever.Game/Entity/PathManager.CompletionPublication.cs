using NexusForever.Game.Abstract;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.PlayerPath;
using NexusForever.Shared.Game.Events;
using NLog;

namespace NexusForever.Game.Entity
{
    public partial class PathManager
    {
        private sealed class CompletionNotice(Mission mission)
        {
            internal Mission Mission { get; } = mission;
            internal bool AdvancedSent { get; set; }
        }

        private static readonly ILogger completionLog = LogManager.GetCurrentClassLogger();
        private readonly object completionGate = new();
        private readonly Queue<CompletionNotice> completionNotices = new();
        private HashSet<uint> confirmedCompletions = [];
        private Identity completionIdentity;
        private IGameSession completionSession;
        private bool completionScheduled;

        private void InitialiseMissionCompletionPublication()
        {
            completionSession = player.Session;
            if (player.Identity != null)
                completionIdentity = new() { Id = player.Identity.Id, RealmId = player.Identity.RealmId };
        }

        private bool OwnsCompletionPublication => player is Player original && completionIdentity != null
            && original.OwnsPathCompletionPublication(this, completionIdentity, completionSession);

        private void RetainMissionCompletion(Mission mission)
        {
            if (!OwnsCompletionPublication)
                return;

            lock (completionGate)
                completionNotices.Enqueue(new CompletionNotice(mission));
            player.RequestSave();
            NotifyMissionCompletionReady();
        }

        internal void AcknowledgeMissionCompletions(ExperiencePersistence.Mission[] missions)
        {
            lock (completionGate)
                confirmedCompletions = missions.Where(row => row.Completed != 0).Select(row => (uint)row.Id).ToHashSet();
        }

        internal void NotifyMissionCompletionReady() => ScheduleMissionCompletion(Task.CompletedTask);

        private bool TryGetConfirmedCompletion(out CompletionNotice notice)
            => completionNotices.TryPeek(out notice) && confirmedCompletions.Contains(notice.Mission.PathMissionId);

        private void ScheduleMissionCompletion(Task delay)
        {
            var events = completionSession?.Events;
            if (events == null)
                return;

            lock (completionGate)
            {
                if (completionScheduled || !TryGetConfirmedCompletion(out _))
                    return;
                completionScheduled = true;
            }

            bool CurrentActor() => OwnsCompletionPublication && ReferenceEquals(completionSession.Events, events);
            events.EnqueueThreadSafe(new PredicateEvent(
                () => !CurrentActor() || (delay.IsCompleted && player is Player { IsNormalSaveInProgress: false }),
                () => PublishMissionCompletions(CurrentActor)));
        }

        private void PublishMissionCompletions(Func<bool> currentActor)
        {
            bool retry = false;
            try
            {
                while (currentActor())
                {
                    CompletionNotice notice;
                    lock (completionGate)
                    {
                        if (!TryGetConfirmedCompletion(out notice))
                            return;
                    }

                    if (!notice.AdvancedSent)
                    {
                        completionSession.EnqueueMessageEncrypted(new ServerPathMissionAdvanced
                        {
                            PathMissionId = (ushort)notice.Mission.PathMissionId
                        });
                        notice.AdvancedSent = true;
                        continue;
                    }

                    completionSession.EnqueueMessageEncrypted(new ServerPathMissionUpdate { Mission = notice.Mission });
                    lock (completionGate)
                        completionNotices.Dequeue();
                }

                lock (completionGate)
                    completionNotices.Clear();
            }
            catch (Exception error)
            {
                retry = true;
                completionLog.Warn(error, "Committed path mission completion enqueue failed; retrying on the original actor.");
            }
            finally
            {
                lock (completionGate)
                    completionScheduled = false;
                ScheduleMissionCompletion(retry ? Task.Delay(TimeSpan.FromSeconds(1)) : Task.CompletedTask);
            }
        }
    }
}
