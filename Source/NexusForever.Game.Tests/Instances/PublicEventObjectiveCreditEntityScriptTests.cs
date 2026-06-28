using System.Reflection;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.Script.Instance;
using NexusForever.Script.Instance.Adventure.WarOfTheWilds.Script;
using NexusForever.Script.Instance.Dungeon.ProtogamesAcademy.Script;
using NexusForever.Script.Instance.WorldStory.HallOfTheHundred.Script;
using NexusForever.Script.Instance.Dungeon.RuinsOfKelVoreth.Script;
using NexusForever.Script.Instance.Dungeon.SanctuaryOfTheSwordmaiden.Script;
using NexusForever.Script.Instance.Dungeon.Skullcano.Script;
using NexusForever.Script.Instance.Dungeon.StormtalonsLair.Script;
using NexusForever.Script.Instance.Dungeon.UltimateProtogames.Script;
using NexusForever.Script.Instance.Expedition.EvilFromTheEther.Script;
using NexusForever.Script.Instance.Expedition.FragmentZero.Script;
using NexusForever.Script.Instance.Expedition.Gauntlet.Script;
using NexusForever.Script.Instance.Expedition.Infestation.Script;
using NexusForever.Script.Instance.Expedition.OutpostM13.Script;
using NexusForever.Script.Instance.Expedition.RageLogic.Script;
using NexusForever.Script.Instance.Expedition.SpaceMadness.Script;
using NexusForever.Script.Instance.Raid.Datascape.Script;
using NexusForever.Script.Instance.Raid.GeneticArchives.Script;
using NexusForever.Script.Instance.Raid.RedMoonTerror.FortyMan.Script;
using NexusForever.Script.Instance.Raid.RedMoonTerror.Script;
using NexusForever.Script.Instance.Dungeon.UltimateProtogames.Downsizer.Script;
using NexusForever.Script.Main.AI;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;
using DatascapeObjective = NexusForever.Script.Instance.Raid.Datascape.PublicEventObjective;
using EvilObjective = NexusForever.Script.Instance.Expedition.EvilFromTheEther.PublicEventObjective;
using FragmentZeroObjective = NexusForever.Script.Instance.Expedition.FragmentZero.PublicEventObjective;
using GauntletObjective = NexusForever.Script.Instance.Expedition.Gauntlet.PublicEventObjective;
using GeneticObjective = NexusForever.Script.Instance.Raid.GeneticArchives.PublicEventObjective;
using HallObjective = NexusForever.Script.Instance.WorldStory.HallOfTheHundred.PublicEventObjective;
using InfestationObjective = NexusForever.Script.Instance.Expedition.Infestation.PublicEventObjective;
using OutpostM13Objective = NexusForever.Script.Instance.Expedition.OutpostM13.PublicEventObjective;
using ProtogamesObjective = NexusForever.Script.Instance.Dungeon.ProtogamesAcademy.PublicEventObjective;
using RageLogicObjective = NexusForever.Script.Instance.Expedition.RageLogic.PublicEventObjective;
using RedMoonObjective = NexusForever.Script.Instance.Raid.RedMoonTerror.PublicEventObjective;
using RedMoonFortyManObjective = NexusForever.Script.Instance.Raid.RedMoonTerror.FortyMan.PublicEventObjective;
using RuinsObjective = NexusForever.Script.Instance.Dungeon.RuinsOfKelVoreth.PublicEventObjective;
using SanctuaryObjective = NexusForever.Script.Instance.Dungeon.SanctuaryOfTheSwordmaiden.PublicEventObjective;
using SkullcanoObjective = NexusForever.Script.Instance.Dungeon.Skullcano.PublicEventObjective;
using SpaceMadnessObjective = NexusForever.Script.Instance.Expedition.SpaceMadness.PublicEventObjective;
using StormtalonObjective = NexusForever.Script.Instance.Dungeon.StormtalonsLair.PublicEventObjective;
using UltimateProtogamesObjective = NexusForever.Script.Instance.Dungeon.UltimateProtogames.PublicEventObjective;
using UltimateProtogamesDownsizerObjective = NexusForever.Script.Instance.Dungeon.UltimateProtogames.Downsizer.PublicEventObjective;
using NexusForever.Shared;

namespace NexusForever.Game.Tests.Instances;

