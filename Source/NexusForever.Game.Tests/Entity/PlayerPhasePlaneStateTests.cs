using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Tests.Entity;

// Real Player state behind the ChangePhase, ChangePlane and ChangeDisplayName effects (Decomp/Analysis/SPELL_EFFECT_CHANGEPHASE_20261108.md, SPELL_EFFECT_CHANGEPLANE_20261108.md,
// SPELL_EFFECT_CHANGEDISPLAYNAME_20261108.md): the phase and plane words, the packets sent when they change, and the generic spell-state removal that must undo them.
public class PlayerPhasePlaneStateTests
{
    private const uint Spell = 5000u;
    private const uint OtherSpell = 5001u;

    [Fact]
    public void ANewPlayerPerceivesOnlyPhaseZeroAndNoPlanes()
    {
        Player player = CreatePlayer(out _);

        Assert.Equal(1u, player.PhasesIPerceive);
        Assert.Equal(1u, player.PhasesThatPerceiveMe);
        Assert.Equal(0u, player.PlanesCanSee);
        Assert.Equal(0u, player.PlanesCanBeSeen);
    }

    [Fact]
    public void APhaseChangeUpdatesBothWordsAndSendsThePacketOnce()
    {
        Player player = CreatePlayer(out var session);

        player.AddPhaseChange(new PhaseChange(1u, Spell, 9u, 2u, 4u, 0u, 0u));
        player.AddPhaseChange(new PhaseChange(1u, Spell, 10u, 2u, 4u, 0u, 0u)); // the same effect instance replaces its record; nothing changes

        Assert.Equal(3u, player.PhasesIPerceive);
        Assert.Equal(5u, player.PhasesThatPerceiveMe);
        var packet = Assert.Single(Packets<ServerPhaseVisibilityWorldLocation>(session));
        Assert.Equal(3u, packet.PhasesIPerceive);
        Assert.Equal(5u, packet.PhasesThatPerceiveMe);
        Assert.Single(player.GetPhaseChanges());
    }

    [Fact]
    public void RemovingAPhaseChangeRestoresTheDefaultAndSendsThePacket()
    {
        Player player = CreatePlayer(out var session);
        player.AddPhaseChange(new PhaseChange(1u, Spell, 9u, 2u, 2u, 0u, 0u));

        Assert.True(player.RemovePhaseChange(1u));
        Assert.False(player.RemovePhaseChange(1u));

        Assert.Equal(1u, player.PhasesIPerceive);
        var packets = Packets<ServerPhaseVisibilityWorldLocation>(session);
        Assert.Equal(2, packets.Count);
        Assert.Equal(1u, packets[1].PhasesIPerceive);
    }

    [Fact]
    public void APlaneChangeUpdatesTheWordsAndSendsThePacketOnlyWhenTheyChange()
    {
        Player player = CreatePlayer(out var session);

        player.AddPlaneChange(new PlaneChange(1u, Spell, 9u, 4u, 4u));
        player.AddPlaneChange(new PlaneChange(2u, OtherSpell, 10u, 4u, 4u)); // a second change with the same bits changes nothing

        Assert.Equal(4u, player.PlanesCanSee);
        Assert.Equal(4u, player.PlanesCanBeSeen);
        var packet = Assert.Single(Packets<ServerPlaneVisibilitySpellSlingerVoid>(session));
        Assert.Equal(4u, packet.PlanesCanSee);

        Assert.True(player.RemovePlaneChange(1u)); // effect 2 still holds the bit
        Assert.Equal(4u, player.PlanesCanSee);
        Assert.True(player.RemovePlaneChange(2u));
        Assert.Equal(0u, player.PlanesCanSee);
        Assert.Equal(2, Packets<ServerPlaneVisibilitySpellSlingerVoid>(session).Count);
    }

    [Fact]
    public void GenericSpellStateRemovalUndoesPhasePlaneAndNameChangesOfTheSpellOnly()
    {
        Player player = CreatePlayer(out var session);
        player.AddPhaseChange(new PhaseChange(1u, Spell, 9u, 2u, 2u, 0u, 0u));
        player.AddPhaseChange(new PhaseChange(2u, OtherSpell, 10u, 4u, 4u, 0u, 0u));
        player.AddPlaneChange(new PlaneChange(3u, Spell, 9u, 4u, 4u));
        player.AddDisplayName(4u, Spell, 9u, "Hacked Dominion Turret", temporary: true);
        player.AddDisplayName(5u, Spell, 9u, "Permanent name", temporary: false);

        var removals = player.RemoveTrackedSpellStates(id => id == Spell, uint.MaxValue);

        Assert.Contains(removals, r => r.Kind == SpellStateRemovalKind.PhaseChange && r.EffectId == 1u && r.Spell4Id == Spell && r.CastingId == 9u);
        Assert.Contains(removals, r => r.Kind == SpellStateRemovalKind.PlaneChange && r.EffectId == 3u);
        Assert.Contains(removals, r => r.Kind == SpellStateRemovalKind.DisplayName && r.EffectId == 4u);
        Assert.DoesNotContain(removals, r => r.EffectId == 2u || r.EffectId == 5u);
        Assert.Equal(5u, player.PhasesIPerceive);             // default 1 plus the other spell's bit 2 (value 4)
        Assert.Equal(0u, player.PlanesCanSee);
        Assert.Equal("Permanent name", player.DisplayNameOverride); // permanent names are never removed by spell-state removal
        Assert.Contains(Packets<ServerPlaneVisibilitySpellSlingerVoid>(session), p => p.PlanesCanSee == 0u);
    }

    [Fact]
    public void ARemovalThatMatchesNothingChangesNothing()
    {
        Player player = CreatePlayer(out var session);
        player.AddPhaseChange(new PhaseChange(1u, Spell, 9u, 2u, 2u, 0u, 0u));
        int packets = Packets<ServerPhaseVisibilityWorldLocation>(session).Count;

        var removals = player.RemoveTrackedSpellStates(id => id == 99999u, uint.MaxValue);

        Assert.Empty(removals);
        Assert.Equal(3u, player.PhasesIPerceive);
        Assert.Equal(packets, Packets<ServerPhaseVisibilityWorldLocation>(session).Count);
    }

    [Fact]
    public void ATimedNameIsRemovedByEffectIdAndRevealsThePreviousOne()
    {
        Player player = CreatePlayer(out _);
        player.AddDisplayName(1u, Spell, 9u, "Permanent name", temporary: false);
        player.AddDisplayName(2u, OtherSpell, 10u, "Timed name", temporary: true);

        Assert.Equal("Timed name", player.DisplayNameOverride);
        Assert.True(player.RemoveDisplayName(2u));
        Assert.Equal("Permanent name", player.DisplayNameOverride);
        Assert.False(player.RemoveDisplayName(2u));
    }

    private static List<T> Packets<T>(RecordingDispatchProxy<IGameSession> session) where T : class
        => session.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)).Select(i => i.Arguments[0]).OfType<T>().ToList();

    private static Player CreatePlayer(out RecordingDispatchProxy<IGameSession> session)
    {
        var player = new Player(RecordingDispatchProxy<IMovementManager>.Create(out _), null, null, null, null, null, null);
        var gameSession = RecordingDispatchProxy<IGameSession>.Create(out session);
        typeof(Player).GetProperty(nameof(Player.Session))!.SetValue(player, gameSession);
        return player;
    }
}
