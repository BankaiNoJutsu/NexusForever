#!/usr/bin/env python3
"""
Build world creature spell context tables for spell reverse engineering.

This parses the optional NexusForever.WorldDatabase SQL files, materializes creature
placements, and joins them to the focused spell reverse-engineering database through:

    world_creature_placements.creature_id
      -> creature_spells.creature_id
      -> spells.game_id
      -> spell4.ID / spell4effects.spellId

The script intentionally has no third-party Python dependencies. It shells out to the
MySQL client so it can run in the same environment used to import the SQL dumps.
"""

from __future__ import annotations

import argparse
import csv
import pathlib
import re
import subprocess
import tempfile
from dataclasses import dataclass
from typing import Any


ROOT = pathlib.Path(__file__).resolve().parents[2]
DEFAULT_WORLD_DB = ROOT.parent / "NexusForever.WorldDatabase"
DEFAULT_MYSQL = pathlib.Path(r"C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe")
SPELL_EFFECT_TYPE_ENUM = ROOT / "Source" / "NexusForever.Game.Static" / "Spell" / "SpellEffectType.cs"


@dataclass
class Placement:
    source_file: str
    continent: str
    zone: str
    context: str | None
    source_index: int
    entity_offset: int | None
    entity_type: int | None
    creature_id: int
    world_id: int | None
    area_id: int | None
    x: float | None
    y: float | None
    z: float | None
    rx: float | None
    ry: float | None
    rz: float | None
    display_info: int | None
    outfit_info: int | None
    faction1: int | None
    faction2: int | None


def sql_quote(value: Any) -> str:
    if value is None:
        return "NULL"

    if isinstance(value, (int, float)):
        return str(value)

    return "'" + str(value).replace("\\", "\\\\").replace("'", "''") + "'"


def split_values(row: str) -> list[str]:
    return next(csv.reader([row], delimiter=",", quotechar="'", escapechar="\\", skipinitialspace=True))


def parse_num(value: str | None, world_id: int | None = None) -> int | float | None:
    if value is None:
        return None

    value = value.strip()
    if value.upper() == "NULL":
        return None
    if value == "@WORLD":
        return world_id

    match = re.fullmatch(r"@GUID\s*\+\s*(\d+)", value, re.IGNORECASE)
    if match:
        return int(match.group(1))

    if re.fullmatch(r"-?\d+", value):
        return int(value)

    if re.fullmatch(r"-?(?:\d+\.\d*|\.\d+)(?:[eE][+-]?\d+)?|-?\d+[eE][+-]?\d+", value):
        return float(value)

    return None


def heading_index(sql: str) -> list[tuple[int, str]]:
    headings: list[tuple[int, str]] = []
    for match in re.finditer(r"--\s*-{5,}\s*\r?\n--\s*(.*?)\s*\r?\n--\s*-{5,}", sql, re.DOTALL):
        heading = " ".join(match.group(1).split())[:255]
        headings.append((match.end(), heading))

    return headings


def heading_for(headings: list[tuple[int, str]], position: int) -> str | None:
    current: str | None = None
    for offset, heading in headings:
        if offset <= position:
            current = heading
        else:
            break

    return current


