using System.Numerics;
using System.Reflection;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Matching.Match;
using NexusForever.Game.Abstract.Matching.Queue;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Entity.Movement.Command;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.Internal;
using NexusForever.Network.Message;
using NexusForever.Network.Session;
using NexusForever.Network.World.Entity;
using NexusForever.Network.World.Entity.Command;
using NexusForever.Network.World.Entity.Model;
using NexusForever.Network.World.Message.Model.Entity;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Tests.Entity;

public class PlayerVisibilityPacketTests
{
    [Fact]
    public void AddVisible_WhenBaseRejectsEntity_DoesNotEmitCreatePacket()
    {
        TestPlayer player = CreatePlayer(out RecordingDispatchProxy<IGameSession> sessionProxy);
        TestWorldEntity entity = CreateWorldEntity(55u);

        player.VisibilityFilter = _ => false;

        player.AddVisible(entity);

        Assert.Null(player.GetVisible<IGridEntity>(entity.Guid));
        Assert.Empty(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
    }

    [Fact]
    public void AddVisible_WhenEntityBecomesVisible_EmitsSingleCreatePacket()
    {
        TestPlayer player = CreatePlayer(out RecordingDispatchProxy<IGameSession> sessionProxy);
        TestWorldEntity entity = CreateWorldEntity(55u);

        player.AddVisible(entity);

        Assert.Same(entity, player.GetVisible<IGridEntity>(entity.Guid));

        RecordingDispatchProxy<IGameSession>.Invocation invocation = Assert.Single(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
        Assert.IsType<ServerEntityCreate>(invocation.Arguments[0]);
    }

    [Fact]
    public void AddVisible_WhenEntityHasCreateAuxPackets_EmitsAuxPacketsBeforeCreate()
    {
        TestPlayer player = CreatePlayer(out RecordingDispatchProxy<IGameSession> sessionProxy);
        TestWorldEntity entity = CreateWorldEntity(55u);
        entity.EntityCreateAuxPackets.Add(new ServerEntityCreateAuxScalarList());

        player.AddVisible(entity);

        IReadOnlyList<object> messages = sessionProxy
            .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(i => i.Arguments[0])
            .ToList();

        Assert.IsType<ServerEntityCreateAuxScalarList>(messages[0]);
        ServerEntityCreate create = Assert.IsType<ServerEntityCreate>(messages[1]);
        Assert.Equal(entity.Guid, create.Guid);
    }

    [Fact]
    public void AddVisible_WhenSettingUpCampAchievedForLandingSiteDeadeye_UsesNeutralPresentationCreature()
    {
        TestPlayer player = CreatePlayer(out RecordingDispatchProxy<IGameSession> sessionProxy);
        SetMap(player, CreateMap(426u));
        SetQuestState(player, _ => QuestState.Achieved);
        TestWorldEntity entity = CreateNonPlayerEntity(55u, 11063u);

        player.AddVisible(entity);

        ServerEntityCreate create = Assert.IsType<ServerEntityCreate>(
            Assert.Single(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))).Arguments[0]);
        NonPlayerEntityModel model = Assert.IsType<NonPlayerEntityModel>(create.EntityModel);
        Assert.Equal(16962u, model.CreatureId);
        Assert.Equal(11063u, entity.CreatureId);
    }

