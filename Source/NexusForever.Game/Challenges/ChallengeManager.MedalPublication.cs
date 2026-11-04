using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Challenges;
using NexusForever.Game.Static.Entity;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Challenges;
using NexusForever.Shared.Game.Events;
using NLog;

namespace NexusForever.Game.Challenges
{
    public sealed partial class ChallengeManager
    {
        private sealed record MedalNotice(IGameSession Session, EventQueue Events, Func<bool> IsCurrent,
            ushort ChallengeId, byte Tier, ChallengeResult Result);

        private static readonly ILogger challengePublicationLog = LogManager.GetCurrentClassLogger();
        private readonly object medalNoticeGate = new();
        private readonly Queue<MedalNotice> medalNotices = new();
        private bool medalNoticeScheduled;

        private Action CaptureMedalNotices(ChallengeRuntimeState state, byte tier, bool complete)
        {
            var session = player.Session;
            var events = session?.Events;
            if (events == null) return null;
            var account = player.Account;
            var accountId = account?.Id;
            var realm = player.Identity?.RealmId;
            var challenge = (ushort)state.ChallengeId;
            var attempt = state.ActiveAttemptId;
            bool IsCurrent() => CharacterRewardsReady && player.CharacterId == characterId && player.Identity?.RealmId == realm
                && ReferenceEquals(player.ChallengeManager, this) && ReferenceEquals(player.Session, session)
                && ReferenceEquals(session.Events, events) && ReferenceEquals(player.Account, account)
                && account?.Id == accountId && ReferenceEquals(account?.Session, session)
                && (session is not IPlayerSession bound || ReferenceEquals(bound.Player, player))
                && session is not NetworkSession { IsDisconnecting: true }
                && player.LogoutManager?.State is not (LogoutState.Logout or LogoutState.Finished)
                && activeChallenges.TryGetValue(challenge, out var current) && current.ActiveAttemptId == attempt;

            return () =>
            {
                lock (medalNoticeGate)
                {
                    medalNotices.Enqueue(new(session, events, IsCurrent, challenge, tier, ChallengeResult.TierAchieved));
                    if (complete)
                        medalNotices.Enqueue(new(session, events, IsCurrent, challenge, tier, ChallengeResult.Completed));
                    ScheduleMedalNotices(Task.CompletedTask);
                }
            };
        }

        private void ScheduleMedalNotices(Task delay)
        {
            if (medalNoticeScheduled || !medalNotices.TryPeek(out var next)) return;
            medalNoticeScheduled = true;
            next.Events.EnqueueThreadSafe(new PredicateEvent(
                () => delay.IsCompleted && player is not Player { IsNormalSaveInProgress: true },
                () => PublishMedalNotices(next.Events)));
        }

        private void PublishMedalNotices(EventQueue events)
        {
            bool retry = false;
            try
            {
                while (true)
                {
                    MedalNotice notice;
                    lock (medalNoticeGate)
                        if (!medalNotices.TryPeek(out notice) || !ReferenceEquals(notice.Events, events)) return;
                    if (notice.IsCurrent())
                    {
                        try
                        {
                            notice.Session.EnqueueMessageEncrypted(new ServerChallengeResult
                                { ChallengeId = notice.ChallengeId, Result = notice.Result, Data = notice.Tier });
                        }
                        catch (Exception error)
                        {
                            challengePublicationLog.Warn(error, "Committed Challenge result failed; retrying on its original actor.");
                            retry = true;
                            return;
                        }
                    }
                    lock (medalNoticeGate) medalNotices.Dequeue();
                }
            }
            finally
            {
                lock (medalNoticeGate)
                {
                    medalNoticeScheduled = false;
                    ScheduleMedalNotices(retry ? Task.Delay(TimeSpan.FromSeconds(1)) : Task.CompletedTask);
                }
            }
        }
    }
}
