# Restoration handoff (2026-11-14)

Current branch: `feature/spell-effects-remaining`. Latest continuation starts from
published `2c35012c` after the type-40 primary-subject correction;
type 102 remains Blocked on the missing retail trace by user direction, and
the next highest-reach primary-subject wrapper is continued instead.
Static/headless investigation only; no native client execution or Computer Use.

## Latest type-12 primary-subject DeadState correction

[Type-12 DeadState subject](PREREQUISITE_12_PRIMARY_SUBJECT_20261114.md):
**Verified native selector/comparison and IsDead/Ghost bindings; Implemented /
Source wrapper; retained death proxy inferred**. Direct switch/vtable bytes bind
case0xc -> 1404a24e7 -> slot140b677c0 -> 14049c800. The primary unit's +0x250,
+0x254 or kind23 normalize to 0/1 and compare with full Value for Equal/NotEqual;
other modes fail. ObjectId/relationship unit are unread. Only the player wrapper
changes to retain player; existing explicit-unit null guard is reused. The native
helper itself has no local null guard, so do not claim native null returns false.

**Mapped field evidence and retained limits:** native Lua IsDead names only
+0x250. Structural setter14045ec90 writes the same input to +0x254/+0x250, but
its complete authority/alias-writer and transition chain is not recovered.
Source !IsAlive means zero health or a Source deathState; equivalence to the
native fields remains inferred. No new death state or timing rule. Concrete
GhostEntity is not IUnitEntity; kind23 unit-proxy tests do not qualify real Ghost
admission. The seven-scope CSV and bounded instruction/write-candidate review
retain those distinctions. No native-label changes are needed.

**Observed verification/reach:** 168 slots / 168 rows / 364 direct/weighted spell
references. Of 39 red-scope cases, 15 fail and 24 pass; 29 are new cases. Final
Prerequisite/Spell/Cooldown gate passes 3,086, zero failed/skipped; owning Game
build has zero warnings/errors. Identity/table/census/coverage/diff checks pass.
Unchanged test/config warnings and earlier non-green full-suite result remain;
no full-suite repeat, deployment or provider/native qualification. No retail parity claim.

**Next:** the full type12 native-field/Source bridge remains pending research.
For selector work, type76 has 102 weighted references but the re-read native
slot+0x300 points at shared false stub140001ba0; it cannot supply a server rule.
Next native-backed selector candidate: type86 Jump (67 weighted), with its own
body/caller read. Type102 stays Blocked on the missing attributable retail Fluff
trace, per user direction. Client/CUA remain stopped.

## Previous type-40 primary-subject health correction (2c35012c)

[Type-40 health subject](PREREQUISITE_40_PRIMARY_SUBJECT_20261114.md): **Verified
native selector/arithmetic + ISA comparison; Implemented / Source**. Re-derived
case 0x28 switch entry 1404a2367 -> 1404a4d90 and actual parameter registers;
the subject is primary. Named Lua GetHealth/GetMaxHealth registration/bodies
independently bind virtual +0x40/+0x48. Single-precision uint conversion,
multiply100 then divide, threshold conversion and null-primary guard agree with
the existing calculation; the player wrapper now retains player.

**New native discrepancy recovered:** 1404a2010's Equal/NotEqual arms use
UCOMISS/JNE and UCOMISS/JE without PF rejection. Intel ISA flags establish
unordered Equal true, NotEqual false, all ordering comparisons false when
comparison is reached with masked FP exceptions. The earlier C decompile/IEEE
comment missed this distinction. A named native comparison is used only by type
40; existing other float paths retain behavior pending their own caller/NaN
review. Do not globally promote their earlier native-parity comments. Zero/zero
still produces NaN, positive/zero infinity; no clamp or guessed default. Retail
zero-max reachability and client exception-mask configuration remain unknown.

