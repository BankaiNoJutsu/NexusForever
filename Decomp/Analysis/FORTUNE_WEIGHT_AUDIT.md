# F-031 Madame Fay Weight Audit

Status date: 2026-06-17 (Fortune retail weights blocked recheck; retail weights still blocked)

## Scope

This audit covers the F-031 Madame Fay per-item retail-weight blocker using
local repository artifacts only. It does not change payout behavior or replace
the current rarity-tier approximation with guessed per-item weights.

## Evidence Inventory

Tracked context:

- `CURRENT_STATUS.md`
- `Decomp/Analysis/MISSING_FEATURE_MATRIX.md`
- `Decomp/Analysis/BLOCKER_EVIDENCE_PLAN.md`
- `Decomp/Analysis/GAMEPLAY_ECONOMY_SOCIAL_STATUS.md`
- `Decomp/Analysis/INITIAL_FINDINGS.md`
- `Decomp/Analysis/function_labels.csv`

Runtime/code/test context:

- `Source/NexusForever.Game/Fortune/FortuneRewardPool.cs`
- `Source/NexusForever.Game/Fortune/FortuneRewardWeights.cs`
- `Source/NexusForever.Game.Abstract/Fortune/IFortuneRewardPool.cs`
- `Source/NexusForever.WorldServer/Network/Message/Handler/Fortune/FortuneSessionManager.cs`
- `Source/NexusForever.Network.World/Message/Model/Fortune/ServerFortuneRewards.cs`
- `Source/NexusForever.Game.Tests/Fortune/*`
- `Source/NexusForever.Database.Auth/Migrations/20260522190000_AccountFortuneSession.cs`

Catalog/table context:

- `.nexusforever-runtime/assets/tbl/AccountItem.tbl`
- `wildstar_client_mysql/AccountItem.tbl.sql`
- `Tools/DataMapping/output/client_source_accountitem_map.csv`
- `wildstar_client_mysql/Item2.tbl.sql`
- `Source/NexusForever.GameTable/Model/AccountItemEntry.cs`

Available live/server artifacts:

- `.nexusforever-runtime/logs/*.log`
- `Source/NexusForever.WorldServer/bin/Debug/net10.0/logs/*.log`
- `artifacts/load-tests`
- `artifacts/verify`

The local logs only contain Fortune opcode/model/handler registration lines.
They do not contain a Fortune UI playthrough, `ClientFortuneStart`, card flip,
payout, screenshot, packet capture, or observed `RewardItemProbabilities` value.

## Confirmed

- `AccountItem.tbl` has exactly 17 fields. The current `AccountItemEntry` model
  has the same 17 fields:
  `Id`, `Flags`, `Item2Id`, `EntitlementId`, `EntitlementCount`,
  `EntitlementScopeEnum`, `InstantEventEnum`, `AccountCurrencyEnum`,
  `AccountCurrencyAmount`, `ButtonIcon`, `PrerequisiteId`,
  `AccountItemCooldownGroupId`, `StoreDisplayInfoId`,
  `StoreIdentifierUpsell`, `Creature2DisplayGroupIdGacha`,
  `EntitlementIdPurchase`, and `GenericUnlockSetId`.
- No `AccountItem.tbl` column represents a per-item chance, weight, rotation
  bucket, active storefront catalog, or Madame Fay season.
- Native labels identify the client UI boundary:
  `Lua_RegisterFortunesLib` (`140766a30`) exposes
  `FortunesLib_GetFortunesLootList` (`140766370`), and that function builds UI
  rows from cached `ServerFortuneRewards` item2 ids plus parallel float
  probabilities. The UI `fProbability` value is `serverFloat * 100`.
- `ServerFortuneRewards_ReadPayload` (`140081f60`, opcode `0x03D2`) reads item
  ids, money rows, and parallel item/money probability float arrays.
- `ServerFortuneCards_ReadPayload` (`1400a0b10`, opcode `0x03D1`) reads the
  operation, three rarity values, three account-item ids, and three flipped
  flags.
- 2026-06-04 Ghidra MCP recheck: `Fortune_ApplyRewards` (`1407292a0`) copies
  server-provided item/money reward arrays plus parallel probabilities into
  Fortune UI state and exposes no active-rotation source. `Fortune_ApplyReset`
  (`1407291f0`) maps the 3-bit `ServerFortuneReset` value `3` to the
  click-empty reset path; NexusForever now names this field `ResetCode`.
- 2026-06-04 false-source cleanup: F-007 `RewardRotation*` table/packet labels
  (`ClientDB_RegisterRewardRotationContent`/`Item`/`Essence`/`Modifier`,
  `Lua_GameLib_BuildRewardRotations`, `RewardRotation_BuildLuaRotationRewards`,
  and `ServerRewardRotationContentContext` `0x07CD`/`0x07D3`) map the separate
  reward-rotation/storefront schedule surface. They do not feed
  `Fortune_ApplyRewards`, `FortunesLib_GetFortunesLootList`, or
  `IFortuneRewardPool`, so they are rejected as Madame Fay active-rotation
  evidence.
