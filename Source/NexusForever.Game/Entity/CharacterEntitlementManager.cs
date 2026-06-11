using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entitlement;
using NexusForever.Game.Static.Entity;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Entity
{
    public class CharacterEntitlementManager : EntitlementManager<ICharacterEntitlement>, ICharacterEntitlementManager
    {
        private readonly IPlayer player;
        private readonly IAssetManager assetManager;

        /// <summary>
        /// Create a new <see cref="ICharacterEntitlementManager"/> from existing database model.
        /// </summary>
        public CharacterEntitlementManager(IPlayer player, CharacterModel model, IAssetManager assetManager, IGameTableManager gameTableManager)
            : base(gameTableManager)
        {
            this.player       = player;
            this.assetManager = assetManager;

            foreach (CharacterEntitlementModel entitlementModel in model.Entitlement)
            {
                EntitlementEntry entry = gameTableManager.Entitlement?.GetEntry(entitlementModel.EntitlementId);
                if (entry == null)
                    throw new DatabaseDataException($"Account {model.Id} has invalid entitlement {entitlementModel.EntitlementId} stored!");

                var entitlement = new CharacterEntitlement(player, entitlementModel, entry);
                entitlements.Add(entitlement.Type, entitlement);
            }
        }

        public void Save(CharacterContext context)
        {
            foreach (ICharacterEntitlement entitlement in entitlements.Values)
                entitlement.Save(context);
        }

        protected override bool CanUpdateEntitlement(EntitlementEntry entry, int value)
        {
            // only handle character entitlements in CharacterEntitlementManager
            EntitlementFlags entitlementFlags = (EntitlementFlags)entry.Flags;
            if (!entitlementFlags.HasFlag(EntitlementFlags.Character))
                return false;

            return base.CanUpdateEntitlement(entry, value);
        }

        protected override void UpdateEntitlement(EntitlementEntry entry, int value)
        {
            base.UpdateEntitlement(entry, value);

            // some reward property premium modifier entries use an existing entitlement values rather than static values
            // make sure we update these when the entitlement changes
            foreach (RewardPropertyPremiumModifierEntry modifierEntry in (assetManager?.GetRewardPropertiesForTier(player.Account.AccountTier) ?? Enumerable.Empty<RewardPropertyPremiumModifierEntry>())
                .Where(e => e.EntitlementIdModifierCount == entry.Id))
            {
                player.Account.RewardPropertyManager.UpdateRewardProperty((RewardPropertyType)modifierEntry.RewardPropertyId, value, modifierEntry.RewardPropertyData);
            }
        }

        protected override ICharacterEntitlement CreateEntitlement(EntitlementEntry type)
        {
            return new CharacterEntitlement(player, type, 0);
        }

        protected override void SendEntitlement(ICharacterEntitlement entitlement)
        {
            player.Session.EnqueueMessageEncrypted(entitlement.Build());
        }
    }
}
