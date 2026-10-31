# Realm bank codecs, relog loading and transfer witnesses

Checkpoint10277 adds tracked single writes, owned deletion and expiry barriers
to the split, merge, swap, codec and loading fixes from10259/10240/10219/10207. Access, capacity, item
identity, binding and entitlement policies are retained. The full assessment
Goal remains active; this is not complete bank or retail qualification.

## Single writes, owned deletion and expiry barriers: checkpoint10277

Six executed regressions fail against the exact10259 Game binary. Failed single
transfers do not permanently reject the following ordinary inventory save; bank
deletion discards the real Item's deletion intent; a changed account owner is not
protected by the GUID-only deletion route. A failed move notification can restore
the pre-enrollment location while SQL commits its captured destination. The
regression source/assembly, six-failure TRX and prior Game hash are retained in
`realm-bank-single/baseline`. Its initial ambiguous ItemDelete-overload compile
failure is separate from the behavioral result.

All Inventory single-transfer, bank Add/stack/use persistence calls now enter the
same outcome tracker used by swaps, merges and splits. Single moves retain their
SQL Task in the rollback boundary, so a later packet failure quarantines rather
than restoring their old location over a possible commit. Ordinary saves and
mutation/save leases remain blocked until these tracked operations settle, and
failed units expose the existing authoritative-reload guard.

Inventory bank deletion now retains a tombstone and stages the real Item through
the existing actual-commit owner. Character/account/realm/definition ownership and
exact tracked/SQL witnesses protect the removal from both storage domains. A
rejected delete retains its intent for a fresh-context retry; an acknowledged
delete settles before the next ordinary save prunes the tombstone. Never-persisted
Items can acknowledge absence without creating a phantom row. Inventory no longer
calls the GUID-only manager deletion entry. That trusted compatibility API is
retained unchanged and is outside this Inventory-path qualification.

The tracker combines overlapping saves rather than replacing the earlier Task.
Individual failures latch quarantine while the combined barrier still waits for
the other operations. A later success cannot conceal the first failure or release
an outstanding save. These cases exercise the real Inventory save/admission guard
with controlled Tasks, alongside provider-backed transfer/deletion regressions.

Expiry accumulation pauses application while a bank operation is pending or the
Inventory requires reload. Elapsed seconds remain in the accumulator, then apply
after successful settlement; expiry deletion uses the owned bank unit. Quarantined
inventories do not process expiry on subsequent updates. This does not add periodic
durable flushing of non-expired bank dirty fields, establish offline expiry rules,
or qualify every mid-update/native/session interleaving. Automatic recovery and
post-commit notifications remain work.

Qualification:

- Full solution build: zero warnings/errors; **10,277 tests**,18 new regressions
 and207 focused bank/item/context/packet checks pass. The bank identity suite has
108 cases;103 run with production MySQL connection-string retry configuration.
- **1,796 MySQL cases**:1,374 Character,140 Auth,179 dual-database and103 bank;
530 configured-strategy cases, plus four independent Auth race probes. Five
 existing SQLite/raw-explicit-transaction bank controls remain full-suite cases.
- Four provider model gates,31 original compatibility probes and726 real Contract
 quote choices pass. Contract ItemInfo/stacking metadata remains synthetic.
- 5,888 source/tool hashes, four changed paths versus10259, one added;169 frozen
 DLLs and54 copied case/helper sources. Both DB binaries are unchanged. The exact
10207 populated60/61/62-table Character and30-table Auth upgrade proofs remain
 bound to those identical binaries and were not rerun for this change.

The current evidence authority is
`I:/GIT/NexusForever-assessment-report/remaining-goal/realm-bank-single/final-review/VERIFICATION.json`.
The separately qualified8378 runtime, nine original identities, six drained queues,
original77 tables/21,989 rows/43 copied legacy Characters, configuration/schema and
defaults15:1/17:1/no44 remain preserved. No deployment, staging/commit/push, native
launch or Computer Use. The earlier checkpoints below retain their original scope.

