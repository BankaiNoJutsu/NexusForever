using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Pvp;
using NexusForever.Game.Static.Achievement;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Pvp;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Pvp;
using NexusForever.Shared;

namespace NexusForever.Game.Pvp
{
    public sealed class DuelManager : Singleton<DuelManager>, IDuelManager
    {
        private const double ChallengeTimeoutSeconds = 30d;
        private const double CountdownSeconds = 3d;

        private enum DuelState
        {
            Pending,
            Countdown,
            Active
        }

        private sealed class DuelSession
        {
            public IPlayer Challenger { get; init; }
            public IPlayer Opponent { get; init; }
            public DuelState State { get; set; }
            public double Timer { get; set; }
        }

        private readonly object syncRoot = new();
        private readonly List<DuelSession> sessions = [];
        private readonly Dictionary<uint, DuelSession> sessionsByPlayer = [];

        public DuelFailureReason? Initiate(IPlayer challenger, IPlayer opponent)
        {
            DuelFailureReason? failure = GetPreflightFailure(challenger, opponent);
            if (failure.HasValue)
                return failure;

            lock (syncRoot)
            {
                failure = GetSessionFailure(challenger, opponent);
                if (failure.HasValue)
                    return failure;

                var session = new DuelSession
                {
                    Challenger = challenger,
                    Opponent   = opponent,
                    State      = DuelState.Pending,
                    Timer      = ChallengeTimeoutSeconds
                };

                sessions.Add(session);
                sessionsByPlayer.Add(challenger.Guid, session);
                sessionsByPlayer.Add(opponent.Guid, session);

                SendToParticipants(session, new ServerDuelChallenge
                {
                    ChallengerUnitId = challenger.Guid,
                    OpponentUnitId   = opponent.Guid
                });
            }

            return null;
        }

        public DuelFailureReason? Accept(IPlayer player)
        {
            lock (syncRoot)
            {
                if (!TryGetSession(player, out DuelSession session))
                    return DuelFailureReason.CannotDuelRightNow;

                if (session.State != DuelState.Pending || session.Opponent.Guid != player.Guid)
                    return DuelFailureReason.CannotDuelRightNow;

                DuelFailureReason? failure = GetPreflightFailure(session.Challenger, session.Opponent);
                if (failure.HasValue)
                {
                    Finish(session, session.Challenger, session.Opponent, DuelFinishReason.DuelCancelled);
                    return failure;
                }

                session.State = DuelState.Countdown;
                session.Timer = CountdownSeconds;

                SendToParticipants(session, new ServerDuelCountdown
                {
                    ChallengerUnitId = session.Challenger.Guid,
                    OpponentUnitId   = session.Opponent.Guid
                });
            }

            return null;
        }

        public bool Decline(IPlayer player)
        {
            lock (syncRoot)
            {
                if (!TryGetSession(player, out DuelSession session))
                    return false;

                if (session.State != DuelState.Pending || session.Opponent.Guid != player.Guid)
                    return false;

                Finish(session, session.Challenger, session.Opponent, DuelFinishReason.DeclinedRequest);
                return true;
            }
        }

        public bool Forfeit(IPlayer player)
        {
            lock (syncRoot)
            {
                if (!TryGetSession(player, out DuelSession session))
                    return false;

                if (session.State != DuelState.Active)
                    return false;

                IPlayer winner = GetOpponent(session, player);
                if (winner == null)
                    return false;

                Finish(session, winner, player, DuelFinishReason.Forfeited2);
                return true;
            }
        }

        public bool AreDueling(IPlayer player, IPlayer target)
        {
            if (player == null || target == null)
                return false;

            lock (syncRoot)
            {
                return sessionsByPlayer.TryGetValue(player.Guid, out DuelSession session)
                    && session.State == DuelState.Active
                    && IsParticipant(session, target);
            }
        }

        public bool TryFinishDefeat(IPlayer loser, IPlayer winner)
        {
            if (loser == null || winner == null)
                return false;

            lock (syncRoot)
            {
                if (!sessionsByPlayer.TryGetValue(loser.Guid, out DuelSession session))
                    return false;

                if (session.State != DuelState.Active || !IsParticipant(session, winner))
                    return false;

                Finish(session, winner, loser, DuelFinishReason.Defeated);
                return true;
            }
        }

