# Video witness V4: telegraph shapes 3 and 6 (2026-11-08)

Question (V4 in [VIDEO_WITNESS_GOAL_PROMPT.md](VIDEO_WITNESS_GOAL_PROMPT.md)): can dated retail footage show what telegraph shape 6 (and 3) look like, and measure them against the table parameters?
Result: **partial, Observed tier only.** The footage shows that some telegraphs are rendered as translucent green 3D boxes (a volume, not a flat decal), but no shape-6 or shape-3 hit area could be **measured**, and the spell identity of the boxes is a candidate, not confirmed. No parity claim; no server change is supported by this record.
Method: corpus subtitle/title search, frames and contact sheets via `ffmpeg`, table joins `Spell4Telegraph -> Spell4 (Spell4BaseIdBaseSpell) -> Spell4Base (LocalizedTextIdName) -> en-US`, `TelegraphDamage` columns as in `TelegraphDamageEntry.cs`. Nothing was copied into the repository.

## Retail videos used (all upload_date < 2018-11-28)
| id | file (under I:\Videos) | upload | URL | use |
|---|---|---|---|---|
| 5613 | `_Additional/Elemental Pairs： Megalith and Mnemesis - Wildstar 40 Man Raiding (Wildstar) [ENIGMA] [--v2QiD_otI].mp4` | 2014-11-21 | https://www.youtube.com/watch?v=--v2QiD_otI | green box telegraphs, Megalith (Datascape 40-man), 100 to 200 s |
| 5657 | `_Additional/Wildstar 20 Man Boss Guide： Dreadphage Ohmna Boss Kill - Warrior Tank (Dreadphage Ohmna) [HGhHvrYVNEM].mp4` | 2014-07-19 | https://www.youtube.com/watch?v=HGhHvrYVNEM | "Body Slam 2.8/3.0" cast bar at 560 s; not measurable |
Leads only (not viewed): other Megalith clips 5744 (2014-12-03), 2007 (2016-04-14), 2000 (2016-01-18), 356 (2015-08-19), 5884 (2017-08-14), all dated retail.

## What is visible (Megalith clip 5613)
- 176 to 195 s: at about 181.5 s (0.5 s frame steps) green translucent **boxes** appear around Megalith; by 183 to 186 s up to about 6 are on screen at once, each a cuboid with edges, lighter top face and a dot near the top-face centre, in several different orientations (tilted/rotated relative to each other and to the camera). They persist at least 6 s (still present at 195 s) and some are re-drawn at other places. Frames 184 s and 188 s show the walls running down from a visible top face (a vertical column seen from above), and the footprint of the near boxes looks roughly square (about 1:1), not like a 2 x 7 bar.
- On-screen text at 184 to 188 s: chat line "[NPC Say] Megalith: Beware of falling rocks!" (19:31); at 140 s the system line "The ground shudders beneath Megalith!" with the cast bar "Superquake 0.9/2.0"; at 101 to 130 s cast bar "Fierce Swipe". Flat red circles/arcs (damage telegraphs) are drawn on the floor in the same clips; the green boxes are the only volumes.
- So the boxes coincide with the "falling rocks" call. The table has a Megalith-neighbourhood spell family named `Rockfall` (Spell4 49916, 50305, 50306, ids adjacent to `Fierce Swipe` 50302/50303, cast 750 ms), all of whose telegraph rows are **shape 6**. That makes the boxes a **candidate** for shape 6, not a confirmed one (no cast bar named "Rockfall" was seen; the NPC yell is the only link).
- Ohmna clip 5657: the cast bar "Body Slam 2.8/3.0" (3.0 s cast) matches spells 47359 (cone) or 72662/75717 (cone + circle), **not** the shape-6 `Body Slam` 54622 (cast 2.9 s). The telegraph at 560 s is a red wedge at the left edge of a perspective frame; nothing measurable. It is a weak cone (shape 4) witness only.

