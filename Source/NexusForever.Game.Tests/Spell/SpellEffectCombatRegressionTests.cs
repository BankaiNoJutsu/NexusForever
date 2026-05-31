using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Static.Spell;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Tests.Spell;

public class SpellEffectCombatRegressionTests
{
    [Fact]
    public void HandleEffectModifySpellCooldown_WithOnslaughtCategoryPayload_DoesNotResetCurrentSpellCooldown()
    {
        IPlayer player = CreatePlayer(out RecordingDispatchProxy<ISpellManager> spellManagerProxy, out _);
        ISpell spell = CreateSpell(46867u, player);
        ISpellTargetEffectInfo info = CreateModifySpellCooldownInfo(
            effectId: 173869u,
            spellId: 46867u,
            mode: 2u,
            targetSpell4Id: 66u,
            operation: 0u,
            dataFloat03: 0f);

        global::NexusForever.Game.Spell.SpellHandler.HandleEffectModifySpellCooldown(spell, player, info);

        Assert.Empty(spellManagerProxy.GetInvocations(nameof(ISpellManager.GetSpellCooldown)));
        Assert.Empty(spellManagerProxy.GetInvocations(nameof(ISpellManager.SetSpellCooldown)));
    }

    [Fact]
    public void HandleEffectProxy_WithRelentlessAddCellChain_CastsChildOnOriginalCaster()
    {
        IPlayer player = CreatePlayer(out _, out RecordingDispatchProxy<IPlayer> playerProxy);
        IUnitEntity target = RecordingDispatchProxy<IUnitEntity>.Create(out RecordingDispatchProxy<IUnitEntity> targetProxy);
        targetProxy.SetProperty(nameof(IUnitEntity.Guid), 170u);

        ISpell spell = CreateSpell(70033u, player);
        ISpellTargetEffectInfo info = CreateProxyInfo(
            effectId: 177057u,
            spellId: 70033u,
            proxySpell4Id: 53865u);

        global::NexusForever.Game.Spell.SpellHandler.HandleEffectProxy(spell, target, info);

        RecordingDispatchProxy<IPlayer>.Invocation cast = Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.CastSpell)));
        Assert.Equal(53865u, (uint)cast.Arguments[0]);
        Assert.Equal(42u, ((ISpellParameters)cast.Arguments[1]).PrimaryTargetId);
        Assert.Empty(targetProxy.GetInvocations(nameof(IUnitEntity.CastSpell)));
    }

    private static IPlayer CreatePlayer(
        out RecordingDispatchProxy<ISpellManager> spellManagerProxy,
        out RecordingDispatchProxy<IPlayer> playerProxy)
    {
        ISpellManager spellManager = RecordingDispatchProxy<ISpellManager>.Create(out spellManagerProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Guid), 42u);
        playerProxy.SetProperty(nameof(IPlayer.SpellManager), spellManager);
        return player;
    }

    private static ISpell CreateSpell(uint spell4Id, IUnitEntity caster)
    {
        ISpellInfo spellInfo = RecordingDispatchProxy<ISpellInfo>.Create(out RecordingDispatchProxy<ISpellInfo> spellInfoProxy);
        spellInfoProxy.SetProperty(nameof(ISpellInfo.Entry), new Spell4Entry { Id = spell4Id });

        ISpellParameters parameters = RecordingDispatchProxy<ISpellParameters>.Create(out RecordingDispatchProxy<ISpellParameters> parametersProxy);
        parametersProxy.SetProperty(nameof(ISpellParameters.SpellInfo), spellInfo);

        ISpell spell = RecordingDispatchProxy<ISpell>.Create(out RecordingDispatchProxy<ISpell> spellProxy);
        spellProxy.SetProperty(nameof(ISpell.Caster), caster);
        spellProxy.SetProperty(nameof(ISpell.CastingId), 7u);
        spellProxy.SetProperty(nameof(ISpell.Parameters), parameters);
        return spell;
    }

    private static ISpellTargetEffectInfo CreateModifySpellCooldownInfo(
        uint effectId,
        uint spellId,
        uint mode,
        uint targetSpell4Id,
        uint operation,
        float dataFloat03)
    {
        ISpellTargetEffectInfo info = RecordingDispatchProxy<ISpellTargetEffectInfo>.Create(out RecordingDispatchProxy<ISpellTargetEffectInfo> infoProxy);
        infoProxy.SetProperty(nameof(ISpellTargetEffectInfo.EffectId), effectId);
        infoProxy.SetProperty(nameof(ISpellTargetEffectInfo.Entry), new Spell4EffectsEntry
        {
            Id         = effectId,
            SpellId    = spellId,
            TargetFlags = 1u,
            EffectType  = SpellEffectType.ModifySpellCooldown,
            DataBits00  = mode,
            DataBits01  = targetSpell4Id,
            DataBits02  = operation,
            DataBits03  = BitConverter.SingleToUInt32Bits(dataFloat03)
        });
        return info;
    }

    private static ISpellTargetEffectInfo CreateProxyInfo(uint effectId, uint spellId, uint proxySpell4Id)
    {
        ISpellTargetEffectInfo info = RecordingDispatchProxy<ISpellTargetEffectInfo>.Create(out RecordingDispatchProxy<ISpellTargetEffectInfo> infoProxy);
        infoProxy.SetProperty(nameof(ISpellTargetEffectInfo.EffectId), effectId);
        infoProxy.SetProperty(nameof(ISpellTargetEffectInfo.Entry), new Spell4EffectsEntry
        {
            Id         = effectId,
            SpellId    = spellId,
            TargetFlags = 4u,
            EffectType  = SpellEffectType.Proxy,
            DataBits00  = proxySpell4Id
        });
        return info;
    }
}