**Observed reach/verification:** 406 slots / 338 rows / 623 direct / 705 weighted
spell references. Of 31 new cases, nine fail red and 22 pass; the final related
Prerequisite/Spell/Cooldown gate passes all 3,057, zero failed/skipped. Owning
Game build has zero warnings/errors. Eight-mode comparison CSV, native identity/
switch/constant/Lua binding, census and diff checks pass. Unchanged test/config
warnings and earlier non-green full suite are retained; no full-suite repeat,
deployment, provider/socket or native execution qualification. No retail parity claim.

**Next selector candidate:** type 12 DeadState (168 slots/rows, 364 weighted
spell references), with its own native/body/caller read first. Type 102 remains
Blocked on the missing attributable untimed-Fluff retail lifecycle trace, per
user direction. No client/CUA launch. Current type-40 finding supersedes the
earlier health note's IEEE claim; the native helper's existing generic label
does not need a rename or new export.

## Previous type-39 primary-subject correction (85b11fbe)

[Type-39 player subject](PREREQUISITE_39_PRIMARY_SUBJECT_20261114.md): **Verified
native selector/kind; Implemented / Source**. Re-read case 0x27, switch entry
1404a22d4 and null/boolean tail against the matching build-16042 binary.
Independently re-read Lua GetType registration and stack string array: kind20
maps to Player. The player wrapper now uses its player as primary; relationship
Target cannot replace it or rescue a null subject. Existing explicit-unit kind
test and entity models remain unchanged. Source activation routing is observed,
but native activation policy is not promoted by this selector correction.

**Observed reach/verification:** 200 slots / 200 rows / 1,140 direct/weighted
spell-column references. Six new cases fail red; two existing explicit-NPC
controls selected by the red filter pass. Final Prerequisite/Spell/Cooldown gate
passes 3,026 cases, zero failed/skipped; owning Game build has zero warnings/errors.
Unchanged test/config warnings and preceding non-green full-suite result are
retained; no full-suite repeat or new provider/native execution qualification.
No retail parity claim. Published preceding type-28 correction: `d15cedbb`.

**Next candidate:** type 40 Health (406 slots / 338 rows / 623 direct / 705
weighted spell references), then type 12 (364 weighted). Re-read each native
selector/body/caller before changing its remaining substituting player wrapper.
Type 102 stays Blocked on the attributable untimed-Fluff retail lifecycle trace;
do not reopen the exhausted search without new evidence. No client/CUA launch.

## Previous type-28 primary-subject correction (d15cedbb)

[Type-28 combat subject](PREREQUISITE_28_PRIMARY_SUBJECT_20261114.md): **Verified
native selector/field; Implemented / Source**. Direct switch bytes select
1404a3045 for case 0x1c; it reads primary unit +0x2ac and the shared boolean
tail. Independently re-read Lua IsInCombat registration/body binds that field.
Only the player wrapper changes to retain player and preserve relationship Target;
explicit-unit routes, comparison and combat producers/timing remain unchanged.

**Observed reach:** fresh read-only reference census ranks remaining substituting
wrappers 28 (1,293), 39 (1,140), 40 (705), 12 (364), by weighted spell-column
references. Type 28 has 230 slots / 230 rows; its direct count is also 1,293.
These are dependencies, not affected casts. Seven new context cases fail red,
then all 3,020 Prerequisite/Spell/Cooldown cases pass. Owning Game build has zero
warnings/errors. Unchanged test/config warnings are recorded in the finding;
no full-suite repeat or green full-suite claim. No retail parity claim.

Next unblocked selector candidate is type 39; re-read its dispatcher/body and
caller contract before correction. Type 102's missing attributable untimed-Fluff
Start/Go/remove/Finish trace remains the exact Blocked input; do not reopen that
search without new evidence. Native labels need no change in this slice.

## Previous type-102 sniff-collection lead follow-through (7702f101)

