using Microsoft.Extensions.Logging;
using NexusForever.Game;
using NexusForever.Game.Static.Group;
using NexusForever.Game.Static.Setting;
using NexusForever.Network.Internal;
using NexusForever.Network.Internal.Message.Group;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Shared;

namespace NexusForever.WorldServer.Network.Message.Handler.Group
{
    public class ClientGroupGotoGroupInstanceHandler : IMessageHandler<IWorldSession, ClientGroupGotoGroupInstance>
    {
        private readonly ILogger<ClientGroupGotoGroupInstanceHandler> log;
        private readonly IInternalMessagePublisher messagePublisher;

        public ClientGroupGotoGroupInstanceHandler(
            ILogger<ClientGroupGotoGroupInstanceHandler> log,
            IInternalMessagePublisher messagePublisher)
        {
            this.log              = log;
            this.messagePublisher = messagePublisher;
        }

        public void HandleMessage(IWorldSession session, ClientGroupGotoGroupInstance groupGotoGroupInstance)
        {
            log.LogDebug("Rejecting group instance transfer request from player {PlayerGuid}: no group instance destination is available for group {GroupId}.",
                session.Player.Guid, groupGotoGroupInstance.GroupId);

            messagePublisher.PublishAsync(new GroupActionResultMessage
            {
                GroupId   = groupGotoGroupInstance.GroupId,
                Recipient = session.Player.Identity.ToInternalIdentity(),
                Target    = session.Player.Identity.ToInternalIdentity(),
                Result    = session.Player.GroupAssociation == groupGotoGroupInstance.GroupId
                    ? GroupActionResult.ChangeSettingsFailed
                    : GroupActionResult.InvalidGroup
            }).FireAndForgetAsync();
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
            if (groupSetInstanceDifficulty.Difficulty >= WorldDifficulty.Count)
                throw new InvalidPacketValueException($"Invalid group instance difficulty received: {groupSetInstanceDifficulty.Difficulty}");

            log.LogDebug("Rejecting group instance difficulty request from player {PlayerGuid}: group settings service has no difficulty state for group {GroupId}, difficulty {Difficulty}.",
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
