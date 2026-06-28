using System.Numerics;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Abstract.Group;
using NexusForever.Game.Abstract.Loot;
using NexusForever.Game.Entity;
using NexusForever.Game.Loot;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Loot;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.Message;
using NexusForever.Network.Session;
using NexusForever.Network.World.Entity;
using NexusForever.Network.World.Entity.Model;
using NexusForever.Network.World.Message.Model.Entity;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Loot;

namespace NexusForever.Game.Tests.Entity;

public class CreatureRespawnPacketTests
{
    [Fact]
    public void Respawn_RemovesLootUsingInjectedLootManager()
    {
        const uint creatureGuid = 9090u;

        IGroupStateManager groupStateManager = RecordingDispatchProxy<IGroupStateManager>.Create(out _);
        var lootManager = new GlobalLootManager(groupStateManager);
        var lootInstance = new LootInstance(
            ownerUnitId: creatureGuid,
            looterIds: [],
            looterType: LooterType.Player,
            lootEntityType: LootEntityType.Creature);
        AddLootInstance(lootManager, lootInstance);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddGameEntity();
        services.AddTransient<INonPlayerEntity, TestNonPlayerEntity>();
        services.AddSingleton(lootManager);
        services.AddSingleton<IGlobalLootManager>(lootManager);
        using ServiceProvider provider = services.BuildServiceProvider();

        IEntityFactory entityFactory = provider.GetRequiredService<IEntityFactory>();
        var creature = Assert.IsType<TestNonPlayerEntity>(entityFactory.CreateWorldEntity(EntityType.NonPlayer));
        creature.SetGuidForTest(creatureGuid);
        creature.SetDeadForTest();

        InvokeRespawn(creature);

        Assert.Equal(0, GetLootInstanceCount(lootManager));
        Assert.Equal(0, GetOwnerIndexCount(lootManager, creatureGuid));
    }

    [Fact]
    public void Respawn_RecreatesVisibleNonPlayerAndClearsLootPresentation()
    {
        TestNonPlayerEntity creature = CreateDeadCreature(77u);
        IPlayer player = CreatePlayer(21u, out RecordingDispatchProxy<IGameSession> sessionProxy);
        creature.AddVisibleForTest(player);

        InvokeRespawn(creature);

        List<IWritable> messages = sessionProxy
            .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(i => Assert.IsAssignableFrom<IWritable>(i.Arguments[0]))
            .ToList();

        Assert.Contains(messages, message => message is ServerLootRemove remove
            && remove.OwnerUnitId == creature.Guid);
        Assert.Contains(messages, message => message is ServerEntityDeathState deathState
            && deathState.UnitId == creature.Guid
            && !deathState.Dead
            && deathState.RezHealth == creature.Health);

        int destroyIndex = messages.FindIndex(message => message is ServerEntityDestroy destroy
            && destroy.Guid == creature.Guid
            && destroy.Flag);
        int createIndex = messages.FindIndex(message => message is ServerEntityCreate create
            && create.Guid == creature.Guid
            && create.Type == EntityType.NonPlayer);

        Assert.True(destroyIndex >= 0);
        Assert.True(createIndex > destroyIndex);
        Assert.DoesNotContain(messages, message => message is ServerEntityCreateAuxScalarList);
        Assert.True(creature.IsAlive);
        Assert.Equal(100u, creature.Health);
        Assert.Equal(25u, creature.RespawnShieldForTest);
    }

    [Fact]
    public void Respawn_RestoresNonPlayerPositionCommandToLeashPosition()
    {
        Vector3 leashPosition = new(12f, 3f, -4f);
        TestNonPlayerEntity creature = CreateDeadCreature(88u);
        creature.SetLeashPositionForTest(leashPosition);

        InvokeRespawn(creature);

        RecordingDispatchProxy<IMovementManager>.Invocation setPosition = Assert.Single(
            creature.MovementProxy.GetInvocations(nameof(IMovementManager.SetPosition)));
        Assert.Equal(leashPosition, setPosition.Arguments[0]);
        Assert.Equal(false, setPosition.Arguments[1]);
    }

