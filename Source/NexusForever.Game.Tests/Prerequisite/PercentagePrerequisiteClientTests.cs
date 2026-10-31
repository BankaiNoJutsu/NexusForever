using Microsoft.Extensions.DependencyInjection;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Prerequisite.Check;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Tests.TestSupport;

namespace NexusForever.Game.Tests.Prerequisite;

public class PercentagePrerequisiteClientTests
{
    [Theory]
    [InlineData(1, 50u, true)]
    [InlineData(2, 50u, false)]
    [InlineData(3, 49u, true)]
    [InlineData(3, 50u, false)]
    [InlineData(4, 50u, true)]
    [InlineData(5, 51u, true)]
    [InlineData(5, 50u, false)]
    [InlineData(6, 50u, true)]
    [InlineData(0, 50u, false)]
    [InlineData(7, 50u, false)]
    public void NativeRawModesComparePercentageRatherThanRawVital(int mode, uint threshold, bool expected)
    {
        var player = Player(15, 30, 25, 100, out _);
        var vital = new PrerequisiteCheckVitalPercent();
        var shield = new PrerequisiteCheckShieldCapacityPercent();
        Assert.Equal(expected, vital.Meets(player, (PrerequisiteComparison)mode, threshold, (uint)Vital.Health, null));
        Assert.Equal(expected, shield.Meets(Player(1, 1, 15, 30, out _), (PrerequisiteComparison)mode, threshold, uint.MaxValue, null));
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, true)]
    [InlineData(3, false)]
    [InlineData(4, false)]
    [InlineData(5, false)]
    [InlineData(6, false)]
    [InlineData(7, false)]
    public void ZeroOverZeroUsesNativeNaNComparisonInsteadOfClamping(int mode, bool expected)
    {
        var player = Player(0, 0, 0, 0, out _);
        var comparison = (PrerequisiteComparison)mode;
        Assert.Equal(expected, new PrerequisiteCheckVitalPercent().Meets(player, comparison, 0, (uint)Vital.Health, null));
        Assert.Equal(expected, new PrerequisiteCheckVitalPercent().Meets(player, comparison, 0, (uint)Vital.ShieldCapacity, null));
        Assert.Equal(expected, new PrerequisiteCheckShieldCapacityPercent().Meets(player, comparison, 0, 0, null));
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    [InlineData(4, true)]
    [InlineData(5, false)]
    [InlineData(6, false)]
    public void NonzeroOverZeroUsesNativePositiveInfinity(int mode, bool expected)
    {
        var player = Player(1, 0, 1, 0, out _);
        Assert.Equal(expected, new PrerequisiteCheckVitalPercent().Meets(player, (PrerequisiteComparison)mode, 100, (uint)Vital.Health, null));
        Assert.Equal(expected, new PrerequisiteCheckShieldCapacityPercent().Meets(player, (PrerequisiteComparison)mode, 100, 0, null));
    }

    [Fact]
    public void UsesSubjectAndCorrectVitalSelectorWithoutReversingExistingTargetContext()
    {
        var subject = Player(10, 40, 60, 100, out var proxy);
        var target = Player(1, 1, 1, 1, out _);
        var parameters = new PrerequisiteParameters { Target = target };
        var check = new PrerequisiteCheckVitalPercent();
        Assert.True(check.Meets(subject, PrerequisiteComparison.Equal, 25, (uint)Vital.Health, parameters));
        Assert.True(check.Meets(subject, PrerequisiteComparison.Equal, 60, (uint)Vital.ShieldCapacity, parameters));
        proxy.SetMethodHandler(nameof(IUnitEntity.TryGetVitalValue), args =>
        {
            Assert.Equal(Vital.Resource7, args[0]); args[1] = 12f; return true;
        });
        proxy.SetMethodHandler(nameof(IUnitEntity.TryGetVitalMax), args =>
        {
            Assert.Equal(Vital.Resource7, args[0]); args[1] = 48f; return true;
        });
        Assert.True(check.Meets(subject, PrerequisiteComparison.Equal, 25, (uint)Vital.Resource7, parameters));
        Assert.True(new PrerequisiteCheckShieldCapacityPercent().Meets(subject, PrerequisiteComparison.Equal, 60, uint.MaxValue, parameters));
    }

    [Fact]
    public void MissingSubjectOrUnsupportedRuntimePoolFailsClosed()
    {
        var check = new PrerequisiteCheckVitalPercent();
        var player = Player(1, 1, 1, 1, out var proxy);
        Assert.False(check.Meets(null, PrerequisiteComparison.NotEqual, 0, 1, null));
        Assert.False(new PrerequisiteCheckShieldCapacityPercent().Meets(null, PrerequisiteComparison.NotEqual, 0, 0, null));
        Assert.False(check.Meets(player, PrerequisiteComparison.NotEqual, 0, uint.MaxValue, null));
        proxy.SetMethodHandler(nameof(IUnitEntity.TryGetVitalValue), args => { args[1] = 1f; return true; });
        Assert.False(check.Meets(player, PrerequisiteComparison.NotEqual, 0, (uint)Vital.Resource7, null));
    }

    [Fact]
    public void PercentageKeepsFractionAndSinglePrecisionWithoutIntegerTruncationOrClamping()
    {
        var check = new PrerequisiteCheckVitalPercent();
        Assert.True(check.Meets(Player(1, 3, 0, 1, out _), PrerequisiteComparison.GreaterThan, 33, 1, null));
        Assert.True(check.Meets(Player(101, 100, 0, 1, out _), PrerequisiteComparison.GreaterThan, 100, 1, null));
        Assert.True(check.Meets(Player(uint.MaxValue, uint.MaxValue, 0, 1, out _), PrerequisiteComparison.Equal, 100, 1, null));
    }

    [Fact]
    public void BothChecksResolveAtVerifiedNativeRawIds()
    {
        var services = new ServiceCollection();
        services.AddGamePrerequisite();
        using var provider = services.BuildServiceProvider();
        Assert.IsType<PrerequisiteCheckVitalPercent>(provider.GetRequiredKeyedService<IPrerequisiteCheck>((PrerequisiteType)71));
        Assert.IsType<PrerequisiteCheckShieldCapacityPercent>(provider.GetRequiredKeyedService<IPrerequisiteCheck>((PrerequisiteType)215));
    }

    private static IPlayer Player(uint health, uint maxHealth, uint shield, uint capacity, out RecordingDispatchProxy<IPlayer> proxy)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out proxy);
        proxy.SetProperty(nameof(IPlayer.Health), health);
        proxy.SetProperty(nameof(IPlayer.MaxHealth), maxHealth);
        proxy.SetProperty(nameof(IPlayer.Shield), shield);
        proxy.SetProperty(nameof(IPlayer.MaxShieldCapacity), capacity);
        return player;
    }
}
