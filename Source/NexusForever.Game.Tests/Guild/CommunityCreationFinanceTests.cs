using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Guild;
using NexusForever.Game.Housing;
using NexusForever.Game.Static.Guild;
using NexusForever.Game.Static.Housing;
using NexusForever.Network.World.Message.Model.Guild;
using NexusForever.Game.Static.Entity;
using NexusForever.Network.Session;
using NexusForever.WorldServer.Network;

namespace NexusForever.Game.Tests.Guild;

public sealed partial class GuildCreationFinanceTests
{
    [Fact]
    public async Task CommunityCreationKeepsTheDraftAndResidencePrivateBeforeItsFeeCommits()
    {
        using var f = await Create(community: true);
        f.Register();
        var draft = Assert.IsType<Community>(f.Actor.Player.GuildManager.GuildAffiliation);
        Assert.Empty(f.Packets<ServerGuildResult>());
        Assert.Null(f.Guilds.GetGuild(draft.Id));
        Assert.Null(f.Residences.GetResidence(draft.Residence.Id));
        using var stored = f.Source.Context(); Assert.Empty(stored.Guild); Assert.Empty(stored.Residence);
    }

    [Fact]
    public async Task CommunityCreationCommitsFeeMembershipAffiliationAndEveryPlotTogether()
    {
        using var f = await Create(community: true);
        f.Register(); await f.Flush();
        using var stored = f.Source.Context();
        var guild = Assert.Single(stored.Guild); var root = Assert.Single(stored.Residence);
        Assert.Equal((byte)GuildType.Community, guild.Type);
        Assert.Equal(4800ul, stored.CharacterCurrency.Single(row => row.CurrencyId == 1).Amount);
        Assert.Equal(guild.Id, stored.Character.Single().GuildAffiliation);
        Assert.Equal(42ul, Assert.Single(stored.GuildMember).CharacterId);
        Assert.Equal(3, stored.GuildRank.Count()); Assert.Empty(stored.GuildData);
        Assert.Equal(guild.Id, root.GuildOwnerId); Assert.Null(root.OwnerId);
        Assert.Equal((byte)PropertyInfoId.Community, root.PropertyInfoId);
        Assert.Equal(2, stored.ResidencePlot.Count());
        Assert.Equal((ushort)573, stored.ResidencePlot.Single(row => row.Index == 0).PlugItemId);
        Assert.Single(f.Packets<ServerGuildResult>(), row => row.Result == GuildResult.YouCreated);
        Assert.Same(f.Guilds.GetGuild(guild.Id), f.Actor.Player.GuildManager.GetGuild<Community>(GuildType.Community));
        Assert.NotNull(f.Residences.GetResidence(root.Id));
        Assert.Equal(2, stored.BankReconciliationReceipt.Count());
        Assert.True(f.Session.CanProcessIncomingPackets);
    }

    [Theory]
    [InlineData("residence_plot")]
    [InlineData("guild_rank")]
    [InlineData("character_currency")]
    public async Task CommunityCreationParticipantFailureRetainsAndRetriesTheEntireAcceptedGraph(string table)
    {
        using var f = await Create(community: true);
        string trigger = table switch
        {
            "residence_plot" => "CREATE TRIGGER reject_community_create BEFORE INSERT ON residence_plot BEGIN SELECT RAISE(ABORT, 'owned community failure'); END",
            "guild_rank" => "CREATE TRIGGER reject_community_create BEFORE INSERT ON guild_rank BEGIN SELECT RAISE(ABORT, 'owned community failure'); END",
            "character_currency" => "CREATE TRIGGER reject_community_create BEFORE UPDATE ON character_currency BEGIN SELECT RAISE(ABORT, 'owned community failure'); END",
            _ => throw new ArgumentOutOfRangeException(nameof(table))
        };
        using (var setup = f.Source.Context()) await setup.Database.ExecuteSqlRawAsync(trigger);
        f.Register(); await f.Flush();
        using (var rejected = f.Source.Context())
        {
            Assert.Empty(rejected.Guild); Assert.Empty(rejected.GuildMember); Assert.Empty(rejected.GuildRank);
            Assert.Empty(rejected.Residence); Assert.Empty(rejected.ResidencePlot);
            Assert.Null(rejected.Character.Single().GuildAffiliation);
            Assert.Equal(5000ul, rejected.CharacterCurrency.Single(row => row.CurrencyId == 1).Amount);
            Assert.DoesNotContain(f.Packets<ServerGuildResult>(), row => row.Result == GuildResult.YouCreated);
            await rejected.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_community_create");
        }
        await f.Flush(); await f.Flush();
        using var stored = f.Source.Context(); Assert.Single(stored.Guild); Assert.Single(stored.Residence);
        Assert.Equal(2, stored.ResidencePlot.Count());
        Assert.Equal(4800ul, stored.CharacterCurrency.Single(row => row.CurrencyId == 1).Amount);
        Assert.Single(f.Packets<ServerGuildResult>(), row => row.Result == GuildResult.YouCreated);
    }

