using NexusForever.Database.Character;
using NexusForever.Network.Message;

namespace NexusForever.Game.Abstract.Achievement
{
    public interface IAchievement : IDatabaseCharacter, INetworkBuildable<Network.World.Message.Model.Achievement.Achievement>
    {
        IAchievementInfo Info { get; }
        ushort Id { get; }
        uint ProgressCount { get; set; }
        uint CompletedChecklistMask { get; set; }
        uint CreditedChecklistMask { get; set; }
        DateTime? DateCompleted { get; set; }

        /// <summary>
        /// Returns if <see cref="IAchievement"/> has been completed.
        /// </summary>
        bool IsComplete();
    }
}