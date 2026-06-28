# Video Evidence Map

Created: 2026-06-19

This map records reusable local and online WildStar video evidence that can
correlate current content-retail blockers. Video evidence is external
corroboration unless paired with local emulator/client smoke, source/table
evidence, and focused tests.

Generated CSVs remain the authority for queue order, row state, and
`retail_claim_allowed`.

## Local Inventory Groups

Current local video inventory under `I:\Videos` includes these useful coverage
groups:

| Coverage group | Local examples | Likely tracker use |
| --- | --- | --- |
| Crimson Isle Dominion tutorial | `Wildstar Tutorial - Crimson Isle - Quests Part 1 (Dominion)`, `Wildstar Tutorial - Crimson Isle - Quests Part 2 (Dominion)`, path/challenge/achievement companion videos | Q5597/Q5573/Q5575/Q5580/Q5604 quest, objective UI, path/challenge, and achievement smoke blockers |
| Northern Wilds Exile tutorial | `Wildstar Tutorial - Northern Wilds - Quests (Exile)` plus path/challenge/achievement companion videos | Q3479/Q3480/Q3486/Q3668/Q3673/Q3797/Q3886/Q3963 quest and path blockers |
| Everstar Grove / Levian Bay tutorials | tutorial quest, path, challenge, lore, and achievement videos | starter-zone inventory and future queue rows outside the current next-slice queue |
| Algoroc walkthrough series | `Wildstar - Algoroc Part 1` through later parts | zone quest/world-spawn corroboration when Algoroc rows enter the queue |
| Instance walkthroughs | `Protogames Academy`, `Fragment Zero`, `Ruins of Kel Voreth`, `Stormtalon's Lair`, `Sanctuary of the Swordmaiden`, `Riot In The Void` walkthrough videos | split-required instance blocker discovery only; implementation still needs bounded phase/objective evidence |
| General systems / tutorial commentary | early-game improvement, new tutorial, impressions, news, and retrospective videos | low-confidence context only unless full UI-visible gameplay is present |

## Northern Wilds Path Missions

Scope:

- Path episodes: Soldier `8` (`Securing the Crash Site`), Explorer `9`
  (`Mapping Northern Wilds`), Scientist `28` (`Cold Science`), Settler `82`
  (`Emergency Aid`).
- These videos are external retail/tutorial corroboration. They support client
  path routing and visible objective behavior, but generated CSVs and local
  emulator/client smoke remain the authority for row state and
  `retail_claim_allowed`.

Local videos:

| Video | Local path | Duration | Online mirror | Notes |
| --- | --- | ---: | --- | --- |
| Wildstar Tutorial - Northern Wilds - Soldier Missions (Exile) | `I:/Videos/Wildstar Tutorial - Northern Wilds - Soldier Missions (Exile)/Wildstar Tutorial - Northern Wilds - Soldier Missions (Exile) (1080p_60fps_H264-128kbit_AAC).mp4` | 00:04:53.454 | `https://www.youtube.com/watch?v=EGsDwWfeeeU` | Description anchors: 00:36 `HOLDOUT: Conquer the Yeti`; 01:49 `DEFEND: Conquer the Coldburrow Skeech`; 02:46 `HOLDOUT: Conquer the Dominion`. |
| Wildstar Tutorial - Northern Wilds - Explorer Missions (Exile) | `I:/Videos/Wildstar Tutorial - Northern Wilds - Explorer Missions (Exile)/Wildstar Tutorial - Northern Wilds - Explorer Missions (Exile) (1080p_60fps_H264-128kbit_AAC).mp4` | 00:04:31.767 | `https://www.youtube.com/watch?v=Hf4N45QZbp8` | Description anchors: `VISTA: Signal for Help` path start 00:28 / beacon point 01:07; `VISTA: Direct Survivors to Camp` path start 01:42 / beacon point 02:10; `SURVEILLANCE: Sniper Support` path start 02:38 / beacon point 03:20; 03:52 `CARTOGRAPHY: Northern Wilds Survey`. |
| Wildstar Tutorial - Northern Wilds - Scientist Missions (Exile) | `I:/Videos/Wildstar Tutorial - Northern Wilds - Scientist Missions (Exile)/Wildstar Tutorial - Northern Wilds - Scientist Missions (Exile) (1080p_60fps_H264-128kbit_AAC).mp4` | 00:04:48.160 | `https://www.youtube.com/watch?v=S9BDXA90TLY` | Description anchors: 00:30 `CHEMISTRY: Vitalium Crystals`; 02:56 `BIOLOGY: Skeech Physiology`; 03:43 `ANALYSIS: Turning the Tide`. |
| Wildstar Tutorial - Northern Wilds - Settler Missions (Exile) | `I:/Videos/Wildstar Tutorial - Northern Wilds - Settler Missions (Exile)/Wildstar Tutorial - Northern Wilds - Settler Missions (Exile) (1080p_60fps_H264-128kbit_AAC).mp4` | 00:03:45.164 | `https://www.youtube.com/watch?v=-8ex2oZE-SQ` | Description anchors: 00:39 path resource tip; 01:08 Salvaged Machine Parts area; 01:18 `EXPANSION: Settler's Reach`; 02:24 `EXPANSION: Coldburrow`; 03:07 `EXPANSION: Icefury`. |

Evidence observations:

