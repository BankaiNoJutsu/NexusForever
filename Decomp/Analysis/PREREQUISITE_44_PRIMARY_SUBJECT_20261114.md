# Type 44: retain the primary subject (2026-11-14)

Provenance: lead re-read of build-16042 native dispatcher/evaluator bodies and
original PE pointer reads, bound through REA to executable SHA256
`231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5`.
LLVM 21.1.8 disassembly corroborates the handler. Evidence tier: **Verified native
subject selection**; existing shared target-group attribute/recursion models keep
their earlier limits. No retail parity claim.

## Question and discrepancy, before editing

Does a relationship target replace the primary subject for type 44? The native
handler does not read it. `PrerequisiteCheckInTargetGroup.Meets` currently uses
`parameters.Target ?? player`, while `MeetsUnit` and spell apply/cast paths retain
explicit subjects. One existing test expressly encodes substitution. Correct
the player wrapper only if the dispatcher and handler establish the primary
unit; preserve the target-as-explicit-subject path and existing membership rules.

## Native witnesses re-read

* `1404a2100`, case `0x2c`, cached fragment lines 303-305, passes `*param_2`
  (primary), comparison and Value to manager vtable `+0x370`. The mechanically
  mapped manager table at `140b67740` has slot `140b67ab0 = 14049ee40` in the
  original executable. This is not the rejected handler-table-index reading.
* `14049ee40..14049eeae`: null primary or Value zero returns false for every
  comparison. The handler installs proxy vtable `140b66440` and stores that
  primary at proxy `+8` (`14049ee5a`). It uses Value to look up the group, then
  evaluates that group with this proxy through `1403b4a20`. It never reads a
  second unit. Equal is evaluator-result zero; NotEqual is its negation; other
  comparisons are false.
* Proxy lookup slot `140b66448 = 1403b4a10`; the thunk forwards the requested
  ID to `140240b40`. Its body dispatches descriptor `140a6d930` (original PE
  name-pointer read: **TargetGroup**) or registers TargetGroup before fallback.
  This disproves the old `Spell4ValidTargets_GetCriteriaById` table label.
  The shared evaluator's actual body at `1403b4a20` returns
  `0x59` for a missing row. Thus a nonzero unknown group is non-member; zero
  Value is separately rejected by the handler. The existing check already
  distinguishes these cases. A missing server table remains fail-closed.

The independent caller/data cross-check is target-owned rows: Spell4 35736
references prerequisite 35898 (Equal, Value 3741), and Spell4 42389 references
35899 (Equal, Value 1879), through `prerequisiteIdTargetCast`. The current
`Spell.CheckPrerequisites` explicitly supplies its target as primary for that
column and its caster as relationship context. Caster checks do the reverse.
Neither needs changing to implement this selector. `Spell.MeetsApplyPrerequisite`
also passes its supplied unit to the explicit-unit manager.

## Re-queried reference-data reach

Imported `wildstar_client` census: **214 slots in 201 prerequisite rows**;
**278 direct spell-column references / 281 slot-weighted references**:

| Column | Direct | Weighted |
|---|---:|---:|
| Spell4 target cast | 16 | 16 |
| Spell4 AOE target | 2 | 2 |
| Effects caster apply | 3 | 3 |
| Effects target apply | 257 | 260 |

These are dependency reach, not affected-cast counts: the explicit-unit spell
routes already select their subjects correctly. Group types by slot: 1 (146),
3 (16), 11 (48), 12 (3), 10 (1); all referenced groups resolve.

Correction to the old class comment: ObjectId is zero in **213 of 214**, not all
slots. Prerequisite 17518 slot 0 has ObjectId 17926 and Value 10363. The native
handler ignores ObjectId, so that observation changes no operand semantics.

## Scope and verification plan

Candidate files: the type-44 player wrapper, existing InTargetGroup tests,
native labels/coverage, ledger and handoff. Run contradictory player/relationship
membership cases through both real manager entry points and spell apply, plus
explicit target-as-subject controls. Test null primary at the owning handler
(the generic player manager requires a non-null player), and absent relationship
context. Run new cases red before production changes. Rewrite the superseded
target-substitution test, preserving its creature membership controls.

