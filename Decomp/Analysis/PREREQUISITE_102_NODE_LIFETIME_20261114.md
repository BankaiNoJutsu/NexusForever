# Type 102: native list node ID and lifetime (2026-11-14)

Provenance: lead re-derived build-16042 native bodies with Ghidra 12.1.4,
LLVM 21.1.8 original-byte disassembly and original PE pointer/descriptor reads.
REA bound the executable to SHA256
`231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5`.
Evidence tier: **Verified native ID, unit ownership and registration lifecycle**;
Source implementation/authority boundaries are distinguished below.
No retail parity claim. Static/headless only.

## Question and discrepancy, recorded before implementation

What does the +0x30 virtual of a node in primary unit +0x15d0 return, and when
does that node enter/leave the list? The earlier pass established only the
primary selector. Competing ID readings were Spell4Effects row ID, Spell4 ID and
cooldown/instance ID. Lifetime readings were registration, a finite timer, or
parent spell unfinished state. A concrete constructor/accessor plus named table
loader and list writers/removers separates them; table correlation alone does not.

Before editing, `HasActiveSpellEffectId` scanned the unit's pending castings,
then every target's effects. It required a concrete Spell, !IsFinished, matching
Entry.Id, !LifetimeEnded and a lifetimeEvents entry. This can attribute a
received effect to its caster and omit it from the actual target; a removable
flag-2 effect without a finite timer is excluded. These discrepancies were
recorded before production edits. Candidate ownership was unit/Spell prerequisite
support, lifecycle tests, native labels/inventory and ledger/handoff.

Reference reach, [previous read-only census](PREREQUISITE_102_PRIMARY_SUBJECT_20261114.md):
**353 slots / 303 prerequisite rows / 302 direct spell columns / 352 weighted
references**, not affected casts. These counts are unchanged.

## ID binding (Verified native)

* Type 102 case 0x66 calls `1404a4fe0`; the loop reads primary +0x15d0, follows
  node +0x30 and compares node virtual +0x30 with ObjectId.
* Constructor `1405d65a0` installs base vtable `140b6e240`. Original PE slot
  `140b6e270` (+0x30) is **1405da910**. Nineteen related vtable slots use this
  same accessor. It returns the first DWORD of node +0xb0's record, zero if
  absent. The neighboring +0x38 accessor `1405da930` returns record +0x10
  (sentinel 152 if absent); +0x48 `1405da950` returns record +0x38.
* Initializer `1405d6880` stores `1404e1c50`'s result at node +0xb0. That lookup
  rejects ID zero, checks a local/controlled runtime override tree +0x7d38,
  then falls back to `1407a1080`: service +0x578 ID map.
* `14055e180` at `140560651..14056068d` constructs 0xa8-byte records with
  `140567950` and inserts their pointers into that same service +0x578 map,
  keyed by the input row ID. `140567950` obtains the shipped row from
  `140238780` and copies its ID to runtime +0, effect type to +0x10, duration
  to +0x38 and flags to +0x3c. The getter dispatches descriptor `140a6d268`;
  its original PE name pointer is `140ae1598`, UTF-16 **Spell4Effects**.

Thus the concrete base element's virtual +0x30 returns its runtime
**Spell4Effects row ID**, not its server-unique instance ID, parent Spell4 ID or
SpellCoolDown ID. Override-tree record writers were not re-derived; no new
server dynamic override or unresolved-ID fallback is invented. The +0x38
SpellEffectType and +0x48 duration bindings support the record interpretation;
this pass does not change prerequisite types 104/105.

## Unit ownership and lifetime (Verified native)

* `140717990` creates a wrapper-local target record, writes target entity ID
  to +0x40, links that wrapper-local record to target +0x15c8, and sends its
  inline variant records to `140718120`.
* `140718120` resolves the runtime effect and selects a node subclass using
  runtime effect type +0x10. It calls the base constructor and initializer
  directly or through a subclass initializer. It is not a direct execution
  of arbitrary retired-server effects.
