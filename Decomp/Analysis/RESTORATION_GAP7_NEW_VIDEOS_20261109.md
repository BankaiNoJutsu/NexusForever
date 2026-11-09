# Restoration gap 7: telegraph shape 6, pass over the 2026-11-09 corpus additions (2026-11-09)

Question: with the corpus rebuilt on 2026-11-09 (8,448 videos; 1,589 new, 890 of those with subtitles), is there a retail clip where a shape-6 spell is identified on screen and its area can be measured against a known reference from a high camera? Earlier record: [RESTORATION_GAP7_TELEGRAPH_SHAPE6_20261108.md](RESTORATION_GAP7_TELEGRAPH_SHAPE6_20261108.md) (nothing measurable); table facts from [VIDEO_WITNESS_TELEGRAPH_20261108.md](VIDEO_WITNESS_TELEGRAPH_20261108.md).
Result: **No measurable shape-6 clip. The new footage adds one strong negative/redirecting observation: the green 3D boxes are not tied to a Rockfall cast and a long contiguous cube wall in 7201 does not fit the Rockfall rows. Do not implement shape 6 from video.** Observed tier at most; no parity claim.
Review note: written by a research subagent; frames read with the image reader, not re-viewed by the lead. Frames are in the session scratchpad only; nothing was copied into the repository.

**Corpus id warning:** the rebuild renumbered ids. The ids in the 2026-11-08 gap 7 record are stale: old 5613 (Megalith/Mnemesis, `--v2QiD_otI`) is now **7037**; old 5657 (Ohmna guide, `HGhHvrYVNEM`) is now **7091**; old 5744 (Megalith/Pyrobane, `IS_duvvJjWA`) is now **7202**. Always re-resolve by URL.

## Method
- Cue search over new videos only ([scripts/gap_newvideos_search.py](scripts/gap_newvideos_search.py)); title/description candidates by regex over the new rows ([scripts/gap7nv_frames.py](scripts/gap7nv_frames.py) for sheets, frames, probe, crop; ffmpeg 8.1.1).
- Cue queries (new videos, subtitle FTS): megalith 5 hits (all id 7201), rockfall 3 (7201, 35 s and 45 s), "rock fall" / "falling rocks" / "falling rock" 0, "dust devil" 9 (quest and lore talk), thunderstorm 0, tormenting 0, twister 3 (a 2013 livestream), carrot (currency/figure of speech), "fire vent" 0, jetpack 3 (Q and A), blaze (verb/class names), "body slam" 11 (7310, 7317 only), ohmna 0 (subtitles spell it differently), mnemesis 0. Title scan listed the new Megalith, Ohmna and Datascape clips used below. No boss guide naming these mechanics with a measurable view turned up.
- Table lookup ([scripts/gap7nv_tele.py](scripts/gap7nv_tele.py), read-only: Spell4 name -> Spell4Telegraph -> TelegraphDamage, same layout as `TelegraphDamageEntry.cs`). Values below are re-read from the tables this pass.

