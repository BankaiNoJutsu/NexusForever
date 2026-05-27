#!/usr/bin/env python3
"""Extract small WIP world-overlay entity rows from LaughingWS SQL."""

from __future__ import annotations

import argparse
import re
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable

from analyze_laughingws_worlddb import (
    INSERT_RE,
    normalise_column,
    read_text,
    split_top_level_commas,
    split_value_rows,
)


SET_WORLD_RE = re.compile(r"\bSET\s+@WORLD\s*:?=\s*(\d+)\s*;", re.IGNORECASE)
INSERT_ENTITY_RE = re.compile(
    r"\bINSERT\s+INTO\s+`?entity`?\s*\((?P<columns>.*?)\)\s*"
    r"VALUE(?:S)?\s*(?P<values>.*?);",
    re.IGNORECASE | re.DOTALL,
)

BASE_SMALL_WORLD_ENTITY_ID = 1_100_200_000
BASE_AURORIA_CHECKLIST_FALLBACK_ENTITY_ID = BASE_SMALL_WORLD_ENTITY_ID + 10_000
BASE_CRIMSON_ISLE_CHECKLIST_FALLBACK_ENTITY_ID = BASE_SMALL_WORLD_ENTITY_ID + 20_000
BASE_EVERSTAR_GROVE_CHECKLIST_FALLBACK_ENTITY_ID = BASE_SMALL_WORLD_ENTITY_ID + 30_000
BASE_LEVIAN_BAY_CHECKLIST_FALLBACK_ENTITY_ID = BASE_SMALL_WORLD_ENTITY_ID + 40_000
EVERSTAR_GROVE_SOURCE = "Map/Open World/Alizar/EverstarGrove.sql"
WILDERRUN_SOURCE = "Map/Open World/Olyssia/Wilderrun.sql"
AURORIA_SOURCE = "Map/Open World/Olyssia/Auroria.sql"
CRIMSON_ISLE_SOURCE = "Map/Open World/Olyssia/Crimson Isle.sql"
LEVIAN_BAY_SOURCE = "Map/Open World/Olyssia/Levian Bay.sql"
QUEST_CHECKLIST_UPDATE_EXPECTED_COUNTS = {
    EVERSTAR_GROVE_SOURCE: {
        28454: 2,
    },
    WILDERRUN_SOURCE: {
        38051: 30,
        40353: 1,
        40362: 1,
        40365: 1,
    },
    AURORIA_SOURCE: {
        12996: 5,
        13652: 7,
        24053: 4,
        24451: 1,
        24459: 3,
        24460: 4,
        24462: 1,
        24463: 6,
        25265: 3,
        25266: 1,
        25267: 3,
        26337: 7,
        27496: 2,
        27667: 5,
        30752: 8,
        35583: 7,
    },
    CRIMSON_ISLE_SOURCE: {
        24225: 2,
        24298: 3,
        24312: 8,
        24703: 6,
        24999: 3,
        26559: 2,
    },
    LEVIAN_BAY_SOURCE: {
        26016: 12,
        26417: 2,
    },
}

