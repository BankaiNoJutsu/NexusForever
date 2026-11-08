Agent-written harvest; the lead has not re-viewed it. Written by a research subagent on 2026-11-08; the frames named below were viewed by the agent, nothing was re-checked by the lead.

# Third OCR pass: time limits of the time-tiered challenges (2026-11-08)

Follows [CHALLENGE_TIME_TIERED_OCR_PASS2_20261108.md](CHALLENGE_TIME_TIERED_OCR_PASS2_20261108.md). Evidence tier: **Observed** (dated retail footage, every video below uploaded before 2018-11-28; no NexusForever or emulator footage). No code change, no parity claim. Raw OCR and cluster tables: `I:\tools\video_index\ocr2` and `I:\tools\ocr3` (not committed); helper scripts `scripts/ocr3_*.py` (reuse `ocr2_run.py`, `ocr2_analyze.py`, `ocr2_sheet.py`, `ocr_server.ps1`).

## Result in one paragraph
No new challenge limit was found. The remaining 413 set-3 subtitle-cue windows (2011-2013 and 2015-2018) were read completely and contained no flagged challenge with a visible countdown. What changed is the quality of the five single-video rows: dense (0.5 s) frames around the first appearance of the widget show that in all five the widget fades in with the value `limit minus 1` (the same rule seen in every clip of pass 2 with a visible start), so Grounding the Storm 120 s, Windspire Ascent 45 s, Entrance Exam 600 s and Rock Climber 30 s are upgraded from lower bounds to probable values, and Race to the Unknown is corrected: it is **not** 120 s but about 112 s, because the clip shows the activation.

## Rule used
A reading is exact only when the start is visible: the widget fades in at limit minus 1 to 2 s (seven clips in pass 2; Thayd Sprint 1:58, Ravenok Racer 0:58, Bingberries 5:58-5:59, Entrance Exam, Hard Way 19:58-19:59, Top of the World 78-79). Where one video shows a fade-in at value V and no second clip exists, the limit is reported as probable (V+1). Nothing in this pass is an exact reading.

## New and corrected readings (pass 3, frames viewed)
| Id | Challenge | Limit | Video (id, date, YouTube id) | Frames |
|---|---|---|---|---|
| 743 | Grounding the Storm | **120 s probable** (was "about 120") | 1887, 2014-06-22, 2ApdFoFpGoo | widget (bottom centre) absent at 31 s, `01:59` at 32 s with the tutorial text box appearing, `01:58` at 33 s, `01:57` at 33.5 s |
| 67 | Windspire Ascent | **45 s probable** (was "about 45") | 4863, 2014-10-04, pTfaRRfZWcc | absent at 947.7 s, faint at 947.9 s, `00:44` at 948.0-948.4 s (name on the widget), `00:43` at 949 s |
| 923 | Entrance Exam | **600 s probable** (was ">= 598") | 6474, 2014-06-18, kqEpcSwGunc | absent at 14.0 s, `09:59` at 14.5 s, `09:58` at 15 s, `09:57` at 16 s; widget list `00 / 9:00 / 9:30` |
| 609 | Rock Climber | **30 s probable** (was ">= 28") | 6015, 2014-06-22, l_WvtM682VY | absent at 124.0 s, `00:29` at 124.5-125 s, `00:28` at 125.5-126 s; list `00 / :07 / :15` |
| 680 | Race to the Unknown | **about 112 s probable (111 to 113); not 120** (was ">= 111, probably 120") | 6793, 2014-06-27, YISTZ4z37l4 | the video opens on the Codex with the challenge selected; the cursor clicks Start at about 31 s while the Codex reads "You are not in the challenge area" and the distance marker counts 14 m, 10 m, 6 m, 3 m, 1 m (30 s to 33 s); the Codex is closed at 34 s; the widget is absent at 35.0 s and fades in at 35.5 s with `01:51`; `01:50` at 37 s, `01:27` at 60 s, "Race to the Unknown Complete!" at 74 s. The widget therefore appears at the activation (arrival at the start marker), and its first value is 1:51, so the limit is about 112 s (the pass-2 note that the challenge may have started before the clip is withdrawn). Single clip, odd number: treat as probable until a second clip exists. The table's gold goal `:56` is half of 112 (Thayd Sprint's silver goal 60 is half of its 120), which weakly supports it |

