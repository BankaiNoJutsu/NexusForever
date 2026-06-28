using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Cinematic;
using NexusForever.Game.Abstract.Cinematic.Cinematics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Static.Quest;
using NexusForever.GameTable;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.CrimsonIsle
{
    /// <summary>
    /// Map script for Crimson Isle (world 870).
    /// </summary>
    [ScriptFilterOwnerId(870)]
    public class CrimsonIsleMapScript : IMapScript, IOwnedScript<IBaseMap>
    {
        private const ushort Q5593MindTheMinesQuest = 5593;

        private const ushort Q5596OrdnanceRecoveryQuest = 5596;
        private const uint Q5596CrashSiteZoneId = 1611u;
        private const uint Q5596CrashSiteObjective = 8255u;

        private readonly ICinematicFactory cinematicFactory;

        public CrimsonIsleMapScript(
            ILogger<CrimsonIsleMapScript> log,
            IEntityFactory entityFactory,
            IGameTableManager gameTableManager,
            ICinematicFactory cinematicFactory)
        {
            this.cinematicFactory = cinematicFactory;
        }

        public void OnLoad(IBaseMap owner)
        {
        }

        public void Update(double lastTick) { }

        public void OnAddToMap(IGridEntity entity)
        {
            if (entity is not IPlayer player)
                return;

            if (player.QuestManager.GetQuestState(Q5593MindTheMinesQuest) != null)
                return;

            player.CinematicManager.QueueCinematic(cinematicFactory.CreateCinematic<ICrimsonIsleOnCreate>());
        }

        public void OnRemoveFromMap(IGridEntity entity) { }

        public void OnEnterZone(IWorldEntity entity, uint zone)
        {
            if (entity is not IPlayer player)
                return;

            if (zone != Q5596CrashSiteZoneId)
                return;

            if (player.QuestManager.GetQuestState(Q5596OrdnanceRecoveryQuest) != QuestState.Accepted)
                return;

            player.QuestManager.ObjectiveUpdate(Q5596CrashSiteObjective, 1u);
        }
    }
}
