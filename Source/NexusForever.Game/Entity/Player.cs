using System.Numerics;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using NexusForever.Database;
using NexusForever.Database.Auth;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Challenges;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Abstract.Guild;
using NexusForever.Game.Abstract.Housing;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.Map.Lock;
using NexusForever.Game.Abstract.Map.Search;
using NexusForever.Game.Abstract.Matching.Match;
using NexusForever.Game.Abstract.Matching.Queue;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Abstract.Reputation;
using NexusForever.Game.Achievement;
using NexusForever.Game.Challenges;
using NexusForever.Game.Character;
using NexusForever.Game.Chat;
using NexusForever.Game.Configuration.Model;
using NexusForever.Game.Map.Search;
using NexusForever.Game.Guild;
using NexusForever.Game.Housing;
using NexusForever.Game.Loot;
using NexusForever.Game.Map;
using NexusForever.Game.Quest;
using NexusForever.Game.Reputation;
using NexusForever.Game.Spell;
using NexusForever.Game.Static;
using NexusForever.Game.Static.Achievement;
using NexusForever.Game.Static.Chat;
using NexusForever.Game.Static.Crafting;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Guild;
using NexusForever.Game.Static.Option;
using NexusForever.Game.Static.Pvp;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Static.RBAC;
using NexusForever.Game.Static.Reputation;
using NexusForever.Game.Static.Setting;
using NexusForever.Game.Static.Spell;
using NexusForever.Game.Pvp;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.Message;
using NexusForever.Network.Internal;
using NexusForever.Network.Internal.Message.Player;
using NexusForever.Network.Session;
using NexusForever.Network.World.Entity;
using NexusForever.Network.World.Entity.Model;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Abilities;
using NexusForever.Network.World.Message.Model.Chat;
using NexusForever.Network.World.Message.Model.Crafting;
using NexusForever.Network.World.Message.Model.Info;
using NexusForever.Network.World.Message.Model.Pregame;
using NexusForever.Network.World.Message.Model.Pvp;
using NexusForever.Network.World.Message.Model.Shared;
using NexusForever.Network.World.Message.Static;
using NexusForever.Script;
using NexusForever.Script.Template;
using NexusForever.Shared;
using NexusForever.Shared.Configuration;
using NexusForever.Shared.Game;
using NexusForever.Shared.Game.Events;
using NLog;
using Path = NexusForever.Game.Static.PlayerPath.Path;
using static NexusForever.Game.Static.Tutorial.StarterTutorialDefinition;

namespace NexusForever.Game.Entity
{
    public class Player : UnitEntity, IPlayer
    {
        private sealed class StarterTutorialEntitySearchCheck : ISearchCheck<IGridEntity>
        {
            public bool CheckEntity(IGridEntity entity)
            {
                return IsStarterTutorialManagedEntity(entity);
            }
        }

        /// <summary>
        /// Determines which fields need saving for <see cref="IPlayer"/> when being saved to the database.
        /// </summary>
        [Flags]
        public enum PlayerSaveMask
        {
            None        = 0x0000,
            Location    = 0x0001,
            Path        = 0x0002,
            Costume     = 0x0004,
            InputKeySet = 0x0008,
            Flags       = 0x0020,
            Innate      = 0x0080,
            Sex         = 0x0100,
            Race        = 0x0200,
            Options     = 0x0400,
        }

        private static readonly ILogger log = LogManager.GetCurrentClassLogger();

        private static double SaveDuration => SharedConfiguration.Instance.Get<WorldConfig>()?.PlayerSaveIntervalSeconds ?? 60d;

        private const ushort TutorialWorldId = 3460;
        private const float DefaultInteractionMaxRange = 5f;
        private const uint ChairBusyEffectId = uint.MaxValue;
        private const uint MaxTradeskillTalentTiers = 10u;
        private const uint TutorialHoverboardProjectorCreatureId = 73419u;
        private const uint TutorialHoverboardBarrierCreatureId = 73610u;
        private const uint TutorialHoverboardObjectiveRingCreatureId = 70939u;
        private const uint TutorialHoverboardDirectionArrowCreatureId = 72051u;
        private const uint TutorialHoverboardHoloringCreatureId = 73416u;
        private const uint TutorialHoverboardBoosterCreatureId = 73461u;
        private const uint TutorialHoverboardFinishLineCreatureId = 73595u;
        private const uint TutorialHoverboardSpinningHologramCreatureId = 73619u;
        private const uint TutorialHoverboardStartArrowCreatureId = 73707u;
        private const uint TutorialHoverboardPressurePlate00CreatureId = 74767u;
        private const uint TutorialHoverboardPressurePlate01CreatureId = 74768u;
        private const uint TutorialHoverboardPressurePlate02CreatureId = 74769u;
        private const uint TutorialCombatMineEasyCreatureId = 73463u;
        private const uint TutorialCombatMineMediumCreatureId = 73667u;
        private const uint TutorialCombatMineHardCreatureId = 73668u;
        private const uint TutorialCombatSpinningHologramCreatureId = 73736u;
        private const uint TutorialCombatProjectorCreatureId = 73735u;
        private const uint TutorialCombatFireHazardCreatureId = 75094u;
        private const uint TutorialCombatMortarHazardCreatureId = 75096u;
        private const uint TutorialHoverboardMountSpellId = 85562u;
        private const uint TutorialHoverboardSprintVisualSpellId = 82298u;
        private const uint TutorialHoverboardProjectorActivateSpellId = 86744u;
        private const float TutorialHoverboardFinishRecoveryPadding = 6f;
        private const float TutorialHoverboardFinishSnapTolerance = 1.25f;

        private static readonly uint[] tutorialManagedCreatureIds =
        [
            TutorialHoverboardProjectorCreatureId,
            TutorialHoverboardBarrierCreatureId,
            TutorialHoverboardObjectiveRingCreatureId,
            TutorialHoverboardDirectionArrowCreatureId,
            TutorialHoverboardHoloringCreatureId,
            TutorialHoverboardBoosterCreatureId,
            TutorialHoverboardFinishLineCreatureId,
            TutorialHoverboardSpinningHologramCreatureId,
            TutorialHoverboardStartArrowCreatureId,
            TutorialHoverboardPressurePlate00CreatureId,
            TutorialHoverboardPressurePlate01CreatureId,
            TutorialHoverboardPressurePlate02CreatureId,
            TutorialCombatMineEasyCreatureId,
            TutorialCombatMineMediumCreatureId,
            TutorialCombatMineHardCreatureId,
            TutorialCombatSpinningHologramCreatureId,
            TutorialCombatProjectorCreatureId,
            TutorialCombatFireHazardCreatureId,
            TutorialCombatMortarHazardCreatureId
        ];
        private static readonly StarterTutorialEntitySearchCheck starterTutorialEntitySearchCheck = new();

        private bool recoveringStarterTutorialQuestProgression;

        public override EntityType Type => EntityType.Player;

        public IAccount Account { get; private set; }

        public Abstract.Identity Identity { get; private set; }

        public ulong CharacterId { get => Identity.Id; }

        public string Name { get; private set; }

        public Sex Sex
        {
            get => sex;
            set
            {
                sex = value;
                saveMask |= PlayerSaveMask.Sex;

                SetVisualEmit(true);
            }
        }

        private Sex sex;

        public Race Race
        {
            get => race;
            set
            {
                race = value;
                saveMask |= PlayerSaveMask.Race;

                SetVisualEmit(true);
            }
        }

        private Race race;

        public Class Class { get; private set; }

        public CharacterFlag Flags
        {
            get => flags;
            set
            {
                flags = value;
                saveMask |= PlayerSaveMask.Flags;
            }
        }
        private CharacterFlag flags;

        public PvPFlag PvPFlag { get; private set; } = PvPFlag.Disabled;

        public Path Path
        {
            get => path;
            set
            {
                path = value;
                PathActivatedTime = DateTime.UtcNow;
                saveMask |= PlayerSaveMask.Path;
            }
        }
        private Path path;

        public DateTime PathActivatedTime { get; private set; }

        public InputSets InputKeySet
        {
            get => inputKeySet;
            set
            {
                inputKeySet = value;
                saveMask |= PlayerSaveMask.InputKeySet;
            }
        }
        private InputSets inputKeySet;

        public byte InnateIndex
        {
            get => innateIndex;
            set
            {
                innateIndex = value;
                saveMask |= PlayerSaveMask.Innate;
            }
        }
        private byte innateIndex;

        public CastingOptionFlags CastingOptions
        {
            get => castingOptions;
            set
            {
                castingOptions = value;
                saveMask |= PlayerSaveMask.Options;
            }
        }
        private CastingOptionFlags castingOptions;

        public uint MatchingEligibilityFlagMask { get; set; }

        public bool SharedChallengeEnabled
        {
            get => sharedChallengeEnabled;
            set
            {
                sharedChallengeEnabled = value;
                saveMask |= PlayerSaveMask.Options;
            }
        }
        private bool sharedChallengeEnabled;

        public bool DisableOtherPlayersCombatLogs
        {
            get => disableOtherPlayersCombatLogs;
            set
            {
                disableOtherPlayersCombatLogs = value;
                saveMask |= PlayerSaveMask.Options;
            }
        }
        private bool disableOtherPlayersCombatLogs;

        public CombatLogOptions CombatLogDisableFlags
        {
            get => combatLogDisableFlags;
            set
            {
                combatLogDisableFlags = value;
                saveMask |= PlayerSaveMask.Options;
            }
        }
        private CombatLogOptions combatLogDisableFlags;
        public AccountPresenceState PresenceState { get; set; }
        public string AwayAutoResponseMessage { get; set; }
        public string BusyAutoResponseMessage { get; set; }
        public WorldDifficulty InstanceDifficulty { get; set; }
        public uint InstancePrimeLevel { get; set; }
        public bool InstanceScalingEnabled { get; set; }
        public IReadOnlyList<uint> AttributePointAllocations => attributePointAllocations;
        private readonly uint[] attributePointAllocations = new uint[6];
        private readonly Dictionary<TradeskillType, TradeskillState> tradeskills = [];
        private readonly Dictionary<uint, SchematicState> schematics = [];

        public override uint Level
        {
            get => base.Level;
            set
            {
                base.Level = value;

                CalculateDefaultProperties();
                SetBaseCharacterProperties();
                if (!IsLoading)
                    SendAttributePoints();
            }
        }

        public DateTime CreateTime { get; private set; }
        public double TimePlayedTotal { get; private set; }
        public double TimePlayedLevel { get; private set; }
        public double TimePlayedSession { get; private set; }

        /// <summary>
        /// Guid of the <see cref="IWorldEntity"/> that currently being controlled by the <see cref="IPlayer"/>.
        /// </summary>
        public uint? ControlGuid { get; private set; }

        /// <summary>
        /// Guid of the <see cref="IPetEntity"/> currently summoned by the <see cref="IPlayer"/>.
        /// </summary>
        public uint? VanityPetGuid { get; set; }

        /// <summary>
        /// Id of the primary group that <see cref="IPlayer"/> is associated with.
        /// </summary>
        public ulong GroupAssociation { get; set; }

        public bool IsSitting => currentChairGuid != null;
        private uint? currentChairGuid;

        /// <summary>
        /// Returns if <see cref="IPlayer"/> has premium signature subscription.
        /// </summary>
        public bool SignatureEnabled => Account.RbacManager.HasPermission(Permission.Signature);

        public IGameSession Session { get; private set; }

        public bool IsOnline { get; private set; }

        /// <summary>
        /// Returns if <see cref="IPlayer"/>'s client is currently in a loading screen.
        /// </summary>
        public bool IsLoading { get; set; } = true;

        public IInventory Inventory { get; private set; }
        public ICurrencyManager CurrencyManager { get; }
        public IPathManager PathManager { get; private set; }
        public ITitleManager TitleManager { get; private set; }
        public ISpellManager SpellManager { get; private set; }
        public ICostumeManager CostumeManager { get; private set; }
        public IPetCustomisationManager PetCustomisationManager { get; private set; }
        public ICharacterKeybindingManager KeybindingManager { get; private set; }
        public IDatacubeManager DatacubeManager { get; private set; }
        public IGalacticArchiveManager GalacticArchiveManager { get; private set; }
        public IMailManager MailManager { get; private set; }
        public IZoneMapManager ZoneMapManager { get; private set; }
        public IChallengeManager ChallengeManager { get; private set; }
        public IQuestManager QuestManager { get; private set; }
        public ICharacterAchievementManager AchievementManager { get; private set; }
        public ISupplySatchelManager SupplySatchelManager { get; private set; }
        public IXpManager XpManager { get; private set; }
        public IReputationManager ReputationManager { get; private set; }
        public IGuildManager GuildManager { get; private set; }
        public IResidenceManager ResidenceManager { get; private set; }
        public ICinematicManager CinematicManager { get; private set; }
        public ICharacterEntitlementManager EntitlementManager { get; private set; }
        public ILogoutManager LogoutManager { get; private set; }
        public IAppearanceManager AppearanceManager { get; private set; }
        public IResurrectionManager ResurrectionManager { get; private set; }

        public IVendorInfo SelectedVendorInfo
        {
            get => selectedVendorInfo;
            set
            {
                selectedVendorInfo = value;
                selectedVendorGuid = value == null
                    ? null
                    : visibleEntities.Values
                        .OfType<INonPlayerEntity>()
                        .FirstOrDefault(v => ReferenceEquals(v.VendorInfo, value))?.Guid;
            }
        }

        private IVendorInfo selectedVendorInfo;
        private uint? selectedVendorGuid;

        public uint? StarterTutorialDepartureTerminalCreatureId { get; private set; }

        private bool forceSave;
        private bool saveInProgress;
        private bool saveRequestedDuringSave;
        private Action deferredSaveCallback;
        private UpdateTimer saveTimer = new(SaveDuration);
        private PlayerSaveMask saveMask;

        private Dictionary<Property, Dictionary<ItemSlot, /*value*/float>> itemProperties = new();

        private UpdateTimer relocationTimer = new(TimeSpan.FromSeconds(1));
        private UpdateTimer ghostSpawnTimer;

