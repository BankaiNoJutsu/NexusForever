#!/usr/bin/env python3
"""Extract curated city/museum/quest-terminal entity rows from LaughingWS SQL."""

from __future__ import annotations

import argparse
import re
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable

from analyze_laughingws_worlddb import (
    normalise_column,
    read_text,
    split_top_level_commas,
    split_value_rows,
)


SET_WORLD_RE = re.compile(r"\bSET\s+@WORLD\s*:?=\s*(\d+)\b", re.IGNORECASE)
INSERT_ENTITY_RE = re.compile(
    r"\bINSERT\s+INTO\s+`?entity`?\s*\((?P<columns>.*?)\)\s*"
    r"VALUE(?:S)?\s*(?P<values>.*?);",
    re.IGNORECASE | re.DOTALL,
)

THAYD_SOURCE = "Map/Open World/Alizar/Thayd.sql"
ILLIUM_SOURCE = "Map/Open World/Olyssia/Illium.sql"
HOUSING_INTRO_SOURCES = {
    ILLIUM_SOURCE: {65296, 65297, 65298, 65299},
    THAYD_SOURCE: {54400, 54401, 54403, 54404},
}

OUTPUT_COLUMNS = [
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
]

CURATED_SOURCES = {
    # WIP/GUESSED: Ringo Hax is DataMapping-corroborated for Return to Illium
    # quest turn-in/spawn placement, but exact NPC runtime smoke is still
    # pending. The broad Illium replacement dump stays blocked.
    ILLIUM_SOURCE: {44961},
    "Map/Open World/Olyssia/Illium Museum.sql": None,
    "Map/Open World/Olyssia/z Illium Halon Ring Update.sql": {46182},
    "Map/Open World/Alizar/z Thayd Halon Ring update.sql": {46183},
    "Map/Open World/New Zones/Halon Ring.sql": {46184, 46185},
    # WIP/GUESSED: branch placement for the Skull's Eye escape pod terminal is
    # corroborated by DataMapping quest 3770/objective 3, but still needs
    # manual quest smoke before claiming exact retail placement parity.
    "Map/Open World/Skulls Eye.sql": {13306},
}

BASE_ENTITY_ID = 1_100_000_000


@dataclass(frozen=True)
class EntityRow:
    source_file: str
    source_index: int
    values: dict[str, str]

    @property
    def creature(self) -> int:
        return int(self.values["creature"], 0)

    @property
    def world(self) -> int:
        return int(self.values["world"], 0)

    @property
    def area(self) -> int:
        return int(self.values["area"], 0)

    @property
    def position_key(self) -> tuple[int, int, int, int, int]:
        return (
            self.world,
            self.creature,
            self.area,
            round(float(self.values["x"]) * 100),
            round(float(self.values["z"]) * 100),
        )

    @property
    def has_placed_position(self) -> bool:
        return any(abs(float(self.values[column])) > 0.0001 for column in ("x", "y", "z"))


@dataclass(frozen=True)
class OfficialQuestChecklistUpdate:
    source_row: EntityRow

    @property
    def creature(self) -> int:
        return self.source_row.creature

    @property
    def quest_checklist_idx(self) -> int:
        return int(self.source_row.values["questchecklistidx"], 0)

    @property
    def active_prop_id(self) -> int:
        return int(self.source_row.values["activepropid"], 0)

    @property
    def label(self) -> str:
        match self.creature:
            case 65296:
                return "Zen Pond"
            case 65297:
                return "Windmill"
            case 65298:
                return "Power Generator"
            case 65299:
                return "Storage Unit"
            case 54400:
                return "Zen Pond"
            case 54401:
                return "Windmill"
            case 54403:
                return "Power Generator"
            case 54404:
                return "Storage Unit"
            case _:
                return f"creature {self.creature}"


def default_laughingws_path(repo_root: Path) -> Path:
    return repo_root / "artifacts" / "external" / "LaughingWS.WorldDatabase.New-Zones-and-more"


def default_official_worlddb_path(repo_root: Path) -> Path:
    return repo_root.parent / "NexusForever.WorldDatabase"


def iter_sql_files(root: Path) -> Iterable[Path]:
    yield from sorted(root.rglob("*.sql"))


def parse_world_variable(text: str) -> int | None:
    match = SET_WORLD_RE.search(text)
    if match:
        return int(match.group(1))
    return None


def normalise_relative_path(path: Path, root: Path) -> str:
    return str(path.relative_to(root)).replace("\\", "/")


