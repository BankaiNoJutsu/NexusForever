using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Guild;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 183: live vtable <c>+0x328</c>
    /// <c>Prerequisite_CheckGuildMembership</c> (<c>14049e9e0</c>) walks player guild list vs registry.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.Guild)]
    public class PrerequisiteCheckGuild : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            uint memberOfGuild = 0u;
            if (objectId != 0u)
            {
                foreach (IGuildBase guild in player.GuildManager)
                {
                    if (guild.Id == objectId)
                    {
                        memberOfGuild = 1u;
                        break;
                    }
                }
            }
            else if (player.GuildManager.Guild != null)
            {
                memberOfGuild = 1u;
            }

            return PrerequisiteCompare.Compare(comparison, memberOfGuild, value);
        }
    }
}
