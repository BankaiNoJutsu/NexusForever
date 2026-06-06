using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Cinematic;
using NexusForever.Game.Abstract.Cinematic.Cinematics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Static.Reputation;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared.Game.Events;
using static NexusForever.Game.Static.Tutorial.StarterTutorialDefinition;

namespace NexusForever.Script.Main.Tutorial
{
    [ScriptFilterCreatureId(73735u)]
    public class TutorialCombatProjectorEntityScript : IWorldEntityScript, IOwnedScript<ISimpleCollidableEntity>
    {
        private const ushort TutorialWorldId = 3460;
        private const ushort ExileHoverboardQuestId = 10527;
        private const ushort DominionHoverboardQuestId = 10532;
        private const ushort ExileCombatQuestId = 10518;
        private const ushort DominionCombatQuestId = 10524;
        private const uint ExileHoverboardProjectorObjectiveId = 21324u;
        private const uint ExileHoverboardRideObjectiveId = 21323u;
        private const uint ExileHoverboardFinishObjectiveId = 21325u;
        private const uint DominionHoverboardProjectorObjectiveId = 21354u;
        private const uint DominionHoverboardRideObjectiveId = 21355u;
        private const uint DominionHoverboardFinishObjectiveId = 21356u;
        private const uint ExileCombatSimulationWorldLocationId = 51739u;
        private const uint DominionCombatSimulationWorldLocationId = 52898u;
        private static readonly TimeSpan combatProjectorTeleportDelay = TimeSpan.FromMilliseconds(8500d);

        private readonly HashSet<uint> pendingTransportPlayers = [];

        private ISimpleCollidableEntity owner;

        private readonly ICinematicFactory cinematicFactory;
        private readonly IGameTableManager gameTableManager;
        private readonly ILogger<TutorialCombatProjectorEntityScript> log;

        public TutorialCombatProjectorEntityScript(
            ILogger<TutorialCombatProjectorEntityScript> log,
            ICinematicFactory cinematicFactory,
            IGameTableManager gameTableManager)
        {
            this.log = log;
            this.cinematicFactory = cinematicFactory;
            this.gameTableManager = gameTableManager;
        }

        public void OnLoad(ISimpleCollidableEntity owner)
        {
            this.owner = owner;
            log.LogDebug("Starter tutorial combat projector script loaded for entity {EntityGuid}: creature={CreatureId}, questChecklistIdx={QuestChecklistIdx}.",
                owner.Guid,
                owner.CreatureId,
                owner.QuestChecklistIdx);
        }

        public void OnActivateSuccess(IPlayer activator)
        {
            if (activator == null
                || !TryGetCombatProjectorContext(activator.Faction1,
                    out ushort hoverboardQuestId,
                    out ushort combatQuestId,
                    out uint projectorObjectiveId,
                    out uint rideObjectiveId,
                    out _,
                    out uint destinationWorldLocationId))
                return;

            QuestState? hoverboardState = activator.QuestManager.GetQuestState(hoverboardQuestId);
            QuestState? combatState = activator.QuestManager.GetQuestState(combatQuestId);
            log.LogDebug("Starter tutorial combat projector activation for player {PlayerGuid}: entity={EntityGuid}, hoverboardState={HoverboardState}, combatState={CombatState}.",
                activator.Guid,
                owner?.Guid ?? 0u,
                hoverboardState?.ToString() ?? "None",
                combatState?.ToString() ?? "None");

            if (!CanUseCombatProjector(activator, hoverboardQuestId, hoverboardState, combatState, projectorObjectiveId, rideObjectiveId))
                return;

            WorldLocation2Entry destination = gameTableManager.WorldLocation2.GetEntry(destinationWorldLocationId);
            if (destination == null)
            {
                log.LogWarning("Starter tutorial combat projector missing destination world location {WorldLocationId} for player {PlayerGuid}.",
                    destinationWorldLocationId,
                    activator.Guid);
                return;
            }

            if (!TryGetCombatSimulationTeleportPosition(activator.Faction1, out Vector3 teleportPosition))
                return;

            if (!pendingTransportPlayers.Add(activator.Guid))
            {
                log.LogDebug("Starter tutorial combat projector ignored duplicate activation for player {PlayerGuid}: hoverboardState={HoverboardState}, combatState={CombatState}.",
                    activator.Guid,
                    hoverboardState?.ToString() ?? "None",
                    combatState?.ToString() ?? "None");
                return;
            }

            activator.CinematicManager.QueueCinematic(cinematicFactory.CreateCinematic<INoviceTutorialCombatProjector>());
            log.LogDebug("Starter tutorial combat projector queued cinematic for player {PlayerGuid}: entity={EntityGuid}, hoverboardState={HoverboardState}, combatState={CombatState}, destinationWorldLocation={WorldLocationId}.",
                activator.Guid,
                owner?.Guid ?? 0u,
                hoverboardState?.ToString() ?? "None",
                combatState?.ToString() ?? "None",
                destinationWorldLocationId);

            activator.Session.Events.EnqueueEvent(new DelayEvent(combatProjectorTeleportDelay, () =>
            {
                pendingTransportPlayers.Remove(activator.Guid);

                if (activator.Map?.Entry?.Id != TutorialWorldId)
                {
                    log.LogDebug("Starter tutorial combat projector skipped delayed transport for player {PlayerGuid}: map={MapId}.",
                        activator.Guid,
                        activator.Map?.Entry?.Id ?? 0u);
                    return;
                }

                if (!activator.CanTeleport())
                {
                    log.LogDebug("Starter tutorial combat projector skipped delayed transport for player {PlayerGuid}: teleport currently unavailable.",
                        activator.Guid);
                    return;
                }

                activator.TryRecoverStarterTutorialQuestProgression();

                QuestState? hoverboardStateAfterRecovery = activator.QuestManager.GetQuestState(hoverboardQuestId);
                QuestState? combatStateAfterRecovery = activator.QuestManager.GetQuestState(combatQuestId);

                activator.TeleportTo((ushort)destination.WorldId, teleportPosition.X, teleportPosition.Y, teleportPosition.Z);
                log.LogDebug("Starter tutorial combat projector transported player {PlayerGuid} to combat simulation world location {WorldLocationId}: ({X}, {Y}, {Z}), hoverboardState={HoverboardState}, combatState={CombatState}.",
                    activator.Guid,
                    destinationWorldLocationId,
                    teleportPosition.X,
                    teleportPosition.Y,
                    teleportPosition.Z,
                    hoverboardStateAfterRecovery?.ToString() ?? "None",
                    combatStateAfterRecovery?.ToString() ?? "None");
            }));
        }

