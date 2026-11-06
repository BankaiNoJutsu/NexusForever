using System.Runtime.CompilerServices;
using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Combat;
using NexusForever.Game.Entity;
using NexusForever.Game.Spell;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Spell;
using NexusForever.Game.Spell.Effect;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Entity;
using NexusForever.Network.World.Entity.Model;

namespace NexusForever.Game.Tests.Spell;

[Collection(MissingGameDataDiagnosticsCollection.Name)]
public class PersonalHealingScopeTests
{
    [Fact]
    public void FusionProbe_OnlyIncreasesHealingFromItsApplyingCaster()
    {
        var caster = new TestUnit();
        var otherCaster = new TestUnit();
        var target = new TestUnit();
        Apply(caster, target);

        Assert.Equal(120u, Heal(caster, target).Damage.RawDamage);
        Assert.Equal(100u, Heal(otherCaster, target).Damage.RawDamage);
        Assert.Equal(1f, target.GetPropertyValue(Property.HealingMultiplierIncoming));
    }

    [Fact]
    public void SameEffectFromTwoCasters_RetainsBothCasterScopes()
    {
        var caster = new TestUnit();
        var otherCaster = new TestUnit();
        var target = new TestUnit();
        Apply(caster, target, 1u);
        Apply(otherCaster, target, 2u);

        Assert.Equal(2, target.CaptureSpellPropertyInstances(88057u).Count);
        Assert.Equal(120u, Heal(caster, target).Damage.RawDamage);
        Assert.Equal(120u, Heal(otherCaster, target).Damage.RawDamage);
    }