Murgh Along (500, goals 0/30/60) appears in the 2017-10-09 clip 3745 (WYQOSJyxiSw, "Four Idiots Play Wildstar Episode 10 Highlights"): final-era tracker "(0:41) Murgh Along" and a silver counter 0:11 at 75.5 s (41 - 30), 0:37 / 0:07 at 79.5 s, which re-confirms `counter = main timer - tier goal` a third time, but the clip is a montage that cuts into the run, so it gives only limit >= 41 s (a gold goal of 60 s implies a limit above 60 s by design; not observed).

## What was read
1. **Rest of set 3 (the sweep pass 2 stopped).** `ocr3_jobs_rest.py` listed the 413 windows of `jobs3.json` without a `.done` file (226 videos: 2011 15, 2012 3, 2013 174, 2015 78, 2016 75, 2017 51, 2018 17 windows; 12.2 h of footage at a 4 s step, whole frame at 1.5x plus a 3x top-right crop). All 413 ran (`ocr2_run.py`, 4 workers, about 20 minutes). `ocr2_analyze.py --min-frames 3` found 119 clusters in 52 videos; every cluster was static (desktop clock, chat time, quest and kill timers) or an unflagged challenge. Clusters carrying a flagged name were opened: 4899 Cellar Dwellers (2016-01-05; frames show no widget at all, the clusters were chat/clock text), 3745 Murgh Along (above), 6814 and 1907 (Gallow Fields / Swiftpaw Slayer, an unflagged kill challenge in a 2013 German video: `Swiftpaw Slayer 02:57` countdown, not a flagged row), 4819 (kill timer 0:30, no name). Nothing new for a flagged row.
2. **Name search.** `ocr3_find_names.py` searched title, description and subtitle cues for every still-unread name and variant (Treeclimber, Windrunner, Stairway to Heaven, Powering Down, Windwalker, To the Roof, Race to the Top, plus 40 other flagged names). The hits are all false positives (a puppet-show script about "General Verisa Windrunner", housing tours naming a plot, a podcast wish for a "stairway to heaven", a Windwalker NPC message, the FABkit video "Magma Flow" whose description says it is obtained from the "Powering Down" challenge and which shows no timer). No retail clip in the library names Treeclimber, Windrunner, Stairway to Heaven, Powering Down, Windwalker, To the Roof or Race to the Top on a widget. `ocr3_grep.py` (fuzzy two-word match over all OCR frames) added no new name on a widget either.
3. **Title-keyword candidates.** `ocr3_titles.py` listed 40 dated videos outside the 160-video set and unread whole whose titles contain race, sprint, climb, dash, jump, parkour, hoverboard and similar; eleven short ones (3 Speed Racer achievement, 1765, 2050, 1729 Cache: Wind Scattered, 2376, 2420, 6587, 5531, 1541, 2384, 6766 Parkour Thayd) were read whole at 2 s; no cluster of any kind appeared.
4. **Cross-checks of the probable rows** with 0.5 s crop sheets (`ocr2_sheet.py`): table above. No second clip exists for any of them (the name search finds exactly one video per name).

## Per-challenge table (all 80 flagged ids; goals are the Challenge tier goals in seconds)
Limit labels: EXACT = start visible in pass-2 footage (not re-viewed in pass 3); PROBABLE = widget fade-in seen once, limit = first value + 1; unknown = no retail footage with a readable widget.

