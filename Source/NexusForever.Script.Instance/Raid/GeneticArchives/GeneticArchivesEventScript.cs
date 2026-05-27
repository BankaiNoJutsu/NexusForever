using NexusForever.Game.Abstract.Cinematic;
using NexusForever.Game.Abstract.Cinematic.Cinematics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Raid.GeneticArchives
{
    [ScriptFilterOwnerId(159)]
    public class GeneticArchivesEventScript : IPublicEventScript, IOwnedScript<IPublicEvent>
    {
        private readonly IGlobalQuestManager globalQuestManager;
        private readonly ICinematicFactory cinematicFactory;

        private IPublicEvent publicEvent;
        private IMapInstance mapInstance;

        public GeneticArchivesEventScript(
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
                ?? throw new InvalidOperationException("Genetic Archives requires a map instance.");

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
                    BroadcastWipCommunicatorMessage(CommunicatorMessage.TheDreadphageOhmna1);
                    break;
                case PublicEventPhase.GetToNextLevel:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatTheFetidMiscreation);
                    break;
                case PublicEventPhase.SecondFloor:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatPhagetechGuardianC148);
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatPhagetechGuardianC432);
                    break;
                case PublicEventPhase.ArchiveDefenseSystem:
                    publicEvent.ActivateObjective(PublicEventObjective.GetThroughTheArchiveDefenseSystem);
                    publicEvent.ActivateObjective(PublicEventObjective.DeathFromAbove);
                    break;
                case PublicEventPhase.PhagebornConvergence:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatThePhagebornConvergence);
                    break;
                case PublicEventPhase.Minibosses:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatTheMalfunctioningPiston);
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatTheMalfunctioningBattery);
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatTheMalfunctioningGear);
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatTheMalfunctioningDynamo);
                    break;
                case PublicEventPhase.Ohmna:
                    QueueWipGuessedCinematic<IGeneticArchivesOpenOhmnaDoor>();
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatDreadphageOhmna);
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
                case PublicEventObjective.DefeatExperimentX89:
                case PublicEventObjective.DefeatKuralakTheDefiler:
                    publicEvent.SetPhase(PublicEventPhase.GetToNextLevel);
                    break;
                case PublicEventObjective.DefeatTheFetidMiscreation:
                    publicEvent.SetPhase(PublicEventPhase.SecondFloor);
                    break;
                case PublicEventObjective.DefeatPhagetechGuardianC148:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatConstructsInTheCentrifuge);
                    break;
                case PublicEventObjective.DefeatConstructsInTheCentrifuge:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatPhageMaw);
                    break;
                case PublicEventObjective.DefeatPhagetechGuardianC432:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatTheParagonsOfSymbiosis);
                    break;
                case PublicEventObjective.DefeatTheParagonsOfSymbiosis:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatThePhagetechPrototypes);
                    break;
                case PublicEventObjective.DefeatPhageMaw:
                case PublicEventObjective.DefeatThePhagetechPrototypes:
                    publicEvent.SetPhase(PublicEventPhase.ArchiveDefenseSystem);
                    break;
                case PublicEventObjective.GetThroughTheArchiveDefenseSystem:
                    publicEvent.SetPhase(PublicEventPhase.PhagebornConvergence);
                    break;
                case PublicEventObjective.DefeatThePhagebornConvergence:
                    publicEvent.SetPhase(PublicEventPhase.Minibosses);
                    break;
                case PublicEventObjective.DefeatTheMalfunctioningGear:
                case PublicEventObjective.DefeatTheMalfunctioningPiston:
                case PublicEventObjective.DefeatTheMalfunctioningDynamo:
                case PublicEventObjective.DefeatTheMalfunctioningBattery:
                    publicEvent.SetPhase(PublicEventPhase.Ohmna);
                    break;
                case PublicEventObjective.DefeatDreadphageOhmna:
                    publicEvent.Finish(PublicEventTeam.PublicTeam);
                    break;
            }
        }

        /// <summary>
        /// Invoked when a cinematic for <see cref="IPlayer"/> has finished.
        /// </summary>
        public void OnCinematicFinish(IPlayer player, uint cinematicId)
        {
            switch ((PublicEventPhase)publicEvent.Phase)
            {
                case PublicEventPhase.Enter:
                    SendWipCommunicatorMessage(player, CommunicatorMessage.TheDreadphageOhmna1);
                    break;
                case PublicEventPhase.Ohmna:
                    SendWipCommunicatorMessage(player, CommunicatorMessage.TheDreadphageOhmna8);
                    break;
            }
        }

        private void BroadcastWipCommunicatorMessage(CommunicatorMessage message)
        {
            // WIP-guessed from LaughingWS Instances-and-more. The branch maps these Ohmna
            // communicator ids to phase/cinematic handoffs, but exact cinematic id gating,
            // door/elevator choreography, and encounter timing remain blocked pending proof.
            ICommunicatorMessage communicatorMessage = globalQuestManager.GetCommunicatorMessage(message);
            foreach (IPlayer player in mapInstance.GetPlayers())
                communicatorMessage?.Send(player.Session);
        }

        private void SendWipCommunicatorMessage(IPlayer player, CommunicatorMessage message)
        {
            // WIP-guessed from LaughingWS Instances-and-more; keep this phase-gated until
            // the exact retail cinematic trigger and replay timing are proven.
            ICommunicatorMessage communicatorMessage = globalQuestManager.GetCommunicatorMessage(message);
            communicatorMessage?.Send(player.Session);
        }

        private void QueueWipGuessedCinematic<T>() where T : ICinematicBase
        {
            // WIP-guessed from LaughingWS Instances-and-more. The branch queues this
            // final Ohmna-door cinematic, but exact door/elevator choreography and
            // real cinematic payload remain blocked pending proof.
            foreach (IPlayer player in mapInstance.GetPlayers())
                player.CinematicManager.QueueCinematic(cinematicFactory.CreateCinematic<T>());
        }
    }
}
