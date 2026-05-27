using NexusForever.Game.Abstract.Cinematic;
using NexusForever.Game.Abstract.Cinematic.Cinematics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.LevianBay
{
    [ScriptFilterOwnerId(1387)]
    public class LevianBayMapScript : IMapScript, IOwnedScript<IBaseMap>
    {
        private const ushort Q6780LightingTheWayQuest = 6780;

        private readonly ICinematicFactory cinematicFactory;

        public LevianBayMapScript(ICinematicFactory cinematicFactory)
        {
            this.cinematicFactory = cinematicFactory;
        }

        public void OnLoad(IBaseMap owner)
        {
        }

        public void OnAddToMap(IGridEntity entity)
        {
            if (entity is not IPlayer player)
                return;

            if (player.QuestManager.GetQuestState(Q6780LightingTheWayQuest) != null)
                return;

            player.CinematicManager.QueueCinematic(cinematicFactory.CreateCinematic<ILevianBayOnCreate>());
        }
    }
}
