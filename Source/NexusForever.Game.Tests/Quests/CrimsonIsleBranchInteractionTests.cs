using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Cinematic;
using NexusForever.Game.Abstract.Cinematic.Cinematics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Map.Search;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Static.Spell;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.IO.Map;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Static;
using NexusForever.Script.Main.Quests.CrimsonIsle;

namespace NexusForever.Game.Tests.Quests;

public class CrimsonIsleBranchInteractionTests
{
    private const ushort LastResistanceQuest = 5594;
    private const ushort DregsAndThievesQuest = 5597;
    private const ushort OlyssiaWorld = 22;
    private const float TeleportX = -5750.767f;
    private const float TeleportY = -971.7648f;
    private const float TeleportZ = -623.33295f;
    private const float RotationX = -1.1721236f;
    private const ushort CrimsonIsleWorld = 870;
    private const uint OrdnanceRecoveryDominionDemolitionsExpert = 24703u;
    private const uint DregsAndThievesChuaExplosives = 24286u;
    private const uint DregsAndThievesChuaExplosivesVirtualItem = 364u;
    private const uint PoweringDownSeizingPowerPowerRegulator = 24999u;
    private const uint EnforcedRadioSilenceTowerControls = 26559u;
    private const uint TacticalDemolitionsExileAntiAirCannon = 24298u;
    private const uint HeavyArmorHellfireTank = 24255u;
    private const uint HeavyArmorVindicatorTank = 24452u;
    private const uint HeavyArmorMegatechGunner = 24030u;
    private const uint HeavyArmorMegatechSwordsmaster = 26591u;
    private const uint HeavyArmorMegatechWarbot = 31792u;
    private const uint HeavyArmorMegatechMerc = 31864u;
    private const uint HeavyArmorMegatechContractor = 31895u;
    private const uint HeavyArmorMegatechShockTrooper = 38228u;
    private const uint KezrekWarbringerCreature = 24158u;
    private const uint KezrekWarbringerReceiverWorldLocation = 17902u;
    private const uint DregMutationsMondoZaxCreature = 24187u;
    private const uint DregMutationsReceiverWorldLocation = 17803u;

    private static readonly Vector3[] OrdnanceRecoveryDeadExpertPositions =
    [
        new(-7432.215f, -980.2523f, -721.0286f),
        new(-7328.925f, -989.9131f, -680.5856f),
        new(-7387.78f, -985.9744f, -707.416f),
        new(-7386.29f, -999.1185f, -687.906f),
        new(-7366.03f, -984.3904f, -721.219f),
        new(-7421.486f, -985.34f, -687.6445f)
    ];

    private static readonly Vector3[] OrdnanceRecoveryDeadExpertRotations =
    [
        new(0.4010453f, 0f, 0f),
        new(-0.9798607f, 0f, 0f),
        new(-0.6889328f, 0f, 0f),
        new(-2.433168f, 0f, 0f),
        new(-0.498628f, 0f, 0f),
        new(1.228232f, 0f, 0f)
    ];

    private static readonly Vector3[] DregsAndThievesChuaExplosivesPositions =
    [
        new(-7140f, -995f, -979f),
        new(-7074f, -994f, -953f),
        new(-7084f, -995f, -863f),
        new(-7182f, -993f, -855f),
        new(-7077f, -995f, -885f),
        new(-7147f, -993f, -948f),
        new(-7298f, -990f, -830f),
        new(-7358f, -992f, -883f),
        new(-7290f, -991f, -860f),
        new(-7295f, -991f, -873f),
        new(-7225f, -991f, -807f),
        new(-7247f, -991f, -791f),
        new(-7276f, -991f, -787f),
        new(-7267f, -990f, -785f),
        new(-7110f, -995f, -860f),
        new(-7073f, -993f, -1033f),
        new(-7123f, -995f, -1030f),
        new(-7100f, -994f, -1043f),
        new(-7104f, -995f, -1012f),
        new(-7115f, -995f, -1022f),
        new(-7102f, -995f, -991f),
        new(-7129f, -994f, -1040f),
        new(-7143f, -995f, -1066f),
        new(-7031f, -993f, -1029f),
        new(-7037f, -994f, -980f),
        new(-6994f, -992f, -1030f),
        new(-6991f, -992f, -1061f),
        new(-7086f, -995f, -1018f),
        new(-7136f, -995f, -1013f),
        new(-7003f, -993f, -1003f)
    ];

