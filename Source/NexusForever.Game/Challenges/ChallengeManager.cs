using NexusForever.Database.Character;
using NexusForever.Database.Auth;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Challenges;
using NexusForever.Game.Retail;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Abstract;
using NexusForever.Game.Static.Challenges;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Quest;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Challenges;
using NexusForever.Network.World.Message.Static;
using NexusForever.Shared;

namespace NexusForever.Game.Challenges
{
    public sealed partial class ChallengeManager : IChallengeManager
    {
        private const double ShareTimeoutSeconds = 30d;
        private const double DefaultActiveSeconds = 300d;
        private const double DefaultCooldownSeconds = 1800d;
        private const double DefaultAreaFailSeconds = 10d;
        private const uint TimeTieredFlag = 0x8u;
        private const uint CooldownTypeFlag = 0x10u;
        private const uint AutoActivateOnProgressFlag = 0x20u;
        private const uint MaxScoreExcludedFlags = 0x140u;
        private const uint RewardTrackItemRewardType = 0u;
        private const uint SkeechSlayerChallengeId = 103u;
        private const uint NorthernWildsWorldId = 426u;
        private const uint NorthernWildsRuntimeWorldZoneId = 1u;
        private const uint NorthernWildsClientWorldZoneId = 35u;

        private static readonly IReadOnlyDictionary<(uint WorldId, uint RuntimeWorldZoneId), uint> ReviewedRuntimeChallengeZoneBridges =
            new Dictionary<(uint WorldId, uint RuntimeWorldZoneId), uint>
            {
                // DataMapping challenge/path evidence maps Northern Wilds runtime/source
                // zone 1 to client WorldZoneId 35. Keep the bridge runtime-owned and
                // scoped to world 426 instead of querying authoring tables at runtime.
                [(NorthernWildsWorldId, NorthernWildsRuntimeWorldZoneId)] = NorthernWildsClientWorldZoneId
            };

        private static readonly IReadOnlyDictionary<uint, uint[]> MappedChallengeCreatureTargets = new Dictionary<uint, uint[]>
        {
            [SkeechSlayerChallengeId] = [11907u, 11910u, 11912u, 11917u, 12518u, 15728u, 36884u]
        };

        private readonly IPlayer player;
        private readonly ulong characterId;
        private readonly IGameTableManager gameTableManager;
        private readonly Dictionary<ushort, ChallengeRuntimeState> activeChallenges = new();
        private readonly Dictionary<ushort, PendingChallengeShare> pendingShares = new();
        private readonly Dictionary<ushort, long> dirtyChallenges = new();

        internal ChallengeManager(IPlayer owner, IGameTableManager gameTableManager)
            : this(owner, null, gameTableManager)
        {
        }

        public ChallengeManager(IPlayer owner, CharacterModel model, IGameTableManager gameTableManager)
            : this(owner, model, gameTableManager, null)
        {
        }

        internal ChallengeManager(IPlayer owner, CharacterModel model, IGameTableManager gameTableManager,
            Func<CharacterContext> rewardContexts, IPrerequisiteManager rewardPrerequisites = null, IItemManager rewardItems = null,
            Func<AuthContext> contractAuthContexts = null)
        {
            player              = owner;
            characterId         = owner.CharacterId;
            this.gameTableManager = gameTableManager;
            this.rewardPrerequisites = rewardPrerequisites;
            this.rewardItems = rewardItems;
            this.contractAuthContexts = contractAuthContexts;
            if (rewardContexts != null) InitialiseCharacterRewards(rewardContexts);

            if (model?.Challenge == null)
                return;

            foreach (CharacterChallengeModel challengeModel in model.Challenge)
            {
                persistenceBaselines[challengeModel.ChallengeId] = CopyChallengeModel(challengeModel);
                if (gameTableManager.Challenge?.GetEntry(challengeModel.ChallengeId) == null)
                    continue;

                ushort id = challengeModel.ChallengeId;
                activeChallenges[id] = new ChallengeRuntimeState
                {
                    ChallengeId         = challengeModel.ChallengeId,
                    ActiveAttemptId     = challengeModel.ActiveAttemptId,
                    Activated           = challengeModel.Activated,
                    OnCooldown          = challengeModel.OnCooldown,
                    LeftArea            = challengeModel.LeftArea,
                    CurrentCount        = challengeModel.CurrentCount,
                    CurrentTier         = challengeModel.CurrentTier,
                    LastRewardTier      = challengeModel.LastRewardTier,
                    CompletionCount     = challengeModel.CompletionCount,
                    ActiveTimer         = challengeModel.ActiveTimerSeconds,
                    CooldownTimer       = challengeModel.CooldownTimerSeconds,
                    AreaFailTimer       = challengeModel.AreaFailTimerSeconds
                };
            }
        }

