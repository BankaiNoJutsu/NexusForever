using System.Reflection;
using System.Runtime.CompilerServices;
using System.Numerics;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Cinematic;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Map.Search;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Abstract.Story;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.PlayerPath;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Static.Spell;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.IO.Map;
using NexusForever.Network.Message;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.PlayerPath;
using NexusForever.Network.World.Message.Static;
using NexusForever.Script.Main.Quests.NorthernWilds;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;
using PlayerPath = NexusForever.Game.Static.PlayerPath.Path;

namespace NexusForever.Game.Tests.Quests;

public class NorthernWildsBranchInteractionTests
{
    private const ushort EmpoweredTowerQuest = 3486;
    private const uint ArrivedAtTowerObjective = 4987u;
    private const ushort TheTowerQuest = 3667;
    private const uint TheTowerTerminalObjective = 4770u;
    private const uint NorthernWildsWorld = 426u;
    private const uint ExoLabZone = 729u;
    private const uint LoftiteCrystalCreature = 11205u;
    private const uint LoftiteCrystalGuid = 1005u;
    private const uint LoftiteCrystalHealth = 55u;
    private const uint LoftiteVirtualItem = 206u;
    private const uint LoftiteCrystalWorldLocation = 7807u;
    private const float LoftiteCrystalRange = 5f;
    private const uint MasterControlPanelCreature = 11194u;
    private const uint MasterControlPanelWorldLocation = 7778u;
    private const float MasterControlPanelRange = 7f;
    private const uint FrostbiteCreature = 11924u;
    private const uint CrystalGuardianCreature = 11925u;
    private const ushort SpatialAnomalyQuest = 4526;
    private const uint SpatialAnomalyCatchObjective = 6165u;
    private const uint SpatialAnomalyWreckageObjective = 6164u;
    private const uint SpatialAnomalyWreckageWorldLocation = 9706u;
    private const uint SpatialAnomalyWorldLocation = 40764u;
    private const uint SpatialAnomalyCreature = 12535u;
    private const uint ScientistPristanCreature = 12560u;
    private const uint ScientistPristanReceiverWorldLocation = 11669u;
    private const float SpatialAnomalyCatchRange = 5f;
    private const uint DeadeyeBrightlandCreature = 11063u;
    private const uint DeadeyeBrightlandWorldSpawnCreature = 12959u;
    private const uint FromTheWreckageReportingForDutyDeadeyeReceiverWorldLocation = 7726u;
    private const uint CalmBeforeTheStormReceiverWorldLocation = 29526u;
    private const uint SettingUpCampReceiverWorldLocation = 9340u;
    private const uint IndigenousIntelligenceBartolSunwardCreature = 12737u;
    private const uint IndigenousIntelligenceBartolReceiverWorldLocation = 9121u;
    private const uint IndigenousIntelligenceScientistLuskCreature = 12484u;
    private const uint IndigenousIntelligenceScientistLuskWorldLocation = 8857u;
    private const uint IndigenousIntelligenceImprisonedSurvivorCreature = 14124u;
    private const uint IndigenousIntelligenceSkeechDivinerCreature = 11907u;
    private const uint IndigenousIntelligenceSkeechScratcherCreature = 11910u;
    private const uint IndigenousIntelligenceSkeechShamanCreature = 11912u;
    private const uint IndigenousIntelligenceSkeechFrostlordCreature = 11917u;
    private const uint IndigenousIntelligenceFierceSkeechScratcherCreature = 36429u;
    private const uint IndigenousIntelligenceSkeechDagunlordCreature = 17545u;
    private const uint IndigenousIntelligenceColdburrowXenobreederCreature = 36884u;
    private const uint YetiSnowstalkerCreature = 11945u;
    private const uint YetiFrostclawCreature = 11948u;
    private const uint FierceYetiIcefangCreature = 36331u;
    private const uint FierceYetiIcefangBossCreature = 36335u;
    private const uint BosunRedmarkCreature = 11062u;
    private const uint CommanderDurekStarterCreature = 11061u;
    private const uint LandsReachCommanderDurekCreature = 11066u;
    private const uint LandsReachCommanderDurekReceiverWorldLocation = 7727u;
    private const uint SecuringTheAreaRootbruteGrimsporeCreature = 12212u;
    private const uint SecuringTheAreaRootbruteDeathcapCreature = 12213u;
    private const uint ScatteredSuppliesExileSupplyCrateCreature = 12919u;
    private const uint FieryDistractionBurningTorchCreature = 13630u;
    private const uint FieryDistractionSkeechHutCreature = 13623u;
    private const uint CaptivesOfDominionDeadExileSoldierCreature = 50668u;
    private const uint CaptivesOfDominionCaptiveSoldierCreature = 12537u;
    private const uint ShellshockDominionCannonCreature = 11251u;
    private const uint ShellshockDominionCannonWorldLocation = 9196u;
    private const uint ShellshockDominionUltrabotCreature = 12526u;
    private const uint ShellshockDominionUltrabotWorldLocation = 9200u;
    private const uint MoreImportantThanRevengeShipControlsCreature = 27196u;
    private const uint MoreImportantThanRevengeShipControlsFirstWorldLocation = 45401u;
    private const uint MoreImportantThanRevengeShipControlsSecondWorldLocation = 45402u;
    private const uint ContactWithThaydSignalFlareOneCreature = 12521u;
    private const uint ContactWithThaydSignalFlareTwoCreature = 13150u;
    private const uint ContactWithThaydSignalFlareThreeCreature = 13151u;
    private const uint ContactWithThaydSignalFlareOneWorldLocation = 8867u;
    private const uint ContactWithThaydSignalFlareTwoWorldLocation = 8868u;
    private const uint ContactWithThaydSignalFlareThreeWorldLocation = 8869u;

    private static readonly Vector3[] ScatteredSuppliesExileSupplyCratePositions =
    [
        new(4582f, -734f, -5562f),
        new(4567f, -733f, -5588f),
        new(4558f, -732f, -5632f),
        new(4606f, -722f, -5647f),
        new(4489f, -743f, -5526f),
        new(4592f, -716f, -5731f),
        new(4572f, -720f, -5723f),
        new(4531f, -737f, -5623f),
        new(4546f, -742f, -5468f),
        new(4483f, -745f, -5465f),
        new(4625f, -728f, -5557f),
        new(4505f, -739f, -5585f),
        new(4526f, -736f, -5665f),
        new(4496f, -741f, -5714f),
        new(4479f, -739f, -5619f),
        new(4558f, -740f, -5510f),
        new(4411f, -738f, -5462f),
        new(4436f, -744f, -5495f),
        new(4617f, -735f, -5473f),
        new(4301f, -733f, -5525f),
        new(4326f, -735f, -5503f),
        new(4463f, -746f, -5492f),
        new(4387f, -742f, -5517f),
        new(4457f, -743f, -5547f),
        new(4620f, -731f, -5524f),
        new(4454f, -741f, -5457f)
    ];

    private static readonly Vector3[] FieryDistractionBurningTorchPositions =
    [
        new(4805f, -710f, -5661f),
        new(4805f, -712f, -5679f),
        new(4768f, -722f, -5648f),
        new(4742f, -728f, -5636f)
    ];

    private static readonly Vector3[] FieryDistractionSkeechHutPositions =
    [
        new(4659f, -726f, -5695f),
        new(4698f, -734f, -5678f),
        new(4796f, -712f, -5704f),
        new(4742f, -724f, -5728f),
        new(4683f, -727f, -5737f),
        new(4621f, -723f, -5717f),
        new(4747f, -727f, -5636f),
        new(4686f, -730f, -5629f),
        new(4735f, -731f, -5625f)
    ];

    [Fact]
    public void Q3486LoftiteCrystal_OnAddToMap_SetsBranchRange()
    {
        Q3486LoftiteCrystalEntityScript script = CreateLoftiteCrystalScript(
            out RecordingDispatchProxy<ICreatureEntity> ownerProxy);
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out _);

        script.OnAddToMap(map);

