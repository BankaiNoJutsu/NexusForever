using System.Reflection;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Abstract.Matching.Match;
using NexusForever.Game.Abstract.Matching.Queue;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.Network.Message;
using NexusForever.Network.Internal;
using NexusForever.Network.Session;
using NexusForever.Network.World.Entity;
using NexusForever.Network.World.Entity.Model;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Tests.Entity;

public class WorldEntityVitalPacketTests
{
    [Fact]
    public void StatChange_ForEnteredWorldOwningPlayerWithoutSelfVisible_SendsStatToOwnSession()
    {
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out RecordingDispatchProxy<IGameSession> sessionProxy);
        var player = new TestPlayerEntity();
        player.SetGuidForTest(1001u);
        player.IsLoading = false;
        SetAutoProperty(player, nameof(Player.Identity), new Identity
        {
            Id      = 1ul,
            RealmId = 1
        });
        SetAutoProperty(player, nameof(Player.Session), session);

        player.SetIntegerStatForTest(Stat.Level, 2u);

        ServerEntityStatUpdateInteger update = Assert.Single(sessionProxy
            .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(invocation => invocation.Arguments[0])
            .OfType<ServerEntityStatUpdateInteger>());
        Assert.Equal(player.Guid, update.UnitId);
        Assert.Equal(Stat.Level, update.Stat.Stat);
        Assert.Equal(StatType.Integer, update.Stat.Type);
        Assert.Equal(2f, update.Stat.Value);
    }

    [Fact]
    public void StatChange_ForLoadingOwningPlayerWithoutSelfVisible_DoesNotSendStatToOwnSession()
    {
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out RecordingDispatchProxy<IGameSession> sessionProxy);
        var player = new TestPlayerEntity();
        player.SetGuidForTest(1001u);
        player.IsLoading = true;
        SetAutoProperty(player, nameof(Player.Identity), new Identity
        {
            Id      = 1ul,
            RealmId = 1
        });
        SetAutoProperty(player, nameof(Player.Session), session);

        player.SetIntegerStatForTest(Stat.Level, 2u);

        Assert.DoesNotContain(sessionProxy
            .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(invocation => invocation.Arguments[0]),
            message => message is ServerEntityStatUpdateInteger);
    }

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

    private static IInternalMessagePublisher CreateMessagePublisher()
    {
        IInternalMessagePublisher messagePublisher = RecordingDispatchProxy<IInternalMessagePublisher>.Create(out RecordingDispatchProxy<IInternalMessagePublisher> proxy);
        proxy.SetMethodReturn(nameof(IInternalMessagePublisher.PublishAsync), Task.CompletedTask);
        return messagePublisher;
    }

    private static T CreateProxy<T>() where T : class
    {
        return RecordingDispatchProxy<T>.Create(out _);
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

    private sealed class TestPlayerEntity : Player
    {
        public TestPlayerEntity()
            : base(
                CreateProxy<IMovementManager>(),
                CreateMessagePublisher(),
                CreateProxy<IEntityFactory>(),
                CreateProxy<IMatchingManager>(),
                CreateProxy<IMatchManager>(),
                CreateProxy<IGameTableManager>(),
                CreateProxy<ICurrencyManager>())
        {
        }

        public void SetGuidForTest(uint guid)
        {
            Guid = guid;
        }

        public void SetIntegerStatForTest(Stat stat, uint value)
        {
            SetStat(stat, value);
        }
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
