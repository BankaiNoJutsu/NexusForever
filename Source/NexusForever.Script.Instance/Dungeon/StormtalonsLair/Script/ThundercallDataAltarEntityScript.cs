using NexusForever.Game.Abstract.Entity;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Dungeon.StormtalonsLair.Script
{
    /// <summary>
    /// Build 16042 maps public-event objective 555 to the Stormtalon's Lair
    /// Thundercall Data Altar Creature2 row.
    /// </summary>
    [ScriptFilterCreatureId(24314u)]
    public class ThundercallDataAltarEntityScript : IWorldEntityScript, IOwnedScript<IWorldEntity>
    {
        private IWorldEntity entity;

        public void OnLoad(IWorldEntity owner)
        {
            entity = owner;
        }

        public void OnActivateSuccess(IPlayer _)
        {
            entity.Map.PublicEventManager.UpdateObjective(PublicEventObjective.ObtainDataFromTheThundercallDataAltar, 1);
        }
    }
}
