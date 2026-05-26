using System.Collections.Immutable;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Character;
using NexusForever.Game.Entity;
using NexusForever.Game.Static;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Reputation;
using NexusForever.GameTable.Model;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

namespace NexusForever.Game.Tests.Character;

public class CharacterCreationStartLocationTests
{
    private const float Tolerance = 0.0001f;

    [Theory]
    [MemberData(nameof(Build16042CreationStartLocations))]
    public void Build16042CreationStartsMatchCharacterCreateSeeds(ExpectedStartLocation expected)
    {
        IReadOnlyDictionary<(Race Race, Faction Faction, CharacterCreationStart Start), CharacterCreateModel> rows = GetCharacterCreationRows();

        Assert.True(rows.TryGetValue((expected.Race, expected.Faction, expected.Start), out CharacterCreateModel row));
        Assert.Equal(expected.WorldId, row.WorldId);
        Assert.Equal(expected.X, row.X, Tolerance);
        Assert.Equal(expected.Y, row.Y, Tolerance);
        Assert.Equal(expected.Z, row.Z, Tolerance);
        Assert.Equal(expected.Rx, row.Rx, Tolerance);
        Assert.Equal(expected.Ry, row.Ry, Tolerance);
        Assert.Equal(expected.Rz, row.Rz, Tolerance);
    }

    [Fact]
    public void CharacterManagerStartingLocationLookupMatchesBuild16042CreationStarts()
    {
        CharacterManager characterManager = CreateCharacterManagerWithSeededStartLocations();

        foreach (ExpectedStartLocation expected in Build16042ExpectedStartLocations())
        {
            ILocation location = characterManager.GetStartingLocation(expected.Race, expected.Faction, expected.Start);

            Assert.NotNull(location);
            Assert.Equal(expected.WorldId, location.World.Id);
            Assert.Equal(expected.X, location.Position.X, Tolerance);
            Assert.Equal(expected.Y, location.Position.Y, Tolerance);
            Assert.Equal(expected.Z, location.Position.Z, Tolerance);
            Assert.Equal(expected.Rx, location.Rotation.X, Tolerance);
            Assert.Equal(expected.Ry, location.Rotation.Y, Tolerance);
            Assert.Equal(expected.Rz, location.Rotation.Z, Tolerance);
        }
    }

    [Theory]
    [InlineData(CharacterCreationStart.Arkship)]
    [InlineData(CharacterCreationStart.Demo01)]
    [InlineData(CharacterCreationStart.Demo02)]
    [InlineData(CharacterCreationStart.CostumeOnly)]
    public void NonRetailBuild16042StartsDoNotHaveCharacterCreateSeeds(CharacterCreationStart start)
    {
        IReadOnlyDictionary<(Race Race, Faction Faction, CharacterCreationStart Start), CharacterCreateModel> rows = GetCharacterCreationRows();

        Assert.DoesNotContain(rows.Keys, key => key.Start == start);
    }

    public static IEnumerable<object[]> Build16042CreationStartLocations()
    {
        foreach (ExpectedStartLocation expected in Build16042ExpectedStartLocations())
            yield return [expected];
    }

    private static IEnumerable<ExpectedStartLocation> Build16042ExpectedStartLocations()
    {
        ExpectedStartLocation novice = new(Race.Human, Faction.Exile, CharacterCreationStart.PreTutorial, 3460u, 29.1286f, -853.8716f, -560.188f, -2.751458f, 0f, 0f);
        foreach ((Race race, Faction faction) in PlayableBuild16042RaceFactions())
            yield return novice with { Race = race, Faction = faction };

        yield return new ExpectedStartLocation(Race.Human, Faction.Exile, CharacterCreationStart.Nexus, 426u, 4110.71f, -658.6249f, -5145.48f, 0.317613f, 0f, 0f);
        yield return new ExpectedStartLocation(Race.Granok, Faction.Exile, CharacterCreationStart.Nexus, 426u, 4110.71f, -658.6249f, -5145.48f, 0.317613f, 0f, 0f);
        yield return new ExpectedStartLocation(Race.Aurin, Faction.Exile, CharacterCreationStart.Nexus, 990u, -771.823f, -904.2852f, -2269.56f, -1.1214035f, 0f, 0f);
        yield return new ExpectedStartLocation(Race.Mordesh, Faction.Exile, CharacterCreationStart.Nexus, 990u, -771.823f, -904.2852f, -2269.56f, -1.1214035f, 0f, 0f);
        yield return new ExpectedStartLocation(Race.Chua, Faction.Dominion, CharacterCreationStart.Nexus, 870u, -8261.3984f, -995.471f, -242.3648f, -2.215535f, 0f, 0f);
        yield return new ExpectedStartLocation(Race.Draken, Faction.Dominion, CharacterCreationStart.Nexus, 870u, -8261.3984f, -995.471f, -242.3648f, -2.215535f, 0f, 0f);
        yield return new ExpectedStartLocation(Race.Human, Faction.Dominion, CharacterCreationStart.Nexus, 1387u, -3835.341f, -980.2174f, -6050.524f, -0.456820f, 0f, 0f);
        yield return new ExpectedStartLocation(Race.Mechari, Faction.Dominion, CharacterCreationStart.Nexus, 1387u, -3835.341f, -980.2174f, -6050.524f, -0.456820f, 0f, 0f);

        yield return new ExpectedStartLocation(Race.Human, Faction.Exile, CharacterCreationStart.Level50, 51u, 4074.34f, -797.8368f, -2399.37f, 0f, 0f, 0f);
        yield return new ExpectedStartLocation(Race.Human, Faction.Dominion, CharacterCreationStart.Level50, 22u, -3343.58f, -887.4646f, -536.03f, -0.7632219f, 0f, 0f);
    }

