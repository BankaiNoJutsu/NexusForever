# Type 87: primary-subject Moving (2026-11-14)

Starting branch/head: `feature/spell-effects-remaining` / `bb388844`.
Question: does Moving inspect only the primary unit's state bit, or may the
relationship target, velocity flag or vector magnitude substitute for it?

Before edits: **Mapped native selector/bit**, live case `0x57` reads primary
`+0x1070 & 2`, fails a null primary and uses the shared boolean tail; Value and
ObjectId are unread. Source's player wrapper substitutes Target and its direct
unit path dereferences null. More significantly, the current predicate adds
Velocity bit1 and velocity/move vector length-squared >0.01 to Move bit2. Those
extra conditions are absent from the native body. Existing Source/tests/comments
cannot authorize that broader predicate. Record the discrepancy before changing it.

**Observed earlier reach:** 26 slots / 26 rows / 50 weighted spell references;
recheck census and anchors. Type 102 remains **Blocked** on the missing
attributable retail untimed-Fluff lifecycle trace, per user direction.

Candidate files: type-87 check, focused context/mask/vector tests, this finding,
coverage, ledger, handoff and spell tracker. Re-read the live switch/branch,
boolean tail and shipped type87 name; distinguish input-state testing from
physical motion and state production. Verification: matching native identity/
disassembly, shipped type/text hashes/readback and read-only census; new cases
red before production edit; owning Game build; related Prerequisite/Spell/
Cooldown/Movement tests; `git diff --check`. Static/headless only; no client
launch, Computer Use, runtime-data or deployment changes. No retail parity claim.

## Native selector, null guard and predicate re-derived by the lead

**Verified native selector/predicate/comparison:** LLVM 21.1.8 disassembly and
bounded PE reads against build 16042 SHA256
`231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5`.
Cached `1404a2100.fragment.c`, case `0x57` (lines 494–500), reads primary
`*param_2`. The actual switch entry at `1404a4848 + (87-1)*4` resolves to
**`1404a2894`**. Instructions `1404a2894..1404a28ae` load/test primary, branch
to the zero-result exit on null, read its `+0x1070` word, AND with **2**, and
jump to `1404a22ec`. Value, ObjectId and relationship unit are unread.

The physical neighboring block `1404a287a` masks4 and is not the indexed type87
entry. The switch bytes, not assumed instruction order or old handler names,
settle which branch applies. The actual type87 body has no velocity-bit test,
vector lookup or magnitude threshold.

The shared tail at `1404a22ec..1404a230a` reads comparison only. Equal selects
the nonzero masked value through `1404a305c`; NotEqual returns whether that value
is zero; all other modes retain the zero result. Source `CompareFlag` agrees
and is retained. Null primary fails before either flag comparison, including
NotEqual. No default zero movement state is invented for a missing subject.

Cache root: `exports/WildStar64.exe/selected_decompiled_cache/functions/`
`sha256_231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5/`.
Ignored `artifacts/prerequisite-type87/native-binding.json`, `native_binding.py`,
`selector-disassembly.txt` and `boolean-tail-disassembly.txt` retain the proof.
The old Source comment's `14049f310` / `+0x16a0` table attribution is superseded
by this inline-case read. No new function label/export refresh is needed.

## Independent shipped type-name witness

**Verified shipped row/text + native case:** the extracted client
`DB/PrerequisiteType.tbl` row87 points to text158173; `en-US.bin` resolves it to
**"Moving requirement not met"**. The existing owning GameTable/TextTable reader
probe was rerun for 87 and the separate row86/Jump control. Imported metadata
agrees, but actual text comes from the shipped payload, not the SQL metadata row.
This corroborates the Moving name, not a velocity-based definition or retail
movement timing.

Payloads retained from the preceding extraction are rechecked by SHA256:

- `DB/PrerequisiteType.tbl`:
  `9ddd928b6c423072a1e34d6797003297ab8f75bdbca6f906227ca557f5ea3a32`.
- `en-US.bin`:
  `588e385a0e8dee8b92dc79018070a17e78d3efd535b948224702effecafa5cf7`.

Raw files/reader wrapper stay ignored under `artifacts/prerequisite-type86/`;
current readback is `artifacts/prerequisite-type87/type-text-probe.log`. No new
binary-format reader or raw payload is committed.

## Source discrepancy, competing readings and caller boundary

**Rejected broader reading:** the current Source check treats Velocity bit1,
or either vector's squared magnitude >0.01, as sufficient without Move bit2.
That cannot reproduce the recovered native input predicate. Matching observed
physical displacement would not change which field this evaluator reads.
No new threshold or replacement velocity policy is introduced.

