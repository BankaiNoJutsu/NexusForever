using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using NexusForever.Database.World.Model;
using NexusForever.Game.Abstract.Combat;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Combat;
using NexusForever.Game.Static.Combat.CrowdControl;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Entity.Movement.Command.Mode;
using NexusForever.Game.Static.Entity.Movement.Spline;
using NexusForever.Game.Static.Reputation;
using NexusForever.Game.Static.Spell;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Story;
using NexusForever.Network.World.Message.Static;
using NexusForever.Script.Main.AI;
using NexusForever.Shared;

namespace NexusForever.Game.Tests.Combat;

public class CombatAITests
{
    private const Faction TutorialDominionCombatFaction = (Faction)1441u;
    private const Faction TutorialExileCombatFaction = (Faction)1442u;

    [Fact]
    public void Constructor_WhenProfileProviderNotRegistered_UsesDefaultProvider()
    {
        IServiceProvider services = new ServiceCollection()
            .AddSingleton(CreateSpellParametersFactory())
            .AddSingleton(CreateGameTableManager())
            .BuildServiceProvider();

        CombatAI script = ActivatorUtilities.CreateInstance<CombatAI>(services);

        Assert.NotNull(script);
    }

    [Theory]
    [InlineData(73464u, false)]
    [InlineData(73494u, true)]
    public void DefaultCombatProfileProvider_WhenCreatureHasReviewedProfile_ReturnsDataBackedProfile(uint creature2Id, bool stationary)
    {
        ICreatureEntity creature = RecordingDispatchProxy<ICreatureEntity>.Create(out RecordingDispatchProxy<ICreatureEntity> creatureProxy);
        creatureProxy.SetProperty(nameof(ICreatureEntity.CreatureId), creature2Id);

        CombatProfile profile = DefaultCombatProfileProvider.Instance.GetProfile(creature);

        Assert.Equal(14f, profile.AggroRange);
        Assert.Equal(50f, profile.MinimumLeashRange);
        Assert.Equal(12f, profile.AssistRange);
        Assert.Equal(stationary, profile.Stationary);
        Assert.True(profile.TraceCombat);
        Assert.Equal(new uint[] { 5649u, 5652u }, profile.AutoAttackSpell4Ids);
        Assert.Equal(41368u, profile.AggroSpell4Id);
        Assert.Equal(5f, profile.ChaseDistance);
    }

    [Fact]
    public void DefaultCombatProfileProvider_WhenStarterCreatureUsesCatalog_ReturnsReviewedKitResolution()
    {
        ICreatureEntity creature = RecordingDispatchProxy<ICreatureEntity>.Create(out RecordingDispatchProxy<ICreatureEntity> creatureProxy);
        creatureProxy.SetProperty(nameof(ICreatureEntity.CreatureId), 73464u);

        CombatProfileResolution resolution = DefaultCombatProfileProvider.Instance.GetResolution(creature);

        Assert.Equal(CombatProfileResolutionSource.ReviewedKitMapping, resolution.Source);
        Assert.Equal("starter-tutorial-combat", resolution.KitId);
        Assert.Equal(41368u, resolution.Profile.AggroSpell4Id);
    }

    [Fact]
    public void DefaultCombatProfileProvider_WhenStarterTurretUsesManualOverride_ReturnsManualResolution()
    {
        ICreatureEntity creature = RecordingDispatchProxy<ICreatureEntity>.Create(out RecordingDispatchProxy<ICreatureEntity> creatureProxy);
        creatureProxy.SetProperty(nameof(ICreatureEntity.CreatureId), 73494u);

        CombatProfileResolution resolution = DefaultCombatProfileProvider.Instance.GetResolution(creature);

        Assert.Equal(CombatProfileResolutionSource.ManualOverride, resolution.Source);
        Assert.Equal("starter-tutorial-turret", resolution.ProfileId);
        Assert.True(resolution.Profile.Stationary);
    }

    [Fact]
    public void DefaultCombatProfileProvider_WhenArtillerybotUsesManualOverride_ReturnsSummonerAssistPolicy()
    {
        ICreatureEntity creature = RecordingDispatchProxy<ICreatureEntity>.Create(out RecordingDispatchProxy<ICreatureEntity> creatureProxy);
        creatureProxy.SetProperty(nameof(ICreatureEntity.CreatureId), 42683u);

        CombatProfileResolution resolution = DefaultCombatProfileProvider.Instance.GetResolution(creature);

        Assert.Equal(CombatProfileResolutionSource.ManualOverride, resolution.Source);
        Assert.Equal("engineer-artillerybot", resolution.ProfileId);
        Assert.Equal(new uint[] { 34521u }, resolution.Profile.AutoAttackSpell4Ids);
        Assert.True(resolution.Profile.AllowNonPlayerTargets);
        Assert.True(resolution.Profile.AssistSummoner);
        Assert.Equal(35f, resolution.Profile.SummonerAssistRange);
        Assert.Equal(4f, resolution.Profile.SummonerFollowDistance);
        Assert.Equal(8f, resolution.Profile.SummonerFollowRepathDistance);
        Assert.Equal(27002u, resolution.Profile.SummonerTierSourceBaseSpell4Id);
        Assert.Equal(20491u, resolution.Profile.SummonerTieredAutoAttackBaseSpell4Id);
    }

    [Fact]
    public void OnLoad_WhenStarterTutorialExileLaneCreatureLoads_NormalizesToDominionCombatFaction()
    {
        CombatHarness harness = CreateHarness(
            new Vector3(100f, 0f, 0f),
            creatureId: 73464u,
            includePlayer: false,
            armRangeCheck: false);

        Assert.Equal(TutorialDominionCombatFaction, harness.Creature.Faction1);
        Assert.Equal(TutorialDominionCombatFaction, harness.Creature.Faction2);
        RecordingDispatchProxy<ICreatureEntity>.Invocation faction1 = Assert.Single(
            harness.CreatureProxy.GetInvocations($"set_{nameof(ICreatureEntity.Faction1)}"));
        RecordingDispatchProxy<ICreatureEntity>.Invocation faction2 = Assert.Single(
            harness.CreatureProxy.GetInvocations($"set_{nameof(ICreatureEntity.Faction2)}"));
        Assert.Equal(TutorialDominionCombatFaction, faction1.Arguments[0]);
        Assert.Equal(TutorialDominionCombatFaction, faction2.Arguments[0]);
    }

    [Fact]
    public void OnLoad_WhenStarterTutorialDominionLaneCreatureLoads_NormalizesToExileCombatFaction()
    {
        CombatHarness harness = CreateHarness(
            new Vector3(100f, 0f, 0f),
            creatureId: 73465u,
            includePlayer: false,
            armRangeCheck: false);

        Assert.Equal(TutorialExileCombatFaction, harness.Creature.Faction1);
        Assert.Equal(TutorialExileCombatFaction, harness.Creature.Faction2);
        RecordingDispatchProxy<ICreatureEntity>.Invocation faction1 = Assert.Single(
            harness.CreatureProxy.GetInvocations($"set_{nameof(ICreatureEntity.Faction1)}"));
        RecordingDispatchProxy<ICreatureEntity>.Invocation faction2 = Assert.Single(
            harness.CreatureProxy.GetInvocations($"set_{nameof(ICreatureEntity.Faction2)}"));
        Assert.Equal(TutorialExileCombatFaction, faction1.Arguments[0]);
        Assert.Equal(TutorialExileCombatFaction, faction2.Arguments[0]);
    }

    [Fact]
    public void OnLoad_WhenOpenWorldProfiledCreatureLoads_DoesNotNormalizeFaction()
    {
        CombatHarness harness = CreateHarness(
            new Vector3(100f, 0f, 0f),
            creatureId: 11910u,
            includePlayer: false,
            armRangeCheck: false);

        Assert.Equal(Faction.Dominion, harness.Creature.Faction1);
        Assert.Equal(Faction.None, harness.Creature.Faction2);
        Assert.Empty(harness.CreatureProxy.GetInvocations($"set_{nameof(ICreatureEntity.Faction1)}"));
        Assert.Empty(harness.CreatureProxy.GetInvocations($"set_{nameof(ICreatureEntity.Faction2)}"));
    }

    [Fact]
    public void DefaultCombatProfileProvider_Audit_ReturnsManualAndCatalogMappingCounts()
    {
        CombatProfileAudit audit = DefaultCombatProfileProvider.Instance.GetAudit();

        Assert.Equal(4, audit.ManualOverrideCreatureCount);
        Assert.Equal(185, audit.ReviewedKitMappingCreatureCount);
        Assert.Equal(0, audit.ActionDerivedProfileCount);
        Assert.Equal(0, audit.ActionIgnoredRuleRowCount);
        Assert.Equal(0, audit.ActionRejectedRuleRowCount);
        Assert.Equal(0, audit.ActionUnknownRowCount);
        Assert.Equal(0, audit.ActionMissingSpellRowCount);
        Assert.Equal(189, audit.MappedCreatureCount);
        Assert.Equal(0, audit.UnmappedCreatureCount);
    }

    [Fact]
    public void DefaultCombatProfileProvider_WhenCreatureHasNoEmbeddedProfile_ReturnsNull()
    {
        ICreatureEntity creature = RecordingDispatchProxy<ICreatureEntity>.Create(out RecordingDispatchProxy<ICreatureEntity> creatureProxy);
        creatureProxy.SetProperty(nameof(ICreatureEntity.CreatureId), 200u);

        CombatProfile profile = DefaultCombatProfileProvider.Instance.GetProfile(creature);

        Assert.Null(profile);
    }

    [Fact]
    public void DefaultCombatProfileProvider_WhenNorthernWildsCreatureHasEmbeddedProfile_ReturnsDataBackedSpellKit()
    {
        ICreatureEntity creature = RecordingDispatchProxy<ICreatureEntity>.Create(out RecordingDispatchProxy<ICreatureEntity> creatureProxy);
        creatureProxy.SetProperty(nameof(ICreatureEntity.CreatureId), 11910u);

        CombatProfile profile = DefaultCombatProfileProvider.Instance.GetProfile(creature);

        Assert.Equal(new uint[] { 5649u, 5652u }, profile.AutoAttackSpell4Ids);
        Assert.Equal(
            new[]
            {
                new CombatSpecialAttack(55843u, 7d),
                new CombatSpecialAttack(55844u, 12d)
            }, profile.SpecialAttacks);
        Assert.Equal(14f, profile.AggroRange);
        Assert.Equal(35f, profile.MinimumLeashRange);
        Assert.Equal(0u, profile.AggroSpell4Id);
        Assert.False(profile.AllowNonPlayerTargets);
    }

    [Fact]
    public void DefaultCombatProfileProvider_WhenDominionUltrabotLoads_UsesQuestAreaLeash()
    {
        ICreatureEntity creature = RecordingDispatchProxy<ICreatureEntity>.Create(out RecordingDispatchProxy<ICreatureEntity> creatureProxy);
        creatureProxy.SetProperty(nameof(ICreatureEntity.CreatureId), 12526u);

        CombatProfileResolution resolution = DefaultCombatProfileProvider.Instance.GetResolution(creature);

        Assert.Equal(CombatProfileResolutionSource.ReviewedKitMapping, resolution.Source);
        Assert.Equal("northern-wilds-ultrabot", resolution.KitId);
        Assert.Equal(80f, resolution.Profile.MinimumLeashRange);
        Assert.False(resolution.Profile.Stationary);
    }

