using NexusForever.Game.Abstract.Guild;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Guild;
using NexusForever.Network.World.Message.Model.Guild;

namespace NexusForever.Game.Guild
{
    public partial class GuildManager
    {
        private GuildCreationSettlement paidCreation;

        public GuildResult RegisterPaidGuild(ClientGuildRegister request, ulong cost)
        {
            if (request.GuildType is not (GuildType.Guild or GuildType.Community) || paidCreation != null || owner is not Player player
                || globalGuildManager is not GlobalGuildManager registry || !player.HousingNameCanBegin)
                return GuildResult.UnableToProcess;
            var result = CanRegisterGuild(request);
            if (result.Result != GuildResult.Success) return result.Result;
            if (request.GuildType == GuildType.Community && request.AlternateCost)
            {
                if (cost != 0 && (player.Account.WorldSessionEpoch == 0 || player.Account.CurrencyManager is not Account.Currency.AccountCurrencyManager))
                    return GuildResult.UnableToProcess;
                if (!player.Account.CurrencyManager.CanAfford(Static.Account.AccountCurrencyType.ServiceToken, cost)) return GuildResult.NotEnoughCredits;
            }
            else if (!player.CurrencyManager.CanAfford(Static.Entity.CurrencyType.Credits, cost)) return GuildResult.NotEnoughCredits;
            return registry.QueuePaidCreation(player, this, request,
                request.GuildType == GuildType.Guild ? new GuildStandard(request.GuildStandard, gameTableManager) : null, cost);
        }

        internal void AcceptPaidCreation(GuildCreationSettlement command)
        {
            if (paidCreation != null) throw new InvalidOperationException("The Character already owns a guild creation.");
            paidCreation = command;
        }
        internal bool OwnsPaidCreation(GuildCreationSettlement command) => ReferenceEquals(paidCreation, command)
            && owner is Player player && ReferenceEquals(player.GuildManager, this);
        internal Action PrepareCreationAffiliation(GuildCreationSettlement command, IGuildBase guild)
        {
            if (!OwnsPaidCreation(command)) throw new InvalidOperationException("Guild creation changed its original membership manager.");
            if (guildAffiliation != null) return () => { };
            SetAffiliation(guild, true);
            long version = affiliationVersion;
            return () =>
            {
                lock (affiliationPersistence.Gate)
                {
                    if (ReferenceEquals(guildAffiliation, guild)) guildAffiliation = null;
                    pendingAffiliations.RemoveAll(row => row.Version == version);
                }
            };
        }
        internal void AdoptPaidCreation(GuildCreationSettlement command, IGuildBase guild)
        {
            if (!OwnsPaidCreation(command)) throw new InvalidOperationException("Guild creation changed its original membership manager.");
            if (guilds.TryGetValue(guild.Id, out var current) && !ReferenceEquals(current, guild))
                throw new InvalidOperationException("Guild creation lost its local membership identity.");
            guilds[guild.Id] = guild;
            if (guild is IGuild ordinary) Guild = ordinary;
        }
        internal void ReleasePaidCreation(GuildCreationSettlement command)
        {
            if (ReferenceEquals(paidCreation, command)) paidCreation = null;
        }
    }
}
