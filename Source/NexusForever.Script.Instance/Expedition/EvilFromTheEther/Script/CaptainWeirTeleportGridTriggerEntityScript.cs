using System.Numerics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Trigger;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Expedition.EvilFromTheEther.Script
{
    [ScriptFilterOwnerId(8243)]
    public class CaptainWeirTeleportGridTriggerEntityScript : IGridEntityScript, IOwnedScript<IGridTriggerEntity>
    {
        private IGridTriggerEntity trigger;

        /// <summary>
        /// Invoked when <see cref="IScript"/> is loaded.
        /// </summary>
        public void OnLoad(IGridTriggerEntity owner)
        {
            trigger = owner;
        }

        /// <summary>
        /// Invoked when <see cref="IGridEntity"/> is added to range check range.
        /// </summary>
        public void OnEnterRange(IGridEntity entity)
        {
            if (entity is not IPlayer player)
                return;

            // Owner trigger 8243 currently teleports to (-398.66, -842.03, 119.30) and
            // credits PublicEventObjectiveType.Script; expedition timing still needs smoke.
            player.TeleportToLocal(new Vector3(-398.65857f, -842.03436f, 119.298386f));
            trigger.Map.PublicEventManager.UpdateObjective(PublicEventObjectiveType.Script, 8243, 1);
        }
    }
}