        #region Dependency Injection

        private readonly IInternalMessagePublisher messagePublisher;
        private readonly IEntityFactory entityFactory;
        private readonly IMatchingManager matchingManager;
        private readonly IMatchManager matchManager;

        public Player(
            IMovementManager movementManager,
            IInternalMessagePublisher messagePublisher,
            IEntityFactory entityFactory,
            IMatchingManager matchingManager,
            IMatchManager matchManager,
            ICurrencyManager currencyManager)
            : base(movementManager)
        {
            this.messagePublisher = messagePublisher;
            this.entityFactory    = entityFactory;
            this.matchingManager  = matchingManager;
            this.matchManager     = matchManager;

            // managers
            CurrencyManager = currencyManager;
        }

        #endregion

        /// <summary>
        /// Initialise <see cref="IPlayer"/> from supplied <see cref="IGameSession"/> and <see cref="CharacterModel"/>.
        /// </summary>
        public void Initialise(IGameSession session, IAccount account, CharacterModel model)
        {
            ActivationRange   = BaseMap.DefaultVisionRange;

            Session           = session;

            Account           = account;
            Identity          = new Abstract.Identity { Id = model.Id, RealmId = RealmContext.Instance.RealmId };
            Name              = model.Name;
            sex               = (Sex)model.Sex;
            race              = (Race)model.Race;
            Class             = (Class)model.Class;
            path              = (Path)model.ActivePath;
            PathActivatedTime = model.PathActivatedTimestamp;
            InputKeySet       = (InputSets)model.InputKeySet;
            Faction1          = (Faction)model.FactionId;
            Faction2          = (Faction)model.FactionId;
            innateIndex       = model.InnateIndex;
            flags             = (CharacterFlag)model.Flags;
            castingOptions           = (CastingOptionFlags)model.CastingOptions;
            sharedChallengeEnabled   = model.SharedChallengeEnabled;
            disableOtherPlayersCombatLogs = model.DisableOtherPlayersCombatLogs;
            combatLogDisableFlags    = (CombatLogOptions)model.CombatLogDisableFlags;

            CreateTime        = model.CreateTime;
            TimePlayedTotal   = model.TimePlayedTotal;
            TimePlayedLevel   = model.TimePlayedLevel;

            foreach (CharacterTradeskillModel tradeskillModel in model.Tradeskill)
            {
                var tradeskillState = TradeskillState.FromModel(tradeskillModel);
                tradeskills[(TradeskillType)tradeskillModel.TradeskillId] = tradeskillState;
            }

            foreach (CharacterSchematicModel schematicModel in model.Schematic)
            {
                if (GameTableManager.Instance.TradeskillSchematic2.GetEntry(schematicModel.TradeskillSchematic2Id) == null)
                    continue;

                schematics[schematicModel.TradeskillSchematic2Id] = SchematicState.FromModel(schematicModel);
            }

            foreach (CharacterStatModel statModel in model.Stat)
            {
                var statValue = new StatValue(statModel);
                stats.Add((Stat)statModel.Stat, statValue);
            }

            //SetStat(Stat.Health, 1);
            SetStat(Stat.Sheathed, 1u);
            // temp
            SetStat(Stat.Dash, 200F);
            // sprint
            SetStat(Stat.Resource0, 500f);

            CalculateDefaultProperties();
            SetBaseCharacterProperties();

            scriptCollection = ScriptManager.Instance.InitialiseEntityScripts<IPlayer>(this);

            // managers
            EntitlementManager      = new CharacterEntitlementManager(this, model);
            Account.RewardPropertyManager.Initialise(this);

            CostumeManager          = new CostumeManager(this, model);
            Inventory               = new Inventory(this, model);
            CurrencyManager.Initialise(this, model);
            PathManager             = new PathManager(this, model);
            TitleManager            = new TitleManager(this, model);
            SpellManager            = new SpellManager(this, model);
            PetCustomisationManager = new PetCustomisationManager(this, model);
            KeybindingManager       = new CharacterKeybindingManager(this, model);
            DatacubeManager         = new DatacubeManager(this, model);
            GalacticArchiveManager  = new GalacticArchiveManager(this, model);
            MailManager             = new MailManager(this, model);
            ZoneMapManager          = new ZoneMapManager(this, model);
            ChallengeManager        = new ChallengeManager(this, model);
            QuestManager            = new QuestManager(this, model);
            AchievementManager      = new CharacterAchievementManager(this, model);
            SupplySatchelManager    = new SupplySatchelManager(this, model);
            XpManager               = new XpManager(this, model);
            ReputationManager       = new ReputationManager(this, model);
            GuildManager            = new GuildManager(this, model);
            ResidenceManager        = new ResidenceManager(this);
            CinematicManager        = new CinematicManager(this);

            LogoutManager           = new LogoutManager(this);
            LogoutManager.OnTimerFinished += Logout;

            AppearanceManager       = new AppearanceManager(this, model);
            ResurrectionManager     = new ResurrectionManager(this);

            // do dependant stat balance after all stats and properties have been set
            SetDependantStatBalance(true);
            foreach (IPropertyValue property in GetProperties())
                DependantStatBalance(property);

            PlayerManager.Instance.AddPlayer(this);
        }

        private void SetBaseCharacterProperties()
        {
            var baseProperties  = CharacterManager.Instance.GetCharacterBaseProperties();
            var classProperties = CharacterManager.Instance.GetCharacterClassBaseProperties(Class);

            foreach (IPropertyModifier propertyValue in baseProperties.Concat(classProperties))
                SetBaseProperty(propertyValue.Property, propertyValue.GetValue(Level));
        }

