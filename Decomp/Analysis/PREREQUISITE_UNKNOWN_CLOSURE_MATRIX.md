# Prerequisite Unknown Closure Matrix

Tracks the remaining `PrerequisiteType.UnknownNNN` entries after pass 106. The
goal is to resolve each id as one of:

- renamed only when semantic ownership is mapped;
- closed as an alias, no-op, orphan, skipped, or rejected surface;
- blocked with one named missing evidence source.

Evidence sources for this matrix:

- `Source/NexusForever.Game.Static/Prerequisite/PrerequisiteType.cs`
- `Source/NexusForever.Game.Tests/Prerequisite/PrerequisiteTypeNamingTests.cs`
- `wildstar_client.prerequisite` row scan across slots 0-2
- full `wildstar_client` prerequisite-reference column scan
- `PrerequisiteManager_EvaluateTypeSlot` (`1404a2100`) cached decompile
- raw PE reads from `Decomp/Client64/WildStar64.exe` live vtable slots

## Current Counts

| Count | Meaning |
| ---: | --- |
| 24 | active enum entries still named `UnknownNNN` after type 267 was renamed |
| 22 | ids explicitly unused in `PrerequisiteType.tbl` |
| 274 | active enum entries in `PrerequisiteType.cs` |

## Closure Buckets

| Bucket | Ids | Closure rule |
| --- | --- | --- |
| Closed rejected no-op | `48`, `142`, `203`, `222`, `259`, `271`, `272`, `278`, `283`, `285`, `294` | Live dispatch reaches `140001ba0` (`return 0`) and there is no non-stub semantic predicate. Reopen only with non-stub live path or live sniff evidence. |
| Closed orphan helpers | `189`, `192`, `193` | Useful helper bodies exist, but live dispatch routes to `140001ba0`, handler-table slots are `_purecall`, and no direct caller proves enum ownership. Reopen only with caller/dispatch proof. |
| Closed duplicate-body aliases | `223`, `240`, `245`, `275`, `286`, `289`, `290`, `291` | Live dispatch reaches an already-mapped body. Keep `UnknownNNN` unless row/use-site evidence proves a semantic owner distinct from the body twin. |
| Closed skipped dispatcher | `144` | No retail rows, no external refs, and no live dispatcher case. Handler-table body remains diagnostic only. |
| Renamed mapped predicate | `267` -> `GroupIsRaid`, `295` -> `EvaluatedEntityPresent` | Live predicate and retail use-site/row evidence prove precise semantic owners. |
| Diagnostic field-owner blockers | `260` | Deep-map runtime field/use-site owner before renaming. |

## Per-Id Matrix

