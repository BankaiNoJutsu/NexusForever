using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Entity;
using NexusForever.Game.Housing;
using NexusForever.Game.Static.Achievement;
using NexusForever.Game.Static.Entity;
using NexusForever.Network.Session;
using NexusForever.Shared.Game.Events;
using NLog;

namespace NexusForever.Game.Guild
{
    // Reuse the existing frozen Character fee actor; shared-row authorization
    // and the draft/name/member lifecycle remain specific to guild creation.
    internal sealed class GuildCreationSettlement : IHousingFeeOperation
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();
        private readonly GlobalGuildManager registry;
        private readonly Player player;
        private readonly IGameSession session;
        private readonly EventQueue events;
        private readonly Abstract.Account.IAccount account;
        private readonly ulong epoch, cost;
        private Player.HousingNameActor actor;
        private Action undoAffiliation;
        private Task attempt, publication;
        private bool releasedGuild, achievementDone, reported, advancing;
        private CharacterContext enrollment;
        private HashSet<object> enrolledRows;
        private List<Func<Task>> notices;
        private int cursor;
        private readonly List<Action> guildEffects = [];
        private int guildEffectCursor;
        private bool preparingGuildEffects;
        private int effectThread;
        internal Guild Guild { get; }
        internal GuildManager Membership { get; }
        internal string Name { get; }
        internal ulong CharacterId { get; }
        internal bool Finished { get; private set; }

        internal GuildCreationSettlement(GlobalGuildManager registry, Guild guild, Player player, GuildManager membership, ulong cost)
        {
            this.registry = registry; Guild = guild; this.player = player; Membership = membership; this.cost = cost;
            Name = guild.Name; CharacterId = player.CharacterId; session = player.Session; events = session.Events;
            account = player.Account; epoch = account.WorldSessionEpoch;
        }
        private bool Owns() => Membership.OwnsPaidCreation(this) && player.CharacterId == CharacterId
            && ReferenceEquals(player.Session, session) && ReferenceEquals(session.Events, events)
            && ReferenceEquals(player.Account, account) && account.WorldSessionEpoch == epoch;
        internal bool OwnsEnrollment(CharacterContext context) => actor?.Owns() == true && Owns() && ReferenceEquals(enrollment, context);
        internal bool Begin()
        {
            Guild.PreparePaidCreation(player, this);
            actor = player.BeginHousingName(this, CurrencyType.Credits, cost);
            if (actor == null) { CancelBeforeAttempt(); return false; }
            return true;
        }
        internal void CancelBeforeAttempt() { undoAffiliation?.Invoke(); undoAffiliation = null; }

        internal void RetainGuildEffect(Abstract.Entity.IPlayer source, Abstract.Achievement.IGuildAchievementManager manager, Action effect)
        {
            if (Guild.PendingCreate || !Owns() || !ReferenceEquals(source, player) || !ReferenceEquals(Guild.AchievementManager, manager))
                throw new InvalidOperationException("Paid creation guild effects require their committed original Character/guild owner.");
            guildEffects.Add(() =>
            {
                if (!Guild.PendingDelete && ReferenceEquals(Guild.AchievementManager, manager)) effect();
            });
        }
        internal bool AllowsAchievementMutation(Achievement.GuildAchievementManager manager)
            => preparingGuildEffects && effectThread == Environment.CurrentManagedThreadId && Owns() && !Guild.PendingCreate
                && ReferenceEquals(Guild.AchievementManager, manager);

        bool IHousingFeeOperation.HasPendingOrdinaryEffects => guildEffectCursor < guildEffects.Count;
        void IHousingFeeOperation.PrepareOrdinaryEffects()
        {
            if (!Owns() || Guild.PendingCreate || preparingGuildEffects)
                throw new InvalidOperationException("Paid creation effects lost their committed original actor.");
            preparingGuildEffects = true; effectThread = Environment.CurrentManagedThreadId;
            try
            {
                // Only the owned achievement manager may mutate here. General
                // guild mutation and independent Save remain fenced. Completion
                // feedback enters the upcoming ordinary Character snapshot.
                while (guildEffectCursor < guildEffects.Count) { guildEffects[guildEffectCursor](); guildEffectCursor++; }
            }
            finally { preparingGuildEffects = false; effectThread = 0; }
        }

