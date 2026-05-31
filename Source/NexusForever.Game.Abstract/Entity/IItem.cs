using System.Collections.Generic;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Game.Static.Entity;
using NexusForever.GameTable.Model;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Shared;

namespace NexusForever.Game.Abstract.Entity
{
    public interface IItem : IDatabaseCharacter, IDatabaseState, INetworkBuildable<Item>
    {
        uint Id { get; }
        IItemInfo Info { get; }
        Spell4BaseEntry SpellEntry { get; }
        ulong Guid { get; }
        ulong? CharacterId { get; set; }
        InventoryLocation Location { get; set; }
        InventoryLocation PreviousLocation { get; set; }
        uint BagIndex { get; set; }
        uint PreviousBagIndex { get; set; }
        uint StackCount { get; set; }
        uint Charges { get; set; }
        float Durability { get; set; }
        uint ExpirationTimeLeft { get; set; }
        bool Soulbound { get; }

        /// <summary>
        /// Socketed microchip item ids (client item-eval +0x114 bitmask is derived from socket types 7-0xd).
        /// </summary>
        IList<uint> MicrochipIds { get; }

        /// <summary>
        /// Permanently bind this item to its current owner.
        /// </summary>
        void MakeSoulbound();

        // <summary>
        /// Returns the <see cref="CurrencyType"/> this <see cref="IItem"/> sells for at a vendor.
        /// </summary>
        CurrencyType GetVendorSellCurrency(byte index);

        /// <summary>
        /// Returns the amount of <see cref="CurrencyType"/> this <see cref="IItem"/> sells for at a vendor.
        /// </summary>
        uint GetVendorSellAmount(byte index);
    }
}
