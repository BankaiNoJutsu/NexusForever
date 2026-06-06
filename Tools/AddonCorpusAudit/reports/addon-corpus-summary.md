# WildStar Addon Corpus Audit

- Source: `I:\Wildstar Addons`
- Archives: 876
- Archives with Lua: 875
- Decode/read errors: 0

## Generated Files

- `addon_api_summary.json`
- `addon_apollo_api_scan.json`
- `addon_category_map.tsv`
- `addon_event_frequency.tsv`
- `addon_inventory.txt`

## Category Coverage

| Category | Addons | Top markers |
| --- | ---: | --- |
| Challenge | 110 | Challenge, ChallengesLib |
| Crafting | 112 | Rune, CraftingLib, Tradeskill, Schematic |
| Events | 763 | RegisterEventHandler |
| GameLib | 759 | GameLib |
| Group | 190 | GroupLib, Raid, ReadyCheck |
| Housing | 87 | HousingLib, Decor, Residence |
| Loot | 41 | LootRoll, MasterLoot |
| Mail | 34 | MailSystemLib, Mailbox |
| Market | 44 | Auction, MarketplaceLib, Commodity, CREDD |
| Matching | 168 | Queue, MatchMaker, MatchingGameLib, MatchMakingLib, GroupFinder, VoteKick, Surrender |
| Network | 86 | JoinChannel, ICCommLib |

## Top Apollo Events

| Event | Addons |
| --- | ---: |
| `KeyDown` | 235 |
| `InterfaceMenuListHasLoaded` | 191 |
| `WindowManagementReady` | 144 |
| `CharacterCreated` | 138 |
| `UnitEnteredCombat` | 133 |
| `UnitCreated` | 125 |
| `UnitDestroyed` | 106 |
| `ChangeWorld` | 101 |
| `NextFrame` | 77 |
| `TargetUnitChanged` | 76 |
| `ChatMessage` | 75 |
| `VarChange_FrameCount` | 67 |
| `SubZoneChanged` | 65 |
| `Group_Left` | 64 |
| `Group_Join` | 54 |
| `CombatLogDamage` | 48 |
| `Group_Remove` | 45 |
| `Group_Add` | 43 |
| `QuestStateChanged` | 41 |
| `SystemKeyDown` | 39 |
| `Group_Updated` | 34 |
| `PublicEventStart` | 33 |
| `QuestObjectiveUpdated` | 33 |
| `Tutorial_RequestUIAnchor` | 32 |
| `AbilityBookChange` | 30 |
| `PlayerLevelChange` | 30 |
| `UnitActivationTypeChanged` | 30 |
| `VarChange_ZoneName` | 30 |
| `PublicEventEnd` | 29 |
| `PlayerPathMissionUpdate` | 28 |

## Top APIs By Namespace

### apollo

| API | Addons |
| --- | ---: |
| `Apollo.RegisterAddon` | 869 |
| `Apollo.RegisterEventHandler` | 763 |
| `Apollo.LoadForm` | 755 |
| `Apollo.RegisterSlashCommand` | 653 |
| `Apollo.AddAddonErrorText` | 473 |
| `Apollo.GetAddon` | 399 |
| `Apollo.GetString` | 357 |
| `Apollo.GetPackage` | 252 |
| `Apollo.RegisterTimerHandler` | 247 |
| `Apollo.RegisterPackage` | 243 |
| `Apollo.CreateTimer` | 223 |
| `Apollo.LoadSprites` | 206 |
| `Apollo.GetConsoleVariable` | 179 |
| `Apollo.RemoveEventHandler` | 172 |
| `Apollo.StopTimer` | 137 |

### challengeslib

| API | Addons |
| --- | ---: |
| `ChallengesLib.GetActiveChallengeList` | 32 |
| `ChallengesLib.ChallengeType_Ability` | 10 |
| `ChallengesLib.ChallengeType_Combat` | 10 |
| `ChallengesLib.ChallengeType_General` | 10 |
| `ChallengesLib.ChallengeType_Item` | 10 |
| `ChallengesLib.ActivateChallenge` | 6 |
| `ChallengesLib.ShowHintArrow` | 6 |
| `ChallengesLib.GetRewardList` | 4 |
| `ChallengesLib.AbandonChallenge` | 2 |
| `ChallengesLib.GenerateRewardList` | 2 |
| `ChallengesLib.AtChallengeStartLocation` | 1 |
| `ChallengesLib.GetLootBonusMultiplier` | 1 |

### craftinglib

