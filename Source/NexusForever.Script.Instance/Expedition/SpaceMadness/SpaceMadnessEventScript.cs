using System.Numerics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Trigger;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Expedition.SpaceMadness
{
    [ScriptFilterOwnerId(390)]
    public class SpaceMadnessEventScript : IPublicEventScript, IOwnedScript<IPublicEvent>
    {
        private IPublicEvent publicEvent;
        private IMapInstance mapInstance;

        private const uint EnterAirlockWorldLocationId = 37702u;
        private const uint EnterAirlockObjectiveObjectId = 5512u;
        private static readonly Vector3 EnterAirlockTriggerPosition = new(-35.28216f, 4.942017f, 270.0408f);

        private const uint EnterResearchLaboratoryWorldLocationId = 37621u;
        private const uint EnterResearchLaboratoryObjectiveObjectId = 5531u;
        private static readonly Vector3 EnterResearchLaboratoryTriggerPosition = new(-55.08974f, -0.05234623f, 137.5821f);

        /// <summary>
        /// Invoked when <see cref="IScript"/> is loaded.
        /// </summary>
        public void OnLoad(IPublicEvent owner)
        {
            publicEvent = owner;
            mapInstance = publicEvent.Map as IMapInstance
                ?? throw new InvalidOperationException("Space Madness requires a map instance.");

            publicEvent.SetPhase(PublicEventPhase.TalkToCaptainTero);
        }

        /// <summary>
        /// Invoked when the public event phase changes.
        /// </summary>
        public void OnPublicEventPhase(uint phase)
        {
            switch ((PublicEventPhase)phase)
            {
                case PublicEventPhase.TalkToCaptainTero:
                    OnPhaseTalkToCaptainTero();
                    break;
                case PublicEventPhase.TalkToMajorLeeBarmy:
                    OnPhaseTalkToMajorLeeBarmy();
                    break;
                case PublicEventPhase.EnterTheAirlock:
                    OnPhaseEnterTheAirlock();
                    break;
                case PublicEventPhase.AccessTheObservationDeckComputer:
                    OnPhaseAccessTheObservationDeckComputer();
                    break;
                case PublicEventPhase.EnterTheResearchLaboratory:
                    OnPhaseEnterTheResearchLaboratory();
                    break;
                case PublicEventPhase.OpenHazmatStorageCloset:
                    OnPhaseOpenHazmatStorageCloset();
                    break;
                case PublicEventPhase.SurviveYourNightmares:
                    OnPhaseSurviveYourNightmares();
                    break;
                case PublicEventPhase.EquipAHazmatSuit:
                    OnPhaseEquipAHazmatSuit();
                    break;
                case PublicEventPhase.KillHallucinatingLivestock:
                    OnPhaseKillHallucinatingLivestock();
                    break;
                case PublicEventPhase.ActivateAirScrubberControls:
                    OnPhaseActivateAirScrubberControls();
                    break;
                case PublicEventPhase.SurviveTheAirScrubbingProcess:
                    OnPhaseSurviveTheAirScrubbingProcess();
                    break;
                case PublicEventPhase.SendTheAllClearSignal:
                    OnPhaseSendTheAllClearSignal();
                    break;
            }
        }

        private void OnPhaseTalkToCaptainTero()
        {
            publicEvent.ActivateObjective(PublicEventObjective.TalkToCaptainTero);
        }

        private void OnPhaseTalkToMajorLeeBarmy()
        {
            publicEvent.ActivateObjective(PublicEventObjective.TalkToMajorLeeBarmy);
        }

        private void OnPhaseEnterTheAirlock()
        {
            publicEvent.ActivateObjective(PublicEventObjective.EnterTheAirlock, mapInstance.PlayerCount);
            SpawnWipGuessedWorldLocationTrigger(
                EnterAirlockWorldLocationId,
                EnterAirlockObjectiveObjectId,
                EnterAirlockTriggerPosition);
        }

        private void OnPhaseAccessTheObservationDeckComputer()
        {
            publicEvent.ActivateObjective(PublicEventObjective.AccessTheObservationDeckComputer);
            publicEvent.ActivateObjective(PublicEventObjective.SavePanickedWorkers);
            publicEvent.ActivateObjective(PublicEventObjective.AccessCrewDatapads);
            publicEvent.ActivateObjective(PublicEventObjective.CollectEscapedCreatures, 3);
        }

        private void OnPhaseEnterTheResearchLaboratory()
        {
            publicEvent.ActivateObjective(PublicEventObjective.EnterTheResearchLaboratory, mapInstance.PlayerCount);
            SpawnWipGuessedWorldLocationTrigger(
                EnterResearchLaboratoryWorldLocationId,
                EnterResearchLaboratoryObjectiveObjectId,
                EnterResearchLaboratoryTriggerPosition);
        }

        private void OnPhaseOpenHazmatStorageCloset()
        {
            publicEvent.ActivateObjective(PublicEventObjective.OpenHazmatStorageCloset);
        }

        private void OnPhaseSurviveYourNightmares()
        {
            publicEvent.ActivateObjective(PublicEventObjective.SurviveYourNightmares);
        }

        private void OnPhaseEquipAHazmatSuit()
        {
            publicEvent.ActivateObjective(PublicEventObjective.EquipAHazmatSuit);
        }

        private void OnPhaseKillHallucinatingLivestock()
        {
            publicEvent.ActivateObjective(PublicEventObjective.KillHallucinatingLivestock);
            publicEvent.ActivateObjective(PublicEventObjective.GiveAirHelmsToWorkers);
            publicEvent.ActivateObjective(PublicEventObjective.AvoidExplodingRowsdowers);
        }

        private void OnPhaseActivateAirScrubberControls()
        {
            publicEvent.ActivateObjective(PublicEventObjective.ActivateAirScrubberControls);
        }

        private void OnPhaseSurviveTheAirScrubbingProcess()
        {
            publicEvent.ActivateObjective(PublicEventObjective.SurviveTheAirScrubbingProcess);
        }

        private void OnPhaseSendTheAllClearSignal()
        {
            publicEvent.ActivateObjective(PublicEventObjective.SendTheAllClearSignal);
        }

        private void SpawnWipGuessedWorldLocationTrigger(uint worldLocationId, uint objectId, Vector3 position)
        {
            // WIP-GUESSED: LaughingWS supplies these Space Madness trigger ids/coordinates without
            // 16042 capture proof. Keep the guess scoped to participant-gather triggers until the
            // retail world-location rows and phase timing are mapped.
            IWorldLocationVolumeGridTriggerEntity triggerEntity = publicEvent.CreateEntity<IWorldLocationVolumeGridTriggerEntity>();
            triggerEntity.Initialise(worldLocationId, objectId);
            AddToMap(triggerEntity, position);
        }

        private void AddToMap(IGridEntity entity, Vector3 position)
        {
            mapInstance.EnqueueAdd(entity, new ScriptMapPosition
            {
                Info = new ScriptMapInfo
                {
                    Entry   = mapInstance.Entry,
                    MapLock = mapInstance.MapLock
                },
                Position = position
            });
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
                case PublicEventObjective.TalkToCaptainTero:
                    publicEvent.SetPhase(PublicEventPhase.TalkToMajorLeeBarmy);
                    break;
                case PublicEventObjective.TalkToMajorLeeBarmy:
                    publicEvent.SetPhase(PublicEventPhase.EnterTheAirlock);
                    break;
                case PublicEventObjective.EnterTheAirlock:
                    publicEvent.SetPhase(PublicEventPhase.AccessTheObservationDeckComputer);
                    break;
                case PublicEventObjective.AccessTheObservationDeckComputer:
                    publicEvent.SetPhase(PublicEventPhase.EnterTheResearchLaboratory);
                    break;
                case PublicEventObjective.EnterTheResearchLaboratory:
                    publicEvent.SetPhase(PublicEventPhase.OpenHazmatStorageCloset);
                    break;
                case PublicEventObjective.OpenHazmatStorageCloset:
                    publicEvent.SetPhase(PublicEventPhase.SurviveYourNightmares);
                    break;
                case PublicEventObjective.SurviveYourNightmares:
                    publicEvent.SetPhase(PublicEventPhase.EquipAHazmatSuit);
                    break;
                case PublicEventObjective.EquipAHazmatSuit:
                    publicEvent.SetPhase(PublicEventPhase.KillHallucinatingLivestock);
                    break;
                case PublicEventObjective.KillHallucinatingLivestock:
                    publicEvent.SetPhase(PublicEventPhase.ActivateAirScrubberControls);
                    break;
                case PublicEventObjective.ActivateAirScrubberControls:
                    publicEvent.SetPhase(PublicEventPhase.SurviveTheAirScrubbingProcess);
                    break;
                case PublicEventObjective.SurviveTheAirScrubbingProcess:
                    publicEvent.SetPhase(PublicEventPhase.SendTheAllClearSignal);
                    break;
                case PublicEventObjective.SendTheAllClearSignal:
                    publicEvent.Finish(PublicEventTeam.PublicTeam);
                    break;
            }
        }
    }
}
