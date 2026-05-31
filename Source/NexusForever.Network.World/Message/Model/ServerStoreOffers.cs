using NexusForever.Game.Static.Storefront;
using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ServerStoreOffers)]
    public class ServerStoreOffers : IWritable
    {
        public class OfferGroup : IWritable
        {
            public class Offer : IWritable
            {
                public class OfferCurrencyData : IWritable
                {
                    public byte CurrencyId { get; set; } // 5
                    public float Price { get; set; }
                    public DiscountType DiscountType { get; set; } // 2
                    public float DiscountValue { get; set; }
                    public long DiscountTimeRemaining { get; set; }
                    public long TimeSinceExpiry { get; set; }

                    public void Write(GamePacketWriter writer)
                    {
                        writer.Write(CurrencyId, 5u);
                        writer.Write(Price);
                        writer.Write(DiscountType, 2u);
                        writer.Write(DiscountValue);
                        writer.Write(DiscountTimeRemaining);
                        writer.Write(TimeSinceExpiry);
                    }
                }

                public class OfferItemData : IWritable
                {
                    public uint Type { get; set; } // Should be 0
                    public ushort AccountItemId { get; set; } // 15
                    public uint Amount { get; set; }

                    public uint Type1AccountItemId { get; set; }
                    public uint Type1Amount { get; set; }

                    public uint Type2AccountItemId { get; set; }

                    public void Write(GamePacketWriter writer)
                    {
                        writer.Write(Type);
                        switch (Type)
                        {
                            case 0:
                                writer.Write(AccountItemId, 15u);
                                break;
                            case 1:
                                writer.Write(Type1AccountItemId);
                                writer.Write(Type1Amount);
                                break;
                            case 2:
                                writer.Write(Type2AccountItemId);
                                break;
                        }

                        writer.Write(Amount);
                    }
                }

                public uint Id { get; set; }
                public string Name { get; set; }
                public string Description { get; set; }
                public float PricePremium { get; set; }
                public float PriceAlternative { get; set; }
                public DisplayFlag DisplayFlags { get; set; }
                /// <summary>
                /// Retail <c>store_offer_item.field_6</c> wire scalar at offer <c>+0x28</c>
                /// (<see cref="RetailStoreOfferWireConstants.CatalogWireScalarBits"/>). Parsed by the client
                /// but not copied in <c>Storefront_ApplyServerStoreOffers</c> @ <c>14044b750</c>.
                /// </summary>
                public long RetailCatalogWireScalar { get; set; }
                /// <summary>
                /// Retail <c>store_offer_item.field_7</c> at offer <c>+0x30</c> (usually <c>0</c>).
                /// <c>ServerStoreOffers_Offer_ReadPayload</c> @ <c>1400a0dc0</c> reads this via
                /// <c>FUN_14006be30</c> (one byte / 8 bits).
                /// </summary>
                public byte RetailCatalogWireByte { get; set; }
                public List<OfferCurrencyData> CurrencyData { get; set; } = new();
                public List<OfferItemData> ItemData { get; set; } = new();

                public void Write(GamePacketWriter writer)
                {
                    writer.Write(Id);
                    writer.WriteStringWide(Name);
                    writer.WriteStringWide(Description);

                    Span<byte> priceBytes = stackalloc byte[8];
                    BitConverter.TryWriteBytes(priceBytes, BitConverter.SingleToUInt32Bits(PricePremium));
                    BitConverter.TryWriteBytes(priceBytes[4..], BitConverter.SingleToUInt32Bits(PriceAlternative));
                    writer.WriteRetailCompositeByteSpan(priceBytes);

                    writer.Write(DisplayFlags, 32u);
                    writer.Write(RetailCatalogWireScalar);
                    writer.Write(RetailCatalogWireByte);

                    writer.Write(CurrencyData.Count);
                    CurrencyData.ForEach(e => e.Write(writer));

                    writer.Write(ItemData.Count);
                    ItemData.ForEach(e => e.Write(writer));
                }
            }

            public struct Category
            {
                public uint Id { get; set; }
                public uint Index { get; set; }
            }

            public uint Id { get; set; }
            public DisplayFlag DisplayFlags { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public ushort DisplayInfoOverride { get; set; }
            public List<Offer> Offers { get; set; } = new();
            public List<Category> Categories { get; set; } = new();

            public void Write(GamePacketWriter writer)
            {
                writer.Write(Id);
                writer.Write(DisplayFlags, 32u);
                writer.WriteStringWide(Name);
                writer.WriteStringWide(Description);
                writer.Write(DisplayInfoOverride, 14u);

                writer.Write(Offers.Count);
                Offers.ForEach(e => e.Write(writer));

                writer.Write(Categories.Count);
                if (Categories.Count != 0)
                {
                    var categoryIds = new uint[Categories.Count];
                    var categoryIndices = new uint[Categories.Count];
                    for (int i = 0; i < Categories.Count; i++)
                    {
                        categoryIds[i]     = Categories[i].Id;
                        categoryIndices[i] = Categories[i].Index;
                    }

                    writer.WriteRetailCompositeUInt32Array(categoryIds);
                    writer.WriteRetailCompositeUInt32Array(categoryIndices);
                }
            }
        }

        public List<OfferGroup> OfferGroups { get; set; } = new();

        public void Write(GamePacketWriter writer)
        {
            writer.Write(OfferGroups.Count);
            OfferGroups.ForEach(e => e.Write(writer));
        }
    }
}
