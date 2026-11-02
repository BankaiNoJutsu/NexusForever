using System;

namespace NexusForever.Database.Character.Model
{
    // Immutable outcome witness. No cascading FK: a later listing/Character
    // removal must not erase evidence needed to resolve a lost acknowledgement.
    public sealed class MarketplaceBidReceiptModel
    {
        public string Id { get; set; }
        public string Payload { get; set; }
        public DateTime CreatedUtc { get; set; }
    }
}
