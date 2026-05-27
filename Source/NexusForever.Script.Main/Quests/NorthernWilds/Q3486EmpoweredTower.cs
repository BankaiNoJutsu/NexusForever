using NexusForever.Game.Abstract.Cinematic;
using NexusForever.Game.Abstract.Cinematic.Cinematics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Loot;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Static.Spell;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.NorthernWilds
{
    [ScriptFilterOwnerId(3486u)]
    public class Q3486EmpoweredTowerQuestScript : IQuestScript, IOwnedScript<IQuest>
    {
        private const ushort QuestSettingUpCamp = 3671;
        private const ushort QuestSecuringTheArea = 3797;

        private IQuest owner;

        private readonly ICinematicFactory cinematicFactory;

        public Q3486EmpoweredTowerQuestScript(ICinematicFactory cinematicFactory)
        {
            this.cinematicFactory = cinematicFactory;
        }

        public void OnLoad(IQuest owner)
        {
            this.owner = owner;
        }

        public void OnQuestStateChange(QuestState newState, QuestState oldState)
        {
            if (newState == QuestState.Completed)
            {
                // WIP/GUESSED: Questing-and-more queues this cinematic on completion; exact retail timing and secondary follow-up mention visibility are not live-smoked.
                owner.Player.CinematicManager.QueueCinematic(cinematicFactory.CreateCinematic<IQ3486EmpoweredTowerCinematic>());
                owner.Player.QuestManager.QuestMention(QuestSettingUpCamp);
                owner.Player.QuestManager.QuestMention(QuestSecuringTheArea);
            }
        }
    }

    [ScriptFilterCreatureId(11205u)]
    public class Q3486LoftiteCrystalEntityScript : IWorldEntityScript, IOwnedScript<ICreatureEntity>
    {
        private const ushort QuestEmpoweredTower = 3486;
        private const uint VirtualItemReward     = 206u;

        private ICreatureEntity owner;

        private readonly IGlobalLootManager globalLootManager;
        private readonly IGameTableManager gameTableManager;

        public Q3486LoftiteCrystalEntityScript(IGlobalLootManager globalLootManager, IGameTableManager gameTableManager)
        {
            this.globalLootManager = globalLootManager;
            this.gameTableManager  = gameTableManager;
        }

        public void OnLoad(ICreatureEntity owner)
        {
            this.owner = owner;
        }

        public void OnAddToMap(IBaseMap map)
        {
            owner.SetInRangeCheck(5f);
        }

        public void OnEnterRange(IGridEntity entity)
        {
            if (entity is not IPlayer player)
                return;

            if (player.QuestManager.GetQuestState(QuestEmpoweredTower) != QuestState.Accepted)
                return;

            // WIP/GUESSED: branch grants virtual item 206 and despawns the crystal on proximity; exact loot presentation/respawn timing is not live-smoked.
            VirtualItemEntry rewardItem = gameTableManager.VirtualItem.GetEntry(VirtualItemReward);
            globalLootManager.GiveLoot(player, rewardItem, 1u, owner.Guid);
            owner.ModifyHealth(owner.Health, DamageType.Physical, null);
        }
    }
}
