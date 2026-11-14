using Microsoft.Extensions.DependencyInjection;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Creature;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Tests.Prerequisite;

// Native rule: 14049eeb0 compares primary's cached/raw Creature2DifficultyId with ObjectId;
// no creature record compares zero, a null primary fails. See PREREQUISITE_179_FIELD_BINDING_20261114.md.
public sealed class CreatureDifficultyPrerequisiteTests
{
    [Theory]
    [InlineData(PrerequisiteComparison.Equal, 0u, true)]
    [InlineData(PrerequisiteComparison.NotEqual, 0u, false)]
    [InlineData(PrerequisiteComparison.GreaterThan, 0u, false)]
    [InlineData(PrerequisiteComparison.GreaterThanOrEqual, 0u, true)]
    [InlineData(PrerequisiteComparison.LessThan, 0u, false)]
    [InlineData(PrerequisiteComparison.LessThanOrEqual, 0u, true)]
    [InlineData(PrerequisiteComparison.Equal, 8u, false)]
    [InlineData(PrerequisiteComparison.NotEqual, 8u, true)]
    [InlineData(PrerequisiteComparison.GreaterThan, 8u, false)]
    [InlineData(PrerequisiteComparison.GreaterThanOrEqual, 8u, false)]
    [InlineData(PrerequisiteComparison.LessThan, 8u, true)]
    [InlineData(PrerequisiteComparison.LessThanOrEqual, 8u, true)]
    [InlineData((PrerequisiteComparison)0, 0u, false)]
    [InlineData((PrerequisiteComparison)0, 8u, false)]
    public void MissingCreatureRecordComparesZeroOnThePrimarySubject(PrerequisiteComparison comparison, uint required, bool expected)
    {
        PrerequisiteEntry row = Row(comparison, required);
        using var fixture = new Fixture(row);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        IUnitEntity target = Unit(8u);
        var parameters = new PrerequisiteParameters { Target = target };

        Assert.Equal(expected, fixture.Manager.MeetsUnit(player, row.Id, parameters));
        Assert.Equal(expected, fixture.Manager.Meets(player, row.Id, parameters));
        Assert.Equal(expected, Game.Spell.Spell.MeetsApplyPrerequisite(row, player, fixture.Manager, target));
        Assert.Same(target, parameters.Target);
    }

    [Theory]
    [InlineData(7u)]
    [InlineData(24u)]
    [InlineData(25u)] // Structural control: missing lookup/sentinel must not erase the raw record ID.
    public void RawDifficultyIdDoesNotRequireAResolvedDifficultyTableRow(uint id)
    {
        PrerequisiteEntry row = Row(PrerequisiteComparison.Equal, id);
        using var fixture = new Fixture(row); // Only ID 8 resolves in this fixture.
        IUnitEntity subject = Unit(id);

        Assert.True(fixture.Manager.MeetsUnit(subject, row.Id));
        Assert.True(Game.Spell.Spell.MeetsApplyPrerequisite(row, subject, fixture.Manager));
    }

