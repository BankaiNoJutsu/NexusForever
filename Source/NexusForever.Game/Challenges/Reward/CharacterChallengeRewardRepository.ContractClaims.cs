using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Quest.Reward;
using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Challenges.Reward
{
    public sealed partial class CharacterChallengeRewardRepository
    {
        internal ContractClaimStage ClaimContract(CharacterChallengeRewardCommand command, IInventoryRewardReservation reservation)
            => Stage(() =>
            {
                Require(command?.Kind == CharacterChallengeRewardCommandKind.ClaimContract && command.CharacterId == characterId,
                    "Contract claim belongs to another command owner.");
                var awards = command.ContractAwards; var quote = ContractRewardClaimQuote.Restore(awards, command.ContractQuote.Json);
                var award = quote.Award;
                Require(context.Character.AsNoTracking().Any(row => row.Id == characterId && row.AccountId == command.ContractAccountId && row.DeleteTime == null),
                    "Contract Character changed its account owner.");
                var current = ContractRewardSnapshot.Validate(ContractRewardSnapshot.Read(context, characterId, () => awards.Catalog), characterId, awards.Catalog);
                var duplicate = context.CharacterContractRewardClaim.AsNoTracking().SingleOrDefault(row => row.Id == characterId
                    && row.TrackId == award.TrackId && row.CycleId == quote.CycleId && row.Ordinal == award.Ordinal);
                if (duplicate != null)
                {
                    var outbox = context.CharacterContractAccountOutbox.AsNoTracking().SingleOrDefault(row => row.ClaimId == duplicate.ClaimId);
                    var proof = ContractRewardClaimProof.FromModel(duplicate, outbox);
                    proof.Validate(awards, characterId, award.TrackId, quote.CycleId, award.Ordinal, award.RewardRowId, quote.SelectionJson);
                    Require(proof.ClaimId == command.Identity && proof.AccountId == command.ContractAccountId && proof.QuoteJson == quote.Json
                        && proof.Choice == command.Choice && proof.CharacterCurrencyJson == CurrencyJson(command), "Another Contract claim already owns this selected slot.");
                    return new ContractClaimStage(false, proof, current.SingleOrDefault(row => row.TrackId == award.TrackId));
                }
                Require(SameContracts(current, command.ContractBefore), "Contract claim baseline changed before execution.");
                var trackState = current.SingleOrDefault(row => row.TrackId == award.TrackId);
                Require(trackState != null && ContractRewardClaimQuote.Capture(awards, characterId, current, award.TrackId, quote.CycleId,
                    quote.Revision, award.RewardRowId, award.Choice).Json == quote.Json && meets(awards.Catalog.GetTrack(award.TrackId).PrerequisiteId),
                    "Contract claim lost its earned published selection.");
                Require(!context.CharacterContractRewardClaim.AsNoTracking().Any(row => row.ClaimId == command.Identity)
                    && !context.CharacterContractAccountOutbox.AsNoTracking().Any(row => row.ClaimId == command.Identity), "Contract claim identity was already used.");
                var track = context.CharacterContractRewardTrack.AsNoTracking().Single(row => row.Id == characterId && row.TrackId == award.TrackId);
                var slot = context.CharacterContractRewardSlot.AsNoTracking().Single(row => row.Id == characterId && row.TrackId == award.TrackId
                    && row.CycleId == quote.CycleId && row.Ordinal == award.Ordinal);
                Require(!slot.Claimed, "Contract selected slot was already claimed.");
                context.Attach(track); track.Revision = checked(track.Revision + 1);
                context.Attach(slot); slot.Claimed = true;
                var receipt = new CharacterContractRewardClaimModel { Id = characterId, TrackId = award.TrackId, CycleId = quote.CycleId,
                    Ordinal = award.Ordinal, RewardRowId = award.RewardRowId, Choice = award.Choice, ClaimId = command.Identity,
                    AccountId = command.ContractAccountId, QuoteVersion = 1, QuoteJson = quote.Json, CreatedUtc = SqlTime(command.CreatedUtc),
                    CharacterCurrencyJson = CurrencyJson(command), DeliveryJson = "" };
                context.CharacterContractRewardClaim.Add(receipt);
                if (award.CharacterCurrency != null)
                {
                    var delivery = ContractCurrencyDelivery.Restore(award.CharacterCurrency, receipt.CharacterCurrencyJson);
                    var wallet = context.CharacterCurrency.AsNoTracking().SingleOrDefault(row => row.Id == characterId && row.CurrencyId == (byte)delivery.CurrencyId);
                    Require((wallet != null) == delivery.BeforeExists && (wallet?.Amount ?? 0) == delivery.BeforeAmount,
                        "Contract credit wallet changed its frozen baseline.");
                    if (wallet == null) { wallet = new() { Id = characterId, CurrencyId = (byte)delivery.CurrencyId }; context.CharacterCurrency.Add(wallet); }
                    else context.Attach(wallet);
                    wallet.Amount = delivery.AfterAmount;
                    Own(wallet, () => context.CharacterCurrency.AsNoTracking().SingleOrDefault(row => row.Id == characterId && row.CurrencyId == (byte)delivery.CurrencyId));
                }
                CharacterContractAccountOutboxModel pending = null;
                if (award.Item != null)
                {
                    Require(reservation != null && reservation.CharacterId == characterId && reservation.ItemId == award.Item.ItemId && reservation.Count == award.Item.Count
                        && reservation.State is InventoryRewardReservationState.Prepared or InventoryRewardReservationState.ReadyToRetry,
                        "Contract inventory reservation differs from its complete canonical choice.");
                    var plan = reservation.StageContract(context, receipt);
                    Require(plan != null && plan.CharacterId == characterId && plan.ItemId == award.Item.ItemId && plan.Count == award.Item.Count
                        && plan.Lines.Count != 0 && plan.Lines.All(line => line.After.CharacterId == characterId && line.After.ItemId == award.Item.ItemId
                            && line.After.Location == InventoryLocation.Inventory && line.After.Soulbound)
                        && plan.Lines.Aggregate(0ul, (sum, line) => checked(sum + line.AwardedCount)) == award.Item.Count,
                        "Contract staged inventory differs from its canonical award.");
                    deliveryItemIds = plan.Lines.Select(line => line.After.Guid).ToHashSet();
                    Require(deliveryItemIds.Count == plan.Lines.Count && !deliveryItemIds.Contains(0)
                        && receipt.DeliveryVersion == plan.Version && receipt.DeliveryJson == plan.ToJson(), "Contract Item receipt is incomplete.");
                }
                else
                {
                    Require(reservation == null, "An account compensation cannot enroll an Item2 delivery.");
                    var intent = ContractAccountIntent.Capture(awards, command.ContractRealmId, command.ContractAccountId, command.Identity, quote);
                    receipt.DeliveryVersion = 2; receipt.DeliveryJson = intent.Json;
                    pending = new() { ClaimId = command.Identity, CharacterId = characterId, AccountId = command.ContractAccountId,
                        RealmId = command.ContractRealmId, TrackId = award.TrackId, CycleId = quote.CycleId, Ordinal = award.Ordinal, PayloadJson = intent.Json };
                    context.CharacterContractAccountOutbox.Add(pending);
                    Own(pending, () => context.CharacterContractAccountOutbox.AsNoTracking().SingleOrDefault(row => row.ClaimId == command.Identity));
                }
                var claimProof = ContractRewardClaimProof.FromModel(receipt, pending);
                claimProof.Validate(awards, characterId, award.TrackId, quote.CycleId, award.Ordinal, award.RewardRowId, quote.SelectionJson);
                Own(track, () => context.CharacterContractRewardTrack.AsNoTracking().SingleOrDefault(row => row.Id == characterId && row.TrackId == award.TrackId));
                Own(slot, () => context.CharacterContractRewardSlot.AsNoTracking().SingleOrDefault(row => row.Id == characterId && row.TrackId == award.TrackId
                    && row.CycleId == quote.CycleId && row.Ordinal == award.Ordinal));
                Own(receipt, () => context.CharacterContractRewardClaim.AsNoTracking().SingleOrDefault(row => row.Id == characterId && row.TrackId == award.TrackId
                    && row.CycleId == quote.CycleId && row.Ordinal == award.Ordinal));
                var after = trackState with { Revision = track.Revision, Slots = Array.AsReadOnly(trackState.Slots.Select(value => value.Ordinal == award.Ordinal ? value with { Claim = claimProof } : value).ToArray()) };
                // Working retail direction (CONTRACT_RETAIL_RESET_EVIDENCE.md): the cycle resets only after the final
                // reward's claim commits, in the same transaction. Earlier unclaimed milestones are forfeited and excess
                // points are discarded; the next cycle starts unselected and is initialized by the next acceptance.
                if (award.Ordinal == after.Slots.Count - 1)
                {
                    track.CycleId = checked(track.CycleId + 1); track.Points = 0; track.SelectionInitialized = false;
                    after = after with { CycleId = track.CycleId, Points = 0, SelectionInitialized = false, Slots = Array.Empty<ContractRewardSlotState>() };
                }
                var expected = current.Select(value => value.TrackId == award.TrackId ? after : value).ToArray();
                context.OnSaveValidation(phase =>
                {
                    if (phase == Database.Character.CharacterSaveValidationPhase.BeforeExplicitCommit)
                        Require(SameContracts(ContractRewardSnapshot.Validate(ContractRewardSnapshot.Read(context, characterId, () => awards.Catalog), characterId, awards.Catalog), expected),
                            "Contract claim SQL differs from its complete frozen aggregate.");
                });
                return new ContractClaimStage(true, claimProof, after);
            });

        private static string CurrencyJson(CharacterChallengeRewardCommand command) => command.ContractQuote.Award.CharacterCurrency == null ? null
            : ContractCurrencyDelivery.Capture(command.ContractQuote.Award.CharacterCurrency, command.ContractCurrencyBefore).Json;
    }
    internal sealed record ContractClaimStage(bool Applied, ContractRewardClaimProof Claim, ContractTrackState Track);
}
