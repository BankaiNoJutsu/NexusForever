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

        /// <summary>
        /// Progress of the public event objective with the supplied id as a percentage (count * 100 / required count; native type 162, <c>FUN_1404a0a70</c>), 0 when it is not found or its required count is 0.
        /// The required count is the dynamic maximum when set, otherwise the table count (the server's own completion rule).
        /// </summary>
        public static float GetObjectivePercent(IPlayer player, uint objectiveId)
        {
            IPublicEventObjective objective = FindObjective(player, objectiveId);
            if (objective == null)
                return 0f;

            uint required = objective.DynamicMax > 0u ? objective.DynamicMax : objective.Entry?.Count ?? 0u;
            return required == 0u ? 0f : objective.Count * 100f / required;
        }

        /// <summary>
        /// The objective with the supplied id together with its parent event when that event has started and not finished on the supplied map, or null.
        /// When <paramref name="characterId"/> is not zero the character must be a member of the event (the client only treats an event as active after it received
        /// its Start) and the objective of the character's own team is preferred; with zero any team's objective is used (a creature subject has no player).
        /// </summary>
        public static (IPublicEvent Event, IPublicEventObjective Objective)? FindActiveObjective(Abstract.Map.IBaseMap map, ulong characterId, uint objectiveId)
        {
            IPublicEventManager manager = map?.PublicEventManager;
            if (manager == null)
                return null;

            foreach (IPublicEvent publicEvent in manager.GetEvents())
            {
                if (publicEvent.IsFinalised)
                    continue;

                IPublicEventObjective own = null;
                IPublicEventObjective other = null;
                bool joined = characterId == 0ul;
                foreach (IPublicEventTeam team in publicEvent.GetTeams())
                {
                    bool onTeam = characterId != 0ul && team.GetMembers().Any(member => member.CharacterId == characterId);
                    joined |= onTeam;

                    IPublicEventObjective objective = team.GetObjectives().FirstOrDefault(o => o.Entry?.Id == objectiveId);
                    if (objective == null)
                        continue;

                    if (onTeam)
                        own ??= objective;
                    else
                        other ??= objective;
                }

                IPublicEventObjective found = own ?? other;
                if (joined && found != null)
                    return (publicEvent, found);
            }

            return null;
        }

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