## Clips reviewed (all upload_date before 2018-11-28, retail player footage)
| id | upload | URL | res | reviewed | outcome |
|---|---|---|---|---|---|
| 7201 | 2014-11-12 | https://www.youtube.com/watch?v=ds82KMuyPQU | 1280x720 30 fps | Datascape 40 Megalith/Mnemesis, spythe, healer POV; sheet of the whole clip at 7 s steps, 30 to 75 s at 1 s steps, 205 to 236 s at 1 s steps, full frames 48 to 53, 205 to 233 | green boxes at about 52 to 56 s and a wall of cubes at about 215 to 232 s; no cast name for them; not measurable (below) |
| 7037 | 2014-11-21 | https://www.youtube.com/watch?v=--v2QiD_otI | 1280x720 | (old 5613) 174 to 190 s sheet, frame 182 | boxes appear from about 181 s; chat at 19:31 holds both "Megalith: Beware of falling rocks!" and "Mnemesis: I will multiply the boundaries of your weakness." |
| 7310 | 2014-07-07 | https://www.youtube.com/watch?v=zy4o0DmjGXA | 1280x720 | Immortal Ohmna kill, 684 to 708 s sheet, frames 692, 693, 695 | "Body Slam" cast bar read (2.2 s at 693 s, 0.2 s at 695 s); red wide wedge from the boss; not measurable |
| 7317 | 2014-07-07 | https://www.youtube.com/watch?v=Pqf1LF6ZfiU | 1280x720 | World first Ohmna, 354 to 370 s sheet | player on a side platform, low camera, boss across the void; "Body Slam" cue only; not measurable |
| 7188 | 2014-12-05 | https://www.youtube.com/watch?v=Pcj4FU5Mdrg | 1280x720 | Aileron/Megalith 40, whole clip at 5 s | high, nearly top-down camera; no green boxes seen at 5 s sampling; no spell name |
| 7195 | 2015-02-13 | https://www.youtube.com/watch?v=b-0g0bevL08 | 1280x720 | Datascape 20 Megalith/Aileron, whole clip at 11 s | no green boxes at that sampling |
| 2731 | 2015-08-28 | https://www.youtube.com/watch?v=airTJSovT7A | 1152x720 | Earth/Air pair "Cymatics", whole clip at 10 s | near top-down; pink and white telegraphs, no green boxes seen at that sampling |
Not viewed: 7167 (Megalith/Mnemesis 20-man, no subtitles), 2750 (Fire/Earth pair), 2264 (Earth/Logic nostalgia kill), 7038, 7151, 7260, 7072 (other Ohmna kills). Whole-clip sampling at 5 to 11 s can miss a 1 to 2 s telegraph, so "no boxes seen" there is weak.

## 7201: what is on screen
- Callout: cues "focus on rockfall" 35 s, "rockfall ... stay away from the rock balls" 45 s, "Rockall Rockall megalith" 105 to 108 s (speaker is a raid member on voice). 34 to 47 s frames show **no green boxes**; the first boxes appear about 52 to 53 s, 7 to 8 s after the callout.
- Cast bars and alerts in 35 to 70 s: Megalith nameplate cast "Superquake" with the banner "The ground shudders beneath Megalith!" at 50 s (also in chat as `[Datachron]`); target cast bar "Frenzy" (about 1.5 s); player bars "Gather Focus", "Runic Healing", "Sustain". **No "Rockfall" cast bar or chat line was seen.** Chat in that window: "Megalith wearies of your presence", "Mnemesis: Your frailty has been reverberated", "Mnemesis: You will not survive my algorithmic connections". Boxes appear at about 52 to 53 s while the Superquake banner is still up, so Superquake cannot be excluded as the cast that coincides with them; the boxes remain unnamed.
- Wall of cubes, about 215 to 232 s (about 17 s on screen): a row of at least 5 to 6 contiguous, axis-aligned, bright green translucent cubes, camera behind and below the player, boss and raid beyond the wall. Chat before it (frame at 208 s, 21:46 to 21:47): "Megalith: You seek to defy one who has raised mountains!", "Megalith: Speed and stone create a deadly dynamic.", "[Datachron] Logic creates powerful data caches!", "Mnemesis: Deploying mathematical obstacles." No cast bar names them.
- Count per call: first call (about 52 to 56 s) at least 3 to 4 boxes at once (partly off screen); second (about 215 to 232 s) at least 5 to 6, cropped by the frame edge. A per-call count of Rockfall rows is therefore **not obtainable**, and since no Rockfall cast is identified, none would be attributable.
- 7037 (old 5613) additionally: the boxes come with random orientations at 182 s (7 or more); the same chat second has both a Megalith and a Mnemesis yell, so the earlier record's attribution of the boxes to Megalith's "falling rocks" call is **not exclusive**: a Mnemesis line ("multiply ... your weakness") is at the same timestamp.