    [Fact]
    public void DefaultCombatProfileProvider_WhenOpenWorldCreatureHasReviewedSpellBridge_ReturnsDataBackedSpellKit()
    {
        ICreatureEntity creature = RecordingDispatchProxy<ICreatureEntity>.Create(out RecordingDispatchProxy<ICreatureEntity> creatureProxy);
        creatureProxy.SetProperty(nameof(ICreatureEntity.CreatureId), 24051u);

        CombatProfileResolution resolution = DefaultCombatProfileProvider.Instance.GetResolution(creature);

        Assert.Equal(CombatProfileResolutionSource.ReviewedKitMapping, resolution.Source);
        Assert.Equal("open-world-venombite-hatchling", resolution.KitId);
        Assert.Equal(new uint[] { 6651u, 6652u }, resolution.Profile.AutoAttackSpell4Ids);
        Assert.Equal(
            new[]
            {
                new CombatSpecialAttack(55327u, 8d)
            }, resolution.Profile.SpecialAttacks);
        Assert.Equal(14f, resolution.Profile.AggroRange);
        Assert.Equal(35f, resolution.Profile.MinimumLeashRange);
    }

    [Fact]
    public void DefaultCombatProfileProvider_WhenCreatureActionRowsAreUnproven_DoesNotResolveProfileAndReportsAuditRows()
    {
        IGameTableManager gameTableManager = CreateCombatProfileGameTableManager(
            creatureEntries:
            [
                new Creature2Entry
                {
                    Id = 90001u,
                    Creature2ActionSetId = 77u
                }
            ],
            actionEntries:
            [
                new Creature2ActionEntry
                {
                    Id                  = 1u,
                    CreatureActionSetId = 77u,
                    VisualEffectId      = 123u
                },
                new Creature2ActionEntry
                {
                    Id                  = 2u,
                    CreatureActionSetId = 77u,
                    Action              = 999u,
                    ActionData00        = 555u
                }
            ]);
        DefaultCombatProfileProvider provider = DefaultCombatProfileProvider.ForGameTables(gameTableManager);
        ICreatureEntity creature = RecordingDispatchProxy<ICreatureEntity>.Create(out RecordingDispatchProxy<ICreatureEntity> creatureProxy);
        creatureProxy.SetProperty(nameof(ICreatureEntity.CreatureId), 90001u);

        CombatProfileResolution resolution = provider.GetResolution(creature);
        CombatProfileAudit audit = provider.GetAudit();
        CombatProfileAuditDetails details = provider.GetAuditDetails();

        Assert.Equal(CombatProfileResolutionSource.None, resolution.Source);
        Assert.Null(resolution.Profile);
        Assert.Equal(0, audit.ActionDerivedProfileCount);
        Assert.Equal(1, audit.ActionVisualOnlyRowCount);
        Assert.Equal(0, audit.ActionIgnoredRuleRowCount);
        Assert.Equal(0, audit.ActionRejectedRuleRowCount);
        Assert.Equal(1, audit.ActionUnknownRowCount);
        Assert.Equal(0, audit.ActionMissingSpellRowCount);
        Assert.Equal(0, audit.MappedCreatureCount);
        Assert.Equal(1, audit.UnmappedCreatureCount);
        CombatProfileCreatureAuditRow creatureRow = Assert.Single(details.CreatureRows, row => row.Creature2Id == 90001u);
        Assert.Equal(CombatProfileResolutionSource.None, creatureRow.Source);
        Assert.Equal(77u, creatureRow.Creature2ActionSetId);
        Assert.Collection(
            details.ActionRows.OrderBy(row => row.Creature2ActionId),
            row => Assert.Equal(CombatActionAuditStatus.VisualOnly, row.Status),
            row => Assert.Equal(CombatActionAuditStatus.Unknown, row.Status));
    }

    [Fact]
    public void DefaultCombatProfileProvider_WhenCreatureActionRuleIsProvenExact_ResolvesActionProfile()
    {
        const uint creature2Id = 90002u;
        const uint actionSetId = 88u;
        const uint spell4Id = 321u;
        IGameTableManager gameTableManager = CreateCombatProfileGameTableManager(
            creatureEntries:
            [
                new Creature2Entry
                {
                    Id                   = creature2Id,
                    Creature2ActionSetId = actionSetId
                }
            ],
            actionEntries:
            [
                new Creature2ActionEntry
                {
                    Id                  = 1u,
                    CreatureActionSetId = actionSetId,
                    State               = 1u,
                    Event               = 2u,
                    Action              = 3u,
                    ActionData00        = spell4Id
                }
            ],
            spell4Entries:
            [
                new Spell4Entry
                {
                    Id             = spell4Id,
                    TargetMaxRange = 17f
                }
            ]);
        DefaultCombatProfileProvider provider = DefaultCombatProfileProvider.ForGameTables(
            gameTableManager,
            [
                new CombatActionRule
                {
                    Kind = CombatActionRuleKind.Ignore
                },
                new CombatActionRule
                {
                    State           = 1u,
                    Event           = 2u,
                    Action          = 3u,
                    Kind            = CombatActionRuleKind.SpecialAttack,
                    Spell4IdField   = "actionData00",
                    CooldownSeconds = 6d,
                    MaxRange        = 17f,
                    FaceTarget      = false,
                    Weight          = 2d,
                    Source          = "unit-test",
                    Evidence        = "exact Creature2Action rule"
                }
            ]);
        ICreatureEntity creature = RecordingDispatchProxy<ICreatureEntity>.Create(out RecordingDispatchProxy<ICreatureEntity> creatureProxy);
        creatureProxy.SetProperty(nameof(ICreatureEntity.CreatureId), creature2Id);

        CombatProfileResolution resolution = provider.GetResolution(creature);
        CombatProfileAudit audit = provider.GetAudit();
        CombatProfileAuditDetails details = provider.GetAuditDetails();

        Assert.Equal(CombatProfileResolutionSource.ProvenActionResolver, resolution.Source);
        Assert.Equal(actionSetId, resolution.Creature2ActionSetId);
        CombatSpecialAttack specialAttack = Assert.Single(resolution.Profile.SpecialAttacks);
        Assert.Equal(spell4Id, specialAttack.Spell4Id);
        Assert.Equal(6d, specialAttack.CooldownSeconds);
        Assert.Equal(17f, specialAttack.MaxRange);
        Assert.False(specialAttack.FaceTarget);
        Assert.Equal(2d, specialAttack.Weight);
        Assert.Equal(1, audit.ActionDerivedProfileCount);
        Assert.Equal(0, audit.ActionIgnoredRuleRowCount);
        Assert.Equal(0, audit.ActionRejectedRuleRowCount);
        Assert.Equal(0, audit.ActionUnknownRowCount);
        Assert.Equal(0, audit.ActionMissingSpellRowCount);
        Assert.Equal(1, audit.MappedCreatureCount);
        Assert.Equal(0, audit.UnmappedCreatureCount);
        CombatProfileCreatureAuditRow creatureRow = Assert.Single(details.CreatureRows, row => row.Creature2Id == creature2Id);
        Assert.Equal(CombatProfileResolutionSource.ProvenActionResolver, creatureRow.Source);
        Assert.Equal(1, creatureRow.SpecialAttackCount);
        CombatProfileActionAuditRow actionRow = Assert.Single(details.ActionRows);
        Assert.Equal(CombatActionAuditStatus.ActivatedSpecialAttack, actionRow.Status);
        Assert.Equal("unit-test", actionRow.RuleSource);
        Assert.Equal(spell4Id, actionRow.Spell4Id);
    }

    [Fact]
    public void DefaultCombatProfileProvider_WhenCreatureActionRuleIsActiveWildcard_DoesNotResolveProfileAndReportsRejectedRule()
    {
        const uint creature2Id = 90003u;
        IGameTableManager gameTableManager = CreateCombatProfileGameTableManager(
            creatureEntries:
            [
                new Creature2Entry
                {
                    Id                   = creature2Id,
                    Creature2ActionSetId = 89u
                }
            ],
            actionEntries:
            [
                new Creature2ActionEntry
                {
                    Id                  = 1u,
                    CreatureActionSetId = 89u,
                    State               = 1u,
                    Event               = 2u,
                    Action              = 3u,
                    ActionData00        = 321u
                }
            ],
            spell4Entries:
            [
                new Spell4Entry
                {
                    Id = 321u
                }
            ]);
        DefaultCombatProfileProvider provider = DefaultCombatProfileProvider.ForGameTables(
            gameTableManager,
            [
                new CombatActionRule
                {
                    Event           = 2u,
                    Action          = 3u,
                    Kind            = CombatActionRuleKind.AutoAttack,
                    Spell4IdField   = "actionData00",
                    Source          = "unit-test",
                    Evidence        = "wildcard active rule"
                }
            ]);
        ICreatureEntity creature = RecordingDispatchProxy<ICreatureEntity>.Create(out RecordingDispatchProxy<ICreatureEntity> creatureProxy);
        creatureProxy.SetProperty(nameof(ICreatureEntity.CreatureId), creature2Id);

        CombatProfileResolution resolution = provider.GetResolution(creature);
        CombatProfileAudit audit = provider.GetAudit();
        CombatProfileAuditDetails details = provider.GetAuditDetails();

        Assert.Equal(CombatProfileResolutionSource.None, resolution.Source);
        Assert.Null(resolution.Profile);
        Assert.Equal(0, audit.ActionDerivedProfileCount);
        Assert.Equal(0, audit.ActionIgnoredRuleRowCount);
        Assert.Equal(1, audit.ActionRejectedRuleRowCount);
        Assert.Equal(0, audit.ActionUnknownRowCount);
        Assert.Equal(0, audit.ActionMissingSpellRowCount);
        Assert.Equal(0, audit.MappedCreatureCount);
        Assert.Equal(1, audit.UnmappedCreatureCount);
        CombatProfileActionAuditRow actionRow = Assert.Single(details.ActionRows);
        Assert.Equal(CombatActionAuditStatus.RejectedRule, actionRow.Status);
        Assert.Contains("exact State, Event, and Action", actionRow.Diagnostic);
    }

    [Fact]
    public void DefaultCombatProfileProvider_WhenCreatureActionRuleLacksActivationEvidence_DoesNotResolveProfileAndReportsRejectedRule()
    {
        const uint creature2Id = 90004u;
        IGameTableManager gameTableManager = CreateCombatProfileGameTableManager(
            creatureEntries:
            [
                new Creature2Entry
                {
                    Id                   = creature2Id,
                    Creature2ActionSetId = 90u
                }
            ],
            actionEntries:
            [
                new Creature2ActionEntry
                {
                    Id                  = 1u,
                    CreatureActionSetId = 90u,
                    State               = 1u,
                    Event               = 2u,
                    Action              = 3u,
                    ActionData00        = 321u
                },
                new Creature2ActionEntry
                {
                    Id                  = 2u,
                    CreatureActionSetId = 90u,
                    State               = 4u,
                    Event               = 5u,
                    Action              = 6u,
                    ActionData01        = 322u
                }
            ],
            spell4Entries:
            [
                new Spell4Entry
                {
                    Id = 321u
                },
                new Spell4Entry
                {
                    Id = 322u
                }
            ]);
        DefaultCombatProfileProvider provider = DefaultCombatProfileProvider.ForGameTables(
            gameTableManager,
            [
                new CombatActionRule
                {
                    State         = 1u,
                    Event         = 2u,
                    Action        = 3u,
                    Kind          = CombatActionRuleKind.AutoAttack,
                    Spell4IdField = "actionData00"
                },
                new CombatActionRule
                {
                    State           = 4u,
                    Event           = 5u,
                    Action          = 6u,
                    Kind            = CombatActionRuleKind.SpecialAttack,
                    Spell4IdField   = "actionData01",
                    CooldownSeconds = 0d,
                    Source          = "unit-test",
                    Evidence        = "no positive cooldown"
                }
            ]);
        ICreatureEntity creature = RecordingDispatchProxy<ICreatureEntity>.Create(out RecordingDispatchProxy<ICreatureEntity> creatureProxy);
        creatureProxy.SetProperty(nameof(ICreatureEntity.CreatureId), creature2Id);

        CombatProfileResolution resolution = provider.GetResolution(creature);
        CombatProfileAudit audit = provider.GetAudit();
        CombatProfileAuditDetails details = provider.GetAuditDetails();

        Assert.Equal(CombatProfileResolutionSource.None, resolution.Source);
        Assert.Null(resolution.Profile);
        Assert.Equal(0, audit.ActionDerivedProfileCount);
        Assert.Equal(0, audit.ActionIgnoredRuleRowCount);
        Assert.Equal(2, audit.ActionRejectedRuleRowCount);
        Assert.Equal(0, audit.ActionUnknownRowCount);
        Assert.Equal(0, audit.ActionMissingSpellRowCount);
        Assert.Equal(0, audit.MappedCreatureCount);
        Assert.Equal(1, audit.UnmappedCreatureCount);
        Assert.All(details.ActionRows, row => Assert.Equal(CombatActionAuditStatus.RejectedRule, row.Status));
        Assert.Contains(details.ActionRows, row => row.Diagnostic.Contains("source and evidence"));
        Assert.Contains(details.ActionRows, row => row.Diagnostic.Contains("positive cooldown"));
    }

