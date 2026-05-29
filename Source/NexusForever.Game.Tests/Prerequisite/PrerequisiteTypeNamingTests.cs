using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Tests.Prerequisite;

public class PrerequisiteTypeNamingTests
{
    [Fact]
    public void ActionSetSpell_UsesPrerequisiteId221()
    {
        Assert.Equal(221, (int)PrerequisiteType.ActionSetSpell);
        Assert.Equal(nameof(PrerequisiteType.ActionSetSpell), Enum.GetName(PrerequisiteType.ActionSetSpell));
    }

    [Fact]
    public void RapidTransport_UsesPrerequisiteId269()
    {
        Assert.Equal(269, (int)PrerequisiteType.RapidTransport);
        Assert.Equal(nameof(PrerequisiteType.RapidTransport), Enum.GetName(PrerequisiteType.RapidTransport));
    }

    [Theory]
    [InlineData(47, nameof(PrerequisiteType.QuestObjective47))]
    [InlineData(50, nameof(PrerequisiteType.Spell50))]
    [InlineData(64, nameof(PrerequisiteType.PathTypeLevel))]
    [InlineData(71, nameof(PrerequisiteType.ClassProgress))]
    [InlineData(76, nameof(PrerequisiteType.CreatureState))]
    [InlineData(123, nameof(PrerequisiteType.PublicEventObjective123))]
    [InlineData(142, nameof(PrerequisiteType.Spell142))]
    public void EvidenceBackedRenames_KeepStableTableIds(int tableId, string expectedName)
    {
        Assert.Equal((PrerequisiteType)tableId, Enum.Parse<PrerequisiteType>(expectedName));
        Assert.Equal(expectedName, Enum.GetName((PrerequisiteType)tableId));
    }
}
