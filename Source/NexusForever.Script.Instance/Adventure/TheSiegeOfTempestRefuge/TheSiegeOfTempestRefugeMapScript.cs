using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Game.Static.Reputation;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Adventure.TheSiegeOfTempestRefuge
{
    /// <summary>
    /// Build 16042 MatchingGameMap 52 and 53 map The Siege of Tempest Refuge
    /// to world 1233 / Map\AdventureGaleras. Public events 173 and 174 are
    /// mirrored Exile and Dominion defense routes.
    /// </summary>
    [ScriptFilterOwnerId(1233)]
    public class TheSiegeOfTempestRefugeMapScript : IContentMapScript, IOwnedScript<IContentMapInstance>
    {
        public const uint ExilePublicEventId = 173u;
        public const uint DominionPublicEventId = 174u;

        private IContentMapInstance map;
        private IPublicEvent exilePublicEvent;
        private IPublicEvent dominionPublicEvent;

        /// <summary>
        /// Invoked when <see cref="IScript"/> is loaded.
        /// </summary>
        public void OnLoad(IContentMapInstance owner)
        {
            map = owner;
            exilePublicEvent = map.PublicEventManager.CreateEvent(ExilePublicEventId);
            dominionPublicEvent = map.PublicEventManager.CreateEvent(DominionPublicEventId);
        }

        /// <summary>
        /// Invoked when <see cref="IGridEntity"/> is added to map.
        /// </summary>
        public void OnAddToMap(IGridEntity entity)
        {
            if (entity is not IPlayer player)
                return;

            GetPublicEventForPlayer(player)?.JoinEvent(player, PublicEventTeam.PublicTeam);
        }

        /// <summary>
        /// Invoked when <see cref="IPublicEvent"/> finishes with the winning <see cref="IPublicEventTeam"/>.
        /// </summary>
        public void OnPublicEventFinish(IPublicEvent publicEvent, IPublicEventTeam publicEventTeam)
        {
            if (publicEvent != exilePublicEvent && publicEvent != dominionPublicEvent)
                return;

            map.Match?.MatchFinish();
        }

        /// <summary>
        /// Invoked when the <see cref="Game.Abstract.Matching.Match.IMatch"/> for the map finishes.
        /// </summary>
        public void OnMatchFinish()
        {
            exilePublicEvent?.Finish(PublicEventTeam.PublicTeam);
            dominionPublicEvent?.Finish(PublicEventTeam.PublicTeam);
        }

        private IPublicEvent GetPublicEventForPlayer(IPlayer player)
        {
            return player.Faction1 == Faction.Dominion
                ? dominionPublicEvent
                : exilePublicEvent;
        }
    }
}
