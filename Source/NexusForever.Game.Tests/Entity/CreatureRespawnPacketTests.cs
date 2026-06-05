using System.Reflection;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.Message;
using NexusForever.Network.Session;
using NexusForever.Network.World.Entity;
using NexusForever.Network.World.Entity.Model;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Loot;

namespace NexusForever.Game.Tests.Entity;

public class CreatureRespawnPacketTests
{
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
        Assert.True(creature.IsAlive);
        Assert.Equal(100u, creature.Health);
        Assert.Equal(25u, creature.RespawnShieldForTest);
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

    private sealed class TestNonPlayerEntity : UnitEntity, INonPlayerEntity
    {
        public override EntityType Type => EntityType.NonPlayer;
        public override uint Health { get; protected set; }
        public IVendorInfo VendorInfo => null;
        public uint RespawnShieldForTest { get; private set; }

        public TestNonPlayerEntity()
            : base(RecordingDispatchProxy<IMovementManager>.Create(out _))
        {
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

        public void AddVisibleForTest(IGridEntity entity)
        {
            visibleEntities.Add(entity.Guid, entity);
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
