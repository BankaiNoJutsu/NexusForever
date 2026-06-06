using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using NexusForever.Game.Abstract.Combat;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Entity;
using NexusForever.Game.Spell;
using NexusForever.Game.Static.Combat.CrowdControl;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Spell;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.World.Entity;
using NexusForever.Network.World.Entity.Model;
using NexusForever.Network.World.Message.Static;
using NexusForever.Shared;

namespace NexusForever.Game.Tests.Entity;

[Collection(LegacyServiceProviderCollection.Name)]
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

    [Fact]
    public void TakeDamage_ShieldAbsorbDelaysShieldRegenUntilRebootExpires()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        using ServiceProvider provider = BuildEntityProvider();
        LegacyServiceProvider.Provider = provider;

        try
        {
            TestUnitEntity victim = new();
            victim.SetHealthForTest(maxHealth: 100u, health: 100u);
            victim.SetShieldForTest(maxShield: 100u, shield: 100u, shieldRegenPct: 0.25f, shieldRebootTimeMs: 2500f, shieldTickTimeMs: 500f);
            IUnitEntity attacker = CreateAttacker();

            IDamageDescription damage = CreateDamage(adjustedDamage: 0u, shieldAbsorbAmount: 10u);

            victim.TakeDamage(attacker, damage);
            Assert.Equal(90u, victim.Shield);

            for (int i = 0; i < 10; i++)
                victim.Update(0.25d);

            Assert.Equal(90u, victim.Shield);

            victim.Update(0.25d);
            victim.Update(0.25d);

            Assert.Equal(100u, victim.Shield);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void TakeDamage_HealthDamageAlsoDelaysShieldRegen()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        using ServiceProvider provider = BuildEntityProvider();
        LegacyServiceProvider.Provider = provider;

        try
        {
            TestUnitEntity victim = new();
            victim.SetHealthForTest(maxHealth: 100u, health: 100u);
            victim.SetShieldForTest(maxShield: 100u, shield: 0u, shieldRegenPct: 0.25f, shieldRebootTimeMs: 2500f, shieldTickTimeMs: 500f);
            IUnitEntity attacker = CreateAttacker();

            victim.TakeDamage(attacker, CreateDamage(adjustedDamage: 10u));

            for (int i = 0; i < 10; i++)
                victim.Update(0.25d);

            Assert.Equal(0u, victim.Shield);

            victim.Update(0.25d);
            victim.Update(0.25d);

            Assert.Equal(12u, victim.Shield);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void Update_DoesNotRegenerateHealthWhileInCombat()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        using ServiceProvider provider = BuildEntityProvider();
        LegacyServiceProvider.Provider = provider;

        try
        {
            TestUnitEntity unit = new();
            unit.SetHealthForTest(maxHealth: 1000u, health: 500u);
            unit.SetInCombatForTest(true);

            unit.Update(0.25d);

            Assert.Equal(500u, unit.Health);

            unit.SetInCombatForTest(false);

            unit.Update(0.25d);

            Assert.Equal(505u, unit.Health);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Theory]
    [InlineData(100u, 125u, true, 25u)]
    [InlineData(100u, 100u, true, 0u)]
    [InlineData(100u, 40u, false, 0u)]
    public void CalculateHealthOverkill_UsesKilledTargetAndPreDamageHealth(uint healthBefore, uint adjustedDamage, bool killedTarget, uint expected)
    {
        Assert.Equal(expected, UnitEntity.CalculateHealthOverkill(healthBefore, adjustedDamage, killedTarget));
    }

    [Theory]
    [InlineData(CCState.Interrupt)]
    [InlineData(CCState.Knockdown)]
    public void AddCCState_WhenStateInterruptsActiveCasting_CancelsActiveCastingSpells(CCState state)
    {
        TestUnitEntity unit = new();
        ISpell spell = RecordingDispatchProxy<ISpell>.Create(out RecordingDispatchProxy<ISpell> spellProxy);
        spellProxy.SetProperty(nameof(ISpell.IsCasting), true);
        spellProxy.SetProperty(nameof(ISpell.BlocksCasting), true);
        AddPendingSpell(unit, spell);

        unit.AddCCState(state, effectId: 10u, spell4Id: 20u, castingId: 30u);

        RecordingDispatchProxy<ISpell>.Invocation cancel = Assert.Single(spellProxy.GetInvocations(nameof(ISpell.CancelCast)));
        Assert.Equal(CastResult.SpellInterrupted, cancel.Arguments[0]);
    }

    [Fact]
    public void AddCCState_Knockdown_CancelsAwaitingImpactSpells()
    {
        TestUnitEntity unit = new();
        ISpell spell = RecordingDispatchProxy<ISpell>.Create(out RecordingDispatchProxy<ISpell> spellProxy);
        spellProxy.SetProperty(nameof(ISpell.IsCasting), false);
        spellProxy.SetProperty(nameof(ISpell.BlocksCasting), true);
        AddPendingSpell(unit, spell);

        unit.AddCCState(CCState.Knockdown, effectId: 10u, spell4Id: 20u, castingId: 30u);

        RecordingDispatchProxy<ISpell>.Invocation cancel = Assert.Single(spellProxy.GetInvocations(nameof(ISpell.CancelCast)));
        Assert.Equal(CastResult.SpellInterrupted, cancel.Arguments[0]);
    }

    [Fact]
    public void CancelSpellsOnMove_WhenMovementInterruptedSpellBlocksButIsExecuting_CancelsSpell()
    {
        TestUnitEntity unit = new();
        ISpell spell = RecordingDispatchProxy<ISpell>.Create(out RecordingDispatchProxy<ISpell> spellProxy);
        spellProxy.SetProperty(nameof(ISpell.IsCasting), false);
        spellProxy.SetProperty(nameof(ISpell.BlocksCasting), true);
        spellProxy.SetMethodReturn(nameof(ISpell.IsMovingInterrupted), true);
        AddPendingSpell(unit, spell);

        unit.CancelSpellsOnMove();

        RecordingDispatchProxy<ISpell>.Invocation cancel = Assert.Single(spellProxy.GetInvocations(nameof(ISpell.CancelCast)));
        Assert.Equal(CastResult.CasterMovement, cancel.Arguments[0]);
    }

    [Fact]
    public void CheckActiveCastSlot_WhenActiveCastPending_ReturnsAlreadyCasting()
    {
        TestUnitEntity unit = new();
        ISpell spell = RecordingDispatchProxy<ISpell>.Create(out RecordingDispatchProxy<ISpell> spellProxy);
        spellProxy.SetProperty(nameof(ISpell.BlocksCasting), true);
        AddPendingSpell(unit, spell);

        CastResult result = unit.CheckActiveCastSlot(new SpellParameters());

        Assert.Equal(CastResult.SpellAlreadyCasting, result);
    }

    [Fact]
    public void CheckActiveCastSlot_WhenNestedSpellCastsDuringActiveCast_AllowsCast()
    {
        TestUnitEntity unit = new();
        ISpell spell = RecordingDispatchProxy<ISpell>.Create(out RecordingDispatchProxy<ISpell> spellProxy);
        spellProxy.SetProperty(nameof(ISpell.BlocksCasting), true);
        AddPendingSpell(unit, spell);

        ISpellInfo parentSpellInfo = RecordingDispatchProxy<ISpellInfo>.Create(out _);
        CastResult result = unit.CheckActiveCastSlot(new SpellParameters
        {
            ParentSpellInfo = parentSpellInfo
        });

        Assert.Equal(CastResult.Ok, result);
    }

    [Fact]
    public void CheckActiveCastSlot_WhenPendingSpellDoesNotBlockCasting_AllowsCast()
    {
        TestUnitEntity unit = new();
        ISpell spell = RecordingDispatchProxy<ISpell>.Create(out RecordingDispatchProxy<ISpell> spellProxy);
        spellProxy.SetProperty(nameof(ISpell.BlocksCasting), false);
        AddPendingSpell(unit, spell);

        CastResult result = unit.CheckActiveCastSlot(new SpellParameters());

        Assert.Equal(CastResult.Ok, result);
    }

    private static IDamageDescription CreateDamage(uint adjustedDamage, uint shieldAbsorbAmount = 0u)
    {
        return new SpellTargetInfo.SpellTargetEffectInfo.DamageDescription
        {
            DamageType         = DamageType.Physical,
            RawDamage          = adjustedDamage + shieldAbsorbAmount,
            RawScaledDamage    = adjustedDamage + shieldAbsorbAmount,
            AdjustedDamage     = adjustedDamage,
            ShieldAbsorbAmount = shieldAbsorbAmount,
            ThreatMultiplier   = 0f,
            CombatResult       = CombatResult.Hit
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

    private static void AddPendingSpell(UnitEntity unit, ISpell spell)
    {
        var pendingSpells = (List<ISpell>)typeof(UnitEntity)
            .GetField("pendingSpells", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(unit)!;

        pendingSpells.Add(spell);
    }

    private static ServiceProvider BuildEntityProvider()
    {
        EntityManager entityManager = new();
        typeof(EntityManager)
            .GetMethod("InitialiseEntityStats", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(entityManager, null);

        return new ServiceCollection()
            .AddSingleton(entityManager)
            .BuildServiceProvider();
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

        public void SetInCombatForTest(bool value)
        {
            typeof(UnitEntity)
                .GetField("inCombat", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(this, value);
        }

        public void SetShieldForTest(uint maxShield, uint shield, float shieldRegenPct, float shieldRebootTimeMs, float shieldTickTimeMs)
        {
            MaxShieldCapacity = maxShield;
            Shield = shield;
            SetBaseProperty(Property.ShieldRegenPct, shieldRegenPct);
            SetBaseProperty(Property.ShieldRebootTime, shieldRebootTimeMs);
            SetBaseProperty(Property.ShieldTickTime, shieldTickTimeMs);
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
