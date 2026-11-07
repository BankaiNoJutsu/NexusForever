using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.CompilerServices;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Game.Static.Quest;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Tests.Quest;

public class AssetManagerTargetGroupTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CacheCreatureTargetGroups_WithMissingTargetGroupTableUsesEmptyCache(bool includeEmptyTable)
    {
        GameTableManager gameTableManager = CreateGameTableManager(
            targetGroupTable: includeEmptyTable ? CreateGameTable<TargetGroupEntry>() : null);

        AssetManager assetManager = CreateAssetManager(gameTableManager);

        InvokeCache(assetManager, "CacheCreatureTargetGroups");

        Assert.Null(assetManager.GetTargetGroupsForCreatureId(73498u));
    }

    [Fact]
    public void CacheCreatureTargetGroups_WithTableBackedGroupsIndexesNestedCreatureGroups()
    {
        TargetGroupEntry creatureGroup = CreateTargetGroup(
            id: 11u,
            type: TargetGroupType.CreatureIdGroup,
            73498u,
            73605u);
        TargetGroupEntry nestedGroup = CreateTargetGroup(
            id: 12u,
            type: TargetGroupType.OtherTargetGroupCreatures,
            11u);

        GameTableManager gameTableManager = CreateGameTableManager(
            targetGroupTable: CreateGameTable(creatureGroup, nestedGroup));

        AssetManager assetManager = CreateAssetManager(gameTableManager);

        InvokeCache(assetManager, "CacheCreatureTargetGroups");

        Assert.Equal([11u, 12u], assetManager.GetTargetGroupsForCreatureId(73498u));
        Assert.Equal([11u, 12u], assetManager.GetTargetGroupsForCreatureId(73605u));
        Assert.Null(assetManager.GetTargetGroupsForCreatureId(1u));
    }

    [Fact]
    public void CacheCreatureTargetGroups_WithUltimateProtogamesElementalHolderIndexesNestedWaveGroups()
    {
        TargetGroupEntry firstWaveGroup = CreateTargetGroup(
            id: 12877u,
            type: TargetGroupType.CreatureIdGroup,
            62451u,
            62452u,
            62468u,
            62469u,
            62470u,
            62471u);
        TargetGroupEntry secondWaveGroup = CreateTargetGroup(
            id: 12878u,
            type: TargetGroupType.CreatureIdGroup,
            62472u,
            62473u,
            63319u,
            67340u,
            67342u,
            67341u,
            67343u);
        TargetGroupEntry holderGroup = CreateTargetGroup(
            id: 12876u,
            type: TargetGroupType.OtherTargetGroupCreatures,
            12877u,
            12878u);

        GameTableManager gameTableManager = CreateGameTableManager(
            targetGroupTable: CreateGameTable(holderGroup, firstWaveGroup, secondWaveGroup));

        AssetManager assetManager = CreateAssetManager(gameTableManager);

        InvokeCache(assetManager, "CacheCreatureTargetGroups");

        Assert.Equal([12876u, 12877u], assetManager.GetTargetGroupsForCreatureId(62451u));
        Assert.Equal([12876u, 12877u], assetManager.GetTargetGroupsForCreatureId(62471u));
        Assert.Equal([12876u, 12878u], assetManager.GetTargetGroupsForCreatureId(62472u));
        Assert.Equal([12876u, 12878u], assetManager.GetTargetGroupsForCreatureId(63319u));
        Assert.Equal([12876u, 12878u], assetManager.GetTargetGroupsForCreatureId(67343u));
    }

    [Fact]
    public void CacheCreatureTargetGroups_WithKelVorethCompositeGroupIndexesParentObjectiveGroup()
    {
        TargetGroupEntry mechanoSlavers = CreateTargetGroup(
            id: 3912u,
            type: TargetGroupType.CreatureIdGroup,
            32547u,
            32549u);
        TargetGroupEntry constructs = CreateTargetGroup(
            id: 4972u,
            type: TargetGroupType.CreatureIdGroup,
            37110u,
            37111u,
            37113u,
            37114u);
        TargetGroupEntry objectiveGroup = CreateTargetGroup(
            id: 4973u,
            type: TargetGroupType.OtherTargetGroupCreatures,
            3912u,
            4972u);

        GameTableManager gameTableManager = CreateGameTableManager(
            targetGroupTable: CreateGameTable(mechanoSlavers, constructs, objectiveGroup));

        AssetManager assetManager = CreateAssetManager(gameTableManager);

        InvokeCache(assetManager, "CacheCreatureTargetGroups");

        Assert.Equal([3912u, 4973u], assetManager.GetTargetGroupsForCreatureId(32547u));
        Assert.Equal([4972u, 4973u], assetManager.GetTargetGroupsForCreatureId(37114u));
    }

    [Fact]
    public void CacheCreatureTargetGroups_WithStormtalonCompositeGroupIndexesParentObjectiveGroup()
    {
        TargetGroupEntry normalPell = CreateTargetGroup(
            id: 11122u,
            type: TargetGroupType.CreatureIdGroup,
            24474u,
            33361u,
            17160u);
        TargetGroupEntry veteranPell = CreateTargetGroup(
            id: 11123u,
            type: TargetGroupType.CreatureIdGroup,
            33362u,
            33405u,
            36758u);
        TargetGroupEntry objectiveGroup = CreateTargetGroup(
            id: 11121u,
            type: TargetGroupType.OtherTargetGroupCreatures,
            11122u,
            11123u);

        GameTableManager gameTableManager = CreateGameTableManager(
            targetGroupTable: CreateGameTable(normalPell, veteranPell, objectiveGroup));

        AssetManager assetManager = CreateAssetManager(gameTableManager);

        InvokeCache(assetManager, "CacheCreatureTargetGroups");

        Assert.Equal([11121u, 11122u], assetManager.GetTargetGroupsForCreatureId(17160u));
        Assert.Equal([11121u, 11123u], assetManager.GetTargetGroupsForCreatureId(33405u));
    }

    [Fact]
    public void CacheCreatureTargetGroups_WithSkullcanoMarauderCompositeGroupIndexesParentObjectiveGroup()
    {
        TargetGroupEntry redmoonMarauders = CreateTargetGroup(
            id: 2611u,
            type: TargetGroupType.CreatureIdGroup,
            37751u,
            24578u,
            24579u,
            24580u,
            24581u,
            24618u);
        TargetGroupEntry redmoonMaraudersVeteran = CreateTargetGroup(
            id: 3946u,
            type: TargetGroupType.CreatureIdGroup,
            37752u,
            24913u,
            24914u,
            24916u,
            24919u,
            24920u);
        TargetGroupEntry redmoonPrisonersNormal = CreateTargetGroup(
            id: 7756u,
            type: TargetGroupType.CreatureIdGroup,
            24578u,
            24512u,
            24510u,
            24514u,
            25561u);
        TargetGroupEntry redmoonPrisonersVeteran = CreateTargetGroup(
            id: 7757u,
            type: TargetGroupType.CreatureIdGroup,
            24913u,
            24910u,
            24908u,
            24911u,
            25562u);
        TargetGroupEntry bosunOctog = CreateTargetGroup(
            id: 2601u,
            type: TargetGroupType.CreatureIdGroup,
            24486u,
            24894u);
        TargetGroupEntry quartermasterGruhar = CreateTargetGroup(
            id: 7759u,
            type: TargetGroupType.CreatureIdGroup,
            24490u,
            24896u);
        TargetGroupEntry ridSkullcanoOfMarauders = CreateTargetGroup(
            id: 3947u,
            type: TargetGroupType.OtherTargetGroupCreatures,
            2611u,
            3946u,
            7756u,
            7757u,
            2601u,
            7759u);

        GameTableManager gameTableManager = CreateGameTableManager(
            targetGroupTable: CreateGameTable(
                redmoonMarauders,
                redmoonMaraudersVeteran,
                redmoonPrisonersNormal,
                redmoonPrisonersVeteran,
                bosunOctog,
                quartermasterGruhar,
                ridSkullcanoOfMarauders));

        AssetManager assetManager = CreateAssetManager(gameTableManager);

        InvokeCache(assetManager, "CacheCreatureTargetGroups");

        Assert.Equal([2611u, 3947u], assetManager.GetTargetGroupsForCreatureId(37751u));
        Assert.Equal([2611u, 3947u, 7756u], assetManager.GetTargetGroupsForCreatureId(24578u));
        Assert.Equal([3946u, 3947u, 7757u], assetManager.GetTargetGroupsForCreatureId(24913u));
        Assert.Equal([2601u, 3947u], assetManager.GetTargetGroupsForCreatureId(24486u));
        Assert.Equal([3947u, 7759u], assetManager.GetTargetGroupsForCreatureId(24896u));
        Assert.Null(assetManager.GetTargetGroupsForCreatureId(1u));
    }

    [Fact]
    public void CacheCreatureTargetGroups_WithMixedNestedCriteriaDoesNotIndexParentGroup()
    {
        TargetGroupEntry creatureGroup = CreateTargetGroup(
            id: 21u,
            type: TargetGroupType.CreatureIdGroup,
            73498u,
            73605u);
        TargetGroupEntry factionGroup = CreateTargetGroup(
            id: 22u,
            type: TargetGroupType.FactionIdGroup,
            1660u);
        TargetGroupEntry mixedGroup = CreateTargetGroup(
            id: 23u,
            type: TargetGroupType.OtherTargetGroup,
            21u,
            22u);

        GameTableManager gameTableManager = CreateGameTableManager(
            targetGroupTable: CreateGameTable(creatureGroup, factionGroup, mixedGroup));

        AssetManager assetManager = CreateAssetManager(gameTableManager);

        InvokeCache(assetManager, "CacheCreatureTargetGroups");

        Assert.Equal([21u], assetManager.GetTargetGroupsForCreatureId(73498u));
        Assert.Equal([21u], assetManager.GetTargetGroupsForCreatureId(73605u));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void CacheQuestObjectiveTargetGroups_WithUnavailableTablesUsesEmptyTargets(
        bool includeQuestObjectiveTable,
        bool includeTargetGroupTable)
    {
        GameTableManager gameTableManager = CreateGameTableManager(
            questObjectiveTable: includeQuestObjectiveTable
                ? CreateGameTable(CreateQuestObjective(1u, QuestObjectiveType.KillTargetGroup, targetGroupId: 44u))
                : null,
            targetGroupTable: includeTargetGroupTable ? CreateGameTable<TargetGroupEntry>() : null);

        AssetManager assetManager = CreateAssetManager(gameTableManager);

        InvokeCache(assetManager, "CacheQuestObjectiveTargetGroups");

        Assert.Empty(assetManager.GetQuestObjectiveTargetIds(1u));
    }

    [Fact]
    public void CacheQuestObjectiveTargetGroups_WithTableBackedNestedGroupsExpandsCreatureTargets()
    {
        QuestObjectiveEntry questObjective = CreateQuestObjective(
            id: 501u,
            type: QuestObjectiveType.KillTargetGroup,
            targetGroupId: 90u);
        TargetGroupEntry parentGroup = CreateTargetGroup(
            id: 90u,
            type: TargetGroupType.OtherTargetGroupCreatures,
            91u,
            92u);
        TargetGroupEntry firstCreatureGroup = CreateTargetGroup(
            id: 91u,
            type: TargetGroupType.CreatureIdGroup,
            73498u,
            73605u);
        TargetGroupEntry secondCreatureGroup = CreateTargetGroup(
            id: 92u,
            type: TargetGroupType.CreatureIdGroup,
            73605u,
            73681u);

        GameTableManager gameTableManager = CreateGameTableManager(
            questObjectiveTable: CreateGameTable(questObjective),
            targetGroupTable: CreateGameTable(parentGroup, firstCreatureGroup, secondCreatureGroup));

        AssetManager assetManager = CreateAssetManager(gameTableManager);

        InvokeCache(assetManager, "CacheQuestObjectiveTargetGroups");

        Assert.Equal([73498u, 73605u, 73681u], assetManager.GetQuestObjectiveTargetIds(501u));
    }

    [Fact]
    public void CacheQuestObjectiveTargetGroups_WithMissingNestedGroupSkipsMissingChild()
    {
        QuestObjectiveEntry questObjective = CreateQuestObjective(
            id: 502u,
            type: QuestObjectiveType.KillTargetGroup,
            targetGroupId: 90u);
        TargetGroupEntry parentGroup = CreateTargetGroup(
            id: 90u,
            type: TargetGroupType.OtherTargetGroupCreatures,
            91u,
            999999u);
        TargetGroupEntry creatureGroup = CreateTargetGroup(
            id: 91u,
            type: TargetGroupType.CreatureIdGroup,
            73498u,
            73605u);

        GameTableManager gameTableManager = CreateGameTableManager(
            questObjectiveTable: CreateGameTable(questObjective),
            targetGroupTable: CreateGameTable(parentGroup, creatureGroup));

        AssetManager assetManager = CreateAssetManager(gameTableManager);

        InvokeCache(assetManager, "CacheQuestObjectiveTargetGroups");

        Assert.Equal([73498u, 73605u], assetManager.GetQuestObjectiveTargetIds(502u));
    }

    [Fact]
    public void CacheQuestObjectiveTargetGroups_Q3797NestedCreatureListGroupsExpandsCreatureTargets()
    {
        QuestObjectiveEntry questObjective = CreateQuestObjective(
            id: 4918u,
            type: QuestObjectiveType.KillTargetGroups,
            targetGroupId: 1177u);
        TargetGroupEntry parentGroup = CreateTargetGroup(
            id: 1177u,
            type: TargetGroupType.OtherTargetGroupCreatures,
            1113u,
            6406u);
        TargetGroupEntry creatureListGroup = CreateTargetGroup(
            id: 1113u,
            type: TargetGroupType.CreatureIdListGroup,
            11963u,
            11962u);
        TargetGroupEntry creatureGroup = CreateTargetGroup(
            id: 6406u,
            type: TargetGroupType.CreatureIdGroup,
            11945u,
            11948u,
            12212u,
            12213u);

        GameTableManager gameTableManager = CreateGameTableManager(
            questObjectiveTable: CreateGameTable(questObjective),
            targetGroupTable: CreateGameTable(parentGroup, creatureListGroup, creatureGroup));

        AssetManager assetManager = CreateAssetManager(gameTableManager);

        InvokeCache(assetManager, "CacheQuestObjectiveTargetGroups");

        Assert.Equal([11945u, 11948u, 11962u, 11963u, 12212u, 12213u], assetManager.GetQuestObjectiveTargetIds(4918u));
    }

    [Fact]
    public void CacheQuestObjectiveTargetGroups_Q3479NestedCreatureGroupsExpandsAllYetiTargets()
    {
        QuestObjectiveEntry questObjective = CreateQuestObjective(
            id: 4564u,
            type: QuestObjectiveType.KillTargetGroup,
            targetGroupId: 7288u);
        TargetGroupEntry parentGroup = CreateTargetGroup(
            id: 7288u,
            type: TargetGroupType.OtherTargetGroupCreatures,
            7287u,
            1463u);
        TargetGroupEntry holdoutYetiGroup = CreateTargetGroup(
            id: 7287u,
            type: TargetGroupType.CreatureIdGroup,
            36331u,
            36335u,
            51126u);
        TargetGroupEntry northernWildsYetiGroup = CreateTargetGroup(
            id: 1463u,
            type: TargetGroupType.CreatureIdGroup,
            12844u,
            11945u,
            11948u,
            13116u,
            13117u,
            13959u,
            51126u);

        GameTableManager gameTableManager = CreateGameTableManager(
            questObjectiveTable: CreateGameTable(questObjective),
            targetGroupTable: CreateGameTable(parentGroup, holdoutYetiGroup, northernWildsYetiGroup));

        AssetManager assetManager = CreateAssetManager(gameTableManager);

        InvokeCache(assetManager, "CacheQuestObjectiveTargetGroups");

        Assert.Equal(
            [11945u, 11948u, 12844u, 13116u, 13117u, 13959u, 36331u, 36335u, 51126u],
            assetManager.GetQuestObjectiveTargetIds(4564u));
    }

    [Fact]
    public void CacheQuestObjectiveTargetGroups_Q3668NestedCreatureGroupsExpandsBlockedMembers()
    {
        QuestObjectiveEntry questObjective = CreateQuestObjective(
            id: 4791u,
            type: QuestObjectiveType.KillTargetGroup,
            targetGroupId: 7293u);
        TargetGroupEntry parentGroup = CreateTargetGroup(
            id: 7293u,
            type: TargetGroupType.OtherTargetGroupCreatures,
            7292u,
            960u);
        TargetGroupEntry firstCreatureGroup = CreateTargetGroup(
            id: 7292u,
            type: TargetGroupType.CreatureIdGroup,
            14054u,
            36429u,
            17545u);
        TargetGroupEntry secondCreatureGroup = CreateTargetGroup(
            id: 960u,
            type: TargetGroupType.CreatureIdGroup,
            11907u,
            11910u,
            11912u,
            11913u,
            11917u,
            36884u);

        GameTableManager gameTableManager = CreateGameTableManager(
            questObjectiveTable: CreateGameTable(questObjective),
            targetGroupTable: CreateGameTable(parentGroup, firstCreatureGroup, secondCreatureGroup));

        AssetManager assetManager = CreateAssetManager(gameTableManager);

        InvokeCache(assetManager, "CacheQuestObjectiveTargetGroups");

        Assert.Equal(
            [11907u, 11910u, 11912u, 11913u, 11917u, 14054u, 17545u, 36429u, 36884u],
            assetManager.GetQuestObjectiveTargetIds(4791u));
    }

    [Fact]
    public void CacheQuestObjectiveTargetGroups_Q5583HeavyArmorExpandsTankAndMegatechTargets()
    {
        QuestObjectiveEntry tankObjective = CreateQuestObjective(
            id: 8231u,
            type: QuestObjectiveType.KillTargetGroups,
            targetGroupId: 3737u);
        QuestObjectiveEntry megatechObjective = CreateQuestObjective(
            id: 8372u,
            type: QuestObjectiveType.KillTargetGroups,
            targetGroupId: 3812u);
        TargetGroupEntry tankGroup = CreateTargetGroup(
            id: 3737u,
            type: TargetGroupType.CreatureIdGroup,
            24255u,
            24452u);
        TargetGroupEntry megatechParentGroup = CreateTargetGroup(
            id: 3812u,
            type: TargetGroupType.OtherTargetGroupCreatures,
            2536u,
            3811u,
            12540u);
        TargetGroupEntry megatechTrooperGroup = CreateTargetGroup(
            id: 2536u,
            type: TargetGroupType.CreatureIdGroup,
            24029u,
            24078u,
            24030u,
            24046u,
            24077u,
            24156u,
            24099u);
        TargetGroupEntry megatechWarbotGroup = CreateTargetGroup(
            id: 3811u,
            type: TargetGroupType.CreatureIdGroup,
            26590u,
            26591u,
            31792u,
            31864u,
            31895u,
            26585u,
            38223u);
        TargetGroupEntry megatechBotGroup = CreateTargetGroup(
            id: 12540u,
            type: TargetGroupType.CreatureIdGroup,
            38226u,
            38228u,
            38230u,
            24481u,
            25292u,
            37968u,
            37971u);

        GameTableManager gameTableManager = CreateGameTableManager(
            questObjectiveTable: CreateGameTable(tankObjective, megatechObjective),
            targetGroupTable: CreateGameTable(tankGroup, megatechParentGroup, megatechTrooperGroup, megatechWarbotGroup, megatechBotGroup));

        AssetManager assetManager = CreateAssetManager(gameTableManager);

        InvokeCache(assetManager, "CacheQuestObjectiveTargetGroups");

        Assert.Equal([24255u, 24452u], assetManager.GetQuestObjectiveTargetIds(8231u));
        Assert.Equal(
            [24029u, 24030u, 24046u, 24077u, 24078u, 24099u, 24156u, 24481u, 25292u, 26585u, 26590u, 26591u, 31792u, 31864u, 31895u, 37968u, 37971u, 38223u, 38226u, 38228u, 38230u],
            assetManager.GetQuestObjectiveTargetIds(8372u));
    }

    [Fact]
    public void CacheQuestObjectiveTargetGroups_ActivateEntityWithRewardPaneTargetGroupExpandsCreatureTargets()
    {
        QuestObjectiveEntry questObjective = CreateQuestObjective(
            id: 6508u,
            type: QuestObjectiveType.ActivateEntity,
            targetGroupId: 0u,
            targetGroupIdRewardPane: 4323u);
        TargetGroupEntry falkrinGroup = CreateTargetGroup(
            id: 4323u,
            type: TargetGroupType.CreatureIdGroup,
            17189u,
            19595u);

        GameTableManager gameTableManager = CreateGameTableManager(
            questObjectiveTable: CreateGameTable(questObjective),
            targetGroupTable: CreateGameTable(falkrinGroup));

        AssetManager assetManager = CreateAssetManager(gameTableManager);

        InvokeCache(assetManager, "CacheQuestObjectiveTargetGroups");

        Assert.Equal([17189u, 19595u], assetManager.GetQuestObjectiveTargetIds(6508u));
    }

    // Native evidence (Decomp/Analysis/TARGETGROUP_QUEST_OBJECTIVE_MEMBERSHIP_20261107.md): the client target-group evaluator FUN_1403b4a20 defines membership by
    // unit attributes. Type 10 is an AND of its sub-groups, 11 an OR, 3/4 faction include/exclude, 12/13 creature unit race include/exclude. Retail KillTargetGroups
    // objectives such as 19319 use type 10 groups of (unit race list AND NOT faction list), which a creature id flattening cannot express: the quest objective's
    // targets are therefore every Creature2 row that satisfies the group.
    [Fact]
    public void CacheQuestObjectiveTargetGroups_WithUnitRaceAndFactionExclusionGroupResolvesMatchingCreatures()
    {
        QuestObjectiveEntry objective = CreateQuestObjective(19319u, QuestObjectiveType.KillTargetGroups, targetGroupId: 14113u);
        TargetGroupEntry races = CreateTargetGroup(14111u, TargetGroupType.CreatureRaceIdGroup, 184u, 278u);
        TargetGroupEntry notFactions = CreateTargetGroup(14112u, TargetGroupType.NotFactionIdGroup, 219u, 693u, 197u);
        TargetGroupEntry all = CreateTargetGroup(14113u, TargetGroupType.OtherTargetGroup, 14111u, 14112u);

        GameTableManager gameTableManager = CreateGameTableManager(
            questObjectiveTable: CreateGameTable(objective),
            targetGroupTable: CreateGameTable(races, notFactions, all),
            creature2Table: CreateGameTable(
                Creature(1001u, unitRaceId: 184u, factionId: 100u),
                Creature(1002u, unitRaceId: 278u, factionId: 100u),
                Creature(1003u, unitRaceId: 184u, factionId: 219u),   // excluded faction
                Creature(1004u, unitRaceId: 5u, factionId: 100u),     // other unit race
                Creature(1005u, unitRaceId: 278u, factionId: 693u))); // excluded faction

        AssetManager assetManager = CreateAssetManager(gameTableManager);

        InvokeCache(assetManager, "CacheQuestObjectiveTargetGroups");

        Assert.Equal([1001u, 1002u], assetManager.GetQuestObjectiveTargetIds(19319u));
    }

    [Fact]
    public void CacheQuestObjectiveTargetGroups_WithFactionIncludeGroupResolvesCreaturesOfThatFaction()
    {
        QuestObjectiveEntry objective = CreateQuestObjective(4112u, QuestObjectiveType.KillTargetGroups, targetGroupId: 847u);
        TargetGroupEntry factions = CreateTargetGroup(847u, TargetGroupType.FactionIdGroup, 166u, 167u);

        GameTableManager gameTableManager = CreateGameTableManager(
            questObjectiveTable: CreateGameTable(objective),
            targetGroupTable: CreateGameTable(factions),
            creature2Table: CreateGameTable(
                Creature(2001u, unitRaceId: 0u, factionId: 166u),
                Creature(2002u, unitRaceId: 0u, factionId: 167u),
                Creature(2003u, unitRaceId: 0u, factionId: 219u)));

        AssetManager assetManager = CreateAssetManager(gameTableManager);

        InvokeCache(assetManager, "CacheQuestObjectiveTargetGroups");

        Assert.Equal([2001u, 2002u], assetManager.GetQuestObjectiveTargetIds(4112u));
    }

    [Fact]
    public void CacheQuestObjectiveTargetGroups_WithCreatureListAndFactionGroupKeepsOnlyListedCreaturesOfTheFaction()
    {
        // Retail spell AoE group 436: creature 5865 AND faction 521. The same AND shape applies to objective groups.
        QuestObjectiveEntry objective = CreateQuestObjective(601u, QuestObjectiveType.KillTargetGroups, targetGroupId: 436u);
        TargetGroupEntry creatures = CreateTargetGroup(428u, TargetGroupType.CreatureIdGroup, 5865u, 5866u);
        TargetGroupEntry faction = CreateTargetGroup(435u, TargetGroupType.FactionIdGroup, 521u);
        TargetGroupEntry all = CreateTargetGroup(436u, TargetGroupType.OtherTargetGroup, 428u, 435u);

        GameTableManager gameTableManager = CreateGameTableManager(
            questObjectiveTable: CreateGameTable(objective),
            targetGroupTable: CreateGameTable(creatures, faction, all),
            creature2Table: CreateGameTable(
                Creature(5865u, unitRaceId: 0u, factionId: 521u),
                Creature(5866u, unitRaceId: 0u, factionId: 100u),     // listed creature, wrong faction
                Creature(5867u, unitRaceId: 0u, factionId: 521u)));   // right faction, not listed

        AssetManager assetManager = CreateAssetManager(gameTableManager);

        InvokeCache(assetManager, "CacheQuestObjectiveTargetGroups");

        Assert.Equal([5865u], assetManager.GetQuestObjectiveTargetIds(601u));
    }

    [Fact]
    public void CacheQuestObjectiveTargetGroups_WithTwoCreatureListsInAnAllOfGroupKeepsTheOverlap()
    {
        QuestObjectiveEntry objective = CreateQuestObjective(602u, QuestObjectiveType.KillTargetGroups, targetGroupId: 33u);
        TargetGroupEntry first = CreateTargetGroup(31u, TargetGroupType.CreatureIdGroup, 1u, 2u, 3u);
        TargetGroupEntry second = CreateTargetGroup(32u, TargetGroupType.CreatureIdGroup, 3u, 4u);
        TargetGroupEntry all = CreateTargetGroup(33u, TargetGroupType.OtherTargetGroup, 31u, 32u);

        GameTableManager gameTableManager = CreateGameTableManager(
            questObjectiveTable: CreateGameTable(objective),
            targetGroupTable: CreateGameTable(first, second, all));

        AssetManager assetManager = CreateAssetManager(gameTableManager);

        InvokeCache(assetManager, "CacheQuestObjectiveTargetGroups");

        Assert.Equal([3u], assetManager.GetQuestObjectiveTargetIds(602u));
    }

    [Fact]
    public void CacheQuestObjectiveTargetGroups_WithoutCreatureDataAttributeGroupsHaveNoTargets()
    {
        QuestObjectiveEntry objective = CreateQuestObjective(603u, QuestObjectiveType.KillTargetGroups, targetGroupId: 847u);

        GameTableManager gameTableManager = CreateGameTableManager(
            questObjectiveTable: CreateGameTable(objective),
            targetGroupTable: CreateGameTable(CreateTargetGroup(847u, TargetGroupType.FactionIdGroup, 166u)));

        AssetManager assetManager = CreateAssetManager(gameTableManager);

        InvokeCache(assetManager, "CacheQuestObjectiveTargetGroups");

        Assert.Empty(assetManager.GetQuestObjectiveTargetIds(603u));
    }

    [Fact]
    public void CacheCreatureTargetGroups_IndexesAttributeGroupsReferencedByQuestObjectives()
    {
        QuestObjectiveEntry objective = CreateQuestObjective(604u, QuestObjectiveType.ActivateTargetGroup, targetGroupId: 847u);
        TargetGroupEntry referenced = CreateTargetGroup(847u, TargetGroupType.FactionIdGroup, 166u);
        TargetGroupEntry unreferenced = CreateTargetGroup(848u, TargetGroupType.FactionIdGroup, 166u);

        GameTableManager gameTableManager = CreateGameTableManager(
            questObjectiveTable: CreateGameTable(objective),
            targetGroupTable: CreateGameTable(referenced, unreferenced),
            creature2Table: CreateGameTable(
                Creature(2001u, unitRaceId: 0u, factionId: 166u),
                Creature(2003u, unitRaceId: 0u, factionId: 219u)));

        AssetManager assetManager = CreateAssetManager(gameTableManager);

        InvokeCache(assetManager, "CacheCreatureTargetGroups");

        Assert.Equal([847u], assetManager.GetTargetGroupsForCreatureId(2001u));
        Assert.Null(assetManager.GetTargetGroupsForCreatureId(2003u));
    }

    // Public event KillTargetGroup objectives (type 0) name their group in ObjectId and credit kills through the creature-to-group index. Five retail objectives
    // (445, 846, 1665, 2281, 4686) use a unit race or composite group, which is only indexed when something refers to it.
    [Fact]
    public void CacheCreatureTargetGroups_IndexesAttributeGroupsReferencedByPublicEventObjectives()
    {
        TargetGroupEntry races = CreateTargetGroup(3972u, TargetGroupType.CreatureRaceIdGroup, 184u);
        TargetGroupEntry unreferenced = CreateTargetGroup(3973u, TargetGroupType.CreatureRaceIdGroup, 184u);

        var publicEventObjective = new PublicEventObjectiveEntry
        {
            Id                          = 445u,
            PublicEventObjectiveTypeEnum = PublicEventObjectiveType.KillTargetGroup,
            ObjectId                    = 3972u
        };

        GameTableManager gameTableManager = CreateGameTableManager(
            targetGroupTable: CreateGameTable(races, unreferenced),
            creature2Table: CreateGameTable(
                Creature(2001u, unitRaceId: 184u, factionId: 0u),
                Creature(2002u, unitRaceId: 5u, factionId: 0u)));
        SetAutoProperty(gameTableManager, nameof(GameTableManager.PublicEventObjective), CreateGameTable(publicEventObjective));

        AssetManager assetManager = CreateAssetManager(gameTableManager);

        InvokeCache(assetManager, "CacheCreatureTargetGroups");

        Assert.Equal([3972u], assetManager.GetTargetGroupsForCreatureId(2001u));
        Assert.Null(assetManager.GetTargetGroupsForCreatureId(2002u));
    }

    private static Creature2Entry Creature(uint id, uint unitRaceId, uint factionId)
        => new() { Id = id, UnitRaceId = unitRaceId, FactionId = factionId };

    private static GameTableManager CreateGameTableManager(
        GameTable<QuestObjectiveEntry> questObjectiveTable = null,
        GameTable<TargetGroupEntry> targetGroupTable = null,
        GameTable<Creature2Entry> creature2Table = null)
    {
        var gameTableManager = (GameTableManager)RuntimeHelpers.GetUninitializedObject(typeof(GameTableManager));
        if (questObjectiveTable != null)
            SetAutoProperty(gameTableManager, nameof(GameTableManager.QuestObjective), questObjectiveTable);
        if (targetGroupTable != null)
            SetAutoProperty(gameTableManager, nameof(GameTableManager.TargetGroup), targetGroupTable);
        if (creature2Table != null)
            SetAutoProperty(gameTableManager, nameof(GameTableManager.Creature2), creature2Table);

        return gameTableManager;
    }

    private static AssetManager CreateAssetManager(GameTableManager gameTableManager)
    {
        return new AssetManager(null, gameTableManager);
    }

    private static QuestObjectiveEntry CreateQuestObjective(
        uint id,
        QuestObjectiveType type,
        uint targetGroupId,
        uint targetGroupIdRewardPane = 0u)
    {
        return new QuestObjectiveEntry
        {
            Id                      = id,
            Type                    = (uint)type,
            Data                    = targetGroupId,
            TargetGroupIdRewardPane = targetGroupIdRewardPane
        };
    }

    private static TargetGroupEntry CreateTargetGroup(uint id, TargetGroupType type, params uint[] dataEntries)
    {
        uint[] entries = new uint[7];
        for (int i = 0; i < dataEntries.Length && i < entries.Length; i++)
            entries[i] = dataEntries[i];

        return new TargetGroupEntry
        {
            Id          = id,
            Type        = (uint)type,
            DataEntries = entries
        };
    }

    private static GameTable<T> CreateGameTable<T>(params T[] entries) where T : class, new()
    {
        var table = (GameTable<T>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<T>));
        SetAutoProperty(table, nameof(GameTable<T>.Entries), entries);
        SetPrivateField(table, "header", new GameTableHeader
        {
            MaxId = entries.Length == 0 ? 0u : entries.Max(GetEntryId) + 1u
        });
        SetPrivateField(table, "lookup", BuildLookup(entries));
        return table;
    }

    private static int[] BuildLookup<T>(IReadOnlyList<T> entries)
    {
        if (entries.Count == 0)
            return [];

        int[] lookup = Enumerable.Repeat(-1, (int)(entries.Max(GetEntryId) + 1u)).ToArray();
        for (int i = 0; i < entries.Count; i++)
            lookup[GetEntryId(entries[i])] = i;

        return lookup;
    }

    private static uint GetEntryId<T>(T entry)
    {
        return (uint)typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public)[0].GetValue(entry);
    }

    private static void InvokeCache(AssetManager assetManager, string methodName)
    {
        typeof(AssetManager)
            .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(assetManager, null);
    }

    private static void SetAutoProperty(object instance, string propertyName, object value)
    {
        FieldInfo backingField = instance.GetType()
            .GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        backingField.SetValue(instance, value);
    }

    private static void SetPrivateField(object instance, string fieldName, object value)
    {
        FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        field.SetValue(instance, value);
    }
}
