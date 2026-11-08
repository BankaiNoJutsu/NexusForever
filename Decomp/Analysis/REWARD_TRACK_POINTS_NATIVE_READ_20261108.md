# Challenge reward-track point amounts: native read (2026-11-08)

Follows [VIDEO_WITNESS_REWARD_TRACK_PASS3_20261108.md](VIDEO_WITNESS_REWARD_TRACK_PASS3_20261108.md). Tier: Ghidra 12.1.4 read of WildStar64.exe (build 16042) plus table rows; decompiler float handling was checked by disassembly where it matters. No source, table or existing file changed; nothing implemented.

## Verdict
The per-medal base is **100 for each of Bronze/Silver/Gold**, read from a game table, multiplied by one client-held float. The footage 100/125/150/200/375 is fully explained by that multiplier and by the banner showing a **cumulative** total. The server's flat 100 per medal with a whole-multiplier snapshot is therefore **not a divergence in the base**; the pass 3 reading "Gold 375 and Silver 200 are base amounts" was wrong.

## 1. Where the amounts come from
| Item | Finding |
|---|---|
| Lua enum | `keRewardTrack` is local to ChallengeLog.lua: Bronze 0, Silver 1, Gold 2 (argument is a tier index, not a challenge). |
| `ChallengesLib.GetRewardTrackPoints` | binding slot `140b73900`, function `140711110`. Returns trunc(float32(base[tier]) * mult). Tier index goes to `14079d2f0`. |
| `GetRewardTrackInfo` | slot `140b73910`, function `140711220`. Builds `tBase`, `tFinal`, and `tBonuses` (only when a bonus exists). |
| `GetRewardTrackByZone` | slot `140b73920`, function `140711010`; zone id to RewardTrack lookup (not needed for amounts). |
| Base amount | `14079d2f0(tier)` caches `GameFormula` row **0x4F4 = 1268** via `140200220` (GameFormula table accessor). Tier 0/1/2 read Dataint0/01/02 (row offsets +4/+8/+0xC); tiers 3/4 would read Dataint03/04. |
| Row 1268 (I:\tools\extract\DB\GameFormula.tbl) | ints 100, 100, 100, 0, 0; floats 0.25, 0, 0, 0, 0. So base 100 per medal; float0 0.25 equals the Signature/Loyalty +25%. |
| Multiplier | `1403ac060(DAT_140c65898+0x1bf8, 4, 3)`: ordered map keyed (property 4 = RewardTrackType, data 3 = Challenge); returns the entry's float at +0x34 when value type is 0 or 2 (additive/scalar), default **1.0** (`DAT_140b79ed0` = 0x3F800000). The map is filled from `ServerPremiumRewards` (the server already sends the whole multiplier). |
| Arithmetic (disasm `1407111b8-1407111f7`) | `cvtsi2ss base; mulss mult; cvttss2si` per tier; truncates each medal separately. |
| `tFinal` (cumulative) | nBronze = t0; nSilver = t0 + t1; nGold = t0 + t1 + t2, each ti = trunc(base * mult). `tBase` is the same sum of untruncated bases. `tBonuses.fSignature` = sum of entries of kind 6, `fLoyalty` = sum of kind 5 with sub-id 0x2C, shown only when abs(value) passes a threshold (the decompiled threshold looks like an int-cast artefact; unverified). |
| Signature source | RewardPropertyPremiumModifier row 223: system 1, tier 1, property 4, data 3, float 0.25. Loyalty (entitlement 44, description id 721801 "Increases the points earned from Challenges by 25%") adds another 0.25 on Basic accounts. |