| Timestamp | Video | Visible evidence | Rows/blockers advanced | Disposition |
| --- | --- | --- | --- | --- |
| 00:47-00:55 | Scientist Missions transcript | First Vitalium crystal scan grants `CHEMISTRY: Vitalium Crystals`; Scanbot scans on three crystals complete the mission. | PathMission `160`, PathScientistCreatureInfo `130`, Creature2 `15888`, episode `28`; supports checklist-count scan progress instead of first-scan completion. | `external_video_observed_pending_local_path_client_smoke` |
| 00:55-01:05 | Scientist Missions transcript | Vitalium crystal scans release a temporary healing aura and can continue after the mission quota is met. | PathMission `160`, PathScientistCreatureInfo `130`; post-quota scan effect remains pending local client smoke. | `external_video_observed_pending_local_path_client_smoke` |
| 03:01-03:26 | Scientist Missions transcript | `BIOLOGY: Skeech Physiology` is granted by scanning Skeech or Skeech-related objects; the observed run completed after 15 scans, with target-dependent variance noted. | PathMission `648`, PathScientistCreatureInfo `128`; runtime uses the table-backed checklist count, while exact per-target weighting remains blocked. | `external_video_observed_pending_local_path_client_smoke` |
| 03:51-04:23 | Scientist Missions transcript | Active Dominion turret scan grants `ANALYSIS: Turning the Tide`; the turret minigame reprograms a turret, and three activated turrets complete the mission. | PathMission `42`, PathScientistCreatureInfo `34`; supports checklist-count scan progress instead of first-scan completion. | `external_video_observed_pending_local_path_client_smoke` |
| 00:28-03:52 | Explorer Missions | All four Explorer path mission starts/beacon/survey anchors are listed in the local description. | PathMission `158`, `35`, `36`, `1254`; Explorer path episode `9`. | `external_video_observed_pending_local_path_client_smoke` |
| 00:36 / 01:49 / 02:46 | Soldier Missions transcript | The three Soldier beacons start `HOLDOUT: Conquer the Yeti`, `DEFEND: Conquer the Coldburrow Skeech`, and `HOLDOUT: Conquer the Dominion`; the spoken walkthrough describes 4-wave 3-minute Yeti/Skeech events with 45-second wave spacing, a 1-minute Dominion event with new waves after clears, and defense of three injured survivors for the Skeech mission. | PathMission `156`, `33`, `34`; Soldier path episode `8`; PathSoldierEvent `2`, `12`, `13`; holdout Creature2 `12508`, `11139`, `11141`. Server scripts now emit table-backed holdout status/next-wave/end lifecycle packets, while actual spawn composition, survivor health, fail states, and local client smoke remain blocked. | `external_video_transcript_correlated_table_backed_runtime_packet_lifecycle_pending_spawn_client_smoke` |
| 01:18 / 02:24 / 03:07 | Settler Missions | All three Settler expansion mission starts are listed in the local description, with resource/build context. | PathMission `650`, `652`, `651`; Settler path episode `82`; hub ObjectId `46`, `48`, `47`. | `external_video_observed_pending_local_path_client_smoke` |

Settler transcript/table details for `NWPath-Settler`:

- Resource: `Salvaged Machine Parts` (`Item2` `14400`). The spoken transcript says Northern Wilds has one Settler resource type and that boxes grant one or two parts into the Settler resource bag. Runtime pickup/material-bag behavior still needs local client smoke or decompile-backed implementation.
- `EXPANSION: Settler's Reach`: PathMission `650`, hub `46`, mission count `4`; group `11` Medical Station Health Booster costs `5`, group `12` Shield Charging Booster costs `5`, group `13` Assault Power Infuser costs `5`, group `2618` Digitally Linked Banking Box costs `8`.
- `EXPANSION: Coldburrow`: PathMission `652`, hub `48`, mission count `4`; group `14` Militia Outpost costs `8`, group `15` Travel Speed Booster costs `5`, group `16` Shield Booster costs `5`, group `17` Critical Hit Enhancer costs `5`.
- `EXPANSION: Icefury`: PathMission `651`, hub `47`, mission count `1`; group `10` Exile Mortar Strike Team costs `8`.
- Current runtime boundary: accepted, table-backed Settler build requests consume the configured resource cost and advance the hub mission count; buff station effects, militia/mortar spawns, temporary mortar action, duration persistence, and resource-node pickup remain blocked pending local smoke/decompile evidence.

## Northern Wilds Challenges

Scope:

- Challenge `105` Rootbrute Slayer, challenge `103` Skeech Slayer, and
  challenge `107` Xenobite Egg Smasher.
- The cited tutorial video is external corroboration for the challenge list and
  route. Runtime behavior is grounded in build-16042 challenge, target-group,
  tier, and reward-track tables plus focused tests.
- YouTube advertised an English auto-caption track for this video, but the
  timed transcript endpoints returned no usable text during this pass. The
  notes below therefore use the public video description timestamps, local table
  evidence, and tests rather than treating transcript text as authoritative.

Online video:

| Video | URL | Duration | Notes |
| --- | --- | ---: | --- |
| Wildstar Tutorial - Northern Wilds - Challenges (Exile) | `https://www.youtube.com/watch?v=D1SvcksMc3Y` | 00:02:42 | Uploader/title metadata fetched 2026-06-22; public description anchors: 00:23 Rootbrute Slayer, 01:06 Skeech Slayer, 01:43 Xenobite Egg Smasher. |

Evidence observations:

| Timestamp | Visible/listed evidence | Rows/blockers advanced | Disposition |
| --- | --- | --- | --- |
| 00:23 | Description starts the `Rootbrute Slayer` segment. | Challenge `105`, Combat type, TargetGroup `1852`, tier `154` count `5`, zone restriction `35`, RewardTrack `16`. Runtime test covers auto-activation and first kill credit for Creature2 `12212`. | `runtime_challenge_lifecycle_progress_tier_reward_track_tested_pending_content_smoke` |
| 01:06 | Description starts the `Skeech Slayer` segment. | Challenge `103`, Combat type, nested TargetGroup `8851 -> 2309/8852`, tier `150` count `5`, zone restriction `604`, RewardTrack `16`. Runtime test covers auto-activation and nested group kill credit for Creature2 `11965`. | `runtime_challenge_lifecycle_progress_tier_reward_track_tested_pending_content_smoke` |
| 01:43 | Description starts the `Xenobite Egg Smasher` segment. | Challenge `107`, Combat type, TargetGroup `1854`, Creature2 `12836`, tier `158` count `10`, zone restriction `609`, RewardTrack `16`. Runtime test covers auto-activation and first egg-destruction credit through the combat kill hook. | `runtime_challenge_lifecycle_progress_tier_reward_track_tested_pending_content_smoke` |

Corroborated source/table/test evidence:

- `wildstar_client/Challenge`
- `wildstar_client/ChallengeTier`
- `wildstar_client/TargetGroup`
- `wildstar_client/RewardTrack`
- `wildstar_client/RewardTrackRewards`
- `Tools/DataMapping/output/challenge_map.csv`
- `Tools/DataMapping/output/challenge_creature_map.csv`
- `Decomp/Analysis/coverage/content_retail_completeness_challenge_evidence.csv`
- `Source/NexusForever.Game/Challenges/ChallengeManager.cs`
- `Source/NexusForever.Game/Challenges/ChallengeCombatHooks.cs`
- `Source/NexusForever.Game.Tests/Challenges/ChallengeDataMappingSliceTests.cs`

