using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 63: live case <c>0x3f</c> compares the evaluated entity identity to the
    /// client local-player entity pointer. NF prerequisite evaluation is session-player scoped.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.IsLocalPlayerEntity)]
    public class PrerequisiteCheckIsLocalPlayerEntity : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            return comparison switch
            {
                PrerequisiteComparison.Equal    => true,
                PrerequisiteComparison.NotEqual => false,
                _                               => false
            };
        }
    }
}