public class PublicEventObjectiveCreditEntityScriptTests
{
    public static IEnumerable<object[]> ObjectiveCreditScripts()
    {
        yield return ScriptCase((f, g) => new InvulnotronEntityScript(f, g), ProtogamesObjective.DefeatInvulnotron);
        yield return ScriptCase((f, g) => new GromkaEntityScript(f, g), ProtogamesObjective.DefeatGromka);
        yield return ScriptCase((f, g) => new IrukiBoldbeardEntityScript(f, g), ProtogamesObjective.DefeatIrukiBoldbeard);
        yield return ScriptCase((f, g) => new SeekNSlaughterEntityScript(f, g), ProtogamesObjective.DefeatSeekNSlaughter);
        yield return ScriptCase((f, g) => new IceboxMk2EntityScript(f, g), ProtogamesObjective.DefeatIceboxMk2);
        yield return ScriptCase((f, g) => new SuperInvulnotronEntityScript(f, g), ProtogamesObjective.DefeatSuperInvulnotron);
        yield return ScriptCase((f, g) => new WrathboneEntityScript(f, g), ProtogamesObjective.DefeatWrathbone);
        yield return ScriptCase((f, g) => new GrondTheCorpsemakerEntityScript(f, g), RuinsObjective.DefeatGrondTheCorpsemaker);
        yield return ScriptCase((f, g) => new SlavemasterDrokkEntityScript(f, g), RuinsObjective.DefeatSlavemasterDrokk);
        yield return ScriptCase((f, g) => new DarkwitchGurkaEntityScript(f, g), RuinsObjective.DefeatDarkwitchGurka);
        yield return ScriptCase((f, g) => new VorethBattleswornDarkwitchEntityScript(f, g), RuinsObjective.KillBattleswornAndDarkwitchOsun);
        yield return ScriptCase((f, g) => new ForgemasterTrogunEntityScript(f, g), RuinsObjective.DefeatForgemasterTrogun);
        yield return ScriptCase((f, g) => new DeadringerShallaosEntityScript(f, g), SanctuaryObjective.DefeatDeadringerShallaos);
        yield return ScriptCase((f, g) => new ZealousTorineEntityScript(f, g), SanctuaryObjective.EliminateZealousTorine);
        yield return ScriptCase((f, g) => new CorruptedTorineSistersEntityScript(f, g), SanctuaryObjective.FreeTheSpiritsOfTheCorruptedTorineSisters);
        yield return ScriptCase((f, g) => new RaynaDarkspeakerEntityScript(f, g), SanctuaryObjective.DefeatRaynaDarkspeaker);
        yield return ScriptCase((f, g) => new MoldwoodOverlordSkashEntityScript(f, g), SanctuaryObjective.DefeatMoldwoodOverlordSkash);
        yield return ScriptCase((f, g) => new MoldwoodOverlordSkashPrisonerChallengeEntityScript(f, g), SanctuaryObjective.DefeatSkashOrHeWillCorruptThePrisoner);
        yield return ScriptCase((f, g) => new DistractedMoldwoodMaulerEntityScript(f, g), SanctuaryObjective.KillDistractedMoldwoodMaulers);
        yield return ScriptCase((f, g) => new MoldwoodSkurgeAndCrawlerEntityScript(f, g), SanctuaryObjective.DestroyMoldwoodSkurgeAndCrawlers);
        yield return ScriptCase((f, g) => new CorruptedTerrorantulaEntityScript(f, g), SanctuaryObjective.KillTheCorruptedTerrorantulas);
        yield return ScriptCase((f, g) => new CorruptedDeathstingSwarmEntityScript(f, g), SanctuaryObjective.DestroyDeathstingSwarms);
        yield return ScriptCase((f, g) => new CorruptedVeteranSwordmaidenEntityScript(f, g), SanctuaryObjective.KillCorruptedVeteranSwordmaidens);
        yield return ScriptCase((f, g) => new MoldwoodCorruptorEntityScript(f, g), SanctuaryObjective.DestroyTheMoldwoodCorruptors);
        yield return ScriptCase((f, g) => new HammerfistMoldjawEntityScript(f, g), SanctuaryObjective.DefeatHammerfistMoldjaw);
        yield return ScriptCase((f, g) => new CorruptedEdgesmithTorianEntityScript(f, g), SanctuaryObjective.DefeatCorruptedEdgesmithTorian);
        yield return ScriptCase((f, g) => new CorruptedLifecallerKhaleeEntityScript(f, g), SanctuaryObjective.KillCorruptedLifecallerKhalee);
        yield return ScriptCase((f, g) => new CorruptedDeathbringerDareiaEntityScript(f, g), SanctuaryObjective.KillCorruptedDeathbringerDareia);
        yield return ScriptCase((f, g) => new OnduLifeweaverEntityScript(f, g), SanctuaryObjective.DefeatOnduLifeweaver);
        yield return ScriptCase((f, g) => new LifeweaverGuardianEntityScript(f, g), SanctuaryObjective.KillTheLifeweaverGuardian);
        yield return ScriptCase((f, g) => new SpiritmotherSeleneTheCorruptedEntityScript(f, g), SanctuaryObjective.DefeatSpiritmotherSeleneTheCorrupted);
        yield return ScriptCase((f, g) => new ElderMoldwoodRavagerEntityScript(f, g), SanctuaryObjective.DestroyTheElderMoldwoodRavager);
        yield return ScriptCase((f, g) => new FlameCrazedDemonEntityScript(f, g), SanctuaryObjective.DestroyTheFlameCrazedDemon);
        yield return ScriptCase((f, g) => new ThunderfootNormalEntityScript(f, g), SkullcanoObjective.DefeatThunderfoot);
        yield return ScriptCase((f, g) => new StewShamanTuggaNormalEntityScript(f, g), SkullcanoObjective.DefeatStewShamanTugga);
        yield return ScriptCase((f, g) => new BosunOctogEntityScript(f, g), SkullcanoObjective.DefeatBosunOctog);
        yield return ScriptCase((f, g) => new QuartermasterGruharEntityScript(f, g), SkullcanoObjective.KillGruharAndTakeStash);
        yield return ScriptCase((f, g) => new MordechaiRedmoonEntityScript(f, g), SkullcanoObjective.DefeatMordechaiRedmoon);
        yield return ScriptCase((f, g) => new GoldInfusedLavaNodeEntityScript(f, g), SkullcanoObjective.MineGoldInfusedLavaCores);
        yield return ScriptCase((f, g) => new MondosMonstrosityEntityScript(f, g), UltimateProtogamesObjective.MonstrosityMassacre, UltimateProtogamesObjective.QuickReflexes);
        yield return ScriptCase((f, g) => new MondosCrateEntityScript(f, g), UltimateProtogamesObjective.MondosCrate);
        yield return ScriptCase((f, g) => new RufflesEntityScript(f, g), UltimateProtogamesObjective.HuntRuffles);
        yield return ScriptCase((f, g) => new HutHutEntityScript(f, g), UltimateProtogamesObjective.DefeatHutHut);
        yield return ScriptCase((f, g) => new DeputyEntityScript(f, g), UltimateProtogamesObjective.Deputy);
        yield return ScriptCase((f, g) => new MisplacedMammothEntityScript(f, g), UltimateProtogamesObjective.QuickReflexes2);
        yield return ScriptCase((f, g) => new BladeWindTheInvokerVeteranEntityScript(f, g), StormtalonObjective.DefeatBladeWindTheInvoker);
        yield return ScriptCase((f, g) => new AethrosVeteranEntityScript(f, g), StormtalonObjective.EliminateAethros);
        yield return ScriptCase((f, g) => new ThundercallZealotRushEntityScript(f, g), StormtalonObjective.SurviveTheThundercallPellZealots);
        yield return ScriptCase((f, g) => new ArcanistBreezeBinderEntityScript(f, g), StormtalonObjective.DefeatArcanistBreezeBinderForTheEncryptionKey);
        yield return ScriptCase((f, g) => new OverseerDriftCatcherEntityScript(f, g), StormtalonObjective.KillOverseerDriftCatcher);
        yield return ScriptCase((f, g) => new StormtalonEntityScript(f, g), StormtalonObjective.DestroyStormtalon);
        yield return ScriptCase((f, g) => new VaregorEntityScript(f, g), HallObjective.DefeatVaregor);
        yield return ScriptCase((f, g) => new UnboundFlameElementalEntityScript(f, g), HallObjective.DefeatUnboundFlameElemental);
        yield return ScriptCase((f, g) => new IceboundOverlordEntityScript(f, g), HallObjective.DefeatIceboundOverlord);
        yield return ScriptCase((f, g) => new DarkwitchYotulEntityScript(f, g), HallObjective.DefeatDarkwitchYotul);
        yield return ScriptCase((f, g) => new HarizogColdbloodEntityScript(f, g), HallObjective.DefeatHarizog);
        yield return ScriptCase((f, g) => new HavikShiverhoundEntityScript(f, g), HallObjective.DefeatHavikShiverhound);
        yield return ScriptCase((f, g) => new DarkwitchUhrgaEntityScript(f, g), HallObjective.DefeatDarkwitchUhrga);
        yield return ScriptCase((f, g) => new HavikHonorguardEntityScript(f, g), HallObjective.DefeatHavikHonorguard);
        yield return ScriptCase((f, g) => new OsunBlockingTheWayEntityScript(f, g), HallObjective.DefeatOsunBlockingTheWay);
        yield return ScriptCase((f, g) => new VaregorWatchhoundEntityScript(f, g), HallObjective.DefeatVaregorWatchhound);
        yield return ScriptCase((f, g) => new PrimalWraithEntityScript(f, g), HallObjective.DefeatPrimalWraith);
        yield return ScriptCase((f, g) => new ColdAndHungryYetiEntityScript(f, g), HallObjective.DefeatYeti);
        yield return ScriptCase((f, g) => new FirstArenaFrenziedCreatureEntityScript(f, g), GauntletObjective.SurviveTheFirstArena);
        yield return ScriptCase((f, g) => new ChampionatorEntityScript(f, g), GauntletObjective.KillTheChampionator);
        yield return ScriptCase((f, g) => new VoodooKinEntityScript(f, g), GauntletObjective.KillTheVoodooKin);
        yield return ScriptCase((f, g) => new OpposingFactionTeamEntityScript(f, g), GauntletObjective.KillTheOpposingFactionsTeam);
        yield return ScriptCase((f, g) => new RockstarYetiEntityScript(f, g), GauntletObjective.KillTheRockstarYeti);
        yield return ScriptCase((f, g) => new GoonSquadEntityScript(f, g), GauntletObjective.DefeatTheGoonSquad);
        yield return ScriptCase((f, g) => new HandlerAndPetsEntityScript(f, g), GauntletObjective.DefeatTheHandlerAndHisPets);
        yield return ScriptCase((f, g) => new SliceAndDiceEntityScript(f, g), GauntletObjective.DefeatSliceAndDice);
        yield return ScriptCase((f, g) => new PyroManiacEntityScript(f, g), GauntletObjective.DefeatPyroManiac);
        yield return ScriptCase((f, g) => new ShowtimeEntityScript(f, g), GauntletObjective.DefeatShowtime);
        yield return ScriptCase((f, g) => new ShockKingEntityScript(f, g), GauntletObjective.DefeatTheShockKing);
        yield return ScriptCase((f, g) => new BrickBraggorEntityScript(f, g), GauntletObjective.DefeatBrickBraggor);
        yield return ScriptCase((f, g) => new SavePanickedWorkersNightmareEntityScript(f, g), SpaceMadnessObjective.SavePanickedWorkers);
        yield return ScriptCase((f, g) => new HallucinatingLivestockEntityScript(f, g), SpaceMadnessObjective.KillHallucinatingLivestock);
        yield return ScriptCase((f, g) => new LumberingParasiteEntityScript(f, g), InfestationObjective.KillLumberingParasites);
        yield return ScriptCase((f, g) => new CyclopeanParasiteEntityScript(f, g), InfestationObjective.KillCyclopeanParasite);
        yield return ScriptCase((f, g) => new LashingFiendEntityScript(f, g), InfestationObjective.DefeatTheAttackOnMedbay);
        yield return ScriptCase((f, g) => new CargoHoldNovaburnMarauderEntityScript(f, g), OutpostM13Objective.KillNovaburnMarauders);
        yield return ScriptCase((f, g) => new RansackerRorghEntityScript(f, g), OutpostM13Objective.KillRansackerRorgh);
        yield return ScriptCase((f, g) => new HivePodMinerInfectorEntityScript(f, g), OutpostM13Objective.DefeatHivePods);
        yield return ScriptCase((f, g) => new HiveQueenEntityScript(f, g), OutpostM13Objective.KillHiveQueen);
        yield return ScriptCase((f, g) => new RagebotAsteroidDefenderEntityScript(f, g), RageLogicObjective.ObliterateRagebotsDefendingAsteroid);
        yield return ScriptCase((f, g) => new PrototypeAlphaEntityScript(f, g), FragmentZeroObjective.DefeatPrototypeAlphansideTheIncubationComplex);
        yield return ScriptCase((f, g) => new PrototypeBetaEntityScript(f, g), FragmentZeroObjective.DefeatPrototypeBeta);
        yield return ScriptCase((f, g) => new PrototypeDeltaEntityScript(f, g), FragmentZeroObjective.DefeatPrototypeDelta);
        yield return ScriptCase((f, g) => new ProjectMatronEntityScript(f, g), FragmentZeroObjective.DefeatProjectMatron);
        yield return ScriptCase((f, g) => new LifeOverseerEntityScript(f, g), FragmentZeroObjective.DefeatTheLifeOverseer);
        yield return ScriptCase((f, g) => new FeastingEthericOrganismEntityScript(f, g), EvilObjective.DefeatEthericOrganisms);
        yield return ScriptCase((f, g) => new TeleporterEthericOrganismEntityScript(f, g), EvilObjective.DefeatEthericOrganisms2);
        yield return ScriptCase((f, g) => new OptimizedMemoryProbeED1EntityScript(f, g), DatascapeObjective.DefeatOptimizedMemoryProbeED1);
        yield return ScriptCase((f, g) => new OptimizedMemoryProbeP2ZEntityScript(f, g), DatascapeObjective.DefeatOptimizedMemoryProbeP2Z);
        yield return ScriptCase((f, g) => new OptimizedMemoryProbeTX67EntityScript(f, g), DatascapeObjective.DefeatOptimizedMemoryProbeTX67);
        yield return ScriptCase((f, g) => new NullSystemDaemonEntityScript(f, g), DatascapeObjective.DefeatTheSystemDaemons);
        yield return ScriptCase((f, g) => new BinarySystemDaemonEntityScript(f, g), DatascapeObjective.DefeatTheSystemDaemons);
        yield return ScriptCase((f, g) => new DatascapeAvatusEntityScript(f, g), DatascapeObjective.DefeatAvatus);
        yield return ScriptCase((f, g) => new FrostBoulderAvalancheFirstEntityScript(f, g), DatascapeObjective.DefeatTheFirstFrostBoulderAvalanche);
        yield return ScriptCase((f, g) => new FrostBoulderAvalancheSecondEntityScript(f, g), DatascapeObjective.DefeatTheSecondFrostBoulderAvalanche);
        yield return ScriptCase((f, g) => new FrostbringerWarlockEntityScript(f, g), DatascapeObjective.DefeatTheFrostbringerWarlock);
        yield return ScriptCase((f, g) => new BioEnhancedBroodmotherEntityScript(f, g), DatascapeObjective.DefeatTheBioEnhancedBroodmother);
        yield return ScriptCase((f, g) => new GloomclawEntityScript(f, g), DatascapeObjective.DefeatGloomclaw);
        yield return ScriptCase((f, g) => new HyperAcceleratedSkeledroidEntityScript(f, g), DatascapeObjective.DefeatTheHyperAcceleratedSkeledroid);
        yield return ScriptCase((f, g) => new AugmentedHeraldOfAvatusEntityScript(f, g), DatascapeObjective.DefeatTheAugmentedHeraldOfAvatus);
        yield return ScriptCase((f, g) => new WarmongerAgrathaEntityScript(f, g), DatascapeObjective.DefeatWarmongerAgratha);
        yield return ScriptCase((f, g) => new WarmongerChunaEntityScript(f, g), DatascapeObjective.DefeatWarmongerChuna);
        yield return ScriptCase((f, g) => new WarmongerTalariiEntityScript(f, g), DatascapeObjective.DefeatWarmongerTalarii);
        yield return ScriptCase((f, g) => new GrandWarmongerTargreshEntityScript(f, g), DatascapeObjective.DefeatGrandWarmongerTargresh);
        yield return ScriptCase((f, g) => new ExperimentX89EntityScript(f, g), GeneticObjective.DefeatExperimentX89);
        yield return ScriptCase((f, g) => new KuralakTheDefilerEntityScript(f, g), GeneticObjective.DefeatKuralakTheDefiler);
        yield return ScriptCase((f, g) => new FetidMiscreationEntityScript(f, g), GeneticObjective.DefeatTheFetidMiscreation);
        yield return ScriptCase((f, g) => new PhagetechGuardianC148EntityScript(f, g), GeneticObjective.DefeatPhagetechGuardianC148);
        yield return ScriptCase((f, g) => new PhagetechGuardianC432EntityScript(f, g), GeneticObjective.DefeatPhagetechGuardianC432);
        yield return ScriptCase((f, g) => new PhageMawEntityScript(f, g), GeneticObjective.DefeatPhageMaw);
        yield return ScriptCase((f, g) => new PhagetechPrototypesEntityScript(f, g), GeneticObjective.DefeatThePhagetechPrototypes);
        yield return ScriptCase((f, g) => new MalfunctioningPistonEntityScript(f, g), GeneticObjective.DefeatTheMalfunctioningPiston);
        yield return ScriptCase((f, g) => new MalfunctioningBatteryEntityScript(f, g), GeneticObjective.DefeatTheMalfunctioningBattery);
        yield return ScriptCase((f, g) => new MalfunctioningDynamoEntityScript(f, g), GeneticObjective.DefeatTheMalfunctioningDynamo);
        yield return ScriptCase((f, g) => new MalfunctioningGearEntityScript(f, g), GeneticObjective.DefeatTheMalfunctioningGear);
        yield return ScriptCase((f, g) => new ChiefWardenLockjawEntityScript(f, g), RedMoonObjective.DefeatChiefWardenLockjaw);
        yield return ScriptCase((f, g) => new SwabbieSkiLiEntityScript(f, g), RedMoonObjective.DefeatSwabbieSkiLi);
        yield return ScriptCase((f, g) => new RobominationEntityScript(f, g), RedMoonObjective.DefeatTheRobomination);
        yield return ScriptCase((f, g) => new AssistantTechnicianSkootyEntityScript(f, g), RedMoonObjective.DefeatAssistantTechnicianSkooty);
        yield return ScriptCase((f, g) => new ChiefEngineScrubberThragEntityScript(f, g), RedMoonObjective.DefeatChiefEngineScrubberThrag);
        yield return ScriptCase((f, g) => new RedMoonEngineerEntityScript(f, g), RedMoonObjective.DefeatTheEngineers);
        yield return ScriptCase((f, g) => new RedMoonTerrorMordechaiRedmoonEntityScript(f, g), RedMoonObjective.DefeatMordechaiRedmoon);
        yield return ScriptCase((f, g) => new StarEaterTheVoraciousEntityScript(f, g), RedMoonObjective.DefeatStarEaterTheVoracious);
        yield return ScriptCase((f, g) => new AntiBoardingTurretEntityScript(f, g), RedMoonObjective.DestroyTheAntiBoardingTurret);
        yield return ScriptCase((f, g) => new MarauderOfficerEntityScript(f, g), RedMoonObjective.DefeatMarauderOfficers);
        yield return ScriptCase((f, g) => new StarmapSimulationEntityScript(f, g), RedMoonObjective.DefeatTheStarmapSimulation);
        yield return ScriptCase((f, g) => new BonedoctorMuburuEntityScript(f, g), RedMoonObjective.DefeatBonedoctorMuburu);
        yield return ScriptCase((f, g) => new HeadshrinkerWgasaEntityScript(f, g), RedMoonObjective.DefeatHeadshrinkerWgasa);
        yield return ScriptCase((f, g) => new TinyEntityScript(f, g), RedMoonObjective.DefeatTiny);
        yield return ScriptCase((f, g) => new UntombedHorrorEntityScript(f, g), RedMoonObjective.DefeatUntombedHorror);
        yield return ScriptCase((f, g) => new LavekaTheDarkHeartedEntityScript(f, g), RedMoonObjective.DefeatLavekaTheDarkHearted);
        yield return ScriptCase((f, g) => new RedMoonTerror40ManLavekaEntityScript(f, g), RedMoonFortyManObjective.DefeatLavekaTheDarkHearted);
        yield return ScriptCase((f, g) => new DownsizerEntityScript(f, g), UltimateProtogamesDownsizerObjective.DefeatTheDownsizer);
    }

