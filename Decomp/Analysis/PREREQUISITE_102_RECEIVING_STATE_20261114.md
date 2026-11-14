# Type 102: receiving-unit discovery and untimed state (2026-11-14)

Starting branch/head: feature/spell-effects-remaining / 983f8934. Lead re-read
of build-16042 native receive/remove bodies with Ghidra 12.1.4 and the original
executable bound through REA to SHA256
231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5.
Evidence tier: Verified native target-list identity and flag-2 lifetime;
Source authoritative state admission/removal is examined independently.
No retail parity claim. Static/headless; no native execution or GUI.

## Question and discrepancies, before edits

How does a receiving unit discover its effects without walking the caster's
pending castings, and what installs/retires an untimed instance? Current lookup
only walks its own pending castings. The preceding target filter removed false
positives but did not create a receiving-unit path. A finite lifetimeEvents entry
cannot define registration: the native flag-2 node is registered without it.

Candidate first slice: property modifiers, conversions and personal-healing
modifiers already retain EffectEntryId in their receiving unit's SpellPropertyState.
All three handlers pass info.Entry.Id to the existing accepted state. Rejected
handlers install nothing. The state's existing exact-instance, whole-spell,
dispel and force-removal paths provide retirement without a new timer/registry.
Query that authoritative identity and prevent stale pending-spell fallback from
resurrecting removed property effects. Tests must run red first and cover actual
handlers, receiver/caster isolation, rejected shapes, explicit removal, stale
expiry and existing finite timing. Do not alter property admission/stacking/timers.

Candidate second slice: native Fluff uses the generic effect node but its Source
handler is empty. A typed no-gameplay marker may be justified only after the
native receive/untimed/removal chain and Source removal ownership are recovered.
No guessed generic registry or permanent instantaneous marker is authorized by
this note.

## Reach (current read-only imported census)

Type 102: 353 slots / 303 rows / 302 direct spell columns / 352 weighted
references (prior full column census; dependency reach, not affected casts).
Current join: 266 slots resolve to 226 shipped effect rows. Fluff (17): 197
slots / 164 IDs, 49 flag-2 slots; UnitPropertyModifier (11): 11 slots / 11 IDs,
one flag-2 slot. Other effect kinds retain their own evidence gates. Counts
and native/source verification will be recorded per completed slice.

## Property-state slice (Implemented / Source)

The receiving unit's accepted `SpellPropertyState.EffectEntryId` is now queried
before the legacy pending-spell fallback. `HandleEffectPropertyModifier`,
`HandleEffectUnitPropertyConversion` and `HandleEffectPersonalDmgHealMod` already
write the effect row ID on that receiver. The pending-spell helper excludes
these three producer families so an old timer cannot resurrect explicitly
removed state. No admission, interpreter, stacking or timer rule changed.

Thirteen new Source cases exercise actual producers, receiver/caster/reused-guid
isolation, finite and untimed state, exact removal, finite expiry, personal
replacement ownership, whole-spell removal and rejected shapes. Final red before
production: **11 failed / two passed**. An initial negative fixture changed its
row after the effect's interpretation had been cached; reconstructing that
fixture before dispatch corrected the test setup before the final red run.
No existing expectation was rewritten.

Rebuilt Prerequisite/Spell/Cooldown gate: **3,014 passed / zero failed/skipped**
with `dotnet test Source/NexusForever.Game.Tests/NexusForever.Game.Tests.csproj
--no-restore -v minimal --nologo -p:UseSharedCompilation=false -m:1
--filter "FullyQualifiedName~Prerequisite|FullyQualifiedName~Spell|FullyQualifiedName~Cooldown"`.
Owning Game build with `--no-restore -v minimal --nologo
-p:UseSharedCompilation=false -m:1`: **zero warnings/errors**. Final full-suite
follow-through is recorded below; no retail parity claim.

## Native receive/removal follow-through

Original dispatcher slots at `1403f1344 + (opcode - 0x61) * 4` resolve 0x07f4
to `1403ee1c9`, 0x0813 to `1403ee349`, 0x0818 to `1403ee403`, and 0x0819 to
`1403ee3af`. The known 0x0818/0x0819 cases corroborate the mechanical table read.
0x07f4 goes through wrapper-id lookup to `14053e5a0`, which resolves each target
record's UnitId and calls `14053d680`. That function finds/creates a wrapper-local
record for that receiving entity, then `140717990` / `1405d6880` attach children
to the receiver's +0x15d0 list. The receiver need not own the casting.

