# Type 102: Fluff admission and Finish ordering (2026-11-14)

Starting head: `2193ecbd`, branch `feature/spell-effects-remaining`.
Question: which Fluff rows does the native client admit, and can available
evidence establish the server's ordering of application, target removal and
Finish without inventing a lifetime policy?

Before edits: **Source** `HandleEffectFluff` is empty; `SendSpellGo` sends
accepted effects with remaining value -1; `Spell.Update` sends Finish when its
event queue empties. **Verified native** Finish destroys all target nodes;
0x0811 destroys listed target nodes; 0x0813 only establishes display-count
removal; 0x0819 has a shape-10 gate. The preceding receiving-property lookup
correction does not qualify parent retention or native transport ordering.
Fluff reach from the preceding imported census: 197 slots / 164 effect IDs,
49 flag-2 slots; dependency reach, not observed casts.

Investigate the runtime-effect admission gate, default-node virtual methods,
runtime parent-field binding and available capture/addon/video evidence. A
second client path can refine consumer semantics but cannot itself prove
original-server packet scheduling. Candidate edits are scoped native labels,
provenance, this finding and trackers; a Source behavior edit requires an
independent separating witness and a failing regression first. Verification:
focused label application/re-export and mapping validation; owning build and
focused Spell/Prerequisite tests if behavior changes. Static/headless only;
no native execution or Computer Use. No retail parity claim.

## Provenance and witnesses

Lead re-derived build-16042 bodies with Ghidra 12.1.4, LLVM 21.1.8 and original
PE pointer reads. REA bound executable SHA256
`231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5`.
Existing `Tools/ArchiveProbe` extracted only `DB/Spell4Base.tbl` and
`DB/SpellEffectType.tbl` from the installed `ClientData.index`, into ignored
artifacts. Their SHA256 values are respectively
`34e3faeeee7c4b9889b808a36ec69b40218ebe013cbf95e9e26fc6440005c93a` and
`ad4f2b7322ca64eabd692402e0b762aead48b84cded36e3a56f9385c549a4570`.
The imported reference join was checked against these shipped inputs, not
treated as a field-meaning witness by itself.

## Fluff client admission (Verified native)

`140567950` zeroes a runtime effect record, looks up the effect's
`SpellEffectType` through `14023e0c0`, and copies the effect only when the
type record's flags at +4 have **mask 1** set. Original PE descriptor
`140a6d700` names SpellEffectType. Shipped type ID **17** has flags **27**;
the imported row agrees. Fluff therefore passes this gate. These are **type
flags**, distinct from each effect row's flags copied to runtime +0x3c.

For an existing wrapper and resolved receiving unit, `140718120` resolves the
received effect ID and chooses a node implementation from runtime effect type.
Type 17 takes the default base-node branch, constructor `1405d65a0`, followed
by initializer `1405d6880`. The factory has no Fluff DataBits admission
predicate. The initializer registers the node on the receiver's +0x15d0 list
before dispatching its apply virtual or creating its finite timer. This is
client admission **given a received variant**, not proof that a server should
emit every table row or bypass its own prerequisites.

Original PE vtable `140b6e240` has +0x10 and +0x40 both pointing to
`140001b70`; original instructions show that function simply returns. Thus
Fluff's base apply and expiry-hook virtuals do not supply a hidden predicate
or destroy its target membership. Flag-2 rows register without a finite timer;
zero-duration non-flag-2 rows also reach registration and the timer path.
There is no native basis for interpreting every zero-duration row as permanent.
Other base virtuals and presentation work are not newly qualified by this read.

## CastMethod binding and reach (Verified native / reference data)

Runtime builder `140568540` resolves Spell4 then Spell4Base and copies raw
Spell4Base **+0x50** to its runtime execution record **+0x18**, reached through
the runtime spell's **+0x70** pointer. Original instructions at
`1405687fb..1405687fe` corroborate that copy. The shipped Spell4Base field
descriptors name column 18 **castMethod** (UInt32), after the icon string.
The raw +0x50 values agree with the imported CastMethod on all **196**
resolvable prerequisite slots, with **zero mismatches**.

Two stale lookup labels were caught on this path. `140236e00` resolves named
descriptor `140a6d118` / **Spell4Base**, not Spell4CastResult. `1402369c0`
resolves `140a6d0e0` / **Spell4AoeTargetConstraints**, not Spell4Base. Original
PE UTF16 descriptor pointers corroborate both corrections. No AOE evaluator
or cast-result behavior is changed.

