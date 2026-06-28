using System.Reflection;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Combat;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Creature;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Combat;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Network.World.Combat;
using NexusForever.Network.World.Entity;
using NexusForever.Network.World.Entity.Model;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Tests.Entity;

public class UnitEntityKillChainTests
{
    [Fact]
    public void RewardKiller_FirstCreatureKill_DoesNotSendKillChainCombatLog()
    {
        IPlayer player = CreateRewardPlayer(guid: 42u, out RecordingDispatchProxy<IGameSession> sessionProxy);

        RewardCreatureKill(player, creatureId: 1001u);

        Assert.Empty(GetKillChainLogs(sessionProxy));
        Assert.Empty(GetKillChainRewards(sessionProxy));
    }

    [Fact]
    public void RewardKiller_SecondCreatureKill_SendsDoubleKillChainCombatLog()
    {
        IPlayer player = CreateRewardPlayer(guid: 42u, out RecordingDispatchProxy<IGameSession> sessionProxy);

        RewardCreatureKill(player, creatureId: 1001u);
        RewardCreatureKill(player, creatureId: 1002u);

        CombatLogKillStreak log = Assert.Single(GetKillChainLogs(sessionProxy));
        Assert.Equal(42u, log.UnitId);
        Assert.Equal(CombatMomentumStat.KillChain, log.StatType);
        Assert.Equal(2u, log.StreakAmount);

        ServerCombatReward reward = Assert.Single(GetKillChainRewards(sessionProxy));
        Assert.Equal((byte)CombatMomentumStat.KillChain, reward.Stat);
        Assert.Equal(2u, reward.NewValue);
        Assert.Equal(4u, reward.CombatRewardId);
        Assert.Equal(1002u, reward.TargetUnitId);
    }

    [Fact]
    public void RewardKiller_ThirdCreatureKill_SendsTripleKillChainCombatLog()
    {
        IPlayer player = CreateRewardPlayer(guid: 42u, out RecordingDispatchProxy<IGameSession> sessionProxy);

        RewardCreatureKill(player, creatureId: 1001u);
        RewardCreatureKill(player, creatureId: 1002u);
        RewardCreatureKill(player, creatureId: 1003u);

        IReadOnlyList<CombatLogKillStreak> logs = GetKillChainLogs(sessionProxy);
        Assert.Equal([2u, 3u], logs.Select(l => l.StreakAmount));
        Assert.All(logs, log =>
        {
            Assert.Equal(42u, log.UnitId);
            Assert.Equal(CombatMomentumStat.KillChain, log.StatType);
        });

        IReadOnlyList<ServerCombatReward> rewards = GetKillChainRewards(sessionProxy);
        Assert.Equal([2ul, 3ul], rewards.Select(r => r.NewValue));
        Assert.Equal([4u, 5u], rewards.Select(r => r.CombatRewardId));
        Assert.Equal([1002u, 1003u], rewards.Select(r => r.TargetUnitId));
    }

    [Fact]
    public void RewardKiller_HigherCreatureKillChain_UsesHighestClientCombatRewardId()
    {
        IPlayer player = CreateRewardPlayer(guid: 42u, out RecordingDispatchProxy<IGameSession> sessionProxy);

        for (uint i = 0u; i < 8u; i++)
            RewardCreatureKill(player, creatureId: 1001u + i);

        IReadOnlyList<ServerCombatReward> rewards = GetKillChainRewards(sessionProxy);
        Assert.Equal([4u, 5u, 6u, 7u, 20u, 21u, 22u], rewards.Select(r => r.CombatRewardId));
        Assert.Equal(8ul, rewards[^1].NewValue);
    }

    private static void RewardCreatureKill(IPlayer player, uint creatureId)
    {
        var unit = new TestUnitEntity();
        unit.InitialiseForCreatureKill(creatureId);
        unit.RewardKillerForTest(player);
    }

    private static IPlayer CreateRewardPlayer(uint guid, out RecordingDispatchProxy<IGameSession> sessionProxy)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IGridEntity.Guid), guid);
        playerProxy.SetProperty(nameof(IPlayer.Session), RecordingDispatchProxy<IGameSession>.Create(out sessionProxy));
        playerProxy.SetProperty(nameof(IPlayer.QuestManager), RecordingDispatchProxy<IQuestManager>.Create(out _));
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), RecordingDispatchProxy<ICharacterAchievementManager>.Create(out _));
        playerProxy.SetProperty(nameof(IPlayer.PathManager), RecordingDispatchProxy<IPathManager>.Create(out _));
        playerProxy.SetProperty(nameof(IPlayer.XpManager), RecordingDispatchProxy<IXpManager>.Create(out _));
        return player;
    }

    private static IReadOnlyList<CombatLogKillStreak> GetKillChainLogs(RecordingDispatchProxy<IGameSession> sessionProxy)
    {
        return sessionProxy
            .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(invocation => invocation.Arguments[0])
            .OfType<ServerCombatLog>()
            .Select(message => message.CombatLog)
            .OfType<CombatLogKillStreak>()
            .ToList();
    }

    private static IReadOnlyList<ServerCombatReward> GetKillChainRewards(RecordingDispatchProxy<IGameSession> sessionProxy)
    {
        return sessionProxy
            .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(invocation => invocation.Arguments[0])
            .OfType<ServerCombatReward>()
            .Where(reward => reward.Stat == (byte)CombatMomentumStat.KillChain)
            .ToList();
    }

    private static void SetAutoProperty(object instance, string propertyName, object value)
    {
        for (Type type = instance.GetType(); type != null; type = type.BaseType)
        {
            PropertyInfo property = type.GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            MethodInfo setter = property?.GetSetMethod(true);
            if (setter == null)
                continue;

            setter.Invoke(instance, [value]);
            return;
        }

        throw new MissingMemberException(instance.GetType().FullName, propertyName);
    }

    private sealed class TestUnitEntity : UnitEntity
    {
        public override EntityType Type => EntityType.Simple;

        public override uint Health { get; protected set; }

        public TestUnitEntity()
            : base(RecordingDispatchProxy<IMovementManager>.Create(out _))
        {
        }

        public void InitialiseForCreatureKill(uint creatureId)
        {
            SetAutoProperty(this, nameof(IGridEntity.Guid), creatureId);
            SetAutoProperty(this, nameof(CreatureEntry), new Creature2Entry
            {
                Id = creatureId
            });

            ICreatureInfo creatureInfo = RecordingDispatchProxy<ICreatureInfo>.Create(out RecordingDispatchProxy<ICreatureInfo> creatureInfoProxy);
            creatureInfoProxy.SetProperty(nameof(ICreatureInfo.DifficultyEntry), new Creature2DifficultyEntry
            {
                Id = 1u
            });
            CreatureInfo = creatureInfo;
        }

        public void RewardKillerForTest(IPlayer player)
        {
            RewardKiller(player);
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
