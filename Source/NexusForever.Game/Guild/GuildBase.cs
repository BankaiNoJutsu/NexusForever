using System.Collections;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Character;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Guild;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Guild;
using NexusForever.GameTable.Text.Filter;
using NexusForever.Network.Internal;
using NexusForever.Network.Internal.Message.Guild;
using NexusForever.Network.Message;
using NexusForever.Network.Message.Model.Shared;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Guild;
using NexusForever.Network.World.Message.Model.Shared;
using NexusForever.Shared;
using NLog;
using NetworkGuildMember = NexusForever.Network.World.Message.Model.Guild.GuildMember;
using NetworkGuildRank = NexusForever.Network.World.Message.Model.Guild.GuildRank;

namespace NexusForever.Game.Guild
{
    public abstract partial class GuildBase : IGuildBase
    {
        /// <summary>
        /// Determines which fields need saving for <see cref="IGuildBase"/> when being saved to the database.
        /// </summary>
        [Flags]
        public enum GuildBaseSaveMask
        {
            None     = 0x0000,
            Create   = 0x0001,
            Delete   = 0x0002,
            Name     = 0x0004,
            LeaderId = 0x0008,
            Flags    = 0x0010
        }

        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        public Abstract.Identity Identity { get; private set; }
        public ulong Id => Identity.Id;
        public abstract GuildType Type { get; }
        public DateTime CreateTime { get; private set; }

        public string Name
        {
            get => name;
            set
            {
                if (paidCreation != null) RequireMutationEditable();
                if (this is Community community) community.RequirePaidNameAssignment();
                if (pendingDisband == null && globalGuildManager?.AssignRegisteredGuildName(this, value) == true)
                    return;
                AssignName(value);
            }
        }
        private string name;
        private GuildNamePersistence namePersistence;
        internal bool IsDisbandAccepted => pendingDisband != null;
        internal void AssignName(string value)
        {
            name = value;
            saveMask |= GuildBaseSaveMask.Name;
        }

        public ulong? LeaderId
        {
            get { lock (MutationGate) return leaderId; }
            set
            {
                lock (MutationGate) { RequireMutationEditable(); leaderId = value; saveMask |= GuildBaseSaveMask.LeaderId; }
            }
        }
        private ulong? leaderId;

        public GuildFlag Flags
        {
            get { lock (MutationGate) return guildFlags; }
            set
            {
                lock (MutationGate)
                {
                    RequireMutationEditable();
                    if (this is Community community) community.RequirePrivacyFlagAssignment(value);
                    guildFlags = value; saveMask |= GuildBaseSaveMask.Flags;
                }
            }
        }
        private GuildFlag guildFlags;

        private GuildBaseSaveMask saveMask;

        public uint MemberCount { get { lock (MutationGate) return (uint)members.Values.Count(member => !member.PendingDelete); } }

        /// <summary>
        /// Returns if <see cref="IGuildBase"/> is enqueued to be saved to the database.
        /// </summary>
        public bool PendingCreate => (saveMask & GuildBaseSaveMask.Create) != 0;

        /// <summary>
        /// Returns if <see cref="IGuildBase"/> is enqueued to be deleted from the database.
        /// </summary>
        public virtual bool PendingDelete => pendingDisband != null || (saveMask & GuildBaseSaveMask.Delete) != 0;

        public virtual bool NeedsSave => pendingDisband != null ? !pendingDisband.Published
            : NeedsMutationPublication || namePersistence?.NeedsSave == true || (mutationPersistence?.NeedsSave
                ?? (saveMask != GuildBaseSaveMask.None || ranks.Values.Any(r => r.NeedsSave) || members.Values.Any(m => m.NeedsSave)));

        /// <summary>
        /// Maximum number of <see cref="IGuildMember"/>'s allowed in the guild.
        /// </summary>
        public abstract uint MaxMembers { get; }

        protected readonly SortedDictionary</*index*/byte, IGuildRank> ranks = new();
        protected readonly Dictionary</*characterId*/ulong, IGuildMember> members = new();
        protected readonly List</*characterId*/ulong> onlineMembers = new();

        #region Dependency Injection

        private readonly IRealmContext realmContext;
        private readonly IInternalMessagePublisher messagePublisher;
        private readonly ICharacterManager characterManager;
        private readonly IGlobalGuildManager globalGuildManager;
        protected readonly IPlayerManager playerManager;
        protected readonly ITextFilterManager textFilterManager;

