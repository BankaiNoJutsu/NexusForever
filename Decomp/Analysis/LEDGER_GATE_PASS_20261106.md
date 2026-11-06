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

## Feature matrix pass F-001..F-036 (2026-11-06)

Three read-only passes over MISSING_FEATURE_MATRIX.md rows and dated notes, with source greps for
every claimed gap. B-NAT = native-client or retail witness; B-ASSET = raw TBL/asset; B-SCHEMA = DDL
decision; B-RT = MySQL/socket/broker runtime. Source-only gaps found are listed after the table.

| F | Open items | Gate |
|---|---|---|
| 001 STS | CKeyRsa encoding/padding, TokenKeyData grammar, optional-route startup order | B-NAT |
| 002 Diagnostics | Sender intent/response for 00C8, 00ED, 011B/D, 012D, 0550, 062A/0634, 063E, 0701, 07E3, 0928 | B-NAT |
| 003 Server unresolved | 0x0015 producer; 50 of 62 aux packets emitter-blocked | B-NAT |
| 004 Housing | Edit-mode ack, invite/eviction timing, roommate/donation/decor entitlement semantics | B-NAT |
| 005 Marketplace/CREDD | Live migrations, retail filter/partial-stack semantics, non-empty CREDD rows | B-RT, B-NAT |
| 006 Storefront | 3 live migrations, Protobucks/VC path, 0x026A tail, type-1/2 offers, coupon 0x0790 | B-RT, B-NAT |
| 007 Reward rotation | 0x07CD apply/Flag semantics, per-content mapping, schedule | B-NAT |
| 008 Crafting | Discovery rolls, station service keys, cadence, 0x084B/0x0855/0x056C timing | B-NAT |
| 009 Transport | Service-token bypass, global route state, embark/completion timing | B-NAT |
| 010 Group/matching | Backfill/merge, 0x05CF/0x0600/0x0718, durable raid locks | B-NAT, B-SCHEMA |
| 011 Guild | Bank economy/tab state, influence, perks, warplot plugs, bank-tab storage | B-NAT, B-SCHEMA |
| 012 ICComm/chat | Auto-response owner, persistent channels, chat aux producers | B-SCHEMA, B-NAT, B-RT |
| 013 Mail | Delete/expiry timing, 0x05A1 producer, VIP gate | B-NAT |
| 014 Loot | BOP policy, 0x08A0/0x08A3 timing, roll/master UI parity | B-NAT |
| 015 PvP/duels | Observers, open-world PvP, score/reward parity, queue smoke | B-NAT, B-RT |
| 016 Procs | Unsupported trigger events, ordering/recursion, aux emission | B-NAT |
| 017 Damage/heal | Distribution, SapVital/ClampVital, HoT, rounding, shield parity | B-NAT |
| 018 Stacks/CC | StackTypeEnum arbitration, DR/breakout, force-move | B-NAT |
| 019 Summons/AI | Creature2Action semantics, seat modes, aggro/leash policy | B-NAT |
| 020 RavelSignal | Receiver graph, modes 2+, payload values | B-NAT |
| 021 LAS/AMP | UpdateSpellInProgress transaction, attribute allocation, Primal Matrix catalogs | B-NAT, B-SCHEMA |
| 022 Quests/path | Holdouts, scan minigame, per-event triggers, 3 Creature2 bridges | B-NAT |
| 023 NPE | Client playthrough, VO/SFX, 84387 producer | B-NAT |
| 024 Evil from the Ether | Objective 4942, trigger rows, medals, encounter mechanics | B-NAT |
| 025 Entity/CSI | Entity-stat aux packets, 0x08A8 flags, deferred-action semantics | B-NAT |
| 026 Items/costumes/pets | Aux producers 0x00B7/0x019A/0x037F/0x056B-D, repair durability | B-NAT |
| 027 Options | Result/readback producers, account vs character split | B-NAT |
| 028 Support | Case list/readback payload, cooldown signalling | B-NAT, B-SCHEMA |
| 029 Client DB | Overlay regeneration (external checkout), SMC item 86919 | B-ASSET, B-SCHEMA |
| 030 Realm/PTR | 0x03EF, 0x06EA, PTR handoff, TransferFlag | B-NAT |
| 031 Fortune | Retail per-item weights and rotation | B-NAT |
| 032 Leaderboards | Season/medal selectors, row caps, score definitions | B-NAT |
| 033 Challenges | Reward UI, timed flag 0x8, collect semantics, RNG odds | B-NAT |
| 034 Datacubes | Pickup chain, path-mission archive rule parity | B-NAT |
| 035 Achievements | Steam payload grammar, unmapped types (needs Achievement TBL) | B-NAT, B-ASSET |
| 036 Zone maps | Non-title/path rewards (needs reward column in raw TBL) | B-ASSET |
| Mail attachment index | 0x05A8 receive/update handler unrecovered | B-NAT |
| Housing travel | Physical travel, admission, restart recovery | B-NAT, B-RT |
| Content restoration | 23 queue rows, none retail-complete; 1,787 blocker rows; next Q3741 then Q3777, Q5597 | B-NAT |

Stale ledger entries found: deserter debuff aura (F-010), bonus-ability persistence (F-021) and durable
path-mission persistence (F-022) are already implemented in source.

Source-only gaps found (low impact; none reproduced as a failing test yet):
- Unguarded required-table reads: `UnitEntity.EnforceSpellPropertyStackGroupCap` Spell4 (UnitEntity.cs:1911),
  `Player` TradeskillTier (Player.cs:2538/2550), `ResidenceMapInstance` HousingPlugItem (:618/:836). These tables
  are startup-required, so only robustness.
- F-032 `LeaderboardScoreIngestion` has no callers or tests; the retail score definition remains B-NAT.
- `MailItem.AttachmentDelete` uses a list index without a bounds/identity check (MailItem.cs:321).
- F-020 handler-level RavelSignal dispatch is untested (regression pin only).

## Tooling update (2026-11-06)

Ghidra 12.1.4 (I:\ghidra_12.1.4_PUBLIC) plus a portable Node v24.22.2 (I:\tools, SHA-256 verified) satisfy
REA's doctor for the Ghidra provider (JDK 21 accepted for 12.1.4). The WildStar client binary is at
I:\WildStar\Client64\WildStar64.exe and ClientData.archive/.index exist under I:\WildStar\Patch. The
table-extraction path from those archives is not yet exercised; no native analysis has been run.

## Leaderboard score ingestion pins (2026-11-06)

`LeaderboardScoreIngestionTests` (4 cases, Source/SQLite) cover the previously untested
`LeaderboardScoreIngestion`: zero-time/zero-rating/null-player skips, persisted PvE/PvP rows with
sequential ids and realm id, team-member JSON, background completion callback, and id continuation
after a restart. These are regression pins: they passed on first run, so no defect was found or fixed.
22 Leaderboard cases pass in that filter. Score producers (what a retail match records) remain B-NAT.
