# Actual-catalog Contract producer loss and recovery

6 November 2026. This extends the published
[726-choice actual-catalog delivery proof](CONTRACT_ACTUAL_CATALOG_DELIVERY.md)
with real producer-process termination and independent recovery over retained
SQLite databases. Production Source, schema and public 07CE routing are unchanged.

The completed control and seven termination windows use the recovered 16-table
catalog, concrete ItemInfo and the existing internal claim executor. Parent
instrumentation retains each original process handle, creation time, executable
path/hash and reached transaction gate. It terminates that generation only.
Transaction interceptors pause execution; they do not simulate successful
commit, throw a synthetic lost acknowledgement or choose the recovery outcome.

| Producer boundary | Fresh-process result |
| --- | --- |
| Completed final-item/credit control | Original claimed state, item and credits reload |
| Before Character commit | Uncommitted item/credit/claim roll back; cycle stays unclaimed |
| After Character commit, before final-item publication | Original item/credit/receipt survive without another grant |
| After Character commit, nonstackable item | Original item GUID, quantity, binding and receipt survive |
| After Character commit, before Auth compensation | Persisted account intent is delivered and acknowledged once |
| Before Auth commit | Rolled-back Auth transaction is completed from the committed source intent |
| After Auth commit, before source acknowledgement | Original Auth operation prevents another credit; source acknowledgement completes |
| After source acknowledgement | Original delivered intent, operation and track revision remain stable |

Fresh processes validate the frozen quote, original claim/account/cycle and
canonical receipt against the actual catalog. The account cases invoke the
existing ContractAccountDelivery service twice. Independent reload reconstructs
item instances with real metadata. No Challenge claim or medal history appears.
The before-Character-commit case has no committed receipt to resume and is not
silently retried as a new purchase.

## Spent-award replay and review corrections

Owned fixture SQL then models consuming the delivered item, spending the final
credits or spending the account compensation. Seven additional fresh processes
restore the retained original claim and invoke the real claim executor again.
Item cases prepare a new actual-ItemInfo inventory reservation against existing
SQL without reseeding. Every replay commits with Applied=false and Changed=false;
the new reservation stays unconsumed. Items and spent balances stay spent, and
receipt hashes, account operation keys and track revisions remain unchanged.

Spec review identified that the first item/credit spent checks were read-only;
the executor replays above close that verification gap. Standards review found
that a replay-helper exception could leave a child alive. Guarded original-handle
cleanup now stops/waits/records it. A separate injected post-launch exception
control verifies that path. Neither finding is counted as a production defect.
Both final review axes have zero remaining findings.

## Evidence and timing

The eight initial cases/seven producer kills pass in 145.03s across 30 processes.
Seven fresh spent-claim replays pass in 29.11s; one additional child verifies
helper cleanup. All 38 recorded generations are independently absent. All 16
producer-owned temporary directories and three replay inventory directories
are absent. The 16 disposable SQLite databases are retained as ignored evidence;
each passes an independent integrity check.

The 21 initial interceptor controls pass in 21s. Six final default-fixture controls
pass in 5s after the no-reseed seam is added. These are separately bound candidates,
not one 27-case run. Final test/runner builds are clean in 18.20/2.71s. The source
statement comparison verifies that the previous 385-file verification and 16-table
loader were only moved into shared helpers. The earlier 726/967 batches retain
their original identities and were not repeated. Process execution and corpus
loading dominate this gate.

The [portable record](coverage/contract-process-recovery-20261106.json) binds four
test Source files, both 140-DLL candidate manifests, real gates, original/replay
results, helper correction/control, process absence, retained SQL and reviews.
Raw evidence is under `artifacts/continuation-20261106/contract-process-recovery/`.
Only the test assembly and its runner differ between the two candidates; all
production/reader dependencies are identical. The isolated candidate excludes
foreign root work. No full solution build or full suite ran. Latest 20 upstream
heads/bases/states are unchanged, including #608 open and #609 closed/unmerged.

## Remaining boundary

This is internal claim/compensation recovery, with seeded prerequisites/cycles,
proxy Player/session/ItemManager collaborators and SQL-modeled consumption.
Recovery explicitly invokes the existing services; it does not qualify an
autonomous hosted World restart, actor publication or public gameplay consumption.
The named representative real item/account outcomes are not 726 physical-failure
permutations. Public07CE/reset/unclaimed/final-first/excess policy, real player
eligibility, nonempty inventory/asset presentation, configured MySQL/copied-upgrade,
database-server/connection loss, broker/socket/deployment and native/retail remain
open. Continue the independent gameplay/progression queue. Client/CUA remain
stopped; concurrent Matrix/data-recovery/Source/provider work is preserved.
