using System.Collections;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using NexusForever.Database;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Character;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Guild;
using NexusForever.Game.Guild;
using NexusForever.Game.Static.Achievement;
using NexusForever.Game.Static.Guild;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.GameTable.Text.Filter;
using NexusForever.Network.Internal;
using GameGuild = NexusForever.Game.Guild.Guild;

namespace NexusForever.Game.Tests.Achievement;

public sealed partial class RealmFirstArbitrationTests
{
    private const ushort GuildAchievementId = 501;

    [Fact]
    public async Task StaleGuildPeerCannotAnnounceASecondCommittedRealmFirst()
    {
        using var f = await CreateGuilds();
        f.First.AchievementManager.GrantAchievement(GuildAchievementId); await f.Save(f.First); await f.Pump();
        f.Second.AchievementManager.GrantAchievement(GuildAchievementId); await f.Save(f.Second); await f.Pump();
        AssertGuildWinner(f, f.First);
        using var sql = f.Characters.Source.Context();
        Assert.Equal(2, sql.GuildAchievement.Count(row => row.DateCompleted != null));
    }

    [Fact]
    public async Task RejectedGuildGraphCannotKeepTheWinnerAheadOfACommittedPeer()
    {
        using var f = await CreateGuilds();
        using (var setup = f.Characters.Source.Context())
            await setup.Database.ExecuteSqlRawAsync(setup.Database.IsSqlite()
                ? "CREATE TRIGGER reject_guild_claim BEFORE INSERT ON guild_achievement WHEN NEW.id=77 BEGIN SELECT RAISE(ABORT, 'owned guild realm-first failure'); END"
                : "CREATE TRIGGER reject_guild_claim BEFORE INSERT ON guild_achievement FOR EACH ROW BEGIN IF NEW.id=77 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='owned guild realm-first failure'; END IF; END");
        f.First.AchievementManager.GrantAchievement(GuildAchievementId);
        await Assert.ThrowsAsync<DbUpdateException>(() => f.Save(f.First)); await f.Pump();
        f.Second.AchievementManager.GrantAchievement(GuildAchievementId); await f.Save(f.Second); await f.Pump();
        using (var setup = f.Characters.Source.Context()) await setup.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_guild_claim");
        await f.Save(f.First); await f.Pump();
        AssertGuildWinner(f, f.Second);
    }

    [Fact]
    public async Task LostGuildAcknowledgementCannotReassignTheCommittedWinner()
    {
        using var f = await CreateGuilds();
        f.First.AchievementManager.GrantAchievement(GuildAchievementId);
        await Assert.ThrowsAsync<IOException>(() => f.Save(f.First, new LostAcknowledgement())); await f.Pump();
        Assert.All(f.Characters.Endpoints, endpoint => Assert.Empty(endpoint.Announcements));
        f.Second.AchievementManager.GrantAchievement(GuildAchievementId); await f.Save(f.Second); await f.Pump();
        Assert.All(f.Characters.Endpoints, endpoint => Assert.Empty(endpoint.Announcements));
        await f.Save(f.First); await f.Pump(); AssertGuildWinner(f, f.First);
    }

    private static void AssertGuildWinner(GuildPair f, GameGuild winner)
    {
        using var sql = f.Characters.Source.Context();
        Assert.Equal(winner.Id, Assert.Single(sql.RealmFirstAchievement.Where(row => row.IsGuild)).OwnerId);
        Assert.All(f.Characters.Endpoints, endpoint =>
        {
            var announcement = Assert.Single(endpoint.Announcements);
            Assert.True(announcement.IsGuildAchievement);
            Assert.Equal(GuildAchievementId, announcement.AchievementId);
            Assert.Equal(winner.Name, announcement.Name);
        });
    }

