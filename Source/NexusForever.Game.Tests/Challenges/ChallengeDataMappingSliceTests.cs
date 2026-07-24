using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Challenges;
using NexusForever.Game.Static.Challenges;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Configuration.Model;
using NexusForever.GameTable.Model;
using NexusForever.Network.Message;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Challenges;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.Game.Tests.Challenges;

public class ChallengeDataMappingSliceTests
{
    private const ushort ExplodingIcetailsChallengeId = 182;
    private const uint ExplodingIcetailsTargetGroupId = 28780u;
    private const uint EnragedRingtailCreature2Id = 20775u;
    private const uint ExplodingIcetailsTier0Id = 338u;

    private const ushort FalkrinWarBannersChallengeId = 309;
    private const uint FalkrinWarBannersTargetGroupId = 6675u;
    private const uint FalkrinWarBannerCreature2Id = 25640u;
    private const uint FalkrinWarBannersTier0Id = 670u;

    private const ushort ExtraterrestrialExterminatorChallengeId = 409;
    private const uint ExtraterrestrialExterminatorTargetGroupId = 3341u;
    private const uint CobaltTitanCreature2Id = 50321u;
    private const uint ExtraterrestrialExterminatorTier0Id = 928u;
    private const uint FarsideRewardTrackId = 24u;
    private const uint FarsideRewardTrackRewardId = 475u;
    private const uint JourneymanCraftersBagItem2Id = 84760u;

    private const uint AutoActivateOnProgressFlag = 0x20u;
    private const ushort SkeechSlayerChallengeId = 103;
    private const uint SkeechSlayerTargetGroupId = 8851u;
    private const uint SkeechSlayerChildTargetGroupId = 2309u;
    private const uint SkeechSlayerRareChildTargetGroupId = 8852u;
    private const uint SkeechScratcherCreature2Id = 11965u;
    private const uint MappedSkeechScratcherCreature2Id = 11910u;
    private const uint SkeechSlayerWorldZoneRestrictionId = 604u;
    private const uint SkeechSlayerTier0Id = 150u;

    private const ushort RootbruteSlayerChallengeId = 105;
    private const uint RootbruteSlayerTargetGroupId = 1852u;
    private const uint RootbruteGrimsporeCreature2Id = 12212u;
    private const uint RootbruteDeathcapCreature2Id = 12213u;
    private const uint RootbruteSlayerWorldZoneId = 35u;
    private const uint RootbruteSlayerTier0Id = 154u;

    private const ushort XenobiteEggSmasherChallengeId = 107;
    private const uint XenobiteEggSmasherTargetGroupId = 1854u;
    private const uint XenobiteEggCreature2Id = 12836u;
    private const uint XenobiteEggSmasherWorldZoneRestrictionId = 609u;
    private const uint XenobiteEggSmasherTier0Id = 158u;
    private const uint XenobiteEggSmasherChallengeFlags = AutoActivateOnProgressFlag | 0x80u;

    private const uint NorthernWildsRewardTrackId = 16u;
    private const uint NorthernWildsBronzeRewardTrackRewardId = 320u;
    private const uint SmallBagItem2Id = 12045u;
    private const uint NorthernWildsWorldId = 426u;
    private const uint NorthernWildsRuntimeWorldZoneId = 1u;
    private const uint NorthernWildsRegionalWorldZoneId = 590u;
    private const uint NorthernWildsParentAreaWorldZoneId = 597u;
    private const uint NorthernWildsRootbruteSpawnWorldZoneId = 949u;

    [Fact]
    public void MappedActivationTarget_AdvancesAbilityChallengeThroughTargetGroup()
    {
        GameTableManager gameTables = CreateGameTables(
            new ChallengeEntry
            {
                Id                      = ExplodingIcetailsChallengeId,
                ChallengeTypeEnum       = (uint)ChallengeType.Ability,
                Target                  = ExplodingIcetailsTargetGroupId,
                ChallengeTierId00       = ExplodingIcetailsTier0Id,
                TargetGroupIdRewardPane = ExplodingIcetailsTargetGroupId
            },
            [
                new ChallengeTierEntry
                {
                    Id    = ExplodingIcetailsTier0Id,
                    Count = 1u
                }
            ],
            [
                new TargetGroupEntry
                {
                    Id          = ExplodingIcetailsTargetGroupId,
                    Type        = (uint)TargetGroupType.CreatureIdListGroup,
                    DataEntries = [EnragedRingtailCreature2Id, 20779u, 20776u, 61864u, 0u, 0u, 0u]
                }
            ]);
        ChallengeManager manager = CreateManager(gameTables, out IPlayer player, out RecordingDispatchProxy<IGameSession> sessionProxy, out _, out _);

        manager.HandleChoice(ExplodingIcetailsChallengeId, ChallengeChoice.Activate);
        ChallengeActivationHooks.OnTargetActivated(player, EnragedRingtailCreature2Id);

        ServerChallengeResult completed = GetMessages<ServerChallengeResult>(sessionProxy)
            .Last(result => result.Result == ChallengeResult.Completed);
        Assert.Equal(ExplodingIcetailsChallengeId, completed.ChallengeId);
    }

