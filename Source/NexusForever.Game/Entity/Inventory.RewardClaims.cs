using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;

namespace NexusForever.Game.Entity
{
    public partial class Inventory
    {
        private sealed partial class RewardDeliveryReservation
        {
            private sealed record ClaimIdentity(bool Contract, ulong Character, uint Track, ulong Cycle, byte Ordinal,
                uint Reward, uint Choice, string Claim, DateTime Created, uint Account = 0, uint QuoteVersion = 0, string QuoteJson = null);
            private sealed record ReceiptProof(ClaimIdentity Identity, uint DeliveryVersion, string DeliveryJson);
            private sealed record SlotIdentity(ulong Character, uint Track, ulong Cycle, byte Ordinal, uint Reward, uint Points, string SelectionJson = null);
            private sealed record SlotProof(SlotIdentity Identity, bool Claimed);
            private sealed record TrackProof(ulong Cycle, uint Points, bool Initialized = true);

            // Only the two explicit typed entry points construct these adapters.
            // No caller-provided callbacks may weaken the durable receipt guard.
            private abstract class RewardClaimEnrollment
            {
                protected object slot;
                protected ClaimIdentity bound;
                private SlotIdentity before;
                private string expectedDelivery;
                internal abstract object Receipt { get; }
                internal ClaimIdentity Identity => CurrentReceipt.Identity;
                protected abstract ReceiptProof CurrentReceipt { get; }
                protected abstract SlotProof CurrentSlot { get; }
                internal abstract void SetCreated(DateTime created);
                protected abstract void WriteDelivery(string payload);
                internal void SetDelivery(string payload) { WriteDelivery(payload); expectedDelivery = payload; }
                protected abstract object FindTrackedSlot(CharacterContext context);
                protected abstract SlotProof ReadSlot(CharacterContext context);
                protected abstract ReceiptProof ReadReceipt(CharacterContext context);
                protected abstract TrackProof ReadTrack(CharacterContext context);

                internal void Bind(CharacterContext context, ulong character)
                {
                    var identity = Identity; bound = identity;
                    if (context.Entry(Receipt).State != EntityState.Added || identity.Character != character
                        || identity.Track == 0 || identity.Reward == 0 || identity.Ordinal >= 32
                        || !Guid.TryParseExact(identity.Claim, "N", out var claim) || claim == Guid.Empty || identity.Created == default
                        || identity.Contract && (identity.Account == 0 || identity.QuoteVersion != 1 || string.IsNullOrEmpty(identity.QuoteJson)
                            || identity.QuoteJson.Length > 131072 || !context.Character.AsNoTracking().Any(row => row.Id == character
                                && row.AccountId == identity.Account && row.DeleteTime == null)))
                        throw new InvalidOperationException("A new owned claim with a retained identity is required.");
                    slot = FindTrackedSlot(context);
                    var source = ReadSlot(context); var track = ReadTrack(context);
                    if (slot == null || source == null || source.Claimed || !CurrentSlot.Claimed || CurrentSlot.Identity != source.Identity
                        || source.Identity.Character != identity.Character || source.Identity.Track != identity.Track
                        || source.Identity.Cycle != identity.Cycle || source.Identity.Ordinal != identity.Ordinal || source.Identity.Reward != identity.Reward
                        || track == null || !track.Initialized || track.Cycle != identity.Cycle || track.Points < source.Identity.Points
                        || identity.Contract && string.IsNullOrEmpty(source.Identity.SelectionJson) || ReadReceipt(context) != null)
                        throw new InvalidOperationException("Claim source is no longer its unclaimed durable slot.");
                    before = source.Identity;
                    // This callback runs directly at the Character boundary,
                    // outside Item validation gates. A live tracked entity is
                    // insufficient proof that its receipt/claimed bit reached SQL.
                    context.OnSaveValidation(phase =>
                    {
                        if (phase == CharacterSaveValidationPhase.BeforeExplicitCommit
                            && !ReadOutcome(context, bound, expectedDelivery).Committed)
                            throw new InvalidOperationException("Reward delivery lost its durable receipt/claimed-slot witness.");
                    });
                }

                internal void Validate(CharacterContext context, bool flushed, string payload)
                {
                    var receipt = CurrentReceipt;
                    var entry = context.Entry(slot);
                    if (receipt.DeliveryVersion != 1 || receipt.DeliveryJson != payload || CurrentSlot != new SlotProof(before, true)
                        || context.Entry(Receipt).State is EntityState.Detached or EntityState.Deleted
                        || entry.State is EntityState.Detached or EntityState.Deleted
                        || entry.OriginalValues.GetValue<ulong>("Id") != before.Character
                        || entry.OriginalValues.GetValue<uint>("TrackId") != before.Track
                        || entry.OriginalValues.GetValue<ulong>("CycleId") != before.Cycle
                        || entry.OriginalValues.GetValue<byte>("Ordinal") != before.Ordinal
                        || entry.OriginalValues.GetValue<uint>("RewardRowId") != before.Reward
                        || entry.OriginalValues.GetValue<uint>("RequiredPoints") != before.Points
                        || Identity.Contract && entry.OriginalValues.GetValue<string>("SelectionJson") != before.SelectionJson
                        || !flushed && (context.Entry(Receipt).State != EntityState.Added || entry.State != EntityState.Modified
                            || !entry.Property("Claimed").IsModified || entry.OriginalValues.GetValue<bool>("Claimed")))
                        throw new InvalidOperationException("Reward receipt/claimed-state enrollment changed or is incomplete.");
                }

