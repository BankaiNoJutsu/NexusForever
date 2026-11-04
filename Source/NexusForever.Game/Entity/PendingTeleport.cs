using System.Numerics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Map;
using NexusForever.Game.Static.Entity;
using NexusForever.Network.Session;

namespace NexusForever.Game.Entity
{
    public class PendingTeleport : IPendingTeleport
    {
        public TeleportReason Reason { get; init; }
        public IMapPosition MapPosition { get; init; }
        public uint? VanityPetId { get; init; }
        internal PendingTaxiFlight TaxiFlight { get; init; }
    }

    internal sealed record PendingTaxiFlight(IGameSession Session, uint DestinationNode, ulong Fare, Vector3 Rotation)
    {
        public bool Admitted { get; set; }
    }
}
