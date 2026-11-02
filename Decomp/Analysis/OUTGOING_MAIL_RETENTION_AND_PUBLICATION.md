# Outgoing mail retention and publication

1 November 2026 destination continuation. All 10,620 full-suite tests pass with
zero failures/skips and a zero-warning/error full rebuild. All 10,586 prior test
names remain; 34 new cases pass. Both the pre-rebuild and refreshed-binary focused
runs pass all 49 outgoing/legacy delivery cases. The full run took 10m59s; the
longer duration alone did not establish a timeout or cause claim.

Normal outgoing mail now retains its original object and specific membership
save receipt across disposal, rollback, no-flush outer commits, savepoint rollback
and lost acknowledgement. Delivery requires the complete acknowledged SQL header,
attachment membership and Item witnesses. Recipient Character/account ownership
is checked again before flush and commit. Mailbox save ownership excludes return,
delete, cash and attachment actions while the unit is unfinished. Later read and
Item intent remains dirty and can be saved through the receiving mailbox.

Publication runs through captured source and receiver actor queues. Offline mail
remains in the sender's retained queue until inbox acceptance or explicit stale-
actor quarantine; subsequent normal saves therefore preserve later dirty fields.
The weak database/mail registry coordinates both initial login and periodic SQL
polling. A small internal delivery-owner interface performs final acceptance under
the same gate as sender enrollment, so a callback readiness check cannot authorize
a transfer across a newly started save. Replaced source/receiver sessions or mail
managers suppress stale notifications; authoritative SQL polling remains available.
This is local actor ownership, not a cross-process concurrency token.

Inbox saving copies available/pending membership under a short metadata lock and
performs SQL outside it. Worker saving no longer removes live inbox entries.
Delivery callbacks and Inventory operations execute outside that metadata lock.
Creature mail, its attachments and the granting Character changes continue to
share the Character transaction: rollback cannot leave a separately committed
grant. Both instant and delayed self-delivery retain later attachment intent.

Sixteen distinct failing regressions were executed across eight separately frozen
source epochs. Every row retains its complete source/tool/DLL manifest, ZIP and
eight owning-project binaries in the ignored local mail-outgoing evidence directory.
These historical proofs retain their original inputs.

| Epoch | Inputs | Failed | Passed controls | Reproduced boundary |
| --- | ---: | ---: | ---: | --- |
| negative | 6070 | 3 | 1 | Queue lost on rollback; receiver publication before commit |
| poll-negative | 6073 | 1 | 7 | Receiver polling bypasses original actor adoption |
| guards-negative | 6075 | 3 | 12 | Return/delete during save; recipient account rewrite |
| snapshot-negative | 6077 | 1 | 1 | Pending arrival invalidates live enumeration |
| login-negative | 6080 | 2 | 37 | Initial login creates a duplicate owner |
| offline-negative | 6081 | 3 | 41 | Offline dirty intent lost; callback crosses later save |
| dead-negative | 6081 | 2 | 46 | Queued delivery survives source actor replacement |
| accept-negative | 6081 | 1 | 48 | Readiness check cannot authorize final inbox acceptance |

The final verifier passes all 6081 current inputs, complete archives and eight
DLLs, plus the full/focused results and all eight executed negative epochs. Earlier 19/23/30/39/44/48-case
passes remain intermediate observations, not full-suite qualification for later
revisions. Compile/import failures and the rejected duplicate-target patch remain
recorded; that rejected patch made no changes. Codebase-design informed the
internal owner/receipt interface; regressions use caller APIs and real private
migrated SQLite transactions.

The published 10277-source handoff verifier rejects this intentionally changed
working source. Its historical manifest stays unchanged; the new input verifier
separately checks the current archive, binaries, executed failures and final run.
The upstream refresh at 31 October 23:20:27 UTC still has 56 open PRs, all 56 heads
unchanged, including the previously reconciled #608 and #609.

Manual legacy return/delete reply timing, attachment-taking and marketplace
economics, durable recovery after a consumed uncertain command, and broader
feature work remain. No fresh MySQL/configured-strategy, copied-upgrade,
socket/deployment/load, native presentation or retail completeness is established.
Defaults remain 15:1/17:1 with no automatic 44 or seven baseline character grants.
No push or Computer Use/native client launch was performed; those stops remain.
