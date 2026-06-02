using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Guild;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 184: live vtable <c>+0x330</c>
    /// <c>Prerequisite_CheckGuildMembershipOnTarget</c> (<c>14049ea30</c>) guild registry check with target context.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.Guild2)]
    public class PrerequisiteCheckGuild2 : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            if (parameters.Target is not IPlayer targetPlayer)
                return false;

            IGuildBase playerGuild = player.GuildManager.Guild;
            IGuildBase targetGuild = targetPlayer.GuildManager.Guild;
            uint sameGuild = playerGuild != null && targetGuild != null && playerGuild.Id == targetGuild.Id ? 1u : 0u;

            return PrerequisiteCompare.Compare(comparison, sameGuild, value);
        }
    }
}
