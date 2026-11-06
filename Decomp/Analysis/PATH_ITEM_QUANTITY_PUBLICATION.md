# Committed Path item quantity publication

6 November 2026. Supported Path level, mission and episode item adds and stack
updates now wait for the complete original Player Character save. A rejected
save retains the notices; a later retry publishes without granting again.
This extends the existing [Path progress publication](../../CURRENT_STATUS.md#committed-path-mission-notices)
and its Item/marker persistence boundary.

The original real-Player baseline reproduces eight failing cases and one passing
ordinary-item control. Item packets appeared before the reward marker and Item
commit; enqueue exceptions could interrupt the Path grant. The fix retains
quantity notices, confirms only the identities included in the save, and uses
the original actor queue after the save finishes. A standalone Inventory.Save
does not confirm the owning Path reward marker.

## Saved state and ordered retries

Both review axes found a further same-item defect in the first candidate:
an ordinary merge or consumption could supersede a retained mutation-time count,
then the delayed Path packet restored the older count or re-added a removed item.
Five concrete counterexamples failed before the correction.

Full-save capture now freezes the current Item membership and quantity with the
Item save. It suppresses add/stack notices for removed items. Related ordinary
add/stack/delete notices for a pending or ready Path Item GUID stay ordered and
keep their original reasons. Unrelated ordinary creation keeps its immediate
route. Earning or consumption accepted during Auth waits for its own frozen save,
rather than being released by completion of the earlier snapshot.

A failed enqueue retains the first undelivered packet. Earlier accepted packets
are not repeated, and no retry reruns the grant. Each attempt constructs a fresh
packet and deep Item payload. Original Player, PathManager, inventory, Character,
realm, account/id/epoch, session/queue and disconnect guards prevent redirection.

## Verification and evidence

All 17 new cases and all 355 scoped cases pass. The scope comprises Player
Experience persistence, PathManager, reward normal-save snapshots and inventory
reward delivery. It covers level/mission/episode producers, new and existing
stacks, rollback/retry, overlapping frozen saves, ordinary merge/consumption,
removal, isolated inventory save, second-packet failure and stale actor contexts.

Final owning/test-only builds are clean in 7.55/16.45 seconds. The final scoped
run takes 89 seconds; the focused post-review run takes 18 seconds. No full
solution build or full suite ran. Final owning Game and test-copy Game DLLs
match. Among all 138 dependencies, only Game and Game.Tests differ from baseline.

The isolated candidate comparison covers 6,298 parent tracked Source files,
five owned tracked changes and two new files, with eight retained historical
filter/format differences. Foreign Player work is excluded from the candidate
and staged Source. Initial missing-namespace/global-capacity fixture failures
are retained separately from the eight meaningful baseline failures. Both final
review axes report zero remaining findings; their initial P2 records remain.

The [portable record](coverage/path-item-publication-20261106.json) binds the
seven Source inputs, clean-filtered staging identities, raw candidate hashes,
138 dependencies, failing controls, final scope, timings and both reviews.
Raw logs/TRX/manifests are under
artifacts/continuation-20261106/path-item-publication/.
The latest 20 reviewed upstream PR heads/bases/states are unchanged, including
#608 open and #609 closed/unmerged.

## Remaining scope

These are prepared real-Player/headless collaborator and synthetic metadata
fixtures. Actual Path catalog/spatial inputs, full-inventory/overflow delivery,
reward/count/flag rules, broader mission/scientist semantics and wider inventory
movement/property/effect notifications remain open. The change does not select
an overflow destination or qualify retail presentation.

Configured providers, physical restart/outbox, socket/broker/deployment and
native/retail remain separate gates. No schema, migration or entitlement changed.
Client launch and Computer Use remain stopped. The overall Goal remains active.
