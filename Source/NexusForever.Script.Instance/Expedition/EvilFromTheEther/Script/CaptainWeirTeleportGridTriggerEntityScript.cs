using System.Collections.Generic;
using System.Numerics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Trigger;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Expedition.EvilFromTheEther.Script
{
    [ScriptFilterOwnerId(8243)]
    public class CaptainWeirTeleportGridTriggerEntityScript : IGridEntityScript, IOwnedScript<IGridTriggerEntity>
    {
        private readonly HashSet<ulong> creditedCharacters = [];

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

            // Build 16042 objective 4927 is the escape Script row that shares
            // objectId 8242 with the upper-deck teleport. Credit it directly;
            // expedition timing still needs smoke.
            player.TeleportToLocal(new Vector3(-398.65857f, -842.03436f, 119.298386f));
            if (!creditedCharacters.Add(player.CharacterId))
                return;

            trigger.Map.PublicEventManager.UpdateObjective(PublicEventObjective.EscapeToTheTeleporter, 1);
        }
    }
}
