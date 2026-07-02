using System.Numerics;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Cinematic;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Abstract.Entity.Movement.Command.Position;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Map.Search;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Abstract.Story;
using NexusForever.Game.Static.Combat.CrowdControl;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Entity.Movement.Command.Mode;
using NexusForever.Game.Static.Entity.Movement.Command.State;
using NexusForever.Game.Static.Entity.Movement.Spline;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Static.Spell;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Entity;
using NexusForever.Script.Template;
using NexusForever.Script.Main.Quests.NorthernWilds;
using SharedTargetInfo = NexusForever.Network.World.Message.Model.Shared.TargetInfo;

namespace NexusForever.Game.Tests.Quests;

public class NorthernWildsAvalancheScriptTests
{
    private const uint NorthernWildsWorld = 426u;
    private const uint AvalancheCreature = 15615u;
    private const uint AvalancheCasterCreature = 67662u;
    private const uint GranokDropPodCreature = 13747u;
    private const uint GranokDropPodDecalCreature = 14126u;
    private const byte GranokDropPodDecalQuestChecklistIdx = 255;
    private const ushort SnowSmashAchievement = 3504;
    private const uint AvalancheKnockdownSpell4Effect = 42793u;
    private const uint AvalancheDamageSpell4Effect = 42794u;
    private const uint AvalancheAchievementSpell4Effect = 126234u;
    private const uint AvalancheKnockdownSpell = 26298u;
    private const uint FirstAvalancheCasting = 7001u;
    private const uint FirstAvalancheDamageEffect = 8001u;
    private const uint FirstAvalancheKnockdownEffect = 8002u;
    private const float AvalancheRouteVisualLift = 5f;
    private const float AvalancheMoveSpeed = 9f;
    private const ushort EmpoweredTowerQuest = 3486;
    private static readonly Vector3 AvalancheSource = new(3951f, -663f, -5673f);
    private static readonly Vector3 AvalancheSource2 = new(3941f, -693f, -5607f);
    private static readonly Vector3 AvalancheSource3 = new(3962f, -710f, -5540f);
    private static readonly Vector3 ImportedStaticAvalancheRow = new(4017f, -728f, -5518f);
    private static readonly Vector3 AvalancheRunout = new(4017f, -728f, -5518f);
    private static readonly Vector3 AvalancheFinalRunout = new(4012f, -714f, -5606f);
    private static readonly Vector2 AvalancheSourceForward2D = Vector2.Normalize(new Vector2(4f, 12f));
    private static readonly Vector3 AvalanchePastSpawn = AvalancheSource + AvalancheForwardOffset(7f);

    [Fact]
    public void NorthernWildsMapScript_OnLoad_SpawnsReviewedAvalancheFallbacks()
    {
        (NorthernWildsMapScript _, RecordingDispatchProxy<IBaseMap> mapProxy, IReadOnlyList<CreatedEntity> createdEntities) =
            CreateMapScript(spawnExistingAvalanche: false);

        IReadOnlyList<RecordingDispatchProxy<IBaseMap>.Invocation> adds =
            mapProxy.GetInvocations(nameof(IBaseMap.EnqueueAdd));
        Assert.Equal(3, adds.Count);
        Assert.Equal(3, createdEntities.Count);

        foreach (CreatedEntity createdEntity in createdEntities)
        {
            RecordingDispatchProxy<INonPlayerEntity>.Invocation initialise = Assert.Single(
                createdEntity.Proxy.GetInvocations(nameof(INonPlayerEntity.Initialise)));
            Assert.Equal(AvalancheCreature, initialise.Arguments[0]);

            Assert.Contains(createdEntity.Proxy.GetInvocations("set_" + nameof(INonPlayerEntity.CreateFlags)),
                invocation => ((EntityCreateFlag)invocation.Arguments[0] & EntityCreateFlag.Immediate) != 0);
        }

        foreach (RecordingDispatchProxy<IBaseMap>.Invocation add in adds)
        {
            Assert.IsAssignableFrom<INonPlayerEntity>(add.Arguments[0]);
            IMapPosition position = Assert.IsAssignableFrom<IMapPosition>(add.Arguments[1]);
            Assert.Equal(NorthernWildsWorld, position.Info.Entry.Id);
        }

        Assert.Contains(adds, add => IsMapPosition(add.Arguments[1], AvalancheSource));
        Assert.Contains(adds, add => IsMapPosition(add.Arguments[1], AvalancheSource2));
        Assert.Contains(adds, add => IsMapPosition(add.Arguments[1], AvalancheSource3));
    }

    [Fact]
    public void NorthernWildsMapScript_OnLoad_WhenStaticAvalancheRowsExist_StillSpawnsManagedFallbacks()
    {
        (_, RecordingDispatchProxy<IBaseMap> mapProxy, IReadOnlyList<CreatedEntity> createdEntities) =
            CreateMapScript(spawnExistingAvalanche: true);

        Assert.Equal(3, createdEntities.Count);
        Assert.Equal(3, mapProxy.GetInvocations(nameof(IBaseMap.EnqueueAdd)).Count);
    }

    [Fact]
    public void NorthernWildsMapScript_OnAddToMap_RemovesImportedStaticAvalancheRows()
    {
        (NorthernWildsMapScript script, _, _) = CreateMapScript(spawnExistingAvalanche: false);
        IWorldEntity importedAvalanche = CreateMapAvalancheEntity(
            out RecordingDispatchProxy<IWorldEntity> importedAvalancheProxy,
            EntityCreateFlag.None,
            entityId: 123u,
            position: AvalancheFinalRunout);

        script.OnAddToMap(importedAvalanche);

        Assert.Single(importedAvalancheProxy.GetInvocations(nameof(IWorldEntity.RemoveFromMap)));
    }

    [Fact]
    public void NorthernWildsMapScript_OnAddToMap_RemovesImportedStaticAvalancheRowsEvenWhenImmediate()
    {
        (NorthernWildsMapScript script, _, _) = CreateMapScript(spawnExistingAvalanche: false);
        IWorldEntity importedAvalanche = CreateMapAvalancheEntity(
            out RecordingDispatchProxy<IWorldEntity> importedAvalancheProxy,
            EntityCreateFlag.Immediate,
            entityId: 123u,
            position: AvalancheFinalRunout);

        script.OnAddToMap(importedAvalanche);

        Assert.Single(importedAvalancheProxy.GetInvocations(nameof(IWorldEntity.RemoveFromMap)));
    }

    [Fact]
    public void NorthernWildsMapScript_OnAddToMap_RemovesImportedStaticAvalancheCasterRows()
    {
        (NorthernWildsMapScript script, _, _) = CreateMapScript(spawnExistingAvalanche: false);
        IWorldEntity importedAvalanche = CreateMapAvalancheEntity(
            out RecordingDispatchProxy<IWorldEntity> importedAvalancheProxy,
            EntityCreateFlag.None,
            entityId: 123u,
            position: AvalancheFinalRunout,
            creatureId: AvalancheCasterCreature);

        script.OnAddToMap(importedAvalanche);

        Assert.Single(importedAvalancheProxy.GetInvocations(nameof(IWorldEntity.RemoveFromMap)));
    }

    [Fact]
    public void NorthernWildsMapScript_OnAddToMap_KeepsManagedAvalancheFallbacks()
    {
        (NorthernWildsMapScript script, _, _) = CreateMapScript(spawnExistingAvalanche: false);
        IWorldEntity managedAvalanche = CreateMapAvalancheEntity(
            out RecordingDispatchProxy<IWorldEntity> managedAvalancheProxy,
            EntityCreateFlag.Immediate,
            entityId: 0u,
            position: AvalancheSource);

        script.OnAddToMap(managedAvalanche);

        Assert.Empty(managedAvalancheProxy.GetInvocations(nameof(IWorldEntity.RemoveFromMap)));
    }

    [Fact]
    public void NorthernWildsMapScript_OnLoad_SpawnsMissingDropPodVisibilityFallbacks()
    {
        (_, RecordingDispatchProxy<IBaseMap> mapProxy, IReadOnlyList<CreatedEntity> createdEntities) =
            CreateMapScript(spawnExistingAvalanche: true, spawnExistingDropPods: false);

        IReadOnlyList<RecordingDispatchProxy<IBaseMap>.Invocation> adds =
            mapProxy.GetInvocations(nameof(IBaseMap.EnqueueAdd));
        Assert.Equal(7, adds.Count);
        Assert.Equal(7, createdEntities.Count);
        Assert.Equal(3, createdEntities.Count(entity => GetInitialisedCreatureId(entity) == AvalancheCreature));
        Assert.Equal(2, createdEntities.Count(entity => GetInitialisedCreatureId(entity) == GranokDropPodCreature));
        Assert.Equal(2, createdEntities.Count(entity => GetInitialisedCreatureId(entity) == GranokDropPodDecalCreature));

        foreach (CreatedEntity createdEntity in createdEntities)
        {
            Assert.Contains(createdEntity.Proxy.GetInvocations("set_" + nameof(INonPlayerEntity.CreateFlags)),
                invocation => ((EntityCreateFlag)invocation.Arguments[0] & EntityCreateFlag.Immediate) != 0);
        }

        foreach (CreatedEntity decalEntity in createdEntities.Where(entity => GetInitialisedCreatureId(entity) == GranokDropPodDecalCreature))
        {
            RecordingDispatchProxy<INonPlayerEntity>.Invocation questChecklist = Assert.Single(
                decalEntity.Proxy.GetInvocations(nameof(INonPlayerEntity.SetQuestChecklistIndex)));
            Assert.Equal(GranokDropPodDecalQuestChecklistIdx, questChecklist.Arguments[0]);
        }

        Assert.Contains(adds, add => IsMapPosition(add.Arguments[1], 4119.69f, -681.132f, -5193.9f));
        Assert.Contains(adds, add => IsMapPosition(add.Arguments[1], 4115.84f, -684.355f, -5215.019f));
    }