    [Fact]
    public async Task CommunityCreationFreezesThePrivateResidenceAgainstIndependentEditsAndSaves()
    {
        using var f = await Create(community: true); f.Register();
        var draft = Assert.IsType<Community>(f.Actor.Player.GuildManager.GuildAffiliation);
        var root = Assert.IsType<Residence>(draft.Residence);
        Assert.Throws<InvalidOperationException>(() => root.Name = "Unpaid edit");
        Assert.Throws<InvalidOperationException>(() => draft.Residence = null);
        using (var competing = f.Source.Context()) Assert.Throws<InvalidOperationException>(() => root.GetPlot(0).Save(competing));
        Assert.Throws<InvalidOperationException>(() => draft.DisbandGuild());
        await f.Flush();
        using var stored = f.Source.Context(); Assert.Equal("Paid guild", Assert.Single(stored.Residence).Name);
        Assert.Equal(4800ul, stored.CharacterCurrency.Single().Amount);
    }

    [Fact]
    public async Task CommunityCreationReconcilesLostAcknowledgementWithoutAnotherFeeOrPlotGraph()
    {
        var lost = new LostAcknowledgement(); using var f = await Create(lost, community: true);
        f.Register(); await f.Flush(); await f.Flush();
        using var stored = f.Source.Context();
        Assert.True(lost.Observed); Assert.Single(stored.Guild); Assert.Single(stored.Residence);
        Assert.Equal(2, stored.ResidencePlot.Count()); Assert.Equal(2, stored.BankReconciliationReceipt.Count());
        Assert.Equal(4800ul, stored.CharacterCurrency.Single().Amount);
        Assert.Single(f.Packets<ServerGuildResult>(), row => row.Result == GuildResult.YouCreated);
        Assert.True(f.Session.CanProcessIncomingPackets);
    }

    [Fact]
    public async Task CommunityCreationRetainsAcceptedFeeAndPrivateGraphsThroughOrdinaryFollowup()
    {
        using var f = await Create(community: true); int auth = 0;
        f.Actor.OnAuth = () =>
        {
            var draft = Assert.IsType<Community>(f.Actor.Player.GuildManager.GuildAffiliation);
            Assert.Null(f.Guilds.GetGuild(draft.Id)); Assert.Null(f.Residences.GetResidence(draft.Residence.Id));
            Assert.Throws<InvalidOperationException>(() => draft.Residence.Name = "Early name");
            if (++auth != 1) return;
            f.Tables.GameFormula.GetEntry(1159).Dataint0 = 999;
            f.Currency.CurrencyAddAmount(CurrencyType.Credits, 25);
            f.Guilds.Update(61); // A reentrant update must not acquire another save attempt.
        };
        f.Register(); await f.Flush(); await f.Flush();
        using var stored = f.Source.Context();
        Assert.Equal(4825ul, stored.CharacterCurrency.Single().Amount);
        Assert.Equal("Paid guild", Assert.Single(stored.Residence).Name);
        Assert.True(auth >= 2); Assert.True(f.Session.CanProcessIncomingPackets);
        Assert.Single(f.Packets<ServerGuildResult>(), row => row.Result == GuildResult.YouCreated);
    }

