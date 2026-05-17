using System.Threading.Tasks;
using NexusForever.Game;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Group;
using NexusForever.Network.Internal.Message.Group;
using NexusForever.Network.Internal.Message.Group.Shared;
using NexusForever.Network.World.Message.Model;
using Rebus.Handlers;

namespace NexusForever.WorldServer.Network.Internal.Handler.Group
{
    public class GroupLootRulesUpdatedHandler : IHandleMessages<GroupLootRulesUpdatedMessage>
    {
        #region Dependency Injection

        private readonly IPlayerManager playerManager;
        private readonly IGroupStateManager groupStateManager;

        public GroupLootRulesUpdatedHandler(
            IPlayerManager playerManager,
            IGroupStateManager groupStateManager)
        {
            this.playerManager      = playerManager;
            this.groupStateManager = groupStateManager;
        }

        #endregion

        public Task Handle(GroupLootRulesUpdatedMessage message)
        {
            groupStateManager.UpdateGroup(message.Group.ToGroupLootState());

            var groupLootRulesChanged = new ServerGroupLootRulesChange
            {
                GroupId                   = message.Group.Id,
                LootRulesUnderThreshold   = message.Group.NormalRule,
                LootRulesThresholdAndOver = message.Group.ThresholdRule,
                LootThreshold             = message.Group.ThresholdQuality,
                HarvestLootRule           = message.Group.HarvestRule
            };

            foreach (GroupMember groupMember in message.Group.Members)
            {
                IPlayer player = playerManager.GetPlayer(groupMember.Identity.ToGameIdentity());
                player?.Session.EnqueueMessageEncrypted(groupLootRulesChanged);
            }

            return Task.CompletedTask;
        }
    }
}
