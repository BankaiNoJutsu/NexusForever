using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Prerequisite.Check;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Tests.TestSupport;

namespace NexusForever.Game.Tests.Prerequisite;

// Prerequisite type 232 Plane (native FUN_14049e610, Decomp/Analysis/PREREQUISITE_NATIVE_COMPARISONS_20261107.md): (local player's plane word & Value) != 0, where the word is the second dword of
// packet 0x0172; Equal returns the flag, NotEqual its negation. Retail uses Value 4 in eight slots and 1 and 2 once each. The word is zero until a packet arrives.
public class PlaneMaskPrerequisiteTests
{
    [Theory]
    [InlineData(4u, 4u, true)]
    [InlineData(5u, 4u, true)]
    [InlineData(1u, 4u, false)]
    [InlineData(0u, 4u, false)]
    [InlineData(6u, 2u, true)]
    [InlineData(4u, 3u, false)]
    public void EqualTestsWhetherAnyValueBitIsSetInThePlaneWord(uint mask, uint value, bool expected)
    {
        var check = new PrerequisiteCheckPlane();
        IPlayer player = Player(mask);

        Assert.Equal(expected, check.Meets(player, PrerequisiteComparison.Equal, value, 0u, new PrerequisiteParameters()));
        Assert.Equal(!expected, check.Meets(player, PrerequisiteComparison.NotEqual, value, 0u, new PrerequisiteParameters()));
    }

    [Fact]
    public void OtherComparisonsAreFalse()
    {
        var check = new PrerequisiteCheckPlane();

        Assert.False(check.Meets(Player(uint.MaxValue), PrerequisiteComparison.GreaterThan, 4u, 0u, new PrerequisiteParameters()));
    }

    [Fact]
    public void ANonPlayerSubjectHasNoPlanes()
    {
        IPrerequisiteCheck check = new PrerequisiteCheckPlane();
        IUnitEntity creature = RecordingDispatchProxy<IUnitEntity>.Create(out var proxy);
        proxy.SetProperty(nameof(IUnitEntity.Type), EntityType.NonPlayer);

        Assert.False(check.MeetsUnit(creature, PrerequisiteComparison.Equal, 4u, 0u, new PrerequisiteParameters()));
        Assert.True(check.MeetsUnit(creature, PrerequisiteComparison.NotEqual, 4u, 0u, new PrerequisiteParameters()));
    }

    [Fact]
    public void APlayerSubjectUsesItsOwnPlaneWord()
    {
        IPrerequisiteCheck check = new PrerequisiteCheckPlane();
        IUnitEntity player = Player(4u);

        Assert.True(check.MeetsUnit(player, PrerequisiteComparison.Equal, 4u, 0u, new PrerequisiteParameters()));
    }

    private static IPlayer Player(uint planesCanSee)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var proxy);
        proxy.SetProperty(nameof(IPlayer.Type), EntityType.Player);
        proxy.SetProperty(nameof(IPlayer.PlanesCanSee), planesCanSee);
        return player;
    }
}
