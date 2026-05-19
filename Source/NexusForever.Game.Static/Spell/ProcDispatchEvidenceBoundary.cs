namespace NexusForever.Game.Static.Spell
{
    /// <summary>
    /// Describes the conservative proc dispatch boundary that is currently backed by evidence.
    /// </summary>
    public readonly record struct ProcDispatchEvidenceBoundarySnapshot(
        bool TriggerEventSupported,
        string TriggerEventLabel,
        bool TargetDataSupported,
        string TargetDataLabel,
        ProcDispatchTargetRoute? TargetRoute,
        string BlockedReason)
    {
        public bool IsConservativelyDispatchSupported => BlockedReason == null;

        public string DispatchSupportLabel => BlockedReason ?? "dispatch-supported";

        public string TargetRouteLabel => TargetRoute switch
        {
            ProcDispatchTargetRoute.Holder      => "holder",
            ProcDispatchTargetRoute.Counterpart => "counterpart",
            _                                   => "unsupported"
        };
    }

    /// <summary>
    /// Central evidence gate for proc dispatch. Unsupported trigger events or target-data tails must remain diagnostic-only.
    /// </summary>
    public static class ProcDispatchEvidenceBoundary
    {
        public static ProcDispatchEvidenceBoundarySnapshot Describe(uint triggerEvent, uint targetData)
        {
            bool triggerEventSupported = ProcTriggerEventCandidate.TryGetConservativeLabel(triggerEvent, out string triggerEventLabel);
            bool targetDataSupported = ProcTargetDataCandidate.TryGetConservativeLabel(targetData, out string targetDataLabel);
            ProcDispatchTargetRoute? targetRoute = ProcTargetDataCandidate.TryGetConservativeDispatchRoute(targetData, out ProcDispatchTargetRoute route)
                ? route
                : null;

            string blockedReason = (triggerEventSupported, targetDataSupported) switch
            {
                (false, false) => "unsupported-trigger-event+unsupported-target-data",
                (false, true)  => "unsupported-trigger-event",
                (true, false)  => "unsupported-target-data",
                _              => null
            };

            return new ProcDispatchEvidenceBoundarySnapshot(
                triggerEventSupported,
                triggerEventLabel ?? "dispatch-unsupported",
                targetDataSupported,
                targetDataLabel ?? "dispatch-unsupported",
                targetRoute,
                blockedReason);
        }
    }
}
