using NexusForever.Game.Entity.Movement.AntiTamper;
using NexusForever.Game.Static.Entity.Movement.Command.State;

namespace NexusForever.Game.Tests.Entity;

public class ClientMovementCommandValidatorTests
{
    [Fact]
    public void ValidateTime_AllowsObservedClientClockDrift()
    {
        var validator = new ClientMovementCommandValidator();

        Exception exception = Record.Exception(() => validator.ValidateTime(365339u, 0u));

        Assert.Null(exception);
    }

    [Theory]
    [InlineData(52u)]
    [InlineData(54u)]
    [InlineData(62u)]
    [InlineData(86u)]
    [InlineData(304u)]
    [InlineData(306u)]
    [InlineData(2134u)]
    [InlineData(2164u)]
    [InlineData(2166u)]
    [InlineData(2174u)]
    [InlineData(2418u)]
    public void ValidateState_AllowsObservedJumpAndHoverboardStateBits(uint state)
    {
        var validator = new ClientMovementCommandValidator();

        Exception exception = Record.Exception(() => validator.ValidateState((StateFlags)state));

        Assert.Null(exception);
    }
}