| Id | Rows | External refs | Live dispatch | Current class | Next unblock |
| ---: | ---: | ---: | --- | --- | --- |
| 48 | 2 | 5 | case `0x30`, vtable `+0x1e8` -> `140001ba0` | closed rejected no-op | Spell4Effects caster-persistence refs only; reopen only with non-stub live path or live sniff. |
| 142 | 9 | 8 | case `0x8e`, vtable `+0x170` -> `140001ba0` | closed rejected no-op | Spell4Effects caster/target apply refs only; reopen only with non-stub eval-context predicate proof. |
| 144 | 0 | 0 | no live case; handler table[144] -> `1404a0b70` | closed skipped dispatcher | Diagnostic handler-table body only; reopen only with live reachability or retail rows. |
| 189 | 9 | 24 | case `0xbd`, vtable `+0x2d8` -> `140001ba0`; handler table `_purecall` | closed orphan helper | Orphan `1404a01e0` remains labelled; reopen only with caller/dispatch proof. |
| 192 | 3 | 3 | case `0xc0`, vtable `+0x630` -> `140001ba0`; handler table `_purecall` | closed orphan helper | Orphan `1404a0260` remains labelled; reopen only with caller/dispatch proof. |
| 193 | 12 | 65 | case `0xc1`, vtable `+0x2e0` -> `140001ba0`; handler table `_purecall` | closed orphan helper | Orphan `1404a02e0` remains labelled; reopen only with caller/dispatch proof. |
| 203 | 7 | 65 | case `0xcb`, vtable `+0x348` -> `140001ba0`; handler table `_purecall` | closed rejected no-op | Achievement server refs only; reopen only with non-stub live path or live sniff. |
| 222 | 2 | 0 | case `0xde`, vtable `+0x380` -> `140001ba0`; handler table `_purecall` | closed rejected no-op | No external refs; reopen only with non-stub live path or live sniff. |
| 223 | 2 | 6 | case `0xdf`, vtable `+0x438` -> `14049f8c0` | closed duplicate-body alias | Same body as account-item list count NPC type 97; reopen only with distinct owner proof. |
| 240 | 15 | 20 | case `0xf0`, vtable `+0x2e8` -> `14049e7e0` | closed duplicate-body alias | Same currency body; reopen only with distinct owner proof. |
| 245 | 249 | 248 | case `0xf5`, vtable `+0x698` -> `1404a1790` | closed duplicate-body alias | Same body as `ItemTradeSkill`; reopen only with distinct owner proof. |
| 259 | 4 | 0 | case `0x103`, vtable `+0x530` -> `140001ba0`; handler table `_purecall` | closed rejected no-op | No external refs; reopen only with non-stub live path or live sniff. |
| 260 | 1 | 0 | case `0x104`, vtable `+0x640` -> `1404a1220` | diagnostic field-owner blocker | State `5` is backed by `HousingPlotEntry_DispatchBuildCompleteIfState5` (`1405a9920`) and `HousingResidence_SetBuildState5AndDispatchComplete` (`1405a9980`); `ClientDB_GetHousingPlotInfo` (`140205fa0`), `ClientDB_GetHousingPlugItem` (`140206c60`), and `HousingResidence_FindPlotEntryByPlotInfoId` (`1405aea10`) now map the lookup chain. Row `36343` still lacks a prerequisite use-site and the active residence state fields remain unnamed. |
| 267 | 2 | 1 | case `0x10b`, decimal vtable `800` (`+0x320`) -> `14049e9a0` | renamed mapped predicate | Renamed to `GroupIsRaid`; bit 1 at `*(DAT_140c65898+0x6c50)+8` maps to current group context `GroupFlags.Raid`; retail rows use `NotEqual 0`. |
| 271 | 2 | 5 | case `0x10f`, vtable `+0x178` -> `140001ba0`; handler table `_purecall` | closed rejected no-op | Spell4Effects target-apply refs only; reopen only with non-stub live path or live sniff. |
| 272 | 0 | 0 | case `0x110`, vtable `+0x180` -> `140001ba0`; handler table `_purecall` | closed rejected no-op | No rows/refs; reopen only with non-stub live path or live sniff. |
| 275 | 281 | 281 | case `0x113`, vtable `+0x6a0` -> `1404a17e0` | closed duplicate-body alias | Same body as `ItemTradeSkillKnown`; reopen only with distinct owner proof. |
| 278 | 1 | 1 | case `0x116`, vtable `+0x300` -> `140001ba0` on caster and target | closed rejected no-op | Test spell `84203` only; reopen only with non-stub live path or live sniff. |
| 283 | 0 | 0 | case `0x11b`, vtable `+0x6a8` -> `140001ba0`; handler table `_purecall` | closed rejected no-op | No rows/refs; reopen only with non-stub live path or live sniff. |
| 285 | 2 | 7 | case `0x11d`, vtable `+0x6b0` -> `140001ba0`; handler table `_purecall` | closed rejected no-op | AOE target refs only; reopen only with non-stub live path or live sniff. |
| 286 | 2 | 6 | case `0x11e`, vtable `+0x688` -> `1404a1710` | closed duplicate-body alias | Same body as `DailyLoginDaysTotal`; reopen only with distinct owner proof. |
| 289 | 3 | 0 | case `0x121`, vtable `+0x3d0` -> `14049f0c0` | closed duplicate-body alias | Same body as `Mount`; no external refs; reopen only with distinct owner proof. |
| 290 | 7 | 38 | case `0x122`, vtable `+0x3a8` -> `14049efa0` | closed duplicate-body alias | Same body as `UnderForcedMovement`; reopen only with distinct owner proof. |
| 291 | 1 | 0 | case `0x123`, vtable `+0x6c0` -> `1404a1890` | closed duplicate-body alias | Same body as `ProgressTrackOnMatchingEntity`; no external refs; reopen only with distinct owner proof. |
| 294 | 1 | 0 | case `0x126`, vtable `+0x6d0` -> `140001ba0`; handler table `_purecall` | closed rejected no-op | Unreferenced all-zero row; reopen only with non-stub live path or live sniff. |
| 295 | 1 | 1 | case `0x127` inline: true only for `NotEqual` and evaluated entity present | renamed mapped predicate | Renamed to `EvaluatedEntityPresent`; row 44550 is a target-apply gate on Engineer Portable Black Hole Large Pull. |

