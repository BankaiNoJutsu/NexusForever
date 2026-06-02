# Placeholder rename tracker

Tracks `Unknown*` / opaque `Data*` placeholders in `Source/**` (excluding EF migration history).
Use the evidence ladder from `CONTINUATION_GUIDE.md`: Observed -> Correlated -> Mapped -> Verified -> Implemented.

**Inventory (2026-05-23):** ~761 `Unknown*` token matches in 176 `Source/**/*.cs` files (ripgrep); ~147 distinct symbol names; ~205 enum members (64 in `PrerequisiteType` alone).

## Initiative status — ACTIVE (2026-06-02 pass 100)

Pass 100 corrected handler-table[5] mislabel (`14049d470` is orphan entity-id list walk; live type 5 Reputation uses inline `entity+0x118` + `1404a2010`). Labeled `PrerequisiteManager_ApplyComparisonFloat` @ `1404a2010`.

## Initiative status — ACTIVE (2026-05-23 resume)

Decomp pass 130 added group stat-block layout mapping and retail catalog wire scalar names. Ghidra shared project was lock-blocked; used cached `140607490.fragment.c` and retail SQL.

## Current roadmap buckets (2026-06-02)

This tracker intentionally separates names that are safe to use from names that
must stay opaque until another evidence source appears.

### Implemented in the active prerequisite / diagnostic slice

- Mapped `PrerequisiteType` names with conservative NF checks:
  `AccountCurrencyAmount`, `CREDDPendingOrderState`, `PetOrEsperPetEntity`,
  `PathMissionChecklistItemComplete`, `HousingResidenceLoaded`,
  `HousingNeighborResidence`, `DailyLoginDaysTotal`,
  `DailyLoginRewardsAvailable`, `SpellTierUnlocked`, and the account-item /
  spell / path helper names listed below.
- Structural client packet names backed by row serializers:
  `ClientAccountRealmData`, `ClientRealmListRealmRow`, and
  `ClientRealmListMessageRow`. Their standalone send/consumer events remain
  blocked, so handlers stay diagnostic-only.

### Deliberately blocked / keep opaque

- No-op or `_purecall` prerequisite dispatch slots: `Unknown48`, `Unknown142`,
  `Unknown189`, `Unknown192`, `Unknown193`, `Unknown203`, `Unknown222`,
  `Unknown259`, `Unknown271`, `Unknown272`, `Unknown278`, `Unknown283`,
  `Unknown285`, and `Unknown294`.
- Duplicate-body aliases without a semantic owner: `Unknown223`, `Unknown240`,
  `Unknown245`, `Unknown286`, `Unknown289`, `Unknown290`, and `Unknown291`.
- Diagnostic predicates with blocked field owner: `Unknown267`, `Unknown260`,
  and `Unknown295`.
- Broad static/table placeholders such as `Stat.Unknown*`,
  `InventoryLocation.Unknown*`, `UserTextFlags.Unknown*`, and generic packet
  tails remain out of scope until consumer evidence proves names.

### Next evidence targets

- Runtime producer backlog: F-025 entity-stat aux, map-tracked-unit producer,
  F-004 neighborhood entry/list, F-008 crafting discovery / complex-craft /
  `0x084B` / `0x0855`, and F-031 retail Fortune weights.
- Packet `ValueN` fields: follow `ENTITY_AUX_DECODE_ROADMAP.md` one opcode
  cluster at a time; do not rename or emit from shape adjacency alone.
- Client diagnostics still needing a gameplay send-site or live sniff:
  `Client0x011B`, `Client0x011D`, `Client0x012D`, `Client0x0550`,
  `Client0x062A`, `Client0x0634`, `Client0x0701`, `Client0x07E3`, and
  `Client0x0928`.

## Initiative status — prior closure note

The 2026-05-23 closure pass marked the first tranche complete. Every in-scope placeholder was either **renamed with evidence** (below) or **explicitly blocked / out of scope** with Ghidra or runtime negative evidence. Remaining `Unknown*` in `Source/**` are intentional until a future pass adds per-field proof.

**In scope:** packet/account/storefront/pending-item/achievement/quest-objective placeholders touched by the 2026-05 decomp passes, plus correlated group stat-block packets (`0x0436` / `0x0468`).

**Out of scope (do not bulk-rename):** remaining `PrerequisiteType.UnknownNNN` members, generic `Network.World` packet tails and `Server0x*` unresolved rows, item wire offset names (`Unknown44`, …), `Stat` / `InventoryLocation` enums, table-backed `Game.Static` ids without client strings, and cosmetic renames (`Field6`, `PurchaseField0`, housing `Unknown0` without semantics).

**Re-open criteria:** new Ghidra consumer for a blocked offset, live capture with non-zero blocked wire fields, or NF runtime proof tying `QuestObjectiveType` / prerequisite id to behavior.

## Implemented (safe to use in code)

