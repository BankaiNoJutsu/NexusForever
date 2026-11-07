# Ledger gate pass C: the whole "Current next evidence and implementation queue" (2026-11-07)

Evidence tier: reading of every queue item (current items 1 to 5 and the historical items 1 to 5 of `ASSESSMENT_REMAINING_WORK.md`), spot re-verification in the worktree (below) and the work recorded in the
linked records. Source/SQLite unless stated. Not completion and not a retail-parity claim. Earlier passes: [LEDGER_GATE_PASS_20261106.md](LEDGER_GATE_PASS_20261106.md),
[STOPPING_CONDITION_20261107.md](STOPPING_CONDITION_20261107.md), [LEDGER_GATE_PASS_20261107B.md](LEDGER_GATE_PASS_20261107B.md).

## Re-verified in the worktree this pass
- Public 07CE stays unwired: `ClientRewardTrackHandlers.cs` claim switch has Loyalty and Character Challenge cases only; the choose switch routes types 0 and 1 to `ChooseContractRewardTrack`, there is no Contract claim case.
- The 481,759-byte Communicator table is still absent: only the 494,256-byte `CommunicatorMessages.tbl` exists under `I:\tools\extract\DB`.
- Account currencies 15 (Crimson Essence) and 17 (Viridian Essence) are unchanged in `AccountCurrencyType`; the 15:1 and 17:1 entitlement defaults were not touched by any commit in this stretch (not re-derived from migrations here).
- Native client launch and Computer Use were not used.

## Current queue

| # | Item | Status | Gate / next input |
|---|---|---|---|
| 1 | Contract public claim/reset (07CE) | Gated | Reset edge rules (unclaimed milestones, excess points), native `nChoiceIdx` mapping to the table's three columns and native timing; no evidence in client Lua or decompiled presentation code. Do not wire speculatively |
| 2a | Guild disband, name lifecycle, creation, achievements, community, donation, rename, privacy, directory, remodel, placement, realm-first, titles, archive, tradeskill publication | Done at their recorded gates (the queue says do not repeat) | Cross-guild durable replay and live broker/restart remain (runtime) |
| 2b | Marketplace posting, bid, buyout, expiry, cancellation, commodity posting/return, settlement, lost acknowledgement | Done at recorded gates. **This pass** added replay pins under a strategy that really replays once: auction posting (2 cases) and, in `NormalAuctionBidCommandTests`, commodity return (2), buyout (2), cancellation (2) and expiry (2) lost-acknowledgement rows: each settles exactly once and the test counts the replay | Full public flow on MySQL profiles and physical failures; MySQL's own transience rules are not exercised by the SQLite strategy |
| 2c | Cross-character notification atomicity, other atomic financial settlement, live broker/restart | Open, runtime | Needs a broker/restart harness; no Source/SQLite slice with a witness |
| 2d | Legacy mail action/publication, ordinary Item audit, attachment index (receive/update) | Done at recorded gates; attachment-index correction not implemented | Independent Verified corroboration of the nServerIndex receive/update behavior (retail/sniff) |
| 2e | Revised 604 taxi, physical/provider travel | Reconciled; physical travel unqualified | Runtime/retail witness |
| 3 | Qualify current binaries on fresh disposable MySQL, model/copied-upgrade, full-table delivery, socket/deployment, load | Partly done: fresh and copied-upgrade migration on 8.0.46, plain-options and string-generator runs of 59 classes, 20 of 55 classes on the plain retry strategy, and (later this pass) all 49 harness classes passing under the production retry options with user-opened transactions exempted ([MYSQL_RETRY_CONVERSION_YIELD_20261107.md](MYSQL_RETRY_CONVERSION_YIELD_20261107.md): portability and production-options compatibility, not retry evidence for test-opened units) | MySQL 8.4.13, socket, broker, load, deployment: runtime/harness not in the repository |
| 4 | 609 native/actual-table qualification | Gated | The 481,759-byte table `6a46e0e5...` (or independent confirmation); Verified table/runtime/sniff corroboration before changing the zero-bound predicate; full-table delivery, native timing/UI, retail qualification |
| 5a | Track 35 Zone earning | Gated | Producer witness ([ZONE_REWARD_TRACK_35_EVIDENCE.md](ZONE_REWARD_TRACK_35_EVIDENCE.md)) |
| 5b | Unsupported Challenge producers, retail odds | Gated | Retail witness |
| 5c | Broader AMP families | Gated | Server formula/ordering evidence; not in the client or tables |
| 5d | #595 personal healing/Focus | Gated | Producer and property ownership evidence ([HEALING_PERSONAL_MODIFIER_EVIDENCE.md](HEALING_PERSONAL_MODIFIER_EVIDENCE.md)) |
| 5e | #557 piercing/mitigation order | Gated | Pairwise damage path and independent order evidence, coefficients above the rating threshold ([NATIVE_STATIC_ARMOR_PIERCE_EVIDENCE.md](NATIVE_STATIC_ARMOR_PIERCE_EVIDENCE.md)) |
| 5f | Remaining prerequisite/protocol semantics | Worked this stretch: types 5, 42, 50, 59, 60, 130, 280 corrected; types 1, 4/250, 73, 109, 214, 190, 245/275, 76, 103/104 read without change. Remaining gates are per type in [LEDGER_GATE_PASS_20261107B.md](LEDGER_GATE_PASS_20261107B.md) | Native field identities (4/250 packet fields, 214/190/245/275 flags and lists, 54/110/53 mission virtuals, 9 episode writer), server-only concepts (47, 76, ...), PvP rating producers |
| 5g | Hycrest later gameplay/physical triggers | Gated | Native/retail witness for cinematic and physical triggers |
| 5h | Q5597 quest/achievement-specific qualification | Gated | New quest/achievement-specific native UI and persistence smoke; native launch is stopped |
| 5i | Telegraph shapes 3 and 6 (263 rows) | Gated | Client renderer shape code (builder chain has no shape branch); even then only geometry |
| 5j | Quest reward type 10 (86 rows), per-type achievement credit, 2,478 spell-effect rows, persistence design decisions | Gated | As in STOPPING_CONDITION_20261107.md (currency mapping witness, producer witnesses, DataBits semantics, schema/DDL decisions) |

## Historical queue (provenance)
Its items 1 to 5 restate the above (general reward tracks, #595, #557, AMP/protocol/Hycrest, Q5597); each is either marked done in its own text or gated as listed. No separate item exists there.

## Recorded observations that are not restoration slices
- `GroupPlayerInviteHandler.Handle` and `GroupMemberRequestHandler.Handle` (only these two; the other eleven Group/Friendship/Chat strategy files publish nothing inside the delegate) publish a message before commit inside a replayable strategy delegate (upstream design; gate: upstream decision, publish after commit or an outbox).
- `AuthDatabase.TryAddStorePurchaseHistory` has no idempotency key for a replay after a lost commit acknowledgement (theoretical for the MySQL strategy; gate: a schema decision for a purchase key).

## Conclusion
After this pass no queue item is both unblocked and unfinished except runtime-harness work (broker/restart, MySQL 8.4.13, socket, load); the bulk test-fixture conversion is replaced by the tolerant-mode census, which passed. Every other item
names its blocking input above. This is a stopping-condition statement for the Source/SQLite lane, not completion.
