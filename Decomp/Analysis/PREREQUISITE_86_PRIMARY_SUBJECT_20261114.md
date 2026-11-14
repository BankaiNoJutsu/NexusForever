# Type 86: primary-subject Jump (2026-11-14)

Starting branch/head: `feature/spell-effects-remaining` / `9366e4f4`.
Question: does Jump inspect the primary unit, which native flag does it read,
and does the current Source wrapper preserve that contract?

Before edits: **Mapped native selector/flag**, evaluator case `0x56` tests primary
unit `+0x1070 & 0x40`, with a null-primary failure and the shared boolean tail.
Source's player wrapper substitutes Target; explicit-unit checks retain their
subject but dereference a null entity. Existing comparison semantics ignore
Value/ObjectId and use only Equal/NotEqual. The XML comment still cites a
superseded handler-table address/offset; it is not evidence for the live case.

**Observed earlier reach:** 22 slots / 22 rows / 67 weighted spell references;
recheck the census and caster/target anchors. Type 102 remains **Blocked** on the
missing attributable retail untimed-Fluff trace, per user direction.

Candidate files: type-86 check, focused context/flag tests, this finding,
coverage, ledger, handoff and spell tracker. Re-read live switch/branch and
movement-state field/bit witnesses before correction. Preserve distinction
between Jump, Fall and DoubleJump; do not infer physical input/timing policy.
Verification: matching native identity/disassembly and flag binding, read-only
reference census; new cases red before production edit; owning Game build;
Prerequisite/Spell/Cooldown/Movement tests; `git diff --check`. Static/headless;
no client launch, Computer Use, runtime data or deployment. No retail parity claim.

## Native selector, flag and comparison re-derived by the lead

**Verified native selector/null/bit/comparison:** LLVM 21.1.8 disassembly and
bounded PE reads against build 16042 SHA256
`231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5`.
Cached `1404a2100.fragment.c`, case `0x56` (lines 487–493), reads primary
`*param_2`; the actual switch entry at `1404a4848 + (86-1)*4` resolves to
`1404a2860`. Instructions `1404a2860..1404a287a` load/test primary, branch to
the zero-result exit when null, load its `+0x1070` word, mask with `0x40` and
enter shared tail `1404a22ec`. The relationship unit, Value and ObjectId are
unread. Unlike type12, this native case has its own null-primary branch.

The tail at `1404a22ec..1404a230a` reads only comparison. Equal selects the
masked value at `1404a305c`, so any nonzero `0x40` result meets; NotEqual returns
whether the masked value is zero; every other mode retains the zero result.
The existing Source `CompareFlag` implements those boolean results. No change
to Value interpretation or shared comparison is supported or needed.

Raw masks other than `0x40` cannot make this predicate true by themselves.
Source `StateFlags.Jump` is `0x40`, Fall `0x8`, DoubleJump `0x800`, Move `0x2`.
The tests distinguish these numeric inputs; they do not establish that a physical
double jump omits or includes Jump at any particular moment.

Cache root: `exports/WildStar64.exe/selected_decompiled_cache/functions/`
`sha256_231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5/`.
Ignored `artifacts/prerequisite-type86/native-binding.json` and `native_binding.py`
retain binary/switch checks; `selector-disassembly.txt` and
`boolean-tail-disassembly.txt` retain the reads. The old Source comment's
`14049f190` / `+0x16a0` handler-table attribution is removed in favor of this
direct inline-case witness. No new function label or export refresh is needed.

## Independent shipped type-name witness

**Observed imported metadata:** `prerequisitetype` row86 points to error text
158172. The imported `localizedtext` row has metadata only, so it cannot supply
the actual string. The existing ArchiveProbe extracted `DB/PrerequisiteType.tbl`
from `ClientData.index` and `en-US.bin` from `ClientDataEN.index` in the installed
client. A temporary wrapper uses the owning `GameTable<PrerequisiteTypeEntry>`
and `TextTable` readers, not a new binary-format reader.

**Verified shipped row/text + native case:** the extracted row86 independently
points to 158172, whose English entry is **"Jump requirement not met"**.
Row87/text158173 is "Moving requirement not met", a separate adjacent identity.
This corroborates the type86 Jump name, not movement timing/physics or the old
function label. Extracted payload SHA256:

- `DB/PrerequisiteType.tbl`:
  `9ddd928b6c423072a1e34d6797003297ab8f75bdbca6f906227ca557f5ea3a32`.
- `en-US.bin`:
  `588e385a0e8dee8b92dc79018070a17e78d3efd535b948224702effecafa5cf7`.

