# Type 40: primary-subject health percentage (2026-11-14)

Starting branch/head: `feature/spell-effects-remaining` / `85b11fbe`.
Question: does type 40 compare the primary unit's health percentage or substitute
its relationship target, and does the current percentage calculation match the
native body?

Before edits: **Mapped native selector** in evaluator case `0x28` passes the
primary unit to `1404a4d90`; the earlier finding records a float health percentage.
Source's player wrapper substitutes Target, while the explicit-unit path uses
the supplied subject. Existing `Health_ComparesHealthPercentOnTarget` encodes
the substitution. Re-read the evaluator/helper, numeric operations and named
health accessors before correcting behavior. Type 102 stays **Blocked** on the
missing retail untimed-Fluff lifecycle trace, per user direction.

**Observed earlier reference census:** 406 slots / 338 rows / 623 direct /
705 weighted spell-column references. Recheck census plus caster/target anchors;
these measure dependencies, not affected casts or native activation policy.

Candidate files: type-40 check, context/health tests, this finding, ledger,
handoff and spell tracker. Verification: matching binary hash and native
disassembly/registration/constant reads; reference census; new tests red before
production edits; owning Game build; Prerequisite/Spell/Cooldown tests;
`git diff --check`. Static/headless only; no native client execution, Computer
Use or runtime data changes. No retail parity claim.

## Additional discrepancy recorded before correction

**Mapped native comparison discrepancy:** `1404a2010` uses UCOMISS with only
JNE for Equal and JE for NotEqual, without testing the parity flag. This differs
from the existing `CompareFloat` method/comment for an unordered operand: a
non-null unit with zero health/max health produces NaN, so native Equal appears
true and NotEqual false, unlike managed IEEE comparisons. Confirm instruction
flags and actual switch targets before changing this edge. The earlier health
note's IEEE claim is superseded only where re-derived here. This is separate
from null-primary failure, which precedes the arithmetic in the native body.

Do not change every `CompareFloat` consumer from this one recovered call site.
Candidate extension: a named native-helper comparison used by type 40; keep the
existing IEEE method for callers whose native comparison sequence is not re-read.
Verify zero/zero, nonzero/zero, fractional percentages and unsigned conversion
boundaries. Record the competing managed-IEEE and native-flag readings, rather
than inserting a max-health clamp or a guessed zero-denominator default.

## Native evidence re-derived by the lead

**Verified native selector and calculation:** installed build 16042 SHA256
`231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5`,
read with LLVM 21.1.8 and a bounded PE registration/constant/switch reader.

- Cached `1404a2100.fragment.c`, case `0x28` (lines 290–292), passes primary
  `*param_2`, comparison at slot `+4`, and Value at `+0xc`. The actual switch
  entry resolves to `1404a2367`; its `1404a236f` instruction loads primary into
  RDX before calling `1404a4d90` at `1404a2372`. The relationship unit is unread.
  The cached helper call has a misleading first argument; the register read
  establishes the consumed parameters instead of trusting that rendering.
- `1404a4d90..1404a4e29` fails a null primary first. It obtains current health
  through virtual `+0x40`, zero-extends the result, converts to float, multiplies
  by the verified **100.0f** at `140b7a0dc`, obtains maximum through virtual
  `+0x48`, zero-extends/converts it, then divides with DIVSS. Value is independently
  zero-extended from R9D and converted to float. No integer percentage truncation,
  double-precision intermediate, clamp or denominator check appears.
- Independent native UI binding: actual method-table entries `140c5a810` and
  `140c5a820` name `GetHealth` / `GetMaxHealth` and point to `14064a4e0` /
  `14064a6d0`. Their bodies call virtual `+0x40` at `14064a54b` and `+0x48` at
  `14064a745`. UI-only availability/type filters are not added to the evaluator.

**Verified native comparison plus ISA semantics:** the six actual switch arms
of `1404a2010` resolve to `1404a202d`, `1404a2038`, `1404a2043`, `1404a204c`,
`1404a2055`, `1404a205e`, for modes 1–6 respectively. The first two use UCOMISS
then JNE / JE; the latter four use COMISS then SETA / SETAE, with operands
reversed for less/less-equal. The decompiler's C `==` / `!=` rendering loses the
unordered distinction; the disassembly supplies the relevant flags.

