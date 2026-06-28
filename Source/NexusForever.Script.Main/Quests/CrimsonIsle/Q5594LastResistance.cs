using System.Numerics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.CrimsonIsle
{
    [ScriptFilterCreatureId(39962u)]
    public class Q5594ShipControlsEntityScript : IWorldEntityScript, IOwnedScript<ICreatureEntity>
    {
        private const ushort WorldOlyssia = 22;
        private static readonly Vector3 LocDerradunBloodfireVillage = new(-5750.767f, -971.7648f, -623.33295f);
        private static readonly Vector3 RotDerradunBloodfireVillage = new(-1.1721236f, 0f, 0f);

        private ICreatureEntity owner;

        public void OnLoad(ICreatureEntity owner)
        {
            this.owner = owner;
        }

        public void OnActivateSuccess(IPlayer activator)
        {
            if (activator.QuestManager.GetQuestState(5594) == QuestState.Accepted)
                activator.QuestManager.QuestAchieve(5594);

            // WIP/GUESSED: the branch teleports after activation even when quest credit is not active; exact access gating remains un-smoked.
            if (!activator.CanTeleport())
                return;

            activator.Rotation = RotDerradunBloodfireVillage;
            activator.TeleportTo(
                WorldOlyssia,
                LocDerradunBloodfireVillage.X,
                LocDerradunBloodfireVillage.Y,
                LocDerradunBloodfireVillage.Z,
                reason: TeleportReason.Relocate);
        }
    }

    [ScriptFilterCreatureId(31792u)]
    public class Q5594WarbotEntityScript : IUnitScript, IOwnedScript<ICreatureEntity>
    {
        private const ushort Q5594LastResistance = 5594;
        private const ushort AchievementWarbot = 1730;
        private const uint QObjWarbotKill      = 8249u;

        private ICreatureEntity owner;

        public void OnLoad(ICreatureEntity owner)
        {
            this.owner = owner;
        }

        public void OnKilled(IUnitEntity killer)
        {
            if (killer is not IPlayer player)
                return;

            if (player.QuestManager.GetQuestState(Q5594LastResistance) != QuestState.Accepted)
                return;

            if (!player.AchievementManager.HasCompletedAchievement(AchievementWarbot))
                player.AchievementManager.GrantAchievement(AchievementWarbot);

            player.QuestManager.ObjectiveUpdate(QObjWarbotKill, 1u);
        }
    }
}