    private async Task<GuildPair> CreateGuilds()
    {
        var characters = await CreateCharacters();
        using (var seed = characters.Source.Context())
        {
            foreach ((ulong id, ulong leader) in new[] { (77ul, 42ul), (78ul, 43ul) })
                seed.Guild.Add(new GuildModel { Id = id, Type = (byte)GuildType.Guild, Name = "Guild" + id, LeaderId = leader,
                    GuildData = new GuildDataModel { Id = id, MessageOfTheDay = "MOTD", AdditionalInfo = "Info", BackgroundIconPartId = 1, ForegroundIconPartId = 2, ScanLinesPartId = 3 },
                    GuildRank = [new GuildRankModel { Id = id, Index = 0, Name = "Leader", Permission = (uint)GuildRankPermission.Leader }],
                    GuildMember = [new GuildMemberModel { Id = id, CharacterId = leader, Rank = 0 }] });
            await seed.SaveChangesAsync();
        }
        var databases = (IDatabaseManager)typeof(NexusForever.Game.Entity.Player).GetField("databaseManager", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(characters.First.Actor.Player)!;
        var realm = RecordingDispatchProxy<IRealmContext>.Create(out var realmProxy); realmProxy.SetProperty(nameof(IRealmContext.RealmId), (ushort)1);
        var publisher = RecordingDispatchProxy<IInternalMessagePublisher>.Create(out var messages); messages.SetMethodReturn(nameof(IInternalMessagePublisher.PublishAsync), Task.CompletedTask);
        var filter = RecordingDispatchProxy<ITextFilterManager>.Create(out var text); text.SetMethodReturn(nameof(ITextFilterManager.IsTextValid), true);
        var tables = RecordingDispatchProxy<IGameTableManager>.Create(out var table);
        table.SetProperty(nameof(IGameTableManager.Achievement), PlayerBindPointTests.Table(new AchievementEntry
            { Id = GuildAchievementId, Flags = (uint)AchievementFlags.Guild, AchievementTypeId = (uint)AchievementType.RealmFirst, RequiredProgress = 1 }));
        table.SetProperty(nameof(IGameTableManager.GuildStandardPart), PlayerBindPointTests.Table(new GuildStandardPartEntry { Id = 1 }, new GuildStandardPartEntry { Id = 2 }, new GuildStandardPartEntry { Id = 3 }));
        var players = RecordingDispatchProxy<IPlayerManager>.Create(out var playerRegistry);
        playerRegistry.SetMethodHandler(nameof(IEnumerable.GetEnumerator), _ => characters.Endpoints.Select(endpoint => (IPlayer)endpoint.Actor.Player).ToArray().AsEnumerable().GetEnumerator());
        playerRegistry.SetMethodHandler(nameof(IPlayerManager.GetPlayer), args => characters.Endpoints.SingleOrDefault(endpoint => args[0] is ulong id
            ? endpoint.Actor.Player.CharacterId == id : args[0] is Identity identity && endpoint.Actor.Player.Identity.Id == identity.Id && endpoint.Actor.Player.Identity.RealmId == identity.RealmId)?.Actor.Player);
        var characterManager = RecordingDispatchProxy<ICharacterManager>.Create(out var characterLookup);
        characterLookup.SetMethodHandler(nameof(ICharacterManager.GetCharacter), args =>
        {
            var character = RecordingDispatchProxy<ICharacter>.Create(out var value); value.SetProperty(nameof(ICharacter.Name), "Owner" + args[0]); return character;
        });
        ServiceProvider Node()
        {
            var global = new NexusForever.Game.Achievement.GlobalAchievementManager(databases, tables); global.Initialise();
            var services = new ServiceCollection();
            services.AddSingleton<IGuildFactory, GuildFactory>();
            services.AddSingleton<IGlobalGuildManager>(provider => new GlobalGuildManager(provider.GetRequiredService<IGuildFactory>(), databases));
            services.AddKeyedTransient<IGuildBase>(GuildType.Guild, (provider, _) => new GameGuild(realm, publisher, filter,
                characterManager: characterManager, gameTableManager: tables, globalGuildManager: provider.GetRequiredService<IGlobalGuildManager>(),
                playerManager: players, globalAchievementManager: global));
            var built = services.BuildServiceProvider(); built.GetRequiredService<IGlobalGuildManager>().Initialise(); return built;
        }
        var firstServices = Node(); var secondServices = Node();
        var firstManager = (GlobalGuildManager)firstServices.GetRequiredService<IGlobalGuildManager>();
        var secondManager = (GlobalGuildManager)secondServices.GetRequiredService<IGlobalGuildManager>();
        var first = Assert.IsType<GameGuild>(firstManager.GetGuild(77)); var second = Assert.IsType<GameGuild>(secondManager.GetGuild(78));
        foreach ((Endpoint endpoint, GameGuild guild) in new[] { (characters.First, first), (characters.Second, second) })
        {
            ((RecordingDispatchProxy<IGuildManager>)(object)endpoint.Actor.Player.GuildManager).SetProperty(nameof(IGuildManager.GuildAffiliation), guild);
            guild.OnPlayerLogin(endpoint.Actor.Player);
        }
        return new(characters, firstServices, secondServices, firstManager, secondManager, first, second);
    }

    private sealed record GuildPair(CharacterPair Characters, ServiceProvider FirstServices, ServiceProvider SecondServices,
        GlobalGuildManager FirstManager, GlobalGuildManager SecondManager, GameGuild First, GameGuild Second) : IDisposable
    {
        internal async Task Save(GameGuild guild, params IInterceptor[] interceptors)
        {
            using var context = Characters.Source.Context(interceptors); guild.Save(context); await context.SaveChangesAsync();
        }
        internal async Task Pump()
        {
            await First.PublishMutationsAsync(); await Second.PublishMutationsAsync(); Characters.Pump();
        }
        public void Dispose() { FirstServices.Dispose(); SecondServices.Dispose(); Characters.Dispose(); }
    }
}
