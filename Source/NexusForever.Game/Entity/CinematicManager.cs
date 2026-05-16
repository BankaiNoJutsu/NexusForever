using NexusForever.Game.Abstract.Cinematic;
using NexusForever.Game.Abstract.Cinematic.Cinematics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Cinematic;
using NexusForever.Game.Static.Quest;
using NLog;

namespace NexusForever.Game.Entity
{
    public class CinematicManager : ICinematicManager
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();
        private static readonly ushort[] riderReefStarterTutorialQuestIds = [10513, 10521, 10527, 10532];
        private static readonly ushort[] riderReefFollowUpTutorialQuestIds = [10518, 10519, 10520, 10522, 10523, 10524, 10525, 10526, 10528, 10530, 10540, 10541];

        private IPlayer owner;

        private ICinematicBase currentCinematic;
        private readonly Queue<ICinematicBase> queuedCinematics = new();
        private bool riderReefIntroQueuedThisSession;

        /// <summary>
        /// Initialise a <see cref="ICinematicManager"/> for this <see cref="IPlayer"/>.
        /// </summary>
        public CinematicManager(IPlayer player)
        {
            owner = player;
        }

        /// <summary>
        /// Queue a <see cref="ICinematicBase"/> to be played.
        /// </summary>
        public void QueueCinematic(ICinematicBase cinematic)
        {
            if (cinematic is INoviceTutorialOnEnter)
            {
                if (ShouldSuppressRidersReefIntroCinematic())
                {
                    log.Debug("Suppressing Rider's Reef intro cinematic for character {CharacterId} (guid {PlayerGuid}) due to existing tutorial progress or prior session playback.",
                        owner.CharacterId,
                        owner.Guid);
                    return;
                }

                riderReefIntroQueuedThisSession = true;
            }

            queuedCinematics.Enqueue(cinematic);
            if (currentCinematic == null && queuedCinematics.Count >= 1u)
                PlayQueuedCinematic();
        }

        private bool ShouldSuppressRidersReefIntroCinematic()
        {
            if (riderReefIntroQueuedThisSession)
                return true;

            if (owner?.QuestManager == null)
                return false;

            if (riderReefFollowUpTutorialQuestIds.Any(questId => owner.QuestManager.GetQuestState(questId) != null))
                return true;

            foreach (ushort questId in riderReefStarterTutorialQuestIds)
            {
                QuestState? questState = owner.QuestManager.GetQuestState(questId);
                if (questState is null)
                    continue;

                if (questState != QuestState.Accepted)
                    return true;
            }

            return owner.QuestManager.GetActiveQuests()
                .Where(q => riderReefStarterTutorialQuestIds.Contains(q.Id))
                .Any(q => q.Any(o => o.Progress > 0u));
        }

        /// <summary>
        /// Play the next queued <see cref="ICinematicBase"/>.
        /// </summary>
        public void PlayQueuedCinematic()
        {
            if (queuedCinematics.Count == 0)
                return;

            ICinematicBase cinematic = queuedCinematics.Dequeue();
            currentCinematic = cinematic;
            currentCinematic.StartPlayback(owner);
        }

        /// <summary>
        /// Handle the <see cref="CinematicState"/> the Client sent back. Only to be called from Client Handlers.
        /// </summary>
        public void HandleClientCinematicState(CinematicState cinematicState)
        {
            switch (cinematicState)
            {
                case CinematicState.Initalising:
                    // Cast Spell: Generic - Cinematic Player State - Tier 1 (Spell4 ID: 49887)
                    // Make the Player invisible/immune from aggro
                    break;
                case CinematicState.Finishing:
                    // End Cinematic Player State spell
                    // Make the Player visible/remove immunity
                    break;
                case CinematicState.Ended:
                    // Player is back in the world. Continue any scripts that may've been paused.
                    owner.Map?.PublicEventManager.OnCinematicFinish(owner, currentCinematic.CinematicId);

                    currentCinematic = null;
                    PlayQueuedCinematic();
                    break;
            }
        }
    }
}