    private static readonly Vector3[] TacticalDemolitionsExileAntiAirCannonPositions =
    [
        new(-7350.335f, -937.0211f, -1084.566f),
        new(-7379.671f, -931.5699f, -1148.277f),
        new(-7442.555f, -954.2583f, -1078.531f)
    ];

    private static readonly Vector3[] TacticalDemolitionsExileAntiAirCannonRotations =
    [
        new(-2.232193f, 0f, 0f),
        new(-2.514863f, 0f, 0f),
        new(-3.141593f, 0f, 0f)
    ];

    private static readonly ulong[] TacticalDemolitionsExileAntiAirCannonActivePropIds =
    [
        1085356u,
        1085429u,
        1081959u
    ];

    private static readonly Vector3[] PowerRegulatorPositions =
    [
        new(-7836.315f, -942.0623f, -372.4393f),
        new(-7742.86f, -945.3322f, -322.7298f),
        new(-7710.831f, -940.8549f, -399.7918f)
    ];

    private static readonly Vector3[] PowerRegulatorRotations =
    [
        new(-1.390593f, 0f, 0f),
        new(-0.2827478f, 0f, 0f),
        new(-1.536915f, 0f, 0f)
    ];

    private static readonly ulong[] PowerRegulatorActivePropIds =
    [
        5708188u,
        5708196u,
        5708174u
    ];

    private static readonly Vector3[] EnforcedRadioSilenceTowerControlsPositions =
    [
        new(-7594.24f, -941.743f, -1355.979f),
        new(-7701.885f, -933.3383f, -1412.195f)
    ];

    private static readonly Vector3[] EnforcedRadioSilenceTowerControlsRotations =
    [
        new(1.674811f, 0f, 0f),
        new(-2.327762f, 0f, 0f)
    ];

    private static readonly ulong[] EnforcedRadioSilenceTowerControlsActivePropIds =
    [
        1137353u,
        1137404u
    ];

    private static readonly uint[] HeavyArmorKillTargetCreatureIds =
    [
        HeavyArmorHellfireTank,
        HeavyArmorVindicatorTank,
        HeavyArmorMegatechGunner,
        HeavyArmorMegatechSwordsmaster,
        HeavyArmorMegatechShockTrooper,
        HeavyArmorMegatechWarbot,
        HeavyArmorMegatechMerc,
        HeavyArmorMegatechContractor
    ];

    private static readonly Vector3[] HeavyArmorKillTargetPositions =
    [
        new(-7717f, -950f, -1300f),
        new(-7696f, -950f, -1330f),
        new(-7773f, -950f, -1365f),
        new(-7756f, -950f, -1323f),
        new(-7743f, -950f, -1333f),
        new(-7953f, -941f, -1337f),
        new(-7944f, -941f, -1344f),
        new(-7959f, -941f, -1333f)
    ];

    [Fact]
    public void Q5573PoweringDown_OnPowerRegulatorsComplete_QueuesCinematicAndCreditsObjective()
    {
        ICinematicBase cinematic = RecordingDispatchProxy<IQ5573PoweringDownCinematic>.Create(out _);
        var script = new Q5573PoweringDownQuestScript(
            CreateCinematicFactory(cinematic),
            CreateGlobalQuestManager(),
            NullLogger<Q5573PoweringDownQuestScript>.Instance);
        IQuest quest = CreateQuest(
            QuestState.Accepted,
            out RecordingDispatchProxy<IQuest> questProxy,
            out RecordingDispatchProxy<ICinematicManager> cinematicManagerProxy);

        script.OnLoad(quest);
        script.OnObjectiveUpdate(CreateObjective(8229u, complete: true));

        RecordingDispatchProxy<ICinematicManager>.Invocation queued = Assert.Single(
            cinematicManagerProxy.GetInvocations(nameof(ICinematicManager.QueueCinematic)));
        Assert.Same(cinematic, queued.Arguments[0]);

        RecordingDispatchProxy<IQuest>.Invocation objectiveUpdate = Assert.Single(
            questProxy.GetInvocations(nameof(IQuest.ObjectiveUpdate)));
        Assert.Equal(12870u, objectiveUpdate.Arguments[0]);
        Assert.Equal(1u, objectiveUpdate.Arguments[1]);
    }

