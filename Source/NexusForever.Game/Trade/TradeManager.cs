using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Trade;
using NexusForever.Game.Static.Entity;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Static;
using NexusForever.Shared;
using static NexusForever.Network.World.Message.Model.ServerP2PTradeResult;

namespace NexusForever.Game.Trade
{
    public sealed class TradeManager : Singleton<TradeManager>, ITradeManager
    {
        private const double InviteTimeoutSeconds = 30d;

        private enum TradeState
        {
            Pending,
            Active
        }

        private sealed class TradeOffer
        {
            public List<ulong> ItemGuids { get; } = [];
            public ulong Credits { get; set; }
            public bool Committed { get; set; }
        }

        private sealed class TradeSession
        {
            public IPlayer Initiator { get; init; }
            public IPlayer Target { get; init; }
            public TradeState State { get; set; }
            public double Timer { get; set; }
            public TradeOffer InitiatorOffer { get; } = new();
            public TradeOffer TargetOffer { get; } = new();
        }

        private readonly object syncRoot = new();
        private readonly List<TradeSession> sessions = [];
        private readonly Dictionary<uint, TradeSession> sessionsByPlayer = [];

        public P2PTradeResult? Initiate(IPlayer initiator, IPlayer target)
        {
            P2PTradeResult? failure = GetPreflightFailure(initiator, target);
            if (failure.HasValue)
                return failure;

            lock (syncRoot)
            {
                failure = GetSessionFailure(initiator, target);
                if (failure.HasValue)
                    return failure;

                var session = new TradeSession
                {
                    Initiator = initiator,
                    Target    = target,
                    State     = TradeState.Pending,
                    Timer     = InviteTimeoutSeconds
                };

                sessions.Add(session);
                sessionsByPlayer.Add(initiator.Guid, session);
                sessionsByPlayer.Add(target.Guid, session);

                target.Session?.EnqueueMessageEncrypted(new ServerP2PTradeInvite
                {
                    TradeInviterUnitId = initiator.Guid
                });
            }

            return null;
        }

        public P2PTradeResult? Accept(IPlayer player)
        {
            lock (syncRoot)
            {
                if (!TryGetSession(player, out TradeSession session))
                    return P2PTradeResult.ErrorInitiating;

                if (session.State != TradeState.Pending || session.Target.Guid != player.Guid)
                    return P2PTradeResult.ErrorInitiating;

                P2PTradeResult? failure = GetPreflightFailure(session.Initiator, session.Target);
                if (failure.HasValue)
                {
                    Finish(session, failure.Value, true);
                    return null;
                }

                session.State = TradeState.Active;
                session.Timer = 0d;

                SendToParticipants(session, new ServerP2PTradeResult
                {
                    Result = P2PTradeResult.PlayerAcceptedInvite
                });
            }

            return null;
        }

        public P2PTradeResult? Decline(IPlayer player)
        {
            lock (syncRoot)
            {
                if (!TryGetSession(player, out TradeSession session))
                    return P2PTradeResult.ErrorInitiating;

                if (session.State != TradeState.Pending || session.Target.Guid != player.Guid)
                    return P2PTradeResult.ErrorInitiating;

                Finish(session, P2PTradeResult.PlayerDeclinedInvite, true);
            }

            return null;
        }

        public P2PTradeResult? Cancel(IPlayer player)
        {
            lock (syncRoot)
            {
                if (!TryGetSession(player, out TradeSession session))
                    return null;

                Finish(session, P2PTradeResult.PlayerCanceled, true);
            }

            return null;
        }

        public P2PTradeResult? AddItem(IPlayer player, IItem item)
        {
            lock (syncRoot)
            {
                if (!TryGetActiveSession(player, out TradeSession session))
                    return P2PTradeResult.ErrorAddingItem;

                if (!IsTradableItem(player, item) || IsItemOffered(session, item.Guid))
                    return P2PTradeResult.ErrorAddingItem;

                TradeOffer offer = GetOffer(session, player);
                offer.ItemGuids.Add(item.Guid);
                ResetCommits(session);

                SendToParticipants(session, new ServerP2PTradeUpdateItem
                {
                    TradeIndex = (uint)offer.ItemGuids.Count - 1u,
                    OwnerUnitId = player.Guid,
                    ItemId      = item.Id,
                    ItemGuid    = item.Guid,
                    StackCount  = item.StackCount
                });
            }

            return null;
        }

