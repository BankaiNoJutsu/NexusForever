using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Spell;
using NexusForever.Game.Static.Spell;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Tests.Spell;

public class RavelSignalHandlerDispatchTests
{
    private const uint SignalId = 27096u;

    [Fact]
    public void RavelSignalMode1_SendsTheSignalToTheTargetOnce()
    {
        IUnitEntity target = RecordingDispatchProxy<IUnitEntity>.Create(out var targetProxy);

        SpellHandler.HandleEffectRavelSignal(CreateSpell(), target, CreateEffectInfo(RavelSignalReceiverEvidenceBoundary.EntityScriptOnSignalMode, SignalId));

        var send = Assert.Single(targetProxy.GetInvocations(nameof(IWorldEntity.SendSignal)));
        Assert.Equal(SignalId, send.Arguments[0]);
    }

    [Fact]
    public void RavelSignalMode1_WorldEntityEntryPointDispatchesTheSame()
    {
        IWorldEntity target = RecordingDispatchProxy<IWorldEntity>.Create(out var targetProxy);

        SpellHandler.HandleEffectRavelSignalWorld(CreateSpell(), target, CreateEffectInfo(RavelSignalReceiverEvidenceBoundary.EntityScriptOnSignalMode, SignalId));

        Assert.Equal(SignalId, Assert.Single(targetProxy.GetInvocations(nameof(IWorldEntity.SendSignal))).Arguments[0]);
    }

    [Theory]
    [InlineData(0u)]
    [InlineData(2u)]
    [InlineData(99u)]
    public void UnsupportedRavelSignalModes_SendNothing(uint mode)
    {
        IUnitEntity target = RecordingDispatchProxy<IUnitEntity>.Create(out var targetProxy);

        SpellHandler.HandleEffectRavelSignal(CreateSpell(), target, CreateEffectInfo(mode, SignalId));

        Assert.Empty(targetProxy.GetInvocations(nameof(IWorldEntity.SendSignal)));
    }

    private static ISpell CreateSpell()
    {
        ISpellInfo spellInfo = RecordingDispatchProxy<ISpellInfo>.Create(out var spellInfoProxy);
        spellInfoProxy.SetProperty(nameof(ISpellInfo.Entry), new Spell4Entry { Id = 1u });
        ISpellParameters parameters = RecordingDispatchProxy<ISpellParameters>.Create(out var parametersProxy);
        parametersProxy.SetProperty(nameof(ISpellParameters.SpellInfo), spellInfo);
        ISpell spell = RecordingDispatchProxy<ISpell>.Create(out var spellProxy);
        spellProxy.SetProperty(nameof(ISpell.Parameters), parameters);
        spellProxy.SetProperty(nameof(ISpell.CastingId), 1u);
        return spell;
    }

    private static ISpellTargetEffectInfo CreateEffectInfo(uint mode, uint signalId)
    {
        ISpellTargetEffectInfo info = RecordingDispatchProxy<ISpellTargetEffectInfo>.Create(out var infoProxy);
        infoProxy.SetProperty(nameof(ISpellTargetEffectInfo.EffectId), 1u);
        infoProxy.SetProperty(nameof(ISpellTargetEffectInfo.Entry), new Spell4EffectsEntry
        {
            Id = 1u, SpellId = 1u, TargetFlags = 1u, EffectType = SpellEffectType.RavelSignal,
            DataBits00 = mode, DataBits01 = signalId
        });
        return info;
    }
}
