using System;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Shared;

namespace NexusForever.Network.World.Message.Model.Spell
{
    [Message(GameMessageOpcode.ClientCastGuildBossToken)]
    public class ClientCastGuildBossToken : IReadable
    {
        public Identity GuildIdentity { get; private set; } = new Identity();
        public uint Item2Id { get; private set; } // item2Id of the guild boss token item being used
        public uint ContextToken { get; private set; }

        [Obsolete("Use ContextToken. Native packet proof shows this field carries the generated cast-context token.")]
        public uint ClientSpellCastUniqueId => ContextToken;

        public void Read(GamePacketReader reader)
        {
            GuildIdentity.Read(reader);
            Item2Id = reader.ReadUInt();
            ContextToken = reader.ReadUInt();
        }
    }
}