    [Fact]
    public void DefaultCombatProfileProvider_WhenCreatureActionRuleSpellIsMissing_DoesNotResolveProfileAndReportsMissingSpell()
    {
        const uint creature2Id = 90005u;
        IGameTableManager gameTableManager = CreateCombatProfileGameTableManager(
            creatureEntries:
            [
                new Creature2Entry
                {
                    Id                   = creature2Id,
                    Creature2ActionSetId = 91u
                }
            ],
            actionEntries:
            [
                new Creature2ActionEntry
                {
                    Id                  = 1u,
                    CreatureActionSetId = 91u,
                    State               = 1u,
                    Event               = 2u,
                    Action              = 3u,
                    ActionData00        = 999u
                }
            ]);
        DefaultCombatProfileProvider provider = DefaultCombatProfileProvider.ForGameTables(
            gameTableManager,
            [
                new CombatActionRule
                {
                    State         = 1u,
                    Event         = 2u,
                    Action        = 3u,
                    Kind          = CombatActionRuleKind.AutoAttack,
                    Spell4IdField = "actionData00",
                    Source        = "unit-test",
                    Evidence      = "missing spell row"
                }
            ]);
        ICreatureEntity creature = RecordingDispatchProxy<ICreatureEntity>.Create(out RecordingDispatchProxy<ICreatureEntity> creatureProxy);
        creatureProxy.SetProperty(nameof(ICreatureEntity.CreatureId), creature2Id);

        CombatProfileResolution resolution = provider.GetResolution(creature);
        CombatProfileAudit audit = provider.GetAudit();
        CombatProfileAuditDetails details = provider.GetAuditDetails();

        Assert.Equal(CombatProfileResolutionSource.None, resolution.Source);
        Assert.Null(resolution.Profile);
        Assert.Equal(0, audit.ActionDerivedProfileCount);
        Assert.Equal(0, audit.ActionRejectedRuleRowCount);
        Assert.Equal(0, audit.ActionUnknownRowCount);
        Assert.Equal(1, audit.ActionMissingSpellRowCount);
        Assert.Equal(0, audit.MappedCreatureCount);
        Assert.Equal(1, audit.UnmappedCreatureCount);
        CombatProfileActionAuditRow actionRow = Assert.Single(details.ActionRows);
        Assert.Equal(CombatActionAuditStatus.MissingSpell, actionRow.Status);
        Assert.Equal(999u, actionRow.Spell4Id);
    }

    [Fact]
    public void DefaultCombatProfileProvider_DefaultProfile_ReturnsDataBackedFallbackSpellKit()
    {
        CombatProfile profile = DefaultCombatProfileProvider.Instance.GetDefaultProfile();

        Assert.Equal(new uint[] { 5649u, 5652u }, profile.AutoAttackSpell4Ids);
        Assert.Equal(0u, profile.AggroSpell4Id);
        Assert.Equal(5f, profile.ChaseDistance);
        Assert.Null(profile.AggroRange);
        Assert.Equal(0f, profile.AssistRange);
        Assert.False(profile.Stationary);
        Assert.False(profile.TraceCombat);
        Assert.False(profile.AllowNonPlayerTargets);
        Assert.False(profile.AssistSummoner);
        Assert.Equal(0f, profile.SummonerAssistRange);
        Assert.Equal(0f, profile.SummonerFollowDistance);
        Assert.Equal(0f, profile.SummonerFollowRepathDistance);
        Assert.Empty(profile.SpecialAttacks);
    }

    [Fact]
    public void Update_WhenIdleUnitAlreadyInsideLeashMovesIntoAggroRange_AggroesTarget()
    {
        CombatHarness harness = CreateHarness(new Vector3(10f, 0f, 0f));

        harness.Script.Update(0.5d);

        Assert.NotNull(harness.CreatureThreat.GetHostile(harness.Player.Guid));
        RecordingDispatchProxy<ICreatureEntity>.Invocation cast = Assert.Single(harness.CreatureProxy.GetInvocations(nameof(ICreatureEntity.CastSpell)));
        Assert.Equal(41368u, cast.Arguments[0]);
        RecordingDispatchProxy<IMovementManager>.Invocation face = Assert.Single(harness.MovementProxy.GetInvocations(nameof(IMovementManager.SetRotationFaceUnit)));
        Assert.Equal(harness.Player.Guid, face.Arguments[0]);
    }

    [Fact]
    public void Update_WhenIdleUnitTargetInsideLeashButOutsideAggroRange_DoesNotAggro()
    {
        CombatHarness harness = CreateHarness(new Vector3(20f, 0f, 0f));

        harness.Script.Update(0.5d);

        Assert.Null(harness.CreatureThreat.GetHostile(harness.Player.Guid));
        Assert.Empty(harness.CreatureProxy.GetInvocations(nameof(ICreatureEntity.CastSpell)));
        Assert.Empty(harness.MovementProxy.GetInvocations(nameof(IMovementManager.SetRotationFaceUnit)));
    }

    [Fact]
    public void Update_WhenProfiledAllySharesFactionAndIsNearby_AssistsAggroTarget()
    {
        ICreatureEntity ally = CreateAlly(200u, new Vector3(6f, 0f, 0f), TutorialDominionCombatFaction, out IThreatManager allyThreat);
        CombatHarness harness = CreateHarness(new Vector3(10f, 0f, 0f), extraInRange: [ally]);

        harness.Script.Update(0.5d);

        Assert.NotNull(harness.CreatureThreat.GetHostile(harness.Player.Guid));
        Assert.NotNull(allyThreat.GetHostile(harness.Player.Guid));
    }

    [Fact]
    public void Update_WhenProfiledAllyHasDifferentFaction_DoesNotAssistAggroTarget()
    {
        ICreatureEntity ally = CreateAlly(200u, new Vector3(6f, 0f, 0f), Faction.Exile, out IThreatManager allyThreat);
        CombatHarness harness = CreateHarness(new Vector3(10f, 0f, 0f), extraInRange: [ally]);

        harness.Script.Update(0.5d);

        Assert.NotNull(harness.CreatureThreat.GetHostile(harness.Player.Guid));
        Assert.Null(allyThreat.GetHostile(harness.Player.Guid));
    }

    [Fact]
    public void Update_WhenProfiledAllyCannotSeeTarget_DoesNotAssistAggroTarget()
    {
        ICreatureEntity ally = CreateAlly(200u, new Vector3(6f, 0f, 0f), Faction.Dominion, out IThreatManager allyThreat, canSeeAttackableTarget: false);
        CombatHarness harness = CreateHarness(new Vector3(10f, 0f, 0f), extraInRange: [ally]);

        harness.Script.Update(0.5d);

        Assert.NotNull(harness.CreatureThreat.GetHostile(harness.Player.Guid));
        Assert.Null(allyThreat.GetHostile(harness.Player.Guid));
    }

    [Fact]
    public void Update_WhenSummonedAssistProfileOwnerTargetsHostileCreature_AssistsOwnerTarget()
    {
        ICreatureEntity hostileCreature = CreateHostileCreature(400u, new Vector3(12f, 0f, 0f));
        CombatProfile profile = CombatProfile.Default with
        {
            AllowNonPlayerTargets = true,
            AssistSummoner = true,
            SummonerAssistRange = 35f,
            SummonerFollowDistance = 4f,
            SummonerFollowRepathDistance = 8f
        };
        CombatHarness harness = CreateHarness(
            new Vector3(10f, 0f, 0f),
            extraInRange: [hostileCreature],
            canAttack: unit => ReferenceEquals(unit, hostileCreature),
            profileProvider: new FixedCombatProfileProvider(profile),
            summonerGuid: 200u,
            playerTargetGuid: hostileCreature.Guid);

        harness.Script.Update(0.5d);

        Assert.NotNull(harness.CreatureThreat.GetHostile(hostileCreature.Guid));
        Assert.Equal(hostileCreature.Guid, harness.Creature.TargetGuid);
    }

    [Fact]
    public void Update_WhenSummonedAssistProfileHostileTargetsOwner_AssistsOwnerAttacker()
    {
        ICreatureEntity hostileCreature = CreateHostileCreature(400u, new Vector3(12f, 0f, 0f), targetGuid: 200u);
        CombatProfile profile = CombatProfile.Default with
        {
            AllowNonPlayerTargets = true,
            AssistSummoner = true,
            SummonerAssistRange = 35f,
            SummonerFollowDistance = 4f,
            SummonerFollowRepathDistance = 8f
        };
        CombatHarness harness = CreateHarness(
            new Vector3(10f, 0f, 0f),
            extraInRange: [hostileCreature],
            canAttack: unit => ReferenceEquals(unit, hostileCreature),
            profileProvider: new FixedCombatProfileProvider(profile),
            summonerGuid: 200u);

        harness.Script.Update(0.5d);

        Assert.NotNull(harness.CreatureThreat.GetHostile(hostileCreature.Guid));
        Assert.Equal(hostileCreature.Guid, harness.Creature.TargetGuid);
    }

    [Fact]
    public void Update_WhenSummonedAssistProfileIdleAndOwnerMovesAway_FollowsOwner()
    {
        CombatProfile profile = CombatProfile.Default with
        {
            AssistSummoner = true,
            SummonerFollowDistance = 4f,
            SummonerFollowRepathDistance = 8f
        };
        CombatHarness harness = CreateHarness(
            new Vector3(12f, 0f, 0f),
            dispositionToPlayer: Disposition.Friendly,
            profileProvider: new FixedCombatProfileProvider(profile),
            summonerGuid: 200u);

        harness.Script.Update(1d);

        RecordingDispatchProxy<IMovementManager>.Invocation follow =
            Assert.Single(harness.MovementProxy.GetInvocations(nameof(IMovementManager.Follow)));
        Assert.Same(harness.Player, follow.Arguments[0]);
        Assert.Equal(4f, follow.Arguments[1]);
    }

    [Fact]
    public void Update_WhenUnprofiledCreatureSeesHostileCreature_DoesNotIdleAggro()
    {
        ICreatureEntity hostileCreature = CreateHostileCreature(400u, new Vector3(6f, 0f, 0f));
        CombatHarness harness = CreateHarness(
            new Vector3(100f, 0f, 0f),
            creatureId: 200u,
            includePlayer: false,
            extraInRange: [hostileCreature],
            canAttack: unit => ReferenceEquals(unit, hostileCreature));

        harness.Script.Update(0.5d);

        Assert.Null(harness.CreatureThreat.GetHostile(hostileCreature.Guid));
        Assert.Empty(harness.CreatureProxy.GetInvocations(nameof(ICreatureEntity.CastSpell)));
        Assert.Empty(harness.MovementProxy.GetInvocations(nameof(IMovementManager.SetRotationFaceUnit)));
    }