    private static IEnumerable<(Race Race, Faction Faction)> PlayableBuild16042RaceFactions()
    {
        yield return (Race.Human, Faction.Exile);
        yield return (Race.Granok, Faction.Exile);
        yield return (Race.Aurin, Faction.Exile);
        yield return (Race.Mordesh, Faction.Exile);
        yield return (Race.Chua, Faction.Dominion);
        yield return (Race.Draken, Faction.Dominion);
        yield return (Race.Human, Faction.Dominion);
        yield return (Race.Mechari, Faction.Dominion);
    }

    private static IReadOnlyDictionary<(Race Race, Faction Faction, CharacterCreationStart Start), CharacterCreateModel> GetCharacterCreationRows()
    {
        DbContextOptions<CharacterContext> options = new DbContextOptionsBuilder<CharacterContext>()
            .UseMySql(
                "Server=127.0.0.1;Database=nexus_forever_character;User ID=nexus_forever;Password=nexus_forever;",
                new MySqlServerVersion(new Version(8, 0, 0)))
            .Options;

        using CharacterContext context = new(options);
        IModel designTimeModel = context.GetService<IDesignTimeModel>().Model;

        return designTimeModel.FindEntityType(typeof(CharacterCreateModel))
            .GetSeedData()
            .Select(ToCharacterCreateModel)
            .ToDictionary(r => ((Race)r.Race, (Faction)r.Faction, (CharacterCreationStart)r.CreationStart));
    }

    private static CharacterManager CreateCharacterManagerWithSeededStartLocations()
    {
        ImmutableDictionary<(Race, Faction, CharacterCreationStart), ILocation>.Builder builder = ImmutableDictionary.CreateBuilder<(Race, Faction, CharacterCreationStart), ILocation>();
        foreach (CharacterCreateModel row in GetCharacterCreationRows().Values)
        {
            var location = new Location(
                new WorldEntry { Id = row.WorldId },
                new(row.X, row.Y, row.Z),
                new(row.Rx, row.Ry, row.Rz));

            builder.Add(((Race)row.Race, (Faction)row.Faction, (CharacterCreationStart)row.CreationStart), location);
        }

        var characterManager = new CharacterManager();
        FieldInfo field = typeof(CharacterManager).GetField("characterCreationData", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        field.SetValue(characterManager, builder.ToImmutable());

        return characterManager;
    }

    private static CharacterCreateModel ToCharacterCreateModel(IDictionary<string, object> row)
    {
        return new CharacterCreateModel
        {
            Race          = Convert.ToByte(row[nameof(CharacterCreateModel.Race)]),
            Faction       = Convert.ToUInt16(row[nameof(CharacterCreateModel.Faction)]),
            CreationStart = Convert.ToByte(row[nameof(CharacterCreateModel.CreationStart)]),
            WorldId       = Convert.ToUInt32(row[nameof(CharacterCreateModel.WorldId)]),
            X             = Convert.ToSingle(row[nameof(CharacterCreateModel.X)]),
            Y             = Convert.ToSingle(row[nameof(CharacterCreateModel.Y)]),
            Z             = Convert.ToSingle(row[nameof(CharacterCreateModel.Z)]),
            Rx            = Convert.ToSingle(row[nameof(CharacterCreateModel.Rx)]),
            Ry            = Convert.ToSingle(row[nameof(CharacterCreateModel.Ry)]),
            Rz            = Convert.ToSingle(row[nameof(CharacterCreateModel.Rz)]),
            Comment       = Convert.ToString(row[nameof(CharacterCreateModel.Comment)])
        };
    }

    public readonly record struct ExpectedStartLocation(
        Race Race,
        Faction Faction,
        CharacterCreationStart Start,
        uint WorldId,
        float X,
        float Y,
        float Z,
        float Rx,
        float Ry,
        float Rz);
}