        private static bool CanUseCombatProjector(IPlayer activator, ushort hoverboardQuestId, QuestState? hoverboardState, QuestState? combatState, uint projectorObjectiveId, uint rideObjectiveId)
        {
            if (combatState is QuestState.Accepted or QuestState.Achieved or QuestState.Completed)
                return true;

            if (hoverboardState is not (QuestState.Accepted or QuestState.Achieved or QuestState.Completed))
                return false;

            IQuest hoverboardQuest = activator.QuestManager.GetActiveQuests()?.FirstOrDefault(q => q.Id == hoverboardQuestId);
            if (hoverboardQuest == null)
                return false;

            return IsObjectiveComplete(hoverboardQuest, projectorObjectiveId)
                && IsObjectiveComplete(hoverboardQuest, rideObjectiveId);
        }

        private static bool TryGetCombatProjectorContext(Faction faction, out ushort hoverboardQuestId, out ushort combatQuestId, out uint projectorObjectiveId, out uint rideObjectiveId, out uint finishObjectiveId, out uint destinationWorldLocationId)
        {
            switch (faction)
            {
                case Faction.Exile:
                    hoverboardQuestId = ExileHoverboardQuestId;
                    combatQuestId = ExileCombatQuestId;
                    projectorObjectiveId = ExileHoverboardProjectorObjectiveId;
                    rideObjectiveId = ExileHoverboardRideObjectiveId;
                    finishObjectiveId = ExileHoverboardFinishObjectiveId;
                    destinationWorldLocationId = ExileCombatSimulationWorldLocationId;
                    return true;
                case Faction.Dominion:
                    hoverboardQuestId = DominionHoverboardQuestId;
                    combatQuestId = DominionCombatQuestId;
                    projectorObjectiveId = DominionHoverboardProjectorObjectiveId;
                    rideObjectiveId = DominionHoverboardRideObjectiveId;
                    finishObjectiveId = DominionHoverboardFinishObjectiveId;
                    destinationWorldLocationId = DominionCombatSimulationWorldLocationId;
                    return true;
                default:
                    hoverboardQuestId = 0;
                    combatQuestId = 0;
                    projectorObjectiveId = 0u;
                    rideObjectiveId = 0u;
                    finishObjectiveId = 0u;
                    destinationWorldLocationId = 0u;
                    return false;
            }
        }

        private static bool IsObjectiveComplete(IQuest quest, uint objectiveId)
        {
            return quest.Any(objective => objective.ObjectiveInfo.Id == objectiveId && objective.IsComplete());
        }
    }
}
