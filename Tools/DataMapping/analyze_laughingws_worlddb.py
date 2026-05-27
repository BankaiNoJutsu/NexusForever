#!/usr/bin/env python3
"""
Audit LaughingWS/NexusForever.WorldDatabase.New-Zones-and-more for safe intake.

The LaughingWS repository is a useful source of hand-authored world data, but
many files are full replacement dumps. This tool inventories the SQL files,
compares them with the official NexusForever.WorldDatabase checkout when
available, and writes review artifacts that identify which files need curated
overlay extraction instead of raw setup import.
"""

from __future__ import annotations

import argparse
import csv
import hashlib
import json
import re
import sys
from collections import Counter, defaultdict
from dataclasses import dataclass, field
from pathlib import Path
from typing import Iterable, Optional


INSERT_RE = re.compile(
    r"\b(?:INSERT(?:\s+IGNORE)?|REPLACE)\s+INTO\s+`?([A-Za-z0-9_]+)`?\s*"
    r"\((.*?)\)\s*VALUE(?:S)?\s*(.*?);",
    re.IGNORECASE | re.DOTALL,
)
WORLD_RE = re.compile(r"(?im)^\s*SET\s+@WORLD\s*=\s*(\d+)\s*;")
EVENT_RE = re.compile(r"(?im)^\s*SET\s+@EVENTID\s*=\s*(\d+)\s*;")
DELETE_WORLD_RE = re.compile(
    r"(?is)\bDELETE\s+FROM\s+`?entity`?\s+WHERE\s+`?world`?\s*=\s*@WORLD\b"
)
DELETE_EVENT_RE = re.compile(
    r"(?is)\bDELETE\s+FROM\s+`?entity_event`?\s+WHERE\s+`?eventId`?\s*=\s*@EVENTID\b"
)
DDL_PATTERNS = {
    "drop_table": re.compile(r"(?i)\bDROP\s+TABLE\b"),
    "create_table": re.compile(r"(?i)\bCREATE\s+TABLE\b"),
    "alter_table": re.compile(r"(?i)\bALTER\s+TABLE\b"),
    "truncate_table": re.compile(r"(?i)\bTRUNCATE\s+TABLE\b"),
}
FOREIGN_KEY_OFF_RE = re.compile(r"(?i)\bFOREIGN_KEY_CHECKS\s*=\s*0\b")


WORLD_COLUMN_MAP = {
    "activepropid": "activePropId",
    "area": "area",
    "creature": "creature",
    "displayinfo": "displayInfo",
    "eventid": "eventId",
    "faction1": "faction1",
    "faction2": "faction2",
    "field_6": "field_6",
    "field_7": "field_7",
    "field_14": "field_14",
    "fx": "fx",
    "fy": "fy",
    "fz": "fz",
    "id": "id",
    "index": "index",
    "itemid": "itemId",
    "lootgroupid": "lootGroupId",
    "mapid": "mapId",
    "mode": "mode",
    "outfitinfo": "outfitInfo",
    "parentid": "parentId",
    "phase": "phase",
    "property": "property",
    "questchecklistidx": "questChecklistIdx",
    "rx": "rx",
    "ry": "ry",
    "rz": "rz",
    "scriptname": "scriptName",
    "speed": "speed",
    "splineid": "splineId",
    "stat": "stat",
    "team": "team",
    "triggerid": "triggerId",
    "type": "type",
    "value": "value",
    "world": "world",
    "worldlocationid": "worldLocationId",
    "worldsocketid": "worldSocketId",
    "x": "x",
    "y": "y",
    "z": "z",
}


# Current WorldContext runtime tables. This is intentionally local and static:
# the tool is an offline triage pass, not a database mutator.
WORLD_SCHEMA = {
    "creature_info_property": {"id", "property", "value"},
    "creature_info_stat": {"id", "stat", "value"},
    "creature_loot": {
        "creatureid",
        "itemid",
        "chance",
        "droptimes",
        "aggregatedropsum",
        "aggregatedropcount",
        "gameversion",
        "sourcedropid",
        "versioneditemdropaggregateid",
        "versionedcreaturedropaggregateid",
        "lastseenin",
        "matchstatus",
        "sourcename",
        "itemname",
    },
    "disable": {"type", "objectid", "note"},
    "entity": {
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
        "displayinfo",
        "outfitinfo",
        "faction1",
        "faction2",
        "questchecklistidx",
        "activepropid",
        "worldsocketid",
        "mode",
    },
    "entity_event": {"id", "eventid", "phase"},
    "entity_loot": {"id", "lootgroupid", "comment"},
    "entity_property": {"id", "property", "value"},
    "entity_script": {"id", "scriptname"},
    "entity_spline": {"id", "splineid", "mode", "speed", "fx", "fy", "fz"},
    "entity_stats": {"id", "stat", "value"},
    "entity_vendor": {"id", "buypricemultiplier", "sellpricemultiplier"},
    "entity_vendor_category": {"id", "index", "localisedtextid"},
    "entity_vendor_item": {
        "id",
        "index",
        "categoryindex",
        "itemid",
        "extracost1itemorcurrencyid",
        "extracost1quantity",
        "extracost1type",
        "extracost2itemorcurrencyid",
        "extracost2quantity",
        "extracost2type",
    },
    "item_loot": {"id", "lootgroupid", "comment"},
    "loot_group": {
        "id",
        "parentid",
        "probability",
        "mindrop",
        "maxdrop",
        "conditiontype",
        "condition",
        "comment",
    },
    "loot_item": {
        "id",
        "type",
        "staticid",
        "probability",
        "mincount",
        "maxcount",
        "comment",
    },
    "map_entrance": {"mapid", "team", "worldlocationid"},
    "store_category": {"id", "parentid", "name", "description", "index", "visible"},
    "store_offer_group": {
        "id",
        "displayflags",
        "name",
        "description",
        "displayinfooverride",
        "visible",
    },
    "store_offer_group_category": {"id", "categoryid", "index", "visible"},
    "store_offer_item": {
        "id",
        "groupid",
        "name",
        "description",
        "displayflags",
        "field_6",
        "field_7",
        "visible",
    },
    "store_offer_item_data": {"id", "itemid", "type", "amount"},
    "store_offer_item_price": {
        "id",
        "currencyid",
        "price",
        "discounttype",
        "discountvalue",
        "field_14",
        "expiry",
    },
    "tutorial": {"id", "type", "triggerid", "note"},
    "version": {"filename", "filehash", "appliedon"},
}


