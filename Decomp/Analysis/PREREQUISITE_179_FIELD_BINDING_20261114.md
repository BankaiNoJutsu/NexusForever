# Type 179: difficulty-id binding and comparison ownership (2026-11-14)

Provenance: lead re-derived with read-only Ghidra 12.1.4 exports, LLVM 21.1.8
disassembly, original PE descriptor/pointer reads, and imported build-16042 data.
REA binds the installed executable to SHA256
`231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5`.
Evidence tier: **Verified native field binding and evaluator rule**. No retail
parity claim. Supersedes the field-meaning gate in
[the accessor pass](PREREQUISITE_179_NATIVE_ACCESSOR_20261114.md).

## Question, discrepancy and reach (before implementation)

The type-179 player wrapper substitutes Target, the manager helper rejects a
missing/unknown difficulty-table row, and the pure-179 spell shortcut accepts
zero without evaluating the comparison. The shortcut also evaluates slots after
the first None. Unify these paths only if the native value and world-object
subject are established. Reach from the preceding census: 41 raw slots / 39
prerequisite rows / 504 direct spell-column references / 517 weighted references;
not a count of affected casts.

## Native field binding, re-read directly

* `ClientDB_RegisterCreature2` `1401f6b00` uses descriptor `140a61ca0`, whose
  name is Creature2 and schema pointer is `140a4ef20`. Schema entries have a
  0x28-byte stride. The entry at `140a4f010` names `creature2DifficultyId` and
  its offset word at `140a4f01c` is **0x1c**. Raw Creature2 `+0x14` is the AOI
  size; it must not be confused with the intermediate record.
* `CreatureInfo_FillFromCreature2Row` `1404852b0` calls the Creature2 getter
  `1401f6d60`. At `1404853dd` it reads raw row `+0x1c`; `1404853e4` stores
  that ID at intermediate record **+0x14**. It also passes the same raw value
  to Creature2Difficulty getter `1401f64e0` at `140485412`.
* Intermediate-record constructor `140482720` writes **25** to record `+0x14`
  at `14048273c` (a qword write, also clearing the next word). Manager loader
  `1404836c0` constructs each 0x1e0-byte record through this constructor before
  dispatching its builder. The builder's `1404853e0..1404853e4` zero test skips
  the ID store when the raw field is zero. Therefore a **present creature
  record with raw difficulty zero has effective ID 25**, distinct from a
  subject with no creature record, whose prerequisite operand is zero.
  Read-only shipped-data census: 17,990 of 53,137 Creature2 rows have raw ID 0.
* `1401f64e0` is re-read, not trusted by label: its callback uses descriptor
  `140a69bf0`, named Creature2Difficulty. Its fallback registers that same
  table. A missing row does not undo the ID store in the builder.
* Unit-record setter `140458a40` stores the intermediate record at unit
  `+0x18`, copies its `+0x14` to unit **+0x30** at `140458a78`, and separately
  looks up that ID in Creature2Difficulty at `140458a7e`, retaining the
  resolved row at unit `+0x100`. Thus **the raw/cached ID and the resolved row
  pointer are distinct**. Successful lookup is not a prerequisite of the ID
  comparison.
* Constructor `140454380` installs vtable `140b66fd0` and starts unit `+0x30`
  at 25. Slot `140b66fe0` selects accessor `14045ba40`: cached ID below 25,
  otherwise intermediate-record ID, or accessor default 25. The shipped
  difficulty table contains exactly 24 IDs, 1..24. This corroborates the
  sentinel boundary; no server-side override facility is invented from it.
* Type-179 evaluator `14049eeb0`, selected by `1404a2100` case `0xb3` /
  manager vtable `+0x378`, still has its own rule: null primary fails;
  **no creature record compares zero**, bypassing the accessor's 25 default;
  otherwise the accessor supplies the difficulty ID. Comparison is against
  ObjectId with integer helper `1404a2090`; Value and the second unit are not
  read. Native row construction/evaluation stops at the first empty type slot
  (`1404a5190` / `1404a1ca0`, already mapped in the prerequisite audit).

Independent witnesses are the native builder/setter/evaluator bodies and the
client's named table schema plus the shipped table's ID domain. The alternative
rank/category reading is rejected: this exact value is copied from and used to
look up `creature2DifficultyId`, not `Creature2Difficulty.RankValue` or GroupValue.

## World-object applicability and virtual dispatch

Normal unit creation `1403d9760` allocates a CUnit through `140454380`, then
calls `140456960`; its caller does not install a derived vtable. The second
observed constructor caller, `14047f770`, uses the same constructor and create
routine for auxiliary units. Only `140b66fe0` directly references the accessor
in the Ghidra cross-reference pass; no different override is claimed absent
from all possible indirect construction paths.

Within `140456960`, creature-backed types 0, 8, 11, 13 and 32 (NonPlayer,
CollectableUnit, Platform, AiTurret, SimpleCollidable), plus Simple 10, select
their creature ID and reach the common runtime-record lookup and
`140458a40` call at `1404579fd`. They use the same native CUnit record/selector
despite the server splitting IUnitEntity from IWorldEntity. This supports the
pure-179 world-object check on the entity's own CreatureInfo/CreatureEntry;
it does not justify widening other prerequisite types on non-unit objects.

## Server scope and intended regressions