        public P2PTradeResult? RemoveItem(IPlayer player, ulong itemGuid)
        {
            lock (syncRoot)
            {
                if (!TryGetActiveSession(player, out TradeSession session))
                    return P2PTradeResult.ErrorRemovingItem;

                TradeOffer offer = GetOffer(session, player);
                if (!offer.ItemGuids.Remove(itemGuid))
                    return P2PTradeResult.ErrorRemovingItem;

                ResetCommits(session);

                SendToParticipants(session, new ServerPTPTradeItemRemoved
                {
                    ItemGuid = itemGuid
                });
            }

            return null;
        }

        public P2PTradeResult? SetMoney(IPlayer player, ulong credits)
        {
            lock (syncRoot)
            {
                if (!TryGetActiveSession(player, out TradeSession session))
                    return P2PTradeResult.ErrorInitiating;

                if (!player.CurrencyManager.CanAfford(CurrencyType.Credits, credits))
                    return P2PTradeResult.ErrorInitiating;

                TradeOffer offer = GetOffer(session, player);
                if (offer.Credits != credits)
                {
                    offer.Credits = credits;
                    ResetCommits(session);
                }

                SendToParticipants(session, new ServerP2PTradeUpdateMoney
                {
                    Credits = credits,
                    UnitId  = player.Guid
                });
            }

            return null;
        }

        public P2PTradeResult? Commit(IPlayer player)
        {
            TradeSession sessionToSettle = null;
            lock (syncRoot)
            {
                if (!TryGetActiveSession(player, out TradeSession session))
                    return P2PTradeResult.ErrorInitiating;

                TradeOffer offer = GetOffer(session, player);
                P2PTradeResult commitResult = GetCommitResult(session, player, !offer.Committed);

                if (offer.Committed)
                {
                    offer.Committed = false;
                    SendToParticipants(session, new ServerP2PTradeResult
                    {
                        Result = commitResult
                    });
                    return null;
                }

                P2PTradeResult? failure = ValidateOffer(player, offer);
                if (failure.HasValue)
                {
                    Finish(session, failure.Value, true);
                    return null;
                }

                offer.Committed = true;
                SendToParticipants(session, new ServerP2PTradeResult
                {
                    Result = commitResult
                });

                if (session.InitiatorOffer.Committed && session.TargetOffer.Committed)
                    sessionToSettle = session;
            }

            if (sessionToSettle != null)
                Settle(sessionToSettle);

            return null;
        }

        public void Update(double lastTick)
        {
            lock (syncRoot)
            {
                foreach (TradeSession session in sessions.ToArray())
                {
                    if (!IsSessionStillValid(session))
                    {
                        Finish(session, P2PTradeResult.MissingPlayer, true);
                        continue;
                    }

                    if (session.State != TradeState.Pending)
                        continue;

                    session.Timer -= lastTick;
                    if (session.Timer <= 0d)
                        Finish(session, P2PTradeResult.PlayerCanceled, true);
                }
            }
        }

        private static P2PTradeResult? GetPreflightFailure(IPlayer initiator, IPlayer target)
        {
            if (initiator == null || target == null || initiator.Guid == target.Guid)
                return P2PTradeResult.MissingPlayer;

            if (!initiator.InWorld || !target.InWorld || initiator.Map != target.Map)
                return P2PTradeResult.MissingPlayer;

            if (!initiator.IsAlive || !target.IsAlive)
                return P2PTradeResult.TargetNotAllowedToTrade;

            return null;
        }

        private P2PTradeResult? GetSessionFailure(IPlayer initiator, IPlayer target)
        {
            if (sessionsByPlayer.ContainsKey(initiator.Guid))
                return P2PTradeResult.ErrorInitiating;

            if (sessionsByPlayer.ContainsKey(target.Guid))
                return P2PTradeResult.TargetBusy;

            return null;
        }

        private static bool IsSessionStillValid(TradeSession session)
        {
            return session.Initiator.InWorld
                && session.Target.InWorld
                && session.Initiator.Map == session.Target.Map;
        }

