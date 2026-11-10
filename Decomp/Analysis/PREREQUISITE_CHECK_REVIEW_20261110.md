# Prerequisite check review against the native evaluator (2026-11-10)

Evidence tier: **native body read** for every change listed in section 2 (evaluator case in `PrerequisiteManager_EvaluateTypeSlot` `1404a2100` plus the handler body where one is called), with retail row shapes from
`wildstar_client.prerequisite`. Source tests only. No retail parity claim. Follows `PREREQUISITE_HANDLER_TABLE_AUDIT_20261110.md`.

## 1. Method
One dossier per type (retail row shape per comparison, evaluator case, native handler body, server check source) was generated for the 111 handled types whose check class did not name its native code.
Four were compared by hand (11, 15, 28, 39); the other 107 were compared in six parallel read-only passes that reported structure only (which slot word is read, which comparisons are honoured, which unit is
tested, null handling). Every reported difference acted on below was then re-read against the decompile before the server was changed. Reports that were not re-read are listed as such.

In the evaluator `param_3[1]` is the comparison, `param_3[2]` ObjectId and `param_3[3]` Value; `param_2[0]` is the primary unit and `param_2[1]` a second unit. Entity type (`+0x80`) `0x14` is Player (20) and
`0x17` is Ghost (23).

## 2. Server checks changed

| Type | Check | Native rule | What was wrong | Retail slots affected |
|---:|---|---|---|---|
| 15 | UnderSpell | handler receives Value only | fell back to ObjectId when Value was 0 | 1 |
| 105 | SpellEffectTypeOnUnit | inline; compares with ObjectId | read Value | 7 (previous commit) |
| 176 | ChallengeObject | flag for Equal, negation for NotEqual, Value never read; player/ghost primary with no (or a player/ghost) second unit answers NotEqual only | compared a 0/1 flag with Value | 24 of 25 inverted |
| 90 | MountVehicleType | handler receives unit and comparison only; flag is a link at unit `+0xf00` | compared a platform type number with Value | 9 of 9 inverted |
| 267 | GroupIsRaid | handler receives unit and comparison only; bit 1 of the group context flags | compared the flag with Value | 2 of 2 inverted |
| 85 | Taxi | handler receives unit, comparison and ObjectId; Value never read | compared the flag with Value | 14 of 17 inverted |
| 266 | PetOrEsperPetEntity | handler `14049cae0` (slot `+0xc8` read with `DumpPointerSlots.java`) receives unit and comparison only; entity type `0x18` or `0x19` | compared the flag with Value | 4 of 4 inverted |
| 221 | ActionSetSpell | handler `14049d6d0`: local player only, 12 slots of the set selected by the index at `DAT_140c65898 + 0x6dec`, flag / negation, Value never read | compared the flag with Value and searched every set | 59 of 59 wrong |
| 275 | Unknown275 (tradeskill known item) | handler `1404a17e0`: primary unit must be Player or Ghost, known-item lookup of ObjectId, flag / negation | required a non-player target and compared the flag with Value | 281 of 281 inverted |
| 241 | HousingResidenceLoaded | primary unit must be Player or Ghost; second unit not passed | required a non-player target | 6 |
| 108 | PositionalRequirement | no second unit, ObjectId 0 or no entry: NotEqual true, everything else false | returned false for NotEqual as well | the 14 NotEqual slots in those states |
| 91 | PathLevel | handler receives unit, comparison and Value only | used ObjectId as a path selector | 1 |
| 139 | ItemMicrochip | handler `1404a0e10`: Value mapped by `Prerequisite_MapMicrochipIdToBit` (ids 7 to 13) and bit-tested; ObjectId never read | mapped ObjectId; treated Value as an installed-rune count | 2 |
| 288 | PurchasedTitle | comparisons other than Equal / NotEqual are false | returned true | 0 (latent) |
| 4 | Faction | handler `14049c620`: Equal / NotEqual only, null unit false | honoured all six | 0 (latent) |
| 109 | WorldRequirement | Equal / NotEqual against ObjectId only | honoured all six | 0 (latent) |
| 93 | Spell4EffectCategoryOnUnit | inline: a dword of each active spell (`[[[node+0x38]+0x138]+0x70]+0xf8`) equals ObjectId; flag / negation | read Value as an effect-group id | 5 of 5 (all asked for id 0) |
| 116 | Stealth | inline: `1 < *(int *)(unit + 0x54)`; flag / negation; neither word read | no check existed (unhandled types answer false) | 32 slots, 327 spell references |
| 103 | SpellCooldownNodeOnTarget | `14046a190`: an effect node with id ObjectId whose virtual `+0x20` equals the second unit id; no second unit answers NotEqual only | cooldown-node lookup by Value on the player | 36 slots, 46 spell references |
| 245 | Unknown245 | `1404a1790`: Player/Ghost gate, flag from a lookup of ObjectId, flag / negation | tradeskill tier compared with Value behind the NPC-target gate | 249 slots; every one of the 220 "Learn Dye" spells failed its cast check |

