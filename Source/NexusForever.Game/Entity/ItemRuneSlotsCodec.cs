using System.Globalization;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Crafting;

namespace NexusForever.Game.Entity
{
    internal static class ItemRuneSlotsCodec
    {
        // type:runeItem2Id,type:runeItem2Id (slot order preserved)
        public static string Serialize(IList<ItemRuneSlot> runeSlots)
        {
            if (runeSlots == null || runeSlots.Count == 0)
                return string.Empty;

            return string.Join(',', runeSlots.Select(slot =>
                $"{(uint)slot.Type}:{slot.RuneItem2Id}"));
        }

        public static void DeserializeInto(string value, ICollection<ItemRuneSlot> target)
        {
            target.Clear();
            if (string.IsNullOrWhiteSpace(value))
                return;

            foreach (string part in value.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                string[] fields = part.Split(':', StringSplitOptions.RemoveEmptyEntries);
                if (fields.Length != 2)
                    continue;

                uint typeId = uint.Parse(fields[0], CultureInfo.InvariantCulture);
                uint runeItem2Id = uint.Parse(fields[1], CultureInfo.InvariantCulture);
                if (!Enum.IsDefined(typeof(RuneType), (int)typeId))
                    continue;

                target.Add(new ItemRuneSlot((RuneType)typeId, runeItem2Id));
            }
        }
    }
}
