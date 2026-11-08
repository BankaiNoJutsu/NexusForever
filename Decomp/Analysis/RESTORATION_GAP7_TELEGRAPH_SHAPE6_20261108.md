# Restoration gap 7: telegraph shape 6, second video pass (2026-11-08)

Question ([RESTORATION_GAPS_GOAL_PROMPT.md](RESTORATION_GAPS_GOAL_PROMPT.md) section 7): is there a retail clip where a shape-6 spell is named on screen (cast bar or combat log) and seen from a high camera so that its area can be measured against a known reference?
Result: **No. Nothing is measurable and no spell name was confirmed. Do not implement shape 6 from this evidence.** Observed tier only; no parity claim.
Method: corpus `find` (subtitle cues, via the scratch reader [scripts/gap7_find.py](scripts/gap7_find.py)), contact sheets and full-resolution frames through ffmpeg 8.1.1 ([scripts/gap7_sheet.sh](scripts/gap7_sheet.sh), [scripts/gap7_lookup.py](scripts/gap7_lookup.py)), frames read with the image reader. Frames are in the session scratchpad only, nothing was copied into the repository. Follows [VIDEO_WITNESS_TELEGRAPH_20261108.md](VIDEO_WITNESS_TELEGRAPH_20261108.md); table facts below are copied from that record, not recomputed here.
Review note: written by a research subagent; not re-viewed by the lead.

## Clips reviewed (all dated before 2018-11-28, all retail player footage)
| id | upload | URL | res | what was reviewed | outcome |
|---|---|---|---|---|---|
| 5613 | 2014-11-21 | https://www.youtube.com/watch?v=--v2QiD_otI | 1280x720 | Megalith + Mnemesis 40-man, English UI, 172 to 200 s frame by frame, plus cast-bar and chat crops | the only clip with 3D green boxes; no spell name; strong perspective; not measurable (details below) |
| 5744 | 2014-12-03 | https://www.youtube.com/watch?v=IS_duvvJjWA | 1280x720 | whole clip at 12 to 20 s steps, 40 to 60 s at 1 s steps (Spellslinger POV, Megalith/Pyrobane) | no green boxes; the dark blocks are the lava-phase pillars ("A pillar has been destroyed!" alert at about 51 to 57 s), not telegraphs; no usable telegraph |
| 2000 | 2016-01-18 | https://www.youtube.com/watch?v=Cr-oDwE55cc | 854x480, French UI | whole clip at 18 s steps, 40 to 76 s at 2 s steps (Stalker DPS POV, Megalith/Pyrobane) | flat green arcs/chevrons/diamonds near the boss feet (about 40 to 75 s), no spell name legible at 854x480, boss cast bar unreadable |
| 2007 | 2016-04-14 | https://www.youtube.com/watch?v=6LiaYIboyPQ | 1280x720 | whole clip at 18 s steps (healer POV, Megalith/Pyrobane) | big green discs are healing zones; no box telegraph, no cast name; a Norzza/Alura overlay covers the lower left |
| 356 | 2015-08-19 | https://www.youtube.com/watch?v=7_u2yqPRErQ | 1280x720, French UI | whole clip at 23 s steps (tank POV, Ventemort + Megalith) | red circles and strips, cyan player circles; no green box, no readable name |
| 5884 | 2017-08-14 | https://www.youtube.com/watch?v=IfXbkM5BD4A | 1280x720, 60 fps, boss-timer addon | first 9 s at 1 s steps, 150 to 158 s at 1 s steps, whole clip at 14 s steps (Alieron + Megalith) | flat cyan translucent parallelogram on the floor at 5 to 8 s (spell unnamed), red concentric rings at 152 s; the high camera is good but nothing carries a shape-6 name |
| 5657 | 2014-07-19 | https://www.youtube.com/watch?v=HGhHvrYVNEM | 1280x720 | Ohmna, 170 to 190 s and 255 to 265 s, cast-bar crops | "Body Slam" cast bar fills in about 3 s (3.0 s cast, so 47359/72662/75717 rather than shape-6 54622 at 2.9 s, within frame-step error); the telegraph is a thin red strip at the platform edge and a red wedge, the boss is off screen at the top; no scale |

Other body-slam cue hits (GA 20-man raid streams 5782, 5783, 5784, 5788, 5789, 5798, 5805, 5809, 5811, 5812, 5837, uploaded 2015-06 to 2015-11) are long multi-hour raid streams of the same Ohmna Body Slam; they were not frame-searched (see Not done).

## Megalith leads: "Beware of falling rocks" and the green boxes
- 5613: the on-screen alert banner "Beware of falling rocks!" (top centre) and the chat line "[NPC Say] Megalith: Beware of falling rocks!" (19:31) are present at 177.5 s. The only cast bar on Megalith in the window 177 to 182.5 s is "Fierce Swipe" (0.4/0.8 at 177.0 s, then none). So the green boxes are **not** announced by a Megalith cast bar, and no chat or combat-log line names a spell. The call is by NPC yell only, as in the earlier record. The name "Rockfall" is therefore **not confirmed** by any frame.
- Boxes first appear about 181.5 s, about 4 s after the banner. At 182 s at least 7 are in one frame (several on top of Megalith, others at the frame edges), each a cuboid with a lighter top face, in the frame the boxes look roughly cubic (height about equal to the visible footprint width), orientations differ. One box at the right frame edge stays unchanged from about 190 s to at least 198 s. The chat shows a **second** "Beware of falling rocks!" line by 196 s, so at least two calls occur in 172 to 200 s; the first call has at least 7 boxes at once; the count for the second call was not separated.
- In no other Megalith clip (5744, 2000, 2007, 356, 5884; 2014-12 to 2017-08) did the 3D green boxes appear. The 2016 clip 2000 shows flat green chevrons/diamonds, the 2017 clip 5884 flat cyan quads. This could mean a render or colour change between 2014 and 2015-2017 builds, or that those shapes are different spells; footage cannot decide. It is a lead only.

