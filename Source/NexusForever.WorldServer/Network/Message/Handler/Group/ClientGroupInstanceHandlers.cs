using Microsoft.Extensions.Logging;
using NexusForever.Game;
using NexusForever.Game.Static.Group;
using NexusForever.Network.Internal;
using NexusForever.Network.Internal.Message.Group;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Shared;

namespace NexusForever.WorldServer.Network.Message.Handler.Group
{
    public class ClientGroupGotoGroupInstanceHandler : IMessageHandler<IWorldSession, ClientGroupGotoGroupInstance>
    {
        private readonly ILogger<ClientGroupGotoGroupInstanceHandler> log;

        public ClientGroupGotoGroupInstanceHandler(ILogger<ClientGroupGotoGroupInstanceHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientGroupGotoGroupInstance groupGotoGroupInstance)
        {
            log.LogDebug("Ignoring unsupported group instance transfer request from player {PlayerGuid}: group {GroupId}.",
                session.Player.Guid, groupGotoGroupInstance.GroupId);
        }
    }

    public class ClientGroupSetInstanceDifficultyHandler : IMessageHandler<IWorldSession, ClientGroupSetInstanceDifficulty>
    {
        private readonly ILogger<ClientGroupSetInstanceDifficultyHandler> log;
        private readonly IInternalMessagePublisher messagePublisher;

        public ClientGroupSetInstanceDifficultyHandler(
            ILogger<ClientGroupSetInstanceDifficultyHandler> log,
            IInternalMessagePublisher messagePublisher)
        {
            this.log              = log;
            this.messagePublisher = messagePublisher;
        }

        public void HandleMessage(IWorldSession session, ClientGroupSetInstanceDifficulty groupSetInstanceDifficulty)
        {
            log.LogDebug("Rejecting unsupported group instance difficulty request from player {PlayerGuid}: group {GroupId}, difficulty {Difficulty}.",
                session.Player.Guid, groupSetInstanceDifficulty.GroupId, groupSetInstanceDifficulty.Difficulty);

            messagePublisher.PublishAsync(new GroupActionResultMessage
            {
                GroupId   = groupSetInstanceDifficulty.GroupId,
                Recipient = session.Player.Identity.ToInternalIdentity(),
                Target    = session.Player.Identity.ToInternalIdentity(),
                Result    = GroupActionResult.ChangeSettingsFailed
            }).FireAndForgetAsync();
        }
    }
}