- Current Fortune tests cover packet wire shape, catalog probability forwarding
  from `IFortuneRewardPool`, Fortune coin debit and target-scoped Fortune Coin
  account-item auto-claiming, three-card dealing, card flip state,
  `ServerFortuneCardUpdate.HasUpdate` on runtime flip updates, account-item
  grant calls with current-character target identity, account-scoped in-memory
  state, restart reset behavior, and invalid-flip resets.

## Current Emulator Probability Shape

The current emulator builds its card and display catalog pool from `AccountItem`
rows where `Item2Id` is non-zero. Entitlement-only, generic-unlock-only, and
Fortune Coin currency rows are excluded from dealt `ServerFortuneCards` until a
card-safe non-item retail payload is mapped. It maps `Item2.ItemQualityId` to
the three `RewardRarity` values and applies the current rarity-tier constants:

- Normal: `1000`
- Rare: `200`
- Epic: `50`

Catalog audit from `Tools/DataMapping/output/client_source_accountitem_map.csv`
plus `wildstar_client_mysql/Item2.tbl.sql`:

| Slice | Count | Weight | Share |
| --- | ---: | ---: | ---: |
| Item-backed picker/display candidates | 2121 | 624350 | 100% |
| Normal | 256 | 256000 | 41.0026% |
| Rare | 1834 | 366800 | 58.7491% |
| Epic | 31 | 1550 | 0.2483% |

This proves the current approximation is deterministic and auditable against
the extracted tables. It does not prove retail parity.

## Questing-and-more Branch Audit

The LaughingWS `Questing-and-more` Fortune commits `819197dc0f` and
`27486d5e8d` were reviewed as a possible implementation source. They are
rejected/superseded by the current Fortune subsystem and should not be ported
over it.

Confirmed branch shape:

- The branch uses an old `MtxHandler` surface for `ClientGachaOpen`,
  `ClientGachaRollRequest`, and `ClientGachaClaimItem`, emitting old
  `ServerGachaInit`, `ServerGachaRollResult`, and `ServerGachaGrantItem`
  messages rather than the currently mapped `ClientFortune*` /
  `ServerFortune*` packet family.
- `ServerGachaInit` hardcodes a visible account-item list, and card rolls use
  three uniform account-item arrays plus a fourth bonus-card array added by
  `27486d5e8d`. Those arrays do not encode per-item probabilities, active
  storefront rotations, or server-synchronised catalog state.
- Claim state is stored in a static process-wide `bool[]`, making it shared
  across sessions/accounts instead of account-scoped or persistent.
- `27486d5e8d` adds Fortune Coin debit and Fortune Charge reset behavior, but
  still has no retail capture, rotation source, current packet shape, or durable
  session evidence beyond the branch-local hardcoded lists.

Conclusion: the branch records useful historical emulator intent and a sample
set of account-item ids, but it is not stronger evidence than the current
table-audited `FortuneRewardPool`. Its hardcoded arrays are not treated as
retail Madame Fay weights or a validated active rotation.

## Unsupported

- There is no local evidence for exact per-account-item Madame Fay retail
  weights.
- There is no local active-rotation catalog proving which subset of
  `AccountItem` rows belonged to any particular retail Fortune week.
- There is no local live/server capture proving the retail
  `ServerFortuneRewards.RewardItemProbabilities` values.
- The `Questing-and-more` hardcoded gacha account-item arrays are not retail
  weight or rotation evidence and are rejected/superseded by the current mapped
  Fortune implementation.
- There is no local evidence that the current `1000/200/50` rarity-tier
  constants are retail values. They are an emulator approximation that is
  compatible with the mapped client UI packet shape.
- The local logs do not verify live persistence through
  `account_fortune_session`; they only confirm registration of the Fortune
  packet models and handlers. Persistence code and migration are present in the
  tree, but this audit found no live DB smoke artifact for them.

## Verification

Catalog/table audit:

- Parsed `Tools/DataMapping/output/client_source_accountitem_map.csv` and
  `wildstar_client_mysql/Item2.tbl.sql` to compute the current emulator
  candidate counts and rarity-tier probability shares listed above.
- Searched `.nexusforever-runtime/logs`, `Source/NexusForever.WorldServer/bin`,
  `artifacts/load-tests`, `artifacts/verify`, and tracked `Decomp/Analysis`
  docs for Fortune live evidence. Only registration lines and tracked
  decompile findings were found.

Focused tests:

