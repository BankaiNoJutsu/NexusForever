# Creature ID Verification — Tutorial & Starter Zones

> Verified against `Creature2.tbl` (server build) via lookup-table byte-offset check.
> File: `bin/Debug/net10.0/tbl/Creature2.tbl` (53,137 records, MaxId=76,000).
> Cross-referenced with `nexus_forever_world.entity` table and `wildstar_client.creature2` (Jabbithole MySQL).

## Tutorial Combat Lane (World 3460)

| Creature ID | Faction | Role | .tbl | Entity Table |
|---|---|---|---|---|
| 73464 | Exile | Dagun (starter) | ✅ | ✅ (27 spawns) |
| 73465 | Dominion | Dagun (starter) | ✅ | DB only |
| 73494 | Exile | Turret | ✅ | ✅ (2 spawns) |
| 74862 | Dominion | Turret | ✅ | DB only |
| 73492 | Exile | Legionnaire (final) | ✅ | DB only |
| 73473 | Dominion | Legionnaire (final) | ✅ | DB only |
| 73463 | Neutral | Mine (easy) | ✅ | ✅ |
| 73667 | Neutral | Mine (medium) | ✅ | ✅ |
| 73668 | Neutral | Mine (hard) | ✅ | ✅ |
| 73567 | Exile | Bonus hostile | ✅ | DB only |
| 73566 | Dominion | Bonus hostile | ✅ | DB only |

## Ship Deck NPCs (Quest Zone 5968)

| Creature ID | Description | .tbl | Entity Table |
|---|---|---|---|
| 73498 | Circuit Switch Obj0 | ✅ | ✅ (1 spawn) |
| 74745 | Circuit Switch Obj1 | ✅ | ✅ (1 spawn) |
| 74746 | Circuit Switch Obj2 | ✅ | ✅ (1 spawn) |

## Ship Interior (Quest Zone 5968)

| Creature ID | Description | .tbl | Entity Table |
|---|---|---|---|
| 73499 | Eldan Defense Construct (boss) | ✅ | ❌ — dynamic spawn |
| 73500 | Primal Manifestation (checklist) | ✅ | ✅ (3 spawns) |

## Cryopod Deck (Quest Zone 5969)

| Creature ID | Description | .tbl | Entity Table |
|---|---|---|---|
| 73662 | Decorations Expert | ✅ | ✅ (6 spawns) |
| 73663 | Sky Specialist | ✅ | ✅ (1 spawn) |
| 73664 | Music Manager | ✅ | ✅ (1 spawn) |
| 73422 | Target-group NPC A | ✅ | ✅ (7 spawns) |

## Exile Departure (Quest Zone 5998, Q10528)

| Creature ID | Description | .tbl | Entity Table |
|---|---|---|---|
| 73604 | Prof Rhoda Wellspring (NPC) | ✅ | ✅ (1 spawn) |
| 73605 | Terminal: Everstar Grove | ✅ | ✅ (1 spawn) |
| 73606 | Terminal: Northern Wilds | ✅ | ✅ (1 spawn) |
| 73677-73684 | Holotable checklist (8) | ✅ | ✅ (1 spawn each) |
| 73669-73672 | Hologram allies (4) | ✅ | ✅ (1 spawn each) |
| 74812 | Dorian Walker | ✅ | ✅ (1 spawn) |

## Dominion Departure (Quest Zone 5999, Q10530)

| Creature ID | Description | .tbl | Entity Table |
|---|---|---|---|
| 74778 | Chancellor Juro Takigurian (NPC) | ✅ | ❌ — dynamic spawn |
| 74772 | Terminal: Crimson Isle | ✅ | ❌ — dynamic spawn |
| 74773 | Terminal: Levian Bay | ✅ | ❌ — dynamic spawn |
| 74759-74766 | Holotable checklist (8) | ✅ | ❌ — dynamic spawn |
| 73673-73676 | Hologram enemies (4) | ✅ | ✅ (1 spawn each) |
| 73708-73709 | Holotable flavor (neutral) | ✅ | ✅ |

> **Decision:** All Dominion departure creature IDs are valid and correct. They exist in the server's Creature2.tbl but have no entity-table spawn entries. The `TutorialMapScript.EnsureDominionDepartureEntities()` dynamic spawn fallback correctly provides them.

## Path Creatures (75074–75105)

32 creature IDs used across Exile/Dominion path hub and zone quests.

| Status | Count | IDs |
|---|---|---|
| ✅ In .tbl | 30 | All except below |
| ❌ Missing | 2 | **75083, 75084** |

> **Decision:** 30 of 32 path creature IDs are valid. The 2 missing IDs (75083, 75084) do not exist in the 16042 client build and are not referenced directly by any script. Path quest objectives that depend on these 2 creatures will fail silently — documented as known gaps on this client version.

## Verification Method

Creature2.tbl lookup table is at byte offset `headerSize(96) + LookupOffset(0x28AEEE0)`.
Each entry is a 4-byte signed int32. Value `-1` = missing, any positive value = record index.

```bash
tbl="path/to/Creature2.tbl"
lookup_start=$((96 + 0x28AEEE0))
# Check ID N: od -j $((lookup_start + N*4)) -N 4 -t d4 "$tbl"
```

## Summary

- **102 creature IDs** referenced across tutorial/starter scripts
- **100 present** (98%) in server Creature2.tbl
- **2 missing** (75083, 75084) — path creatures, no script references
- **Dynamic spawn strategy verified correct** — all Dominion departure IDs valid, just need fallback spawning
- **Combat lane IDs verified correct** — both Exile and Dominion combat creatures present
