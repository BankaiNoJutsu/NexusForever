using System.Collections;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Quest;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model;
using NexusForever.Script;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Collection;
using NexusForever.Shared;
using NexusForever.Shared.Game;
using NLog;

namespace NexusForever.Game.Quest
{
    public partial class Quest : IQuest
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();

        private static readonly HashSet<ushort> guidanceDiagnosticQuestIds =
        [
            5573,
            5575,
            5868,
            6302,
            6677,
            6986,
            10513,
            10518,
            10521,
            10527,
            10524,
            10532
        ];

        private enum ObjectiveWorldLocationResolutionSource
        {
            InactiveQuest,
            InactiveObjective,
            QuestDirection,
            EnterAreaDirectionEntry,
            EnterAreaDirection,
            Indicator,
            Unresolved
        }

        private readonly record struct ObjectiveWorldLocationResolution(uint WorldLocationId, ObjectiveWorldLocationResolutionSource Source);

        [Flags]
        private enum QuestSaveMask
        {
            None   = 0x00,
            Create = 0x01,
            State  = 0x02,
            Flags  = 0x04,
            Reset  = 0x08,
            Delete = 0x10,
            Timer  = 0x20,
            ContractAcceptance = 0x40
        }

        private const QuestStateFlags ObjectiveCompletionFlagsMask =
            QuestStateFlags.Objective0Complete |
            QuestStateFlags.Objective1Complete |
            QuestStateFlags.Objective2Complete |
            QuestStateFlags.Objective3Complete |
            QuestStateFlags.Objective4Complete |
            QuestStateFlags.Objective5Complete;

        public ushort Id => (ushort)Info.Entry.Id;
        public IQuestInfo Info { get; }
        public IPlayer Player => player;

        public QuestState State
        {
            get => state;
            set
            {
                QuestState oldState = state;
                bool created = !stateSet;
                lock (persistence.Gate) { state = value; stateSet = true; persistence.Dirty((int)QuestSaveMask.State); }
                OnStateChange(oldState, created);
                player.RequestSave();
            }
        }

        private QuestState state;
        private bool stateSet;

        public QuestStateFlags Flags
        {
            get => flags;
            set
            {
                lock (persistence.Gate) { flags = value; persistence.Dirty((int)QuestSaveMask.Flags); }
                player.RequestSave();
            }
        }

        private QuestStateFlags flags;

        public uint? Timer
        {
            get => timer;
            set
            {
                lock (persistence.Gate) { timer = value; persistence.Dirty((int)QuestSaveMask.Timer); }
            }
        }

        private uint? timer;

        public DateTime? Reset
        {
            get => reset;
            set
            {
                lock (persistence.Gate) { reset = value; persistence.Dirty((int)QuestSaveMask.Reset); }
                player.RequestSave();
            }
        }

        private DateTime? reset;

        /// <summary>
        /// Returns if <see cref="IQuest"/> is enqueued to be saved to the database.
        /// </summary>
        public bool PendingCreate => persistence.Has((int)QuestSaveMask.Create) && !persistence.WasAttempted;

        /// <summary>
        /// Returns if <see cref="IQuest"/> is enqueued to be deleted from the database.
        /// </summary>
        public bool PendingDelete { get { lock (persistence.Gate) return deleteRequested; } }

        private readonly IPlayer player;
        private readonly IAssetManager assetManager;
        private readonly IGlobalQuestManager globalQuestManager;
        private readonly IScriptManager scriptManager;
        private readonly IGameTableManager gameTableManager;
        private readonly List<IQuestObjective> objectives = new();

        private uint currentObjectiveId;

        private UpdateTimer questTimer;

        private IScriptCollection scriptCollection;