| Symbol | Real name | Evidence |
|--------|-----------|----------|
| Achievement `Data0` / `Data1` | `ProgressState` / `CreditedChecklistMask` | Client `Achievement_GetProgressValue` @ `140642b30`; NF achievement runtime |
| `QuestObjectiveType.Unknown10` | `ScriptedTargetGroupChecklist` | NF runtime + scripts |
| `QuestObjectiveType.Unknown20` | `CompletePublicEventRecipe` | `QuestObjective.Data` = `PublicEventObjective.Id` |
| `QuestObjectiveType.Unknown28` | `RescuePublicEventCreatures` | same |
| `QuestObjectiveType.Unknown31` | `CompletePublicEventObjective` | same |
| Account item flag `Unknown1` | `HasTargetPlayerIdentity` | `AccountInventoryItem_ReadPayload` @ `1400a9c20` |
| `ServerAccountCurrencyGrant` tail | `Reason` / `Reserved` | Packet shape tests |
| Storefront purchase shared payload | `PaymentCurrencySlot`, `PurchaseMoneyAmountBits`, `PurchaseOptionId`, `PurchaseExtensionId` | `Storefront_PurchaseCharacter_WritePayload` @ `1400acf80`; `StorefrontLib_PurchaseOffer` @ `1404f1150` |
| Storefront account purchase tail | `AccountPurchaseExtensionId`, `AccountTarget`, `RecipientName` | `Storefront_PurchaseAccount_WritePayload` @ `140080a20`; `Storefront_SendClientPurchaseAccountOffer` @ `1404507e0` |
| `PendingAccountItemGroup` wire fields | `SenderAccountId`, `TargetAccountId`, `HasTargetPlayerIdentity` (u64) | `PendingAccountItemGroup_ReadPayload` @ `1400a9b20`; cache insert @ `140005bf0`; NF `account_pending_item` columns |
| Group stat block | `StatBlockPrefix17`, `GroupMemberId`, `GroupMemberStatSlot` | `Group_CopyMemberStatBlockFromPayload` @ `140607490`; `GROUP_MEMBER_STAT_BLOCK.md` |
| Store catalog offer tail | `RetailCatalogWireScalar`, `RetailCatalogWireByte` | `RetailStoreOfferWireConstants.CatalogWireScalarBits`; retail `field_6`/`field_7`; not consumed in Apply @ `14044b750` |
| `AuctionInfo.UnknownArray` | `MicrochipIds` | NF marketplace persistence + shared `Item.Microchips` 3-bit wire array |
| `ServerMatchingListPlayersQueuedForMap.QueuedPlayerInfo.Unknown30`–`Unknown38` | `PrimeLevel`, `IsParty`, `Roles` | `MatchingQueuedPlayerInfo_ReadPayload` @ `140098500`; correlates with `ClientMatchingQueue_WritePayload` @ `140098a70` and `MatchingQueueJoinQueueData_ReadPayload` @ `1400989d0` |
| `ServerMatchingListPlayersQueuedForMap.QueuedPlayerInfo.Unknown3C` | `TrailingUInt32_0x3C` | `MatchingQueuedPlayerInfo_ReadPayload` @ `140098500`; uint32 wire confirmed, semantics blocked |
| `ServerSpellCastTargetReport.UnknownStructure0` / `unknownStructure0` | `TargetReportRow` / `TargetReportRows` | Partial row map (`CasterId` known); tail fields blocked |
| `PrerequisiteType.Spell221` / `Unknown269` | `ActionSetSpell` / `RapidTransport` | Table ids 221/269; client dispatch @ `1404a2100` cases `0xdd` / `0x10d` |
| `PrerequisiteType.Unknown170` | `GameFormula` | `value0` is `gameformula.id`; client case `0xaa` @ `1404a2100` (+0x90) |
| `PrerequisiteType.Unknown277` | `QuestObjective47OnCasterAndTarget` | Client case `0x115` requires caster **and** target to pass QuestObjective47 handler (+0x2f8) |
| `PrerequisiteType.Unknown292` | `PrimalMatrixNode` | `objectId0` matches `primalmatrixnode.id`; client case `0x124` (+0x6c8) |
| `PrerequisiteType.Unknown275` | `DoesNotOwnAccountItem` | All rows `NotEqual` and join `accountitem.item2Id`; client case `0x113` (+0x6a0) |
| `PrerequisiteType.Inventory` | `DoesNotOwnAccountItemOnCharacter` | All rows `NotEqual`; `value0` is account `item2.id`; client case `0xf6` (+0x130) |
| `PrerequisiteType.Unknown50` | `UnderSpellOnTarget` | Client case `0x32` calls `FUN_1404699f0` on target `param_2[1]`; `value0` is Spell4 id |
| `PrerequisiteType.Unknown59` | `SpellTier` | `objectId0` is Spell4 id; `value0` is tier rank; client case `0x3b` via `FUN_1404a4f60` |
| `PrerequisiteType.Unknown60` | `SpellTierOnTarget` | Client case `0x3c` calls `FUN_14046a110` on target `param_2[1]`; same row shape as `SpellTier` |
| `PrerequisiteType.Unknown56` | `DifficultyRankSlotAssigned` | Client case `0x38` checks populated pointer at `entity+0x2d8 + value0*0x10` when `value0 < 0x1c` |
| `PrerequisiteType.Unknown106` | `TargetEntityLookupHit` | Client `14049e900` (+0x318) resolves target identity `+0x1a0` via `FUN_14079ee60` |
| `PrerequisiteType.Unknown244` | `OwnsAccountItem` | Client `14049ca70` (+0xc0) checks account Item2 ownership on NPC types `0x14`/`0x17` |
| `PrerequisiteType.Unknown248` | `NpcInventoryItemCount` | Client `1404a0cb0` (+0x5c0) counts Item2 id on NPC via `1405f68f0` bag walk |
| `PrerequisiteType.Unknown279` | `UnderSpellOnCasterAndTarget` | Client case `0x117` requires caster **and** target to pass UnderSpell helper `FUN_1404a4ec0` |
| `PrerequisiteType.Unknown129` | `ActiveSpellEffectOnUnit` | Client case `0x81` inline walk of `entity+0x15c8` with SpellService compare on `value0` |
| `PrerequisiteType.Unknown130` | `ActiveSpellEffectOnTarget` | Client case `0x82` calls `FUN_140469a70` on caster with target `+8` Spell4 filter |
| `PrerequisiteType.Unknown82` | `UnitEntityType` | Client case `0x52` via `14049c5b0` (+0x48); entity `+0x80` unit type id vs `value0` |
| `PrerequisiteType.Unknown89` | `PetMatchTarget` | Client case `0x59` via `14049d4f0` (+0x160); pet lookup id match caster vs target |
| `PrerequisiteType.Unknown102`–`105` | `SpellCooldownNodeOnUnit` / `OnTarget` / `InServiceSet` / `SpellEffectTypeOnUnit` | Client cases `0x66`–`0x69` walk `entity+0x15d0` cooldown-node list |
| `PrerequisiteType.Unknown133`–`136` | `ItemStatData` / `ItemStatId` / `AppliedItemStatId` / `Item2Id` | Item eval handlers `1404a0d00`–`1404a0d90`; type `135` `objectId0` = `ItemStat.Id` @ `+0x144` |
| `PrerequisiteType.Unknown90` | `MountVehicleType` | Client `14049e730` (+0x2c0) mount subobject unit-type gate |
| `PrerequisiteType.Unknown191` | `PetEntitySpell4` | Client case `0xbf` inline `Prerequisite_ResolveSpellOnPetEntity` @ `1404695e0` on player/pet `entity+0x1600` |
| `PrerequisiteType.InfrastructureState` (148) | NF `PrerequisiteCheckInfrastructureState` | Client `1404a0150`; retail pairs `PathSettlerInfrastructure.Id` with `PathMission` type `21` (`0x15`) |
| `PrerequisiteType.PetEntitySpell4` (191) | NF `PrerequisiteCheckPetEntitySpell4` | Client case `0xbf`; vanity-pet summon proxy until `entity+0x1600` wrapper id is mapped |
| `PrerequisiteType.ItemRolledPropertyValue` (was `Unknown140`) | renamed pass 19 | `1404a0e80` + `14040e610` Property slot ids at `+0x94..+0xcc`; zero `Prerequisite.tbl` rows |
| `PrerequisiteType.ItemSpecial` (138) | NF handler pass 21 | `1404a0dd0` +0x114/+0x118; zero tbl rows typed 138 |
| `PrerequisiteType.ItemMicrochip` (139) | NF handler pass 21/24 | `1404a0e10` + `14049bdc0`; objectId0 7–0xd = `RuneType` socket bitmask via `IItem.RuneSlots`; wire `Microchips[]` separate |
| Augment microchip **install** opcode | pass 44/46 mapped (replication + partial patch) | **Client C2S install** = `0x085B` only. Wire **`Microchips[]`** on **full add** via `SharedItem_ReadPayload` → `140569c90` → `14056aa20`. **Existing-slot microchip-only patch** = `1403b8540` (caller PE `0x1403eef8a`, cluster `0x1403eed00`) — **player opcode still blocked** (pass 47: **not** `0x046F` guild bank) |
| Partial inventory slot field apply | pass 46 correlated | PE cluster `0x1403eed00`–`0x1403ef100` per-field patches on **player bag**; distinct from `0x0111` and from **`0x046F`/`0x047A` guild bank** |
| `ServerGuildBankInventoryAdd_ReadPayload` | pass 47–48 mapped | `140092580` = **`0x046F`** @ `14006c290`; apply `GuildBank_ApplyInventoryAdd` `14057d190`; apply-table cell **`140e1a760`** |
| `ServerGuildBankTabInventory_ReadPayload` | pass 47–48 mapped | `140092470` = **`0x047A`**; apply `GuildBank_ApplyTabInventoryRows` `14057cdc0`; apply-table cell **`140e1a718`** |
| Partial inventory field-apply gap | pass 48 inspect | Cluster **`0x1403eed00`**; microchip case **`0x1403eef8a`→`1403b8540`**; inspect fragments in cache; **not** spell jumptable `0x1403f1344`; opcode still blocked |
| `Prerequisite_CheckFaction` | pass 47 mapped | Type **128** case `0x80` @ `+0x68` → `14049c720` + `PlayerFactionService_IsFactionOrAncestor` `1407176b0` |
| `ItemAdded` native subscribers | pass 44 mapped | Only dispatcher `1403b8060` in `string_xrefs`; Lua ClientEvent paths + `140409330` case **9** `uItem` refresh; no additional labeled C++ subscriber in export cache |
| `Game.ItemData.GetDetailedInfo` | pass 45 mapped | `140417de0` PE registration at `.data` `0x140c58c10` with string `GetDetailedInfo`; builds `tPrimary`/`tCompare` (not `GetRuneSlots`) |
| Tooltip `tRunes` | pass 45 mapped | `140673ab0` reuses `140673b80` for tooltip context; distinct Lua key from `GetRuneSlots` / `GetDetailedInfo` |
| `PrerequisiteType.ItemMicrochip` objectId0=0 | pass 45 rejected on client | `1404a0e10` bitmask-only; rows 10610/10685 need NF count proxy until alternate client handler found |
| ItemSpecial row `+0x10` → item-eval `+0x114` | pass 35 correlated + rejected | SQL `spell4IdOnEquip`; augment rows hold Spell4 FK (81886+). Do not use as socket mask except rare values ⊆ socket-bit set (only id 3694=4 in reference DB) |
| Category **177** fusion-tier `item2TypeId` | pass 36 correlated | Types **515/516/530/563** (and empty **514/517**) map to `RuneType.Fusion` via `item2TypeId ≥ 502`; elemental band **340–345** unchanged (base **333**) |
| `Item.Build` sparse `Glyphs[]` | pass 37 implemented | `ItemRuneNetworkWire` emits one glyph id per `RuneSlots` index (0 = empty); wire→`itemData+0x518` producer still blocked |
| Wire → shared-runtime | pass 39 mapped | `SharedItemRuntime_ApplyParseBuffer` 140411b60: glyphs `parse+0x88` → runtime `+0x8f`; microchips via `ItemEval_ApplyItemSpecialToEval` → runtime `+0x20` (Inspect-only via 1403deff0) |
| Wire → entity inventory item | pass 40 mapped | `InventoryItem_ApplySharedItemPayload` 140569c90: glyphs `parse+0x88` → entity `+0xbc`; microchips `parse+0x78` → entity `+0x98`; compact sockets via `InventoryItem_RefreshItemState` entity `+0x60` |
| Entity item → item-eval | pass 41 mapped | `InventoryItem_RefreshItemState` 14056a430 → `ItemEval_ApplyItemSpecialToEval` with entity `+0xbc`/`+0x98`/`+0x60` |
| Entity `+0xbc` → shared-runtime `[0x8f]` | pass 42 mapped | `ItemEval_CommitRuneDataFromLinkedEntity` 140413520 on `ItemEval_CommitPendingRuneData` 140412ad0 reads `(*runtime)[0]+0xbc` |
| Entity `+0x60` → runtime socket fields | pass 42 mapped | Same commit copies entity `+0x60` compact bytes into runtime `+0x5d` region (GetRuneSlots `+0x388` proxy via `+0x71` index) |
| GetRuneSlots `+0x544` / `[0xa9]` gates | pass 43 mapped | Scratch-context indices alias embedded shared-runtime `+0x4a4` / `[0x95]`; filled by `ItemEval_SyncFromItemDataUserdata` `140410680` from Lua `Game.ItemData` userdata (`140417660` + `140410300`), not separate parent-only writers |
| Entity item → Game.ItemData | blocked (narrowed) | No direct entity→userdata writer; live `GetRuneSlots` reads userdata native blob synced via `140410680`; entity glyphs reach userdata through eval commit `140413520` + Lua refresh (`ItemAdded` / case 9), not `ItemModified`→`itemData` |
| Slot update → shared runtime | pass 41 mapped | `Inventory_ApplySlotUpdateAndDispatchItemAdded` 1403b8380 → slot `Entity_GetIndexedSlotEntry` + `ItemEval_CommitPendingRuneData` + `ItemAdded` |
| Wire → item-eval | pass 38 mapped | `ItemEval_SyncFromSharedItem` 140410300 copies runtime `+0x20`/`+0x158` into item-eval; `itemData+0x388`/`+0x518` writer still blocked |
| `FUN_14040f320` | `ItemRuneType_ToCompactSocketId` | RuneType 7–13 → compact 1–7 for `0x0859` `IsNotFusion` (`14051ed80`) |
| `PrerequisiteType.Unknown92`–`93` | `ActiveSpellTargetMechanic` / `Spell4EffectCategoryOnUnit` | Client cases `0x5c`/`0x5d` on active spell-effect list |
| `PrerequisiteType.Unknown280` | `SpellTierOnCasterAndTarget` | Client case `0x118` requires caster **and** target to pass SpellTier helper `FUN_1404a4f60` |
| `PrerequisiteType.Unknown77` | `QuestObjectiveOnTarget` | Client case `0x4d` (+0x548) passes target context; `objectId0` is QuestObjective id |
| `PrerequisiteType.Unknown172` | `HealthScaled` | Client case `0xac` inline: `Entity_GetHealthScaleFactor` `14047a940` * health `+0xd08+0x8c`; fallback `Creature2_GetRowByUnitId` `14022d500` on `+0xd8` |
| `PrerequisiteType.Unknown174` | `ItemTradeSkillKnown` | `PrerequisiteTypeHandlerTable[174]` → `Prerequisite_CheckItemTradeSkillKnown` `1404a17e0` → `TradeSkill_ClientHasKnownItem2Id` `1403b91d0` (known-recipe list binary search); NPC types `0x14`/`0x17` |
| `PrerequisiteType.ItemTradeSkill` (173) | NF `PrerequisiteCheckItemTradeSkill` | Handler table `1404a1790` → `TradeSkill_CheckItemTradeskillRequirement` `1403c16e0`; tier rank from `TradeskillTier` + `GetTradeskillXp` |
| `PrerequisiteType.ItemTradeSkillKnown` (174) | NF `PrerequisiteCheckItemTradeSkillKnown` | Handler table `1404a17e0` → `TradeSkill_ClientHasKnownItem2Id` `1403b91d0`; `HasLearnedSchematic` on schematics referencing Item2 |
| `PrerequisiteType.Unknown178` | `ProgressTrackOnMatchingEntity` | Handler table[178] `1404a1890` + `Progress_GetTrackedScalar` `1403fa980`; NF uses challenge completion count when `objectId0` is a `Challenge.tbl` id (track table `+0x8010` still blocked) |
| `PrerequisiteType.TradeSkill` (175) | NF `PrerequisiteCheckTradeSkill` | Handler table stub; NF tier rank via `HasTradeskill` + `TradeskillTier` |
| `PrerequisiteType.ChallengeObject` (176) | NF `PrerequisiteCheckChallengeObject` | Handler table stub; NF `IsChallengeActivated` for `Challenge.tbl` id |
| `PrerequisiteType.TrueLevel` (177) | NF `PrerequisiteCheckPlayerGlobalTreeLookup` | Handler `1404a1830` + `1403d407b`; enum name unverified — rename blocked |
| `PrerequisiteType.CreatureDifficultyRank` (180) | NF `PrerequisiteCheckCreatureDifficultyRank` | Handler table stub; NF `RankValue` on target difficulty row |
| `PrerequisiteType.CreatureDifficulty` (179) | NF `PrerequisiteCheckCreatureDifficulty` | Spell path + NF manager; handler table[179] `1404a18f0` is PrimalMatrix lookup `1404d6a60` (documented mismatch) |
| `PrerequisiteType.PrimalMatrixNode` (292) | NF stub `PrerequisiteCheckPrimalMatrixNode` | Live vtable `+0x6c8`; handler table `_purecall`; allocation runtime blocked |
| `PrerequisiteType.LiveEventCount` (168) | `AccountItemCount` | Handler table[168] `Prerequisite_CheckAccountItemCount` `1404a1620` → `AccountItem_CountOwnedByItem2Id` `1403d2140`; live case `0xa8` still vtable `+0x5a0` |
| `PrerequisiteType.LiveEvent169` (169) | `AccountItemCountCompared` | Handler table[169] `Prerequisite_CheckAccountItemCountCompared` `1404a1670`; NF `PrerequisiteCheckAccountItemCountCompared` |
| `PrerequisiteType.Unknown171` (171) | `DailyLoginDaysTotal` | Handler table[171] reads accountItemList+0x180 = DailyLogin Value0; NF `GetDailyLoginDaysTotal()` |
| `PrerequisiteType.Unknown96` | `EvalContextFloatByObjectId` | Handler table[96] `Prerequisite_CheckEvalContextFloatByObjectId` `14049f810`; active eval-context list key `+0x20`, float `+0x28`; NF item stat eval proxy |
| `PrerequisiteType.Unknown97`-`Unknown99` | `AccountItemListItem2CountNpc97` / `AccountItemListItem2CountNpc98` / `AccountItemListItem2CountNpc99` | Handler table entries `14049f8c0` / `14049f990` / `14049f9d0`; NPC type gate then `AccountItemList_WalkItem2IdMatch`; numeric suffix retained because no finer native distinction is proven |
| `PrerequisiteType.Unknown100`-`Unknown101` | `AccountItemListItem2Count100` / `AccountItemListItem2Count101` | Handler table entries `14049fa80` / `14049fac0`; same account Item2 row walk without NPC gate; numeric suffix retained because no finer native distinction is proven |
| `PrerequisiteType.Unknown223` | no enum rename | Raw PE vtable `PTR_FUN_140b67740+0x438` -> `14049f8c0`; same NPC-gated account Item2 row-count handler as type 97, but no semantic owner beyond duplicate-body alias |
| `PrerequisiteType.Unknown233` | `SpellTierUnlocked` | Live vtable `+0x1a8` -> `Prerequisite_CheckSpellTierUnlocked` `14049d820`; `objectId0` Spell4 row, compares ability-book tier data to Spell4.tierIndex, ignores `value0`, Equal/NotEqual only |
| `PrerequisiteType.Unknown240` | no enum rename | Raw PE vtable `+0x2e8` -> `Prerequisite_CheckCurrency` `14049e7e0`; `objectId` currency id, `value` amount, but no semantic owner beyond duplicate-body alias |
| `PrerequisiteType.Unknown241` | `HousingResidenceLoaded` | Live vtable `+0x638` -> `Prerequisite_CheckHousingResidenceLoaded` `1404a11e0`; NPC gate then Equal/NotEqual on active housing TargetResidence identity at world client `+0x7500/+0x7508` |
| `PrerequisiteType.Unknown186` | `HousingNeighborResidence` | Live vtable `+0x628` -> `Prerequisite_CheckHousingNeighborResidence_Table186` `1404a10e0`; NPC gate then Equal/NotEqual on current housing residence identity presence in cached neighbor map. Only retail row `17536` pairs `HouseOwnership` with type 186 for housing seed/relic/active-prop gates. |
| `PrerequisiteType.Unknown245` | no enum rename | Raw PE vtable `+0x698` -> `Prerequisite_CheckItemTradeSkill` `1404a1790`; NPC-gated Item2 tradeskill tier requirement, but no semantic owner beyond duplicate-body alias |
| `PrerequisiteType.Unknown286` | no enum rename | Raw PE vtable `+0x688` -> `Prerequisite_CheckDailyLoginDaysTotal` `1404a1710`; accountItemList+0x180, but no semantic owner beyond duplicate-body alias |
| `PrerequisiteType.Unknown287` | `DailyLoginRewardsAvailable` | Raw PE vtable `+0x690` -> `Prerequisite_CheckDailyLoginRewardsAvailable` `1404a1750`; accountItemList+0x184 |
| `PrerequisiteType.Unknown289` | no enum rename | Raw PE vtable `+0x3d0` -> `Prerequisite_CheckMount` `14049f0c0`; duplicate of type 84 body without proven finer semantic owner |
| `PrerequisiteType.Unknown290` | no enum rename | Raw PE vtable `+0x3a8` -> `Prerequisite_CheckUnderForcedMovement` `14049efa0`; duplicate of type 79 body without proven finer semantic owner |
| `PrerequisiteType.Unknown291` | no enum rename | Raw PE vtable `+0x6c0` -> `Prerequisite_CheckProgressTrackOnMatchingEntity` `1404a1890`; duplicate of type 178 body without proven finer semantic owner |
| `PrerequisiteType.Unknown293` | `AccountCurrencyAmount` | `InspectCodeAddress` at `14049d3a0`: for `objectId0 <= 18` reads `DAT_140c635f0+0x15d0+0xd0+objectId0*8`, otherwise compares zero, then `ApplyComparison`; account-item add/remove/update writers `140006000`/`140004c40`/`140005600` prove `+0xd0` slots are `AccountItem.accountCurrencyEnum` balances; row 44322 uses objectId0=15 Crimson Essence, value0=5000 |
| `PrerequisiteType.Unknown266` | `PetOrEsperPetEntity` | Corrected mapping: live case `0x10a` uses decimal vtable operand `200` (`+0xc8`) -> `14049cae0`; instruction window reads entity `+0x80`, subtracts `0x18`, and accepts values `0x18`/`0x19`, which NF `EntityType` maps to `Pet`/`EsperPet`. This is not matching/PvP candidate `14049e300`. |
| `PrerequisiteType.Unknown268` | `CREDDPendingOrderState` | Live case `0x10c` vtable `+0x3c8` -> `Prerequisite_CheckCREDDPendingOrderState_Table268` `14049f090`; compares `DAT_140c635f0+0x1708` CREDD pending-order flag to `objectId0`. Row `38851` is `NotEqual objectId0=1` and gates `creature2.prerequisiteIdVisibility` for CREDD Exchange NPCs in Thayd/Illium and the MTX Protostar consultant. |
| `PrerequisiteType.Unknown159` | `PositionalRequirementBetweenCasterAndTarget` | Client case `0x9f` dispatches vtable `+0x1e0` -> `14049d940`; body resolves `PositionalRequirement` from `objectId0`, computes angular cone using row `AngleCenter`/`AngleRange`, checks caster-vs-target bearing, and swaps facing source when row `Flags & 1`; retail row 6117 uses objectId0=6 (`AngleCenter=0`, `AngleRange=300`, `Flags=0`). |
| `PrerequisiteType.Unknown63` | `IsLocalPlayerEntity` | Client case `0x3f` dispatches vtable `+0x78` -> raw `14049c7b0`; body compares evaluated entity `+8` identity to `DAT_140c65898+0x78` local-player entity `+8`, supports Equal/NotEqual only, and retail rows 3977/22213 are used by `VisualEffect.prerequisiteId` local-vs-nonlocal gates. |
| `PrerequisiteType.PublicEventParticipant166` (166) | `LiveEventTreeLookup` | Handler table[166] `1404a1580` → LiveEventTree_LookupByObjectId 1404a7f50; NF requires NPC target + LiveEvent row (progress proxy 0) |
| `PrerequisiteType.LiveEvent167` (167) | `LiveEventWorldFactionBranch` | Handler table[167] `1404a15d0` → 1404a80b0 + LiveEvent_GetWorldFactionField 1404a8430 (world 0xa6/0xa7 = Dominion/Exile fields +0x68/+0x88) |
| `PrerequisiteType.GameFormula` (170) live path | `Prerequisite_CheckEntitySetCountAt7188` @ 14049c880 | Counts NPC entities in player global +0x7188 set; distinct from tbl formula-id and handler-table +0x110 paths |
| `PrerequisiteType.HealthScaled` (172) | NF `PrerequisiteCheckHealthScaled` | Live case `0xac` inline scaled health; NF uses raw `IWorldEntity.Health` proxy; handler table[172] = `accountItemList+0x184` daily-login rewards |
| `PrerequisiteType.Unknown220` | `PathMissionChecklistItemComplete` | Live case `0xdc` vtable `+0x498` -> `Prerequisite_CheckPathMissionChecklistItemComplete_Table220` `14049fe80`; NF proxy checks completed missions as complete and otherwise tests persisted path mission `ProgressData` bit `value` for `value < 32`; per-type checklist/clue ownership remains blocked. |
| `PrerequisiteType.Unknown48` | no enum rename | Two retail rows: `10846` pairs `QuestObjectiveOnTarget` with type 48 but is unreferenced; `20451` is NotEqual zero and is referenced by `Spell4Effects.prerequisiteIdCasterPersistence` on dungeon raid-wipe timer spell 47945 effects. Live case `0x30` calls vtable `+0x1e8`, but that slot resolves to no-op stub `140001ba0`; semantic owner blocked. |
| `PrerequisiteType.Unknown142` | no enum rename | 9 retail rows; references are Spell4Effects caster/target apply gates for deprecated Spellslinger variants and Dangerous Game. Live case `0x8e` passes eval-context `param_2[4]` to vtable `+0x170`, but that slot resolves to no-op stub `140001ba0`; eval-context slot meaning remains blocked. |
| `Client0x0928` | blocked | Shared wire with `ClientPetSetStance` (`0x068E`); no gameplay PE send site (registration batch `1400a8524` only) |
| `PrerequisiteType.Unknown189` (189) | no enum rename | Orphan label `Prerequisite_CheckPathSettlerHubBuildProgress` (`1404a01e0`) reads record+0x60, but live case `0xbd` dispatches a no-op stub and handler table[189] is `_purecall`; NF prerequisite handler removed until dispatch proof exists |
| `PrerequisiteType.Unknown192` (192) | no enum rename | Orphan label `Prerequisite_CheckPathSettlerHubContributionProgress` (`1404a0260`) reads record+0x68, but live case `0xc0` dispatches a no-op stub and handler table[192] is `_purecall`; NF prerequisite handler removed until dispatch proof exists |
| `PrerequisiteType.Unknown193` (193) | no enum rename | Orphan label `Prerequisite_CheckPathSettlerHubProgressPercent` (`1404a02e0`) reads record+0x64, but live case `0xc1` dispatches a no-op stub and handler table[193] is `_purecall`; NF prerequisite handler removed until dispatch proof exists |
| `accountItemList+0x110` (type 170 handler table) | blocked | `1404a16c0` compares scalar vs `value0`; `140022192` uses +0x108/+0x110; NF `GetAccountInventoryItemCount()` row-count proxy only |
| `PrerequisiteTypeHandlerTable` base | `0x140b67870` (not `0x140b66870`) | PE pointer scan for `1404a1620` / type **168** slot; pass 52 correction |
| `Client0x003D` | `ClientAccountRealmData` | `ClientAccountRealmData_WritePayload` @ `1400aba70` = one `AccountRealmData` row; nested in `ClientRealmListRealmRow`; standalone send blocked |
| `Client0x0760` | `ClientRealmListRealmRow` | `Client0x0760_WritePayload` @ `1400abd30` + `ServerRealmList` realm row serializer; send-site still blocked |
| `Client0x0762` | `ClientRealmListMessageRow` | `Client0x0762_WritePayload` @ `1400ac410` + `ServerRealmList` message row serializer; send-site still blocked |
| Prerequisite handler evidence | pass 49/86 | Use **`0x140b67870` / `PrerequisiteTypeHandlerTable[typeId]`** for handler bodies; **`PTR_FUN_140b67740` vtable slots** often disagree (destructor/UI) — do not rename from vtable PE pointer alone |
| `AchievementEntry.Value` | `RequiredProgress` | Achievement progress runtime |
| `Quest2Entry.FactionLevelCompPreq*` | `FactionLevelRequireAtMostPreq*` | Quest prerequisite comparison semantics |
| `RewardRotationModifierEntry.Value` | `ModifierValue` | Reward rotation schedule builder |
| Reputation packets `Value` | `ReputationDelta` / `ReputationAmount` | Packet shape tests |
| `GroupCharacter.UnknownStruct1` | `PrimeLevelInfo` / `PrimeLevels` | `MatchingPrimeLevelInfo_ReadPayload` @ `1400ad150` |
| `GroupCharacter.Unknown11`–`Unknown22` | `Health` … `HealingAbsorbMax` (packed `ushort` vitals) | `GroupCharacter_ReadPayload` @ `140082420` `+0x54`..`+0x6a`; same set as `ServerGroupMemberStatUpdate` |
| `GroupCharacter.Unknown28`/`Unknown29` | `PhaseFlags1` / `PhaseFlags2` | Parsed `+0x94`/`+0x98`; matches `ServerGroupMemberStatUpdate` @ `140083d30` |
| Spell damage `UnknownStructure3` | `TrailingStructure` / `TrailingStructures` | `SpellDamageDescription_ReadPayload` @ `1400946c0`; row reader `SpellDamageTrailingRow_ReadPayload` @ `1400945e0` (seven damage uint32s + 3-bit tail; row consumer blocked) |
| `TrailingStructure.Unknown0`–`Unknown7` | `RawDamage` … `GlanceAmount`, `DamageType` (3-bit) | Same field widths as parent `DamageDescription` via `SpellDamageTrailingRow_ReadPayload` @ `1400945e0`; per-row gameplay use blocked |
| `ServerSpellEffectNestedTargets` placeholders | `ServerUniqueId`, `Spell4EffectId`, `TargetId`, `DamageDescriptions` | Reader @ `140095810`; shared damage row @ `1400946c0`; structural diff vs `0x07F6` (`ServerSpellEffectDamage_ReadPayload` @ `140095910`) |
| `0x07F6` post-parse consumer | `SpellWrapper_ApplyServerEffectDamage` @ `1405a5800`, `SpellWrapper_DispatchEffectDamageCombatLog` @ `14053f3f0` | Via `Entity_ExecuteSpellEffectHighRange` jump case `1403ee268`; combat-log primary row only; trailing rows still blocked |
| `ServerP2PTradeUpdateItem` unknown tail | `RandomCircuitData`, `RandomGlyphData`, `ThresholdData`, `WorldRequirement_Item2Id`, `Item2IdMicrochip[5]` | Client opcode `0x018E` read `FUN_1400aa2b0` @ `1400aa2b0`, write `FUN_1400aa030` @ `1400aa030`; same tail as mail `ServerMailAvailable.Attachment` minus charges and eight glyph slots |

