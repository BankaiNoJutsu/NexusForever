using System.Security.Cryptography;
using NexusForever.Game.Static.ICComm;
using NexusForever.Network;
using NexusForever.Network.Message;

internal sealed record ICCommWire(GameMessageOpcode Opcode, ulong Channel, uint MessageId, ICCommChannelType? Type,
    int? Result, string? Name, string? Text, string? Sender, string SHA256, string Body);

internal static class ICCommProtocol
{
    internal static void Join(GamePacketWriter writer, ICCommChannelType type, ulong guild, string name)
    { writer.Write(type, 3); writer.Write(guild); writer.WriteStringWide(name); }
    internal static void Send(GamePacketWriter writer, ulong channel, uint id, string text, string recipient)
    { writer.Write(channel); writer.Write(id); writer.WriteString(text); writer.WriteStringWide(recipient); }
    internal static ICCommWire? Read(GameFrame frame)
    {
        if (frame.Opcode is not (GameMessageOpcode.ServerICCommChannelJoin or GameMessageOpcode.ServerICCommChannelJoinResult
            or GameMessageOpcode.ServerICCommMessageResult or GameMessageOpcode.ServerICCommOrderedMessage
            or GameMessageOpcode.ServerICCommDirectedMessage or GameMessageOpcode.ServerICCommChannelLeft)) return null;
        using var reader = new GamePacketReader(new MemoryStream(frame.Body));
        ulong channel = 0; uint id = 0; ICCommChannelType? type = null; int? result = null;
        string? name = null, text = null, sender = null;
        switch (frame.Opcode)
        {
            case GameMessageOpcode.ServerICCommChannelJoin:
                type = reader.ReadEnum<ICCommChannelType>(3); channel = reader.ReadULong(); name = reader.ReadWideString(); break;
            case GameMessageOpcode.ServerICCommChannelJoinResult:
                type = reader.ReadEnum<ICCommChannelType>(3); result = (int)reader.ReadUInt(4); name = reader.ReadWideString(); break;
            case GameMessageOpcode.ServerICCommMessageResult:
                channel = reader.ReadULong(); id = reader.ReadUInt(); result = (int)reader.ReadUInt(4); break;
            case GameMessageOpcode.ServerICCommOrderedMessage:
                channel = reader.ReadULong(); id = reader.ReadUInt(); text = reader.ReadWideString(); break;
            case GameMessageOpcode.ServerICCommDirectedMessage:
                channel = reader.ReadULong(); text = reader.ReadString(); sender = reader.ReadWideString(); break;
            case GameMessageOpcode.ServerICCommChannelLeft:
                channel = reader.ReadULong(); break;
        }
        if (reader.BytesRemaining != 0) throw new InvalidDataException("Unexpected ICComm trailing bytes.");
        return new(frame.Opcode, channel, id, type, result, name, text, sender,
            Convert.ToHexString(SHA256.HashData(frame.Body)), Convert.ToBase64String(frame.Body));
    }
}
