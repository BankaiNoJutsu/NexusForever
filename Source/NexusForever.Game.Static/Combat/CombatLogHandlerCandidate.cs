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
        public const string CCState              = "CombatLog_DispatchCCStateEvent (14060b750)";
        public const string CCStateBreak         = "CombatLog_DispatchCCStateBreakEvent (14060bbc0)";
        public const string ModifyInterruptArmor = "CombatLog_DispatchModifyInterruptArmorEvent (14060e210)";
    }
}