        void IHousingFeeOperation.PrepareEffects() => undoAffiliation = Membership.PrepareCreationAffiliation(this, Guild);
        void IHousingFeeOperation.ApplyCommittedEffects()
        {
            if (!Owns()) throw new InvalidOperationException("Committed guild creation changed its original Character actor.");
            Membership.AdoptPaidCreation(this, Guild);
            undoAffiliation = null;
            if (!achievementDone)
            {
                player.AchievementManager.CheckAchievements(player, AchievementType.GuildOrCircleJoin, (uint)Guild.Type);
                achievementDone = true;
            }
        }
        CharacterSaveReconciliation IHousingFeeOperation.CreateReceipt(bool primary)
            => new GuildCreationSaveReconciliation(Guid.NewGuid().ToString("N"), CharacterId, Guild.Id, (byte)Guild.Type, Name,
                context =>
                {
                    if (actor?.Owns() != true || !Owns()) throw new InvalidOperationException("Paid creation lost its complete original actor.");
                    CharacterDatabaseIdentity.Require(context, actor.Database);
                    var row = context.Guild.AsNoTracking().SingleOrDefault(row => row.Id == Guild.Id);
                    if ((!primary && row == null) || row != null && (row.Type != (byte)Guild.Type || row.LeaderId != CharacterId || row.DeleteTime != null || row.Name != Name))
                        throw new InvalidOperationException("Paid creation SQL conflicts with its accepted guild.");
                }, context =>
                {
                    if (primary && (!ReferenceEquals(enrollment, context) || enrolledRows == null
                        || context.ChangeTracker.Entries<GuildModel>().Count(row => row.Entity.Id == Guild.Id) != 1
                        || context.ChangeTracker.Entries<GuildDataModel>().Count(row => row.Entity.Id == Guild.Id) != 1
                        || !context.ChangeTracker.Entries<GuildRankModel>().Where(row => row.Entity.Id == Guild.Id)
                            .Select(row => row.Entity.Index).Order().SequenceEqual(new byte[] { 0, 1, 9 })
                        || context.ChangeTracker.Entries<GuildMemberModel>().Count(row => row.Entity.Id == Guild.Id && row.Entity.CharacterId == CharacterId) != 1))
                        throw new InvalidOperationException("Paid creation did not enroll its complete original guild graph.");
                }, entry => primary && ReferenceEquals(entry.Context, enrollment) && enrolledRows?.Contains(entry.Entity) == true);
        void IHousingFeeOperation.Stage(CharacterContext context)
        {
            if (actor?.Owns() != true || !Owns()) throw new InvalidOperationException("Paid creation lost its original coupled enrollment.");
            enrollment = context;
            Guild.Save(context);
            enrolledRows = context.ChangeTracker.Entries().Where(entry => entry.Entity is GuildModel or GuildDataModel or GuildRankModel or GuildMemberModel)
                .Select(entry => entry.Entity).ToHashSet(ReferenceEqualityComparer.Instance);
        }
        internal void Advance()
        {
            if (advancing) return;
            advancing = true;
            try { AdvanceCore(); }
            finally { advancing = false; }
        }
        private void AdvanceCore()
        {
            if (Finished) return;
            if (attempt != null)
            {
                if (!attempt.IsCompleted) return;
                if (attempt.IsFaulted)
                {
                    if (!reported) { log.Error(attempt.Exception, "Paid guild creation retains its unresolved original save."); reported = true; }
                    return;
                }
                attempt = null;
            }
            if (notices == null) actor.RefreshPublicationReadiness();
            if (!actor.ReadyToPublish) { attempt = actor.Attempt(); return; }
            if (notices == null)
            {
                // Keep the draft/name fence through the ordinary followup as
                // well. A public rename/leave must not change the secondary
                // receipt's accepted parent while that save is still pending.
                if (!releasedGuild)
                {
                    registry.AdoptPaidCreation(this); Guild.ReleasePaidCreation(this); releasedGuild = true;
                }
                var prepared = new List<Func<Task>> { () => Guild.PublishMutationsAsync() };
                foreach (var packet in actor.BeginPublication().Concat(Guild.CreationPackets(player)))
                    prepared.Add(() =>
                    {
                        if (actor.CanPublish && Owns()) session.EnqueueMessageEncrypted(packet());
                        return Task.CompletedTask;
                    });
                notices = prepared;
            }
            while (cursor < notices.Count)
            {
                if (publication != null)
                {
                    if (!publication.IsCompleted) return;
                    Task completed = publication; publication = null;
                    completed.GetAwaiter().GetResult(); cursor++; continue;
                }
                publication = notices[cursor]();
            }
            actor.End(); Membership.ReleasePaidCreation(this); Finished = true;
        }
    }
}
