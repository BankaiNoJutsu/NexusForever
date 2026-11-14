# Type 12: primary-subject DeadState (2026-11-14)

Starting branch/head: `feature/spell-effects-remaining` / `2c35012c`.
Question: does DeadState inspect the primary unit, and what native dead-state
predicate is safe to carry into the current Source model?

Before edits: **Mapped native selector/predicate**, case `0x0c` dispatches primary
unit to `14049c800` through vtable `+0x80`; the earlier note records two nonzero
fields (`+0x250`, `+0x254`) or Ghost kind, then comparison of that boolean with
Value for Equal/NotEqual only. Source's player wrapper substitutes Target; its
explicit-unit counterpart retains the subject. The two native fields' equivalence
to Source `!IsAlive` remains **inferred**, not promoted by existing tests. The
old test explicitly expects the APIs to disagree; it is not native evidence.

**Observed earlier reach:** 168 slots / 168 rows / 364 direct/weighted spell-
column references. Recheck census and anchors. Type 102 remains **Blocked** on
the missing attributable retail untimed-Fluff lifecycle trace, per user direction.

Candidate files: type-12 check, focused context/dead-state tests, this finding,
ledger, handoff, spell tracker and any evidence coverage warranted by the read.
Re-read dispatcher/helper and named native IsDead path; seek bounded field-writer
evidence before declaring the existing proxy verified. No general death/ghost
model, wire or activation policy change follows from subject selection alone.
Verification: matching executable hash, native disassembly/registration, read-only
reference census; new focused cases red before production edit; owning Game
build and Prerequisite/Spell/Cooldown tests; `git diff --check`. Static/headless;
no client launch, Computer Use or runtime-data changes. No retail parity claim.

## Native selector and predicate re-derived by the lead

**Verified native selector/comparison:** LLVM 21.1.8 and direct PE reads against
build 16042 SHA256
`231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5`.
Cached dispatcher `1404a2100.fragment.c`, case `0xc`, lines 188–190, passes
`*param_2` as subject, comparison and Value to manager virtual `+0x80`. The
matching executable's switch entry resolves to `1404a24e7`; instructions
`1404a24ea..1404a24f8` put Value in R9D, comparison in R8D, primary unit in RDX,
then call that virtual slot. Native vtable slot `140b677c0` contains `14049c800`.
No relationship-unit read or ObjectId read appears in this case.

`14049c800..14049c83e` normalizes the OR of two nonzero 32-bit fields (`+0x250`,
`+0x254`) and kind `+0x80 == 0x17` to integer 0/1. Mode 1 compares that integer
with the full Value operand for equality; mode 2 compares for inequality;
all other modes fail. Value is neither ignored nor converted into a truth flag.
This agrees with the existing Source comparison, so no comparison change is made.

**Source boundary, not a native null-return claim:** this helper dereferences
the supplied unit without a local null check. Source's existing explicit-unit
handler and manager fail null closed; the player wrapper is corrected to reuse
that contract rather than substitute a populated Target. No claim is made that
calling the native helper with null returns false.

**Verified native UI bindings:** Lua table entry `140c5b140` names `IsDead` and
points at `1406535f0`. Its body resolves a unit then tests **only `+0x250`** at
`140653621`; it does not test `+0x254` or Ghost kind. The first field therefore
has a direct named IsDead witness, while the full prerequisite predicate is
broader than that Lua getter. Independently, Lua GetType's index23 assignment
at `140649f68` points at string `140b2bc58`, `Ghost`; the array is indexed by
entity `+0x80`. This corroborates the helper's literal `0x17` and Source enum23.

Cache root: `exports/WildStar64.exe/selected_decompiled_cache/functions/`
`sha256_231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5/`.
Re-read fragments: `1404a2100.fragment.c`, `14049c800.fragment.c`. Ignored
`artifacts/prerequisite-type12/native-binding.json` and `native_binding.py`
retain identity/table checks; selector/dead-state/IsDead disassemblies are in
the same directory. Existing dispatcher/DeadState/IsDead labels remain accurate;
no label edit or decompiler export refresh is needed.

## Bounded field-writer investigation and model limit

**Observed instruction inventory:** a whole `.text` LLVM disassembly scan records
548 instructions using explicit `0x250`/`0x254` base-register displacements and
102 syntactic write candidates. Most refer to other objects, stack storage or
virtual slots; an offset match is not an entity-field witness. This is not an
exhaustive alias, bulk/vector-write or state-transition analysis.

**Mapped structural writer:** `14045ec90` checks entity kind at `+0x80`, compares
the supplied 32-bit input with current `+0x250`, and on its changing path writes
that same input to **both** `+0x254` at `14045ecd6` and `+0x250` at `14045ecdc`.
It then performs entity/effect follow-up. This recovers a shared update contract,
not the input's complete caller authority, packet provenance or transition order.
Creation function `140456960` derives `+0x250` from a zero test of word `+0x40`
at `140457bc5..140457bce`; this pass does not independently bind that raw word.
Constructor, metadata-builder and unrelated-object candidates were inspected
only to bound offset-based inferences. In particular `140625410` uses its own
`+0x80` as a pointer, not the entity-kind integer; its `+0x250` write cannot be
promoted into an entity dead-state writer by matching the offset.

