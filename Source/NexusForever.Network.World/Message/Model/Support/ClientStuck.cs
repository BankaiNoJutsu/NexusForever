using System;
using NexusForever.Game.Static.Support;
using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.Support
{
    // Initiates a spell cast to unstick the player
    [Message(GameMessageOpcode.ClientStuck)]
    public class ClientStuck : IReadable
    {
        public UnstickType UnstickingType { get; private set; }
        public uint ContextToken { get; private set; }

        [Obsolete("Use ContextToken. Native packet proof shows this field carries the generated cast-context token.")]
        public uint ClientSpellCastUniqueId => ContextToken;

        public void Read(GamePacketReader reader)
        {
            UnstickingType = reader.ReadEnum<UnstickType>(3u);
            ContextToken = reader.ReadUInt();
        }
    }
}