        /// <summary>
        /// Create a new <see cref="IQuest"/> from an existing database model.
        /// </summary>
        public Quest(
            IPlayer owner,
            IQuestInfo info,
            CharacterQuestModel model,
            IAssetManager assetManager = null,
            IGlobalQuestManager globalQuestManager = null,
            IScriptManager scriptManager = null,
            IGameTableManager gameTableManager = null)
        {
            player            = owner;
            this.assetManager = assetManager;
            this.globalQuestManager = globalQuestManager;
            this.scriptManager = scriptManager;
            this.gameTableManager = gameTableManager;
            Info              = info;
            state  = (QuestState)model.State;
            stateSet = true;
            flags  = (QuestStateFlags)model.Flags;
            timer  = model.Timer;
            reset  = model.Reset;
            contractAcceptanceId = model.ContractAcceptanceId;
            if (contractAcceptanceId != null && (!Info.IsContract() || !ValidContractAcceptanceId(contractAcceptanceId)))
                throw new InvalidOperationException("Stored Contract acceptance identity is invalid.");
            InitialisePersistence(model);

            if (timer != null)
                questTimer = new UpdateTimer(timer.Value / 1000d);

            foreach (CharacterQuestObjectiveModel objectiveModel in model.QuestObjective.OrderBy(objective => objective.Index))
                objectives.Add(new QuestObjective(player, info, info.Objectives[objectiveModel.Index], objectiveModel, assetManager));

            SyncObjectiveCompletionFlags(true);

            currentObjectiveId = GetCurrentObjectiveId();
            scriptCollection = GetScriptManager().InitialiseOwnedScripts<IQuest>(this, info.Entry.Id);
        }

        /// <summary>
        /// Create a new <see cref="IQuest"/> from supplied <see cref="IQuestInfo"/>.
        /// </summary>
        public Quest(
            IPlayer owner,
            IQuestInfo info,
            IAssetManager assetManager = null,
            IGlobalQuestManager globalQuestManager = null,
            IScriptManager scriptManager = null,
            IGameTableManager gameTableManager = null)
        {
            player            = owner;
            this.assetManager = assetManager;
            this.globalQuestManager = globalQuestManager;
            this.scriptManager = scriptManager;
            this.gameTableManager = gameTableManager;
            Info              = info;
            state  = QuestState.Accepted;
            InitialisePersistence(null);

            for (byte i = 0; i < info.Objectives.Count; i++)
                objectives.Add(new QuestObjective(player, info, info.Objectives[i], i, assetManager));

            if (objectives.Count == 0)
                state = QuestState.Achieved;

            SyncObjectiveCompletionFlags(false);

            persistence.Dirty((int)QuestSaveMask.Create);

            currentObjectiveId = GetCurrentObjectiveId();
            scriptCollection = GetScriptManager().InitialiseOwnedScripts<IQuest>(this, info.Entry.Id);
        }

        public void Dispose()
        {
            if (scriptCollection != null)
                GetScriptManager().Unload(scriptCollection);

            scriptCollection = null;
        }

        public void InitialiseTimer()
        {
            if (Info.Entry.MaxTimeAllowedMS != 0u)
            {
                questTimer = new UpdateTimer(Info.Entry.MaxTimeAllowedMS / 1000d);
                Timer = (uint)(questTimer.Time * 1000d);
            }

        }

        public uint GetCurrentObjectiveId()
        {
            if (State != QuestState.Accepted)
                return 0u;

            IQuestObjective objective = objectives
                .Where(o => !o.IsComplete() && !o.ObjectiveInfo.IsOptional() && CanUpdateObjective(o))
                .OrderBy(o => o.Index)
                .FirstOrDefault();

            if (objective != null)
                return objective.ObjectiveInfo.Id;

            objective = objectives
                .Where(o => !o.IsComplete() && CanUpdateObjective(o))
                .OrderBy(o => o.Index)
                .FirstOrDefault();

            return objective?.ObjectiveInfo.Id ?? 0u;
        }

        public void Save(CharacterContext context) => SavePersistence(context);

        public void Update(double lastTick)
        {
            scriptCollection?.Invoke<IUpdate>(s => s.Update(lastTick));

            if (questTimer != null)
            {
                questTimer.Update(lastTick);
                Timer = (uint)(questTimer.Time * 1000d);

                if (questTimer.HasElapsed)
                {
                    // ran out of time to complete quest
                    State = QuestState.Botched;
                    questTimer = null;
                }
            }

            foreach (IQuestObjective objective in objectives)
                objective.Update(lastTick);
        }

        private void SyncObjectiveCompletionFlags(bool requestSave)
        {
            QuestStateFlags syncedFlags = flags & ~ObjectiveCompletionFlagsMask;

            for (int index = 0; index < objectives.Count && index <= 5; index++)
            {
                if (!objectives[index].IsComplete())
                    continue;

                syncedFlags |= GetObjectiveCompletionFlag(index);
            }

            if (syncedFlags == flags)
                return;

            lock (persistence.Gate) { flags = syncedFlags; persistence.Dirty((int)QuestSaveMask.Flags); }

            if (requestSave)
                player.RequestSave();
        }

