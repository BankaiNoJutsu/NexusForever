# Whole-Item auction posting

Completed at the source/private-SQLite boundary on 1 November 2026 UTC. The full
solution passes **11,354 tests**, zero failures/skips; build zero warnings/errors.
193 focused and 1,106 affected cases pass. Final Spec0 / Standards0 findings.
This is the latest qualified working-source archive, including commodity posting
and the earlier completed economic flows. Provider, actual-table, socket/native
and retail qualification remain separate.

The real ClientAuctionSellOrderSubmitHandler captures scalar input before request
reuse and scope disposal. The original Player preflights its complete Character,
captures a zero-change wallet and exact whole Item/bag capabilities, then moves
the same complete stored Item to null-owner escrow and inserts its listing in
one receipt-backed transaction. Removal and post success follow the original
actor's complete ordinary reconciliation. Price policy, Unix expiry/default
duration, slot limits and the absence of a listing cash debit remain unchanged.
Bound Items retain AuctionCannotFillOrder refusal; legacy IPlayer adapters remain.

Fresh exclusively owned contexts validate the complete owner/Item/listing/wallet
graph, custody exclusions, sealed SQL targets and exact receipt. Lost replies
resolve that receipt; changed graphs are never replayed. Read-current adoption
updates the book while operation ownership excludes competing mutation/reload.
The ordinary save locks and validates the parent Item witness through commit and
enrolls the unchanged listing. Its receipt hash binds the parent's complete Item
target without enrolling null-owner escrow as Character-owned inventory.

An Item-only metadata token blocks scalar, cached codec-list and rune-child
mutation before assignment throughout the hold. Confirmed escrow permanently
retires the original inventory object's write authority; the manager hydrates a
distinct escrow object. Deferred grants use the established buffered inventory
and durable recovery-mail flow. Custody conflicts preserve later accepted currency
and Item intent without restoring stale escrow. Known rollback applies held
expiration before ordinary persistence and publishes the buffered expiry only
after save; commit clears the detached source's expiration entry. Captured escrow
TTL semantics are preserved rather than replaced with a new retail policy.

Evidence: artifacts/continuation-20261031/auction-posting/.

- producer-negative.trx retains two real public failures: ItemDelete before SQL
  escrow and post success before dirty currency/quest persistence.
- All 18 new public and 12 command cases pass. Coverage includes nonempty codec
  metadata, scope disposal/request mutation, rollback/lost replies, later grants,
  retirement/replacement, callback mutation, custody changes before/during ordinary
  save, competing ownership, bound refusal, expiry, receipt collision, context
  reuse and forged roles.
- review-negative.trx retains all three executed metadata/expiry failures.
  review-fixed-focused.trx passes 193; final-affected.trx passes 1,106. Final
  review-reports.md clears both axes; initial-review.* retains resolved findings.
- full-build.log/full-build-time.json: 17.90 seconds MSBuild, 18.465 seconds command.
  full.trx/full-test-time.json: 14m33s test duration, 883.098 seconds command.
  Verification used normal sequential builds, with no assertion/deadline relaxation.
- final-inputs.json/final-inputs.zip/final-binding.json/verification.json bind
  6,156 Source/Tools inputs, 9 owning/test-copy DLL pairs and the test assembly.
  SourceToolsSHA256: a900f698618a6d3bb2c736f105d0ea327c0b3c2ea6d5078673d9580b44690db7
  ArchiveSHA256: 2847e828a9b465697d5ee4e245034bb6d5141148cc778a1c5c10a2964c5a8c95
  TestAssemblySHA256: 6ef1a3a74c1a6e118bd439bdb9288b92b922528149d87b97b798332d28fad102
  VerifiedUtc: 2026-11-01T22:59:00.783823+00:00
  Full session29559 is terminal with exit0. No validation remains in flight.

Failure history stays literal. Initial combined final-focused.trx has 106 passes
and one existing AuctionBidActor drain failure; the exact same-binary case passes
in actor-isolated.trx. Its non-atomic diagnostic at20.416s shows idle save/events/
queue/lease and pool pending0, with fixture connection diagnostics disabled.
The cause remains unproven; later passing gates do not prove it fixed. The earlier
commodity full failure remains in its own failed-gate record. The current full
gate follows material auction implementation/review fixes, not an unchanged retry
solely to obtain green.

Other intermediate evidence remains: namespace build correction, a real null versus
empty deferred-graph integration fault, and three excluded fixture failures
(incomplete foreign-custody move and out-of-range stack99 probe). Corrected inputs
exercise Owner42 custody and an in-range stack9 guard. Temporary diagnostics were
removed; the normal execution wrapper is used. iteration-history.md retains the
pre-qualification record.

The 23:03UTC upstream refresh found no PR updates since21:51UTC. All five direct
604/608/609/game_rework/expedition heads match prior reconciled values.608 remains
newest open and609 closed unmerged; see upstream-final.json/upstream-final-heads.txt.
The separate [healing record](HEALING_PERSONAL_MODIFIER_EVIDENCE.md) confirms595's
unchanged head and records missing personal-modifier/Focus evidence without
enabling guessed behavior. No new schema/provider setup or runtime deployment.
Historical continuation-verifier Source divergence remains expected; old records
were not altered. Defaults15:1/17:1 remain; no44 or seven baseline auto-grants.
Computer Use/native client launch remain stopped. No commit or push. The overall
Goal remains active for the remaining roadmap requirements.
