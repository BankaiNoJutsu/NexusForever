namespace NexusForever.Database.Character.Model
{
    /// <summary>
    /// One unlocked inlaid augmentation id of a character (the list the client keeps at player + 0x6df4 and prerequisite type 231 reads).
    /// </summary>
    public class CharacterInlaidAugmentationModel
    {
        public ulong Id { get; set; }
        public ushort AugmentationId { get; set; }
        public CharacterModel Character { get; set; }
    }
}
