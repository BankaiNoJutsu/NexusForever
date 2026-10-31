using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Prerequisite.Check;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Tests.TestSupport;

namespace NexusForever.Game.Tests.Prerequisite;

public class VitalPrerequisiteScalarClientTests
{
    [Theory]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(3, false)]
    [InlineData(4, true)]
    [InlineData(5, false)]
    [InlineData(6, true)]
    [InlineData(0, false)]
    [InlineData(7, false)]
    public void RawModesCompareTheTruncatedResource(int mode, bool expected)
    {
        Assert.Equal(expected, Check().MeetsUnit(Unit(300.5f), (PrerequisiteComparison)mode, 300, 6, null));
    }

    [Theory]
    [InlineData(0.99f, 0u)]
    [InlineData(-0.99f, 0u)]
    [InlineData(-1.5f, 0xffffffffu)]
    [InlineData(2147483648f, 0x80000000u)]
    [InlineData(-2147483648f, 0x80000000u)]
    [InlineData(4294967296f, 0u)]
    [InlineData(-4294967296f, 0u)]
    [InlineData(4294967808f, 512u)]
    [InlineData(9223372036854775808f, 0u)]
    [InlineData(-9223372036854775808f, 0u)]
    [InlineData(float.NaN, 0u)]
    [InlineData(float.PositiveInfinity, 0u)]
    [InlineData(float.NegativeInfinity, 0u)]
    [InlineData(float.MaxValue, 0u)]
    [InlineData(float.MinValue, 0u)]
    public void UsesSignedInt64TruncationThenUnsignedLowBits(float resource, uint expected)
    {
        Assert.True(Check().MeetsUnit(Unit(resource), PrerequisiteComparison.Equal, expected, 6, null));
    }

    [Fact]
    public void ThresholdStaysUint32InsteadOfBeingRoundedToFloat()
    {
        Assert.False(Check().MeetsUnit(Unit(16777216f), PrerequisiteComparison.Equal, 16777217, 6, null));
        Assert.False(Check().MeetsUnit(Unit(4294967296f), PrerequisiteComparison.Equal, uint.MaxValue, 6, null));
        Assert.True(Check().MeetsUnit(Unit(-1.5f), PrerequisiteComparison.GreaterThan, int.MaxValue, 6, null));
    }

    [Fact]
    public void ShippedPrerequisites5985And5986KeepTheirResourceBoundary()
    {
        var check = Check();
        var subject = Unit(300.5f);
        // 5985 is Resource1 >300; 5986 is Resource1 <=300 AND >200.
        Assert.False(check.MeetsUnit(subject, (PrerequisiteComparison)3, 300, 6, null));
        Assert.True(check.MeetsUnit(subject, (PrerequisiteComparison)6, 300, 6, null));
        Assert.True(check.MeetsUnit(subject, (PrerequisiteComparison)3, 200, 6, null));
    }

    [Fact]
    public void MissingSubjectOrUnsupportedPoolFailsEvenWhenNotEqual()
    {
        Assert.False(Check().MeetsUnit(null, PrerequisiteComparison.NotEqual, 0, 6, null));
        Assert.False(Check().MeetsUnit(Unit(300.5f), PrerequisiteComparison.NotEqual, 0, uint.MaxValue, null));
    }

    [Fact]
    public void RawSeventyThreeRegistrationPreservesPlayerAndNpcSubjectOwnership()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddGamePrerequisite();
        using var provider = services.BuildServiceProvider();
        var check = Assert.IsType<PrerequisiteCheckVital>(provider.GetRequiredKeyedService<IPrerequisiteCheck>((PrerequisiteType)73));
        var player = RecordingDispatchProxy<IPlayer>.Create(out var proxy);
        Configure(proxy);
        var parameters = new PrerequisiteParameters { Target = Unit(1) };
        Assert.True(check.Meets(player, PrerequisiteComparison.Equal, 300, 6, parameters));
        Assert.True(check.MeetsUnit(Unit(300.5f), PrerequisiteComparison.Equal, 300, 6, parameters));

        static void Configure(RecordingDispatchProxy<IPlayer> proxy)
            => proxy.SetMethodHandler(nameof(IPlayer.TryGetVitalValue), args => { args[1] = 300.5f; return true; });
    }

    private static PrerequisiteCheckVital Check() => new(NullLogger<PrerequisiteCheckVital>.Instance);

    private static IUnitEntity Unit(float resource)
    {
        var unit = RecordingDispatchProxy<IUnitEntity>.Create(out var proxy);
        proxy.SetMethodHandler(nameof(IUnitEntity.TryGetVitalValue), args =>
        {
            args[1] = resource;
            return (Vital)args[0] == Vital.Resource1;
        });
        return unit;
    }
}
