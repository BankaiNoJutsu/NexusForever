namespace NexusForever.Game.Abstract.Quest
{
    /// <summary>
    /// Immutable static description of a Contract's base reward-track points.
    /// This is neither a completion receipt nor permission to credit or claim.
    /// Offering, completion, modifiers and durable delivery require their owners.
    /// </summary>
    public sealed record ContractRewardSource(ushort QuestId, uint PeriodicQuestGroupId,
        uint TrackId, uint TrackType, uint TrackPrerequisiteId, uint Quality,
        uint BasePoints, bool UsesFormulaFallback);
}
