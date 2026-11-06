using System.Security.Cryptography;
using NexusForever.Network;

internal sealed record CharacterListRow(ulong Id, string Name, uint Level, ushort World,
    ushort Zone, ushort Realm, bool Locked, bool RequiresRename, uint GearMask, uint AppearanceCount, uint GearCount);

internal sealed record CharacterListObservation(string BodySha256, string BodyHex, ushort Realm, IReadOnlyList<CharacterListRow> Characters)
{
    // Mirrors the complete existing ServerCharacterList writer, including rows after
    // the selected one. A SQL ID or a partially decoded row is not a list witness.
    internal static CharacterListObservation Read(byte[] body)
    {
        using var reader = new GamePacketReader(new MemoryStream(body));
        uint Count(uint minimumBits)
        {
            uint count = reader.ReadUInt();
            if (count > (ulong)body.Length * 8 / minimumBits)
                throw new InvalidDataException("Character-list count exceeds its payload.");
            return count;
        }
        uint Visuals()
        {
            uint count = Count(68);
            for (uint i = 0; i < count; i++)
            {
                reader.ReadUInt(7); reader.ReadUInt(15); reader.ReadUInt(14); reader.ReadUInt();
            }
            return count;
        }
        void UIntArray()
        {
            uint count = Count(32);
            for (uint i = 0; i < count; i++) reader.ReadUInt();
        }
        reader.ReadULong(); // Server time.
        uint characterCount = Count(64);
        var characters = new List<CharacterListRow>();
        var identities = new HashSet<ulong>();
        for (uint i = 0; i < characterCount; i++)
        {
            ulong id = reader.ReadULong();
            if (!identities.Add(id)) throw new InvalidDataException("Duplicate Character identity in wire list.");
            string name = reader.ReadWideString();
            reader.ReadUInt(2); reader.ReadUInt(5); reader.ReadUInt(5); reader.ReadUInt();
            uint level = reader.ReadUInt();
            uint appearance = Visuals(), gear = Visuals();
            ushort world = reader.ReadUShort(15), zone = reader.ReadUShort(15), realm = reader.ReadUShort(14);
            for (int field = 0; field < 5; field++) reader.ReadSingle(); // XYZ, yaw, pitch.
            reader.ReadUInt(3);
            bool locked = reader.ReadBit(), rename = reader.ReadBit();
            uint mask = reader.ReadUInt(), labels = reader.ReadUInt(4);
            for (uint label = 0; label < labels * 2; label++) reader.ReadUInt();
            uint bones = Count(32);
            for (uint bone = 0; bone < bones; bone++) reader.ReadSingle();
            reader.ReadSingle(); // Days since logout.
            characters.Add(new(id, name, level, world, zone, realm, locked, rename, mask, appearance, gear));
        }
        UIntArray(); UIntArray(); // Enabled/disabled creation choices.
        ushort listRealm = reader.ReadUShort(14);
        reader.ReadUShort(14); reader.ReadULong(); // Removal identity.
        for (int field = 0; field < 4; field++) reader.ReadUInt();
        reader.ReadUInt(14); reader.ReadBit(); // Faction restriction, free level50.
        if (reader.BytesRemaining != 0) throw new InvalidDataException("Trailing character-list bytes.");
        return new(Convert.ToHexStringLower(SHA256.HashData(body)), Convert.ToHexString(body), listRealm, characters);
    }
}
