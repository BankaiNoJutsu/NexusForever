using NexusForever.Game.Abstract.PublicEvent;
using System.Numerics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Quest;
using NexusForever.Game.Static.Achievement;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.GameTable.Model;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Shared.Game;
using NetworkPublicEventObjective = NexusForever.Network.World.Message.Model.Shared.PublicEventObjective;
using NetworkPublicEventObjectiveStatus = NexusForever.Network.World.Message.Model.Shared.PublicEventObjectiveStatus;

namespace NexusForever.Game.PublicEvent
{
    public class PublicEventObjective : IPublicEventObjective
    {
        public IPublicEventTeam Team { get; private set; }
        public PublicEventObjectiveEntry Entry { get; private set; }
        public PublicEventStatus Status { get; private set; }
        public uint Count { get; private set; }
        public uint DynamicMax { get; set; }

        public bool IsBusy { get; private set; }

        private uint objectiveData;
        private IReadOnlyCollection<NetworkPublicEventObjectiveStatus.VirtualItem> virtualItems
            = Array.Empty<NetworkPublicEventObjectiveStatus.VirtualItem>();

        private double elapsedTimer;
        private UpdateTimer failureTimer;
        private readonly IPlayerManager playerManager;

        public PublicEventObjective(IPlayerManager playerManager = null)
        {
            this.playerManager = playerManager;
        }

        /// <summary>
        /// Initialise <see cref="PublicEventObjective"/> with suppled <see cref="IPublicEventTeam"/> and <see cref="PublicEventObjectiveEntry"/>.
        /// </summary>
        public void Initialise(
            IPublicEventTeam team,
            PublicEventObjectiveEntry entry,
            IReadOnlyCollection<NetworkPublicEventObjectiveStatus.VirtualItem> virtualItems = null)
        {
            Team  = team;
            Entry = entry;
            this.virtualItems = virtualItems?.ToArray()
                ?? Array.Empty<NetworkPublicEventObjectiveStatus.VirtualItem>();
            ResetProgress();

            Status = entry.PublicEventObjectiveFlags.HasFlag(PublicEventObjectiveFlag.InitialObjective)
                ? PublicEventStatus.Active : PublicEventStatus.Inactive;
        }

        /// <summary>
        /// Invoked each world tick with the delta since the previous tick occurred.
        /// </summary>
        public void Update(double lastTick)
        {
            if (IsBusy || Status != PublicEventStatus.Active)
                return;

            elapsedTimer += lastTick;

            if (failureTimer == null)
                return;

            failureTimer.Update(lastTick);
            if (failureTimer.HasElapsed)
            {
                failureTimer = null;
                SetStatus(PublicEventStatus.Failed);
            }
        }

        private void SetStatus(PublicEventStatus status)
        {
            Status = status;
            BroadcastObjectiveStatusUpdate();
            if (status == PublicEventStatus.Succeeded)
                UpdateObjectiveCompletionAchievements();

            Team.PublicEvent.InvokeScriptCollection<IPublicEventScript>(s => s.OnPublicEventObjectiveStatus(this));
        }

        private void UpdateObjectiveCompletionAchievements()
        {
            foreach (IPublicEventTeamMember member in Team.GetMembers())
            {
                IPlayer player = playerManager?.GetPlayer(member.CharacterId);
                if (player != null)
                {
                    player.AchievementManager.CheckAchievements(player, AchievementType.PublicEventObjectiveComplete, Entry.Id);
                    PublicEventQuestObjectiveUpdater.OnPublicEventObjectiveSucceeded(player, Entry.Id);
                }
            }
        }

        private void BroadcastObjectiveUpdate()
        {
            BroadcastObjectiveUpdate(Build());
        }

        private void BroadcastObjectiveStatusUpdate()
        {
            BroadcastObjectiveUpdate(new ServerPublicEventObjectiveStatusUpdate
            {
                ObjectiveId     = Entry.Id,
                ObjectiveStatus = BuildObjectiveStatus()
            });
        }

        private void BroadcastObjectiveUpdate(IWritable message)
        {
            // Lua_PublicEventObjective_GetDescription/GetShortDescription switch
            // viewers between owning-team and other-team text ids, so objectives
            // with an other-team text variant need updates broadcast to every team.
            if (Entry.LocalizedTextIdOtherTeam == 0)
                Team.Broadcast(message);
            else
            {
                foreach (IPublicEventTeam team in Team.PublicEvent.GetTeams())
                    team.Broadcast(message);
            }
        }

