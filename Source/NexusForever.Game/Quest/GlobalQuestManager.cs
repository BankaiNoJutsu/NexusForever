using System.Collections.Immutable;
using System.Diagnostics;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Shared;
using NLog;

namespace NexusForever.Game.Quest
{
    public sealed class GlobalQuestManager : IGlobalQuestManager
    {
        private const string Quest2TableName = "Quest2.tbl";
        private const string Creature2TableName = "Creature2.tbl";
        private const string CommunicatorMessagesTableName = "CommunicatorMessages.tbl";

        private static readonly ILogger log = LogManager.GetCurrentClassLogger();
        private static readonly IReadOnlyDictionary<ushort, uint[]> questGiverCreatureOverrides = new Dictionary<ushort, uint[]>
        {
            // Build 16042 Creature2 50668 omits Q3781, while reviewed DataMapping starter relation 75 maps the Dead Exile Soldier.
            [3781] = [50668u],
            // Build 16042 has no CommunicatorMessages row for Q3797; reviewed DataMapping starter relation 166 maps Commander Durek.
            [3797] = [11061u]
        };

        private static readonly IReadOnlyDictionary<ushort, uint[]> questReceiverCreatureOverrides = new Dictionary<ushort, uint[]>
        {
            // Build 16042 Quest2 routes Q3670 through Deadeye Brightland at receiver WL 29526 and alt receiver WL 12354.
            // Creature2 omits Q3670 from QuestIdReceive, while reviewed DataMapping finisher relations 38 and 1492 map these creature ids.
            [3670] = [11063u, 16622u],
            // Build 16042 Creature2 12959 is the Camp Icefury Deadeye Brightland receiver for Q3671.
            // Creature2 11063 also lists Q3671, but that is the major-hub/landing-site Deadeye and is suppressed below for this quest.
            [3671] = [12959u],
            // Build 16042 Quest2 alt receiver WL 12354 and reviewed DataMapping finisher relation 1841 map Q3781 to Galeras Deadeye Brightland.
            [3781] = [16622u],
            // Build 16042 Quest2 receiver WL 7727 and reviewed DataMapping finisher relation 1693 map Q3797 to Commander Durek.
            [3797] = [11061u],
            // Build 16042 Quest2 routes Q5580/Q5583 through receiver WL 17902. Creature2 24158 carries Q5594 directly,
            // but omits these two receiver slots while reviewed DataMapping finisher relations 1559/1560 map them to Kezrek Warbringer.
            [5580] = [24158u],
            [5583] = [24158u]
        };

        private static readonly IReadOnlyDictionary<ushort, uint[]> questReceiverCreatureSuppressions = new Dictionary<ushort, uint[]>
        {
            // Q3671 completes at Camp Icefury Deadeye 12959 only; do not allow the outside-camp Deadeye 11063.
            [3671] = [11063u]
        };

        /// <summary>
        /// <see cref="DateTime"/> representing the next daily reset.
        /// </summary>
        public DateTime NextDailyReset { get; private set; }

        /// <summary>
        /// <see cref="DateTime"/> representing the next weekly reset.
        /// </summary>
        public DateTime NextWeeklyReset { get; private set; }

        private ImmutableDictionary<ushort, IQuestInfo> questInfoStore;
        private ImmutableDictionary<ushort, ImmutableList<uint>> questGiverStore;
        private ImmutableDictionary<ushort, ImmutableList<uint>> questReceiverStore;

        private ImmutableDictionary<uint, ICommunicatorMessage> communicatorStore;
        private ImmutableDictionary<ushort, ImmutableList<ICommunicatorMessage>> communicatorQuestStore;
        private ImmutableDictionary<(ushort /*questId*/, QuestState), ImmutableList<ICommunicatorMessage>> communicatorQuestStateTriggerStore;
        private readonly IPrerequisiteManager prerequisiteManager;
        private readonly IGameTableManager gameTableManager;

