using System.Collections.Concurrent;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Character;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Guild;
using NexusForever.Game.Abstract.Housing;
using NexusForever.Game.Configuration.Model;
using NexusForever.Game.Guild;
using NexusForever.Game.Static.Guild;
using NexusForever.Game.Static.Housing;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Static;
using NexusForever.Shared;
using NexusForever.Shared.Configuration;
using NLog;

namespace NexusForever.Game.Housing
{
    public sealed partial class GlobalResidenceManager : IGlobalResidenceManager
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Id to be assigned to the next created residence.
        /// </summary>
        public ulong NextResidenceId { get { lock (saveGate) return nextResidenceId++; } }
        private ulong nextResidenceId;

        /// <summary>
        /// Id to be assigned to the next created residence.
        /// </summary>
        public ulong NextDecorId { get { lock (saveGate) return nextDecorId++; } }
        private ulong nextDecorId;

        private readonly ConcurrentDictionary<ulong, IResidence> residences = new();
        private readonly object saveGate = new();
        private readonly HashSet<ulong> savesInFlight = [];
        private readonly Dictionary<ulong, ulong> residenceOwnerCache = new();
        private readonly Dictionary<ulong, ulong> communityOwnerCache = new();
        private readonly Dictionary<string, ulong> residenceSearchCache = new(StringComparer.InvariantCultureIgnoreCase);
        private readonly Dictionary<string, ulong> communitySearchCache = new(StringComparer.InvariantCultureIgnoreCase);

        private readonly Dictionary<ulong, IPublicResidence> visitableResidences = new();
        private readonly Dictionary<ulong, IPublicCommunity> visitableCommunities = new();
        private sealed record CommittedCommunity(Community Owner, IResidence Root, string Name, ulong? Leader, bool Private);
        private readonly ConcurrentDictionary<Community, CommittedCommunity> committedCommunities = new();

        private double timeToSave;

        #region Dependency Injection

        private readonly ICharacterManager characterManager;
        private readonly IGlobalGuildManager globalGuildManager;
        private readonly IRealmContext realmContext;
        private readonly IDatabaseManager databaseManager;
        private readonly ISharedConfiguration sharedConfiguration;
        private readonly IGameTableManager gameTableManager;

        public GlobalResidenceManager(
            ICharacterManager characterManager = null,
            IGlobalGuildManager globalGuildManager = null,
            IRealmContext realmContext = null,
            IDatabaseManager databaseManager = null,
            ISharedConfiguration sharedConfiguration = null,
            IGameTableManager gameTableManager = null)
        {
            this.characterManager = characterManager;
            this.globalGuildManager = globalGuildManager;
            this.realmContext = realmContext;
            this.databaseManager = databaseManager;
            this.sharedConfiguration = sharedConfiguration;
            this.gameTableManager = gameTableManager;
            timeToSave = GetSaveDuration();
        }

        #endregion

        /// <summary>
        /// Initialise <see cref="IGlobalResidenceManager"/> and any related resources.
        /// </summary>
        public void Initialise()
        {
            CharacterDatabase database = GetCharacterDatabase();
            nextResidenceId = database.GetNextResidenceId() + 1ul;
            nextDecorId     = database.GetNextDecorId() + 1ul;

            InitialiseResidences();
        }

        private void InitialiseResidences()
        {
            foreach (ResidenceModel model in GetCharacterDatabase().GetResidences())
            {
                if (model.OwnerId.HasValue)
                {
                    ICharacter character = GetCharacterManager().GetCharacter(model.OwnerId.Value);
                    if (character == null)
                        throw new DatabaseDataException($"Character owner {model.OwnerId.Value} of residence {model.Id} is invalid!");

                    var residence = new Residence(model, this, realmContext?.RealmId ?? (ushort)0, gameTableManager);
                    StoreResidence(residence, character.Name);
                }
                else if (model.GuildOwnerId.HasValue)
                {
                    ICommunity community = GetGlobalGuildManager().GetGuild<ICommunity>(model.GuildOwnerId.Value);
                    if (community == null)
                        throw new DatabaseDataException($"Community owner {model.OwnerId.Value} of residence {model.Id} is invalid!");

                    var residence = new Residence(model, this, realmContext?.RealmId ?? (ushort)0, gameTableManager);
                    community.Residence = residence;

                    StoreCommunity(residence, community, loaded: true);
                }
            }

            // create links between parents and children
            // only residences with both a character and guild owner are children
            foreach (IResidence residence in residences.Values
                .Where(r => r.OwnerId.HasValue && r.GuildOwnerId.HasValue))
            {
                ICommunity community = GetGlobalGuildManager().GetGuild<ICommunity>(residence.GuildOwnerId.Value);
                if (community == null)
                    continue;

                IGuildMember member = community.GetMember(residence.OwnerId.Value);
                if (member == null)
                    throw new DatabaseDataException($"Residence {residence.Id} is a child of {community.Residence.Id} but character {residence.OwnerId.Value} but isn't a member of community {community.Id}!");

                // temporary child status comes from the member data
                int communityPlotReservation = member?.CommunityPlotReservation ?? -1; 
                community.Residence.AddChild(residence, communityPlotReservation == -1);
            }

            log.Info($"Loaded {residences.Count} housing residences!");
        }

