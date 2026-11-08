# Video witness V2 (third pass): does retail persist the final tier? (2026-11-08)

Goal: [VIDEO_WITNESS_GOAL_PROMPT.md](VIDEO_WITNESS_GOAL_PROMPT.md); follows [VIDEO_WITNESS_CHALLENGE_TIERS_20261108.md](VIDEO_WITNESS_CHALLENGE_TIERS_20261108.md) (divergence 1: the server never counts the final tier in `CurrentTier`). Question from that record: is a persisted completed-final-tier representation needed, or is a runtime-only flag enough?
Evidence tier: **Observed** (dated pre-shutdown retail footage, below) plus a **native read** (two functions). No code changed; no retail parity claim.

## Provenance rule applied
Only footage uploaded before the retail shutdown (2018-11-28) counts as retail; the index has 6,111 such videos, 277 later uploads and 490 undated ones. Later or undated footage can come from an emulator, so it is a lead only. Consequence for earlier records: the Everstar Grove challenge tutorial used in [VIDEO_WITNESS_CHALLENGE_20261108.md](VIDEO_WITNESS_CHALLENGE_20261108.md) is **undated, provenance unconfirmed** (its table correlation stands, its on-screen "Bronze Completed ... 100 Challenge Points Earned" is downgraded to a lead). The Arcterra preview (official channel, 2016-03-08), Thayd Sprint (2014-06-05) and Might as Well Jump (2014-06-27) are dated retail.

## Footage (dated retail)
`_Additional/Wildstar Medic PvE DPS Guide [...].mp4` (uploaded 2016-06-17, 1280x720), 526 to 542 s. The Codex "Challenge Log" tab lists challenges with three medal icons per row. Completed tiers carry a green check: Sticky Supplies shows `0/6 check`, `0/12 check`, `0/18` (third pending); Living Stone and Coastal Scarhide Executioner each show `100% check`, `100% check` and a pending third medal. At 536 s the player has just started Coastal Scarhide Executioner again: the active widget is at 0% while the log row still shows its two checks. A Reward Track strip ("Pocket Cap (0 Points)") with three milestones sits above the list. (A 2015-08-01 video, `26 Beginner Tips`, shows the same list at 640x360, too small to read the pips.)

## Native read
- `Challenge:GetDisplayTier` (binding `FUN_140685540`, registered at `140c5c870`) reads a **second** per-challenge map at `DAT_140c65948 + 0xd0`, value at `+0x24`. The ChallengeLog addon uses it for the check marks and for "completed with at least one tier is not a failure".
- That map is written by `FUN_14048dde0`, called from two places: the `ServerChallengeResult` consumer (value `Data + 1`, see the addendum of the earlier record) and the `ServerChallengeUpdate` row consumer `FUN_14048ded0` **when the row creates a new client state record** (login or first sight). Disassembly at `14048e0e0`: `MOV R8D,[RBX+0x20]` before `CALL 14048dde0`, so the initial display tier is the row's **CurrentTier** (accepted only when below 4).

## Conclusion for the marker decision
1. Retail shows per-tier achieved checks for past completions, kept while a new attempt runs: the client keeps a best/last-achieved display tier that survives re-activation within a session (Observed).
2. After a relog that display tier can only come from the login row's CurrentTier (native), so retail's server must send CurrentTier = achieved tier count (1 to 3) for a terminal challenge that earned medals, not 0 and not the last medal index. A runtime-only flag would lose this at relog; therefore **a persisted representation is required**, and the Lua `GetCurrentTier` card path and prerequisite 70 need the same count.
3. Not decided by footage: how retail resets CurrentTier for a challenge whose latest attempt earned nothing while an older attempt did (the log keeps the check within a session; the login row after such an attempt is not observed).

## Options for the server (unchanged code)
(a) Derive the login/terminal CurrentTier from the existing medal receipts (the durable model records `CharacterChallengeMedalReceipt` rows per attempt); needs a check that legacy-mode characters have receipts. (b) Add one persisted column (achieved tier count of the last attempt) and relax the `Tier < 3` / terminal `Tier == medal` invariants to match. (c) Redefine persisted `Tier` as the achieved count everywhere. Option (a) avoids a migration if receipts cover every case and is the first thing to verify; (b) is the fallback.