        public void Save(CharacterContext context) => SaveChallengePersistence(context);

        private CharacterChallengeModel BuildModel(ChallengeRuntimeState state)
        {
            return new CharacterChallengeModel
            {
                Id                   = characterId,
                ChallengeId          = (ushort)state.ChallengeId,
                ActiveAttemptId      = state.ActiveAttemptId,
                Activated            = state.Activated,
                OnCooldown           = state.OnCooldown,
                LeftArea             = state.LeftArea,
                CurrentCount         = state.CurrentCount,
                CurrentTier          = state.CurrentTier,
                LastRewardTier       = state.LastRewardTier,
                CompletionCount      = state.CompletionCount,
                ActiveTimerSeconds   = state.ActiveTimer,
                CooldownTimerSeconds = state.CooldownTimer,
                AreaFailTimerSeconds = state.AreaFailTimer
            };
        }

        private void MarkDirty(ushort challengeId)
        {
            lock (persistenceGate)
                dirtyChallenges[challengeId] = ++persistenceRevision;
        }

        public void SendInitialPackets()
        {
            bool firstRequest = !characterRewardPacketsRequested;
            characterRewardPacketsRequested = true;
            player.RewardTrackPresentation?.ExpectCharacterTracks();
            if (player.QuestManager is Entity.QuestManager quests) quests.ExpectContractRewardTracks(packets: true);
            if (firstRequest && committedCharacterRewards != null) PublishCharacterRewards();
            LoadCharacterRewards();
            SendChallengeUpdate();
        }

        public void Update(double lastTick)
        {
            if (!CharacterRewardsReady) return;
            UpdateContractAccounts(lastTick);
            bool challengeUpdateRequired = false;
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
                    {
                        state.OnCooldown = false;
                        state.CooldownTimer = 0d;
                        MarkDirty((ushort)state.ChallengeId);
                        challengeUpdateRequired = true;
                    }
                }

                if (state.Activated && state.LeftArea && state.AreaFailTimer > 0d)
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

            if (challengeUpdateRequired)
                SendChallengeUpdate();
        }

