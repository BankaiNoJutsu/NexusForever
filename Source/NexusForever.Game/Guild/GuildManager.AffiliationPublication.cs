using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Guild;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Guild;
using NexusForever.Network.Internal.Message.Player;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Guild;
using NexusForever.Shared.Game.Events;

namespace NexusForever.Game.Guild
{
    public partial class GuildManager
    {
        internal sealed record AffiliationPresentation(ulong? Id, string Name, GuildType Type);
        private sealed record Holomark(ushort ScanLines, ushort Background, ushort Foreground);
        private sealed record AffiliationNotice(long Version, AffiliationPresentation State, Holomark Visual, bool Notify);
        private sealed record AffiliationActor(Player Player, Identity Identity, IGameSession Session, EventQueue Events,
            IAccount Account, ulong Epoch, int Generation);
        private sealed class AffiliationPublication(AffiliationNotice[] notices)
        {
            internal readonly AffiliationNotice[] Notices = notices;
            internal List<Func<Task>> Steps;
            internal int Cursor;
            internal Task Pending;
        }
        private readonly List<AffiliationNotice> pendingAffiliations = [];
        private readonly Queue<AffiliationPublication> affiliationPublications = new();
        private long affiliationVersion;
        private AffiliationPresentation publishedAffiliation = new(null, null, GuildType.None);
        private Holomark publishedHolomark;
        private AffiliationActor affiliationActor;
        private bool affiliationScheduled, affiliationPublishing;

        internal AffiliationPresentation PublishedAffiliation
        {
            get
            {
                if (owner is not Player) return new(guildAffiliation?.Id, guildAffiliation?.Name, guildAffiliation?.Type ?? GuildType.None);
                if (affiliationPersistence == null) return publishedAffiliation;
                lock (affiliationPersistence.Gate) return publishedAffiliation;
            }
        }

        private static (AffiliationPresentation State, Holomark Visual) CaptureAffiliation(IGuildBase guild, bool requireVisual)
        {
            Holomark visual = null;
            if (guild?.Type == GuildType.Guild)
            {
                var standard = (guild as IGuild)?.Standard;
                if (standard != null) visual = new((ushort)standard.ScanLines.GuildStandardPartEntry.ItemDisplayIdStandard,
                    (ushort)standard.BackgroundIcon.GuildStandardPartEntry.ItemDisplayIdStandard,
                    (ushort)standard.ForegroundIcon.GuildStandardPartEntry.ItemDisplayIdStandard);
                else if (requireVisual) throw new InvalidOperationException("Guild affiliation has no holomark definition.");
            }
            return (new(guild?.Id, guild?.Name, guild?.Type ?? GuildType.None), visual);
        }

        private void InitialiseAffiliationPublication(CharacterModel model)
        {
            pendingAffiliations.Clear(); affiliationPublications.Clear(); affiliationVersion = 0;
            affiliationScheduled = false; affiliationPublishing = false;
            var loaded = CaptureAffiliation(model.GuildAffiliation.HasValue ? guilds.GetValueOrDefault(model.GuildAffiliation.Value) : null, false);
            publishedAffiliation = loaded.State; publishedHolomark = loaded.Visual;
            affiliationActor = owner is Player original && original.Identity != null
                ? new(original, new() { Id = original.Identity.Id, RealmId = original.Identity.RealmId }, original.Session,
                    original.Session?.Events, original.Account, original.Account?.WorldSessionEpoch ?? 0, affiliationGeneration) : null;
            if (guildAffiliation?.Id != model.GuildAffiliation) SetAffiliation(guildAffiliation, false);
        }

        private void SetAffiliation(IGuildBase guild, bool notify)
        {
            if (affiliationPersistence == null || owner is not Player) { guildAffiliation = guild; return; }
            lock (affiliationPersistence.Gate)
            {
                var captured = CaptureAffiliation(guild, notify);
                guildAffiliation = guild;
                pendingAffiliations.Add(new(++affiliationVersion, captured.State, captured.Visual, notify));
            }
        }

