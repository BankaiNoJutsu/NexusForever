# Committed marketplace mail publication

Date: 1 November 2026. Source/private SQLite evidence only. Full solution
rebuild and refreshed test copies pass with zero warnings/errors. All10749 full
tests and297 reviewed mail cases pass, including every prior case and23 new
publication cases, with zero failures/skips.6104 final inputs, all four source
changes, eight owning/test-copy DLLs and the complete archives verify against
the pinned two-axis review. See the local verification.json for exact bindings.

## Reproduced failure

MarketplaceMailDelivery.PersistMail called recipient.MailManager.EnqueueMail
directly after SaveBlocking. The caller's persistence catch also covered this
notification. A packet enqueue error could therefore return false and restore
an escrow Item's previous CharacterId even though its header, link and Item SQL
had committed. A worker caller also mutated the recipient mailbox and emitted
packets before the recipient actor updated.

Seven cases were executed against unchanged publication code: six failures and
one confirmed-SQL preparation control. The negative6102-input archive includes
only the added test file over custody10726. Initial test compilation used the
private SendAvailableMail method by mistake; the retained compile failure was
corrected to public SendInitialPackets before any baseline execution.

## Implemented boundary

The producer captures its normalized database identity during preparation.
After SaveBlocking returns successfully, notification has a separate exception
boundary. Lookup, route preparation and notification failure leave success and
the confirmed Item owner intact, with SQL inbox recovery available.

For a concrete mailbox, a fresh source context must match the committed route.
Actual Character AccountId and DeleteTime are checked before the receiver
accepts the notification. The receiver checks its own database route, account,
character and original manager/session/account/inventory/realm identity, then
queues through its captured EventQueue. The callback waits through an enrolled
mailbox or normal Character save and rechecks actor and queue identity. It
performs no provider work under mailbox metadata locks or on the actor callback.
Replacement drops the old notification; login/poll can recover persisted mail.
Nonconcrete adapters keep their existing path inside the post-commit catch.

Twenty-three focused cases pass, including actual item/commodity/credit
producers; original actor waiting; changed manager/session/account/account
session/account ID/inventory/character/realm/event queue/logout; held save scope;
soft-deleted or stale SQL account owner; mismatched receiver/source routes;
receiver context/player lookup/adapter/packet enqueue failures; login recovery;
single credit publication and true SQL rollback restoring escrow ownership.
The expanded draft's missing Identity namespace caused a retained compile
failure, corrected before the expanded run. No expanded test execution failed.

Both independent review axes report zero findings at Source/Tools SHA256
cc008fa4823903cc9d7565ed48c756de98585ebd724e1d5e4241db90ccd5ebc6.
Exact local evidence is in artifacts/continuation-20261031/marketplace-publication.

## Explicit remaining requirements

This does not resolve SaveBlocking's own lost-acknowledgement ambiguity or
replay arbitrary additionalSaveAction callbacks. Marketplace listing/wallet/
debit/refund/proceeds transactions and online/offline delivery still require
their complete captured settlement. Cross-process CAS and durable packet replay
after consumption remain separate. Account ownership can change after the
post-commit lookup; this actor guard is not a distributed ownership lock.
The producer API supplies no independent source-realm authority.

No configured MySQL/upgrade/deployment/socket/load/native/retail completeness
is claimed. Computer Use/native launch remain stopped. Defaults15:1/17:1,
no automatic44/seven baseline grants and the no-push instruction remain.

Upstream refresh02:54UTC:56 open PRs,609 newest; all56 PR heads and both official
branch heads match02:04UTC. Recent merged query since29 October returns598 only,
already reconciled. The historical handoff verifier was run and rejected the
changed working source at its checkpoint10277 equality assertion; it does not
verify this new slice. Its failure log is retained without rewriting history.
