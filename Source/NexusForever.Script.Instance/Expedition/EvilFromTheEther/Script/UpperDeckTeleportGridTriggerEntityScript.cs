using System.Collections.Generic;
using System.Numerics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Trigger;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Expedition.EvilFromTheEther.Script
{
    [ScriptFilterOwnerId(8242)]
    public class UpperDeckTeleportGridTriggerEntityScript : IGridEntityScript, IOwnedScript<IGridTriggerEntity>
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

            // Build 16042 objective 4919 is the upper-deck Script row for
            // objectId 8242. Credit the direct objective so other 8242 rows stay
            // phase-owned; trigger timing still needs smoke.
            player.TeleportToLocal(new Vector3(-53.353714f, -841.44684f, 164.51099f), false);
            if (!creditedCharacters.Add(player.CharacterId))
                return;

            trigger.Map.PublicEventManager.UpdateObjective(PublicEventObjective.TeleportToUpperDeck, 1);
        }
    }
}
