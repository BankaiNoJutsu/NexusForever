using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.GameTable;

namespace NexusForever.Game.Prerequisite.Check
{
    [PrerequisiteCheck(PrerequisiteType.LiveEventWorldFactionBranch)]
    public class PrerequisiteCheckLiveEventWorldFactionBranch : IPrerequisiteCheck
    {
        private readonly IGameTableManager gameTableManager;

        public PrerequisiteCheckLiveEventWorldFactionBranch(IGameTableManager gameTableManager)
        {
            this.gameTableManager = gameTableManager;
        }

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            if (!LiveEventPrerequisiteProgress.TryGetLiveEventEntry(gameTableManager, objectId, out _))
                return false;

            if (!LiveEventPrerequisiteProgress.RequiresNpcContext(player, parameters))
                return false;

            uint progress = LiveEventPrerequisiteProgress.GetFactionBranchProgress(gameTableManager, player, objectId);
            return PrerequisiteCompare.Compare(comparison, progress, value);
        }
    }
}