        public GuildBase(
            IRealmContext realmContext,
            IInternalMessagePublisher messagePublisher,
            ITextFilterManager textFilterManager,
            ICharacterManager characterManager = null,
            IGlobalGuildManager globalGuildManager = null,
            IPlayerManager playerManager = null)
        {
            this.realmContext     = realmContext;
            this.messagePublisher = messagePublisher;
            this.textFilterManager = textFilterManager;
            this.characterManager = characterManager;
            this.globalGuildManager = globalGuildManager;
            this.playerManager    = playerManager;
        }

        #endregion

        /// <summary>
        /// Create a new <see cref="IGuildBase"/> from an existing database model.
        /// </summary>
        public virtual void Initialise(GuildModel model)
        {
            Identity   = new Abstract.Identity
            {
                RealmId = realmContext.RealmId,
                Id     = model.Id
            };
            Name       = model.Name;
            Flags      = (GuildFlag)model.Flags;
            LeaderId   = model.LeaderId;
            CreateTime = model.CreateTime;

            foreach (GuildRankModel rankModel in model.GuildRank)
                ranks.Add(rankModel.Index, new GuildRank(rankModel));

            foreach (GuildMemberModel memberModel in model.GuildMember)
            {
                if (!ranks.TryGetValue(memberModel.Rank, out IGuildRank rank))
                    throw new DatabaseDataException($"Guild member {memberModel.Id} has an invalid rank {memberModel.Rank} for guild {memberModel.Guild.Id}!");
                
                var member = new GuildMember(memberModel, this, rank, characterManager);
                rank.AddMember(member);
                members.Add(memberModel.CharacterId, member);
            }

            saveMask = GuildBaseSaveMask.None;
            namePersistence = new GuildNamePersistence(this, model.Name, GuildLoadOrigin.Get(model),
                savedName => globalGuildManager?.GuildNameSaved(this, savedName));
            InitialiseMutations(GuildMutationPersistence.FromModel(model), GuildLoadOrigin.Get(model));
        }

        /// <summary>
        /// Create a new <see cref="IGuildBase"/> using supplied parameters.
        /// </summary>
        public virtual void Initialise(string guildName, string leaderRankName, string councilRankName, string memberRankName)
        {
            Identity    = new Abstract.Identity
            {
                RealmId = realmContext.RealmId,
                Id      = GetGlobalGuildManager().NextGuildId
            };
            Name       = guildName;
            Flags      = GuildFlag.None;
            CreateTime = new DateTime(DateTime.UtcNow.Ticks / TimeSpan.TicksPerSecond * TimeSpan.TicksPerSecond, DateTimeKind.Utc);

            InitialiseRanks(leaderRankName, councilRankName, memberRankName);

            saveMask = GuildBaseSaveMask.Create;
            InitialiseMutations(null, null);
        }

        protected virtual void InitialiseRanks(string leaderRankName, string councilRankName, string memberRankName)
        {
            AddRank(0, leaderRankName, GuildRankPermission.Leader, ulong.MaxValue, ulong.MaxValue, ulong.MaxValue);
            AddRank(1, councilRankName, GuildRankPermission.Council, ulong.MaxValue, ulong.MaxValue, ulong.MaxValue);
            AddRank(9, memberRankName, GuildRankPermission.MemberChat, 0, 0, 0);
        }

