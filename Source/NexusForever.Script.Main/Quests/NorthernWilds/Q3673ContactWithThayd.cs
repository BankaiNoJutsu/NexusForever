using NexusForever.Game.Abstract.Cinematic;
using NexusForever.Game.Abstract.Cinematic.Cinematics;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.NorthernWilds
{
    /// <summary>
    /// Northern Wilds finale — Contact With Thayd.
    /// Quest 3673. On Achieved, plays cinematic. On Completed, grants the retail follow-up quest 3670.
    /// </summary>
    [ScriptFilterOwnerId(3673u)]
    public class Q3673ContactWithThaydQuestScript : IQuestScript, IOwnedScript<IQuest>
    {
        private const ushort NextQuestId = 3670;

        private IQuest owner;
        private readonly ICinematicFactory cinematicFactory;
        private readonly IGlobalQuestManager globalQuestManager;

        public Q3673ContactWithThaydQuestScript(
            ICinematicFactory cinematicFactory,
            IGlobalQuestManager globalQuestManager)
        {
            this.cinematicFactory = cinematicFactory;
            this.globalQuestManager = globalQuestManager;
        }

        public void OnLoad(IQuest owner)
        {
            this.owner = owner;
        }

        public void OnQuestStateChange(QuestState newState, QuestState oldState)
        {
            if (newState == QuestState.Achieved)
            {
                // WIP/GUESSED: Questing-and-more queues this cinematic on Achieved; exact retail turn-in/cinematic timing is not live-smoked.
                owner.Player.CinematicManager.QueueCinematic(cinematicFactory.CreateCinematic<IQ3673ContactWithThaydCinematic>());
            }

            if (newState != QuestState.Completed)
                return;

            GrantNext();
        }

        private void GrantNext()
        {
            if (owner.Player.QuestManager.GetQuestState(NextQuestId) != null)
                return;

            IQuestInfo info = globalQuestManager.GetQuestInfo(NextQuestId);
            if (info == null)
                return;

            owner.Player.QuestManager.QuestAdd(info);
        }
    }
}
