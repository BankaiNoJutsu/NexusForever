using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Native type 179: evaluator case 0xb3 calls manager vtable +0x378 (14049eeb0) with
    /// the primary entity, comparison and ObjectId; Value and the relationship target are not read.
    /// Uses the same slot rule as the pure-179 spell path. Evidence tier: Verified native field
    /// binding; Source implementation, no retail parity claim. See PREREQUISITE_179_FIELD_BINDING_20261114.md.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.CreatureDifficulty)]
    public class PrerequisiteCheckCreatureDifficulty : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => MeetsUnit(player, comparison, value, objectId, parameters);

        public bool MeetsUnit(IUnitEntity subject, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => CreatureDifficultyPrerequisiteHelper.MeetsPrerequisite(subject, comparison, objectId);
    }
}
