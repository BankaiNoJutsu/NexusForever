using System.Numerics;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Abstract.Group;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Matching.Match;
using NexusForever.Game.Abstract.Matching.Queue;
using NexusForever.Game.Configuration.Model;
using NexusForever.Game.Entity;
using NexusForever.Game.Loot;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Entity.Movement.Command;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.Network.Internal;
using NexusForever.Network.Message;
using NexusForever.Network.Session;
using NexusForever.Network.World.Entity;
using NexusForever.Network.World.Entity.Command;
using NexusForever.Network.World.Entity.Model;
using NexusForever.Network.World.Message.Model;
using NexusForever.Shared;
using NexusForever.Shared.Configuration;

namespace NexusForever.Game.Tests.Entity;

[Collection(LegacyServiceProviderCollection.Name)]
public class PlayerVisibilityPacketTests
{
    [Fact]
    public void AddVisible_WhenBaseRejectsEntity_DoesNotEmitCreatePacket()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildProvider();

        try
        {
            TestPlayer player = CreatePlayer(out RecordingDispatchProxy<IGameSession> sessionProxy);
            TestWorldEntity entity = CreateWorldEntity(55u);

            player.VisibilityFilter = _ => false;

            player.AddVisible(entity);

            Assert.Null(player.GetVisible<IGridEntity>(entity.Guid));
            Assert.Empty(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void AddVisible_WhenEntityBecomesVisible_EmitsSingleCreatePacket()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildProvider();

        try
        {
            TestPlayer player = CreatePlayer(out RecordingDispatchProxy<IGameSession> sessionProxy);
            TestWorldEntity entity = CreateWorldEntity(55u);

            player.AddVisible(entity);

            Assert.Same(entity, player.GetVisible<IGridEntity>(entity.Guid));

            RecordingDispatchProxy<IGameSession>.Invocation invocation = Assert.Single(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
            Assert.IsType<ServerEntityCreate>(invocation.Arguments[0]);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void AddVisible_WhenRemotePlayerMissingOwner_AddsReciprocalVisibility()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildProvider();

        try
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
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void AddVisible_WhenRemotePlayerCreateHasStalePosition_ReplacesWithCurrentMapPosition()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildProvider();

        try
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

            ServerEntityCreate create = Assert.IsType<ServerEntityCreate>(
                sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))[0].Arguments[0]);
            AssertPositionSnapshot(create, remotePlayer.Position);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void AddVisible_WhenRemotePlayerAlreadyTracksOwner_RefreshesRemotePlayerCreate()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildProvider();

        try
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

            ServerEntityCreate create = Assert.IsType<ServerEntityCreate>(remoteMessages[1].Arguments[0]);
            Assert.Equal(player.Guid, create.Guid);
            AssertPositionSnapshot(create, player.Position);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void RemoveVisible_WhenEntityIsNotTracked_DoesNotEmitDestroyPacket()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildProvider();

        try
        {
            TestPlayer player = CreatePlayer(out RecordingDispatchProxy<IGameSession> sessionProxy);
            TestWorldEntity entity = CreateWorldEntity(55u);

            player.RemoveVisible(entity);

            Assert.Empty(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void RemoveVisible_WhenEntityIsTracked_EmitsSingleDestroyPacket()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildProvider();

        try
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
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    private static void AssertPositionSnapshot(ServerEntityCreate create, Vector3 expectedPosition)
    {
        INetworkEntityCommand positionCommand = Assert.Single(create.Commands, c => c.Command == EntityCommand.SetPosition);
        SetPositionCommand model = Assert.IsType<SetPositionCommand>(positionCommand.Model);
        Assert.Equal(expectedPosition, model.Position);
        Assert.False(model.Blend);
    }

    private static IServiceProvider BuildProvider()
    {
        IGroupStateManager groupStateManager = RecordingDispatchProxy<IGroupStateManager>.Create(out _);
        var configuration = new SharedConfiguration(new ConfigurationBuilder().Build());
        configuration.Initialise<TestConfiguration>();

        return new ServiceCollection()
            .AddSingleton(configuration)
            .AddSingleton(new GlobalLootManager(groupStateManager))
            .BuildServiceProvider();
    }

    private sealed class TestConfiguration
    {
        public WorldConfig World { get; set; }
    }

    private static TestPlayer CreatePlayer(out RecordingDispatchProxy<IGameSession> sessionProxy, uint guid = 21u)
    {
        IMovementManager movementManager = RecordingDispatchProxy<IMovementManager>.Create(out _);
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

    private static TestWorldEntity CreateWorldEntity(uint guid)
    {
        IMovementManager movementManager = RecordingDispatchProxy<IMovementManager>.Create(out _);
        var entity = new TestWorldEntity(movementManager);
        entity.SetGuidForTest(guid);
        entity.SetPositionForTest(new Vector3(1f, 0f, 0f));
        return entity;
    }

    private static void SetMap(TestPlayer player, IBaseMap map)
    {
        SetAutoProperty(player, nameof(GridEntity.Map), map);
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
            return [];
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
        public TestWorldEntity(IMovementManager movementManager)
            : base(movementManager)
        {
        }

        public override EntityType Type => EntityType.SimpleCollidable;

        protected override IEntityModel BuildEntityModel()
        {
            return new SimpleCollidableEntityModel();
        }

        public override IReadOnlyList<IWritable> BuildEntityCreateAuxPackets()
        {
            return [];
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