    [Fact]
    public void MappedChecklistActivationTarget_AdvancesChecklistActivateChallengeThroughTargetGroup()
    {
        GameTableManager gameTables = CreateGameTables(
            new ChallengeEntry
            {
                Id                      = FalkrinWarBannersChallengeId,
                ChallengeTypeEnum       = (uint)ChallengeType.ChecklistActivate,
                Target                  = FalkrinWarBannersTargetGroupId,
                ChallengeTierId00       = FalkrinWarBannersTier0Id,
                TargetGroupIdRewardPane = FalkrinWarBannersTargetGroupId
            },
            [
                new ChallengeTierEntry
                {
                    Id    = FalkrinWarBannersTier0Id,
                    Count = 6u
                }
            ],
            [
                new TargetGroupEntry
                {
                    Id          = FalkrinWarBannersTargetGroupId,
                    Type        = (uint)TargetGroupType.CreatureIdGroup,
                    DataEntries = [FalkrinWarBannerCreature2Id, 0u, 0u, 0u, 0u, 0u, 0u]
                }
            ]);
        ChallengeManager manager = CreateManager(gameTables, out IPlayer player, out RecordingDispatchProxy<IGameSession> sessionProxy, out _, out _);

        manager.HandleChoice(FalkrinWarBannersChallengeId, ChallengeChoice.Activate);
        ChallengeActivationHooks.OnTargetActivated(player, FalkrinWarBannerCreature2Id);

        ServerChallengeUpdate.Challenge row = Assert.Single(GetMessages<ServerChallengeUpdate>(sessionProxy).Last().ActiveChallenges);
        Assert.Equal(1u, row.CurrentCount);
        Assert.Equal(6u, row.GoalCount);
        Assert.True(row.Activated);
    }

    [Fact]
    public void MappedKillTarget_AdvancesCombatChallengeThroughTargetGroup()
    {
        GameTableManager gameTables = CreateGameTables(
            new ChallengeEntry
            {
                Id                      = ExtraterrestrialExterminatorChallengeId,
                ChallengeTypeEnum       = (uint)ChallengeType.Combat,
                Target                  = ExtraterrestrialExterminatorTargetGroupId,
                ChallengeTierId00       = ExtraterrestrialExterminatorTier0Id,
                TargetGroupIdRewardPane = ExtraterrestrialExterminatorTargetGroupId
            },
            [
                new ChallengeTierEntry
                {
                    Id    = ExtraterrestrialExterminatorTier0Id,
                    Count = 15u
                }
            ],
            [
                new TargetGroupEntry
                {
                    Id          = ExtraterrestrialExterminatorTargetGroupId,
                    Type        = (uint)TargetGroupType.CreatureIdListGroup,
                    DataEntries = [CobaltTitanCreature2Id, 44372u, 44377u, 28945u, 27569u, 27615u, 0u]
                }
            ]);
        ChallengeManager manager = CreateManager(gameTables, out IPlayer player, out RecordingDispatchProxy<IGameSession> sessionProxy, out _, out _);

        manager.HandleChoice(ExtraterrestrialExterminatorChallengeId, ChallengeChoice.Activate);
        ChallengeCombatHooks.OnCreatureKilled(player, CobaltTitanCreature2Id);

        ServerChallengeUpdate.Challenge row = Assert.Single(GetMessages<ServerChallengeUpdate>(sessionProxy).Last().ActiveChallenges);
        Assert.Equal(1u, row.CurrentCount);
        Assert.Equal(15u, row.GoalCount);
        Assert.True(row.Activated);
    }

    [Fact]
    public void RootbruteSlayer_AutoActivatesAndCreditsFirstRootbruteKill()
    {
        GameTableManager gameTables = CreateGameTables(
            new ChallengeEntry
            {
                Id                      = RootbruteSlayerChallengeId,
                ChallengeTypeEnum       = (uint)ChallengeType.Combat,
                Target                  = RootbruteSlayerTargetGroupId,
                ChallengeFlags          = AutoActivateOnProgressFlag,
                WorldZoneIdRestriction  = RootbruteSlayerWorldZoneId,
                WorldZoneId             = RootbruteSlayerWorldZoneId,
                ChallengeTierId00       = RootbruteSlayerTier0Id
            },
            [
                new ChallengeTierEntry
                {
                    Id    = RootbruteSlayerTier0Id,
                    Count = 5u
                }
            ],
            [
                new TargetGroupEntry
                {
                    Id          = RootbruteSlayerTargetGroupId,
                    Type        = (uint)TargetGroupType.CreatureIdGroup,
                    DataEntries = [RootbruteGrimsporeCreature2Id, 13549u, RootbruteDeathcapCreature2Id, 13118u, 33848u, 0u, 0u]
                }
            ]);
        ChallengeManager manager = CreateManager(
            gameTables,
            out IPlayer player,
            out RecordingDispatchProxy<IGameSession> sessionProxy,
            out _,
            out _,
            new WorldZoneEntry
            {
                Id = RootbruteSlayerWorldZoneId
            });

        ChallengeCombatHooks.OnCreatureKilled(player, RootbruteGrimsporeCreature2Id);

        List<IWritable> sentMessages = sessionProxy
            .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(invocation => invocation.Arguments[0])
            .OfType<IWritable>()
            .ToList();
        int activateIndex = sentMessages.FindIndex(message =>
            message is ServerChallengeResult result && result.Result == ChallengeResult.Activate);
        Assert.True(activateIndex > 0);
        ServerChallengeUpdate activationUpdate = Assert.IsType<ServerChallengeUpdate>(sentMessages[activateIndex - 1]);
        ServerChallengeUpdate.Challenge activationRow = Assert.Single(activationUpdate.ActiveChallenges);
        Assert.Equal(1u, activationRow.CurrentCount);
        Assert.Equal(20u, activationRow.ObjectiveCompletion);

        ServerChallengeResult activate = GetMessages<ServerChallengeResult>(sessionProxy)
            .Single(result => result.Result == ChallengeResult.Activate);
        Assert.Equal(RootbruteSlayerChallengeId, activate.ChallengeId);

        ServerChallengeUpdate.Challenge row = Assert.Single(GetMessages<ServerChallengeUpdate>(sessionProxy).Last().ActiveChallenges);
        Assert.Equal((uint)RootbruteSlayerChallengeId, row.ChallengeId);
        Assert.Equal(ChallengeType.Combat, row.Type);
        Assert.Equal(RootbruteSlayerTargetGroupId, row.TargetGroupId);
        Assert.Equal(1u, row.CurrentCount);
        Assert.Equal(5u, row.GoalCount);
        Assert.Equal(20u, row.ObjectiveCompletion);
        Assert.True(row.Activated);
        Assert.Equal(300_000u, row.TimeActivatedDt);
        Assert.Equal(300_000u, row.TimeTotalActive);
    }

