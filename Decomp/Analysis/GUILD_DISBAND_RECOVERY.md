# Guild disband recovery and upstream #401 reconciliation

The implementation after checkpoint `4dd8bf56` passes all 170 affected guild/housing
tests, including 18 new regressions, and **all 11,666 full tests**, zero failures or
skips. Every prior 11,648 test label is preserved. Both Source review axes have zero
remaining findings. The full solution build passes with zero warnings/errors in
29.70 seconds; the test command takes 823.28 seconds (runner: 13 minutes 31 seconds).
This qualifies the source/private-SQLite outcome and reconciles the current #401
cache-cleanup requirement. It does not establish retail or live-broker completeness.

Frozen Source/Tools SHA256:
`55fc582193fb9cc6166e34b8476853703486abd763ec15dce97e5098e2193dc2`
over 6,207 inputs. Source review patch SHA256:
`aaaf265a31040bd90d35891e18d5e9bae3d0ddc4314e8658e06003041aeaf083`.

## Observable acceptance

The real public guild-operation disband and periodic GlobalGuildManager save
must retain an accepted existing-guild tombstone through failed or rolled-back
saves. Internal member-removal/disband cleanup must wait for confirmed persistence.
If broker publication fails, retain the original actor/name reservation and retry
only messages without a successful publication acknowledgement; do not rewrite an
already-confirmed tombstone. Release every name-index entry owned by the deleted
guild only after this cleanup completes. Preserve the existing soft-delete archive
of guild members/ranks and the never-persisted create/disband cancellation path.
Community residence recovery must continue to pass its existing integration tests.

## Executed starting defects

`artifacts/continuation-20261102/guild-disband-recovery/before-real.trx` retains
three executed failures against production4dd8bf56:

- Public disband published GuildMemberRemovedMessage and GuildDisbandedMessage
  while the database still contained an active guild.
- A trigger-rejected periodic save consumed PendingDelete, preventing a later
  timer retry and leaving the name reserved.
- A caller-owned transaction rollback also consumed PendingDelete.

`before.log` is a preliminary fixture compilation failure (private packet setters),
not a behavior negative. The corrected fixture reads ClientGuildOperation through
its actual packet reader and enters GlobalGuildManager.HandleGuildOperation.
`candidate-initial.log` retains a candidate compile correction: PendingDelete is
concrete GuildBase state rather than part of IGuildBase. `disband-focused.log`
retains a ChatChannel.ChatId fixture compilation correction.

Three further executed failures in `review-negatives.trx` reproduce the first
review finding and an associated cancellation defect: an unflushed transaction
commit and a detached/suppressed write could qualify deletion, and a direct save
of a canceled create could insert an active guild. The corrected witness requires
the retained tracked fields, matching tombstone SQL, an observed flush and terminal
success. It reuses CharacterColumnSaveIntent and the existing context validation/
completion contract. The common immutable tombstone now belongs to GuildBase;
Community keeps its residence/removal lifecycle without a conflicting renderer.

`corrected-focused.trx` retains 54 passes/two failures: ChangeTracker.Clear left
an old entry handle, and an outer commit after community savepoint rollback was
incorrectly rejected. Current tracker membership is now required, while a rolled-
back savepoint retains intent without acknowledging deletion. `restage-before.trx`
then reproduces the review-found equivalent same-context restaging regression.
The retained enrollment now makes that repeated Save a no-op until terminal
completion, when its context and event handler are released. The seven starting/
review reproductions remain failures in their historical records; the two
intermediate correction failures remain separate.

## Candidate behavior and checks

The periodic manager awaits broker publication after the confirmed save. An
acknowledged member cleanup is retained across a later disband-publication failure;
the original guild/name reservation remains until completion. A pending publication
holds the existing in-flight save guard. A confirmed tombstone is not rewritten on
that retry. The five tombstone values are captured at accepted disband, with whole-
second timestamp precision matching the existing MySQL datetime column.

The 18 new cases cover public operation/periodic commit and failure, caller rollback,
unflushed/suppressed commits, SQL tampering before commit, direct confirmed saves,
equivalent same-context saves, failed/held publication, duplicate disband, original
name-index cleanup after rename, and never-persisted cancellation. Four cases use
the actual current ChatServer create/disband handlers for Guild, Circle, WarParty
and Community channels. They verify referenced main/officer cleanup, member cascade,
duplicate cleanup, unrelated-channel preservation and recreation without a stale
name cache. The local handler checks do not exercise a message broker.

The final affected record is `final-affected.trx`: 170 passed, zero failed/skipped.
`verification.json` binds the full result, all 18 new/170 affected labels, all prior
11,648 labels, 6,207 unchanged final inputs and 11 owner/test-copy DLL pairs. Database
source/schema are unchanged; rebuilt DLL identities retain separate provider limits.
Earlier narrower runs remain retained; they are not added together. The full-build
runner initially stopped before starting tests because task-copy return values
polluted its exit-code variable. That bookkeeping failure is retained separately;
the corrected runner reused the passed build and started the first full test run.

## Upstream evidence and remaining limits