STORE_TABLES = {
    "store_category",
    "store_offer_group",
    "store_offer_group_category",
    "store_offer_item",
    "store_offer_item_data",
    "store_offer_item_price",
}

LEGACY_TABLE_ALIASES = {
    # LaughingWS Dungeon Chase uses these older/hand-written store table names.
    # Treat them as current-schema names for audit purposes; the extractor still
    # emits WIP/GUESSED comments for the salvaged hidden store row.
    "store_offer_category": "store_offer_group_category",
    "store_offer_data": "store_offer_item_data",
}

LOOT_TABLES = {"entity_loot", "item_loot", "loot_group", "loot_item"}
WORLD_ENTITY_TABLES = {
    "entity",
    "entity_event",
    "entity_property",
    "entity_script",
    "entity_spline",
    "entity_stats",
    "entity_vendor",
    "entity_vendor_category",
    "entity_vendor_item",
    "map_entrance",
    "creature_info_property",
    "creature_info_stat",
}

STORE_CATALOG_SEED_SOURCES = {
    "events/dungeon chase.sql",
    "events/shades eve.sql",
    "events/winterfest.sql",
    "store/store events/valentine's day items.sql",
    "store/storecatalog.sql",
}
QUEST_LOOT_SEED_SOURCES = {"loot/creaturequestloot.sql"}
CITY_CONTENT_SEED_SOURCES = {
    "map/open world/alizar/z thayd halon ring update.sql",
    "map/open world/olyssia/illium museum.sql",
    "map/open world/skulls eye.sql",
}
PARTIAL_CITY_CONTENT_SEED_SOURCES = {
    "map/open world/alizar/thayd.sql",
    "map/open world/new zones/halon ring.sql",
    "map/open world/olyssia/illium.sql",
    "map/open world/olyssia/z illium halon ring update.sql",
}
REJECTED_CITY_CONTENT_PLACEHOLDER_SOURCES = {
    "map/open world/olyssia/z illium halon ring update.sql",
}
REJECTED_CITY_CONTENT_ORPHAN_VENDOR_SOURCES = {
    "map/open world/olyssia/illium.sql",
}
BLOCKED_THAYD_RESIDUAL_SOURCES = {
    "map/open world/alizar/thayd.sql",
}
QUEST_INSTANCE_WIP_SEED_SOURCES = {"map/open world/the dust stalker.sql"}
SMALL_WORLD_WIP_SEED_SOURCES = {
    "map/open world/alizar/northern wastes.sql",
    "map/open world/arcterra.sql",
    "map/open world/farside.sql",
    "map/open world/olyssia/crimson badlands.sql",
    "map/open world/olyssia/star-comm basin.sql",
    "map/open world/palaver point.sql",
    "map/open world/tideborn facility.sql",
}
PARTIAL_SMALL_WORLD_WIP_SEED_SOURCES = {
    "map/open world/alizar/everstargrove.sql",
    "map/open world/olyssia/auroria.sql",
    "map/open world/olyssia/crimson isle.sql",
    "map/open world/olyssia/levian bay.sql",
    "map/open world/alizar/northern wilds.sql",
    "map/open world/olyssia/wilderrun.sql",
}
BLOCKED_EVERSTAR_GROVE_RESIDUAL_SOURCES = {
    "map/open world/alizar/everstargrove.sql",
}
BLOCKED_AURORIA_RESIDUAL_SOURCES = {
    "map/open world/olyssia/auroria.sql",
}
BLOCKED_CRIMSON_ISLE_RESIDUAL_SOURCES = {
    "map/open world/olyssia/crimson isle.sql",
}
BLOCKED_LEVIAN_BAY_RESIDUAL_SOURCES = {
    "map/open world/olyssia/levian bay.sql",
}
BLOCKED_DERADUNE_RESIDUAL_SOURCES = {
    "map/open world/olyssia/deradune.sql",
}
BLOCKED_ELLEVAR_RESIDUAL_SOURCES = {
    "map/open world/olyssia/ellevar.sql",
}
BLOCKED_WILDERRUN_DORIAN_SOURCES = {
    "map/open world/olyssia/wilderrun.sql",
}
BLOCKED_NORTHERN_WILDS_RESIDUAL_SOURCES = {
    "map/open world/alizar/northern wilds.sql",
}
REJECTED_NORTHERN_WILDS_VENDOR_SOURCE_ROWS = {
    "map/open world/alizar/northern wilds.sql",
}
BLOCKED_ALGOROC_RESIDUAL_SOURCES = {
    "map/open world/alizar/algoroc.sql",
}
BLOCKED_CELESTION_RESIDUAL_SOURCES = {
    "map/open world/alizar/celestion.sql",
}
BLOCKED_GALERAS_RESIDUAL_SOURCES = {
    "map/open world/alizar/galeras.sql",
}
BLOCKED_WHITEVALE_RESIDUAL_SOURCES = {
    "map/open world/alizar/whitevale.sql",
}
BLOCKED_BROAD_NEW_ZONE_SOURCES = {
    "map/open world/new zones/dreadmoor.sql",
    "map/open world/new zones/halon ring.sql",
    "map/open world/new zones/murkmire.sql",
}
INSTANCE_ENTITY_WIP_SEED_SOURCES = {
    "map/instances/dungeons/coldblood citadel.sql",
    "map/instances/dungeons/protogames academy.sql",
    "map/instances/dungeons/ruins of kel voreth.sql",
    "map/instances/dungeons/sanctuary of the swordmaiden.sql",
    "map/instances/dungeons/skullcano.sql",
    "map/instances/dungeons/stormtalon's lair.sql",
    "map/instances/dungeons/ultimate protogames.sql",
    "map/instances/event instances/protostar supermall-in-the-sky.sql",
    "map/instances/event instances/shade's eve.sql",
    "map/instances/expeditions/fragment zero.sql",
    "map/instances/expeditions/gauntlet.sql",
    "map/instances/expeditions/infestation.sql",
    "map/instances/expeditions/outpost m13.sql",
    "map/instances/expeditions/space madness.sql",
    "map/instances/raids/genetic archives.sql",
    "map/instances/raids/redmoon terror.sql",
}
PARTIAL_INSTANCE_ENTITY_WIP_SEED_SOURCES = {
    "map/instances/expeditions/evil from the ether.sql",
    "map/instances/raids/datascape.sql",
}
BLOCKED_PARTIAL_INSTANCE_ENTITY_RESIDUAL_SOURCES = {
    "map/instances/expeditions/evil from the ether.sql",
    "map/instances/raids/datascape.sql",
}
HOUSING_SKYPLOT_WIP_SEED_SOURCES = {"map/instances/housing/skyplot.sql"}
LIVE_EVENT_WIP_SEED_SOURCES = {
    "events/battle chase.sql",
    "events/dungeon chase.sql",
    "events/shades eve.sql",
    "events/sim chase/sim chase 1.sql",
    "events/space chase.sql",
    "events/starfall/starfall week 1.sql",
    "events/starfall/starfall week 2.sql",
    "events/starfall/starfall week 3.sql",
    "events/starfall/starfall week 4.sql",
    "events/starfall/starfall week 5.sql",
    "events/zprix.sql",
}
LIVE_EVENT_OFFICIAL_DUPLICATE_SOURCES = {
    "events/winterfest.sql",
}
REJECTED_LIVE_EVENT_PLACEHOLDER_SOURCES = {
    "events/residential renovation.sql",
    "events/worldboss hunter challange.sql",
}
REJECTED_LIVE_EVENT_DUPLICATE_SOURCES = {
    "events/sim chase/sim chase 2.sql",
    "events/sim chase/sim chase.sql",
}
REJECTED_LIVE_EVENT_REMOVER_SOURCES = {
    "events/remove any event.sql",
    "store/storeeventremover.sql",
}
OFFICIAL_ARENA_IMPORT_SOURCES = {
    "map/instances/pvp/arena/the cryo-plex.sql",
    "map/instances/pvp/arena/the slaughterdome.sql",
}
# These branch zone dumps are already covered by the current official
# NexusForever.WorldDatabase import. Blighthaven, Malgrave, Southern Grimvault,
# and The Defile are byte-for-byte identical; Western Grimvault differs only in
# header/comment whitespace.
OFFICIAL_WORLD_DUPLICATE_SOURCES = {
    "map/open world/isigrol/blighthaven.sql",
    "map/open world/isigrol/malgrave.sql",
    "map/open world/isigrol/southerngrimvault.sql",
    "map/open world/isigrol/thedefile.sql",
    "map/open world/isigrol/western grimvault.sql",
}
REJECTED_ARENA_DECORATIVE_SOURCE_ROWS = {"map/instances/pvp/arena/the slaughterdome.sql"}
REJECTED_ARKSHIP_TUTORIAL_SOURCES = {
    "map/instances/tutorial zones/dominion arkship.sql",
    "map/instances/tutorial zones/exile arkship.sql",
}
REJECTED_INSTANCE_ENTITY_SOURCES = {
    "map/instances/raids/initialization core y-83.sql",
}


