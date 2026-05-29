# LaughingWS Instances-and-more Audit

Updated: 2026-05-27

Branch: `LaughingWS/NexusForever` `Instances-and-more`

- Merge base: `5fa96f9131b2685b65cbc58be84cf51bb7626384`
- Audited head: `00b06ea9e956637873be1fc6425e2047b8f99fd2`
- Local tracking ref: `laughingws/Instances-and-more`

This note tracks how the branch is being used for instance, expedition,
adventure, battleground, arena, dungeon, raid, and event-instance content. Do
not blindly copy the branch: it contains useful objective ids and script
scaffolds, but many event details are guessed, branch-local, typo-prone, or
missing stronger 16042 retail proof. Current table evidence, client-reader
evidence, current script patterns, and focused tests stay authoritative.

Remaining smoke/proof-only blockers from the implementation plan are tracked
task-by-task in
`Decomp/Analysis/LAUGHINGWS_REMAINING_BLOCKERS_CLOSURE_MATRIX.md`: unproven
instance, expedition, dungeon, raid, PvP, adventure, and event-instance behavior
stays mapped-only or explicitly rejected until the named evidence is captured.
The evidence harness itself is now implemented: `Start-BlockerEvidenceHarness.ps1`
creates timestamped bundles with manifests, command/log/client/negative-case
templates, screenshot/video/log folders, and log-tail/collection helpers.
The current closure-matrix pass records the remaining instance-side LWS rows as
implemented-with-verification, mapped-only with manual smoke or client/table
proof named, or rejected with a reason. It does not convert WIP-guessed
triggers, doors, cinematics, rewards, PvP scoring, or encounter mechanics into
retail-complete behavior.

## Ported Or Harvested