Remaining local blockers:

- Local build 16042 client smoke for challenge UI activation, progress text,
  reward-track selection/presentation, and completion toast behavior.
- Spawn availability and placement smoke for Rootbrute, Skeech, and Xenobite
  Egg target groups in the relevant Northern Wilds subzones.
- Reward-choice UI and alternate choice persistence for RewardTrack `16`
  rewards at 200 and 300 points.
- Generated challenge evidence rows remain `not_retail_complete` until local
  smoke and validator-backed tracker outputs allow promotion.

## Q5597 - Dregs and Thieves

Queue slice:

- Queue rank: 1
- Lane: quest
- Content id: `5597`
- Actionability: `quest_validation_ready`
- Current claim boundary: external video corroboration plus emulator/source/table
  tests, still `not_retail_complete` and `retail_claim_allowed=false`.

Local videos:

| Video | Local path | Duration | Online mirror | Notes |
| --- | --- | ---: | --- | --- |
| Wildstar Tutorial - Crimson Isle - Quests Part 1 (Dominion) | `I:/Videos/Wildstar Tutorial - Crimson Isle - Quests Part 1 (Dominion)/Wildstar Tutorial - Crimson Isle - Quests Part 1 (Dominion) (1080p_60fps_H264-128kbit_AAC).mp4` | 00:09:52.434 | `https://www.youtube.com/watch?v=uu4avnomefU` | Local metadata title/artist/date: CutenessDoom, 2016. Web search still finds the YouTube title on 2026-06-19. |
| Wildstar Tutorial - Crimson Isle - Quests Part 2 (Dominion) | `I:/Videos/Wildstar Tutorial - Crimson Isle - Quests Part 2 (Dominion)/Wildstar Tutorial - Crimson Isle - Quests Part 2 (Dominion) (1080p_30fps_H264-128kbit_AAC).mp4` | 00:12:36.274 | `https://www.youtube.com/watch?v=YA0Gyn870aI` | Local metadata title/artist/date: CutenessDoom, 2016. Web search finds the title through the CutenessDoom Wildstar Tutorials playlist on 2026-06-19. |

Evidence observations:

| Timestamp | Video | Visible evidence | Rows/blockers advanced | Disposition |
| --- | --- | --- | --- | --- |
| 00:06:52 | Part 1 | Mondo Zax offers `Dregs and Thieves`; UI text references recovering explosives; chat/log area shows `Quest Completed: Ordnance Recovery`. | Q5597 prerequisite rows for level/faction/Q5596 accept path; Mondo starter relation; receiver/dialog path; episode chain Q5596 -> Q5597. | `external_video_observed_pending_local_dialog_smoke` |
| 00:00:35 | Part 2 | Chua Explosives object has `Collect` prompt; objective tracker shows `Dregs and Thieves` and `50% Retrieve Chua Explosives`; UI shows Bloodstone Canyon/Crimson Isle context. | Q5597 objective 8256, creature relation 1090, TargetGroup 4373, indicator rows 17896/17898/17897/17899, world-zone row 622. | `external_video_observed_pending_local_client_ui_smoke` |
| 00:00:50 | Part 2 | Chat/log shows `Quest Completed: Dregs and Thieves`; Mondo Zax immediately offers the follow-up `Tactical Demolitions`; reward dialog is visible. | Q5597 finisher relation 1564, reward rows 3000/5236, achievement checklist 5495/achievement 4134, Q5597 -> Q5604 handoff. | `external_video_reward_observed_pending_local_client_smoke` |

Corroborated source/table/test evidence:

- `wildstar_client_mysql/Quest2.tbl.sql`
- `wildstar_client_mysql/QuestObjective.tbl.sql`
- `wildstar_client_mysql/TargetGroup.tbl.sql`
- `wildstar_client_mysql/Creature2.tbl.sql`
- `wildstar_client_mysql/WorldLocation2.tbl.sql`
- `Tools/DataMapping/output/quest_objective_map.csv`
- `Tools/DataMapping/output/creature_quest_map.csv`
- `Tools/DataMapping/output/creature_spawn_map.csv`
- `Source/NexusForever.Script.Main/Quests/CrimsonIsle/CrimsonIsleMapScript.cs`
- `Source/NexusForever.Script.Main/Quests/CrimsonIsle/Q5597.cs`
- `Source/NexusForever.Game.Tests/Quests/CrimsonIsleBranchInteractionTests.cs`
- `Source/NexusForever.Game.Tests/Quests/CrimsonIsleQuestChainTests.cs`
- `Source/NexusForever.Game.Tests/Quest/QuestTests.cs`

Remaining local blockers:

- Local build 16042 route/map guidance for Q5597 and indicator locations
  `17896`, `17898`, `17897`, and `17899`.
- Local activation packet timing and Chua Explosives despawn/respawn visual
  state.
- Duplicate and negative activation/completion cases through the local client.
- Reward inventory persistence and achievement UI/toast/progression smoke.
- End-to-end local Q5596 -> Q5597 -> Q5604 smoke bundle.

Capture command when services/client are not already being launched:

```powershell
.\Decomp\Analysis\Start-BlockerEvidenceHarness.ps1 `
  -BundleName "quest-5597-dregs-and-thieves-local-client-smoke" `
  -ClientDirectory "I:\WildStar" `
  -WorldIds 870 `
  -ObjectiveIds 8256 `
  -NegativeCases "missing Q5596 prerequisite", "wrong faction", "low level", "duplicate Chua Explosives activation", "completion without visible Mondo" `
  -PromptForRootPassword
```

Use `-CreateBundleOnly -SkipServerLaunch -SkipClientLaunch` when the stack is
already running and only a worksheet is needed.

## Q3479 - From the Wreckage

Queue slice:

- Queue rank: 2
- Lane: quest
- Content id: `3479`
- Actionability: `quest_validation_ready`
- Current claim boundary: external retail video corroborates the visible quest
  flow, but Q3479 remains `not_retail_complete` and
  `retail_claim_allowed=false`.

