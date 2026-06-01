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
    [InlineData(50, nameof(PrerequisiteType.UnderSpellOnTarget))]
    [InlineData(59, nameof(PrerequisiteType.SpellTier))]
    [InlineData(64, nameof(PrerequisiteType.PathTypeLevel))]
    [InlineData(71, nameof(PrerequisiteType.ClassProgress))]
    [InlineData(76, nameof(PrerequisiteType.CreatureState))]
    [InlineData(123, nameof(PrerequisiteType.PublicEventObjective123))]
    [InlineData(56, nameof(PrerequisiteType.DifficultyRankSlotAssigned))]
    [InlineData(60, nameof(PrerequisiteType.SpellTierOnTarget))]
    [InlineData(106, nameof(PrerequisiteType.TargetEntityLookupHit))]
    [InlineData(244, nameof(PrerequisiteType.OwnsAccountItem))]
    [InlineData(248, nameof(PrerequisiteType.NpcInventoryItemCount))]
    [InlineData(55, nameof(PrerequisiteType.Currency))]
    [InlineData(180, nameof(PrerequisiteType.CreatureDifficultyRank))]
    [InlineData(170, nameof(PrerequisiteType.GameFormula))]
    [InlineData(77, nameof(PrerequisiteType.QuestObjectiveOnTarget))]
    [InlineData(246, nameof(PrerequisiteType.DoesNotOwnAccountItemOnCharacter))]
    [InlineData(269, nameof(PrerequisiteType.RapidTransport))]
    [InlineData(277, nameof(PrerequisiteType.QuestObjective47OnCasterAndTarget))]
    [InlineData(279, nameof(PrerequisiteType.UnderSpellOnCasterAndTarget))]
    [InlineData(280, nameof(PrerequisiteType.SpellTierOnCasterAndTarget))]
    [InlineData(292, nameof(PrerequisiteType.PrimalMatrixNode))]
    [InlineData(129, nameof(PrerequisiteType.ActiveSpellEffectOnUnit))]
    [InlineData(130, nameof(PrerequisiteType.ActiveSpellEffectOnTarget))]
    [InlineData(82, nameof(PrerequisiteType.UnitEntityType))]
    [InlineData(89, nameof(PrerequisiteType.PetMatchTarget))]
    [InlineData(102, nameof(PrerequisiteType.SpellCooldownNodeOnUnit))]
    [InlineData(103, nameof(PrerequisiteType.SpellCooldownNodeOnTarget))]
    [InlineData(104, nameof(PrerequisiteType.SpellCooldownNodeInServiceSet))]
    [InlineData(105, nameof(PrerequisiteType.SpellEffectTypeOnUnit))]
    [InlineData(92, nameof(PrerequisiteType.ActiveSpellTargetMechanic))]
    [InlineData(93, nameof(PrerequisiteType.Spell4EffectCategoryOnUnit))]
    [InlineData(133, nameof(PrerequisiteType.ItemStatData))]
    [InlineData(134, nameof(PrerequisiteType.ItemStatId))]
    [InlineData(136, nameof(PrerequisiteType.Item2Id))]
    [InlineData(90, nameof(PrerequisiteType.MountVehicleType))]
    [InlineData(135, nameof(PrerequisiteType.AppliedItemStatId))]
    [InlineData(140, nameof(PrerequisiteType.ItemRolledPropertyValue))]
    [InlineData(191, nameof(PrerequisiteType.PetEntitySpell4))]
    [InlineData(275, nameof(PrerequisiteType.DoesNotOwnAccountItem))]
    [InlineData(172, nameof(PrerequisiteType.HealthScaled))]
    [InlineData(174, nameof(PrerequisiteType.ItemTradeSkillKnown))]
    public void EvidenceBackedRenames_KeepStableTableIds(int tableId, string expectedName)
    {
        Assert.Equal((PrerequisiteType)tableId, Enum.Parse<PrerequisiteType>(expectedName));
        Assert.Equal(expectedName, Enum.GetName((PrerequisiteType)tableId));
    }
}