    [Fact]
    public void OnAddToMap_SetsAvalancheHitRangeAndStartsMovementFromSource()
    {
        NorthernWildsAvalancheEntityScript script = CreateAvalancheScript(
            out RecordingDispatchProxy<ICreatureEntity> ownerProxy,
            out RecordingDispatchProxy<IMovementManager> movementProxy);

        script.OnAddToMap(CreateMap(NorthernWildsWorld));

        RecordingDispatchProxy<ICreatureEntity>.Invocation range = Assert.Single(
            ownerProxy.GetInvocations(nameof(ICreatureEntity.SetInRangeCheck)));
        Assert.Equal(12f, range.Arguments[0]);

        List<Vector3> path = AssertSingleAvalancheLaunchPath(movementProxy);
        Assert.Equal(Lifted(AvalancheSource), path[0]);
        Assert.Equal(Lifted(AvalancheFinalRunout), path[^1]);
        AssertClientObservedDownhill(path);

        Assert.Single(movementProxy.GetInvocations(nameof(IMovementManager.BroadcastNetworkEntityCommands)));
        AssertSingleBroadcastLaunchOrder(movementProxy);
    }

    [Fact]
    public void OnAddToMap_WhenRouteNodeIsBelowTerrain_LiftsMovementPathAboveTerrain()
    {
        float terrainAtSource = AvalancheSource.Y + 5f;
        NorthernWildsAvalancheEntityScript script = CreateAvalancheScript(
            out RecordingDispatchProxy<ICreatureEntity> ownerProxy,
            out RecordingDispatchProxy<IMovementManager> movementProxy,
            terrainHeight: (x, z) => IsClose(x, AvalancheSource.X) && IsClose(z, AvalancheSource.Z)
                ? terrainAtSource
                : null);

        script.OnAddToMap(CreateMap(NorthernWildsWorld));

        List<Vector3> path = AssertSingleAvalancheLaunchPath(movementProxy);
        Assert.Equal(AvalancheSource.X, path[0].X);
        Assert.Equal(AvalancheSource.Z, path[0].Z);
        Assert.Equal(terrainAtSource + 2.5f, path[0].Y);
    }

    [Fact]
    public void OnAddToMap_WhenTerrainRisesBetweenRouteNodes_PreservesDownhillPath()
    {
        Vector3 firstRouteBend = new(3955f, -670f, -5661f);
        Vector3 terrainSample = Vector3.Lerp(AvalancheSource, firstRouteBend, 0.6f);
        float terrainAtSample = terrainSample.Y + 10f;
        NorthernWildsAvalancheEntityScript script = CreateAvalancheScript(
            out RecordingDispatchProxy<ICreatureEntity> ownerProxy,
            out RecordingDispatchProxy<IMovementManager> movementProxy,
            terrainHeight: (x, z) => HorizontalDistanceSquared(x, z, terrainSample.X, terrainSample.Z) < 0.25f
                ? terrainAtSample
                : null);

        script.OnAddToMap(CreateMap(NorthernWildsWorld));

        List<Vector3> path = AssertSingleAvalancheLaunchPath(movementProxy);
        int closestMidpointIndex = Enumerable.Range(0, path.Count)
            .OrderBy(index => HorizontalDistanceSquared(path[index].X, path[index].Z, terrainSample.X, terrainSample.Z))
            .First();
        Vector3 closestMidpointNode = path[closestMidpointIndex];

        Assert.True(path.Count > 10);
        Assert.True(closestMidpointIndex > 0);
        Assert.True(HorizontalDistanceSquared(closestMidpointNode.X, closestMidpointNode.Z, terrainSample.X, terrainSample.Z) < 0.25f);
        Assert.True(closestMidpointNode.Y <= path[closestMidpointIndex - 1].Y);
        AssertClientObservedDownhill(path);
    }

    [Fact]
    public void OnAddVisibleEntity_AfterMapAdd_DoesNotLaunchDuplicateMovement()
    {
        NorthernWildsAvalancheEntityScript script = CreateAvalancheScript(
            out RecordingDispatchProxy<ICreatureEntity> ownerProxy,
            out RecordingDispatchProxy<IMovementManager> movementProxy);

        script.OnAddToMap(CreateMap(NorthernWildsWorld));
        ((IGridEntityScript)script).OnAddVisibleEntity(CreateVisiblePlayer(100u));

        RecordingDispatchProxy<IMovementManager>.Invocation mode = Assert.Single(
            movementProxy.GetInvocations(nameof(IMovementManager.SetMode)));
        Assert.Equal(ModeType.Walk, mode.Arguments[0]);

        List<Vector3> path = AssertSingleAvalancheLaunchPath(movementProxy);
        RecordingDispatchProxy<IMovementManager>.Invocation reset = Assert.Single(
            movementProxy.GetInvocations(nameof(IMovementManager.SetPosition)));
        Assert.Equal(path[0], reset.Arguments[0]);
        Assert.False((bool)reset.Arguments[1]);

        Assert.True(path.Count >= 2);
        Assert.Equal(Lifted(AvalancheSource), path[0]);
        Assert.Equal(Lifted(AvalancheFinalRunout), path[^1]);
        AssertClientObservedDownhill(path);

        Assert.Single(movementProxy.GetInvocations(nameof(IMovementManager.BroadcastNetworkEntityCommands)));
        AssertSingleBroadcastLaunchOrder(movementProxy);
    }

    [Fact]
    public void OnAddVisibleEntity_AfterMapAdd_WithMultiplePlayersDoesNotLaunchDuplicateMovement()
    {
        NorthernWildsAvalancheEntityScript script = CreateAvalancheScript(
            out RecordingDispatchProxy<ICreatureEntity> ownerProxy,
            out RecordingDispatchProxy<IMovementManager> movementProxy);

        script.OnAddToMap(CreateMap(NorthernWildsWorld));
        ((IGridEntityScript)script).OnAddVisibleEntity(CreateVisiblePlayer(100u));
        ((IGridEntityScript)script).OnAddVisibleEntity(CreateVisiblePlayer(101u));

        AssertSingleAvalancheLaunchPath(movementProxy);
        Assert.Single(movementProxy.GetInvocations(nameof(IMovementManager.BroadcastNetworkEntityCommands)));
    }

    [Fact]
    public void OnPositionEntityCommandFinalise_DoesNotResetOneShotVisualPath()
    {
        NorthernWildsAvalancheEntityScript script = CreateAvalancheScript(
            out RecordingDispatchProxy<ICreatureEntity> ownerProxy,
            out RecordingDispatchProxy<IMovementManager> movementProxy);
        script.OnAddToMap(CreateMap(NorthernWildsWorld));

        IPositionCommand command = RecordingDispatchProxy<IPositionCommand>.Create(out _);
        script.OnPositionEntityCommandFinalise(command);
        ((IGridEntityScript)script).OnAddVisibleEntity(CreateVisiblePlayer(100u));

        AdvanceAvalanchePastSpawn(script);

        AssertSingleAvalancheLaunchPath(movementProxy);
        Assert.Single(movementProxy.GetInvocations(nameof(IMovementManager.SetPosition)));
        Assert.Single(movementProxy.GetInvocations(nameof(IMovementManager.BroadcastNetworkEntityCommands)));
    }

    [Fact]
    public void OnAddToMap_WhenPlayerAlreadySeesAvalanche_StartsMovementFromSource()
    {
        NorthernWildsAvalancheEntityScript script = CreateAvalancheScript(
            out RecordingDispatchProxy<ICreatureEntity> ownerProxy,
            out RecordingDispatchProxy<IMovementManager> movementProxy);

        ((IGridEntityScript)script).OnAddVisibleEntity(CreateVisiblePlayer(100u));
        script.OnAddToMap(CreateMap(NorthernWildsWorld));

        List<Vector3> path = AssertSingleAvalancheLaunchPath(movementProxy);
        Assert.Equal(Lifted(AvalancheSource), path[0]);
        Assert.Equal(Lifted(AvalancheFinalRunout), path[^1]);
        AssertClientObservedDownhill(path);

        Assert.Single(movementProxy.GetInvocations(nameof(IMovementManager.BroadcastNetworkEntityCommands)));
        AssertSingleBroadcastLaunchOrder(movementProxy);
    }