        /// <summary>
        /// Save this <see cref="IGuildBase"/> to a <see cref="GuildModel"/>
        /// </summary>
        public virtual void Save(CharacterContext context)
        {
            lock (MutationGate)
            {
                if (paidCreation != null && !paidCreation.OwnsEnrollment(context))
                {
                    var error = new InvalidOperationException("Paid guild creation owns its complete fee and guild save.");
                    context.RejectSave(error); throw error;
                }
                mutationPersistence?.RequireAvailable(context);
                mutationPersistence?.RequireOriginalDatabase(context);
                namePersistence?.RequireAvailable(context);
                if (OwnsMutationSave(context) || pendingDisband != null && namePersistence?.Owns(context) == true) return;
                if (pendingDisband is { Published: true }) return;
                if (pendingDisband is { Committed: true })
                {
                    if (mutationPersistence?.NeedsSave == true)
                    {
                        ObserveDisbandSave(context, pendingDisband, true);
                        mutationPersistence.Save(context, true);
                        Save(context, GuildBaseSaveMask.None);
                    }
                    return;
                }
                if (pendingDisband != null && PendingCreate)
                {
                    if (CanCancelUnpersistedCreation) return;
                    // Resolve the attempted create before archiving it. Inserting
                    // NULL into the generated-default name column is not equivalent
                    // to the existing tombstone UPDATE.
                    mutationPersistence.Save(context);
                    Save(context, GuildBaseSaveMask.None);
                    return;
                }
                if (pendingDisband != null)
                    lock (pendingDisband.Contexts)
                        if (pendingDisband.Contexts.Contains(context))
                            return;
                if (pendingDisband != null && !pendingDisband.Committed && !PendingCreate)
                    saveMask |= GuildBaseSaveMask.Delete;
                if (pendingDisband == null) namePersistence?.Save(context);
                if (mutationPersistence != null)
                {
                    if (pendingDisband != null)
                    {
                        StageDisbandHeader(context);
                        ObserveDisbandSave(context, pendingDisband);
                    }
                    mutationPersistence.Save(context, pendingDisband != null, this is Community { PrivacyNeedsCommit: true });
                    Save(context, GuildBaseSaveMask.None);
                    return;
                }
                bool creating = (saveMask & GuildBaseSaveMask.Create) != 0;
                if (saveMask != GuildBaseSaveMask.None)
                {
                    if ((saveMask & GuildBaseSaveMask.Create) != 0)
                    {
                        context.Add(new GuildModel
                        {
                            Id         = Id,
                            Type       = (byte)Type,
                            Name       = Name,
                            LeaderId   = LeaderId,
                            CreateTime = CreateTime
                        });
                    }
                    else if ((saveMask & GuildBaseSaveMask.Delete) != 0)
                        StageDisbandHeader(context);
                    else
                    {
                        EntityEntry<GuildModel> entity = context.ChangeTracker.Entries<GuildModel>()
                            .SingleOrDefault(row => row.Entity.Id == Id) ?? context.Attach(new GuildModel { Id = Id });
                        GuildModel model = entity.Entity;
                        if (namePersistence == null && (saveMask & GuildBaseSaveMask.Name) != 0)
                        {
                            model.Name = name;
                            entity.Property(p => p.Name).IsModified = true;
                        }

                        if ((saveMask & GuildBaseSaveMask.LeaderId) != 0)
                        {
                            model.LeaderId = LeaderId;
                            entity.Property(p => p.LeaderId).IsModified = true;
                        }

                        if ((saveMask & GuildBaseSaveMask.Flags) != 0)
                        {
                            model.Flags = (uint)Flags;
                            entity.Property(p => p.Flags).IsModified = true;
                        }
                    }
                }

                Save(context, saveMask);
                saveMask = GuildBaseSaveMask.None;

                if (pendingDisband is { Committed: false } disband)
                    ObserveDisbandSave(context, disband);

                foreach (IGuildRank rank in ranks.Values.ToList())
                {
                    if (rank.PendingDelete)
                        ranks.Remove(rank.Index);

                    rank.Save(context);
                }

                foreach (IGuildMember member in members.Values.ToList())
                {
                    if (member.PendingDelete)
                        members.Remove(member.CharacterId);

                    member.Save(context);
                }
                if (creating)
                    ObserveCreatedName(context);
            }
        }

        protected virtual void Save(CharacterContext context, GuildBaseSaveMask saveMask)
        {
            // deliberately empty
        }

        public virtual GuildData Build()
        {
            lock (MutationGate)
            {
                return new GuildData
                {
                    GuildId           = Id,
                    GuildName         = Name,
                    Flags             = Flags,
                    Type              = Type,
                    Ranks             = GetGuildRanksPackets().ToList(),
                    MemberCount       = MemberCount,
                    OnlineMemberCount = (uint)onlineMembers.Count,
                    GuildInfo =
                    {
                        GuildCreationDateInDays = GetGuildCreationDateInDays()
                    }
                };
            }
        }

        protected float GetGuildCreationDateInDays()
        {
            return (float)DateTime.UtcNow.Subtract(ToUtc(CreateTime)).TotalDays * -1f;
        }

        private static DateTime ToUtc(DateTime dateTime)
        {
            return dateTime.Kind switch
            {
                DateTimeKind.Utc         => dateTime,
                DateTimeKind.Unspecified => DateTime.SpecifyKind(dateTime, DateTimeKind.Utc),
                _                        => dateTime.ToUniversalTime()
            };
        }