QUEST_CHECKLIST_UPDATE_EVIDENCE = {
    EVERSTAR_GROVE_SOURCE: (
        "official Everstar Grove questChecklistIdx reconciliation",
        "branch QuestChecklistIdx values are applied only to the two already-existing official "
        "Exo-Lab 71 Eldan Teleporter surface rows. Runtime coverage already maps creature 28454 "
        "through WorldLocationTeleporterEntityScript to worldLocation2 21760, and local Quest2/"
        "Jabbithole data has unmatched Arboretum entrance/access objectives; exact checklist "
        "smoke remains pending, so this stays WIP/GUESSED.",
    ),
    WILDERRUN_SOURCE: (
        "official Wilderrun questChecklistIdx reconciliation",
        "branch QuestChecklistIdx values line up with DataMapping quest/objective rows for "
        "Dominion History Lesson / Ancient Crash Site style interactables, but exact client smoke is still pending.",
    ),
    AURORIA_SOURCE: (
        "official Auroria questChecklistIdx reconciliation",
        "branch QuestChecklistIdx values are applied only to already-existing official rows, "
        "and Quest2/DataMapping evidence maps these creature groups to Auroria objectives such as "
        "Denial of Resources, Seismic Instability, The Savior of Hycrest, Ashes to Ashes, "
        "Resuming Production, Loading Zone: No Landing, Bingberry Bootlegging, and Running Dry. "
        "Residual branch-only Auroria rows remain blocked pending row-level/client smoke proof.",
    ),
    CRIMSON_ISLE_SOURCE: (
        "official Crimson Isle questChecklistIdx reconciliation",
        "branch QuestChecklistIdx values are applied only to already-existing official rows, "
        "and Quest2/DataMapping evidence maps these creature groups to Crimson Isle objectives "
        "for Data Grab, Tactical Demolitions, Fire and Brimstone, Ordnance Recovery, "
        "Powering Down / Seizing Power, and Enforced Radio Silence. The Crimson Isle quest "
        "script chain is covered, but exact checklist-objective smoke remains pending.",
    ),
    LEVIAN_BAY_SOURCE: (
        "official Levian Bay questChecklistIdx reconciliation",
        "branch QuestChecklistIdx values are applied only to already-existing official Signal "
        "Flare and Drop Pod Landing Beacon rows. DataMapping maps creature 26016 to the "
        "Lighting the Way objective and creature 26417 to Lost in the Fog; exact Levian Bay "
        "checklist smoke remains pending, so this stays WIP/GUESSED.",
    ),
}
QUEST_CHECKLIST_FALLBACK_ENTITY_ID_BASES = {
    AURORIA_SOURCE: BASE_AURORIA_CHECKLIST_FALLBACK_ENTITY_ID,
    CRIMSON_ISLE_SOURCE: BASE_CRIMSON_ISLE_CHECKLIST_FALLBACK_ENTITY_ID,
    EVERSTAR_GROVE_SOURCE: BASE_EVERSTAR_GROVE_CHECKLIST_FALLBACK_ENTITY_ID,
    LEVIAN_BAY_SOURCE: BASE_LEVIAN_BAY_CHECKLIST_FALLBACK_ENTITY_ID,
}

QUEST_CHECKLIST_LABELS = {
    28454: "Everstar Grove Exo-Lab 71 Eldan Teleporter",
    38051: "Kel Ulgar Weapon Rack",
    40353: "Tattered Banner",
    40362: "Intact Data Cache",
    40365: "Cassian Commonwealth Emblem",
    12996: "Auroria Black Hoods Alchemy Supplies",
    13652: "Auroria Cubig Security Zapper Console",
    24053: "Auroria Seismic Thumper",
    24451: "Auroria infected Hycrest citizen cluster",
    24459: "Auroria infected Hycrest citizen cluster",
    24460: "Auroria infected Hycrest citizen cluster",
    24462: "Auroria infected Hycrest citizen cluster",
    24463: "Auroria infected Hycrest citizen cluster",
    25265: "Auroria deceased plague victim cluster",
    25266: "Auroria deceased plague victim cluster",
    25267: "Auroria deceased plague victim cluster",
    26337: "Auroria Freebot Drill",
    27496: "Auroria Navigation Control Panel",
    27667: "Auroria Bingberry Still",
    30752: "Auroria GL-04 Auto Turret",
    35583: "Auroria GL-04 Auto Turret continuation",
    24225: "Crimson Isle Megatech Terminal",
    24298: "Crimson Isle Exile Anti-Air Cannon",
    24312: "Crimson Isle Dreg Tent",
    24703: "Crimson Isle Dominion Demolitions Expert",
    24999: "Crimson Isle Power Regulator",
    26559: "Crimson Isle Tower Controls",
    26016: "Levian Bay Signal Flare",
    26417: "Levian Bay Drop Pod Landing Beacon",
}

OUTPUT_COLUMNS = (
    "id",
    "type",
    "creature",
    "world",
    "area",
    "x",
    "y",
    "z",
    "rx",
    "ry",
    "rz",
    "displayInfo",
    "outfitInfo",
    "faction1",
    "faction2",
    "questChecklistIdx",
    "activePropId",
    "worldSocketId",
    "mode",
)

