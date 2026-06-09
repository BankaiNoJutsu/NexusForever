using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.Option
{
    [Message(GameMessageOpcode.ClientCombatLogDisableOthers)]
    public class ClientCombatLogDisableOthers : IReadable
    {
        public uint DisableOtherPlayersValue { get; set; }
        public bool DisableOtherPlayers
        {
            get => DisableOtherPlayersValue != 0u;
            set => DisableOtherPlayersValue = value ? 1u : 0u;
        }

        public void Read(GamePacketReader reader)
        {
            DisableOtherPlayersValue = reader.ReadUInt();
        }
    }
}