## Next Pass Order

1. No active rename candidates remain in this 26-id slice.
2. `260` stays mapped-only/blocked until the active residence state fields and a reachable prerequisite use-site are named. The lookup chain now reaches `HousingPlotInfo` / `HousingPlugItem` / residence plot-entry helpers and state value `5` is native-event backed by `HousingBuildComplete`, but row `36343` still has no prerequisite reference outside the row itself.

## Closed Slices

### Pass 108 - no-row/no-ref no-op rejects

Closed as rejected/no semantic predicate:

- `Unknown222`: two retail rows, no external prerequisite references, live
  vtable `+0x380` -> `140001ba0`, handler table `_purecall`.
- `Unknown259`: four retail rows, no external prerequisite references, live
  vtable `+0x530` -> `140001ba0`, handler table `_purecall`.
- `Unknown272`: no retail rows, no external prerequisite references, live
  vtable `+0x180` -> `140001ba0`, handler table `_purecall`.
- `Unknown283`: no retail rows, no external prerequisite references, live
  vtable `+0x6a8` -> `140001ba0`, handler table `_purecall`.
- `Unknown294`: one all-zero unreferenced retail row, live vtable `+0x6d0`
  -> `140001ba0`, handler table `_purecall`.

These stay `UnknownNNN` in the enum to preserve table ids, but are no longer
active semantic rename candidates. Reopen only if a non-stub live path, direct
caller, or live sniff proves behavior outside the current 16042 evidence.

### Pass 109 - content-referenced no-op rejects

Closed as rejected/no semantic predicate:

- `Unknown48`: five `Spell4Effects.prerequisiteIdCasterPersistence` refs on
  spell `47945`; live vtable `+0x1e8` -> `140001ba0`.
- `Unknown142`: eight `Spell4Effects` caster/target apply refs; live vtable
  `+0x170` -> `140001ba0`.
- `Unknown203`: 65 `achievement.prerequisiteIdServer` refs; live vtable
  `+0x348` -> `140001ba0`, handler table `_purecall`.
- `Unknown271`: five `Spell4Effects.prerequisiteIdTargetApply` refs; live
  vtable `+0x178` -> `140001ba0`, handler table `_purecall`.
- `Unknown278`: one test spell `84203` AOE target ref; live vtable `+0x300`
  -> `140001ba0` on both caster and target.
- `Unknown285`: seven Spell4 AOE target/preferred-target refs; live vtable
  `+0x6b0` -> `140001ba0`, handler table `_purecall`.

`140001ba0` decompiles to `return 0`, so these content references appear to be
disabled gates or dead/test gates in build 16042, not meaningful prerequisite
predicates. Reopen only with non-stub live path or live sniff evidence.

### Pass 110 - orphan helper rejects

Closed as orphan helpers rather than semantic enum predicates:

- `Unknown189`: live case `0xbd` vtable `+0x2d8` -> `140001ba0`;
  handler table[189] `_purecall`; orphan helper `1404a01e0` remains labelled as
  `Prerequisite_CheckPathSettlerHubBuildProgress`.
- `Unknown192`: live case `0xc0` vtable `+0x630` -> `140001ba0`;
  handler table[192] `_purecall`; orphan helper `1404a0260` remains labelled as
  `Prerequisite_CheckPathSettlerHubContributionProgress`.
- `Unknown193`: live case `0xc1` vtable `+0x2e0` -> `140001ba0`;
  handler table[193] `_purecall`; orphan helper `1404a02e0` remains labelled as
  `Prerequisite_CheckPathSettlerHubProgressPercent`.

