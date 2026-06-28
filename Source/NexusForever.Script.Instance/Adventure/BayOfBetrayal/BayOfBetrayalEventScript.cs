using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Adventure.BayOfBetrayal
{
    /// <summary>
    /// Parent adventure event. Trial child events PE 674, 675, 676, and 695
    /// remain blocked until retail branch, race, medal, and technopathy
    /// mechanics are mapped.
    /// </summary>
    [ScriptFilterOwnerId(673)]
    public class BayOfBetrayalEventScript : IPublicEventScript, IOwnedScript<IPublicEvent>
    {
        private IPublicEvent publicEvent;

        /// <summary>
        /// Invoked when <see cref="IScript"/> is loaded.
        /// </summary>
        public void OnLoad(IPublicEvent owner)
        {
            publicEvent = owner;
            publicEvent.SetPhase(PublicEventPhase.SurviveTheTrials);
        }

        /// <summary>
        /// Invoked when the public event phase changes.
        /// </summary>
        public void OnPublicEventPhase(uint phase)
        {
            switch ((PublicEventPhase)phase)
            {
                case PublicEventPhase.SurviveTheTrials:
                    publicEvent.ActivateObjective(PublicEventObjective.SurviveTheTrials);
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

            if ((PublicEventObjective)objective.Entry.Id == PublicEventObjective.SurviveTheTrials)
                publicEvent.Finish(PublicEventTeam.PublicTeam);
        }
    }
}
