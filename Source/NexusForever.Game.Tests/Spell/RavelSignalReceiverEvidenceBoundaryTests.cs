using NexusForever.Game.Static.Spell;

namespace NexusForever.Game.Tests.Spell;

public class RavelSignalReceiverEvidenceBoundaryTests
{
    [Fact]
    public void Describe_Mode1_ActivateAndClearFixture_IsEntityScriptOnSignal()
    {
        RavelSignalReceiverEvidenceBoundarySnapshot boundary =
            RavelSignalReceiverEvidenceBoundary.Describe(RavelSignalReceiverEvidenceBoundary.EntityScriptOnSignalMode);

        Assert.True(boundary.ModeSupported);
        Assert.Equal("entity-script-on-signal", boundary.ModeLabel);
        Assert.Equal(RavelSignalReceiverRoute.EntityScriptOnSignal, boundary.ReceiverRoute);
        Assert.Null(boundary.BlockedReason);
        Assert.True(boundary.IsConservativelyDispatchSupported);
    }

    [Theory]
    [InlineData(2u)]
    [InlineData(3u)]
    [InlineData(4u)]
    [InlineData(5u)]
    public void Describe_UnsupportedModes_StayDiagnosticsOnly(uint mode)
    {
        RavelSignalReceiverEvidenceBoundarySnapshot boundary = RavelSignalReceiverEvidenceBoundary.Describe(mode);

        Assert.False(boundary.ModeSupported);
        Assert.Equal(RavelSignalReceiverRoute.DiagnosticsOnly, boundary.ReceiverRoute);
        Assert.Equal("unsupported-ravel-signal-mode", boundary.BlockedReason);
        Assert.False(boundary.IsConservativelyDispatchSupported);
    }
}
