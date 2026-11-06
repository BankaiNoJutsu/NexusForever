using System.Diagnostics;
using System.Reflection;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Database.Configuration.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Character;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Guild;
using NexusForever.Game.Entity;
using NexusForever.Game.Guild;
using NexusForever.Game.Housing;
using NexusForever.Game.Static.Housing;
using NexusForever.Game.Abstract.RBAC;
using NexusForever.Game.Achievement;
using NexusForever.Game.Static.Achievement;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Guild;
using NexusForever.Game.Tests.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.GameTable.Text.Filter;
using NexusForever.Network.Internal;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Guild;
using NexusForever.Shared.Game.Events;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Guild;
using Actor = NexusForever.Game.Tests.Challenges.RewardNormalSaveSnapshotTests.Actor;
using SourceFixture = NexusForever.Game.Tests.Quest.QuestPersistenceDurabilityTests.Fixture;
using GameGuild = NexusForever.Game.Guild.Guild;

namespace NexusForever.Game.Tests.Guild;

public sealed partial class GuildCreationFinanceTests
{
    [Fact]
    public async Task PublicCreationDoesNotPublishSuccessBeforeItsFeeAndGuildCommit()
    {
        using var f = await Create();
        f.Register();
        Assert.Empty(f.Packets<ServerGuildResult>());
        Assert.Empty(f.Packets<ServerGuildJoin>());
        await f.Flush();
        using var stored = f.Source.Context();
        Assert.Equal(4900ul, stored.CharacterCurrency.Single(row => row.CurrencyId == 1).Amount);
        var guild = Assert.Single(stored.Guild);
        Assert.Equal(guild.Id, stored.Character.Single().GuildAffiliation);
        Assert.Equal(42ul, Assert.Single(stored.GuildMember).CharacterId);
        Assert.Equal(3, stored.GuildRank.Count()); Assert.Single(stored.GuildData);
        Assert.Single(f.Packets<ServerGuildResult>(), row => row.Result == GuildResult.YouCreated);
        Assert.Single(f.Packets<ServerGuildJoin>());
    }

    [Fact]
    public async Task AcceptedFeeRemainsProvisionalUntilTheOriginalCharacterTransactionCommits()
    {
        using var f = await Create();
        f.Register();
        Assert.Equal(5000ul, f.Currency.Single(row => row.Id == CurrencyType.Credits).Amount);
        Assert.False(f.Currency.CanAfford(CurrencyType.Credits, 5000)); // The fee is reserved, not adopted.
        using var pending = f.Source.Context(); Assert.Empty(pending.Guild);
        Assert.Equal(5000ul, pending.CharacterCurrency.Single(row => row.CurrencyId == 1).Amount);
        await f.Flush();
        Assert.False(f.Currency.CanAfford(CurrencyType.Credits, 5000));
    }

    [Fact]
    public async Task PeriodicGuildSaveCannotCommitAnUnpaidCreation()
    {
        using var f = await Create();
        f.Register(); await f.Flush();
        using var stored = f.Source.Context(); Assert.Single(stored.Guild);
        Assert.Equal(4900ul, stored.CharacterCurrency.Single(row => row.CurrencyId == 1).Amount);
    }

