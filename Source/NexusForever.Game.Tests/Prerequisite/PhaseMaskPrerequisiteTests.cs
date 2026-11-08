using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Prerequisite.Check;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Tests.TestSupport;

namespace NexusForever.Game.Tests.Prerequisite;

// Prerequisite types 24 (InPhase) and 25 (CanSeePhase) (native FUN_14049e640, Decomp/Analysis/PREREQUISITE_24_25_219_NATIVE_READ_20261108.md): both test bit (Value & 31) of the local player's
// PhasesIPerceive word, the dword the server sends in packet 0x0171; Equal returns the bit, NotEqual its negation, any other comparison false. The entity, the target and ObjectId are not read,
// and the entity's EntityEvent.Phase (spawn data, WorldEntity.PublicEventPhase) is a different concept that is not consulted.
public class PhaseMaskPrerequisiteTests
{
    public static IEnumerable<object[]> Checks()
    {
        yield return [new PrerequisiteCheckInPhase()];
        yield return [new PrerequisiteCheckCanSeePhase()];
    }

    [Theory]
    [MemberData(nameof(Checks))]
    public void TheDefaultMaskOfOneSatisfiesOnlyValueZero(IPrerequisiteCheck check)
    {
        IPlayer player = Player(mask: 1u);

        Assert.True(check.Meets(player, PrerequisiteComparison.Equal, 0u, 0u, new PrerequisiteParameters()));
        Assert.False(check.Meets(player, PrerequisiteComparison.Equal, 1u, 0u, new PrerequisiteParameters()));
        Assert.False(check.Meets(player, PrerequisiteComparison.NotEqual, 0u, 0u, new PrerequisiteParameters()));
        Assert.True(check.Meets(player, PrerequisiteComparison.NotEqual, 1u, 0u, new PrerequisiteParameters()));
    }

    [Theory]
    [MemberData(nameof(Checks))]
    public void AnyBitOfTheMaskCanBeTested(IPrerequisiteCheck check)
    {
        IPlayer player = Player(mask: 0b100u);

        Assert.True(check.Meets(player, PrerequisiteComparison.Equal, 2u, 0u, new PrerequisiteParameters()));
        Assert.False(check.Meets(player, PrerequisiteComparison.Equal, 0u, 0u, new PrerequisiteParameters()));
        Assert.True(check.Meets(player, PrerequisiteComparison.NotEqual, 0u, 0u, new PrerequisiteParameters()));
    }

    [Theory]
    [MemberData(nameof(Checks))]
    public void TheValueWrapsAtThirtyTwoBits(IPrerequisiteCheck check)
    {
        IPlayer player = Player(mask: 0b10u);

        Assert.True(check.Meets(player, PrerequisiteComparison.Equal, 33u, 0u, new PrerequisiteParameters()));
        Assert.False(check.Meets(player, PrerequisiteComparison.Equal, 32u, 0u, new PrerequisiteParameters()));
    }

    [Theory]
    [MemberData(nameof(Checks))]
    public void AMaskOfZeroIsNeverInAnyPhase(IPrerequisiteCheck check)
    {
        // before packet 0x0171 arrives the client's word is zero, so every Equal is false
        IPlayer player = Player(mask: 0u);

        Assert.False(check.Meets(player, PrerequisiteComparison.Equal, 0u, 0u, new PrerequisiteParameters()));
        Assert.True(check.Meets(player, PrerequisiteComparison.NotEqual, 0u, 0u, new PrerequisiteParameters()));
    }

    [Theory]
    [MemberData(nameof(Checks))]
    public void OtherComparisonsAreFalse(IPrerequisiteCheck check)
    {
        IPlayer player = Player(mask: uint.MaxValue);

        Assert.False(check.Meets(player, PrerequisiteComparison.GreaterThan, 0u, 0u, new PrerequisiteParameters()));
        Assert.False(check.Meets(player, PrerequisiteComparison.LessThan, 0u, 0u, new PrerequisiteParameters()));
    }

    [Theory]
    [MemberData(nameof(Checks))]
    public void TheEntitySpawnPhaseTargetAndObjectAreIgnored(IPrerequisiteCheck check)
    {
        IPlayer player = Player(mask: 1u, out var playerProxy);
        playerProxy.SetProperty(nameof(IWorldEntity.PublicEventPhase), 3u);

        IUnitEntity target = RecordingDispatchProxy<IUnitEntity>.Create(out var targetProxy);
        targetProxy.SetProperty(nameof(IWorldEntity.PublicEventPhase), 5u);
        var parameters = new PrerequisiteParameters { Target = target };

        Assert.True(check.Meets(player, PrerequisiteComparison.Equal, 0u, 3013u, parameters));
        Assert.False(check.Meets(player, PrerequisiteComparison.Equal, 3u, 3013u, parameters));
        Assert.False(check.Meets(player, PrerequisiteComparison.Equal, 5u, 3013u, parameters));
    }

    [Theory]
    [MemberData(nameof(Checks))]
    public void APlayerSubjectUsesItsOwnMaskAndOtherUnitsTheDefault(IPrerequisiteCheck check)
    {
        IUnitEntity otherPlayer = Player(mask: 0b10u);
        IUnitEntity creature = RecordingDispatchProxy<IUnitEntity>.Create(out var creatureProxy);
        creatureProxy.SetProperty(nameof(IUnitEntity.Type), EntityType.NonPlayer);

        Assert.True(check.MeetsUnit(otherPlayer, PrerequisiteComparison.Equal, 1u, 0u, new PrerequisiteParameters()));
        Assert.False(check.MeetsUnit(otherPlayer, PrerequisiteComparison.Equal, 0u, 0u, new PrerequisiteParameters()));

        // a non-player has no native meaning; the default mask of one applies
        Assert.True(check.MeetsUnit(creature, PrerequisiteComparison.Equal, 0u, 0u, new PrerequisiteParameters()));
        Assert.False(check.MeetsUnit(creature, PrerequisiteComparison.Equal, 1u, 0u, new PrerequisiteParameters()));
    }

    private static IPlayer Player(uint mask)
        => Player(mask, out _);

    private static IPlayer Player(uint mask, out RecordingDispatchProxy<IPlayer> proxy)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out proxy);
        proxy.SetProperty(nameof(IPlayer.Type), EntityType.Player);
        proxy.SetProperty(nameof(IPlayer.PhasesIPerceive), mask);
        return player;
    }
}