The helper labels remain useful for native path-settler runtime study, but they
do not prove prerequisite enum ownership in build 16042. Reopen only with direct
caller evidence, live dispatcher evidence, or live-client proof that these
helpers are reached outside the current `PrerequisiteManager_EvaluateTypeSlot`
path.

### Pass 111 - skipped dispatcher reject

Closed `Unknown144` as an unreachable/skipped dispatcher candidate:

- no `wildstar_client.prerequisite` rows;
- no external prerequisite references;
- no live `PrerequisiteManager_EvaluateTypeSlot` case for `0x90`;
- handler table[144] points to diagnostic body `1404a0b70`, but the live
  dispatcher skips that case.

The handler-table body remains labelled for reference as
`Prerequisite_CheckLiveEventStateEqualsOne_Table144`, but it does not prove enum
ownership. Reopen only with live reachability or retail row evidence.

### Pass 112 - duplicate-body alias closure

Closed as duplicate-body aliases without distinct semantic enum ownership:

- `Unknown223`: live vtable `+0x438` -> `14049f8c0`, same body as the account
  item-list count NPC type 97 family.
- `Unknown240`: live vtable `+0x2e8` -> `14049e7e0`, same currency body.
- `Unknown245`: live vtable `+0x698` -> `1404a1790`, same body as
  `ItemTradeSkill`.
- `Unknown275`: live vtable `+0x6a0` -> `1404a17e0`, same body as
  `ItemTradeSkillKnown`.
- `Unknown286`: live vtable `+0x688` -> `1404a1710`, same body as
  `DailyLoginDaysTotal`.
- `Unknown289`: live vtable `+0x3d0` -> `14049f0c0`, same body as `Mount`.
- `Unknown290`: live vtable `+0x3a8` -> `14049efa0`, same body as
  `UnderForcedMovement`.
- `Unknown291`: live vtable `+0x6c0` -> `1404a1890`, same body as
  `ProgressTrackOnMatchingEntity`.

These remain `UnknownNNN` because the duplicate body proves behavior but does
not prove a distinct semantic enum owner. Reopen only with row/use-site evidence
that proves a more specific owner than the already-mapped body twin. NF now
handles the account-item claim row contexts for `Unknown245` and `Unknown275`
without using those contextual rows as enum rename evidence.

### Pass 113 - type 295 renamed

Renamed `Unknown295` to `EvaluatedEntityPresent`.

Evidence:

- live `PrerequisiteManager_EvaluateTypeSlot` case `0x127` is inline and
  returns true only when the evaluated entity pointer is non-null and comparison
  is `NotEqual`; `value0` is ignored;
- retail row `44550` is referenced by `Spell4Effects.prerequisiteIdTargetApply`
  on effect `230486` for spell `88035` (`Engineer - Portable Black Hole - Large
  Pull - Tier 1`);
- NF already proxies the evaluated entity through `IPrerequisiteParameters.Target`
  and has focused tests for the native comparison behavior.

### Pass 114 - type 267 renamed, type 260 blocker narrowed

Renamed `Unknown267` to `GroupIsRaid`.

Evidence:

- live `PrerequisiteManager_EvaluateTypeSlot` case `0x10b` dispatches vtable
  `+0x320` to `14049e9a0`;
- `14049e9a0` returns bit 1 of the current group context flags at
  `*(DAT_140c65898+0x6c50)+8`;
- NexusForever already models that bit as `GroupFlags.Raid` in the internal
  group snapshot;
- retail rows `19225` and `37519` use type 267 with `NotEqual 0` against that
  raid bit.

`Unknown260` remains intentionally unnamed. The predicate now has a mapped
lookup chain through `ClientDB_GetHousingPlotInfo` (`140205fa0`),
`ClientDB_GetHousingPlugItem` (`140206c60`), and
`HousingResidence_FindPlotEntryByPlotInfoId` (`1405aea10`), while the housing
plug state value `5` is backed by native `HousingBuildComplete` paths:
`HousingPlotEntry_DispatchBuildCompleteIfState5` (`1405a9920`) dispatches the
event for plot-entry rows at state `5`, and
`HousingResidence_SetBuildState5AndDispatchComplete` (`1405a9980`) transitions
residence build state `4` -> `5`, clears handles, and dispatches the same
event. The active residence state fields and row/use-site reachability are
still not named well enough for a safe enum rename.

