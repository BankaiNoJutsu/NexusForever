using NexusForever.Game.Abstract.Cinematic;
using NexusForever.Game.Abstract.Cinematic.Cinematics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.EverstarGrove
{
    [ScriptFilterOwnerId(990)]
    public class EverstarGroveMapScript : IMapScript, IOwnedScript<IBaseMap>
    {
        private const ushort Q6296NaturesUprisingQuest = 6296;

        private readonly ICinematicFactory cinematicFactory;

        public EverstarGroveMapScript(ICinematicFactory cinematicFactory)
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

            if (player.QuestManager.GetQuestState(Q6296NaturesUprisingQuest) != null)
                return;

            player.CinematicManager.QueueCinematic(cinematicFactory.CreateCinematic<IEverstarGroveOnCreate>());
        }
    }
}
