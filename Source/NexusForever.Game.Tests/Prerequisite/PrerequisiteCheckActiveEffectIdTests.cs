using Microsoft.Extensions.DependencyInjection;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Prerequisite.Check;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Tests.Prerequisite;

// Native evidence (Decomp/Analysis/PREREQUISITE_ACTIVE_EFFECT_ID_20261107.md): type 102 (evaluator case 0x66 -> FUN_1404a4fe0) walks the unit's list at unit+0x15d0 and returns whether a node's
// virtual +0x30 equals ObjectId (the existing Spell4Effects interpretation comes from table correlation, not a node-class/lifetime read). A null unit is false for every comparison; Equal returns
// the match, NotEqual its negation, other comparisons false. Value is not read. Retail: 353 slots, NotEqual 249, Equal 104; Value is 0 in 254 of them.
// The enum name SpellCooldownNodeOnUnit is an older guess and the previous implementation looked up a cooldown node by Value.
// Verified native selector: both entry points retain the primary subject; see PREREQUISITE_102_PRIMARY_SUBJECT_20261114.md. Tests are Source tier; no retail parity claim.
public class PrerequisiteCheckActiveEffectIdTests
{
    // Verified native selector, Source tests of the existing effect-instance model.
    [Theory]
    [InlineData(true, PrerequisiteComparison.Equal, 0u)]
    [InlineData(false, PrerequisiteComparison.Equal, 0u)]
    [InlineData(true, PrerequisiteComparison.NotEqual, 0u)]
    [InlineData(false, PrerequisiteComparison.NotEqual, 0u)]
    [InlineData(true, PrerequisiteComparison.Equal, 45050u)]
    [InlineData(false, PrerequisiteComparison.Equal, 45050u)]
    [InlineData(true, PrerequisiteComparison.NotEqual, 45050u)]
    [InlineData(false, PrerequisiteComparison.NotEqual, 45050u)]
    public void PlayerAndExplicitUnitPathsRetainThePrimaryEffectSubject(bool primaryMatches, PrerequisiteComparison comparison, uint value)
    {
        var row = new PrerequisiteEntry
        {
            Id = 1u,
            Flags = EvaluationMode.EvaluateAND,
            PrerequisiteTypeId = [PrerequisiteType.SpellCooldownNodeOnUnit],
            PrerequisiteComparisonId = [comparison],
            ObjectId = [45049u],
            Value = [value]
        };
        using ServiceProvider services = Services(row);
        IPrerequisiteManager manager = services.GetRequiredService<IPrerequisiteManager>();
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetMethodHandler(nameof(IUnitEntity.HasActiveSpellEffectId), args => primaryMatches && (uint)args[0] == 45049u);
        IUnitEntity target = RecordingDispatchProxy<IUnitEntity>.Create(out var targetProxy);
        targetProxy.SetMethodHandler(nameof(IUnitEntity.HasActiveSpellEffectId), args => !primaryMatches && (uint)args[0] == 45049u);
        var parameters = new PrerequisiteParameters { Target = target };
        bool expected = comparison == PrerequisiteComparison.Equal ? primaryMatches : !primaryMatches;

        Assert.Equal(expected, manager.MeetsUnit(player, row.Id, parameters));
        Assert.Equal(expected, manager.Meets(player, row.Id, parameters));
        Assert.Equal(expected, Game.Spell.Spell.MeetsApplyPrerequisite(row, player, manager, target));
        Assert.Same(target, parameters.Target);
        Assert.Empty(targetProxy.GetInvocations(nameof(IUnitEntity.HasActiveSpellEffectId)));
        Assert.All(playerProxy.GetInvocations(nameof(IUnitEntity.HasActiveSpellEffectId)), call => Assert.Equal(45049u, call.Arguments[0]));
        Assert.Equal(!expected, manager.MeetsUnit(target, row.Id, new PrerequisiteParameters { Target = player }));
        Assert.Equal(!expected, Game.Spell.Spell.MeetsApplyPrerequisite(row, target, manager, player));
    }