    [Fact]
    public void RootbruteSlayer_RepeatAutoActivationStartsFreshProgress()
    {
        GameTableManager gameTables = CreateRootbruteSlayerGameTables(
            [
                new WorldZoneEntry
                {
                    Id = RootbruteSlayerWorldZoneId
                }
            ]);
        ChallengeManager manager = CreateManager(
            gameTables,
            out IPlayer player,
            out RecordingDispatchProxy<IGameSession> sessionProxy,
            out _,
            out _,
            new WorldZoneEntry
            {
                Id = RootbruteSlayerWorldZoneId
            });

        for (int i = 0; i < 5; i++)
            ChallengeCombatHooks.OnCreatureKilled(player, RootbruteGrimsporeCreature2Id);

        ServerChallengeResult completed = GetMessages<ServerChallengeResult>(sessionProxy)
            .Single(result => result.Result == ChallengeResult.Completed);
        Assert.Equal(RootbruteSlayerChallengeId, completed.ChallengeId);

        ChallengeCombatHooks.OnCreatureKilled(player, RootbruteGrimsporeCreature2Id);

        ServerChallengeUpdate.Challenge row = Assert.Single(GetMessages<ServerChallengeUpdate>(sessionProxy).Last().ActiveChallenges);
        Assert.Equal((uint)RootbruteSlayerChallengeId, row.ChallengeId);
        Assert.True(row.Activated);
        Assert.Equal(1u, row.CurrentCount);
        Assert.Equal(5u, row.GoalCount);
        Assert.Equal(1u, row.CompletionCount);
        Assert.Equal(300_000u, row.TimeActivatedDt);
        Assert.Equal(300_000u, row.TimeTotalActive);
        Assert.Single(GetMessages<ServerChallengeResult>(sessionProxy), result => result.Result == ChallengeResult.Completed);
    }

    [Fact]
    public void SkeechSlayer_AutoActivatesAndCreditsNestedTargetGroupKillInColdburrow()
    {
        GameTableManager gameTables = CreateGameTables(
            new ChallengeEntry
            {
                Id                      = SkeechSlayerChallengeId,
                ChallengeTypeEnum       = (uint)ChallengeType.Combat,
                Target                  = SkeechSlayerTargetGroupId,
                ChallengeFlags          = AutoActivateOnProgressFlag,
                WorldZoneIdRestriction  = SkeechSlayerWorldZoneRestrictionId,
                WorldZoneId             = RootbruteSlayerWorldZoneId,
                ChallengeTierId00       = SkeechSlayerTier0Id
            },
            [
                new ChallengeTierEntry
                {
                    Id    = SkeechSlayerTier0Id,
                    Count = 5u
                }
            ],
            [
                new TargetGroupEntry
                {
                    Id          = SkeechSlayerTargetGroupId,
                    Type        = (uint)TargetGroupType.OtherTargetGroupCreatures,
                    DataEntries = [SkeechSlayerChildTargetGroupId, SkeechSlayerRareChildTargetGroupId, 0u, 0u, 0u, 0u, 0u]
                },
                new TargetGroupEntry
                {
                    Id          = SkeechSlayerChildTargetGroupId,
                    Type        = (uint)TargetGroupType.CreatureIdListGroup,
                    DataEntries = [20607u, SkeechScratcherCreature2Id, 11580u, 20605u, 20608u, 11576u, 15063u]
                },
                new TargetGroupEntry
                {
                    Id          = SkeechSlayerRareChildTargetGroupId,
                    Type        = (uint)TargetGroupType.CreatureIdListGroup,
                    DataEntries = [48101u, 48100u, 48102u, 17185u, 17171u, 0u, 0u]
                }
            ]);
        ChallengeManager manager = CreateManager(
            gameTables,
            out IPlayer player,
            out RecordingDispatchProxy<IGameSession> sessionProxy,
            out _,
            out _,
            new WorldZoneEntry
            {
                Id = SkeechSlayerWorldZoneRestrictionId
            });

        ChallengeCombatHooks.OnCreatureKilled(player, SkeechScratcherCreature2Id);

        ServerChallengeResult activate = GetMessages<ServerChallengeResult>(sessionProxy)
            .Single(result => result.Result == ChallengeResult.Activate);
        Assert.Equal(SkeechSlayerChallengeId, activate.ChallengeId);

        ServerChallengeUpdate.Challenge row = Assert.Single(GetMessages<ServerChallengeUpdate>(sessionProxy).Last().ActiveChallenges);
        Assert.Equal((uint)SkeechSlayerChallengeId, row.ChallengeId);
        Assert.Equal(ChallengeType.Combat, row.Type);
        Assert.Equal(1u, row.CurrentCount);
        Assert.Equal(5u, row.GoalCount);
        Assert.True(row.Activated);
    }

