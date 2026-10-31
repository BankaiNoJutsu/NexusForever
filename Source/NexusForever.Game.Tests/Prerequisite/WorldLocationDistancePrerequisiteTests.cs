using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Prerequisite.Check;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Tests.Prerequisite;

public class WorldLocationDistancePrerequisiteTests
{
    [Theory]
    [InlineData(1, 5u, true)]
    [InlineData(1, 4u, false)]
    [InlineData(2, 5u, false)]
    [InlineData(2, 4u, true)]
    [InlineData(3, 4u, true)]
    [InlineData(3, 5u, false)]
    [InlineData(4, 5u, true)]
    [InlineData(4, 6u, false)]
    [InlineData(5, 6u, true)]
    [InlineData(5, 5u, false)]
    [InlineData(6, 5u, true)]
    [InlineData(6, 4u, false)]
    [InlineData(0, 5u, false)]
    [InlineData(7, 5u, false)]
    public void NativeRawComparisonsUseFullThreeDimensionalDistance(int mode, uint value, bool expected)
    {
        var check = Check(Location());
        Assert.Equal(expected, check.Meets(Player(new Vector3(3, 4, 0)), (PrerequisiteComparison)mode, value, 10, null));
    }

    [Fact]
    public void PreservesFractionsAndIgnoresRadiusVerticalBoundsAndTargetPosition()
    {
        var location = Location();
        location.Radius = 100;
        location.MaxVerticalDistance = 1;
        var check = Check(location);
        var parameters = new PrerequisiteParameters { Target = Player(Vector3.Zero) };
        Assert.True(check.Meets(Player(new Vector3(0, 5.5f, 0)), PrerequisiteComparison.GreaterThan, 5, 10, parameters));
        Assert.False(check.Meets(Player(new Vector3(0, 5.5f, 0)), PrerequisiteComparison.Equal, 5, 10, parameters));
    }

    [Fact]
    public void AllThreeAxesSubtractTheLocationCenter()
    {
        var location = Location();
        location.Position0 = 100;
        location.Position1 = -200;
        location.Position2 = 300;
        var check = Check(location);
        Assert.True(check.Meets(Player(new Vector3(103, -196, 312)), PrerequisiteComparison.Equal, 13, 10, null));
        Assert.False(check.Meets(Player(new Vector3(103, -196, 312)), PrerequisiteComparison.Equal, 5, 10, null));
    }

    [Theory]
    [InlineData(0xffffffffu, true, false)]
    [InlineData(0x80000000u, true, false)]
    [InlineData(0x7fffffffu, false, true)]
    public void ThresholdUsesSignedInt32Conversion(uint value, bool greater, bool less)
    {
        var check = Check(Location());
        Assert.Equal(greater, check.Meets(Player(Vector3.Zero), PrerequisiteComparison.GreaterThan, value, 10, null));
        Assert.Equal(less, check.Meets(Player(Vector3.Zero), PrerequisiteComparison.LessThan, value, 10, null));
    }

