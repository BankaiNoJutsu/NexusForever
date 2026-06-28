using NexusForever.Game.Static.PublicEvent;
using NexusForever.GameTable.Model;
using IBaseMap = NexusForever.Game.Abstract.Map.IBaseMap;

namespace NexusForever.Game.Tests.PublicEvents;

public class PublicEventFlowTests
{
    [Fact]
    public void UpdateObjective_ById_FinalisesEventWhenRequiredObjectiveCompletes()
    {
        var entry = new PublicEventObjectiveEntry
        {
            Id                              = 201,
            Count                           = 1,
            PublicEventTeamId               = PublicEventTeam.PublicTeam,
            PublicEventObjectiveFlags       = PublicEventObjectiveFlag.InitialObjective,
            PublicEventObjectiveCategoryEnum = PublicEventObjectiveCategory.Main
        };

        var publicEvent = PublicEventTestSupport.CreatePublicEvent(entry, out var mapProxy);

        publicEvent.UpdateObjective(entry.Id, 1);

        Assert.True(publicEvent.IsFinalised);
        Assert.Single(mapProxy.GetInvocations(nameof(IBaseMap.OnPublicEventFinish)));
    }

    [Fact]
    public void UpdateObjective_ByVirtualCollectObject_FinalisesEventWhenRequiredObjectiveCompletes()
    {
        var entry = new PublicEventObjectiveEntry
        {
            Id                              = 203,
            Count                           = 2,
            PublicEventTeamId               = PublicEventTeam.PublicTeam,
            PublicEventObjectiveFlags       = PublicEventObjectiveFlag.InitialObjective,
            PublicEventObjectiveCategoryEnum = PublicEventObjectiveCategory.Main,
            PublicEventObjectiveTypeEnum    = PublicEventObjectiveType.VirtualCollect,
            ObjectId                        = 1143u
        };

        var publicEvent = PublicEventTestSupport.CreatePublicEvent(entry, out var mapProxy);

        publicEvent.UpdateObjective(PublicEventObjectiveType.VirtualCollect, 1143u, 2);

        Assert.True(publicEvent.IsFinalised);
        Assert.Single(mapProxy.GetInvocations(nameof(IBaseMap.OnPublicEventFinish)));
    }

    [Fact]
    public void ResetObjective_ClearsTeamCompletionState()
    {
        var entry = new PublicEventObjectiveEntry
        {
            Id                              = 202,
            Count                           = 1,
            PublicEventTeamId               = PublicEventTeam.PublicTeam,
            PublicEventObjectiveFlags       = PublicEventObjectiveFlag.InitialObjective,
            PublicEventObjectiveCategoryEnum = PublicEventObjectiveCategory.Main
        };

        (NexusForever.Game.PublicEvent.PublicEventTeam team, NexusForever.Game.PublicEvent.PublicEventObjective objective) = PublicEventTestSupport.CreateTeamWithObjective(entry);

        team.UpdateObjective(entry.Id, 1);
        Assert.True(team.IsFinialised);
        Assert.Equal(PublicEventStatus.Succeeded, objective.Status);

        team.ResetObjective(entry.Id);

        Assert.False(team.IsFinialised);
        Assert.Equal(PublicEventStatus.Inactive, objective.Status);
    }
}
