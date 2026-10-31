# Character item actual-commit durability

Latest continuation: [Realm bank persistence and loading](REALM_BANK_CODECS_AND_LOADING.md)
qualifies checkpoint10277: **10,277 full tests**,18 new/207 focused,1,796 MySQL
cases (530 configured-strategy cases), four Auth race probes, four model gates and
31 compatibility probes; zero full-build warnings/errors. Six executed10259
negatives reproduce missing single-write save guards, discarded deletion intent,
changed-owner deletion and post-enrollment location restoration. Inventory bank
writes now share outcome tracking; deletion retains its real Item owner and exact
SQL witnesses. Overlapping outcomes retain earlier failures, while expiry waits
for active bank units without losing elapsed time.103 provider bank cases pass.
Both DB binaries remain identical to the10207 upgrade bindings; all726 Contract
quotes pass. Periodic bank dirty/expiry persistence, general asynchronous recovery,
post-commit publication, public07CE claims, retail reset edges and the broader Goal
remain. The8378 runtime, nine identities, six drained queues, original data/config/
schema and defaults15:1/17:1/no44 stay preserved. No staging/commit/push, native
launch or Computer Use. Q5597 remains unavailable; no repeat request or reset
policy question is pending. The full Goal stays active.

The following records the earlier item-durability checkpoint.

Latest30 October continuation: [reward Item enrollment/recovery](CHARACTER_REWARD_ITEM_ENROLLMENT.md)
passes7,307 tests,31 compatibility probes and47 final MySQL cases, including
new private stacks and retained merges. Production aggregate delivery remains open.

30 October continuation: [reward inventory preparation](CHARACTER_REWARD_INVENTORY_RESERVATIONS.md)
adds Bag/Item mutation guards, permanently rejected Character units and a serialized
item-ID allocator. The full7,254-case gate,31 compatibility probes and11 new MySQL
validation cases pass. The45 earlier item-specific MySQL cases below retain their
original binary scope; transactional reward delivery remains open.

2026-10-29. This slice preserves character-item intent until the Character database save unit reaches its actual terminal outcome. It does not implement the remaining Challenge reward-track progression, selection, reset or claim-receipt policy.

## Problem and retained negative control

Previously, `Item.Save` reset its dirty mask as soon as it attached an INSERT, UPDATE or DELETE to an EF context. `Inventory.Save` also cleared removed-item tombstones immediately. A later failed flush, transaction rollback or abandoned context discarded the only persistence intent even though the live item still reflected the change. A fresh context then omitted that change; relog restored the older database row.

The initial relational baseline is retained externally as [baseline-red.log](I:/GIT/NexusForever-assessment-report/remaining-goal/item-durability/baseline-red.log) and [item-baseline-red.trx](I:/GIT/NexusForever-assessment-report/remaining-goal/item-durability/item-baseline-red.trx): **10 failures and 1 pass out of 11 cases** against the original Item implementation. This is an executed negative control, not a source-only inference. The expanded tests retain those original scenarios.

## Save ownership and actual SQL snapshots

[`Item.Persistence.cs`](../../Source/NexusForever.Game/Entity/Item.Persistence.cs) keeps a loaded durable baseline, an active save registration and per-field mutation revisions. Staging no longer acknowledges an intent. The save registration observes `CharacterContext.SavedChanges` and captures the model and revisions which actually reached SQL; terminal completion settles only that flushed snapshot. This distinction covers both an outer commit with no `SaveChanges` and a second restage that has not been flushed when the outer transaction commits. `SaveChanges(false)` remains supported: EF acceptance of tracked states is distinct from database commit.

An older captured snapshot may commit while a later mutation remains dirty for the next unit. Revision comparison preserves later changes, including changes to different fields and same-field mutation sequences. Snapshot serialization preserves the existing microchip and rune codecs; it does not retain references to mutable rune-slot elements as the captured SQL value. Public item constructors, GUIDs, item packet formats, charge initialization, expiration and binding behavior remain compatible.

