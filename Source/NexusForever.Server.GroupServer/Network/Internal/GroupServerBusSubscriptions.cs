using NexusForever.Network.Internal.Message.Group;
using NexusForever.Network.Internal.Message.Match;
using NexusForever.Network.Internal.Message.Player;
using Rebus.Bus;

namespace NexusForever.Server.GroupServer.Network.Internal
{
    internal static class GroupServerBusSubscriptions
    {
        public static async Task SubscribeAll(IBus bus)
        {
            await bus.Subscribe<GroupDisbandMessage>();
            await bus.Subscribe<GroupFlagsUpdateMessage>();
            await bus.Subscribe<GroupInstanceDifficultyUpdateMessage>();
            await bus.Subscribe<GroupLootRulesUpdateMessage>();
            await bus.Subscribe<GroupMarkerMessage>();
            await bus.Subscribe<GroupMemberFlagUpdateMessage>();
            await bus.Subscribe<GroupMemberKickMessage>();
            await bus.Subscribe<GroupMemberLeaveMessage>();
            await bus.Subscribe<GroupMemberPromoteMessage>();
            await bus.Subscribe<GroupMemberRequestMessage>();
            await bus.Subscribe<GroupMemberRequestReponseMessage>();
            await bus.Subscribe<GroupPlayerInviteMessage>();
            await bus.Subscribe<GroupPlayerInviteRespondedMessage>();
            await bus.Subscribe<GroupReadyCheckMessage>();

            await bus.Subscribe<MatchCreatedMessage>();
            await bus.Subscribe<MatchMemberLeftMessage>();
            await bus.Subscribe<MatchRemovedMessage>();

            await bus.Subscribe<PlayerLoggedInMessage>();
            await bus.Subscribe<PlayerLoggedOutMessage>();
            await bus.Subscribe<PlayerAbsorptionUpdatedMessage>();
            await bus.Subscribe<PlayerPositionUpdatedMessage>();
            await bus.Subscribe<PlayerPropertyUpdatedMessage>();
            await bus.Subscribe<PlayerStatUpdatedMessage>();
            await bus.Subscribe<PlayerWorldUpdatedMessage>();
            await bus.Subscribe<PlayerWorldZoneUpdatedMessage>();
        }
    }
}