@dataclass
class FileInventory:
    relative_path: str
    normalised_key: str
    byte_count: int
    worlds: list[str]
    event_ids: list[str]
    table_counts: Counter[str] = field(default_factory=Counter)
    unsupported_targets: set[str] = field(default_factory=set)
    hazards: set[str] = field(default_factory=set)
    entity_script_names: set[str] = field(default_factory=set)
    missing_script_names: set[str] = field(default_factory=set)
    official_path: str = ""
    official_rows: int = 0
    official_table_counts: Counter[str] = field(default_factory=Counter)

    @property
    def insert_rows(self) -> int:
        return sum(self.table_counts.values())

    @property
    def row_delta(self) -> int:
        return self.insert_rows - self.official_rows

    @property
    def tables(self) -> set[str]:
        return set(self.table_counts)


def split_top_level_commas(text: str) -> list[str]:
    values: list[str] = []
    start = 0
    depth = 0
    in_single = False
    in_double = False
    in_backtick = False
    index = 0
    while index < len(text):
        char = text[index]
        if in_single:
            if char == "\\":
                index += 2
                continue
            if char == "'":
                if index + 1 < len(text) and text[index + 1] == "'":
                    index += 2
                    continue
                in_single = False
            index += 1
            continue

        if char == "'" and not in_double and not in_backtick:
            in_single = True
        elif char == '"' and not in_single and not in_backtick:
            in_double = not in_double
        elif char == "`" and not in_single and not in_double:
            in_backtick = not in_backtick
        elif not in_single and not in_double and not in_backtick:
            if char == "(":
                depth += 1
            elif char == ")" and depth > 0:
                depth -= 1
            elif char == "," and depth == 0:
                values.append(text[start:index])
                start = index + 1
        index += 1

    values.append(text[start:])
    return values


def split_value_rows(values_sql: str) -> list[str]:
    rows: list[str] = []
    start: Optional[int] = None
    depth = 0
    in_single = False
    index = 0
    while index < len(values_sql):
        char = values_sql[index]
        if in_single:
            if char == "\\":
                index += 2
                continue
            if char == "'":
                if index + 1 < len(values_sql) and values_sql[index + 1] == "'":
                    index += 2
                    continue
                in_single = False
            index += 1
            continue

        if char == "'":
            in_single = True
        elif char == "(":
            if depth == 0:
                start = index + 1
            depth += 1
        elif char == ")" and depth > 0:
            depth -= 1
            if depth == 0 and start is not None:
                rows.append(values_sql[start:index])
                start = None
        index += 1

    return rows


def unquote_sql(value: str) -> str:
    text = value.strip()
    if len(text) >= 2 and text[0] == "'" and text[-1] == "'":
        result: list[str] = []
        index = 1
        end = len(text) - 1
        while index < end:
            char = text[index]
            if char == "\\" and index + 1 < end:
                index += 1
                result.append(text[index])
            elif char == "'" and index + 1 < end and text[index + 1] == "'":
                result.append("'")
                index += 1
            else:
                result.append(char)
            index += 1
        return "".join(result)
    return text


def normalise_column(column: str) -> str:
    name = column.strip().strip("`").strip('"').strip()
    mapped = WORLD_COLUMN_MAP.get(name.lower(), name)
    return mapped.lower()


