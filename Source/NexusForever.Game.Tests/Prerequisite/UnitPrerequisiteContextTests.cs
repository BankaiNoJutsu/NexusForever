using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Creature;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Prerequisite.Check;
using NexusForever.Game.Spell;
using NexusForever.Game.Spell.Effect;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Entity.Movement.Command.Mode;
using NexusForever.Game.Static.Entity.Movement.Command.State;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Static.Spell;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Static;
using NexusForever.Script;
using NexusForever.Script.Template.Collection;

namespace NexusForever.Game.Tests.Prerequisite;

public sealed class UnitPrerequisiteContextTests
{
    // Native evaluator 1404a2100: cases 0x26, 0x3b and 0x81 all read the primary unit.
    // A relationship target must not silently replace it in the legacy player entry point.
    [Theory]
    [InlineData(38, true, PrerequisiteComparison.Equal)]
    [InlineData(38, false, PrerequisiteComparison.Equal)]
    [InlineData(38, true, PrerequisiteComparison.NotEqual)]
    [InlineData(38, false, PrerequisiteComparison.NotEqual)]
    [InlineData(59, true, PrerequisiteComparison.Equal)]
    [InlineData(59, false, PrerequisiteComparison.Equal)]
    [InlineData(59, true, PrerequisiteComparison.NotEqual)]
    [InlineData(59, false, PrerequisiteComparison.NotEqual)]
    [InlineData(129, true, PrerequisiteComparison.Equal)]
    [InlineData(129, false, PrerequisiteComparison.Equal)]
    [InlineData(129, true, PrerequisiteComparison.NotEqual)]
    [InlineData(129, false, PrerequisiteComparison.NotEqual)]
    public void PlayerAndUnitEntryPointsRetainTheSamePrimarySubject(int type, bool primaryMatches, PrerequisiteComparison comparison)
    {
        uint value = type == 38 ? 16623u : type == 59 ? 1u : 0u;
        uint objectId = type == 129 ? 375u : 100u;
        PrerequisiteEntry row = Row(1u, (PrerequisiteType)type, value, objectId);
        row.PrerequisiteComparisonId = [comparison];
        using var fixture = new Fixture(row);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IUnitEntity target = RecordingDispatchProxy<IUnitEntity>.Create(out var targetProxy);
        playerProxy.SetProperty(nameof(IWorldEntity.CreatureId), primaryMatches ? 16623u : 0u);
        targetProxy.SetProperty(nameof(IWorldEntity.CreatureId), primaryMatches ? 0u : 16623u);
        playerProxy.SetMethodReturn(nameof(IUnitEntity.GetActiveSpellStackCount), primaryMatches ? 1u : 0u);
        targetProxy.SetMethodReturn(nameof(IUnitEntity.GetActiveSpellStackCount), primaryMatches ? 0u : 1u);
        playerProxy.SetMethodReturn(nameof(IUnitEntity.HasActiveSpellInGroup), primaryMatches);
        targetProxy.SetMethodReturn(nameof(IUnitEntity.HasActiveSpellInGroup), !primaryMatches);
        var parameters = new PrerequisiteParameters { Target = target };
        bool expected = comparison == PrerequisiteComparison.Equal ? primaryMatches : !primaryMatches;

        Assert.Equal(expected, fixture.Manager.MeetsUnit(player, 1u, parameters));
        Assert.Equal(expected, fixture.Manager.Meets(player, 1u, parameters));
        Assert.Same(target, parameters.Target);
        // Callers can still choose the target explicitly; the relationship becomes the player.
        Assert.Equal(!expected, fixture.Manager.MeetsUnit(target, 1u, new PrerequisiteParameters { Target = player }));
    }

    [Theory]
    [InlineData(38, PrerequisiteComparison.Equal)]
    [InlineData(38, PrerequisiteComparison.NotEqual)]
    [InlineData(59, PrerequisiteComparison.Equal)]
    [InlineData(59, PrerequisiteComparison.NotEqual)]
    [InlineData(129, PrerequisiteComparison.Equal)]
    [InlineData(129, PrerequisiteComparison.NotEqual)]
    public void NullPlayerSubjectIsNotReplacedByTheRelationshipTarget(int type, PrerequisiteComparison comparison)
    {
        IPrerequisiteCheck check = type switch
        {
            38 => new PrerequisiteCheckIsCreature(),
            59 => new PrerequisiteCheckSpellTier(),
            _ => new PrerequisiteCheckActiveSpellEffectOnUnit()
        };
        IUnitEntity target = RecordingDispatchProxy<IUnitEntity>.Create(out var proxy);
        proxy.SetProperty(nameof(IWorldEntity.CreatureId), 16623u);
        proxy.SetMethodReturn(nameof(IUnitEntity.GetActiveSpellStackCount), 1u);
        proxy.SetMethodReturn(nameof(IUnitEntity.HasActiveSpellInGroup), true);
        uint value = type == 38 ? 16623u : 1u;
        var parameters = new PrerequisiteParameters { Target = target };

        Assert.False(check.Meets(null, comparison, value, 100u, parameters));
        Assert.False(check.MeetsUnit(null, comparison, value, 100u, parameters));
    }

    [Theory]
    [InlineData(1, 20u)]
    [InlineData(4, 17u)]
    [InlineData(12, 0u)]
    [InlineData(15, 123u)]
    [InlineData(28, 1u)]
    [InlineData(40, 50u)]   // type 40 compares the health percentage: 45 of 90 is 50 (PREREQUISITE_HEALTH_PERCENT_20261107.md)
    [InlineData(71, 50u)]
    [InlineData(73, 23u)]
    [InlineData(82, (uint)EntityType.NonPlayer)]
    [InlineData(250, 17u)]
    [InlineData(215, 50u)]
    public void SupportedNpcChecksEvaluateSubjectRatherThanUnrelatedTarget(int type, uint required)
    {
        using var fixture = new Fixture(Row(1, (PrerequisiteType)type, required,
            objectId: (uint)(type == 71 ? Vital.Health : Vital.Resource1)));
        ICreatureEntity subject = RecordingDispatchProxy<ICreatureEntity>.Create(out var proxy);
        proxy.SetProperty(nameof(IUnitEntity.Level), 20u);
        proxy.SetProperty(nameof(IUnitEntity.Faction1), (Game.Static.Reputation.Faction)17);
        proxy.SetProperty(nameof(IUnitEntity.InCombat), true);
        proxy.SetProperty(nameof(IUnitEntity.IsAlive), true);
        proxy.SetProperty(nameof(IUnitEntity.Health), 45u);
        proxy.SetProperty(nameof(IUnitEntity.MaxHealth), 90u);
        proxy.SetProperty(nameof(IUnitEntity.Shield), 9u);
        proxy.SetProperty(nameof(IUnitEntity.MaxShieldCapacity), 18u);
        proxy.SetProperty(nameof(IUnitEntity.Type), EntityType.NonPlayer);
        proxy.SetMethodReturn(nameof(IUnitEntity.HasTrackedSpellState), true);
        proxy.SetMethodHandler(nameof(IUnitEntity.TryGetVitalValue), args => { args[1] = 23f; return true; });
        IPlayer target = RecordingDispatchProxy<IPlayer>.Create(out _);
        var parameters = new PrerequisiteParameters { Target = target };
        Assert.True(fixture.Manager.MeetsUnit(subject, 1, parameters));
        Assert.Same(target, parameters.Target);
    }

