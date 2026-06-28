using System.Collections.Generic;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Trigger;
using NexusForever.Script.Template;

namespace NexusForever.Script.Instance.Dungeon.Skullcano.Script
{
    public abstract class SkullcanoGroupObjectiveTriggerScript : IGridEntityScript, IOwnedScript<IGridTriggerEntity>
    {
        private readonly HashSet<ulong> creditedCharacters = [];

        private IGridTriggerEntity trigger;

        protected abstract PublicEventObjective Objective { get; }

        public void OnLoad(IGridTriggerEntity owner)
        {
            trigger = owner;
        }

        public void OnEnterRange(IGridEntity entity)
        {
            if (entity is not IPlayer player)
                return;

            if (!creditedCharacters.Add(player.CharacterId))
                return;

            trigger.Map.PublicEventManager.UpdateObjective(Objective, 1);
        }
    }
}
