using System.Numerics;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Crafting;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Crafting;
using NexusForever.Network.World.Message.Model.Shared;
using NexusForever.Shared.Game.Events;
using NLog;
using Identity = NexusForever.Game.Abstract.Identity;

namespace NexusForever.Game.Entity
{
    public partial class Player
    {
        private TradeskillPublication tradeskillPublication;
        // Standalone interface-only sessions retain their existing immediate
        // compatibility path. Once bound, a stale world owner cannot fall back.
        private bool UsesCommittedTradeskillPublication => tradeskillPublication != null || Session is IPlayerSession;
        private TradeskillPublication GetTradeskillPublication()
        {
            var persistence = GetProgressionPersistence();
            lock (persistence.Gate) return tradeskillPublication ??= new(this, persistence, persistence.Confirmed);
        }
        private void SendLearnedSchematic(TradeskillType type, uint id, float x, float y)
        {
            if (UsesCommittedTradeskillPublication) { GetTradeskillPublication().RetainRecipe(type, id, x, y); return; }
            Session.EnqueueMessageEncrypted(new ServerSchematicAddLearned { TradeskillId = type, TradeskillSchematic2Id = id, DiscoveryCoordinates = new(x, y) });
        }
        private void SendTradeskillResetCooldown()
        {
            if (UsesCommittedTradeskillPublication) { GetTradeskillPublication().RetainCooldown(); return; }
            Session.EnqueueMessageEncrypted(new ServerTradeskillRelearnCooldown());
        }

