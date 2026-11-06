using NexusForever.Database.Character;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Account;
using NexusForever.Network.Message;
using NexusForever.Game.Static.Entity;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Static;
using NexusForever.Shared.Game.Events;
using NetworkItem = NexusForever.Network.World.Message.Model.Shared.Item;

namespace NexusForever.Game.Entity
{
    public partial class Inventory
    {
        private enum PathItemNoticeKind { Add, Stack, Delete }
        private sealed class PathItemNotice(ulong guid, PathItemNoticeKind kind, ItemUpdateReason reason)
        {
            internal ulong Guid { get; } = guid;
            internal PathItemNoticeKind Kind { get; } = kind;
            internal ItemUpdateReason Reason { get; } = reason;
        }
        private sealed record PathItemCapture(PathItemNotice Notice, Func<IWritable> Build);
        private PathItemPublication pathItemPublication;

        private bool BufferPathItemMessage(IWritable message)
        {
            if (player is not Player actor) return false;
            PathItemNotice notice = message switch
            {
                ServerItemAdd added => new(added.InventoryItem.Item.ItemGuid, PathItemNoticeKind.Add, added.InventoryItem.Reason),
                ServerItemStackCountUpdate stack => new(stack.Guid, PathItemNoticeKind.Stack, stack.Reason),
                ServerItemDelete deleted => new(deleted.Guid, PathItemNoticeKind.Delete, deleted.Reason),
                _ => null
            };
            if (notice == null || notice.Reason != ItemUpdateReason.PathReward
                && pathItemPublication?.Owns(notice.Guid) != true) return false;

            // Related quantity/removal notices cannot overtake an unpublished
            // Path item. Unrelated ordinary items keep their existing route.
            pathItemPublication ??= new(this, actor);
            pathItemPublication.Retain(notice);
            return true;
        }

        // Only the complete Player save opts in. An isolated Inventory.Save
        // cannot confirm the Path reward marker that owns these notifications.
        internal void StageUnfrozenPathItemNotices(CharacterContext context)
        {
            lock (MutationGate)
                StagePathItemNotices(context, pathItemPublication?.Capture() ?? []);
        }

        private void StagePathItemNotices(CharacterContext context, PathItemCapture[] captured)
        {
            if (captured.Length == 0) return;
            var publication = pathItemPublication;
            context.OnSaveCompleted(committed =>
            {
                if (committed) publication.Confirm(captured);
            });
        }

        private sealed class PathItemPublication
        {
            private readonly Inventory owner;
            private readonly Player actor;
            private readonly PathManager path;
            private readonly Identity identity;
            private readonly IAccount account;
            private readonly uint accountId;
            private readonly ulong epoch;
            private readonly IGameSession session;
            private readonly EventQueue events;
            private readonly List<PathItemNotice> pending = [];
            private readonly Queue<PathItemCapture> ready = [];
            private bool scheduled;

            internal PathItemPublication(Inventory owner, Player actor)
            {
                this.owner = owner; this.actor = actor;
                path = actor.PathManager as PathManager;
                identity = actor.Identity == null ? null : new() { Id = actor.Identity.Id, RealmId = actor.Identity.RealmId };
                account = actor.Account; accountId = account?.Id ?? 0; epoch = account?.WorldSessionEpoch ?? 0;
                session = actor.Session; events = session?.Events;
            }

            private bool Current => identity != null && path != null && events != null && account != null
                && !owner.rewardOwnerInvalidated && !owner.RequiresAuthoritativeReload
                && actor.OwnsPathPublication(path, identity, session)
                && ReferenceEquals(actor.Inventory, owner) && actor.CharacterId == owner.characterId
                && ReferenceEquals(actor.Account, account) && account.Id == accountId && account.WorldSessionEpoch == epoch
                && ReferenceEquals(account.Session, session) && ReferenceEquals(session.Events, events);

            internal void Retain(PathItemNotice notice)
            {
                if (!Current) return;
                pending.Add(notice);
                actor.RequestSave();
            }

            internal bool Owns(ulong guid)
                => pending.Any(notice => notice.Guid == guid) || ready.Any(captured => captured.Notice.Guid == guid);

