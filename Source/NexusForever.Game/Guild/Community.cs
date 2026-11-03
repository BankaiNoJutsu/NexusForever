using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Character;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Guild;
using NexusForever.Game.Abstract.Housing;
using NexusForever.Game.Abstract.Map.Lock;
using NexusForever.Game.Housing;
using NexusForever.Game.Static.Guild;
using NexusForever.GameTable.Text.Filter;
using NexusForever.Network.Internal;

namespace NexusForever.Game.Guild
{
    public partial class Community : GuildBase, ICommunity
    {
        public override GuildType Type => GuildType.Community;
        public override uint MaxMembers => 20u;

        public IResidence Residence { get; set; }

        #region Dependency Injection

        private readonly IGlobalResidenceManager globalResidenceManager;
        private readonly IMapLockManager mapLockManager;
        private readonly IPlayerManager residencePlayerManager;

        public Community(
            IRealmContext realmContext,
            IInternalMessagePublisher messagePublisher,
            IGlobalResidenceManager globalResidenceManager,
            ITextFilterManager textFilterManager,
            IMapLockManager mapLockManager,
            ICharacterManager characterManager = null,
            IGlobalGuildManager globalGuildManager = null,
            IPlayerManager playerManager = null)
            : base(realmContext, messagePublisher, textFilterManager, characterManager, globalGuildManager, playerManager)
        {
            this.globalResidenceManager = globalResidenceManager;
            this.mapLockManager         = mapLockManager;
            residencePlayerManager      = playerManager;
        }

        #endregion

        /// <summary>
        /// Create a new <see cref="ICommunity"/> using supplied parameters.
        /// </summary>
        public override void Initialise(string guildName, string leaderRankName, string councilRankName, string memberRankName)
        {
            base.Initialise(guildName, leaderRankName, councilRankName, memberRankName);
            Residence = globalResidenceManager.CreateCommunity(this);

        }

        private IResidence creationResidence;
        private readonly object directoryGate = new();
        private IResidence directoryResidence;
        private string directoryName;
        private uint directoryFlags;
        private ulong? directoryLeader;
        internal bool DirectoryPrivateConfirmed { get { lock (directoryGate) return (directoryFlags & (uint)GuildFlag.CommunityPrivate) != 0; } }
        internal ulong? DirectoryLeaderConfirmed { get { lock (directoryGate) return directoryLeader; } }

        internal void ConfirmDirectoryMutations(uint flags, ulong? leader)
        {
            lock (directoryGate)
            {
                directoryFlags = flags;
                directoryLeader = leader;
                QueueConfirmedDirectory();
            }
        }
        internal void ConfirmDirectoryName(IResidence residence, string name)
        {
            lock (directoryGate)
            {
                directoryResidence = residence;
                directoryName = name;
                QueueConfirmedDirectory();
            }
        }
        private void QueueConfirmedDirectory()
        {
            if (directoryResidence != null && directoryName != null && globalResidenceManager is GlobalResidenceManager registry)
                registry.QueueConfirmedCommunityDirectory(directoryResidence, this, directoryName, directoryLeader, DirectoryPrivateConfirmed);
        }
        internal void PrepareResidenceCreation(IResidence residence)
        {
            lock (residenceLifecycleLock)
            {
                creationResidence ??= residence;
                QueueResidenceChange(residence);
            }
        }
        internal void QueueCreatedDirectory(string name, ulong? leader, uint flags)
        {
            lock (directoryGate)
            {
                directoryLeader = leader;
                directoryFlags = flags;
                if (creationResidence != null) ConfirmDirectoryName(creationResidence, name);
            }
        }

        /// <summary>
        /// Set <see cref="ICommunity"/> privacy level.
        /// </summary>
        public void SetCommunityPrivate(bool enabled)
        {
            if (NameRegistry is GlobalGuildManager registry && ReferenceEquals(registry.GetGuild(Id), this))
            {
                if (!QueuePrivacyChange(null, enabled)) throw new InvalidOperationException("Community privacy requires its registered residence.");
                return;
            }
            if (enabled)
                SetFlag(GuildFlag.CommunityPrivate);
            else
                RemoveFlag(GuildFlag.CommunityPrivate);

            SendGuildFlagUpdate();
        }
    }
}