    // Type 38 compares the unit's creature id with Value (PREREQUISITE_CREATURE_ITEM_CHALLENGE_20261107.md): the NPC subject carries the id, the unrelated player target does not.
    [Fact]
    public void IsCreatureEvaluatesTheNpcSubjectRatherThanTheUnrelatedTarget()
    {
        using var fixture = new Fixture(Row(1, PrerequisiteType.IsCreature, 16623u));
        ICreatureEntity subject = RecordingDispatchProxy<ICreatureEntity>.Create(out var proxy);
        proxy.SetProperty(nameof(IWorldEntity.CreatureId), 16623u);
        IPlayer target = RecordingDispatchProxy<IPlayer>.Create(out _);

        Assert.True(fixture.Manager.MeetsUnit(subject, 1, new PrerequisiteParameters { Target = target }));
        Assert.False(fixture.Manager.MeetsUnit(target, 1, new PrerequisiteParameters { Target = subject }));
    }

    // Type 39 (IsPlayer) returns the flag for Equal and its negation for NotEqual (PREREQUISITE_FLAG_COMPARISON_20261107.md). The NPC subject is not a player while the unrelated target is, so only a
    // subject-based evaluation makes NotEqual meet and Equal fail.
    [Theory]
    [InlineData(PrerequisiteComparison.NotEqual, true)]
    [InlineData(PrerequisiteComparison.Equal, false)]
    public void IsPlayerEvaluatesTheNpcSubjectRatherThanTheUnrelatedPlayerTarget(PrerequisiteComparison comparison, bool expected)
    {
        PrerequisiteEntry row = Row(1, PrerequisiteType.IsPlayer, 0u);
        row.PrerequisiteComparisonId = [comparison];
        using var fixture = new Fixture(row);
        ICreatureEntity subject = RecordingDispatchProxy<ICreatureEntity>.Create(out _);
        IPlayer target = RecordingDispatchProxy<IPlayer>.Create(out _);

        Assert.Equal(expected, fixture.Manager.MeetsUnit(subject, 1, new PrerequisiteParameters { Target = target }));
    }

    [Theory]
    // Type 24 is no longer here: it tests the player's PhasesIPerceive mask, not the unit's PublicEventPhase (see PhaseMaskPrerequisiteTests).
    [InlineData(78, (uint)ModeType.Swim)]
    [InlineData(79, 1u)]
    [InlineData(80, 50u)]
    [InlineData(86, 1u)]
    [InlineData(87, 1u)]
    [InlineData(129, 123u)]
    [InlineData(172, 50u)]
    [InlineData(266, 1u)]
    public void ExistingUnitStateProxiesKeepTheirScalarBehaviorOnTheExplicitSubject(int type, uint value)
    {
        using var fixture = new Fixture(Row(1, (PrerequisiteType)type, value));
        IUnitEntity subject = Unit(2, 20, out var proxy);
        var movement = RecordingDispatchProxy<IMovementManager>.Create(out var movementProxy);
        movementProxy.SetMethodReturn(nameof(IMovementManager.GetMode), ModeType.Swim);
        movementProxy.SetMethodReturn(nameof(IMovementManager.GetState), StateFlags.Jump | StateFlags.Move);
        movementProxy.SetProperty(nameof(IMovementManager.ServerControl), true);
        proxy.SetProperty(nameof(IUnitEntity.MovementManager), movement);
        proxy.SetProperty(nameof(IUnitEntity.PublicEventPhase), 7u);
        proxy.SetProperty(nameof(IUnitEntity.Health), 45u);
        proxy.SetProperty(nameof(IUnitEntity.MaxHealth), 90u);
        proxy.SetProperty(nameof(IUnitEntity.Type), EntityType.Pet);
        proxy.SetMethodReturn(nameof(IUnitEntity.HasTrackedSpellState), true);
        // Type 129 now tests ObjectId as a spell group id on the unit (PREREQUISITE_ACTIVE_SPELL_GROUP_20261107.md); the subject answers it, the unrelated target does not.
        proxy.SetMethodReturn(nameof(IUnitEntity.HasActiveSpellInGroup), true);
        Assert.True(fixture.Manager.MeetsUnit(subject, 1, new PrerequisiteParameters { Target = Unit(3, 10, out _) }));
    }

    // Native case 0x1c reads the primary unit's combat field, not the relationship unit.
    [Theory]
    [InlineData(true, PrerequisiteComparison.Equal)]
    [InlineData(false, PrerequisiteComparison.Equal)]
    [InlineData(true, PrerequisiteComparison.NotEqual)]
    [InlineData(false, PrerequisiteComparison.NotEqual)]
    public void InCombatPlayerAndUnitEntryPointsRetainThePrimarySubject(bool inCombat, PrerequisiteComparison comparison)
    {
        PrerequisiteEntry row = Row(1, PrerequisiteType.InCombat, 0);
        row.PrerequisiteComparisonId = [comparison];
        using var fixture = new Fixture(row);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IUnitEntity target = Unit(2, 20, out var targetProxy);
        playerProxy.SetProperty(nameof(IUnitEntity.InCombat), inCombat);
        targetProxy.SetProperty(nameof(IUnitEntity.InCombat), !inCombat);
        var parameters = new PrerequisiteParameters { Target = target };
        bool expected = comparison == PrerequisiteComparison.Equal ? inCombat : !inCombat;

        Assert.Equal(expected, fixture.Manager.MeetsUnit(player, 1, parameters));
        Assert.Equal(expected, fixture.Manager.Meets(player, 1, parameters));
        Assert.Equal(!expected, fixture.Manager.MeetsUnit(target, 1, new PrerequisiteParameters { Target = player }));
        Assert.Same(target, parameters.Target);
    }

    [Theory]
    [InlineData(true, PrerequisiteComparison.Equal)]
    [InlineData(false, PrerequisiteComparison.NotEqual)]
    public void InCombatNullPrimaryIsNotReplacedByTheRelationship(bool targetInCombat, PrerequisiteComparison comparison)
    {
        IUnitEntity target = Unit(2, 20, out var targetProxy);
        targetProxy.SetProperty(nameof(IUnitEntity.InCombat), targetInCombat);
        var check = new PrerequisiteCheckInCombat();
        var parameters = new PrerequisiteParameters { Target = target };

        Assert.False(check.Meets(null, comparison, 0u, 0u, parameters));
        Assert.False(check.MeetsUnit(null, comparison, 0u, 0u, parameters));
    }

    [Fact]
    public void InCombatDoesNotRequireRelationshipParameters()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetProperty(nameof(IUnitEntity.InCombat), true);
        var check = new PrerequisiteCheckInCombat();