| API | Addons |
| --- | ---: |
| `CraftingLib.CodeEnumTradeskill` | 25 |
| `CraftingLib.GetKnownTradeskills` | 25 |
| `CraftingLib.GetTradeskillInfo` | 25 |
| `CraftingLib.GetSchematicInfo` | 18 |
| `CraftingLib.CodeEnumTradeskillResult` | 9 |
| `CraftingLib.CraftItem` | 9 |
| `CraftingLib.GetSchematicList` | 8 |
| `CraftingLib.CodeEnumTradeskillTier` | 6 |
| `CraftingLib.GetCurrentCraft` | 6 |
| `CraftingLib.CompleteCraft` | 5 |
| `CraftingLib.GetValidGlyphableItems` | 5 |
| `CraftingLib.GetAvailablePowerCores` | 4 |
| `CraftingLib.AddAdditive` | 3 |
| `CraftingLib.ClearSigil` | 3 |
| `CraftingLib.GetEngravingInfo` | 3 |

### gamelib

| API | Addons |
| --- | ---: |
| `GameLib.CodeEnumAddonSaveLevel` | 583 |
| `GameLib.GetPlayerUnit` | 531 |
| `GameLib.CodeEnumClass` | 137 |
| `GameLib.CodeEnumInputMouse` | 113 |
| `GameLib.GetTargetUnit` | 94 |
| `GameLib.GetRealmName` | 90 |
| `GameLib.GetCurrentZoneMap` | 74 |
| `GameLib.GetGameTime` | 73 |
| `GameLib.SetTargetUnit` | 62 |
| `GameLib.GetPlayerCurrency` | 59 |
| `GameLib.CodeEnumConfirmButtonType` | 58 |
| `GameLib.GetAccountRealmCharacter` | 51 |
| `GameLib.GetLocalTime` | 44 |
| `GameLib.GetSpell` | 40 |
| `GameLib.WorldLocToScreenPoint` | 34 |

### grouplib

| API | Addons |
| --- | ---: |
| `GroupLib.GetMemberCount` | 104 |
| `GroupLib.GetGroupMember` | 89 |
| `GroupLib.InGroup` | 71 |
| `GroupLib.GetUnitForGroupMember` | 61 |
| `GroupLib.InRaid` | 43 |
| `GroupLib.InInstance` | 28 |
| `GroupLib.AmILeader` | 25 |
| `GroupLib.Difficulty` | 15 |
| `GroupLib.ConvertToRaid` | 10 |
| `GroupLib.Invite` | 10 |
| `GroupLib.ReadyCheck` | 10 |
| `GroupLib.GetGroupMaxSize` | 8 |
| `GroupLib.GetLootRules` | 6 |
| `GroupLib.Kick` | 6 |
| `GroupLib.LeaveGroup` | 6 |

### guildlib

| API | Addons |
| --- | ---: |
| `GuildLib.GetGuilds` | 71 |
| `GuildLib.GuildType_Guild` | 54 |
| `GuildLib.GuildType_WarParty` | 23 |
| `GuildLib.GuildType_Circle` | 14 |
| `GuildLib.GuildType_ArenaTeam_2v2` | 8 |
| `GuildLib.GuildType_ArenaTeam_3v3` | 8 |
| `GuildLib.GuildType_ArenaTeam_5v5` | 8 |
| `GuildLib.GuildResult_GuildDisbanded` | 4 |
| `GuildLib.GuildResult_KickedYou` | 4 |
| `GuildLib.GuildResult_YouQuit` | 4 |
| `GuildLib.Decline` | 3 |
| `GuildLib.GuildResult_InviteAccepted` | 3 |
| `GuildLib.GuildResult_YouCreated` | 3 |
| `GuildLib.GuildResult_YouJoined` | 3 |
| `GuildLib.Accept` | 2 |

### housinglib

| API | Addons |
| --- | ---: |
| `HousingLib.IsHousingWorld` | 26 |
| `HousingLib.GetResidence` | 15 |
| `HousingLib.IsOnMyResidence` | 15 |
| `HousingLib.GetPlot` | 14 |
| `HousingLib.GetNeighborList` | 13 |
| `HousingLib.RequestTakeMeHome` | 12 |
| `HousingLib.IsResidenceOwner` | 11 |
| `HousingLib.VisitNeighborResidence` | 9 |
| `HousingLib.GetDecorCatalogList` | 7 |
| `HousingLib.GetRandomResidenceList` | 6 |
| `HousingLib.RequestRandomResidenceList` | 6 |
| `HousingLib.RequestVisitPlayer` | 6 |
| `HousingLib.GetPlotCount` | 5 |
| `HousingLib.GetPlugItem` | 5 |
| `HousingLib.NeighborInviteByName` | 5 |

### iccommlib

