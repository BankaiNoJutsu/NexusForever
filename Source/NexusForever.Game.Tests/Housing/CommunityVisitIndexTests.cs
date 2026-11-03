using System.Reflection;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Character;
using NexusForever.Game.Abstract.Guild;
using NexusForever.Game.Abstract.Housing;
using NexusForever.Game.Guild;
using NexusForever.Game.Housing;
using NexusForever.Game.Static.Guild;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Static;
using NexusForever.WorldServer.Network;
using static NexusForever.Game.Tests.Housing.HousingRenameFeeRecoveryTests;

namespace NexusForever.Game.Tests.Housing;

public sealed class CommunityVisitIndexTests
{
    private readonly DbContextOptions<CharacterContext> options;
    public CommunityVisitIndexTests() { }
    internal CommunityVisitIndexTests(DbContextOptions<CharacterContext> options) => this.options = options;
    private Task<Fixture> Create() => new HousingRenameFeeRecoveryTests(options).Create();

    [Fact]
    public async Task ConfirmedPaidRenameMakesTheNewVisitorNameResolve()
    {
        using var f = await Create(); f.Handler.HandleMessage(f.Session, Request("Accepted")); await Flush(f);
        AssertCommitted(f, "Accepted"); Assert.Same(f.Community.Residence, f.Residences.GetCommunityByOwner("Accepted"));
    }
    [Fact]
    public async Task ConfirmedPaidRenameRemovesItsOldVisitorAlias()
    {
        using var f = await Create(); f.Handler.HandleMessage(f.Session, Request("Accepted")); await Flush(f);
        AssertCommitted(f, "Accepted"); Assert.Null(f.Residences.GetCommunityByOwner("Original"));
    }
    [Theory]
    [InlineData("Accepted")]
    [InlineData("original")]
    public async Task ConfirmedPaidRenameRefreshesThePublicDisplayName(string name)
    {
        using var f = await Create(); EnsurePublicVisit(f);
        f.Handler.HandleMessage(f.Session, Request(name)); await Flush(f); AssertCommitted(f, name);
        Assert.Equal(name, Assert.Single(f.Residences.GetRandomVisitableCommunities()).Name);
    }
    [Fact]
    public async Task FreshResidenceModelLoadRestoresThePublicVisitEntry()
    {
        using var f = await Create(); var loaded = Reload(f);
        var visit = Assert.Single(loaded.GetRandomVisitableCommunities());
        Assert.Equal(77ul, visit.NeighbourhoodId); Assert.Equal("Original", visit.Name); Assert.Equal("Leader", visit.Owner);
        Assert.Same(f.Community.Residence, loaded.GetCommunityByOwner("Original"));
    }
    [Fact]
    public async Task FreshResidenceModelLoadDoesNotExposeAPrivateCommunity()
    {
        using var f = await Create(); f.Community.SetCommunityPrivate(true); await Flush(f);
        var loaded = Reload(f); Assert.Empty(loaded.GetRandomVisitableCommunities());
        Assert.Same(f.Community.Residence, loaded.GetCommunityByOwner("Original"));
    }
    [Fact]
    public async Task PrivatePaidRenameDoesNotCreateAPublicVisitEntry()
    {
        using var f = await Create(); f.Community.SetCommunityPrivate(true); await Flush(f);
        f.Handler.HandleMessage(f.Session, Request("Accepted")); await Flush(f); AssertCommitted(f, "Accepted");
        Assert.Empty(f.Residences.GetRandomVisitableCommunities());
    }
    [Fact]
    public async Task FailedPaidRenameKeepsThePriorIndexesUntilItsRecoveryCommits()
    {
        using var f = await Create(); EnsurePublicVisit(f);
        using (var setup = f.Source.Context())
            await setup.Database.ExecuteSqlRawAsync(setup.Database.IsSqlite()
                ? "CREATE TRIGGER reject_visit_rename BEFORE UPDATE ON guild BEGIN SELECT RAISE(ABORT, 'owned visitor name failure'); END"
                : "CREATE TRIGGER reject_visit_rename BEFORE UPDATE ON guild FOR EACH ROW SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='owned visitor name failure'");
        f.Handler.HandleMessage(f.Session, Request("Accepted")); await Flush(f);
        using (var failed = f.Source.Context())
        {
            Assert.Equal("Original", failed.Guild.Single(row => row.Id == 77).Name);
            Assert.Same(f.Community.Residence, f.Residences.GetCommunityByOwner("Original")); Assert.Null(f.Residences.GetCommunityByOwner("Accepted"));
            Assert.Equal("Original", Assert.Single(f.Residences.GetRandomVisitableCommunities()).Name);
            await failed.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_visit_rename");
        }
        await Flush(f); AssertCommitted(f, "Accepted");
        Assert.Same(f.Community.Residence, f.Residences.GetCommunityByOwner("Accepted")); Assert.Null(f.Residences.GetCommunityByOwner("Original"));
        Assert.Equal("Accepted", Assert.Single(f.Residences.GetRandomVisitableCommunities()).Name);
    }