## Atomic splits and creation identity: checkpoint10259

Five executed regressions fail against the exact10240 Game binary. Cross-bank
splits persist only one half before an ordinary save; same-bank splitting can
commit the newly created stack while reducing the source fails. A failed split
also permits a partial ordinary save. Reusing a bank GUID through the allocator
can reduce the original source instead of rejecting the operation. The original
regression source/assembly, five-failure TRX and prior Game hash are retained in
`realm-bank-splits/baseline`. Its missing ItemLocation import compile failure is
retained separately, outside the behavioral result.

The actor now prepares the new stack and reduces the source before enrolling
both Items in one CharacterContext/transaction. This applies to Character-to-bank,
bank-to-Character and same-bank splits. Both captured tracked/SQL row witnesses
must reach the complete desired state. Either failing write or a stored INSERT
rewrite rolls the whole unit back. Fresh-context retries retain the same new
identity after rejected writes; the failed live Inventory remains quarantined
under the existing multi-item save barrier until replaced. Automatic recovery and
lost-acknowledgement split lifecycle are not newly qualified by these tests.

The split-specific entry requires a distinct new Item and checks its creation
intent before staging either half. Initial creation rejects GUIDs already in
either storage domain. Retained uncertain-state reconciliation remains available
for this same creation's retry; it does not turn an unrelated bank row into a new
Character stack. A non-bank destination also rejects unexpected bank ownership.
This supplements the previously fixed allocator high-water behavior, rather than
reallocating or silently overwriting an existing identity.

The existing constructor's charge/socket initialization, copied expiration and
soulbinding, destination bag rules and Add-before-Stack notification order remain.
The Add payload is built before capture so its initialization belongs to the same
stored state. Source codecs stay with their source and newly created stacks keep
the existing initialization policy; no codec-copying policy is inferred. Loading
players retain suppressed Add notifications. Zero-count splits reject before
creation, and rollback snapshots refuse reserved slots. Ordinary Character-only
splits still persist through their existing combined normal inventory save.

Split errors before enrollment restore the operation snapshot. Packet errors
after enrollment instead retain both prepared stacks and quarantine the Inventory,
avoiding restoration over a possible SQL commit. This reuses the10240 multi-item
barrier. Packets still precede the asynchronous commit outcome; post-commit actor
publication and complete socket/session lifecycle remain unqualified.

Qualification:

- Full solution build: zero warnings/errors; **10,259 tests**,19 new regressions
 and189 focused bank/item/context/packet checks pass. The bank identity suite now
 has90 cases;85 run with production MySQL connection-string retry configuration.
- **1,778 MySQL cases**:1,374 Character,140 Auth,179 dual-database and85 bank;
 512 configured-strategy cases, plus four independent Auth race probes. Five
 existing SQLite/raw-explicit-transaction bank controls remain full-suite cases.
- Four provider model gates,31 original compatibility probes and726 real Contract
 quote choices pass. Contract ItemInfo/stacking metadata remains synthetic.
- 5,887 source/tool hashes, five changed paths versus10240, one added;169 frozen
 DLLs and53 copied case/helper sources. Both DB binaries are unchanged. The exact
10207 populated60/61/62-table Character and30-table Auth upgrade proofs remain
 bound to those identical binaries and were not rerun for this change.

The10259 evidence authority is
`I:/GIT/NexusForever-assessment-report/remaining-goal/realm-bank-splits/final-review/VERIFICATION.json`.
The separately qualified8378 runtime, nine original identities, six drained queues,
original77 tables/21,989 rows/43 copied legacy Characters, configuration/schema and
defaults15:1/17:1/no44 remain preserved. No deployment, staging/commit/push, native
launch or Computer Use. The earlier checkpoints below retain their original scope.

## Consumed stacks and multi-item failure guards: checkpoint10240

