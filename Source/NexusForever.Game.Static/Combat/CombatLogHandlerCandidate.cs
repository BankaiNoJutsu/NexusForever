namespace NexusForever.Game.Static.Combat
{
    /// <summary>
    /// Candidate labels for client combat-log handlers mapped from decompile anchors.
    /// </summary>
    /// <remarks>
    /// These labels are diagnostics/inspection-only until payload-level behavior is fully verified.
    /// </remarks>
    public static class CombatLogHandlerCandidate
    {
        public const string CCStateBreak         = "CombatLog_HandleCCStateBreak (14060bbc0)";
        public const string ModifyInterruptArmor = "CombatLog_HandleModifyInterruptArmor (14060e210)";
    }
}