| Slice | Status |
| --- | --- |
| Cryo-Plex arena, Daggerstone Pass, Halls of the Bloodsworn, Walatiki Temple | Cryo-Plex map/event/sub-event ids were wired through the existing Slaughterdome arena runtime pattern as WIP-guessed code. Daggerstone/Halls/Walatiki map bindings create, join, and finish their public events through the current PvP content-map base and are marked WIP-guessed. Focused tests cover the branch PvP/adventure scaffold. |
| Coldblood Citadel | Conservative map binding, phase/objective progression, communicator hooks, entry/gather trigger handling, Hailstone Gatecrasher kill credit, and the branch optional-objective rolls for liquid Soulfrost, shards, Pell rally, prisoners, canisters, and Soulfrost traps were ported. The optional objective selection is WIP-guessed with code comments because exact retail route weights remain unproven. |
| War of the Wilds | Branch-mapped base adventure fight scaffold for the giant Moodie totem and totem-health objectives was ported as WIP-guessed code with focused tests. |
| Outpost M-13 | Conservative objective chain from Captain Milo through Hive Queen and shuttle return was ported; the unknown final shuttle trigger remains disabled because the branch candidate uses world-location id `5` at the zero vector with an inline "no world location???" note. |
| Space Madness | Conservative public event chain from Captain Tero through the all-clear signal was ported with focused tests. Airlock and research-laboratory participant-gather triggers are now ported as WIP-guessed behavior with code comments marking missing retail trigger-row/timing proof, and the on-create cinematic hook queues an immediate completion-only placeholder. |
| Protogames Academy | Map binding and conservative academy-initiation-to-Wrathbone objective chain were ported with focused tests. Invulnotron, Gromka, Iruki Boldbeard, source-only Seek-N-Slaughter, and source-only Icebox Mk. 2 SQL/WIP seed script hooks now credit their mapped defeat objectives as WIP-guessed behavior. PhineasARotostar1..9 phase communicator broadcasts and gather/teleporter world-location triggers are WIP-guessed with code comments marking missing timing/entity-cleanup proof. |
| Fragment Zero | Conservative search-continuation-to-Hugo chain was ported with focused tests. Supervisor Lola phase broadcasts, the Captain Hugo continuation follow-up, early search/friendly-Skeech/continuation triggers, and the first-warning cinematic hook are now ported as WIP-guessed behavior with code comments marking missing timing/cinematic/entity-cleanup proof, and the on-create cinematic hook queues an immediate completion-only placeholder. |
| Gauntlet | Corrected the branch's `Gautlet` path typo to `Gauntlet`, then ported the Pilot Taboro through Judge Kain/Agent Lex objective chain with focused tests. Airlock and swarm-pit participant-gather triggers plus the FindOutWhatHappened and Brick Braggor cinematic hooks are now ported as WIP-guessed behavior with code comments marking missing retail trigger-row/timing/cinematic proof. |
| Infestation | Conservative cargo-ship-entry through medical-bay/heal-shorthand objective chain was ported with focused tests. The initial cargo-ship turnstile trigger is now ported as WIP-guessed behavior with code comments marking missing retail placement/range proof, and the on-create cinematic hook queues an immediate completion-only placeholder. |
| Evil from the Ether | Branch-derived world `3404` map/public-event `781` route, dynamic trigger creation, door handling, medbay/upper-deck/escape teleports, communicator/cinematic sends, crew-log callouts, portal/tether behavior, Security Chief/Ravenous/Katja combat scaffolds, and `11` Drive Spark phase-anchor rows for `PickUpDriveSchematics` are retained. Trigger, teleport, communicator, cinematic, drive-spark placement, and encounter assumptions are marked WIP-guessed in code or generated SQL comments. Focused tests cover the initial phase, gather-trigger creation, medbay teleport/cleanup/message, participant-range recovery, objective/final transitions, cinematic phase messages, and teleport trigger scripts. |
| Ruins of Kel Voreth | Map binding and conservative Blood Pit through Forgemaster Trogun boss chain were ported with focused tests. The branch optional objective rolls for slave mercy, Eldan data storage, forge destruction, Osun, and war supplies are WIP-guessed with code comments/tests. Forgemaster Trogun, Grond, and Slavemaster Drokk SQL script hooks now credit their mapped defeat objectives as WIP-guessed behavior. Digsite Scar, Drokk, Exanite Forges, and Blood Pit cinematic triggers plus Blood Pit Gladiator death credit are also WIP-guessed with code comments marking missing retail trigger/faction/cinematic proof. |
| Stormtalon's Lair | Map binding and conservative Thundercall Pell survival through Stormtalon chain were ported with focused tests. Blade-Wind the Invoker and Aethros SQL script hooks now credit their mapped defeat objectives as WIP-guessed behavior, high-priest trigger objective script is WIP-guessed with code comments marking the missing retail trigger/count proof, and the branch optional objective rolls for tainted stems, altar data, storm totems, prisoners, grenades, Arcanist/Overseer route split, and Stormtalon reborn cinematic hook are now WIP-guessed with tests. |
| Skullcano | Map binding and conservative Thunderfoot/Stew-Shaman through Mordechai Redmoon chain were ported with focused tests. Branch optional side objectives for captured Lopp, Redmoon prisoners/marauders, and missile consoles are WIP-guessed with code comments/tests. Thunderfoot and Stew-Shaman Tugga SQL script hooks now credit their mapped defeat objectives as WIP-guessed behavior. Cave/chasm route selection, Find Chief and cave continuation objectives, Chasm, Find Chief, platform, and Eldan Terraformer trigger objective scripts plus Dorian/Artemis cave, chasm, Bosun, prisoner, and missile-console communicator broadcasts are also WIP-guessed with code comments marking the missing smoke proof. |
| Initialization Core Y-83 | Map binding and conservative quarantine-door-to-boss objective chain were ported with focused tests. The central-access-corridor Nurton communicator trigger plus Nurton2/Nurton3 phase/final-boss broadcasts and the open-door cinematic hook are now ported as WIP-guessed behavior with code comments marking the missing retail trigger/timing/cinematic proof. |
| Shade's Eve | Map binding and conservative early objective chain from fountain discovery through the locals vote handoff were ported with focused tests. TheAngel1/TheAngel5 callouts are now ported as WIP-guessed behavior with code comments marking missing cinematic/timing proof, the Etty/fountain branch rows are preserved as WIP-guessed instance seed anchors, and the on-create cinematic hook queues an immediate completion-only placeholder. |
| Red Moon Terror | Map binding and conservative main raid chain from the Brig through Laveka were ported with focused tests. Ish'amel Robomination and engineering callouts are now ported as WIP-guessed behavior with code comments marking missing timing/choreography proof, and the source-only Laveka row now attaches a WIP-guessed objective-credit script for the final Laveka objective. |
| Genetic Archives | Map binding and conservative middle-to-final raid chain through Dreadphage Ohmna were ported with focused tests. Ohmna entry/final cinematic callouts and the open-Ohmna-door cinematic hook are now WIP-guessed with code comments marking missing cinematic/timing proof. Experiment X-89 and Kuralak SQL script hooks now credit their mapped defeat objectives as WIP-guessed behavior, while Kuralak's pillar is only a WIP no-op loader hook pending mechanics proof. |
| Sanctuary of the Swordmaiden | Map binding and conservative Deadringer Shallaos through corrupted Spiritmother Selene dungeon chain were ported with focused tests. The branch phase/objective type bug around spirit relic placement was corrected during the port. Branch optional side objectives for corrupted Torine spirits, Moldwood corruptors, skurge/crawlers, soul spores, terrorantulas, and Corrupted Lifeweaver Pell are WIP-guessed with code comments/tests. The branch random Temple/Moldwood route choice, Flame-Crazed Demon objective credit, Temple, Moldwood, Lifeweaver Terrace, flame-miniboss, and random-path Spiritmother Selene communicator behavior are also WIP-guessed with code comments marking the missing smoke proof. |
| Datascape | Map binding and conservative raid objective chain through all three datacores and Avatus were ported with focused tests. Caretaker111..114 wing callouts and the Avatus-spawn cinematic hook are now WIP-guessed with code comments marking missing timing/wing-order/cinematic proof. Optimized Memory Probe TX-67 now credits its mapped objective through a WIP-guessed SQL script-name alias. The empty branch `Enter` phase was made actionable, the Warmonger Chuna phase bug was corrected, and all three personality datacores are required before the Oculus handoff. |
| Deep Space Exploration, Rage Logic, Ultimate Protogames dungeon/raid, Protostar SuperMall in the Sky, Journey into OMNICore-1 | Branch objective/phase/cinematic catalogs are now consumed by narrow WIP-guessed entry/phase/cinematic-hook scaffolds: Deep Space activates `TalkToCrewMembers` and queues a completion-only on-create cinematic placeholder, Rage Logic sets `ChooseAVehicle` then stops before unproven vehicle routing, Ultimate Protogames dungeon activates `InitiateUltimateProtogames` then stops at the coarse random-event gate, Ultimate Protogames raid activates the Downsizer objective set and finishes on Downsizer defeat, SuperMall activates the greeter gather objective then stops at the coarse random-path gate and queues a completion-only on-create cinematic placeholder, and OMNICore queues an immediate completion-only placeholder for the branch on-create cinematic hook. Deeper routing, store/room selection, real cinematics, rewards, doors, vehicle choice, and encounter behavior remain blocked. |