    [Fact]
    public async Task CommunityCreationRetriesRejectedJoinWithoutReplayingAcceptedSuccessOrDebit()
    {
        using var f = await Create(community: true); bool reject = true; int joins = 0;
        f.Messages.SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerGuildJoin join)
            {
                if (reject) { join.GuildData.GuildName = "Rejected packet mutation"; throw new IOException("Owned Community join rejection."); }
                Assert.Equal("Paid guild", join.GuildData.GuildName); joins++;
            }
            return null;
        });
        f.Register(); await f.Flush(); reject = false; await f.Flush();
        using var stored = f.Source.Context(); Assert.Single(stored.Guild); Assert.Single(stored.Residence);
        Assert.Equal(4800ul, stored.CharacterCurrency.Single().Amount);
        Assert.Single(f.Packets<ServerGuildResult>(), row => row.Result == GuildResult.YouCreated);
        Assert.Equal(1, joins); Assert.Equal(2, stored.BankReconciliationReceipt.Count());
    }

    [Fact]
    public async Task CommunityCreationPublishesNeitherSuccessNorAdmissionToAReplacementPlayer()
    {
        using var f = await Create(community: true);
        f.Actor.OnAuth = () => f.Messages.SetProperty(nameof(IWorldSession.Player), null);
        f.Register(); await f.Flush();
        using var stored = f.Source.Context(); Assert.Single(stored.Guild); Assert.Single(stored.Residence);
        Assert.Equal(4800ul, stored.CharacterCurrency.Single().Amount);
        Assert.Empty(f.Packets<ServerGuildJoin>());
        Assert.DoesNotContain(f.Packets<ServerGuildResult>(), row => row.Result == GuildResult.YouCreated);
        Assert.False(f.Session.CanProcessIncomingPackets);
    }

    [Fact]
    public async Task CommunityCreationPublicResidenceFactoryCannotPublishAnotherUnpaidRoot()
    {
        using var f = await Create(community: true); f.Register();
        var draft = Assert.IsType<Community>(f.Actor.Player.GuildManager.GuildAffiliation);
        Assert.Throws<InvalidOperationException>(() => f.Residences.CreateCommunity(draft));
        Assert.Null(f.Residences.GetResidence(draft.Residence.Id));
        await f.Flush();
        using var stored = f.Source.Context(); Assert.Single(stored.Residence); Assert.Equal(2, stored.ResidencePlot.Count());
        Assert.Equal(4800ul, stored.CharacterCurrency.Single().Amount);
        Assert.Single(f.Packets<ServerGuildResult>(), row => row.Result == GuildResult.YouCreated);
    }

    [Fact]
    public async Task CommunityCreationPreservesAnExistingOrdinaryGuildAndItsAffiliation()
    {
        using var f = await Create(community: true);
        (f with { Community = false }).Register(); await f.Flush();
        var ordinary = Assert.IsType<NexusForever.Game.Guild.Guild>(f.Actor.Player.GuildManager.Guild);
        f.Register(); await f.Flush();
        Assert.Same(ordinary, f.Actor.Player.GuildManager.Guild);
        Assert.Same(ordinary, f.Actor.Player.GuildManager.GuildAffiliation);
        using var stored = f.Source.Context();
        Assert.Equal(ordinary.Id, stored.Character.Single().GuildAffiliation);
        Assert.Equal(2, stored.Guild.Count()); Assert.Equal(2, stored.GuildMember.Count());
        Assert.Equal(4700ul, stored.CharacterCurrency.Single().Amount);
        Assert.NotEqual(ordinary.Id, Assert.Single(stored.Residence).GuildOwnerId);
        Assert.True(f.Session.CanProcessIncomingPackets);
    }

    [Fact]
    public async Task CommunityCreationPreservesDirectFreeRegistration()
    {
        using var f = await Create(community: true);
        f.Actor.Player.GuildManager.RegisterGuild(GuildType.Community, "Free community", "L", "C", "M");
        await f.Flush();
        using var stored = f.Source.Context();
        Assert.Equal("Free community", Assert.Single(stored.Guild).Name);
        Assert.Single(stored.Residence); Assert.Equal(2, stored.ResidencePlot.Count());
        Assert.Equal(5000ul, stored.CharacterCurrency.Single().Amount);
        Assert.Empty(stored.BankReconciliationReceipt);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CommunityCreationReceiptCannotAuthorizeForeignResidenceRows(bool plot)
    {
        using var f = await Create(community: true);
        var receipt = new CommunityCreationSaveReconciliation(Guid.NewGuid().ToString("N"), 42, 1, 1, "Paid guild",
            _ => { }, _ => { }, _ => true);
        var failure = await Assert.ThrowsAsync<CharacterReconciliationRolledBackException>(() =>
            CharacterAtomicSave.ExecuteCommunityCreationReconciliation(() => f.Source.Context(), 42, context =>
            {
                if (plot) context.ResidencePlot.Add(new ResidencePlotModel { Id = 999, Index = 0 });
                else context.Residence.Add(new ResidenceModel { Id = 999, GuildOwnerId = 1, PropertyInfoId = 105, Name = "Paid guild" });
            }, receipt));
        Assert.Equal(plot ? "Community creation contains a foreign or unowned shared row."
            : "Atomic Character graph contains another character's rows.", failure.InnerException!.Message);
        using var stored = f.Source.Context(); Assert.Empty(stored.Residence); Assert.Empty(stored.ResidencePlot);
        Assert.Empty(stored.BankReconciliationReceipt);
    }
}
