using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.Paths
{
    /// <summary>Exile Scientist: kill intro quest. Quest 10553 -> 10554.</summary>
    [ScriptFilterOwnerId(10553u)]
    public class Q10553ScientistKillQuestScript : FollowUpQuestScript<Q10553ScientistKillQuestScript>
    {
        protected override ushort NextQuestId => 10554;
        public Q10553ScientistKillQuestScript(ILogger<Q10553ScientistKillQuestScript> log, IGlobalQuestManager m) : base(log, m) { }
    }
    /// <summary>Exile Scientist: spell+activate quest. Quest 10554 -> 10555.</summary>
    [ScriptFilterOwnerId(10554u)]
    public class Q10554ScientistSpellQuestScript : FollowUpQuestScript<Q10554ScientistSpellQuestScript>
    {
        protected override ushort NextQuestId => 10555;
        public Q10554ScientistSpellQuestScript(ILogger<Q10554ScientistSpellQuestScript> log, IGlobalQuestManager m) : base(log, m) { }
    }
    /// <summary>Exile Scientist: SucceedCSI quest. Quest 10555 (terminal).</summary>
    [ScriptFilterOwnerId(10555u)]
    public class Q10555ScientistCSIQuestScript : IQuestScript, IOwnedScript<IQuest>
    {
        private readonly ILogger<Q10555ScientistCSIQuestScript> log; private IQuest owner;
        public Q10555ScientistCSIQuestScript(ILogger<Q10555ScientistCSIQuestScript> log) => this.log = log;
        public void OnLoad(IQuest o) { owner = o; log.LogDebug("Path {QuestId} loaded.", o.Id); }
        public void OnQuestStateChange(QuestState n, QuestState old) => log.LogDebug("Path {QuestId}: {Old} -> {New}.", owner.Id, old, n);
    }
}
