using NexusForever.Game.Abstract.Challenges;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Challenges;
using NexusForever.Game.Static.Quest;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model.Challenges;
using NexusForever.Shared;

namespace NexusForever.Game.Challenges
{
    public sealed class ChallengeManager : IChallengeManager
    {
        private const double ShareTimeoutSeconds = 30d;
        private const double DefaultActiveSeconds = 300d;
        private const double DefaultCooldownSeconds = 1800d;
        private const double DefaultAreaFailSeconds = 10d;
        private const uint TimeTieredFlag = 0x8u;
        private const uint CooldownTypeFlag = 0x10u;

        private readonly IPlayer player;
        private readonly IGameTableManager gameTableManager;
        private readonly Dictionary<ushort, ChallengeRuntimeState> activeChallenges = new();
        private readonly Dictionary<ushort, PendingChallengeShare> pendingShares = new();

        public ChallengeManager(IPlayer owner)
            : this(owner, GameTableManager.Instance)
        {
        }

        internal ChallengeManager(IPlayer owner, IGameTableManager gameTableManager)
        {
            player             = owner;
            this.gameTableManager = gameTableManager;
        }

        public void SendInitialPackets()
        {
            SendChallengeUpdate();
        }

        public void Update(double lastTick)
        {
            foreach (ChallengeRuntimeState state in activeChallenges.Values.ToList())
            {
                if (state.Activated && state.ActiveTimer > 0d)
                {
                    state.ActiveTimer -= lastTick;
                    if (state.ActiveTimer <= 0d)
                        HandleActiveTimerExpired(state);
                }

                if (state.OnCooldown && state.CooldownTimer > 0d)
                {
                    state.CooldownTimer -= lastTick;
                    if (state.CooldownTimer <= 0d)
                        state.OnCooldown = false;
                }

                if (state.LeftArea && state.AreaFailTimer > 0d)
                {
                    state.AreaFailTimer -= lastTick;
                    if (state.AreaFailTimer <= 0d)
                        HandleAreaFailExpired(state);
                }
            }

            foreach ((ushort challengeId, PendingChallengeShare share) in pendingShares.ToList())
            {
                share.Timer -= lastTick;
                if (share.Timer > 0d)
                    continue;

                pendingShares.Remove(challengeId);
                player.Session?.EnqueueMessageEncrypted(new ServerChallengeShareTimeout
                {
                    ChallengeId = challengeId
                });
            }

            if (activeChallenges.Count > 0)
                SendChallengeUpdate();
        }

        public void HandleChoice(ushort challengeId, ChallengeChoice choice)
        {
            switch (choice)
            {
                case ChallengeChoice.Activate:
                    TryActivate(challengeId);
                    break;
                case ChallengeChoice.Abandon:
                    TryAbandon(challengeId);
                    break;
                case ChallengeChoice.AcceptShared:
                    TryAcceptShared(challengeId);
                    break;
                case ChallengeChoice.DeclineShared:
                    TryDeclineShared(challengeId);
                    break;
                default:
                    SendResult(challengeId, ChallengeResult.GenericFail);
                    break;
            }
        }

        public void ShareWithTarget(ushort challengeId)
        {
            if (!activeChallenges.TryGetValue(challengeId, out ChallengeRuntimeState state) || !state.Activated)
            {
                SendResult(challengeId, ChallengeResult.GenericFail);
                return;
            }

            if (player.TargetGuid == null)
            {
                SendResult(challengeId, ChallengeResult.GenericFail);
                return;
            }

            IPlayer recipient = player.GetVisible<IPlayer>(player.TargetGuid.Value);
            if (recipient == null || !recipient.SharedChallengeEnabled)
            {
                SendResult(challengeId, ChallengeResult.GenericFail);
                return;
            }

            recipient.ChallengeManager.ReceiveShare(challengeId, player.Guid);
        }

        public void ReceiveShare(ushort challengeId, uint sharerUnitId)
        {
            ChallengeEntry entry = gameTableManager.Challenge.GetEntry(challengeId);
            if (entry == null || !player.SharedChallengeEnabled)
                return;

            pendingShares[challengeId] = new PendingChallengeShare
            {
                SharerUnitId = sharerUnitId,
                Timer        = ShareTimeoutSeconds
            };

            player.Session?.EnqueueMessageEncrypted(new ServerChallengeShared
            {
                ChallengeId  = challengeId,
                SharerUnitId = sharerUnitId
            });
        }