    [Fact]
    public void AddVisible_WhenSettingUpCampAchievedForCampDeadeye_KeepsCampPresentationCreature()
    {
        TestPlayer player = CreatePlayer(out RecordingDispatchProxy<IGameSession> sessionProxy);
        SetMap(player, CreateMap(426u));
        SetQuestState(player, _ => QuestState.Achieved);
        TestWorldEntity entity = CreateNonPlayerEntity(55u, 12959u);

        player.AddVisible(entity);

        ServerEntityCreate create = Assert.IsType<ServerEntityCreate>(
            Assert.Single(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))).Arguments[0]);
        NonPlayerEntityModel model = Assert.IsType<NonPlayerEntityModel>(create.EntityModel);
        Assert.Equal(12959u, model.CreatureId);
        Assert.Equal(12959u, entity.CreatureId);
    }

    [Fact]
    public void RefreshQuestPresentation_WhenSettingUpCampBecomesAchieved_RecreatesLandingSiteDeadeyeWithNeutralPresentation()
    {
        QuestState? state = null;
        TestPlayer player = CreatePlayer(out RecordingDispatchProxy<IGameSession> sessionProxy);
        SetMap(player, CreateMap(426u));
        SetQuestState(player, _ => state);
        TestWorldEntity entity = CreateNonPlayerEntity(55u, 11063u);

        player.AddVisible(entity);

        ServerEntityCreate initialCreate = Assert.IsType<ServerEntityCreate>(
            Assert.Single(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))).Arguments[0]);
        Assert.Equal(11063u, Assert.IsType<NonPlayerEntityModel>(initialCreate.EntityModel).CreatureId);

        state = QuestState.Achieved;
        sessionProxy.Invocations.Clear();
        player.RefreshQuestPresentation(3671);

        IReadOnlyList<RecordingDispatchProxy<IGameSession>.Invocation> invocations =
            sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted));

        ServerEntityDestroy destroy = Assert.IsType<ServerEntityDestroy>(invocations[0].Arguments[0]);
        Assert.Equal(entity.Guid, destroy.Guid);
        Assert.True(destroy.Flag);

        ServerEntityCreate refreshedCreate = Assert.IsType<ServerEntityCreate>(invocations[1].Arguments[0]);
        Assert.Equal(16962u, Assert.IsType<NonPlayerEntityModel>(refreshedCreate.EntityModel).CreatureId);
        Assert.Equal(11063u, entity.CreatureId);
    }

    [Fact]
    public void AddVisible_WhenRemotePlayerMissingOwner_AddsReciprocalVisibility()
    {
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out _);
        TestPlayer player = CreatePlayer(out _, 21u);
        TestPlayer remotePlayer = CreatePlayer(out RecordingDispatchProxy<IGameSession> remoteSessionProxy, 77u);
        SetMap(player, map);
        SetMap(remotePlayer, map);

        player.AddVisible(remotePlayer);

        Assert.Same(remotePlayer, player.GetVisible<IGridEntity>(remotePlayer.Guid));
        Assert.Same(player, remotePlayer.GetVisible<IGridEntity>(player.Guid));
        Assert.Contains(remoteSessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)),
            i => i.Arguments[0] is ServerEntityCreate create && create.Guid == player.Guid);
        Assert.DoesNotContain(remoteSessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)),
            i => i.Arguments[0] is ServerEntityDestroy);
    }

    [Fact]
    public void AddVisible_WhenRemotePlayerCreateHasStalePosition_ReplacesWithCurrentMapPosition()
    {
        TestPlayer player = CreatePlayer(out RecordingDispatchProxy<IGameSession> sessionProxy, 21u);
        TestPlayer remotePlayer = CreatePlayer(out _, 77u);
        remotePlayer.SetPositionForTest(new Vector3(12f, 3f, 4f));
        remotePlayer.CreateCommands.Add(new NetworkEntityCommand
        {
            Command = EntityCommand.SetPosition,
            Model   = new SetPositionCommand
            {
                Position = new Vector3(4370f, 0f, 0f),
                Blend    = true
            }
        });

        player.AddVisible(remotePlayer);

        ServerEntityCreate create = sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(i => i.Arguments[0])
            .OfType<ServerEntityCreate>()
            .Single();
        AssertPositionSnapshot(create, remotePlayer.Position);
    }

    [Fact]
    public void AddVisible_WhenRemotePlayerAlreadyTracksOwner_RefreshesRemotePlayerCreate()
    {
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out _);
        TestPlayer player = CreatePlayer(out _, 21u);
        TestPlayer remotePlayer = CreatePlayer(out RecordingDispatchProxy<IGameSession> remoteSessionProxy, 77u);
        player.SetPositionForTest(new Vector3(8f, 1f, 2f));
        SetMap(player, map);
        SetMap(remotePlayer, map);
        SetVisibleEntity(remotePlayer, player);

        player.AddVisible(remotePlayer);

        IReadOnlyList<RecordingDispatchProxy<IGameSession>.Invocation> remoteMessages =
            remoteSessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted));

        ServerEntityDestroy destroy = Assert.IsType<ServerEntityDestroy>(remoteMessages[0].Arguments[0]);
        Assert.Equal(player.Guid, destroy.Guid);
        Assert.True(destroy.Flag);

        Assert.IsType<ServerEntityCreateAuxScalarList>(remoteMessages[1].Arguments[0]);

        ServerEntityCreate create = Assert.IsType<ServerEntityCreate>(remoteMessages[2].Arguments[0]);
        Assert.Equal(player.Guid, create.Guid);
        AssertPositionSnapshot(create, player.Position);
    }

    [Fact]
    public void RemoveVisible_WhenEntityIsNotTracked_DoesNotEmitDestroyPacket()
    {
        TestPlayer player = CreatePlayer(out RecordingDispatchProxy<IGameSession> sessionProxy);
        TestWorldEntity entity = CreateWorldEntity(55u);

        player.RemoveVisible(entity);

        Assert.Empty(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
    }

    [Fact]
    public void RemoveVisible_WhenEntityIsTracked_EmitsSingleDestroyPacket()
    {
        TestPlayer player = CreatePlayer(out RecordingDispatchProxy<IGameSession> sessionProxy);
        TestWorldEntity entity = CreateWorldEntity(55u);

        player.AddVisible(entity);
        sessionProxy.Invocations.Clear();

        player.RemoveVisible(entity);

        Assert.Null(player.GetVisible<IGridEntity>(entity.Guid));

        RecordingDispatchProxy<IGameSession>.Invocation invocation = Assert.Single(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
        ServerEntityDestroy destroy = Assert.IsType<ServerEntityDestroy>(invocation.Arguments[0]);
        Assert.Equal(entity.Guid, destroy.Guid);
    }

    [Fact]
    public void Dismount_WhenPlatformIsNotVehicle_DetachesPlatformWithoutThrowing()
    {
        TestPlayer player = CreatePlayer(out _, trackMovementState: true);
        TestWorldEntity platform = CreateWorldEntity(77u);
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out RecordingDispatchProxy<IBaseMap> mapProxy);
        mapProxy.SetMethodHandler(nameof(IBaseMap.GetEntity), args =>
        {
            uint guid = (uint)args[0];
            return guid == platform.Guid ? platform : null;
        });

        SetMap(player, map);
        SetVisibleEntity(player, platform);
        player.SetPlatform(platform);

        Assert.Equal(platform.Guid, player.PlatformGuid);

        player.Dismount();

        Assert.Null(player.PlatformGuid);
    }

    private static void AssertPositionSnapshot(ServerEntityCreate create, Vector3 expectedPosition)
    {
        INetworkEntityCommand positionCommand = Assert.Single(create.Commands, c => c.Command == EntityCommand.SetPosition);
        SetPositionCommand model = Assert.IsType<SetPositionCommand>(positionCommand.Model);
        Assert.Equal(expectedPosition, model.Position);
        Assert.False(model.Blend);
    }

    private static TestPlayer CreatePlayer(out RecordingDispatchProxy<IGameSession> sessionProxy, uint guid = 21u, bool trackMovementState = false)
    {
        IMovementManager movementManager = RecordingDispatchProxy<IMovementManager>.Create(out RecordingDispatchProxy<IMovementManager> movementManagerProxy);
        if (trackMovementState)
            TrackMovementState(movementManagerProxy);
        IInternalMessagePublisher messagePublisher = RecordingDispatchProxy<IInternalMessagePublisher>.Create(out _);
        IEntityFactory entityFactory = RecordingDispatchProxy<IEntityFactory>.Create(out _);
        IMatchingManager matchingManager = RecordingDispatchProxy<IMatchingManager>.Create(out _);
        IMatchManager matchManager = RecordingDispatchProxy<IMatchManager>.Create(out _);
        IGameTableManager gameTableManager = RecordingDispatchProxy<IGameTableManager>.Create(out _);
        ICurrencyManager currencyManager = RecordingDispatchProxy<ICurrencyManager>.Create(out _);
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out sessionProxy);

        var player = new TestPlayer(movementManager, messagePublisher, entityFactory, matchingManager, matchManager, gameTableManager, currencyManager)
        {
            VisibilityFilter = null
        };

        player.SetGuidForTest(guid);
        player.SetPositionForTest(Vector3.Zero);
        SetAutoProperty(player, nameof(Player.Session), session);
        SetAutoProperty(player, nameof(Player.Identity), new NexusForever.Game.Abstract.Identity { Id = 42ul, RealmId = (ushort)1 });

        return player;
    }

    private static void TrackMovementState(RecordingDispatchProxy<IMovementManager> movementManagerProxy)
    {
        uint? platform = null;
        Vector3 position = Vector3.Zero;
        Vector3 rotation = Vector3.Zero;

        movementManagerProxy.SetMethodHandler(nameof(IMovementManager.GetPlatform), _ => platform);
        movementManagerProxy.SetMethodHandler(nameof(IMovementManager.SetPlatform), args =>
        {
            platform = (uint?)args[0];
            return null;
        });
        movementManagerProxy.SetMethodHandler(nameof(IMovementManager.GetPosition), _ => position);
        movementManagerProxy.SetMethodHandler(nameof(IMovementManager.SetPosition), args =>
        {
            position = (Vector3)args[0];
            return null;
        });
        movementManagerProxy.SetMethodHandler(nameof(IMovementManager.GetRotation), _ => rotation);
        movementManagerProxy.SetMethodHandler(nameof(IMovementManager.SetRotation), args =>
        {
            rotation = (Vector3)args[0];
            return null;
        });
    }

    private static TestWorldEntity CreateWorldEntity(uint guid)
    {
        IMovementManager movementManager = RecordingDispatchProxy<IMovementManager>.Create(out _);
        var entity = new TestWorldEntity(movementManager);
        entity.SetGuidForTest(guid);
        entity.SetPositionForTest(new Vector3(1f, 0f, 0f));
        return entity;
    }

    private static TestWorldEntity CreateNonPlayerEntity(uint guid, uint creatureId)
    {
        IMovementManager movementManager = RecordingDispatchProxy<IMovementManager>.Create(out _);
        var entity = new TestWorldEntity(
            movementManager,
            EntityType.NonPlayer,
            () => new NonPlayerEntityModel
            {
                CreatureId = creatureId
            });

        entity.SetGuidForTest(guid);
        entity.SetPositionForTest(new Vector3(1f, 0f, 0f));
        SetAutoProperty(entity, nameof(WorldEntity.CreatureEntry), new Creature2Entry { Id = creatureId });
        return entity;
    }

    private static IBaseMap CreateMap(uint worldId)
    {
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out RecordingDispatchProxy<IBaseMap> mapProxy);
        mapProxy.SetProperty(nameof(IBaseMap.Entry), new WorldEntry { Id = worldId });
        return map;
    }

    private static void SetMap(TestPlayer player, IBaseMap map)
    {
        SetAutoProperty(player, nameof(GridEntity.Map), map);
    }

    private static void SetQuestState(TestPlayer player, Func<ushort, QuestState?> getState)
    {
        IQuestManager questManager = RecordingDispatchProxy<IQuestManager>.Create(out RecordingDispatchProxy<IQuestManager> questManagerProxy);
        questManagerProxy.SetMethodHandler(nameof(IQuestManager.GetQuestState), args => getState((ushort)args[0]));
        SetAutoProperty(player, nameof(Player.QuestManager), questManager);
    }

    private static void SetVisibleEntity(TestPlayer player, IGridEntity visibleEntity)
    {
        FieldInfo field = typeof(GridEntity).GetField("visibleEntities", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        field.SetValue(player, new Dictionary<uint, IGridEntity>
        {
            [visibleEntity.Guid] = visibleEntity
        });
    }

    private static void SetAutoProperty(object instance, string propertyName, object value)
    {
        FieldInfo backingField = null;
        for (Type type = instance.GetType(); type != null; type = type.BaseType)
        {
            backingField = type.GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
            if (backingField != null)
                break;
        }

        Assert.NotNull(backingField);
        backingField.SetValue(instance, value);
    }

    private sealed class TestPlayer : Player
    {
        public TestPlayer(
            IMovementManager movementManager,
            IInternalMessagePublisher messagePublisher,
            IEntityFactory entityFactory,
            IMatchingManager matchingManager,
            IMatchManager matchManager,
            IGameTableManager gameTableManager,
            ICurrencyManager currencyManager)
            : base(movementManager, messagePublisher, entityFactory, matchingManager, matchManager, gameTableManager, currencyManager)
        {
        }

        public Predicate<IGridEntity> VisibilityFilter { get; set; }
        public List<INetworkEntityCommand> CreateCommands { get; } = [];

        public override bool CanSeeEntity(IGridEntity entity)
        {
            if (VisibilityFilter != null)
                return VisibilityFilter(entity);

            return base.CanSeeEntity(entity);
        }

        public void SetGuidForTest(uint guid)
        {
            Guid = guid;
        }

        public void SetPositionForTest(Vector3 position)
        {
            Position = position;
        }

        public override IReadOnlyList<IWritable> BuildEntityCreateAuxPackets()
        {
            return [new ServerEntityCreateAuxScalarList()];
        }

        public override ServerEntityCreate BuildCreatePacket(bool isLoading)
        {
            return new ServerEntityCreate
            {
                Guid     = Guid,
                Type     = EntityType.Player,
                Commands = CreateCommands.ToList()
            };
        }
    }

    private sealed class TestWorldEntity : WorldEntity
    {
        private readonly EntityType entityType;
        private readonly Func<IEntityModel> entityModelFactory;

        public TestWorldEntity(
            IMovementManager movementManager,
            EntityType entityType = EntityType.SimpleCollidable,
            Func<IEntityModel> entityModelFactory = null)
            : base(movementManager)
        {
            this.entityType          = entityType;
            this.entityModelFactory = entityModelFactory ?? (() => new SimpleCollidableEntityModel());
        }

        public override EntityType Type => entityType;

        public List<IWritable> EntityCreateAuxPackets { get; } = [];

        protected override IEntityModel BuildEntityModel()
        {
            return entityModelFactory();
        }

        public override IReadOnlyList<IWritable> BuildEntityCreateAuxPackets()
        {
            return EntityCreateAuxPackets;
        }

        public override ServerEntityCreate BuildCreatePacket(bool isLoading)
        {
            return new ServerEntityCreate
            {
                Guid = Guid,
                Type = Type,
                EntityModel = BuildEntityModel()
            };
        }

        public void SetGuidForTest(uint guid)
        {
            Guid = guid;
        }

        public void SetPositionForTest(Vector3 position)
        {
            Position = position;
        }
    }
}