## Mapped - storefront purchase (`0x082A` / `0x0828`) - detail reference

Decompiled 2026-05-23 (`InspectCodeAddress` on `WildStar64.exe`).

**Wire order** (`Storefront_PurchaseCharacter_WritePayload` @ `1400acf80`):

| Index | Width | Client source (`StorefrontLib_PurchaseOffer` @ `1404f1150` -> `Storefront_SendClientPurchaseCharacterOffer` @ `140450720`) | Proposed name | Confidence |
|-------|-------|------------------------------------------------------------------------------------------------------------------|---------------|------------|
| `[0]` | u32 | Offer id (`uVar3`) | `OfferId` | Verified |
| `[1]` | 5-bit | `Game.Money` field at `+0x14` (`param_4`) | `PaymentCurrencySlot` | Implemented |
| `[2]` | u32 | `Game.Money` amount float bits (`param_5`) | `PurchaseMoneyAmountBits` | Implemented |
| `[3]` | 14-bit | Global table constant `*(DAT_140c7aac0+8)` | `CurrencyId` (NF property name) | Verified in handlers |
| `[4]` | u32 | Lua purchase arg 3 (`plVar13`) | `PurchaseOptionId` | Implemented |
| `[6..]` | Identity | Current player `+0x1a0/+0x1a8` | `Target` | Verified |
| `[10]` | u32 | Lua purchase arg 4 (`uVar5`) | `PurchaseExtensionId` | Implemented |

