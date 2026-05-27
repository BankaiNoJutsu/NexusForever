namespace NexusForever.Database.Character.Model
{
    public class CharacterPathMissionModel
    {
        public ulong Id { get; set; }
        public ushort PathMissionId { get; set; }
        public ushort PathEpisodeId { get; set; }
        public byte State { get; set; }
        public byte Completed { get; set; }
        public uint ProgressCount { get; set; }
        public uint ProgressData { get; set; }
        public uint Xp { get; set; }

        public CharacterModel Character { get; set; }
    }
}