COLUMN_NAME_MAP = {
    "activepropid": "activePropId",
    "area": "area",
    "creature": "creature",
    "displayinfo": "displayInfo",
    "faction1": "faction1",
    "faction2": "faction2",
    "id": "id",
    "mode": "mode",
    "outfitinfo": "outfitInfo",
    "questchecklistidx": "questChecklistIdx",
    "rx": "rx",
    "ry": "ry",
    "rz": "rz",
    "type": "type",
    "world": "world",
    "worldsocketid": "worldSocketId",
    "x": "x",
    "y": "y",
    "z": "z",
}


@dataclass(frozen=True)
class OverlaySpec:
    source_file: str
    source_creature: int
    label: str
    overrides: tuple[tuple[str, str], ...]
    stats: tuple[tuple[int, int], ...]
    evidence: str
    source_match: tuple[tuple[str, str], ...] = ()


# WIP/GUESSED: most rows are promoted where the branch placement is corroborated
# by DataMapping creature/spawn evidence. A few explicitly source-only rows are
# also kept when the branch provides a narrow, non-destructive placement that is
# useful for currently reachable content. Those source-only rows are not retail
# parity proof; exact placement, stats, and interaction behavior stay blocked.
# Source health=1 rows and creature/area/display/outfit mismatches are reconciled
# to DataMapping output values rather than imported verbatim.
# Dominion/Exile Arkship tutorial rows are intentionally not extracted: build
# 16042 creation routes through Rider's Reef or surface starts, and the branch
# rows are hand-authored/TODO placements without DataMapping or runtime proof.
OVERLAY_SPECS = (
    OverlaySpec(
        "Map/Open World/Olyssia/Crimson Badlands.sql",
        36023,
        "Crimson Badlands Tactical Uplink",
        (("creature", "34050"), ("area", "1969")),
        ((0, 101), (10, 1), (20, 0), (21, 0)),
        "creature_map jabbithole 3047 and creature_spawn_map 13429 map Tactical Uplink to creature 34050, world 2997, area 1969, matching the source coordinates/display.",
    ),
    OverlaySpec(
        "Map/Open World/Alizar/Northern Wastes.sql",
        33966,
        "Northern Wastes Captain Darkstone",
        (),
        ((0, 48457), (10, 50), (20, 0), (21, 0)),
        "creature_map jabbithole 20512 and creature_spawn_map 1920900/2156553/2354985/3905157 map Captain Darkstone to creature 33966, world 1658, area 1787, matching the source coordinates/display/outfit.",
    ),
    OverlaySpec(
        "Map/Open World/Farside.sql",
        27404,
        "Farside Commander Durek",
        (("creature", "11061"), ("area", "1472"), ("outfitInfo", "9065")),
        ((0, 98971216), (10, 50), (20, 84971216), (21, 18)),
        "creature_map jabbithole 10100 and creature_spawn_map 130777 map Commander Durek to creature 11061, world 1421, area 1472, matching the source coordinates/display; outfit uses DataMapping default 9065 instead of source 9375.",
    ),
    OverlaySpec(
        "Map/Open World/Tideborn Facility.sql",
        30896,
        "Tideborn Cortex Prime Whitewater",
        (),
        ((0, 3656), (10, 12), (20, 810), (21, 0)),
        "creature_map jabbithole 11888 and creature_spawn_map 242797 map Cortex Prime Whitewater to creature 30896, world 2161, area 1979, matching the source coordinates/display.",
    ),
    OverlaySpec(
        "Map/Open World/Tideborn Facility.sql",
        31899,
        "Tideborn Corrigan Doon",
        (("creature", "56858"),),
        ((0, 98971216), (10, 50), (20, 84971216), (21, 18)),
        "creature_map jabbithole 11728 and creature_spawn_map 222393 map Corrigan Doon to creature 56858, world 2161, area 1979, matching the source coordinates/display/outfit.",
    ),
    OverlaySpec(
        "Map/Open World/Olyssia/Star-Comm Basin.sql",
        58369,
        "Star-Comm Basin The Caretaker",
        (("creature", "18409"), ("displayInfo", "24983")),
        ((0, 98971216), (10, 50), (20, 84971216), (21, 18)),
        "creature_map jabbithole 31150 and creature_spawn_map 7081601 map The Caretaker to creature 18409, world 2979, area 4206, matching the source coordinates; display uses DataMapping default 24983 instead of source 25105.",
    ),
    OverlaySpec(
        "Map/Open World/Arcterra.sql",
        70779,
        "Arcterra The Caretaker",
        (),
        ((0, 98971220), (10, 50), (15, 0), (20, 84971220), (21, 18), (22, 0)),
        "source-only WIP/GUESSED: the branch adds a single Caretaker placement in reachable world 3335; exact retail placement and stats are not DataMapping-corroborated.",
    ),
    OverlaySpec(
        "Map/Open World/Arcterra.sql",
        75701,
        "Arcterra Coldblood Citadel entrance portal",
        (),
        (),
        "source-only WIP/GUESSED: the branch adds a single Coldblood Citadel portal placement in reachable world 3335; exact interaction target, placement, and portal behavior need client/table proof.",
    ),
    OverlaySpec(
        "Map/Open World/Palaver Point.sql",
        75343,
        "Palaver Point Ish'amel the Bloodied",
        (),
        ((0, 18715), (10, 50), (12, 0), (13, 0), (14, 5727544), (15, 1), (20, 4748), (21, 0), (22, 0)),
        "source-only WIP/GUESSED: the branch adds one Palaver Point Ish'amel placement and quest evidence proves Palaver objectives, but this creature placement/stats still need manual smoke proof.",
    ),
    OverlaySpec(
        "Map/Open World/Alizar/Northern Wilds.sql",
        11063,
        "Northern Wilds Deadeye Brightland landing-site quest giver",
        (("area", "597"), ("rx", "0"), ("ry", "0"), ("rz", "0")),
        ((0, 98971216), (10, 50), (15, 0), (20, 84971216), (21, 18), (22, 0)),
        "WIP/GUESSED: DataMapping reviews jabbithole creature 1073 as current Deadeye Brightland creature 11063 in world 426, and source coordinate 4368 matches this branch placement; exact quest-giver availability still needs Northern Wilds smoke.",
        (("x", "4341.38"), ("y", "-751.761"), ("z", "-5683.05")),
    ),
    OverlaySpec(
        "Map/Open World/Alizar/Northern Wilds.sql",
        12484,
        "Northern Wilds Scientist Lusk Q3668 objective NPC",
        (("rx", "0"),),
        ((0, 1360), (10, 6), (20, 0), (21, 0)),
        "WIP/GUESSED: DataMapping maps jabbithole creature 1043 to current creature 12484 in world 426 area 604, and quest 3668 has an unmatched find-Scientist-Lusk objective before the Coldburrow objectives.",
    ),
    OverlaySpec(
        "Map/Open World/Alizar/Northern Wilds.sql",
        12521,
        "Northern Wilds Signal Flare 1 Q3673 objective",
        (("rx", "0"), ("ry", "0"), ("rz", "0")),
        (),
        "WIP/GUESSED: quest 3673 objective 4888 targets creature 12521 and the branch supplies the missing checklist-indexed interactable near WorldLocation2 8867.",
    ),
    OverlaySpec(
        "Map/Open World/Alizar/Northern Wilds.sql",
        12737,
        "Northern Wilds Elder Bartol Q3668 turn-in NPC",
        (("creature", "12737"), ("rx", "0"), ("ry", "0"), ("rz", "0")),
        ((0, 269), (10, 3), (20, 0), (21, 0)),
        "WIP/GUESSED: current Creature2 12737 receives quest 3668 and DataMapping has a matching world 426 source coordinate for Bartol Sunward at this placement.",
    ),
    OverlaySpec(
        "Map/Open World/Alizar/Northern Wilds.sql",
        12959,
        "Northern Wilds Deadeye Brightland Camp Icefury quest giver",
        (("creature", "12959"), ("rx", "0"), ("ry", "0"), ("rz", "0")),
        ((0, 98971216), (10, 50), (15, 0), (20, 84971216), (21, 18), (22, 0)),
        "WIP/GUESSED: current Creature2 12959 is the Camp Icefury Deadeye Brightland quest giver/receiver for quests 3487, 3963, 3670, and 3673; the branch placement matches a DataMapping Deadeye coordinate but exact creature-id split needs live quest smoke.",
    ),
    OverlaySpec(
        "Map/Open World/Alizar/Northern Wilds.sql",
        13150,
        "Northern Wilds Signal Flare 2 Q3673 objective",
        (("rx", "0"), ("ry", "0"), ("rz", "0")),
        (),
        "WIP/GUESSED: quest 3673 objective 4889 targets creature 13150 and the branch supplies the missing checklist-indexed interactable near WorldLocation2 8868.",
    ),
    OverlaySpec(
        "Map/Open World/Alizar/Northern Wilds.sql",
        13151,
        "Northern Wilds Signal Flare 3 Q3673 objective",
        (("rx", "0"), ("ry", "0"), ("rz", "0")),
        (),
        "WIP/GUESSED: quest 3673 objective 4890 targets creature 13151 and the branch supplies the missing checklist-indexed interactable near WorldLocation2 8869.",
    ),
)


