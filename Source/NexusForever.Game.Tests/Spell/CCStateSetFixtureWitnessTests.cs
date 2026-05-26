using NexusForever.Game.Static.Combat.CrowdControl;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Spell;
using NexusForever.Game.Static.Spell;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Tests.Spell;

public class CCStateSetFixtureWitnessTests
{
    [Fact]
    public void HivemindTrap57355_MapsDataBits00ToTether()
    {
        Assert.Equal(CCState.Tether, (CCState)20u);
    }

    [Fact]
    public void StarterTutorialScan81662_DropsDisableCcState()
    {
        ISpell spell = CreateSpell(81662u);
        IUnitEntity target = RecordingDispatchProxy<IUnitEntity>.Create(out RecordingDispatchProxy<IUnitEntity> targetProxy);
        ISpellTargetEffectInfo info = CreateEffectInfo(
            effectType: SpellEffectType.CCStateSet,
            spellId: 81662u,
            durationTime: 2000u,
            dataBits00: (uint)CCState.Disable);

        SpellHandler.HandleEffectCCStateSet(spell, target, info);

        Assert.True(info.DropEffect);
        Assert.Empty(targetProxy.GetInvocations(nameof(IUnitEntity.AddCCState)));
    }

    private static ISpell CreateSpell(uint spell4Id)
    {
        ISpellInfo spellInfo = RecordingDispatchProxy<ISpellInfo>.Create(out RecordingDispatchProxy<ISpellInfo> spellInfoProxy);
        spellInfoProxy.SetProperty(nameof(ISpellInfo.Entry), new Spell4Entry { Id = spell4Id });

        ISpellParameters parameters = RecordingDispatchProxy<ISpellParameters>.Create(out RecordingDispatchProxy<ISpellParameters> parametersProxy);
        parametersProxy.SetProperty(nameof(ISpellParameters.SpellInfo), spellInfo);

        ISpell spell = RecordingDispatchProxy<ISpell>.Create(out RecordingDispatchProxy<ISpell> spellProxy);
        spellProxy.SetProperty(nameof(ISpell.Parameters), parameters);
        spellProxy.SetProperty(nameof(ISpell.CastingId), 123u);
        return spell;
    }

    private static ISpellTargetEffectInfo CreateEffectInfo(
        SpellEffectType effectType,
        uint spellId,
        uint durationTime,
        uint dataBits00)
    {
        ISpellTargetEffectInfo info = RecordingDispatchProxy<ISpellTargetEffectInfo>.Create(out RecordingDispatchProxy<ISpellTargetEffectInfo> infoProxy);
        infoProxy.SetProperty(nameof(ISpellTargetEffectInfo.EffectId), 456u);
        infoProxy.SetProperty(nameof(ISpellTargetEffectInfo.Entry), new Spell4EffectsEntry
        {
            Id           = 214758u,
            SpellId      = spellId,
            TargetFlags  = 1u,
            EffectType   = effectType,
            DurationTime = durationTime,
            DataBits00   = dataBits00
        });

        return info;
    }
}
