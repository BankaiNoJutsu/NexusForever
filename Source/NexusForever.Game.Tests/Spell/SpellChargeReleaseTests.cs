using System.Numerics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Static.Spell;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Configuration.Model;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Static;
using NexusForever.Script;
using NexusForever.Script.Template.Collection;

namespace NexusForever.Game.Tests.Spell;

[Collection(LegacyServiceProviderCollection.Name)]
public class SpellChargeReleaseTests
{
    private const uint CasterId = 100u;
    private const uint ParentSpell4Id = 34718u;
    private const uint FirstThresholdSpell4Id = 34720u;
    private const uint SecondThresholdSpell4Id = 34722u;
    private const uint BaseSpell4Id = 20684u;
    private const uint PrimaryTargetId = 100u;
    private const uint ClientContextToken = 444u;

    [Fact]
    public void Cast_WithChargeReleaseThreshold_StartsThresholdAndWaits()
    {
        using var _ = new LegacyServiceProviderScope(BuildProvider());
        IPlayer player = CreatePlayer(out RecordingDispatchProxy<IPlayer> playerProxy, out RecordingDispatchProxy<IGameSession> sessionProxy);
        ISpellInfo parentSpellInfo = CreateChargeReleaseSpellInfo(out ICharacterSpell characterSpell);

        var spell = new NexusForever.Game.Spell.Spell(player, new NexusForever.Game.Spell.SpellParameters
        {
            CharacterSpell         = characterSpell,
            SpellInfo              = parentSpellInfo,
            RootSpellInfo          = parentSpellInfo,
            PrimaryTargetId        = PrimaryTargetId,
            UserInitiatedSpellCast = true,
            ClientContextToken     = ClientContextToken,
            ClientRequestSource    = "test-charge"
        });

        Assert.Equal(CastResult.Ok, spell.Cast());

        Assert.True(spell.IsCasting);
        Assert.True(spell.BlocksCasting);
        Assert.False(spell.IsFinished);
        Assert.Contains(playerProxy.GetInvocations(nameof(IWorldEntity.EnqueueToVisible)), i => i.Arguments[0] is ServerSpellStart);
        Assert.Contains(playerProxy.GetInvocations(nameof(IWorldEntity.EnqueueToVisible)), i => i.Arguments[0] is ServerSpellGo);
        Assert.DoesNotContain(playerProxy.GetInvocations(nameof(IWorldEntity.EnqueueToVisible)), i => i.Arguments[0] is ServerSpellFinish);

        ServerSpellThresholdStart thresholdStart = Assert.Single(GetSessionMessages<ServerSpellThresholdStart>(sessionProxy));
        Assert.Equal(ParentSpell4Id, thresholdStart.Spell4Id);
        Assert.Equal(ParentSpell4Id, thresholdStart.RootSpell4Id);
        Assert.Equal(spell.CastingId, thresholdStart.CastingId);
        Assert.Empty(GetSessionMessages<ServerSpellThresholdClear>(sessionProxy));
    }