        /// <summary>
        /// Add a new <see cref="GuildFlag"/>.
        /// </summary>
        public void SetFlag(GuildFlag flags)
        {
            Flags |= flags;
        }

        /// <summary>
        /// Remove an existing <see cref="GuildFlag"/>.
        /// </summary>
        public void RemoveFlag(GuildFlag flags)
        {
            Flags &= ~flags;
        }

        /// <summary>
        /// Returns if supplied <see cref="GuildFlag"/> exists.
        /// </summary>
        public bool HasFlags(GuildFlag flags)
        {
            return (Flags & flags) != 0;
        }

        /// <summary>
        /// Trigger login events for <see cref="IPlayer"/> for <see cref="IGuildBase"/>.
        /// </summary>
        public void OnPlayerLogin(IPlayer player)
        {
            if (!members.TryGetValue(player.CharacterId, out IGuildMember member))
                throw new ArgumentException($"Invalid member {player.CharacterId} for guild {Id}.");

            MemberOnline(member);

            AnnounceGuildMemberChange(member);
            AnnounceGuildResult(GuildResult.MemberOnline, referenceText: player.Name);
        }

        /// <summary>
        /// Invoked when a <see cref="IGuildMember"/> comes online.
        /// </summary>
        protected virtual void MemberOnline(IGuildMember member)
        {
            onlineMembers.Add(member.CharacterId);
        }

        /// <summary>
        /// Trigger logout events for <see cref="IPlayer"/> for <see cref="IGuildBase"/>.
        /// </summary>
        public void OnPlayerLogout(IPlayer player)
        {
            if (!members.TryGetValue(player.CharacterId, out IGuildMember member))
                throw new ArgumentException($"Invalid member {player.CharacterId} for guild {Id}.");

            MemberOffline(member);

            AnnounceGuildMemberChange(member);
            AnnounceGuildResult(GuildResult.MemberOffline, referenceText: player.Name);
        }

        /// <summary>
        /// Invoked when a <see cref="IGuildMember"/> goes offline.
        /// </summary>
        protected virtual void MemberOffline(IGuildMember member)
        {
            onlineMembers.Remove(member.CharacterId);
        }

        /// <summary>
        /// Returns if <see cref="IPlayer"/> can join the <see cref="IGuildBase"/>.
        /// </summary>
        public virtual IGuildResultInfo CanJoinGuild(IPlayer player)
        {
            if (PendingDelete)
                return new GuildResultInfo(GuildResult.NotAGuild);
            if (MemberCount >= MaxMembers)
                return new GuildResultInfo(GuildResult.CannotInviteGuildFull);

            if (GetMember(player.CharacterId) != null)
                return new GuildResultInfo(GuildResult.AlreadyAMember);

            return new GuildResultInfo(GuildResult.Success);
        }

        /// <summary>
        /// Add a new <see cref="IPlayer"/> to the <see cref="IGuildBase"/>.
        /// </summary>
        /// <remarks>
        /// <see cref="CanJoinGuild(IPlayer)"/> should be invoked before invoking this method.
        /// If the <see cref="IGuildBase"/> has no members the <see cref="IPlayer"/> will become the leader.
        /// </remarks>
        public void JoinGuild(IPlayer player)
        {
            lock (MutationGate)
            {
                if (IsDisbandAccepted) throw new InvalidOperationException("Disband has captured its original membership.");
                IGuildMember member;
                if (MemberCount == 0u)
                {
                    log.Trace($"Guild{Id} has no leader, new member {player.CharacterId} will be assigned to leader.");

                    LeaderId = player.CharacterId;
                    member   = AddMember(player, 0);
                    SendGuildResult(player.Session, GuildResult.YouCreated, Identity, referenceText: Name);
                }
                else
                {
                    member = AddMember(player);
                    SendGuildResult(player.Session, GuildResult.YouJoined, Identity, referenceText: Name);
                }

                SendGuildJoin(player, member.Build(), new GuildWithdrawlInfo());
                SendGuildRoster(player.Session);
                AnnounceGuildMemberChange(member);
            }
        }

        private void SendGuildJoin(IPlayer player, NetworkGuildMember guildMember, GuildWithdrawlInfo playerLimits)
        {
            player.Session.EnqueueMessageEncrypted(new ServerGuildJoin
            {
                GuildData                 = Build(),
                Self                      = guildMember,
                WithdrawlInfo             = playerLimits,
                DisplayThisGuildNameplate = player.GuildManager is GuildManager committed
                    ? committed.PublishedAffiliation.Id == Id : player.GuildManager.GuildAffiliation?.Id == Id
            });
        }

