# Remaining kirmmin/latest Deltas

Last updated: 2026-05-17

Baseline checked:
- Current repo after the loot granted/explosion/account-item-delete pass.
- Old branch clone: `kirmmin/NexusForever` `latest`, commit `d9b4c0a`, local path `C:\Users\Bankai\AppData\Local\Temp\nexusforever-kirmmin-latest`.

This is a parking lot for deltas still worth revisiting later. Do not blindly copy the old branch: it is useful because it had several working flows, but packet fields and current decompile labels should remain the authority.

## Already Landed In Current Pass

- `LootItem.Granted` is now in the loot item packet model.
- Loot bags now deliver immediately and send granted/explosion `ServerLootNotify` entries.
- Immediate account-currency loot, including random Omnibit kill rewards, now uses the granted shower path.
- `ServerAccountItemDelete = 0x097C` is present and account item removal sends the explicit delete packet.
- Current group loot has basic FreeForAll, RoundRobin, NeedBeforeGreed, and Master distribution, which the old branch did not really solve.

## Priority Deltas

### P1: Verify the full `LootItem` packet layout

Current packet order:

`LootUnitId`, `Type`, `ItemId`, `Amount`, `CanLoot`, `RequiresRoll`, `OnlyMasterLootable`, `Explosion`, `Granted`, `RollTime`, `RandomCircuitData`, `RandomGlyphData`, `ItemQuality2Id`, `MasterList`.

Old branch packet order:

`UniqueId`, `Type`, `StaticId`, `Amount`, `CanLoot`, `NeedsRoll`, `Explosion`, `Granted`, `RollTime`, `RandomCircuitData`, `RandomGlyphData`, `Unknown2`, `MasterList`.

Evidence:
- `function_labels.csv` labels `Lua_GameLib_BuildLootRollEntry`, `Lua_GameLib_GetLootRolls`, `Lua_GameLib_GetMasterLoot`, `Lua_GameLib_AssignMasterLoot`, and client senders for `ClientLootItem`, `ClientLootRollAction`, and `ClientLootAssignMaster`.
- `strings.csv` has loot/UI fields `bCanLoot`, `eLootItemType`, `nTimeLeft`, `bGranted`, `bIsMaster`, `tLooters`, `tLootersOutOfRange`, `itemDrop`, and `nLootId`.

Remaining work:
- Confirm whether current `OnlyMasterLootable` is really a packet field in the `LootItem` payload, or whether `bIsMaster` is derived client-side from `MasterList`/roll state.
- Confirm the bit order around `RequiresRoll`, `OnlyMasterLootable`, `Explosion`, and `Granted`; this is the highest-risk client-visible layout delta.
- Confirm `ServerLootNotify.ParentUnitId`: current code sends `OwnerUnitId`; old branch called this `Unknown0`.
- If validation fails, adjust packet order before changing group/master semantics.

Suggested validation:
- Capture a client session where loot bag, solo corpse loot, need/greed loot, and master loot all render.
- If one mode breaks, inspect the first differing boolean field before touching gameplay logic.

### P1: Account item cooldowns and operation result

Current status:
- Current code has `ServerAccountItemCooldownSet` with fields `AccountItemCooldownGroup` and `CooldownInSeconds`.
- `AccountInventoryManager.SendCooldowns()` is empty.
- Current claim path uses `ClientAccountItemTake` opcode `0x0839`, which is the same opcode old branch called `ClientAccountItemBind`.
- Current failure path sends generic errors rather than old branch's `ServerAccountOperationResult`.

Old branch behavior:
- Tracks `AccountItemCooldown` records in auth DB.
- Initializes missing cooldown groups from `AccountItemCooldownGroup.tbl`.
- On item claim/bind, checks `AccountItemEntry.AccountItemCooldownGroupId`.
- Sends `ServerAccountItemCooldownSet` after triggering a cooldown.
- Sends `ServerAccountOperationResult` with `Operation=TakeItem` and result values such as `Ok`, `Cooldown`, `Prereq`, and `MaxEntitlementCount`.

Evidence:
- `ServerAccountItemCooldownSet_ReadPayload` at `14007a040` reads opcode `0x0974` as cooldown group plus cooldown seconds.
- Old branch `ServerAccountOperationResult` is opcode `0x0970`, payload `AccountOperation` as 32 bits then `AccountOperationResult` as 32 bits.

Remaining work:
- Add a current account cooldown model/manager, or adapt the current auth model if already present.
- Implement cooldown load/save and nonzero cooldown sends.
- Add `ServerAccountOperationResult = 0x0970` only after confirming the current client still expects the old field shape.
- Decide whether current `GenericError` responses should remain in addition to operation result for unsupported paths.

### P1: Group loot edge semantics

Current status:
- Current code has basic group recipient resolution and a `GroupStateManager`.
- Round robin stores only the last `GroupIndex`.
- Need/greed roll resolution sends `ServerLootRoll` and `ServerLootWinner`.
- Master loot authority is currently `group.Leader` if eligible, otherwise first eligible player.

Packet/client evidence:
- Client Lua exposes roll/master state through `GameLib.GetLootRolls`, `GameLib.GetMasterLoot`, `GameLib.RollOnLoot`, `GameLib.PassOnLoot`, and `GameLib.AssignMasterLoot`.
- The Lua loot table builder references `nTimeLeft`, `bIsMaster`, `tLooters`, and `tLootersOutOfRange`.
- `ClientLootRollAction` shape is `owner unit id`, `loot id`, `action`.
- `ClientLootAssignMaster` shape is `owner unit id`, `loot id`, `assignee identity`.