        public GlobalQuestManager(
            IPrerequisiteManager prerequisiteManager = null,
            IGameTableManager gameTableManager = null)
        {
            this.prerequisiteManager = prerequisiteManager;
            this.gameTableManager = gameTableManager;
        }

        public void Initialise()
        {
            Stopwatch sw = Stopwatch.StartNew();

            CalculateResetTimes(); 
            InitialiseQuestInfo();
            InitialiseQuestRelations();

            InitialiseCommunicatorEntries();
            InitialiseCommunicatorQuests();
            InitialiseCommunicatorQuestStateTriggers();

            log.Info($"Cached {questInfoStore.Count} quests in {sw.ElapsedMilliseconds}ms.");
        }

        private void CalculateResetTimes()
        {
            DateTime now = DateTime.UtcNow;
            var resetTime = new DateTime(now.Year, now.Month, now.Day, 10, 0, 0);

            // calculate daily reset (every day 10AM UTC)
            NextDailyReset = resetTime.AddDays(1);

            // calculate weekly reset (every tuesday 10AM UTC)
            NextWeeklyReset = resetTime.AddDays((DayOfWeek.Tuesday - now.DayOfWeek + 7) % 7);
        }

        private void InitialiseQuestInfo()
        {
            var builder = ImmutableDictionary.CreateBuilder<ushort, IQuestInfo>();
            if (gameTableManager.Quest2?.Entries == null)
                MissingGameDataDiagnostics.ReportMissingTable(
                    Quest2TableName,
                    nameof(GlobalQuestManager) + "." + nameof(InitialiseQuestInfo),
                    MissingGameDataSeverity.PlayerImpacting,
                    "Cannot cache quest definitions.");

            IEnumerable<Quest2Entry> questEntries =
                gameTableManager.Quest2?.Entries ?? Enumerable.Empty<Quest2Entry>();
            foreach (Quest2Entry entry in questEntries)
                builder.Add((ushort)entry.Id, new QuestInfo(entry, this, gameTableManager));

            questInfoStore = builder.ToImmutable();
        }

        private void InitialiseQuestRelations()
        {
            var questGivers = new Dictionary<ushort, List<uint>>();
            var questReceivers = new Dictionary<ushort, List<uint>>();
            var availableCreatureIds = new HashSet<uint>();

            if (gameTableManager.Creature2?.Entries == null)
                MissingGameDataDiagnostics.ReportMissingTable(
                    Creature2TableName,
                    nameof(GlobalQuestManager) + "." + nameof(InitialiseQuestRelations),
                    MissingGameDataSeverity.PlayerImpacting,
                    "Cannot cache quest giver and receiver relations.");

            IEnumerable<Creature2Entry> creatureEntries =
                gameTableManager.Creature2?.Entries ?? Enumerable.Empty<Creature2Entry>();
            foreach (Creature2Entry entry in creatureEntries)
            {
                availableCreatureIds.Add(entry.Id);

                // ReSharper disable once PossibleInvalidCastExceptionInForeachLoop
                foreach (ushort questId in (entry.QuestIdGiven ?? []).Where(q => q != 0u))
                    AddQuestCreatureRelation(questGivers, questId, entry.Id);

                // ReSharper disable once PossibleInvalidCastExceptionInForeachLoop
                foreach (ushort questId in (entry.QuestIdReceive ?? []).Where(q => q != 0u))
                    AddQuestCreatureRelation(questReceivers, questId, entry.Id);
            }

            foreach (KeyValuePair<ushort, uint[]> pair in questGiverCreatureOverrides)
                foreach (uint creatureId in pair.Value)
                {
                    if (!availableCreatureIds.Contains(creatureId))
                        continue;

                    AddQuestCreatureRelation(questGivers, pair.Key, creatureId);
                }

            foreach (KeyValuePair<ushort, uint[]> pair in questReceiverCreatureOverrides)
                foreach (uint creatureId in pair.Value)
                {
                    if (!availableCreatureIds.Contains(creatureId))
                        continue;

                    AddQuestCreatureRelation(questReceivers, pair.Key, creatureId);
                }

            foreach (KeyValuePair<ushort, uint[]> pair in questReceiverCreatureSuppressions)
                foreach (uint creatureId in pair.Value)
                    RemoveQuestCreatureRelation(questReceivers, pair.Key, creatureId);

            questGiverStore = questGivers.ToImmutableDictionary(k => k.Key, v => v.Value.ToImmutableList());
            questReceiverStore = questReceivers.ToImmutableDictionary(k => k.Key, v => v.Value.ToImmutableList());
        }