def parse_world_placements(world_db: pathlib.Path) -> list[Placement]:
    placements: list[Placement] = []
    insert_re = re.compile(
        r"INSERT\s+INTO\s+`entity`\s*\(([^)]*)\)\s*VALUES\s*(.*?);",
        re.DOTALL | re.IGNORECASE,
    )

    for path in sorted(world_db.rglob("*.sql")):
        sql = path.read_text(encoding="utf-8", errors="ignore")
        world_match = re.search(r"SET\s+@WORLD\s*=\s*(\d+)\s*;", sql, re.IGNORECASE)
        world_id = int(world_match.group(1)) if world_match else None

        rel = path.relative_to(world_db)
        parts = rel.parts
        continent = parts[0] if len(parts) > 1 else ""
        zone = path.stem
        headings = heading_index(sql)

        source_index = 0
        for match in insert_re.finditer(sql):
            columns = [column.strip().strip("`") for column in match.group(1).split(",")]
            if "Creature" not in columns:
                continue

            context = heading_for(headings, match.start())
            for row_match in re.finditer(r"\(([^()]*)\)", match.group(2)):
                values = split_values(row_match.group(1))
                if len(values) < len(columns):
                    continue

                source_index += 1
                record = dict(zip(columns, values))
                creature = parse_num(record.get("Creature"), world_id)
                if not isinstance(creature, int):
                    continue

                placements.append(
                    Placement(
                        source_file=str(rel).replace("\\", "/"),
                        continent=continent,
                        zone=zone,
                        context=context,
                        source_index=source_index,
                        entity_offset=parse_num(record.get("Id"), world_id),
                        entity_type=parse_num(record.get("Type"), world_id),
                        creature_id=creature,
                        world_id=parse_num(record.get("World"), world_id),
                        area_id=parse_num(record.get("Area"), world_id),
                        x=parse_num(record.get("X"), world_id),
                        y=parse_num(record.get("Y"), world_id),
                        z=parse_num(record.get("Z"), world_id),
                        rx=parse_num(record.get("RX"), world_id),
                        ry=parse_num(record.get("RY"), world_id),
                        rz=parse_num(record.get("RZ"), world_id),
                        display_info=parse_num(record.get("DisplayInfo"), world_id),
                        outfit_info=parse_num(record.get("OutfitInfo"), world_id),
                        faction1=parse_num(record.get("Faction1"), world_id),
                        faction2=parse_num(record.get("Faction2"), world_id),
                    )
                )

    return placements


def parse_effect_type_names() -> list[tuple[int, str]]:
    enum_text = SPELL_EFFECT_TYPE_ENUM.read_text(encoding="utf-8")
    rows: list[tuple[int, str]] = []
    for name, value in re.findall(r"([A-Za-z0-9_]+)\s*=\s*0x([0-9A-Fa-f]+)", enum_text):
        rows.append((int(value, 16), name))

    return sorted(rows)


