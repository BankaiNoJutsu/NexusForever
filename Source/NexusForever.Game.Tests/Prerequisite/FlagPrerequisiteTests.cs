using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Abstract.Group;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Prerequisite.Check;
using NexusForever.Game.Static.Entity.Movement.Command.State;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Tests.TestSupport;

namespace NexusForever.Game.Tests.Prerequisite;

// Native evidence (Decomp/Analysis/PREREQUISITE_FLAG_COMPARISON_20261107.md): prerequisite types 28 (InCombat), 39 (IsPlayer), 42 (IsGroupLeader), 86 (Jump) and 87 (Moving) test one boolean of the
// unit and finish through the evaluator's shared tail: comparison 1 returns the flag, comparison 2 its negation and any other comparison is false. Value is never read. Retail uses Value 0 in 204 of the 230
// type 28 slots, 184 of 200 type 39, 21 of 22 type 86 and 25 of 26 type 87, mostly with Equal or NotEqual; the previous implementations compared the flag with Value, so Equal with Value 0 meant "flag is
// clear" and NotEqual with Value 0 "flag is set", the reverse of the native result.
public class FlagPrerequisiteTests
{
    public static TheoryData<bool, PrerequisiteComparison, uint, bool> Cases()
    {
        var data = new TheoryData<bool, PrerequisiteComparison, uint, bool>();
        foreach (uint value in new[] { 0u, 1u, 7u })
        {
            data.Add(true,  PrerequisiteComparison.Equal,              value, true);
            data.Add(false, PrerequisiteComparison.Equal,              value, false);
            data.Add(true,  PrerequisiteComparison.NotEqual,           value, false);
            data.Add(false, PrerequisiteComparison.NotEqual,           value, true);
            data.Add(true,  PrerequisiteComparison.GreaterThan,        value, false);
            data.Add(false, PrerequisiteComparison.LessThanOrEqual,    value, false);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void InCombatReturnsTheFlagForEqualAndItsNegationForNotEqual(bool inCombat, PrerequisiteComparison comparison, uint value, bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var proxy);
        proxy.SetProperty(nameof(IUnitEntity.InCombat), inCombat);

        var check = new PrerequisiteCheckInCombat();
        Assert.Equal(expected, check.Meets(player, comparison, value, 0u, new PrerequisiteParameters()));
        Assert.Equal(expected, check.MeetsUnit(player, comparison, value, 0u, new PrerequisiteParameters()));
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void IsPlayerReturnsTheFlagForEqualAndItsNegationForNotEqual(bool isPlayer, PrerequisiteComparison comparison, uint value, bool expected)
    {
        IUnitEntity subject = isPlayer
            ? RecordingDispatchProxy<IPlayer>.Create(out _)
            : RecordingDispatchProxy<IUnitEntity>.Create(out _);

        var check = new PrerequisiteCheckIsPlayer();
        Assert.Equal(expected, check.MeetsUnit(subject, comparison, value, 0u, new PrerequisiteParameters()));

        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        // The primary player is kind 20 regardless of the relationship target's kind.
        Assert.Equal(comparison == PrerequisiteComparison.Equal,
            check.Meets(player, comparison, value, 0u, new PrerequisiteParameters { Target = subject }));
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void JumpReturnsTheFlagForEqualAndItsNegationForNotEqual(bool jumping, PrerequisiteComparison comparison, uint value, bool expected)
    {
        IPlayer player = PlayerWithState(jumping ? StateFlags.Jump : StateFlags.None);

        Assert.Equal(expected, new PrerequisiteCheckJump().Meets(player, comparison, value, 0u, new PrerequisiteParameters()));
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void MovingReturnsTheFlagForEqualAndItsNegationForNotEqual(bool moving, PrerequisiteComparison comparison, uint value, bool expected)
    {
        IPlayer player = PlayerWithState(moving ? StateFlags.Move : StateFlags.None);

        Assert.Equal(expected, new PrerequisiteCheckMoving().Meets(player, comparison, value, 0u, new PrerequisiteParameters()));
    }

    private static IPlayer PlayerWithState(StateFlags state)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IMovementManager movement = RecordingDispatchProxy<IMovementManager>.Create(out var movementProxy);
        movementProxy.SetMethodReturn(nameof(IMovementManager.GetState), state);
        movementProxy.SetMethodReturn(nameof(IMovementManager.GetVelocity), System.Numerics.Vector3.Zero);
        movementProxy.SetMethodReturn(nameof(IMovementManager.GetMove), System.Numerics.Vector3.Zero);
        playerProxy.SetProperty(nameof(IWorldEntity.MovementManager), movement);
        return player;
    }
}
