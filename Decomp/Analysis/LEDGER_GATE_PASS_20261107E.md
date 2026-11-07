# Ledger gate pass E: queue re-read after the prerequisite audit (2026-11-07)

Evidence tier: reading of the "Current next evidence and implementation queue" in `ASSESSMENT_REMAINING_WORK.md` (items 1 to 4 and the 5a to 5j list carried in [LEDGER_GATE_PASS_20261107C.md](LEDGER_GATE_PASS_20261107C.md)), the cited gate records, and spot checks listed below. Source/SQLite unless stated. **Not completion and not a retail-parity claim.** "Re-read" means I re-read the gate text and the records it cites; I did not re-derive them.

## Why another pass
The prerequisite work of this stretch ([PREREQUISITE_SHAPE_AUDIT_20261107.md](PREREQUISITE_SHAPE_AUDIT_20261107.md)) showed that some items filed as "gated on native evidence" were reachable by reading the client more carefully (jump-table dump, packet row reader plus update consumer) and that some earlier mappings were wrong. So each gated item below also states whether its missing input could live in the client binary at all.

## Spot checks this pass
- 07CE: the client sender is already labelled (`RewardTrack_SendClientClaimRewardPoint`, `1404d1490`: reward row id plus a uint32 choice, zero choices send `FFFFFFFF`, one choice forces zero); a search for the immediate `0x7CE` found that function and one generic function (`14006c290`) only (indirect uses were not searched). What it cannot show is the server's reset policy, so nothing client-side is missing for the sender; the gate is server behaviour.
- `ClientRewardTrackHandlers` claim switch still has no Contract case; public 07CE stays unwired. The 15:1 and 17:1 entitlement defaults and account currencies 15/17 were not touched.
- The 481,759-byte Communicator table is still absent; native client launch and Computer Use were not used.

## Per-item gates
| Item | Could the missing input be in the client binary? | Gate (what is needed) | Status |
|---|---|---|---|
| 1. Public 07CE claim/reset | Only the sender and presentation (already read); reset edge rules (unclaimed milestones, excess points) and timing are server-side | Retail capture or sniff of a reset with unclaimed milestones and excess points; `nChoiceIdx` to the table's three columns with full-table delivery | Gated; do not wire speculatively |
| 2a. Guild/housing/achievement publication integrations | n/a | Cross-guild durable replay needs a broker/restart harness | Done at recorded gates; remainder runtime |
| 2b. Marketplace flows | n/a | Full public flow on MySQL profiles, physical failures | Done at recorded gates; replay pins added this stretch |
| 2c. Cross-character notification atomicity, other atomic financial settlement, live broker/restart | n/a | A broker/restart harness not in the repository (no Source/SQLite witness) | Open, runtime |
| 2d. Mail/Item audit, attachment index receive/update | The receive/update handler is client-side but the correction needs independent Verified corroboration (sniff) before a wire change | Sniff/retail corroboration of `nServerIndex` | Done at recorded gates; index correction not implemented |
| 2e. Revised 604 taxi, physical travel | No | Runtime/retail witness | Reconciled; physical unqualified |
| 3. Fresh MySQL, model/copied-upgrade, full-table delivery, sockets, load | No | MySQL 8.4.13 instance and socket/load harness; actual full table | Partly done on 8.0.46 (49/49 classes under production options); 8.4.13, sockets, load open |
| 4. 609 native/actual-table qualification | The table content is not in the binary | The 481,759-byte table `6a46e0e5...` (or independent confirmation) and Verified corroboration | Gated on an absent asset |
| 5a. Track 35 Zone earning | Producer is server-side | Producer witness | Gated |
| 5b. Challenge producers, retail odds | Server-side | Retail witness | Gated |
| 5c. AMP families beyond the supported ones | Server formula/ordering | Server formula evidence; not in the client or tables | Gated |
| 5d. #595 personal healing/Focus | Property ownership is server-side | Producer and ownership evidence | Gated |
| 5e. #557 piercing/mitigation order | Static client armor-pierce read exists | Pairwise damage path and independent order evidence | Gated |
| 5f. Prerequisite/protocol semantics | Yes, and this is where the client was productive | Per-type gates in [PREREQUISITE_REMAINING_TYPE_GATES_20261107.md](PREREQUISITE_REMAINING_TYPE_GATES_20261107.md) and the audit; open: 24/25 (phase-mask source), 219 (volume bits), 107/119/122/199/200 (public-event participation/state fields), 149, 9, 146, PvP ratings 251 to 255, hub avenues 150 to 152, the 2,281 client-stub slots | Worked; remaining slots each blocked on a server model or a witness |
| 5g. Hycrest triggers | Physical triggers | Native/retail witness | Gated |
| 5h. Q5597 smoke | Needs the native client | Native UI/persistence smoke; native launch is stopped | Gated |
| 5i. Telegraph shapes | Partly done (shapes 3 and 6 implemented, inferred) | Shape 6 hit capture; box mesh extents | Worked, inferred |
| 5j. Quest reward type 10, per-type achievement credit, 2,478 spell-effect rows, persistence decisions | Mostly server-side semantics | Currency mapping witness, producer witnesses, DataBits semantics, schema decisions | Gated |

## Result
No queue item is both unblocked and unfinished in the Source/SQLite lane except the prerequisite slots named in 5f, and each of those names a missing server model or witness (public-event participation slot, phase mask source, volume bits, avenue points, PvP rating storage). Items 2c and 3 need a runtime harness. This is a stopping-condition statement for the Source/SQLite lane **at this evidence level, not completion**: it does not exclude that reading the client further would unblock a 5f slot, as happened repeatedly this stretch.
