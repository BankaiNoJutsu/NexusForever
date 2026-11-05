using System.Numerics;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using NexusForever.Database;
using NexusForever.Database.Auth;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Challenges;
using NexusForever.Game.Abstract.Character;
using NexusForever.Game.Abstract.Chat;
using NexusForever.Game.Abstract.Customisation;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Abstract.Group;
using NexusForever.Game.Abstract.Guild;
using NexusForever.Game.Abstract.Housing;
using NexusForever.Game.Abstract.Loot;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.Map.Lock;
using NexusForever.Game.Abstract.Map.Search;
using NexusForever.Game.Abstract.Matching.Match;
using NexusForever.Game.Abstract.Matching.Queue;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Abstract.Pvp;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Abstract.Reputation;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Abstract.Storefront;
using NexusForever.Game.Achievement;
using NexusForever.Game.Challenges;
using NexusForever.Game.Character;
using NexusForever.Game.Configuration.Model;
using NexusForever.Game.Map.Search;
using NexusForever.Game.Guild;
using NexusForever.Game.Housing;
using NexusForever.Game.Loot;
using NexusForever.Game.Map;
using NexusForever.Game.Quest;
using NexusForever.Game.RealmBank;
using NexusForever.Game.Reputation;
using NexusForever.Game.Spell;
using NexusForever.Game.Static;
using NexusForever.Game.Static.Achievement;
using NexusForever.Game.Static.Chat;
using NexusForever.Game.Static.Crafting;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Account.Inventory;
using NexusForever.Game.Storefront;
using NexusForever.Game.Static.Guild;
using NexusForever.Game.Static.Option;
using NexusForever.Game.Static.Pvp;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Static.RBAC;
using NexusForever.Game.Static.Reputation;
using NexusForever.Game.Static.Setting;
using NexusForever.Game.Static.Spell;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.GameTable.Text.Filter;
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
using NetworkPropertyValue = NexusForever.Network.World.Message.Model.Shared.PropertyValue;
using Path = NexusForever.Game.Static.PlayerPath.Path;
using static NexusForever.Game.Static.Tutorial.StarterTutorialDefinition;