    [Theory]
    [InlineData(true, PrerequisiteComparison.Equal)]
    [InlineData(false, PrerequisiteComparison.Equal)]
    [InlineData(true, PrerequisiteComparison.NotEqual)]
    [InlineData(false, PrerequisiteComparison.NotEqual)]
    public void NullPrimaryCannotBeReplacedByAnEffectSubject(bool targetMatches, PrerequisiteComparison comparison)
    {
        var check = new PrerequisiteCheckSpellCooldownNodeOnUnit();
        var parameters = new PrerequisiteParameters { Target = Unit(targetMatches ? 45049u : 45050u) };

        Assert.False(check.MeetsUnit(null, comparison, 45050u, 45049u, parameters));
        Assert.False(check.Meets(null, comparison, 45050u, 45049u, parameters));
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, true)]
    [InlineData(PrerequisiteComparison.NotEqual, false)]
    public void PlayerEffectMembershipDoesNotNeedRelationshipParameters(PrerequisiteComparison comparison, bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var proxy);
        proxy.SetMethodHandler(nameof(IUnitEntity.HasActiveSpellEffectId), args => (uint)args[0] == 45049u);

        Assert.Equal(expected, new PrerequisiteCheckSpellCooldownNodeOnUnit().Meets(player, comparison, 45050u, 45049u, null));
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, 45049u, 0u, true)]
    [InlineData(PrerequisiteComparison.Equal, 45050u, 0u, false)]
    [InlineData(PrerequisiteComparison.NotEqual, 45049u, 0u, false)]
    [InlineData(PrerequisiteComparison.NotEqual, 45050u, 0u, true)]
    [InlineData(PrerequisiteComparison.Equal, 45049u, 5856u, true)]       // Value is not read
    [InlineData(PrerequisiteComparison.GreaterThan, 45049u, 0u, false)]
    [InlineData(PrerequisiteComparison.LessThanOrEqual, 45050u, 0u, false)]
    public void ComparesWhetherTheUnitHasTheActiveSpellEffect(PrerequisiteComparison comparison, uint objectId, uint value, bool expected)
    {
        IUnitEntity unit = Unit(activeEffectId: 45049u);

        Assert.Equal(expected, new PrerequisiteCheckSpellCooldownNodeOnUnit().MeetsUnit(unit, comparison, value, objectId, new PrerequisiteParameters()));
    }

    [Fact]
    public void ThePlayerEntryPointRetainsThePlayerAndAllowsExplicitEffectTargetEvaluation()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetMethodHandler(nameof(IUnitEntity.HasActiveSpellEffectId), _ => false);
        IUnitEntity target = Unit(activeEffectId: 45049u);

        var check = new PrerequisiteCheckSpellCooldownNodeOnUnit();
        Assert.False(check.Meets(player, PrerequisiteComparison.Equal, 0u, 45049u, new PrerequisiteParameters { Target = target }));
        Assert.False(check.Meets(player, PrerequisiteComparison.Equal, 0u, 45049u, new PrerequisiteParameters()));
        Assert.True(check.MeetsUnit(target, PrerequisiteComparison.Equal, 0u, 45049u, new PrerequisiteParameters { Target = player }));
    }

    [Fact]
    public void ANullUnitFailsEvenForNotEqual()
    {
        Assert.False(new PrerequisiteCheckSpellCooldownNodeOnUnit().MeetsUnit(null, PrerequisiteComparison.NotEqual, 0u, 45049u, new PrerequisiteParameters()));
    }

    private static IUnitEntity Unit(uint activeEffectId)
    {
        IUnitEntity unit = RecordingDispatchProxy<IUnitEntity>.Create(out var proxy);
        proxy.SetMethodHandler(nameof(IUnitEntity.HasActiveSpellEffectId), args => (uint)args[0] == activeEffectId);
        return unit;
    }

    private static ServiceProvider Services(PrerequisiteEntry row)
    {
        IGameTableManager tables = RecordingDispatchProxy<IGameTableManager>.Create(out var proxy);
        proxy.SetProperty(nameof(IGameTableManager.Prerequisite), PlayerBindPointTests.Table(row));
        var services = new ServiceCollection().AddLogging().AddSingleton(tables);
        services.AddGamePrerequisite();
        return services.BuildServiceProvider();
    }
}
