# Video witness V1, pass 3: reward track points per activity (2026-11-08)

Follows [VIDEO_WITNESS_REWARD_TRACK_PASS2_20261108.md](VIDEO_WITNESS_REWARD_TRACK_PASS2_20261108.md), [VIDEO_WITNESS_REWARD_TRACK_20261108.md](VIDEO_WITNESS_REWARD_TRACK_20261108.md), [ZONE_REWARD_TRACK_35_EVIDENCE.md](ZONE_REWARD_TRACK_35_EVIDENCE.md); rules in [VIDEO_WITNESS_GOAL_PROMPT.md](VIDEO_WITNESS_GOAL_PROMPT.md).
Tier: **Observed** only (on-screen banners, chat lines, panels; all uploads before 2018-11-28 per `_INDEX\index.csv`), Correlated where a table row agrees. No parity claim, no code or existing file changed.
New in this pass: the client prints the credited amount in two places that earlier passes did not use: a top-centre banner "Bronze/Silver/Gold Completed: <challenge>" with "N Challenge Points Earned", and a chat line "You gained N points for Reward Track - <zone>." (Loot channel). Those give per-activity points directly, for challenge tracks (type 3).

## Clips (all `I:\Videos\_Additional`, URL = youtube.com/watch?v=<id>)
| Tag | File id, uploader, date | Era, account | What is read |
|---|---|---|---|
| P | Arcterra first look [AyXyE6qWnMU], Chris Light, 2016-02-25 | PTR, Signature | 438 s banner and chat; panel counters 800/807/945-1126/1255-1745 s |
| S | Spirited Snowdrift Sprint [0HV9MAWUeco], Balkan Gear, 2016-04-07 | live (week 1), BASIC bar, lvl 50 | 58-66 s |
| W | Mutually Assured Destruction [Gqh0z_-v1tw], Llama Prime, 2016-05-01 | live, BASIC, lvl 28 | about 300 s (OCR time 304) |
| H | Watcher's Warning [GeaIZfzsLRc], Wintherman, 2016-10-08 | live, BASIC, lvl 10 | 185 s |
| F | Fire Brigade [2yVGDM8XVgs], Wintherman, 2016-11-01 | live, BASIC, lvl 16 | 64 s |
| C | Colt Plays Wildstar [0V8toG-7FwY], MMOHuts, 2017-02-06 | live, BASIC | 3176-3190 s (854x480) |
| T | Thayd sprint [Qlpuqqwz4aQ], LMC Digital bytes, 2018-06-07 | live, SIGNATURE, lvl 50 | 63 and 75 s |
| K | Skeech challenge [icEIMqRL0iM], mmodev, 2016-01-17 | pre-live, SIGNATURE, lvl 6 | 177 s |
| M | F2P Engineer overview [5aSVf9uvp4g], Kid Lee, 2015-12-30 | pre-live, SIGNATURE | 736 s |

## 1. Points per activity: challenges (type 3 tracks), banner versus chat
| Clip | Challenge, tier | Banner "Challenge Points Earned" | Chat "gained N points for Reward Track - X" |
|---|---|---|---|
| K | Skeech Slayer, Bronze (Sig) | 125 | 125 (Introductory Zones) |
| M | Moodie Massacre, Bronze (Sig) | 150 | not visible |
| S | Diabolic Drift Dash, Silver (Basic, time-tiered) | **200** | **100 (Reward Track - Arcterra)** |
| P | Diabolic Drift Dash, Silver (Sig, PTR) | **200** | **100 (Reward Track - Arcterra)** |
| W | Doing Things The Hard Way, Bronze (Basic) | 100 | 100 (Whitevale) |
| H | Horn of Heralding, Bronze (Basic) | 125 | 125 (Celestion) |
| C | Firestorm Fighter, Bronze (Basic) | 100 | 100 (Celestion); Codex then reads "Reward Track - Celestion (100 Points)" |
| F | Cooling Down, Gold (Basic) | 375 | line not on screen |
| T | Thayd Sprint, Gold (Sig) | 375 | 375 (Capital City) |
Correlation (Challenge.tbl / ChallengeTier.tbl, scratch reader): Skeech Slayer 103/336 = track 16, tier goal 5 (single tier); Firestorm Fighter 449, Moodie Massacre 476, Horn of Heralding 480 = track 18 (thresholds 300/600/900/1200), single tier goal 5; Cooling Down 154 = track 21, goals 4/6/8; Drift Dash = challenge 1134, track 36 (type 3 Arcterra, 300/600/900/1200), time tier counts 0/108000/138000 ms. No table column carries the per-tier point amount, so the figures are not derivable from the recovered rows.
Readings:
- Bronze is not one constant: 100 (W, C), 125 (H, K), 150 (M). The same Celestion track 18 shows 100 (Firestorm Fighter, Basic, 2017), 125 (Horn, Basic, 2016-10) and 150 (Moodie, Signature, 2015-12). Gold 375 appears on both a Basic account (F) and a Signature account (T), so that figure is not "300 plus the Signature bonus" for T alone.
- Chat credited amount equals the banner in every zone shown except Arcterra, where two independent clips (PTR Signature and live Basic, six weeks apart) agree on banner 200 and chat 100 for a Silver. Both tracks 35 (zone) and 36 (challenge) are titled "Reward Track - Arcterra", so the chat line does not say which one was credited.