    [Fact]
    public void Update_WhenUnprofiledCreatureSeesPlayer_DoesNotAggro()
    {
        CombatHarness harness = CreateHarness(new Vector3(10f, 0f, 0f), creatureId: 200u);

        harness.Script.Update(0.5d);

        Assert.Null(harness.CreatureThreat.GetHostile(harness.Player.Guid));
        Assert.Empty(harness.CreatureProxy.GetInvocations(nameof(ICreatureEntity.SetInRangeCheck)));
        Assert.Empty(harness.CreatureProxy.GetInvocations(nameof(ICreatureEntity.CastSpell)));
        Assert.Empty(harness.MovementProxy.GetInvocations(nameof(IMovementManager.SetRotationFaceUnit)));
    }

    [Fact]
    public void Update_WhenNorthernWildsProfiledCreatureSeesHostilePlayer_AggroesWithoutAwarenessSpell()
    {
        CombatHarness harness = CreateHarness(new Vector3(10f, 0f, 0f), creatureId: 11910u);

        harness.Script.Update(0.5d);

        Assert.NotNull(harness.CreatureThreat.GetHostile(harness.Player.Guid));
        RecordingDispatchProxy<ICreatureEntity>.Invocation rangeCheck = Assert.Single(harness.CreatureProxy.GetInvocations(nameof(ICreatureEntity.SetInRangeCheck)));
        Assert.Equal(50f, rangeCheck.Arguments[0]);
        Assert.Empty(harness.CreatureProxy.GetInvocations(nameof(ICreatureEntity.CastSpell)));
        RecordingDispatchProxy<IMovementManager>.Invocation face = Assert.Single(harness.MovementProxy.GetInvocations(nameof(IMovementManager.SetRotationFaceUnit)));
        Assert.Equal(harness.Player.Guid, face.Arguments[0]);
    }

    [Fact]
    public void OnHealthChange_WhenUnprofiledCreatureIsDamagedByVisiblePlayer_Retaliates()
    {
        CombatHarness harness = CreateHarness(new Vector3(10f, 0f, 0f), creatureId: 200u);

        harness.Script.OnHealthChange(harness.Player, 1u, DamageType.Physical);

        Assert.NotNull(harness.CreatureThreat.GetHostile(harness.Player.Guid));
        Assert.Empty(harness.CreatureProxy.GetInvocations(nameof(ICreatureEntity.CastSpell)));
        RecordingDispatchProxy<IMovementManager>.Invocation face = Assert.Single(harness.MovementProxy.GetInvocations(nameof(IMovementManager.SetRotationFaceUnit)));
        Assert.Equal(harness.Player.Guid, face.Arguments[0]);
    }

    [Fact]
    public void OnHealthChange_WhenDamageThreatAlreadySelectedTarget_StillRunsEngageSideEffects()
    {
        CombatHarness harness = CreateHarness(new Vector3(10f, 0f, 0f));
        harness.CreatureProxy.SetProperty(nameof(ICreatureEntity.InCombat), true);
        harness.CreatureThreat.UpdateThreat(harness.Player, 10);

        Assert.Equal(harness.Player.Guid, harness.Creature.TargetGuid);
        Assert.Empty(harness.CreatureProxy.GetInvocations(nameof(ICreatureEntity.CastSpell)));
        Assert.Empty(harness.MovementProxy.GetInvocations(nameof(IMovementManager.SetRotationFaceUnit)));

        harness.Script.OnHealthChange(harness.Player, 1u, DamageType.Physical);

        RecordingDispatchProxy<ICreatureEntity>.Invocation cast = Assert.Single(harness.CreatureProxy.GetInvocations(nameof(ICreatureEntity.CastSpell)));
        Assert.Equal(41368u, cast.Arguments[0]);
        Assert.Single(harness.MovementProxy.GetInvocations(nameof(IMovementManager.Finalise)));
        RecordingDispatchProxy<IMovementManager>.Invocation face = Assert.Single(harness.MovementProxy.GetInvocations(nameof(IMovementManager.SetRotationFaceUnit)));
        Assert.Equal(harness.Player.Guid, face.Arguments[0]);
    }

    [Fact]
    public void Update_WhenDerivedCombatScriptHasNoEmbeddedProfile_UsesDefaultCombatProfileWithoutAwarenessSpell()
    {
        CombatHarness harness = CreateHarness(
            new Vector3(10f, 0f, 0f),
            creatureId: 200u,
            enableDefaultProfileFallback: true);

        harness.Script.Update(0.5d);

        Assert.NotNull(harness.CreatureThreat.GetHostile(harness.Player.Guid));
        Assert.Empty(harness.CreatureProxy.GetInvocations(nameof(ICreatureEntity.CastSpell)));
    }

    [Fact]
    public void Update_WhenNeutralPlayerInsideRange_DoesNotIdleAggro()
    {
        CombatHarness harness = CreateHarness(
            new Vector3(10f, 0f, 0f),
            dispositionToPlayer: Disposition.Neutral);

        harness.Script.Update(0.5d);

        Assert.Null(harness.CreatureThreat.GetHostile(harness.Player.Guid));
        Assert.Empty(harness.CreatureProxy.GetInvocations(nameof(ICreatureEntity.CastSpell)));
        Assert.Empty(harness.MovementProxy.GetInvocations(nameof(IMovementManager.SetRotationFaceUnit)));
    }

    [Fact]
    public void OnHealthChange_WhenNeutralPlayerDamagesCreature_AggroesTarget()
    {
        CombatHarness harness = CreateHarness(
            new Vector3(10f, 0f, 0f),
            dispositionToPlayer: Disposition.Neutral);

        harness.Script.OnHealthChange(harness.Player, 1u, DamageType.Physical);

        Assert.NotNull(harness.CreatureThreat.GetHostile(harness.Player.Guid));
        RecordingDispatchProxy<ICreatureEntity>.Invocation cast = Assert.Single(harness.CreatureProxy.GetInvocations(nameof(ICreatureEntity.CastSpell)));
        Assert.Equal(41368u, cast.Arguments[0]);
    }

    [Fact]
    public void OnEnterRange_WhenPlayerIsNotVisible_DoesNotAggro()
    {
        CombatHarness harness = CreateHarness(
            new Vector3(10f, 0f, 0f),
            isVisible: _ => false);

        harness.Script.OnEnterRange(harness.Player);

        Assert.Null(harness.CreatureThreat.GetHostile(harness.Player.Guid));
        Assert.Empty(harness.CreatureProxy.GetInvocations(nameof(ICreatureEntity.CastSpell)));
        Assert.Empty(harness.MovementProxy.GetInvocations(nameof(IMovementManager.SetRotationFaceUnit)));
    }

    [Fact]
    public void Update_WhenProfileProviderSuppliesCreatureProfile_UsesConfiguredAggroSpellAndRange()
    {
        var profileProvider = new FixedCombatProfileProvider(CombatProfile.Default with
        {
            AggroSpell4Id    = 99123u,
            AggroRange       = 11f,
            MinimumLeashRange = 77f,
            AssistRange      = 0f,
            TraceCombat      = false
        });
        CombatHarness harness = CreateHarness(new Vector3(10f, 0f, 0f), profileProvider: profileProvider);

        RecordingDispatchProxy<ICreatureEntity>.Invocation rangeCheck = Assert.Single(harness.CreatureProxy.GetInvocations(nameof(ICreatureEntity.SetInRangeCheck)));
        Assert.Equal(77f, rangeCheck.Arguments[0]);

        harness.Script.Update(0.5d);

        Assert.NotNull(harness.CreatureThreat.GetHostile(harness.Player.Guid));
        RecordingDispatchProxy<ICreatureEntity>.Invocation cast = Assert.Single(harness.CreatureProxy.GetInvocations(nameof(ICreatureEntity.CastSpell)));
        Assert.Equal(99123u, cast.Arguments[0]);
    }

    [Fact]
    public void Update_BeforeCreatureIsAddedToMap_DoesNotIdleAggro()
    {
        CombatHarness harness = CreateHarness(
            new Vector3(10f, 0f, 0f),
            armRangeCheck: false);

        harness.Script.Update(0.5d);

        Assert.Null(harness.CreatureThreat.GetHostile(harness.Player.Guid));
        Assert.Empty(harness.CreatureProxy.GetInvocations(nameof(ICreatureEntity.SetInRangeCheck)));
        Assert.Empty(harness.CreatureProxy.GetInvocations(nameof(ICreatureEntity.CastSpell)));
        Assert.Empty(harness.MovementProxy.GetInvocations(nameof(IMovementManager.SetRotationFaceUnit)));
    }

    [Fact]
    public void Update_WhenSelfAppearsInRange_DoesNotAggroSelf()
    {
        var profileProvider = new FixedCombatProfileProvider(CombatProfile.Default with
        {
            AllowNonPlayerTargets = true,
            AggroRange            = 14f,
            TraceCombat           = false
        });
        CombatHarness harness = CreateHarness(
            new Vector3(100f, 0f, 0f),
            includePlayer: false,
            includeSelfInRange: true,
            profileProvider: profileProvider,
            canAttack: _ => true);

        harness.Script.Update(0.5d);

        Assert.False(harness.CreatureThreat.IsThreatened);
        Assert.Empty(harness.CreatureProxy.GetInvocations(nameof(ICreatureEntity.CastSpell)));
        Assert.Empty(harness.MovementProxy.GetInvocations(nameof(IMovementManager.SetRotationFaceUnit)));
    }

    [Fact]
    public void Update_WhenProfileSpecialAttackCooldownElapses_CastsSpecialAtCurrentTarget()
    {
        var profileProvider = new FixedCombatProfileProvider(CombatProfile.Default with
        {
            AutoAttackSpell4Ids = [],
            SpecialAttacks =
            [
                new CombatSpecialAttack(
                    Spell4Id: 88101u,
                    CooldownSeconds: 1d,
                    MaxRange: 15f)
            ],
            TraceCombat = false
        });
        CombatHarness harness = CreateHarness(
            new Vector3(10f, 0f, 0f),
            targetSelected: true,
            profileProvider: profileProvider,
            spell4Entries:
            [
                new Spell4Entry
                {
                    Id             = 88101u,
                    TargetMaxRange = 15f,
                    CastTime       = 750u
                }
            ]);

        harness.Script.Update(1d);

        RecordingDispatchProxy<ICreatureEntity>.Invocation cast = Assert.Single(harness.CreatureProxy.GetInvocations(nameof(ICreatureEntity.TryCastSpell)));
        Assert.Equal(88101u, cast.Arguments[0]);

        var parameters = Assert.IsAssignableFrom<ISpellParameters>(cast.Arguments[1]);
        Assert.Equal(harness.Player.Guid, parameters.PrimaryTargetId);

        RecordingDispatchProxy<IMovementManager>.Invocation face = Assert.Single(harness.MovementProxy.GetInvocations(nameof(IMovementManager.SetRotationFaceUnit)));
        Assert.Equal(harness.Player.Guid, face.Arguments[0]);
    }

