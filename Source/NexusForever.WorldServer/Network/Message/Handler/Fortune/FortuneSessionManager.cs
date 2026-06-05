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
using NexusForever.Game.Abstract.Account.Inventory;
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
        private const uint ClickEmptyResetCode = 3u;

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

            if (!EnsureFortuneCoinBalance(session, account))
            {
                SendClickEmptyReset(session);
                return;
            }

            FortuneCardReward[] cardRewards = fortuneRewardPool.PickCardRewards(Random.Shared);
            if (cardRewards.Length != CardCount || !AreCardRewardsDisplayable(cardRewards))
            {
                SendClickEmptyReset(session);
                return;
            }

            var fortuneSession = new FortuneSession(cardRewards);
            if (!TryPersistSession(account.Id, fortuneSession))
            {
                SendClickEmptyReset(session);
                return;
            }

            account.CurrencyManager.CurrencySubtractAmount(AccountCurrencyType.FortuneCoin, FortuneCoinCost);

            lock (sync)
                sessions[account.Id] = fortuneSession;

            session.EnqueueMessageEncrypted(BuildCards(fortuneSession));
        }

        private bool EnsureFortuneCoinBalance(IWorldSession session, IAccount account)
        {
            if (account.CurrencyManager.CanAfford(AccountCurrencyType.FortuneCoin, FortuneCoinCost))
                return true;

            if (!TryClaimFortuneCoinAccountItem(account, session.Player))
                return false;

            return account.CurrencyManager.CanAfford(AccountCurrencyType.FortuneCoin, FortuneCoinCost);
        }

        private bool TryClaimFortuneCoinAccountItem(IAccount account, IPlayer player)
        {
            IAccountInventoryManager inventoryManager = account.InventoryManager;
            if (inventoryManager == null)
                return false;

            foreach (IAccountInventoryItem item in GetClaimableFortuneCoinItems(inventoryManager, player))
            {
                AccountOperationResult result = inventoryManager.TakeItem(player, item.Id);
                log.LogDebug(
                    "Fortune start auto-claim account {AccountId}: inventoryId={InventoryId}, accountItemId={AccountItemId}, amount={Amount}, result={Result}.",
                    account.Id,
                    item.Id,
                    item.AccountItemId,
                    item.Entry.AccountCurrencyAmount,
                    result);
                if (result == AccountOperationResult.Ok)
                    return true;
            }

            return false;
        }

        private static List<IAccountInventoryItem> GetClaimableFortuneCoinItems(IAccountInventoryManager inventoryManager, IPlayer player)
        {
            var items = new List<IAccountInventoryItem>();
            IEnumerator<IAccountInventoryItem> enumerator = inventoryManager.GetEnumerator();
            if (enumerator == null)
                return items;

            using (enumerator)
            {
                while (enumerator.MoveNext())
                {
                    IAccountInventoryItem item = enumerator.Current;
                    if (item?.Entry == null)
                        continue;

                    if (item.ClaimState != AccountItemClaimState.CanClaim)
                        continue;

                    if (!IsTargetPlayer(player, item.TargetPlayerIdentity))
                        continue;

                    if (item.Entry.AccountCurrencyEnum != (uint)AccountCurrencyType.FortuneCoin)
                        continue;

                    if (item.Entry.AccountCurrencyAmount == 0ul)
                        continue;

                    items.Add(item);
                }
            }

            return items;
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
                sessions.TryGetValue(account.Id, out fortuneSession);
            }

            if (fortuneSession == null)
            {
                fortuneSession = LoadSession(account.Id);
                if (fortuneSession == null)
                {
                    SendClickEmptyReset(session);
                    return;
                }

                lock (sync)
                    sessions[account.Id] = fortuneSession;
            }

            lock (sync)
            {
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

            AuthDatabase authDatabase = databaseManager.GetDatabase<AuthDatabase>();
            if (authDatabase == null)
                return null;

            AccountFortuneSessionModel model = authDatabase.GetFortuneSession(accountId);
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

            if (!AreCardsDisplayable(cards))
            {
                log.LogWarning(
                    "Ignoring stored Fortune session for account {AccountId}: contains non-displayable account item ids [{AccountItemIds}].",
                    accountId,
                    string.Join(",", cards.Select(card => card.AccountItemId)));
                authDatabase.DeleteFortuneSession(accountId);
                return null;
            }

            return new FortuneSession(cards);
        }

        private static bool HasActiveCards(AccountFortuneSessionModel model)
        {
            return model.Card0AccountItemId != 0u
                || model.Card1AccountItemId != 0u
                || model.Card2AccountItemId != 0u;
        }

        private bool AreCardRewardsDisplayable(IEnumerable<FortuneCardReward> cardRewards)
        {
            return cardRewards.All(reward => fortuneRewardPool.IsCardRewardDisplayable(reward.AccountItemId));
        }

        private bool AreCardsDisplayable(IEnumerable<FortuneCardState> cards)
        {
            return cards.All(card => fortuneRewardPool.IsCardRewardDisplayable(card.AccountItemId));
        }

        private bool TryPersistSession(uint accountId, FortuneSession fortuneSession)
        {
            if (databaseManager == null || fortuneSession == null)
                return databaseManager == null;

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

            try
            {
                databaseManager.GetDatabase<AuthDatabase>().UpsertFortuneSession(model);
                return true;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Failed to persist Fortune session for account {AccountId}.", accountId);
                return false;
            }
        }

        private void PersistSession(uint accountId, FortuneSession fortuneSession)
        {
            TryPersistSession(accountId, fortuneSession);
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
            account.InventoryManager.AddItem(
                card.AccountItemId,
                targetIdentity,
                hasTargetPlayerIdentity: targetIdentity?.Id != 0ul);
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

        private static bool IsTargetPlayer(IPlayer player, NetworkIdentity targetPlayerIdentity)
        {
            if (targetPlayerIdentity == null || targetPlayerIdentity.Id == 0ul)
                return true;

            if (player?.Identity == null)
                return false;

            return targetPlayerIdentity.Id == player.Identity.Id &&
                (targetPlayerIdentity.RealmId == 0u || targetPlayerIdentity.RealmId == player.Identity.RealmId);
        }

        private static void SendClickEmptyReset(IWorldSession session)
        {
            session.EnqueueMessageEncrypted(new ServerFortuneReset
            {
                ResetCode = ClickEmptyResetCode
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
