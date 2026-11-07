using System.Numerics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Prerequisite.Check;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Tests.TestSupport;

namespace NexusForever.Game.Tests.Entity;

// Native evaluator (Decomp/Analysis/PREREQUISITE_TYPE_COVERAGE_20261107.md): type 31 (FUN_14049ed50) walks the unit's nearby-entity list for an entity whose creature id equals
// ObjectId and whose 3D squared distance from the unit is at most Value squared; Equal returns found, NotEqual returns not found, other comparisons are false. Retail rows:
// 215 slots, comparisons Equal (112) and NotEqual (103), Value a distance such as 128, 10, 32, 20 or 100.
public class PrerequisiteCheckCreatureWithinDistanceTests
{
    private const uint CreatureId = 5150;

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, 10u, true)]     // creature is 5 away
    [InlineData(PrerequisiteComparison.Equal, 5u, true)]      // boundary is inclusive
    [InlineData(PrerequisiteComparison.Equal, 4u, false)]
    [InlineData(PrerequisiteComparison.NotEqual, 10u, false)]
    [InlineData(PrerequisiteComparison.NotEqual, 4u, true)]
    [InlineData(PrerequisiteComparison.GreaterThan, 10u, false)]
    [InlineData(PrerequisiteComparison.LessThan, 10u, false)]
    public void ComparesWhetherTheCreatureIsWithinTheDistance(PrerequisiteComparison comparison, uint distance, bool expected)
    {
        IPlayer player = Player(playerPosition: new Vector3(10, 0, 10), creaturePosition: new Vector3(13, 0, 14)); // 3-4-5 triangle

        Assert.Equal(expected, new PrerequisiteCheckCreatureWithinDistance().Meets(player, comparison, distance, CreatureId, new PrerequisiteParameters()));
    }

    [Fact]
    public void TheDistanceIsThreeDimensional()
    {
        IPlayer player = Player(playerPosition: new Vector3(0, 0, 0), creaturePosition: new Vector3(3, 12, 4)); // 13 away

        Assert.True(new PrerequisiteCheckCreatureWithinDistance().Meets(player, PrerequisiteComparison.Equal, 13u, CreatureId, new PrerequisiteParameters()));
        Assert.False(new PrerequisiteCheckCreatureWithinDistance().Meets(player, PrerequisiteComparison.Equal, 12u, CreatureId, new PrerequisiteParameters()));
    }

    [Fact]
    public void ADifferentCreatureIdOrNoVisibleCreatureIsNotFound()
    {
        IPlayer player = Player(playerPosition: Vector3.Zero, creaturePosition: Vector3.One);

        Assert.False(new PrerequisiteCheckCreatureWithinDistance().Meets(player, PrerequisiteComparison.Equal, 100u, CreatureId + 1, new PrerequisiteParameters()));
        Assert.True(new PrerequisiteCheckCreatureWithinDistance().Meets(player, PrerequisiteComparison.NotEqual, 100u, CreatureId + 1, new PrerequisiteParameters()));
    }

    [Fact]
    public void AnyOfSeveralMatchingCreaturesInRangeSatisfiesEqual()
    {
        IPlayer player = Player(playerPosition: Vector3.Zero, creaturePosition: new Vector3(500, 0, 0), extraPosition: new Vector3(2, 0, 0));

        Assert.True(new PrerequisiteCheckCreatureWithinDistance().Meets(player, PrerequisiteComparison.Equal, 10u, CreatureId, new PrerequisiteParameters()));
    }

    private static IPlayer Player(Vector3 playerPosition, Vector3 creaturePosition, Vector3? extraPosition = null)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Position), playerPosition);
        var creatures = new List<IWorldEntity> { Creature(creaturePosition) };
        if (extraPosition is { } extra)
            creatures.Add(Creature(extra));
        playerProxy.SetMethodHandler(nameof(IGridEntity.GetVisibleCreature), args => (uint)args[0] == CreatureId ? creatures : new List<IWorldEntity>());
        return player;
    }

    private static IWorldEntity Creature(Vector3 position)
    {
        IWorldEntity creature = RecordingDispatchProxy<IWorldEntity>.Create(out var proxy);
        proxy.SetProperty(nameof(IWorldEntity.Position), position);
        return creature;
    }
}
