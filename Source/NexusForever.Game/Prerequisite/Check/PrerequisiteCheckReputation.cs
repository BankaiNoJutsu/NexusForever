using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Static.Reputation;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 5: live <c>PrerequisiteManager_EvaluateTypeSlot</c> case <c>0x5</c> reads
    /// <c>entity+0x118</c> faction component vtable <c>+0x20</c> for <c>objectId0</c> faction id,
    /// then <c>PrerequisiteManager_ApplyComparisonFloat</c> (<c>1404a2010</c>). Handler table[5]
    /// <c>14049d470</c> is an unreached orphan (entity-id list at eval-context <c>+0x2b8</c>).
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.Reputation)]
    public class PrerequisiteCheckReputation : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            float amount = player.ReputationManager.GetReputation((Faction)objectId)?.Amount ?? 0f;
            return PrerequisiteCompare.Compare(comparison, (uint)amount, value);
        }
    }
}