def normalise_path(relative_path: str) -> str:
    key = relative_path.replace("\\", "/").lower()
    key = key.replace("map/open world/", "")
    key = key.replace("map/instances/", "instance/")
    key = key.replace("instances/", "instance/")
    key = key.replace("tutorial zones/", "tutorial/")
    key = key.replace("expeditions/", "expedition/")
    key = key.replace("dungeons/", "dungeon/")
    key = key.replace("adventures/", "adventure/")
    key = key.replace("raids/", "raid/")
    key = key.replace("alizar/", "")
    key = key.replace("olyssia/", "")
    key = key.replace("isigrol/", "")
    for token in (" ", "-", "_"):
        key = key.replace(token, "")
    return key


def read_text(path: Path) -> str:
    return path.read_text(encoding="utf-8-sig", errors="replace")


def repair_known_sql_typos(text: str) -> str:
    # WIP/GUESSED: the Dungeon Chase branch row has a single malformed column
    # quote: `Field_7, `Visible`. Repair only that typo so schema validation can
    # judge the intended current store_offer_item shape.
    return re.sub(r"`Field_7\s*,\s*`Visible`", "`Field_7`, `Visible`", text, flags=re.IGNORECASE)


def strip_sql_line_comments(text: str) -> str:
    return "\n".join(
        line
        for line in text.splitlines()
        if not line.lstrip().startswith("--")
    )


def iter_sql_files(root: Path) -> Iterable[Path]:
    yield from sorted(root.rglob("*.sql"))


def extract_insert_data(text: str) -> tuple[Counter[str], set[str], set[str]]:
    table_counts: Counter[str] = Counter()
    unsupported_targets: set[str] = set()
    entity_script_names: set[str] = set()
    active_sql = strip_sql_line_comments(repair_known_sql_typos(text))

    for match in INSERT_RE.finditer(active_sql):
        raw_table = match.group(1)
        source_table = raw_table.lower()
        table = LEGACY_TABLE_ALIASES.get(source_table, source_table)
        columns = [normalise_column(c) for c in split_top_level_commas(match.group(2))]
        rows = split_value_rows(match.group(3))
        table_counts[table] += len(rows)

        if table not in WORLD_SCHEMA:
            unsupported_targets.add(raw_table)
        else:
            for column in columns:
                if column and column not in WORLD_SCHEMA[table]:
                    unsupported_targets.add(f"{raw_table}.{column}")

        if table == "entity_script":
            try:
                script_index = columns.index("scriptname")
            except ValueError:
                continue

            for row in rows:
                values = split_top_level_commas(row)
                if script_index < len(values):
                    script_name = unquote_sql(values[script_index])
                    if script_name:
                        entity_script_names.add(script_name)

    return table_counts, unsupported_targets, entity_script_names


def normalise_sql_row(columns: list[str], values: list[str]) -> dict[str, str]:
    row: dict[str, str] = {}
    for index, column in enumerate(columns):
        if not column:
            continue
        value = values[index] if index < len(values) else ""
        row[column] = unquote_sql(value)
    return row


def serialise_row(table: str, row: dict[str, str]) -> tuple[str, str]:
    row_json = json.dumps(row, sort_keys=True, separators=(",", ":"))
    row_hash = hashlib.sha256(f"{table}\0{row_json}".encode("utf-8")).hexdigest()
    return row_json, row_hash


def extract_normalised_rows(text: str) -> Counter[tuple[str, str, str]]:
    rows: Counter[tuple[str, str, str]] = Counter()
    active_sql = strip_sql_line_comments(repair_known_sql_typos(text))

    for match in INSERT_RE.finditer(active_sql):
        source_table = match.group(1).lower()
        table = LEGACY_TABLE_ALIASES.get(source_table, source_table)
        columns = [normalise_column(c) for c in split_top_level_commas(match.group(2))]
        for value_row in split_value_rows(match.group(3)):
            values = split_top_level_commas(value_row)
            row = normalise_sql_row(columns, values)
            row_json, row_hash = serialise_row(table, row)
            rows[(table, row_hash, row_json)] += 1

    return rows


def discover_script_names(repo_root: Path) -> set[str]:
    source_root = repo_root / "Source"
    names: set[str] = set()
    if not source_root.exists():
        return names

    for path in source_root.rglob("*.cs"):
        try:
            text = read_text(path)
        except OSError:
            continue

        for match in re.finditer(r"\bclass\s+([A-Za-z_][A-Za-z0-9_]*)", text):
            names.add(match.group(1))
        for match in re.finditer(r'\[ScriptFilterScriptName\("([^"]+)"\)\]', text):
            names.add(match.group(1))

    return names


def scan_repository(root: Path, repo_root: Path, include_script_check: bool) -> dict[str, FileInventory]:
    known_script_names = discover_script_names(repo_root) if include_script_check else set()
    files: dict[str, FileInventory] = {}
    for path in iter_sql_files(root):
        relative_path = str(path.relative_to(root))
        text = read_text(path)
        table_counts, unsupported, script_names = extract_insert_data(text)
        hazards = {
            name for name, pattern in DDL_PATTERNS.items() if pattern.search(text)
        }
        if DELETE_WORLD_RE.search(text):
            hazards.add("delete_world_entities")
        if DELETE_EVENT_RE.search(text):
            hazards.add("delete_event_entities")
        if FOREIGN_KEY_OFF_RE.search(text):
            hazards.add("foreign_key_checks_off")

        missing_scripts = script_names - known_script_names if known_script_names else set()
        inventory = FileInventory(
            relative_path=relative_path,
            normalised_key=normalise_path(relative_path),
            byte_count=path.stat().st_size,
            worlds=WORLD_RE.findall(text),
            event_ids=EVENT_RE.findall(text),
            table_counts=table_counts,
            unsupported_targets=unsupported,
            hazards=hazards,
            entity_script_names=script_names,
            missing_script_names=missing_scripts,
        )
        files[inventory.normalised_key] = inventory

    return files


def attach_official_comparison(
    laughingws_files: dict[str, FileInventory],
    official_files: dict[str, FileInventory],
) -> None:
    for key, file_info in laughingws_files.items():
        official = official_files.get(key)
        if official is None:
            continue
        file_info.official_path = official.relative_path
        file_info.official_rows = official.insert_rows
        file_info.official_table_counts = official.table_counts