def build_sql(placements: list[Placement], effect_names: list[tuple[int, str]]) -> str:
    placement_columns = [
        "source_file",
        "continent",
        "zone",
        "context",
        "source_index",
        "entity_offset",
        "entity_type",
        "creature_id",
        "world_id",
        "area_id",
        "x",
        "y",
        "z",
        "rx",
        "ry",
        "rz",
        "display_info",
        "outfit_info",
        "faction1",
        "faction2",
    ]

    statements = [
        """
DROP VIEW IF EXISTS world_damage_candidate_context;
DROP VIEW IF EXISTS world_unit_property_modifier_context;
DROP VIEW IF EXISTS world_proxy_chain_context;
DROP VIEW IF EXISTS world_runtime_spell_candidates;
DROP VIEW IF EXISTS world_nonplayer_creature_family_summary;
DROP VIEW IF EXISTS world_nonplayer_effect_family_summary;
    DROP VIEW IF EXISTS world_creature_spell_coverage_summary;
    DROP VIEW IF EXISTS world_creature_spell_coverage_gaps;
DROP VIEW IF EXISTS world_creature_activate_spell_context;
DROP VIEW IF EXISTS world_creature_spell_context;
DROP TABLE IF EXISTS world_effect_family_context;
DROP TABLE IF EXISTS world_creature_placements;
CREATE TABLE world_creature_placements (
  id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
  source_file VARCHAR(255) NOT NULL,
  continent VARCHAR(64) NOT NULL,
  zone VARCHAR(128) NOT NULL,
  context VARCHAR(255) NULL,
  source_index INT NOT NULL,
  entity_offset INT NULL,
  entity_type INT NULL,
  creature_id INT NOT NULL,
  world_id INT NULL,
  area_id INT NULL,
  x FLOAT NULL,
  y FLOAT NULL,
  z FLOAT NULL,
  rx FLOAT NULL,
  ry FLOAT NULL,
  rz FLOAT NULL,
  display_info INT NULL,
  outfit_info INT NULL,
  faction1 INT NULL,
  faction2 INT NULL,
  KEY idx_world_creature_placements_creature (creature_id),
  KEY idx_world_creature_placements_zone (continent, zone),
  KEY idx_world_creature_placements_world_area (world_id, area_id)
);
DROP TABLE IF EXISTS spell_effect_type_names;
CREATE TABLE spell_effect_type_names (
  effectType INT NOT NULL PRIMARY KEY,
  effectName VARCHAR(128) NOT NULL
);
""".strip()
    ]

    if effect_names:
        values = ",\n".join(f"({effect_type},{sql_quote(name)})" for effect_type, name in effect_names)
        statements.append(f"INSERT INTO spell_effect_type_names (effectType, effectName) VALUES\n{values};")

    for i in range(0, len(placements), 500):
        chunk = placements[i : i + 500]
        values = []
        for placement in chunk:
            row = [sql_quote(getattr(placement, column)) for column in placement_columns]
            values.append("(" + ",".join(row) + ")")

        statements.append(
            "INSERT INTO world_creature_placements ("
            + ",".join(placement_columns)
            + ") VALUES\n"
            + ",\n".join(values)
            + ";"
        )

    statements.append(
        """
CREATE VIEW world_creature_spell_context AS
SELECT
  wcp.id AS placement_id,
  wcp.source_file,
  wcp.continent,
  wcp.zone,
  wcp.context,
  wcp.source_index,
  wcp.entity_type,
  wcp.creature_id,
  c.description AS creature_description,
  wcp.world_id,
  wcp.area_id,
  wcp.x,
  wcp.y,
  wcp.z,
  cs.id AS creature_spell_row_id,
  cs.spell_id AS jabbithole_spell_id,
  js.game_id AS spell4_id,
  js.name AS jabbithole_spell_name,
  js.tier AS jabbithole_tier,
  js.base_spell_id_game AS jabbithole_base_spell4_id,
  s4.spell4BaseIdBaseSpell AS spell4_base_id,
  s4.tierIndex AS spell4_tier,
  s4.description AS spell4_description
FROM world_creature_placements wcp
JOIN creature_spells cs ON cs.creature_id = wcp.creature_id
JOIN spells js ON js.id = cs.spell_id
LEFT JOIN creature2 c ON c.ID = wcp.creature_id
LEFT JOIN spell4 s4 ON s4.ID = js.game_id;

CREATE VIEW world_creature_activate_spell_context AS
SELECT
    wcp.id AS placement_id,
    wcp.source_file,
    wcp.continent,
    wcp.zone,
    wcp.context,
    wcp.source_index,
    wcp.entity_type,
    wcp.creature_id,
    c2.description AS creature_description,
    wcp.world_id,
    wcp.area_id,
    wcp.x,
    wcp.y,
    wcp.z,
    0 AS activate_slot,
    c2.spell4IdActivate00 AS spell4_id,
    s4.spell4BaseIdBaseSpell AS spell4_base_id,
    s4.tierIndex AS spell4_tier,
    s4.description AS spell4_description,
    c2.prerequisiteIdActivateSpell00 AS prerequisite_id_activate_spell,
    c2.localizedTextIdActivateSpellText AS localized_text_id_activate_spell_text
FROM world_creature_placements wcp
JOIN creature2 c2 ON c2.ID = wcp.creature_id
LEFT JOIN spell4 s4 ON s4.ID = c2.spell4IdActivate00
WHERE c2.spell4IdActivate00 > 0
UNION ALL
SELECT
    wcp.id AS placement_id,
    wcp.source_file,
    wcp.continent,
    wcp.zone,
    wcp.context,
    wcp.source_index,
    wcp.entity_type,
    wcp.creature_id,
    c2.description AS creature_description,
    wcp.world_id,
    wcp.area_id,
    wcp.x,
    wcp.y,
    wcp.z,
    1 AS activate_slot,
    c2.spell4IdActivate01 AS spell4_id,
    s4.spell4BaseIdBaseSpell AS spell4_base_id,
    s4.tierIndex AS spell4_tier,
    s4.description AS spell4_description,
    c2.prerequisiteIdActivateSpell01 AS prerequisite_id_activate_spell,
    c2.localizedTextIdActivateSpellText AS localized_text_id_activate_spell_text
FROM world_creature_placements wcp
JOIN creature2 c2 ON c2.ID = wcp.creature_id
LEFT JOIN spell4 s4 ON s4.ID = c2.spell4IdActivate01
WHERE c2.spell4IdActivate01 > 0
UNION ALL
SELECT
    wcp.id AS placement_id,
    wcp.source_file,
    wcp.continent,
    wcp.zone,
    wcp.context,
    wcp.source_index,
    wcp.entity_type,
    wcp.creature_id,
    c2.description AS creature_description,
    wcp.world_id,
    wcp.area_id,
    wcp.x,
    wcp.y,
    wcp.z,
    2 AS activate_slot,
    c2.spell4IdActivate02 AS spell4_id,
    s4.spell4BaseIdBaseSpell AS spell4_base_id,
    s4.tierIndex AS spell4_tier,
    s4.description AS spell4_description,
    c2.prerequisiteIdActivateSpell02 AS prerequisite_id_activate_spell,
    c2.localizedTextIdActivateSpellText AS localized_text_id_activate_spell_text
FROM world_creature_placements wcp
JOIN creature2 c2 ON c2.ID = wcp.creature_id
LEFT JOIN spell4 s4 ON s4.ID = c2.spell4IdActivate02
WHERE c2.spell4IdActivate02 > 0
UNION ALL
SELECT
    wcp.id AS placement_id,
    wcp.source_file,
    wcp.continent,
    wcp.zone,
    wcp.context,
    wcp.source_index,
    wcp.entity_type,
    wcp.creature_id,
    c2.description AS creature_description,
    wcp.world_id,
    wcp.area_id,
    wcp.x,
    wcp.y,
    wcp.z,
    3 AS activate_slot,
    c2.spell4IdActivate03 AS spell4_id,
    s4.spell4BaseIdBaseSpell AS spell4_base_id,
    s4.tierIndex AS spell4_tier,
    s4.description AS spell4_description,
    c2.prerequisiteIdActivateSpell03 AS prerequisite_id_activate_spell,
    c2.localizedTextIdActivateSpellText AS localized_text_id_activate_spell_text
FROM world_creature_placements wcp
JOIN creature2 c2 ON c2.ID = wcp.creature_id
LEFT JOIN spell4 s4 ON s4.ID = c2.spell4IdActivate03
WHERE c2.spell4IdActivate03 > 0;

CREATE VIEW world_creature_spell_coverage_gaps AS
SELECT
    wcp.id AS placement_id,
    wcp.source_file,
    wcp.continent,
    wcp.zone,
    wcp.context,
    wcp.source_index,
    wcp.entity_type,
    wcp.creature_id,
    c.description AS creature_description,
    wcp.world_id,
    wcp.area_id,
    wcp.x,
    wcp.y,
    wcp.z
FROM world_creature_placements wcp
LEFT JOIN creature2 c ON c.ID = wcp.creature_id
LEFT JOIN creature_spells cs ON cs.creature_id = wcp.creature_id
WHERE cs.id IS NULL;

CREATE VIEW world_creature_spell_coverage_summary AS
SELECT
    wcp.source_file,
    wcp.continent,
    wcp.zone,
    wcp.world_id,
    COUNT(DISTINCT wcp.id) AS placements,
    COUNT(DISTINCT wcp.creature_id) AS creatures,
    COUNT(DISTINCT CASE WHEN cs.id IS NOT NULL THEN wcp.creature_id END) AS creatures_with_spell_rows,
    COUNT(DISTINCT CASE WHEN cs.id IS NULL THEN wcp.creature_id END) AS creatures_without_spell_rows,
    COUNT(DISTINCT CASE WHEN cs.id IS NULL THEN wcp.id END) AS placements_without_spell_rows,
    GROUP_CONCAT(
        DISTINCT CASE
            WHEN cs.id IS NULL THEN CONCAT(wcp.creature_id, ':', LEFT(COALESCE(c.description, 'unknown creature'), 96))
            ELSE NULL
        END
        ORDER BY wcp.creature_id SEPARATOR ' | '
    ) AS missing_creatures
FROM world_creature_placements wcp
LEFT JOIN creature2 c ON c.ID = wcp.creature_id
LEFT JOIN creature_spells cs ON cs.creature_id = wcp.creature_id
GROUP BY wcp.source_file, wcp.continent, wcp.zone, wcp.world_id;

CREATE TABLE world_effect_family_context AS
SELECT
  wcsc.continent,
  wcsc.zone,
  wcsc.creature_id,
  wcsc.creature_description,
  wcsc.spell4_id,
  wcsc.spell4_base_id,
  wcsc.spell4_tier,
  wcsc.spell4_description,
  e.effectType,
  COUNT(*) AS placement_effect_rows,
  COUNT(DISTINCT wcsc.placement_id) AS placements,
  COUNT(DISTINCT e.ID) AS distinct_effect_rows,
  SUM(e.delayTime > 0) AS delayed_rows,
  SUM(e.tickTime > 0) AS tick_rows,
  SUM(e.durationTime > 0) AS duration_rows
FROM world_creature_spell_context wcsc
JOIN spell4effects e ON e.spellId = wcsc.spell4_id
GROUP BY
  wcsc.continent,
  wcsc.zone,
  wcsc.creature_id,
  wcsc.creature_description,
  wcsc.spell4_id,
  wcsc.spell4_base_id,
  wcsc.spell4_tier,
  wcsc.spell4_description,
  e.effectType;

CREATE INDEX idx_world_effect_family_context_effect ON world_effect_family_context(effectType);
CREATE INDEX idx_world_effect_family_context_creature ON world_effect_family_context(creature_id);
CREATE INDEX idx_world_effect_family_context_zone ON world_effect_family_context(continent, zone);

CREATE VIEW world_nonplayer_effect_family_summary AS
SELECT
  e.effectType,
  etn.effectName,
  COUNT(*) AS placement_effect_rows,
  COUNT(DISTINCT wcsc.placement_id) AS placements,
  COUNT(DISTINCT wcsc.creature_id) AS creatures,
  COUNT(DISTINCT wcsc.spell4_id) AS spells,
  SUM(e.delayTime > 0) AS delay_rows,
  SUM(e.tickTime > 0) AS tick_rows,
  SUM(e.durationTime > 0) AS duration_rows,
  SUM(e.prerequisiteIdCasterApply > 0 OR e.prerequisiteIdTargetApply > 0) AS apply_prereq_rows,
  SUM(e.prerequisiteIdCasterPersistence > 0 OR e.prerequisiteIdTargetPersistence > 0) AS persistence_prereq_rows
FROM world_creature_spell_context wcsc
JOIN spell4effects e ON e.spellId = wcsc.spell4_id
LEFT JOIN spell_effect_type_names etn ON etn.effectType = e.effectType
WHERE wcsc.entity_type = 0
GROUP BY e.effectType, etn.effectName;

CREATE VIEW world_nonplayer_creature_family_summary AS
SELECT
  wcsc.continent,
  wcsc.zone,
  wcsc.creature_id,
  wcsc.creature_description,
  COUNT(DISTINCT wcsc.placement_id) AS placements,
  COUNT(DISTINCT wcsc.spell4_id) AS spells,
  COUNT(e.ID) AS placement_effect_rows,
  GROUP_CONCAT(DISTINCT etn.effectName ORDER BY e.effectType SEPARATOR ', ') AS effect_families
FROM world_creature_spell_context wcsc
JOIN spell4effects e ON e.spellId = wcsc.spell4_id
LEFT JOIN spell_effect_type_names etn ON etn.effectType = e.effectType
WHERE wcsc.entity_type = 0
GROUP BY wcsc.continent, wcsc.zone, wcsc.creature_id, wcsc.creature_description;

CREATE VIEW world_runtime_spell_candidates AS
SELECT
  wcsc.continent,
  wcsc.zone,
  wcsc.entity_type,
  wcsc.creature_id,
  wcsc.creature_description,
  wcsc.spell4_id,
  wcsc.spell4_base_id,
  wcsc.spell4_tier,
  wcsc.spell4_description,
  COUNT(DISTINCT wcsc.placement_id) AS placements,
  COUNT(e.ID) AS placement_effect_rows,
  GROUP_CONCAT(DISTINCT COALESCE(etn.effectName, CAST(e.effectType AS CHAR)) ORDER BY e.effectType SEPARATOR ', ') AS effect_families,
  SUM(e.effectType IN (5, 8, 10, 12, 33, 118, 138)) > 0 AS has_damage_family,
  SUM(e.effectType IN (19, 25, 26, 94, 96)) > 0 AS has_proxy_family,
  SUM(e.effectType = 11) > 0 AS has_unit_property_modifier,
  SUM(e.effectType = 4) > 0 AS has_cc_state,
  SUM(e.effectType = 3) > 0 AS has_forced_move,
  SUM(e.effectType = 35) > 0 AS has_npc_execution_delay,
  SUM(e.delayTime > 0) > 0 AS has_delay,
  SUM(e.tickTime > 0) > 0 AS has_tick,
  SUM(e.durationTime > 0) > 0 AS has_duration,
  (
    COUNT(DISTINCT wcsc.placement_id) * 10
    + (SUM(e.effectType IN (5, 8, 10, 12, 33, 118, 138)) > 0) * 1000
    + (SUM(e.effectType IN (19, 25, 26, 94, 96)) > 0) * 900
    + (SUM(e.effectType = 11) > 0) * 800
    + (SUM(e.effectType = 4) > 0) * 400
    + (SUM(e.effectType = 3) > 0) * 300
    + (SUM(e.delayTime > 0) > 0) * 200
    + (SUM(e.tickTime > 0) > 0) * 200
    + (SUM(e.durationTime > 0) > 0) * 200
  ) AS priority_score,
  CONCAT('/spell inspect4 ', wcsc.spell4_id) AS inspect_command,
  CONCAT('/spell cast4 ', wcsc.spell4_id) AS cast_command,
  CONCAT('/spell inspect ', wcsc.spell4_base_id, ' ', wcsc.spell4_tier) AS inspect_base_tier_command,
  CONCAT('/spell cast ', wcsc.spell4_base_id, ' ', wcsc.spell4_tier) AS cast_base_tier_command
FROM world_creature_spell_context wcsc
JOIN spell4effects e ON e.spellId = wcsc.spell4_id
LEFT JOIN spell_effect_type_names etn ON etn.effectType = e.effectType
WHERE wcsc.spell4_id IS NOT NULL
GROUP BY
  wcsc.continent,
  wcsc.zone,
  wcsc.entity_type,
  wcsc.creature_id,
  wcsc.creature_description,
  wcsc.spell4_id,
  wcsc.spell4_base_id,
  wcsc.spell4_tier,
  wcsc.spell4_description;

CREATE VIEW world_proxy_chain_context AS
SELECT
  wcsc.continent,
  wcsc.zone,
  wcsc.entity_type,
  wcsc.creature_id,
  wcsc.creature_description,
  wcsc.spell4_id,
  wcsc.spell4_base_id,
  wcsc.spell4_tier,
  wcsc.spell4_description,
  e.ID AS spell4_effect_id,
  e.effectType,
  etn.effectName,
  e.orderIndex,
  e.targetFlags,
  e.delayTime,
  e.tickTime,
  e.durationTime,
  e.dataBits00 AS chained_spell4_id,
  chained.description AS chained_spell4_description,
  COUNT(DISTINCT wcsc.placement_id) AS placements
FROM world_creature_spell_context wcsc
JOIN spell4effects e ON e.spellId = wcsc.spell4_id
LEFT JOIN spell_effect_type_names etn ON etn.effectType = e.effectType
LEFT JOIN spell4 chained ON chained.ID = e.dataBits00
WHERE e.effectType IN (19, 25, 26, 94, 96)
GROUP BY
  wcsc.continent,
  wcsc.zone,
  wcsc.entity_type,
  wcsc.creature_id,
  wcsc.creature_description,
  wcsc.spell4_id,
  wcsc.spell4_base_id,
  wcsc.spell4_tier,
  wcsc.spell4_description,
  e.ID,
  e.effectType,
  etn.effectName,
  e.orderIndex,
  e.targetFlags,
  e.delayTime,
  e.tickTime,
  e.durationTime,
  e.dataBits00,
  chained.description;

CREATE VIEW world_unit_property_modifier_context AS
SELECT
  wcsc.continent,
  wcsc.zone,
  wcsc.entity_type,
  wcsc.creature_id,
  wcsc.creature_description,
  wcsc.spell4_id,
  wcsc.spell4_base_id,
  wcsc.spell4_tier,
  wcsc.spell4_description,
  e.ID AS spell4_effect_id,
  e.orderIndex,
  e.targetFlags,
  e.delayTime,
  e.tickTime,
  e.durationTime,
  e.prerequisiteIdCasterPersistence,
  e.prerequisiteIdTargetPersistence,
  e.dataBits00 AS property_id,
  u.enumName AS property_name,
  e.dataBits01 AS modifier_type_or_priority,
  e.dataBits02 AS percentage_value_bits,
  e.dataBits03 AS flat_value_bits,
  e.dataBits04 AS level_scale_value_bits,
  COUNT(DISTINCT wcsc.placement_id) AS placements
FROM world_creature_spell_context wcsc
JOIN spell4effects e ON e.spellId = wcsc.spell4_id
LEFT JOIN unitproperty2 u ON u.ID = e.dataBits00
WHERE e.effectType = 11
GROUP BY
  wcsc.continent,
  wcsc.zone,
  wcsc.entity_type,
  wcsc.creature_id,
  wcsc.creature_description,
  wcsc.spell4_id,
  wcsc.spell4_base_id,
  wcsc.spell4_tier,
  wcsc.spell4_description,
  e.ID,
  e.orderIndex,
  e.targetFlags,
  e.delayTime,
  e.tickTime,
  e.durationTime,
  e.prerequisiteIdCasterPersistence,
  e.prerequisiteIdTargetPersistence,
  e.dataBits00,
  u.enumName,
  e.dataBits01,
  e.dataBits02,
  e.dataBits03,
  e.dataBits04;

CREATE VIEW world_damage_candidate_context AS
SELECT
  wcsc.continent,
  wcsc.zone,
  wcsc.entity_type,
  wcsc.creature_id,
  wcsc.creature_description,
  wcsc.spell4_id,
  wcsc.spell4_base_id,
  wcsc.spell4_tier,
  wcsc.spell4_description,
  e.ID AS spell4_effect_id,
  e.effectType,
  etn.effectName,
  e.orderIndex,
  e.targetFlags,
  e.damageType,
  e.delayTime,
  e.tickTime,
  e.durationTime,
  e.dataBits00 AS multiplier_bits,
  e.dataBits01 AS base_value,
  e.parameterType00,
  e.parameterValue00,
  e.parameterType01,
  e.parameterValue01,
  e.parameterType02,
  e.parameterValue02,
  e.parameterType03,
  e.parameterValue03,
  COUNT(DISTINCT wcsc.placement_id) AS placements
FROM world_creature_spell_context wcsc
JOIN spell4effects e ON e.spellId = wcsc.spell4_id
LEFT JOIN spell_effect_type_names etn ON etn.effectType = e.effectType
WHERE e.effectType IN (5, 8, 10, 12, 33, 118, 138)
GROUP BY
  wcsc.continent,
  wcsc.zone,
  wcsc.entity_type,
  wcsc.creature_id,
  wcsc.creature_description,
  wcsc.spell4_id,
  wcsc.spell4_base_id,
  wcsc.spell4_tier,
  wcsc.spell4_description,
  e.ID,
  e.effectType,
  etn.effectName,
  e.orderIndex,
  e.targetFlags,
  e.damageType,
  e.delayTime,
  e.tickTime,
  e.durationTime,
  e.dataBits00,
  e.dataBits01,
  e.parameterType00,
  e.parameterValue00,
  e.parameterType01,
  e.parameterValue01,
  e.parameterType02,
  e.parameterValue02,
  e.parameterType03,
  e.parameterValue03;
""".strip()
    )

    return "\n\n".join(statements)


