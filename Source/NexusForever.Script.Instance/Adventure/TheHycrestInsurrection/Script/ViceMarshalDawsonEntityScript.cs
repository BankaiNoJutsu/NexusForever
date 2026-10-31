using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Adventure.TheHycrestInsurrection.Script
{
    /// <summary>Build16042 objective2113 is TalkTo target group7183, containing Dawson18365.</summary>
    [ScriptFilterCreatureId(18365)]
    public class ViceMarshalDawsonEntityScript : IWorldEntityScript, IOwnedScript<IWorldEntity>
    {
        private IWorldEntity entity;
        public void OnLoad(IWorldEntity owner) => entity = owner;
        public void OnActivateSuccess(IPlayer player)
        {
            if (entity?.Map?.Entry?.Id != 1149 || entity.CreatureId != 18365
                || player == null || !ReferenceEquals(player.Map, entity.Map)) return;
            entity.Map.PublicEventManager.UpdateObjective(player, PublicEventObjectiveType.TalkTo, 7183, 1);
        }
    }
}
