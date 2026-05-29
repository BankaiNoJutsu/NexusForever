using NexusForever.Network.Message;
using NexusForever.Game.Static.Crafting;

namespace NexusForever.Network.World.Message.Model.Crafting
{
    [Message(GameMessageOpcode.ClientTradeskillResetTalents)]
    public class ClientTradeskillResetTalents : IReadable
    {
        /// <summary>
        /// Native sender <c>Tradeskill_SendClientTradeskillResetTalents</c> (<c>14059acb0</c>)
        /// sends opcode <c>0x0858</c> through shared
        /// <c>ClientTradeskillResetTalents_WritePayload</c> (<c>14007d010</c>) as one 32-bit
        /// tradeskill id.
        /// </summary>
        public TradeskillType TradeskillId { get; private set; }

        public void Read(GamePacketReader reader)
        {
            TradeskillId = reader.ReadEnum<TradeskillType>(32u);
        }
    }
}