        Assert.True(check.Meets(player, PrerequisiteComparison.Equal, 0u, 0u, null));
        Assert.True(check.MeetsUnit(player, PrerequisiteComparison.Equal, 0u, 0u, null));
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, true)]
    [InlineData(PrerequisiteComparison.NotEqual, false)]
    public void IsPlayerPlayerAndUnitEntryPointsRetainThePrimarySubject(PrerequisiteComparison comparison, bool expected)
    {
        PrerequisiteEntry row = Row(1, PrerequisiteType.IsPlayer, 0);
        row.PrerequisiteComparisonId = [comparison];
        using var fixture = new Fixture(row);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetProperty(nameof(IWorldEntity.Type), EntityType.Player);
        IUnitEntity target = Unit(2, 20, out var targetProxy);
        targetProxy.SetProperty(nameof(IWorldEntity.Type), EntityType.NonPlayer);
        var parameters = new PrerequisiteParameters { Target = target };

        Assert.Equal(expected, fixture.Manager.MeetsUnit(player, 1, parameters));
        Assert.Equal(expected, fixture.Manager.Meets(player, 1, parameters));
        Assert.Equal(!expected, fixture.Manager.MeetsUnit(target, 1, new PrerequisiteParameters { Target = player }));
        Assert.Same(target, parameters.Target);
    }

    [Theory]
    [InlineData(true, PrerequisiteComparison.Equal)]
    [InlineData(false, PrerequisiteComparison.NotEqual)]
    public void IsPlayerNullPrimaryIsNotReplacedByTheRelationship(bool targetIsPlayer, PrerequisiteComparison comparison)
    {
        IUnitEntity target = targetIsPlayer
            ? RecordingDispatchProxy<IPlayer>.Create(out _)
            : Unit(2, 20, out _);
        var check = new PrerequisiteCheckIsPlayer();
        var parameters = new PrerequisiteParameters { Target = target };

        Assert.False(check.Meets(null, comparison, 0u, 0u, parameters));
        Assert.False(check.MeetsUnit(null, comparison, 0u, 0u, parameters));
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, true)]
    [InlineData(PrerequisiteComparison.NotEqual, false)]
    public void IsPlayerDoesNotRequireRelationshipParameters(PrerequisiteComparison comparison, bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        var check = new PrerequisiteCheckIsPlayer();

        Assert.Equal(expected, check.Meets(player, comparison, 0u, 0u, null));
        Assert.Equal(expected, check.MeetsUnit(player, comparison, 0u, 0u, null));
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, true)]
    [InlineData(PrerequisiteComparison.NotEqual, false)]
    [InlineData(PrerequisiteComparison.GreaterThan, false)]
    [InlineData(PrerequisiteComparison.GreaterThanOrEqual, true)]
    [InlineData(PrerequisiteComparison.LessThan, false)]
    [InlineData(PrerequisiteComparison.LessThanOrEqual, true)]
    public void HealthPlayerAndUnitEntryPointsRetainThePrimarySubject(PrerequisiteComparison comparison, bool expected)
    {
        PrerequisiteEntry row = Row(1, PrerequisiteType.Health, 50);
        row.PrerequisiteComparisonId = [comparison];
        using var fixture = new Fixture(row);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetProperty(nameof(IWorldEntity.Health), 150u);
        playerProxy.SetProperty(nameof(IWorldEntity.MaxHealth), 300u);
        IUnitEntity target = Unit(2, 20, out var targetProxy);
        targetProxy.SetProperty(nameof(IWorldEntity.Health), 900u);
        targetProxy.SetProperty(nameof(IWorldEntity.MaxHealth), 1000u);
        var parameters = new PrerequisiteParameters { Target = target };
        bool explicitTargetExpected = comparison is PrerequisiteComparison.NotEqual
            or PrerequisiteComparison.GreaterThan or PrerequisiteComparison.GreaterThanOrEqual;

        Assert.Equal(expected, fixture.Manager.MeetsUnit(player, 1, parameters));
        Assert.Equal(expected, fixture.Manager.Meets(player, 1, parameters));
        Assert.Equal(explicitTargetExpected, fixture.Manager.MeetsUnit(target, 1, new PrerequisiteParameters { Target = player }));
        Assert.Same(target, parameters.Target);
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal)]
    [InlineData(PrerequisiteComparison.GreaterThanOrEqual)]
    public void HealthNullPrimaryIsNotReplacedByTheRelationship(PrerequisiteComparison comparison)
    {
        IUnitEntity target = Unit(2, 20, out var targetProxy);
        targetProxy.SetProperty(nameof(IWorldEntity.Health), 150u);
        targetProxy.SetProperty(nameof(IWorldEntity.MaxHealth), 300u);
        var check = new PrerequisiteCheckHealth();
        var parameters = new PrerequisiteParameters { Target = target };

        Assert.False(check.Meets(null, comparison, 50u, 0u, parameters));
        Assert.False(check.MeetsUnit(null, comparison, 50u, 0u, parameters));
    }

    [Fact]
    public void HealthDoesNotRequireRelationshipParameters()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetProperty(nameof(IWorldEntity.Health), 150u);
        playerProxy.SetProperty(nameof(IWorldEntity.MaxHealth), 300u);
        var check = new PrerequisiteCheckHealth();

        Assert.True(check.Meets(player, PrerequisiteComparison.Equal, 50u, 0u, null));
        Assert.True(check.MeetsUnit(player, PrerequisiteComparison.Equal, 50u, 0u, null));
    }

    [Theory]
    [InlineData(true, PrerequisiteComparison.Equal, 0u)]
    [InlineData(false, PrerequisiteComparison.Equal, 0u)]
    [InlineData(true, PrerequisiteComparison.NotEqual, 0u)]
    [InlineData(false, PrerequisiteComparison.NotEqual, 0u)]
    [InlineData(true, PrerequisiteComparison.Equal, 1u)]
    [InlineData(false, PrerequisiteComparison.Equal, 1u)]
    [InlineData(true, PrerequisiteComparison.NotEqual, 1u)]
    [InlineData(false, PrerequisiteComparison.NotEqual, 1u)]
    public void DeadStatePlayerAndUnitEntryPointsRetainThePrimarySubject(bool alive, PrerequisiteComparison comparison, uint value)
    {
        PrerequisiteEntry row = Row(1, PrerequisiteType.DeadState, value);
        row.PrerequisiteComparisonId = [comparison];
        using var fixture = new Fixture(row);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetProperty(nameof(IUnitEntity.IsAlive), alive);
        playerProxy.SetProperty(nameof(IWorldEntity.Type), EntityType.Player);
        IUnitEntity target = Unit(2, 20, out var targetProxy);
        targetProxy.SetProperty(nameof(IUnitEntity.IsAlive), !alive);
        targetProxy.SetProperty(nameof(IWorldEntity.Type), EntityType.NonPlayer);
        var parameters = new PrerequisiteParameters { Target = target };
        bool expected = comparison == PrerequisiteComparison.Equal
            ? (alive ? 0u : 1u) == value
            : (alive ? 0u : 1u) != value;

        Assert.Equal(expected, fixture.Manager.MeetsUnit(player, 1, parameters));
        Assert.Equal(expected, fixture.Manager.Meets(player, 1, parameters));
        Assert.Equal(!expected, fixture.Manager.MeetsUnit(target, 1, new PrerequisiteParameters { Target = player }));
        Assert.Same(target, parameters.Target);
    }

    [Theory]
    [InlineData(true, PrerequisiteComparison.Equal, 0u)]
    [InlineData(false, PrerequisiteComparison.Equal, 1u)]
    [InlineData(true, PrerequisiteComparison.NotEqual, 1u)]
    [InlineData(false, PrerequisiteComparison.NotEqual, 0u)]
    public void DeadStateNullPrimaryIsNotReplacedByTheRelationship(bool targetAlive, PrerequisiteComparison comparison, uint value)
    {
        IUnitEntity target = Unit(2, 20, out var targetProxy);
        targetProxy.SetProperty(nameof(IUnitEntity.IsAlive), targetAlive);
        var check = new PrerequisiteCheckDeadState();
        var parameters = new PrerequisiteParameters { Target = target };

        Assert.False(check.Meets(null, comparison, value, 0u, parameters));
        Assert.False(check.MeetsUnit(null, comparison, value, 0u, parameters));
    }

    [Fact]
    public void DeadStateDoesNotRequireRelationshipParameters()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetProperty(nameof(IUnitEntity.IsAlive), true);
        playerProxy.SetProperty(nameof(IWorldEntity.Type), EntityType.Player);
        var check = new PrerequisiteCheckDeadState();

        Assert.True(check.Meets(player, PrerequisiteComparison.Equal, 0u, 0u, null));
        Assert.True(check.MeetsUnit(player, PrerequisiteComparison.Equal, 0u, 0u, null));
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, true)]
    [InlineData(PrerequisiteComparison.NotEqual, false)]
    public void DeadStateGhostRelationshipDoesNotReplaceALivingPrimary(PrerequisiteComparison comparison, bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetProperty(nameof(IUnitEntity.IsAlive), true);
        playerProxy.SetProperty(nameof(IWorldEntity.Type), EntityType.Player);
        IUnitEntity ghost = Unit(2, 20, out var ghostProxy);
        ghostProxy.SetProperty(nameof(IUnitEntity.IsAlive), true);
        ghostProxy.SetProperty(nameof(IWorldEntity.Type), EntityType.Ghost);
        var check = new PrerequisiteCheckDeadState();

        Assert.Equal(expected, check.Meets(player, comparison, 0u, 0u, new PrerequisiteParameters { Target = ghost }));
        Assert.Equal(!expected, check.MeetsUnit(ghost, comparison, 0u, 0u, new PrerequisiteParameters { Target = player }));
    }

    [Theory]
    [InlineData(true, PrerequisiteComparison.Equal)]
    [InlineData(false, PrerequisiteComparison.Equal)]
    [InlineData(true, PrerequisiteComparison.NotEqual)]
    [InlineData(false, PrerequisiteComparison.NotEqual)]
    public void JumpPlayerAndUnitEntryPointsRetainThePrimarySubject(bool jumping, PrerequisiteComparison comparison)
    {
        PrerequisiteEntry row = Row(1, PrerequisiteType.Jump, 0);
        row.PrerequisiteComparisonId = [comparison];
        using var fixture = new Fixture(row);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IMovementManager playerMovement = RecordingDispatchProxy<IMovementManager>.Create(out var playerMovementProxy);
        playerMovementProxy.SetMethodReturn(nameof(IMovementManager.GetState), jumping ? StateFlags.Jump : StateFlags.Fall | StateFlags.DoubleJump);
        playerProxy.SetProperty(nameof(IWorldEntity.MovementManager), playerMovement);
        IUnitEntity target = Unit(2, 20, out var targetProxy);
        IMovementManager targetMovement = RecordingDispatchProxy<IMovementManager>.Create(out var targetMovementProxy);
        targetMovementProxy.SetMethodReturn(nameof(IMovementManager.GetState), jumping ? StateFlags.Fall | StateFlags.DoubleJump : StateFlags.Jump);
        targetProxy.SetProperty(nameof(IWorldEntity.MovementManager), targetMovement);
        var parameters = new PrerequisiteParameters { Target = target };
        bool expected = comparison == PrerequisiteComparison.Equal ? jumping : !jumping;

        Assert.Equal(expected, fixture.Manager.MeetsUnit(player, 1, parameters));
        Assert.Equal(expected, fixture.Manager.Meets(player, 1, parameters));
        Assert.Equal(!expected, fixture.Manager.MeetsUnit(target, 1, new PrerequisiteParameters { Target = player }));
        Assert.Same(target, parameters.Target);
    }

    [Theory]
    [InlineData(true, PrerequisiteComparison.Equal)]
    [InlineData(false, PrerequisiteComparison.NotEqual)]
    public void JumpNullPrimaryIsNotReplacedByTheRelationship(bool targetJumping, PrerequisiteComparison comparison)
    {
        IUnitEntity target = Unit(2, 20, out var targetProxy);
        IMovementManager movement = RecordingDispatchProxy<IMovementManager>.Create(out var movementProxy);
        movementProxy.SetMethodReturn(nameof(IMovementManager.GetState), targetJumping ? StateFlags.Jump : StateFlags.None);
        targetProxy.SetProperty(nameof(IWorldEntity.MovementManager), movement);

        Assert.False(new PrerequisiteCheckJump().Meets(null, comparison, 0u, 0u, new PrerequisiteParameters { Target = target }));
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal)]
    [InlineData(PrerequisiteComparison.NotEqual)]
    public void JumpNullUnitFailsForBothBooleanModes(PrerequisiteComparison comparison)
    {
        Assert.False(new PrerequisiteCheckJump().MeetsUnit(null, comparison, 0u, 0u, null));
    }

    [Fact]
    public void JumpDoesNotRequireRelationshipParameters()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IMovementManager movement = RecordingDispatchProxy<IMovementManager>.Create(out var movementProxy);
        movementProxy.SetMethodReturn(nameof(IMovementManager.GetState), StateFlags.Jump);
        playerProxy.SetProperty(nameof(IWorldEntity.MovementManager), movement);
        var check = new PrerequisiteCheckJump();

        Assert.True(check.MeetsUnit(player, PrerequisiteComparison.Equal, 0u, 0u, null));
        Assert.True(check.Meets(player, PrerequisiteComparison.Equal, 0u, 0u, null));
    }

    [Theory]
    [InlineData(true, PrerequisiteComparison.Equal)]
    [InlineData(false, PrerequisiteComparison.Equal)]
    [InlineData(true, PrerequisiteComparison.NotEqual)]
    [InlineData(false, PrerequisiteComparison.NotEqual)]
    public void MovingPlayerAndUnitEntryPointsRetainThePrimarySubject(bool moving, PrerequisiteComparison comparison)
    {
        PrerequisiteEntry row = Row(1, PrerequisiteType.Moving, 0);
        row.PrerequisiteComparisonId = [comparison];
        using var fixture = new Fixture(row);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IMovementManager playerMovement = RecordingDispatchProxy<IMovementManager>.Create(out var playerMovementProxy);
        playerMovementProxy.SetMethodReturn(nameof(IMovementManager.GetState), moving ? StateFlags.Move : StateFlags.None);
        playerProxy.SetProperty(nameof(IWorldEntity.MovementManager), playerMovement);
        IUnitEntity target = Unit(2, 20, out var targetProxy);
        IMovementManager targetMovement = RecordingDispatchProxy<IMovementManager>.Create(out var targetMovementProxy);
        targetMovementProxy.SetMethodReturn(nameof(IMovementManager.GetState), moving ? StateFlags.None : StateFlags.Move);
        targetProxy.SetProperty(nameof(IWorldEntity.MovementManager), targetMovement);
        var parameters = new PrerequisiteParameters { Target = target };
        bool expected = comparison == PrerequisiteComparison.Equal ? moving : !moving;

        Assert.Equal(expected, fixture.Manager.MeetsUnit(player, 1, parameters));
        Assert.Equal(expected, fixture.Manager.Meets(player, 1, parameters));
        Assert.Equal(!expected, fixture.Manager.MeetsUnit(target, 1, new PrerequisiteParameters { Target = player }));
        Assert.Same(target, parameters.Target);
    }

    [Theory]
    [InlineData(true, PrerequisiteComparison.Equal)]
    [InlineData(false, PrerequisiteComparison.NotEqual)]
    public void MovingNullPrimaryIsNotReplacedByTheRelationship(bool targetMoving, PrerequisiteComparison comparison)
    {
        IUnitEntity target = Unit(2, 20, out var targetProxy);
        IMovementManager movement = RecordingDispatchProxy<IMovementManager>.Create(out var movementProxy);
        movementProxy.SetMethodReturn(nameof(IMovementManager.GetState), targetMoving ? StateFlags.Move : StateFlags.None);
        targetProxy.SetProperty(nameof(IWorldEntity.MovementManager), movement);

        Assert.False(new PrerequisiteCheckMoving().Meets(null, comparison, 0u, 0u, new PrerequisiteParameters { Target = target }));
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal)]
    [InlineData(PrerequisiteComparison.NotEqual)]
    public void MovingNullUnitFailsForBothBooleanModes(PrerequisiteComparison comparison)
    {
        Assert.False(new PrerequisiteCheckMoving().MeetsUnit(null, comparison, 0u, 0u, null));
    }

    [Fact]
    public void MovingDoesNotRequireRelationshipParameters()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IMovementManager movement = RecordingDispatchProxy<IMovementManager>.Create(out var movementProxy);
        movementProxy.SetMethodReturn(nameof(IMovementManager.GetState), StateFlags.Move);
        playerProxy.SetProperty(nameof(IWorldEntity.MovementManager), movement);
        var check = new PrerequisiteCheckMoving();

        Assert.True(check.MeetsUnit(player, PrerequisiteComparison.Equal, 0u, 0u, null));
        Assert.True(check.Meets(player, PrerequisiteComparison.Equal, 0u, 0u, null));
    }

    [Theory]
    [InlineData(false, false, PrerequisiteComparison.Equal, 0u)]
    [InlineData(true, false, PrerequisiteComparison.Equal, 0u)]
    [InlineData(false, true, PrerequisiteComparison.Equal, 0u)]
    [InlineData(true, true, PrerequisiteComparison.Equal, 0u)]
    [InlineData(false, false, PrerequisiteComparison.NotEqual, 0u)]
    [InlineData(true, false, PrerequisiteComparison.NotEqual, 0u)]
    [InlineData(false, true, PrerequisiteComparison.NotEqual, 0u)]
    [InlineData(true, true, PrerequisiteComparison.NotEqual, 0u)]
    [InlineData(false, false, PrerequisiteComparison.Equal, 1u)]
    [InlineData(true, false, PrerequisiteComparison.Equal, 1u)]
    [InlineData(false, true, PrerequisiteComparison.Equal, 1u)]
    [InlineData(true, true, PrerequisiteComparison.Equal, 1u)]
    [InlineData(false, false, PrerequisiteComparison.NotEqual, 1u)]
    [InlineData(true, false, PrerequisiteComparison.NotEqual, 1u)]
    [InlineData(false, true, PrerequisiteComparison.NotEqual, 1u)]
    [InlineData(true, true, PrerequisiteComparison.NotEqual, 1u)]
    public void StatePlayerAndUnitEntryPointsRetainThePrimarySubject(bool busy, bool combat, PrerequisiteComparison comparison, uint value)
    {
        PrerequisiteEntry row = Row(1, PrerequisiteType.State, value);
        row.PrerequisiteComparisonId = [comparison];
        using var fixture = new Fixture(row);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetProperty(nameof(IWorldEntity.IsBusy), busy);
        playerProxy.SetProperty(nameof(IUnitEntity.InCombat), combat);
        IUnitEntity target = Unit(2, 20, out var targetProxy);
        targetProxy.SetProperty(nameof(IWorldEntity.IsBusy), !(busy || combat));
        targetProxy.SetProperty(nameof(IUnitEntity.InCombat), false);
        var parameters = new PrerequisiteParameters { Target = target };
        uint state = busy || combat ? 1u : 0u;
        bool expected = comparison == PrerequisiteComparison.Equal ? state == value : state != value;

        Assert.Equal(expected, fixture.Manager.MeetsUnit(player, 1, parameters));
        Assert.Equal(expected, fixture.Manager.Meets(player, 1, parameters));
        Assert.Equal(!expected, fixture.Manager.MeetsUnit(target, 1, new PrerequisiteParameters { Target = player }));
        Assert.Same(target, parameters.Target);
    }

    [Theory]
    [InlineData(false, PrerequisiteComparison.Equal, 0u)]
    [InlineData(true, PrerequisiteComparison.Equal, 1u)]
    [InlineData(false, PrerequisiteComparison.NotEqual, 1u)]
    [InlineData(true, PrerequisiteComparison.NotEqual, 0u)]
    public void StateNullPrimaryIsNotReplacedByTheRelationship(bool targetBusy, PrerequisiteComparison comparison, uint value)
    {
        IUnitEntity target = Unit(2, 20, out var targetProxy);
        targetProxy.SetProperty(nameof(IWorldEntity.IsBusy), targetBusy);

        Assert.False(new PrerequisiteCheckEntityState().Meets(null, comparison, value, 0u, new PrerequisiteParameters { Target = target }));
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal)]
    [InlineData(PrerequisiteComparison.NotEqual)]
    public void StateNullUnitFailsForBothBooleanModes(PrerequisiteComparison comparison)
    {
        Assert.False(new PrerequisiteCheckEntityState().MeetsUnit(null, comparison, 0u, 0u, null));
    }

    [Fact]
    public void StateDoesNotRequireRelationshipParameters()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetProperty(nameof(IUnitEntity.InCombat), true);
        var check = new PrerequisiteCheckEntityState();

        Assert.True(check.MeetsUnit(player, PrerequisiteComparison.Equal, 1u, 0u, null));
        Assert.True(check.Meets(player, PrerequisiteComparison.Equal, 1u, 0u, null));
    }

    [Theory]
    [InlineData(EvaluationMode.EvaluateAND, false)]
    [InlineData(EvaluationMode.EvaluateOR, true)]
    public void MixedNpcRowsRetainAndOrSemanticsAndUnsupportedPlayerChecksFailClosed(EvaluationMode mode, bool expected)
    {
        var row = Row(1, PrerequisiteType.Level, 20);
        row.Flags = mode;
        row.PrerequisiteTypeId = [PrerequisiteType.Level, PrerequisiteType.Class];
        row.PrerequisiteComparisonId = [PrerequisiteComparison.Equal, PrerequisiteComparison.Equal];
        row.Value = [20, (uint)Class.Warrior];
        row.ObjectId = [0, 0];
        using var fixture = new Fixture(row);
        Assert.Equal(expected, fixture.Manager.MeetsUnit(Unit(2, 20, out _), 1, null));
    }

    [Fact]
    public void PlayerOnlyDefaultAdapterStillCallsExistingHandlerWithAllParameters()
    {
        IPrerequisiteCheck check = new LegacyCheck();
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        var parameters = new PrerequisiteParameters();
        Assert.True(check.MeetsUnit(player, PrerequisiteComparison.Equal, 1, 2, parameters));
        Assert.Same(parameters, ((LegacyCheck)check).Parameters);
        Assert.False(check.MeetsUnit(Unit(2, 20, out _), PrerequisiteComparison.NotEqual, 1, 2, parameters));
    }

    [Fact]
    public void ExistingCustomManagerRemainsCompatibleWithNewPlayerEntryPoint()
    {
        IPrerequisiteManager manager = new LegacyManager();
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        Assert.True(manager.MeetsUnit(player, 1, new PrerequisiteParameters()));
        Assert.False(manager.MeetsUnit(Unit(2, 20, out _), 1, new PrerequisiteParameters()));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NestedPrerequisitesPreserveItemTargetTaxiAndAccountContext(bool unitApi)
    {
        var nested = Row(1, PrerequisiteType.OtherPrerequisite, 0, 2);
        var leaf = Row(2, PrerequisiteType.EvalContextFloatByObjectId, 0);
        using var fixture = new Fixture([nested, leaf], services =>
            services.AddKeyedSingleton<IPrerequisiteCheck>(PrerequisiteType.EvalContextFloatByObjectId, new ContextCheck()));
        IPlayer subject = RecordingDispatchProxy<IPlayer>.Create(out _);
        var context = new PrerequisiteParameters
        {
            Target = Unit(3, 10, out _), Item = RecordingDispatchProxy<IItem>.Create(out _), TaxiNode = 37,
            AccountItemContext = true
        };
        Assert.True(unitApi ? fixture.Manager.MeetsUnit(subject, 1, context) : fixture.Manager.Meets(subject, 1, context));
        var check = Assert.IsType<ContextCheck>(fixture.Services.GetRequiredKeyedService<IPrerequisiteCheck>(PrerequisiteType.EvalContextFloatByObjectId));
        Assert.Same(context, check.Parameters);
    }

    [Fact]
    public void NestedNpcPrerequisiteRetainsSubjectAndSeparateTarget()
    {
        using var fixture = new Fixture(Row(1, PrerequisiteType.OtherPrerequisite, 0, 2), Row(2, PrerequisiteType.Level, 20));
        Assert.True(fixture.Manager.MeetsUnit(Unit(2, 20, out _), 1, new PrerequisiteParameters { Target = Unit(3, 10, out _) }));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NestedItemPrerequisiteUsesTheSuppliedItemInsteadOfAnEmptyContext(bool unitApi)
    {
        using var fixture = new Fixture(Row(1, PrerequisiteType.OtherPrerequisite, 0, 2), Row(2, PrerequisiteType.Item2Id, 0, 123));
        var subject = Player(1, 10);
        var item = RecordingDispatchProxy<IItem>.Create(out var itemProxy);
        var info = RecordingDispatchProxy<IItemInfo>.Create(out var infoProxy);
        infoProxy.SetProperty(nameof(IItemInfo.Id), 123u);
        itemProxy.SetProperty(nameof(IItem.Info), info);
        var parameters = new PrerequisiteParameters { Item = item };
        Assert.True(unitApi ? fixture.Manager.MeetsUnit(subject, 1, parameters) : fixture.Manager.Meets(subject, 1, parameters));
        parameters.Item = null;
        Assert.False(unitApi ? fixture.Manager.MeetsUnit(subject, 1, parameters) : fixture.Manager.Meets(subject, 1, parameters));
    }

    [Fact]
    public void MissingSubjectRowHandlerAndInvalidEvaluationModeFailClosed()
    {
        var unknown = Row(1, (PrerequisiteType)999, 0);
        var invalid = Row(2, PrerequisiteType.Level, 20);
        invalid.Flags = (EvaluationMode)255;
        using var fixture = new Fixture(unknown, invalid);
        var subject = Unit(2, 20, out _);
        Assert.False(fixture.Manager.MeetsUnit(null, 1, null));
        Assert.False(fixture.Manager.MeetsUnit(subject, 0, null));
        Assert.False(fixture.Manager.MeetsUnit(subject, 1, null));
        Assert.False(fixture.Manager.MeetsUnit(subject, 2, null));
    }

    [Fact]
    public void CrossTargetSpellAndDistanceAdaptersRetainSubjectAndOtherUnit()
    {
        using var fixture = new Fixture(Row(1, PrerequisiteType.UnderSpellOnTarget, 123), Row(2, PrerequisiteType.Distance, 5));
        var subject = Unit(2, 20, out var subjectProxy);
        var target = Unit(3, 10, out var targetProxy);
        subjectProxy.SetProperty(nameof(IUnitEntity.Position), Vector3.Zero);
        targetProxy.SetProperty(nameof(IUnitEntity.Position), new Vector3(3, 0, 4));
        // native type 50: the subject holds spell 123 cast by the target (superseded: the target holding it)
        subjectProxy.SetMethodHandler(nameof(IUnitEntity.GetActiveSpellStackCount), args => (uint)args[0] == 123u && (uint)args[1] == target.Guid ? 1u : 0u);
        var parameters = new PrerequisiteParameters { Target = target };
        Assert.True(fixture.Manager.MeetsUnit(subject, 1, parameters));
        Assert.True(fixture.Manager.MeetsUnit(subject, 2, parameters));
        Assert.False(fixture.Manager.MeetsUnit(target, 1, new PrerequisiteParameters { Target = subject }));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CasterAndTargetCastPrerequisitesEvaluateTheirOwnUnit(bool playerCaster)
    {
        using var fixture = new Fixture(Row(1, PrerequisiteType.Level, 10), Row(2, PrerequisiteType.Level, 20));
        var caster = playerCaster ? Player(1, 10) : Unit(1, 10, out _);
        var target = Unit(2, 20, out _);
        var spell = fixture.Spell(caster, target, out var info);
        info.SetProperty(nameof(ISpellInfo.CasterCastPrerequisite), Row(1, PrerequisiteType.Level, 10));
        info.SetProperty(nameof(ISpellInfo.TargetCastPrerequisites), Row(2, PrerequisiteType.Level, 20));
        Assert.Equal(CastResult.Ok, Invoke<CastResult>(spell, "CheckPrerequisites"));
        info.SetProperty(nameof(ISpellInfo.TargetCastPrerequisites), Row(1, PrerequisiteType.Level, 10));
        Assert.Equal(CastResult.PrereqTargetCast, Invoke<CastResult>(spell, "CheckPrerequisites"));
    }

    [Fact]
    public void CastTargetGateRejectsMissingExplicitTargetAndUsesCasterForImplicitSelf()
    {
        using var fixture = new Fixture(Row(1, PrerequisiteType.Level, 10));
        var caster = Unit(1, 10, out _);
        var spell = fixture.Spell(caster, null, out var info);
        info.SetProperty(nameof(ISpellInfo.TargetCastPrerequisites), Row(1, PrerequisiteType.Level, 10));
        spell.Parameters.PrimaryTargetId = 99;
        Assert.Equal(CastResult.PrereqTargetCast, Invoke<CastResult>(spell, "CheckPrerequisites"));
        spell.Parameters.PrimaryTargetId = 0;
        Assert.Equal(CastResult.Ok, Invoke<CastResult>(spell, "CheckPrerequisites"));
    }

    [Fact]
    public void NpcRunnerCanOverrideCasterGateWithoutSkippingTargetGate()
    {
        using var fixture = new Fixture(Row(1, PrerequisiteType.Level, 999), Row(2, PrerequisiteType.Level, 10));
        var caster = Unit(1, 10, out _);
        var spell = fixture.Spell(caster, Unit(2, 20, out _), out var info);
        info.SetProperty(nameof(ISpellInfo.CasterCastPrerequisite), Row(1, PrerequisiteType.Level, 999));
        info.SetProperty(nameof(ISpellInfo.PrerequisiteRunners), new List<PrerequisiteEntry> { Row(2, PrerequisiteType.Level, 10) });
        Assert.Equal(CastResult.Ok, Invoke<CastResult>(spell, "CheckPrerequisites"));
        info.SetProperty(nameof(ISpellInfo.TargetCastPrerequisites), Row(1, PrerequisiteType.Level, 999));
        Assert.Equal(CastResult.PrereqTargetCast, Invoke<CastResult>(spell, "CheckPrerequisites"));
    }

    [Fact]
    public void ApplyAndPersistenceCheckBothNpcSubjectsWithoutReversingThem()
    {
        using var fixture = new Fixture(Row(1, PrerequisiteType.Level, 10), Row(2, PrerequisiteType.Level, 20));
        var caster = Unit(1, 10, out _);
        var target = Unit(2, 20, out _);
        var spell = fixture.Spell(caster, target, out var info);
        var entry = new Spell4EffectsEntry { Id = 1, PrerequisiteIdCasterApply = 1, PrerequisiteIdTargetApply = 2,
            PrerequisiteIdCasterPersistence = 1, PrerequisiteIdTargetPersistence = 2 };
        var effect = SpellEffectInterpreter.Interpret(entry);
        Assert.True(Invoke<bool>(spell, "MeetsApplyPrerequisites", effect, target));
        Assert.True(Invoke<bool>(spell, "MeetsPersistencePrerequisites", effect, target));
        Assert.False(Invoke<bool>(spell, "MeetsApplyPrerequisites", effect, caster));
        Assert.False(Invoke<bool>(spell, "MeetsPersistencePrerequisites", effect, caster));
        info.SetProperty(nameof(ISpellInfo.TargetPersistencePrerequisites), Row(1, PrerequisiteType.Level, 10));
        Assert.False(Invoke<bool>(spell, "MeetsPersistencePrerequisites", effect, target));
    }

    [Fact]
    public void ProxyWordSixIsTestedOnTheCasterAndWordSevenOnTheTarget()
    {
        using var fixture = new Fixture(Row(1, PrerequisiteType.Level, 10), Row(2, PrerequisiteType.Level, 20));
        var caster = Unit(1, 10, out _);
        var target = Unit(2, 20, out _);
        var spell = fixture.Spell(caster, target, out _);

        var both = SpellEffectInterpreter.Interpret(new Spell4EffectsEntry { Id = 1, EffectType = SpellEffectType.Proxy, DataBits06 = 1, DataBits07 = 2 });
        Assert.True(Invoke<bool>(spell, "MeetsApplyPrerequisites", both, target));
        Assert.False(Invoke<bool>(spell, "MeetsApplyPrerequisites", both, caster));

        var casterFails = SpellEffectInterpreter.Interpret(new Spell4EffectsEntry { Id = 2, EffectType = SpellEffectType.Proxy, DataBits06 = 2 });
        Assert.False(Invoke<bool>(spell, "MeetsApplyPrerequisites", casterFails, target));

        // The words belong to Proxy only; other effect types use them for their own data.
        var otherType = SpellEffectInterpreter.Interpret(new Spell4EffectsEntry { Id = 3, EffectType = SpellEffectType.Damage, DataBits06 = 2, DataBits07 = 1 });
        Assert.True(Invoke<bool>(spell, "MeetsApplyPrerequisites", otherType, target));
    }

    // Retail: Medic Restrictor field (Spell4 38287, spellDuration 4000) row 131274 is a Proxy with tick 500 ms, no duration, DataBits01 = 77118; the tooltip reads
    // "a field that lasts for 4s. Apply a Snare to foes inside the field every 0.5s". Warrior Polarity Field (38975, 10000) has a tick-only Damage row with tick 1000.
    [Theory]
    [InlineData(4000u, 500u, 0u, 4000u)]          // tick-only row in a spell with a finite duration: pulses for the spell
    [InlineData(4000u, 500u, 6000u, 6000u)]       // the row's own duration wins
    [InlineData(0u, 500u, 0u, 0u)]                // no spell duration: nothing to pulse
    [InlineData(uint.MaxValue, 500u, 0u, 0u)]     // permanent spell that is not an Aura: nothing to pulse
    [InlineData(4000u, 0u, 0u, 0u)]               // no tick
    public void TickOnlyRowsPulseForTheFiniteDurationOfTheirSpell(uint spellDuration, uint tick, uint rowDuration, uint expected)
    {
        using var fixture = new Fixture();
        var caster = Unit(1, 10, out _);
        var spell = fixture.Spell(caster, null, out var info);
        info.SetProperty(nameof(ISpellInfo.Entry), new Spell4Entry { Id = 38287, SpellDuration = spellDuration });

        var damage = SpellEffectInterpreter.Interpret(new Spell4EffectsEntry { Id = 1, EffectType = SpellEffectType.Damage, TickTime = tick, DurationTime = rowDuration });
        Assert.Equal(expected, Invoke<uint>(spell, "GetEffectPulseDuration", damage));

        var proxy = SpellEffectInterpreter.Interpret(new Spell4EffectsEntry { Id = 131274, EffectType = SpellEffectType.Proxy, TickTime = tick, DurationTime = rowDuration, DataBits01 = 77118 });
        Assert.Equal(tick != 0u ? 77118u : 0u, proxy.Proxy.Spell4Id);
        // A periodic proxy with nothing to pulse it stays inert rather than firing once at cast time.
        Assert.Equal(tick != 0u && expected == 0u, Invoke<bool>(spell, "IsUnpulsedPeriodicProxy", proxy));
    }

    // A spell without a finite duration keeps its tick-only rows pulsing until the spell ends: permanent Auras (687 spells, for example Medic Danger Zone, Spell4 58912,
    // Proxy row 147851, tick 500 ms) and durationless passives and toggles (Warrior Power Link 53599, tick 250 ms; rune special Untouchable 81880, tick 1000 ms).
    // Every such row carries effect flag 0x2. Only rows that leave no state behind are pulsed.
    [Theory]
    [InlineData(uint.MaxValue, SpellEffectType.Proxy, 2u, uint.MaxValue)]
    [InlineData(uint.MaxValue, SpellEffectType.Damage, 2u, uint.MaxValue)]
    [InlineData(0u, SpellEffectType.Proxy, 2u, uint.MaxValue)]
    [InlineData(0u, SpellEffectType.VitalModifier, 2u, uint.MaxValue)]
    [InlineData(0u, SpellEffectType.UnitPropertyModifier, 2u, 0u)]     // leaves state behind
    [InlineData(0u, SpellEffectType.Proxy, 0u, 0u)]                    // not marked as living with the spell
    public void SpellsWithoutAFiniteDurationPulseStatelessTickOnlyRowsUntilTheSpellEnds(uint spellDuration, SpellEffectType effectType, uint effectFlags, uint expected)
    {
        using var fixture = new Fixture();
        var caster = Unit(1, 10, out _);
        var spell = fixture.Spell(caster, null, out var info);
        info.SetProperty(nameof(ISpellInfo.Entry), new Spell4Entry { Id = 53599, SpellDuration = spellDuration });

        var row = SpellEffectInterpreter.Interpret(new Spell4EffectsEntry { Id = 210449, EffectType = effectType, Flags = effectFlags, TickTime = 250, DataBits01 = 79787 });

        Assert.Equal(expected, Invoke<uint>(spell, "GetEffectPulseDuration", row));
    }

    // Warrior Power Link: the ticking rows carry the caster-apply prerequisite "the ON aura is not active", and the same cast applies that aura. The prerequisite therefore
    // decides whether the row starts and is not asked again on each pulse; a second press finds the aura active, so its rows do not start.
    [Fact]
    public void ARowThatLivesWithItsSpellTestsItsCasterApplyPrerequisiteOnceWhenItStarts()
    {
        using var fixture = new Fixture(Row(1, PrerequisiteType.Level, 10), Row(2, PrerequisiteType.Level, 99));
        var caster = Unit(1, 10, out var casterProxy);
        var spell = fixture.Spell(caster, null, out var info);
        info.SetProperty(nameof(ISpellInfo.Entry), new Spell4Entry { Id = 53599 });
        var baseInfo = RecordingDispatchProxy<ISpellBaseInfo>.Create(out var baseProxy);
        baseProxy.SetProperty(nameof(ISpellBaseInfo.Entry), new Spell4BaseEntry { Id = 35146 });
        info.SetProperty(nameof(ISpellInfo.BaseInfo), baseInfo);

        var starts = SpellEffectInterpreter.Interpret(new Spell4EffectsEntry
            { Id = 210449, EffectType = SpellEffectType.Proxy, Flags = 2, TargetFlags = 1, TickTime = 250, DataBits01 = 79787, PrerequisiteIdCasterApply = 1 });
        var blocked = SpellEffectInterpreter.Interpret(new Spell4EffectsEntry
            { Id = 210627, EffectType = SpellEffectType.Proxy, Flags = 2, TargetFlags = 1, TickTime = 250, DataBits01 = 79788, PrerequisiteIdCasterApply = 2 });

        Assert.True(Invoke<bool>(spell, "ScheduleEffect", starts));
        Assert.True(Invoke<bool>(spell, "ScheduleEffect", blocked));    // handled, but not started

        // the caster stops meeting prerequisite 1 once the toggle is on; the started row keeps passing, the other one never started
        casterProxy.SetProperty(nameof(IUnitEntity.Level), 11u);
        Assert.True(Invoke<bool>(spell, "MeetsApplyPrerequisites", starts, caster));
        Assert.False(Invoke<bool>(spell, "MeetsApplyPrerequisites", blocked, caster));
    }

    // A toggle is a running spell kept alive by its ticking rows. SpellForceRemove ends it: the "Spell Remove" of Warrior Power Link (Spell4 79789) names the base spell of the
    // toggle and is what the second press casts.
    [Fact]
    public void EndingASpellLifetimeFinishesTheSpellAndOnlySpellsThatHaveOne()
    {
        using var fixture = new Fixture();
        var caster = Unit(1, 10, out _);
        var spell = fixture.Spell(caster, null, out var info);
        info.SetProperty(nameof(ISpellInfo.Entry), new Spell4Entry { Id = 53599 });
        var baseInfo = RecordingDispatchProxy<ISpellBaseInfo>.Create(out var baseProxy);
        baseProxy.SetProperty(nameof(ISpellBaseInfo.Entry), new Spell4BaseEntry { Id = 35146 });
        info.SetProperty(nameof(ISpellInfo.BaseInfo), baseInfo);
        FieldInfo status = typeof(Game.Spell.Spell).GetField("status", BindingFlags.Instance | BindingFlags.NonPublic)!;
        status.SetValue(spell, Enum.Parse(status.FieldType, "Executing"));

        // nothing keeps it alive yet
        Assert.False(Invoke<bool>(spell, "EndSpellLifetime"));

        var row = SpellEffectInterpreter.Interpret(new Spell4EffectsEntry { Id = 210449, EffectType = SpellEffectType.Proxy, Flags = 2, TargetFlags = 1, TickTime = 250, DataBits01 = 79787 });
        Assert.True(Invoke<bool>(spell, "ScheduleEffect", row));

        Assert.False(spell.IsFinished);
        Assert.True(Invoke<bool>(spell, "EndSpellLifetime"));
        Assert.True(spell.IsFinished);
        Assert.False(Invoke<bool>(spell, "EndSpellLifetime"));
    }

    private static PrerequisiteEntry Row(uint id, PrerequisiteType type, uint value, uint objectId = 0)
        => new() { Id = id, Flags = EvaluationMode.EvaluateAND, PrerequisiteTypeId = [type],
            PrerequisiteComparisonId = [PrerequisiteComparison.Equal], Value = [value], ObjectId = [objectId] };

    private static IUnitEntity Unit(uint guid, uint level, out RecordingDispatchProxy<IUnitEntity> proxy)
    {
        var unit = RecordingDispatchProxy<IUnitEntity>.Create(out proxy);
        proxy.SetProperty(nameof(IUnitEntity.Guid), guid);
        proxy.SetProperty(nameof(IUnitEntity.Level), level);
        return unit;
    }

    private static IPlayer Player(uint guid, uint level)
    {
        var player = RecordingDispatchProxy<IPlayer>.Create(out var proxy);
        proxy.SetProperty(nameof(IPlayer.Guid), guid);
        proxy.SetProperty(nameof(IPlayer.Level), level);
        return player;
    }

    private static T Invoke<T>(object instance, string method, params object[] arguments)
        => (T)instance.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(instance, arguments);

    private sealed class Fixture : IDisposable
    {
        public ServiceProvider Services { get; }
        public IPrerequisiteManager Manager => Services.GetRequiredService<IPrerequisiteManager>();
        private IGameTableManager Tables { get; }

        public Fixture(params PrerequisiteEntry[] rows) : this(rows, null) { }

        public Fixture(PrerequisiteEntry[] rows, Action<IServiceCollection> configure)
        {
            var table = (GameTable<PrerequisiteEntry>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<PrerequisiteEntry>));
            typeof(GameTable<PrerequisiteEntry>).GetProperty(nameof(table.Entries))!.SetValue(table, rows);
            uint max = rows.Select(r => r.Id).DefaultIfEmpty().Max();
            typeof(GameTable<PrerequisiteEntry>).GetField("header", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(table, new GameTableHeader { MaxId = max + 1 });
            int[] lookup = Enumerable.Repeat(-1, (int)max + 1).ToArray();
            for (int i = 0; i < rows.Length; i++) lookup[rows[i].Id] = i;
            typeof(GameTable<PrerequisiteEntry>).GetField("lookup", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(table, lookup);
            Tables = RecordingDispatchProxy<IGameTableManager>.Create(out var tableProxy);
            tableProxy.SetProperty(nameof(IGameTableManager.Prerequisite), table);
            var services = new ServiceCollection().AddLogging().AddSingleton(Tables);
            services.AddGamePrerequisite();
            configure?.Invoke(services);
            Services = services.BuildServiceProvider();
        }

        public Game.Spell.Spell Spell(IUnitEntity caster, IUnitEntity target, out RecordingDispatchProxy<ISpellInfo> infoProxy)
        {
            // Visibility is the production target resolver, not a target stored on the spell test stub.
            if (caster is IPlayer)
                ((RecordingDispatchProxy<IPlayer>)(object)caster).SetMethodReturn(nameof(IPlayer.GetVisible), target);
            else
                ((RecordingDispatchProxy<IUnitEntity>)(object)caster).SetMethodReturn(nameof(IUnitEntity.GetVisible), target);
            var info = RecordingDispatchProxy<ISpellInfo>.Create(out infoProxy);
            infoProxy.SetProperty(nameof(ISpellInfo.Entry), new Spell4Entry { Id = 1 });
            infoProxy.SetProperty(nameof(ISpellInfo.PrerequisiteRunners), new List<PrerequisiteEntry>());
            var scriptCollection = RecordingDispatchProxy<IScriptCollection>.Create(out _);
            var scripts = RecordingDispatchProxy<IScriptManager>.Create(out var scriptsProxy);
            scriptsProxy.SetMethodReturn(nameof(IScriptManager.InitialiseOwnedScripts), scriptCollection);
            var global = RecordingDispatchProxy<IGlobalSpellManager>.Create(out _);
            return new Game.Spell.Spell(caster, new SpellParameters { SpellInfo = info, PrimaryTargetId = target?.Guid ?? 0 }, Manager, global, scripts, Tables);
        }

        public void Dispose() => Services.Dispose();
    }

    private sealed class LegacyCheck : IPrerequisiteCheck
    {
        public IPrerequisiteParameters Parameters { get; private set; }
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        { Parameters = parameters; return true; }
    }

    private sealed class ContextCheck : IPrerequisiteCheck
    {
        public IPrerequisiteParameters Parameters { get; private set; }
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        { Parameters = parameters; return parameters.Item != null && parameters.Target != null && parameters.TaxiNode == 37 && parameters.AccountItemContext; }
    }

    private sealed class LegacyManager : IPrerequisiteManager
    {
        public bool Meets(IPlayer player, uint prerequisiteId) => true;
        public bool Meets(IPlayer player, uint prerequisiteId, IPrerequisiteParameters parameters) => true;
    }
}