    [Fact]
    public void Update_WhenMultipleProfileSpecialAttacksAreReady_RotatesReadySpecials()
    {
        var profileProvider = new FixedCombatProfileProvider(CombatProfile.Default with
        {
            AutoAttackSpell4Ids = [],
            SpecialAttacks =
            [
                new CombatSpecialAttack(
                    Spell4Id: 88111u,
                    CooldownSeconds: 1d),
                new CombatSpecialAttack(
                    Spell4Id: 88112u,
                    CooldownSeconds: 1d)
            ],
            TraceCombat = false
        });
        CombatHarness harness = CreateHarness(
            new Vector3(10f, 0f, 0f),
            targetSelected: true,
            profileProvider: profileProvider,
            spell4Entries:
            [
                new Spell4Entry
                {
                    Id             = 88111u,
                    TargetMaxRange = 15f
                },
                new Spell4Entry
                {
                    Id             = 88112u,
                    TargetMaxRange = 15f
                }
            ]);

        harness.Script.Update(1d);
        harness.Script.Update(0.1d);

        List<RecordingDispatchProxy<ICreatureEntity>.Invocation> casts = harness.CreatureProxy
            .GetInvocations(nameof(ICreatureEntity.TryCastSpell))
            .ToList();
        Assert.Equal(2, casts.Count);
        Assert.Equal(88111u, casts[0].Arguments[0]);
        Assert.Equal(88112u, casts[1].Arguments[0]);
    }

    [Fact]
    public void Update_WhenProfileSpecialAttackCastIsRejected_DoesNotResetCooldown()
    {
        var profileProvider = new FixedCombatProfileProvider(CombatProfile.Default with
        {
            AutoAttackSpell4Ids = [],
            SpecialAttacks =
            [
                new CombatSpecialAttack(
                    Spell4Id: 88104u,
                    CooldownSeconds: 1d)
            ],
            TraceCombat = false
        });
        CombatHarness harness = CreateHarness(
            new Vector3(10f, 0f, 0f),
            targetSelected: true,
            profileProvider: profileProvider,
            spell4Entries:
            [
                new Spell4Entry
                {
                    Id             = 88104u,
                    TargetMaxRange = 15f
                }
            ]);

        harness.CreatureProxy.SetMethodReturn(nameof(ICreatureEntity.TryCastSpell), CastResult.SpellAlreadyCasting);

        harness.Script.Update(1d);

        RecordingDispatchProxy<ICreatureEntity>.Invocation rejectedCast = Assert.Single(harness.CreatureProxy.GetInvocations(nameof(ICreatureEntity.TryCastSpell)));
        Assert.Equal(88104u, rejectedCast.Arguments[0]);

        harness.CreatureProxy.SetMethodReturn(nameof(ICreatureEntity.TryCastSpell), CastResult.Ok);
        harness.Script.Update(0.1d);

        Assert.Equal(2, harness.CreatureProxy.GetInvocations(nameof(ICreatureEntity.TryCastSpell)).Count);
    }

    [Fact]
    public void Update_WhenProfileSpecialAttackHasCastTime_SuppressesAutoAttackDuringWindup()
    {
        var profileProvider = new FixedCombatProfileProvider(CombatProfile.Default with
        {
            AutoAttackSpell4Ids = [],
            SpecialAttacks =
            [
                new CombatSpecialAttack(
                    Spell4Id: 88105u,
                    CooldownSeconds: 10d)
            ],
            TraceCombat = false
        });
        CombatHarness harness = CreateHarness(
            new Vector3(5f, 0f, 0f),
            targetSelected: true,
            profileProvider: profileProvider,
            autoAttacks: [777u],
            spell4Entries:
            [
                new Spell4Entry
                {
                    Id             = 88105u,
                    TargetMaxRange = 15f,
                    CastTime       = 1000u
                },
                new Spell4Entry
                {
                    Id             = 777u,
                    TargetMaxRange = 15f
                }
            ]);

        harness.Script.Update(10d);
        harness.Script.Update(0.5d);

        Assert.Single(harness.CreatureProxy.GetInvocations(nameof(ICreatureEntity.TryCastSpell)));
        Assert.Empty(harness.CreatureProxy.GetInvocations(nameof(ICreatureEntity.CastSpell)));
        Assert.Single(harness.MovementProxy.GetInvocations(nameof(IMovementManager.Finalise)));

        harness.Script.Update(0.5d);
        harness.Script.Update(1d);

        List<RecordingDispatchProxy<ICreatureEntity>.Invocation> casts = harness.CreatureProxy
            .GetInvocations(nameof(ICreatureEntity.TryCastSpell))
            .ToList();
        Assert.Equal(2, casts.Count);
        RecordingDispatchProxy<ICreatureEntity>.Invocation autoAttack = casts[1];
        Assert.Equal(777u, autoAttack.Arguments[0]);
    }

    [Fact]
    public void Update_WhenProfileSpecialAttackHasChannelMaxTime_SuppressesAutoAttackDuringChannel()
    {
        var profileProvider = new FixedCombatProfileProvider(CombatProfile.Default with
        {
            AutoAttackSpell4Ids = [],
            SpecialAttacks =
            [
                new CombatSpecialAttack(
                    Spell4Id: 88113u,
                    CooldownSeconds: 10d)
            ],
            TraceCombat = false
        });
        CombatHarness harness = CreateHarness(
            new Vector3(5f, 0f, 0f),
            targetSelected: true,
            profileProvider: profileProvider,
            autoAttacks: [779u],
            spell4Entries:
            [
                new Spell4Entry
                {
                    Id             = 88113u,
                    TargetMaxRange = 15f,
                    ChannelMaxTime = 2500u
                },
                new Spell4Entry
                {
                    Id             = 779u,
                    TargetMaxRange = 15f
                }
            ]);

        harness.Script.Update(10d);
        harness.Script.Update(2.4d);

        Assert.Single(harness.CreatureProxy.GetInvocations(nameof(ICreatureEntity.TryCastSpell)));
        Assert.Empty(harness.CreatureProxy.GetInvocations(nameof(ICreatureEntity.CastSpell)));
        Assert.Single(harness.MovementProxy.GetInvocations(nameof(IMovementManager.Finalise)));

        harness.Script.Update(0.1d);
        harness.Script.Update(1.5d);

        List<RecordingDispatchProxy<ICreatureEntity>.Invocation> casts = harness.CreatureProxy
            .GetInvocations(nameof(ICreatureEntity.TryCastSpell))
            .ToList();
        Assert.Equal(2, casts.Count);
        RecordingDispatchProxy<ICreatureEntity>.Invocation autoAttack = casts[1];
        Assert.Equal(779u, autoAttack.Arguments[0]);
    }

    [Fact]
    public void Update_WhenProfileSpecialAttackWindupIsInterrupted_AllowsAiToResume()
    {
        var profileProvider = new FixedCombatProfileProvider(CombatProfile.Default with
        {
            AutoAttackSpell4Ids = [],
            SpecialAttacks =
            [
                new CombatSpecialAttack(
                    Spell4Id: 88106u,
                    CooldownSeconds: 10d)
            ],
            TraceCombat = false
        });
        CombatHarness harness = CreateHarness(
            new Vector3(5f, 0f, 0f),
            targetSelected: true,
            profileProvider: profileProvider,
            autoAttacks: [778u],
            spell4Entries:
            [
                new Spell4Entry
                {
                    Id             = 88106u,
                    TargetMaxRange = 15f,
                    CastTime       = 5000u
                },
                new Spell4Entry
                {
                    Id             = 778u,
                    TargetMaxRange = 15f
                }
            ]);

        harness.Script.Update(10d);
        harness.CreatureProxy.SetProperty(nameof(ICreatureEntity.ActiveCCStateMask), 1u << (int)CCState.Interrupt);
        harness.Script.Update(1.5d);

        List<RecordingDispatchProxy<ICreatureEntity>.Invocation> casts = harness.CreatureProxy
            .GetInvocations(nameof(ICreatureEntity.TryCastSpell))
            .ToList();
        Assert.Equal(2, casts.Count);
        RecordingDispatchProxy<ICreatureEntity>.Invocation autoAttack = casts[1];
        Assert.Equal(778u, autoAttack.Arguments[0]);
    }

    [Fact]
    public void Update_WhenProfileSpecialAttackTargetOutOfRange_DoesNotResetCooldown()
    {
        var profileProvider = new FixedCombatProfileProvider(CombatProfile.Default with
        {
            AutoAttackSpell4Ids = [],
            SpecialAttacks =
            [
                new CombatSpecialAttack(
                    Spell4Id: 88101u,
                    CooldownSeconds: 1d,
                    MaxRange: 15f)
            ],
            TraceCombat = false
        });
        CombatHarness harness = CreateHarness(
            new Vector3(20f, 0f, 0f),
            targetSelected: true,
            profileProvider: profileProvider,
            spell4Entries:
            [
                new Spell4Entry
                {
                    Id             = 88101u,
                    TargetMaxRange = 15f
                }
            ]);

        harness.Script.Update(1d);
        Assert.Empty(harness.CreatureProxy.GetInvocations(nameof(ICreatureEntity.TryCastSpell)));

        harness.PlayerProxy.SetProperty(nameof(IPlayer.Position), new Vector3(10f, 0f, 0f));
        harness.Script.Update(0.1d);

        RecordingDispatchProxy<ICreatureEntity>.Invocation cast = Assert.Single(harness.CreatureProxy.GetInvocations(nameof(ICreatureEntity.TryCastSpell)));
        Assert.Equal(88101u, cast.Arguments[0]);
    }

    [Fact]
    public void Update_WhenProfileSpecialAttackTargetTooCloseForSpellMinRange_DoesNotCast()
    {
        var profileProvider = new FixedCombatProfileProvider(CombatProfile.Default with
        {
            AutoAttackSpell4Ids = [],
            SpecialAttacks =
            [
                new CombatSpecialAttack(
                    Spell4Id: 88102u,
                    CooldownSeconds: 1d)
            ],
            TraceCombat = false
        });
        CombatHarness harness = CreateHarness(
            new Vector3(2f, 0f, 0f),
            targetSelected: true,
            profileProvider: profileProvider,
            spell4Entries:
            [
                new Spell4Entry
                {
                    Id             = 88102u,
                    TargetMinRange = 4f,
                    TargetMaxRange = 15f
                }
            ]);

        harness.Script.Update(1d);

        Assert.Empty(harness.CreatureProxy.GetInvocations(nameof(ICreatureEntity.CastSpell)));
    }

    [Fact]
    public void Update_WhenProfileSpecialAttackTargetExceedsSpellVerticalRange_DoesNotCast()
    {
        var profileProvider = new FixedCombatProfileProvider(CombatProfile.Default with
        {
            AutoAttackSpell4Ids = [],
            SpecialAttacks =
            [
                new CombatSpecialAttack(
                    Spell4Id: 88103u,
                    CooldownSeconds: 1d)
            ],
            TraceCombat = false
        });
        CombatHarness harness = CreateHarness(
            new Vector3(2f, 5f, 0f),
            targetSelected: true,
            profileProvider: profileProvider,
            spell4Entries:
            [
                new Spell4Entry
                {
                    Id                  = 88103u,
                    TargetMaxRange      = 15f,
                    TargetVerticalRange = 3f
                }
            ]);

        harness.Script.Update(1d);

        Assert.Empty(harness.CreatureProxy.GetInvocations(nameof(ICreatureEntity.CastSpell)));
    }

    [Fact]
    public void ThreatAdd_WhenProfiledCreatureThreatenedByHostileCreature_RejectsThreatAndResets()
    {
        ICreatureEntity hostileCreature = CreateHostileCreature(400u, new Vector3(6f, 0f, 0f));
        CombatHarness harness = CreateHarness(
            new Vector3(100f, 0f, 0f),
            includePlayer: false,
            extraInRange: [hostileCreature],
            canAttack: unit => ReferenceEquals(unit, hostileCreature));

        harness.CreatureThreat.UpdateThreat(hostileCreature, 1);

        Assert.Null(harness.CreatureThreat.GetHostile(hostileCreature.Guid));
        Assert.Null(hostileCreature.ThreatManager.GetHostile(harness.Creature.Guid));
        Assert.Contains(harness.CreatureProxy.GetInvocations(nameof(ICreatureEntity.SetTarget)), invocation => invocation.Arguments[0] == null);
        Assert.Single(harness.MovementProxy.GetInvocations(nameof(IMovementManager.Finalise)));
    }

