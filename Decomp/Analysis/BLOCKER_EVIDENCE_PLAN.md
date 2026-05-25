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
.\Decomp\Analysis\Start-BlockerEvidenceHarness.ps1 -ClientDirectory "I:\WildStar" -PromptForRootPassword
```

Or invoke the setup script directly:

```powershell
.\Tools\Setup\Restart-NexusForeverAuthWorldLocal.ps1 -ClientDirectory "I:\WildStar" -EnableClientConsole -LogLevel Trace -PromptForRootPassword
```

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

## Test and close criteria

For each accepted bundle, add focused tests before implementation: handler branch, packet shape, persistence reload, or artifact parser test as appropriate. Then run the narrow owning tests, falling back to:

```powershell
dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj -v minimal --nologo
dotnet build Source\NexusForever.WorldServer\NexusForever.WorldServer.csproj --no-restore -v minimal --nologo
```

Related trackers: `MATCHING_IMPLEMENTATION_STATUS.md`, `GAMEPLAY_ECONOMY_SOCIAL_STATUS.md`, `MISSING_FEATURE_MATRIX.md`, `CURRENT_STATUS.md`, `CONTINUATION_GUIDE.md`.