    [Theory]
    [InlineData("guild_rank")]
    [InlineData("character_currency")]
    [InlineData("character")]
    public async Task RejectedParticipantRollsBackFeeGuildAndAffiliationThenRetriesTogether(string table)
    {
        using var f = await Create();
        string trigger = table switch
        {
            "guild_rank" => "CREATE TRIGGER reject_paid_create BEFORE INSERT ON guild_rank BEGIN SELECT RAISE(ABORT, 'owned creation failure'); END",
            "character_currency" => "CREATE TRIGGER reject_paid_create BEFORE UPDATE ON character_currency BEGIN SELECT RAISE(ABORT, 'owned creation failure'); END",
            "character" => "CREATE TRIGGER reject_paid_create BEFORE UPDATE ON character BEGIN SELECT RAISE(ABORT, 'owned creation failure'); END",
            _ => throw new ArgumentOutOfRangeException(nameof(table))
        };
        using (var setup = f.Source.Context()) await setup.Database.ExecuteSqlRawAsync(trigger);
        f.Register(); await f.Flush();
        using (var rejected = f.Source.Context())
        {
            Assert.Empty(rejected.Guild); Assert.Empty(rejected.GuildMember); Assert.Empty(rejected.GuildRank); Assert.Empty(rejected.GuildData);
            Assert.Null(rejected.Character.Single().GuildAffiliation);
            Assert.Equal(5000ul, rejected.CharacterCurrency.Single(row => row.CurrencyId == 1).Amount);
            Assert.DoesNotContain(f.Packets<ServerGuildResult>(), row => row.Result == GuildResult.YouCreated);
            Assert.Null(f.Guilds.GetGuild(GuildType.Guild, "Paid guild"));
            await rejected.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_paid_create");
        }
        await f.Flush(); await f.Flush();
        using var stored = f.Source.Context();
        Assert.Equal(4900ul, stored.CharacterCurrency.Single(row => row.CurrencyId == 1).Amount);
        Assert.Equal(Assert.Single(stored.Guild).Id, stored.Character.Single().GuildAffiliation);
        Assert.Single(f.Packets<ServerGuildResult>(), row => row.Result == GuildResult.YouCreated);
    }

    [Fact]
    public async Task LostCommitAcknowledgementReconcilesTheCompleteCreateWithoutAnotherFee()
    {
        var lost = new LostAcknowledgement(); using var f = await Create(lost);
        f.Register(); await f.Flush(); await f.Flush();
        using var stored = f.Source.Context();
        Assert.True(lost.Observed); Assert.Single(stored.Guild); Assert.Single(stored.GuildMember);
        Assert.Equal(4900ul, stored.CharacterCurrency.Single(row => row.CurrencyId == 1).Amount);
        Assert.Equal(2, stored.BankReconciliationReceipt.Count());
        Assert.Single(f.Packets<ServerGuildResult>(), row => row.Result == GuildResult.YouCreated);
    }