Remaining work:
- Confirm exact master authority: leader, explicitly assigned master looter, fallback behavior if the leader is out of range/offline, and behavior after leadership changes while loot is open.
- Separate eligible looters from visible/master candidate looters so `tLootersOutOfRange` can be represented correctly if the packet supports it.
- Round robin should be stable under member churn. Current `GroupIndex` cursor can repeat or skip in awkward reorder/leave cases; consider tracking last winner identity plus group epoch/snapshot.
- Roll finalization currently marks loot delivered-without-winner if the winner is offline; retail likely keeps state, retries, or mails depending on item type.
- Roll UI timing needs live validation. Current `RollTime` is seconds remaining at notify time; client labels suggest `nTimeLeft`, but the exact timer source should be confirmed.
- `HarvestLootRule` is present in group packets but not used by loot generation. Harvest/resource loot still needs its own rule path.

### P2: Unused loot packets

Current packet models exist but are not wired:
- `ServerLootNotification`: notifies other players when someone loots an item.
- `ServerLootCanLoot`: updates the client-side `CanLoot` flag for a loot entry.
- `ServerLootBindOnPickup`: triggers the bind-on-pickup confirmation.

Remaining work:
- Map these from decompile/sniff before use. They may be needed for retail-like group loot feedback, BoP prompts, and delayed can-loot updates after roll/master resolution.
- `ChatFormatLoot` says its `LootUnitId` must match `ServerLootNotification`, so group loot chat and notification path should be validated together.

### P2: Account-currency grant model cleanup

Current status:
- `ServerAccountCurrencyGrant` is the used packet and matches the old branch model: `AccountCurrency`, `Unknown0`, `Unknown1`.
- `ServerGrantAccountCurrency` also exists with the same opcode but a different payload shape and appears unused.

Remaining work:
- Confirm which class is actually registered at runtime when two message models share `ServerAccountCurrencyGrant`.
- Remove or quarantine the unused duplicate if it is not valid for this client build.
- Old branch also grants CosmicReward when spending Omnibits/Protobucks. This is gameplay policy, not packet-required; port only if desired.

### P2: Random Omnibit shower tuning

Current status:
- Random Omnibit kill rewards are implemented with local constants and the granted shower packet path.

Remaining work:
- Find retail-ish chance/amount formulas if available in client tables, server data, or sniffs.
- Confirm whether Omnibits should be suppressed for tutorial maps, PvP, pets, summons, or non-XP creatures.
- Confirm whether shower entries should sum exactly to the grant amount. Current code preserves the exact total; old branch did not.

### P3: Quest/content script ports from old branch

Old branch scripts still not ported:
- `Q10510_LearningToShop`
- `NorthernWilds/Q3486-EmpoweredTower`
- `NorthernWilds/Q3487-Shellshock`
- `NorthernWilds/Q3667-TheTower`
- `NorthernWilds/Q3673-ContactWithThayd`
- `CrimsonIsle/Q5573-PoweringDown`
- `CrimsonIsle/Q5584-VenomousIntent`
- `CrimsonIsle/Q5594-LastResistance`
- `CrimsonIsle/Q5604-TacticalDemolitions`
- `CrimsonIsle/Q8855-StasisInterrupted`

Current script support is better structured, but a few old script hooks need translation:
- Old `QuestScript.OnQuestStateChange(Player, Quest, QuestState)` maps to current owned `IQuestScript.OnQuestStateChange(newState, oldState)` plus `IOwnedScript<IQuest>`.
- Old `OnObjectiveUpdate(Player, Quest, QuestObjective)` maps to current `OnObjectiveUpdate(IQuestObjective)` plus owner access.
- Old creature hooks such as `OnActivateSuccess`, `OnEnterRange`, and `OnAddToMap` mostly exist in current interfaces.
- Old `OnDeathRewardGrant(WorldEntity me, WorldEntity killer)` does not have a direct current script hook with killer context; add a safe current hook before porting scripts that require killer/player attribution.

Recommended approach:
- Port the simple objective/range scripts first.
- Port cinematic-completion scripts only after confirming the corresponding cinematic classes and quest objective ids exist in current code.
- Treat these as content fixes, not packet fixes.

### P3: Loot data mapping completeness

Current status:
- WorldServer logs show loot loading from mapped loot tables and item loot.
- Direct imported creature rows are currently skipped when mapped loot groups already exist for the same creature.

Remaining work:
- Decide a merge/precedence strategy for old-table `loot_group` rows versus direct imported `creature_loot` rows.
- Validate probability scaling: old branch tables use percent-like probability in `LootGroup`/`LootItem`; current direct import clamps SQL chance from `0..1` then converts to percent.
- Use `Various SQL`, jabbithole, and MySQL client data to map condition types, multi-drop groups, item loot bags, quest-only loot, and level/content gating.
- Add a small data report after each import pass: creature count, item count, skipped rows, invalid Item2 ids, and rows skipped due to existing mapped groups.

## Suggested Next Pass Order

1. Validate `LootItem` boolean/field order with a live client test or sniff.
2. Implement account item cooldowns plus `ServerAccountOperationResult` if the packet shape is confirmed.
3. Tighten group loot semantics: master authority, round-robin churn, offline roll winners, and out-of-range candidate UI.
4. Wire `ServerLootNotification`, `ServerLootCanLoot`, and `ServerLootBindOnPickup` only after field confirmation.
5. Port low-risk quest scripts and add the missing killer-aware script hook if needed.
