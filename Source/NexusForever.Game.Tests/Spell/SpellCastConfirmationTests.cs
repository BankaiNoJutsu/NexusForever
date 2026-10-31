using System.Numerics;
using System.Reflection;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Spell;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.Session;
using NexusForever.Network.World.Entity;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Static;
using NexusForever.Script;
using NexusForever.Script.Template.Collection;
using NexusForever.WorldServer.Network;

namespace NexusForever.Game.Tests.Spell;

public sealed class SpellCastConfirmationTests
{
    [Theory]
    [InlineData(0u, false)]
    [InlineData(0x12345678u, true)]
    public void RealCastAcknowledgesParsedRequestIncludingZeroBeforeVisibleStart(uint token, bool userInitiated)
    {
        var f = new Fixture();
        f.Parameters.ClientContextToken = token;
        f.Parameters.UserInitiatedSpellCast = userInitiated;
        Assert.Equal(CastResult.Ok, f.Spell.Cast());
        var reply = Assert.Single(f.OwnerMessages.OfType<ServerSpellCastPositionSync>());
        var start = Assert.Single(f.VisibleMessages.OfType<ServerSpellStart>());
        Assert.Equal(token, reply.ContextToken);
        Assert.Equal(77u, reply.CastingId);
        Assert.Equal(f.Target.Guid, reply.PrimaryTargetId);
        Assert.Equal(start.FieldPosition.Vector, reply.Position.Vector);
        Assert.Equal(start.Yaw, reply.Yaw);
        Assert.Equal(start.InitialPositionData.Single().Position.Vector, reply.InitialPositionData.Single().Position.Vector);
        Assert.Equal(start.InitialPositionData.Single().Yaw, reply.InitialPositionData.Single().Yaw);
        Assert.Empty(f.VisibleMessages.OfType<ServerSpellCastPositionSync>());
        Assert.Equal(new[] { "global-cooldown", "confirm", "visible-start" }, f.Order);
        Assert.Empty(f.Manager.GetInvocations(nameof(ISpellManager.SetSpellCooldown)));
        Assert.Throws<InvalidOperationException>(() => f.Spell.Cast());
        Assert.Single(f.OwnerMessages.OfType<ServerSpellCastPositionSync>());
    }

    [Theory]
    [InlineData("no-request")]
    [InlineData("server")]
    [InlineData("child")]
    [InlineData("loading")]
    public void CastsWithoutAnOwnedRootRequestNeverAcknowledgeRetainedDiagnosticTokens(string scenario)
    {
        var f = new Fixture();
        if (scenario is "no-request" or "server") f.Parameters.HasClientCastRequest = false;
        if (scenario == "server") f.Parameters.UserInitiatedSpellCast = false;
        if (scenario == "child") f.Parameters.ParentSpellInfo = f.Parameters.SpellInfo;
        if (scenario == "loading") f.Player.SetProperty(nameof(IPlayer.IsLoading), true);
        Assert.Equal(CastResult.Ok, f.Spell.Cast());
        Assert.Empty(f.OwnerMessages.OfType<ServerSpellCastPositionSync>());
        Assert.Single(f.VisibleMessages.OfType<ServerSpellStart>());
    }

    [Fact]
    public void RejectedCastKeepsFailureCorrelationAndDoesNotAcknowledgeOrStartCooldown()
    {
        var f = new Fixture();
        f.Manager.SetMethodReturn(nameof(ISpellManager.GetSpellCooldown), 2d);
        Assert.Equal(CastResult.SpellCooldown, f.Spell.Cast());
        Assert.Empty(f.OwnerMessages.OfType<ServerSpellCastPositionSync>());
        var failure = Assert.Single(f.OwnerMessages.OfType<ServerSpellCastResult>());
        Assert.Equal(f.Parameters.ClientContextToken, failure.ContextToken);
        Assert.Equal(CastResult.SpellCooldown, failure.CastResult);
        Assert.Empty(f.VisibleMessages);
        Assert.Empty(f.Manager.GetInvocations(nameof(ISpellManager.SetGlobalSpellCooldown)));
    }