def resolve_default_official_path(repo_root: Path) -> Optional[Path]:
    candidates = [
        repo_root.parent / "NexusForever.WorldDatabase",
        Path(r"C:\nexusforever-database"),
    ]
    for candidate in candidates:
        if candidate.exists():
            return candidate
    return None


def build_recommendations(info: FileInventory) -> list[str]:
    recommendations: list[str] = []
    lower_path = info.relative_path.replace("\\", "/").lower()
    tables = info.tables
    covered_by_store_seed = lower_path in STORE_CATALOG_SEED_SOURCES and bool(tables & STORE_TABLES)
    covered_by_quest_loot_seed = lower_path in QUEST_LOOT_SEED_SOURCES and bool(tables & LOOT_TABLES)
    covered_by_city_content_seed = (
        lower_path in CITY_CONTENT_SEED_SOURCES
        or lower_path in PARTIAL_CITY_CONTENT_SEED_SOURCES
    )
    fully_resolved_by_city_content_review = (
        lower_path in CITY_CONTENT_SEED_SOURCES
        or lower_path in REJECTED_CITY_CONTENT_PLACEHOLDER_SOURCES
        or lower_path in REJECTED_CITY_CONTENT_ORPHAN_VENDOR_SOURCES
    )
    rejected_city_content_placeholder_rows = lower_path in REJECTED_CITY_CONTENT_PLACEHOLDER_SOURCES
    rejected_city_content_orphan_vendor_rows = lower_path in REJECTED_CITY_CONTENT_ORPHAN_VENDOR_SOURCES
    blocked_thayd_residual_rows = lower_path in BLOCKED_THAYD_RESIDUAL_SOURCES
    covered_by_quest_instance_seed = lower_path in QUEST_INSTANCE_WIP_SEED_SOURCES
    covered_by_small_world_seed = (
        lower_path in SMALL_WORLD_WIP_SEED_SOURCES
        or lower_path in PARTIAL_SMALL_WORLD_WIP_SEED_SOURCES
    )
    blocked_everstar_grove_residual_rows = lower_path in BLOCKED_EVERSTAR_GROVE_RESIDUAL_SOURCES
    blocked_auroria_residual_rows = lower_path in BLOCKED_AURORIA_RESIDUAL_SOURCES
    blocked_crimson_isle_residual_rows = lower_path in BLOCKED_CRIMSON_ISLE_RESIDUAL_SOURCES
    blocked_levian_bay_residual_rows = lower_path in BLOCKED_LEVIAN_BAY_RESIDUAL_SOURCES
    blocked_deradune_residual_rows = lower_path in BLOCKED_DERADUNE_RESIDUAL_SOURCES
    blocked_ellevar_residual_rows = lower_path in BLOCKED_ELLEVAR_RESIDUAL_SOURCES
    blocked_wilderrun_dorian_rows = lower_path in BLOCKED_WILDERRUN_DORIAN_SOURCES
    blocked_northern_wilds_residual_rows = lower_path in BLOCKED_NORTHERN_WILDS_RESIDUAL_SOURCES
    blocked_algoroc_residual_rows = lower_path in BLOCKED_ALGOROC_RESIDUAL_SOURCES
    blocked_celestion_residual_rows = lower_path in BLOCKED_CELESTION_RESIDUAL_SOURCES
    blocked_galeras_residual_rows = lower_path in BLOCKED_GALERAS_RESIDUAL_SOURCES
    blocked_whitevale_residual_rows = lower_path in BLOCKED_WHITEVALE_RESIDUAL_SOURCES
    blocked_small_world_residual = (
        blocked_everstar_grove_residual_rows
        or blocked_auroria_residual_rows
        or blocked_crimson_isle_residual_rows
        or blocked_levian_bay_residual_rows
        or blocked_deradune_residual_rows
        or blocked_ellevar_residual_rows
        or blocked_wilderrun_dorian_rows
        or blocked_northern_wilds_residual_rows
        or blocked_algoroc_residual_rows
        or blocked_celestion_residual_rows
        or blocked_galeras_residual_rows
        or blocked_whitevale_residual_rows
    )
    rejected_northern_wilds_vendor_rows = lower_path in REJECTED_NORTHERN_WILDS_VENDOR_SOURCE_ROWS
    blocked_broad_new_zone_source = lower_path in BLOCKED_BROAD_NEW_ZONE_SOURCES
    covered_by_instance_entity_seed = (
        lower_path in INSTANCE_ENTITY_WIP_SEED_SOURCES
        or lower_path in PARTIAL_INSTANCE_ENTITY_WIP_SEED_SOURCES
    )
    blocked_partial_instance_entity_residual = lower_path in BLOCKED_PARTIAL_INSTANCE_ENTITY_RESIDUAL_SOURCES
    fully_resolved_by_instance_entity_review = (
        lower_path in INSTANCE_ENTITY_WIP_SEED_SOURCES
        or blocked_partial_instance_entity_residual
    )
    covered_by_housing_skyplot_seed = lower_path in HOUSING_SKYPLOT_WIP_SEED_SOURCES
    covered_by_live_event_seed = lower_path in LIVE_EVENT_WIP_SEED_SOURCES
    covered_by_official_live_event_rows = lower_path in LIVE_EVENT_OFFICIAL_DUPLICATE_SOURCES
    rejected_live_event_placeholder_rows = lower_path in REJECTED_LIVE_EVENT_PLACEHOLDER_SOURCES
    rejected_live_event_duplicate_rows = lower_path in REJECTED_LIVE_EVENT_DUPLICATE_SOURCES
    rejected_live_event_remover_script = lower_path in REJECTED_LIVE_EVENT_REMOVER_SOURCES
    covered_by_official_arena_import = lower_path in OFFICIAL_ARENA_IMPORT_SOURCES
    covered_by_official_world_rows = lower_path in OFFICIAL_WORLD_DUPLICATE_SOURCES
    rejected_arena_decorative_source_rows = lower_path in REJECTED_ARENA_DECORATIVE_SOURCE_ROWS
    rejected_arkship_tutorial_source = lower_path in REJECTED_ARKSHIP_TUTORIAL_SOURCES
    rejected_instance_entity_source = lower_path in REJECTED_INSTANCE_ENTITY_SOURCES
    live_event_non_store_review_complete = (
        covered_by_live_event_seed
        or covered_by_official_live_event_rows
        or rejected_live_event_placeholder_rows
        or rejected_live_event_duplicate_rows
        or rejected_live_event_remover_script
    )
    fully_resolved_by_reviewed_seed = (
        fully_resolved_by_city_content_review
        or covered_by_quest_instance_seed
        or (
            covered_by_small_world_seed
            and lower_path not in PARTIAL_SMALL_WORLD_WIP_SEED_SOURCES
        )
        or blocked_small_world_residual
        or fully_resolved_by_instance_entity_review
        or covered_by_housing_skyplot_seed
        or covered_by_live_event_seed
        or covered_by_official_arena_import
        or covered_by_official_world_rows
        or blocked_broad_new_zone_source
        or blocked_thayd_residual_rows
    )
    rejected_by_review = (
        rejected_arkship_tutorial_source
        or rejected_instance_entity_source
        or rejected_arena_decorative_source_rows
        or rejected_city_content_placeholder_rows
        or rejected_city_content_orphan_vendor_rows
        or rejected_northern_wilds_vendor_rows
        or covered_by_official_live_event_rows
        or rejected_live_event_placeholder_rows
        or rejected_live_event_duplicate_rows
        or rejected_live_event_remover_script
    )

    if covered_by_store_seed:
        recommendations.append("covered_by_laughingws_store_catalog_seed")
    if covered_by_quest_loot_seed:
        recommendations.append("covered_by_laughingws_quest_loot_seed")
    if covered_by_city_content_seed:
        recommendations.append("covered_by_laughingws_city_content_seed")
    if rejected_city_content_placeholder_rows:
        recommendations.append("rejected_city_content_placeholder_rows")
    if rejected_city_content_orphan_vendor_rows:
        recommendations.append("rejected_laughingws_illium_orphan_vendor_stubs")
    if blocked_thayd_residual_rows:
        recommendations.append("blocked_laughingws_thayd_residual_rows")
    if covered_by_quest_instance_seed:
        recommendations.append("covered_by_laughingws_quest_instance_wip_seed")
    if covered_by_small_world_seed:
        recommendations.append("covered_by_laughingws_small_world_wip_seed")
    if blocked_everstar_grove_residual_rows:
        recommendations.append("blocked_laughingws_everstar_grove_residual_rows")
    if blocked_auroria_residual_rows:
        recommendations.append("blocked_laughingws_auroria_residual_rows")
    if blocked_crimson_isle_residual_rows:
        recommendations.append("blocked_laughingws_crimson_isle_residual_rows")
    if blocked_levian_bay_residual_rows:
        recommendations.append("blocked_laughingws_levian_bay_residual_rows")
    if blocked_deradune_residual_rows:
        recommendations.append("blocked_laughingws_deradune_residual_rows")
    if blocked_ellevar_residual_rows:
        recommendations.append("blocked_laughingws_ellevar_residual_rows")
    if blocked_wilderrun_dorian_rows:
        recommendations.append("blocked_laughingws_wilderrun_dorian_rows")
    if blocked_northern_wilds_residual_rows:
        recommendations.append("blocked_laughingws_northern_wilds_residual_rows")
    if blocked_algoroc_residual_rows:
        recommendations.append("blocked_laughingws_algoroc_residual_rows")
    if blocked_celestion_residual_rows:
        recommendations.append("blocked_laughingws_celestion_residual_rows")
    if blocked_galeras_residual_rows:
        recommendations.append("blocked_laughingws_galeras_residual_rows")
    if blocked_whitevale_residual_rows:
        recommendations.append("blocked_laughingws_whitevale_residual_rows")
    if rejected_northern_wilds_vendor_rows:
        recommendations.append("rejected_laughingws_northern_wilds_vendor_duplicate_rows")
    if blocked_broad_new_zone_source:
        recommendations.append("blocked_laughingws_new_zone_broad_rows")
    if covered_by_instance_entity_seed:
        recommendations.append("covered_by_laughingws_instance_entity_wip_seed")
    if blocked_partial_instance_entity_residual:
        recommendations.append("blocked_laughingws_instance_entity_residual_rows")
    if covered_by_housing_skyplot_seed:
        recommendations.append("covered_by_laughingws_housing_skyplot_wip_seed")
    if covered_by_live_event_seed:
        recommendations.append("covered_by_laughingws_live_event_wip_seed")
    if covered_by_official_live_event_rows:
        recommendations.append("covered_by_official_live_event_rows")
    if rejected_live_event_placeholder_rows:
        recommendations.append("rejected_live_event_placeholder_rows")
    if rejected_live_event_duplicate_rows:
        recommendations.append("rejected_live_event_duplicate_source_rows")
    if rejected_live_event_remover_script:
        recommendations.append("skip_live_event_remover_script")
    if covered_by_official_arena_import:
        recommendations.append("covered_by_official_arena_import")
    if covered_by_official_world_rows:
        recommendations.append("covered_by_official_worlddb_import")
    if rejected_arena_decorative_source_rows:
        recommendations.append("rejected_branch_arena_decorative_rows")
    if rejected_arkship_tutorial_source:
        recommendations.append("rejected_historical_arkship_tutorial_rows")
    if rejected_instance_entity_source:
        recommendations.append("rejected_zero_coordinate_instance_rows")

    if lower_path.startswith("all in one/"):
        recommendations.append("skip_raw_full_dump")
    if "drop_table" in info.hazards or "truncate_table" in info.hazards:
        if covered_by_store_seed or covered_by_quest_loot_seed:
            pass
        elif tables <= STORE_TABLES:
            recommendations.append("extract_store_catalog_without_ddl")
        elif tables <= LOOT_TABLES:
            recommendations.append("extract_loot_overlay_without_ddl")
        else:
            recommendations.append("skip_raw_destructive_ddl")
    if info.unsupported_targets:
        recommendations.append("fix_or_exclude_unsupported_targets")
    sandbox_only_client_asset_check_required = "test zones/" in lower_path
    if sandbox_only_client_asset_check_required:
        recommendations.append("sandbox_only_client_asset_check_required")
    replaces_required_riders_reef_import = "new tutorial.sql" in lower_path or (
        set(info.worlds) == {"3460"} and info.insert_rows < 100
    )
    if replaces_required_riders_reef_import:
        recommendations.append("skip_replaces_required_riders_reef_import")
    if info.missing_script_names:
        recommendations.append("strip_or_implement_missing_script_rows")
    map_entrance_only = tables == {"map_entrance"}

    if (
        "delete_world_entities" in info.hazards
        and tables & WORLD_ENTITY_TABLES
        and not replaces_required_riders_reef_import
        and not sandbox_only_client_asset_check_required
        and not map_entrance_only
        and not fully_resolved_by_reviewed_seed
        and not rejected_by_review
    ):
        recommendations.append("extract_additive_world_overlay")
    if (
        tables
        and tables <= STORE_TABLES
        and "extract_store_catalog_without_ddl" not in recommendations
        and not covered_by_store_seed
    ):
        recommendations.append("candidate_store_overlay")
    if (
        tables
        and tables <= LOOT_TABLES
        and "extract_loot_overlay_without_ddl" not in recommendations
        and not covered_by_quest_loot_seed
    ):
        recommendations.append("candidate_loot_overlay")
    if covered_by_store_seed and not tables <= STORE_TABLES and not live_event_non_store_review_complete:
        recommendations.append("review_non_store_event_rows_manually")
    if (
        "map_entrance" in tables
        and not lower_path.startswith("all in one/")
        and not covered_by_official_arena_import
    ):
        recommendations.append("covered_by_laughingws_map_entrance_seed")
    if not recommendations:
        recommendations.append("review_manually")

    # Preserve order while removing duplicates.
    return list(dict.fromkeys(recommendations))


