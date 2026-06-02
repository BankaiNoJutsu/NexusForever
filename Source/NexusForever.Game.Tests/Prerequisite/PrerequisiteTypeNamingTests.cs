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
    [InlineData(96, nameof(PrerequisiteType.EvalContextFloatByObjectId))]
    [InlineData(97, nameof(PrerequisiteType.AccountItemListItem2CountNpc97))]
    [InlineData(98, nameof(PrerequisiteType.AccountItemListItem2CountNpc98))]
    [InlineData(99, nameof(PrerequisiteType.AccountItemListItem2CountNpc99))]
    [InlineData(100, nameof(PrerequisiteType.AccountItemListItem2Count100))]
    [InlineData(101, nameof(PrerequisiteType.AccountItemListItem2Count101))]
    [InlineData(233, nameof(PrerequisiteType.SpellTierUnlocked))]
    [InlineData(241, nameof(PrerequisiteType.HousingResidenceLoaded))]
    [InlineData(135, nameof(PrerequisiteType.AppliedItemStatId))]
    [InlineData(140, nameof(PrerequisiteType.ItemRolledPropertyValue))]
    [InlineData(191, nameof(PrerequisiteType.PetEntitySpell4))]
    [InlineData(172, nameof(PrerequisiteType.HealthScaled))]
    [InlineData(173, nameof(PrerequisiteType.ItemTradeSkill))]
    [InlineData(174, nameof(PrerequisiteType.ItemTradeSkillKnown))]
    [InlineData(175, nameof(PrerequisiteType.TradeSkill))]
    [InlineData(176, nameof(PrerequisiteType.ChallengeObject))]
    [InlineData(177, nameof(PrerequisiteType.TrueLevel))]
    [InlineData(168, nameof(PrerequisiteType.AccountItemCount))]
    [InlineData(169, nameof(PrerequisiteType.AccountItemCountCompared))]
    [InlineData(166, nameof(PrerequisiteType.LiveEventTreeLookup))]
    [InlineData(167, nameof(PrerequisiteType.LiveEventWorldFactionBranch))]
    [InlineData(171, nameof(PrerequisiteType.DailyLoginDaysTotal))]
    [InlineData(287, nameof(PrerequisiteType.DailyLoginRewardsAvailable))]
    [InlineData(178, nameof(PrerequisiteType.ProgressTrackOnMatchingEntity))]
    [InlineData(293, nameof(PrerequisiteType.AccountCurrencyAmount))]
    [InlineData(266, nameof(PrerequisiteType.PetOrEsperPetEntity))]
    [InlineData(159, nameof(PrerequisiteType.PositionalRequirementBetweenCasterAndTarget))]
    [InlineData(40, nameof(PrerequisiteType.Health))]
    [InlineData(41, nameof(PrerequisiteType.ZoneExplored))]
    [InlineData(12, nameof(PrerequisiteType.DeadState))]
    [InlineData(28, nameof(PrerequisiteType.InCombat))]
    [InlineData(38, nameof(PrerequisiteType.IsCreature))]
    [InlineData(39, nameof(PrerequisiteType.IsPlayer))]
    [InlineData(42, nameof(PrerequisiteType.IsGroupLeader))]
    [InlineData(220, nameof(PrerequisiteType.PathMissionChecklistItemComplete))]
    [InlineData(221, nameof(PrerequisiteType.ActionSetSpell))]
    [InlineData(4, nameof(PrerequisiteType.Faction))]
    [InlineData(5, nameof(PrerequisiteType.Reputation))]
    [InlineData(13, nameof(PrerequisiteType.ItemEquipped))]
    [InlineData(14, nameof(PrerequisiteType.ItemOnCharacter))]
    [InlineData(51, nameof(PrerequisiteType.ItemQuantity))]
    [InlineData(182, nameof(PrerequisiteType.HouseOwnership))]
    [InlineData(186, nameof(PrerequisiteType.HousingNeighborResidence))]
    [InlineData(183, nameof(PrerequisiteType.Guild))]
    [InlineData(184, nameof(PrerequisiteType.Guild2))]
    [InlineData(185, nameof(PrerequisiteType.GuildPerk))]
    [InlineData(37, nameof(PrerequisiteType.RandomPercent))]
    [InlineData(43, nameof(PrerequisiteType.IsObjectiveActive))]
    [InlineData(68, nameof(PrerequisiteType.QuestObjective))]
    [InlineData(128, nameof(PrerequisiteType.Faction128))]
    [InlineData(188, nameof(PrerequisiteType.WarplotPermission))]
    [InlineData(63, nameof(PrerequisiteType.IsLocalPlayerEntity))]
    [InlineData(267, nameof(PrerequisiteType.GroupIsRaid))]
    [InlineData(268, nameof(PrerequisiteType.CREDDPendingOrderState))]
    [InlineData(295, nameof(PrerequisiteType.EvaluatedEntityPresent))]
    public void EvidenceBackedRenames_KeepStableTableIds(int tableId, string expectedName)
    {
        Assert.Equal((PrerequisiteType)tableId, Enum.Parse<PrerequisiteType>(expectedName));
        Assert.Equal(expectedName, Enum.GetName((PrerequisiteType)tableId));
    }

    [Theory]
    [InlineData(223, nameof(PrerequisiteType.Unknown223))]
    [InlineData(240, nameof(PrerequisiteType.Unknown240))]
    [InlineData(245, nameof(PrerequisiteType.Unknown245))]
    [InlineData(275, nameof(PrerequisiteType.Unknown275))]
    [InlineData(286, nameof(PrerequisiteType.Unknown286))]
    [InlineData(289, nameof(PrerequisiteType.Unknown289))]
    [InlineData(290, nameof(PrerequisiteType.Unknown290))]
    [InlineData(291, nameof(PrerequisiteType.Unknown291))]
    public void DuplicateBodyAliases_RemainUnknownUntilSemanticOwnerIsProven(int tableId, string expectedName)
    {
        Assert.Equal((PrerequisiteType)tableId, Enum.Parse<PrerequisiteType>(expectedName));
        Assert.Equal(expectedName, Enum.GetName((PrerequisiteType)tableId));
    }

    [Theory]
    [InlineData(189, nameof(PrerequisiteType.Unknown189))]
    [InlineData(192, nameof(PrerequisiteType.Unknown192))]
    [InlineData(193, nameof(PrerequisiteType.Unknown193))]
    public void OrphanHelperCandidates_RemainUnknownUntilLiveDispatchIsProven(int tableId, string expectedName)
    {
        Assert.Equal((PrerequisiteType)tableId, Enum.Parse<PrerequisiteType>(expectedName));
        Assert.Equal(expectedName, Enum.GetName((PrerequisiteType)tableId));
    }

    [Theory]
    [InlineData(144, nameof(PrerequisiteType.Unknown144))]
    public void SkippedLiveDispatcherCandidates_RemainUnknownUntilReachabilityIsProven(int tableId, string expectedName)
    {
        Assert.Equal((PrerequisiteType)tableId, Enum.Parse<PrerequisiteType>(expectedName));
        Assert.Equal(expectedName, Enum.GetName((PrerequisiteType)tableId));
    }

    [Theory]
    [InlineData(48, nameof(PrerequisiteType.Unknown48))]
    [InlineData(142, nameof(PrerequisiteType.Unknown142))]
    [InlineData(203, nameof(PrerequisiteType.Unknown203))]
    [InlineData(222, nameof(PrerequisiteType.Unknown222))]
    [InlineData(259, nameof(PrerequisiteType.Unknown259))]
    [InlineData(271, nameof(PrerequisiteType.Unknown271))]
    [InlineData(272, nameof(PrerequisiteType.Unknown272))]
    [InlineData(278, nameof(PrerequisiteType.Unknown278))]
    [InlineData(283, nameof(PrerequisiteType.Unknown283))]
    [InlineData(285, nameof(PrerequisiteType.Unknown285))]
    [InlineData(294, nameof(PrerequisiteType.Unknown294))]
    public void NoOpSlotCandidates_RemainUnknownUntilNonStubHandlerIsProven(int tableId, string expectedName)
    {
        Assert.Equal((PrerequisiteType)tableId, Enum.Parse<PrerequisiteType>(expectedName));
        Assert.Equal(expectedName, Enum.GetName((PrerequisiteType)tableId));
    }

    [Theory]
    [InlineData(260, nameof(PrerequisiteType.Unknown260))]
    public void DiagnosticFieldOwnerCandidates_RemainUnknownUntilSemanticOwnerIsProven(int tableId, string expectedName)
    {
        Assert.Equal((PrerequisiteType)tableId, Enum.Parse<PrerequisiteType>(expectedName));
        Assert.Equal(expectedName, Enum.GetName((PrerequisiteType)tableId));
    }
}
