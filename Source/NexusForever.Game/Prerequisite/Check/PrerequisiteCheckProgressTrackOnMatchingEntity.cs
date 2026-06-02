using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.GameTable;

namespace NexusForever.Game.Prerequisite.Check
{
    [PrerequisiteCheck(PrerequisiteType.ProgressTrackOnMatchingEntity)]
    [PrerequisiteCheck(PrerequisiteType.Unknown291)]
    public class PrerequisiteCheckProgressTrackOnMatchingEntity : IPrerequisiteCheck
    {
        private readonly IGameTableManager gameTableManager;

        public PrerequisiteCheckProgressTrackOnMatchingEntity(IGameTableManager gameTableManager)
        {
            this.gameTableManager = gameTableManager;
        }

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            IWorldEntity target = parameters.Target as IWorldEntity ?? player;
            if (target.Guid != player.Guid)
                return false;

            uint progress = ProgressTrackPrerequisiteHelper.GetTrackedScalar(
                player,
                gameTableManager,
                ProgressTrackPrerequisiteHelper.ClientProgressTrackTypeEnum,
                objectId);

            return PrerequisiteCompare.Compare(comparison, progress, value);
        }
    }
}
