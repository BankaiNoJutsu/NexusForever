using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Dungeon.UltimateProtogames.Script
{
    /// <summary>
    /// Build 16042 objective 4535 is an initially active TalkTo counter for
    /// TargetGroup 12376, whose type-1 member is Creature2 68272, the
    /// Protostar Event Specialist.
    /// </summary>
    [ScriptFilterCreatureId(68272u)]
    public class ProtostarEventSpecialistEntityScript : IWorldEntityScript, IOwnedScript<IWorldEntity>
    {
        private const uint ProtostarEventSpecialistTargetGroupId = 12376u;

        private IWorldEntity entity;

        public void OnLoad(IWorldEntity owner)
        {
            entity = owner;
        }

        public void OnActivateSuccess(IPlayer player)
        {
            entity.Map.PublicEventManager.UpdateObjective(
                player,
                PublicEventObjectiveType.TalkTo,
                ProtostarEventSpecialistTargetGroupId,
                1);
        }
    }
}