    [Fact]
    public void ReleaseCharge_AfterThresholdUpdate_CastsSelectedThresholdChild()
    {
        using var _ = new LegacyServiceProviderScope(BuildProvider());
        IPlayer player = CreatePlayer(
            out RecordingDispatchProxy<IPlayer> playerProxy,
            out RecordingDispatchProxy<IGameSession> sessionProxy,
            out RecordingDispatchProxy<ISpellManager> spellManagerProxy);
        ISpellInfo parentSpellInfo = CreateChargeReleaseSpellInfo(out ICharacterSpell characterSpell);

        var spell = new NexusForever.Game.Spell.Spell(player, new NexusForever.Game.Spell.SpellParameters
        {
            CharacterSpell         = characterSpell,
            SpellInfo              = parentSpellInfo,
            RootSpellInfo          = parentSpellInfo,
            PrimaryTargetId        = PrimaryTargetId,
            UserInitiatedSpellCast = true,
            ClientContextToken     = ClientContextToken,
            ClientRequestSource    = "test-charge"
        });

        Assert.Equal(CastResult.Ok, spell.Cast());
        spell.Update(1.1d);

        ServerSpellThresholdUpdate thresholdUpdate = Assert.Single(GetSessionMessages<ServerSpellThresholdUpdate>(sessionProxy));
        Assert.Equal(ParentSpell4Id, thresholdUpdate.Spell4Id);
        Assert.Equal(1, thresholdUpdate.Stage);

        Assert.True(spell.TryReleaseChargeSpell(characterSpell, ParentSpell4Id, PrimaryTargetId, ClientContextToken, "test-release"));

        RecordingDispatchProxy<IPlayer>.Invocation childCast =
            Assert.Single(playerProxy.GetInvocations(nameof(IUnitEntity.TryCastSpell)));
        Assert.Equal(SecondThresholdSpell4Id, childCast.Arguments[0]);
        NexusForever.Game.Spell.SpellParameters childParameters = Assert.IsType<NexusForever.Game.Spell.SpellParameters>(childCast.Arguments[1]);
        Assert.Same(parentSpellInfo, childParameters.ParentSpellInfo);
        Assert.Same(parentSpellInfo, childParameters.RootSpellInfo);
        Assert.Equal(PrimaryTargetId, childParameters.PrimaryTargetId);
        Assert.True(childParameters.IgnoreGlobalCooldown);
        Assert.Equal(ClientContextToken, childParameters.ClientContextToken);
        Assert.Equal("test-release", childParameters.ClientRequestSource);
        Assert.Null(childParameters.CharacterSpell);

        RecordingDispatchProxy<ISpellManager>.Invocation cooldown =
            Assert.Single(spellManagerProxy.GetInvocations(nameof(ISpellManager.SetSpellCooldown)));
        Assert.Equal(ParentSpell4Id, cooldown.Arguments[0]);
        Assert.Equal(10d, cooldown.Arguments[1]);

        Assert.True(spell.IsFinished);
        Assert.Single(GetSessionMessages<ServerSpellThresholdClear>(sessionProxy));
        Assert.Contains(playerProxy.GetInvocations(nameof(IWorldEntity.EnqueueToVisible)), i => i.Arguments[0] is ServerSpellFinish);
    }

    private static IServiceProvider BuildProvider()
    {
        IScriptCollection scriptCollection = RecordingDispatchProxy<IScriptCollection>.Create(out _);
        IScriptManager scriptManager = RecordingDispatchProxy<IScriptManager>.Create(out RecordingDispatchProxy<IScriptManager> scriptManagerProxy);
        scriptManagerProxy.SetMethodReturn(nameof(IScriptManager.InitialiseOwnedScripts), scriptCollection);

        return new ServiceCollection()
            .AddSingleton<NexusForever.Game.Spell.GlobalSpellManager>()
            .AddSingleton(new GameTableManager(Options.Create(new GameTableConfig())))
            .AddSingleton(scriptManager)
            .BuildServiceProvider();
    }

    private static IPlayer CreatePlayer(
        out RecordingDispatchProxy<IPlayer> playerProxy,
        out RecordingDispatchProxy<IGameSession> sessionProxy)
    {
        return CreatePlayer(out playerProxy, out sessionProxy, out _);
    }

