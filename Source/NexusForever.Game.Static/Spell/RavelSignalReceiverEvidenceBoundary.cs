namespace NexusForever.Game.Static.Spell
{
    public enum RavelSignalReceiverRoute
    {
        /// <summary>
        /// Dispatch <see cref="NexusForever.Script.Template.IWorldEntityScript.OnSignal"/> on the spell-resolved target entity.
        /// </summary>
        EntityScriptOnSignal,

        /// <summary>
        /// Emit diagnostics only; receiver graph for this mode is not evidenced on a single witness.
        /// </summary>
        DiagnosticsOnly
    }

    public readonly record struct RavelSignalReceiverEvidenceBoundarySnapshot(
        bool ModeSupported,
        string ModeLabel,
        RavelSignalReceiverRoute? ReceiverRoute,
        string BlockedReason)
    {
        public bool IsConservativelyDispatchSupported => BlockedReason == null && ReceiverRoute == RavelSignalReceiverRoute.EntityScriptOnSignal;

        public string DispatchSupportLabel => BlockedReason ?? "entity-script-on-signal";
    }

    /// <summary>
    /// Conservative receiver graph for <see cref="SpellEffectType.RavelSignal"/> before widening generic signal side effects.
    /// </summary>
    /// <remarks>
    /// Witness <c>Spell4=76797</c> pairs <c>DataBits00=1</c> with activation-object target routing and
    /// <c>DataBits01=27096</c>, which matches the paired <c>SetBusy</c> context id on the same spell.
    /// Client low-range dispatch falls through to the shared Ravel/Lua epilogue rather than a dedicated C++ handler.
    /// </remarks>
    public static class RavelSignalReceiverEvidenceBoundary
    {
        public const uint EntityScriptOnSignalMode = 1u;

        public static RavelSignalReceiverEvidenceBoundarySnapshot Describe(uint mode)
        {
            if (mode == EntityScriptOnSignalMode)
            {
                return new RavelSignalReceiverEvidenceBoundarySnapshot(
                    true,
                    "entity-script-on-signal",
                    RavelSignalReceiverRoute.EntityScriptOnSignal,
                    null);
            }

            return new RavelSignalReceiverEvidenceBoundarySnapshot(
                false,
                $"unsupported-mode-{mode}",
                RavelSignalReceiverRoute.DiagnosticsOnly,
                "unsupported-ravel-signal-mode");
        }
    }
}