## 2. Banner and chat line
- Banner (ChallengeDisplay.lua `SetChallengeView`): title uses text id 740166 `$1n Completed: $2n` (tier name, challenge name); button text uses text id 740010 `$c Challenge Points Earned` with `nReward = tFinal[nCurrentTier]`, i.e. the **cumulative** total of medals up to the earned tier, computed purely client-side from row 1268 and the (4,3) multiplier. No packet carries the banner number.
- Chat line: text id 735170 `You gained $2c points for $1n.` (Lua key `ChatLog_Progress_Challenge`, ChatLog.lua line 799; key-to-id mapping inferred from identical text, not read from a string table). `ChatLog:OnChannelUpdate_Progress` posts it to the Loot channel only when the track type is Challenge (3) and `nGain` is set.
- Source of `nGain`: packet handler `1404d18e0` (opcode 0x07D5, `ServerRewardTrackUpdate`) calls `1404d13a0`, which stores points at track +0x24 (the packet's RewardPointsEarned, a running total), fires `RewardTrackUpdated`, and if the new total is greater than the old and the static track type is not 2 (Loyalty) calls `1404283d0` with gain = new - old. That raises `ChannelUpdate_Progress` with `{rtCurr, nGain}`. So the chat number is the **delta between two consecutive server totals**, and a track reload (0x07D3 then 0x07CD) prints nothing.
- Consequence: the zone track (type 4) never prints this chat line; "Reward Track - Arcterra" with 100 was the challenge track 36, not the zone track 35.

## 3. Footage values against the reading
| Clip value | Explained by | Fit |
|---|---|---|
| Bronze 100 (W, C), Silver 200 (S, P) | multiplier 1.0: 100 / 200 cumulative | yes |
| Bronze 125 (H Basic, K Sig), Gold 375 (F Basic, T Sig) | multiplier 1.25: 125 / 250 / 375 cumulative | yes (Sig row 223, or Loyalty on Basic) |
| Bronze 150 (M, Sig, pre-live) | multiplier 1.5 = 1 + 0.25 Signature + 0.25 second source | yes, second source unproven |
| Chat 100 beside banner 200 (S, P Silver) | banner cumulative (two medals), chat = one update's delta | fits if the Bronze 100 was a separate earlier update; the earlier line was not on screen |
| Chat 375 beside banner 375 (T Gold) | one update carrying all three medals, or Bronze and Silver were already counted | open (see unknowns) |
Basic accounts at 1.25 (H, F) point to the Loyalty challenge bonus (entitlement 44), not a temporary event.

## 4. What the server should do
Current server: base 100 per medal literal in `CharacterChallengeRewardRepository.cs` (line 172) and `ChallengeManager.LiveRewards.cs` (line 36); whole multiplier = 1 + raw delta + loyalty (`ChallengeRewardProperty.cs`); each medal truncated separately (matches native); medals advance one at a time. This already matches the native formula for the data in row 1268.
Proposed changes (not done):
1. Replace the literal 100 with GameFormula 1268 Dataint0/01/02 selected by medal index 0/1/2 (fail closed if the row is missing or a value is 0). No behaviour change today; removes a magic number and aligns the client banner and server credit if the table is ever edited.
2. Keep `ServerRewardTrackUpdate.RewardPointsEarned` as the post-credit running total (the client derives the chat gain from it). Never resend a stale or lower total, and avoid a no-op update that would hide a gain.
3. Replace the 0.25f literal in `SetCommittedLoyalty` with GameFormula 1268 float0 (the comment already notes the corroboration).
Tests to add (ChallengeMedalPointsTests or a new data-backed class):
- Row 1268 gives {100,100,100} and float0 0.25 from the real GameTable.
- Multiplier 1.0 / 1.25 / 1.5: cumulative Bronze/Silver/Gold = 100/200/300, 125/250/375, 150/300/450 using per-medal truncation.
- Two consecutive medal credits produce two track updates whose totals differ by exactly the medal increment (chat nGain), and a Gold-jump credit sums to 375 at 1.25.
- Signature premium row 223 alone gives multiplier 1.25; with loyalty 1.5.
- Zone track (type 4) credit does not rely on this path.

## 5. Remaining unknowns
- Whether retail credited medals one update at a time (time-tiered Drift Dash showed chat 100 against banner 200) or all newly earned medals in one update (Thayd Sprint chat 375). The server's per-medal queue emits one update per medal; time-tiered challenges are excluded from the durable producer (`SupportsDurableProducer`), so this ordering is still unproven for them.
- Why the PTR Signature clip P showed multiplier 1.0 (test account or pre-launch data); the 150 source in M (second 0.25).
- Server-side zone-track (type 4) and public-event amounts: unaffected by this path, still unwitnessed.
- Threshold that gates `tBonuses` display (decompiler artefact); string-table key to id mapping for 735170/740010/740166.

Lead review (2026-11-08): the table fact was re-checked: `GameFormula` row 1268 holds Dataint0/01/02 = 100/100/100 and Datafloat0 = 0.25 (bits 0x3E800000). The native claims (bindings, the multiplier map filled by `ServerPremiumRewards`, banner totals computed client-side, the chat line only for challenge tracks) are the agent's reading and were not re-derived. Conclusion: the footage values fit a flat per-medal base of 100 times a multiplier (1.0, 1.25, 1.5) with the banner showing **cumulative** totals (100/200/300, 125/250/375, 150/...), so the server's flat 100 per medal is consistent with native and the "divergence" suggested by [VIDEO_WITNESS_REWARD_TRACK_PASS3_20261108.md](VIDEO_WITNESS_REWARD_TRACK_PASS3_20261108.md) is withdrawn. No code change; reading the base from `GameFormula` 1268 instead of the literal 100 would be hygiene only.

