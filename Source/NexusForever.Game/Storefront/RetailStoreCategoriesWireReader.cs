using NexusForever.Game.Static.Storefront;
using NexusForever.Network;

namespace NexusForever.Game.Storefront
{
    /// <summary>
    /// Retail-aligned reader for <c>ServerStoreCategories</c> bodies
    /// (<c>ServerStoreCategories_ReadPayload</c> @ <c>1400a12d0</c>).
    /// </summary>
    internal static class RetailStoreCategoriesWireReader
    {
        public static bool TryRead(GamePacketReader reader, out string failure)
        {
            try
            {
                uint categoryCount = reader.ReadUInt();
                for (uint i = 0u; i < categoryCount; i++)
                {
                    reader.ReadWideString();
                    reader.ReadWideString();
                    reader.ReadUInt();
                    reader.ReadUInt();
                    reader.ReadUInt();
                    reader.ReadBit();
                }

                reader.ReadEnum<RealCurrency>(3u);

                uint packageCount = reader.ReadUInt();
                for (uint i = 0u; i < packageCount; i++)
                {
                    if (!TryReadCurrencyPackageRow(reader, out failure))
                    {
                        failure = $"currencyPackageIndex={i} {failure}";
                        return false;
                    }
                }

                if (reader.BytesRemaining != 0)
                {
                    failure = $"trailing bytes={reader.BytesRemaining}";
                    return false;
                }

                failure = null;
                return true;
            }
            catch (Exception ex)
            {
                failure = ex.Message;
                return false;
            }
        }

        private static bool TryReadCurrencyPackageRow(GamePacketReader reader, out string failure)
        {
            reader.ReadUInt();
            reader.ReadWideString();
            reader.ReadUInt();
            reader.ReadSingle();
            reader.ReadUInt();
            failure = null;
            return true;
        }
    }
}