NF handlers currently use only `OfferId` and `CurrencyId` (14-bit field); other fields are client-side pricing/UI context.

Account route adds after shared payload: `AccountPurchaseExtensionId` (u32 at struct `+0x30`, same Lua arg 4 binding as `PurchaseExtensionId` but second wire slot), `AccountTarget`, `RecipientName` (`Storefront_PurchaseAccount_WritePayload` @ `140080a20`).

## Mapped - `ServerStoreOffers` offer row (`0x098B`)

`ServerStoreOffers_Offer_ReadPayload` @ `1400a0dc0` struct layout (client parse):

| Offset | Size | NF wire | Retail DB | Status |
|--------|------|---------|-----------|--------|
| `+0x00` | u32 | `Id` | `id` | Verified |
| `+0x08` | wstr | `Name` | `name` | Verified |
| `+0x10` | wstr | `Description` | `description` | Verified |
| `+0x18` | 8 | `PricePremium` / `PriceAlternative` | (derived in NF from prices table) | Verified |
| `+0x20` | u32 | `DisplayFlags` | `displayFlags` | Verified |
| `+0x28` | i64 | `RetailCatalogWireScalar` | `field_6` (`RetailStoreOfferWireConstants.CatalogWireScalarBits`) | **Mapped** - retail wire passthrough; Apply @ `14044b750` does not copy |
| `+0x30` | 8 bits | `RetailCatalogWireTrailingByte` | `field_7` (usually `0`) | **Mapped** - `FUN_14006be30` @ `1400a0dc0` |
| `+0x34` | u32 | currency row count | - | Verified |
| `+0x40` | u32 | item row count | - | Verified |

