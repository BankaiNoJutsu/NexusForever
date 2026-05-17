using NexusForever.Network.Internal.Message.Shared;

namespace NexusForever.Network.Internal.Message.Player
{
    public class PlayerAbsorptionUpdatedMessage
    {
        public Identity Identity { get; set; }
        public float Absorption { get; set; }
        public float AbsorptionMax { get; set; }
        public float HealingAbsorb { get; set; }
        public float HealingAbsorbMax { get; set; }
    }
}
