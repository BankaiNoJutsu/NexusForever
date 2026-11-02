using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.Entity
{
    /// <summary>
    /// Updates unit name text: build16042 case1403edadd calls14045b030 and UnitNameChanged.
    /// </summary>
    [Message(GameMessageOpcode.ServerEntityStatUInt32WideString)]
    public class ServerUnitNameChange : ServerEntityStatUInt32WideString
    {
        public uint UnitId
        {
            get => Value;
            set => Value = value;
        }

        public string Name
        {
            get => Text;
            set => Text = value;
        }
    }
}