Online video:

| Video | URL | Duration | Notes |
| --- | --- | ---: | --- |
| WildStar - Exiled Aurin Esper Gameplay 4 - Northern Wilds - Arrival | `https://www.youtube.com/watch?v=iMSWhvau5mI` | 00:11:38 | Uploader: AyinMaiden. Metadata/description fetched 2026-06-19 with `yt-dlp`; description lists `From the Wreckage` at 00:01:38, `Body in the Snow` at 00:05:57, `Wreckage Refuge` at 00:07:39. |

Evidence observations:

| Timestamp | Visible evidence | Rows/blockers advanced | Disposition |
| --- | --- | --- | --- |
| 00:01:35 | Bosun Redmark is visible at the crash site with a quest marker and dialog after `Welcome to Northern Wilds`; task text points to Bosun. | Q3479 starter relation `150`, starter placement/dialog targeting. | `external_video_accept_observed_pending_local_dialog_smoke` |
| 00:01:39 | Bosun Redmark dialog option is `From the Wreckage`. | Q3479 starter relation and accept-path smoke target. | `external_video_accept_observed_pending_local_dialog_smoke` |
| 00:02:05 | Quest tracker shows `From the Wreckage [0]`, `33% Rescue Trapped Survivors`, and `0% Kill yeti`. | Q3479 objective `4467` Trapped Survivor progress and objective UI. | `external_video_progress_observed_pending_local_client_smoke` |
| 00:03:10 | Combat frame names `Yeti Snowstalker`; tracker shows `13% Kill yeti`. | Q3479 objective `4564`, target-group member `11945`, and yeti combat target visibility. | `external_video_combat_progress_observed_pending_local_client_smoke` |
| 00:04:30 | Tracker shows `From the Wreckage [7]` and `77% Kill yeti`. | Q3479 kill-objective progress and route-density corroboration. | `external_video_progress_observed_pending_local_client_smoke` |
| 00:05:45 | Tracker shows `Complete: From the Wreckage` and `Report to Deadeye Brightland`. | Q3479 objective completion, Deadeye finisher/receiver routing. | `external_video_completion_observed_pending_local_dialog_smoke` |
| 00:06:00 | Deadeye Brightland completion dialog is visible with a reward panel. | Q3479 Deadeye receiver/finisher and reward-presentation targeting. | `external_video_reward_panel_observed_pending_local_client_smoke` |

Corroborated source/table/test evidence:

- `wildstar_client_mysql/Quest2.tbl.sql`
- `wildstar_client_mysql/QuestObjective.tbl.sql`
- `wildstar_client_mysql/TargetGroup.tbl.sql`
- `wildstar_client_mysql/Creature2.tbl.sql`
- `wildstar_client_mysql/WorldLocation2.tbl.sql`
- `wildstar_client_mysql/Quest2Reward.tbl.sql`
- `wildstar_client_mysql/Item2.tbl.sql`
- `Tools/DataMapping/output/creature_quest_map.csv`
- `Tools/DataMapping/output/creature_spawn_map.csv`
- `Source/NexusForever.Script.Main/Quests/NorthernWilds/NorthernWildsMapScript.cs`
- `Source/NexusForever.Script.Main/Quests/NorthernWilds/Q3479FromTheWreckage.cs`
- `Source/NexusForever.Game/Quest/GlobalQuestManager.cs`
- `Source/NexusForever.Game/Entity/QuestManager.cs`
- `Source/NexusForever.Game.Tests/Quests/NorthernWildsBranchInteractionTests.cs`
- `Source/NexusForever.Game.Tests/Quest/QuestTests.cs`
- `Source/NexusForever.Game.Tests/Quest/GlobalQuestManagerPartialTableTests.cs`

Remaining local blockers:

- Local build 16042 Bosun accept dialog, quest visibility/prerequisite behavior,
  and negative accept cases.
- Exact Trapped Survivor CSI activation packets, phase/visibility, and duplicate
  activation behavior.
- Full map-guidance review for all indicator locations
  `12155/12156/12157/12158`.
- Yeti combat smoke for tap/threat/respawn/loot cadence and the Frostclaw
  member path not directly visible in the cited video frames.
- Deadeye completion activation, alternate receiver `12354` / prerequisite
  `29356` routing, reward-choice UI, item/currency persistence, achievement UI,
  and end-to-end Q4106 -> Q3479 -> Q3667 smoke.

## Q3480 - Reporting for Duty

Queue slice:

- Queue rank: 3
- Lane: quest
- Content id: `3480`
- Actionability: `quest_validation_ready`
- Current claim boundary: local tutorial footage corroborates the visible retail
  quest flow, but Q3480 remains `not_retail_complete` and
  `retail_claim_allowed=false`.

Local video:

| Video | Local path / URL | Duration | Notes |
| --- | --- | ---: | --- |
| Wildstar Tutorial - Northern Wilds - Quests (Exile) | `I:/Videos/Wildstar Tutorial - Northern Wilds - Quests (Exile)/Wildstar Tutorial - Northern Wilds - Quests (Exile) (1080p_60fps_H264-128kbit_AAC).mp4` / `https://www.youtube.com/watch?v=2aACq9Zg0KE` | 00:14:12.800 | Description lists Q3480 `Reporting for Duty`: 00:01:26 accept, 00:01:34 target area, 00:03:06 complete. English subtitles at 00:01:19-00:01:55 narrate Durek, Q3480 pickup, Yeti kills, and three survivors. |

Evidence observations:

