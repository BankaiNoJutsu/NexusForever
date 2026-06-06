using System.Numerics;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Creature;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.Matching.Match;
using NexusForever.Game.Map;
using NexusForever.Script;
using NexusForever.Script.Template;

namespace NexusForever.Game.Map.Instance
{
    public class ContentMapInstance : MapInstance, IContentMapInstance
    {
        /// <summary>
        /// Active <see cref="IMatch"/> for map.
        /// </summary>
        public IMatch Match { get; private set; }

        private readonly Dictionary<Identity, IMapPosition> playerReturnLocations = [];

        public override float? VisionRange { get; protected set; } = null;

        #region Dependency Injection

        protected readonly IScriptManager scriptManager;

        public ContentMapInstance(
            IEntityFactory entityFactory,
            IPublicEventManager publicEventManager,
            IScriptManager scriptManager,
            ICreatureInfoManager creatureInfoManager = null)
            : base(entityFactory, publicEventManager, creatureInfoManager)
        {
            this.scriptManager = scriptManager;
        }

        #endregion

        protected override void InitialiseScriptCollection()
        {
            scriptCollection = scriptManager.InitialiseOwnedScripts<IContentMapInstance>(this, Entry.Id);
        }

        protected override void OnUnload()
        {
            // the match and public events should already be cleaned up before unload
            // this is just a safety check for manual unloads
            if (Match != null)
            {
                Match.MatchFinish();
                // need to call cleanup right away since matches usually have a delay before they are cleaned up
                Match.MatchCleanup();
                Match = null;
            }

            PublicEventManager.Cleanup();
        }

        protected override IMapPosition GetPlayerReturnLocation(IPlayer player)
        {
            if (Match != null)
            {
                IMapPosition mapPosition = Match.GetReturnPosition(player);
                if (mapPosition != null)
                    return mapPosition;
            }

            if (playerReturnLocations.Remove(player.Identity, out IMapPosition returnLocation))
                return returnLocation;

            return null;
        }

        protected override void AddEntity(IGridEntity entity, Vector3 vector)
        {
            if (entity is IPlayer contentPlayer
                && Match == null
                && contentPlayer.PreviousMap?.Entry != null
                && contentPlayer.PreviousMap.Entry.Id != Entry.Id)
            {
                playerReturnLocations[contentPlayer.Identity] = new MapPosition
                {
                    Info     = contentPlayer.PreviousMap,
                    Position = contentPlayer.Position
                };
            }

            base.AddEntity(entity, vector);

            if (entity is IPlayer player)
                Match?.MatchEnter(player);
        }

        protected override void RemoveEntity(IGridEntity entity)
        {
            base.RemoveEntity(entity);

            if (entity is IPlayer player)
                Match?.MatchExit(player, false);
        }

        /// <summary>
        /// Add <see cref="IMatch"/> for map.
        /// </summary>
        public void SetMatch(IMatch match)
        {
            if (Match != null)
                throw new InvalidOperationException();

            Match = match;
            Match.SetMap(this);
        }

        /// <summary>
        /// Remove <see cref="IMatch"/> for map.
        /// </summary>
        public void RemoveMatch()
        {
            if (Match == null)
                throw new InvalidOperationException();

            Match = null;
        }

        /// <summary>
        /// Invoked when the <see cref="IMatch"/> for the map finishes.
        /// </summary>
        public void OnMatchFinish()
        {
            scriptCollection.Invoke<IContentMapScript>(s => s.OnMatchFinish());
        }
    }
}
