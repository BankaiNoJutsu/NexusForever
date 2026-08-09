using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NexusForever.Database.Query;
using NexusForever.Database.Query.Model;
using NexusForever.Database.Query.Repository;
using NexusForever.Database.Query.Repository.Query;
using NexusForever.Game.Static.Entity;
using NexusForever.Network.Internal.Message.Who;
using NexusForever.Network.Internal.Message.Who.Parameter;
using NexusForever.Server.Character.Configuration;
using NexusForever.Server.Character.Game.Who;
using PlayerClass = NexusForever.Game.Static.Entity.Class;
using PlayerPath = NexusForever.Game.Static.PlayerPath.Path;

namespace NexusForever.Game.Tests.Chat;

public class ClientWhoRequestHandlerTests
{
    [Fact]
    public async Task QueryAsync_RestrictsResultsToRequestedRealm()
    {
        var options = new DbContextOptionsBuilder<QueryContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        await using var context = new QueryContext(options);
        await context.Database.OpenConnectionAsync();
        await context.Database.EnsureCreatedAsync();
        context.Character.AddRange(
            new CharacterModel { CharacterId = 1u, RealmId = 1, Name = "Local" },
            new CharacterModel { CharacterId = 2u, RealmId = 2, Name = "Remote" });
        await context.SaveChangesAsync();

        var repository = new QueryRepository(context, new QueryExpressionBuilder());
        List<CharacterModel> matches = await repository.QueryAsync(new Query { RealmId = 1, MaxResults = 10u });

        CharacterModel match = Assert.Single(matches);
        Assert.Equal("Local", match.Name);
    }

    [Fact]
    public void Build_WithNoParametersDoesNotAddAnEmptyGroup()
    {
        Query query = CreateQuery(new WhoRequestMessage());

        Assert.Empty(query.Groups);
        Assert.True(new QueryExpressionBuilder().Build(query).Compile()(CreateCharacter("Player")));
    }

    [Fact]
    public void Build_WithPlayerParameterSearchesNamesAndGuildsAcrossZones()
    {
        WhoRequestMessage request = CreateRequest(new WhoParameterPlayer { PlayerName = "Alpha" });

        IReadOnlyList<CharacterModel> matches = Filter(
            request,
            CreateCharacter("Alpha", zoneId: 20),
            CreateCharacter("Other", guildName: "Alpha Squad", zoneId: 30),
            CreateCharacter("Beta", guildName: "Other Guild", zoneId: 20));

        Assert.Equal(["Alpha", "Other"], matches.Select(character => character.Name));
    }

    [Fact]
    public void Build_WithLevelParameterTreatsTopLevelAsExclusive()
    {
        WhoRequestMessage request = CreateRequest(new WhoParameterLevel
        {
            BottomLevel = 10,
            TopLevel = 20
        });

        IReadOnlyList<CharacterModel> matches = Filter(
            request,
            CreateCharacter("Below", level: 9),
            CreateCharacter("Inside", level: 19),
            CreateCharacter("AtTop", level: 20));

        CharacterModel match = Assert.Single(matches);
        Assert.Equal("Inside", match.Name);
    }

    [Fact]
    public void Build_WithParameterGroupCountsUsesCumulativeOffsets()
    {
        WhoRequestMessage request = CreateRequest(
            new WhoParameterRace { RaceId = Race.Aurin },
            new WhoParameterClass { ClassId = PlayerClass.Esper },
            new WhoParameterRace { RaceId = Race.Granok },
            new WhoParameterPath { PathId = PlayerPath.Settler },
            new WhoParameterPlayer { PlayerName = "Remote" });
        request.ParameterGroupCounts.Clear();
        request.ParameterGroupCounts.AddRange([2, 4]);

        IReadOnlyList<CharacterModel> matches = Filter(
            request,
            CreateCharacter("AurinEsper", race: Race.Aurin, playerClass: PlayerClass.Esper),
            CreateCharacter("AurinMedic", race: Race.Aurin, playerClass: PlayerClass.Medic),
            CreateCharacter("GranokSettler", race: Race.Granok, path: PlayerPath.Settler),
            CreateCharacter("RemoteName", race: Race.Mechari, path: PlayerPath.Scientist));

        Assert.Equal(["AurinEsper", "GranokSettler", "RemoteName"], matches.Select(character => character.Name));
    }

