using NexusForever.Game.Abstract.Cinematic;
using NexusForever.Game.Abstract.Cinematic.Cinematics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Raid.InitializationCoreY83
{
    [ScriptFilterOwnerId(595)]
    public class InitializationCoreY83EventScript : IPublicEventScript, IOwnedScript<IPublicEvent>
    {
        private readonly IGlobalQuestManager globalQuestManager;
        private readonly ICinematicFactory cinematicFactory;

        private IPublicEvent publicEvent;
        private IMapInstance mapInstance;

        public InitializationCoreY83EventScript(
            IGlobalQuestManager globalQuestManager,
            ICinematicFactory cinematicFactory)
        {
            this.globalQuestManager = globalQuestManager;
            this.cinematicFactory   = cinematicFactory;
        }

        /// <summary>
        /// Invoked when <see cref="IScript"/> is loaded.
        /// </summary>
        public void OnLoad(IPublicEvent owner)
        {
            publicEvent = owner;
            mapInstance = publicEvent.Map as IMapInstance
                ?? throw new InvalidOperationException("Initialization Core Y-83 requires a map instance.");

            publicEvent.SetPhase(PublicEventPhase.Enter);
        }

        /// <summary>
        /// Invoked when the public event phase changes.
        /// </summary>
        public void OnPublicEventPhase(uint phase)
        {
            switch ((PublicEventPhase)phase)
            {
                case PublicEventPhase.Enter:
                    publicEvent.ActivateObjective(PublicEventObjective.UnlockTheQuarantineDoor);
                    break;
                case PublicEventPhase.OpenDoor:
                    publicEvent.ActivateObjective(PublicEventObjective.OpenTheQuarantineDoor);
                    // WIP-guessed from LaughingWS Instances-and-more: branch broadcasts Nurton2
                    // during the OpenDoor phase. Door entity choreography and exact timing remain blocked.
                    BroadcastCommunicatorMessage(CommunicatorMessage.Nurton2);
                    break;
                case PublicEventPhase.Boss:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatThePrimeEvolutionaryOperants);
                    publicEvent.ActivateObjective(PublicEventObjective.ImmortalPrimeEvolutionaryOperants);
                    publicEvent.ActivateObjective(PublicEventObjective.EvolutionaryRevolutions);
                    QueueWipGuessedCinematic<IInitializationCoreY83OpenDoor>();
                    break;
            }
        }

        /// <summary>
        /// Invoked when the <see cref="IPublicEventObjective"/> status changes.
        /// </summary>
        public void OnPublicEventObjectiveStatus(IPublicEventObjective objective)
        {
            if (objective.Status != PublicEventStatus.Succeeded)
                return;

            switch ((PublicEventObjective)objective.Entry.Id)
            {
                case PublicEventObjective.KillThePhagetouchedFreebots:
                case PublicEventObjective.UnlockTheQuarantineDoor:
                    publicEvent.SetPhase(PublicEventPhase.OpenDoor);
                    break;
                case PublicEventObjective.OpenTheQuarantineDoor:
                    publicEvent.SetPhase(PublicEventPhase.Boss);
                    break;
                case PublicEventObjective.DefeatThePrimeEvolutionaryOperants:
                    // WIP-guessed from LaughingWS Instances-and-more: branch broadcasts Nurton3
                    // when the final boss objective succeeds, before finishing the public event.
                    BroadcastCommunicatorMessage(CommunicatorMessage.Nurton3);
                    publicEvent.Finish(PublicEventTeam.PublicTeam);
                    break;
            }
        }

        private void BroadcastCommunicatorMessage(CommunicatorMessage message)
        {
            ICommunicatorMessage communicatorMessage = globalQuestManager.GetCommunicatorMessage(message);
            foreach (IPlayer player in mapInstance.GetPlayers())
                communicatorMessage?.Send(player.Session);
        }

        private void QueueWipGuessedCinematic<T>() where T : ICinematicBase
        {
            // WIP-guessed from LaughingWS Instances-and-more: the branch queues this
            // door cinematic at boss phase entry, but exact door choreography and
            // cinematic payload remain blocked pending proof.
            foreach (IPlayer player in mapInstance.GetPlayers())
                player.CinematicManager.QueueCinematic(cinematicFactory.CreateCinematic<T>());
        }
    }
}
