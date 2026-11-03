# Confirmed community visitor directory recovery

Community visitor/search metadata now follows confirmed community and residence
names. The paid rename flow adopts both names and refreshes the lookup and public
display name before its successful reply. It removes old aliases, preserves an
existing owner caption, and cannot replace another community's current metadata.
Private and retired roots cannot acquire public visit entries.

New registered communities remain absent from search and visits until creation
confirms. SQL callbacks retain immutable confirmed metadata; the residence world
update publishes the latest record for the original community/root pair. A later
ordinary rename retains that publication ownership until its complete name/root
save confirms. Failed saves retain the prior directory state and retry through
the existing owner. Paid renames update the same confirmed directory name.

Settled residence-model loads restore public visit entries with their confirmed
leader caption. Pending names are deferred, and both confirmed and live privacy
must permit public listing. An unsaved name, public flag or leader edit cannot
be treated as loaded SQL evidence. Missing owner labels remain explicit and do
not produce a fabricated public entry. Exact original actor/root and retirement
guards protect replacement metadata.

## Verification

All **25 new SQLite cases**, **440 affected housing/guild cases**, **25 configured
MySQL 8.4.13 cases** and **11,879 full solution tests** pass, with zero failures
or skips. All 11,854 prior full cases are preserved. The full solution build has
zero warnings/errors. Both final Source review axes report zero remaining
findings across ten paths, pinned by manifest SHA256
`47826b7e91f47a3cb6dbc2bd64a01ed9e90ab024446d126fc7a3e3e11ab34533`.

The Source/Tools binding is
`e2dbca5def12145f520db7fbfcb941a0e5888fc72d21af68b6b13409b046ac26` across 6,237 inputs. The test
assembly SHA256 is
`81e6aaad940e46a69eda2e2d744c8c7348312d0a764a29174cfa24cc9114258a`; all 11 owner/test/provider DLL
triples match. The owned MySQL generation and recorded validation processes are
stopped. Build: 23.468s; final MySQL: 221.949s;
full test command: 757.576s. These are observed validation
durations, not performance claims. The earlier candidate's 22 passing MySQL cases
and intermediate builds remain retained separately and do not qualify this final
candidate.

Evidence is retained in
`artifacts/continuation-20261103/community-visit-index/`. The initial rename/load
baseline has six failures and two controls; creation and stale replacement
regressions each reproduce two additional failures. Review regressions reproduce
three ordinary-rename/privacy failures, two model-load name/privacy failures and
one unconfirmed-leader caption failure. The compile-only missing-using failure
and intermediate candidate results remain retained. The proposed concurrent
creation concern was withdrawn after identifying the existing lifecycle lock;
there is no executed concurrency-failure claim.

## Remaining boundaries

These checks use source, owned SQLite and configured MySQL model fixtures. A
residence-manager model load is not a full server restart. Native client, socket,
full-table, copied-upgrade, broker/restart and durable-outbox qualification remain
open. Other housing placement/remodel/fee publication remains open; the broader
Goal is active. A separate real public remodel/map/Player probe reproduces
premature publication: SQL music stays 0 through a rejected owned SQLite save,
while the live root and broadcast already contain 29. Removing the trigger lets
normal recovery persist 29. Only map membership is injected; no remodel fee or
table-validation claim follows. Its `next-remodel-evidence.json` and
`next-remodel-binding.json` retain the final source/test and 11-DLL probe bindings.
Complete committed remodel publication as the next integrated outcome. The
historical continuation verifier's expected Source divergence remains recorded,
and its checkpoint manifests have not been rewritten.

The current upstream collection still ends at #609 (closed, unmerged); #608 is
open at its previously source-reconciled revision, and game_rework is unchanged.
No upstream PR mutation occurs. Defaults 15:1/17:1 remain, with no automatic 44 or
seven baseline character grants. Computer Use/native launch remain stopped.