| API | Addons |
| --- | ---: |
| `ICCommLib.JoinChannel` | 76 |
| `ICCommLib.CodeEnumICCommChannelType` | 45 |
| `ICCommLib.CodeEnumICCommMessageResult` | 9 |
| `ICCommLib.CodeEnumICCommJoinResult` | 8 |
| `ICCommLib.GetUploadCapacityByType` | 4 |
| `ICCommLib.GetDownloadCapacityByType` | 2 |

### mailsystemlib

| API | Addons |
| --- | ---: |
| `MailSystemLib.GetInbox` | 10 |
| `MailSystemLib.MailDeliverySpeed_Instant` | 10 |
| `MailSystemLib.DeleteMultipleMessages` | 3 |
| `MailSystemLib.GetItemFromInventoryId` | 3 |
| `MailSystemLib.EmailType_Character` | 2 |
| `MailSystemLib.EmailType_CommodityAuction` | 2 |
| `MailSystemLib.AtMailbox` | 1 |
| `MailSystemLib.EmailType_Creature` | 1 |
| `MailSystemLib.EmailType_GMMail` | 1 |
| `MailSystemLib.EmailType_ItemAuction` | 1 |
| `MailSystemLib.GetAttachmentMaxCount` | 1 |
| `MailSystemLib.GetMessageCharacterLimit` | 1 |
| `MailSystemLib.GetNameCharacterLimit` | 1 |
| `MailSystemLib.GetRealmCharacterLimit` | 1 |
| `MailSystemLib.GetSendCost` | 1 |

### marketplacelib

| API | Addons |
| --- | ---: |
| `MarketplaceLib.RequestCommodityInfo` | 8 |
| `MarketplaceLib.kfCommodityBuyOrderTaxMultiplier` | 5 |
| `MarketplaceLib.AuctionSort` | 4 |
| `MarketplaceLib.GetCommodityCategories` | 4 |
| `MarketplaceLib.GetCommodityFamilies` | 4 |
| `MarketplaceLib.GetCommodityTypes` | 4 |
| `MarketplaceLib.kAuctionSearchPageSize` | 4 |
| `MarketplaceLib.kCommodityAuctionRake` | 4 |
| `MarketplaceLib.knCommodityBuyOrderTaxMinimum` | 4 |
| `MarketplaceLib.RequestOwnedCommodityOrders` | 4 |
| `MarketplaceLib.GetCommodityItems` | 3 |
| `MarketplaceLib.kMaxCommodityOrder` | 3 |
| `MarketplaceLib.RequestItemAuctionsByItems` | 3 |
| `MarketplaceLib.SearchCommodityItems` | 3 |
| `MarketplaceLib.AuctionPostResult` | 2 |

### matchinggamelib

| API | Addons |
| --- | ---: |
| `MatchingGameLib.GetQueueEntry` | 7 |
| `MatchingGameLib.IsFinished` | 5 |
| `MatchingGameLib.IsInPvpGame` | 5 |
| `MatchingGameLib.GetPvpMatchState` | 4 |
| `MatchingGameLib.IsInGameInstance` | 4 |
| `MatchingGameLib.LeaveGame` | 3 |
| `MatchingGameLib.ConfirmRole` | 2 |
| `MatchingGameLib.DeclineRoleCheck` | 2 |
| `MatchingGameLib.GetMatchingGameType` | 2 |
| `MatchingGameLib.IsLookingForReplacements` | 2 |
| `MatchingGameLib.IsPendingGame` | 2 |
| `MatchingGameLib.MatchType` | 2 |
| `MatchingGameLib.CanLookForReplacements` | 1 |
| `MatchingGameLib.CanVoteSurrender` | 1 |
| `MatchingGameLib.CastVoteKick` | 1 |

### matchmakinglib

| API | Addons |
| --- | ---: |
| `MatchMakingLib.Roles` | 6 |
| `MatchMakingLib.MatchType` | 5 |
| `MatchMakingLib.Queue` | 5 |
| `MatchMakingLib.QueueAsGroup` | 5 |
| `MatchMakingLib.GetMatchMakingEntries` | 4 |
| `MatchMakingLib.IsQueuedForMatching` | 4 |
| `MatchMakingLib.GetEligibleRoles` | 3 |
| `MatchMakingLib.GetQueuedEntries` | 3 |
| `MatchMakingLib.IsQueuedAsGroupForMatching` | 3 |
| `MatchMakingLib.MatchQueueResult` | 3 |
| `MatchMakingLib.GetMatchQueueResultString` | 2 |
| `MatchMakingLib.GetPvpRatingByType` | 2 |
| `MatchMakingLib.LeaveAllQueues` | 2 |
| `MatchMakingLib.RatingType` | 2 |
| `MatchMakingLib.CanLeaveQueueAsGroup` | 1 |
