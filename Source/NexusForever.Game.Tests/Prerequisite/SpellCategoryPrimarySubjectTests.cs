using Microsoft.Extensions.DependencyInjection;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Entity;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Prerequisite.Check;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Static.Spell;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Tests.Prerequisite;

// Verified native case0x5d: primary list, ObjectId class match, Value unread.
// Source tracked immunity records below are fixtures, not a retail list-lifetime proof.
public sealed class SpellCategoryPrimarySubjectTests
{
    [Theory]
    [InlineData(38u, PrerequisiteComparison.Equal, 0u)]
    [InlineData(36u, PrerequisiteComparison.Equal, 0u)]
    [InlineData(38u, PrerequisiteComparison.NotEqual, 0u)]
    [InlineData(36u, PrerequisiteComparison.NotEqual, 0u)]
    [InlineData(38u, PrerequisiteComparison.Equal, uint.MaxValue)]
    [InlineData(36u, PrerequisiteComparison.Equal, uint.MaxValue)]
    [InlineData(38u, PrerequisiteComparison.NotEqual, uint.MaxValue)]
    [InlineData(36u, PrerequisiteComparison.NotEqual, uint.MaxValue)]
    public void PlayerAndUnitManagerEntryPointsRetainThePrimary(uint primaryClass, PrerequisiteComparison comparison, uint value)
    {
        IGameTableManager tables = Tables(primaryClass, primaryClass == 38u ? 36u : 38u, comparison, value);
        var collection = new ServiceCollection().AddLogging().AddSingleton(tables);
        collection.AddGamePrerequisite();
        using ServiceProvider services = collection.BuildServiceProvider();
        IPrerequisiteManager manager = services.GetRequiredService<IPrerequisiteManager>();
        Player primary = PlayerWithSpell(100u, tables);
        Player target = PlayerWithSpell(200u, tables);
        var parameters = new PrerequisiteParameters { Target = target };
        bool matches = primaryClass == 38u;
        bool expected = comparison == PrerequisiteComparison.Equal ? matches : !matches;

        Assert.Equal(expected, manager.MeetsUnit(primary, 1u, parameters));
        Assert.Equal(expected, manager.Meets(primary, 1u, parameters));
        Assert.Equal(!expected, manager.MeetsUnit(target, 1u, new PrerequisiteParameters { Target = primary }));
        Assert.Same(target, parameters.Target);
    }

    [Theory]
    [InlineData(38u, PrerequisiteComparison.Equal)]
    [InlineData(36u, PrerequisiteComparison.Equal)]
    [InlineData(38u, PrerequisiteComparison.NotEqual)]
    [InlineData(36u, PrerequisiteComparison.NotEqual)]
    public void NullPrimaryIsNotReplacedByTheRelationship(uint targetClass, PrerequisiteComparison comparison)
    {
        IGameTableManager tables = Tables(targetClass);
        var check = new PrerequisiteCheckSpell4EffectCategoryOnUnit(tables);

        Assert.False(check.Meets(null, comparison, 0u, 38u, new PrerequisiteParameters { Target = PlayerWithSpell(100u, tables) }));
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal)]
    [InlineData(PrerequisiteComparison.NotEqual)]
    public void NullUnitFailsBeforeTheBooleanComparison(PrerequisiteComparison comparison)
    {
        Assert.False(new PrerequisiteCheckSpell4EffectCategoryOnUnit(Tables(38u)).MeetsUnit(null, comparison, 0u, 38u, null));
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, true)]
    [InlineData(PrerequisiteComparison.NotEqual, false)]
    public void RelationshipParametersAreNotRequired(PrerequisiteComparison comparison, bool expected)
    {
        IGameTableManager tables = Tables(38u);
        Player primary = PlayerWithSpell(100u, tables);
        var check = new PrerequisiteCheckSpell4EffectCategoryOnUnit(tables);

        Assert.Equal(expected, check.MeetsUnit(primary, comparison, 1234u, 38u, null));
        Assert.Equal(expected, check.Meets(primary, comparison, 1234u, 38u, null));
    }

    [Theory]
    [InlineData((PrerequisiteComparison)0)]
    [InlineData(PrerequisiteComparison.GreaterThan)]
    [InlineData(PrerequisiteComparison.GreaterThanOrEqual)]
    [InlineData(PrerequisiteComparison.LessThan)]
    [InlineData(PrerequisiteComparison.LessThanOrEqual)]
    [InlineData((PrerequisiteComparison)7)]
    public void UnsupportedModesFail(PrerequisiteComparison comparison)
    {
        IGameTableManager tables = Tables(38u);
        Assert.False(new PrerequisiteCheckSpell4EffectCategoryOnUnit(tables).MeetsUnit(PlayerWithSpell(100u, tables), comparison, 0u, 38u, null));
    }

    [Theory]
    [InlineData(0u, 0u, true)]
    [InlineData(567u, 567u, true)]
    [InlineData(uint.MaxValue, uint.MaxValue, true)]
    [InlineData(38u, 36u, false)]
    public void AnyTrackedEntryCanMatchTheFullObjectId(uint spellClass, uint objectId, bool expected)
    {
        IGameTableManager tables = Tables(123u, spellClass);
        Player primary = PlayerWithSpell(100u, tables);
        primary.AddSpellEffectImmunity(2u, 200u, 20u, SpellEffectType.Damage);
        var check = new PrerequisiteCheckSpell4EffectCategoryOnUnit(tables);

        Assert.Equal(expected, check.MeetsUnit(primary, PrerequisiteComparison.Equal, 123u, objectId, null));
        Assert.Equal(!expected, check.MeetsUnit(primary, PrerequisiteComparison.NotEqual, 123u, objectId, null));
    }

    private static Player PlayerWithSpell(uint spell4Id, IGameTableManager tables)
    {
        var player = new Player(RecordingDispatchProxy<IMovementManager>.Create(out _), null, null, null, null, tables, null);
        player.AddSpellEffectImmunity(1u, spell4Id, 10u, SpellEffectType.Damage);
        return player;
    }

    private static IGameTableManager Tables(uint primaryClass, uint targetClass = 36u, PrerequisiteComparison comparison = PrerequisiteComparison.Equal, uint value = 0u)
    {
        IGameTableManager tables = RecordingDispatchProxy<IGameTableManager>.Create(out var proxy);
        proxy.SetProperty(nameof(IGameTableManager.Spell4), PlayerBindPointTests.Table(
            new Spell4Entry { Id = 100u, Spell4BaseIdBaseSpell = 7u },
            new Spell4Entry { Id = 200u, Spell4BaseIdBaseSpell = 8u }));
        proxy.SetProperty(nameof(IGameTableManager.Spell4Base), PlayerBindPointTests.Table(
            new Spell4BaseEntry { Id = 7u, SpellClass = primaryClass },
            new Spell4BaseEntry { Id = 8u, SpellClass = targetClass }));
        proxy.SetProperty(nameof(IGameTableManager.Prerequisite), PlayerBindPointTests.Table(new PrerequisiteEntry
        {
            Id = 1u,
            Flags = EvaluationMode.EvaluateAND,
            PrerequisiteTypeId = [PrerequisiteType.Spell4EffectCategoryOnUnit],
            PrerequisiteComparisonId = [comparison],
            Value = [value],
            ObjectId = [38u]
        }));
        return tables;
    }
}
