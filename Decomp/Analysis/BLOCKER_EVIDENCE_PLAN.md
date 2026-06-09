# Evidence Plan To Close Remaining NexusForever Blockers

Status date: 2026-06-05 (aligned with current F-009/F-016 live-log follow-up)

Use `I:\WildStar` build 16042, GM accounts, Trace server logs, client screenshots/video, and existing evidence collectors to turn each blocker into one of three states: **implementable now**, **diagnostic-only**, or **still retail/native blocked**. Server logs are first-class evidence, but they only prove current emulator/client interaction; retail producer semantics still need native/decompile, packet captures, or old retail logs.

External cross-checks confirm only project/client context, not blocker semantics: [NexusForever GitHub](https://github.com/NexusForever/NexusForever), [NexusForever installation wiki mirror](https://github-wiki-see.page/m/NexusForever/NexusForever/wiki/Installation), [WildStar slash command archive](https://wildstaronline-archive.fandom.com/wiki/WildStar_Slash_commands).

## Blocker disposition (code + trackers)

| ID | Emulator-parity implemented | Diagnostic-only | Still blocked |
| --- | --- | --- | --- |
| F-008 Crafting | Fixed-recipe craft, station/tradeskill mismatch rejection including observed all-tradeskills station sentinels, additive zero-station rejection, LootId delivery, durable rune item bridge (`item.runeSlots`, `ItemRuneSlotsCodec`, `ItemRuneNetworkWire`), table-backed Tech Tree reward grants, removal of the legacy 10-talent-point learn/reset seed, packet-shape pins; C2S rune install mapped to `0x085B` | Service-key numeric logging, complex `CraftStats`/`ChargeCounts`, `0x084B`/`0x0855`/`0x056C` wire models, and the observed `480/450` Crafting Result progress display until `ServerProfessionUpdate(0x0860)` order/payload evidence is captured | Discovery rolls/unlock mutation, service-key `0x2C`/`0x4F`/`0x57` names, current-craft cadence, `0x084B`/`0x0855` emit intent, non-success sigil precision, `ServerItemMicrochips` (`0x056C`) producer timing, and profession-update display ordering |
| F-009 Transport | `TaxiRoute.tbl` runtime loading, clean route/node/location table rejection, rapid transport route resolve, credit/cooldown rejection branches, captured table-backed rapid route debit/cast context, RapidTransport prerequisite type 269 using client taxi-node cast context, and contiguous flight-path purchase charge/teleport validation | — | Service-token bypass, global route state, taxi embark/completion, broader charge/teleport parity, passenger/seat modes, deployable vehicles |
| F-010 Group/matching | Replacement `0x05D5`/`0x0602` validation+logging, zero `ServerRaidQueueStatus` with raid-info plus non-zero wire-order guard, flexible roles, deserter persistence, `Client0x062A`/`Client0x0634` log-only handler guards, `ServerMatching0x05CF` raw `uint32` shape plus correlated apply candidate | `Client0x062A`/`Client0x0634` value logging only until native sender/live UI context is proven; `ServerMatching0x05CF` remains diagnostic/mapped-only until the apply-table index or live payload context is proven; `ServerRaidQueueStatus` row fields are mapped through `0x071A`/`Group_DispatchRaidInfoResponse`, but the standalone non-zero `0x0718` producer/timing remains blocked | Replacement backfill/merge, non-zero raid queue semantics, `0x05CF` field/apply semantics, `Client0x062A`/`Client0x0634` sender intent |
| F-011 Guild | Holomark update handler + guild manager paths exist | — | Bank economy, perks, recruitment parity, warplot semantics |
| F-012 ICComm | Join/message validation, transient membership | — | Entitlement gating, persistent-channel restore |
| F-015 Duel/PvP | Duel leash/cancel-warning/disconnect and PvP toggle-off cooldown persistence are implemented/test-pinned | — | Observer/reward parity |
| F-016..F-020 Spells | Per-family fixtures where captured; hostile damage effects use caster-side attack permission so captured Pulse Blast-style damage can flow to combat logs | `!spell procunsupported` etc. | Uncaptured effect families |
| F-021 LAS | Preflight checks where mapped | — | `UpdateSpellInProgress`, async transaction semantics |
| F-031 Fortune | Coin cost, emulator rarity-tier pool, UI probability transport, `account_fortune_session` persistence code path | Local catalog/table audit only (`FORTUNE_WEIGHT_AUDIT.md`); F-007 `RewardRotation*` schedules rejected as Fortune active-rotation evidence; no live Fortune play artifact found | Per-item retail Madame Fay weights and active rotation catalog |

A blocker is **closed** only when the evidence bundle, implementation, tests, and tracker note agree. Anything proven only by current emulator logs but not by retail/native producer semantics is **emulator-parity implemented**, not **retail-parity proven**.

2026-06-09 live F-008 station/craft proof: bundle `artifacts\blocker_evidence\20260609-212659-F008-crafting-live` plus packet evidence `artifacts\packet_evidence\F008-crafting-live\20260609-201741-packet-evidence.jsonl` closed the generic-station fixed-recipe smoke for emulator parity. Creature `21793` uses activate spell `1817` with prerequisite `1050` (`IsPlayer == 0`), so activate-spell prerequisites must evaluate against the activated unit target. The live station unit `354` exposed `Creature2.TradeSkillIdStation = 4294967295`, proving the all-tradeskills sentinel can arrive as `uint.MaxValue`; older SQL/reference checks also showed `2147483647`, so validation accepts both sentinels. The client sent `ClientCraftingCraftItemAutoCraft(0x0852)` for the UI `Simple Craft` button, not `ClientCraftingSimpleCraft`; after the sentinel fix, contexts `47..49` completed for schematic `270` at station `354`, emitted `ServerSupplySatchelUpdate(0x0199)` and `ServerCraftingFinish(0x0853)`, and produced item `14838` with persisted stack `12` after three crafts while material `11` dropped to `28`. Treat the repeated `TradeSkillProfession` prerequisite warnings during UI refresh as a separate prerequisite-coverage issue, not an auto-craft blocker.

2026-06-09 live F-008 Tech Tree/talent proof: the observed `+40 Technologist XP (480/450)` result did not indicate a lost XP write. Character `30` persisted `tradeskillXp = 480`, and tier achievement `1481` completed after crossing the tier-2 `450` threshold. The remaining `480/450` display issue is now blocked on packet/order evidence because the packet evidence filter omitted `ServerProfessionUpdate(0x0860)`. The real server-state bug was that completed `TradeskillAchievementReward` rows were not applied: achievement `1470` reward `599` grants one Technologist talent point, and achievement `1471` reward `162` grants one talent point plus schematic `2954` (`Spirovine Extract`). Runtime now applies those table-backed rewards idempotently, backfills already-completed rewards on login/learn, and removes the old simplified 10-talent-point learn/reset seed. A live retry proved the final dependency gap: `TradeskillAchievementReward.tbl` existed in runtime assets but was not marked `[GameData]`, so WorldServer never loaded it and the reward pass saw zero rows. `TradeskillAchievementReward` is now runtime-required game data and the contract tests pin it; focused reward/table verification passed 47/47 and WorldServer build passed. After reconnect/save, character `30` persisted Technologist `talentPoints = 2` and schematic `2954` with XP still `480`. Capture `0x0860` before changing profession-update ordering or Crafting Result progress display behavior.

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

2026-06-05 evidence review:

- `artifacts\verify\spell-evidence\20260605-113719317-spell4-82922...json` shows Rapid Transport `82922` now reaches `CastResult=Ok` with `RapidTransport` and `Fluff` effects after the type `269` prerequisite fix. The older `20260605-101900744...` artifact is the pre-fix `PrereqCasterCast` comparison.
- `artifacts\verify\spell-evidence\20260605-113837894-spell4-37302...json` shows Pulse Blast from creature `335` hitting player `295` with three `CombatLogDamage` rows and shield deltas, while the non-hostile/simple target is ignored. A player-originated hostile-target proof is still needed.
- `artifacts\verify\spell-evidence\20260605-113920634-spell4-4046...json` plus `artifacts\verify\proc-evidence\20260605-113928580...json` show Brutal registering a supported proc: trigger event `12` (`deal-damage`), trigger spell `4047`, chance `0.15`, targetData `4`, route `counterpart`. The report only captured registration; a real post-registration damage trigger is still needed.
- `artifacts\verify\reward-evidence` still contains empty/manual reward reports from the earlier pass, but the latest world log proves client refresh requests for indices `1-6` emitted content-context packets and index `0` emitted empty schedule/state placeholders. Two later command attempts failed as `0!reward`, so run reward commands one at a time and wait for each result.
- `artifacts\verify\account-evidence\20260605-114239710...json` confirms the coupon path remains intentionally blocked as `coupon-request-unmapped`, with `ServerAccountOperationResult InvalidCoupon`.
- `artifacts\verify\loot-evidence` is empty. The latest world log has 15 Battle Beast loot generations, 4 immediate Omnibit grants, and no runtime loot artifact because the corpses expired before `ServerLootNotify`. Use a loot bag or a known visible-loot source for the next loot capture.
- The invisible Battle Beast respawn/client-state bug was reproduced in `NexusForever.WorldServer_20260605_25700.log` with 40k+ repeated “source unit ... is not visible” aggro skips after reused creature GUID respawns. The fix now sends loot remove plus destroy/create on non-player respawn; the next live pass should verify visible respawns and no persistent loot shower.
- Realm Bank now reaches runtime code. The local DB had skipped the hand-written
  `20260522180000_RealmBankItems` migration because the migration class lacked
  EF metadata; the migration is now discoverable and the local table can be
  created through the character migration path before re-running Realm Bank
  interaction proof.

Round 2 follow-up commands after the 2026-06-05 evidence review:

```text
# Run one chat command at a time, and wait for the command response/log line before the next.
!help entitlement
!help spell
!help loot
!help reward
!help account
!character level 50
!currency character add Credits 10000000
!currency account add ServiceToken 1000
!character save

# RealmBank: run only after the character DB has the realm_bank_item table.
!teleport name Thayd
!entitlement account list
!entitlement add SharedRealmBankUnlock 0
!entitlement add SharedRealmBankSlots 0
!character save
# Interact with the Realm Bank NPC/terminal; capture the locked/no-crash result in world logs.
!entitlement add SharedRealmBankUnlock 1
!entitlement add SharedRealmBankSlots 1
!character save
# Interact again; capture open/sync behavior and any item move rejection/success logs.

# RapidTransport: prove spell 82922 no longer fails PrereqCasterCast for nodes 88/89.
!teleport name Thayd
!spell inspect4 82922
!spell capturenext
# Use the rapid transport UI for node 88; inspect artifacts\verify\spell-evidence.
!spell capturenext
# Use the rapid transport UI for node 89; inspect artifacts\verify\spell-evidence.

# Pulse Blast/damage: prove raw/adjusted damage, health/shield deltas, and combat logs are emitted.
!teleport location 51739
!spell inspect4 37302
!spell capture4 37302
!spell capturenext
# Cast Pulse Blast from the client against a hostile target, then inspect artifacts\verify\spell-evidence.
!spell inspect4 4046
!spell capture4 4046
# Deal damage with Brutal active before exporting proc evidence.
!spell procstates
!spell procreport
!spell procunsupported

# Loot: Battle Beasts only produced immediate Omnibit grants; use a visible-loot source.
!item add 84623 1 1
!loot capturenext
# Right-click/use the loot bag item once, then inspect artifacts\verify\loot-evidence.
!loot capturenext
# Kill one loot-bearing creature or force one visible loot notify, then inspect artifacts\verify\loot-evidence.

# Reward/coupon: evidence-only until live schedule/state/coupon policy is proven.
!reward capturenext
# Open the reward/storefront UI once; inspect artifacts\verify\reward-evidence.
!reward report 0
!reward context 0
!reward report 1
!reward context 1
!reward report 2
!reward context 2
!account couponblockers
```

An evidence bundle is accepted only when it has: exact command/action transcript, matching server log lines with player/request ids, client-visible result, negative/rejection case, and JSON artifact where a collector exists.

For opcode-level packet passes, prefer server-decrypted evidence over pcap-only
interpretation. Start the target server with:

```powershell
$env:NEXUSFOREVER_PACKET_EVIDENCE = '1'
$env:NEXUSFOREVER_PACKET_EVIDENCE_DIR = 'I:\GIT\NexusForever\artifacts\packet_evidence\<pass-name>\server-decrypted'
$env:NEXUSFOREVER_PACKET_EVIDENCE_OPCODES = '0x0186,0x0187,0x0188,0x00CA,0x01A7,0x01A8,0x056B,0x056C,0x056D'
```

The recorder writes plaintext opcode/body JSONL records before outbound
encryption and after inbound decryption. Pair it with Wireshark/dumpcap on the
local ports for timing and TCP correlation, but use the server-decrypted JSONL
as the opcode source of truth.

For F-009 transport passes, include the client transport opcodes in the recorder
filter too:

```powershell
$env:NEXUSFOREVER_PACKET_EVIDENCE_OPCODES = '0x00FF,0x0141,0x0186,0x0187,0x0188'
```

## Blocker passes

**F-010 Group/Raid/Matching: implementable for empty/status diagnostics, still blocked for non-zero queue semantics.**
Open raid and queue UI, request raid info, start/stop replacement search if an in-progress match can be created. Capture logs for `ClientRaidInfoRequest`, `ServerRaidQueueStatus`, `ServerMatching0x05CF` if it appears, `ClientMatchingMatchInitiateLookingForReplacements`, `ClientMatchingStopLookingForReplacements`, and diagnostics `Client0x062A`/`Client0x0634`. Implement only observed empty-status/validation behavior; keep non-zero queue position, `0x05CF` field semantics, and backfill merge lifecycle blocked until a live non-zero payload or client consumer proves meaning.

**F-009 Rapid Transport/Flight: implement captured route behavior, keep global route-state/service-token blocked.**
At city hubs, run `!spell inspect4 82922`, then `!spell capturenext` before using the rapid transport/taxi/flight UI for nodes 88 and 89. Inspect `artifacts\verify\spell-evidence` and world logs for taxi node, context token, route id, source/destination, price, credit debit, spell id, and accept/reject reason. The 2026-06-05 fix maps prerequisite type 269 to the client taxi-node cast context; the next successful proof should no longer end with `CastResult=PrereqCasterCast` for the captured nodes. Implement route(s) and rejection branches proven by logs; do not infer route unlock tables, global route state, or service-token bypass.

**F-008 Crafting/Rune discovery: station request semantics and durable rune bridge closed; keep discovery, service-key names, and aux emit blocked.**
Station request branches are covered by native sender evidence and focused tests: simple/complex/autocraft can carry zero station ids, while additive requires a non-zero station before `0x084A`. The durable rune item bridge is implemented through `item.runeSlots`, `ItemRuneSlotsCodec`, `ItemRuneNetworkWire`, `RandomGlyphData`/`Glyphs`, and migration `20260531224115_ItemMicrochipIdsAndRuneSlots`, with focused rune tests covering socket/install round trips. Current evidence rejects a distinct client microchip-install mutator: client rune install is `0x085B` (`RuneCrafting_SendClientRuneInstall`), while `ServerItemMicrochips` (`0x056C`) and `Inventory_UpdateItemMicrochipsFromWire` are server-side item patch/update evidence with blocked producer timing. For the next live pass, teleport to crafting hubs, use station UI, `!item lookup <name> 25`, `!item add <itemId> <qty>`, then craft complex/random outputs and failed cases. Capture logs for station unit id, schematic id, material ids, craft type, completion/rejection reason, hot/cold UI state, and any current-craft/finish packets. Implement only behavior proven by logs or producer decomp. Discovery roll thresholds/unlock mutation, `CraftStats`/`ChargeCounts` semantics beyond diagnostics, service-key names `0x2C`/`0x4F`/`0x57`, current-craft cadence, `0x084B`/`0x0855` emit intent, non-success sigil precision, and `0x056C` server-side microchip patch timing remain blocked without producer/native/live evidence.

**F-011 Guild bank/perks/holomarks: implement observable persistence/logged packet handling only.**
Client 1: `!guild register Guild NFEvidence`; client 2: `!guild join Guild NFEvidence`. Exercise holomark changes, guild bank tab open, item/money deposits, recruitment subscribe/details, and reload after `!character save`. Use logs for bank/recruitment handlers and screenshots before/after reload. Implement holomark persistence or logged response-shape fixes if proven; keep bank economy, perk effects, recruitment parity, and warplot semantics blocked.

**F-012 ICComm: implement transient routing/cleanup where proven, keep entitlement/persistent channels blocked.**
Use two clients in same hub and same guild/circle/community. Test join, leave/logout, direct message, guild channel message, invalid channel, and non-member send. Capture `ClientICComm*` logs plus both client screens. Test `!entitlement account list` and only use `!entitlement add <ExactEntitlementType> <value>` if help/logs identify the enum. Entitlement gating and persistent-channel restore remain blocked unless toggles produce reproducible client-visible differences.

**F-015 Duel/PvP observer/reward: close lifecycle smoke, keep observer/reward parity blocked.**
Two clients: `!teleport name Thayd`, initiate duel, accept, decline, forfeit, toggle PvP, then move one player beyond the 120f leash with `!teleport coordinates` and wait 15 seconds. Capture challenge/countdown/leash/cancel-warning logs and client UI. Implement lifecycle/cooldown fixes only if mismatched; observer broadcasts, reward parity, and retail stat behavior remain blocked.

**F-016-F-020 Spell families/procs/loot/reward: implement one fixture per captured spell family.**
For each target spell: `!spell inspect4 <spell4Id>`, `!spell capture4 <spell4Id>`, `!spell diag4 <spell4Id>`, or `!spell capturenext` before a real client cast. For the current Pulse Blast proof, use `!spell inspect4 37302`, `!spell capture4 37302`, then `!spell capturenext` before a real client Pulse Blast cast against a hostile target; the accepted artifact must show damage descriptions, raw/adjusted damage, health or shield deltas, and `CombatLogDamage` rows. Follow with proc proof commands for the observed proc spell (`!spell inspect4 4046`, `!spell capture4 4046`, `!spell procstates`, `!spell procreport`, `!spell procunsupported`). For combat/loot: `!teleport location 51739`, `!teleport location 51740`, or `!teleport location 53015`; use `!loot capturenext`, `!reward capturenext`, `!spell procstates`, `!spell procreport`, `!spell procunsupported`. Implement only captured spell/effect/proc fixtures with tests; leave uncaptured effect families blocked.

**F-021 Action Set/LAS: implement preflight/result mapping only if UI plus logs prove it.**
Use `!spell add <spell4BaseId> [tier]`, `!spell resetcooldown`, then change LAS slots, tiers, spec, and AMP state in normal, dead, combat, and PvP states. Capture UI result and any server logs. `UpdateSpellInProgress` and async transaction semantics remain blocked unless a real in-progress transition is observed.

**F-031 Madame Fay/Fortune: local tests validate session/payout boundaries and UI probability transport; per-item retail weights remain blocked.**
Use Fortune UI with enough `FortuneCoin`/account currency, capture logs and screenshots for cost/result/session behavior, reload persistence (`account_fortune_session`), and displayed `fProbability` values. Emulator now sends `ServerFortuneRewards.RewardItemProbabilities` from rarity-tier weights; `FORTUNE_WEIGHT_AUDIT.md` confirms `AccountItem.tbl` has no weight column, F-007 `RewardRotation*` schedules are rejected as Fortune active-rotation evidence, and no local live Fortune play artifact was found. Exact per-account-item retail weights still need retail `ServerFortuneRewards` capture or storefront-server catalog evidence.

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
