# Gap 8: native reads for ModifyCreatureFlags (0x3C), ForcedAction (0x18), RestedXpDecorBonus (0x6D) (2026-11-08)

Scope: build 16042 `WildStar64.exe` (Ghidra 12.1.4 headless, project `I:\tools\ghidra_proj`, program `WildStar64.exe`, `-process -noanalysis -readOnly`, nothing saved, one process at a time) and the build 16042
tables `DB\Spell4Effects.tbl`, `DB\SpellEffectType.tbl`, `DB\Creature2.tbl`, `DB\GameFormula.tbl` (extracted under `I:\tools\extract\DB`). Method follows the ChangePhase / ChangePlane / ChangeDisplayName
records ([SPELL_EFFECT_CHANGEPHASE_20261108.md](SPELL_EFFECT_CHANGEPHASE_20261108.md), [SPELL_EFFECT_CHANGEPLANE_20261108.md](SPELL_EFFECT_CHANGEPLANE_20261108.md)); the row census is
[SPELL_EFFECT_REMAINING_20261108.md](SPELL_EFFECT_REMAINING_20261108.md).

**Evidence tier of this whole file: Observed (agent-written native reads and table parses) with a few Correlated statements marked as such. Nothing here is "re-derived" by the lead, nothing is Verified,
and nothing is retail parity.** Decompiled lines are quoted so the lead can re-check; the full decompiler dumps are scratch files under `I:\tools\gap8\` (outside the repository).

Scripts written for this record (all read-only): `Decomp/Analysis/scripts/Gap8Decomp.java`, `Gap8Callers.java`, `Gap8CallerGrep.java`, `Gap8ListFuncs.java`, `Gap8RefsFrom.java`, `Gap8Bytes.java`, `Gap8InsScan.java`,
`Gap8EffectTypeCmp.java`, `Gap8CreatureInfoScan.java`, `gap8_run.ps1`, and the table scripts `gap8_effect_rows.py`, `gap8_spell_effects.py`, `gap8_creature_flags_fit.py`.

## 0. What the client does with Spell4Effects rows at all (applies to all three types)

* `Spell4Manager_BuildRuntimeData` (`14055e180`) loads every `Spell4Effects` row (descriptor `PTR_u_Spell4Effects_140a6d268`), runs `Spell4Effects_ValidateRow` (`140567af0`) and, when it returns 0,
  copies the row into a 0xa8-byte runtime effect with `FUN_140567950`. Quoted from `140567950` (the row pointer is `puVar1 = FUN_140238780(param_3)`, the runtime object `param_2`):
  `param_2[4] = puVar1[3];` (effect type, `runtime+0x10`), `param_2[0xe] = puVar1[7];` (duration), `param_2[0xf] = puVar1[8];` (flags), `param_2[0x10] = puVar1[9];` (DataBits00) ... `param_2[0x19] = puVar1[0x12];`
  (DataBits09). So the client keeps all ten data words verbatim.
* `Spell4Effects_ValidateRow` (`140567af0`) is a per-type switch on `*(undefined4 *)(lVar8 + 0xc)` (the row's effect type). It has cases for types 1, 2, 3, 4, 5/8/0xc/0x21/0x8a, 7, 0xb, 0xe, 0xf, 0x12, 0x14, **0x18**,
  0x1d, 0x1e, 0x26/0x2b/0x2c, 0x2d, 0x32, 0x34, 0x97. **There is no case for 0x3C or 0x6D.** Only 0x18 carries data validation (section 2).
* `FUN_14023e0c0(effectType)` returns a `SpellEffectType.tbl` row; the validator requires `(*(uint *)(row + 4) & 1) != 0`. `SpellEffectType.tbl` (12 words per row: Id, Flags, DataType00..09) holds per-type data
  typing: 0 = float, 1 = integer, 2 = unused/raw (Correlated: the code in section 2 treats exactly the DataType 1 and 0 slots as int and float).
  Rows for our three types: `0x18` Flags 0x1b DataType = `2,1,2,0,2,2,2,2,2,2`; `0x3C` Flags 0x1b all 2; `0x6D` Flags 0x19 DataType = `0,2,2,...`.
* I scanned every instruction of the program for `CMP/SUB/LEA` against 0x18, 0x3C and 0x6D whose neighbourhood reads a `[reg+0x10]` or `[reg+0xc]` dword (`Gap8EffectTypeCmp.java`, output `cmp18.txt`, 1,931 loose
  hits). The only exact `CMP reg/mem, 0x18` hits near spell code were unrelated (`FUN_14056b260` is a settler-mission switch, `FUN_1402b45a0` / `FUN_1402b5160` are binary header parsers), and there is **no**
  `CMP ...,0x3c` or `CMP ...,0x6d` against an effect-type field. **Negative result, not proof:** a jump-table switch compiled without a literal compare could be missed; nothing in the spell-state paths I read
  (`Prerequisite_CheckActiveSpellEffectOnTarget` `140469a70`, `Spell4Manager_BuildRuntimeData`) branches on these types.
* Label conflict found on the way, relevant to earlier claims: `Entity_ExecuteSpellEffectLowRange` (`1406b1300`) is **not** a spell-effect executor. Its `switch(param_2)` cases are 1,2,3,0x1c,0x1d,4,5,6,7,8,0x1b,0x3d,0x45,
  0x46,0x47,0x5f,0x60 and its bodies build wide-string parameter tables with the literals `L"price"`, `L"reservePrice"`, `L"total"`, `L"buyout"`, `L"purchase"`, `L"gifter"`, `L"existing"`, `L"name"` (for example
  `memcpy(local_598,L"price",sVar3);` inside `case 1/2/3/0x1c/0x1d`). It looks like an auction / purchase / gift message-parameter formatter keyed by a message type. The existing statement "handles spell effectTypes
  0x01-0x60 ... only types 4/5/6/8/27 have unique handlers" should not be used as evidence about spell effect types (proposed relabel in section 5).

## 1. ModifyCreatureFlags (0x3C, 75 rows)

### 1.1 Row decode (Observed, `gap8_creature_flags_fit.py`)
All 75 rows use only DataBits00..03 (DataBits04..09 are zero in every row). The four words are **two (apply, mask) pairs**:

| Word | Values | Reading |
|---|---|---|
| DataBits00 | 0 (49 rows), 1 (26) | 1 = set the bits of DataBits01, 0 = clear them |
| DataBits01 | masks such as 1, 17 (`0x11`), `0x1000`, `0x2082`, `0x4000`, `0x10000`, `0x12093`, `0x800000` | mask applied to word A |
| DataBits02 | 0 (35 rows), 1 (40) | 1 = set the bits of DataBits03, 0 = clear them |
| DataBits03 | masks `0x4020` (bit 5 is set in 45 of the 75 rows, bit 14 in 40), `0x2f`, `0x402f`, `0x40af`, `0x40a7`, `0x40a0`, `0x20`, `0x80`, `0x100`, `0x8`, ... | mask applied to word B |

Evidence for the set/clear reading is the paired spells: spell 83221 "SpellCannotBeSelection" is `[1, 1, 0, 0]` and spell 83222 "-SpellCannotBeSelection" is `[0, 1, 0, 0]` (same mask, opposite apply word);
"Rumble" (35234) is `[0, 0, 1, 0x4020]` for 1,683 ms (set `0x4020` in word B for the duration), "Well-Fed" (83356) is `[0, 1, 0, 0x4020]` with no duration (clear bit 0 of word A and `0x4020` of word B).
Rows with a `DurationTime` (34 of 75) are the "set for a while" ones. This is a table-shape reading; no client code interprets the apply words.

### 1.2 Which flag words these are (Correlated)
`DB\Creature2.tbl` has `Flags` (row offset `0x150`), `UiFlags` (`0x154`) and `ActivationFlags` (`0x158`). The client copies them in `FUN_1404852b0` (`CreatureInfo` builder, reads `FUN_1401f6d60(param_3)` = Creature2
row by id):

```
param_2[0x24] = puVar2[0x54];   // Creature2.Flags           -> creature info +0x90
param_2[0x25] = puVar2[0x55];   // Creature2.UiFlags         -> creature info +0x94
param_2[0x26] = puVar2[0x56];   // Creature2.ActivationFlags -> creature info +0x98
```
The fit script (`gap8_creature_flags_fit.py`) compares the effect masks with the bit usage over all 53,137 Creature2 rows: every DataBits01 bit is attested in Creature2.Flags (bits 0,1,4,5,7,12,13,14,16,23); bit 23
(`0x800000`, "Reading Vitals") is **not** a UiFlags bit (UiFlags only has bits 0..21), and the DataBits01 values `0x11`, `0x1011`, `0x2082`, `0x12093` are whole Creature2.Flags values that are not UiFlags values.
Conversely `0x2f`, `0x402f`, `0x40af`, `0x40a0` (DataBits03) are whole UiFlags values and not Flags values. So **word A (DataBits00/01) is the Creature2.Flags word and word B (DataBits02/03) is the Creature2.UiFlags word**.
Weakness: most masks are low bits that exist in both words; the discrimination rests on bit 23 and about a dozen whole-value matches. It is a correlation, not a decode.

### 1.3 Client consumers of the flag bits (Observed; the per-bit names are mine, taken from what the client does)
Entity `+0x18` points at the creature info (built per Creature2 id by `FUN_140483d20` from `FUN_140485490`/`FUN_1404852b0`; the manager keeps the table-derived objects, so this looks shared per creature id: inferred, not
checked against the create path). Reads found (decompiled lines):

| Bit | Consumer | Quoted line | What it does |
|---|---|---|---|
| UiFlags `0x40` | `Lua_Unit_IsElite` `1406567d0` | `if ((((*(byte *)(lVar5 + 0x94) & 0x40) != 0) \|\| (iVar4 == 0xc)) \|\| (iVar4 == 0x1f)) { bVar3 = 1; }` | unit counts as elite |
| UiFlags `0x10` | `Lua_Unit_IsRare` `1406568b0` | `*puVar3 = (uint)((bVar1 & 0x10) != 0);` with `bVar1 = *(byte *)(*(longlong *)(lVar4 + 0x18) + 0x94)` | unit counts as rare |
| UiFlags low nibble `0xf` | `Lua_Unit_GetMouseOverType` `1406566e0` | `pcVar4 = "Simple", ((byte)*(undefined4 *)(*(longlong *)(lVar2 + 0x18) + 0x94) & 0xf) != 0xf)) { pcVar4 = "Normal"; }` | all four low bits set returns mouse-over type "Simple", otherwise "Normal" (this includes the common UiFlags values `0xf`, `0x2f`, `0xaf`) |
| Flags `0x40000000` | `Lua_Unit_IsTagged` `1406612c0`, `FUN_140482450` | `((*(uint *)(*(longlong *)(lVar4 + 0x18) + 0x90) & 0x40000000) == 0)) && ((*(int *)(lVar4 + 0x124) != 0 \|\| (*(longlong *)(lVar4 + 0x128) != 0)))` | when set the unit is never reported as tagged (tag owners at entity `+0x124/+0x128` ignored) |
| Flags `0x10000` | `Interaction_HandlePostActionTargetFeedback` `1403dd1c0`, `Entity_ApplySceneProximityCue` `14047a1f0` | `((*(uint *)(*(longlong *)((longlong)pvVar13 + 0x18) + 0x90) & 0x10000) == 0)) { ... FUN_140195960((longlong)pvVar13 + 0x1628,5000,&local_158,4); FUN_140470650(pvVar13,1,1);` | when clear the unit runs a 5,000 ms reaction after interaction / scene proximity (behaviour not fully read; plausibly "turn to / acknowledge the player"); when set it is skipped |
| Flags `0x8` | `FUN_140460250` | `if ((*(byte *)(lVar1 + 0x90) & 8) != 0) { ... if (*(ushort *)(lVar1 + 0xb0) <= uVar5) ... if (uVar3 <= *(ushort *)(lVar1 + 0xb2))` | when set, true only while the local player's level lies in the creature's `[+0xb0, +0xb2]` range (Creature2 words `0x65/0x66`) |
| Flags `0x400` | `FUN_14045bd30` | `if ((*(uint *)(*(longlong *)(param_1 + 0x18) + 0x90) & 0x400) == 0)` then a level test against GameFormula `0x3bd`, else returns true | when set the level test is bypassed |

Not read: the meaning of Flags bits 0, 1, 4, 5, 7, 12, 13, 14, 16 (the ones the effect rows actually touch) and UiFlags bits 5, 7, 14 (`0x20`, `0x80`, `0x4000`, the most used in the effect rows). My instruction scans for
`TEST/AND/CMP [reg+0x90|0x94], imm` only find tests that use an immediate on the memory operand; the rest go through a register load and were not traced. So **most bits the 75 rows touch remain unnamed.**

### 1.4 Mapping to the server's create-flag enum
There is none. `EntityCreateFlag` (`Source\NexusForever.Game.Static\Entity\EntityCreateFlag.cs`, 8 bits, written with `writer.Write(CreateFlags, 8)` in `ServerEntityCreate.cs:134`) is a packet byte; the effect words are
32-bit Creature2.Flags/UiFlags masks (bits up to 23 and 14). No `Server*` packet in `NexusForever.Network.World/Message/Model` carries a Creature2-flag word (`ServerEntityCreate` sends the creature id through the entity
model, plus the create byte). The one client use of an entity-level flag byte I saw (`Lua_Unit_ShouldShowNamePlate`, `if ((*(longlong *)(lVar2 + 0x18) != 0) && ((*(byte *)(lVar2 + 0x28) & 0x20) != 0))`) tests
entity `+0x28`, which is not the creature-info word.

### 1.5 Verdict
* What the client does with the data words: **nothing found.** No consumer of effect type 0x3C exists in the spell paths read, the type has no validator case, and the Creature2 flags are read from a table-built
  creature-info object, not from a per-entity field the server can update. I found no packet that would deliver a modified flag word.
* Enough for a server handler? **No.** The set/clear pair layout (1.1) is solid enough to interpret, but the server has no Creature2-flag state, the client would not see a change, and the bit meanings that matter
  are mostly unread. A handler would be a server-private flag pair with no observable effect. If one is wanted anyway the fields are (DataBits00, DataBits01, DataBits02, DataBits03) with the semantics of 1.1 and
  expiry on `DurationTime`, labelled as internal state only.
* Missing to make it restoration-grade: a client path that re-evaluates unit flags from active effects (not found), or a retail witness that a flagged unit changes its Lua `IsElite`/`IsTagged`/mouse-over answers.

## 2. ForcedAction (0x18, 95 rows)

### 2.1 What the client validates (Observed, `Spell4Effects_ValidateRow` `140567af0`)
```
case 0x18:
    if (1000 < *(int *)(lVar8 + 0x28) + 1U) { return 0x5c; }
    if ((*(float *)(lVar8 + 0x30) < 0.0) || (DAT_140b79ed0 < *(float *)(lVar8 + 0x30))) { return 0x5d; }
    break;
```
Row offsets: `+0x24` DataBits00, `+0x28` DataBits01, `+0x2c` DataBits02, `+0x30` DataBits03 (confirmed by `FUN_140567950`, where runtime `+0x40` = row `+0x24`). `DAT_140b79ed0` is `0x3f800000` = 1.0
(`Gap8Bytes.java`, `b1.txt`). So:
* **DataBits01 is an integer in [-1, 999]** (error 0x5c otherwise). Table values: -1 (10 rows), 1 (47), 2, 3, 4, 5, 10, 20, 100, 999 (20 rows). Reading as a use count with -1 / 999 meaning "unlimited" is a
  name-free guess (Correlated with spells such as "Preparation" `[0/1, 999, 0/1, 0.0x]` over 4 to 8 s); not decoded by any client code.
* **DataBits03 is a float in [0.0, 1.0]** (error 0x5d otherwise): a probability. 52 rows carry real floats (0.04 to 1.0, for example `0x3f800000`, `0x3da3d70a` = 0.08 "Preparation", `0x3e800000` = 0.25
  "Fade Out"). **43 rows carry the raw integer 1** (the pre-float format: all four words `1,1,1,1` / `2,1,0,1` ...), which the validator reads as the denormal 1.4e-45. Either retail treated these as "always" through a path that
  does not look at the float, or they were never re-authored. The client does not say which.
* `SpellEffectType.tbl` row 0x18 types DataBits01 as int and DataBits03 as float, DataBits00 and DataBits02 as untyped (2): consistent with the validator.

### 2.2 The untyped words (not decoded)
Shapes (DataBits00, DataBits02): (1,0) 30 rows, (0,1) 22, (1,1) 21, (2,0) 13, (0,0) 7, (2,1) 1, (3,0) 1. DataBits04 is 2 in 14 rows together with a non-zero DataBits05 (333, 248, 277, 734, 745, 746, 748, 780, 781,
`0xf64a`); 8 of those 10 distinct values are ids in `Spell4StackGroup.tbl` (333 and `0xf64a` are not), so DataBits05 is probably a stack-group-like reference (Correlated, incomplete).
Name correlation (spell names, not code): DataBits00 = 2 on "On Heal Other Forced Fierce", "Critical Hit", "Crit Buff Proxy", "Bloodlust", "Retribution", "Predict: Cast", "Furious Gambit Proxy 1-4"; DataBits00 = 1
on "Forced Miss", "On Damage Forced Miss", "On Damage Forced Shallow", "Predict: Fumble", "Counterstrike", "Rewind"; DataBits00 = 0 with DataBits02 = 1 on the defensive procs "Like the Wind", "Blur", "Fade Out",
"Sentinel", "Dirty Tricks", "Preparation (second row)"; DataBits00 = 3 only on "Deadeye!". **But** "Forced Miss" (spell 8606), "On Damage Forced Miss" (8793) and "On Damage Forced Shallow" (8795) have identical
words `[1,1,1,1]` and no sibling effect (checked with `gap8_spell_effects.py`), so the words cannot be what separates a forced miss from a forced shallow hit; that distinction lives in something not in this row (the
spell, a prerequisite, or the hit-result calculation).

### 2.3 Combat-roll / prediction consumer
Not found. The client has no code that compares an effect type against 0x18 (section 0 scan), and combat results arrive from the server (`SpellDamageDescription_ReadPayload` carries a 4-bit combat result). The
"Predict: ..." spell names do not correspond to a client prediction routine I could find.

### 2.4 Verdict
* Client use of the data words: validation only (DataBits01 int in [-1, 999], DataBits03 float in [0, 1]).
* Enough for a handler? **No.** The server would need a combat-roll override model (which roll, which side, how the counter in DataBits01 is consumed, what DataBits00/02 mean, how the 43 integer-1 rows behave);
  only the two typed fields are native-validated and neither is mapped to a behaviour. A handler written now would invent the roll semantics.
* Missing: a retail witness of a forced-miss / forced-crit proc (combat log result for a known row), or the server-side roll code from another source.

## 3. RestedXpDecorBonus (0x6D, 25 rows)

### 3.1 Row data (Observed)
DataBits00 is the only used word and is a float (`SpellEffectType.tbl` row 0x6D DataType00 = 0 = float; every other word 0): Comfort / Lighting / Aroma / Ambience / Pride - Small / Medium / Large = `0x3e4ccccd` (0.2) /
`0x3ecccccd` (0.4) / `0x3f19999a` (0.6); campfire "Takin' a Breather" (32775, 32776) = 0.25 and 0.5 with prerequisite columns 390 (type 28, comparison 2, no value); "Rest XP - 99-80 ... 19-0"
(52861 to 52865) = `0x3f99999a` 1.2, `0x3fb33333` 1.4, `0x3fcccccd` 1.6, `0x3fe66666` 1.8, `0x40000000` 2.0, each with prerequisite 22359 (type 73 Vital, object 7, comparison 5, value 100: not resolved);
"Sleeping Time Snuggles" (71080) = 0.1 for 60,000 ms; test spells 28133 (0.5) and 34790 (0.2, 3,000 ms). None of the 25 rows has data in any other word.

### 3.2 Client side (Observed)
* No consumer of type 0x6D (section 0): no validator case, no literal compare.
* The client's rested-XP surface is the pool word at current-player `+0x1688`: `Lua_GameLib_GetRestXp` (`14050cbc0`): `iVar1 = *(int *)(DAT_140c65898 + 0x1688); ... *pdVar2 = (double)iVar1;`.
  `ServerExperienceGained_ApplyPlayerXpState` (`1403c8760`): `*(int *)(param_1 + 0x1678) = *(int *)(param_1 + 0x1678) + *param_2; *(int *)(param_1 + 0x1688) = *(int *)(param_1 + 0x1688) - param_2[1];`.
  The server therefore decides both the XP and how much of the pool a gain consumes; the client only subtracts what it is told.
* New detail on the display accessor `Lua_GameLib_GetRestXpKillCreaturePool` (`14050cbf0`), which earlier records described only as "applies a coefficient": it reads `GameFormula` row `0x155` word at `+0x18`
  (`DAT_140dc3294 = *(float *)(lVar3 + 0x18);`), computes `fVar4 = DAT_140b79ed0 / DAT_140dc3294;` then `fVar4 = fVar4 + DAT_140b79ed0;` and returns `(int)((float)uVar1 * fVar4)`. `GameFormula.tbl` row 0x155
  has `Datafloat0 = 0.5`, so the displayed "kill creature pool" is `pool * (1 + 1/0.5) = pool * 3.0`. This describes how many XP of kills the stored pool covers (a 200% rested bonus on creature kills) and is the
  only quantitative rested-XP rule the client contains. Whether the pool is stored in "bonus XP" units that the server consumes at 1:1 or 1:3 is not shown by the client.
* No client function that accrues the pool from spell effects or housing (earlier sweep of `+0x1688` in INITIAL_FINDINGS.md agrees; I did not repeat it).

### 3.3 Verdict
* Client use of the data word: none.
* Enough for a handler? **No.** The float is a rate/multiplier with no native combine rule (additive across decor sources? multiplies the 0.24%-per-hour accrual? replaces it?). The "Rest XP - 99-80 ... 19-0" rows ramping
  1.2 to 2.0 by level bracket hint at a multiplier on the base accrual, and the small/medium/large decor values (0.2/0.4/0.6) hint at a small additive bonus, but those are name/shape guesses. The `GameFormula 0x155`
  coefficient (0.5) is the only native number and concerns display of kill coverage.
* Missing: a retail witness of the rested-XP pool growing with and without a decor/campfire aura (rate per hour before/after), or the server-side accrual code.

## 4. Summary table

| Effect | Client consumer of the effect type | Native facts | Handler-ready? |
|---|---|---|---|
| 0x3C ModifyCreatureFlags | none found | row = two (apply, mask) pairs; words correlate with Creature2.Flags / UiFlags; 7 flag bits named from client reads; creature info is table-built | No |
| 0x18 ForcedAction | validator only | DataBits01 int [-1, 999]; DataBits03 float [0, 1] (validator `140567af0`); 43 rows use raw 1 | No |
| 0x6D RestedXpDecorBonus | none found | DataBits00 float; client pool display = pool x 3.0 via GameFormula 0x155 | No |

## 5. Proposed function label rows (not applied; format of `function_labels.csv`: `program,address,name,comment`)
Tier: Observed unless stated. Rows 1 to 7 are new names; rows 8 and 9 supersede existing rows whose names the decompilation contradicts; row 10 is a suspect row to re-check, not a rename.

```
WildStar64.exe,1401f6d60,ClientDB_GetCreature2,Creature2 row by id: DAT_140c63840 dispatcher with descriptor PTR_u_Creature2_140a69c60 else ClientDB_RegisterCreature2 then DAT_140c63d10 vtable +0x18. Sibling wrappers 1401f6d10 (count) and 1401f6dc0 (by index) are the same pattern; only 1401f6d60 was decompiled.
WildStar64.exe,1404852b0,CreatureInfo_FillFromCreature2Row,Copies a Creature2 row into the runtime creature info: Flags (row +0x150) to info +0x90 and UiFlags (row +0x154) to +0x94 and ActivationFlags (row +0x158) to +0x98; also name and difficulty and level range words.
WildStar64.exe,140485490,CreatureInfo_BuildFromCreature2Id,Calls CreatureInfo_FillFromCreature2Row then allocates the optional sub-blocks at info +0xc0 and +0xc8 and +0xd0 from Creature2 words 0x0f-0x16 and 0x57-0x59 and 0x9d-0xa6.
WildStar64.exe,140483d20,CreatureInfoManager_BuildInfoEntry,Virtual method of the creature info manager: builds one table-derived creature info entry (BuildFromCreature2Id plus text ids and zone/instance/vehicle references); the entity creature-info pointer at entity +0x18 refers to such entries.
WildStar64.exe,140238780,ClientDB_GetSpell4Effects,Spell4Effects row by id (descriptor PTR_u_Spell4Effects_140a6d268 at 140a6d268); only direct callers are Spell4Effects_BuildRuntimeEffect and Spell4Effects_ValidateRow.
WildStar64.exe,140567950,Spell4Effects_BuildRuntimeEffect,Zeroes a 0xa8-byte runtime effect then copies a Spell4Effects row into it: type to +0x10 and delay +0x20 and tick +0x24 and duration +0x38 and flags +0x3c and DataBits00-09 to +0x40..+0x64; called from Spell4Manager_BuildRuntimeData after ValidateRow returns 0.
WildStar64.exe,140482450,Entity_IsTaggedByOtherUnit,Returns 1 when creature info Flags bit 0x40000000 is clear and a tag owner (entity +0x124 or +0x128) is set that is not the local player's; Lua_Unit_IsTagged tests the same flag.
WildStar64.exe,14022d500,ClientDB_GetRace,SUPERSEDES Creature2_GetRowByUnitId: decompiled body dispatches &PTR_u_Race_140a6c938 and falls back to ClientDB_RegisterRace; it is a Race table row getter (the existing note about entity +0xd8 and prerequisite 172 / Entity_GetHealthScaleFactor was not re-read; the table descriptor is Race).
WildStar64.exe,1406b1300,SystemMessage_BuildTypedParams,SUPERSEDES Entity_ExecuteSpellEffectLowRange: switch on a message type (cases 1-8 and 0x1b-0x1d and 0x3d and 0x45-0x47 and 0x5f-0x60) building wide-string parameter tables with price and reservePrice and total and buyout and purchase and gifter and existing and name; not a spell-effect executor.
```
Suspect, not proposed as a rename: `14049fd50` `Prerequisite_CheckSpellEffectTypeOnUnit_Table105` compares `*(int *)(DAT_140c65970 + 8) == param_4`, and `DAT_140c65970` is the global read by roughly 200 PlayerPath / Settler / Soldier
functions (`Gap8Callers.java` on `140c65970`), not an entity active-effect list. The "active spell-effect type on entity+0x15d0" description does not match that body and should be re-read before anything is built on it.

The two open conflicts named in the gaps prompt (`140001ba0`, `1400d7b40`/`1400da180`) were not touched by these reads.

## 6. Reproduction notes
* Ghidra runs: `powershell -NoProfile -File Decomp/Analysis/scripts/gap8_run.ps1 -Script <Gap8Script> -Out <file> -ScriptArgs "<comma separated args>"` (one `analyzeHeadless` process: `-process WildStar64.exe -noanalysis -readOnly`).
* Table dumps: `python Decomp/Analysis/scripts/gap8_effect_rows.py 0x3c|0x18|0x6d`, `gap8_spell_effects.py <spell ids>`, `gap8_creature_flags_fit.py`.
* Stays "not re-derived": every address and quoted line above, and the bit names in 1.3.

## Lead review (2026-11-08)
Not re-derived: every address and decompiled line above except the two below. The lead re-decompiled `1406b1300` (strings `price`, `reservePrice`, `total`, `buyout`, `purchase`, `gifter`, `existing`, `name`: a message-parameter builder) and `14022d500`
(dispatches `&PTR_u_Race_140a6c938`, falls back to `ClientDB_RegisterRace`: a Race row getter). Both label corrections were applied to `function_labels.csv` in place (`SystemMessage_BuildTypedParams`, `ClientDB_GetRace`); the
Ghidra project and `function_evidence_inventory.csv` were not updated and will pick the new names up at the next label application. Earlier notes that call `14022d500` a Creature2 getter for prerequisite 172 are unverified.
Conclusion stands: the client has no consumer for effect types 0x18, 0x3C and 0x6D, so none gets a handler from this evidence. The 14049fd50 suspicion (prerequisite 105 reads a path global) is unchecked.

## Label application (2026-11-10)
Rows 1 to 7 of section 5 were re-decompiled on a freshly analysed project (`run_ghidra_analysis.ps1`, Ghidra 12.0.4, binary `sha256:231bb2bb...96b3e5`) and added to `function_labels.csv`; each row's comment states
what was re-derived and what was carried over from this note. An export-only pass applied them (`applied=5540, missing=0`) and `function_evidence_inventory.csv` was backfilled (7 rows).
`14049fd50` was re-read and resolved: it is the type 52 Path handler (evaluator case `0x34`, vtable `+0x478`), so the suspicion above is confirmed and the row is renamed `Prerequisite_CheckPath_LiveCase34`.
Type 105 is inline in the evaluator and compares against ObjectId; details, the server fix and the wider `_TableN` label problem are in `PREREQUISITE_105_SPELL_EFFECT_TYPE_20261110.md`.
The section 4 verdicts were not revisited: no new consumer search was run for effect types 0x18, 0x3C or 0x6D.