0x0813 reads wrapper ID first and receiver ID second, then calls `140717df0`
with current count minus one. The shared two-UInt32 reader `14007a040` confirms
the wire order. This matches Source `ServerSpellBuffRemove` (0x0813); no packet
layout change is needed. It is distinct from 0x0819's reversed field roles and
shape-10 prune. `140717df0` changes the displayed buff count, and
`1404694a0` / `140717fa0` release displayed buff records. Those bodies alone do
not prove effect-node destruction; the initial count-zero destruction reading
is rejected pending the later cleanup path. No untimed marker is implemented
from that reading.

Ignored outputs under `artifacts/prerequisite-type102-receiving/` include
`native-receive-remove.txt`, `receive-message.txt`, `count-retirement.txt`,
`remove-empty-and-refresh.txt`, `remove-reader.txt`, `opcode-dispatch-slots.txt`,
`native-go.txt`, `native-0813.txt`, `native-0818-0819.txt`,
`reference-kind-census.tsv`, `property-red-final.log`, `property-green.log`,
`property-build.log` and matching headless logs. Native element binding and
flag-2 timer suppression remain as in the prior node-lifetime note; remaining
retirement and presentation-to-authoritative-state gates are explicit.

## Removal boundaries recovered after c6eee3bc (Verified native)

The original PE slot for **0x0811** is `1403f3204`, resolving dispatcher
`1403ee3a0` -> `1405a5aa0`. Its input structure contains wrapper ID at +0,
count at +4, and a pointer to receiving-unit IDs at +8. For each listed ID it
finds the wrapper-local target node by +0x40, calls `1407177b0`, and frees it.
The target-node destructor destroys its effect children, whose destructors
unlink the receiving-unit +0x15d0 list. This is target removal, not target-data
replay. It is not guarded by the shape-10 rule of 0x0819. The Source model's
legacy `CasterId` list holds receiving-unit IDs. Comments are corrected; the
name, wire layout and non-emitted status are preserved. Native reader and
registration factory are not recovered in this pass; the Source counted-UInt32
writer is **Correlated**, not independently verified as a complete wire schema.

The original **0x07fe** slot at `1403f31b8` resolves `1403ee438`. Original
instructions at `1403ee45b..1403ee468` pass stop reason 8 to `14053e1b0`.
That body unconditionally destroys every wrapper-local target node at +0x48,
including untimed children; it has no flag-2 exception for that destruction.
The dispatcher then calls service cleanup `140561ad0`. Source `SendSpellFinish`
writes its CastingId as the first UInt32, matching the consumer's wrapper lookup.
The original server's decision about **when** to emit Finish is not recovered
by reading what the client does when it receives it.

This also bounds the property slice: it queries already accepted **Source**
state, which can outlive the Source parent casting. It does not reconcile that
server state with the native target-list teardown caused by Source Finish.
Receiver discovery is corrected within the current property model; its parent
retention, transport and original-server lifetime are not newly qualified.

**0x0819** has the opposite field order to 0x0813: receiving-unit ID first,
wrapper ID second. `140718af0` runs only when the wrapper runtime record's
+0x70 record has +0x18 equal to **10**. It destroys flag-2 children from the
active list, keeps finite children and recomputes their remaining window. If
the active list is empty it destroys the target node. The runtime field is
kept as an unnamed shape here; the Source enum's value 10 alone does not bind
that native field to CastMethod. Do not emit this as universal untimed removal.

`1404550d0` also destroys the departing receiving entity's wrapper-local
target nodes and detaches its native lists. It is an entity destructor, not
an automatic count-zero expiry tick. `140717df0`, `1404694a0` and `140717fa0`
remain display-count/record operations. The **Rejected** interpretation is
"0x0813 count zero itself destroys every effect child"; no such destruction
occurs in those bodies. Do not replace 0x0813 with 0x0819 or reverse its fields.

The wrapper constructor `14053c500` installs vtable `140b6c608`; its +8 slot
is `1402f61d0`. Original instructions show a read of wrapper +0x130 and return.
The accepted-start consumer `1405a51a0` uses the returned ID for spell cooldown
lookup. The calls to this slot in the receive/remove paths are not cleanup
callbacks. No label is added for a guessed state-changing meaning.

## Untimed Fluff decision and separating witnesses

**Verified native:** the default Fluff node follows the same received variant,
effect-row lookup, unit registration and destruction chain. Flag 2 registers
without a finite timer. Native membership ends with node destruction, not an
arbitrary timer predicate or displayed buff count. Reach remains 197 resolved
type-102 slots / 164 Fluff effect IDs; 49 slots use flag 2. These are dependency
counts, not observed casts.