                internal (bool Committed, bool Absent) ReadOutcome(CharacterContext context, ClaimIdentity identity, string payload)
                {
                    if (identity.Contract && !context.Character.AsNoTracking().Any(row => row.Id == identity.Character
                        && row.AccountId == identity.Account && row.DeleteTime == null)) return (false, false);
                    var receipt = ReadReceipt(context); var source = ReadSlot(context);
                    bool matches = before != null && source != null && source.Identity == before;
                    return (matches && source.Claimed && receipt?.Identity == identity && receipt.DeliveryVersion == 1
                        && payload != null && receipt.DeliveryJson == payload,
                        matches && !source.Claimed && receipt == null);
                }
            }

            private sealed class ChallengeClaimEnrollment(CharacterChallengeRewardClaimModel receipt) : RewardClaimEnrollment
            {
                internal override object Receipt => receipt;
                private static ReceiptProof Proof(CharacterChallengeRewardClaimModel row) => row == null ? null : new(
                    new(false, row.Id, row.TrackId, row.CycleId, row.Ordinal, row.RewardRowId, row.Choice, row.ClaimId, row.CreatedUtc), row.DeliveryVersion, row.DeliveryJson);
                private static SlotProof Proof(CharacterChallengeRewardSlotModel row) => row == null ? null : new(
                    new(row.Id, row.TrackId, row.CycleId, row.Ordinal, row.RewardRowId, row.RequiredPoints), row.Claimed);
                protected override ReceiptProof CurrentReceipt => Proof(receipt);
                protected override SlotProof CurrentSlot => Proof((CharacterChallengeRewardSlotModel)slot);
                internal override void SetCreated(DateTime created) => receipt.CreatedUtc = created;
                protected override void WriteDelivery(string payload) { receipt.DeliveryVersion = 1; receipt.DeliveryJson = payload; }
                protected override object FindTrackedSlot(CharacterContext context) => context.ChangeTracker.Entries<CharacterChallengeRewardSlotModel>()
                    .Select(entry => entry.Entity).SingleOrDefault(row => row.Id == bound.Character && row.TrackId == bound.Track && row.CycleId == bound.Cycle && row.Ordinal == bound.Ordinal);
                protected override SlotProof ReadSlot(CharacterContext context) => Proof(context.CharacterChallengeRewardSlot.AsNoTracking().SingleOrDefault(row => row.Id == bound.Character
                    && row.TrackId == bound.Track && row.CycleId == bound.Cycle && row.Ordinal == bound.Ordinal));
                protected override ReceiptProof ReadReceipt(CharacterContext context) => Proof(context.CharacterChallengeRewardClaim.AsNoTracking().SingleOrDefault(row => row.Id == bound.Character
                    && row.TrackId == bound.Track && row.CycleId == bound.Cycle && row.Ordinal == bound.Ordinal));
                protected override TrackProof ReadTrack(CharacterContext context)
                {
                    var row = context.CharacterChallengeRewardTrack.AsNoTracking().SingleOrDefault(row => row.Id == bound.Character && row.TrackId == bound.Track);
                    return row == null ? null : new(row.CycleId, row.Points);
                }
            }

            private sealed class ContractClaimEnrollment(CharacterContractRewardClaimModel receipt) : RewardClaimEnrollment
            {
                internal override object Receipt => receipt;
                private static ReceiptProof Proof(CharacterContractRewardClaimModel row) => row == null ? null : new(
                    new(true, row.Id, row.TrackId, row.CycleId, row.Ordinal, row.RewardRowId, row.Choice, row.ClaimId, row.CreatedUtc, row.AccountId, row.QuoteVersion, row.QuoteJson), row.DeliveryVersion, row.DeliveryJson);
                private static SlotProof Proof(CharacterContractRewardSlotModel row) => row == null ? null : new(
                    new(row.Id, row.TrackId, row.CycleId, row.Ordinal, row.RewardRowId, row.RequiredPoints, row.SelectionJson), row.Claimed);
                protected override ReceiptProof CurrentReceipt => Proof(receipt);
                protected override SlotProof CurrentSlot => Proof((CharacterContractRewardSlotModel)slot);
                internal override void SetCreated(DateTime created) => receipt.CreatedUtc = created;
                protected override void WriteDelivery(string payload) { receipt.DeliveryVersion = 1; receipt.DeliveryJson = payload; }
                protected override object FindTrackedSlot(CharacterContext context) => context.ChangeTracker.Entries<CharacterContractRewardSlotModel>()
                    .Select(entry => entry.Entity).SingleOrDefault(row => row.Id == bound.Character && row.TrackId == bound.Track && row.CycleId == bound.Cycle && row.Ordinal == bound.Ordinal);
                protected override SlotProof ReadSlot(CharacterContext context) => Proof(context.CharacterContractRewardSlot.AsNoTracking().SingleOrDefault(row => row.Id == bound.Character
                    && row.TrackId == bound.Track && row.CycleId == bound.Cycle && row.Ordinal == bound.Ordinal));
                protected override ReceiptProof ReadReceipt(CharacterContext context) => Proof(context.CharacterContractRewardClaim.AsNoTracking().SingleOrDefault(row => row.Id == bound.Character
                    && row.TrackId == bound.Track && row.CycleId == bound.Cycle && row.Ordinal == bound.Ordinal));
                protected override TrackProof ReadTrack(CharacterContext context)
                {
                    var row = context.CharacterContractRewardTrack.AsNoTracking().SingleOrDefault(row => row.Id == bound.Character && row.TrackId == bound.Track);
                    return row == null ? null : new(row.CycleId, row.Points, row.SelectionInitialized);
                }
            }
        }
    }
}