    [Fact]
    public void PersonalHealthBonus_DoesNotBecomeAShieldHealingBonus()
    {
        var caster = new TestUnit();
        var target = new TestUnit();
        Apply(caster, target);

        Assert.Equal(100u, Heal(caster, target, shield: true).Damage.RawDamage);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OriginalExpiry_CannotRemoveReplacementOrOtherCaster(bool reuseEffectId)
    {
        var caster = new TestUnit();
        var otherCaster = new TestUnit();
        var target = new TestUnit();
        var original = Apply(caster, target, 1u);
        var other = Apply(otherCaster, target, 2u);
        var replacement = Apply(caster, target, reuseEffectId ? 1u : 3u);

        EndEffect(caster, target, original);
        Assert.Equal(2, target.CaptureSpellPropertyInstances(88057u).Count);
        Assert.Equal(120u, Heal(caster, target).Damage.RawDamage);
        Assert.Equal(120u, Heal(otherCaster, target).Damage.RawDamage);

        EndEffect(caster, target, replacement);
        EndEffect(caster, target, replacement);
        Assert.Equal(100u, Heal(caster, target).Damage.RawDamage);
        Assert.Equal(120u, Heal(otherCaster, target).Damage.RawDamage);
        EndEffect(otherCaster, target, other);
        Assert.False(target.HasTrackedSpellState(88057u));
    }

    [Fact]
    public void CasterIdentity_IsNotSharedByAReusedGuidOrAnotherTarget()
    {
        // Fresh units all have the same unadmitted GUID (zero), but distinct object identity.
        var caster = new TestUnit();
        var reusedGuid = new TestUnit();
        var target = new TestUnit();
        var otherTarget = new TestUnit();
        Apply(caster, target);
        Assert.Equal(caster.Guid, reusedGuid.Guid);
        Assert.Equal(100u, Heal(reusedGuid, target).Damage.RawDamage);
        Assert.Equal(100u, Heal(caster, otherTarget).Damage.RawDamage);
    }

    [Fact]
    public void GlobalModifiersAndHealingAbsorption_StillApplyOnce()
    {
        var caster = new TestUnit();
        var target = new TestUnit();
        caster.AddSpellModifierProperty(new SpellPropertyModifier(Property.HealingMultiplierOutgoing, 1u, 2f, 0f, 0f), 3u, 100u, 3u, 1u);
        target.AddSpellModifierProperty(new SpellPropertyModifier(Property.HealingMultiplierIncoming, 1u, 1.5f, 0f, 0f), 4u, 101u, 4u, 1u);
        Apply(caster, target);
        target.AddHealingAbsorption(5u, 102u, 1u, 60u);

        var heal = Heal(caster, target);
        Assert.Equal(360u, heal.Damage.RawDamage);
        Assert.Equal(60u, heal.Damage.AbsorbedAmount);
        Assert.Equal(300u, heal.Damage.AdjustedDamage);
        Assert.Equal(1.5f, target.GetPropertyValue(Property.HealingMultiplierIncoming));
        Assert.Equal(300u, Heal(caster, target, shield: true).Damage.RawDamage);
        Assert.Equal(150u, Heal(new TestUnit(), target).Damage.RawDamage);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExistingTrackedRemoval_CleansPersonalState(bool dispel)
    {
        var caster = new TestUnit();
        var target = new TestUnit();
        Apply(caster, target);
        if (dispel)
            Assert.Single(target.RemoveTrackedSpellStates(id => id == 88057u, 1u));
        else
            Assert.True(target.RemoveSpellProperties(88057u));
        Assert.False(target.HasTrackedSpellState(88057u));
        Assert.Equal(100u, Heal(caster, target).Damage.RawDamage);
    }

    [Theory]
    [InlineData("mode")]
    [InlineData("tail3")]
    [InlineData("tail4")]
    [InlineData("tail5")]
    [InlineData("tail6")]
    [InlineData("tail7")]
    [InlineData("tail8")]
    [InlineData("tail9")]
    [InlineData("group")]
    [InlineData("zero")]
    [InlineData("negative")]
    [InlineData("nan")]
    [InlineData("infinity")]
    public void UnsupportedPersonalShape_DoesNotInstallAGlobalOrScopedModifier(string scenario)
    {
        var caster = new TestUnit();
        var target = new TestUnit();
        var entry = PersonalEntry();
        if (scenario == "mode") entry.DataBits01 = 2u;
        else if (scenario.StartsWith("tail")) typeof(Spell4EffectsEntry).GetField("DataBits0" + scenario[4..])!.SetValue(entry, 1u);
        else if (scenario == "group") entry.Spell4EffectGroupListId = 1u;
        else entry.DataBits02 = BitConverter.SingleToUInt32Bits(scenario switch
        {
            "zero" => 0f, "negative" => -1f, "nan" => float.NaN, _ => float.PositiveInfinity
        });
        var info = new SpellTargetInfo.SpellTargetEffectInfo(1u, entry);
        SpellHandler.HandleEffectPersonalDmgHealMod(CreateSpell(caster), target, info);
        Assert.Null(info.PersonalHealingInstance);
        Assert.False(target.HasTrackedSpellState(88057u));
        Assert.Equal(100u, Heal(caster, target).Damage.RawDamage);
        Assert.Equal(1f, target.GetPropertyValue(Property.HealingMultiplierIncoming));
    }

    [Fact]
    public void RejectedEffectExpiry_CannotRemoveAnInstalledEffectWithTheSameId()
    {
        var caster = new TestUnit();
        var target = new TestUnit();
        Apply(caster, target);
        var rejectedEntry = PersonalEntry();
        rejectedEntry.DataBits01 = 2u;
        var rejected = new SpellTargetInfo.SpellTargetEffectInfo(1u, rejectedEntry);
        SpellHandler.HandleEffectPersonalDmgHealMod(CreateSpell(caster), target, rejected);
        EndEffect(caster, target, rejected);
        Assert.Equal(120u, Heal(caster, target).Damage.RawDamage);
    }

    [Fact]
    public void HealHandler_AppliesScopedHealingToLiveHealth()
    {
        var caster = new TestUnit();
        var target = new TestUnit();
        Apply(caster, target);
        var calculator = Calculator();
        var resolver = RecordingDispatchProxy<ISpellEffectDependencyResolver>.Create(out var proxy);
        proxy.SetMethodReturn(nameof(ISpellEffectDependencyResolver.CreateDamageCalculator), calculator);
        var previous = SpellHandler.InitialiseDependencyResolver(resolver);
        try
        {
            var info = HealInfo();
            SpellHandler.HandleEffectHeal(CreateSpell(caster), target, info);
            Assert.Equal(220u, target.Health);
            Assert.Equal(120u, info.Damage.AdjustedDamage);
        }
        finally { SpellHandler.InitialiseDependencyResolver(previous); }
    }

    [Fact]
    public void ScopedHealing_CapsHealthBeforeAbsorptionAndRecordsOverheal()
    {
        var caster = new TestUnit();
        var target = new TestUnit();
        target.SetHealth(950u);
        Apply(caster, target);
        target.AddHealingAbsorption(5u, 102u, 1u, 20u);
        var info = Heal(caster, target);
        Assert.Equal(120u, info.Damage.RawDamage);
        Assert.Equal(30u, info.Damage.AdjustedDamage);
        Assert.Equal(20u, info.Damage.AbsorbedAmount);
        Assert.Equal(70u, info.Damage.OverkillAmount);
        Assert.Equal(0u, target.CurrentHealingAbsorption);
        var log = Assert.Single(info.CombatLogs.OfType<NexusForever.Network.World.Combat.CombatLogHeal>());
        Assert.Equal(30u, log.HealAmount);
        Assert.Equal(70u, log.Overheal);
        Assert.Equal(20u, log.Absorption);
    }

    [Fact]
    public void MissingCaster_CannotInstallOrReadAPersonalBonus()
    {
        var target = new TestUnit();
        var info = Apply(null, target);
        Assert.Null(info.PersonalHealingInstance);
        Assert.Equal(1f, target.GetPersonalHealingMultiplier(null));
        Assert.False(target.HasTrackedSpellState(88057u));
    }

    private static SpellTargetInfo.SpellTargetEffectInfo Apply(IUnitEntity caster, TestUnit target, uint effectId = 1u)
    {
        var info = new SpellTargetInfo.SpellTargetEffectInfo(effectId, PersonalEntry());
        SpellHandler.HandleEffectPersonalDmgHealMod(CreateSpell(caster), target, info);
        return info;
    }

    private static Spell4EffectsEntry PersonalEntry() => new()
    {
        Id = 231375u, SpellId = 88057u, EffectType = SpellEffectType.PersonalDmgHealMod,
        DataBits00 = 12u, DataBits01 = 1u, DataBits02 = BitConverter.SingleToUInt32Bits(1.2f), Flags = 2u
    };

    private static DamageCalculator Calculator()
    {
        IGameTableManager tables = RecordingDispatchProxy<IGameTableManager>.Create(out var tableProxy);
        // An empty real table gives the producer its existing formula fallback.
        var formulas = (GameTable<GameFormulaEntry>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<GameFormulaEntry>));
        typeof(GameTable<GameFormulaEntry>).GetField("header", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .SetValue(formulas, new GameTableHeader { MaxId = 0u });
        tableProxy.SetProperty(nameof(IGameTableManager.GameFormula), formulas);
        return new DamageCalculator(NullLogger<DamageCalculator>.Instance, tables);
    }

    private static SpellTargetInfo.SpellTargetEffectInfo HealInfo(bool shield = false) => new(10u, new Spell4EffectsEntry
        {
            Id = 10u, EffectType = shield ? SpellEffectType.HealShields : SpellEffectType.Heal,
            DamageType = DamageType.Heal, DataBits00 = BitConverter.SingleToUInt32Bits(1f), DataBits01 = 100u
        });

    private static SpellTargetInfo.SpellTargetEffectInfo Heal(IUnitEntity caster, TestUnit target, bool shield = false)
    {
        var calculator = Calculator();
        var info = HealInfo(shield);
        if (shield)
            calculator.CalculateShieldHealing(caster, target, CreateSpell(caster), info);
        else
            calculator.CalculateHealing(caster, target, CreateSpell(caster), info);
        return info;
    }

    private static void EndEffect(IUnitEntity caster, TestUnit target, SpellTargetInfo.SpellTargetEffectInfo info)
    {
        var spell = (Game.Spell.Spell)RuntimeHelpers.GetUninitializedObject(typeof(Game.Spell.Spell));
        var parameters = CreateSpell(caster).Parameters;
        var baseInfo = RecordingDispatchProxy<ISpellBaseInfo>.Create(out _);
        ((RecordingDispatchProxy<ISpellInfo>)(object)parameters.SpellInfo).SetProperty(nameof(ISpellInfo.BaseInfo), baseInfo);
        const BindingFlags fields = BindingFlags.NonPublic | BindingFlags.Instance;
        typeof(Game.Spell.Spell).GetField("<Caster>k__BackingField", fields)!.SetValue(spell, caster);
        typeof(Game.Spell.Spell).GetField("<Parameters>k__BackingField", fields)!.SetValue(spell, parameters);
        var method = typeof(Game.Spell.Spell).GetMethod("TryRemoveActiveEffect", fields,
            [typeof(SpellEffectInterpretation), typeof(IUnitEntity), typeof(ISpellTargetEffectInfo)])!;
        method.Invoke(spell, [info.Interpretation, target, info]);
    }

    private static ISpell CreateSpell(IUnitEntity caster)
    {
        ISpellInfo spellInfo = RecordingDispatchProxy<ISpellInfo>.Create(out var infoProxy);
        infoProxy.SetProperty(nameof(ISpellInfo.Entry), new Spell4Entry { Id = 88057u });
        infoProxy.SetProperty(nameof(ISpellInfo.Effects), new List<Spell4EffectsEntry>());
        ISpell spell = RecordingDispatchProxy<ISpell>.Create(out var proxy);
        proxy.SetProperty(nameof(ISpell.Caster), caster);
        proxy.SetProperty(nameof(ISpell.CastingId), 1u);
        proxy.SetProperty(nameof(ISpell.Parameters), new SpellParameters { SpellInfo = spellInfo });
        return spell;
    }

    private sealed class TestUnit : UnitEntity
    {
        public TestUnit() : base(RecordingDispatchProxy<IMovementManager>.Create(out _))
        {
            MaxHealth = 1000u;
            Health = 100u;
            MaxShieldCapacity = 1000u;
        }
        public override EntityType Type => EntityType.Simple;
        public void SetHealth(uint value) => Health = value;
        protected override IEntityModel BuildEntityModel() => new WorldUnitEntityModel();
        protected override float CalculateDefaultProperty(Property property) => 1f;
    }
}
