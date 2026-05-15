using Microsoft.Extensions.Logging;

using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Pvp;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Pvp;

namespace NexusForever.WorldServer.Network.Message.Handler.Pvp
{
    public class ClientDuelInitateHandler : IMessageHandler<IWorldSession, ClientDuelInitate>
    {
        private readonly ILogger<ClientDuelInitateHandler> log;

        public ClientDuelInitateHandler(ILogger<ClientDuelInitateHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientDuelInitate duelInitiate)
        {
            IPlayer target = GetSelectedPlayer(session.Player);
            if (target == null)
            {
                log.LogDebug("Rejecting duel initiate from player {PlayerGuid}: no selected player target.", session.Player?.Guid);
                SendFailure(session, DuelFailureReason.InvalidDuelTarget);
                return;
            }

            DuelFailureReason? failureReason = GetDuelPreflightFailure(session.Player, target);
            log.LogDebug("Rejecting unsupported duel initiate from player {PlayerGuid} to player {TargetGuid}: reason {Reason}.",
                session.Player.Guid, target.Guid, failureReason ?? DuelFailureReason.CannotDuelRightNow);

            SendFailure(session, failureReason ?? DuelFailureReason.CannotDuelRightNow);
        }

        private static IPlayer GetSelectedPlayer(IPlayer player)
        {
            if (player?.TargetGuid == null)
                return null;

            IPlayer target = player.GetVisible<IPlayer>(player.TargetGuid.Value);
            return target?.Guid == player.Guid ? null : target;
        }

        private static DuelFailureReason? GetDuelPreflightFailure(IPlayer player, IPlayer target)
        {
            if (!player.IsAlive)
                return DuelFailureReason.YouCannotDuelWhileDead;

            if (!target.IsAlive)
                return DuelFailureReason.CannotDuelDeadPlayer;

            if (target.HasFlag(CharacterFlag.IgnoreDuelRequests))
                return DuelFailureReason.PlayerIsIgnoringDuels;

            if (player.InCombat)
                return DuelFailureReason.YouAreInCombat;

            if (target.InCombat)
                return DuelFailureReason.PlayerIsInCombat;

            return null;
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

        public ClientDuelAcceptHandler(ILogger<ClientDuelAcceptHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientDuelAccept duelAccept)
        {
            log.LogDebug("Ignoring unsupported duel accept request from player {PlayerGuid}.", session.Player?.Guid);
        }
    }

    public class ClientDuelDeclineHandler : IMessageHandler<IWorldSession, ClientDuelDecline>
    {
        private readonly ILogger<ClientDuelDeclineHandler> log;

        public ClientDuelDeclineHandler(ILogger<ClientDuelDeclineHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientDuelDecline duelDecline)
        {
            log.LogDebug("Ignoring unsupported duel decline request from player {PlayerGuid}.", session.Player?.Guid);
        }
    }

    public class ClientDuelForfeitHandler : IMessageHandler<IWorldSession, ClientDuelForfeit>
    {
        private readonly ILogger<ClientDuelForfeitHandler> log;

        public ClientDuelForfeitHandler(ILogger<ClientDuelForfeitHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientDuelForfeit duelForfeit)
        {
            log.LogDebug("Ignoring unsupported duel forfeit request from player {PlayerGuid}.", session.Player?.Guid);
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
            log.LogDebug("Ignoring unsupported PvP flag toggle from player {PlayerGuid}: value {Value}.",
                session.Player?.Guid, pvpToggleFlags.Value);
        }
    }
}
