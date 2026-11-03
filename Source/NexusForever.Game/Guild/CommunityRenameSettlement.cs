using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Housing;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Guild;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Static;
using NLog;

namespace NexusForever.Game.Guild
{
    internal sealed class CommunityRenameSettlement
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();
        private readonly GlobalGuildManager registry;
        private readonly Community community;
        private readonly Residence root;
        private readonly Player player;
        private readonly IGameSession session;
        private readonly Abstract.Guild.IGuildManager membership;
        private readonly CurrencyManager currencies;
        private readonly string name, before;
        private readonly CurrencyType currency;
        private readonly ulong cost;
        private readonly IDisposable reservation;
        private Residence.PaidNameLease residence;
        private Player.HousingNameActor actor;
        private Task attempt;
        private object enrolledGuild;
        private CharacterContext enrolledContext;
        private List<Action> publications;
        private int cursor;
        private bool reporting;
        internal bool Prepared { get; private set; }
        internal bool Finished { get; private set; }
        internal ulong GuildId => community.Id;

        internal CommunityRenameSettlement(GlobalGuildManager registry, Community community, Residence root, Player player,
            string name, CurrencyType currency, ulong cost, IDisposable reservation)
        {
            this.registry = registry; this.community = community; this.root = root; this.player = player;
            this.name = name; this.currency = currency; this.cost = cost; this.reservation = reservation;
            before = community.Name; session = player.Session; membership = player.GuildManager; currencies = player.CurrencyManager as CurrencyManager;
        }
        private bool CurrentPlayer => ReferenceEquals(player.Session, session) && session is IPlayerSession original
            && ReferenceEquals(original.Player, player) && ReferenceEquals(player.GuildManager, membership)
            && ReferenceEquals(player.CurrencyManager, currencies);
        private bool CurrentCommunity => ReferenceEquals(registry.GetGuild(community.Id), community)
            && ReferenceEquals(community.Residence, root) && !community.PendingDelete;
        private bool CurrentMembership => ReferenceEquals(membership.GetGuild<Abstract.Guild.ICommunity>(GuildType.Community), community);

        internal void Advance()
        {
            lock (community.MutationGate)
            {
                if (Finished || !community.OwnsPaidRename(this)) return;
                if (!Prepared)
                {
                    if (!CurrentPlayer || !CurrentCommunity || !CurrentMembership)
                    { Cancel(HousingResult.Failed); return; }
                    if (community.PendingCreate || community.NeedsSave || community.NameUnitActive || !player.HousingNameCanBegin) return;
                    if (!currencies.CanAfford(currency, cost)) { Cancel(HousingResult.InsufficientFunds); return; }
                    residence = root.TryReservePaidName(name);
                    if (residence == null) return;
                    try
                    {
                        Prepared = true; actor = player.BeginHousingName(this, currency, cost);
                        if (actor == null) { Prepared = false; residence.Finish(); residence = null; return; }
                    }
                    catch { Prepared = false; residence.Finish(); residence = null; Cancel(HousingResult.Failed); throw; }
                }
                if (attempt != null)
                {
                    if (!attempt.IsCompleted) return;
                    if (attempt.IsFaulted)
                    {
                        if (!reporting) { log.Error(attempt.Exception, "Paid community rename retains its original actor after an unresolved save error."); reporting = true; }
                        return;
                    }
                    attempt = null;
                }
                if (publications == null) actor.RefreshPublicationReadiness();
                if (!actor.ReadyToPublish)
                {
                    attempt = actor.Attempt(); registry.ObservePaidNameAttempt(this, attempt); return;
                }
                if (publications == null) PreparePublication();
                while (cursor < publications.Count) { publications[cursor](); cursor++; }
                actor.End(); residence.Finish(); reservation.Dispose(); community.ReleasePaidRename(this);
                registry.GuildNameSaved(community, name); Finished = true;
            }
        }
        internal HousingRenameSaveReconciliation CreateReceipt(bool primary)
            => new(Guid.NewGuid().ToString("N"), player.CharacterId, community.Id, root.Id, (byte)community.Type, name,
                context =>
                {
                    if (actor?.Owns() != true || !community.OwnsPaidRename(this) || !registry.OwnsPaidNameReservation(community, name, reservation))
                        throw new InvalidOperationException("Paid name lost its original actor or registry reservation.");
                    CharacterDatabaseIdentity.Require(context, actor.Database);
                    var guild = context.Guild.AsNoTracking().SingleOrDefault(row => row.Id == community.Id);
                    if (guild == null || guild.Type != (byte)community.Type || guild.LeaderId != player.CharacterId || guild.DeleteTime != null
                        || guild.Name != name && (!primary || guild.Name != before))
                        throw new InvalidOperationException("Paid name SQL differs from its original active community.");
                    if (!primary) residence.ValidateCommitted(context);
                },
                context =>
                {
                    if (primary && (!ReferenceEquals(enrolledContext, context) || enrolledGuild == null))
                        throw new InvalidOperationException("Paid name did not enroll its complete coupled graph.");
                },
                entry => ReferenceEquals(entry.Context, enrolledContext) && ReferenceEquals(entry.Entity, enrolledGuild) || residence.Authorizes(entry));
        internal void StageNames(CharacterContext context)
        {
            lock (community.MutationGate)
            {
                if (!Prepared || actor?.Owns() != true || !community.OwnsPaidRename(this)) throw new InvalidOperationException("Paid name lost its coupled enrollment.");
                community.SavePaidName(context, name);
                enrolledContext = context; enrolledGuild = context.ChangeTracker.Entries<GuildModel>().Single(row => row.Entity.Id == community.Id).Entity;
                residence.Stage(context);
            }
        }
        private void PreparePublication()
        {
            registry.ConfirmPaidName(community, name, reservation);
            community.ApplyPaidRename(this, name);
            residence.Apply();
            var prepared = new List<Action>();
            foreach (var packet in actor.BeginPublication())
                prepared.Add(() => { if (actor.CanPublish) session.EnqueueMessageEncrypted(packet()); });
            if (CurrentCommunity) prepared.AddRange(community.CapturePaidNameBroadcast(this, name));
            var map = residence.Map;
            prepared.Add(() =>
            {
                if (CurrentCommunity && residence.Registered && map != null && !map.UnloadStatus.HasValue && ReferenceEquals(residence.Map, map))
                    map.EnqueueToAll(residence.Packet());
            });
            prepared.Add(() =>
            {
                if (actor.CanPublish && CurrentPlayer && CurrentMembership && CurrentCommunity && residence.Registered)
                    session.EnqueueMessageEncrypted(new ServerHousingCommunityRenameResult { Result = HousingResult.Success });
            });
            publications = prepared;
        }
        private void Cancel(HousingResult result)
        {
            if (CurrentPlayer) session.EnqueueMessageEncrypted(new ServerHousingCommunityRenameResult { Result = result });
            reservation.Dispose(); community.ReleasePaidRename(this); Finished = true;
        }
    }
}
