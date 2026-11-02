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
Ordinary non-disband guild field/member recovery and the separate stale rename
index/new-name collision path remain actionable audits. Defaults15:1/17:1, no44
or seven baseline character grants and the native client/CUA stop remain intact.