## Table values re-read (for comparison, not a match)
- Rockfall 49916 / 50305 / 50306 (shape 6): eight rows, one per 1 s (50305) or 1.5 s (49916) window, Param00 0.5 to 2.5, Param01 15, Param02 3 to 8, scattered offsets (x -8 to 12, z -8 to 14), flags 0x145000; 14743 to 14814 add three rotated copies (45, -45, 90) per step. At any instant one row (or four with the rotated copies) is active, not 5 to 6 contiguous boxes for 17 s.
- Logic family with cube/obstacle names (not shape 6): **Cube Smash 48220, shape 2, Param 12/12/12, Y offset -4, rotation 45, ramp-in 1500, flags 0x3000000**; Logic Smash 45032 (shape 4 bars); Freeze Logic Walls 52530 (shape 4, 0/10/90); Destroy Obstacles 52870 (circle r 6.5); Logic Grenade 52877 (circle r 10). Which of these (if any) is Mnemesis's "mathematical obstacles" is **not established**; the spell to boss link is not in the tables and no cast name was seen.
- Body Slam: 47359 (shape 4, 5/40/30), 48035 (shape 4, 0/30/20), 72662 and 75717 (shape 4 row 25/150/30 at z offset -25, plus a circle r 8), and **54622 shape 6, 5/30/5, 0 to 2000 ms, then circle r 5 for 2000 to 2550 ms**.
- Other shape-6 spells (full list of 65 names: Corruption Spread, Booming Ink, Test Ellipsoid Telegraphs, Blaze, Rockfall x3, Exanite Shards (one row), Overcharged 53421/74785 5/10/5 at Y -2.5, Rock Strike 4/20/4, Body Slam 54622, Burrowing Ambush, Dust Devil, Inversion Field 15/30/15, Bone Clamp, Platform/Holoring Auras, Critical Malice 10/3/10, Lazer Spark 20/0.35/1.25, Barrel Roll, Power Plunge, Divekick Telegraph, Bounce, Tormenting Twister, Girrok Trap, Fire Burn, Thunderstorm, Touch of the Maw, Power Rush, Burrowed Pursuit, Fire Vent, Hurtling Junk 2.5/10/2.5, Snarefrost, Carrot Crush, Air Vent, Jetpack x4, Electro-Drill x9, Potent Echo, Mature Primal Parasite). None was named on screen in any clip reviewed.
- Flag observation (unexplained, not a finding): Cube Smash (0x3000000) and Body Slam 54622 (0x3000020) share bits 0x1000000 and 0x2000000, Rock Strike has 0x1000000 (0xb05000), Rockfall rows (0x145000) have neither. The meaning of those bits was not read here.

## 7310 and 7317: Ohmna Body Slam
- 7310 frames 692 to 695 s: target cast bar "Body Slam" counting 2.2 s (693 s) to 0.2 s (695 s); consistent with a cast of about 3 s but the total was not shown. The telegraph is a wide red wedge from the boss (boss body mostly out of frame at the top), the camera is behind the group at a low to moderate pitch, the arena is a purple void floor. A wide wedge matches the shape-4 rows 72662/75717 qualitatively; it does not look like a Param 5/30/5 box. No shape-0 circle of known radius is in a frame with a named shape-6 telegraph; no second phase circle was identified.
- 7317 at 354 to 370 s: platform seen from the side from a low camera; the cast bar reads "Body Slam" on the target frame, no top-down view.
- So the Body Slam 54622 shape-6 row (5/30/5 then circle r 5) is **still not seen**; the observed Body Slam telegraph is a wedge, in line with the earlier record's reading that the retail Ohmna cast uses cone rows.

## Why nothing was measured
1. Spell identity: no cast bar, combat log or addon alert names any shape-6 spell during a box or volume telegraph. 7201 names Superquake (not a table shape-6 spell) and Body Slam on the cone.
2. Camera: 7201 is a third-person view at about 30 to 40 degrees pitch (frames 53 and 220 s), strongly perspective, FOV unknown; 7037 similar. The two near top-down clips (7188, 2731) show no boxes in the sampled frames.
3. Reference: no shape-0 circle of known radius in the same frame as a box; no boss of known radius.
4. A deliberately rough bound, only to test the Rockfall reading and **not a measurement**: in the 220 s frame of 7201 a cube's front-base edge is about 240 to 270 px wide and the cube is roughly as tall as wide (height edge about 180 to 205 px, tilted view), against a player model about 80 px tall at about the same depth or nearer. With player height about 1.8 to 2 m assumed (race unknown) and the cube behind the player, edge length would be at least about 5 to 6 m, plausibly 6 to 10 m; the error bars are larger than a factor of 1.5 because of depth, FOV, pitch and the unknown race. This is compatible with a 12 m Cube Smash only loosely and says only that the cubes are not 0.5 to 2.5 wide by 15 tall: the aspect ratio of the visible cubes is about 1:1, a Rockfall row would be 15 units tall over a 0.5 to 2.5 by 3 to 8 footprint.

