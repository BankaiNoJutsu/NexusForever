using Microsoft.Extensions.DependencyInjection;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement.Force;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Spell;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Spell;
using NexusForever.Game.Static.Spell.Effect;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Combat;
using NexusForever.Network.World.Message.Model;
using NexusForever.Shared;

namespace NexusForever.Game.Tests.Spell;

[Collection(LegacyServiceProviderCollection.Name)]
public class SpellDamagePermissionTests : IDisposable
{
    private readonly IServiceProvider previousProvider;
    private readonly ISpellEffectDependencyResolver previousDependencyResolver;
    private readonly ServiceProvider provider;

    public SpellDamagePermissionTests()
    {
        previousProvider = LegacyServiceProvider.Provider;
        previousDependencyResolver = SpellHandler.InitialiseDependencyResolver(null);
        provider = new ServiceCollection()
            .AddSingleton<IFactory<IDamageCalculator>>(new StaticFactory<IDamageCalculator>(new FakeDamageCalculator(17u, 11u)))
            .BuildServiceProvider();
        LegacyServiceProvider.Provider = provider;
    }

    public void Dispose()
    {
        SpellHandler.InitialiseDependencyResolver(previousDependencyResolver);
        LegacyServiceProvider.Provider = previousProvider;
        provider.Dispose();
    }

    [Fact]
    public void HandleEffectDamage_UsesCasterAttackPermission()
    {
        IUnitEntity caster = RecordingDispatchProxy<IUnitEntity>.Create(out RecordingDispatchProxy<IUnitEntity> casterProxy);
        casterProxy.SetProperty(nameof(IGridEntity.Guid), 1u);
        casterProxy.SetMethodReturn(nameof(IUnitEntity.CanAttack), true);

        IUnitEntity target = RecordingDispatchProxy<IUnitEntity>.Create(out RecordingDispatchProxy<IUnitEntity> targetProxy);
        targetProxy.SetProperty(nameof(IGridEntity.Guid), 2u);
        targetProxy.SetMethodReturn(nameof(IUnitEntity.CanAttack), false);

        ISpellInfo spellInfo = RecordingDispatchProxy<ISpellInfo>.Create(out RecordingDispatchProxy<ISpellInfo> spellInfoProxy);
        spellInfoProxy.SetProperty(nameof(ISpellInfo.Entry), new Spell4Entry { Id = 37302u });

        ISpell spell = RecordingDispatchProxy<ISpell>.Create(out RecordingDispatchProxy<ISpell> spellProxy);
        spellProxy.SetProperty(nameof(ISpell.Caster), caster);
        spellProxy.SetProperty(nameof(ISpell.Parameters), new SpellParameters { SpellInfo = spellInfo });

        var effectInfo = new SpellTargetInfo.SpellTargetEffectInfo(3u, new Spell4EffectsEntry
        {
            Id         = 79090u,
            EffectType = SpellEffectType.Damage,
            DamageType = DamageType.Physical
        });

        SpellHandler.HandleEffectDamage(spell, target, effectInfo);

        Assert.Single(casterProxy.GetInvocations(nameof(IUnitEntity.CanAttack)));
        Assert.Empty(targetProxy.GetInvocations(nameof(IUnitEntity.CanAttack)));
        Assert.Single(targetProxy.GetInvocations(nameof(IUnitEntity.TakeDamage)));
        CombatLogDamage combatLog = Assert.IsType<CombatLogDamage>(Assert.Single(effectInfo.CombatLogs));
        Assert.Equal(17u, combatLog.RawDamage);
    }

