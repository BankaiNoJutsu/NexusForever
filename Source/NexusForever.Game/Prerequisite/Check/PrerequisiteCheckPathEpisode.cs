using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 53: native case <c>0x35</c> (<c>FUN_14049fd80</c>) is true when <c>Value</c> equals the id of the path service's current episode (the object at <c>DAT_140c65970 + 0x58</c>) and false when
    /// there is no service or no current episode. Equal returns that, NotEqual its negation, other comparisons are false; <c>ObjectId</c> is not read.
    /// Status: native body read; the server's current episode is the last one it sent with <c>ServerPathSetCurrentEpisode</c> (inferred).
    /// See Decomp/Analysis/PREREQUISITE_PATH_EPISODE_20261107.md.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.PathEpisode)]
    public class PrerequisiteCheckPathEpisode : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            uint current = player.PathManager?.CurrentEpisodeId ?? 0u;
            bool match = current != 0u && current == value;
            return PrerequisiteCompare.CompareFlag(comparison, match);
        }
    }
}
