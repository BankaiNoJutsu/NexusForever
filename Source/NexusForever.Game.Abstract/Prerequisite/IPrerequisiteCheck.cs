using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Abstract.Prerequisite
{
    public interface IPrerequisiteCheck
    {
        bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters);

        /// <summary>
        /// Evaluates an explicit subject, with Target retained as a separate relationship context.
        /// Existing player-only handlers remain compatible; unsupported non-player subjects fail closed.
        /// </summary>
        bool MeetsUnit(IUnitEntity subject, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => subject is IPlayer player && Meets(player, comparison, value, objectId, parameters);
    }
}