    [Fact]
    public void CurrentCreatureInfoDifficultyIsComparedWithObjectIdRatherThanValue()
    {
        PrerequisiteEntry row = Row(PrerequisiteComparison.Equal, 8u);
        row.Value = [99u];
        using var fixture = new Fixture(row);
        ICreatureInfo info = RecordingDispatchProxy<ICreatureInfo>.Create(out var infoProxy);
        infoProxy.SetProperty(nameof(ICreatureInfo.DifficultyEntry), new Creature2DifficultyEntry { Id = 8u });
        IPlayer subject = RecordingDispatchProxy<IPlayer>.Create(out var proxy);
        proxy.SetProperty(nameof(IWorldEntity.CreatureInfo), info);
        var parameters = new PrerequisiteParameters { Target = Unit(7u) };

        Assert.True(fixture.Manager.Meets(subject, row.Id, parameters));
        Assert.True(fixture.Manager.MeetsUnit(subject, row.Id, parameters));
        Assert.True(Game.Spell.Spell.MeetsApplyPrerequisite(row, subject, fixture.Manager, parameters.Target));
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, 25u, true)]
    [InlineData(PrerequisiteComparison.Equal, 0u, false)]
    [InlineData(PrerequisiteComparison.NotEqual, 0u, true)]
    [InlineData(PrerequisiteComparison.GreaterThan, 0u, true)]
    [InlineData(PrerequisiteComparison.LessThan, 25u, false)]
    [InlineData(PrerequisiteComparison.LessThanOrEqual, 24u, false)]
    public void PresentCreatureRecordWithRawZeroDifficultyUsesNativeDefault25(PrerequisiteComparison comparison, uint required, bool expected)
    {
        // 140482720 initializes runtime record +0x14 to 25; 1404852b0 skips its store for raw ID 0.
        PrerequisiteEntry row = Row(comparison, required);
        using var fixture = new Fixture(row);
        IUnitEntity subject = Unit(0u);
        IWorldEntity worldObject = RecordingDispatchProxy<IWorldEntity>.Create(out var proxy);
        proxy.SetProperty(nameof(IWorldEntity.CreatureEntry), new Creature2Entry { Id = 123u, Creature2DifficultyId = 0u });

        Assert.Equal(expected, fixture.Manager.MeetsUnit(subject, row.Id));
        Assert.Equal(expected, Game.Spell.Spell.MeetsApplyPrerequisite(row, subject, fixture.Manager));
        Assert.Equal(expected, Game.Spell.Spell.MeetsApplyPrerequisite(row, worldObject));
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, 0u)]
    [InlineData(PrerequisiteComparison.NotEqual, 8u)]
    [InlineData(PrerequisiteComparison.GreaterThanOrEqual, 0u)]
    [InlineData(PrerequisiteComparison.LessThanOrEqual, 8u)]
    public void NullSubjectFailsEvenWithARelationshipTarget(PrerequisiteComparison comparison, uint required)
    {
        PrerequisiteEntry row = Row(comparison, required);
        using var fixture = new Fixture(row);
        IUnitEntity target = Unit(8u);

        Assert.False(fixture.Manager.MeetsUnit(null, row.Id, new PrerequisiteParameters { Target = target }));
        Assert.False(fixture.Check.Meets(null, comparison, 0u, required, new PrerequisiteParameters { Target = target }));
        Assert.False(Game.Spell.Spell.MeetsApplyPrerequisite(row, null, fixture.Manager, target));
    }

    [Theory]
    [InlineData(EvaluationMode.EvaluateAND, true)]
    [InlineData(EvaluationMode.EvaluateOR, false)]
    public void FirstEmptySlotEndsThePureDifficultyRow(EvaluationMode mode, bool expected)
    {
        PrerequisiteEntry row = Row(mode == EvaluationMode.EvaluateAND ? PrerequisiteComparison.Equal : PrerequisiteComparison.NotEqual, 8u);
        row.Flags = mode;
        row.PrerequisiteTypeId = [PrerequisiteType.CreatureDifficulty, PrerequisiteType.None, PrerequisiteType.CreatureDifficulty];
        row.PrerequisiteComparisonId = [row.PrerequisiteComparisonId[0], 0, expected ? PrerequisiteComparison.NotEqual : PrerequisiteComparison.Equal];
        row.ObjectId = [8u, 0u, 8u];
        row.Value = [0u, 0u, 0u];
        using var fixture = new Fixture(row);
        IUnitEntity subject = Unit(8u);

        Assert.Equal(expected, fixture.Manager.MeetsUnit(subject, row.Id));
        Assert.Equal(expected, Game.Spell.Spell.MeetsApplyPrerequisite(row, subject, fixture.Manager));
    }

    [Theory]
    [InlineData(EntityType.CollectableUnit)]
    [InlineData(EntityType.Simple)]
    [InlineData(EntityType.Platform)]
    [InlineData(EntityType.AiTurret)]
    [InlineData(EntityType.SimpleCollidable)]
    public void CreatureBackedWorldObjectsUseTheSameSlotRule(EntityType type)
    {
        IWorldEntity subject = RecordingDispatchProxy<IWorldEntity>.Create(out var proxy);
        proxy.SetProperty(nameof(IWorldEntity.Type), type);
        proxy.SetProperty(nameof(IWorldEntity.CreatureEntry), new Creature2Entry { Id = 123u, Creature2DifficultyId = 8u });

        Assert.True(Game.Spell.Spell.MeetsApplyPrerequisite(Row(PrerequisiteComparison.Equal, 8u), subject));
        Assert.False(Game.Spell.Spell.MeetsApplyPrerequisite(Row(PrerequisiteComparison.Equal, 7u), subject));
        proxy.SetProperty(nameof(IWorldEntity.CreatureEntry), null);
        Assert.False(Game.Spell.Spell.MeetsApplyPrerequisite(Row(PrerequisiteComparison.GreaterThan, 0u), subject));
        Assert.True(Game.Spell.Spell.MeetsApplyPrerequisite(Row(PrerequisiteComparison.Equal, 0u), subject));
    }

    [Fact]
    public void RetailRow30526ComparesZeroWhenThePrimaryHasNoCreatureRecord()
    {
        // Imported 16042 row 30526: type 179 <= ObjectId 6, Value 0; e.g. caster-apply on effect 185015 / Spell4 339.
        PrerequisiteEntry row = Row(PrerequisiteComparison.LessThanOrEqual, 6u);
        row.Id = 30526u;
        using var fixture = new Fixture(row);
        IPlayer subject = RecordingDispatchProxy<IPlayer>.Create(out _);

        Assert.True(fixture.Manager.MeetsUnit(subject, row.Id));
        Assert.True(fixture.Manager.Meets(subject, row.Id));
        Assert.True(Game.Spell.Spell.MeetsApplyPrerequisite(row, subject, fixture.Manager));
    }

    private static PrerequisiteEntry Row(PrerequisiteComparison comparison, uint required)
        => new()
        {
            Id = 1u,
            Flags = EvaluationMode.EvaluateAND,
            PrerequisiteTypeId = [PrerequisiteType.CreatureDifficulty],
            PrerequisiteComparisonId = [comparison],
            ObjectId = [required],
            Value = [0u]
        };

    private static IUnitEntity Unit(uint difficultyId)
    {
        IUnitEntity subject = RecordingDispatchProxy<IUnitEntity>.Create(out var proxy);
        proxy.SetProperty(nameof(IWorldEntity.CreatureEntry), new Creature2Entry { Id = 123u, Creature2DifficultyId = difficultyId });
        return subject;
    }

    private sealed class Fixture : IDisposable
    {
        private readonly ServiceProvider services;
        public IPrerequisiteManager Manager => services.GetRequiredService<IPrerequisiteManager>();
        public IPrerequisiteCheck Check => services.GetRequiredKeyedService<IPrerequisiteCheck>(PrerequisiteType.CreatureDifficulty);

        public Fixture(PrerequisiteEntry row)
        {
            IGameTableManager tables = RecordingDispatchProxy<IGameTableManager>.Create(out var proxy);
            proxy.SetProperty(nameof(IGameTableManager.Prerequisite), PlayerBindPointTests.Table(row));
            proxy.SetProperty(nameof(IGameTableManager.Creature2Difficulty), PlayerBindPointTests.Table(new Creature2DifficultyEntry { Id = 8u }));
            var collection = new ServiceCollection().AddLogging().AddSingleton(tables);
            collection.AddGamePrerequisite();
            services = collection.BuildServiceProvider();
        }

        public void Dispose() => services.Dispose();
    }
}