        private void SendGuildRoster(IGameSession session)
        {
            session.EnqueueMessageEncrypted(new ServerGuildRoster
            {
                GuildIdentity = Identity.ToNetworkIdentity(),
                GuildMembers = members.Values
                    .Where(member => !member.PendingDelete)
                    .Select(m => m.Build())
                    .ToList(),
                Done       = true
            });
        }

        /// <summary>
        /// Returns if <see cref="IPlayer"/> can leave the <see cref="IGuildBase"/>.
        /// </summary>
        public IGuildResultInfo CanLeaveGuild(IPlayer player)
        {
            IGuildMember member = GetMember(player.CharacterId);
            if (member == null)
                return new GuildResultInfo(GuildResult.NotInThatGuild);

            return CanLeaveGuild(member);
        }

        /// <summary>
        /// Returns if <see cref="IGuildMember"/> can leave the <see cref="IGuildBase"/>.
        /// </summary>
        protected virtual IGuildResultInfo CanLeaveGuild(IGuildMember member)
        {
            if (member.Rank.Index == 0)
                return new GuildResultInfo(GuildResult.GuildmasterCannotLeaveGuild);

            return new GuildResultInfo(GuildResult.Success);
        }

        /// <summary>
        /// Remove an existing <see cref="IPlayer"/> from the <see cref="IGuildBase"/>.
        /// </summary>
        /// <remarks>
        /// <see cref="CanLeaveGuild(IPlayer)"/> should be invoked before invoking this method.
        /// </remarks>
        public void LeaveGuild(IPlayer player, GuildResult reason)
        {
            lock (MutationGate)
            {
                if (paidCreation != null) RequireMutationEditable();
                if (IsDisbandAccepted) throw new InvalidOperationException("Disband has captured its original membership.");
                if (!members.TryGetValue(player.CharacterId, out IGuildMember member))
                    throw new ArgumentException($"Invalid member {player.CharacterId} for guild {Id}.");

                LeaveGuild(member, reason == GuildResult.GuildDisbanded);
                SendGuildResult(player.Session, reason, Identity, referenceText: Name);

                player.Session.EnqueueMessageEncrypted(new ServerGuildRemove
                {
                    GuildIdentity = Identity.ToNetworkIdentity(),
                });
            }
        }

        /// <summary>
        /// Remove an existing <see cref="IGuildMember"/> from the <see cref="IGuildBase"/>.
        /// </summary>
        /// <remarks>
        /// <see cref="CanLeaveGuild(IGuildMember)"/> should be invoked before invoking this method.
        /// </remarks>
        private void LeaveGuild(IGuildMember member, bool disband = false)
        {
            RemoveMember(member.CharacterId, disband);

            if (!disband)
            {
                Broadcast(new ServerGuildMemberRemove
                {
                    GuildIdentity  = Identity.ToNetworkIdentity(),
                    MemberToRemove = member.PlayerIdentity.ToNetworkIdentity(),
                });
            }

            GetGlobalGuildManager().UntrackCharacterGuild(member.CharacterId, Id);
        }

        /// <summary>
        /// Returns if the <see cref="IGuildBase"/> can be disbanded.
        /// </summary>
        protected virtual IGuildResultInfo CanDisbandGuild()
        {
            // deliberately returns success
            return new GuildResultInfo(GuildResult.Success);
        }

        /// <summary>
        /// Disband <see cref="IGuildBase"/>.
        /// </summary>
        public virtual void DisbandGuild()
        {
            lock (MutationGate)
            {
                if (paidCreation != null) RequireMutationEditable();
                if (pendingDisband != null)
                    return;
                var disband = new DisbandChange(Name, LeaderId, members.Values.Select(member => member.CharacterId).ToArray());
                foreach (IGuildMember member in members.Values.ToList())
                {
                    // if the player is online handle through the local manager otherwise directly in the guild
                    IPlayer player = playerManager?.GetPlayer(member.CharacterId);
                    if (player != null)
                        player.GuildManager.LeaveGuild(Id, GuildResult.GuildDisbanded);
                    else
                        LeaveGuild(member, true);
                }

                pendingDisband = disband;
                saveMask |= GuildBaseSaveMask.Delete;
                log.Trace($"Guild {Id} was disbanded.");
            }
        }

