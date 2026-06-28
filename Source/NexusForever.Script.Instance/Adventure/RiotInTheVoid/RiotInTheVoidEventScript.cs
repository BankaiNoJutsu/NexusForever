using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Adventure.RiotInTheVoid
{
    /// <summary>
    /// Parent adventure event. Public-event votes, mission routing, and child
    /// events PE 403-417 remain blocked until retail selection semantics are mapped.
    /// </summary>
    [ScriptFilterOwnerId(179)]
    public class RiotInTheVoidEventScript : IPublicEventScript, IOwnedScript<IPublicEvent>
    {
        private IPublicEvent publicEvent;

        /// <summary>
        /// Invoked when <see cref="IScript"/> is loaded.
        /// </summary>
        public void OnLoad(IPublicEvent owner)
        {
            publicEvent = owner;
            publicEvent.SetPhase(PublicEventPhase.QuellRiotInAstrovoidPrison);
        }

        /// <summary>
        /// Invoked when the public event phase changes.
        /// </summary>
        public void OnPublicEventPhase(uint phase)
        {
            switch ((PublicEventPhase)phase)
            {
                case PublicEventPhase.QuellRiotInAstrovoidPrison:
                    publicEvent.ActivateObjective(PublicEventObjective.QuellRiotInAstrovoidPrison);
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

            if ((PublicEventObjective)objective.Entry.Id == PublicEventObjective.QuellRiotInAstrovoidPrison)
                publicEvent.Finish(PublicEventTeam.PublicTeam);
        }
    }
}
