using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Guild;
using NexusForever.Game.Abstract.ICComm;
using NexusForever.Game.Static.ICComm;
using NexusForever.Network.World.Message.Model.ICComm;
using NexusForever.Shared;

namespace NexusForever.Game.ICComm
{
    public sealed class ICCommManager : Singleton<ICCommManager>, IICCommManager
    {
        private sealed class ICCommChannel
        {
            public ulong Id { get; init; }
            public ICCommChannelType Type { get; init; }
            public ulong ScopeId { get; init; }
            public string KeyName { get; init; }
            public string DisplayName { get; init; }
            public Dictionary<uint, IPlayer> Members { get; } = [];
        }

        private readonly object syncRoot = new();
        private readonly Dictionary<ulong, ICCommChannel> channelsById = [];
        private readonly Dictionary<(ICCommChannelType Type, ulong ScopeId, string KeyName), ICCommChannel> channelsByKey = [];
        private readonly Dictionary<uint, HashSet<ulong>> channelsByPlayer = [];
        private ulong nextChannelId = 1ul;

        public ICCommJoinResult? Join(IPlayer player, ICCommChannelType type, ulong guildId, string name, out ulong iccommId, out string channelName)
        {
            iccommId = 0ul;
            channelName = string.Empty;

            if (player == null)
                return ICCommJoinResult.BadName;

            ICCommJoinResult? keyFailure = TryCreateChannelKey(player, type, guildId, name, out ulong scopeId, out string keyName, out channelName);
            if (keyFailure.HasValue)
                return keyFailure;

            lock (syncRoot)
            {
                var key = (type, scopeId, keyName);
                if (!channelsByKey.TryGetValue(key, out ICCommChannel channel))
                {
                    channel = new ICCommChannel
                    {
                        Id          = nextChannelId++,
                        Type        = type,
                        ScopeId     = scopeId,
                        KeyName     = keyName,
                        DisplayName = channelName
                    };

                    channelsByKey.Add(key, channel);
                    channelsById.Add(channel.Id, channel);
                }

                channel.Members[player.Guid] = player;

                if (!channelsByPlayer.TryGetValue(player.Guid, out HashSet<ulong> playerChannels))
                {
                    playerChannels = [];
                    channelsByPlayer.Add(player.Guid, playerChannels);
                }

                playerChannels.Add(channel.Id);
                iccommId = channel.Id;
                channelName = channel.DisplayName;
            }

            return null;
        }

        public ICCommMessageResult SendMessage(IPlayer player, ulong iccommId, uint messageId, string message, string recipientName)
        {
            if (player == null)
                return ICCommMessageResult.NotInChannel;

            if (string.IsNullOrWhiteSpace(message))
                return ICCommMessageResult.InvalidText;

            List<IPlayer> recipients;
            lock (syncRoot)
            {
                if (!channelsById.TryGetValue(iccommId, out ICCommChannel channel) ||
                    !channel.Members.ContainsKey(player.Guid))
                {
                    return ICCommMessageResult.NotInChannel;
                }

                if (!IsChannelMemberCurrent(channel, player))
                {
                    RemoveMembership(player.Guid, channel.Id);
                    return ICCommMessageResult.NotInChannel;
                }

                PruneStaleMembers(channel);

                recipients = GetRecipients(channel, player, recipientName);
                if (!string.IsNullOrWhiteSpace(recipientName) && recipients.Count == 0)
                    return ICCommMessageResult.NotInChannel;
            }

            player.Session?.EnqueueMessageEncrypted(new ServerICCommMessageResult
            {
                IccomId   = iccommId,
                MessageId = messageId,
                Result    = ICCommMessageResult.Sent
            });

            player.Session?.EnqueueMessageEncrypted(new ServerICCommOrderedMessage
            {
                IccomId   = iccommId,
                MessageId = messageId,
                Message   = message
            });

            foreach (IPlayer recipient in recipients)
            {
                recipient.Session?.EnqueueMessageEncrypted(new ServerICCommDirectedMessage
                {
                    IccomId    = iccommId,
                    Message    = message,
                    SenderName = player.Name
                });
            }

            return ICCommMessageResult.Sent;
        }