[Lead follow-through](PREREQUISITE_102_SNIFF_COLLECTION_LEADS_20261114.md):
**Observed primary statements/excerpt / Correlated provenance / Mapped Source
outputs / Blocked original-server witness**. The two open issues' complete
returned timelines have two/five events, with no recording or linked-PR location.
WorldDatabase attributes initial data to custom tooling ingesting retail-era
sniffs; checklist changes predate 370 and fit its extraction workflow. The
reviewed SQL keeps entity/stat/spline data, not cast IDs or ordered spell traffic.
Vendor/store PRs likewise supply static SQL. SpellWorks loads client archives
and tables. These outputs cannot recover Fluff order.

A public PR 62 comment (2018-12-09) contains a parsed player-entity excerpt,
attributed by its author to a sniff shared by lucas-jones on Discord. It supplies
neither independent recording provenance/build nor Start/Go/remove/Finish.
The cited participant's five listed public fork branch trees and empty releases
list provide no named capture candidate. Sharing attribution does not establish
collection custody or current access. Related checklist code and later sniff
citations provide no raw-file location in reviewed records.

The source inventory now has **32 scopes**, including nine new scopes and
expanded issue timeline review. Exact refs, direct links and unreviewed-history/
external-payload limits are in the finding. No application/native-label changes.
Identity/SQL-shape/CSV/diff checks pass; no build/test repeat. No retail parity claim.

**Next gate remains Blocked:** actual attributable recording bytes (or complete
extraction) retaining build/context, concrete Spell4/effect, wrapper/receiver IDs
and ordered lifecycle for untimed Fluff; include a multi-target survivor before
generalizing retirement scope. The reported collections and historical sharing
route are leads, not known available inputs. No third-party messages, login or
private-channel access performed. Do not invent custody/access or persist Fluff
markers/parents from derived data or the player excerpt.

## Previous type-102 original-server trace search (4388354d)

[Trace search](PREREQUISITE_102_RETAIL_TRACE_SEARCH_20261114.md): **Mapped Source
capture format / Observed source inventory / Blocked original-server witness**.
No qualifying untimed-Fluff Start/Go/remove/Finish trace was located. The parser's
`.awps` format was absent from the preceding loose-file search; expanded searches
now include it and another local client/addon corpus. All 911 ZIP / 122 7z member
listings succeed; no applicable capture candidate. Reviewed public default trees,
three Internet Archive ZIP catalogs plus 37 selected indexes, and two wiki XML
histories supply code/addon/reference material rather than a verified sequence.
The then-23-row source inventory retains exact scope and unreviewed-payload limits;
the latest pass above extends it.

Upstream issues **370 and 372** are the strongest remaining leads: they refer to
sniffs/a collection, but their reviewed bodies/comments/events provide no trace
download. Do not claim the uninspected collection is absent, private, accessible,
or known to cover Fluff. The next input is actual original-server bytes with
build/context and ordered wrapper/effect/receiver IDs, preferably with a
multi-target survivor before generalizing removal policy. No third-party message
or private-channel access was performed. Parser code, addon events, client assets
and modern emulator captures are not substitutes for that witness.

Evidence/tracker updates only. Source behavior, timers, packet layouts/emission
and prior native findings are unchanged. Inventory/hash/diff checks pass; no C#
build/tests or full-suite repeat. Current earlier non-green Source test result
remains below. No retail parity claim.

## Previous type-102 Fluff admission continuation (9419040d)

[Fluff admission and ordering](PREREQUISITE_102_FLUFF_ADMISSION_20261114.md):
**Verified native + shipped-table witnesses**. Fluff type 17's flags 27 pass
the runtime copy gate; received variants use the base effect node and register
on the receiver before apply/timers. No-op apply/expiry virtuals do not establish
a server lifetime. The 0x0819 guard is **Spell4Base CastMethod 10**: raw +0x50
copies to runtime +0x18, with 196 joined slots matching the shipped table.
Of 49 untimed Fluff slots / 42 effect rows, only 20 slots / 16 rows use that
method; 29 slots / 26 rows use other methods. Overall Fluff reach remains
197 slots / 164 rows. One parent join is unresolved and is retained explicitly.

