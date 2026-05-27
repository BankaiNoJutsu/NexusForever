# LaughingWS Live Event Smoke Review

Date: 2026-05-27

## Decision

LWS-054 is closed as mapped-only. The live-event seed preserves useful
branch-authored entity/vendor rows, but no event lifecycle behavior is promoted
or claimed retail-complete without live-client smoke.

Do not implement spawn timing, vendor timing, lifecycle activation, or cleanup
state from the branch SQL alone. Add runtime tests only after a captured bundle
proves a concrete lifecycle state machine or event-specific behavior.

## Preserved Data

`Tools/DataMapping/sql/laughingws_live_event_wip_seed.sql` preserves `440`
WIP/GUESSED rows:

| Table | Rows |
| --- | ---: |
| `entity` | 198 |
| `entity_stats` | 68 |
| `entity_vendor` | 8 |
| `entity_vendor_category` | 24 |
| `entity_vendor_item` | 142 |

The seed covers the plan's smoke targets with deterministic high entity ids in
the `2100000000` range:

| Source | Kept rows | Notes |
| --- | ---: | --- |
| `Events/Battle Chase.sql` | 55 | Source comments say the event was hand-authored from guesses without videos/screenshots. |
| `Events/Dungeon Chase.sql` | 77 | Entity/vendor rows are WIP; the hidden SMC store placeholder is rejected separately in `LAUGHINGWS_DUNGEON_CHASE_SMC_PLACEHOLDER_REVIEW.md`. |
| `Events/Shades Eve.sql` | 139 | Entity/vendor rows are WIP; one malformed vendor-item row is skipped by the extractor. |
| `Events/Sim Chase/Sim Chase 1.sql` | 61 | Sim Chase 2 / combined Sim Chase sources are duplicate-source rejections in the audit. |
| `Events/Space Chase.sql` | 48 | WIP entity/vendor overlay only. |
| `Events/Starfall/Starfall week 1.sql` | 22 | WIP weekly placement only. |
| `Events/Starfall/Starfall week 2.sql` | 6 | WIP weekly placement only. |
| `Events/Starfall/Starfall week 3.sql` | 6 | WIP weekly placement only. |
| `Events/Starfall/Starfall week 4.sql` | 6 | WIP weekly placement only. |
| `Events/Starfall/Starfall week 5.sql` | 6 | WIP weekly placement only. |
| `Events/zPrix.sql` | 14 | Source marks zPrix as WIP. |

## Source Hazards

`extract_laughingws_live_events.py` strips or skips data that is unsafe for
runtime promotion:

- raw `@GUID/MAX(id)` allocation is replaced by deterministic ids;
- event remover scripts are excluded;
- official duplicate rows are skipped instead of duplicated;
- placeholder-coordinate rows are skipped;
- duplicate source-coordinate rows are skipped;
- entity-owned stats/vendor/category/item rows are skipped when their entity is
  skipped;
- malformed unsupported vendor-item rows are left out.

The generated seed currently records skipped source hazards:

| Skip reason | Count |
| --- | ---: |
| Official duplicate rows | 3,740 |
| Placeholder event coordinates | 84 |
| Duplicate event coordinates | 50 |
| Event remover scripts | 1 |
| Stats for skipped entities | 234 |
| Vendor rows for skipped entities | 9 |
| Vendor categories for skipped entities | 11 |
| Vendor items for skipped entities | 130 |
| Malformed Shade's Eve vendor item | 1 |

## Evidence Boundary

The current repo state proves safe preservation only:

- the live-event extractor and seed mark all rows WIP/GUESSED;
- `Decomp/Analysis/LAUGHINGWS_WORLDDB_AND_MORE_AUDIT.md` records that
  Battle Chase, Dungeon Chase, Shades Eve, Space Chase, Starfall, Sim Chase 1,
  and zPrix still need lifecycle timing, cleanup semantics, and retail/client
  proof;
- current focused event scripts cover separate instance/event chains where
  tested, but they do not prove the live-event spawn schedule, vendor timing,
  activation windows, or cleanup behavior for these branch event overlays.

## Required Smoke Bundles

Capture one bundle per event family before removing WIP/GUESSED or implementing
runtime event state:

```powershell
.\Decomp\Analysis\Start-BlockerEvidenceHarness.ps1 `
  -BundleName LWS-054-battle-chase `
  -Notes "Battle Chase spawn timing, vendor timing, lifecycle activation, cleanup" `
  -NegativeCases "event inactive; event ending during vendor interaction; cleanup after event end"

.\Decomp\Analysis\Start-BlockerEvidenceHarness.ps1 `
  -BundleName LWS-054-dungeon-chase `
  -Notes "Dungeon Chase spawn timing, vendor timing, lifecycle activation, cleanup; hidden SMC placeholder remains rejected unless storefront schema proof changes" `
  -NegativeCases "inactive event; vendor after cleanup; hidden SMC placeholder unavailable"

.\Decomp\Analysis\Start-BlockerEvidenceHarness.ps1 `
  -BundleName LWS-054-shades-eve `
  -Notes "Shades Eve live-event entity/vendor timing, activation, cleanup, and interaction proof" `
  -NegativeCases "inactive event; post-cleanup interaction; malformed vendor row absence"

.\Decomp\Analysis\Start-BlockerEvidenceHarness.ps1 `
  -BundleName LWS-054-space-chase `
  -Notes "Space Chase spawn timing, vendor timing, lifecycle activation, cleanup" `
  -NegativeCases "inactive event; vendor after cleanup; duplicate-source coordinate absence"

.\Decomp\Analysis\Start-BlockerEvidenceHarness.ps1 `
  -BundleName LWS-054-starfall `
  -Notes "Starfall week 1-5 spawn timing, lifecycle activation, weekly selection, cleanup" `
  -NegativeCases "wrong week active; inactive event; cleanup after event end"

.\Decomp\Analysis\Start-BlockerEvidenceHarness.ps1 `
  -BundleName LWS-054-sim-chase-1 `
  -Notes "Sim Chase 1 spawn timing, vendor timing, lifecycle activation, cleanup" `
  -NegativeCases "inactive event; duplicate Sim Chase sources absent; vendor after cleanup"

.\Decomp\Analysis\Start-BlockerEvidenceHarness.ps1 `
  -BundleName LWS-054-zprix `
  -Notes "zPrix WIP source placement, spawn timing, lifecycle activation, cleanup" `
  -NegativeCases "inactive event; WIP-only placement absent after cleanup"
```

Each bundle must include event calendar/state setup, character/account state,
server logs, coordinates, screenshots/video, observed vendor rows where present,
activation/deactivation timestamps, and post-cleanup negative checks.
