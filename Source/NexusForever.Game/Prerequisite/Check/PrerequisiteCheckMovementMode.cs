using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Entity.Movement.Command.Mode;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 78: handler table <c>Prerequisite_CheckMovementMode</c> (<c>14049ef70</c>) reads
    /// movement mode byte at entity <c>+0x640</c>. NF uses <see cref="IMovementManager.GetMode"/>.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.MovementMode)]
    public class PrerequisiteCheckMovementMode : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            IWorldEntity entity = parameters.Target as IWorldEntity ?? player;
            ModeType mode = entity.MovementManager.GetMode();
            return PrerequisiteCompare.Compare(comparison, (uint)mode, value);
        }
    }
}
