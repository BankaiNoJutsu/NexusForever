using System.Buffers.Binary;
using NexusForever.Game.Static.Entity;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Static;

internal sealed record ChallengeWireState(uint Id, uint Count, uint Goal, uint Tier, uint LastTier, uint Completions, bool Active);
internal sealed record RewardWireState(uint Id, uint Points, uint Claimed, bool Active, IReadOnlyList<uint> Rows);

// Read only the mapped portions needed by this scenario. Entity create stops
// after the NPC model prefix; other entity models are not guessed or skipped.
internal static class ChallengeProtocol
{
    internal static (uint Guid, uint Creature)? ReadNpc(byte[] body)
    {
        using var reader = new GamePacketReader(new MemoryStream(body));
        uint guid = reader.ReadUInt();
        var type = reader.ReadEnum<EntityType>(6);
        if (type != EntityType.NonPlayer) return null;
        uint creature = reader.ReadUInt(18);
        _ = reader.ReadByte(); // mapped QuestChecklistIdx
        if (guid == 0 || creature == 0) throw new InvalidDataException("NPC create has an invalid identity.");
        return (guid, creature);
    }

    internal static IReadOnlyList<ChallengeWireState> ReadChallenges(byte[] body)
    {
        using var reader = new GamePacketReader(new MemoryStream(body));
        int count = Count(reader, 1024); var rows = new List<ChallengeWireState>(count); var ids = new HashSet<uint>();
        for (int i = 0; i < count; ++i)
        {
            uint id = reader.ReadUInt(14); _ = reader.ReadUInt(4);
            _ = reader.ReadUInt(); _ = reader.ReadUInt(); _ = reader.ReadUInt();
            uint progress = reader.ReadUInt(), goal = reader.ReadUInt(); _ = reader.ReadUInt();
            uint tier = reader.ReadUInt(), last = reader.ReadUInt(), completed = reader.ReadUInt();
            _ = reader.ReadBit(); bool active = reader.ReadBit(); _ = reader.ReadBit(); _ = reader.ReadBit();
            for (int field = 0; field < 9; ++field) _ = reader.ReadUInt(); // six timers and three tier goals
            if (id == 0 || !ids.Add(id)) throw new InvalidDataException("Challenge update has duplicate/invalid identities.");
            rows.Add(new(id, progress, goal, tier, last, completed, active));
        }
        return rows.AsReadOnly();
    }

    internal static IReadOnlyList<RewardWireState> ReadRewards(byte[] body)
    {
        using var reader = new GamePacketReader(new MemoryStream(body));
        int count = Count(reader, 1024); var rows = new List<RewardWireState>(count); var ids = new HashSet<uint>();
        for (int i = 0; i < count; ++i)
        {
            uint id = reader.ReadUInt(14), points = reader.ReadUInt();
            uint claimed = BinaryPrimitives.ReadUInt32LittleEndian(reader.ReadBytes(4));
            int slots = Count(reader, 32); _ = reader.ReadUInt();
            uint[] selected = new uint[slots]; for (int j = 0; j < slots; ++j) selected[j] = reader.ReadUInt();
            bool active = reader.ReadBit();
            uint allowed = slots == 32 ? uint.MaxValue : (1u << slots) - 1;
            if (id == 0 || !ids.Add(id) || (claimed & ~allowed) != 0 || selected.Any(row => row == 0 || row > 0x3FFF))
                throw new InvalidDataException("Reward reload has an invalid identity, slot or claim mask.");
            rows.Add(new(id, points, claimed, active, Array.AsReadOnly(selected)));
        }
        return rows.AsReadOnly();
    }

    private static int Count(GamePacketReader reader, uint maximum)
    {
        uint count = reader.ReadUInt();
        if (count > maximum) throw new InvalidDataException("Protocol collection exceeds the scenario bound.");
        return (int)count;
    }

    internal static void WriteSelectedCast(GamePacketWriter writer, uint token, uint spell, uint target, float x, float y, float z)
    {
        writer.Write(token); writer.Write(spell); writer.Write(target); writer.Write(x); writer.Write(y); writer.Write(z);
    }
}