def write_csv(path: Path, rows: list[dict[str, object]], fieldnames: list[str]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=fieldnames)
        writer.writeheader()
        writer.writerows(rows)


def serialise_counter(counter: Counter[str]) -> str:
    return json.dumps(dict(sorted(counter.items())), sort_keys=True)


def make_file_rows(files: dict[str, FileInventory]) -> list[dict[str, object]]:
    rows: list[dict[str, object]] = []
    for info in sorted(files.values(), key=lambda item: item.relative_path.lower()):
        rows.append(
            {
                "file": info.relative_path,
                "normalised_key": info.normalised_key,
                "worlds": ";".join(info.worlds),
                "event_ids": ";".join(info.event_ids),
                "bytes": info.byte_count,
                "insert_rows": info.insert_rows,
                "tables_json": serialise_counter(info.table_counts),
                "official_file": info.official_path,
                "official_rows": info.official_rows,
                "row_delta": info.row_delta,
                "hazards": ";".join(sorted(info.hazards)),
                "unsupported_targets": ";".join(sorted(info.unsupported_targets)),
                "entity_script_names": ";".join(sorted(info.entity_script_names)),
                "missing_script_names": ";".join(sorted(info.missing_script_names)),
                "recommendations": ";".join(build_recommendations(info)),
            }
        )
    return rows


