using NexusForever.Game.Static.Spell;
using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.Pet
{
    /// <summary>
    /// Native misc-skill action-bar click handler sends the shortcut set and row slot index.
    /// Engineer combat-bot row 299 uses this for the PrimaryPetBar Attack/Stop buttons.
    /// </summary>
    [Message(GameMessageOpcode.ClientPetCommand)]
    public class ClientPetCommand : IReadable
    {
        public ShortcutSet ShortcutSet { get; private set; }
        public uint SlotIndex { get; private set; }

        public void Read(GamePacketReader reader)
        {
            ShortcutSet = reader.ReadEnum<ShortcutSet>(32u);
            SlotIndex   = reader.ReadUInt();
        }
    }
}