What each change does **not** establish: the meaning of the flag itself. For types 176, 90, 85, 275 and 221 the server keeps its existing notion of the flag (challenge activated, has a platform, on a taxi, knows a
schematic for the item, spell on the active action set); only the operand and comparison rule were corrected. Type 85 does not apply the native ObjectId filter (an id held by the taxi entity that the server
does not model). Type 90 does not model the redirect through a type 3/7/9 link. Type 108 still ignores the entry flag that swaps the two units and wrapped angle ranges.

### The Player/Ghost gate was read backwards
Several handlers start with `unit == null || (type != 0x14 && type != 0x17) -> false`. The server modelled this as `PrerequisiteNpcTargetContext.RequiresNpcTarget` ("needs an NPC target that is not the
player"). `0x14` and `0x17` are Player and Ghost, so the gate requires the **primary unit to be a player or ghost** and is satisfied whenever the server evaluates a player. Types 241 and 275 were corrected.
`RequiresNpcTarget` is still used by types 97, 98, 99, 168 (client stubs), 166/167 (client stubs), 186 and the shared tradeskill helpers for types 173/174/245; those were not changed (see section 3).

### Types 93 and 116: identified from the client and Jabbithole data (added later the same day)
* **93.** All 5 retail slots use ObjectId 36 or 38, which are exactly `SpellClass.BuffDispellable` (0x24) and `DebuffDispellable` (0x26), and the only spells gated by those rows are the tiers of Medic
  "Antidote" (Spell4 52480 and up: `SpellDispel` plus proxies whose caster-apply prerequisite is the type 93 row). The check now asks whether a spell with tracked state on the unit has that
  `Spell4Base.SpellClass`. The native dword itself was not identified. Status: Implemented, inferred.
* **116.** 22 of 32 slots carry Value 28455, which the client never reads and which is Spell4 "Stalker - Optic Camouflage" (first effect: Stealth); the rows are paired with UnderSpell checks for
  "Stalker - Combat Stealth". With the client error text for the type ("Stealth property requirement not met") the flag is read as the stealth state. What the field at `+0x54` holds was not read natively.
  Status: Implemented, inferred.

### Types 103 and 245, and the LearnDyeColor effect (added later the same day)
* **103.** 23 of the 46 spell references are target-apply prerequisites of the form (NotEqual, the same effect row), for example Spell4 29061 "Landmine Proximity" gating its own CCStateSet row 50511. Evaluated
  with the target as the unit and the caster as the second unit this reads "do not apply while my own copy is on the target", so virtual `+0x20` is read as the unit that applied the effect. The server does
  not track effect rows applied by other units; the check approximates the row by the spell that owns it (tracked state of that spell registered to the second unit). Status: Implemented, inferred.
* **245.** All 220 spells gated by type 245 rows are "Learn Dye - ..." spells with (NotEqual, ObjectId) as caster-cast prerequisite, and in 219 of them ObjectId equals DataBits00 of the spell's
  LearnDyeColor effect. The check now asks whether that dye colour is unlocked. The native lookup `1403c16e0` is labelled `TradeSkill_CheckItemTradeskillRequirement`; that label is contradicted by this data
  and the body was not re-read. Status: Implemented, inferred.
* **LearnDyeColor (effect type 110).** DataBits00 is the dye colour (`GenericUnlockEntry.UnlockObject`), not a `GenericUnlockEntry` id. Matching by the entry icon names, "Learn Dye - Red_Light1" (DataBits00 2)
  is `Dye_Red_Light1` only when 2 is read as UnlockObject (entry id 2 is `Dye_Red_Dark2_Dull`); 222 of 223 retail rows resolve to a Dye entry by colour against 161 by entry id. The handler unlocked the entry
  with the same id and therefore taught a different dye. It now resolves the entry by colour. Evidence tier: table names; no retail parity claim.

### Types 170 and 190 (added later the same day)
* **170.** Native `14049c880`: false without a unit; the flag is "the object at local-player global `+0x7188` exists and Value is 0 or that object's id (virtual `+0x68`)"; Equal / NotEqual only; ObjectId
  is not read and no table is consulted. All 14 retail rows gate creature activate spells or animation states of transport NPCs whose spell is a "Transport to ... Yes/No CSI" spell, with NotEqual, and
  Value equals that spell's own `Spell4Base.clientSideInteractionId` in 5 of the 7 spells (62688 -> 1029, 62734 -> 1031, 63342 -> 1050, 87494 -> 1363, 87510 -> 1367). The object is therefore read as the
  active client-side interaction: "offer the transport unless its confirmation is already open". The server models that as a pending spell waiting on a client-side interaction response
  (`UnitEntity.HasActiveClientSideInteraction`). The previous check compared a GameFormula row with ObjectId. Not identified natively: the object at `+0x7188`; the 2 spells whose Value differs from
  their own interaction id were not explained. Status: Implemented, inferred.
  Later the same day the object was identified natively: `Lua_CSIsLib_GetActiveCSI` (`140755000`), `Lua_CSIsLib_IsCSIRunning`, `CSIKey_HandlePress` and `ProgressClickWindow_SetActiveObject` all read
  or write the same slot (`+0x7188`), so it is the active client-side interaction. Only the server-side model of "active" remains inferred.
* **190.** Native `1404a14d0`: Player or Ghost only; `expected` is 0 without a PetFlair row for ObjectId, 1 when the row's second unlock bit index (row `+8`) is 0, otherwise 2; `actual` is a per-player
  count for the same flair (`FUN_1403a9c40`, body not read); Equal is `actual == expected`, NotEqual the negation; Value is not read. All 298 retail ObjectIds are PetFlair ids, all slots are NotEqual,
  and they are referenced by 206 AccountItem rows and 36 ItemSpecial rows, none by spells. Read as "the flair is fully unlocked"; the check now asks `PetCustomisationManager.HasFlair(ObjectId)` where
  it asked for a hoverboard customisation with that id. Not modelled: a flair with two unlock bits of which only one is set (natively not equal to the expected 2, so "not unlocked"; the server has one
  unlock state per flair). Status: Implemented, inferred.

### Types 194 / 195 (mount and hoverboard usage) and 182 / 186 (housing) (added later the same day)
* **194 / 195.** Both call `14049ced0`. For the local player it takes the unit's current zone id (unit `+0x17e8`; the same field is read by `Lua_Unit_IsVisibleOnCurrentZoneMinimap` and the
  current-zone-map Lua function), looks the zone up in the tree at global `+0x7160` (`14043ed60`) and walks the node and its parents reading the row flags at `+0x1c`, which is
  `WorldZone.flags`. Bit `0x10` anywhere on the chain makes the flag false. Bit `0x100` is an allow bit: it is required only when the object at unit `+0x16f0` exists and its virtual `+0x78`
  returns 0. Types 197 (glider, `14049cfb0`) and 198 (spaceship, `14049d090`) are the same body with bits `0x40` / `0x200` and `0x80` / `0x400`, and `1403c9500` selects the same three bit pairs
  by a vehicle kind. Table: 281 of 3,436 WorldZone rows carry `0x10` (250 carry `0x40`, 238 `0x80`), 6 carry `0x100`; the named ones are interior rooms ("Queen's Den", "Crimson Legion Control
  Room"). The 4 + 4 slots sit, through nested rows 35184, 35364, 36099 and 37820, in the cast prerequisite of 224 mount summon spells. Both checks returned true unconditionally; they now
  walk the player's zone chain for `0x10`. Not modelled: the `+0x16f0` condition and with it the allow bit (the object is listed in older notes as a bounding-volume object; what its virtual
  `+0x78` reports was not read). Types 197 and 198 have no retail rows and no server check. Status: Implemented from the native body and the table; the meaning "mounts not allowed here" is
  inferred from the failure text and the gated spells.
