using System.Collections.Generic;
using System.Numerics;
using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Entity.Movement.Spline;
using NexusForever.Game.Static.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.NorthernWilds
{
    /// <summary>
    /// Quest 3487 (Shellshock!) -> grants 3963 (More Important Than Revenge) on completion.
    /// </summary>
    [ScriptFilterOwnerId(3487u)]
    public class Q3487ShellshockQuestScript : IQuestScript, IOwnedScript<IQuest>
    {
        private const ushort NextQuestId = 3963;
        private readonly ILogger<Q3487ShellshockQuestScript> log;
        private readonly IGlobalQuestManager globalQuestManager;
        private IQuest owner;

        public Q3487ShellshockQuestScript(ILogger<Q3487ShellshockQuestScript> log, IGlobalQuestManager globalQuestManager)
        {
            this.log = log; this.globalQuestManager = globalQuestManager;
        }

        public void OnLoad(IQuest owner) { this.owner = owner; }

        public void OnQuestStateChange(QuestState newState, QuestState oldState)
        {
            if (newState == QuestState.Completed)
                GrantNext();
        }

        private void GrantNext()
        {
            if (owner.Player.QuestManager.GetQuestState(NextQuestId) != null) return;
            IQuestInfo info = globalQuestManager.GetQuestInfo(NextQuestId);
            if (info == null) { log.LogWarning("Next quest {NextId} info missing.", NextQuestId); return; }
            owner.Player.QuestManager.QuestAdd(info);
            log.LogDebug("Granted follow-up quest {NextId} after {QuestId}.", NextQuestId, owner.Id);
        }
    }

    [ScriptFilterCreatureId(11251u)]
    public class Q3487DominionCannonEntityScript : IWorldEntityScript, IOwnedScript<ICreatureEntity>
    {
        private ICreatureEntity owner;

        public void OnLoad(ICreatureEntity owner)
        {
            this.owner = owner;
        }

        public void OnActivateSuccess(IPlayer activator)
        {
            activator.QuestManager.ObjectiveUpdate(QuestObjectiveType.ScriptedTargetGroupChecklist, owner.CreatureId, owner.QuestChecklistIdx);
        }
    }

    [ScriptFilterCreatureId(12526u)]
    public class Q3487UltrabotEntityScript : IUnitScript, IOwnedScript<ICreatureEntity>
    {
        private const ushort AchievementWarbot = 1296;
        private static readonly Vector3 LocDestination = new(4450f, -700f, -5150f);

        private ICreatureEntity owner;

        public void OnLoad(ICreatureEntity owner)
        {
            this.owner = owner;
        }

        public void OnAddToMap(IBaseMap map)
        {
            owner.MovementManager.SetPositionPath(
                new List<Vector3> { LocDestination },
                SplineType.Linear,
                SplineMode.OneShot,
                5f);
        }

        public void OnKilled(IUnitEntity killer)
        {
            if (killer is IPlayer player)
                player.AchievementManager.GrantAchievement(AchievementWarbot);
        }
    }
}
