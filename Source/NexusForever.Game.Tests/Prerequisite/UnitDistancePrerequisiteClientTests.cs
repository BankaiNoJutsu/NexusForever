using System.Numerics;
using Microsoft.Extensions.DependencyInjection;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Prerequisite.Check;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Tests.TestSupport;

namespace NexusForever.Game.Tests.Prerequisite;

public class UnitDistancePrerequisiteClientTests
{
    [Theory]
    [InlineData(1, false)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    [InlineData(4, true)]
    [InlineData(5, false)]
    [InlineData(6, false)]
    [InlineData(0, false)]
    [InlineData(7, false)]
    public void FractionalDistanceDoesNotFloorTheSquareRoot(int mode, bool expected)
    {
        Assert.Equal(expected, Meets(Vector3.Zero, new Vector3(5.1f, 0, 0), (PrerequisiteComparison)mode, 5));
    }

    [Fact]
    public void TruncationOccursAfterSquaringRatherThanComparingFloatDistance()
    {
        Assert.True(Meets(Vector3.Zero, new Vector3(5.01f, 0, 0), PrerequisiteComparison.Equal, 5));
        Assert.False(Meets(Vector3.Zero, new Vector3(5.01f, 0, 0), PrerequisiteComparison.GreaterThan, 5));
        Assert.True(Meets(Vector3.Zero, new Vector3(5.1f, 0, 0), PrerequisiteComparison.GreaterThan, 5));
    }

    [Fact]
    public void IncludesAllAxesAndBothUnitPositionsWithoutWorldOrObjectIdLookup()
    {
        Assert.True(Meets(new Vector3(100, -200, 300), new Vector3(103, -196, 312), PrerequisiteComparison.Equal, 13, uint.MaxValue));
        Assert.False(Meets(new Vector3(100, -200, 300), new Vector3(103, -196, 312), PrerequisiteComparison.Equal, 5));
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(2, true)]
    [InlineData(3, false)]
    [InlineData(4, false)]
    [InlineData(5, false)]
    [InlineData(6, false)]
    [InlineData(7, false)]
    public void MissingTargetPassesOnlyNativeNotEqual(int mode, bool expected)
    {
        var check = new PrerequisiteCheckDistance();
        var subject = Unit(Vector3.Zero);
        Assert.Equal(expected, check.MeetsUnit(subject, (PrerequisiteComparison)mode, 1, 0, null));
        Assert.Equal(expected, check.MeetsUnit(subject, (PrerequisiteComparison)mode, 1, 0, new PrerequisiteParameters()));
        Assert.False(check.MeetsUnit(null, (PrerequisiteComparison)mode, 1, 0, new PrerequisiteParameters { Target = subject }));
    }

    [Theory]
    [InlineData(0u, true)]
    [InlineData(65536u, true)]
    [InlineData(0x80000000u, true)]
    [InlineData(65537u, false)]
    [InlineData(0xffffffffu, false)]
    public void ThresholdSquareWrapsAtThirtyTwoBits(uint threshold, bool expected)
    {
        Assert.Equal(expected, Meets(Vector3.Zero, Vector3.Zero, PrerequisiteComparison.Equal, threshold));
    }

    [Fact]
    public void DistanceTruncatesToInt64ThenRetainsLowThirtyTwoBits()
    {
        Assert.True(Meets(Vector3.Zero, new Vector3(65536, 0, 0), PrerequisiteComparison.Equal, 0));
        Assert.True(Meets(Vector3.Zero, new Vector3(65537, 0, 0), PrerequisiteComparison.GreaterThan, 0));
        Assert.True(Meets(Vector3.Zero, Vector3.UnitX, PrerequisiteComparison.Equal, uint.MaxValue));
    }

    [Fact]
    public void SquaredSumRetainsNativeLeftToRightFloat32Rounding()
    {
        Assert.True(Meets(Vector3.Zero, new Vector3(4096, 1, 1), PrerequisiteComparison.Equal, 4096));
    }

    [Fact]
    public void LowThirtyTwoBitsAreComparedAsUnsigned()
    {
        Assert.True(Meets(Vector3.Zero, new Vector3(32768, 32768, 0), PrerequisiteComparison.GreaterThan, 0));
    }

    [Fact]
    public void ExactTwoToTheSixtyThirdIsOutsideTheSignedConversionRange()
    {
        Assert.True(Meets(Vector3.Zero, new Vector3(2147483648f, 2147483648f, 0), PrerequisiteComparison.Equal, 0));
    }

    [Theory]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    [InlineData(float.MaxValue)]
    [InlineData(4294967296f)]
    public void InvalidInt64ConversionHasNativeLowBitsZeroInsteadOfClrSaturation(float coordinate)
    {
        var target = new Vector3(coordinate, 0, 0);
        Assert.True(Meets(Vector3.Zero, target, PrerequisiteComparison.Equal, 0));
        Assert.False(Meets(Vector3.Zero, target, PrerequisiteComparison.NotEqual, 0));
        Assert.True(Meets(Vector3.Zero, target, PrerequisiteComparison.LessThan, 1));
    }

    [Fact]
    public void ActualShippedPrerequisite5814UsesLessThanOrEqualFifty()
    {
        Assert.True(Meets(Vector3.Zero, new Vector3(30, 40, 0), (PrerequisiteComparison)6, 50));
        Assert.False(Meets(Vector3.Zero, new Vector3(31, 40, 0), (PrerequisiteComparison)6, 50));
    }

    [Fact]
    public void RawNinetyFiveRegistrationRetainsPlayerAndUnitApis()
    {
        var services = new ServiceCollection();
        services.AddGamePrerequisite();
        using var provider = services.BuildServiceProvider();
        var check = Assert.IsType<PrerequisiteCheckDistance>(provider.GetRequiredKeyedService<IPrerequisiteCheck>((PrerequisiteType)95));
        var player = RecordingDispatchProxy<IPlayer>.Create(out var proxy);
        proxy.SetProperty(nameof(IPlayer.Position), new Vector3(1, 2, 3));
        var parameters = new PrerequisiteParameters { Target = Unit(new Vector3(4, 6, 15)) };
        Assert.True(check.Meets(player, PrerequisiteComparison.Equal, 13, 0, parameters));
        Assert.True(check.MeetsUnit(player, PrerequisiteComparison.Equal, 13, 0, parameters));
    }

    private static bool Meets(Vector3 position, Vector3 target, PrerequisiteComparison comparison, uint value, uint objectId = 0)
        => new PrerequisiteCheckDistance().MeetsUnit(Unit(position), comparison, value, objectId, new PrerequisiteParameters { Target = Unit(target) });

    private static IUnitEntity Unit(Vector3 position)
    {
        var unit = RecordingDispatchProxy<IUnitEntity>.Create(out var proxy);
        proxy.SetProperty(nameof(IUnitEntity.Position), position);
        return unit;
    }
}
