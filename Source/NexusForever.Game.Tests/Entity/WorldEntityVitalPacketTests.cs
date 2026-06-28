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

namespace NexusForever.Game.Tests.Entity;

public class WorldEntityVitalPacketTests
{
    [Fact]
    public void ShieldChange_ForVisiblePlayer_SendsShieldStatThenHealthRefresh()
    {
        var unit = new TestWorldUnitEntity();
        unit.SetGuidForTest(1001u);
        unit.SetHealthForTest(maxHealth: 1000u, health: 750u);
        unit.SetShieldForTest(maxShield: 100u, shield: 100u);
        unit.AddVisibleForTest(CreatePlayer(guid: 2002u, out RecordingDispatchProxy<IGameSession> sessionProxy));

        unit.Shield = 75u;

        List<IWritable> messages = sessionProxy
            .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(invocation => Assert.IsAssignableFrom<IWritable>(invocation.Arguments[0]))
            .ToList();

        int shieldStatIndex = messages.FindIndex(message =>
            message is ServerEntityStatUpdateInteger update
            && update.UnitId == unit.Guid
            && update.Stat.Stat == Stat.Shield
            && update.Stat.Type == StatType.Integer
            && update.Stat.Value == 75f);

        int healthRefreshIndex = messages.FindIndex(message =>
            message is ServerEntityHealthUpdate update
            && update.UnitId == unit.Guid
            && update.Health == 750u);

        Assert.True(shieldStatIndex >= 0);
        Assert.True(healthRefreshIndex > shieldStatIndex);
    }

    private static IPlayer CreatePlayer(uint guid, out RecordingDispatchProxy<IGameSession> sessionProxy)
    {
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out sessionProxy);
        return TestPlayerBuilder.Create()
            .WithGuid(guid)
            .WithSession(session)
            .Build();
    }

    private sealed class TestWorldUnitEntity : UnitEntity
    {
        public override EntityType Type => EntityType.WorldUnit;

        public TestWorldUnitEntity()
            : base(RecordingDispatchProxy<IMovementManager>.Create(out _))
        {
        }

        public void SetGuidForTest(uint guid)
        {
            Guid = guid;
        }

        public void SetHealthForTest(uint maxHealth, uint health)
        {
            MaxHealth = maxHealth;
            Health    = health;
        }

        public void SetShieldForTest(uint maxShield, uint shield)
        {
            MaxShieldCapacity = maxShield;
            Shield            = shield;
        }

        public void AddVisibleForTest(IGridEntity entity)
        {
            visibleEntities.Add(entity.Guid, entity);
        }

        protected override IEntityModel BuildEntityModel()
        {
            return new WorldUnitEntityModel();
        }

        protected override float CalculateDefaultProperty(Property property)
        {
            return 0f;
        }
    }
}