0x0811 reader `14007a550` and registration `14006c290` independently confirm
the existing Source counted receiver-ID writer. All four removal contracts now
have reader/registration/consumer evidence. `140561ad0` unregisters/deletes a
wrapper, with SprintStateUpdated only a conditional side effect. The nonzero
registered-ID map is removed on Finish; later Go/tier lookup cannot re-admit
under it without fresh registration. Two wrong table getter labels are corrected.
Ten labels applied/re-exported; **8,348 mapping rows** validate; read-only census
and shipped-anchor checks pass. Source edits are packet comments only; no new
behavior tests/build/full-suite repeat. The previous non-green suite below stands.

**Mapped only / Blocked server policy:** no available trace establishes when
the original server admitted an identified untimed Fluff row, retained/replaced
its wrapper, or emitted target removal/Finish. Local packet-file inventory,
retail video leads and a pinned 2016 addon source do not close that gate. Seek
an original-server Start/Go/remove/Finish trace with wrapper/effect/receiver IDs
and a multi-target survivor, or the corresponding original-server producer.
Do not replace Source's empty-queue Finish with guessed retention or add a marker
that outlives native teardown. No state/packet emission or deployment change.
No retail parity claim.

## Previous type-102 receiving continuation (c6eee3bc / 2193ecbd)

[Receiving-unit state](PREREQUISITE_102_RECEIVING_STATE_20261114.md): **Verified
native receiver/ID; Implemented / Source** for installed property modifiers,
conversions and personal-healing states. Their existing receiving-unit row IDs
are queried directly; stale pending-spell timers cannot resurrect removed state.
Admission, stacking and timers are unchanged. Thirteen new cases ran red
(11 failed / two passed); rebuilt focused gate: **3,014 passed**. Owning Game
build has zero warnings/errors. Full suite: **15,130 passed / 17 failed / two
skipped**. All 23 type-102 check, five ownership and 13 new receiving-property
cases passed. Sixteen failures reported EF provider-cache warnings; one was a
challenge reward assertion. All **nine failed methods pass isolated reruns
covering 19 cases**. Their cause is not established; this is not a green suite.

**Verified native removal boundaries:** 0x0811 -> `1405a5aa0` destroys the listed
receivers' target nodes and children; 0x07fe -> `14053e1b0` destroys all target
nodes including untimed children. 0x0813 is displayed-count removal, whose
count-zero helper does not itself destroy effect children. 0x0819 destroys
flag-2 active children only under parent shape 10, preserving finite children.
Do not conflate those packets or reverse 0x0813's fields. Source 0x0811 replay
comments are corrected; wire layout and non-emitted status are preserved.
Six labels applied/re-exported (zero missing/duplicates); **8,344 mapping rows**
validate. REA closed; read-only Ghidra changes discarded.

**Mapped only / Blocked:** do not implement the empty Fluff handler's untimed
state from client membership alone. It reaches 197 resolved type-102 slots /
164 effect rows (49 flag-2 slots). Source automatically sends Finish when its
event queue empties, which destroys native untimed nodes. Neither persisting a
marker beyond that Finish nor delaying Finish has its original-server policy
witness. Next separating evidence is a known flag-2 cast traced through apply,
target removal and Finish, with a multi-target survivor, or the equivalent
original-server producer body. Static client removal is recovered; server
ordering and admission remain unknown. `SendSpellGo`'s universal -1 remaining
value versus the native nonzero override is a separate recorded timing gate.
The property correction queries accepted Source state and does not qualify its
parent retention or reconcile its possible survival beyond Source Finish with
native teardown. No new registry, packet emission or runtime deployment.
No retail parity claim.

## Previous type-102 node continuation (983f8934)

Evidence tier: **Verified native ID/ownership/registration lifecycle; Source
correction with explicit coverage limits**. [Native node evidence](PREREQUISITE_102_NODE_LIFETIME_20261114.md)
binds accessor `1405da910` through node `+0xb0` to the runtime Spell4Effects
record, its builder and service map. Initializer `1405d6880` links target
`+0x15d0` and caster `+0x15d8` separately. Flag 2 does not require a finite timer;
timer expiry and target-list destruction are distinct transitions. Parent
teardown destroys children; the 0x0819 prune branch is guarded by parent shape 10.

