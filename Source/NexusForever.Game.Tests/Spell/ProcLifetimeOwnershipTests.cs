using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Spell;
using NexusForever.Game.Spell.Effect;
using NexusForever.Game.Static.Spell;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model;
using RuntimeSpell = NexusForever.Game.Spell.Spell;

namespace NexusForever.Game.Tests.Spell;

[Collection(MissingGameDataDiagnosticsCollection.Name)]
public sealed class ProcLifetimeOwnershipTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RetiredProcLifetimeCannotRemoveItsReplacement(bool rejectedOriginal)
    {
        var tables = RecordingDispatchProxy<IGameTableManager>.Create(out var table);
        table.SetProperty(nameof(IGameTableManager.Spell4), PlayerBindPointTests.Table(new Spell4Entry { Id = 60979 }));
        using var services = new ServiceCollection().AddSingleton(tables).BuildServiceProvider();
        var previous = SpellHandler.InitialiseDependencyResolver(new SpellEffectDependencyResolver(services));
        try
        {
            var target = new AmpTableAugmentationTests.TestUnit();
            var original = CreateSpell(out var originalCaster);
            var replacement = CreateSpell(out var replacementCaster);
            var originalEffect = Effect(rejectedOriginal ? 60980u : 60979u);
            var replacementEffect = Effect(60979u);

            SpellHandler.HandleEffectProc(original, target, originalEffect);
            Assert.Equal(rejectedOriginal ? 0 : 1, target.CreateProcRegistrationSnapshot().Count);
            SpellHandler.HandleEffectProc(replacement, target, replacementEffect);
            var registered = Assert.Single(target.CreateProcRegistrationSnapshot());

            EndEffect(original, target, originalEffect);

            Assert.Equal(registered, Assert.Single(target.CreateProcRegistrationSnapshot()));
            Assert.Empty(originalCaster.GetInvocations(nameof(IWorldEntity.EnqueueToVisible)));

            EndEffect(replacement, target, replacementEffect);
            EndEffect(replacement, target, replacementEffect);

            Assert.Empty(target.CreateProcRegistrationSnapshot());
            Assert.IsType<ServerSpellBuffRemove>(
                Assert.Single(replacementCaster.GetInvocations(nameof(IWorldEntity.EnqueueToVisible))).Arguments[0]);
        }
        finally { SpellHandler.InitialiseDependencyResolver(previous); }
    }

    private static SpellTargetInfo.SpellTargetEffectInfo Effect(uint triggeredSpell) => new(71u, new Spell4EffectsEntry
    {
        Id = 141788, SpellId = 59126, EffectType = SpellEffectType.Proc, TargetFlags = 1, Flags = 2,
        DataBits00 = ProcTriggerEventCandidate.KillTarget, DataBits01 = triggeredSpell,
        DataBits02 = BitConverter.SingleToUInt32Bits(1f), DataBits03 = 9, DataBits04 = 1000, DataBits05 = 1
    });

    private static RuntimeSpell CreateSpell(out RecordingDispatchProxy<IUnitEntity> casterProxy)
    {
        var caster = RecordingDispatchProxy<IUnitEntity>.Create(out casterProxy);
        var spellInfo = RecordingDispatchProxy<ISpellInfo>.Create(out var info);
        var baseInfo = RecordingDispatchProxy<ISpellBaseInfo>.Create(out var baseProxy);
        baseProxy.SetProperty(nameof(ISpellBaseInfo.HasIcon), true);
        info.SetProperty(nameof(ISpellInfo.BaseInfo), baseInfo);
        info.SetProperty(nameof(ISpellInfo.Entry), new Spell4Entry { Id = 59126 });
        var spell = (RuntimeSpell)RuntimeHelpers.GetUninitializedObject(typeof(RuntimeSpell));
        const BindingFlags fields = BindingFlags.NonPublic | BindingFlags.Instance;
        typeof(RuntimeSpell).GetField("<Caster>k__BackingField", fields)!.SetValue(spell, caster);
        typeof(RuntimeSpell).GetField("<Parameters>k__BackingField", fields)!.SetValue(spell, new SpellParameters { SpellInfo = spellInfo });
        typeof(RuntimeSpell).GetField("<CastingId>k__BackingField", fields)!.SetValue(spell, 17u);
        return spell;
    }

    private static void EndEffect(RuntimeSpell spell, IUnitEntity target, SpellTargetInfo.SpellTargetEffectInfo effect)
    {
        var method = typeof(RuntimeSpell).GetMethod("TryRemoveActiveEffect", BindingFlags.NonPublic | BindingFlags.Instance,
            [typeof(SpellEffectInterpretation), typeof(IUnitEntity), typeof(ISpellTargetEffectInfo)])!;
        method.Invoke(spell, [effect.Interpretation, target, effect]);
    }
}
