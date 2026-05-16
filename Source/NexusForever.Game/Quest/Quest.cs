using System.Collections;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Quest;
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
    public class Quest : IQuest
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
            Timer  = 0x20
        }

        public ushort Id => (ushort)Info.Entry.Id;
        public IQuestInfo Info { get; }

        public QuestState State
        {
            get => state;
            set
            {
                QuestState oldState = state;

                state = value;
                saveMask |= QuestSaveMask.State;

                OnStateChange(oldState);
                player.RequestSave();
            }
        }

        private QuestState state;

        public QuestStateFlags Flags
        {
            get => flags;
            set
            {
                flags = value;
                saveMask |= QuestSaveMask.Flags;
                player.RequestSave();
            }
        }

        private QuestStateFlags flags;

        public uint? Timer
        {
            get => timer;
            set
            {
                timer = value;
                saveMask |= QuestSaveMask.Timer;
            }
        }

        private uint? timer;

        public DateTime? Reset
        {
            get => reset;
            set
            {
                reset = value;
                saveMask |= QuestSaveMask.Reset;
                player.RequestSave();
            }
        }

        private DateTime? reset;

        /// <summary>
        /// Returns if <see cref="IQuest"/> is enqueued to be saved to the database.
        /// </summary>
        public bool PendingCreate => (saveMask & QuestSaveMask.Create) != 0;

        /// <summary>
        /// Returns if <see cref="IQuest"/> is enqueued to be deleted from the database.
        /// </summary>
        public bool PendingDelete => (saveMask & QuestSaveMask.Delete) != 0;

        private QuestSaveMask saveMask;

        private readonly IPlayer player;
        private readonly List<IQuestObjective> objectives = new();

        private UpdateTimer questTimer;

        private IScriptCollection scriptCollection;

        /// <summary>
        /// Create a new <see cref="IQuest"/> from an existing database model.
        /// </summary>
        public Quest(IPlayer owner, IQuestInfo info, CharacterQuestModel model)
        {
            player = owner;
            Info   = info;
            state  = (QuestState)model.State;
            flags  = (QuestStateFlags)model.Flags;
            timer  = model.Timer;
            reset  = model.Reset;

            if (timer != null)
                questTimer = new UpdateTimer(timer.Value);

            foreach (CharacterQuestObjectiveModel objectiveModel in model.QuestObjective)
                objectives.Add(new QuestObjective(player, info, info.Objectives[objectiveModel.Index], objectiveModel));

            scriptCollection = ScriptManager.Instance.InitialiseOwnedScripts<IQuest>(this, info.Entry.Id);
        }

        /// <summary>
        /// Create a new <see cref="IQuest"/> from supplied <see cref="IQuestInfo"/>.
        /// </summary>
        public Quest(IPlayer owner, IQuestInfo info)
        {
            player = owner;
            Info   = info;
            state  = QuestState.Accepted;

            for (byte i = 0; i < info.Objectives.Count; i++)
                objectives.Add(new QuestObjective(player, info, info.Objectives[i], i));

            if (objectives.Count == 0)
                state = QuestState.Achieved;

            saveMask = QuestSaveMask.Create;

            scriptCollection = ScriptManager.Instance.InitialiseOwnedScripts<IQuest>(this, info.Entry.Id);
        }

        public void Dispose()
        {
            if (scriptCollection != null)
                ScriptManager.Instance.Unload(scriptCollection);

            scriptCollection = null;
        }

        public void InitialiseTimer()
        {
            if (Info.Entry.MaxTimeAllowedMS != 0u)
            {
                questTimer = new UpdateTimer(Info.Entry.MaxTimeAllowedMS / 1000d);
                Timer = (uint)(questTimer.Time * 1000d);
            }

            // TODO: objective timers
        }

        public void Save(CharacterContext context)
        {
            if (saveMask != QuestSaveMask.None)
            {
                if ((saveMask & QuestSaveMask.Create) != 0)
                {
                    context.Add(new CharacterQuestModel
                    {
                        Id      = player.CharacterId,
                        QuestId = Id,
                        State   = (byte)State,
                        Flags   = (byte)Flags,
                        Timer   = Timer,
                        Reset   = Reset
                    });
                }
                else if ((saveMask & QuestSaveMask.Delete) != 0)
                {
                    var model = new CharacterQuestModel
                    {
                        Id      = player.CharacterId,
                        QuestId = Id
                    };

                    context.Entry(model).State = EntityState.Deleted;
                }
                else
                {
                    var model = new CharacterQuestModel
                    {
                        Id      = player.CharacterId,
                        QuestId = Id
                    };

                    EntityEntry<CharacterQuestModel> entity = context.Attach(model);
                    if ((saveMask & QuestSaveMask.State) != 0)
                    {
                        model.State = (byte)State;
                        entity.Property(p => p.State).IsModified = true;
                    }

                    if ((saveMask & QuestSaveMask.Flags) != 0)
                    {
                        model.Flags = (byte)Flags;
                        entity.Property(p => p.Flags).IsModified = true;
                    }

                    if ((saveMask & QuestSaveMask.Reset) != 0)
                    {
                        model.Reset = Reset;
                        entity.Property(p => p.Reset).IsModified = true;
                    }

                    if ((saveMask & QuestSaveMask.Timer) != 0)
                    {
                        model.Timer = Timer;
                        entity.Property(p => p.Timer).IsModified = true;
                    }
                }

                saveMask = QuestSaveMask.None;
            }

            foreach (IQuestObjective objective in objectives)
                objective.Save(context);
        }

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
        }

        /// <summary>
        /// Enqueue <see cref="IQuest"/> to be deleted from the database.
        /// </summary>
        public void EnqueueDelete(bool set)
        {
            if (set)
                saveMask |= QuestSaveMask.Delete;
            else
                saveMask &= ~QuestSaveMask.Delete;

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

            if (State == QuestState.Achieved)
                return;

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
            }

            // TODO: Should you be able to complete optional objectives after required are completed?
            if (RequiredObjectivesComplete())
                CompleteOptionalObjectives();

            if (objectives.All(o => o.IsComplete()))
                State = QuestState.Achieved;
        }

        /// <summary>
        /// Update any <see cref="IQuestObjective"/>'s with supplied ID with progress.
        /// </summary>
        public void ObjectiveUpdate(uint id, uint progress)
        {
            if (PendingDelete)
                return;

            if (State == QuestState.Achieved)
                return;

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

            // TODO: Should you be able to complete optional objectives after required are completed?
            if (RequiredObjectivesComplete())
                CompleteOptionalObjectives();

            if (objectives.All(o => o.IsComplete()))
                State = QuestState.Achieved;
        }

        public void SendObjectiveWorldLocationUpdates()
        {
            foreach (IQuestObjective objective in objectives)
            {
                ObjectiveWorldLocationResolution resolution = ResolveObjectiveWorldLocation(objective);
                player.Session.EnqueueMessageEncrypted(new ServerQuestObjectiveWorldLocation
                {
                    QuestId = Id,
                    QuestObjectiveIndex = objective.Index,
                    WorldLocation2Id = resolution.WorldLocationId
                });

                LogObjectiveWorldLocationUpdate(objective, resolution);
            }
        }

        private bool CanUpdateObjective(IQuestObjective objective)
        {
            if (objective.ObjectiveInfo.IsSequential())
            {
                for (int i = 0; i < objective.Index; i++)
                {
                    if (objectives[i].ObjectiveInfo.IsOptional())
                        continue;

                    if (!objectives[i].IsComplete())
                        return false;
                }
            }

            // TODO: client also checks objective flags 1 and 8 in the same function
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
            }
        }

        private void SendQuestObjectiveUpdate(IQuestObjective objective)
        {
            // Only update objectives if the state isn't complete. Some scripts will complete quest without objective update.
            if (State == QuestState.Completed)
                return;

            player.Session.EnqueueMessageEncrypted(new ServerQuestObjectiveUpdate
            {
                QuestId   = Id,
                QuestObjectiveIndex     = objective.Index,
                Completed = objective.Progress
            });

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
        private void OnStateChange(QuestState oldState)
        {
            player.Session.EnqueueMessageEncrypted(new ServerQuestStateChange
            {
                QuestId    = Id,
                QuestState = State
            });

            SendObjectiveWorldLocationUpdates();

            // check if this quest and state is a trigger for a new communicator message
            foreach (ICommunicatorMessage message in GlobalQuestManager.Instance.GetQuestCommunicatorQuestStateTriggers(Id, state))
                if (message.Meets(player))
                {
                    message.Send(player.Session);

                    if (message.DeliversQuest)
                        player.QuestManager.QuestMention(message.QuestId);
                }

            scriptCollection?.Invoke<IQuestScript>(s => s.OnQuestStateChange(State, oldState));

            player.TryRecoverStarterTutorialQuestProgression();
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

            QuestDirectionEntry direction = GameTableManager.Instance.QuestDirection.GetEntry(directionId);
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

            QuestDirectionEntryEntry directionEntry = GameTableManager.Instance.QuestDirectionEntry.GetEntry(directionEntryId);
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
