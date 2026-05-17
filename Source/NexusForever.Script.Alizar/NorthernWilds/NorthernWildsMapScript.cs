using NexusForever.Game.Abstract.Cinematic;
using NexusForever.Game.Abstract.Cinematic.Cinematics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Story;
using NexusForever.Game.Static.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Alizar.NorthernWilds
{
    [ScriptFilterOwnerId(426)]
    public class NorthernWildsMapScript : IMapScript, IOwnedScript<IBaseMap>
    {
        public enum Quest : ushort
        {
            ReportingForDuty = 3480,
            EmpoweredTower   = 3486
        }

        public enum Zones : ushort
        {
            EmpoweredTower = 729
        }

        public enum Objective : uint
        {
            ArrivedAtTower = 4987
        }

        private const uint ArrivedAtTowerStoryPanel = 1575;

        #region Dependency Injection

        private readonly ICinematicFactory cinematicFactory;
        private readonly IStoryBuilder storyBuilder;

        public NorthernWildsMapScript(
            ICinematicFactory cinematicFactory,
            IStoryBuilder storyBuilder)
        {
            this.cinematicFactory = cinematicFactory;
            this.storyBuilder     = storyBuilder;
        }

        #endregion

        public void OnAddToMap(IGridEntity entity)
        {
            if (entity is not IPlayer player)
                return;

            if (player.QuestManager.GetQuestState(Quest.ReportingForDuty) == null)
            {
                player.CinematicManager.QueueCinematic(cinematicFactory.CreateCinematic<INorthernWildsOnCreate>());
            }
        }

        public void OnEnterZone(IWorldEntity entity, uint zone)
        {
            if (entity is not IPlayer player)
                return;

            if (zone != (uint)Zones.EmpoweredTower)
                return;

            if (player.QuestManager.GetQuestState(Quest.EmpoweredTower) != QuestState.Accepted)
                return;

            storyBuilder.SendServerStoryPanelShow(player, ArrivedAtTowerStoryPanel);
            player.QuestManager.ObjectiveUpdate((uint)Objective.ArrivedAtTower, 1u);
        }
    }
}