        private static QuestStateFlags GetObjectiveCompletionFlag(int objectiveIndex)
        {
            return objectiveIndex switch
            {
                0 => QuestStateFlags.Objective0Complete,
                1 => QuestStateFlags.Objective1Complete,
                2 => QuestStateFlags.Objective2Complete,
                3 => QuestStateFlags.Objective3Complete,
                4 => QuestStateFlags.Objective4Complete,
                5 => QuestStateFlags.Objective5Complete,
                _ => QuestStateFlags.None
            };
        }

        /// <summary>
        /// Enqueue <see cref="IQuest"/> to be deleted from the database.
        /// </summary>
        public void EnqueueDelete(bool set)
        {
            lock (persistence.Gate)
            {
                deleteRequested = set;
                if (set) persistence.Dirty((int)QuestSaveMask.Delete);
                else
                {
                    persistence.Cancel((int)QuestSaveMask.Delete);
                    persistence.Dirty((int)(QuestSaveMask.State | QuestSaveMask.Flags | QuestSaveMask.Timer | QuestSaveMask.Reset));
                }
            }

            player.RequestSave();
        }

        /// <summary>
        /// Returns if <see cref="IQuest"/> can be deleted.
        /// </summary>
        public bool CanDelete()
        {
            return Info.IsQuestMentioned != true;
        }

        /// <summary>
        /// Returns if <see cref="IQuest"/> can be abandoned.
        /// </summary>
        public bool CanAbandon()
        {
            if (State != QuestState.Botched && Info.CannotAbandon())
                return false;

            if (State == QuestState.Achieved && Info.CannotAbandonWhenAchieved())
                return false;

            return true;
        }

        /// <summary>
        /// Returns if <see cref="IQuest"/> can be shared with another <see cref="IPlayer"/>.
        /// </summary>
        public bool CanShare()
        {
            if (Info.Entry.QuestShareEnum == 0u)
                return false;

            return State is QuestState.Accepted or QuestState.Achieved or QuestState.Completed;
        }

        /// <summary>
        /// Update any <see cref="IQuestObjective"/>'s with supplied <see cref="QuestObjectiveType"/> and data with progress.
        /// </summary>
        public void ObjectiveUpdate(QuestObjectiveType type, uint data, uint progress)
        {
            if (PendingDelete)
                return;

            if (State is QuestState.Achieved or QuestState.Completed)
                return;

            uint previousObjectiveId = currentObjectiveId;

            // Order in reverse Index so that sequential steps don't completed by the same action.
            foreach (IQuestObjective objective in objectives
                .Where(o => o.ObjectiveInfo.Entry.Type == (uint)type && o.IsTarget(data))
                .OrderByDescending(o => o.Index))
            {
                if (objective.IsComplete())
                    continue;

                if (!CanUpdateObjective(objective))
                    continue;

                uint oldProgress = objective.Progress;
                objective.ObjectiveUpdate(progress);

                if (objective.Progress != oldProgress)
                    SendQuestObjectiveUpdate(objective);

                scriptCollection?.Invoke<IQuestScript>(s => s.OnObjectiveUpdate(objective));
                if (objective.IsComplete()) PublishObjectiveCompletion(objective);
            }

            if (RequiredObjectivesComplete())
                CompleteOptionalObjectives();

            SyncObjectiveCompletionFlags(true);

            if (objectives.All(o => o.IsComplete()))
                State = QuestState.Achieved;
            else
                SendQuestStateChangeIfCurrentObjectiveChanged(previousObjectiveId);
        }