internal sealed class ChallengeSocketObserver(uint challengeId, uint spellId = 0)
{
    private readonly object gate = new();
    private readonly Dictionary<uint, uint> npcs = [];
    private Dictionary<uint, RewardWireState> rewards = [];
    private ChallengeWireState? challenge;
    private long reloads;
    private uint acceptedTarget;
    private readonly Dictionary<uint, uint> sentCasts = [];
    private readonly Dictionary<string, int> castResults = [];
    internal long Reloads { get { lock (gate) return reloads; } }
    internal bool ChoicePublishedSince(uint trackId, long before)
    {
        lock (gate) return reloads > before && rewards.TryGetValue(trackId, out var track) && track.Active;
    }
    internal ChallengeWireState? Challenge { get { lock (gate) return challenge; } }
    internal RewardWireState? Reward(uint id) { lock (gate) return rewards.GetValueOrDefault(id); }
    internal uint[] Targets(IReadOnlySet<uint> creatures) { lock (gate) return npcs.Where(row => creatures.Contains(row.Value)).Select(row => row.Key).Order().ToArray(); }
    internal uint AcceptedTarget { get { lock (gate) return acceptedTarget; } }
    internal void SentCast(uint token, uint target)
    {
        lock (gate) { if (sentCasts.Count >= 256) sentCasts.Remove(sentCasts.Keys.Min()); sentCasts[token] = target; }
    }
    internal Dictionary<string, int> CastResults { get { lock (gate) return new(castResults); } }
    internal void Receive(GameFrame frame)
    {
        lock (gate)
        {
            switch (frame.Opcode)
            {
                case GameMessageOpcode.ServerEntityCreate:
                    if (ChallengeProtocol.ReadNpc(frame.Body) is { } npc) npcs[npc.Guid] = npc.Creature;
                    break;
                case GameMessageOpcode.ServerEntityDestroy:
                    using (var reader = new GamePacketReader(new MemoryStream(frame.Body)))
                    { uint id = reader.ReadUInt(); npcs.Remove(id); if (acceptedTarget == id) acceptedTarget = 0; }
                    break;
                case GameMessageOpcode.ServerChallengeUpdate:
                    challenge = ChallengeProtocol.ReadChallenges(frame.Body).SingleOrDefault(row => row.Id == challengeId);
                    break;
                case GameMessageOpcode.ServerRewardTracksReload:
                    rewards = ChallengeProtocol.ReadRewards(frame.Body).ToDictionary(row => row.Id); ++reloads;
                    break;
                case GameMessageOpcode.ServerSpellCastResult:
                    using (var reader = new GamePacketReader(new MemoryStream(frame.Body)))
                    {
                        uint token = reader.ReadUInt(); _ = reader.ReadUInt(18); var outcome = reader.ReadEnum<CastResult>(9); string result = outcome.ToString();
                        castResults[result] = castResults.GetValueOrDefault(result) + 1;
                        if (outcome != CastResult.Ok && sentCasts.TryGetValue(token, out uint target) && target == acceptedTarget) acceptedTarget = 0;
                    }
                    break;
                case GameMessageOpcode.ServerSpellStart:
                    using (var reader = new GamePacketReader(new MemoryStream(frame.Body)))
                    {
                        _ = reader.ReadUInt(); uint started = reader.ReadUInt(18); _ = reader.ReadUInt(18); _ = reader.ReadUInt(18);
                        _ = reader.ReadUInt(); _ = reader.ReadUShort(); uint target = reader.ReadUInt();
                        if (started == spellId && npcs.ContainsKey(target)) acceptedTarget = target;
                    }
                    break;
                case GameMessageOpcode.ServerForceKick:
                case GameMessageOpcode.ServerCharacterSelectFail:
                    throw new InvalidOperationException($"Scenario rejected by {frame.Opcode}.");
            }
        }
    }

    internal static bool Advanced(ChallengeWireState? before, ChallengeWireState? after)
        => after != null && (after.Completions > (before?.Completions ?? 0)
            || (before?.Active != true ? after.Active && after.Count > 0
                : after.Active && (after.Tier > before.Tier || after.Tier == before.Tier && after.Count > before.Count)));
}
