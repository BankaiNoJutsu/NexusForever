# Prerequisite player entry points must retain their primary subject (2026-11-14)

Provenance: lead re-read of cached Ghidra exports for build 16042, bound to the
installed executable by REA `open_binary` on 2026-11-14 (SHA256
`231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5`).
Evidence tier: **Verified native subject selection**; the existing active-spell,
stack-count and group-membership models retain their earlier inferred limits.
No retail parity claim. Static pass; no native client execution.

## Question and discrepancy recorded before editing

Does the relationship target replace the primary subject for prerequisite types
38, 59 and 129? Native evidence says no. Their `MeetsUnit` implementations retain
the supplied subject, but their player `Meets` entry points use
`parameters.Target ?? player`. Consequently the two public manager entry points
can answer opposite results for the same player, prerequisite and relationship
context. Existing tests explicitly encode the target substitution for 38 and 59;
they are implementation history, not a witness for it.

Type 15 (5,757 slot-weighted spell references in the earlier audit) was checked
first: its player entry point already retains the player, and `1404a4ec0` reads
the supplied unit. No change is proposed there. Of the simple target-substituting
player wrappers identified in this pass, 38, 59 and 129 have the greatest spell
reach. Remaining wrappers require their own native selector and caller review.

## Native witnesses re-read

All fragment paths below are relative to
`exports/WildStar64.exe/selected_decompiled_cache/functions/sha256_231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5/`.

* `1404a2100.fragment.c`, case `0x26`, lines 278-280: the dispatcher passes
  `*param_2` (primary unit), comparison and Value to vtable `+0x350`.
  `14049ecc0.fragment.c` reads that unit's creature record at `+0x18`, or id zero
  without one. It never substitutes the second unit. Its cached function name
  is stale; this read uses the dispatcher and body, not that name.
* `1404a2100.fragment.c`, case `0x3b`, lines 359-362: `1404a4f60` receives
  `*param_2`, comparison, ObjectId and Value. Its cached body walks the supplied
  unit's `+0x15c8` list. The cached decompile drops later arguments and the
  accumulator; this pass verifies the **subject only**, not the stack formula.
* `1404a2100.fragment.c`, case `0x81`, lines 712-736: null primary fails;
  otherwise the body walks `*param_2 + 0x15c8` and asks the group manager about
  each active spell and ObjectId. The second unit is never read in this case.

The independent shipped-data cross-check is the coexistence of caster and target
prerequisite columns referencing these types. Selection belongs to the caller:
the same row can be evaluated on either unit. Examples: type 38 caster rows on
Spell4 32510 / prerequisite 9552 and 32513 / 9553; type 59 on Spell4 30165 /
7783; type 129 mount Spell4 2823 / 16018 (NotEqual group 343). Table data alone
does not prove selector semantics; the native dispatcher supplies that proof.

## Reach (read-only imported reference DB census)

`wildstar_client.prerequisite` and every `prerequisiteId*` column of `spell4`
and `spell4effects`, re-queried 2026-11-14:

| Type | Slots | Distinct prerequisite rows | Direct column references | Slot-weighted references |
|---:|---:|---:|---:|---:|
| 38 | 1,182 | 919 | 1,122 | 1,703 |
| 59 | 909 | 824 | 949 | 1,004 |
| 129 | 146 | 136 | 792 | 830 |

A direct reference is a populated table-row/column pointing at a prerequisite
containing the type; a weighted reference counts each matching slot in that
prerequisite. These are dependency reach, **not counts of broken casts**. Most
spell paths already use `MeetsUnit`, so these numbers cannot quantify the impact
of the player-wrapper correction. Nested prerequisites and non-spell consumers
are outside this census.

## Candidate change and verification

Change only the three player wrappers to delegate with `player`. Keep Target as
relationship context. `IPrerequisiteCheck` documents this distinction; the real
`PrerequisiteManager.Meets` and `MeetsUnit` dispatch through separate wrappers.
Spell cast/apply/persistence paths already choose their subject explicitly.
`ClientActivateUnitCastHandler.MeetsActivateSpellPrerequisite` supplies the player
and an activation target separately; this correction preserves the caller's
player as subject. Native activation-caller policy is not qualified here.