Repeated staging into one valid context reuses the owned tracked ItemModel. A fully loaded model already tracked by a mail/auction caller can be reused without replacing unrelated dirty columns. A partial/conflicting tracked model is rejected. Simultaneous staging by another context or another live Item instance with the same GUID is rejected; a sequential stale instance must still match its loaded durable baseline before writing. Conflicting persisted owner, definition or other baseline values fail instead of being overwritten by an unconditional upsert.

[`CharacterContext.SaveCompletion.cs`](../../Source/NexusForever.Database.Character/CharacterContext.SaveCompletion.cs) distinguishes successful flushes from terminal transaction completion. Caller-owned commit/rollback, transaction-only disposal and context disposal release registrations. An implicit transaction's terminal notification is deferred until the enclosing `SaveChanges` returns, so the flushed snapshot is available before owners settle. A throwing completion observer is logged and does not prevent the remaining callbacks from releasing their leases or turn a committed SQL unit into a reported rollback.

## Rollback, savepoints and uncertain completion

A failed or uncertain completion retains intent and the finite set of snapshots belonging to that live operation. A fresh context reads the durable row and accepts only a matching possible state before retrying. This permits a rolled-back create to be inserted, an already committed create to be recognized without a duplicate INSERT, and an already absent deletion to converge idempotently. It also covers the bounded lost-acknowledgement case without replaying unrelated context work. This is baseline reconciliation, not a new durable receipt or universal retry mechanism.

Rollback to a savepoint makes the existing context's tracked snapshot uncertain. Further staging of that active item in the same context is rejected. The owner must finish/abandon the transaction and retry in a fresh context, which can distinguish writes rolled back before the item flush from writes retained before a later savepoint. A conservative unsuccessful callback does not assert that every row is absent or unchanged.

## Inventory removal and buyback

[`Inventory.cs`](../../Source/NexusForever.Game/Entity/Inventory.cs) retains exact item references in its removed-item collection until the relevant deletion or canceled creation is settled. A failed flush does not drop the tombstone. Completion removes only the settled references captured by that save, preserving later removals and a newer occupant of the freed bag slot.

Create-then-remove handles both an unissued insert and removal while the insert is pending. If that insert commits, deletion remains pending; if it rolls back, the canceled lifetime can converge without creating a ghost row. Re-adding the same object through the real `Inventory.AddItem` buyback path cancels an unissued deletion or recreates the live row after a completed deletion. Tests cover re-add before SQL, after DELETE flush and after commit, without changing vendor prices, currency policy or packet ordering.

## Realm-bank compatibility

[`RealmBankManager.cs`](../../Source/NexusForever.Game/RealmBank/RealmBankManager.cs) coordinates real Item deposit/withdrawal with the same CharacterContext registration. Character-row removal and bank-row creation, or bank-row removal and character-row restoration, settle together. The live Item retains a bank-side baseline and validates pending transitions; a stale character save cannot recreate a bank-owned GUID, and withdrawal requires an enrolled bank-row removal. Bank writes check account, realm and item definition rather than replacing a conflicting row.

Loaded bank items now retain their actual Item2 definition and persisted scalar state. [`CharacterDatabase.GetNextItemId`](../../Source/NexusForever.Database.Character/CharacterDatabase.cs) considers the maximum GUID in both character items and realm-bank items before the existing ItemManager startup increment, preventing restart allocation from reusing a GUID found only in the bank. This preserves the existing in-memory allocator design; it does not introduce a durable multi-process sequence.

No entitlement values or defaults change. Shared-bank access/capacity checks continue to use existing entitlements, and baseline account entitlements remain outside this slice. There are no migrations or item schema changes.

## Verification and reproducibility

Focused relational coverage consists of **45 CharacterItemDurabilityTests**, **40 CharacterSaveCompletionTests** and **21 RealmBankItemIdentityTests**. The Item fixture uses a unique file-backed SQLite database per test, explicitly disabling foreign keys only for its synthetic isolated item fixture and disabling connection pooling. Durable assertions reload through independent contexts. Its internal provider-options constructor permits the external owned-schema MySQL proof to exercise the same controls without `EnsureCreated`; the three SQLite `RAISE(ABORT)` trigger cases require a provider-specific equivalent rather than being mislabeled as portable MySQL cases.