    [Fact]
    public void ThresholdRetainsNativeSinglePrecisionRounding()
    {
        Assert.True(Check(Location()).Meets(Player(new Vector3(16777216, 0, 0)), PrerequisiteComparison.Equal, 16777217, 10, null));
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, true)]
    [InlineData(3, false)]
    [InlineData(4, false)]
    [InlineData(5, false)]
    [InlineData(6, false)]
    [InlineData(7, false)]
    public void NaNCoordinatesUseIeeeComparison(int mode, bool expected)
    {
        Assert.Equal(expected, Check(Location()).Meets(Player(new Vector3(float.NaN, 0, 0)), (PrerequisiteComparison)mode, 5, 10, null));
    }

    [Fact]
    public void InfiniteDistanceDoesNotBecomeAnIntegerOrClampToZero()
    {
        var check = Check(Location());
        Assert.True(check.Meets(Player(new Vector3(float.PositiveInfinity, 0, 0)), PrerequisiteComparison.GreaterThan, int.MaxValue, 10, null));
        Assert.False(check.Meets(Player(new Vector3(float.PositiveInfinity, 0, 0)), PrerequisiteComparison.LessThan, int.MaxValue, 10, null));
    }

    [Fact]
    public void MissingWorldRowTableOrSubjectFailsEvenForNotEqual()
    {
        var check = Check(Location());
        Assert.False(check.Meets(null, PrerequisiteComparison.NotEqual, 1, 10, null));
        Assert.False(check.Meets(Player(Vector3.Zero, 2), PrerequisiteComparison.NotEqual, 1, 10, null));
        Assert.False(check.Meets(Player(Vector3.Zero), PrerequisiteComparison.NotEqual, 1, 11, null));
        Assert.False(Check().Meets(Player(Vector3.Zero), PrerequisiteComparison.NotEqual, 1, 10, null));
        Assert.False(new PrerequisiteCheckDistanceToWorldLocation(null).Meets(Player(Vector3.Zero), PrerequisiteComparison.NotEqual, 1, 10, null));
        var player = Player(Vector3.Zero);
        ((RecordingDispatchProxy<IPlayer>)(object)player).SetProperty(nameof(IPlayer.Map), null);
        Assert.False(check.Meets(player, PrerequisiteComparison.NotEqual, 1, 10, null));
        var map = RecordingDispatchProxy<IBaseMap>.Create(out _);
        ((RecordingDispatchProxy<IPlayer>)(object)player).SetProperty(nameof(IPlayer.Map), map);
        Assert.False(check.Meets(player, PrerequisiteComparison.NotEqual, 1, 10, null));
    }

    [Fact]
    public void RealTableWitnessResolvesThroughRawType16ForPlayerAndNpc()
    {
        // Shipped Prerequisite 2048: type16/comparison4/value50/location6956.
        var location = new WorldLocation2Entry
        {
            Id = 6956, WorldId = 22, Radius = 50, MaxVerticalDistance = 0,
            Position0 = -5685.96484375f, Position1 = -998.722900390625f, Position2 = 115.67254638671875f
        };
        var tables = RecordingDispatchProxy<IGameTableManager>.Create(out var proxy);
        proxy.SetProperty(nameof(IGameTableManager.WorldLocation2), Table(location));
        proxy.SetProperty(nameof(IGameTableManager.Prerequisite), Table(new PrerequisiteEntry
        {
            Id = 2048, Flags = EvaluationMode.EvaluateAND,
            PrerequisiteTypeId = [(PrerequisiteType)16], PrerequisiteComparisonId = [(PrerequisiteComparison)4],
            ObjectId = [6956], Value = [50]
        }));
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(tables);
        services.AddGamePrerequisite();
        using var provider = services.BuildServiceProvider();
        Assert.IsType<PrerequisiteCheckDistanceToWorldLocation>(provider.GetRequiredKeyedService<IPrerequisiteCheck>((PrerequisiteType)16));
        var manager = provider.GetRequiredService<IPrerequisiteManager>();
        var position = new Vector3(location.Position0 + 30, location.Position1 + 40, location.Position2);
        var parameters = new PrerequisiteParameters { Target = Player(Vector3.Zero, 999) };
        Assert.True(manager.Meets(Player(position, 22), 2048, parameters));
        Assert.False(manager.Meets(Player(position - new Vector3(1, 0, 0), 22), 2048, parameters));
        var npc = RecordingDispatchProxy<IUnitEntity>.Create(out var npcProxy);
        npcProxy.SetProperty(nameof(IUnitEntity.Position), position);
        npcProxy.SetProperty(nameof(IUnitEntity.Map), Map(22));
        Assert.True(manager.MeetsUnit(npc, 2048, parameters));
    }

    private static WorldLocation2Entry Location() => new() { Id = 10, WorldId = 1 };

    private static PrerequisiteCheckDistanceToWorldLocation Check(WorldLocation2Entry location = null)
    {
        var tables = RecordingDispatchProxy<IGameTableManager>.Create(out var proxy);
        if (location != null)
            proxy.SetProperty(nameof(IGameTableManager.WorldLocation2), Table(location));
        return new PrerequisiteCheckDistanceToWorldLocation(tables);
    }

    private static IPlayer Player(Vector3 position, uint world = 1)
    {
        var player = RecordingDispatchProxy<IPlayer>.Create(out var proxy);
        proxy.SetProperty(nameof(IPlayer.Position), position);
        proxy.SetProperty(nameof(IPlayer.Map), Map(world));
        return player;
    }

    private static IBaseMap Map(uint world)
    {
        var map = RecordingDispatchProxy<IBaseMap>.Create(out var proxy);
        proxy.SetProperty(nameof(IBaseMap.Entry), new WorldEntry { Id = world });
        return map;
    }

    private static GameTable<T> Table<T>(T entry) where T : class, new()
    {
        var table = (GameTable<T>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<T>));
        typeof(GameTable<T>).GetProperty(nameof(GameTable<T>.Entries)).SetValue(table, new[] { entry });
        uint id = (uint)typeof(T).GetField("Id").GetValue(entry);
        var lookup = Enumerable.Repeat(-1, checked((int)id + 1)).ToArray();
        lookup[id] = 0;
        typeof(GameTable<T>).GetField("lookup", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(table, lookup);
        typeof(GameTable<T>).GetField("header", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(table, new GameTableHeader { MaxId = id + 1u });
        return table;
    }
}