    [Fact]
    public void Build_WithCompletedGroupDoesNotAppendAnEmptyGroup()
    {
        WhoRequestMessage request = CreateRequest(new WhoParameterRace { RaceId = Race.Aurin });

        Query query = CreateQuery(request);

        Assert.Single(query.Groups);
        Assert.NotNull(new QueryExpressionBuilder().Build(query));
    }

    [Fact]
    public void Build_WithComboPathCanMatchSoldier()
    {
        WhoRequestMessage request = CreateRequest(new WhoParameterCombo
        {
            SearchString = "Soldier",
            PathId = PlayerPath.Soldier
        });

        IReadOnlyList<CharacterModel> matches = Filter(
            request,
            CreateCharacter("SoldierPath", path: PlayerPath.Soldier),
            CreateCharacter("SettlerPath", path: PlayerPath.Settler));

        CharacterModel match = Assert.Single(matches);
        Assert.Equal("SoldierPath", match.Name);
    }

    [Fact]
    public void Build_WithComboRequiresAllProvidedFields()
    {
        WhoRequestMessage request = CreateRequest(new WhoParameterCombo
        {
            SearchString = "Matching",
            RaceId = Race.Aurin,
            PathId = PlayerPath.Soldier,
            ClassId = PlayerClass.Esper,
            WorldZoneId = 40
        });

        IReadOnlyList<CharacterModel> matches = Filter(
            request,
            CreateCharacter("MatchingEsper", race: Race.Aurin, path: PlayerPath.Soldier, playerClass: PlayerClass.Esper, zoneId: 40),
            CreateCharacter("MatchingRace", race: Race.Human, path: PlayerPath.Soldier, playerClass: PlayerClass.Esper, zoneId: 40),
            CreateCharacter("MatchingPath", race: Race.Aurin, path: PlayerPath.Explorer, playerClass: PlayerClass.Esper, zoneId: 40),
            CreateCharacter("MatchingClass", race: Race.Aurin, path: PlayerPath.Soldier, playerClass: PlayerClass.Engineer, zoneId: 40),
            CreateCharacter("MatchingZone", race: Race.Aurin, path: PlayerPath.Soldier, playerClass: PlayerClass.Esper, zoneId: 50),
            CreateCharacter("OtherName", race: Race.Aurin, path: PlayerPath.Soldier, playerClass: PlayerClass.Esper, zoneId: 40));

        CharacterModel match = Assert.Single(matches);
        Assert.Equal("MatchingEsper", match.Name);
    }

    [Fact]
    public void Build_WithComboWorldZonesMatchesEitherProvidedZone()
    {
        WhoRequestMessage request = CreateRequest(new WhoParameterCombo
        {
            WorldZoneId = 40,
            WorldZoneId2 = 50
        });

        IReadOnlyList<CharacterModel> matches = Filter(
            request,
            CreateCharacter("FirstZone", zoneId: 40),
            CreateCharacter("SecondZone", zoneId: 50),
            CreateCharacter("OtherZone", zoneId: 60));

        Assert.Equal(["FirstZone", "SecondZone"], matches.Select(character => character.Name));
    }

    private static IReadOnlyList<CharacterModel> Filter(
        WhoRequestMessage request,
        params CharacterModel[] characters)
    {
        Query query = CreateQuery(request);
        Func<CharacterModel, bool> expression = new QueryExpressionBuilder().Build(query).Compile();
        return characters.Where(expression).ToList();
    }

    private static Query CreateQuery(WhoRequestMessage request)
    {
        var builder = new QueryBuilder(Options.Create(new WhoOptions { MaxResults = 100 }));
        return builder.Build(request);
    }

    private static WhoRequestMessage CreateRequest(params IWhoParameter[] parameters)
    {
        var request = new WhoRequestMessage();
        request.Parameters.AddRange(parameters);
        request.ParameterGroupCounts.Add((uint)parameters.Length);
        return request;
    }

    private static CharacterModel CreateCharacter(
        string name,
        string guildName = null,
        Race race = Race.Human,
        PlayerPath path = PlayerPath.Explorer,
        PlayerClass playerClass = PlayerClass.Warrior,
        ushort zoneId = 10,
        uint level = 20)
    {
        return new CharacterModel
        {
            Name = name,
            GuildName = guildName,
            Race = race,
            Path = path,
            Class = playerClass,
            WorldZoneId = zoneId,
            Level = level
        };
    }
}