## Verification

| Gate | Result |
| --- | --- |
| Focused branch/path/event test filter | `547/547` passed. |
| Focused instance/public-event test filter | `436/436` passed with isolated output because the local world server locks normal debug binaries. |
| Focused map-only entry/boss/phase/cinematic-hook scaffold tests | `22/22` passed with isolated output. |
| Focused event/trigger cinematic-hook tests | `155/155` passed with isolated output. |
| Focused Coldblood Citadel event tests | `10/10` passed with isolated output. |
| Focused Ruins of Kel Voreth event/trigger tests | `16/16` passed with isolated output. |
| Focused Stormtalon's Lair event/trigger tests | `19/19` passed with isolated output. |
| Focused Skullcano event/trigger tests | `34/34` passed with isolated output. |
| Focused Sanctuary of the Swordmaiden event/trigger tests | `38/38` passed with isolated output. |
| Focused map-binding tests | `19/19` passed with isolated output. |
| Focused PvP/adventure branch tests | `25/25` passed with isolated output. |
| Focused Evil from the Ether event/trigger tests | `12/12` passed with isolated output. |
| Focused Protogames Academy trigger tests | `25/25` passed with isolated output. |
| Focused Fragment Zero trigger tests | `13/13` passed with isolated output after fixing a missing test import for `Identity`. |
| Focused Gauntlet trigger tests | `16/16` passed with isolated output. |
| Focused Infestation trigger tests | `9/9` passed with isolated output after retrying with MSBuild node reuse disabled to avoid a transient `obj` DLL lock. |
| Focused SQL objective-credit and Datascape script tests | `PublicEventObjectiveCreditEntityScriptTests|DatascapeEventScriptTests` passed `102/102`. |
| Focused transporter tests, from adjacent branch harvest state | `49/49` passed. |
| Branch spell-script tests, from adjacent branch harvest state | `11/11` passed. |
| `NexusForever.Script.Instance` build | Passed with `0` warnings and `0` errors after the map-only entry scaffold pass. |
| Focused Space Madness trigger tests | `8/8` passed with an isolated `OutDir` because the local world server had the normal debug output locked. |
| Manual client smoke | Not run. Manual instance, dungeon, raid, arena, battleground, and event-instance smoke remains required before claiming retail-complete content behavior. |
| LWS tracker verification | The post-review focused gates passed for PvP/adventure `29/29`, expedition `78/78`, dungeon `149/149`, and raid/event-instance `169/169`; no new instance/public-event script behavior was added by the current closure-matrix pass. |