| Timestamp | Visible evidence | Rows/blockers advanced | Disposition |
| --- | --- | --- | --- |
| 00:01:26 | Commander Durek is visible with dialog option `Reporting for Duty (3)`; tracker points to discovering the mission from Commander Durek. | Q3480 starter relation `821`, accept-path smoke target. | `external_video_accept_observed_pending_local_dialog_smoke` |
| 00:01:28 | Chapter overlay shows `Reporting for Duty (3)`, coordinates `4125.00, -686.10, -5242.58, -32.24`, `Talk to Commander Durek`, `152XP`, `83c`, `[Gloves]`, and `Begin Episode: Arrival`. | Durek starter placement, episode start, reward-presentation expectations. | `external_video_accept_observed_pending_local_dialog_smoke` |
| 00:01:34 | Map overlay shows `Kill Yeti & Rescue Trapped Survivors` and the highlighted crash-site/Ancient Tower target area. | Q3480 objective `4470`, objective `4565`, and indicator-location targeting. | `external_video_map_observed_pending_local_client_smoke` |
| 00:01:47 | Tracker shows `Reporting for Duty (3)`, `33% Rescue Trapped Survivors`, and `64% Kill yeti`; chat shows `Quest Accepted: Reporting for Duty`. | Trapped Survivor progress and Kill yeti progress. | `external_video_progress_observed_pending_local_client_smoke` |
| 00:02:39 | Tracker shows `Complete: Reporting for Duty` and `Report to Deadeye Brightland`. | Objective completion and Deadeye receiver routing. | `external_video_completion_observed_pending_local_dialog_smoke` |
| 00:03:10 | Map overlay shows `Report to Deadeye Brightland` at coordinates `4183.85, -722.56, -5691.58, -13.47`. | Q3480 receiver location `7726` and Deadeye routing. | `external_video_receiver_route_observed_pending_local_dialog_smoke` |
| 00:03:16 | Deadeye Brightland is visible with dialog option `Reporting for Duty (3)` and completed tracker. | Q3480 finisher relation `579` and completion-dialog target. | `external_video_dialog_observed_pending_local_dialog_smoke` |
| 00:03:20 | Deadeye reward panel shows `You will receive: 152 XP`, `83c`, and `Scout's Thermal Gloves`. | Q3480 reward presentation, fixed cash presentation, selectable item UI target. | `external_video_reward_panel_observed_pending_local_client_smoke` |
| 00:03:22 | Tooltip comparison for `Scout's Thermal Gloves` is visible against currently equipped gloves. | Reward item presentation and selection UI targeting. | `external_video_reward_panel_observed_pending_local_client_smoke` |

Corroborated source/table/test evidence:

- `wildstar_client_mysql/Quest2.tbl.sql`
- `wildstar_client_mysql/QuestObjective.tbl.sql`
- `wildstar_client_mysql/TargetGroup.tbl.sql`
- `wildstar_client_mysql/Creature2.tbl.sql`
- `wildstar_client_mysql/WorldLocation2.tbl.sql`
- `wildstar_client_mysql/Quest2Reward.tbl.sql`
- `wildstar_client_mysql/Item2.tbl.sql`
- `Tools/DataMapping/output/creature_quest_map.csv`
- `Tools/DataMapping/output/creature_spawn_map.csv`
- `Source/NexusForever.Script.Main/Quests/NorthernWilds/NorthernWildsMapScript.cs`
- `Source/NexusForever.Game/Quest/GlobalQuestManager.cs`
- `Source/NexusForever.Game/Entity/QuestManager.cs`
- `Source/NexusForever.Game.Tests/Quests/NorthernWildsBranchInteractionTests.cs`
- `Source/NexusForever.Game.Tests/Quest/QuestTests.cs`
- `Source/NexusForever.Game.Tests/Quest/GlobalQuestManagerPartialTableTests.cs`

Remaining local blockers:

- Local build 16042 Durek accept dialog, quest visibility/prerequisite behavior,
  duplicate accept behavior, and negative accept cases.
- Exact Trapped Survivor CSI activation packets, phase/visibility, and duplicate
  activation behavior.
- Full map-guidance review for all indicator locations
  `12155/12156/12157/12158`.
- Yeti combat smoke for tap/threat/respawn/loot cadence and named
  Snowstalker/Frostclaw member behavior, since the cited Q3480 frames show
  generic `Kill yeti` progress rather than creature-name plates.
- Deadeye completion activation, alternate receiver `12354` / prerequisite
  `29356` routing, reward-choice UI, item/currency persistence, achievement UI,
  and end-to-end Q3480 smoke.

## Current Queue Video-Evidence Baseline

Baseline rebuilt: 2026-06-19 CEST from
`Decomp/Analysis/coverage/content_retail_completeness_next_slice_queue.csv` and
`Decomp/Analysis/coverage/content_retail_completeness_next_slice_blockers.csv`.

Queue state:

- `23` current next-slice rows: `12` quests and `11` instances.
- All current rows have `retail_claim_allowed=false`.
- All current rows remain `not_retail_complete`; video evidence is only a
  corroboration and targeting input until local build 16042 smoke, source/table
  evidence, focused tests, generated outputs, and validator gates agree.
- Blocker details total `2,495` rows. Largest current families are
  `instance_dependency` (`736`), `public_event_evidence` (`584`),
  `script_objective_producer` (`538`), `instance_entity` (`105`),
  `script_runtime_action` (`93`), `quest_world_dependency` (`89`),
  `quest_creature` (`58`), and `instance_reward` (`48`).

Worker reconciliation:

- Local inventory worker inspected `I:\Videos` filenames plus nearby
  `.txt`/`.srt` sidecars and found `120` local video files with `230`
  sidecar/metadata files.
- CSV worker confirmed the queue, blocker counts, Q5597 first blockers, and
  `instance-3041` validation-ready blockers.
- Quest worker mapped all `12` `quest_validation_ready` rows to video search
  terms and the local-smoke gates that video cannot replace.
- Instance worker mapped all `11` instance queue rows to bounded video
  questions while preserving split-required parent blockers.

## Reusable Local Queue Index