    [Fact]
    public void OnAddToMap_WhenImportedAvalancheIsRunoutRow_LeavesRemovalToMapScript()
    {
        NorthernWildsAvalancheEntityScript script = CreateAvalancheScript(
            out RecordingDispatchProxy<ICreatureEntity> ownerProxy,
            out RecordingDispatchProxy<IMovementManager> movementProxy,
            position: AvalancheRunout,
            managedAvalanche: false);

        script.OnAddToMap(CreateMap(NorthernWildsWorld));

        Assert.Empty(ownerProxy.GetInvocations(nameof(ICreatureEntity.SetInRangeCheck)));
        Assert.Empty(ownerProxy.GetInvocations(nameof(ICreatureEntity.RemoveFromMap)));
        Assert.Empty(movementProxy.GetInvocations(nameof(IMovementManager.LaunchSpline)));
    }

    [Fact]
    public void OnAddToMap_WhenImportedSimpleAvalancheIsRunoutRow_LeavesRemovalToMapScript()
    {
        NorthernWildsAvalancheEntityScript script = CreateSimpleAvalancheScript(
            out RecordingDispatchProxy<ISimpleEntity> ownerProxy,
            out RecordingDispatchProxy<IMovementManager> movementProxy,
            position: AvalancheRunout,
            managedAvalanche: false);

        script.OnAddToMap(CreateMap(NorthernWildsWorld));

        Assert.Empty(ownerProxy.GetInvocations(nameof(ISimpleEntity.SetInRangeCheck)));
        Assert.Empty(ownerProxy.GetInvocations(nameof(ISimpleEntity.RemoveFromMap)));
        Assert.Empty(movementProxy.GetInvocations(nameof(IMovementManager.LaunchSpline)));
    }

    [Fact]
    public void OnAddToMap_WhenImportedSimpleAvalancheStartsAtSource_LeavesRemovalToMapScript()
    {
        NorthernWildsAvalancheEntityScript script = CreateSimpleAvalancheScript(
            out RecordingDispatchProxy<ISimpleEntity> ownerProxy,
            out RecordingDispatchProxy<IMovementManager> movementProxy,
            managedAvalanche: false);

        script.OnAddToMap(CreateMap(NorthernWildsWorld));

        Assert.Empty(ownerProxy.GetInvocations(nameof(ISimpleEntity.SetInRangeCheck)));
        Assert.Empty(ownerProxy.GetInvocations(nameof(ISimpleEntity.RemoveFromMap)));
        Assert.Empty(movementProxy.GetInvocations(nameof(IMovementManager.LaunchSpline)));
    }

    [Theory]
    [InlineData(3951f, -663f, -5673f, 3951f, -663f, -5673f, 4012f, -714f, -5606f)]
    [InlineData(3941f, -693f, -5607f, 3941f, -693f, -5607f, 4019f, -724f, -5546f)]
    [InlineData(3962f, -710f, -5540f, 3962f, -710f, -5540f, 4017f, -728f, -5518f)]
    public void OnAddToMap_WhenManagedAvalancheStartsInSourceRegion_UsesClosestDownhillRoute(
        float x,
        float y,
        float z,
        float sourceX,
        float sourceY,
        float sourceZ,
        float runoutX,
        float runoutY,
        float runoutZ)
    {
        Vector3 expectedSource = new(sourceX, sourceY, sourceZ);
        Vector3 expectedRunout = new(runoutX, runoutY, runoutZ);
        NorthernWildsAvalancheEntityScript script = CreateSimpleAvalancheScript(
            out RecordingDispatchProxy<ISimpleEntity> ownerProxy,
            out RecordingDispatchProxy<IMovementManager> movementProxy,
            position: new Vector3(x, y, z));

        script.OnAddToMap(CreateMap(NorthernWildsWorld));

        List<Vector3> path = AssertSingleAvalancheLaunchPath(movementProxy);
        Assert.Equal(Lifted(expectedSource), path[0]);
        Assert.Equal(Lifted(expectedRunout), path[^1]);
        AssertClientObservedDownhill(path);
    }

    [Fact]
    public void OnAddToMap_WhenImportedAvalancheUsesOldStaticSourceRow_LeavesRemovalToMapScript()
    {
        NorthernWildsAvalancheEntityScript script = CreateSimpleAvalancheScript(
            out RecordingDispatchProxy<ISimpleEntity> ownerProxy,
            out RecordingDispatchProxy<IMovementManager> movementProxy,
            position: ImportedStaticAvalancheRow,
            managedAvalanche: false);

        script.OnAddToMap(CreateMap(NorthernWildsWorld));

        Assert.Empty(ownerProxy.GetInvocations(nameof(ISimpleEntity.RemoveFromMap)));
        Assert.Empty(movementProxy.GetInvocations(nameof(IMovementManager.LaunchSpline)));
    }

    [Fact]
    public void OnPositionEntityCommandFinalise_DoesNotRelaunchAvalancheBeforeRunCompletes()
    {
        NorthernWildsAvalancheEntityScript script = CreateAvalancheScript(
            out RecordingDispatchProxy<ICreatureEntity> ownerProxy,
            out RecordingDispatchProxy<IMovementManager> movementProxy);
        script.OnAddToMap(CreateMap(NorthernWildsWorld));

        IPositionCommand command = RecordingDispatchProxy<IPositionCommand>.Create(out _);
        script.OnPositionEntityCommandFinalise(command);

        script.Update(0.1d);

        AssertSingleAvalancheLaunchPath(movementProxy);
        Assert.Single(movementProxy.GetInvocations(nameof(IMovementManager.SetPosition)));
        Assert.Empty(movementProxy.GetInvocations(nameof(IMovementManager.ResetTime)));
        Assert.Single(movementProxy.GetInvocations(nameof(IMovementManager.BroadcastNetworkEntityCommands)));
    }

    [Fact]
    public void Update_WhenRunCompletes_RelaunchesAvalancheFromSource()
    {
        NorthernWildsAvalancheEntityScript script = CreateAvalancheScript(
            out RecordingDispatchProxy<ICreatureEntity> ownerProxy,
            out RecordingDispatchProxy<IMovementManager> movementProxy);
        script.OnAddToMap(CreateMap(NorthernWildsWorld));
        List<Vector3> path = AssertSingleAvalancheLaunchPath(movementProxy);

        script.Update(CalculateAvalancheRunDuration(path) + 0.1d);

        IReadOnlyList<List<Vector3>> launches = AssertAvalancheLaunchPaths(movementProxy, 2);
        Assert.Equal(path, launches[1]);
        Assert.Equal(2, movementProxy.GetInvocations(nameof(IMovementManager.SetPosition)).Count);
        Assert.Empty(movementProxy.GetInvocations(nameof(IMovementManager.ResetTime)));
        Assert.Equal(2, movementProxy.GetInvocations(nameof(IMovementManager.BroadcastNetworkEntityCommands)).Count);
    }

    [Fact]
    public void OnPositionEntityCommandFinalise_WhenNoPlayerCanSeeAvalanche_RelaunchesOnlyAfterRunCompletes()
    {
        NorthernWildsAvalancheEntityScript script = CreateAvalancheScript(
            out RecordingDispatchProxy<ICreatureEntity> ownerProxy,
            out RecordingDispatchProxy<IMovementManager> movementProxy);
        script.OnAddToMap(CreateMap(NorthernWildsWorld));
        List<Vector3> path = AssertSingleAvalancheLaunchPath(movementProxy);
        ((IGridEntityScript)script).OnRemoveVisibleEntity(CreateVisiblePlayer(100u));

        IPositionCommand command = RecordingDispatchProxy<IPositionCommand>.Create(out _);
        script.OnPositionEntityCommandFinalise(command);
        script.Update(CalculateAvalancheRunDuration(path) + 0.1d);

        AssertAvalancheLaunchPaths(movementProxy, 2);
        Assert.Equal(2, movementProxy.GetInvocations(nameof(IMovementManager.SetPosition)).Count);
        Assert.Empty(movementProxy.GetInvocations(nameof(IMovementManager.ResetTime)));
        Assert.Equal(2, movementProxy.GetInvocations(nameof(IMovementManager.BroadcastNetworkEntityCommands)).Count);
    }

