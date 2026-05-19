namespace NexusForever.Game.Static.Spell
{
    public enum ProcDispatchTargetRoute
    {
        Holder,
        Counterpart
    }

    /// <summary>
    /// Conservative proc target-routing candidates backed by current evidence.
    /// </summary>
    public static class ProcTargetDataCandidate
    {
        public static bool IsConservativelySupported(uint targetData)
        {
            return TryGetConservativeDispatchRoute(targetData, out _);
        }

        public static bool TryGetConservativeDispatchRoute(uint targetData, out ProcDispatchTargetRoute route)
        {
            switch (targetData)
            {
                case 1u:
                case 2u:
                case 9u:
                    route = ProcDispatchTargetRoute.Holder;
                    return true;
                case 4u:
                case 12u:
                    route = ProcDispatchTargetRoute.Counterpart;
                    return true;
                default:
                    route = default;
                    return false;
            }
        }

        public static bool TryGetConservativeLabel(uint targetData, out string label)
        {
            if (!TryGetConservativeDispatchRoute(targetData, out ProcDispatchTargetRoute route))
            {
                label = null;
                return false;
            }

            label = route switch
            {
                ProcDispatchTargetRoute.Holder      => "holder route candidate",
                ProcDispatchTargetRoute.Counterpart => "counterpart route candidate",
                _                                   => null
            };

            return label != null;
        }
    }
}