Do **not** rename to bare `Field6`/`Field7`; use `RetailCatalogWireScalar` / `RetailCatalogWireByte` for the documented passthrough.

## Blocked — per-field (closed without rename)

| Symbol | Evidence summary |
|--------|------------------|
| `PendingAccountItemGroup.Unknown2` | Wire `+0x10`; cache `[2]` @ `140005bf0`; lookup by `Id` only @ `140007810`; gift UI @ `140519260` uses group string at cache `+0x38` and UI target fields — not slot `[2]` |
| `GroupCharacter.StatBlockPrefix17` | 17-bit prefix at parsed `+0x1c` in stat block; copied to client `+0x98`; gameplay meaning blocked |
| `GroupMemberStatSlot.Value` | Five-row ushort in stat block; per-row semantics blocked |
| `GroupCharacter.Unknown10` | `uint32` after mentoring (`+0x50`); no labeled consumer |
| `QuestObjectiveType.Unknown27` / `Unknown29` | No localization / single-digit table counts; no NF runtime handler |
| `PathMissionChecklistItemComplete` exact checklist runtime | Enum/check implemented with an NF proxy; exact per-path-type `+0x50` checklist/clue ownership remains blocked until path mission runtime state is modeled beyond completed-mission and persisted `ProgressData` bits. |
| `PrerequisiteType.Unknown260` | One retail row `36343` nests `OtherPrerequisite` -> row `17536` (`HouseOwnership` + `HousingNeighborResidence`) and type 260 Equal with `objectId1=12` (`HousingPlugItem` row flags `2`), but no imported `wildstar_client` prerequisite reference column points at row `36343`. Live case `0x104` vtable `+0x640` -> `1404a1220` ignores row object/value and resolves active housing plot/plug state through `HousingPlotInfo`, current/default `HousingPlugItem`, plug flags `0x08`/`0x20`, and active residence fields `+0x60/+0x64` vs state value `5`; state semantic remains unmapped, so no enum rename. |
| Matching/rating candidate `14049e300` | Rejected as type 266: live prerequisite case `0x10a` uses decimal vtable operand `200` (`+0xc8`) -> `14049cae0`, not `+0x200`. `14049e300` remains orphan/unassigned until a caller or slot proves ownership. |
| `PrerequisiteType.ScanCreature` native path | Enum name remains table/text-backed, but stale labels were rejected: handler table[62] `14049e9a0` is `Prerequisite_CheckEntityLookupFlagBit1_Table62`, not a scientist checklist walk; live case `0x3e` dispatches vtable `+0x4a8` -> raw `14049ff30`, which has scan-looking target bitmask fields but no safe semantic label yet. |
| `PrerequisiteType.Unknown267` | No enum rename: live case `0x10b` vtable `+0x320` -> diagnostic body `Prerequisite_CheckEntityLookupFlagBit1_Table62` `14049e9a0` (same body as mislabeled handler table[62]); bit-1 semantics at `*(DAT_140c65898+0x6c50)+8` still blocked — not `ScanCreature`. |
| `Prerequisite_CheckScanCreature_LiveCase3E` (`14049ff30`) | Pass 88: live type 62 case `0x3e` vtable `+0x4a8`; target-unit scan bitmask predicate documented; NF still uses datacube-only `PrerequisiteCheckScanCreature`. |
| `PrerequisiteType.HousingNeighborResidence` (186) | Pass 95: `PrerequisiteCheckHousingNeighborResidence` proxies neighbor presence on player residence; native visit-context map membership still blocked. |
| `Prerequisite_CheckHousingPlotPlugState_Table260` (`1404a1220`) | Pass 95: diagnostic label for type 260 live body; enum rename still blocked. |
| `PrerequisiteType.Unknown144` | No `wildstar_client.prerequisite` rows found and live `PrerequisiteManager_EvaluateTypeSlot` skips case `0x90` (`0x8f` -> vtable `+0x3a0`, `0x91` -> inline Equal true). Handler table[144] `1404a0b70` is labelled only as `Prerequisite_CheckLiveEventStateEqualsOne_Table144`: it looks up `objectId` through `PublicEventService_GetLiveEventById` and tests runtime record `+0x1d0 == 1`, but reachability and field meaning are blocked, so no enum rename. |
| Handler table[5] `14049d470` | Renamed pass 100 to `Prerequisite_CheckEntityIdInEvalContextList_Table5Orphan` — walks eval-context `+0x2b8` list; **not** live type 5. Live `Reputation` = case `0x5` inline `entity+0x118` vtable `+0x20` + `PrerequisiteManager_ApplyComparisonFloat` `1404a2010`. |
| `PrerequisiteType.Unknown203` | Rows exist, but live case `0xcb` dispatches vtable `+0x348` -> `140001ba0` no-op stub; no semantic owner is proven. |
| `PrerequisiteType.Unknown222` | Rows `9470`/`9472` exist, but live case `0xde` dispatches vtable `+0x380` -> `140001ba0` no-op stub; no semantic owner is proven. |
| `PrerequisiteType.Unknown259` | Rows `39430`/`39431`/`39799`/`39800` use objectId0 `38923`/`38924`, but live case `0x103` dispatches vtable `+0x530` -> `140001ba0` no-op stub; no semantic owner is proven. |
| `PrerequisiteType.Unknown271` | Rows exist, but live case `0x10f` dispatches vtable `+0x178` -> `140001ba0` no-op stub; no semantic owner is proven. |
| `PrerequisiteType.Unknown272` | No retail rows found and live case `0x110` dispatches vtable `+0x180` -> `140001ba0` no-op stub; no semantic owner is proven. |
| `PrerequisiteType.Unknown278` | One row (`40531`, Equal `objectId0=1`, `value0=1`) is referenced by `Spell4.prerequisiteIdAoeTarget` on `[TEST] Origin/Target Prereqs - PBAE - Tier 3` (`Spell4` 84203). Live case `0x116` calls vtable `+0x300` on caster and target, but that slot resolves to no-op stub `140001ba0`; no semantic owner is proven. |
| `PrerequisiteType.Unknown283` | No retail rows found and live case `0x11b` dispatches vtable `+0x6a8` -> `140001ba0` no-op stub; no semantic owner is proven. |
| `PrerequisiteType.Unknown285` | Rows `41892`/`43208` exist, but live case `0x11d` dispatches vtable `+0x6b0` -> `140001ba0` no-op stub; no semantic owner is proven. |
| `PrerequisiteType.Unknown294` | One row (`44546`) is unreferenced in every `wildstar_client` column containing `prerequisite`; it uses `NotEqual` with all ids/values zero. Live case `0x126` dispatches vtable `+0x6d0` -> `140001ba0` no-op stub, and handler table[294] is `_purecall`; no semantic rename. |
| `PrerequisiteType.Unknown295` | Pass 98: `PrerequisiteCheckUnknown295` proxies live case `0x127` (NotEqual + evaluated entity present). Row `44550` still unreferenced; enum rename blocked. |