    [Fact]
    public void Respawn_RestoresNonPlayerRotationCommandToLeashRotation()
    {
        Vector3 leashRotation = new(1.25f, 0f, 0f);
        TestNonPlayerEntity creature = CreateDeadCreature(89u);
        creature.SetLeashRotationForTest(leashRotation);

        InvokeRespawn(creature);

        RecordingDispatchProxy<IMovementManager>.Invocation setRotation = Assert.Single(
            creature.MovementProxy.GetInvocations(nameof(IMovementManager.SetRotation)));
        Assert.Equal(leashRotation, setRotation.Arguments[0]);
        Assert.Equal(false, setRotation.Arguments[1]);
    }

    private static TestNonPlayerEntity CreateDeadCreature(uint guid)
    {
        var creature = new TestNonPlayerEntity();
        creature.SetGuidForTest(guid);
        creature.SetDeadForTest();
        return creature;
    }

    private static IPlayer CreatePlayer(uint guid, out RecordingDispatchProxy<IGameSession> sessionProxy)
    {
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out sessionProxy);
        return TestPlayerBuilder.Create()
            .WithGuid(guid)
            .WithSession(session)
            .Build();
    }

    private static void InvokeRespawn(UnitEntity creature)
    {
        typeof(UnitEntity)
            .GetMethod("Respawn", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(creature, null);
    }

    private static void AddLootInstance(GlobalLootManager manager, LootInstance lootInstance)
    {
        typeof(GlobalLootManager)
            .GetMethod("AddLootInstance", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(manager, [lootInstance]);
    }

    private static int GetLootInstanceCount(GlobalLootManager manager)
    {
        var instances = (List<LootInstance>)typeof(GlobalLootManager)
            .GetField("lootInstances", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(manager)!;
        return instances.Count;
    }

    private static int GetOwnerIndexCount(GlobalLootManager manager, uint ownerUnitId)
    {
        var instancesByOwnerUnit = (Dictionary<uint, List<LootInstance>>)typeof(GlobalLootManager)
            .GetField("lootInstancesByOwnerUnit", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(manager)!;
        return instancesByOwnerUnit.TryGetValue(ownerUnitId, out List<LootInstance> instances)
            ? instances.Count
            : 0;
    }

    private sealed class TestNonPlayerEntity : UnitEntity, INonPlayerEntity
    {
        public override EntityType Type => EntityType.NonPlayer;
        public override uint Health { get; protected set; }
        public IVendorInfo VendorInfo => null;
        public uint RespawnShieldForTest { get; private set; }
        public RecordingDispatchProxy<IMovementManager> MovementProxy { get; }

        public TestNonPlayerEntity()
            : this(CreateMovementManager(out RecordingDispatchProxy<IMovementManager> movementProxy), movementProxy)
        {
        }

        private TestNonPlayerEntity(IMovementManager movementManager, RecordingDispatchProxy<IMovementManager> movementProxy)
            : base(movementManager)
        {
            MovementProxy = movementProxy;
        }

        private static IMovementManager CreateMovementManager(out RecordingDispatchProxy<IMovementManager> movementProxy)
        {
            return RecordingDispatchProxy<IMovementManager>.Create(out movementProxy);
        }

        public void SetGuidForTest(uint guid)
        {
            Guid = guid;
        }

        public void SetDeadForTest()
        {
            Health = 0u;
            DeathState = EntityDeathState.Dead;
        }

        public void SetLeashPositionForTest(Vector3 position)
        {
            LeashPosition = position;
        }

        public void SetLeashRotationForTest(Vector3 rotation)
        {
            LeashRotation = rotation;
        }

        public void AddVisibleForTest(IGridEntity entity)
        {
            visibleEntities.Add(entity.Guid, entity);
        }

        public override IReadOnlyList<IWritable> BuildEntityCreateAuxPackets()
        {
            return [new ServerEntityCreateAuxScalarList()];
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

        protected override IEntityModel BuildEntityModel()
        {
            return new NonPlayerEntityModel();
        }

        protected override void ResetVitalsForRespawn()
        {
            Health = 100u;
            RespawnShieldForTest = 25u;
        }

        protected override float CalculateDefaultProperty(Property property)
        {
            return 0f;
        }
    }
}
