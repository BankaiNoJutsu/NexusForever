# Type 179: primary subject and native accessor (2026-11-14)

Provenance: lead direct LLVM 21.1.8 disassembly and PE pointer reads of build
16042, SHA256 `231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5`.
The executable identity was checked through REA in the preceding
[primary-subject slice](PREREQUISITE_PRIMARY_SUBJECT_20261114.md).
Evidence tier: **Mapped native control/data flow**; field meanings below remain
unknown. End state: **mapped only**, with the exact implementation gate recorded.
No retail parity claim; no server behavior changed.

## Question and reach

Can the type-179 player wrapper, manager helper and spell shortcut be unified
against the client, without guessing what its accessor returns? This follows
`ff3cf41f` on `feature/spell-effects-remaining`.

Read-only `wildstar_client` census: 41 slots in 39 prerequisite rows; 504 direct
`Spell4`/`Spell4Effects` prerequisite-column references, 517 slot-weighted
references. These are dependency reach, not a count of broken casts.

Current Source discrepancies, recorded before any implementation:

* `PrerequisiteCheckCreatureDifficulty.Meets` substitutes `parameters.Target`
  for the player; its `MeetsUnit` retains the subject.
* `CreatureDifficultyPrerequisiteHelper` looks up the server's
  `Creature2Difficulty` row/id and rejects zero/missing difficulty.
* `Spell.TryEvaluateCreatureDifficultyPrerequisite` accepts a missing difficulty
  without evaluating the comparison, including non-unit world objects. Its
  nonzero path reads `Creature2DifficultyId` directly.

## Read directly

1. Evaluator `1404a2100`, case `0xb3` (cached fragment lines 910-912), passes the
   primary unit, comparison and ObjectId to manager vtable `+0x378`. This is the
   live dispatcher map from `coverage/prerequisite_evaluator_case_map.csv`, not
   the disproven handler-table index interpretation.
2. Body `14049eeb0..14049eefc`: null primary returns false. Without a pointer at
   primary `+0x18`, its actual comparison operand is zero. Otherwise it calls
   primary virtual `+0x10`. The result is passed to integer comparison helper
   `1404a2090` against ObjectId. This body never reads the relationship target
   or Value. Subject selection and zero-without-record are established, even
   though the nonzero accessor's meaning is not.
3. Constructor `140454380`: instruction `140454391` loads vtable `140b66fd0`,
   installed at `1404543d5`. It clears the record pointer at `+0x18` and sets
   unit dword `+0x30` to 25 at `1404543b3`.
4. Direct PE read at that table's `+0x10` (`140b66fe0`) gives `14045ba40`.
   This is a mechanically selected accessor from an installed vtable, not a
   guessed pointer-table index.
5. Accessor `14045ba40..14045ba5b`: reads unit dword `+0x30`; when signed-less
   than 25 it returns that word. Otherwise it reads the pointer at unit `+0x18`
   and returns that record's dword `+0x14`, or 25 when no record exists. Note
   that type 179 bypasses the accessor for a missing record and compares zero
   instead of this accessor's 25 fallback.

The original constructor and base-table association was a lead from
`function_labels.csv` (`Unit_Construct_SetsTable140b66fd0`); the constructor,
pointer slot and accessor were all re-read directly here. Derived unit-class
overrides of this slot were not enumerated.

## Hypotheses, not a field mapping

* Existing reading: this is the creature difficulty id, with unit `+0x30` an
  override and 25 a sentinel. The old server model and enum suggest this, but
  they are not independent evidence. No writer or named native consumer of
  `+0x30` has established the reading in this pass.
* Competing reading: a rank/category selector using the same override/fallback
  pattern. The small integer domain and sentinel also fit that. The record's
  runtime field layout must be established; a managed/raw-table field offset
  cannot simply be assigned to native record `+0x14`.

An extra native read of `Lua_Unit_GetDifficulty` (`140654f40..140654fe3`) does not
resolve this: it gets another record through `1403dec00` and returns that
record's `+8`. The `1403dec00..1403dec65` body keys a global tree by the unit's
pointer at `+0x138`; it does not call virtual `+0x10`. Its name alone cannot
corroborate the accessor's value.

## Exact gate and next separating read

Before changing type 179, recover the writers of unit `+0x30`, bind native
record `+0x14` to the client loader/schema (or a named getter/packet writer),
and inspect relevant derived vtables. This separates the two field readings.
For the spell shortcut, also determine the intended non-unit world-object
policy; the native unit evaluator does not establish it. The null/zero branch
and primary selector alone do not qualify a unified replacement for all three
server paths.

This is **pending native research**, not a claim that all available sources are
exhausted or that the retired-server behavior is unrecoverable. The type's
existing inferred implementation is unchanged. No new label asserts a semantic
name for `14045ba40`.

## Reproduce / verification

Use `llvm-objdump -d --start-address=<start> --stop-address=<end> <matching exe>`
for the ranges above. Map VA `140b66fe0` through the PE section table and read
one little-endian uint64. Local read output is retained under ignored
`artifacts/prerequisite-primary-subject/type179-native-disassembly.txt`.
`git diff --check` is the check for this documentation-only mapping slice.
No C# build or test was repeated: Source remains the `ff3cf41f` candidate whose
2,882-case Prerequisite/Spell gate passed, with the separately recorded full-run
failure and isolated reruns. No runtime, GUI, schemas or generated exports changed.
