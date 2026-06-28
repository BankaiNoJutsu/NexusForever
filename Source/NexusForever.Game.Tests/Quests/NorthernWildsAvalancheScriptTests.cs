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
using NexusForever.Game.Abstract.Story;
using NexusForever.Game.Static.Combat.CrowdControl;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Entity.Movement.Command.Mode;
using NexusForever.Game.Static.Entity.Movement.Command.State;
using NexusForever.Game.Static.Entity.Movement.Spline;
using NexusForever.Game.Static.Spell;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model.Entity;
using NexusForever.Script.Main.Quests.NorthernWilds;

namespace NexusForever.Game.Tests.Quests;

public class NorthernWildsAvalancheScriptTests
{
    private const uint NorthernWildsWorld = 426u;
    private const uint AvalancheCreature = 67662u;
    private const uint GranokDropPodCreature = 13747u;
    private const uint GranokDropPodDecalCreature = 14126u;
    private const byte GranokDropPodDecalQuestChecklistIdx = 255;
    private const ushort SnowSmashAchievement = 3504;
    private const uint AvalancheKnockdownEffect = AvalancheCreature;
    private static readonly Vector3 AvalancheSource = new(3952f, -664f, -5672f);
    private static readonly Vector3 AvalancheNearSource = new(3959f, -678f, -5650f);
    private static readonly Vector3 AvalancheRunout = new(4013f, -727f, -5519f);