    [Fact]
    public void ThreatAdd_WhenResettingCreatureHasInvalidPosition_MovesBackToLeashPosition()
    {
        Vector3 leashPosition = new(12f, 3f, -4f);
        ICreatureEntity hostileCreature = CreateHostileCreature(400u, new Vector3(6f, 0f, 0f));
        CombatHarness harness = CreateHarness(
            new Vector3(100f, 0f, 0f),
            includePlayer: false,
            extraInRange: [hostileCreature],
            canAttack: unit => ReferenceEquals(unit, hostileCreature),
            creaturePosition: new Vector3(float.NaN, float.NaN, float.NaN),
            leashPosition: leashPosition);

        harness.CreatureThreat.UpdateThreat(hostileCreature, 1);

        RecordingDispatchProxy<IMovementManager>.Invocation setPosition = Assert.Single(harness.MovementProxy.GetInvocations(nameof(IMovementManager.SetPosition)));
        Assert.Equal(leashPosition, setPosition.Arguments[0]);
        Assert.Equal(true, setPosition.Arguments[1]);
    }

    [Fact]
    public void Update_WhenChaseAlreadyTracksSameTargetPosition_DoesNotRepath()
    {
        CombatHarness harness = CreateHarness(new Vector3(20f, 0f, 0f), targetSelected: true);

        harness.Script.Update(1d);
        harness.Script.Update(1d);

        Assert.Single(harness.MovementProxy.GetInvocations(nameof(IMovementManager.Chase)));
        Assert.Empty(harness.MovementProxy.GetInvocations(nameof(IMovementManager.Finalise)));
    }

    [Fact]
    public void Update_WhenChaseTargetMovesPastRepathThreshold_ReissuesFollow()
    {
        CombatHarness harness = CreateHarness(new Vector3(20f, 0f, 0f), targetSelected: true);

        harness.Script.Update(1d);
        harness.PlayerProxy.SetProperty(nameof(IPlayer.Position), new Vector3(23f, 0f, 0f));
        harness.Script.Update(1d);

        Assert.Equal(2, harness.MovementProxy.GetInvocations(nameof(IMovementManager.Chase)).Count);
    }

    [Fact]
    public void Update_WhenChaseTargetMovesInsideChaseDistance_FinalisesActiveFollow()
    {
        CombatHarness harness = CreateHarness(new Vector3(20f, 0f, 0f), targetSelected: true);

        harness.Script.Update(1d);
        harness.PlayerProxy.SetProperty(nameof(IPlayer.Position), new Vector3(3f, 0f, 0f));
        harness.Script.Update(1d);

        Assert.Single(harness.MovementProxy.GetInvocations(nameof(IMovementManager.Chase)));
        Assert.Single(harness.MovementProxy.GetInvocations(nameof(IMovementManager.Finalise)));
    }

    [Fact]
    public void Update_WhenTargetEffectiveRangeIsInsideChaseDistance_DoesNotFollow()
    {
        CombatHarness harness = CreateHarness(
            new Vector3(6f, 0f, 0f),
            targetSelected: true,
            creatureHitRadius: 2f,
            playerHitRadius: 2f);

        harness.Script.Update(1d);

        Assert.Empty(harness.MovementProxy.GetInvocations(nameof(IMovementManager.Chase)));
    }

    [Fact]
    public void Update_WhenTargetEffectiveRangeIsInsideSpellRange_CastsAutoAttack()
    {
        const uint spell4Id = 777u;
        CombatHarness harness = CreateHarness(
            new Vector3(5.75f, 0f, 0f),
            targetSelected: true,
            creatureHitRadius: 2f,
            playerHitRadius: 2f,
            autoAttacks: [spell4Id],
            spell4Entries:
            [
                new Spell4Entry
                {
                    Id             = spell4Id,
                    TargetMaxRange = 5f
                }
            ]);

        harness.Script.Update(1.5d);

        RecordingDispatchProxy<ICreatureEntity>.Invocation cast = Assert.Single(harness.CreatureProxy.GetInvocations(nameof(ICreatureEntity.TryCastSpell)));
        Assert.Equal(spell4Id, cast.Arguments[0]);
    }

    [Fact]
    public void Update_WhenArtillerybotSummonerHasTier2_UsesTier2AutoAttack()
    {
        const uint summonBaseSpell4Id = 27002u;
        const uint autoAttackBaseSpell4Id = 20491u;
        const uint tier1AutoAttackSpell4Id = 34521u;
        const uint tier2AutoAttackSpell4Id = 56257u;

        ICreatureEntity target = CreateHostileCreature(300u, new Vector3(5f, 0f, 0f));
        var profileProvider = new FixedCombatProfileProvider(CombatProfile.Default with
        {
            AutoAttackSpell4Ids = [tier1AutoAttackSpell4Id],
            AllowNonPlayerTargets = true,
            SummonerTierSourceBaseSpell4Id = summonBaseSpell4Id,
            SummonerTieredAutoAttackBaseSpell4Id = autoAttackBaseSpell4Id
        });
        CombatHarness harness = CreateHarness(
            Vector3.Zero,
            targetSelected: true,
            extraInRange: [target],
            creatureId: 42683u,
            canAttack: unit => unit.Guid == target.Guid,
            initialTargetGuid: target.Guid,
            profileProvider: profileProvider,
            autoAttacks: [tier1AutoAttackSpell4Id],
            spell4Entries:
            [
                new Spell4Entry
                {
                    Id                    = tier1AutoAttackSpell4Id,
                    Spell4BaseIdBaseSpell = autoAttackBaseSpell4Id,
                    TierIndex             = 1u,
                    TargetMaxRange        = 10f
                },
                new Spell4Entry
                {
                    Id                    = tier2AutoAttackSpell4Id,
                    Spell4BaseIdBaseSpell = autoAttackBaseSpell4Id,
                    TierIndex             = 2u,
                    TargetMaxRange        = 10f
                }
            ],
            summonerGuid: 200u);

        ISpellManager spellManager = RecordingDispatchProxy<ISpellManager>.Create(out RecordingDispatchProxy<ISpellManager> spellManagerProxy);
        ICharacterSpell summonSpell = RecordingDispatchProxy<ICharacterSpell>.Create(out _);
        spellManagerProxy.SetMethodHandler(nameof(ISpellManager.GetSpell), args =>
            (uint)args[0] == summonBaseSpell4Id ? summonSpell : null);
        spellManagerProxy.SetMethodReturn(nameof(ISpellManager.GetSpellTier), (byte)2);
        harness.PlayerProxy.SetProperty(nameof(IPlayer.SpellManager), spellManager);

        harness.Script.Update(1.5d);

        RecordingDispatchProxy<ICreatureEntity>.Invocation cast = Assert.Single(harness.CreatureProxy.GetInvocations(nameof(ICreatureEntity.TryCastSpell)));
        Assert.Equal(tier2AutoAttackSpell4Id, cast.Arguments[0]);
    }

    [Fact]
    public void Update_WhenAutoAttackCastReentersAi_DoesNotCastAgainInSameStack()
    {
        const uint spell4Id = 777u;
        CombatHarness harness = CreateHarness(
            new Vector3(5f, 0f, 0f),
            targetSelected: true,
            autoAttacks: [spell4Id],
            spell4Entries:
            [
                new Spell4Entry
                {
                    Id             = spell4Id,
                    TargetMaxRange = 10f
                }
            ]);

        bool reentered = false;
        harness.CreatureProxy.SetMethodHandler(nameof(ICreatureEntity.TryCastSpell), _ =>
        {
            if (!reentered)
            {
                reentered = true;
                harness.Script.Update(1.5d);
            }

            return CastResult.Ok;
        });

        harness.Script.Update(1.5d);

        Assert.True(reentered);
        RecordingDispatchProxy<ICreatureEntity>.Invocation cast = Assert.Single(harness.CreatureProxy.GetInvocations(nameof(ICreatureEntity.TryCastSpell)));
        Assert.Equal(spell4Id, cast.Arguments[0]);
    }

    [Fact]
    public void Update_WhenAutoAttackCastIsRejected_DoesNotRotateAutoAttack()
    {
        const uint firstSpell4Id = 777u;
        const uint secondSpell4Id = 778u;
        CombatHarness harness = CreateHarness(
            new Vector3(5f, 0f, 0f),
            targetSelected: true,
            autoAttacks: [firstSpell4Id, secondSpell4Id],
            spell4Entries:
            [
                new Spell4Entry
                {
                    Id             = firstSpell4Id,
                    TargetMaxRange = 10f
                },
                new Spell4Entry
                {
                    Id             = secondSpell4Id,
                    TargetMaxRange = 10f
                }
            ]);

        harness.CreatureProxy.SetMethodReturn(nameof(ICreatureEntity.TryCastSpell), CastResult.SpellAlreadyCasting);
        harness.Script.Update(1.5d);

        harness.CreatureProxy.SetMethodReturn(nameof(ICreatureEntity.TryCastSpell), CastResult.Ok);
        harness.Script.Update(1.5d);

        List<RecordingDispatchProxy<ICreatureEntity>.Invocation> casts = harness.CreatureProxy
            .GetInvocations(nameof(ICreatureEntity.TryCastSpell))
            .ToList();
        Assert.Equal(2, casts.Count);
        Assert.Equal(firstSpell4Id, casts[0].Arguments[0]);
        Assert.Equal(firstSpell4Id, casts[1].Arguments[0]);
    }

    [Fact]
    public void Update_WhenTargetExceedsSpellVerticalRange_DoesNotCastAutoAttack()
    {
        const uint spell4Id = 778u;
        CombatHarness harness = CreateHarness(
            new Vector3(1f, 5f, 0f),
            targetSelected: true,
            autoAttacks: [spell4Id],
            spell4Entries:
            [
                new Spell4Entry
                {
                    Id                  = spell4Id,
                    TargetMaxRange      = 10f,
                    TargetVerticalRange = 3f
                }
            ]);

        harness.Script.Update(1.5d);

        Assert.Empty(harness.CreatureProxy.GetInvocations(nameof(ICreatureEntity.TryCastSpell)));
    }