* **182 / 186.** `1404a1070` and `1404a10e0` both require a Player or Ghost unit and a second unit, and resolve a residence with `1405b15c0(housing global, secondUnit+0x172c)`: the loaded
  residence that has a plot whose `HousingPlotInfo` row has that world socket id (row `+4`). Without one both are false for every comparison. 182 then compares residence `+0x20` with unit
  `+0x1a8` through the general comparison (read from the disassembly; the decompiler drops the arguments); 186 looks the residence key (`+8`, `+0x20`) up in the local player's neighbour list.
  Read as "the player owns / is a neighbour of the residence this housing unit stands on". Row 17536 ("182 or 186") gates 42 "Plant Seeds" spells and housing prop creatures. Both server checks
  tested the player's own residence wherever the player was, so any house owner passed on another player's plot, and 186 asked whether that own residence had any neighbour at all. They now
  resolve the residence from the second unit (`IResidenceMapInstance.GetResidenceForEntity`). Server approximations: a unit without a socket id resolves to the residence whose plug it is or to
  the only residence on the map (natively it matches nothing); a spell cast without a target on a community map therefore resolves nothing. Not read natively: that residence `+0x20` and unit
  `+0x1a8` are owner and character ids. Status: Implemented, inferred.

### Types 178 and 104 (added later the same day)
* **178.** Live case `0xb2` dispatches vtable `+0x140` (`14049d2c0`), not the body at `1404a1890` the server followed (that one is type 291 only). ObjectId is an `ItemSlot` id (`14020e7a0`
  reads the ItemSlot table); the index of the lowest set bit of the row's `equippedSlotFlags` selects an entry of the local player's indexed storage with selector 0, and the flag is "the entry
  exists". No row or no bit below 31 answers NotEqual only. Value is not read. Retail: ObjectId 46 = "ArmorGadget" (flags `0x800`, equipped index 11 = Gadget) is the unlock prerequisite of
  ActionSlotPrereq slot index 8, the gadget action slot; ObjectId 43 = "ArmorShields" (`0x8000`, index 15 = Shields). An older enum comment had rejected this "Equipped" reading on the strength
  of the disproven handler table. New check `PrerequisiteCheckItemSlotEquipped`; the progress-track body stays on type 291. Status: Implemented from the native body; selector 0 = equipped bag
  is inferred from the bit to equipped-index match.
