using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;

namespace NexusForever.Game.Prerequisite
{
    /// <summary>
    /// Shared gate for handler-table checks that require an NPC/vendor target (client entity+0x80
    /// types <c>0x14</c>/<c>0x17</c>) instead of the local player.
    /// </summary>
    internal static class PrerequisiteNpcTargetContext
    {
        public static bool RequiresNpcTarget(IPlayer player, IPrerequisiteParameters parameters)
        {
            return parameters.Target != null && parameters.Target.Guid != player.Guid;
        }
    }
}
