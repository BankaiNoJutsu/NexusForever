using NexusForever.Game.Abstract.Guild;

namespace NexusForever.Game.Guild
{
    public sealed partial class GlobalGuildManager
    {
        private sealed class NameReservation(GlobalGuildManager owner, IGuildBase guild, string name, bool added) : IDisposable
        {
            public IGuildBase Guild { get; } = guild;
            public string Name { get; } = name;
            public bool Added { get; } = added;
            public bool Accepted { get; set; }
            public void Dispose() => owner.ReleaseName(this);
        }

        private readonly Dictionary<ulong, NameReservation> nameReservations = [];

        public IDisposable TryReserveGuildName(IGuildBase guild, string name)
        {
            ArgumentNullException.ThrowIfNull(guild);
            lock (cacheLock)
            {
                if (!guilds.TryGetValue(guild.Id, out IGuildBase current) || !ReferenceEquals(current, guild)
                    || guild is GuildBase { IsDisbandAccepted: true } || nameReservations.ContainsKey(guild.Id))
                    return null;
                var key = (guild.Type, name);
                bool exists = guildNameCache.TryGetValue(key, out ulong ownerId);
                if (exists && ownerId != guild.Id)
                    return null;
                var reservation = new NameReservation(this, guild, name, !exists);
                guildNameCache[key] = guild.Id;
                nameReservations.Add(guild.Id, reservation);
                return reservation;
            }
        }

        public bool AssignRegisteredGuildName(IGuildBase guild, string name)
        {
            lock (cacheLock)
            {
                if (!guilds.TryGetValue(guild.Id, out IGuildBase current))
                    return false; // Factory initialization precedes registration.
                if (!ReferenceEquals(current, guild))
                    throw new InvalidOperationException("Guild name assignment changed its registered actor.");
                if (nameReservations.TryGetValue(guild.Id, out NameReservation reservation)
                    && !StringComparer.Ordinal.Equals(reservation.Name, name))
                    throw new InvalidOperationException("Guild name assignment conflicts with its pending reservation.");
                if (guildNameCache.TryGetValue((guild.Type, name), out ulong ownerId) && ownerId != guild.Id)
                    throw new InvalidOperationException("The guild name is already reserved.");
                guildNameCache[(guild.Type, name)] = guild.Id;
                if (reservation != null)
                    reservation.Accepted = true;
                ((GuildBase)guild).AssignName(name);
                return true;
            }
        }

        public void GuildNameSaved(IGuildBase guild, string savedName)
        {
            lock (cacheLock)
            {
                if (!guilds.TryGetValue(guild.Id, out IGuildBase current) || !ReferenceEquals(current, guild))
                    return;
                nameReservations.TryGetValue(guild.Id, out NameReservation reservation);
                var comparer = new GuildNameEqualityComparer();
                foreach (var key in guildNameCache.Where(pair => pair.Value == guild.Id).Select(pair => pair.Key).ToArray())
                    if (!comparer.Equals(key, (guild.Type, savedName)) && !comparer.Equals(key, (guild.Type, guild.Name))
                        && (reservation == null || !comparer.Equals(key, (guild.Type, reservation.Name))))
                        guildNameCache.Remove(key);
            }
        }

        private void ReleaseName(NameReservation reservation)
        {
            lock (cacheLock)
            {
                if (!nameReservations.TryGetValue(reservation.Guild.Id, out NameReservation current) || !ReferenceEquals(current, reservation))
                    return;
                nameReservations.Remove(reservation.Guild.Id);
                var key = (reservation.Guild.Type, reservation.Name);
                if (!reservation.Accepted && reservation.Added && guildNameCache.TryGetValue(key, out ulong ownerId) && ownerId == reservation.Guild.Id)
                    guildNameCache.Remove(key);
            }
        }
    }
}
