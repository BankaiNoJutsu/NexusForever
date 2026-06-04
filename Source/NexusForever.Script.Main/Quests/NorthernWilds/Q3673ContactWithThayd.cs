using Microsoft.Extensions.Logging;
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
    public class Q3673ContactWithThaydQuestScript : FollowUpQuestScript<Q3673ContactWithThaydQuestScript>
    {
        protected override ushort NextQuestId => 3670;

        private readonly ICinematicFactory cinematicFactory;

        public Q3673ContactWithThaydQuestScript(
            ICinematicFactory cinematicFactory,
            ILogger<Q3673ContactWithThaydQuestScript> log,
            IGlobalQuestManager globalQuestManager)
            : base(log, globalQuestManager)
        {
            this.cinematicFactory = cinematicFactory;
        }

        public override void OnQuestStateChange(QuestState newState, QuestState oldState)
        {
            if (newState == QuestState.Achieved)
            {
                // WIP/GUESSED: Questing-and-more queues this cinematic on Achieved; exact retail turn-in/cinematic timing is not live-smoked.
                Owner.Player.CinematicManager.QueueCinematic(cinematicFactory.CreateCinematic<IQ3673ContactWithThaydCinematic>());
            }

            base.OnQuestStateChange(newState, oldState);
        }
    }
}
