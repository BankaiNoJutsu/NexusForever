using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Raid.UltimateProtogames
{
    [ScriptFilterOwnerId(642)]
    public class UltimateProtogamesEventScript : IPublicEventScript, IOwnedScript<IPublicEvent>
    {
        private IPublicEvent publicEvent;

        /// <summary>
        /// Invoked when <see cref="IScript"/> is loaded.
        /// </summary>
        public void OnLoad(IPublicEvent owner)
        {
            publicEvent = owner;

            // WIP-guessed from LaughingWS Instances-and-more: the branch supplies
            // only this raid objective catalog and a map binding. Activate the
            // Downsizer objective set, but leave exact boss mechanics/rewards blocked.
            publicEvent.ActivateObjective(PublicEventObjective.DefeatTheDownsizer);
            publicEvent.ActivateObjective(PublicEventObjective.VoltaicConversion);
            publicEvent.ActivateObjective(PublicEventObjective.Overcharge);
            publicEvent.ActivateObjective(PublicEventObjective.ElectrostaticDynamo);
            publicEvent.ActivateObjective(PublicEventObjective.ElectromagneticInduction);
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
                case PublicEventObjective.DefeatTheDownsizer:
                    // WIP-guessed completion boundary. Challenge objective semantics,
                    // encounter choreography, and rewards remain blocked pending proof.
                    publicEvent.Finish(PublicEventTeam.PublicTeam);
                    break;
            }
        }
    }
}