        /// <summary>
        /// Shutdown <see cref="IGlobalResidenceManager"/> and any related resources.
        /// </summary>
        /// <remarks>
        /// This will force save all residences.
        /// </remarks>
        public void Shutdown()
        {
            log.Info("Shutting down residence manager...");

            SaveResidences();
        }

        public void Update(double lastTick)
        {
            PublishCommittedCommunities();
            SaveDonations();
            timeToSave -= lastTick;
            if (timeToSave <= 0d)
            {
                SaveResidences();
                timeToSave = GetSaveDuration();
            }
        }

        private void SaveResidences()
        {
            CharacterDatabase database = databaseManager?.GetDatabase<CharacterDatabase>();
            if (database == null)
                return;

            foreach (IResidence residence in residences.Values)
            {
                if (residence is Residence concreteResidence && (!concreteResidence.NeedsSave || concreteResidence.CommunitySavePending || concreteResidence.DonationSavePending || concreteResidence.PaidNameSavePending))
                    continue;

                lock (saveGate) if (!savesInFlight.Add(residence.Id)) continue;
                SaveResidence(database, residence).FireAndForgetAsync(ex => log.Error(ex, "Failed to save residence {0}.", residence.Id));
            }
        }

        private async Task SaveResidence(CharacterDatabase database, IResidence residence)
        {
            try { await database.Save(residence.Save); }
            finally { lock (saveGate) savesInFlight.Remove(residence.Id); }
        }
        private void BindPersistence(IResidence residence)
        {
            if (residence is not Residence concrete || databaseManager?.GetDatabase<CharacterDatabase>() is not {} database) return;
            using CharacterContext context = database.CreateContext();
            concrete.BindPersistence(context, () => residences.TryGetValue(residence.Id, out IResidence current) && ReferenceEquals(current, residence));
        }

        /// <summary>
        /// Create new <see cref="IResidence"/> for supplied <see cref="IPlayer"/>.
        /// </summary>
        public IResidence CreateResidence(IPlayer player)
        {
            HousingResult? blocked = RetailHousingRules.ValidateResidenceAccess(player);
            if (blocked != null)
                throw new HousingException("Player does not meet retail housing unlock requirements.");

            var residence = new Residence(player, this, gameTableManager);
            StoreResidence(residence, player.Name);

            log.Trace($"Created new residence {residence.Id} for player {player.Name}.");
            return residence;
        }

        private void StoreResidence(IResidence residence, string name)
        {
            if (residence is not Residence { HasPersistence: true }) BindPersistence(residence);
            if (!residences.TryAdd(residence.Id, residence)) throw new InvalidOperationException("Residence identity is already registered.");

            residenceOwnerCache.Add(residence.OwnerId.Value, residence.Id);
            residenceSearchCache.Add(name, residence.Id);

            if (residence.PrivacyLevel == ResidencePrivacyLevel.Public)
                RegisterResidenceVists(residence, name);
        }

        /// <summary>
        /// Create new <see cref="IResidence"/> for supplied <see cref="ICommunity"/>.
        /// </summary>
        public IResidence CreateCommunity(ICommunity community)
        {
            var residence = new Residence(community, this, realmContext?.RealmId ?? (ushort)0, gameTableManager);
            BindPersistence(residence);
            if (residence.HasPersistence && community is Community original) original.PrepareResidenceCreation(residence);
            StoreCommunity(residence, community);

            log.Trace($"Created new residence {residence.Id} for community {community.Name}.");
            return residence;
        }