namespace NexusForever.Game.Entity
{
    public partial class Player : UnitEntity, IPlayer
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
            Level       = 0x0010,
            Flags       = 0x0020,
            Innate      = 0x0080,
            Sex         = 0x0100,
            Race        = 0x0200,
            Options     = 0x0400,
            PvP         = 0x0800,
        }

        private static readonly ILogger log = LogManager.GetCurrentClassLogger();

        private const ushort TutorialWorldId = 3460;
        private const float DefaultInteractionMaxRange = 10f;
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
        private const ulong UngroupedClientGroupAssociationMask = 0x8000000000000000ul;
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
        private bool starterTutorialCompletionInFlight;
        private StarterTutorialRecovery? queuedStarterTutorialRecovery;

        [Flags]
        private enum StarterTutorialRecovery
        {
            QuestProgression = 1,
            FinishLocation = 2,
            CombatProjector = 4
        }

        public override EntityType Type => EntityType.Player;

        public IAccount Account { get; private set; }

        public Abstract.Identity Identity { get; private set; }

        public ulong CharacterId { get => Identity.Id; }
        private ulong presenceSessionEpoch, presenceAdmission;

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
                if (base.Level == value)
                    return;

                RetainQuestLevelForVisibility(value);
                base.Level = value;
                saveMask |= PlayerSaveMask.Level;

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

        /// <summary>
        /// Stable non-zero client-side group sentinel for ungrouped players.
        /// </summary>
        public ulong ClientGroupAssociation => GroupAssociation != 0ul ? GroupAssociation : UngroupedClientGroupAssociationMask | CharacterId;

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
        private bool deferredLoadingWorldZoneRuntimeSync;

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
        public IGuildManager GuildManager { get; }
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
        private readonly VendorPriceModifierCollection vendorPriceModifiers = new();

        public float VendorSellPriceMultiplier => vendorPriceModifiers.VendorSellMultiplier;
        public float VendorBuyPriceMultiplier => vendorPriceModifiers.VendorBuyMultiplier;

        public bool TryAddVendorPriceModifier(uint effectId, float vendorSellMultiplier, float vendorBuyMultiplier, uint spell4Id, uint spell4EffectId, uint castingId, uint stackGroupId, uint stackCap, out string skippedReason)
        {
            bool added = vendorPriceModifiers.TryAdd(
                effectId,
                vendorSellMultiplier,
                vendorBuyMultiplier,
                spell4Id,
                spell4EffectId,
                castingId,
                stackGroupId,
                stackCap,
                out skippedReason);
            if (added)
                RefreshSelectedVendorInfo();

            return added;
        }

        public bool RemoveVendorPriceModifier(uint effectId)
        {
            bool removed = vendorPriceModifiers.Remove(effectId);
            if (removed)
                RefreshSelectedVendorInfo();

            return removed;
        }

        public void RefreshSelectedVendorInfo(uint? vendorGuid = null)
        {
            if (vendorGuid.HasValue)
                selectedVendorGuid = vendorGuid;

            if (selectedVendorInfo == null || !selectedVendorGuid.HasValue || Session == null)
                return;

            ServerVendorItemsUpdated update = selectedVendorInfo.Build();
            update.VendorUnitId = selectedVendorGuid.Value;
            update.SellPriceMultiplier *= VendorSellPriceMultiplier;
            update.BuyPriceMultiplier  *= VendorBuyPriceMultiplier;
            Session.EnqueueMessageEncrypted(update);
        }

        public uint? StarterTutorialDepartureTerminalCreatureId { get; private set; }

        private bool forceSave;
        private bool saveInProgress;
        private bool saveRequestedDuringSave;
        private Action deferredSaveCallback;
        private Action deferredSaveFailureCallback;
        private UpdateTimer saveTimer;
        private PlayerSaveMask saveMask;

        private Dictionary<Property, Dictionary<ItemSlot, /*value*/float>> itemProperties = new();

        private UpdateTimer relocationTimer = new(TimeSpan.FromSeconds(1));
        private UpdateTimer ghostSpawnTimer;
        private UpdateTimer pvpFlagDisableTimer;
        private DateTime? pvpFlagDisableUntilUtc;

        #region Dependency Injection

        private readonly IInternalMessagePublisher messagePublisher;
        private readonly IEntityFactory entityFactory;
        private readonly IMatchingManager matchingManager;
        private readonly IMatchManager matchManager;
        private readonly IGameTableManager gameTableManager;
        private readonly IGlobalLootManager globalLootManager;
        private readonly IGroupStateManager groupStateManager;
        private readonly ICleanupManager cleanupManager;
        private readonly IBuybackManager buybackManager;
        private readonly IGlobalAchievementManager globalAchievementManager;
        private readonly IGlobalGuildManager globalGuildManager;
        private readonly IGlobalQuestManager globalQuestManager;
        private readonly IGlobalSpellManager globalSpellManager;
        private readonly ICustomisationManager customisationManager;
        private readonly IDuelManager duelManager;
        private readonly IGlobalStorefrontManager globalStorefrontManager;
        private readonly IGlobalResidenceManager globalResidenceManager;
        private readonly IMapManager mapManager;
        private readonly ITextFilterManager textFilterManager;
        private readonly IPrerequisiteManager prerequisiteManager;
        private readonly IShutdownManager shutdownManager;
        private readonly IGlobalChatManager globalChatManager;
        private readonly IFactionManager factionManager;
        private readonly RealmBankManager realmBankManager;
        private readonly IDisableManager disableManager;
        private readonly IAssetManager assetManager;
        private readonly ICharacterManager characterManager;
        private readonly IScriptManager scriptManager;
        private readonly IRealmContext realmContext;
        private readonly IPlayerManager playerManager;
        private readonly IItemManager itemManager;
        private readonly IDatabaseManager databaseManager;
        private readonly ISharedConfiguration sharedConfiguration;
        private readonly IContractManager contractManager;

        public Player(
            IMovementManager movementManager,
            IInternalMessagePublisher messagePublisher,
            IEntityFactory entityFactory,
            IMatchingManager matchingManager,
            IMatchManager matchManager,
            IGameTableManager gameTableManager,
            ICurrencyManager currencyManager,
            IGlobalLootManager globalLootManager = null,
            IGroupStateManager groupStateManager = null,
            ICleanupManager cleanupManager = null,
            IBuybackManager buybackManager = null,
            IGlobalAchievementManager globalAchievementManager = null,
            IGlobalGuildManager globalGuildManager = null,
            IGlobalQuestManager globalQuestManager = null,
            IGlobalSpellManager globalSpellManager = null,
            ICustomisationManager customisationManager = null,
            IDuelManager duelManager = null,
            IGlobalStorefrontManager globalStorefrontManager = null,
            IGlobalResidenceManager globalResidenceManager = null,
            IMapManager mapManager = null,
            ITextFilterManager textFilterManager = null,
            IPrerequisiteManager prerequisiteManager = null,
            IShutdownManager shutdownManager = null,
            IGlobalChatManager globalChatManager = null,
            IFactionManager factionManager = null,
            RealmBankManager realmBankManager = null,
            IDisableManager disableManager = null,
            IAssetManager assetManager = null,
            ICharacterManager characterManager = null,
            IScriptManager scriptManager = null,
            IRealmContext realmContext = null,
            IPlayerManager playerManager = null,
            IItemManager itemManager = null,
            IDatabaseManager databaseManager = null,
            ISharedConfiguration sharedConfiguration = null,
            IContractManager contractManager = null,
            IGuildManager guildManager = null)
            : base(movementManager, sharedConfiguration)
        {
            this.messagePublisher = messagePublisher;
            this.entityFactory    = entityFactory;
            this.matchingManager  = matchingManager;
            this.matchManager     = matchManager;
            this.gameTableManager = gameTableManager;
            this.globalLootManager = globalLootManager;
            this.groupStateManager = groupStateManager;
            this.cleanupManager    = cleanupManager;
            this.buybackManager    = buybackManager;
            this.globalAchievementManager = globalAchievementManager;
            this.globalGuildManager = globalGuildManager;
            this.globalQuestManager = globalQuestManager;
            this.globalSpellManager = globalSpellManager;
            this.customisationManager = customisationManager;
            this.duelManager       = duelManager;
            this.globalStorefrontManager = globalStorefrontManager;
            this.globalResidenceManager = globalResidenceManager;
            this.mapManager        = mapManager;
            this.textFilterManager = textFilterManager;
            this.prerequisiteManager = prerequisiteManager;
            this.shutdownManager   = shutdownManager;
            this.globalChatManager = globalChatManager;
            this.factionManager    = factionManager;
            this.realmBankManager  = realmBankManager;
            this.disableManager    = disableManager;
            this.assetManager      = assetManager;
            this.characterManager  = characterManager;
            this.scriptManager     = scriptManager;
            this.realmContext      = realmContext;
            this.playerManager     = playerManager;
            this.itemManager       = itemManager;
            this.databaseManager   = databaseManager;
            this.sharedConfiguration = sharedConfiguration;
            this.contractManager   = contractManager;
            saveTimer              = new UpdateTimer(GetSaveDuration());

            InitialiseScriptManager(() => scriptManager);

            InitialiseRuntimeDependencies(
                null,
                null,
                () => disableManager,
                () => assetManager,
                () => prerequisiteManager,
                () => globalSpellManager);

            // managers
            CurrencyManager = currencyManager;
            GuildManager    = guildManager;
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
            presenceSessionEpoch = account.WorldSessionEpoch;
            presenceAdmission = account.NextPresenceAdmission();
            Identity          = new Abstract.Identity { Id = model.Id, RealmId = realmContext?.RealmId ?? (ushort)0 };
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
            InitialisePvPFlagDisable(model.PvpFlagDisableUntilUtc);
            LoadBindPoint(model.Bind);
            LoadTaxiNodes(model.TaxiNode);
            progressionDatabase = CharacterLoadOrigin.Get(model)?.Database;

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
                if (gameTableManager.TradeskillSchematic2.GetEntry(schematicModel.TradeskillSchematic2Id) == null)
                { unavailableSchematics.Add(ProgressionPersistence.FromModel(schematicModel)); continue; }

                schematics[schematicModel.TradeskillSchematic2Id] = SchematicState.FromModel(schematicModel);
            }

            foreach (CharacterStatModel statModel in model.Stat)
            {
                var statValue = new StatValue(statModel);
                stats.Add((Stat)statModel.Stat, statValue);
            }

            byte resolvedLevel = NexusForever.Game.Entity.XpManager.ResolveStoredLevel(model.Level, model.TotalXp, gameTableManager);
            if (Level > resolvedLevel)
                resolvedLevel = (byte)Math.Min(Level, byte.MaxValue);

            if (stats.TryGetValue(Stat.Level, out IStatValue levelStat))
            {
                if ((uint)levelStat.Value != resolvedLevel)
                    levelStat.Value = resolvedLevel;
            }
            else
            {
                stats.Add(Stat.Level, new StatValue(Stat.Level, (uint)resolvedLevel));
            }

            if (resolvedLevel != model.Level)
                saveMask |= PlayerSaveMask.Level;

            //SetStat(Stat.Health, 1);
            SetStat(Stat.Sheathed, 1u);
            // temp
            SetStat(Stat.Dash, 200F);
            // sprint
            SetStat(Stat.Resource0, 500f);

            CalculateDefaultProperties();
            SetBaseCharacterProperties();

            scriptCollection = GetScriptManager().InitialiseEntityScripts<IPlayer>(this);

            // managers
            EntitlementManager      = new CharacterEntitlementManager(this, model, assetManager, gameTableManager);
            Account.RewardPropertyManager.Initialise(this);

            CostumeManager          = new CostumeManager(this, model, itemManager, gameTableManager);
            Inventory               = new Inventory(this, model, realmBankManager, prerequisiteManager, itemManager, gameTableManager);
            CurrencyManager.Initialise(this, model);
            PathManager             = new PathManager(this, model, prerequisiteManager, gameTableManager, entityFactory);
            TitleManager            = new TitleManager(this, model, gameTableManager);
            SpellManager            = new SpellManager(this, model, prerequisiteManager, globalSpellManager, gameTableManager);
            SpellManager.Initialise();
            PetCustomisationManager = new PetCustomisationManager(this, model, gameTableManager, textFilterManager, prerequisiteManager);
            KeybindingManager       = new CharacterKeybindingManager(this, model);
            DatacubeManager         = new DatacubeManager(this, model, gameTableManager);
            GalacticArchiveManager  = new GalacticArchiveManager(this, model, gameTableManager);
            MailManager             = new MailManager(this, model, assetManager, characterManager, realmContext?.RealmId ?? Identity.RealmId, playerManager, itemManager, gameTableManager, sharedConfiguration, databaseManager?.GetDatabase<CharacterDatabase>() is { } mailDatabase ? mailDatabase.CreateContext : null);
            ZoneMapManager          = new ZoneMapManager(this, model, gameTableManager);
            ChallengeManager        = new ChallengeManager(this, model, gameTableManager, () => GetCharacterDatabase().CreateContext(), prerequisiteManager, itemManager,
                () => GetAuthDatabase().CreateContext());
            QuestManager            = new QuestManager(this, model, disableManager, assetManager, prerequisiteManager, globalQuestManager, scriptManager, gameTableManager, contractManager);
            AchievementManager      = new CharacterAchievementManager(this, model, groupStateManager, disableManager, globalAchievementManager, prerequisiteManager, playerManager, gameTableManager);
            SupplySatchelManager    = new SupplySatchelManager(this, model, gameTableManager);
            XpManager               = new XpManager(this, model, gameTableManager, sharedConfiguration);
            ReputationManager       = new ReputationManager(this, model, factionManager);
            GuildManager?.Initialise(this, model);
            ResidenceManager        = new ResidenceManager(this, globalResidenceManager, characterManager, globalSpellManager);
            CinematicManager        = new CinematicManager(this);

            LogoutManager           = new LogoutManager(this);
            LogoutManager.OnTimerFinished += Logout;

            AppearanceManager       = new AppearanceManager(this, model, customisationManager);
            ResurrectionManager     = new ResurrectionManager(this, sharedConfiguration, gameTableManager);

            // do dependant stat balance after all stats and properties have been set
            SetDependantStatBalance(true);
            foreach (IPropertyValue property in GetProperties())
                DependantStatBalance(property);

            playerManager?.AddPlayer(this);
        }

        private void SetBaseCharacterProperties()
        {
            var baseProperties  = GetCharacterManager().GetCharacterBaseProperties();
            var classProperties = GetCharacterManager().GetCharacterClassBaseProperties(Class);

            foreach (IPropertyModifier propertyValue in baseProperties.Concat(classProperties))
                SetBaseProperty(propertyValue.Property, propertyValue.GetValue(Level));
        }

        private ICharacterManager GetCharacterManager()
        {
            return characterManager ?? throw new InvalidOperationException("Player requires an ICharacterManager.");
        }

        private IGlobalQuestManager GetGlobalQuestManager()
        {
            return globalQuestManager ?? throw new InvalidOperationException("Player requires an IGlobalQuestManager.");
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
            UpdatePvPFlagDisable(lastTick);
            UpdateTaxiDiscovery(lastTick);

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
            if ((saveTimer.HasElapsed || forceSave) && !saveInProgress && !RewardInventorySaveBlocked)
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
            if (housingNameActor != null)
            { housingNameActor.Retired = true; housingNameActor.DisposeRequested = true; return; }
            if (bankActor != null)
            { bankActor.Retired = true; bankActor.DisposeRequested = true; return; }
            if (auctionBidActor != null)
            { auctionBidActor.Retired = true; auctionBidActor.DisposeRequested = true; return; }
            (CurrencyManager as CurrencyManager)?.InvalidateContractCredit();
            InvalidateRewardTrackPresentation();
            Inventory?.InvalidateRewardDeliveries();
            ChallengeManager?.InvalidateRewardOperations(true);
            lock (mapAdmission.SyncRoot)
            {
                CancelMapAdmission();
                base.Dispose();
                QuestManager.Dispose();
            }
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
            Save(callback, null);
        }

        private sealed record RecoverableSaveActor(ulong Character, IGameSession Session, EventQueue Events,
            IAccount Account, ulong? Epoch, IInventory Inventory, IQuestManager Quests, IChallengeManager Challenges,
            ISupplySatchelManager Satchel);
        private RecoverableSaveActor pendingConversionSave;

        internal void SaveResourceConversion(Action callback)
        {
            pendingConversionSave ??= CaptureRecoverableSaveActor();
            Save(callback);
        }

        private RecoverableSaveActor CaptureRecoverableSaveActor()
            => new(CharacterId, Session, Session.Events, Account, Account?.WorldSessionEpoch, Inventory, QuestManager, ChallengeManager, SupplySatchelManager);

        private bool OwnsRecoverableSaveActor(RecoverableSaveActor actor)
            => CharacterId == actor.Character && ReferenceEquals(Session, actor.Session) && ReferenceEquals(actor.Session.Events, actor.Events)
                && ReferenceEquals(Account, actor.Account) && Account?.WorldSessionEpoch == actor.Epoch
                && ReferenceEquals(Inventory, actor.Inventory) && ReferenceEquals(QuestManager, actor.Quests)
                && ReferenceEquals(ChallengeManager, actor.Challenges)
                && ReferenceEquals(SupplySatchelManager, actor.Satchel)
                && (actor.Session is not IPlayerSession bound || ReferenceEquals(bound.Player, this))
                && actor.Session is not NetworkSession { IsDisconnecting: true }
                && LogoutManager?.State is not (LogoutState.Logout or LogoutState.Finished);

        private bool OwnsRecoverableSave(NormalRewardSave snapshot, RecoverableSaveActor actor)
            => saveInProgress && ReferenceEquals(normalRewardSave, snapshot) && OwnsRecoverableSaveActor(actor);

        private void Save(Action callback, Action failureCallback)
        {
            if (QuestManager is QuestManager { IsContractCompletionMutationInProgress: true }
                or QuestManager { IsOrdinaryCompletionMutationInProgress: true }
                or QuestManager { IsPublicEventQuestMutationInProgress: true })
            {
                DeferInventorySave(callback, failureCallback); return;
            }
            if (saveInProgress)
            {
                saveRequestedDuringSave = true;
                deferredSaveCallback        += callback;
                deferredSaveFailureCallback += failureCallback;
                return;
            }

            StartSave(callback, failureCallback);
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
            if (!TryAcquireInventorySave(out var lease))
                throw new InvalidOperationException("Direct player save is blocked by inventory work before Auth enrollment.");
            using var held = lease;
            var snapshot = CaptureNormalRewardSave(); bool saved = false;
            System.Threading.Interlocked.Increment(ref directSaveOperations);
            try
            {
                await GetAuthDatabase().Save(context => SaveFrozenAuth(context, snapshot));
                await SaveCharacterUnit(snapshot); saved = true;
            }
            finally
            {
                try
                {
                    bool released = snapshot?.Finish(saved) ?? saved;
                    if (snapshot?.Quests.HasContractCompletion == true)
                    {
                        if (saved && released) Session.CanProcessIncomingPackets = true;
                        else Session.ForceDisconnect();
                    }
                    if (!released && saved) throw new InvalidOperationException("Direct player save did not release its frozen ownership.");
                }
                finally { System.Threading.Interlocked.Decrement(ref directSaveOperations); }
            }
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

        private void StartSave(Action callback = null, Action failureCallback = null)
        {
            var recoverableActor = pendingConversionSave;
            if (recoverableActor != null && !OwnsRecoverableSaveActor(recoverableActor))
            {
                HandleSaveFailure("recoverable save owner", failureCallback, recoverableActor);
                return;
            }

            if (!TryAcquireInventorySave(out var lease))
            {
                if (RewardStateRequiresReload)
                    HandleSaveFailure("inventory", failureCallback, recoverableActor);
                else DeferInventorySave(callback, failureCallback);
                return;
            }
            inventorySaveLease = lease;
            callback += deferredSaveCallback;
            failureCallback += deferredSaveFailureCallback;
            deferredSaveCallback = deferredSaveFailureCallback = null;
            NormalRewardSave snapshot;
            try { snapshot = CaptureNormalRewardSave(); }
            catch { HandleSaveFailure("snapshot", failureCallback, recoverableActor); throw; }
            OrdinaryCharacterSaveReconciliation recoveryReceipt = null;
            if (recoverableActor != null)
                recoveryReceipt = new ResourceConversionSaveReconciliation(System.Guid.NewGuid().ToString("N"), recoverableActor.Character);
            else if (snapshot?.CanRecoverPublicEventCredit == true)
            {
                recoverableActor = CaptureRecoverableSaveActor();
                recoveryReceipt = new PublicEventQuestSaveReconciliation(System.Guid.NewGuid().ToString("N"), recoverableActor.Character);
            }
            if (recoveryReceipt != null && snapshot != null) snapshot.Challenges.RetainBidRollback = true;
            pendingConversionSave = null;
            saveRequestedDuringSave = false;
            saveInProgress = true;
            saveTimer.Reset(false);

            // prevent packets from being processed until asynchronous player save task is complete
            Session.CanProcessIncomingPackets = false;

            Task authSaveTask;
            try { authSaveTask = GetAuthDatabase().Save(context => SaveFrozenAuth(context, snapshot)); }
            catch
            {
                HandleSaveFailure("auth", failureCallback, recoverableActor);
                throw;
            }
            (recoverableActor?.Events ?? Session.Events).EnqueueEvent(new TaskEvent(authSaveTask,
                () =>
                {
                    if (recoverableActor != null && !OwnsRecoverableSave(snapshot, recoverableActor))
                    {
                        HandleSaveFailure("recoverable save owner", failureCallback, recoverableActor);
                        return;
                    }

                    Task characterSaveTask;
                    try { characterSaveTask = SaveCharacterUnit(snapshot, recoveryReceipt); }
                    catch
                    {
                        HandleSaveFailure("character", failureCallback, recoverableActor);
                        throw;
                    }
                    (recoverableActor?.Events ?? Session.Events).EnqueueEvent(new TaskEvent(characterSaveTask,
                        () => CompleteSave(callback, failureCallback, recoverableActor, snapshot),
                        () => HandleCharacterSaveFailure(characterSaveTask, snapshot, callback, failureCallback,
                            recoveryReceipt, recoverableActor)));
                },
                () => HandleSaveFailure("auth", failureCallback, recoverableActor)));
        }

        internal AuthDatabase GetAuthDatabase()
        {
            AuthDatabase authDatabase = databaseManager?.GetDatabase<AuthDatabase>();
            return authDatabase ?? throw new InvalidOperationException("AuthDatabase is not available.");
        }

        internal CharacterDatabase GetCharacterDatabase()
        {
            CharacterDatabase characterDatabase = databaseManager?.GetDatabase<CharacterDatabase>();
            return characterDatabase ?? throw new InvalidOperationException("CharacterDatabase is not available.");
        }

        private void HandleCharacterSaveFailure(Task task, NormalRewardSave snapshot, Action callback, Action failureCallback,
            OrdinaryCharacterSaveReconciliation recoveryReceipt = null, RecoverableSaveActor recoverableActor = null)
        {
            var rollback = task.Exception?.Flatten().InnerExceptions.OfType<CharacterReconciliationRolledBackException>().SingleOrDefault();
            if (recoverableActor != null)
            {
                if (rollback?.ReceiptId == recoveryReceipt.Id && OwnsRecoverableSave(snapshot, recoverableActor)
                    && snapshot?.RetryConfirmedOrdinaryRollback(rollback) == true)
                {
                    recoverableActor.Events.EnqueueEvent(new DelayEvent(TimeSpan.FromSeconds(1), () =>
                    {
                        if (!OwnsRecoverableSave(snapshot, recoverableActor))
                        {
                            HandleSaveFailure("recoverable save owner", failureCallback, recoverableActor);
                            return;
                        }

                        // Retry the same frozen writes once after proven rollback, with a fresh receipt ID.
                        var retryReceipt = recoveryReceipt with { Id = System.Guid.NewGuid().ToString("N") };
                        Task retry;
                        try { retry = SaveCharacterUnit(snapshot, retryReceipt); }
                        catch (Exception error)
                        {
                            log.Error(error, "Failed to enroll a recoverable Character save retry.");
                            HandleSaveFailure("recovery enrollment", failureCallback, recoverableActor);
                            return;
                        }
                        recoverableActor.Events.EnqueueEvent(new TaskEvent(retry,
                            () => CompleteSave(callback, failureCallback, recoverableActor, snapshot),
                            () => HandleSaveFailure("recovery retry", failureCallback, recoverableActor)));
                    }));
                    return;
                }
                HandleSaveFailure("recoverable Character", failureCallback, recoverableActor);
                return;
            }
            if (rollback != null && (rollback is BidReconciliationRolledBackException bid && AuctionBidSaveAllowed && snapshot?.RetryBidRollback(bid) == true
                || BankSaveAllowed && snapshot?.RetryBankRollback(rollback) == true))
            {
                // Preserve the original normal snapshot, inventory/Character
                // leases and already applied earning effects. Only persistence
                // enrollment runs again, on this original actor, after proof.
                Session.Events.EnqueueEvent(new DelayEvent(TimeSpan.FromSeconds(1), () =>
                {
                    if (!AuctionBidSaveAllowed && !BankSaveAllowed) { HandleSaveFailure("economic owner", failureCallback); return; }
                    Task retry;
                    try { retry = SaveCharacterUnit(snapshot); }
                    catch { HandleSaveFailure("bid enrollment", failureCallback); throw; }
                    Session.Events.EnqueueEvent(new TaskEvent(retry, () => CompleteSave(callback, failureCallback),
                        () => HandleCharacterSaveFailure(retry, snapshot, callback, failureCallback)));
                }));
                return;
            }
            HandleSaveFailure("character", failureCallback);
        }

        private void CompleteSave(Action callback, Action failureCallback, RecoverableSaveActor recoverableActor = null,
            NormalRewardSave snapshot = null)
        {
            if (recoverableActor != null && !OwnsRecoverableSave(snapshot, recoverableActor))
            {
                HandleSaveFailure("recoverable save owner", failureCallback, recoverableActor);
                return;
            }
            if (!ReleaseNormalRewardSave(true)) { HandleSaveFailure("snapshot", failureCallback, recoverableActor); return; }
            ReleaseInventorySave();
            if (auctionBidActor == null && bankActor == null && housingNameActor == null) Session.CanProcessIncomingPackets = true;
            saveInProgress                    = false;
            saveTimer.Resume();

            if (saveRequestedDuringSave)
            {
                saveRequestedDuringSave = false;
                Action chainedCallback = callback;
                chainedCallback += deferredSaveCallback;
                deferredSaveCallback = null;

                Action chainedFailureCallback = failureCallback;
                chainedFailureCallback += deferredSaveFailureCallback;
                deferredSaveFailureCallback = null;

                StartSave(chainedCallback, chainedFailureCallback);
                return;
            }

            callback?.Invoke();
        }

        private void HandleSaveFailure(string phase, Action failureCallback, RecoverableSaveActor recoverableActor = null)
        {
            var failedSession = recoverableActor?.Session ?? Session;
            bool OwnsFailedSession() => recoverableActor != null ? OwnsRecoverableSaveActor(recoverableActor)
                : ReferenceEquals(Session, failedSession) && (failedSession is not IPlayerSession current || ReferenceEquals(current.Player, this));
            ReleaseNormalRewardSave(false);
            ReleaseInventorySave();
            log.Error("Failed to save {0} data for character {1}.", phase, CharacterId);
            Action chainedFailureCallback = failureCallback;
            chainedFailureCallback += deferredSaveFailureCallback;

            saveInProgress                    = false;
            saveRequestedDuringSave           = false;
            pendingConversionSave              = null;
            deferredSaveCallback              = null;
            deferredSaveFailureCallback       = null;
            if (OwnsFailedSession() && auctionBidActor == null && bankActor == null && housingNameActor == null) failedSession.CanProcessIncomingPackets = true;
            saveTimer.Resume();

            try
            {
                chainedFailureCallback?.Invoke();
            }
            catch (Exception e)
            {
                log.Error(e, "Failed to execute save failure callback for character {0}.", CharacterId);
            }

            if (OwnsFailedSession()) failedSession.ForceDisconnect();
        }

        /// <summary>
        /// Save database changes for <see cref="Player"/> to <see cref="AuthContext"/>.
        /// </summary>
        public void Save(AuthContext context)
        {
            CheckInventorySaveAllowed();
            Account.Save(context);
        }

        /// <summary>
        /// Save database changes for <see cref="Player"/> to <see cref="CharacterContext"/>.
        /// </summary>
        public void Save(CharacterContext context) => SaveCharacter(context, null);

        private Task SaveCharacterUnit(NormalRewardSave snapshot, OrdinaryCharacterSaveReconciliation recoveryReceipt = null)
        {
            if (snapshot?.HousingReceipt is { } housingReceipt)
                return GetCharacterDatabase().SaveHousingRenameReconciliation(CharacterId, context =>
                {
                    SaveCharacter(context, snapshot); housingNameActor.StageNames(context);
                }, housingReceipt);
            if (snapshot?.BankReceipt is { } bankReceipt)
                return GetCharacterDatabase().SaveBankReconciliation(CharacterId, context => SaveCharacter(context, snapshot), bankReceipt);
            if (snapshot?.BidReceipt is { } receipt)
                return GetCharacterDatabase().SaveBidReconciliation(CharacterId, context => SaveCharacter(context, snapshot), receipt);
            if (recoveryReceipt is ResourceConversionSaveReconciliation conversionReceipt)
                return GetCharacterDatabase().SaveResourceConversionReconciliation(CharacterId, context => SaveCharacter(context, snapshot), conversionReceipt);
            if (recoveryReceipt is PublicEventQuestSaveReconciliation publicEventReceipt)
                return GetCharacterDatabase().SavePublicEventQuestReconciliation(CharacterId, context => SaveCharacter(context, snapshot), publicEventReceipt);
            return snapshot?.Quests.HasContractCompletion == true || CurrencyManager is CurrencyManager
                ? GetCharacterDatabase().SaveAtomic(CharacterId, context => SaveCharacter(context, snapshot))
                : GetCharacterDatabase().Save(context => SaveCharacter(context, snapshot));
        }

        private void SaveCharacter(CharacterContext context, NormalRewardSave snapshot)
        {
            try { GetExperiencePersistence(); snapshot?.Validate(); CheckInventorySaveAllowed(frozen: snapshot != null); snapshot?.Quests.BeginContractSave(context); snapshot?.Challenges.BeginContractSave(context); snapshot?.StageDelivery(context); }
            catch (Exception error) { context.RejectSave(error); throw; }
            SaveBindPoint(context);
            if (snapshot == null) SaveTaxiNodes(context); else SaveTaxiPersistence(context, snapshot.TaxiFareVersion);
            var model = new CharacterModel
            {
                Id = CharacterId
            };

            EntityEntry<CharacterModel> entity = context.Attach(model);

            if (saveMask != PlayerSaveMask.None)
            {
                var location = (saveMask & PlayerSaveMask.Location) != 0 ? CaptureMapLocationForSave(snapshot?.TaxiFareVersion) : null;
                if (location is { } current)
                {
                    model.LocationX = current.Position.X;
                    entity.Property(p => p.LocationX).IsModified = true;

                    model.LocationY = current.Position.Y;
                    entity.Property(p => p.LocationY).IsModified = true;

                    model.LocationZ = current.Position.Z;
                    entity.Property(p => p.LocationZ).IsModified = true;

                    model.RotationX = current.Rotation.X;
                    entity.Property(p => p.RotationX).IsModified = true;

                    model.RotationY = current.Rotation.Y;
                    entity.Property(p => p.RotationY).IsModified = true;

                    model.RotationZ = current.Rotation.Z;
                    entity.Property(p => p.RotationZ).IsModified = true;

                    model.WorldId = current.World;
                    entity.Property(p => p.WorldId).IsModified = true;

                    model.WorldZoneId = current.Zone;
                    entity.Property(p => p.WorldZoneId).IsModified = true;
                }

                if ((saveMask & PlayerSaveMask.Path) != 0 && experiencePathOwner == null)
                {
                    model.ActivePath = (uint)Path;
                    entity.Property(p => p.ActivePath).IsModified = true;
                    model.PathActivatedTimestamp = PathActivatedTime;
                    entity.Property(p => p.PathActivatedTimestamp).IsModified = true;
                }

                if ((saveMask & PlayerSaveMask.InputKeySet) != 0 && KeybindingManager is not CharacterKeybindingManager)
                {
                    model.InputKeySet = (sbyte)InputKeySet;
                    entity.Property(p => p.InputKeySet).IsModified = true;
                }

                if ((saveMask & PlayerSaveMask.Flags) != 0)
                {
                    model.Flags = (uint)Flags;
                    entity.Property(p => p.Flags).IsModified = true;
                }

                if ((saveMask & PlayerSaveMask.Level) != 0 && experienceXpOwner == null)
                {
                    model.Level = checked((byte)Level);
                    entity.Property(p => p.Level).IsModified = true;
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

                if ((saveMask & PlayerSaveMask.PvP) != 0)
                {
                    model.PvpFlagDisableUntilUtc = pvpFlagDisableUntilUtc;
                    entity.Property(p => p.PvpFlagDisableUntilUtc).IsModified = true;
                }

                // Keep location dirty when this save only staged the other character fields.
                saveMask = location.HasValue ? PlayerSaveMask.None : saveMask & PlayerSaveMask.Location;
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

            if (snapshot == null) Inventory.Save(context); else snapshot.SaveInventory(context);
            if (snapshot == null) CurrencyManager.Save(context); else snapshot.SaveCurrencies(context);
            if (snapshot == null) SaveExperience(context); else snapshot.SaveExperience(context);
            if (snapshot == null) TitleManager.Save(context); else snapshot.SaveTitles(context);
            if (snapshot == null) CostumeManager.Save(context); else snapshot.SaveWardrobe(context);
            if (experiencePetOwner == null) PetCustomisationManager.Save(context); else experiencePetOwner.SaveFlairs(context);
            if (snapshot == null) KeybindingManager.Save(context); else snapshot.SaveKeybindings(context);
            if (experienceSpellOwner == null) SpellManager.Save(context);
            if (experienceDatacubeOwner == null) DatacubeManager.Save(context);
            if (experienceArchiveOwner == null) GalacticArchiveManager.Save(context);
            MailManager.Save(context);
            if (experienceMapOwner == null) ZoneMapManager.Save(context);
            if (snapshot == null) QuestManager.Save(context); else snapshot.Quests.Save(context);
            if (experienceAchievementOwner == null) AchievementManager.Save(context);
            if (ChallengeManager is ChallengeManager challengeManager)
            {
                if (snapshot == null) challengeManager.Save(context); else snapshot.Challenges.Save(context);
            }
            if (snapshot == null) SupplySatchelManager.Save(context); else snapshot.SaveSatchel(context);
            if (snapshot == null) SaveProgression(context); else snapshot.SaveProgression(context);
            if (experienceReputationOwner == null) ReputationManager.Save(context);
            GuildManager.Save(context);
            EntitlementManager.Save(context);
            AppearanceManager.Save(context);
            snapshot?.Quests.EndContractSave(context);
        }

        protected override IEntityModel BuildEntityModel()
        {
            return new PlayerEntityModel
            {
                Id        = CharacterId,
                RealmId   = Identity.RealmId,
                Name      = Name,
                Race      = Race,
                Class     = Class,
                Sex       = Sex,
                Bones     = AppearanceManager.GetBones()
                    .Select(b => b.BoneValue)
                    .ToList(),
                Title     = TitleManager is TitleManager titles ? titles.PublishedActiveTitle : TitleManager.ActiveTitleId,
                GuildIds  = GuildManager
                    .Select(g => g.Id)
                    .ToList(),
                GuildName = GuildManager.GuildAffiliation?.Name,
                GuildType = GuildManager.GuildAffiliation?.Type ?? GuildType.None,
                PvPFlag   = PvPFlag,

                // We use Group 1 as the "dominant group"
                GroupId   = ClientGroupAssociation
            };
        }

        public override void OnAddToMap(IBaseMap map, uint guid, Vector3 vector)
        {
            lock (mapAdmission.SyncRoot)
            {
                ulong? generation = mapAdmission.BeginBootstrap();
                if (!generation.HasValue)
                    throw new InvalidOperationException("Player map addition requires a current queued admission.");
                OnAddToMapCore(map, guid, vector, generation.Value);
            }
        }

        private void OnAddToMapCore(IBaseMap map, uint guid, Vector3 vector, ulong generation)
        {
            IsLoading = true;
            deferredLoadingWorldZoneRuntimeSync = false;

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
            if (!IsAdmissionAllowed() || !mapAdmission.IsCurrent(generation))
                return;

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
            if (!IsAdmissionAllowed() || !mapAdmission.IsCurrent(generation))
                return;

            if (!IsAlive)
                OnDeath();

            if (!IsAdmissionAllowed() || !mapAdmission.IsCurrent(generation))
                return;
            if (PreviousMap == null)
                OnLogin(generation);
            if (!IsAdmissionAllowed() || !mapAdmission.IsCurrent(generation))
                return;

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
            PathManager.CompleteCurrentExplorerExploreZoneMission();
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

            Creature2Entry creatureEntry = gameTableManager.Creature2.GetEntry(vendor.CreatureId);
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
                TextTable tt = gameTableManager.GetTextTable(Language.English);
                if (tt != null)
                {
                    globalChatManager.SendMessage(Session, $"New Zone: ({Zone.Id}){tt.GetEntry(Zone.LocalizedTextIdName)}");
                }

                uint tutorialId = assetManager?.GetTutorialIdForZone(Zone.Id) ?? 0u;
                if (tutorialId > 0)
                {
                    Session.EnqueueMessageEncrypted(new ServerTutorial
                    {
                        TutorialId = tutorialId
                    });
                }

                if (IsLoading)
                    deferredLoadingWorldZoneRuntimeSync = true;
                else
                    SyncWorldZoneRuntimeState();
            }

            QuestManager?.Communicator?.OnZone(Zone?.Id ?? 0);
            ZoneMapManager.OnZoneUpdate();

            messagePublisher.PublishAsync(new PlayerWorldZoneUpdatedMessage
            {
                Identity    = Identity.ToInternalIdentity(),
                WorldZoneId = (ushort)Zone?.Id,
            }).FireAndForgetAsync();
        }

        private void SendDeferredInWorldStorefrontCatalog()
        {
            StorePurchaseHistoryManager.SendPurchaseHistory(Account, databaseManager);
            globalStorefrontManager?.SendBootstrapCatalogPacketsIfNeeded(Session, Account.Id);
        }

        internal bool CanSendDeferredInWorldStorefrontCatalog()
        {
            return !IsLoading;
        }

        private void SendPacketsAfterAddToMap()
        {
            if (ReputationManager is ReputationManager reputation)
                reputation.SendInitialStanding(SendMapPacketBurst);
            else
                SendMapPacketBurst(ReputationManager.Select(row => new ServerPlayerCreate.Faction.FactionReputation
                    { FactionId = row.Id, ReputationAmount = row.Amount }).ToList());
        }

        private void SendMapPacketBurst(List<ServerPlayerCreate.Faction.FactionReputation> standing)
        {
            DateTime start = DateTime.UtcNow;

            SendInGameTime();
            buybackManager?.SendBuybackItems(this);

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
            SendTaxiNodes();

            var playerCreate = new ServerPlayerCreate
            {
                ItemProficiencies = GetItemProficiencies(),
                FactionData       = new ServerPlayerCreate.Faction
                {
                    FactionId = Faction1, // This does not do anything for the player's "main" faction. Exiles/Dominion
                    FactionReputations = standing
                },
                ActiveCostumeIndex    = CostumeManager.CostumeIndex ?? -1,
                BindPointId           = bindPointId,
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
            if (AchievementManager is CharacterAchievementManager characterAchievementManager)
                characterAchievementManager.GrantCompletedTradeskillAchievementRewards(this);
            SendTradeskillInitialPackets();
            AchievementManager.SendInitialPackets(null);
            Account.RewardPropertyManager.SendInitialPackets();
            Account.LoyaltyManager?.SendInitialPackets(this);
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

            // Do not send 0988/098B/0987 in the same enqueue pass as ServerPlayerCreate and the rest of the
            // login burst; the client can drop or mis-apply store payloads while applying create data.
            Session.Events.EnqueueEvent(new PredicateEvent(
                CanSendDeferredInWorldStorefrontCatalog,
                SendDeferredInWorldStorefrontCatalog));

            log.Trace($"Player {Name} took {(DateTime.UtcNow - start).TotalMilliseconds}ms to send packets after add to map.");
        }

        public ItemProficiency GetItemProficiencies()
        {
            ClassEntry classEntry = gameTableManager.Class.GetEntry((ulong)Class);
            return (ItemProficiency)classEntry.StartingItemProficiencies;
        }

        public override void OnRemoveFromMap()
        {
            ClearPetMiniBars(sendPackets: true);
            DestroyDependents();
            base.OnRemoveFromMap();
        }

        public override void OnSummon(IWorldEntity entity)
        {
            base.OnSummon(entity);
            PathManager?.OnScientistScanbotSummoned(entity);
            SpellHandler.OnEngineerCombatBotSummoned(this, entity);
        }

        public override void OnUnsummon(IWorldEntity entity)
        {
            PathManager?.OnScientistScanbotUnsummoned(entity);
            base.OnUnsummon(entity);
            SpellHandler.UnregisterEngineerCombatBotActions(this, entity);
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
            lock (mapAdmission.SyncRoot)
            {
                CancelMapAdmission();
                OnLogout();
                Cleanup();
            }
        }

        private void Cleanup()
        {
            lock (mapAdmission.SyncRoot)
            {
                CancelMapAdmission();
                CleanupCore();
            }
        }

        private void CleanupCore()
        {
            if (housingNameActor != null)
            { housingNameActor.Retired = true; housingNameActor.CleanupRequested = true; return; }
            if (bankActor != null)
            { bankActor.Retired = true; bankActor.CleanupRequested = true; return; }
            if (auctionBidActor != null)
            { auctionBidActor.Retired = true; auctionBidActor.CleanupRequested = true; return; }
            InvalidateRewardTrackPresentation();
            Inventory?.InvalidateRewardDeliveries();
            ChallengeManager?.InvalidateRewardOperations();
            QuestManager?.InvalidateRewardOperations();
            log.Trace($"Cleanup for character {Name}({CharacterId})...");

            // Character-select packets queued during logout must wait until this player is fully detached.
            Session.CanProcessIncomingPackets = false;

            playerManager?.RemovePlayer(this);
            cleanupManager?.AddPlayer(this);

            log.Trace($"Waiting to cleanup character {Name}({CharacterId})...");

            Session.Events.EnqueueEvent(new TimeoutPredicateEvent(TimeSpan.FromSeconds(15), CanCleanup,
                () =>
            {
                int cleanupCompleted = 0, cleanupFinishing = 0;
                void CompleteCleanup()
                {
                    if (System.Threading.Interlocked.Exchange(ref cleanupCompleted, 1) != 0) return;
                    cleanupManager?.RemovePlayer(this);
                    log.Trace($"Cleanup for character {Name}({CharacterId}) has completed.");

                    LogoutManager.State = LogoutState.Finished;
                }

                void DisposeLoggedOutPlayer()
                {
                    if (Map != null)
                        RemoveFromMap();

                    messagePublisher.PublishAsync(new PlayerLoggedOutMessage
                    {
                        AccountId = Account.Id,
                        SessionEpoch = presenceSessionEpoch,
                        Admission = presenceAdmission,
                        OccurredUtc = DateTime.UtcNow,
                        Identity = Identity.ToInternalIdentity()
                    }).FireAndForgetAsync();

                    Dispose();
                }

                void FinishCleanup()
                {
                    if (System.Threading.Interlocked.Exchange(ref cleanupFinishing, 1) != 0) return;
                    try
                    {
                        DisposeLoggedOutPlayer();
                    }
                    finally
                    {
                        CompleteCleanup();
                    }
                }

                log.Trace($"Cleanup for character {Name}({CharacterId}) has started...");

                try
                {
                    if (CurrencyManager is CurrencyManager { RequiresContractCurrencyReload: true } or CurrencyManager { HasPendingContractCurrency: true }
                        || ChallengeManager?.RequiresAuthoritativeReload == true || ChallengeManager?.HasPendingRewardOperation == true
                        || QuestManager?.RequiresAuthoritativeReload == true || QuestManager?.HasPendingRewardOperation == true
                        || Inventory?.SupportsTransactionalSoulboundDelivery == true
                        && (Inventory.RequiresAuthoritativeReload || Inventory.HasPendingRewardDelivery))
                        FinishCleanup(); // timeout/quarantine: never save stale Item state or start Auth first
                    else Save(FinishCleanup, FinishCleanup);
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
            lock (mapAdmission.SyncRoot)
                return pendingTeleport == null && !mapAdmission.BootstrapGeneration.HasValue
                    && (CurrencyManager is not CurrencyManager { HasPendingContractCurrency: true } || ((CurrencyManager)CurrencyManager).RequiresContractCurrencyReload)
                    && (ChallengeManager?.HasPendingRewardOperation != true || ChallengeManager.RequiresAuthoritativeReload)
                    && (QuestManager?.HasPendingRewardOperation != true || QuestManager.RequiresAuthoritativeReload)
                    && (Inventory?.HasPendingRewardDelivery != true || Inventory.RequiresAuthoritativeReload);
        }

        private void OnLogin(ulong generation)
        {
            scriptCollection.Invoke<IPlayerScript>(s => s.OnLogin());
            if (!IsAdmissionAllowed() || !mapAdmission.IsCurrent(generation))
                return;

            string motd = realmContext?.Motd;
            if (motd?.Length > 0)
                globalChatManager.SendMessage(Session, motd, "MOTD", ChatChannelType.Realm);

            GuildManager.OnLogin();
            if (!IsAdmissionAllowed() || !mapAdmission.IsCurrent(generation))
                return;

            shutdownManager?.OnLogin(this);
            if (!IsAdmissionAllowed() || !mapAdmission.IsCurrent(generation))
                return;

            matchingManager.OnLogin(this);
            if (!IsAdmissionAllowed() || !mapAdmission.IsCurrent(generation))
                return;
            matchManager.OnLogin(this);
            if (!IsAdmissionAllowed() || !mapAdmission.IsCurrent(generation))
                return;
            SendPendingPvPFlagDisableCooldown();

            IsOnline = true;
            forceSave = true;

            messagePublisher.PublishAsync(new PlayerLoggedInMessage
            {
                SessionEpoch = presenceSessionEpoch,
                Admission = presenceAdmission,
                OccurredUtc = DateTime.UtcNow,
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
            WorldEntry entry = gameTableManager.World.GetEntry(worldId);
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
            lock (mapAdmission.SyncRoot)
            {
                if (!IsAdmissionAllowed())
                    return;
                TeleportToCore(mapPosition, reason);
            }
        }

        private void TeleportToCore(IMapPosition mapPosition, TeleportReason reason, PendingTaxiFlight taxiFlight = null,
            Vector3? arrivalRotation = null, PendingMatchTransfer matchTransfer = null, uint? carriedVanityPetId = null)
        {
            if (!CanTeleport())
            {
                SendGenericError(GenericError.InstanceTransferPending);
                return;
            }

            if (disableManager?.IsDisabled(DisableType.World, mapPosition.Info.Entry.Id) == true)
            {
                SendSystemMessage($"Unable to teleport to world {mapPosition.Info.Entry.Id} because it is disabled.");
                return;
            }

            // store vanity pet summoned before teleport so it can be summoned again after being added to the new map
            uint? vanityPetId = carriedVanityPetId;
            if (VanityPetGuid != null)
            {
                IPetEntity pet = GetVisible<IPetEntity>(VanityPetGuid.Value);
                vanityPetId = pet?.CreatureId ?? vanityPetId;
            }

            pendingTeleport = new PendingTeleport
            {
                Reason        = reason,
                MapPosition   = mapPosition,
                VanityPetId   = vanityPetId,
                TaxiFlight    = taxiFlight,
                MatchTransfer = matchTransfer
            };

            if (arrivalRotation.HasValue) Rotation = arrivalRotation.Value;

            SetControl(null);

            if (mapManager == null)
                throw new InvalidOperationException("Player requires an IMapManager to teleport.");

            CaptureOpenWorldReturnPosition();
            if (PlatformGuid != null)
                SetPlatform(null);
            mapManager.AddToMap(this, mapPosition);
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
            if (PlatformGuid != null)
                SetPlatform(null);
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
            lock (mapAdmission.SyncRoot)
            {
                if (!IsAdmissionAllowed())
                    return;
                OnTeleportToFailedCore(error);
            }
        }

        private void OnTeleportToFailedCore(GenericError error)
        {
            pendingTeleport = null;
            matchReturnAfterEntry = null;
            IsLoading = false;
            if (Map != null)
            {
                mapAdmission.AbortQueue();
                SendGenericError(error);

                SetControl(this);

                log.Trace($"Error {error} occured during teleport for {Name}({CharacterId})!");
            }
            else
            {
                // Login or a transfer that has already left the source map cannot resume in place.
                mapAdmission.Cancel();
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
            lock (mapAdmission.SyncRoot)
            {
                if (IsAdmissionAllowed() && IsLoading && mapAdmission.RequestEntry(Map != null && Guid != 0))
                    CompleteEnteredWorld(mapAdmission.Generation);
            }
        }

        private void CompleteEnteredWorld(ulong generation)
        {
            if (!IsAdmissionAllowed() || !mapAdmission.IsCurrent(generation))
                return;
            // Consume only this completed transition. Recovery callbacks may queue a new teleport.
            pendingTeleport = null;
            var matchReturn = matchReturnAfterEntry;
            matchReturnAfterEntry = null;
            // right before the loading screen is removed we can now send the actual network entity commands
            // this ensures there is no desync between the client and server caused by loading
            foreach (IGridEntity item in visibleEntities.Values)
                if (item is IWorldEntity we && we.MovementManager.RequiresSynchronisation)
                    we.MovementManager.SendNetworkEntityCommands(Session);

            Session.EnqueueMessageEncrypted(new ServerPlayerEnteredWorld());
            RefreshVisiblePlayersForNearbyList();
            QuestManager.SendInitialPackets();
            PathManager.SendInitialPackets();
            if (matchReturn is { } transfer && transfer.Generation == generation
                && ReferenceEquals(transfer.Map, Map) && IsAdmissionAllowed() && mapAdmission.IsCurrent(generation))
            {
                TeleportToCore(transfer.Position, TeleportReason.Relocate, arrivalRotation: transfer.Rotation);
                if (!CanTeleport())
                    return;
            }
            SyncDeferredLoadingWorldZoneRuntimeState();
            if (!IsAdmissionAllowed() || !mapAdmission.IsCurrent(generation))
                return;

            TryRecoverStarterTutorialOnEnteredWorld();
            if (!IsAdmissionAllowed() || !mapAdmission.IsCurrent(generation))
                return;
            SyncStarterTutorialEntityVisibility();
            TryRecoverStarterTutorialHoverboardRide();
            if (!IsAdmissionAllowed() || !mapAdmission.IsCurrent(generation))
                return;
            TryRecoverStarterTutorialQuestProgression(allowCombatTransitionRecovery: true);

            if (IsAdmissionAllowed() && mapAdmission.IsCurrent(generation))
            {
                IsLoading = false;
                QuestManager?.Communicator?.OnEnteredWorld();
            }
        }

        private void SyncDeferredLoadingWorldZoneRuntimeState()
        {
            if (!deferredLoadingWorldZoneRuntimeSync)
                return;

            deferredLoadingWorldZoneRuntimeSync = false;
            SyncWorldZoneRuntimeState();
        }

        private void SyncWorldZoneRuntimeState()
        {
            ulong generation = mapAdmission.Generation;
            if (!IsAdmissionAllowed())
                return;
            AchievementManager.CheckAchievements(this, AchievementType.EnterWorldZone, Zone.Id);
            if (!IsAdmissionAllowed() || !mapAdmission.IsCurrent(generation))
                return;
            QuestManager.ObjectiveUpdate(QuestObjectiveType.EnterZone, Zone.Id, 1);
            if (!IsAdmissionAllowed() || !mapAdmission.IsCurrent(generation))
                return;

            // Current path content activates table-backed zone episodes and checks already-complete
            // Explorer map-zone objectives; relocation handles newly revealed hexes.
            PathManager.TryActivateCurrentZoneEpisode();
            if (!IsAdmissionAllowed() || !mapAdmission.IsCurrent(generation))
                return;
            PathManager.CompleteCurrentExplorerExploreZoneMission();
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

            IQuestInfo questInfo = GetGlobalQuestManager().GetQuestInfo(questId);
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
                WorldLocation2Entry worldLocation = gameTableManager.WorldLocation2.GetEntry(TutorialWorldLocationIds[index]);
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
            uint lengthOfInGameDayInSeconds = sharedConfiguration?.Get<RealmConfig>()?.LengthOfInGameDay ?? 0u;
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
                total += gameTableManager.XpPerLevel.GetEntry(level)?.AttributePointsPerLevel ?? 0u;

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

        public uint GetTradeskillXp(TradeskillType tradeskillId)
        {
            return tradeskills.TryGetValue(tradeskillId, out TradeskillState tradeskill) ? tradeskill.TradeskillXp : 0u;
        }

        public bool LearnTradeskill(TradeskillType toLearnTradeskillId, TradeskillType toDropTradeskillId)
        {
            if (toDropTradeskillId != 0
                && toDropTradeskillId != toLearnTradeskillId
                && tradeskills.TryGetValue(toDropTradeskillId, out TradeskillState droppedTradeskill)
                && droppedTradeskill.IsActive != 0u)
            {
                droppedTradeskill.IsActive = 0u;
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

            SendProfessionUpdate(learnedTradeskill.BuildInfo());
            QuestManager.ObjectiveUpdate(QuestObjectiveType.LearnTradeskill, (uint)toLearnTradeskillId, 1u);
            if (AchievementManager is CharacterAchievementManager characterAchievementManager)
                characterAchievementManager.GrantCompletedTradeskillAchievementRewards(this);
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
            SendProfessionUpdate(tradeskill.BuildInfo());
            CheckTradeskillTierAchievements(tradeskillId, previousXp, tradeskill.TradeskillXp);
            return tradeskill.TradeskillXp - previousXp;
        }

        private void CheckTradeskillTierAchievements(TradeskillType tradeskillId, uint previousXp, uint currentXp)
        {
            foreach (TradeskillTierEntry tierEntry in gameTableManager.TradeskillTier.Entries
                .Where(entry => entry.TradeSkillId == (uint)tradeskillId
                    && entry.RequiredXp <= currentXp
                    && (entry.RequiredXp == 0u ? previousXp == 0u : entry.RequiredXp > previousXp)))
                AchievementManager.CheckAchievements(this, AchievementType.TradeskillTier, tierEntry.Id);
        }

        public uint AddTradeskillXpForTier(uint tradeskillTierId, uint amount)
        {
            if (amount == 0u)
                return 0u;

            TradeskillTierEntry tierEntry = gameTableManager.TradeskillTier.GetEntry(tradeskillTierId);
            if (tierEntry == null || tierEntry.TradeSkillId == 0u)
                return 0u;

            var tradeskillId = (TradeskillType)tierEntry.TradeSkillId;
            if (!Enum.IsDefined(tradeskillId))
                return 0u;

            return AddTradeskillXp(tradeskillId, amount);
        }

        public uint EnsureTradeskillTalentPointTotal(TradeskillType tradeskillId, uint earnedTalentPoints)
        {
            if (earnedTalentPoints == 0u || !HasTradeskill(tradeskillId))
                return 0u;

            TradeskillState tradeskill = tradeskills[tradeskillId];
            uint selectedTalents = tradeskill.GetSelectedTalentCount();
            uint requiredUnspentTalentPoints = earnedTalentPoints > selectedTalents
                ? earnedTalentPoints - selectedTalents
                : 0u;
            if (tradeskill.TalentPoints >= requiredUnspentTalentPoints)
                return 0u;

            uint previousTalentPoints = tradeskill.TalentPoints;
            tradeskill.TalentPoints = requiredUnspentTalentPoints;
            SendProfessionUpdate(tradeskill.BuildInfo());
            return tradeskill.TalentPoints - previousTalentPoints;
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

            SendProfessionUpdate(tradeskill.BuildInfo());
            return true;
        }

        public bool ResetTradeskillTalents(TradeskillType tradeskillId)
        {
            if (!HasTradeskill(tradeskillId))
                return false;

            TradeskillState tradeskill = tradeskills[tradeskillId];
            Array.Clear(tradeskill.TalentTierIds);
            tradeskill.TalentPoints = 0u;

            if (AchievementManager is CharacterAchievementManager characterAchievementManager)
                characterAchievementManager.GrantCompletedTradeskillAchievementRewards(this);
            SendProfessionUpdate(tradeskill.BuildInfo());
            SendTradeskillResetCooldown();
            return true;
        }

        public bool HasLearnedSchematic(uint tradeskillSchematic2Id)
        {
            return schematics.ContainsKey(tradeskillSchematic2Id);
        }

        public bool LearnSchematic(uint tradeskillSchematic2Id, bool discovered = false)
        {
            TradeskillSchematic2Entry schematicEntry = gameTableManager.TradeskillSchematic2.GetEntry(tradeskillSchematic2Id);
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
                }

                return false;
            }

            SchematicState schematic = SchematicState.Create(CharacterId, schematicEntry, discovered);
            schematics.Add(tradeskillSchematic2Id, schematic);

            SendLearnedSchematic((TradeskillType)schematicEntry.TradeSkillId, tradeskillSchematic2Id,
                schematic.DiscoveryCoordinateX, schematic.DiscoveryCoordinateY);

            QuestManager.ObjectiveUpdate(QuestObjectiveType.ObtainSchematic, tradeskillSchematic2Id, 1u);
            return true;
        }

        public bool DiscoverSchematic(uint tradeskillSchematic2Id)
        {
            return LearnSchematic(tradeskillSchematic2Id, true);
        }

        public void SendTradeskillInitialPackets()
        {
            if (UsesCommittedTradeskillPublication) { GetTradeskillPublication().SendInitial(); return; }
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
            if (UsesCommittedTradeskillPublication) { GetTradeskillPublication().RetainSkill(tradeskillInfo); return; }
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
            if (pendingMount != null)
            {
                if (PlatformGuid == null && !PendingMountExpired)
                    return false;
                pendingMount = null;
            }
            return PlatformGuid == null && pendingTeleport == null;
        }

        private IMountEntity pendingMount;
        private long pendingMountTimestamp;
        private bool PendingMountExpired => System.Diagnostics.Stopwatch.GetElapsedTime(pendingMountTimestamp) >= TimeSpan.FromSeconds(10);

        internal bool OwnsPendingMount(IMountEntity mount)
            => ReferenceEquals(pendingMount, mount) && !PendingMountExpired && pendingTeleport == null;

        internal void ClearPendingMount(IMountEntity mount)
        {
            if (ReferenceEquals(pendingMount, mount))
                pendingMount = null;
        }

        public void SetPendingMount(IMountEntity mount)
        {
            pendingMount = mount;
            pendingMountTimestamp = System.Diagnostics.Stopwatch.GetTimestamp();
        }

        /// <summary>
        /// Dismounts this <see cref="IPlayer"/> from a vehicle that it's attached to
        /// </summary>
        public void Dismount()
        {
            if (PlatformGuid == null)
                return;

            pendingMount = null;

            IWorldEntity platform = GetVisible<IWorldEntity>(PlatformGuid.Value);
            if (platform is VehicleEntity concrete)
            {
                if (concrete.TryRemovePassenger(this))
                    return;
            }
            else if (platform is IVehicleEntity vehicle && vehicle.GetPassenger(Guid) != null)
            {
                vehicle.PassengerRemove(this);
                return;
            }

            SetPlatform(null);
            SetControl(this);
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

            WorldLocation2Entry finishWorldLocation = gameTableManager.WorldLocation2.GetEntry(TutorialHoverboardFinishWorldLocationId);
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
            => RecoverStarterTutorialProgression(allowCombatTransitionRecovery
                ? StarterTutorialRecovery.FinishLocation : StarterTutorialRecovery.QuestProgression);

        public void TryRecoverStarterTutorialCombatProjectorActivation()
            => RecoverStarterTutorialProgression(StarterTutorialRecovery.CombatProjector);

        private void RecoverStarterTutorialProgression(StarterTutorialRecovery recovery)
        {
            if (Map?.Entry?.Id != TutorialWorldId || recoveringStarterTutorialQuestProgression
                || Session is GameSession { IsDisconnecting: true })
                return;

            if (queuedStarterTutorialRecovery is { } queued)
            {
                queuedStarterTutorialRecovery = queued | recovery;
                return;
            }

            recoveringStarterTutorialQuestProgression = true;
            try
            {
                bool updatedObjectives = (recovery & (StarterTutorialRecovery.QuestProgression | StarterTutorialRecovery.FinishLocation)) != 0
                    && TryRecoverStarterTutorialHoverboardRideObjective();
                if (QuestManager is QuestManager quests && Session is GameSession session
                    && (starterTutorialCompletionInFlight || ReceiverlessQuestIds.Any(id => quests.GetQuestState(id) == QuestState.Achieved)))
                {
                    if (RewardStateRequiresReload) return;
                    if (starterTutorialCompletionInFlight || !quests.CanStartTutorialQuestCompletion(this))
                    {
                        QueueStarterTutorialRecovery(quests, session, recovery);
                        return;
                    }

                    starterTutorialCompletionInFlight = true;
                    quests.SaveOrdinaryQuestMutation(this, session, () =>
                    {
                        bool changed = RecoverQuests(strictCompletion: true);
                        if (updatedObjectives || changed) SyncStarterTutorialEntityVisibility();
                        return changed;
                    }, () =>
                    {
                        starterTutorialCompletionInFlight = false;
                        if (RecoverCombatTransition()) SyncStarterTutorialEntityVisibility();
                    });
                }
                else
                {
                    bool changed = RecoverQuests(strictCompletion: false);
                    bool transitioned = RecoverCombatTransition();
                    if (updatedObjectives || changed || transitioned) SyncStarterTutorialEntityVisibility();
                }

                bool RecoverQuests(bool strictCompletion)
                {
                    bool completedQuests = TryCompleteAchievedStarterTutorialQuests(strictCompletion);
                    bool advancedQuestChain = TryRecoverStarterTutorialQuestChain(strictCompletion);
                    return completedQuests || advancedQuestChain;
                }

                bool RecoverCombatTransition()
                {
                    bool projector = recovery.HasFlag(StarterTutorialRecovery.CombatProjector);
                    return (projector || recovery.HasFlag(StarterTutorialRecovery.FinishLocation))
                        && TryRecoverStarterTutorialCombatTransition(requireFinishWorldLocation: !projector);
                }
            }
            finally
            {
                recoveringStarterTutorialQuestProgression = false;
            }
        }

        private void QueueStarterTutorialRecovery(QuestManager quests, GameSession session, StarterTutorialRecovery recovery)
        {
            var events = session.Events;
            var account = Account;
            ulong epoch = account?.WorldSessionEpoch ?? 0;
            bool CanResume() => !session.IsDisconnecting && ReferenceEquals(Session, session) && ReferenceEquals(session.Events, events)
                && ReferenceEquals(Account, account) && account?.WorldSessionEpoch == epoch && ReferenceEquals(QuestManager, quests)
                && session is IPlayerSession bound && ReferenceEquals(bound.Player, this) && !RewardStateRequiresReload;

            queuedStarterTutorialRecovery = recovery;
            events.EnqueueEvent(new PredicateEvent(
                () => !CanResume() || !starterTutorialCompletionInFlight && quests.CanStartTutorialQuestCompletion(this), () =>
            {
                StarterTutorialRecovery next = queuedStarterTutorialRecovery ?? recovery;
                queuedStarterTutorialRecovery = null;
                if (CanResume()) RecoverStarterTutorialProgression(next);
            }));
        }

        private bool TryRecoverStarterTutorialHoverboardRideObjective()
        {
            if (Map?.Entry?.Id != TutorialWorldId)
                return false;

            WorldLocation2Entry finishWorldLocation = gameTableManager.WorldLocation2.GetEntry(TutorialHoverboardFinishWorldLocationId);
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
                WorldLocation2Entry finishWorldLocation = gameTableManager.WorldLocation2.GetEntry(TutorialHoverboardFinishWorldLocationId);
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

            WorldLocation2Entry destination = gameTableManager.WorldLocation2.GetEntry(destinationWorldLocationId);
            if (destination == null)
            {
                log.Warn($"Starter tutorial combat transition recovery missing destination world location {destinationWorldLocationId} for player {Guid}.");
                return false;
            }

            if (!TryGetCombatSimulationTeleportPosition(Faction1, out Vector3 teleportPosition))
                return false;

            TeleportTo((ushort)destination.WorldId, teleportPosition.X, teleportPosition.Y, teleportPosition.Z);
            log.Debug($"Starter tutorial combat transition recovery teleported player {Guid}: destinationWorldLocation={destinationWorldLocationId}, destination=({teleportPosition.X}, {teleportPosition.Y}, {teleportPosition.Z}), hoverboardState={hoverboardState?.ToString() ?? "None"}, combatState={combatState?.ToString() ?? "None"}.");
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

        private bool TryCompleteAchievedStarterTutorialQuests(bool strictCompletion)
        {
            bool completedAny = false;

            foreach (ushort questId in ReceiverlessQuestIds)
            {
                if (QuestManager.GetQuestState(questId) != QuestState.Achieved)
                    continue;

                try
                {
                    QuestManager.QuestComplete(questId, 0, false);
                    completedAny |= QuestManager.GetQuestState(questId) == QuestState.Completed;
                    log.Debug($"Tutorial quest completion recovery for player {Guid}: quest={questId}, states [{FormatQuestStates(ReceiverlessQuestIds)}].");
                }
                catch (Exception exception) when (!strictCompletion || exception is QuestException)
                {
                    log.Debug(exception, $"Tutorial quest completion recovery skipped for player {Guid}: quest={questId}, states [{FormatQuestStates(ReceiverlessQuestIds)}].");
                }
            }

            return completedAny;
        }

        private bool TryRecoverStarterTutorialQuestChain(bool strictCompletion)
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

                IQuestInfo nextQuestInfo = GetGlobalQuestManager().GetQuestInfo(nextQuestId);
                if (nextQuestInfo == null)
                    continue;

                try
                {
                    QuestManager.QuestAdd(nextQuestInfo);

                    log.Debug($"Starter tutorial quest chain recovery for player {Guid}: previousQuest={previousQuestId}, addedQuest={nextQuestId}, chain states [{FormatQuestStates(questChain)}].");
                    return QuestManager.GetQuestState(nextQuestId) is QuestState.Accepted or QuestState.Achieved;
                }
                catch (Exception exception) when (!strictCompletion || exception is QuestException)
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
            return GetSaveDuration() - saveTimer.Time;
        }

        private double GetSaveDuration()
        {
            return sharedConfiguration?.Get<WorldConfig>()?.PlayerSaveIntervalSeconds ?? 60d;
        }

        /// <summary>
        /// Return <see cref="Disposition"/> between <see cref="IPlayer"/> and <see cref="Faction"/>.
        /// </summary>
        public override Disposition GetDispositionTo(Faction factionId, bool primary = true)
        {
            if (factionId == Faction.None)
                return Disposition.Unknown;

            IFactionNode targetFaction = factionManager.GetFaction(factionId);
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
            ClearPvPFlagDisable();

            EnqueueToVisible(new ServerUnitPvpStateChange
            {
                UnitId = Guid,
                State  = GetPvpState()
            }, true);
        }

        public void RequestPvPFlagDisable(uint cooldownMs)
        {
            if ((PvPFlag & PvPFlag.Enabled) == 0)
                return;

            pvpFlagDisableUntilUtc = DateTime.UtcNow.AddMilliseconds(cooldownMs);
            pvpFlagDisableTimer = new UpdateTimer(TimeSpan.FromMilliseconds(cooldownMs));
            saveMask |= PlayerSaveMask.PvP;
            Session.EnqueueMessageEncrypted(new ServerPvpCooldownUpdate
            {
                CooldownRemaining = cooldownMs
            });
        }

        public void CancelPvPFlagDisable()
        {
            if (pvpFlagDisableTimer == null && !pvpFlagDisableUntilUtc.HasValue)
                return;

            ClearPvPFlagDisable();
            Session.EnqueueMessageEncrypted(new ServerPvpCooldownClear());
        }

        private void UpdatePvPFlagDisable(double lastTick)
        {
            if (pvpFlagDisableTimer == null)
                return;

            pvpFlagDisableTimer.Update(lastTick);
            if (!pvpFlagDisableTimer.HasElapsed)
                return;

            FinishPvPFlagDisable();
            Session.EnqueueMessageEncrypted(new ServerPvpCooldownClear());
        }

        private void InitialisePvPFlagDisable(DateTime? disableUntilUtc)
        {
            if (!disableUntilUtc.HasValue)
                return;

            DateTime utcExpiry = DateTime.SpecifyKind(disableUntilUtc.Value, DateTimeKind.Utc);
            uint remainingMs = GetPvPFlagDisableRemainingMilliseconds(utcExpiry);
            if (remainingMs == 0u)
            {
                pvpFlagDisableUntilUtc = null;
                PvPFlag &= PvPFlag.Forced;
                saveMask |= PlayerSaveMask.PvP;
                return;
            }

            pvpFlagDisableUntilUtc = utcExpiry;
            pvpFlagDisableTimer    = new UpdateTimer(TimeSpan.FromMilliseconds(remainingMs));
            PvPFlag |= PvPFlag.Enabled;
        }

        private void SendPendingPvPFlagDisableCooldown()
        {
            if (!pvpFlagDisableUntilUtc.HasValue)
                return;

            uint remainingMs = GetPvPFlagDisableRemainingMilliseconds(pvpFlagDisableUntilUtc.Value);
            if (remainingMs == 0u)
            {
                FinishPvPFlagDisable();
                return;
            }

            Session.EnqueueMessageEncrypted(new ServerPvpCooldownUpdate
            {
                CooldownRemaining = remainingMs
            });
        }

        private void FinishPvPFlagDisable()
        {
            ClearPvPFlagDisable();
            SetPvPFlag(PvPFlag & PvPFlag.Forced);
        }

        private void ClearPvPFlagDisable()
        {
            if (pvpFlagDisableTimer == null && !pvpFlagDisableUntilUtc.HasValue)
                return;

            pvpFlagDisableTimer    = null;
            pvpFlagDisableUntilUtc = null;
            saveMask |= PlayerSaveMask.PvP;
        }

        private static uint GetPvPFlagDisableRemainingMilliseconds(DateTime disableUntilUtc)
        {
            TimeSpan remaining = disableUntilUtc - DateTime.UtcNow;
            if (remaining <= TimeSpan.Zero)
                return 0u;

            if (remaining.TotalMilliseconds >= uint.MaxValue)
                return uint.MaxValue;

            return (uint)Math.Ceiling(remaining.TotalMilliseconds);
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

        private Dictionary<Property, NetworkPropertyValue> retainedQuestProperties;
        internal bool HasRetainedQuestProperties => retainedQuestProperties?.Count > 0;
        internal bool IsRetainingQuestProperty(Property property) => retainedQuestProperties?.ContainsKey(property) == true;

        internal void RetainQuestProperty(QuestManager quests, Property property, NetworkPropertyValue previous)
        {
            (retainedQuestProperties ??= []).Add(property, previous);
            quests.RetainOrdinaryCompletionPublication(() =>
            {
                retainedQuestProperties.Remove(property);
                SetPropertyEmit(property);
                PublishPropertyUpdate(property, GetPropertyValue(property));
            });
        }

        /// <summary>
        /// Invoked when <see cref="IPlayer"/> has a <see cref="Property"/> updated.
        /// </summary>
        protected override void OnPropertyUpdate(IPropertyValue propertyValue)
        {
            base.OnPropertyUpdate(propertyValue);

            if (!IsRetainingQuestProperty(propertyValue.Property))
                PublishPropertyUpdate(propertyValue.Property, propertyValue.Value);
        }

        private void PublishPropertyUpdate(Property property, float value)
            => messagePublisher.PublishAsync(new PlayerPropertyUpdatedMessage
            {
                Identity = Identity.ToInternalIdentity(),
                Property = property,
                Value    = value
            }).FireAndForgetAsync();

        /// <summary>
        /// Invoked when <see cref="IWorldEntity"/> has a <see cref="Stat"/> updated.
        /// </summary>
        protected override void OnStatUpdate(IStatValue statValue)
        {
            Stat stat = statValue.Stat;
            float value = statValue.Value;
            if (IsRetainingQuestVital(stat))
                return;

            if (stat == Stat.Level && QuestManager is QuestManager { IsOrdinaryCompletionMutationInProgress: true } quests)
                quests.RetainOrdinaryCompletionPublication(() =>
                {
                    if (Level == value) PublishStatUpdate(stat, value);
                });
            else
                PublishStatUpdate(stat, value);
        }

        private void PublishStatUpdate(Stat stat, float value)
            => messagePublisher.PublishAsync(new PlayerStatUpdatedMessage
            {
                Identity = Identity.ToInternalIdentity(),
                Stat = stat,
                Value = value
            }).FireAndForgetAsync();

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
                if (duelManager == null || !duelManager.AreDueling(this, playerTarget))
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
                duelManager?.TryFinishDefeat(this, player);
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
            internal ProgressionPersistence.Recipe Baseline { get; init; }

            public static SchematicState Create(ulong characterId, TradeskillSchematic2Entry schematicEntry, bool discovered)
            {
                return new SchematicState
                {
                    CharacterId              = characterId,
                    TradeskillSchematic2Id   = schematicEntry.Id,
                    Discovered               = discovered,
                    DiscoveryCoordinateX     = schematicEntry.VectorX,
                    DiscoveryCoordinateY     = schematicEntry.VectorY
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
                    DiscoveryCoordinateY     = model.DiscoveryCoordinateY,
                    Baseline                 = ProgressionPersistence.FromModel(model)
                };
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
            internal ProgressionPersistence.Skill Baseline { get; init; }

            public static TradeskillState Create(ulong characterId, TradeskillType tradeskillId)
            {
                return new TradeskillState
                {
                    CharacterId   = characterId,
                    TradeskillId  = tradeskillId,
                    IsActive      = 1u
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
                    TalentPoints              = model.TalentPoints,
                    Baseline                  = ProgressionPersistence.FromModel(model)
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

            public uint GetSelectedTalentCount()
            {
                return (uint)TalentTierIds.Count(t => t != 0u);
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