| Id | Challenge | Tier goals (s) | Limit | Videos (id, upload date, YouTube id) | What was read |
|---|---|---|---|---|---|
| 22 | Treeclimber | 0 | unknown | none found | no retail clip with a readable widget in the library |
| 47 | Supply Run | 0/90/120 | unknown | none found | no retail clip with a readable widget in the library |
| 67 | Windspire Ascent | 1/11/23 | 45 s PROBABLE | 4863 2014-10-04 pTfaRRfZWcc | pass 3: widget absent at 947.7 s, faint 00:44 at 947.9 s, 00:44 at 948-948.4 s, 00:43 at 949 s (title Windspire Ascent on the widget) |
| 116 | The Grid | 1 | unknown | none found | no retail clip with a readable widget in the library |
| 118 | Eldan Wall Scaler | 0/30/40 | unknown | none found | no retail clip with a readable widget in the library |
| 119 | Eldan Construct Border Sca | 0/30/40 | unknown | none found | no retail clip with a readable widget in the library |
| 166 | Windrunner | 0/15/30 | unknown | none found | no retail clip with a readable widget in the library |
| 184 | Scaling Mount Tempest | 1/30/75 | unknown | none found | no retail clip with a readable widget in the library |
| 194 | Race to the XAS Dig Site | 0/180/225 | unknown | none found | no retail clip with a readable widget in the library |
| 204 | VISTA: Gallow Fields | 0/240 | unknown | none found | no retail clip with a readable widget in the library |
| 244 | (no text) | 10 | unknown | none found | no retail clip with a readable widget in the library |
| 245 | (no text) | 10/20/30 | unknown | none found | no retail clip with a readable widget in the library |
| 246 | (no text) | 10 | unknown | none found | no retail clip with a readable widget in the library |
| 247 | (no text) | 10/20/30 | unknown | none found | no retail clip with a readable widget in the library |
| 265 | To the Roof | 0/180/240 | unknown | none found | no retail clip with a readable widget in the library |
| 271 | Cellar Dwellers | 0/60/150 | unknown | none found | no retail clip with a readable widget in the library |
| 273 | Bringin' the Bingberries | 0/100/180 | 360 s EXACT (pass 2, two videos) | 1691 2014-07-06; 6498 2014-05-14 | 5:58 / 5:59 first visible (pass 2) |
| 276 | Surveying Device Says... | 0/45/120 | unknown | none found | no retail clip with a readable widget in the library |
| 294 | Veggie Delight | 0/60/120 | unknown | none found | no retail clip with a readable widget in the library |
| 304 | (no text) | 0/30/60 | unknown | none found | no retail clip with a readable widget in the library |
| 314 | (no text) | 2683995/2683996/2683997 | unknown | none found | no retail clip with a readable widget in the library |
| 325 | Owanee Speedway | 0/30/40 | unknown | none found | no retail clip with a readable widget in the library |
| 329 | Race to the Top | 0/28/33 | unknown | none found | no retail clip with a readable widget in the library |
| 332 | (no text) | 0/100/180 | unknown | none found | no retail clip with a readable widget in the library |
| 391 | Skeech Saboteur | 60/120/200 | unknown | none found | no retail clip with a readable widget in the library |
| 422 | Getting a Better Signal | 0/30/45 | unknown | none found | no retail clip with a readable widget in the library |
| 435 | Stairway to Heaven | 0/60/90 | unknown | none found | no retail clip with a readable widget in the library |
| 456 | Contents Under Pressure | 0/90/135 | unknown | none found | no retail clip with a readable widget in the library |
| 461 | (no text) | 0/30/60 | unknown | none found | no retail clip with a readable widget in the library |
| 470 | Kickin' It | 0 | unknown | none found | no retail clip with a readable widget in the library |
| 477 | Splashin' Dash | 120 | unknown | none found | no retail clip with a readable widget in the library |
| 500 | Murgh Along | 0/30/60 | unknown | none found | no retail clip with a readable widget in the library |
| 506 | VISTA: Hijunga Village | 0/5 | unknown | none found | no retail clip with a readable widget in the library |
| 508 | Can You Dig It? | 0/60 | unknown | none found | no retail clip with a readable widget in the library |
| 511 | Ravenok Racer | 0/10/20 | 60 s EXACT (pass 2, one video) | 6641 2014-04-23 Pb88yMDWcoI | 0:58 first visible at 2 s (pass 2) |
| 515 | Shock to Your Systems | 0/45 | unknown | none found | no retail clip with a readable widget in the library |
| 529 | Swiftfoot Supplies | 0/30 | unknown | none found | no retail clip with a readable widget in the library |
| 546 | Powering Down | 0/30/60 | unknown | none found | no retail clip with a readable widget in the library |
| 559 | One Small Hop | 0/15/20 | unknown | none found | no retail clip with a readable widget in the library |
| 579 | A Spirited Climb | 5/20/35 | unknown | none found | no retail clip with a readable widget in the library |
| 609 | Rock Climber | 0/7/15 | 30 s PROBABLE (>= 29) | 6015 2014-06-22 l_WvtM682VY | pass 3: widget absent at 124.0 s, 00:29 at 124.5-125 s, 00:28 at 125.5-126 s (title Rock Climber) |
| 641 | XT-9 Hazardous Hallway | 1/20/40 | unknown | none found | no retail clip with a readable widget in the library |
| 654 | Jungle Dash | 1/50/80 | unknown | none found | no retail clip with a readable widget in the library |
| 655 | To the High Flame | 1/40/60 | unknown | none found | no retail clip with a readable widget in the library |
| 662 | View From a Hill | 0/60/75 | unknown | none found | no retail clip with a readable widget in the library |
| 666 | Pulp-Gulp Guzzler | 60/90/120 | unknown | none found | no retail clip with a readable widget in the library |
| 671 | Faster and Fearless | 0/20/40 | unknown | none found | no retail clip with a readable widget in the library |
| 680 | Race to the Unknown | 0/45/56 | 112 s PROBABLE (111 to 113; NOT 120) | 6793 2014-06-27 YISTZ4z37l4 | pass 3: Codex Start clicked t=31 s, distance to start 14 m, 10, 6, 3, 1 m at t=30-33 s; widget absent at 35.0 s, 01:51 at 35.5 s, 01:50 at 37 s, 01:27 at 60 s, Complete card at 74 s |
| 686 | How to Save a Life | 0/30/60 | unknown | none found | no retail clip with a readable widget in the library |
| 690 | Peep Chase | 0/30/60 | unknown | none found | no retail clip with a readable widget in the library |
| 691 | Dockside Delivery! | 0/30/45 | unknown | none found | no retail clip with a readable widget in the library |
| 743 | Grounding the Storm | 0 | 120 s PROBABLE (upgraded) | 1887 2014-06-22 2ApdFoFpGoo | pass 3: widget absent at 31 s, 01:59 at 32 s, 01:58 at 33 s, 01:57 at 33.5-34 s; first visible = limit minus 1 |
| 753 | Top of the World | 0 | 80 s (pass 1/2, five videos) | 1886, 1932, 1969, 2471, 2992 | first values 78-79 (pass 2) |
| 776 | Broodrot Barnstorming | 1/45/60 | unknown | none found | no retail clip with a readable widget in the library |
| 817 | Honey Samples | 0/120/240 | unknown | none found | no retail clip with a readable widget in the library |
| 823 | (no text) | 0/30/60 | unknown | none found | no retail clip with a readable widget in the library |
| 839 | Phantasmal Flora Collectio | 0/120/240 | unknown | none found | no retail clip with a readable widget in the library |
| 850 | Mudwalker Ascent | 0/15/25 | unknown | none found | no retail clip with a readable widget in the library |
| 910 | Sandcastle Critique | 0/5/10 | unknown | none found | no retail clip with a readable widget in the library |
| 923 | Entrance Exam | 0/540/570 | 600 s PROBABLE (>= 599) | 6474 2014-06-18 kqEpcSwGunc (FABkit video, real challenge on screen) | pass 3: widget absent at 14.0 s, 09:59 at 14.5 s, 09:58 at 15-15.5 s, 09:57 at 16 s |
| 956 | Back to Basic | 0/18/25 | 65 s EXACT (pass 2) | 1731 2015-01-25; 5343 2014-06-27 | first visible 1:04 (pass 2) |
| 957 | Just Keep Swinging | 0/90/180 | unknown | none found | no retail clip with a readable widget in the library |
| 973 | Red Light Green Light | 0/15/25 | unknown | none found | no retail clip with a readable widget in the library |
| 979 | Doing Things The Hard Way | 0/180/360 | 1200 s EXACT (pass 2) | 1823 2014-06-16; 1828 2014-06-11; 4284 2014-09-29 | 19:59 / 19:58 first visible (pass 2) |
| 981 | Doing Things The Hard Way | 0/180/360 | unknown (same name and goals as 979; no clip separates the two ids) | none attributable | see 979 |
| 996 | (no text) | 0 | unknown | none found | no retail clip with a readable widget in the library |
| 1002 | Thayd Sprint | 1/60/70 | 120 s EXACT (pass 2) | 585 2014-06-05 IdrR35AkrpI; 2464; 3433; 3464; 6764 2018-06-07 | first visible 1:58 / 1:57 (pass 2) |
| 1014 | Windwalker | 1/10/30 | unknown | none found | no retail clip with a readable widget in the library |
| 1035 | Imbuement: A Hop, Skip, an | 0 | unknown | none found | no retail clip with a readable widget in the library |
| 1048 | Whirlwind Racer | 0/20 | unknown | none found | no retail clip with a readable widget in the library |
| 1054 | Imbuement: An Urnest Reque | 0 | unknown | none found | no retail clip with a readable widget in the library |
| 1058 | Imbuement: The Windswept P | 0 | unknown | none found | no retail clip with a readable widget in the library |
| 1078 | Diagnostic Tests | 0/180/265 | unknown | none found | no retail clip with a readable widget in the library |
| 1079 | Resonance Runner | 0/45/90 | unknown | none found | no retail clip with a readable widget in the library |
| 1085 | Windy Climb | 1/37/75 | unknown | none found | no retail clip with a readable widget in the library |
| 1105 | Another Sassy Mess | 0/30/60 | unknown | none found | no retail clip with a readable widget in the library |
| 1106 | Another Scruffy Mess | 0/30/60 | unknown | none found | no retail clip with a readable widget in the library |
| 1116 | Climbing the Pyre | 0 | unknown | none found | no retail clip with a readable widget in the library |
| 1134 | Diabolic Drift Dash | 0/108/138 | unknown | none found | no retail clip with a readable widget in the library |
| 1136 | Squirg Run | 0 | unknown | none found | no retail clip with a readable widget in the library |

