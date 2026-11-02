using System.Reflection;
using System.Runtime.CompilerServices;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Entity;
using NexusForever.Game.Spell;
using NexusForever.Game.Static;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.Game.Tests.Spell;

public sealed class EarlyClientCastRefusalTests
{
    [Theory]
    [InlineData("dead-parameters", 0u)] [InlineData("dead-parameters", 123u)]
    [InlineData("dead-id", 0u)] [InlineData("dead-id", 123u)]
    [InlineData("dead-base", 0u)] [InlineData("dead-base", 123u)]
    [InlineData("disabled-base", 0u)] [InlineData("disabled-base", 123u)]
    [InlineData("disabled-spell", 0u)] [InlineData("disabled-spell", 123u)]
    [InlineData("active", 0u)] [InlineData("active", 123u)]
    public void PreSpellRefusalsAnswerTheParsedRootTokenExactlyOnce(string scenario, uint token)
    {
        var (player, parameters, replies) = Fixture(scenario);
        parameters.ClientContextToken = token;
        if (scenario == "dead-id") Assert.Equal(CastResult.CasterCannotBeDead, player.TryCastSpell(222u, parameters));
        else if (scenario == "dead-base") player.CastSpell(111u, 1, parameters);
        else player.CastSpell(parameters);
        var reply = Assert.Single(replies);
        Assert.Equal(token, reply.ContextToken); Assert.Equal(222u, reply.Spell4Id);
        Assert.Equal(scenario.StartsWith("dead") ? CastResult.CasterCannotBeDead
            : scenario == "active" ? CastResult.SpellAlreadyCasting : CastResult.SpellRemoved, reply.CastResult);
    }

    [Theory]
    [InlineData("server")]
    [InlineData("child")]
    [InlineData("loading")]
    public void EarlyRefusalDoesNotPublishWithoutTheLiveRootRequest(string scenario)
    {
        var (player, parameters, replies) = Fixture("dead-parameters");
        parameters.ClientContextToken = 123;
        if (scenario == "server") parameters.HasClientCastRequest = false;
        if (scenario == "child") parameters.ParentSpellInfo = parameters.SpellInfo;
        if (scenario == "loading") typeof(Player).GetProperty(nameof(Player.IsLoading))!.SetValue(player, true);
        Assert.Equal(CastResult.CasterCannotBeDead, player.TryCastSpell(222u, parameters));
        Assert.Empty(replies);
    }

    [Fact]
    public void NullParametersRejectBeforeTheDeadCasterShortcut()
    {
        var (player, _, _) = Fixture("dead-parameters");
        Assert.Throws<ArgumentNullException>(() => player.CastSpell((ISpellParameters)null));
    }

    private static (RefusalPlayer Player, SpellParameters Parameters, List<ServerSpellCastResult> Replies) Fixture(string scenario)
    {
        var player = (RefusalPlayer)RuntimeHelpers.GetUninitializedObject(typeof(RefusalPlayer));
        player.SetHealth(scenario.StartsWith("dead") ? 0u : 1u);
        var replies = new List<ServerSpellCastResult>();
        var session = RecordingDispatchProxy<IGameSession>.Create(out var wire);
        wire.SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), args => { if (args[0] is ServerSpellCastResult result) replies.Add(result); return null; });
        typeof(Player).GetProperty(nameof(Player.Session))!.SetValue(player, session);
        var spells = new List<ISpell>();
        typeof(UnitEntity).GetField("pendingSpells", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(player, spells);
        if (scenario == "active")
        {
            var spell = RecordingDispatchProxy<ISpell>.Create(out var pending);
            pending.SetProperty(nameof(ISpell.BlocksCasting), true); spells.Add(spell);
        }
        var disables = RecordingDispatchProxy<IDisableManager>.Create(out var disabled);
        disabled.SetMethodHandler(nameof(IDisableManager.IsDisabled), args => scenario == "disabled-base"
            ? (DisableType)args[0] == DisableType.BaseSpell : scenario == "disabled-spell" && (DisableType)args[0] == DisableType.Spell);
        player.InitialiseRuntimeDependencies(null, null, disableManagerResolver: () => disables);
        var baseInfo = RecordingDispatchProxy<ISpellBaseInfo>.Create(out var baseProxy);
        baseProxy.SetProperty(nameof(ISpellBaseInfo.Entry), new Spell4BaseEntry { Id = 111 });
        var info = RecordingDispatchProxy<ISpellInfo>.Create(out var spellProxy);
        spellProxy.SetProperty(nameof(ISpellInfo.BaseInfo), baseInfo);
        spellProxy.SetProperty(nameof(ISpellInfo.Entry), new Spell4Entry { Id = 222 });
        return (player, new SpellParameters { SpellInfo = info, HasClientCastRequest = true, UserInitiatedSpellCast = false }, replies);
    }

    private sealed class RefusalPlayer : Player
    {
        private RefusalPlayer() : base(null, null, null, null, null, null, null) { }
        public override uint Health { get; protected set; }
        internal void SetHealth(uint health) => Health = health;
    }
}
