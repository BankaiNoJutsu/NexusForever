# Milestone 1: Playable Final-Live NPE PvE Slice

## Goal
Get a new character into world `3460` with the WildStar `16042` client, with `Map\NewPlayerExperience` loading, core NPE spawns visible, and one simple tutorial kill quest working end to end.

Success means assets load, world DB imports, the player can enter the NPE map, kill one creature, see quest progress advance, complete the quest, receive table-backed XP/money/rewards, and watch the creature respawn. Full scripted NPE phases, loot, AI polish, and broader expedition systems stay out of scope.

## Summary
- Restore a focused New Player Experience slice for WildStar build `16042`: local full-stack login, character creation, world `3460` entry, visible NPE spawns, combat kills, kill quest progress, quest completion rewards, and creature respawn.
- Work from `I:\GIT\NexusForever` on `game_rework`. The client table exports identify world `3460` as `Map\NewPlayerExperience`; the matching base-map file should be `NewPlayerExperience.nfmap`.
- Treat source truth in this order: extracted 16042 client `tbl`/`bin` and generated `nfmap` assets; latest `NexusForever.WorldDatabase` SQL from GitHub; local searchable client/JabbitHole SQL dumps under `D:\temp\wildstar` and `D:\Wildstar_TEMP`; human-readable references from `https://www.jabbithole.com/` and `https://www.emulator.ws/`.
- Do not fabricate unrecovered retail loot, XP rates, creature formulas, or scripted behavior. Missing or uncertain data stays disabled with explicit TODOs.
- Treat this as the first playable PvE loop and the foundation for later zone-by-zone restoration.

## Resource Baseline
- `I:\GIT\NexusForever` is the main source checkout and contains `Source\NexusForever.slnx`, `Source\NexusForever.sln`, Aspire projects, the map generator, and current world schema migrations.
- `D:\temp\wildstar\NexusForever.WorldDatabase` is an older local WorldDatabase clone from 2021. It has continent SQL and `All_NexusForever.WorldDatabase.sql`, but it does not contain the current `Instance\Tutorial\New Player Experience.sql`; update it or make a fresh clone before using it for milestone acceptance.
- `https://github.com/NexusForever/NexusForever.WorldDatabase` is the current optional world-data source. It contains `Instance\Tutorial\New Player Experience.sql`, including `entity.Mode` values and `entity_script` rows needed by the NPE data.
- `D:\temp\wildstar\wildstar_client_mysql` and `D:\Wildstar_TEMP\optimized.*.sql` are useful searchable SQL exports of client tables. They confirm the NPE world, NPE map zones, NPE client events, quest objectives, quest rewards, achievements, target groups, creature data, and localization references.
- `D:\temp\wildstar\jabbithole_mysql` and `D:\temp\wildstar\all_jabbithole_mysql.sql` are JabbitHole dumps with quests, creatures, items, drops, rewards, and coordinates. Use them for cross-checking and human-readable lookup, not as direct NexusForever world-schema imports.
- No `*.tbl`, `*.bin`, or `*.nfmap` runtime assets were found in the listed local folders during this review, so runtime verification still requires an actual extracted 16042 client asset output or a known existing asset directory.
- The official NexusForever repo has a branch `expedition-evil-from-the-ether` at `146e4f0c` that contains the Evil from the Ether expedition work plus supporting script, combat, spell, entity-script, entity-mode, public-event, and initial NPE tutorial changes.
- `https://github.com/derdotte/NexusForever/tree/Prerequisites` is a descendant of that official expedition branch at `e49314c`. It contains the expedition work and adds six prerequisite-focused commits: faction/reputation/death-state checks, prerequisite handler refactoring, item-on-character/equipped checks, and distance-to-world-location checks. Treat it as a useful reference branch, not as the baseline upstream.

