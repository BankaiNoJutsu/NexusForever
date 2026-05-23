using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.Paths
{
    /// <summary>Exile Explorer: kill intro quest. Quest 10550 -> 10551.</summary>
    [ScriptFilterOwnerId(10550u)]
    public class Q10550ExplorerKillQuestScript : FollowUpQuestScript<Q10550ExplorerKillQuestScript>
    {
        protected override ushort NextQuestId => 10551;
        public Q10550ExplorerKillQuestScript(ILogger<Q10550ExplorerKillQuestScript> log, IGlobalQuestManager m) : base(log, m) { }
    }
    /// <summary>Exile Explorer: spell+activate quest. Quest 10551 -> 10552.</summary>
    [ScriptFilterOwnerId(10551u)]
    public class Q10551ExplorerSpellQuestScript : FollowUpQuestScript<Q10551ExplorerSpellQuestScript>
    {
        protected override ushort NextQuestId => 10552;
        public Q10551ExplorerSpellQuestScript(ILogger<Q10551ExplorerSpellQuestScript> log, IGlobalQuestManager m) : base(log, m) { }
    }
    /// <summary>Exile Explorer: SucceedCSI quest. Quest 10552 (terminal).</summary>
    [ScriptFilterOwnerId(10552u)]
    public class Q10552ExplorerCSIQuestScript : IQuestScript, IOwnedScript<IQuest>
    {
        private readonly ILogger<Q10552ExplorerCSIQuestScript> log; private IQuest owner;
        public Q10552ExplorerCSIQuestScript(ILogger<Q10552ExplorerCSIQuestScript> log) => this.log = log;
        public void OnLoad(IQuest o) { owner = o; log.LogDebug("Path {QuestId} loaded.", o.Id); }
        public void OnQuestStateChange(QuestState n, QuestState old) => log.LogDebug("Path {QuestId}: {Old} -> {New}.", owner.Id, old, n);
    }
}
