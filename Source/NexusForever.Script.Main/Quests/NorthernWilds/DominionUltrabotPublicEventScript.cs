using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.NorthernWilds
{
    /// <summary>
    /// Northern Wilds public event 154: Dominion Ultrabot.
    /// </summary>
    [ScriptFilterOwnerId(154)]
    public class DominionUltrabotPublicEventScript : IPublicEventScript, IOwnedScript<IPublicEvent>
    {
        private const uint DominionUltrabotCreatureId = 12526u;
        private const uint DefeatDominionUltrabotObjectiveId = 371u;

        private IPublicEvent owner;

        public void OnLoad(IPublicEvent owner)
        {
            this.owner = owner;
        }

        public void OnDeath(IUnitEntity entity)
        {
            if (entity.CreatureId != DominionUltrabotCreatureId)
                return;

            owner.UpdateObjective(DefeatDominionUltrabotObjectiveId, 1);
        }
    }
}
