using NexusForever.Game.Abstract.Entity;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Dungeon.StormtalonsLair.Script
{
    /// <summary>
    /// Build 16042 maps public-event objective 562 to TargetGroup 4419,
    /// whose Creature2 member is the Thundercall Storm Totem 27262.
    /// </summary>
    [ScriptFilterCreatureId(27262u)]
    public class ThundercallStormTotemEntityScript : IWorldEntityScript, IOwnedScript<IWorldEntity>
    {
        private IWorldEntity entity;

        public void OnLoad(IWorldEntity owner)
        {
            entity = owner;
        }

        public void OnActivateSuccess(IPlayer _)
        {
            entity.Map.PublicEventManager.UpdateObjective(PublicEventObjective.HijackPowerFromTheThundercallStormTotems, 1);
        }
    }
}
