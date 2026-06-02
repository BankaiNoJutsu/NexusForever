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
| 260 | 1 | 0 | case `0x104`, vtable `+0x640` -> `1404a1220` | diagnostic field-owner blocker | State `5` correlates to `HousingBuildComplete`, but active residence fields `+0x60/+0x64` and row/use-site reachability remain unnamed. |
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
2. `260` stays mapped-only/blocked until active residence fields `+0x60/+0x64` and a reachable row/use-site are named.

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
that proves a more specific owner than the already-mapped body twin.

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

`Unknown260` remains intentionally unnamed. The housing plug state value `5`
correlates with native `HousingBuildComplete` paths, but the active residence
fields `+0x60/+0x64` and row/use-site reachability are still not named well
enough for a safe enum rename.
