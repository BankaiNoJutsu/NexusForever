using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Creature;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Prerequisite
{
    internal static class CreatureDifficultyPrerequisiteHelper
    {
        public static bool TryGetCreatureDifficultyId(IWorldEntity entity, IGameTableManager gameTableManager, out uint creatureDifficultyId)
        {
            creatureDifficultyId = entity.CreatureInfo?.DifficultyEntry?.Id
                ?? gameTableManager.Creature2Difficulty.GetEntry(entity.CreatureEntry?.Creature2DifficultyId ?? 0u)?.Id
                ?? 0u;

            return creatureDifficultyId != 0u;
        }
    }
}
