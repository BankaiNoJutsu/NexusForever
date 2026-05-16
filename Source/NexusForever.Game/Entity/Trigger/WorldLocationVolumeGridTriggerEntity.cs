using System.Linq;
using System.Numerics;
using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Trigger;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Script;

namespace NexusForever.Game.Entity.Trigger
{
    public class WorldLocationVolumeGridTriggerEntity : VolumeGridTriggerEntity, IWorldLocationVolumeGridTriggerEntity
    {
        private static readonly uint[] tutorialWorldLocationIds = [51735u, 51736u, 51737u, 51703u, 51734u];

        public WorldLocation2Entry Entry { get; private set; }

        #region Dependency Injection

        private readonly IGameTableManager gameTableManager;
        private readonly ILogger<WorldLocationVolumeGridTriggerEntity> log;

        public WorldLocationVolumeGridTriggerEntity(
            IScriptManager scriptManager,
            IGameTableManager gameTableManager,
            ILogger<WorldLocationVolumeGridTriggerEntity> log)
            : base(scriptManager)
        {
            this.gameTableManager = gameTableManager;
            this.log              = log;
        }

        #endregion

        /// <summary>
        /// Initialise trigger with supplied world location id and objective object id.
        /// </summary>
        public void Initialise(uint worldLocationId, uint objectId)
        {
            if (Entry != null)
                throw new InvalidOperationException("World location volume trigger is already initialised.");

            Entry = gameTableManager.WorldLocation2.GetEntry(worldLocationId)
                ?? throw new InvalidOperationException($"Unknown WorldLocation2 entry {worldLocationId}.");

            Initialise(worldLocationId, Entry.Radius, objectId);
        }

        protected override void AddToRange(IGridEntity entity)
        {
            base.AddToRange(entity);

            if (entity is not IPlayer player)
                return;

            foreach (IQuest quest in player.QuestManager.GetActiveQuests())
            {
                List<IQuestObjective> matchingObjectives = GetObjectivesToUpdate(quest).ToList();
                foreach (IQuestObjective objective in matchingObjectives)
                {
                    if (Entry != null && tutorialWorldLocationIds.Contains(Entry.Id))
                    {
                        log.LogDebug("Tutorial trigger advanced player {PlayerGuid}: world location {WorldLocationId}, quest {QuestId}, objective {ObjectiveId}, trigger position ({TriggerX}, {TriggerY}, {TriggerZ}), player position ({PlayerX}, {PlayerY}, {PlayerZ}).",
                            player.Guid, Entry.Id, quest.Id, objective.ObjectiveInfo.Id,
                            Position.X, Position.Y, Position.Z,
                            player.Position.X, player.Position.Y, player.Position.Z);
                    }

                    quest.ObjectiveUpdate(objective.ObjectiveInfo.Id, 1u);
                }
            }
        }

        protected override bool IsInRange(IGridEntity target)
        {
            if (Entry == null)
                return base.IsInRange(target);

            float targetRadius = target is IUnitEntity unit
                ? unit.HitRadius * 0.5f
                : 0f;

            // WorldLocation2 markers use a horizontal radius with a separate vertical clamp.
            float horizontalDistanceSquared = Vector2.DistanceSquared(
                new Vector2(Position.X, Position.Z),
                new Vector2(target.Position.X, target.Position.Z));

            float horizontalRange = Entry.Radius + targetRadius;
            if (horizontalDistanceSquared > horizontalRange * horizontalRange)
                return false;

            return Entry.MaxVerticalDistance <= 0f
                || MathF.Abs(Position.Y - target.Position.Y) <= Entry.MaxVerticalDistance;
        }

        private bool MatchesWorldLocation(IQuestObjective objective)
        {
            if (objective.ObjectiveInfo.Type != QuestObjectiveType.EnterArea)
                return false;

            QuestObjectiveEntry objectiveEntry = objective.ObjectiveInfo.Entry;
            return objectiveEntry.WorldLocationsIdIndicator00 == Entry.Id
                || objectiveEntry.WorldLocationsIdIndicator01 == Entry.Id
                || objectiveEntry.WorldLocationsIdIndicator02 == Entry.Id
                || objectiveEntry.WorldLocationsIdIndicator03 == Entry.Id;
        }

        private IEnumerable<IQuestObjective> GetObjectivesToUpdate(IQuest quest)
        {
            List<IQuestObjective> matchingObjectives = quest
                .Where(MatchesWorldLocation)
                .Where(o => !o.IsComplete())
                .ToList();

            if (matchingObjectives.Count == 0)
                return [];

            IEnumerable<IQuestObjective> objectivesToUpdate = matchingObjectives.Any(o => !o.ObjectiveInfo.IsOptional())
                ? matchingObjectives.Where(o => !o.ObjectiveInfo.IsOptional())
                : matchingObjectives;

            // Process matching objectives in reverse order so a single trigger entry
            // cannot advance multiple sequential steps that share the same location.
            return objectivesToUpdate.OrderByDescending(o => o.Index).ToList();
        }
    }
}