        /// <summary>
        /// Add a new <see cref="IGuildRank"/> using the supplied parameters.
        /// </summary>
        private void AddRank(byte index, string name, GuildRankPermission permissions = GuildRankPermission.Disabled,
            ulong bankPermissions = 0ul, ulong bankMoneyWithdrawlLimits = 0ul, ulong repairLimits = 0ul)
        {
            lock (MutationGate)
            {
                if (paidCreation != null) RequireMutationEditable();
                if (IsDisbandAccepted) throw new InvalidOperationException("Disband has captured its original membership.");
                if (index > 9)
                    throw new ArgumentOutOfRangeException(nameof(index), $"Index {index} out of range, maximum rank index is 9!");

                if (ranks.TryGetValue(index, out IGuildRank rank))
                {
                    if (!rank.PendingDelete)
                        throw new InvalidOperationException($"Rank {index} for guild {Id} already exists!");

                    // rank is pending delete, reuse object
                    rank.EnqueueDelete(false);
                    rank.Name                     = name;
                    rank.Permissions              = permissions;
                    rank.BankPermissions          = bankPermissions;
                    rank.BankMoneyWithdrawlLimits = bankMoneyWithdrawlLimits;
                    rank.RepairLimit              = repairLimits;
                }
                else
                {
                    var created = new GuildRank(Id, index, name, permissions, bankPermissions, bankMoneyWithdrawlLimits, repairLimits);
                    created.BindSaveOwner(this);
                    rank = created;
                    ranks.Add(index, rank);
                }

                log.Trace($"Added rank {name}({index}) to guild {Id}.");
            }
        }

        /// <summary>
        /// Remove an existing <see cref="IGuildRank"/> at the supplied index.
        /// </summary>
        private void RemoveRank(byte index)
        {
            lock (MutationGate)
            {
                if (paidCreation != null) RequireMutationEditable();
                if (IsDisbandAccepted) throw new InvalidOperationException("Disband has captured its original membership.");
                if (index > 9)
                    throw new ArgumentOutOfRangeException(nameof(index), $"Index {index} out of range, maximum rank index is 9!");

                if (!ranks.TryGetValue(index, out IGuildRank rank))
                    throw new ArgumentException($"Invalid rank {rank} for guild {Id}.");

                if (rank.MemberCount > 0u)
                    throw new InvalidOperationException($"Rank {index} for guild {Id} has existing members!");

                // rank is pending create, direct remove
                if (rank.PendingCreate && mutationPersistence?.WasRankAttempted(index) != true)
                    ranks.Remove(index);
                else
                    rank.EnqueueDelete(true);

                log.Trace($"Removed rank {rank.Name}({rank.Index}) from guild {Id}.");
            }
        }

        /// <summary>
        /// Returns if a <see cref="IGuildRank"/> exists with the given name.
        /// </summary>
        private bool RankExists(string name)
        {
            return ranks.Values.FirstOrDefault(r =>
                !r.PendingDelete && string.Equals(r.Name, name, StringComparison.InvariantCultureIgnoreCase)) != null;
        }

        /// <summary>
        /// Returns the <see cref="IGuildRank"/> using the given index
        /// </summary>
        private IGuildRank GetRank(byte index)
        {
            if (ranks.TryGetValue(index, out IGuildRank rank) && !rank.PendingDelete)
                return rank;
            return null;
        }

        /// <summary>
        /// Returns the <see cref="IGuildRank"/> that is below the rank at the given index.
        /// </summary>
        /// <remarks>
        /// A demoted rank has an increased index.
        /// </remarks>
        private IGuildRank GetDemotedRank(byte index)
        {
            if (index > 8)
                throw new ArgumentOutOfRangeException(nameof(index), $"Index {index} out of range, maximum demote rank index is 8!");

            for (byte i = (byte)(index + 1); i <= 9; i++)
                if (ranks.TryGetValue(i, out IGuildRank rank) && !rank.PendingDelete)
                    return rank;

            return null;
        }

        /// <summary>
        /// Returns the <see cref="IGuildRank"/> that is above the rank at the given index.
        /// </summary>
        /// <remarks>
        /// A promoted rank has an decreased index.
        /// </remarks>
        private IGuildRank GetPromotedRank(byte index)
        {
            if (index < 1)
                throw new ArgumentOutOfRangeException(nameof(index), $"Index {index} out of range, minimum promote rank index is 1!");
            if (index > 9)
                throw new ArgumentOutOfRangeException(nameof(index), $"Index {index} out of range, maximum demote rank index is 9!");

            for (sbyte i = (sbyte)(index - 1); i >= 0; i--)
                if (ranks.TryGetValue((byte)i, out IGuildRank rank) && !rank.PendingDelete)
                    return rank;

            return null;
        }