* **104.** `14046a210(unit, ObjectId)`: the decompiler drops the argument of the spell service call (virtual `+0x40`, `140561e00`), which looks ObjectId up in the map at service `+0x740` and
  returns a set of effect ids; the flag is "an active effect of the unit is in the set". `Spell4Manager_BuildRuntimeData` (`14055e180`) fills that map from each Spell4Effects row's
  Spell4EffectGroupList, one entry per group id. ObjectId is a spell effect group. Retail row 23816 ("owner or neighbour, and NotEqual group 12") is the animation-state prerequisite of the
  "Eldan Statue" and "Warhorn" housing plugs, and group 12 holds exactly the 17 effects of the "Housing Statue Buff" and "Housing Warhorn Buff" spells. The server asked whether the player knew
  a spell with cooldown id Value (always 0); it now uses the existing effect-group helper. Rows 5634 (group 4, on an effect whose spell no longer exists) and 29932 (group 14, empty) are dead.
  Status: Implemented from the native body and the table builder.

### Types 175 and 289 (added later the same day)
* **175.** `14049f190` walks the list at local-player global `+0x1698` / `+0x16a0`. An instruction scan shows that list is the known-tradeskill list (`Lua_Crafting_GetKnownTradeskills`,
  `Lua_Crafting_GetTradeskillTalents`, `Tradeskill_SendClientTradeskillPickTalent` and a dozen more crafting functions read it). For every entry with `+0xc` non-zero the ten ids at `+0x18` are
  searched for ObjectId; Value is not read. The entry layout (tradeskill id, active flag, ten ids) is the server's own tradeskill state with its ten talent tier picks, and the one retail
  ObjectId, 153, is a TradeskillBonus row that is one of the three choices (152, 153, 154) of TradeskillTalentTier 42. Read as "tradeskill talent ObjectId is picked". The server read ObjectId
  as a tradeskill id and compared its tier with Value. New `IPlayer.HasTradeskillTalent`. The retail row has no referencing row. Status: Implemented from the native body; the meaning of the
  ten ids is inferred. The labels `Prerequisite_CheckJump`, `_CheckTaxi`, `_CheckMoving` and `_CheckPet` on the neighbouring bodies that read the same list are stale names.
