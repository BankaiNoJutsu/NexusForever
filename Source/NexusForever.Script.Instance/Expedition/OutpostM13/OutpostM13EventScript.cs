using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Expedition.OutpostM13
{
    [ScriptFilterOwnerId(108)]
    public class OutpostM13EventScript : IPublicEventScript, IOwnedScript<IPublicEvent>
    {
        private IPublicEvent publicEvent;
        private IMapInstance mapInstance;

        /// <summary>
        /// Invoked when <see cref="IScript"/> is loaded.
        /// </summary>
        public void OnLoad(IPublicEvent owner)
        {
            publicEvent = owner;
            mapInstance = publicEvent.Map as IMapInstance ?? throw new InvalidOperationException("Outpost M-13 requires a map instance.");

            publicEvent.SetPhase(PublicEventPhase.TalkToCaptainMilo);
        }

        /// <summary>
        /// Invoked when the public event phase changes.
        /// </summary>
        public void OnPublicEventPhase(uint phase)
        {
            switch ((PublicEventPhase)phase)
            {
                case PublicEventPhase.TalkToCaptainMilo:
                    OnPhaseTalkToCaptainMilo();
                    break;
                case PublicEventPhase.GoToTheCargoHold:
                    OnPhaseGoToTheCargoHold();
                    break;
                case PublicEventPhase.KillTheNovaburnMarauders:
                    OnPhaseKillTheNovaburnMarauders();
                    break;
                case PublicEventPhase.KillRansackerRorgh:
                    OnPhaseKillRansackerRorgh();
                    break;
                case PublicEventPhase.GoToTheAsteroidSurface:
                    OnPhaseGoToTheAsteroidSurface();
                    break;
                case PublicEventPhase.FindForemanKrause:
                    OnPhaseFindForemanKrause();
                    break;
                case PublicEventPhase.CollectDatachrons:
                    OnPhaseCollectDatachrons();
                    break;
                case PublicEventPhase.SearchTheMine:
                    OnPhaseSearchTheMine();
                    break;
                case PublicEventPhase.DefeatHivePods:
                    OnPhaseDefeatHivePods();
                    break;
                case PublicEventPhase.KillHiveQueen:
                    OnPhaseKillHiveQueen();
                    break;
                case PublicEventPhase.HeadToMilosShuttle:
                    OnPhaseHeadToMilosShuttle();
                    break;
            }
        }

        private void OnPhaseTalkToCaptainMilo()
        {
            publicEvent.ActivateObjective(PublicEventObjective.TalkToCaptainMilo);
        }

        private void OnPhaseGoToTheCargoHold()
        {
            publicEvent.ActivateObjective(PublicEventObjective.GoToTheCargoHold, mapInstance.PlayerCount);
        }

        private void OnPhaseKillTheNovaburnMarauders()
        {
            publicEvent.ActivateObjective(PublicEventObjective.KillNovaburnMarauders);
        }

        private void OnPhaseKillRansackerRorgh()
        {
            publicEvent.ActivateObjective(PublicEventObjective.KillRansackerRorgh);
        }

        private void OnPhaseGoToTheAsteroidSurface()
        {
            publicEvent.ActivateObjective(PublicEventObjective.GoToTheAsteroidSurface);
        }

        private void OnPhaseFindForemanKrause()
        {
            publicEvent.ActivateObjective(PublicEventObjective.FindForemanKrause);
        }

        private void OnPhaseCollectDatachrons()
        {
            publicEvent.ActivateObjective(PublicEventObjective.CollectDatachrons);
        }

        private void OnPhaseSearchTheMine()
        {
            publicEvent.ActivateObjective(PublicEventObjective.SearchTheMine);
        }

        private void OnPhaseDefeatHivePods()
        {
            publicEvent.ActivateObjective(PublicEventObjective.DefeatHivePods);
        }

        private void OnPhaseKillHiveQueen()
        {
            publicEvent.ActivateObjective(PublicEventObjective.KillHiveQueen);
        }

        private void OnPhaseHeadToMilosShuttle()
        {
            publicEvent.ActivateObjective(PublicEventObjective.HeadToMilosShuttle, mapInstance.PlayerCount);
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
                case PublicEventObjective.TalkToCaptainMilo:
                    publicEvent.SetPhase(PublicEventPhase.GoToTheCargoHold);
                    break;
                case PublicEventObjective.GoToTheCargoHold:
                    publicEvent.SetPhase(PublicEventPhase.KillTheNovaburnMarauders);
                    break;
                case PublicEventObjective.KillNovaburnMarauders:
                    publicEvent.SetPhase(PublicEventPhase.KillRansackerRorgh);
                    break;
                case PublicEventObjective.KillRansackerRorgh:
                    publicEvent.SetPhase(PublicEventPhase.GoToTheAsteroidSurface);
                    break;
                case PublicEventObjective.GoToTheAsteroidSurface:
                    publicEvent.SetPhase(PublicEventPhase.FindForemanKrause);
                    break;
                case PublicEventObjective.FindForemanKrause:
                    publicEvent.SetPhase(PublicEventPhase.CollectDatachrons);
                    break;
                case PublicEventObjective.CollectDatachrons:
                    publicEvent.SetPhase(PublicEventPhase.SearchTheMine);
                    break;
                case PublicEventObjective.SearchTheMine:
                    publicEvent.SetPhase(PublicEventPhase.DefeatHivePods);
                    break;
                case PublicEventObjective.DefeatHivePods:
                    publicEvent.SetPhase(PublicEventPhase.KillHiveQueen);
                    break;
                case PublicEventObjective.KillHiveQueen:
                    publicEvent.SetPhase(PublicEventPhase.HeadToMilosShuttle);
                    break;
                case PublicEventObjective.HeadToMilosShuttle:
                    publicEvent.Finish(PublicEventTeam.PublicTeam);
                    break;
            }
        }
    }
}
