using System.Numerics;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Abstract.Group;
using NexusForever.Game.Abstract.Matching.Match;
using NexusForever.Game.Abstract.Matching.Queue;
using NexusForever.Game.Configuration.Model;
using NexusForever.Game.Entity;
using NexusForever.Game.Loot;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.Internal;
using NexusForever.Network.Message;
using NexusForever.Network.Session;
using NexusForever.Network.World.Entity;
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

    private static TestPlayer CreatePlayer(out RecordingDispatchProxy<IGameSession> sessionProxy)
    {
        IMovementManager movementManager = RecordingDispatchProxy<IMovementManager>.Create(out _);
        IInternalMessagePublisher messagePublisher = RecordingDispatchProxy<IInternalMessagePublisher>.Create(out _);
        IEntityFactory entityFactory = RecordingDispatchProxy<IEntityFactory>.Create(out _);
        IMatchingManager matchingManager = RecordingDispatchProxy<IMatchingManager>.Create(out _);
        IMatchManager matchManager = RecordingDispatchProxy<IMatchManager>.Create(out _);
        ICurrencyManager currencyManager = RecordingDispatchProxy<ICurrencyManager>.Create(out _);
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out sessionProxy);

        var player = new TestPlayer(movementManager, messagePublisher, entityFactory, matchingManager, matchManager, currencyManager)
        {
            VisibilityFilter = null
        };

        player.SetGuidForTest(21u);
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

    private static void SetAutoProperty(object instance, string propertyName, object value)
    {
        FieldInfo backingField = instance.GetType().GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? instance.GetType().BaseType?.GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);

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
            ICurrencyManager currencyManager)
            : base(movementManager, messagePublisher, entityFactory, matchingManager, matchManager, currencyManager)
        {
        }

        public Predicate<IGridEntity> VisibilityFilter { get; set; }

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