**Verified native input rule / Observed Source representation:**
`StateFlags.Move` is2 and `StateFlags.Velocity` is1. MovementManager.GetState
delegates to its current state command. Stored-state commands return their flags;
the real `StateDefaultCommand` derives Velocity from a nonzero velocity vector
and Move from a nonzero move vector. Those producers are unchanged. Consequently
a default command may still supply Move for a tiny move vector; the check consumes
the resulting bit instead of independently applying the old magnitude cutoff.
A default command with velocity only supplies no Move bit and does not meet.
This is Source representation/producer evidence, not retail physics qualification.

**Observed caller routing:** Spell caster/target prerequisites select explicit
subjects through `MeetsUnit`, retaining the other unit as relationship context.
The activation helper supplies player plus activated Target through `Meets`.
The wrapper must preserve the caller's subject. Native activation policy,
movement production, crowd-control filtering, interpolation and wire format are
outside this correction. A physical action's exact state transitions are not
derived from these input tests.

## Reach and anchors

**Observed read-only imported census:** all three prerequisite slots and all13
prerequisite columns of `spell4`/`spell4effects` reproduce **26 slots / 26 rows /
50 direct and weighted references**. Raw slot counts do not filter slots after
an earlier None; nested/non-spell consumers are outside scope. These are dependency
counts, not affected casts. Script/results stay ignored in the slice directory:
`census.py`, `ranking.json`, `slots.json`, `references.json`.

Caster-cast Spell4 **57746** and target-cast Spell4 **77165** both reference
prerequisite **11119**, type87 / NotEqual / Value0, with later slots empty. The
same row has both caller roles; tables do not define which unit the evaluator
selects. `anchors.tsv` retains the read-only query and type87 metadata row.

The 26 slots split into Equal18 / NotEqual8; 25 have Value0 and one Value9.
The native case ignores Value; no speed or magnitude meaning is assigned to9.

## Red regression evidence and correction

**Observed Source red run:** 26 new cases, **16 failed / ten passed / zero
skipped**, before production edits. Nine failures expose Target substitution,
null-unit dereference or required relationship parameters. Six raw-state/vector
cases and one real default-state-producer case expose the extra moving conditions.
Ten controls already pass. Output: `artifacts/prerequisite-type87/red.log`.

**Verified native rule / Source correction under verification:** the player
wrapper delegates with player; the direct path returns false on null; the
predicate tests only Move bit2. Velocity/vector alternatives and the invented
0.01 squared-magnitude cutoff are removed. Shared comparison, Value/ObjectId
handling and all movement producers are unchanged. The stale handler-table
address/offset comment is replaced by the direct live-case witness.

Four real-manager cases preserve primary/relationship separation and explicit
target-as-primary controls, two reject null-player substitution, two test direct
null-unit checks and one tests absent parameters. Twelve raw state/vector cases
distinguish bits1/2 and velocity/move magnitudes; two reject unsupported raw modes.
Three cases use the actual StateDefaultCommand to retain its existing vector-
to-flag derivation, including a tiny move vector that sets Move. The existing
18-case Moving flag theory now supplies contradictory relationship state through
both APIs without dropping or relaxing its Value/comparison expectations.
Tests qualify Source input behavior, not physical retail motion or native timing.

## Result: Implemented / Source

**Observed verification:**

- `dotnet test Source/NexusForever.Game.Tests/NexusForever.Game.Tests.csproj --no-restore -v minimal --nologo -p:UseSharedCompilation=false -m:1 --filter "FullyQualifiedName~Prerequisite|FullyQualifiedName~Spell|FullyQualifiedName~Cooldown|FullyQualifiedName~Movement"`
  passes **3,200 / zero failed / zero skipped**, including all 26 new cases.
  Red/green output stays in the ignored slice directory. Unchanged warnings
  remain in Aspire configuration, MySqlProviderQualificationTests (five CS8632)
  and ReceivedPropertyEffectTests (one xUnit2031); none is represented as fixed.
- Owning `dotnet build Source/NexusForever.Game/NexusForever.Game.csproj --no-restore -v minimal --nologo -p:UseSharedCompilation=false -m:1`
  passes with **zero warnings/errors** (`owning-build.log`).
- Native identity/indexed entry/bit2, shipped type/text readback and payload
  hashes, census, twelve coverage inputs, retained wrapper/null rule and note
  links pass `python artifacts/prerequisite-type87/verify_slice.py`.
  `git diff --check` passes. No new labels or raw payloads are committed.
- Full suite not repeated; its preceding non-green result remains in the
  handoff. No deployment, provider/socket/native-client acceptance or retail
  movement qualification.

End state: **implemented with Source verification; Verified native selector,
null/bit/comparison and independent shipped type-name witness**. Physical
motion/state transitions remain outside scope. Type 102 remains **Blocked** on
the missing retail trace. Next native-backed selector candidate is type149 State
(36 weighted references), requiring its own dispatcher/body/caller and writer
review; type76's shared stub still supplies no server rule. No retail parity claim.
