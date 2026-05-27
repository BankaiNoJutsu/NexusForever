using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Adventure.WarOfTheWilds
{
    /// <summary>
    /// WIP-guessed from LaughingWS Instances-and-more: this keeps the branch's base
    /// Moodie/Skeech totem objective scaffold only. Faction-specific start events,
    /// end delay, chat timing, and adventure rewards remain blocked pending smoke.
    /// </summary>
    [ScriptFilterOwnerId(158)]
    public class WarOfTheWildsAdventureEventScript : IPublicEventScript, IOwnedScript<IPublicEvent>
    {
        private IPublicEvent publicEvent;

        /// <summary>
        /// Invoked when <see cref="IScript"/> is loaded.
        /// </summary>
        public void OnLoad(IPublicEvent owner)
        {
            publicEvent = owner;
            publicEvent.SetPhase(PublicEventPhase.Fight);
        }

        /// <summary>
        /// Invoked when the public event phase changes.
        /// </summary>
        public void OnPublicEventPhase(uint phase)
        {
            switch ((PublicEventPhase)phase)
            {
                case PublicEventPhase.Fight:
                    OnPhaseFight();
                    break;
            }
        }

        private void OnPhaseFight()
        {
            publicEvent.ActivateObjective(PublicEventObjective.DestroyTheGiantMoodieTotem);
            publicEvent.ActivateObjective(PublicEventObjective.MoodieTotemHealth);
            publicEvent.ActivateObjective(PublicEventObjective.SkeechTotemHealth);
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
                case PublicEventObjective.DestroyTheGiantMoodieTotem:
                    publicEvent.Finish(PublicEventTeam.PublicTeam);
                    break;
            }
        }
    }
}