@dataclass(frozen=True)
class EntityRow:
    source_file: str
    source_index: int
    source_id: str
    values: dict[str, str]

    @property
    def creature(self) -> int:
        return int(self.values["creature"], 0)


@dataclass(frozen=True)
class OverlayRow:
    entity_id: int
    spec: OverlaySpec
    source_row: EntityRow
    values: dict[str, str]


@dataclass(frozen=True)
class OfficialQuestChecklistUpdate:
    source_row: EntityRow
    fallback_entity_id: int | None = None
    fallback_stats: tuple[tuple[str, str], ...] = ()

    @property
    def creature(self) -> int:
        return self.source_row.creature

    @property
    def quest_checklist_idx(self) -> int:
        return int(self.source_row.values["questChecklistIdx"], 0)

    @property
    def active_prop_id(self) -> int:
        return int(self.source_row.values["activePropId"], 0)

    @property
    def world(self) -> int:
        return int(self.source_row.values["world"], 0)

    @property
    def label(self) -> str:
        return QUEST_CHECKLIST_LABELS.get(self.creature, f"creature {self.creature}")


def default_laughingws_path(repo_root: Path) -> Path:
    return repo_root / "artifacts" / "external" / "LaughingWS.WorldDatabase.New-Zones-and-more"


def normalise_source_column(column: str) -> str:
    name = normalise_column(column)
    return COLUMN_NAME_MAP.get(name.lower(), name)