## Not Fully Implemented Or Blocked

| Slice | Blocked or incomplete work |
| --- | --- |
| Shared public-event behavior | Public-event votes, scoreboards, exact objective notification parity, quest-share precision, encounter-specific boss scripts, exact spawn/despawn choreography, and manual content smoke remain incomplete. |
| Triggers, doors, cinematics, and communicators | Many branch trigger coordinates, door ids, communicator timings, and cinematics are guessed or branch-local. They remain blocked until retail/client/table evidence maps the trigger rows, coordinates, timing, and send conditions. |
| Cryo-Plex, Daggerstone Pass, Halls of the Bloodsworn, Walatiki Temple | Branch-derived PvP map/event/sub-event behavior is WIP-guessed and covered by focused tests. Queue/match smoke, scoring, rewards, round/capture objectives, exact PvP stats, and battleground/arena parity remain blocked. |
| Coldblood Citadel | Branch optional-objective selection for liquid Soulfrost, shards, Pell rally, prisoners, canisters, and Soulfrost traps is WIP-guessed and covered by focused tests. Exact optional route weights, objective trigger placement, door/choreography proof, and full dungeon route proof remain blocked pending manual smoke and stronger evidence. |
| War of the Wilds | Branch-derived totem objective scaffold is WIP-guessed and covered by focused tests. Faction-specific start events `170`/`171`, end-delay behavior, chat timing, rewards, and manual adventure smoke remain blocked. |
| Outpost M-13 | Final shuttle world-location trigger proof and manual expedition smoke remain blocked. The branch's id `5` / zero-vector shuttle trigger is rejected as runtime behavior until a real retail/table-backed shuttle trigger is mapped. |
| Space Madness | Airlock and research-laboratory participant-gather triggers plus the on-create completion-only cinematic placeholder are WIP-guessed and covered by focused tests. Placeholder branch door ids, direct local teleport proof, exact trigger timing, real cinematic payload, and manual expedition smoke remain blocked. |
| Protogames Academy | Invulnotron, Gromka, Iruki Boldbeard, Seek-N-Slaughter, and Icebox Mk. 2 objective-credit scripts, PhineasARotostar1..9 phase broadcasts, and gather/teleporter trigger spawns are WIP-guessed and covered by focused tests. Exact boss combat mechanics, trigger timing, communicator timing, platform/launcher cleanup, and entity-removal choreography remain blocked. |
| Fragment Zero | Supervisor Lola/Captain Hugo callouts, early search/friendly-Skeech/continuation triggers, and the on-create completion-only cinematic placeholder are WIP-guessed and covered by focused tests. Exact trigger timing, real cinematic payload, communicator timing, doors, and entity cleanup remain blocked. |
| Gauntlet | Airlock and swarm-pit participant-gather triggers are WIP-guessed and covered by focused tests. Cinematics, announcer/communicator timing, arena doors, exact trigger timing, and manual expedition smoke remain blocked. |
| Infestation | Cargo-ship turnstile trigger behavior and the on-create completion-only cinematic placeholder are WIP-guessed and covered by focused tests. Exact placement/range proof, door/open-vent choreography, real cinematic payload, exact medical-bay attack timing, parasite objective activation, and manual expedition smoke remain blocked. |
| Evil from the Ether | Public-event route, medbay/upper-deck/escape teleports, dynamic triggers, crew-log callouts, communicator/cinematic sends, portal/tether behavior, Security Chief/Ravenous/Katja scripts, and the `11` Drive Spark phase anchors are WIP-guessed and covered by focused tests or extractor verification. Exact retail trigger rows/coordinates, direct medbay transition proof, door/marker cleanup, crew-log targeting, drive-spark interaction/visual smoke, Katja choreography, boss spell cadence/mechanics, cinematic ids/timing, rewards, and manual expedition smoke remain blocked. |
| Ruins of Kel Voreth | Optional objective rolls, Forgemaster Trogun, Grond, Slavemaster Drokk, Digsite Scar, Drokk, Exanite Forges, Blood Pit cinematic trigger, and Blood Pit Gladiator death-credit behavior are WIP-guessed and covered by focused tests. Exact boss combat mechanics, trigger placement, door choreography, optional route weights/objective availability, faction-specific communicator targeting, real Blood Pit cinematic payload/runtime proof, and the branch duplicate-owner Forgemaster message trigger remain blocked. The Forgemaster message trigger duplicates owner `446` while sending the wrong message variable in its second loop. |
| Stormtalon's Lair | Blade-Wind, Aethros, high-priest trigger objective update, branch optional objective rolls, and the Stormtalon reborn completion-only cinematic placeholder are WIP-guessed and covered by focused tests. Exact boss combat mechanics, trigger placement/count, real Stormtalon reborn cinematic payload, optional route weights, objective availability, and boss spawn/version selection remain blocked. |
| Skullcano | Optional captured Lopp, prisoner/marauder, and missile-console objectives; Thunderfoot; Stew-Shaman Tugga; WIP cave/chasm route selection; Chasm; Find Chief; cave continuation; platform; Eldan Terraformer trigger objective updates; and Dorian/Artemis cave, chasm, Bosun, prisoner, and missile-console broadcasts are WIP-guessed and covered by focused tests. Exact boss combat mechanics, cave/chasm route weights, Chief Kaskalak choreography, trigger placement, door/platform choreography, communicator timing/faction pairing, optional route weights/objective availability, and manual dungeon smoke remain blocked. |
| Initialization Core Y-83 | The central-access-corridor Nurton trigger, Nurton2/Nurton3 event broadcasts, and open-door completion-only cinematic placeholder are WIP-guessed and covered by focused tests. Quarantine/corridor trigger placement, door entity choreography, exact communicator/cinematic timing, target-set proof, real cinematic payload, and manual raid smoke remain blocked. |
| Shade's Eve | TheAngel1/TheAngel5 callouts, Etty/fountain seed anchors, and the on-create completion-only cinematic placeholder are WIP-guessed and covered by focused tests/extractor verification. Gather-ring cleanup, town-gate opening, exact communicator/cinematic timing, real cinematic payload, exact vote follow-up, seed-row interaction smoke, and manual event smoke remain blocked. |
| Red Moon Terror | Ish'amel Robomination/engineering callouts plus the source-only Laveka seed row and WIP objective-credit hook are WIP-guessed and covered by focused tests/extractor verification. Exact communicator/cinematic timing, Laveka choreography/awakening/challenge mechanics, door/elevator movement, and manual raid smoke remain blocked. |
| Genetic Archives | Ohmna callouts, open-Ohmna-door completion-only cinematic placeholder, Experiment X-89 and Kuralak objective-credit scripts, plus the Kuralak pillar loader hook are WIP-guessed and covered by focused tests. Exact boss combat mechanics, pillar behavior, communicator/cinematic timing, real cinematic payload, weekly/random encounter selection, boss choreography, door/elevator movement, and manual raid smoke remain blocked. |
| Sanctuary of the Swordmaiden | Optional corrupted Torine spirits, Moldwood corruptors, skurge/crawlers, soul spores, terrorantulas, and Corrupted Lifeweaver Pell objectives; WIP Temple/Moldwood route choice; Flame-Crazed Demon objective credit; Temple, Moldwood, Lifeweaver Terrace, flame-miniboss, and random-path Spiritmother Selene communicator behavior are WIP-guessed and covered by focused tests. Exact miniboss combat mechanics, route weights/objective availability, trigger placement, door choreography, communicator timing, and manual dungeon smoke remain blocked. |
| Datascape | Caretaker111..114 callouts, Avatus-spawn completion-only cinematic placeholder, and Optimized Memory Probe TX-67 objective credit are WIP-guessed and covered by focused tests. Hydroflux/Mnemesis mechanics, communicator/cinematic timing, real cinematic payload, encounter choreography, door/trigger placement, exact wing-order retail proof, challenge mechanics, and manual raid smoke remain blocked. |
| Deep Space Exploration | The branch single phase/objective pairing is implemented as a WIP-guessed first visible objective, and the on-create cinematic hook queues an immediate completion-only placeholder. Follow-up expedition routing, doors, real cinematic payload, encounter order, rewards, and manual smoke remain blocked. |
| Rage Logic | The branch `ChooseAVehicle` phase is now set as WIP-guessed event behavior. Vehicle choice, objective routing, rewards, and encounter behavior remain blocked. |
| Ultimate Protogames dungeon | The branch `Welcome` / `InitiateUltimateProtogames` entry pairing is implemented as WIP-guessed behavior and advances only to the coarse `RandomEvent1` gate. Room randomization, objective routing, boss mechanics, rewards, and manual smoke remain blocked. |
| Ultimate Protogames raid | The branch Downsizer objective catalog is implemented as WIP-guessed boss-objective behavior. Exact challenge semantics, boss mechanics, rewards, and manual smoke remain blocked. |
| Protostar SuperMall in the Sky | The branch greeter gather objective is implemented as WIP-guessed entry behavior and advances only to the coarse `RandomPath` gate. The on-create cinematic hook queues an immediate completion-only placeholder; store routing, encounter logic, real cinematic payload, rewards, and manual smoke remain blocked. |
| Journey into OMNICore-1 | The branch on-create hook now queues a WIP-guessed immediate completion-only cinematic placeholder. The real actor/camera/text/timing payload, event routing, rewards, and encounter logic remain blocked. |