def make_table_rows(files: dict[str, FileInventory]) -> list[dict[str, object]]:
    by_table: Counter[str] = Counter()
    file_count_by_table: Counter[str] = Counter()
    for info in files.values():
        by_table.update(info.table_counts)
        for table in info.table_counts:
            file_count_by_table[table] += 1

    return [
        {
            "table": table,
            "rows": by_table[table],
            "files": file_count_by_table[table],
        }
        for table in sorted(by_table)
    ]


def make_world_rows(files: dict[str, FileInventory]) -> list[dict[str, object]]:
    by_world: dict[str, Counter[str]] = defaultdict(Counter)
    file_count_by_world: Counter[str] = Counter()
    for info in files.values():
        for world in info.worlds:
            by_world[world].update(info.table_counts)
            file_count_by_world[world] += 1

    rows: list[dict[str, object]] = []
    for world in sorted(by_world, key=lambda value: int(value) if value.isdigit() else value):
        rows.append(
            {
                "world": world,
                "files": file_count_by_world[world],
                "insert_rows": sum(by_world[world].values()),
                "tables_json": serialise_counter(by_world[world]),
            }
        )
    return rows


def make_row_reconciliation_rows(
    laughingws_path: Path,
    official_path: Optional[Path],
    files: dict[str, FileInventory],
) -> list[dict[str, object]]:
    rows: list[dict[str, object]] = []
    for info in sorted(files.values(), key=lambda item: item.relative_path):
        laughingws_file = laughingws_path / info.relative_path
        try:
            laughingws_rows = extract_normalised_rows(read_text(laughingws_file))
        except OSError:
            laughingws_rows = Counter()

        official_rows: Counter[tuple[str, str, str]] = Counter()
        if official_path and info.official_path:
            official_file = official_path / info.official_path
            try:
                official_rows = extract_normalised_rows(read_text(official_file))
            except OSError:
                official_rows = Counter()

        recommendations = ";".join(build_recommendations(info))
        row_keys = sorted(
            set(laughingws_rows) | set(official_rows),
            key=lambda item: (item[0], item[1], item[2]),
        )
        for table, row_hash, row_json in row_keys:
            laughingws_count = laughingws_rows[(table, row_hash, row_json)]
            official_count = official_rows[(table, row_hash, row_json)]
            common_count = min(laughingws_count, official_count)
            if laughingws_count > common_count:
                rows.append(
                    {
                        "file": info.relative_path,
                        "official_file": info.official_path,
                        "recommendations": recommendations,
                        "side": "branch_only",
                        "table": table,
                        "count": laughingws_count - common_count,
                        "row_hash": row_hash,
                        "row_json": row_json,
                    }
                )
            if official_count > common_count:
                rows.append(
                    {
                        "file": info.relative_path,
                        "official_file": info.official_path,
                        "recommendations": recommendations,
                        "side": "official_only",
                        "table": table,
                        "count": official_count - common_count,
                        "row_hash": row_hash,
                        "row_json": row_json,
                    }
                )

    return rows


