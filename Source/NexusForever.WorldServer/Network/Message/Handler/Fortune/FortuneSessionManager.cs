using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Database;
using NexusForever.Database.Auth;
using NexusForever.Database.Auth.Model;
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
        private const uint ClickEmptyResetValue = 3u;

        private readonly IFortuneRewardPool fortuneRewardPool;
        private readonly IRealmContext realmContext;
        private readonly IDatabaseManager databaseManager;
        private readonly ILogger<FortuneSessionManager> log;
        private readonly Dictionary<uint, FortuneSession> sessions = [];
        private readonly object sync = new();

        public FortuneSessionManager(IFortuneRewardPool fortuneRewardPool, IRealmContext realmContext)
            : this(fortuneRewardPool, realmContext, null, NullLogger<FortuneSessionManager>.Instance)
        {
        }

        public FortuneSessionManager(
            IFortuneRewardPool fortuneRewardPool,
            IRealmContext realmContext,
            IDatabaseManager databaseManager)
            : this(fortuneRewardPool, realmContext, databaseManager, NullLogger<FortuneSessionManager>.Instance)
        {
        }

        public FortuneSessionManager(
            IFortuneRewardPool fortuneRewardPool,
            IRealmContext realmContext,
            IDatabaseManager databaseManager,
            ILogger<FortuneSessionManager> log)
        {
            this.fortuneRewardPool = fortuneRewardPool;
            this.realmContext       = realmContext;
            this.databaseManager    = databaseManager;
            this.log                = log;
        }

        public void SendStatus(IWorldSession session)
        {
            ServerFortuneRewards rewards = BuildRewards();
            LogRewardCatalogProbabilities(rewards);
            session.EnqueueMessageEncrypted(rewards);
            session.EnqueueMessageEncrypted(BuildCards(GetSession(session)));
        }

        public void Start(IWorldSession session)
        {
            if (!TryGetAccount(session, out IAccount account))
            {
                SendClickEmptyReset(session);
                return;
            }

            if (!account.CurrencyManager.CanAfford(AccountCurrencyType.FortuneCoin, FortuneCoinCost))
            {
                SendClickEmptyReset(session);
                return;
            }

            FortuneCardReward[] cardRewards = fortuneRewardPool.PickCardRewards(Random.Shared);
            if (cardRewards.Length != CardCount)
            {
                SendClickEmptyReset(session);
                return;
            }

            account.CurrencyManager.CurrencySubtractAmount(AccountCurrencyType.FortuneCoin, FortuneCoinCost);

            var fortuneSession = new FortuneSession(cardRewards);
            lock (sync)
                sessions[account.Id] = fortuneSession;

            PersistSession(account.Id, fortuneSession);
            session.EnqueueMessageEncrypted(BuildCards(fortuneSession));
        }

        public void FlipCard(IWorldSession session, ClientFortuneFlipCard flipCard)
        {
            if (flipCard.SelectedCardIndex >= CardCount || !TryGetAccount(session, out IAccount account))
            {
                SendClickEmptyReset(session);
                return;
            }

            FortuneSession fortuneSession;
            lock (sync)
            {
                if (!sessions.TryGetValue(account.Id, out fortuneSession))
                {
                    fortuneSession = LoadSession(account.Id);
                    if (fortuneSession == null)
                    {
                        SendClickEmptyReset(session);
                        return;
                    }

                    sessions[account.Id] = fortuneSession;
                }

                FortuneCardState card = fortuneSession.Cards[flipCard.SelectedCardIndex];
                if (card.Flipped)
                {
                    SendClickEmptyReset(session);
                    return;
                }

                card.Flipped = true;
                GrantCardReward(account, session.Player, card);
            }

            PersistSession(account.Id, fortuneSession);

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
            {
                if (sessions.TryGetValue(accountId, out FortuneSession fortuneSession))
                    return fortuneSession;

                fortuneSession = LoadSession(accountId);
                if (fortuneSession != null)
                    sessions[accountId] = fortuneSession;

                return fortuneSession;
            }
        }

        private FortuneSession LoadSession(uint accountId)
        {
            if (databaseManager == null)
                return null;

            AccountFortuneSessionModel model = databaseManager.GetDatabase<AuthDatabase>().GetFortuneSession(accountId);
            if (model == null || !HasActiveCards(model))
                return null;

            var cards = new[]
            {
                new FortuneCardState(model.Card0AccountItemId, (RewardRarity)model.Card0Rarity, model.Card0Flipped, model.Card0Granted),
                new FortuneCardState(model.Card1AccountItemId, (RewardRarity)model.Card1Rarity, model.Card1Flipped, model.Card1Granted),
                new FortuneCardState(model.Card2AccountItemId, (RewardRarity)model.Card2Rarity, model.Card2Flipped, model.Card2Granted)
            };

            if (cards.All(card => card.AccountItemId == 0u))
                return null;

            return new FortuneSession(cards);
        }

        private static bool HasActiveCards(AccountFortuneSessionModel model)
        {
            return model.Card0AccountItemId != 0u
                || model.Card1AccountItemId != 0u
                || model.Card2AccountItemId != 0u;
        }

        private void PersistSession(uint accountId, FortuneSession fortuneSession)
        {
            if (databaseManager == null || fortuneSession == null)
                return;

            var model = new AccountFortuneSessionModel
            {
                Id                 = accountId,
                Card0AccountItemId = fortuneSession.Cards[0].AccountItemId,
                Card1AccountItemId = fortuneSession.Cards[1].AccountItemId,
                Card2AccountItemId = fortuneSession.Cards[2].AccountItemId,
                Card0Rarity        = (byte)fortuneSession.Cards[0].Rarity,
                Card1Rarity        = (byte)fortuneSession.Cards[1].Rarity,
                Card2Rarity        = (byte)fortuneSession.Cards[2].Rarity,
                Card0Flipped       = fortuneSession.Cards[0].Flipped,
                Card1Flipped       = fortuneSession.Cards[1].Flipped,
                Card2Flipped       = fortuneSession.Cards[2].Flipped,
                Card0Granted       = fortuneSession.Cards[0].Granted,
                Card1Granted       = fortuneSession.Cards[1].Granted,
                Card2Granted       = fortuneSession.Cards[2].Granted
            };

            databaseManager.GetDatabase<AuthDatabase>().UpsertFortuneSession(model);
        }

        private ServerFortuneRewards BuildRewards()
        {
            FortuneRewardCatalog catalog = fortuneRewardPool.GetRewardCatalog();
            return new ServerFortuneRewards
            {
                Item2IdRewards          = catalog.Item2IdRewards.ToList(),
                RewardItemProbabilities = catalog.RewardItemProbabilities.ToList()
            };
        }

        private void LogRewardCatalogProbabilities(ServerFortuneRewards rewards)
        {
            if (!log.IsEnabled(LogLevel.Debug) || rewards.Item2IdRewards.Count == 0)
                return;

            log.LogDebug(
                "Fortune rewards catalog itemCount={ItemCount} (emulator rarity-tier weights; retail per-item weights blocked)",
                rewards.Item2IdRewards.Count);

            int sampleCount = Math.Min(rewards.Item2IdRewards.Count, 8);
            for (int i = 0; i < sampleCount; i++)
            {
                float probability = i < rewards.RewardItemProbabilities.Count
                    ? rewards.RewardItemProbabilities[i]
                    : 0f;
                log.LogDebug(
                    "Fortune rewards sample[{Index}] item2Id={Item2Id} probability={Probability}",
                    i,
                    rewards.Item2IdRewards[i],
                    probability);
            }
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

        private static void SendClickEmptyReset(IWorldSession session)
        {
            session.EnqueueMessageEncrypted(new ServerFortuneReset
            {
                Unknown = ClickEmptyResetValue
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

            public FortuneSession(FortuneCardState[] cards)
            {
                Cards = cards;
            }
        }

        private sealed class FortuneCardState
        {
            public uint AccountItemId { get; }
            public RewardRarity Rarity { get; }
            public bool Flipped { get; set; }
            public bool Granted { get; set; }

            public FortuneCardState(uint accountItemId, RewardRarity rarity, bool flipped = false, bool granted = false)
            {
                AccountItemId = accountItemId;
                Rarity        = rarity;
                Flipped       = flipped;
                Granted       = granted;
            }
        }
    }
}