    private static IPlayer CreatePlayer(
        out RecordingDispatchProxy<IPlayer> playerProxy,
        out RecordingDispatchProxy<IGameSession> sessionProxy,
        out RecordingDispatchProxy<ISpellManager> spellManagerProxy)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out playerProxy);
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out sessionProxy);
        ISpellManager spellManager = RecordingDispatchProxy<ISpellManager>.Create(out spellManagerProxy);

        playerProxy.SetProperty(nameof(IPlayer.Guid), CasterId);
        playerProxy.SetProperty(nameof(IPlayer.Position), Vector3.Zero);
        playerProxy.SetProperty(nameof(IPlayer.Rotation), Vector3.Zero);
        playerProxy.SetProperty(nameof(IPlayer.HitRadius), 1f);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);
        playerProxy.SetProperty(nameof(IPlayer.SpellManager), spellManager);
        playerProxy.SetProperty(nameof(IPlayer.IsAlive), true);
        spellManagerProxy.SetMethodReturn(nameof(ISpellManager.GetSpellCooldown), 0d);
        spellManagerProxy.SetMethodReturn(nameof(ISpellManager.GetGlobalSpellCooldown), 0d);
        playerProxy.SetMethodReturn(nameof(IUnitEntity.TryCastSpell), CastResult.Ok);
        return player;
    }

    private static ISpellInfo CreateChargeReleaseSpellInfo(out ICharacterSpell characterSpell)
    {
        characterSpell = RecordingDispatchProxy<ICharacterSpell>.Create(out _);
        ISpellBaseInfo baseInfo = RecordingDispatchProxy<ISpellBaseInfo>.Create(out RecordingDispatchProxy<ISpellBaseInfo> baseInfoProxy);
        ISpellInfo parentSpellInfo = RecordingDispatchProxy<ISpellInfo>.Create(out RecordingDispatchProxy<ISpellInfo> parentProxy);
        ISpellInfo firstChildSpellInfo = CreateThresholdChildSpellInfo(FirstThresholdSpell4Id);
        ISpellInfo secondChildSpellInfo = CreateThresholdChildSpellInfo(SecondThresholdSpell4Id);

        baseInfoProxy.SetProperty(nameof(ISpellBaseInfo.Entry), new Spell4BaseEntry { Id = BaseSpell4Id, CastMethod = (uint)SpellCastMethod.ChargeRelease });
        baseInfoProxy.SetProperty(nameof(ISpellBaseInfo.CastMethod), SpellCastMethod.ChargeRelease);

        var thresholds = new List<Spell4ThresholdsEntry>
        {
            new()
            {
                Spell4IdParent    = ParentSpell4Id,
                Spell4IdToCast    = FirstThresholdSpell4Id,
                OrderIndex        = 0u,
                ThresholdDuration = 0u
            },
            new()
            {
                Spell4IdParent    = ParentSpell4Id,
                Spell4IdToCast    = SecondThresholdSpell4Id,
                OrderIndex        = 1u,
                ThresholdDuration = 1000u
            }
        };

        parentProxy.SetProperty(nameof(ISpellInfo.Entry), new Spell4Entry
        {
            Id                   = ParentSpell4Id,
            Spell4BaseIdBaseSpell = BaseSpell4Id,
            SpellCoolDown        = 10000u
        });
        parentProxy.SetProperty(nameof(ISpellInfo.BaseInfo), baseInfo);
        parentProxy.SetProperty(nameof(ISpellInfo.Telegraphs), new List<TelegraphDamageEntry>());
        parentProxy.SetProperty(nameof(ISpellInfo.Effects), new List<Spell4EffectsEntry>());
        parentProxy.SetProperty(nameof(ISpellInfo.Thresholds), thresholds);
        parentProxy.SetMethodHandler(nameof(ISpellInfo.GetThresholdSpellInfo), args =>
        {
            uint thresholdIndex = (uint)args[0];
            args[1] = thresholdIndex == 0u ? thresholds[0] : thresholds[1];
            return thresholdIndex == 0u ? firstChildSpellInfo : secondChildSpellInfo;
        });

        return parentSpellInfo;
    }

    private static ISpellInfo CreateThresholdChildSpellInfo(uint spell4Id)
    {
        ISpellBaseInfo baseInfo = RecordingDispatchProxy<ISpellBaseInfo>.Create(out RecordingDispatchProxy<ISpellBaseInfo> baseInfoProxy);
        ISpellInfo spellInfo = RecordingDispatchProxy<ISpellInfo>.Create(out RecordingDispatchProxy<ISpellInfo> spellProxy);

        baseInfoProxy.SetProperty(nameof(ISpellBaseInfo.Entry), new Spell4BaseEntry { Id = spell4Id + 1000u, CastMethod = (uint)SpellCastMethod.ChargeRelease });
        baseInfoProxy.SetProperty(nameof(ISpellBaseInfo.CastMethod), SpellCastMethod.ChargeRelease);
        spellProxy.SetProperty(nameof(ISpellInfo.Entry), new Spell4Entry
        {
            Id                   = spell4Id,
            Spell4BaseIdBaseSpell = spell4Id + 1000u,
            TierIndex            = 1u
        });
        spellProxy.SetProperty(nameof(ISpellInfo.BaseInfo), baseInfo);
        spellProxy.SetProperty(nameof(ISpellInfo.Telegraphs), new List<TelegraphDamageEntry>());
        spellProxy.SetProperty(nameof(ISpellInfo.Effects), new List<Spell4EffectsEntry>());
        spellProxy.SetProperty(nameof(ISpellInfo.Thresholds), new List<Spell4ThresholdsEntry>());
        return spellInfo;
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
