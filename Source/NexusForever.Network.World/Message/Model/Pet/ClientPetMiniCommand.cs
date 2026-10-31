using NexusForever.Game.Static.Spell;
using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.Pet;

/// <summary>Type11 shortcut click: shortcut set and row slot, both native u32 fields.</summary>
[Message(GameMessageOpcode.ClientPetMiniCommand)]
public class ClientPetMiniCommand : IReadable
{
    public ShortcutSet ShortcutSet { get; private set; }
    public uint SlotIndex { get; private set; }
    public void Read(GamePacketReader reader)
    {
        ShortcutSet = reader.ReadEnum<ShortcutSet>(32u);
        SlotIndex = reader.ReadUInt();
    }
}
