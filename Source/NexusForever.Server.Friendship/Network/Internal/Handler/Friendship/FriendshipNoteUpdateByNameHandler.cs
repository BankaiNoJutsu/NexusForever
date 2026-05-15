using NexusForever.Database.Friendship;
using NexusForever.Game.Static.Friendship;
using NexusForever.GameTable.Text.Filter;
using NexusForever.GameTable.Text.Static;
using NexusForever.Network.Internal;
using NexusForever.Network.Internal.Message.Friendship;
using NexusForever.Server.Friendship.Game.Character;
using NexusForever.Server.Friendship.Game.Friend;
using Rebus.Handlers;
using InternalIdentityName = NexusForever.Network.Internal.Message.Shared.IdentityName;

namespace NexusForever.Server.Friendship.Network.Internal.Handler.Friendship
{
    public class FriendshipNoteUpdateByNameHandler : IHandleMessages<FriendshipNoteUpdateByNameMessage>
    {
        #region Dependency Injection

        private readonly FriendshipContext _context;
        private readonly IInternalMessagePublisher _messagePublisher;

        private readonly CharacterManager _characterManager;
        private readonly ITextFilterManager _textFilterManager;
        private readonly FriendshipResultPublisher _friendshipResultPublisher;

        public FriendshipNoteUpdateByNameHandler(
            FriendshipContext context,
            OutboxMessagePublisher messagePublisher,
            CharacterManager characterManager,
            ITextFilterManager textFilterManager,
            FriendshipResultPublisher friendshipResultPublisher)
        {
            _context                   = context;
            _messagePublisher          = messagePublisher;
            _characterManager          = characterManager;
            _textFilterManager         = textFilterManager;
            _friendshipResultPublisher = friendshipResultPublisher;
        }

        #endregion

        public async Task Handle(FriendshipNoteUpdateByNameMessage message)
        {
            Task<FriendshipResult?> task = UpdateNoteAsync(message);
            await _friendshipResultPublisher.PublishResultAsync(_messagePublisher, message.Source, task);

            await _context.SaveChangesAsync();
        }

        private async Task<FriendshipResult?> UpdateNoteAsync(FriendshipNoteUpdateByNameMessage message)
        {
            Character source = await _characterManager.GetCharacterAsync(message.Source.ToFriendshipIdentity());
            if (source == null)
                return FriendshipResult.PlayerNotFound;

            IdentityName targetIdentity = ResolveTargetName(source, message.TargetName);
            Character target = await _characterManager.GetCharacterRemoteAsync(targetIdentity);
            if (target == null)
                return FriendshipResult.PlayerNotFound;

            Friend friend = await source.GetFriendByIdentityAsync(target.Identity);
            if (friend == null)
                return FriendshipResult.PlayerNotFriend;

            if (message.Note != null)
            {
                if (!_textFilterManager.IsTextValid(message.Note, UserText.FriendshipNote))
                    return FriendshipResult.InvalidDisplayName;

                if (!_textFilterManager.IsTextValid(message.Note))
                    return FriendshipResult.ContainsProfanity;
            }

            await friend.UpdateNoteAsync(message.Note);

            return null;
        }

        private static IdentityName ResolveTargetName(Character source, InternalIdentityName targetName)
        {
            if (string.IsNullOrWhiteSpace(targetName.RealmName))
            {
                return new IdentityName
                {
                    Name      = targetName.Name,
                    RealmName = source.IdentityName.RealmName
                };
            }

            return targetName.ToFriendshipIdentity();
        }
    }
}