def resolve_token(token: str, world_id: int | None, guid_scope: str | None = None) -> str:
    value = token.strip()
    if value.upper() == "@WORLD":
        if world_id is None:
            raise ValueError("@WORLD used before a SET @WORLD declaration")
        return str(world_id)
    if guid_scope is not None:
        guid_match = re.fullmatch(r"@GUID\s*\+\s*(\d+)", value, re.IGNORECASE)
        if guid_match:
            return f"{guid_scope}+{guid_match.group(1)}"
        if re.fullmatch(r"@GUID", value, re.IGNORECASE):
            return f"{guid_scope}+0"
    return value


def parse_world_variable(text: str) -> int | None:
    match = SET_WORLD_RE.search(text)
    if match:
        return int(match.group(1))
    return None


def parse_entity_and_stat_rows(
    root: Path,
    relative_source: str,
) -> tuple[list[EntityRow], dict[str, tuple[tuple[str, str], ...]]]:
    path = root / Path(relative_source)
    text = read_text(path)
    world_id: int | None = None
    guid_scope_index = 0
    rows: list[EntityRow] = []
    stats_by_source_id: dict[str, list[tuple[str, str]]] = {}
    token_re = re.compile(
        r"(?is)("
        r"SET\s+@WORLD\s*:?=\s*\d+\s*;"
        r"|SET\s+@GUID\s*=.*?;"
        r"|(?:INSERT(?:\s+IGNORE)?|REPLACE)\s+INTO\s+`?[A-Za-z0-9_]+`?\s*"
        r"\(.*?\)\s*VALUE(?:S)?\s*.*?;"
        r")"
    )

    for token_match in token_re.finditer(text):
        statement = token_match.group(1)
        world_match = SET_WORLD_RE.search(statement)
        if world_match:
            world_id = int(world_match.group(1))
            continue

        if re.search(r"\bSET\s+@GUID\s*=", statement, re.IGNORECASE):
            guid_scope_index += 1
            continue

        insert_match = INSERT_RE.match(statement)
        if not insert_match:
            continue

        table = insert_match.group(1).lower()
        if table not in {"entity", "entity_stats"}:
            continue

        columns = [
            normalise_source_column(column)
            for column in split_top_level_commas(insert_match.group(2))
        ]

        guid_scope = f"g{guid_scope_index}"
        for row_sql in split_value_rows(insert_match.group(3)):
            raw_values = split_top_level_commas(row_sql)
            if len(raw_values) != len(columns):
                raise ValueError(
                    f"{relative_source}: value count {len(raw_values)} does not match column count {len(columns)}"
                )

            values = {
                column: resolve_token(raw_values[index], world_id, guid_scope)
                for index, column in enumerate(columns)
            }

            if table == "entity_stats":
                source_id = values.get("id", "")
                stats_by_source_id.setdefault(source_id, []).append(
                    (values.get("stat", "0"), values.get("value", "0"))
                )
                continue

            if "creature" not in values or "world" not in values:
                continue

            for column in OUTPUT_COLUMNS:
                values.setdefault(column, "0")

            rows.append(EntityRow(relative_source, len(rows), values.get("id", ""), values))

    stats = {
        source_id: tuple(source_stats)
        for source_id, source_stats in stats_by_source_id.items()
    }
    return rows, stats


