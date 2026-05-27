using System.Numerics;
using NexusForever.Game.Abstract.Cinematic;
using NexusForever.Game.Abstract.Cinematic.Cinematics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Trigger;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Expedition.Gauntlet
{
    [ScriptFilterOwnerId(446)]
    public class GauntletEventScript : IPublicEventScript, IOwnedScript<IPublicEvent>
    {
        private readonly ICinematicFactory cinematicFactory;

        private IPublicEvent publicEvent;
        private IMapInstance mapInstance;

        private const uint AirlockWorldLocationId = 38909u;
        private const uint AirlockObjectiveObjectId = 5735u;
        private static readonly Vector3 AirlockTriggerPosition = new(523.0805f, 0.1994047f, -507.9437f);

        private const uint SwarmPitWorldLocationId = 39001u;
        private const uint SwarmPitObjectiveObjectId = 5753u;
        private static readonly Vector3 SwarmPitTriggerPosition = new(-1007.078f, 5.185604E-06f, 1063.784f);

        public GauntletEventScript(
            ICinematicFactory cinematicFactory)
        {
            this.cinematicFactory = cinematicFactory;
        }

        /// <summary>
        /// Invoked when <see cref="IScript"/> is loaded.
        /// </summary>
        public void OnLoad(IPublicEvent owner)
        {
            publicEvent = owner;
            mapInstance = publicEvent.Map as IMapInstance
                ?? throw new InvalidOperationException("Gauntlet requires a map instance.");

            publicEvent.SetPhase(PublicEventPhase.TalkToPilotTaboro);
        }

        /// <summary>
        /// Invoked when the public event phase changes.
        /// </summary>
        public void OnPublicEventPhase(uint phase)
        {
            switch ((PublicEventPhase)phase)
            {
                case PublicEventPhase.TalkToPilotTaboro:
                    publicEvent.ActivateObjective(PublicEventObjective.TalkToPilotTaboro);
                    break;
                case PublicEventPhase.GetIntoAirlock:
                    publicEvent.ActivateObjective(PublicEventObjective.GetIntoAirlock, mapInstance.PlayerCount);
                    SpawnWipGuessedWorldLocationTrigger(AirlockWorldLocationId, AirlockObjectiveObjectId, AirlockTriggerPosition);
                    break;
                case PublicEventPhase.WhatHappened:
                    publicEvent.ActivateObjective(PublicEventObjective.FindOutWhatHappenedToYou);
                    QueueWipGuessedCinematic<IGauntletFindOutWhatHappened>();
                    break;
                case PublicEventPhase.ActivateSwitches:
                    publicEvent.ActivateObjective(PublicEventObjective.ActivateSwitches);
                    publicEvent.ActivateObjective(PublicEventObjective.CollectGoldenSkulls);
                    break;
                case PublicEventPhase.GatherAtTheSwarmPit:
                    publicEvent.ActivateObjective(PublicEventObjective.GatherAtTheSwarmPit, mapInstance.PlayerCount);
                    SpawnWipGuessedWorldLocationTrigger(SwarmPitWorldLocationId, SwarmPitObjectiveObjectId, SwarmPitTriggerPosition);
                    break;
                case PublicEventPhase.SurviveTheFirstArena:
                    publicEvent.ActivateObjective(PublicEventObjective.SurviveTheFirstArena);
                    break;
                case PublicEventPhase.EnterTheChamberOfChoices:
                    publicEvent.ActivateObjective(PublicEventObjective.EnterTheChamberOfChoices, mapInstance.PlayerCount);
                    break;
                case PublicEventPhase.SurviveTheSplorg:
                    publicEvent.ActivateObjective(PublicEventObjective.SurviveTheSplorg);
                    publicEvent.ActivateObjective(PublicEventObjective.CollectGauntletScoreTokens);
                    break;
                case PublicEventPhase.ChamberOfChoices:
                    publicEvent.ActivateObjective(PublicEventObjective.EnterTheFactionFrictionArena, mapInstance.PlayerCount);
                    publicEvent.ActivateObjective(PublicEventObjective.EnterTheCharnelChamber, mapInstance.PlayerCount);
                    publicEvent.ActivateObjective(PublicEventObjective.EndorseAProduct);
                    publicEvent.ActivateObjective(PublicEventObjective.SaveGauntletContestantsFromTheDarkspur);
                    break;
                case PublicEventPhase.EnterTheMainEventArena:
                    publicEvent.ActivateObjective(PublicEventObjective.EnterTheMainEventArena, mapInstance.PlayerCount);
                    break;
                case PublicEventPhase.SurviveTheMainEvent:
                    publicEvent.ActivateObjective(PublicEventObjective.SurviveTheMainEvent);
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatTheGoonSquad);
                    break;
                case PublicEventPhase.DefeatSliceAndDice:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatSliceAndDice);
                    break;
                case PublicEventPhase.DefeatTheShockKing:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatTheShockKing);
                    break;
                case PublicEventPhase.DefeatBrickBraggor:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatBrickBraggor);
                    QueueWipGuessedCinematic<IGauntletOnSpawnBrickBraggor>();
                    break;
                case PublicEventPhase.TalkToNpc:
                    publicEvent.ActivateObjective(PublicEventObjective.TalkToJudgeKain);
                    publicEvent.ActivateObjective(PublicEventObjective.TalkToAgentLex);
                    break;
            }
        }

        private void SpawnWipGuessedWorldLocationTrigger(uint worldLocationId, uint objectId, Vector3 position)
        {
            // WIP-guessed from LaughingWS Instances-and-more: these Gauntlet gather
            // trigger ids/coordinates are branch-provided and covered by tests, but
            // still need retail trigger-row/timing proof and manual expedition smoke.
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

        private void QueueWipGuessedCinematic<T>() where T : ICinematicBase
        {
            // WIP-guessed from LaughingWS Instances-and-more. The branch queues these
            // Gauntlet phase cinematics, but notes that at least the Brick Braggor
            // hook still needs a real payload and exact post-cinematic choreography.
            foreach (IPlayer player in mapInstance.GetPlayers())
                player.CinematicManager.QueueCinematic(cinematicFactory.CreateCinematic<T>());
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
                case PublicEventObjective.TalkToPilotTaboro:
                    publicEvent.SetPhase(PublicEventPhase.GetIntoAirlock);
                    break;
                case PublicEventObjective.GetIntoAirlock:
                    publicEvent.SetPhase(PublicEventPhase.WhatHappened);
                    break;
                case PublicEventObjective.FindOutWhatHappenedToYou:
                    publicEvent.SetPhase(PublicEventPhase.ActivateSwitches);
                    break;
                case PublicEventObjective.ActivateSwitches:
                    publicEvent.SetPhase(PublicEventPhase.GatherAtTheSwarmPit);
                    break;
                case PublicEventObjective.GatherAtTheSwarmPit:
                    publicEvent.SetPhase(PublicEventPhase.SurviveTheFirstArena);
                    break;
                case PublicEventObjective.SurviveTheFirstArena:
                    publicEvent.SetPhase(PublicEventPhase.EnterTheChamberOfChoices);
                    break;
                case PublicEventObjective.EnterTheChamberOfChoices:
                    publicEvent.SetPhase(PublicEventPhase.SurviveTheSplorg);
                    break;
                case PublicEventObjective.SurviveTheSplorg:
                    publicEvent.SetPhase(PublicEventPhase.ChamberOfChoices);
                    break;
                case PublicEventObjective.EnterTheCharnelChamber:
                    publicEvent.ActivateObjective(PublicEventObjective.KillTheChampionator);
                    break;
                case PublicEventObjective.EnterTheFactionFrictionArena:
                    publicEvent.ActivateObjective(PublicEventObjective.KillTheOpposingFactionsTeam);
                    break;
                case PublicEventObjective.KillTheChampionator:
                case PublicEventObjective.KillTheOpposingFactionsTeam:
                    publicEvent.SetPhase(PublicEventPhase.EnterTheMainEventArena);
                    break;
                case PublicEventObjective.EnterTheMainEventArena:
                    publicEvent.SetPhase(PublicEventPhase.SurviveTheMainEvent);
                    break;
                case PublicEventObjective.DefeatTheGoonSquad:
                    publicEvent.SetPhase(PublicEventPhase.DefeatSliceAndDice);
                    break;
                case PublicEventObjective.DefeatSliceAndDice:
                    publicEvent.SetPhase(PublicEventPhase.DefeatTheShockKing);
                    break;
                case PublicEventObjective.DefeatTheShockKing:
                    publicEvent.SetPhase(PublicEventPhase.DefeatBrickBraggor);
                    break;
                case PublicEventObjective.DefeatBrickBraggor:
                    publicEvent.SetPhase(PublicEventPhase.TalkToNpc);
                    break;
                case PublicEventObjective.TalkToJudgeKain:
                case PublicEventObjective.TalkToAgentLex:
                    publicEvent.Finish(PublicEventTeam.PublicTeam);
                    break;
            }
        }
    }
}