def resolve_token(token: str, world_id: int | None) -> str:
    value = token.strip()
    if value.upper() == "@WORLD":
        if world_id is None:
            raise ValueError("@WORLD used before a SET @WORLD declaration")
        return str(world_id)
    return value


def parse_entity_rows(root: Path, curated_only: bool) -> list[EntityRow]:
    rows: list[EntityRow] = []
    source_index = 0

    for path in iter_sql_files(root):
        relative_path = normalise_relative_path(path, root)
        allowed_creatures = CURATED_SOURCES.get(relative_path)
        if curated_only and relative_path not in CURATED_SOURCES:
            continue

        text = read_text(path)
        world_id = parse_world_variable(text)

        for insert_match in INSERT_ENTITY_RE.finditer(text):
            columns = [
                normalise_column(column)
                for column in split_top_level_commas(insert_match.group("columns"))
            ]

            for row_sql in split_value_rows(insert_match.group("values")):
                raw_values = split_top_level_commas(row_sql)
                values = {
                    column: resolve_token(raw_values[index], world_id)
                    for index, column in enumerate(columns)
                    if index < len(raw_values)
                }

                if "id" not in values or "creature" not in values or "world" not in values:
                    continue

                for column in OUTPUT_COLUMNS:
                    values.setdefault(column, "0")

                row = EntityRow(relative_path, source_index, values)
                source_index += 1

                if curated_only and allowed_creatures is not None and row.creature not in allowed_creatures:
                    continue
                if curated_only and not row.has_placed_position:
                    continue

                rows.append(row)

    return rows


def build_overlay_rows(laughingws_rows: list[EntityRow], official_rows: list[EntityRow]) -> list[EntityRow]:
    official_keys = {row.position_key for row in official_rows}
    return [
        row
        for row in laughingws_rows
        if row.position_key not in official_keys
    ]


def build_housing_intro_updates(root: Path) -> list[OfficialQuestChecklistUpdate]:
    all_rows = parse_entity_rows(root, curated_only=False)
    updates: list[OfficialQuestChecklistUpdate] = []

    for source_file, expected_creatures in HOUSING_INTRO_SOURCES.items():
        rows = [
            row
            for row in all_rows
            if row.source_file == source_file
            and row.creature in expected_creatures
            and int(row.values["questchecklistidx"], 0) > 0
        ]

        expected_counts = {creature: 1 for creature in expected_creatures}
        actual_counts = {
            creature: sum(1 for row in rows if row.creature == creature)
            for creature in expected_counts
        }
        if actual_counts != expected_counts:
            raise ValueError(
                f"{source_file}: expected housing questChecklistIdx update counts "
                f"{expected_counts}, found {actual_counts}"
            )

        updates.extend(OfficialQuestChecklistUpdate(row) for row in rows)

    return updates


def append_housing_intro_update_block(
    lines: list[str],
    updates: list[OfficialQuestChecklistUpdate],
) -> None:
    if not updates:
        return

    lines.append(f"-- official Thayd/Illium housing intro questChecklistIdx reconciliation: {len(updates)} row(s)")
    lines.append(
        "-- WIP/GUESSED: branch QuestChecklistIdx values match the existing HousingIntroEntityScript creature/story-panel "
        "mapping and are keyed to official worlddb rows by creature, ActivePropId, and exact coordinates."
    )
    lines.append(
        "-- WIP/GUESSED: fallback INSERT statements add the exact source row only when an older local import lacks the official housing prop."
    )
    for index, update in enumerate(updates):
        values = update.source_row.values
        where_parts = [
            f"`world` = {values['world']}",
            f"`creature` = {values['creature']}",
            f"`activePropId` = {values['activepropid']}",
            f"ABS(`x` - ({values['x']})) < 0.001",
            f"ABS(`y` - ({values['y']})) < 0.001",
            f"ABS(`z` - ({values['z']})) < 0.001",
        ]
        lines.append(
            f"--   {update.label}: questChecklistIdx {update.quest_checklist_idx}; source {update.source_row.source_file}"
        )
        lines.append(
            "UPDATE `entity` SET "
            f"`questChecklistIdx` = {update.quest_checklist_idx} "
            "WHERE " + " AND ".join(where_parts) + ";"
        )
        fallback_values: dict[str, str] = {
            column: values.get(column, "0")
            for column in OUTPUT_COLUMNS
        }
        fallback_values["id"] = str(BASE_ENTITY_ID + 10_000 + index + 1)
        fallback_values["questchecklistidx"] = str(update.quest_checklist_idx)

        # WIP/GUESSED: normal setup updates the official worlddb row. The
        # fallback keeps older local databases usable without importing the
        # destructive broad Thayd/Illium replacement dumps.
        lines.append(
            "INSERT INTO `entity` ("
            + ", ".join(f"`{column}`" for column in OUTPUT_COLUMNS)
            + ")"
        )
        lines.append(
            "SELECT "
            + ",".join(fallback_values[column] for column in OUTPUT_COLUMNS)
            + " WHERE NOT EXISTS (SELECT 1 FROM `entity` WHERE "
            + " AND ".join(where_parts)
            + ");"
        )
    lines.append("")


