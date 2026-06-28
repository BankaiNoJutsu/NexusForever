using System.Numerics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Static;
using NexusForever.Script;
using NexusForever.Script.Template.Collection;

namespace NexusForever.Game.Tests.Spell;

public class SpellCastResultContextTokenTests
{
    private const uint Spell4Id = 98765u;
    private const uint Spell4BaseId = 987u;
    private const uint ClientContextToken = 0xAABBCCDDu;

    [Fact]
    public void CastFailure_EchoesClientContextTokenInCastResult()
    {
        IPlayer player = CreatePlayer(out RecordingDispatchProxy<IGameSession> sessionProxy);
        ISpellInfo spellInfo = CreateSpellInfo();

        var spell = new NexusForever.Game.Spell.Spell(
            player,
            new NexusForever.Game.Spell.SpellParameters
            {
                SpellInfo              = spellInfo,
                UserInitiatedSpellCast = true,
                ClientContextToken     = ClientContextToken,
                ClientRequestSource    = "test-context-token"
            },
            globalSpellManager: CreateGlobalSpellManager(),
            scriptManager: CreateScriptManager());

        Assert.Equal(CastResult.SpellCooldown, spell.Cast());

        ServerSpellCastResult result = GetSessionMessages<ServerSpellCastResult>(sessionProxy).Single();
        Assert.Equal(ClientContextToken, result.ContextToken);
        Assert.Equal(Spell4Id, result.Spell4Id);
        Assert.Equal(CastResult.SpellCooldown, result.CastResult);
    }

    private static IPlayer CreatePlayer(out RecordingDispatchProxy<IGameSession> sessionProxy)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out sessionProxy);
        ISpellManager spellManager = RecordingDispatchProxy<ISpellManager>.Create(out RecordingDispatchProxy<ISpellManager> spellManagerProxy);

        playerProxy.SetProperty(nameof(IPlayer.Guid), 100u);
        playerProxy.SetProperty(nameof(IPlayer.Position), Vector3.Zero);
        playerProxy.SetProperty(nameof(IPlayer.Rotation), Vector3.Zero);
        playerProxy.SetProperty(nameof(IPlayer.HitRadius), 1f);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);
        playerProxy.SetProperty(nameof(IPlayer.SpellManager), spellManager);
        playerProxy.SetProperty(nameof(IPlayer.IsAlive), true);
        spellManagerProxy.SetMethodReturn(nameof(ISpellManager.GetSpellCooldown), 1d);
        spellManagerProxy.SetMethodReturn(nameof(ISpellManager.GetGlobalSpellCooldown), 0d);
        return player;
    }

    private static ISpellInfo CreateSpellInfo()
    {
        ISpellBaseInfo baseInfo = RecordingDispatchProxy<ISpellBaseInfo>.Create(out RecordingDispatchProxy<ISpellBaseInfo> baseInfoProxy);
        ISpellInfo spellInfo = RecordingDispatchProxy<ISpellInfo>.Create(out RecordingDispatchProxy<ISpellInfo> spellProxy);

        baseInfoProxy.SetProperty(nameof(ISpellBaseInfo.Entry), new Spell4BaseEntry { Id = Spell4BaseId });
        spellProxy.SetProperty(nameof(ISpellInfo.Entry), new Spell4Entry
        {
            Id = Spell4Id,
            Spell4BaseIdBaseSpell = Spell4BaseId
        });
        spellProxy.SetProperty(nameof(ISpellInfo.BaseInfo), baseInfo);
        spellProxy.SetProperty(nameof(ISpellInfo.Telegraphs), new List<TelegraphDamageEntry>());
        spellProxy.SetProperty(nameof(ISpellInfo.Effects), new List<Spell4EffectsEntry>());
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

    private static IReadOnlyList<TMessage> GetSessionMessages<TMessage>(RecordingDispatchProxy<IGameSession> sessionProxy)
    {
        return sessionProxy
            .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(i => i.Arguments[0])
            .OfType<TMessage>()
            .ToList();
    }
}
