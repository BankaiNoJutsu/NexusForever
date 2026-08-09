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

    [Theory]
    [InlineData(3271u, 7773u, 7774u)]
    [InlineData(3272u, 7774u, 7773u)]
    [InlineData(3273u, 7775u, 7774u)]
    [InlineData(3274u, 7779u, 7775u)]
    [InlineData(3275u, 7780u, 7779u)]
    [InlineData(3276u, 7542u, 7780u)]
    [InlineData(3277u, 7540u, 7542u)]
    [InlineData(3278u, 7781u, 7540u)]
    [InlineData(3279u, 7782u, 7781u)]
    [InlineData(3280u, 7783u, 7782u)]
    [InlineData(3281u, 7784u, 7783u)]
    public void UltimateProtogamesNextEvent_CurrentTurnstileBoundaryRequiresExactTypeAndObject(
        uint objectiveId,
        uint objectId,
        uint wrongObjectId)
    {
        var entry = new PublicEventObjectiveEntry
        {
            Id                              = objectiveId,
            Count                           = 1,
            ObjectId                        = objectId,
            PublicEventTeamId               = PublicEventTeam.PublicTeam,
            PublicEventObjectiveFlags       = PublicEventObjectiveFlag.UsesDynamicMaxCount,
            PublicEventObjectiveCategoryEnum = PublicEventObjectiveCategory.Main,
            PublicEventObjectiveTypeEnum    = PublicEventObjectiveType.Turnstile
        };

        (NexusForever.Game.PublicEvent.PublicEventTeam team, NexusForever.Game.PublicEvent.PublicEventObjective objective)
            = PublicEventTestSupport.CreateTeamWithObjective(entry);
        team.ActivateObjective(entry.Id, 1u);

        team.UpdateObjective(PublicEventObjectiveType.Turnstile, wrongObjectId, 1);
        team.UpdateObjective(PublicEventObjectiveType.Script, objectId, 1);

        Assert.Equal(PublicEventStatus.Active, objective.Status);
        Assert.Equal(0u, objective.Count);
        Assert.False(team.IsFinialised);

        team.UpdateObjective(PublicEventObjectiveType.Turnstile, objectId, 1);

        Assert.Equal(PublicEventStatus.Succeeded, objective.Status);
        Assert.Equal(1u, objective.Count);
        Assert.True(team.IsFinialised);

        team.UpdateObjective(PublicEventObjectiveType.Turnstile, objectId, 1);

        Assert.Equal(1u, objective.Count);
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