Regression scope: contradictory primary/target state through both real manager
entry points, both boolean comparisons, null primary with a populated target,
and explicit target-as-subject controls. Rewrite the tests that encoded
target substitution. Run the new cases red before editing production code.

Commands: owning `NexusForever.Game.csproj` build; Game.Tests filtered to
`UnitPrerequisiteContextTests`, `PrerequisiteCheckActiveSpellGroupTests`,
`PrerequisiteCheckSpellStackCountTests`, `PrerequisiteCheckTests`; then the full
Game.Tests suite after the coherent slice.

## Result: Implemented / Source

* Baseline owning Game build: zero warnings/errors.
* New 18-case red run, unchanged production: 15 failed, 3 passed.
* First focused run: 371 passed. This omitted
  `CreatureItemChallengePrerequisiteTests`, which also encoded target substitution.
* Initial full run (`dotnet test ... --no-build --no-restore`): 15,044 passed,
  19 failed, 2 skipped, 15,065 total. Two failures were the additional obsolete
  type-38 player expectation; it was rewritten to assert the primary player's
  id zero while retaining the explicit-creature controls. The other rewritten
  tests are `PrerequisiteCheckTests.IsCreature_ComparesTheExplicitSubjectCreatureId`
  and `PrerequisiteCheckSpellStackCountTests.PlayerChecksRetainThePlayerWhenARelationshipTargetIsSupplied`.
* All nine failing methods outside this change passed in separate processes
  (40 cases across those isolated reruns). These were AccountPendingItemDurability
  provider retry, CharacterChallengeRewardExecutor provider retry, Tradeskill
  recipe publication, Friendship account-command retry, Guild rank provider-model
  caching, AuctionCancellation original-actor return, and the three Contract
  completion retry methods. Failures in the full process included EF's
  `ManyServiceProvidersCreatedWarning` and timing/assertion failures. Isolation
  demonstrates non-reproduction in that scope, not their root cause or baseline
  equivalence. Raw full TRX and per-method results remain under ignored
  `artifacts/prerequisite-primary-subject/`.
* Final rebuild and widened gate:
  `dotnet test Source/NexusForever.Game.Tests/NexusForever.Game.Tests.csproj --no-restore -v minimal --nologo -p:UseSharedCompilation=false -m:1 --filter "FullyQualifiedName~Prerequisite|FullyQualifiedName~Spell"`:
  **2,882 passed, zero failed/skipped**. This builds the owning Game project and
  test dependencies. Five CS8632 warnings arise in the unmodified
  `MySqlProviderQualificationTests.cs`. The full suite was not repeated after
  the test correction; it is not reported green.
* `git diff --check` passes. No native launch, GUI, runtime deployment or schema
  change. REA's read-only binary session was closed.

No new spell-effect semantics, stack model or group model are authorized by this
finding. Broader target-substituting wrappers remain a review queue, not a claim
that every wrapper is wrong. Existing inferred models are not promoted by these
tests. No retail parity claim.

## Next native question (mapped only)

Type 179 is the next candidate by raw spell dependency reach: 41 slots in 39
prerequisite rows, 504 direct column references / 517 slot-weighted references.
LLVM 21.1.8 disassembly of the same binary at `14049eeb0..14049eefc`, correlated
with evaluator case `0xb3` / vtable `+0x378`, confirms a primary-unit selector:
null fails; no creature record at unit `+0x18` supplies zero; otherwise unit
virtual `+0x10` supplies the compared number; ObjectId is the integer comparison
operand. The player wrapper substitutes Target; its helper rejects missing
difficulty, while the spell's pure-179 shortcut accepts missing difficulty.
These are three different default paths. Record them before changing any one.

The missing input for a unified correction is the return meaning of unit virtual
`+0x10`, and the caller policy for non-unit world objects currently accepted by
`Spell.TryEvaluateCreatureDifficultyPrerequisite`. A native read can still recover
the former; it is **pending research**, not declared unrecoverable. The
`Lua_Unit_GetDifficulty` entry at `140654f40` was also disassembled; it reads a
resolved record's `+8` field through `1403dec00`, not virtual `+0x10`, so it does
not yet supply that mapping. No type-179 code was changed in this slice.
