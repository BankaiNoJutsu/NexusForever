using System.Numerics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;

namespace NexusForever.Game.Tests.Entity;

public class GridEntityRangeCheckTests
{
    [Fact]
    public void GetInRange_ReturnsOnlyEntitiesThatEnteredRange()
    {
        var source = new TestGridEntity(1u, Vector3.Zero);
        var inRange = new TestGridEntity(2u, new Vector3(5f, 0f, 0f));
        var outOfRange = new TestGridEntity(3u, new Vector3(25f, 0f, 0f));

        source.SetInRangeCheck(10f);
        source.AddVisible(inRange);
        source.AddVisible(outOfRange);

        TestGridEntity[] entities = source.GetInRange<TestGridEntity>(0u).ToArray();

        Assert.Single(entities);
        Assert.Same(inRange, entities[0]);
    }

    [Fact]
    public void SetInRangeCheck_AfterVisibleEntities_TracksPlayersAlreadyNearby()
    {
        var source = new TestGridEntity(1u, Vector3.Zero);
        var inRange = new TestGridEntity(2u, new Vector3(5f, 0f, 0f));
        var outOfRange = new TestGridEntity(3u, new Vector3(25f, 0f, 0f));

        source.AddVisible(inRange);
        source.AddVisible(outOfRange);

        source.SetInRangeCheck(10f);

        TestGridEntity[] entities = source.GetInRange<TestGridEntity>(0u).ToArray();

        Assert.Single(entities);
        Assert.Same(inRange, entities[0]);
    }

    private sealed class TestGridEntity : GridEntity
    {
        public TestGridEntity(uint guid, Vector3 position)
        {
            Guid = guid;
            Position = position;
        }
    }
}
