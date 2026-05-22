using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Daily-login reward claim request (opcode 0x078F). Client sender <c>WildStar64.exe</c> <c>1400070f0</c>
    /// emits a zero-byte payload before <see cref="ServerDailyLoginUpdate"/>.
    /// </summary>
    [Message(GameMessageOpcode.ClientDailyLoginClaimReward)]
    public class ClientDailyLoginClaimReward : IReadable
    {
        public void Read(GamePacketReader reader)
        {
        }
    }
}
