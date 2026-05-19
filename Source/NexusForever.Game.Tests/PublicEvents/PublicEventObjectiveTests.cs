using NexusForever.Game.Static.PublicEvent;
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
}