        private static void AddQuestCreatureRelation(Dictionary<ushort, List<uint>> store, ushort questId, uint creatureId)
        {
            if (!store.TryGetValue(questId, out List<uint> creatureIds))
            {
                creatureIds = [];
                store.Add(questId, creatureIds);
            }

            if (!creatureIds.Contains(creatureId))
                creatureIds.Add(creatureId);
        }

        private static void RemoveQuestCreatureRelation(Dictionary<ushort, List<uint>> store, ushort questId, uint creatureId)
        {
            if (!store.TryGetValue(questId, out List<uint> creatureIds))
                return;

            creatureIds.Remove(creatureId);
            if (creatureIds.Count == 0)
                store.Remove(questId);
        }

        private void InitialiseCommunicatorEntries()
        {
            var builder = ImmutableDictionary.CreateBuilder<uint, ICommunicatorMessage>();
            if (gameTableManager.CommunicatorMessages?.Entries == null)
                MissingGameDataDiagnostics.ReportMissingTable(
                    CommunicatorMessagesTableName,
                    nameof(GlobalQuestManager) + "." + nameof(InitialiseCommunicatorEntries),
                    MissingGameDataSeverity.PlayerImpacting,
                    "Cannot cache communicator messages.");

            IEnumerable<CommunicatorMessagesEntry> communicatorEntries =
                gameTableManager.CommunicatorMessages?.Entries ?? Enumerable.Empty<CommunicatorMessagesEntry>();
            foreach (CommunicatorMessagesEntry entry in communicatorEntries)
            {
                var communicator = new CommunicatorMessage(entry, prerequisiteManager, gameTableManager);
                builder.Add(communicator.Id, communicator);
            }

            communicatorStore = builder.ToImmutable();
        }

        private void InitialiseCommunicatorQuests()
        {
            var builder = new Dictionary<ushort, List<ICommunicatorMessage>>();
            IEnumerable<CommunicatorMessagesEntry> communicatorEntries =
                gameTableManager.CommunicatorMessages?.Entries ?? Enumerable.Empty<CommunicatorMessagesEntry>();
            foreach (CommunicatorMessagesEntry entry in communicatorEntries
                .Where(e => e.QuestIdDelivered != 0u))
            {
                if (!communicatorStore.TryGetValue(entry.Id, out ICommunicatorMessage communicator))
                    continue;

                if (!builder.ContainsKey(communicator.QuestId))
                    builder.Add(communicator.QuestId, new List<ICommunicatorMessage>());

                builder[communicator.QuestId].Add(communicator);
            }

            communicatorQuestStore = builder.ToImmutableDictionary(e => e.Key, e => e.Value.ToImmutableList());
        }

