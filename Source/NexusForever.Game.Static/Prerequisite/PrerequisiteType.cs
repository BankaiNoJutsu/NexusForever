namespace NexusForever.Game.Static.Prerequisite
{
    public enum PrerequisiteType
    {
        None                        = 0,
        Level                       = 1, // Level requirement not met
        Race                        = 2, // Race requirement not met
        Class                       = 3, // Class requirement not met
        Faction                     = 4, // Faction requirement not met - handler table[4] Prerequisite_CheckFactionType4 14049d3f0; NF compares player Faction1 to value0 (distinct from type 128)
        Reputation                  = 5, // Reputation requirement not met - live case 0x5 inline: entity+0x118 faction component vtable+0x20(objectId0) then PrerequisiteManager_ApplyComparisonFloat 1404a2010; handler table[5] 14049d470 is orphan entity-id list walk (not reputation); NF compares ReputationManager amount for faction objectId0 to value0
        QuestState                  = 6, // Quest requirement not met
        AchievementState            = 7, // Achievement requirement not met
        ItemProficiency             = 8, // Item proficiency requirement not met
        EpisodeState                = 9, // Episode requirement not met
        Gender                      = 10, // Gender requirement not met - client enum PrerequisiteComp_Sex
        OtherPrerequisite           = 11, // Other requirement not met
        DeadState                   = 12, // Player death state not correct - handler table[12] shares body with ActionSetSpell 14049d6d0 (live 0xdd); NF uses IsAlive scalar until separate dead-state witness
        ItemEquipped                = 13, // Item equipment requirement not met - handler table[13] Prerequisite_CheckItemEquipped 14049d760; NF checks Equipped bag index objectId0
        ItemOnCharacter             = 14, // Inventory requirement not met - handler table[14] Prerequisite_CheckItemOnCharacter 14049d7b0; NF uses Inventory.GetItemCount
        UnderSpell                  = 15, // Spell requirements not met
        DistanceToWorldLocation     = 16, // Distance requirements not met
        // 17 is unused in PrerequisiteType.tbl
        ScheduledEvent              = 18, // You cannot do that at this time
        TradeSkillProfession        = 19, // You must have the correct tradeskill tier.
        GroupSize                   = 20, // Incorrect party size
        TradeSkillSchematicLevel    = 21, // Incorrect schematic level
        QuestObjective0             = 22, // Quest objective requirements not met
        QuestObjective1             = 23, // Quest objective requirements not met
        InPhase                     = 24, // You cannot do that - handler table[24] Prerequisite_CheckInPhase 14049db50; NF compares IWorldEntity.PublicEventPhase on target/player
        CanSeePhase                 = 25, // You cannot do that - handler table[25] Prerequisite_CheckCanSeePhase 14049db90; NF compares player vs target PublicEventPhase match (1/0)
        InSubZone                   = 26, // Incorrect zone
        InTriggerVolume             = 27, // You are out of bounds - handler table[27] Prerequisite_CheckInTriggerVolume 14049dc50; NF WorldLocation2 radius+vertical proxy for objectId0 volume id
        InCombat                    = 28, // You must be in combat - handler table[28] Prerequisite_CheckInCombat 14049dcb0; NF compares IUnitEntity.InCombat to value0
        TimeOfDay                   = 29, // Incorrect time of day - handler table[29] Prerequisite_CheckTimeOfDay 14049dd10; NF uses InGameTimePrerequisiteHelper (SendInGameTime clock)
        FacilityWithinDistance      = 30, // Distance requirement not met
        CreatureWithinDistance      = 31, // Distance requirement not met
        // 32 is unused in PrerequisiteType.tbl
        QuestObjective2             = 33, // Quest objective requirements not met
        QuestObjective3             = 34, // Quest objective requirements not met
        QuestObjective4             = 35, // Quest objective requirements not met
        QuestObjective5             = 36, // Quest objective requirements not met
        RandomPercent               = 37, // Random check: Failed - handler table[37] Prerequisite_CheckRandomPercent 14049e350; NF rolls 0-99 vs value0
        IsCreature                  = 38, // Incorrect creature - handler table[38] Prerequisite_CheckIsCreature 14049e3a0; NF compares target is ICreatureEntity to value0
        IsPlayer                    = 39, // Incorrect player - handler table[39] Prerequisite_CheckIsPlayer 14049e3f0; NF compares target is IPlayer to value0
        Health                      = 40, // Incorrect health - handler table[40] Prerequisite_CheckHealth 14049e440; NF compares raw IWorldEntity.Health (type 172 HealthScaled uses percent)
        ZoneExplored                = 41, // Zone exploration criteria not met - handler table[41] Prerequisite_CheckZoneExplored 14049e490; NF uses IZoneMapManager.GetMapZoneExploredPercent for objectId0 MapZone id
        IsGroupLeader               = 42, // You cannot do that - handler table[42] Prerequisite_CheckIsGroupLeader 14049e540; NF uses IGroupStateManager + GroupLootState.Leader
        IsObjectiveActive           = 43, // You cannot do that right now - handler table[43] Prerequisite_CheckIsObjectiveActive 14049e610; NF uses IQuestManager.IsActiveObjectiveId
        InTargetGroup               = 44, // You cannot do that right now
        // 45 is unused in PrerequisiteType.tbl
        Waypoint                    = 46, // Incorrect waypoint direction - handler table[46] Prerequisite_CheckWaypoint 14049e680; NF PositionalRequirement angular cone when objectId0 resolves and target is set
        QuestObjective47            = 47, // You cannot do that right now - Value is QuestObjective id
        Unknown48                   = 48, // You cannot do that right now - two tbl rows; only row 20451 is referenced by Spell4Effects caster-persistence gates on dungeon raid-wipe timer spell 47945; live case 0x30 calls vtable +0x1e8, but that slot is no-op stub 140001ba0
        Schedule                    = 49, // That schedule is unavailable
        UnderSpellOnTarget            = 50, // You cannot do that now - live Prerequisite_CheckUnderSpellOnTargetUnit 1404699f0; handler table[50] alias 14049e730; NF uses SpellPrerequisiteHelper.IsUnderSpell on target
        ItemQuantity                = 51, // You do not have the correct number of items - handler table[51] Prerequisite_CheckItemQuantity 14049e780; NF uses Inventory.GetItemCount(objectId0)
        Path                        = 52, // You do not meet the player path requirement
        PathEpisode                 = 53, // You do not meet the player path episode requirement
        PlayerPathMission           = 54, // You do not meet the player path mission requirement
        Currency                    = 55, // Requirements not met - handler table[55] Prerequisite_CheckCurrency 14049e830; objectId0 CurrencyType id; NF uses CurrencyPrerequisiteHelper + ICurrencyManager
        DifficultyRankSlotAssigned    = 56, // Requirements not met - client case 0x38 checks entity+0x2d8 slot at index value0 (< 0x1c) is populated
        // 57 is unused in PrerequisiteType.tbl
        // 58 is unused in PrerequisiteType.tbl
        SpellTier                     = 59, // Spell tier requirement not met - live Prerequisite_CheckSpellTier 1404a4f60; table[59] 14049e820; objectId0 Spell4 id; value0 tier; NF uses ISpellManager.GetSpellTier
        SpellTierOnTarget             = 60, // Spell tier requirement on target not met - live Prerequisite_GetSpellTierOnTargetUnit 14046a110; table[60] 14049e850; NF tier check on target IPlayer
        PathMissionCount            = 61, // Path mission count is incorrect 
        ScanCreature                = 62, // Unable to scan this creature - NF datacube checklist progress; native live case 0x3e vtable +0x4a8 -> Prerequisite_CheckScanCreature_LiveCase3E 14049ff30 (target +0x3754/+0x3750 scan bitmask vs value0)
        IsLocalPlayerEntity         = 63, // Requirements not met - live case 0x3f vtable +0x78 -> raw 14049c7b0; compares evaluated entity +8 identity to DAT_140c65898+0x78 local player +8
        PathTypeLevel               = 64, // Requirements not met - objectId0 path type; value0 path level; NF uses PathPrerequisiteHelper + PathLevel.tbl
        Deprecated65                = 65, // Marked as DEPRECATED
        Deprecated66                = 66, // Marked as DEPRECATED
        PathMissionRequirement      = 67, // You do not meet the path mission requirement
        QuestObjective              = 68, // Quest objective requirement not met - handler table[68] Prerequisite_CheckQuestObjective 14049ecc0; NF checks active-quest objective complete by QuestObjective id (objectId0)
        ChallengeRequirement        = 69, // Challenge requirement not met
        ChallengeTier               = 70, // Challenge tier requirement not met - handler table[70] Prerequisite_CheckChallengeTier 14049ed20; NF uses IChallengeManager.GetCompletionCount for objectId0 challenge id (tier proxy)
        ClassProgress               = 71, // Requirements not met - ObjectId is Class id, value is class progression threshold
        // 72 is unused in PrerequisiteType.tbl
        Vital                       = 73, // Requirements not met - Part of Vital
        ChallengeLocked             = 74, // The specified challenge is locked
        // 75 is unused in PrerequisiteType.tbl
        CreatureState               = 76, // Requirements not met - handler table[76] Prerequisite_CheckCreatureState 14049ef00; objectId0 optional Creature2 id filter; value0 is unit state index; NF uses IUnitEntity.HasUnitState
        QuestObjectiveOnTarget        = 77, // Requirements not met - client case 0x4d via manager +0x548 with target param_2[1]; objectId0 is QuestObjective id when non-zero (15159 seen)
        MovementMode                = 78, // Invalid movement mode - handler table[78] Prerequisite_CheckMovementMode 14049ef70 (entity+0x640); NF uses IMovementManager.GetMode
        UnderForcedMovement         = 79, // Not under forced movement - handler table[79] Prerequisite_CheckUnderForcedMovement 14049efa0; NF proxies IMovementManager.ServerControl
        HealthRequirement           = 80, // You do not meet the health requirements - handler table[80] stub 140001ba0; NF health percent on evaluated unit (live uses scaled path at type 172)
        WrongSpellMechanic          = 81, // Wrong spell mechanic - handler table[81] Prerequisite_CheckWrongSpellMechanic 14049efc0; NF Spell4TargetMechanics.Flags overlap on active persistent spells (tbl comparison selects forbidden vs required)
        UnitEntityType              = 82, // Requirements not met - handler table[82] Prerequisite_CheckUnitEntityType 14049f010; NF compares EntityType on evaluated unit (client entity+0x80 unit-type id proxy)
        Vehicle                     = 83, // Vehicle conditions not met - live vtable +0xe0 Prerequisite_CheckVehicleCreature_Table83 14049cbb0; objectId0 is optional vehicle Creature2 id, value0 optional context value; NF currently checks platform EntityType.Vehicle
        Mount                       = 84, // Mount conditions not met - handler table[84] Prerequisite_CheckMount 14049f0c0; NF checks platform EntityType.Mount
        Taxi                        = 85, // Taxi conditions not met - handler table[85] Prerequisite_CheckTaxi 14049f0f0 (entity+0x16a0); NF checks platform EntityType.Taxi
        Jump                        = 86, // Jump requirement not met - handler table[86] Prerequisite_CheckJump 14049f190; NF uses StateFlags.Jump
        Moving                      = 87, // Moving requirement not met - handler table[87] Prerequisite_CheckMoving 14049f310; NF uses StateFlags.Move/Velocity and velocity/move vectors
        Pet                         = 88, // Pet requirements not met - handler table[88] Prerequisite_CheckPet 14049f370; NF checks visible vanity pet via VanityPetGuid
        PetMatchTarget              = 89, // Requirements not met - live Prerequisite_CheckPetMatchTarget 14049d4f0 (+0x160); handler table[89] 14049f3d0; NF compares vanity-pet CreatureId on caster vs target player
        MountVehicleType            = 90, // Requirements not met - live vtable +0x2c0 Prerequisite_CheckMountVehicleType 14049e730; handler table[90] Prerequisite_CheckMountVehicleType 14049f460; NF compares platform EntityType while mounted
        PathLevel                   = 91, // Incorrect path level - handler table[91] 14049f4d0; NF compares PathPrerequisiteHelper level for objectId0 path or active path
        ActiveSpellTargetMechanic   = 92, // Spell conditions not met - live Prerequisite_CheckActiveSpellTargetMechanic 14049d5b0 (+0x168); table[92] 14049f5b0; NF Spell4TargetMechanics.Flags overlap on active persistent spells
        Spell4EffectCategoryOnUnit  = 93, // Spell conditions not met - live case 0x5d via 1403d6f1c; table[93] 14049f690; NF Spell4EffectGroupList membership for value0 effect-group id on persistent effects
        QuestObjective6             = 94, // Quest objective requirements not met - handler table[94] 14049f700; NF reuses QuestObjectivePrerequisiteHelper (type 68 path)
        Distance                    = 95, // Distance requirements not met - handler table[95] Prerequisite_CheckDistance 14049f770; NF compares floor 3D distance (player to target) in world units
        EvalContextFloatByObjectId  = 96, // Requirements not met - table[96] Prerequisite_CheckEvalContextFloatByObjectId 14049f810; eval-context list (+0x20 key, +0x28 float); NF item-eval proxy via PrerequisiteEvalListHelper
        AccountItemListItem2CountNpc97 = 97, // Requirements not met - table[97] 14049f8c0 NPC 0x14/0x17 then AccountItemList_WalkItem2IdMatch 140497d9c; NF account-row Item2 count + NPC gate
        AccountItemListItem2CountNpc98 = 98, // Requirements not met - table[98] 14049f990 same Item2 walk family as 97/99; NF account-row Item2 count + NPC gate
        AccountItemListItem2CountNpc99 = 99, // Requirements not met - table[99] 14049f9d0 NPC 0x14/0x17 then AccountItemList_WalkItem2IdMatch 140497d9c; NF account-row Item2 count + NPC gate
        AccountItemListItem2Count100 = 100, // Requirements not met - table[100] 14049fa80 AccountItemList_WalkItem2IdMatch 140497d9c (no NPC gate); NF account-row Item2 count vs value0
        AccountItemListItem2Count101 = 101, // Requirements not met - table[101] 14049fac0 AccountItemList_WalkItem2IdMatch 140497d9c (no NPC gate); NF account-row Item2 count vs value0
        SpellCooldownNodeOnUnit       = 102, // Spell effect requirements not met - live 1404a4fe0; table[102] 14049fb50; value0=cooldown-node id; NF checks active SpellManager cooldown referencing SpellCoolDown id
        SpellCooldownNodeOnTarget     = 103, // Spell effect requirements not met - live 14046a190; table[103] 14049fbe0; value0=cooldown-node id on caster with prerequisite target present
        SpellCooldownNodeInServiceSet = 104, // Spell effect requirements not met - live 14046a210; table[104] 14049fc70; value0=cooldown-node id; NF checks spell-book membership referencing node
        SpellEffectTypeOnUnit         = 105, // Spell effect requirements not met - handler table[105] 14049fd50; value0=SpellEffectType; NF checks persistent lifetime effects on unit pending spells
        TargetEntityLookupHit         = 106, // Requirements not met - live Prerequisite_CheckTargetEntityLookupHit 14049e900 (+0x318); table[106] 14049fd80; NF resolves target on player.Map
        QuestObjective7             = 107, // Quest object requirement not met
        /// <summary>
        /// Checks to see if a PositionalRequirement Entry is met.
        /// </summary>
        PositionalRequirement       = 108, // Requirements not met
        WorldRequirement            = 109, // World requirement not met
        PlayerPathMissionCount      = 110, // Player path mission count not correct
        HazardProperty111           = 111, // Hazard property requirement not met
        HazardProperty112           = 112, // Hazard property requirement not met
        HazardProperty113           = 113, // Hazard property requirement not met
        Vital114                    = 114, // Vital requirement not met
        Unit                        = 115, // Unit requirement not met
        Stealth                     = 116, // Stealth property requirement not met
        PublicEvent117              = 117, // Public event requirement not met
        PublicEventObjective188     = 118, // Public event objective requirement not met 
        PublicEventObjectiveObject  = 119, // Public event objective object requirement not met
        PublicEvent120              = 120, // Public event requirement not met
        PublicEvent121              = 121, // Public event requirement not met.
        PublicEventObjective122     = 122, // Public event objective not objective spawn
        PublicEventObjective123     = 123, // Public event objective requirement not met - ObjectId is PublicEventObjective id
        ChallengeCompletionCount    = 124, // Invalid challenge completion count
        Challenge125                = 125, // Challenge requirement not met
        PathHoldout                 = 126, // Path holdout requirement not met
        PathHoldoutPlayer           = 127, // Path holdout requirement for player not met
        Faction128                  = 128, // Faction requirement not met - live vtable +0x68 Prerequisite_CheckFaction 14049c720 (handler table[128] stub); NF uses FactionPrerequisiteHelper + IFactionNode.GetAscendant on value0 faction id
        ActiveSpellEffectOnUnit       = 129, // Spell requirement not met - handler table[129] 1404a0540; live case 0x81 entity+0x15c8 walk; NF HasActiveSpell4 on evaluated unit
        ActiveSpellEffectOnTarget     = 130, // Spell requirement not met - live Prerequisite_CheckActiveSpellEffectOnTarget 140469a70; table[130] 1404a0590; NF HasActiveSpell4 on caster (value0 Spell4 id)
        Level131                    = 131, // Level requirement not met
        // 132 is unused in PrerequisiteType.tbl
        ItemStatData                  = 133, // Item requirement not met - client case 0x85 via manager +0x5c8 (1404a0d00); item eval param_2[2]+0x140 (ItemStat row field cached at load in 1408ea170)
        ItemStatId                    = 134, // Item requirement not met - client case 0x86 via manager +0x5d0 (1404a0d30); item eval param_2[2]+0x148 (Item2.ItemStatId cache)
        AppliedItemStatId           = 135, // Item requirement not met - client case 0x87 via Prerequisite_CheckAppliedItemStatId 1404a0d60 (+0x5d8); item eval param_2[2]+0x144 vs objectId0; all rows objectId0 is ItemStat.Id (distinct from ItemStatId +0x148)
        Item2Id                     = 136, // Item requirement not met - client case 0x88 via manager +0x5e0 (1404a0d90); item eval param_2[3]+0x0 via ApplyComparison
        ItemLevel                   = 137, // Item level requirement not met - client case 0x89 via manager +0x5e8 (1404a0db0); item eval param_2[3]+0x4 vs value1 via ApplyComparison
        ItemSpecial                 = 138, // Item special requirement not met - client case 0x8a via manager +0x5f0 (1404a0dd0); item param_2[3]+0x114/+0x118 zero/non-zero gate
        ItemMicrochip               = 139, // Item microchip requirement not met - client case 0x8b via Prerequisite_CheckItemMicrochip 1404a0e10 (+0x5f8); +0x114 bitmask via Prerequisite_MapMicrochipIdToBit 14049bdc0; retail rows 10610/10685 use objectId0=0 value0=count
        ItemRolledPropertyValue     = 140, // Item requirement not met - client case 0x8c via Prerequisite_CheckItemDwordArray0x94 1404a0e80 (+0x600); 15 Property slot ids at item-eval +0x94..+0xcc, rolled magnitudes at +0xd0+index*4 vs value1; writer FUN_14040e610 uses ids 0x29/0xaf/0xb0/0xb2 and empty slot 0xc5; zero Prerequisite.tbl rows
        IsOutOfBounds               = 141, // You are out of bounds
        Unknown142                  = 142, // Spell requirement not met - 9 tbl rows referenced by Spell4Effects caster/target apply gates; live case 0x8e passes eval-context param_2[4] to vtable +0x170, but that slot is no-op stub 140001ba0, so slot semantics remain blocked
        Difficulty                  = 143, // Difficulty requirement not met
        Unknown144                  = 144, // Exist in PrerequisiteType.tbl but does not have a description - no retail rows found; live dispatcher skips case 0x90, while handler table[144] 1404a0b70 checks live event runtime +0x1d0 == 1 with field semantics blocked
        ItemIsSelfCraftedWeapon     = 145, // Self-crafted weapon requirement not met
        ItemTradeSkillLevel         = 146, // Item tradeskill level requirement not met
        PlayersInWorld              = 147, // There are no players in the world who meet the requirement
        InfrastructureState         = 148, // The infrastructure state requirement is not met - client case 0x94 via Prerequisite_CheckInfrastructureState 1404a0150 (+0x4e8); FUN_1403d2d60 lookup then value0 vs state +0x10; NF maps PathMissionTypeEnum 0x15 (21) ObjectId = PathSettlerInfrastructure.Id
        State                       = 149, // State requirement not met
        HubEconomyProgress          = 150, // Hub economy progress requirement not met
        HubQualityOfLife            = 151, // Hub quality of life progress requirement not met
        HubSecurity                 = 152, // Hub security progress requirement not met
        HoldoutWave                 = 153, // The specified holdout wave requirement not met
        Holdout                     = 154, // Soldier holdout requirement not met
        PathMission155              = 155, // Path mission requirement not met
        // 156 is unused in PrerequisiteType.tbl
        // 157 is unused in PrerequisiteType.tbl
        // 158 is unused in PrerequisiteType.tbl
        PositionalRequirementBetweenCasterAndTarget = 159, // Requirements not met - client case 0x9f vtable +0x1e0 -> 14049d940; PositionalRequirement objectId0 cone between caster/target, Flags bit0 swaps facing source
        Challenge160                = 160, // Challenge requirement not met
        // 161 is unused in PrerequisiteType.tbl
        PublicEventObjective2       = 162, // Public event objective requirement not met
        PublicEventParticipantCount = 163, // Public event participant count requirement not met
        PublicEventParticipant164   = 164, // Public event participant requirement not met
        // 165 is unused in PrerequisiteType.tbl
        LiveEventTreeLookup           = 166, // Live event requirement not met - handler table[166] Prerequisite_CheckLiveEventTreeLookup 1404a1580: NPC types 0x14/0x17 then LiveEventTree_LookupByObjectId 1404a7f50 on entity+0x3f8 (returns 1 when liveEvent id present in tree)
        LiveEventWorldFactionBranch   = 167, // Live event requirement not met - handler table[167] Prerequisite_CheckLiveEventWorldFactionBranch 1404a15d0: NPC types 0x14/0x17 then LiveEventTree_LookupWithWorldBranch 1404a80b0 + LiveEvent_GetWorldFactionField 1404a8430 (world 0xa6/0xa7 -> record+0x68/+0x88)
        AccountItemCount            = 168, // Account item count requirement not correct - handler table[168] Prerequisite_CheckAccountItemCount 1404a1620: NPC types 0x14/0x17 then AccountItem_CountOwnedByItem2Id 1403d2140 (player inventory + account item list, capped by Item2 MaxStackCount); live case 0xa8 uses vtable +0x5a0 not this pointer
        AccountItemCountCompared    = 169, // Account item count requirement not met - handler table[169] Prerequisite_CheckAccountItemCountCompared 1404a1670: AccountItem_CountOwnedByItem2Id then ApplyComparison; live case 0xa9 uses vtable +0x5a8
        GameFormula                   = 170, // Requirements not met - value0 is GameFormula.tbl id; live case 0xaa vtable +0x90 @ 14049c880 Prerequisite_CheckEntitySetCountAt7188 (counts NPC entities in player global +0x7188 set matching objectId, not GameFormula_GetEntry); handler table[170] @ 0x140b67870 is Prerequisite_CheckAccountItemListScalarAt110 1404a16c0 (accountItemList+0x110 vs value0); NF PrerequisiteCheckGameFormula uses tbl formula-id path only
        DailyLoginDaysTotal           = 171, // Daily login days requirement not met - handler table[171] Prerequisite_CheckAccountItemListScalarAt180 1404a1710: accountItemList+0x180 equals DailyLoginUpdate Value0 (DailyLoginUpdate_HandleServer096E 140005540; DailyLogin_DayTimerCallback 140006890 inc); live case 0xab vtable +0x510 @ 1404a03c0
        HealthScaled                  = 172, // Health requirement not met - client case 0xac inline: Entity_GetHealthScaleFactor 14047a940(entity) * *(float*)(entity+0xd08+0x8c) float; NF compares health percent (Health*100/MaxHealth) until scale factor is modeled; handler table[172] reads accountItemList+0x184 (DailyLogin rewardsAvailable) on dead path
        ItemTradeSkill              = 173, // Item tradeskill requirement not met - handler table[173] Prerequisite_CheckItemTradeSkill 1404a1790: NPC types 0x14/0x17; TradeSkill_CheckItemTradeskillRequirement 1403c16e0(item2Id)
        ItemTradeSkillKnown         = 174, // Known tradeskill recipe requirement not met - handler table[174] Prerequisite_CheckItemTradeSkillKnown 1404a17e0: NPC types 0x14/0x17; TradeSkill_ClientHasKnownItem2Id 1403b91d0 binary-searches player known-recipe list
        TradeSkill                  = 175, // Tradeskill requirement not met - handler table[175] stub 140001ba0; NF compares TradeskillType objectId0 tier rank via HasTradeskill + TradeskillTier
        ChallengeObject             = 176, // Challenge object requirement not met - handler table[176] stub 140001ba0; NF proxies activated challenge for objectId0 Challenge.tbl id
        TrueLevel                   = 177, // Enum label unverified - handler table[177] Prerequisite_CheckPlayerGlobalTreeLookup177 1404a1830: NPC gate + PlayerGlobal_LookupObjectIdInTree 1403d407b at global+0x6300 (presence); NF uses challenge activated/completion proxy
        ProgressTrackOnMatchingEntity = 178, // Requirements not met - handler table[178] Prerequisite_CheckProgressTrackOnMatchingEntity 1404a1890: target entity id (+8) must equal local player, then Progress_GetTrackedScalar 1403fa980(1, objectId0) before ApplyComparison; rejects tbl Equipped hint
        CreatureDifficulty          = 179, // Requirements not met - objectId0 is Creature2Difficulty id; spell apply path + NF PrerequisiteCheckCreatureDifficulty; handler table[179] 1404a18f0 calls PrimalMatrix_LookupAllocatedNode 1404d6a60 (separate dead-path cluster — not difficulty compare)
        CreatureDifficultyRank      = 180, // Requirements not met - handler table[180] stub 140001ba0; value0 is Creature2Difficulty.RankValue on target (optional objectId0 filters difficulty id); spell path also handles type 179 inline
        // 181 is unused in PrerequisiteType.tbl
        HouseOwnership              = 182, // Housing ownership requirement not met - handler table[182] Prerequisite_CheckHouseOwnership 1404a5150; NF checks IResidenceManager residence OwnerId + optional PropertyInfoId objectId0
        Guild                       = 183, // Guild requirement not met - live vtable +0x328 Prerequisite_CheckGuildMembership 14049e9e0 (table[183] not authoritative); NF checks IGuildManager membership for objectId0 guild id
        Guild2                      = 184, // Guild requirement not met - live vtable +0x330 Prerequisite_CheckGuildMembershipOnTarget 14049ea30; NF checks caster/target share IGuildBase id
        GuildPerk                   = 185, // Guild perk requirement not met - live Prerequisite_CheckGuildPerk 14049eae0 (+0x338); NF trace stub until UnlockedPerks modeled on server guild
        HousingNeighborResidence    = 186, // Requirements not met - live case 0xba vtable +0x628 -> Prerequisite_CheckHousingNeighborResidence_Table186 1404a10e0; NPC gate + current housing residence identity present in cached neighbor map
        WarplotPlugUpgrade          = 187, // Warplot plug upgrade tier requirement not met
        WarplotPermission           = 188, // Warplot permission requirement not met - live Prerequisite_CheckWarplotCirclePerk 14049ebf0 (+0x340); handler table[188] _purecall; NF trace stub until warplot circle perk runtime exists
        Unknown189                  = 189, // Path settler requirement not met - 9 tbl rows (value0 often 3), but live case 0xbd dispatches vtable +0x2d8 no-op stub and handler table[189] is _purecall; orphan body 1404a01e0 is not dispatch proof, so no enum rename
        PetFlair                    = 190, // Pet flair requirement not met - client case 0xbe via Prerequisite_CheckPetFlair 1404a14d0 (+0x658); entity+0x80 type 0x14/0x17; objectId0 PetFlair id vs player flair state
        PetEntitySpell4             = 191, // Spell requirement not met - client case 0xbf inline Prerequisite_ResolveSpellOnPetEntity FUN_1404695e0; SpellService resolve on caster/pet entity+0x1600; NF proxy: visible vanity pet + vanity spell book entry (or spell id when objectId0 set)
        Unknown192                  = 192, // Path settler requirement not met - 3 tbl rows pair QuestState with value0=3, but live case 0xc0 dispatches vtable +0x630 no-op stub and handler table[192] is _purecall; orphan body 1404a0260 is not dispatch proof, so no enum rename
        Unknown193                  = 193, // Path settler requirement not met - 12 tbl rows objectId0=0, but live case 0xc1 dispatches vtable +0x2e0 no-op stub and handler table[193] is _purecall; orphan body 1404a02e0 is not dispatch proof, so no enum rename
        MountUsage                  = 194, // Ground mounts cannot be used in this area
        HoverboardUsage             = 195, // Hoverboard mounts cannot be used in this area
        Racial                      = 196, // Racial requirement not met
        GliderUsage                 = 197, // Gliders cannot be used in this area
        SpaceshipUsage              = 198, // Spaceships cannot be used in this area
        PublicEvent199              = 199, // Public event requirement not met
        PublicEventObjective200     = 200, // Public event objective requirement not met
        AchievementObject           = 201, // Achievement object requirement not met
        // 202 is unused in PrerequisiteType.tbl
        Unknown203                  = 203, // Exist in PrerequisiteType.tbl but does not have a description - rows exist, but live case 0xcb dispatches vtable +0x348 no-op stub; no semantic rename
        Dueling                     = 204, // Dueling requirement not met
        Personal2V2ArenaRating205   = 205, // Personal arena 2v2 rating requirement not met
        Personal3V3ArenaRating206   = 206, // Personal arena 3v3 rating requirement not met
        Personal5V5ArenaRating207   = 207, // Personal arena 5v5 rating requirement not met
        Battlegrounds208            = 208, // Battlegrounds requirement not met 
        Warplots209                 = 209, // Warplots requirement not met
        Personal2V2ArenaRating210   = 210, // Personal arena 2v2 rating requirement not met
        Personal3V3ArenaRating211   = 211, // Personal arena 3v3 rating requirement not met
        Personal5V5ArenaRating212   = 212, // Personal arena 5v5 rating requirement not met
        PersonalWarplotRating       = 213, // Personal warplots rating requirement not met
        SpellBaseId                 = 214, // Spell requirement not met
        Shield215                   = 215, // Shield requirement not met
        Shield216                   = 216, // Shield requirement not met
        PvpFlag                     = 217, // Target is not flagged for PvP.
        Datacube                    = 218, // Datacube requirement not met - handler table[218-221] stub 1407db510; NF uses IDatacubeManager progress bitmask (DatacubeType.Datacube) per SimpleEntity.OnActivateCast
        Volume                      = 219, // Volume requirement not met - handler table[218-221] stub 1407db510; NF uses IDatacubeManager progress bitmask (DatacubeType.Journal / datacube volume)
        PathMissionChecklistItemComplete = 220, // Exists without description - live case 0xdc vtable +0x498 -> Prerequisite_CheckPathMissionChecklistItemComplete_Table220 14049fe80; PathMissionRuntime_FindById(objectId0) then vtable +0x50(value0), used by Lua bIsComplete/bIsCompleted checklist/clue paths
        ActionSetSpell              = 221, // Spell requirement not met - live case 0xdd Prerequisite_CheckActionSetSpell 14049d6d0; NF scans all IActionSet shortcuts for SpellbookItem objectId0 Spell4 id
        Unknown222                  = 222, // Requirements not met - two rows exist, but live case 0xde dispatches vtable +0x380 no-op stub; no semantic rename
        Unknown223                  = 223, // Requirements not met - live case 0xdf vtable +0x438 -> Prerequisite_CheckAccountItemListItem2CountNpc97 14049f8c0; objectId0 Item2 row walk + NPC gate; duplicate-body alias, no semantic rename
        LevelGrantAbilityTierPoints = 224, // Level does not grant any ability tier points
        LevelGrantAmpPoints         = 225, // Level does not grant any AMP points
        Entitlement                 = 226, // Entitlement requirement not met
        EldanAugmentation227        = 227, // Eldan augmentation requirement not met
        TradeSkill228               = 228, // Tradeskill requirement not met
        TradeSkill229               = 229, // Tradeskill requirement not met
        WarplotRating               = 230, // Warplot rating requirement not met
        EldanAugmentation231        = 231, // Eldan augmentation requirement not met
        Plane                       = 232, // Plane requirement not met
        SpellTierUnlocked           = 233, // Spell requirement not met - live case 0xe9 vtable +0x1a8 -> Prerequisite_CheckSpellTierUnlocked 14049d820; objectId0 Spell4 row, ignores value0, Equal if current ability-book tier reaches Spell4.tierIndex
        WarplotUpgrade              = 234, // Warplot upgrade requirement not met
        BonusAbilityTierPoints      = 235, // Bonus ability tier point requirement not met
        AbilityTierPoints           = 236, // Ability tier points requirement not met
        BonusPower                  = 237, // Bonus power requirement not met
        Power                       = 238, // Power requirement not met
        ActionBar                   = 239, // Action bar does not meet requirements 
        Unknown240                  = 240, // Exists without description - live case 0xf0 vtable +0x2e8 -> Prerequisite_CheckCurrency 14049e7e0; objectId currency id, value amount; duplicate-body alias, no semantic rename
        HousingResidenceLoaded      = 241, // Exists without description - live case 0xf1 vtable +0x638 -> Prerequisite_CheckHousingResidenceLoaded 1404a11e0; NPC gate + active housing TargetResidence identity at world client +0x7500/+0x7508
        LiveEvent242                = 242, // Live event not complete
        Faction243                  = 243, // Faction requirement not met
        OwnsAccountItem               = 244, // Requirements not met - client case 0xf4 via Prerequisite_CheckOwnsAccountItem 14049ca70; objectId0 is account Item2 id; FUN_1401ed460 bind-point lookup; NPC entity types 0x14/0x17 only
        Unknown245                  = 245, // Exists without description - live case 0xf5 vtable +0x698 -> Prerequisite_CheckItemTradeSkill 1404a1790; NPC gate + item2 tradeskill tier requirement; duplicate-body alias, no semantic rename
        DoesNotOwnAccountItemOnCharacter = 246, // Requirements not met - value0 is account Item2 id; all rows use NotEqual; client case 0xf6 via Prerequisite_CheckDoesNotOwnAccountItemOnCharacter 14049d240 (+0x130; FUN_1403ac590 mode 0x707)
        // 247 is unused in PrerequisiteType.tbl
        NpcInventoryItemCount         = 248, // Requirements not met - client case 0xf8 via Prerequisite_CheckNpcInventoryItemCount 1404a0cb0; objectId0 is Item2 id; FUN_1405f68f0 counts item in NPC bags (type 0x1c) on entity types 0x14/0x17
        // 249 is unused in PrerequisiteType.tbl
        BaseFaction                 = 250, // Base faction requirement not met
        Personal2V2ArenaRating251   = 251, // Personal arena 2v2 rating requirement not met
        Personal3V3ArenaRating252   = 252, // Personal arena 3v3 rating requirement not met
        Personal5V5ArenaRating253   = 253, // Personal arena 5v5 rating requirement not met
        Battlegrounds254            = 254, // Battlegrounds requirement not met
        Warplots255                 = 255, // Warplots requirement not met
        WarplotRating256            = 256, // Warplot rating requirement not met
        // 257 is unused in PrerequisiteType.tbl
        // 258 is unused in PrerequisiteType.tbl
        Unknown259                  = 259, // Exist in PrerequisiteType.tbl but does not have a description - four rows use objectId0 38923/38924, but live case 0x103 dispatches vtable +0x530 no-op stub; no semantic rename
        Unknown260                  = 260, // Requirements not met - one orphan row 36343 nests HousingNeighborResidence and objectId1=12 HousingPlugItem, but live case 0x104 ignores object/value and checks active housing plot/plug flags 0x08/0x20 against state 5; state semantic blocked, no rename
        Personal2V2ArenaRating261   = 261, // Personal arena 2v2 rating requirement not met
        Personal3V3ArenaRating262   = 262, // Personal arena 3v3 rating requirement not met
        Personal5V5ArenaRating263   = 263, // Personal arena 5v5 rating requirement not met
        Battlegrounds264            = 264, // Battlegrounds requirement not met
        Warplots265                 = 265, // Warplots requirement not met
        PetOrEsperPetEntity        = 266, // Exists without description - client case 0x10a vtable +0xc8 -> 14049cae0 checks entity type 0x18/0x19 (Pet/EsperPet)
        Unknown267                  = 267, // Requirements not met - live case 0x10b vtable +0x320 -> Prerequisite_CheckEntityLookupFlagBit1_Table62 14049e9a0 (bit 1 of *(DAT_140c65898+0x6c50)+8); semantic blocked; not ScanCreature
        CREDDPendingOrderState      = 268, // Exists without description - live case 0x10c vtable +0x3c8 Prerequisite_CheckCREDDPendingOrderState_Table268 14049f090; compares CREDD pending-order flag to objectId0; row 38851 gates CREDD exchange NPC visibility with NotEqual objectId0=1
        RapidTransport              = 269, // Requirements not met - client case 0x10d via manager +0xd0; objectId0 is rapid-transport node id
        LoyaltyRewards              = 270, // Loyalty requirement not met
        Unknown271                  = 271, // Exist in PrerequisiteType.tbl but does not have a description - rows exist, but live case 0x10f dispatches vtable +0x178 no-op stub; no semantic rename
        Unknown272                  = 272, // Exist in PrerequisiteType.tbl but does not have a description - no retail rows found; live case 0x110 dispatches vtable +0x180 no-op stub; no semantic rename
        EntitlementCount            = 273, // Entitlement count requirement not met
        // 274 is unused in PrerequisiteType.tbl
        DoesNotOwnAccountItem         = 275, // Requirements not met - objectId0 is account Item2 id; all 277 rows use NotEqual and join accountitem.item2Id; client case 0x113 via manager +0x6a0
        OutOfBounds                 = 276, // You're out of bounds
        QuestObjective47OnCasterAndTarget = 277, // You cannot do that right now - client case 0x115 requires caster and target to pass QuestObjective47 (+0x2f8)
        Unknown278                  = 278, // Requirements not met - one tbl row referenced by Spell4.prerequisiteIdAoeTarget on test spell 84203; live case 0x116 calls vtable +0x300 on caster and target, but that slot is no-op stub 140001ba0, so no semantic rename
        UnderSpellOnCasterAndTarget   = 279, // Spell requirements not met - live case 0x117 Prerequisite_CheckUnderSpell 1404a4ec0 on caster and target; NF SpellPrerequisiteHelper.IsUnderSpell on both
        SpellTierOnCasterAndTarget    = 280, // Spell tier requirements not met - live case 0x118 dual Prerequisite_CheckSpellTier 1404a4f60 on caster and target IPlayer
        Falling                     = 281, // Falling requirement not met
        // 282 is unused in PrerequisiteType.tbl
        Unknown283                  = 283, // Marked as N/A - no retail rows found; live case 0x11b dispatches vtable +0x6a8 no-op stub; no semantic rename
        // 284 is unused in PrerequisiteType.tbl
        Unknown285                  = 285, // Exist in PrerequisiteType.tbl but does not have a description - two rows exist, but live case 0x11d dispatches vtable +0x6b0 no-op stub; no semantic rename
        Unknown286                  = 286, // Exists without description - live case 0x11e vtable +0x688 -> Prerequisite_CheckDailyLoginDaysTotal 1404a1710; accountItemList+0x180; duplicate-body alias, no semantic rename
        DailyLoginRewardsAvailable  = 287, // Exists without description - live case 0x11f vtable +0x690 -> Prerequisite_CheckDailyLoginRewardsAvailable 1404a1750; accountItemList+0x184
        PurchasedTitle              = 288, // Character title requirement not met
        Unknown289                  = 289, // Exists without description - live case 0x121 vtable +0x3d0 -> Prerequisite_CheckMount 14049f0c0; duplicate-body alias, no semantic rename
        Unknown290                  = 290, // Exists without description - live case 0x122 vtable +0x3a8 -> Prerequisite_CheckUnderForcedMovement 14049efa0; duplicate-body alias, no semantic rename
        Unknown291                  = 291, // Exists without description - live case 0x123 vtable +0x6c0 -> Prerequisite_CheckProgressTrackOnMatchingEntity 1404a1890; duplicate-body alias, no semantic rename
        PrimalMatrixNode            = 292, // Requirements not met - objectId is PrimalMatrixNode.tbl id; value0 is allocation threshold (always 1 in client rows); live case 0x124 vtable +0x6c8; handler table[292] _purecall; PrimalMatrix_LookupAllocatedNode 1404d6a60 is wired at handler table[179] 1404a18f0 only
        AccountCurrencyAmount       = 293, // Exists without description - client case 0x125 at 14049d3a0 reads accountItemList+0xd0+objectId0*8 AccountCurrencyType amount (objectId0=15 Crimson Essence row) then ApplyComparison vs value0
        Unknown294                  = 294, // Exist in PrerequisiteType.tbl but does not have a description - one unreferenced row 44546 (NotEqual, all ids/values zero); live case 0x126 vtable +0x6d0 and handler table[294] are no-op/_purecall
        Unknown295                  = 295  // Requirements not met - live case 0x127 NotEqual when evaluated entity exists (value0 ignored); NF PrerequisiteCheckUnknown295; row 44550 unreferenced
    }
}
