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

What each change does **not** establish: the meaning of the flag itself. For types 176, 90, 85, 275 and 221 the server keeps its existing notion of the flag (challenge activated, has a platform, on a taxi, knows a
schematic for the item, spell on the active action set); only the operand and comparison rule were corrected. Type 85 does not apply the native ObjectId filter (an id held by the taxi entity that the server
does not model). Type 90 does not model the redirect through a type 3/7/9 link. Type 108 still ignores the entry flag that swaps the two units and wrapped angle ranges.

### The Player/Ghost gate was read backwards
Several handlers start with `unit == null || (type != 0x14 && type != 0x17) -> false`. The server modelled this as `PrerequisiteNpcTargetContext.RequiresNpcTarget` ("needs an NPC target that is not the
player"). `0x14` and `0x17` are Player and Ghost, so the gate requires the **primary unit to be a player or ghost** and is satisfied whenever the server evaluates a player. Types 241 and 275 were corrected.
`RequiresNpcTarget` is still used by types 97, 98, 99, 168 (client stubs), 166/167 (client stubs), 186 and the shared tradeskill helpers for types 173/174/245; those were not changed (see section 3).

## 3. Differences found and not changed

| Type | Check | Finding | Why not changed |
|---:|---|---|---|
| 245 | Unknown245 | handler `1404a1790`: Player/Ghost gate, `TradeSkill_CheckItemTradeskillRequirement(ObjectId)` flag / negation; server compares a tier rank with Value behind the NPC-target gate; 249 slots, all NotEqual with Value 0 | what the requirement function tests was not read; the server has no equivalent flag |
| 73 | Vital | native switch `140466520` handles vital ids the server rejects (2, 0x15, 0x16 to 0x18, 0x1b to 0x1d) and still compares 0 for unknown ids; server returns false | reported by a review pass, not yet re-read |
| 292 | PrimalMatrixNode | native: local player only, `PrimalMatrix_LookupAllocatedNode(1, ObjectId)` compared with Value; server always false; 24 slots | needs the server primal-matrix allocation surface; not yet done |
| 148 | InfrastructureState | lookup miss: native answers NotEqual only; server substitutes state 0 | server API cannot distinguish a miss from state 0 |
| 93 / 103 / 104 | | wrong slot word (ObjectId), see the audit note section 4.3 | compared field or set not identified |
| 170 | GameFormula | native compares the id of a live object at global `+0x7188` with Value (flag true when Value is 0); server compares a GameFormula row with ObjectId; 14 slots | the object is not identified; the server check has no native basis |
| 190 | PetFlair | native compares a per-player integer with an expected value of 0, 1 or 2 derived from a table row; server tests presence under one pet type; 298 slots | the two functions involved were not exported or read |
| 194 / 195 | MountUsage / HoverboardUsage | native computes a flag for the local player; server returns true unconditionally; 4 slots each | flag meaning not identified |
| 88 | Pet | native tests whether the primary unit id is in a list on the second unit; server tests the vanity pet against Value; 1 slot | list not identified |
| 178, 175, 182, 289 | | server compares a number with Value where the native handler never receives Value | native flag not identified |
| 94 | QuestObjective6 | native result depends on unit types and the unit slot at `+0x35f8`; 781 slots | unit participation slot is a known unmodelled concept |
| 41 | ZoneExplored | native compares an untruncated float ratio; server a truncated integer | small; not yet done |
| 179, 80, 172, 218, 246, 78, 140, 81, 173, 174 | | latent or zero-row differences | no retail slot affected, or reported and not re-read |

### Client stubs with server-invented logic
For these types the evaluator calls the shared stub `140001ba0`, so the client contributes no rule. The server checks implement behaviour that has no native basis; whether it is right can only come from a
server-side or retail witness: 27, 29, 46, 76, 96, 97, 98, 99, 100, 101, 166, 167, 168, 169, 180. For several of them the evaluator does not even pass the words the server reads (for example type 46 reads
ObjectId while only Value is passed; types 97 to 101 read ObjectId and Value while only the two units are passed).

### Systemic: which unit `Meets` tests
Many check classes evaluate `parameters.Target ?? player` in `Meets`, while the native case always tests the primary unit (`param_2[0]`) and reaches the second unit only where a handler is passed it. Reported
for types 87, 129, 196, 80, 93, 78, 172, 291 and 295, and likely wider. Whether this is a defect depends on how callers map their context onto `player` and `parameters.Target` (if a caller evaluating a
target prerequisite passes the tested unit as Target, the results coincide). This was not changed: it needs one decision for the whole manager, traced from the call sites, not a per-check patch.

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