    [Fact]
    public void OnThreatRemoveTarget_WhenPatrollingCreatureReturnsHome_ResumesPatrolSpline()
    {
        CombatHarness harness = CreateHarness(
            new Vector3(20f, 0f, 0f),
            targetSelected: true,
            creaturePosition: new Vector3(10f, 0f, 0f),
            patrolSpline: new EntitySplineModel
            {
                SplineId = 77,
                Mode     = SplineMode.BackAndForth,
                Speed    = -2f
            });

        harness.Script.OnThreatRemoveTarget(null);

        RecordingDispatchProxy<IGameSession>.Invocation evaded = Assert.Single(harness.SessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
        ServerGenericFloaterLocalised floater = Assert.IsType<ServerGenericFloaterLocalised>(evaded.Arguments[0]);
        Assert.Equal(0x5F95Cu, floater.LocalisedTextId);

        RecordingDispatchProxy<IMovementManager>.Invocation returnHome = Assert.Single(
            harness.MovementProxy.GetInvocations(nameof(IMovementManager.LaunchPath)));
        Assert.Equal(Vector3.Zero, returnHome.Arguments[0]);
        Assert.Equal(10f, returnHome.Arguments[1]);
        Assert.Equal(SplineMode.OneShot, returnHome.Arguments[2]);

        harness.Script.OnPositionEntityCommandFinalise(null);

        RecordingDispatchProxy<IMovementManager>.Invocation walk = Assert.Single(harness.MovementProxy.GetInvocations(nameof(IMovementManager.SetMode)));
        Assert.Equal(ModeType.Walk, walk.Arguments[0]);

        RecordingDispatchProxy<IMovementManager>.Invocation patrol = Assert.Single(
            harness.MovementProxy.GetInvocations(nameof(IMovementManager.LaunchSpline)),
            invocation => invocation.Arguments[0] is ushort);
        Assert.Equal((ushort)77, patrol.Arguments[0]);
        Assert.Equal(SplineMode.BackAndForthReverse, patrol.Arguments[1]);
        Assert.Equal(2f, patrol.Arguments[2]);
        Assert.False((bool)patrol.Arguments[3]);
    }

    [Fact]
    public void OnExitRange_WhenCombatTargetLeavesMovingRangeCheckButIsInsideLeash_DoesNotDropThreat()
    {
        CombatHarness harness = CreateHarness(
            new Vector3(35f, 0f, 0f),
            targetSelected: true,
            creaturePosition: new Vector3(-20f, 0f, 0f),
            leashPosition: Vector3.Zero);
        harness.CreatureThreat.UpdateThreat(harness.Player, 10);
        harness.CreatureProxy.Invocations.Clear();
        harness.MovementProxy.Invocations.Clear();

        harness.Script.OnExitRange(harness.Player);

        Assert.NotNull(harness.CreatureThreat.GetHostile(harness.Player.Guid));
        Assert.NotNull(harness.Player.ThreatManager.GetHostile(harness.Creature.Guid));
        Assert.Equal(harness.Player.Guid, harness.Creature.TargetGuid);
        Assert.Empty(harness.CreatureProxy.GetInvocations(nameof(ICreatureEntity.SetTarget)));
        Assert.Empty(harness.CreatureProxy.GetInvocations(nameof(ICreatureEntity.ModifyHealth)));
        Assert.Empty(harness.MovementProxy.GetInvocations(nameof(IMovementManager.LaunchPath)));
        Assert.Empty(harness.MovementProxy.GetInvocations(nameof(IMovementManager.Finalise)));
    }

    [Fact]
    public void Update_WhenCurrentTargetTemporarilyMissingFromVisibleSetButStillOnMap_DoesNotDropThreat()
    {
        bool targetVisible = true;
        CombatHarness harness = CreateHarness(
            new Vector3(35f, 0f, 0f),
            targetSelected: true,
            creaturePosition: Vector3.Zero,
            leashPosition: Vector3.Zero,
            isVisible: _ => targetVisible);
        harness.CreatureThreat.UpdateThreat(harness.Player, 10);
        harness.CreatureProxy.Invocations.Clear();
        harness.MovementProxy.Invocations.Clear();
        harness.SessionProxy.Invocations.Clear();

        targetVisible = false;
        harness.Script.OnRemoveVisibleEntity(harness.Player);
        harness.Script.Update(1d);

        Assert.NotNull(harness.CreatureThreat.GetHostile(harness.Player.Guid));
        Assert.NotNull(harness.Player.ThreatManager.GetHostile(harness.Creature.Guid));
        Assert.Equal(harness.Player.Guid, harness.Creature.TargetGuid);
        Assert.Empty(harness.CreatureProxy.GetInvocations(nameof(ICreatureEntity.ModifyHealth)));
        Assert.Empty(harness.MovementProxy.GetInvocations(nameof(IMovementManager.LaunchPath)));
        Assert.Empty(harness.MovementProxy.GetInvocations(nameof(IMovementManager.Finalise)));
        Assert.Empty(harness.SessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
    }

    [Fact]
    public void Update_WhenCurrentTargetLeavesLeashBriefly_DoesNotDropThreat()
    {
        CombatHarness harness = CreateHarness(
            new Vector3(55f, 0f, 0f),
            targetSelected: true,
            creaturePosition: Vector3.Zero,
            leashPosition: Vector3.Zero);
        harness.CreatureThreat.UpdateThreat(harness.Player, 10);
        harness.CreatureProxy.Invocations.Clear();
        harness.MovementProxy.Invocations.Clear();

        harness.Script.Update(1d);

        Assert.NotNull(harness.CreatureThreat.GetHostile(harness.Player.Guid));
        Assert.NotNull(harness.Player.ThreatManager.GetHostile(harness.Creature.Guid));
        Assert.Equal(harness.Player.Guid, harness.Creature.TargetGuid);
        Assert.Empty(harness.CreatureProxy.GetInvocations(nameof(ICreatureEntity.ModifyHealth)));
        Assert.Empty(harness.MovementProxy.GetInvocations(nameof(IMovementManager.LaunchPath)));
        Assert.Empty(harness.MovementProxy.GetInvocations(nameof(IMovementManager.Finalise)));
    }

    [Fact]
    public void Update_WhenCurrentTargetStaysOutsideLeashPastGrace_DropsThreatAndResets()
    {
        CombatHarness harness = CreateHarness(
            new Vector3(55f, 0f, 0f),
            targetSelected: true,
            creaturePosition: new Vector3(10f, 0f, 0f),
            leashPosition: Vector3.Zero);
        harness.CreatureThreat.UpdateThreat(harness.Player, 10);
        harness.CreatureProxy.Invocations.Clear();
        harness.MovementProxy.Invocations.Clear();
        harness.SessionProxy.Invocations.Clear();

        harness.Script.Update(0.25d);
        harness.Script.Update(3d);

        Assert.Null(harness.CreatureThreat.GetHostile(harness.Player.Guid));
        Assert.Null(harness.Player.ThreatManager.GetHostile(harness.Creature.Guid));
        Assert.Null(harness.Creature.TargetGuid);
        Assert.Single(harness.CreatureProxy.GetInvocations(nameof(ICreatureEntity.ModifyHealth)));
        Assert.Single(harness.SessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
        Assert.Single(harness.MovementProxy.GetInvocations(nameof(IMovementManager.LaunchPath)));
    }

    [Fact]
    public void Update_WhenDominionUltrabotFightsAtQuestMarker_DoesNotLeashBackToStaticHome()
    {
        Vector3 staticHome = new(4387.59f, -699.195f, -5131.24f);
        Vector3 questMarker = new(4438.33f, -701.694f, -5154.24f);
        CombatHarness harness = CreateHarness(
            questMarker,
            targetSelected: true,
            creatureId: 12526u,
            creaturePosition: questMarker,
            leashPosition: staticHome,
            leashRange: 15f);
        harness.CreatureThreat.UpdateThreat(harness.Player, 10);
        harness.CreatureProxy.Invocations.Clear();
        harness.MovementProxy.Invocations.Clear();
        harness.SessionProxy.Invocations.Clear();

        harness.Script.Update(0.25d);
        harness.Script.Update(3d);

        Assert.NotNull(harness.CreatureThreat.GetHostile(harness.Player.Guid));
        Assert.NotNull(harness.Player.ThreatManager.GetHostile(harness.Creature.Guid));
        Assert.Equal(harness.Player.Guid, harness.Creature.TargetGuid);
        Assert.Empty(harness.CreatureProxy.GetInvocations(nameof(ICreatureEntity.ModifyHealth)));
        Assert.Empty(harness.SessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
        Assert.Empty(harness.MovementProxy.GetInvocations(nameof(IMovementManager.LaunchPath)));
    }

    [Fact]
    public void OnThreatChange_WhenCurrentTargetOutsideLeash_DoesNotBypassLeashGrace()
    {
        CombatHarness harness = CreateHarness(
            new Vector3(55f, 0f, 0f),
            targetSelected: true,
            creaturePosition: Vector3.Zero,
            leashPosition: Vector3.Zero);
        harness.CreatureThreat.UpdateThreat(harness.Player, 10);
        harness.CreatureProxy.Invocations.Clear();
        harness.MovementProxy.Invocations.Clear();

        harness.CreatureThreat.UpdateThreat(harness.Player, 1);

        Assert.NotNull(harness.CreatureThreat.GetHostile(harness.Player.Guid));
        Assert.NotNull(harness.Player.ThreatManager.GetHostile(harness.Creature.Guid));
        Assert.Equal(harness.Player.Guid, harness.Creature.TargetGuid);
        Assert.Empty(harness.CreatureProxy.GetInvocations(nameof(ICreatureEntity.ModifyHealth)));
        Assert.Empty(harness.MovementProxy.GetInvocations(nameof(IMovementManager.LaunchPath)));
    }

    [Fact]
    public void OnThreatRemoveTarget_WhenCreatureIsDead_DoesNotResetCorpsePosition()
    {
        CombatHarness harness = CreateHarness(
            new Vector3(20f, 0f, 0f),
            targetSelected: true,
            creaturePosition: new Vector3(10f, 0f, 0f),
            leashPosition: Vector3.Zero,
            creatureIsAlive: false);

        harness.Script.OnThreatRemoveTarget(null);

        Assert.Empty(harness.CreatureProxy.GetInvocations(nameof(ICreatureEntity.SetTarget)));
        Assert.Empty(harness.CreatureProxy.GetInvocations(nameof(ICreatureEntity.ModifyHealth)));
        Assert.Empty(harness.MovementProxy.GetInvocations(nameof(IMovementManager.SetPosition)));
        Assert.Empty(harness.MovementProxy.GetInvocations(nameof(IMovementManager.LaunchPath)));
        Assert.Empty(harness.MovementProxy.GetInvocations(nameof(IMovementManager.Finalise)));
    }

    private static CombatHarness CreateHarness(
        Vector3 playerPosition,
        bool targetSelected = false,
        IReadOnlyCollection<IUnitEntity> extraInRange = null,
        uint creatureId = 73464u,
        bool includePlayer = true,
        Func<IUnitEntity, bool> canAttack = null,
        Vector3? creaturePosition = null,
        Vector3? leashPosition = null,
        uint? initialTargetGuid = null,
        EntitySplineModel patrolSpline = null,
        ICombatProfileProvider profileProvider = null,
        Func<IUnitEntity, bool> isVisible = null,
        float creatureHitRadius = 0f,
        float playerHitRadius = 0f,
        IReadOnlyList<uint> autoAttacks = null,
        Spell4Entry[] spell4Entries = null,
        bool includeSelfInRange = false,
        bool armRangeCheck = true,
        Disposition dispositionToPlayer = Disposition.Hostile,
        bool enableDefaultProfileFallback = false,
        bool creatureIsAlive = true,
        float leashRange = 50f,
        uint? summonerGuid = null,
        uint? playerTargetGuid = null)
    {
        ICreatureEntity creature = RecordingDispatchProxy<ICreatureEntity>.Create(out RecordingDispatchProxy<ICreatureEntity> creatureProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        IMovementManager movementManager = RecordingDispatchProxy<IMovementManager>.Create(out RecordingDispatchProxy<IMovementManager> movementProxy);
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out RecordingDispatchProxy<IBaseMap> mapProxy);
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out RecordingDispatchProxy<IGameSession> sessionProxy);

        var creatureThreat = new ThreatManager(creature);
        var playerThreat = new ThreatManager(player);
        TestCombatAI script = null;

        creatureProxy.SetProperty(nameof(ICreatureEntity.Guid), 100u);
        creatureProxy.SetProperty(nameof(ICreatureEntity.CreatureId), creatureId);
        creatureProxy.SetProperty(nameof(ICreatureEntity.IsAlive), creatureIsAlive);
        creatureProxy.SetProperty(nameof(ICreatureEntity.InCombat), false);
        creatureProxy.SetProperty(nameof(ICreatureEntity.Position), creaturePosition ?? Vector3.Zero);
        creatureProxy.SetProperty(nameof(ICreatureEntity.LeashPosition), leashPosition ?? Vector3.Zero);
        creatureProxy.SetProperty(nameof(ICreatureEntity.LeashRange), leashRange);
        creatureProxy.SetProperty(nameof(ICreatureEntity.HitRadius), creatureHitRadius);
        creatureProxy.SetProperty(nameof(ICreatureEntity.Faction1), Faction.Dominion);
        creatureProxy.SetProperty(nameof(ICreatureEntity.Faction2), Faction.None);
        creatureProxy.SetProperty(nameof(ICreatureEntity.Map), map);
        creatureProxy.SetProperty(nameof(ICreatureEntity.Spline), patrolSpline);
        creatureProxy.SetProperty(nameof(ICreatureEntity.SummonerGuid), summonerGuid);
        if (targetSelected)
            creatureProxy.SetProperty(nameof(ICreatureEntity.TargetGuid), initialTargetGuid ?? 200u);

        creatureProxy.SetProperty(nameof(ICreatureEntity.ThreatManager), creatureThreat);
        creatureProxy.SetProperty(nameof(ICreatureEntity.MovementManager), movementManager);
        creatureProxy.SetMethodHandler(nameof(ICreatureEntity.CanAttack), args => canAttack?.Invoke((IUnitEntity)args[0]) ?? ReferenceEquals(args[0], player));
        creatureProxy.SetMethodHandler(nameof(ICreatureEntity.GetDispositionTo), _ => dispositionToPlayer);
        List<IUnitEntity> GetVisibleUnits()
        {
            IEnumerable<IUnitEntity> units = includePlayer ? [player] : [];
            if (includeSelfInRange)
                units = units.Concat([creature]);

            if (extraInRange != null)
                units = units.Concat(extraInRange);

            return units.ToList();
        }

        creatureProxy.SetMethodHandler(nameof(ICreatureEntity.GetInRange), _ => GetVisibleUnits().ToArray());
        creatureProxy.SetMethodHandler(nameof(ICreatureEntity.GetVisible), args =>
        {
            IUnitEntity unit = GetVisibleUnits().FirstOrDefault(unit => unit.Guid == (uint)args[0]);
            if (unit == null)
                return null;

            return isVisible?.Invoke(unit) == false ? null : unit;
        });
        creatureProxy.SetMethodHandler(nameof(ICreatureEntity.GetPropertyValue), _ => 1f);
        creatureProxy.SetMethodHandler(nameof(ICreatureEntity.SetTarget), args =>
        {
            uint? targetGuid = args[0] switch
            {
                uint unitId         => unitId,
                IWorldEntity target => target?.Guid,
                _                   => null
            };
            creatureProxy.SetProperty(nameof(ICreatureEntity.TargetGuid), targetGuid);
            return null;
        });
        creatureProxy.SetMethodHandler(nameof(ICreatureEntity.OnThreatAddTarget), args =>
        {
            script?.OnThreatAddTarget((IHostileEntity)args[0]);
            return null;
        });
        creatureProxy.SetMethodHandler(nameof(ICreatureEntity.OnThreatRemoveTarget), args =>
        {
            script?.OnThreatRemoveTarget((IHostileEntity)args[0]);
            return null;
        });
        creatureProxy.SetMethodHandler(nameof(ICreatureEntity.OnThreatChange), args =>
        {
            script?.OnThreatChange((IHostileEntity)args[0]);
            return null;
        });

        mapProxy.SetMethodHandler(nameof(IBaseMap.GetEntity), args =>
            GetVisibleUnits().FirstOrDefault(unit => unit.Guid == (uint)args[0]));

        playerProxy.SetProperty(nameof(IPlayer.Guid), 200u);
        playerProxy.SetProperty(nameof(IPlayer.Position), playerPosition);
        playerProxy.SetProperty(nameof(IPlayer.HitRadius), playerHitRadius);
        playerProxy.SetProperty(nameof(IPlayer.Faction1), Faction.Exile);
        playerProxy.SetProperty(nameof(IPlayer.Faction2), Faction.None);
        playerProxy.SetProperty(nameof(IPlayer.ThreatManager), playerThreat);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);
        playerProxy.SetProperty(nameof(IPlayer.TargetGuid), playerTargetGuid);

        script = new TestCombatAI(CreateSpellParametersFactory(), CreateGameTableManager(spell4Entries), profileProvider, autoAttacks, enableDefaultProfileFallback);
        script.OnLoad(creature);
        if (armRangeCheck)
            script.OnAddToMap(map);

        return new CombatHarness(script, creature, creatureProxy, creatureThreat, player, playerProxy, sessionProxy, movementProxy);
    }

    private static ICreatureEntity CreateAlly(
        uint attackableTargetGuid,
        Vector3 position,
        Faction faction,
        out IThreatManager threat,
        bool canSeeAttackableTarget = true)
    {
        ICreatureEntity ally = RecordingDispatchProxy<ICreatureEntity>.Create(out RecordingDispatchProxy<ICreatureEntity> allyProxy);
        threat = new ThreatManager(ally);

        allyProxy.SetProperty(nameof(ICreatureEntity.Guid), 300u);
        allyProxy.SetProperty(nameof(ICreatureEntity.CreatureId), 73465u);
        allyProxy.SetProperty(nameof(ICreatureEntity.IsAlive), true);
        allyProxy.SetProperty(nameof(ICreatureEntity.InCombat), false);
        allyProxy.SetProperty(nameof(ICreatureEntity.Position), position);
        allyProxy.SetProperty(nameof(ICreatureEntity.LeashPosition), Vector3.Zero);
        allyProxy.SetProperty(nameof(ICreatureEntity.LeashRange), 50f);
        allyProxy.SetProperty(nameof(ICreatureEntity.Faction1), faction);
        allyProxy.SetProperty(nameof(ICreatureEntity.Faction2), Faction.None);
        allyProxy.SetProperty(nameof(ICreatureEntity.ThreatManager), threat);
        allyProxy.SetMethodHandler(nameof(ICreatureEntity.CanAttack), args => args[0] is IUnitEntity unit && unit.Guid == attackableTargetGuid);
        allyProxy.SetMethodHandler(nameof(ICreatureEntity.GetVisible), args =>
        {
            if (!canSeeAttackableTarget || (uint)args[0] != attackableTargetGuid)
                return null;

            IUnitEntity visibleTarget = RecordingDispatchProxy<IUnitEntity>.Create(out RecordingDispatchProxy<IUnitEntity> visibleTargetProxy);
            visibleTargetProxy.SetProperty(nameof(IUnitEntity.Guid), attackableTargetGuid);
            return visibleTarget;
        });

        return ally;
    }

    private static ICreatureEntity CreateHostileCreature(uint guid, Vector3 position, uint? targetGuid = null)
    {
        ICreatureEntity hostileCreature = RecordingDispatchProxy<ICreatureEntity>.Create(out RecordingDispatchProxy<ICreatureEntity> hostileProxy);
        var threat = new ThreatManager(hostileCreature);
        hostileProxy.SetProperty(nameof(ICreatureEntity.Guid), guid);
        hostileProxy.SetProperty(nameof(ICreatureEntity.Position), position);
        hostileProxy.SetProperty(nameof(ICreatureEntity.IsAlive), true);
        hostileProxy.SetProperty(nameof(ICreatureEntity.TargetGuid), targetGuid);
        hostileProxy.SetProperty(nameof(ICreatureEntity.ThreatManager), threat);
        return hostileCreature;
    }

    private static IFactory<ISpellParameters> CreateSpellParametersFactory()
    {
        IFactory<ISpellParameters> factory = RecordingDispatchProxy<IFactory<ISpellParameters>>.Create(out RecordingDispatchProxy<IFactory<ISpellParameters>> factoryProxy);
        factoryProxy.SetMethodHandler(nameof(IFactory<ISpellParameters>.Resolve), _ =>
            RecordingDispatchProxy<ISpellParameters>.Create(out RecordingDispatchProxy<ISpellParameters> parametersProxy));

        return factory;
    }

    private static IGameTableManager CreateGameTableManager(params Spell4Entry[] spell4Entries)
    {
        IGameTableManager gameTableManager = RecordingDispatchProxy<IGameTableManager>.Create(out RecordingDispatchProxy<IGameTableManager> gameTableManagerProxy);
        if (spell4Entries is { Length: > 0 })
            gameTableManagerProxy.SetProperty(nameof(IGameTableManager.Spell4), CreateGameTable(spell4Entries));

        return gameTableManager;
    }

    private static IGameTableManager CreateCombatProfileGameTableManager(
        Creature2Entry[] creatureEntries = null,
        Creature2ActionEntry[] actionEntries = null,
        Spell4Entry[] spell4Entries = null)
    {
        IGameTableManager gameTableManager = RecordingDispatchProxy<IGameTableManager>.Create(out RecordingDispatchProxy<IGameTableManager> gameTableManagerProxy);
        if (creatureEntries != null)
            gameTableManagerProxy.SetProperty(nameof(IGameTableManager.Creature2), CreateGameTable(creatureEntries));
        if (actionEntries != null)
            gameTableManagerProxy.SetProperty(nameof(IGameTableManager.Creature2Action), CreateGameTable(actionEntries));
        if (spell4Entries != null)
            gameTableManagerProxy.SetProperty(nameof(IGameTableManager.Spell4), CreateGameTable(spell4Entries));

        return gameTableManager;
    }

    private static GameTable<T> CreateGameTable<T>(params T[] entries) where T : class, new()
    {
        var table = (GameTable<T>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<T>));
        uint maxId = entries
            .Select(entry => (uint)typeof(T).GetField("Id")!.GetValue(entry)!)
            .DefaultIfEmpty()
            .Max();

        int[] lookup = Enumerable.Repeat(-1, checked((int)maxId + 1)).ToArray();
        for (int index = 0; index < entries.Length; index++)
        {
            uint id = (uint)typeof(T).GetField("Id")!.GetValue(entries[index])!;
            lookup[id] = index;
        }

        typeof(GameTable<T>)
            .GetProperty(nameof(GameTable<T>.Entries), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?.SetValue(table, entries);
        typeof(GameTable<T>)
            .GetField("lookup", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(table, lookup);
        typeof(GameTable<T>)
            .GetField("header", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(table, new GameTableHeader { MaxId = (ulong)lookup.Length });

        return table;
    }

    private sealed class TestCombatAI : CombatAI
    {
        private readonly IReadOnlyList<uint> testAutoAttacks;
        private readonly bool enableDefaultProfileFallback;

        protected override bool EnablesDefaultCombatProfile => enableDefaultProfileFallback;

        public TestCombatAI(IFactory<ISpellParameters> spellParametersFactory, IGameTableManager gameTableManager, ICombatProfileProvider profileProvider = null, IReadOnlyList<uint> testAutoAttacks = null, bool enableDefaultProfileFallback = false)
            : base(spellParametersFactory, gameTableManager, profileProvider)
        {
            this.testAutoAttacks = testAutoAttacks;
            this.enableDefaultProfileFallback = enableDefaultProfileFallback;
        }

        public override void OnLoad(ICreatureEntity owner)
        {
            base.OnLoad(owner);
            autoAttacks = testAutoAttacks?.ToList() ?? [];
        }
    }

    private sealed class FixedCombatProfileProvider : ICombatProfileProvider
    {
        private readonly CombatProfile profile;

        public FixedCombatProfileProvider(CombatProfile profile)
        {
            this.profile = profile;
        }

        public CombatProfile GetProfile(ICreatureEntity creature)
        {
            return profile;
        }
    }

    private sealed record CombatHarness(
        CombatAI Script,
        ICreatureEntity Creature,
        RecordingDispatchProxy<ICreatureEntity> CreatureProxy,
        IThreatManager CreatureThreat,
        IPlayer Player,
        RecordingDispatchProxy<IPlayer> PlayerProxy,
        RecordingDispatchProxy<IGameSession> SessionProxy,
        RecordingDispatchProxy<IMovementManager> MovementProxy);
}
