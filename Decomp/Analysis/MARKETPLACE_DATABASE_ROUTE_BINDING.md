# Marketplace database-route binding

Date:1 November2026. Source/private SQLite evidence only. Final full solution
and test-copy rebuilds have zero warnings/errors. All10820 full tests pass with
zero failures/skips, including191 marketplace and297 prior mail cases in rebuilt
full. All earlier cases remain and9 new cases are included. Five6108-input
archives/eight owning/test-copy DLLs and raw review snapshots verify. Exact local
bindings:verification.json. This does not complete the broader Goal.

Two separately owned SQLite files were populated with identical auction/order/
Item scalar rows and matching Character IDs. A book loaded in one could mutate
the equal row in the other after its injected database lookup changed. Original
four cases executed2 failures/2 same-route controls. Prototype4 passes.

Loaded and known-confirmed rows now retain the existing normalized credential-
free database identity. Require*Row validates it before expected-row queries,
then validates tracked/fresh SQL preimages and returns the checked model. Equal
values cannot confer authority across a different route. Known insertion and
settlement bind their actual context; pending incoming/memory mode remain.

Loading uses one owned context for the route, models and startup ID marks.
Expanded controls exposed an introduced regression: the required Item join can
omit orphan auctions, so deriving the ID maximum from joined models reused low
IDs (expected51, actual2). A separate raw auction-ID query in that same context
restores the earlier high-water behavior. The nine-case8pass/1fail candidate is
preserved separately; corrected9 passes. Controls also cover connection-key
ordering and a newly inserted partial-fill remainder. The expanded draft's
incorrect CommodityOrder namespace failed compilation before execution and was
corrected from the source definition. Earlier EOF blank-line warnings were
removed before the final review pin; historical logs remain.

Local artifacts:artifacts/continuation-20261031/marketplace-routing. Final review
pin:eaf52a2cf5bbf37f60a002e498c617cead20866b884429a9ebf086250f3c29d7,
zero findings on both axes. Raw snapshots and input manifests bind exact bytes.

This does not join wallet/listing/Item/mail outcomes or prove SQL postimages,
CAS/concurrency, actor authority across multiple participants or durable replies.
Existing provisional wallet compensation and post-commit delivery exceptions
remain work. No configured-provider/upgrade/socket/load/native/retail promotion,
client/UI launch, entitlement/default changes, commit or push. Goal remains active.

Upstream04:23UTC refresh:55 open PRs,608 newest open; created-since29 October
returns609/608 and merged-since query returns598. Of58 tracked refs,604 alone
changed from19786895e3bb399a95576295916318b4dfc9bbc2 to
36b59aaaaebbcd0ec3e23a94fd3cf9de8975923b. Its divergent history required an exact
two-point diff, retained separately under upstream604-refresh; reconciliation is
pending. Do not call every upstream head unchanged or treat a merge-base compare
as the delta.609's earlier closed-unmerged status remains recorded separately.

The historical handoff verifier was rerun and rejected evolved Source at the
checkpoint10277 equality assertion. That failure and prior EOF warnings remain
historical evidence; final git diff --check is clean. The provisional corrected
capture was renamed boundary-fixed without changing archive contents.