Consequently `140718af0` / 0x0819's guard is now bound to **CastMethod 10**
(named Aura by Source). The preceding unnamed-shape gate is superseded by
this field binding. It remains a conditional prune of flag-2 active children,
preserving finite children, rather than a universal receiver removal.

Read-only join results, reproduced by
`coverage/prerequisite_102_fluff_admission_20261114.sql`:

| Shape | Type-102 slots | Distinct Fluff effect rows |
| --- | ---: | ---: |
| Flag 2, CastMethod 10 | 20 | 16 |
| Flag 2, CastMethod 0 | 28 | 25 |
| Flag 2, CastMethod 8 | 1 | 1 |
| Non-flag-2, positive duration | 145 | 119 |
| Non-flag-2, zero duration | 3 | 3 |
| Total | 197 | 164 |

One positive-duration slot, effect **36886** / parent **23202**, has no joined
Spell4 parent, so its CastMethod is unknown. It is included in the 145 and
not silently dropped. That join failure does not prove that a client cannot
receive the effect under another wrapper. The 49 flag-2 slots cover 42 effect
rows; 48 have delay/tick/duration zero, and one has delay 1 with tick/duration
zero. Effect **65808** / spell **33730** / base **19711** is a CastMethod-0
flag-2 anchor. Effect **44148** / spell **26793** is a finite 6,000 ms row with
200 ms delay. These are dependency counts, not cast observations or proof of
original-server admission.

## Wire contracts and Finish boundary (Verified native)

Direct registration block `14006c290` binds:

| Opcode | Native reader | Structure / wire shape |
| --- | --- | --- |
| 0x07fe | `14007ab50` | 4-byte structure; one UInt32 wrapper ID |
| 0x0811 | `14007a550` | 0x10-byte structure; UInt32 wrapper ID, UInt32 count, count UInt32 receiver IDs |
| 0x0813 | `14007a040` | 8-byte structure; UInt32 wrapper ID then receiver ID |
| 0x0819 | `14007a040` | 8-byte structure; UInt32 receiver ID then wrapper ID |

For 0x0811, `14007a550` reads the first two 32-bit fields, allocates count*4
bytes, and reads that data through `140337160`. Consumer `1405a5aa0` interprets
the entries as receiver IDs and destroys matching target nodes and all their
children. Source `ServerSpellCastTargetList.Write` independently matches that
wire shape. Its earlier native-reader and registration blockers are closed;
the legacy property name and non-emitted status are preserved. Source 0x0813
and 0x0819 field order is already correct. None is replaced with another.

The prior original PE opcode-to-dispatch bindings remain valid. Finish calls
`14053e1b0` with reason 8 to destroy all target nodes, then `140561ad0`.
That service helper is **not just a SprintStateUpdated event raiser**: it
conditionally raises that event, unregisters the wrapper when appropriate,
then invokes its deleting destructor. Its automatic string-anchor label was
misleading and is corrected. Base deleting destructor `14053c8a0` calls
`14053c8e0` and frees the allocation when requested.

`140561780` distinguishes zero-ID local prediction from received nonzero-ID
wrappers: zero-ID wrappers can enter service list +0x7c8; nonzero received IDs
enter the service +0x7a0 map. `140561c30` resolves that map. Cleanup removes
the registered ID from the same map before deleting the wrapper. Therefore,
for this registered nonzero-ID path, a subsequent 0x07f4 or 0x0818 cannot
re-admit effects through the retired ID without fresh wrapper registration.
Do not generalize that map branch to zero-ID prediction.

Initializer `14053d1f0` copies the received ID to wrapper +0x5c and stores the
runtime spell at +0x138; original instructions corroborate the decompiler's
otherwise dropped copy-helper arguments (destination wrapper +0x68). It queues
callback `140546e60`. **Mapped follow-through:** that callback processes route
presentation records and reschedules itself; no direct stop/destructor call
was found in its body. Its transitive helpers are not all re-derived here.
That callback is not evidence for copying Source's empty-event-queue Finish
rule into the native model.

This proves a **client constraint**: any received state meant to remain under
that wrapper must precede its Finish and cannot survive its destruction. It
does not establish when the original server emitted Finish, whether it used
replacement wrappers, or which row/target admissions it accepted.

## Other available evidence and its limits

