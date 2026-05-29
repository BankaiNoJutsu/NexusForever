using NexusForever.Network.Message;
using NexusForever.Network.World.Entity;
using NexusForever.Network.World.Message.Model.Shared;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Opcode <c>0x0469</c>; mapped from <c>ServerGroupPositionUpdate_ReadPayload</c> (<c>140084030</c>).
    /// Native wire shape is four parallel arrays after the shared group/world prefix: identities, raw position triplets, world-zone ids, and flags.
    /// </summary>
    [Message(GameMessageOpcode.ServerGroupPositionUpdate)]
    public class ServerGroupPositionUpdate : IWritable
    {
        public class UnknownStruct0
        {
            public Identity Identity { get; set; }
            public Position Position { get; set; }
            public uint WorldZoneId { get; set; }
            public uint Flags { get; set; } = 0; // bInCombatPvp = 1, bIInCombatPve = 2, InCombat = 3
        }

        public ulong GroupId { get; set; }
        public uint WorldId { get; set; }
        public List<UnknownStruct0> Updates { get; set; } = new List<UnknownStruct0>();

        public void Write(GamePacketWriter writer)
        {
            writer.Write(GroupId);
            writer.Write(WorldId, 15);

            writer.Write((uint)Updates.Count);
            Updates.ForEach(update => update.Identity.Write(writer));
            Updates.ForEach(update => update.Position.Write(writer));
            Updates.ForEach(update => writer.Write(update.WorldZoneId));
            Updates.ForEach(update => writer.Write(update.Flags));
        }
    }
}