## Branch Files Intentionally Not Ported

| Branch file or slice | Reason |
| --- | --- |
| `Dungeon/RuinsOfKelVoreth/Script/ForgemasterMessageTriggerScript.cs` | Blocked. It uses the same `ScriptFilterOwnerId(446)` as the active Drokk trigger and has a branch bug where the Toric lookup is never sent. The active owner `446` script is documented as WIP-guessed until trigger-row/timing evidence proves whether this owner should emit Drokk, Forgemaster, or split callouts. |
| Branch-only `PublicEventCreature`, `CommunicatorMessage`, `PublicEventObjective`, and `PublicEventPhase` catalogs without current consumers | Not runtime behavior by themselves. They stay unported unless an active event script, trigger, SQL hook, table import, or test consumes the ids. |
| Remaining Journey into OMNICore-1 event-routing catalog stub | Blocked from runtime routing. The branch supplies only the map hook and abstract cinematic interface; no event script or concrete cinematic payload proves transitions, objective activation order, rewards, or encounter behavior. The current hook only queues a WIP-guessed immediate completion placeholder. |
| Door/platform/marker-only `PublicEventCreature` catalogs for Protogames Academy, Ruins of Kel Voreth, Skullcano, Fragment Zero, Infestation, Shade's Eve, and Initialization Core Y-83 | Left unported where the corresponding door/platform/cleanup behavior is still blocked. Several ids are explicitly marked by the branch as guessed, placeholder, or needing real trigger/door proof, so adding the enum without an active, tested consumer would not improve runtime behavior. |
| Initialization Core Y-83 zero-coordinate SQL entities | Left unported from the instance seed. The branch raid SQL supplies Freebot/Drillbot/Prime/Datacube rows at `(0, 0, 0)` with no `entity_event` or `entity_script` rows and uncertain comments (`left?`, `right???????`, `mid?`), so adding deterministic runtime rows would not improve the current WIP event chain. |
| Empty branch catalogs for War of the Wilds, Gauntlet, Outpost M-13, Space Madness communicators, Datascape creatures, Genetic Archives creatures, Red Moon Terror creatures, and several shared arena/adventure creature enums | Rejected as no-op code. Empty enums or unused id bags do not implement content and would only create stale-looking surfaces. |
| Shared branch `ArenaEventScript` / `ArenaSubEventScript` base refactor | Superseded by the current arena-specific Slaughterdome/Cryo-Plex patterns. The useful Cryo-Plex ids were ported without adopting the branch-wide base rewrite. |
| Renamed/typo-only files such as branch `CentralAccessCorridorTrigger.cs`, `FlameMinibossMessageTrigger.cs`, and Sanctuary trigger names | Superseded by current style names with the same active owner behavior and WIP comments. The branch filenames are not kept when the runtime script was ported under the repo's naming conventions. |