| Alias | Local video | Duration | Queue use |
| --- | --- | ---: | --- |
| `CI1` | `I:/Videos/Wildstar Tutorial - Crimson Isle - Quests Part 1 (Dominion)/Wildstar Tutorial - Crimson Isle - Quests Part 1 (Dominion) (1080p_60fps_H264-128kbit_AAC).mp4` | 00:09:52.434 | Q5573, Q5597, shared Crimson Isle chain context |
| `CI2` | `I:/Videos/Wildstar Tutorial - Crimson Isle - Quests Part 2 (Dominion)/Wildstar Tutorial - Crimson Isle - Quests Part 2 (Dominion) (1080p_30fps_H264-128kbit_AAC).mp4` | 00:12:36.274 | Q5597, Q5580, Q5604 follow-up context |
| `NWQ` | `I:/Videos/Wildstar Tutorial - Northern Wilds - Quests (Exile)/Wildstar Tutorial - Northern Wilds - Quests (Exile) (1080p_60fps_H264-128kbit_AAC).mp4` | 00:14:12.800 | Q3479/Q3480/Q3486/Q3668/Q3673/Q3886/Q3963/Q3797 quest context |
| `NWPath-Soldier` | `I:/Videos/Wildstar Tutorial - Northern Wilds - Soldier Missions (Exile)/Wildstar Tutorial - Northern Wilds - Soldier Missions (Exile) (1080p_60fps_H264-128kbit_AAC).mp4` | 00:04:53.454 | Northern Wilds Soldier path episode `8` and missions `156`, `33`, `34` |
| `NWPath-Explorer` | `I:/Videos/Wildstar Tutorial - Northern Wilds - Explorer Missions (Exile)/Wildstar Tutorial - Northern Wilds - Explorer Missions (Exile) (1080p_60fps_H264-128kbit_AAC).mp4` | 00:04:31.767 | Northern Wilds Explorer path episode `9` and missions `158`, `35`, `36`, `1254` |
| `NWPath-Scientist` | `I:/Videos/Wildstar Tutorial - Northern Wilds - Scientist Missions (Exile)/Wildstar Tutorial - Northern Wilds - Scientist Missions (Exile) (1080p_60fps_H264-128kbit_AAC).mp4` | 00:04:48.160 | Northern Wilds Scientist path episode `28` and missions `160`, `648`, `42` |
| `NWPath-Settler` | `I:/Videos/Wildstar Tutorial - Northern Wilds - Settler Missions (Exile)/Wildstar Tutorial - Northern Wilds - Settler Missions (Exile) (1080p_60fps_H264-128kbit_AAC).mp4` | 00:03:45.164 | Northern Wilds Settler path episode `82` and missions `650`, `652`, `651` |
| `ProtoAcademy-local` | `I:/Videos/Wildstar Walkthrough Ep.9 w_Angel - Protogames Academy!.mp4` | 00:29:12.224 | Protogames Academy mechanics context only; not exact Ultimate Protogames proof |
| `FragmentZero-local` | `I:/Videos/Wildstar Walkthrough Ep.6 w_Angel - Fragment Zero!.mp4` | 00:34:00.639 | Instance `3180` candidate footage |
| `Ruins-local` | `I:/Videos/Wildstar Walkthrough Ep.24 w_Angel - Ruins of Kel Voreth!.mp4` | 00:23:31.378 | Instance `1336` candidate footage |
| `Stormtalon-local` | `I:/Videos/Wildstar Walkthrough Ep.27 w_Angel - Stormtalon's Lair!.mp4` | 00:31:02.310 | Instance `382` candidate footage |
| `Swordmaiden-local` | `I:/Videos/Wildstar Walkthrough Ep.35 w_Angel - Sanctuary of the Swordmaiden!.mp4` | 00:28:09.530 | Instance `1271` candidate footage |
| `Redmoon-local` | `I:/Videos/Wildstar Walkthrough Ep.38 w_Angel - Mordechai Redmoon!.mp4` | 01:01:12.769 | Redmoon/Mordechai raid context; not a current exact queue row |
| `Vault-news-local` | `I:/Videos/Wildstar News_ New Zone, New Dungeon & New Raid on the Horizon/Wildstar News_ New Zone, New Dungeon & New Raid on the Horizon (720p_30fps_H264-192kbit_AAC).mp4` | not timed here | Mentions Vault only; not gameplay proof |

## Online Search Index

Search/curation dates: 2026-06-19, updated 2026-06-24. These links are
external corroboration candidates, not local emulator evidence.

| Queue area | Candidate URL | Use |
| --- | --- | --- |
| Crimson Isle quests | `https://www.youtube.com/watch?v=uu4avnomefU` | Online mirror for `CI1` |
| Crimson Isle quests | `https://www.youtube.com/watch?v=YA0Gyn870aI` | Online mirror for `CI2` |
| Northern Wilds quests | `https://www.youtube.com/watch?v=2aACq9Zg0KE` | Online mirror for `NWQ` |
| Northern Wilds Q3673 cinematic | `https://www.youtube.com/watch?v=dPFnwNUoWcE` | User-provided 2026-06-24; start of video corroborates the Contact with Thayd cinematic as part of lighting the flares. |
| Northern Wilds Soldier path | `https://www.youtube.com/watch?v=EGsDwWfeeeU` | Online mirror for `NWPath-Soldier` |
| Northern Wilds Explorer path | `https://www.youtube.com/watch?v=Hf4N45QZbp8` | Online mirror for `NWPath-Explorer` |
| Northern Wilds Scientist path | `https://www.youtube.com/watch?v=S9BDXA90TLY` | Online mirror for `NWPath-Scientist` |
| Northern Wilds Settler path | `https://www.youtube.com/watch?v=-8ex2oZE-SQ` | Online mirror for `NWPath-Settler` |
| CutenessDoom tutorial playlist | `https://m.youtube.com/playlist?list=PLibV_Cg_WlvsKg4MvUcaXS7G4ebizzTPA` | Local tutorial-series cross-check |
| Ultimate Protogames | `https://m.youtube.com/watch?v=fVvx7Uo-8IA` | Candidate live dungeon guide for split-required row `2980` |
| Ultimate Protogames | `https://www.youtube.com/watch?v=eZsj02Rm_NA` | Candidate recent external run footage; needs timestamp review |
| Protogames Academy | `https://www.youtube.com/watch?v=hhahp97rHvw` | Context for training dungeon; not exact Ultimate Protogames proof |
| Stormtalon's Lair | `https://www.youtube.com/watch?v=T2OJbSH7aD8` | Candidate dungeon walkthrough |
| Sanctuary of the Swordmaiden | `https://www.youtube.com/watch?v=8lJX5YzUk8c` | Candidate dungeon walkthrough |
| Fragment Zero | `https://www.youtube.com/watch?v=oyU_RXfsiK4` | Candidate expedition walkthrough |
| Fragment Zero | `https://www.youtube.com/watch?v=y4g3YVf3_Eo` | Candidate shiphand run |
| Skullcano | `https://www.youtube.com/watch?v=Q9fNxG9IK1Q` | Candidate dungeon run |
| Gauntlet | `https://www.youtube.com/watch?v=WqUHl-fVLLY` | Candidate shiphand walkthrough |
| Evil from the Ether | `https://www.youtube.com/watch?v=vHL7ODtX0uU` | Candidate expedition footage |
| Evil from the Ether | `https://www.youtube.com/watch?v=AY3W8n8Zh3c` | Official/livestream context; may not be clean blocker proof |
| Space Madness | `https://www.youtube.com/watch?v=FHZ3GMRvXHg` | Candidate video with Space Madness link/context in description |
| Vault of the Archon | `https://www.youtube.com/watch?v=EwoFqQ9Kw5Y` | Candidate world-story walkthrough |
| Vault of the Archon | `https://www.youtube.com/watch?v=vF2fQvphDtA` | Candidate multi-part footage |
| Ruins of Kel Voreth | `https://www.youtube.com/watch?v=c_vIxh03mnk` | Candidate veteran guide |