    [Fact]
    public void Q5573PoweringDown_OnPowerRegulatorsComplete_WhenAlreadyAchieved_DoesNotQueueOrCredit()
    {
        ICinematicBase cinematic = RecordingDispatchProxy<IQ5573PoweringDownCinematic>.Create(out _);
        var script = new Q5573PoweringDownQuestScript(
            CreateCinematicFactory(cinematic),
            CreateGlobalQuestManager(),
            NullLogger<Q5573PoweringDownQuestScript>.Instance);
        IQuest quest = CreateQuest(
            QuestState.Achieved,
            out RecordingDispatchProxy<IQuest> questProxy,
            out RecordingDispatchProxy<ICinematicManager> cinematicManagerProxy);

        script.OnLoad(quest);
        script.OnObjectiveUpdate(CreateObjective(8229u, complete: true));

        Assert.Empty(cinematicManagerProxy.GetInvocations(nameof(ICinematicManager.QueueCinematic)));
        Assert.Empty(questProxy.GetInvocations(nameof(IQuest.ObjectiveUpdate)));
    }

    [Fact]
    public void Q5604TacticalDemolitions_OnExileCannonsComplete_QueuesCinematicAndCreditsObjective()
    {
        ICinematicBase cinematic = RecordingDispatchProxy<IQ5604TacticalDemolitionsCinematic>.Create(out _);
        var script = new Q5604TacticalDemolitionsQuestScript(
            CreateCinematicFactory(cinematic),
            CreateGlobalQuestManager(),
            NullLogger<Q5604TacticalDemolitionsQuestScript>.Instance);
        IQuest quest = CreateQuest(
            QuestState.Accepted,
            out RecordingDispatchProxy<IQuest> questProxy,
            out RecordingDispatchProxy<ICinematicManager> cinematicManagerProxy);

        script.OnLoad(quest);
        script.OnObjectiveUpdate(CreateObjective(8268u, complete: true));

        RecordingDispatchProxy<ICinematicManager>.Invocation queued = Assert.Single(
            cinematicManagerProxy.GetInvocations(nameof(ICinematicManager.QueueCinematic)));
        Assert.Same(cinematic, queued.Arguments[0]);

        RecordingDispatchProxy<IQuest>.Invocation objectiveUpdate = Assert.Single(
            questProxy.GetInvocations(nameof(IQuest.ObjectiveUpdate)));
        Assert.Equal(15918u, objectiveUpdate.Arguments[0]);
        Assert.Equal(1u, objectiveUpdate.Arguments[1]);
    }

    [Fact]
    public void Q5604TacticalDemolitions_OnExileCannonsComplete_WhenAlreadyAchieved_DoesNotQueueOrCredit()
    {
        ICinematicBase cinematic = RecordingDispatchProxy<IQ5604TacticalDemolitionsCinematic>.Create(out _);
        var script = new Q5604TacticalDemolitionsQuestScript(
            CreateCinematicFactory(cinematic),
            CreateGlobalQuestManager(),
            NullLogger<Q5604TacticalDemolitionsQuestScript>.Instance);
        IQuest quest = CreateQuest(
            QuestState.Achieved,
            out RecordingDispatchProxy<IQuest> questProxy,
            out RecordingDispatchProxy<ICinematicManager> cinematicManagerProxy);

        script.OnLoad(quest);
        script.OnObjectiveUpdate(CreateObjective(8268u, complete: true));

        Assert.Empty(cinematicManagerProxy.GetInvocations(nameof(ICinematicManager.QueueCinematic)));
        Assert.Empty(questProxy.GetInvocations(nameof(IQuest.ObjectiveUpdate)));
    }