## Table rows (TelegraphDamage.tbl, 12,085 rows; Spell4Telegraph links)
- Shape 6: 159 rows, 166 spell links. `Rockfall` 50305 (rows 5853 to 5860): eight rows, each active 1000 ms back to back from 2000 ms, all `Param01 = 15` (height), `Param00` 0.5 to 2.5, `Param02` 3 to 8, per-row XYZ offsets such as (4, 0, 10), (-8, 0, 8), (2, 0, 6), (12, 0, -2), (-8, 0, -8); 49916 is the same eight rows with windows 3000 to 15000 ms in 1500 ms steps; 50306 uses 2333 to 6500 ms in 500 ms steps. Rows 14743 to 14814 add three copies of each step with extra `RotationDegrees` (30, -30 style values), i.e. later-patch rows; the 2014 clip may pre-date them.
- Other shape-6 spells (names only, Param00/01/02): Body Slam 54622 5/30/5 (active 0 to 2000 ms, then a shape-0 circle radius 5 for 2000 to 2550 ms), Dust Devil 10/20/10, Thunderstorm 3/20/3, Tormenting Twister 6/60/6, Carrot Crush 6/30/6, Fire Vent 9/60/9, Jetpack 2/200/2, Blaze 75/2.5/75 (flat), `Test Ellipsoid Telegraphs` 65/6/65, Corruption Spread 10/5/5, 20/10/10, 30/15/15, 60/25/25.
- Statistics (all 159 shape-6 rows): Param00 = Param02 in 43; Param01 > Param00 in 130 and >= 2 x Param00 in 129; Param01 = Param02 in only 13; `RotationDegrees` non-zero in 72. The earlier record's sentence "Param01 equals Param02 in all but a few" ([TELEGRAPH_SHAPES_3_AND_6_20261107.md](TELEGRAPH_SHAPES_3_AND_6_20261107.md)) holds only for the Corruption Spread family and should read "Param01 is usually the large (height) value".
- Shape 3: 104 rows, 133 spell links; Param02 >= 3 x Param00 in 77 (long thin bars: Thunder Cross 10/10/103 and 10/10/34, Tele-Pulse 30/5/200, Molten Torrent 7/30/175, Super Conductor 10/30/300, Tentacular Retaliation 9/8/37.5, Cannon Fire 1/15/26), Param00 = Param02 in 22 (Static Field 14/8/14, Psyblade Barrage 4.5/8/4.5).

## Correlation
- Consistent with the 2026-11-07 reads: shape 6 has a big height-like Param01 (so Param01 as the vertical extent is plausible; the name `Test Ellipsoid Telegraphs` 65/6/65 hints at a three-axis volume), and a Param00 = Param02 footprint in 43 rows is a circular/square footprint.
- **Tension, unresolved:** (a) the visible volumes are boxes, while the client builder groups shapes {5, 6} in one draw class with Pie (a round mesh) and the server reads shape 6 as an ellipse; (b) `Rockfall` parameters (x 0.5 to 2.5, z 3 to 8, height 15) would be 1:3.5 footprints if they are the boxes seen, but the near boxes look square; (c) up to about 6 simultaneous long-lived boxes do not fit eight non-overlapping 1 s windows. Either the boxes are another spell (for example a shape-2 Square row, or the later rotated copies), or shape 6 is drawn as a box with a different scale rule. This record cannot decide between these.
- Shape 3 "cross" reading: no cross-shaped telegraph was seen; the data suggest shape 3 is mostly a long thin bar (width Param00, length Param02), a cross only if the second-pass swap is used. Not tested by footage.

## Confidence
| Claim | Tier |
|---|---|
| Retail renders some telegraphs as translucent green oriented boxes (2014-11-21, Megalith) | Observed |
| Those boxes are the shape-6 `Rockfall` rows | Candidate (spell link by NPC yell and adjacent ids only) |
| Shape-6 hit area size, axis meaning (radius vs full dimension), ellipse vs box | Not established; footage contradicts the "ellipse" picture only if the boxes are shape 6 |
| Measured sizes | None. Perspective frames, camera elevation and FOV unknown; at best the near boxes are several player heights wide (error bars larger than the 2x question the box-extents record asks) |

## Still missing (exact footage)
1. A clip where a shape-6 spell is identified by an on-screen cast bar or combat log line (candidates by name: Dust Devil, Thunderstorm, Tormenting Twister, Carrot Crush, Fire Vent, Body Slam 54622, Rockfall), seen from a high, near-top-down camera, ideally with a known reference: a shape-0 circle of known radius in the same frame (Body Slam 54622 has both: 5/30/5 then circle r 5) or a boss with known melee radius.
2. A second Megalith clip (5744, 2007, 2000, 356, 5884) showing the "Beware of falling rocks!" cast bar name and how many boxes appear per call, to count rows per call and separate 2014 from later rotated copies.
3. For shape 3: a Thunder Cross, Tele-Pulse or Tentacular Retaliation clip with a visible cast name.
4. A sized reference for the box footprint: character at the box edge in an overhead frame (player height about 2 m is only an assumption).
Library limits: subtitle cues name spells rarely (queries tried: "tormenting twister", "rock strike", "carrot crush", "critical malice", "thunder cross", "tele-pulse", "tentacular retaliation" returned 0 retail cue hits; "body slam" 56, "thunderstorm" 18, "dust devil" 3, "rockfall" 5, mostly unrelated speech). Boss to spell links are not in the tables (encounter scripts are server side), so spells must be matched through on-screen cast bars.

Review note (lead, 2026-11-08): written by a research subagent; not re-viewed by the lead. The Rockfall to green-box match is a candidate resting on a boss yell and adjacent spell ids; the statistics on shape-6 parameters were not recomputed by the lead.
