using NexusForever.Game.Abstract.Entity;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Housing
{
    /// <summary>
    /// Emits mapped housing auxiliary packets that share empty or scalar readers with the early housing cluster.
    /// </summary>
    public static class HousingAuxiliaryPacketEmitter
    {
        public static void EnqueueResidenceSessionPackets(IPlayer owner, ulong residenceId)
        {
            foreach (IWritable packet in BuildResidenceSessionPackets(residenceId))
                owner.Session.EnqueueMessageEncrypted(packet);
        }

        public static IReadOnlyList<IWritable> BuildResidenceSessionPackets(ulong residenceId)
        {
            uint residenceId32 = (uint)(residenceId & 0xFFFFFFFFu);

            return
            [
                new ServerHousingResidenceEmpty(),
                new ServerHousingResidenceUInt15 { Value = residenceId32 },
                new ServerHousingResidenceUInt15Alt { Value = residenceId32 },
                new ServerHousingResidenceWideString { Text = string.Empty },
                new ServerHousingResidenceEmptyFollowUp(),
                new ServerHousingBasicsEmpty(),
                new ServerHousingBasicsFollowup
                {
                    Value0 = residenceId32,
                    Value1 = 0u,
                    Value2 = 0u,
                    Value3 = 0u,
                },
            ];
        }
    }
}