    [Fact]
    public void ConfirmationPreservesPreparedTelegraphRowsAndCasterAnchoring()
    {
        var f = new Fixture();
        f.Info.SetProperty(nameof(ISpellInfo.Telegraphs), new List<TelegraphDamageEntry>
        {
            new() { Id = 51, DamageShapeEnum = 0, Param00 = 5 }
        });
        typeof(Game.Spell.Spell).GetMethod("InitialiseTelegraphs", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(f.Spell, null);
        Assert.Equal(CastResult.Ok, f.Spell.Cast());
        var start = Assert.Single(f.VisibleMessages.OfType<ServerSpellStart>());
        var reply = Assert.Single(f.OwnerMessages.OfType<ServerSpellCastPositionSync>());
        var original = Assert.Single(start.TelegraphPositionData);
        var copied = Assert.Single(reply.TelegraphPositionData);
        Assert.Equal(original.TelegraphId, copied.TelegraphId);
        Assert.Equal(original.AttachedUnitId, copied.AttachedUnitId);
        Assert.Equal(original.TargetFlags, copied.TargetFlags);
        Assert.Equal(original.Position.Vector, copied.Position.Vector);
        Assert.Equal(original.Yaw, copied.Yaw);
        Assert.Equal(original.Pitch, copied.Pitch);
    }

    [Fact]
    public void ConfirmationDoesNotMovePersonalCooldownFromExecutionToAcceptance()
    {
        var f = new Fixture();
        Assert.Equal(CastResult.Ok, f.Spell.Cast());
        Assert.Empty(f.Manager.GetInvocations(nameof(ISpellManager.SetSpellCooldown)));
        f.Spell.Update(1.1);
        var cooldown = Assert.Single(f.Manager.GetInvocations(nameof(ISpellManager.SetSpellCooldown)));
        Assert.Equal(123u, cooldown.Arguments[0]);
        Assert.Equal(3d, cooldown.Arguments[1]);
        Assert.Single(f.OwnerMessages.OfType<ServerSpellCastPositionSync>());
    }

    [Fact]
    public void RealClientHandlerPropagatesZeroTokenPresenceThroughRealSpellStart()
    {
        var f = new Fixture();
        var session = RecordingDispatchProxy<IWorldSession>.Create(out var sessionProxy);
        sessionProxy.SetProperty(nameof(IWorldSession.Player), f.Spell.Caster);
        var character = RecordingDispatchProxy<ICharacterSpell>.Create(out var characterProxy);
        characterProxy.SetProperty(nameof(ICharacterSpell.SpellInfo), f.Parameters.SpellInfo);
        characterProxy.SetMethodReturn(nameof(ICharacterSpell.GetSpellInfoForCast), f.Parameters.SpellInfo);
        SpellParameters request = null;
        f.Player.SetMethodHandler(nameof(IPlayer.TryCastSpell), args =>
        {
            request = Assert.IsType<SpellParameters>(args[1]);
            return NewSpell(f.Spell.Caster, request).Cast();
        });
        WorldServer.Network.Message.Handler.Spell.ClientCastSpellHandler.CastCharacterSpell(session, character,
            f.Target.Guid, clientContextToken: 0, clientRequestSource: nameof(ClientCastSpell));
        Assert.NotNull(request);
        Assert.True(request.HasClientCastRequest);
        Assert.Equal(0u, Assert.Single(f.OwnerMessages.OfType<ServerSpellCastPositionSync>()).ContextToken);
        Assert.Single(f.VisibleMessages.OfType<ServerSpellStart>());
    }

    [Fact]
    public void NonPlayerCastNeverBroadcastsAClientTokenConfirmation()
    {
        var f = new Fixture();
        var npc = RecordingDispatchProxy<IUnitEntity>.Create(out var proxy);
        proxy.SetProperty(nameof(IUnitEntity.Guid), 300u);
        proxy.SetProperty(nameof(IUnitEntity.IsAlive), true);
        var visible = new List<object>();
        proxy.SetMethodHandler(nameof(IUnitEntity.EnqueueToVisible), args => { visible.Add(args[0]); return null; });
        f.Parameters.PrimaryTargetId = 0;
        Assert.Equal(CastResult.Ok, NewSpell(npc, f.Parameters).Cast());
        Assert.Single(visible.OfType<ServerSpellStart>());
        Assert.Empty(visible.OfType<ServerSpellCastPositionSync>());
    }

    [Fact]
    public void NativeLayoutAndLegacyRawAliasesAreByteCompatible()
    {
        var raw = new ServerSpellCastPositionSync { Time = 0x01020304, CastingId = 0x11121314,
            CasterId = 0x21222324, Position = new Position(new Vector3(1, 2, 3)), Unknown16 = 0xBFA00000 };
        var typed = new ServerSpellCastPositionSync { ContextToken = raw.Time, CastingId = raw.CastingId,
            PrimaryTargetId = raw.CasterId, Position = raw.Position, Yaw = -1.25f };
        Assert.Equal(raw.Time, typed.Time);
        Assert.Equal(raw.CasterId, typed.CasterId);
        Assert.Equal(raw.Unknown16, typed.Unknown16);
        Assert.Equal(Write(raw), Write(typed));
        Assert.Equal(30, Write(typed).Length); // 7 DWORDs and two byte counts.
        using var reader = new GamePacketReader(new MemoryStream(Write(typed)));
        Assert.Equal(raw.Time, reader.ReadUInt());
        Assert.Equal(raw.CastingId, reader.ReadUInt());
        Assert.Equal(raw.CasterId, reader.ReadUInt());
        Assert.Equal(1f, reader.ReadSingle()); Assert.Equal(2f, reader.ReadSingle()); Assert.Equal(3f, reader.ReadSingle());
        Assert.Equal(-1.25f, reader.ReadSingle());
        Assert.Equal(0u, reader.ReadUInt(8)); Assert.Equal(0u, reader.ReadUInt(8));
        var unchanged = new ServerSpellCastPositionSync();
        Assert.Equal(0u, unchanged.Time); Assert.Equal(0u, unchanged.Unknown16);
    }

    private static byte[] Write(IWritable packet)
    {
        using var stream = new MemoryStream();
        using var writer = new GamePacketWriter(stream);
        packet.Write(writer); writer.FlushBits(); return stream.ToArray();
    }

    private static Game.Spell.Spell NewSpell(IUnitEntity caster, SpellParameters parameters)
    {
        var global = RecordingDispatchProxy<IGlobalSpellManager>.Create(out var globalProxy);
        globalProxy.SetProperty(nameof(IGlobalSpellManager.NextCastingId), 77u);
        var scripts = RecordingDispatchProxy<IScriptManager>.Create(out var scriptProxy);
        scriptProxy.SetMethodReturn(nameof(IScriptManager.InitialiseOwnedScripts), RecordingDispatchProxy<IScriptCollection>.Create(out _));
        return new Game.Spell.Spell(caster, parameters, globalSpellManager: global, scriptManager: scripts);
    }

    private sealed class Fixture
    {
        public RecordingDispatchProxy<IPlayer> Player { get; }
        public RecordingDispatchProxy<ISpellManager> Manager { get; }
        public RecordingDispatchProxy<ISpellInfo> Info { get; }
        public IUnitEntity Target { get; }
        public SpellParameters Parameters { get; }
        public Game.Spell.Spell Spell { get; }
        public List<object> OwnerMessages { get; } = [];
        public List<object> VisibleMessages { get; } = [];
        public List<string> Order { get; } = [];

        public Fixture()
        {
            var player = RecordingDispatchProxy<IPlayer>.Create(out var pp); Player = pp;
            var session = RecordingDispatchProxy<IGameSession>.Create(out var sessionProxy);
            sessionProxy.SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), args =>
            { OwnerMessages.Add(args[0]); if (args[0] is ServerSpellCastPositionSync) Order.Add("confirm"); return null; });
            var manager = RecordingDispatchProxy<ISpellManager>.Create(out var managerProxy); Manager = managerProxy;
            managerProxy.SetMethodHandler(nameof(ISpellManager.SetGlobalSpellCooldown), _ => { Order.Add("global-cooldown"); return null; });
            pp.SetProperty(nameof(IPlayer.Guid), 100u);
            pp.SetProperty(nameof(IPlayer.IsAlive), true);
            pp.SetProperty(nameof(IPlayer.Position), new Vector3(10, 20, 30));
            pp.SetProperty(nameof(IPlayer.Rotation), new Vector3(-1.25f, 0, 0));
            pp.SetProperty(nameof(IPlayer.Session), session);
            pp.SetProperty(nameof(IPlayer.SpellManager), manager);
            pp.SetMethodHandler(nameof(IPlayer.EnqueueToVisible), args =>
            { VisibleMessages.Add(args[0]); if (args[0] is ServerSpellStart) Order.Add("visible-start"); return null; });
            Target = RecordingDispatchProxy<IUnitEntity>.Create(out var targetProxy);
            targetProxy.SetProperty(nameof(IUnitEntity.Guid), 200u);
            targetProxy.SetProperty(nameof(IUnitEntity.IsAlive), true);
            targetProxy.SetProperty(nameof(IUnitEntity.Position), new Vector3(10, 20, 31));
            pp.SetMethodReturn(nameof(IPlayer.GetVisible), Target);
            var info = RecordingDispatchProxy<ISpellInfo>.Create(out var infoProxy); Info = infoProxy;
            var baseInfo = RecordingDispatchProxy<ISpellBaseInfo>.Create(out var baseProxy);
            baseProxy.SetProperty(nameof(ISpellBaseInfo.Entry), new Spell4BaseEntry { Id = 12 });
            infoProxy.SetProperty(nameof(ISpellInfo.BaseInfo), baseInfo);
            infoProxy.SetProperty(nameof(ISpellInfo.Entry), new Spell4Entry { Id = 123, Spell4BaseIdBaseSpell = 12,
                CastTime = 1000, SpellCoolDown = 3000 });
            infoProxy.SetProperty(nameof(ISpellInfo.GlobalCooldown), new SpellCoolDownEntry { Id = 7, CooldownTime = 1250 });
            infoProxy.SetProperty(nameof(ISpellInfo.Telegraphs), new List<TelegraphDamageEntry>());
            infoProxy.SetProperty(nameof(ISpellInfo.Effects), new List<Spell4EffectsEntry>());
            var global = RecordingDispatchProxy<IGlobalSpellManager>.Create(out var globalProxy);
            globalProxy.SetProperty(nameof(IGlobalSpellManager.NextCastingId), 77u);
            var scripts = RecordingDispatchProxy<IScriptManager>.Create(out var scriptProxy);
            scriptProxy.SetMethodReturn(nameof(IScriptManager.InitialiseOwnedScripts), RecordingDispatchProxy<IScriptCollection>.Create(out _));
            Parameters = new SpellParameters { SpellInfo = info, PrimaryTargetId = 200, HasClientCastRequest = true,
                ClientContextToken = 0x12345678, ClientRequestSource = nameof(ClientCastSpell), UserInitiatedSpellCast = true };
            Spell = new Game.Spell.Spell(player, Parameters, globalSpellManager: global, scriptManager: scripts);
        }
    }
}