def mysql_args(args: argparse.Namespace) -> list[str]:
    command = [str(args.mysql), f"-u{args.user}", f"--host={args.host}", f"--database={args.database}"]
    if args.password:
        command.insert(2, f"-p{args.password}")

    return command


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--world-db", type=pathlib.Path, default=DEFAULT_WORLD_DB)
    parser.add_argument("--mysql", type=pathlib.Path, default=DEFAULT_MYSQL if DEFAULT_MYSQL.exists() else pathlib.Path("mysql"))
    parser.add_argument("--database", default="nexus_spell_re")
    parser.add_argument("--host", default="localhost")
    parser.add_argument("--user", default="bankai")
    parser.add_argument("--password", default="bankai")
    args = parser.parse_args()

    placements = parse_world_placements(args.world_db)
    effect_names = parse_effect_type_names()
    sql = build_sql(placements, effect_names)

    with tempfile.NamedTemporaryFile("w", suffix=".sql", delete=False, encoding="utf-8") as handle:
        handle.write(sql)
        sql_path = pathlib.Path(handle.name)

    source_path = str(sql_path).replace("\\", "/")
    command = mysql_args(args) + ["-e", f"SET SESSION sql_mode=''; SOURCE {source_path};"]
    subprocess.check_call(command)

    print(f"placements={len(placements)}")
    print(f"effect_type_names={len(effect_names)}")
    print(f"sql_file={sql_path}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
