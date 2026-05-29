# LaughingWS Skyplot Housing Review

Date: 2026-05-27

## Decision

LWS-053 remains mapped-only. The branch Skyplot rows remain useful WIP overlay
data, but they are not retail-complete housing behavior.

Do not promote these rows or related housing behavior beyond the current
WIP/GUESSED overlay until a local DB/client smoke bundle proves vendor
placement, return-pad interaction, catalogue completeness, and active Skyplot
community lifecycle behavior.

## Preserved Data

`Tools/DataMapping/sql/laughingws_housing_skyplot_wip_seed.sql` preserves `411`
rows from `Map/Instances/Housing/Skyplot.sql`:

| Table | Rows | Notes |
| --- | ---: | --- |
| `entity` | 3 | Return pad plus two Skyplot vendors in world `1229`, area `1136`. |
| `entity_stats` | 12 | Six stat rows for each vendor. |
| `entity_vendor` | 2 | Vendor price multipliers. |
| `entity_vendor_category` | 72 | `36` categories per vendor. |
| `entity_vendor_item` | 322 | Vendor catalogue rows from the branch source. |

Entity ids are deterministic DataMapping-owned ids:

| Seed id | Creature | Source role | Position |
| ---: | ---: | --- | --- |
| `2000000001` | `35298` | Return teleporter / pad, `activePropId` `4676188` | `1567.3907, -707.63617, 1323.1534` |
| `2000001001` | `68423` | Skyplot vendor, display `32799`, outfit `9902` | `1578.6104, -706.989, 1326.0824` |
| `2000002001` | `68424` | Skyplot vendor, display `28454`, outfit `9269` | `1578.6283, -706.9887, 1326.0425` |

## Source Hazards

The branch source starts with `DELETE FROM entity WHERE world = @WORLD` for
world `1229` and uses runtime `@GUID = MAX(id)` allocation. The generated seed
omits the destructive delete and replaces source ids with deterministic high
ids in the `2000000000` range.

The source vendor rows omit `OutfitInfo` from the column list while supplying
an outfit-like value before faction fields. `extract_laughingws_housing_skyplot.py`
corrects that one source shape and records the generated SQL as WIP/GUESSED.

## Evidence Boundary

Current repository evidence supports preserving the rows as branch-authored
overlay data only:

- `Tools/DataMapping/extract_laughingws_housing_skyplot.py` marks the output
  WIP/GUESSED and documents the destructive-delete and column-shape corrections.
- `Tools/DataMapping/sql/README.md` documents the `411` generated rows and the
  omitted source world replacement.
- `Decomp/Analysis/RETAIL_FEATURE_EVIDENCE.md` records general retail Skyplot
  community facts, but does not prove these exact branch vendor placements,
  return-pad interaction, or catalogue completeness.
- Existing housing/community packet work maps several community surfaces, but
  the active Skyplot lifecycle, vendor placement proof, and return-state
  behavior remain partial under F-004.

## Required Smoke Bundle

Use the blocker evidence harness before removing WIP/GUESSED or implementing
new behavior:

```powershell
.\Decomp\Analysis\Start-BlockerEvidenceHarness.ps1 `
  -SkyplotHousingSmoke `
  -ClientDirectory "I:\WildStar" `
  -PromptForRootPassword
```

The preset creates `lws-053-skyplot-housing-targets.md`, preloads world `1229`,
and configures negative cases for return-pad activation without a residence or
community session, vendor open before unlock, missing or invalid catalog rows,
and inactive Skyplot lifecycle. The bundle must capture character/account
state, loaded residence/community state, world coordinates, vendor-list rows,
return-pad activation result, server logs, screenshots/video, and negative
cases. Only rows or behavior proven by that bundle should be promoted beyond
WIP/GUESSED.

Dry-run guard (2026-05-28): `Decomp/Analysis/test_blocker_evidence_harness_presets.py`
now runs `-SkyplotHousingSmoke -CreateBundleOnly` and verifies the generated
manifest, default world id, target worksheet, helper files, and negative-case
scaffold. This remains capture infrastructure only; vendor placement,
catalogue completeness, return-pad interaction, and active Skyplot lifecycle
are still blocked pending client/server evidence.
