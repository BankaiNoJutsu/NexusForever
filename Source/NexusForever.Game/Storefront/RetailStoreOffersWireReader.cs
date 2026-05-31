using NexusForever.Game.Static.Storefront;
using NexusForever.Network;

namespace NexusForever.Game.Storefront
{
    /// <summary>
    /// Retail-aligned reader for <c>ServerStoreOffers</c> bodies
    /// (<c>ServerStoreOffers_ReadPayload</c> @ <c>1400a1400</c>).
    /// Offer prices and category arrays use <c>FUN_140337160</c> composite blocks
    /// per <c>ServerStoreOffers_Offer_ReadPayload</c> @ <c>1400a0dc0</c> and
    /// <c>ServerStoreOffers_OfferGroup_ReadPayload</c> @ <c>1400a0fa0</c>.
    /// </summary>
    internal static class RetailStoreOffersWireReader
    {
        public static bool TryRead(GamePacketReader reader, out string failure, out uint groupId, out uint offerId)
        {
            groupId = 0u;
            offerId = 0u;

            try
            {
                uint groupCount = reader.ReadUInt();
                for (uint groupIndex = 0u; groupIndex < groupCount; groupIndex++)
                {
                    if (!TryReadOfferGroup(reader, out failure, out groupId, out offerId))
                        return false;
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

        private static bool TryReadOfferGroup(GamePacketReader reader, out string failure, out uint groupId, out uint offerId)
        {
            groupId = reader.ReadUInt();
            offerId = 0u;
            reader.ReadUInt();
            reader.ReadWideString();
            reader.ReadWideString();
            reader.ReadUInt(14u);

            uint offerCount = reader.ReadUInt();
            for (uint offerIndex = 0u; offerIndex < offerCount; offerIndex++)
            {
                if (!TryReadOffer(reader, out failure, out offerId))
                {
                    failure = $"group={groupId} offerIndex={offerIndex} {failure}";
                    return false;
                }
            }

            uint categoryCount = reader.ReadUInt();
            if (categoryCount != 0)
            {
                reader.ReadRetailCompositeUInt32Array((int)categoryCount);
                reader.ReadRetailCompositeUInt32Array((int)categoryCount);
            }

            failure = null;
            return true;
        }

        private static bool TryReadOffer(GamePacketReader reader, out string failure, out uint offerId)
        {
            offerId = reader.ReadUInt();
            reader.ReadWideString();
            reader.ReadWideString();
            reader.ReadRetailCompositeByteSpan(8);
            reader.ReadUInt();
            reader.ReadLong();
            reader.ReadByte();

            uint currencyCount = reader.ReadUInt();
            for (uint i = 0u; i < currencyCount; i++)
            {
                if (!TryReadCurrencyRow(reader, out failure))
                {
                    failure = $"offer={offerId} currencyIndex={i} {failure}";
                    return false;
                }
            }

            uint itemCount = reader.ReadUInt();
            for (uint i = 0u; i < itemCount; i++)
            {
                if (!TryReadItemRow(reader, out failure))
                {
                    failure = $"offer={offerId} itemIndex={i} {failure}";
                    return false;
                }
            }

            failure = null;
            return true;
        }

        private static bool TryReadCurrencyRow(GamePacketReader reader, out string failure)
        {
            reader.ReadUInt(5u);
            reader.ReadSingle();
            reader.ReadEnum<DiscountType>(2u);
            reader.ReadSingle();
            reader.ReadLong();
            reader.ReadLong();
            failure = null;
            return true;
        }

        private static bool TryReadItemRow(GamePacketReader reader, out string failure)
        {
            uint type = reader.ReadUInt();
            switch (type)
            {
                case 0u:
                    reader.ReadUInt(15u);
                    break;
                case 1u:
                    reader.ReadUInt();
                    reader.ReadUInt();
                    break;
                case 2u:
                    reader.ReadUInt();
                    break;
                default:
                    failure = $"unsupported item type={type}";
                    return false;
            }

            reader.ReadUInt();
            failure = null;
            return true;
        }
    }
}
