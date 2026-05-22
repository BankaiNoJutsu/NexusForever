namespace NexusForever.Database.Character.Model
{
    public class ResidenceNeighborModel
    {
        public ulong ResidenceId { get; set; }
        public ulong NeighborCharacterId { get; set; }
        public byte PermissionLevel { get; set; }

        public ResidenceModel Residence { get; set; }
    }
}