* **289.** `14049f0c0` reads bit 0 of `*(DAT_140c635f0 + 0x16e8) + 4`. `WorldSocket_InitPersistentRuntimeNodes` (`14000b690`) fills `+0x16e8` with the RealmDataCenter row for the
  `realmDataCenterId` launch argument (`14022e1c0`), and `+4` is that row's `flags`; the same pointer supplies the redirect and store URLs. Of the ten retail rows only
  `auth.ptr.wildstar-online.com` (id 12) and one internal row (id 13) carry bit 0. Read as "public test realm". The three slots offer quests 10433 and 10440 either after their predecessor or,
  on such a realm, without it. The server compared "is mounted" with Value; it now returns the flag as false. The label `Prerequisite_CheckMount` was wrong for this body. Status: Implemented
  from the native body; the meaning of the bit is inferred from the rows that carry it.

### Type 94 (added later the same day)
`14049f5b0(unit, comparison, ObjectId, Value, secondUnit)`. The creature is the unit when it is not a Player or Ghost, else the second unit when that is not a Player or Ghost; with no
creature, or no quest definition for ObjectId, only NotEqual is met. Otherwise the flag is `140552f50` (the quest is in the active quest map) and `140551730(quest, Value, creature+0x35f8)`:
the objective at index Value exists, `1405517a0` accepts its type (10, 13, 14, 18; or 27 and 28 when `SettlerMission_LookupById` gives mode 1 or 2), the slot is below 32, and that bit is
set in the objective word (virtual `+0x40`).

* `+0x35f8` is written in the unit-created path (`140456960`, two stores) and reset to -1 in the unit constructor; it is the slot type 107 already reads as the entity's QuestChecklistIdx.
* Table test of the reading: 771 of 781 slots carry a Quest2 id; resolving the objective at index Value gives type 14 (ActivateTargetGroupChecklist) on 564 slots and type 10
  (ScriptedTargetGroupChecklist) on 56, together 620 of the 722 that resolve. The rest point at types the native gate rejects (12 on 37 slots, 23, 32, 25, 5, 8, 3, 17, 11, 22, 2) or accepts
  conditionally (18 on 9, 28 on 7).
* The server stores checklist progress as bits indexed by QuestChecklistIdx for types 14 and 10 (`QuestObjective.ObjectiveUpdate`, written from `SpellEffectHandler` and
  `InteractionObjectiveUpdater`), so the check reads the same bit the producers write.

Read as "this creature is already ticked off in the checklist objective". The server applied the type 68 rule (objective complete; a Completed quest counts), which answers the same for
every creature of a checklist. Not modelled: types 13, 18, 27 and 28, which the native gate accepts but for which the server keeps a count (16 slots). That the native gate treats 13
(SpellSuccess2) and 18 (TalkToTargetGroup) as checklist objectives contradicts `QuestObjective.IsChecklist`; recorded, not changed. Status: Implemented from the native bodies; meaning
inferred from the objective types.

## 3. Differences found and not changed

