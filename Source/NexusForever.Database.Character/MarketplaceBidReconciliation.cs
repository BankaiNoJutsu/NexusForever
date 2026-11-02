using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace NexusForever.Database.Character
{
    public sealed class BidReconciliationRolledBackException : CharacterReconciliationRolledBackException
    {
        internal BidReconciliationRolledBackException(CharacterContext context, string database, string receiptId, Exception cause, bool enrollmentCompleted = true)
            : base(context, database, receiptId, cause, enrollmentCompleted) { }
    }

    /// <summary>Identity of one bid actor's complete ordinary reconciliation save.</summary>
    public sealed record MarketplaceBidReconciliation(string Id, string BidId, string BidPayloadSHA256, bool BidCommitted)
        : CharacterSaveReconciliation(Id)
    {
        public void RetireDeliveryItem(BidReconciliationRolledBackException proof, Model.ItemModel expected, Model.ItemModel allowedAfter)
        {
            if (proof == null || proof.ReceiptId != Id || proof.EnrollmentCompleted)
                throw new InvalidOperationException("Delivery retirement requires this receipt's partial-enrollment rollback proof.");
            Retry.RetireDeliveryItem(proof.Context, expected, allowedAfter);
        }
        public void RetireCommodityItem(BidReconciliationRolledBackException proof, Model.ItemModel expected, Model.ItemModel after)
        {
            if (proof == null || proof.ReceiptId != Id || proof.EnrollmentCompleted || after == null || expected == null
                || after.StackCount <= expected.StackCount)
                throw new InvalidOperationException("Commodity quantity transfer requires this receipt's partial rollback proof.");
            Retry.RetireDeliveryItem(proof.Context, expected, after, commodityQuantity: true);
        }

        public void RetireCommodityMaterial(BidReconciliationRolledBackException proof, ushort material, ushort? before, ushort? after)
        {
            if (proof == null || proof.ReceiptId != Id || proof.EnrollmentCompleted)
                throw new InvalidOperationException("Material retirement requires this receipt's partial-enrollment rollback proof.");
            Retry.RetireCommodityMaterial(proof.Context, material, before, after);
        }

        internal override void Validate()
        {
            if (!Guid.TryParseExact(Id, "N", out var id) || id == Guid.Empty || Id != id.ToString("N")
                || !Guid.TryParseExact(BidId, "N", out var bid) || bid == Guid.Empty || BidId != bid.ToString("N")
                || Id == BidId || BidPayloadSHA256?.Length != 64 || BidPayloadSHA256.Any(ch => !"0123456789ABCDEF".Contains(ch)))
                throw new InvalidOperationException("Bid reconciliation requires distinct retained identities and its parent payload hash.");
        }

        internal override string Kind => "normal-bid-reconciliation";
        internal override void ValidateParent(CharacterContext context)
        {
            var parent = context.MarketplaceBidReceipt.AsNoTracking().SingleOrDefault(row => row.Id == BidId);
            if (BidCommitted ? parent == null || Hash(parent.Payload) != BidPayloadSHA256 : parent != null)
                throw new InvalidOperationException("Bid reconciliation lost its confirmed parent outcome.");
        }
        internal override string ReadPayload(CharacterContext context, string id)
            => context.MarketplaceBidReceipt.AsNoTracking().Where(row => row.Id == id).Select(row => row.Payload).SingleOrDefault();
        internal override object AddReceipt(CharacterContext context, string payload, DateTime created)
        {
            var row = new Model.MarketplaceBidReceiptModel { Id = Id, Payload = payload, CreatedUtc = created };
            context.MarketplaceBidReceipt.Add(row); return row;
        }
        internal override CharacterReconciliationRolledBackException RolledBack(CharacterContext context, string database, Exception cause, bool completed)
            => new BidReconciliationRolledBackException(context, database, Id, cause, completed);

        internal static string Hash(string payload) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
    }
}
