# Player mail transaction and inbox recovery

31 October 2026 continuation, local branch `legacy-di-storefront-hardening`,
base `a50dfa1347b9fb4d5b3d40c3454dec2e7b82ab0a`. The handoff commit preserves
the committed Source tree from the original29384228 checkpoint. Implementation
changes remain local and uncommitted. The Goal remains active.

## Implemented boundary

Real Player sends now capture the mail header, exact attachment identities,
carried/new/bank Item writes, bank withdrawal and complete Credits debit in one
retained `BankSaveCommand`. The mail identity is allocated once, before command
replay. Duplicate attachment GUIDs and checked postage-plus-gift affordability
are rejected before detaching items. The existing COD/gift exclusion, mailbox
range, social entitlement, soulbound and equipped-item checks remain. This does
not establish native permission to mail from any additional UI location.

`Inventory.BankMail` prepares the complete inventory snapshot and ordered
publication batch. Its detached attachment route explicitly requires a real
Item with no Character owner, location None and no deletion intent. Ordinary
bank moves retain their existing owner/deletion checks. An attachment-free send
still has its typed mail companion and locked source Character identity; other
empty bank commands remain rejected. A missing Character database cannot
acknowledge a companion operation. Currency adoption also checks a singleton
confirmed stored value before changing the live amount.

`BankMailDelivery` captures all17 header scalars and each attachment's mail id,
index and Item GUID. It reuses the existing actual-commit row ownership,
uncertainty and tracked/SQL witness machinery. Every fresh attempt validates the
recipient's nondeleted Character/account and exact attachment membership. Header
rewrites, extra attachment rows, allocator collisions, changed source accounts
and changed/deleted recipients reject the complete transaction. MySQL datetime
normalization is expressed in the participant, but fresh provider qualification
has not been run. SQLite exercises actual transactions and failure triggers.

Packets, live debit adoption and recipient notification wait for confirmed SQL
and the original sender/session/account/realm/inventory actor. Failed SQL keeps
the stored bank stack and wallet intact and requires authoritative reload of the
prepared sender. A possible committed outcome is never overwritten by restoring
the old in-memory snapshot. Dead senders settle durable rows without success
packets. Mail/attachment creation flags clear only after commit proof, preventing
a recipient's later ordinary save from inserting them again.

## Recipient delivery

The online recipient receives an original-object notification on its actor
through `EventQueue.EnqueueThreadSafe`. Both scheduling and delivery validate the
actual recipient Character, original session/account, realm and MailManager/
inventory identities. Cross-actor producers do not mutate the ordinary Queue.

Production Player construction supplies a Character context factory. A single
in-flight,30-second inbox refresh reads the recipient and up to128 unseen mail
rows with their attachment Items in a private serializable transaction. Read
failures retry on a later actor tick; stale actors publish nothing. Known IDs
suppress repeated notifications, while each query considers unseen lower IDs,
so commit ordering is not mistaken for allocation ordering. The query orders
by CreateTime without narrowing unsigned IDs. SQLite's unsupported ulong order
and ToString translations were caught and removed during intermediate gates.

A database-scoped weak registry retains the original locally committed mail
objects through actor adoption, so a racing refresh cannot replace their later
dirty Item intent with freshly hydrated copies. Pending local operations wait;
quarantined/dead senders recover from SQL. Registration is released after delivery
or quarantine; dead weak entries are pruned. Confirmed incoming pending mail is
included in ordinary saves, preserving later dirty attachments during delayed
delivery. The pending-mail timer now explicitly uses one world second; world
ticks are seconds, rather than the previous numeric1000 duration.

## Verification and remaining work

**10,421 full tests pass**, with zero failures/skips and223 bank cases. The full
solution rebuild passes with zero warnings/errors.31 tests were added:29 bank/mail,
one pending-mail timer and one concurrent event-queue case. The79-case focused
mail/event gate passes; all10,390 earlier full-suite cases remain present.

The new full-source binding, frozen binaries, build log, full TRX, focused79-case
gate and eight executed pre-implementation negatives are under
`artifacts/continuation-20261031/mail-handoffs`. `verification.json` records the
completed full gate and rechecks all6,032 source/tool/binary hashes. Every test
from the prior10,390-test gate must remain in that full result. Constructor,
provider-query and fixture/compile failures are retained separately and are not
counted as pre-change behavioral negatives.

Regressions cover each SQL member's rollback, commit replay/lost acknowledgement,
dirty/deferred currency, later attachment scalars/codecs, delayed-mail saves,
new/existing carried items, attachment-free sends, recipient polling, lower-ID
late commits, stale managers/sessions, cross-wired recipients, changed SQL owners,
mail rewrites and unexpected attachment membership. The external event queue has
a concurrent-producer exactly-once drain regression.