        private bool TryGetSession(IPlayer player, out TradeSession session)
        {
            session = null;
            return player != null && sessionsByPlayer.TryGetValue(player.Guid, out session);
        }

        private bool TryGetActiveSession(IPlayer player, out TradeSession session)
        {
            if (!TryGetSession(player, out session))
                return false;

            return session.State == TradeState.Active;
        }

        private static TradeOffer GetOffer(TradeSession session, IPlayer player)
        {
            return session.Initiator.Guid == player.Guid
                ? session.InitiatorOffer
                : session.TargetOffer;
        }

        private static bool IsItemOffered(TradeSession session, ulong itemGuid)
        {
            return session.InitiatorOffer.ItemGuids.Contains(itemGuid)
                || session.TargetOffer.ItemGuids.Contains(itemGuid);
        }

        private static bool IsTradableItem(IPlayer owner, IItem item)
        {
            return item != null
                && item.Info != null
                && item.CharacterId == owner.CharacterId
                && item.Location == InventoryLocation.Inventory
                && !item.Soulbound
                && !item.Info.IsEquippableBag();
        }

        private void ResetCommits(TradeSession session)
        {
            ResetCommit(session, session.InitiatorOffer, P2PTradeResult.InitiatorUnCommitted);
            ResetCommit(session, session.TargetOffer, P2PTradeResult.TargetUnCommitted);
        }

        private void ResetCommit(TradeSession session, TradeOffer offer, P2PTradeResult result)
        {
            if (!offer.Committed)
                return;

            offer.Committed = false;
            SendToParticipants(session, new ServerP2PTradeResult
            {
                Result = result
            });
        }

        private static P2PTradeResult GetCommitResult(TradeSession session, IPlayer player, bool committed)
        {
            bool isInitiator = session.Initiator.Guid == player.Guid;

            return (isInitiator, committed) switch
            {
                (true, true)   => P2PTradeResult.InitiatorCommitted,
                (true, false)  => P2PTradeResult.InitiatorUnCommitted,
                (false, true)  => P2PTradeResult.TargetCommitted,
                (false, false) => P2PTradeResult.TargetUnCommitted
            };
        }

        private static P2PTradeResult? ValidateOffer(IPlayer player, TradeOffer offer)
        {
            foreach (ulong itemGuid in offer.ItemGuids)
            {
                IItem item = player.Inventory.GetItem(itemGuid);
                if (!IsTradableItem(player, item))
                    return P2PTradeResult.ErrorAddingItem;
            }

            if (!player.CurrencyManager.CanAfford(CurrencyType.Credits, offer.Credits))
                return P2PTradeResult.ErrorInitiating;

            return null;
        }

        private static P2PTradeResult? ValidateSettlement(TradeSession session)
        {
            if (!HasInventorySpaceForExchange(session.Initiator, session.InitiatorOffer, session.TargetOffer)
                || !HasInventorySpaceForExchange(session.Target, session.TargetOffer, session.InitiatorOffer))
                return P2PTradeResult.ErrorAddingItem;

            if (!CanReceiveCredits(session.Initiator, session.TargetOffer.Credits, session.InitiatorOffer.Credits)
                || !CanReceiveCredits(session.Target, session.InitiatorOffer.Credits, session.TargetOffer.Credits))
                return P2PTradeResult.ErrorInitiating;

            return null;
        }

        private static bool HasInventorySpaceForExchange(IPlayer player, TradeOffer outgoingOffer, TradeOffer incomingOffer)
        {
            ulong freeSlotsAfterOutgoing = player.Inventory.GetInventorySlotsRemaining(InventoryLocation.Inventory)
                + (ulong)outgoingOffer.ItemGuids.Count;

            return freeSlotsAfterOutgoing >= (ulong)incomingOffer.ItemGuids.Count;
        }

        private static bool CanReceiveCredits(IPlayer player, ulong incomingCredits, ulong outgoingCredits)
        {
            if (incomingCredits == 0ul)
                return true;

            ICurrency currency = player.CurrencyManager.FirstOrDefault(c => c.Id == CurrencyType.Credits);
            if (currency?.Entry.CapAmount is not > 0ul)
                return true;

            if (currency.Amount < outgoingCredits)
                return false;

            ulong amountAfterOutgoing = currency.Amount - outgoingCredits;

            return amountAfterOutgoing <= currency.Entry.CapAmount
                && incomingCredits <= currency.Entry.CapAmount - amountAfterOutgoing;
        }

