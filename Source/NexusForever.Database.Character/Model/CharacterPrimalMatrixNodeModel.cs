namespace NexusForever.Database.Character.Model
{
    /// <summary>
    /// The saved allocation count of one Primal Matrix node of a character (the count the client snapshot carries per node and prerequisite type 292 reads).
    /// </summary>
    public class CharacterPrimalMatrixNodeModel
    {
        public ulong Id { get; set; }
        public ushort NodeId { get; set; }
        public byte Allocations { get; set; }
        public CharacterModel Character { get; set; }
    }
}
