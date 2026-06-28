using System.Numerics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Static;
using NexusForever.Script;
using NexusForever.Script.Template.Collection;

namespace NexusForever.Game.Tests.Spell;

public class SpellGlobalCooldownEnumTests
{
    private const uint Spell4Id = 73001u;
    private const uint Spell4BaseId = 730u;
    private const uint GlobalCooldownId = 5u;
    private const uint GlobalCooldownMs = 1250u;

    [Fact]
    public void Cast_WithGlobalCooldownEnumOne_BlocksOnActiveGlobalCooldown()
    {
        IPlayer player = CreatePlayer(out _, out RecordingDispatchProxy<ISpellManager> spellManagerProxy, activeGlobalCooldown: 0.75d);
        ISpellInfo spellInfo = CreateSpellInfo(globalCooldownEnum: 1u);
        var spell = CreateSpell(player, spellInfo);

        CastResult result = spell.Cast();

        Assert.Equal(CastResult.SpellGlobalCooldown, result);
        Assert.Single(spellManagerProxy.GetInvocations(nameof(ISpellManager.GetGlobalSpellCooldown)));
        Assert.Empty(spellManagerProxy.GetInvocations(nameof(ISpellManager.SetGlobalSpellCooldown)));
    }

    [Fact]
    public void Cast_WithGlobalCooldownEnumOne_StartsGlobalCooldown()
    {
        IPlayer player = CreatePlayer(out _, out RecordingDispatchProxy<ISpellManager> spellManagerProxy, activeGlobalCooldown: 0d);
        ISpellInfo spellInfo = CreateSpellInfo(globalCooldownEnum: 1u);
        var spell = CreateSpell(player, spellInfo);

        CastResult result = spell.Cast();

        Assert.Equal(CastResult.Ok, result);
        RecordingDispatchProxy<ISpellManager>.Invocation cooldown =
            Assert.Single(spellManagerProxy.GetInvocations(nameof(ISpellManager.SetGlobalSpellCooldown)), i => i.Arguments.Length == 2);
        Assert.Equal(GlobalCooldownId, cooldown.Arguments[0]);
        Assert.Equal(GlobalCooldownMs / 1000d, cooldown.Arguments[1]);
    }

    [Theory]
    [InlineData(2u)]
    [InlineData(3u)]
    public void Cast_WithGlobalCooldownEnumTwoOrThree_DoesNotBlockOrStartGlobalCooldown(uint globalCooldownEnum)
    {
        IPlayer player = CreatePlayer(out _, out RecordingDispatchProxy<ISpellManager> spellManagerProxy, activeGlobalCooldown: 0.75d);
        ISpellInfo spellInfo = CreateSpellInfo(globalCooldownEnum);
        var spell = CreateSpell(player, spellInfo);

        CastResult result = spell.Cast();

        Assert.Equal(CastResult.Ok, result);
        Assert.Empty(spellManagerProxy.GetInvocations(nameof(ISpellManager.GetGlobalSpellCooldown)));
        Assert.Empty(spellManagerProxy.GetInvocations(nameof(ISpellManager.SetGlobalSpellCooldown)));
    }

    [Fact]
    public void Cast_WithIgnoredGlobalCooldown_DoesNotBlockOrStartGlobalCooldown()
    {
        IPlayer player = CreatePlayer(out _, out RecordingDispatchProxy<ISpellManager> spellManagerProxy, activeGlobalCooldown: 0.75d);
        ISpellInfo spellInfo = CreateSpellInfo(globalCooldownEnum: 0u);
        var spell = CreateSpell(player, spellInfo, ignoreGlobalCooldown: true);

        CastResult result = spell.Cast();

        Assert.Equal(CastResult.Ok, result);
        Assert.Empty(spellManagerProxy.GetInvocations(nameof(ISpellManager.GetGlobalSpellCooldown)));
        Assert.Empty(spellManagerProxy.GetInvocations(nameof(ISpellManager.SetGlobalSpellCooldown)));
    }

