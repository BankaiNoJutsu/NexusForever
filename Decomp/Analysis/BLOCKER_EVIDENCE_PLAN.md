# Evidence Plan To Close Remaining NexusForever Blockers

Status date: 2026-05-25 (aligned with `CURRENT_STATUS.md` F-031 audit pass)

Use `I:\WildStar` build 16042, GM accounts, Trace server logs, client screenshots/video, and existing evidence collectors to turn each blocker into one of three states: **implementable now**, **diagnostic-only**, or **still retail/native blocked**. Server logs are first-class evidence, but they only prove current emulator/client interaction; retail producer semantics still need native/decompile, packet captures, or old retail logs.

External cross-checks confirm only project/client context, not blocker semantics: [NexusForever GitHub](https://github.com/NexusForever/NexusForever), [NexusForever installation wiki mirror](https://github-wiki-see.page/m/NexusForever/NexusForever/wiki/Installation), [WildStar slash command archive](https://wildstaronline-archive.fandom.com/wiki/WildStar_Slash_commands).

## Blocker disposition (code + trackers)

| ID | Emulator-parity implemented | Diagnostic-only | Still blocked |
| --- | --- | --- | --- |
| F-008 Crafting | Fixed-recipe craft, station/tradeskill mismatch rejection, additive zero-station rejection, LootId delivery, packet-shape pins | — | Discovery rolls, service-key `0x2C/0x4F/0x57` meanings, durable rune DB bridge, `0x084B`/`0x0855` emit |
| F-009 Transport | Rapid transport route resolve, credit/cooldown rejection branches, flight-path purchase validation | — | Service-token bypass, global route state, taxi embark/completion, deployable vehicles |
| F-010 Group/matching | Replacement `0x05D5`/`0x0602` validation+logging, zero `ServerRaidQueueStatus` with raid-info, flexible roles, deserter persistence | `Client0x062A`/`Client0x0634` value logging | Replacement backfill/merge, non-zero raid queue semantics |
| F-011 Guild | Holomark update handler + guild manager paths exist | — | Bank economy, perks, recruitment parity, warplot semantics |
| F-012 ICComm | Join/message validation, transient membership | — | Entitlement gating, persistent-channel restore |
| F-015 Duel/PvP | Duel leash/cancel-warning/disconnect; PvP cooldown emit on flag off | — | Observer/reward parity |
| F-016..F-020 Spells | Per-family fixtures where captured | `!spell procunsupported` etc. | Uncaptured effect families |
| F-021 LAS | Preflight checks where mapped | — | `UpdateSpellInProgress`, async transaction semantics |
| F-031 Fortune | Coin cost, emulator rarity-tier pool, UI probability transport, `account_fortune_session` persistence code path | Local catalog/table audit only (`FORTUNE_WEIGHT_AUDIT.md`); no live Fortune play artifact found | Per-item retail Madame Fay weights and active rotation catalog |

A blocker is **closed** only when the evidence bundle, implementation, tests, and tracker note agree. Anything proven only by current emulator logs but not by retail/native producer semantics is **emulator-parity implemented**, not **retail-parity proven**.

## Evidence harness

Start Trace logging and the client console:

```powershell
.\Decomp\Analysis\Start-BlockerEvidenceHarness.ps1 `
  -Lws036ChecklistSmoke `
  -BundleName "LWS-036-starter-zone-checklist-smoke" `
  -ClientDirectory "I:\WildStar" `
  -WorldIds 426,990 `
  -ObjectiveIds 4888,4889,4890 `
  -NegativeCases "missing quest objective", "wrong world/objective" `
  -PromptForRootPassword
```

The harness creates `artifacts\blocker_evidence\<timestamp>-<bundle>` before
launching servers/client. Each bundle includes `manifest.json`, command
transcript, server-log notes, client-observation notes, negative-case notes,
screenshots/video/log folders, and helper scripts to tail or collect logs. Use
`-CreateBundleOnly` to create the bundle skeleton without touching running
services. `-Lws036ChecklistSmoke` adds `lws-036-targets.md`, preloads the
Illium/Thayd/Northern-Wilds/small-world target worlds when no explicit
`-WorldIds` are supplied, and fills the required negative cases for the
LaughingWS city/checklist smoke gate.
`-NewZoneAssetProof` adds `new-zone-asset-proof-targets.md`, preloads the
Dreadmoor/Halon/Murkmire world ids when no explicit `-WorldIds` are supplied,
and records the required negative/safety cases for LWS-040 through LWS-043.
`test_blocker_evidence_harness_presets.py` dry-runs `-Lws036ChecklistSmoke`
and `-NewZoneAssetProof` with `-CreateBundleOnly` and verifies their manifests,
worksheets, default world ids, helper files, and negative-case scaffolds; this
guards the capture workflow only and does not prove any row safe to promote.
`-DustStalkerQ4516Smoke` adds `lws-051-dust-stalker-targets.md`, preloads world
`1138` and objectives `7633`, `6189`, `7637`, `7638`, and `7639` when no
explicit ids are supplied, and records the required Boss Xagg, bridge-control,
exit-panel, timer-expiry, and repeat-interaction negative cases for LWS-051.
`-ArcterraPalaverSourceSmoke` adds `lws-052-arcterra-palaver-targets.md`,
preloads worlds `3335` and `3519` plus the known Palaver objective ids, and
records the required Caretaker prerequisite, Coldblood portal, Palaver
wrong-state, and repeat-interaction negative cases for LWS-052.
`-SkyplotHousingSmoke` adds `lws-053-skyplot-housing-targets.md`, preloads world
`1229`, and records the required return-pad, vendor-before-unlock,
invalid-catalog-row, and inactive-lifecycle negative cases for LWS-053.
`-LiveEventSmoke` adds `lws-054-live-event-targets.md` and records inactive
event, event-ending, post-cleanup, and skipped-source-row negative cases for
the Battle Chase, Dungeon Chase, Shades Eve, Space Chase, Starfall, Sim Chase 1,
and zPrix LWS-054 proof passes.
`-QuestVirtualLootSmoke` adds `lws-055-quest-virtual-loot-targets.md`, preloads
representative objectives `13011`, `12605`, `6700`, `6313`, and `21278`, and
records inactive-objective, pre-activation kill, completed-objective repeat, and
declined/abandoned-loot negative cases for LWS-055.
`-PublicEventVoteScoreboardSmoke` adds
`lws-071-072-public-event-vote-scoreboard-targets.md` and records
non-participant/duplicate/late vote, non-member scoreboard request, and
scoreboard unsubscribe negative cases for LWS-071 and LWS-072 vote/scoreboard
proof passes.
`-PublicEventObjectiveNotificationSmoke` adds
`lws-073-public-event-objective-notification-targets.md` and records
non-audience objective packets, duplicate status transition, premature
quest-share/text, and post-completion notification-mode negative cases for the
LWS-073 objective notification proof pass.
`-PvpAdventureSmoke` adds `lws-080-085-pvp-adventure-targets.md`, preloads
worlds `797`, `1393`, `1627`, `2166`, `3022`, and `3449`, preloads public
events `158`, `170`, `171`, `213`, `217`, `366`, `438`, `466`, `581`, `582`,
`876`, and `877`, and records queue/match, disconnect/leave, premature reward,
wrong-team/wrong-phase, and early-finish negative cases for the PvP/adventure
proof passes.
`-ExpeditionSmoke` adds `lws-090-096-expedition-targets.md`, preloads worlds
`1232`, `1319`, `2149`, `2183`, `2188`, `3180`, and `3404`, preloads public
events `95`, `108`, `390`, `446`, `447`, `680`, and `781`, and records
zero-vector/placeholder trigger, premature door/shuttle/teleport, wrong-phase
trigger, completion-only cinematic, and early-finish reward negative cases for
the expedition proof passes.
`-DungeonSmoke` adds `lws-100-106-dungeon-targets.md`, preloads worlds `382`,
`1263`, `1271`, `1336`, `2980`, `3173`, and `3522`, preloads public events
`145`, `148`, `161`, `166`, `594`, `667`, and `907`, and records premature
trigger, premature door/platform/launcher/teleporter, wrong-route optional
objective, completion-only cinematic, and early-finish reward negative cases
for the dungeon proof passes.
`-RaidEventSmoke` adds `lws-110-117-raid-event-targets.md`, preloads worlds
`1333`, `1462`, `3032`, `3040`, `3041`, `3044`, `3045`, and `3094`, preloads
public events `157`, `159`, `595`, `597`, `605`, `642`, `679`, and `705`, and
records premature trigger/target-set, premature door/elevator/gate/room
interaction, wrong-route weekly/wing/room, completion-only cinematic, and
early-finish reward negative cases for the raid/event-instance proof passes.

Or invoke the setup script directly:

```powershell
.\Tools\Setup\Restart-NexusForeverLocal.ps1 `
  -ClientDirectory "I:\WildStar" `
  -EnableClientConsole `
  -EnableClientLogging `
  -LogLevel Trace `
  -PromptForRootPassword
```

`-LogLevel` configures NexusForever server NLog output only. `-EnableClientLogging`
enables retail `CLog` file output under `<WildStar>\Logs`. See
`Decomp/Analysis/CLIENT_LOGGING.md` for switch details and tail commands.

The retail client toggles that console with `Alt` plus virtual key `0xC0`.
On a typical US layout that is backtick; on a Swiss layout it is the key that
produces `¨`, `!`, or `]` depending on modifiers. If the physical mapping is
unclear, use `.\Tools\Setup\Send-WildStarConsoleToggle.ps1` after the client
window is open.

Tail evidence logs in separate terminals:

```powershell
Get-Content -Wait -Tail 200 .\.nexusforever-runtime\logs\NexusForever_World.stdout.log
Get-Content -Wait -Tail 200 .\Source\NexusForever.WorldServer\bin\Debug\net10.0\logs\NexusForever.WorldServer_*.log
Get-Content -Wait -Tail 200 .\.nexusforever-runtime\logs\NexusForever_Group.stdout.log
Get-Content -Wait -Tail 200 .\.nexusforever-runtime\logs\NexusForever_Friendship.stdout.log
```

Use `gm/gm` as primary and `admin/admin` or `player/player` as a second client. Use chat commands with `!`; if using the raw cheat console path, omit `!`.

Baseline commands for every pass:

```text
!help
!help teleport
!character level 50
!currency character add Credits 10000000
!character save
!teleport name Thayd
!teleport name Illium
!teleport coordinates <x> <y> <z> [worldId]
```

An evidence bundle is accepted only when it has: exact command/action transcript, matching server log lines with player/request ids, client-visible result, negative/rejection case, and JSON artifact where a collector exists.

## Blocker passes

**F-010 Group/Raid/Matching: implementable for empty/status diagnostics, still blocked for non-zero queue semantics.**
Open raid and queue UI, request raid info, start/stop replacement search if an in-progress match can be created. Capture logs for `ClientRaidInfoRequest`, `ServerRaidQueueStatus`, `ClientMatchingMatchInitiateLookingForReplacements`, `ClientMatchingStopLookingForReplacements`, and diagnostics `Client0x062A`/`Client0x0634`. Implement only observed empty-status/validation behavior; keep non-zero queue position/backfill merge lifecycle blocked.

**F-009 Rapid Transport/Flight: implement captured route behavior, keep global route-state/service-token blocked.**
At city hubs, run `!spell capturenext`, use rapid transport/taxi/flight UI, then inspect `artifacts\verify\spell-evidence`. Required logs: taxi node, context token, route id, source/destination, price, credit debit, spell id, accept/reject reason. Implement route(s) and rejection branches proven by logs; do not infer route unlock tables or service-token bypass.

**F-008 Crafting/Rune discovery: station request semantics closed; keep discovery thresholds and service-key names blocked.**
Station request branches are covered by native sender evidence and focused tests: simple/complex/autocraft can carry zero station ids, while additive requires a non-zero station before `0x084A`. For the next pass, teleport to crafting hubs, use station UI, `!item lookup <name> 25`, `!item add <itemId> <qty>`, then craft complex/random outputs and failed cases. Capture logs for station unit id, schematic id, material ids, craft type, completion/rejection reason, hot/cold UI state, and any current-craft/finish packets. Implement only behavior proven by logs or producer decomp. Discovery roll thresholds, `CraftStats`/`ChargeCounts` semantics, service-key names `0x2C/0x4F/0x57`, and durable rune item DB bridge remain blocked without producer/native evidence.

**F-011 Guild bank/perks/holomarks: implement observable persistence/logged packet handling only.**
Client 1: `!guild register Guild NFEvidence`; client 2: `!guild join Guild NFEvidence`. Exercise holomark changes, guild bank tab open, item/money deposits, recruitment subscribe/details, and reload after `!character save`. Use logs for bank/recruitment handlers and screenshots before/after reload. Implement holomark persistence or logged response-shape fixes if proven; keep bank economy, perk effects, recruitment parity, and warplot semantics blocked.

**F-012 ICComm: implement transient routing/cleanup where proven, keep entitlement/persistent channels blocked.**
Use two clients in same hub and same guild/circle/community. Test join, leave/logout, direct message, guild channel message, invalid channel, and non-member send. Capture `ClientICComm*` logs plus both client screens. Test `!entitlement account list` and only use `!entitlement add <ExactEntitlementType> <value>` if help/logs identify the enum. Entitlement gating and persistent-channel restore remain blocked unless toggles produce reproducible client-visible differences.

**F-015 Duel/PvP observer/reward: close lifecycle smoke, keep observer/reward parity blocked.**
Two clients: `!teleport name Thayd`, initiate duel, accept, decline, forfeit, toggle PvP, then move one player beyond the 120f leash with `!teleport coordinates` and wait 15 seconds. Capture challenge/countdown/leash/cancel-warning logs and client UI. Implement lifecycle/cooldown fixes only if mismatched; observer broadcasts, reward parity, and retail stat behavior remain blocked.

**F-016-F-020 Spell families/procs/loot/reward: implement one fixture per captured spell family.**
For each target spell: `!spell inspect4 <spell4Id>`, `!spell capture4 <spell4Id>`, `!spell diag4 <spell4Id>`, or `!spell capturenext` before a real client cast. For combat/loot: `!teleport location 51739`, `!teleport location 51740`, or `!teleport location 53015`; use `!loot capturenext`, `!reward capturenext`, `!spell procstates`, `!spell procreport`, `!spell procunsupported`. Implement only captured spell/effect/proc fixtures with tests; leave uncaptured effect families blocked.

**F-021 Action Set/LAS: implement preflight/result mapping only if UI plus logs prove it.**
Use `!spell add <spell4BaseId> [tier]`, `!spell resetcooldown`, then change LAS slots, tiers, spec, and AMP state in normal, dead, combat, and PvP states. Capture UI result and any server logs. `UpdateSpellInProgress` and async transaction semantics remain blocked unless a real in-progress transition is observed.

**F-031 Madame Fay/Fortune: local tests validate session/payout boundaries and UI probability transport; per-item retail weights remain blocked.**
Use Fortune UI with enough `FortuneCoin`/account currency, capture logs and screenshots for cost/result/session behavior, reload persistence (`account_fortune_session`), and displayed `fProbability` values. Emulator now sends `ServerFortuneRewards.RewardItemProbabilities` from rarity-tier weights; `FORTUNE_WEIGHT_AUDIT.md` confirms `AccountItem.tbl` has no weight column and no local live Fortune play artifact was found. Exact per-account-item retail weights still need retail `ServerFortuneRewards` capture or storefront-server catalog evidence.

Harness shortcut:

```powershell
.\Decomp\Analysis\Start-BlockerEvidenceHarness.ps1 -FortuneRewardsSmoke -PromptForRootPassword
```

This creates the LWS-066 worksheet for `ServerFortuneRewards`, active rotation or
storefront context, card deal/flip/payout, reload persistence, insufficient
currency, invalid flip, and repeated-flip rejection evidence.

## Test and close criteria

For each accepted bundle, add focused tests before implementation: handler branch, packet shape, persistence reload, or artifact parser test as appropriate. Then run the narrow owning tests, falling back to:

```powershell
dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj -v minimal --nologo
dotnet build Source\NexusForever.WorldServer\NexusForever.WorldServer.csproj --no-restore -v minimal --nologo
```

Related trackers: `MATCHING_IMPLEMENTATION_STATUS.md`, `GAMEPLAY_ECONOMY_SOCIAL_STATUS.md`, `MISSING_FEATURE_MATRIX.md`, `CURRENT_STATUS.md`, `CONTINUATION_GUIDE.md`.
