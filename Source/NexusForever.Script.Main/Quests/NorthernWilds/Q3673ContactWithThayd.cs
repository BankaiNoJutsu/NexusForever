using NexusForever.Game.Abstract.Cinematic;
using NexusForever.Game.Abstract.Cinematic.Cinematics;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.NorthernWilds
{
    [ScriptFilterOwnerId(3673u)]
    public class Q3673ContactWithThaydQuestScript : IQuestScript, IOwnedScript<IQuest>
    {
        private IQuest owner;

        private readonly ICinematicFactory cinematicFactory;

        public Q3673ContactWithThaydQuestScript(ICinematicFactory cinematicFactory)
        {
            this.cinematicFactory = cinematicFactory;
        }

        public void OnLoad(IQuest owner)
        {
            this.owner = owner;
        }

        public void OnQuestStateChange(QuestState newState, QuestState oldState)
        {
            if (newState == QuestState.Achieved)
                owner.Player.CinematicManager.QueueCinematic(cinematicFactory.CreateCinematic<IQ3673ContactWithThaydCinematic>());
        }
    }
}