        public void HandleChoice(ushort challengeId, ChallengeChoice choice)
        {
            if (!CharacterRewardsReady) return;
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
            if (!CharacterRewardsReady) return;
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
            if (!CharacterRewardsReady) return;
            ChallengeEntry entry = gameTableManager.Challenge?.GetEntry(challengeId);
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
            ChallengeEntry entry = gameTableManager.Challenge?.GetEntry(challengeId);
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

            if (CountActivatedChallenges() >= RetailCertainRules.MaxConcurrentActiveChallenges)
            {
                SendResult(challengeId, ChallengeResult.GenericFail);
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

            ActivateChallenge(challengeId);
        }

        private void ActivateChallenge(ushort challengeId, uint initialProgress = 0u)
        {
            ChallengeRuntimeState state = GetOrCreateState(challengeId);
            ChallengeEntry entry = gameTableManager.Challenge?.GetEntry(challengeId);
            uint[] tierGoals = entry == null ? [] : GetTierGoalCounts(entry);
            uint goalCount = tierGoals.Length > 0 ? tierGoals[0] : 0u;
            string previousAttempt = state.ActiveAttemptId;
            bool durable = entry != null && SupportsDurableProducer(entry);
            var legacyPredecessor = durable && previousAttempt == null ? Reward.CharacterChallengeState.FromModel(BuildModel(state)) : null;
            state.ActiveAttemptId = durable ? Guid.NewGuid().ToString("N") : null;
            ulong activation = ++state.ActivationGeneration;

            state.Activated     = true;
            state.OnCooldown    = false;
            state.LeftArea      = false;
            state.CurrentCount  = goalCount == 0u ? 0u : Math.Min(initialProgress, goalCount);
            state.CurrentTier   = 0u;
            state.LastRewardTier = 0u;
            state.ActiveTimer   = DefaultActiveSeconds;
            state.AreaFailTimer = 0d;
            MarkDirty(challengeId);
            if (durable && !QueueProducerActivation(state, entry, previousAttempt, legacyPredecessor)) return;

            string attempt = state.ActiveAttemptId;
            SendChallengeUpdate(tolerateSendFailure: true);
            if (!state.Activated || state.ActivationGeneration != activation || state.ActiveAttemptId != attempt) return;
            SendResult(challengeId, ChallengeResult.Activate, tolerateSendFailure: true);

            if (entry != null && goalCount > 0u && state.Activated && state.ActivationGeneration == activation && state.ActiveAttemptId == attempt
                && state.CurrentTier == 0u && state.CurrentCount >= goalCount)
                HandleTierGoalReached(challengeId, state, entry, tierGoals);
        }

        private void TryAbandon(ushort challengeId)
        {
            if (!activeChallenges.TryGetValue(challengeId, out ChallengeRuntimeState state) || !state.Activated)
            {
                SendResult(challengeId, ChallengeResult.GenericFail);
                return;
            }

            Deactivate(state);
            MarkDirty(challengeId);
            if (ProducerMode(state) != null && !QueueProducerTransition(state, null, false)) return;
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
            Deactivate(state);

            if (HasCooldownType(state.ChallengeId))
            {
                state.OnCooldown     = true;
                state.CooldownTimer  = DefaultCooldownSeconds;
            }

            MarkDirty((ushort)state.ChallengeId);
            if (ProducerMode(state) != null && !QueueProducerTransition(state, null, false)) return;
            SendResult((ushort)state.ChallengeId, ChallengeResult.TimerExpired);
            SendChallengeUpdate();
        }

        private void HandleAreaFailExpired(ChallengeRuntimeState state)
        {
            Deactivate(state);
            MarkDirty((ushort)state.ChallengeId);
            if (ProducerMode(state) != null && !QueueProducerTransition(state, null, false)) return;
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

                ChallengeEntry entry = gameTableManager.Challenge?.GetEntry(state.ChallengeId);
                if (entry != null && entry.ChallengeTypeEnum == challengeType)
                    return true;
            }

            return false;
        }

        private int CountActivatedChallenges()
        {
            int count = 0;
            foreach (ChallengeRuntimeState state in activeChallenges.Values)
            {
                if (state.Activated)
                    count++;
            }

            return count;
        }

        private bool MeetsZoneRestriction(ChallengeEntry entry)
        {
            if (entry.WorldZoneIdRestriction == 0u || player.Zone == null)
                return true;

            return IsZoneOrDescendantOfRestriction(player.Zone, entry.WorldZoneIdRestriction)
                || MatchesReviewedRuntimeZoneBridge(entry.WorldZoneIdRestriction);
        }

        private bool MeetsAutoActivationZoneRestriction(ChallengeEntry entry)
        {
            if (entry.WorldZoneIdRestriction == 0u)
                return true;

            if (player.Zone == null)
                return false;

            return IsZoneOrDescendantOfRestriction(player.Zone, entry.WorldZoneIdRestriction)
                || IsRestrictionDescendantOfZone(player.Zone, entry.WorldZoneIdRestriction)
                || MatchesReviewedRuntimeZoneBridge(entry.WorldZoneIdRestriction);
        }

        private bool MatchesReviewedRuntimeZoneBridge(uint restrictionId)
        {
            uint worldId = player.Map?.Entry?.Id ?? 0u;
            uint runtimeZoneId = player.Zone?.Id ?? 0u;
            if (!ReviewedRuntimeChallengeZoneBridges.TryGetValue((worldId, runtimeZoneId), out uint bridgedWorldZoneId))
                return false;

            if (restrictionId == bridgedWorldZoneId)
                return true;

            WorldZoneEntry restrictedZone = gameTableManager.WorldZone?.GetEntry(restrictionId);
            return restrictedZone != null && ZoneLineageContains(restrictedZone, bridgedWorldZoneId);
        }

        private bool IsZoneOrDescendantOfRestriction(WorldZoneEntry zone, uint restrictionId)
        {
            return ZoneLineageContains(zone, restrictionId);
        }

        private bool IsRestrictionDescendantOfZone(WorldZoneEntry zone, uint restrictionId)
        {
            WorldZoneEntry restrictedZone = gameTableManager.WorldZone?.GetEntry(restrictionId);
            return restrictedZone != null && ZoneLineageContains(restrictedZone, zone.Id);
        }