def parse_entity_rows(root: Path, relative_source: str) -> list[EntityRow]:
    rows, _ = parse_entity_and_stat_rows(root, relative_source)
    return rows


def build_overlay_rows(root: Path) -> list[OverlayRow]:
    rows_by_source: dict[str, list[EntityRow]] = {}
    overlay_rows: list[OverlayRow] = []

    for index, spec in enumerate(OVERLAY_SPECS):
        source_path = root / Path(spec.source_file)
        if not source_path.is_file():
            raise FileNotFoundError(f"LaughingWS source does not exist: {source_path}")

        if spec.source_file not in rows_by_source:
            rows_by_source[spec.source_file] = parse_entity_rows(root, spec.source_file)

        matches = [
            row
            for row in rows_by_source[spec.source_file]
            if row.creature == spec.source_creature
            and all(row.values.get(column) == value for column, value in spec.source_match)
        ]
        if len(matches) != 1:
            raise ValueError(
                f"{spec.source_file}: expected 1 row for creature {spec.source_creature}, found {len(matches)}"
            )

        values = dict(matches[0].values)
        for column, value in spec.overrides:
            values[column] = value

        overlay_rows.append(
            OverlayRow(
                entity_id=BASE_SMALL_WORLD_ENTITY_ID + index + 1,
                spec=spec,
                source_row=matches[0],
                values=values,
            )
        )

    return overlay_rows


def build_source_quest_checklist_updates(
    root: Path,
    source_file: str,
    expected_counts: dict[int, int],
) -> list[OfficialQuestChecklistUpdate]:
    all_rows, stats_by_source_id = parse_entity_and_stat_rows(root, source_file)
    rows = [
        row
        for row in all_rows
        if row.creature in expected_counts
        and int(row.values["questChecklistIdx"], 0) > 0
    ]

    actual_counts = {
        creature: sum(1 for row in rows if row.creature == creature)
        for creature in expected_counts
    }
    if actual_counts != expected_counts:
        raise ValueError(
            f"{source_file}: expected questChecklistIdx update counts {expected_counts}, found {actual_counts}"
        )

    fallback_base = QUEST_CHECKLIST_FALLBACK_ENTITY_ID_BASES.get(source_file)
    updates: list[OfficialQuestChecklistUpdate] = []
    for index, row in enumerate(rows):
        fallback_entity_id = fallback_base + index + 1 if fallback_base is not None else None
        updates.append(
            OfficialQuestChecklistUpdate(
                row,
                fallback_entity_id,
                stats_by_source_id.get(row.source_id, ()),
            )
        )

    return updates


def build_official_quest_checklist_updates(root: Path) -> list[OfficialQuestChecklistUpdate]:
    updates: list[OfficialQuestChecklistUpdate] = []
    for source_file, expected_counts in QUEST_CHECKLIST_UPDATE_EXPECTED_COUNTS.items():
        updates.extend(build_source_quest_checklist_updates(root, source_file, expected_counts))
    return updates


def sql_identifier(name: str) -> str:
    return "`" + name.replace("`", "``") + "`"


def sql_row(values: Iterable[str]) -> str:
    return "(" + ", ".join(values) + ")"


