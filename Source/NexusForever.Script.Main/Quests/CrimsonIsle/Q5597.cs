using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.CrimsonIsle
{
    /// <summary>
    /// Crimson Isle: intermediate quest after Q5596 merge gate.
    /// Grants Q5604 (Tactical Demolitions) on completion.
    /// </summary>
    [ScriptFilterOwnerId(5597u)]
    public class Q5597QuestScript : FollowUpQuestScript<Q5597QuestScript>
    {
        protected override ushort NextQuestId => 5604;

        public Q5597QuestScript(
            ILogger<Q5597QuestScript> log,
            IGlobalQuestManager globalQuestManager)
            : base(log, globalQuestManager)
        {
        }
    }

    public abstract class Q5597ChuaExplosivesEntityScriptBase : IWorldEntityScript
    {
        private const ushort QuestDregsAndThieves = 5597;
        private const uint ChuaExplosivesVirtualItem = 364u;

        private IWorldEntity owner;
        private bool collected;

        protected void OnLoad(IWorldEntity owner)
        {
            this.owner = owner;
        }

        public void OnActivateSuccess(IPlayer activator)
        {
            if (collected)
                return;

            if (activator.QuestManager.GetQuestState(QuestDregsAndThieves) != QuestState.Accepted)
                return;

            collected = true;
            activator.QuestManager.ObjectiveUpdate(QuestObjectiveType.VirtualCollect, ChuaExplosivesVirtualItem, 1u);
            owner.RemoveFromMap();
        }
    }

    [ScriptFilterCreatureId(24286u)]
    public class Q5597ChuaExplosivesEntityScript : Q5597ChuaExplosivesEntityScriptBase, IOwnedScript<ICollectableUnitEntity>
    {
        public void OnLoad(ICollectableUnitEntity owner)
        {
            OnLoad((IWorldEntity)owner);
        }
    }

    [ScriptFilterCreatureId(24286u)]
    public class Q5597ChuaExplosivesCreatureEntityScript : Q5597ChuaExplosivesEntityScriptBase, IOwnedScript<ICreatureEntity>
    {
        public void OnLoad(ICreatureEntity owner)
        {
            OnLoad((IWorldEntity)owner);
        }
    }
}
