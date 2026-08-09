using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using PublicEventObjectiveModel = NexusForever.Game.PublicEvent.PublicEventObjective;

namespace NexusForever.Game.Tests.PublicEvents;

public class PublicEventObjectiveTests
{
    [Fact]
    public void Update_BeforeActivation_DoesNotAdvanceElapsedTimeOrFail()
    {
        PublicEventObjectiveModel objective = PublicEventTestSupport.CreateObjective(new PublicEventObjectiveEntry
        {
            Id                         = 101,
            Count                      = 1,
            FailureTimeMs              = 1000,
            PublicEventTeamId          = PublicEventTeam.PublicTeam,
            PublicEventObjectiveFlags  = PublicEventObjectiveFlag.None,
            PublicEventObjectiveCategoryEnum = PublicEventObjectiveCategory.Main
        });

        objective.Update(1.5d);

        Assert.Equal(PublicEventStatus.Inactive, objective.Status);
        Assert.Equal(0u, objective.Build().ElapsedTimeMs);
    }

    [Fact]
    public void ResetObjective_ClearsElapsedTimeAndRestartsFailureTimer()
    {
        PublicEventObjectiveModel objective = PublicEventTestSupport.CreateObjective(new PublicEventObjectiveEntry
        {
            Id                         = 102,
            Count                      = 1,
            FailureTimeMs              = 1000,
            PublicEventTeamId          = PublicEventTeam.PublicTeam,
            PublicEventObjectiveFlags  = PublicEventObjectiveFlag.None,
            PublicEventObjectiveCategoryEnum = PublicEventObjectiveCategory.Main
        });

        objective.ActivateObjective();
        objective.Update(0.6d);
        objective.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Succeeded, objective.Status);
        Assert.Equal(600u, objective.Build().ElapsedTimeMs);

        objective.ResetObjective();
        objective.Update(1.1d);

        Assert.Equal(PublicEventStatus.Inactive, objective.Status);
        Assert.Equal(0u, objective.Build().ElapsedTimeMs);

        objective.ActivateObjective();
        objective.Update(0.6d);

        Assert.Equal(PublicEventStatus.Active, objective.Status);
        Assert.Equal(600u, objective.Build().ElapsedTimeMs);

        objective.Update(0.5d);

