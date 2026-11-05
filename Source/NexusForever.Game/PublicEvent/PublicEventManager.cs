using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Network.Session;
using NexusForever.Script.Template;
using NexusForever.Shared;

namespace NexusForever.Game.PublicEvent
{
    public class PublicEventManager : IPublicEventManager
    {
        private IBaseMap map;
        private bool cleanupStarted;
        private readonly Dictionary<uint, IPublicEvent> publicEvents = [];

        private readonly Dictionary<ulong, MapCharacter> characters = [];
        private sealed record MapCharacter(IPlayer Owner, IGameSession Session, IPublicEventCharacter Events);

        #region Dependency Injection

        private readonly ILogger<PublicEventManager> log;

        private readonly IPublicEventTemplateManager publicEventTemplateManager;
        private readonly IPublicEventFactory publicEventFactory;
        private readonly IFactory<IPublicEventCharacter> publicEventCharacterFactory;

        public PublicEventManager(
            ILogger<PublicEventManager> log,
            IPublicEventTemplateManager publicEventTemplateManager,
            IPublicEventFactory publicEventFactory,
            IFactory<IPublicEventCharacter> publicEventCharacterFactory)
        {
            this.log                         = log;

            this.publicEventTemplateManager  = publicEventTemplateManager;
            this.publicEventFactory          = publicEventFactory;
            this.publicEventCharacterFactory = publicEventCharacterFactory;
        }

        #endregion

        /// <summary>
        /// Initialise <see cref="PublicEventManager"/> with suppled <see cref="IBaseMap"/> owner.
        /// </summary>
        public void Initialise(IBaseMap map)
        {
            if (this.map != null)
                throw new InvalidOperationException();

            this.map = map;

            log.LogTrace($"Initialised public event manager for map {map.Entry.Id}.");
        }

        /// <summary>
        /// Force cleanup of all <see cref="IPublicEvent"/> and <see cref="IPublicEventCharacter"/>.
        /// </summary>
        /// <remarks>
        /// This should really only be called when the map is being unloaded.
        /// </remarks>
        public void Cleanup()
        {
            if (cleanupStarted)
                return;

            cleanupStarted = true;

            foreach (IPublicEvent @event in publicEvents.Values)
                CleanupEvent(@event);

            publicEvents.Clear();
            characters.Clear();
        }

        private void CleanupEvent(IPublicEvent @event)
        {
            try
            {
                @event.Finish(null);
            }
            catch (Exception exception)
            {
                log.LogError(exception, "Failed to finish public event {EventId} during unload of map {MapId}.", @event.Id, map.Entry.Id);
            }

            try
            {
                @event.Dispose();
            }
            catch (Exception exception)
            {
                log.LogError(exception, "Failed to dispose public event {EventId} during unload of map {MapId}.", @event.Id, map.Entry.Id);
            }
        }

        /// <summary>
        /// Invoked each world tick with the delta since the previous tick occurred.
        /// </summary>
        public void Update(double lastTick)
        {
            if (cleanupStarted || publicEvents.Count == 0)
                return;

            var toRemove = new List<IPublicEvent>();

            foreach (IPublicEvent @event in publicEvents.Values)
            {
                @event.Update(lastTick);
                if (cleanupStarted)
                    return;

                if (@event.IsFinalised)
                    toRemove.Add(@event);
            }

            foreach (IPublicEvent @event in toRemove)
            {
                if (cleanupStarted)
                    return;

                publicEvents.Remove(@event.Id);
                @event.Dispose();

                log.LogTrace($"Removed public event {@event.Id} for map {map.Entry.Id} from store.");
            }
        }

