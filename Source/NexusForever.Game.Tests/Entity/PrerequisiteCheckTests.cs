using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Prerequisite.Check;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Tests.Entity;

public class PrerequisiteCheckTests
{
    [Theory]
    [InlineData(PrerequisiteComparison.Equal, true, true)]
    [InlineData(PrerequisiteComparison.Equal, false, false)]
    [InlineData(PrerequisiteComparison.NotEqual, true, false)]
    [InlineData(PrerequisiteComparison.NotEqual, false, true)]
    public void UnderSpell_UsesActiveTrackedSpellState(PrerequisiteComparison comparison, bool active, bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetMethodReturn(nameof(IPlayer.HasTrackedSpellState), active);

        var check = new PrerequisiteCheckUnderSpell(NullLogger<PrerequisiteCheckUnderSpell>.Instance);

        bool result = check.Meets(player, comparison, value: 85563u, objectId: 0u, new PrerequisiteParameters());

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, 517u, true)]
    [InlineData(PrerequisiteComparison.Equal, 518u, false)]
    [InlineData(PrerequisiteComparison.NotEqual, 517u, false)]
    [InlineData(PrerequisiteComparison.NotEqual, 518u, true)]
    public void InSubZone_ComparesAgainstCurrentZone(PrerequisiteComparison comparison, uint requiredZoneId, bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Zone), new WorldZoneEntry { Id = 517u });

        var check = new PrerequisiteCheckInSubZone(NullLogger<PrerequisiteCheckInSubZone>.Instance, gameTableManager: null);

        bool result = check.Meets(player, comparison, value: requiredZoneId, objectId: 0u, new PrerequisiteParameters());

        Assert.Equal(expected, result);
    }
}