* `1405d6880` stores the parent wrapper at node +0x50, caster ID at +0x58 and
  wrapper-local target ID at +0x5c. Decompiler-dropped arguments were checked
  in original disassembly: `1405d6992..1405d69da` resolves the caster and links
  node +0x38/+0x40 to caster +0x15d8; `1405d69de..1405d6a19` resolves the target
  and links node +0x28/+0x30 to **target +0x15d0**. Registration occurs before
  immediate virtual +0x10 application and before scheduling the finite timer.
* A finite window at node +0xbc is calculated from payload timing and virtual
  +0x48 duration, with a payload override. Raw payload timing field meanings
  are not all named here. Runtime flags +0x3c bit **0x2** makes the window -1
  and suppresses the timer at node +0xc0. It **does not suppress registration**.
* Timer callback `140252318` jumps to node virtual +0x18, base `1405d6b30`.
  That invokes the parent's virtual +8 (meaning not assigned here), unlinks
  the parent's active-child +8/+0x10 chain, and dispatches node +0x40.
  It does **not** unlink target +0x28/+0x30.
  Therefore type 102 tests membership, not a remaining-time predicate.
* Destructor `1405d6680` unlinks the parent chains, target +0x28/+0x30 and
  caster +0x38/+0x40 and clears both event records. `1407177b0` destroys every
  child when the wrapper-local parent is destroyed. Parent-removal paths
  include `14053e1b0` and unit teardown `1404550d0`.
* `140718af0` (0x0819 prune/refresh) is guarded by parent spell shape 10;
  it destroys flag-2 active children, keeps finite children, and destroys the
  wrapper-local parent when no active child remains. Do not generalize that
  branch into a universal server removal rule.

The native list can contain a delayed or untimed registered node. Timer expiry
and target-list destruction are separate transitions; client presentation/frame
ordering is not an authoritative-server timing specification. No change is
licensed to instantiate every instantaneous row, invent a delay, or replay the
client's presentation nodes as server gameplay execution.

## Independent reference anchors (reference-data tier)

Fresh imported-row query:

| Effect ID | Parent Spell4 | Type | Delay | Duration | Flags |
|---:|---:|---:|---:|---:|---:|
| 44148 | 26793 | 17 | 200 | 6000 | 0 |
| 45049 | 27141 | 71 | 0 | 5000 | 0 |
| 44081 | 26804 | 37 | 0 | 0 | 2 |
| 65808 | 33730 | 17 | 0 | 0 | 2 |
| 200070 | 63120 | 17 | 0 | 7000 | 0 |

Of the 353 prerequisite slots, 266 resolve to 226 distinct shipped effect IDs;
87 slots/64 distinct IDs do not resolve in this imported table. Among resolved
slots: 64 flag-2, 21 delayed and 189 positive-duration without flag 2. These
categories overlap; they are not counts of affected or implemented casts.
The incomplete imported ID coverage does not change the native accessor binding
or justify fallback to another ID space.

## Implementation/verification

The proposed general registration model is **not implemented**. Native timer
expiry does not directly unlink target membership; the 0x0819 path is guarded
by parent shape. Transplanting client registration into a generic authoritative
server registry would invent admission/retirement rules for unsupported and
untimed effects. Exploratory Source probes expose received-effect and flag-2
gaps, but do not qualify that model. They remain ignored diagnostics, not a
replacement for native evidence or committed acceptance tests.

The supported correction is narrower: when the existing pending-spell lookup
examines effects, it must restrict them to the queried target object. It may
not report another target's effect on the caster. Five focused Source tests
exercise self/other rows in both target orders, positive self controls, row ID
versus instance/parent ID, existing finite retirement and reused-guid object
identity. All **five failed with assertion failures before production changes**.
No pre-existing test expectation was rewritten. Receiving-unit discovery,
untimed registration, exact delayed presence and expiry-to-destruction timing
remain explicit gates. Existing parameter interpreters, timer admission,
effect execution and world-object policy are unchanged.