            internal PathItemCapture[] Capture()
            {
                return pending.Select(notice =>
                {
                    // Freeze the same current membership/quantity as the owning
                    // Item save, rather than replaying the earlier mutation value.
                    var item = owner.GetItem(notice.Guid);
                    if (item?.Location is InventoryLocation.Ability or InventoryLocation.RealmBank) item = null;
                    Func<IWritable> build = null;
                    if (notice.Kind == PathItemNoticeKind.Delete)
                    {
                        if (item == null) build = () => new ServerItemDelete { Guid = notice.Guid, Reason = notice.Reason };
                    }
                    else if (item != null)
                    {
                        if (notice.Kind == PathItemNoticeKind.Add)
                        {
                            var frozen = ClonePathRewardItem(item.Build());
                            build = () => new ServerItemAdd { InventoryItem = new() { Item = ClonePathRewardItem(frozen), Reason = notice.Reason } };
                        }
                        else
                        {
                            uint count = item.StackCount;
                            build = () => new ServerItemStackCountUpdate { Guid = notice.Guid, StackCount = count, Reason = notice.Reason };
                        }
                    }
                    return new PathItemCapture(notice, build);
                }).ToArray();
            }

            internal void Confirm(PathItemCapture[] captured)
            {
                lock (owner.MutationGate)
                    foreach (var capturedNotice in captured)
                        if (pending.Remove(capturedNotice.Notice) && capturedNotice.Build != null) ready.Enqueue(capturedNotice);
                Schedule(Task.CompletedTask);
            }

            private void Schedule(Task delay)
            {
                lock (owner.MutationGate)
                {
                    if (!Current) { pending.Clear(); ready.Clear(); return; }
                    if (scheduled || ready.Count == 0) return;
                    scheduled = true;
                }
                events.EnqueueThreadSafe(new PredicateEvent(
                    () => !Current || delay.IsCompleted && !actor.IsNormalSaveInProgress,
                    Publish));
            }

            private void Publish()
            {
                bool retry = false;
                try
                {
                    while (Current)
                    {
                        PathItemCapture notice;
                        lock (owner.MutationGate)
                            if (!ready.TryPeek(out notice)) return;
                        session.EnqueueMessageEncrypted(notice.Build());
                        lock (owner.MutationGate)
                            ready.Dequeue();
                    }
                    lock (owner.MutationGate) { pending.Clear(); ready.Clear(); }
                }
                catch (Exception error)
                {
                    retry = true;
                    log.Warn(error, "Committed Path item enqueue failed; retaining the original actor and packet cursor.");
                }
                finally
                {
                    lock (owner.MutationGate) scheduled = false;
                    Schedule(retry ? Task.Delay(TimeSpan.FromSeconds(1)) : Task.CompletedTask);
                }
            }
        }

        private static NetworkItem ClonePathRewardItem(NetworkItem value)
        {
            var copy = new NetworkItem
            {
                ItemGuid = value.ItemGuid, MakerCharacterId = value.MakerCharacterId, Item2Id = value.Item2Id,
                LocationData = new() { Location = value.LocationData.Location, BagIndex = value.LocationData.BagIndex },
                StackCount = value.StackCount, Charges = value.Charges, RandomCircuitData = value.RandomCircuitData,
                RandomGlyphData = value.RandomGlyphData, ThresholdData = value.ThresholdData, Durability = value.Durability,
                Unknown44 = value.Unknown44, Unknown48 = value.Unknown48, DyeData = value.DyeData, DynamicFlags = value.DynamicFlags,
                ExpirationTimeLeft = value.ExpirationTimeLeft, PowerCoreItem2Id = value.PowerCoreItem2Id, EffectiveItemLevel = value.EffectiveItemLevel,
                SellPrices = value.SellPrices.Select(price => new NetworkItem.PriceInfo { CostType = price.CostType, Amount = price.Amount, Value = price.Value }).ToArray()
            };
            copy.Microchips.AddRange(value.Microchips);
            copy.Glyphs.AddRange(value.Glyphs);
            copy.TimeLimitedTradingPartners.AddRange(value.TimeLimitedTradingPartners.Select(row => new NetworkItem.TradingPartnerInfo { Unknown0 = row.Unknown0, Unknown8 = row.Unknown8 }));
            return copy;
        }
    }
}
