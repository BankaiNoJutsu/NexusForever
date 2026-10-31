using NexusForever.Game.Static.Quest;
using NexusForever.Network;
using NexusForever.Network.Message;

internal static class ContractProtocol
{
    internal static void WriteAccept(GamePacketWriter writer, ushort questId)
    {
        if (questId is 0 or > 0x7FFF) throw new ArgumentOutOfRangeException(nameof(questId));
        writer.Write(300u, 9); // Existing receiverless Quest accept sentinel.
        writer.Write(0u);
        writer.Write(questId, 15);
    }

    internal static (ushort Id, QuestState State) ReadChange(byte[] body)
    {
        using var reader = new GamePacketReader(new MemoryStream(body));
        ushort id = reader.ReadUShort(15); var state = reader.ReadEnum<QuestState>(32);
        _ = reader.ReadUInt();
        Validate(id, state);
        return (id, state);
    }

    internal static IReadOnlyDictionary<ushort, QuestState> ReadInit(byte[] body)
    {
        using var reader = new GamePacketReader(new MemoryStream(body));
        var states = new Dictionary<ushort, QuestState>();
        void Add(ushort id, QuestState state)
        {
            Validate(id, state);
            if (!states.TryAdd(id, state)) throw new InvalidDataException("Duplicate Quest identity.");
        }
        int completed = Count(reader);
        for (int i = 0; i < completed; ++i) { ushort id = reader.ReadUShort(15); _ = reader.ReadBit(); Add(id, QuestState.Completed); }
        int inactive = Count(reader);
        for (int i = 0; i < inactive; ++i) Add(reader.ReadUShort(15), reader.ReadEnum<QuestState>(4));
        int active = Count(reader);
        for (int i = 0; i < active; ++i)
        {
            Add(reader.ReadUShort(15), reader.ReadEnum<QuestState>(4));
            _ = reader.ReadUInt(); _ = reader.ReadUInt();
            int objectives = Count(reader);
            for (int j = 0; j < objectives; ++j) _ = reader.ReadUInt();
            _ = reader.ReadUInt();
            for (int j = 0; j < objectives; ++j) _ = reader.ReadUInt();
        }
        _ = reader.ReadULong();
        return states;
    }

    private static int Count(GamePacketReader reader)
    {
        uint count = reader.ReadUInt();
        if (count > 4096) throw new InvalidDataException("Quest collection exceeds the scenario bound.");
        return (int)count;
    }

    private static void Validate(ushort id, QuestState state)
    {
        if (id == 0 || !Enum.IsDefined(state)) throw new InvalidDataException("Invalid Quest identity or state.");
    }
}

internal sealed class ContractSocketObserver
{
    private readonly object gate = new();
    private Dictionary<ushort, QuestState> states = [];
    private int changes;
    internal int Changes { get { lock (gate) return changes; } }
    internal QuestState? State(ushort id) { lock (gate) return states.TryGetValue(id, out var state) ? state : null; }
    internal void Receive(GameFrame frame)
    {
        lock (gate)
        {
            if (frame.Opcode == GameMessageOpcode.ServerQuestInit) states = new(ContractProtocol.ReadInit(frame.Body));
            else if (frame.Opcode == GameMessageOpcode.ServerQuestStateChange)
            {
                var change = ContractProtocol.ReadChange(frame.Body); states[change.Id] = change.State; ++changes;
            }
        }
    }
}
