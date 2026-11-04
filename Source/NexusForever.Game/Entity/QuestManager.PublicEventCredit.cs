using System.Threading;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Quest;
using NexusForever.Network;
using NexusForever.Network.Session;
using NexusForever.Shared.Game.Events;

namespace NexusForever.Game.Entity
{
    public partial class QuestManager
    {
        private readonly List<PublicEventPublication> publicEventPublications = [];
        private List<Action> capturingPublicEventPublications;
        private sealed record PublicEventPublication(Func<bool> OwnsSession, EventQueue Events, List<Action> Notices);
        internal bool IsPublicEventQuestMutationInProgress =>
            Monitor.IsEntered(questRewardGate) && capturingPublicEventPublications != null;

        internal void CreditPublicEventQuestObjective(uint objectiveId, IReadOnlyList<QuestObjectiveType> types)
        {
            lock (questRewardGate)
            {
                // A synchronous quest script can complete another event for this player.
                if (capturingPublicEventPublications != null)
                {
                    ApplyCredit();
                    return;
                }

                var session = player.Session;
                var events = session?.Events;
                var account = player.Account;
                ulong? epoch = account?.WorldSessionEpoch;
                ulong character = player.CharacterId;
                List<Action> notices = [];
                capturingPublicEventPublications = notices;
                try { ApplyCredit(); }
                finally
                {
                    capturingPublicEventPublications = null;
                    if (notices.Count != 0 && events != null)
                        publicEventPublications.Add(new(OwnsSession, events, notices));
                }

                bool OwnsSession() => character == player.CharacterId && ReferenceEquals(player.QuestManager, this)
                    && ReferenceEquals(player.Session, session) && ReferenceEquals(session.Events, events)
                    && ReferenceEquals(player.Account, account) && account?.WorldSessionEpoch == epoch
                    && session is not NetworkSession { IsDisconnecting: true }
                    && (session is not IPlayerSession bound || ReferenceEquals(bound.Player, player));
            }

            void ApplyCredit()
            {
                foreach (QuestObjectiveType type in types)
                    ObjectiveUpdate(type, objectiveId, 1u);
            }
        }

        internal bool RetainPublicEventQuestNotice(Action notice)
        {
            if (!IsPublicEventQuestMutationInProgress)
                return false;
            capturingPublicEventPublications.Add(notice);
            return true;
        }

        private Action<CharacterContext> CapturePublicEventPublications(out bool hasCurrentCredit)
        {
            PublicEventPublication[] pending;
            lock (questRewardGate)
            {
                if (publicEventPublications.Count == 0)
                {
                    hasCurrentCredit = false;
                    return null;
                }
                pending = publicEventPublications.ToArray();
            }
            hasCurrentCredit = pending.Any(publication => publication.OwnsSession());

            return context =>
            {
                bool flushed = false;
                void ObserveFlush(object _, SavedChangesEventArgs args)
                    => flushed |= args.EntitiesSavedCount > 0;
                context.SavedChanges += ObserveFlush;
                context.OnSaveSettled(committed =>
                {
                    context.SavedChanges -= ObserveFlush;
                    if (!committed || !flushed)
                        return;
                    foreach (var publication in pending)
                    {
                        lock (questRewardGate)
                            if (!publicEventPublications.Remove(publication))
                                continue;
                        publication.Events.EnqueueThreadSafe(new PredicateEvent(
                            () => !publication.OwnsSession() || player is not Player { IsNormalSaveInProgress: true },
                            () => PublishPublicEventQuestNotices(publication)));
                    }
                });
            };
        }

        private void PublishPublicEventQuestNotices(PublicEventPublication publication)
        {
            foreach (var notice in publication.Notices)
            {
                if (!publication.OwnsSession() || RequiresAuthoritativeReload)
                    return;
                try { notice(); }
                catch (Exception error) { log.Error(error, "Failed to publish committed public event quest progress."); }
            }
        }
    }
}