## Superseded Or Rejected

| Branch slice | Decision |
| --- | --- |
| Raw trigger/door/cinematic guesses | Generally kept blocked when the branch itself lacked evidence or marked ids/coordinates as guesses. Narrow trigger objective/message behavior is only ported when scoped, tested, and explicitly marked WIP-guessed in code. |
| Direct local teleport shortcuts | Generally omitted unless backed by stronger current table/client evidence. Evil from the Ether medbay, upper-deck, and escape teleports are retained only as WIP-guessed branch behavior with focused tests and blockers. |
| Branch communicator spam/timing | Omitted for most ports unless the current content path had enough evidence and safe runtime dependencies. Exact timings remain blocked. |
| Branch-only creature/communicator enum catalogs | Left unported unless a current event script or WIP-guessed trigger uses them. Id catalogs alone do not create runtime behavior or prove encounter parity. |
| Event creature/boss choreography | Not treated as retail behavior from branch scaffolds alone. Boss scripts require stronger client/table/sniff/manual-smoke evidence. |
| Ultimate Protogames deeper enum-only content | Kept out of runtime event routing beyond the dungeon entry gate because ids without room transitions are not enough proof. |
| Deep Space/SuperMall/OMNICore/Fragment Zero/Infestation/Space Madness/Shade's Eve abstract cinematic hooks | Active only as WIP-guessed immediate completion placeholders because the branch lacks actor/camera/text/timing data for these hooks. |
| Datascape Warmonger Chuna transition | Branch code incorrectly activated a phase enum as an objective. The port fixes this to a phase transition rather than copying the bug. |
| Sanctuary spirit relic transition | Branch code mixed objective and phase enum types. The port fixes the transition to the mapped `PlaceSpiritRelics` phase. |
| Gauntlet `Gautlet` path typo | Corrected to the current `Expedition\Gauntlet` path; the typo path is not kept as a valid script location. |