### Pass 115 - prerequisite localized text/use-site audit

Joined `wildstar_client.prerequisite`, `prerequisitetype`, and `stringsenus`
for the remaining unknowns and adjacent account-item gates.

No additional unknown enum renames were accepted:

- `Unknown245` account-item rows use `objectId` as a dye colour-ramp unlock id
  with `NotEqual 0` claim text such as "Dye already acquired!", but live
  dispatcher evidence still maps type `245` to the `ItemTradeSkill` duplicate
  body. NF handles this only when evaluating `accountitem.prerequisiteId`.
- `Unknown275` account-item rows use `objectId` as the Holo-Wardrobe Item2 id
  with `NotEqual 0` claim text, but live dispatcher evidence still maps type
  `275` to the `ItemTradeSkillKnown` duplicate body. NF handles this only when
  evaluating `accountitem.prerequisiteId`.
- Type `270` was already named `LoyaltyRewards`; the join maps it to account
  currency `8` (`CosmicReward`) with the threshold in `valueN`, so NF now has a
  dedicated handler for that server-side gate.

### Pass 116 - account-item live/use-site ownership check

Checked whether the account-item claim/use surfaces prove distinct enum
ownership for `Unknown245` or `Unknown275`.

SQL/use-site evidence:

- Full prerequisite-reference column inventory includes many possible
  `wildstar_client` prerequisite owner columns, but rows containing type `245`
  or `275` are referenced only by `accountitem.prerequisiteId` and
  `spell4.prerequisiteIdCasterCast` in this pass.
- `Unknown245`: `accountitem.prerequisiteId` has 28 refs / 28 distinct
  prerequisites; `spell4.prerequisiteIdCasterCast` has 220 refs / 219 distinct
  prerequisites. Slot shape is `NotEqual 0` with `objectId` ranges `2..504`
  in slot 0 and `355..371` in slot 2.
- `Unknown275`: `accountitem.prerequisiteId` has 281 refs / 281 distinct
  prerequisites and no `spell4` refs. Slot shape is `NotEqual 0` with
  Holo-Wardrobe Item2 ids in `objectId`.
- Joined failure text confirms the account-item context: `245` rows say the
  dye is already learned; `275` rows say the item was already added to the
  Holo-Wardrobe.

Native ownership evidence:

- `PrerequisiteManager_EvaluateTypeSlot` (`1404a2100`) dispatches case `0xf5`
  to vtable `+0x698` -> `Prerequisite_CheckItemTradeSkill` (`1404a1790`) and
  case `0x113` to vtable `+0x6a0` ->
  `Prerequisite_CheckItemTradeSkillKnown` (`1404a17e0`).
- `Prerequisite_CheckItemTradeSkill` and
  `Prerequisite_CheckItemTradeSkillKnown` both require NPC target types
  `0x14/0x17` and call the tradeskill/known-recipe helpers. No dye,
  Holo-Wardrobe, or account-item ownership branch is present in either body.
- Cached account-item Lua/UI use paths
  `Lua_AccountItemLib_TakeAccountItem` (`1404e50f0`),
  `AccountItemUi_ClaimSelectedPendingItemGroup` (`140518b70`), and
  `AccountItemUi_TakeSelectedAccountItem` (`140518be0`) resolve the selected
  pending group/account item and send opcodes `0x0233` or `0x0839` through
  `AccountItem_SendClientClaimPendingItemGroup` (`140006ba0`) or
  `AccountItem_SendClientAccountItemTake` (`140006d00`). The selected call-edge
  graph shows no call from these account-item surfaces to
  `PrerequisiteManager_EvaluateEntry`, `PrerequisiteManager_EvaluateTypeSlot`,
  `Prerequisite_CheckItemTradeSkill`, or
  `Prerequisite_CheckItemTradeSkillKnown`.

Conclusion:

- Keep enum names `Unknown245` and `Unknown275`. The account-item row text
  proves server-side contextual handling, not live client enum ownership.
