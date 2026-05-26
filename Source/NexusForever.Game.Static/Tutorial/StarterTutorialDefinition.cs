namespace NexusForever.Game.Static.Tutorial
{
    public static class StarterTutorialDefinition
    {
        public const ushort ExileMovementQuestId = 10513;
        public const ushort DominionMovementQuestId = 10521;
        public const ushort ExileCombatQuestId = 10518;
        public const ushort DominionCombatQuestId = 10524;
        public const ushort ExileHoverboardQuestId = 10527;
        public const ushort DominionHoverboardQuestId = 10532;
        public const ushort ExileDepartureQuestId = 10528;
        public const ushort DominionDepartureQuestId = 10530;

        public const uint ExileCombatSimulationWorldLocationId = 51739u;
        public const uint DominionCombatSimulationWorldLocationId = 52898u;
        public const uint TutorialHoverboardFinishWorldLocationId = 51734u;
        public const uint ExileEverstarGroveDepartureTerminalCreatureId = 73605u;
        public const uint ExileNorthernWildsDepartureTerminalCreatureId = 73606u;
        public const uint DominionCrimsonIsleDepartureTerminalCreatureId = 74772u;
        public const uint DominionLevianBayDepartureTerminalCreatureId = 74773u;

        public static readonly ushort[] StarterQuestIds =
            [ExileMovementQuestId, DominionMovementQuestId, ExileHoverboardQuestId, DominionHoverboardQuestId];

        public static readonly ushort[] ReceiverlessQuestIds =
            [ExileMovementQuestId, DominionMovementQuestId, ExileHoverboardQuestId, DominionHoverboardQuestId, ExileCombatQuestId, DominionCombatQuestId];

        public static readonly ushort[] FollowUpQuestIds =
            [ExileCombatQuestId, 10519, 10520, 10522, 10523, DominionCombatQuestId, 10525, 10526, ExileDepartureQuestId, DominionDepartureQuestId, 10540, 10541];

        public static readonly ushort[] ExileQuestChain =
            [ExileMovementQuestId, ExileHoverboardQuestId, ExileCombatQuestId, 10525, 10540, 10519, 10520, ExileDepartureQuestId];

        public static readonly ushort[] DominionQuestChain =
            [DominionMovementQuestId, DominionHoverboardQuestId, DominionCombatQuestId, 10526, 10541, 10522, 10523, DominionDepartureQuestId];

        public static readonly uint[] HoverboardProjectorObjectiveIds = [21324u, 21354u];
        public static readonly uint[] HoverboardRideObjectiveIds = [21323u, 21355u];
        public static readonly uint[] HoverboardFinishObjectiveIds = [21325u, 21356u];
        public static readonly uint[] TutorialWorldLocationIds = [51735u, 51736u, 51737u, 51703u, TutorialHoverboardFinishWorldLocationId];
        public static readonly uint[] DepartureTerminalCreatureIds =
        [
            ExileEverstarGroveDepartureTerminalCreatureId,
            ExileNorthernWildsDepartureTerminalCreatureId,
            DominionCrimsonIsleDepartureTerminalCreatureId,
            DominionLevianBayDepartureTerminalCreatureId
        ];

        public static bool ShouldRecoverHoverboardFinishPosition(
            bool projectorObjectiveComplete,
            bool rideObjectiveComplete,
            bool finishObjectiveComplete)
        {
            return projectorObjectiveComplete
                && rideObjectiveComplete
                && !finishObjectiveComplete;
        }
    }
}