        protected virtual void MemberChangeRank(IGuildMember member, IGuildRank newRank)
        {
            lock (MutationGate)
            {
                if (IsDisbandAccepted) throw new InvalidOperationException("Disband has captured its original membership.");
                member.Rank.RemoveMember(member);
                newRank.AddMember(member);
                member.Rank = newRank;

                PublishMemberMutation(new GuildMemberRankUpdatedMessage
                {
                    GuildId = Id,
                    Type    = Type,
                    Member  = new Network.Internal.Message.Shared.Identity
                    {
                        Id      = member.CharacterId,
                        RealmId = realmContext.RealmId
                    },
                    Rank        = newRank.Index,
                    Permissions = newRank.Permissions
                });
            }
        }

        /// <summary>
        /// Return a collection of <see cref="Message.Model.Shared.GuildRank"/>'s.
        /// </summary>
        /// <remarks>
        /// This will always return 10 ranks, if there are less than 10 ranks in the <see cref="IGuildBase"/> empty place holders will be returned.
        /// </remarks>
        protected IEnumerable<NetworkGuildRank> GetGuildRanksPackets()
        {
            for (byte i = 0; i < 10; i++)
            {
                IGuildRank rank = GetRank(i);
                if (rank != null)
                    yield return rank.Build();
                else
                    yield return new NetworkGuildRank();
            }
        }

        /// <summary>
        /// Add a new <see cref="IGuildMember"/> with supplied <see cref="IPlayer"/>.
        /// </summary>
        protected virtual IGuildMember AddMember(IPlayer player, byte rank = 9)
        {
            lock (MutationGate)
            {
                if (paidCreation != null) RequireMutationEditable();
                if (IsDisbandAccepted) throw new InvalidOperationException("Disband has captured its original membership.");
                if (!ranks.TryGetValue(rank, out IGuildRank guildRank))
                    throw new ArgumentException($"Invalid rank {rank} for guild {Id}.");

                if (members.TryGetValue(player.CharacterId, out IGuildMember member))
                {
                    if (!member.PendingDelete)
                        throw new InvalidOperationException($"Member {player.CharacterId} for guild {Id} already exists!");

                    // rank is pending delete, reuse object
                    member.EnqueueDelete(false);
                    member.Rank = guildRank;
                }
                else
                {
                    // new members default to the lowest rank
                    member = new GuildMember(this, player.CharacterId, guildRank, characterManager: characterManager);
                    members.Add(player.CharacterId, member);
                }

                PublishMemberMutation(new GuildMemberAddedMessage
                {
                    GuildId     = Id,
                    Type        = Type,
                    Member      = player.Identity.ToInternalIdentity(),
                    Rank        = guildRank.Index,
                    Permissions = guildRank.Permissions
                });

                MemberOnline(member);
                guildRank.AddMember(member);

                log.Trace($"Added member {player.CharacterId} to guild {Id}.");
                return member;
            }
        }

        /// <summary>
        /// Remove an existing <see cref="IGuildMember"/> with supplied character id.
        /// </summary>
        protected virtual void RemoveMember(ulong characterId, bool disband)
        {
            lock (MutationGate)
            {
                if (paidCreation != null) RequireMutationEditable();
                if (IsDisbandAccepted) throw new InvalidOperationException("Disband has captured its original membership.");
                if (!members.TryGetValue(characterId, out IGuildMember member))
                    throw new ArgumentException($"Invalid member {characterId} for guild {Id}.");

                if (!disband)
                {
                    // member is pending create, direct remove
                    if (member.PendingCreate && mutationPersistence?.WasMemberAttempted(characterId) != true)
                        members.Remove(characterId);
                    else
                        member.EnqueueDelete(true);
                }

                if (!disband)
                    PublishMemberMutation(new GuildMemberRemovedMessage
                    {
                        GuildId = Id,
                        Type    = Type,
                        Member  = new Network.Internal.Message.Shared.Identity
                        {
                            Id      = characterId,
                            RealmId = realmContext.RealmId
                        }
                    });

                MemberOffline(member);
                member.Rank.RemoveMember(member);

                log.Trace($"Removed member {member.CharacterId} from guild {Id}.");
            }
        }

