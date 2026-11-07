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
        {
            IPublicEvent publicEvent = player.Map?.PublicEventManager?.GetEvent(eventId);
            return publicEvent != null && !publicEvent.IsFinalised;
        }

        /// <summary>
        /// Status of the public event objective with the supplied id, preferring the objective of a team the player is a member of. The native
        /// query for prerequisite type 118 yields 0 when the objective cannot be found, which is <see cref="PublicEventStatus.Inactive"/>.
        /// </summary>
        public static PublicEventStatus GetObjectiveStatus(IPlayer player, uint objectiveId)
        {
            IPublicEventManager manager = player.Map?.PublicEventManager;
            if (manager == null)
                return PublicEventStatus.Inactive;

            IPublicEventObjective fallback = null;
            foreach (IPublicEvent publicEvent in manager.GetEvents())
            {
                foreach (IPublicEventTeam team in publicEvent.GetTeams())
                {
                    IPublicEventObjective objective = team.GetObjectives().FirstOrDefault(o => o.Entry?.Id == objectiveId);
                    if (objective == null)
                        continue;

                    if (team.GetMembers().Any(member => member.CharacterId == player.CharacterId))
                        return objective.Status;

                    fallback ??= objective;
                }
            }

            return fallback?.Status ?? PublicEventStatus.Inactive;
        }
    }
}
