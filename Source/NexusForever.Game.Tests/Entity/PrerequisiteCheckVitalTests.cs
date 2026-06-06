using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Prerequisite.Check;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Tests.TestSupport;

namespace NexusForever.Game.Tests.Entity;

public class PrerequisiteCheckVitalTests
{
    [Theory]
    [InlineData(PrerequisiteComparison.Equal, 42u, true)]
    [InlineData(PrerequisiteComparison.Equal, 41u, false)]
    [InlineData(PrerequisiteComparison.NotEqual, 42u, false)]
    [InlineData(PrerequisiteComparison.NotEqual, 41u, true)]
    [InlineData(PrerequisiteComparison.GreaterThanOrEqual, 42u, true)]
    [InlineData(PrerequisiteComparison.GreaterThan, 41u, true)]
    [InlineData(PrerequisiteComparison.LessThanOrEqual, 42u, true)]
    [InlineData(PrerequisiteComparison.LessThan, 43u, true)]
    public void Meets_UsesUnitVitalValuesForResourceVitals(
        PrerequisiteComparison comparison,
        uint requiredValue,
        bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetMethodHandler(nameof(IPlayer.TryGetVitalValue), args =>
        {
            if ((Vital)args[0] == Vital.Resource1)
            {
                args[1] = 42f;
                return true;
            }

            args[1] = 0f;
            return false;
        });

        var check = new PrerequisiteCheckVital(NullLogger<PrerequisiteCheckVital>.Instance);

        bool result = check.Meets(player, comparison, requiredValue, (uint)Vital.Resource1, new PrerequisiteParameters());

        Assert.Equal(expected, result);
    }
}
