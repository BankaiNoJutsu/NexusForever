using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Expedition.RageLogic
{
    [ScriptFilterOwnerId(214)]
    public class RageLogicEventScript : IPublicEventScript, IOwnedScript<IPublicEvent>
    {
        private IPublicEvent publicEvent;

        /// <summary>
        /// Invoked when <see cref="IScript"/> is loaded.
        /// </summary>
        public void OnLoad(IPublicEvent owner)
        {
            publicEvent = owner;
            publicEvent.SetPhase(PublicEventPhase.ObliterateRagebotsDefendingTheAsteroid);
        }

        /// <summary>
        /// Invoked when the public event phase changes.
        /// </summary>
        public void OnPublicEventPhase(uint phase)
        {
            switch ((PublicEventPhase)phase)
            {
                case PublicEventPhase.ObliterateRagebotsDefendingTheAsteroid:
                    publicEvent.ActivateObjective(PublicEventObjective.ObliterateRagebotsDefendingAsteroid);
                    break;
                case PublicEventPhase.DestroyAsteroidEngines:
                    publicEvent.ActivateObjective(PublicEventObjective.DestroyAsteroidEngines);
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
                case PublicEventObjective.ObliterateRagebotsDefendingAsteroid:
                    publicEvent.SetPhase(PublicEventPhase.DestroyAsteroidEngines);
                    break;
                case PublicEventObjective.DestroyAsteroidEngines:
                    // Stop at the first proven Rage Logic runtime slice. Factory,
                    // teleporter, Axiom, reward, and medal routing remain blocked.
                    break;
            }
        }
    }
}