## Blocked buckets (do not rename without new evidence)

| Bucket | Count | Notes |
|--------|------:|-------|
| `PrerequisiteType.UnknownNNN` | remaining | Table ids + generic failure strings only — **out of scope** |
| `Network.World` packet tails | ~400 refs | Many `Server0xNNNN` shapes decoded but event semantics open — **out of scope** |
| `Stat.Unknown*`, `InventoryLocation.Unknown*` | ~15 | No client stat/location enum strings |
| `EntityCreateFlag.Unknown08` | 1 | Used in cinematics; no label |
| Marketplace `AuctionInfo.Unknown2` | 1 | No consumer label |
| Item packet offset names (`Unknown44`, etc.) | many | Layout known, semantics not |

## Mapped - `PendingAccountItemGroup` (`0x0976` / `0x0979`) - detail reference

| Offset | NF property | Notes |
|--------|-------------|-------|
| `+0x00` | `Id` | Pending item id |
| `+0x08` | `AccountItemId` | |
| `+0x10` | `Unknown2` | **Blocked** - stored in client cache `[2]`; grouped-map lookup uses `Id` at `+0x00` only (`AccountPendingItemGroupCache_LookupByPendingItemId` @ `140007810`); gift/claim UI paths read `TargetAccountId` at `+0x38`, not this field |
| `+0x18` | `Group` | Wide string key |
| `+0x20` | `SenderAccountId` | Implemented |
| `+0x28` | `SenderIdentity` | |
| `+0x38` | `TargetAccountId` | Implemented; account-gift target (`ClientAccountItemGiftPendingItemGroupToAccount`) |
| `+0x40` | `ClaimState` | 5-bit |
| `+0x48` | `HasTargetPlayerIdentity` | Wire u64; NF emits `0`/`1` |
| after | `TargetIdentity` | Second identity in 0x60 struct |