        public override void Update(double lastTick)
        {
            LogoutManager.Update(lastTick);

            // don't process world updates while logout is finalising
            if (LogoutManager.State is LogoutState.Logout or LogoutState.Finished)
                return;

            base.Update(lastTick);

            TitleManager.Update(lastTick);
            SpellManager.Update(lastTick);
            CostumeManager.Update(lastTick);
            Inventory.Update(lastTick);
            QuestManager.Update(lastTick);
            ChallengeManager.Update(lastTick);
            ResurrectionManager.Update(lastTick);
            UpdatePendingGhostSpawn(lastTick);

            relocationTimer.Update(lastTick);
            if (relocationTimer.HasElapsed)
            {
                messagePublisher.PublishAsync(new PlayerPositionUpdatedMessage
                {
                    Identity = Identity.ToInternalIdentity(),
                    Position = new Network.Internal.Message.Shared.Position
                    {
                        X = Position.X,
                        Y = Position.Y,
                        Z = Position.Z
                    }
                }).FireAndForgetAsync();

                relocationTimer.Reset(false);
            }

            saveTimer.Update(lastTick);
            if ((saveTimer.HasElapsed || forceSave) && !saveInProgress)
            {
                forceSave = false;

                double timeSinceLastSave = GetTimeSinceLastSave();
                TimePlayedSession += timeSinceLastSave;
                TimePlayedLevel += timeSinceLastSave;
                TimePlayedTotal += timeSinceLastSave;

                Save();
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            QuestManager.Dispose();
        }

        /// <summary>
        /// Save <see cref="IPlayer"/> to database, invoke supplied <see cref="Action"/> once save is complete.
        /// </summary>
        /// <remarks>
        /// This is a delayed save, <see cref="AuthContext"/> changes are saved first followed by <see cref="CharacterContext"/> changes.
        /// Packets for session will not be handled until save is complete.
        /// </remarks>
        public void Save(Action callback = null)
        {
            if (saveInProgress)
            {
                saveRequestedDuringSave = true;
                deferredSaveCallback += callback;
                return;
            }

            StartSave(callback);
        }

        /// <summary>
        /// Save <see cref="IPlayer"/> to database.
        /// </summary>
        /// <remarks>
        /// This is an instant save, <see cref="AuthContext"/> changes are saved first followed by <see cref="CharacterContext"/> changes.
        /// Returns a task that completes once both database saves have finished.
        /// </remarks>
        public async Task SaveDirect()
        {
            await DatabaseManager.Instance.GetDatabase<AuthDatabase>().Save(Save);
            await DatabaseManager.Instance.GetDatabase<CharacterDatabase>().Save(Save);
        }

        public void RequestSave()
        {
            if (saveInProgress)
            {
                saveRequestedDuringSave = true;
                return;
            }

            forceSave = true;
        }

        private void StartSave(Action callback = null)
        {
            saveInProgress = true;

            var authSaveTask = DatabaseManager.Instance.GetDatabase<AuthDatabase>().Save(Save);
            Session.Events.EnqueueEvent(new TaskEvent(authSaveTask,
            () =>
            {
                if (authSaveTask.IsFaulted)
                    log.Error(authSaveTask.Exception, $"Failed to save auth data for character {CharacterId}.");

                var characterSaveTask = DatabaseManager.Instance.GetDatabase<CharacterDatabase>().Save(Save);
                Session.Events.EnqueueEvent(new TaskEvent(characterSaveTask,
                () =>
                {
                    if (characterSaveTask.IsFaulted)
                        log.Error(characterSaveTask.Exception, $"Failed to save character data for character {CharacterId}.");

                    Session.CanProcessIncomingPackets = true;
                    saveTimer.Resume();
                    saveInProgress = false;

                    if (saveRequestedDuringSave)
                    {
                        saveRequestedDuringSave = false;
                        Action chainedCallback = callback;
                        chainedCallback += deferredSaveCallback;
                        deferredSaveCallback = null;
                        StartSave(chainedCallback);
                        return;
                    }

                    callback?.Invoke();
                }));
            }));

            saveTimer.Reset(false);

            // prevent packets from being processed until asynchronous player save task is complete
            Session.CanProcessIncomingPackets = false;
        }

        /// <summary>
        /// Save database changes for <see cref="Player"/> to <see cref="AuthContext"/>.
        /// </summary>
        public void Save(AuthContext context)
        {
            Account.Save(context);
        }

        /// <summary>
        /// Save database changes for <see cref="Player"/> to <see cref="CharacterContext"/>.
        /// </summary>
        public void Save(CharacterContext context)
        {
            var model = new CharacterModel
            {
                Id = CharacterId
            };

            EntityEntry<CharacterModel> entity = context.Attach(model);

            if (saveMask != PlayerSaveMask.None)
            {
                if ((saveMask & PlayerSaveMask.Location) != 0)
                {
                    model.LocationX = Position.X;
                    entity.Property(p => p.LocationX).IsModified = true;

                    model.LocationY = Position.Y;
                    entity.Property(p => p.LocationY).IsModified = true;

                    model.LocationZ = Position.Z;
                    entity.Property(p => p.LocationZ).IsModified = true;

                    model.RotationX = Rotation.X;
                    entity.Property(p => p.RotationX).IsModified = true;

                    model.RotationY = Rotation.Y;
                    entity.Property(p => p.RotationY).IsModified = true;

                    model.RotationZ = Rotation.Z;
                    entity.Property(p => p.RotationZ).IsModified = true;

                    model.WorldId = (ushort)Map.Entry.Id;
                    entity.Property(p => p.WorldId).IsModified = true;

                    model.WorldZoneId = (ushort)Zone.Id;
                    entity.Property(p => p.WorldZoneId).IsModified = true;
                }

                if ((saveMask & PlayerSaveMask.Path) != 0)
                {
                    model.ActivePath = (uint)Path;
                    entity.Property(p => p.ActivePath).IsModified = true;
                    model.PathActivatedTimestamp = PathActivatedTime;
                    entity.Property(p => p.PathActivatedTimestamp).IsModified = true;
                }

                if ((saveMask & PlayerSaveMask.InputKeySet) != 0)
                {
                    model.InputKeySet = (sbyte)InputKeySet;
                    entity.Property(p => p.InputKeySet).IsModified = true;
                }

                if ((saveMask & PlayerSaveMask.Flags) != 0)
                {
                    model.Flags = (uint)Flags;
                    entity.Property(p => p.Flags).IsModified = true;
                }

                if ((saveMask & PlayerSaveMask.Innate) != 0)
                {
                    model.InnateIndex = InnateIndex;
                    entity.Property(p => p.InnateIndex).IsModified = true;
                }

                if ((saveMask & PlayerSaveMask.Sex) != 0)
                {
                    model.Sex = (byte)Sex;
                    entity.Property(p => p.Sex).IsModified = true;
                }

                if ((saveMask & PlayerSaveMask.Race) != 0)
                {
                    model.Race = (byte)Race;
                    entity.Property(p => p.Race).IsModified = true;
                }

                if ((saveMask & PlayerSaveMask.Options) != 0)
                {
                    model.CastingOptions = (byte)CastingOptions;
                    entity.Property(p => p.CastingOptions).IsModified = true;

                    model.SharedChallengeEnabled = SharedChallengeEnabled;
                    entity.Property(p => p.SharedChallengeEnabled).IsModified = true;

                    model.DisableOtherPlayersCombatLogs = DisableOtherPlayersCombatLogs;
                    entity.Property(p => p.DisableOtherPlayersCombatLogs).IsModified = true;

                    model.CombatLogDisableFlags = (ushort)CombatLogDisableFlags;
                    entity.Property(p => p.CombatLogDisableFlags).IsModified = true;
                }

                saveMask = PlayerSaveMask.None;
            }

            model.TimePlayedLevel = (uint)TimePlayedLevel;
            entity.Property(p => p.TimePlayedLevel).IsModified = true;
            model.TimePlayedTotal = (uint)TimePlayedTotal;
            entity.Property(p => p.TimePlayedTotal).IsModified = true;

            model.IsOnline = IsOnline;
            entity.Property(p => p.IsOnline).IsModified = true;

            if (!IsOnline)
            {
                model.LastOnline = DateTime.UtcNow;
                entity.Property(p => p.LastOnline).IsModified = true;
            }

            foreach (IStatValue stat in stats.Values)
                stat.SaveCharacter(CharacterId, context);

            Inventory.Save(context);
            CurrencyManager.Save(context);
            PathManager.Save(context);
            TitleManager.Save(context);
            CostumeManager.Save(context);
            PetCustomisationManager.Save(context);
            KeybindingManager.Save(context);
            SpellManager.Save(context);
            DatacubeManager.Save(context);
            GalacticArchiveManager.Save(context);
            MailManager.Save(context);
            ZoneMapManager.Save(context);
            QuestManager.Save(context);
            AchievementManager.Save(context);
            if (ChallengeManager is ChallengeManager challengeManager)
                challengeManager.Save(context);
            SupplySatchelManager.Save(context);
            SaveTradeskills(context);
            SaveSchematics(context);
            XpManager.Save(context);
            ReputationManager.Save(context);
            GuildManager.Save(context);
            EntitlementManager.Save(context);
            AppearanceManager.Save(context);
        }

        private void SaveTradeskills(CharacterContext context)
        {
            foreach (TradeskillState tradeskill in tradeskills.Values)
            {
                CharacterTradeskillModel model = tradeskill.BuildModel();
                if (tradeskill.PendingCreate)
                {
                    context.Add(model);
                }
                else if (tradeskill.Dirty)
                {
                    EntityEntry<CharacterTradeskillModel> entity = context.Attach(model);
                    entity.Property(p => p.TradeskillXp).IsModified = true;
                    entity.Property(p => p.IsActive).IsModified = true;
                    entity.Property(p => p.PropertyProficiencyFlags).IsModified = true;
                    entity.Property(p => p.TalentPoints).IsModified = true;
                    entity.Property(p => p.TalentTier00).IsModified = true;
                    entity.Property(p => p.TalentTier01).IsModified = true;
                    entity.Property(p => p.TalentTier02).IsModified = true;
                    entity.Property(p => p.TalentTier03).IsModified = true;
                    entity.Property(p => p.TalentTier04).IsModified = true;
                    entity.Property(p => p.TalentTier05).IsModified = true;
                    entity.Property(p => p.TalentTier06).IsModified = true;
                    entity.Property(p => p.TalentTier07).IsModified = true;
                    entity.Property(p => p.TalentTier08).IsModified = true;
                    entity.Property(p => p.TalentTier09).IsModified = true;
                }

                tradeskill.ClearSaveState();
            }
        }

        private void SaveSchematics(CharacterContext context)
        {
            foreach (SchematicState schematic in schematics.Values)
            {
                CharacterSchematicModel model = schematic.BuildModel();
                if (schematic.PendingCreate)
                    context.Add(model);
                else if (schematic.Dirty)
                {
                    EntityEntry<CharacterSchematicModel> entity = context.Attach(model);
                    entity.Property(p => p.Discovered).IsModified = true;
                    entity.Property(p => p.DiscoveryCoordinateX).IsModified = true;
                    entity.Property(p => p.DiscoveryCoordinateY).IsModified = true;
                }

                schematic.ClearSaveState();
            }
        }

        protected override IEntityModel BuildEntityModel()
        {
            return new PlayerEntityModel
            {
                Id        = CharacterId,
                RealmId   = RealmContext.Instance.RealmId,
                Name      = Name,
                Race      = Race,
                Class     = Class,
                Sex       = Sex,
                Bones     = AppearanceManager.GetBones()
                    .Select(b => b.BoneValue)
                    .ToList(),
                Title     = TitleManager.ActiveTitleId,
                GuildIds  = GuildManager
                    .Select(g => g.Id)
                    .ToList(),
                GuildName = GuildManager.GuildAffiliation?.Name,
                GuildType = GuildManager.GuildAffiliation?.Type ?? GuildType.None,
                PvPFlag   = PvPFlag,

                // We use Group 1 as the "dominant group"
                GroupId   = GroupAssociation
            };
        }

        public override void OnAddToMap(IBaseMap map, uint guid, Vector3 vector)
        {
            IsLoading = true;

            Session.EnqueueMessageEncrypted(new ServerChangeWorld
            {
                WorldId  = (ushort)map.Entry.Id,
                Position = new Position(vector),
                Yaw      = Rotation.X
            });

            // this must come before OnAddToMap
            // the client UI initialises the Holomark checkboxes during OnDocumentReady
            SendCharacterFlagsUpdated();

            base.OnAddToMap(map, guid, vector);

            // resummon vanity pet if it existed before teleport
            if (pendingTeleport?.VanityPetId != null)
            {
                var pet = entityFactory.CreateEntity<IPetEntity>();
                pet.Initialise(this, pendingTeleport.VanityPetId.Value);

                var position = new MapPosition
                {
                    Position = Position
                };

                if (map.CanEnter(pet, position))
                    map.EnqueueAdd(pet, position);
            }

            SendPacketsAfterAddToMap();

            if (!IsAlive)
                OnDeath();

            if (PreviousMap == null)
                OnLogin();

            messagePublisher.PublishAsync(new PlayerWorldUpdatedMessage
            {
                Identity    = Identity.ToInternalIdentity(),
                WorldId     = Map.Entry.Id
            }).FireAndForgetAsync();
        }

        public override void OnRelocate(Vector3 vector)
        {
            base.OnRelocate(vector);
            saveMask |= PlayerSaveMask.Location;

            ZoneMapManager.OnRelocate(vector);
            ClearSelectedVendorIfOutOfRange();
            TryRecoverStarterTutorialOnRelocate();

            if (!relocationTimer.IsTicking)
                relocationTimer.Resume();
        }

        private void ClearSelectedVendorIfOutOfRange()
        {
            if (selectedVendorInfo == null)
                return;

            if (!selectedVendorGuid.HasValue)
            {
                SelectedVendorInfo = null;
                return;
            }

            INonPlayerEntity vendor = GetVisible<INonPlayerEntity>(selectedVendorGuid.Value);
            if (vendor == null || !ReferenceEquals(vendor.VendorInfo, selectedVendorInfo))
            {
                SelectedVendorInfo = null;
                return;
            }

            Creature2Entry creatureEntry = GameTableManager.Instance.Creature2.GetEntry(vendor.CreatureId);
            float maxRange = creatureEntry?.ActivateSpellMaxRange > 0f
                ? creatureEntry.ActivateSpellMaxRange
                : DefaultInteractionMaxRange;

            if (Vector3.DistanceSquared(Position, vendor.Position) > maxRange * maxRange)
                SelectedVendorInfo = null;
        }

        protected override void OnZoneUpdate()
        {
            if (Zone != null)
            {
                TextTable tt = GameTableManager.Instance.GetTextTable(Language.English);
                if (tt != null)
                {
                    GlobalChatManager.Instance.SendMessage(Session, $"New Zone: ({Zone.Id}){tt.GetEntry(Zone.LocalizedTextIdName)}");
                }

                uint tutorialId = AssetManager.Instance.GetTutorialIdForZone(Zone.Id);
                if (tutorialId > 0)
                {
                    Session.EnqueueMessageEncrypted(new ServerTutorial
                    {
                        TutorialId = tutorialId
                    });
                }

                AchievementManager.CheckAchievements(this, AchievementType.EnterWorldZone, Zone.Id);
                QuestManager.ObjectiveUpdate(QuestObjectiveType.EnterZone, Zone.Id, 1);
            }

            ZoneMapManager.OnZoneUpdate();

            messagePublisher.PublishAsync(new PlayerWorldZoneUpdatedMessage
            {
                Identity    = Identity.ToInternalIdentity(),
                WorldZoneId = (ushort)Zone?.Id,
            }).FireAndForgetAsync();
        }

        private void SendPacketsAfterAddToMap()
        {
            DateTime start = DateTime.UtcNow;

            SendInGameTime();
            PathManager.SendInitialPackets();
            BuybackManager.Instance.SendBuybackItems(this);

            ResidenceManager.SendHousingBasics();
            ResidenceManager.SendHousingNeighbors();
            Session.EnqueueMessageEncrypted(new ServerInstanceSettings
            {
                Difficulty                     = InstanceDifficulty,
                PrimeLevel                     = InstancePrimeLevel,
                Flags                          = InstanceScalingEnabled ? ServerInstanceSettings.WorldSetting.WorldForcesLevelScaling : 0,
                ClientEntitySendUpdateInterval = 125
            });
            SendAttributePoints();

            SetControl(this);

            CostumeManager.SendInitialPackets();
            Account.CostumeManager.SendInitialPackets();

            var playerCreate = new ServerPlayerCreate
            {
                ItemProficiencies = GetItemProficiencies(),
                FactionData       = new ServerPlayerCreate.Faction
                {
                    FactionId          = Faction1, // This does not do anything for the player's "main" faction. Exiles/Dominion
                    FactionReputations = ReputationManager
                        .Select(r => new ServerPlayerCreate.Faction.FactionReputation
                        {
                            FactionId = r.Id,
                            Value     = r.Amount
                        })
                        .ToList()
                },
                ActiveCostumeIndex    = CostumeManager.CostumeIndex ?? -1,
                InputKeySet           = (uint)InputKeySet,
                CharacterEntitlements = EntitlementManager
                    .Select(e => new ServerPlayerCreate.CharacterEntitlement
                    {
                        Entitlement = e.Type,
                        Count       = e.Amount
                    })
                    .ToList(),
                TradeskillMaterials          = SupplySatchelManager.BuildNetworkPacket(),
                Xp                           = XpManager.TotalXp,
                RestBonusXp                  = XpManager.RestBonusXp,
                MatchingEligibilityFlagMask  = MatchingEligibilityFlagMask
            };

            foreach (ICurrency currency in CurrencyManager)
                playerCreate.Money[(byte)currency.Id - 1] = currency.Amount;

            foreach (IItem item in Inventory
                .Where(b => b.Location != InventoryLocation.Ability)
                .SelectMany(i => i))
            {
                playerCreate.Inventory.Add(new InventoryItem
                {
                    Item   = item.Build(),
                    Reason = ItemUpdateReason.NoReason
                });
            }

            playerCreate.SpecIndex = SpellManager.ActiveActionSet;
            Session.EnqueueMessageEncrypted(playerCreate);

            TitleManager.SendTitles();
            SpellManager.SendInitialPackets();
            PetCustomisationManager.SendInitialPackets();
            DatacubeManager.SendInitialPackets();
            GalacticArchiveManager.SendInitialPackets();
            MailManager.SendInitialPackets();
            ZoneMapManager.SendInitialPackets();
            ChallengeManager.SendInitialPackets();
            Account.CurrencyManager.SendInitialPackets();
            Account.InventoryManager.SendInitialPackets();
            Account.GenericUnlockManager.SendCharacterUnlockSync();
            SendTradeskillInitialPackets();
            AchievementManager.SendInitialPackets(null);
            Account.RewardPropertyManager.SendInitialPackets();
            ResurrectionManager.SendInitialPackets();

            Session.EnqueueMessageEncrypted(new ServerStanceChanged
            {
                InnateIndex = InnateIndex
            });

            Session.EnqueueMessageEncrypted(new ServerPhaseVisibilityWorldLocation
            {
                PhasesIPerceive = 1,
                PhasesThatPerceiveMe = 1
            });

            log.Trace($"Player {Name} took {(DateTime.UtcNow - start).TotalMilliseconds}ms to send packets after add to map.");
        }

        public ItemProficiency GetItemProficiencies()
        {
            ClassEntry classEntry = GameTableManager.Instance.Class.GetEntry((ulong)Class);
            return (ItemProficiency)classEntry.StartingItemProficiencies;
        }

        public override void OnRemoveFromMap()
        {
            DestroyDependents();
            base.OnRemoveFromMap();
        }

        public override bool CanSeeEntity(IGridEntity entity)
        {
            if (ShouldForceStarterTutorialEntityVisibility(entity))
                return true;

            return base.CanSeeEntity(entity) && !ShouldHideStarterTutorialEntity(entity);
        }

        public override void AddVisible(IGridEntity entity)
        {
            bool wasVisible = visibleEntities.ContainsKey(entity.Guid);
            base.AddVisible(entity);

            if (wasVisible || !visibleEntities.ContainsKey(entity.Guid))
                return;

            if (entity is IWorldEntity worldEntity)
            {
                foreach (IWritable auxiliary in worldEntity.BuildEntityCreateAuxPackets())
                    Session.EnqueueMessageEncrypted(auxiliary);

                Session.EnqueueMessageEncrypted(worldEntity.BuildCreatePacket(IsLoading));
                GlobalLootManager.Instance.SendLootNotifyForVisibleOwner(this, worldEntity);
            }

            if (entity is IPlayer playerEntity)
                Session.EnqueueMessageEncrypted(new ServerSetUnitPathType
                {
                    UnitId = playerEntity.Guid,
                    Path = playerEntity.Path
                });

            if (entity == this)
            {
                Session.EnqueueMessageEncrypted(new ServerPlayerChanged
                {
                    Guid = entity.Guid,
                    Unknown1 = 1
                });
            }

            if (entity is IUnitEntity unitEntity && unitEntity.InCombat)
            {
                Session.EnqueueMessageEncrypted(new ServerUnitEnteredCombat
                {
                    UnitId = unitEntity.Guid,
                    InCombat = unitEntity.InCombat
                });
            }

            if (entity is IWorldEntity busyEntity && busyEntity.IsBusy)
            {
                Session.EnqueueMessageEncrypted(new ServerUnitInUse
                {
                    UnitId = busyEntity.Guid,
                    InUse  = true
                });
            }
        }

        public override void RemoveVisible(IGridEntity entity)
        {
            if (ShouldForceStarterTutorialEntityVisibility(entity))
                return;

            bool wasVisible = visibleEntities.ContainsKey(entity.Guid);
            base.RemoveVisible(entity);

            if (!wasVisible || visibleEntities.ContainsKey(entity.Guid))
                return;

            if (selectedVendorGuid == entity.Guid)
                SelectedVendorInfo = null;

            if (entity is IWorldEntity && entity != this)
            {
                Session.EnqueueMessageEncrypted(new ServerEntityDestroy
                {
                    Guid = entity.Guid,
                    Flag = true
                });
            }
        }

        protected override void AddVisible(uint gridX, uint gridZ)
        {
            base.AddVisible(gridX, gridZ);
            Map.GridAddVisiblePlayer(gridX, gridZ);
        }

        protected override void RemoveVisible(uint gridX, uint gridZ)
        {
            base.RemoveVisible(gridX, gridZ);
            Map.GridRemoveVisiblePlayer(gridX, gridZ);
        }

        /// <summary>
        /// Set the <see cref="IWorldEntity"/> that currently being controlled by the <see cref="IPlayer"/>.
        /// </summary>
        public void SetControl(IWorldEntity entity)
        {
            if (ControlGuid != null)
            {
                IWorldEntity control = Map.GetEntity<IWorldEntity>(ControlGuid.Value);
                if (control != null)
                    control.ControllerGuid = null;
            }

            ControlGuid = entity?.Guid;

            if (ControlGuid != null)
            {
                entity.ControllerGuid = Guid;

                Session.EnqueueMessageEncrypted(new ServerMovementControl
                {
                    Ticket    = 1,
                    Immediate = true,
                    UnitId    = ControlGuid.Value
                });
            }
            else
                Session.EnqueueMessageEncrypted(new ServerMovementControlRemove());
        }

        private void Logout()
        {
            OnLogout();
            Cleanup();
        }

        private void Cleanup()
        {
            log.Trace($"Cleanup for character {Name}({CharacterId})...");

            PlayerManager.Instance.RemovePlayer(this);
            CleanupManager.Instance.AddPlayer(this);

            log.Trace($"Waiting to cleanup character {Name}({CharacterId})...");

            Session.Events.EnqueueEvent(new TimeoutPredicateEvent(TimeSpan.FromSeconds(15), CanCleanup,
                () =>
            {
                void CompleteCleanup()
                {
                    CleanupManager.Instance.RemovePlayer(this);
                    log.Trace($"Cleanup for character {Name}({CharacterId}) has completed.");

                    LogoutManager.State = LogoutState.Finished;
                }

                log.Trace($"Cleanup for character {Name}({CharacterId}) has started...");

                try
                {
                    Save(() =>
                    {
                        try
                        {
                            if (Map != null)
                                RemoveFromMap();

                            messagePublisher.PublishAsync(new PlayerLoggedOutMessage
                            {
                                Identity = Identity.ToInternalIdentity()
                            }).FireAndForgetAsync();

                            Dispose();
                        }
                        finally
                        {
                            CompleteCleanup();
                        }
                    });
                }
                catch
                {
                    CompleteCleanup();
                    throw;
                }
            }));
        }

        private bool CanCleanup()
        {
            return pendingTeleport == null;
        }

        private void OnLogin()
        {
            scriptCollection.Invoke<IPlayerScript>(s => s.OnLogin());

            string motd = RealmContext.Instance.Motd;
            if (motd?.Length > 0)
                GlobalChatManager.Instance.SendMessage(Session, motd, "MOTD", ChatChannelType.Realm);

            GuildManager.OnLogin();

            ShutdownManager.Instance.OnLogin(this);

            matchingManager.OnLogin(this);
            matchManager.OnLogin(this);

            IsOnline = true;
            forceSave = true;

            messagePublisher.PublishAsync(new PlayerLoggedInMessage
            {
                Identity  = Identity.ToInternalIdentity(),
                AccountId = Account.Id
            }).FireAndForgetAsync();
        }

        private void OnLogout()
        {
            GuildManager.OnLogout();

            matchingManager.OnLogout(this);
            matchManager.OnLogout(this);

            IsOnline = false;

            scriptCollection.Invoke<IPlayerScript>(s => s.OnLogout());
        }

        /// <summary>
        /// Returns if <see cref="IPlayer"/> can teleport.
        /// </summary>
        public bool CanTeleport() => pendingTeleport == null;
        private PendingTeleport pendingTeleport;

        /// <summary>
        /// Teleport <see cref="IPlayer"/> to supplied location.
        /// </summary>
        public void TeleportTo(ushort worldId, float x, float y, float z, IMapLock mapLock = null, TeleportReason reason = TeleportReason.Relocate)
        {
            WorldEntry entry = GameTableManager.Instance.World.GetEntry(worldId);
            if (entry == null)
                throw new ArgumentException($"{worldId} is not a valid world id!");

            TeleportTo(entry, new Vector3(x, y, z), mapLock, reason);
        }

        /// <summary>
        /// Teleport <see cref="IPlayer"/> to supplied location.
        /// </summary>
        public void TeleportTo(WorldEntry entry, Vector3 position, IMapLock mapLock = null, TeleportReason reason = TeleportReason.Relocate)
        {
            TeleportTo(new MapPosition
            {
                Info = new MapInfo
                {
                    Entry   = entry,
                    MapLock = mapLock
                },
                Position = position
            }, reason);
        }

        /// <summary>
        /// Teleport <see cref="IPlayer"/> to supplied location.
        /// </summary>
        public void TeleportTo(IMapPosition mapPosition, TeleportReason reason = TeleportReason.Relocate)
        {
            if (!CanTeleport())
            {
                SendGenericError(GenericError.InstanceTransferPending);
                return;
            }

            if (DisableManager.Instance.IsDisabled(DisableType.World, mapPosition.Info.Entry.Id))
            {
                SendSystemMessage($"Unable to teleport to world {mapPosition.Info.Entry.Id} because it is disabled.");
                return;
            }

            // store vanity pet summoned before teleport so it can be summoned again after being added to the new map
            uint? vanityPetId = null;
            if (VanityPetGuid != null)
            {
                IPetEntity pet = GetVisible<IPetEntity>(VanityPetGuid.Value);
                vanityPetId = pet?.CreatureId;
            }

            pendingTeleport = new PendingTeleport
            {
                Reason      = reason,
                MapPosition = mapPosition,
                VanityPetId = vanityPetId
            };

            SetControl(null);

            MapManager.Instance.AddToMap(this, mapPosition);
            log.Trace($"Teleporting {Name}({CharacterId}) to map: {mapPosition.Info.Entry.Id}, instance: {mapPosition.Info.MapLock?.InstanceId ?? null}.");
        }

        /// <summary>
        /// Teleport <see cref="IPlayer"/> to a new position on the current map.
        /// </summary>
        public void TeleportToLocal(Vector3 position, bool showLoadingScreen = true, Action<Vector3> callback = null)
        {
            if (!CanTeleport())
            {
                SendGenericError(GenericError.InstanceTransferPending);
                return;
            }

            if (Map == null)
                return;

            if (showLoadingScreen)
                Session.EnqueueMessageEncrypted(new ServerLoadingScreen());

            SetControl(null);
            MovementManager.SetPosition(position, false);
            MovementManager.BroadcastNetworkEntityCommands();
            Relocate(position);
            SetControl(this);

            callback?.Invoke(position);

            log.Trace($"Teleporting {Name}({CharacterId}) locally to location {position.X}, {position.Y}, {position.Z}.");
        }

        /// <summary>
        /// Invoked when <see cref="IPlayer"/> teleport fails.
        /// </summary>
        public void OnTeleportToFailed(GenericError error)
        {
            if (Map != null)
            {
                SendGenericError(error);
                pendingTeleport = null;

                SetControl(this);

                log.Trace($"Error {error} occured during teleport for {Name}({CharacterId})!");
            }
            else
            {
                // player failed prerequisites to enter map on login
                // can not proceed, disconnect the client with a message
                Session.EnqueueMessageEncrypted(new ServerForceKick
                {
                    Reason = ForceKickReason.WorldDisconnect
                });

                log.Trace($"Error {error} occured during teleport for {Name}({CharacterId}), client will be disconnected!");
            }
        }

        /// <summary>
        /// Invoked when <see cref="IPlayer""/> has finished loading and is ready to enter world.
        /// </summary>
        public void OnEnteredWorld()
        {
            // right before the loading screen is removed we can now send the actual network entity commands
            // this ensures there is no desync between the client and server caused by loading
            foreach (IGridEntity item in visibleEntities.Values)
                if (item is IWorldEntity we && we.MovementManager.RequiresSynchronisation)
                    we.MovementManager.SendNetworkEntityCommands(Session);

            Session.EnqueueMessageEncrypted(new ServerPlayerEnteredWorld());
            QuestManager.SendInitialPackets();

            TryRecoverStarterTutorialOnEnteredWorld();
            SyncStarterTutorialEntityVisibility();
            TryRecoverStarterTutorialHoverboardRide();
            TryRecoverStarterTutorialQuestProgression(allowCombatTransitionRecovery: true);

            pendingTeleport = null;
            IsLoading = false;
        }

        private void TryRecoverStarterTutorialOnEnteredWorld()
        {
            if (Map?.Entry?.Id != TutorialWorldId)
                return;

            log.Debug($"Tutorial entered-world recovery for player {Guid}: position ({Position.X}, {Position.Y}, {Position.Z}), starter states [{FormatQuestStates(StarterQuestIds)}], follow-up states [{FormatQuestStates(FollowUpQuestIds)}].");

            if (HasAnyQuestState(FollowUpQuestIds))
            {
                log.Debug($"Skipping entered-world Rider's Reef recovery for player {Guid}: follow-up quest state already present [{FormatQuestStates(FollowUpQuestIds)}].");
                return;
            }

            (ushort movementQuestId, ushort hoverboardQuestId) = Faction1 switch
            {
                Faction.Exile    => (ExileMovementQuestId, ExileHoverboardQuestId),
                Faction.Dominion => (DominionMovementQuestId, DominionHoverboardQuestId),
                _                => ((ushort)0, (ushort)0)
            };

            if (movementQuestId == 0)
                return;

            QuestState? movementQuestState = QuestManager.GetQuestState(movementQuestId);
            if (movementQuestState == null)
                GrantTutorialQuestIfMissing(movementQuestId);
            else if (movementQuestState == QuestState.Completed)
                GrantTutorialQuestIfMissing(hoverboardQuestId);

            SyncStarterTutorialAreaObjectives(logNoOverlap: true);
        }

        private void TryRecoverStarterTutorialOnRelocate()
        {
            if (IsLoading || Map?.Entry?.Id != TutorialWorldId)
                return;

            bool hasRelevantActiveQuest = QuestManager.GetActiveQuests()
                .Any(q => StarterQuestIds.Contains(q.Id) || FollowUpQuestIds.Contains(q.Id));
            if (!hasRelevantActiveQuest)
                return;

            bool updatedAreaObjectives = SyncStarterTutorialAreaObjectives(logNoOverlap: false);
            bool recoveredRide = TryRecoverStarterTutorialHoverboardRideObjective();

            if (updatedAreaObjectives || recoveredRide)
                SyncStarterTutorialEntityVisibility();

            TryRecoverStarterTutorialQuestProgression(allowCombatTransitionRecovery: true);
        }

        private void GrantTutorialQuestIfMissing(ushort questId)
        {
            if (QuestManager.GetQuestState(questId) != null)
                return;

            IQuestInfo questInfo = GlobalQuestManager.Instance.GetQuestInfo(questId);
            if (questInfo == null)
                return;

            QuestManager.QuestAdd(questInfo);
            log.Debug($"Entered-world Rider's Reef recovery granted tutorial quest {questId} to player {Guid}.");
        }

        private bool SyncStarterTutorialAreaObjectives(bool logNoOverlap)
        {
            int furthestReachedIndex = GetFurthestReachedTutorialWorldLocationIndex();
            if (furthestReachedIndex < 0)
            {
                if (logNoOverlap)
                    log.Debug($"Entered-world Rider's Reef recovery found no world-location overlap for player {Guid}: position ({Position.X}, {Position.Y}, {Position.Z}), hit radius {HitRadius}, starter states [{FormatQuestStates(StarterQuestIds)}].");

                return false;
            }

            bool updated = false;
            foreach (IQuest quest in QuestManager.GetActiveQuests().Where(q => StarterQuestIds.Contains(q.Id)))
            {
                for (int index = 0; index <= furthestReachedIndex; index++)
                {
                    uint worldLocationId = TutorialWorldLocationIds[index];
                    foreach (IQuestObjective objective in GetTutorialObjectivesToUpdate(quest, worldLocationId))
                    {
                        log.Debug($"Entered-world Rider's Reef recovery advanced player {Guid}: quest {quest.Id}, objective {objective.ObjectiveInfo.Id}, world location {worldLocationId}, furthest index {furthestReachedIndex}.");
                        quest.ObjectiveUpdate(objective.ObjectiveInfo.Id, 1u);
                        updated = true;
                    }
                }
            }

            return updated;
        }

        private int GetFurthestReachedTutorialWorldLocationIndex()
        {
            int furthestIndex = -1;
            float horizontalPadding = HitRadius * 0.5f;

            for (int index = 0; index < TutorialWorldLocationIds.Length; index++)
            {
                WorldLocation2Entry worldLocation = GameTableManager.Instance.WorldLocation2.GetEntry(TutorialWorldLocationIds[index]);
                if (worldLocation != null && IsInsideStarterTutorialWorldLocation(Position, worldLocation, horizontalPadding))
                    furthestIndex = index;
            }

            return furthestIndex;
        }

        private static IEnumerable<IQuestObjective> GetTutorialObjectivesToUpdate(IQuest quest, uint worldLocationId)
        {
            List<IQuestObjective> matchingObjectives = quest
                .Where(o => MatchesTutorialWorldLocation(o, worldLocationId))
                .Where(o => !o.IsComplete())
                .ToList();

            if (matchingObjectives.Count == 0)
                return [];

            IEnumerable<IQuestObjective> objectivesToUpdate = matchingObjectives.Any(o => !o.ObjectiveInfo.IsOptional())
                ? matchingObjectives.Where(o => !o.ObjectiveInfo.IsOptional())
                : matchingObjectives;

            return objectivesToUpdate.OrderByDescending(o => o.Index).ToList();
        }

        private static bool MatchesTutorialWorldLocation(IQuestObjective objective, uint worldLocationId)
        {
            if (objective.ObjectiveInfo.Type != QuestObjectiveType.EnterArea)
                return false;

            QuestObjectiveEntry objectiveEntry = objective.ObjectiveInfo.Entry;
            return objectiveEntry.WorldLocationsIdIndicator00 == worldLocationId
                || objectiveEntry.WorldLocationsIdIndicator01 == worldLocationId
                || objectiveEntry.WorldLocationsIdIndicator02 == worldLocationId
                || objectiveEntry.WorldLocationsIdIndicator03 == worldLocationId;
        }

        private static bool IsInsideWorldLocation(Vector3 position, WorldLocation2Entry worldLocation, float horizontalPadding = 0f)
        {
            float horizontalDistanceSquared = Vector2.DistanceSquared(
                new Vector2(position.X, position.Z),
                new Vector2(worldLocation.Position0, worldLocation.Position2));

            float horizontalRange = worldLocation.Radius + horizontalPadding;
            if (horizontalDistanceSquared > horizontalRange * horizontalRange)
                return false;

            return worldLocation.MaxVerticalDistance <= 0f
                || MathF.Abs(position.Y - worldLocation.Position1) <= worldLocation.MaxVerticalDistance;
        }

        private static bool IsInsideStarterTutorialWorldLocation(Vector3 position, WorldLocation2Entry worldLocation, float horizontalPadding = 0f)
        {
            if (worldLocation.Id == TutorialHoverboardFinishWorldLocationId)
                horizontalPadding = MathF.Max(horizontalPadding, TutorialHoverboardFinishRecoveryPadding);

            return IsInsideWorldLocation(position, worldLocation, horizontalPadding);
        }

        private bool HasAnyQuestState(IEnumerable<ushort> questIds)
        {
            return questIds.Any(questId => QuestManager.GetQuestState(questId) != null);
        }

        private string FormatQuestStates(IEnumerable<ushort> questIds)
        {
            return string.Join(", ", questIds.Select(questId => $"{questId}:{QuestManager.GetQuestState(questId)?.ToString() ?? "None"}"));
        }

        /// <summary>
        /// Used to send the current in game time to this player
        /// </summary>
        private void SendInGameTime()
        {
            uint lengthOfInGameDayInSeconds = SharedConfiguration.Instance.Get<RealmConfig>().LengthOfInGameDay;
            if (lengthOfInGameDayInSeconds == 0u)
                lengthOfInGameDayInSeconds = (uint)TimeSpan.FromHours(3.5d).TotalSeconds; // Live servers were 3.5h per in game day

            double timeOfDay = DateTime.UtcNow.Subtract(DateTime.UnixEpoch).TotalSeconds / lengthOfInGameDayInSeconds % 1;

            Session.EnqueueMessageEncrypted(new ServerTimeOfDay
            {
                TimeOfDay   = (uint)(timeOfDay * TimeSpan.FromDays(1).TotalSeconds),
                LengthOfDay = lengthOfInGameDayInSeconds
            });
        }

        /// <summary>
        /// Make <see cref="IPlayer"/> sit on provided <see cref="IWorldEntity"/>.
        /// </summary>
        public void Sit(IWorldEntity chair)
        {
            if (IsSitting)
                Unsit();

            currentChairGuid = chair.Guid;

            chair.AddBusy(ChairBusyEffectId, 0u, 0u, 0u, Guid, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u);
            EnqueueToVisible(new ServerUnitSetChair
            {
                UnitId      = Guid,
                UnitIdChair = chair.Guid,
                WaitForUnit = false
            }, true);
        }

        /// <summary>
        /// Remove <see cref="IPlayer"/> from the <see cref="IWorldEntity"/> it is sitting on.
        /// </summary>
        public void Unsit()
        {
            if (!IsSitting)
                return;

            IWorldEntity currentChair = GetVisible<IWorldEntity>(currentChairGuid.Value);
            if (currentChair == null)
                throw new InvalidOperationException();

            currentChair.RemoveBusy(ChairBusyEffectId);
            EnqueueToVisible(new ServerUnitSetChair
            {
                UnitId      = Guid,
                UnitIdChair = 0,
                WaitForUnit = false
            }, true);

            currentChairGuid = null;
        }

        /// <summary>
        /// Send <see cref="GenericError"/> to <see cref="IPlayer"/>.
        /// </summary>
        public void SendGenericError(GenericError error)
        {
            Session.EnqueueMessageEncrypted(new ServerGenericError
            {
                Error = error
            });
        }

        /// <summary>
        /// Send message to <see cref="IPlayer"/> using the <see cref="ChatChannel.System"/> channel.
        /// </summary>
        /// <param name="text"></param>
        public void SendSystemMessage(string text)
        {
            Session.EnqueueMessageEncrypted(new ServerChat
            {
                Channel = new Channel
                {
                    ChatChannelId = ChatChannelType.System
                },
                From = new Network.World.Message.Model.Shared.Identity
                {
                    Id = 0,
                    RealmId = 0,
                },
                Text = text
            });
        }

        public uint GetTotalAttributePoints()
        {
            uint total = 0u;
            for (uint level = 1u; level <= Level; level++)
                total += GameTableManager.Instance.XpPerLevel.GetEntry(level)?.AttributePointsPerLevel ?? 0u;

            return total;
        }

        public uint GetAvailableAttributePoints()
        {
            uint spent = 0u;
            foreach (uint allocation in attributePointAllocations)
                spent += allocation;

            uint total = GetTotalAttributePoints();
            return spent >= total ? 0u : total - spent;
        }

        public bool TrySpendAttributePoints(IReadOnlyList<uint> allocations, out uint availableAttributePoints)
        {
            if (allocations == null || allocations.Count != attributePointAllocations.Length)
            {
                availableAttributePoints = GetAvailableAttributePoints();
                return false;
            }

            ulong spent = 0ul;
            foreach (uint allocation in allocations)
                spent += allocation;

            uint total = GetTotalAttributePoints();
            if (spent > total)
            {
                availableAttributePoints = GetAvailableAttributePoints();
                return false;
            }

            for (int i = 0; i < attributePointAllocations.Length; i++)
                attributePointAllocations[i] = allocations[i];

            availableAttributePoints = total - (uint)spent;
            SendAttributePoints();
            return true;
        }

        public void ResetAttributePoints()
        {
            Array.Clear(attributePointAllocations);
            SendAttributePoints();
        }

        public void SendAttributePoints()
        {
            if (Session == null)
                return;

            Session.EnqueueMessageEncrypted(new ServerAttributePoints
            {
                AttributePoints = GetAvailableAttributePoints()
            });
        }

        public bool HasTradeskill(TradeskillType tradeskillId)
        {
            return tradeskills.TryGetValue(tradeskillId, out TradeskillState tradeskill) && tradeskill.IsActive != 0u;
        }

        public bool LearnTradeskill(TradeskillType toLearnTradeskillId, TradeskillType toDropTradeskillId)
        {
            if (toDropTradeskillId != 0
                && toDropTradeskillId != toLearnTradeskillId
                && tradeskills.TryGetValue(toDropTradeskillId, out TradeskillState droppedTradeskill)
                && droppedTradeskill.IsActive != 0u)
            {
                droppedTradeskill.IsActive = 0u;
                droppedTradeskill.MarkDirty();
                SendProfessionUpdate(BuildInactiveTradeskillInfo(toDropTradeskillId));
            }

            bool wasActive = HasTradeskill(toLearnTradeskillId);
            bool alreadyKnown = tradeskills.TryGetValue(toLearnTradeskillId, out TradeskillState learnedTradeskill);
            if (!alreadyKnown)
            {
                learnedTradeskill = TradeskillState.Create(CharacterId, toLearnTradeskillId);
                tradeskills.Add(toLearnTradeskillId, learnedTradeskill);
            }

            learnedTradeskill.IsActive = 1u;
            learnedTradeskill.EnsureTalentPointBudget(MaxTradeskillTalentTiers);
            learnedTradeskill.MarkDirty();

            SendProfessionUpdate(learnedTradeskill.BuildInfo());
            QuestManager.ObjectiveUpdate(QuestObjectiveType.LearnTradeskill, (uint)toLearnTradeskillId, 1u);
            if (!wasActive && !alreadyKnown)
                CheckTradeskillTierAchievements(toLearnTradeskillId, 0u, learnedTradeskill.TradeskillXp);
            return true;
        }

        public uint AddTradeskillXp(TradeskillType tradeskillId, uint amount)
        {
            if (amount == 0u || !HasTradeskill(tradeskillId))
                return 0u;

            TradeskillState tradeskill = tradeskills[tradeskillId];
            uint previousXp = tradeskill.TradeskillXp;
            tradeskill.TradeskillXp = uint.MaxValue - tradeskill.TradeskillXp < amount
                ? uint.MaxValue
                : tradeskill.TradeskillXp + amount;

            if (tradeskill.TradeskillXp == previousXp)
                return 0u;

            tradeskill.MarkDirty();
            SendProfessionUpdate(tradeskill.BuildInfo());
            CheckTradeskillTierAchievements(tradeskillId, previousXp, tradeskill.TradeskillXp);
            return tradeskill.TradeskillXp - previousXp;
        }

        private void CheckTradeskillTierAchievements(TradeskillType tradeskillId, uint previousXp, uint currentXp)
        {
            foreach (TradeskillTierEntry tierEntry in GameTableManager.Instance.TradeskillTier.Entries
                .Where(entry => entry.TradeSkillId == (uint)tradeskillId
                    && entry.RequiredXp <= currentXp
                    && (entry.RequiredXp == 0u ? previousXp == 0u : entry.RequiredXp > previousXp)))
                AchievementManager.CheckAchievements(this, AchievementType.TradeskillTier, tierEntry.Id);
        }

        public uint AddTradeskillXpForTier(uint tradeskillTierId, uint amount)
        {
            if (amount == 0u)
                return 0u;

            TradeskillTierEntry tierEntry = GameTableManager.Instance.TradeskillTier.GetEntry(tradeskillTierId);
            if (tierEntry == null || tierEntry.TradeSkillId == 0u)
                return 0u;

            var tradeskillId = (TradeskillType)tierEntry.TradeSkillId;
            if (!Enum.IsDefined(tradeskillId))
                return 0u;

            return AddTradeskillXp(tradeskillId, amount);
        }

        public bool PickTradeskillTalent(TradeskillType tradeskillId, uint tier, uint tradeskillBonusId)
        {
            if (tier >= MaxTradeskillTalentTiers || !HasTradeskill(tradeskillId))
                return false;

            TradeskillState tradeskill = tradeskills[tradeskillId];
            int index = (int)tier;
            if (tradeskill.TalentTierIds[index] == 0u)
            {
                if (tradeskill.TalentPoints == 0u)
                    return false;

                tradeskill.TalentPoints--;
            }

            tradeskill.TalentTierIds[index] = tradeskillBonusId;
            tradeskill.MarkDirty();

            SendProfessionUpdate(tradeskill.BuildInfo());
            return true;
        }

        public bool ResetTradeskillTalents(TradeskillType tradeskillId)
        {
            if (!HasTradeskill(tradeskillId))
                return false;

            TradeskillState tradeskill = tradeskills[tradeskillId];
            Array.Clear(tradeskill.TalentTierIds);
            tradeskill.TalentPoints = MaxTradeskillTalentTiers;
            tradeskill.MarkDirty();

            SendProfessionUpdate(tradeskill.BuildInfo());
            Session.EnqueueMessageEncrypted(new ServerTradeskillRelearnCooldown());
            return true;
        }

        public bool HasLearnedSchematic(uint tradeskillSchematic2Id)
        {
            return schematics.ContainsKey(tradeskillSchematic2Id);
        }

        public bool LearnSchematic(uint tradeskillSchematic2Id, bool discovered = false)
        {
            TradeskillSchematic2Entry schematicEntry = GameTableManager.Instance.TradeskillSchematic2.GetEntry(tradeskillSchematic2Id);
            if (schematicEntry == null)
                return false;

            if (!Enum.IsDefined((TradeskillType)schematicEntry.TradeSkillId))
                return false;

            if (schematics.TryGetValue(tradeskillSchematic2Id, out SchematicState existingSchematic))
            {
                if (discovered && !existingSchematic.Discovered)
                {
                    existingSchematic.Discovered = true;
                    existingSchematic.DiscoveryCoordinateX = schematicEntry.VectorX;
                    existingSchematic.DiscoveryCoordinateY = schematicEntry.VectorY;
                    existingSchematic.MarkDirty();
                }

                return false;
            }

            SchematicState schematic = SchematicState.Create(CharacterId, schematicEntry, discovered);
            schematics.Add(tradeskillSchematic2Id, schematic);

            Session.EnqueueMessageEncrypted(new ServerSchematicAddLearned
            {
                TradeskillId           = (TradeskillType)schematicEntry.TradeSkillId,
                TradeskillSchematic2Id = tradeskillSchematic2Id,
                DiscoveryCoordinates   = new Vector2(schematic.DiscoveryCoordinateX, schematic.DiscoveryCoordinateY)
            });

            QuestManager.ObjectiveUpdate(QuestObjectiveType.ObtainSchematic, tradeskillSchematic2Id, 1u);
            return true;
        }

        public bool DiscoverSchematic(uint tradeskillSchematic2Id)
        {
            return LearnSchematic(tradeskillSchematic2Id, true);
        }

        public void SendTradeskillInitialPackets()
        {
            List<uint> learnedSchematics = schematics.Keys
                .OrderBy(s => s)
                .ToList();

            Session.EnqueueMessageEncrypted(new ServerProfessionsLoad
            {
                Tradeskills = tradeskills.Values
                    .OrderBy(t => t.TradeskillId)
                    .Select(t => t.BuildInfo())
                    .ToList(),
                LearnedSchematics = learnedSchematics,
                DiscoveredSchematics = schematics.Values
                    .Where(s => s.Discovered)
                    .OrderBy(s => s.TradeskillSchematic2Id)
                    .Select(s => new ServerProfessionsLoad.DiscoveredSchematic
                    {
                        TradeskillSchematic2Id = s.TradeskillSchematic2Id,
                        Coordinates = new Vector2(s.DiscoveryCoordinateX, s.DiscoveryCoordinateY)
                    })
                    .ToList(),
                LearnedSchematicDiscoveredFlags = BuildLearnedSchematicDiscoveredFlags(learnedSchematics)
            });

            Session.EnqueueMessageEncrypted(new ServerProfessionModifiers());
        }

        private List<uint> BuildLearnedSchematicDiscoveredFlags(IReadOnlyList<uint> learnedSchematics)
        {
            var flags = new List<uint>();
            for (int i = 0; i < learnedSchematics.Count; i++)
            {
                if (i % 32 == 0)
                    flags.Add(0u);

                if (schematics.TryGetValue(learnedSchematics[i], out SchematicState schematic) && schematic.Discovered)
                    flags[^1] |= 1u << (i % 32);
            }

            return flags;
        }

        private void SendProfessionUpdate(TradeskillInfo tradeskillInfo)
        {
            Session.EnqueueMessageEncrypted(new ServerProfessionUpdate
            {
                Tradeskill = tradeskillInfo
            });
        }

        private static TradeskillInfo BuildInactiveTradeskillInfo(TradeskillType tradeskillId)
        {
            return new TradeskillInfo
            {
                TradeskillId = tradeskillId,
                IsActive     = 0u
            };
        }

        /// <summary>
        /// Returns whether this <see cref="IPlayer"/> is allowed to summon or be added to a mount.
        /// </summary>
        public bool CanMount()
        {
            return PlatformGuid == null && pendingTeleport == null;
        }

        /// <summary>
        /// Dismounts this <see cref="IPlayer"/> from a vehicle that it's attached to
        /// </summary>
        public void Dismount()
        {
            if (PlatformGuid == null)
                return;

            IVehicleEntity vehicle = GetVisible<IVehicleEntity>(PlatformGuid.Value);
            vehicle?.PassengerRemove(this);
        }

        public void RecordStarterTutorialDepartureTerminal(uint creatureId)
        {
            if (!DepartureTerminalCreatureIds.Contains(creatureId))
                return;

            StarterTutorialDepartureTerminalCreatureId = creatureId;
            log.Debug($"Recorded starter tutorial departure terminal for player {Guid}: creature={creatureId}.");
        }

        public void SyncStarterTutorialEntityVisibility()
        {
            if (Map?.Entry?.Id != TutorialWorldId)
                return;

            bool dismountedForFinishInteraction = TryPrepareStarterTutorialHoverboardFinishInteraction();
            bool hasUnlockedHoverboardCourse = HasUnlockedStarterHoverboardCourse();
            bool hasCombatProjectorAvailable = HasStarterTutorialCombatProjectorAvailable();
            var tutorialEntities = new Dictionary<uint, IGridEntity>();

            foreach (IGridEntity entity in Map.Search(Vector3.Zero, null, starterTutorialEntitySearchCheck))
            {
                tutorialEntities[entity.Guid] = entity;
            }

            foreach (IGridEntity entity in visibleEntities.Values)
            {
                if (IsStarterTutorialManagedEntity(entity))
                    tutorialEntities[entity.Guid] = entity;
            }

            foreach (IGridEntity entity in tutorialEntities.Values)
            {
                bool isVisible = visibleEntities.ContainsKey(entity.Guid);
                bool shouldBeVisible = CanSeeEntity(entity);

                if (shouldBeVisible)
                {
                    if (isVisible)
                        continue;

                    AddVisible(entity);
                    if (entity != this)
                        entity.AddVisible(this);

                    continue;
                }

                if (!isVisible)
                {
                    ForceRemoveStarterTutorialEntityFromClient(entity, isVisible);
                    continue;
                }

                RemoveVisible(entity);
                if (entity != this)
                    entity.RemoveVisible(this);

                ForceRemoveStarterTutorialEntityFromClient(entity, isVisible);
            }

            if (tutorialEntities.Count != 0)
            {
                string visibilityDecisions = string.Join(", ",
                    tutorialEntities.Values
                        .OfType<IWorldEntity>()
                        .OrderBy(e => e.CreatureId)
                        .ThenBy(e => e.Guid)
                        .Select(e => $"{e.CreatureId}/{e.QuestChecklistIdx}/{e.EntityMode}:{(ShouldHideStarterTutorialEntity(e) ? "hide" : "show")}:visible={visibleEntities.ContainsKey(e.Guid)}"));

                log.Debug($"Tutorial hoverboard visibility sync for player {Guid}: unlocked={hasUnlockedHoverboardCourse}, finishReady={hasCombatProjectorAvailable}, dismounted={dismountedForFinishInteraction}, progress [{FormatStarterHoverboardQuestProgress()}], entities [{visibilityDecisions}].");
            }
        }

        private bool ShouldHideStarterTutorialEntity(IGridEntity entity)
        {
            if (Map?.Entry?.Id != TutorialWorldId || entity is not IWorldEntity worldEntity)
                return false;

            return ShouldHideStarterTutorialEntity(worldEntity);
        }

        private bool ShouldForceStarterTutorialEntityVisibility(IGridEntity entity)
        {
            return Map?.Entry?.Id == TutorialWorldId
                && IsStarterTutorialManagedEntity(entity)
                && !ShouldHideStarterTutorialEntity(entity);
        }

        private bool ShouldHideStarterTutorialEntity(IWorldEntity entity)
        {
            bool hasUnlockedHoverboardCourse = HasUnlockedStarterHoverboardCourse();
            bool hasCombatProjectorAvailable = HasStarterTutorialCombatProjectorAvailable();

            return entity.CreatureId switch
            {
                TutorialHoverboardBarrierCreatureId => hasUnlockedHoverboardCourse,
                TutorialHoverboardSpinningHologramCreatureId => hasUnlockedHoverboardCourse,
                TutorialHoverboardObjectiveRingCreatureId => hasUnlockedHoverboardCourse,
                TutorialHoverboardPressurePlate00CreatureId => hasUnlockedHoverboardCourse,
                TutorialHoverboardPressurePlate01CreatureId => hasUnlockedHoverboardCourse,
                TutorialHoverboardPressurePlate02CreatureId => hasUnlockedHoverboardCourse,
                TutorialHoverboardDirectionArrowCreatureId => ShouldHideStarterTutorialDirectionArrow(entity, hasUnlockedHoverboardCourse),
                TutorialHoverboardHoloringCreatureId => !hasUnlockedHoverboardCourse,
                TutorialHoverboardStartArrowCreatureId => !hasUnlockedHoverboardCourse,
                TutorialHoverboardBoosterCreatureId => !hasUnlockedHoverboardCourse,
                TutorialHoverboardFinishLineCreatureId => !hasUnlockedHoverboardCourse,
                TutorialCombatSpinningHologramCreatureId => !hasCombatProjectorAvailable,
                TutorialCombatProjectorCreatureId => !hasCombatProjectorAvailable,
                _ => false
            };
        }

        private static bool ShouldHideStarterTutorialDirectionArrow(IWorldEntity entity, bool hasUnlockedHoverboardCourse)
        {
            bool isPartOneArrow = entity.QuestChecklistIdx <= 3;
            return hasUnlockedHoverboardCourse ? isPartOneArrow : !isPartOneArrow;
        }

        private bool HasStarterTutorialCombatProjectorAvailable()
        {
            if (!TryGetStarterTutorialCombatTransitionContext(out ushort hoverboardQuestId,
                    out ushort combatQuestId,
                    out uint projectorObjectiveId,
                    out uint rideObjectiveId,
                    out uint finishObjectiveId,
                    out _))
                return false;

            QuestState? hoverboardState = QuestManager.GetQuestState(hoverboardQuestId);
            QuestState? combatState = QuestManager.GetQuestState(combatQuestId);
            if (combatState is QuestState.Accepted or QuestState.Achieved or QuestState.Completed)
                return true;

            if (hoverboardState is not (QuestState.Accepted or QuestState.Achieved or QuestState.Completed))
                return false;

            IQuest hoverboardQuest = QuestManager.GetActiveQuests().FirstOrDefault(q => q.Id == hoverboardQuestId);
            if (hoverboardQuest == null)
                return false;

            return IsStarterTutorialObjectiveComplete(hoverboardQuest, projectorObjectiveId)
                && IsStarterTutorialObjectiveComplete(hoverboardQuest, rideObjectiveId)
                && !IsStarterTutorialObjectiveComplete(hoverboardQuest, finishObjectiveId);
        }

        private bool TryPrepareStarterTutorialHoverboardFinishInteraction()
        {
            if (Map?.Entry?.Id != TutorialWorldId || PlatformGuid == null || !HasStarterTutorialCombatProjectorAvailable())
                return false;

            WorldLocation2Entry finishWorldLocation = GameTableManager.Instance.WorldLocation2.GetEntry(TutorialHoverboardFinishWorldLocationId);
            if (finishWorldLocation == null || !IsInsideStarterTutorialWorldLocation(Position, finishWorldLocation, HitRadius * 0.5f))
                return false;

            Dismount();
            bool dismounted = PlatformGuid == null;
            if (dismounted)
            {
                log.Debug($"Starter tutorial hoverboard finish interaction prepared for player {Guid}: dismounted at worldLocation={TutorialHoverboardFinishWorldLocationId}, progress [{FormatStarterHoverboardQuestProgress()}].");
            }

            return dismounted;
        }

        private bool HasUnlockedStarterHoverboardCourse()
        {
            if (HasAnyQuestState(FollowUpQuestIds))
                return true;

            foreach (ushort questId in new[] { ExileHoverboardQuestId, DominionHoverboardQuestId })
            {
                QuestState? questState = QuestManager.GetQuestState(questId);
                if (questState is QuestState.Achieved or QuestState.Completed)
                    return true;
            }

            return QuestManager.GetActiveQuests()
                .Where(q => q.Id == ExileHoverboardQuestId || q.Id == DominionHoverboardQuestId)
                .SelectMany(q => q)
                .Any(o => HoverboardProjectorObjectiveIds.Contains(o.ObjectiveInfo.Id) && o.IsComplete());
        }

        private static bool IsStarterTutorialManagedEntity(IGridEntity entity)
        {
            return entity is IWorldEntity worldEntity && tutorialManagedCreatureIds.Contains(worldEntity.CreatureId);
        }

        private void ForceRemoveStarterTutorialEntityFromClient(IGridEntity entity, bool wasVisible)
        {
            if (entity is not IWorldEntity worldEntity)
                return;

            if (!tutorialManagedCreatureIds.Contains(worldEntity.CreatureId))
                return;

            Session.EnqueueMessageEncrypted(new ServerEntityDestroy
            {
                Guid = entity.Guid,
                Flag = true
            });

            log.Debug($"Tutorial hoverboard forced destroy for player {Guid}: entity={entity.Guid}, creature={worldEntity.CreatureId}, wasVisible={wasVisible}, progress [{FormatStarterHoverboardQuestProgress()}].");
        }

        private void TryRecoverStarterTutorialHoverboardRide()
        {
            if (Map?.Entry?.Id != TutorialWorldId)
                return;

            if (PlatformGuid != null)
                return;

            if (!IsStarterHoverboardRidePending())
                return;

            var mountSpellParameters = new SpellParameters
            {
                PrimaryTargetId        = Guid,
                UserInitiatedSpellCast = false,
                IgnoreGlobalCooldown   = true,
                CancelActiveTrade      = true,
                ClientRequestSource    = nameof(TryRecoverStarterTutorialHoverboardRide)
            };

            CastResult mountCastResult = TryCastSpell(TutorialHoverboardMountSpellId, mountSpellParameters);
            log.Debug($"Tutorial hoverboard ride direct remount recovery for player {Guid}: castResult={mountCastResult}, progress [{FormatStarterHoverboardQuestProgress()}].");

            if (mountCastResult == CastResult.Ok)
            {
                CastSpell(TutorialHoverboardSprintVisualSpellId, new SpellParameters
                {
                    PrimaryTargetId        = Guid,
                    UserInitiatedSpellCast = false,
                    IgnoreGlobalCooldown   = true,
                    CancelActiveTrade      = true,
                    ClientRequestSource    = nameof(TryRecoverStarterTutorialHoverboardRide)
                });
                TryRecoverStarterTutorialQuestProgression();
                return;
            }

            IWorldEntity projector = GetVisibleCreature<IWorldEntity>(TutorialHoverboardProjectorCreatureId).FirstOrDefault();
            if (projector == null)
            {
                log.Debug($"Tutorial hoverboard ride recovery skipped for player {Guid}: no visible projector, progress [{FormatStarterHoverboardQuestProgress()}].");
                return;
            }

            var spellParameters = new SpellParameters
            {
                PrimaryTargetId        = projector.Guid,
                UserInitiatedSpellCast = false,
                CancelActiveTrade      = true,
                ClientRequestSource    = nameof(TryRecoverStarterTutorialHoverboardRide)
            };

            CastResult castResult = TryCastSpell(TutorialHoverboardProjectorActivateSpellId, spellParameters);
            log.Debug($"Tutorial hoverboard ride recovery for player {Guid}: projector={projector.Guid}, castResult={castResult}, progress [{FormatStarterHoverboardQuestProgress()}].");

            if (castResult != CastResult.Ok)
            {
                projector.OnActivateFail(this);
                return;
            }

            projector.OnActivateCast(this);
            projector.OnActivateSuccess(this);
            TryRecoverStarterTutorialQuestProgression();
        }

        public void TryRecoverStarterTutorialQuestProgression()
        {
            TryRecoverStarterTutorialQuestProgression(allowCombatTransitionRecovery: false);
        }

        public void TryRecoverStarterTutorialQuestProgression(bool allowCombatTransitionRecovery)
        {
            if (Map?.Entry?.Id != TutorialWorldId || recoveringStarterTutorialQuestProgression)
                return;

            recoveringStarterTutorialQuestProgression = true;

            try
            {
                bool updatedObjectives = TryRecoverStarterTutorialHoverboardRideObjective();
                bool completedQuests = TryCompleteAchievedStarterTutorialQuests();
                bool advancedQuestChain = TryRecoverStarterTutorialQuestChain();
                bool recoveredCombatTransition = allowCombatTransitionRecovery && TryRecoverStarterTutorialCombatTransition();

                if (updatedObjectives || completedQuests || advancedQuestChain || recoveredCombatTransition)
                    SyncStarterTutorialEntityVisibility();
            }
            finally
            {
                recoveringStarterTutorialQuestProgression = false;
            }
        }

        public void TryRecoverStarterTutorialCombatProjectorActivation()
        {
            if (Map?.Entry?.Id != TutorialWorldId || recoveringStarterTutorialQuestProgression)
                return;

            recoveringStarterTutorialQuestProgression = true;

            try
            {
                bool completedQuests = TryCompleteAchievedStarterTutorialQuests();
                bool advancedQuestChain = TryRecoverStarterTutorialQuestChain();
                bool recoveredCombatTransition = TryRecoverStarterTutorialCombatTransition(requireFinishWorldLocation: false);

                log.Debug($"Starter tutorial combat projector activation recovery for player {Guid}: completedQuests={completedQuests}, advancedQuestChain={advancedQuestChain}, recoveredCombatTransition={recoveredCombatTransition}, position=({Position.X}, {Position.Y}, {Position.Z}), progress [{FormatStarterHoverboardQuestProgress()}].");

                if (completedQuests || advancedQuestChain || recoveredCombatTransition)
                    SyncStarterTutorialEntityVisibility();
            }
            finally
            {
                recoveringStarterTutorialQuestProgression = false;
            }
        }

        private bool TryRecoverStarterTutorialHoverboardRideObjective()
        {
            if (Map?.Entry?.Id != TutorialWorldId)
                return false;

            WorldLocation2Entry finishWorldLocation = GameTableManager.Instance.WorldLocation2.GetEntry(TutorialHoverboardFinishWorldLocationId);
            if (finishWorldLocation == null || !IsInsideStarterTutorialWorldLocation(Position, finishWorldLocation, HitRadius * 0.5f))
                return false;

            bool updated = false;

            foreach (IQuest quest in QuestManager.GetActiveQuests().Where(q => q.Id == ExileHoverboardQuestId || q.Id == DominionHoverboardQuestId))
            {
                bool projectorObjectiveComplete = quest.Any(o => HoverboardProjectorObjectiveIds.Contains(o.ObjectiveInfo.Id) && o.IsComplete());
                if (!projectorObjectiveComplete)
                    continue;

                IQuestObjective rideObjective = quest.FirstOrDefault(o => HoverboardRideObjectiveIds.Contains(o.ObjectiveInfo.Id));
                bool rideObjectiveComplete = rideObjective?.IsComplete() == true;

                if (rideObjective != null && !rideObjectiveComplete)
                {
                    uint requiredProgress = rideObjective.ObjectiveInfo.Entry.Count;
                    quest.ObjectiveUpdate(rideObjective.ObjectiveInfo.Id, requiredProgress == 0u ? 1u : requiredProgress);
                    rideObjectiveComplete = true;
                    updated = true;

                    log.Debug($"Starter tutorial hoverboard ride objective recovery for player {Guid}: quest={quest.Id}, objective={rideObjective.ObjectiveInfo.Id}, progress [{FormatStarterHoverboardQuestProgress()}].");
                }

                bool finishObjectiveComplete = quest.Any(o => HoverboardFinishObjectiveIds.Contains(o.ObjectiveInfo.Id) && o.IsComplete());
                if (ShouldRecoverHoverboardFinishPosition(projectorObjectiveComplete, rideObjectiveComplete, finishObjectiveComplete))
                    updated |= TrySnapStarterTutorialHoverboardFinishLocation(finishWorldLocation);
            }

            return updated;
        }

        private bool TrySnapStarterTutorialHoverboardFinishLocation(WorldLocation2Entry finishWorldLocation)
        {
            if (!CanTeleport())
                return false;

            var destination = new Vector3(finishWorldLocation.Position0, finishWorldLocation.Position1, finishWorldLocation.Position2);
            if (Vector3.DistanceSquared(Position, destination) <= TutorialHoverboardFinishSnapTolerance * TutorialHoverboardFinishSnapTolerance)
                return false;

            if (PlatformGuid != null)
                Dismount();

            TeleportToLocal(destination, showLoadingScreen: false);
            log.Debug($"Starter tutorial hoverboard finish snap for player {Guid}: destination=({destination.X}, {destination.Y}, {destination.Z}), progress [{FormatStarterHoverboardQuestProgress()}].");
            return true;
        }

        private bool TryRecoverStarterTutorialCombatTransition(bool requireFinishWorldLocation = true)
        {
            if (Map?.Entry?.Id != TutorialWorldId || !CanTeleport())
                return false;

            if (requireFinishWorldLocation)
            {
                WorldLocation2Entry finishWorldLocation = GameTableManager.Instance.WorldLocation2.GetEntry(TutorialHoverboardFinishWorldLocationId);
                if (finishWorldLocation == null || !IsInsideStarterTutorialWorldLocation(Position, finishWorldLocation, HitRadius * 0.5f))
                    return false;
            }

            if (!TryGetStarterTutorialCombatTransitionContext(out ushort hoverboardQuestId,
                    out ushort combatQuestId,
                    out uint projectorObjectiveId,
                    out uint rideObjectiveId,
                    out uint finishObjectiveId,
                    out uint destinationWorldLocationId))
                return false;

            QuestState? hoverboardState = QuestManager.GetQuestState(hoverboardQuestId);
            QuestState? combatState = QuestManager.GetQuestState(combatQuestId);
            if (!CanRecoverStarterTutorialCombatTransition(hoverboardQuestId, hoverboardState, combatState, projectorObjectiveId, rideObjectiveId, finishObjectiveId))
                return false;

            WorldLocation2Entry destination = GameTableManager.Instance.WorldLocation2.GetEntry(destinationWorldLocationId);
            if (destination == null)
            {
                log.Warn($"Starter tutorial combat transition recovery missing destination world location {destinationWorldLocationId} for player {Guid}.");
                return false;
            }

            TeleportTo((ushort)destination.WorldId, destination.Position0, destination.Position1, destination.Position2);
            log.Debug($"Starter tutorial combat transition recovery teleported player {Guid}: destinationWorldLocation={destinationWorldLocationId}, hoverboardState={hoverboardState?.ToString() ?? "None"}, combatState={combatState?.ToString() ?? "None"}.");
            return true;
        }

        private bool CanRecoverStarterTutorialCombatTransition(ushort hoverboardQuestId, QuestState? hoverboardState, QuestState? combatState, uint projectorObjectiveId, uint rideObjectiveId, uint finishObjectiveId)
        {
            if (combatState is QuestState.Accepted or QuestState.Achieved or QuestState.Completed)
                return true;

            if (hoverboardState is not (QuestState.Accepted or QuestState.Achieved or QuestState.Completed))
                return false;

            IQuest hoverboardQuest = QuestManager.GetActiveQuests().FirstOrDefault(q => q.Id == hoverboardQuestId);
            if (hoverboardQuest == null)
                return false;

            return IsStarterTutorialObjectiveComplete(hoverboardQuest, projectorObjectiveId)
                && IsStarterTutorialObjectiveComplete(hoverboardQuest, rideObjectiveId)
                && IsStarterTutorialObjectiveComplete(hoverboardQuest, finishObjectiveId);
        }

        private bool TryGetStarterTutorialCombatTransitionContext(out ushort hoverboardQuestId, out ushort combatQuestId, out uint projectorObjectiveId, out uint rideObjectiveId, out uint finishObjectiveId, out uint destinationWorldLocationId)
        {
            switch (Faction1)
            {
                case Faction.Exile:
                    hoverboardQuestId = ExileHoverboardQuestId;
                    combatQuestId = ExileCombatQuestId;
                    projectorObjectiveId = 21324u;
                    rideObjectiveId = 21323u;
                    finishObjectiveId = 21325u;
                    destinationWorldLocationId = ExileCombatSimulationWorldLocationId;
                    return true;
                case Faction.Dominion:
                    hoverboardQuestId = DominionHoverboardQuestId;
                    combatQuestId = DominionCombatQuestId;
                    projectorObjectiveId = 21354u;
                    rideObjectiveId = 21355u;
                    finishObjectiveId = 21356u;
                    destinationWorldLocationId = DominionCombatSimulationWorldLocationId;
                    return true;
                default:
                    hoverboardQuestId = 0;
                    combatQuestId = 0;
                    projectorObjectiveId = 0u;
                    rideObjectiveId = 0u;
                    finishObjectiveId = 0u;
                    destinationWorldLocationId = 0u;
                    return false;
            }
        }

        private static bool IsStarterTutorialObjectiveComplete(IQuest quest, uint objectiveId)
        {
            return quest.Any(objective => objective.ObjectiveInfo.Id == objectiveId && objective.IsComplete());
        }

        private bool TryCompleteAchievedStarterTutorialQuests()
        {
            bool completedAny = false;

            foreach (ushort questId in ReceiverlessQuestIds)
            {
                if (QuestManager.GetQuestState(questId) != QuestState.Achieved)
                    continue;

                try
                {
                    QuestManager.QuestComplete(questId, 0, false);
                    completedAny = true;
                    log.Debug($"Tutorial quest completion recovery for player {Guid}: quest={questId}, states [{FormatQuestStates(ReceiverlessQuestIds)}].");
                }
                catch (Exception exception)
                {
                    log.Debug(exception, $"Tutorial quest completion recovery skipped for player {Guid}: quest={questId}, states [{FormatQuestStates(ReceiverlessQuestIds)}].");
                }
            }

            return completedAny;
        }

        private bool TryRecoverStarterTutorialQuestChain()
        {
            ushort[] questChain;
            switch (Faction1)
            {
                case Faction.Exile:
                    questChain = ExileQuestChain;
                    break;
                case Faction.Dominion:
                    questChain = DominionQuestChain;
                    break;
                default:
                    return false;
            }

            for (int i = 0; i < questChain.Length - 1; i++)
            {
                ushort previousQuestId = questChain[i];
                ushort nextQuestId = questChain[i + 1];

                if (QuestManager.GetQuestState(previousQuestId) != QuestState.Completed)
                    continue;

                QuestState? nextQuestState = QuestManager.GetQuestState(nextQuestId);
                if (nextQuestState is QuestState.Accepted or QuestState.Achieved or QuestState.Completed or QuestState.Ignored)
                    continue;

                IQuestInfo nextQuestInfo = GlobalQuestManager.Instance.GetQuestInfo(nextQuestId);
                if (nextQuestInfo == null)
                    continue;

                try
                {
                    QuestManager.QuestAdd(nextQuestInfo);

                    log.Debug($"Starter tutorial quest chain recovery for player {Guid}: previousQuest={previousQuestId}, addedQuest={nextQuestId}, chain states [{FormatQuestStates(questChain)}].");
                    return true;
                }
                catch (Exception exception)
                {
                    log.Debug(exception, $"Starter tutorial quest chain recovery skipped for player {Guid}: previousQuest={previousQuestId}, nextQuest={nextQuestId}, chain states [{FormatQuestStates(questChain)}].");
                }
            }

            return false;
        }

        private bool IsStarterHoverboardRidePending()
        {
            return QuestManager.GetActiveQuests()
                .Where(q => q.Id == ExileHoverboardQuestId || q.Id == DominionHoverboardQuestId)
                .Any(q => q.Any(o => HoverboardProjectorObjectiveIds.Contains(o.ObjectiveInfo.Id) && o.IsComplete())
                    && q.Any(o => HoverboardRideObjectiveIds.Contains(o.ObjectiveInfo.Id) && !o.IsComplete()));
        }

        private string FormatStarterHoverboardQuestProgress()
        {
            return string.Join(", ", QuestManager.GetActiveQuests()
                .Where(q => q.Id == ExileHoverboardQuestId || q.Id == DominionHoverboardQuestId)
                .Select(q => $"{q.Id}[{string.Join(",", q.Select(o => $"{o.ObjectiveInfo.Id}={o.Progress}"))}]"));
        }

        /// <summary>
        /// Remove all entities associated with the <see cref="IPlayer"/>
        /// </summary>
        private void DestroyDependents()
        {
            // vehicle will be removed if player is the last passenger
            Dismount();

            if (VanityPetGuid != null)
            {
                IPetEntity pet = GetVisible<IPetEntity>(VanityPetGuid.Value);
                pet?.RemoveFromMap();
                VanityPetGuid = null;
            }

            RemoveControlUnit();
        }

        private void RemoveControlUnit()
        {
            if (ControlGuid == null || ControlGuid == Guid)
                return;

            IWorldEntity controlled = Map.GetEntity<IWorldEntity>(ControlGuid.Value);
            controlled?.RemoveFromMap();
            SetControl(this);
        }

        /// <summary>
        /// Returns the time in seconds that has past since the last <see cref="IPlayer"/> save.
        /// </summary>
        public double GetTimeSinceLastSave()
        {
            return SaveDuration - saveTimer.Time;
        }

        /// <summary>
        /// Return <see cref="Disposition"/> between <see cref="IPlayer"/> and <see cref="Faction"/>.
        /// </summary>
        public override Disposition GetDispositionTo(Faction factionId, bool primary = true)
        {
            if (factionId == Faction.None)
                return Disposition.Unknown;

            IFactionNode targetFaction = FactionManager.Instance.GetFaction(factionId);
            if (targetFaction == null)
                throw new ArgumentException($"Invalid faction {factionId}!");

            // find disposition based on reputation level
            Disposition? dispositionFromReputation = GetDispositionFromReputation(targetFaction);
            if (dispositionFromReputation.HasValue)
                return dispositionFromReputation.Value;

            return base.GetDispositionTo(factionId, primary);
        }

        private Disposition? GetDispositionFromReputation(IFactionNode node)
        {
            if (node == null)
                return null;

            // check if current node has required reputation
            IReputation reputation = ReputationManager.GetReputation(node.FactionId);
            if (reputation != null)
                return FactionNode.GetDisposition(FactionNode.GetFactionLevel(reputation.Amount));

            // check if parent node has required reputation
            return GetDispositionFromReputation(node.Parent);
        }

        /// <summary>
        /// Add a new <see cref="CharacterFlag"/>.
        /// </summary>
        public void SetFlag(CharacterFlag flag)
        {
            Flags |= flag;
            SendCharacterFlagsUpdated();
        }

        /// <summary>
        /// Remove an existing <see cref="CharacterFlag"/>.
        /// </summary>
        public void RemoveFlag(CharacterFlag flag)
        {
            Flags &= ~flag;
            SendCharacterFlagsUpdated();
        }

        /// <summary>
        /// Returns if supplied <see cref="CharacterFlag"/> exists.
        /// </summary>
        public bool HasFlag(CharacterFlag flag)
        {
            return (Flags & flag) != 0;
        }

        /// <summary>
        /// Send <see cref="ServerCharacterFlagsUpdated"/> to client.
        /// </summary>
        public void SendCharacterFlagsUpdated()
        {
            Session.EnqueueMessageEncrypted(new ServerCharacterFlagsUpdated
            {
                Flags = flags
            });
        }

        public void SetPvPFlag(PvPFlag flag)
        {
            PvPFlag = flag & (PvPFlag.Enabled | PvPFlag.Forced);

            EnqueueToVisible(new ServerUnitPvpStateChange
            {
                UnitId = Guid,
                State  = GetPvpState()
            }, true);
        }

        private PvpState GetPvpState()
        {
            PvpState state = 0;

            if ((PvPFlag & PvPFlag.Enabled) != 0)
                state |= PvpState.PvpOn;

            if ((PvPFlag & PvPFlag.Forced) != 0)
                state |= PvpState.Forced;

            return state;
        }

        /// <summary>
        /// Add or update <see cref="IItemVisual"/>.
        /// </summary>
        /// <remarks>
        /// Checks for <see cref="IItemVisual"/> from <see cref="ICostume"/> if equipped for the same <see cref="ItemSlot"/> and replace if necessary.
        /// </remarks>
        public override void AddVisual(IItemVisual visual)
        {
            if (CostumeManager.CostumeIndex.HasValue)
            {
                IItemVisual costumeVisual = CostumeManager.GetItemVisual(CostumeManager.CostumeIndex.Value, visual.Slot);
                if (costumeVisual != null && costumeVisual.DisplayId.HasValue)
                    visual = costumeVisual;
            }

            base.AddVisual(visual);
        }

        protected override ServerEntityVisualUpdate BuildVisualUpdate()
        {
            // when emitting a visual update, include player specific properties
            ServerEntityVisualUpdate update = base.BuildVisualUpdate();
            update.Race = (byte)Race;
            update.Sex  = (byte)Sex;
            return update;
        }

        /// <summary>
        /// Add a <see cref="Property"/> modifier given a <see cref="ItemSlot"/> and value.
        /// </summary>
        public void AddItemProperty(Property property, ItemSlot itemSlot, float value)
        {
            if (itemProperties.TryGetValue(property, out Dictionary<ItemSlot, float> itemDict))
            {
                if (itemDict.ContainsKey(itemSlot))
                    itemDict[itemSlot] = value;
                else
                    itemDict.Add(itemSlot, value);
            }
            else
            {
                itemProperties.Add(property, new Dictionary<ItemSlot, float>
                {
                    { itemSlot, value }
                });
            }

            CalculateProperty(property);
        }

        /// <summary>
        /// Remove a <see cref="Property"/> modifier by a item that is currently affecting this <see cref="IPlayer"/>.
        /// </summary>
        public void RemoveItemProperty(Property property, ItemSlot itemSlot)
        {
            if (itemProperties.TryGetValue(property, out Dictionary<ItemSlot, float> itemDict))
                itemDict.Remove(itemSlot);

            CalculateProperty(property);
        }

        /// <summary>
        /// Calculate the primary value for <see cref="Property"/>.
        /// </summary>
        protected override void CalculatePropertyValue(IPropertyValue propertyValue)
        {
            base.CalculatePropertyValue(propertyValue);

            if (itemProperties.TryGetValue(propertyValue.Property, out Dictionary<ItemSlot, float> properties))
                foreach (float values in properties.Values)
                    propertyValue.Value += values;
        }

        /// <summary>
        /// Invoked when <see cref="IPlayer"/> has a <see cref="Property"/> updated.
        /// </summary>
        protected override void OnPropertyUpdate(IPropertyValue propertyValue)
        {
            messagePublisher.PublishAsync(new PlayerPropertyUpdatedMessage
            {
                Identity = Identity.ToInternalIdentity(),
                Property = propertyValue.Property,
                Value    = propertyValue.Value
            }).FireAndForgetAsync();
        }

        /// <summary>
        /// Invoked when <see cref="IWorldEntity"/> has a <see cref="Stat"/> updated.
        /// </summary>
        protected override void OnStatUpdate(IStatValue statValue)
        {
            messagePublisher.PublishAsync(new PlayerStatUpdatedMessage
            {
                Identity = Identity.ToInternalIdentity(),
                Stat     = statValue.Stat,
                Value    = statValue.Value
            }).FireAndForgetAsync();
        }

        protected override void OnAbsorptionUpdate()
        {
            PublishAbsorptionUpdate();
        }

        protected override void OnHealingAbsorptionUpdate()
        {
            PublishAbsorptionUpdate();
        }

        private void PublishAbsorptionUpdate()
        {
            messagePublisher.PublishAsync(new PlayerAbsorptionUpdatedMessage
            {
                Identity         = Identity.ToInternalIdentity(),
                Absorption       = CurrentAbsorption,
                AbsorptionMax    = MaxAbsorption,
                HealingAbsorb    = CurrentHealingAbsorption,
                HealingAbsorbMax = MaxHealingAbsorption
            }).FireAndForgetAsync();
        }

        /// <summary>
        /// Determine if this <see cref="IPlayer"/> can attack supplied <see cref="IUnitEntity"/>.
        /// </summary>
        public override bool CanAttack(IUnitEntity target)
        {
            if (target is IPlayer playerTarget)
            {
                if (!DuelManager.Instance.AreDueling(this, playerTarget))
                    return false;

                return IsAlive
                    && target.IsAlive
                    && !IsAggroImmune
                    && !target.IsAggroImmune
                    && IsValidAttackTarget()
                    && target.IsValidAttackTarget();
            }

            return base.CanAttack(target);
        }

        /// <summary>
        /// Modify the health of this <see cref="IPlayer"/> by the supplied amount.
        /// </summary>
        /// <remarks>
        /// If the <see cref="DamageType"/> is <see cref="DamageType.Heal"/> amount is added to current health otherwise subtracted.
        /// </remarks>
        public override void ModifyHealth(uint amount, DamageType type, IUnitEntity source)
        {
            base.ModifyHealth(amount, type, source);

            Session.EnqueueMessageEncrypted(new ServerPlayerHealthUpdate
            {
                UnitId = Guid,
                Health = Health,
                Mask   = type switch
                {
                    DamageType.Fall      => UpdateHealthMask.FallDamage,
                    DamageType.Suffocate => UpdateHealthMask.SuffocateDamage,
                    _                    => UpdateHealthMask.None
                }
            });

            if (Health > 0 && DeathState != null)
                OnResurrection(source);

            if (!IsAlive && source is IPlayer player)
                DuelManager.Instance.TryFinishDefeat(this, player);
        }

        protected override void OnDeath()
        {
            base.OnDeath();

            Dismount();
            RemoveControlUnit();

            ghostSpawnTimer = new UpdateTimer(TimeSpan.FromSeconds(2d));
        }

        private void UpdatePendingGhostSpawn(double lastTick)
        {
            if (ghostSpawnTimer == null)
                return;

            if (IsAlive || Map == null)
            {
                ghostSpawnTimer = null;
                return;
            }

            ghostSpawnTimer.Update(lastTick);
            if (!ghostSpawnTimer.HasElapsed)
                return;

            ghostSpawnTimer = null;
            IGhostEntity ghost = entityFactory.CreateEntity<IGhostEntity>();
            ghost.Initialise(this);

            Map.EnqueueAdd(ghost, new MapPosition
            {
                Info = new MapInfo
                {
                    Entry   = Map.Entry,
                    MapLock = Map is IMapInstance instance ? instance.MapLock : null
                },
                Position = Position
            });
        }

        protected override void RewardKiller(IPlayer player)
        {
            player.QuestManager.ObjectiveUpdate(QuestObjectiveType.PvPKills, 0u, 1u);
            if (Map?.Entry != null)
                player.QuestManager.ObjectiveUpdate(QuestObjectiveType.PvPKills, Map.Entry.Id, 1u);

            // PvP reward currencies are not awarded by the currently modeled duel flow.
        }

        protected void OnResurrection(IUnitEntity resurrector)
        {
            DeathState = null;
            RemoveControlUnit();
            Map?.PublicEventManager.OnResurrection(this);
        }

        private sealed class SchematicState
        {
            public ulong CharacterId { get; init; }
            public uint TradeskillSchematic2Id { get; init; }
            public bool Discovered { get; set; }
            public float DiscoveryCoordinateX { get; set; }
            public float DiscoveryCoordinateY { get; set; }
            public bool PendingCreate { get; private set; }
            public bool Dirty { get; private set; }

            public static SchematicState Create(ulong characterId, TradeskillSchematic2Entry schematicEntry, bool discovered)
            {
                return new SchematicState
                {
                    CharacterId              = characterId,
                    TradeskillSchematic2Id   = schematicEntry.Id,
                    Discovered               = discovered,
                    DiscoveryCoordinateX     = schematicEntry.VectorX,
                    DiscoveryCoordinateY     = schematicEntry.VectorY,
                    PendingCreate            = true
                };
            }

            public static SchematicState FromModel(CharacterSchematicModel model)
            {
                return new SchematicState
                {
                    CharacterId              = model.Id,
                    TradeskillSchematic2Id   = model.TradeskillSchematic2Id,
                    Discovered               = model.Discovered,
                    DiscoveryCoordinateX     = model.DiscoveryCoordinateX,
                    DiscoveryCoordinateY     = model.DiscoveryCoordinateY
                };
            }

            public void MarkDirty()
            {
                if (!PendingCreate)
                    Dirty = true;
            }

            public void ClearSaveState()
            {
                PendingCreate = false;
                Dirty         = false;
            }

            public CharacterSchematicModel BuildModel()
            {
                return new CharacterSchematicModel
                {
                    Id                    = CharacterId,
                    TradeskillSchematic2Id = TradeskillSchematic2Id,
                    Discovered            = Discovered,
                    DiscoveryCoordinateX  = DiscoveryCoordinateX,
                    DiscoveryCoordinateY  = DiscoveryCoordinateY
                };
            }
        }

        private sealed class TradeskillState
        {
            public ulong CharacterId { get; init; }
            public TradeskillType TradeskillId { get; init; }
            public uint TradeskillXp { get; set; }
            public uint IsActive { get; set; }
            public uint PropertyProficiencyFlags { get; set; }
            public uint TalentPoints { get; set; }
            public uint[] TalentTierIds { get; } = new uint[MaxTradeskillTalentTiers];
            public bool PendingCreate { get; private set; }
            public bool Dirty { get; private set; }

            public static TradeskillState Create(ulong characterId, TradeskillType tradeskillId)
            {
                return new TradeskillState
                {
                    CharacterId   = characterId,
                    TradeskillId  = tradeskillId,
                    IsActive      = 1u,
                    TalentPoints  = MaxTradeskillTalentTiers,
                    PendingCreate = true
                };
            }

            public static TradeskillState FromModel(CharacterTradeskillModel model)
            {
                var state = new TradeskillState
                {
                    CharacterId               = model.Id,
                    TradeskillId              = (TradeskillType)model.TradeskillId,
                    TradeskillXp              = model.TradeskillXp,
                    IsActive                  = model.IsActive,
                    PropertyProficiencyFlags  = model.PropertyProficiencyFlags,
                    TalentPoints              = model.TalentPoints
                };

                state.TalentTierIds[0] = model.TalentTier00;
                state.TalentTierIds[1] = model.TalentTier01;
                state.TalentTierIds[2] = model.TalentTier02;
                state.TalentTierIds[3] = model.TalentTier03;
                state.TalentTierIds[4] = model.TalentTier04;
                state.TalentTierIds[5] = model.TalentTier05;
                state.TalentTierIds[6] = model.TalentTier06;
                state.TalentTierIds[7] = model.TalentTier07;
                state.TalentTierIds[8] = model.TalentTier08;
                state.TalentTierIds[9] = model.TalentTier09;

                return state;
            }

            public void EnsureTalentPointBudget(uint maxTalentPoints)
            {
                uint selectedTalents = (uint)TalentTierIds.Count(t => t != 0u);
                if (selectedTalents >= maxTalentPoints)
                {
                    TalentPoints = 0u;
                    return;
                }

                if (TalentPoints == 0u)
                    TalentPoints = maxTalentPoints - selectedTalents;
            }

            public void MarkDirty()
            {
                if (!PendingCreate)
                    Dirty = true;
            }

            public void ClearSaveState()
            {
                PendingCreate = false;
                Dirty         = false;
            }

            public TradeskillInfo BuildInfo()
            {
                return new TradeskillInfo
                {
                    TradeskillId              = TradeskillId,
                    TradeskillXp              = TradeskillXp,
                    IsActive                  = IsActive,
                    PropertyProficiencyFlags  = PropertyProficiencyFlags,
                    TalentPoints              = TalentPoints,
                    TradeskillTalentTierIds   = TalentTierIds.ToArray()
                };
            }

            public CharacterTradeskillModel BuildModel()
            {
                return new CharacterTradeskillModel
                {
                    Id                       = CharacterId,
                    TradeskillId             = (uint)TradeskillId,
                    TradeskillXp             = TradeskillXp,
                    IsActive                 = IsActive,
                    PropertyProficiencyFlags = PropertyProficiencyFlags,
                    TalentPoints             = TalentPoints,
                    TalentTier00             = TalentTierIds[0],
                    TalentTier01             = TalentTierIds[1],
                    TalentTier02             = TalentTierIds[2],
                    TalentTier03             = TalentTierIds[3],
                    TalentTier04             = TalentTierIds[4],
                    TalentTier05             = TalentTierIds[5],
                    TalentTier06             = TalentTierIds[6],
                    TalentTier07             = TalentTierIds[7],
                    TalentTier08             = TalentTierIds[8],
                    TalentTier09             = TalentTierIds[9]
                };
            }
        }
    }
}
