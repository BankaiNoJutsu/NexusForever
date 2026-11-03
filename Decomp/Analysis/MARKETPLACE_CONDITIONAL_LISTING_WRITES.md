# Conditional marketplace listing writes

## Actual MySQL qualification - 3 November 2026

The unchanged `decde2c5` Source and its retained DLLs pass **44 scoped MySQL
8.4.13 cases**: 38 original compiled listing race cases, two additional Unicode
CSV mutations through the same original assertion body, and four provider-only
repeatable-read snapshot scenarios. The portable [coverage report](coverage/marketplace-mysql-concurrency-20261103.json)
records exact case labels, outcomes, connection identities and input hashes.
Detailed local evidence is under
`artifacts/continuation-20261103/marketplace-mysql-concurrency/`.

The original cases exercise auction/commodity updates and deletes against
independently committed changes, including NULL CSV directions, primary keys,
mapped scalar fields and unchanged controls. All 40 original-body cases observe
`utf8mb4_0900_ai_ci` on the actual auction column. The U+00AD and U+200B competing
CSV rows survive their rejected deletions. The existing binary string predicate
implementation is retained; no new SQL generator or Source correction was needed.

The four additional scenarios establish an owner repeatable-read transaction and
read its original row before a second connection commits U+00AD. A fresh third
context observes that commit while the owner still reads its stale snapshot.
Conditional UPDATE and DELETE each reject with DbUpdateConcurrencyException and
preserve the foreign row; matching unchanged controls update/delete successfully.
Recorded owner/writer IDs differ and all four strategy-attempt counts are one.
These are new provider-only scenarios reusing original fixture/row observers,
not four additional original compiled xUnit cases.

Source/Tools remains 6,271 inputs under
`85cc5c60914174d37d5e485c1d81706a0086781539a94d8824da73ebb7e10e81`;
all 11 owning/test/main-provider/snapshot-provider DLL copies match. The original
case cohort takes 805.4s; the snapshot cohort takes 121.4s, partly overlapping.
Both helper builds have zero warnings/errors. The owned server/listener and four
recorded runner/helper generations are absent after graceful shutdown. Two
PowerShell attempts failed during managed dependency loading before executing
their comparison query; they are excluded. The actual column observations come from
the successful managed provider runner.

This qualifies the named before-transaction and post-snapshot interleavings on
the observed MySQL profile. Other collations/isolation levels, physical network
loss/deadlocks, complete multi-party financial commands, broker/restart, deployed
schemas, native timing and retail qualification remain separate. Original hosts,
data/configuration, schema, entitlement defaults15:1/17:1 and no44/seven grants
remain unchanged. Client/CUA remain stopped. No full suite was run, per the user's
scoped-only policy. The Goal remains active.

## Historical source and SQLite qualification

1 November 2026. Source/private SQLite execution and connection-blocked provider
model qualification. All 10,896 full solution tests pass with zero failures or
skips, retaining every previous 10,860 identity and adding 36 cases. All 251
marketplace cases pass, including 177 owned-SQL and 74 other compatibility
checks; all 297 previously reviewed mail cases occur in rebuilt full. Full
solution and subsequent test-copy rebuilds report zero warnings and errors.

The pre-flush scalar check preceded EF's implicit transaction. A second owned
connection could commit at TransactionStarting, after validation but before the
first transaction began. A delete could then remove the competing row and still
satisfy expected absence; an update could overwrite the exact column it intended
to change and still satisfy its postimage. These deterministic tests require the
competing hook to complete. They do not rely on thread scheduling or sleep.

Executed baseline: 34 cases, 28 failures and six controls. Every failure lacked
the expected concurrency exception (actual null), after its competing write
completed. Four controls have no competing mutation; two relocate the primary
key and already fail through ordinary key predicates. Auction deletion covers
all 16 mapped scalars, commodity deletion all 10, plus overwritten bid/fill
columns. The corrected 34 cases pass. Expanded cases additionally cover null
CSV originals becoming non-null and non-null CSV becoming null. All 36 pass in
the owned-SQL and full runs.

EF now treats all 15 auction and nine commodity non-key scalars as concurrency
tokens. Updates/deletes therefore predicate on their loaded original values as
well as their primary key. The existing immutable original/mask and complete
SQL postimage witnesses remain. Current production consumers load full models;
no incomplete-stub update/removal consumer was found. Competing stored values
are preserved when the conditional write rejects. No field, default or data
rewrite was introduced, and both provider model snapshots retain the metadata.

Both has-pending-model-changes commands report current snapshot agreement.
Review correctly identified that this alone did not prove migration necessity
against the old snapshots. A separate ModelDiffProbe loads the frozen original
snapshot classes in isolated assembly contexts, shares current EF, initializes
their relational models under each current provider, and compares them directly
with current IDesignTimeModel using IMigrationsModelDiffer. SQLite and MySQL
each show zero old listing tokens, 24 new tokens, zero migration operations and
zero connection attempts. Connection interception throws before any open.
Original/current DLL hashes bind the comparison to the negative and final
archives. No physical migration is needed for this metadata-only change. This
does not run MySQL DML or qualify a real data upgrade.

Final Source/Tools review pin:
0da489032447a9380e46114926a44b85ecf25c7b639379410199e6fa1d858d8b.
Both read-only review axes have zero final findings; the initial evidence gap
and its resolution are retained. Four 6,114-input archives include nine owning/
test-copy DLL pairs, adding the changed migration assembly to the earlier eight.
Local exact proof:artifacts/continuation-20261031/marketplace-concurrency/verification.json.
Historical handoff verification still rejects evolved Source against checkpoint
10277; its failure is retained. Final git diff --check is clean.

Fresh 05:47 UTC metadata finds 55 open PRs, 608 newest open; created-since29
October returns 609/608, merged-since returns already-reconciled598. Direct609
remains closed unmerged with unchanged head, and all 58 tracked refs match.
The revised604 source integration remains qualified separately. No reason for
609's closure is inferred.

This is bounded SQLite conditional-write evidence, not complete live-MySQL CAS:
collation, null comparison, isolation and affected-row behavior require actual
provider proof. Captured multi-party wallet/listing/Item/mail outcomes, normal-
save exclusion, lost-ack classification, provisional compensation and direct-
delivery recovery remain, alongside Contract and broader feature work. No
configured-provider/socket/load/native/retail promotion, client/Computer Use
launch, entitlement/default change, commit or push. Defaults remain15:1/17:1;
44 and seven baseline character grants remain disabled. Goal stays active.