## 2. Arcterra zone track (type 4) counter readings
All from P (PTR, threshold 600000, so a pre-final scale; see pass 2 for the 6,000,000 versus 600,000 mismatch). Map side panel "Reward Track - Arcterra", counter read by crop:
| Time (s) | Counter | Note |
|---|---|---|
| 800 | 937519 / 600000 (156%) | before claim click (pass 2) |
| 807-825 | 337519 (56%) | after claim: exactly minus 600000 |
| 945, 955, 970, 1120, 1126 | 355295 / 600000 (59%) | +17,776 since 825 |
| 1255, 1265, 1420, 1480, 1625, 1665, 1745 | 376034 / 600000 (62%) | +20,739 since 1126; then unchanged for about 490 s |
What the footage supports: (a) between 825 and 940 s a combat challenge "Hunt Coldwater walatusks" was active the whole time and no completion banner or quest turn-in was seen before the map opened, yet the counter rose by 17,776; (b) between 1126 and 1255 s the player was in Soulfrost/Renhakul public-event content and killing mobs (loot burst stamped 20:29: 6382 and 3191 Elder Points lines), the counter rose by 20,739; (c) from 1255 to 1745 s a combat challenge (Moving Mountains) was active and the counter stayed at 376034 across checks at 1480 and 1625 s. The counter is only visible when the map is open, so each delta is a bundle of unseen activity; no single event, kill or quest is isolated. A per-activity zone amount is therefore NOT established. The Drift Dash Silver at 430-438 s credited 100 to "Reward Track - Arcterra" (chat), on a scale where 100 is small against 600000, which suggests that line is the challenge track 36, not the zone track.
Question 1 verdict: challenge amounts per tier above are observed; public event, quest and kill amounts for the zone track are not. Other zone tracks: MapZone.tbl has only 476 (Arcterra) with a RewardTrackId (35), unchanged from the 2026-11-06 record; all other visible tracks (Introductory Zones, Celestion, Whitevale, Capital City, "Pocket Cap (450 Points)" in 367, 2016-07-22) are type 3 challenge tracks.

## 3. Final-build numbers (after 2016-03-30)
No post-launch frame with a zone-track counter was found. Panel-hit OCR sweeps (6 s step) of the dated post-launch Arcterra videos 6590 (U_-1hJX99W8, GER release walk), 6591 (hv9oXr8nsWw), 6597 (bDh7U72ASqc), 6600 (T4gPDHYhZUw) and 6601 (5Cj8FVikdUc, 2016-04-08, 8395 s) returned no "Reward Track - Arcterra" panel, counter, banner or "gained N points" line (OCR on 1920 px frames; low-resolution text can be missed, so this is "not found", not "absent"). Therefore 6,000,000 versus 600,000 stays unresolved for the zone track. Only challenge-track amounts exist post-launch (S 2016-04-07 to T 2018-06-07), and the 2018 Gold 375 and chat line format is unchanged from 2016.

