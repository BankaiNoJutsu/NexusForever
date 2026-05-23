using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Matching.Match;
using NexusForever.Game.Static.Matching;

namespace NexusForever.WorldServer.Network.Message.Handler.Matching
{
    /// <summary>
    /// Shared validation for replacement-search client requests (0x05D5 / 0x0602).
    /// Server backfill/merge remains blocked; only mapped client request surfaces are accepted.
    /// </summary>
    internal static class MatchingLookingForReplacementsValidation
    {
        // Client sender 0x14076aa30 emits only role bits 0..2 for opcode 0x05D5.
        private const Role ValidReplacementRoles = Role.Tank | Role.Healer | Role.DPS;

        public static bool IsValidReplacementRoleMask(Role roles)
        {
            return (roles & ~ValidReplacementRoles) == Role.None;
        }

        public static bool TryGetInProgressMatch(IMatchManager matchManager, IPlayer player, out IMatch match)
        {
            match = null;
            if (player == null)
                return false;

            IMatchCharacter matchCharacter = matchManager.GetMatchCharacter(player.Identity);
            match = matchCharacter?.Match;
            if (match == null)
                return false;

            return match.Status == MatchStatus.InProgress;
        }
    }
}
