using System;
using System.Collections.Generic;
using NexusForever.Game.Static.Account;
using NetworkIdentity = NexusForever.Network.World.Message.Model.Shared.Identity;

namespace NexusForever.Game.Abstract.Account.Inventory
{
    public enum PendingAccountItemGroupTransferKind
    {
        Unknown,
        GiftToCharacter,
        GiftToAccount,
        ReturnToSender
    }

    /// <summary>
    /// Evidence-backed seam for pending account-item group handoff.
    /// Implementations may route groups to online recipients, but must not invent
    /// offline persistence, TTL, mail fallback, or coupon policy without more evidence.
    /// </summary>
    public interface IPendingAccountItemGroupDelivery
    {
        AccountOperationResult Deliver(PendingAccountItemGroupDeliveryRequest request);
    }

    public sealed class PendingAccountItemGroupDeliveryRequest
    {
        public string SourceGroup { get; init; } = string.Empty;
        public PendingAccountItemGroupTransferKind TransferKind { get; init; }
        public AccountOperation Operation { get; init; }
        public uint SourceAccountId { get; init; }
        public uint TargetAccountId { get; init; }
        public IReadOnlyList<uint> AccountItemIds { get; init; } = Array.Empty<uint>();
        public NetworkIdentity SenderIdentity { get; init; } = new();
        public NetworkIdentity TargetIdentity { get; init; } = new();
    }
}
