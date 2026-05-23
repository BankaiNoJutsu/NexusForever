using Microsoft.Extensions.Logging;

using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Pvp;
using NexusForever.Game.Retail;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Pvp;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Pvp;

namespace NexusForever.WorldServer.Network.Message.Handler.Pvp
{
    public class ClientDuelInitiateHandler : IMessageHandler<IWorldSession, ClientDuelInitiate>
    {
        private readonly ILogger<ClientDuelInitiateHandler> log;
        private readonly IDuelManager duelManager;

        public ClientDuelInitiateHandler(
            ILogger<ClientDuelInitiateHandler> log,
            IDuelManager duelManager)
        {
            this.log         = log;
            this.duelManager = duelManager;
        }

        public void HandleMessage(IWorldSession session, ClientDuelInitiate duelInitiate)
        {
            IPlayer target = GetSelectedPlayer(session.Player);
            if (target == null)
            {
                log.LogDebug("Rejecting duel initiate from player {PlayerGuid}: no selected player target.", session.Player?.Guid);
                SendFailure(session, DuelFailureReason.InvalidDuelTarget);
                return;
            }

            DuelFailureReason? failureReason = duelManager.Initiate(session.Player, target);
            if (failureReason.HasValue)
            {
                log.LogDebug("Rejecting duel initiate from player {PlayerGuid} to player {TargetGuid}: reason {Reason}.",
                    session.Player.Guid, target.Guid, failureReason);

                SendFailure(session, failureReason.Value);
                return;
            }

            log.LogDebug("Started duel challenge from player {PlayerGuid} to player {TargetGuid}.",
                session.Player.Guid, target.Guid);
        }

        private static IPlayer GetSelectedPlayer(IPlayer player)
        {
            if (player?.TargetGuid == null)
                return null;

            IPlayer target = player.GetVisible<IPlayer>(player.TargetGuid.Value);
            return target?.Guid == player.Guid ? null : target;
        }

        private static void SendFailure(IWorldSession session, DuelFailureReason reason)
        {
            session.EnqueueMessageEncrypted(new ServerDuelFailure
            {
                Reason = reason
            });
        }
    }

    public class ClientDuelAcceptHandler : IMessageHandler<IWorldSession, ClientDuelAccept>
    {
        private readonly ILogger<ClientDuelAcceptHandler> log;
        private readonly IDuelManager duelManager;

        public ClientDuelAcceptHandler(
            ILogger<ClientDuelAcceptHandler> log,
            IDuelManager duelManager)
        {
            this.log         = log;
            this.duelManager = duelManager;
        }

        public void HandleMessage(IWorldSession session, ClientDuelAccept duelAccept)
        {
            DuelFailureReason? failureReason = duelManager.Accept(session.Player);
            if (failureReason.HasValue)
            {
                session.EnqueueMessageEncrypted(new ServerDuelFailure
                {
                    Reason = failureReason.Value
                });

                log.LogDebug("Rejecting duel accept request from player {PlayerGuid}: reason {Reason}.",
                    session.Player?.Guid, failureReason);
                return;
            }

            log.LogDebug("Accepted duel request for player {PlayerGuid}.", session.Player?.Guid);
        }
    }

    public class ClientDuelDeclineHandler : IMessageHandler<IWorldSession, ClientDuelDecline>
    {
        private readonly ILogger<ClientDuelDeclineHandler> log;
        private readonly IDuelManager duelManager;

        public ClientDuelDeclineHandler(
            ILogger<ClientDuelDeclineHandler> log,
            IDuelManager duelManager)
        {
            this.log         = log;
            this.duelManager = duelManager;
        }

        public void HandleMessage(IWorldSession session, ClientDuelDecline duelDecline)
        {
            if (!duelManager.Decline(session.Player))
            {
                log.LogDebug("Ignoring duel decline request without pending duel from player {PlayerGuid}.",
                    session.Player?.Guid);
                return;
            }

            log.LogDebug("Declined duel request for player {PlayerGuid}.", session.Player?.Guid);
        }
    }

    public class ClientDuelForfeitHandler : IMessageHandler<IWorldSession, ClientDuelForfeit>
    {
        private readonly ILogger<ClientDuelForfeitHandler> log;
        private readonly IDuelManager duelManager;

        public ClientDuelForfeitHandler(
            ILogger<ClientDuelForfeitHandler> log,
            IDuelManager duelManager)
        {
            this.log         = log;
            this.duelManager = duelManager;
        }

        public void HandleMessage(IWorldSession session, ClientDuelForfeit duelForfeit)
        {
            if (!duelManager.Forfeit(session.Player))
            {
                log.LogDebug("Ignoring duel forfeit request without active duel from player {PlayerGuid}.",
                    session.Player?.Guid);
                return;
            }

            log.LogDebug("Forfeited duel for player {PlayerGuid}.", session.Player?.Guid);
        }
    }

    public class ClientSetIgnoreDuelRequestsHandler : IMessageHandler<IWorldSession, ClientSetIgnoreDuelRequests>
    {
        private readonly ILogger<ClientSetIgnoreDuelRequestsHandler> log;

        public ClientSetIgnoreDuelRequestsHandler(ILogger<ClientSetIgnoreDuelRequestsHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientSetIgnoreDuelRequests setIgnoreDuelRequests)
        {
            if (session.Player == null)
                return;

            if (setIgnoreDuelRequests.Ignore)
                session.Player.SetFlag(CharacterFlag.IgnoreDuelRequests);
            else
                session.Player.RemoveFlag(CharacterFlag.IgnoreDuelRequests);

            log.LogDebug("Updated ignore-duel toggle for player {PlayerGuid}: ignore {Ignore}.",
                session.Player.Guid, setIgnoreDuelRequests.Ignore);
        }
    }

    public class ClientPvpToggleFlagsHandler : IMessageHandler<IWorldSession, ClientPvpToggleFlags>
    {
        private readonly ILogger<ClientPvpToggleFlagsHandler> log;

        public ClientPvpToggleFlagsHandler(ILogger<ClientPvpToggleFlagsHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientPvpToggleFlags pvpToggleFlags)
        {
            if (session.Player == null)
                return;

            PvPFlag pvpFlag = session.Player.PvPFlag & PvPFlag.Forced;
            if (pvpToggleFlags.Value)
                pvpFlag |= PvPFlag.Enabled;

            session.Player.SetPvPFlag(pvpFlag);
            if (!pvpToggleFlags.Value)
                session.EnqueueMessageEncrypted(new ServerPvpCooldownUpdate
                {
                    CooldownRemaining = RetailCertainRules.PvpFlagCooldownMs
                });

            log.LogDebug("Updated PvP flag toggle for player {PlayerGuid}: value {Value}.",
                session.Player?.Guid, pvpToggleFlags.Value);
        }
    }
}
