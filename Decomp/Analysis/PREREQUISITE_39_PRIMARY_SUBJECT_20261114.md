# Type 39: primary-subject player identity (2026-11-14)

Starting branch/head: `feature/spell-effects-remaining` / `d15cedbb`.
Question: does IsPlayer inspect the primary unit or the relationship target?

Before edits: **Mapped native selector**, evaluator case `0x27` tests the primary
unit's `+0x80` against `0x14` then uses the boolean comparison tail. Source's
player wrapper substitutes Target; its explicit-unit path retains the subject.
The flag test currently treats that substitution as expected behavior. Recheck
the actual kind-field binding and caller contract before correcting the wrapper.
No general entity-kind/model change is proposed from this selector question.

**Observed reference census:** the next highest-reach remaining substituting
wrapper after type 28: 200 slots / 200 rows / 1,140 direct and weighted
spell-column references. Count method and read-only imported-reference snapshot
are retained under `artifacts/prerequisite-type28/`; these are dependencies,
not affected casts. Type 102 remains **Blocked** on the missing retail trace.

Candidate files: type-39 check, focused context/flag/entity tests, this note,
ledger, handoff and spell tracker. Verification: native dispatch and kind-field
binding, reference anchors, focused red cases before production edit, owning
Game build, Prerequisite/Spell/Cooldown checks, `git diff --check`. Static/headless;
no client launch, Computer Use, runtime data or deployment. No retail parity claim.

## Native and Source witnesses re-derived by the lead

**Verified native selector/kind:** LLVM 21.1.8 disassembly and direct PE reads
against build 16042 SHA256
`231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5`.

- Cached evaluator `1404a2100.fragment.c`, case `0x27`, lines 281–288, reads
  `*param_2` and tests primary `+0x80 == 0x14`; it does not read `param_2[1]`.
  The actual switch entry at `1404a4848 + (39-1)*4` resolves to `1404a22d4`.
  Disassembly `1404a22d4..1404a230a` confirms null fails and the boolean tail
  returns the flag for Equal, its negation for NotEqual, otherwise false.
  Value/ObjectId are not consulted.
- The actual Lua registration at `140c5a7b0` names `GetType`, points at
  `140649e20`, and belongs to method table `140c5a770`. That function indexes
  its string array with unit `+0x80` at `14064a002`. Index 20 is stack
  `rsp+0xc0 == rbp-0x40`; `140649f47..140649f52` assigns string `1409f45ec`,
  whose bytes read `Player`. This independently corroborates the kind value,
  without relying on an old function label.
- Cache root: `exports/WildStar64.exe/selected_decompiled_cache/functions/`
  `sha256_231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5/`.
  Local ignored evidence: `artifacts/prerequisite-type39/native-binding.json`
  and `native_binding.py`; the shared dispatcher/GetType disassembly is under
  `artifacts/prerequisite-type28/`. No new label or raw-code export is committed.

**Observed Source model/callers:** concrete `Player.Type` is `EntityType.Player`
(20), while `GhostEntity` is a separate `WorldEntity` with kind 23, not an
`IPlayer`. This slice preserves the existing `subject is IPlayer` test rather
than expanding entity-model behavior. Spell caster/target prerequisites already
select their explicit subjects through `MeetsUnit`; the activation helper passes
player plus relationship Target. Native activation-caller policy remains outside
this correction, as in the preceding type-28 finding.

**Rejected reading:** substituting the relationship Target for IsPlayer does not
match the native case. Null-primary NotEqual must also fail rather than negating
the missing subject into success. Delegate the player wrapper to its existing
null-guarded `MeetsUnit` path. Rewrite the flag test's player expectation to use
the primary player while retaining all non-player explicit-unit flag checks.
Six new context cases cover contradictory kinds through the real manager APIs,
null primary with matching relationship targets and absent relationship parameters.

**Observed imported anchors:** Spell4 54070's caster-cast prerequisite 1050
contains type 39 / Equal / Value 0; Spell4 30161's target-cast prerequisite 960
contains type 39 / NotEqual / Value 0. Both occupy slot 0 with later slots empty.
The two caller roles coexist; data does not authorize replacing either caller's
chosen subject. Read-only query output is `artifacts/prerequisite-type39/anchors.tsv`.

**Observed red run:** the unchanged production check fails all six new context
cases. The two existing explicit-NPC cases also selected by the IsPlayer filter
pass: **six failed / two passed / zero skipped**, eight total. This establishes
the selector/null discrepancies in Source; it is not a retail execution witness.
The flag test's explicit-unit cases remain intact, while its player assertion
now follows the primary player. `IsPlayer_ComparesTargetEntityKind` is renamed
and given a non-player relationship control rather than checking player against
itself. The explicit-unit comparison cases and expected results are retained.

## Result: Implemented / Source

**Implemented / Source:** only the player wrapper now delegates with player to
the existing explicit-unit method. This also uses that method's null-primary
guard for both Equal and NotEqual. Relationship context is preserved; entity
models, kind tests, boolean comparison and callers remain otherwise unchanged.

**Observed verification:**

- `dotnet test Source/NexusForever.Game.Tests/NexusForever.Game.Tests.csproj --no-restore -v minimal --nologo -p:UseSharedCompilation=false -m:1 --filter "FullyQualifiedName~Prerequisite|FullyQualifiedName~Spell|FullyQualifiedName~Cooldown"`
  passes **3,026 / zero failed / zero skipped**, including all six new cases.
  Red/green logs remain under `artifacts/prerequisite-type39/`.
- Owning `dotnet build Source/NexusForever.Game/NexusForever.Game.csproj --no-restore -v minimal --nologo -p:UseSharedCompilation=false -m:1`
  passes with **zero warnings/errors**, retained in `owning-build.log`.
- The related test build retains the unchanged Aspire configuration warning,
  five CS8632 warnings in MySqlProviderQualificationTests and one xUnit2031
  warning in ReceivedPropertyEffectTests. No warning fix is claimed.
- Native switch/name/field bindings reproduce, and `git diff --check` passes.
  No full-suite repeat; the preceding non-green full result remains recorded.
  No provider/socket, native execution, deployment or retail acceptance claim.

End state: **implemented with Source verification; Verified native selector**.
Type 102 remains Blocked on the missing retail trace. The next raw-reach
substituting wrapper is type 40 (Health), 623 direct / 705 weighted references,
whose selector and caller still require their own review. No retail parity claim.