        private bool OwnsAffiliation(AffiliationActor actor)
            => actor != null && ReferenceEquals(affiliationActor, actor) && actor.Generation == affiliationGeneration
                && actor.Player.OwnsGuildAffiliationPublication(this, actor.Identity, actor.Session, actor.Events, actor.Account, actor.Epoch);

        private void AcknowledgeAffiliation(ulong? id, long version)
        {
            if (owner is not Player) return;
            var captured = pendingAffiliations.Where(row => row.Version <= version).ToArray();
            if (captured.Length == 0) return;
            if (captured[^1].State.Id != id) throw new InvalidOperationException("Guild affiliation lost its frozen publication value.");
            affiliationPublications.Enqueue(new(captured));
            pendingAffiliations.RemoveAll(row => row.Version <= version);
        }

        private void ScheduleAffiliationPublication()
        {
            if (affiliationPersistence == null) return;
            AffiliationActor actor;
            lock (affiliationPersistence.Gate)
            {
                if (affiliationScheduled || affiliationPublishing || affiliationPublications.Count == 0) return;
                actor = affiliationActor;
                if (actor?.Events == null) return;
                affiliationScheduled = true;
            }
            try
            {
                actor.Events.EnqueueThreadSafe(new PredicateEvent(() => !OwnsAffiliation(actor)
                    || !actor.Player.IsNormalSaveInProgress && AffiliationStepReady(), () => PublishAffiliation(actor)));
            }
            catch (Exception error)
            {
                lock (affiliationPersistence.Gate) affiliationScheduled = false;
                log.Error(error, "Committed affiliation scheduling will retry.");
            }
        }
        private bool AffiliationStepReady()
        {
            lock (affiliationPersistence.Gate)
                return !affiliationPublications.TryPeek(out var head) || head.Pending == null || head.Pending.IsCompleted;
        }

