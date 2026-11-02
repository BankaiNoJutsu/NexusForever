# Marketplace loaded-row preimages

Date:1 November2026. Source/private SQLite evidence only. Final full solution
and test-copy rebuilds pass with zero warnings/errors. All10811 full tests pass,
including the182 reviewed marketplace and297 prior mail cases in rebuilt full,
with zero failures/skips. All earlier cases remain and28 new cases are present.
All6107 final/6106 negative inputs, four complete archives/eight owning/test-copy
DLLs and final review snapshots verify. Exact local bindings:verification.json.

All15 non-key auction scalars and9 non-key commodity scalars were changed after
the memory book loaded, through real EF storage and public bid/refund operations.
The original26-case baseline ran24 failures/2 unchanged controls: row presence
alone accepted every changed preimage. The frozen negative6106-input archive
contains only the new tests over required-row10783. The initial implementation
passes26, and the expanded182-case marketplace gate passes, including legacy CSV/
repeated bids and a reentrant bid rejected before its competing wallet changes.

The implementation stores immutable copies of all16 auction and10 commodity
mapped scalars. Expected updates and deletes compare both tracked values and
fresh untracked SQL to the loaded preimage. Known successful settlement advances
the expected stored/live target; pre-operation live-view guards reject a retained
row whose live book no longer matches that expected state. A record with no
stored outcome retains the existing memory-only preparation/error path, but SQL
writes still require an owned preimage. Pending incoming commodity insertion and
the earlier missing-row, expiry and capacity protections remain.

Raw auction CSV is kept separate from the existing decoded packet representation.
This preserves whitespace decoding/canonical serialization and repeated bids
without treating the decoded value as its original raw SQL preimage.

Local artifacts are in artifacts/continuation-20261031/marketplace-witnesses.
The first review found no spec issue and one optional naming advisory; its pin,
report and snapshots are preserved separately. Qualification must bind the final
renamed source and rebuilt DLLs before promotion.

This is a read/staging preimage check. It does not establish immutable SQL
postimages, a database-route binding, locking/CAS, cross-process ownership or
complete economic outcome recovery. SaveBlocking errors can still trigger live
wallet compensation; provisional wallet packets and post-commit direct delivery
exceptions remain. Complete listing/wallet/Item/mail capture, once-only callback
execution, later intent, ambiguous outcomes and publication remain actionable.
Generic QuestRowPersistence was considered but not used as a drop-in across the
unresolved aggregate boundary. Source tests are not configured-provider/native/
retail completeness. No client/UI launch, defaults/entitlement changes, commit
or push; Goal remains active.

Final review pin:d7b29aba9a4c3e90a1ab24c4f249a0331494a8e6faacc742c995e7c5c5f2fdaf,
zero findings on both axes. The private naming advisory was resolved; review
snapshot copying now preserves original bytes/line endings without changing the
source pin. The historical handoff verifier was rerun and rejected evolved Source
at its checkpoint10277 equality assertion; the failure is retained separately.
