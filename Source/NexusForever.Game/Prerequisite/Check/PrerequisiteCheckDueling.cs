using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Abstract.Pvp;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Build 16042 type 204 tests active duel state, excluding invitation/countdown.
    /// Its dispatcher forwards only the entity and comparison, not value/object ID.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.Dueling)]
    public class PrerequisiteCheckDueling : IPrerequisiteCheck
    {
        private readonly IDuelManager duelManager;

        public PrerequisiteCheckDueling(IDuelManager duelManager)
        {
            this.duelManager = duelManager;
        }

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            if (player == null)
                return false;

            return comparison switch
            {
                PrerequisiteComparison.Equal => duelManager.IsDueling(player),
                PrerequisiteComparison.NotEqual => !duelManager.IsDueling(player),
                _ => false
            };
        }
    }
}