    [Fact]
    public void XenobiteEggSmasher_AutoActivatesAndCreditsEggDestructionInColdburrowCavern()
    {
        GameTableManager gameTables = CreateGameTables(
            new ChallengeEntry
            {
                Id                      = XenobiteEggSmasherChallengeId,
                ChallengeTypeEnum       = (uint)ChallengeType.Combat,
                Target                  = XenobiteEggSmasherTargetGroupId,
                ChallengeFlags          = XenobiteEggSmasherChallengeFlags,
                WorldZoneIdRestriction  = XenobiteEggSmasherWorldZoneRestrictionId,
                WorldZoneId             = RootbruteSlayerWorldZoneId,
                ChallengeTierId00       = XenobiteEggSmasherTier0Id
            },
            [
                new ChallengeTierEntry
                {
                    Id    = XenobiteEggSmasherTier0Id,
                    Count = 10u
                }
            ],
            [
                new TargetGroupEntry
                {
                    Id          = XenobiteEggSmasherTargetGroupId,
                    Type        = (uint)TargetGroupType.CreatureIdGroup,
                    DataEntries = [XenobiteEggCreature2Id, 0u, 0u, 0u, 0u, 0u, 0u]
                }
            ]);
        ChallengeManager manager = CreateManager(
            gameTables,
            out IPlayer player,
            out RecordingDispatchProxy<IGameSession> sessionProxy,
            out _,
            out _,
            new WorldZoneEntry
            {
                Id = XenobiteEggSmasherWorldZoneRestrictionId
            });

        ChallengeCombatHooks.OnCreatureKilled(player, XenobiteEggCreature2Id);

        ServerChallengeResult activate = GetMessages<ServerChallengeResult>(sessionProxy)
            .Single(result => result.Result == ChallengeResult.Activate);
        Assert.Equal(XenobiteEggSmasherChallengeId, activate.ChallengeId);

        ServerChallengeUpdate.Challenge row = Assert.Single(GetMessages<ServerChallengeUpdate>(sessionProxy).Last().ActiveChallenges);
        Assert.Equal((uint)XenobiteEggSmasherChallengeId, row.ChallengeId);
        Assert.Equal(ChallengeType.Combat, row.Type);
        Assert.Equal(1u, row.CurrentCount);
        Assert.Equal(10u, row.GoalCount);
        Assert.True(row.Activated);
    }

    [Fact]
    public void RootbruteSlayer_AutoActivatesAndCreditsKillFromDescendantNorthernWildsZone()
    {
        WorldZoneEntry[] worldZones = CreateNorthernWildsWorldZones();
        GameTableManager gameTables = CreateRootbruteSlayerGameTables(worldZones);
        ChallengeManager manager = CreateManager(
            gameTables,
            out IPlayer player,
            out RecordingDispatchProxy<IGameSession> sessionProxy,
            out _,
            out _,
            worldZones.Single(zone => zone.Id == NorthernWildsRootbruteSpawnWorldZoneId));

        ChallengeCombatHooks.OnCreatureKilled(player, RootbruteGrimsporeCreature2Id);

        ServerChallengeResult activate = GetMessages<ServerChallengeResult>(sessionProxy)
            .Single(result => result.Result == ChallengeResult.Activate);
        Assert.Equal(RootbruteSlayerChallengeId, activate.ChallengeId);

        ServerChallengeUpdate.Challenge row = Assert.Single(GetMessages<ServerChallengeUpdate>(sessionProxy).Last().ActiveChallenges);
        Assert.Equal((uint)RootbruteSlayerChallengeId, row.ChallengeId);
        Assert.Equal(1u, row.CurrentCount);
        Assert.Equal(5u, row.GoalCount);
        Assert.True(row.Activated);
    }

    [Fact]
    public void SkeechSlayer_AutoActivatesAndCreditsMappedRuntimeSkeechCreature()
    {
        WorldZoneEntry[] worldZones = CreateNorthernWildsWorldZones();
        GameTableManager gameTables = CreateSkeechSlayerGameTables(worldZones);
        ChallengeManager manager = CreateManager(
            gameTables,
            out IPlayer player,
            out RecordingDispatchProxy<IGameSession> sessionProxy,
            out _,
            out _,
            worldZones.Single(zone => zone.Id == SkeechSlayerWorldZoneRestrictionId));

        ChallengeCombatHooks.OnCreatureKilled(player, MappedSkeechScratcherCreature2Id);

        ServerChallengeResult activate = GetMessages<ServerChallengeResult>(sessionProxy)
            .Single(result => result.Result == ChallengeResult.Activate);
        Assert.Equal(SkeechSlayerChallengeId, activate.ChallengeId);

        ServerChallengeUpdate.Challenge row = Assert.Single(GetMessages<ServerChallengeUpdate>(sessionProxy).Last().ActiveChallenges);
        Assert.Equal((uint)SkeechSlayerChallengeId, row.ChallengeId);
        Assert.Equal(1u, row.CurrentCount);
        Assert.Equal(5u, row.GoalCount);
        Assert.True(row.Activated);
    }

