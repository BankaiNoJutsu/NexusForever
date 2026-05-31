namespace NexusForever.Game.Static.Prerequisite
{
    public enum PrerequisiteType
    {
        None                        = 0,
        Level                       = 1, // Level requirement not met
        Race                        = 2, // Race requirement not met
        Class                       = 3, // Class requirement not met
        Faction                     = 4, // Faction requirement not met
        Reputation                  = 5, // Reputation requirement not met
        QuestState                  = 6, // Quest requirement not met
        AchievementState            = 7, // Achievement requirement not met
        ItemProficiency             = 8, // Item proficiency requirement not met
        EpisodeState                = 9, // Episode requirement not met
        Gender                      = 10, // Gender requirement not met - client enum PrerequisiteComp_Sex
        OtherPrerequisite           = 11, // Other requirement not met
        DeadState                   = 12, // Player death state not correct
        ItemEquipped                = 13, // Item equipment requirement not met
        ItemOnCharacter             = 14, // Inventory requirement not met
        UnderSpell                  = 15, // Spell requirements not met
        DistanceToWorldLocation     = 16, // Distance requirements not met
        // 17 is unused in PrerequisiteType.tbl
        ScheduledEvent              = 18, // You cannot do that at this time
        TradeSkillProfession        = 19, // You must have the correct tradeskill tier.
        GroupSize                   = 20, // Incorrect party size
        TradeSkillSchematicLevel    = 21, // Incorrect schematic level
        QuestObjective0             = 22, // Quest objective requirements not met
        QuestObjective1             = 23, // Quest objective requirements not met
        InPhase                     = 24, // You cannot do that
        CanSeePhase                 = 25, // You cannot do that
        InSubZone                   = 26, // Incorrect zone
        InTriggerVolume             = 27, // You are out of bounds
        InCombat                    = 28, // You must be in combat
        TimeOfDay                   = 29, // Incorrect time of day
        FacilityWithinDistance      = 30, // Distance requirement not met
        CreatureWithinDistance      = 31, // Distance requirement not met
        // 32 is unused in PrerequisiteType.tbl
        QuestObjective2             = 33, // Quest objective requirements not met
        QuestObjective3             = 34, // Quest objective requirements not met
        QuestObjective4             = 35, // Quest objective requirements not met
        QuestObjective5             = 36, // Quest objective requirements not met
        RandomPercent               = 37, // Random check: Failed
        IsCreature                  = 38, // Incorrect creature
        IsPlayer                    = 39, // Incorrect player
        Health                      = 40, // Incorrect health
        ZoneExplored                = 41, // Zone exploration criteria not met
        IsGroupLeader               = 42, // You cannot do that
        IsObjectiveActive           = 43, // You cannot do that right now
        InTargetGroup               = 44, // You cannot do that right now
        // 45 is unused in PrerequisiteType.tbl
        Waypoint                    = 46, // Incorrect waypoint direction
        QuestObjective47            = 47, // You cannot do that right now - Value is QuestObjective id
        Unknown48                   = 48, // You cannot do that right now - client case 0x30 via manager +0x1e8; vtable slot is no-op stub (140001ba0); 1 tbl row (NotEqual, all ids zero)
        Schedule                    = 49, // That schedule is unavailable
        UnderSpellOnTarget            = 50, // You cannot do that now - client case 0x32 calls FUN_1404699f0 on target param_2[1]; value0 is Spell4 id
        ItemQuantity                = 51, // You do not have the correct number of items
        Path                        = 52, // You do not meet the player path requirement
        PathEpisode                 = 53, // You do not meet the player path episode requirement
        PlayerPathMission           = 54, // You do not meet the player path mission requirement
        Currency                    = 55, // Requirements not met - ObjectId is CurrencyType id, value is required amount
        DifficultyRankSlotAssigned    = 56, // Requirements not met - client case 0x38 checks entity+0x2d8 slot at index value0 (< 0x1c) is populated
        // 57 is unused in PrerequisiteType.tbl
        // 58 is unused in PrerequisiteType.tbl
        SpellTier                     = 59, // Spell tier requirement not met - objectId0 is Spell4 id; value0 is tier rank; client case 0x3b via FUN_1404a4f60
        SpellTierOnTarget             = 60, // Spell tier requirement on target not met - client case 0x3c calls FUN_14046a110 on target param_2[1]; objectId0 is Spell4 id; value0 is tier rank
        PathMissionCount            = 61, // Path mission count is incorrect 
        ScanCreature                = 62, // Unable to scan this creature - Probably HasScannedCreature
        Unknown63                   = 63, // Requirements not met
        PathTypeLevel               = 64, // Requirements not met - ObjectId is path type, value is minimum path level for that path
        Deprecated65                = 65, // Marked as DEPRECATED
        Deprecated66                = 66, // Marked as DEPRECATED
        PathMissionRequirement      = 67, // You do not meet the path mission requirement
        QuestObjective              = 68, // Quest objective requirement not met
        ChallengeRequirement        = 69, // Challenge requirement not met
        ChallengeTier               = 70, // Challenge tier requirement not met
        ClassProgress               = 71, // Requirements not met - ObjectId is Class id, value is class progression threshold
        // 72 is unused in PrerequisiteType.tbl
        Vital                       = 73, // Requirements not met - Part of Vital
        ChallengeLocked             = 74, // The specified challenge is locked
        // 75 is unused in PrerequisiteType.tbl
        CreatureState               = 76, // Requirements not met - ObjectId is often Creature2 id, value is unit state index
        QuestObjectiveOnTarget        = 77, // Requirements not met - client case 0x4d via manager +0x548 with target param_2[1]; objectId0 is QuestObjective id when non-zero (15159 seen)
        MovementMode                = 78, // Invalid movement mode
        UnderForcedMovement         = 79, // Not under forced movement
        HealthRequirement           = 80, // You do not meet the health requirements
        WrongSpellMechanic          = 81, // Wrong spell mechanic
        UnitEntityType              = 82, // Requirements not met - client case 0x52 via manager +0x48 (14049c5b0); compares entity+0x80 unit type id to value0
        Vehicle                     = 83, // Vehicle conditions not met
        Mount                       = 84, // Mount conditions not met
        Taxi                        = 85, // Taxi conditions not met
        Jump                        = 86, // Jump requirement not met
        Moving                      = 87, // Moving requirement not met
        Pet                         = 88, // Pet requirements not met
        PetMatchTarget              = 89, // Requirements not met - client case 0x59 via Prerequisite_CheckPetMatchTarget 14049d4f0 (+0x160); compares pet lookup ids from FUN_1403dec00(entity+0x138) for caster vs target
        MountVehicleType            = 90, // Requirements not met - client case 0x5a via Prerequisite_CheckMountVehicleType 14049e730 (+0x2c0); mount subobject at entity+0xf00 unit-type filter (types 3-5)
        PathLevel                   = 91, // Incorrect path level
        ActiveSpellTargetMechanic   = 92, // Spell conditions not met - client case 0x5c via Prerequisite_CheckActiveSpellTargetMechanic 14049d5b0 (+0x168); active spell-effect list scan for Spell4TargetMechanic flags
        Spell4EffectCategoryOnUnit  = 93, // Spell conditions not met - client case 0x5d inline walk of entity+0x15c8; nested spell-effect category at +0x70/+0xf8 equals value0
        QuestObjective6             = 94, // Quest objective requirements not met
        Distance                    = 95, // Distance requirements not met
        Unknown96                   = 96, // Requirements not met - client case 0x60 via manager +0x1b0 stub (140001ba0); 1 tbl row (objectId1=3, value0=10)
        Unknown97                   = 97, // Requirements not met - client case 0x61 via manager +0x1b8 stub (140001ba0); 1 tbl row
        Unknown98                   = 98, // Requirements not met - client case 0x62 via manager +0x1c0 stub (140001ba0); 3 tbl rows
        Unknown99                   = 99, // Requirements not met - client case 99 via manager +0x1c8 stub (140001ba0); 2 tbl rows
        Unknown100                  = 100, // Requirements not met - client case 100 via manager +0x1d0 stub (140001ba0); 1 tbl row
        Unknown101                  = 101, // Requirements not met - client case 0x65 via manager +0x1d8 stub (140001ba0); 4 tbl rows
        SpellCooldownNodeOnUnit       = 102, // Spell effect requirements not met - client case 0x66 via FUN_1404a4fe0; walks entity+0x15d0 cooldown-node list (+0x30 id) for value0
        SpellCooldownNodeOnTarget     = 103, // Spell effect requirements not met - client case 0x67 via FUN_14046a190; cooldown node on caster matching value0 and target param_2[1]+8 via +0x20
        SpellCooldownNodeInServiceSet = 104, // Spell effect requirements not met - client case 0x68 via FUN_14046a210; caster entity+0x15d0 node id present in SpellService set from +0x40
        SpellEffectTypeOnUnit         = 105, // Spell effect requirements not met - client case 0x69 inline walk of entity+0x15d0 nodes; vtable +0x38 result equals value0
        TargetEntityLookupHit         = 106, // Requirements not met - client case 0x6a via Prerequisite_CheckTargetEntityLookupHit 14049e900; FUN_14079ee60 binary-searches client entity table on target identity (+0x1a0)
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
        Faction128                  = 128, // Faction requirement not met
        ActiveSpellEffectOnUnit       = 129, // Spell requirement not met - client case 0x81 inline walk of entity+0x15c8 active spell effects; SpellService compare on value0 via DAT_140c65b70+0x38
        ActiveSpellEffectOnTarget     = 130, // Spell requirement not met - client case 0x82 calls FUN_140469a70 on caster with target param_2[1]+8 Spell4 filter and value0 compare
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
        Unknown142                  = 142, // Spell requirement not met - client case 0x8e via manager +0x170 passes eval context param_2[4]; vtable slot is no-op stub (140001ba0); 9 tbl rows (value0 often 3)
        Difficulty                  = 143, // Difficulty requirement not met
        Unknown144                  = 144, // Exist in PrerequisiteType.tbl but does not have a description
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
        Unknown159                  = 159, // Requirements not met
        Challenge160                = 160, // Challenge requirement not met
        // 161 is unused in PrerequisiteType.tbl
        PublicEventObjective2       = 162, // Public event objective requirement not met
        PublicEventParticipantCount = 163, // Public event participant count requirement not met
        PublicEventParticipant164   = 164, // Public event participant requirement not met
        // 165 is unused in PrerequisiteType.tbl
        PublicEventParticipant166   = 166, // Public event participant requirement not met
        LiveEvent167                = 167, // Live event requirement not met
        LiveEventCount              = 168, // Live event count requirement not correct
        LiveEvent169                = 169, // Live event requirement not met
        GameFormula                   = 170, // Requirements not met - value0 is GameFormula.tbl id (client case 0xaa via manager +0x90)
        Unknown171                  = 171, // Requirements not met
        Unknown172                  = 172, // Requirements not met
        ItemTradeSkill              = 173, // Item tradeskill requirement not met
        Unknown174                  = 174, // Item requirement not met
        TradeSkill                  = 175, // Tradeskill requirement not met
        ChallengeObject             = 176, // Challenge object requirement not met
        TrueLevel                   = 177, // True level requirement not met
        Unknown178                  = 178, // Item of type Equipped?
        CreatureDifficulty          = 179, // Requirements not met - ObjectId is Creature2Difficulty id (spell apply path)
        CreatureDifficultyRank      = 180, // Requirements not met - Value is Creature2Difficulty.rankValue
        // 181 is unused in PrerequisiteType.tbl
        HouseOwnership              = 182, // Housing ownership requirement not met
        Guild                       = 183, // Guild requirement not met - client case 0xb7 via manager +0x328 (14049e9e0); walks player guild list at entity+0xa0/+0xa8
        Guild2                      = 184, // Guild requirement not met - client case 0xb8 via manager +0x330 (14049ea30); guild registry DAT_140c7de18 with target param_2[1]
        GuildPerk                   = 185, // Guild perk requirement not met - client case 0xb9 via Prerequisite_CheckGuildPerk 14049eae0 (+0x338); guild perk bitmask at plVar1+0x42
        Unknown186                  = 186, // Requirements not met
        WarplotPlugUpgrade          = 187, // Warplot plug upgrade tier requirement not met
        WarplotPermission           = 188, // Warplot permission requirement not met - client case 0xbc via Prerequisite_CheckWarplotCirclePerk 14049ebf0 (+0x340); circle guild type 3 perk bitmask or DAT_140c65b98+400 fallback
        Unknown189                  = 189, // No PrerequisiteType.tbl description - client case 0xbd via manager +0x2d8 stub (140001ba0); 9 Prerequisite rows (value0 often 3); objectId0 maps PathSettlerHub(5)/PathSettlerImprovementGroup(84); orphaned DAT_140c65960 percent helper 1404a01e0 (+inner+4, record+0x60, no direct calls)
        PetFlair                    = 190, // Pet flair requirement not met - client case 0xbe via Prerequisite_CheckPetFlair 1404a14d0 (+0x658); entity+0x80 type 0x14/0x17; objectId0 PetFlair id vs player flair state
        PetEntitySpell4             = 191, // Spell requirement not met - client case 0xbf inline Prerequisite_ResolveSpellOnPetEntity FUN_1404695e0; SpellService resolve on caster/pet entity+0x1600; NF proxy: visible vanity pet + vanity spell book entry (or spell id when objectId0 set)
        Unknown192                  = 192, // No PrerequisiteType.tbl description - client case 0xc0 via manager +0x630 stub (140001ba0); 3 Prerequisite rows pair QuestState(6) NotEqual Quest2 7109/7115 with type 192 cmp2 value0=3
        Unknown193                  = 193, // Requirement not met - client case 0xc1 via manager +0x2e0 stub (140001ba0); orphaned PathSettlerHub percent helper 1404a02e0 (FUN_140432960 on DAT_140c65960, zero direct calls); 12 tbl rows all objectId0=0
        MountUsage                  = 194, // Ground mounts cannot be used in this area
        HoverboardUsage             = 195, // Hoverboard mounts cannot be used in this area
        Racial                      = 196, // Racial requirement not met
        GliderUsage                 = 197, // Gliders cannot be used in this area
        SpaceshipUsage              = 198, // Spaceships cannot be used in this area
        PublicEvent199              = 199, // Public event requirement not met
        PublicEventObjective200     = 200, // Public event objective requirement not met
        AchievementObject           = 201, // Achievement object requirement not met
        // 202 is unused in PrerequisiteType.tbl
        Unknown203                  = 203, // Exist in PrerequisiteType.tbl but does not have a description
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
        Datacube                    = 218, // Datacube requirement not met
        Volume                      = 219, // Volume requirement not met
        Unknown220                  = 220, // Exist in PrerequisiteType.tbl but does not have a description
        ActionSetSpell              = 221, // Spell requirement not met - client case 0xdd via manager +0x190 (14049d6d0); local player action-bar slot search for objectId0 Spell4 id
        Unknown222                  = 222, // Requirements not met
        Unknown223                  = 223, // Requirements not met
        LevelGrantAbilityTierPoints = 224, // Level does not grant any ability tier points
        LevelGrantAmpPoints         = 225, // Level does not grant any AMP points
        Entitlement                 = 226, // Entitlement requirement not met
        EldanAugmentation227        = 227, // Eldan augmentation requirement not met
        TradeSkill228               = 228, // Tradeskill requirement not met
        TradeSkill229               = 229, // Tradeskill requirement not met
        WarplotRating               = 230, // Warplot rating requirement not met
        EldanAugmentation231        = 231, // Eldan augmentation requirement not met
        Plane                       = 232, // Plane requirement not met
        Unknown233                  = 233, // Spell requirement not met
        WarplotUpgrade              = 234, // Warplot upgrade requirement not met
        BonusAbilityTierPoints      = 235, // Bonus ability tier point requirement not met
        AbilityTierPoints           = 236, // Ability tier points requirement not met
        BonusPower                  = 237, // Bonus power requirement not met
        Power                       = 238, // Power requirement not met
        ActionBar                   = 239, // Action bar does not meet requirements 
        Unknown240                  = 240, // Exist in PrerequisiteType.tbl but does not have a description
        Unknown241                  = 241, // Exist in PrerequisiteType.tbl but does not have a description
        LiveEvent242                = 242, // Live event not complete
        Faction243                  = 243, // Faction requirement not met
        OwnsAccountItem               = 244, // Requirements not met - client case 0xf4 via Prerequisite_CheckOwnsAccountItem 14049ca70; objectId0 is account Item2 id; FUN_1401ed460 bind-point lookup; NPC entity types 0x14/0x17 only
        Unknown245                  = 245, // Exist in PrerequisiteType.tbl but does not have a description
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
        Unknown259                  = 259, // Exist in PrerequisiteType.tbl but does not have a description
        Unknown260                  = 260, // Requirements not met
        Personal2V2ArenaRating261   = 261, // Personal arena 2v2 rating requirement not met
        Personal3V3ArenaRating262   = 262, // Personal arena 3v3 rating requirement not met
        Personal5V5ArenaRating263   = 263, // Personal arena 5v5 rating requirement not met
        Battlegrounds264            = 264, // Battlegrounds requirement not met
        Warplots265                 = 265, // Warplots requirement not met
        Unknown266                  = 266, // Exist in PrerequisiteType.tbl but does not have a description
        Unknown267                  = 267, // Exist in PrerequisiteType.tbl but does not have a description
        Unknown268                  = 268, // Exist in PrerequisiteType.tbl but does not have a description
        RapidTransport              = 269, // Requirements not met - client case 0x10d via manager +0xd0; objectId0 is rapid-transport node id
        LoyaltyRewards              = 270, // Loyalty requirement not met
        Unknown271                  = 271, // Exist in PrerequisiteType.tbl but does not have a description
        Unknown272                  = 272, // Exist in PrerequisiteType.tbl but does not have a description
        EntitlementCount            = 273, // Entitlement count requirement not met
        // 274 is unused in PrerequisiteType.tbl
        DoesNotOwnAccountItem         = 275, // Requirements not met - objectId0 is account Item2 id; all 277 rows use NotEqual and join accountitem.item2Id; client case 0x113 via manager +0x6a0
        OutOfBounds                 = 276, // You're out of bounds
        QuestObjective47OnCasterAndTarget = 277, // You cannot do that right now - client case 0x115 requires caster and target to pass QuestObjective47 (+0x2f8)
        Unknown278                  = 278, // Requirements not met - client case 0x116 dual +0x300 on caster and target; vtable slot is no-op stub (140001ba0); 1 tbl row (objectId0=1, value0=1)
        UnderSpellOnCasterAndTarget   = 279, // Spell requirements not met - client case 0x117 requires caster and target to pass UnderSpell helper FUN_1404a4ec0
        SpellTierOnCasterAndTarget    = 280, // Spell tier requirements not met - client case 0x118 requires caster and target to pass SpellTier helper FUN_1404a4f60
        Falling                     = 281, // Falling requirement not met
        // 282 is unused in PrerequisiteType.tbl
        Unknown283                  = 283, // Marked as N/A
        // 284 is unused in PrerequisiteType.tbl
        Unknown285                  = 285, // Exist in PrerequisiteType.tbl but does not have a description
        Unknown286                  = 286, // Exist in PrerequisiteType.tbl but does not have a description
        Unknown287                  = 287, // Exist in PrerequisiteType.tbl but does not have a description
        PurchasedTitle              = 288, // Character title requirement not met
        Unknown289                  = 289, // Exist in PrerequisiteType.tbl but does not have a description
        Unknown290                  = 290, // Exist in PrerequisiteType.tbl but does not have a description
        Unknown291                  = 291, // Exist in PrerequisiteType.tbl but does not have a description
        PrimalMatrixNode            = 292, // Requirements not met - objectId is PrimalMatrixNode.tbl id; value0 is allocation threshold (always 1 in client rows); client case 0x124 via 1404a18f0 (+0x6c8) local-player FUN_1404d6a60 node check
        Unknown293                  = 293, // Exist in PrerequisiteType.tbl but does not have a description
        Unknown294                  = 294, // Exist in PrerequisiteType.tbl but does not have a description
        Unknown295                  = 295  // Requirements not met           
    }
}
