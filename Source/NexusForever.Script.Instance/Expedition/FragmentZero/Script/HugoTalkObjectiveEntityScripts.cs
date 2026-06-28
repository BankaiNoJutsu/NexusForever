using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Expedition.FragmentZero.Script
{
    public abstract class FragmentZeroTalkObjectiveEntityScriptBase : IWorldEntityScript, IOwnedScript<IWorldEntity>
    {
        private readonly uint targetGroupId;
        private IWorldEntity entity;

        protected FragmentZeroTalkObjectiveEntityScriptBase(uint targetGroupId)
        {
            this.targetGroupId = targetGroupId;
        }

        public void OnLoad(IWorldEntity owner)
        {
            entity = owner;
        }

        public void OnActivateSuccess(IPlayer player)
        {
            entity.Map.PublicEventManager.UpdateObjective(
                player,
                PublicEventObjectiveType.TalkTo,
                targetGroupId,
                1);
        }
    }

    /// <summary>
    /// Build 16042 maps objective 4641 to TargetGroup 12471,
    /// whose Creature2 member is Captain Hugo aura 68886.
    /// </summary>
    [ScriptFilterCreatureId(68886u)]
    public class CaptainHugoAuraEntityScript : FragmentZeroTalkObjectiveEntityScriptBase
    {
        public CaptainHugoAuraEntityScript()
            : base(12471u)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps objective 4642 to TargetGroup 12458,
    /// whose Creature2 member is Captain Hugo event-end 68854.
    /// </summary>
    [ScriptFilterCreatureId(68854u)]
    public class CaptainHugoEventEndEntityScript : FragmentZeroTalkObjectiveEntityScriptBase
    {
        public CaptainHugoEventEndEntityScript()
            : base(12458u)
        {
        }
    }
}
