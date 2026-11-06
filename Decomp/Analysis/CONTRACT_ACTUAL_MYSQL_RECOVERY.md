# Actual-catalog Contract recovery on configured MySQL

6 November 2026. The eight representative windows from
[actual-catalog producer recovery](CONTRACT_ACTUAL_PROCESS_RECOVERY.md) now pass
on fresh MySQL 8.4.13 Auth/Character schemas with the repository's configured
retry strategy. The [726-choice catalog qualification](CONTRACT_ACTUAL_CATALOG_DELIVERY.md)
is reused, including the original 385-payload verification, 16-table loader and
real ItemInfo. The prior 726/967 batches were not repeated.

Production Source, schemas and public 07CE routing are unchanged. The only test
Source change adds optional Character/Auth provider options to the existing
process helper; its SQLite defaults and every other assertion remain exact.
A default SQLite produce/recover control passes on the same test and production
DLLs. That control used the retained earlier adapter binary; qualified MySQL
cases use the later path-normalized adapter.

## Completed recovery boundary

| Original producer boundary | Independent fresh-process result |
| --- | --- |
| Completed final-item/credit control | Original item, credits and receipt reload |
| Before Character commit | Claim, item and credit transaction rolls back; no receipt or automatic new claim |
| After Character commit, before final-item publication | Original item, credits and receipt survive |
| After Character commit, nonstackable item | Original item identity, quantity and binding survive |
| After Character commit, before Auth delivery | Committed source intent delivers and acknowledges once |
| Before Auth commit | Rolled-back Auth operation completes from the durable source intent |
| After Auth commit, before source acknowledgement | Original Auth operation prevents another credit; source acknowledgement completes |
| After source acknowledgement | Delivered intent, Auth operation and track revision remain stable |

The parent validates each original process generation and complete reached gate,
then physically terminates that producer only. The seven kills replace the
producer process, not a simulated exception or synthetic commit outcome.
The engine remains the same generation throughout. Fresh recovery invokes the
existing ContractAccountDelivery service twice for account cases.

Seven committed cases then model spending through owned fixture SQL and replay
the retained original claim through the real executor in another process.
Applied=false and Changed=false, and the newly prepared inventory reservation
stays unconsumed. Spent items and balances remain spent. Receipt, quote, operation
and revision identities remain stable through independent final verification.

## Provider, validation and cleanup

Each case applies the exact compiled 42 Auth and 49 Character migrations to two
fresh leased schemas. All 16 schemas are dropped through their successful-CREATE
leases and independently queried absent before shutdown. Control observations
record MySQL 8.4.13, REPEATABLE-READ and TLS_AES_256_GCM_SHA384; EF configuration
requires TLS and enables retries. Existing databases, global services and other
workloads are preserved.

All eight cases pass: 53 qualified case-child generations plus one retained
pre-schema setup failure. The final independent audit confirms all 81 recorded
probe/orchestrator/engine generations absent, all 19 recorded temporary
directories absent and the loopback engine listener closed. Shutdown was graceful.

The test-only build is clean in 14.33s; the final adapter build is clean in 2.55s.
All 138 candidate dependencies remain exact after execution; only Game.Tests.dll
differs from the published Rotation candidate. Production/reader DLLs are
unchanged. The isolated Source comparison covers 6,298 tracked files with one
owned test change and eight previously recorded filter/format differences.
No full solution build or full suite ran.

The resumed orchestration takes 383.74s including fresh migrations, corpus
loading, processes and shutdown. The qualified child start through case cleanup
is 374.72s. These are observed harness times, not production performance results.

## Retained diagnostics and reviews

The initial standalone build accidentally compiled the saved before-source
snapshot; explicit Compile inputs corrected that harness-only failure. The first
control prepare then refused an unnormalized case path before any CREATE.
Full-path normalization allowed the same case and engine to resume after the
failed child was terminal and schema leases were absent. Both failures and their
original identities remain retained.

Standards review found a possible partial gate-file read. The local parent now
retries partial/in-progress reads while observing the same producer. A separate
partial-write control passes with five same-handle observations. All eight
provider cases were already terminal when that correction landed; they were
not repeated, and their original successful readers validated complete gates
before each kill. Normalized-text and raw-file hashes are separately labeled.
A read-only audit setup error was corrected without repeating provider work.

Both final review axes report zero remaining findings. The latest 20 upstream
PR heads/bases/states are unchanged, including #608 open and #609 closed/unmerged.
The [portable record](coverage/contract-actual-mysql-20261106.json) binds Source,
138 dependencies, both adapter boundaries, migration identities, individual
results, physical kills, spent replays, cleanup, diagnostics and reviews.
Raw evidence remains under artifacts/continuation-20261106/contract-actual-mysql/.

## Remaining scope

This qualifies internal prepared-actor recovery on fresh configured MySQL.
Prerequisites/cycles and Player/session/inventory collaborators are prepared;
spending is fixture SQL. Recovery explicitly invokes existing services.
Public 07CE/reset/unclaimed/final-first/excess policy, real eligibility,
autonomous hosted World recovery/publication, actual gameplay consumption,
copied upgrades, database-server/connection failure, other profiles/providers,
broker/socket/deployment and native/retail remain open. Eight representative
windows are not 726 physical-failure permutations. Client/CUA remain stopped.
