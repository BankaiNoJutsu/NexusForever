using NexusForever.Database.Auth;
using NexusForever.Game.Challenges;

namespace NexusForever.Game.Entity
{
    public partial class Player
    {
        private NormalRewardSave normalRewardSave;

        // The queue has already discovered this identity on its worker. Capturing
        // normal-save targets here performs no provider initialization or SQL.
        internal NormalRewardSave CaptureNormalRewardSave(ChallengeManager.RewardPersistenceIdentity identity = null)
        {
            if (normalRewardSave != null) throw new InvalidOperationException("Player already has a frozen normal save.");
            if (QuestManager is not QuestManager quests || ChallengeManager is not ChallengeManager challenges) return null;
            identity ??= challenges.NormalSaveIdentity;
            if (identity == null) return null; // existing startup/legacy path; reward capture still refuses overlap
            var questSnapshot = HousingNameSaveAllowed ? housingNameActor.CaptureQuests(quests, identity.ExecutionDatabase)
                : AuctionBidSaveAllowed ? auctionBidActor.CaptureQuestReconciliation(quests, identity.ExecutionDatabase)
                : BankSaveAllowed ? bankActor.CaptureQuests(quests, identity.ExecutionDatabase)
                : quests.CaptureNormalSave(identity.ExecutionDatabase);
            try
            {
                long taxiFareVersion = TaxiFareVersion;
                var currencySnapshot = CurrencyManager is not CurrencyManager currencies ? null
                    : HousingNameSaveAllowed ? housingNameActor.CaptureCurrencies(questSnapshot.Token)
                    : AuctionBidSaveAllowed ? currencies.CaptureAuctionBidNormalSave(questSnapshot.Token, auctionBidActor.Wallet)
                    : BankSaveAllowed && bankActor.Wallet != null ? bankActor.Wallet.CaptureNormalSave(questSnapshot.Token)
                    : currencies.CaptureNormalSave(questSnapshot.Token);
                var wardrobeSnapshot = (CostumeManager as CostumeManager)?.CaptureNormalSave(questSnapshot.Token, identity.ExecutionDatabase);
                var titleSnapshot = (TitleManager as TitleManager)?.CaptureNormalSave(questSnapshot.Token, identity.ExecutionDatabase);
                var progressionSnapshot = CaptureProgressionSave(questSnapshot.Token, identity.ExecutionDatabase);
                var experienceSnapshot = CaptureExperienceSave(questSnapshot.Token, identity.ExecutionDatabase);
                // Bid reconciliation owns its specialized escrow/delivery
                // witnesses. Ordinary saves freeze accepted Item membership here.
                var inventorySnapshot = AuctionBidSaveAllowed ? null : (Inventory as Inventory)?.CaptureNormalSave(identity.ExecutionDatabase);
                var satchelSnapshot = (SupplySatchelManager as SupplySatchelManager)?.CaptureNormalSave(questSnapshot.Token, identity.ExecutionDatabase);
                var keybindingSnapshot = (KeybindingManager as CharacterKeybindingManager)?.CaptureNormalSave(questSnapshot.Token, identity.ExecutionDatabase);
                var challengeSnapshot = challenges.CaptureNormalSave(identity);
                var bidReceipt = AuctionBidSaveAllowed ? auctionBidActor.CaptureReconciliationReceipt() : null;
                var bankReceipt = BankSaveAllowed ? bankActor.CaptureReceipt() : null;
                var housingReceipt = HousingNameSaveAllowed ? housingNameActor.CaptureReceipt() : null;
                challengeSnapshot.RetainBidRollback = bidReceipt != null || bankReceipt != null || housingReceipt != null;
                normalRewardSave = new(this, quests, challenges, questSnapshot, challengeSnapshot, currencySnapshot, bidReceipt,
                    AuctionBidSaveAllowed ? auctionBidActor.CaptureDeliveryWitness() : null,
                    AuctionBidSaveAllowed ? auctionBidActor.CaptureCommodityWitness() : null,
                    AuctionBidSaveAllowed ? auctionBidActor.CaptureEscrowWitness() : null, wardrobeSnapshot, titleSnapshot, progressionSnapshot, experienceSnapshot, inventorySnapshot, satchelSnapshot, keybindingSnapshot, bankReceipt, housingReceipt, taxiFareVersion); return normalRewardSave;
            }
            catch { questSnapshot.Dispose(); throw; }
        }

