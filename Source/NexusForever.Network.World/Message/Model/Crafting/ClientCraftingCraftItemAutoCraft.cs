using System;
using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.Crafting
{
    [Message(GameMessageOpcode.ClientCraftingCraftItemAutoCraft)]
    public class ClientCraftingCraftItemAutoCraft : IReadable
    {
        public uint ContextToken { get; private set; } // creates a spell cast when the craft is Runecrafting

        [Obsolete("Use ContextToken. Native packet proof shows this field carries the generated cast-context token.")]
        public uint ClientSpellcastUniqueId => ContextToken;
        public uint CraftingStationUnitId { get; private set; }
        public uint TradeskillSchematic2Id { get; private set; }
        public uint SchematicCount { get; private set; }

        public void Read(GamePacketReader reader)
        {
            ContextToken = reader.ReadUInt();
            CraftingStationUnitId = reader.ReadUInt();
            TradeskillSchematic2Id = reader.ReadUInt();
            SchematicCount = reader.ReadUInt();
        }
    }
}
