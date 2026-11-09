using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Dungeon.RuinsOfKelVoreth.Script
{
    [ScriptFilterOwnerId(449)]
    public class TheExaniteForgesMessageTriggerScript : RuinsOfKelVorethMessageTriggerScriptBase
    {
        private const uint RuinsOfKelVorethEventId = 161u;

        private bool osunEliminationStarted;

        public TheExaniteForgesMessageTriggerScript(
            IGlobalQuestManager globalQuestManager)
            : base(globalQuestManager, CommunicatorMessage.AvraDarkos8)
        {
        }

        /// <summary>
        /// Starts the timed "Osun elimination" challenge (846, 10 minutes, 15 kills) when the group reaches the Exanite Forges.
        /// </summary>
        /// <remarks>
        /// Observed in two retail-era clips (2014 Carbine stream: absent at 1850 s, present at 1900 s; 2015 veteran guide:
        /// added about 15 s after the other last-section rows). That this trigger is the moment is Correlated, not
        /// verified; the retail trigger placement is not read from any table.
        /// </remarks>
        protected override void OnPlayerEnter(IMapInstance mapInstance)
        {
            if (osunEliminationStarted)
                return;

            IPublicEvent publicEvent = mapInstance.PublicEventManager?.GetEvent(RuinsOfKelVorethEventId);
            if (publicEvent == null || publicEvent.Phase != (uint)PublicEventPhase.ForgeMasterTrogun)
                return;

            osunEliminationStarted = true;
            publicEvent.ActivateObjective(PublicEventObjective.KillAsManyVorethOsunAsYouCanBeforeTimeExpires);
        }
    }
}
