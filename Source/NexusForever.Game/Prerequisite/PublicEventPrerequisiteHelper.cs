using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Static.PublicEvent;

namespace NexusForever.Game.Prerequisite
{
    internal static class PublicEventPrerequisiteHelper
    {
        /// <summary>
        /// Whether the public event with the supplied id exists on the player's map and has not finished. This is the server model of the
        /// native event-manager query behind prerequisite type 117 (the client asks its global manager; here events live on the map instance).
        /// </summary>
        public static bool IsEventRunning(IPlayer player, uint eventId)
            => GetRunningEvent(player, eventId) != null;

        /// <summary>
        /// The running public event with the supplied id on the player's map, or null.
        /// </summary>
        public static IPublicEvent GetRunningEvent(IPlayer player, uint eventId)
        {
            IPublicEvent publicEvent = player.Map?.PublicEventManager?.GetEvent(eventId);
            return publicEvent != null && !publicEvent.IsFinalised ? publicEvent : null;
        }

        /// <summary>
        /// Status of the public event objective with the supplied id, preferring the objective of a team the player is a member of. The native
        /// query for prerequisite type 118 yields 0 when the objective cannot be found, which is <see cref="PublicEventStatus.Inactive"/>.
        /// </summary>
        public static PublicEventStatus GetObjectiveStatus(IPlayer player, uint objectiveId)
            => FindObjective(player, objectiveId)?.Status ?? PublicEventStatus.Inactive;

        /// <summary>
        /// Progress count of the public event objective with the supplied id (virtual +0x98 in the native prerequisite types 123 and 162), 0 when not found.
        /// </summary>
        public static uint GetObjectiveCount(IPlayer player, uint objectiveId)
            => FindObjective(player, objectiveId)?.Count ?? 0u;

        private static IPublicEventObjective FindObjective(IPlayer player, uint objectiveId)
        {
            IPublicEventManager manager = player.Map?.PublicEventManager;
            if (manager == null)
                return null;

            IPublicEventObjective fallback = null;
            foreach (IPublicEvent publicEvent in manager.GetEvents())
            {
                foreach (IPublicEventTeam team in publicEvent.GetTeams())
                {
                    IPublicEventObjective objective = team.GetObjectives().FirstOrDefault(o => o.Entry?.Id == objectiveId);
                    if (objective == null)
                        continue;

                    if (team.GetMembers().Any(member => member.CharacterId == player.CharacterId))
                        return objective;

                    fallback ??= objective;
                }
            }

            return fallback;
        }
    }
}
