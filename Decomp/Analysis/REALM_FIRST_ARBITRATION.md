# Durable realm-first arbitration

Continuation after `2f24c354bdaa5bed1b821a0a1efea645d50f157f`, 3 November 2026.
The durable character/guild claim and original achievement graph now commit
together through real producers. The final source/provider qualification below
closes this arbitration milestone; durable restart delivery remains separate.

## Acceptance boundary

Two real character/guild producers sharing one Character database must select
one durable announcement winner. The claim and original achievement graph must
commit together. Rejected saves, rollback and uncertain acknowledgement must
retain accepted work without consuming another actor's opportunity or selecting
a second winner. Character and guild scopes remain separate. Historical completed
rows close the corresponding claim without assigning an unproven historical
winner. Preserve ordinary achievement progress for other owners.

The claim must participate in the save graph: validation callbacks prohibit
writes, and ordinary saves can use EF's implicit transaction. Durable delivery
after process restart, an outbox, cross-character reward atomicity and companion
reward publication remain separate requirements.

## Executed baseline

Evidence: `artifacts/continuation-20261103/realm-first-arbitration/`.
`before-owned-profile.trx` records **three failures and one historical control**
against unchanged published production source. Actual Player/achievement managers,
owned inventories, public grants, CharacterAtomicSave and session event queues
exercise duplicate stale-peer announcements, a rejected save reserving the shared
registry's winner, and lost acknowledgement allowing a stale peer to announce.
Both owners' progress remains part of the expected behavior.

The preceding `before.trx` and `before-owned-inventory.trx` each retain four fixture
failures: a placeholder inventory caused reward quarantine, then the inventory
constructor lacked global capacity assets. They do not qualify arbitration
behavior. The corrected fixture reuses the existing owned-inventory helper.
`baseline-binding.json` binds the corrected source, TRX and copied runtime.

## Connected implementation and current checks

The durable store has one key per character/guild scope and achievement. It has
no owner FK, so owner deletion cannot reopen a claim. Actor-local immutable
tokens/names join the same Character or guild save graph; SQL uniqueness selects
the winner. Rollback retains intent, while a lost acknowledgement can adopt its
own stored token. Existing historical completions are sealed without an owner or
token. Real Player/GameGuild producers use this path; interface-only compatibility
producers retain their existing process-local behavior.

Both additive provider migrations and current snapshots are scaffolded offline:
MySQL `20261103170256_RealmFirstClaims`, SQLite `20261103170302_RealmFirstClaims`.
Each Up creates only `realm_first_achievement`; each Down drops only that table.
Scaffolding used EF10.0.10 with an explicit provider/version and no connection.

`producers-corrected.trx` passes **51 checks**, including nine new arbitration
cases, existing character publication/guild recovery controls and migration history.
The new cases cover separate registries/actors, rejected writes, lost
acknowledgements, historical closure, savepoint rollback and deletion of a winner.
Earlier intermediate failures remain in their original logs: a SQLite unsigned
ORDER BY in the new assertion, a guild local-name compile correction, and three
guild fixture failures because its short timer tick did not drain publication.
The fixture now awaits the actual guild publication method.

`migration-controls-corrected.trx` passes **nine model/upgrade controls**, including
three new cases. Both runtime models and current snapshots agree, keep stable
composite claim keys, and retain the prior GuildRank ValueGenerated.Never metadata.
EF's generated snapshot normalization removes redundant Never calls and reorders
the bank receipt block without changing those effective mappings. The SQLite
copy upgrades twice, preserves every old runtime table's rows/columns, closes
historical completions without owners/tokens and leaves the original file's bytes
unchanged. A generic-options compile correction is retained in the first control
log; it is not a behavioral failure.

The original provider candidate passes all nine original compiled character/guild
cases. Two independent-process scenarios also pass with five managed worker
processes: both writers reach the claim INSERT before either executes, one loses
with MySQL1062, its whole graph rolls back, and retry preserves both owners'
progress with one announcement. Lost acknowledgement retains the original winner;
a fresh process after winner deletion cannot reopen the claim.

