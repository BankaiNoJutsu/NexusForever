using NexusForever.Game.Abstract.Entity;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Dungeon.UltimateProtogames.Script
{
    /// <summary>
    /// Build 16042 Creature2 65900 is the [UP] w2980 initiation button; use a
    /// script-name filter because the same Creature2 id is reused by Protogames Academy.
    /// </summary>
    [ScriptFilterScriptName("UltimateProtogamesStartButtonEntityScript")]
    public class UltimateProtogamesStartButtonEntityScript : IWorldEntityScript, IOwnedScript<IWorldEntity>
    {
        private IWorldEntity entity;
        private bool activated;

        public void OnLoad(IWorldEntity owner)
        {
            entity = owner;
        }

        public void OnActivateSuccess(IPlayer _)
        {
            if (activated)
                return;

            activated = true;
            entity.Map.PublicEventManager.UpdateObjective(PublicEventObjective.InitiateUltimateProtogames, 1);
        }
    }
}