The item cases cover create/update/delete failures and retries, abandonment/disposal, savepoints before/after writes, outer commit without a flush, flushed-versus-restaged snapshots, same-context refresh, active and sequential stale owners, owner/type conflicts, exact-reference Inventory removals, buyback, codecs, charges, expiration and binding. The context cases cover callback ordering and terminal boundaries. The bank cases cover definition/scalar loading, allocation high-water marks, real transitions, rollback and stale ownership.

The final solution build passes with **zero warnings/errors**, and **7,019/7,019 tests pass** with none skipped. The executed final artifacts are [provider-lease-final-build.log](I:/GIT/NexusForever-assessment-report/remaining-goal/item-durability/provider-lease-final-build.log), [provider-lease-final-tests.log](I:/GIT/NexusForever-assessment-report/remaining-goal/item-durability/provider-lease-final-tests.log) and [item-full-final.trx](I:/GIT/NexusForever-assessment-report/remaining-goal/item-durability/item-full-final.trx). The final TRX SHA256 is `A6B7D137AFF8DA367906E0929C7D4AD2F305C458450682FC52D2BA9450E10622`. All **31 original fork compatibility probes** also pass against these frozen binaries.

The first MySQL replay, `MySqlCharacterItemProof_29g1`, retained a real negative control: 41 portable cases passed, then same-context duplicate ownership was incorrectly accepted because opening the connection removed its password from the reported connection string. `SaveDatabaseKey` now removes `Password`/`Pwd`, normalizes the remaining configured key parts and includes the provider. No lease key retains or logs credentials.

The final [MySqlCharacterItemProof_29g3](I:/GIT/NexusForever-assessment-report/remaining-goal/MySqlCharacterItemProof_29g3) passes **45 cases** against **169 frozen DLLs and 14 item-slice source files**, bound to the full-suite TRX hash. It replays 42 provider-neutral fixture cases, tests synthetic pre-commit failure and lost commit acknowledgement, and creates a real InnoDB deadlock with the actual item writer selected as victim (`1213`) before a fresh-context retry. Source and copied original rows in both item tables retain exact hashes. The proof uses a fresh owned two-table schema with no foreign keys into source schemas; it is not a migration or cross-Auth transaction proof. The original checkout remains clean. See [MYSQL_CHARACTER_ITEM_PROOF.md](I:/GIT/NexusForever-assessment-report/remaining-goal/MYSQL_CHARACTER_ITEM_PROOF.md) and `post-proof-freeze-audit.json`. Earlier `29g2` remains valid 45-case evidence with the external writer as deadlock victim; `29g3` strengthens that boundary.

Native client UI validation was not performed for this slice. Computer use remains stopped after Escape; SQLite or MySQL model assertions do not establish a native inventory/buyback UI observation.

## Deliberate limits

- Live leases are process-local and keyed by provider, normalized credential-free configured connection parts and GUID. Equivalent databases addressed through different aliases/settings, another server process, or arbitrary cross-relog history are not protected by that coordinator.
- An enrollment exception must propagate so the caller abandons the enclosing operation. Catching and ignoring it is not made into an atomic reward receipt; a caller could otherwise commit unrelated work while omitting the rejected item. Future Challenge claim receipts require a separate transaction design.
- Existing custom `IItem` adapters retain their compatible fallback path; they do not acquire the real Item implementation's baseline/revision protocol automatically.
- Raw public collection APIs do not become safe for arbitrary concurrent mutation. The existing mutation/serialization contracts still apply.
- Checkpoint10207 supersedes the old bank codec/relog/loading gap with additive columns and per-inventory ownership. Historical codecs already lost before that schema cannot be reconstructed. Checkpoint10240 also coordinates consumed merges and guards failed multi-item units. General asynchronous retry/post-commit publication and single-item/plain-deletion lifecycle remain separate work.
- This fixes bounded persistence intent and transfer ordering. It does not establish cross-Auth/Character atomicity, durable exactly-once item grants, cross-process optimistic concurrency, or complete Challenge reward tracks.