Keep one shared type-179 slot comparison: non-null entity, current server
CreatureInfo difficulty ID or raw Creature2DifficultyId; a present creature
record with raw ID zero maps to native default 25, while no record maps to zero;
compare ObjectId without requiring a resolved difficulty-table row. A null
subject fails. The player wrapper retains the player. Pure-179 spell AND/OR
evaluation uses that slot rule and stops at None; mixed non-unit rows keep their
existing policy. The type-180 rank helper remains separate and unchanged.

Candidate files: `Prerequisite/CreatureDifficultyPrerequisiteHelper.cs`,
`Prerequisite/Check/PrerequisiteCheckCreatureDifficulty.cs`, `Spell/Spell.cs`,
focused prerequisite tests and the superseded target-substitution test.
Regressions run red first: absent-record zero versus present-record default 25,
missing table lookup, null subject,
separate relationship target, AND/OR termination at None, and world-object controls.
Owning Game build and Prerequisite/Spell tests gate the implementation; the full
suite and any isolated reruns retain separate results.

## Reproduce

Focused exports are under ignored `artifacts/prerequisite-primary-subject/`:
`type179-loaders.txt`, `type179-record-binding.txt`, `type179-runtime-creature.txt`,
`type179-build-raw.txt`, `type179-callers.txt`. Use `Gap8Decomp.java` and
`Gap8Callers.java` read-only on the addresses above. LLVM independently checks
`1404853dd..140485428`, `140458a5c..140458a91` and `14045ba40..14045ba5b`.
Raw schema pointers are original PE virtual addresses, mapped through its
section table. No decompiled client source is committed.

Four durable function labels were applied and re-exported in the read-only
project (`type179-labelled.txt`): `CreatureInfo_ConstructWithDefaults`,
`Unit_SetCreatureInfoRecord`, `Unit_GetEffectiveCreatureDifficultyId`,
`Prerequisite_CheckCreatureDifficulty_LiveCase179`. Apply summary: four applied,
one function created (the previously unrecognized 28-byte accessor), zero missing
or duplicate rows. Focused case/audit CSV rows now name the live type-179 body.
The audit still flags the historical "handler table[179]" mention in type 292's
enum comment; it is not used as evidence for type 179.

## Result

The three paths now share `CreatureDifficultyPrerequisiteHelper.MeetsPrerequisite`.
The check's unused table-manager dependency was removed. Type 180's existing
rank helper and mixed non-unit prerequisite policy are unchanged. The existing
`CreatureDifficulty_ComparesTargetCreatureDifficultyId` test was rewritten as
an explicit-subject test, retaining its positive and negative difficulty IDs.

Initial 30-case red run failed all cases; the expanded 36-case red run also failed
all cases before production changes. Some null cases reached an unsupported
generic player-manager null logging path. First wider green attempt: 2,914
passed / four null cases failed at that logger. Those four tests were corrected
to call the type-179 player handler directly while retaining the real explicit-unit
manager null checks and every false assertion. The final four handler-level null
tests were not separately rerun red; the other 32 cases had genuine behavioral
baseline failures. The generic player manager's null logging is outside this
slice, not suppressed in production.

Final rebuilt scope:
`dotnet test Source/NexusForever.Game.Tests/NexusForever.Game.Tests.csproj --no-restore -v minimal --nologo -p:UseSharedCompilation=false -m:1 --filter "FullyQualifiedName~Prerequisite|FullyQualifiedName~Spell"`:
**2,918 passed, zero failed/skipped**, including all 36 new cases. This builds
the owning Game project and test dependencies; five CS8632 warnings are in the
unmodified MySQL qualification test file. Source tests do not establish retail
parity or native acceptance.

Explicit owning build:
`dotnet build Source/NexusForever.Game/NexusForever.Game.csproj --no-restore -v minimal --nologo -p:UseSharedCompilation=false -m:1`:
zero warnings/errors.

Full suite (`dotnet test ... --no-build --no-restore`): **15,069 passed / 30 failed /
two skipped / 15,101 total**. All 36 type-179 cases passed in this run. Failures
are in 17 methods outside the changed area: 28 report EF's
`ManyServiceProvidersCreatedWarning`, with one True and one Empty assertion
failure. Each failed method was then rerun in its own process: **all 17 methods
passed**, covering 94 cases, including all 30 previously failing cases.
This is non-reproduction under isolation, not a proven root cause or a green
full suite. No unrelated test/assertion or EF-warning policy was weakened.
Raw TRX, failed-method list, per-method output and result JSON remain under
ignored `artifacts/prerequisite-primary-subject/type179-full/`.

The function evidence inventory's old packet-consumer entry for the intermediate
constructor was corrected and three native function records added.
`Validate-DecompileMappingArtifacts.ps1` passed: **8,331 rows / seven files**.
Focused label uniqueness and both type-179 coverage rows pass. `git diff --check`
passes. No runtime/client launch, GUI or schema work; no entitlement change or
reference-database runtime dependency.

Remaining limits: the existing server CreatureInfo/CreatureEntry model supplies
the difficulty state; no new independent dynamic override state is invented.
Type 180, mixed non-unit row policy, native client acceptance and retail
qualification remain separate. End state: **Implemented / Source**, grounded in
Verified native semantics. No retail parity claim.