        /// <summary>
        /// Set busy state for the objective.
        /// </summary>
        /// <remarks>
        /// This will pause the objective preventing updates.
        /// </remarks>
        public void SetBusy(bool busy)
        {
            // WildStar64.exe 14007b490 maps 0x0132 as the full objective payload;
            // avoid emitting duplicate payloads when the server state is unchanged.
            if (IsBusy == busy)
                return;

            IsBusy = busy;
            BroadcastObjectiveUpdate();
        }

        /// <summary>
        /// Update objective with the supplied count.
        /// </summary>
        public void UpdateObjective(int count)
        {
            if (IsBusy)
                return;

            if (Status != PublicEventStatus.Active)
                return;

            if (IsChecklist())
            {
                UpdateChecklistObjective(count);
            }
            else
            {
                uint oldCount = Count;
                Count = (uint)Math.Max(0, (int)Count + count);

                if (oldCount != Count)
                    BroadcastObjectiveUpdate();
            }

            if (IsComplete())
                SetStatus(PublicEventStatus.Succeeded);
        }

        private void UpdateChecklistObjective(int checklistIndex)
        {
            if (checklistIndex < 0 || checklistIndex >= sizeof(uint) * 8)
                return;

            uint oldObjectiveData = objectiveData;
            objectiveData |= 1u << checklistIndex;
            if (oldObjectiveData == objectiveData)
                return;

            Count = (uint)BitOperations.PopCount(objectiveData);
            BroadcastObjectiveUpdate();
        }

        private bool IsComplete()
        {
            // WildStar64.exe Lua_PublicEventObjective_GetRequiredCount (14068ef30)
            // returns 0 for type 0x19, while GetCount (14068e110) continues to
            // expose the raw running count. ScriptWithoutMax is therefore a
            // controller-owned counter and must not auto-complete at Entry.Count.
            if (Entry.PublicEventObjectiveTypeEnum == PublicEventObjectiveType.ScriptWithoutMax)
                return false;

            if (IsChecklist())
                return Count >= GetMaxCount();

            if (DynamicMax > 0u)
                return Count >= DynamicMax;

            if (Entry.PublicEventObjectiveFlags.HasFlag(PublicEventObjectiveFlag.UsesDynamicMaxCount))
                return Count >= DynamicMax;

            return Count >= Entry.Count;
        }

        private bool IsChecklist()
        {
            return Entry.PublicEventObjectiveTypeEnum is PublicEventObjectiveType.ActivateTargetGroupChecklist
                or PublicEventObjectiveType.TalkToChecklist;
        }

        private uint GetMaxCount()
        {
            if (DynamicMax > 0u)
                return DynamicMax;

            return Entry.Count;
        }

        /// <summary>
        /// Activate the objective.
        /// </summary>
        /// <remarks>
        /// This shows the objective to members and allows it to be updated.
        /// </remarks>
        public void ActivateObjective()
        {
            if (Status != PublicEventStatus.Inactive)
                return;

            SetStatus(PublicEventStatus.Active);
        }

        /// <summary>
        /// Reset the objective to its inactive initial state.
        /// </summary>
        public void ResetObjective()
        {
            if (Status != PublicEventStatus.Succeeded)
                return;

            ResetProgress();
            SetStatus(PublicEventStatus.Inactive);
        }

        private void ResetProgress()
        {
            Count        = 0;
            DynamicMax   = 0;
            objectiveData = 0;
            elapsedTimer = 0d;
            failureTimer = CreateFailureTimer();
        }

        private UpdateTimer CreateFailureTimer()
        {
            return Entry?.FailureTimeMs > 0
                ? new UpdateTimer(TimeSpan.FromMilliseconds(Entry.FailureTimeMs))
                : null;
        }

        public NetworkPublicEventObjective Build()
        {
            return new NetworkPublicEventObjective
            {
                ObjectiveId      = Entry.Id,
                ObjectiveStatus  = BuildObjectiveStatus(),
                Busy             = IsBusy,
                ElapsedTimeMs    = (uint)(elapsedTimer * 1000d),
                Locations        = Entry.WorldLocation2Id == 0u ? [] : [Entry.WorldLocation2Id]
            };
        }

        private NetworkPublicEventObjectiveStatus BuildObjectiveStatus()
        {
            var status = new NetworkPublicEventObjectiveStatus
            {
                Status     = Status,
                ObjectiveData = objectiveData,
                Count      = Count,
                DynamicMax = DynamicMax
            };

            if (virtualItems.Count != 0)
            {
                status.DataType = PublicEventObjectiveDataType.VirtualItemDepot;
                status.VirtualItems = virtualItems
                    .Select(item => new NetworkPublicEventObjectiveStatus.VirtualItem
                    {
                        ItemId = item.ItemId,
                        Count  = item.Count
                    })
                    .ToList();
            }

            return status;
        }
    }
}