        RecordingDispatchProxy<ICreatureEntity>.Invocation range = Assert.Single(
            ownerProxy.GetInvocations(nameof(ICreatureEntity.SetInRangeCheck)));
        Assert.Equal(LoftiteCrystalRange, range.Arguments[0]);
    }

    [Fact]
    public void Q3486LoftiteCrystal_OnEnterRange_WhenQuestAccepted_CreditsArrivalFragmentAndDespawns()
    {
        Q3486LoftiteCrystalEntityScript script = CreateLoftiteCrystalScript(
            out RecordingDispatchProxy<ICreatureEntity> ownerProxy);
        IPlayer player = CreatePlayerWithQuestState(QuestState.Accepted, out RecordingDispatchProxy<IQuestManager> questManagerProxy);

        script.OnEnterRange(player);

        IReadOnlyList<RecordingDispatchProxy<IQuestManager>.Invocation> objectiveUpdates =
            questManagerProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate));
        Assert.Equal(2, objectiveUpdates.Count);

        Assert.Equal(ArrivedAtTowerObjective, objectiveUpdates[0].Arguments[0]);
        Assert.Equal(1u, objectiveUpdates[0].Arguments[1]);

        Assert.Equal(QuestObjectiveType.VirtualCollect, objectiveUpdates[1].Arguments[0]);
        Assert.Equal(LoftiteVirtualItem, objectiveUpdates[1].Arguments[1]);
        Assert.Equal(1u, objectiveUpdates[1].Arguments[2]);

        RecordingDispatchProxy<ICreatureEntity>.Invocation despawn = Assert.Single(
            ownerProxy.GetInvocations(nameof(ICreatureEntity.ModifyHealth)));
        Assert.Equal(LoftiteCrystalHealth, despawn.Arguments[0]);
        Assert.Equal(DamageType.Physical, despawn.Arguments[1]);
        Assert.Null(despawn.Arguments[2]);
    }

    [Fact]
    public void Q3486LoftiteCrystal_OnEnterRange_WhenQuestMissing_DoesNotCreditOrDespawn()
    {
        Q3486LoftiteCrystalEntityScript script = CreateLoftiteCrystalScript(
            out RecordingDispatchProxy<ICreatureEntity> ownerProxy);
        IPlayer player = CreatePlayerWithQuestState(null, out RecordingDispatchProxy<IQuestManager> questManagerProxy);

        script.OnEnterRange(player);

        Assert.Empty(questManagerProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)));
        Assert.Empty(ownerProxy.GetInvocations(nameof(ICreatureEntity.ModifyHealth)));
    }

    [Fact]
    public void Q3486LoftiteCrystal_Update_WhenAcceptedPlayerAlreadyInRange_CollectsCrystal()
    {
        IPlayer player = CreatePlayerWithQuestState(QuestState.Accepted, out RecordingDispatchProxy<IQuestManager> questManagerProxy);
        Q3486LoftiteCrystalEntityScript script = CreateLoftiteCrystalScript(
            out RecordingDispatchProxy<ICreatureEntity> ownerProxy,
            inRangePlayers: [player]);

        script.Update(0.1d);

        IReadOnlyList<RecordingDispatchProxy<IQuestManager>.Invocation> objectiveUpdates =
            questManagerProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate));
        Assert.Equal(2, objectiveUpdates.Count);

        Assert.Equal(ArrivedAtTowerObjective, objectiveUpdates[0].Arguments[0]);
        Assert.Equal(1u, objectiveUpdates[0].Arguments[1]);

        Assert.Equal(QuestObjectiveType.VirtualCollect, objectiveUpdates[1].Arguments[0]);
        Assert.Equal(LoftiteVirtualItem, objectiveUpdates[1].Arguments[1]);
        Assert.Equal(1u, objectiveUpdates[1].Arguments[2]);

        RecordingDispatchProxy<ICreatureEntity>.Invocation despawn = Assert.Single(
            ownerProxy.GetInvocations(nameof(ICreatureEntity.ModifyHealth)));
        Assert.Equal(LoftiteCrystalHealth, despawn.Arguments[0]);
    }

    [Fact]
    public void Q3486LoftiteCrystal_Update_AfterCollected_DoesNotGrantAgain()
    {
        IPlayer player = CreatePlayerWithQuestState(QuestState.Accepted, out RecordingDispatchProxy<IQuestManager> questManagerProxy);
        Q3486LoftiteCrystalEntityScript script = CreateLoftiteCrystalScript(
            out RecordingDispatchProxy<ICreatureEntity> ownerProxy,
            inRangePlayers: [player]);

        script.Update(0.1d);
        script.Update(0.1d);

        Assert.Equal(2, questManagerProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)).Count);
        Assert.Single(ownerProxy.GetInvocations(nameof(ICreatureEntity.ModifyHealth)));
    }

    [Fact]
    public void Q3486LoftiteCrystal_OnEnterRange_WhenEntityIsNotPlayer_DoesNothing()
    {
        Q3486LoftiteCrystalEntityScript script = CreateLoftiteCrystalScript(
            out RecordingDispatchProxy<ICreatureEntity> ownerProxy);
        ICreatureEntity creature = RecordingDispatchProxy<ICreatureEntity>.Create(out _);

        script.OnEnterRange(creature);

        Assert.Empty(ownerProxy.GetInvocations(nameof(ICreatureEntity.ModifyHealth)));
    }

    [Fact]
    public void Q3667ControlPanel_OnAddToMap_SetsBranchRange()
    {
        Q3667ControlPanelEntityScript script = CreateControlPanelScript(
            out RecordingDispatchProxy<ICreatureEntity> ownerProxy);
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out _);

        script.OnAddToMap(map);

        RecordingDispatchProxy<ICreatureEntity>.Invocation range = Assert.Single(
            ownerProxy.GetInvocations(nameof(ICreatureEntity.SetInRangeCheck)));
        Assert.Equal(MasterControlPanelRange, range.Arguments[0]);
    }

    [Fact]
    public void Q3667ControlPanel_OnActivateSuccess_WhenQuestAccepted_CreditsTerminalObjective()
    {
        Q3667ControlPanelEntityScript script = CreateControlPanelScript(out _);
        IPlayer player = CreatePlayerWithQuestState(
            QuestState.Accepted,
            out RecordingDispatchProxy<IQuestManager> questManagerProxy,
            questId: TheTowerQuest);

        script.OnActivateSuccess(player);

        RecordingDispatchProxy<IQuestManager>.Invocation update = Assert.Single(
            questManagerProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)));
        Assert.Equal(TheTowerTerminalObjective, update.Arguments[0]);
        Assert.Equal(1u, update.Arguments[1]);
    }

    [Fact]
    public void Q3667ControlPanel_OnEnterRange_WhenQuestAccepted_CreditsTerminalObjective()
    {
        Q3667ControlPanelEntityScript script = CreateControlPanelScript(out _);
        IPlayer player = CreatePlayerWithQuestState(
            QuestState.Accepted,
            out RecordingDispatchProxy<IQuestManager> questManagerProxy,
            questId: TheTowerQuest);

        script.OnEnterRange(player);

        RecordingDispatchProxy<IQuestManager>.Invocation update = Assert.Single(
            questManagerProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)));
        Assert.Equal(TheTowerTerminalObjective, update.Arguments[0]);
        Assert.Equal(1u, update.Arguments[1]);
    }

    [Fact]
    public void Q3667ControlPanel_OnActivateSuccess_WhenQuestMissing_DoesNotCreditTerminalObjective()
    {
        Q3667ControlPanelEntityScript script = CreateControlPanelScript(out _);
        IPlayer player = CreatePlayerWithQuestState(
            null,
            out RecordingDispatchProxy<IQuestManager> questManagerProxy,
            questId: TheTowerQuest);

        script.OnActivateSuccess(player);

        Assert.Empty(questManagerProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)));
    }

    [Fact]
    public void Q4526SpatialAnomaly_OnAddToMap_SetsCatchRange()
    {
        Q4526SpatialAnomalyEntityScript script = CreateSpatialAnomalyScript(
            out RecordingDispatchProxy<ICreatureEntity> ownerProxy);
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out _);

        script.OnAddToMap(map);

        RecordingDispatchProxy<ICreatureEntity>.Invocation range = Assert.Single(
            ownerProxy.GetInvocations(nameof(ICreatureEntity.SetInRangeCheck)));
        Assert.Equal(SpatialAnomalyCatchRange, range.Arguments[0]);
    }

    [Fact]
    public void Q4526SpatialAnomaly_OnActivateSuccess_WhenQuestAccepted_CreditsCatchObjective()
    {
        var script = new Q4526SpatialAnomalyEntityScript();
        ICreatureEntity owner = RecordingDispatchProxy<ICreatureEntity>.Create(out _);
        IPlayer player = CreatePlayerWithQuestState(
            QuestState.Accepted,
            out RecordingDispatchProxy<IQuestManager> questManagerProxy,
            questId: SpatialAnomalyQuest);

        script.OnLoad(owner);
        script.OnActivateSuccess(player);

        RecordingDispatchProxy<IQuestManager>.Invocation update = Assert.Single(
            questManagerProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)));
        Assert.Equal(SpatialAnomalyCatchObjective, update.Arguments[0]);
        Assert.Equal(1u, update.Arguments[1]);
    }

    [Fact]
    public void Q4526SpatialAnomaly_OnEnterRange_WhenQuestAccepted_CreditsCatchObjective()
    {
        Q4526SpatialAnomalyEntityScript script = CreateSpatialAnomalyScript(out _);
        IPlayer player = CreatePlayerWithQuestState(
            QuestState.Accepted,
            out RecordingDispatchProxy<IQuestManager> questManagerProxy,
            questId: SpatialAnomalyQuest);

        script.OnEnterRange(player);

        RecordingDispatchProxy<IQuestManager>.Invocation update = Assert.Single(
            questManagerProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)));
        Assert.Equal(SpatialAnomalyCatchObjective, update.Arguments[0]);
        Assert.Equal(1u, update.Arguments[1]);
    }

    [Fact]
    public void Q4526SpatialAnomaly_OnEnterRangeThenActivateSuccess_WhenSamePlayer_CreditsCatchObjectiveOnce()
    {
        Q4526SpatialAnomalyEntityScript script = CreateSpatialAnomalyScript(out _);
        IPlayer player = CreatePlayerWithQuestState(
            QuestState.Accepted,
            out RecordingDispatchProxy<IQuestManager> questManagerProxy,
            questId: SpatialAnomalyQuest);

        script.OnEnterRange(player);
        script.OnActivateSuccess(player);

        RecordingDispatchProxy<IQuestManager>.Invocation update = Assert.Single(
            questManagerProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)));
        Assert.Equal(SpatialAnomalyCatchObjective, update.Arguments[0]);
        Assert.Equal(1u, update.Arguments[1]);
    }

    [Fact]
    public void Q4526SpatialAnomaly_OnEnterRange_WhenQuestMissing_DoesNotCreditCatchObjective()
    {
        Q4526SpatialAnomalyEntityScript script = CreateSpatialAnomalyScript(out _);
        IPlayer player = CreatePlayerWithQuestState(
            null,
            out RecordingDispatchProxy<IQuestManager> questManagerProxy,
            questId: SpatialAnomalyQuest);

        script.OnEnterRange(player);

        Assert.Empty(questManagerProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)));
    }

    [Fact]
    public void Q3486CrystalGuardian_OnKilled_WhenQuestAccepted_CreditsArrivalAndLoftiteFragment()
    {
        var script = new Q3486CrystalGuardianEntityScript();
        ICreatureEntity owner = RecordingDispatchProxy<ICreatureEntity>.Create(out _);
        IPlayer killer = CreatePlayerWithQuestState(QuestState.Accepted, out RecordingDispatchProxy<IQuestManager> questManagerProxy);

        script.OnLoad(owner);
        script.OnKilled(killer);

        IReadOnlyList<RecordingDispatchProxy<IQuestManager>.Invocation> objectiveUpdates =
            questManagerProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate));
        Assert.Equal(2, objectiveUpdates.Count);

        RecordingDispatchProxy<IQuestManager>.Invocation arrival = objectiveUpdates[0];
        Assert.Equal(ArrivedAtTowerObjective, arrival.Arguments[0]);
        Assert.Equal(1u, arrival.Arguments[1]);

        RecordingDispatchProxy<IQuestManager>.Invocation fragment = objectiveUpdates[1];
        Assert.Equal(QuestObjectiveType.VirtualCollect, fragment.Arguments[0]);
        Assert.Equal(LoftiteVirtualItem, fragment.Arguments[1]);
        Assert.Equal(1u, fragment.Arguments[2]);
    }

    [Fact]
    public void Q3486CrystalGuardian_Filter_IncludesFrostbiteTargetGroupMember()
    {
        ScriptFilterCreatureIdAttribute attribute = typeof(Q3486CrystalGuardianEntityScript)
            .GetCustomAttribute<ScriptFilterCreatureIdAttribute>();

        Assert.NotNull(attribute);
        Assert.Contains(FrostbiteCreature, attribute.CreatureId);
        Assert.Contains(CrystalGuardianCreature, attribute.CreatureId);
    }

    [Fact]
    public void Q3486CrystalGuardian_OnKilled_WhenQuestMissing_DoesNotCreditArrivalObjective()
    {
        var script = new Q3486CrystalGuardianEntityScript();
        ICreatureEntity owner = RecordingDispatchProxy<ICreatureEntity>.Create(out _);
        IPlayer killer = CreatePlayerWithQuestState(null, out RecordingDispatchProxy<IQuestManager> questManagerProxy);

        script.OnLoad(owner);
        script.OnKilled(killer);

        Assert.Empty(questManagerProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)));
    }

    [Fact]
    public void Q3486CrystalGuardian_OnKilled_WhenKillerIsNotPlayer_DoesNothing()
    {
        var script = new Q3486CrystalGuardianEntityScript();
        ICreatureEntity owner = RecordingDispatchProxy<ICreatureEntity>.Create(out _);
        ICreatureEntity killer = RecordingDispatchProxy<ICreatureEntity>.Create(out _);

        script.OnLoad(owner);
        script.OnKilled(killer);
    }

    [Fact]
    public void NorthernWildsMapScript_Update_WhenQ3486AcceptedAndPlayerOverlapsCrystalVolume_CollectsCrystal()
    {
        IPlayer player = CreatePlayerWithQuestState(
            QuestState.Accepted,
            out RecordingDispatchProxy<IQuestManager> questManagerProxy,
            position: Vector3.Zero,
            zoneId: ExoLabZone,
            hitRadius: 2f);
        ICreatureEntity crystal = CreateLoftiteCrystal(
            position: new Vector3(6f, 9f, 0f),
            hitRadius: 10f,
            out RecordingDispatchProxy<ICreatureEntity> crystalProxy);
        NorthernWildsMapScript script = CreateNorthernWildsMapScript([player], [crystal]);

        script.Update(0.1d);

        IReadOnlyList<RecordingDispatchProxy<IQuestManager>.Invocation> objectiveUpdates =
            questManagerProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate));
        Assert.Equal(2, objectiveUpdates.Count);

        Assert.Equal(ArrivedAtTowerObjective, objectiveUpdates[0].Arguments[0]);
        Assert.Equal(1u, objectiveUpdates[0].Arguments[1]);

        Assert.Equal(QuestObjectiveType.VirtualCollect, objectiveUpdates[1].Arguments[0]);
        Assert.Equal(LoftiteVirtualItem, objectiveUpdates[1].Arguments[1]);
        Assert.Equal(1u, objectiveUpdates[1].Arguments[2]);

        RecordingDispatchProxy<ICreatureEntity>.Invocation despawn = Assert.Single(
            crystalProxy.GetInvocations(nameof(ICreatureEntity.ModifyHealth)));
        Assert.Equal(LoftiteCrystalHealth, despawn.Arguments[0]);
    }

    [Fact]
    public void NorthernWildsMapScript_Update_WhenQ3486Missing_DoesNotCollectCrystal()
    {
        IPlayer player = CreatePlayerWithQuestState(
            null,
            out RecordingDispatchProxy<IQuestManager> questManagerProxy,
            position: Vector3.Zero,
            zoneId: ExoLabZone,
            hitRadius: 2f);
        ICreatureEntity crystal = CreateLoftiteCrystal(
            position: new Vector3(6f, 9f, 0f),
            hitRadius: 10f,
            out RecordingDispatchProxy<ICreatureEntity> crystalProxy);
        NorthernWildsMapScript script = CreateNorthernWildsMapScript([player], [crystal]);

        script.Update(0.1d);

        Assert.Empty(questManagerProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)));
        Assert.Empty(crystalProxy.GetInvocations(nameof(ICreatureEntity.ModifyHealth)));
    }

    [Fact]
    public void NorthernWildsMapScript_Update_AfterCrystalCollected_DoesNotCollectAgain()
    {
        IPlayer player = CreatePlayerWithQuestState(
            QuestState.Accepted,
            out RecordingDispatchProxy<IQuestManager> questManagerProxy,
            position: Vector3.Zero,
            zoneId: ExoLabZone,
            hitRadius: 2f);
        ICreatureEntity crystal = CreateLoftiteCrystal(
            position: new Vector3(6f, 9f, 0f),
            hitRadius: 10f,
            out RecordingDispatchProxy<ICreatureEntity> crystalProxy);
        NorthernWildsMapScript script = CreateNorthernWildsMapScript([player], [crystal]);

        script.Update(0.1d);
        script.Update(0.1d);

        Assert.Equal(2, questManagerProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)).Count);
        Assert.Single(crystalProxy.GetInvocations(nameof(ICreatureEntity.ModifyHealth)));
    }

    [Fact]
    public void NorthernWildsMapScript_Update_WhenQ4526AcceptedAndPlayerAtWreckage_CreditsInvestigationObjective()
    {
        IPlayer player = CreatePlayerWithQuestState(
            QuestState.Accepted,
            out RecordingDispatchProxy<IQuestManager> questManagerProxy,
            position: new Vector3(4536.17f, -715.836f, -5801.43f),
            hitRadius: 1f,
            questId: SpatialAnomalyQuest);
        NorthernWildsMapScript script = CreateNorthernWildsMapScript(
            [player],
            [],
            CreateWorldLocation(SpatialAnomalyWreckageWorldLocation, 1f, 4536.17f, -715.836f, -5801.43f));

        script.Update(0.1d);

        RecordingDispatchProxy<IQuestManager>.Invocation update = Assert.Single(
            questManagerProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)));
        Assert.Equal(SpatialAnomalyWreckageObjective, update.Arguments[0]);
        Assert.Equal(1u, update.Arguments[1]);
    }

    [Fact]
    public void NorthernWildsMapScript_Update_WhenQ4526Missing_DoesNotCreditInvestigationObjective()
    {
        IPlayer player = CreatePlayerWithQuestState(
            null,
            out RecordingDispatchProxy<IQuestManager> questManagerProxy,
            position: new Vector3(4536.17f, -715.836f, -5801.43f),
            hitRadius: 1f,
            questId: SpatialAnomalyQuest);
        NorthernWildsMapScript script = CreateNorthernWildsMapScript(
            [player],
            [],
            CreateWorldLocation(SpatialAnomalyWreckageWorldLocation, 1f, 4536.17f, -715.836f, -5801.43f));

        script.Update(0.1d);

        Assert.Empty(questManagerProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)));
    }

    [Fact]
    public void NorthernWildsMapScript_OnAddToMap_ActivatesCurrentZonePathEpisode()
    {
        IPlayer player = CreatePlayerWithPathManager(out RecordingDispatchProxy<IPathManager> pathManagerProxy);
        NorthernWildsMapScript script = CreateNorthernWildsMapScript([player], []);

        script.OnAddToMap(player);

        RecordingDispatchProxy<IPathManager>.Invocation activation =
            Assert.Single(pathManagerProxy.GetInvocations(nameof(IPathManager.TryActivateCurrentZoneEpisode)));
        Assert.Empty(activation.Arguments);
        Assert.Empty(pathManagerProxy.GetInvocations(nameof(IPathManager.ActivateMissions)));
    }

    [Fact]
    public void NorthernWildsMapScript_OnEnterZone_ActivatesCurrentZonePathEpisode()
    {
        IPlayer player = CreatePlayerWithPathManager(out RecordingDispatchProxy<IPathManager> pathManagerProxy);
        NorthernWildsMapScript script = CreateNorthernWildsMapScript([player], []);

        script.OnEnterZone(player, zone: 647u);

        RecordingDispatchProxy<IPathManager>.Invocation activation =
            Assert.Single(pathManagerProxy.GetInvocations(nameof(IPathManager.TryActivateCurrentZoneEpisode)));
        Assert.Equal(647u, activation.Arguments[0]);
        Assert.Empty(pathManagerProxy.GetInvocations(nameof(IPathManager.ActivateMissions)));
    }

    [Theory]
    [InlineData(typeof(NorthernWildsScientistTurningTheTideEntityScript), 34u)]
    [InlineData(typeof(NorthernWildsScientistVitaliumCrystalEntityScript), 130u)]
    [InlineData(typeof(NorthernWildsScientistSkeechPhysiologyEntityScript), 128u)]
    public void NorthernWildsScientistPathScripts_OnVisibleAndActivate_ProgressTableBackedScanMission(
        Type scriptType,
        uint expectedCreatureInfoId)
    {
        ICreatureEntity owner = RecordingDispatchProxy<ICreatureEntity>.Create(out RecordingDispatchProxy<ICreatureEntity> ownerProxy);
        ownerProxy.SetProperty(nameof(ICreatureEntity.Guid), 15888u);
        NorthernWildsScientistPathMissionEntityScript script = (NorthernWildsScientistPathMissionEntityScript)Activator.CreateInstance(scriptType)!;
        script.OnLoad(owner);
        IPlayer player = CreatePlayerWithPath(
            PlayerPath.Scientist,
            out RecordingDispatchProxy<IPathManager> pathManagerProxy,
            out RecordingDispatchProxy<IGameSession> sessionProxy);

        script.OnAddVisibleEntity(player);
        script.OnActivateSuccess(player);

        IReadOnlyList<object> messages = sessionProxy
            .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(i => i.Arguments[0])
            .ToList();
        ServerPathScientistAddCreatureInfoToCreature addCreatureInfo =
            Assert.IsType<ServerPathScientistAddCreatureInfoToCreature>(messages[0]);
        Assert.Equal(15888u, addCreatureInfo.UnitId);
        Assert.Equal(expectedCreatureInfoId, addCreatureInfo.PathScientistCreatureInfoId);
        ServerPathScientistUnitScanParameters scanParameters =
            Assert.IsType<ServerPathScientistUnitScanParameters>(messages[1]);
        Assert.Equal(15888u, scanParameters.UnitId);
        Assert.Equal(ScanReward.RewardForScan, scanParameters.ScanRewardFlags);
        Assert.True(scanParameters.IsScannable);

        RecordingDispatchProxy<IPathManager>.Invocation scanCredit =
            Assert.Single(pathManagerProxy.GetInvocations(nameof(IPathManager.ProgressScientistCreatureScanMission)));
        Assert.Equal(expectedCreatureInfoId, scanCredit.Arguments[0]);
        Assert.Empty(pathManagerProxy.GetInvocations(nameof(IPathManager.MarkScientistCreatureScanned)));
        Assert.Empty(pathManagerProxy.GetInvocations(nameof(IPathManager.CompleteMission)));
    }

    [Theory]
    [InlineData(typeof(NorthernWildsColdburrowSkeechHoldoutEntityScript), 12, 10000, 45000, 180000)]
    [InlineData(typeof(NorthernWildsDominionHoldoutEntityScript), 13, 3000, 20000, 60000)]
    public void NorthernWildsSoldierPathScripts_OnActivate_RunTableBackedHoldoutLifecycle(
        Type scriptType,
        ushort expectedPathSoldierEventId,
        int initialSpawnMilliseconds,
        int maxTimeBetweenWavesMilliseconds,
        int maxEventMilliseconds)
    {
        NorthernWildsSoldierHoldoutEntityScript script = CreateSoldierHoldoutScript(scriptType);
        IPlayer player = CreatePlayerWithPath(
            PlayerPath.Soldier,
            out RecordingDispatchProxy<IPathManager> pathManagerProxy,
            out RecordingDispatchProxy<IGameSession> sessionProxy);
        pathManagerProxy.SetMethodHandler(nameof(IPathManager.IsMissionActiveByObjectId),
            args => (uint)args[0] == expectedPathSoldierEventId);
        pathManagerProxy.SetMethodHandler(nameof(IPathManager.CompleteMissionByObjectId),
            args => (uint)args[0] == expectedPathSoldierEventId);

        script.OnActivateSuccess(player);

        RecordingDispatchProxy<IPathManager>.Invocation activeCheck = Assert.Single(
            pathManagerProxy.GetInvocations(nameof(IPathManager.IsMissionActiveByObjectId)));
        Assert.Equal((uint)expectedPathSoldierEventId, activeCheck.Arguments[0]);
        Assert.Empty(pathManagerProxy.GetInvocations(nameof(IPathManager.CompleteMissionByObjectId)));
        IReadOnlyList<object> messages = GetSessionMessages(sessionProxy);
        ServerPathSoldierHoldoutStatus initialStatus =
            Assert.IsType<ServerPathSoldierHoldoutStatus>(Assert.Single(messages));
        Assert.Equal(expectedPathSoldierEventId, initialStatus.PathSoldierEventId);
        Assert.Equal(PlayerPathSoldierEventMode.InitialDelay, initialStatus.Mode);
        Assert.Equal(initialSpawnMilliseconds, initialStatus.DelayTime);
        Assert.Equal(0, initialStatus.WaveIndex);
        Assert.Equal(0, initialStatus.StartTimeOffset);

        script.Update(initialSpawnMilliseconds / 1000d);

        messages = GetSessionMessages(sessionProxy);
        ServerPathSoldierHoldoutStatus activeStatus =
            Assert.IsType<ServerPathSoldierHoldoutStatus>(messages[1]);
        Assert.Equal(expectedPathSoldierEventId, activeStatus.PathSoldierEventId);
        Assert.Equal(PlayerPathSoldierEventMode.Active, activeStatus.Mode);
        Assert.Equal(maxEventMilliseconds, activeStatus.DelayTime);
        Assert.Equal(0, activeStatus.StartTimeOffset);
        ServerPathSoldierHoldOutNextWave firstWave =
            Assert.IsType<ServerPathSoldierHoldOutNextWave>(messages[2]);
        Assert.Equal(expectedPathSoldierEventId, firstWave.PathSoldierEventId);
        Assert.Equal(0u, firstWave.WaveIndex);
        Assert.False(firstWave.IsBoss);
        Assert.Empty(pathManagerProxy.GetInvocations(nameof(IPathManager.CompleteMissionByObjectId)));

        int messageCountBeforeSecondWave = messages.Count;
        script.Update(maxTimeBetweenWavesMilliseconds / 1000d);

        messages = GetSessionMessages(sessionProxy);
        ServerPathSoldierHoldOutNextWave secondWave =
            Assert.IsType<ServerPathSoldierHoldOutNextWave>(messages[messageCountBeforeSecondWave]);
        Assert.Equal(expectedPathSoldierEventId, secondWave.PathSoldierEventId);
        Assert.Equal(1u, secondWave.WaveIndex);
        Assert.False(secondWave.IsBoss);

        script.Update((maxEventMilliseconds - initialSpawnMilliseconds - maxTimeBetweenWavesMilliseconds) / 1000d);

        Assert.Empty(pathManagerProxy.GetInvocations(nameof(IPathManager.CompleteMissionByObjectId)));

        script.Update(initialSpawnMilliseconds / 1000d);

        RecordingDispatchProxy<IPathManager>.Invocation completion = Assert.Single(
            pathManagerProxy.GetInvocations(nameof(IPathManager.CompleteMissionByObjectId)));
        Assert.Equal((uint)expectedPathSoldierEventId, completion.Arguments[0]);
        ServerPathSoldierHoldoutEnd end = Assert.IsType<ServerPathSoldierHoldoutEnd>(
            GetSessionMessages(sessionProxy).Last());
        Assert.Equal(expectedPathSoldierEventId, end.PathSoldierEventId);
        Assert.Equal(PlayerPathSoldierResult.Success, end.Reason);
    }

    [Fact]
    public void NorthernWildsSoldierPathScripts_CanAttachToSimpleControlPointEntities()
    {
        Assert.True(typeof(IOwnedScript<ISimpleEntity>).IsAssignableFrom(typeof(NorthernWildsYetiHoldoutEntityScript)));
    }

    [Fact]
    public void NorthernWildsYetiHoldout_OnActivate_RemovesPreExistingIcefangSpawns()
    {
        IWorldEntity staleIcefang = CreateWorldEntity(
            FierceYetiIcefangCreature,
            9001u,
            new Vector3(4160f, -743f, -5526f),
            inWorld: true,
            out RecordingDispatchProxy<IWorldEntity> staleIcefangProxy);
        IWorldEntity farIcefang = CreateWorldEntity(
            FierceYetiIcefangCreature,
            9002u,
            new Vector3(4300f, -743f, -5526f),
            inWorld: true,
            out RecordingDispatchProxy<IWorldEntity> farIcefangProxy);
        IWorldEntity boss = CreateWorldEntity(
            FierceYetiIcefangBossCreature,
            9003u,
            new Vector3(4157f, -744f, -5536f),
            inWorld: true,
            out RecordingDispatchProxy<IWorldEntity> bossProxy);

        IBaseMap map = CreateHoldoutMap(
            [staleIcefang, farIcefang, boss],
            out _);
        ISimpleEntity owner = CreateHoldoutOwner(map);
        NorthernWildsYetiHoldoutEntityScript script = new();
        script.OnLoad(owner);

        IPlayer player = CreatePlayerWithPath(
            PlayerPath.Soldier,
            out RecordingDispatchProxy<IPathManager> pathManagerProxy,
            out _);
        pathManagerProxy.SetMethodHandler(nameof(IPathManager.IsMissionActiveByObjectId),
            args => (uint)args[0] == 2u);

        script.OnActivateSuccess(player);

        Assert.Single(staleIcefangProxy.GetInvocations(nameof(IWorldEntity.RemoveFromMap)));
        Assert.Empty(farIcefangProxy.GetInvocations(nameof(IWorldEntity.RemoveFromMap)));
        Assert.Single(bossProxy.GetInvocations(nameof(IWorldEntity.RemoveFromMap)));
    }

    [Fact]
    public void NorthernWildsYetiHoldout_OnActivate_WhenMissionAlreadyComplete_RemovesPreExistingEventSpawnsOnly()
    {
        IWorldEntity staleBoss = CreateWorldEntity(
            FierceYetiIcefangBossCreature,
            9003u,
            new Vector3(4157f, -744f, -5536f),
            inWorld: true,
            out RecordingDispatchProxy<IWorldEntity> staleBossProxy);

        IBaseMap map = CreateHoldoutMap([staleBoss], out _);
        ISimpleEntity owner = CreateHoldoutOwner(map);
        NorthernWildsYetiHoldoutEntityScript script = new();
        script.OnLoad(owner);

        IPlayer player = CreatePlayerWithPath(
            PlayerPath.Soldier,
            out RecordingDispatchProxy<IPathManager> pathManagerProxy,
            out RecordingDispatchProxy<IGameSession> sessionProxy);
        pathManagerProxy.SetMethodHandler(nameof(IPathManager.IsMissionCompleteByObjectId),
            args => (uint)args[0] == 2u);
        pathManagerProxy.SetMethodHandler(nameof(IPathManager.IsMissionActiveByObjectId),
            args => (uint)args[0] == 2u);

        script.OnActivateSuccess(player);

        Assert.Single(staleBossProxy.GetInvocations(nameof(IWorldEntity.RemoveFromMap)));
        Assert.Empty(pathManagerProxy.GetInvocations(nameof(IPathManager.IsMissionActiveByObjectId)));
        Assert.Empty(GetSessionMessages(sessionProxy));
    }

    [Fact]
    public void NorthernWildsYetiHoldout_Update_SpawnsIcefangWavesAndBoss()
    {
        var createdEntities = new List<INonPlayerEntity>();
        var createdEntityProxies = new List<RecordingDispatchProxy<INonPlayerEntity>>();
        var createdMovementProxies = new List<RecordingDispatchProxy<IMovementManager>>();
        IEntityFactory entityFactory = RecordingDispatchProxy<IEntityFactory>.Create(out RecordingDispatchProxy<IEntityFactory> entityFactoryProxy);
        entityFactoryProxy.SetMethodReturnFactory(nameof(IEntityFactory.CreateEntity), () =>
        {
            INonPlayerEntity entity = RecordingDispatchProxy<INonPlayerEntity>.Create(out RecordingDispatchProxy<INonPlayerEntity> entityProxy);
            IMovementManager movementManager = RecordingDispatchProxy<IMovementManager>.Create(out RecordingDispatchProxy<IMovementManager> movementProxy);
            entityProxy.SetProperty(nameof(INonPlayerEntity.InWorld), true);
            entityProxy.SetProperty(nameof(INonPlayerEntity.MovementManager), movementManager);
            entityProxy.SetMethodHandler(nameof(INonPlayerEntity.Initialise), args =>
            {
                entityProxy.SetProperty(nameof(INonPlayerEntity.CreatureId), args[0]);
                return null;
            });
            createdEntities.Add(entity);
            createdEntityProxies.Add(entityProxy);
            createdMovementProxies.Add(movementProxy);
            return entity;
        });

        IBaseMap map = CreateHoldoutMap([], out RecordingDispatchProxy<IBaseMap> mapProxy);
        ISimpleEntity owner = CreateHoldoutOwner(map);
        var script = new NorthernWildsYetiHoldoutEntityScript(entityFactory);
        script.OnLoad(owner);

        IPlayer player = CreatePlayerWithPath(
            PlayerPath.Soldier,
            out RecordingDispatchProxy<IPathManager> pathManagerProxy,
            out RecordingDispatchProxy<IGameSession> sessionProxy);
        pathManagerProxy.SetMethodHandler(nameof(IPathManager.IsMissionActiveByObjectId),
            args => (uint)args[0] == 2u);
        pathManagerProxy.SetMethodHandler(nameof(IPathManager.CompleteMissionByObjectId),
            args => (uint)args[0] == 2u);

        script.OnActivateSuccess(player);
        script.Update(7d);
        script.Update(45d);
        script.Update(45d);
        script.Update(45d);

        Assert.Equal(
            [FierceYetiIcefangCreature, FierceYetiIcefangCreature, FierceYetiIcefangCreature, FierceYetiIcefangCreature, FierceYetiIcefangCreature, FierceYetiIcefangCreature, FierceYetiIcefangCreature, FierceYetiIcefangBossCreature],
            createdEntityProxies
                .Select(e => (uint)e.GetInvocations(nameof(INonPlayerEntity.Initialise)).Single().Arguments[0])
                .ToArray());

        Assert.Equal(8, mapProxy.GetInvocations(nameof(IBaseMap.EnqueueAdd)).Count);
        foreach (RecordingDispatchProxy<INonPlayerEntity> entityProxy in createdEntityProxies)
            Assert.Contains(entityProxy.GetInvocations("set_" + nameof(INonPlayerEntity.CreateFlags)),
                i => ((EntityCreateFlag)i.Arguments[0]).HasFlag(EntityCreateFlag.Immediate));
        Assert.Empty(createdMovementProxies.Take(7).SelectMany(p => p.GetInvocations(nameof(IMovementManager.SetScale))));
        RecordingDispatchProxy<IMovementManager>.Invocation bossScale = Assert.Single(
            createdMovementProxies.Last().GetInvocations(nameof(IMovementManager.SetScale)));
        Assert.Equal(1.5f, (float)bossScale.Arguments[0]);

        ServerPathSoldierHoldOutNextWave bossWave = Assert.IsType<ServerPathSoldierHoldOutNextWave>(
            GetSessionMessages(sessionProxy).OfType<ServerPathSoldierHoldOutNextWave>().Last());
        Assert.Equal(3u, bossWave.WaveIndex);
        Assert.True(bossWave.IsBoss);
        Assert.Empty(pathManagerProxy.GetInvocations(nameof(IPathManager.CompleteMissionByObjectId)));
        Assert.Empty(GetSessionMessages(sessionProxy).OfType<ServerPathSoldierHoldoutEnd>());

        var bossScript = new NorthernWildsYetiHoldoutSpawnEntityScript();
        bossScript.OnLoad(createdEntities.Last());
        bossScript.OnKilled(player);

        RecordingDispatchProxy<IPathManager>.Invocation completion = Assert.Single(
            pathManagerProxy.GetInvocations(nameof(IPathManager.CompleteMissionByObjectId)));
        Assert.Equal(2u, completion.Arguments[0]);
        ServerPathSoldierHoldoutEnd end = Assert.IsType<ServerPathSoldierHoldoutEnd>(
            GetSessionMessages(sessionProxy).Last());
        Assert.Equal(2u, end.PathSoldierEventId);
        Assert.Equal(PlayerPathSoldierResult.Success, end.Reason);
        Assert.Equal(8, createdEntityProxies
            .Sum(e => e.GetInvocations(nameof(INonPlayerEntity.RemoveFromMap)).Count));
        Assert.Single(createdEntityProxies.Last().GetInvocations(nameof(INonPlayerEntity.RemoveFromMap)));
    }

    [Fact]
    public void NorthernWildsYetiHoldoutSpawn_OnKilled_RemovesWaveCreatureWithoutCompletingHoldout()
    {
        var createdEntities = new List<INonPlayerEntity>();
        var createdEntityProxies = new List<RecordingDispatchProxy<INonPlayerEntity>>();
        IEntityFactory entityFactory = RecordingDispatchProxy<IEntityFactory>.Create(out RecordingDispatchProxy<IEntityFactory> entityFactoryProxy);
        entityFactoryProxy.SetMethodReturnFactory(nameof(IEntityFactory.CreateEntity), () =>
        {
            INonPlayerEntity entity = RecordingDispatchProxy<INonPlayerEntity>.Create(out RecordingDispatchProxy<INonPlayerEntity> entityProxy);
            entityProxy.SetProperty(nameof(INonPlayerEntity.InWorld), true);
            entityProxy.SetProperty(nameof(INonPlayerEntity.MovementManager), RecordingDispatchProxy<IMovementManager>.Create(out _));
            entityProxy.SetMethodHandler(nameof(INonPlayerEntity.Initialise), args =>
            {
                entityProxy.SetProperty(nameof(INonPlayerEntity.CreatureId), args[0]);
                return null;
            });
            createdEntities.Add(entity);
            createdEntityProxies.Add(entityProxy);
            return entity;
        });

        IBaseMap map = CreateHoldoutMap([], out _);
        ISimpleEntity owner = CreateHoldoutOwner(map);
        var script = new NorthernWildsYetiHoldoutEntityScript(entityFactory);
        script.OnLoad(owner);

        IPlayer player = CreatePlayerWithPath(
            PlayerPath.Soldier,
            out RecordingDispatchProxy<IPathManager> pathManagerProxy,
            out RecordingDispatchProxy<IGameSession> sessionProxy);
        pathManagerProxy.SetMethodHandler(nameof(IPathManager.IsMissionActiveByObjectId),
            args => (uint)args[0] == 2u);

        script.OnActivateSuccess(player);
        script.Update(7d);

        var spawnScript = new NorthernWildsYetiHoldoutSpawnEntityScript();
        spawnScript.OnLoad(createdEntities[0]);
        spawnScript.OnKilled(player);

        Assert.Single(createdEntityProxies[0].GetInvocations(nameof(INonPlayerEntity.RemoveFromMap)));
        Assert.Empty(pathManagerProxy.GetInvocations(nameof(IPathManager.CompleteMissionByObjectId)));
        Assert.Empty(GetSessionMessages(sessionProxy).OfType<ServerPathSoldierHoldoutEnd>());
    }

    [Fact]
    public void NorthernWildsSoldierPathScripts_OnActivate_WhenMissionCannotActivate_DoesNothing()
    {
        NorthernWildsSoldierHoldoutEntityScript script = CreateSoldierHoldoutScript(typeof(NorthernWildsYetiHoldoutEntityScript));
        IPlayer player = CreatePlayerWithPath(
            PlayerPath.Soldier,
            out RecordingDispatchProxy<IPathManager> pathManagerProxy,
            out RecordingDispatchProxy<IGameSession> sessionProxy);

        script.OnActivateSuccess(player);
        script.Update(180d);

        RecordingDispatchProxy<IPathManager>.Invocation activeCheck = Assert.Single(
            pathManagerProxy.GetInvocations(nameof(IPathManager.IsMissionActiveByObjectId)));
        Assert.Equal(2u, activeCheck.Arguments[0]);
        RecordingDispatchProxy<IPathManager>.Invocation activateAttempt = Assert.Single(
            pathManagerProxy.GetInvocations(nameof(IPathManager.TryActivateSoldierMissionByEventId)));
        Assert.Equal(2u, activateAttempt.Arguments[0]);
        Assert.Empty(GetSessionMessages(sessionProxy));
        Assert.Empty(pathManagerProxy.GetInvocations(nameof(IPathManager.CompleteMissionByObjectId)));
    }

    [Fact]
    public void NorthernWildsSoldierPathScripts_OnActivate_WhenMissionActivates_StartsHoldout()
    {
        NorthernWildsSoldierHoldoutEntityScript script = CreateSoldierHoldoutScript(typeof(NorthernWildsYetiHoldoutEntityScript));
        IPlayer player = CreatePlayerWithPath(
            PlayerPath.Soldier,
            out RecordingDispatchProxy<IPathManager> pathManagerProxy,
            out RecordingDispatchProxy<IGameSession> sessionProxy);
        pathManagerProxy.SetMethodHandler(nameof(IPathManager.TryActivateSoldierMissionByEventId),
            args => (uint)args[0] == 2u);

        script.OnActivateSuccess(player);

        RecordingDispatchProxy<IPathManager>.Invocation activeCheck = Assert.Single(
            pathManagerProxy.GetInvocations(nameof(IPathManager.IsMissionActiveByObjectId)));
        Assert.Equal(2u, activeCheck.Arguments[0]);
        RecordingDispatchProxy<IPathManager>.Invocation activateAttempt = Assert.Single(
            pathManagerProxy.GetInvocations(nameof(IPathManager.TryActivateSoldierMissionByEventId)));
        Assert.Equal(2u, activateAttempt.Arguments[0]);

        ServerPathSoldierHoldoutStatus initialStatus =
            Assert.IsType<ServerPathSoldierHoldoutStatus>(Assert.Single(GetSessionMessages(sessionProxy)));
        Assert.Equal(2u, initialStatus.PathSoldierEventId);
        Assert.Equal(PlayerPathSoldierEventMode.InitialDelay, initialStatus.Mode);
        Assert.Empty(pathManagerProxy.GetInvocations(nameof(IPathManager.CompleteMissionByObjectId)));
    }

    [Fact]
    public void NorthernWildsSoldierPathScripts_OnActivate_WhenMissionIsAlreadyComplete_DoesNothing()
    {
        NorthernWildsSoldierHoldoutEntityScript script = CreateSoldierHoldoutScript(typeof(NorthernWildsYetiHoldoutEntityScript));
        IPlayer player = CreatePlayerWithPath(
            PlayerPath.Soldier,
            out RecordingDispatchProxy<IPathManager> pathManagerProxy,
            out RecordingDispatchProxy<IGameSession> sessionProxy);
        pathManagerProxy.SetMethodHandler(nameof(IPathManager.IsMissionCompleteByObjectId),
            args => (uint)args[0] == 2u);
        pathManagerProxy.SetMethodHandler(nameof(IPathManager.IsMissionActiveByObjectId),
            args => (uint)args[0] == 2u);

        script.OnActivateSuccess(player);
        script.Update(180d);

        RecordingDispatchProxy<IPathManager>.Invocation completeCheck = Assert.Single(
            pathManagerProxy.GetInvocations(nameof(IPathManager.IsMissionCompleteByObjectId)));
        Assert.Equal(2u, completeCheck.Arguments[0]);
        Assert.Empty(pathManagerProxy.GetInvocations(nameof(IPathManager.IsMissionActiveByObjectId)));
        Assert.Empty(pathManagerProxy.GetInvocations(nameof(IPathManager.CompleteMissionByObjectId)));
        Assert.Empty(GetSessionMessages(sessionProxy));
    }

    [Fact]
    public void NorthernWildsSoldierPathScripts_OnActivate_WhenPlayerIsNotSoldier_DoesNothing()
    {
        NorthernWildsSoldierHoldoutEntityScript script = CreateSoldierHoldoutScript(typeof(NorthernWildsYetiHoldoutEntityScript));
        IPlayer player = CreatePlayerWithPath(
            PlayerPath.Explorer,
            out RecordingDispatchProxy<IPathManager> pathManagerProxy,
            out RecordingDispatchProxy<IGameSession> sessionProxy);

        script.OnActivateSuccess(player);
        script.Update(180d);

        Assert.Empty(GetSessionMessages(sessionProxy));
        Assert.Empty(pathManagerProxy.GetInvocations(nameof(IPathManager.IsMissionCompleteByObjectId)));
        Assert.Empty(pathManagerProxy.GetInvocations(nameof(IPathManager.IsMissionActiveByObjectId)));
        Assert.Empty(pathManagerProxy.GetInvocations(nameof(IPathManager.CompleteMissionByObjectId)));
    }

    private static Q3486LoftiteCrystalEntityScript CreateLoftiteCrystalScript(
        out RecordingDispatchProxy<ICreatureEntity> ownerProxy,
        IReadOnlyList<IPlayer> inRangePlayers = null)
    {
        ICreatureEntity owner = RecordingDispatchProxy<ICreatureEntity>.Create(out ownerProxy);
        ownerProxy.SetProperty(nameof(ICreatureEntity.Guid), LoftiteCrystalGuid);
        ownerProxy.SetProperty(nameof(ICreatureEntity.Health), LoftiteCrystalHealth);
        if (inRangePlayers != null)
            ownerProxy.SetMethodHandler(nameof(ICreatureEntity.GetInRange), _ => inRangePlayers);

        var script = new Q3486LoftiteCrystalEntityScript();
        script.OnLoad(owner);
        return script;
    }

    private static Q4526SpatialAnomalyEntityScript CreateSpatialAnomalyScript(
        out RecordingDispatchProxy<ICreatureEntity> ownerProxy)
    {
        ICreatureEntity owner = RecordingDispatchProxy<ICreatureEntity>.Create(out ownerProxy);
        var script = new Q4526SpatialAnomalyEntityScript();
        script.OnLoad(owner);
        return script;
    }

    private static Q3667ControlPanelEntityScript CreateControlPanelScript(
        out RecordingDispatchProxy<ICreatureEntity> ownerProxy)
    {
        ICreatureEntity owner = RecordingDispatchProxy<ICreatureEntity>.Create(out ownerProxy);
        var script = new Q3667ControlPanelEntityScript();
        script.OnLoad(owner);
        return script;
    }

    private static NorthernWildsSoldierHoldoutEntityScript CreateSoldierHoldoutScript(Type scriptType)
    {
        ICreatureEntity owner = RecordingDispatchProxy<ICreatureEntity>.Create(out _);
        NorthernWildsSoldierHoldoutEntityScript script = (NorthernWildsSoldierHoldoutEntityScript)Activator.CreateInstance(scriptType)!;
        script.OnLoad(owner);
        return script;
    }

    private static IBaseMap CreateHoldoutMap(
        IReadOnlyList<IWorldEntity> searchEntities,
        out RecordingDispatchProxy<IBaseMap> mapProxy)
    {
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out mapProxy);
        mapProxy.SetProperty(nameof(IBaseMap.Entry), new WorldEntry { Id = NorthernWildsWorld });
        mapProxy.SetMethodHandler(nameof(IBaseMap.Search), args =>
        {
            var check = (ISearchCheck<IWorldEntity>)args[2];
            return searchEntities
                .Where(check.CheckEntity)
                .ToArray();
        });
        return map;
    }

    private static ISimpleEntity CreateHoldoutOwner(IBaseMap map)
    {
        ISimpleEntity owner = RecordingDispatchProxy<ISimpleEntity>.Create(out RecordingDispatchProxy<ISimpleEntity> ownerProxy);
        ownerProxy.SetProperty(nameof(ISimpleEntity.Map), map);
        ownerProxy.SetProperty(nameof(ISimpleEntity.Position), new Vector3(4157f, -744f, -5536f));
        return owner;
    }

    private static IWorldEntity CreateWorldEntity(
        uint creatureId,
        uint guid,
        Vector3 position,
        bool inWorld,
        out RecordingDispatchProxy<IWorldEntity> entityProxy)
    {
        IWorldEntity entity = RecordingDispatchProxy<IWorldEntity>.Create(out entityProxy);
        entityProxy.SetProperty(nameof(IWorldEntity.CreatureId), creatureId);
        entityProxy.SetProperty(nameof(IWorldEntity.Guid), guid);
        entityProxy.SetProperty(nameof(IWorldEntity.Position), position);
        entityProxy.SetProperty(nameof(IWorldEntity.InWorld), inWorld);
        return entity;
    }

    private static IReadOnlyList<object> GetSessionMessages(RecordingDispatchProxy<IGameSession> sessionProxy)
    {
        return sessionProxy
            .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(i => i.Arguments[0])
            .ToList();
    }

    private static ICreatureEntity CreateLoftiteCrystal(
        Vector3 position,
        float hitRadius,
        out RecordingDispatchProxy<ICreatureEntity> crystalProxy)
    {
        ICreatureEntity crystal = RecordingDispatchProxy<ICreatureEntity>.Create(out crystalProxy);
        crystalProxy.SetProperty(nameof(ICreatureEntity.CreatureId), LoftiteCrystalCreature);
        crystalProxy.SetProperty(nameof(ICreatureEntity.Guid), LoftiteCrystalGuid);
        crystalProxy.SetProperty(nameof(ICreatureEntity.Health), LoftiteCrystalHealth);
        crystalProxy.SetProperty(nameof(ICreatureEntity.Position), position);
        crystalProxy.SetProperty(nameof(ICreatureEntity.HitRadius), hitRadius);
        return crystal;
    }

    private static void AssertCreatureInitialise<T>(
        RecordingDispatchProxy<T> entityProxy,
        uint expectedCreatureId)
        where T : class
    {
        RecordingDispatchProxy<T>.Invocation initialise = Assert.Single(
            entityProxy.GetInvocations(nameof(IWorldEntity.Initialise)));
        Assert.Equal(expectedCreatureId, initialise.Arguments[0]);
    }

    private static NorthernWildsMapScript CreateNorthernWildsMapScript(
        IReadOnlyList<IPlayer> players,
        IReadOnlyList<IWorldEntity> crystals,
        params WorldLocation2Entry[] worldLocations)
    {
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out RecordingDispatchProxy<IBaseMap> mapProxy);
        mapProxy.SetProperty(nameof(IBaseMap.Entry), new WorldEntry { Id = NorthernWildsWorld });
        mapProxy.SetProperty(nameof(IBaseMap.PublicEventManager), RecordingDispatchProxy<IPublicEventManager>.Create(out _));
        mapProxy.SetMethodHandler(nameof(IBaseMap.Search), args =>
        {
            if (args[1] == null)
                return players
                    .Where(player => ((ISearchCheck<IPlayer>)args[2]).CheckEntity(player))
                    .ToArray();

            return crystals
                .Where(crystal => ((ISearchCheck<IWorldEntity>)args[2]).CheckEntity(crystal))
                .ToArray();
        });

        ICinematicFactory cinematicFactory = RecordingDispatchProxy<ICinematicFactory>.Create(out _);
        IEntityFactory entityFactory = RecordingDispatchProxy<IEntityFactory>.Create(out RecordingDispatchProxy<IEntityFactory> entityFactoryProxy);
        entityFactoryProxy.SetMethodHandler(nameof(IEntityFactory.CreateEntity), (method, args) =>
        {
            Type entityType = method.GetGenericArguments()[0];
            if (entityType == typeof(ICollectableUnitEntity))
                return RecordingDispatchProxy<ICollectableUnitEntity>.Create(out _);

            return RecordingDispatchProxy<INonPlayerEntity>.Create(out _);
        });
        IGameTableManager gameTableManager = RecordingDispatchProxy<IGameTableManager>.Create(out RecordingDispatchProxy<IGameTableManager> gameTableManagerProxy);
        gameTableManagerProxy.SetProperty(nameof(IGameTableManager.WorldLocation2), CreateGameTable(worldLocations));
        IStoryBuilder storyBuilder = RecordingDispatchProxy<IStoryBuilder>.Create(out _);

        var script = new NorthernWildsMapScript(
            NullLogger<NorthernWildsMapScript>.Instance,
            entityFactory,
            gameTableManager,
            cinematicFactory,
            storyBuilder);
        script.OnLoad(map);
        return script;
    }

    private static IPlayer CreatePlayerWithQuestState(
        QuestState? questState,
        out RecordingDispatchProxy<IQuestManager> questManagerProxy,
        Vector3? position = null,
        uint? zoneId = null,
        float hitRadius = 1f,
        ushort questId = EmpoweredTowerQuest)
    {
        IQuestManager questManager = RecordingDispatchProxy<IQuestManager>.Create(out questManagerProxy);
        questManagerProxy.SetMethodHandler(nameof(IQuestManager.GetQuestState), args =>
            (ushort)args[0] == questId ? questState : null);

        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.QuestManager), questManager);
        playerProxy.SetProperty(nameof(IPlayer.Position), position ?? Vector3.Zero);
        playerProxy.SetProperty(nameof(IPlayer.HitRadius), hitRadius);
        if (zoneId.HasValue)
            playerProxy.SetProperty(nameof(IPlayer.Zone), new WorldZoneEntry { Id = zoneId.Value });

        return player;
    }

    private static IPlayer CreatePlayerWithPathManager(out RecordingDispatchProxy<IPathManager> pathManagerProxy)
    {
        IQuestManager questManager = RecordingDispatchProxy<IQuestManager>.Create(out RecordingDispatchProxy<IQuestManager> questManagerProxy);
        questManagerProxy.SetMethodHandler(nameof(IQuestManager.GetQuestState), _ => QuestState.Accepted);
        IPathManager pathManager = RecordingDispatchProxy<IPathManager>.Create(out pathManagerProxy);

        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), 42ul);
        playerProxy.SetProperty(nameof(IPlayer.QuestManager), questManager);
        playerProxy.SetProperty(nameof(IPlayer.PathManager), pathManager);
        return player;
    }

    private static IPlayer CreatePlayerWithPath(
        PlayerPath path,
        out RecordingDispatchProxy<IPathManager> pathManagerProxy,
        out RecordingDispatchProxy<IGameSession> sessionProxy)
    {
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out sessionProxy);
        IPathManager pathManager = RecordingDispatchProxy<IPathManager>.Create(out pathManagerProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Path), path);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);
        playerProxy.SetProperty(nameof(IPlayer.PathManager), pathManager);
        return player;
    }

    private static WorldLocation2Entry CreateWorldLocation(uint id, float radius, float x, float y, float z)
    {
        return new WorldLocation2Entry
        {
            Id = id,
            Radius = radius,
            Position0 = x,
            Position1 = y,
            Position2 = z
        };
    }

    private static GameTable<T> CreateGameTable<T>(params T[] entries) where T : class, new()
    {
        var table = (GameTable<T>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<T>));
        SetAutoProperty(table, nameof(GameTable<T>.Entries), entries);
        SetPrivateField(table, "header", new GameTableHeader
        {
            MaxId = entries.Length == 0 ? 0u : entries.Max(GetEntryId) + 1u
        });
        SetPrivateField(table, "lookup", BuildLookup(entries));
        return table;
    }

    private static int[] BuildLookup<T>(IReadOnlyList<T> entries)
    {
        if (entries.Count == 0)
            return [];

        int[] lookup = Enumerable.Repeat(-1, (int)(entries.Max(GetEntryId) + 1u)).ToArray();
        for (int i = 0; i < entries.Count; i++)
            lookup[GetEntryId(entries[i])] = i;

        return lookup;
    }

    private static uint GetEntryId<T>(T entry)
    {
        FieldInfo idField = typeof(T).GetField("Id")!;
        return (uint)idField.GetValue(entry)!;
    }

    private static void SetAutoProperty(object instance, string propertyName, object value)
    {
        FieldInfo backingField = instance.GetType()
            .GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!;
        backingField.SetValue(instance, value);
    }

    private static void SetPrivateField(object instance, string fieldName, object value)
    {
        FieldInfo field = instance.GetType()
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!;
        field.SetValue(instance, value);
    }

}