    [Fact]
    public void XenobiteEggSmasher_AutoActivatesAndCreditsEggFromParentAreaZone()
    {
        WorldZoneEntry[] worldZones = CreateNorthernWildsWorldZones();
        GameTableManager gameTables = CreateXenobiteEggSmasherGameTables(worldZones);
        ChallengeManager manager = CreateManager(
            gameTables,
            out IPlayer player,
            out RecordingDispatchProxy<IGameSession> sessionProxy,
            out _,
            out _,
            worldZones.Single(zone => zone.Id == NorthernWildsParentAreaWorldZoneId));

        ChallengeCombatHooks.OnCreatureKilled(player, XenobiteEggCreature2Id);

        ServerChallengeResult activate = GetMessages<ServerChallengeResult>(sessionProxy)
            .Single(result => result.Result == ChallengeResult.Activate);
        Assert.Equal(XenobiteEggSmasherChallengeId, activate.ChallengeId);

        ServerChallengeUpdate.Challenge row = Assert.Single(GetMessages<ServerChallengeUpdate>(sessionProxy).Last().ActiveChallenges);
        Assert.Equal((uint)XenobiteEggSmasherChallengeId, row.ChallengeId);
        Assert.Equal(1u, row.CurrentCount);
        Assert.Equal(10u, row.GoalCount);
        Assert.True(row.Activated);
    }

    [Fact]
    public void SkeechSlayer_ManualActivateFromReviewedNorthernWildsRuntimeZoneStartsTimer()
    {
        WorldZoneEntry[] worldZones = CreateNorthernWildsWorldZones();
        GameTableManager gameTables = CreateSkeechSlayerGameTables(worldZones);
        ChallengeManager manager = CreateManager(
            gameTables,
            out _,
            out RecordingDispatchProxy<IGameSession> sessionProxy,
            out _,
            out _,
            new WorldZoneEntry
            {
                Id = NorthernWildsRuntimeWorldZoneId
            },
            NorthernWildsWorldId);

        manager.HandleChoice(SkeechSlayerChallengeId, ChallengeChoice.Activate);

        ServerChallengeResult result = Assert.Single(GetMessages<ServerChallengeResult>(sessionProxy));
        Assert.Equal(SkeechSlayerChallengeId, result.ChallengeId);
        Assert.Equal(ChallengeResult.Activate, result.Result);

        ServerChallengeUpdate.Challenge row = Assert.Single(GetMessages<ServerChallengeUpdate>(sessionProxy).Last().ActiveChallenges);
        Assert.Equal((uint)SkeechSlayerChallengeId, row.ChallengeId);
        Assert.True(row.Activated);
        Assert.Equal(300_000u, row.TimeActivatedDt);
        Assert.Equal(300_000u, row.TimeTotalActive);
    }

    [Fact]
    public void SkeechSlayer_ManualActivateFromUnreviewedRuntimeWorldKeepsAreaRestriction()
    {
        WorldZoneEntry[] worldZones = CreateNorthernWildsWorldZones();
        GameTableManager gameTables = CreateSkeechSlayerGameTables(worldZones);
        ChallengeManager manager = CreateManager(
            gameTables,
            out _,
            out RecordingDispatchProxy<IGameSession> sessionProxy,
            out _,
            out _,
            new WorldZoneEntry
            {
                Id = NorthernWildsRuntimeWorldZoneId
            },
            51u);

        manager.HandleChoice(SkeechSlayerChallengeId, ChallengeChoice.Activate);

        ServerChallengeResult result = Assert.Single(GetMessages<ServerChallengeResult>(sessionProxy));
        Assert.Equal(SkeechSlayerChallengeId, result.ChallengeId);
        Assert.Equal(ChallengeResult.AreaRestriction, result.Result);
        Assert.Empty(GetMessages<ServerChallengeUpdate>(sessionProxy));
    }

    [Fact]
    public void RestrictedCombatChallenge_DoesNotAutoActivateWhenZoneIsUnknown()
    {
        GameTableManager gameTables = CreateGameTables(
            new ChallengeEntry
            {
                Id                      = XenobiteEggSmasherChallengeId,
                ChallengeTypeEnum       = (uint)ChallengeType.Combat,
                Target                  = XenobiteEggSmasherTargetGroupId,
                ChallengeFlags          = XenobiteEggSmasherChallengeFlags,
                WorldZoneIdRestriction  = XenobiteEggSmasherWorldZoneRestrictionId,
                WorldZoneId             = RootbruteSlayerWorldZoneId,
                ChallengeTierId00       = XenobiteEggSmasherTier0Id
            },
            [
                new ChallengeTierEntry
                {
                    Id    = XenobiteEggSmasherTier0Id,
                    Count = 10u
                }
            ],
            [
                new TargetGroupEntry
                {
                    Id          = XenobiteEggSmasherTargetGroupId,
                    Type        = (uint)TargetGroupType.CreatureIdGroup,
                    DataEntries = [XenobiteEggCreature2Id, 0u, 0u, 0u, 0u, 0u, 0u]
                }
            ]);
        ChallengeManager manager = CreateManager(
            gameTables,
            out IPlayer player,
            out RecordingDispatchProxy<IGameSession> sessionProxy,
            out _,
            out _);

        ChallengeCombatHooks.OnCreatureKilled(player, XenobiteEggCreature2Id);

        Assert.Empty(GetMessages<ServerChallengeResult>(sessionProxy));
        Assert.Empty(GetMessages<ServerChallengeUpdate>(sessionProxy));
    }

