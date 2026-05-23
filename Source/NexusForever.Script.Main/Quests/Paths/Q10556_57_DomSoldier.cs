using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.Paths
{
    /// <summary>Dominion Soldier: mass activate quest. Quest 10556 -> 10557.</summary>
    [ScriptFilterOwnerId(10556u)]
    public class Q10556DomSoldierActivateQuestScript : FollowUpQuestScript<Q10556DomSoldierActivateQuestScript>
    {
        protected override ushort NextQuestId => 10557;
        public Q10556DomSoldierActivateQuestScript(ILogger<Q10556DomSoldierActivateQuestScript> log, IGlobalQuestManager m) : base(log, m) { }
    }
    /// <summary>Dominion Soldier: SucceedCSI quest. Quest 10557 (terminal).</summary>
    [ScriptFilterOwnerId(10557u)]
    public class Q10557DomSoldierCSIQuestScript : IQuestScript, IOwnedScript<IQuest>
    {
        private readonly ILogger<Q10557DomSoldierCSIQuestScript> log; private IQuest owner;
        public Q10557DomSoldierCSIQuestScript(ILogger<Q10557DomSoldierCSIQuestScript> log) => this.log = log;
        public void OnLoad(IQuest o) { owner = o; log.LogDebug("Path {QuestId} loaded.", o.Id); }
        public void OnQuestStateChange(QuestState n, QuestState old) => log.LogDebug("Path {QuestId}: {Old} -> {New}.", owner.Id, old, n);
    }
}