def duplicate_update_clause(columns: Iterable[str], key_columns: set[str]) -> str:
    assignments = [
        f"{sql_identifier(column)} = VALUES({sql_identifier(column)})"
        for column in columns
        if column not in key_columns
    ]
    return "ON DUPLICATE KEY UPDATE " + ", ".join(assignments) + ";"


def append_insert_block(
    lines: list[str],
    table: str,
    columns: tuple[str, ...],
    values: list[list[str]],
    key_columns: set[str],
) -> None:
    if not values:
        return

    column_sql = ", ".join(sql_identifier(column) for column in columns)
    lines.append(f"-- {table}: {len(values)} row(s)")
    lines.append(f"INSERT INTO {sql_identifier(table)} ({column_sql}) VALUES")
    for index, row in enumerate(values):
        suffix = "," if index + 1 < len(values) else ""
        lines.append(f"  {sql_row(row)}{suffix}")
    lines.append(duplicate_update_clause(columns, key_columns))
    lines.append("")


def build_entity_values(rows: list[OverlayRow]) -> list[list[str]]:
    return [
        [str(row.entity_id)] + [row.values[column] for column in OUTPUT_COLUMNS[1:]]
        for row in rows
    ]


def build_entity_stat_values(rows: list[OverlayRow]) -> list[list[str]]:
    output_rows: list[list[str]] = []
    for row in rows:
        for stat, value in row.spec.stats:
            output_rows.append([str(row.entity_id), str(stat), str(value)])
    return output_rows


def append_update_block(
    lines: list[str],
    source_file: str,
    updates: list[OfficialQuestChecklistUpdate],
) -> None:
    if not updates:
        return

    title, evidence = QUEST_CHECKLIST_UPDATE_EVIDENCE[source_file]
    lines.append(f"-- {title}: {len(updates)} row(s)")
    lines.append(f"-- WIP/GUESSED: {evidence}")
    if any(update.fallback_entity_id is not None for update in updates):
        lines.append(
            "-- WIP/GUESSED: fallback INSERT statements add exact branch/official-matching rows "
            "only when an older local import lacks the official source row."
        )
    for update in updates:
        values = update.source_row.values
        where_parts = [
            f"`world` = {values['world']}",
            f"`creature` = {values['creature']}",
            f"ABS(`x` - ({values['x']})) < 0.001",
            f"ABS(`y` - ({values['y']})) < 0.001",
            f"ABS(`z` - ({values['z']})) < 0.001",
        ]
        if update.active_prop_id:
            where_parts.append(f"`activePropId` = {values['activePropId']}")

        lines.append(
            f"--   {update.label}: questChecklistIdx {update.quest_checklist_idx}; source {source_file}"
        )
        lines.append(
            "UPDATE `entity` SET "
            f"`questChecklistIdx` = {update.quest_checklist_idx} "
            "WHERE " + " AND ".join(where_parts) + ";"
        )
        if update.fallback_entity_id is None:
            continue

        fallback_values = {
            column: values.get(column, "0")
            for column in OUTPUT_COLUMNS
        }
        fallback_values["id"] = str(update.fallback_entity_id)
        fallback_values["questChecklistIdx"] = str(update.quest_checklist_idx)
        lines.append(
            "INSERT INTO `entity` ("
            + ", ".join(sql_identifier(column) for column in OUTPUT_COLUMNS)
            + ")"
        )
        lines.append(
            "SELECT "
            + ",".join(fallback_values[column] for column in OUTPUT_COLUMNS)
            + " WHERE NOT EXISTS (SELECT 1 FROM `entity` WHERE "
            + " AND ".join(where_parts)
            + ");"
        )
        if update.fallback_stats:
            lines.append("INSERT INTO `entity_stats` (`id`, `stat`, `value`)")
            lines.append("SELECT `id`, `stat`, `value` FROM (")
            for stat_index, (stat, value) in enumerate(update.fallback_stats):
                prefix = "  SELECT " if stat_index == 0 else "  UNION ALL SELECT "
                lines.append(
                    f"{prefix}{update.fallback_entity_id} AS `id`, {stat} AS `stat`, {value} AS `value`"
                    if stat_index == 0
                    else f"{prefix}{update.fallback_entity_id}, {stat}, {value}"
                )
            lines.append(
                ") AS `fallback_stats` "
                f"WHERE EXISTS (SELECT 1 FROM `entity` WHERE `id` = {update.fallback_entity_id}) "
                "ON DUPLICATE KEY UPDATE `value` = VALUES(`value`);"
            )
    lines.append("")


