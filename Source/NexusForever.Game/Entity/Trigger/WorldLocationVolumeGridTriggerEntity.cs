using System.Linq;
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
        public WorldLocation2Entry Entry { get; private set; }

        #region Dependency Injection

        private readonly IGameTableManager gameTableManager;

        public WorldLocationVolumeGridTriggerEntity(
            IScriptManager scriptManager,
            IGameTableManager gameTableManager)
            : base(scriptManager)
        {
            this.gameTableManager = gameTableManager;
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
                // Process matching objectives in reverse order so a single trigger entry
                // cannot advance multiple sequential steps that share the same location.
                foreach (IQuestObjective objective in quest
                    .Where(MatchesWorldLocation)
                    .OrderByDescending(o => o.Index))
                {
                    quest.ObjectiveUpdate(objective.ObjectiveInfo.Id, 1u);
                }
            }
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
    }
}
