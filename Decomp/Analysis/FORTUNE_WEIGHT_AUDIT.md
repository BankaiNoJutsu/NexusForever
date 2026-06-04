# F-031 Madame Fay Weight Audit

Status date: 2026-06-02 (runtime blocker tranche reaffirmed)

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
- Current Fortune tests cover packet wire shape, catalog probability forwarding
  from `IFortuneRewardPool`, Fortune coin debit, three-card dealing, card flip
  state, account-item grant calls, account-scoped in-memory state, restart
  reset behavior, and invalid-flip resets.

## Current Emulator Probability Shape

The current emulator builds its candidate pool from `AccountItem` rows where
`Item2Id`, `EntitlementId`, or `GenericUnlockSetId` is non-zero, excluding
non-item `FortuneCoin` currency rows. It maps `Item2.ItemQualityId` to the
three `RewardRarity` values and applies the current rarity-tier constants:

- Normal: `1000`
- Rare: `200`
- Epic: `50`

Catalog audit from `Tools/DataMapping/output/client_source_accountitem_map.csv`
plus `wildstar_client_mysql/Item2.tbl.sql`:

| Slice | Count | Weight | Share |
| --- | ---: | ---: | ---: |
| All picker candidates | 2176 | 679350 | 100% |
| Picker Normal | 311 | 311000 | 45.7791% |
| Picker Rare | 1834 | 366800 | 53.9928% |
| Picker Epic | 31 | 1550 | 0.2282% |
| Displayed item candidates | 2121 | 624350 | 100% |
| Display Normal | 256 | 256000 | 41.0026% |
| Display Rare | 1834 | 366800 | 58.7491% |
| Display Epic | 31 | 1550 | 0.2483% |
| Non-item picker candidates not advertised in `Item2IdRewards` | 55 | 55000 | 8.0957% of picker weight |

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
  -p:OutDir=I:\GIT\NexusForever\.nexusforever-runtime\build\fortune-audit-outdir\ `
  -p:UseSharedCompilation=false -m:1 -v minimal --nologo
```

Result: `17` passed, `0` failed, `0` skipped. The normal project output path was
locked by a running local `NexusForever.WorldServer`, so the verification used an
isolated local output directory. The build emitted unrelated existing warnings in
`NexusForever.Script.Main` and `TutorialStartFlowTests`.

## Runtime blocker tranche (2026-06-02)

- `ServerFortuneRewards` wire fields were already named (`Item2IdRewards`, `RewardItemProbabilities`); no guessed per-item weights added.
- `FortuneSessionManager.SendStatus` logs up to eight catalog `(item2Id, probability)` pairs at **Debug** when enabled; roll logic unchanged.
- `FortuneSessionManagerTests.SendStatus_WithNoSessionSendsRewardCatalogAndResetCards`
  now guards the runtime emitter boundary: `ServerFortuneRewards` forwards the
  mapped item/probability catalog and keeps money reward/probability arrays
  empty until retail/catalog evidence proves those producers.
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
