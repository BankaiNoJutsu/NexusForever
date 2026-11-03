# Community rename fee settlement

The real `ClientHousingCommunityRenameHandler` now queues one registered
community, its original residence and the original complete Player save actor.
The formula 2395 currency/fee and name reservation remain fixed for that command.
The selected debit, both SQL names and the frozen Character save graph commit
in one serializable Character transaction. Auth retains its existing preceding
save; this does not establish distributed Auth/Character atomicity.

The ordinary reconciliation receipt uses the existing physical receipt table
with a distinct `normal-housing-rename` payload kind. No schema changes are made.
The database independently checks the SQL leader, guild type and residence owner
through enrollment/flush/commit. Game authority additionally requires the exact
original actor, registry reservation, route and guarded residence rows. Proven
rollback retains the same frozen intent; lost acknowledgement probes the receipt
and graph instead of charging again. Foreign or unresolved outcomes stay owned
and cannot publish success.

The world update adopts committed names and publishes wallet updates, local
online-member guild rename, map properties and private success in retained order.
An enqueue exception resumes its failed stage with a fresh DTO. There is no new
broker rename producer. Later currency earning is reconciled before publication,
including the gap after the prior save callback. Map properties preserve later
unrelated residence changes. The existing inventory publication scope excludes
reentrant Item/satchel mutations while delivery is pending. Exact actor/session,
membership, root and recipient guards suppress stale notifications; retirement
waits for the accepted transaction. Zero fee preserves an absent selected wallet.

## Verification

All **31 new rename cases**, **553 affected cases**, **31 configured MySQL 8.4.13
cases** and **11,828 full solution tests** pass, with zero failures/skips and all
11,797 previous cases preserved. The full solution build passes with zero warnings
or errors. Both final Source review axes report zero remaining findings across 29
paths, bound by manifest SHA256
`6092b54f526eaadcaa2f305c296a7bd12e9fe2bbd090df0090631bf2ce8a5335`.

The final Source/Tools input hash is
`70f0717547f9aa3452d6fb5f2b22a04c93a95fe3d4a8faf13ca1e29a527a741f` across 6,233 inputs.
The test assembly SHA256 is
`2203757028fd7298da14ad2b9b8bf1b81a4eb75d5bfcdf6f598bb3ca9a2e31e2`; all 11 owner/test/provider
DLL triples match. The owned database and recorded validation generations are
stopped. Build: 28.303s; MySQL execution: 284.053s;
full test command: 860.269s. These are observed validation
durations, not performance claims.

Evidence lives under
`artifacts/continuation-20261103/housing-rename-fee-settlement/`. The corrected
pre-change 10-case public-handler baseline records 7 failures and 3 controls. The
initial guild test watched an unused broker path; its corrected baseline observes
the real local online-member broadcast, and the misbound run is retained. Later
compile, fixture-metadata and expected-exception corrections are retained too.
Assertions continue to check the transaction, exact fee, original graph and public
messages; the provider runner invokes the original compiled test methods with
configured options and isolated owned schemas.

## Remaining boundaries

These are source/configured-provider model results, not retail completeness.
Socket/native client, actual full tables, copied upgrades, broker/restart and
durable outbox delivery remain unqualified. A transport that accepts then throws
can still duplicate a notification; process restart is not an in-memory cursor
retry. Other housing fees and privacy/placement/remodel publication remain open.
Contract 07CE reset/unclaimed/excess policy and all 726 choices/full item metadata,
track 35 earning, native Focus/armor corroboration, F025 auxiliary producers,
Hycrest and Q5597 retain their existing explicit evidence gaps.

The current upstream delta query returns no PR updates since 06:11:42UTC;
game_rework and reviewed #608/#609 heads are unchanged. Reuse #608's source
reconciliation and #609's closed-unmerged record; no upstream PR mutation occurs.
Defaults 15:1/17:1 remain; no automatic 44 or seven baseline grants are added.
Computer Use/native launch remain stopped. The broader Goal remains active.
