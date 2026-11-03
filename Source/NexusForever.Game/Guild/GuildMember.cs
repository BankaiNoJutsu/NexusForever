using Microsoft.EntityFrameworkCore.ChangeTracking;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Character;
using NexusForever.Game.Abstract.Guild;
using NetworkGuildMember = NexusForever.Network.World.Message.Model.Guild.GuildMember;

namespace NexusForever.Game.Guild
{
    public class GuildMember : IGuildMember
    {
        /// <summary>
        /// Determines which fields need saving for <see cref="IGuildMember"/> when being saved to the database.
        /// </summary>
        [Flags]
        public enum GuildMemberSaveMask
        {
            None                     = 0x0000,
            Create                   = 0x0001,
            Delete                   = 0x0002,
            Rank                     = 0x0004,
            Note                     = 0x0008,
            CommunityPlotReservation = 0x0010
        }

        public IGuildBase Guild { get; }
        public Identity PlayerIdentity { get; }
        public ulong CharacterId { get => PlayerIdentity.Id; }

        public IGuildRank Rank
        {
            get { lock (MutationGate) return rank; }
            set
            {
                lock (MutationGate) { (Guild as GuildBase)?.RequireMutationEditable(); rank = value; saveMask |= GuildMemberSaveMask.Rank; }
            }
        }
        private IGuildRank rank;

        public string Note
        {
            get { lock (MutationGate) return note; }
            set
            {
                lock (MutationGate) { (Guild as GuildBase)?.RequireMutationEditable(); note = value; saveMask |= GuildMemberSaveMask.Note; }
            }
        }
        private string note;

        public int CommunityPlotReservation
        {
            get { lock (MutationGate) return communityPlotReservation; }
            set
            {
                lock (MutationGate) { (Guild as GuildBase)?.RequireMutationEditable(); (Guild as Community)?.RequireAssociationPlotAssignment(); communityPlotReservation = value; saveMask |= GuildMemberSaveMask.CommunityPlotReservation; }
            }
        }
        private int communityPlotReservation;
        internal void AdoptAssociationReservation(int value) => communityPlotReservation = value;

        private object MutationGate => (Guild as GuildBase)?.MutationGate ?? this;
        private GuildMemberSaveMask saveMask;
        internal void AcknowledgeMutation(GuildMutationPersistence.Member stored)
        {
            saveMask &= ~GuildMemberSaveMask.Create;
            if (!PendingDelete && Rank.Index == stored.Rank && Note == stored.Note && CommunityPlotReservation == stored.Plot)
                saveMask = GuildMemberSaveMask.None;
        }
        public bool NeedsSave => saveMask != GuildMemberSaveMask.None;
        private readonly ICharacterManager characterManager;

        /// <summary>
        /// Returns if <see cref="IGuildMember"/> is enqueued to be saved to the database.
        /// </summary>
        public bool PendingCreate => (saveMask & GuildMemberSaveMask.Create) != 0;

        /// <summary>
        /// Returns if <see cref="IGuildMember"/> is enqueued to be deleted from the database.
        /// </summary>
        public bool PendingDelete => (saveMask & GuildMemberSaveMask.Delete) != 0;

        /// <summary>
        /// Create a new <see cref="IGuildMember"/> from an existing database model.
        /// </summary>
        public GuildMember(GuildMemberModel model, IGuildBase guild, IGuildRank guildRank, ICharacterManager characterManager = null)
        {
            Guild                    = guild;
            PlayerIdentity           = new Identity{ Id = model.CharacterId, RealmId = guild.Identity.RealmId };
            rank                     = guildRank;
            note                     = model.Note;
            communityPlotReservation = model.CommunityPlotReservation;
            this.characterManager    = characterManager;

            saveMask = GuildMemberSaveMask.None;
        }