        public bool RemoveClientMembership(IPlayer player, ulong iccommId)
        {
            if (player == null)
                return false;

            lock (syncRoot)
            {
                return RemoveMembership(player.Guid, iccommId);
            }
        }

        public void Update(double lastTick)
        {
            lock (syncRoot)
            {
                foreach (ICCommChannel channel in channelsById.Values.ToArray())
                {
                    foreach (IPlayer member in channel.Members.Values.ToArray())
                        if (!IsChannelMemberCurrent(channel, member))
                            RemoveMembership(member.Guid, channel.Id);
                }
            }
        }

        private static ICCommJoinResult? TryCreateChannelKey(IPlayer player, ICCommChannelType type, ulong guildId, string name,
            out ulong scopeId, out string keyName, out string channelName)
        {
            scopeId = 0ul;
            keyName = string.Empty;
            channelName = string.Empty;

            switch (type)
            {
                case ICCommChannelType.Global:
                    channelName = NormaliseDisplayName(name);
                    if (channelName.Length == 0)
                        return ICCommJoinResult.BadName;

                    keyName = channelName.ToUpperInvariant();
                    return null;
                case ICCommChannelType.Group:
                    if (player.GroupAssociation == 0ul)
                        return ICCommJoinResult.NoGroup;

                    scopeId = player.GroupAssociation;
                    keyName = scopeId.ToString();
                    channelName = NormaliseDisplayName(name);
                    if (channelName.Length == 0)
                        channelName = $"Group {scopeId}";
                    return null;
                case ICCommChannelType.Guild:
                    IGuild guild = player.GuildManager.Guild;
                    if (guild == null || guildId != 0ul && guild.Id != guildId)
                        return ICCommJoinResult.NoGuild;

                    scopeId = guild.Id;
                    keyName = scopeId.ToString();
                    channelName = guild.Name;
                    return null;
                default:
                    return ICCommJoinResult.BadName;
            }
        }

        private static bool IsChannelMemberCurrent(ICCommChannel channel, IPlayer player)
        {
            if (player == null || !player.InWorld)
                return false;

            switch (channel.Type)
            {
                case ICCommChannelType.Global:
                    return true;
                case ICCommChannelType.Group:
                    return player.GroupAssociation == channel.ScopeId;
                case ICCommChannelType.Guild:
                    return player.GuildManager?.Guild?.Id == channel.ScopeId;
                default:
                    return false;
            }
        }

        private void PruneStaleMembers(ICCommChannel channel)
        {
            foreach (IPlayer member in channel.Members.Values.ToArray())
                if (!IsChannelMemberCurrent(channel, member))
                    RemoveMembership(member.Guid, channel.Id);
        }

        private static string NormaliseDisplayName(string name)
        {
            return (name ?? string.Empty).Trim();
        }

        private static List<IPlayer> GetRecipients(ICCommChannel channel, IPlayer sender, string recipientName)
        {
            IEnumerable<IPlayer> members = channel.Members.Values
                .Where(m => m.InWorld && m.Guid != sender.Guid);

            if (!string.IsNullOrWhiteSpace(recipientName))
            {
                string normalisedRecipient = recipientName.Trim();
                members = members.Where(m => string.Equals(m.Name, normalisedRecipient, StringComparison.OrdinalIgnoreCase));
            }

            return members.ToList();
        }

        private bool RemoveMembership(uint playerGuid, ulong channelId)
        {
            if (!channelsById.TryGetValue(channelId, out ICCommChannel channel))
                return false;

            if (!channel.Members.Remove(playerGuid))
                return false;

            if (channelsByPlayer.TryGetValue(playerGuid, out HashSet<ulong> playerChannels))
            {
                playerChannels.Remove(channelId);
                if (playerChannels.Count == 0)
                    channelsByPlayer.Remove(playerGuid);
            }

            if (channel.Members.Count == 0)
            {
                channelsById.Remove(channel.Id);
                channelsByKey.Remove((channel.Type, channel.ScopeId, channel.KeyName));
            }

            return true;
        }
    }
}
