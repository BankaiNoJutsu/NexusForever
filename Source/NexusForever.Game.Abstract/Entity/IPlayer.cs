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

        /// <summary>Optional actor-owned full reward-track snapshot composition.</summary>
        NexusForever.Game.Abstract.Reward.IRewardTrackPresentation RewardTrackPresentation => null;

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

        /// <summary>
        /// Record the reason of the last <c>ServerPublicEventEnd</c> delivered to this player for the public event.
        /// </summary>
        /// <remarks>
        /// The client keeps this per event for its whole session (it is only ever overwritten by a later End) and prerequisite 199 tests it.
        /// </remarks>
        void RecordPublicEventEnd(uint publicEventId, Static.PublicEvent.PublicEventRemoveReason reason);

        /// <summary>
        /// Reason of the last <c>ServerPublicEventEnd</c> delivered to this player for the public event, or null when none was delivered this session.
        /// </summary>
        Static.PublicEvent.PublicEventRemoveReason? GetPublicEventEndReason(uint publicEventId);

        /// <summary>
        /// Phase bits this player perceives, the second dword of <c>ServerPhaseVisibilityWorldLocation</c> (0x0171). Prerequisites 24 and 25 test bit (Value &amp; 31) of it. Defaults to 1.
        /// </summary>
        uint PhasesIPerceive { get; }

        /// <summary>
        /// Phase bits in which other players perceive this player, the first dword of <c>ServerPhaseVisibilityWorldLocation</c> (0x0171). Defaults to 1.
        /// </summary>
        uint PhasesThatPerceiveMe { get; }

        /// <summary>
        /// Replace both phase masks and send <c>ServerPhaseVisibilityWorldLocation</c> to the client when either changed.
        /// </summary>
        void SetPhaseVisibility(uint phasesIPerceive, uint phasesThatPerceiveMe);

        /// <summary>
        /// Apply a <c>ChangePhase</c> spell effect: record it (replacing an earlier record of the same effect instance), recompute both phase words from the default mask and every active change,
        /// and send the packet when they changed.
        /// </summary>
        void AddPhaseChange(PhaseChange change);

        /// <summary>
        /// Remove the change owned by the spell effect instance and recompute the phase words. Returns false when none was recorded.
        /// </summary>
        bool RemovePhaseChange(uint effectId);

        /// <summary>
        /// Active <c>ChangePhase</c> records in the order they were applied.
        /// </summary>
        IReadOnlyList<PhaseChange> GetPhaseChanges();

        /// <summary>
        /// Plane bits this player can see, the second dword of <c>ServerPlaneVisibilitySpellSlingerVoid</c> (0x0172). Prerequisite 232 tests it. Zero until a <c>ChangePlane</c> effect sets bits.
        /// </summary>
        uint PlanesCanSee { get; }

        /// <summary>
        /// Plane bits in which this player can be seen, the first dword of 0x0172; the client stores it but never reads it.
        /// </summary>
        uint PlanesCanBeSeen { get; }

        /// <summary>
        /// Apply a <c>ChangePlane</c> spell effect: record it (replacing an earlier record of the same effect instance), recompute both plane words from zero and every active change, and send
        /// the packet when they changed.
        /// </summary>
        void AddPlaneChange(PlaneChange change);

        /// <summary>
        /// Remove the change owned by the spell effect instance and recompute the plane words. Returns false when none was recorded.
        /// </summary>
        bool RemovePlaneChange(uint effectId);

        /// <summary>
        /// Active <c>ChangePlane</c> records in the order they were applied.
        /// </summary>
        IReadOnlyList<PlaneChange> GetPlaneChanges();

        bool DisableOtherPlayersCombatLogs { get; set; }
        CombatLogOptions CombatLogDisableFlags { get; set; }
        AccountPresenceState PresenceState { get; set; }
        ulong PresenceSessionEpoch { get; }
        ulong PresenceAdmission { get; }
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
        IInlaidAugmentationManager InlaidAugmentationManager { get; }
        IPrimalMatrixManager PrimalMatrixManager { get; }
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

        /// <summary>
        /// Whether the hazard is enabled for the player (listed to the client with <c>Enabled = true</c>).
        /// </summary>
        bool IsHazardEnabled(uint hazardId);

        /// <summary>
        /// The hazard's current meter value, -1 when the player has no record of it.
        /// </summary>
        float GetHazardMeter(uint hazardId);
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
        void SetBindPoint(ushort bindPointId, ushort worldId, Vector3 position);
        bool TryGetBindPoint(out ushort worldId, out Vector3 position);
        IMapPosition GetOpenWorldReturnPosition();
        void DiscoverTaxiNode(uint taxiNodeId);
        bool IsNearTaxiNode(uint taxiNodeId, float range);
        void QueueTaxiFlight(WorldEntry world, Vector3 position, Vector3 rotation, uint destinationNode, ulong fare);

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

        /// <summary>
        /// Returns whether an active tradeskill of this <see cref="IPlayer"/> has the supplied TradeskillBonus id picked as a talent in any tier.
        /// </summary>
        bool HasTradeskillTalent(uint tradeskillBonusId);
        bool HasLearnedSchematic(uint tradeskillSchematic2Id);
        bool LearnSchematic(uint tradeskillSchematic2Id, bool discovered = false);
        bool DiscoverSchematic(uint tradeskillSchematic2Id);
        void SendTradeskillInitialPackets();

        /// <summary>
        /// Returns whether this <see cref="IPlayer"/> is allowed to summon or be added to a vehicle.
        /// </summary>
        bool CanMount();
        void SetPendingMount(IMountEntity mount);

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
        /// Replace the property totals granted by the player's Primal Matrix allocations.
        /// </summary>
        void SetPrimalMatrixProperties(IReadOnlyDictionary<Property, float> totals);

        /// <summary>
        /// Remove a <see cref="Property"/> modifier by a item that is currently affecting this <see cref="IPlayer"/>.
        /// </summary>
        void RemoveItemProperty(Property property, ItemSlot itemSlot);
    }
}