        private void PublishAffiliation(AffiliationActor actor)
        {
            lock (affiliationPersistence.Gate)
            {
                if (!ReferenceEquals(affiliationActor, actor)) return;
                affiliationScheduled = false;
                if (affiliationPublishing) return;
                affiliationPublishing = true;
            }
            try
            {
                while (true)
                {
                    if (!OwnsAffiliation(actor)) { DiscardAffiliation(actor); return; }
                    AffiliationPublication head;
                    lock (affiliationPersistence.Gate) if (!affiliationPublications.TryPeek(out head)) return;
                    head.Steps ??= CaptureAffiliationSteps(actor, head);
                    while (head.Cursor < head.Steps.Count)
                    {
                        if (!OwnsAffiliation(actor)) { DiscardAffiliation(actor); return; }
                        if (head.Pending != null)
                        {
                            if (!head.Pending.IsCompleted) return;
                            Task completed = head.Pending; head.Pending = null;
                            completed.GetAwaiter().GetResult();
                            head.Cursor++; continue;
                        }
                        Task task = head.Steps[head.Cursor]() ?? throw new InvalidOperationException("Affiliation publication returned no task.");
                        if (task.IsCompletedSuccessfully) { head.Cursor++; continue; }
                        head.Pending = task;
                        // Observe faults even if a replaced actor later discards
                        // this queue; observing cannot publish or advance it.
                        _ = task.ContinueWith(done => { _ = done.Exception; }, CancellationToken.None,
                            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
                        return;
                    }
                    lock (affiliationPersistence.Gate)
                    {
                        if (!affiliationPublications.TryPeek(out var current) || !ReferenceEquals(current, head))
                            throw new InvalidOperationException("Affiliation publication lost its queue head.");
                        affiliationPublications.Dequeue();
                    }
                }
            }
            catch (Exception error) { log.Error(error, "Committed affiliation publication will retry."); }
            finally
            {
                bool original;
                lock (affiliationPersistence.Gate)
                {
                    original = ReferenceEquals(affiliationActor, actor);
                    if (original) affiliationPublishing = false;
                }
                if (original) ScheduleAffiliationPublication();
            }
        }

        private void DiscardAffiliation(AffiliationActor actor)
        {
            lock (affiliationPersistence.Gate)
                if (ReferenceEquals(affiliationActor, actor)) affiliationPublications.Clear();
        }

        private List<Func<Task>> CaptureAffiliationSteps(AffiliationActor actor, AffiliationPublication publication)
        {
            var last = publication.Notices[^1];
            var steps = new List<Func<Task>> { () =>
            {
                lock (affiliationPersistence.Gate) { publishedAffiliation = last.State; publishedHolomark = last.Visual; }
                return Task.CompletedTask;
            } };
            uint ownerGuid = actor.Player.Guid; object map = actor.Player.Map;
            foreach (var notice in publication.Notices)
            {
                steps.Add(() => { ApplyAffiliationVisuals(notice.State, notice.Visual); return Task.CompletedTask; });
                if (!notice.Notify) continue;
                foreach (IPlayer recipient in actor.Player.CaptureGuildAffiliationRecipients())
                {
                    var identity = recipient.Identity == null ? null : new Identity { Id = recipient.Identity.Id, RealmId = recipient.Identity.RealmId };
                    var session = recipient.Session; var events = session?.Events; uint guid = recipient.Guid;
                    steps.Add(() =>
                    {
                        if (identity == null || recipient.Identity?.Id != identity.Id || recipient.Identity.RealmId != identity.RealmId
                            || !ReferenceEquals(recipient.Session, session) || !ReferenceEquals(session?.Events, events)
                            || session is not IPlayerSession current || !ReferenceEquals(current.Player, recipient)
                            || session is NetworkSession { IsDisconnecting: true }) return Task.CompletedTask;
                        if (actor.Player.Guid != ownerGuid || !ReferenceEquals(actor.Player.Map, map)) return Task.CompletedTask;
                        if (!ReferenceEquals(recipient, actor.Player) && (!ReferenceEquals(actor.Player.Map, map)
                            || !ReferenceEquals(actor.Player.GetVisible<IGridEntity>(guid), recipient))) return Task.CompletedTask;
                        session.EnqueueMessageEncrypted(new ServerUnitGuildNameplateChanged
                            { UnitId = ownerGuid, GuildName = notice.State.Name ?? "", GuildType = notice.State.Type });
                        return Task.CompletedTask;
                    });
                }
                steps.Add(() => messagePublisher.PublishAsync(new PlayerGuildAssociationUpdatedMessage
                {
                    Identity = new() { Id = actor.Identity.Id, RealmId = actor.Identity.RealmId }, GuildName = notice.State.Name
                }));
            }
            return steps;
        }

        private void ApplyAffiliationVisuals(AffiliationPresentation state, Holomark visual)
        {
            if (state.Type != GuildType.Guild) { RemoveHolomark(); return; }
            if (visual == null) throw new InvalidOperationException("Published guild affiliation has no holomark definition.");
            owner.AddVisual(ItemSlot.GuildStandardScanLines, visual.ScanLines);
            owner.AddVisual(ItemSlot.GuildStandardBackgroundIcon, visual.Background);
            owner.AddVisual(ItemSlot.GuildStandardForegroundIcon, visual.Foreground);
            owner.AddVisual(ItemSlot.GuildStandardChest, 5411);
            bool near = owner.HasFlag(CharacterFlag.HolomarkNear);
            if (!owner.HasFlag(CharacterFlag.HolomarkHideBack)) owner.AddVisual(ItemSlot.GuildStandardBack, (ushort)(near ? 7163 : 5580));
            else owner.RemoveVisual(ItemSlot.GuildStandardBack);
            if (!owner.HasFlag(CharacterFlag.HolomarkHideLeft)) owner.AddVisual(ItemSlot.GuildStandardShoulderL, (ushort)(near ? 7164 : 5581));
            else owner.RemoveVisual(ItemSlot.GuildStandardShoulderL);
            if (!owner.HasFlag(CharacterFlag.HolomarkHideRight)) owner.AddVisual(ItemSlot.GuildStandardShoulderR, (ushort)(near ? 7165 : 5582));
            else owner.RemoveVisual(ItemSlot.GuildStandardShoulderR);
        }
    }
}