## Still Blocked

- Manual client smoke for every branch-derived instance, dungeon, raid,
  expedition, adventure, arena, battleground, and event-instance flow.
- Retail proof for trigger coordinates, door ids, phase timing, entity cleanup,
  communicator timing, cinematics, optional objective randomization, and boss
  choreography.
- Public-event vote, scoreboard, objective notification, and reward semantics.
- Unused branch-only `PublicEventCreature`, `CommunicatorMessage`,
  `PublicEventObjective`, and `PublicEventPhase` catalogs until active,
  evidence-backed runtime consumers need them. `BranchCatalogCleanupTests` now
  guards the known rejected all-in-one Datascape Hydroflux/Mnemesis script names
  plus representative empty or door/platform/marker-only catalog files across
  adventure, dungeon, expedition, event-instance, and raid content so they do
  not quietly enter runtime source without consumer proof.
- Exact route/randomization weights and choreography for Skullcano, Sanctuary of
  the Swordmaiden, Datascape, Genetic Archives, and other branch-derived paths.
- Journey into OMNICore event routing and real cinematic payloads until concrete
  cinematic/runtime evidence are found, Rage Logic vehicle-choice/objective
  routing, plus deeper Deep Space, Ultimate Protogames dungeon/raid, and
  SuperMall routing beyond the WIP entry/boss/phase/cinematic-hook gates.