        private void TryActivate(ushort challengeId)
        {
            ChallengeEntry entry = gameTableManager.Challenge.GetEntry(challengeId);
            if (entry == null)
            {
                SendResult(challengeId, ChallengeResult.GenericFail);
                return;
            }

            if (HasActiveChallengeOfType(entry.ChallengeTypeEnum))
            {
                SendResult(challengeId, ChallengeResult.TypeAlreadyActive);
                return;
            }

            if (activeChallenges.TryGetValue(challengeId, out ChallengeRuntimeState existing))
            {
                if (existing.OnCooldown)
                {
                    SendResult(challengeId, ChallengeResult.CooldownActive);
                    return;
                }

                if (existing.Activated)
                {
                    SendResult(challengeId, ChallengeResult.TypeAlreadyActive);
                    return;
                }
            }

            if (!MeetsZoneRestriction(entry))
            {
                SendResult(challengeId, ChallengeResult.AreaRestriction);
                return;
            }

            ChallengeRuntimeState state = GetOrCreateState(challengeId);
            state.Activated    = true;
            state.OnCooldown   = false;
            state.LeftArea       = false;
            state.ActiveTimer  = DefaultActiveSeconds;
            state.AreaFailTimer = 0d;

            SendResult(challengeId, ChallengeResult.Activate);
            SendChallengeUpdate();
        }

        private void TryAbandon(ushort challengeId)
        {
            if (!activeChallenges.TryGetValue(challengeId, out ChallengeRuntimeState state) || !state.Activated)
            {
                SendResult(challengeId, ChallengeResult.GenericFail);
                return;
            }

            Deactivate(state);
            SendResult(challengeId, ChallengeResult.AbandonRemove);
            SendChallengeUpdate();
        }

        private void TryAcceptShared(ushort challengeId)
        {
            if (!pendingShares.Remove(challengeId))
            {
                SendResult(challengeId, ChallengeResult.GenericFail);
                return;
            }

            TryActivate(challengeId);
        }

        private void TryDeclineShared(ushort challengeId)
        {
            if (!pendingShares.Remove(challengeId))
            {
                SendResult(challengeId, ChallengeResult.GenericFail);
                return;
            }
        }

        private void HandleActiveTimerExpired(ChallengeRuntimeState state)
        {
            state.Activated   = false;
            state.ActiveTimer = 0d;

            if (HasCooldownType(state.ChallengeId))
            {
                state.OnCooldown     = true;
                state.CooldownTimer  = DefaultCooldownSeconds;
            }

            SendResult((ushort)state.ChallengeId, ChallengeResult.TimerExpired);
            SendChallengeUpdate();
        }

        private void HandleAreaFailExpired(ChallengeRuntimeState state)
        {
            state.LeftArea      = false;
            state.AreaFailTimer = 0d;
            Deactivate(state);
            SendResult((ushort)state.ChallengeId, ChallengeResult.LeftArea);
            SendChallengeUpdate();
        }

        private void Deactivate(ChallengeRuntimeState state)
        {
            state.Activated   = false;
            state.ActiveTimer = 0d;
            state.LeftArea      = false;
            state.AreaFailTimer = 0d;
        }

        private bool HasActiveChallengeOfType(uint challengeType)
        {
            foreach (ChallengeRuntimeState state in activeChallenges.Values)
            {
                if (!state.Activated)
                    continue;

                ChallengeEntry entry = gameTableManager.Challenge.GetEntry(state.ChallengeId);
                if (entry != null && entry.ChallengeTypeEnum == challengeType)
                    return true;
            }

            return false;
        }

        private bool MeetsZoneRestriction(ChallengeEntry entry)
        {
            if (entry.WorldZoneIdRestriction == 0u || player.Zone == null)
                return true;

            return player.Zone.Id == entry.WorldZoneIdRestriction;
        }

        private bool HasCooldownType(uint challengeId)
        {
            ChallengeEntry entry = gameTableManager.Challenge.GetEntry(challengeId);
            return entry != null && (entry.ChallengeFlags & CooldownTypeFlag) != 0u;
        }

        private ChallengeRuntimeState GetOrCreateState(uint challengeId)
        {
            ushort id = (ushort)challengeId;
            if (!activeChallenges.TryGetValue(id, out ChallengeRuntimeState state))
            {
                state = new ChallengeRuntimeState
                {
                    ChallengeId = challengeId
                };
                activeChallenges.Add(id, state);
            }

            return state;
        }

        internal bool TryAdvanceProgress(ushort challengeId, uint progress = 1u)
        {
            if (!activeChallenges.TryGetValue(challengeId, out ChallengeRuntimeState state) || !state.Activated)
                return false;

            ChallengeEntry entry = gameTableManager.Challenge.GetEntry(challengeId);
            if (entry == null)
                return false;

            uint[] tierGoals = GetTierGoalCounts(entry);
            uint goalCount = tierGoals.Length > 0 ? tierGoals[Math.Min(state.CurrentTier, (uint)tierGoals.Length - 1)] : 0u;
            if (goalCount == 0u)
                return false;

            state.CurrentCount = Math.Min(state.CurrentCount + progress, goalCount);
            SendChallengeUpdate();

            if (state.CurrentCount < goalCount)
                return true;

            uint achievedTier = state.CurrentTier;
            state.LastRewardTier = achievedTier;
            SendResult(challengeId, ChallengeResult.TierAchieved, (int)achievedTier);

            if (achievedTier + 1u >= tierGoals.Length)
            {
                CompleteChallenge(state, entry);
                return true;
            }

            state.CurrentTier++;
            state.CurrentCount = 0u;
            SendChallengeUpdate();
            return true;
        }

