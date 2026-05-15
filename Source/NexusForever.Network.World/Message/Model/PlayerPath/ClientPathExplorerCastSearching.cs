using System;
using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.PlayerPath
{
    [Message(GameMessageOpcode.ClientCastPathExplorerSearching)]
    public class ClientPathExplorerCastSearching : IReadable
    {
        public uint ContextToken { get; private set; }

        [Obsolete("Use ContextToken. Native packet proof shows this field carries the generated cast-context token.")]
        public uint ClientSpellCastUniqueID => ContextToken;
        public byte SearchRadiusBand { get; private set; } // casts 1 of 4 Searching spells depending on band
        public ushort PathExplorerScavengerClueId {  get; private set; } // Relates to nearest ScavengerHunt WorldLocation

        public void Read(GamePacketReader reader)
        {
            ContextToken = reader.ReadUInt();
            SearchRadiusBand = reader.ReadByte(2);
            PathExplorerScavengerClueId = reader.ReadUShort(14);
        }
    }
}