    [Theory]
    [MemberData(nameof(ObjectiveCreditScripts))]
    public void OnDeath_UpdatesMappedPublicEventObjective(
        Func<IFactory<ISpellParameters>, IGameTableManager, PublicEventObjectiveCreditEntityScript> createScript,
        uint[] expectedObjectiveIds)
    {
        IPublicEventManager publicEventManager = RecordingDispatchProxy<IPublicEventManager>.Create(out RecordingDispatchProxy<IPublicEventManager> managerProxy);
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out RecordingDispatchProxy<IBaseMap> mapProxy);
        mapProxy.SetProperty(nameof(IBaseMap.PublicEventManager), publicEventManager);

        ICreatureEntity creature = RecordingDispatchProxy<ICreatureEntity>.Create(out RecordingDispatchProxy<ICreatureEntity> creatureProxy);
        creatureProxy.SetProperty(nameof(IWorldEntity.CreatureId), 1u);
        creatureProxy.SetProperty(nameof(IGridEntity.Map), map);

        PublicEventObjectiveCreditEntityScript script = createScript(CreateSpellParametersFactory(), RecordingDispatchProxy<IGameTableManager>.Create(out _));
        script.OnLoad(creature);
        script.OnDeath();

        AssertObjectiveUpdates(managerProxy, expectedObjectiveIds);
    }

    [Theory]
    [MemberData(nameof(ObjectiveCreditScripts))]
    public void OnDeath_WhenRepeated_UpdatesMappedPublicEventObjectiveOnce(
        Func<IFactory<ISpellParameters>, IGameTableManager, PublicEventObjectiveCreditEntityScript> createScript,
        uint[] expectedObjectiveIds)
    {
        IPublicEventManager publicEventManager = RecordingDispatchProxy<IPublicEventManager>.Create(out RecordingDispatchProxy<IPublicEventManager> managerProxy);
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out RecordingDispatchProxy<IBaseMap> mapProxy);
        mapProxy.SetProperty(nameof(IBaseMap.PublicEventManager), publicEventManager);

        ICreatureEntity creature = RecordingDispatchProxy<ICreatureEntity>.Create(out RecordingDispatchProxy<ICreatureEntity> creatureProxy);
        creatureProxy.SetProperty(nameof(IWorldEntity.CreatureId), 1u);
        creatureProxy.SetProperty(nameof(IGridEntity.Map), map);

        PublicEventObjectiveCreditEntityScript script = createScript(CreateSpellParametersFactory(), RecordingDispatchProxy<IGameTableManager>.Create(out _));
        script.OnLoad(creature);
        script.OnDeath();
        script.OnDeath();

        AssertObjectiveUpdates(managerProxy, expectedObjectiveIds);
    }

    [Fact]
    public void BevORageEntityScript_OnDeath_UpdatesMappedBossAndTimedChildObjectivesOnce()
    {
        IPublicEventManager publicEventManager = RecordingDispatchProxy<IPublicEventManager>.Create(out RecordingDispatchProxy<IPublicEventManager> managerProxy);
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out RecordingDispatchProxy<IBaseMap> mapProxy);
        mapProxy.SetProperty(nameof(IBaseMap.PublicEventManager), publicEventManager);

        ICreatureEntity creature = RecordingDispatchProxy<ICreatureEntity>.Create(out RecordingDispatchProxy<ICreatureEntity> creatureProxy);
        creatureProxy.SetProperty(nameof(IWorldEntity.CreatureId), 61463u);
        creatureProxy.SetProperty(nameof(IGridEntity.Map), map);

        var script = new BevORageEntityScript(CreateSpellParametersFactory(), RecordingDispatchProxy<IGameTableManager>.Create(out _));
        script.OnLoad(creature);
        script.OnDeath();
        script.OnDeath();

        Assert.Collection(
            managerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)),
            update => AssertObjectiveUpdate(update, UltimateProtogamesObjective.DefeatBevORage),
            update => AssertObjectiveUpdate(update, UltimateProtogamesObjective.Caffeinated),
            update => AssertObjectiveUpdate(update, UltimateProtogamesObjective.OutOfOrder));
    }

    public static IEnumerable<object[]> ScriptNameFilteredObjectiveCreditScripts()
    {
        foreach (object[] scriptCase in ObjectiveCreditScripts())
        {
            var createScript = (Func<IFactory<ISpellParameters>, IGameTableManager, PublicEventObjectiveCreditEntityScript>)scriptCase[0];
            PublicEventObjectiveCreditEntityScript script = createScript(
                CreateSpellParametersFactory(),
                RecordingDispatchProxy<IGameTableManager>.Create(out _));

            if (script.GetType().GetCustomAttribute<ScriptFilterCreatureIdAttribute>() != null)
                continue;

            yield return scriptCase;
        }
    }

    [Theory]
    [MemberData(nameof(ScriptNameFilteredObjectiveCreditScripts))]
    public void ObjectiveCreditScripts_RequireScriptNameFilter(
        Func<IFactory<ISpellParameters>, IGameTableManager, PublicEventObjectiveCreditEntityScript> createScript,
        uint[] expectedObjectiveIds)
    {
        _ = expectedObjectiveIds;

        PublicEventObjectiveCreditEntityScript script = createScript(CreateSpellParametersFactory(), RecordingDispatchProxy<IGameTableManager>.Create(out _));
        ScriptFilterScriptNameAttribute attribute = script.GetType().GetCustomAttribute<ScriptFilterScriptNameAttribute>();

        Assert.NotNull(attribute);
        Assert.False(string.IsNullOrWhiteSpace(attribute.ScriptName));
    }

    [Theory]
    [MemberData(nameof(ObjectiveCreditScripts))]
    public void ObjectiveCreditScripts_DoNotInheritCombatAI(
        Func<IFactory<ISpellParameters>, IGameTableManager, PublicEventObjectiveCreditEntityScript> createScript,
        uint[] expectedObjectiveIds)
    {
        _ = expectedObjectiveIds;

        PublicEventObjectiveCreditEntityScript script = createScript(CreateSpellParametersFactory(), RecordingDispatchProxy<IGameTableManager>.Create(out _));

        Assert.False(typeof(CombatAI).IsAssignableFrom(script.GetType()));
    }

    [Theory]
    [MemberData(nameof(ScriptNameFilteredObjectiveCreditScripts))]
    public void ObjectiveCreditScripts_OnlyMatchExplicitScriptNameSearch(
        Func<IFactory<ISpellParameters>, IGameTableManager, PublicEventObjectiveCreditEntityScript> createScript,
        uint[] expectedObjectiveIds)
    {
        _ = expectedObjectiveIds;

        PublicEventObjectiveCreditEntityScript script = createScript(CreateSpellParametersFactory(), RecordingDispatchProxy<IGameTableManager>.Create(out _));
        ScriptFilterParameters parameters = new(RecordingDispatchProxy<IServiceProvider>.Create(out _));
        parameters.Initialise(script.GetType());

        IScriptFilterSearch unnamedSearch = new ScriptFilterSearch()
            .FilterByScriptType<IOwnedScript<ICreatureEntity>>()
            .FilterByCreatureId(1u);
        Assert.False(new ScriptFilterMatch().Match(unnamedSearch, parameters));

        IScriptFilterSearch namedSearch = new ScriptFilterSearch()
            .FilterByScriptType<IOwnedScript<ICreatureEntity>>()
            .FilterByScriptNames([parameters.ScriptName]);
        Assert.True(new ScriptFilterMatch().Match(namedSearch, parameters));
    }

    [Fact]
    public void BevORageEntityScript_UsesReviewedCreatureFilter()
    {
        ScriptFilterParameters parameters = new(RecordingDispatchProxy<IServiceProvider>.Create(out _));
        parameters.Initialise(typeof(BevORageEntityScript));

        IScriptFilterSearch bevORageSearch = new ScriptFilterSearch()
            .FilterByScriptType<IOwnedScript<ICreatureEntity>>()
            .FilterByCreatureId(61463u);
        IScriptFilterSearch unrelatedSearch = new ScriptFilterSearch()
            .FilterByScriptType<IOwnedScript<ICreatureEntity>>()
            .FilterByCreatureId(61464u);

        var match = new ScriptFilterMatch();
        Assert.True(match.Match(bevORageSearch, parameters));
        Assert.False(match.Match(unrelatedSearch, parameters));
    }

    [Theory]
    [InlineData(typeof(MondosMonstrosityEntityScript), 62575u)]
    [InlineData(typeof(MondosCrateEntityScript), 62549u)]
    [InlineData(typeof(RufflesEntityScript), 65794u)]
    [InlineData(typeof(HutHutEntityScript), 61417u)]
    [InlineData(typeof(GildedFowlEntityScript), 63055u)]
    [InlineData(typeof(DeputyEntityScript), 68949u)]
    [InlineData(typeof(MisplacedMammothEntityScript), 63312u)]
    public void UltimateProtogamesObjectiveDeathCreditScripts_UseReviewedCreatureFilters(
        Type scriptType,
        uint creatureId)
    {
        AssertCreatureFilterMatches(scriptType, [creatureId], 61463u);
    }

    [Theory]
    [InlineData(typeof(SuperInvulnotronEntityScript), 68096u, 67944u)]
    [InlineData(typeof(WrathboneEntityScript), 67944u, 68096u)]
    public void ProtogamesFinalBossEntityScripts_UseMappedCreatureFilters(
        Type scriptType,
        uint matchingCreatureId,
        uint unrelatedCreatureId)
    {
        AssertCreatureFilterMatches(
            scriptType,
            [matchingCreatureId],
            unrelatedCreatureId);
    }

    [Fact]
    public void GiantMoodieTotemEntityScript_UsesMappedCreatureFilter()
    {
        AssertCreatureFilterMatches(
            typeof(GiantMoodieTotemEntityScript),
            [25952u],
            25956u);
    }

    [Fact]
    public void DeadringerShallaosEntityScript_UsesCreatureFilterForTargetGroupRows()
    {
        AssertCreatureFilterMatches(
            typeof(DeadringerShallaosEntityScript),
            [28597u, 28599u, 28600u],
            28733u);
    }

    [Fact]
    public void ZealousTorineEntityScript_UsesCreatureFilterForNestedTargetGroupRows()
    {
        AssertCreatureFilterMatches(
            typeof(ZealousTorineEntityScript),
            [28580u, 28612u, 28646u, 28585u, 28613u, 28599u, 28600u],
            28733u);
    }

    [Fact]
    public void CorruptedTorineSistersEntityScript_UsesCreatureFilterForNestedTargetGroupRows()
    {
        AssertCreatureFilterMatches(
            typeof(CorruptedTorineSistersEntityScript),
            [
                29266u, 29303u, 29222u, 72983u, 72984u, 72985u,
                29267u, 29304u, 29223u, 72995u, 72997u, 72998u,
                28733u, 28732u, 28736u, 28735u,
                28985u, 28986u, 28993u, 28992u, 28995u, 28996u
            ],
            29198u);
    }

    [Fact]
    public void RaynaDarkspeakerEntityScript_UsesCreatureFilterForNormalAndVeteranRows()
    {
        AssertCreatureFilterMatches(
            typeof(RaynaDarkspeakerEntityScript),
            [28733u, 28732u],
            28728u);
    }

    [Fact]
    public void MoldwoodOverlordSkashEntityScript_UsesCreatureFilterForNormalAndVeteranRows()
    {
        AssertCreatureFilterMatches(
            typeof(MoldwoodOverlordSkashEntityScript),
            [28728u, 28727u],
            28732u);
    }

    [Fact]
    public void MoldwoodOverlordSkashPrisonerChallengeEntityScript_UsesCreatureFilterForNormalAndVeteranRows()
    {
        AssertCreatureFilterMatches(
            typeof(MoldwoodOverlordSkashPrisonerChallengeEntityScript),
            [28728u, 28727u],
            28732u);
    }

    [Fact]
    public void HammerfistMoldjawEntityScript_UsesCreatureFilterForNormalAndVeteranRows()
    {
        AssertCreatureFilterMatches(
            typeof(HammerfistMoldjawEntityScript),
            [41219u, 41220u],
            28985u);
    }

    [Fact]
    public void CorruptedEdgesmithTorianEntityScript_UsesCreatureFilterForNormalAndVeteranRows()
    {
        AssertCreatureFilterMatches(
            typeof(CorruptedEdgesmithTorianEntityScript),
            [28985u, 28986u],
            28993u);
    }

    [Fact]
    public void CorruptedLifecallerKhaleeEntityScript_UsesCreatureFilterForNormalAndVeteranRows()
    {
        AssertCreatureFilterMatches(
            typeof(CorruptedLifecallerKhaleeEntityScript),
            [28993u, 28992u],
            28985u);
    }

    [Fact]
    public void CorruptedDeathbringerDareiaEntityScript_UsesCreatureFilterForNormalAndVeteranRows()
    {
        AssertCreatureFilterMatches(
            typeof(CorruptedDeathbringerDareiaEntityScript),
            [28995u, 28996u],
            28993u);
    }

    [Fact]
    public void OnduLifeweaverEntityScript_UsesCreatureFilterForTargetGroupRows()
    {
        AssertCreatureFilterMatches(
            typeof(OnduLifeweaverEntityScript),
            [28719u, 28720u, 28721u],
            28728u);
    }

    [Fact]
    public void LifeweaverGuardianEntityScript_UsesCreatureFilterForNormalAndVeteranRows()
    {
        AssertCreatureFilterMatches(
            typeof(LifeweaverGuardianEntityScript),
            [28774u, 28775u],
            28719u);
    }

    [Fact]
    public void SpiritmotherSeleneTheCorruptedEntityScript_UsesCreatureFilterForNormalAndVeteranRows()
    {
        AssertCreatureFilterMatches(
            typeof(SpiritmotherSeleneTheCorruptedEntityScript),
            [28736u, 28735u],
            28732u);
    }

    [Fact]
    public void ElderMoldwoodRavagerEntityScript_UsesCreatureFilterForNormalAndVeteranRows()
    {
        AssertCreatureFilterMatches(
            typeof(ElderMoldwoodRavagerEntityScript),
            [29198u, 29199u],
            28774u);
    }

    [Fact]
    public void DistractedMoldwoodMaulerEntityScript_UsesCreatureFilterForObjectiveRows()
    {
        AssertCreatureFilterMatches(
            typeof(DistractedMoldwoodMaulerEntityScript),
            [29311u, 29310u, 72988u, 72999u],
            28728u);
    }

    [Fact]
    public void MoldwoodSkurgeAndCrawlerEntityScript_UsesCreatureFilterForMappedEventRows()
    {
        AssertCreatureFilterMatches(
            typeof(MoldwoodSkurgeAndCrawlerEntityScript),
            [28930u, 28892u, 41204u, 15655u, 15654u, 15666u],
            30209u);
    }

    [Fact]
    public void CorruptedTerrorantulaEntityScript_UsesCreatureFilterForMappedEventRows()
    {
        AssertCreatureFilterMatches(
            typeof(CorruptedTerrorantulaEntityScript),
            [28829u, 15664u],
            30209u);
    }

    [Fact]
    public void CorruptedDeathstingSwarmEntityScript_UsesCreatureFilterForMappedEventRows()
    {
        AssertCreatureFilterMatches(
            typeof(CorruptedDeathstingSwarmEntityScript),
            [29249u, 29252u, 15653u, 15691u],
            29267u);
    }

    [Fact]
    public void CorruptedVeteranSwordmaidenEntityScript_UsesCreatureFilterForMappedEventRows()
    {
        AssertCreatureFilterMatches(
            typeof(CorruptedVeteranSwordmaidenEntityScript),
            [29267u, 15658u],
            29249u);
    }

    [Fact]
    public void MoldwoodCorruptorEntityScript_UsesCreatureFilterForMappedEventRows()
    {
        AssertCreatureFilterMatches(
            typeof(MoldwoodCorruptorEntityScript),
            [30209u, 15683u],
            28829u);
    }

    [Fact]
    public void FirstArenaFrenziedCreatureEntityScript_UsesCreatureFilterForNormalAndVeteranRows()
    {
        AssertCreatureFilterMatches(
            typeof(FirstArenaFrenziedCreatureEntityScript),
            [
                48461u, 48765u, 48793u, 48833u, 48835u, 48842u,
                48843u, 48844u, 48863u, 48865u, 48866u, 48867u,
                69237u, 69239u, 69240u, 69241u, 69242u, 69243u,
                69244u, 69246u, 69247u, 69248u, 69250u, 69251u
            ],
            48554u);
    }

    [Fact]
    public void ChampionatorEntityScript_UsesCreatureFilterForNormalAndVeteranRows()
    {
        AssertCreatureFilterMatches(
            typeof(ChampionatorEntityScript),
            [48529u, 69255u],
            48554u);
    }

    [Fact]
    public void VoodooKinEntityScript_UsesCreatureFilterForNormalAndVeteranRows()
    {
        AssertCreatureFilterMatches(
            typeof(VoodooKinEntityScript),
            [48511u, 48499u, 69258u, 69257u],
            48554u);
    }

    [Fact]
    public void OpposingFactionTeamEntityScript_UsesCreatureFilterForMappedFactionFrictionRows()
    {
        AssertCreatureFilterMatches(
            typeof(OpposingFactionTeamEntityScript),
            [48666u, 48667u, 48669u, 48670u, 69282u, 69283u, 69284u, 69285u],
            48554u);
    }

    [Fact]
    public void RockstarYetiEntityScript_UsesCreatureFilterForNormalAndVeteranRows()
    {
        AssertCreatureFilterMatches(
            typeof(RockstarYetiEntityScript),
            [48491u, 69254u],
            48554u);
    }

    [Fact]
    public void GoonSquadEntityScript_UsesCreatureFilterForMappedNormalAndVeteranRows()
    {
        AssertCreatureFilterMatches(
            typeof(GoonSquadEntityScript),
            [48588u, 48589u, 48591u, 69309u, 69310u, 69311u, 52112u],
            48554u);
    }

    [Fact]
    public void HandlerAndPetsEntityScript_UsesCreatureFilterForNormalAndVeteranRows()
    {
        AssertCreatureFilterMatches(
            typeof(HandlerAndPetsEntityScript),
            [48516u, 48518u, 48519u, 69302u, 69303u, 69304u],
            48554u);
    }

    [Fact]
    public void SliceAndDiceEntityScript_UsesCreatureFilterForNormalAndVeteranRows()
    {
        AssertCreatureFilterMatches(
            typeof(SliceAndDiceEntityScript),
            [48557u, 48558u, 69306u, 69307u],
            48554u);
    }

    [Fact]
    public void PyroManiacEntityScript_UsesCreatureFilterForNormalAndVeteranRows()
    {
        AssertCreatureFilterMatches(
            typeof(PyroManiacEntityScript),
            [48493u, 48510u, 69300u, 69301u],
            48554u);
    }

    [Fact]
    public void ShowtimeEntityScript_UsesCreatureFilterForNormalAndVeteranRows()
    {
        AssertCreatureFilterMatches(
            typeof(ShowtimeEntityScript),
            [48579u, 69308u],
            48554u);
    }

    [Fact]
    public void ShockKingEntityScript_UsesCreatureFilterForNormalAndVeteranRows()
    {
        AssertCreatureFilterMatches(
            typeof(ShockKingEntityScript),
            [48554u, 69305u],
            69688u);
    }

    [Fact]
    public void BrickBraggorEntityScript_UsesCreatureFilterForReviewedBossRow()
    {
        AssertCreatureFilterMatches(
            typeof(BrickBraggorEntityScript),
            [69688u],
            69728u);
    }

    [Fact]
    public void SavePanickedWorkersNightmareEntityScript_UsesCreatureFilterForNestedNightmareTargetGroups()
    {
        AssertCreatureFilterMatches(
            typeof(SavePanickedWorkersNightmareEntityScript),
            [
                46123u, 46124u, 46126u, 46127u, 46128u, 46130u, 46131u,
                46132u, 46133u, 46135u, 46137u, 46721u, 58783u, 58798u
            ],
            45903u);
    }

    [Fact]
    public void HallucinatingLivestockEntityScript_UsesCreatureFilterForReviewedPhaseFourRows()
    {
        AssertCreatureFilterMatches(
            typeof(HallucinatingLivestockEntityScript),
            [46483u, 46714u],
            45903u);
    }

    [Fact]
    public void DarkwitchGurkaEntityScript_UsesCreatureFilterForNormalAndVeteranRows()
    {
        ScriptFilterParameters parameters = new(RecordingDispatchProxy<IServiceProvider>.Create(out _));
        parameters.Initialise(typeof(DarkwitchGurkaEntityScript));

        IScriptFilterSearch normalSearch = new ScriptFilterSearch()
            .FilterByScriptType<IOwnedScript<ICreatureEntity>>()
            .FilterByCreatureId(33049u);
        IScriptFilterSearch veteranSearch = new ScriptFilterSearch()
            .FilterByScriptType<IOwnedScript<ICreatureEntity>>()
            .FilterByCreatureId(33050u);
        IScriptFilterSearch unrelatedSearch = new ScriptFilterSearch()
            .FilterByScriptType<IOwnedScript<ICreatureEntity>>()
            .FilterByCreatureId(33051u);

        var match = new ScriptFilterMatch();
        Assert.True(match.Match(normalSearch, parameters));
        Assert.True(match.Match(veteranSearch, parameters));
        Assert.False(match.Match(unrelatedSearch, parameters));
    }

    [Fact]
    public void VorethBattleswornDarkwitchEntityScript_UsesCreatureFilterForTargetGroupRows()
    {
        AssertCreatureFilterMatches(
            typeof(VorethBattleswornDarkwitchEntityScript),
            [32555u, 32556u, 32618u, 32619u],
            32534u);
    }

    [Fact]
    public void StewShamanTuggaNormalEntityScript_UsesCreatureFilterForNormalAndVeteranRows()
    {
        AssertCreatureFilterMatches(
            typeof(StewShamanTuggaNormalEntityScript),
            [24493u, 24898u],
            24475u);
    }

    [Fact]
    public void ThunderfootNormalEntityScript_UsesCreatureFilterForNormalAndVeteranRows()
    {
        AssertCreatureFilterMatches(
            typeof(ThunderfootNormalEntityScript),
            [24475u, 24893u],
            24493u);
    }

    [Fact]
    public void BosunOctogEntityScript_UsesCreatureFilterForNormalAndVeteranRows()
    {
        ScriptFilterParameters parameters = new(RecordingDispatchProxy<IServiceProvider>.Create(out _));
        parameters.Initialise(typeof(BosunOctogEntityScript));

        IScriptFilterSearch normalSearch = new ScriptFilterSearch()
            .FilterByScriptType<IOwnedScript<ICreatureEntity>>()
            .FilterByCreatureId(24486u);
        IScriptFilterSearch veteranSearch = new ScriptFilterSearch()
            .FilterByScriptType<IOwnedScript<ICreatureEntity>>()
            .FilterByCreatureId(24894u);
        IScriptFilterSearch unrelatedSearch = new ScriptFilterSearch()
            .FilterByScriptType<IOwnedScript<ICreatureEntity>>()
            .FilterByCreatureId(24490u);

        var match = new ScriptFilterMatch();
        Assert.True(match.Match(normalSearch, parameters));
        Assert.True(match.Match(veteranSearch, parameters));
        Assert.False(match.Match(unrelatedSearch, parameters));
    }

    [Fact]
    public void QuartermasterGruharEntityScript_UsesCreatureFilterForNormalAndVeteranRows()
    {
        ScriptFilterParameters parameters = new(RecordingDispatchProxy<IServiceProvider>.Create(out _));
        parameters.Initialise(typeof(QuartermasterGruharEntityScript));

        IScriptFilterSearch normalSearch = new ScriptFilterSearch()
            .FilterByScriptType<IOwnedScript<ICreatureEntity>>()
            .FilterByCreatureId(24490u);
        IScriptFilterSearch veteranSearch = new ScriptFilterSearch()
            .FilterByScriptType<IOwnedScript<ICreatureEntity>>()
            .FilterByCreatureId(24896u);
        IScriptFilterSearch unrelatedSearch = new ScriptFilterSearch()
            .FilterByScriptType<IOwnedScript<ICreatureEntity>>()
            .FilterByCreatureId(24486u);

        var match = new ScriptFilterMatch();
        Assert.True(match.Match(normalSearch, parameters));
        Assert.True(match.Match(veteranSearch, parameters));
        Assert.False(match.Match(unrelatedSearch, parameters));
    }

    [Fact]
    public void MordechaiRedmoonEntityScript_UsesCreatureFilterForNormalAndVeteranRows()
    {
        ScriptFilterParameters parameters = new(RecordingDispatchProxy<IServiceProvider>.Create(out _));
        parameters.Initialise(typeof(MordechaiRedmoonEntityScript));

        IScriptFilterSearch normalSearch = new ScriptFilterSearch()
            .FilterByScriptType<IOwnedScript<ICreatureEntity>>()
            .FilterByCreatureId(24489u);
        IScriptFilterSearch veteranSearch = new ScriptFilterSearch()
            .FilterByScriptType<IOwnedScript<ICreatureEntity>>()
            .FilterByCreatureId(24895u);
        IScriptFilterSearch unrelatedSearch = new ScriptFilterSearch()
            .FilterByScriptType<IOwnedScript<ICreatureEntity>>()
            .FilterByCreatureId(24490u);

        var match = new ScriptFilterMatch();
        Assert.True(match.Match(normalSearch, parameters));
        Assert.True(match.Match(veteranSearch, parameters));
        Assert.False(match.Match(unrelatedSearch, parameters));
    }

    [Fact]
    public void GoldInfusedLavaNodeEntityScript_UsesCreatureFilterForNormalAndVeteranRows()
    {
        AssertCreatureFilterMatches(
            typeof(GoldInfusedLavaNodeEntityScript),
            [24680u, 24921u],
            24675u);
    }

    [Fact]
    public void StormtalonEntityScript_UsesCreatureFilterForNormalAndVeteranRows()
    {
        ScriptFilterParameters parameters = new(RecordingDispatchProxy<IServiceProvider>.Create(out _));
        parameters.Initialise(typeof(StormtalonEntityScript));

        IScriptFilterSearch normalSearch = new ScriptFilterSearch()
            .FilterByScriptType<IOwnedScript<ICreatureEntity>>()
            .FilterByCreatureId(17163u);
        IScriptFilterSearch veteranSearch = new ScriptFilterSearch()
            .FilterByScriptType<IOwnedScript<ICreatureEntity>>()
            .FilterByCreatureId(33406u);
        IScriptFilterSearch unrelatedSearch = new ScriptFilterSearch()
            .FilterByScriptType<IOwnedScript<ICreatureEntity>>()
            .FilterByCreatureId(32703u);

        var match = new ScriptFilterMatch();
        Assert.True(match.Match(normalSearch, parameters));
        Assert.True(match.Match(veteranSearch, parameters));
        Assert.False(match.Match(unrelatedSearch, parameters));
    }

    [Fact]
    public void BladeWindTheInvokerVeteranEntityScript_UsesCreatureFilterForNormalAndVeteranRows()
    {
        ScriptFilterParameters parameters = new(RecordingDispatchProxy<IServiceProvider>.Create(out _));
        parameters.Initialise(typeof(BladeWindTheInvokerVeteranEntityScript));

        IScriptFilterSearch normalSearch = new ScriptFilterSearch()
            .FilterByScriptType<IOwnedScript<ICreatureEntity>>()
            .FilterByCreatureId(17160u);
        IScriptFilterSearch veteranSearch = new ScriptFilterSearch()
            .FilterByScriptType<IOwnedScript<ICreatureEntity>>()
            .FilterByCreatureId(33405u);
        IScriptFilterSearch unrelatedSearch = new ScriptFilterSearch()
            .FilterByScriptType<IOwnedScript<ICreatureEntity>>()
            .FilterByCreatureId(17166u);

        var match = new ScriptFilterMatch();
        Assert.True(match.Match(normalSearch, parameters));
        Assert.True(match.Match(veteranSearch, parameters));
        Assert.False(match.Match(unrelatedSearch, parameters));
    }

    [Fact]
    public void AethrosVeteranEntityScript_UsesCreatureFilterForNormalAndVeteranRows()
    {
        ScriptFilterParameters parameters = new(RecordingDispatchProxy<IServiceProvider>.Create(out _));
        parameters.Initialise(typeof(AethrosVeteranEntityScript));

        IScriptFilterSearch normalSearch = new ScriptFilterSearch()
            .FilterByScriptType<IOwnedScript<ICreatureEntity>>()
            .FilterByCreatureId(17166u);
        IScriptFilterSearch veteranSearch = new ScriptFilterSearch()
            .FilterByScriptType<IOwnedScript<ICreatureEntity>>()
            .FilterByCreatureId(32703u);
        IScriptFilterSearch unrelatedSearch = new ScriptFilterSearch()
            .FilterByScriptType<IOwnedScript<ICreatureEntity>>()
            .FilterByCreatureId(17160u);

        var match = new ScriptFilterMatch();
        Assert.True(match.Match(normalSearch, parameters));
        Assert.True(match.Match(veteranSearch, parameters));
        Assert.False(match.Match(unrelatedSearch, parameters));
    }

    [Fact]
    public void ThundercallZealotRushEntityScript_UsesCreatureFilterForTargetGroupRows()
    {
        AssertCreatureFilterMatches(
            typeof(ThundercallZealotRushEntityScript),
            [16728u, 26448u],
            17160u);
    }

    [Fact]
    public void ArcanistBreezeBinderEntityScript_UsesCreatureFilterForNormalAndVeteranRows()
    {
        ScriptFilterParameters parameters = new(RecordingDispatchProxy<IServiceProvider>.Create(out _));
        parameters.Initialise(typeof(ArcanistBreezeBinderEntityScript));

        IScriptFilterSearch normalSearch = new ScriptFilterSearch()
            .FilterByScriptType<IOwnedScript<ICreatureEntity>>()
            .FilterByCreatureId(24474u);
        IScriptFilterSearch veteranSearch = new ScriptFilterSearch()
            .FilterByScriptType<IOwnedScript<ICreatureEntity>>()
            .FilterByCreatureId(34711u);
        IScriptFilterSearch unrelatedSearch = new ScriptFilterSearch()
            .FilterByScriptType<IOwnedScript<ICreatureEntity>>()
            .FilterByCreatureId(33361u);

        var match = new ScriptFilterMatch();
        Assert.True(match.Match(normalSearch, parameters));
        Assert.True(match.Match(veteranSearch, parameters));
        Assert.False(match.Match(unrelatedSearch, parameters));
    }

    [Fact]
    public void OverseerDriftCatcherEntityScript_UsesCreatureFilterForNormalAndVeteranRows()
    {
        ScriptFilterParameters parameters = new(RecordingDispatchProxy<IServiceProvider>.Create(out _));
        parameters.Initialise(typeof(OverseerDriftCatcherEntityScript));

        IScriptFilterSearch normalSearch = new ScriptFilterSearch()
            .FilterByScriptType<IOwnedScript<ICreatureEntity>>()
            .FilterByCreatureId(33361u);
        IScriptFilterSearch veteranSearch = new ScriptFilterSearch()
            .FilterByScriptType<IOwnedScript<ICreatureEntity>>()
            .FilterByCreatureId(33362u);
        IScriptFilterSearch unrelatedSearch = new ScriptFilterSearch()
            .FilterByScriptType<IOwnedScript<ICreatureEntity>>()
            .FilterByCreatureId(24474u);

        var match = new ScriptFilterMatch();
        Assert.True(match.Match(normalSearch, parameters));
        Assert.True(match.Match(veteranSearch, parameters));
        Assert.False(match.Match(unrelatedSearch, parameters));
    }

    [Fact]
    public void VaregorEntityScript_UsesCreatureFilterForHallBossRow()
    {
        ScriptFilterParameters parameters = new(RecordingDispatchProxy<IServiceProvider>.Create(out _));
        parameters.Initialise(typeof(VaregorEntityScript));

        IScriptFilterSearch varegorSearch = new ScriptFilterSearch()
            .FilterByScriptType<IOwnedScript<ICreatureEntity>>()
            .FilterByCreatureId(67457u);
        IScriptFilterSearch unrelatedSearch = new ScriptFilterSearch()
            .FilterByScriptType<IOwnedScript<ICreatureEntity>>()
            .FilterByCreatureId(67458u);

        var match = new ScriptFilterMatch();
        Assert.True(match.Match(varegorSearch, parameters));
        Assert.False(match.Match(unrelatedSearch, parameters));
    }

    [Fact]
    public void UnboundFlameElementalEntityScript_UsesCreatureFilterForHallOptionalBossRow()
    {
        ScriptFilterParameters parameters = new(RecordingDispatchProxy<IServiceProvider>.Create(out _));
        parameters.Initialise(typeof(UnboundFlameElementalEntityScript));

        IScriptFilterSearch elementalSearch = new ScriptFilterSearch()
            .FilterByScriptType<IOwnedScript<ICreatureEntity>>()
            .FilterByCreatureId(71414u);
        IScriptFilterSearch unrelatedSearch = new ScriptFilterSearch()
            .FilterByScriptType<IOwnedScript<ICreatureEntity>>()
            .FilterByCreatureId(67444u);

        var match = new ScriptFilterMatch();
        Assert.True(match.Match(elementalSearch, parameters));
        Assert.False(match.Match(unrelatedSearch, parameters));
    }

    [Fact]
    public void IceboundOverlordEntityScript_UsesCreatureFilterForHallOptionalBossRow()
    {
        ScriptFilterParameters parameters = new(RecordingDispatchProxy<IServiceProvider>.Create(out _));
        parameters.Initialise(typeof(IceboundOverlordEntityScript));

        IScriptFilterSearch overlordSearch = new ScriptFilterSearch()
            .FilterByScriptType<IOwnedScript<ICreatureEntity>>()
            .FilterByCreatureId(71577u);
        IScriptFilterSearch unrelatedSearch = new ScriptFilterSearch()
            .FilterByScriptType<IOwnedScript<ICreatureEntity>>()
            .FilterByCreatureId(71414u);

        var match = new ScriptFilterMatch();
        Assert.True(match.Match(overlordSearch, parameters));
        Assert.False(match.Match(unrelatedSearch, parameters));
    }

    [Fact]
    public void DarkwitchYotulEntityScript_UsesCreatureFilterForHallOptionalBossRow()
    {
        ScriptFilterParameters parameters = new(RecordingDispatchProxy<IServiceProvider>.Create(out _));
        parameters.Initialise(typeof(DarkwitchYotulEntityScript));

        IScriptFilterSearch yotulSearch = new ScriptFilterSearch()
            .FilterByScriptType<IOwnedScript<ICreatureEntity>>()
            .FilterByCreatureId(71173u);
        IScriptFilterSearch unrelatedSearch = new ScriptFilterSearch()
            .FilterByScriptType<IOwnedScript<ICreatureEntity>>()
            .FilterByCreatureId(71577u);

        var match = new ScriptFilterMatch();
        Assert.True(match.Match(yotulSearch, parameters));
        Assert.False(match.Match(unrelatedSearch, parameters));
    }

    [Fact]
    public void HarizogColdbloodEntityScript_UsesCreatureFilterForHallBossRow()
    {
        ScriptFilterParameters parameters = new(RecordingDispatchProxy<IServiceProvider>.Create(out _));
        parameters.Initialise(typeof(HarizogColdbloodEntityScript));

        IScriptFilterSearch harizogSearch = new ScriptFilterSearch()
            .FilterByScriptType<IOwnedScript<ICreatureEntity>>()
            .FilterByCreatureId(67444u);
        IScriptFilterSearch unrelatedSearch = new ScriptFilterSearch()
            .FilterByScriptType<IOwnedScript<ICreatureEntity>>()
            .FilterByCreatureId(67457u);

        var match = new ScriptFilterMatch();
        Assert.True(match.Match(harizogSearch, parameters));
        Assert.False(match.Match(unrelatedSearch, parameters));
    }

    [Theory]
    [InlineData(typeof(HavikShiverhoundEntityScript), 67851u)]
    [InlineData(typeof(DarkwitchUhrgaEntityScript), 67855u)]
    [InlineData(typeof(HavikHonorguardEntityScript), 67853u)]
    public void HarizogSummonedAddEntityScripts_UseCreatureFilterForMappedRows(Type scriptType, uint creatureId)
    {
        AssertCreatureFilterMatches(scriptType, [creatureId], 67444u);
    }

    [Fact]
    public void OsunBlockingTheWayEntityScript_UsesCreatureFilterForHallExitRows()
    {
        AssertCreatureFilterMatches(
            typeof(OsunBlockingTheWayEntityScript),
            [67429u, 67430u],
            67431u);
    }

    [Fact]
    public void VaregorWatchhoundEntityScript_UsesCreatureFilterForHallSideMissionRow()
    {
        AssertCreatureFilterMatches(
            typeof(VaregorWatchhoundEntityScript),
            [67884u],
            67885u);
    }

    [Fact]
    public void PrimalWraithEntityScript_UsesCreatureFilterForHallSideMissionBossRow()
    {
        AssertCreatureFilterMatches(
            typeof(PrimalWraithEntityScript),
            [68013u],
            68012u);
    }

    [Fact]
    public void CargoHoldNovaburnMarauderEntityScript_UsesCreatureFilterForEventRows()
    {
        AssertCreatureFilterMatches(
            typeof(CargoHoldNovaburnMarauderEntityScript),
            [23453u, 23454u, 69074u, 69075u],
            69878u);
    }

    [Fact]
    public void RansackerRorghEntityScript_UsesCreatureFilterForNamedCargoHoldBoss()
    {
        AssertCreatureFilterMatches(
            typeof(RansackerRorghEntityScript),
            [23455u],
            23453u);
    }

    [Fact]
    public void HivePodMinerInfectorEntityScript_UsesCreatureFilterForMineCleanupRows()
    {
        AssertCreatureFilterMatches(
            typeof(HivePodMinerInfectorEntityScript),
            [23536u, 23514u, 23500u, 69080u, 69079u, 69077u],
            23513u);
    }

    [Fact]
    public void HiveQueenEntityScript_UsesCreatureFilterForOutpostM13ObjectiveRow()
    {
        AssertCreatureFilterMatches(
            typeof(HiveQueenEntityScript),
            [23513u],
            69078u);
    }

    [Fact]
    public void RagebotAsteroidDefenderEntityScript_UsesCreatureFilterForMappedAsteroidDefenderRows()
    {
        AssertCreatureFilterMatches(
            typeof(RagebotAsteroidDefenderEntityScript),
            [69107u, 69097u, 32302u, 69106u, 69096u, 32322u, 69095u, 69094u, 32323u, 44232u, 43827u, 32324u],
            32365u);
    }

    [Fact]
    public void DownsizerEntityScript_UsesCreatureFilterForMappedBossRow()
    {
        AssertCreatureFilterMatches(
            typeof(DownsizerEntityScript),
            [61420u],
            61421u);
    }

    [Fact]
    public void ProjectMatronEntityScript_UsesCreatureFilterForNormalAndVeteranRows()
    {
        AssertCreatureFilterMatches(
            typeof(ProjectMatronEntityScript),
            [69088u, 69664u],
            67526u);
    }

    [Fact]
    public void LifeOverseerEntityScript_UsesCreatureFilterForMappedRows()
    {
        AssertCreatureFilterMatches(
            typeof(LifeOverseerEntityScript),
            [67522u, 69635u],
            69088u);
    }

    [Fact]
    public void FetidMiscreationEntityScript_UsesCreatureFilterForMappedBossRow()
    {
        AssertCreatureFilterMatches(
            typeof(FetidMiscreationEntityScript),
            [56377u],
            49198u);
    }

    [Fact]
    public void PhagetechGuardianC148EntityScript_UsesCreatureFilterForMappedBossRow()
    {
        AssertCreatureFilterMatches(
            typeof(PhagetechGuardianC148EntityScript),
            [54785u],
            54787u);
    }

    [Fact]
    public void PhagetechGuardianC432EntityScript_UsesCreatureFilterForMappedBossRow()
    {
        AssertCreatureFilterMatches(
            typeof(PhagetechGuardianC432EntityScript),
            [54787u],
            54785u);
    }

    [Fact]
    public void PhageMawEntityScript_UsesCreatureFilterForMappedBossRow()
    {
        AssertCreatureFilterMatches(
            typeof(PhageMawEntityScript),
            [52974u],
            54029u);
    }

    [Fact]
    public void PhagetechPrototypesEntityScript_UsesCreatureFilterForMappedRows()
    {
        AssertCreatureFilterMatches(
            typeof(PhagetechPrototypesEntityScript),
            [54029u, 54030u, 54031u, 54032u],
            52974u);
    }

    [Fact]
    public void MalfunctioningPistonEntityScript_UsesCreatureFilterForMappedMinibossRow()
    {
        AssertCreatureFilterMatches(
            typeof(MalfunctioningPistonEntityScript),
            [56106u],
            56174u);
    }

    [Fact]
    public void MalfunctioningBatteryEntityScript_UsesCreatureFilterForMappedMinibossRow()
    {
        AssertCreatureFilterMatches(
            typeof(MalfunctioningBatteryEntityScript),
            [56174u],
            56106u);
    }

    [Fact]
    public void MalfunctioningDynamoEntityScript_UsesCreatureFilterForMappedMinibossRow()
    {
        AssertCreatureFilterMatches(
            typeof(MalfunctioningDynamoEntityScript),
            [54935u],
            55066u);
    }

    [Fact]
    public void MalfunctioningGearEntityScript_UsesCreatureFilterForMappedMinibossRow()
    {
        AssertCreatureFilterMatches(
            typeof(MalfunctioningGearEntityScript),
            [55066u],
            54935u);
    }

    [Theory]
    [InlineData(typeof(ChiefWardenLockjawEntityScript), 75214u, 68655u)]
    [InlineData(typeof(SwabbieSkiLiEntityScript), 68655u, 75214u)]
    [InlineData(typeof(RobominationEntityScript), 66085u, 75214u)]
    [InlineData(typeof(AssistantTechnicianSkootyEntityScript), 72872u, 72873u)]
    [InlineData(typeof(ChiefEngineScrubberThragEntityScript), 72873u, 72872u)]
    [InlineData(typeof(RedMoonTerrorMordechaiRedmoonEntityScript), 65800u, 72758u)]
    [InlineData(typeof(StarEaterTheVoraciousEntityScript), 72758u, 65800u)]
    public void RedMoonTerrorDirectKillEntityScripts_UseCreatureFilterForMappedObjectiveRow(
        Type scriptType,
        uint matchingCreatureId,
        uint unrelatedCreatureId)
    {
        AssertCreatureFilterMatches(
            scriptType,
            [matchingCreatureId],
            unrelatedCreatureId);
    }

    [Fact]
    public void RedMoonEngineerEntityScript_UsesCreatureFilterForMappedObjectiveRows()
    {
        AssertCreatureFilterMatches(
            typeof(RedMoonEngineerEntityScript),
            [65759u, 65758u],
            72884u);
    }

    [Fact]
    public void AntiBoardingTurretEntityScript_UsesCreatureFilterForMappedTargetGroupRow()
    {
        AssertCreatureFilterMatches(
            typeof(AntiBoardingTurretEntityScript),
            [75645u],
            72884u);
    }

    [Fact]
    public void MarauderOfficerEntityScript_UsesCreatureFilterForMappedTargetGroupRows()
    {
        AssertCreatureFilterMatches(
            typeof(MarauderOfficerEntityScript),
            [72884u, 72885u, 72886u, 72887u],
            75645u);
    }

    [Theory]
    [InlineData(typeof(StarmapSimulationEntityScript), 73622u, 72888u)]
    [InlineData(typeof(BonedoctorMuburuEntityScript), 72888u, 73622u)]
    [InlineData(typeof(HeadshrinkerWgasaEntityScript), 72889u, 72888u)]
    [InlineData(typeof(TinyEntityScript), 72891u, 72890u)]
    [InlineData(typeof(UntombedHorrorEntityScript), 72890u, 72891u)]
    public void RedMoonTerrorLateChainEntityScripts_UseCreatureFilterForMappedObjectiveRow(
        Type scriptType,
        uint matchingCreatureId,
        uint unrelatedCreatureId)
    {
        AssertCreatureFilterMatches(
            scriptType,
            [matchingCreatureId],
            unrelatedCreatureId);
    }

    [Theory]
    [InlineData(typeof(ExperimentX89EntityScript), "ExperimentX-89EntityScript")]
    [InlineData(typeof(OptimizedMemoryProbeTX67EntityScript), "OptimizedMemoryProbeTX-67EntityScript")]
    [InlineData(typeof(LavekaTheDarkHeartedEntityScript), "LavekaTheDarkHeartedEntityScript")]
    [InlineData(typeof(RedMoonTerror40ManLavekaEntityScript), "RedMoonTerror40ManLavekaEntityScript")]
    [InlineData(typeof(SeekNSlaughterEntityScript), "SeekNSlaughterEntityScript")]
    [InlineData(typeof(IceboxMk2EntityScript), "IceboxMk2EntityScript")]
    public void ExplicitSqlScriptNames_UseScriptFilter(Type scriptType, string expectedScriptName)
    {
        ScriptFilterScriptNameAttribute attribute = scriptType.GetCustomAttribute<ScriptFilterScriptNameAttribute>();

        Assert.NotNull(attribute);
        Assert.Equal(expectedScriptName, attribute.ScriptName);
    }

    private static void AssertCreatureFilterMatches(Type scriptType, IReadOnlyCollection<uint> matchingCreatureIds, uint unrelatedCreatureId)
    {
        ScriptFilterParameters parameters = new(RecordingDispatchProxy<IServiceProvider>.Create(out _));
        parameters.Initialise(scriptType);

        var match = new ScriptFilterMatch();
        foreach (uint matchingCreatureId in matchingCreatureIds)
        {
            IScriptFilterSearch matchingSearch = new ScriptFilterSearch()
                .FilterByScriptType<IOwnedScript<ICreatureEntity>>()
                .FilterByCreatureId(matchingCreatureId);
            Assert.True(match.Match(matchingSearch, parameters));
        }

        IScriptFilterSearch unrelatedSearch = new ScriptFilterSearch()
            .FilterByScriptType<IOwnedScript<ICreatureEntity>>()
            .FilterByCreatureId(unrelatedCreatureId);
        Assert.False(match.Match(unrelatedSearch, parameters));
    }

    private static void AssertObjectiveUpdate(
        RecordingDispatchProxy<IPublicEventManager>.Invocation update,
        UltimateProtogamesObjective objective)
    {
        Assert.Equal(2, update.Arguments.Length);
        Assert.Equal((uint)objective, (uint)update.Arguments[0]);
        Assert.Equal(1, (int)update.Arguments[1]);
    }

    private static void AssertObjectiveUpdates(
        RecordingDispatchProxy<IPublicEventManager> managerProxy,
        uint[] expectedObjectiveIds)
    {
        RecordingDispatchProxy<IPublicEventManager>.Invocation[] updates = managerProxy
            .GetInvocations(nameof(IPublicEventManager.UpdateObjective))
            .Where(i => i.Arguments.Length == 2)
            .ToArray();

        Assert.Equal(expectedObjectiveIds.Length, updates.Length);
        foreach (uint expectedObjectiveId in expectedObjectiveIds)
        {
            RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
                updates,
                i => (uint)i.Arguments[0] == expectedObjectiveId);
            Assert.Equal(1, (int)update.Arguments[1]);
        }
    }

    private static object[] ScriptCase<TObjective>(
        Func<IFactory<ISpellParameters>, IGameTableManager, PublicEventObjectiveCreditEntityScript> createScript,
        params TObjective[] objectives)
        where TObjective : Enum
    {
        return [createScript, objectives.Select(objective => Convert.ToUInt32(objective)).ToArray()];
    }

    private static IFactory<ISpellParameters> CreateSpellParametersFactory()
    {
        IFactory<ISpellParameters> factory = RecordingDispatchProxy<IFactory<ISpellParameters>>.Create(out RecordingDispatchProxy<IFactory<ISpellParameters>> factoryProxy);
        factoryProxy.SetMethodReturn(nameof(IFactory<ISpellParameters>.Resolve), RecordingDispatchProxy<ISpellParameters>.Create(out _));
        return factory;
    }
}