        /// <summary>
        /// Update any <see cref="IQuestObjective"/>'s with supplied ID with progress.
        /// </summary>
        public void ObjectiveUpdate(uint id, uint progress)
        {
            if (PendingDelete)
                return;

            if (State is QuestState.Achieved or QuestState.Completed)
                return;

            uint previousObjectiveId = currentObjectiveId;

            IQuestObjective objective = objectives.SingleOrDefault(o => o.ObjectiveInfo.Id == id);
            if (objective == null)
                return;

            if (objective.IsComplete())
                return;

            if (!CanUpdateObjective(objective))
                return;

            uint oldProgress = objective.Progress;
            objective.ObjectiveUpdate(progress);

            if (objective.Progress != oldProgress)
                SendQuestObjectiveUpdate(objective);

            scriptCollection?.Invoke<IQuestScript>(s => s.OnObjectiveUpdate(objective));
            if (objective.IsComplete()) PublishObjectiveCompletion(objective);

            if (RequiredObjectivesComplete())
                CompleteOptionalObjectives();

            SyncObjectiveCompletionFlags(true);

            if (objectives.All(o => o.IsComplete()))
                State = QuestState.Achieved;
            else
                SendQuestStateChangeIfCurrentObjectiveChanged(previousObjectiveId);
        }

        public void SendObjectiveWorldLocationUpdates()
        {
            foreach (IQuestObjective objective in objectives)
            {
                ObjectiveWorldLocationResolution resolution = ResolveObjectiveWorldLocation(objective);
                ushort questId = Id; byte index = objective.Index; uint location = resolution.WorldLocationId;
                var session = player.Session;
                PublishProgressNotice(() => session.EnqueueMessageEncrypted(new ServerQuestObjectiveWorldLocation
                    { QuestId = questId, QuestObjectiveIndex = index, WorldLocation2Id = location }),
                    () => !PendingDelete && ResolveObjectiveWorldLocation(objective) == resolution);

                LogObjectiveWorldLocationUpdate(objective, resolution);
            }
        }

        private bool CanUpdateObjective(IQuestObjective objective)
        {
            if (objective.ObjectiveInfo.RequiresPreviousObjectives())
            {
                for (int i = 0; i < objective.Index; i++)
                {
                    if (objectives[i].ObjectiveInfo.IsOptional())
                        continue;

                    if (!objectives[i].IsComplete())
                        return false;
                }
            }

            return true;
        }

        private bool RequiredObjectivesComplete()
        {
            return objectives
                .Where(o => !o.ObjectiveInfo.IsOptional())
                .All(o => o.IsComplete());
        }

        private void CompleteOptionalObjectives()
        {
            foreach (IQuestObjective objective in objectives
                .Where(o => o.ObjectiveInfo.IsOptional() && !o.IsComplete()))
            {
                objective.Complete();
                SendQuestObjectiveUpdate(objective);
                PublishObjectiveCompletion(objective);
            }
        }

        private void PublishObjectiveCompletion(IQuestObjective objective)
        {
            PublishProgressNotice(() => player.QuestManager?.Communicator?.OnObjectiveComplete(Id, objective.Index),
                () => !PendingDelete && objective.IsComplete());
        }

        private void PublishProgressNotice(Action publish, Func<bool> current)
        {
            Action notice = () => { if (current()) publish(); };
            if (player.QuestManager is QuestManager manager && manager.RetainPublicEventQuestNotice(notice)) return;
            if (player.Inventory is Inventory inventory && inventory.RetainPhysicalLootQuestNotice(notice)) return;
            publish();
        }

        public byte GetObjectiveCompletionMask()
        {
            byte mask = 0;
            foreach (var objective in objectives)
                if (objective.Index < 8 && objective.IsComplete()) mask |= (byte)(1 << objective.Index);
            return mask;
        }

        private void SendQuestObjectiveUpdate(IQuestObjective objective)
        {
            // Only update objectives if the state isn't complete. Some scripts will complete quest without objective update.
            if (State == QuestState.Completed)
                return;

            ushort questId = Id; byte index = objective.Index; uint completed = objective.Progress;
            var session = player.Session;
            PublishProgressNotice(() => session.EnqueueMessageEncrypted(new ServerQuestObjectiveUpdate
                { QuestId = questId, QuestObjectiveIndex = index, Completed = completed }),
                () => !PendingDelete && State != QuestState.Completed && objective.Progress == completed);

            if (log.IsDebugEnabled && guidanceDiagnosticQuestIds.Contains(Id))
            {
                log.Debug(
                    "Quest objective update: player={PlayerId}, quest={QuestId}, objectiveIndex={ObjectiveIndex}, objectiveId={ObjectiveId}, objectiveType={ObjectiveType}, objectiveData={ObjectiveData}, progress={Progress}, state={QuestState}.",
                    player.CharacterId,
                    Id,
                    objective.Index,
                    objective.ObjectiveInfo.Id,
                    objective.ObjectiveInfo.Type,
                    objective.ObjectiveInfo.Entry.Data,
                    objective.Progress,
                    State);
            }

            SendObjectiveWorldLocationUpdates();
            TrySyncStarterTutorialEntityVisibility();
        }

