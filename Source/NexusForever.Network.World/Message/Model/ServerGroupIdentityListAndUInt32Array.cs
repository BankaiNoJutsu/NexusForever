using NexusForever.Game.Static.Group;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Shared;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Reader-backed structural payload for opcode <c>0x0438</c>.
    /// Native reader <c>ServerGroupIdentityListAndUInt32Array_ReadPayload</c> (<c>140083990</c>)
    /// parses group id, one leading uint32 value, a counted identity array, and a parallel uint32 array.
    /// Producer semantics remain provisional until the role/flag consumer is mapped.
    /// </summary>
    [Message(GameMessageOpcode.ServerGroupIdentityListAndUInt32Array)]
    public class ServerGroupIdentityListAndUInt32Array : IWritable
    {
        public ulong GroupId { get; set; }

        public uint LeadingValue { get; set; }

        public List<Identity> MemberIdentities { get; } = [];

        public List<uint> Values { get; } = [];

        public void Write(GamePacketWriter writer)
        {
            if (MemberIdentities.Count != Values.Count)
                throw new InvalidOperationException("Group identity/value arrays must have matching counts.");

            writer.Write(GroupId);
            writer.Write(LeadingValue);
            writer.Write((uint)MemberIdentities.Count);
            foreach (Identity identity in MemberIdentities)
                identity.Write(writer);
            writer.WriteRetailCompositeUInt32Array(Values.ToArray());
        }
    }
}
