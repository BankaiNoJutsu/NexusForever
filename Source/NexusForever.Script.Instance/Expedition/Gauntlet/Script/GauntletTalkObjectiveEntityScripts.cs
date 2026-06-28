using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Expedition.Gauntlet.Script
{
    public abstract class GauntletTalkObjectiveEntityScriptBase : IWorldEntityScript, IOwnedScript<IWorldEntity>
    {
        private readonly uint targetGroupId;
        private IWorldEntity entity;

        protected GauntletTalkObjectiveEntityScriptBase(uint targetGroupId)
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
    /// Build 16042 maps objective 1821 to TargetGroup 6818,
    /// whose Creature2 member is Pilot Taboro 48580.
    /// </summary>
    [ScriptFilterCreatureId(48580u)]
    public class PilotTaboroEntityScript : GauntletTalkObjectiveEntityScriptBase
    {
        public PilotTaboroEntityScript()
            : base(6818u)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps objective 1914 to TargetGroup 6898,
    /// whose Creature2 member is Judge Kain 48945.
    /// </summary>
    [ScriptFilterCreatureId(48945u)]
    public class JudgeKainEntityScript : GauntletTalkObjectiveEntityScriptBase
    {
        public JudgeKainEntityScript()
            : base(6898u)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps objective 1915 to TargetGroup 6899,
    /// whose Creature2 member is Agent Lex 48950.
    /// </summary>
    [ScriptFilterCreatureId(48950u)]
    public class AgentLexEntityScript : GauntletTalkObjectiveEntityScriptBase
    {
        public AgentLexEntityScript()
            : base(6899u)
        {
        }
    }
}
