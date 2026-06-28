using NexusForever.Game.Abstract.Entity;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Adventure.WarOfTheWilds.Script
{
    /// <summary>
    /// Build 16042 public event 158 maps objectives 408 and 1659 to TargetGroup
    /// 2812, count 100, whose only member is Creature2 25952, Giant Moodie Totem - NWAdv.
    /// </summary>
    [ScriptFilterCreatureId(25952u)]
    public class GiantMoodieTotemEntityScript : IUnitScript, IOwnedScript<ICreatureEntity>
    {
        private ICreatureEntity entity;
        private bool credited;

        public void OnLoad(ICreatureEntity owner)
        {
            entity = owner;
        }

        /// <summary>
        /// Invoked when <see cref="IUnitEntity"/> is killed.
        /// </summary>
        public void OnDeath()
        {
            if (credited)
                return;

            credited = true;
            entity.Map.PublicEventManager.UpdateObjective(PublicEventObjective.DestroyTheGiantMoodieTotem, 100);
            entity.Map.PublicEventManager.UpdateObjective(PublicEventObjective.MoodieTotemHealth, 100);
        }
    }
}