The four initial regressions fail against the exact10219 Game binary. Fully
consumed Character-to-bank and bank-to-Character merges do not persist the source
and survivor together; failed merges also permit a later ordinary save. The
original regression assembly/source, failing TRX and Game hash are retained in
`realm-bank-consumed/baseline`. Two further regressions against the interim atomic
implementation reproduce a different failure: a thrown swap notification restores
the consumed source into live inventory while the already enrolled SQL unit can
commit. Their executed Game/test assemblies, source and failing TRX are retained
in `packet-baseline`. These are six behavioral negatives; the separate Item-type
namespace compile failure is not counted as a behavioral result.

Inventory moves now defer standalone bank deletion for consumed merges. The
removed source retains deletion intent and an actual-commit Item owner, alongside
the destination in the same context/transaction. Both source storage domains must
be absent and the survivor must match its exact captured row before commit.
Conflicting Character/account/realm/item ownership, either failing write, or a
stored destination rewrite rejects the complete unit. A never-persisted source
can be consumed without creating a row or consuming another GUID. Existing stack
eligibility and destination metadata rules are retained; no source-codec merging
or new stacking policy is inferred. This qualifies cross-bank and same-bank merges.

The Inventory retains its multi-item bank save Task. While it is unsettled,
ordinary inventory saves reject their context, save/mutation leases and reward
reservations refuse admission, and another bank move waits. Failed/cancelled
units expose the existing authoritative-reload guard to inventory mutation and
reward consumers. They retain the consumed source rather than letting a later
ordinary save delete one half independently. Both Items can retry through a fresh
manager context; the failed live Inventory remains quarantined until replaced.
Fresh SQL-loaded fixture inventories pass their subsequent normal save without
recreating the consumed source. Automatic retry is not connected by this change.

A pre-enrollment stack/delete notification failure restores the operation snapshot,
including pending-delete state and the retained tombstone list. A notification
failure after SQL enrollment instead quarantines the Inventory and does not
restore live Items over the possible commit. Ordinary successful packet order is
unchanged. Notifications still precede the asynchronous SQL outcome; this is not
post-commit actor publication or complete socket/session lifecycle qualification.

Qualification:

- Full solution build: zero warnings/errors; **10,240 tests**,21 new regressions
 and170 focused bank/item/context/packet checks pass. The earlier10234 full pass
 is retained as interim evidence, before the additional notification guard.
- **1,759 MySQL cases**:1,374 Character,140 Auth,179 dual-database and66 bank;
 493 configured-strategy cases, plus four independent Auth race probes.
- The bank identity suite has71 cases. Its five existing SQLite-specific/raw
 transaction controls remain in the full suite, outside the66 MySQL cases.
- Four provider model gates,31 original compatibility probes and726 real Contract
 quote choices pass. Contract ItemInfo/stacking metadata remains synthetic.
- 5,886 source/tool hashes, nine changed paths versus10219, two added;169 frozen
 DLLs and52 copied case/helper sources. Both DB binaries are unchanged. The exact
10207 populated60/61/62-table Character and30-table Auth upgrade proofs remain
 bound to those identical binaries; they were not rerun for this change.

The10240 evidence authority is
`I:/GIT/NexusForever-assessment-report/remaining-goal/realm-bank-consumed/final-review/VERIFICATION.json`.
The separately qualified8378 runtime, nine original process identities, six
drained queues, original77 tables/21,989 rows/43 copied legacy Characters,
configuration/schema and defaults15:1/17:1/no44 are preserved. Source/schema is
not deployed. No staging/commit/push, native launch or Computer Use.

Checkpoint10259 above supersedes the former independently persisted ItemSplit gap.

## Atomic surviving-item swaps and partial merges: checkpoint10219

The real Inventory.ItemMove path previously launched one save per Item. Executed
regressions against the exact10207 Game binary force the second insert to fail in
each cross-bank direction: the first direction leaves no Character item rows,
and the other leaves two. The original regression assembly, source, failing TRX
and prior Game hash are retained in `realm-bank-swaps/baseline`. Its initial
namespace compile error is retained separately and is not a behavioral result.