        internal sealed class NormalRewardSave(Player owner, QuestManager quests, ChallengeManager challenges,
            QuestManager.NormalSaveSnapshot questSnapshot, ChallengeManager.NormalSaveSnapshot challengeSnapshot,
            Action<NexusForever.Database.Character.CharacterContext> currencySnapshot,
            NexusForever.Database.Character.MarketplaceBidReconciliation bidReceipt = null,
            Inventory.AuctionDeliveryWitness deliveryWitness = null, Inventory.CommodityDeliveryWitness commodityWitness = null,
            Inventory.AuctionEscrowWitness escrowWitness = null,
            Action<NexusForever.Database.Character.CharacterContext> wardrobeSnapshot = null,
            Action<NexusForever.Database.Character.CharacterContext> titleSnapshot = null,
            Action<NexusForever.Database.Character.CharacterContext> progressionSnapshot = null,
            Action<NexusForever.Database.Character.CharacterContext> experienceSnapshot = null,
            Action<NexusForever.Database.Character.CharacterContext> inventorySnapshot = null,
            Action<NexusForever.Database.Character.CharacterContext> satchelSnapshot = null,
            Action<NexusForever.Database.Character.CharacterContext> keybindingSnapshot = null,
            NexusForever.Database.Character.BankSaveReconciliation bankReceipt = null,
            NexusForever.Database.Character.HousingRenameSaveReconciliation housingReceipt = null,
            long taxiFareVersion = 0)
        {
            internal long TaxiFareVersion { get; } = taxiFareVersion;
            internal NexusForever.Database.Character.MarketplaceBidReconciliation BidReceipt { get; private set; } = bidReceipt;
            internal NexusForever.Database.Character.BankSaveReconciliation BankReceipt { get; private set; } = bankReceipt;
            internal NexusForever.Database.Character.HousingRenameSaveReconciliation HousingReceipt { get; private set; } = housingReceipt;
            internal QuestManager.NormalSaveSnapshot Quests { get; } = questSnapshot;
            internal ChallengeManager.NormalSaveSnapshot Challenges { get; } = challengeSnapshot;
            internal void SaveCurrencies(NexusForever.Database.Character.CharacterContext context)
            { if (currencySnapshot == null) owner.CurrencyManager.Save(context); else currencySnapshot(context); }
            internal void SaveWardrobe(NexusForever.Database.Character.CharacterContext context)
            { if (wardrobeSnapshot == null) owner.CostumeManager.Save(context); else wardrobeSnapshot(context); }
            internal void SaveTitles(NexusForever.Database.Character.CharacterContext context)
            { if (titleSnapshot == null) owner.TitleManager.Save(context); else titleSnapshot(context); }
            internal void SaveProgression(NexusForever.Database.Character.CharacterContext context)
            { if (progressionSnapshot == null) owner.SaveProgression(context); else progressionSnapshot(context); }
            internal void SaveExperience(NexusForever.Database.Character.CharacterContext context)
            { if (experienceSnapshot == null) owner.SaveExperience(context); else experienceSnapshot(context); }
            internal void SaveInventory(NexusForever.Database.Character.CharacterContext context)
            { if (inventorySnapshot == null) owner.Inventory.Save(context); else inventorySnapshot(context); }
            internal void SaveSatchel(NexusForever.Database.Character.CharacterContext context)
            { if (satchelSnapshot == null) owner.SupplySatchelManager.Save(context); else satchelSnapshot(context); }
            internal void SaveKeybindings(NexusForever.Database.Character.CharacterContext context)
            { if (keybindingSnapshot == null) owner.KeybindingManager.Save(context); else keybindingSnapshot(context); }
            private bool ended;
            private Inventory.AuctionDeliveryWitness delivery = deliveryWitness;
            private Inventory.CommodityDeliveryWitness commodity = commodityWitness;
            private Inventory.AuctionEscrowWitness escrow = escrowWitness;
            internal void StageDelivery(NexusForever.Database.Character.CharacterContext context)
            { escrow?.Stage(context); delivery?.Stage(context); commodity?.Stage(context); }
            internal bool RetryBidRollback(NexusForever.Database.Character.BidReconciliationRolledBackException proof)
            {
                Validate();
                bool partial = !proof.EnrollmentCompleted;
                if (BidReceipt == null || proof.ReceiptId != BidReceipt.Id || !Quests.CanRetryBidRollback(proof.Context, partial)
                    || !Challenges.CanRetryBidRollback(proof.Context, partial)) return false;
                Quests.RetryBidRollback(proof.Context, partial); Challenges.RetryBidRollback(proof.Context, partial);
                owner.auctionBidActor?.ObserveCommodityRollback(proof);
                if (proof.InnerException is Inventory.AuctionEscrowWitness.CustodyConflict escrowConflict)
                {
                    if (!ReferenceEquals(escrowConflict.Witness, escrow) || owner.auctionBidActor == null)
                        throw new InvalidOperationException("Escrow conflict belongs to another frozen reconciliation.");
                    // This witness owns no Item write. Unchanged listing rows
                    // are not retained dirty intents in CharacterBidSaveRetry.
                    owner.auctionBidActor.RejectEscrowWitness(); escrow = null;
                }
                if (proof.InnerException is Inventory.CommodityDeliveryWitness.MaterialConflict materialConflict)
                {
                    if (!ReferenceEquals(materialConflict.Witness, commodity) || owner.auctionBidActor == null)
                        throw new InvalidOperationException("Material conflict belongs to another frozen reconciliation.");
                    BidReceipt.RetireCommodityMaterial(proof, commodity.MaterialId, commodity.MaterialBefore, commodity.MaterialEnrolled);
                    commodity = owner.auctionBidActor.RebaseCommodityMaterial(commodity, materialConflict.Current);
                }
                if (proof.InnerException is Inventory.AuctionDeliveryWitness.CustodyConflict conflict)
                {
                    if (!ReferenceEquals(conflict.Witness, delivery) || owner.auctionBidActor == null)
                        throw new InvalidOperationException("Delivery conflict belongs to another frozen reconciliation.");
                    BidReceipt.RetireDeliveryItem(proof, delivery.ItemPreimage, delivery.ItemCodecPostimage);
                    owner.auctionBidActor.RejectDeliveryWitness(); delivery = null;
                }
                if (proof.InnerException is Inventory.CommodityDeliveryWitness.CustodyConflict commodityConflict)
                {
                    if (!ReferenceEquals(commodityConflict.Witness, commodity) || owner.auctionBidActor == null)
                        throw new InvalidOperationException("Commodity custody belongs to another frozen reconciliation.");
                    var quantities = owner.auctionBidActor.PreviewCommodityRecovery();
                    foreach (var item in commodity.Items)
                    {
                        var after = item.ItemCodecPostimage;
                        if (quantities.TryGetValue(after.Id, out uint quantity))
                        {
                            after.StackCount = quantity;
                            BidReceipt.RetireCommodityItem(proof, item.ItemPreimage, after);
                        }
                        else BidReceipt.RetireDeliveryItem(proof, item.ItemPreimage, after);
                    }
                    owner.auctionBidActor.RejectCommodityWitness();
                    commodity = owner.auctionBidActor.RenewCommodityWitness();
                }
                BidReceipt = BidReceipt with { Id = System.Guid.NewGuid().ToString("N") };
                return true;
            }
            internal bool RetryBankRollback(NexusForever.Database.Character.CharacterReconciliationRolledBackException proof)
            {
                Validate(); bool partial = !proof.EnrollmentCompleted;
                if (BankReceipt == null || proof.ReceiptId != BankReceipt.Id || !Quests.CanRetryBidRollback(proof.Context, partial)
                    || !Challenges.CanRetryBidRollback(proof.Context, partial)) return false;
                Quests.RetryBidRollback(proof.Context, partial); Challenges.RetryBidRollback(proof.Context, partial);
                BankReceipt = BankReceipt with { Id = System.Guid.NewGuid().ToString("N") }; return true;
            }
            internal bool RetryHousingRollback(NexusForever.Database.Character.CharacterReconciliationRolledBackException proof)
            {
                Validate(); bool partial = !proof.EnrollmentCompleted;
                if (HousingReceipt == null || proof.ReceiptId != HousingReceipt.Id || !Quests.CanRetryBidRollback(proof.Context, partial)
                    || !Challenges.CanRetryBidRollback(proof.Context, partial)) return false;
                Quests.RetryBidRollback(proof.Context, partial); Challenges.RetryBidRollback(proof.Context, partial);
                HousingReceipt = HousingReceipt with { Id = System.Guid.NewGuid().ToString("N") }; return true;
            }
            internal void Validate()
            {
                if (ended || !ReferenceEquals(owner.normalRewardSave, this)) throw new InvalidOperationException("Player frozen save lost its owner.");
            }
            internal bool Finish(bool success)
            {
                if (ended) return false; Validate(); ended = true;
                success = Quests.FinishContract(success);
                success = Challenges.FinishContract(success);
                if (!success) Quarantine();
                else if (commodity != null) owner.auctionBidActor.ConfirmCommodityWitness(commodity);
                Quests.Dispose(); Challenges.Dispose(); owner.normalRewardSave = null;
                bool accepted = success && !quests.RequiresAuthoritativeReload && !challenges.RequiresAuthoritativeReload;
                if (!accepted) Quarantine();
                else if (Quests.HasContractCompletion)
                {
                    var session = owner.Session;
                    var queue = session?.Events;
                    try
                    {
                        quests.PublishContractRewards(onReloadFailure: () =>
                        {
                            if (!ReferenceEquals(owner.Session, session) || !ReferenceEquals(session?.Events, queue)
                                || !ReferenceEquals(owner.QuestManager, quests) || !ReferenceEquals(owner.ChallengeManager, challenges)) return;
                            Quarantine();
                            session?.ForceDisconnect();
                        });
                    }
                    catch { Quarantine(); return false; }
                }
                return accepted;
            }
            private void Quarantine()
            {
                quests.RequireNormalSaveReload(); challenges.RequireRewardReload();
                quests.InvalidateRewardOperations(true); challenges.InvalidateRewardOperations(true);
            }
        }

        private void SaveFrozenAuth(AuthContext context, NormalRewardSave snapshot)
        {
            if (snapshot == null) { Save(context); return; }
            snapshot.Validate(); CheckInventorySaveAllowed(frozen: true, housingSnapshot: snapshot); Account.Save(context);
        }
        private bool ReleaseNormalRewardSave(bool success) => normalRewardSave?.Finish(success) ?? success;
    }
}
