using System.Collections;
using NexusForever.Game.Abstract.Entity;

namespace NexusForever.Game.Entity
{
    public class BuybackInfo : IBuybackInfo
    {
        private readonly Dictionary<uint /*UniqueId*/, IBuybackItem> buybackItems = new();

        /// <summary>
        /// Return stored <see cref="IBuybackItem"/> with supplied unique id.
        /// </summary>
        public IBuybackItem GetItem(uint uniqueId)
        {
            return buybackItems.TryGetValue(uniqueId, out IBuybackItem buybackItem) ? buybackItem : null;
        }

        /// <summary>
        /// Store an <see cref="IBuybackItem"/> with its assigned request identity.
        /// </summary>
        public void AddItem(IBuybackItem item) => buybackItems.Add(item.UniqueId, item);

        /// <summary>
        /// Remove stored <see cref="IBuybackItem"/> with supplied unique id.
        /// </summary>
        public void RemoveItem(uint uniqueId)
        {
            buybackItems.Remove(uniqueId);
        }

        public IEnumerator<IBuybackItem> GetEnumerator()
        {
            return buybackItems.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