        private void StoreCommunity(IResidence residence, ICommunity community, bool loaded = false)
        {
            if (residence is not Residence { HasPersistence: true }) BindPersistence(residence);
            if (!residences.TryAdd(residence.Id, residence)) throw new InvalidOperationException("Residence identity is already registered.");

            communityOwnerCache.Add(residence.GuildOwnerId.Value, residence.Id);
            bool settled = loaded && community is Community { PendingCreate: false, PendingDelete: false } actor
                && !actor.PaidNamePrepared && !actor.PrivacyPrepared && !actor.NameUnitActive
                && StringComparer.Ordinal.Equals(community.Name, residence.Name);
            if (settled)
            {
                communitySearchCache.Add(community.Name, residence.Id);
                if (community is Community confirmed) confirmed.ConfirmDirectoryName(residence, community.Name);
            }

            // Only loaded, confirmed actors may restore public directory entries.
            // A newly created community still belongs to its creation save unit.
            if (settled && community is Community owner
                && !owner.DirectoryPrivateConfirmed && !owner.HasFlags(GuildFlag.CommunityPrivate))
            {
                ulong? leader = owner.DirectoryLeaderConfirmed;
                ICharacter character = leader.HasValue ? GetCharacterManager().GetCharacter(leader.Value) : null;
                if (character != null) PublishCommunityPrivacy(residence, owner, false, character.Name, owner.Name);
                else log.Warn("Public community {0} has no current owner label; its visit entry remains unavailable.", owner.Id);
            }
        }

        internal void QueueConfirmedCommunityDirectory(IResidence residence, Community community, string name, ulong? leader, bool isPrivate)
            => committedCommunities[community] = new(community, residence, name, leader, isPrivate);

        private void RemoveCommittedCommunity(CommittedCommunity entry)
            => ((ICollection<KeyValuePair<Community, CommittedCommunity>>)committedCommunities).Remove(new(entry.Owner, entry));

        private void PublishCommittedCommunities()
        {
            foreach (var entry in committedCommunities.Values.ToArray())
            {
                if (entry.Owner.PendingDelete || !ReferenceEquals(GetResidence(entry.Root.Id), entry.Root)
                    || !ReferenceEquals(GetGlobalGuildManager().GetGuild<ICommunity>(entry.Owner.Id), entry.Owner)
                    || !ReferenceEquals(entry.Owner.Residence, entry.Root)) { RemoveCommittedCommunity(entry); continue; }
                if (entry.Owner.PaidNamePrepared || entry.Owner.PrivacyPrepared || entry.Owner.NameUnitActive)
                    continue;
                // Retain ownership while a later ordinary name intent is unsaved.
                // Its confirmed complete save replaces this captured directory record.
                if (entry.Owner.Name != entry.Name || entry.Root.Name != entry.Name) continue;
                try
                {
                    PublishCommunityRename(entry.Root, entry.Owner, entry.Name, entry.Leader, entry.Private);
                    RemoveCommittedCommunity(entry);
                }
                catch (Exception error)
                {
                    log.Error(error, "Confirmed community directory publication remains queued.");
                }
            }
        }

        internal void PublishCommunityRename(IResidence residence, Community community, string name, ulong? leader, bool? confirmedPrivate = null)
        {
            if (residence.OwnerId.HasValue || residence.GuildOwnerId != community.Id
                || !ReferenceEquals(GetResidence(residence.Id), residence)
                || !ReferenceEquals(GetGlobalGuildManager().GetGuild<ICommunity>(community.Id), community)) return;
            bool current = !community.PendingDelete && ReferenceEquals(community.Residence, residence)
                && ReferenceEquals(GetCommunityByOwner(community.Id), residence);
            if (current && (!StringComparer.Ordinal.Equals(community.Name, name) || !StringComparer.Ordinal.Equals(residence.Name, name)))
                throw new InvalidOperationException("Community visitor name requires both adopted names.");
            if (current && communitySearchCache.TryGetValue(name, out ulong other) && other != residence.Id)
            {
                if (GetCommunityByOwner(name) != null)
                    throw new InvalidOperationException("Community visitor name belongs to another residence.");
                communitySearchCache.Remove(name); // An inert stale alias cannot reserve a confirmed name.
            }

            foreach (string alias in communitySearchCache.Where(row => row.Value == residence.Id).Select(row => row.Key).ToArray())
                if (!current || !communitySearchCache.Comparer.Equals(alias, name)) communitySearchCache.Remove(alias);
            if (current) communitySearchCache[name] = residence.Id;

            bool isPrivate = confirmedPrivate == true || community.DirectoryPrivateConfirmed || community.HasFlags(GuildFlag.CommunityPrivate);
            if (visitableCommunities.TryGetValue(residence.Id, out IPublicCommunity entry) && entry.NeighbourhoodId == community.Id)
            {
                if (!current || isPrivate) visitableCommunities.Remove(residence.Id);
                else visitableCommunities[residence.Id] = new PublicCommunity { NeighbourhoodId = community.Id, Owner = entry.Owner, Name = name };
            }
            else if (entry == null && current && !isPrivate)
            {
                ICharacter character = leader.HasValue ? GetCharacterManager().GetCharacter(leader.Value) : null;
                if (character != null) PublishCommunityPrivacy(residence, community, false, character.Name, name);
                else log.Warn("Public community {0} has no confirmed owner label; its visit entry remains unavailable.", community.Id);
            }
        }

