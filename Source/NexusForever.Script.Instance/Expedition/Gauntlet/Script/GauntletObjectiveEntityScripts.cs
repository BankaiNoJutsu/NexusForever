using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Expedition.Gauntlet.Script
{
    public abstract class GauntletTargetGroupObjectiveEntityScriptBase : IWorldEntityScript, IOwnedScript<IWorldEntity>
    {
        private readonly PublicEventObjectiveType objectiveType;
        private readonly uint targetGroupId;

        private IWorldEntity entity;
        private bool activated;

        protected GauntletTargetGroupObjectiveEntityScriptBase(
            PublicEventObjectiveType objectiveType,
            uint targetGroupId)
        {
            this.objectiveType = objectiveType;
            this.targetGroupId = targetGroupId;
        }

        public void OnLoad(IWorldEntity owner)
        {
            entity = owner;
        }

        public void OnActivateSuccess(IPlayer _)
        {
            if (activated)
                return;

            activated = true;
            entity.Map.PublicEventManager.UpdateObjective(objectiveType, targetGroupId, 1);
        }
    }

    /// <summary>
    /// Build 16042 maps objective 1837 to TargetGroup 6831, whose members are
    /// the three electric-room Door Lock objects 48682, 48683, and 48684.
    /// </summary>
    [ScriptFilterCreatureId(48682u, 48683u, 48684u)]
    public class ElectricRoomDoorLockEntityScript : GauntletTargetGroupObjectiveEntityScriptBase
    {
        public ElectricRoomDoorLockEntityScript()
            : base(PublicEventObjectiveType.ActivateTargetGroup, 6831u)
        {
        }
    }

    /// <summary>
    /// Gauntlet Score Token (Creature2 49577, found by name; its activate spell, Creature2 word 15, is Spell4 47579 "Collect Token" with a
    /// 750 ms cast and effects RavelSignal x2 and DespawnUnit). Interacting with a token credits "Collect Gauntlet Score Tokens" 4670
    /// (Script, count 20) once and removes the token. Evidence: the 2016-02-23 Gold Expeditions guide says "interact with 20 gauntlet score
    /// tokens" and its veteran frame shows the "Collect" prompt and the "[18/20]" tracker line. Where tokens spawn is NOT known: world
    /// 2183 holds no token spawn rows, so nothing places them yet. See INSTANCE_GAUNTLET_VIDEO_ANALYSIS_20261109.md.
    /// </summary>
    [ScriptFilterCreatureId(49577u)]
    public class GauntletScoreTokenEntityScript : IWorldEntityScript, IOwnedScript<IWorldEntity>
    {
        private IWorldEntity entity;
        private bool collected;

        public void OnLoad(IWorldEntity owner)
        {
            entity = owner;
        }

        public void OnActivateSuccess(IPlayer _)
        {
            if (collected)
                return;

            collected = true;
            entity.Map.PublicEventManager.UpdateObjective(PublicEventObjective.CollectTwentyGauntletScoreTokens, 1);
            entity.RemoveFromMap();
        }
    }
}
