using NexusForever.Game.Abstract.Cinematic;
using NexusForever.Game.Abstract.Cinematic.Cinematics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Abstract.Story;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Static.Spell;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.NorthernWilds
{
    [ScriptFilterOwnerId(3486u)]
    public class Q3486EmpoweredTowerQuestScript : IQuestScript, IOwnedScript<IQuest>
    {
        private const ushort QuestSettingUpCamp = 3671;
        private const ushort QuestSecuringTheArea = 3797;
        private const uint ArrivalZone = 729u;
        private const uint ArrivalObjective = 4987u;
        private const uint ArrivalStoryPanel = 1575u;

        private IQuest owner;

        private readonly ICinematicFactory cinematicFactory;
        private readonly IStoryBuilder storyBuilder;

        public Q3486EmpoweredTowerQuestScript(ICinematicFactory cinematicFactory, IStoryBuilder storyBuilder)
        {
            this.cinematicFactory = cinematicFactory;
            this.storyBuilder      = storyBuilder;
        }

        public void OnLoad(IQuest owner)
        {
            this.owner = owner;
        }

        public void OnQuestStateChange(QuestState newState, QuestState oldState)
        {
            if (newState == QuestState.Accepted)
                TryCreditArrivalInCurrentZone();

            if (newState == QuestState.Completed)
            {
                // WIP/GUESSED: Questing-and-more queues this cinematic on completion; exact retail timing and secondary follow-up mention visibility are not live-smoked.
                owner.Player.CinematicManager.QueueCinematic(cinematicFactory.CreateCinematic<IQ3486EmpoweredTowerCinematic>());
                owner.Player.QuestManager.QuestMention(QuestSettingUpCamp);
                owner.Player.QuestManager.QuestMention(QuestSecuringTheArea);
            }
        }

        private void TryCreditArrivalInCurrentZone()
        {
            if (owner.Player.Zone?.Id != ArrivalZone)
                return;

            // WIP/GUESSED: OnEnterZone can fire before this follow-up quest is accepted when the player is already at Exo-Lab 729.
            storyBuilder.SendServerStoryPanelShow(owner.Player, ArrivalStoryPanel);
            owner.Player.QuestManager.ObjectiveUpdate(ArrivalObjective, 1u);
        }
    }

    [ScriptFilterCreatureId(11205u)]
    public class Q3486LoftiteCrystalEntityScript : IWorldEntityScript, IOwnedScript<ICreatureEntity>
    {
        private ICreatureEntity owner;
        private bool collected;

        public void OnLoad(ICreatureEntity owner)
        {
            this.owner = owner;
        }

        public void OnAddToMap(IBaseMap map)
        {
            owner.SetInRangeCheck(Q3486LoftiteCrystalCollector.CollectionRange);
        }

        public void Update(double lastTick)
        {
            if (owner.Health == 0u || collected)
                return;

            foreach (IPlayer player in owner.GetInRange<IPlayer>(0u).ToList())
                if (TryCollect(player))
                    return;
        }

        public void OnEnterRange(IGridEntity entity)
        {
            if (entity is not IPlayer player)
                return;

            TryCollect(player);
        }

        private bool TryCollect(IPlayer player)
        {
            if (collected)
                return false;

            collected = Q3486LoftiteCrystalCollector.TryCollect(player, owner);
            return collected;
        }
    }

    internal static class Q3486LoftiteCrystalCollector
    {
        public const ushort QuestEmpoweredTower = 3486;
        public const uint ArrivalObjective      = 4987u;
        public const uint VirtualItemReward     = 206u;
        public const float CollectionRange      = 5f;

        public static bool TryCollect(IPlayer player, ICreatureEntity crystal)
        {
            if (player == null || crystal == null || crystal.Health == 0u)
                return false;

            if (player.QuestManager.GetQuestState(QuestEmpoweredTower) != QuestState.Accepted)
                return false;

            // WIP/GUESSED: QuestObjective 4485 tracks virtual item 206 from jumping into Loftite crystals; exact client collision volume is map-script recovered.
            player.QuestManager.ObjectiveUpdate(ArrivalObjective, 1u);
            player.QuestManager.ObjectiveUpdate(QuestObjectiveType.VirtualCollect, VirtualItemReward, 1u);

            crystal.ModifyHealth(crystal.Health, DamageType.Physical, null);
            return true;
        }
    }

    [ScriptFilterCreatureId(11924u, 11925u)]
    public class Q3486CrystalGuardianEntityScript : IUnitScript, IOwnedScript<ICreatureEntity>
    {
        private const ushort QuestEmpoweredTower = 3486;
        private const uint ArrivalObjective      = 4987u;

        public void OnLoad(ICreatureEntity owner)
        {
        }

        public void OnKilled(IUnitEntity killer)
        {
            if (killer is not IPlayer player)
                return;

            if (player.QuestManager.GetQuestState(QuestEmpoweredTower) != QuestState.Accepted)
                return;

            // QuestObjective 4485 TargetGroup 4985 covers Frostbite and Crystal Guardians as kill-credit sources.
            player.QuestManager.ObjectiveUpdate(ArrivalObjective, 1u);
            player.QuestManager.ObjectiveUpdate(QuestObjectiveType.VirtualCollect, Q3486LoftiteCrystalCollector.VirtualItemReward, 1u);
        }
    }
}
