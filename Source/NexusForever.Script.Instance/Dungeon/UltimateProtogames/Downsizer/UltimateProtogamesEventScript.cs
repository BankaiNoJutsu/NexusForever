using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Dungeon.UltimateProtogames.Downsizer
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

            // Build 16042 maps public event 642 to the Downsizer objective set.
            // Only objective 3197 has a mapped Creature2 death-credit producer;
            // the four challenge rows remain blocked on ability/mechanic proof.
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
                    // Completion boundary for the mapped Downsizer kill objective.
                    // Challenge semantics, encounter choreography, and rewards remain blocked.
                    publicEvent.Finish(PublicEventTeam.PublicTeam);
                    break;
            }
        }
    }
}