    [Fact]
    public void OnEnterRange_WhenPlayerIsHit_DamagesAndGrantsSnowSmash()
    {
        NorthernWildsAvalancheEntityScript script = CreateAvalancheScript(
            out RecordingDispatchProxy<ICreatureEntity> ownerProxy,
            movementPosition: AvalanchePastSpawn);
        script.OnAddToMap(CreateMap(NorthernWildsWorld));
        IPlayer player = CreatePlayer(
            health: 1000u,
            shield: 50u,
            alreadyCompletedAchievement: false,
            out RecordingDispatchProxy<IPlayer> playerProxy,
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementProxy,
            out RecordingDispatchProxy<IMovementManager> movementProxy,
            position: AvalanchePastSpawn);

        AdvanceAvalanchePastSpawn(script);
        script.OnEnterRange(player);

        RecordingDispatchProxy<IPlayer>.Invocation shieldUpdate = Assert.Single(
            playerProxy.GetInvocations("set_" + nameof(IPlayer.Shield)));
        Assert.Equal(0u, shieldUpdate.Arguments[0]);

        RecordingDispatchProxy<IPlayer>.Invocation damage = Assert.Single(
            playerProxy.GetInvocations(nameof(IPlayer.ModifyHealth)));
        Assert.Equal(300u, damage.Arguments[0]);
        Assert.Equal(DamageType.Magic, damage.Arguments[1]);
        Assert.IsAssignableFrom<ICreatureEntity>(damage.Arguments[2]);

        RecordingDispatchProxy<IPlayer>.Invocation knockdown = Assert.Single(
            playerProxy.GetInvocations(nameof(IPlayer.AddCCState)));
        Assert.Equal(CCState.Knockdown, knockdown.Arguments[0]);
        Assert.Equal(FirstAvalancheKnockdownEffect, knockdown.Arguments[1]);
        Assert.Equal(AvalancheKnockdownSpell, knockdown.Arguments[2]);
        Assert.Equal(FirstAvalancheCasting, knockdown.Arguments[3]);

        RecordingDispatchProxy<IMovementManager>.Invocation moveStop = Assert.Single(
            movementProxy.GetInvocations(nameof(IMovementManager.SetMove)));
        Assert.Equal(Vector3.Zero, moveStop.Arguments[0]);
        Assert.False((bool)moveStop.Arguments[1]);

        RecordingDispatchProxy<IMovementManager>.Invocation velocityStop = Assert.Single(
            movementProxy.GetInvocations(nameof(IMovementManager.SetVelocity)));
        Assert.Equal(Vector3.Zero, velocityStop.Arguments[0]);
        Assert.False((bool)velocityStop.Arguments[1]);

        RecordingDispatchProxy<IMovementManager>.Invocation stateStop = Assert.Single(
            movementProxy.GetInvocations(nameof(IMovementManager.SetState)));
        Assert.Equal(StateFlags.None, stateStop.Arguments[0]);

        IReadOnlyList<RecordingDispatchProxy<ICreatureEntity>.Invocation> ownerMessages =
            ownerProxy.GetInvocations(nameof(IWorldEntity.EnqueueToVisible)).ToList();
        Assert.Equal(4, ownerMessages.Count);

        ServerSpellStart spellStart = Assert.IsType<ServerSpellStart>(ownerMessages[0].Arguments[0]);
        Assert.Equal(FirstAvalancheCasting, spellStart.CastingId);
        Assert.Equal(AvalancheKnockdownSpell, spellStart.Spell4Id);
        Assert.Equal(AvalancheKnockdownSpell, spellStart.RootSpell4Id);
        Assert.Equal(AvalancheKnockdownSpell, spellStart.ParentSpell4Id);
        Assert.Equal(100u, spellStart.PrimaryTargetId);
        Assert.Single(spellStart.InitialPositionData);

        ServerSpellGo spellGo = Assert.IsType<ServerSpellGo>(ownerMessages[1].Arguments[0]);
        Assert.Equal(FirstAvalancheCasting, spellGo.ServerUniqueId);
        Assert.Single(spellGo.TargetInfoData);
        Assert.Equal(100u, spellGo.TargetInfoData[0].UnitId);

        Assert.Contains(spellGo.TargetInfoData[0].EffectInfoData, effect =>
            effect.Spell4EffectId == AvalancheKnockdownSpell4Effect
            && effect.EffectUniqueId == FirstAvalancheKnockdownEffect);
        SharedTargetInfo.EffectInfo damageEffect = Assert.Single(spellGo.TargetInfoData[0].EffectInfoData, effect =>
            effect.Spell4EffectId == AvalancheDamageSpell4Effect);
        Assert.Equal(FirstAvalancheDamageEffect, damageEffect.EffectUniqueId);
        Assert.Equal(50u, damageEffect.DamageDescriptionData.ShieldAbsorbAmount);
        Assert.Equal(300u, damageEffect.DamageDescriptionData.AdjustedDamage);
        Assert.Contains(spellGo.TargetInfoData[0].EffectInfoData, effect =>
            effect.Spell4EffectId == AvalancheAchievementSpell4Effect
            && effect.EffectUniqueId == 8003u);

        ServerEntityCCStateSet ccStateSet = Assert.IsType<ServerEntityCCStateSet>(ownerMessages[2].Arguments[0]);
        Assert.Equal(100u, ccStateSet.UnitId);
        Assert.Equal(CCState.Knockdown, ccStateSet.CCType);
        Assert.Equal(FirstAvalancheKnockdownEffect, ccStateSet.SpellEffectUniqueId);

        ServerSpellFinish spellFinish = Assert.IsType<ServerSpellFinish>(ownerMessages[3].Arguments[0]);
        Assert.Equal(FirstAvalancheCasting, spellFinish.ServerUniqueId);
        Assert.All(ownerMessages, message => Assert.True((bool)message.Arguments[1]));

        RecordingDispatchProxy<ICharacterAchievementManager>.Invocation grant = Assert.Single(
            achievementProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
        Assert.Equal(SnowSmashAchievement, grant.Arguments[0]);
    }

    [Fact]
    public void OnEnterRange_WhenPlayerAlreadyHasKnockdownState_StillAppliesAvalancheKnockdown()
    {
        NorthernWildsAvalancheEntityScript script = CreateAvalancheScript(
            out RecordingDispatchProxy<ICreatureEntity> ownerProxy,
            movementPosition: AvalanchePastSpawn);
        script.OnAddToMap(CreateMap(NorthernWildsWorld));
        IPlayer player = CreatePlayer(
            health: 1000u,
            shield: 0u,
            alreadyCompletedAchievement: false,
            out RecordingDispatchProxy<IPlayer> playerProxy,
            out _,
            out _,
            position: AvalanchePastSpawn);
        playerProxy.SetProperty(nameof(IPlayer.ActiveCCStateMask), 1u << (int)CCState.Knockdown);

        AdvanceAvalanchePastSpawn(script);
        script.OnEnterRange(player);

        RecordingDispatchProxy<IPlayer>.Invocation knockdown = Assert.Single(
            playerProxy.GetInvocations(nameof(IPlayer.AddCCState)));
        Assert.Equal(CCState.Knockdown, knockdown.Arguments[0]);
        Assert.Equal(FirstAvalancheKnockdownEffect, knockdown.Arguments[1]);
        Assert.Equal(AvalancheKnockdownSpell, knockdown.Arguments[2]);
        Assert.Equal(FirstAvalancheCasting, knockdown.Arguments[3]);

        IReadOnlyList<RecordingDispatchProxy<ICreatureEntity>.Invocation> ownerMessages =
            ownerProxy.GetInvocations(nameof(IWorldEntity.EnqueueToVisible)).ToList();
        ServerSpellGo spellGo = Assert.IsType<ServerSpellGo>(ownerMessages[1].Arguments[0]);
        Assert.Contains(spellGo.TargetInfoData[0].EffectInfoData, effect =>
            effect.Spell4EffectId == AvalancheKnockdownSpell4Effect
            && effect.EffectUniqueId == FirstAvalancheKnockdownEffect);

        ServerEntityCCStateSet ccStateSet = Assert.IsType<ServerEntityCCStateSet>(ownerMessages[2].Arguments[0]);
        Assert.Equal(CCState.Knockdown, ccStateSet.CCType);
        Assert.Equal(FirstAvalancheKnockdownEffect, ccStateSet.SpellEffectUniqueId);
    }

    [Fact]
    public void Update_WhenPlayerNearCurrentRoutePosition_DamagesAndGrantsSnowSmashWithoutRangeEvent()
    {
        IPlayer player = CreatePlayer(
            health: 1000u,
            shield: 50u,
            alreadyCompletedAchievement: false,
            out RecordingDispatchProxy<IPlayer> playerProxy,
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementProxy,
            out _,
            position: AvalanchePastSpawn);
        NorthernWildsAvalancheEntityScript script = CreateAvalancheScript(
            out _,
            searchedPlayers: [player],
            movementPosition: AvalanchePastSpawn);
        script.OnAddToMap(CreateMap(NorthernWildsWorld));

        AdvanceAvalanchePastSpawn(script);

        Assert.Single(playerProxy.GetInvocations("set_" + nameof(IPlayer.Shield)));
        Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.ModifyHealth)));
        Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.AddCCState)));
        Assert.Single(achievementProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
    }

    [Theory]
    [InlineData(QuestState.Achieved)]
    [InlineData(QuestState.Completed)]
    public void Update_WhenEmpoweredTowerHasStoppedStorm_DoesNotHitPlayer(QuestState questState)
    {
        IPlayer player = CreatePlayer(
            health: 1000u,
            shield: 50u,
            alreadyCompletedAchievement: false,
            out RecordingDispatchProxy<IPlayer> playerProxy,
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementProxy,
            out _,
            position: AvalanchePastSpawn,
            empoweredTowerState: questState);
        NorthernWildsAvalancheEntityScript script = CreateAvalancheScript(
            out RecordingDispatchProxy<ICreatureEntity> ownerProxy,
            searchedPlayers: [player],
            movementPosition: AvalanchePastSpawn);
        script.OnAddToMap(CreateMap(NorthernWildsWorld));

        AdvanceAvalanchePastSpawn(script);

        Assert.Empty(playerProxy.GetInvocations("set_" + nameof(IPlayer.Shield)));
        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.ModifyHealth)));
        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.AddCCState)));
        Assert.Empty(achievementProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
        Assert.DoesNotContain(ownerProxy.GetInvocations(nameof(IWorldEntity.EnqueueToVisible)),
            message => message.Arguments[0] is ServerSpellGo);
    }

    [Fact]
    public void Update_WhenPlayerIsWithinSpellSearchButOutsideContactRectangle_DoesNotHit()
    {
        IPlayer player = CreatePlayer(
            health: 1000u,
            shield: 50u,
            alreadyCompletedAchievement: false,
            out RecordingDispatchProxy<IPlayer> playerProxy,
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementProxy,
            out _,
            position: AvalanchePastSpawn + AvalancheSideOffset(10.5f));
        NorthernWildsAvalancheEntityScript script = CreateAvalancheScript(
            out RecordingDispatchProxy<ICreatureEntity> ownerProxy,
            searchedPlayers: [player],
            movementPosition: AvalanchePastSpawn);
        script.OnAddToMap(CreateMap(NorthernWildsWorld));

        AdvanceAvalanchePastSpawn(script);

        Assert.Empty(playerProxy.GetInvocations("set_" + nameof(IPlayer.Shield)));
        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.ModifyHealth)));
        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.AddCCState)));
        Assert.Empty(achievementProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
        Assert.DoesNotContain(ownerProxy.GetInvocations(nameof(IWorldEntity.EnqueueToVisible)),
            message => message.Arguments[0] is ServerSpellGo);
    }

    [Fact]
    public void Update_WhenAvalancheIsAtSpawnPoint_DoesNotHit()
    {
        IPlayer player = CreatePlayer(
            health: 1000u,
            shield: 50u,
            alreadyCompletedAchievement: false,
            out RecordingDispatchProxy<IPlayer> playerProxy,
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementProxy,
            out _,
            position: AvalancheSource);
        NorthernWildsAvalancheEntityScript script = CreateAvalancheScript(
            out RecordingDispatchProxy<ICreatureEntity> ownerProxy,
            searchedPlayers: [player],
            movementPosition: AvalancheSource);
        script.OnAddToMap(CreateMap(NorthernWildsWorld));

        script.Update(0.1d);

        Assert.Empty(playerProxy.GetInvocations("set_" + nameof(IPlayer.Shield)));
        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.ModifyHealth)));
        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.AddCCState)));
        Assert.Empty(achievementProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
        Assert.DoesNotContain(ownerProxy.GetInvocations(nameof(IWorldEntity.EnqueueToVisible)),
            message => message.Arguments[0] is ServerSpellGo);
    }

    [Fact]
    public void Update_WhenAvalancheRespawnsAtSource_DoesNotHitPlayerDownChuteUntilWallArrives()
    {
        IPlayer player = CreatePlayer(
            health: 1000u,
            shield: 50u,
            alreadyCompletedAchievement: false,
            out RecordingDispatchProxy<IPlayer> playerProxy,
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementProxy,
            out _,
            position: AvalanchePastSpawn);
        NorthernWildsAvalancheEntityScript script = CreateAvalancheScript(
            out RecordingDispatchProxy<ICreatureEntity> ownerProxy,
            out RecordingDispatchProxy<IMovementManager> movementProxy,
            searchedPlayers: [player],
            movementPosition: AvalanchePastSpawn);
        script.OnAddToMap(CreateMap(NorthernWildsWorld));
        List<Vector3> path = AssertSingleAvalancheLaunchPath(movementProxy);

        script.Update(CalculateAvalancheRunDuration(path) + 0.1d);

        AssertAvalancheLaunchPaths(movementProxy, 2);
        Assert.Empty(playerProxy.GetInvocations("set_" + nameof(IPlayer.Shield)));
        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.ModifyHealth)));
        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.AddCCState)));
        Assert.Empty(achievementProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
        Assert.DoesNotContain(ownerProxy.GetInvocations(nameof(IWorldEntity.EnqueueToVisible)),
            message => message.Arguments[0] is ServerSpellGo);

        AdvanceAvalanchePastSpawn(script);

        Assert.Single(playerProxy.GetInvocations("set_" + nameof(IPlayer.Shield)));
        Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.ModifyHealth)));
        Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.AddCCState)));
        Assert.Single(achievementProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
    }

    [Fact]
    public void Update_WhenPlayerIsBroadsideInsideVisualWall_DamagesAndKnocksDown()
    {
        IPlayer player = CreatePlayer(
            health: 1000u,
            shield: 50u,
            alreadyCompletedAchievement: false,
            out RecordingDispatchProxy<IPlayer> playerProxy,
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementProxy,
            out _,
            position: AvalanchePastSpawn + AvalancheSideOffset(9.75f));
        NorthernWildsAvalancheEntityScript script = CreateAvalancheScript(
            out _,
            searchedPlayers: [player],
            movementPosition: AvalanchePastSpawn);
        script.OnAddToMap(CreateMap(NorthernWildsWorld));

        AdvanceAvalanchePastSpawn(script);

        Assert.Single(playerProxy.GetInvocations("set_" + nameof(IPlayer.Shield)));
        Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.ModifyHealth)));
        Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.AddCCState)));
        Assert.Single(achievementProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
    }

    [Fact]
    public void Update_WhenPlayerIsAheadOfVisualWall_DoesNotHitYet()
    {
        IPlayer player = CreatePlayer(
            health: 1000u,
            shield: 50u,
            alreadyCompletedAchievement: false,
            out RecordingDispatchProxy<IPlayer> playerProxy,
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementProxy,
            out _,
            position: AvalanchePastSpawn + AvalancheForwardOffset(3.25f));
        NorthernWildsAvalancheEntityScript script = CreateAvalancheScript(
            out RecordingDispatchProxy<ICreatureEntity> ownerProxy,
            searchedPlayers: [player],
            movementPosition: AvalanchePastSpawn);
        script.OnAddToMap(CreateMap(NorthernWildsWorld));

        AdvanceAvalanchePastSpawn(script);

        Assert.Empty(playerProxy.GetInvocations("set_" + nameof(IPlayer.Shield)));
        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.ModifyHealth)));
        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.AddCCState)));
        Assert.Empty(achievementProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
        Assert.DoesNotContain(ownerProxy.GetInvocations(nameof(IWorldEntity.EnqueueToVisible)),
            message => message.Arguments[0] is ServerSpellGo);
    }

    [Fact]
    public void Update_WhenMovementPositionIsOffActivePath_DoesNotHitFromInternalRouteClock()
    {
        IPlayer player = CreatePlayer(
            health: 1000u,
            shield: 50u,
            alreadyCompletedAchievement: false,
            out RecordingDispatchProxy<IPlayer> playerProxy,
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementProxy,
            out _,
            position: AvalancheSource);
        NorthernWildsAvalancheEntityScript script = CreateAvalancheScript(
            out RecordingDispatchProxy<ICreatureEntity> ownerProxy,
            searchedPlayers: [player],
            movementPosition: Vector3.Zero);
        script.OnAddToMap(CreateMap(NorthernWildsWorld));

        AdvanceAvalanchePastSpawn(script);

        Assert.Empty(playerProxy.GetInvocations("set_" + nameof(IPlayer.Shield)));
        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.ModifyHealth)));
        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.AddCCState)));
        Assert.Empty(achievementProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
        Assert.DoesNotContain(ownerProxy.GetInvocations(nameof(IWorldEntity.EnqueueToVisible)),
            message => message.Arguments[0] is ServerSpellGo);
    }

    [Fact]
    public void Update_WhenPlayerMatchesHorizontallyButIsTooFarVertically_DoesNotHit()
    {
        IPlayer player = CreatePlayer(
            health: 1000u,
            shield: 50u,
            alreadyCompletedAchievement: false,
            out RecordingDispatchProxy<IPlayer> playerProxy,
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementProxy,
            out _,
            position: AvalanchePastSpawn + new Vector3(0f, 20f, 0f));
        NorthernWildsAvalancheEntityScript script = CreateAvalancheScript(
            out RecordingDispatchProxy<ICreatureEntity> ownerProxy,
            searchedPlayers: [player],
            movementPosition: AvalanchePastSpawn);
        script.OnAddToMap(CreateMap(NorthernWildsWorld));

        AdvanceAvalanchePastSpawn(script);

        Assert.Empty(playerProxy.GetInvocations("set_" + nameof(IPlayer.Shield)));
        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.ModifyHealth)));
        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.AddCCState)));
        Assert.Empty(achievementProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
        Assert.DoesNotContain(ownerProxy.GetInvocations(nameof(IWorldEntity.EnqueueToVisible)),
            message => message.Arguments[0] is ServerSpellGo);
    }

    [Fact]
    public void OnEnterRange_WhenPlayerIsOutsideCurrentRoutePosition_DoesNotHit()
    {
        NorthernWildsAvalancheEntityScript script = CreateAvalancheScript(out RecordingDispatchProxy<ICreatureEntity> ownerProxy);
        script.OnAddToMap(CreateMap(NorthernWildsWorld));
        IPlayer player = CreatePlayer(
            health: 1000u,
            shield: 0u,
            alreadyCompletedAchievement: false,
            out RecordingDispatchProxy<IPlayer> playerProxy,
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementProxy,
            out _,
            position: AvalancheRunout);

        AdvanceAvalanchePastSpawn(script);
        script.OnEnterRange(player);

        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.ModifyHealth)));
        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.AddCCState)));
        Assert.Empty(achievementProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
        Assert.DoesNotContain(ownerProxy.GetInvocations(nameof(IWorldEntity.EnqueueToVisible)),
            message => message.Arguments[0] is ServerSpellGo);
    }

    [Fact]
    public void OnEnterRange_WhenAchievementAlreadyComplete_DamagesWithoutGrantingAgain()
    {
        NorthernWildsAvalancheEntityScript script = CreateAvalancheScript(
            out _,
            movementPosition: AvalanchePastSpawn);
        script.OnAddToMap(CreateMap(NorthernWildsWorld));
        IPlayer player = CreatePlayer(
            health: 1000u,
            shield: 0u,
            alreadyCompletedAchievement: true,
            out RecordingDispatchProxy<IPlayer> playerProxy,
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementProxy,
            out _,
            position: AvalanchePastSpawn);

        AdvanceAvalanchePastSpawn(script);
        script.OnEnterRange(player);

        Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.ModifyHealth)));
        Assert.Empty(achievementProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
    }

    [Fact]
    public void OnEnterRange_WhenAchievementAlreadyComplete_DoesNotSendAchievementEffect()
    {
        NorthernWildsAvalancheEntityScript script = CreateAvalancheScript(
            out RecordingDispatchProxy<ICreatureEntity> ownerProxy,
            movementPosition: AvalanchePastSpawn);
        script.OnAddToMap(CreateMap(NorthernWildsWorld));
        IPlayer player = CreatePlayer(
            health: 1000u,
            shield: 0u,
            alreadyCompletedAchievement: true,
            out _,
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementProxy,
            out _,
            position: AvalanchePastSpawn);

        AdvanceAvalanchePastSpawn(script);
        script.OnEnterRange(player);

        IReadOnlyList<RecordingDispatchProxy<ICreatureEntity>.Invocation> ownerMessages =
            ownerProxy.GetInvocations(nameof(IWorldEntity.EnqueueToVisible)).ToList();
        ServerSpellGo spellGo = Assert.IsType<ServerSpellGo>(ownerMessages[1].Arguments[0]);
        Assert.DoesNotContain(spellGo.TargetInfoData[0].EffectInfoData, effect =>
            effect.Spell4EffectId == AvalancheAchievementSpell4Effect);
        Assert.Empty(achievementProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
    }

    [Fact]
    public void Update_WhenAvalancheKnockdownExpires_RemovesOwnKnockdownState()
    {
        NorthernWildsAvalancheEntityScript script = CreateAvalancheScript(
            out _,
            movementPosition: AvalanchePastSpawn);
        script.OnAddToMap(CreateMap(NorthernWildsWorld));
        IPlayer player = CreatePlayer(
            health: 1000u,
            shield: 0u,
            alreadyCompletedAchievement: false,
            out RecordingDispatchProxy<IPlayer> playerProxy,
            out _,
            out _,
            position: AvalanchePastSpawn);
        playerProxy.SetMethodReturn(nameof(IPlayer.RemoveCCState), true);

        AdvanceAvalanchePastSpawn(script);
        script.OnEnterRange(player);
        script.Update(2.6d);

        RecordingDispatchProxy<IPlayer>.Invocation removeState = Assert.Single(
            playerProxy.GetInvocations(nameof(IPlayer.RemoveCCState)));
        Assert.Equal(CCState.Knockdown, removeState.Arguments[0]);
        Assert.Equal(FirstAvalancheKnockdownEffect, removeState.Arguments[1]);

        IReadOnlyList<RecordingDispatchProxy<IPlayer>.Invocation> messages =
            playerProxy.GetInvocations(nameof(IPlayer.EnqueueToVisible));
        RecordingDispatchProxy<IPlayer>.Invocation message = Assert.Single(messages);
        ServerEntityCCStateRemove ccStateRemove = Assert.IsType<ServerEntityCCStateRemove>(message.Arguments[0]);
        Assert.Equal(100u, ccStateRemove.UnitId);
        Assert.Equal(CCState.Knockdown, ccStateRemove.CCType);
        Assert.Equal(FirstAvalancheCasting, ccStateRemove.SpellCastUniqueId);
        Assert.Equal(FirstAvalancheKnockdownEffect, ccStateRemove.SpellEffectUniqueId);
        Assert.True(ccStateRemove.Removed);
    }

    [Fact]
    public void OnRemoveFromMap_WhenKnockdownIsActive_RemovesOwnKnockdownState()
    {
        NorthernWildsAvalancheEntityScript script = CreateAvalancheScript(
            out _,
            movementPosition: AvalanchePastSpawn);
        IBaseMap map = CreateMap(NorthernWildsWorld);
        script.OnAddToMap(map);
        IPlayer player = CreatePlayer(
            health: 1000u,
            shield: 0u,
            alreadyCompletedAchievement: false,
            out RecordingDispatchProxy<IPlayer> playerProxy,
            out _,
            out _,
            position: AvalanchePastSpawn);
        playerProxy.SetMethodReturn(nameof(IPlayer.RemoveCCState), true);

        AdvanceAvalanchePastSpawn(script);
        script.OnEnterRange(player);
        script.OnRemoveFromMap(map);

        RecordingDispatchProxy<IPlayer>.Invocation removeState = Assert.Single(
            playerProxy.GetInvocations(nameof(IPlayer.RemoveCCState)));
        Assert.Equal(CCState.Knockdown, removeState.Arguments[0]);
        Assert.Equal(FirstAvalancheKnockdownEffect, removeState.Arguments[1]);

        IReadOnlyList<RecordingDispatchProxy<IPlayer>.Invocation> messages =
            playerProxy.GetInvocations(nameof(IPlayer.EnqueueToVisible));
        RecordingDispatchProxy<IPlayer>.Invocation message = Assert.Single(messages);
        ServerEntityCCStateRemove ccStateRemove = Assert.IsType<ServerEntityCCStateRemove>(message.Arguments[0]);
        Assert.Equal(FirstAvalancheCasting, ccStateRemove.SpellCastUniqueId);
        Assert.Equal(FirstAvalancheKnockdownEffect, ccStateRemove.SpellEffectUniqueId);
        Assert.True(ccStateRemove.Removed);
    }

    [Fact]
    public void OnEnterRange_WhenCalledTwiceWithinCooldown_HitsOnlyOnce()
    {
        NorthernWildsAvalancheEntityScript script = CreateAvalancheScript(
            out _,
            movementPosition: AvalanchePastSpawn);
        script.OnAddToMap(CreateMap(NorthernWildsWorld));
        IPlayer player = CreatePlayer(
            health: 1000u,
            shield: 0u,
            alreadyCompletedAchievement: false,
            out RecordingDispatchProxy<IPlayer> playerProxy,
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementProxy,
            out _,
            position: AvalanchePastSpawn);

        AdvanceAvalanchePastSpawn(script);
        script.OnEnterRange(player);
        script.OnEnterRange(player);

        Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.ModifyHealth)));
        Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.AddCCState)));
        Assert.Single(achievementProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
    }

    [Fact]
    public void OnEnterRange_WhenAvalancheIsOutsideNorthernWilds_DoesNothing()
    {
        NorthernWildsAvalancheEntityScript script = CreateAvalancheScript(out _, worldId: 51u);
        script.OnAddToMap(CreateMap(51u));
        IPlayer player = CreatePlayer(
            health: 1000u,
            shield: 0u,
            alreadyCompletedAchievement: false,
            out RecordingDispatchProxy<IPlayer> playerProxy,
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementProxy);

        script.OnEnterRange(player);

        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.ModifyHealth)));
        Assert.Empty(achievementProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
    }

    private static (NorthernWildsMapScript Script, RecordingDispatchProxy<IBaseMap> MapProxy, IReadOnlyList<CreatedEntity> CreatedEntities) CreateMapScript(
        bool spawnExistingAvalanche,
        bool spawnExistingDropPods = true)
    {
        IWorldEntity existingAvalanche = RecordingDispatchProxy<IWorldEntity>.Create(out RecordingDispatchProxy<IWorldEntity> existingAvalancheProxy);
        existingAvalancheProxy.SetProperty(nameof(IWorldEntity.CreatureId), AvalancheCreature);
        IWorldEntity existingDropPod = RecordingDispatchProxy<IWorldEntity>.Create(out RecordingDispatchProxy<IWorldEntity> existingDropPodProxy);
        existingDropPodProxy.SetProperty(nameof(IWorldEntity.CreatureId), GranokDropPodCreature);
        IWorldEntity existingDropPodDecal = RecordingDispatchProxy<IWorldEntity>.Create(out RecordingDispatchProxy<IWorldEntity> existingDropPodDecalProxy);
        existingDropPodDecalProxy.SetProperty(nameof(IWorldEntity.CreatureId), GranokDropPodDecalCreature);

        IBaseMap map = CreateMap(NorthernWildsWorld, out RecordingDispatchProxy<IBaseMap> mapProxy);
        mapProxy.SetProperty(nameof(IBaseMap.PublicEventManager), RecordingDispatchProxy<IPublicEventManager>.Create(out _));
        mapProxy.SetMethodHandler(nameof(IBaseMap.Search), args =>
        {
            if (args[2] is ISearchCheck<IWorldEntity> check)
            {
                if (spawnExistingAvalanche && check.CheckEntity(existingAvalanche))
                    return new[] { existingAvalanche };

                if (spawnExistingDropPods && check.CheckEntity(existingDropPod))
                    return new[] { existingDropPod };

                if (spawnExistingDropPods && check.CheckEntity(existingDropPodDecal))
                    return new[] { existingDropPodDecal };
            }

            return Array.Empty<IWorldEntity>();
        });

        List<CreatedEntity> createdEntities = [];
        IEntityFactory entityFactory = RecordingDispatchProxy<IEntityFactory>.Create(out RecordingDispatchProxy<IEntityFactory> entityFactoryProxy);
        entityFactoryProxy.SetMethodReturnFactory(nameof(IEntityFactory.CreateEntity), () =>
        {
            INonPlayerEntity entity = RecordingDispatchProxy<INonPlayerEntity>.Create(out RecordingDispatchProxy<INonPlayerEntity> proxy);
            createdEntities.Add(new CreatedEntity(entity, proxy));
            return entity;
        });

        IGameTableManager gameTableManager = RecordingDispatchProxy<IGameTableManager>.Create(out _);
        ICinematicFactory cinematicFactory = RecordingDispatchProxy<ICinematicFactory>.Create(out _);
        IStoryBuilder storyBuilder = RecordingDispatchProxy<IStoryBuilder>.Create(out _);

        var script = new NorthernWildsMapScript(
            NullLogger<NorthernWildsMapScript>.Instance,
            entityFactory,
            gameTableManager,
            cinematicFactory,
            storyBuilder);
        script.OnLoad(map);
        return (script, mapProxy, createdEntities);
    }

    private static IWorldEntity CreateMapAvalancheEntity(
        out RecordingDispatchProxy<IWorldEntity> proxy,
        EntityCreateFlag createFlags,
        uint entityId,
        Vector3 position,
        uint creatureId = AvalancheCreature)
    {
        IWorldEntity entity = RecordingDispatchProxy<IWorldEntity>.Create(out proxy);
        proxy.SetProperty(nameof(IWorldEntity.EntityId), entityId);
        proxy.SetProperty(nameof(IWorldEntity.CreatureId), creatureId);
        proxy.SetProperty(nameof(IWorldEntity.CreateFlags), createFlags);
        proxy.SetProperty(nameof(IWorldEntity.Position), position);
        return entity;
    }

    private static uint GetInitialisedCreatureId(CreatedEntity createdEntity)
    {
        RecordingDispatchProxy<INonPlayerEntity>.Invocation initialise = Assert.Single(
            createdEntity.Proxy.GetInvocations(nameof(INonPlayerEntity.Initialise)));
        return (uint)initialise.Arguments[0];
    }

    private static bool IsMapPosition(object value, float x, float y, float z)
    {
        IMapPosition position = Assert.IsAssignableFrom<IMapPosition>(value);
        const float tolerance = 0.001f;
        return MathF.Abs(position.Position.X - x) < tolerance
            && MathF.Abs(position.Position.Y - y) < tolerance
            && MathF.Abs(position.Position.Z - z) < tolerance;
    }

    private static bool IsMapPosition(object value, Vector3 expected)
    {
        return IsMapPosition(value, expected.X, expected.Y, expected.Z);
    }

    private static Vector3 Lifted(Vector3 position)
    {
        return new Vector3(position.X, position.Y + AvalancheRouteVisualLift, position.Z);
    }

    private static void AssertClientObservedDownhill(IReadOnlyList<Vector3> path)
    {
        Assert.True(path[^1].Y < path[0].Y, $"Avalanche route climbed instead of descending from {path[0]} to {path[^1]}.");
        Assert.True(path[^1].Z > path[0].Z, $"Avalanche route moved away from the observed runout from {path[0]} to {path[^1]}.");
        Assert.True(path[^1].X > path[0].X, $"Avalanche route moved back toward the observed source cluster from {path[0]} to {path[^1]}.");

        for (int i = 1; i < path.Count; i++)
            Assert.True(path[i].Y <= path[i - 1].Y, $"Avalanche route segment {i - 1}->{i} climbs from {path[i - 1]} to {path[i]}.");
    }

    private static List<Vector3> AssertSingleAvalancheLaunchPath(RecordingDispatchProxy<IMovementManager> movementProxy)
    {
        RecordingDispatchProxy<IMovementManager>.Invocation movement = Assert.Single(
            movementProxy.GetInvocations(nameof(IMovementManager.LaunchSpline)));
        return AssertAvalancheLaunchInvocation(movement);
    }

    private static IReadOnlyList<List<Vector3>> AssertAvalancheLaunchPaths(
        RecordingDispatchProxy<IMovementManager> movementProxy,
        int expectedCount)
    {
        IReadOnlyList<RecordingDispatchProxy<IMovementManager>.Invocation> movements =
            movementProxy.GetInvocations(nameof(IMovementManager.LaunchSpline));
        Assert.Equal(expectedCount, movements.Count);
        return movements
            .Select(AssertAvalancheLaunchInvocation)
            .ToArray();
    }

    private static List<Vector3> AssertAvalancheLaunchInvocation(RecordingDispatchProxy<IMovementManager>.Invocation movement)
    {
        List<Vector3> path = Assert.IsType<List<Vector3>>(movement.Arguments[0]);
        Assert.Equal(SplineType.Linear, movement.Arguments[1]);
        Assert.Equal(SplineMode.OneShot, movement.Arguments[2]);
        Assert.Equal(AvalancheMoveSpeed, movement.Arguments[3]);
        return path;
    }

    private static double CalculateAvalancheRunDuration(IReadOnlyList<Vector3> path)
    {
        double length = 0d;
        for (int i = 1; i < path.Count; i++)
            length += Vector3.Distance(path[i - 1], path[i]);

        return length / AvalancheMoveSpeed;
    }

    private static void AssertSingleBroadcastLaunchOrder(RecordingDispatchProxy<IMovementManager> movementProxy)
    {
        string[] methodNames = movementProxy.Invocations
            .Select(invocation => invocation.MethodName)
            .ToArray();

        Assert.Contains(nameof(IMovementManager.SetPosition), methodNames);
        Assert.Contains(nameof(IMovementManager.SetMode), methodNames);
        Assert.Contains(nameof(IMovementManager.LaunchSpline), methodNames);
        Assert.Contains(nameof(IMovementManager.BroadcastNetworkEntityCommands), methodNames);
        Assert.True(
            Array.IndexOf(methodNames, nameof(IMovementManager.SetPosition)) <
            Array.IndexOf(methodNames, nameof(IMovementManager.LaunchSpline)));
        Assert.True(
            Array.IndexOf(methodNames, nameof(IMovementManager.SetMode)) <
            Array.IndexOf(methodNames, nameof(IMovementManager.LaunchSpline)));
        Assert.True(
            Array.IndexOf(methodNames, nameof(IMovementManager.LaunchSpline)) <
            Array.IndexOf(methodNames, nameof(IMovementManager.BroadcastNetworkEntityCommands)));
        Assert.Single(methodNames, methodName => methodName == nameof(IMovementManager.LaunchSpline));
        Assert.Single(methodNames, methodName => methodName == nameof(IMovementManager.BroadcastNetworkEntityCommands));
    }

    private static NorthernWildsAvalancheEntityScript CreateAvalancheScript(
        out RecordingDispatchProxy<ICreatureEntity> ownerProxy,
        uint worldId = NorthernWildsWorld,
        IReadOnlyCollection<IPlayer> searchedPlayers = null,
        Vector3? movementPosition = null,
        Func<float, float, float?> terrainHeight = null)
    {
        return CreateAvalancheScript(
            out ownerProxy,
            out _,
            worldId,
            searchedPlayers: searchedPlayers,
            movementPosition: movementPosition,
            terrainHeight: terrainHeight);
    }

    private static NorthernWildsAvalancheEntityScript CreateAvalancheScript(
        out RecordingDispatchProxy<ICreatureEntity> ownerProxy,
        out RecordingDispatchProxy<IMovementManager> movementProxy,
        uint worldId = NorthernWildsWorld,
        Vector3? position = null,
        bool managedAvalanche = true,
        IReadOnlyCollection<IPlayer> searchedPlayers = null,
        Vector3? movementPosition = null,
        Func<float, float, float?> terrainHeight = null)
    {
        IBaseMap map = CreateMap(worldId, out RecordingDispatchProxy<IBaseMap> mapProxy);
        IMovementManager movementManager = RecordingDispatchProxy<IMovementManager>.Create(out movementProxy);
        movementProxy.SetMethodReturn(nameof(IMovementManager.GetPosition), movementPosition ?? position ?? AvalancheSource);
        SetPlayerSearchHandler(mapProxy, searchedPlayers);
        SetTerrainHeightHandler(mapProxy, terrainHeight);

        ICreatureEntity owner = RecordingDispatchProxy<ICreatureEntity>.Create(out ownerProxy);
        ownerProxy.SetProperty(nameof(ICreatureEntity.Map), map);
        ownerProxy.SetProperty(nameof(ICreatureEntity.CreatureId), AvalancheCreature);
        ownerProxy.SetProperty(nameof(ICreatureEntity.Position), position ?? AvalancheSource);
        ownerProxy.SetProperty(nameof(ICreatureEntity.CreateFlags), managedAvalanche ? EntityCreateFlag.Immediate : EntityCreateFlag.None);
        ownerProxy.SetProperty(nameof(ICreatureEntity.MovementManager), movementManager);
        ownerProxy.SetMethodReturn(nameof(ICreatureEntity.GetInRange), Array.Empty<IPlayer>());

        var script = new NorthernWildsAvalancheEntityScript(CreateGlobalSpellManager());
        script.OnLoad(owner);
        return script;
    }

    private static NorthernWildsAvalancheEntityScript CreateSimpleAvalancheScript(
        out RecordingDispatchProxy<ISimpleEntity> ownerProxy,
        out RecordingDispatchProxy<IMovementManager> movementProxy,
        uint worldId = NorthernWildsWorld,
        Vector3? position = null,
        bool managedAvalanche = true,
        IReadOnlyCollection<IPlayer> searchedPlayers = null,
        Vector3? movementPosition = null,
        Func<float, float, float?> terrainHeight = null)
    {
        IBaseMap map = CreateMap(worldId, out RecordingDispatchProxy<IBaseMap> mapProxy);
        IMovementManager movementManager = RecordingDispatchProxy<IMovementManager>.Create(out movementProxy);
        movementProxy.SetMethodReturn(nameof(IMovementManager.GetPosition), movementPosition ?? position ?? AvalancheSource);
        SetPlayerSearchHandler(mapProxy, searchedPlayers);
        SetTerrainHeightHandler(mapProxy, terrainHeight);

        ISimpleEntity owner = RecordingDispatchProxy<ISimpleEntity>.Create(out ownerProxy);
        ownerProxy.SetProperty(nameof(ISimpleEntity.Map), map);
        ownerProxy.SetProperty(nameof(ISimpleEntity.CreatureId), AvalancheCreature);
        ownerProxy.SetProperty(nameof(ISimpleEntity.Position), position ?? AvalancheSource);
        ownerProxy.SetProperty(nameof(ISimpleEntity.CreateFlags), managedAvalanche ? EntityCreateFlag.Immediate : EntityCreateFlag.None);
        ownerProxy.SetProperty(nameof(ISimpleEntity.MovementManager), movementManager);
        ownerProxy.SetMethodReturn(nameof(ISimpleEntity.GetInRange), Array.Empty<IPlayer>());

        var script = new NorthernWildsAvalancheEntityScript(CreateGlobalSpellManager());
        script.OnLoad(owner);
        return script;
    }

    private static IGlobalSpellManager CreateGlobalSpellManager()
    {
        IGlobalSpellManager spellManager = RecordingDispatchProxy<IGlobalSpellManager>.Create(
            out RecordingDispatchProxy<IGlobalSpellManager> spellManagerProxy);
        uint nextCastingId = FirstAvalancheCasting;
        uint nextEffectId = FirstAvalancheDamageEffect;

        spellManagerProxy.SetMethodReturnFactory("get_" + nameof(IGlobalSpellManager.NextCastingId), () => nextCastingId++);
        spellManagerProxy.SetMethodReturnFactory("get_" + nameof(IGlobalSpellManager.NextEffectId), () => nextEffectId++);
        return spellManager;
    }

    private static IPlayer CreatePlayer(
        uint health,
        uint shield,
        bool alreadyCompletedAchievement,
        out RecordingDispatchProxy<IPlayer> playerProxy,
        out RecordingDispatchProxy<ICharacterAchievementManager> achievementProxy)
    {
        return CreatePlayer(
            health,
            shield,
            alreadyCompletedAchievement,
            out playerProxy,
            out achievementProxy,
            out _);
    }

    private static IPlayer CreatePlayer(
        uint health,
        uint shield,
        bool alreadyCompletedAchievement,
        out RecordingDispatchProxy<IPlayer> playerProxy,
        out RecordingDispatchProxy<ICharacterAchievementManager> achievementProxy,
        out RecordingDispatchProxy<IMovementManager> movementProxy,
        Vector3? position = null,
        QuestState? empoweredTowerState = null)
    {
        ICharacterAchievementManager achievementManager =
            RecordingDispatchProxy<ICharacterAchievementManager>.Create(out achievementProxy);
        achievementProxy.SetMethodHandler(nameof(ICharacterAchievementManager.HasCompletedAchievement), args =>
        {
            Assert.Equal(SnowSmashAchievement, args[0]);
            return alreadyCompletedAchievement;
        });
        IMovementManager movementManager = RecordingDispatchProxy<IMovementManager>.Create(out movementProxy);
        movementProxy.SetMethodReturn(nameof(IMovementManager.GetState), StateFlags.Move | StateFlags.Velocity | StateFlags.Jump);
        IQuestManager questManager = RecordingDispatchProxy<IQuestManager>.Create(out RecordingDispatchProxy<IQuestManager> questProxy);
        questProxy.SetMethodHandler(nameof(IQuestManager.GetQuestState), args =>
        {
            return (ushort)args[0] == EmpoweredTowerQuest
                ? empoweredTowerState
                : null;
        });

        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), 42ul);
        playerProxy.SetProperty(nameof(IPlayer.Guid), 100u);
        playerProxy.SetProperty(nameof(IPlayer.IsAlive), true);
        playerProxy.SetProperty(nameof(IPlayer.Health), health);
        playerProxy.SetProperty(nameof(IPlayer.Shield), shield);
        playerProxy.SetProperty(nameof(IPlayer.Position), position ?? AvalancheSource);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievementManager);
        playerProxy.SetProperty(nameof(IPlayer.MovementManager), movementManager);
        playerProxy.SetProperty(nameof(IPlayer.QuestManager), questManager);
        return player;
    }

    private static IPlayer CreateVisiblePlayer(uint guid)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Guid), guid);
        return player;
    }

    private static Vector3 AvalancheForwardOffset(float distance)
    {
        return new Vector3(AvalancheSourceForward2D.X * distance, 0f, AvalancheSourceForward2D.Y * distance);
    }

    private static Vector3 AvalancheSideOffset(float distance)
    {
        return new Vector3(-AvalancheSourceForward2D.Y * distance, 0f, AvalancheSourceForward2D.X * distance);
    }

    private static void AdvanceAvalanchePastSpawn(NorthernWildsAvalancheEntityScript script)
    {
        script.Update(0.8d);
    }

    private static IBaseMap CreateMap(uint worldId)
    {
        return CreateMap(worldId, out _);
    }

    private static IBaseMap CreateMap(uint worldId, out RecordingDispatchProxy<IBaseMap> mapProxy)
    {
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out mapProxy);
        mapProxy.SetProperty(nameof(IBaseMap.Entry), new WorldEntry { Id = worldId });
        return map;
    }

    private static void SetPlayerSearchHandler(
        RecordingDispatchProxy<IBaseMap> mapProxy,
        IReadOnlyCollection<IPlayer> searchedPlayers = null)
    {
        mapProxy.SetMethodHandler(nameof(IBaseMap.Search), args =>
        {
            if (args[2] is ISearchCheck<IPlayer> check)
                return (searchedPlayers ?? Array.Empty<IPlayer>())
                    .Where(check.CheckEntity)
                    .ToArray();

            if (args[2] is ISearchCheck<IWorldEntity>)
                return Array.Empty<IWorldEntity>();

            return Array.Empty<IGridEntity>();
        });
    }

    private static void SetTerrainHeightHandler(
        RecordingDispatchProxy<IBaseMap> mapProxy,
        Func<float, float, float?> terrainHeight = null)
    {
        if (terrainHeight == null)
            return;

        mapProxy.SetMethodHandler(nameof(IBaseMap.GetTerrainHeight), args => terrainHeight((float)args[0], (float)args[1]));
    }

    private static bool IsClose(float actual, float expected)
    {
        return MathF.Abs(actual - expected) < 0.001f;
    }

    private static float HorizontalDistanceSquared(float x1, float z1, float x2, float z2)
    {
        return Vector2.DistanceSquared(new Vector2(x1, z1), new Vector2(x2, z2));
    }

    private sealed record CreatedEntity(INonPlayerEntity Instance, RecordingDispatchProxy<INonPlayerEntity> Proxy);
}