    [Fact]
    public void HandleEffectDamage_UsesInitialisedDependencyResolverBeforeLegacyProvider()
    {
        var injectedCalculator = new FakeDamageCalculator(23u, 19u);
        SpellHandler.InitialiseDependencyResolver(new FakeSpellEffectDependencyResolver(injectedCalculator));

        IUnitEntity caster = RecordingDispatchProxy<IUnitEntity>.Create(out RecordingDispatchProxy<IUnitEntity> casterProxy);
        casterProxy.SetProperty(nameof(IGridEntity.Guid), 1u);
        casterProxy.SetMethodReturn(nameof(IUnitEntity.CanAttack), true);

        IUnitEntity target = RecordingDispatchProxy<IUnitEntity>.Create(out RecordingDispatchProxy<IUnitEntity> targetProxy);
        targetProxy.SetProperty(nameof(IGridEntity.Guid), 2u);

        ISpellInfo spellInfo = RecordingDispatchProxy<ISpellInfo>.Create(out RecordingDispatchProxy<ISpellInfo> spellInfoProxy);
        spellInfoProxy.SetProperty(nameof(ISpellInfo.Entry), new Spell4Entry { Id = 37302u });

        ISpell spell = RecordingDispatchProxy<ISpell>.Create(out RecordingDispatchProxy<ISpell> spellProxy);
        spellProxy.SetProperty(nameof(ISpell.Caster), caster);
        spellProxy.SetProperty(nameof(ISpell.Parameters), new SpellParameters { SpellInfo = spellInfo });

        var effectInfo = new SpellTargetInfo.SpellTargetEffectInfo(3u, new Spell4EffectsEntry
        {
            Id         = 79090u,
            EffectType = SpellEffectType.Damage,
            DamageType = DamageType.Physical
        });

        SpellHandler.HandleEffectDamage(spell, target, effectInfo);

        Assert.Equal(1, injectedCalculator.CalculateDamageCalls);
        Assert.Single(targetProxy.GetInvocations(nameof(IUnitEntity.TakeDamage)));
        CombatLogDamage combatLog = Assert.IsType<CombatLogDamage>(Assert.Single(effectInfo.CombatLogs));
        Assert.Equal(23u, combatLog.RawDamage);
    }

    private sealed class FakeDamageCalculator : IDamageCalculator
    {
        private readonly uint rawDamage;
        private readonly uint adjustedDamage;

        public FakeDamageCalculator(uint rawDamage, uint adjustedDamage)
        {
            this.rawDamage      = rawDamage;
            this.adjustedDamage = adjustedDamage;
        }

        public int CalculateDamageCalls { get; private set; }

        public void CalculateDamage(IUnitEntity attacker, IUnitEntity victim, ISpell spell, ISpellTargetEffectInfo info)
        {
            CalculateDamageCalls++;
            info.AddDamage(new SpellTargetInfo.SpellTargetEffectInfo.DamageDescription
            {
                DamageType      = DamageType.Physical,
                RawDamage       = rawDamage,
                RawScaledDamage = rawDamage,
                AdjustedDamage  = adjustedDamage,
                CombatResult    = CombatResult.Hit
            });
        }

        public void CalculateHealing(IUnitEntity caster, IUnitEntity target, ISpell spell, ISpellTargetEffectInfo info) =>
            throw new NotSupportedException();

        public void CalculateShieldHealing(IUnitEntity caster, IUnitEntity target, ISpell spell, ISpellTargetEffectInfo info) =>
            throw new NotSupportedException();

        public void CalculateShieldDamage(IUnitEntity attacker, IUnitEntity victim, ISpell spell, ISpellTargetEffectInfo info) =>
            throw new NotSupportedException();

        public uint CalculateAbsorption(IUnitEntity caster, IUnitEntity target, ISpell spell, ISpellTargetEffectInfo info) =>
            throw new NotSupportedException();

        public uint CalculateHealingAbsorption(IUnitEntity caster, IUnitEntity target, ISpell spell, ISpellTargetEffectInfo info) =>
            throw new NotSupportedException();
    }

    private sealed class StaticFactory<T> : IFactory<T> where T : class
    {
        private readonly T instance;

        public StaticFactory(T instance)
        {
            this.instance = instance;
        }

        public T Resolve() => instance;
    }

    private sealed class FakeSpellEffectDependencyResolver : ISpellEffectDependencyResolver
    {
        private readonly IDamageCalculator damageCalculator;

        public FakeSpellEffectDependencyResolver(IDamageCalculator damageCalculator)
        {
            this.damageCalculator = damageCalculator;
        }

        public IDamageCalculator CreateDamageCalculator()
        {
            return damageCalculator;
        }

        public IEntityFactory GetEntityFactory()
        {
            return null;
        }

        public IForcedMovementGenerator GetForcedMovementGenerator()
        {
            return null;
        }
    }
}
