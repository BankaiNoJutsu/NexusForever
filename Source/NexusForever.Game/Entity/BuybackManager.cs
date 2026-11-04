using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Entity
{
    public sealed class BuybackManager : IBuybackManager
    {
        private readonly Dictionary<ulong /*CharacterId*/, IBuybackInfo> buybackInfo = new();
        private readonly IPlayerManager playerManager;
        private int lastUniqueId = -1;

        public BuybackManager(IPlayerManager playerManager = null)
        {
            this.playerManager = playerManager;
        }

        public void Update(double lastTick)
        {
            foreach ((ulong characterId, IBuybackInfo info) in buybackInfo)
            {
                foreach (IBuybackItem buybackItem in info.ToArray())
                {
                    if (buybackItem is BuybackItem { IsRedeemed: true })
                    {
                        info.RemoveItem(buybackItem.UniqueId);
                        continue;
                    }
                    buybackItem.Update(lastTick);
                    if (!buybackItem.HasExpired || buybackItem is BuybackItem { IsAvailableForRedemption: false })
                        continue;

                    info.RemoveItem(buybackItem.UniqueId);

                    IPlayer player = playerManager?.GetPlayer(characterId);
                    player?.Session?.EnqueueMessageEncrypted(new ServerBuybackItemRemoved
                    {
                        UniqueId = buybackItem.UniqueId
                    });
                }
            }
        }

        /// <summary>
        /// Return stored <see cref="IBuybackItem"/> for <see cref="IPlayer"/> with unique id.
        /// </summary>
        public IBuybackItem GetItem(IPlayer player, uint uniqueId)
        {
            IBuybackItem item = buybackInfo.TryGetValue(player.CharacterId, out IBuybackInfo info) ? info.GetItem(uniqueId) : null;
            return item is BuybackItem { IsAvailableForRedemption: false } ? null : item;
        }

        /// <summary>
        /// Create a new <see cref="IBuybackItem"/> from sold <see cref="IItem"/> for <see cref="IPlayer"/>.
        /// </summary>
        public void AddItem(IPlayer player, IItem item, uint quantity, List<(CurrencyType CurrencyTypeId, ulong CurrencyAmount)> currencyChange)
        {
            if (!buybackInfo.ContainsKey(player.CharacterId))
                buybackInfo.Add(player.CharacterId, new BuybackInfo());

            // A request can arrive after the character's previous list was emptied.
            uint uniqueId = unchecked((uint)Interlocked.Increment(ref lastUniqueId));
            var entry = new BuybackItem(uniqueId, item, quantity, currencyChange);
            buybackInfo[player.CharacterId].AddItem(entry);

            player.Session.EnqueueMessageEncrypted(new ServerBuybackItemUpdated
            {
                BuybackItem = entry.Build()
            });
        }

        /// <summary>
        /// Remove the original stored <see cref="IBuybackItem"/> for <see cref="IPlayer"/>.
        /// </summary>
        public void RemoveItem(IPlayer player, IBuybackItem item, bool notify = true)
        {
            if (!buybackInfo.TryGetValue(player.CharacterId, out IBuybackInfo info)
                || !ReferenceEquals(info.GetItem(item.UniqueId), item))
                return;

            info.RemoveItem(item.UniqueId);
            if (!info.Any())
                buybackInfo.Remove(player.CharacterId);

            var session = player.Session;
            if (notify && (session is not IPlayerSession bound || ReferenceEquals(bound.Player, player)))
                session?.EnqueueMessageEncrypted(new ServerBuybackItemRemoved { UniqueId = item.UniqueId });
        }

        /// <summary>
        /// Send <see cref="ServerBuybackItems"/> for <see cref="IPlayer"/>.
        /// </summary>
        public void SendBuybackItems(IPlayer player)
        {
            if (!buybackInfo.TryGetValue(player.CharacterId, out IBuybackInfo info))
                return;

            var serverBuybackItems = new ServerBuybackItems();
            foreach (IBuybackItem buybackItem in info)
                if (buybackItem is not BuybackItem { IsRedeemed: true })
                    serverBuybackItems.BuybackItems.Add(buybackItem.Build());

            player.Session.EnqueueMessageEncrypted(serverBuybackItems);
        }
    }
}
