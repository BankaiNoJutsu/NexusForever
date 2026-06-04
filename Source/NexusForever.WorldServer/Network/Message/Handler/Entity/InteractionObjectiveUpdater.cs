using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Shared.Game.Events;
using NLog;

namespace NexusForever.WorldServer.Network.Message.Handler.Entity
{
    internal static class InteractionObjectiveUpdater
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();

        private const uint TutorialCombatProjectorCreatureId = 73735u;
        private const uint TutorialCombatMineEasyCreatureId = 73463u;
        private const uint TutorialCombatMineMediumCreatureId = 73667u;
        private const uint TutorialCombatMineHardCreatureId = 73668u;
        private const ushort ExileCombatQuestId = 10518;
        private const ushort DominionCombatQuestId = 10524;
        private const uint ShellshockDominionCannonCreatureId = 11251u;
        private static readonly TimeSpan tutorialCombatProjectorRecoveryDelay = TimeSpan.FromMilliseconds(9000d);
        private static readonly HashSet<uint> tutorialCombatMineCreatureIds =
        [
            TutorialCombatMineEasyCreatureId,
            TutorialCombatMineMediumCreatureId,
            TutorialCombatMineHardCreatureId
        ];
        // Q3487 cannon checklist credit is script-owned; avoid generic activate-cast SucceedCSI/TalkTo attempts.
        private static readonly HashSet<uint> scriptHandledActivateCastCreatureIds =
        [
            ShellshockDominionCannonCreatureId
        ];

        private static readonly Dictionary<(ushort QuestId, uint CreatureId), TutorialMineObjectiveCredit> tutorialCombatMineObjectiveCredits = new()
        {
            [(ExileCombatQuestId, TutorialCombatMineEasyCreatureId)]     = new TutorialMineObjectiveCredit(21287u, 51662u),
            [(ExileCombatQuestId, TutorialCombatMineMediumCreatureId)]   = new TutorialMineObjectiveCredit(21340u, 51663u),
            [(ExileCombatQuestId, TutorialCombatMineHardCreatureId)]     = new TutorialMineObjectiveCredit(21318u, 51664u),
            [(DominionCombatQuestId, TutorialCombatMineEasyCreatureId)]  = new TutorialMineObjectiveCredit(21313u, 52899u),
            [(DominionCombatQuestId, TutorialCombatMineMediumCreatureId)] = new TutorialMineObjectiveCredit(21314u, 52900u),
            [(DominionCombatQuestId, TutorialCombatMineHardCreatureId)]  = new TutorialMineObjectiveCredit(21315u, 52901u)
        };

        public static void UpdateDirectInteractionObjectives(IPlayer player, IWorldEntity entity, IAssetManager assetManager)
        {
            UpdateObjectives(player, entity, assetManager, includeActivateEntity: true);
        }

        public static void UpdateActivateSuccessObjectives(IPlayer player, IWorldEntity entity, IAssetManager assetManager, bool includeActivateEntity)
        {
            UpdateObjectives(player, entity, assetManager, includeActivateEntity);
        }

        private static void UpdateObjectives(IPlayer player, IWorldEntity entity, IAssetManager assetManager, bool includeActivateEntity)
        {
            if (entity == null)
                return;

            player.RecordStarterTutorialDepartureTerminal(entity.CreatureId);

            bool handledStarterTutorialCombatMine = TryUpdateStarterTutorialCombatMineObjective(player, entity);
            bool suppressGenericActivateCastObjectives = !includeActivateEntity && scriptHandledActivateCastCreatureIds.Contains(entity.CreatureId);

            if (!suppressGenericActivateCastObjectives)
            {
                if (includeActivateEntity && !handledStarterTutorialCombatMine)
                    player.QuestManager.ObjectiveUpdate(QuestObjectiveType.ActivateEntity, entity.CreatureId, 1u);

                player.QuestManager.ObjectiveUpdate(QuestObjectiveType.SucceedCSI, entity.CreatureId, 1u);
                player.QuestManager.ObjectiveUpdate(QuestObjectiveType.TalkTo, entity.CreatureId, 1u);

                player.QuestManager.ObjectiveUpdate(QuestObjectiveType.TalkToTargetGroup, entity.CreatureId, 1u);
                player.QuestManager.ObjectiveUpdate(QuestObjectiveType.ActivateTargetGroup, entity.CreatureId, 1u);
                player.QuestManager.ObjectiveUpdate(QuestObjectiveType.ActivateTargetGroupChecklist, entity.CreatureId, entity.QuestChecklistIdx);
                player.QuestManager.ObjectiveUpdate(QuestObjectiveType.GatheResource, entity.CreatureId, 1u);
            }

            player.SyncStarterTutorialEntityVisibility();
            player.TryRecoverStarterTutorialQuestProgression();
            TryQueueStarterTutorialCombatProjectorRecovery(player, entity);
        }

