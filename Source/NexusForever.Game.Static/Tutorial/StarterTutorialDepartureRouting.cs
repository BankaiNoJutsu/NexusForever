using System.Numerics;

namespace NexusForever.Game.Static.Tutorial
{
    public readonly record struct StarterTutorialDepartureDestination(string Name, ushort WorldId, Vector3 Position, ushort WelcomeQuestId);

    public static class StarterTutorialDepartureRouting
    {
        private const ushort EverstarGroveWorldId = 990;
        private const ushort NorthernWildsWorldId = 426;
        private const ushort CrimsonIsleWorldId = 870;
        private const ushort LevianBayWorldId = 1387;

        // Quest2.tbl build 16042: preq_flags=1 lets these welcome quests unlock from Rider's Reef or the legacy arkship equivalent.
        private const ushort EverstarGroveWelcomeQuestId = 9113;
        private const ushort NorthernWildsWelcomeQuestId = 9112;
        private const ushort CrimsonIsleWelcomeQuestId = 9127;
        private const ushort LevianBayWelcomeQuestId = 9126;

        private static readonly Vector3 EverstarGroveArrival = new(-771.823f, -904.2852f, -2269.56f);
        private static readonly Vector3 NorthernWildsArrival = new(4086f, -683f, -5217f);
        private static readonly Vector3 CrimsonIsleArrival = new(-8261.3984f, -995.471f, -242.3648f);
        private static readonly Vector3 LevianBayArrival = new(-3835.341f, -980.2174f, -6050.524f);

        public static StarterTutorialDepartureDestination ResolveExileDestination(uint? terminalCreatureId)
        {
            return terminalCreatureId switch
            {
                StarterTutorialDefinition.ExileEverstarGroveDepartureTerminalCreatureId => new StarterTutorialDepartureDestination("Everstar Grove", EverstarGroveWorldId, EverstarGroveArrival, EverstarGroveWelcomeQuestId),
                StarterTutorialDefinition.ExileNorthernWildsDepartureTerminalCreatureId => new StarterTutorialDepartureDestination("Northern Wilds", NorthernWildsWorldId, NorthernWildsArrival, NorthernWildsWelcomeQuestId),
                _ => new StarterTutorialDepartureDestination("Northern Wilds", NorthernWildsWorldId, NorthernWildsArrival, NorthernWildsWelcomeQuestId)
            };
        }

        public static StarterTutorialDepartureDestination ResolveDominionDestination(uint? terminalCreatureId)
        {
            return terminalCreatureId switch
            {
                StarterTutorialDefinition.DominionCrimsonIsleDepartureTerminalCreatureId => new StarterTutorialDepartureDestination("Crimson Isle", CrimsonIsleWorldId, CrimsonIsleArrival, CrimsonIsleWelcomeQuestId),
                StarterTutorialDefinition.DominionLevianBayDepartureTerminalCreatureId => new StarterTutorialDepartureDestination("Levian Bay", LevianBayWorldId, LevianBayArrival, LevianBayWelcomeQuestId),
                _ => new StarterTutorialDepartureDestination("Crimson Isle", CrimsonIsleWorldId, CrimsonIsleArrival, CrimsonIsleWelcomeQuestId)
            };
        }
    }
}