## Deferred (future passes — not part of closed initiative)

1. `PendingAccountItemGroup.Unknown2` — live `0x0979` capture with non-zero `+0x10`, or Lua/UI reading cache `[2]`.
2. `ServerStoreOffers.Offer.Unknown6`/`Unknown7` — any client path outside catalog Apply that consumes `+0x28/+0x30` (purchase history @ `14044c540` uses a different row layout).
3. `0x082A` purchase `[3]` 14-bit — live sniff vs `AccountCurrencyType` if handlers diverge from retail.
4. Group roster tail — decompile consumer for `GroupCharacter.Unknown10` (`uint32` @ parsed `+0x50` after mentoring).

## Deferred - structurally mapped packet opcodes

| Opcode | Structure | Evidence | Blocker |
|--------|-----------|----------|---------|
| `Client0x011B` / `Client0x011D` / `Client0x012D` | empty / `uint32` / wide string | Pass 94 comparison scan: native registration is confirmed at `Network_RegisterServerOpcode_0351` (`14006c290`) with `0x011B` -> zero-payload writer `140001ba0`, `0x011D` -> `ClientUInt32_ReadPayload` / `ClientTradeskillResetTalents_WritePayload`, and `0x012D` -> `ClientSuggest_WritePayload`. Non-registration hits are rejected: `0x011B`/`0x011D` in `LuaLexer_ReadNextToken`/parser functions are Lua token ids, `Movement_UpdateAndSendFallStateOpcodes` uses `GameFormula_GetEntryById(0x11B)`, and `SpellCast_SendClientCastSpellOrPosition` returns `CastResult.IllegalSpellCast` (`0x011D`) rather than sending opcode `0x011D`. | Gameplay sender/consumer or live sniff before semantic rename |
| `Client0x0550` / `0x062A` / `0x0634` / `0x07E3` | `uint32` via `14007d010` | Pass 58 PE: sole `mov eax` each in `ClientWorldOpcodeRegister_MovementSpline` (`1400a824e`/`82b6`/`872c`/`8282`); **not** `0x05D5` (gameplay sends `140075918`/`14076ab61`) | Indirect send rail or live sniff before semantic rename |
| `Client0x0701` | 2-bit + `uint32` | `Network_RegisterServerOpcode_0351` @ `14006c290`; writer `1400a69d0`; sole `mov eax,0x701` @ `140079e05` (harness only) | Gameplay sender/consumer before rename |

## Verification (initiative closure)

```powershell
dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj `
  --filter "FullyQualifiedName~PacketPlaceholderNamingTests|FullyQualifiedName~AccountInventoryPendingGroupTests|FullyQualifiedName~GroupPacketShapeTests|FullyQualifiedName~StorefrontPurchaseHandlerTests" -v minimal --nologo
```

## Commands

```powershell
.\Decomp\Analysis\run_ghidra_analysis.ps1 -ExportOnly -Targets WildStar64.exe -MaxDecompiledFunctions 0 `
  -ExtraPostScript InspectCodeAddress.java -ExtraPostScriptArgs @('1400acf80') -SkipCoverage
```

Log: `Decomp/Analysis/logs/WildStar64.NexusForeverClient64_WildStar64.InspectCodeAddress.ghidra.log`