The supported Source correction only excludes another target's effects from
the querying caster's pending-spell lookup. Five new cases ran red before the
edit, including positive self controls, target order and reused-guid identity.
The rebuilt Prerequisite/Spell/Cooldown gate passes **3,001 cases**; owning Game
build has zero warnings/errors. Nine new labels plus the selector refreshed
(ten applied, three accessor functions created; zero missing/duplicates);
8,341 mapping rows validate. Full suite: **15,106 passed / 28 failed / two
skipped**; all 23 type-102 check and five ownership cases passed. Twenty-six
failures reported EF provider-cache warnings; two were challenge/auction
assertions. All 16 failed methods pass isolated reruns covering 61 cases. This
does not establish their cause or make the full suite green. Implemented /
Source for the bounded correction; no retail parity claim.

Do not convert the native client list into a generic server timer registry.
Receiving-unit discovery and untimed registration/retirement still need an
authoritative server producer/removal model; the current timer-event and
unfinished-parent gates are not newly qualified. Instantaneous/delayed row
admission, dynamic override writers and exact presentation timing remain separate.
Reference reach stays 353 slots / 303 rows / 302 direct / 352 weighted references.

## Previous type-102 subject continuation (ca826690)

Evidence tier: **Verified native selector; Implemented / Source**.
[Type-102 primary subject and census](PREREQUISITE_102_PRIMARY_SUBJECT_20261114.md)
re-derives the direct dispatcher call and helper `1404a4fe0`: primary unit list,
ObjectId comparison, unread Value and relationship unit. The player wrapper now
retains the player; explicit-unit, cast, apply and persistence routes already
retain their supplied subject. The existing substitution expectation is rewritten
with an explicit-target control. Fourteen new cases ran red (10 failed / four
passed); rebuilt Prerequisite/Spell/Cooldown gate: **2,996 passed**. Owning Game
build: zero warnings/errors. One conservative native label re-exported;
8,332 mapping rows validate. Full suite: **15,112 passed / 17 failed / two
skipped**; all 23 type-102 cases passed. Fifteen failures reported EF
provider-cache warnings; two were challenge/title assertions. All 13 failed
methods pass isolated reruns covering 19 cases. This does not establish their
cause or make the full suite green. Commands/outputs are in the linked note.

Reference-data census: **353 slots / 303 prerequisite rows / 302 direct columns /
352 weighted references**. The audit's 352 remains correct as a weighted count.
The earlier 102/121 result is incomplete: excluding Effects target apply and
target persistence reproduces that lower total. Its historical cause is unknown;
the original query shape now returns 302/352. A tracked read-only SQL census
reproduces the counts and anchors. These are dependency reach, not affected casts.
The native element ID/lifecycle gate is superseded by the latest section above;
the broader server producer/lifetime interpretation, mixed non-unit-row policy
and native activation caller policy retain their limits. No retail parity claim.

## Previous type-44 continuation (6f69d108)

Evidence tier: **Verified native selector; Implemented / Source**.
[Type-44 primary subject](PREREQUISITE_44_PRIMARY_SUBJECT_20261114.md) re-derives
the case `0x2c` dispatcher, handler `14049ee40`, criteria proxy and TargetGroup
getter. The player entry point now retains the player; explicit-unit and spell
paths already retain their supplied subject. One target-substitution expectation
is rewritten with explicit-creature controls. Sixteen new regressions ran red
(14 failed / two passed); rebuilt Prerequisite/Spell gate: **2,934 passed**.
Owning Game build: zero warnings/errors. Three native labels re-exported;
8,332 mapping rows validate. Full suite: **15,100 passed / 15 failed / two
skipped**; all 40 type-44 cases passed. The ten failed methods pass isolated
reruns covering 66 cases. Thirteen full-run failures reported EF provider-cache
warnings; two were assertions. This does not establish their cause or make the
full suite green. Commands and outputs are recorded in the linked note.

