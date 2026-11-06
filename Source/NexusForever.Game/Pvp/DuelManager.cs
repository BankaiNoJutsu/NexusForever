using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Pvp;
using NexusForever.Game.Static.Achievement;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Pvp;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Pvp;

namespace NexusForever.Game.Pvp
{
    public sealed partial class DuelManager : IDuelManager
    {
        private const double ChallengeTimeoutSeconds = 30d;
        private const double CountdownSeconds = 3d;

        /// <summary>Max distance (metres) between duelists before the left-area warning fires.</summary>
        private const float DuelLeashRange = 120f;

        /// <summary>How long (seconds) both duelists can be out of leash range before the duel is cancelled.</summary>
        private const double DuelLeashTimeout = 15d;

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
            public bool LeftAreaWarningSent { get; set; }
            public double LeashTimer { get; set; }
            public DuelParticipant ChallengerOwner { get; init; }
            public DuelParticipant OpponentOwner { get; init; }
            public IPlayer PendingWinner { get; set; }
            public IPlayer PendingLoser { get; set; }
        }

        private readonly object syncRoot = new();
        private readonly List<DuelSession> sessions = [];
        private readonly Dictionary<uint, DuelSession> sessionsByPlayer = [];

        public DuelFailureReason? Initiate(IPlayer challenger, IPlayer opponent)
        {
            DuelFailureReason? failure = GetPreflightFailure(challenger, opponent);
            if (failure.HasValue)
                return failure;

            var challengerOwner = DuelParticipant.Capture(challenger);
            var opponentOwner = DuelParticipant.Capture(opponent);
            if (challengerOwner == null || opponentOwner == null)
                return DuelFailureReason.CannotDuelRightNow;

            lock (syncRoot)
            {
                if (challengerOwner.Ownership() != DuelOwnership.Current || opponentOwner.Ownership() != DuelOwnership.Current)
                    return DuelFailureReason.CannotDuelRightNow;
                failure = GetSessionFailure(challenger, opponent);
                if (failure.HasValue)
                    return failure;

                var session = new DuelSession
                {
                    Challenger = challenger,
                    Opponent   = opponent,
                    ChallengerOwner = challengerOwner,
                    OpponentOwner = opponentOwner,
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

                if (GetOwnership(session) != DuelOwnership.Current)
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

                if (GetOwnership(session) != DuelOwnership.Current)
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

                if (session.PendingWinner != null) return false;
                if (GetOwnership(session) != DuelOwnership.Current)
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

        public bool IsDueling(IPlayer player)
        {
            lock (syncRoot)
                return TryGetSession(player, out DuelSession session) && session.State == DuelState.Active
                    && GetOwnership(session) == DuelOwnership.Current;
        }

        public bool AreDueling(IPlayer player, IPlayer target)
        {
            if (player == null || target == null)
                return false;

            lock (syncRoot)
            {
                return !ReferenceEquals(player, target) && TryGetSession(player, out DuelSession session)
                    && session.State == DuelState.Active
                    && IsParticipant(session, target) && GetOwnership(session) == DuelOwnership.Current;
            }
        }

        public bool TryFinishDefeat(IPlayer loser, IPlayer winner)
        {
            if (loser == null || winner == null)
                return false;

            lock (syncRoot)
            {
                if (ReferenceEquals(loser, winner) || !TryGetSession(loser, out DuelSession session))
                    return false;

                if (session.State != DuelState.Active || !IsParticipant(session, winner))
                    return false;

                if (session.PendingWinner != null)
                    return ReferenceEquals(session.PendingWinner, winner) && ReferenceEquals(session.PendingLoser, loser);
                var ownership = GetOwnership(session);
                if (ownership == DuelOwnership.Stale) return false;
                if (ownership == DuelOwnership.Busy)
                {
                    // A lethal hit is not replayed by Player. Retain its original
                    // participants until map ownership can be checked without waiting.
                    session.PendingWinner = winner; session.PendingLoser = loser;
                    return true;
                }
                Finish(session, winner, loser, DuelFinishReason.Defeated);
                return true;
            }
        }

        public void OnPlayerDisconnect(IPlayer player)
        {
            if (player == null)
                return;

            lock (syncRoot)
            {
                if (!TryGetSession(player, out DuelSession session))
                    return;

                Finish(session, session.Challenger, session.Opponent, DuelFinishReason.DuelCancelled);
            }
        }

        public void Update(double lastTick)
        {
            lock (syncRoot)
            {
                foreach (DuelSession session in sessions.ToArray())
                {
                    if (!IsRegistered(session)) continue;
                    var ownership = GetOwnership(session);
                    if (ownership == DuelOwnership.Busy) continue;
                    if (ownership == DuelOwnership.Stale)
                    {
                        Finish(session, session.Challenger, session.Opponent, DuelFinishReason.DuelCancelled);
                        continue;
                    }
                    if (session.PendingWinner != null)
                    {
                        Finish(session, session.PendingWinner, session.PendingLoser, DuelFinishReason.Defeated);
                        continue;
                    }
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
                        case DuelState.Active:
                            UpdateActiveDuelLeash(session, lastTick);
                            break;
                    }
                }
            }
        }

        private void UpdateActiveDuelLeash(DuelSession session, double lastTick)
        {
            float distance = session.Challenger.Position.GetDistance(session.Opponent.Position);
            bool outOfRange = distance > DuelLeashRange;

            if (outOfRange)
            {
                if (!session.LeftAreaWarningSent)
                {
                    SendToParticipants(session, new ServerDuelLeftArea());
                    session.LeftAreaWarningSent = true;
                    session.LeashTimer = DuelLeashTimeout;
                }

                session.LeashTimer -= lastTick;
                if (session.LeashTimer <= 0d)
                {
                    Finish(session, session.Challenger, session.Opponent, DuelFinishReason.DuelCancelled);
                }
            }
            else if (session.LeftAreaWarningSent)
            {
                // Player re-entered the dueling area while the warning is still active.
                SendToParticipants(session, new ServerDuelCancelWarning());
                session.LeftAreaWarningSent = false;
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
            return player != null && sessionsByPlayer.TryGetValue(player.Guid, out session) && IsParticipant(session, player);
        }

        private static bool IsParticipant(DuelSession session, IPlayer player)
        {
            return ReferenceEquals(session.Challenger, player) || ReferenceEquals(session.Opponent, player);
        }

        private static IPlayer GetOpponent(DuelSession session, IPlayer player)
        {
            if (ReferenceEquals(session.Challenger, player))
                return session.Opponent;

            if (ReferenceEquals(session.Opponent, player))
                return session.Challenger;

            return null;
        }

        private void Finish(DuelSession session, IPlayer winner, IPlayer loser, DuelFinishReason reason)
        {
            if (!RemoveSession(session)) return;

            UpdateDuelAchievements(winner, loser, reason);

            bool hasWinner = reason != DuelFinishReason.DuelCancelled
                          && reason != DuelFinishReason.DeclinedRequest;

            SendToParticipants(session, new ServerDuelResult
            {
                WinnerUnitId = hasWinner ? winner?.Guid ?? 0u : 0u,
                LoserUnitId  = hasWinner ? loser?.Guid ?? 0u : 0u,
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

        private bool RemoveSession(DuelSession session)
        {
            if (!sessions.Remove(session)) return false;
            RemoveOwner(session.ChallengerOwner); RemoveOwner(session.OpponentOwner);
            return true;
            void RemoveOwner(DuelParticipant owner)
            {
                if (sessionsByPlayer.TryGetValue(owner.UnitId, out var current) && ReferenceEquals(current, session))
                    sessionsByPlayer.Remove(owner.UnitId);
            }
        }

        private void SendToParticipants(DuelSession session, IWritable message)
        {
            Send(session.ChallengerOwner); Send(session.OpponentOwner);
            void Send(DuelParticipant owner)
            {
                if (owner.Ownership() != DuelOwnership.Current) return;
                if (sessionsByPlayer.TryGetValue(owner.UnitId, out var current) && !ReferenceEquals(current, session)) return;
                owner.Session.EnqueueMessageEncrypted(message);
            }
        }
    }
}
