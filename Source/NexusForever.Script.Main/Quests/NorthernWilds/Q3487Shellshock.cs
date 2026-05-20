using System.Collections.Generic;
using System.Numerics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Entity.Movement.Spline;
using NexusForever.Game.Static.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.NorthernWilds
{
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
