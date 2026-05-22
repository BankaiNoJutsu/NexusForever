using NexusForever.Game.Abstract.Combat;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Entity;
using NexusForever.Game.Spell;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Spell;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.World.Entity;
using NexusForever.Network.World.Entity.Model;

namespace NexusForever.Game.Tests.Entity;

public class UnitEntityDamageResultTests
{
    [Fact]
    public void TakeDamage_SetsKilledTargetAndHealthOverkill()
    {
        TestUnitEntity victim = new();
        victim.SetHealthForTest(maxHealth: 100u, health: 100u);
        IUnitEntity attacker = CreateAttacker();

        IDamageDescription damage = CreateDamage(adjustedDamage: 125u);

        victim.TakeDamage(attacker, damage);

        Assert.False(victim.IsAlive);
        Assert.True(damage.KilledTarget);
        Assert.Equal(25u, damage.OverkillAmount);
        Assert.Equal(0u, victim.Health);
    }

    [Fact]
    public void TakeDamage_ClearsOverkillWhenDamageDoesNotKill()
    {
        TestUnitEntity victim = new();
        victim.SetHealthForTest(maxHealth: 100u, health: 100u);
        IUnitEntity attacker = CreateAttacker();

        IDamageDescription damage = CreateDamage(adjustedDamage: 40u);
        damage.OverkillAmount = 99u;

        victim.TakeDamage(attacker, damage);

        Assert.True(victim.IsAlive);
        Assert.False(damage.KilledTarget);
        Assert.Equal(0u, damage.OverkillAmount);
        Assert.Equal(60u, victim.Health);

    }

    [Theory]
    [InlineData(100u, 125u, true, 25u)]
    [InlineData(100u, 100u, true, 0u)]
    [InlineData(100u, 40u, false, 0u)]
    public void CalculateHealthOverkill_UsesKilledTargetAndPreDamageHealth(uint healthBefore, uint adjustedDamage, bool killedTarget, uint expected)
    {
        Assert.Equal(expected, UnitEntity.CalculateHealthOverkill(healthBefore, adjustedDamage, killedTarget));
    }

    private static IDamageDescription CreateDamage(uint adjustedDamage)
    {
        return new SpellTargetInfo.SpellTargetEffectInfo.DamageDescription
        {
            DamageType       = DamageType.Physical,
            RawDamage        = adjustedDamage,
            RawScaledDamage  = adjustedDamage,
            AdjustedDamage   = adjustedDamage,
            ThreatMultiplier = 0f,
            CombatResult     = CombatResult.Hit
        };
    }

    private static IUnitEntity CreateAttacker()
    {
        IUnitEntity attacker = RecordingDispatchProxy<IUnitEntity>.Create(out var attackerProxy);
        IThreatManager threatManager = RecordingDispatchProxy<IThreatManager>.Create(out _);
        attackerProxy.SetProperty(nameof(IGridEntity.Guid), 42u);
        attackerProxy.SetProperty(nameof(IUnitEntity.IsAlive), true);
        attackerProxy.SetProperty(nameof(IUnitEntity.ThreatManager), threatManager);
        return attacker;
    }

    private sealed class TestUnitEntity : UnitEntity
    {
        public override EntityType Type => EntityType.Simple;

        public override uint Health { get; protected set; }

        public TestUnitEntity()
            : base(RecordingDispatchProxy<IMovementManager>.Create(out _))
        {
        }

        public void SetHealthForTest(uint maxHealth, uint health)
        {
            MaxHealth = maxHealth;
            Health    = health;
        }

        protected override IEntityModel BuildEntityModel()
        {
            return new WorldUnitEntityModel();
        }

        protected override float CalculateDefaultProperty(Property property)
        {
            return 0f;
        }

        protected override void OnDeath()
        {
            // Keep the result-state test isolated from reward, respawn, and map side effects.
        }
    }
}