Do not modify shared group matching, missing-table policy, recursion limits,
non-unit prerequisite-row bypass, or activation caller policy without their own
evidence. `ClientActivateUnitCastHandler` supplies player and relationship target
separately; its native caller policy was not re-derived here. Source consistency
of this wrapper is not qualification of every producer using it.

## Implementation and verification

The player wrapper now delegates with `player`; the explicit-unit handler,
shared evaluator and spell implementation are unchanged. The existing
`ThePlayerEntryPointEvaluatesTheTargetWhenOneIsSupplied` test is rewritten as
`ThePlayerEntryPointRetainsThePlayerAndAllowsExplicitTargetEvaluation`, preserving
the explicit-creature membership control. Sixteen new cases cover contradictory
primary/target state through the real player manager, explicit-unit manager and
spell apply; explicit target-as-subject controls; null primary at the owning
handler; and absent relationship parameters. All 16 ran before the production
change: **14 failed / two passed**. No tests were weakened.

Rebuilt Prerequisite/Spell gate:
`dotnet test Source/NexusForever.Game.Tests/NexusForever.Game.Tests.csproj --no-restore -v minimal --nologo -p:UseSharedCompilation=false -m:1 --filter "FullyQualifiedName~Prerequisite|FullyQualifiedName~Spell"`:
**2,934 passed, zero failed/skipped**. Owning Game build with `--no-restore -v minimal --nologo -p:UseSharedCompilation=false -m:1` passes with zero warnings/errors.
Test compilation retains five CS8632 warnings in the unmodified MySQL test file.

Three native labels applied and re-exported in a read-only Ghidra 12.1.4 session:
`Prerequisite_CheckInTargetGroup_LiveCase44`, `ClientDB_GetTargetGroup` and
`EntityCriteria_GetRelatedCriteriaThunk` (three applied; zero missing/duplicates).
The lookup/thunk evidence inventory records are corrected and one handler record
added; both focused coverage rows name the live type-44 body. The shared
evaluator's stale Spell4ValidTargets table description is corrected to
TargetGroup without changing its label, Mapped status or attribute-model limits.
`Validate-DecompileMappingArtifacts.ps1`: **8,332 rows / seven files pass**.

Native outputs are under ignored `artifacts/prerequisite-primary-subject/`:
`type44-native.txt`, `type44-labelled.txt` and matching run logs. Reproduce with
`Gap8Decomp.java` on `14049ee40` / `140240b40` and LLVM disassembly of
`14049ee40..14049eeae` / `1403b4a10..1403b4a16`. Original PE slots and descriptor
addresses are listed above. No decompiled client source is committed. REA's
binary session was closed; no native client execution, GUI or deployment.

Full-suite Source verification (`dotnet test` on the same project with
`--no-build --no-restore -v minimal --nologo`, TRX logger): **15,100 passed /
15 failed / two skipped / 15,117 total**. All **40 type-44 cases passed**.
Thirteen failures reported EF `ManyServiceProvidersCreatedWarning`; the other
two were assertions. The ten failing methods span challenge reward, database
retry/bootstrap, friendship ownership, guild rank identity and quest reward
tests. All ten passed separate-process reruns covering **66 cases**. Isolation
does not establish the cause or make the full suite green; no provider warning
or assertion was suppressed. Outputs: `type44-full.log`,
`type44-full/type44-full.trx`, `type44-full/failed-methods.json`,
`type44-full/isolated-results.json` and `type44-full/isolated-*.log`, all beneath
the ignored artifact directory above. Focused red/green and owning build logs
are `type44-red.log`, `type44-green.log` and `type44-owning-build.log`.

End state: **Implemented / Source**, supported by Verified native selector
evidence; no retail parity claim. Existing group-model, non-unit row and
activation-caller qualifications remain separate.
