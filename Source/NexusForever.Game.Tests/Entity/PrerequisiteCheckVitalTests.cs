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
        IPlayer player = PlayerWithResources(Class.Warrior, (Vital.Resource1, 42f));
        var check = new PrerequisiteCheckVital(null, NullLogger<PrerequisiteCheckVital>.Instance);

        bool result = check.Meets(player, comparison, requiredValue, (uint)Vital.Resource1, new PrerequisiteParameters());

        Assert.Equal(expected, result);
    }

    // Retail row 43654: the Engineer tank rune set buff persists while Absorption (0x15) > 0.
    [Theory]
    [InlineData(0u, false)]
    [InlineData(250u, true)]
    public void AbsorptionReadsTheUnitAbsorptionPool(uint absorption, bool expected)
    {
        IUnitEntity unit = RecordingDispatchProxy<IUnitEntity>.Create(out var proxy);
        proxy.SetProperty(nameof(IUnitEntity.CurrentAbsorption), absorption);
        var check = new PrerequisiteCheckVital(null, NullLogger<PrerequisiteCheckVital>.Instance);

        Assert.Equal(expected, check.MeetsUnit(unit, PrerequisiteComparison.GreaterThan, 0u, (uint)Vital.Absorption, new PrerequisiteParameters()));
    }

    private static IPlayer PlayerWithResources(Class playerClass, params (Vital Vital, float Value)[] resources)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Class), playerClass);
        playerProxy.SetMethodHandler(nameof(IPlayer.TryGetVitalValue), args =>
        {
            foreach ((Vital vital, float value) in resources)
            {
                if ((Vital)args[0] == vital)
                {
                    args[1] = value;
                    return true;
                }
            }

            args[1] = 0f;
            return true;
        });
        return player;
    }
}
