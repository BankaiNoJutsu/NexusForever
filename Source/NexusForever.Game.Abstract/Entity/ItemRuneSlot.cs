using NexusForever.Game.Static.Crafting;

namespace NexusForever.Game.Abstract.Entity
{
    public sealed class ItemRuneSlot
    {
        public RuneType Type { get; set; }
        public uint RuneItem2Id { get; set; }

        public ItemRuneSlot()
        {
        }

        public ItemRuneSlot(RuneType type, uint runeItem2Id = 0u)
        {
            Type        = type;
            RuneItem2Id = runeItem2Id;
        }
    }
}