    [Fact]
    public async Task NewCommunityDirectoryNameWaitsForItsCreationSave()
    {
        using var f = await Create(); var fresh = await RegisterFresh(f);
        Assert.True(fresh.PendingCreate); Assert.Null(f.Residences.GetCommunityByOwner("Fresh"));
        Assert.Empty(f.Residences.GetRandomVisitableCommunities());
    }
    [Fact]
    public async Task ConfirmedCommunityCreationPublishesItsNameAndPublicVisit()
    {
        using var f = await Create(); var fresh = await RegisterFresh(f); await Flush(f);
        using var sql = f.Source.Context(); Assert.Equal("Fresh", sql.Guild.Single(row => row.Id == fresh.Id).Name);
        Assert.Equal("Fresh", sql.Residence.Single(row => row.Id == fresh.Residence.Id).Name); Assert.False(fresh.PendingCreate);
        Assert.Same(fresh.Residence, f.Residences.GetCommunityByOwner("Fresh"));
        var visit = Assert.Single(f.Residences.GetRandomVisitableCommunities()); Assert.Equal(fresh.Id, visit.NeighbourhoodId); Assert.Equal("Fresh", visit.Name);
    }

    [Fact]
    public async Task RenameIndexesAreCurrentBeforeItsSuccessfulPrivateReply()
    {
        using var f = await Create(); int replies = 0;
        f.Messages.SetMethodHandler(nameof(IWorldSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerHousingCommunityRenameResult { Result: HousingResult.Success })
            {
                replies++; Assert.Same(f.Community.Residence, f.Residences.GetCommunityByOwner("Accepted"));
                Assert.Null(f.Residences.GetCommunityByOwner("Original"));
                Assert.Equal("Accepted", Assert.Single(f.Residences.GetRandomVisitableCommunities()).Name);
            }
            return null;
        });
        f.Handler.HandleMessage(f.Session, Request("Accepted")); await Flush(f); Assert.Equal(1, replies);
    }
    [Fact]
    public async Task RenamePreservesTheAlreadyPublishedOwnerCaption()
    {
        using var f = await Create(); f.Residences.DeregisterCommunityVists(f.Community.Residence.Id);
        f.Residences.RegisterCommunityVisits(f.Community.Residence, f.Community, "Published caption");
        f.Handler.HandleMessage(f.Session, Request("Accepted")); await Flush(f);
        Assert.Equal("Published caption", Assert.Single(f.Residences.GetRandomVisitableCommunities()).Owner);
    }
    [Fact]
    public async Task FailedCreationKeepsItsDirectoryUnpublishedUntilRetry()
    {
        using var f = await Create(); var fresh = await RegisterFresh(f);
        using (var setup = f.Source.Context())
            await setup.Database.ExecuteSqlRawAsync(setup.Database.IsSqlite()
                ? "CREATE TRIGGER reject_directory_create BEFORE INSERT ON guild BEGIN SELECT RAISE(ABORT, 'owned directory creation failure'); END"
                : "CREATE TRIGGER reject_directory_create BEFORE INSERT ON guild FOR EACH ROW SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='owned directory creation failure'");
        await Flush(f);
        using (var failed = f.Source.Context())
        {
            Assert.DoesNotContain(failed.Guild, row => row.Id == fresh.Id); Assert.True(fresh.PendingCreate);
            Assert.Null(f.Residences.GetCommunityByOwner("Fresh")); Assert.Empty(f.Residences.GetRandomVisitableCommunities());
            await failed.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_directory_create");
        }
        await Flush(f); Assert.False(fresh.PendingCreate); Assert.Same(fresh.Residence, f.Residences.GetCommunityByOwner("Fresh"));
        Assert.Equal("Fresh", Assert.Single(f.Residences.GetRandomVisitableCommunities()).Name);
    }
    [Fact]
    public async Task PrivateCreationPublishesItsSearchNameWithoutAPublicVisit()
    {
        using var f = await Create(); var fresh = await RegisterFresh(f); fresh.SetFlag(GuildFlag.CommunityPrivate); await Flush(f);
        using var sql = f.Source.Context(); Assert.Equal((uint)GuildFlag.CommunityPrivate, sql.Guild.Single(row => row.Id == fresh.Id).Flags);
        Assert.Same(fresh.Residence, f.Residences.GetCommunityByOwner("Fresh")); Assert.Empty(f.Residences.GetRandomVisitableCommunities());
    }
    [Fact]
    public async Task RetirementBeforeCreationDirectoryPublicationCannotReAddTheRoot()
    {
        using var f = await Create(); var fresh = await RegisterFresh(f);
        f.Guilds.Update(61); await Wait(f, f.Guilds, "cacheLock", "guildSavesInFlight"); Assert.False(fresh.PendingCreate);
        fresh.DisbandGuild(); f.Residences.Update(0);
        Assert.Null(f.Residences.GetCommunityByOwner("Fresh")); Assert.Empty(f.Residences.GetRandomVisitableCommunities());
        await Flush(f);
    }
    [Fact]
    public async Task ACreationDirectoryRecordCannotUndoALaterPaidRename()
    {
        using var f = await Create(); var fresh = await RegisterFresh(f);
        f.Guilds.Update(61); await Wait(f, f.Guilds, "cacheLock", "guildSavesInFlight");
        f.Handler.HandleMessage(f.Session, Request("Accepted")); await Flush(f); await Flush(f);
        using var sql = f.Source.Context(); Assert.Equal("Accepted", sql.Guild.Single(row => row.Id == fresh.Id).Name);
        Assert.Equal(4900ul, sql.CharacterCurrency.Single(row => row.CurrencyId == 1).Amount);
        Assert.Null(f.Residences.GetCommunityByOwner("Fresh")); Assert.Same(fresh.Residence, f.Residences.GetCommunityByOwner("Accepted"));
        Assert.Equal("Accepted", Assert.Single(f.Residences.GetRandomVisitableCommunities()).Name);
    }
    [Fact]
    public async Task RetirementRemovesTheConfirmedNewNameAndVisitEntry()
    {
        using var f = await Create(); f.Handler.HandleMessage(f.Session, Request("Accepted")); await Flush(f);
        f.Community.DisbandGuild(); Assert.Null(f.Residences.GetCommunityByOwner("Accepted"));
        Assert.Null(f.Residences.GetCommunityByOwner("Original")); Assert.Empty(f.Residences.GetRandomVisitableCommunities()); await Flush(f);
    }
    [Fact]
    public async Task AStaleRootCannotRemoveTheReplacementCommunityMetadata()
    {
        using var f = await Create(); IResidence replacement = InstallReplacement(f);
        f.Residences.RemoveCommunity(f.Community);
        Assert.Same(replacement, f.Residences.GetCommunityByOwner("Taken"));
        Assert.Equal(88ul, Assert.Single(f.Residences.GetRandomVisitableCommunities()).NeighbourhoodId);
    }
    [Fact]
    public async Task AnOldAliasCannotResolveAnotherCommunitysReplacementRoot()
    {
        using var f = await Create(); IResidence replacement = InstallReplacement(f);
        Assert.Null(f.Residences.GetCommunityByOwner("Original")); Assert.Same(replacement, f.Residences.GetCommunityByOwner("Taken"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OrdinaryRenameAfterCreationCapturePublishesOnlyItsConfirmedName(bool beforeCommit)
    {
        using var f = await Create(); var fresh = await RegisterFresh(f);
        using (var context = f.Source.Context())
        {
            using var transaction = await context.Database.BeginTransactionAsync();
            fresh.Save(context); await context.SaveChangesAsync();
            if (beforeCommit) fresh.RenameGuild("Later");
            await transaction.CommitAsync();
        }
        if (!beforeCommit) fresh.RenameGuild("Later");
        f.Residences.Update(0);
        Assert.Null(f.Residences.GetCommunityByOwner("Later")); Assert.Empty(f.Residences.GetRandomVisitableCommunities());
        using (var context = f.Source.Context()) { fresh.Save(context); await context.SaveChangesAsync(); }
        Assert.Null(f.Residences.GetCommunityByOwner("Later"));
        f.Residences.Update(0);
        using var sql = f.Source.Context(); Assert.Equal("Later", sql.Guild.Single(row => row.Id == fresh.Id).Name);
        Assert.Equal("Later", sql.Residence.Single(row => row.Id == fresh.Residence.Id).Name);
        Assert.Null(f.Residences.GetCommunityByOwner("Fresh")); Assert.Same(fresh.Residence, f.Residences.GetCommunityByOwner("Later"));
        Assert.Equal("Later", Assert.Single(f.Residences.GetRandomVisitableCommunities()).Name);
    }

    [Fact]
    public async Task UnsavedPublicFlagCannotExposeAConfirmedPrivateCreation()
    {
        using var f = await Create(); var fresh = await RegisterFresh(f); fresh.SetFlag(GuildFlag.CommunityPrivate);
        using (var context = f.Source.Context()) { fresh.Save(context); await context.SaveChangesAsync(); }
        fresh.RemoveFlag(GuildFlag.CommunityPrivate); f.Residences.Update(0);
        using (var sql = f.Source.Context()) Assert.Equal((uint)GuildFlag.CommunityPrivate, sql.Guild.Single(row => row.Id == fresh.Id).Flags);
        Assert.Empty(f.Residences.GetRandomVisitableCommunities());
        using (var context = f.Source.Context()) { fresh.Save(context); await context.SaveChangesAsync(); }
        Assert.Empty(f.Residences.GetRandomVisitableCommunities()); f.Residences.Update(0);
        Assert.Equal("Fresh", Assert.Single(f.Residences.GetRandomVisitableCommunities()).Name);
    }

    [Fact]
    public async Task ModelReloadCannotPublishAnUnsavedCommunityName()
    {
        using var f = await Create(); f.Community.RenameGuild("Pending"); var loaded = Reload(f);
        using var sql = f.Source.Context(); Assert.Equal("Original", sql.Guild.Single(row => row.Id == 77).Name);
        Assert.Null(loaded.GetCommunityByOwner("Pending")); Assert.DoesNotContain(loaded.GetRandomVisitableCommunities(), row => row.Name == "Pending");
    }

    [Fact]
    public async Task ModelReloadCannotExposeAnUnsavedPublicFlag()
    {
        using var f = await Create(); f.Community.SetCommunityPrivate(true); await Flush(f);
        f.Community.RemoveFlag(GuildFlag.CommunityPrivate); var loaded = Reload(f);
        using var sql = f.Source.Context(); Assert.Equal((uint)GuildFlag.CommunityPrivate, sql.Guild.Single(row => row.Id == 77).Flags);
        Assert.Empty(loaded.GetRandomVisitableCommunities());
    }

    [Fact]
    public async Task ModelReloadUsesTheConfirmedLeaderCaption()
    {
        using var f = await Create();
        var manager = (ICharacterManager)typeof(GlobalResidenceManager).GetField("characterManager", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(f.Residences);
        ((RecordingDispatchProxy<ICharacterManager>)(object)manager).SetMethodHandler(nameof(ICharacterManager.GetCharacter), args =>
        {
            var character = RecordingDispatchProxy<ICharacter>.Create(out var proxy);
            proxy.SetProperty(nameof(ICharacter.Name), (ulong)args[0] == 42 ? "Leader" : "Other"); return character;
        });
        f.Community.LeaderId = 43; var loaded = Reload(f);
        using var sql = f.Source.Context(); Assert.Equal(42ul, sql.Guild.Single(row => row.Id == 77).LeaderId);
        Assert.Equal("Leader", Assert.Single(loaded.GetRandomVisitableCommunities()).Owner);
        loaded.Update(0); Assert.Equal("Leader", Assert.Single(loaded.GetRandomVisitableCommunities()).Owner);
    }

    private static IResidence InstallReplacement(Fixture f)
    {
        var other = Assert.IsType<Community>(f.Guilds.GetGuild(88));
        var root = new Residence(new ResidenceModel { Id = 100, GuildOwnerId = 88, Name = "Taken", PropertyInfoId = (byte)NexusForever.Game.Static.Housing.PropertyInfoId.Community },
            f.Residences, 1, f.Source.Source.Tables); other.Residence = root;
        T Cache<T>(string name) => (T)typeof(GlobalResidenceManager).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(f.Residences);
        Cache<IDictionary<ulong,IResidence>>("residences")[100] = root;
        Cache<Dictionary<ulong,ulong>>("communityOwnerCache")[88] = 100;
        Cache<Dictionary<string,ulong>>("communitySearchCache")["Taken"] = 100;
        f.Residences.DeregisterCommunityVists(100); f.Residences.RegisterCommunityVisits(root, other, "Other"); return root;
    }

    private static async Task<Community> RegisterFresh(Fixture f)
    {
        f.Community.DisbandGuild(); await Flush(f);
        var membership = (RecordingDispatchProxy<IGuildManager>)(object)f.Actor.Player.GuildManager;
        membership.SetMethodReturn(nameof(IGuildManager.GetGuild), null);
        var fresh = Assert.IsType<Community>(f.Guilds.RegisterGuild(GuildType.Community, "Fresh", "Leader", "Council", "Member"));
        membership.SetProperty(nameof(IGuildManager.GuildAffiliation), fresh);
        fresh.JoinGuild(f.Actor.Player); membership.SetMethodReturn(nameof(IGuildManager.GetGuild), fresh);
        return fresh;
    }

    private static void AssertCommitted(Fixture f, string name)
    {
        using var context = f.Source.Context(); Assert.Equal(name, context.Guild.Single(row => row.Id == 77).Name);
        Assert.Equal(name, context.Residence.Single().Name); Assert.Equal(4900ul, context.CharacterCurrency.Single(row => row.CurrencyId == 1).Amount);
    }
    private static void EnsurePublicVisit(Fixture f)
    {
        if (!f.Residences.GetRandomVisitableCommunities().Any()) f.Residences.RegisterCommunityVisits(f.Community.Residence, f.Community, "Leader");
    }
    private static GlobalResidenceManager Reload(Fixture f)
    {
        T Dependency<T>(string name) => (T)typeof(GlobalResidenceManager).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(f.Residences);
        var loaded = new GlobalResidenceManager(characterManager:Dependency<ICharacterManager>("characterManager"),
            globalGuildManager:Dependency<IGlobalGuildManager>("globalGuildManager"), realmContext:Dependency<IRealmContext>("realmContext"),
            databaseManager:Dependency<IDatabaseManager>("databaseManager"), gameTableManager:Dependency<IGameTableManager>("gameTableManager"));
        loaded.Initialise(); return loaded;
    }
}