`Spell.HasPersistentEffectWithId` now receives the queried unit and excludes
target records belonging to a different object before checking the row ID and
existing lifetime-event predicate. `UnitEntity` supplies `this`. This removes
false positives while preserving existing positive self-effect behavior. It
does not make the lookup complete for effects received from other casters.

Rebuilt Source gate:
`dotnet test Source/NexusForever.Game.Tests/NexusForever.Game.Tests.csproj --no-restore -v minimal --nologo -p:UseSharedCompilation=false -m:1 --filter "FullyQualifiedName~Prerequisite|FullyQualifiedName~Spell|FullyQualifiedName~Cooldown"`:
**3,001 passed / zero failed or skipped**. Owning Game build using
`--no-restore -v minimal --nologo -p:UseSharedCompilation=false -m:1`:
**zero warnings/errors**. Existing ASPIRE010 and five CS8632 warnings remain in
test/dependency compilation. Mapping validation: **8,341 rows / seven files**.
Full suite on the same rebuilt project, using
`--no-build --no-restore -v minimal --nologo --logger "trx;LogFileName=full.trx" --results-directory artifacts/prerequisite-type102-lifetime/full`:
**15,106 passed / 28 failed / two skipped / 15,136 total**. All 23 type-102
check cases and all five new ownership cases passed. Twenty-six failures
reported EF `ManyServiceProvidersCreatedWarning`; the other two were
`Assert.Equal` in the challenge reward executor and `Assert.Empty` in auction
bid actor enrollment. All **16 failed methods pass separate-process reruns,
covering 61 cases**. Isolation does not establish the cause or make the full
suite green. No assertion/provider warning was suppressed or changed.

End state: **Implemented / Source** for the bounded ownership correction;
**Verified native** for the ID and registration/retirement flow. Next gate is
authoritative received/untimed-effect producer and retirement coverage, rather
than treating client membership as a timer predicate. No retail parity claim.

## Reproduction

Ignored artifacts under `artifacts/prerequisite-type102-lifetime/`:
`list-offset-scan.txt`, `list-head-family-scan.txt`, `unit-list-consumers.txt`,
`node-accessors.txt`, `id-vtable-refs.csv`, `node-vtable-writers.txt`,
`element-create-destroy.txt`, `node-registration.txt`, `lookup-and-parent-cleanup.txt`,
`runtime-map-builder.txt`, `lifetime-pruning.txt`, `accessors-disassembly.txt`,
`node-lifecycle-disassembly.txt`,
`pe-bindings.txt`, `reference-anchors.tsv`, `focused-labels.csv`, `labelled.txt`
and matching run logs. `labelled-final.txt` / `labelled-final-run.log` contain
the final ten-label application/export. Source outputs: `subject-red.log`,
`green.log`, `owning-build.log`, `mapping-validation.log`, `full.log`,
`full/full.trx`, `full/summary.json`, `full/failed-methods.json`,
`full/isolated-results.json` and `full/isolated-*.log`. Exploratory design probes
are preserved in ignored `registration-design-probes.cs`; those unsupported
expectations are not committed tests. An initial missing test using and missing
base-row fixture were corrected before the final five-case red baseline.
Use `Gap8Decomp.java` with addresses above; use
`Gap8InsScan.java` to find +0x15d0 and neighboring list heads;
`FindPointerReferences.java` with 1405da910 and `Gap8Callers.java` for the
constructor/vtable/retirement chain. LLVM disassembly resolves the initializer's
unit-ID arguments and the timer thunk. Original PE pointer reads corroborate
vtable slots and the named table descriptor.

Nine new labels plus the refreshed selector label applied/re-exported:
**ten applied, three accessor functions created, zero missing/duplicates**.
REA session closed. No decompiled client source is committed. Exact native
presentation timing, dynamic override writers, unrepresented effects, native
acceptance and retail qualification remain separate gates. No deployment,
native client execution or Computer Use.
