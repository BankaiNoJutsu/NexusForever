using System.Numerics;
using System.Threading.Tasks;
using NexusForever.Database.Auth;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Challenges;
using NexusForever.Game.Abstract.Guild;
using NexusForever.Game.Abstract.Housing;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Map.Lock;
using NexusForever.Game.Abstract.Reputation;
using NexusForever.Game.Static.Chat;
using NexusForever.Game.Static.Crafting;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Option;
using NexusForever.Game.Static.Setting;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.Game.Abstract.Entity
{
    public interface IPlayer : IUnitEntity, IDatabaseAuth, IDatabaseCharacter
    {
        IAccount Account { get; }

        Identity Identity { get; }
        ulong CharacterId { get; }
        string Name { get; }
        Sex Sex { get; set; }
        Race Race { get; set; }
        Class Class { get; }
        CharacterFlag Flags { get; set; }
        PvPFlag PvPFlag { get; }
        Static.PlayerPath.Path Path { get; set; }
        DateTime PathActivatedTime { get; }
        InputSets InputKeySet { get; set; }
        byte InnateIndex { get; set; }
        CastingOptionFlags CastingOptions { get; set; }
        bool SharedChallengeEnabled { get; set; }
        bool DisableOtherPlayersCombatLogs { get; set; }
        CombatLogOptions CombatLogDisableFlags { get; set; }
        AccountPresenceState PresenceState { get; set; }
        string AwayAutoResponseMessage { get; set; }
        string BusyAutoResponseMessage { get; set; }
        WorldDifficulty InstanceDifficulty { get; set; }
        uint InstancePrimeLevel { get; set; }
        bool InstanceScalingEnabled { get; set; }
        IReadOnlyList<uint> AttributePointAllocations { get; }

        DateTime CreateTime { get; }
        double TimePlayedTotal { get; }
        double TimePlayedLevel { get; }
        double TimePlayedSession { get; }

        void Initialise(IGameSession session, IAccount account, CharacterModel model);

        /// <summary>
        /// Guid of the <see cref="IWorldEntity"/> that currently being controlled by the <see cref="IPlayer"/>.
        /// </summary>
        uint? ControlGuid { get; }

        /// <summary>
        /// Guid of the <see cref="IPetEntity"/> currently summoned by the <see cref="IPlayer"/>.
        /// </summary>
        uint? VanityPetGuid { get; set; }

        /// <summary>
        /// Id of the primary group that <see cref="IPlayer"/> is associated with.
        /// </summary>
        ulong GroupAssociation { get; set; }

        /// <summary>
        /// Group association value emitted in player entity packets.
        /// </summary>
        ulong ClientGroupAssociation { get; }

        bool IsSitting { get; }

        /// <summary>
        /// Returns if <see cref="IPlayer"/> has premium signature subscription.
        /// </summary>
        bool SignatureEnabled { get; }

        IGameSession Session { get; }

        /// <summary>
        /// Returns if <see cref="IPlayer"/>'s client is currently in a loading screen.
        /// </summary>
        bool IsLoading { get; set; }

        IInventory Inventory { get; }
        ICurrencyManager CurrencyManager { get; }
        IPathManager PathManager { get; }
        ITitleManager TitleManager { get; }
        ISpellManager SpellManager { get; }
        ICostumeManager CostumeManager { get; }
        IPetCustomisationManager PetCustomisationManager { get; }
        ICharacterKeybindingManager KeybindingManager { get; }
        IDatacubeManager DatacubeManager { get; }
        IGalacticArchiveManager GalacticArchiveManager { get; }
        IMailManager MailManager { get; }
        IZoneMapManager ZoneMapManager { get; }
        IChallengeManager ChallengeManager { get; }
        IQuestManager QuestManager { get; }
        ICharacterAchievementManager AchievementManager { get; }
        ISupplySatchelManager SupplySatchelManager { get; }
        IXpManager XpManager { get; }
        IReputationManager ReputationManager { get; }
        IGuildManager GuildManager { get; }
        IResidenceManager ResidenceManager { get; }
        ICinematicManager CinematicManager { get; }
        ICharacterEntitlementManager EntitlementManager { get; }
        ILogoutManager LogoutManager { get; }
        IAppearanceManager AppearanceManager { get; }
        IResurrectionManager ResurrectionManager { get; }

        IVendorInfo SelectedVendorInfo { get; set; }
        float VendorSellPriceMultiplier { get; }
        float VendorBuyPriceMultiplier { get; }

        bool TryAddVendorPriceModifier(uint effectId, float vendorSellMultiplier, float vendorBuyMultiplier, uint spell4Id, uint spell4EffectId, uint castingId, uint stackGroupId, uint stackCap, out string skippedReason);
        bool RemoveVendorPriceModifier(uint effectId);
        void RefreshSelectedVendorInfo(uint? vendorGuid = null);

        bool TryEnableHazard(uint effectId, uint hazardId, out string skippedReason);
        bool RemoveHazard(uint effectId);
        bool TryModifyHazard(uint hazardId, float amount, out string skippedReason);
        bool TrySuspendHazard(uint effectId, uint hazardId, uint targetMode, out string skippedReason);
        bool RemoveHazardSuspension(uint effectId);

        /// <summary>
        /// Save <see cref="IPlayer"/> to database, invoke supplied <see cref="Action"/> once save is complete.
        /// </summary>
        /// <remarks>
        /// This is a delayed save, <see cref="AuthContext"/> changes are saved first followed by <see cref="CharacterContext"/> changes.
        /// Packets for session will not be handled until save is complete.
        /// </remarks>
        void Save(Action callback = null);

        /// <summary>
        /// Save <see cref="IPlayer"/> to database.
        /// </summary>
        /// <remarks>
        /// This is an instant save, <see cref="AuthContext"/> changes are saved first followed by <see cref="CharacterContext"/> changes.
        /// Returns a task that completes once both database saves have finished.
        /// </remarks>
        Task SaveDirect();

        /// <summary>
        /// Request a delayed save on the next available update tick.
        /// </summary>
        void RequestSave();

        ItemProficiency GetItemProficiencies();

        /// <summary>
        /// Set the <see cref="IWorldEntity"/> that currently being controlled by the <see cref="IPlayer"/>.
        /// </summary>
        void SetControl(IWorldEntity entity);

        /// <summary>
        /// Returns if <see cref="IPlayer"/> can teleport.
        /// </summary>
        bool CanTeleport();

        /// <summary>
        /// Teleport <see cref="IPlayer"/> to supplied location.
        /// </summary>
        void TeleportTo(ushort worldId, float x, float y, float z, IMapLock mapLock = null, TeleportReason reason = TeleportReason.Relocate);

        /// <summary>
        /// Teleport <see cref="IPlayer"/> to supplied location.
        /// </summary>
        void TeleportTo(WorldEntry entry, Vector3 position, IMapLock mapLock = null, TeleportReason reason = TeleportReason.Relocate);

        /// <summary>
        /// Teleport <see cref="IPlayer"/> to supplied location.
        /// </summary>
        void TeleportTo(IMapPosition mapPosition, TeleportReason reason = TeleportReason.Relocate);

        /// <summary>
        /// Teleport <see cref="IPlayer"/> to a new position on the current map.
        /// </summary>
        void TeleportToLocal(Vector3 position, bool showLoadingScreen = true, Action<Vector3> callback = null);

        /// <summary>
        /// Invoked when <see cref="IPlayer"/> teleport fails.
        /// </summary>
        void OnTeleportToFailed(GenericError error);

        /// <summary>
        /// Invoked when <see cref="IPlayer"/> has finished loading and is ready to enter world.
        /// </summary>
        void OnEnteredWorld();

        /// <summary>
        /// Make <see cref="IPlayer"/> sit on provided <see cref="IWorldEntity"/>.
        /// </summary>
        void Sit(IWorldEntity chair);

        /// <summary>
        /// Remove <see cref="IPlayer"/> from the <see cref="IWorldEntity"/> it is sitting on.
        /// </summary>
        void Unsit();

        void SendGenericError(GenericError error);
        void SendSystemMessage(string text);
        uint GetTotalAttributePoints();
        uint GetAvailableAttributePoints();
        bool TrySpendAttributePoints(IReadOnlyList<uint> allocations, out uint availableAttributePoints);
        void ResetAttributePoints();
        void SendAttributePoints();
        bool HasTradeskill(TradeskillType tradeskillId);
        uint GetTradeskillXp(TradeskillType tradeskillId);
        bool LearnTradeskill(TradeskillType toLearnTradeskillId, TradeskillType toDropTradeskillId);
        uint AddTradeskillXp(TradeskillType tradeskillId, uint amount);
        uint AddTradeskillXpForTier(uint tradeskillTierId, uint amount);
        uint EnsureTradeskillTalentPointTotal(TradeskillType tradeskillId, uint earnedTalentPoints);
        bool PickTradeskillTalent(TradeskillType tradeskillId, uint tier, uint tradeskillBonusId);
        bool ResetTradeskillTalents(TradeskillType tradeskillId);
        bool HasLearnedSchematic(uint tradeskillSchematic2Id);
        bool LearnSchematic(uint tradeskillSchematic2Id, bool discovered = false);
        bool DiscoverSchematic(uint tradeskillSchematic2Id);
        void SendTradeskillInitialPackets();

        /// <summary>
        /// Returns whether this <see cref="IPlayer"/> is allowed to summon or be added to a vehicle.
        /// </summary>
        bool CanMount();

        /// <summary>
        /// Dismounts this <see cref="IPlayer"/> from a vehicle that it's attached to
        /// </summary>
        void Dismount();

        /// <summary>
        /// Last starter tutorial departure terminal activated by this <see cref="IPlayer"/>.
        /// </summary>
        uint? StarterTutorialDepartureTerminalCreatureId { get; }

        /// <summary>
        /// Record a starter tutorial departure terminal activation for destination selection.
        /// </summary>
        void RecordStarterTutorialDepartureTerminal(uint creatureId);

        /// <summary>
        /// Re-evaluate tutorial-specific entity visibility for this <see cref="IPlayer"/>.
        /// </summary>
        void SyncStarterTutorialEntityVisibility();

        /// <summary>
        /// Recover starter tutorial quest progression when the retail client flow needs compatibility recovery.
        /// </summary>
        void TryRecoverStarterTutorialQuestProgression();

        /// <summary>
        /// Recover starter tutorial quest progression when the retail client flow needs compatibility recovery.
        /// </summary>
        void TryRecoverStarterTutorialQuestProgression(bool allowCombatTransitionRecovery);

        /// <summary>
        /// Recover the starter tutorial combat transition after the combat projector has been activated.
        /// </summary>
        void TryRecoverStarterTutorialCombatProjectorActivation();

        /// <summary>
        /// Returns the time in seconds that has past since the last <see cref="IPlayer"/> save.
        /// </summary>
        double GetTimeSinceLastSave();

        /// <summary>
        /// Add a new <see cref="CharacterFlag"/>.
        /// </summary>
        void SetFlag(CharacterFlag flag);

        /// <summary>
        /// Remove an existing <see cref="CharacterFlag"/>.
        /// </summary>
        void RemoveFlag(CharacterFlag flag);

        /// <summary>
        /// Returns if supplied <see cref="CharacterFlag"/> exists.
        /// </summary>
        bool HasFlag(CharacterFlag flag);

        void SendCharacterFlagsUpdated();
        void SetPvPFlag(PvPFlag flag);
        void RequestPvPFlagDisable(uint cooldownMs);
        void CancelPvPFlagDisable();

        /// <summary>
        /// Add a <see cref="Property"/> modifier given a <see cref="ItemSlot"/> and value.
        /// </summary>
        void AddItemProperty(Property property, ItemSlot itemSlot, float value);

        /// <summary>
        /// Remove a <see cref="Property"/> modifier by a item that is currently affecting this <see cref="IPlayer"/>.
        /// </summary>
        void RemoveItemProperty(Property property, ItemSlot itemSlot);
    }
}