    [Fact]
    public void MappedRewardTrackItem_GrantsFirstSupportedItemChoiceOnCompletion()
    {
        GameTableManager gameTables = CreateGameTables(
            new ChallengeEntry
            {
                Id                      = ExtraterrestrialExterminatorChallengeId,
                ChallengeTypeEnum       = (uint)ChallengeType.Combat,
                Target                  = ExtraterrestrialExterminatorTargetGroupId,
                ChallengeTierId00       = ExtraterrestrialExterminatorTier0Id,
                TargetGroupIdRewardPane = ExtraterrestrialExterminatorTargetGroupId,
                RewardTrackId           = FarsideRewardTrackId
            },
            [
                new ChallengeTierEntry
                {
                    Id    = ExtraterrestrialExterminatorTier0Id,
                    Count = 1u
                }
            ],
            [
                new TargetGroupEntry
                {
                    Id          = ExtraterrestrialExterminatorTargetGroupId,
                    Type        = (uint)TargetGroupType.CreatureIdListGroup,
                    DataEntries = [CobaltTitanCreature2Id, 44372u, 44377u, 28945u, 27569u, 27615u, 0u]
                }
            ],
            [
                new RewardTrackEntry
                {
                    Id                    = FarsideRewardTrackId,
                    RewardTrackTypeEnum   = 3u,
                    RewardPointCost00     = 300u,
                    RewardPointCost01     = 600u,
                    RewardPointCost02     = 900u,
                    RewardPointCost03     = 1200u,
                    RewardTrackIdParent   = 0u
                }
            ],
            [
                new RewardTrackRewardsEntry
                {
                    Id                            = FarsideRewardTrackRewardId,
                    RewardTrackId                 = FarsideRewardTrackId,
                    RewardPointFlags              = 1u,
                    RewardTrackRewardTypeEnum00   = 0u,
                    RewardTrackRewardTypeEnum01   = 0u,
                    RewardTrackRewardTypeEnum02   = 0u,
                    RewardChoiceId00              = JourneymanCraftersBagItem2Id,
                    RewardChoiceId01              = 83757u,
                    RewardChoiceId02              = 85182u,
                    RewardChoiceCount00           = 1u,
                    RewardChoiceCount01           = 1u,
                    RewardChoiceCount02           = 1u
                }
            ]);
        ChallengeManager manager = CreateManager(
            gameTables,
            out _,
            out RecordingDispatchProxy<IGameSession> sessionProxy,
            out _,
            out RecordingDispatchProxy<IInventory> inventoryProxy);

        manager.HandleChoice(ExtraterrestrialExterminatorChallengeId, ChallengeChoice.Activate);
        Assert.True(manager.TryAdvanceProgress(ExtraterrestrialExterminatorChallengeId));

        ServerChallengeResult completed = GetMessages<ServerChallengeResult>(sessionProxy)
            .Last(result => result.Result == ChallengeResult.Completed);
        Assert.Equal(0, completed.Data);

        RecordingDispatchProxy<IInventory>.Invocation grant = Assert.Single(
            inventoryProxy.GetInvocations(nameof(IInventory.ItemCreate)));
        Assert.Equal(InventoryLocation.Inventory, grant.Arguments[0]);
        Assert.Equal(JourneymanCraftersBagItem2Id, grant.Arguments[1]);
        Assert.Equal(1u, grant.Arguments[2]);
        Assert.Equal(ItemUpdateReason.Challenge, grant.Arguments[3]);
    }

