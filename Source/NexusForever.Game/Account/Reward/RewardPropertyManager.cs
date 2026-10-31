using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Account.Reward;
using NexusForever.Game.Abstract.Account.Entitlement;
using NexusForever.Game.Account.Entitlement;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Reward;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model.Reward;

namespace NexusForever.Game.Account.Reward
{
    public partial class RewardPropertyManager : IRewardPropertyManager
    {
        private readonly IAccount account;
        private readonly IAssetManager assetManager;
        private readonly IGameTableManager gameTableManager;
        private readonly Dictionary<RewardPropertyType, IRewardProperty> rewardProperties = new();
        private readonly object gate = new();
        private ChallengeRewardProperty challenge;
        private IAccountEntitlement committedLoyaltyOwner;
        private ulong committedLoyaltyRevision;
        private uint committedLoyaltyAmount;
        private bool initialPacketsSent;

        public RewardPropertyManager(IAccount account, IAssetManager assetManager, IGameTableManager gameTableManager)
        {
            this.account          = account;
            this.assetManager     = assetManager;
            this.gameTableManager = gameTableManager;
            if (account.EntitlementManager?.GetEntitlement(EntitlementType.LoyaltyChallengeBonus) is AccountEntitlement entitlement)
            {
                var snapshot = entitlement.GetCommittedSnapshot();
                committedLoyaltyOwner = entitlement;
                committedLoyaltyRevision = snapshot.Revision;
                committedLoyaltyAmount = snapshot.Amount;
            }
            UpdateRewardPropertiesPremiumModifiers(null);
        }

        public void Initialise(IPlayer player)
        {
            UpdateRewardPropertiesPremiumModifiers(player);
        }

        private void UpdateRewardPropertiesPremiumModifiers(IPlayer player)
        {
            lock (gate)
            {
                rewardProperties.Clear();
                RewardPropertyEntry challengeEntry = gameTableManager.RewardProperty?.GetEntry((uint)RewardPropertyType.RewardTrackType);
                if (challengeEntry?.RewardModifierValueTypeEnum == (uint)RewardModifierValueType.AdditiveScalar)
                {
                    challenge ??= new ChallengeRewardProperty(challengeEntry, gate);
                    challenge.Initialise(player);
                    challenge.SetCommittedLoyalty(committedLoyaltyAmount);
                    rewardProperties.Add(RewardPropertyType.RewardTrackType, challenge);
                }
                var challengePremium = new List<(uint Id, float Value)>();
                var contractPremium = new[] { new List<(uint Id, float Value)>(), new List<(uint Id, float Value)>() };

                foreach (RewardPropertyPremiumModifierEntry modifierEntry in assetManager?.GetRewardPropertiesForTier(account.AccountTier) ?? Enumerable.Empty<RewardPropertyPremiumModifierEntry>())
                {
                    RewardPropertyEntry entry = gameTableManager.RewardProperty?.GetEntry(modifierEntry.RewardPropertyId);
                    if (entry == null)
                        continue;

                    if (!TryGetPremiumModiferValue(entry, modifierEntry, player, out float value))
                        continue;

                    if (challenge != null && entry.Id == (uint)RewardPropertyType.RewardTrackType && modifierEntry.RewardPropertyData == 3)
                    {
                        challengePremium.Add((modifierEntry.Id, value));
                        continue;
                    }
                    if (challenge != null && entry.Id == (uint)RewardPropertyType.RewardTrackType && modifierEntry.RewardPropertyData < 2)
                    {
                        contractPremium[modifierEntry.RewardPropertyData].Add((modifierEntry.Id, value));
                        continue;
                    }

                    // update reward without sending
                    IRewardProperty rewardProperty = GetRewardProperty(entry);
                    rewardProperty.UpdateValue(modifierEntry.RewardPropertyData, value);
                }
                challenge?.SetPremium(challengePremium.ToArray());
                for (uint type = 0; type < 2; ++type) challenge?.SetContractPremium(type, contractPremium[type].ToArray());
            }
        }

        private bool TryGetPremiumModiferValue(RewardPropertyEntry entry, RewardPropertyPremiumModifierEntry modifierEntry, IPlayer player, out float value)
        {
            value = 0f;

            // some reward property premium modifier entries use an existing entitlement values rather than static values
            if (modifierEntry.EntitlementIdModifierCount != 0u)
            {
                EntitlementEntry entitlementEntry = gameTableManager.Entitlement?.GetEntry(modifierEntry.EntitlementIdModifierCount);
                if (entitlementEntry == null)
                    return false;

                EntitlementFlags entitlementFlags = (EntitlementFlags)entitlementEntry.Flags;
                if (entitlementFlags.HasFlag(EntitlementFlags.Character))
                    value = player?.EntitlementManager?.GetEntitlement((EntitlementType)modifierEntry.EntitlementIdModifierCount)?.Amount ?? 0u;
                else
                    value = account.EntitlementManager?.GetEntitlement((EntitlementType)modifierEntry.EntitlementIdModifierCount)?.Amount ?? 0u;

                return true;
            }
            value = (RewardModifierValueType)entry.RewardModifierValueTypeEnum switch
            {
                RewardModifierValueType.AdditiveScalar       => modifierEntry.ModifierValueFloat,
                RewardModifierValueType.Discrete             => modifierEntry.ModifierValueInt,
                RewardModifierValueType.MultiplicativeScalar => modifierEntry.ModifierValueFloat,
                _ => 0f
            };
            return true;
        }