    [Fact]
    public void Q5575SeizingPower_OnPowerRegulatorsComplete_CreditsObjective()
    {
        var script = new Q5575QuestScript(
            NullLogger<Q5575QuestScript>.Instance,
            CreateGlobalQuestManager());
        IQuest quest = CreateQuest(
            QuestState.Accepted,
            out RecordingDispatchProxy<IQuest> questProxy,
            out RecordingDispatchProxy<ICinematicManager> cinematicManagerProxy);

        script.OnLoad(quest);
        script.OnObjectiveUpdate(CreateObjective(8371u, complete: true));

        Assert.Empty(cinematicManagerProxy.GetInvocations(nameof(ICinematicManager.QueueCinematic)));

        RecordingDispatchProxy<IQuest>.Invocation objectiveUpdate = Assert.Single(
            questProxy.GetInvocations(nameof(IQuest.ObjectiveUpdate)));
        Assert.Equal(12871u, objectiveUpdate.Arguments[0]);
        Assert.Equal(1u, objectiveUpdate.Arguments[1]);
    }

    [Fact]
    public void Q5575SeizingPower_OnPowerRegulatorsComplete_WhenAlreadyAchieved_DoesNotCredit()
    {
        var script = new Q5575QuestScript(
            NullLogger<Q5575QuestScript>.Instance,
            CreateGlobalQuestManager());
        IQuest quest = CreateQuest(
            QuestState.Achieved,
            out RecordingDispatchProxy<IQuest> questProxy,
            out _);

        script.OnLoad(quest);
        script.OnObjectiveUpdate(CreateObjective(8371u, complete: true));

        Assert.Empty(questProxy.GetInvocations(nameof(IQuest.ObjectiveUpdate)));
    }

    [Fact]
    public void Q5604TacticalDemolitions_OnExileCannonsComplete_WhenRepeated_QueuesAndCreditsOnce()
    {
        ICinematicBase cinematic = RecordingDispatchProxy<IQ5604TacticalDemolitionsCinematic>.Create(out _);
        var script = new Q5604TacticalDemolitionsQuestScript(
            CreateCinematicFactory(cinematic),
            CreateGlobalQuestManager(),
            NullLogger<Q5604TacticalDemolitionsQuestScript>.Instance);
        IQuest quest = CreateQuest(
            QuestState.Accepted,
            out RecordingDispatchProxy<IQuest> questProxy,
            out RecordingDispatchProxy<ICinematicManager> cinematicManagerProxy);
        IQuestObjective objective = CreateObjective(8268u, complete: true);

        script.OnLoad(quest);
        script.OnObjectiveUpdate(objective);
        script.OnObjectiveUpdate(objective);

        RecordingDispatchProxy<ICinematicManager>.Invocation queued = Assert.Single(
            cinematicManagerProxy.GetInvocations(nameof(ICinematicManager.QueueCinematic)));
        Assert.Same(cinematic, queued.Arguments[0]);

        RecordingDispatchProxy<IQuest>.Invocation objectiveUpdate = Assert.Single(
            questProxy.GetInvocations(nameof(IQuest.ObjectiveUpdate)));
        Assert.Equal(15918u, objectiveUpdate.Arguments[0]);
        Assert.Equal(1u, objectiveUpdate.Arguments[1]);
    }

    [Fact]
    public void Q5604TacticalDemolitions_OnExileCannonsIncomplete_DoesNotQueueOrCredit()
    {
        ICinematicBase cinematic = RecordingDispatchProxy<IQ5604TacticalDemolitionsCinematic>.Create(out _);
        var script = new Q5604TacticalDemolitionsQuestScript(
            CreateCinematicFactory(cinematic),
            CreateGlobalQuestManager(),
            NullLogger<Q5604TacticalDemolitionsQuestScript>.Instance);
        IQuest quest = CreateQuest(
            QuestState.Accepted,
            out RecordingDispatchProxy<IQuest> questProxy,
            out RecordingDispatchProxy<ICinematicManager> cinematicManagerProxy);

        script.OnLoad(quest);
        script.OnObjectiveUpdate(CreateObjective(8268u, complete: false));

        Assert.Empty(cinematicManagerProxy.GetInvocations(nameof(ICinematicManager.QueueCinematic)));
        Assert.Empty(questProxy.GetInvocations(nameof(IQuest.ObjectiveUpdate)));
    }