    [Fact]
    public void NorthernWildsRewardTrack_GrantsSmallBagOnBronzeCompletion()
    {
        GameTableManager gameTables = CreateGameTables(
            new ChallengeEntry
            {
                Id                      = RootbruteSlayerChallengeId,
                ChallengeTypeEnum       = (uint)ChallengeType.Combat,
                Target                  = RootbruteSlayerTargetGroupId,
                ChallengeFlags          = AutoActivateOnProgressFlag,
                WorldZoneIdRestriction  = RootbruteSlayerWorldZoneId,
                WorldZoneId             = RootbruteSlayerWorldZoneId,
                ChallengeTierId00       = RootbruteSlayerTier0Id,
                RewardTrackId           = NorthernWildsRewardTrackId
            },
            [
                new ChallengeTierEntry
                {
                    Id    = RootbruteSlayerTier0Id,
                    Count = 5u
                }
            ],
            [
                new TargetGroupEntry
                {
                    Id          = RootbruteSlayerTargetGroupId,
                    Type        = (uint)TargetGroupType.CreatureIdGroup,
                    DataEntries = [RootbruteGrimsporeCreature2Id, 13549u, RootbruteDeathcapCreature2Id, 13118u, 33848u, 0u, 0u]
                }
            ],
            [
                new RewardTrackEntry
                {
                    Id                    = NorthernWildsRewardTrackId,
                    RewardTrackTypeEnum   = 3u,
                    RewardPointCost00     = 100u,
                    RewardPointCost01     = 200u,
                    RewardPointCost02     = 300u,
                    RewardTrackIdParent   = 0u
                }
            ],
            [
                new RewardTrackRewardsEntry
                {
                    Id                            = NorthernWildsBronzeRewardTrackRewardId,
                    RewardTrackId                 = NorthernWildsRewardTrackId,
                    RewardPointFlags              = 1u,
                    RewardTrackRewardTypeEnum00   = 0u,
                    RewardTrackRewardTypeEnum01   = 0u,
                    RewardTrackRewardTypeEnum02   = 0u,
                    RewardChoiceId00              = SmallBagItem2Id,
                    RewardChoiceId01              = 0u,
                    RewardChoiceId02              = 0u,
                    RewardChoiceCount00           = 1u,
                    RewardChoiceCount01           = 0u,
                    RewardChoiceCount02           = 0u
                }
            ]);
        ChallengeManager manager = CreateManager(
            gameTables,
            out _,
            out RecordingDispatchProxy<IGameSession> sessionProxy,
            out _,
            out RecordingDispatchProxy<IInventory> inventoryProxy,
            new WorldZoneEntry
            {
                Id = RootbruteSlayerWorldZoneId
            });

        manager.HandleChoice(RootbruteSlayerChallengeId, ChallengeChoice.Activate);
        for (int i = 0; i < 5; i++)
            Assert.True(manager.TryAdvanceProgress(RootbruteSlayerChallengeId));

        ServerChallengeResult completed = GetMessages<ServerChallengeResult>(sessionProxy)
            .Last(result => result.Result == ChallengeResult.Completed);
        Assert.Equal(RootbruteSlayerChallengeId, completed.ChallengeId);
        Assert.Equal(0, completed.Data);

        RecordingDispatchProxy<IInventory>.Invocation grant = Assert.Single(
            inventoryProxy.GetInvocations(nameof(IInventory.ItemCreate)));
        Assert.Equal(InventoryLocation.Inventory, grant.Arguments[0]);
        Assert.Equal(SmallBagItem2Id, grant.Arguments[1]);
        Assert.Equal(1u, grant.Arguments[2]);
        Assert.Equal(ItemUpdateReason.Challenge, grant.Arguments[3]);
    }

    private static GameTableManager CreateRootbruteSlayerGameTables(IEnumerable<WorldZoneEntry> worldZones = null)
    {
        return CreateGameTables(
            new ChallengeEntry
            {
                Id                      = RootbruteSlayerChallengeId,
                ChallengeTypeEnum       = (uint)ChallengeType.Combat,
                Target                  = RootbruteSlayerTargetGroupId,
                ChallengeFlags          = AutoActivateOnProgressFlag,
                WorldZoneIdRestriction  = RootbruteSlayerWorldZoneId,
                WorldZoneId             = RootbruteSlayerWorldZoneId,
                ChallengeTierId00       = RootbruteSlayerTier0Id
            },
            [
                new ChallengeTierEntry
                {
                    Id    = RootbruteSlayerTier0Id,
                    Count = 5u
                }
            ],
            [
                new TargetGroupEntry
                {
                    Id          = RootbruteSlayerTargetGroupId,
                    Type        = (uint)TargetGroupType.CreatureIdGroup,
                    DataEntries = [RootbruteGrimsporeCreature2Id, 13549u, RootbruteDeathcapCreature2Id, 13118u, 33848u, 0u, 0u]
                }
            ],
            worldZones: worldZones);
    }

    private static GameTableManager CreateSkeechSlayerGameTables(IEnumerable<WorldZoneEntry> worldZones = null)
    {
        return CreateGameTables(
            new ChallengeEntry
            {
                Id                      = SkeechSlayerChallengeId,
                ChallengeTypeEnum       = (uint)ChallengeType.Combat,
                Target                  = SkeechSlayerTargetGroupId,
                ChallengeFlags          = AutoActivateOnProgressFlag,
                WorldZoneIdRestriction  = SkeechSlayerWorldZoneRestrictionId,
                WorldZoneId             = RootbruteSlayerWorldZoneId,
                ChallengeTierId00       = SkeechSlayerTier0Id
            },
            [
                new ChallengeTierEntry
                {
                    Id    = SkeechSlayerTier0Id,
                    Count = 5u
                }
            ],
            [
                new TargetGroupEntry
                {
                    Id          = SkeechSlayerTargetGroupId,
                    Type        = (uint)TargetGroupType.OtherTargetGroupCreatures,
                    DataEntries = [SkeechSlayerChildTargetGroupId, SkeechSlayerRareChildTargetGroupId, 0u, 0u, 0u, 0u, 0u]
                },
                new TargetGroupEntry
                {
                    Id          = SkeechSlayerChildTargetGroupId,
                    Type        = (uint)TargetGroupType.CreatureIdListGroup,
                    DataEntries = [20607u, SkeechScratcherCreature2Id, 11580u, 20605u, 20608u, 11576u, 15063u]
                },
                new TargetGroupEntry
                {
                    Id          = SkeechSlayerRareChildTargetGroupId,
                    Type        = (uint)TargetGroupType.CreatureIdListGroup,
                    DataEntries = [48101u, 48100u, 48102u, 17185u, 17171u, 0u, 0u]
                }
            ],
            worldZones: worldZones);
    }