        /// <summary>
        /// Create a new <see cref="IPublicEvent"/> with supplied id.
        /// </summary>
        public IPublicEvent CreateEvent(uint id)
        {
            if (publicEvents.TryGetValue(id, out IPublicEvent existingEvent))
                return existingEvent;

            if (cleanupStarted)
                return null;

            IPublicEventTemplate template = publicEventTemplateManager.GetTemplate(id);
            if (template == null)
                return null;

            if (template.Entry.WorldId != map.Entry.Id)
                throw new InvalidOperationException($"Public event {id} is not available in map {map.Entry.Id}!");

            IPublicEvent @event = publicEventFactory.CreateEvent(id);
            @event.Initialise(this, template, map);

            // Initialisation scripts can unload the map before this event is registered.
            if (cleanupStarted)
            {
                CleanupEvent(@event);
                return null;
            }

            log.LogTrace($"Created new public event {id} for map {map.Entry.Id}.");

            publicEvents.Add(id, @event);
            return @event;
        }

        /// <summary>
        /// Return the <see cref="IPublicEvent"/> from owner <see cref="IBaseMap"/> with the supplied id.
        /// </summary>
        public IPublicEvent GetEvent(uint id)
        {
            return publicEvents.TryGetValue(id, out IPublicEvent @event) ? @event : null;
        }

        /// <summary>
        /// Return a collection of all <see cref="IPublicEvent"/>'s on the owner <see cref="IBaseMap"/>.
        /// </summary>
        public IEnumerable<IPublicEvent> GetEvents()
        {
            return publicEvents.Values;
        }

        /// <summary>
        /// Add character to <see cref="IPublicEvent"/>.
        /// </summary>
        public void AddEvent(ulong characterId, IPublicEvent publicEvent)
        {
            if (characters.TryGetValue(characterId, out MapCharacter character))
                character.Events.AddEvent(publicEvent);
        }

        /// <summary>
        /// Remove character from <see cref="IPublicEvent"/>.
        /// </summary>
        public void RemoveEvent(ulong characterId, IPublicEvent publicEvent)
        {
            if (characters.TryGetValue(characterId, out MapCharacter character))
                character.Events.RemoveEvent(publicEvent);
        }

        /// <summary>
        /// Invoked when a <see cref="IGridEntity"/> is added to owner <see cref="IBaseMap"/>.
        /// </summary>
        public void OnAddToMap(IGridEntity gridEntity)
        {
            if (cleanupStarted)
                return;

            if (gridEntity is IPlayer player)
            {
                IPublicEventCharacter character = publicEventCharacterFactory.Resolve();
                character.Initialise(player);

                if (!characters.TryAdd(player.CharacterId, new MapCharacter(player, player.Session, character)))
                    return;

                log.LogTrace($"Public event information for character {player.CharacterId} added to map {map.Entry.Id} store.");
            }

            InvokeScriptCollection<IPublicEventScript>(s => s.OnAddToMap(gridEntity));
        }

        /// <summary>
        /// Invoked when a <see cref="IGridEntity"/> is removed from owner <see cref="IBaseMap"/>.
        /// </summary>
        public void OnRemoveFromMap(IGridEntity gridEntity)
        {
            if (gridEntity is not IPlayer player)
            {
                InvokeScriptCollection<IPublicEventScript>(s => s.OnRemoveFromMap(gridEntity));
                return;
            }

            if (!characters.TryGetValue(player.CharacterId, out MapCharacter character)
                || !ReferenceEquals(character.Owner, player))
                return;

            InvokeScriptCollection<IPublicEventScript>(s => s.OnRemoveFromMap(gridEntity));
            character.Events.OnRemoveFromMap(player);

            characters.Remove(player.CharacterId);

            log.LogTrace($"Public event information for character {player.CharacterId} removed from map {map.Entry.Id} store.");
        }

        private IPublicEventCharacter GetCurrentCharacter(IPlayer player)
        {
            if (player == null || !ReferenceEquals(player.Map, map)
                || !characters.TryGetValue(player.CharacterId, out MapCharacter character)
                || !ReferenceEquals(character.Owner, player)
                || !ReferenceEquals(character.Session, player.Session)
                || character.Session is null or NetworkSession { IsDisconnecting: true }
                || (character.Session is IPlayerSession bound && !ReferenceEquals(bound.Player, player)))
                return null;

            return character.Events;
        }

