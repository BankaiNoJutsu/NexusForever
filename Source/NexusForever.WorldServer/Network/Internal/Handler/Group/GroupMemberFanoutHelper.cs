using System;
using System.Collections.Generic;
using NexusForever.Game;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Network.Internal.Message.Group.Shared;
using NexusForever.Network.Message;

namespace NexusForever.WorldServer.Network.Internal.Handler.Group
{
    internal static class GroupMemberFanoutHelper
    {
        public static void EnqueueToOnlineMembers(this IPlayerManager playerManager, IEnumerable<GroupMember> members, IWritable message)
        {
            playerManager.EnqueueToOnlineMembers(members, _ => true, message);
        }

        public static void EnqueueToOnlineMembers(this IPlayerManager playerManager, IEnumerable<GroupMember> members, Func<GroupMember, bool> includeMember, IWritable message)
        {
            foreach (IPlayer player in playerManager.GetOnlineGroupMembers(members, includeMember))
                player.Session.EnqueueMessageEncrypted(message);
        }

        public static IEnumerable<IPlayer> GetOnlineGroupMembers(this IPlayerManager playerManager, IEnumerable<GroupMember> members)
        {
            return playerManager.GetOnlineGroupMembers(members, _ => true);
        }

        public static IEnumerable<IPlayer> GetOnlineGroupMembers(this IPlayerManager playerManager, IEnumerable<GroupMember> members, Func<GroupMember, bool> includeMember)
        {
            foreach (GroupMember member in members)
            {
                if (!includeMember(member))
                    continue;

                IPlayer player = playerManager.GetPlayer(member.Identity.ToGameIdentity());
                if (player != null)
                    yield return player;
            }
        }
    }
}