        private bool ZoneLineageContains(WorldZoneEntry zone, uint expectedZoneId)
        {
            if (zone == null || expectedZoneId == 0u)
                return false;

            var visitedZoneIds = new HashSet<uint>();
            WorldZoneEntry current = zone;
            while (current != null && visitedZoneIds.Add(current.Id))
            {
                if (current.Id == expectedZoneId)
                    return true;

                if (current.ParentZoneId == 0u)
                    return false;

                current = gameTableManager.WorldZone?.GetEntry(current.ParentZoneId);
            }

            return false;
        }

        private bool HasCooldownType(uint challengeId)
        {
            ChallengeEntry entry = gameTableManager.Challenge?.GetEntry(challengeId);
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
                MarkDirty(id);
            }

            return state;
        }

        internal void TryAdvanceCombatKill(uint creatureId)
        {
            if (!CharacterRewardsReady) return;
            if (TryAdvanceMatchingChallenges(creatureId, static type => type == (uint)ChallengeType.Combat))
                return;

            TryAutoActivateCombatKill(creatureId);
        }

        private bool TryAdvanceMatchingChallenges(uint creatureId, Func<uint, bool> matchesType)
        {
            // Progress and completion callbacks can activate or restart another
            // Challenge. That new attempt cannot consume this earlier event.
            var attempts = activeChallenges.Where(pair => pair.Value.Activated)
                .Select(pair => (pair.Key, pair.Value, pair.Value.ActivationGeneration, pair.Value.ActiveAttemptId))
                .ToArray();
            bool advanced = false;
            foreach (var (challengeId, state, activation, attempt) in attempts)
            {
                if (!state.Activated || state.ActivationGeneration != activation || state.ActiveAttemptId != attempt)
                    continue;

                ChallengeEntry entry = gameTableManager.Challenge?.GetEntry(challengeId);
                if (entry == null || !matchesType(entry.ChallengeTypeEnum))
                    continue;

                if (!MatchesChallengeTarget(entry, creatureId))
                    continue;

                if (TryAdvanceProgress(challengeId))
                    advanced = true;
            }

            return advanced;
        }

        private void TryAutoActivateCombatKill(uint creatureId)
        {
            if (creatureId == 0u)
                return;

            if (HasActiveChallengeOfType((uint)ChallengeType.Combat))
                return;

            if (CountActivatedChallenges() >= RetailCertainRules.MaxConcurrentActiveChallenges)
                return;

            foreach (ChallengeEntry entry in gameTableManager.Challenge?.Entries ?? [])
            {
                if (entry.Id > ushort.MaxValue)
                    continue;

                if (entry.ChallengeTypeEnum != (uint)ChallengeType.Combat)
                    continue;

                if (!HasAutoActivateOnProgressFlag(entry))
                    continue;

                ushort challengeId = (ushort)entry.Id;
                if (activeChallenges.TryGetValue(challengeId, out ChallengeRuntimeState existing)
                    && (existing.Activated || existing.OnCooldown))
                    continue;

                if (!MatchesChallengeTarget(entry, creatureId))
                    continue;

                if (!MeetsAutoActivationZoneRestriction(entry))
                    continue;

                ActivateChallenge(challengeId, 1u);
                return;
            }
        }

        private static bool HasAutoActivateOnProgressFlag(ChallengeEntry entry)
        {
            return (entry.ChallengeFlags & AutoActivateOnProgressFlag) != 0u;
        }

        internal void TryAdvanceActivationTarget(uint creatureId)
        {
            if (CharacterRewardsReady)
                TryAdvanceMatchingChallenges(creatureId, IsActivationChallengeType);
        }

        private static bool IsActivationChallengeType(uint challengeType)
        {
            return challengeType == (uint)ChallengeType.Ability
                || challengeType == (uint)ChallengeType.ChecklistActivate;
        }

        private bool MatchesChallengeTarget(ChallengeEntry entry, uint creatureId)
        {
            if (creatureId == 0u)
                return false;

            if (entry.Target == 0u)
                return MatchesMappedChallengeCreatureTarget(entry, creatureId);

            TargetGroupEntry targetGroup = gameTableManager.TargetGroup?.GetEntry(entry.Target);
            bool groupTarget = entry.ChallengeTypeEnum is (uint)ChallengeType.Combat or (uint)ChallengeType.ChecklistActivate;
            // Known combat/checklist groups own their creature namespace. Ability
            // and direct targets without a group retain their existing binding.
            if (entry.Target == creatureId && (!groupTarget || targetGroup == null))
                return true;

            return TargetGroupContainsCreature(targetGroup, creatureId, new HashSet<uint>())
                || MatchesMappedChallengeCreatureTarget(entry, creatureId);
        }

        private static bool MatchesMappedChallengeCreatureTarget(ChallengeEntry entry, uint creatureId)
        {
            return MappedChallengeCreatureTargets.TryGetValue(entry.Id, out uint[] mappedCreatureIds)
                && mappedCreatureIds.Contains(creatureId);
        }

        private bool TargetGroupContainsCreature(TargetGroupEntry entry, uint creatureId, ISet<uint> visitedTargetGroups)
        {
            if (entry == null || !visitedTargetGroups.Add(entry.Id))
                return false;

            switch ((TargetGroupType)entry.Type)
            {
                case TargetGroupType.CreatureIdGroup:
                case TargetGroupType.CreatureIdListGroup:
                    return entry.DataEntries?.Any(id => id == creatureId) == true;
                case TargetGroupType.OtherTargetGroup:
                case TargetGroupType.OtherTargetGroupCreatures:
                    foreach (uint targetGroupId in entry.DataEntries?.Where(id => id != 0u) ?? [])
                    {
                        TargetGroupEntry childEntry = gameTableManager.TargetGroup?.GetEntry(targetGroupId);
                        if (TargetGroupContainsCreature(childEntry, creatureId, visitedTargetGroups))
                            return true;
                    }

                    return false;
                default:
                    return false;
            }
        }

        public bool TryAdvanceProgress(ushort challengeId, uint progress = 1u)
        {
            if (!CharacterRewardsReady) return false;
            if (!activeChallenges.TryGetValue(challengeId, out ChallengeRuntimeState state) || !state.Activated)
                return false;

            ChallengeEntry entry = gameTableManager.Challenge?.GetEntry(challengeId);
            if (entry == null)
                return false;

            uint[] tierGoals = GetTierGoalCounts(entry);
            uint goalCount = tierGoals.Length > 0 ? tierGoals[Math.Min(state.CurrentTier, (uint)tierGoals.Length - 1)] : 0u;
            if (goalCount == 0u)
                return false;

            state.CurrentCount = (uint)Math.Min((ulong)state.CurrentCount + progress, goalCount);
            MarkDirty(challengeId);
            string attempt = state.ActiveAttemptId;
            ulong activation = state.ActivationGeneration;
            uint tier = state.CurrentTier;
            SendChallengeUpdate(tolerateSendFailure: true);

            if (!state.Activated || state.ActivationGeneration != activation || state.ActiveAttemptId != attempt
                || state.CurrentTier != tier || state.CurrentCount < goalCount)
                return true;

            HandleTierGoalReached(challengeId, state, entry, tierGoals);
            return true;
        }

        private void HandleTierGoalReached(
            ushort challengeId,
            ChallengeRuntimeState state,
            ChallengeEntry entry,
            uint[] tierGoals)
        {
            uint achievedTier = state.CurrentTier;
            var mode = ProducerMode(state);
            NexusForever.Game.Abstract.Account.Reward.ChallengeRewardModifierSnapshot modifier = default;
            if (mode?.Mode == Reward.CharacterChallengeRewardMode.CharacterTrack && !CaptureProducerModifier(out modifier))
            { RejectProducer(); return; }
            state.LastRewardTier = achievedTier;

            if (achievedTier + 1u >= tierGoals.Length)
            {
                CompleteChallenge(state, entry, mode != null ? (byte)achievedTier : null, modifier);
                return;
            }

            state.CurrentTier++;
            state.CurrentCount = 0u;
            MarkDirty(challengeId);
            if (mode != null && !QueueProducerTransition(state, (byte)achievedTier, false, modifier)) return;
            if (mode == null)
                SendResult(challengeId, ChallengeResult.TierAchieved, (int)achievedTier, tolerateSendFailure: true);
            SendChallengeUpdate(tolerateSendFailure: true);
        }

        private void CompleteChallenge(ChallengeRuntimeState state, ChallengeEntry entry, byte? medal = null,
            NexusForever.Game.Abstract.Account.Reward.ChallengeRewardModifierSnapshot modifier = default)
        {
            Deactivate(state);
            state.OnCooldown = HasCooldownType(state.ChallengeId);
            if (state.OnCooldown)
                state.CooldownTimer = DefaultCooldownSeconds;

            state.CompletionCount++;
            MarkDirty((ushort)state.ChallengeId);
            if (medal.HasValue)
            {
                if (!QueueProducerTransition(state, medal, true, modifier)) return;
            }
            else
            {
                // Legacy attempts have no persisted attempt ID. Finish gameplay before an immediate notice can reenter activation.
                ulong activation = state.ActivationGeneration;
                var session = player.Session;
                uint tier = state.LastRewardTier;
                bool OwnsResults() => state.ActivationGeneration == activation && ReferenceEquals(player.Session, session);
                GrantRewardTrackItem(entry, tier);
                player.QuestManager.ObjectiveUpdate(QuestObjectiveType.CompleteChallenge, 0u, 1u);
                if (!OwnsResults()) return;
                SendResult((ushort)state.ChallengeId, ChallengeResult.TierAchieved, (int)tier, tolerateSendFailure: true);
                if (!OwnsResults()) return;
                SendResult((ushort)state.ChallengeId, ChallengeResult.Completed, (int)tier, tolerateSendFailure: true);
                if (!OwnsResults()) return;
            }
            SendChallengeUpdate(tolerateSendFailure: true);
        }

        private void GrantRewardTrackItem(ChallengeEntry entry, uint achievedTier)
        {
            if (entry.RewardTrackId == 0u || achievedTier >= 32u || player.Inventory == null)
                return;

            RewardTrackEntry rewardTrack = gameTableManager.RewardTrack?.GetEntry(entry.RewardTrackId);
            if (rewardTrack == null || gameTableManager.RewardTrackRewards?.Entries == null)
                return;

            uint rewardPointFlag = 1u << (int)achievedTier;
            RewardTrackRewardsEntry reward = gameTableManager.RewardTrackRewards.Entries
                .Where(r => r.RewardTrackId == rewardTrack.Id && (r.RewardPointFlags & rewardPointFlag) != 0u)
                .OrderBy(r => r.Id)
                .FirstOrDefault();
            if (reward == null)
                return;

            if (TryGrantRewardTrackChoice(reward.RewardTrackRewardTypeEnum00, reward.RewardChoiceId00, reward.RewardChoiceCount00))
                return;

            if (TryGrantRewardTrackChoice(reward.RewardTrackRewardTypeEnum01, reward.RewardChoiceId01, reward.RewardChoiceCount01))
                return;

            TryGrantRewardTrackChoice(reward.RewardTrackRewardTypeEnum02, reward.RewardChoiceId02, reward.RewardChoiceCount02);
        }

        private bool TryGrantRewardTrackChoice(uint rewardType, uint choiceId, uint count)
        {
            if (rewardType != RewardTrackItemRewardType || choiceId == 0u)
                return false;

            player.Inventory.ItemCreate(
                InventoryLocation.Inventory,
                choiceId,
                count == 0u ? 1u : count,
                ItemUpdateReason.Challenge);
            return true;
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

            ChallengeTierEntry tier = gameTableManager.ChallengeTier?.GetEntry(tierId);
            return tier?.Count ?? 0u;
        }

        private void SendResult(ushort challengeId, ChallengeResult result, int data = 0, bool tolerateSendFailure = false)
        {
            SendChallengePacket(new ServerChallengeResult
            {
                ChallengeId = challengeId,
                Result      = result,
                Data        = data
            }, tolerateSendFailure);
        }

        private void SendChallengeUpdate(bool tolerateSendFailure = false)
        {
            var update = new ServerChallengeUpdate();
            foreach (ChallengeRuntimeState state in activeChallenges.Values)
            {
                ChallengeEntry entry = gameTableManager.Challenge?.GetEntry(state.ChallengeId);
                if (entry == null)
                    continue;

                uint[] tierGoals = GetTierGoalCounts(entry);
                uint goalCount = tierGoals.Length > 0
                    ? tierGoals[Math.Min(state.CurrentTier, (uint)tierGoals.Length - 1)]
                    : 0u;
                bool leftArea = state.Activated && state.LeftArea;

                update.ActiveChallenges.Add(new ServerChallengeUpdate.Challenge
                {
                    ChallengeId         = state.ChallengeId,
                    Type                  = (ChallengeType)entry.ChallengeTypeEnum,
                    TargetGroupId         = ResolveRewardPaneTargetGroupId(entry),
                    QualifyCount          = 0u,
                    QualityTotal          = 0u,
                    CurrentCount          = state.CurrentCount,
                    GoalCount             = goalCount,
                    ObjectiveCompletion   = CalculateSingleTierMaxScoreCompletionPercentage(entry, state, goalCount, tierGoals),
                    CurrentTier           = state.CurrentTier,
                    LastRewardTier        = state.LastRewardTier,
                    CompletionCount       = state.CompletionCount,
                    Unlocked              = true,
                    Activated             = state.Activated,
                    OnCooldown            = state.OnCooldown,
                    LeftArea              = leftArea,
                    TimeActivatedDt       = state.Activated ? ToTimerMilliseconds(state.ActiveTimer) : 0u,
                    TimeTotalActive       = ToTimerMilliseconds(DefaultActiveSeconds),
                    TimeCooldownDt        = state.OnCooldown ? ToTimerMilliseconds(state.CooldownTimer) : 0u,
                    TimeTotalCooldown     = ToTimerMilliseconds(DefaultCooldownSeconds),
                    TimeAreaFailDt        = leftArea ? ToTimerMilliseconds(state.AreaFailTimer) : 0u,
                    TimeTotalAreaFail     = ToTimerMilliseconds(DefaultAreaFailSeconds),
                    TierGoalCount         = PadTierGoals(tierGoals)
                });
            }

            SendChallengePacket(update, tolerateSendFailure);
        }

        private void SendChallengePacket(IWritable packet, bool tolerateSendFailure)
        {
            try { player.Session?.EnqueueMessageEncrypted(packet); }
            catch (Exception error) when (tolerateSendFailure)
            {
                challengePublicationLog.Warn(error, "Live Challenge packet enqueue failed.");
            }
        }

        private uint ResolveRewardPaneTargetGroupId(ChallengeEntry entry)
        {
            if (entry.TargetGroupIdRewardPane != 0u)
                return entry.TargetGroupIdRewardPane;

            if (entry.Target != 0u && gameTableManager.TargetGroup?.GetEntry(entry.Target) != null)
                return entry.Target;

            return 0u;
        }

        private static uint ToTimerMilliseconds(double seconds)
        {
            if (seconds <= 0d)
                return 0u;

            return (uint)Math.Min(uint.MaxValue, Math.Round(seconds * 1000d));
        }

        private static uint[] PadTierGoals(uint[] tierGoals)
        {
            uint[] padded = new uint[3];
            for (int i = 0; i < padded.Length && i < tierGoals.Length; i++)
                padded[i] = tierGoals[i];

            return padded;
        }

        private static uint CalculateSingleTierMaxScoreCompletionPercentage(
            ChallengeEntry entry,
            ChallengeRuntimeState state,
            uint goalCount,
            uint[] tierGoals)
        {
            if (entry.ChallengeTypeEnum != (uint)ChallengeType.Combat
                || (entry.ChallengeFlags & MaxScoreExcludedFlags) != 0u
                || tierGoals.Length != 1
                || goalCount <= 1u)
                return 0u;

            ulong scaledProgress = Math.Min(state.CurrentCount, goalCount) * 100ul;
            return (uint)Math.Min(scaledProgress / goalCount, 100ul);
        }

        /// <summary>
        /// Counts challenges with at least one recorded completion in the supplied world-zone set.
        /// Completion counts persisted rows with completionCount &gt; 0.
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

                ChallengeEntry entry = gameTableManager.Challenge?.GetEntry(challengeId);
                if (entry == null)
                    continue;

                if (worldZoneIds.Contains(entry.WorldZoneId)
                    || (entry.WorldZoneIdRestriction != 0u && worldZoneIds.Contains(entry.WorldZoneIdRestriction)))
                    count++;
            }

            return count;
        }

        public uint GetCompletionCount(ushort challengeId)
        {
            return activeChallenges.TryGetValue(challengeId, out ChallengeRuntimeState state)
                ? state.CompletionCount
                : 0u;
        }

        public bool IsChallengeActivated(ushort challengeId)
        {
            return activeChallenges.TryGetValue(challengeId, out ChallengeRuntimeState state) && state.Activated;
        }
    }
}
