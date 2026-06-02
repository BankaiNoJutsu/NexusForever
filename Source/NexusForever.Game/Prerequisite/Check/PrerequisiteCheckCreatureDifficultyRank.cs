using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Prerequisite.Check
{
    [PrerequisiteCheck(PrerequisiteType.CreatureDifficultyRank)]
    public class PrerequisiteCheckCreatureDifficultyRank : IPrerequisiteCheck
    {
        private readonly IGameTableManager gameTableManager;

        public PrerequisiteCheckCreatureDifficultyRank(IGameTableManager gameTableManager)
        {
            this.gameTableManager = gameTableManager;
        }

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            IWorldEntity entity = parameters.Target as IWorldEntity ?? player;
            Creature2DifficultyEntry difficulty = entity.CreatureInfo?.DifficultyEntry
                ?? (CreatureDifficultyPrerequisiteHelper.TryGetCreatureDifficultyId(entity, gameTableManager, out uint creatureDifficultyId)
                    ? gameTableManager.Creature2Difficulty.GetEntry(creatureDifficultyId)
                    : null);

            if (difficulty == null)
                return false;

            if (objectId != 0u && difficulty.Id != objectId)
                return false;

            return PrerequisiteCompare.Compare(comparison, difficulty.RankValue, value);
        }
    }
}
