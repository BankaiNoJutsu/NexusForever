using System.Linq;
using Microsoft.Extensions.Logging;
using NexusForever.Game;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Group;
using NexusForever.Game.Abstract.Map.Lock;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Group;
using NexusForever.Game.Static.Setting;
using NexusForever.Network;
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
        private readonly IInternalMessagePublisher messagePublisher;
        private readonly IGroupStateManager groupStateManager;
        private readonly IMapLockManager mapLockManager;

        public ClientGroupGotoGroupInstanceHandler(
            ILogger<ClientGroupGotoGroupInstanceHandler> log,
            IInternalMessagePublisher messagePublisher,
            IGroupStateManager groupStateManager,
            IMapLockManager mapLockManager)
        {
            this.log               = log;
            this.messagePublisher  = messagePublisher;
            this.groupStateManager = groupStateManager;
            this.mapLockManager    = mapLockManager;
        }

        public void HandleMessage(IWorldSession session, ClientGroupGotoGroupInstance groupGotoGroupInstance)
        {
            if (!groupStateManager.TryGetGroup(groupGotoGroupInstance.GroupId, out GroupLootState group))
            {
                SendResult(groupGotoGroupInstance.GroupId, session.Player.Identity, GroupActionResult.InvalidGroup);
                return;
            }

            if (!group.HasMember(session.Player.Identity))
            {
                SendResult(groupGotoGroupInstance.GroupId, session.Player.Identity, GroupActionResult.InvalidGroup);
                return;
            }

            // Find the group's map lock for the current instance the leader entered
            IMapLockCollection lockCollection = mapLockManager.TryGetGroupLockCollection(groupGotoGroupInstance.GroupId);
            IMapLock groupLock = lockCollection?.FirstOrDefault();

            if (groupLock == null)
            {
                log.LogDebug("Player {PlayerGuid} requested group instance transfer for group {GroupId}, but no group instance lock exists (leader has not entered an instance yet).",
                    session.Player.Guid, groupGotoGroupInstance.GroupId);

                messagePublisher.PublishAsync(new GroupActionResultMessage
                {
                    GroupId   = groupGotoGroupInstance.GroupId,
                    Recipient = session.Player.Identity.ToInternalIdentity(),
                    Target    = session.Player.Identity.ToInternalIdentity(),
                    Result    = GroupActionResult.ChangeSettingsFailed
                }).FireAndForgetAsync();
                return;
            }

            log.LogDebug("Teleporting player {PlayerGuid} to group {GroupId} instance {InstanceId} in world {WorldId}.",
                session.Player.Guid, groupGotoGroupInstance.GroupId, groupLock.InstanceId, groupLock.WorldId);

            // Teleport player into the group's shared instance
            // Entry position (0,0,0) will place the player at the map's default spawn point
            session.Player.TeleportTo(
                (ushort)groupLock.WorldId,
                0f, 0f, 0f,
                groupLock,
                TeleportReason.Relocate);

            // Send success to requester
            SendResult(groupGotoGroupInstance.GroupId, session.Player.Identity, GroupActionResult.ChangeSettingsSuccess);
        }

        private void SendResult(ulong groupId, Identity identity, GroupActionResult result)
        {
            messagePublisher.PublishAsync(new GroupActionResultMessage
            {
                GroupId   = groupId,
                Recipient = identity.ToInternalIdentity(),
                Target    = identity.ToInternalIdentity(),
                Result    = result
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

            messagePublisher.PublishAsync(new GroupInstanceDifficultyUpdateMessage
            {
                GroupId    = groupSetInstanceDifficulty.GroupId,
                Identity   = session.Player.Identity.ToInternalIdentity(),
                Difficulty = groupSetInstanceDifficulty.Difficulty
            }).FireAndForgetAsync();

            log.LogDebug("Player {PlayerGuid} requested group {GroupId} instance difficulty {Difficulty}.",
                session.Player.Guid, groupSetInstanceDifficulty.GroupId, groupSetInstanceDifficulty.Difficulty);
        }
    }
}
