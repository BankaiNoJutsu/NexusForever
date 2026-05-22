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
        private readonly IGroupStateManager groupStateManager;
        private readonly IPlayerManager playerManager;

        public ClientGroupSetInstanceDifficultyHandler(
            ILogger<ClientGroupSetInstanceDifficultyHandler> log,
            IInternalMessagePublisher messagePublisher,
            IGroupStateManager groupStateManager,
            IPlayerManager playerManager)
        {
            this.log               = log;
            this.messagePublisher  = messagePublisher;
            this.groupStateManager = groupStateManager;
            this.playerManager     = playerManager;
        }

        public void HandleMessage(IWorldSession session, ClientGroupSetInstanceDifficulty groupSetInstanceDifficulty)
        {
            if (groupSetInstanceDifficulty.Difficulty >= WorldDifficulty.Count)
                throw new InvalidPacketValueException($"Invalid group instance difficulty received: {groupSetInstanceDifficulty.Difficulty}");

            if (!groupStateManager.TryGetGroup(groupSetInstanceDifficulty.GroupId, out GroupLootState group))
            {
                SendResult(groupSetInstanceDifficulty.GroupId, session.Player.Identity, GroupActionResult.InvalidGroup);
                return;
            }

            if (!group.HasMember(session.Player.Identity))
            {
                SendResult(groupSetInstanceDifficulty.GroupId, session.Player.Identity, GroupActionResult.InvalidGroup);
                return;
            }

            // Apply difficulty to the requesting player.
            // The ClientGroupSetInstanceDifficulty packet (0x0412) only carries difficulty;
            // prime level and rally are set via the solo ClientSetInstanceSettings (0x014E).
            session.Player.InstanceDifficulty = groupSetInstanceDifficulty.Difficulty;

            log.LogDebug("Player {PlayerGuid} set group {GroupId} instance difficulty to {Difficulty}.",
                session.Player.Guid, groupSetInstanceDifficulty.GroupId, groupSetInstanceDifficulty.Difficulty);

            BroadcastInstanceDifficulty(group, session.Player.Guid, groupSetInstanceDifficulty.Difficulty);

            SendResult(groupSetInstanceDifficulty.GroupId, session.Player.Identity, GroupActionResult.ChangeSettingsSuccess);
        }

        private void BroadcastInstanceDifficulty(GroupLootState group, uint setterGuid, WorldDifficulty difficulty)
        {
            var packet = new ServerGroupInstanceDifficultyResponse
            {
                GroupId        = group.GroupId,
                CharacterGuid  = setterGuid,
                Difficulty     = difficulty,
                Unknown0       = 0u
            };

            foreach (GroupLootMember member in group.Members)
            {
                IPlayer memberPlayer = playerManager.GetPlayer(member.Identity);
                memberPlayer?.Session.EnqueueMessageEncrypted(packet);
            }
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
}