def write_markdown(
    path: Path,
    laughingws_path: Path,
    official_path: Optional[Path],
    files: dict[str, FileInventory],
) -> None:
    total_files = len(files)
    ddl_files = [f for f in files.values() if f.hazards & set(DDL_PATTERNS)]
    delete_world_files = [f for f in files.values() if "delete_world_entities" in f.hazards]
    unsupported_files = [f for f in files.values() if f.unsupported_targets]
    missing_script_files = [f for f in files.values() if f.missing_script_names]
    fork_only = [f for f in files.values() if not f.official_path]
    changed_common = [
        f
        for f in files.values()
        if f.official_path and (f.row_delta != 0 or f.table_counts != f.official_table_counts)
    ]

    recommendation_counts: Counter[str] = Counter()
    for info in files.values():
        recommendation_counts.update(build_recommendations(info))

    top_changed = sorted(changed_common, key=lambda item: abs(item.row_delta), reverse=True)[:20]
    top_fork_only = sorted(fork_only, key=lambda item: item.insert_rows, reverse=True)[:20]

    lines = [
        "# LaughingWS World Database Audit",
        "",
        f"- Source: `{laughingws_path}`",
        f"- Official comparison: `{official_path}`" if official_path else "- Official comparison: not found",
        f"- SQL files scanned: {total_files}",
        f"- Insert rows counted: {sum(f.insert_rows for f in files.values())}",
        f"- Files with destructive or schema DDL: {len(ddl_files)}",
        f"- Files deleting all rows for `@WORLD`: {len(delete_world_files)}",
        f"- Files with unsupported current-schema targets: {len(unsupported_files)}",
        f"- Files referencing missing C# entity scripts: {len(missing_script_files)}",
        "",
        "## Recommendation Counts",
        "",
        "| Recommendation | Files |",
        "| --- | ---: |",
    ]
    for recommendation, count in recommendation_counts.most_common():
        lines.append(f"| `{recommendation}` | {count} |")

    lines.extend(
        [
            "",
            "## Largest Changed Official Files",
            "",
            "| Fork file | Official file | Rows | Official rows | Delta | Recommendations |",
            "| --- | --- | ---: | ---: | ---: | --- |",
        ]
    )
    for info in top_changed:
        lines.append(
            "| "
            + " | ".join(
                [
                    f"`{info.relative_path}`",
                    f"`{info.official_path}`",
                    str(info.insert_rows),
                    str(info.official_rows),
                    str(info.row_delta),
                    ", ".join(f"`{r}`" for r in build_recommendations(info)),
                ]
            )
            + " |"
        )

    lines.extend(
        [
            "",
            "## Largest Fork-Only Files",
            "",
            "| File | Worlds | Rows | Recommendations |",
            "| --- | --- | ---: | --- |",
        ]
    )
    for info in top_fork_only:
        lines.append(
            "| "
            + " | ".join(
                [
                    f"`{info.relative_path}`",
                    ", ".join(info.worlds) or "",
                    str(info.insert_rows),
                    ", ".join(f"`{r}`" for r in build_recommendations(info)),
                ]
            )
            + " |"
        )

    lines.extend(
        [
            "",
            "## Intake Rule",
            "",
            "Do not pass this repository directly to `-WorldDatabasePath`. Use this report to select",
            "small overlays, strip destructive DDL/deletes, and promote reviewed rows into runtime-owned",
            "tables or checked SQL seeds.",
            "",
        ]
    )
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text("\n".join(lines), encoding="utf-8")


def parse_args() -> argparse.Namespace:
    repo_root = Path(__file__).resolve().parents[2]
    default_output = repo_root / "artifacts" / "laughingws_worlddb_audit"
    default_official = resolve_default_official_path(repo_root)

    parser = argparse.ArgumentParser(
        description="Audit LaughingWS world database SQL for safe NexusForever intake."
    )
    parser.add_argument(
        "--laughingws-path",
        required=True,
        type=Path,
        help="Path to LaughingWS/NexusForever.WorldDatabase.New-Zones-and-more.",
    )
    parser.add_argument(
        "--official-worlddb-path",
        type=Path,
        default=default_official,
        help="Optional NexusForever.WorldDatabase path for row/table comparison.",
    )
    parser.add_argument(
        "--repo-root",
        type=Path,
        default=repo_root,
        help="NexusForever checkout root used for C# entity script discovery.",
    )
    parser.add_argument(
        "--output-dir",
        type=Path,
        default=default_output,
        help="Directory for generated CSV/Markdown audit artifacts.",
    )
    parser.add_argument(
        "--skip-script-check",
        action="store_true",
        help="Do not check entity_script names against Source/**/*.cs class names.",
    )
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    laughingws_path = args.laughingws_path.resolve()
    official_path = args.official_worlddb_path.resolve() if args.official_worlddb_path else None
    repo_root = args.repo_root.resolve()
    output_dir = args.output_dir.resolve()

    if not laughingws_path.exists():
        print(f"LaughingWS path does not exist: {laughingws_path}", file=sys.stderr)
        return 2

    official_files: dict[str, FileInventory] = {}
    if official_path and official_path.exists():
        official_files = scan_repository(official_path, repo_root, include_script_check=False)
    elif official_path:
        print(f"Official world database path not found: {official_path}", file=sys.stderr)
        official_path = None

    laughingws_files = scan_repository(
        laughingws_path,
        repo_root,
        include_script_check=not args.skip_script_check,
    )
    attach_official_comparison(laughingws_files, official_files)

    file_rows = make_file_rows(laughingws_files)
    write_csv(
        output_dir / "laughingws_worlddb_file_inventory.csv",
        file_rows,
        [
            "file",
            "normalised_key",
            "worlds",
            "event_ids",
            "bytes",
            "insert_rows",
            "tables_json",
            "official_file",
            "official_rows",
            "row_delta",
            "hazards",
            "unsupported_targets",
            "entity_script_names",
            "missing_script_names",
            "recommendations",
        ],
    )
    write_csv(
        output_dir / "laughingws_worlddb_table_summary.csv",
        make_table_rows(laughingws_files),
        ["table", "rows", "files"],
    )
    write_csv(
        output_dir / "laughingws_worlddb_world_summary.csv",
        make_world_rows(laughingws_files),
        ["world", "files", "insert_rows", "tables_json"],
    )
    write_csv(
        output_dir / "laughingws_worlddb_row_reconciliation.csv",
        make_row_reconciliation_rows(laughingws_path, official_path, laughingws_files),
        [
            "file",
            "official_file",
            "recommendations",
            "side",
            "table",
            "count",
            "row_hash",
            "row_json",
        ],
    )
    write_markdown(
        output_dir / "laughingws_worlddb_audit.md",
        laughingws_path,
        official_path,
        laughingws_files,
    )

    print(f"Scanned {len(laughingws_files)} SQL files.")
    print(f"Wrote audit artifacts to {output_dir}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
