# Committed title publication

Continuation after `d2d09f0d8ade49ad69fedf3fc7a2e8cf8a4f56e0`, 3 November 2026.
Real Player title publication now follows its original confirmed save graph.
The final source/provider qualification below closes this milestone; durable
restart delivery and the wider roadmap remain separate.

Real Player grants, revocations, expiry, active selection and bulk commands must
retain their accepted packet intent through rejected, rolled-back and uncertain
saves. Only a confirmed original title graph can release that intent to guarded
world-update publication. Preserve suppression, accepted packet order, later
mutations and existing title checks. Initial lists/entity visibility must use the
published committed state. Interface-only compatibility keeps its existing path.
Restart/outbox, archive/tradeskill publication and native/retail qualification are
separate requirements. Defaults 15:1/17:1/no44/seven grants and client/CUA stop remain.

The prior real achievement/title probe is retained under
`artifacts/continuation-20261103/achievement-title-publication-probe/`.
Current evidence is under `artifacts/continuation-20261103/title-publication/`.
`before.trx` records three real early-publication failures (achievement reward,
bulk list, ordered grant/already-owned/revoke), plus an excluded entity-model
fixture error caused by missing appearance metadata. The corrected entity fixture
configures empty owned appearance/guild collections before testing that path.

The first corrected-fixture attempt still failed and remains excluded. Source
edits during compilation left freshness uncertain. A test-only rebuild against
the unchanged published Game DLL produces
`before-entity-fresh.trx`: entity title22 is visible while SQL still owns21.
`baseline/binding.json` binds that corrected fixture, TRX and retained DLLs. The
three earlier packet negatives remain attached to their preceding test build.

The connected implementation retains ordered notice versions with each ordinary
or frozen title save. Only a flushed, confirmed graph acknowledges those versions;
savepoint rollback cannot advance the title baseline. Immutable committed frames
enter the publication queue under the persistence gate before another save can
enter; session inspection and world scheduling occur after releasing the gate.
World publication retains its recipient cursor through a failed send. Initial
lists and Player entity models use published state, while existing title checks
and accepted runtime selection remain available to gameplay.

The first candidate passes23 cases, retaining an analyzer warning that was then
corrected without changing its assertion. The recovery candidate passes31 cases,
including lost acknowledgement, suppressed flush, savepoint rollback, frozen
normal-save intent, unchanged graphs, replacement and failed-recipient controls.
`admission-before.trx` then reproduces two additional gaps with two controls: a
cleared identity raised world errors, and repeated expiry ticks queued duplicate
revocations. Both are corrected in the current candidate. That intermediate
15-case surface still needed broader integration and qualification; the final
results below supersede it. No Database source, schema or wire shape is changed.

The first affected gate passes181 cases. Independent reviews then found three
gaps: a bound row save acknowledged unsaved sibling/selection notices, two bulk
commands lost their accepted list snapshots, and interface-only AlreadyOwned
suppression compatibility drifted. `review-before.trx` reproduces all three with
one standalone-row control. Real bound leaves now enroll the complete original
manager graph, bulk notices retain accepted immutable lists, and fake/standalone
paths preserve legacy behavior. Four new regressions qualify those corrections.

`review-corrected.trx` passes185 affected cases, including all 19 new cases. Both
corrected Source reviews are clear across 9 paths, manifest SHA256
`ce6df1a2139abca6fe228f32ea69e7bfcff0d76751c36208570937e0f2138028`.
The full solution build passes with zero warnings/errors (30.4s). All 18 original
compiled real-Player/MySQL cases pass, including both theory arguments (149.6s).
Provider workload generations and the owned MySQL server/listener are absent.
Source/Tools hash `4d895939323d3fe0544ce9a0eab1994a25e4c9e5890e3c6f7a66d3962cff94f5`
covers 6,264 inputs; test assembly
`78b9371a79cfdf3ad1bcfb8d13c20dd2df69ce90cbd049ef3bee4e6bc1050b0f`
and all 11 owner/test/provider DLL triples match. The full-suite gate passes; the final qualification below binds its result. Restart/outbox and retail remain open.

A separate probe under `artifacts/continuation-20261103/archive-publication-probe/`
now reproduces an early archive packet through real achievement/archive/title
managers. SQL rejection leaves all four participating tables empty, while retry
commits all four rows. The current title code sends no premature title packet.
Archive publication supplies the next integrated outcome after this gate.

## Final qualification

The full `Source/NexusForever.slnx` gate passes **11,989 tests**, zero failures or
skips, preserving every preceding 11,970 label. All 19 new and 185 affected cases
are covered. The full build has zero warnings/errors. Both corrected Source
reviews are clear across 9 paths. All 18 original compiled MySQL producer cases
pass, including both stale-owner theory arguments and standalone compatibility.
The controlled table/session/visibility boundary remains explicit.

`verification.json` binds 6,264 Source/Tools inputs under
`4d895939323d3fe0544ce9a0eab1994a25e4c9e5890e3c6f7a66d3962cff94f5`,
all 11 owner/test/provider DLL triples, and test assembly
`78b9371a79cfdf3ad1bcfb8d13c20dd2df69ce90cbd049ef3bee4e6bc1050b0f`.
The provider workload generations and owned MySQL server/listener are absent.
Database source/schema are unchanged; earlier upgrade proofs retain their own
version boundaries. No unchanged migration or full gate was repeated.

Observed validation time: full build 30.4s, corrected
affected test execution about one minute, configured provider 149.6s,
full gate 910.3s. Fixture failures, the analyzer warning,
two admission negatives and all three review negatives remain retained. None is
silently attributed to the final passing candidate. The historical continuation
verifier's expected Source divergence remains separately recorded.

These checks do not qualify a deployed server, broker, restart/outbox, actual
retail table corpus, native client timing or retail completeness. Defaults
15:1/17:1 and no automatic 44/seven baseline grants remain unchanged; client/CUA
remain stopped. The broader Goal is active.

## Next integrated outcome

`artifacts/continuation-20261103/archive-publication-probe/results.json` records a
real achievement grant through real archive/title managers with controlled rule
metadata. One archive update reaches the session before any save. A rejected
complete graph leaves archive, achievement, title and claim tables empty while
that archive packet remains sent. The new title path emits no premature title
packet. Retry commits all four rows. Archive publication is next; tradeskill,
restart/outbox and broader external/native/retail requirements remain open.