This qualifies the source/SQLite player-send boundary. It does not qualify fresh
MySQL configured retries, copied upgrades, deployment, connected sockets, native
UI, large-history/throughput behavior or retail completeness. Legacy creature
and other outgoing mail, return/expiry, gift/COD collection, attachment taking,
marketplace settlement and their general save/publication outcomes still need
separate audit and hardening. `MailItem.Save` and legacy outgoing queues retain
their older nontransactional outcome behavior outside this new creation path.
Broader Contract/retail, track35 and assessment work remains, along with newly
discovered [upstream609 reconciliation](UPSTREAM_609_COMMUNICATOR_RECONCILIATION.md).
Defaults15:1/17:1, no automatic44 or seven-character grants, no push and the
Computer Use/native-launch stop remain in force.

### Legacy header and deletion outcome follow-through:10520

Eight new SQLite cases executed seven failures and one later-mutation control
before implementation. They reproduced discarded read/currency/recipient intent
after abandoned enrollment or outer rollback, deletion and expiry that never
reached SQL, and a loaded owner updating a header after its recipient changed.
The exact6054-input baseline, complete archive, eight DLLs and TRX are frozen
under `artifacts/continuation-20261031/mail-normal-save/negative-*`.

The current source reuses `QuestRowPersistence` for the complete17-scalar mail
header, including immutable SQL witnesses, per-row leases and fresh-context
uncertainty recovery. A shared `MailHeaderState` keeps the existing player-send
companion and ordinary header owner on the same row-lease identity. Player-send
adoption supplies its confirmed SQL header and database; ordinary retries retain
their first execution database. Loaded raw ContentType values remain unchanged
while the public resolved display value is preserved. MySQL date encoding uses
that session's DATETIME conversion, retaining the chosen timestamp through retry;
no fresh MySQL qualification is claimed.

`MailDelete` and an already-queued inbox deletion now enter retained tombstones.
Expired/deleted messages remain there through disposal or rollback, and leave
only after confirmed SQL absence. Immediate legacy unavailable/result packet
behavior remains; this slice does not claim post-commit action publication.
Header mutations share the capture gate, and older committed captures cannot
consume later mutations. Existing enrollment tests now use seeded private SQLite
rather than an unconnected MySQL context, keeping their assertions/test names.

The93-case intermediate mail gate and302-case final mail/bank gate pass.
The complete solution rebuild has zero warnings/errors. Twelve further cases cover creation
commit/rollback, lost acknowledgement, repeated flushes, conflicting owners,
tracked/SQL rewrites, tombstone rollback and execution database retention. One
additional queued-deletion case is included in that final focused gate and the
passing full run. **10,520 full tests pass**, zero failures/skips, with all10499
prior case names retained. All6,056 current source/tool/binary inputs, eight frozen
DLLs and their complete archive verify; the6,054-input pre-implementation archive
and eight negative binaries verify separately. The exact report is
`artifacts/continuation-20261031/mail-normal-save/verification.json`. The10499
communicator proof stays historical and has not been rebound.

Attachment row/deleted-attachment intent, legacy outgoing/recipient publication,
return/creature sends, gift/COD collection, attachment-taking economic boundaries
and marketplace outcomes remain separate work. Header persistence alone does not
qualify their complete transaction or publication behavior. Existing receipts,
player-send ownership, defaults15:1/17:1/no44/no seven grants, no push and the
Computer Use/native-launch stop remain in force.

### Attachment aggregate outcome follow-through (10542 source checkpoint)

The current source adds22 attachment/aggregate regressions after the10520 header
checkpoint. All116 mail cases pass before the final solution gate. The complete
solution rebuild has zero warnings/errors and324 final bank/mail cases pass.
The default-concurrency full run reports10541 passes and one existing Challenge
producer20-second drain failure. All16 producer variants then pass unchanged in
isolation. A controlled full run with xUnit.MaxParallelThreads=2 passes all10542
cases. The default retry without concurrent heavy verification also reports10541
passes/one producer failure, masked by a persistent SQLite sharing violation
during teardown. Thus verifier overlap is not a necessary condition. A subsequent
test-only failure diagnostic records queue/task/thread-pool state at the unchanged
deadline; its source/build/test epoch is separate from the6060-input proof. That
subsequent full default run passes all10542 cases, zero failures/skips, with a
zero-warning/error rebuild and16 focused producer passes. No diagnostic fired,
so this passing run does not establish the intermittent failure's cause. The
separate6060-input archive and verifier are in mail-attachments-diagnostic;
five new unbuilt cash tests are explicitly outside its executed case set.
Contention is a hypothesis, not a confirmed cause. No deadline/product code was
changed. The failed case's end is23.889ms after the concurrently running handoff
source-hash verifier wrote its mismatch log; the timing correlation is retained
in `deadline-investigation.json`, without a causal claim. The final6060-input snapshot, eight DLLs and full archive are frozen
before those runs under `artifacts/continuation-20261031/mail-attachments`.

The corrected initial baseline executes eight failures and three controls:
create/delete intent lost through disposal or outer rollback, conflicting loaded
owners, changed item links, and extra membership inserted before enrollment or
by raw SQL after flush. Its6057 inputs, complete archive and eight DLLs are frozen
with `negative2.trx`. An incorrect later Item-count assertion was corrected before
that authoritative run; the earlier attempt remains separate. Intermediate105
mail cases then pass. Additional aggregate probes execute three introduced
failures and one passing membership-trigger control: an implicit header rewrite
and two suppressed saves acknowledging unwritten new/dirty Items. That6059-input
state and its exact DLLs/archive are frozen with `aggregate-negative.trx`.
Earlier implicit-only attempts and analyzer/fixture corrections remain retained.

