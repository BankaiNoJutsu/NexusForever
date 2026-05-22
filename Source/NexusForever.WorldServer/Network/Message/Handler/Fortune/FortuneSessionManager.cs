using System;
using System.Collections.Generic;
using System.Linq;
using NexusForever.Game;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Fortune;
using NexusForever.Game.Static.Account;
using NexusForever.Game.Static.Fortune;
using NexusForever.Network.World.Message.Model.Fortune;
using NexusForever.Shared;
using NetworkIdentity = NexusForever.Network.World.Message.Model.Shared.Identity;

namespace NexusForever.WorldServer.Network.Message.Handler.Fortune
{
    public class FortuneSessionManager : IFortuneSessionManager
    {
        private const int CardCount = 3;
        private const ulong FortuneCoinCost = 1ul;
        private const uint ResetClickEmpty = 3u;

        private readonly IFortuneRewardPool fortuneRewardPool;
        private readonly IRealmContext realmContext;
        private readonly Dictionary<uint, FortuneSession> sessions = [];
        private readonly object sync = new();

        public FortuneSessionManager(IFortuneRewardPool fortuneRewardPool, IRealmContext realmContext)
        {
            this.fortuneRewardPool = fortuneRewardPool;
            this.realmContext       = realmContext;
        }

        public void SendStatus(IWorldSession session)
        {
            session.EnqueueMessageEncrypted(BuildRewards());
            session.EnqueueMessageEncrypted(BuildCards(GetSession(session)));
        }

        public void Start(IWorldSession session)
        {
            if (!TryGetAccount(session, out IAccount account))
            {
                SendReset(session);
                return;
            }

            if (!account.CurrencyManager.CanAfford(AccountCurrencyType.FortuneCoin, FortuneCoinCost))
            {
                SendReset(session);
                return;
            }

            FortuneCardReward[] cardRewards = fortuneRewardPool.PickCardRewards(Random.Shared);
            if (cardRewards.Length != CardCount)
            {
                SendReset(session);
                return;
            }

            account.CurrencyManager.CurrencySubtractAmount(AccountCurrencyType.FortuneCoin, FortuneCoinCost);

            var fortuneSession = new FortuneSession(cardRewards);
            lock (sync)
                sessions[account.Id] = fortuneSession;

            session.EnqueueMessageEncrypted(BuildCards(fortuneSession));
        }

        public void FlipCard(IWorldSession session, ClientFortuneFlipCard flipCard)
        {
            if (flipCard.SelectedCardIndex >= CardCount || !TryGetAccount(session, out IAccount account))
            {
                SendReset(session);
                return;
            }

            FortuneSession fortuneSession;
            lock (sync)
            {
                if (!sessions.TryGetValue(account.Id, out fortuneSession))
                {
                    SendReset(session);
                    return;
                }

                FortuneCardState card = fortuneSession.Cards[flipCard.SelectedCardIndex];
                if (card.Flipped)
                {
                    SendReset(session);
                    return;
                }

                card.Flipped = true;
                GrantCardReward(account, session.Player, card);
            }

            session.EnqueueMessageEncrypted(new ServerFortuneCardUpdate
            {
                Operation   = FortuneOperation.Update,
                CardFlipped = CopyCardFlips(fortuneSession)
            });
        }

        private FortuneSession GetSession(IWorldSession session)
        {
            if (!TryGetAccountId(session, out uint accountId))
                return null;

            lock (sync)
                return sessions.GetValueOrDefault(accountId);
        }

        private ServerFortuneRewards BuildRewards()
        {
            return new ServerFortuneRewards
            {
                Item2IdRewards = fortuneRewardPool.GetDisplayItem2Ids().ToList()
            };
        }

        private static ServerFortuneCards BuildCards(FortuneSession fortuneSession)
        {
            if (fortuneSession == null)
            {
                return new ServerFortuneCards
                {
                    Operation     = FortuneOperation.Reset,
                    Rarity        = [RewardRarity.Normal, RewardRarity.Normal, RewardRarity.Normal],
                    AccountItemId = [0u, 0u, 0u],
                    CardFlipped   = [false, false, false]
                };
            }

            return new ServerFortuneCards
            {
                Operation     = FortuneOperation.Update,
                Rarity        = fortuneSession.Cards.Select(card => card.Rarity).ToArray(),
                AccountItemId = fortuneSession.Cards.Select(card => card.AccountItemId).ToArray(),
                CardFlipped   = CopyCardFlips(fortuneSession)
            };
        }

        private static bool[] CopyCardFlips(FortuneSession fortuneSession)
        {
            if (fortuneSession == null)
                return [false, false, false];

            return fortuneSession.Cards.Select(card => card.Flipped).ToArray();
        }

        private void GrantCardReward(IAccount account, IPlayer player, FortuneCardState card)
        {
            if (card.Granted || card.AccountItemId == 0u)
                return;

            NetworkIdentity targetIdentity = BuildTargetIdentity(player);
            account.InventoryManager.AddItem(card.AccountItemId, targetIdentity);
            card.Granted = true;
        }

        private NetworkIdentity BuildTargetIdentity(IPlayer player)
        {
            if (player == null)
                return null;

            return new NetworkIdentity
            {
                RealmId = realmContext.RealmId,
                Id      = player.CharacterId
            };
        }

        private static void SendReset(IWorldSession session)
        {
            session.EnqueueMessageEncrypted(new ServerFortuneReset
            {
                Unknown = ResetClickEmpty
            });
        }

        private static bool TryGetAccount(IWorldSession session, out IAccount account)
        {
            account = session?.Account;
            return account != null && account.Id != 0u;
        }

        private static bool TryGetAccountId(IWorldSession session, out uint accountId)
        {
            accountId = session?.Account?.Id ?? 0u;
            return accountId != 0u;
        }

        private sealed class FortuneSession
        {
            public FortuneCardState[] Cards { get; }

            public FortuneSession(FortuneCardReward[] cardRewards)
            {
                Cards = cardRewards
                    .Select(reward => new FortuneCardState(reward.AccountItemId, reward.Rarity))
                    .ToArray();
            }
        }

        private sealed class FortuneCardState
        {
            public uint AccountItemId { get; }
            public RewardRarity Rarity { get; }
            public bool Flipped { get; set; }
            public bool Granted { get; set; }

            public FortuneCardState(uint accountItemId, RewardRarity rarity)
            {
                AccountItemId = accountItemId;
                Rarity        = rarity;
            }
        }
    }
}