        /// <summary>
        /// Create a new <see cref="IGuildMember"/> from the supplied member information.
        /// </summary>
        public GuildMember(IGuildBase guild, ulong characterId, IGuildRank guildRank, string note = "", ICharacterManager characterManager = null)
        {
            Guild                    = guild;
            PlayerIdentity           = new Identity { Id = characterId, RealmId = guild.Identity.RealmId };
            rank                     = guildRank;
            this.note                = note;
            communityPlotReservation = -1;
            this.characterManager    = characterManager;

            saveMask = GuildMemberSaveMask.Create;
        }

        /// <summary>
        /// Create a new <see cref="IGuildMember"/> from the supplied member information.
        /// </summary>
        public GuildMember(IGuildBase guild, Identity playerIdentity, IGuildRank guildRank, string note = "", ICharacterManager characterManager = null)
        {
            Guild                    = guild;
            PlayerIdentity           = playerIdentity;
            rank                     = guildRank;
            this.note                = note;
            communityPlotReservation = -1;
            this.characterManager    = characterManager;

            saveMask = GuildMemberSaveMask.Create;
        }

        /// <summary>
        /// Save this <see cref="IGuildMember"/> to a <see cref="GuildMemberModel"/>
        /// </summary>
        public void Save(CharacterContext context)
        {
            if (Guild is GuildBase concrete && concrete.SaveMemberMutations(this, context)) return;
            if (saveMask == GuildMemberSaveMask.None)
                return;

            var model = new GuildMemberModel
            {
                Id          = Guild.Id,
                CharacterId = CharacterId
            };

            if ((saveMask & GuildMemberSaveMask.Create) != 0)
            {
                model.Rank = rank.Index;
                model.Note = note;
                context.Add(model);
            }
            else if ((saveMask & GuildMemberSaveMask.Delete) != 0)
                context.Remove(model);
            else
            {
                EntityEntry<GuildMemberModel> entity = context.Attach(model);
                if ((saveMask & GuildMemberSaveMask.Rank) != 0)
                {
                    model.Rank = rank.Index;
                    entity.Property(p => p.Rank).IsModified = true;
                }
                if ((saveMask & GuildMemberSaveMask.Note) != 0)
                {
                    model.Note = note;
                    entity.Property(p => p.Note).IsModified = true;
                }
                if ((saveMask & GuildMemberSaveMask.CommunityPlotReservation) != 0)
                {
                    model.CommunityPlotReservation = communityPlotReservation;
                    entity.Property(p => p.CommunityPlotReservation).IsModified = true;
                }
            }

            saveMask = GuildMemberSaveMask.None;
        }

        /// <summary>
        /// Return a <see cref="NetworkGuildMember"/> packet of this <see cref="IGuildMember"/>
        /// </summary>
        public NetworkGuildMember Build()
        {
            ICharacter characterInfo = GetCharacterManager().GetCharacter(CharacterId);

            return new NetworkGuildMember
            {
                PlayerIdentity           = PlayerIdentity.ToNetworkIdentity(),
                Rank                     = rank.Index,
                Name                     = characterInfo.Name,
                Sex                      = characterInfo.Sex,
                Class                    = characterInfo.Class,
                Path                     = characterInfo.Path,
                Level                    = characterInfo.Level,
                Note                     = Note,
                LastLogoutTimeDays       = characterInfo.GetOnlineStatus() ?? 0f,
                CommunityReservedPlotIndex = communityPlotReservation
            };
        }

        private ICharacterManager GetCharacterManager()
        {
            return characterManager ?? throw new InvalidOperationException("GuildMember requires an ICharacterManager.");
        }

        /// <summary>
        /// Enqueue <see cref="IGuildMember"/> to be deleted from the database.
        /// </summary>
        public void EnqueueDelete(bool set)
        {
            lock (MutationGate)
            {
                (Guild as GuildBase)?.RequireMutationEditable();
                if (set)
                    saveMask |= GuildMemberSaveMask.Delete;
                else
                    saveMask &= ~GuildMemberSaveMask.Delete;
            }
        }
    }
}