## Axis meaning
**No axis-meaning statement is supported by video.** The Correlated hypothesis from the earlier record (Param01 as the vertical extent, Param00/Param02 horizontal) is unchanged and untested. The 7201 cubes are 1:1:1 in appearance and, if they are a Logic-family cube, may be a shape-2 row (Cube Smash 12/12/12) rather than shape 6.

## Recommendation
**Do not implement shape 6 hit-testing or drawing from this evidence.** No named, measurable shape-6 clip exists in the new videos. If a shape-6 branch must exist, keep it inert (log, no damage). The new, more useful lead is to stop treating the Megalith green boxes as Rockfall: the footage ties them to Mnemesis lines as plausibly as to Megalith's, shows a 17 s contiguous cube wall that cannot come from 1 s Rockfall windows, and the table has a cube-named shape-2 row (Cube Smash 12/12/12) in the Logic family. A native read of the shape-5/6/2 draw builder (does shape 2 draw a vertical box? what do Param01 and the Y offset do?) would test that without further video.

## What would close it
1. A clip whose cast bar names Rockfall, Body Slam 54622, Dust Devil, Thunderstorm, Tormenting Twister, Carrot Crush, Fire Vent, Jetpack, Rock Strike, Inversion Field or Critical Malice during a drawn telegraph, camera at 60 degrees or steeper, with a flat circle of known radius in the same frame.
2. For the Megalith boxes: any Megalith/Mnemesis clip with the casting unit's nameplate bar legible on the frame where a box first appears (7201 at about 52 s and 215 s, 7037 at about 181 s are the exact moments; the nameplate there was too small or hidden). The not-viewed clips 7167, 2750, 2264 are the cheapest next candidates (2264 and 7167 have no subtitles; sample at 1 s around Mnemesis yells).
3. A native decode of how shape 2 and shape 6 are drawn, which would settle box versus ellipsoid without footage.

## Confidence
| Claim | Tier |
|---|---|
| 7201 shows green translucent boxes at about 52 to 56 s (3 to 4 or more) and a wall of 5 to 6 or more cubes at about 215 to 232 s, no cast name | Observed |
| 7201 shows no boxes at 34 to 47 s although "rockfall" is said at 35 and 45 s | Observed |
| Megalith's Superquake cast and Mnemesis yells coincide with the box moments; no Rockfall name seen | Observed |
| The boxes are Rockfall shape-6 rows | Not supported (contradicted by duration, contiguity and aspect; not excluded for 7037) |
| The boxes are Mnemesis obstacles / Cube Smash (shape 2) | Candidate only |
| Ohmna Body Slam telegraph in 7310 is a wide red wedge, cast bar "Body Slam" | Observed |
| Any shape-6 area, axis meaning, ellipsoid vs box | Not measured |
| Cube edge in 7201 at 220 s at least about 5 to 6 m, probably 6 to 10 m | Rough estimate, not a measurement |

## Lead review (2026-11-09)
Not re-viewed by the lead except frame 45 s of corpus video 7201 (2014-11-12, Megalith/Mnemesis): green translucent boxes at the top edge of the frame, no spell name on screen, strongly tilted third-person camera. The record's conclusion (no measurable shape-6 clip in the new videos,
do not implement shape 6 from video) matches that frame. The `Cube Smash` 48220 shape-2 candidate is a table lead only. A native read of how the client draws shapes 2 and 6 would test it without more footage and was not run.
