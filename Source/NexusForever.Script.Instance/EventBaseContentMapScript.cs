using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;

namespace NexusForever.Script.Instance
{
    public abstract class EventBaseContentMapScript : IContentMapScript, IOwnedScript<IContentMapInstance>
    {
        public abstract uint PublicEventId { get; }
        protected virtual IEnumerable<uint> AdditionalPublicEventIds => [];

        protected IContentMapInstance map;
        protected IPublicEvent publicEvent;
        private readonly List<IPublicEvent> additionalPublicEvents = [];

        /// <summary>
        /// Invoked when <see cref="IContentMapInstance"/> is loaded.
        /// </summary>
        public void OnLoad(IContentMapInstance owner)
        {
            map         = owner;
            publicEvent = map.PublicEventManager.CreateEvent(PublicEventId);
            var createdEventIds = new HashSet<uint> { PublicEventId };

            foreach (uint additionalPublicEventId in AdditionalPublicEventIds)
                CreateAdditionalPublicEvent(additionalPublicEventId, createdEventIds);

            CreateChildPublicEvents(publicEvent, createdEventIds);
        }

        private void CreateChildPublicEvents(IPublicEvent owner, ISet<uint> createdEventIds)
        {
            if (owner?.ChildEventIds == null)
                return;

            foreach (uint childEventId in owner.ChildEventIds)
                CreateAdditionalPublicEvent(childEventId, createdEventIds);
        }

        private IPublicEvent CreateAdditionalPublicEvent(uint publicEventId, ISet<uint> createdEventIds)
        {
            if (publicEventId == 0u || !createdEventIds.Add(publicEventId))
                return null;

            IPublicEvent additionalPublicEvent = map.PublicEventManager.GetEvent(publicEventId)
                ?? map.PublicEventManager.CreateEvent(publicEventId);
            if (additionalPublicEvent == null)
                return null;

            additionalPublicEvents.Add(additionalPublicEvent);
            CreateChildPublicEvents(additionalPublicEvent, createdEventIds);
            return additionalPublicEvent;
        }

        /// <summary>
        /// Invoked when <see cref="IGridEntity"/> is added to map.
        /// </summary>
        public virtual void OnAddToMap(IGridEntity entity)
        {
            if (entity is not IPlayer player)
                return;

            publicEvent?.JoinEvent(player, PublicEventTeam.PublicTeam);
            foreach (IPublicEvent additionalPublicEvent in additionalPublicEvents)
                additionalPublicEvent.JoinEvent(player, PublicEventTeam.PublicTeam);
        }

        /// <summary>
        /// Invoked when <see cref="IPublicEvent"/> finishes with the winning <see cref="IPublicEventTeam"/>.
        /// </summary>
        public void OnPublicEventFinish(IPublicEvent publicEvent, IPublicEventTeam publicEventTeam)
        {
            if (this.publicEvent != publicEvent)
                return;

            map.Match?.MatchFinish();
        }

        /// <summary>
        /// Invoked when the <see cref="IMatch"/> for the map finishes.
        /// </summary>
        public void OnMatchFinish()
        {
            publicEvent?.Finish(PublicEventTeam.PublicTeam);
            foreach (IPublicEvent additionalPublicEvent in additionalPublicEvents)
                additionalPublicEvent.Finish(PublicEventTeam.PublicTeam);
        }
    }
}
