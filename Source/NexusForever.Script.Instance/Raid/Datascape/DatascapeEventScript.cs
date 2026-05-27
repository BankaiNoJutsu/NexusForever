using NexusForever.Game.Abstract.Cinematic;
using NexusForever.Game.Abstract.Cinematic.Cinematics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Raid.Datascape
{
    [ScriptFilterOwnerId(157)]
    public class DatascapeEventScript : IPublicEventScript, IOwnedScript<IPublicEvent>
    {
        private readonly IGlobalQuestManager globalQuestManager;
        private readonly ICinematicFactory cinematicFactory;
        private readonly HashSet<PublicEventObjective> retrievedDatacores = [];

        private IPublicEvent publicEvent;
        private IMapInstance mapInstance;

        public DatascapeEventScript(
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
                ?? throw new InvalidOperationException("Datascape requires a map instance.");

            retrievedDatacores.Clear();
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
                case PublicEventPhase.HallsOfTheInfiniteMind:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatTheSystemDaemons);
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatOptimizedMemoryProbeED1);
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatOptimizedMemoryProbeP2Z);
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatOptimizedMemoryProbeTX67);
                    break;
                case PublicEventPhase.TheOculus:
                    BroadcastWipCommunicatorMessage(CommunicatorMessage.Caretaker111);
                    break;
                case PublicEventPhase.FirstFrostBoulder:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatTheFirstFrostBoulderAvalanche);
                    BroadcastWipCommunicatorMessage(CommunicatorMessage.Caretaker112);
                    break;
                case PublicEventPhase.SecondFrostBoulder:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatTheSecondFrostBoulderAvalanche);
                    break;
                case PublicEventPhase.FrostbringerWarlock:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatTheFrostbringerWarlock);
                    break;
                case PublicEventPhase.MaelstromAuthority:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatTheMaelstromAuthority);
                    break;
                case PublicEventPhase.AlphaElementalGuardians:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatTheElementalGuardians1);
                    BroadcastWipCommunicatorMessage(CommunicatorMessage.Caretaker113);
                    break;
                case PublicEventPhase.AlphaPersonalityDatacore:
                    publicEvent.ActivateObjective(PublicEventObjective.RetrieveTheFirstPersonalityDatacore);
                    break;
                case PublicEventPhase.EarthRoomCanimid:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatTheFullyOptimizedCanimid);
                    break;
                case PublicEventPhase.EarthRoomRock:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatTheLogicGuidedRockslide);
                    break;
                case PublicEventPhase.Gloomclaw:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatGloomclaw);
                    break;
                case PublicEventPhase.LogicWingRoom1:
                    publicEvent.ActivateObjective(PublicEventObjective.PowerUpAllOfTheEldanPowerGenerators);
                    publicEvent.ActivateObjective(PublicEventObjective.GeneratorCharge1);
                    publicEvent.ActivateObjective(PublicEventObjective.GeneratorCharge2);
                    publicEvent.ActivateObjective(PublicEventObjective.GeneratorCharge3);
                    break;
                case PublicEventPhase.LogicWingRoom2:
                    publicEvent.ActivateObjective(PublicEventObjective.PowerUpTheEldanPowerGenerators);
                    publicEvent.ActivateObjective(PublicEventObjective.GeneratorCharge4);
                    publicEvent.ActivateObjective(PublicEventObjective.GeneratorCharge5);
                    publicEvent.ActivateObjective(PublicEventObjective.GeneratorCharge6);
                    break;
                case PublicEventPhase.LogicWingRoom3:
                    publicEvent.ActivateObjective(PublicEventObjective.PowerUpAllOfTheEldanPowerGenerators3);
                    publicEvent.ActivateObjective(PublicEventObjective.GeneratorCharge7);
                    publicEvent.ActivateObjective(PublicEventObjective.GeneratorCharge8);
                    publicEvent.ActivateObjective(PublicEventObjective.GeneratorCharge9);
                    break;
                case PublicEventPhase.LogicWingLogicElemental:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatTheAbstractAugmentationAlgorithm);
                    break;
                case PublicEventPhase.DeltaElementalGuardians:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatTheElementalGuardians3);
                    break;
                case PublicEventPhase.DeltaPersonalityDatacore:
                    publicEvent.ActivateObjective(PublicEventObjective.RetrieveTheSecondPersonalityDatacore);
                    break;
                case PublicEventPhase.VolatilityLattice:
                    publicEvent.ActivateObjective(PublicEventObjective.EscapeAvatusAttention);
                    publicEvent.ActivateObjective(PublicEventObjective.TimedOut);
                    break;
                case PublicEventPhase.WarmongerAgratha:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatWarmongerAgratha);
                    break;
                case PublicEventPhase.WarmongerChuna:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatWarmongerChuna);
                    break;
                case PublicEventPhase.WarmongerTalarii:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatWarmongerTalarii);
                    break;
                case PublicEventPhase.GrandWarmongerTargresh:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatGrandWarmongerTargresh);
                    break;
                case PublicEventPhase.BetaElementalGuardians:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatTheElementalGuardians2);
                    BroadcastWipCommunicatorMessage(CommunicatorMessage.Caretaker114);
                    break;
                case PublicEventPhase.BetaPersonalityDatacore:
                    publicEvent.ActivateObjective(PublicEventObjective.RetrieveTheThirdPersonalityDatacore);
                    break;
                case PublicEventPhase.MemoryCores:
                    publicEvent.ActivateObjective(PublicEventObjective.PlaceTheDatacoresInTheOculus);
                    break;
                case PublicEventPhase.Avatus:
                    QueueWipGuessedCinematic<IDatascapeAvatusSpawn>();
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatAvatus);
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
                case PublicEventObjective.DefeatTheSystemDaemons:
                    publicEvent.SetPhase(PublicEventPhase.TheOculus);
                    break;
                case PublicEventObjective.DefeatOptimizedMemoryProbeP2Z:
                    publicEvent.ActivateObjective(PublicEventObjective.EscapeTheLimboInfomatrix);
                    break;
                case PublicEventObjective.EscapeTheLimboInfomatrix:
                    publicEvent.SetPhase(PublicEventPhase.FirstFrostBoulder);
                    break;
                case PublicEventObjective.DefeatTheFirstFrostBoulderAvalanche:
                    publicEvent.SetPhase(PublicEventPhase.SecondFrostBoulder);
                    break;
                case PublicEventObjective.DefeatTheSecondFrostBoulderAvalanche:
                    publicEvent.SetPhase(PublicEventPhase.FrostbringerWarlock);
                    break;
                case PublicEventObjective.DefeatTheFrostbringerWarlock:
                    publicEvent.SetPhase(PublicEventPhase.MaelstromAuthority);
                    break;
                case PublicEventObjective.DefeatTheMaelstromAuthority:
                    publicEvent.SetPhase(PublicEventPhase.AlphaPersonalityDatacore);
                    break;
                case PublicEventObjective.DefeatOptimizedMemoryProbeED1:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatTheBioEnhancedBroodmother);
                    break;
                case PublicEventObjective.DefeatTheBioEnhancedBroodmother:
                    publicEvent.SetPhase(PublicEventPhase.EarthRoomCanimid);
                    break;
                case PublicEventObjective.DefeatTheFullyOptimizedCanimid:
                    publicEvent.SetPhase(PublicEventPhase.EarthRoomRock);
                    break;
                case PublicEventObjective.DefeatTheLogicGuidedRockslide:
                    publicEvent.SetPhase(PublicEventPhase.Gloomclaw);
                    break;
                case PublicEventObjective.DefeatGloomclaw:
                    publicEvent.SetPhase(PublicEventPhase.LogicWingRoom1);
                    break;
                case PublicEventObjective.GeneratorCharge2:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatTheHyperAcceleratedSkeledroid);
                    break;
                case PublicEventObjective.DefeatTheHyperAcceleratedSkeledroid:
                    publicEvent.ActivateObjective(PublicEventObjective.DefyPerspective);
                    break;
                case PublicEventObjective.PowerUpAllOfTheEldanPowerGenerators:
                    publicEvent.SetPhase(PublicEventPhase.LogicWingRoom2);
                    break;
                case PublicEventObjective.GeneratorCharge8:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatTheAugmentedHeraldOfAvatus);
                    break;
                case PublicEventObjective.PowerUpTheEldanPowerGenerators:
                    publicEvent.SetPhase(PublicEventPhase.LogicWingRoom3);
                    break;
                case PublicEventObjective.PowerUpAllOfTheEldanPowerGenerators3:
                    publicEvent.SetPhase(PublicEventPhase.LogicWingLogicElemental);
                    break;
                case PublicEventObjective.DefeatTheAbstractAugmentationAlgorithm:
                    publicEvent.SetPhase(PublicEventPhase.DeltaElementalGuardians);
                    break;
                case PublicEventObjective.DefeatTheElementalGuardians3:
                    publicEvent.SetPhase(PublicEventPhase.DeltaPersonalityDatacore);
                    break;
                case PublicEventObjective.DefeatOptimizedMemoryProbeTX67:
                    publicEvent.SetPhase(PublicEventPhase.VolatilityLattice);
                    break;
                case PublicEventObjective.EscapeAvatusAttention:
                    publicEvent.SetPhase(PublicEventPhase.WarmongerAgratha);
                    break;
                case PublicEventObjective.DefeatWarmongerAgratha:
                    publicEvent.SetPhase(PublicEventPhase.WarmongerChuna);
                    break;
                case PublicEventObjective.DefeatWarmongerChuna:
                    publicEvent.SetPhase(PublicEventPhase.WarmongerTalarii);
                    break;
                case PublicEventObjective.DefeatWarmongerTalarii:
                    publicEvent.SetPhase(PublicEventPhase.GrandWarmongerTargresh);
                    break;
                case PublicEventObjective.DefeatGrandWarmongerTargresh:
                    publicEvent.SetPhase(PublicEventPhase.BetaElementalGuardians);
                    break;
                case PublicEventObjective.DefeatTheElementalGuardians2:
                    publicEvent.SetPhase(PublicEventPhase.BetaPersonalityDatacore);
                    break;
                case PublicEventObjective.RetrieveTheFirstPersonalityDatacore:
                case PublicEventObjective.RetrieveTheSecondPersonalityDatacore:
                case PublicEventObjective.RetrieveTheThirdPersonalityDatacore:
                    OnDatacoreRetrieved((PublicEventObjective)objective.Entry.Id);
                    break;
                case PublicEventObjective.PlaceTheDatacoresInTheOculus:
                    publicEvent.SetPhase(PublicEventPhase.Avatus);
                    break;
                case PublicEventObjective.DefeatAvatus:
                    publicEvent.Finish(PublicEventTeam.PublicTeam);
                    break;
            }
        }

        private void OnDatacoreRetrieved(PublicEventObjective objective)
        {
            retrievedDatacores.Add(objective);

            if (retrievedDatacores.Count == 3)
                publicEvent.SetPhase(PublicEventPhase.MemoryCores);
        }

        private void BroadcastWipCommunicatorMessage(CommunicatorMessage message)
        {
            // WIP-guessed from LaughingWS Instances-and-more. The branch pairs these Caretaker
            // broadcasts with Datascape wing handoffs, but exact wing order, cinematic timing,
            // encounter choreography, and door/trigger placement remain blocked pending proof.
            ICommunicatorMessage communicatorMessage = globalQuestManager.GetCommunicatorMessage(message);
            foreach (IPlayer player in mapInstance.GetPlayers())
                communicatorMessage?.Send(player.Session);
        }

        private void QueueWipGuessedCinematic<T>() where T : ICinematicBase
        {
            // WIP-guessed from LaughingWS Instances-and-more. The branch queues this
            // Avatus cinematic at final phase entry, but exact spawn choreography,
            // challenge timing, and cinematic payload remain blocked pending proof.
            foreach (IPlayer player in mapInstance.GetPlayers())
                player.CinematicManager.QueueCinematic(cinematicFactory.CreateCinematic<T>());
        }
    }
}