        private void CompleteChallenge(ChallengeRuntimeState state, ChallengeEntry entry)
        {
            state.Activated = false;
            state.OnCooldown = HasCooldownType(state.ChallengeId);
            if (state.OnCooldown)
                state.CooldownTimer = DefaultCooldownSeconds;

            state.CompletionCount++;
            SendResult((ushort)state.ChallengeId, ChallengeResult.Completed, (int)state.LastRewardTier);
            player.QuestManager.ObjectiveUpdate(QuestObjectiveType.CompleteChallenge, 0u, 1u);
            SendChallengeUpdate();
        }

        private uint[] GetTierGoalCounts(ChallengeEntry entry)
        {
            return new uint[]
            {
                GetTierCount(entry.ChallengeTierId00),
                GetTierCount(entry.ChallengeTierId01),
                GetTierCount(entry.ChallengeTierId02)
            }.Where(count => count > 0u).ToArray();
        }

        private uint GetTierCount(uint tierId)
        {
            if (tierId == 0u)
                return 0u;

            ChallengeTierEntry tier = gameTableManager.ChallengeTier.GetEntry(tierId);
            return tier?.Count ?? 0u;
        }

        private void SendResult(ushort challengeId, ChallengeResult result, int data = 0)
        {
            player.Session?.EnqueueMessageEncrypted(new ServerChallengeResult
            {
                ChallengeId = challengeId,
                Result      = result,
                Data        = data
            });
        }

        private void SendChallengeUpdate()
        {
            var update = new ServerChallengeUpdate();
            foreach (ChallengeRuntimeState state in activeChallenges.Values)
            {
                ChallengeEntry entry = gameTableManager.Challenge.GetEntry(state.ChallengeId);
                if (entry == null)
                    continue;

                uint[] tierGoals = GetTierGoalCounts(entry);
                uint goalCount = tierGoals.Length > 0
                    ? tierGoals[Math.Min(state.CurrentTier, (uint)tierGoals.Length - 1)]
                    : 0u;

                update.ActiveChallenges.Add(new ServerChallengeUpdate.Challenge
                {
                    ChallengeId         = state.ChallengeId,
                    Type                  = (ChallengeType)entry.ChallengeTypeEnum,
                    TargetGroupId         = entry.TargetGroupIdRewardPane,
                    QualifyCount          = 0u,
                    QualityTotal          = 0u,
                    CurrentCount          = state.CurrentCount,
                    GoalCount             = goalCount,
                    ObjectiveCompletion   = 0u,
                    CurrentTier           = state.CurrentTier,
                    LastRewardTier        = state.LastRewardTier,
                    CompletionCount       = state.CompletionCount,
                    Unlocked              = true,
                    Activated             = state.Activated,
                    OnCooldown            = state.OnCooldown,
                    LeftArea              = state.LeftArea,
                    TimeActivatedDt       = state.Activated ? ToTimerUnits(state.ActiveTimer, DefaultActiveSeconds) : 0u,
                    TimeTotalActive       = (uint)DefaultActiveSeconds,
                    TimeCooldownDt        = state.OnCooldown ? ToTimerUnits(state.CooldownTimer, DefaultCooldownSeconds) : 0u,
                    TimeTotalCooldown     = (uint)DefaultCooldownSeconds,
                    TimeAreaFailDt        = state.LeftArea ? ToTimerUnits(state.AreaFailTimer, DefaultAreaFailSeconds) : 0u,
                    TimeTotalAreaFail     = (uint)DefaultAreaFailSeconds,
                    TierGoalCount         = PadTierGoals(tierGoals)
                });
            }

            player.Session?.EnqueueMessageEncrypted(update);
        }

        private static uint ToTimerUnits(double remainingSeconds, double totalSeconds)
        {
            if (totalSeconds <= 0d)
                return 0u;

            return (uint)Math.Max(0d, Math.Round(remainingSeconds));
        }

        private static uint[] PadTierGoals(uint[] tierGoals)
        {
            uint[] padded = new uint[3];
            for (int i = 0; i < padded.Length && i < tierGoals.Length; i++)
                padded[i] = tierGoals[i];

            return padded;
        }

        /// <summary>
        /// Counts challenges with at least one recorded completion in the supplied world-zone set.
        /// Completion is session-local until challenge persistence exists.
        /// </summary>
        internal uint GetCompletedCountForWorldZones(IReadOnlySet<uint> worldZoneIds)
        {
            if (worldZoneIds == null || worldZoneIds.Count == 0)
                return 0u;

            uint count = 0u;
            foreach ((ushort challengeId, ChallengeRuntimeState state) in activeChallenges)
            {
                if (state.CompletionCount == 0u)
                    continue;

                ChallengeEntry entry = gameTableManager.Challenge.GetEntry(challengeId);
                if (entry == null)
                    continue;

                if (worldZoneIds.Contains(entry.WorldZoneId)
                    || (entry.WorldZoneIdRestriction != 0u && worldZoneIds.Contains(entry.WorldZoneIdRestriction)))
                    count++;
            }

            return count;
        }
    }
}