- NF's account-item-context handlers remain valid as server gates, but a safe
  enum rename now requires live debugger call stacks or fresh static evidence
  proving an alternate account-item evaluator reaches these ids outside the
  normal dispatcher.

### Pass 117 - local live-debug and CDB evidence

Closed the remaining live/use-site proof locally.

- Stopped the stale `NexusForever.WorldServer` lock, restarted
  `NexusForever.WorldServer`, launched WildStar through
  `NexusForever.ClientConnector`, and confirmed the client connected to local
  auth on port `6600`.
- A Python breakpoint harness under `artifacts/live-prereq-debug/` attempted to
  attach to `WildStar64` with breakpoints on the account-item senders,
  account-item UI/Lua entry points, and prerequisite dispatcher/body addresses.
- Non-elevated attach from this Codex session was denied (`Access is denied`),
  and the token does not have `SeDebugPrivilege` assigned. An elevated Python
  retry attached, but the first version passed the MSVC thread-name exception
  `0x406d1388` through to the debuggee and caused the client to close during
  login. The harness now consumes that exception.
- After the Ghidra lock cleared, focused `TraceFunctionCallers` passes were
  rerun for `AccountItem_SendClientClaimPendingItemGroup` (`140006ba0`) and
  `AccountItem_SendClientAccountItemTake` (`140006d00`). The claim sender has
  direct code callers only from `FUN_1404e5050` and
  `AccountItemUi_ClaimSelectedPendingItemGroup` (`140518b70`); the take sender
  has direct code callers only from `Lua_AccountItemLib_TakeAccountItem`
  (`1404e50f0`) and `AccountItemUi_TakeSelectedAccountItem` (`140518be0`).
  No refreshed static caller trace shows an account-item path calling
  `PrerequisiteManager_EvaluateEntry`, `PrerequisiteManager_EvaluateTypeSlot`,
  `Prerequisite_CheckItemTradeSkill`, or
  `Prerequisite_CheckItemTradeSkillKnown`.
- `cdbX64.exe` was then run elevated through
  `artifacts/live-prereq-debug/Run-PrereqCdbProbe.ps1`; log:
  `artifacts/live-prereq-debug/prereq_probe_cdb_admin_20260605_225844.log`.
  The CDB script ignores `0x406d1388` with `sxi 406d1388` and sets absolute
  breakpoints from the loaded `WildStar64` base address.
- Live CDB hit `PrerequisiteManager_EvaluateTypeSlot` for type `0x113` rows
  with slot dumps shaped like `00000113 00000002 <Item2Id> 00000000`, followed
  immediately by `Prerequisite_CheckItemTradeSkillKnown` (`1404a17e0`). The
  target-type word dumped from the handler target was `0x14`, matching the
  NPC-gated known-tradeskill duplicate body. This reinforces that type `275`
  has native duplicate-body behavior, not a distinct Holo-Wardrobe client
  evaluator.
- Live CDB also hit the account-item take path twice:
  `Lua_AccountItemLib_TakeAccountItem` (`1404e50f0`) then
  `AccountItem_SendClientAccountItemTake` (`140006d00`). The immediate stack is
  the Lua/account-item send path; it does not include
  `PrerequisiteManager_EvaluateEntry`, `PrerequisiteManager_EvaluateTypeSlot`,
  `Prerequisite_CheckItemTradeSkill`, or
  `Prerequisite_CheckItemTradeSkillKnown`.
- No live claim-group click was captured in this pass. Claim remains covered by
  the refreshed static caller trace above, while the live take evidence proves
  the account-item UI/Lua send path can execute without the client prerequisite
  evaluator.

Conclusion remains unchanged and stronger: keep `Unknown245` and `Unknown275`
as duplicate-body aliases for enum naming. NF should keep the account-item
context handlers as server gates for dye/Holo-Wardrobe claim data, but those
contextual account-item rows are not rename-grade evidence for distinct client
prerequisite enum ownership.

### Pass 118 - account-item claim retry under CDB

Retried the live pass with `cdbX64.exe` still attached after the client was
restarted. The CDB command file continued to ignore the MSVC thread-name
exception (`sxi 406d1388`) and set account-item sender plus duplicate-body
breakpoints from the loaded `WildStar64` base.