**Source:** `HandleEffectFluff` is empty. `Spell.Update` finishes an executing
spell when its event queue empties (apart from the existing CSI wait), and
`Finish` sends 0x07fe. There is no accepted receiver-owned Fluff instance or
retirement handle. A new marker cannot borrow the property states' already
accepted ownership/removal model without adding a new producer contract.

Competing candidates and their limits:

| Candidate | Evidence for | Evidence against / missing witness |
| --- | --- | --- |
| Register an untimed receiver marker until explicit removal | Native flag-2 registration and explicit 0x0811/0x07fe destruction | Keeping it beyond Source Finish disagrees with client teardown; keeping the parent alive changes server Finish policy without its witness. |
| Remove an untimed marker when buff count reaches zero | Existing Source buff-removal emission | Rejected as a native child-destruction rule by 140717df0/1404694a0/140717fa0. |
| Use 0x0819 for all untimed removals | Native flag-2 prune | Rejected universally: native shape-10 gate, finite-child retention, and different field order. |
| Define presence by Source pending timers | Existing finite fallback | Rejected as a complete native model: flag-2 nodes have no finite timer and receivers need not own the casting. |

**End state: mapped only / Blocked** for adding Fluff state. The remaining
input is a server-authority lifetime/transport witness: one known flag-2 Fluff
cast followed through application, target removal and Finish (including a
multi-target survivor), or an independently recovered server producer that
establishes the same ordering. A retail packet capture or original-server body
would separate these policies. The current static client bodies establish the
consumer transitions, not that missing server policy. Native execution/GUI was
not authorized by this pass and was not used. Do not invent permanent
instantaneous markers, new packet emissions, or a generic timer registry.

Separately **Mapped native / Source discrepancy**: variant reader `140094890`
places the TimeRemaining UInt32 at +0x0c; `1405d6880` uses a nonzero value to
override its duration window before flag-2 suppression. Source `SendSpellGo`
currently writes -1 for every effect. Whether/how finite server rows should
use zero or an elapsed remaining value needs its own producer/timing witness;
no wire value is changed here.

## Focused label/provenance verification

Three new labels and three corrected comments were applied and re-exported
in a read-only Ghidra session: **six applied, zero created/missing/duplicates**.
Function evidence records state Verified native consumer semantics. Four opcode
provenance rows are Mapped, with unrecovered registration/reader and original
server policy fields left explicit. Mapping validation: **8,344 rows / seven
files**. No decompiled source is committed. Packet changes in this slice are
comments only, so no new behavior test or build is required for them.

Additional ignored reproduction outputs: `finish-callers.txt`,
`entity-cleanup.txt`, `wrapper-class.txt`, `wrapper-class-refs.txt`,
`wrapper-owner-lifecycle.txt`, `native-wrapper-start.txt`, `focused-labels.csv`,
`labelled-removal.txt`, `labelled-removal.log`, `mapping-validation.log` and
matching headless logs. Use the existing `Gap8Callers.java`, `Gap8Decomp.java`
and `ApplyNexusForeverLabels.java`; original PE table reads and LLVM disassembly
corroborate dispatch. The read-only Ghidra project changes were discarded.
No retail parity claim.

## Final Source full-suite follow-through

On the same rebuilt project, `dotnet test
Source/NexusForever.Game.Tests/NexusForever.Game.Tests.csproj --no-restore
--no-build -v minimal --nologo --logger "trx;LogFileName=receiving-full.trx"
--results-directory artifacts/prerequisite-type102-receiving/full-suite`:
**15,130 passed / 17 failed / two skipped / 15,149 total**, duration 6m12s.
All **23** `PrerequisiteCheckActiveEffectIdTests`, **five**
`ActiveEffectSubjectTests`, and **13** new `ReceivedPropertyEffectTests` passed
in that full run. Sixteen failures reported EF
`ManyServiceProvidersCreatedWarning`; the other was an `Assert.Equal` failure
in the challenge reward executor. All **nine failed methods pass separate-process
reruns covering 19 cases** with exact `FullyQualifiedName` filters and
`--no-build --no-restore`. No assertion/provider warning was suppressed or
changed. Their cause is not established; the full suite is not green.

Ignored records: `full-suite.log`, `full-suite/receiving-full.trx`,
`full-suite/summary.json`, `full-suite/failed-methods.json`,
`full-suite/isolated-results.json`, `full-suite/isolated-*.log` and `.trx`;
the local `isolate_failures.py` drives one process per failed method.
Final `git diff --check` passes. Source verification is not native acceptance
or retail qualification. REA is closed. No retail parity claim.
