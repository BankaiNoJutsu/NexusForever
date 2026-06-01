using System.Globalization;

namespace NexusForever.Game.Entity
{
    internal static class ItemMicrochipIdsCodec
    {
        public static string Serialize(IList<uint> microchipIds)
        {
            if (microchipIds == null || microchipIds.Count == 0)
                return string.Empty;

            return string.Join(',', microchipIds);
        }

        public static void DeserializeInto(string value, ICollection<uint> target)
        {
            target.Clear();
            if (string.IsNullOrWhiteSpace(value))
                return;

            foreach (string part in value.Split(',', StringSplitOptions.RemoveEmptyEntries))
                target.Add(uint.Parse(part, CultureInfo.InvariantCulture));
        }
    }
}