    [Fact]
    public void NorthernWildsMapScript_OnLoad_SpawnsReviewedAvalancheFallbacks()
    {
        (NorthernWildsMapScript _, RecordingDispatchProxy<IBaseMap> mapProxy, IReadOnlyList<CreatedEntity> createdEntities) =
            CreateMapScript(spawnExistingAvalanche: false);

        IReadOnlyList<RecordingDispatchProxy<IBaseMap>.Invocation> adds =
            mapProxy.GetInvocations(nameof(IBaseMap.EnqueueAdd));
        Assert.Single(adds);
        Assert.Single(createdEntities);

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
            Assert.Equal(AvalancheSource, position.Position);
        }
    }

    [Fact]
    public void NorthernWildsMapScript_OnLoad_WhenAvalanchesAlreadyActive_DoesNotSpawnDuplicateFallbacks()
    {
        (_, RecordingDispatchProxy<IBaseMap> mapProxy, IReadOnlyList<CreatedEntity> createdEntities) =
            CreateMapScript(spawnExistingAvalanche: true);

        Assert.Empty(createdEntities);
        Assert.Empty(mapProxy.GetInvocations(nameof(IBaseMap.EnqueueAdd)));
    }

    [Fact]
    public void NorthernWildsMapScript_OnLoad_SpawnsMissingDropPodVisibilityFallbacks()
    {
        (_, RecordingDispatchProxy<IBaseMap> mapProxy, IReadOnlyList<CreatedEntity> createdEntities) =
            CreateMapScript(spawnExistingAvalanche: true, spawnExistingDropPods: false);

        IReadOnlyList<RecordingDispatchProxy<IBaseMap>.Invocation> adds =
            mapProxy.GetInvocations(nameof(IBaseMap.EnqueueAdd));
        Assert.Equal(4, adds.Count);
        Assert.Equal(4, createdEntities.Count);
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
    public void OnAddToMap_SetsAvalancheHitRangeAndWaitsForVisiblePlayer()
    {
        NorthernWildsAvalancheEntityScript script = CreateAvalancheScript(
            out RecordingDispatchProxy<ICreatureEntity> ownerProxy,
            out RecordingDispatchProxy<IMovementManager> movementProxy);

        script.OnAddToMap(CreateMap(NorthernWildsWorld));

        RecordingDispatchProxy<ICreatureEntity>.Invocation range = Assert.Single(
            ownerProxy.GetInvocations(nameof(ICreatureEntity.SetInRangeCheck)));
        Assert.Equal(6f, range.Arguments[0]);

        Assert.Empty(movementProxy.GetInvocations(nameof(IMovementManager.SetMode)));
        Assert.Empty(movementProxy.GetInvocations(nameof(IMovementManager.SetPosition)));
        Assert.Empty(movementProxy.GetInvocations(nameof(IMovementManager.LaunchSpline)));
        Assert.Empty(movementProxy.GetInvocations(nameof(IMovementManager.BroadcastNetworkEntityCommands)));
    }

    [Fact]
    public void OnAddVisibleEntity_WhenFirstPlayerSeesAvalanche_StartsMovementFromSource()
    {
        NorthernWildsAvalancheEntityScript script = CreateAvalancheScript(
            out _,
            out RecordingDispatchProxy<IMovementManager> movementProxy);

        script.OnAddToMap(CreateMap(NorthernWildsWorld));
        script.OnAddVisibleEntity(CreateVisiblePlayer(100u));

        RecordingDispatchProxy<IMovementManager>.Invocation mode = Assert.Single(
            movementProxy.GetInvocations(nameof(IMovementManager.SetMode)));
        Assert.Equal(ModeType.Walk, mode.Arguments[0]);

        RecordingDispatchProxy<IMovementManager>.Invocation reset = Assert.Single(
            movementProxy.GetInvocations(nameof(IMovementManager.SetPosition)));
        Assert.Equal(AvalancheSource, reset.Arguments[0]);
        Assert.False((bool)reset.Arguments[1]);

        RecordingDispatchProxy<IMovementManager>.Invocation movement = Assert.Single(
            movementProxy.GetInvocations(nameof(IMovementManager.LaunchSpline)));
        List<Vector3> path = Assert.IsType<List<Vector3>>(movement.Arguments[0]);
        Assert.True(path.Count >= 2);
        Assert.Equal(AvalancheSource, path[0]);
        Assert.Equal(SplineType.Linear, movement.Arguments[1]);
        Assert.Equal(SplineMode.OneShot, movement.Arguments[2]);
        Assert.Equal(9f, movement.Arguments[3]);

        Assert.Single(movementProxy.GetInvocations(nameof(IMovementManager.BroadcastNetworkEntityCommands)));
    }

    [Fact]
    public void OnAddToMap_WhenPlayerAlreadySeesAvalanche_StartsMovementFromSource()
    {
        NorthernWildsAvalancheEntityScript script = CreateAvalancheScript(
            out _,
            out RecordingDispatchProxy<IMovementManager> movementProxy);

        script.OnAddVisibleEntity(CreateVisiblePlayer(100u));
        script.OnAddToMap(CreateMap(NorthernWildsWorld));

        RecordingDispatchProxy<IMovementManager>.Invocation movement = Assert.Single(
            movementProxy.GetInvocations(nameof(IMovementManager.LaunchSpline)));
        List<Vector3> path = Assert.IsType<List<Vector3>>(movement.Arguments[0]);
        Assert.Equal(AvalancheSource, path[0]);
        Assert.Equal(SplineMode.OneShot, movement.Arguments[2]);
        Assert.Equal(9f, movement.Arguments[3]);

        Assert.Single(movementProxy.GetInvocations(nameof(IMovementManager.BroadcastNetworkEntityCommands)));
    }

    [Fact]
    public void OnAddToMap_WhenImportedAvalancheIsRunoutRow_RemovesStaticBottomEntity()
    {
        NorthernWildsAvalancheEntityScript script = CreateAvalancheScript(
            out RecordingDispatchProxy<ICreatureEntity> ownerProxy,
            out RecordingDispatchProxy<IMovementManager> movementProxy,
            position: AvalancheRunout);

        script.OnAddToMap(CreateMap(NorthernWildsWorld));

        Assert.Single(ownerProxy.GetInvocations(nameof(ICreatureEntity.SetInRangeCheck)));
        Assert.Single(ownerProxy.GetInvocations(nameof(ICreatureEntity.RemoveFromMap)));
        Assert.Empty(movementProxy.GetInvocations(nameof(IMovementManager.LaunchSpline)));
    }

    [Fact]
    public void OnPositionEntityCommandFinalise_QueuesDownhillAvalancheRepeatFromStart()
    {
        NorthernWildsAvalancheEntityScript script = CreateAvalancheScript(
            out _,
            out RecordingDispatchProxy<IMovementManager> movementProxy);
        script.OnAddToMap(CreateMap(NorthernWildsWorld));
        script.OnAddVisibleEntity(CreateVisiblePlayer(100u));

        IPositionCommand command = RecordingDispatchProxy<IPositionCommand>.Create(out RecordingDispatchProxy<IPositionCommand> commandProxy);
        commandProxy.SetMethodReturn(nameof(IPositionCommand.GetPosition), new Vector3(4020f, -728f, -5518f));

        script.OnPositionEntityCommandFinalise(command);

        Assert.Single(movementProxy.GetInvocations(nameof(IMovementManager.LaunchSpline)));
        Assert.Single(movementProxy.GetInvocations(nameof(IMovementManager.BroadcastNetworkEntityCommands)));

        script.Update(0.1d);

        IReadOnlyList<RecordingDispatchProxy<IMovementManager>.Invocation> launches =
            movementProxy.GetInvocations(nameof(IMovementManager.LaunchSpline));
        Assert.Equal(2, launches.Count);
        List<Vector3> path = Assert.IsType<List<Vector3>>(launches[1].Arguments[0]);
        Assert.Equal(AvalancheSource, path[0]);
        Assert.Equal(new Vector3(4020f, -728f, -5518f), path[^1]);
        Assert.Equal(SplineType.Linear, launches[1].Arguments[1]);
        Assert.Equal(SplineMode.OneShot, launches[1].Arguments[2]);
        Assert.Equal(9f, launches[1].Arguments[3]);
        Assert.Equal(2, movementProxy.GetInvocations(nameof(IMovementManager.BroadcastNetworkEntityCommands)).Count);

        RecordingDispatchProxy<IMovementManager>.Invocation reset = Assert.Single(
            movementProxy.GetInvocations(nameof(IMovementManager.SetPosition)).Skip(1));
        Assert.Equal(AvalancheSource, reset.Arguments[0]);
        Assert.False((bool)reset.Arguments[1]);
    }

    [Fact]
    public void OnPositionEntityCommandFinalise_WhenNoPlayersCanSeeAvalanche_DoesNotRestartAtRunout()
    {
        NorthernWildsAvalancheEntityScript script = CreateAvalancheScript(
            out _,
            out RecordingDispatchProxy<IMovementManager> movementProxy);
        script.OnAddToMap(CreateMap(NorthernWildsWorld));

        IPositionCommand command = RecordingDispatchProxy<IPositionCommand>.Create(out _);
        script.OnPositionEntityCommandFinalise(command);
        script.Update(0.1d);

        Assert.Empty(movementProxy.GetInvocations(nameof(IMovementManager.LaunchSpline)));
        Assert.Empty(movementProxy.GetInvocations(nameof(IMovementManager.BroadcastNetworkEntityCommands)));
    }

    [Fact]
    public void OnEnterRange_WhenPlayerIsHit_DamagesAndGrantsSnowSmash()
    {
        NorthernWildsAvalancheEntityScript script = CreateAvalancheScript(out _);
        IPlayer player = CreatePlayer(
            health: 1000u,
            shield: 50u,
            alreadyCompletedAchievement: false,
            out RecordingDispatchProxy<IPlayer> playerProxy,
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementProxy,
            out RecordingDispatchProxy<IMovementManager> movementProxy);

        script.OnEnterRange(player);

        RecordingDispatchProxy<IPlayer>.Invocation shieldUpdate = Assert.Single(
            playerProxy.GetInvocations("set_" + nameof(IPlayer.Shield)));
        Assert.Equal(0u, shieldUpdate.Arguments[0]);

        RecordingDispatchProxy<IPlayer>.Invocation damage = Assert.Single(
            playerProxy.GetInvocations(nameof(IPlayer.ModifyHealth)));
        Assert.Equal(200u, damage.Arguments[0]);
        Assert.Equal(DamageType.Physical, damage.Arguments[1]);
        Assert.IsAssignableFrom<ICreatureEntity>(damage.Arguments[2]);

        RecordingDispatchProxy<IPlayer>.Invocation knockdown = Assert.Single(
            playerProxy.GetInvocations(nameof(IPlayer.AddCCState)));
        Assert.Equal(CCState.Knockdown, knockdown.Arguments[0]);
        Assert.Equal(AvalancheKnockdownEffect, knockdown.Arguments[1]);
        Assert.Equal(AvalancheCreature, knockdown.Arguments[2]);
        Assert.Equal(AvalancheCreature, knockdown.Arguments[3]);

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

        RecordingDispatchProxy<IPlayer>.Invocation ccMessage = Assert.Single(
            playerProxy.GetInvocations(nameof(IPlayer.EnqueueToVisible)));
        ServerEntityCCStateSet ccStateSet = Assert.IsType<ServerEntityCCStateSet>(ccMessage.Arguments[0]);
        Assert.Equal(100u, ccStateSet.UnitId);
        Assert.Equal(CCState.Knockdown, ccStateSet.CCType);
        Assert.Equal(AvalancheKnockdownEffect, ccStateSet.SpellEffectUniqueId);
        Assert.True((bool)ccMessage.Arguments[1]);

        RecordingDispatchProxy<ICharacterAchievementManager>.Invocation grant = Assert.Single(
            achievementProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
        Assert.Equal(SnowSmashAchievement, grant.Arguments[0]);
    }

    [Fact]
    public void OnEnterRange_WhenAchievementAlreadyComplete_DamagesWithoutGrantingAgain()
    {
        NorthernWildsAvalancheEntityScript script = CreateAvalancheScript(out _);
        IPlayer player = CreatePlayer(
            health: 1000u,
            shield: 0u,
            alreadyCompletedAchievement: true,
            out RecordingDispatchProxy<IPlayer> playerProxy,
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementProxy);

        script.OnEnterRange(player);

        Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.ModifyHealth)));
        Assert.Empty(achievementProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
    }

    [Fact]
    public void Update_WhenAvalancheKnockdownExpires_RemovesOwnKnockdownState()
    {
        NorthernWildsAvalancheEntityScript script = CreateAvalancheScript(out _);
        IPlayer player = CreatePlayer(
            health: 1000u,
            shield: 0u,
            alreadyCompletedAchievement: false,
            out RecordingDispatchProxy<IPlayer> playerProxy,
            out _);
        playerProxy.SetMethodReturn(nameof(IPlayer.RemoveCCState), true);

        script.OnEnterRange(player);
        script.Update(2.1d);

        RecordingDispatchProxy<IPlayer>.Invocation removeState = Assert.Single(
            playerProxy.GetInvocations(nameof(IPlayer.RemoveCCState)));
        Assert.Equal(CCState.Knockdown, removeState.Arguments[0]);
        Assert.Equal(AvalancheKnockdownEffect, removeState.Arguments[1]);

        IReadOnlyList<RecordingDispatchProxy<IPlayer>.Invocation> messages =
            playerProxy.GetInvocations(nameof(IPlayer.EnqueueToVisible));
        Assert.Equal(2, messages.Count);
        ServerEntityCCStateRemove ccStateRemove = Assert.IsType<ServerEntityCCStateRemove>(messages[1].Arguments[0]);
        Assert.Equal(100u, ccStateRemove.UnitId);
        Assert.Equal(CCState.Knockdown, ccStateRemove.CCType);
        Assert.Equal(AvalancheCreature, ccStateRemove.SpellCastUniqueId);
        Assert.Equal(AvalancheKnockdownEffect, ccStateRemove.SpellEffectUniqueId);
        Assert.True(ccStateRemove.Removed);
    }

    [Fact]
    public void OnEnterRange_WhenCalledTwiceWithinCooldown_HitsOnlyOnce()
    {
        NorthernWildsAvalancheEntityScript script = CreateAvalancheScript(out _);
        IPlayer player = CreatePlayer(
            health: 1000u,
            shield: 0u,
            alreadyCompletedAchievement: false,
            out RecordingDispatchProxy<IPlayer> playerProxy,
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementProxy);

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

    private static NorthernWildsAvalancheEntityScript CreateAvalancheScript(
        out RecordingDispatchProxy<ICreatureEntity> ownerProxy,
        uint worldId = NorthernWildsWorld)
    {
        return CreateAvalancheScript(out ownerProxy, out _, worldId);
    }

    private static NorthernWildsAvalancheEntityScript CreateAvalancheScript(
        out RecordingDispatchProxy<ICreatureEntity> ownerProxy,
        out RecordingDispatchProxy<IMovementManager> movementProxy,
        uint worldId = NorthernWildsWorld,
        Vector3? position = null)
    {
        IBaseMap map = CreateMap(worldId);
        IMovementManager movementManager = RecordingDispatchProxy<IMovementManager>.Create(out movementProxy);

        ICreatureEntity owner = RecordingDispatchProxy<ICreatureEntity>.Create(out ownerProxy);
        ownerProxy.SetProperty(nameof(ICreatureEntity.Map), map);
        ownerProxy.SetProperty(nameof(ICreatureEntity.Position), position ?? AvalancheNearSource);
        ownerProxy.SetProperty(nameof(ICreatureEntity.MovementManager), movementManager);
        ownerProxy.SetMethodReturn(nameof(ICreatureEntity.GetInRange), Array.Empty<IPlayer>());

        var script = new NorthernWildsAvalancheEntityScript();
        script.OnLoad(owner);
        return script;
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
        out RecordingDispatchProxy<IMovementManager> movementProxy)
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

        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), 42ul);
        playerProxy.SetProperty(nameof(IPlayer.Guid), 100u);
        playerProxy.SetProperty(nameof(IPlayer.IsAlive), true);
        playerProxy.SetProperty(nameof(IPlayer.Health), health);
        playerProxy.SetProperty(nameof(IPlayer.Shield), shield);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievementManager);
        playerProxy.SetProperty(nameof(IPlayer.MovementManager), movementManager);
        return player;
    }

    private static IPlayer CreateVisiblePlayer(uint guid)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Guid), guid);
        return player;
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

    private sealed record CreatedEntity(INonPlayerEntity Instance, RecordingDispatchProxy<INonPlayerEntity> Proxy);
}