        /// <summary>
        /// Invoked when <see cref="QuestState"/> for <see cref="IQuest"/> is updated.
        /// </summary>
        private void OnStateChange(QuestState oldState, bool created)
        {
            SendQuestStateChange(GetCurrentObjectiveId());

            SendObjectiveWorldLocationUpdates();

            QuestState newState = state;
            PublishProgressNotice(() => player.QuestManager?.Communicator?.OnQuestState(Id, oldState, newState, created),
                () => !PendingDelete && State == newState);

            scriptCollection?.Invoke<IQuestScript>(s => s.OnQuestStateChange(State, oldState));

            player.TryRecoverStarterTutorialQuestProgression();
        }

        private IGlobalQuestManager GetGlobalQuestManager()
        {
            return globalQuestManager ?? throw new InvalidOperationException($"{nameof(Quest)} requires an {nameof(IGlobalQuestManager)}.");
        }

        private IScriptManager GetScriptManager()
        {
            return scriptManager ?? throw new InvalidOperationException($"{nameof(Quest)} requires an {nameof(IScriptManager)}.");
        }

        private void SendQuestStateChangeIfCurrentObjectiveChanged(uint previousObjectiveId)
        {
            uint objectiveId = GetCurrentObjectiveId();
            if (objectiveId != previousObjectiveId)
                SendQuestStateChange(objectiveId);
            else
                currentObjectiveId = objectiveId;
        }

        private void SendQuestStateChange(uint objectiveId)
        {
            currentObjectiveId = objectiveId;
            ushort questId = Id; QuestState savedState = State;
            var session = player.Session;
            PublishProgressNotice(() => session.EnqueueMessageEncrypted(new ServerQuestStateChange
                { QuestId = questId, QuestState = savedState, QuestObjectiveId = objectiveId }),
                () => !PendingDelete && State == savedState && currentObjectiveId == objectiveId);

            if (log.IsDebugEnabled && guidanceDiagnosticQuestIds.Contains(Id))
            {
                log.Debug(
                    "Quest state update: player={PlayerId}, quest={QuestId}, state={QuestState}, currentObjectiveId={QuestObjectiveId}.",
                    player.CharacterId,
                    Id,
                    State,
                    objectiveId);
            }
        }