        public void Update(double lastTick)
        {
            lock (syncRoot)
            {
                foreach (DuelSession session in sessions.ToArray())
                {
                    if (!IsSessionStillValid(session))
                    {
                        Finish(session, session.Challenger, session.Opponent, DuelFinishReason.DuelCancelled);
                        continue;
                    }

                    switch (session.State)
                    {
                        case DuelState.Pending:
                            session.Timer -= lastTick;
                            if (session.Timer <= 0d)
                                Finish(session, session.Challenger, session.Opponent, DuelFinishReason.DuelCancelled);
                            break;
                        case DuelState.Countdown:
                            session.Timer -= lastTick;
                            if (session.Timer <= 0d)
                            {
                                session.State = DuelState.Active;
                                SendToParticipants(session, new ServerDuelStart
                                {
                                    ChallengerUnitId = session.Challenger.Guid,
                                    OpponentUnitId   = session.Opponent.Guid
                                });
                            }
                            break;
                    }
                }
            }
        }

        private static DuelFailureReason? GetPreflightFailure(IPlayer challenger, IPlayer opponent)
        {
            if (challenger == null || opponent == null || challenger.Guid == opponent.Guid)
                return DuelFailureReason.InvalidDuelTarget;

            if (!challenger.InWorld || !opponent.InWorld || challenger.Map != opponent.Map)
                return DuelFailureReason.PlayerInAnotherPhase;

            if (!challenger.IsAlive)
                return DuelFailureReason.YouCannotDuelWhileDead;

            if (!opponent.IsAlive)
                return DuelFailureReason.CannotDuelDeadPlayer;

            if (opponent.HasFlag(CharacterFlag.IgnoreDuelRequests))
                return DuelFailureReason.PlayerIsIgnoringDuels;

            if (challenger.InCombat)
                return DuelFailureReason.YouAreInCombat;

            if (opponent.InCombat)
                return DuelFailureReason.PlayerIsInCombat;

            return null;
        }

        private DuelFailureReason? GetSessionFailure(IPlayer challenger, IPlayer opponent)
        {
            if (sessionsByPlayer.TryGetValue(challenger.Guid, out DuelSession challengerSession))
                return challengerSession.State == DuelState.Active
                    ? DuelFailureReason.YouAreAlreadyDueling
                    : DuelFailureReason.YouHaveDuelRequestPending;

            if (sessionsByPlayer.TryGetValue(opponent.Guid, out DuelSession opponentSession))
                return opponentSession.State == DuelState.Active
                    ? DuelFailureReason.PlayerIsAlreadyDueling
                    : DuelFailureReason.PlayerHasDuelRequestPending;

            return null;
        }

        private static bool IsSessionStillValid(DuelSession session)
        {
            return session.Challenger.InWorld
                && session.Opponent.InWorld
                && session.Challenger.Map == session.Opponent.Map
                && session.Challenger.IsAlive
                && session.Opponent.IsAlive;
        }

        private bool TryGetSession(IPlayer player, out DuelSession session)
        {
            session = null;
            return player != null && sessionsByPlayer.TryGetValue(player.Guid, out session);
        }

        private static bool IsParticipant(DuelSession session, IPlayer player)
        {
            return session.Challenger.Guid == player.Guid || session.Opponent.Guid == player.Guid;
        }

        private static IPlayer GetOpponent(DuelSession session, IPlayer player)
        {
            if (session.Challenger.Guid == player.Guid)
                return session.Opponent;

            if (session.Opponent.Guid == player.Guid)
                return session.Challenger;

            return null;
        }

        private void Finish(DuelSession session, IPlayer winner, IPlayer loser, DuelFinishReason reason)
        {
            RemoveSession(session);

            UpdateDuelAchievements(winner, loser, reason);

            SendToParticipants(session, new ServerDuelResult
            {
                WinnerUnitId = winner?.Guid ?? 0u,
                LoserUnitId  = loser?.Guid ?? 0u,
                Reason       = reason
            });
        }

        private static void UpdateDuelAchievements(IPlayer winner, IPlayer loser, DuelFinishReason reason)
        {
            if (winner == null || loser == null)
                return;

            if (reason != DuelFinishReason.Defeated && reason != DuelFinishReason.Forfeited2)
                return;

            winner.AchievementManager.CheckAchievements(winner, AchievementType.DuelParticipate, 0u);
            loser.AchievementManager.CheckAchievements(loser, AchievementType.DuelParticipate, 0u);
            winner.AchievementManager.CheckAchievements(winner, AchievementType.DuelWin, 0u);
        }

        private void RemoveSession(DuelSession session)
        {
            sessions.Remove(session);
            sessionsByPlayer.Remove(session.Challenger.Guid);
            sessionsByPlayer.Remove(session.Opponent.Guid);
        }

        private static void SendToParticipants(DuelSession session, IWritable message)
        {
            session.Challenger.Session?.EnqueueMessageEncrypted(message);
            session.Opponent.Session?.EnqueueMessageEncrypted(message);
        }
    }
}