def write_seed(
    output_path: Path,
    rows: list[EntityRow],
    official_updates: list[OfficialQuestChecklistUpdate],
) -> None:
    output_path.parent.mkdir(parents=True, exist_ok=True)

    lines = [
        "-- Generated by Tools/DataMapping/extract_laughingws_city_content.py.",
        "-- Source: Curated LaughingWS city/museum/quest-terminal entity rows.",
        "-- Purpose: Add placed transport, Illium Museum, housing intro checklist, and Skull's Eye escape-pod content without importing broad world replacement SQL.",
        "-- WIP/GUESSED: Skull's Eye terminal placement is branch-authored and still needs manual quest smoke for exact retail parity.",
        "-- WIP/GUESSED: Ringo Hax Illium placement and Thayd/Illium housing questChecklistIdx updates are evidence-backed but still need manual client smoke.",
        "",
        "USE `nexus_forever_world`;",
        "",
        "START TRANSACTION;",
        "",
    ]

    if rows:
        column_sql = ", ".join(f"`{column}`" for column in OUTPUT_COLUMNS)
        lines.append(f"-- entity: {len(rows)} row(s)")
        lines.append(f"INSERT IGNORE INTO `entity` ({column_sql}) VALUES")
        for index, row in enumerate(rows):
            entity_id = BASE_ENTITY_ID + index + 1
            values = [str(entity_id)] + [row.values[column] for column in OUTPUT_COLUMNS[1:]]
            suffix = "," if index + 1 < len(rows) else ";"
            lines.append(f"    ({','.join(values)}){suffix}")
        lines.append("")
    else:
        lines.append("-- No curated city content overlay rows were found.")
        lines.append("")

    append_housing_intro_update_block(lines, official_updates)

    lines.append("-- Sources:")
    sources = {row.source_file for row in rows}
    sources.update(update.source_row.source_file for update in official_updates)
    for source in sorted(sources):
        lines.append(f"--   {source}")
    lines.extend(["", "COMMIT;", ""])
    output_path.write_text("\n".join(lines), encoding="ascii", newline="\n")


def parse_args() -> argparse.Namespace:
    repo_root = Path(__file__).resolve().parents[2]
    parser = argparse.ArgumentParser(
        description="Extract curated LaughingWS city, museum, and quest-terminal entity rows into a safe seed.",
    )
    parser.add_argument(
        "--laughingws-path",
        type=Path,
        default=default_laughingws_path(repo_root),
        help="Path to LaughingWS/NexusForever.WorldDatabase.New-Zones-and-more.",
    )
    parser.add_argument(
        "--official-worlddb-path",
        type=Path,
        default=default_official_worlddb_path(repo_root),
        help="Path to the official NexusForever.WorldDatabase checkout.",
    )
    parser.add_argument(
        "--output-sql",
        type=Path,
        default=repo_root / "Tools" / "DataMapping" / "sql" / "laughingws_city_content_seed.sql",
        help="Output SQL seed path.",
    )
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    laughingws_path = args.laughingws_path.resolve()
    official_path = args.official_worlddb_path.resolve()

    if not laughingws_path.is_dir():
        raise SystemExit(f"LaughingWS path does not exist: {laughingws_path}")
    if not official_path.is_dir():
        raise SystemExit(f"Official world database path does not exist: {official_path}")

    laughingws_rows = parse_entity_rows(laughingws_path, curated_only=True)
    official_rows = parse_entity_rows(official_path, curated_only=False)
    overlay_rows = build_overlay_rows(laughingws_rows, official_rows)
    official_updates = build_housing_intro_updates(laughingws_path)
    write_seed(args.output_sql, overlay_rows, official_updates)

    print(
        f"Wrote {len(overlay_rows)} city content entity row(s) and "
        f"{len(official_updates)} official questChecklistIdx update statement(s) to {args.output_sql.resolve()} "
        f"({len(laughingws_rows)} curated LaughingWS row(s))."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
