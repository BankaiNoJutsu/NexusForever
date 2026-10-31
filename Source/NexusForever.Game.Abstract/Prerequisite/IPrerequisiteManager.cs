using NexusForever.Game.Abstract.Entity;

namespace NexusForever.Game.Abstract.Prerequisite
{
    public interface IPrerequisiteManager
    {
        /// <summary>
        /// Checks if <see cref="IPlayer"/> meets supplied prerequisite.
        /// </summary>
        bool Meets(IPlayer player, uint prerequisiteId);

        /// <summary>
        /// Checks if <see cref="IPlayer"/> meets supplied prerequisite.
        /// </summary>
        bool Meets(IPlayer player, uint prerequisiteId, IPrerequisiteParameters parameters);

        /// <summary>
        /// Evaluates an explicit unit subject without changing the existing player/target APIs.
        /// </summary>
        bool MeetsUnit(IUnitEntity subject, uint prerequisiteId, IPrerequisiteParameters parameters = null)
            => subject is IPlayer player && Meets(player, prerequisiteId, parameters);
    }
}