Local files/probe/output remain ignored under the slice directory:
`tables/`, `localization/`, `TextProbe/`, `type-text-probe.log`. Only this record
and the [raw-mask coverage](coverage/prerequisite_86_masks_20261114.csv) are tracked.

## Reach, anchors and Source caller contract

**Observed read-only reference census:** all three prerequisite slots and all
13 prerequisite columns of `spell4` / `spell4effects` reproduce **22 slots /
22 prerequisite rows / 67 direct and weighted references**. Raw slot counts do
not apply an early-empty-slot reach filter; nested/non-spell consumers are
outside scope. These are dependency counts, not affected casts. The census
script/results are ignored in `census.py`, `ranking.json`, `slots.json` and
`references.json` under the slice directory.

The 22 slots split evenly between Equal/NotEqual (11 each); 21 have Value0,
one Value27801. The latter is not given a new meaning: the native branch ignores
Value. Source tests use uint-max operands as additional ignored-field controls.

Caster-cast Spell4 **71497** points to prerequisite **30369** (Jump / Equal /
Value0 in an OR row with type15). Target-cast Spell4 **63436** points to **12848**
(Jump / NotEqual / Value0, later slots empty). Both caller roles exist; tables
do not define selector semantics themselves. `anchors.tsv` retains the query.

**Observed Source routing:** `Spell.CheckPrerequisites` chooses caster/target
through `MeetsUnit`, with the opposite unit as relationship context. The
activation helper supplies player plus activated Target through `Meets`.
`MovementManager.GetState` delegates to its state-command group; the current
`StateCommand` returns its stored flags and publishes them through 32-bit
`SetStateCommand`. This slice does not alter state production, crowd-control
filtering, interpolation, wire format or native activation-caller policy.

## Regression and correction

**Observed Source red run:** 19 new cases, **nine failed / ten passed / zero
skipped**, before production edits. Four real-manager cases expose opposing
primary/relationship states; two expose null-player substitution, two null-unit
dereferences and one required relationship parameters. Eight raw-bit controls
and two unsupported comparison-mode controls already pass.

**Verified native rule / Source correction under verification:** delegate the
player wrapper with player, and fail a null entity before reading its movement
state. Keep the exact Jump mask and comparison. The former three-case
`Jump_ComparesJumpStateFlag` test becomes
`Jump_ComparesPrimaryPlayerJumpStateFlag`, with a Jump primary and a relationship
containing Fall/DoubleJump without Jump; all comparison/Value expectations remain.
New tests retain explicit target-as-primary controls and relationship identity,
and use uint-max Value/ObjectId to confirm those operands are ignored. Source
tests verify the recovered input contract, not original-server movement behavior.

## Result: Implemented / Source

**Observed verification:**

- `dotnet test Source/NexusForever.Game.Tests/NexusForever.Game.Tests.csproj --no-restore -v minimal --nologo -p:UseSharedCompilation=false -m:1 --filter "FullyQualifiedName~Prerequisite|FullyQualifiedName~Spell|FullyQualifiedName~Cooldown|FullyQualifiedName~Movement"`
  passes **3,174 / zero failed / zero skipped**, including all 19 new cases.
  This adds Movement to the earlier related filter; totals are not added across
  those different scopes. Red/green output stays in the ignored slice directory.
- Owning `dotnet build Source/NexusForever.Game/NexusForever.Game.csproj --no-restore -v minimal --nologo -p:UseSharedCompilation=false -m:1`
  passes with **zero warnings/errors** (`owning-build.log`). The test build
  retains unchanged Aspire configuration, five CS8632 warnings in
  MySqlProviderQualificationTests and one xUnit2031 in ReceivedPropertyEffectTests.
- Native identity/switch/mask, both shipped payload hashes, extracted type/text
  identity, census, eight coverage masks and note links pass
  `python artifacts/prerequisite-type86/verify_slice.py`. `git diff --check`
  passes. No generated/native payloads or temporary probe code are committed.
- Full suite not repeated; its preceding non-green result remains in the
  handoff. No runtime deployment, provider/socket/native-client acceptance or
  retail movement qualification.

End state: **implemented with Source verification; Verified native selector,
null/bit/comparison and independent shipped type-name witness**. Physical Jump/
DoubleJump transition timing remains outside scope. Type 102 remains **Blocked**
on the missing retail trace. Next native-backed selector candidate: type87
Moving (50 weighted references), with its own subject/body/caller review.
Type76's shared native stub still supplies no server rule. No retail parity claim.