The [Intel SDM volume 2B](https://www.intel.com/content/dam/www/public/us/en/documents/manuals/64-ia-32-architectures-software-developer-vol-2b-manual.pdf)
(253667-060US, September 2016, UCOMISS, printed 4-685 / PDF page 687) defines
unordered ZF/PF/CF all set. Thus, when comparison is reached with masked floating
exceptions, unordered Equal returns true, NotEqual false, and all four ordering
modes false. The native code has no PF rejection for Equal. Invalid comparison
modes fail. Positive infinity keeps ordinary ordered results. No claim is made
about the client's configured exception masks, or retail reachability of zero
maximum health. The Source correction models the recovered result, not FP traps.

**Rejected prior reading:** managed IEEE `NaN == x` false / `NaN != x` true does
not model this helper's first two branches. The helper name remains correct; no
new native label is needed. [Comparison coverage](coverage/prerequisite_40_native_comparison_20261114.csv)
records the eight tested raw modes and addresses. Existing other IEEE consumers
and percentage-helper native-parity comments need separate caller/body review;
their behavior is not qualified or changed by this type-40 read.

Local ignored evidence: `artifacts/prerequisite-type40/` contains native-binding
JSON/script and selector, calculation, float-comparison and two Lua disassemblies.
Cache root: `exports/WildStar64.exe/selected_decompiled_cache/functions/`
`sha256_231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5/`.
Raw native source/binary data are not committed.

## Reference data and Source caller boundaries

**Observed reference census:** fresh read-only imported `wildstar_client` query
reproduces 406 type slots / 338 distinct prerequisite rows / 623 direct and
705 slot-weighted spell-column references across all 13 prerequisite columns of
`spell4` and `spell4effects`. Counts include raw slots, without an empty-slot
reach filter; nested/non-spell consumers are outside scope. They are not broken
cast counts. `census.py`, `ranking.json`, `slots.json`, `references.json` remain
under the ignored slice directory.

**Observed anchors:** Spell4 32732 caster-cast prerequisite 9647 contains type40,
GreaterThanOrEqual 80; Spell4 8481 target-cast prerequisite 965 contains type40,
LessThanOrEqual 25. Both have later slots empty and ObjectId zero. The data
corroborates separate caller roles but cannot establish selector semantics by
itself. Read-only anchor output: `anchors.tsv`.

**Observed Source routing:** `Spell.CheckPrerequisites` selects Caster or explicit
target through `MeetsUnit` and supplies the opposite unit as relationship context.
The activation helper supplies player plus activated Target through `Meets`.
`PrerequisiteManager` dispatches these through different check entry points;
the wrapper's Target substitution causes disagreement for the same primary.
Native activation-caller selection remains outside this correction.

## Correction and red regression evidence

**Observed Source red run:** 31 new cases, **nine failed / 22 passed / zero
skipped**, before the production edit. Seven failures expose subject substitution,
null-primary substitution or required relationship parameters. Two expose
unordered Equal/NotEqual on zero health/maximum. The existing finite fractional,
unsigned-conversion and infinity behavior already passes; those operations are
retained. Raw red output: `artifacts/prerequisite-type40/red.log`.

**Verified native rule / Source correction under verification:** the player
wrapper delegates to `MeetsUnit(player, ...)`; Target remains relationship
context. The percentage formula is unchanged. `CompareNativeFloat` represents
the recovered `1404a2010` flag rule, and only type 40 uses it. Existing managed
`CompareFloat` behavior stays unchanged; its false claim of exact native-helper
IEEE correspondence is removed from the comment. This avoids treating other
unreviewed callers as newly verified. No guessed zero-denominator default is
introduced: zero/zero remains NaN and positive/zero remains infinity.

`Health_ComparesHealthPercentOnTarget` becomes
`Health_ComparesHealthPercentOnPrimaryPlayer`, with opposite player/target
percentages and all seven comparison cases retained. The new context cases use
both real manager APIs, a 50%-health primary and 90%-health target, explicit
target-as-primary controls, null primary and absent parameters. Numeric cases
cover all raw modes 0–7 for zero/zero and positive/zero, plus fractional and
unsigned-boundary percentages. Source tests check these recovered contracts;
they do not establish a retail trace or exception-mask configuration.

## Result: Implemented / Source

**Observed verification:**

- `dotnet test Source/NexusForever.Game.Tests/NexusForever.Game.Tests.csproj --no-restore -v minimal --nologo -p:UseSharedCompilation=false -m:1 --filter "FullyQualifiedName~Prerequisite|FullyQualifiedName~Spell|FullyQualifiedName~Cooldown"`
  passes **3,057 / zero failed / zero skipped**, including all 31 new cases.
  `green.log` retains the output. Test-build warnings remain in unchanged Aspire
  configuration, MySqlProviderQualificationTests (five CS8632) and
  ReceivedPropertyEffectTests (one xUnit2031); none is represented as fixed.
- Owning `dotnet build Source/NexusForever.Game/NexusForever.Game.csproj --no-restore -v minimal --nologo -p:UseSharedCompilation=false -m:1`
  passes with **zero warnings/errors** (`owning-build.log`).
- Native hash/switch/constant/Lua-name bindings, fresh census, all eight CSV
  comparison modes and note links check with
  `python artifacts/prerequisite-type40/verify_slice.py`. `git diff --check`
  passes. No native-label changes or decompiler export refresh is needed.
- Full suite not repeated for this bounded correction; its preceding non-green
  result remains in the handoff. No runtime deployment, provider/socket,
  native client execution or retail acceptance qualification.

End state: **implemented with Source verification; Verified native selector,
arithmetic and instruction-flag result**. Other float/percentage paths retain
their earlier implementations and require separate native caller/NaN review;
tests of those earlier readings are not retail evidence. Type 102 remains
**Blocked** on the missing original-server trace. Next selector candidate is
type 12 (364 weighted spell references), not a reopened type-102 search.
No retail parity claim.
