using System;
using System.Collections.Generic;
using System.Numerics;
using NexusForever.Shared.GameTable.Model;
using NexusForever.WorldServer.Game.Entity;

namespace NexusForever.WorldServer.Game.Map
{
    public sealed class InstancedMap<T> : IInstancedMap where T : IMapInstance, new()
    {
        private WorldEntry entry;
        private readonly Dictionary</*instanceId*/ uint, T> instances = new();
        private readonly Queue<T> pendingInstances = new();
        private readonly QueuedCounter instanceCounter = new();

        public InstancedMap(MapInfo info)
        {
            entry = info.Entry;
        }
        
        public void Initialise()
        {
        }

        public void EnqueueAdd(GridEntity entity, Vector3 position)
        {
            // entities are never directly added to an instanced map, only its children
            throw new InvalidOperationException();
        }

        public void Update(double lastTick)
        {
            while (pendingInstances.TryDequeue(out T instance))
                instances.Add(instanceCounter.Dequeue(), instance);

            foreach (T map in instances.Values)
                map.Update(lastTick);
        }

        /// <summary>
        /// Create or find child <see cref="T"/> for <see cref="Player"/>.
        /// </summary>
        public IMap CreateInstance(MapInfo info, Player player)
        {
            if (info.InstanceId != 0u)
                return GetInstance(info.InstanceId);
            if (info.ResidenceId != 0ul)
            {
                T instance = GetInstance(i => (i as ResidenceMap)?.Id == info.ResidenceId);
                if (instance != null)
                    return instance;
            }

            var newInstance = new T();
            newInstance.CreateInstance(info, player);
            pendingInstances.Enqueue(newInstance);
            return newInstance;
        }

        private T GetInstance(uint instanceId)
        {
            return instances.TryGetValue(instanceId, out T map) ? map : default;
        }

        private T GetInstance(Func<T, bool> func)
        {
            foreach ((uint instanceId, T map) in instances)
                if (func.Invoke(map))
                    return map;

            return default;
        }
    }
}
