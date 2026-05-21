using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.Paths
{
    /// <summary>Cross-faction path quest: ActivateEntity ×2. Quest 10563.</summary>
    [ScriptFilterOwnerId(10563u)]
    public class Q10563PathQuestScript : IQuestScript, IOwnedScript<IQuest>
    {
        private readonly ILogger<Q10563PathQuestScript> log; private IQuest owner;
        public Q10563PathQuestScript(ILogger<Q10563PathQuestScript> log) => this.log = log;
        public void OnLoad(IQuest o) { owner = o; log.LogDebug("Path {QuestId} loaded.", o.Id); }
        public void OnQuestStateChange(QuestState n, QuestState old) => log.LogDebug("Path {QuestId}: {Old} -> {New}.", owner.Id, old, n);
    }
    /// <summary>Cross-faction: kill targets + collect + CSI. Quest 10566.</summary>
    [ScriptFilterOwnerId(10566u)]
    public class Q10566PathQuestScript : IQuestScript, IOwnedScript<IQuest>
    {
        private readonly ILogger<Q10566PathQuestScript> log; private IQuest owner;
        public Q10566PathQuestScript(ILogger<Q10566PathQuestScript> log) => this.log = log;
        public void OnLoad(IQuest o) { owner = o; log.LogDebug("Path {QuestId} loaded.", o.Id); }
        public void OnQuestStateChange(QuestState n, QuestState old) => log.LogDebug("Path {QuestId}: {Old} -> {New}.", owner.Id, old, n);
    }
    /// <summary>Path spell-1. Quest 10568.</summary>
    [ScriptFilterOwnerId(10568u)]
    public class Q10568PathSpellQuestScript : IQuestScript, IOwnedScript<IQuest>
    {
        private readonly ILogger<Q10568PathSpellQuestScript> log; private IQuest owner;
        public Q10568PathSpellQuestScript(ILogger<Q10568PathSpellQuestScript> log) => this.log = log;
        public void OnLoad(IQuest o) { owner = o; log.LogDebug("Path {QuestId} loaded.", o.Id); }
        public void OnQuestStateChange(QuestState n, QuestState old) => log.LogDebug("Path {QuestId}: {Old} -> {New}.", owner.Id, old, n);
    }
    /// <summary>Path spell-2. Quest 10569.</summary>
    [ScriptFilterOwnerId(10569u)]
    public class Q10569PathSpellQuestScript : IQuestScript, IOwnedScript<IQuest>
    {
        private readonly ILogger<Q10569PathSpellQuestScript> log; private IQuest owner;
        public Q10569PathSpellQuestScript(ILogger<Q10569PathSpellQuestScript> log) => this.log = log;
        public void OnLoad(IQuest o) { owner = o; log.LogDebug("Path {QuestId} loaded.", o.Id); }
        public void OnQuestStateChange(QuestState n, QuestState old) => log.LogDebug("Path {QuestId}: {Old} -> {New}.", owner.Id, old, n);
    }
    /// <summary>Path spell-3. Quest 10570.</summary>
    [ScriptFilterOwnerId(10570u)]
    public class Q10570PathSpellQuestScript : IQuestScript, IOwnedScript<IQuest>
    {
        private readonly ILogger<Q10570PathSpellQuestScript> log; private IQuest owner;
        public Q10570PathSpellQuestScript(ILogger<Q10570PathSpellQuestScript> log) => this.log = log;
        public void OnLoad(IQuest o) { owner = o; log.LogDebug("Path {QuestId} loaded.", o.Id); }
        public void OnQuestStateChange(QuestState n, QuestState old) => log.LogDebug("Path {QuestId}: {Old} -> {New}.", owner.Id, old, n);
    }
}