The controlled MySQL 8.4.13 copied-upgrade proof passes. Both copies use the pinned
previous CreateScript (SHA256 `c451b033fbb69779e6c4c3dd2cad8a03f387d76d9991f359af2478db28f4af47`)
and a **synthetic** prior migration history. All65 old runtime tables, their DDL
and values, and the source copy remain unchanged. Migration and real global
achievement initialization each run twice; two historical claims close with
null owner/token/name and no entitlements are granted. This does not qualify an
actual deployed historical migration chain. The first two harness attempts are
retained: duplicate model seed copying, then omission of DEFAULT_GENERATED columns.
The corrected copy replaces destination seed rows and excludes only actual
generated expressions; both failed attempts cleaned up their owned schemas.

Two independent Source reviews found and corrected two enrollment gaps. Claims
already tracked without SQL now require the exact context, model instance and
original fields staged by the coordinator; arbitrary null closures/foreign winners
cannot consume an opportunity. Real Player completion retains its claim before
title/archive callbacks can synchronously save. `ownership-before.trx` reproduces
both ownership defects with one valid shared-context control; `callback-before.trx`
reproduces the callback gap with three controls. Four new public-flow regressions
cover these boundaries. Corrected Spec and Standards reviews have zero remaining
findings against 27 paths, manifest SHA256
`42717c483353b17016480212b6a569250bd59e4791f46fb0522c117ed038ca94`.

`affected-final.trx` passes237 tests, including all 16 new cases and existing
achievement/guild/reward/quest/experience/migration controls. The full solution
build has zero warnings/errors. Final-binary process scenarios pass; provider
coverage is 12 passes plus one recovered case on the same final test/owner DLLs.
The retained first final-provider gate failed only during the last case's schema
creation because the disk was full, before its body ran. Incomplete generated
probe outputs were removed, and generated task artifacts were compressed without
changing their bytes. The single failed provider case then passed unchanged.
The owned MySQL generation and listener are absent after graceful shutdown.
The coherent full test gate passes. Earlier upgrade evidence uses an earlier
test/Game copy with the same final Database and migration DLLs; its boundary is
versioned rather than attributed to a different test assembly.
Source tests will not establish deployed multi-server or retail completeness.

Defaults remain 15:1/17:1, with no automatic 44 or seven baseline character grants.
Computer Use and native client launch remain stopped. The broader Goal is active.

## Final qualification

`verification.json` binds **11,970 full cases**, zero failures/skips, including
all 11,954 preceding labels, all 16 new cases and all 237 affected cases. The full
`Source/NexusForever.slnx` build has zero warnings/errors. Both corrected Source
reviews are clear across 27 paths. Source/Tools hash
`80f7799c3fc334b2821f480d7404c4f663d97616c543825b03fbab7d857e11d1` covers 6,260 inputs;
all 11 owner/test/provider DLL triples and test assembly
`5a456e5ae6277bae8355bf63ad0b9f75cca77ac0d34bff03e63d49d231a064f2` match the executed candidate.

Configured MySQL coverage is 12 retained passes plus one recovered case on the
same final test/owner DLLs, with the disk-full setup failure retained. The two
final independent-process scenarios pass across five workers. Previous nine-case
provider/process evidence remains separately versioned. Both model snapshots and
the copied SQLite upgrade pass; the controlled MySQL copy preserves all 65 old
runtime tables and uses synthetic migration history. The final database/migration
DLLs match the upgrade copies. All 18 recorded provider workload generations and
the owned server/listener are absent. No original database or deployment changed.

Observed validation time: full build 32.8s; affected test
execution approximately 71s; final process proof 39.5s; provider 145.7s plus 11.6s
single-case recovery; copied MySQL upgrade 19.3s; full gate
1177.8s. No unchanged full gate was repeated. The historical
continuation verifier's expected Source divergence remains recorded separately.
These results do not qualify a deployed WorldServer, live broker, restart outbox,
actual retail tables, native client timing or retail completeness.

## Next integrated outcome

`artifacts/continuation-20261103/achievement-title-publication-probe/results.json`
records an executed real Player achievement grant with a real TitleManager and
controlled reward metadata. A title update packet is sent before any save. An
owned trigger rejects the subsequent complete save, leaving title, achievement
and claim tables empty while the client has already received that title packet.
Removing the trigger and retrying commits all three rows. This is the next
companion-publication defect; the arbitration milestone does not claim to fix it.
Archive/tradeskill publication, restart/outbox and native/retail gates remain open.