Reference-data reach is 214 slots / 201 prerequisite rows / 278 direct spell
columns / 281 weighted references, not affected-cast counts. Group attribute,
recursion, missing-table, non-unit-row and activation-caller policy are unchanged
and are not newly qualified. The native lookup's descriptor identifies
TargetGroup, correcting its old Spell4ValidTargets table label. ObjectId is
ignored but nonzero in one slot; the earlier all-zero claim is corrected.

## Previous type-179 continuation (3d1c6aa0)

Evidence tier: **Verified native field binding/evaluator; Implemented / Source**.
[Type-179 field binding](PREREQUISITE_179_FIELD_BINDING_20261114.md) proves that
raw Creature2 `+0x1c` becomes intermediate CreatureInfo `+0x14`, cached by the
unit at `+0x30`. A present record whose raw ID is zero retains constructor default
25; the evaluator uses zero only with no creature record. The setter's separate
Creature2Difficulty lookup does not gate that raw/cached ID. Raw-zero reference
data reach: 17,990 / 53,137 Creature2 rows; type-179 dependency reach: 41 slots /
504 direct spell-column references / 517 weighted references, not broken-cast counts.

The type-179 player wrapper, explicit-unit handler and pure-179 spell shortcut
now share one comparison helper. Null subjects fail; Target remains relationship
context; the pure shortcut stops at None. One test encoding target substitution
was rewritten with explicit-subject controls. Four native labels were reapplied
and re-exported; the accessor was created as a function in the read-only Ghidra
session. The constructor is not a packet consumer, correcting its old generic label.

Earlier commits remain: `ff3cf41f` corrects primary subjects for 38/59/129;
`b021a3ce` records the then-unmapped type-179 accessor. Their validation is in
[the earlier slice](PREREQUISITE_PRIMARY_SUBJECT_20261114.md); its pending field
gate is superseded by this continuation.

## Previous type-179 verification and limits

Initial expanded red: 36 failures before production changes. Some null tests
reached unsupported generic player-manager null logging. The first wider green
attempt passed 2,914 / failed four null cases at that logger; these were corrected
to exercise the actual type-179 player handler while keeping the real explicit-unit
manager null checks and false assertions. The final four handler-level null cases
were not separately rerun red; the other 32 cases had behavioral baseline failures.
Final owning/dependency rebuild and Prerequisite/Spell gate: **2,918 passed**,
zero failed/skipped. Five CS8632 warnings are in the unmodified MySQL test file.
Explicit owning Game build: zero warnings/errors. Mapping validator: 8,331 rows /
seven files pass. Full suite: 15,069 passed / 30 failed / two skipped / 15,101
total; all 36 type-179 cases passed. All 17 failing methods passed separate
reruns covering 94 cases, including all 30 failed cases. Twenty-eight full-run
failures reported EF provider-cache warnings; two were assertion failures.
Isolation does not establish their root cause or make the full suite green.
See the field-binding note for output paths and qualification limits.

## Open outcomes

Type 179 is implemented for the existing server CreatureInfo/CreatureEntry model.
No independent dynamic difficulty override state was introduced. Type 180's
rank helper and mixed non-unit prerequisite-row policy remain unchanged; native
acceptance, live UI/timing and retail qualification were not performed.

The broader restoration queue remains open. Lower remaining target-substituting
wrappers require their own dispatcher/body and caller checks. Type 102's full
reference census is resolved above; the cause of the earlier incomplete result
remains unknown. Generic player-manager null logging is outside these selector
corrections; the null-capable explicit-unit manager remains tested.

## Not claimed

No retail parity, native client run, Computer Use, runtime deployment, provider
qualification or broad restoration completion. Original runtime data and
entitlements are preserved. Continue normal scoped commits/pushes on the branch
above; no force push. The commit containing this handoff carries the type-102
node evidence, labels, bounded ownership correction and regressions together.
