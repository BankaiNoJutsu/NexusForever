# Galactic Archive publication

Continuation after `db8e56d0`, 3 November 2026. The final source/provider
qualification below closes this milestone; the wider Goal remains active.

## Observable boundary

Real Player archive unlock, linked unlock, rule and viewed notices must wait for
confirmation of the original complete ExperiencePersistence graph, then publish
on its owning world update. Initial sync must expose confirmed rows. Preserve
accepted notice order, quiet rule updates, frozen later bits, reward callbacks,
unavailable raw rows, fake-player behavior and existing packet fields.

The title milestone's
`artifacts/continuation-20261103/archive-publication-probe/results.json` reproduces one
archive packet before any save. A rejected complete graph leaves all four
archive/achievement/title/claim tables empty; the title control emits no packet.
The ordinary retry commits all four rows.

## Current evidence

Local evidence: `artifacts/continuation-20261103/archive-publication/`.
The corrected pre-change baseline has five genuine failing cases; the earlier
missing-using compile failure is retained separately. The implemented candidate
passes 68 focused cases. Recovery and affected collection/experience controls
pass 138 cases, zero failures/skips, in `recovery-candidate.trx`.
The new coverage includes lost acknowledgement, savepoint rollback, frozen later
view bits, per-packet retry, initial-send reentry, quiet rules, unknown rows,
replaced managers, obsolete identities/sessions and accepted payload order.

The first affected candidate passes 239 cases. Spec review identifies duplicate
notices when initial sync follows an already-retained viewed change or unlock.
`review-negative.trx` executes three failures, including initial requests before
and after SQL commit. Initial sync now recognizes exact pending/queued notices
while retaining the synthetic request needed for newly quiet rule rows. The
corrected affected gate passes 241 cases, including all 18 new cases. Both
corrected reviews are clear across eight Source paths; manifest SHA256:
`bdffd56c7cddeafe20012c8b6cdef71e79d503020be09a5329c1fb9c80a5961a`.
The full solution build has zero warnings/errors (28.9s). All 18 original compiled
cases pass on configured MySQL 8.4.13 (133.5s), using controlled article/rule tables
and sessions. The owned server/listener is absent after graceful shutdown. The
earlier 16-case MySQL candidate is retained separately. The final full-suite gate passes.

Final binding: 6,268 Source/Tools inputs, SHA256
`8459c65ae8879da23c0b53a7c98a948612aa1a1c068090b4087e684bd7d53021`;
test assembly `96531f3abed1a863dfb461d02132c9baf9d5b3d206cdb16544e7d73f74307131`.
All 11 owner/test/provider DLL triples match. Database source/schema are unchanged;
historical upgrades retain their own version boundaries. No unchanged migration
or historical checkpoint verifier was repeated.

## Connected behavior

The original ExperiencePersistence graph acknowledges archive metadata only after
its flushed transaction confirms. Accepted unlock/view notices retain immutable
rows and consume only the confirmed FIFO prefix: public mutations add bits, so
later frozen view bits cannot be released by an earlier save. Quiet graph changes
advance the confirmed initial state without creating unsolicited notices.

World scheduling and session inspection occur outside the persistence metadata
gate. The achievement callback is split into metadata and ready-to-schedule
stages to preserve that boundary across both managers. Archive rewards keep their
existing gameplay flow; notice retention precedes outward reward callbacks that
may synchronously save. Initial sync serializes with publication to avoid older
rows arriving after newer updates. A failed packet retains its cursor; manager,
identity, player registry, session and disconnect guards reject obsolete owners.
Unavailable loaded rows stay raw in persistence and stay out of published lists.
Interface-only managers preserve their existing immediate-send behavior.

No Database source, schema, entitlement default or wire change was made. Defaults 15:1/17:1 remain; no automatic 44 or
seven baseline character autogrants. Restart/outbox, deployment, physical/native
and retail qualification remain open. Computer Use/native launch stay stopped.

## Final qualification

The full `Source/NexusForever.slnx` gate passes **12,007 tests**, zero failures or
skips. All preceding 11,989 labels, all 18 new cases and all 241 affected cases
are preserved in that run. Both Source review axes are clear across eight paths;
full build has zero warnings/errors. All 18 original compiled configured-MySQL
cases pass on the final candidate. `verification.json` binds 6,268 inputs and
all 11 owner/test/provider DLL triples. Recorded runner/helper/test-host
generations and the owned MySQL server/listener are absent.

Observed validation time: final full build 28.9s, corrected
affected execution about 67s, final provider 133.5s, full tests
772.9s. The five baseline failures, excluded missing-using
compile error, three review negatives and earlier passing candidates are retained
with their original identities. No unchanged full run or migration proof was repeated.
The historical continuation verifier's expected Source divergence remains separately
recorded. These checks do not qualify actual retail table delivery, deployment,
broker/restart/outbox durability, physical gameplay or native client timing.

## Next integrated outcome

`artifacts/continuation-20261103/tradeskill-publication-probe/results.json`
reproduces real Player profession and schematic updates before any save, plus an
initial list exposing both unsaved rows. An owned recipe trigger rejects the
complete graph, leaving skill/recipe/archive tables empty. The current archive
path emits no premature packet; retry commits all three rows. Tradeskill
publication is the next integrated outcome. Crafting semantics, Contract/reset,
restart/outbox and broader gameplay/external/native/retail requirements remain.

The separate `tradeskill-savepoint-probe/control-results.json` in the same dated
artifact root rules out a suspected baseline defect: shared CharacterContext
completion already reports rollback as uncommitted, and the original actor retries
both skill/recipe rows successfully. Its first failed hypothesis check is retained;
do not invent a new savepoint recovery fix from that control.
## Datacube and journal companion publication

4 November 2026. The live creature activation and collection manager previously
published Datacube and journal progress before the Player save. The baseline
reproduced premature packets, leaked later bits across an Auth-phase save
boundary, and let failed duplicate-response sends escape to the caller.

The collection now retains each accepted additive progress snapshot, including
the existing duplicate interaction response. Its ordinary or frozen Experience
graph releases only confirmed notices. Initial lists use committed state and
preserve the last published values while older notices await their world tick.
The queue retries a failed send on the original Player, manager and session.
Scientist scan state stays server-side and standalone collection adapters
retain their existing send behavior.

The regressions cover the real creature activation, rejected save and recovery,
frozen Auth-gap edits, initial-list ordering, and a duplicate send failure with
and without session rebinding. Review found that a queued notice could hide
quiet progress on another entry and that a reentrant send could overtake an
initial list. Per-entry snapshots and the shared send guard cover both cases.
The owning build and all 231 scoped cases passed, including six new regressions.
The scoped tests took 52 seconds. Local results and binary bindings are in
`artifacts/continuation-20261104/datacube-publication/`. Native presentation,
restart replay and retail behavior remain open.
