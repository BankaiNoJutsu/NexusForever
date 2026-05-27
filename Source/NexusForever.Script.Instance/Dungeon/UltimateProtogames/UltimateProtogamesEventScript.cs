using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Dungeon.UltimateProtogames
{
    [ScriptFilterOwnerId(594)]
    public class UltimateProtogamesEventScript : IPublicEventScript, IOwnedScript<IPublicEvent>
    {
        private IPublicEvent publicEvent;

        /// <summary>
        /// Invoked when <see cref="IScript"/> is loaded.
        /// </summary>
        public void OnLoad(IPublicEvent owner)
        {
            publicEvent = owner;
            publicEvent.SetPhase(PublicEventPhase.Welcome);
        }

        /// <summary>
        /// Invoked when the public event phase changes.
        /// </summary>
        public void OnPublicEventPhase(uint phase)
        {
            switch ((PublicEventPhase)phase)
            {
                case PublicEventPhase.Welcome:
                    // WIP-guessed from LaughingWS Instances-and-more: the branch exposes
                    // Ultimate Protogames phase/objective ids but no event script. Only the
                    // entry objective is wired until room randomisation and boss routing are proven.
                    publicEvent.ActivateObjective(PublicEventObjective.InitiateUltimateProtogames);
                    break;
            }
        }

        /// <summary>
        /// Invoked when the <see cref="IPublicEventObjective"/> status changes.
        /// </summary>
        public void OnPublicEventObjectiveStatus(IPublicEventObjective objective)
        {
            if (objective.Status != PublicEventStatus.Succeeded)
                return;

            switch ((PublicEventObjective)objective.Entry.Id)
            {
                case PublicEventObjective.InitiateUltimateProtogames:
                    // The branch names later random-event phases but has no route selector.
                    // Stop at the coarse gate instead of inventing room order or rewards.
                    publicEvent.SetPhase(PublicEventPhase.RandomEvent1);
                    break;
            }
        }
    }
}
