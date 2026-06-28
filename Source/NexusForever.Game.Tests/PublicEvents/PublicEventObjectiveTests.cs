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
