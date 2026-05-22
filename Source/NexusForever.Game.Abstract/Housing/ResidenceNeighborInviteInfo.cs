using System;

namespace NexusForever.Game.Abstract.Housing
{
    public sealed class ResidenceNeighborInviteInfo
    {
        public ulong InviterCharacterId { get; set; }
        public ulong InviterResidenceId { get; set; }
        public string InviterName { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}