using System.Threading.Tasks;
using NexusForever.Game;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Group;
using NexusForever.Network.Internal.Message.Group;
using NexusForever.Network.World.Message.Model;
using Rebus.Handlers;

namespace NexusForever.WorldServer.Network.Internal.Handler.Group
{
    public class GroupActionResultHandler : IHandleMessages<GroupActionResultMessage>
    {
        #region Dependency Injection

        private readonly IPlayerManager playerManager;

        public GroupActionResultHandler(
            IPlayerManager playerManager)
        {
            this.playerManager = playerManager;
        }

        #endregion

        public Task Handle(GroupActionResultMessage message)
        {
            IPlayer player = playerManager.GetPlayer(message.Recipient.ToGameIdentity());
            if (player == null)
                return Task.CompletedTask;

            switch (message.Result)
            {
                case GroupActionResult.KickSuccess:
                case GroupActionResult.KickFailed:
                    player.Session.EnqueueMessageEncrypted(new ServerGroupKickResult
                    {
                        GroupId = message.GroupId,
                        Result  = message.Result
                    });
                    return Task.CompletedTask;
                case GroupActionResult.ChangeSettingsSuccess:
                case GroupActionResult.ChangeSettingsFailed:
                    player.Session.EnqueueMessageEncrypted(new ServerGroupLootRuleValidationResult
                    {
                        GroupId = message.GroupId,
                        Result  = message.Result
                    });
                    return Task.CompletedTask;
            }

            player.Session.EnqueueMessageEncrypted(new ServerGroupActionResult
            {
                GroupId  = message.GroupId,
                Identity = message.Target.ToNetworkIdentity(),
                Result   = message.Result
            });

            return Task.CompletedTask;
        }
    }
}