    private static NexusForever.Game.Spell.Spell CreateSpell(IPlayer player, ISpellInfo spellInfo, bool ignoreGlobalCooldown = false)
    {
        return new NexusForever.Game.Spell.Spell(
            player,
            new NexusForever.Game.Spell.SpellParameters
            {
                SpellInfo            = spellInfo,
                IgnoreGlobalCooldown = ignoreGlobalCooldown
            },
            globalSpellManager: CreateGlobalSpellManager(),
            scriptManager: CreateScriptManager());
    }

    private static IPlayer CreatePlayer(
        out RecordingDispatchProxy<IPlayer> playerProxy,
        out RecordingDispatchProxy<ISpellManager> spellManagerProxy,
        double activeGlobalCooldown)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out playerProxy);
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out _);
        ISpellManager spellManager = RecordingDispatchProxy<ISpellManager>.Create(out spellManagerProxy);

        playerProxy.SetProperty(nameof(IPlayer.Guid), 101u);
        playerProxy.SetProperty(nameof(IPlayer.Position), Vector3.Zero);
        playerProxy.SetProperty(nameof(IPlayer.Rotation), Vector3.Zero);
        playerProxy.SetProperty(nameof(IPlayer.HitRadius), 1f);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);
        playerProxy.SetProperty(nameof(IPlayer.SpellManager), spellManager);
        playerProxy.SetProperty(nameof(IPlayer.IsAlive), true);
        spellManagerProxy.SetMethodReturn(nameof(ISpellManager.GetSpellCooldown), 0d);
        spellManagerProxy.SetMethodReturn(nameof(ISpellManager.GetGlobalSpellCooldown), activeGlobalCooldown);
        return player;
    }

    private static ISpellInfo CreateSpellInfo(uint globalCooldownEnum)
    {
        ISpellBaseInfo baseInfo = RecordingDispatchProxy<ISpellBaseInfo>.Create(out RecordingDispatchProxy<ISpellBaseInfo> baseInfoProxy);
        ISpellInfo spellInfo = RecordingDispatchProxy<ISpellInfo>.Create(out RecordingDispatchProxy<ISpellInfo> spellInfoProxy);

        baseInfoProxy.SetProperty(nameof(ISpellBaseInfo.Entry), new Spell4BaseEntry { Id = Spell4BaseId });
        spellInfoProxy.SetProperty(nameof(ISpellInfo.Entry), new Spell4Entry
        {
            Id                    = Spell4Id,
            Spell4BaseIdBaseSpell = Spell4BaseId,
            SpellCoolDownIdGlobal = GlobalCooldownId,
            GlobalCooldownEnum    = globalCooldownEnum
        });
        spellInfoProxy.SetProperty(nameof(ISpellInfo.BaseInfo), baseInfo);
        spellInfoProxy.SetProperty(nameof(ISpellInfo.GlobalCooldown), new SpellCoolDownEntry
        {
            Id           = GlobalCooldownId,
            CooldownTime = GlobalCooldownMs
        });
        spellInfoProxy.SetProperty(nameof(ISpellInfo.Telegraphs), new List<TelegraphDamageEntry>());
        spellInfoProxy.SetProperty(nameof(ISpellInfo.Effects), new List<Spell4EffectsEntry>());
        return spellInfo;
    }

    private static IGlobalSpellManager CreateGlobalSpellManager()
    {
        IGlobalSpellManager globalSpellManager = RecordingDispatchProxy<IGlobalSpellManager>.Create(out RecordingDispatchProxy<IGlobalSpellManager> proxy);
        proxy.SetProperty(nameof(IGlobalSpellManager.NextCastingId), 1u);
        proxy.SetProperty(nameof(IGlobalSpellManager.NextEffectId), 1u);
        return globalSpellManager;
    }

    private static IScriptManager CreateScriptManager()
    {
        IScriptCollection scriptCollection = RecordingDispatchProxy<IScriptCollection>.Create(out _);
        IScriptManager scriptManager = RecordingDispatchProxy<IScriptManager>.Create(out RecordingDispatchProxy<IScriptManager> scriptManagerProxy);
        scriptManagerProxy.SetMethodReturn(nameof(IScriptManager.InitialiseOwnedScripts), scriptCollection);
        return scriptManager;
    }
}
