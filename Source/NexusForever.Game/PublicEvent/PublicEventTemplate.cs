using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model.Shared;

namespace NexusForever.Game.PublicEvent
{
    public class PublicEventTemplate : IPublicEventTemplate
    {
        public PublicEventEntry Entry { get; private set; }
        public Dictionary<uint, PublicEventObjectiveEntry> Objectives { get; private set; }
        public List<PublicEventTeamEntry> Teams { get; } = [];
        public List<PublicEventCustomStatEntry> CustomStats { get; private set; }
        public IReadOnlyList<uint> Locations { get; private set; } = [];
        public IReadOnlyList<uint> ChildEventIds { get; private set; } = [];

        private IReadOnlyDictionary<uint, IReadOnlyList<PublicEventObjectiveStatus.VirtualItem>> objectiveVirtualItems
            = new Dictionary<uint, IReadOnlyList<PublicEventObjectiveStatus.VirtualItem>>();

        #region Dependency Injection

        private readonly IGameTableManager gameTableManager;

        public PublicEventTemplate(
            IGameTableManager  gameTableManager)
        {
            this.gameTableManager = gameTableManager;
        }

        #endregion

        /// <summary>
        /// Initialise <see cref="PublicEventTemplate"/> with <see cref="PublicEventEntry"/>.
        /// </summary>
        public void Initialise(PublicEventEntry entry)
        {
            Entry = entry;
            Locations = entry.WorldLocation2Id == 0u ? [] : [entry.WorldLocation2Id];
            ChildEventIds = gameTableManager.PublicEvent?.Entries?
                .Where(e => e.PublicEventIdParent == entry.Id)
                .Select(e => e.Id)
                .OrderBy(id => id)
                .ToList() ?? [];

            Objectives = gameTableManager.PublicEventObjective.Entries
                .Where(e => e.PublicEventId == entry.Id)
                .ToDictionary(e => e.Id);
            objectiveVirtualItems = BuildObjectiveVirtualItems();

            foreach (Static.PublicEvent.PublicEventTeam team in Objectives.Values
                .Select(o => o.PublicEventTeamId)
                .Distinct())
            {
                PublicEventTeamEntry teamEntry = gameTableManager.PublicEventTeam.GetEntry((uint)team);
                if (teamEntry != null)
                    Teams.Add(teamEntry);
            }

            CustomStats = (gameTableManager.PublicEventCustomStat?.Entries ?? [])
                .Where(e => e.PublicEventId == entry.Id
                    || (e.PublicEventId == 0u && e.PublicEventTypeEnum == entry.PublicEventTypeEnum))
                .GroupBy(e => e.StatIndex)
                .Select(g => g
                    .OrderByDescending(e => e.PublicEventId == entry.Id)
                    .ThenBy(e => e.Id)
                    .First())
                .OrderBy(e => e.StatIndex)
                .ToList();
        }

        public IReadOnlyList<PublicEventObjectiveStatus.VirtualItem> GetObjectiveVirtualItems(PublicEventObjectiveEntry entry)
        {
            if (entry == null)
                return [];

            return objectiveVirtualItems.TryGetValue(entry.Id, out IReadOnlyList<PublicEventObjectiveStatus.VirtualItem> virtualItems)
                ? virtualItems
                : [];
        }

        private IReadOnlyDictionary<uint, IReadOnlyList<PublicEventObjectiveStatus.VirtualItem>> BuildObjectiveVirtualItems()
        {
            if (gameTableManager.PublicEventDepot?.Entries == null
                || gameTableManager.PublicEventVirtualItemDepot?.Entries == null)
                return new Dictionary<uint, IReadOnlyList<PublicEventObjectiveStatus.VirtualItem>>();

            var depotsById = new Dictionary<uint, PublicEventDepotEntry>();
            foreach (PublicEventDepotEntry depot in gameTableManager.PublicEventDepot.Entries)
            {
                if (depot != null && !depotsById.ContainsKey(depot.Id))
                    depotsById.Add(depot.Id, depot);
            }

            var virtualItemDepotByCreature = new Dictionary<uint, List<PublicEventVirtualItemDepotEntry>>();
            foreach (PublicEventVirtualItemDepotEntry depot in gameTableManager.PublicEventVirtualItemDepot.Entries)
            {
                if (depot == null || depot.Creature2Id == 0u)
                    continue;

                if (!virtualItemDepotByCreature.TryGetValue(depot.Creature2Id, out List<PublicEventVirtualItemDepotEntry> depots))
                {
                    depots = [];
                    virtualItemDepotByCreature.Add(depot.Creature2Id, depots);
                }

                depots.Add(depot);
            }

            var virtualItemsByObjective = new Dictionary<uint, IReadOnlyList<PublicEventObjectiveStatus.VirtualItem>>();
            foreach (PublicEventObjectiveEntry objective in Objectives.Values)
            {
                if (objective.PublicEventObjectiveTypeEnum != PublicEventObjectiveType.InteractDepot
                    || objective.ObjectId == 0u
                    || !depotsById.TryGetValue(objective.ObjectId, out PublicEventDepotEntry depot)
                    || !virtualItemDepotByCreature.TryGetValue(depot.Creature2Id, out List<PublicEventVirtualItemDepotEntry> virtualItemDepots))
                    continue;

                List<PublicEventObjectiveStatus.VirtualItem> virtualItems = BuildVirtualItems(objective, virtualItemDepots);
                if (virtualItems.Count != 0)
                    virtualItemsByObjective.Add(objective.Id, virtualItems);
            }

            return virtualItemsByObjective;
        }

        private static List<PublicEventObjectiveStatus.VirtualItem> BuildVirtualItems(
            PublicEventObjectiveEntry objective,
            IEnumerable<PublicEventVirtualItemDepotEntry> virtualItemDepots)
        {
            var virtualItems = new List<PublicEventObjectiveStatus.VirtualItem>();
            var seenVirtualItems = new HashSet<uint>();

            foreach (uint virtualItemId in virtualItemDepots
                .OrderBy(depot => depot.Id)
                .SelectMany(GetVirtualItemIds))
            {
                if (virtualItemId == 0u || !seenVirtualItems.Add(virtualItemId))
                    continue;

                virtualItems.Add(new PublicEventObjectiveStatus.VirtualItem
                {
                    ItemId = virtualItemId,
                    Count  = objective.Count
                });
            }

            return virtualItems;
        }

        private static IEnumerable<uint> GetVirtualItemIds(PublicEventVirtualItemDepotEntry entry)
        {
            yield return entry.VirtualItemId00;
            yield return entry.VirtualItemId01;
            yield return entry.VirtualItemId02;
            yield return entry.VirtualItemId03;
            yield return entry.VirtualItemId04;
            yield return entry.VirtualItemId05;
        }

        /// <summary>
        /// Returns if event has live stats.
        /// </summary>
        /// <remarks>
        /// This determines if live stats should be periodically sent to members of the event.
        /// </remarks>
        public bool HasLiveStats()
        {
            return Entry.PublicEventTypeEnum
                is Static.PublicEvent.PublicEventType.Warplot
                or Static.PublicEvent.PublicEventType.BattlegroundVortex
                or Static.PublicEvent.PublicEventType.BattlegroundHoldTheLine
                or Static.PublicEvent.PublicEventType.BattlegroundCannon
                or Static.PublicEvent.PublicEventType.BattlegroundSabotage
                or Static.PublicEvent.PublicEventType.Arena;
        }
    }
}