Local setup and seed evidence:

- Runtime log: `Source/NexusForever.WorldServer/bin/Debug/net10.0/logs/NexusForever.WorldServer_20260605_44724.log`.
- CDB retry log:
  `artifacts/live-prereq-debug/prereq_probe_cdb_retry_20260605_232004.log`.
- Seeded dye pending group `codex-live-dye-20260605231934` for account `1`,
  account item `272`, prerequisite `39615`. That row contains type `245`
  (`0xf5`) with dye colour-ramp object `362` (`0x16a`).
- Seeded Holo-Wardrobe pending group `codex-live-holo-20260605232615` for
  account `1`, account item `24`, item2/object `42367` (`0xa57f`),
  prerequisite `39781`.

Live account-item evidence:

- The dye group loaded in the initial account packets with
  `pending=[codex-live-dye-20260605231934[1:272:CanClaim]]` and claimed
  successfully. World log lines `32340..32347` show the claim request/result;
  line `32341` shows the account item add as inventory id `60`.
- CDB line `10273` hit `AccountItem_SendClientClaimPendingItemGroup` during
  the dye claim. CDB lines `10186/10229`, `11116/11159`, `21786/21829`,
  `24076/24119`, and `34668/34711` also hit the Lua/take sender path
  (`Lua_AccountItemLib_TakeAccountItem` ->
  `AccountItem_SendClientAccountItemTake`).
- A later dye take of inventory id `60` succeeded server-side; world log lines
  `41257..41262` show `ClientAccountItemTake` result `Ok`.
- The Holo-Wardrobe group loaded with
  `pending=[codex-live-holo-20260605232615[1:24:CanClaim]]` and claimed
  successfully. World log lines `45230..45237` show the claim request/result;
  line `45231` shows account item `24` added as inventory id `61`.
- CDB line `45094` hit `AccountItem_SendClientClaimPendingItemGroup` during
  the Holo claim.
- The first Holo take attempts for inventory id `61` returned `GenericFail`
  because the character inventory had no free slot (operator observation).
  After freeing inventory space, the same account item take succeeded; world
  log lines `50241..50246` show `ClientAccountItemTake` result `Ok`.
- CDB captured the Holo take sender path during those attempts:
  `Lua_AccountItemLib_TakeAccountItem` -> `AccountItem_SendClientAccountItemTake`
  at lines `56451/56494`, `56538/56581`, `56625/56668`,
  `56712/56755`, `56799/56842`, and `59028/59071`.

Live prerequisite-slot evidence:

- Type `245` repeatedly reached `Prerequisite_CheckItemTradeSkill` with the
  dye slot dump `000000f5 00000002 0000016a 00000000`; representative CDB
  anchors include lines `10372`, `10540`, `10889`, `44877`, `45193`,
  `45281`, `45448`, `45835`, and `45985`.
- Type `275` reached `Prerequisite_CheckItemTradeSkillKnown` for the seeded
  Holo item with slot dump `00000113 00000002 0000a57f 00000000`
  (`0xa57f` = item2/object `42367`); representative CDB anchors include lines
  `45237`, `45486`, `45873`, `45911`, `45948`, `56030`, `56106`,
  `56410`, `56985`, `57117`, `57251`, and `58987`.
- The separate live disconnect observed during the pass was not caused by the
  prerequisite probes. World log lines `36752..36757` show the server received
  old-model `ClientStorefrontPurchaseVirtualCurrencyPackage(0x082E)` and then
  threw `EndOfStreamException` in that reader, which forced a world session
  disconnect. Follow-up raw PE registration-table evidence corrected `0x082E`
  to `StorefrontLib.RequestHistory` (`140b69e10` -> `1404f1d50`), a zero-byte
  storefront purchase-history request now modeled as
  `ClientStorefrontRequestPurchaseHistory`.

Conclusion remains unchanged: this is mapped-only evidence for enum naming.
The live debugger now proves the client can claim account items while the
duplicate-body handlers fire for the same dye/Holo object ids, but it still
does not prove distinct client enum owners for `245` or `275`. Keep
`Unknown245` and `Unknown275`; keep the server-side account-item contextual
handlers because the runtime account-item rows and live claim/take paths need
those gates.