The manager now enrolls both surviving Items in one CharacterContext and its
single transaction. Cross-bank swaps, same-bank slot swaps and non-consuming
stack merges retain both original GUIDs, owners, codecs, scalar values and the
existing binding/capacity/packet behavior. A merge also saves a changed survivor
in Character inventory even when that survivor never left its bag. A move onto
the same Item is a no-op, preventing self-stacking or duplicate save enrollment.

Each Item keeps its existing actual-commit owner; both row witnesses validate
before commit. Failure in either write rolls the entire unit back. A conflicting
second Item save or bank owner rejects the context and releases the first Item
without consuming its dirty intent. Fresh-context retries using the retained
Items settle both halves, including after a rejected second insert or bank-only
UPDATE. Later ordinary inventory saves cannot recreate the deposited item.
The implementation does not replay gameplay callbacks in an execution strategy.

This is a SQL atomicity result. The existing asynchronous wrappers still publish
inventory/packet changes before observing SQL success; automatic retry, failure
publication and full lifecycle reconciliation remain work. A merge that consumes
its source still uses the prior deletion path and is not qualified here.
Process-local leases do not establish cross-process or alias-equivalent ordering.

Qualification for this checkpoint:

- Full solution build: zero warnings/errors; all **10,219 tests** pass, with
 12 new regressions and149 focused bank/item/context/packet checks.
- **1,738 MySQL cases**:1,374 Character,140 Auth,179 dual-database and45 bank;
 472 cases use the configured retry strategy, plus four Auth race probes.
- The bank identity suite now has50 cases. Five existing SQLite-specific/raw
 transaction controls remain in the full suite, outside the45 MySQL claims.
- Four provider model gates,31 original compatibility probes and726 actual
 Contract quote choices pass. Contract item metadata remains synthetic.
- 5,884 source/tool hashes, three changed paths versus10207, one added;169
 frozen DLLs and51 copied case/helper sources. Both DB binaries are unchanged.
 The exact10207 populated60/61/62-table Character and30-table Auth upgrade
 proofs remain bound to those identical binaries; they were not rerun here.

The10219 evidence authority is
`I:/GIT/NexusForever-assessment-report/remaining-goal/realm-bank-swaps/final-review/VERIFICATION.json`.
The10207 results below remain historical codec/loading and schema evidence.

## Executed negative controls

The three new regressions failed against the exact checkpoint10190 Game binary:

- Depositing an item with microchips and runes, then loading the bank in another
  Character, returned empty codecs. Withdrawal could not recover that state.
- The singleton bank manager remembered only account/realm. A fresh inventory
  for the same account/realm was treated as already loaded and received no items.
- The loaded marker was set before querying SQL. A failed read prevented a retry
  on the same inventory even after the owned fixture database recovered.

`realm-bank-codecs/baseline/proof.json` binds the old Game hash, compiled regression
assembly and actual three-failure TRX. The initial missing-import compile log is
retained separately and is not counted as a behavioral negative control.

## Persisted codec state

`RealmBankItemModel` now has `MicrochipIds` and `RuneSlots`, using the same existing
codecs and varchar128/varchar256 storage as Character items. Both deposit and
withdrawal preserve their order, duplicates, empty state and full uint payloads.
Real items supply the exact snapshot captured by their persistence owner rather
than a separately sampled mutable list. Custom IItem adapters use the established
serializers. Loaded bank items restore those fields before entering the inventory.

The additive migrations are:

- MySQL: `20261031085420_RealmBankItemCodecs`.
- SQLite: `20261031085546_RealmBankItemCodecs`.

Each adds only the two nullable codec columns with empty defaults. Existing bank
rows remain empty because their lost historical codecs cannot be inferred. No
socket/rune sampling, grants, item ids, ownership, capacities or entitlement
defaults are backfilled or changed. Character item serialization and packet
layouts retain their existing formats. Bank baseline/uncertain-state comparison
now includes codecs; legacy null and empty bank codecs both represent absence.