        public void SendInitialPackets()
        {
            lock (gate)
            {
                account.Session.EnqueueMessageEncrypted(new ServerPremiumRewards
                {
                    Properties = rewardProperties.Values
                        .SelectMany(e => e.Build())
                        .ToList()
                });
                initialPacketsSent = true;
            }
        }

        /// <summary>
        /// Returns a <see cref="IRewardProperty"/> with the supplied <see cref="RewardPropertyType"/>.
        /// </summary>
        public IRewardProperty GetRewardProperty(RewardPropertyType type)
        {
            lock (gate) return rewardProperties.TryGetValue(type, out IRewardProperty rewardProperty) ? rewardProperty : null;
        }

        /// <summary>
        /// Update <see cref="RewardPropertyType"/> with supplied value and data.
        /// </summary>
        /// <remarks>
        /// A positive value will increment and a negative value will decrement the value.
        /// </remarks>
        public void UpdateRewardProperty(RewardPropertyType type, float value, uint data = 0u)
        {
            RewardPropertyEntry entry = gameTableManager.RewardProperty?.GetEntry((ulong)type);
            if (entry == null)
                return;

            UpdateRewardProperty(entry, value, data);
        }

        /// <summary>
        /// Update <see cref="RewardPropertyEntry"/> with supplied value and data.
        /// </summary>
        /// <remarks>
        /// A positive value will increment and a negative value will decrement the value.
        /// </remarks>
        public void UpdateRewardProperty(RewardPropertyEntry entry, float value, uint data = 0u)
        {
            lock (gate)
            {
                IRewardProperty rewardProperty = GetRewardProperty(entry);
                rewardProperty.UpdateValue(data, value);

                account.Session.EnqueueMessageEncrypted(new ServerPremiumRewards
                {
                    Properties = rewardProperty.Build().ToList()
                });
            }
        }

        public void UpdateChallengeSpellModifier(IPlayer actor, uint effectId, uint spellId, float value, bool removed)
        {
            lock (gate)
            {
                if (challenge == null)
                {
                    UpdateRewardProperty(RewardPropertyType.RewardTrackType, removed ? -value : value, 3);
                    return;
                }
                if (challenge.UpdateSpell(actor, effectId, spellId, value, removed))
                    account.Session.EnqueueMessageEncrypted(new ServerPremiumRewards { Properties = challenge.Build().ToList() });
            }
        }

        public void RefreshCommittedEntitlement(IAccountEntitlement entitlement, uint amount, ulong revision)
        {
            if (entitlement is not AccountEntitlement real || entitlement.Type != EntitlementType.LoyaltyChallengeBonus
                || !ReferenceEquals(account.EntitlementManager?.GetEntitlement(entitlement.Type), entitlement)) return;
            // Read before taking the projection lock: terminal notifications never invert the entitlement lock.
            var durable = real.GetCommittedSnapshot();
            if (durable.Revision != revision || durable.Amount != amount) return;
            lock (gate)
            {
                if (committedLoyaltyOwner != null && !ReferenceEquals(committedLoyaltyOwner, entitlement)) return;
                if (revision < committedLoyaltyRevision) return;
                bool changed = amount != committedLoyaltyAmount;
                committedLoyaltyOwner = entitlement;
                committedLoyaltyRevision = revision;
                committedLoyaltyAmount = amount;
                challenge?.SetCommittedLoyalty(amount);
                if (changed && initialPacketsSent && challenge != null)
                    account.Session.EnqueueMessageEncrypted(new ServerPremiumRewards { Properties = challenge.Build().ToList() });
            }
        }

        public bool CaptureChallengeModifier(out ChallengeRewardModifierSnapshot snapshot)
        {
            lock (gate)
            {
                if (challenge == null || challenge.Entry.Id != (uint)RewardPropertyType.RewardTrackType
                    || challenge.Entry.RewardModifierValueTypeEnum != (uint)RewardModifierValueType.AdditiveScalar
                    || !rewardProperties.TryGetValue(RewardPropertyType.RewardTrackType, out IRewardProperty current)
                    || !ReferenceEquals(current, challenge))
                {
                    snapshot = default;
                    return false;
                }
                snapshot = challenge.CaptureModifier();
                return true;
            }
        }

        /// <summary>
        /// Returns an <see cref="IRewardProperty"/> with supplied <see cref="RewardPropertyEntry"/>.
        /// </summary>
        /// <remarks>
        /// If <see cref="IRewardProperty"/> doesn't exist, a new one will be created and returned.
        /// </remarks>
        private IRewardProperty GetRewardProperty(RewardPropertyEntry entry)
        {
            RewardPropertyType type = (RewardPropertyType)entry.Id;
            if (!rewardProperties.TryGetValue(type, out IRewardProperty rewardProperty))
            {
                rewardProperty = new RewardProperty(entry);
                rewardProperties.Add(type, rewardProperty);
            }

            return rewardProperty;
        }
    }
}