        /// <summary>
        /// Invoked when a <see cref="IUnitEntity"/> dies on owner <see cref="IBaseMap"/>.
        /// </summary>
        public void OnDeath(IUnitEntity entity)
        {
            InvokeScriptCollection<IPublicEventScript>(s => s.OnDeath(entity));
        }

        /// <summary>
        /// Invoked when a <see cref="IPlayer"/> resurrects on owner <see cref="IBaseMap"/>.
        /// </summary>
        public void OnResurrection(IPlayer player)
        {
            InvokeScriptCollection<IPublicEventScript>(s => s.OnResurrection(player));
        }

        /// <summary>
        /// Update any objective for any public event <see cref="IPlayer"/> is part of that meets the supplied <see cref="PublicEventObjectiveType"/>, objectId and count.
        /// </summary>
        public void UpdateObjective(IPlayer player, PublicEventObjectiveType type, uint objectId, int count)
        {
            GetCurrentCharacter(player)?.UpdateObjective(player, type, objectId, count);
        }

        /// <summary>
        /// Update any objective for any public event that meets the supplied <see cref="PublicEventObjectiveType"/>, objectId and count.
        /// </summary>
        public void UpdateObjective(PublicEventObjectiveType type, uint objectId, int count)
        {
            foreach (IPublicEvent publicEvent in publicEvents.Values)
                publicEvent.UpdateObjective(type, objectId, count);
        }

        /// <summary>
        /// Update a specific objective with the supplied objectiveId and count.
        /// </summary>
        public void UpdateObjective<T>(T objectiveId, int count) where T : Enum
        {
            foreach (IPublicEvent publicEvent in publicEvents.Values)
                publicEvent.UpdateObjective(objectiveId, count);
        }

        /// <summary>
        /// Update a specific objective with the supplied objectiveId and count.
        /// </summary>
        public void UpdateObjective(uint objectiveId, int count)
        {
            foreach (IPublicEvent publicEvent in publicEvents.Values)
                publicEvent.UpdateObjective(objectiveId, count);
        }

        /// <summary>
        /// Update stat for any public event <see cref="IPlayer"/> is part of with the supplied <see cref="PublicEventStat"/> and value.
        /// </summary>
        public void UpdateStat(IPlayer player, PublicEventStat stat, uint value)
        {
            GetCurrentCharacter(player)?.UpdateStat(player, stat, value);
        }

        /// <summary>
        /// Increment stat for any public event <see cref="IPlayer"/> is part of with the supplied <see cref="PublicEventStat"/> and value.
        /// </summary>
        public void IncrementStat(IPlayer player, PublicEventStat stat, uint value)
        {
            GetCurrentCharacter(player)?.IncrementStat(player, stat, value);
        }

        /// <summary>
        /// Update custom stat for any public event <see cref="IPlayer"/> is part of with the supplied index and value.
        /// </summary>
        public void UpdateCustomStat(IPlayer player, uint index, uint value)
        {
            GetCurrentCharacter(player)?.UpdateCustomStat(player, index, value);
        }

        /// <summary>
        /// Invoked when a cinematic finishes for <see cref="IPlayer"/>.
        /// </summary>
        public void OnCinematicFinish(IPlayer player, uint cinematicId)
        {
            GetCurrentCharacter(player)?.OnCinematicFinish(player, cinematicId);
        }

        /// <summary>
        /// Respond to vote in a specific public event for the <see cref="IPlayer"/> with the supplied choice.
        /// </summary>
        public void RespondVote(IPlayer player, uint eventId, uint voteId, uint teamId, uint choice)
        {
            GetCurrentCharacter(player)?.RespondVote(player, eventId, voteId, teamId, choice);
        }

        private void InvokeScriptCollection<T>(Action<T> action)
        {
            foreach (IPublicEvent @event in publicEvents.Values)
                @event.InvokeScriptCollection(action);
        }
    }
}