        private void Settle(TradeSession session)
        {
            lock (syncRoot)
            {
                if (!sessions.Contains(session))
                    return;

                if (!HasAnyOffer(session))
                {
                    Finish(session, P2PTradeResult.NothingToTrade, true);
                    return;
                }

                P2PTradeResult? initiatorFailure = ValidateOffer(session.Initiator, session.InitiatorOffer);
                if (initiatorFailure.HasValue)
                {
                    Finish(session, initiatorFailure.Value, true);
                    return;
                }

                P2PTradeResult? targetFailure = ValidateOffer(session.Target, session.TargetOffer);
                if (targetFailure.HasValue)
                {
                    Finish(session, targetFailure.Value, true);
                    return;
                }

                P2PTradeResult? settlementFailure = ValidateSettlement(session);
                if (settlementFailure.HasValue)
                {
                    Finish(session, settlementFailure.Value, true);
                    return;
                }

                RemoveSession(session);
            }

            TransferOffers(session);

            SendToParticipants(session, new ServerP2PTradeResult
            {
                Result    = P2PTradeResult.FinishedSuccess,
                Cancelled = false
            });
        }

        private static bool HasAnyOffer(TradeSession session)
        {
            return session.InitiatorOffer.Credits > 0ul
                || session.TargetOffer.Credits > 0ul
                || session.InitiatorOffer.ItemGuids.Count > 0
                || session.TargetOffer.ItemGuids.Count > 0;
        }

        private static void TransferOffers(TradeSession session)
        {
            List<IItem> initiatorItems = RemoveOfferItems(session.Initiator, session.InitiatorOffer);
            List<IItem> targetItems = RemoveOfferItems(session.Target, session.TargetOffer);

            SubtractCredits(session.Initiator, session.InitiatorOffer.Credits);
            SubtractCredits(session.Target, session.TargetOffer.Credits);

            AddOfferItems(session.Target, initiatorItems);
            AddOfferItems(session.Initiator, targetItems);

            AddCredits(session.Target, session.InitiatorOffer.Credits);
            AddCredits(session.Initiator, session.TargetOffer.Credits);
        }

        private static List<IItem> RemoveOfferItems(IPlayer from, TradeOffer offer)
        {
            var items = new List<IItem>(offer.ItemGuids.Count);
            foreach (ulong itemGuid in offer.ItemGuids)
            {
                IItem item = from.Inventory.GetItem(itemGuid);
                from.Inventory.ItemRemove(item, ItemUpdateReason.Trade);
                items.Add(item);
            }

            return items;
        }

        private static void AddOfferItems(IPlayer to, IEnumerable<IItem> items)
        {
            foreach (IItem item in items)
            {
                item.CharacterId = to.CharacterId;
                to.Inventory.AddItem(item, InventoryLocation.Inventory, ItemUpdateReason.Trade);
            }
        }

        private static void SubtractCredits(IPlayer player, ulong credits)
        {
            if (credits == 0ul)
                return;

            player.CurrencyManager.CurrencySubtractAmount(CurrencyType.Credits, credits);
        }

        private static void AddCredits(IPlayer player, ulong credits)
        {
            if (credits == 0ul)
                return;

            player.CurrencyManager.CurrencyAddAmount(CurrencyType.Credits, credits);
        }

        private void Finish(TradeSession session, P2PTradeResult result, bool cancelled)
        {
            RemoveSession(session);

            SendToParticipants(session, new ServerP2PTradeResult
            {
                Result    = result,
                Cancelled = cancelled
            });
        }

        private void RemoveSession(TradeSession session)
        {
            sessions.Remove(session);
            sessionsByPlayer.Remove(session.Initiator.Guid);
            sessionsByPlayer.Remove(session.Target.Guid);
        }

        private static void SendToParticipants(TradeSession session, IWritable message)
        {
            session.Initiator.Session?.EnqueueMessageEncrypted(message);
            session.Target.Session?.EnqueueMessageEncrypted(message);
        }
    }
}