**Reference data:** Jabbithole resolves spell 63120 to Suppression Wave. The
local retail-video index returns a 2015-10-13
[Slavemaster Drokk guide](https://www.youtube.com/watch?v=w3kbhbj9CcA), with
caption leads around 130-136 seconds. This is an **Observed lead**, not a
verified identification of effect 200070 or any untimed effect. The video
search also returned buff cancellation/removal discussion; captions and buff
presentation expose neither effect-row membership nor raw Finish ordering.
No timing or server-state conclusion is promoted from those leads.

The recorded local addon archive directory is absent; aggregate audit reports
still record BuffAdded/BuffRemoved use but contain no packet sequence. A bounded
primary-source check of the retail-era
[BuffFilter source](https://github.com/kaporten/BuffFilter/blob/975b270f0c7c3e3f7b4830ee2ba35fbdef17ef02/BuffFilter.lua#L201)
(file blob `404008282fc2061ea4187a864dba8c08a93ba3ea`, last file commit
2016-11-27) finds a tooltip/UI-buff hook and a BuffAdded callback that schedules
filter updates for player/target/focus units. **Correlated UI surface only**:
it does not expose Fluff effect IDs or record Go, target removal or Finish.
No addon timer is treated as a native membership witness.

A filename inventory of the current checkout, assessment checkout/report and
installed client covered **314,590 files**, finding no `.pcap`, `.pcapng`,
`.pkt`, `.sniff`, `.wspp` or `spell-evidence` path. This is a bounded inventory,
not a claim that all historic captures are unavailable. Public searches for
WildStar SpellFinish/sniff and Fluff/SpellGo capture evidence yielded no
applicable packet trace. Unrelated-game results and emulator implementations
are not witnesses. Existing Source runtime logs cannot establish original-server
policy. Native execution or a new local emulator capture would also not, by
itself, establish that missing original-server ordering.

## Decision and verification

**Verified native:** conditional received-Fluff admission; the named CastMethod
gate; four removal wire contracts; registered-wrapper teardown and late-admission
boundary. **Mapped only / Blocked server policy:** do not add Fluff state or
keep Source parents alive yet. Source's empty-queue Finish still conflicts with
leaving an untimed receiver marker alive beyond it. Native no-op application
does not imply a permanent server state, and native read support does not imply
an original-server emission rule. No gameplay, timer, wire layout or emission
behavior changes in this slice; packet edits are explanatory comments only.

The next separating witness is a retail/original-server trace for an identified
flag-2 Fluff row, beginning with Start/Go and following target removal and
Finish, including a multi-target survivor or independently recovered server
producer. The trace must show wrapper IDs, effect-row IDs, receiver IDs and
ordering; display-count zero alone is insufficient. Both parent-retention and
replacement-wrapper models remain possible without it. Current client bytes
settle their consumer consequences, not which policy the server used.

Ten focused labels applied/re-exported in read-only Ghidra: **ten applied,
zero created/missing/duplicates**; project changes discarded. Four opcode
provenance records now contain registration/reader/consumer witnesses with
server-policy limits explicit. Mapping validator: **8,348 rows / seven files**.
The tracked read-only SQL census ran successfully. ArchiveProbe extraction
succeeded with existing package-audit warnings; no packages or configuration
were changed. No behavior tests/build or full-suite repeat was needed for
comments/evidence-only edits. The previous Source full-suite result remains
15,130 passed / 17 failed / two skipped, with all nine failed methods passing
isolated reruns (19 cases); this slice does not make that run green.

Ignored reproduction root: `artifacts/prerequisite-type102-fluff-admission/`.
Native records: `admission-and-finish.txt`, `parent-runtime-builders.txt`,
`table-bindings.txt`, `neighbor-getter-and-parent-destroy.txt`,
`wrapper-registration-and-lookup.txt`, `registration-block.txt`,
`wrapper-initialization.txt`, `wrapper-initialization-disassembly.txt`,
`wrapper-update-event.txt`,
`target-removal-reader.txt`, `finish-reader-and-list-data.txt`,
`original-descriptors.txt`, `base-vtable.txt`, `builder-disassembly.txt`,
`noop-disassembly.txt`, `labelled-admission.txt` and matching logs.
Data/corpus records: `shipped/DB/`, `shipped-schema-probe.txt`,
`shipped-anchor-verification.txt`, `tracked-census.tsv`, `fluff-reference.tsv`,
`reference-summary.json`, `anchor-names.tsv`, `available-packet-files.txt`,
`video-leads.json`, and archive inventory/extraction logs. Existing headless
scripts `Gap8Decomp.java` / `ApplyNexusForeverLabels.java` reproduce the reads.
REA closed after the reads/exports. No decompiled client source or raw shipped
table is committed. No retail parity claim.
