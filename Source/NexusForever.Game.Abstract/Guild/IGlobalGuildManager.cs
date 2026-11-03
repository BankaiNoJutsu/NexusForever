using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Guild;
using NexusForever.Network.World.Message.Model.Guild;
using NexusForever.Shared;

namespace NexusForever.Game.Abstract.Guild
{
    public interface IGlobalGuildManager : IUpdate
    {
        /// <summary>
        /// Id to be assigned to the next created guild.
        /// </summary>
        ulong NextGuildId { get; }

        /// <summary>
        /// Initialise the <see cref="IGlobalGuildManager"/>, and build cache of all existing guilds.
        /// </summary>
        void Initialise();

        /// <summary>
        /// Shutdown <see cref="IGlobalGuildManager"/> and any related resources.
        /// </summary>
        /// <remarks>
        /// This will force save all guilds.
        /// </remarks>
        void Shutdown();

        /// <summary>
        /// Returns <see cref="IGuildBase"/> with supplied id.
        /// </summary>
        IGuildBase GetGuild(ulong guildId);

        /// <summary>
        /// Returns <see cref="IGuildBase"/> with supplied identity.
        /// </summary>
        IGuildBase GetGuild(Identity identity);

        /// <summary>
        /// Returns <see cref="IGuildBase"/> with supplied id.
        /// </summary>
        T GetGuild<T>(ulong guildId) where T : IGuildBase;

        /// <summary>
        /// Returns <see cref="IGuildBase"/> with supplied <see cref="GuildType"> and name.
        /// </summary>
        IGuildBase GetGuild(GuildType guildType, string name);

        /// <summary>
        /// Returns <see cref="IGuildBase"/> with supplied <see cref="GuildType"/> and name.
        /// </summary>
        T GetGuild<T>(GuildType guildType, string name) where T : IGuildBase;

        /// <summary>
        /// Returns a collection of <see cref="IGuildBase"/>'s in which supplied character id belongs to.
        /// </summary>
        /// <remarks>
        /// This should only be used in situations where the local <see cref="IGuildManager"/> is not accessible for a character.
        /// </remarks>
        IEnumerable<IGuildBase> GetCharacterGuilds(ulong characterId);

        /// <summary>
        /// Track a new guild for the supplied character.
        /// </summary>
        /// <remarks>
        /// Used to notify the global manager that a local manager is tracking a new guild.
        /// </remarks>
        void TrackCharacterGuild(ulong characterId, ulong guildId);

        /// <summary>
        /// Stop tracking an existing guild for the supplied character.
        /// </summary>
        /// <remarks>
        /// Used to notify the global manager that a local manager has stopped tracking an existing guild.
        /// </remarks>
        void UntrackCharacterGuild(ulong characterId, ulong guildId);

        /// <summary>
        /// Register and return a new <see cref="IGuildBase"/> with the supplied parameters.
        /// </summary>
        /// <remarks>
        /// The new guild does not have a leader, the first <see cref="IPlayer"/> to join will be assigned to the leader.
        /// </remarks>
        IGuildBase RegisterGuild(GuildType type, string name, string leaderRankName, string councilRankName, string memberRankName, IGuildStandard standard = null);

        /// <summary>Reserve a registered guild's proposed name before accepting a paid rename; null means unavailable.</summary>
        /// <remarks>Disposal releases an unused reservation. Assigning the guild name accepts it until persistence confirms the name.</remarks>
        IDisposable TryReserveGuildName(IGuildBase guild, string name);

        /// <summary>Track an accepted name assignment, retaining older database reservations until confirmation.</summary>
        bool AssignRegisteredGuildName(IGuildBase guild, string name);

        /// <summary>Release obsolete name reservations after the original guild's verified name save.</summary>
        void GuildNameSaved(IGuildBase guild, string savedName);

        /// <summary>
        /// Invoke operation delegate to handle <see cref="GuildOperation"/>.
        /// </summary>
        void HandleGuildOperation(IPlayer player, ClientGuildOperation operation);
    }
}