## Key Changes
- Bootstrap the local verification environment using the current NexusForever docs: install .NET 10 SDK, Docker Desktop, and the Aspire prerequisites; use Aspire for MariaDB/RabbitMQ; build with `dotnet build Source\NexusForever.slnx`.
- Generate or locate runtime assets from a WildStar 16042 `Patch` directory with `NexusForever.MapGenerator`: extract `tbl`/`bin`, generate base maps, and at minimum generate world `3460`. Configure `GameTablePath` to the extracted `tbl` folder and `MapPath` to the generated `map` folder.
- Add world `3460` to both `Realm:Map:PrecacheBaseMaps` and `Realm:Map:PrecacheMapSpawns` for this slice.
- Refresh or separately clone the latest `NexusForever.WorldDatabase` before import. Point `AspireMigrations.json` `WorldDatabase:Path` at that updated checkout, not the stale 2021 local clone unless it has first been updated.
- Make world DB import compatible with current NPE SQL without editing upstream dumps: add/import `entity.mode` with a safe default, add/persist `entity_script`, and verify `Instance/Tutorial/New Player Experience.sql` imports cleanly with entities for world `3460`.
- Tighten migration visibility for this milestone: an NPE SQL import failure should block acceptance, and post-import checks should verify `version` records the NPE file plus nonzero `entity` rows for world `3460`.
- Keep DB-driven script execution conservative: preserve existing compiled script filters such as the tutorial map script; store `entity_script.scriptName` data, but do not execute arbitrary script-name strings until a later explicit script resolver pass.
- When borrowing from the expedition/prerequisite branches, cherry-pick or port only the pieces needed by this NPE slice: `entity_script` persistence, `entity.mode`, safe script filtering, tutorial phase 1 scripts, and prerequisite checks needed by NPE visibility/objectives. Do not pull full Evil from the Ether expedition behavior into this milestone unless it is required to compile a reused shared abstraction.
- Complete the PvE kill loop in `Source\NexusForever.Game\Entity\UnitEntity.cs`: current death rewards update quest kill objectives only. Add kill achievement checks for creature IDs and target groups, clear combat state safely, and respawn non-player creatures in place after a configurable default delay.
- Keep rewards retail-shaped but minimal. `QuestManager` already grants quest XP/money and item/currency reward rows it can map; validate NPE reward rows from `Quest2Reward` and log unsupported reward types instead of crashing. Creature item loot and creature kill XP remain disabled unless a verified 16042 table/formula source is identified.
- Add safe handlers for client loot requests if needed so unsupported loot interactions fail quietly or log at debug level instead of throwing or disconnecting.

## Public Interfaces And Data
- Add world DB model/schema support for `entity.mode` and `entity_script`; expose the data to import/query paths while leaving runtime string-script binding off by default.
- Add `World:CreatureRespawnSeconds` config with default `30` for this milestone.
- Keep the existing `QuestManager`, `XpManager`, `CurrencyManager`, `AchievementManager`, `AssetManager`, and map precache flows; no new external service API is required.

## Test Plan
- Build: `dotnet build Source\NexusForever.slnx` after .NET 10 SDK is installed.
- Asset verification: confirm `World.tbl` contains world `3460` as `Map\NewPlayerExperience`, `GameTablePath` resolves, and `MapPath` contains `NewPlayerExperience.nfmap`.
- WorldDatabase verification: confirm the active WorldDatabase checkout contains `Instance\Tutorial\New Player Experience.sql`; import into a fresh disposable world DB; verify `version` records the file; verify world `3460` has entity rows and `entity_script` rows import.
- Local full stack: start Aspire, let migrations/imports finish, create/login an account, create a new character, connect with the 16042 client, and enter the NPE map.
- Manual acceptance: tutorial cinematic triggers, NPE NPCs/creatures spawn, a tutorial creature can be killed, kill quest objectives advance, the creature respawns, and a completable NPE quest grants XP/money/reward rows without server exceptions.
- Regression smoke: import should not break older WorldDatabase files that omit `Mode` or `entity_script`, and unsupported loot/reward rows must log rather than crash.

## Assumptions
- Work continues on `game_rework` and targets WildStar client build `16042`.
- Verification can use disposable local databases.
- The local SQL dumps are evidence and lookup material, not a substitute for actual runtime `tbl`/`bin`/`nfmap` assets.
- JabbitHole's current public site is a useful Drop 6/Reloaded browsing reference; conflicts are resolved in favor of the 16042 client data and the current NexusForever WorldDatabase SQL.
- This milestone intentionally excludes housing, raids, PvP, full loot tables, full AI behavior, arbitrary DB script execution, and broad zone restoration.