Counts: 80 flagged ids; 6 firm (1002, 956, 979, 273, 511 exact; 753 at 80 s from five videos), 5 PROBABLE (743, 67, 923, 609, 680), 69 unknown. Up the Falls 481 (120 s exact, pass 2) is not one of the 80.

## Not covered
- **Set-3 windows:** all 1163 windows are now read (750 in pass 2, 413 in pass 3). They are a 100 s window at a 4 s step around subtitle cues containing "challenge"; a widget visible for less than 8 s, or outside the window, would be missed, and videos without subtitles have no windows.
- **Not read at all:** videos without subtitles and not in the 160-video challenge set (whole-video sweep was done only for 11 short title-keyword candidates; the other 29 candidates, those longer than 900 s and the veteran dungeon, housing and PvP titles were excluded); cue windows in videos excluded as dungeon, raid, adventure, PvP, housing or ship titles (the exclusion list is in `ocr2_jobs3.py`); the 62 housing "Custom-Built Decor" / "Wildman" playthroughs skipped in pass 2.
- **Challenges with no retail footage in the library (69 of the 80):** all rows marked unknown, in particular Treeclimber 22, Windrunner 166, Stairway to Heaven 435, Powering Down 546 (community 3:00 not verified), Windwalker 1014, To the Roof 265, Race to the Top 329, Diabolic Drift Dash 1134 (only the illegible 2179 clip and the unrelated 6593), The Grid 116, Doing Things The Hard Way 981 (no clip separates it from 979), plus every challenge not named in the table of pass 2.
- **Single-clip rows still wanting a second clip:** 743, 67, 923, 609, 680 (Race to the Unknown most of all, because 112 s is an unusual number); 511 Ravenok Racer has one clip with a visible start.
- **Frame-time caveat:** video times from the OCR run and from single-frame extraction differ by up to 2.5 s in some clips; the readings above were taken on extracted frames at 0.5 s steps, but a fade-in can hide the first half second.
- **Time spent:** about 45 minutes; nothing else was started.

Lead review (2026-11-08): not re-viewed. The agent's report was cross-read against the pass 2 record: no new exact limit was found, so `ChallengeTimeLimits` (exact readings only) is unchanged. The four single-clip rows it upgraded to probable (Grounding the Storm 743 about 120 s, Windspire Ascent 67 about 45 s, Entrance Exam 923 about 600 s, Rock Climber 609 about 30 s) agree with pass 2 and remain single-clip readings; Race to the Unknown 680 is now about 112 s (111 to 113, was "at least 111, probably 120") and the pass 2 remark that its challenge might have started before the clip is withdrawn by the agent. A third final-era clip (3745, 2017-10-09, Murgh Along) shows the counter rule `counter = main timer - tier goal` again. Coverage statement: 69 of the 80 flagged challenges have no retail footage in the library; the unread remainder is listed in the agent's "not covered" section. A second exact reading is what would move a probable row into the table.

