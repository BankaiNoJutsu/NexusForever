using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Account.Reward;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Reward;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model.Reward;

namespace NexusForever.Game.Account.Reward
{
    public class RewardPropertyManager : IRewardPropertyManager
    {
        private readonly IAccount account;
        private readonly IAssetManager assetManager;
        private readonly IGameTableManager gameTableManager;
        private readonly Dictionary<RewardPropertyType, IRewardProperty> rewardProperties = new();

        public RewardPropertyManager(IAccount account, IAssetManager assetManager, IGameTableManager gameTableManager)
        {
            this.account          = account;
            this.assetManager     = assetManager;
            this.gameTableManager = gameTableManager;
            UpdateRewardPropertiesPremiumModifiers(null);
        }

        public void Initialise(IPlayer player)
        {
            UpdateRewardPropertiesPremiumModifiers(player);
        }

        private void UpdateRewardPropertiesPremiumModifiers(IPlayer player)
        {
            rewardProperties.Clear();

            foreach (RewardPropertyPremiumModifierEntry modifierEntry in assetManager?.GetRewardPropertiesForTier(account.AccountTier) ?? Enumerable.Empty<RewardPropertyPremiumModifierEntry>())
            {
                RewardPropertyEntry entry = gameTableManager.RewardProperty?.GetEntry(modifierEntry.RewardPropertyId);
                if (entry == null)
                    continue;

                if (!TryGetPremiumModiferValue(entry, modifierEntry, player, out float value))
                    continue;

                // update reward without sending
                IRewardProperty rewardProperty = GetRewardProperty(entry);
                rewardProperty.UpdateValue(modifierEntry.RewardPropertyData, value);
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
                if (entitlementFlags.HasFlag(EntitlementFlags.Disabled))
                    return true;

                if (entitlementFlags.HasFlag(EntitlementFlags.Character))
                    value = player?.EntitlementManager?.GetEntitlement((EntitlementType)modifierEntry.EntitlementIdModifierCount)?.Amount ?? 0u;
                else
                    value = account.EntitlementManager?.GetEntitlement((EntitlementType)modifierEntry.EntitlementIdModifierCount)?.Amount ?? 0u;

                return true;
            }
            value = (RewardPropertyModifierValueType)entry.RewardModifierValueTypeEnum switch
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
            account.Session.EnqueueMessageEncrypted(new ServerPremiumRewards
            {
                Properties = rewardProperties.Values
                    .SelectMany(e => e.Build())
                    .ToList()
            });
        }

        /// <summary>
        /// Returns a <see cref="IRewardProperty"/> with the supplied <see cref="RewardPropertyType"/>.
        /// </summary>
        public IRewardProperty GetRewardProperty(RewardPropertyType type)
        {
            return rewardProperties.TryGetValue(type, out IRewardProperty rewardProperty) ? rewardProperty : null;
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
            IRewardProperty rewardProperty = GetRewardProperty(entry);
            rewardProperty.UpdateValue(data, value);

            account.Session.EnqueueMessageEncrypted(new ServerPremiumRewards
            {
                Properties = rewardProperty.Build().ToList()
            });
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