        private static bool TryUpdateStarterTutorialCombatMineObjective(IPlayer player, IWorldEntity entity)
        {
            if (!tutorialCombatMineCreatureIds.Contains(entity.CreatureId))
                return false;

            bool hasActiveCombatQuest = false;
            foreach (IQuest quest in player.QuestManager.GetActiveQuests())
            {
                if (quest.Id is not (ExileCombatQuestId or DominionCombatQuestId))
                    continue;

                hasActiveCombatQuest = true;
                if (!tutorialCombatMineObjectiveCredits.TryGetValue((quest.Id, entity.CreatureId), out TutorialMineObjectiveCredit credit))
                    continue;

                if (!IsNearWorldLocation(entity.Position, credit.WorldLocation2Id))
                {
                    log.Debug("Skipped Rider's Reef combat mine objective credit: player={PlayerGuid}, quest={QuestId}, entity={EntityGuid}, creature={CreatureId}, expectedWorldLocation={WorldLocationId}, position=({X}, {Y}, {Z}).",
                        player.Guid,
                        quest.Id,
                        entity.Guid,
                        entity.CreatureId,
                        credit.WorldLocation2Id,
                        entity.Position.X,
                        entity.Position.Y,
                        entity.Position.Z);
                    continue;
                }

                IQuestObjective objective = quest.FirstOrDefault(o => o.ObjectiveInfo.Id == credit.ObjectiveId);
                if (objective == null || objective.IsComplete())
                    return true;

                quest.ObjectiveUpdate(credit.ObjectiveId, 1u);
                log.Debug("Credited Rider's Reef combat mine objective: player={PlayerGuid}, quest={QuestId}, objective={ObjectiveId}, entity={EntityGuid}, creature={CreatureId}, worldLocation={WorldLocationId}.",
                    player.Guid,
                    quest.Id,
                    credit.ObjectiveId,
                    entity.Guid,
                    entity.CreatureId,
                    credit.WorldLocation2Id);
                return true;
            }

            return hasActiveCombatQuest;
        }

        private static bool IsNearWorldLocation(Vector3 position, uint worldLocationId)
        {
            WorldLocation2Entry worldLocation = GameTableManager.Instance.WorldLocation2.GetEntry(worldLocationId);
            if (worldLocation == null)
                return false;

            float horizontalRange = MathF.Max(worldLocation.Radius, 1f) + 6f;
            float horizontalDistanceSquared = Vector2.DistanceSquared(
                new Vector2(position.X, position.Z),
                new Vector2(worldLocation.Position0, worldLocation.Position2));

            return horizontalDistanceSquared <= horizontalRange * horizontalRange;
        }

        private static void TryQueueStarterTutorialCombatProjectorRecovery(IPlayer player, IWorldEntity entity)
        {
            if (entity.CreatureId != TutorialCombatProjectorCreatureId)
                return;

            player.Session.Events.EnqueueEvent(new DelayEvent(tutorialCombatProjectorRecoveryDelay, () =>
            {
                player.TryRecoverStarterTutorialCombatProjectorActivation();
            }));
        }

        private readonly record struct TutorialMineObjectiveCredit(uint ObjectiveId, uint WorldLocation2Id);
    }
}