## Per-inventory loading

The singleton uses a weak inventory-keyed load state with retained account object,
account id, realm and Character ownership. Repeated opens of one loaded inventory
do not duplicate items; another inventory loads independently. Read and codec
construction finish before any live insertion. Failed reads can retry. A failure
after insertion starts quarantines that partial load until a fresh inventory is
created, avoiding duplicate replay. Reentrant or changed-owner loads fail closed.
The established unknown-item-definition skip policy is retained.

## Transfer transaction validation

Bank operations retain immutable tracked and stored witnesses for both storage
domains. Deposit must insert/update its exact bank row and remove its Character
row; withdrawal must remove the bank row and persist its exact Character snapshot.
Tracked replacement, changed values/originals, suppressed modifications and SQL
rewrites reject the Character unit before commit. Even a bank-only single UPDATE
requests an EF transaction so its stored witness can be checked before commit.
Caught enrollment errors permanently reject that context. A stale unchanged
Character object cannot delete a bank row while omitting its restoration write.

These checks preserve the existing actual-commit Item owner and finite uncertain
baselines. A failed transfer leaves both old rows/codecs intact; a fresh retry
converges without overwriting another live Item owner. The existing in-memory GUID
allocator and its bank-aware high-water mark remain unchanged.

## Qualification

Evidence root:
`I:\GIT\NexusForever-assessment-report\remaining-goal\realm-bank-codecs`.
The final verifier binds the following gates; tests alone do not prove native UI.

- Full solution build: zero warnings/errors. **10,207 full tests**,17 new and137
  focused passed; all earlier10,190 cases are retained. The bank identity suite
  has38 cases, including the three reproduced failures.
- **1,726 MySQL cases**:1,374 Character,140 Auth,179 dual-database and33 bank,
  including460 configured-strategy cases, plus four independent Auth race probes.
  The bank replay uses production connection-string configuration with retry
  enabled. Five SQLite-specific/raw explicit-transaction controls stay in the
  full suite and are not claimed as MySQL cases.
- Both providers exercise the populated bank-only upgrade and replay, preserving
  all ten old bank columns and producing empty legacy codecs without items or
  Characters. Fresh MySQL predecessor upgrades preserve60/61/62 tables and
  68/69/70 old logical rows across all old column projections. The refreshed
  Auth upgrade preserves30 tables/145 rows and defaults15:1/17:1.
- Four provider model gates,31 original compatibility probes and726 real Contract
  quote choices pass. Contract fixture ItemInfo/stacking metadata remains synthetic.
- 5,883 source/tool hashes,14 changed paths versus10190, seven added;169 frozen
  DLLs and50 copied case/helper sources. Character DB binary changes for the two
  columns; its upgrade proofs rerun with that binary. Auth binary is unchanged.

The separately qualified8378 runtime, nine process identities, six drained queues,
original77 tables/21,989 rows/43 copied legacy Characters, private configuration,
runtime schema and entitlement defaults15:1/17:1/no44 remain preserved. The new
schema/source is not deployed. No staging, commit, push, native client launch or
Computer Use. Q5597 remains unavailable and is not requested again.

## Remaining full Goal

General asynchronous bank retry/post-commit actor publication and periodic bank
dirty/expiry persistence remain. Checkpoint10277 qualifies the Inventory single-write
and owned-deletion paths plus expiry barriers, without claiming full lifecycle parity. Checkpoint10259 also qualifies
bank split transactions in addition to10240 consumed merges and10219 swaps. The process-local Item lease does not establish cross-process or
alias-equivalent concurrency, and public collection concurrency retains its
existing contract. Further copied-runtime upgrades, bank load/socket/native UI
checks and large-history behavior remain unqualified.

Contract public07CE claims, retail reset/unclaimed/excess behavior, actual full-table
delivery, track35 earning and the broader assessment/upstream/AMP/combat/content
requirements remain. The user directs retail reset behavior; no policy preference
question is pending. The full Goal stays active.