        /// <summary>
        /// Return existing <see cref="IResidence"/> by supplied residence id.
        /// </summary>
        public IResidence GetResidence(ulong residenceId)
        {
            return residences.TryGetValue(residenceId, out IResidence residence) ? residence : null;
        }

        /// <summary>
        /// Return existing <see cref="IResidence"/> by supplied owner name.
        /// </summary>
        public IResidence GetResidenceByOwner(string name)
        {
            return residenceSearchCache.TryGetValue(name, out ulong residenceId) ? GetResidence(residenceId) : null;
        }

        /// <summary>
        /// Return existing <see cref="IResidence"/> by supplied owner id.
        /// </summary>
        public IResidence GetResidenceByOwner(ulong characterId)
        {
            return residenceOwnerCache.TryGetValue(characterId, out ulong residenceId) ? GetResidence(residenceId) : null;
        }

        /// <summary>
        /// Return existing <see cref="IResidence"/> by supplied community name.
        /// </summary>
        public IResidence GetCommunityByOwner(string name)
        {
            if (!communitySearchCache.TryGetValue(name, out ulong residenceId) || GetResidence(residenceId) is not { OwnerId: null, GuildOwnerId: { } guild } root)
                return null;
            ICommunity owner = GetGlobalGuildManager().GetGuild<ICommunity>(guild);
            return owner != null && owner is not GuildBase { PendingCreate: true } and not GuildBase { PendingDelete: true }
                && ReferenceEquals(owner.Residence, root) && communitySearchCache.Comparer.Equals(owner.Name, name)
                && communitySearchCache.Comparer.Equals(root.Name, name) ? root : null;
        }

        /// <summary>
        /// return existing <see cref="IResidence"/> by supplied owner id.
        /// </summary>
        public IResidence GetCommunityByOwner(ulong communityId)
        {
            return communityOwnerCache.TryGetValue(communityId, out ulong residenceId) ? GetResidence(residenceId) : null;
        }

        /// <summary>
        /// Remove an existing <see cref="IResidence"/> by supplied character name.
        /// </summary>
        public void RemoveResidence(string name)
        {
            if (!residenceSearchCache.TryGetValue(name, out ulong residenceId))
                return;

            if (!residences.TryGetValue(residenceId, out IResidence residence))
                return;

            if (residence.Parent != null)
            {
                if (residence.Map != null)
                    residence.Map.RemoveChild(residence);
                else
                    residence.Parent.RemoveChild(residence);
            }
            else
                residence.Map?.Unload();

            if (residence.PrivacyLevel == ResidencePrivacyLevel.Public)
                DeregisterResidenceVists(residence.Id);

            residences.TryRemove(residence.Id, out _);
            residenceOwnerCache.Remove(residence.OwnerId.Value);
            residenceSearchCache.Remove(name);
        }

        public void RemoveCommunity(ICommunity community)
        {
            IResidence residence = community?.Residence;
            if (residence == null)
                return;
            if (!residences.TryGetValue(residence.Id, out IResidence current) || !ReferenceEquals(current, residence))
                return;

            DeregisterCommunityVists(residence.Id);
            if (residence is Residence concrete) concrete.RetainCommunityRetirement(community);
            residences.TryRemove(residence.Id, out _);
            if (communityOwnerCache.TryGetValue(community.Id, out ulong id) && id == residence.Id)
                communityOwnerCache.Remove(community.Id);
            foreach (string name in communitySearchCache.Where(p => p.Value == residence.Id).Select(p => p.Key).ToList())
                communitySearchCache.Remove(name);
        }