`MailAttachment` now shares its immutable3-scalar row type and exact row keys
with the player-send companion. Row/group leases and retained possible outcomes
exclude conflicting contexts/owners and permit fresh-context recovery. Parent
mail deletion stages child deletion explicitly. Deleted attachment objects leave
the parent's retained set only after confirmed absence; Item retention follows
the existing behavior. Header and leaf creation adoption use the already-confirmed
player-send database identity and exact row snapshots.

The mail aggregate retains the complete sorted membership baseline and all
possible staged/flushed sets through ambiguous outcomes. It validates the exact
tracked set before flush, and header/set SQL witnesses before implicit or
explicit commit. Standard Item owners supply copied enrolled snapshots covering
all12 persisted scalar/codec fields. These validate tracked ownership/originals
and SQL storage without consuming later Item revisions. A suppressed save can
be acknowledged only when the complete aggregate already matches SQL; missing
writes reject the context and preserve intent. A legitimate no-op second flush
remains valid. This observer is limited to the mail aggregate; ordinary non-mail
Item save outcomes remain a separate audit.

Further cases cover savepoint rollback followed by an outer commit, repeated
flushes/restaging, later live Item changes, later attachment deletion, tracked
snapshot/original rewrites, and a raw SQL Item transfer after flush. Full-suite
qualification now includes the separately bound diagnostic/default full pass. Existing
10520 and earlier checkpoints retain their original bindings without rebinding.

Legacy outgoing queue/recipient publication, return/creature sends,
attachment-taking economic atomicity and marketplace outcomes remain. Gift/COD
collection now has the narrower in-progress source gate below.
Header/link/Item persistence does not establish those action/publication boundaries.
Fresh MySQL/configured retries, copied upgrades, sockets, deployment/load, native
presentation and retail completeness remain open. Defaults15:1/17:1/no44/no seven
automatic grants, no push and the Computer Use/native-launch stop remain.

### Gift and COD receipt boundary (10565 source checkpoint)

Three baseline failures and two controls execute against6061 frozen inputs,
their complete archive and eight DLLs under artifacts/continuation-20261031/mail-cash.
Wrong-mode actions mutate the paid flag/wallet, and an unrelated player send
persists a collected gift's wallet while its SQL receipt stays unpaid. These are
source economic failures, without a retail error-code claim.

The current source freezes the existing17-field receipt and complete attachment
membership, pairs it with a bank currency companion, and includes the COD sender
settlement in the same retained command. Live paid/wallet state and success
packets wait for commit and actor adoption. The captured MailManager and exact
available-mail object extend the bank publication's original-actor guard. Read
flags can arrive later and remain dirty. Unfinished cash work excludes deletion,
return, attachment mutation and normal mail saves; expiry waits. Wrong-mode
actions receive existing generic refusals as server policy.

All23 focused cash cases pass, including SQL/trigger rollback, lost acknowledgements,
retry identity, pre-commit publication, later read intent, stale MailManager,
foreign header/membership and two suppressed header/wallet primitive flushes.
The suppressed-flush cases exercise the enrolled companions directly, without a
COD settlement/inventory action, and do not claim that broader route. Existing116
mail cases also pass at the first implementation stage. The reviewed full
solution rebuild has zero warnings/errors; all10565 full cases pass with no
failures/skips, covering347 mail/bank cases, and23 reviewed cash-focused cases
pass. Complete6066 reviewed inputs,6061 negative inputs and eight DLLs per state
verify. A preceding final-labelled candidate archive/336-case pass is preserved;
review then separated the worker's reference-only manager guard from actor-only
mail dictionary checks before the separately bound full gate. All10542 earlier
cases remain. NuGet's47-project current advisory query reports no vulnerable
package entries; the21:25UTC upstream check retains56 unchanged open heads.
The published10277 handoff verifier deliberately rejects this working delta;
its historical manifest was not rebound. The first local verifier attempt stopped
on Windows' implicit encoding for GitHub JSON; explicit UTF-8 fixed the verifier
without changing any Source/test/binary input. Ordinary non-mail Item
outcomes, outgoing queues/recipient publication, attachment taking, marketplace,
fresh MySQL/provider retries/upgrade/load/native and retail gates remain separate.

The full rebuild produced different Auth and Character DB DLL hashes from the
vendor checkpoint despite no DB source changes. REA static inspection of the two
Auth artifacts confirms different SHA256/MVID identities with matching reported
metadata counts, assembly version and target framework. This is identity
evidence, not semantic equivalence or a fresh database upgrade proof. Historical
provider/upgrade bindings must not be presented as bindings for these new DLLs.
The exact observations are saved as `auth-static-0.json` and `auth-static-1.json`
alongside this checkpoint's evidence. No database migration or deployment ran.
