using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Script.Main.Quests.NorthernWilds;

namespace NexusForever.Game.Tests.Quests;

public class QuestScriptConstructionTests
{
    /// <summary>
    /// DI smoke test: verifies Q3479FromTheWreckageQuestScript can be constructed
    /// with its FollowUpQuestScript&lt;T&gt; base class pattern intact.
    /// </summary>
    [Fact]
    public void Q3479FromTheWreckageQuestScript_CanBeConstructed()
    {
        IGlobalQuestManager mockGlobalQuestManager = RecordingDispatchProxy<IGlobalQuestManager>.Create(out _);

        var script = new Q3479FromTheWreckageQuestScript(
            NullLogger<Q3479FromTheWreckageQuestScript>.Instance,
            mockGlobalQuestManager);

        Assert.NotNull(script);
    }
}