**Inferred Source bridge retained:** `UnitEntity.IsAlive` means Health > 0 and
no Source deathState. No witness here proves that its negation equals the two
native fields in every transition. Competing readings remain: the second field
could be a retained counterpart of the UI dead state, or a separately driven
state/latch that overlaps it on the shared setter path. Lua's one-field getter
and the shared setter fit both. A separating input is the complete caller/alias-
writer chain showing when `+0x254` can differ from `+0x250`, alongside health/
death restoration and its authoritative packet/state inputs. This is pending
native research, not a claim of unrecoverability; no new Source state is invented.

**Observed Source ghost boundary:** concrete `GhostEntity`/`IGhostEntity` is an
`IWorldEntity`, not an `IUnitEntity`; current prerequisite APIs accept units.
An IUnitEntity test proxy with kind Ghost verifies the existing kind branch,
not real-Ghost admission or a new world-object route. That broader model/caller
policy remains separate. Local field scan and inspected writer disassemblies
remain ignored in `artifacts/prerequisite-type12/`.

## Reference census and caller contract

**Observed imported reference data:** fresh read-only census over all three
prerequisite slots and all 13 prerequisite columns of `spell4`/`spell4effects`
reproduces **168 slots / 168 rows / 364 direct and weighted references**.
The slots use Equal 159 / NotEqual 9, with Value 0 or 1. These are raw dependency
counts, not affected casts; nested/non-spell consumers and early-empty-slot
reach filtering are outside scope. Census script/results stay under the ignored
slice directory (`census.py`, `ranking.json`, `slots.json`, `references.json`).

Caster-cast Spell4 **50201** points to prerequisite **31617** (type12 / Equal 0
in slot0, followed by type56). Target-cast Spell4 **27900** points to **5810**
(type12 / Equal 0 in slot0, followed by types27/116). The latter slot has nonzero
ObjectId, which this native case does not consume. These anchors show both caller
roles, not selector semantics by themselves. Read-only output: `anchors.tsv`.

**Observed Source routing:** Spell caster/target checks already choose their
subjects with `MeetsUnit`, supplying the other unit as relationship context.
`ClientActivateUnitCastHandler.MeetsActivateSpellPrerequisite` supplies player
plus activated Target to `Meets`. Native activation-caller policy is not newly
qualified; the check's wrapper must preserve the Source caller's chosen primary.

## Red cases and bounded correction

**Observed Source red run:** 29 new cases plus ten existing controls produced
**15 failed / 24 passed / zero skipped**, 39 total. All 15 new context cases
failed; fourteen new integer-Value/unsupported-mode cases and ten old controls
passed. This isolates Target substitution and missing relationship parameters,
not a defect in the retained boolean comparison or proof of the !IsAlive proxy.

**Verified native selector / Source correction under verification:** only the
player wrapper delegates to `MeetsUnit(player, ...)`. It preserves Target as
relationship context and the existing null guard, !IsAlive proxy, Ghost-kind
branch and boolean-versus-Value comparison. The obsolete target-first test is
renamed and rewritten to retain the primary, with explicit target-as-primary
controls. The existing two entity-test comparisons use opposite living/dead
primary/target states. Eight manager cases cover both states, modes and Values;
four cover Source null-primary rejection, one absent parameters and two a
Ghost-kind relationship proxy. The fourteen numeric controls preserve integer
Value and invalid-mode semantics. Tests remain Source evidence, not native or
retail model qualification.

## Result: Implemented / Source

**Observed verification:**

- `dotnet test Source/NexusForever.Game.Tests/NexusForever.Game.Tests.csproj --no-restore -v minimal --nologo -p:UseSharedCompilation=false -m:1 --filter "FullyQualifiedName~Prerequisite|FullyQualifiedName~Spell|FullyQualifiedName~Cooldown"`
  passes **3,086 / zero failed / zero skipped**, including all 29 new cases.
  `green.log` retains the output. Warnings remain in unchanged Aspire config,
  MySqlProviderQualificationTests (five CS8632) and ReceivedPropertyEffectTests
  (one xUnit2031); none is represented as fixed.
- Owning `dotnet build Source/NexusForever.Game/NexusForever.Game.csproj --no-restore -v minimal --nologo -p:UseSharedCompilation=false -m:1`
  passes with **zero warnings/errors** (`owning-build.log`).
- Native identity/switch/vtable/IsDead/Ghost bindings, census, the seven-scope
  [semantics record](coverage/prerequisite_12_semantics_20261114.csv), retained
  Source proxy and note links pass
  `python artifacts/prerequisite-type12/verify_slice.py`. `git diff --check`
  passes. No C# changes to the shared comparison or entity death model.
- Full suite not repeated; its preceding non-green result remains in the
  handoff. No deployment, provider/socket, native client execution or retail
  qualification.

End state: **implemented with Source verification; Verified native selector and
boolean comparison; existing Source death proxy remains inferred**. The full
field-to-model bridge is still pending native research. Type 102 remains
**Blocked** on the missing retail trace. No retail parity claim.

**Next selector queue boundary:** type76 has 102 weighted references but its
re-read case `0x4c` dispatches through `+0x300`, whose actual pointer is the
shared false stub `140001ba0` (XOR EAX / RET). That does not recover the original
server rule or authorize a CreatureState correction. The next native-backed
selector candidate is type86 Jump (67 weighted references), which needs its
own dispatcher/body/caller read. No type76/86 code changes are part of this slice.
