using System.Data;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Guild;
using NexusForever.Game.Abstract.Housing;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Guild;
using NexusForever.Game.Static.Guild;
using NexusForever.Game.Static.Housing;
using NexusForever.Network;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Housing
{
    public partial class Residence
    {
        private Donation donation;
        internal bool DonationSavePending { get { lock (MutationGate) return donation is { Prepared: true }; } }
        internal bool OwnsDonation(object reservation) => reservation != null && ReferenceEquals(donation, reservation);
        internal void RequireDonationSave(object reservation)
        {
            if (donation is { Prepared: true } && !OwnsDonation(reservation))
                throw new InvalidOperationException("Residence belongs to an unfinished paired donation.");
        }

        // Only registered original owners can accept a coupled transfer. The
        // handler keeps its existing packet/flag validation before this boundary.
        public void Donate(IPlayer player, ICommunity community, IReadOnlyCollection<ulong> ids)
        {
            if (ids.Count == 0) return;
            if (globalResidenceManager is not GlobalResidenceManager manager || community is not Community original
                || community.Residence is not Residence recipient || ReferenceEquals(this, recipient)
                || !ReferenceEquals(globalResidenceManager, recipient.globalResidenceManager))
                throw new InvalidPacketValueException();
            var command = new Donation(this, recipient, player, original);
            command.Accept(ids);
            manager.QueueDonation(command);
        }

        internal sealed class Donation
        {
            private readonly Residence donor, recipient, first, second;
            private readonly IPlayer player;
            private readonly IGameSession session;
            private readonly IResidenceManager housing;
            private readonly IGuildManager membership;
            private readonly Community community;
            private Decor[] sources;
            private Decor[] copies;
            private ResidencePersistence.Decoration[] sourceRows, copyRows;
            private ResidencePersistence.Snapshot donorBefore, recipientBefore, donorTarget, recipientTarget;
            private bool witnessed, committed, applied;
            private IResidenceMapInstance donorMap, recipientMap;
            private int publication;
            internal bool Prepared { get; private set; }
            internal bool Finished { get; private set; }
            internal bool ReadyToPublish => readyToPublish;
            private volatile bool readyToPublish;
            internal ulong DonorId => donor.Id;
            internal ulong RecipientId => recipient.Id;
            internal readonly TaskCompletionSource Completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

            internal Donation(Residence donor, Residence recipient, IPlayer player, Community community)
            {
                this.donor = donor; this.recipient = recipient; this.player = player; this.community = community;
                session = player.Session; housing = player.ResidenceManager; membership = player.GuildManager;
                first = donor.Id < recipient.Id ? donor : recipient;
                second = ReferenceEquals(first, donor) ? recipient : donor;
            }

            internal void Accept(IReadOnlyCollection<ulong> ids)
            {
                lock (community.MutationGate) lock (first.MutationGate) lock (second.MutationGate)
                {
                    donor.RequirePersistenceEditable(); recipient.RequirePersistenceEditable();
                    if (donor.persistence == null || recipient.persistence == null || donor.donation != null || recipient.donation != null
                        || donor.paidName != null || recipient.paidName != null || donor.CurrentRemodel != null || recipient.CurrentRemodel != null
                        || donor.OwnerId != player.CharacterId || recipient.OwnerId != null || recipient.GuildOwnerId != community.Id
                        || community.PendingDelete || community.GetMember(player.CharacterId) is not { PendingDelete: false }
                        || !OriginalPlayer() || !ReferenceEquals(community.Residence, recipient) || ids.Distinct().Count() != ids.Count)
                        throw new InvalidPacketValueException();
                    sources = ids.Select(id => donor.decors.TryGetValue(id, out IDecor row) ? row as Decor : null).ToArray();
                    if (sources.Any(row => row == null || row.PendingDelete || row.Type != DecorType.Crate
                        || row.Entry == null || (row.Entry.Flags & 8u) != 0)) throw new InvalidPacketValueException();
                    sourceRows = sources.Select(row => row.CapturePersistence()).ToArray();
                    copyRows = sourceRows.Select(row => row with { Id = donor.globalResidenceManager.NextDecorId }).ToArray();
                    copies = copyRows.Select((row, index) => new Decor(recipient, Model(recipient.Id, row), sources[index].Entry)).ToArray();
                    donor.donation = recipient.donation = this;
                }
            }

            internal bool HoldsDecor(Residence residence, ulong id) => ReferenceEquals(residence, donor)
                ? sourceRows.Any(row => row.Id == id) : copyRows.Any(row => row.Id == id);

            private bool OriginalPlayer() => ReferenceEquals(player.Session, session)
                && session is IPlayerSession original && ReferenceEquals(original.Player, player)
                && ReferenceEquals(player.ResidenceManager, housing) && ReferenceEquals(housing.Residence, donor)
                && ReferenceEquals(player.GuildManager, membership)
                && ReferenceEquals(membership.GetGuild<ICommunity>(GuildType.Community), community)
                && ReferenceEquals(community.Residence, recipient);

            private bool Prepare()
            {
                lock (community.MutationGate) lock (first.MutationGate) lock (second.MutationGate)
                {
                    if (Prepared) return true;
                    // Existing creation/name units are prerequisites, not part of
                    // this command. They may save while selected decor is held.
                    if (donor.stagedSaveUnits.Count != 0 || recipient.stagedSaveUnits.Count != 0) return false;
                    if (community.PendingDelete || !ReferenceEquals(donor.globalResidenceManager.GetResidence(donor.Id), donor)
                        || !ReferenceEquals(recipient.globalResidenceManager.GetResidence(recipient.Id), recipient))
                    {
                        Finish(); return false; // Nothing was transferred or acknowledged.
                    }
                    if (donor.CommunitySavePending || recipient.CommunitySavePending) return false;
                    var beforeDonor = donor.CapturePersistence();
                    var beforeRecipient = recipient.CapturePersistence();
                    donorTarget = beforeDonor with { Decor = beforeDonor.Decor.Where(row => !HoldsDecor(donor, row.Id)).ToArray() };
                    recipientTarget = beforeRecipient with { Decor = beforeRecipient.Decor.Concat(copyRows).OrderBy(row => row.Id).ToArray() };
                    Prepared = true; return true;
                }
            }

            internal async Task Run(Func<CharacterContext> contexts)
            {
                if (!Prepare()) return;
                if (!committed)
                {
                    await using var context = contexts();
                    lock (first.MutationGate) lock (second.MutationGate)
                    {
                        donor.persistence.RequireOwner(context, this); recipient.persistence.RequireOwner(context, this);
                    }
                    bool entered = false;
                    Task saving = context.Database.CreateExecutionStrategy().Execute(() =>
                    {
                        if (entered) throw new InvalidOperationException("Donation enrollment cannot be replayed by a provider strategy.");
                        entered = true;
                        var transaction = context.Database.BeginTransaction(IsolationLevel.Serializable);
                        try
                        {
                            bool mysql = context.Database.ProviderName?.Contains("MySql", StringComparison.Ordinal) == true;
                            if (!mysql && !context.Database.IsSqlite()) throw new InvalidOperationException("Unsupported donation provider.");
                            // Serialize both original SQL roots in canonical order.
                            foreach (ulong id in new[] { first.Id, second.Id })
                                _ = (mysql ? context.Residence.FromSqlInterpolated($"SELECT * FROM residence WHERE id={id} FOR UPDATE")
                                    : context.Residence.Where(row => row.Id == id)).AsNoTracking().AsEnumerable().SingleOrDefault();
                            lock (first.MutationGate) lock (second.MutationGate)
                            {
                                var actualDonor = donor.persistence.Read(context);
                                var actualRecipient = recipient.persistence.Read(context);
                                if (!witnessed)
                                {
                                    donorBefore = actualDonor; recipientBefore = actualRecipient; witnessed = true;
                                }
                                bool before = ResidencePersistence.Equal(actualDonor, donorBefore) && ResidencePersistence.Equal(actualRecipient, recipientBefore);
                                bool after = ResidencePersistence.Equal(actualDonor, donorTarget) && ResidencePersistence.Equal(actualRecipient, recipientTarget);
                                if (!before && !after) throw new InvalidOperationException("Donation differs from its complete retained pair.");
                                donor.persistence.Save(context, this, donorTarget);
                                recipient.persistence.Save(context, this, recipientTarget);
                                context.OnSaveSettled(success => { if (success) committed = true; });
                            }
                            return Complete();
                        }
                        catch { transaction.Dispose(); throw; }

                        async Task Complete()
                        {
                            await using (transaction)
                            {
                                await context.SaveChangesAsync();
                                await transaction.CommitAsync();
                            }
                        }
                    });
                    try { await saving; }
                    catch when (committed) { /* The terminal callback already confirmed both graphs. */ }
                }
                readyToPublish = true;
            }

            internal void PublishOnWorldUpdate()
            {
                lock (community.MutationGate) lock (first.MutationGate) lock (second.MutationGate)
                {
                    if (!readyToPublish || !committed) throw new InvalidOperationException("Donation has no confirmed transaction.");
                    if (!applied)
                    {
                        foreach (Decor source in sources)
                        {
                            if (!donor.decors.TryGetValue(source.DecorId, out IDecor current) || !ReferenceEquals(current, source))
                                throw new InvalidOperationException("Donation lost its original source decor.");
                        }
                        foreach (Decor copy in copies)
                            if (recipient.decors.ContainsKey(copy.DecorId)) throw new InvalidOperationException("Donation destination identity is occupied.");
                        foreach (Decor source in sources) donor.decors.Remove(source.DecorId);
                        foreach (Decor copy in copies) recipient.decors.Add(copy.DecorId, copy);
                        donorMap = donor.Map; recipientMap = recipient.Map; applied = true;
                    }
                    Publish();
                    Finish();
                }
            }

            private void Publish()
            {
                // A stage advances only after enqueue returns. A throw retains
                // the immutable proposal; it never reruns committed SQL or an
                // acknowledged earlier stage. Transport exactly-once is separate.
                while (publication < sources.Length * 2 + 1)
                {
                    if (publication == sources.Length * 2)
                    {
                        if (OriginalPlayer() && !community.PendingDelete && community.GetMember(player.CharacterId) is { PendingDelete: false }
                            && ReferenceEquals(donor.globalResidenceManager.GetResidence(donor.Id), donor)
                            && ReferenceEquals(recipient.globalResidenceManager.GetResidence(recipient.Id), recipient))
                        {
                            var receipt = new ServerHousingCommunityDonateUpdate();
                            for (int index = 0; index < sourceRows.Length; index++)
                                receipt.Entries.Add(new() { Value0 = unchecked((uint)sourceRows[index].Id), Value1 = unchecked((uint)copyRows[index].Id) });
                            session.EnqueueMessageEncrypted(receipt);
                        }
                    }
                    else
                    {
                        int index = publication / 2;
                        bool add = publication % 2 == 0;
                        Residence root = add ? recipient : donor;
                        IResidenceMapInstance map = add ? recipientMap : donorMap;
                        if (map != null && !map.UnloadStatus.HasValue && ReferenceEquals(root.Map, map)
                            && ReferenceEquals(root.globalResidenceManager.GetResidence(root.Id), root))
                            map.EnqueueToAll(new ServerHousingResidenceDecor { DecorData = [add ? Packet(root, copyRows[index])
                                : new() { RealmId = root.RealmId, ResidenceId = root.Id, DecorId = sourceRows[index].Id, DecorInfoId = 0 }] });
                    }
                    publication++;
                }
            }

            private void Finish()
            {
                if (!ReferenceEquals(donor.donation, this) || !ReferenceEquals(recipient.donation, this))
                    throw new InvalidOperationException("Donation lost its exact original reservations.");
                donor.donation = recipient.donation = null; Finished = true; Completion.TrySetResult();
            }
            private static ResidenceDecor Model(ulong residence, ResidencePersistence.Decoration row) => new()
            {
                Id = residence, DecorId = row.Id, DecorInfoId = row.Info, DecorType = row.Type, DecorData = row.Data,
                HookBagIndex = row.Bag, HookIndex = row.Hook, PlotIndex = row.Plot, Scale = row.Scale,
                X = row.X, Y = row.Y, Z = row.Z, Qx = row.Qx, Qy = row.Qy, Qz = row.Qz, Qw = row.Qw,
                ActivePropUnitId = row.Active, DecorParentId = row.Parent, ColourShiftId = row.Colour
            };
            private static ServerHousingResidenceDecor.Decor Packet(Residence residence, ResidencePersistence.Decoration row) => new()
            {
                RealmId = residence.RealmId, ResidenceId = residence.Id, DecorId = row.Id, DecorInfoId = row.Info,
                DecorType = (DecorType)row.Type, DecorData = row.Data, HookBagIndex = row.Bag, HookIndex = row.Hook, PlotIndex = row.Plot,
                Scale = row.Scale, Position = new(row.X, row.Y, row.Z), Rotation = new(row.Qx, row.Qy, row.Qz, row.Qw),
                ActivePropUnitId = row.Active, ParentDecorId = row.Parent, ColourShift = row.Colour
            };
        }
    }
}
