using System.Numerics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Trigger;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Expedition.EvilFromTheEther.Script
{
    [ScriptFilterOwnerId(8242)]
    public class UpperDeckTeleportGridTriggerEntityScript : IGridEntityScript, IOwnedScript<IGridTriggerEntity>
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

            // Owner trigger 8242 currently teleports to (-53.35, -841.45, 164.51) and
            // credits PublicEventObjectiveType.Script; trigger timing still needs smoke.
            player.TeleportToLocal(new Vector3(-53.353714f, -841.44684f, 164.51099f), false);
            trigger.Map.PublicEventManager.UpdateObjective(PublicEventObjectiveType.Script, 8242, 1);
        }
    }
}