## Current Quest Queue Disposition

| Rank | Slice | Local/online evidence | Video disposition | Still blocked |
| ---: | --- | --- | --- | --- |
| 1 | `quest-5597` Dregs and Thieves | `CI1` 00:06:47 accept; `CI2` 00:00:23 target/complete. Existing detailed rows above also record 00:06:52, 00:00:35, and 00:00:50. | `external_video_observed_pending_local_client_smoke` for Mondo accept/complete, Chua Explosives collect prompt/progress, reward dialog, and Q5597 -> Q5604 handoff. | Local route/map guidance, indicator locations `17896/17898/17897/17899`, activation packet timing, despawn/respawn visuals, duplicate/negative cases, reward persistence, achievement UI, end-to-end Q5596 -> Q5597 -> Q5604. |
| 2 | `quest-3479` From the Wreckage | Online AyinMaiden video `https://www.youtube.com/watch?v=iMSWhvau5mI`: 00:01:39 Bosun dialog option, 00:02:05 objective progress, 00:03:10 Yeti Snowstalker combat/progress, 00:05:45 completion route to Deadeye, 00:06:00 Deadeye completion dialog/reward panel. `NWQ` remains useful adjacent Q3480/shared-mechanic context. | `external_video_observed_pending_local_client_smoke` for Bosun accept targeting, Trapped Survivor/yeti progress, Deadeye completion routing, and reward-panel presentation. | Local CSI activation packets, phase/visibility, all indicator locations, yeti tap/threat/respawn/loot, Frostclaw-specific smoke, alternate receiver/prereq routing, reward-choice/persistence, achievement UI, negative cases, Q4106 -> Q3479 -> Q3667 end-to-end smoke. |
| 3 | `quest-3480` Reporting for Duty | `NWQ` / online mirror `https://www.youtube.com/watch?v=2aACq9Zg0KE`: 00:01:26 Durek dialog option, 00:01:28 accept overlay with coordinates/rewards, 00:01:34 map objective `Kill Yeti & Rescue Trapped Survivors`, 00:01:47 objective progress, 00:02:39 completion route, 00:03:10 Deadeye route coordinates, 00:03:16 Deadeye dialog, 00:03:20-00:03:22 reward panel/tooltip. | `external_video_observed_pending_local_client_smoke` for Durek accept targeting, shared survivor/yeti objective presentation/progress, Deadeye completion routing/dialog, and reward-panel presentation. | Local Durek accept/prerequisite negatives, CSI activation packets, phase/visibility, all indicator locations, yeti tap/threat/respawn/loot, Snowstalker/Frostclaw-specific behavior, alternate receiver/prereq routing, reward-choice/persistence, achievement UI, end-to-end Q3480. |
| 4 | `quest-3486` Empowered Tower | `NWQ` 00:03:43 accept, 00:03:50 target, 00:04:35 complete. | High-confidence chapter evidence for Exo-Lab/objective flow. | Trigger timing/story panel, Loftite Crystal collision/respawn, Crystal Guardian/Frostbite proof, reward mismatch review, reward/achievement smoke. |
| 5 | `quest-3668` Indigenous Intelligence | `NWQ` 00:05:41 accept, 00:08:12 and 00:08:25 target, 00:09:36 complete. | High-confidence chapter evidence for Lusk/prisoner/Skeech video targeting. | Local activate/CSI packets, survivor transform/despawn, missing target-group member review, combat/tap/loot smoke, rewards/achievements. |
| 6 | `quest-3673` Contact with Thayd | `NWQ` 00:10:07 chapter; user-provided `https://www.youtube.com/watch?v=dPFnwNUoWcE` start-of-video shows the flare-lighting cinematic. | External video now corroborates that the Q3673 cinematic plays as part of the flare-lighting resolution; local build 16042 client smoke is still required before retail-complete claims. | Signal flare activation packet timing, phased visibility, local hidden completion objective `13391`/cinematic packet ordering smoke, reward side effects, end-to-end smoke. |
| 7 | `quest-3886` Fiery Distraction | `NWQ` 00:09:05 accept, 00:09:12 target, sidecar lists 00:08:51 complete. | Medium-confidence evidence; timestamp anomaly needs visual check before citing exact completion. | Torch/hut activation packets, burn/despawn/respawn visuals, duplicate relation review, reward-choice UI/persistence. |
| 8 | `quest-3963` More Important Than Revenge | `NWQ` 00:12:09 accept, 00:12:47 target, 00:13:14 complete. | High-confidence chapter evidence for ship controls/Deadeye handoff targeting. | Exact activation timing, Q3487 -> Q3963 chain proof, receiver dialog, reward/achievement side effects. |
| 9 | `quest-5573` Powering Down | `CI1` 00:02:36 accept, 00:02:41 target, 00:03:43 target/complete. | High-confidence chapter evidence for Power Regulator mechanics. | World trigger `18449`, active-prop visuals/despawn/respawn, hidden objective `12870`, cinematic timing, reward/progression smoke. |
| 10 | `quest-5575` Seizing Power | `CI1`/`CI2` Crimson Isle group only; no exact title hit in local captions or descriptions. | Low-confidence shared-mechanic target only through Power Regulator footage; exact Q5575 proof still missing. | Exact title/objective proof, hidden objective `12871`, no-cinematic retail check, Kezrek dialog/spawn path, reward/progression smoke. |
| 11 | `quest-5580` Enforced Radio Silence | Frame-reviewed local `CI2`: 00:07:24 map/tracker/chat shows `Quest Accepted: Enforced Radio Silence`, `[0/2] Sabotage the Tower Controls`, and Megatech Station context; 00:07:30 marks the first Tower Control at `-7596.38, -941.54, -1356.19, -81.83`; 00:09:50 marks the second Tower Control at `-7700.23, -933.39, -1410.65, 47.31` with `Sabotage` prompt, tracker `[1/2]`, and Datachron countdown; 00:10:02 shows Kezrek reward dialog and chat `Quest Completed: Enforced Radio Silence`; 00:10:08 shows `Last Resistance` unlock text. | `external_video_observed_pending_local_client_smoke`: route/objective UI, first/second Tower Control prompts/progress, completion/reward dialog, and follow-up unlock are externally observed. | Local activation packet timing, active-prop despawn/respawn, QuestDirection `288` entries `510/511` local route verification, Kezrek receiver override/starter-slot explanation, Q5604 -> Q5580/Q5583 -> Q5594 end-to-end flow, reward inventory/achievement UI, local build-16042 client smoke, and negative/repeat cases. |
| 12 | `quest-3797` Securing the Area | `NWQ` 00:06:01 accept, 00:06:24 target, 00:06:37 complete. | High-confidence chapter evidence for Durek/rootbrute/yeti targeting. | Local combat kill credit, placement/density/respawn/tap/threat, type-9 branch review, Q3486 prerequisite flow, Durek dialog, reward side effects. |