    [Fact]
    public void Q5584TrappedAssistant_OnActivateSuccess_SetsStateAndKillsAssistant()
    {
        ICreatureEntity owner = CreateCreature(maxHealth: 125u, out RecordingDispatchProxy<ICreatureEntity> ownerProxy);
        IPlayer activator = RecordingDispatchProxy<IPlayer>.Create(out _);
        var script = new Q5584TrappedAssistantEntityScript();

        script.OnLoad(owner);
        script.OnActivateSuccess(activator);

        RecordingDispatchProxy<ICreatureEntity>.Invocation standState = Assert.Single(ownerProxy.GetInvocations("set_StandState"));
        Assert.Equal(StandState.State2, standState.Arguments[0]);

        RecordingDispatchProxy<ICreatureEntity>.Invocation damage = Assert.Single(ownerProxy.GetInvocations(nameof(ICreatureEntity.ModifyHealth)));
        Assert.Equal(125u, damage.Arguments[0]);
        Assert.Equal(DamageType.Physical, damage.Arguments[1]);
        Assert.Null(damage.Arguments[2]);
    }

    [Fact]
    public void Q5594ShipControls_OnActivateSuccess_WhenQuestAccepted_AchievesAndTeleports()
    {
        IPlayer player = CreatePlayer(
            QuestState.Accepted,
            canTeleport: true,
            out RecordingDispatchProxy<IQuestManager> questManagerProxy,
            out RecordingDispatchProxy<IPlayer> playerProxy);
        var script = new Q5594ShipControlsEntityScript();

        script.OnLoad(CreateCreature(maxHealth: 100u, out _));
        script.OnActivateSuccess(player);

        RecordingDispatchProxy<IQuestManager>.Invocation achieved = Assert.Single(questManagerProxy.GetInvocations(nameof(IQuestManager.QuestAchieve)));
        Assert.Equal(LastResistanceQuest, achieved.Arguments[0]);
        AssertTeleportedToBloodfireVillage(playerProxy);
    }

    [Fact]
    public void Q5594ShipControls_OnActivateSuccess_WhenQuestMissing_StillTeleportsWithoutQuestCredit()
    {
        IPlayer player = CreatePlayer(
            questState: null,
            canTeleport: true,
            out RecordingDispatchProxy<IQuestManager> questManagerProxy,
            out RecordingDispatchProxy<IPlayer> playerProxy);
        var script = new Q5594ShipControlsEntityScript();

        script.OnLoad(CreateCreature(maxHealth: 100u, out _));
        script.OnActivateSuccess(player);

        Assert.Empty(questManagerProxy.GetInvocations(nameof(IQuestManager.QuestAchieve)));
        AssertTeleportedToBloodfireVillage(playerProxy);
    }

