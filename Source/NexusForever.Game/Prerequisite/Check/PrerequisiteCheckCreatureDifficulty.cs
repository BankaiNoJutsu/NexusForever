using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.GameTable;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Mirrors spell apply-path checks in <see cref="NexusForever.Game.Spell.Spell"/> for rows typed
    /// <see cref="PrerequisiteType.CreatureDifficulty"/>. Handler table[179] at <c>1404a18f0</c> calls
    /// <c>PrimalMatrix_LookupAllocatedNode</c> (<c>1404d6a60</c>) and is a separate dead-path cluster.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.CreatureDifficulty)]
    public class PrerequisiteCheckCreatureDifficulty : IPrerequisiteCheck
    {
        private readonly IGameTableManager gameTableManager;

        public PrerequisiteCheckCreatureDifficulty(IGameTableManager gameTableManager)
        {
            this.gameTableManager = gameTableManager;
        }

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            IWorldEntity entity = parameters.Target as IWorldEntity ?? player;
            if (!CreatureDifficultyPrerequisiteHelper.TryGetCreatureDifficultyId(entity, gameTableManager, out uint creatureDifficultyId))
                return false;

            return PrerequisiteCompare.Compare(comparison, creatureDifficultyId, objectId);
        }
    }
}
