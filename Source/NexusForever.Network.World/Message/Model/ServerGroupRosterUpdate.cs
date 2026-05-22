using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Per-member roster/stat refresh (0x0436, 0x60 bytes).
    /// Client copy layout matches <see cref="ServerGroupMemberStatUpdate"/> via FUN_140607490 @ 140607490.
    /// </summary>
    [Message(GameMessageOpcode.ServerGroupRosterUpdate)]
    public class ServerGroupRosterUpdate : ServerGroupMemberStatUpdate
    {
    }
}