        Assert.Equal(PublicEventStatus.Failed, objective.Status);
    }

    [Fact]
    public void TotalDomination_SucceedsBeforeFiveMinuteDeadlineAndRejectsLateCredit()
    {
        PublicEventObjectiveEntry entry = new()
        {
            Id                              = 2884,
            Count                           = 0,
            FailureTimeMs                   = 300000,
            PublicEventTeamId               = PublicEventTeam.PublicTeam,
            PublicEventObjectiveFlags       = PublicEventObjectiveFlag.None,
            PublicEventObjectiveCategoryEnum = PublicEventObjectiveCategory.Optional,
            PublicEventObjectiveTypeEnum    = PublicEventObjectiveType.TimedWin
        };

        (NexusForever.Game.PublicEvent.PublicEventTeam _, PublicEventObjectiveModel beforeDeadline)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);
        beforeDeadline.Team.ActivateObjective(2884u, 1u);
        beforeDeadline.Update(299.999d);
        beforeDeadline.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Succeeded, beforeDeadline.Status);
        Assert.Equal(299999u, beforeDeadline.Build().ElapsedTimeMs);

        (NexusForever.Game.PublicEvent.PublicEventTeam _, PublicEventObjectiveModel afterDeadline)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);
        afterDeadline.Team.ActivateObjective(2884u, 1u);
        afterDeadline.Update(300d);
        afterDeadline.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Failed, afterDeadline.Status);
        Assert.Equal(0u, afterDeadline.Count);
    }

    [Fact]
    public void PanicPronto_SharedTimerBoundarySucceedsBeforeThirtySecondsAndRejectsLateCredit()
    {
        PublicEventObjectiveEntry entry = new()
        {
            Id                              = 2885,
            Count                           = 0,
            FailureTimeMs                   = 30000,
            PublicEventTeamId               = PublicEventTeam.PublicTeam,
            PublicEventObjectiveFlags       = (PublicEventObjectiveFlag)4,
            PublicEventObjectiveCategoryEnum = PublicEventObjectiveCategory.Challenge,
            PublicEventObjectiveTypeEnum    = PublicEventObjectiveType.Script
        };

        (NexusForever.Game.PublicEvent.PublicEventTeam _, PublicEventObjectiveModel beforeDeadline)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);
        beforeDeadline.Team.ActivateObjective(2885u, 1u);
        beforeDeadline.Update(29.999d);
        beforeDeadline.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Succeeded, beforeDeadline.Status);
        Assert.Equal(29999u, beforeDeadline.Build().ElapsedTimeMs);

        (NexusForever.Game.PublicEvent.PublicEventTeam _, PublicEventObjectiveModel afterDeadline)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);
        afterDeadline.Team.ActivateObjective(2885u, 1u);
        afterDeadline.Update(30d);
        afterDeadline.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Failed, afterDeadline.Status);
        Assert.Equal(0u, afterDeadline.Count);
    }

    [Fact]
    public void EscapedPatient_SharedTimerBoundarySucceedsBeforeSixtySecondsAndRejectsLateCredit()
    {
        PublicEventObjectiveEntry entry = new()
        {
            Id                              = 2887,
            Count                           = 0,
            FailureTimeMs                   = 60000,
            PublicEventTeamId               = PublicEventTeam.PublicTeam,
            PublicEventObjectiveFlags       = (PublicEventObjectiveFlag)4,
            PublicEventObjectiveCategoryEnum = PublicEventObjectiveCategory.Challenge,
            PublicEventObjectiveTypeEnum    = PublicEventObjectiveType.TimedWin
        };

        (NexusForever.Game.PublicEvent.PublicEventTeam _, PublicEventObjectiveModel beforeDeadline)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);
        beforeDeadline.Team.ActivateObjective(2887u, 1u);
        beforeDeadline.Update(59.999d);
        beforeDeadline.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Succeeded, beforeDeadline.Status);
        Assert.Equal(59999u, beforeDeadline.Build().ElapsedTimeMs);

        (NexusForever.Game.PublicEvent.PublicEventTeam _, PublicEventObjectiveModel afterDeadline)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);
        afterDeadline.Team.ActivateObjective(2887u, 1u);
        afterDeadline.Update(60d);
        afterDeadline.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Failed, afterDeadline.Status);
        Assert.Equal(0u, afterDeadline.Count);
    }

    [Fact]
    public void QuickVisit_SharedTimerBoundarySucceedsBeforeThreeMinutesAndRejectsLateCredit()
    {
        PublicEventObjectiveEntry entry = new()
        {
            Id                              = 2888,
            Count                           = 0,
            FailureTimeMs                   = 180000,
            PublicEventTeamId               = PublicEventTeam.PublicTeam,
            PublicEventObjectiveFlags       = (PublicEventObjectiveFlag)4,
            PublicEventObjectiveCategoryEnum = PublicEventObjectiveCategory.Challenge,
            PublicEventObjectiveTypeEnum    = PublicEventObjectiveType.Script
        };

        (NexusForever.Game.PublicEvent.PublicEventTeam _, PublicEventObjectiveModel beforeDeadline)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);
        beforeDeadline.Team.ActivateObjective(2888u, 1u);
        beforeDeadline.Update(179.999d);
        beforeDeadline.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Succeeded, beforeDeadline.Status);
        Assert.Equal(179999u, beforeDeadline.Build().ElapsedTimeMs);

        (NexusForever.Game.PublicEvent.PublicEventTeam _, PublicEventObjectiveModel afterDeadline)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);
        afterDeadline.Team.ActivateObjective(2888u, 1u);
        afterDeadline.Update(180d);
        afterDeadline.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Failed, afterDeadline.Status);
        Assert.Equal(0u, afterDeadline.Count);
    }

    [Fact]
    public void Chronophobia_SharedTimerBoundarySucceedsBeforeTenMinutesAndRejectsLateCredit()
    {
        PublicEventObjectiveEntry entry = new()
        {
            Id                              = 2890,
            Count                           = 0,
            FailureTimeMs                   = 600000,
            PublicEventTeamId               = PublicEventTeam.PublicTeam,
            PublicEventObjectiveFlags       = (PublicEventObjectiveFlag)4,
            PublicEventObjectiveCategoryEnum = PublicEventObjectiveCategory.Optional,
            PublicEventObjectiveTypeEnum    = PublicEventObjectiveType.TimedWin
        };

        (NexusForever.Game.PublicEvent.PublicEventTeam _, PublicEventObjectiveModel beforeDeadline)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);
        beforeDeadline.Team.ActivateObjective(2890u, 1u);
        beforeDeadline.Update(599.999d);
        beforeDeadline.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Succeeded, beforeDeadline.Status);
        Assert.Equal(599999u, beforeDeadline.Build().ElapsedTimeMs);

        (NexusForever.Game.PublicEvent.PublicEventTeam _, PublicEventObjectiveModel afterDeadline)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);
        afterDeadline.Team.ActivateObjective(2890u, 1u);
        afterDeadline.Update(600d);
        afterDeadline.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Failed, afterDeadline.Status);
        Assert.Equal(0u, afterDeadline.Count);
    }

    [Fact]
    public void DriveBy_SharedTimerBoundarySucceedsBeforeTwoMinutesAndRejectsLateCredit()
    {
        PublicEventObjectiveEntry entry = new()
        {
            Id                              = 2853,
            Count                           = 0,
            FailureTimeMs                   = 120000,
            PublicEventTeamId               = PublicEventTeam.PublicTeam,
            PublicEventObjectiveFlags       = (PublicEventObjectiveFlag)2171908,
            PublicEventObjectiveCategoryEnum = PublicEventObjectiveCategory.Challenge,
            PublicEventObjectiveTypeEnum    = PublicEventObjectiveType.KillClusterEventObjectiveUnit
        };

        (NexusForever.Game.PublicEvent.PublicEventTeam _, PublicEventObjectiveModel beforeDeadline)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);
        beforeDeadline.Team.ActivateObjective(2853u, 1u);
        beforeDeadline.Update(119.999d);
        beforeDeadline.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Succeeded, beforeDeadline.Status);
        Assert.Equal(1u, beforeDeadline.Count);

        (NexusForever.Game.PublicEvent.PublicEventTeam _, PublicEventObjectiveModel afterDeadline)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);
        afterDeadline.Team.ActivateObjective(2853u, 1u);
        afterDeadline.Update(120d);
        afterDeadline.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Failed, afterDeadline.Status);
        Assert.Equal(0u, afterDeadline.Count);
    }

    [Fact]
    public void FastHands_ExactTargetGroupTimerBoundarySucceedsBeforeThreeAndAHalfMinutesAndRejectsLateCredit()
    {
        PublicEventObjectiveEntry entry = new()
        {
            Id                              = 2863,
            Count                           = 0,
            ObjectId                        = 10583,
            FailureTimeMs                   = 210000,
            PublicEventTeamId               = PublicEventTeam.PublicTeam,
            PublicEventObjectiveFlags       = (PublicEventObjectiveFlag)2105348,
            PublicEventObjectiveCategoryEnum = PublicEventObjectiveCategory.Optional,
            PublicEventObjectiveTypeEnum    = PublicEventObjectiveType.ActivateTargetGroup
        };

        (NexusForever.Game.PublicEvent.PublicEventTeam beforeDeadlineTeam, PublicEventObjectiveModel beforeDeadline)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);
        beforeDeadlineTeam.ActivateObjective(2863u, 1u);
        beforeDeadline.Update(209.999d);
        beforeDeadlineTeam.UpdateObjective(PublicEventObjectiveType.ActivateTargetGroup, 10583u, 1);

        Assert.Equal(PublicEventStatus.Succeeded, beforeDeadline.Status);
        Assert.Equal(1u, beforeDeadline.Count);
        Assert.Equal(209999u, beforeDeadline.Build().ElapsedTimeMs);

        (NexusForever.Game.PublicEvent.PublicEventTeam afterDeadlineTeam, PublicEventObjectiveModel afterDeadline)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);
        afterDeadlineTeam.ActivateObjective(2863u, 1u);
        afterDeadline.Update(210d);
        afterDeadlineTeam.UpdateObjective(PublicEventObjectiveType.ActivateTargetGroup, 10583u, 1);

        Assert.Equal(PublicEventStatus.Failed, afterDeadline.Status);
        Assert.Equal(0u, afterDeadline.Count);
    }

    [Fact]
    public void MasterTheElements_ResourcePoolRequiresSixControllerCreditsAndRejectsPostSuccessCredit()
    {
        PublicEventObjectiveEntry entry = new()
        {
            Id                              = 2674,
            Count                           = 6,
            FailureTimeMs                   = 0,
            WorldLocation2Id                = 41714,
            PublicEventTeamId               = PublicEventTeam.PublicTeam,
            PublicEventObjectiveFlags       = 0,
            PublicEventObjectiveCategoryEnum = PublicEventObjectiveCategory.Main,
            PublicEventObjectiveTypeEnum    = PublicEventObjectiveType.ResourcePool
        };

        (NexusForever.Game.PublicEvent.PublicEventTeam team, PublicEventObjectiveModel objective)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);
        team.ActivateObjective(2674u, 0u);
        objective.UpdateObjective(5);

        Assert.Equal(PublicEventStatus.Active, objective.Status);
        Assert.Equal(5u, objective.Count);
        Assert.Equal([41714u], objective.Build().Locations);

        objective.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Succeeded, objective.Status);
        Assert.Equal(6u, objective.Count);

        objective.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Succeeded, objective.Status);
        Assert.Equal(6u, objective.Count);
    }

    [Fact]
    public void Slaughterhouse_ResourcePoolRequiresFortyCreditsAndRejectsPostSuccessCredit()
    {
        PublicEventObjectiveEntry entry = new()
        {
            Id                              = 2898,
            Count                           = 40,
            PublicEventTeamId               = PublicEventTeam.PublicTeam,
            PublicEventObjectiveFlags       = (PublicEventObjectiveFlag)4,
            PublicEventObjectiveCategoryEnum = PublicEventObjectiveCategory.Challenge,
            PublicEventObjectiveTypeEnum    = PublicEventObjectiveType.ResourcePool
        };

        (NexusForever.Game.PublicEvent.PublicEventTeam team, PublicEventObjectiveModel objective)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);
        team.ActivateObjective(2898u, 0u);
        objective.UpdateObjective(39);

        Assert.Equal(PublicEventStatus.Active, objective.Status);
        Assert.Equal(39u, objective.Count);

        objective.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Succeeded, objective.Status);
        Assert.Equal(40u, objective.Count);

        objective.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Succeeded, objective.Status);
        Assert.Equal(40u, objective.Count);
    }

    [Fact]
    public void ClearTheLostAndFound_ResourcePoolRequiresEightyCratesAndRejectsPostSuccessCredit()
    {
        PublicEventObjectiveEntry entry = new()
        {
            Id                               = 2673,
            Count                            = 80,
            WorldLocation2Id                 = 41745,
            PublicEventTeamId                = PublicEventTeam.PublicTeam,
            PublicEventObjectiveTypeEnum     = PublicEventObjectiveType.ResourcePool
        };

        (NexusForever.Game.PublicEvent.PublicEventTeam team, PublicEventObjectiveModel objective)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);
        team.ActivateObjective(2673u, 0u);
        objective.UpdateObjective(79);

        Assert.Equal(PublicEventStatus.Active, objective.Status);
        Assert.Equal(79u, objective.Count);
        Assert.Equal([41745u], objective.Build().Locations);

        objective.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Succeeded, objective.Status);
        Assert.Equal(80u, objective.Count);

        objective.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Succeeded, objective.Status);
        Assert.Equal(80u, objective.Count);
    }

    [Fact]
    public void RunawayVeggie_ResourcePoolSucceedsBeforeFortyFiveSecondsAndRejectsLateCredit()
    {
        PublicEventObjectiveEntry entry = new()
        {
            Id                              = 2899,
            Count                           = 1,
            FailureTimeMs                   = 45000,
            PublicEventTeamId               = PublicEventTeam.PublicTeam,
            PublicEventObjectiveFlags       = (PublicEventObjectiveFlag)4,
            PublicEventObjectiveCategoryEnum = PublicEventObjectiveCategory.Challenge,
            PublicEventObjectiveTypeEnum    = PublicEventObjectiveType.ResourcePool
        };

        (NexusForever.Game.PublicEvent.PublicEventTeam _, PublicEventObjectiveModel beforeDeadline)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);
        beforeDeadline.Team.ActivateObjective(2899u, 0u);
        beforeDeadline.Update(44.999d);
        beforeDeadline.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Succeeded, beforeDeadline.Status);
        Assert.Equal(44999u, beforeDeadline.Build().ElapsedTimeMs);

        (NexusForever.Game.PublicEvent.PublicEventTeam _, PublicEventObjectiveModel afterDeadline)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);
        afterDeadline.Team.ActivateObjective(2899u, 0u);
        afterDeadline.Update(45d);
        afterDeadline.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Failed, afterDeadline.Status);
        Assert.Equal(0u, afterDeadline.Count);
    }

    [Fact]
    public void AnotherJabbit_ScriptWithoutMaxAccumulatesBeyondConfiguredCountWithoutAutoSuccess()
    {
        PublicEventObjectiveEntry entry = new()
        {
            Id                              = 2901,
            Count                           = 1,
            PublicEventTeamId               = PublicEventTeam.PublicTeam,
            PublicEventObjectiveFlags       = (PublicEventObjectiveFlag)516,
            PublicEventObjectiveCategoryEnum = PublicEventObjectiveCategory.Optional,
            PublicEventObjectiveTypeEnum    = PublicEventObjectiveType.ScriptWithoutMax
        };

        (NexusForever.Game.PublicEvent.PublicEventTeam team, PublicEventObjectiveModel objective)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);
        team.ActivateObjective(2901u, 0u);
        objective.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Active, objective.Status);
        Assert.Equal(1u, objective.Count);

        objective.UpdateObjective(2);

        Assert.Equal(PublicEventStatus.Active, objective.Status);
        Assert.Equal(3u, objective.Count);

        objective.UpdateObjective(-4);

        Assert.Equal(PublicEventStatus.Active, objective.Status);
        Assert.Equal(0u, objective.Count);
    }

    [Theory]
    [InlineData(4351u)]
    [InlineData(4352u)]
    [InlineData(4353u)]
    public void UltimateProtogamesHiddenInitialScriptWithoutMax_RemainsControllerOwned(uint objectiveId)
    {
        PublicEventObjectiveEntry entry = new()
        {
            Id                              = objectiveId,
            Count                           = 0,
            PublicEventTeamId               = PublicEventTeam.PublicTeam,
            PublicEventObjectiveFlags       = (PublicEventObjectiveFlag)513,
            PublicEventObjectiveCategoryEnum = PublicEventObjectiveCategory.Main,
            PublicEventObjectiveTypeEnum    = PublicEventObjectiveType.ScriptWithoutMax
        };

        (NexusForever.Game.PublicEvent.PublicEventTeam team, PublicEventObjectiveModel objective)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);

        Assert.Equal(PublicEventStatus.Active, objective.Status);

        objective.UpdateObjective(5);

        Assert.Equal(PublicEventStatus.Active, objective.Status);
        Assert.Equal(5u, objective.Count);
        Assert.False(team.IsFinialised);

        objective.UpdateObjective(-7);

        Assert.Equal(PublicEventStatus.Active, objective.Status);
        Assert.Equal(0u, objective.Count);
        Assert.False(team.IsFinialised);
    }

    [Fact]
    public void SplorgStepper_ExactScriptRowRequiresControllerActivationAndCredit()
    {
        PublicEventObjectiveEntry entry = new()
        {
            Id                              = 4524,
            Count                           = 1,
            PublicEventTeamId               = PublicEventTeam.PublicTeam,
            PublicEventObjectiveFlags       = (PublicEventObjectiveFlag)4,
            PublicEventObjectiveCategoryEnum = PublicEventObjectiveCategory.Optional,
            PublicEventObjectiveTypeEnum    = PublicEventObjectiveType.Script
        };

        (NexusForever.Game.PublicEvent.PublicEventTeam team, PublicEventObjectiveModel objective)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);

        team.UpdateObjective(4524u, 1);

        Assert.Equal(PublicEventStatus.Inactive, objective.Status);
        Assert.Equal(0u, objective.Count);

        team.ActivateObjective(4524u, 0u);
        team.UpdateObjective(PublicEventObjectiveType.ScriptWithoutMax, 0u, 1);
        team.UpdateObjective(PublicEventObjectiveType.Script, 1u, 1);

        Assert.Equal(PublicEventStatus.Active, objective.Status);
        Assert.Equal(0u, objective.Count);

        team.UpdateObjective(4524u, 1);

        Assert.Equal(PublicEventStatus.Succeeded, objective.Status);
        Assert.Equal(1u, objective.Count);

        team.UpdateObjective(4524u, 1);

        Assert.Equal(PublicEventStatus.Succeeded, objective.Status);
        Assert.Equal(1u, objective.Count);
    }

    [Fact]
    public void ProtostarEventSpecialist_InitialTalkToCounterUsesExactTargetGroupWithoutAutoCompleting()
    {
        PublicEventObjectiveEntry entry = new()
        {
            Id                              = 4535,
            Count                           = uint.MaxValue,
            ObjectId                        = 12376,
            PublicEventTeamId               = PublicEventTeam.PublicTeam,
            PublicEventObjectiveFlags       = (PublicEventObjectiveFlag)513,
            PublicEventObjectiveCategoryEnum = PublicEventObjectiveCategory.Main,
            PublicEventObjectiveTypeEnum    = PublicEventObjectiveType.TalkTo
        };

        (NexusForever.Game.PublicEvent.PublicEventTeam team, PublicEventObjectiveModel objective)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);

        Assert.Equal(PublicEventStatus.Active, objective.Status);

        team.UpdateObjective(PublicEventObjectiveType.TalkToChecklist, 12376u, 1);
        team.UpdateObjective(PublicEventObjectiveType.TalkTo, 12375u, 1);

        Assert.Equal(PublicEventStatus.Active, objective.Status);
        Assert.Equal(0u, objective.Count);

        team.UpdateObjective(PublicEventObjectiveType.TalkTo, 12376u, 1);
        team.UpdateObjective(PublicEventObjectiveType.TalkTo, 12376u, 1);

        Assert.Equal(PublicEventStatus.Active, objective.Status);
        Assert.Equal(2u, objective.Count);
        Assert.False(team.IsFinialised);
    }

    [Fact]
    public void ExtraPoint_ExactTimedWinBoundaryRequiresControllerSuccessBeforeFortyFiveSecondFailure()
    {
        PublicEventObjectiveEntry entry = new()
        {
            Id                              = 4541,
            Count                           = 0,
            FailureTimeMs                   = 45000,
            WorldLocation2Id                = 46229,
            PublicEventTeamId               = PublicEventTeam.PublicTeam,
            PublicEventObjectiveFlags       = (PublicEventObjectiveFlag)4,
            PublicEventObjectiveCategoryEnum = PublicEventObjectiveCategory.Optional,
            PublicEventObjectiveTypeEnum    = PublicEventObjectiveType.TimedWin
        };

        (NexusForever.Game.PublicEvent.PublicEventTeam _, PublicEventObjectiveModel beforeDeadline)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);
        beforeDeadline.Team.ActivateObjective(4541u, 0u);
        beforeDeadline.Update(44.999d);

        Assert.Equal(PublicEventStatus.Active, beforeDeadline.Status);
        Assert.Equal(0u, beforeDeadline.Count);
        Assert.Equal([46229u], beforeDeadline.Build().Locations);

        beforeDeadline.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Succeeded, beforeDeadline.Status);
        Assert.Equal(1u, beforeDeadline.Count);
        Assert.Equal(44999u, beforeDeadline.Build().ElapsedTimeMs);

        (NexusForever.Game.PublicEvent.PublicEventTeam _, PublicEventObjectiveModel atDeadline)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);
        atDeadline.Team.ActivateObjective(4541u, 0u);
        atDeadline.Update(45d);
        atDeadline.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Failed, atDeadline.Status);
        Assert.Equal(0u, atDeadline.Count);
    }

    [Fact]
    public void PestControl_ExactTimedWinBoundaryRequiresControllerSuccessBeforeThreeMinuteFailure()
    {
        PublicEventObjectiveEntry entry = new()
        {
            Id                              = 2672,
            Count                           = 0,
            FailureTimeMs                   = 180000,
            WorldLocation2Id                = 41741,
            PublicEventTeamId               = PublicEventTeam.PublicTeam,
            PublicEventObjectiveFlags       = PublicEventObjectiveFlag.None,
            PublicEventObjectiveCategoryEnum = PublicEventObjectiveCategory.Main,
            PublicEventObjectiveTypeEnum    = PublicEventObjectiveType.TimedWin
        };

        (NexusForever.Game.PublicEvent.PublicEventTeam _, PublicEventObjectiveModel beforeDeadline)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);
        beforeDeadline.Team.ActivateObjective(2672u, 0u);
        beforeDeadline.Update(179.999d);

        Assert.Equal(PublicEventStatus.Active, beforeDeadline.Status);
        Assert.Equal(0u, beforeDeadline.Count);
        Assert.Equal([41741u], beforeDeadline.Build().Locations);

        beforeDeadline.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Succeeded, beforeDeadline.Status);
        Assert.Equal(1u, beforeDeadline.Count);
        Assert.Equal(179999u, beforeDeadline.Build().ElapsedTimeMs);

        (NexusForever.Game.PublicEvent.PublicEventTeam _, PublicEventObjectiveModel atDeadline)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);
        atDeadline.Team.ActivateObjective(2672u, 0u);
        atDeadline.Update(180d);
        atDeadline.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Failed, atDeadline.Status);
        Assert.Equal(0u, atDeadline.Count);
    }

    [Fact]
    public void TankTrample_ExactTargetAndSixtySecondBoundaryRequireOneTankDeathBeforeDeadline()
    {
        PublicEventObjectiveEntry entry = new()
        {
            Id                              = 2868,
            Count                           = 1,
            ObjectId                        = 12671,
            FailureTimeMs                   = 60000,
            PublicEventTeamId               = PublicEventTeam.PublicTeam,
            PublicEventObjectiveFlags       = (PublicEventObjectiveFlag)2106372,
            PublicEventObjectiveCategoryEnum = PublicEventObjectiveCategory.Challenge,
            PublicEventObjectiveTypeEnum    = PublicEventObjectiveType.Exterminate
        };

        (NexusForever.Game.PublicEvent.PublicEventTeam beforeDeadlineTeam, PublicEventObjectiveModel beforeDeadline)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);
        beforeDeadlineTeam.ActivateObjective(2868u, 1u);
        beforeDeadline.Update(59.999d);
        beforeDeadlineTeam.UpdateObjective(PublicEventObjectiveType.Exterminate, 12672u, 1);

        Assert.Equal(PublicEventStatus.Active, beforeDeadline.Status);
        Assert.Equal(0u, beforeDeadline.Count);

        beforeDeadlineTeam.UpdateObjective(PublicEventObjectiveType.Exterminate, 12671u, 1);

        Assert.Equal(PublicEventStatus.Succeeded, beforeDeadline.Status);
        Assert.Equal(1u, beforeDeadline.Count);
        Assert.Equal(59999u, beforeDeadline.Build().ElapsedTimeMs);

        (NexusForever.Game.PublicEvent.PublicEventTeam atDeadlineTeam, PublicEventObjectiveModel atDeadline)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);
        atDeadlineTeam.ActivateObjective(2868u, 1u);
        atDeadline.Update(60d);
        atDeadlineTeam.UpdateObjective(PublicEventObjectiveType.Exterminate, 12671u, 1);

        Assert.Equal(PublicEventStatus.Failed, atDeadline.Status);
        Assert.Equal(0u, atDeadline.Count);
    }

    [Fact]
    public void DisableTheAlarm_ExactTargetGroupRequiresActivationAndMatchingPanelCredit()
    {
        PublicEventObjectiveEntry entry = new()
        {
            Id                              = 4648,
            Count                           = 0,
            ObjectId                        = 12478,
            WorldLocation2Id                = 41759,
            PublicEventTeamId               = PublicEventTeam.PublicTeam,
            PublicEventObjectiveFlags       = (PublicEventObjectiveFlag)512,
            PublicEventObjectiveCategoryEnum = PublicEventObjectiveCategory.Optional,
            PublicEventObjectiveTypeEnum    = PublicEventObjectiveType.ActivateTargetGroup
        };

        (NexusForever.Game.PublicEvent.PublicEventTeam team, PublicEventObjectiveModel objective)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);

        team.UpdateObjective(PublicEventObjectiveType.ActivateTargetGroup, 12478u, 1);

        Assert.Equal(PublicEventStatus.Inactive, objective.Status);
        Assert.Equal(0u, objective.Count);

        team.ActivateObjective(4648u, 1u);
        team.UpdateObjective(PublicEventObjectiveType.ActivateTargetGroupChecklist, 12478u, 1);
        team.UpdateObjective(PublicEventObjectiveType.ActivateTargetGroup, 12479u, 1);

        Assert.Equal(PublicEventStatus.Active, objective.Status);
        Assert.Equal(0u, objective.Count);
        Assert.Equal([41759u], objective.Build().Locations);

        team.UpdateObjective(PublicEventObjectiveType.ActivateTargetGroup, 12478u, 1);

        Assert.Equal(PublicEventStatus.Succeeded, objective.Status);
        Assert.Equal(1u, objective.Count);

        team.UpdateObjective(PublicEventObjectiveType.ActivateTargetGroup, 12478u, 1);

        Assert.Equal(PublicEventStatus.Succeeded, objective.Status);
        Assert.Equal(1u, objective.Count);
    }

    [Fact]
    public void WelcomeToTheThunderdome_ExactScriptWithoutCountBoundaryExposesMissingCadenceController()
    {
        PublicEventObjectiveEntry entry = new()
        {
            Id                              = 4649,
            Count                           = 0,
            FailureTimeMs                   = 5000,
            WorldLocation2Id                = 41745,
            PublicEventTeamId               = PublicEventTeam.PublicTeam,
            PublicEventObjectiveFlags       = (PublicEventObjectiveFlag)4,
            PublicEventObjectiveCategoryEnum = PublicEventObjectiveCategory.Optional,
            PublicEventObjectiveTypeEnum    = PublicEventObjectiveType.ScriptWithoutCount
        };

        (NexusForever.Game.PublicEvent.PublicEventTeam _, PublicEventObjectiveModel firstCrate)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);

        firstCrate.Team.UpdateObjective(4649u, 1);

        Assert.Equal(PublicEventStatus.Inactive, firstCrate.Status);
        Assert.Equal(0u, firstCrate.Count);

        firstCrate.Team.ActivateObjective(4649u, 0u);
        firstCrate.Update(4.999d);

        Assert.Equal(PublicEventStatus.Active, firstCrate.Status);
        Assert.Equal(0u, firstCrate.Count);
        Assert.Equal([41745u], firstCrate.Build().Locations);

        firstCrate.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Succeeded, firstCrate.Status);
        Assert.Equal(1u, firstCrate.Count);

        (NexusForever.Game.PublicEvent.PublicEventTeam _, PublicEventObjectiveModel missedCadence)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);
        missedCadence.Team.ActivateObjective(4649u, 0u);
        missedCadence.Update(5d);
        missedCadence.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Failed, missedCadence.Status);
        Assert.Equal(0u, missedCadence.Count);
    }

    [Fact]
    public void Deputy_ExactKillTargetGroupBoundaryRequiresActivationAndMatchingTarget()
    {
        PublicEventObjectiveEntry entry = new()
        {
            Id                              = 4657,
            Count                           = 1,
            ObjectId                        = 12528,
            WorldLocation2Id                = 41759,
            PublicEventTeamId               = PublicEventTeam.PublicTeam,
            PublicEventObjectiveFlags       = (PublicEventObjectiveFlag)1540,
            PublicEventObjectiveCategoryEnum = PublicEventObjectiveCategory.Optional,
            PublicEventObjectiveTypeEnum    = PublicEventObjectiveType.KillTargetGroup
        };

        (NexusForever.Game.PublicEvent.PublicEventTeam team, PublicEventObjectiveModel objective)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);

        team.UpdateObjective(PublicEventObjectiveType.KillTargetGroup, 12528u, 1);

        Assert.Equal(PublicEventStatus.Inactive, objective.Status);
        Assert.Equal(0u, objective.Count);

        team.ActivateObjective(4657u, 0u);
        team.UpdateObjective(PublicEventObjectiveType.KillClusterTargetGroup, 12528u, 1);
        team.UpdateObjective(PublicEventObjectiveType.KillTargetGroup, 12527u, 1);

        Assert.Equal(PublicEventStatus.Active, objective.Status);
        Assert.Equal(0u, objective.Count);
        Assert.Equal([41759u], objective.Build().Locations);

        team.UpdateObjective(PublicEventObjectiveType.KillTargetGroup, 12528u, 1);

        Assert.Equal(PublicEventStatus.Succeeded, objective.Status);
        Assert.Equal(1u, objective.Count);

        team.UpdateObjective(PublicEventObjectiveType.KillTargetGroup, 12528u, 1);

        Assert.Equal(PublicEventStatus.Succeeded, objective.Status);
        Assert.Equal(1u, objective.Count);
    }

    [Fact]
    public void DNTRoomNumberHolder_ExactInitialScriptCounterBoundaryDoesNotProveRoomEncoding()
    {
        PublicEventObjectiveEntry entry = new()
        {
            Id                              = 4659,
            Count                           = 10,
            PublicEventTeamId               = PublicEventTeam.PublicTeam,
            PublicEventObjectiveFlags       = (PublicEventObjectiveFlag)517,
            PublicEventObjectiveCategoryEnum = (PublicEventObjectiveCategory)10,
            PublicEventObjectiveTypeEnum    = PublicEventObjectiveType.Script
        };

        (NexusForever.Game.PublicEvent.PublicEventTeam team, PublicEventObjectiveModel objective)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);

        Assert.Equal(PublicEventStatus.Active, objective.Status);
        Assert.Empty(objective.Build().Locations);

        team.UpdateObjective(PublicEventObjectiveType.ScriptWithoutCount, 0u, 3);
        team.UpdateObjective(PublicEventObjectiveType.Script, 1u, 3);

        Assert.Equal(PublicEventStatus.Active, objective.Status);
        Assert.Equal(0u, objective.Count);

        team.UpdateObjective(PublicEventObjectiveType.Script, 0u, 3);

        Assert.Equal(PublicEventStatus.Active, objective.Status);
        Assert.Equal(3u, objective.Count);

        team.UpdateObjective(4659u, 7);

        Assert.Equal(PublicEventStatus.Succeeded, objective.Status);
        Assert.Equal(10u, objective.Count);

        team.UpdateObjective(PublicEventObjectiveType.Script, 0u, 1);

        Assert.Equal(PublicEventStatus.Succeeded, objective.Status);
        Assert.Equal(10u, objective.Count);
    }

    [Fact]
    public void DNTBossOneMedalHolder_ExactInitialScriptCounterBoundaryDoesNotProveMedalEncoding()
    {
        PublicEventObjectiveEntry entry = new()
        {
            Id                              = 4660,
            Count                           = 4,
            PublicEventTeamId               = PublicEventTeam.PublicTeam,
            PublicEventObjectiveFlags       = (PublicEventObjectiveFlag)517,
            PublicEventObjectiveCategoryEnum = (PublicEventObjectiveCategory)4,
            PublicEventObjectiveTypeEnum    = PublicEventObjectiveType.Script
        };

        (NexusForever.Game.PublicEvent.PublicEventTeam team, PublicEventObjectiveModel objective)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);

        Assert.Equal(PublicEventStatus.Active, objective.Status);
        Assert.Empty(objective.Build().Locations);

        team.UpdateObjective(PublicEventObjectiveType.ScriptWithoutCount, 0u, 1);
        team.UpdateObjective(PublicEventObjectiveType.Script, 1u, 1);

        Assert.Equal(PublicEventStatus.Active, objective.Status);
        Assert.Equal(0u, objective.Count);

        team.UpdateObjective(PublicEventObjectiveType.Script, 0u, 1);

        Assert.Equal(PublicEventStatus.Active, objective.Status);
        Assert.Equal(1u, objective.Count);

        team.UpdateObjective(4660u, 3);

        Assert.Equal(PublicEventStatus.Succeeded, objective.Status);
        Assert.Equal(4u, objective.Count);

        team.UpdateObjective(PublicEventObjectiveType.Script, 0u, 1);

        Assert.Equal(PublicEventStatus.Succeeded, objective.Status);
        Assert.Equal(4u, objective.Count);
    }

    [Fact]
    public void DNTBossTwoMedalHolder_ExactInitialScriptCounterBoundaryDoesNotProveMedalEncoding()
    {
        PublicEventObjectiveEntry entry = new()
        {
            Id                              = 4661,
            Count                           = 4,
            PublicEventTeamId               = PublicEventTeam.PublicTeam,
            PublicEventObjectiveFlags       = (PublicEventObjectiveFlag)517,
            PublicEventObjectiveCategoryEnum = (PublicEventObjectiveCategory)4,
            PublicEventObjectiveTypeEnum    = PublicEventObjectiveType.Script
        };

        (NexusForever.Game.PublicEvent.PublicEventTeam team, PublicEventObjectiveModel objective)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);

        Assert.Equal(PublicEventStatus.Active, objective.Status);
        Assert.Empty(objective.Build().Locations);

        team.UpdateObjective(PublicEventObjectiveType.ScriptWithoutCount, 0u, 1);
        team.UpdateObjective(PublicEventObjectiveType.Script, 1u, 1);

        Assert.Equal(PublicEventStatus.Active, objective.Status);
        Assert.Equal(0u, objective.Count);

        team.UpdateObjective(PublicEventObjectiveType.Script, 0u, 2);

        Assert.Equal(PublicEventStatus.Active, objective.Status);
        Assert.Equal(2u, objective.Count);

        team.UpdateObjective(4661u, 2);

        Assert.Equal(PublicEventStatus.Succeeded, objective.Status);
        Assert.Equal(4u, objective.Count);

        team.UpdateObjective(PublicEventObjectiveType.Script, 0u, 1);

        Assert.Equal(PublicEventStatus.Succeeded, objective.Status);
        Assert.Equal(4u, objective.Count);
    }

    [Fact]
    public void DNTBossThreeMedalHolder_ExactInitialScriptCounterBoundaryDoesNotProveMedalEncoding()
    {
        PublicEventObjectiveEntry entry = new()
        {
            Id                              = 4662,
            Count                           = 4,
            PublicEventTeamId               = PublicEventTeam.PublicTeam,
            PublicEventObjectiveFlags       = (PublicEventObjectiveFlag)517,
            PublicEventObjectiveCategoryEnum = (PublicEventObjectiveCategory)4,
            PublicEventObjectiveTypeEnum    = PublicEventObjectiveType.Script
        };

        (NexusForever.Game.PublicEvent.PublicEventTeam team, PublicEventObjectiveModel objective)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);

        Assert.Equal(PublicEventStatus.Active, objective.Status);
        Assert.Empty(objective.Build().Locations);

        team.UpdateObjective(PublicEventObjectiveType.ScriptWithoutCount, 0u, 1);
        team.UpdateObjective(PublicEventObjectiveType.Script, 1u, 1);

        Assert.Equal(PublicEventStatus.Active, objective.Status);
        Assert.Equal(0u, objective.Count);

        team.UpdateObjective(PublicEventObjectiveType.Script, 0u, 3);

        Assert.Equal(PublicEventStatus.Active, objective.Status);
        Assert.Equal(3u, objective.Count);

        team.UpdateObjective(4662u, 1);

        Assert.Equal(PublicEventStatus.Succeeded, objective.Status);
        Assert.Equal(4u, objective.Count);

        team.UpdateObjective(PublicEventObjectiveType.Script, 0u, 1);

        Assert.Equal(PublicEventStatus.Succeeded, objective.Status);
        Assert.Equal(4u, objective.Count);
    }

    [Fact]
    public void LostAndFoundExterminateHolder4737_ExactZeroObjectBoundaryRejectsOrdinaryTargetGroups()
    {
        PublicEventObjectiveEntry entry = new()
        {
            Id                              = 4737,
            Count                           = 0,
            WorldLocation2Id                = 41745,
            PublicEventTeamId               = PublicEventTeam.PublicTeam,
            PublicEventObjectiveFlags       = (PublicEventObjectiveFlag)8708,
            PublicEventObjectiveCategoryEnum = PublicEventObjectiveCategory.Optional,
            PublicEventObjectiveTypeEnum    = PublicEventObjectiveType.Exterminate
        };

        (NexusForever.Game.PublicEvent.PublicEventTeam team, PublicEventObjectiveModel objective)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);

        team.UpdateObjective(PublicEventObjectiveType.Exterminate, 0u, 1);

        Assert.Equal(PublicEventStatus.Inactive, objective.Status);
        Assert.Equal(0u, objective.Count);

        team.ActivateObjective(4737u, 0u);
        team.UpdateObjective(PublicEventObjectiveType.KillTargetGroup, 0u, 1);
        team.UpdateObjective(PublicEventObjectiveType.Exterminate, 12528u, 1);

        Assert.Equal(PublicEventStatus.Active, objective.Status);
        Assert.Equal(0u, objective.Count);
        Assert.Equal([41745u], objective.Build().Locations);

        team.UpdateObjective(PublicEventObjectiveType.Exterminate, 0u, 1);

        Assert.Equal(PublicEventStatus.Succeeded, objective.Status);
        Assert.Equal(1u, objective.Count);

        team.UpdateObjective(PublicEventObjectiveType.Exterminate, 0u, 1);

        Assert.Equal(PublicEventStatus.Succeeded, objective.Status);
        Assert.Equal(1u, objective.Count);
    }

    [Fact]
    public void LostAndFoundExterminateHolder4738_ExactZeroObjectBoundaryRejectsOrdinaryTargetGroups()
    {
        PublicEventObjectiveEntry entry = new()
        {
            Id                              = 4738,
            Count                           = 0,
            WorldLocation2Id                = 41745,
            PublicEventTeamId               = PublicEventTeam.PublicTeam,
            PublicEventObjectiveFlags       = (PublicEventObjectiveFlag)8708,
            PublicEventObjectiveCategoryEnum = PublicEventObjectiveCategory.Optional,
            PublicEventObjectiveTypeEnum    = PublicEventObjectiveType.Exterminate
        };

        (NexusForever.Game.PublicEvent.PublicEventTeam team, PublicEventObjectiveModel objective)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);

        team.UpdateObjective(PublicEventObjectiveType.Exterminate, 0u, 1);

        Assert.Equal(PublicEventStatus.Inactive, objective.Status);
        Assert.Equal(0u, objective.Count);

        team.ActivateObjective(4738u, 0u);
        team.UpdateObjective(PublicEventObjectiveType.KillTargetGroup, 0u, 1);
        team.UpdateObjective(PublicEventObjectiveType.Exterminate, 12528u, 1);

        Assert.Equal(PublicEventStatus.Active, objective.Status);
        Assert.Equal(0u, objective.Count);
        Assert.Equal([41745u], objective.Build().Locations);

        team.UpdateObjective(PublicEventObjectiveType.Exterminate, 0u, 1);

        Assert.Equal(PublicEventStatus.Succeeded, objective.Status);
        Assert.Equal(1u, objective.Count);

        team.UpdateObjective(PublicEventObjectiveType.Exterminate, 0u, 1);

        Assert.Equal(PublicEventStatus.Succeeded, objective.Status);
        Assert.Equal(1u, objective.Count);
    }

    [Fact]
    public void LostAndFoundExterminateHolder4739_ExactZeroObjectBoundaryRejectsOrdinaryTargetGroups()
    {
        PublicEventObjectiveEntry entry = new()
        {
            Id                              = 4739,
            Count                           = 0,
            WorldLocation2Id                = 41745,
            PublicEventTeamId               = PublicEventTeam.PublicTeam,
            PublicEventObjectiveFlags       = (PublicEventObjectiveFlag)8708,
            PublicEventObjectiveCategoryEnum = PublicEventObjectiveCategory.Optional,
            PublicEventObjectiveTypeEnum    = PublicEventObjectiveType.Exterminate
        };

        (NexusForever.Game.PublicEvent.PublicEventTeam team, PublicEventObjectiveModel objective)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);

        team.UpdateObjective(PublicEventObjectiveType.Exterminate, 0u, 1);

        Assert.Equal(PublicEventStatus.Inactive, objective.Status);
        Assert.Equal(0u, objective.Count);

        team.ActivateObjective(4739u, 0u);
        team.UpdateObjective(PublicEventObjectiveType.KillTargetGroup, 0u, 1);
        team.UpdateObjective(PublicEventObjectiveType.Exterminate, 12528u, 1);

        Assert.Equal(PublicEventStatus.Active, objective.Status);
        Assert.Equal(0u, objective.Count);
        Assert.Equal([41745u], objective.Build().Locations);

        team.UpdateObjective(PublicEventObjectiveType.Exterminate, 0u, 1);

        Assert.Equal(PublicEventStatus.Succeeded, objective.Status);
        Assert.Equal(1u, objective.Count);

        team.UpdateObjective(PublicEventObjectiveType.Exterminate, 0u, 1);

        Assert.Equal(PublicEventStatus.Succeeded, objective.Status);
        Assert.Equal(1u, objective.Count);
    }

    [Fact]
    public void LostAndFoundExterminateHolder4740_ExactZeroObjectBoundaryRejectsOrdinaryTargetGroups()
    {
        PublicEventObjectiveEntry entry = new()
        {
            Id                              = 4740,
            Count                           = 0,
            WorldLocation2Id                = 41745,
            PublicEventTeamId               = PublicEventTeam.PublicTeam,
            PublicEventObjectiveFlags       = (PublicEventObjectiveFlag)8708,
            PublicEventObjectiveCategoryEnum = PublicEventObjectiveCategory.Optional,
            PublicEventObjectiveTypeEnum    = PublicEventObjectiveType.Exterminate
        };

        (NexusForever.Game.PublicEvent.PublicEventTeam team, PublicEventObjectiveModel objective)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);

        team.UpdateObjective(PublicEventObjectiveType.Exterminate, 0u, 1);

        Assert.Equal(PublicEventStatus.Inactive, objective.Status);
        Assert.Equal(0u, objective.Count);

        team.ActivateObjective(4740u, 0u);
        team.UpdateObjective(PublicEventObjectiveType.KillTargetGroup, 0u, 1);
        team.UpdateObjective(PublicEventObjectiveType.Exterminate, 12528u, 1);

        Assert.Equal(PublicEventStatus.Active, objective.Status);
        Assert.Equal(0u, objective.Count);
        Assert.Equal([41745u], objective.Build().Locations);

        team.UpdateObjective(PublicEventObjectiveType.Exterminate, 0u, 1);

        Assert.Equal(PublicEventStatus.Succeeded, objective.Status);
        Assert.Equal(1u, objective.Count);

        team.UpdateObjective(PublicEventObjectiveType.Exterminate, 0u, 1);

        Assert.Equal(PublicEventStatus.Succeeded, objective.Status);
        Assert.Equal(1u, objective.Count);
    }

    [Fact]
    public void LostAndFoundParticipantsHolder4741_ExactZeroCountBoundaryDoesNotProveTriggerPlacement()
    {
        PublicEventObjectiveEntry entry = new()
        {
            Id                              = 4741,
            Count                           = 0,
            ObjectId                        = 8105,
            PublicEventTeamId               = PublicEventTeam.PublicTeam,
            PublicEventObjectiveFlags       = (PublicEventObjectiveFlag)517,
            PublicEventObjectiveCategoryEnum = PublicEventObjectiveCategory.Main,
            PublicEventObjectiveTypeEnum    = PublicEventObjectiveType.ParticipantsInTriggerVolume
        };

        (NexusForever.Game.PublicEvent.PublicEventTeam entryTeam, PublicEventObjectiveModel entryObjective)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);

        Assert.Equal(PublicEventStatus.Active, entryObjective.Status);
        Assert.Empty(entryObjective.Build().Locations);

        entryTeam.UpdateObjective(PublicEventObjectiveType.Turnstile, 8105u, 1);
        entryTeam.UpdateObjective(PublicEventObjectiveType.ParticipantsInTriggerVolume, 8104u, 1);

        Assert.Equal(PublicEventStatus.Active, entryObjective.Status);
        Assert.Equal(0u, entryObjective.Count);

        entryTeam.UpdateObjective(PublicEventObjectiveType.ParticipantsInTriggerVolume, 8105u, 1);

        Assert.Equal(PublicEventStatus.Succeeded, entryObjective.Status);
        Assert.Equal(1u, entryObjective.Count);

        (NexusForever.Game.PublicEvent.PublicEventTeam leaveTeam, PublicEventObjectiveModel leaveObjective)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);

        leaveTeam.UpdateObjective(PublicEventObjectiveType.ParticipantsInTriggerVolume, 8105u, -1);

        Assert.Equal(PublicEventStatus.Succeeded, leaveObjective.Status);
        Assert.Equal(0u, leaveObjective.Count);
    }

    [Fact]
    public void HiddenPrototentiaryEventObjective4742_ExactBoundaryRejectsCurrentWardenDeathDispatchKeys()
    {
        PublicEventObjectiveEntry entry = new()
        {
            Id                              = 4742,
            Count                           = 0,
            ObjectId                        = 10569,
            WorldLocation2Id                = 49683,
            PublicEventTeamId               = PublicEventTeam.PublicTeam,
            PublicEventObjectiveFlags       = (PublicEventObjectiveFlag)517,
            PublicEventObjectiveCategoryEnum = PublicEventObjectiveCategory.Optional,
            PublicEventObjectiveTypeEnum    = PublicEventObjectiveType.KillEventObjectiveUnit
        };

        (NexusForever.Game.PublicEvent.PublicEventTeam team, PublicEventObjectiveModel objective)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);

        Assert.Equal(PublicEventStatus.Active, objective.Status);

        team.UpdateObjective(PublicEventObjectiveType.KillEventUnit, 10569u, 1);
        team.UpdateObjective(PublicEventObjectiveType.KillEventObjectiveUnit, 0u, 1);
        team.UpdateObjective(PublicEventObjectiveType.KillEventObjectiveUnit, 12474u, 1);

        Assert.Equal(PublicEventStatus.Active, objective.Status);
        Assert.Equal(0u, objective.Count);
        Assert.Equal([49683u], objective.Build().Locations);

        team.UpdateObjective(PublicEventObjectiveType.KillEventObjectiveUnit, 10569u, 1);

        Assert.Equal(PublicEventStatus.Succeeded, objective.Status);
        Assert.Equal(1u, objective.Count);

        team.UpdateObjective(PublicEventObjectiveType.KillEventObjectiveUnit, 10569u, 1);

        Assert.Equal(PublicEventStatus.Succeeded, objective.Status);
        Assert.Equal(1u, objective.Count);
    }

    [Fact]
    public void HiddenPowerPlungeKillHolder4771_ExactCounterBoundaryRequiresMatchingCubigTargetGroup()
    {
        PublicEventObjectiveEntry entry = new()
        {
            Id                              = 4771,
            Count                           = uint.MaxValue,
            ObjectId                        = 12871,
            PublicEventTeamId               = PublicEventTeam.PublicTeam,
            PublicEventObjectiveFlags       = (PublicEventObjectiveFlag)517,
            PublicEventObjectiveCategoryEnum = PublicEventObjectiveCategory.Main,
            PublicEventObjectiveTypeEnum    = PublicEventObjectiveType.KillTargetGroup
        };

        (NexusForever.Game.PublicEvent.PublicEventTeam team, PublicEventObjectiveModel objective)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);

        Assert.Equal(PublicEventStatus.Active, objective.Status);
        Assert.Empty(objective.Build().Locations);

        team.UpdateObjective(PublicEventObjectiveType.KillClusterTargetGroup, 12871u, 1);
        team.UpdateObjective(PublicEventObjectiveType.KillTargetGroup, 12870u, 1);

        Assert.Equal(PublicEventStatus.Active, objective.Status);
        Assert.Equal(0u, objective.Count);

        team.UpdateObjective(PublicEventObjectiveType.KillTargetGroup, 12871u, 1);
        team.UpdateObjective(PublicEventObjectiveType.KillTargetGroup, 12871u, 1);

        Assert.Equal(PublicEventStatus.Active, objective.Status);
        Assert.Equal(2u, objective.Count);
        Assert.False(team.IsFinialised);

        team.UpdateObjective(PublicEventObjectiveType.KillTargetGroup, 12871u, -3);

        Assert.Equal(PublicEventStatus.Active, objective.Status);
        Assert.Equal(0u, objective.Count);
    }

    [Fact]
    public void HiddenElementalHospitalKillHolder4777_ExactCounterBoundaryRequiresMatchingNestedTargetGroup()
    {
        PublicEventObjectiveEntry entry = new()
        {
            Id                              = 4777,
            Count                           = uint.MaxValue,
            ObjectId                        = 12876,
            PublicEventTeamId               = PublicEventTeam.PublicTeam,
            PublicEventObjectiveFlags       = (PublicEventObjectiveFlag)517,
            PublicEventObjectiveCategoryEnum = PublicEventObjectiveCategory.Main,
            PublicEventObjectiveTypeEnum    = PublicEventObjectiveType.KillTargetGroup
        };

        (NexusForever.Game.PublicEvent.PublicEventTeam team, PublicEventObjectiveModel objective)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);

        Assert.Equal(PublicEventStatus.Active, objective.Status);
        Assert.Empty(objective.Build().Locations);

        team.UpdateObjective(PublicEventObjectiveType.KillClusterTargetGroup, 12876u, 1);
        team.UpdateObjective(PublicEventObjectiveType.KillTargetGroup, 12878u, 1);

        Assert.Equal(PublicEventStatus.Active, objective.Status);
        Assert.Equal(0u, objective.Count);

        team.UpdateObjective(PublicEventObjectiveType.KillTargetGroup, 12876u, 1);
        team.UpdateObjective(PublicEventObjectiveType.KillTargetGroup, 12876u, 1);

        Assert.Equal(PublicEventStatus.Active, objective.Status);
        Assert.Equal(2u, objective.Count);
        Assert.False(team.IsFinialised);

        team.UpdateObjective(PublicEventObjectiveType.KillTargetGroup, 12876u, -3);

        Assert.Equal(PublicEventStatus.Active, objective.Status);
        Assert.Equal(0u, objective.Count);
    }

    [Fact]
    public void RookieBonusObjective5182_ExactScriptBoundaryRequiresControllerActivationAndMatchingCredit()
    {
        PublicEventObjectiveEntry entry = new()
        {
            Id                              = 5182,
            Count                           = 1,
            ObjectId                        = 0,
            PublicEventTeamId               = PublicEventTeam.PublicTeam,
            PublicEventObjectiveFlags       = (PublicEventObjectiveFlag)4,
            PublicEventObjectiveCategoryEnum = PublicEventObjectiveCategory.Challenge,
            PublicEventObjectiveTypeEnum    = PublicEventObjectiveType.Script
        };

        (NexusForever.Game.PublicEvent.PublicEventTeam team, PublicEventObjectiveModel objective)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);

        Assert.Equal(PublicEventStatus.Inactive, objective.Status);
        Assert.Empty(objective.Build().Locations);

        team.UpdateObjective(5182u, 1);
        team.UpdateObjective(PublicEventObjectiveType.Script, 0u, 1);

        Assert.Equal(PublicEventStatus.Inactive, objective.Status);
        Assert.Equal(0u, objective.Count);

        team.ActivateObjective(5182u, 0u);
        team.UpdateObjective(PublicEventObjectiveType.ResourcePool, 0u, 1);
        team.UpdateObjective(PublicEventObjectiveType.Script, 1u, 1);

        Assert.Equal(PublicEventStatus.Active, objective.Status);
        Assert.Equal(0u, objective.Count);

        team.UpdateObjective(PublicEventObjectiveType.Script, 0u, 1);

        Assert.Equal(PublicEventStatus.Succeeded, objective.Status);
        Assert.Equal(1u, objective.Count);

        team.UpdateObjective(5182u, 1);

        Assert.Equal(PublicEventStatus.Succeeded, objective.Status);
        Assert.Equal(1u, objective.Count);
    }

    [Fact]
    public void SpringCleaning_SharedTimerBoundarySucceedsBeforeSixMinutesAndRejectsLateCredit()
    {
        PublicEventObjectiveEntry entry = new()
        {
            Id                              = 2921,
            Count                           = 0,
            FailureTimeMs                   = 360000,
            PublicEventTeamId               = PublicEventTeam.PublicTeam,
            PublicEventObjectiveFlags       = (PublicEventObjectiveFlag)4,
            PublicEventObjectiveCategoryEnum = PublicEventObjectiveCategory.Optional,
            PublicEventObjectiveTypeEnum    = PublicEventObjectiveType.Script
        };

        (NexusForever.Game.PublicEvent.PublicEventTeam _, PublicEventObjectiveModel beforeDeadline)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);
        beforeDeadline.Team.ActivateObjective(2921u, 0u);
        beforeDeadline.Update(359.999d);
        beforeDeadline.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Succeeded, beforeDeadline.Status);
        Assert.Equal(359999u, beforeDeadline.Build().ElapsedTimeMs);

        (NexusForever.Game.PublicEvent.PublicEventTeam _, PublicEventObjectiveModel afterDeadline)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);
        afterDeadline.Team.ActivateObjective(2921u, 0u);
        afterDeadline.Update(360d);
        afterDeadline.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Failed, afterDeadline.Status);
        Assert.Equal(0u, afterDeadline.Count);
    }

    [Fact]
    public void MondosCrate_ExactSixtySecondBoundaryRequiresOneCrateDeathBeforeDeadline()
    {
        PublicEventObjectiveEntry entry = new()
        {
            Id                               = 2920,
            Count                            = 0,
            FailureTimeMs                    = 60000,
            WorldLocation2Id                 = 41745,
            PublicEventTeamId                = PublicEventTeam.PublicTeam,
            PublicEventObjectiveFlags        = (PublicEventObjectiveFlag)4,
            PublicEventObjectiveCategoryEnum = PublicEventObjectiveCategory.Challenge,
            PublicEventObjectiveTypeEnum     = PublicEventObjectiveType.Script
        };

        (NexusForever.Game.PublicEvent.PublicEventTeam _, PublicEventObjectiveModel beforeDeadline)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);
        beforeDeadline.Team.ActivateObjective(2920u, 1u);
        beforeDeadline.Update(59.999d);

        Assert.Equal(PublicEventStatus.Active, beforeDeadline.Status);
        Assert.Equal(0u, beforeDeadline.Count);
        Assert.Equal([41745u], beforeDeadline.Build().Locations);

        beforeDeadline.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Succeeded, beforeDeadline.Status);
        Assert.Equal(1u, beforeDeadline.Count);
        Assert.Equal(59999u, beforeDeadline.Build().ElapsedTimeMs);

        (NexusForever.Game.PublicEvent.PublicEventTeam _, PublicEventObjectiveModel atDeadline)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);
        atDeadline.Team.ActivateObjective(2920u, 1u);
        atDeadline.Update(60d);
        atDeadline.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Failed, atDeadline.Status);
        Assert.Equal(0u, atDeadline.Count);
    }

    [Fact]
    public void QuickReflexes_ExactTimedChildRequiresMonstrosityDeathBeforeSixtySeconds()
    {
        PublicEventObjectiveEntry entry = new()
        {
            Id                               = 2941,
            Count                            = 1,
            FailureTimeMs                    = 60000,
            WorldLocation2Id                 = 41745,
            PublicEventTeamId                = PublicEventTeam.PublicTeam,
            PublicEventObjectiveFlags        = (PublicEventObjectiveFlag)4,
            PublicEventObjectiveCategoryEnum = PublicEventObjectiveCategory.Challenge,
            PublicEventObjectiveTypeEnum     = PublicEventObjectiveType.TimedWin
        };

        (NexusForever.Game.PublicEvent.PublicEventTeam _, PublicEventObjectiveModel beforeDeadline)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);
        beforeDeadline.Team.ActivateObjective(2941u, 0u);
        beforeDeadline.Update(59.999d);

        Assert.Equal(PublicEventStatus.Active, beforeDeadline.Status);
        Assert.Equal(0u, beforeDeadline.Count);
        Assert.Equal([41745u], beforeDeadline.Build().Locations);

        beforeDeadline.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Succeeded, beforeDeadline.Status);
        Assert.Equal(1u, beforeDeadline.Count);
        Assert.Equal(59999u, beforeDeadline.Build().ElapsedTimeMs);

        (NexusForever.Game.PublicEvent.PublicEventTeam _, PublicEventObjectiveModel atDeadline)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);
        atDeadline.Team.ActivateObjective(2941u, 0u);
        atDeadline.Update(60d);
        atDeadline.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Failed, atDeadline.Status);
        Assert.Equal(0u, atDeadline.Count);
    }

    [Fact]
    public void RapidFire_SharedTimerBoundaryRequiresThreeCreditsBeforeTenSecondsAndRejectsLateThirdCredit()
    {
        PublicEventObjectiveEntry entry = new()
        {
            Id                              = 2922,
            Count                           = 3,
            FailureTimeMs                   = 10000,
            PublicEventTeamId               = PublicEventTeam.PublicTeam,
            PublicEventObjectiveFlags       = (PublicEventObjectiveFlag)4,
            PublicEventObjectiveCategoryEnum = PublicEventObjectiveCategory.Challenge,
            PublicEventObjectiveTypeEnum    = PublicEventObjectiveType.Script
        };

        (NexusForever.Game.PublicEvent.PublicEventTeam _, PublicEventObjectiveModel beforeDeadline)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);
        beforeDeadline.Team.ActivateObjective(2922u, 0u);
        beforeDeadline.UpdateObjective(2);
        beforeDeadline.Update(9.999d);
        beforeDeadline.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Succeeded, beforeDeadline.Status);
        Assert.Equal(3u, beforeDeadline.Count);
        Assert.Equal(9999u, beforeDeadline.Build().ElapsedTimeMs);

        (NexusForever.Game.PublicEvent.PublicEventTeam _, PublicEventObjectiveModel afterDeadline)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);
        afterDeadline.Team.ActivateObjective(2922u, 0u);
        afterDeadline.UpdateObjective(2);
        afterDeadline.Update(10d);
        afterDeadline.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Failed, afterDeadline.Status);
        Assert.Equal(2u, afterDeadline.Count);
    }

    [Fact]
    public void Kick10Marauders_RequiresTenCreditsAndRejectsPostSuccessCredit()
    {
        PublicEventObjectiveEntry entry = new()
        {
            Id                              = 2923,
            Count                           = 10,
            PublicEventTeamId               = PublicEventTeam.PublicTeam,
            PublicEventObjectiveFlags       = (PublicEventObjectiveFlag)4,
            PublicEventObjectiveCategoryEnum = PublicEventObjectiveCategory.Challenge,
            PublicEventObjectiveTypeEnum    = PublicEventObjectiveType.Script
        };

        (NexusForever.Game.PublicEvent.PublicEventTeam team, PublicEventObjectiveModel objective)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);
        team.ActivateObjective(2923u, 0u);
        objective.UpdateObjective(9);

        Assert.Equal(PublicEventStatus.Active, objective.Status);
        Assert.Equal(9u, objective.Count);

        objective.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Succeeded, objective.Status);
        Assert.Equal(10u, objective.Count);

        objective.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Succeeded, objective.Status);
        Assert.Equal(10u, objective.Count);
    }

    [Fact]
    public void DustStorm_SharedTimerBoundaryRequiresTwentyCreditsBeforeTwentySecondsAndRejectsLateCredit()
    {
        PublicEventObjectiveEntry entry = new()
        {
            Id                              = 2924,
            Count                           = 20,
            FailureTimeMs                   = 20000,
            PublicEventTeamId               = PublicEventTeam.PublicTeam,
            PublicEventObjectiveFlags       = (PublicEventObjectiveFlag)4,
            PublicEventObjectiveCategoryEnum = PublicEventObjectiveCategory.Challenge,
            PublicEventObjectiveTypeEnum    = PublicEventObjectiveType.Script
        };

        (NexusForever.Game.PublicEvent.PublicEventTeam _, PublicEventObjectiveModel beforeDeadline)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);
        beforeDeadline.Team.ActivateObjective(2924u, 0u);
        beforeDeadline.UpdateObjective(19);
        beforeDeadline.Update(19.999d);
        beforeDeadline.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Succeeded, beforeDeadline.Status);
        Assert.Equal(20u, beforeDeadline.Count);
        Assert.Equal(19999u, beforeDeadline.Build().ElapsedTimeMs);

        (NexusForever.Game.PublicEvent.PublicEventTeam _, PublicEventObjectiveModel afterDeadline)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);
        afterDeadline.Team.ActivateObjective(2924u, 0u);
        afterDeadline.UpdateObjective(19);
        afterDeadline.Update(20d);
        afterDeadline.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Failed, afterDeadline.Status);
        Assert.Equal(19u, afterDeadline.Count);
    }

    [Fact]
    public void SplorgSpree_ResourcePoolRequiresSevenKillsBeforeTwentySecondsAndRejectsLateCredit()
    {
        PublicEventObjectiveEntry entry = new()
        {
            Id                              = 2925,
            Count                           = 7,
            FailureTimeMs                   = 20000,
            PublicEventTeamId               = PublicEventTeam.PublicTeam,
            PublicEventObjectiveFlags       = (PublicEventObjectiveFlag)4,
            PublicEventObjectiveCategoryEnum = PublicEventObjectiveCategory.Challenge,
            PublicEventObjectiveTypeEnum    = PublicEventObjectiveType.ResourcePool
        };

        (NexusForever.Game.PublicEvent.PublicEventTeam _, PublicEventObjectiveModel beforeDeadline)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);
        beforeDeadline.Team.ActivateObjective(2925u, 0u);
        beforeDeadline.UpdateObjective(6);
        beforeDeadline.Update(19.999d);
        beforeDeadline.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Succeeded, beforeDeadline.Status);
        Assert.Equal(7u, beforeDeadline.Count);
        Assert.Equal(19999u, beforeDeadline.Build().ElapsedTimeMs);

        (NexusForever.Game.PublicEvent.PublicEventTeam _, PublicEventObjectiveModel afterDeadline)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);
        afterDeadline.Team.ActivateObjective(2925u, 0u);
        afterDeadline.UpdateObjective(6);
        afterDeadline.Update(20d);
        afterDeadline.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Failed, afterDeadline.Status);
        Assert.Equal(6u, afterDeadline.Count);
    }

    [Fact]
    public void RowsdowerRoundUp_ResourcePoolRequiresSixKillsAndRejectsPostSuccessCredit()
    {
        PublicEventObjectiveEntry entry = new()
        {
            Id                              = 2927,
            Count                           = 6,
            PublicEventTeamId               = PublicEventTeam.PublicTeam,
            PublicEventObjectiveFlags       = (PublicEventObjectiveFlag)4,
            PublicEventObjectiveCategoryEnum = PublicEventObjectiveCategory.Challenge,
            PublicEventObjectiveTypeEnum    = PublicEventObjectiveType.ResourcePool
        };

        (NexusForever.Game.PublicEvent.PublicEventTeam _, PublicEventObjectiveModel objective)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);
        objective.Team.ActivateObjective(2927u, 0u);
        objective.UpdateObjective(5);

        Assert.Equal(PublicEventStatus.Active, objective.Status);
        Assert.Equal(5u, objective.Count);

        objective.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Succeeded, objective.Status);
        Assert.Equal(6u, objective.Count);

        objective.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Succeeded, objective.Status);
        Assert.Equal(6u, objective.Count);
    }

    [Fact]
    public void KingsAndQueensOfTheHill_ScriptOptionalWaitsForExplicitControllerCompletion()
    {
        PublicEventObjectiveEntry entry = new()
        {
            Id                              = 2928,
            Count                           = 0,
            WorldLocation2Id                = 41745,
            PublicEventTeamId               = PublicEventTeam.PublicTeam,
            PublicEventObjectiveFlags       = (PublicEventObjectiveFlag)4,
            PublicEventObjectiveCategoryEnum = PublicEventObjectiveCategory.Optional,
            PublicEventObjectiveTypeEnum    = PublicEventObjectiveType.Script
        };

        (NexusForever.Game.PublicEvent.PublicEventTeam _, PublicEventObjectiveModel objective)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);
        objective.Team.ActivateObjective(2928u, 0u);
        objective.Update(30d);

        Assert.Equal(PublicEventStatus.Active, objective.Status);
        Assert.Equal(0u, objective.Count);
        Assert.Equal([41745u], objective.Build().Locations);

        objective.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Succeeded, objective.Status);
        Assert.Equal(1u, objective.Count);
    }

    [Fact]
    public void FriendOfCrate_TimedWinRequiresControllerSuccessBeforeThirtySecondsAndRejectsLateCredit()
    {
        PublicEventObjectiveEntry entry = new()
        {
            Id                              = 2929,
            Count                           = 0,
            FailureTimeMs                   = 30000,
            WorldLocation2Id                = 41745,
            PublicEventTeamId               = PublicEventTeam.PublicTeam,
            PublicEventObjectiveFlags       = (PublicEventObjectiveFlag)4,
            PublicEventObjectiveCategoryEnum = PublicEventObjectiveCategory.Challenge,
            PublicEventObjectiveTypeEnum    = PublicEventObjectiveType.TimedWin
        };

        (NexusForever.Game.PublicEvent.PublicEventTeam _, PublicEventObjectiveModel beforeDeadline)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);
        beforeDeadline.Team.ActivateObjective(2929u, 0u);
        beforeDeadline.Update(29.999d);

        Assert.Equal(PublicEventStatus.Active, beforeDeadline.Status);
        Assert.Equal([41745u], beforeDeadline.Build().Locations);

        beforeDeadline.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Succeeded, beforeDeadline.Status);
        Assert.Equal(29999u, beforeDeadline.Build().ElapsedTimeMs);

        (NexusForever.Game.PublicEvent.PublicEventTeam _, PublicEventObjectiveModel afterDeadline)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);
        afterDeadline.Team.ActivateObjective(2929u, 0u);
        afterDeadline.Update(30d);
        afterDeadline.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Failed, afterDeadline.Status);
        Assert.Equal(0u, afterDeadline.Count);
    }

    [Fact]
    public void DamageControl_ResourcePoolRequiresTenControllerCreditsAndRejectsPostSuccessCredit()
    {
        PublicEventObjectiveEntry entry = new()
        {
            Id                              = 2930,
            Count                           = 10,
            FailureTimeMs                   = 0,
            PublicEventTeamId               = PublicEventTeam.PublicTeam,
            PublicEventObjectiveFlags       = (PublicEventObjectiveFlag)4,
            PublicEventObjectiveCategoryEnum = PublicEventObjectiveCategory.Challenge,
            PublicEventObjectiveTypeEnum    = PublicEventObjectiveType.ResourcePool
        };

        (NexusForever.Game.PublicEvent.PublicEventTeam team, PublicEventObjectiveModel objective)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);
        team.ActivateObjective(2930u, 0u);
        objective.UpdateObjective(9);
        objective.Update(600d);

        Assert.Equal(PublicEventStatus.Active, objective.Status);
        Assert.Equal(9u, objective.Count);

        objective.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Succeeded, objective.Status);
        Assert.Equal(10u, objective.Count);

        objective.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Succeeded, objective.Status);
        Assert.Equal(10u, objective.Count);
    }

    [Fact]
    public void SpeedySlaughterfest_ScriptOptionalRequiresControllerSuccessBefore270SecondsAndRejectsLateCredit()
    {
        PublicEventObjectiveEntry entry = new()
        {
            Id                              = 2947,
            Count                           = 0,
            FailureTimeMs                   = 270000,
            PublicEventTeamId               = PublicEventTeam.PublicTeam,
            PublicEventObjectiveFlags       = (PublicEventObjectiveFlag)4,
            PublicEventObjectiveCategoryEnum = PublicEventObjectiveCategory.Optional,
            PublicEventObjectiveTypeEnum    = PublicEventObjectiveType.Script
        };

        (NexusForever.Game.PublicEvent.PublicEventTeam _, PublicEventObjectiveModel beforeDeadline)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);
        beforeDeadline.Team.ActivateObjective(2947u, 0u);
        beforeDeadline.Update(269.999d);

        Assert.Equal(PublicEventStatus.Active, beforeDeadline.Status);

        beforeDeadline.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Succeeded, beforeDeadline.Status);
        Assert.Equal(269999u, beforeDeadline.Build().ElapsedTimeMs);

        (NexusForever.Game.PublicEvent.PublicEventTeam _, PublicEventObjectiveModel afterDeadline)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);
        afterDeadline.Team.ActivateObjective(2947u, 0u);
        afterDeadline.Update(270d);
        afterDeadline.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Failed, afterDeadline.Status);
        Assert.Equal(0u, afterDeadline.Count);
    }

    [Fact]
    public void CleanSweep_ResourcePoolRequiresFiveControllerCreditsAndRejectsPostSuccessCredit()
    {
        PublicEventObjectiveEntry entry = new()
        {
            Id                              = 2951,
            Count                           = 5,
            FailureTimeMs                   = 0,
            PublicEventTeamId               = PublicEventTeam.PublicTeam,
            PublicEventObjectiveFlags       = (PublicEventObjectiveFlag)4,
            PublicEventObjectiveCategoryEnum = PublicEventObjectiveCategory.Challenge,
            PublicEventObjectiveTypeEnum    = PublicEventObjectiveType.ResourcePool
        };

        (NexusForever.Game.PublicEvent.PublicEventTeam team, PublicEventObjectiveModel objective)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);
        team.ActivateObjective(2951u, 0u);
        objective.UpdateObjective(4);
        objective.Update(600d);

        Assert.Equal(PublicEventStatus.Active, objective.Status);
        Assert.Equal(4u, objective.Count);

        objective.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Succeeded, objective.Status);
        Assert.Equal(5u, objective.Count);

        objective.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Succeeded, objective.Status);
        Assert.Equal(5u, objective.Count);
    }

    [Fact]
    public void AcceleratedEradication_ScriptOptionalDoesNotUseObjectiveWideTwentyFiveSecondTimer()
    {
        PublicEventObjectiveEntry entry = new()
        {
            Id                              = 2952,
            Count                           = 0,
            FailureTimeMs                   = 0,
            WorldLocation2Id                = 41756,
            PublicEventTeamId               = PublicEventTeam.PublicTeam,
            PublicEventObjectiveFlags       = (PublicEventObjectiveFlag)4,
            PublicEventObjectiveCategoryEnum = PublicEventObjectiveCategory.Optional,
            PublicEventObjectiveTypeEnum    = PublicEventObjectiveType.Script
        };

        (NexusForever.Game.PublicEvent.PublicEventTeam team, PublicEventObjectiveModel objective)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);
        team.ActivateObjective(2952u, 0u);
        objective.Update(25.001d);

        Assert.Equal(PublicEventStatus.Active, objective.Status);
        Assert.Equal(0u, objective.Count);
        Assert.Equal([41756u], objective.Build().Locations);

        objective.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Succeeded, objective.Status);
        Assert.Equal(1u, objective.Count);

        objective.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Succeeded, objective.Status);
        Assert.Equal(1u, objective.Count);
    }

    [Fact]
    public void SixthSense_CurrentGenericScriptBoundaryTreatsFiveCreditsAsSuccessBeforeTwentyFiveSecondFailure()
    {
        PublicEventObjectiveEntry entry = new()
        {
            Id                              = 2953,
            Count                           = 5,
            FailureTimeMs                   = 25000,
            WorldLocation2Id                = 41756,
            PublicEventTeamId               = PublicEventTeam.PublicTeam,
            PublicEventObjectiveFlags       = (PublicEventObjectiveFlag)4,
            PublicEventObjectiveCategoryEnum = PublicEventObjectiveCategory.Challenge,
            PublicEventObjectiveTypeEnum    = PublicEventObjectiveType.Script
        };

        (NexusForever.Game.PublicEvent.PublicEventTeam team, PublicEventObjectiveModel beforeDeadline)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);
        team.ActivateObjective(2953u, 0u);
        beforeDeadline.UpdateObjective(4);
        beforeDeadline.Update(24.999d);

        Assert.Equal(PublicEventStatus.Active, beforeDeadline.Status);
        Assert.Equal(4u, beforeDeadline.Count);
        Assert.Equal([41756u], beforeDeadline.Build().Locations);

        beforeDeadline.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Succeeded, beforeDeadline.Status);
        Assert.Equal(5u, beforeDeadline.Count);

        beforeDeadline.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Succeeded, beforeDeadline.Status);
        Assert.Equal(5u, beforeDeadline.Count);

        (NexusForever.Game.PublicEvent.PublicEventTeam _, PublicEventObjectiveModel atDeadline)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);
        atDeadline.Team.ActivateObjective(2953u, 0u);
        atDeadline.Update(25d);
        atDeadline.UpdateObjective(5);

        Assert.Equal(PublicEventStatus.Failed, atDeadline.Status);
        Assert.Equal(0u, atDeadline.Count);
    }

    [Fact]
    public void Kick20Marauders_ScriptChallengeRequiresTwentyControllerCreditsAndRejectsPostSuccessCredit()
    {
        PublicEventObjectiveEntry entry = new()
        {
            Id                              = 2954,
            Count                           = 20,
            FailureTimeMs                   = 0,
            WorldLocation2Id                = 41756,
            PublicEventTeamId               = PublicEventTeam.PublicTeam,
            PublicEventObjectiveFlags       = (PublicEventObjectiveFlag)4,
            PublicEventObjectiveCategoryEnum = PublicEventObjectiveCategory.Challenge,
            PublicEventObjectiveTypeEnum    = PublicEventObjectiveType.Script
        };

        (NexusForever.Game.PublicEvent.PublicEventTeam team, PublicEventObjectiveModel objective)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);
        team.ActivateObjective(2954u, 0u);
        objective.UpdateObjective(19);
        objective.Update(600d);

        Assert.Equal(PublicEventStatus.Active, objective.Status);
        Assert.Equal(19u, objective.Count);
        Assert.Equal([41756u], objective.Build().Locations);

        objective.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Succeeded, objective.Status);
        Assert.Equal(20u, objective.Count);

        objective.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Succeeded, objective.Status);
        Assert.Equal(20u, objective.Count);
    }

    [Fact]
    public void Kick40Marauders_ScriptChallengeRequiresFortyControllerCreditsAndRejectsPostSuccessCredit()
    {
        PublicEventObjectiveEntry entry = new()
        {
            Id                              = 2955,
            Count                           = 40,
            FailureTimeMs                   = 0,
            WorldLocation2Id                = 41756,
            PublicEventTeamId               = PublicEventTeam.PublicTeam,
            PublicEventObjectiveFlags       = (PublicEventObjectiveFlag)4,
            PublicEventObjectiveCategoryEnum = PublicEventObjectiveCategory.Challenge,
            PublicEventObjectiveTypeEnum    = PublicEventObjectiveType.Script
        };

        (NexusForever.Game.PublicEvent.PublicEventTeam team, PublicEventObjectiveModel objective)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);
        team.ActivateObjective(2955u, 0u);
        objective.UpdateObjective(39);
        objective.Update(600d);

        Assert.Equal(PublicEventStatus.Active, objective.Status);
        Assert.Equal(39u, objective.Count);
        Assert.Equal([41756u], objective.Build().Locations);

        objective.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Succeeded, objective.Status);
        Assert.Equal(40u, objective.Count);

        objective.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Succeeded, objective.Status);
        Assert.Equal(40u, objective.Count);
    }

    [Fact]
    public void Kick80Marauders_ScriptChallengeRequiresEightyControllerCreditsAndRejectsPostSuccessCredit()
    {
        PublicEventObjectiveEntry entry = new()
        {
            Id                              = 2956,
            Count                           = 80,
            FailureTimeMs                   = 0,
            WorldLocation2Id                = 41756,
            PublicEventTeamId               = PublicEventTeam.PublicTeam,
            PublicEventObjectiveFlags       = (PublicEventObjectiveFlag)4,
            PublicEventObjectiveCategoryEnum = PublicEventObjectiveCategory.Challenge,
            PublicEventObjectiveTypeEnum    = PublicEventObjectiveType.Script
        };

        (NexusForever.Game.PublicEvent.PublicEventTeam team, PublicEventObjectiveModel objective)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);
        team.ActivateObjective(2956u, 0u);
        objective.UpdateObjective(79);
        objective.Update(600d);

        Assert.Equal(PublicEventStatus.Active, objective.Status);
        Assert.Equal(79u, objective.Count);
        Assert.Equal([41756u], objective.Build().Locations);

        objective.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Succeeded, objective.Status);
        Assert.Equal(80u, objective.Count);

        objective.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Succeeded, objective.Status);
        Assert.Equal(80u, objective.Count);
    }

    [Fact]
    public void PerfectPrecision_ScriptOptionalRequiresControllerSuccessAndDoesNotTreatZeroCountAsComplete()
    {
        PublicEventObjectiveEntry entry = new()
        {
            Id                              = 2957,
            Count                           = 0,
            FailureTimeMs                   = 0,
            WorldLocation2Id                = 41756,
            PublicEventTeamId               = PublicEventTeam.PublicTeam,
            PublicEventObjectiveFlags       = (PublicEventObjectiveFlag)4,
            PublicEventObjectiveCategoryEnum = PublicEventObjectiveCategory.Optional,
            PublicEventObjectiveTypeEnum    = PublicEventObjectiveType.Script
        };

        (NexusForever.Game.PublicEvent.PublicEventTeam team, PublicEventObjectiveModel objective)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);
        team.ActivateObjective(2957u, 0u);
        objective.Update(600d);

        Assert.Equal(PublicEventStatus.Active, objective.Status);
        Assert.Equal(0u, objective.Count);
        Assert.Equal([41756u], objective.Build().Locations);

        objective.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Succeeded, objective.Status);
        Assert.Equal(1u, objective.Count);

        objective.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Succeeded, objective.Status);
        Assert.Equal(1u, objective.Count);
    }

    [Fact]
    public void NoSoupForYall_CurrentTimedWinBoundaryRequiresControllerSuccessBeforeThirtySecondFailure()
    {
        PublicEventObjectiveEntry entry = new()
        {
            Id                              = 3148,
            Count                           = 0,
            FailureTimeMs                   = 30000,
            WorldLocation2Id                = 45907,
            PublicEventTeamId               = PublicEventTeam.PublicTeam,
            PublicEventObjectiveFlags       = (PublicEventObjectiveFlag)4,
            PublicEventObjectiveCategoryEnum = PublicEventObjectiveCategory.Challenge,
            PublicEventObjectiveTypeEnum    = PublicEventObjectiveType.TimedWin
        };

        (NexusForever.Game.PublicEvent.PublicEventTeam team, PublicEventObjectiveModel beforeDeadline)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);
        team.ActivateObjective(3148u, 0u);
        beforeDeadline.Update(29.999d);

        Assert.Equal(PublicEventStatus.Active, beforeDeadline.Status);
        Assert.Equal(0u, beforeDeadline.Count);
        Assert.Equal([45907u], beforeDeadline.Build().Locations);

        beforeDeadline.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Succeeded, beforeDeadline.Status);
        Assert.Equal(1u, beforeDeadline.Count);

        beforeDeadline.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Succeeded, beforeDeadline.Status);
        Assert.Equal(1u, beforeDeadline.Count);

        (NexusForever.Game.PublicEvent.PublicEventTeam _, PublicEventObjectiveModel atDeadline)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);
        atDeadline.Team.ActivateObjective(3148u, 0u);
        atDeadline.Update(30d);
        atDeadline.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Failed, atDeadline.Status);
        Assert.Equal(0u, atDeadline.Count);
    }

    [Fact]
    public void BingeEating_CurrentResourcePoolBoundaryRequiresTenControllerCreditsAndRejectsPostSuccessCredit()
    {
        PublicEventObjectiveEntry entry = new()
        {
            Id                              = 3149,
            Count                           = 10,
            FailureTimeMs                   = 0,
            WorldLocation2Id                = 45907,
            PublicEventTeamId               = PublicEventTeam.PublicTeam,
            PublicEventObjectiveFlags       = (PublicEventObjectiveFlag)4,
            PublicEventObjectiveCategoryEnum = PublicEventObjectiveCategory.Challenge,
            PublicEventObjectiveTypeEnum    = PublicEventObjectiveType.ResourcePool
        };

        (NexusForever.Game.PublicEvent.PublicEventTeam team, PublicEventObjectiveModel objective)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);
        team.ActivateObjective(3149u, 0u);
        objective.UpdateObjective(9);
        objective.Update(600d);

        Assert.Equal(PublicEventStatus.Active, objective.Status);
        Assert.Equal(9u, objective.Count);
        Assert.Equal([45907u], objective.Build().Locations);

        objective.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Succeeded, objective.Status);
        Assert.Equal(10u, objective.Count);

        objective.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Succeeded, objective.Status);
        Assert.Equal(10u, objective.Count);
    }

    [Fact]
    public void Purge_CurrentScriptBoundaryRequiresControllerSuccessBeforeFifteenSecondFailure()
    {
        PublicEventObjectiveEntry entry = new()
        {
            Id                              = 3150,
            Count                           = 0,
            FailureTimeMs                   = 15000,
            WorldLocation2Id                = 45907,
            PublicEventTeamId               = PublicEventTeam.PublicTeam,
            PublicEventObjectiveFlags       = (PublicEventObjectiveFlag)4,
            PublicEventObjectiveCategoryEnum = PublicEventObjectiveCategory.Challenge,
            PublicEventObjectiveTypeEnum    = PublicEventObjectiveType.Script
        };

        (NexusForever.Game.PublicEvent.PublicEventTeam team, PublicEventObjectiveModel beforeDeadline)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);
        team.ActivateObjective(3150u, 0u);
        beforeDeadline.Update(14.999d);

        Assert.Equal(PublicEventStatus.Active, beforeDeadline.Status);
        Assert.Equal(0u, beforeDeadline.Count);
        Assert.Equal([45907u], beforeDeadline.Build().Locations);

        beforeDeadline.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Succeeded, beforeDeadline.Status);
        Assert.Equal(1u, beforeDeadline.Count);

        beforeDeadline.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Succeeded, beforeDeadline.Status);
        Assert.Equal(1u, beforeDeadline.Count);

        (NexusForever.Game.PublicEvent.PublicEventTeam _, PublicEventObjectiveModel atDeadline)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);
        atDeadline.Team.ActivateObjective(3150u, 0u);
        atDeadline.Update(15d);
        atDeadline.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Failed, atDeadline.Status);
        Assert.Equal(0u, atDeadline.Count);
    }

    [Fact]
    public void FastFeast_CurrentScriptOptionalBoundaryRequiresControllerSuccessBeforeThreeMinuteFailure()
    {
        PublicEventObjectiveEntry entry = new()
        {
            Id                              = 3151,
            Count                           = 0,
            FailureTimeMs                   = 180000,
            WorldLocation2Id                = 45907,
            PublicEventTeamId               = PublicEventTeam.PublicTeam,
            PublicEventObjectiveFlags       = (PublicEventObjectiveFlag)4,
            PublicEventObjectiveCategoryEnum = PublicEventObjectiveCategory.Optional,
            PublicEventObjectiveTypeEnum    = PublicEventObjectiveType.Script
        };

        (NexusForever.Game.PublicEvent.PublicEventTeam team, PublicEventObjectiveModel beforeDeadline)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);
        team.ActivateObjective(3151u, 0u);
        beforeDeadline.Update(179.999d);

        Assert.Equal(PublicEventStatus.Active, beforeDeadline.Status);
        Assert.Equal(0u, beforeDeadline.Count);
        Assert.Equal([45907u], beforeDeadline.Build().Locations);

        beforeDeadline.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Succeeded, beforeDeadline.Status);
        Assert.Equal(1u, beforeDeadline.Count);

        beforeDeadline.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Succeeded, beforeDeadline.Status);
        Assert.Equal(1u, beforeDeadline.Count);

        (NexusForever.Game.PublicEvent.PublicEventTeam _, PublicEventObjectiveModel atDeadline)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);
        atDeadline.Team.ActivateObjective(3151u, 0u);
        atDeadline.Update(180d);
        atDeadline.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Failed, atDeadline.Status);
        Assert.Equal(0u, atDeadline.Count);
    }

    [Fact]
    public void OneOfEach_CurrentScriptOptionalRequiresControllerSuccessAndDoesNotTreatZeroCountAsComplete()
    {
        PublicEventObjectiveEntry entry = new()
        {
            Id                              = 3152,
            Count                           = 0,
            FailureTimeMs                   = 0,
            WorldLocation2Id                = 45907,
            PublicEventTeamId               = PublicEventTeam.PublicTeam,
            PublicEventObjectiveFlags       = (PublicEventObjectiveFlag)4,
            PublicEventObjectiveCategoryEnum = PublicEventObjectiveCategory.Optional,
            PublicEventObjectiveTypeEnum    = PublicEventObjectiveType.Script
        };

        (NexusForever.Game.PublicEvent.PublicEventTeam team, PublicEventObjectiveModel objective)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);
        team.ActivateObjective(3152u, 0u);
        objective.Update(600d);

        Assert.Equal(PublicEventStatus.Active, objective.Status);
        Assert.Equal(0u, objective.Count);
        Assert.Equal([45907u], objective.Build().Locations);

        objective.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Succeeded, objective.Status);
        Assert.Equal(1u, objective.Count);

        objective.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Succeeded, objective.Status);
        Assert.Equal(1u, objective.Count);
    }

    [Fact]
    public void SquirgDefuser_CurrentResourcePoolBoundaryRequiresThreeControllerCreditsAndRejectsPostSuccessCredit()
    {
        PublicEventObjectiveEntry entry = new()
        {
            Id                              = 3198,
            Count                           = 3,
            FailureTimeMs                   = 0,
            WorldLocation2Id                = 0,
            PublicEventTeamId               = PublicEventTeam.PublicTeam,
            PublicEventObjectiveFlags       = (PublicEventObjectiveFlag)4,
            PublicEventObjectiveCategoryEnum = PublicEventObjectiveCategory.Challenge,
            PublicEventObjectiveTypeEnum    = PublicEventObjectiveType.ResourcePool
        };

        (NexusForever.Game.PublicEvent.PublicEventTeam team, PublicEventObjectiveModel objective)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);
        team.ActivateObjective(3198u, 0u);
        objective.UpdateObjective(2);
        objective.Update(600d);

        Assert.Equal(PublicEventStatus.Active, objective.Status);
        Assert.Equal(2u, objective.Count);
        Assert.Empty(objective.Build().Locations);

        objective.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Succeeded, objective.Status);
        Assert.Equal(3u, objective.Count);

        objective.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Succeeded, objective.Status);
        Assert.Equal(3u, objective.Count);
    }

    [Fact]
    public void TwinkleToes_CurrentScriptOptionalRequiresControllerSuccessAndDoesNotTreatZeroCountAsComplete()
    {
        PublicEventObjectiveEntry entry = new()
        {
            Id                              = 3199,
            Count                           = 0,
            FailureTimeMs                   = 0,
            WorldLocation2Id                = 0,
            PublicEventTeamId               = PublicEventTeam.PublicTeam,
            PublicEventObjectiveFlags       = (PublicEventObjectiveFlag)4,
            PublicEventObjectiveCategoryEnum = PublicEventObjectiveCategory.Optional,
            PublicEventObjectiveTypeEnum    = PublicEventObjectiveType.Script
        };

        (NexusForever.Game.PublicEvent.PublicEventTeam team, PublicEventObjectiveModel objective)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);
        team.ActivateObjective(3199u, 0u);
        objective.Update(600d);

        Assert.Equal(PublicEventStatus.Active, objective.Status);
        Assert.Equal(0u, objective.Count);
        Assert.Empty(objective.Build().Locations);

        objective.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Succeeded, objective.Status);
        Assert.Equal(1u, objective.Count);

        objective.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Succeeded, objective.Status);
        Assert.Equal(1u, objective.Count);
    }

    [Fact]
    public void JumpJump_CurrentResourcePoolBoundaryRequiresFifteenControllerCreditsAndRejectsPostSuccessCredit()
    {
        PublicEventObjectiveEntry entry = new()
        {
            Id                              = 3200,
            Count                           = 15,
            FailureTimeMs                   = 0,
            WorldLocation2Id                = 0,
            PublicEventTeamId               = PublicEventTeam.PublicTeam,
            PublicEventObjectiveFlags       = (PublicEventObjectiveFlag)4,
            PublicEventObjectiveCategoryEnum = PublicEventObjectiveCategory.Challenge,
            PublicEventObjectiveTypeEnum    = PublicEventObjectiveType.ResourcePool
        };

        (NexusForever.Game.PublicEvent.PublicEventTeam team, PublicEventObjectiveModel objective)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);
        team.ActivateObjective(3200u, 0u);
        objective.UpdateObjective(14);
        objective.Update(600d);

        Assert.Equal(PublicEventStatus.Active, objective.Status);
        Assert.Equal(14u, objective.Count);
        Assert.Empty(objective.Build().Locations);

        objective.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Succeeded, objective.Status);
        Assert.Equal(15u, objective.Count);

        objective.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Succeeded, objective.Status);
        Assert.Equal(15u, objective.Count);
    }

    [Fact]
    public void Coordination_CurrentGenericScriptBoundaryTreatsFiveCreditsAsSuccessDespiteLessThanFiveRetailWording()
    {
        PublicEventObjectiveEntry entry = new()
        {
            Id                              = 3205,
            Count                           = 5,
            FailureTimeMs                   = 0,
            WorldLocation2Id                = 45901,
            PublicEventTeamId               = PublicEventTeam.PublicTeam,
            PublicEventObjectiveFlags       = (PublicEventObjectiveFlag)4,
            PublicEventObjectiveCategoryEnum = PublicEventObjectiveCategory.Challenge,
            PublicEventObjectiveTypeEnum    = PublicEventObjectiveType.Script
        };

        (NexusForever.Game.PublicEvent.PublicEventTeam team, PublicEventObjectiveModel objective)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);
        team.ActivateObjective(3205u, 0u);
        objective.UpdateObjective(4);
        objective.Update(600d);

        Assert.Equal(PublicEventStatus.Active, objective.Status);
        Assert.Equal(4u, objective.Count);
        Assert.Equal([45901u], objective.Build().Locations);

        objective.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Succeeded, objective.Status);
        Assert.Equal(5u, objective.Count);

        objective.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Succeeded, objective.Status);
        Assert.Equal(5u, objective.Count);
    }

    [Fact]
    public void MakeItRain_CurrentScriptBoundaryRequiresSeventyFiveControllerCreditsAndRejectsPostSuccessCredit()
    {
        PublicEventObjectiveEntry entry = new()
        {
            Id                              = 3207,
            Count                           = 75,
            FailureTimeMs                   = 0,
            WorldLocation2Id                = 45901,
            PublicEventTeamId               = PublicEventTeam.PublicTeam,
            PublicEventObjectiveFlags       = (PublicEventObjectiveFlag)4,
            PublicEventObjectiveCategoryEnum = PublicEventObjectiveCategory.Optional,
            PublicEventObjectiveTypeEnum    = PublicEventObjectiveType.Script
        };

        (NexusForever.Game.PublicEvent.PublicEventTeam team, PublicEventObjectiveModel objective)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);
        team.ActivateObjective(3207u, 0u);
        objective.UpdateObjective(74);
        objective.Update(600d);

        Assert.Equal(PublicEventStatus.Active, objective.Status);
        Assert.Equal(74u, objective.Count);
        Assert.Equal([45901u], objective.Build().Locations);

        objective.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Succeeded, objective.Status);
        Assert.Equal(75u, objective.Count);

        objective.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Succeeded, objective.Status);
        Assert.Equal(75u, objective.Count);
    }

    [Fact]
    public void HappyFeet_CurrentGenericScriptBoundaryTreatsFifthHitAsSuccessAndThirtySecondsAsFailure()
    {
        PublicEventObjectiveEntry entry = new()
        {
            Id                              = 3208,
            Count                           = 5,
            FailureTimeMs                   = 30000,
            WorldLocation2Id                = 45901,
            PublicEventTeamId               = PublicEventTeam.PublicTeam,
            PublicEventObjectiveFlags       = (PublicEventObjectiveFlag)4,
            PublicEventObjectiveCategoryEnum = PublicEventObjectiveCategory.Challenge,
            PublicEventObjectiveTypeEnum    = PublicEventObjectiveType.Script
        };

        (NexusForever.Game.PublicEvent.PublicEventTeam team, PublicEventObjectiveModel beforeDeadline)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);
        team.ActivateObjective(3208u, 0u);
        beforeDeadline.UpdateObjective(4);
        beforeDeadline.Update(29.999d);

        Assert.Equal(PublicEventStatus.Active, beforeDeadline.Status);
        Assert.Equal(4u, beforeDeadline.Count);
        Assert.Equal(29999u, beforeDeadline.Build().ElapsedTimeMs);
        Assert.Equal([45901u], beforeDeadline.Build().Locations);

        beforeDeadline.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Succeeded, beforeDeadline.Status);
        Assert.Equal(5u, beforeDeadline.Count);

        beforeDeadline.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Succeeded, beforeDeadline.Status);
        Assert.Equal(5u, beforeDeadline.Count);

        (NexusForever.Game.PublicEvent.PublicEventTeam _, PublicEventObjectiveModel atDeadline)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);
        atDeadline.Team.ActivateObjective(3208u, 0u);
        atDeadline.Update(30d);
        atDeadline.UpdateObjective(5);

        Assert.Equal(PublicEventStatus.Failed, atDeadline.Status);
        Assert.Equal(0u, atDeadline.Count);
        Assert.Equal([45901u], atDeadline.Build().Locations);
    }

    [Fact]
    public void LightningRound_CurrentGenericScriptBoundaryRequiresControllerCreditBeforeSeventySecondFailure()
    {
        PublicEventObjectiveEntry entry = new()
        {
            Id                              = 3209,
            Count                           = 0,
            FailureTimeMs                   = 70000,
            WorldLocation2Id                = 45901,
            PublicEventTeamId               = PublicEventTeam.PublicTeam,
            PublicEventObjectiveFlags       = (PublicEventObjectiveFlag)4,
            PublicEventObjectiveCategoryEnum = PublicEventObjectiveCategory.Challenge,
            PublicEventObjectiveTypeEnum    = PublicEventObjectiveType.Script
        };

        (NexusForever.Game.PublicEvent.PublicEventTeam team, PublicEventObjectiveModel beforeDeadline)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);
        team.ActivateObjective(3209u, 0u);
        beforeDeadline.Update(69.999d);

        Assert.Equal(PublicEventStatus.Active, beforeDeadline.Status);
        Assert.Equal(0u, beforeDeadline.Count);
        Assert.Equal(69999u, beforeDeadline.Build().ElapsedTimeMs);
        Assert.Equal([45901u], beforeDeadline.Build().Locations);

        beforeDeadline.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Succeeded, beforeDeadline.Status);
        Assert.Equal(1u, beforeDeadline.Count);

        beforeDeadline.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Succeeded, beforeDeadline.Status);
        Assert.Equal(1u, beforeDeadline.Count);

        (NexusForever.Game.PublicEvent.PublicEventTeam _, PublicEventObjectiveModel atDeadline)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);
        atDeadline.Team.ActivateObjective(3209u, 0u);
        atDeadline.Update(70d);
        atDeadline.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Failed, atDeadline.Status);
        Assert.Equal(0u, atDeadline.Count);
        Assert.Equal([45901u], atDeadline.Build().Locations);
    }

    [Fact]
    public void UpdateObjective_UsesDynamicMaxCountWhenFlagSet()
    {
        (NexusForever.Game.PublicEvent.PublicEventTeam team, PublicEventObjectiveModel objective) = PublicEventTestSupport.CreateTeamWithObjective(new PublicEventObjectiveEntry
        {
            Id                              = 103,
            Count                           = 1,
            PublicEventTeamId               = PublicEventTeam.PublicTeam,
            PublicEventObjectiveFlags       = PublicEventObjectiveFlag.UsesDynamicMaxCount,
            PublicEventObjectiveCategoryEnum = PublicEventObjectiveCategory.Main
        });

        team.ActivateObjective(103u, 3u);
        objective.UpdateObjective(2);

        Assert.Equal(PublicEventStatus.Active, objective.Status);

        objective.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Succeeded, objective.Status);
    }

    [Fact]
    public void UpdateObjective_UsesActivationMaxWhenSupplied()
    {
        (NexusForever.Game.PublicEvent.PublicEventTeam team, PublicEventObjectiveModel objective) = PublicEventTestSupport.CreateTeamWithObjective(new PublicEventObjectiveEntry
        {
            Id                              = 104,
            Count                           = 0,
            PublicEventTeamId               = PublicEventTeam.PublicTeam,
            PublicEventObjectiveFlags       = PublicEventObjectiveFlag.None,
            PublicEventObjectiveCategoryEnum = PublicEventObjectiveCategory.Main
        });

        team.ActivateObjective(104u, 3u);
        objective.UpdateObjective(2);

        Assert.Equal(PublicEventStatus.Active, objective.Status);
        Assert.Equal(3u, objective.DynamicMax);

        objective.UpdateObjective(1);

        Assert.Equal(PublicEventStatus.Succeeded, objective.Status);
    }

    [Fact]
    public void UpdateObjective_ChecklistUsesObjectiveDataBitsAndIgnoresReplay()
    {
        IPublicEventTeam team = RecordingDispatchProxy<IPublicEventTeam>.Create(out var teamProxy);
        var objective = new PublicEventObjectiveModel();
        objective.Initialise(team, new PublicEventObjectiveEntry
        {
            Id                              = 106,
            Count                           = 3,
            PublicEventTeamId               = PublicEventTeam.PublicTeam,
            PublicEventObjectiveFlags       = PublicEventObjectiveFlag.InitialObjective,
            PublicEventObjectiveCategoryEnum = PublicEventObjectiveCategory.Main,
            PublicEventObjectiveTypeEnum    = PublicEventObjectiveType.ActivateTargetGroupChecklist
        });

        objective.UpdateObjective(2);
        objective.UpdateObjective(2);
        objective.UpdateObjective(32);
        objective.UpdateObjective(-1);
        objective.UpdateObjective(4);

        Assert.Equal(PublicEventStatus.Active, objective.Status);
        Assert.Equal(2u, objective.Count);
        Assert.Equal((1u << 2) | (1u << 4), objective.Build().ObjectiveStatus.ObjectiveData);
        Assert.Equal(2f, objective.Build().ObjectiveStatus.Count);
        Assert.Equal(2, teamProxy.GetInvocations(nameof(IPublicEventTeam.Broadcast)).Count);
    }

    [Fact]
    public void TeamUpdateObjective_ChecklistRequiresDistinctBitsToComplete()
    {
        (NexusForever.Game.PublicEvent.PublicEventTeam team, PublicEventObjectiveModel objective) = PublicEventTestSupport.CreateTeamWithObjective(new PublicEventObjectiveEntry
        {
            Id                              = 107,
            Count                           = 2,
            ObjectId                        = 44,
            PublicEventTeamId               = PublicEventTeam.PublicTeam,
            PublicEventObjectiveFlags       = PublicEventObjectiveFlag.InitialObjective,
            PublicEventObjectiveCategoryEnum = PublicEventObjectiveCategory.Main,
            PublicEventObjectiveTypeEnum    = PublicEventObjectiveType.TalkToChecklist
        });

        team.UpdateObjective(PublicEventObjectiveType.TalkToChecklist, 44u, 3);
        team.UpdateObjective(PublicEventObjectiveType.TalkToChecklist, 44u, 3);

        Assert.Equal(PublicEventStatus.Active, objective.Status);
        Assert.False(team.IsFinialised);
        Assert.Equal(1u, objective.Count);

        team.UpdateObjective(PublicEventObjectiveType.TalkToChecklist, 44u, 5);

        Assert.Equal(PublicEventStatus.Succeeded, objective.Status);
        Assert.True(team.IsFinialised);
        Assert.Equal(2u, objective.Count);
        Assert.Equal((1u << 3) | (1u << 5), objective.Build().ObjectiveStatus.ObjectiveData);
    }

    [Fact]
    public void SetBusy_WithUnchangedState_DoesNotBroadcastDuplicateObjectiveUpdate()
    {
        IPublicEventTeam team = RecordingDispatchProxy<IPublicEventTeam>.Create(out var teamProxy);
        var objective = new PublicEventObjectiveModel();
        objective.Initialise(team, new PublicEventObjectiveEntry
        {
            Id                              = 105,
            Count                           = 1,
            PublicEventTeamId               = PublicEventTeam.PublicTeam,
            PublicEventObjectiveFlags       = PublicEventObjectiveFlag.InitialObjective,
            PublicEventObjectiveCategoryEnum = PublicEventObjectiveCategory.Main
        });

        objective.SetBusy(true);
        objective.SetBusy(true);
        objective.SetBusy(false);
        objective.SetBusy(false);

        Assert.Equal(2, teamProxy.GetInvocations(nameof(IPublicEventTeam.Broadcast)).Count);
    }
}
