using System.Collections.Generic;
using System.Linq;
using NexusForever.Game.Static.Fortune;
using NexusForever.Network.World.Message.Model.Fortune;

namespace NexusForever.WorldServer.Network.Message.Handler.Fortune
{
    public class FortuneSessionManager : IFortuneSessionManager
    {
        private const int CardCount = 3;
        private const uint ResetClickEmpty = 3u;

        private readonly Dictionary<uint, FortuneSession> sessions = [];
        private readonly object sync = new();

        public void SendStatus(IWorldSession session)
        {
            session.EnqueueMessageEncrypted(new ServerFortuneRewards());
            session.EnqueueMessageEncrypted(BuildCards(GetSession(session)));
        }

        public void Start(IWorldSession session)
        {
            if (!TryGetAccountId(session, out uint accountId))
            {
                SendReset(session);
                return;
            }

            var fortuneSession = new FortuneSession();
            lock (sync)
                sessions[accountId] = fortuneSession;

            session.EnqueueMessageEncrypted(BuildCards(fortuneSession));
        }

        public void FlipCard(IWorldSession session, ClientFortuneFlipCard flipCard)
        {
            if (flipCard.SelectedCardIndex >= CardCount || !TryGetAccountId(session, out uint accountId))
            {
                SendReset(session);
                return;
            }

            FortuneSession fortuneSession;
            lock (sync)
            {
                if (!sessions.TryGetValue(accountId, out fortuneSession))
                {
                    SendReset(session);
                    return;
                }

                fortuneSession.CardFlipped[flipCard.SelectedCardIndex] = true;
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

        private static ServerFortuneCards BuildCards(FortuneSession fortuneSession)
        {
            return new ServerFortuneCards
            {
                Operation     = fortuneSession == null ? FortuneOperation.Reset : FortuneOperation.Update,
                Rarity        = [RewardRarity.Normal, RewardRarity.Normal, RewardRarity.Normal],
                AccountItemId = [0u, 0u, 0u],
                CardFlipped   = CopyCardFlips(fortuneSession)
            };
        }

        private static bool[] CopyCardFlips(FortuneSession fortuneSession)
        {
            if (fortuneSession == null)
                return [false, false, false];

            return fortuneSession.CardFlipped.ToArray();
        }

        private static void SendReset(IWorldSession session)
        {
            session.EnqueueMessageEncrypted(new ServerFortuneReset
            {
                Unknown = ResetClickEmpty
            });
        }

        private static bool TryGetAccountId(IWorldSession session, out uint accountId)
        {
            accountId = session?.Account?.Id ?? 0u;
            return accountId != 0u;
        }

        private sealed class FortuneSession
        {
            public bool[] CardFlipped { get; } = new bool[CardCount];
        }
    }
}
