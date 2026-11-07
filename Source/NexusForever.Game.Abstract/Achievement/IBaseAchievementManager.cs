using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Achievement;

namespace NexusForever.Game.Abstract.Achievement
{
    public interface IBaseAchievementManager<T> : IDatabaseCharacter where T : class, IAchievementModel, new()
    {
        uint AchievementPoints { get; }

        /// <summary>
        /// Initialise a collection of existing achievement database models.
        /// </summary>
        void Initialise(IEnumerable<T> models, bool isPlayer);

        /// <summary>
        /// Returns if the supplied achievement id has been completed.
        /// </summary>
        bool HasCompletedAchievement(ushort id);

        /// <summary>
        /// Returns if bit <paramref name="bit"/> is set in the supplied achievement's progress state (bits 0 to 31) or credited checklist word (bits 32 to 63).
        /// An achievement without a record, or a bit beyond 63, has no bits set.
        /// </summary>
        bool IsChecklistBitSet(ushort id, uint bit);

        /// <summary>
        /// Send initial <see cref="IAchievement{T}"/> information to owner on login.
        /// </summary>
        void SendInitialPackets(IPlayer target);

        /// <summary>
        /// Grant achievement by supplied achievement id.
        /// </summary>
        void GrantAchievement(ushort id);

        /// <summary>
        /// Update or complete any achievements of <see cref="AchievementType"/> as <see cref="IPlayer"/> with supplied object ids.
        /// </summary>
        void CheckAchievements(IPlayer target, AchievementType type, uint objectId, uint objectIdAlt = 0, uint count = 1);

        /// <summary>
        /// Set current progress for threshold achievements of <see cref="AchievementType"/> as <see cref="IPlayer"/> with supplied object ids.
        /// </summary>
        void SetAchievementProgress(IPlayer target, AchievementType type, uint objectId, uint objectIdAlt, uint value);
    }
}