        private sealed class TradeskillPublication
        {
            private static readonly ILogger log = LogManager.GetCurrentClassLogger();
            private abstract record Notice;
            private sealed record SkillNotice(ProgressionPersistence.Skill State) : Notice;
            private sealed record RecipeNotice(TradeskillType Type, uint Id, float X, float Y) : Notice;
            private sealed record CooldownNotice : Notice;
            private sealed record Pending(long Version, Notice Notice);
            private sealed class Publication(ProgressionPersistence.Snapshot state, Notice[] notices, bool initial = false)
            {
                internal readonly ProgressionPersistence.Snapshot State = state;
                internal readonly Notice[] Notices = notices;
                internal readonly bool Initial = initial;
                internal List<Action> Packets;
                internal int Cursor;
            }
            private readonly Player owner;
            private readonly ProgressionPersistence persistence;
            private readonly IGameSession session;
            private readonly Identity identity;
            private readonly List<Pending> pending = [];
            private readonly Queue<Publication> publications = new();
            private ProgressionPersistence.Snapshot published;
            private bool scheduled, publishing, initialSending;
            internal long Version { get; private set; }
            internal TradeskillPublication(Player owner, ProgressionPersistence persistence, ProgressionPersistence.Snapshot baseline)
            {
                this.owner = owner; this.persistence = persistence; published = baseline; session = owner.Session;
                if (owner.Identity != null) identity = new() { Id = owner.Identity.Id, RealmId = owner.Identity.RealmId };
            }
            private bool Owns => identity != null && owner.Identity?.Id == identity.Id && owner.Identity.RealmId == identity.RealmId
                && ReferenceEquals(owner.tradeskillPublication, this) && ReferenceEquals(owner.progressionPersistence, persistence)
                && ReferenceEquals(owner.Session, session)
                && (owner.playerManager == null || ReferenceEquals(owner.playerManager.GetPlayer(identity), owner))
                && session is IPlayerSession current && ReferenceEquals(current.Player, owner)
                && session is not NetworkSession { IsDisconnecting: true };
            private void Retain(Notice notice)
            {
                lock (persistence.Gate) pending.Add(new(++Version, notice));
                Schedule();
            }
            internal void RetainSkill(TradeskillInfo row)
            {
                uint[] tiers = row.TradeskillTalentTierIds;
                Retain(new SkillNotice(new(identity?.Id ?? 0, (uint)row.TradeskillId, row.TradeskillXp, row.IsActive, row.PropertyProficiencyFlags, row.TalentPoints,
                    tiers[0], tiers[1], tiers[2], tiers[3], tiers[4], tiers[5], tiers[6], tiers[7], tiers[8], tiers[9])));
            }
            internal void RetainRecipe(TradeskillType type, uint id, float x, float y) => Retain(new RecipeNotice(type, id, x, y));
            internal void RetainCooldown() => Retain(new CooldownNotice());
            internal void Acknowledge(ProgressionPersistence.Snapshot state, long version)
            {
                // Metadata only, serialized with the original SQL baseline.
                lock (persistence.Gate)
                {
                    publications.Enqueue(new(state, pending.Where(row => row.Version <= version).Select(row => row.Notice).ToArray()));
                    pending.RemoveAll(row => row.Version <= version);
                }
            }
            internal void SendInitial()
            {
                if (!Owns) return;
                ProgressionPersistence.Snapshot snapshot = null;
                lock (persistence.Gate)
                {
                    if (publishing || initialSending || publications.Count != 0) publications.Enqueue(new(persistence.Confirmed, [], true));
                    else { initialSending = true; snapshot = published; }
                }
                if (snapshot == null) { Schedule(); return; }
                try { foreach (Action packet in InitialPackets(snapshot)) { if (!Owns) return; packet(); } }
                finally { lock (persistence.Gate) initialSending = false; Schedule(); }
            }
            internal void Schedule()
            {
                lock (persistence.Gate)
                {
                    if (scheduled || publishing || publications.Count == 0) return;
                    scheduled = true;
                }
                try
                {
                    var events = session?.Events;
                    if (events == null) { lock (persistence.Gate) scheduled = false; return; }
                    events.EnqueueThreadSafe(new PredicateEvent(() =>
                    {
                        if (!Owns) return true;
                        lock (persistence.Gate) return !initialSending && !owner.IsNormalSaveInProgress;
                    }, Publish));
                }
                catch (Exception error) { lock (persistence.Gate) scheduled = false; log.Error(error, "Committed tradeskill scheduling will retry."); }
            }
            private void Publish()
            {
                lock (persistence.Gate) { scheduled = false; if (publishing) return; publishing = true; }
                try
                {
                    while (true)
                    {
                        if (!Owns) { lock (persistence.Gate) publications.Clear(); return; }
                        Publication publication;
                        lock (persistence.Gate) if (!publications.TryPeek(out publication)) return;
                        publication.Packets ??= CapturePackets(publication);
                        while (true)
                        {
                            if (!Owns) { lock (persistence.Gate) publications.Clear(); return; }
                            Action packet;
                            lock (persistence.Gate)
                            {
                                RequireHead(publication);
                                if (publication.Cursor == publication.Packets.Count) { publications.Dequeue(); break; }
                                packet = publication.Packets[publication.Cursor];
                            }
                            packet(); lock (persistence.Gate) { RequireHead(publication); publication.Cursor++; }
                        }
                    }
                }
                catch (Exception error) { log.Error(error, "Committed tradeskill publication will retry."); }
                finally { lock (persistence.Gate) publishing = false; Schedule(); }
            }
            private void RequireHead(Publication publication)
            {
                if (!publications.TryPeek(out var head) || !ReferenceEquals(head, publication)) throw new InvalidOperationException("Tradeskill publication lost its retained queue owner.");
            }
            private List<Action> CapturePackets(Publication publication)
            {
                var packets = new List<Action> { () => { lock (persistence.Gate) published = publication.State; } };
                if (publication.Initial) packets.AddRange(InitialPackets(publication.State));
                foreach (Notice notice in publication.Notices)
                    switch (notice)
                    {
                        case SkillNotice skill: packets.Add(() => session.EnqueueMessageEncrypted(new ServerProfessionUpdate { Tradeskill = Info(skill.State) })); break;
                        case RecipeNotice recipe: packets.Add(() => session.EnqueueMessageEncrypted(new ServerSchematicAddLearned
                            { TradeskillId = recipe.Type, TradeskillSchematic2Id = recipe.Id, DiscoveryCoordinates = new(recipe.X, recipe.Y) })); break;
                        case CooldownNotice: packets.Add(() => session.EnqueueMessageEncrypted(new ServerTradeskillRelearnCooldown())); break;
                        default: throw new InvalidOperationException("Unknown retained tradeskill notice.");
                    }
                return packets;
            }
            private static TradeskillInfo Info(ProgressionPersistence.Skill row) => new()
            {
                TradeskillId = (TradeskillType)row.Id, TradeskillXp = row.Xp, IsActive = row.Active, PropertyProficiencyFlags = row.Flags, TalentPoints = row.Points,
                TradeskillTalentTierIds = [row.Tier0, row.Tier1, row.Tier2, row.Tier3, row.Tier4, row.Tier5, row.Tier6, row.Tier7, row.Tier8, row.Tier9]
            };
            private List<Action> InitialPackets(ProgressionPersistence.Snapshot state)
            {
                var recipes = state.Recipes.Where(row => owner.schematics.ContainsKey(row.Id)).OrderBy(row => row.Id).ToArray();
                var flags = new List<uint>();
                for (int i = 0; i < recipes.Length; i++) { if (i % 32 == 0) flags.Add(0); if (recipes[i].Discovered) flags[^1] |= 1u << (i % 32); }
                return [() => session.EnqueueMessageEncrypted(new ServerProfessionsLoad
                {
                    Tradeskills = state.Skills.OrderBy(row => row.Id).Select(Info).ToList(), LearnedSchematics = recipes.Select(row => row.Id).ToList(),
                    DiscoveredSchematics = recipes.Where(row => row.Discovered).Select(row => new ServerProfessionsLoad.DiscoveredSchematic
                        { TradeskillSchematic2Id = row.Id, Coordinates = new Vector2(row.X, row.Y) }).ToList(), LearnedSchematicDiscoveredFlags = flags.ToList()
                }), () => session.EnqueueMessageEncrypted(new ServerProfessionModifiers())];
            }
        }
    }
}
