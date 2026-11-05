using NexusForever.Game.Entity;
using NexusForever.Game.Static.Guild;
using NexusForever.Network.World.Message.Model.Guild;

namespace NexusForever.Game.Guild
{
    public sealed partial class GlobalGuildManager
    {
        private readonly HashSet<GuildCreationSettlement> paidCreations = [];

        internal bool IsCreationNameReserved(GuildType type, string name)
        {
            lock (cacheLock) return guildNameCache.ContainsKey((type, name));
        }
        internal GuildResult QueuePaidCreation(Player player, GuildManager membership, ClientGuildRegister request,
            Abstract.Guild.IGuildStandard standard, ulong cost)
        {
            lock (cacheLock)
            {
                var key = (GuildType.Guild, request.GuildName);
                if (guildNameCache.ContainsKey(key)) return GuildResult.GuildNameUnavailable;
                var guild = guildFactory.CreateGuild(GuildType.Guild, request.GuildName, request.MasterTitle, request.CouncilTitle, request.MemberTitle, standard) as Guild
                    ?? throw new InvalidOperationException("Paid creation requires a concrete ordinary guild.");
                using (var context = GetCharacterDatabase().CreateContext()) guild.BindCreationDatabase(context);
                var command = new GuildCreationSettlement(this, guild, player, membership, cost);
                guildNameCache.Add(key, guild.Id);
                try
                {
                    membership.AcceptPaidCreation(command);
                    if (!command.Begin()) { CancelPaidCreation(command); return GuildResult.UnableToProcess; }
                    paidCreations.Add(command); return GuildResult.Success;
                }
                catch
                {
                    command.CancelBeforeAttempt(); CancelPaidCreation(command); throw;
                }
            }
        }
        internal void CancelPaidCreation(GuildCreationSettlement command)
        {
            lock (cacheLock)
            {
                var key = (GuildType.Guild, command.Name);
                if (guildNameCache.TryGetValue(key, out var owner) && owner == command.Guild.Id && !guilds.ContainsKey(owner))
                    guildNameCache.Remove(key);
                paidCreations.Remove(command); command.Membership.ReleasePaidCreation(command);
            }
        }
        internal void AdoptPaidCreation(GuildCreationSettlement command)
        {
            lock (cacheLock)
            {
                if (!paidCreations.Contains(command) || command.Guild.PendingCreate
                    || !guildNameCache.TryGetValue((GuildType.Guild, command.Name), out var id) || id != command.Guild.Id)
                    throw new InvalidOperationException("Paid creation lost its confirmed original guild/name reservation.");
                if (guilds.TryGetValue(id, out var current))
                {
                    if (!ReferenceEquals(current, command.Guild)) throw new InvalidOperationException("Paid creation changed its registered guild actor.");
                    return;
                }
                guilds.Add(id, command.Guild); TrackCharacterGuild(command.CharacterId, id);
            }
        }
        private void UpdatePaidCreations()
        {
            GuildCreationSettlement[] pending;
            lock (cacheLock) pending = paidCreations.ToArray();
            foreach (var command in pending)
            {
                try
                {
                    command.Advance();
                    if (command.Finished) lock (cacheLock) paidCreations.Remove(command);
                }
                catch (Exception error) { log.Error(error, "Paid guild creation retains its original actor for world-update retry."); }
            }
        }
    }
}