| Type | Check | Finding | Why not changed |
|---:|---|---|---|
| 73 | Vital | re-read: native switch `140466520` reads Breath (id 2, virtual `+0x58`), Absorption (`0x15`, virtual `+0x88`), three public-event resources (`0x16` to `0x18`) and Resource8 to 10 (`0x1b` to `0x1d`), and for any other id returns the class resource slot of the local player or 0, then always compares. The server returns false without comparing for ids it cannot resolve. Retail slots hit: id 0 (1 slot, `>= 80`), id 2 (1 slot, `<= 0`), id 21 (1 slot, `> 0`) | re-checked: blocked on server features, not on evidence. Comparing 0 for an unresolved id would make the retail Breath row (`<= 0`) permanently true, so the early false stays until Breath exists; the class-resource fallback needs the class object's resource index (virtual `+0x60`), which was not read |
| 292 | PrimalMatrixNode | native: local player only, `PrimalMatrix_LookupAllocatedNode(1, ObjectId)` compared with Value; server always false; 24 slots | understood; blocked on a server feature, not on evidence: the player has no primal matrix allocation state to query |
| 148 | InfrastructureState | lookup miss: native answers NotEqual only; server substitutes state 0 | server API cannot distinguish a miss from state 0 |
| 88 | Pet | native tests whether the primary unit id is in a list on the second unit (`+0x2b8` / `+0x2c0`); server tests the vanity pet against Value; 1 slot (row 15631), no referencing row found. An instruction scan for `+0x2b8` returns 309 hits and no labelled unit function, so the list stays unidentified | list not identified; nothing references the row |
| 41 | ZoneExplored | native compares an untruncated float ratio; server a truncated integer. Re-checked: none of the 14 rows using type 41 is referenced by any table. 13 are early rows (ids 716 to 1250) with ObjectId 0 and a zone-like id in Value under Equal, which the current native body cannot satisfy; only row 7964 (zone 1, `>= 25`) has the current shape | no live reference |
| 179, 80, 172, 218, 246, 78, 140, 81, 173, 174 | | latent or zero-row differences | no retail slot affected, or reported and not re-read |

### Client stubs with server-invented logic
For these types the evaluator calls the shared stub `140001ba0`, so the client contributes no rule. The server checks implement behaviour that has no native basis; whether it is right can only come from a
server-side or retail witness: 27, 29, 46, 76, 96, 97, 98, 99, 100, 101, 166, 167, 168, 169, 180. For several of them the evaluator does not even pass the words the server reads (for example type 46 reads
ObjectId while only Value is passed; types 97 to 101 read ObjectId and Value while only the two units are passed).

### Which unit `Meets` tests: traced, not a spell defect
Twenty-one check classes evaluate `parameters.Target ?? player` in `Meets`, while the native case tests the primary unit (`param_2[0]`). Traced from the call sites:

* The spell code (`Spell.CheckPrerequisites`, runner override, effect apply and persistence checks) always calls `PrerequisiteManager.MeetsUnit(subject, id, parameters)` with the tested unit as the subject and
  the other unit as `parameters.Target`, which is the native (primary, second) arrangement.
* All 21 of those classes override `MeetsUnit` and test the subject there, so on the spell path they test the primary unit.
* `Meets(player, ..., parameters)` with a Target set is reached from one place only: `ClientActivateUnitCastHandler.MeetsActivateSpellPrerequisite`, where the Target is the activated entity. There the 21 classes
  test the activated entity instead of the player. Which unit the client treats as primary for activation-spell prerequisites was not read, so that path is unchanged.
* A check class with no `MeetsUnit` override answers false for any non-player subject (the interface default). That fail-closed gap is a coverage limit, not a wrong answer for players.

## 4. Types reviewed and found consistent
1, 7, 8, 9, 10, 11, 16, 22, 26, 28, 35, 39, 51, 56, 68, 71, 86, 95, 117, 118, 136, 177, 204, 214, 215, 244, 250, 270, 286, 287. Some of these carry a "not visible in the decompile" caveat in the pass
reports (elided arguments of the comparison helper); none showed a structural difference.

Not judged because the handler body was not in the dossier at the time: 2, 3, 23, 24, 25, 33, 34, 36, 55, 64, 79, 115, 171, 196, 239, 269, 293. Their vtable slots held pointers to code where analysis had
not created a function; `DumpPointerSlots.java` read the 19 slots and labels were added, so the bodies are now reachable for a later pass.

## 5. Map completion
`scripts/prerequisite_evaluator_case_map.py` now parses decimal vtable offsets (the decompiler prints `+ 400` for `+0x190`). With the 16 new functions the generated map resolves every one of the 213
vtable cases to a function; 40 are the shared stub.

## 6. Verification
`dotnet test` filter `Game.Tests.Entity.PrerequisiteCheckTests | Game.Tests.Prerequisite | ItemRune`: 1,318 passed. Ten existing tests encoded the superseded behaviour (GroupIsRaid, ChallengeObject, Unknown275,
ItemMicrochip) and were rewritten from the native rule and retail rows; they failed before the test changes and pass after.
