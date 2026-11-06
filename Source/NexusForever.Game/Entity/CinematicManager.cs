using NexusForever.Game.Abstract.Cinematic;
using NexusForever.Game.Cinematic;
using NexusForever.Game.Abstract.Cinematic.Cinematics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Network.Session;
using NexusForever.Shared.Game.Events;
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

        private readonly IPlayer owner;

        private sealed class Playback(ICinematicBase cinematic, IPlayer player)
        {
            internal readonly ICinematicBase Cinematic = cinematic;
            internal readonly IBaseMap Map = player.Map;
            internal readonly IPublicEventManager Events = player.Map?.PublicEventManager;
            private readonly IGameSession session = player.Session;
            private readonly EventQueue queue = player.Session?.Events;
            private readonly IAccount account = player.Account;
            private readonly ulong epoch = player.Account?.WorldSessionEpoch ?? 0;
            internal ushort? StartedId;

            internal bool Current(IPlayer owner) => ReferenceEquals(owner.Map, Map)
                && ReferenceEquals(Map?.PublicEventManager, Events)
                && ReferenceEquals(owner.Session, session) && ReferenceEquals(session?.Events, queue)
                && ReferenceEquals(owner.Account, account)
                && (account == null || account.WorldSessionEpoch == epoch && ReferenceEquals(account.Session, session))
                && session is not NetworkSession { IsDisconnecting: true }
                && (session is not IPlayerSession bound || ReferenceEquals(bound.Player, owner));
        }

        private Playback currentCinematic;
        private readonly Queue<Playback> queuedCinematics = new();
        private readonly HashSet<Type> queuedSurfaceStarterIntroTypesThisSession = [];
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
            if (TryGetSurfaceStarterIntroType(cinematic, out Type surfaceStarterIntroType))
            {
                if (!queuedSurfaceStarterIntroTypesThisSession.Add(surfaceStarterIntroType))
                {
                    log.Debug("Suppressing duplicate surface starter intro cinematic {CinematicType} for character {CharacterId} (guid {PlayerGuid}) during the same session.",
                        surfaceStarterIntroType.Name,
                        owner.CharacterId,
                        owner.Guid);
                    return;
                }
            }

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

            queuedCinematics.Enqueue(new Playback(cinematic, owner));
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

        private static bool TryGetSurfaceStarterIntroType(ICinematicBase cinematic, out Type cinematicType)
        {
            cinematicType = cinematic switch
            {
                INorthernWildsOnCreate => typeof(INorthernWildsOnCreate),
                ICrimsonIsleOnCreate => typeof(ICrimsonIsleOnCreate),
                IEverstarGroveOnCreate => typeof(IEverstarGroveOnCreate),
                ILevianBayOnCreate => typeof(ILevianBayOnCreate),
                _ => null
            };

            return cinematicType != null;
        }

        /// <summary>
        /// Play the next queued <see cref="ICinematicBase"/>.
        /// </summary>
        public void PlayQueuedCinematic()
        {
            if (currentCinematic != null)
                return;

            while (queuedCinematics.TryDequeue(out Playback playback))
            {
                if (!playback.Current(owner))
                    continue;

                currentCinematic = playback;
                try
                {
                    if (playback.Cinematic is CinematicBase scripted)
                        scripted.StartPlayback(owner, id => playback.StartedId = id,
                            () => ReferenceEquals(currentCinematic, playback) && playback.Current(owner));
                    else
                    {
                        // Preserve the legacy interface-only path; provided scripts
                        // derive from CinematicBase and use the guarded emitter.
                        playback.Cinematic.StartPlayback(owner);
                        playback.StartedId = playback.Cinematic.CinematicId;
                    }
                }
                catch
                {
                    if (ReferenceEquals(currentCinematic, playback))
                        currentCinematic = null;
                    throw;
                }
                if (ReferenceEquals(currentCinematic, playback) && !playback.StartedId.HasValue)
                {
                    // Setup revoked this frame before its first notification.
                    currentCinematic = null;
                    continue;
                }
                return;
            }
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
                    Playback finished = currentCinematic;
                    if (finished == null)
                        return;
                    currentCinematic = null;
                    try
                    {
                        if (finished.Current(owner))
                            finished.Events?.OnCinematicFinish(owner, finished.StartedId ?? finished.Cinematic.CinematicId);
                    }
                    finally
                    {
                        PlayQueuedCinematic();
                    }
                    break;
            }
        }
    }
}
