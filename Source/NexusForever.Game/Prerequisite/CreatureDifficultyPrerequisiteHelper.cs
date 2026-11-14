using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Creature;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Prerequisite
{
    internal static class CreatureDifficultyPrerequisiteHelper
    {
        private const uint NativeDefaultCreatureDifficultyId = 25u;

        /// <summary>
        /// Native type 179 (14049eeb0): compare the primary entity's difficulty ID with ObjectId.
        /// No creature record compares zero; a present record with raw ID zero retains the runtime
        /// record constructor's default 25. The native setter caches the ID independently of the
        /// Creature2Difficulty lookup. See PREREQUISITE_179_FIELD_BINDING_20261114.md.
        /// </summary>
        public static bool MeetsPrerequisite(IWorldEntity entity, PrerequisiteComparison comparison, uint objectId)
        {
            if (entity == null)
                return false;

            ICreatureInfo creatureInfo = entity.CreatureInfo;
            Creature2Entry creatureEntry = entity.CreatureEntry ?? creatureInfo?.Entry;
            uint creatureDifficultyId = creatureInfo?.DifficultyEntry?.Id ?? creatureEntry?.Creature2DifficultyId ?? 0u;
            if (creatureDifficultyId == 0u && (creatureInfo != null || creatureEntry != null))
                creatureDifficultyId = NativeDefaultCreatureDifficultyId;

            return PrerequisiteCompare.Compare(comparison, creatureDifficultyId, objectId);
        }

        public static bool TryGetCreatureDifficultyId(IWorldEntity entity, IGameTableManager gameTableManager, out uint creatureDifficultyId)
        {
            creatureDifficultyId = entity.CreatureInfo?.DifficultyEntry?.Id
                ?? gameTableManager.Creature2Difficulty.GetEntry(entity.CreatureEntry?.Creature2DifficultyId ?? 0u)?.Id
                ?? 0u;

            return creatureDifficultyId != 0u;
        }
    }
}
