using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Housing;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Housing;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Tests.Housing;

public class ResidenceManagerTests
{
    [Theory]
    [InlineData(true, false, true, true)]
    [InlineData(true, true, true, false)]
    [InlineData(false, false, true, false)]
    [InlineData(true, false, false, false)]
    public void RecallHouseIsTaughtOnlyToKnownResidenceOwnersWithAvailableSpellData(bool ownsResidence, bool alreadyKnown, bool hasData, bool expected)
    {
        var f = new RecallFixture(ownsResidence, alreadyKnown, hasData);
        Assert.Equal(expected ? 1 : 0, f.Spells.GetInvocations(nameof(ISpellManager.AddSpell)).Count);
        if (expected) Assert.Equal(22919u, f.Spells.GetInvocations(nameof(ISpellManager.AddSpell))[0].Arguments[0]);
    }

    [Fact]
    public void CreatingResidenceTeachesRecallOnceAndPreservesUnlockGuard()
    {
        var f = new RecallFixture(false);
        Assert.Empty(f.Spells.GetInvocations(nameof(ISpellManager.AddSpell)));
        Assert.Same(f.Residence, f.Manager.GetOrCreateResidence());
        Assert.Same(f.Residence, f.Manager.GetOrCreateResidence());
        Assert.Single(f.Spells.GetInvocations(nameof(ISpellManager.AddSpell)));
        Assert.Single(f.Global.GetInvocations(nameof(IGlobalResidenceManager.CreateResidence)));

        var blocked = new RecallFixture(false);
        blocked.Player.SetProperty(nameof(IPlayer.Level), 1u);
        Assert.Throws<HousingException>(() => blocked.Manager.GetOrCreateResidence());
        Assert.Empty(blocked.Global.GetInvocations(nameof(IGlobalResidenceManager.CreateResidence)));
        Assert.Empty(blocked.Spells.GetInvocations(nameof(ISpellManager.AddSpell)));
    }

    [Fact]
    public void ForeignResidenceDoesNotTeachOwnersRecall()
    {
        var f = new RecallFixture(false);
        f.ResidenceProxy.SetProperty(nameof(IResidence.OwnerId), (ulong?)99ul);
        f.Manager.GetOrCreateResidence();
        Assert.Empty(f.Spells.GetInvocations(nameof(ISpellManager.AddSpell)));
    }

    [Fact]
    public void MissingTierDataDoesNotBreakResidenceInitialization()
    {
        var f = new RecallFixture(false);
        f.BaseInfo.SetMethodReturn(nameof(ISpellBaseInfo.GetSpellInfo), null);
        f.Manager.GetOrCreateResidence();
        Assert.Same(f.Residence, f.Manager.Residence);
        Assert.Empty(f.Spells.GetInvocations(nameof(ISpellManager.AddSpell)));
    }

    [Fact]
    public void SendHousingBasics_WithoutResidence_OnlySendsHousingBasics()
    {
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out var sessionProxy);
        IPlayer player = TestPlayerBuilder.Create()
            .WithCharacterId(0x0F0E0D0C0B0A0908ul)
            .WithSession(session)
            .Build();

        var manager = new ResidenceManager(player);
        manager.SendHousingBasics();

        IReadOnlyList<RecordingDispatchProxy<IGameSession>.Invocation> calls = sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted));
        RecordingDispatchProxy<IGameSession>.Invocation call = Assert.Single(calls);

        var packet = Assert.IsType<ServerHousingBasics>(call.Arguments[0]);
        Assert.Equal(0ul, packet.ResidenceId);
    }
    private sealed class RecallFixture
    {
        public ResidenceManager Manager { get; }
        public IResidence Residence { get; }
        public RecordingDispatchProxy<IResidence> ResidenceProxy { get; }
        public RecordingDispatchProxy<IPlayer> Player { get; }
        public RecordingDispatchProxy<ISpellManager> Spells { get; }
        public RecordingDispatchProxy<IGlobalResidenceManager> Global { get; }
        public RecordingDispatchProxy<ISpellBaseInfo> BaseInfo { get; }

        public RecallFixture(bool existing, bool known = false, bool hasData = true)
        {
            Residence = RecordingDispatchProxy<IResidence>.Create(out var rp);
            ResidenceProxy = rp;
            rp.SetProperty(nameof(IResidence.OwnerId), (ulong?)42ul);
            var global = RecordingDispatchProxy<IGlobalResidenceManager>.Create(out var gp);
            Global = gp;
            gp.SetMethodReturn(nameof(IGlobalResidenceManager.GetResidenceByOwner), existing ? Residence : null);
            gp.SetMethodReturn(nameof(IGlobalResidenceManager.CreateResidence), Residence);
            var spells = RecordingDispatchProxy<ISpellManager>.Create(out var sp);
            Spells = sp;
            var knownSpell = RecordingDispatchProxy<ICharacterSpell>.Create(out _);
            sp.SetMethodHandler(nameof(ISpellManager.GetSpell), _ => known ? knownSpell : null);
            sp.SetMethodHandler(nameof(ISpellManager.AddSpell), _ => { known = true; return null; });
            var baseInfo = RecordingDispatchProxy<ISpellBaseInfo>.Create(out var bp);
            BaseInfo = bp;
            bp.SetMethodReturn(nameof(ISpellBaseInfo.GetSpellInfo), RecordingDispatchProxy<ISpellInfo>.Create(out _));
            var globalSpells = RecordingDispatchProxy<IGlobalSpellManager>.Create(out var gsp);
            gsp.SetMethodReturn(nameof(IGlobalSpellManager.GetSpellBaseInfo), hasData ? baseInfo : null);
            var player = RecordingDispatchProxy<IPlayer>.Create(out var pp);
            Player = pp;
            pp.SetProperty(nameof(IPlayer.CharacterId), 42ul);
            pp.SetProperty(nameof(IPlayer.Level), 50u);
            pp.SetProperty(nameof(IPlayer.SpellManager), spells);
            Manager = new ResidenceManager(player, global, globalSpellManager: globalSpells);
            pp.SetProperty(nameof(IPlayer.ResidenceManager), Manager);
        }
    }
}
