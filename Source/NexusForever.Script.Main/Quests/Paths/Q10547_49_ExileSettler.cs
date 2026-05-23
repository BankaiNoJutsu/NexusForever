using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.Paths
{
    /// <summary>Exile Settler: kill intro quest. Quest 10547 -> 10548.</summary>
    [ScriptFilterOwnerId(10547u)]
    public class Q10547SettlerKillQuestScript : FollowUpQuestScript<Q10547SettlerKillQuestScript>
    {
        protected override ushort NextQuestId => 10548;
        public Q10547SettlerKillQuestScript(ILogger<Q10547SettlerKillQuestScript> log, IGlobalQuestManager m) : base(log, m) { }
    }
    /// <summary>Exile Settler: spell+activate quest. Quest 10548 -> 10549.</summary>
    [ScriptFilterOwnerId(10548u)]
    public class Q10548SettlerSpellQuestScript : FollowUpQuestScript<Q10548SettlerSpellQuestScript>
    {
        protected override ushort NextQuestId => 10549;
        public Q10548SettlerSpellQuestScript(ILogger<Q10548SettlerSpellQuestScript> log, IGlobalQuestManager m) : base(log, m) { }
    }
    /// <summary>Exile Settler: mass activate quest. Quest 10549 (terminal).</summary>
    [ScriptFilterOwnerId(10549u)]
    public class Q10549SettlerActivateQuestScript : IQuestScript, IOwnedScript<IQuest>
    {
        private readonly ILogger<Q10549SettlerActivateQuestScript> log; private IQuest owner;
        public Q10549SettlerActivateQuestScript(ILogger<Q10549SettlerActivateQuestScript> log) => this.log = log;
        public void OnLoad(IQuest o) { owner = o; log.LogDebug("Path {QuestId} loaded.", o.Id); }
        public void OnQuestStateChange(QuestState n, QuestState old) => log.LogDebug("Path {QuestId}: {Old} -> {New}.", owner.Id, old, n);
    }
}