## Current Instance Queue Disposition

| Rank | Slice | Evidence candidates | Video disposition | Still blocked |
| ---: | --- | --- | --- | --- |
| 13 | `instance-2980` Ultimate Protogames | Online: Ultimate Protogames guide/run candidates. Local: Protogames Academy videos are context only. | External video can identify bounded room/objective families, but parent remains `split_required_producer_gap`. | Full random room selection, room mechanics, producer timing, rewards, achievements, full dungeon smoke. |
| 14 | `instance-382` Stormtalon's Lair | `Stormtalon-local`; online dungeon guide candidates. | External/local footage can corroborate boss order, altar/totem/cage/launch-pad prompts, and route visuals. | Exact combat/wave mechanics, route weights, grenade semantics, rewards, achievements, full dungeon smoke. |
| 15 | `instance-3009` Vault of the Archon | Online Vault walkthrough candidates. Local news video is a mention only. | Use online full gameplay for Hall of the Hundred/Vault phase targeting; local news is not blocker proof. | Party-count semantics, portal/exit routing, chase mechanics, optional events, rewards, achievements, full world-story smoke. |
| 16 | `instance-1271` Sanctuary of the Swordmaiden | `Swordmaiden-local`; online dungeon guide candidates. | Footage can map boss/miniboss order, relic/cluster/totem/spore UI, Selene escort/challenge visibility. | Exact boss mechanics, route weights, spawn variants, challenge failures, rewards, achievements, full dungeon completion. |
| 17 | `instance-3180` Fragment Zero | `FragmentZero-local`; online expedition walkthrough candidates. | Footage can map defense panel, recordings, eggs, Hugo/Jo/Syrus objectives, timer UI, and cinematics. | Skeech ambush producer, trigger correctness, timer/reward semantics, portal/queue, full expedition smoke. |
| 18 | `instance-1263` Skullcano | Online Skullcano run/guide candidates; no local exact match found. | Online footage can target Tugga/Thunderfoot/Bosun/Quartermaster/Mordechai order and interactable prompts. | Escort/failure conditions, door/platform choreography, optional objectives, spawn cadence, rewards, achievements. |
| 19 | `instance-2183` Gauntlet | Online Gauntlet shiphand walkthrough candidate; no local exact match found. | Online footage can target arena order, Pilot/Judge/Agent flow, prompts, score UI. | Score-token/gold-skull semantics, route selection, cinematic timing, door state, rewards, full expedition smoke. |
| 20 | `instance-3404` Evil from the Ether | Online expedition and livestream candidates; no local exact match found. | Online footage can target Captain Weir, medbay/parts/generator flow, teleports, Etheric Organism/Katja phases, timer UI. | Portal failure proof, organism wave cadence, diagnostics/despawn timing, placement smoke, rewards, achievements. |
| 21 | `instance-2149` Space Madness | Online Space Madness candidate via playthrough/context links; no local exact match found. | Video can target Captain Tero/Major Lee flow, computers, hazmat/control panels, air scrubber, datapads, timer/cinematic UI. | Hallucination counters, air-scrubbing wave/timer, Rowsdower failure tracking, rewards, full expedition smoke. |
| 22 | `instance-3041` Map/UltimateProtogamesRaid | Online Ultimate Protogames/Downsizer candidates; local Protogames Academy is context only. | Use video only to target Downsizer PE642, objective `3197`, challenge objectives `3266-3269`, boss visibility, portal/settings UI. | Exact portal route, challenge ability semantics, boss mechanics, rewards, achievements, manual client smoke. |
| 23 | `instance-1336` Ruins of Kel Voreth | `Ruins-local`; online veteran guide candidates. | Footage can target Grond/Drokk/Trogun/Gurka order, schematic/supplies/phase monitor/forge prompts, doors/triggers, cinematics. | Exact boss mechanics, optional routes, spawn cadence, Drokk exo-defense failure, rewards, achievements, full dungeon smoke. |

## Next Video-Evidence Capture Targets

1. Visual-review the high-confidence local quest chapters (`NWQ`, `CI1`, `CI2`)
   and promote only timestamped UI-visible observations into row-specific
   evidence notes.
2. For `Q5575`, find exact title/objective footage or record it as still
   needing targeted local capture; nearby shared-mechanic footage is not enough.
3. For `instance-3041`, search for exact Downsizer/Ultimate Protogames PE642
   footage before touching implementation. If none is found, create a
   `Start-BlockerEvidenceHarness.ps1 -CreateBundleOnly` worksheet for portal,
   boss, challenge objective, reward, and achievement smoke.
4. For split-required instance parents, use video only to propose bounded
   validation-ready sub-slices. Do not promote a whole instance row from video
   guide footage.
