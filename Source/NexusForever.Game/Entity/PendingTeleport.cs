using System.Numerics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Matching.Match;
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
        internal PendingMatchTransfer MatchTransfer { get; init; }
    }

    internal sealed record PendingTaxiFlight(IGameSession Session, uint DestinationNode, ulong Fare, Vector3 Rotation)
    {
        public bool Admitted { get; set; }
    }

    internal sealed record PendingMatchTransfer(IMatchTeamMember Member, IMapPosition Source, Vector3 SourceRotation)
    {
        public bool SourceRemovalStarted { get; set; }
    }
}
