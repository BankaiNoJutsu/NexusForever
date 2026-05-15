using NexusForever.Database.Friendship;
using NexusForever.Game.Static.Friendship;
using NexusForever.Network.Internal;
using NexusForever.Network.Internal.Message.Friendship;
using NexusForever.Server.Friendship.Game.Character;
using NexusForever.Server.Friendship.Game.Friend;
using Rebus.Handlers;
using InternalIdentityName = NexusForever.Network.Internal.Message.Shared.IdentityName;

namespace NexusForever.Server.Friendship.Network.Internal.Handler.Friendship
{
    public class FriendshipRemoveNameHandler : IHandleMessages<FriendshipRemoveNameMessage>
    {
        #region Dependency Injection

        private readonly FriendshipContext _context;
        private readonly IInternalMessagePublisher _messagePublisher;

        private readonly CharacterManager _characterManager;
        private readonly FriendManager _friendManager;
        private readonly FriendshipResultPublisher _friendshipResultPublisher;

        public FriendshipRemoveNameHandler(
            FriendshipContext context,
            OutboxMessagePublisher messagePublisher,
            CharacterManager characterManager,
            FriendManager friendManager,
            FriendshipResultPublisher friendshipResultPublisher)
        {
            _context                   = context;
            _messagePublisher          = messagePublisher;
            _characterManager          = characterManager;
            _friendManager             = friendManager;
            _friendshipResultPublisher = friendshipResultPublisher;
        }

        #endregion

        public async Task Handle(FriendshipRemoveNameMessage message)
        {
            Task<FriendshipResult?> task = RemoveAsync(message);
            await _friendshipResultPublisher.PublishResultAsync(_messagePublisher, message.Inviter, task);

            await _context.SaveChangesAsync();
        }

        private async Task<FriendshipResult?> RemoveAsync(FriendshipRemoveNameMessage message)
        {
            if (!IsValidRemovableType(message.Type))
                return FriendshipResult.InvalidType;

            Character inviter = await _characterManager.GetCharacterAsync(message.Inviter.ToFriendshipIdentity());
            if (inviter == null)
                return FriendshipResult.PlayerNotFound;

            IdentityName inviteeIdentity = ResolveInviteeName(inviter, message.InviteeName);
            Character invitee = await _characterManager.GetCharacterRemoteAsync(inviteeIdentity);
            if (invitee == null)
                return FriendshipResult.PlayerNotFound;

            Friend friend = await inviter.GetFriendByIdentityAsync(invitee.Identity);
            if (friend == null)
                return GetNotFoundResult(message.Type);

            if (friend.Type == FriendshipType.FriendAndRival)
            {
                if (message.Type == FriendshipType.Friend)
                    await friend.UpdateType(FriendshipType.Rival);
                else if (message.Type == FriendshipType.Rival)
                    await friend.UpdateType(FriendshipType.Friend);
                else
                    return GetNotFoundResult(message.Type);
            }
            else
            {
                if (friend.Type != message.Type)
                    return GetNotFoundResult(message.Type);

                await inviter.RemoveFriendAsync(friend);
                invitee.RemoveFriendInverse(friend);

                _friendManager.RemoveFriend(friend);
            }

            return null;
        }

        private static bool IsValidRemovableType(FriendshipType type)
        {
            return type == FriendshipType.Friend ||
                type == FriendshipType.Ignore ||
                type == FriendshipType.Rival;
        }

        private static FriendshipResult GetNotFoundResult(FriendshipType type)
        {
            return type switch
            {
                FriendshipType.Ignore => FriendshipResult.PlayerNotIgnored,
                FriendshipType.Rival  => FriendshipResult.PlayerNotRival,
                _                     => FriendshipResult.PlayerNotFriend
            };
        }

        private static IdentityName ResolveInviteeName(Character inviter, InternalIdentityName inviteeName)
        {
            if (string.IsNullOrWhiteSpace(inviteeName.RealmName))
            {
                return new IdentityName
                {
                    Name      = inviteeName.Name,
                    RealmName = inviter.IdentityName.RealmName
                };
            }

            return inviteeName.ToFriendshipIdentity();
        }
    }
}
