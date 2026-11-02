# Ordinary Item save outcome recovery

31 October2026 destination continuation;10586 source checkpoint.
The preceding source checkpoint is10565 (gift/COD receipt recovery).
No provider/native/retail completeness is claimed here.

## Executed failures

The original ordinary Item SavedChanges handler ignored zero-count acknowledgements
only for reward owners. Ordinary creates, updates and deletions consumed their
intent despite an injected zero-write save. A fresh save then omitted the new
row, retained the old count, or left the deleted row present. All three assertions
failed; a complete stored no-op and later live stack control passed. The6067-input
source/tool/binary archive and eight DLLs are frozen with negative.trx under
artifacts/continuation-20261031/ordinary-item. The first test compile lacked an
IItemManager namespace import; it was corrected before this authoritative run.
The premature freeze attempt stopped on the missing test DLL and produced no
input archive/manifest.

After the first witness implementation,48 focused cases passed. A second probe
then exposed deletion of a row transferred after enrollment; the update control
already rejected and rolled back. That1-failure/1-control run has its own6068-input
archive/DLLs and ownership-negative.trx. It is distinct from the initial state.

## Current implementation

Item.SaveWitness contains the copied enrolled Item target and original/tracked
state. Ordinary SaveUnits retain separate current and last-flushed witnesses.
Before-write checks compare the stored row with its enrolled/last-flushed state;
post-write/commit checks compare the complete12-scalar/codec target and reject a
duplicate bank row. Deletion retains its model/original ownership too. A zero or
advertised positive affected-row count cannot acknowledge missing SQL. A complete
already-stored no-op can settle. Single-row saves request an implicit transaction
so SQL trigger changes can reject before commit.

The ordinary witness is excluded when a bank/frozen/reward capability owns the
unit; those existing paths keep their witnesses. The mail aggregate retains its
existing MailSaveWitness API over the common implementation. Unflushed restaging
cannot replace the last-flushed SQL witness at explicit commit. Dirty revisions
and uncertain candidates retain later intent, rollback/savepoint and lost-outcome
recovery. Prior changes staged by another owner in the same context still enter
the enrolled target; only later outside rewrites are rejected.

These are checks under the existing relational save unit and process-local leases.
No persistent concurrency token or cross-process compare-and-swap was added.
Sequential owner-transfer/SQL rewrite probes do not establish arbitrary concurrent
provider safety. Fresh MySQL/configured strategies, copied upgrades, deployment,
load, sockets, native presentation and retail qualification remain separate.

## Verification in progress

The21-case ordinary regression set includes asynchronous/synchronous suppressed
writes, false affected-row counts, complete no-ops, later live intent, owner
transfer before/after flush, SQL triggers, tracked/original rewrites, savepoints,
repeated flushes and unflushed restaging. Twenty ordinary plus43 mail aggregate
cases passed before the final shared-owner preservation control was added.
The full solution rebuild has zero warnings/errors. All10586 full tests and64
final focused cases pass with zero failures/skips; all10565 earlier names remain.
Complete6069 final inputs,6067 initial negative inputs,6068 ownership-negative
inputs and eight DLLs per state verify with their complete archives. The handoff
verifier intentionally rejects the working delta against its published10277
manifest; that historical manifest was not weakened or rebound. Final proof is
artifacts/continuation-20261031/ordinary-item/verification.json.

All10565 earlier cases must remain. Source tests do not qualify retail behavior.
Defaults15:1/17:1, no automatic44/seven grants, no push, and the native/Computer
Use stop remain. Outgoing mail publication, attachment taking, marketplace and
other recovery/feature requirements remain in the assessment queue.