def append_quest_checklist_update_blocks(
    lines: list[str],
    updates: list[OfficialQuestChecklistUpdate],
) -> None:
    for source_file in QUEST_CHECKLIST_UPDATE_EXPECTED_COUNTS:
        source_updates = [
            update
            for update in updates
            if update.source_row.source_file == source_file
        ]
        append_update_block(lines, source_file, source_updates)


def describe_overrides(spec: OverlaySpec) -> str:
    if not spec.overrides:
        return f"source creature {spec.source_creature}"

    parts = [f"{column} -> {value}" for column, value in spec.overrides]
    return f"source creature {spec.source_creature}; overrides " + ", ".join(parts)


def write_seed(
    output_path: Path,
    rows: list[OverlayRow],
    official_updates: list[OfficialQuestChecklistUpdate],
) -> None:
    output_path.parent.mkdir(parents=True, exist_ok=True)

    lines = [
        "-- Generated by Tools/DataMapping/extract_laughingws_small_world_overlays.py.",
        "-- Source: selected small LaughingWS world overlay entity rows with DataMapping corroboration.",
        "-- Purpose: preserve useful one-off placed entities without importing broad world replacement SQL or source deletes.",
        "-- WIP/GUESSED: rows remain branch-authored and need manual client smoke before retail placement parity is claimed.",
        "-- WIP/GUESSED: DataMapping-corroborated rows reconcile source creature/area/display/outfit/stat mismatches to creature_map and creature_spawn_map evidence below.",
        "-- WIP/GUESSED: source-only rows are explicitly called out and remain blocked for exact placement/stat/interaction proof.",
        "-- WIP/GUESSED: official-row questChecklistIdx updates are coordinate-keyed and keep broad world replacement SQL blocked.",
        "",
        "USE `nexus_forever_world`;",
        "",
        "START TRANSACTION;",
        "",
        "-- Entity evidence and WIP overrides:",
    ]

    for row in rows:
        lines.append(
            f"--   {row.entity_id}: {row.spec.label}; {describe_overrides(row.spec)}; {row.spec.evidence}"
        )
    lines.append("")

    append_insert_block(lines, "entity", OUTPUT_COLUMNS, build_entity_values(rows), {"id"})
    append_insert_block(lines, "entity_stats", ("id", "stat", "value"), build_entity_stat_values(rows), {"id", "stat"})
    append_quest_checklist_update_blocks(lines, official_updates)

    lines.append("-- Sources:")
    sources = {row.spec.source_file for row in rows}
    sources.update(update.source_row.source_file for update in official_updates)
    for source in sorted(sources):
        lines.append(f"--   {source}")
    lines.extend(["", "COMMIT;", ""])

    output_path.write_text("\n".join(lines), encoding="ascii", newline="\n")


def parse_args() -> argparse.Namespace:
    repo_root = Path(__file__).resolve().parents[2]
    parser = argparse.ArgumentParser(
        description="Extract WIP LaughingWS small world overlay rows into a safe seed.",
    )
    parser.add_argument(
        "--laughingws-path",
        type=Path,
        default=default_laughingws_path(repo_root),
        help="Path to LaughingWS/NexusForever.WorldDatabase.New-Zones-and-more.",
    )
    parser.add_argument(
        "--output-sql",
        type=Path,
        default=repo_root / "Tools" / "DataMapping" / "sql" / "laughingws_small_world_wip_seed.sql",
        help="Output SQL seed path.",
    )
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    laughingws_path = args.laughingws_path.resolve()
    if not laughingws_path.is_dir():
        raise SystemExit(f"LaughingWS path does not exist: {laughingws_path}")

    rows = build_overlay_rows(laughingws_path)
    official_updates = build_official_quest_checklist_updates(laughingws_path)
    write_seed(args.output_sql, rows, official_updates)

    stat_count = sum(len(row.spec.stats) for row in rows)
    print(
        f"Wrote {len(rows)} WIP small-world entity row(s), {stat_count} entity_stats row(s), "
        f"and {len(official_updates)} official questChecklistIdx update statement(s) "
        f"to {args.output_sql.resolve()}."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
