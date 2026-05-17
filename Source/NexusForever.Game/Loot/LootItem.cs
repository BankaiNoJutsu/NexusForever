using NexusForever.Database.World.Model;
using NexusForever.Game.Static.Loot;

namespace NexusForever.Game.Loot
{
    public class LootItem
    {
        public LootItemType Type { get; }
        public uint StaticId { get; }

        private readonly float probability;
        private readonly uint minCount;
        private readonly uint maxCount;

        public LootItem(LootItemModel model)
        {
            Type        = (LootItemType)model.Type;
            StaticId    = model.StaticId;
            probability = model.Probability;
            minCount    = model.MinCount;
            maxCount    = Math.Max(model.MinCount, model.MaxCount);
        }

        public LootItem(uint staticId, LootItemType type, float probability, uint minCount, uint maxCount)
        {
            StaticId         = staticId;
            Type             = type;
            this.probability = Math.Clamp(probability, 0f, 100f);
            this.minCount    = minCount;
            this.maxCount    = Math.Max(minCount, maxCount);
        }

        public bool TryGetDrop(out uint count)
        {
            count = 0u;

            double chance = Random.Shared.NextDouble() * 100d;
            if (chance >= probability)
                return false;

            uint minimum = Math.Max(1u, minCount);
            uint maximum = Math.Max(minimum, maxCount);
            count = minimum == maximum ? minimum : (uint)Random.Shared.Next((int)minimum, (int)maximum + 1);
            return true;
        }
    }
}