Official [PR #401](https://github.com/NexusForever/NexusForever/pull/401), still open
at head `e35d636cf1ef9cdb64092859cc24bfdb4b8657d6`, proposes freeing legacy guild/chat
name caches at disband time. Its four changed-file patches are retained beside
the regressions. The current source instead uses the separate ChatServer,
ChatChannelRepository and identity-referenced GuildDisbandedHandler. There is no
reason to port the obsolete in-process chat cache or free a persisted name before
the current transaction succeeds. The current-head cache-cleanup requirement is
source-reconciled through this verified public/periodic path and actual current
ChatServer handler controls.

This is a source/private-SQLite recovery outcome. No live broker, current-binary
MySQL, process restart/outbox durability, socket/native client or retail behavior
is qualified. An acknowledgement lost after actual broker delivery may cause a
duplicate cleanup attempt; successful in-process acknowledgements can be retained.
At the disband checkpoint, ordinary field/member recovery and separate rename
index/collision flows remained actionable. The later name gate below completes
the rename slice; ordinary other guild recovery remains open. Defaults15:1/17:1, no44
or seven baseline character grants and the native client/CUA stop remain intact.

## Name lifecycle recovery: verified

After `a5c6d378`, four new executed regressions reproduce the stale accepted-name
index, a conflicting paid rename charging currency, a failed periodic save losing
rename intent, and an unloaded community residence retaining its old name. Evidence:
`artifacts/continuation-20261103/guild-rename-recovery/before-real.trx`. The earlier
missing IWorldSession import is a fixture compilation failure, not a behavior result.

Acceptance for this integrated outcome: reserve a registered original actor's target
name before the existing debit; reject known or held conflicts without charging;
release an unused reservation when payment fails. Retain current and possible SQL
name aliases until a verified flush/commit, including later accepted edits. Persist
the community residence name with the guild rename even when its map is unloaded.
Preserve route, tracked-field, explicit transaction, savepoint and same-context
ownership, existing fee selection/text policy, and disband/archive behavior. A first
confirmed create must initialize name ownership and release obsolete draft aliases.

The candidate uses an actor-bound disposable name reservation, atomic field/index
assignment, loaded database provenance and exact name SQL/tracked-field witnesses.
Community rename joins the existing residence save graph and captures its name for
retries. First-create confirmation observes the existing full save; it does not add
whole-create graph recovery. Ordinary scalar/rank/member/data and failed/uncertain
first-create recovery remain actionable requirements, as do restart/outbox durability,
atomic fee/guild/residence settlement and live broker/provider/native/retail gates.

The first candidate build required a System import. Its corrected execution retained
65 passes/one retry failure. A direct-save control exposed the mutable baseline:
attaching and changing the loaded model also changed the supposed prior SQL name.
The scalar string is now captured before staging. The subsequent 72 focused cases
pass, including ten new cases and the prior community/disband/handler controls.
The first-create name handoff adds one more case; all 181 affected cases pass.
Its SQL witness also uses ordinal text comparison after reading the row, rather
than relying on provider collation. At that intermediate stage, provider execution,
final review and the full gate were pending; the final result below supersedes
that candidate limit.

The Source review then found three coupling defects. `review-before.trx` retains
three executed failures: a new community/root rename assumed an existing SQL row;
same-name residence intent bypassed the loaded route; and a residence attach error
left a guild-only rename committable. The correction distinguishes an Added row's
pre-flush absence, validates the route for residence-name intent even without a
guild-name delta, and rejects the whole context after partial enrollment fails.
All 184 affected cases, including 14 new cases, now pass. The first-create residence
fixture uses a real pending-create model without client plot tables; this does not
qualify complete housing creation or retail data. Re-review/provider/full gates
were pending at that intermediate checkpoint; their final results are below.
The seven starting/review negatives and separate mutable-baseline correction
failure remain retained rather than rewritten as passes.

The final name-lifecycle candidate passes **11,680 full tests**, zero failures or
skips, including all14 new/184 affected labels and every prior11,666 label.
Both Source review axes have zero remaining findings, including the three P1
coupling corrections and the fixture-only configured-strategy supplement.
Final Source/Tools SHA256: `ca81304622f5e249ecbefa041d244b11f857a0565656b6cfc951539cf57b02df` over6,213 inputs;
review patch: `28549d571a7bc7f492dc94cc4d5bad6c7cb1d6301ae24a3e7192c629921982c6`. Eleven owner/test-copy pairs and
the frozen test assembly verify. Guild load-provenance database source changed;
models, schemas and migrations did not change.

Configured MySQL8.4.13 name coverage passes as12 retained outcomes plus2 corrected
manual-transaction fixture outcomes on the same production DLLs. The first14-case
run retains its two MySqlRetryingExecutionStrategy admission failures. Both
transaction fixtures now enter the configured strategy and reject any second
invocation before gameplay callbacks replay; assertions and deadlines are unchanged.
The corrected two cases pass in18.65 seconds, alongside the original12 passes
from114.65 seconds. Initial listener admission failed during161-second startup
before any case ran; the same owned server later became ready. This is separate
from the two fixture failures. The owned process/listener and all recorded full-run
workload generations are absent after validation. No original database/config was
changed. Evidence root: `artifacts/continuation-20261103/guild-rename-recovery/`,
with verification.json, provider-verification.json, full.trx and retained failures.

Final full build: zero warnings/errors in31.08 seconds. Full test
command: 713.95 seconds. The earlier passed build (27.68 seconds) is
retained separately; the final build follows the material test-fixture correction.
No unchanged passing full suite was repeated. The184 affected cases and final full
validate the real handler, periodic save, name cache, residence graph and existing
recovery/disband controls. This does not qualify atomic fee/guild/residence financial
settlement, ordinary other guild fields/ranks/members/data, failed or uncertain
first-create recovery, copied upgrades, live broker, restart/outbox, raw-ADO or
distributed writers, native gameplay or retail completeness. Defaults15:1/17:1,
no44/seven baseline grants and the client/CUA stop remain intact.