        /// <summary>
        /// Return <see cref="IGuildMember"/> with supplied character id.
        /// </summary>
        public IGuildMember GetMember(ulong characterId)
        {
            lock (MutationGate)
            {
                if (members.TryGetValue(characterId, out IGuildMember member) && !member.PendingDelete)
                    return member;
                return null;
            }
        }

        /// <summary>
        /// Return <see cref="IGuildMember"/> with supplied character name.
        /// </summary>
        public IGuildMember GetMember(string memberName)
        {
            ulong? characterId = GetCharacterManager().GetCharacterIdByName(memberName);
            if (characterId == null)
                return null;

            return GetMember(characterId.Value);
        }

        private ICharacterManager GetCharacterManager()
        {
            return characterManager ?? throw new InvalidOperationException("GuildBase requires an ICharacterManager.");
        }

        private IGlobalGuildManager GetGlobalGuildManager()
        {
            return globalGuildManager ?? throw new InvalidOperationException("GuildBase requires an IGlobalGuildManager.");
        }

        /// <summary>
        /// Send <see cref="ServerGuildResult"/> to <see cref="IGameSession"/> based on supplied <see cref="IGuildResultInfo"/>.
        /// </summary>
        public static void SendGuildResult(IGameSession session, IGuildResultInfo info)
        {
            SendGuildResult(session, info.Result, info.GuildIdentity, info.ReferenceId, info.ReferenceString);
        }

        /// <summary>
        /// Send <see cref="ServerGuildResult"/> to <see cref="IGameSession"/> based on supplied parameters.
        /// </summary>
        public static void SendGuildResult(IGameSession session, GuildResult result, Abstract.Identity guildIdentity, uint referenceId = 0u, string referenceText = "")
        {
            session.EnqueueMessageEncrypted(new ServerGuildResult
            {
                Result        = result,
                GuildIdentity = guildIdentity.ToNetworkIdentity(),
                ReferenceId   = referenceId,
                TargetName    = referenceText
            });
        }

        /// <summary>
        /// Send <see cref="IWritable"/> to all online members.
        /// </summary>
        public void Broadcast(IWritable writable)
        {
            foreach (ulong characterId in onlineMembers)
            {
                IPlayer player = playerManager?.GetPlayer(characterId);
                player?.Session?.EnqueueMessageEncrypted(writable);
            }
        }

        /// <summary>
        /// Sends <see cref="ServerGuildResult"/> to all online members based on supplied parameters.
        /// </summary>
        private void AnnounceGuildResult(GuildResult result, uint referenceId = 0, string referenceText = "")
        {
            Broadcast(new ServerGuildResult
            {
                Result        = result,
                GuildIdentity = Identity.ToNetworkIdentity(),
                ReferenceId   = referenceId,
                TargetName    = referenceText,
            });
        }

        /// <summary>
        /// Send <see cref="ServerGuildMemberChange"/> to all online members with supplied <see cref="IGuildMember"/>.
        /// </summary>
        protected void AnnounceGuildMemberChange(IGuildMember member)
        {
            Broadcast(new ServerGuildMemberChange
            {
                GuildIdentity     = Identity.ToNetworkIdentity(),
                GuildMember       = member.Build(),
                MemberCount       = (ushort)MemberCount,
                OnlineMemberCount = (ushort)onlineMembers.Count
            });
        }

        /// <summary>
        /// Send <see cref="ServerGuildRankChange"/> to all online members.
        /// </summary>
        private void AnnounceGuildRankChange()
        {
            Broadcast(new ServerGuildRankChange
            {
                GuildIdentity = Identity.ToNetworkIdentity(),
                Ranks         = GetGuildRanksPackets().ToList()
            });
        }

        protected void SendGuildFlagUpdate()
        {
            Broadcast(new ServerGuildFlagsUpdate
            {
                GuildIdentity = Identity.ToNetworkIdentity(),
                Flags         = Flags
            });
        }

        /// <summary>
        /// Rename <see cref="IGuildBase"/> with supplied name.
        /// </summary>
        public virtual void RenameGuild(string name)
        {
            Name = name;

            Broadcast(new ServerGuildRename
            {
                GuildIdentity = Identity.ToNetworkIdentity(),
                Name          = name
            });
        }

        public IEnumerator<IGuildMember> GetEnumerator()
        {
            lock (MutationGate)
            {
                return members.Values.Where(member => !member.PendingDelete).ToArray().AsEnumerable().GetEnumerator();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