        public IEnumerator<IQuestObjective> GetEnumerator()
        {
            return objectives.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private ObjectiveWorldLocationResolution ResolveObjectiveWorldLocation(IQuestObjective objective)
        {
            if (State != QuestState.Accepted)
                return new ObjectiveWorldLocationResolution(0u, ObjectiveWorldLocationResolutionSource.InactiveQuest);

            if (!IsObjectiveEligibleForGuidance(objective))
                return new ObjectiveWorldLocationResolution(0u, ObjectiveWorldLocationResolutionSource.InactiveObjective);

            QuestObjectiveEntry entry = objective.ObjectiveInfo.Entry;

            if (TryResolveSingleDirectionLocation(entry.QuestDirectionId, out uint worldLocationId))
                return new ObjectiveWorldLocationResolution(worldLocationId, ObjectiveWorldLocationResolutionSource.QuestDirection);

            if (objective.ObjectiveInfo.Type == QuestObjectiveType.EnterArea)
            {
                if (TryResolveSingleDirectionEntryLocation(entry.Data, out worldLocationId))
                    return new ObjectiveWorldLocationResolution(worldLocationId, ObjectiveWorldLocationResolutionSource.EnterAreaDirectionEntry);

                if (TryResolveSingleDirectionLocation(entry.Data, out worldLocationId))
                    return new ObjectiveWorldLocationResolution(worldLocationId, ObjectiveWorldLocationResolutionSource.EnterAreaDirection);
            }

            return TryResolveSingleIndicatorLocation(entry, out worldLocationId)
                ? new ObjectiveWorldLocationResolution(worldLocationId, ObjectiveWorldLocationResolutionSource.Indicator)
                : new ObjectiveWorldLocationResolution(0u, ObjectiveWorldLocationResolutionSource.Unresolved);
        }

        private void LogObjectiveWorldLocationUpdate(IQuestObjective objective, ObjectiveWorldLocationResolution resolution)
        {
            if (!log.IsDebugEnabled || !guidanceDiagnosticQuestIds.Contains(Id))
                return;

            log.Debug(
                "QuestGuidance objective-world-location: player={PlayerId}, quest={QuestId}, objectiveIndex={ObjectiveIndex}, objectiveType={ObjectiveType}, progress={Progress}, state={QuestState}, worldLocation2Id={WorldLocation2Id}, source={Source}.",
                player.CharacterId,
                Id,
                objective.Index,
                objective.ObjectiveInfo.Type,
                objective.Progress,
                State,
                resolution.WorldLocationId,
                resolution.Source);
        }

        private bool IsObjectiveEligibleForGuidance(IQuestObjective objective)
        {
            if (objective.IsComplete())
                return false;

            if (objective.ObjectiveInfo.IsHidden() || objective.ObjectiveInfo.IsOptional())
                return false;

            return CanUpdateObjective(objective);
        }

        private bool TryResolveSingleDirectionLocation(uint directionId, out uint worldLocationId)
        {
            worldLocationId = 0u;
            if (directionId == 0u)
                return false;

            QuestDirectionEntry direction = gameTableManager?.QuestDirection?.GetEntry(directionId);
            if (direction == null)
                return false;

            HashSet<uint> locations = [];
            foreach (uint directionEntryId in EnumerateDirectionEntryIds(direction))
            {
                if (TryResolveSingleDirectionEntryLocation(directionEntryId, out uint entryWorldLocationId))
                    locations.Add(entryWorldLocationId);
            }

            if (locations.Count != 1)
                return false;

            worldLocationId = locations.First();
            return true;
        }

        private bool TryResolveSingleDirectionEntryLocation(uint directionEntryId, out uint worldLocationId)
        {
            worldLocationId = 0u;
            if (directionEntryId == 0u)
                return false;

            QuestDirectionEntryEntry directionEntry = gameTableManager?.QuestDirectionEntry?.GetEntry(directionEntryId);
            if (directionEntry == null || directionEntry.WorldLocation2Id == 0u)
                return false;

            worldLocationId = directionEntry.WorldLocation2Id;
            return true;
        }

        private void TrySyncStarterTutorialEntityVisibility()
        {
            if (Id is 10527 or 10532)
                player.SyncStarterTutorialEntityVisibility();
        }

        private static IEnumerable<uint> EnumerateDirectionEntryIds(QuestDirectionEntry direction)
        {
            uint[] directionEntryIds =
            [
                direction.QuestDirectionEntryId00,
                direction.QuestDirectionEntryId01,
                direction.QuestDirectionEntryId02,
                direction.QuestDirectionEntryId03,
                direction.QuestDirectionEntryId04,
                direction.QuestDirectionEntryId05,
                direction.QuestDirectionEntryId06,
                direction.QuestDirectionEntryId07,
                direction.QuestDirectionEntryId08,
                direction.QuestDirectionEntryId09,
                direction.QuestDirectionEntryId10,
                direction.QuestDirectionEntryId11,
                direction.QuestDirectionEntryId12,
                direction.QuestDirectionEntryId13,
                direction.QuestDirectionEntryId14,
                direction.QuestDirectionEntryId15
            ];

            return directionEntryIds.Where(id => id != 0u);
        }

        private static bool TryResolveSingleIndicatorLocation(QuestObjectiveEntry entry, out uint worldLocationId)
        {
            uint[] indicatorIds =
            [
                entry.WorldLocationsIdIndicator00,
                entry.WorldLocationsIdIndicator01,
                entry.WorldLocationsIdIndicator02,
                entry.WorldLocationsIdIndicator03
            ];

            HashSet<uint> locations = indicatorIds.Where(id => id != 0u).ToHashSet();

            if (locations.Count != 1)
            {
                worldLocationId = 0u;
                return false;
            }

            worldLocationId = locations.First();
            return true;
        }
    }
}
