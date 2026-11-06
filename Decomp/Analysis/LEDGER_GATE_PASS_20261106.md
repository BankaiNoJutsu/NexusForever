# Ledger gate pass (2026-11-06)

Pass over the "Current next evidence and implementation queue" of ASSESSMENT_REMAINING_WORK.md after
the scalar-save (9a0939a0) and quest AccountItem (1edfaf74) slices. Verified against current source by
grep/read; Source/SQLite is the only runnable tier on this checkout. Each row names the exact missing input.

| Item | Status | Gate / missing input |
|---|---|---|
| Contract public 07CE (types 0/1) | Unwired on purpose | Raw 481759-byte table (sha256 6a46e0e5...) for full-table delivery; retail witness for unclaimed-milestone / excess-point reset edges |
| Cross-guild durable replay, notification atomicity, broker/restart | Open | Configured MySQL, socket and broker runtime |
| MySQL / copied-upgrade / socket / load qualification | Open | Fresh disposable MySQL and deployment runtime; none run here |
| 609 Communicator finite-bound correction (22 cases unbuilt) | Open | Raw TBL, sniff or runtime corroboration (Verified tier) |
| #272 track-35 Zone earning, Challenge producers/odds, Hycrest triggers | Open | Native or retail witness |
| #595 shield healing / Focus / lifecycle | Open | Shield-specific native witness |
| #557 Armor Pierce ordering (`DamageCalculator.ApplyArmorMitigation` ignores `RatingArmorPierce`) | Open | Verified ordering corroboration beyond static evidence |
| Broader AMP families, #463 | Open | Retail ordering and triggered-child semantics |
| Q5597 quest/achievement qualification | Open | Retail data |
| Primal Matrix save (handler is trace-only) | Open | Schema/DDL decision and essence-debit rules |
| Friendship auto-response persistence | Open | DDL and retail restore/owner witness |
| Quest reward types 8/9/10 | Open | Table binding of ObjectId and grant routine (see QUEST_ACCOUNT_ITEM_REWARD.md) |
| Duel full damage/death/respawn | Open | Retail death/respawn behavior unverified; not implemented speculatively |
| Mixed-bundle ordinary quest reward atomicity | No gap reproduced | `SaveOrdinaryQuestMutation` quarantines and reloads on failure; existing "mixed" variant covers Item rollback. No new failing test could be written |
| Scalar Character save acknowledgement | Done | PLAYER_SCALAR_SAVE_RETRY.md (Source/SQLite) |
| Quest AccountItem reward | Done | QUEST_ACCOUNT_ITEM_REWARD.md (Source/SQLite) |

No claim of retail parity follows from any Source/SQLite entry above.
