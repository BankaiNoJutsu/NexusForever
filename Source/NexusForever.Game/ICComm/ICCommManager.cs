using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Guild;
using NexusForever.Game.Abstract.ICComm;
using NexusForever.Game.Static.ICComm;
using NexusForever.Network.World.Message.Model.ICComm;
using NexusForever.Network.Session;

namespace NexusForever.Game.ICComm
{
    public sealed class ICCommManager : IICCommManager
    {
        private sealed class ICCommChannel
        {
            public ulong Id { get; init; }
            public ICCommChannelType Type { get; init; }
            public ulong ScopeId { get; init; }
            public string KeyName { get; init; }
            public Dictionary<uint, Membership> Members { get; } = [];
        }

        // The numeric world GUID is an index, not authority to inherit another
        // actor's membership. A leave/rejoin also creates a new delivery owner.
        private sealed record Membership(uint Guid, IPlayer Player, IGameSession Session);

        private const int MaxMessageLength = 512;

        private readonly object syncRoot = new();
        private readonly Dictionary<ulong, ICCommChannel> channelsById = [];
        private readonly Dictionary<(ICCommChannelType Type, ulong ScopeId, string KeyName), ICCommChannel> channelsByKey = [];
        private readonly Dictionary<uint, HashSet<ulong>> channelsByPlayer = [];
        private ulong nextChannelId = 1ul;

        public ICCommJoinResult? Join(IPlayer player, ICCommChannelType type, ulong guildId, string name, out ulong iccommId, out string channelName)
        {
            iccommId = 0ul;
            // Replies select the client's pending (type, exact request name),
            // independently of the server's canonical channel key.
            channelName = name ?? string.Empty;

            if (player == null)
                return ICCommJoinResult.BadName;

            var admitted = new Membership(player.Guid, player, player.Session);

            ICCommJoinResult? keyFailure = TryCreateChannelKey(player, type, guildId, name, out ulong scopeId, out string keyName);
            if (keyFailure.HasValue)
                return keyFailure;

            lock (syncRoot)
            {
                if (!OwnsActor(admitted)) return ICCommJoinResult.BadName;

                var key = (type, scopeId, keyName);
                if (!channelsByKey.TryGetValue(key, out ICCommChannel channel))
                {
                    channel = new ICCommChannel
                    {
                        Id          = nextChannelId++,
                        Type        = type,
                        ScopeId     = scopeId,
                        KeyName     = keyName
                    };

                    channelsByKey.Add(key, channel);
                    channelsById.Add(channel.Id, channel);
                }

                if (!channel.Members.TryGetValue(admitted.Guid, out Membership previous)
                    || !ReferenceEquals(previous.Player, player) || !ReferenceEquals(previous.Session, admitted.Session))
                    channel.Members[admitted.Guid] = admitted;

                if (!channelsByPlayer.TryGetValue(admitted.Guid, out HashSet<ulong> playerChannels))
                {
                    playerChannels = [];
                    channelsByPlayer.Add(admitted.Guid, playerChannels);
                }

                playerChannels.Add(channel.Id);
                iccommId = channel.Id;
            }

            return null;
        }