    [Fact]
    public async Task RejectedJoinPacketRetriesWithoutRepeatingFeeOrEarlierSuccess()
    {
        using var f = await Create(); bool reject = true; int joins = 0;
        f.Messages.SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerGuildJoin join)
            {
                if (reject) { join.GuildData.GuildName = "mutated rejected packet"; throw new IOException("Owned join rejection."); }
                Assert.Equal("Paid guild", join.GuildData.GuildName); joins++;
            }
            return null;
        });
        f.Register(); await f.Flush();
        using (var stored = f.Source.Context())
        { Assert.Equal(4900ul, stored.CharacterCurrency.Single(row => row.CurrencyId == 1).Amount); Assert.Single(stored.Guild); Assert.Equal(2, stored.BankReconciliationReceipt.Count()); }
        reject = false; await f.Flush();
        Assert.Equal(1, joins); Assert.Single(f.Packets<ServerGuildResult>(), row => row.Result == GuildResult.YouCreated);
        Assert.True(f.Session.CanProcessIncomingPackets);
    }

    [Fact]
    public async Task ChangedFormulaAndLateEarningCannotChangeTheAcceptedCreationFee()
    {
        using var f = await Create(); bool changed = false;
        f.Actor.OnAuth = () =>
        {
            if (changed) return; changed = true;
            f.Tables.GameFormula.GetEntry(764).Dataint0 = 999;
            f.Currency.CurrencyAddAmount(CurrencyType.Credits, 25);
        };
        f.Register(); await f.Flush();
        using var stored = f.Source.Context(); Assert.Equal(4925ul, stored.CharacterCurrency.Single(row => row.CurrencyId == 1).Amount);
        Assert.Single(stored.Guild); Assert.True(f.Session.CanProcessIncomingPackets);
    }

    [Fact]
    public async Task ReplacedSessionPlayerCannotReceiveCreationSuccessOrReopenAdmission()
    {
        using var f = await Create();
        f.Actor.OnAuth = () => f.Messages.SetProperty(nameof(IWorldSession.Player), null);
        f.Register(); await f.Flush();
        using var stored = f.Source.Context(); Assert.Equal(4900ul, stored.CharacterCurrency.Single(row => row.CurrencyId == 1).Amount);
        Assert.Single(stored.Guild); Assert.Empty(f.Packets<ServerGuildJoin>());
        Assert.DoesNotContain(f.Packets<ServerGuildResult>(), row => row.Result == GuildResult.YouCreated);
        Assert.False(f.Session.CanProcessIncomingPackets);
    }

    [Fact]
    public async Task PendingNameAndDraftGraphCannotBeTakenByAnotherCreationOrSave()
    {
        using var f = await Create(); f.Register();
        var draft = Assert.IsType<GameGuild>(f.Actor.Player.GuildManager.GuildAffiliation);
        using (var competing = f.Source.Context()) Assert.Throws<InvalidOperationException>(() => draft.Save(competing));
        Assert.Throws<InvalidOperationException>(() => f.Guilds.RegisterGuild(GuildType.Guild, "Paid guild", "L", "C", "M", draft.Standard));
        Assert.Throws<InvalidOperationException>(() => draft.DisbandGuild());
        await f.Flush();
        using var stored = f.Source.Context(); Assert.Single(stored.Guild); Assert.Equal(4900ul, stored.CharacterCurrency.Single(row => row.CurrencyId == 1).Amount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DraftStructuralRemovalCannotBypassThePaidCreationFence(bool rank)
    {
        using var f = await Create(); f.Register();
        var draft = Assert.IsType<GameGuild>(f.Actor.Player.GuildManager.GuildAffiliation);
        if (rank)
        {
            var error = Assert.Throws<TargetInvocationException>(() => typeof(GuildBase)
                .GetMethod("RemoveRank", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(draft, [(byte)1]));
            Assert.IsType<InvalidOperationException>(error.InnerException);
        }
        else Assert.Throws<InvalidOperationException>(() => draft.LeaveGuild(f.Actor.Player, GuildResult.MemberQuit));
        await f.Flush();
        using var stored = f.Source.Context(); Assert.Equal(3, stored.GuildRank.Count()); Assert.Single(stored.GuildMember);
        Assert.Equal(4900ul, stored.CharacterCurrency.Single(row => row.CurrencyId == 1).Amount);
    }

    [Fact]
    public async Task GuildRemainsPrivateThroughTheOrdinaryFollowupSoARenameCannotStrandItsOwner()
    {
        using var f = await Create(); int auth = 0; bool publicDuringFollowup = false;
        f.Actor.OnAuth = () =>
        {
            if (++auth != 2) return;
            var visible = f.Guilds.GetGuild(GuildType.Guild, "Paid guild");
            publicDuringFollowup = visible != null;
            if (visible == null) return;
            visible.RenameGuild("Later accepted name");
            using var context = f.Source.Context(); visible.Save(context); context.SaveChanges();
        };
        f.Register(); await f.Flush();
        Assert.False(publicDuringFollowup);
        Assert.True(f.Session.CanProcessIncomingPackets);
        using var stored = f.Source.Context(); Assert.Equal("Paid guild", Assert.Single(stored.Guild).Name);
        Assert.Equal(4900ul, stored.CharacterCurrency.Single(row => row.CurrencyId == 1).Amount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RealJoinAchievementsReachOrdinarySettlementWithoutOpeningTheDraftFence(bool matching)
    {
        using var f = await Create(matchingAchievements: matching);
        f.Register(); await f.Flush(); await f.Flush();
        Assert.True(f.Session.CanProcessIncomingPackets);
        using var stored = f.Source.Context();
        Assert.Single(stored.Guild); Assert.Equal(4900ul, stored.CharacterCurrency.Single(row => row.CurrencyId == 1).Amount);
        Assert.Single(f.Packets<ServerGuildResult>(), row => row.Result == GuildResult.YouCreated);
        if (matching)
        {
            Assert.Equal(1u, Assert.Single(stored.CharacterAchievement).ProgressState);
            Assert.Equal(1u, Assert.Single(stored.GuildAchievement).ProgressState);
        }
        else { Assert.Empty(stored.CharacterAchievement); Assert.Empty(stored.GuildAchievement); }
    }

    [Fact]
    public async Task EarningAfterLocalAdoptionRetainsGuildForwardingUntilTheOrdinarySaveSettles()
    {
        using var f = await Create(matchingAchievements: true); int auth = 0;
        f.Actor.OnAuth = () => { if (++auth == 2) f.Currency.CurrencyAddAmount(CurrencyType.Credits, 25); };
        f.Register(); await f.Flush(); await f.Flush();
        using var stored = f.Source.Context(); Assert.Equal(4925ul, stored.CharacterCurrency.Single().Amount);
        Assert.Equal(1u, Assert.Single(stored.CharacterAchievement).ProgressState);
        Assert.Equal(1u, Assert.Single(stored.GuildAchievement).ProgressState);
        Assert.True(f.Session.CanProcessIncomingPackets);
        Assert.Single(f.Packets<ServerGuildResult>(), row => row.Result == GuildResult.YouCreated);
    }

    [Fact]
    public async Task RejectedDeferredGuildEffectDoesNotRepeatCommittedCharacterJoinProgress()
    {
        using var f = await Create(matchingAchievements: true); bool reject = true;
        var guildInfo = f.Achievements.GetAchievement(501);
        ((RecordingDispatchProxy<IGlobalAchievementManager>)(object)f.Achievements)
            .SetMethodHandler(nameof(IGlobalAchievementManager.GetGuildAchievements), args =>
            {
                if ((AchievementType)args[0] != AchievementType.GuildOrCircleJoin) return Array.Empty<IAchievementInfo>();
                if (reject) throw new IOException("Owned guild join effect rejected before mutation.");
                return new IAchievementInfo[] { guildInfo };
            });
        f.Register(); await f.Flush();
        using (var pending = f.Source.Context())
        {
            Assert.Equal(4900ul, pending.CharacterCurrency.Single().Amount);
            Assert.Empty(pending.CharacterAchievement);
            Assert.Empty(pending.GuildAchievement); Assert.Single(pending.BankReconciliationReceipt);
        }
        Assert.Null(f.Guilds.GetGuild(GuildType.Guild, "Paid guild"));
        Assert.Throws<InvalidOperationException>(() => f.Actor.Player.GuildManager.Guild.RenameGuild("Cannot take this draft"));
        Assert.False(f.Session.CanProcessIncomingPackets);
        reject = false; await f.Flush();
        using var stored = f.Source.Context(); Assert.Equal(1u, Assert.Single(stored.CharacterAchievement).ProgressState);
        Assert.Equal(1u, Assert.Single(stored.GuildAchievement).ProgressState); Assert.Equal(2, stored.BankReconciliationReceipt.Count());
        Assert.True(f.Session.CanProcessIncomingPackets);
    }

    [Fact]
    public async Task CompletingGuildAchievementFeedsCharacterProgressIntoTheRetainedOrdinarySave()
    {
        using var f = await Create(matchingAchievements: true, completeGuildAchievement: true);
        f.Register(); await f.Flush(); await f.Flush();
        using var stored = f.Source.Context();
        Assert.NotNull(Assert.Single(stored.GuildAchievement).DateCompleted);
        Assert.Equal(1u, Assert.Single(stored.CharacterAchievement.Where(row => row.AchievementId == 502)).ProgressState);
        Assert.Equal(4900ul, stored.CharacterCurrency.Single().Amount);
        Assert.True(f.Session.CanProcessIncomingPackets);
    }

    private sealed class LostAcknowledgement : DbTransactionInterceptor
    {
        internal bool Observed;
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData data, CancellationToken cancellationToken = default)
        {
            if (Observed) return Task.CompletedTask;
            Observed = true; return Task.FromException(new IOException("Owned creation commit acknowledgement loss."));
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReceiptCallbackCannotAuthorizeForeignGuildRows(bool member)
    {
        using var f = await Create();
        var receipt = new GuildCreationSaveReconciliation(Guid.NewGuid().ToString("N"), 42, 1, (byte)GuildType.Guild, "Paid guild",
            _ => { }, _ => { }, _ => true);
        var failure = await Assert.ThrowsAsync<CharacterReconciliationRolledBackException>(() =>
            CharacterAtomicSave.ExecuteGuildCreationReconciliation(() => f.Source.Context(), 42, context =>
            {
                if (member) context.GuildMember.Add(new GuildMemberModel { Id = 999, CharacterId = 42, Rank = 0 });
                else context.GuildData.Add(new GuildDataModel { Id = 999 });
            }, receipt));
        Assert.Contains("foreign or unowned guild row", failure.InnerException!.Message);
        using var stored = f.Source.Context(); Assert.Empty(stored.GuildMember); Assert.Empty(stored.GuildData);
        Assert.Empty(stored.BankReconciliationReceipt); Assert.Equal(5000ul, stored.CharacterCurrency.Single().Amount);
    }

    [Fact]
    public async Task ReentrantWorldUpdateDuringAuthCannotStartASecondCreationAttempt()
    {
        using var f = await Create(); int auth = 0;
        f.Actor.OnAuth = () => { if (++auth == 1) f.Guilds.Update(61); };
        f.Register(); await f.Flush();
        using var stored = f.Source.Context(); Assert.Single(stored.Guild);
        Assert.Equal(4900ul, stored.CharacterCurrency.Single(row => row.CurrencyId == 1).Amount);
        Assert.Equal(2, auth); Assert.Equal(2, stored.BankReconciliationReceipt.Count());
        Assert.Single(f.Packets<ServerGuildResult>(), row => row.Result == GuildResult.YouCreated);
        Assert.True(f.Session.CanProcessIncomingPackets);
    }

    private readonly Func<(DbContextOptions<CharacterContext> Character, DatabaseConnectionString Auth)> providerFixture;
    public GuildCreationFinanceTests() { }
    internal GuildCreationFinanceTests(Func<(DbContextOptions<CharacterContext> Character, DatabaseConnectionString Auth)> providerFixture)
        => this.providerFixture = providerFixture;

    private sealed class InterceptedDatabase(SourceFixture source, IInterceptor interceptor) : CharacterDatabase
    {
        public override Task SaveGuildCreationReconciliation(ulong characterId, Action<CharacterContext> action, GuildCreationSaveReconciliation receipt)
            => CharacterAtomicSave.ExecuteGuildCreationReconciliation(() => source.Context(interceptor), characterId, action, receipt);
        public override Task SaveCommunityCreationReconciliation(ulong characterId, Action<CharacterContext> action, CommunityCreationSaveReconciliation receipt)
            => CharacterAtomicSave.ExecuteCommunityCreationReconciliation(() => source.Context(interceptor), characterId, action, receipt);
    }

    private async Task<Fixture> Create(IInterceptor interceptor = null, bool matchingAchievements = false, bool completeGuildAchievement = false, bool community = false, bool tokens = false,
        NexusForever.Game.Abstract.Prerequisite.IPrerequisiteManager achievementPrerequisites = null,
        IPlayerManager achievementPlayers = null)
    {
        var configured = providerFixture?.Invoke();
        var source = new SourceFixture(configured?.Character); source.Seed();
        var tables = source.Source.Tables;
        var table = (RecordingDispatchProxy<IGameTableManager>)(object)tables;
        table.SetProperty(nameof(IGameTableManager.CurrencyType), PlayerBindPointTests.Table(new CurrencyTypeEntry { Id = 1, CapAmount = 1000000 }));
        table.SetProperty(nameof(IGameTableManager.GameFormula), PlayerBindPointTests.Table(new GameFormulaEntry { Id = 764, Dataint0 = 100 },
            new GameFormulaEntry { Id = 1159, Dataint0 = 200, Dataint01 = 5 }));
        table.SetProperty(nameof(IGameTableManager.HousingPlotInfo), PlayerBindPointTests.Table(
            new HousingPlotInfoEntry { Id = 3, HousingPropertyInfoId = (uint)PropertyInfoId.Community, HousingPropertyPlotIndex = 0 },
            new HousingPlotInfoEntry { Id = 4, HousingPropertyInfoId = (uint)PropertyInfoId.Community, HousingPropertyPlotIndex = 1 }));
        table.SetProperty(nameof(IGameTableManager.HousingPlugItem), PlayerBindPointTests.Table(new HousingPlugItemEntry { Id = 573 }));
        table.SetProperty(nameof(IGameTableManager.GuildStandardPart), PlayerBindPointTests.Table(
            new GuildStandardPartEntry { Id = 1, GuildStandardPartTypeEnum = 3 },
            new GuildStandardPartEntry { Id = 2, GuildStandardPartTypeEnum = 4 },
            new GuildStandardPartEntry { Id = 3, GuildStandardPartTypeEnum = 5 }));
        var database = interceptor == null ? new CharacterDatabase() : new InterceptedDatabase(source, interceptor);
        using (var seed = source.Context())
        {
            // Capture before the first open: MySqlConnector removes credentials afterwards.
            var configuration = new DatabaseConnectionString { Provider = seed.Database.IsSqlite() ? DatabaseProvider.Sqlite : DatabaseProvider.MySql,
                ConnectionString = seed.Database.GetDbConnection().ConnectionString };
            seed.Character.Single().AccountId = 12; seed.Character.Single().Name = "Leader";
            seed.CharacterCurrency.Add(new CharacterCurrencyModel { Id = 42, CurrencyId = 1, Amount = 5000 });
            await seed.SaveChangesAsync();
            database.Initialise(configuration);
        }
        var databases = RecordingDispatchProxy<IDatabaseManager>.Create(out var db);
        db.SetMethodReturn(nameof(IDatabaseManager.GetDatabase), database);
        var realm = RecordingDispatchProxy<IRealmContext>.Create(out var realmProxy); realmProxy.SetProperty(nameof(IRealmContext.RealmId), (ushort)1);
        var publisher = RecordingDispatchProxy<IInternalMessagePublisher>.Create(out var broker); broker.SetMethodReturn(nameof(IInternalMessagePublisher.PublishAsync), Task.CompletedTask);
        var filter = RecordingDispatchProxy<ITextFilterManager>.Create(out var text); text.SetMethodReturn(nameof(ITextFilterManager.IsTextValid), true);
        var characters = RecordingDispatchProxy<ICharacterManager>.Create(out var people);
        var character = RecordingDispatchProxy<ICharacter>.Create(out var person); person.SetProperty(nameof(ICharacter.Name), "Leader");
        people.SetMethodReturn(nameof(ICharacterManager.GetCharacter), character);
        var characterInfo = new AchievementInfo(new AchievementEntry { Id = 500, AchievementTypeId = (uint)AchievementType.GuildOrCircleJoin, RequiredProgress = 2 });
        var guildInfo = new AchievementInfo(new AchievementEntry { Id = 501, AchievementTypeId = (uint)AchievementType.GuildOrCircleJoin,
            RequiredProgress = completeGuildAchievement ? 1u : 2u, Flags = (uint)AchievementFlags.Guild });
        var feedbackInfo = new AchievementInfo(new AchievementEntry { Id = 502, AchievementTypeId = (uint)AchievementType.AchievementComplete,
            ObjectId = 501, RequiredProgress = 2 });
        var achievements = RecordingDispatchProxy<IGlobalAchievementManager>.Create(out var achievementCatalog);
        achievementCatalog.SetMethodHandler(nameof(IGlobalAchievementManager.GetAchievement), args => (ushort)args[0] switch { 500 => characterInfo, 501 => guildInfo, 502 => feedbackInfo, _ => null });
        achievementCatalog.SetMethodHandler(nameof(IGlobalAchievementManager.GetCharacterAchievements), args =>
            matchingAchievements && (AchievementType)args[0] == AchievementType.GuildOrCircleJoin ? new IAchievementInfo[] { characterInfo }
            : completeGuildAchievement && (AchievementType)args[0] == AchievementType.AchievementComplete ? new IAchievementInfo[] { feedbackInfo } : []);
        achievementCatalog.SetMethodHandler(nameof(IGlobalAchievementManager.GetGuildAchievements), args => matchingAchievements && (AchievementType)args[0] == AchievementType.GuildOrCircleJoin
            ? new IAchievementInfo[] { guildInfo } : []);
        GlobalGuildManager guilds = null;
        GlobalResidenceManager residences = null;
        var services = new ServiceCollection();
        services.AddKeyedTransient<IGuildBase>(GuildType.Guild, (_, _) => new GameGuild(realm, publisher, filter,
            characterManager: characters, gameTableManager: tables, globalGuildManager: guilds, globalAchievementManager: achievements,
            prerequisiteManager: achievementPrerequisites));
        services.AddKeyedTransient<IGuildBase>(GuildType.Community, (_, _) => new Community(realm, publisher, residences, filter, null, characters, guilds));
        var factory = new GuildFactory(services.BuildServiceProvider());
        guilds = new GlobalGuildManager(factory, databases); guilds.Initialise();
        residences = new GlobalResidenceManager(characters, guilds, realm, databases, gameTableManager: tables); residences.Initialise();
        var account = (RecordingDispatchProxy<IAccount>)(object)source.Source.Player.Account; account.SetProperty(nameof(IAccount.Id), 12u);
        TokenAuthStore tokenAuth = tokens ? new TokenAuthStore() : null;
        if (tokenAuth != null) account.SetProperty(nameof(IAccount.WorldSessionEpoch), 1ul);
        if (community)
        {
            var permissions = RecordingDispatchProxy<IAccountRBACManager>.Create(out var permission);
            permission.SetMethodReturn(nameof(IAccountRBACManager.HasPermission), true);
            account.SetProperty(nameof(IAccount.RbacManager), permissions);
        }
        var events = new EventQueue(); var session = RecordingDispatchProxy<IWorldSession>.Create(out var connection);
        connection.SetProperty(nameof(IWorldSession.Events), events); connection.SetProperty(nameof(IWorldSession.CanProcessIncomingPackets), true);
        account.SetProperty(nameof(IAccount.Session), session);
        CharacterModel model = Assert.Single(await database.GetCharacters(12));
        var actor = new Actor(source, actorModel: model, actorSession: session, actorEvents: events, characterDatabase: database, authDatabase: tokenAuth?.Database,
            inventoryFactory: (owner, _) => BankDeferredCurrencyRecoveryTests.Inventory(owner, (IItemManager)(object)source.Source.ManagerProxy, tables,
                new NexusForever.Game.RealmBank.RealmBankManager(databases)));
        actor.Set(nameof(Player.Name), "Leader"); actor.Set(nameof(Player.Guid), 4200u);
        if (community)
            ((Dictionary<Stat, IStatValue>)typeof(WorldEntity).GetField("stats", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(actor.Player))
                .Add(Stat.Level, new StatValue(Stat.Level, 50u));
        actor.Set(nameof(Player.ResidenceManager), new ResidenceManager(actor.Player, residences, characters));
        connection.SetProperty(nameof(IWorldSession.Player), actor.Player);
        connection.SetProperty(nameof(IWorldSession.Account), actor.Player.Account);
        typeof(GridEntity).GetField("visibleEntities", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(actor.Player, new Dictionary<uint, IGridEntity>());
        typeof(WorldEntity).GetField("itemVisuals", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(actor.Player, new Dictionary<ItemSlot, IItemVisual>());
        var currency = new CurrencyManager(tables); currency.Initialise(actor.Player, model); actor.Set(nameof(Player.CurrencyManager), currency);
        var membership = new GuildManager(publisher, filter, guilds, realm, gameTableManager: tables);
        actor.Set(nameof(Player.GuildManager), membership); membership.Initialise(actor.Player, model);
        actor.Set(nameof(Player.AchievementManager), new CharacterAchievementManager(actor.Player, model, globalAchievementManager: achievements,
            gameTableManager: tables, prerequisiteManager: achievementPrerequisites, playerManager: achievementPlayers));
        if (tokenAuth != null)
        {
            var wallets = new NexusForever.Game.Account.Currency.AccountCurrencyManager(actor.Player.Account, tokenAuth.Model, tables);
            account.SetProperty(nameof(IAccount.CurrencyManager), wallets);
            account.SetMethodHandler(nameof(IAccount.Save), args => { actor.OnAuth?.Invoke(); wallets.Save((NexusForever.Database.Auth.AuthContext)args[0]); return null; });
        }
        await actor.Load();
        return new(source, actor, currency, guilds, session, connection, tables, achievements, residences, community, tokenAuth);
    }

    private sealed record Fixture(SourceFixture Source, Actor Actor, CurrencyManager Currency, GlobalGuildManager Guilds,
        IWorldSession Session, RecordingDispatchProxy<IWorldSession> Messages, IGameTableManager Tables, IGlobalAchievementManager Achievements,
        GlobalResidenceManager Residences, bool Community, TokenAuthStore TokenAuth) : IDisposable
    {
        internal void Register()
        {
            var request = new ClientGuildRegister();
            foreach (var (key, value) in new (string, object)[] { (nameof(request.GuildType), Community ? GuildType.Community : GuildType.Guild), (nameof(request.GuildName), "Paid guild"),
                (nameof(request.MasterTitle), "Leader"), (nameof(request.CouncilTitle), "Council"), (nameof(request.MemberTitle), "Member") })
                typeof(ClientGuildRegister).GetProperty(key)!.SetValue(request, value);
            request.GuildStandard.BackgroundIcon.GuildStandardPartId = 1;
            request.GuildStandard.ForegroundIcon.GuildStandardPartId = 2;
            request.GuildStandard.ScanLines.GuildStandardPartId = 3;
            if (TokenAuth != null) typeof(ClientGuildRegister).GetProperty(nameof(request.AlternateCost))!.SetValue(request, true);
            new ClientGuildRegisterHandler(Tables).HandleMessage(Session, request);
        }
        internal IEnumerable<T> Packets<T>() => Messages.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)).Select(call => call.Arguments[0]).OfType<T>();
        internal async Task Flush()
        {
            var elapsed = Stopwatch.StartNew();
            var field = typeof(GlobalGuildManager).GetField("guildSavesInFlight", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var gate = typeof(GlobalGuildManager).GetField("cacheLock", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(Guilds);
            var creations = typeof(GlobalGuildManager).GetField("paidCreations", BindingFlags.Instance | BindingFlags.NonPublic)!;
            bool ObservingWallet()
            {
                lock (gate)
                    foreach (object creation in (System.Collections.IEnumerable)creations.GetValue(Guilds))
                    {
                        object payment = creation.GetType().GetField("tokenPayment", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(creation);
                        if (payment?.GetType().GetField("observation", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(payment) is Task { IsCompleted: false })
                            return true;
                    }
                return false;
            }
            // A completed event hands work back to the following world tick.
            // Drain each tick's SQL/events before advancing the coordinator again.
            for (int i = 0; i < 10; i++)
            {
                Guilds.Update(61);
                bool active;
                do
                {
                    Actor.Events.Update(.01); await Task.Delay(5);
                    lock (gate) active = ((HashSet<ulong>)field.GetValue(Guilds)).Count != 0;
                    Assert.True(elapsed.Elapsed < TimeSpan.FromSeconds(20), "Guild creation save/event work did not settle.");
                }
                while (active || Actor.Events.PendingEvents || ObservingWallet());
            }
            Assert.Empty(Actor.Errors);
        }
        public void Dispose() { Actor.Dispose(); Source.Dispose(); TokenAuth?.Dispose(); }
    }
}
