using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Spell;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Network;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Static;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Spell;

namespace NexusForever.Game.Tests.Spell;

public sealed class ActionBarCastResolutionTests
{
    [Theory]
    [InlineData(false, false, 0u)] [InlineData(false, false, 0x12345678u)]
    [InlineData(false, true, 0u)] [InlineData(false, true, 0x12345678u)]
    [InlineData(true, false, 0u)] [InlineData(true, false, 0x12345678u)]
    [InlineData(true, true, 0u)] [InlineData(true, true, 0x12345678u)]
    public void StaleActionBarRequestReceivesItsCorrelatedRefusal(bool position, bool missingSpellbookEntry, uint token)
    {
        var f = new Fixture();
        if (!missingSpellbookEntry) f.Inventory.SetMethodReturn(nameof(IInventory.GetItem), null);
        Exception error = Record.Exception(() => f.Request(position, token));
        Assert.Null(error);
        var reply = Assert.Single(f.Replies);
        Assert.Equal(token, reply.ContextToken); Assert.Equal(0u, reply.Spell4Id);
        Assert.Equal(CastResult.SpellUnknown, reply.CastResult);
        Assert.Empty(f.Player.GetInvocations(nameof(IUnitEntity.TryCastSpell)));
    }

    [Theory]
    [InlineData(false, 0u)] [InlineData(false, uint.MaxValue)]
    [InlineData(true, 0u)] [InlineData(true, uint.MaxValue)]
    public void AvailableActionBarRequestKeepsItsResolvedTierTargetAndToken(bool position, uint token)
    {
        var f = new Fixture(); var character = f.AvailableSpell();
        f.Request(position, token);
        var cast = Assert.Single(f.Player.GetInvocations(nameof(IUnitEntity.TryCastSpell)));
        Assert.Equal(301u, cast.Arguments[0]);
        var parameters = Assert.IsType<SpellParameters>(cast.Arguments[1]);
        Assert.Equal(token, parameters.ClientContextToken); Assert.True(parameters.HasClientCastRequest);
        Assert.True(parameters.UserInitiatedSpellCast); Assert.Equal(300u, parameters.RootSpellInfo.Entry.Id);
        Assert.Equal(position ? nameof(ClientCastSpellPosition) : nameof(ClientCastSpell), parameters.ClientRequestSource);
        Assert.Equal(position ? 0u : 77u, parameters.PrimaryTargetId);
        if (position) Assert.Equal(new System.Numerics.Vector3(1, 2, 3), parameters.Position.Vector);
        else Assert.Null(parameters.Position);
        Assert.Equal(CastResult.Ok, Assert.Single(character.GetInvocations(nameof(ICharacterSpell.CompleteSpellInfoCast))).Arguments[1]);
        Assert.Empty(f.Replies);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void LoadingPlayerDoesNotReceiveAStaleSlotReply(bool position)
    {
        var f = new Fixture(); f.Player.SetProperty(nameof(IPlayer.IsLoading), true);
        f.Request(position, 0); Assert.Empty(f.Replies);
        Assert.Empty(f.Player.GetInvocations(nameof(IUnitEntity.TryCastSpell)));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void PlayerReplacedDuringSlotLookupDoesNotInheritTheRequest(bool position)
    {
        var f = new Fixture();
        var character = f.AvailableSpell();
        IPlayer replacement = RecordingDispatchProxy<IPlayer>.Create(out var replacementProxy);
        f.Spells.SetMethodHandler(nameof(ISpellManager.GetSpell), args =>
        {
            f.Connection.SetProperty(nameof(IWorldSession.Player), replacement);
            return (ICharacterSpell)(object)character;
        });
        f.Request(position, 77); Assert.Empty(f.Replies);
        Assert.Empty(f.Player.GetInvocations(nameof(IUnitEntity.TryCastSpell)));
        Assert.Empty(replacementProxy.GetInvocations(nameof(IUnitEntity.TryCastSpell)));
    }

    [Fact]
    public void ReplacedSessionDoesNotReceiveTheOlderSlotReply()
    {
        var f = new Fixture(); var replacement = RecordingDispatchProxy<IWorldSession>.Create(out var messages);
        f.Player.SetProperty(nameof(IPlayer.Session), replacement);
        f.Request(false, 77); Assert.Empty(f.Replies);
        Assert.Empty(messages.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void RequestAfterCharacterRemovalHasNoCastOrReply(bool position)
    {
        var f = new Fixture(); f.Connection.SetProperty(nameof(IWorldSession.Player), null);
        f.Request(position, 77); Assert.Empty(f.Replies);
        Assert.Empty(f.Player.GetInvocations(nameof(IUnitEntity.TryCastSpell)));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void DownstreamRefusalIsNotAnsweredTwice(bool position)
    {
        var f = new Fixture(); var character = f.AvailableSpell();
        f.Player.SetMethodHandler(nameof(IUnitEntity.TryCastSpell), args =>
        {
            var parameters = Assert.IsType<SpellParameters>(args[1]);
            f.Session.EnqueueMessageEncrypted(new ServerSpellCastResult { ContextToken = parameters.ClientContextToken,
                Spell4Id = (uint)args[0], CastResult = CastResult.SpellCooldown });
            return CastResult.SpellCooldown;
        });
        f.Request(position, 77);
        var reply = Assert.Single(f.Replies); Assert.Equal(77u, reply.ContextToken); Assert.Equal(301u, reply.Spell4Id);
        Assert.Equal(CastResult.SpellCooldown, Assert.Single(character.GetInvocations(nameof(ICharacterSpell.CompleteSpellInfoCast))).Arguments[1]);
    }

    private sealed class Fixture
    {
        internal readonly IWorldSession Session;
        internal readonly RecordingDispatchProxy<IWorldSession> Connection;
        internal readonly RecordingDispatchProxy<IPlayer> Player;
        internal readonly RecordingDispatchProxy<IInventory> Inventory;
        internal readonly RecordingDispatchProxy<ISpellManager> Spells;
        internal IEnumerable<ServerSpellCastResult> Replies => Connection.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(row => row.Arguments[0]).OfType<ServerSpellCastResult>();

        internal Fixture()
        {
            Session = RecordingDispatchProxy<IWorldSession>.Create(out Connection);
            IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out Player);
            IInventory inventory = RecordingDispatchProxy<IInventory>.Create(out Inventory);
            ISpellManager spells = RecordingDispatchProxy<ISpellManager>.Create(out Spells);
            IItem item = RecordingDispatchProxy<IItem>.Create(out var itemProxy); itemProxy.SetProperty(nameof(IItem.Id), 100u);
            Connection.SetProperty(nameof(IWorldSession.Player), player);
            Player.SetProperty(nameof(IPlayer.Session), Session); Player.SetProperty(nameof(IPlayer.Inventory), inventory);
            Player.SetProperty(nameof(IPlayer.SpellManager), spells); Inventory.SetMethodReturn(nameof(IInventory.GetItem), item);
            Player.SetMethodReturn(nameof(IUnitEntity.TryCastSpell), CastResult.Ok);
        }

        internal RecordingDispatchProxy<ICharacterSpell> AvailableSpell()
        {
            ICharacterSpell character = RecordingDispatchProxy<ICharacterSpell>.Create(out var spell);
            ISpellInfo root = RecordingDispatchProxy<ISpellInfo>.Create(out var rootInfo);
            ISpellInfo resolved = RecordingDispatchProxy<ISpellInfo>.Create(out var resolvedInfo);
            rootInfo.SetProperty(nameof(ISpellInfo.Entry), new Spell4Entry { Id = 300 });
            resolvedInfo.SetProperty(nameof(ISpellInfo.Entry), new Spell4Entry { Id = 301 });
            spell.SetProperty(nameof(ICharacterSpell.SpellInfo), root);
            spell.SetMethodReturn(nameof(ICharacterSpell.GetSpellInfoForCast), resolved);
            Spells.SetMethodReturn(nameof(ISpellManager.GetSpell), character);
            return spell;
        }

        internal void Request(bool position, uint token)
        {
            using var bytes = new MemoryStream();
            using (var writer = new GamePacketWriter(bytes))
            {
                writer.Write(token); writer.Write((ushort)4);
                if (position) { writer.Write(1f); writer.Write(2f); writer.Write(3f); }
                else { writer.Write(77u); writer.Write(true); }
                writer.FlushBits();
            }
            using var input = new MemoryStream(bytes.ToArray()); using var reader = new GamePacketReader(input);
            if (position)
            {
                var request = new ClientCastSpellPosition(); request.Read(reader);
                new ClientCastSpellPositionHandler().HandleMessage(Session, request);
            }
            else
            {
                var request = new ClientCastSpell(); request.Read(reader);
                new ClientCastSpellHandler().HandleMessage(Session, request);
            }
        }
    }
}
