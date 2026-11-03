using Microsoft.EntityFrameworkCore.ChangeTracking;
using NexusForever.Database.Character;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Static.Housing;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Housing
{
    public partial class Residence
    {
        private PaidNameLease paidName;
        internal bool PaidNameSavePending { get { lock (MutationGate) return paidName != null; } }
        internal bool OwnsPaidName(object token) => token != null && ReferenceEquals(paidName, token);
        internal void RequirePaidNameSave(object token)
        {
            if (paidName != null && !OwnsPaidName(token)) throw new InvalidOperationException("Residence belongs to its retained paid name unit.");
        }
        internal PaidNameLease TryReservePaidName(string name)
        {
            lock (MutationGate)
            {
                RequirePersistenceEditable();
                if (persistence == null) throw new InvalidOperationException("Paid name requires a registered residence graph.");
                if (paidName != null || donation != null || communitySaveOwner != null || stagedSaveUnits.Count != 0) return null;
                var captured = CapturePersistence();
                return paidName = new(this, captured with { Header = captured.Header with { Name = name } });
            }
        }
        internal sealed class PaidNameLease(Residence owner, ResidencePersistence.Snapshot target)
        {
            private readonly TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            internal Task Completion => completion.Task;
            internal ulong Id => owner.Id;
            internal IResidenceMapInstance Map => owner.Map;
            internal bool Registered => ReferenceEquals(owner.globalResidenceManager.GetResidence(owner.Id), owner);
            private void Require()
            {
                if (!ReferenceEquals(owner.paidName, this)) throw new InvalidOperationException("Paid name lost its original residence reservation.");
            }
            internal void Stage(CharacterContext context)
            {
                lock (owner.MutationGate)
                {
                    Require(); owner.RegisterSaveUnit(context); owner.persistence.Save(context, this, target);
                }
            }
            internal bool Authorizes(EntityEntry entry)
            {
                lock (owner.MutationGate) { Require(); return owner.persistence.OwnsRow(entry); }
            }
            internal void ValidateCommitted(CharacterContext context)
            {
                lock (owner.MutationGate)
                {
                    Require(); owner.persistence.RequireOwner(context, this);
                    if (!ResidencePersistence.Equal(owner.persistence.Read(context), target))
                        throw new InvalidOperationException("Paid name residence differs from its committed frozen graph.");
                }
            }
            internal void Apply()
            {
                lock (owner.MutationGate)
                {
                    Require();
                    if (!Registered) return;
                    owner.name = target.Header.Name; owner.saveMask &= ~ResidenceSaveMask.Name;
                }
            }
            internal void PublishName(Guild.Community community, ulong leader)
            {
                lock (owner.MutationGate)
                {
                    Require();
                    if (owner.globalResidenceManager is not GlobalResidenceManager registry)
                        throw new InvalidOperationException("Paid name requires its original visitor registry.");
                    community.ConfirmDirectoryName(owner, target.Header.Name);
                    registry.PublishCommunityRename(owner, community, target.Header.Name, leader);
                }
            }
            internal ServerHousingProperties Packet()
            {
                lock (owner.MutationGate)
                {
                    // Rename changes only the name. Preserve later unrelated root
                    // properties, as the existing map rename producer does. Every
                    // retry constructs a fresh DTO from the current registered root.
                    var h = owner.CaptureHeader() with { Name = target.Header.Name };
                    var packet = new ServerHousingProperties();
                    packet.Residences.Add(new()
                    {
                        RealmId = owner.RealmId, ResidenceId = owner.Id, NeighbourhoodId = h.Guild ?? 0, CharacterIdOwner = h.Owner,
                        GuildIdOwner = owner.Type == ResidenceType.Community ? h.Guild : 0, Type = owner.Type, Name = h.Name,
                        PropertyInfoId = (PropertyInfoId)h.Property, WallpaperExterior = h.Wallpaper, Entryway = h.Entry,
                        Roof = h.Roof, Door = h.Door, Ground = h.Ground, Music = h.Music, Sky = h.Sky,
                        Flags = (ResidenceFlags)h.Flags, ResourceSharing = h.Resource, GardenSharing = h.Garden
                    });
                    return packet;
                }
            }
            internal void Finish()
            {
                lock (owner.MutationGate) { Require(); owner.paidName = null; completion.TrySetResult(); }
            }
        }
    }
}
