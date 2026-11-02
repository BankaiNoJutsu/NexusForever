using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Quest;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Tests.Quest;

public partial class CommunicatorMessageTests
{
    [Fact]
    public void IndexedMessagesRetainInjectedAncestorAndPrerequisiteServicesForPredicateCallers()
    {
        var tables = CreateGameTableManager(new WorldZoneEntry { Id = 35 }, new WorldZoneEntry { Id = 590, ParentZoneId = 35 },
            new WorldZoneEntry { Id = 597, ParentZoneId = 590 });
        var prerequisites = RecordingDispatchProxy<IPrerequisiteManager>.Create(out var pp);
        pp.SetMethodReturn(nameof(IPrerequisiteManager.Meets), true);
        var index = CommunicatorIndex.Build([new CommunicatorMessagesEntry { Id = 1, Quests = [], States = [], WorldZoneId = 35, PrerequisiteId = 99 }],
            _ => new CommunicatorQuestInfo(0, false, 0), prerequisites, tables);
        var player = CreatePlayer(null, new WorldZoneEntry { Id = 597, ParentZoneId = 590 });
        Assert.True(index.Messages[1].Meets(player));
        pp.SetMethodReturn(nameof(IPrerequisiteManager.Meets), false); Assert.False(index.Messages[1].Meets(player));
    }

    [Fact]
    public void CommunicatorRaceConditionRejectsAnotherRaceBeforeSendingItsOffer()
    {
        var player = CreatePlayer(null);
        ((RecordingDispatchProxy<IPlayer>)(object)player).SetProperty(nameof(IPlayer.Race), (Race)2);
        var message = new CommunicatorMessage(new CommunicatorMessagesEntry { Id = 1, RaceId = 1, Quests = [], States = [] });
        Assert.False(message.Meets(player));
    }

    [Theory]
    [InlineData(12u, QuestState.Accepted)]
    [InlineData(12u, QuestState.Achieved)]
    [InlineData(13u, QuestState.Achieved)]
    [InlineData(13u, QuestState.Completed)]
    [InlineData(0u, QuestState.Mentioned)]
    public void CommunicatorUsesTablePredicatesRatherThanServerStateEnumValues(uint required, QuestState state)
    {
        var tables = CreateGameTableManager();
        ((RecordingDispatchProxy<IGameTableManager>)(object)tables).SetProperty(nameof(IGameTableManager.Quest2), CreateGameTable(new Quest2Entry { Id = QuestId }));
        var message = new CommunicatorMessage(new CommunicatorMessagesEntry
        { Id = 1, Quests = [QuestId], States = [required] }, gameTableManager: tables);
        Assert.True(message.Meets(CreatePlayer(state)));
    }
}
