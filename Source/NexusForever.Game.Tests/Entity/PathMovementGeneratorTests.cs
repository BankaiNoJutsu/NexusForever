using System.Numerics;
using NexusForever.Game.Entity.Movement.Generator;

namespace NexusForever.Game.Tests.Entity;

public class PathMovementGeneratorTests
{
    [Fact]
    public void CalculatePath_InterpolatesVerticalTravelWithoutMap()
    {
        var generator = new PathMovementGenerator
        {
            Begin = new Vector3(0f, 2f, 0f),
            Final = new Vector3(5f, 8f, 0f)
        };

        List<Vector3> path = generator.CalculatePath();

        Assert.Equal(4, path.Count);
        AssertVector(path[0], 0f, 2f, 0f);
        AssertVector(path[1], 2f, 4.4f, 0f);
        AssertVector(path[2], 4f, 6.8f, 0f);
        AssertVector(path[3], 5f, 8f, 0f);
    }

    [Fact]
    public void CalculatePath_ExactStepDistance_DoesNotDuplicateFinalPoint()
    {
        var generator = new PathMovementGenerator
        {
            Begin = Vector3.Zero,
            Final = new Vector3(4f, 0f, 0f)
        };

        List<Vector3> path = generator.CalculatePath();

        Assert.Equal(3, path.Count);
        AssertVector(path[0], 0f, 0f, 0f);
        AssertVector(path[1], 2f, 0f, 0f);
        AssertVector(path[2], 4f, 0f, 0f);
    }

    private static void AssertVector(Vector3 actual, float expectedX, float expectedY, float expectedZ)
    {
        Assert.Equal(expectedX, actual.X, 5);
        Assert.Equal(expectedY, actual.Y, 5);
        Assert.Equal(expectedZ, actual.Z, 5);
    }
}