        public IResidenceEntrance GetResidenceEntrance(PropertyInfoId propertyInfoId)
        {
            HousingPropertyInfoEntry propertyEntry = gameTableManager.HousingPropertyInfo?.GetEntry((ulong)propertyInfoId);
            if (propertyEntry == null)
                throw new HousingException();

            WorldLocation2Entry locationEntry = gameTableManager.WorldLocation2?.GetEntry(propertyEntry.WorldLocation2Id);
            if (locationEntry == null)
                throw new HousingException();

            return new ResidenceEntrance(locationEntry, gameTableManager);
        }

        /// <summary>
        /// Register residence as visitable, this allows anyone to visit through the random property feature.
        /// </summary>
        public void RegisterResidenceVists(IResidence residence, string name)
        {
            visitableResidences.Add(residence.Id, new PublicResidence
            {
                ResidenceId = residence.Id,
                Owner       = name,
                Name        = residence.Name
            });
        }

        /// <summary>
        /// Register community as visitable, this allows anyone to visit through the random property feature.
        /// </summary>
        public void RegisterCommunityVisits(IResidence residence, ICommunity community, string name)
        {
            visitableCommunities.Add(residence.Id, new PublicCommunity
            {
                NeighbourhoodId = community.Id,
                Owner           = name,
                Name            = community.Name
            });
        }

        /// <summary>
        /// Deregister residence as visitable, this prevents anyone from visiting through the random property feature.
        /// </summary>
        public void DeregisterResidenceVists(ulong residenceId)
        {
            visitableResidences.Remove(residenceId);
        }

        /// <summary>
        /// Deregister community as visitable, this prevents anyone from visiting through the random property feature.
        /// </summary>
        public void DeregisterCommunityVists(ulong residenceId)
        {
            visitableCommunities.Remove(residenceId);
        }

        internal void PublishCommunityPrivacy(IResidence residence, Guild.Community community, bool isPrivate, string owner, string name)
        {
            if (!ReferenceEquals(GetResidence(residence.Id), residence)
                || !ReferenceEquals(GetGlobalGuildManager().GetGuild<ICommunity>(community.Id), community)) return;
            if (isPrivate)
            {
                // A replaced current-root reference must not leave this original
                // root publicly listed after its private flag has committed.
                if (visitableCommunities.TryGetValue(residence.Id, out var entry) && entry.NeighbourhoodId == community.Id)
                    visitableCommunities.Remove(residence.Id);
                return;
            }
            if (community.PendingDelete || !ReferenceEquals(community.Residence, residence)
                || !ReferenceEquals(GetCommunityByOwner(community.Id), residence)) return;
            visitableCommunities[residence.Id] = new PublicCommunity
            {
                NeighbourhoodId = community.Id, Name = name,
                Owner = owner ?? GetCharacterManager().GetCharacter(community.LeaderId!.Value)?.Name
                    ?? throw new InvalidOperationException("Community privacy lost its public owner label.")
            };
        }

        /// <summary>
        /// Return 50 random registered visitable residences.
        /// </summary>
        public IEnumerable<IPublicResidence> GetRandomVisitableResidences()
        {
            // unsure if this is how it was done on retail, might need to be tweaked
            return visitableResidences
                .Values
                .OrderBy(_ => Random.Shared.Next())
                .Take(50);
        }

        /// <summary>
        /// Return 50 random registered visitable communities.
        /// </summary>
        public IEnumerable<IPublicCommunity> GetRandomVisitableCommunities()
        {
            // unsure if this is how it was done on retail, might need to be tweaked
            return visitableCommunities
                .Values
                .OrderBy(_ => Random.Shared.Next())
                .Take(50);
        }

        private ICharacterManager GetCharacterManager()
        {
            return characterManager ?? throw new InvalidOperationException("GlobalResidenceManager requires an ICharacterManager.");
        }

        private IGlobalGuildManager GetGlobalGuildManager()
        {
            return globalGuildManager ?? throw new InvalidOperationException("GlobalResidenceManager requires an IGlobalGuildManager.");
        }

        private CharacterDatabase GetCharacterDatabase()
        {
            CharacterDatabase database = databaseManager?.GetDatabase<CharacterDatabase>();
            return database ?? throw new InvalidOperationException("CharacterDatabase is not available.");
        }

        private double GetSaveDuration()
        {
            return sharedConfiguration?.Get<WorldConfig>()?.ResidenceSaveIntervalSeconds ?? 60d;
        }
    }
}