    [Fact]
    public void Q5594ShipControls_OnActivateSuccess_WhenTeleportBlocked_AchievesWithoutRelocation()
    {
        IPlayer player = CreatePlayer(
            QuestState.Accepted,
            canTeleport: false,
            out RecordingDispatchProxy<IQuestManager> questManagerProxy,
            out RecordingDispatchProxy<IPlayer> playerProxy);
        var script = new Q5594ShipControlsEntityScript();

        script.OnLoad(CreateCreature(maxHealth: 100u, out _));
        script.OnActivateSuccess(player);

        RecordingDispatchProxy<IQuestManager>.Invocation achieved = Assert.Single(questManagerProxy.GetInvocations(nameof(IQuestManager.QuestAchieve)));
        Assert.Equal(LastResistanceQuest, achieved.Arguments[0]);
        Assert.Empty(playerProxy.GetInvocations("set_Rotation"));
        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.TeleportTo)));
    }

    [Fact]
    public void Q5597ChuaExplosives_OnActivateSuccess_WhenQuestAccepted_CreditsVirtualCollectAndRemoves()
    {
        IPlayer player = CreatePlayer(
            DregsAndThievesQuest,
            QuestState.Accepted,
            canTeleport: false,
            out RecordingDispatchProxy<IQuestManager> questManagerProxy,
            out _);
        ICollectableUnitEntity owner = RecordingDispatchProxy<ICollectableUnitEntity>.Create(out RecordingDispatchProxy<ICollectableUnitEntity> ownerProxy);
        var script = new Q5597ChuaExplosivesEntityScript();

        script.OnLoad(owner);
        script.OnActivateSuccess(player);

        RecordingDispatchProxy<IQuestManager>.Invocation objectiveUpdate = Assert.Single(
            questManagerProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)));
        Assert.Equal(QuestObjectiveType.VirtualCollect, objectiveUpdate.Arguments[0]);
        Assert.Equal(DregsAndThievesChuaExplosivesVirtualItem, objectiveUpdate.Arguments[1]);
        Assert.Equal(1u, objectiveUpdate.Arguments[2]);
        Assert.Single(ownerProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
    }

    [Fact]
    public void Q5597ChuaExplosivesCreature_OnActivateSuccess_WhenQuestAccepted_CreditsVirtualCollectAndRemoves()
    {
        IPlayer player = CreatePlayer(
            DregsAndThievesQuest,
            QuestState.Accepted,
            canTeleport: false,
            out RecordingDispatchProxy<IQuestManager> questManagerProxy,
            out _);
        ICreatureEntity owner = CreateCreature(maxHealth: 1u, out RecordingDispatchProxy<ICreatureEntity> ownerProxy);
        var script = new Q5597ChuaExplosivesCreatureEntityScript();

        script.OnLoad(owner);
        script.OnActivateSuccess(player);

        RecordingDispatchProxy<IQuestManager>.Invocation objectiveUpdate = Assert.Single(
            questManagerProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)));
        Assert.Equal(QuestObjectiveType.VirtualCollect, objectiveUpdate.Arguments[0]);
        Assert.Equal(DregsAndThievesChuaExplosivesVirtualItem, objectiveUpdate.Arguments[1]);
        Assert.Equal(1u, objectiveUpdate.Arguments[2]);
        Assert.Single(ownerProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(QuestState.Achieved)]
    [InlineData(QuestState.Completed)]
    public void Q5597ChuaExplosives_OnActivateSuccess_WhenQuestNotAccepted_DoesNotCreditOrRemove(QuestState? questState)
    {
        IPlayer player = CreatePlayer(
            DregsAndThievesQuest,
            questState,
            canTeleport: false,
            out RecordingDispatchProxy<IQuestManager> questManagerProxy,
            out _);
        ICollectableUnitEntity owner = RecordingDispatchProxy<ICollectableUnitEntity>.Create(out RecordingDispatchProxy<ICollectableUnitEntity> ownerProxy);
        var script = new Q5597ChuaExplosivesEntityScript();

        script.OnLoad(owner);
        script.OnActivateSuccess(player);

        Assert.Empty(questManagerProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)));
        Assert.Empty(ownerProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
    }

    [Fact]
    public void Q5597ChuaExplosives_OnActivateSuccess_WhenRepeated_CreditsOnce()
    {
        IPlayer player = CreatePlayer(
            DregsAndThievesQuest,
            QuestState.Accepted,
            canTeleport: false,
            out RecordingDispatchProxy<IQuestManager> questManagerProxy,
            out _);
        ICollectableUnitEntity owner = RecordingDispatchProxy<ICollectableUnitEntity>.Create(out RecordingDispatchProxy<ICollectableUnitEntity> ownerProxy);
        var script = new Q5597ChuaExplosivesEntityScript();

        script.OnLoad(owner);
        script.OnActivateSuccess(player);
        script.OnActivateSuccess(player);

        Assert.Single(questManagerProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)));
        Assert.Single(ownerProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
    }

    [Fact]
    public void Q5597ChuaExplosives_OnAllPromotedPlacementsActivated_CreditsVirtualCollectUpdatesAndRemovesEach()
    {
        IPlayer player = CreatePlayer(
            DregsAndThievesQuest,
            QuestState.Accepted,
            canTeleport: false,
            out RecordingDispatchProxy<IQuestManager> questManagerProxy,
            out _);

        for (int i = 0; i < DregsAndThievesChuaExplosivesPositions.Length; i++)
        {
            ICollectableUnitEntity owner = RecordingDispatchProxy<ICollectableUnitEntity>.Create(out RecordingDispatchProxy<ICollectableUnitEntity> ownerProxy);
            var script = new Q5597ChuaExplosivesEntityScript();

            script.OnLoad(owner);
            script.OnActivateSuccess(player);

            Assert.Single(ownerProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
        }

        IReadOnlyList<RecordingDispatchProxy<IQuestManager>.Invocation> objectiveUpdates =
            questManagerProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate));
        Assert.Equal(DregsAndThievesChuaExplosivesPositions.Length, objectiveUpdates.Count);
        foreach (RecordingDispatchProxy<IQuestManager>.Invocation objectiveUpdate in objectiveUpdates)
        {
            Assert.Equal(QuestObjectiveType.VirtualCollect, objectiveUpdate.Arguments[0]);
            Assert.Equal(DregsAndThievesChuaExplosivesVirtualItem, objectiveUpdate.Arguments[1]);
            Assert.Equal(1u, objectiveUpdate.Arguments[2]);
        }
    }

    private static ICreatureEntity CreateCreature(
        uint maxHealth,
        out RecordingDispatchProxy<ICreatureEntity> ownerProxy)
    {
        ICreatureEntity owner = RecordingDispatchProxy<ICreatureEntity>.Create(out ownerProxy);
        ownerProxy.SetProperty(nameof(ICreatureEntity.MaxHealth), maxHealth);
        return owner;
    }

    private static IPlayer CreatePlayer(
        QuestState? questState,
        bool canTeleport,
        out RecordingDispatchProxy<IQuestManager> questManagerProxy,
        out RecordingDispatchProxy<IPlayer> playerProxy)
    {
        return CreatePlayer(LastResistanceQuest, questState, canTeleport, out questManagerProxy, out playerProxy);
    }

    private static IPlayer CreatePlayer(
        ushort questId,
        QuestState? questState,
        bool canTeleport,
        out RecordingDispatchProxy<IQuestManager> questManagerProxy,
        out RecordingDispatchProxy<IPlayer> playerProxy)
    {
        IQuestManager questManager = RecordingDispatchProxy<IQuestManager>.Create(out questManagerProxy);
        questManagerProxy.SetMethodHandler(nameof(IQuestManager.GetQuestState), args =>
            (ushort)args[0] == questId ? questState : null);

        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.QuestManager), questManager);
        playerProxy.SetMethodReturn(nameof(IPlayer.CanTeleport), canTeleport);
        return player;
    }

    private static void AssertTeleportedToBloodfireVillage(RecordingDispatchProxy<IPlayer> playerProxy)
    {
        RecordingDispatchProxy<IPlayer>.Invocation rotation = Assert.Single(playerProxy.GetInvocations("set_Rotation"));
        Vector3 vector = Assert.IsType<Vector3>(rotation.Arguments[0]);
        Assert.Equal(RotationX, vector.X, precision: 6);
        Assert.Equal(0f, vector.Y);
        Assert.Equal(0f, vector.Z);

        RecordingDispatchProxy<IPlayer>.Invocation teleport = Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.TeleportTo)));
        Assert.Equal(OlyssiaWorld, teleport.Arguments[0]);
        Assert.Equal(TeleportX, (float)teleport.Arguments[1], precision: 3);
        Assert.Equal(TeleportY, (float)teleport.Arguments[2], precision: 3);
        Assert.Equal(TeleportZ, (float)teleport.Arguments[3], precision: 3);
        Assert.Null(teleport.Arguments[4]);
        Assert.Equal(TeleportReason.Relocate, teleport.Arguments[5]);
    }

    private static void AssertCreatureInitialised<T>(RecordingDispatchProxy<T> entityProxy, uint creatureId)
        where T : class, IWorldEntity
    {
        RecordingDispatchProxy<T>.Invocation initialise = Assert.Single(
            entityProxy.GetInvocations(nameof(IWorldEntity.Initialise)));
        Assert.Equal(creatureId, initialise.Arguments[0]);
    }

    private static void AssertEnqueuedAt(
        RecordingDispatchProxy<IBaseMap>.Invocation enqueue,
        IGridEntity expectedEntity,
        WorldEntry expectedWorld,
        Vector3 expectedPosition)
    {
        Assert.Same(expectedEntity, enqueue.Arguments[0]);
        var position = (IMapPosition)enqueue.Arguments[1];
        Assert.Equal(expectedPosition, position.Position);
        Assert.Same(expectedWorld, position.Info.Entry);
    }

    private static INonPlayerEntity[] CreateNonPlayerEntities(
        int count,
        out RecordingDispatchProxy<INonPlayerEntity>[] proxies)
    {
        return CreateEntities(count, out proxies);
    }

    private static ISimpleEntity[] CreateSimpleEntities(
        int count,
        out RecordingDispatchProxy<ISimpleEntity>[] proxies)
    {
        return CreateEntities(count, out proxies);
    }

    private static ICollectableUnitEntity[] CreateCollectableUnitEntities(
        int count,
        out RecordingDispatchProxy<ICollectableUnitEntity>[] proxies)
    {
        return CreateEntities(count, out proxies);
    }

    private static T[] CreateEntities<T>(
        int count,
        out RecordingDispatchProxy<T>[] proxies) where T : class
    {
        proxies = new RecordingDispatchProxy<T>[count];
        var entities = new T[count];
        for (int i = 0; i < count; i++)
            entities[i] = RecordingDispatchProxy<T>.Create(out proxies[i]);

        return entities;
    }

    private static IQuest CreateQuest(
        QuestState state,
        out RecordingDispatchProxy<IQuest> questProxy,
        out RecordingDispatchProxy<ICinematicManager> cinematicManagerProxy)
    {
        ICinematicManager cinematicManager = RecordingDispatchProxy<ICinematicManager>.Create(out cinematicManagerProxy);

        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.CinematicManager), cinematicManager);

        IQuest quest = RecordingDispatchProxy<IQuest>.Create(out questProxy);
        questProxy.SetProperty(nameof(IQuest.State), state);
        questProxy.SetProperty(nameof(IQuest.Player), player);
        return quest;
    }

    private static IQuestObjective CreateObjective(uint objectiveId, bool complete)
    {
        IQuestObjectiveInfo objectiveInfo = RecordingDispatchProxy<IQuestObjectiveInfo>.Create(out RecordingDispatchProxy<IQuestObjectiveInfo> objectiveInfoProxy);
        objectiveInfoProxy.SetProperty(nameof(IQuestObjectiveInfo.Id), objectiveId);

        IQuestObjective objective = RecordingDispatchProxy<IQuestObjective>.Create(out RecordingDispatchProxy<IQuestObjective> objectiveProxy);
        objectiveProxy.SetProperty(nameof(IQuestObjective.ObjectiveInfo), objectiveInfo);
        objectiveProxy.SetMethodReturn(nameof(IQuestObjective.IsComplete), complete);
        return objective;
    }

    private static ICinematicFactory CreateCinematicFactory(ICinematicBase cinematic)
    {
        ICinematicFactory cinematicFactory = RecordingDispatchProxy<ICinematicFactory>.Create(out RecordingDispatchProxy<ICinematicFactory> cinematicFactoryProxy);
        cinematicFactoryProxy.SetMethodReturn(nameof(ICinematicFactory.CreateCinematic), cinematic);
        return cinematicFactory;
    }

    private static IGlobalQuestManager CreateGlobalQuestManager()
    {
        return RecordingDispatchProxy<IGlobalQuestManager>.Create(out _);
    }

    private static WorldLocation2Entry CreateWorldLocation(uint id, float x, float y, float z)
    {
        return new WorldLocation2Entry
        {
            Id = id,
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