```powershell
dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj `
  --filter "FullyQualifiedName~Fortune" `
  -p:OutDir=I:\GIT\NexusForever\artifacts\testbin\f031-fortune-target\ `
  -p:UseSharedCompilation=false -m:1 -v minimal --nologo
```

Result: `20` passed, `0` failed, `0` skipped. The normal project output path was
locked by a running local `NexusForever.WorldServer`, so the verification used an
isolated local output directory.

## Runtime blocker tranche (2026-06-02)

- `ServerFortuneRewards` wire fields were already named (`Item2IdRewards`, `RewardItemProbabilities`); no guessed per-item weights added.
- `FortuneSessionManager.SendStatus` logs up to eight catalog `(item2Id, probability)` pairs at **Debug** when enabled; roll logic unchanged.
- `FortuneSessionManagerTests.SendStatus_WithNoSessionSendsRewardCatalogAndResetCards`
  now guards the runtime emitter boundary: `ServerFortuneRewards` forwards the
  mapped item/probability catalog and keeps money reward/probability arrays
  empty until retail/catalog evidence proves those producers.
- `FortuneSessionManagerTests.FlipCard_AfterStartGrantsAccountItemAndUpdatesSelectedCard`
  and account-scope coverage now guard that emitted `ServerFortuneCardUpdate`
  messages keep `HasUpdate=true`; native `Fortune_ApplyCardUpdate`
  (`1407290a0`) ignores operation/card flags when this leading bool is false.
- The same payout test now pins the account-inventory grant target boundary:
  Fortune passes the current realm/character identity and sets
  `hasTargetPlayerIdentity` when awarding the flipped card's account item.
- Unblock still requires retail `ServerFortuneRewards` capture or storefront rotation dump (`f031-fortune-playthrough` harness bundle).

## Closure State

Mapped only / blocked.

The client-facing probability transport is mapped, current emulator probability
output is table-audited, the `Questing-and-more` gacha implementation is
audited/rejected as superseded, and current Fortune tests cover the implemented
session and packet boundaries. Exact per-item retail weights remain blocked.

The blocker would be unlocked by one of:

- A retail-era packet capture containing `ServerFortuneRewards` for a known
  Madame Fay rotation.
- A storefront-server catalog dump or service response containing the active
  account-item ids and weights for a known rotation.
- A native/server artifact that maps the active rotation list and per-item
  weights before `ServerFortuneRewards` is emitted.

Evidence collection update (2026-05-28): `Start-BlockerEvidenceHarness.ps1`
now supports `-FortuneRewardsSmoke`, which creates an LWS-066 bundle worksheet
for the packet/catalog evidence above plus card deal/flip/payout, reload
persistence, insufficient-currency, invalid-flip, and repeated-flip checks.
Parser validation and a create-only bundle pass were run after adding the
preset. `Decomp/Analysis/test_blocker_evidence_harness_presets.py` now
dry-runs the preset and verifies the manifest, worksheet, helper files, and
negative-case scaffold; the blocker remains open until a real bundle provides
retail/catalog weights.

Runtime-boundary update (2026-05-28): focused tests now exercise the real
`FortuneRewardPool` with synthetic `AccountItem.tbl` and `Item2.tbl` rows.
`GetRewardCatalog_RealPoolAdvertisesOnlyMappedItemRewards` pins the mapped
`ServerFortuneRewards` item/probability transport and keeps Fortune Coin plus
non-item account rewards out of the advertised display catalog.
`PickCardRewards_RealPoolKeepsNonItemAccountRewardsPickerOnly` preserves the
current picker-only handling for entitlement/generic-unlock account rewards
without treating those rows as mapped retail item probabilities. Exact
per-item weights and active rotations remain blocked.

Runtime-boundary update (2026-06-04): `ServerFortuneReset.Unknown` was renamed
to `ResetCode` after the native `Fortune_ApplyReset` consumer confirmed code
`3` drives the click-empty reset path. A same-pass `Fortune_ApplyRewards`
recheck found only client-side cache application of server-provided reward and
probability arrays, so active rotation and exact retail weights remain blocked
on a retail `ServerFortuneRewards` capture or storefront-server catalog dump.
Fortune payout grants now explicitly target the current character identity in
the account-inventory API, matching the already-built `BuildTargetIdentity`
boundary without widening card selection or reward weights.
Focused `FullyQualifiedName~Fortune` verification passed 20/20 with isolated
output at `artifacts/testbin/f031-fortune-target`.

Target-scoped auto-claim guard (2026-06-07): `ClientFortuneStart` still
auto-claims a matching `CanClaim` Fortune Coin account item before re-checking
and debiting the one-coin start cost, but a bundle targeted at another character
is not claimed. The client receives the mapped click-empty reset, no currency is
added or debited, and the account-inventory row remains claimable. Focused
`FullyQualifiedName~Fortune` verification passed 24/24 with isolated output at
`artifacts/codex-test-bin/`.

Reward-rotation false-source cleanup (2026-06-04): the F-007 reward-rotation
surface is a rejected source for F-031 active Madame Fay rotation. Native labels
for the reward-rotation DB tables, `Lua_GameLib_BuildRewardRotations`,
`RewardRotation_BuildLuaRotationRewards`, and the `0x07CD`/`0x07D3`
content-context packets explain GameLib/matchmaking reward schedule refreshes
and the storefront bootstrap ordering that can precede Fortune status. They do
not explain Fortune catalog selection or probabilities, which still enter the
UI only through `ServerFortuneRewards` -> `Fortune_ApplyRewards` ->
`FortunesLib_GetFortunesLootList`. This changes no runtime behavior and keeps
retail active rotation blocked on a real `ServerFortuneRewards` capture,
storefront-server catalog dump, or native/server artifact that produces that
packet family. Focused boundary verification passed 32/32:
`FullyQualifiedName~Fortune|FullyQualifiedName~RewardRotationRuntimeEvidenceTests|FullyQualifiedName~ClientRewardUpdateRequestHandlerTests`
with isolated output at `artifacts/testbin/f031-fortune-rewardrotation-falsesource/`.

Missing-inventory flip guard (2026-06-08): `ClientFortuneFlipCard` now fails
closed with the mapped click-empty reset when account-inventory delivery is
unavailable, before marking the selected card flipped, granting an account item,
persisting flipped state, or emitting `ServerFortuneCardUpdate`. This is a
runtime dependency guard only; it does not change weights, active rotations, or
eligible reward pools. Focused `FullyQualifiedName~FortuneSessionManagerTests`
verification passed 14/14 and broader `FullyQualifiedName~Fortune` verification
passed 25/25 with isolated output at `artifacts/codex-test-bin/`.

Reward-pool partial-table guard (2026-06-08): `FortuneRewardPool` now treats a
missing `AccountItem` table like no reward candidates and returns an empty
catalog/card pool instead of throwing. A missing `Item2` table follows the
existing missing-row behavior and maps otherwise item-backed rewards to
`RewardRarity.Normal`. This is a static-data availability guard only; it does
not add non-item rewards, change rarity-tier weights, or infer active rotation
state. Focused `FullyQualifiedName~FortuneRewardPoolTests` verification passed
6/6 and broader `FullyQualifiedName~Fortune` verification passed 27/27 with
isolated output at `artifacts/codex-test-bin/`.

Cached-export/source recheck (2026-06-09): Ghidra MCP discovery found no
running instances, so this pass used tracked labels plus cached
`WildStar64.exe` fragments under
`selected_decompiled_cache/functions/sha256_231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5`.
`ServerFortuneRewards_ReadPayload` (`140081f60`) still proves only the
item2/money/probability transport shape. `Fortune_ApplyRewards` (`1407292a0`)
copies server-provided arrays into UI state, `FortunesLib_GetFortunesLootList`
(`140766370`) exposes cached item2 rewards and displays
`fProbability = serverFloat * 100`, and `FortuneNode_ApplyServerFortunePackets`
(`1404d60f0`) dispatches `0x03CF`-`0x03D2`. Source still emits
`ServerFortuneRewards` from emulator rarity-tier `FortuneRewardPool`; no retail
active rotation source, per-item weight table, or storefront-server catalog
proof surfaced. The closure state remains mapped-only / blocked.

Blocked recheck (2026-06-17): `mcp__ghidra_mcp.list_instances` again returned
no running Ghidra instance, so this pass used cached `WildStar64.exe`
fragments, current source/tests, and this audit. The evidence boundary is
unchanged: `ServerFortuneRewards_ReadPayload` (`140081f60`) proves item2,
money, and probability-array transport; `Fortune_ApplyRewards` (`1407292a0`)
copies server-provided arrays into Fortune UI state; `FortunesLib_GetFortunesLootList`
(`140766370`) displays `fProbability = serverFloat * 100`;
`FortuneNode_ApplyServerFortunePackets` (`1404d60f0`) dispatches the
`0x03CF`-`0x03D2` packet family; and `ServerFortuneCards_ReadPayload`
(`1400a0b10`) remains card-state transport evidence. Current source still
emits `ServerFortuneRewards` from emulator rarity-tier `FortuneRewardPool`, and
no retail `ServerFortuneRewards` capture, storefront-server catalog dump, or
native/server producer artifact is available locally. The create-bundle-only
worksheet
`artifacts/blocker_evidence/20260617-225039-F031-fortune-retail-weights-recheck`
records the missing evidence path and LWS-066 target checklist. Exact per-item
Madame Fay probabilities, money reward arrays, and active rotation remain
blocked until retail/catalog evidence proves item ids and probabilities.