## 4. Last threshold of a multi-tier track
Not witnessed. The highest readings seen are "Reward Track - Celestion (750 Points)" (M, 2015-12-30, between thresholds 600 and 900) and "(450 Points)" Pocket Cap (367). No clip reaches 1200 (or 300 on track 16) or shows the panel after the last milestone. Pass 2 already showed the zone track subtracts one threshold at claim (937519 to 337519 and 1022313 to 422313), which is the only reset behaviour observed.

## 5. Signature bonus (+25%)
- Challenge tracks: Introductory track 16 reads 100 for a Basic Bronze (pass 2 C, 2015-10-17, header "(100 Points)") and 125 for Signature (K, banner and chat 125; pass 2 D header 125). That agrees with the commentary of +25% for Signature on challenge reward points. Contradicting data: Basic accounts show 125 for Horn of Heralding (H) and 375 for Gold (F), while a Basic Firestorm Fighter Bronze is 100 (C). Whether late-2016 Basic accounts received a temporary bonus or Bronze base values differ per challenge is not resolved by this footage.
- Zone track (type 4) and public events: not tested. The Elder Point lines in P show "Bonus Signature Elder Points" at 25% of Elder Points (1519 of 6382, 759 of 3191), which is the XP-side bonus, not track points. In C the codex shows a "Get Bonus Points!" button on a Basic account, consistent with Signature affecting track points. The chat amount for Drift Dash was 100 on both a Signature (P) and a Basic (S) account, so no +25% appeared on that credit.

## Effect on 07CE / track gates (Observed, not implemented)
- Per-tier challenge credit values are challenge-specific data not present in Challenge/ChallengeTier/RewardTrack rows; the gate still needs the source of the amount (client-visible only via the banner/chat).
- Chat and banner lines give the client-visible acknowledgement of a track credit; the type 4 earning producer stays unwitnessed. Public 07CE and zone claim stay disabled.
- Claim semantics unchanged from pass 2: player click, subtract one threshold.

## Still missing
- Zone counter read immediately before and after one isolated public event, quest turn-in or challenge, post-launch (needs a clip that keeps the map open or shows the chat line "gained N points for Reward Track - Arcterra" after a public event; none among the 6590/6591/6597/6600/6601 sweeps).
- A Basic and a Signature account completing the same challenge tier (settles the +25% question).
- A track panel past its last threshold.
- Date of the 125/375 versus 100/200 scale (between 2016-05-01 and 2016-10-08).

## Queries and sweeps tried
FTS: reward track, challenge points, reward points, points earned, 375 points, 125 points, bonus points, Get Bonus Points, challenge log, Icebound Pouch, On the Right Track, Soulfrost, reward progress (few dated hits, mostly speech only). Existing OCR (`ocr`, `ocr2`, challenge_videos.json, 160 videos) searched for Reward Track, gained, Challenge Points, Points), Completed (note: `ocr2` rows use fields `w`/`b`, not `text`). New 6 s and 5 s OCR sweeps of the five Arcterra videos and clip P. A 3 s chat OCR of Wildman Challenge Ep3 (4167) returned no text (480p), so the Wildman series (about 40 videos, dated 2016-08 to 2017-07) was not harvested; it is the best remaining source for banner/chat pairs but needs a higher-quality text read.

Review note (lead, 2026-11-08): written by a research subagent; the readings were not re-viewed by the lead. Cross-check with the server: `CHARACTER_CHALLENGE_LIVE_PRODUCERS.md` and `ChallengeManager` credit one 100-point base increment for every medal, while this record reports Gold 375 and an Arcterra Silver banner of 200 in dated retail clips, so the per-tier base amounts look like a divergence. The amounts are not derivable from the Challenge, ChallengeTier or RewardTrack rows; a scan of `GameFormula` for 100/125/150/200/375 constants was too noisy to attribute. Next evidence: the native read of the Lua binding `ChallengesLib.GetRewardTrackPoints` (used by `ChallengeLog.lua` for `keRewardTrack.Bronze/Silver/Gold`) and of `GetRewardTrackInfo.tFinal`, which should say where the client gets the three amounts (a Ghidra job, queued behind the running native read).