## Why the box size cannot be measured
1. Reference scale: none is known. No shape-0 circle of known radius is in the same frame and at the same depth; Megalith's own radius and the arena tile size are not in any table I read; player height (about 2 m) is only an assumption.
2. Camera: in 5613 the camera is a close, strongly tilted third-person view (horizon and tall structures visible); boxes at the frame edges are close to the lens and the ones on the boss are far. Nothing is near-top-down. FOV and pitch are unknown, so a single ratio to a player model is not valid across depths. An honest bound from the 183 s frame (a box of about 2 to 3 character heights across, at unknown depth) is not usable: error is larger than a factor of 2.
3. Spell identity: no name on screen (above), so even a measured size could not be joined to a Spell4 row.
4. The only clips that are really high-camera (5744, 5884) show no box telegraph or no named spell at the moment of a candidate shape.
5. Resolution: 2000 and 356 are 854x480 or French-UI, too small for names.

I therefore record **no dimension**. The nearest thing to a number is the table's, not the footage's.

## Can Param00/01/02 axis meaning be stated?
Only as a table-derived Correlated hypothesis, not from video:
- Names and values from the earlier record: Thunderstorm 3/20/3, Fire Vent 9/60/9, Carrot Crush 6/30/6, Tormenting Twister 6/60/6, Dust Devil 10/20/10, Body Slam 54622 5/30/5 (then a shape-0 circle r 5), Jetpack 2/200/2, Rockfall x 0.5 to 2.5, y 15, z 3 to 8; opposite extreme: Blaze 75/2.5/75. A large middle value for things that fall from above or rise (lightning, vents, a dropped object, a jetpack, a twister) and a small middle value for the flat Blaze is what one expects if **Param01 is the vertical (Y) extent and Param00/Param02 are the horizontal X/Z extents**. This agrees with the existing note that shape 6 is a three-axis volume and with `Test Ellipsoid Telegraphs` 65/6/65. Tier: Correlated (names plus value pattern), not Observed, not Mapped.
- Not established: whether each Param is a radius/semi-axis or a full width; whether the draw is an ellipsoid or a box; the interaction of `RotationDegrees` (72 of 159 shape-6 rows non-zero); how the visible cubes (height about equal to width) relate to Rockfall rows of height 15 versus footprint 0.5 to 8. The cubes do not look like 15-unit columns, so either they are a different spell, or the height is clamped, or a box shape uses another rule. The footage does not settle it.

## Recommendation
**Do not implement shape 6 hit-testing/drawing now.** Reasons: nothing measured, no name confirmed, axis meaning for radius versus full extent unknown, and the visible cubes contradict the naive reading of the one candidate spell family. If the server must keep a shape-6 path for completeness, keep it inert (log, do not damage) rather than guess; a later patch should be driven by a native read of the shape-6 branch (the builder with shape 5, the client's collision test) plus one of the clips below.

## What would close it (exact footage still missing)
1. A clip whose cast bar or damage log names a shape-6 spell (Dust Devil, Thunderstorm, Tormenting Twister, Carrot Crush, Fire Vent, Body Slam 54622, Jetpack, Rockfall) with the camera at 60 degrees or steeper pitch, at 720p or more, and a flat telegraph of known size (shape-0 circle of known radius at the same spot) in the same frame; Body Slam 54622 is the best case because its second phase is a shape-0 circle r 5.
2. A Megalith clip in English at 720p or more, 2014 build, with the second call framed from above, to count boxes per call and to see whether the damage line reads "Rockfall".
Library limit: subtitle cues rarely name spells (queries this pass: "falling rocks" 12 cues, none in a Megalith fight; "rockfall" 5; "dust devil" 3; "thunderstorm" 18; "fire vent", "tormenting", "carrot", "jetpack" returned only unrelated speech such as housing tours or the carrot currency; "body slam" Ohmna only). Boss to spell links are not in the tables, so identification depends on a legible on-screen name.

## Not done
- The GA 20-man raid streams (5782 to 5837) were not frame-searched for Body Slam; at 2015 resolution and with the Ohmna platform seen at a low angle they are unlikely to give a measurable shape, but a targeted pass at the cue times (5798 at 2508 s, 5809 at 2184 s, 5811 at 2120 s, 5812 at 895 s, 5837 at 572 s) is the cheapest remaining try for a Body Slam telegraph with the platform as a reference. Whether Ohmna's Body Slam is the shape-6 row 54622 is itself unverified (the cast-bar timing favours the cone rows).
- No Ghidra read, no table recomputation, no Source change, no build.

## Confidence
| Claim | Tier |
|---|---|
| 5613 shows 3D green boxes (about 7 at once) after "Beware of falling rocks!", no Megalith cast bar in that window | Observed |
| The boxes are the shape-6 `Rockfall` rows | Not confirmed (candidate only) |
| Any shape-6 area or radius | Not measured |
| Param01 is the vertical extent of shape 6 | Correlated hypothesis from table names, not video |
| Retail 2015 to 2017 builds draw other Megalith telegraphs flat (green chevrons, cyan quads) | Observed, spell identity unknown |

## Lead review (2026-11-08)
Not re-viewed: the clip readings are the agent's. The record's conclusion (nothing measurable, no name on screen, do not implement shape 6) is consistent with its own evidence and the earlier witness record. No server change.
The cheapest remaining try is the targeted Body Slam pass at the listed cue times in the Ohmna streams; it was not run.