        private void InitialiseCommunicatorQuestStateTriggers()
        {
            var builder = new Dictionary<(ushort, QuestState), List<ICommunicatorMessage>>();
            IEnumerable<CommunicatorMessagesEntry> communicatorEntries =
                gameTableManager.CommunicatorMessages?.Entries ?? Enumerable.Empty<CommunicatorMessagesEntry>();
            foreach (CommunicatorMessagesEntry entry in communicatorEntries)
            {
                if (!communicatorStore.TryGetValue(entry.Id, out ICommunicatorMessage communicator))
                    continue;

                foreach ((ushort QuestId, uint QuestState) p in
                    (entry.Quests ?? []).Zip(entry.States ?? [], (a, b) => ((ushort)a, b)))
                {
                    if (p.QuestId == 0)
                        continue;

                    if (entry.QuestIdDelivered != 0u && p.QuestId == entry.QuestIdDelivered)
                        continue;

                    if (!CommunicatorQuestState.TryGetServerQuestState(p.QuestState, out QuestState questState))
                        continue;

                    var key = (p.QuestId, questState);
                    if (!builder.ContainsKey(key))
                        builder.Add(key, new List<ICommunicatorMessage>());

                    builder[key].Add(communicator);
                }
            }

            communicatorQuestStateTriggerStore = builder.ToImmutableDictionary(e => e.Key, e => e.Value.ToImmutableList());
        }

        public void Update(double lastTick)
        {
            DateTime now = DateTime.UtcNow;
            if (NextDailyReset <= now)
                NextDailyReset = NextDailyReset.AddDays(1);

            if (NextWeeklyReset <= now)
                NextWeeklyReset = NextWeeklyReset.AddDays(7);
        }

        /// <summary>
        /// Return <see cref="IQuestInfo"/> for supplied quest.
        /// </summary>
        public IQuestInfo GetQuestInfo(ushort questId)
        {
            return questInfoStore.TryGetValue(questId, out IQuestInfo questInfo) ? questInfo : null;
        }

        /// <summary>
        /// Return a collection of creatures that start the supplied quest.
        /// </summary>
        public IEnumerable<uint> GetQuestGivers(ushort questId)
        {
            return questGiverStore.TryGetValue(questId, out ImmutableList<uint> creatureIds) ? creatureIds : Enumerable.Empty<uint>();
        }

        /// <summary>
        /// Return a collection of creatures that finish the supplied quest.
        /// </summary>
        public IEnumerable<uint> GetQuestReceivers(ushort questId)
        {
            return questReceiverStore.TryGetValue(questId, out ImmutableList<uint> creatureIds) ? creatureIds : Enumerable.Empty<uint>();
        }

        /// <summary>
        /// Return a collection of <see cref="ICommunicatorMessage"/>'s that start the supplied quest.
        /// </summary>
        public IEnumerable<ICommunicatorMessage> GetQuestCommunicatorMessages(ushort questId)
        {
            return communicatorQuestStore.TryGetValue(questId, out ImmutableList<ICommunicatorMessage> creatureIds)
                ? creatureIds : Enumerable.Empty<ICommunicatorMessage>();
        }

        /// <summary>
        /// Return <see cref="ICommunicatorMessage"/> by id.
        /// </summary>
        public ICommunicatorMessage GetCommunicatorMessage<T>(T communicatorMessageId) where T : Enum
        {
            return GetCommunicatorMessage(communicatorMessageId.As<T, uint>());
        }

        /// <summary>
        /// Return <see cref="ICommunicatorMessage"/> by id.
        /// </summary>
        public ICommunicatorMessage GetCommunicatorMessage(uint communicatorMessageId)
        {
            return communicatorStore.TryGetValue(communicatorMessageId, out ICommunicatorMessage communicatorMessage)
                ? communicatorMessage : null;
        }

        /// <summary>
        /// Return a collection of <see cref="ICommunicatorMessage"/>'s that are triggered when a quest hits a certain state.
        /// </summary>
        public IEnumerable<ICommunicatorMessage> GetQuestCommunicatorQuestStateTriggers(ushort questId, QuestState state)
        {
            return communicatorQuestStateTriggerStore.TryGetValue((questId, state), out ImmutableList<ICommunicatorMessage> triggers)
                ? triggers : Enumerable.Empty<ICommunicatorMessage>();
        }
    }
}