    private static GameTableManager CreateXenobiteEggSmasherGameTables(IEnumerable<WorldZoneEntry> worldZones = null)
    {
        return CreateGameTables(
            new ChallengeEntry
            {
                Id                      = XenobiteEggSmasherChallengeId,
                ChallengeTypeEnum       = (uint)ChallengeType.Combat,
                Target                  = XenobiteEggSmasherTargetGroupId,
                ChallengeFlags          = XenobiteEggSmasherChallengeFlags,
                WorldZoneIdRestriction  = XenobiteEggSmasherWorldZoneRestrictionId,
                WorldZoneId             = RootbruteSlayerWorldZoneId,
                ChallengeTierId00       = XenobiteEggSmasherTier0Id
            },
            [
                new ChallengeTierEntry
                {
                    Id    = XenobiteEggSmasherTier0Id,
                    Count = 10u
                }
            ],
            [
                new TargetGroupEntry
                {
                    Id          = XenobiteEggSmasherTargetGroupId,
                    Type        = (uint)TargetGroupType.CreatureIdGroup,
                    DataEntries = [XenobiteEggCreature2Id, 0u, 0u, 0u, 0u, 0u, 0u]
                }
            ],
            worldZones: worldZones);
    }

    private static WorldZoneEntry[] CreateNorthernWildsWorldZones()
    {
        return
        [
            new WorldZoneEntry
            {
                Id = RootbruteSlayerWorldZoneId
            },
            new WorldZoneEntry
            {
                Id           = NorthernWildsRegionalWorldZoneId,
                ParentZoneId = RootbruteSlayerWorldZoneId
            },
            new WorldZoneEntry
            {
                Id           = NorthernWildsParentAreaWorldZoneId,
                ParentZoneId = NorthernWildsRegionalWorldZoneId
            },
            new WorldZoneEntry
            {
                Id           = SkeechSlayerWorldZoneRestrictionId,
                ParentZoneId = NorthernWildsParentAreaWorldZoneId
            },
            new WorldZoneEntry
            {
                Id           = XenobiteEggSmasherWorldZoneRestrictionId,
                ParentZoneId = NorthernWildsParentAreaWorldZoneId
            },
            new WorldZoneEntry
            {
                Id           = NorthernWildsRootbruteSpawnWorldZoneId,
                ParentZoneId = NorthernWildsParentAreaWorldZoneId
            }
        ];
    }

    private static ChallengeManager CreateManager(
        GameTableManager gameTables,
        out IPlayer player,
        out RecordingDispatchProxy<IGameSession> sessionProxy,
        out RecordingDispatchProxy<IQuestManager> questProxy,
        out RecordingDispatchProxy<IInventory> inventoryProxy,
        WorldZoneEntry zone = null,
        uint? worldId = null)
    {
        player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out sessionProxy);
        IQuestManager questManager = RecordingDispatchProxy<IQuestManager>.Create(out questProxy);
        IInventory inventory = RecordingDispatchProxy<IInventory>.Create(out inventoryProxy);
        if (worldId.HasValue)
        {
            IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out RecordingDispatchProxy<IBaseMap> mapProxy);
            mapProxy.SetProperty(nameof(IBaseMap.Entry), new WorldEntry { Id = worldId.Value });
            playerProxy.SetProperty(nameof(IPlayer.Map), map);
        }

        playerProxy.SetProperty(nameof(IPlayer.Guid), 9001u);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), 42ul);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);
        playerProxy.SetProperty(nameof(IPlayer.QuestManager), questManager);
        playerProxy.SetProperty(nameof(IPlayer.Inventory), inventory);
        if (zone != null)
            playerProxy.SetProperty(nameof(IPlayer.Zone), zone);

        var manager = new ChallengeManager(player, gameTables);
        playerProxy.SetProperty(nameof(IPlayer.ChallengeManager), manager);
        return manager;
    }

    private static GameTableManager CreateGameTables(
        ChallengeEntry challenge,
        IEnumerable<ChallengeTierEntry> challengeTiers,
        IEnumerable<TargetGroupEntry> targetGroups = null,
        IEnumerable<RewardTrackEntry> rewardTracks = null,
        IEnumerable<RewardTrackRewardsEntry> rewardTrackRewards = null,
        IEnumerable<WorldZoneEntry> worldZones = null)
    {
        var gameTableManager = new GameTableManager(Options.Create(new GameTableConfig
        {
            GameTablePath = string.Empty
        }));

        SetAutoProperty(gameTableManager, nameof(GameTableManager.Challenge), CreateGameTable(challenge));
        SetAutoProperty(gameTableManager, nameof(GameTableManager.ChallengeTier), CreateGameTable(challengeTiers.ToArray()));
        SetAutoProperty(gameTableManager, nameof(GameTableManager.TargetGroup), CreateGameTable((targetGroups ?? []).ToArray()));
        SetAutoProperty(gameTableManager, nameof(GameTableManager.RewardTrack), CreateGameTable((rewardTracks ?? []).ToArray()));
        SetAutoProperty(gameTableManager, nameof(GameTableManager.RewardTrackRewards), CreateGameTable((rewardTrackRewards ?? []).ToArray()));
        SetAutoProperty(gameTableManager, nameof(GameTableManager.WorldZone), CreateGameTable((worldZones ?? []).ToArray()));
        return gameTableManager;
    }

    private static IReadOnlyList<T> GetMessages<T>(RecordingDispatchProxy<IGameSession> sessionProxy)
        where T : class, IWritable
    {
        return sessionProxy
            .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(invocation => invocation.Arguments[0])
            .OfType<T>()
            .ToList();
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
        return (uint)typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public)[0].GetValue(entry)!;
    }

    private static void SetAutoProperty(object target, string propertyName, object value)
    {
        FieldInfo backingField = target.GetType()
            .GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!;
        backingField.SetValue(target, value);
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType()
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!;
        field.SetValue(target, value);
    }
}
