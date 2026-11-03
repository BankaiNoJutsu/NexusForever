# Committed community privacy publication

The real `ClientHousingCommunityPrivacyLevelHandler` now retains ordered requests
on the original registered community and residence. The prior live privacy flag,
visit index and public messages remain unchanged until SQL confirms the target.
The pending private bit is overlaid only on the existing complete guild save
snapshot; it does not provisionally change access checks. The normal guild graph
keeps its original route, rollback and uncertain-outcome guards. A no-op request
still enrolls a confirmation witness instead of waiting indefinitely for a dirty
field. Registered direct `SetCommunityPrivate` calls use the same path.

The guild manager applies the confirmed privacy bit on its world update, then
publishes the visit entry, local online-member flags and private reply in retained
order. SQL completion only records confirmation; it does not enumerate mutable
world actors. Requests on one community retain their accepted order. An enqueue
failure retries its failed stage with fresh packet objects, and reentrant updates
cannot release or replay another request. Later unrelated flag edits remain dirty
for their normal owner.

Exact registry, root, member and session guards suppress stale publication.
Confirmed private changes remove only their captured root's matching visit entry,
including when the community's current-root reference was replaced. Adding a
public entry keeps the stricter current-root checks. Retirement cannot re-add a
community to the visit list. Existing realm/residence/permission checks and
privacy wire values remain unchanged; standalone interface controls retain their
existing behavior. No fee, entitlement or database-schema policy changes occur.

## Verification

All **26 focused SQLite cases**, **415 affected guild/housing cases**, **26
configured MySQL 8.4.13 cases** and **11,854 full solution tests** pass, with zero
failures/skips and all 11,828 previous cases preserved. The full solution build
passes with zero warnings/errors. Both final Source review axes report zero
remaining findings across 12 paths, bound by manifest SHA256
`c2c15356e75de9443bda52749f87d107052bc3b4b27fdaabbddf8910ff47e13b`.

The final Source/Tools hash is
`a5792855696ae4a4d4afdb81fdd71dd975ce188e1d85236242f3fe6cbcbe8a97` across 6,236 inputs.
The test assembly SHA256 is
`bd9c20f57b9a4225c2f5a71bedf0d74a1c5ea366801367732dcbc402a26ceac4`; all 11 owner/test/provider
DLL triples match. The owned database and recorded validation generations are
stopped. Build: 18.586s; corrected MySQL: 215.664s;
full test command: 782.663s. The initial MySQL fixture run
took 333.297s and failed during setup; it is excluded from passes.
These are observed validation durations, not performance claims. The full gate
also covers all 415 affected cases after the fixture-only credential correction.

Evidence lives under
`artifacts/continuation-20261103/housing-privacy-publication/`. The original
14-case public-handler baseline records 10 failures and 4 controls. Review's
opposite-direction root replacement reproducer records 1 failure and 1 control;
the corrected regression covers both directions. The initial MySQL run records
26 setup failures with missing authentication on the fixture's later connection;
no public flow was reached. The fixture now captures its original configured
connection before opening it, matching the previous actor fixture. Production
code and assertions were unchanged by that correction. Retain these failed runs.

The existing checkpoint verifier's expected Source divergence remains historical;
its checkpoint manifests have not been changed to match implementation work.

## Remaining boundaries

These are source/configured-provider model checks. Native client, sockets,
actual full tables, copied upgrades, broker/restart and durable outbox delivery
remain unqualified. A transport that accepts then throws can still duplicate a
notification; an in-memory cursor does not establish restart recovery. Other
housing placement/remodel/fee flows remain open.

A separate owned SQLite probe now reproduces visitor/search metadata failures
after a confirmed paid rename: SQL guild/root names are Accepted and credits
4,900, but the new name lookup misses, the old alias still resolves and the visit
label remains Original. A fresh residence-manager model load has the public root
named Accepted and a public guild flag, but no public visit entry. Evidence and
11-DLL bindings are `next-visit-evidence.json` and `next-visit-binding.json` in the
milestone directory. This is a model load, not a full server restart. The actual
name-based visitor consumes this lookup in `ClientHousingVisitHandler`. Complete
that index lifecycle as the next integrated outcome; placement/remodel remain open.
Contract 07CE and its full choice/item data, track 35, native Focus/armor evidence,
F025 auxiliary producers, Hycrest and Q5597 retain their linked evidence gaps.

The current upstream delta query reports no new PR updates; reviewed
game_rework/#608/#609 heads are unchanged. Defaults 15:1/17:1 remain, with no
automatic 44 or seven baseline character grants. Computer Use/native launch
remain stopped. The broader Goal stays active.