        public ICCommMessageResult SendMessage(IPlayer player, ulong iccommId, uint messageId, string message, string recipientName)
        {
            if (player == null)
                return ICCommMessageResult.NotInChannel;

            if (string.IsNullOrWhiteSpace(message) || message.Length > MaxMessageLength)
                return ICCommMessageResult.InvalidText;

            ICCommChannel channel;
            Membership sender;
            List<Membership> recipients;
            string senderName;
            lock (syncRoot)
            {
                if (!channelsById.TryGetValue(iccommId, out channel)
                    || !channel.Members.TryGetValue(player.Guid, out sender)
                    || !ReferenceEquals(sender.Player, player))
                    return ICCommMessageResult.NotInChannel;

                if (!IsChannelMemberCurrent(channel, sender))
                {
                    RemoveMembership(channel, sender);
                    return ICCommMessageResult.NotInChannel;
                }

                PruneStaleMembers(channel);
                recipients = GetRecipients(channel, sender, recipientName);
                senderName = player.Name;
                if (!IsChannelMemberCurrent(channel, sender)
                    || !string.IsNullOrWhiteSpace(recipientName) && recipients.Count == 0)
                    return ICCommMessageResult.NotInChannel;
            }

            // Enqueues can call outward code. Recheck the accepted membership
            // before each publication and always use its original session.
            if (!CanPublish(channel, sender)) return ICCommMessageResult.NotInChannel;
            sender.Session.EnqueueMessageEncrypted(new ServerICCommMessageResult
            {
                IccomId   = iccommId,
                MessageId = messageId,
                Result    = ICCommMessageResult.Sent
            });

            // Once acknowledged, losing the owner cancels the remaining work;
            // it must not produce a contradictory second request result.
            if (!CanPublish(channel, sender)) return ICCommMessageResult.Sent;
            sender.Session.EnqueueMessageEncrypted(new ServerICCommOrderedMessage
            {
                IccomId   = iccommId,
                MessageId = messageId,
                Message   = message
            });

            foreach (Membership recipient in recipients)
            {
                if (!CanPublish(channel, sender)) break;
                if (!CanPublish(channel, sender, recipient)) continue;
                recipient.Session.EnqueueMessageEncrypted(new ServerICCommDirectedMessage
                {
                    IccomId    = iccommId,
                    Message    = message,
                    SenderName = senderName
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
                return channelsById.TryGetValue(iccommId, out ICCommChannel channel)
                    && channel.Members.TryGetValue(player.Guid, out Membership member)
                    && ReferenceEquals(member.Player, player) && ReferenceEquals(member.Session, player.Session)
                    && RemoveMembership(channel, member);
            }
        }

        public void Update(double lastTick)
        {
            lock (syncRoot)
            {
                foreach (ICCommChannel channel in channelsById.Values.ToArray())
                {
                    foreach (Membership member in channel.Members.Values.ToArray())
                        if (!IsChannelMemberCurrent(channel, member))
                            RemoveMembership(channel, member);
                }
            }
        }

        private static ICCommJoinResult? TryCreateChannelKey(IPlayer player, ICCommChannelType type, ulong guildId, string name,
            out ulong scopeId, out string keyName)
        {
            scopeId = 0ul;
            keyName = string.Empty;

            switch (type)
            {
                case ICCommChannelType.Global:
                    keyName = NormaliseDisplayName(name).ToUpperInvariant();
                    if (keyName.Length == 0)
                        return ICCommJoinResult.BadName;

                    return null;
                case ICCommChannelType.Group:
                    if (player.GroupAssociation == 0ul)
                        return ICCommJoinResult.NoGroup;

                    scopeId = player.GroupAssociation;
                    keyName = name ?? string.Empty;
                    return null;
                case ICCommChannelType.Guild:
                    IGuild guild = player.GuildManager?.Guild;
                    if (guild == null || guildId != 0ul && guild.Id != guildId)
                        return ICCommJoinResult.NoGuild;

                    scopeId = guild.Id;
                    keyName = name ?? string.Empty;
                    return null;
                default:
                    return ICCommJoinResult.BadName;
            }
        }

        private static bool OwnsActor(Membership member)
            => member.Player.Guid == member.Guid && member.Session != null
                && ReferenceEquals(member.Player.Session, member.Session)
                && (member.Session is not IPlayerSession bound || ReferenceEquals(bound.Player, member.Player))
                && member.Session is not NetworkSession { IsDisconnecting: true };

        private bool IsChannelMemberCurrent(ICCommChannel channel, Membership member)
        {
            if (!OwnsActor(member) || !member.Player.InWorld) return false;
            bool scope = channel.Type switch
            {
                ICCommChannelType.Global => true,
                ICCommChannelType.Group => member.Player.GroupAssociation == channel.ScopeId,
                ICCommChannelType.Guild => member.Player.GuildManager?.Guild?.Id == channel.ScopeId,
                _ => false
            };
            // Property access can reenter. Validate the exact retained handle
            // after reading actor/session/scope state, not only its numeric key.
            return scope && channelsById.TryGetValue(channel.Id, out var current) && ReferenceEquals(current, channel)
                && channel.Members.TryGetValue(member.Guid, out var registered) && ReferenceEquals(registered, member);
        }

        private bool CanPublish(ICCommChannel channel, Membership sender, Membership recipient = null)
        {
            lock (syncRoot)
            {
                if (!IsChannelMemberCurrent(channel, sender)) return false;
                if (recipient != null && !IsChannelMemberCurrent(channel, recipient))
                {
                    RemoveMembership(channel, recipient);
                    return false;
                }
                return IsChannelMemberCurrent(channel, sender);
            }
        }

        private void PruneStaleMembers(ICCommChannel channel)
        {
            foreach (Membership member in channel.Members.Values.ToArray())
                if (!IsChannelMemberCurrent(channel, member))
                    RemoveMembership(channel, member);
        }

        private static string NormaliseDisplayName(string name)
        {
            return (name ?? string.Empty).Trim();
        }

        private List<Membership> GetRecipients(ICCommChannel channel, Membership sender, string recipientName)
        {
            IEnumerable<Membership> members = channel.Members.Values.ToArray()
                .Where(member => !ReferenceEquals(member, sender) && IsChannelMemberCurrent(channel, member));

            if (!string.IsNullOrWhiteSpace(recipientName))
            {
                string normalisedRecipient = recipientName.Trim();
                members = members.Where(member => string.Equals(member.Player.Name, normalisedRecipient, StringComparison.OrdinalIgnoreCase));
            }

            return members.ToList();
        }

        private bool RemoveMembership(ICCommChannel channel, Membership member)
        {
            if (!channelsById.TryGetValue(channel.Id, out var current) || !ReferenceEquals(current, channel)
                || !channel.Members.TryGetValue(member.Guid, out var registered) || !ReferenceEquals(registered, member))
                return false;

            channel.Members.Remove(member.Guid);

            if (channelsByPlayer.TryGetValue(member.Guid, out HashSet<ulong> playerChannels))
            {
                playerChannels.Remove(channel.Id);
                if (playerChannels.Count == 0)
                    channelsByPlayer.Remove(member.Guid);
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
