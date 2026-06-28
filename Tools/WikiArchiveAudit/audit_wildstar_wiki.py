#!/usr/bin/env python3
"""Audit facts from the local WildStar wiki archive against NexusForever data."""

from __future__ import annotations

import argparse
import csv
import html
import json
import re
import struct
import sys
from collections import Counter, defaultdict
from pathlib import Path
from typing import Iterable


CLASS_NAMES = [
    "Warrior",
    "Engineer",
    "Esper",
    "Medic",
    "Spellslinger",
    "Stalker",
]

RACE_NAMES = [
    "human",
    "cassian",
    "granok",
    "draken",
    "aurin",
    "mechari",
    "mordesh",
    "chua",
]

RACE_BY_CLIENT = {
    (1, 167): "human",
    (1, 166): "cassian",
    (3, 167): "granok",
    (4, 167): "aurin",
    (16, 167): "mordesh",
    (5, 166): "draken",
    (12, 166): "mechari",
    (13, 166): "chua",
}

SUPPORTED_STARTS = {
    3: "Nexus",
    4: "PreTutorial",
    5: "Level50",
}

START_NAMES = {
    0: "Arkship",
    1: "Demo01",
    2: "Demo02",
    3: "Nexus",
    4: "PreTutorial",
    5: "Level50",
    99: "CostumeOnly",
}

PATH_NAMES_BY_ID = {
    0: "Soldier",
    1: "Settler",
    2: "Scientist",
    3: "Explorer",
}

PATH_REWARD_KIND_MARKERS = {
    "item": ("Item2Id", "ItemCreate"),
    "spell": ("Spell4Id", "AddSpell"),
    "title": ("CharacterTitleId", "AddTitle"),
    "scanbot": ("PathScientistScanBotProfileId", "UnlockScanBotProfile"),
}

PATH_IDS_BY_NAME = {name: path_id for path_id, name in PATH_NAMES_BY_ID.items()}

PATH_ABILITY_PAGES = {
    "Back into the Fray": ("Soldier", "Back into the Fray", ("Back into the Fray",)),
    "Bail Out!": ("Soldier", "Bail Out!", ("Bail Out",)),
    "Combat Supply Drop": ("Soldier", "Combat Supply Drop", ("Combat Supply Drop",)),
    "Settler's Campfire": ("Settler", "Settler's Campfire", ("Campfire",)),
    "Summon: Mail Box": ("Settler", "Summon Mail Box", ("Mail Box",)),
    "Summon: Vendbot": ("Settler", "Summon Vendbot", ("Vendbot",)),
    "Summon: Crafting Station": ("Settler", "Summon Crafting Station", ("Crafting Station",)),
    "Holographic Distraction": ("Scientist", "Holographic Distraction", ("Holographic Distraction",)),
    "Summon Group": ("Scientist", "Summon Group", ("Summon Group",)),
    "Create Portal - Capital City": ("Scientist", "Create Portal - Capital City", ("Create Portal - Capital City",)),
    "Explorer's Safe Fall": ("Explorer", "Explorer's Safe Fall", ("Safe Fall",)),
    "Jetpack Jump": ("Explorer", "Jetpack Jump", ("Jetpack Jump",)),
    "Translocate Beacon": ("Explorer", "Translocate Beacon", ("Translocate Beacon",)),
}

WIKI_EQUIPMENT_TO_PROFICIENCY = {
    "Tech Sword": "GreatWeapon",
    "Launcher": "HeavyGun",
    "Psyblade": "Psyblade",
    "Resonators": "Resonators",
    "Dual Pistols": "Pistols",
    "Claws": "Claws",
}

WIKI_ARMOR_TO_PROFICIENCY = {
    "Heavy Armor": "HeavyArmor",
    "Medium Armor": "MediumArmor",
    "Light Armor": "LightArmor",
}

PROXY_EFFECT_TYPES = {
    0x0013,  # ProxyLinearAE
    0x0019,  # ProxyChannel
    0x001A,  # Proxy
    0x005E,  # ProxyChannelVariableTime
    0x0060,  # ProxyRandomExclusive
}

IMPLEMENTED_AUDIT_DOMAINS = {
    "character-creation": "Class/race availability, starts, paths, path rewards, path abilities, and rested-XP spell coverage.",
    "abilities": "Class ability pages, client SpellLevel unlock rows, and server spell-grant coverage.",
    "amps": "AMP wiki pages, client EldanAugmentation rows, AMP id persistence, and server AMP validation.",
    "housing": "Residence, plot, plug, decor, comfort/rested-XP, and housing spell behavior.",
    "quests": "Quest wiki pages, client Quest2/objective/reward rows, and server quest lifecycle coverage.",
    "achievements": "Achievement wiki pages, client achievement/checklist/title rows, and server achievement runtime coverage.",
    "tradeskills": "Tradeskill wiki pages, client schematic/material/tier rows, and server profession/crafting coverage.",
    "lore": "Datacube, journal, zone-lore, Galactic Archive, and story-panel coverage.",
}

PLANNED_AUDIT_DOMAINS: dict[str, str] = {}

CLASS_ABILITY_TYPES = ("Assault", "Support", "Utility")
AMP_BRANCH_NAMES = ("Assault", "Hybrid A/S", "Support", "Hybrid S/U", "Utility", "Hybrid A/U")
HOUSING_CORE_TABLES = (
    "HousingResidenceInfo",
    "HousingPropertyInfo",
    "HousingMapInfo",
    "HousingPlotInfo",
    "HousingPlotType",
    "HousingPlugItem",
    "HousingDecorInfo",
    "HousingDecorLimitCategory",
    "HousingWallpaperInfo",
    "HousingNeighborhoodInfo",
)
HOUSING_DECOR_BONUS_CATEGORIES = ("Pride", "Ambience", "Aroma", "Lighting", "Comfort")
QUEST_CORE_TABLES = (
    "Quest2",
    "QuestObjective",
    "Quest2Reward",
    "QuestCategory",
    "QuestGroup",
    "QuestHub",
    "Episode",
    "EpisodeQuest",
    "PeriodicQuestGroup",
    "PeriodicQuestSet",
)
ACHIEVEMENT_CORE_TABLES = (
    "Achievement",
    "AchievementChecklist",
    "AchievementCategory",
    "AchievementGroup",
    "AchievementSubGroup",
    "AchievementText",
    "CharacterTitle",
    "CharacterTitleCategory",
    "Challenge",
    "ChallengeTier",
)
TRADESKILL_CORE_TABLES = (
    "Tradeskill",
    "TradeskillTier",
    "TradeskillTalentTier",
    "TradeskillSchematic2",
    "TradeskillMaterial",
    "TradeskillMaterialCategory",
    "TradeskillProficiency",
    "TradeskillHarvestingInfo",
    "TradeskillCatalyst",
    "TradeskillCatalystOrdering",
    "TradeskillBonus",
    "TradeskillAdditive",
    "TradeskillAchievementReward",
    "TradeskillAchievementLayout",
    "MaterialData",
    "MaterialType",
    "MaterialSet",
    "MaterialRemap",
    "ReplaceableMaterialInfo",
    "ItemRuneInstance",
)
TRADESKILL_WIKI_FACT_PAGE_TITLES = ("Tradeskill", "Crafting", "Tradeskills")
TRADESKILL_WIKI_PROFESSION_PAGE_TITLES = (
    "Architect",
    "Armorer",
    "Cooking",
    "Farmer",
    "Mining",
    "Outfitter",
    "Relic Hunter",
    "Runecrafting",
    "Salvaging",
    "Survivalist",
    "Tailor",
    "Technologist",
    "Weaponsmith",
)
TRADESKILL_NAME_BY_ID = {
    1: "Weaponsmith",
    2: "Cooking",
    12: "Armorer",
    13: "Mining",
    14: "Outfitter",
    15: "Survivalist",
    16: "Technologist",
    17: "Architect",
    18: "Relic Hunter",
    19: "Fishing",
    20: "Farmer",
    21: "Tailor",
    22: "Runecrafting",
}
TRADESKILL_WIKI_PROFESSION_TO_ID = {
    name: tradeskill_id
    for tradeskill_id, name in TRADESKILL_NAME_BY_ID.items()
    if name in TRADESKILL_WIKI_PROFESSION_PAGE_TITLES
}
LORE_CORE_TABLES = (
    "Datacube",
    "DatacubeVolume",
    "PathScientistDatacubeDiscovery",
    "ArchiveArticle",
    "ArchiveEntry",
    "ArchiveEntryUnlockRule",
    "ArchiveLink",
    "ArchiveCategory",
    "StoryPanel",
)

DEFAULT_AUDIT_DOMAINS = tuple(IMPLEMENTED_AUDIT_DOMAINS)


def repo_root() -> Path:
    return Path(__file__).resolve().parents[2]


def read_text(path: Path) -> str:
    return path.read_text(encoding="utf-8")


def parse_sql_values(line: str) -> list[str]:
    match = re.search(r"VALUES\s*\((.*)\);", line)
    if not match:
        raise ValueError(f"not an INSERT VALUES line: {line[:80]}")

    reader = csv.reader([match.group(1)], quotechar="'", escapechar="\\", skipinitialspace=True)
    return next(reader)


def iter_insert_rows(path: Path, table_name: str) -> Iterable[list[str]]:
    prefix = f"INSERT INTO {table_name} VALUES "
    with path.open("r", encoding="utf-8") as handle:
        for line in handle:
            if line.startswith(prefix):
                yield parse_sql_values(line)


def find_wiki_page(wikitext_dir: Path, title: str) -> Path:
    suffix = f"-{title.replace(' ', '_')}.wiki"
    matches = sorted(wikitext_dir.glob(f"*{suffix}"))
    if len(matches) != 1:
        raise FileNotFoundError(f"expected one wiki page for {title}, found {len(matches)}")
    return matches[0]


def find_wiki_page_matches(wikitext_dir: Path, title: str) -> list[Path]:
    suffix = f"-{title.replace(' ', '_')}.wiki"
    return sorted(wikitext_dir.glob(f"*{suffix}"))


def parse_template_fields(text: str, template_name: str) -> dict[str, str]:
    match = re.search(r"\{\{\s*" + re.escape(template_name) + r"\b(.*?)\n\}\}", text, re.IGNORECASE | re.DOTALL)
    if not match:
        raise ValueError(f"template {template_name!r} not found")

    fields: dict[str, str] = {}
    for line in match.group(1).splitlines():
        line = line.strip()
        if not line.startswith("|") or "=" not in line:
            continue

        key, value = line[1:].split("=", 1)
        fields[key.strip().lower()] = value.strip()

    return fields


def parse_wiki_class_races(archive_dir: Path) -> dict[str, set[str]]:
    wikitext_dir = archive_dir / "pages" / "wikitext"
    result: dict[str, set[str]] = {}

    for class_name in CLASS_NAMES:
        page = find_wiki_page(wikitext_dir, class_name)
        fields = parse_template_fields(read_text(page), "class infobox")
        result[class_name] = {
            race
            for race in RACE_NAMES
            if fields.get(race, "").strip().casefold() == "yes"
        }

    return result


def parse_wiki_class_fields(archive_dir: Path) -> dict[str, dict[str, str]]:
    wikitext_dir = archive_dir / "pages" / "wikitext"
    result: dict[str, dict[str, str]] = {}

    for class_name in CLASS_NAMES:
        page = find_wiki_page(wikitext_dir, class_name)
        result[class_name] = parse_template_fields(read_text(page), "class infobox")

    return result


def parse_wiki_class_roles(archive_dir: Path) -> dict[str, set[str]]:
    fields_by_class = parse_wiki_class_fields(archive_dir)
    result: dict[str, set[str]] = {}

    for class_name, fields in fields_by_class.items():
        roles: set[str] = set()
        role_text = fields.get("role", "")
        if "DPS" in role_text:
            roles.add("DPS")
        if "Tank" in role_text:
            roles.add("Tank")
        if "Healer" in role_text:
            roles.add("Healer")
        result[class_name] = roles

    return result


def parse_code_default_roles(matching_data_manager: Path) -> dict[str, set[str]]:
    text = read_text(matching_data_manager)
    match = re.search(r"public Role GetDefaultRole\(Class @class\).*?switch \(@class\)\s*\{(.*?)\n\s*\}", text, re.DOTALL)
    if not match:
        raise ValueError("GetDefaultRole switch body not found")

    result: dict[str, set[str]] = {}
    pending_classes: list[str] = []
    for line in match.group(1).splitlines():
        class_match = re.search(r"case Class\.([A-Za-z_][A-Za-z0-9_]*):", line)
        if class_match:
            pending_classes.append(class_match.group(1))
            continue

        return_match = re.search(r"return\s+([^;]+);", line)
        if return_match:
            roles = set(re.findall(r"Role\.([A-Za-z_][A-Za-z0-9_]*)", return_match.group(1)))
            roles.discard("None")
            for class_name in pending_classes:
                result[class_name] = roles
            pending_classes = []

    return result


def parse_wiki_class_proficiencies(archive_dir: Path) -> dict[str, set[str]]:
    fields_by_class = parse_wiki_class_fields(archive_dir)
    result: dict[str, set[str]] = {}

    for class_name, fields in fields_by_class.items():
        proficiencies: set[str] = set()
        equipment = fields.get("equipment", "")
        armor = fields.get("armor", "")

        for text, proficiency in WIKI_EQUIPMENT_TO_PROFICIENCY.items():
            if text in equipment:
                proficiencies.add(proficiency)

        for text, proficiency in WIKI_ARMOR_TO_PROFICIENCY.items():
            if text in armor:
                proficiencies.add(proficiency)

        result[class_name] = proficiencies

    return result


def parse_item_proficiencies(item_proficiency_enum: Path) -> dict[str, int]:
    text = read_text(item_proficiency_enum)
    values: dict[str, int] = {}

    for match in re.finditer(r"^\s*([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(0x[0-9A-Fa-f]+|\d+)", text, re.MULTILINE):
        values[match.group(1)] = int(match.group(2), 0)

    return values


def parse_client_class_proficiencies(client_sql_dir: Path, root: Path) -> dict[str, set[str]]:
    proficiency_values = parse_item_proficiencies(root / "Source" / "NexusForever.Game.Static" / "Entity" / "ItemProficiency.cs")
    result: dict[str, set[str]] = {}

    for values in iter_insert_rows(client_sql_dir / "Class.tbl.sql", "Class"):
        class_name = values[1]
        if class_name.casefold() == "esper":
            class_name = "Esper"
        if class_name not in CLASS_NAMES:
            continue

        flags = int(values[14])
        result[class_name] = {
            name
            for name, value in proficiency_values.items()
            if value != 0 and (flags & value) == value
        }

    return result


def parse_client_classes(client_sql_dir: Path) -> dict[int, str]:
    class_by_id: dict[int, str] = {}
    for values in iter_insert_rows(client_sql_dir / "Class.tbl.sql", "Class"):
        class_id = int(values[0])
        class_name = values[1]
        if class_name.casefold() == "esper":
            class_name = "Esper"
        class_by_id[class_id] = class_name

    return class_by_id


def parse_character_creation_rows(client_sql_dir: Path) -> list[dict[str, int | bool]]:
    rows: list[dict[str, int | bool]] = []

    for values in iter_insert_rows(client_sql_dir / "CharacterCreation.tbl.sql", "CharacterCreation"):
        rows.append(
            {
                "id": int(values[0]),
                "class_id": int(values[1]),
                "race_id": int(values[2]),
                "sex": int(values[3]),
                "faction_id": int(values[4]),
                "costume_only": int(values[5]) == 1,
                "enabled": int(values[22]) == 1,
                "start": int(values[23]),
                "xp": int(values[24]),
                "entitlement_id": int(values[27]),
            }
        )

    return rows


def parse_client_class_races(client_sql_dir: Path) -> dict[str, set[str]]:
    class_by_id = parse_client_classes(client_sql_dir)
    result: dict[str, set[str]] = defaultdict(set)

    for row in parse_character_creation_rows(client_sql_dir):
        if not row["enabled"]:
            continue

        class_name = class_by_id.get(int(row["class_id"]))
        race_name = RACE_BY_CLIENT.get((int(row["race_id"]), int(row["faction_id"])))
        if class_name in CLASS_NAMES and race_name:
            result[class_name].add(race_name)

    return result


def parse_seeded_character_starts(character_context: Path) -> set[tuple[int, int, int]]:
    seeded: set[tuple[int, int, int]] = set()
    text = read_text(character_context)

    for block in re.findall(r"new CharacterCreateModel\s*\{(.*?)\n\s*\}", text, re.DOTALL):
        race = re.search(r"\bRace\s*=\s*(\d+)", block)
        faction = re.search(r"\bFaction\s*=\s*(\d+)", block)
        start = re.search(r"\bCreationStart\s*=\s*(\d+)", block)
        if race and faction and start:
            seeded.add((int(race.group(1)), int(faction.group(1)), int(start.group(1))))

    return seeded


def expected_supported_start_keys(client_sql_dir: Path) -> set[tuple[int, int, int]]:
    expected: set[tuple[int, int, int]] = set()

    for row in parse_character_creation_rows(client_sql_dir):
        if not row["enabled"] or row["costume_only"]:
            continue
        start = int(row["start"])
        if start not in SUPPORTED_STARTS:
            continue
        expected.add((int(row["race_id"]), int(row["faction_id"]), start))

    return expected


def parse_wiki_paths(archive_dir: Path) -> set[str]:
    page = find_wiki_page(archive_dir / "pages" / "wikitext", "Path")
    text = read_text(page)
    return set(re.findall(r"'''\[\[([A-Za-z]+)\]\]'''", text))


def parse_code_paths(path_enum: Path) -> set[str]:
    text = read_text(path_enum)
    body = re.search(r"enum\s+Path\s*:\s*byte\s*\{(.*?)\n\s*\}", text, re.DOTALL)
    if not body:
        raise ValueError("Path enum body not found")

    names: set[str] = set()
    for line in body.group(1).splitlines():
        match = re.match(r"\s*([A-Za-z_][A-Za-z0-9_]*)\s*=", line)
        if match:
            names.add(match.group(1))
    return names


def parse_client_path_levels(client_sql_dir: Path) -> dict[int, dict[int, int]]:
    levels: dict[int, dict[int, int]] = defaultdict(dict)

    for values in iter_insert_rows(client_sql_dir / "PathLevel.tbl.sql", "PathLevel"):
        path_id = int(values[1])
        level = int(values[2])
        xp = int(values[3])
        levels[path_id][level] = xp

    return levels


def parse_client_path_rewards(client_sql_dir: Path) -> list[dict[str, int]]:
    rows: list[dict[str, int]] = []

    for values in iter_insert_rows(client_sql_dir / "PathReward.tbl.sql", "PathReward"):
        rows.append(
            {
                "id": int(values[0]),
                "type": int(values[1]),
                "object_id": int(values[2]),
                "spell_id": int(values[3]),
                "item_id": int(values[4]),
                "quest_id": int(values[5]),
                "title_id": int(values[6]),
                "prerequisite_id": int(values[7]),
                "count": int(values[8]),
                "flags": int(values[9]),
                "scanbot_profile_id": int(values[10]),
            }
        )

    return rows


def path_reward_object_id_for_level(path_id: int, level: int) -> int:
    base_reward_object_id = path_id * 30 + 6
    if level <= 1:
        return base_reward_object_id

    return base_reward_object_id + 1 + min(level - 2, 29)


def path_reward_level_from_object_id(object_id: int) -> tuple[int, int] | None:
    for path_id in PATH_NAMES_BY_ID:
        base_reward_object_id = path_id * 30 + 6
        if base_reward_object_id <= object_id <= base_reward_object_id + 29:
            return path_id, object_id - base_reward_object_id + 1

    return None


def path_reward_kinds(row: dict[str, int]) -> set[str]:
    kinds: set[str] = set()
    if row["item_id"] > 0:
        kinds.add("item")
    if row["spell_id"] > 0:
        kinds.add("spell")
    if row["title_id"] > 0:
        kinds.add("title")
    if row["scanbot_profile_id"] > 0:
        kinds.add("scanbot")
    return kinds


def is_grantable_level_reward(row: dict[str, int]) -> bool:
    if row["flags"] > 0:
        return False
    if row["type"] != 0:
        return False
    return bool(path_reward_kinds(row))


def parse_server_path_reward_kinds(path_manager: Path) -> set[str]:
    text = read_text(path_manager)
    kinds: set[str] = set()

    for kind, markers in PATH_REWARD_KIND_MARKERS.items():
        if all(marker in text for marker in markers):
            kinds.add(kind)

    return kinds


def parse_wiki_path_reward_ranks(archive_dir: Path, path_name: str) -> dict[int, str]:
    page = find_wiki_page(archive_dir / "pages" / "wikitext", path_name)
    text = read_text(page)
    match = re.search(
        r"==\s*" + re.escape(path_name) + r"\s+path rewards\s*==(?P<body>.*?)(?:\n==|\Z)",
        text,
        re.IGNORECASE | re.DOTALL,
    )
    if not match:
        raise ValueError(f"{path_name} path rewards section not found")

    ranks: dict[int, str] = {}
    for line in match.group("body").splitlines():
        rank = re.match(r"\|\s*(\d+)\s*\|\|\s*(.*)$", line)
        if rank:
            ranks[int(rank.group(1))] = rank.group(2).strip()

    return ranks


def parse_wiki_path_ability_levels(archive_dir: Path, page_title: str) -> set[int]:
    page = find_wiki_page(archive_dir / "pages" / "wikitext", page_title)
    text = read_text(page)
    levels = {
        int(match)
        for match in re.findall(r"path level\s+(\d+)", text, re.IGNORECASE)
    }

    fields = parse_template_fields(text, "ability infobox")
    unlock = fields.get("unlock", "")
    if unlock.isdigit():
        levels.add(int(unlock))

    return levels


def wiki_page_title_from_path(path: Path) -> str:
    if "-" not in path.stem:
        return path.stem.replace("_", " ")

    return path.stem.split("-", 1)[1].replace("_", " ")


def normalise_name(value: str) -> str:
    value = html.unescape(value)
    value = re.sub(r"\([^)]*\)", " ", value)
    value = value.replace("&", " and ")
    value = re.sub(r"[^A-Za-z0-9]+", " ", value).casefold()
    return re.sub(r"\s+", " ", value).strip()


def compact_normalised_name(value: str) -> str:
    return normalise_name(value).replace(" ", "")


def ability_name_matches_description(ability_name: str, description: str) -> bool:
    normalised_ability = normalise_name(ability_name)
    normalised_description = normalise_name(description)
    if normalised_ability in normalised_description:
        return True

    compact_ability = compact_normalised_name(ability_name)
    return len(compact_ability) >= 6 and compact_ability in compact_normalised_name(description)


def canonical_class_name(value: str) -> str:
    normalised_value = normalise_name(value)
    for class_name in CLASS_NAMES:
        if normalise_name(class_name) == normalised_value:
            return class_name

    return value


def parse_int_from_text(value: str) -> int | None:
    match = re.search(r"\d+", value)
    if not match:
        return None

    return int(match.group(0))


def parse_wiki_ability_pages(archive_dir: Path) -> list[dict[str, str | int | bool]]:
    wikitext_dir = archive_dir / "pages" / "wikitext"
    abilities: list[dict[str, str | int | bool]] = []

    for page in sorted(wikitext_dir.glob("*.wiki")):
        text = read_text(page)
        if "{{ability infobox" not in text.casefold():
            continue

        title = wiki_page_title_from_path(page)
        if title.startswith("Template "):
            continue

        fields = parse_template_fields(text, "ability infobox")
        unlock_level = parse_int_from_text(fields.get("unlock", ""))

        abilities.append(
            {
                "title": title,
                "class": canonical_class_name(fields.get("class", "")),
                "type": fields.get("type", ""),
                "unlock_level": unlock_level if unlock_level is not None else 0,
                "has_unlock_level": unlock_level is not None,
                "removed": "{{removed" in text.casefold(),
            }
        )

    return abilities


def parse_wiki_amp_pages(archive_dir: Path) -> list[dict[str, str | int | bool | tuple[str, ...]]]:
    wikitext_dir = archive_dir / "pages" / "wikitext"
    amp_pages: list[dict[str, str | int | bool | tuple[str, ...]]] = []

    for page in sorted(wikitext_dir.glob("*.wiki")):
        text = read_text(page)
        if "{{amp infobox" not in text.casefold():
            continue

        title = wiki_page_title_from_path(page)
        if title.startswith("Template "):
            continue

        fields = parse_template_fields(text, "AMP infobox")
        classes = tuple(
            class_name
            for class_name in CLASS_NAMES
            if re.search(r"\[\[Category:" + re.escape(class_name) + r"(?: [^\]]*)? AMPs\]\]", text)
        )

        amp_pages.append(
            {
                "title": title,
                "branch": fields.get("branch", ""),
                "rank": parse_int_from_text(fields.get("rank", "")) or 0,
                "cost": parse_int_from_text(fields.get("cost", "")) or 0,
                "classes": classes,
                "removed": "{{removed" in text.casefold(),
            }
        )

    return amp_pages


def parse_client_spells(client_sql_dir: Path) -> dict[int, str]:
    spells: dict[int, str] = {}

    for values in iter_insert_rows(client_sql_dir / "Spell4.tbl.sql", "Spell4"):
        spells[int(values[0])] = values[1]

    return spells


def parse_client_spell_rows(client_sql_dir: Path) -> dict[int, dict[str, int | str]]:
    spells: dict[int, dict[str, int | str]] = {}

    for values in iter_insert_rows(client_sql_dir / "Spell4.tbl.sql", "Spell4"):
        spells[int(values[0])] = {
            "description": values[1],
            "base_spell_id": int(values[2]),
            "tier_index": int(values[3]),
        }

    return spells


def parse_client_class_spell_unlocks(client_sql_dir: Path) -> list[dict[str, int | str]]:
    class_by_id = parse_client_classes(client_sql_dir)
    spell_rows = parse_client_spell_rows(client_sql_dir)
    unlocks: list[dict[str, int | str]] = []

    for values in iter_insert_rows(client_sql_dir / "SpellLevel.tbl.sql", "SpellLevel"):
        class_name = class_by_id.get(int(values[1]))
        if class_name not in CLASS_NAMES:
            continue

        spell_id = int(values[4])
        spell_row = spell_rows.get(spell_id, {})
        unlocks.append(
            {
                "class": class_name,
                "level": int(values[2]),
                "prerequisite_id": int(values[3]),
                "spell_id": spell_id,
                "base_spell_id": int(spell_row.get("base_spell_id", 0)),
                "description": str(spell_row.get("description", "")),
            }
        )

    return unlocks


def parse_client_eldan_augmentations(client_sql_dir: Path) -> list[dict[str, int | str]]:
    class_by_id = parse_client_classes(client_sql_dir)
    augmentations: list[dict[str, int | str]] = []

    for values in iter_insert_rows(client_sql_dir / "EldanAugmentation.tbl.sql", "EldanAugmentation"):
        class_id = int(values[3])
        if class_id == 0:
            class_name = "Global"
        else:
            class_name = class_by_id.get(class_id, f"Class{class_id}")

        augmentations.append(
            {
                "id": int(values[0]),
                "class_id": class_id,
                "class": class_name,
                "power_cost": int(values[4]),
                "required_id": int(values[5]),
                "spell_id": int(values[6]),
                "item_unlock_id": int(values[7]),
                "category_id": int(values[8]),
                "category_tier": int(values[9]),
            }
        )

    return augmentations


def parse_client_eldan_augmentation_categories(client_sql_dir: Path) -> list[dict[str, int]]:
    categories: list[dict[str, int]] = []

    for values in iter_insert_rows(client_sql_dir / "EldanAugmentationCategory.tbl.sql", "EldanAugmentationCategory"):
        categories.append(
            {
                "id": int(values[0]),
                "tier2_category_00": int(values[1]),
                "tier2_category_01": int(values[2]),
                "tier2_cost_00": int(values[3]),
                "tier2_cost_01": int(values[4]),
                "tier3_cost_00": int(values[5]),
                "tier3_cost_01": int(values[6]),
            }
        )

    return categories


def count_client_table_rows(client_sql_dir: Path, table_name: str) -> int:
    return sum(1 for _ in iter_insert_rows(client_sql_dir / f"{table_name}.tbl.sql", table_name))


def parse_wiki_housing_facts(archive_dir: Path) -> dict[str, bool | int | tuple[str, ...]]:
    wikitext_dir = archive_dir / "pages" / "wikitext"
    housing_text = read_text(find_wiki_page(wikitext_dir, "Housing"))
    customizing_text = read_text(find_wiki_page(wikitext_dir, "Customizing your house"))
    housing_text_folded = housing_text.casefold()

    return {
        "level_14_unlock": "level 14" in housing_text_folded,
        "rested_xp": "==rested xp==" in housing_text_folded or "rest xp" in housing_text_folded,
        "decor_bonus_categories": tuple(
            category
            for category in HOUSING_DECOR_BONUS_CATEGORIES
            if category.casefold() in housing_text_folded
        ),
        "sockets_and_plugs": "sockets & plugs" in housing_text_folded
        and "plugs" in housing_text_folded,
        "visitor_rules": "visitor rules" in housing_text_folded,
        "communities": "communities" in housing_text_folded,
        "housing_expeditions": "housing expeditions" in housing_text_folded,
        "five_times_scale_claim": "five times bigger or smaller" in housing_text_folded,
        "customizing_page_sections": len(re.findall(r"^\s*[A-Za-z][A-Za-z ]+\s*=", customizing_text, re.MULTILINE)),
    }


def parse_client_housing_residence_rows(client_sql_dir: Path) -> list[dict[str, int]]:
    rows: list[dict[str, int]] = []

    for values in iter_insert_rows(client_sql_dir / "HousingResidenceInfo.tbl.sql", "HousingResidenceInfo"):
        rows.append(
            {
                "id": int(values[0]),
                "default_roof": int(values[1]),
                "default_entryway": int(values[2]),
                "default_door": int(values[3]),
                "default_wallpaper": int(values[4]),
                "inside_location_00": int(values[5]),
                "outside_location_00": int(values[9]),
            }
        )

    return rows


def parse_client_housing_plot_rows(client_sql_dir: Path) -> list[dict[str, int]]:
    rows: list[dict[str, int]] = []

    for values in iter_insert_rows(client_sql_dir / "HousingPlotInfo.tbl.sql", "HousingPlotInfo"):
        rows.append(
            {
                "id": int(values[0]),
                "world_socket_id": int(values[1]),
                "plot_type": int(values[2]),
                "property_info_id": int(values[3]),
                "property_plot_index": int(values[4]),
                "default_plug_item_id": int(values[5]),
            }
        )

    return rows


def parse_client_housing_plug_item_rows(client_sql_dir: Path) -> list[dict[str, int | float]]:
    rows: list[dict[str, int | float]] = []

    for values in iter_insert_rows(client_sql_dir / "HousingPlugItem.tbl.sql", "HousingPlugItem"):
        rows.append(
            {
                "id": int(values[0]),
                "plot_type_id": int(values[2]),
                "flags": int(values[8]),
                "contribution_00": int(values[18]),
                "contribution_01": int(values[19]),
                "contribution_02": int(values[20]),
                "contribution_03": int(values[21]),
                "contribution_04": int(values[22]),
                "next_upgrade_id": int(values[23]),
                "prerequisite_00": int(values[27]),
                "prerequisite_01": int(values[28]),
                "prerequisite_02": int(values[29]),
                "prerequisite_unlock": int(values[30]),
                "housing_build_id": int(values[31]),
                "upkeep_type": int(values[32]),
                "upkeep_charges": int(values[33]),
                "upkeep_time": float(values[34]),
                "plug_type": int(values[46]),
            }
        )

    return rows


def parse_client_housing_decor_rows(client_sql_dir: Path) -> list[dict[str, int | float]]:
    rows: list[dict[str, int | float]] = []

    for values in iter_insert_rows(client_sql_dir / "HousingDecorInfo.tbl.sql", "HousingDecorInfo"):
        rows.append(
            {
                "id": int(values[0]),
                "decor_type_id": int(values[1]),
                "hook_type_id": int(values[2]),
                "flags": int(values[4]),
                "active_prop_creature_id": int(values[8]),
                "prerequisite_id": int(values[9]),
                "interior_buff_spell_id": int(values[10]),
                "limit_category_id": int(values[11]),
                "min_scale": float(values[14]),
                "max_scale": float(values[15]),
            }
        )

    return rows


def extract_wiki_section(text: str, heading: str) -> str:
    pattern = r"^==\s*" + re.escape(heading) + r"\s*==\s*(.*?)(?=^==[^=].*?==|\Z)"
    match = re.search(pattern, text, re.IGNORECASE | re.MULTILINE | re.DOTALL)
    return match.group(1) if match else ""


def parse_wiki_quest_pages(archive_dir: Path) -> list[dict[str, str | int | bool]]:
    wikitext_dir = archive_dir / "pages" / "wikitext"
    quest_pages: list[dict[str, str | int | bool]] = []

    for page in sorted(wikitext_dir.glob("*.wiki")):
        text = read_text(page)
        if "{{quest infobox" not in text.casefold():
            continue

        title = wiki_page_title_from_path(page)
        if title.startswith("Template "):
            continue

        fields = parse_template_fields(text, "quest infobox")
        objectives_section = extract_wiki_section(text, "Objectives")
        objective_lines = [
            line
            for line in objectives_section.splitlines()
            if line.lstrip().startswith("*")
        ]

        quest_pages.append(
            {
                "title": title,
                "faction": fields.get("faction", ""),
                "location": fields.get("location", ""),
                "episode": fields.get("episode", ""),
                "has_rewards": bool(fields.get("rewards", "") or fields.get("currency", "")),
                "has_previous": bool(fields.get("previous", "")),
                "has_next": bool(fields.get("next", "")),
                "objective_count": len(objective_lines),
                "has_description": bool(extract_wiki_section(text, "Description").strip()),
                "has_episode_progression": "episode progression" in text.casefold(),
            }
        )

    return quest_pages


def parse_client_quest_rows(client_sql_dir: Path) -> list[dict[str, int | tuple[int, ...]]]:
    rows: list[dict[str, int | tuple[int, ...]]] = []

    for values in iter_insert_rows(client_sql_dir / "Quest2.tbl.sql", "Quest2"):
        rows.append(
            {
                "id": int(values[0]),
                "type": int(values[5]),
                "prerequisite_level": int(values[6]),
                "prerequisite_flags": int(values[7]),
                "prerequisite_quests": tuple(int(value) for value in values[8:11] if int(value) > 0),
                "prerequisite_race": int(values[11]),
                "prerequisite_item": int(values[12]),
                "quest_player_faction": int(values[13]),
                "world_zone_id": int(values[14]),
                "reward_xp_override": int(values[16]),
                "reward_cash_override": int(values[17]),
                "pushed_item_ids": tuple(int(value) for value in values[18:24] if int(value) > 0),
                "objective_ids": tuple(int(value) for value in values[30:36] if int(value) > 0),
                "prerequisite_class": int(values[42]),
                "faction_prerequisites": tuple(int(value) for value in values[44:47] if int(value) > 0),
                "quest_exclusion_prerequisites": tuple(int(value) for value in values[53:56] if int(value) > 0),
                "alt_receiver_prerequisites": tuple(int(value) for value in values[62:65] if int(value) > 0),
                "max_time_allowed_ms": int(values[72]),
                "prerequisite_id": int(values[73]),
                "quest_share_enum": int(values[74]),
                "virtual_item_ids": tuple(int(value) for value in values[88:92] if int(value) > 0),
                "reputation_reward_factions": tuple(int(value) for value in values[101:103] if int(value) > 0),
                "periodic_quest_group_id": int(values[107]),
                "quest_repeat_period": int(values[109]),
                "group_size": int(values[111]),
            }
        )

    return rows


def parse_client_quest_objective_rows(client_sql_dir: Path) -> list[dict[str, int]]:
    rows: list[dict[str, int]] = []

    for values in iter_insert_rows(client_sql_dir / "QuestObjective.tbl.sql", "QuestObjective"):
        rows.append(
            {
                "id": int(values[0]),
                "type": int(values[1]),
                "flags": int(values[2]),
                "data": int(values[3]),
                "count": int(values[4]),
                "max_time_allowed_ms": int(values[10]),
                "quest_direction_id": int(values[13]),
            }
        )

    return rows


def parse_client_quest_reward_rows(client_sql_dir: Path) -> list[dict[str, int]]:
    rows: list[dict[str, int]] = []

    for values in iter_insert_rows(client_sql_dir / "Quest2Reward.tbl.sql", "Quest2Reward"):
        rows.append(
            {
                "id": int(values[0]),
                "quest_id": int(values[1]),
                "type": int(values[2]),
                "object_id": int(values[3]),
                "amount": int(values[4]),
                "flags": int(values[5]),
            }
        )

    return rows


def parse_enum_values_by_id(enum_path: Path) -> dict[int, str]:
    text = read_text(enum_path)
    values: dict[int, str] = {}

    for match in re.finditer(r"^\s*([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(0x[0-9A-Fa-f]+|\d+)", text, re.MULTILINE):
        values[int(match.group(2), 0)] = match.group(1)

    return values


def parse_quest_client_name_map(root: Path) -> dict[int, str]:
    path = root / "Tools" / "DataMapping" / "output" / "quest_client_map.csv"
    if not path.exists():
        return {}

    names: dict[int, str] = {}
    with path.open("r", encoding="utf-8", newline="") as handle:
        for row in csv.DictReader(handle):
            quest_name = (row.get("quest_name") or "").strip()
            if quest_name:
                names[int(row["quest2_id"])] = quest_name

    return names


def parse_wiki_achievement_pages(archive_dir: Path) -> list[dict[str, str | int | bool]]:
    wikitext_dir = archive_dir / "pages" / "wikitext"
    achievement_pages: list[dict[str, str | int | bool]] = []

    for page in sorted(wikitext_dir.glob("*.wiki")):
        text = read_text(page)
        if "{{achievement infobox" not in text.casefold():
            continue

        title = wiki_page_title_from_path(page)
        if title.startswith("Template "):
            continue

        fields = parse_template_fields(text, "achievement infobox")
        achievement_pages.append(
            {
                "title": fields.get("name", "") or title,
                "style": fields.get("style", ""),
                "type": fields.get("type", ""),
                "subtype": fields.get("subtype", ""),
                "points": parse_int_from_text(fields.get("points", "")) or 0,
                "has_description": bool(fields.get("desc", "")),
                "has_criteria": bool(fields.get("criteria", "")),
                "has_title_reward": bool(fields.get("title", "")),
                "has_series": bool(extract_wiki_section(text, "Series").strip()),
            }
        )

    return achievement_pages


def parse_client_achievement_rows(client_sql_dir: Path) -> list[dict[str, int | str]]:
    rows: list[dict[str, int | str]] = []

    for values in iter_insert_rows(client_sql_dir / "Achievement.tbl.sql", "Achievement"):
        rows.append(
            {
                "id": int(values[0]),
                "type": int(values[1]),
                "category_id": int(values[2]),
                "flags": int(values[3]),
                "world_zone_id": int(values[4]),
                "object_id": int(values[9]),
                "object_id_alt": int(values[10]),
                "value": int(values[11]),
                "character_title_id": int(values[12]),
                "prerequisite_id": int(values[13]),
                "prerequisite_id_server": int(values[14]),
                "prerequisite_id_objective": int(values[15]),
                "prerequisite_id_objective_alt": int(values[16]),
                "parent_tier_id": int(values[17]),
                "group_id": int(values[19]),
                "subgroup_id": int(values[20]),
                "points_enum": int(values[21]),
                "steam_name": values[22],
            }
        )

    return rows


def parse_client_achievement_checklist_rows(client_sql_dir: Path) -> list[dict[str, int]]:
    rows: list[dict[str, int]] = []

    for values in iter_insert_rows(client_sql_dir / "AchievementChecklist.tbl.sql", "AchievementChecklist"):
        rows.append(
            {
                "id": int(values[0]),
                "achievement_id": int(values[1]),
                "bit": int(values[2]),
                "object_id": int(values[3]),
                "object_id_alt": int(values[4]),
                "prerequisite_id": int(values[5]),
                "prerequisite_id_alt": int(values[6]),
            }
        )

    return rows


def parse_achievement_client_name_map(root: Path) -> dict[int, str]:
    path = root / "Tools" / "DataMapping" / "output" / "achievement_client_map.csv"
    if not path.exists():
        return {}

    names: dict[int, str] = {}
    with path.open("r", encoding="utf-8", newline="") as handle:
        for row in csv.DictReader(handle):
            achievement_title = (row.get("achievement_title") or "").strip()
            if achievement_title:
                names[int(row["achievement_id"])] = achievement_title

    return names


def parse_wiki_tradeskill_facts(archive_dir: Path) -> dict[str, bool | int | tuple[str, ...]]:
    wikitext_dir = archive_dir / "pages" / "wikitext"
    tradeskill_text = read_text(find_wiki_page(wikitext_dir, "Tradeskill"))
    tradeskill_folded = tradeskill_text.casefold()
    profession_pages_found: list[str] = []
    profession_pages_missing: list[str] = []

    for title in TRADESKILL_WIKI_PROFESSION_PAGE_TITLES:
        if find_wiki_page_matches(wikitext_dir, title):
            profession_pages_found.append(title)
        else:
            profession_pages_missing.append(title)

    schematic_page_titles = [
        wiki_page_title_from_path(path)
        for path in sorted(wikitext_dir.glob("*List_of_*_schematics.wiki"))
    ]
    schematic_professions = tuple(
        sorted(
            {
                profession
                for profession in TRADESKILL_WIKI_PROFESSION_PAGE_TITLES
                for title in schematic_page_titles
                if normalise_name(profession) in normalise_name(title)
            }
        )
    )

    return {
        "main_page_exists": True,
        "crafting_redirect_exists": find_wiki_page(wikitext_dir, "Crafting").exists(),
        "tradeskills_redirect_exists": find_wiki_page(wikitext_dir, "Tradeskills").exists(),
        "level_10_active_limit": "level 10" in tradeskill_folded and "2 active tradeskills" in tradeskill_folded,
        "gathering_section": "==gathering tradeskills==" in tradeskill_folded,
        "production_section": "==production tradeskills==" in tradeskill_folded,
        "hobbies_section": "==hobbies==" in tradeskill_folded,
        "other_section": "==other==" in tradeskill_folded,
        "runecrafting_level_15": "runecrafting is available to all players at level 15" in tradeskill_folded,
        "salvaging_level_8": "salvaging is available to all players at level 8" in tradeskill_folded,
        "profession_pages_found": tuple(profession_pages_found),
        "profession_pages_missing": tuple(profession_pages_missing),
        "schematic_list_pages": len(schematic_page_titles),
        "schematic_professions": schematic_professions,
    }


def parse_client_tradeskill_rows(client_sql_dir: Path) -> list[dict[str, int | str]]:
    rows: list[dict[str, int | str]] = []

    for values in iter_insert_rows(client_sql_dir / "Tradeskill.tbl.sql", "Tradeskill"):
        tradeskill_id = int(values[0])
        rows.append(
            {
                "id": tradeskill_id,
                "name": TRADESKILL_NAME_BY_ID.get(tradeskill_id, f"Tradeskill{tradeskill_id}"),
                "flags": int(values[3]),
                "tutorial_id": int(values[4]),
                "achievement_category_id": int(values[5]),
                "max_additives": int(values[6]),
            }
        )

    return rows


def parse_client_tradeskill_tier_rows(client_sql_dir: Path) -> list[dict[str, int]]:
    rows: list[dict[str, int]] = []

    for values in iter_insert_rows(client_sql_dir / "TradeskillTier.tbl.sql", "TradeskillTier"):
        rows.append(
            {
                "id": int(values[0]),
                "tradeskill_id": int(values[1]),
                "tier": int(values[2]),
                "required_xp": int(values[3]),
                "learn_xp": int(values[4]),
                "craft_xp": int(values[5]),
                "first_craft_xp": int(values[6]),
                "quest_xp": int(values[7]),
                "fail_xp": int(values[8]),
                "item_level_min": int(values[9]),
                "max_additives": int(values[10]),
                "relearn_cost": int(values[11]),
                "achievement_category_id": int(values[12]),
            }
        )

    return rows


def parse_client_tradeskill_schematic_rows(client_sql_dir: Path) -> list[dict[str, int | float | tuple[int, ...]]]:
    rows: list[dict[str, int | float | tuple[int, ...]]] = []

    for values in iter_insert_rows(client_sql_dir / "TradeskillSchematic2.tbl.sql", "TradeskillSchematic2"):
        material_item_ids = tuple(int(value) for value in values[9:14] if int(value) > 0)
        material_costs = tuple(int(value) for value in values[14:19] if int(value) > 0)
        rows.append(
            {
                "id": int(values[0]),
                "tradeskill_id": int(values[2]),
                "item_output_id": int(values[3]),
                "item_output_fail_id": int(values[4]),
                "output_count": int(values[5]),
                "loot_id": int(values[6]),
                "tier": int(values[7]),
                "flags": int(values[8]),
                "material_item_ids": material_item_ids,
                "material_costs": material_costs,
                "parent_id": int(values[19]),
                "vector_x": float(values[20]),
                "vector_y": float(values[21]),
                "radius": float(values[22]),
                "crit_radius": float(values[23]),
                "item_output_crit_id": int(values[24]),
                "output_count_crit_bonus": int(values[25]),
                "priority": int(values[26]),
                "max_additives": int(values[27]),
                "discoverable_quadrant": int(values[28]),
                "discoverable_radius": float(values[29]),
                "discoverable_angle": float(values[30]),
                "catalyst_ordering_id": int(values[31]),
            }
        )

    return rows


def parse_client_tradeskill_material_rows(client_sql_dir: Path) -> list[dict[str, int]]:
    rows: list[dict[str, int]] = []

    for values in iter_insert_rows(client_sql_dir / "TradeskillMaterial.tbl.sql", "TradeskillMaterial"):
        rows.append(
            {
                "id": int(values[0]),
                "stat_revolution_item_id": int(values[1]),
                "item_id": int(values[2]),
                "display_index": int(values[3]),
                "category_id": int(values[4]),
            }
        )

    return rows


def parse_client_tradeskill_harvesting_rows(client_sql_dir: Path) -> list[dict[str, int]]:
    rows: list[dict[str, int]] = []

    for values in iter_insert_rows(client_sql_dir / "TradeskillHarvestingInfo.tbl.sql", "TradeskillHarvestingInfo"):
        rows.append(
            {
                "id": int(values[0]),
                "tradeskill_tier_id": int(values[1]),
                "prerequisite_id": int(values[2]),
                "minimap_marker_id": int(values[3]),
            }
        )

    return rows


def parse_wiki_lore_facts(archive_dir: Path) -> dict[str, bool | int]:
    wikitext_dir = archive_dir / "pages" / "wikitext"
    lore_text = read_text(find_wiki_page(wikitext_dir, "Lore"))
    datacube_text = read_text(find_wiki_page(wikitext_dir, "Datacube"))
    archive_text = read_text(find_wiki_page(wikitext_dir, "Galactic Archive"))
    codex_text = read_text(find_wiki_page(wikitext_dir, "Codex"))
    lore_folded = lore_text.casefold()
    datacube_folded = datacube_text.casefold()
    archive_folded = archive_text.casefold()
    codex_folded = codex_text.casefold()
    zone_lore_pages = [
        path
        for path in wikitext_dir.glob("*.wiki")
        if wiki_page_title_from_path(path).endswith("Zone Lore entries")
    ]
    datacube_entry_pages = [
        path
        for path in wikitext_dir.glob("*.wiki")
        if wiki_page_title_from_path(path).startswith("DATACUBE ENTRY")
    ]
    datacube_decryption_pages = [
        path
        for path in wikitext_dir.glob("*.wiki")
        if wiki_page_title_from_path(path).startswith("DATACUBE DECRYPTION")
    ]
    archive_topic_rows = len(re.findall(r"style=\"text-align:center\"", archive_text))

    return {
        "lore_page_links_archive_and_zone_lore": "galactic archive" in lore_folded and "zone lore" in lore_folded,
        "codex_page_lists_logs": "achievement log" in codex_folded and "quest log" in codex_folded,
        "datacube_page_documents_gameplay": "world story" in datacube_folded and "scientist" in datacube_folded,
        "archive_page_documents_unlocks": "entries are unlocked" in archive_folded and "articles" in archive_folded,
        "zone_lore_pages": len(zone_lore_pages),
        "datacube_entry_pages": len(datacube_entry_pages),
        "datacube_decryption_pages": len(datacube_decryption_pages),
        "archive_topic_rows": archive_topic_rows,
    }


def parse_client_datacube_rows(client_sql_dir: Path) -> list[dict[str, int | str]]:
    rows: list[dict[str, int | str]] = []

    for values in iter_insert_rows(client_sql_dir / "Datacube.tbl.sql", "Datacube"):
        text_ids = tuple(int(value) for value in values[3:9] if int(value) > 0)
        rows.append(
            {
                "id": int(values[0]),
                "type": int(values[1]),
                "title_text_id": int(values[2]),
                "text_id_count": len(text_ids),
                "sound_event_id": int(values[9]),
                "world_zone_id": int(values[10]),
                "unlock_count": int(values[11]),
                "asset_path_image": values[12],
                "faction": int(values[13]),
                "world_location_id": int(values[14]),
                "quest_direction_id": int(values[15]),
            }
        )

    return rows


def parse_client_datacube_volume_rows(client_sql_dir: Path) -> list[dict[str, int | tuple[int, ...]]]:
    rows: list[dict[str, int | tuple[int, ...]]] = []

    for values in iter_insert_rows(client_sql_dir / "DatacubeVolume.tbl.sql", "DatacubeVolume"):
        rows.append(
            {
                "id": int(values[0]),
                "name_text_id": int(values[1]),
                "datacube_ids": tuple(int(value) for value in values[3:19] if int(value) > 0),
            }
        )

    return rows


def parse_client_archive_article_rows(client_sql_dir: Path) -> list[dict[str, int | str | tuple[int, ...]]]:
    rows: list[dict[str, int | str | tuple[int, ...]]] = []

    for values in iter_insert_rows(client_sql_dir / "ArchiveArticle.tbl.sql", "ArchiveArticle"):
        rows.append(
            {
                "id": int(values[0]),
                "portrait_creature_id": int(values[1]),
                "entry_ids": tuple(int(value) for value in values[10:26] if int(value) > 0),
                "flags": int(values[26]),
                "category_ids": tuple(int(value) for value in values[27:30] if int(value) > 0),
                "tooltip_text_id": int(values[30]),
                "world_zone_id": int(values[31]),
                "character_title_id_reward": int(values[32]),
                "link_name": values[33],
            }
        )

    return rows


def parse_client_archive_entry_rows(client_sql_dir: Path) -> list[dict[str, int | tuple[int, ...] | str]]:
    rows: list[dict[str, int | tuple[int, ...] | str]] = []

    for values in iter_insert_rows(client_sql_dir / "ArchiveEntry.tbl.sql", "ArchiveEntry"):
        rows.append(
            {
                "id": int(values[0]),
                "heading_text_id": int(values[1]),
                "text_ids": tuple(int(value) for value in values[2:8] if int(value) > 0),
                "scientist_text_ids": tuple(int(value) for value in values[8:14] if int(value) > 0),
                "portrait_creature_id": int(values[14]),
                "icon_asset_path": values[15],
                "inline_asset_path": values[16],
                "type": int(values[17]),
                "flags": int(values[18]),
                "header": int(values[19]),
                "character_title_id_reward": int(values[20]),
            }
        )

    return rows


def parse_client_archive_unlock_rule_rows(client_sql_dir: Path) -> list[dict[str, int | tuple[int, ...]]]:
    rows: list[dict[str, int | tuple[int, ...]]] = []

    for values in iter_insert_rows(client_sql_dir / "ArchiveEntryUnlockRule.tbl.sql", "ArchiveEntryUnlockRule"):
        rows.append(
            {
                "id": int(values[0]),
                "archive_entry_id": int(values[1]),
                "type": int(values[2]),
                "flags": int(values[3]),
                "objects": tuple(int(value) for value in values[4:10] if int(value) > 0),
            }
        )

    return rows


def parse_client_story_panel_rows(client_sql_dir: Path) -> list[dict[str, int]]:
    rows: list[dict[str, int]] = []

    for values in iter_insert_rows(client_sql_dir / "StoryPanel.tbl.sql", "StoryPanel"):
        rows.append(
            {
                "id": int(values[0]),
                "body_text_id": int(values[1]),
                "sound_event_id": int(values[2]),
                "window_type_id": int(values[3]),
                "duration_ms": int(values[4]),
                "prerequisite_id": int(values[5]),
                "style": int(values[6]),
            }
        )

    return rows


def parse_client_path_ability_spell_levels(
    path_reward_rows: Iterable[dict[str, int]],
    spell_descriptions: dict[int, str],
) -> tuple[dict[str, set[int]], dict[str, set[int]]]:
    levels_by_ability: dict[str, set[int]] = defaultdict(set)
    spell_ids_by_ability: dict[str, set[int]] = defaultdict(set)

    for row in path_reward_rows:
        if row["spell_id"] <= 0 or not is_grantable_level_reward(row):
            continue

        level_info = path_reward_level_from_object_id(row["object_id"])
        if level_info is None:
            continue

        path_id, level = level_info
        description = spell_descriptions.get(row["spell_id"], "").casefold()
        for ability_name, (path_name, _, markers) in PATH_ABILITY_PAGES.items():
            if PATH_IDS_BY_NAME[path_name] != path_id:
                continue
            if any(marker.casefold() in description for marker in markers):
                levels_by_ability[ability_name].add(level)
                spell_ids_by_ability[ability_name].add(row["spell_id"])
                break

    return levels_by_ability, spell_ids_by_ability


def parse_spell_effect_types(client_sql_dir: Path, spell_ids: set[int]) -> dict[int, set[int]]:
    effect_types_by_spell: dict[int, set[int]] = defaultdict(set)

    for values in iter_insert_rows(client_sql_dir / "Spell4Effects.tbl.sql", "Spell4Effects"):
        spell_id = int(values[1])
        if spell_id in spell_ids:
            effect_types_by_spell[spell_id].add(int(values[3]))

    return effect_types_by_spell


def uint32_to_float(value: int) -> float:
    return struct.unpack("<f", struct.pack("<I", value & 0xFFFFFFFF))[0]


def parse_spell_effect_rows_for_types(client_sql_dir: Path, effect_types: set[int]) -> list[dict[str, int | float]]:
    rows: list[dict[str, int | float]] = []

    for values in iter_insert_rows(client_sql_dir / "Spell4Effects.tbl.sql", "Spell4Effects"):
        effect_type = int(values[3])
        if effect_type not in effect_types:
            continue

        rows.append(
            {
                "id": int(values[0]),
                "spell_id": int(values[1]),
                "target_flags": int(values[2]),
                "effect_type": effect_type,
                "delay_time": int(values[5]),
                "tick_time": int(values[6]),
                "duration_time": int(values[7]),
                "flags": int(values[8]),
                "data_bits_00": int(values[9]),
                "data_float_00": uint32_to_float(int(values[9])),
                "parameter_value_00": float(values[36]),
            }
        )

    return rows


def parse_proxy_spell_ids(client_sql_dir: Path, spell_ids: set[int]) -> set[int]:
    proxy_spell_ids: set[int] = set()

    for values in iter_insert_rows(client_sql_dir / "Spell4Effects.tbl.sql", "Spell4Effects"):
        spell_id = int(values[1])
        effect_type = int(values[3])
        if spell_id not in spell_ids or effect_type not in PROXY_EFFECT_TYPES:
            continue

        proxied_spell_id = int(values[9])
        if proxied_spell_id > 0:
            proxy_spell_ids.add(proxied_spell_id)

    return proxy_spell_ids


def parse_spell_effect_type_names(spell_effect_type_path: Path) -> dict[int, str]:
    text = read_text(spell_effect_type_path)
    names: dict[int, str] = {}

    for match in re.finditer(r"^\s*([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(0x[0-9A-Fa-f]+|\d+)", text, re.MULTILINE):
        names[int(match.group(2), 0)] = match.group(1)

    return names


def parse_server_spell_effect_handlers(spell_effect_handler_path: Path) -> set[str]:
    text = read_text(spell_effect_handler_path)
    return set(re.findall(r"SpellEffectHandler\(SpellEffectType\.([A-Za-z_][A-Za-z0-9_]*)\)", text))


def format_set(values: Iterable[str]) -> str:
    return ", ".join(sorted(values)) or "none"


def format_numbers(values: Iterable[int]) -> str:
    return ", ".join(str(value) for value in sorted(values)) or "none"


def validate_input_paths(archive_dir: Path, client_sql_dir: Path, root: Path) -> None:
    required_paths = [
        (archive_dir, "wiki archive directory"),
        (archive_dir / "pages" / "wikitext", "wiki archive wikitext directory"),
        (client_sql_dir, "client SQL directory"),
        (client_sql_dir / "Class.tbl.sql", "client Class.tbl.sql"),
        (client_sql_dir / "CharacterCreation.tbl.sql", "client CharacterCreation.tbl.sql"),
        (client_sql_dir / "EldanAugmentation.tbl.sql", "client EldanAugmentation.tbl.sql"),
        (client_sql_dir / "EldanAugmentationCategory.tbl.sql", "client EldanAugmentationCategory.tbl.sql"),
        (client_sql_dir / "PathReward.tbl.sql", "client PathReward.tbl.sql"),
        (client_sql_dir / "Spell4.tbl.sql", "client Spell4.tbl.sql"),
        (client_sql_dir / "Spell4Effects.tbl.sql", "client Spell4Effects.tbl.sql"),
        (client_sql_dir / "SpellLevel.tbl.sql", "client SpellLevel.tbl.sql"),
        (root / "Source", "NexusForever Source directory"),
    ]
    required_paths.extend(
        (client_sql_dir / f"{table_name}.tbl.sql", f"client {table_name}.tbl.sql")
        for table_name in HOUSING_CORE_TABLES
    )
    required_paths.extend(
        (client_sql_dir / f"{table_name}.tbl.sql", f"client {table_name}.tbl.sql")
        for table_name in QUEST_CORE_TABLES
    )
    required_paths.extend(
        (client_sql_dir / f"{table_name}.tbl.sql", f"client {table_name}.tbl.sql")
        for table_name in ACHIEVEMENT_CORE_TABLES
    )
    required_paths.extend(
        (client_sql_dir / f"{table_name}.tbl.sql", f"client {table_name}.tbl.sql")
        for table_name in TRADESKILL_CORE_TABLES
    )
    required_paths.extend(
        (client_sql_dir / f"{table_name}.tbl.sql", f"client {table_name}.tbl.sql")
        for table_name in LORE_CORE_TABLES
    )

    missing = [f"{label}: {path}" for path, label in required_paths if not path.exists()]
    if missing:
        raise FileNotFoundError("Missing required audit inputs:\n- " + "\n- ".join(missing))


def format_domain_listing() -> str:
    lines: list[str] = []

    lines.append("Implemented audit domains:")
    for domain, description in IMPLEMENTED_AUDIT_DOMAINS.items():
        lines.append(f"- {domain}: {description}")

    lines.append("")
    lines.append("Planned audit domains:")
    if PLANNED_AUDIT_DOMAINS:
        for domain, description in PLANNED_AUDIT_DOMAINS.items():
            lines.append(f"- {domain}: {description}")
    else:
        lines.append("- none")

    return "\n".join(lines) + "\n"


def build_character_creation_report(archive_dir: Path, client_sql_dir: Path, root: Path) -> tuple[list[str], list[str], list[str]]:
    failures: list[str] = []
    warnings: list[str] = []
    lines: list[str] = []

    wiki_classes = parse_wiki_class_races(archive_dir)
    client_classes = parse_client_class_races(client_sql_dir)

    lines.append("## Character Class/Race Availability")
    lines.append("")
    lines.append("| Class | Wiki races | Client enabled races | Result |")
    lines.append("| --- | --- | --- | --- |")

    for class_name in CLASS_NAMES:
        wiki_races = wiki_classes[class_name]
        client_races = client_classes[class_name]
        result = "PASS" if wiki_races == client_races else "FAIL"
        if result == "FAIL":
            failures.append(f"{class_name} class/race availability differs")
        lines.append(f"| {class_name} | {format_set(wiki_races)} | {format_set(client_races)} | {result} |")

    lines.append("")

    wiki_roles = parse_wiki_class_roles(archive_dir)
    code_roles = parse_code_default_roles(root / "Source" / "NexusForever.Game" / "Matching" / "MatchingDataManager.cs")

    lines.append("## Class Roles")
    lines.append("")
    lines.append("| Class | Wiki roles | Server default roles | Result |")
    lines.append("| --- | --- | --- | --- |")

    for class_name in CLASS_NAMES:
        wiki_class_roles = wiki_roles[class_name]
        code_class_roles = code_roles.get(class_name, set())
        result = "PASS" if wiki_class_roles == code_class_roles else "FAIL"
        if result == "FAIL":
            failures.append(f"{class_name} default roles differ from wiki roles")
        lines.append(f"| {class_name} | {format_set(wiki_class_roles)} | {format_set(code_class_roles)} | {result} |")

    lines.append("")

    wiki_proficiencies = parse_wiki_class_proficiencies(archive_dir)
    client_proficiencies = parse_client_class_proficiencies(client_sql_dir, root)

    lines.append("## Class Item Proficiencies")
    lines.append("")
    lines.append("| Class | Wiki-derived proficiencies | Client proficiencies | Result |")
    lines.append("| --- | --- | --- | --- |")

    for class_name in CLASS_NAMES:
        wiki_class_proficiencies = wiki_proficiencies[class_name]
        client_class_proficiencies = client_proficiencies[class_name]
        result = "PASS" if wiki_class_proficiencies == client_class_proficiencies else "INFO"
        if result == "INFO":
            warnings.append(f"{class_name} wiki proficiency wording differs from client Class.tbl.sql")
        lines.append(
            f"| {class_name} | {format_set(wiki_class_proficiencies)} | {format_set(client_class_proficiencies)} | {result} |"
        )

    lines.append("")

    expected_starts = expected_supported_start_keys(client_sql_dir)
    seeded_starts = parse_seeded_character_starts(root / "Source" / "NexusForever.Database.Character" / "CharacterContext.cs")
    missing_starts = expected_starts - seeded_starts
    extra_starts = seeded_starts - expected_starts

    lines.append("## Character Start Coverage")
    lines.append("")
    lines.append(f"- Supported client start keys: {len(expected_starts)}")
    lines.append(f"- Seeded server start keys: {len(seeded_starts)}")
    lines.append(f"- Missing seeded keys: {len(missing_starts)}")
    lines.append(f"- Extra seeded keys: {len(extra_starts)}")

    if missing_starts:
        failures.append("supported CharacterCreation starts missing seeded locations")
        for race, faction, start in sorted(missing_starts):
            lines.append(f"  - Missing race={race} faction={faction} start={start} ({START_NAMES.get(start, 'Unknown')})")

    if extra_starts:
        for race, faction, start in sorted(extra_starts):
            lines.append(f"  - Extra race={race} faction={faction} start={start} ({START_NAMES.get(start, 'Unknown')})")

    if not missing_starts:
        lines.append("- Result: PASS")

    lines.append("")

    unsupported_counts: Counter[int] = Counter()
    for row in parse_character_creation_rows(client_sql_dir):
        if row["enabled"] and int(row["start"]) not in SUPPORTED_STARTS:
            unsupported_counts[int(row["start"])] += 1

    lines.append("## Unsupported Enabled Client Starts")
    lines.append("")
    if unsupported_counts:
        for start, count in sorted(unsupported_counts.items()):
            lines.append(f"- {START_NAMES.get(start, 'Unknown')} ({start}): {count} enabled rows")
        lines.append("- Result: INFO, these rows are rejected by server validation until matching start locations are implemented.")
    else:
        lines.append("- Result: PASS, no unsupported enabled client starts found.")

    lines.append("")

    wiki_paths = parse_wiki_paths(archive_dir)
    code_paths = parse_code_paths(root / "Source" / "NexusForever.Game.Static" / "PlayerPath" / "Path.cs")
    lines.append("## Player Paths")
    lines.append("")
    lines.append(f"- Wiki paths: {format_set(wiki_paths)}")
    lines.append(f"- Server enum paths: {format_set(code_paths)}")
    if wiki_paths == code_paths:
        lines.append("- Result: PASS")
    else:
        failures.append("wiki path names differ from server enum")
        lines.append("- Result: FAIL")

    lines.append("")

    path_levels = parse_client_path_levels(client_sql_dir)
    expected_level_numbers = set(range(1, 31))
    soldier_curve = path_levels.get(0, {})

    lines.append("## Player Path Level Curve")
    lines.append("")
    lines.append("| Path | Client levels | XP curve matches Soldier | Result |")
    lines.append("| --- | --- | --- | --- |")

    for path_id, path_name in PATH_NAMES_BY_ID.items():
        levels = path_levels.get(path_id, {})
        level_numbers = set(levels)
        missing_levels = expected_level_numbers - level_numbers
        extra_levels = level_numbers - expected_level_numbers
        curve_matches = levels == soldier_curve
        result = "PASS" if not missing_levels and not extra_levels and curve_matches else "FAIL"
        if result == "FAIL":
            failures.append(f"{path_name} path level curve differs from expected client levels")
        level_summary = f"{min(level_numbers)}-{max(level_numbers)}" if level_numbers else "none"
        lines.append(f"| {path_name} | {level_summary} | {curve_matches} | {result} |")
        if missing_levels:
            lines.append(f"  - {path_name} missing client levels: {format_numbers(missing_levels)}")
        if extra_levels:
            lines.append(f"  - {path_name} extra client levels: {format_numbers(extra_levels)}")

    lines.append("")

    path_reward_rows = parse_client_path_rewards(client_sql_dir)
    rewards_by_object_id: dict[int, list[dict[str, int]]] = defaultdict(list)
    for row in path_reward_rows:
        rewards_by_object_id[row["object_id"]].append(row)

    client_level_reward_kinds: set[str] = set()
    client_blank_levels_by_path: dict[str, set[int]] = {}

    lines.append("## Player Path Reward Coverage")
    lines.append("")
    lines.append("| Path | Empty client levels | Covered client levels | Client reward kinds | Result |")
    lines.append("| --- | --- | --- | --- | --- |")

    for path_id, path_name in PATH_NAMES_BY_ID.items():
        missing_reward_levels: set[int] = set()
        blank_reward_levels: set[int] = set()
        covered_reward_levels: set[int] = set()
        path_reward_kinds_found: set[str] = set()

        for level in range(1, 31):
            object_id = path_reward_object_id_for_level(path_id, level)
            grantable_rows = [
                row
                for row in rewards_by_object_id.get(object_id, [])
                if is_grantable_level_reward(row)
            ]
            level_reward_kinds = set().union(*(path_reward_kinds(row) for row in grantable_rows)) if grantable_rows else set()
            if level_reward_kinds:
                covered_reward_levels.add(level)
                path_reward_kinds_found.update(level_reward_kinds)
                client_level_reward_kinds.update(level_reward_kinds)
            else:
                blank_reward_levels.add(level)
                if level != 1:
                    missing_reward_levels.add(level)

        client_blank_levels_by_path[path_name] = blank_reward_levels
        result = "PASS" if not missing_reward_levels else "FAIL"
        if result == "FAIL":
            failures.append(f"{path_name} path has client levels without grantable rewards")
        lines.append(
            f"| {path_name} | {format_numbers(blank_reward_levels)} | {format_numbers(covered_reward_levels)} | "
            f"{format_set(path_reward_kinds_found)} | {result} |"
        )

    lines.append("")

    server_reward_kinds = parse_server_path_reward_kinds(root / "Source" / "NexusForever.Game" / "Entity" / "PathManager.cs")
    missing_server_reward_kinds = client_level_reward_kinds - server_reward_kinds

    lines.append("## Server Path Reward Grant Handlers")
    lines.append("")
    lines.append(f"- Client level reward kinds: {format_set(client_level_reward_kinds)}")
    lines.append(f"- Server grant handlers: {format_set(server_reward_kinds)}")
    if missing_server_reward_kinds:
        failures.append("server path rewards do not handle all client level reward kinds")
        lines.append(f"- Missing server handlers: {format_set(missing_server_reward_kinds)}")
        lines.append("- Result: FAIL")
    else:
        lines.append("- Result: PASS")

    lines.append("")

    lines.append("## Wiki Path Reward Table Coverage")
    lines.append("")
    lines.append("| Path | Wiki ranks | Blank wiki ranks | Empty client levels | Result |")
    lines.append("| --- | --- | --- | --- | --- |")

    for path_name in PATH_NAMES_BY_ID.values():
        wiki_reward_ranks = parse_wiki_path_reward_ranks(archive_dir, path_name)
        wiki_rank_numbers = set(wiki_reward_ranks)
        missing_wiki_ranks = expected_level_numbers - wiki_rank_numbers
        blank_wiki_ranks = {
            rank
            for rank, reward in wiki_reward_ranks.items()
            if not reward.strip()
        }
        client_blank_levels = client_blank_levels_by_path[path_name]

        if missing_wiki_ranks:
            failures.append(f"{path_name} wiki path reward table is missing ranks")
            result = "FAIL"
        elif blank_wiki_ranks == client_blank_levels:
            result = "PASS"
        else:
            warnings.append(
                f"{path_name} wiki blank reward ranks differ from client PathReward coverage "
                f"(wiki {format_numbers(blank_wiki_ranks)}, client {format_numbers(client_blank_levels)})"
            )
            result = "INFO"

        rank_summary = f"{min(wiki_rank_numbers)}-{max(wiki_rank_numbers)}" if wiki_rank_numbers else "none"
        lines.append(
            f"| {path_name} | {rank_summary} | {format_numbers(blank_wiki_ranks)} | "
            f"{format_numbers(client_blank_levels)} | {result} |"
        )
        if missing_wiki_ranks:
            lines.append(f"  - {path_name} missing wiki ranks: {format_numbers(missing_wiki_ranks)}")

    lines.append("")

    client_spell_descriptions = parse_client_spells(client_sql_dir)
    client_path_ability_levels, client_path_ability_spell_ids = parse_client_path_ability_spell_levels(
        path_reward_rows,
        client_spell_descriptions,
    )
    path_ability_spell_ids: set[int] = set()

    lines.append("## Path Ability Unlock Levels")
    lines.append("")
    lines.append("| Ability | Path | Wiki levels | Client spell reward levels | Result |")
    lines.append("| --- | --- | --- | --- | --- |")

    for ability_name, (path_name, page_title, _) in PATH_ABILITY_PAGES.items():
        wiki_levels = parse_wiki_path_ability_levels(archive_dir, page_title)
        client_levels = client_path_ability_levels.get(ability_name, set())
        path_ability_spell_ids.update(client_path_ability_spell_ids.get(ability_name, set()))

        if not client_levels:
            failures.append(f"{ability_name} path ability has no matched client spell rewards")
            result = "FAIL"
        elif wiki_levels == client_levels:
            result = "PASS"
        else:
            warnings.append(
                f"{ability_name} wiki unlock levels differ from client PathReward spell levels "
                f"(wiki {format_numbers(wiki_levels)}, client {format_numbers(client_levels)})"
            )
            result = "INFO"

        lines.append(
            f"| {ability_name} | {path_name} | {format_numbers(wiki_levels)} | "
            f"{format_numbers(client_levels)} | {result} |"
        )

    lines.append("")

    spell_effect_names = parse_spell_effect_type_names(root / "Source" / "NexusForever.Game.Static" / "Spell" / "SpellEffectType.cs")
    server_spell_effect_handlers = parse_server_spell_effect_handlers(root / "Source" / "NexusForever.Game" / "Spell" / "SpellEffectHandler.cs")
    spell_effect_types_by_spell = parse_spell_effect_types(client_sql_dir, path_ability_spell_ids)
    effect_spell_ids: dict[int, set[int]] = defaultdict(set)
    for spell_id, effect_types in spell_effect_types_by_spell.items():
        for effect_type in effect_types:
            effect_spell_ids[effect_type].add(spell_id)

    lines.append("## Path Ability Spell Handler Coverage")
    lines.append("")
    lines.append("| Direct effect type | Path ability spell ids | Server handler | Result |")
    lines.append("| --- | --- | --- | --- |")

    missing_effect_handlers: set[str] = set()
    for effect_type in sorted(effect_spell_ids):
        effect_name = spell_effect_names.get(effect_type, f"EffectType{effect_type}")
        handler_present = effect_name in server_spell_effect_handlers
        if not handler_present:
            missing_effect_handlers.add(effect_name)
        lines.append(
            f"| {effect_name} ({effect_type}) | {format_numbers(effect_spell_ids[effect_type])} | "
            f"{'yes' if handler_present else 'no'} | {'PASS' if handler_present else 'INFO'} |"
        )

    if missing_effect_handlers:
        warnings.append(
            "path ability direct spell effects lack mapped server handlers: "
            f"{format_set(missing_effect_handlers)}"
        )
        lines.append("")
        lines.append("- Result: INFO, missing handlers require deeper spell behavior evidence before runtime implementation.")
    else:
        lines.append("")
        lines.append("- Result: PASS")

    lines.append("")

    proxied_spell_ids = parse_proxy_spell_ids(client_sql_dir, path_ability_spell_ids)
    proxied_spell_effect_types_by_spell = parse_spell_effect_types(client_sql_dir, proxied_spell_ids)
    proxied_effect_spell_ids: dict[int, set[int]] = defaultdict(set)
    for spell_id, effect_types in proxied_spell_effect_types_by_spell.items():
        for effect_type in effect_types:
            proxied_effect_spell_ids[effect_type].add(spell_id)

    lines.append("## Path Ability Proxied Spell Handler Coverage")
    lines.append("")
    lines.append(f"- One-hop proxied spell ids: {format_numbers(proxied_spell_ids)}")
    lines.append("")
    lines.append("| Proxied effect type | Proxied spell ids | Server handler | Result |")
    lines.append("| --- | --- | --- | --- |")

    missing_proxied_effect_handlers: set[str] = set()
    for effect_type in sorted(proxied_effect_spell_ids):
        effect_name = spell_effect_names.get(effect_type, f"EffectType{effect_type}")
        handler_present = effect_name in server_spell_effect_handlers
        if not handler_present:
            missing_proxied_effect_handlers.add(effect_name)
        lines.append(
            f"| {effect_name} ({effect_type}) | {format_numbers(proxied_effect_spell_ids[effect_type])} | "
            f"{'yes' if handler_present else 'no'} | {'PASS' if handler_present else 'INFO'} |"
        )

    if missing_proxied_effect_handlers:
        warnings.append(
            "path ability proxied spell effects lack mapped server handlers: "
            f"{format_set(missing_proxied_effect_handlers)}"
        )
        lines.append("")
        lines.append("- Result: INFO, proxied effects require their own evidence before runtime implementation.")
    else:
        lines.append("")
        lines.append("- Result: PASS")

    lines.append("")

    rested_effect_rows = parse_spell_effect_rows_for_types(client_sql_dir, {0x006D, 0x0086})
    rested_rows_by_effect_type: dict[int, list[dict[str, int | float]]] = defaultdict(list)
    for row in rested_effect_rows:
        rested_rows_by_effect_type[int(row["effect_type"])].append(row)

    lines.append("## Rested XP Spell Effect Coverage")
    lines.append("")
    lines.append("| Effect type | Client spell ids | DataBits00 float values | ParameterValue00 values | Server handler | Result |")
    lines.append("| --- | --- | --- | --- | --- | --- |")

    for effect_type in (0x0086, 0x006D):
        effect_rows = rested_rows_by_effect_type.get(effect_type, [])
        effect_name = spell_effect_names.get(effect_type, f"EffectType{effect_type}")
        handler_present = effect_name in server_spell_effect_handlers
        data_float_values = {
            f"{float(row['data_float_00']):.6g}"
            for row in effect_rows
        }
        parameter_values = {
            f"{float(row['parameter_value_00']):.6g}"
            for row in effect_rows
        }
        result = "PASS" if handler_present else "INFO"
        if not handler_present:
            warnings.append(f"{effect_name} remains mapped-only; runtime stacking and persistence need stronger evidence")
        lines.append(
            f"| {effect_name} ({effect_type}) | {format_numbers(int(row['spell_id']) for row in effect_rows)} | "
            f"{format_set(data_float_values)} | {format_set(parameter_values)} | "
            f"{'yes' if handler_present else 'no'} | {result} |"
        )

    lines.append("")
    lines.append("- Result: INFO, direct `ModifyRestedXP` is handled; decor/aura `RestedXpDecorBonus` remains blocked.")
    lines.append("")

    return lines, failures, warnings


def build_abilities_report(archive_dir: Path, client_sql_dir: Path, root: Path) -> tuple[list[str], list[str], list[str]]:
    failures: list[str] = []
    warnings: list[str] = []
    lines: list[str] = []

    wiki_abilities = parse_wiki_ability_pages(archive_dir)
    wiki_counts_by_class: dict[str, Counter[str]] = defaultdict(Counter)
    for ability in wiki_abilities:
        class_name = str(ability["class"])
        ability_type = str(ability["type"])
        if class_name in CLASS_NAMES:
            wiki_counts_by_class[class_name][ability_type] += 1

    lines.append("## Ability Wiki Page Coverage")
    lines.append("")
    lines.append(f"- Ability infobox pages: {len(wiki_abilities)}")
    lines.append(f"- Class combat ability types audited: {format_set(CLASS_ABILITY_TYPES)}")
    lines.append("")
    lines.append("| Class | Assault pages | Support pages | Utility pages | Innate pages | Result |")
    lines.append("| --- | --- | --- | --- | --- | --- |")

    for class_name in CLASS_NAMES:
        counts = wiki_counts_by_class[class_name]
        has_combat_pages = all(counts[ability_type] > 0 for ability_type in CLASS_ABILITY_TYPES)
        if not has_combat_pages:
            failures.append(f"{class_name} wiki ability pages are missing one or more combat ability types")
        lines.append(
            f"| {class_name} | {counts['Assault']} | {counts['Support']} | {counts['Utility']} | "
            f"{counts['Innate']} | {'PASS' if has_combat_pages else 'FAIL'} |"
        )

    lines.append("")

    client_unlocks = parse_client_class_spell_unlocks(client_sql_dir)
    client_unlock_counts = Counter(str(unlock["class"]) for unlock in client_unlocks)

    lines.append("## Client SpellLevel Unlock Rows")
    lines.append("")
    lines.append("| Class | Client SpellLevel rows | Result |")
    lines.append("| --- | --- | --- |")

    for class_name in CLASS_NAMES:
        count = client_unlock_counts[class_name]
        if count == 0:
            failures.append(f"{class_name} has no client SpellLevel unlock rows")
        lines.append(f"| {class_name} | {count} | {'PASS' if count > 0 else 'FAIL'} |")

    lines.append("")

    current_wiki_combat_abilities = [
        ability
        for ability in wiki_abilities
        if str(ability["class"]) in CLASS_NAMES
        and str(ability["type"]) in CLASS_ABILITY_TYPES
        and bool(ability["has_unlock_level"])
        and not bool(ability["removed"])
    ]

    match_counts_by_class: dict[str, Counter[str]] = defaultdict(Counter)
    exception_rows: list[dict[str, int | str]] = []

    for ability in sorted(
        current_wiki_combat_abilities,
        key=lambda row: (str(row["class"]), str(row["type"]), str(row["title"])),
    ):
        title = str(ability["title"])
        class_name = str(ability["class"])
        ability_type = str(ability["type"])
        wiki_unlock_level = int(ability["unlock_level"])
        matching_unlocks = [
            unlock
            for unlock in client_unlocks
            if unlock["class"] == class_name and ability_name_matches_description(title, str(unlock["description"]))
        ]
        client_levels = {int(unlock["level"]) for unlock in matching_unlocks}
        client_spell_ids = {int(unlock["spell_id"]) for unlock in matching_unlocks}

        if not matching_unlocks:
            match_counts_by_class[class_name]["no_client_match"] += 1
            exception_rows.append(
                {
                    "title": title,
                    "class": class_name,
                    "type": ability_type,
                    "wiki_level": wiki_unlock_level,
                    "client_levels": "none",
                    "client_spell_ids": "none",
                    "result": "INFO",
                }
            )
            continue

        if wiki_unlock_level in client_levels:
            match_counts_by_class[class_name]["exact"] += 1
        else:
            match_counts_by_class[class_name]["client_differs"] += 1
            exception_rows.append(
                {
                    "title": title,
                    "class": class_name,
                    "type": ability_type,
                    "wiki_level": wiki_unlock_level,
                    "client_levels": format_numbers(client_levels),
                    "client_spell_ids": format_numbers(client_spell_ids),
                    "result": "INFO",
                }
            )

    missing_client_matches = [
        str(row["title"])
        for row in exception_rows
        if row["client_levels"] == "none"
    ]
    client_level_differences = [
        row
        for row in exception_rows
        if row["client_levels"] != "none"
    ]

    if missing_client_matches:
        warnings.append(
            f"{len(missing_client_matches)} wiki combat ability page(s) have no current client SpellLevel match: "
            f"{format_set(missing_client_matches)}"
        )
    if client_level_differences:
        warnings.append(
            f"{len(client_level_differences)} wiki combat ability unlock level(s) differ from client SpellLevel data; "
            "client SpellLevel rows remain the implementation authority."
        )

    lines.append("## Wiki Ability Unlocks Compared To Client SpellLevel")
    lines.append("")
    lines.append(f"- Current wiki combat ability pages with unlock levels: {len(current_wiki_combat_abilities)}")
    lines.append(f"- Exact wiki/client unlock-level matches: {sum(counts['exact'] for counts in match_counts_by_class.values())}")
    lines.append(f"- Wiki pages matched to different client unlock levels: {len(client_level_differences)}")
    lines.append(f"- Wiki pages without current client SpellLevel match: {len(missing_client_matches)}")
    lines.append("")
    lines.append("| Class | Exact matches | Client-level differences | No current client match | Result |")
    lines.append("| --- | --- | --- | --- | --- |")

    for class_name in CLASS_NAMES:
        counts = match_counts_by_class[class_name]
        lines.append(
            f"| {class_name} | {counts['exact']} | {counts['client_differs']} | {counts['no_client_match']} | INFO |"
        )

    lines.append("")
    lines.append("### Unlock Exceptions")
    lines.append("")
    if exception_rows:
        lines.append("| Ability | Class | Type | Wiki unlock | Client levels | Client spell ids | Result |")
        lines.append("| --- | --- | --- | --- | --- | --- | --- |")
        for row in exception_rows:
            lines.append(
                f"| {row['title']} | {row['class']} | {row['type']} | {row['wiki_level']} | "
                f"{row['client_levels']} | {row['client_spell_ids']} | {row['result']} |"
            )
    else:
        lines.append("- Result: PASS, every audited wiki combat ability matched the client unlock level.")

    lines.append("")

    spell_manager_text = read_text(root / "Source" / "NexusForever.Game" / "Entity" / "SpellManager.cs")
    server_markers = {
        "Uses client SpellLevel rows": "GameTableManager.Instance.SpellLevel.Entries" in spell_manager_text,
        "Filters by player class and level": "s.ClassId == (byte)player.Class" in spell_manager_text
        and "s.CharacterLevel <= player.Level" in spell_manager_text,
        "Respects SpellLevel prerequisites": "PrerequisiteManager.Instance.Meets(player, spellLevel.PrerequisiteId)" in spell_manager_text,
        "Grants Spell4 base ids": "AddSpell(spell4Entry.Spell4BaseIdBaseSpell)" in spell_manager_text,
        "Grants class innate and primary attacks": all(
            marker in spell_manager_text
            for marker in (
                "Spell4IdInnateAbilityActive",
                "Spell4IdInnateAbilityPassive",
                "Spell4IdAttackPrimary",
                "Spell4IdAttackUnarmed",
            )
        ),
        "Creates ability-book spell items": ".SpellCreate(" in spell_manager_text,
    }

    lines.append("## Server Ability Grant Surface")
    lines.append("")
    lines.append("| Implementation surface | Present | Result |")
    lines.append("| --- | --- | --- |")

    for marker, present in server_markers.items():
        if not present:
            failures.append(f"server ability grant surface missing marker: {marker}")
        lines.append(f"| {marker} | {'yes' if present else 'no'} | {'PASS' if present else 'FAIL'} |")

    lines.append("")

    return lines, failures, warnings


def build_amps_report(archive_dir: Path, client_sql_dir: Path, root: Path) -> tuple[list[str], list[str], list[str]]:
    failures: list[str] = []
    warnings: list[str] = []
    lines: list[str] = []

    wiki_amp_pages = parse_wiki_amp_pages(archive_dir)
    client_amps = parse_client_eldan_augmentations(client_sql_dir)
    client_amp_categories = parse_client_eldan_augmentation_categories(client_sql_dir)

    wiki_branch_counts = Counter(str(page["branch"]) for page in wiki_amp_pages)
    wiki_class_counts = Counter(
        class_name
        for page in wiki_amp_pages
        for class_name in page["classes"]  # type: ignore[union-attr]
    )
    client_class_counts = Counter(str(row["class"]) for row in client_amps)
    client_ids = {int(row["id"]) for row in client_amps}
    client_ids_over_byte = {amp_id for amp_id in client_ids if amp_id > 0xFF}
    client_ids_over_ushort = {amp_id for amp_id in client_ids if amp_id > 0xFFFF}
    required_amp_rows = [row for row in client_amps if int(row["required_id"]) > 0]
    inlaid_unlock_rows = [row for row in client_amps if int(row["item_unlock_id"]) > 0]
    tier_locked_rows = [row for row in client_amps if int(row["category_tier"]) > 0]

    lines.append("## AMP Wiki Page Coverage")
    lines.append("")
    lines.append(f"- AMP infobox pages: {len(wiki_amp_pages)}")
    lines.append(f"- Removed AMP pages: {sum(1 for page in wiki_amp_pages if bool(page['removed']))}")
    lines.append("")
    lines.append("| Branch | Wiki AMP pages | Result |")
    lines.append("| --- | --- | --- |")

    for branch in AMP_BRANCH_NAMES:
        count = wiki_branch_counts[branch]
        if count == 0:
            failures.append(f"wiki AMP pages missing branch {branch}")
        lines.append(f"| {branch} | {count} | {'PASS' if count else 'FAIL'} |")

    unknown_branch_count = sum(
        count
        for branch, count in wiki_branch_counts.items()
        if branch not in AMP_BRANCH_NAMES
    )
    if unknown_branch_count:
        warnings.append(f"{unknown_branch_count} wiki AMP page(s) have no audited branch value")
        lines.append(f"| Unclassified | {unknown_branch_count} | INFO |")

    lines.append("")
    lines.append("| Class | Wiki class AMP category refs | Client class rows | Result |")
    lines.append("| --- | --- | --- | --- |")

    for class_name in CLASS_NAMES:
        wiki_count = wiki_class_counts[class_name]
        client_count = client_class_counts[class_name]
        if client_count == 0:
            failures.append(f"{class_name} has no client EldanAugmentation rows")
        lines.append(f"| {class_name} | {wiki_count} | {client_count} | {'PASS' if client_count else 'FAIL'} |")

    lines.append(f"| Global | n/a | {client_class_counts['Global']} | PASS |")
    lines.append("")

    lines.append("## Client EldanAugmentation Data")
    lines.append("")
    lines.append(f"- Client AMP rows: {len(client_amps)}")
    lines.append(f"- Client AMP category rows: {len(client_amp_categories)}")
    lines.append(f"- Client AMP id range: {min(client_ids)}-{max(client_ids)}")
    lines.append(f"- Client AMP ids above byte range: {len(client_ids_over_byte)}")
    lines.append(f"- Client AMP ids above ushort range: {len(client_ids_over_ushort)}")
    lines.append(f"- Rows with required predecessor AMP: {len(required_amp_rows)}")
    lines.append(f"- Rows with inlaid item unlock id: {len(inlaid_unlock_rows)}")
    lines.append(f"- Rows with category tier lock: {len(tier_locked_rows)}")

    if client_ids_over_ushort:
        failures.append("client AMP ids exceed ushort network/database storage")
        lines.append("- Result: FAIL")
    else:
        lines.append("- Result: PASS")

    lines.append("")

    action_set_amp_text = read_text(root / "Source" / "NexusForever.Game" / "Spell" / "ActionSetAmp.cs")
    action_set_text = read_text(root / "Source" / "NexusForever.Game" / "Spell" / "ActionSet.cs")
    action_set_amp_model_text = read_text(root / "Source" / "NexusForever.Database.Character" / "Model" / "CharacterActionSetAmpModel.cs")
    character_context_text = read_text(root / "Source" / "NexusForever.Database.Character" / "CharacterContext.cs")
    client_commit_amp_spec_text = read_text(root / "Source" / "NexusForever.Network.World" / "Message" / "Model" / "Abilities" / "ClientCommitAmpSpec.cs")
    server_amp_list_text = read_text(root / "Source" / "NexusForever.Network.World" / "Message" / "Model" / "Abilities" / "ServerAMPList.cs")

    persistence_markers = {
        "Database model stores AmpId as ushort": "public ushort AmpId" in action_set_amp_model_text,
        "Database column is unsigned smallint": 'HasColumnType("smallint(5) unsigned")' in character_context_text,
        "Client commit packet reads ushort AMP ids": "List<ushort> Amps" in client_commit_amp_spec_text
        and "ReadUShort()" in client_commit_amp_spec_text,
        "Server AMP list writes ushort AMP ids": "List<ushort> Amps" in server_amp_list_text
        and "foreach (ushort amp" in server_amp_list_text,
        "ActionSetAmp save preserves ushort AMP ids": "checked((ushort)Entry.Id)" in action_set_amp_text
        and "AmpId     = (byte)" not in action_set_amp_text,
        "ActionSet server list preserves AMP ids": "ServerAmpList" in action_set_text
        and "Amps.Add((ushort)amp.Entry.Id)" in action_set_text,
    }

    lines.append("## Server AMP Id Persistence Surface")
    lines.append("")
    lines.append("| Implementation surface | Present | Result |")
    lines.append("| --- | --- | --- |")

    for marker, present in persistence_markers.items():
        if not present:
            failures.append(f"server AMP id persistence surface missing marker: {marker}")
        lines.append(f"| {marker} | {'yes' if present else 'no'} | {'PASS' if present else 'FAIL'} |")

    lines.append("")

    commit_handler_text = read_text(root / "Source" / "NexusForever.WorldServer" / "Network" / "Message" / "Handler" / "Spell" / "ClientCommitAmpSpecHandler.cs")
    action_set_changes_handler_text = read_text(root / "Source" / "NexusForever.WorldServer" / "Network" / "Message" / "Handler" / "Spell" / "ClientRequestActionSetChangesHandler.cs")

    validation_markers = {
        "Commit handler validates AMP ids": "EldanAugmentation.GetEntry(ampId)" in commit_handler_text,
        "Commit handler validates AMP power": "entry.PowerCost" in commit_handler_text
        and "EldanAugmentationNotEnoughPower" in commit_handler_text,
        "Commit handler validates AMP class": "entry.ClassId" in commit_handler_text
        and "UnknownClassId" in commit_handler_text,
        "Commit handler validates AMP series": "EldanAugmentationIdRequired" in commit_handler_text
        and "EldanAugmentationInvalidSeries" in commit_handler_text,
        "Action-set change handler validates AMP ids": "EldanAugmentation.GetEntry(ampId)" in action_set_changes_handler_text,
        "Action-set change handler validates AMP power": "entry.PowerCost" in action_set_changes_handler_text
        and "EldanAugmentationNotEnoughPower" in action_set_changes_handler_text,
        "Action-set change handler validates AMP class": "entry.ClassId" in action_set_changes_handler_text
        and "UnknownClassId" in action_set_changes_handler_text,
        "Action-set change handler validates AMP series": "EldanAugmentationIdRequired" in action_set_changes_handler_text
        and "EldanAugmentationInvalidSeries" in action_set_changes_handler_text,
    }

    lines.append("## Server AMP Validation Surface")
    lines.append("")
    lines.append("| Implementation surface | Present | Result |")
    lines.append("| --- | --- | --- |")

    for marker, present in validation_markers.items():
        if not present:
            failures.append(f"server AMP validation surface missing marker: {marker}")
        lines.append(f"| {marker} | {'yes' if present else 'no'} | {'PASS' if present else 'FAIL'} |")

    if inlaid_unlock_rows:
        warnings.append(
            "client EldanAugmentation rows include inlaid item unlock ids; learned inlaid AMP state remains blocked"
        )
    if tier_locked_rows:
        warnings.append(
            "client EldanAugmentation rows include category tier locks; tier unlock spending rules remain blocked"
        )

    lines.append("")
    lines.append(
        "- Result: INFO, id/power/class/series validation is covered; inlaid unlock and category-tier validation remain blocked."
    )
    lines.append("")

    return lines, failures, warnings


def build_housing_report(archive_dir: Path, client_sql_dir: Path, root: Path) -> tuple[list[str], list[str], list[str]]:
    failures: list[str] = []
    warnings: list[str] = []
    lines: list[str] = []

    wiki_facts = parse_wiki_housing_facts(archive_dir)
    decor_bonus_categories = set(wiki_facts["decor_bonus_categories"])  # type: ignore[arg-type]

    wiki_markers = {
        "Housing page documents level 14 unlock": bool(wiki_facts["level_14_unlock"]),
        "Housing page documents rested XP": bool(wiki_facts["rested_xp"]),
        "Housing page documents all five decor bonus categories": decor_bonus_categories == set(HOUSING_DECOR_BONUS_CATEGORIES),
        "Housing page documents sockets and plugs": bool(wiki_facts["sockets_and_plugs"]),
        "Housing page documents visitor rules": bool(wiki_facts["visitor_rules"]),
        "Housing page documents housing expeditions": bool(wiki_facts["housing_expeditions"]),
        "Housing page documents communities": bool(wiki_facts["communities"]),
        "Customizing page exposes room-configuration sections": int(wiki_facts["customizing_page_sections"]) >= 4,
    }

    lines.append("## Housing Wiki Coverage")
    lines.append("")
    lines.append("| Wiki fact | Present | Result |")
    lines.append("| --- | --- | --- |")

    for marker, present in wiki_markers.items():
        if not present:
            failures.append(f"wiki housing fact missing marker: {marker}")
        lines.append(f"| {marker} | {'yes' if present else 'no'} | {'PASS' if present else 'FAIL'} |")

    lines.append(f"| Decor bonus categories found | {format_set(decor_bonus_categories)} | PASS |")
    lines.append(f"| Room-configuration sections found | {int(wiki_facts['customizing_page_sections'])} | PASS |")
    lines.append("")

    table_counts = {
        table_name: count_client_table_rows(client_sql_dir, table_name)
        for table_name in HOUSING_CORE_TABLES
    }

    lines.append("## Client Housing Table Coverage")
    lines.append("")
    lines.append("| Client table | Rows | Result |")
    lines.append("| --- | --- | --- |")

    for table_name, row_count in table_counts.items():
        if row_count == 0:
            failures.append(f"client housing table has no rows: {table_name}")
        lines.append(f"| {table_name} | {row_count} | {'PASS' if row_count else 'FAIL'} |")

    lines.append("")

    residence_rows = parse_client_housing_residence_rows(client_sql_dir)
    plot_rows = parse_client_housing_plot_rows(client_sql_dir)
    plug_rows = parse_client_housing_plug_item_rows(client_sql_dir)
    decor_rows = parse_client_housing_decor_rows(client_sql_dir)

    residence_default_decor_rows = [
        row
        for row in residence_rows
        if int(row["default_roof"]) > 0
        or int(row["default_entryway"]) > 0
        or int(row["default_door"]) > 0
        or int(row["default_wallpaper"]) > 0
    ]
    plot_rows_with_default_plug = [row for row in plot_rows if int(row["default_plug_item_id"]) > 0]
    plug_rows_with_upgrade = [row for row in plug_rows if int(row["next_upgrade_id"]) > 0]
    plug_rows_with_contribution_cost = [
        row
        for row in plug_rows
        if any(int(row[f"contribution_0{index}"]) > 0 for index in range(5))
    ]
    plug_rows_with_upkeep = [
        row
        for row in plug_rows
        if int(row["upkeep_type"]) > 0 or int(row["upkeep_charges"]) > 0 or float(row["upkeep_time"]) > 0.0
    ]
    decor_rows_with_active_prop = [row for row in decor_rows if int(row["active_prop_creature_id"]) > 0]
    decor_rows_with_prerequisite = [row for row in decor_rows if int(row["prerequisite_id"]) > 0]
    decor_rows_with_limit_category = [row for row in decor_rows if int(row["limit_category_id"]) > 0]
    decor_buff_spell_ids = {
        int(row["interior_buff_spell_id"])
        for row in decor_rows
        if int(row["interior_buff_spell_id"]) > 0
    }
    rested_decor_effect_rows = parse_spell_effect_rows_for_types(client_sql_dir, {0x006D})
    rested_decor_spell_ids = {int(row["spell_id"]) for row in rested_decor_effect_rows}
    decor_rested_buff_spell_ids = decor_buff_spell_ids & rested_decor_spell_ids
    scale_minimums = {float(row["min_scale"]) for row in decor_rows}
    scale_maximums = {float(row["max_scale"]) for row in decor_rows}

    lines.append("## Client Housing Data Details")
    lines.append("")
    lines.append(f"- Residences with default decor or wallpaper: {len(residence_default_decor_rows)}")
    lines.append(f"- Plot rows with default plug items: {len(plot_rows_with_default_plug)}")
    lines.append(f"- Plug rows with upgrade chain: {len(plug_rows_with_upgrade)}")
    lines.append(f"- Plug rows with contribution cost ids: {len(plug_rows_with_contribution_cost)}")
    lines.append(f"- Plug rows with upkeep fields: {len(plug_rows_with_upkeep)}")
    lines.append(f"- Decor rows with active-prop creature ids: {len(decor_rows_with_active_prop)}")
    lines.append(f"- Decor rows with unlock prerequisites: {len(decor_rows_with_prerequisite)}")
    lines.append(f"- Decor rows with limit categories: {len(decor_rows_with_limit_category)}")
    lines.append(f"- Decor rows with interior buff spells: {len(decor_buff_spell_ids)}")
    lines.append(f"- Decor/campfire `RestedXpDecorBonus` spell-effect rows: {len(rested_decor_effect_rows)}")
    lines.append(f"- Decor interior buff spells using `RestedXpDecorBonus`: {len(decor_rested_buff_spell_ids)}")
    lines.append(f"- Decor scale min range: {min(scale_minimums):g}-{max(scale_minimums):g}")
    lines.append(f"- Decor scale max range: {min(scale_maximums):g}-{max(scale_maximums):g}")

    if wiki_facts["five_times_scale_claim"] and (min(scale_minimums) < 0.2 or max(scale_maximums) > 5.0):
        warnings.append(
            "wiki housing scale text says five times bigger/smaller, but client HousingDecorInfo allows broader per-row scale data"
        )

    lines.append("")

    residence_text = read_text(root / "Source" / "NexusForever.Game" / "Housing" / "Residence.cs")
    plot_text = read_text(root / "Source" / "NexusForever.Game" / "Housing" / "Plot.cs")
    decor_text = read_text(root / "Source" / "NexusForever.Game" / "Housing" / "Decor.cs")
    residence_map_text = read_text(root / "Source" / "NexusForever.Game" / "Map" / "Instance" / "ResidenceMapInstance.cs")
    decor_update_handler_text = read_text(root / "Source" / "NexusForever.WorldServer" / "Network" / "Message" / "Handler" / "Housing" / "ClientHousingDecorUpdateHandler.cs")
    plug_update_handler_text = read_text(root / "Source" / "NexusForever.WorldServer" / "Network" / "Message" / "Handler" / "Housing" / "ClientHousingPlugUpdateHander.cs")
    neighbor_handlers_text = read_text(root / "Source" / "NexusForever.WorldServer" / "Network" / "Message" / "Handler" / "Housing" / "ClientHousingNeighborHandlers.cs")
    client_plug_update_text = read_text(root / "Source" / "NexusForever.Network.World" / "Message" / "Model" / "ClientHousingPlugUpdate.cs")
    client_decor_update_text = read_text(root / "Source" / "NexusForever.Network.World" / "Message" / "Model" / "ClientHousingDecorUpdate.cs")
    client_wallpaper_update_text = read_text(root / "Source" / "NexusForever.Network.World" / "Message" / "Model" / "ClientHousingInteriorWallpaperUpdate.cs")

    housing_surface_markers = {
        "Residence validates and saves decor/wallpaper game-table ids": "HousingDecorInfo.GetEntry" in residence_text
        and "HousingWallpaperInfo.GetEntry" in residence_text
        and "ResidenceSaveMask.Wallpaper" in residence_text,
        "Residence creates plots from HousingPlotInfo rows": "GameTableManager.Instance.HousingPlotInfo.Entries" in residence_text
        and "new Plot" in residence_text,
        "Plot default plugs use HousingPlugItem rows": "HousingPlugItemIdDefault" in plot_text
        and "GameTableManager.Instance.HousingPlugItem.GetEntry" in plot_text,
        "Decor serializes ServerHousingResidenceDecor payloads": "ServerHousingResidenceDecor.Decor Build()" in decor_text
        and "DecorInfoId" in decor_text,
        "Client decor update packet reads DecorInfo records": "List<DecorInfo> DecorUpdates" in client_decor_update_text
        and "Operation = reader.ReadEnum<DecorUpdateOperation>" in client_decor_update_text,
        "WorldServer routes decor updates into ResidenceMapInstance": "DecorUpdate(session.Player, housingDecorUpdate)" in decor_update_handler_text
        and "DecorUpdate(IPlayer player" in residence_map_text,
        "Client plug update packet reads mapped plug fields": "HousingPlotInfoId = reader.ReadUInt()" in client_plug_update_text
        and "HousingPlugItemId = reader.ReadUInt()" in client_plug_update_text,
        "WorldServer routes plug updates into ResidenceMapInstance": "PlugUpdate(session.Player, housingPlugUpdate)" in plug_update_handler_text
        and "PlugUpdate(IPlayer player" in residence_map_text,
        "Plug placement validates plot and plug table rows": "HousingPlugItem.GetEntry" in residence_map_text
        and "PlotInfoEntry.HousingPlugItemIdDefault" in residence_map_text,
        "Unsupported plug cost/prereq/runtime paths are explicitly bounded": "HasUnsupportedPlugContributionCost" in residence_map_text
        and "HasUnsupportedPlugPrerequisites" in residence_map_text
        and "HasUnsupportedPlugRuntime" in residence_map_text,
        "Interior wallpaper update packet is parsed": "ClientHousingInteriorWallpaperUpdate" in client_wallpaper_update_text
        and "DecorUpdates.Add(decor)" in client_wallpaper_update_text,
        "Interior wallpaper update is diagnostic-only until persistence is mapped": "changedSlots" in neighbor_handlers_text
        and "ClientHousingInteriorWallpaperUpdateHandler" in neighbor_handlers_text,
    }

    lines.append("## Server Housing Runtime Surface")
    lines.append("")
    lines.append("| Implementation surface | Present | Result |")
    lines.append("| --- | --- | --- |")

    for marker, present in housing_surface_markers.items():
        if not present:
            failures.append(f"server housing runtime surface missing marker: {marker}")
        lines.append(f"| {marker} | {'yes' if present else 'no'} | {'PASS' if present else 'FAIL'} |")

    lines.append("")

    xp_manager_text = read_text(root / "Source" / "NexusForever.Game" / "Entity" / "XpManager.cs")
    character_model_text = read_text(root / "Source" / "NexusForever.Database.Character" / "Model" / "CharacterModel.cs")
    server_player_create_text = read_text(root / "Source" / "NexusForever.Network.World" / "Message" / "Model" / "ServerPlayerCreate.cs")
    server_experience_gained_text = read_text(root / "Source" / "NexusForever.Network.World" / "Message" / "Model" / "ServerExperienceGained.cs")
    spell_handler_text = read_text(root / "Source" / "NexusForever.Game" / "Spell" / "SpellEffectHandler.cs")
    initial_findings_text = read_text(root / "Decomp" / "Analysis" / "INITIAL_FINDINGS.md")

    rested_xp_markers = {
        "RestBonusXp is persisted on the character model": "public uint RestBonusXp" in character_model_text,
        "ServerPlayerCreate sends RestBonusXp": "public uint RestBonusXp" in server_player_create_text
        and "writer.Write(RestBonusXp)" in server_player_create_text,
        "ServerExperienceGained sends RestXpAmount": "public uint RestXpAmount" in server_experience_gained_text
        and "writer.Write(RestXpAmount)" in server_experience_gained_text,
        "Kill creature XP consumes the rested pool": "RestBonusXp -= restXp" in xp_manager_text
        and "RestXpAmount      = restXp" in xp_manager_text,
        "Direct ModifyRestedXP effect modifies rested pool": "SpellEffectType.ModifyRestedXP" in spell_handler_text
        and "ModifyRestBonusXp" in spell_handler_text,
        "RestedXpDecorBonus has no mutating handler": "SpellEffectType.RestedXpDecorBonus" not in spell_handler_text,
        "Decompile notes record RestedXpDecorBonus as blocked": "RestedXpDecorBonus blocker state" in initial_findings_text,
    }

    lines.append("## Housing Rested-XP Surface")
    lines.append("")
    lines.append("| Implementation surface | Present | Result |")
    lines.append("| --- | --- | --- |")

    for marker, present in rested_xp_markers.items():
        if not present:
            failures.append(f"housing rested-XP surface missing marker: {marker}")
        lines.append(f"| {marker} | {'yes' if present else 'no'} | {'PASS' if present else 'FAIL'} |")

    if decor_rested_buff_spell_ids:
        warnings.append(
            "client decor/campfire buff spells include RestedXpDecorBonus rows; accrual timing, stacking, and persistence remain blocked by decompile evidence"
        )
    if "HousingRestXpPercentPerHour" in xp_manager_text:
        warnings.append(
            "base housing rested-XP login accrual exists in server code, but decor/category multipliers remain blocked until stronger client/runtime evidence is mapped"
        )

    lines.append("")
    lines.append(
        "- Result: INFO, residence/plot/plug/decor surfaces are table-backed; `RestedXpDecorBonus` remains mapped-only/blocked."
    )
    lines.append("")

    return lines, failures, warnings


def build_quests_report(archive_dir: Path, client_sql_dir: Path, root: Path) -> tuple[list[str], list[str], list[str]]:
    failures: list[str] = []
    warnings: list[str] = []
    lines: list[str] = []

    wiki_quest_pages = parse_wiki_quest_pages(archive_dir)
    client_quest_rows = parse_client_quest_rows(client_sql_dir)
    client_objective_rows = parse_client_quest_objective_rows(client_sql_dir)
    client_reward_rows = parse_client_quest_reward_rows(client_sql_dir)
    client_quest_names = parse_quest_client_name_map(root)

    lines.append("## Quest Wiki Page Coverage")
    lines.append("")
    lines.append(f"- Quest infobox pages: {len(wiki_quest_pages)}")
    lines.append(f"- Pages with objective bullets: {sum(1 for page in wiki_quest_pages if int(page['objective_count']) > 0)}")
    lines.append(f"- Pages with reward fields: {sum(1 for page in wiki_quest_pages if bool(page['has_rewards']))}")
    lines.append(f"- Pages with episode fields: {sum(1 for page in wiki_quest_pages if bool(page['episode']))}")
    lines.append(f"- Pages with previous/next chain fields: {sum(1 for page in wiki_quest_pages if bool(page['has_previous']) or bool(page['has_next']))}")
    lines.append(f"- Pages with episode-progression transclusion: {sum(1 for page in wiki_quest_pages if bool(page['has_episode_progression']))}")

    if not wiki_quest_pages:
        failures.append("wiki archive has no quest infobox pages")

    if client_quest_names:
        wiki_titles = {normalise_name(str(page["title"])) for page in wiki_quest_pages}
        client_titles = {normalise_name(name) for name in client_quest_names.values()}
        matched_titles = wiki_titles & client_titles
        unmatched_titles = sorted(
            str(page["title"])
            for page in wiki_quest_pages
            if normalise_name(str(page["title"])) not in client_titles
        )
        lines.append(f"- Wiki quest titles matched to DataMapping client names: {len(matched_titles)}/{len(wiki_titles)}")
        if unmatched_titles:
            warnings.append(
                f"{len(unmatched_titles)} wiki quest page title(s) do not match DataMapping quest_client_map names; sample: {', '.join(unmatched_titles[:8])}"
            )
    else:
        warnings.append("DataMapping quest_client_map.csv is unavailable; wiki quest title matching skipped")

    lines.append("")

    table_counts = {
        table_name: count_client_table_rows(client_sql_dir, table_name)
        for table_name in QUEST_CORE_TABLES
    }

    lines.append("## Client Quest Table Coverage")
    lines.append("")
    lines.append("| Client table | Rows | Result |")
    lines.append("| --- | --- | --- |")

    for table_name, row_count in table_counts.items():
        if row_count == 0:
            failures.append(f"client quest table has no rows: {table_name}")
        lines.append(f"| {table_name} | {row_count} | {'PASS' if row_count else 'FAIL'} |")

    lines.append("")

    quests_with_objectives = [row for row in client_quest_rows if row["objective_ids"]]
    quests_with_rewards = {
        int(row["quest_id"])
        for row in client_reward_rows
    }
    quests_with_prereq_quests = [row for row in client_quest_rows if row["prerequisite_quests"]]
    quests_with_prereq_id = [row for row in client_quest_rows if int(row["prerequisite_id"]) > 0]
    quests_with_level_prereq = [row for row in client_quest_rows if int(row["prerequisite_level"]) > 0]
    quests_with_race_prereq = [row for row in client_quest_rows if int(row["prerequisite_race"]) > 0]
    quests_with_class_prereq = [row for row in client_quest_rows if int(row["prerequisite_class"]) > 0]
    quests_with_faction_gate = [row for row in client_quest_rows if int(row["quest_player_faction"]) in (0, 1)]
    quests_with_item_prereq = [row for row in client_quest_rows if int(row["prerequisite_item"]) > 0]
    quests_with_faction_level_prereq = [row for row in client_quest_rows if row["faction_prerequisites"]]
    quests_with_exclusion_prereq = [row for row in client_quest_rows if row["quest_exclusion_prerequisites"]]
    quests_with_alt_receiver_prereq = [row for row in client_quest_rows if row["alt_receiver_prerequisites"]]
    quests_with_pushed_items = [row for row in client_quest_rows if row["pushed_item_ids"]]
    quests_with_virtual_items = [row for row in client_quest_rows if row["virtual_item_ids"]]
    repeatable_quests = [row for row in client_quest_rows if int(row["quest_repeat_period"]) > 0]
    timed_quests = [row for row in client_quest_rows if int(row["max_time_allowed_ms"]) > 0]
    grouped_quests = [row for row in client_quest_rows if int(row["group_size"]) > 0]

    lines.append("## Client Quest Data Details")
    lines.append("")
    lines.append(f"- Client Quest2 rows: {len(client_quest_rows)}")
    lines.append(f"- Client Quest2 rows with objectives: {len(quests_with_objectives)}")
    lines.append(f"- Client Quest2 rows with Quest2Reward rows: {len(quests_with_rewards)}")
    lines.append(f"- Client Quest2 rows with prerequisite quest ids: {len(quests_with_prereq_quests)}")
    lines.append(f"- Client Quest2 rows with prerequisiteId: {len(quests_with_prereq_id)}")
    lines.append(f"- Client Quest2 rows with level prerequisite: {len(quests_with_level_prereq)}")
    lines.append(f"- Client Quest2 rows with race prerequisite: {len(quests_with_race_prereq)}")
    lines.append(f"- Client Quest2 rows with class prerequisite: {len(quests_with_class_prereq)}")
    lines.append(f"- Client Quest2 rows with player-faction gate: {len(quests_with_faction_gate)}")
    lines.append(f"- Client Quest2 rows with item prerequisite: {len(quests_with_item_prereq)}")
    lines.append(f"- Client Quest2 rows with faction-level prerequisite fields: {len(quests_with_faction_level_prereq)}")
    lines.append(f"- Client Quest2 rows with exclusion prerequisite fields: {len(quests_with_exclusion_prereq)}")
    lines.append(f"- Client Quest2 rows with alt-receiver prerequisite fields: {len(quests_with_alt_receiver_prereq)}")
    lines.append(f"- Client Quest2 rows with pushed item ids: {len(quests_with_pushed_items)}")
    lines.append(f"- Client Quest2 rows with virtual pushed item ids: {len(quests_with_virtual_items)}")
    lines.append(f"- Client Quest2 rows with repeat periods: {len(repeatable_quests)}")
    lines.append(f"- Client Quest2 rows with quest timers: {len(timed_quests)}")
    lines.append(f"- Client Quest2 rows with group size: {len(grouped_quests)}")
    lines.append("")

    objective_type_names = parse_enum_values_by_id(root / "Source" / "NexusForever.Game.Static" / "Quest" / "QuestObjectiveType.cs")
    reward_type_names = parse_enum_values_by_id(root / "Source" / "NexusForever.Game.Static" / "Quest" / "QuestRewardType.cs")
    objective_type_counts = Counter(int(row["type"]) for row in client_objective_rows)
    reward_type_counts = Counter(int(row["type"]) for row in client_reward_rows)

    missing_objective_types = sorted(set(objective_type_counts) - set(objective_type_names))
    missing_reward_types = sorted(set(reward_type_counts) - set(reward_type_names))
    if missing_objective_types:
        failures.append(f"QuestObjectiveType enum missing client type(s): {format_numbers(missing_objective_types)}")
    if missing_reward_types:
        failures.append(f"QuestRewardType enum missing client reward type(s): {format_numbers(missing_reward_types)}")

    lines.append("## Quest Objective Type Coverage")
    lines.append("")
    lines.append("| Type id | Server enum | Client rows | Result |")
    lines.append("| --- | --- | --- | --- |")
    for objective_type_id, row_count in sorted(objective_type_counts.items()):
        enum_name = objective_type_names.get(objective_type_id, "missing")
        lines.append(
            f"| {objective_type_id} | {enum_name} | {row_count} | {'PASS' if enum_name != 'missing' else 'FAIL'} |"
        )

    lines.append("")
    lines.append("## Quest Reward Type Coverage")
    lines.append("")
    lines.append("| Type id | Server enum | Client rows | Runtime grant | Result |")
    lines.append("| --- | --- | --- | --- | --- |")

    implemented_reward_types = {1, 2, 3, 4, 5, 7}
    for reward_type_id, row_count in sorted(reward_type_counts.items()):
        enum_name = reward_type_names.get(reward_type_id, "missing")
        runtime_grant = reward_type_id in implemented_reward_types
        lines.append(
            f"| {reward_type_id} | {enum_name} | {row_count} | {'yes' if runtime_grant else 'blocked'} | {'PASS' if enum_name != 'missing' else 'FAIL'} |"
        )
        if enum_name != "missing" and not runtime_grant:
            if reward_type_id == 10:
                warnings.append(
                    "Quest2Reward type 10 (RotationEssence) expands through server-provided active reward-rotation "
                    "state keyed by WorldZone.RewardRotationContentId and Quest2.Id; no runtime grant is safe until "
                    "the server has authoritative schedule entries and multiplier"
                )
            else:
                warnings.append(
                    f"Quest2Reward type {reward_type_id} ({enum_name}) has {row_count} client row(s) but no mapped runtime grant"
                )

    lines.append("")

    global_quest_manager_text = read_text(root / "Source" / "NexusForever.Game" / "Quest" / "GlobalQuestManager.cs")
    quest_info_text = read_text(root / "Source" / "NexusForever.Game" / "Quest" / "QuestInfo.cs")
    quest_text = read_text(root / "Source" / "NexusForever.Game" / "Quest" / "Quest.cs")
    quest_objective_text = read_text(root / "Source" / "NexusForever.Game" / "Quest" / "QuestObjective.cs")
    quest_manager_text = read_text(root / "Source" / "NexusForever.Game" / "Entity" / "QuestManager.cs")
    quest_handler_dir = root / "Source" / "NexusForever.WorldServer" / "Network" / "Message" / "Handler" / "Quest"
    quest_handler_text = "\n".join(read_text(path) for path in sorted(quest_handler_dir.glob("*.cs")))
    server_quest_init_text = read_text(root / "Source" / "NexusForever.Network.World" / "Message" / "Model" / "ServerQuestInit.cs")
    server_objective_update_text = read_text(root / "Source" / "NexusForever.Network.World" / "Message" / "Model" / "ServerQuestObjectiveUpdate.cs")
    initial_findings_text = read_text(root / "Decomp" / "Analysis" / "INITIAL_FINDINGS.md")

    quest_item_prereq_mapped = (
        "PrerequisiteItem" in quest_manager_text
        and "Inventory.HasItemCount(info.Entry.PrerequisiteItem, 1u)" in quest_manager_text
    )
    quest_exclusion_prereq_mapped = (
        "QuestIdExclusionPreq0" in quest_manager_text
        and "QuestIdExclusionPreq1" in quest_manager_text
        and "QuestIdExclusionPreq2" in quest_manager_text
        and "GetQuestState((ushort)exclusionQuestId) != null" in quest_manager_text
    )
    quest_faction_level_prereq_mapped = (
        "MeetsFactionLevelPrerequisites" in quest_manager_text
        and "MeetsFactionLevelRequirement" in quest_manager_text
        and "FactionLevelCompPreq0" in quest_manager_text
        and "FactionNode.GetFactionLevel" in quest_manager_text
    )
    quest_virtual_items_client_derived = (
        "QuestRuntime_BuildVirtualItemList" in initial_findings_text
        and "client-derived UI/inventory projection" in initial_findings_text
    )
    quest_alt_receivers_client_guidance = (
        "Quest receiver-routing scope" in initial_findings_text
        and "Dialog_BuildCreatureQuestResponses" in initial_findings_text
        and "client-side dialog/map guidance metadata" in initial_findings_text
    )

    quest_surface_markers = {
        "GlobalQuestManager caches Quest2 rows": "GameTableManager.Instance.Quest2.Entries" in global_quest_manager_text
        and "new QuestInfo(entry)" in global_quest_manager_text,
        "QuestInfo resolves prerequisite quests": "InitialisePrerequisiteQuests" in quest_info_text
        and "Entry.PrerequisiteQuests" in quest_info_text,
        "QuestInfo resolves QuestObjective rows": "InitialiseObjectives" in quest_info_text
        and "GameTableManager.Instance.QuestObjective.GetEntry" in quest_info_text,
        "QuestInfo resolves Quest2Reward rows": "InitialiseRewards" in quest_info_text
        and "GameTableManager.Instance.Quest2Reward.Entries" in quest_info_text,
        "Quest accept validates faction/race/class/level/prereq quests/prerequisiteId": "QuestPlayerFactionEnum" in quest_manager_text
        and "PrerequisiteRace" in quest_manager_text
        and "PrerequisiteClass" in quest_manager_text
        and "PrerequisiteLevel" in quest_manager_text
        and "PrerequisiteQuests" in quest_manager_text
        and "PrerequisiteManager.Instance.Meets" in quest_manager_text,
        "Quest accept validates prerequisite item": quest_item_prereq_mapped,
        "Quest accept validates quest exclusion prerequisites": quest_exclusion_prereq_mapped,
        "Quest accept validates faction-level prerequisites": quest_faction_level_prereq_mapped,
        "Quest alternate receivers are mapped as client-side guidance": quest_alt_receivers_client_guidance
        and "GetQuestReceivers" in quest_manager_text,
        "Quest virtual pushed items are client-derived from quest state": quest_virtual_items_client_derived
        and "QuestObjectiveType.VirtualCollect" in quest_manager_text + quest_text + read_text(root / "Source" / "NexusForever.Game" / "Loot" / "LootInstanceItem.cs"),
        "Quest accept grants pushed items": "Entry.PushedItemIds" in quest_manager_text
        and "ItemCreate(InventoryLocation.Inventory, itemId" in quest_manager_text,
        "Quest objective progress persists and sends updates": "QuestObjectiveSaveMask.Progress" in quest_objective_text
        and "ServerQuestObjectiveUpdate" in quest_text,
        "Quest completion validates state and receiver/communicator": "quest.State != QuestState.Achieved" in quest_manager_text
        and "IsCommunicatorReceived" in quest_manager_text
        and "GetQuestReceivers" in quest_manager_text,
        "Quest completion grants item rewards": "QuestRewardType.Item" in quest_manager_text
        and "Inventory.ItemCreate" in quest_manager_text,
        "Quest completion grants reputation rewards": "QuestRewardType.Reputation" in quest_manager_text
        and "ReputationManager.UpdateReputation" in quest_manager_text,
        "Quest completion grants currency rewards": "QuestRewardType.Money" in quest_manager_text
        and "CurrencyManager.CurrencyAddAmount" in quest_manager_text,
        "Quest completion grants tradeskill XP and professions": "QuestRewardType.TradeSkillXp" in quest_manager_text
        and "AddTradeskillXpForTier" in quest_manager_text
        and "QuestRewardType.TradeSkill" in quest_manager_text
        and "LearnTradeskill" in quest_manager_text,
        "Quest completion grants account currency rewards": "QuestRewardType.AccountCurrency" in quest_manager_text
        and "Account.CurrencyManager.CurrencyAddAmount" in quest_manager_text,
        "Quest completion grants XP and cash overrides/formulas": "GetRewardExperience" in quest_manager_text
        and "GetRewardMoney" in quest_manager_text,
        "Quest repeat periods schedule daily/weekly reset": "QuestRepeatPeriod.Daily" in quest_manager_text
        and "QuestRepeatPeriod.Weekly" in quest_manager_text,
        "World handlers route quest lifecycle messages": "QuestAdd(questAccept.QuestId" in quest_handler_text
        and "QuestComplete(questComplete.QuestId" in quest_handler_text
        and "QuestAbandon(questAbandon.QuestId" in quest_handler_text
        and "QuestRetry(questRetry.QuestId" in quest_handler_text
        and "QuestIgnore(questSetIgnore.QuestId" in quest_handler_text
        and "QuestTrack(questSetTracked.QuestId" in quest_handler_text
        and "QuestShare(questShare.QuestId" in quest_handler_text
        and "QuestShareResult(questShareResult.QuestId" in quest_handler_text,
        "ServerQuestInit serializes active/inactive/completed state": "List<QuestComplete> Completed" in server_quest_init_text
        and "List<QuestActive> Active" in server_quest_init_text,
        "ServerQuestObjectiveUpdate serializes objective progress": "QuestObjectiveIndex" in server_objective_update_text
        and "Completed" in server_objective_update_text,
    }

    lines.append("## Server Quest Runtime Surface")
    lines.append("")
    lines.append("| Implementation surface | Present | Result |")
    lines.append("| --- | --- | --- |")

    for marker, present in quest_surface_markers.items():
        if not present:
            failures.append(f"server quest runtime surface missing marker: {marker}")
        lines.append(f"| {marker} | {'yes' if present else 'no'} | {'PASS' if present else 'FAIL'} |")

    if quests_with_item_prereq and not quest_item_prereq_mapped:
        warnings.append("Quest2 rows include prerequisite item fields; quest accept item-prerequisite handling remains blocked")
    if quests_with_faction_level_prereq and not quest_faction_level_prereq_mapped:
        warnings.append("Quest2 rows include faction-level prerequisite fields; quest accept faction reputation gates remain blocked")
    if quests_with_exclusion_prereq and not quest_exclusion_prereq_mapped:
        warnings.append("Quest2 rows include quest exclusion prerequisite fields; exclusion handling remains blocked")
    if quests_with_alt_receiver_prereq and not quest_alt_receivers_client_guidance:
        warnings.append("Quest2 rows include alternate receiver prerequisite fields; receiver routing semantics remain unmapped")
    if quests_with_virtual_items and not quest_virtual_items_client_derived:
        warnings.append("Quest2 rows include virtual pushed item fields; virtual-item push handling remains blocked")

    lines.append("")
    lines.append(
        "- Result: INFO, core quest lifecycle, item/exclusion/faction-level prerequisites, client-guidance alternate receivers, client-derived virtual pushed items, and item/money/reputation/tradeskill/account-currency rewards are covered; RotationEssence remains bounded by the active reward-rotation schedule blocker."
    )
    lines.append("")

    return lines, failures, warnings


def build_achievements_report(archive_dir: Path, client_sql_dir: Path, root: Path) -> tuple[list[str], list[str], list[str]]:
    failures: list[str] = []
    warnings: list[str] = []
    lines: list[str] = []

    wiki_achievement_pages = parse_wiki_achievement_pages(archive_dir)
    client_achievement_rows = parse_client_achievement_rows(client_sql_dir)
    client_checklist_rows = parse_client_achievement_checklist_rows(client_sql_dir)
    client_achievement_names = parse_achievement_client_name_map(root)

    lines.append("## Achievement Wiki Page Coverage")
    lines.append("")
    lines.append(f"- Achievement infobox pages: {len(wiki_achievement_pages)}")
    lines.append(f"- Pages with description fields: {sum(1 for page in wiki_achievement_pages if bool(page['has_description']))}")
    lines.append(f"- Pages with criteria fields: {sum(1 for page in wiki_achievement_pages if bool(page['has_criteria']))}")
    lines.append(f"- Pages with title reward fields: {sum(1 for page in wiki_achievement_pages if bool(page['has_title_reward']))}")
    lines.append(f"- Pages with series sections: {sum(1 for page in wiki_achievement_pages if bool(page['has_series']))}")

    if not wiki_achievement_pages:
        failures.append("wiki archive has no achievement infobox pages")

    if client_achievement_names:
        wiki_titles = {normalise_name(str(page["title"])) for page in wiki_achievement_pages}
        client_titles = {normalise_name(name) for name in client_achievement_names.values()}
        matched_titles = wiki_titles & client_titles
        unmatched_titles = sorted(
            str(page["title"])
            for page in wiki_achievement_pages
            if normalise_name(str(page["title"])) not in client_titles
        )
        lines.append(f"- Wiki achievement titles matched to DataMapping client names: {len(matched_titles)}/{len(wiki_titles)}")
        if unmatched_titles:
            warnings.append(
                f"{len(unmatched_titles)} wiki achievement page title(s) do not match DataMapping achievement_client_map names; sample: {', '.join(unmatched_titles[:8])}"
            )
    else:
        warnings.append("DataMapping achievement_client_map.csv is unavailable; wiki achievement title matching skipped")

    lines.append("")

    table_counts = {
        table_name: count_client_table_rows(client_sql_dir, table_name)
        for table_name in ACHIEVEMENT_CORE_TABLES
    }

    lines.append("## Client Achievement Table Coverage")
    lines.append("")
    lines.append("| Client table | Rows | Result |")
    lines.append("| --- | --- | --- |")

    for table_name, row_count in table_counts.items():
        if row_count == 0:
            failures.append(f"client achievement table has no rows: {table_name}")
        lines.append(f"| {table_name} | {row_count} | {'PASS' if row_count else 'FAIL'} |")

    lines.append("")

    achievements_with_checklists = {int(row["achievement_id"]) for row in client_checklist_rows}
    achievements_with_titles = [row for row in client_achievement_rows if int(row["character_title_id"]) > 0]
    achievements_with_parent_tier = [row for row in client_achievement_rows if int(row["parent_tier_id"]) > 0]
    achievements_with_prereq = [
        row
        for row in client_achievement_rows
        if int(row["prerequisite_id"]) > 0
        or int(row["prerequisite_id_server"]) > 0
        or int(row["prerequisite_id_objective"]) > 0
        or int(row["prerequisite_id_objective_alt"]) > 0
    ]
    checklist_rows_with_prereq = [
        row
        for row in client_checklist_rows
        if int(row["prerequisite_id"]) > 0 or int(row["prerequisite_id_alt"]) > 0
    ]
    steam_rows = [row for row in client_achievement_rows if str(row["steam_name"])]
    guild_rows = [row for row in client_achievement_rows if (int(row["flags"]) & 0x04) != 0]

    lines.append("## Client Achievement Data Details")
    lines.append("")
    lines.append(f"- Client Achievement rows: {len(client_achievement_rows)}")
    lines.append(f"- Client AchievementChecklist rows: {len(client_checklist_rows)}")
    lines.append(f"- Achievements with checklist rows: {len(achievements_with_checklists)}")
    lines.append(f"- Achievements with character title rewards: {len(achievements_with_titles)}")
    lines.append(f"- Achievements with parent tier id: {len(achievements_with_parent_tier)}")
    lines.append(f"- Achievements with prerequisite fields: {len(achievements_with_prereq)}")
    lines.append(f"- Checklist rows with prerequisite fields: {len(checklist_rows_with_prereq)}")
    lines.append(f"- Guild achievement rows: {len(guild_rows)}")
    lines.append(f"- Steam achievement name rows: {len(steam_rows)}")
    lines.append("")

    achievement_type_names = parse_enum_values_by_id(root / "Source" / "NexusForever.Game.Static" / "Achievement" / "AchievementType.cs")
    achievement_type_counts = Counter(int(row["type"]) for row in client_achievement_rows)
    missing_achievement_types = sorted(set(achievement_type_counts) - set(achievement_type_names))
    if missing_achievement_types:
        warnings.append(
            f"{len(missing_achievement_types)} client achievement type id(s) are not named in AchievementType; unmapped type ids remain data-only"
        )

    runtime_trigger_type_ids = {
        achievement_type_id
        for achievement_type_id, name in achievement_type_names.items()
        if name in {
            "KillCreatureEntry",
            "KillCreatureGroup",
            "KillCreatureChecklist",
            "QuestComplete",
            "QuestCompleteChecklist",
            "AchievementComplete",
            "EnterWorldZone",
            "DiscoverObject",
            "ActivateCreature",
            "CraftItem",
            "TradeskillTier",
            "CostumeUnlock",
            "CraftItemChecklist",
            "ReputationLevel",
            "QuestCompleteChecklistCount",
            "MapComplete",
            "CharacterLevel",
            "TitleEarned",
            "PathLevel",
            "GuildBelovedReputation",
            "CurrencyEarned",
            "ItemConsume",
            "FindSecretStash",
            "GuildMaxPathLevel",
            "DuelParticipate",
            "DuelWin",
            "ClassLevel50",
            "EmoteTargetCreature",
            "CriticalDeathblow",
            "GuildOrCircleJoin",
            "GroupJoin",
            "FriendAdd",
            "HousingPlugPlace",
            "HousingDecorPurchase",
            "RealmFirst",
            "ContractQualityComplete",
            "ContractTypeComplete",
            "ContractComplete",
            "AccountCurrencyEarned",
            "CostumeSetUnlock",
            "PublicEventObjectiveComplete",
            "PrimalEssenceEarned",
        }
    }
    blocked_runtime_type_ids = sorted(set(achievement_type_counts) - runtime_trigger_type_ids)
    if blocked_runtime_type_ids:
        warnings.append(
            f"{len(blocked_runtime_type_ids)} client achievement type id(s) have no mapped runtime trigger path"
        )

    lines.append("## Achievement Type Coverage")
    lines.append("")
    lines.append("| Type id | Server enum | Client rows | Runtime trigger | Result |")
    lines.append("| --- | --- | --- | --- | --- |")
    for achievement_type_id, row_count in sorted(achievement_type_counts.items()):
        enum_name = achievement_type_names.get(achievement_type_id, "unmapped")
        runtime_trigger = achievement_type_id in runtime_trigger_type_ids
        result = "PASS" if enum_name != "unmapped" else "INFO"
        lines.append(
            f"| {achievement_type_id} | {enum_name} | {row_count} | {'yes' if runtime_trigger else 'blocked'} | {result} |"
        )

    lines.append("")

    global_achievement_text = read_text(root / "Source" / "NexusForever.Game" / "Achievement" / "GlobalAchievementManager.cs")
    achievement_info_text = read_text(root / "Source" / "NexusForever.Game" / "Achievement" / "AchievementInfo.cs")
    achievement_text = read_text(root / "Source" / "NexusForever.Game" / "Achievement" / "Achievement.cs")
    base_achievement_text = read_text(root / "Source" / "NexusForever.Game" / "Achievement" / "BaseAchievementManager.cs")
    character_achievement_text = read_text(root / "Source" / "NexusForever.Game" / "Achievement" / "CharacterAchievementManager.cs")
    guild_achievement_text = read_text(root / "Source" / "NexusForever.Game" / "Achievement" / "GuildAchievementManager.cs")
    quest_manager_text = read_text(root / "Source" / "NexusForever.Game" / "Entity" / "QuestManager.cs")
    unit_entity_text = read_text(root / "Source" / "NexusForever.Game" / "Entity" / "UnitEntity.cs")
    simple_entity_text = read_text(root / "Source" / "NexusForever.Game" / "Entity" / "SimpleEntity.cs")
    client_activate_unit_handler_text = read_text(root / "Source" / "NexusForever.WorldServer" / "Network" / "Message" / "Handler" / "Entity" / "ClientActivateUnitHandler.cs")
    client_activate_unit_cast_handler_text = read_text(root / "Source" / "NexusForever.WorldServer" / "Network" / "Message" / "Handler" / "Entity" / "ClientActivateUnitCastHandler.cs")
    activation_achievement_updater_text = read_text(root / "Source" / "NexusForever.WorldServer" / "Network" / "Message" / "Handler" / "Entity" / "ActivationAchievementUpdater.cs")
    inventory_text = read_text(root / "Source" / "NexusForever.Game" / "Entity" / "Inventory.cs")
    currency_manager_text = read_text(root / "Source" / "NexusForever.Game" / "Entity" / "CurrencyManager.cs")
    player_text = read_text(root / "Source" / "NexusForever.Game" / "Entity" / "Player.cs")
    xp_manager_text = read_text(root / "Source" / "NexusForever.Game" / "Entity" / "XpManager.cs")
    path_manager_text = read_text(root / "Source" / "NexusForever.Game" / "Entity" / "PathManager.cs")
    title_manager_text = read_text(root / "Source" / "NexusForever.Game" / "Entity" / "TitleManager.cs")
    guild_manager_text = read_text(root / "Source" / "NexusForever.Game" / "Guild" / "GuildManager.cs")
    duel_manager_text = read_text(root / "Source" / "NexusForever.Game" / "Pvp" / "DuelManager.cs")
    residence_map_text = read_text(root / "Source" / "NexusForever.Game" / "Map" / "Instance" / "ResidenceMapInstance.cs")
    reputation_manager_text = read_text(root / "Source" / "NexusForever.Game" / "Reputation" / "ReputationManager.cs")
    zone_map_text = read_text(root / "Source" / "NexusForever.Game" / "Map" / "ZoneMapManager.cs")
    spell_handler_text = read_text(root / "Source" / "NexusForever.Game" / "Spell" / "SpellEffectHandler.cs")
    emote_handler_text = read_text(root / "Source" / "NexusForever.WorldServer" / "Network" / "Message" / "Handler" / "Chat" / "ClientEmoteHandler.cs")
    craft_handler_text = read_text(root / "Source" / "NexusForever.WorldServer" / "Network" / "Message" / "Handler" / "Crafting" / "ClientCraftingCraftHandlers.cs")
    loot_instance_item_text = read_text(root / "Source" / "NexusForever.Game" / "Loot" / "LootInstanceItem.cs")
    account_inventory_text = read_text(root / "Source" / "NexusForever.Game" / "Account" / "Inventory" / "AccountInventoryManager.cs")
    account_currency_achievement_text = read_text(root / "Source" / "NexusForever.Game" / "Achievement" / "AccountCurrencyAchievementUpdater.cs")
    account_costume_manager_text = read_text(root / "Source" / "NexusForever.Game" / "Account" / "Costume" / "AccountCostumeManager.cs")
    costume_achievement_text = read_text(root / "Source" / "NexusForever.Game" / "Achievement" / "CostumeAchievementUpdater.cs")
    costume_unlock_handler_text = read_text(root / "Source" / "NexusForever.WorldServer" / "Network" / "Message" / "Handler" / "Costume" / "ClientCostumeItemUnlockHandler.cs")
    public_event_objective_text = read_text(root / "Source" / "NexusForever.Game" / "PublicEvent" / "PublicEventObjective.cs")
    group_joined_handler_text = read_text(root / "Source" / "NexusForever.WorldServer" / "Network" / "Internal" / "Handler" / "Group" / "GroupMemberJoinedHandler.cs")
    friendship_added_handler_text = read_text(root / "Source" / "NexusForever.WorldServer" / "Network" / "Internal" / "Handler" / "Friendship" / "FriendshipAddedHandler.cs")
    friendship_type_handler_text = read_text(root / "Source" / "NexusForever.WorldServer" / "Network" / "Internal" / "Handler" / "Friendship" / "FriendshipTypeUpdatedHandler.cs")
    server_achievement_init_text = read_text(root / "Source" / "NexusForever.Network.World" / "Message" / "Model" / "Achievement" / "ServerAchievementInit.cs")
    server_achievement_update_text = read_text(root / "Source" / "NexusForever.Network.World" / "Message" / "Model" / "Achievement" / "ServerAchievementUpdate.cs")

    achievement_surface_markers = {
        "GlobalAchievementManager caches Achievement rows": "GameTableManager.Instance.Achievement.Entries" in global_achievement_text
        and "new AchievementInfo(entry)" in global_achievement_text,
        "AchievementInfo loads checklist rows": "AchievementChecklist.Entries" in achievement_info_text
        and "AchievementId == entry.Id" in achievement_info_text,
        "Achievement persistence tracks data and completion time": "SaveMask.Data0" in achievement_text
        and "DateCompleted" in achievement_text,
        "Achievement completion handles value and checklist progress": "Info.ChecklistEntries.Count == 0" in achievement_text
        and "Info.ChecklistEntries.All" in achievement_text,
        "Base manager sends initial/update packets": "ServerAchievementInit" in base_achievement_text
        and "ServerAchievementUpdate" in base_achievement_text,
        "Base manager checks achievement prerequisites": "PrerequisiteIdServer" in base_achievement_text
        and "PrerequisiteIdObjectiveAlt" in base_achievement_text,
        "Character achievement completion grants titles": "TitleManager.AddTitle" in character_achievement_text,
        "Realm-first achievement broadcasts are supported": "TryClaimRealmFirstAchievement" in character_achievement_text
        and "TryClaimRealmFirstAchievement" in guild_achievement_text
        and "ServerRealmFirstAchievement" in base_achievement_text,
        "Quest completion triggers achievements": "AchievementType.QuestComplete" in quest_manager_text,
        "Quest completion triggers checklist achievements": "AchievementType.QuestCompleteChecklist" in quest_manager_text,
        "Quest completion triggers counted checklist achievements": "AchievementType.QuestCompleteChecklistCount" in quest_manager_text
        and "UsesChecklistValueProgress" in base_achievement_text,
        "Achievement completion triggers meta achievements": "AchievementType.AchievementComplete" in character_achievement_text
        and "AchievementType.AchievementComplete" in guild_achievement_text,
        "Creature kill triggers achievements": "AchievementType.KillCreatureEntry" in unit_entity_text
        and "AchievementType.KillCreatureGroup" in unit_entity_text,
        "Creature kill triggers checklist creature achievements": "AchievementType.KillCreatureChecklist" in unit_entity_text
        and "IsAllGuildGroup" in character_achievement_text,
        "World-zone entry triggers achievements": "AchievementType.EnterWorldZone" in player_text
        and "entry.WorldZoneId" in base_achievement_text,
        "Simple object activation triggers creature-id achievements": "AchievementType.ActivateCreature" in simple_entity_text
        and "DatacubeManager" in simple_entity_text,
        "Shared activation success triggers discovery-object achievements": "AchievementType.DiscoverObject" in activation_achievement_updater_text
        and "ActivationAchievementUpdater.Update" in client_activate_unit_handler_text
        and "ActivationAchievementUpdater.Update" in client_activate_unit_cast_handler_text,
        "Shared activation success triggers secret-stash achievements": "AchievementType.FindSecretStash" in activation_achievement_updater_text
        and "ActivationAchievementUpdater.Update" in client_activate_unit_handler_text
        and "ActivationAchievementUpdater.Update" in client_activate_unit_cast_handler_text,
        "Crafting triggers crafted-item achievements": "AchievementType.CraftItem" in craft_handler_text
        and "AchievementType.CraftItemChecklist" in craft_handler_text,
        "Tradeskill tier changes trigger achievements": "AchievementType.TradeskillTier" in player_text
        and "TradeskillTier.Entries" in player_text,
        "Reputation level changes trigger achievements": "AchievementType.ReputationLevel" in reputation_manager_text
        and "FactionNode.GetFactionLevel" in reputation_manager_text,
        "Map completion triggers achievements": "AchievementType.MapComplete" in zone_map_text,
        "Character level changes trigger achievements": "AchievementType.CharacterLevel" in xp_manager_text
        and "SetAchievementProgress" in xp_manager_text,
        "Title grants trigger achievements": "AchievementType.TitleEarned" in title_manager_text,
        "Path level changes trigger achievements": "AchievementType.PathLevel" in path_manager_text
        and "SetAchievementProgress" in path_manager_text,
        "Guild beloved reputation totals trigger achievements": "AchievementType.GuildBelovedReputation" in reputation_manager_text
        and "FactionLevel.Beloved" in reputation_manager_text,
        "Currency gains trigger earned-currency achievements": "AchievementType.CurrencyEarned" in currency_manager_text
        and "CurrencyAddAmount" in currency_manager_text,
        "Item consume triggers achievements": "AchievementType.ItemConsume" in inventory_text,
        "Guild max-path-level totals trigger achievements": "AchievementType.GuildMaxPathLevel" in path_manager_text
        and "MaxPathLevel" in path_manager_text,
        "Duel completion triggers participation and win achievements": "AchievementType.DuelParticipate" in duel_manager_text
        and "AchievementType.DuelWin" in duel_manager_text,
        "Class level-50 transitions trigger achievements": "AchievementType.ClassLevel50" in xp_manager_text
        and "player.Class" in xp_manager_text,
        "Targeted emotes trigger creature/emote checklist achievements": "AchievementType.EmoteTargetCreature" in emote_handler_text
        and "TargetUnitId" in emote_handler_text
        and "emote.EmoteId" in emote_handler_text,
        "Critical deathblows trigger achievements": "AchievementType.CriticalDeathblow" in unit_entity_text
        and "CombatResult.Critical" in unit_entity_text
        and "KilledTarget" in unit_entity_text,
        "Guild and circle joins trigger achievements": "AchievementType.GuildOrCircleJoin" in guild_manager_text
        and "GuildType" in guild_manager_text,
        "Group joins trigger achievements": "AchievementType.GroupJoin" in group_joined_handler_text,
        "Friend additions trigger achievements": "AchievementType.FriendAdd" in friendship_added_handler_text
        and "AchievementType.FriendAdd" in friendship_type_handler_text,
        "Housing plug placement triggers achievements": "AchievementType.HousingPlugPlace" in residence_map_text,
        "Housing decor purchases trigger achievements": "AchievementType.HousingDecorPurchase" in residence_map_text,
        "Contract completions trigger contract achievements": "AchievementType.ContractComplete" in quest_manager_text
        and "AchievementType.ContractQualityComplete" in quest_manager_text
        and "AchievementType.ContractTypeComplete" in quest_manager_text
        and "PeriodicQuestGroup.GetEntry" in quest_manager_text,
        "Account currency grants trigger achievements": "AccountCurrencyAchievementUpdater.Update" in quest_manager_text
        and "AccountCurrencyAchievementUpdater.Update" in loot_instance_item_text
        and "AccountCurrencyAchievementUpdater.Update" in account_inventory_text
        and "AchievementType.AccountCurrencyEarned" in account_currency_achievement_text,
        "Primal essence account-currency grants trigger total achievements": "AchievementType.PrimalEssenceEarned" in account_currency_achievement_text
        and "IsPrimalEssence" in account_currency_achievement_text,
        "Costume item unlocks trigger wardrobe achievements": "CostumeAchievementUpdater.Update" in account_costume_manager_text
        and "AchievementType.CostumeUnlock" in costume_achievement_text
        and "AchievementType.CostumeSetUnlock" in costume_achievement_text
        and "UnlockItem(session.Player, item)" in costume_unlock_handler_text,
        "Public event objective success triggers objective achievements": "AchievementType.PublicEventObjectiveComplete" in public_event_objective_text
        and "PublicEventStatus.Succeeded" in public_event_objective_text
        and "Team.GetMembers()" in public_event_objective_text,
        "AchievementAdvance spell effect grants achievements": "SpellEffectType.AchievementAdvance" in spell_handler_text
        and "GrantAchievement" in spell_handler_text,
        "ServerAchievementInit serializes achievements": "ServerAchievementInit" in server_achievement_init_text
        and "Achievements.ForEach" in server_achievement_init_text,
        "ServerAchievementUpdate serializes achievements": "ServerAchievementUpdate" in server_achievement_update_text
        and "Achievements.ForEach" in server_achievement_update_text,
    }

    lines.append("## Server Achievement Runtime Surface")
    lines.append("")
    lines.append("| Implementation surface | Present | Result |")
    lines.append("| --- | --- | --- |")

    for marker, present in achievement_surface_markers.items():
        if not present:
            failures.append(f"server achievement runtime surface missing marker: {marker}")
        lines.append(f"| {marker} | {'yes' if present else 'no'} | {'PASS' if present else 'FAIL'} |")

    lines.append("")
    lines.append(
        "- Result: INFO, persistence, packets, checklist/value completion, titles, realm-firsts, completion-driven meta achievements, and mapped trigger paths are covered; remaining client achievement types stay blocked until their event families are mapped."
    )
    lines.append("")

    return lines, failures, warnings


def build_tradeskills_report(archive_dir: Path, client_sql_dir: Path, root: Path) -> tuple[list[str], list[str], list[str]]:
    failures: list[str] = []
    warnings: list[str] = []
    lines: list[str] = []

    wiki_facts = parse_wiki_tradeskill_facts(archive_dir)
    client_tradeskills = parse_client_tradeskill_rows(client_sql_dir)
    client_tiers = parse_client_tradeskill_tier_rows(client_sql_dir)
    client_schematics = parse_client_tradeskill_schematic_rows(client_sql_dir)
    client_materials = parse_client_tradeskill_material_rows(client_sql_dir)
    client_harvesting = parse_client_tradeskill_harvesting_rows(client_sql_dir)

    wiki_markers = {
        "Tradeskill page exists": bool(wiki_facts["main_page_exists"]),
        "Crafting redirect exists": bool(wiki_facts["crafting_redirect_exists"]),
        "Tradeskills redirect exists": bool(wiki_facts["tradeskills_redirect_exists"]),
        "Tradeskill page documents level 10/two active limit": bool(wiki_facts["level_10_active_limit"]),
        "Tradeskill page documents gathering section": bool(wiki_facts["gathering_section"]),
        "Tradeskill page documents production section": bool(wiki_facts["production_section"]),
        "Tradeskill page documents hobbies section": bool(wiki_facts["hobbies_section"]),
        "Tradeskill page documents other section": bool(wiki_facts["other_section"]),
        "Tradeskill page documents runecrafting level 15": bool(wiki_facts["runecrafting_level_15"]),
        "Tradeskill page documents salvaging level 8": bool(wiki_facts["salvaging_level_8"]),
        "Profession pages exist": not wiki_facts["profession_pages_missing"],
    }

    lines.append("## Tradeskill Wiki Coverage")
    lines.append("")
    lines.append("| Wiki fact | Present | Result |")
    lines.append("| --- | --- | --- |")

    for marker, present in wiki_markers.items():
        if not present:
            failures.append(f"wiki tradeskill fact missing marker: {marker}")
        lines.append(f"| {marker} | {'yes' if present else 'no'} | {'PASS' if present else 'FAIL'} |")

    profession_pages_found = set(wiki_facts["profession_pages_found"])  # type: ignore[arg-type]
    schematic_professions = set(wiki_facts["schematic_professions"])  # type: ignore[arg-type]
    lines.append(f"| Profession pages found | {format_set(profession_pages_found)} | PASS |")
    lines.append(f"| Schematic list pages | {int(wiki_facts['schematic_list_pages'])} | PASS |")
    lines.append(f"| Schematic professions found | {format_set(schematic_professions)} | PASS |")
    lines.append("")

    table_counts = {
        table_name: count_client_table_rows(client_sql_dir, table_name)
        for table_name in TRADESKILL_CORE_TABLES
    }

    lines.append("## Client Tradeskill Table Coverage")
    lines.append("")
    lines.append("| Client table | Rows | Result |")
    lines.append("| --- | --- | --- |")

    for table_name, row_count in table_counts.items():
        if row_count == 0:
            failures.append(f"client tradeskill table has no rows: {table_name}")
        lines.append(f"| {table_name} | {row_count} | {'PASS' if row_count else 'FAIL'} |")

    lines.append("")

    server_tradeskill_enum = parse_enum_values_by_id(root / "Source" / "NexusForever.Game.Static" / "Crafting" / "TradeskillType.cs")
    client_tradeskill_ids = {int(row["id"]) for row in client_tradeskills}
    missing_server_tradeskill_ids = sorted(client_tradeskill_ids - set(server_tradeskill_enum))
    wiki_profession_ids = {
        TRADESKILL_WIKI_PROFESSION_TO_ID[name]
        for name in profession_pages_found
        if name in TRADESKILL_WIKI_PROFESSION_TO_ID
    }
    client_only_tradeskill_names = {
        str(row["name"])
        for row in client_tradeskills
        if int(row["id"]) not in wiki_profession_ids
    }

    if missing_server_tradeskill_ids:
        failures.append(
            f"client Tradeskill ids are missing server TradeskillType enum entries: {format_numbers(missing_server_tradeskill_ids)}"
        )
    if "Fishing" in client_only_tradeskill_names:
        warnings.append("client Tradeskill.tbl includes Fishing, but the archived wiki tradeskill page does not expose it as a profession page")
    if server_tradeskill_enum.get(16) == "Augmentor":
        warnings.append("server TradeskillType id 16 is named Augmentor while wiki-facing profession text uses Technologist")

    schematics_by_tradeskill = Counter(int(row["tradeskill_id"]) for row in client_schematics)
    tiers_by_tradeskill = Counter(int(row["tradeskill_id"]) for row in client_tiers)

    lines.append("## Tradeskill Id Coverage")
    lines.append("")
    lines.append("| Client id | Canonical name | Server enum | Wiki page | Schematic rows | Tier rows | Result |")
    lines.append("| --- | --- | --- | --- | --- | --- | --- |")

    for row in sorted(client_tradeskills, key=lambda item: int(item["id"])):
        tradeskill_id = int(row["id"])
        canonical_name = str(row["name"])
        enum_name = server_tradeskill_enum.get(tradeskill_id, "missing")
        wiki_page = canonical_name in profession_pages_found
        if enum_name == "missing":
            result = "FAIL"
        elif wiki_page or canonical_name == "Fishing":
            result = "PASS" if wiki_page else "INFO"
        else:
            result = "INFO"
        lines.append(
            f"| {tradeskill_id} | {canonical_name} | {enum_name} | {'yes' if wiki_page else 'no'} | "
            f"{schematics_by_tradeskill[tradeskill_id]} | {tiers_by_tradeskill[tradeskill_id]} | {result} |"
        )

    lines.append("")

    schematics_with_direct_output = [row for row in client_schematics if int(row["item_output_id"]) > 0 and int(row["output_count"]) > 0]
    schematics_with_loot_output = [row for row in client_schematics if int(row["loot_id"]) > 0]
    schematics_with_materials = [row for row in client_schematics if row["material_item_ids"]]
    schematics_with_parent = [row for row in client_schematics if int(row["parent_id"]) > 0]
    schematics_with_fail_output = [row for row in client_schematics if int(row["item_output_fail_id"]) > 0]
    schematics_with_crit_output = [row for row in client_schematics if int(row["item_output_crit_id"]) > 0 or int(row["output_count_crit_bonus"]) > 0]
    schematics_with_additives = [row for row in client_schematics if int(row["max_additives"]) > 0]
    schematics_with_discovery = [
        row
        for row in client_schematics
        if int(row["discoverable_quadrant"]) > 0 or float(row["discoverable_radius"]) > 0.0 or float(row["discoverable_angle"]) > 0.0
    ]
    schematics_with_catalyst_ordering = [row for row in client_schematics if int(row["catalyst_ordering_id"]) > 0]
    materials_with_stat_revolution_item = [row for row in client_materials if int(row["stat_revolution_item_id"]) > 0]
    tiers_with_craft_xp = [row for row in client_tiers if int(row["craft_xp"]) > 0 or int(row["first_craft_xp"]) > 0]
    tiers_with_quest_xp = [row for row in client_tiers if int(row["quest_xp"]) > 0]
    tiers_with_relearn_cost = [row for row in client_tiers if int(row["relearn_cost"]) > 0]
    harvesting_with_prerequisites = [row for row in client_harvesting if int(row["prerequisite_id"]) > 0]
    harvesting_with_map_markers = [row for row in client_harvesting if int(row["minimap_marker_id"]) > 0]

    lines.append("## Client Tradeskill Data Details")
    lines.append("")
    lines.append(f"- Client Tradeskill rows: {len(client_tradeskills)}")
    lines.append(f"- Client TradeskillTier rows: {len(client_tiers)}")
    lines.append(f"- Client TradeskillSchematic2 rows: {len(client_schematics)}")
    lines.append(f"- Schematics with direct item output: {len(schematics_with_direct_output)}")
    lines.append(f"- Schematics with loot output: {len(schematics_with_loot_output)}")
    lines.append(f"- Schematics with material costs: {len(schematics_with_materials)}")
    lines.append(f"- Schematics with parent unlocks/chains: {len(schematics_with_parent)}")
    lines.append(f"- Schematics with fail output ids: {len(schematics_with_fail_output)}")
    lines.append(f"- Schematics with crit output or crit bonus: {len(schematics_with_crit_output)}")
    lines.append(f"- Schematics with additive slots: {len(schematics_with_additives)}")
    lines.append(f"- Schematics with discovery vectors: {len(schematics_with_discovery)}")
    lines.append(f"- Schematics with catalyst ordering: {len(schematics_with_catalyst_ordering)}")
    lines.append(f"- Materials with stat-revolution item ids: {len(materials_with_stat_revolution_item)}")
    lines.append(f"- Tiers with craft/first-craft XP data: {len(tiers_with_craft_xp)}")
    lines.append(f"- Tiers with quest XP data: {len(tiers_with_quest_xp)}")
    lines.append(f"- Tiers with relearn cost data: {len(tiers_with_relearn_cost)}")
    lines.append(f"- Harvesting rows with prerequisites: {len(harvesting_with_prerequisites)}")
    lines.append(f"- Harvesting rows with minimap markers: {len(harvesting_with_map_markers)}")
    lines.append("")

    player_text = read_text(root / "Source" / "NexusForever.Game" / "Entity" / "Player.cs")
    character_tradeskill_model_text = read_text(root / "Source" / "NexusForever.Database.Character" / "Model" / "CharacterTradeskillModel.cs")
    character_schematic_model_text = read_text(root / "Source" / "NexusForever.Database.Character" / "Model" / "CharacterSchematicModel.cs")
    character_context_text = read_text(root / "Source" / "NexusForever.Database.Character" / "CharacterContext.cs")
    tradeskill_info_text = read_text(root / "Source" / "NexusForever.Network.World" / "Message" / "Model" / "Shared" / "TradeskillInfo.cs")
    server_professions_load_text = read_text(root / "Source" / "NexusForever.Network.World" / "Message" / "Model" / "Crafting" / "ServerProfessionsLoad.cs")
    tradeskill_handler_text = read_text(root / "Source" / "NexusForever.WorldServer" / "Network" / "Message" / "Handler" / "Crafting" / "ClientTradeskillHandlers.cs")
    crafting_handler_text = read_text(root / "Source" / "NexusForever.WorldServer" / "Network" / "Message" / "Handler" / "Crafting" / "ClientCraftingCraftHandlers.cs")
    rune_handler_text = read_text(root / "Source" / "NexusForever.WorldServer" / "Network" / "Message" / "Handler" / "Crafting" / "ClientCraftingRuneHandlers.cs")
    supply_satchel_text = read_text(root / "Source" / "NexusForever.Game" / "Entity" / "SupplySatchelManager.cs")
    spell_handler_text = read_text(root / "Source" / "NexusForever.Game" / "Spell" / "SpellEffectHandler.cs")
    server_crafting_finish_text = read_text(root / "Source" / "NexusForever.Network.World" / "Message" / "Model" / "Crafting" / "ServerCraftingFinish.cs")

    tradeskill_surface_markers = {
        "Character tradeskill model persists xp/active/proficiency/talents": "public uint TradeskillXp" in character_tradeskill_model_text
        and "public uint IsActive" in character_tradeskill_model_text
        and "PropertyProficiencyFlags" in character_tradeskill_model_text
        and "TalentTier09" in character_tradeskill_model_text,
        "CharacterContext maps CharacterTradeskill rows": "CharacterTradeskillModel" in character_context_text
        and "entity.ToTable(\"character_tradeskill\")" in character_context_text,
        "Player loads and saves tradeskill state": "TradeskillState.FromModel" in player_text
        and "SaveTradeskills(context)" in player_text,
        "Player learn/drop/talent/reset methods update state": "LearnTradeskill" in player_text
        and "PickTradeskillTalent" in player_text
        and "ResetTradeskillTalents" in player_text,
        "ServerProfessionsLoad and ServerProfessionUpdate serialize profession state": "ServerProfessionsLoad" in player_text
        and "ServerProfessionUpdate" in player_text
        and "TradeskillInfo" in tradeskill_info_text,
        "Tradeskill handlers validate client Tradeskill and bonus/tier rows": "ValidateTradeskill" in tradeskill_handler_text
        and "Tradeskill.GetEntry" in tradeskill_handler_text
        and "TradeskillBonus.GetEntry" in tradeskill_handler_text
        and "TradeskillTalentTier.GetEntry" in tradeskill_handler_text,
        "Craft handlers validate schematic rows": "TradeskillSchematic2.GetEntry" in crafting_handler_text,
        "Craft handlers require active schematic tradeskill": "HasTradeskill(requiredTradeskill)" in crafting_handler_text
        and "schematic.TradeSkillId" in crafting_handler_text,
        "Craft handlers support direct item and loot outputs": "Item2IdOutput" in crafting_handler_text
        and "TryGenerateLoot" in crafting_handler_text
        and "GiveGeneratedLoot" in crafting_handler_text,
        "Craft handlers consume schematic materials from satchel/inventory": "GetMaterialRequirements" in crafting_handler_text
        and "SupplySatchelManager.RemoveAmount" in crafting_handler_text
        and "Inventory.ItemDelete" in crafting_handler_text,
        "Craft handlers validate and consume transient additive/catalyst items": "TryBuildCraftingModifierItemCounts" in crafting_handler_text
        and "ValidateCraftingAdditive" in rune_handler_text
        and "ValidateCraftingCatalyst" in rune_handler_text
        and "TradeskillAdditiveCost" in crafting_handler_text,
        "Supply satchel persists and sends material stack updates": "CharacterTradeskillMaterialModel" in supply_satchel_text
        and "ServerSupplySatchelUpdate" in supply_satchel_text,
        "Craft finish packet serializes success, crafted schematic, item, and earned XP fields": "TradeskillSchematic2IdCrafted" in server_crafting_finish_text
        and "Item2IdCrafted" in server_crafting_finish_text
        and "EarnedXp" in server_crafting_finish_text,
        "Fixed-recipe craft success reports non-discovery Success hot/cold state": "HotOrCold = CraftingDiscovery.Success" in crafting_handler_text
        and "HotOrCold" in server_crafting_finish_text
        and "Success = 3" in read_text(root / "Source" / "NexusForever.Game.Static" / "Crafting" / "CraftingDiscovery.cs"),
        "Player persists and serializes learned/discovered schematic state": "CharacterSchematicModel" in character_schematic_model_text
        and "SaveSchematics(context)" in player_text
        and "LearnedSchematics = learnedSchematics" in player_text
        and "LearnedSchematicDiscoveredFlags" in player_text,
        "GiveSchematic spell records durable learned-schematic state": "SpellEffectType.GiveSchematic" in spell_handler_text
        and "LearnSchematic" in spell_handler_text,
        "Rune handlers validate rune/item requests and send sigil results": "ClientCraftingRuneInstallHandler" in rune_handler_text
        and "ValidateRuneType" in rune_handler_text
        and "ServerTradeskillSigilResult" in rune_handler_text,
    }

    lines.append("## Server Tradeskill Runtime Surface")
    lines.append("")
    lines.append("| Implementation surface | Present | Result |")
    lines.append("| --- | --- | --- |")

    for marker, present in tradeskill_surface_markers.items():
        if not present:
            failures.append(f"server tradeskill runtime surface missing marker: {marker}")
        lines.append(f"| {marker} | {'yes' if present else 'no'} | {'PASS' if present else 'FAIL'} |")

    if "LearnedSchematics = learnedSchematics" not in player_text and "LearnedSchematics" in server_professions_load_text:
        warnings.append("learned/discovered schematic state has packet models, but Player does not persist or populate it")
    if "packet-only" in spell_handler_text:
        warnings.append("GiveSchematic currently notifies the client and quest objective only; durable learned schematic storage remains blocked")
    if tiers_with_craft_xp and "EarnedXp =" not in crafting_handler_text:
        warnings.append("TradeskillTier rows include craft/first-craft XP, but crafting success does not award or serialize earned tradeskill XP")
    if tiers_with_quest_xp and "QuestRewardType.TradeSkillXp" not in read_text(root / "Source" / "NexusForever.Game" / "Entity" / "QuestManager.cs"):
        warnings.append("TradeskillTier rows include quest XP values; Quest2Reward TradeSkillXp remains blocked until XP mutation semantics are mapped")
    if tiers_with_relearn_cost and "relearnCost" not in player_text:
        warnings.append("TradeskillTier rows include relearn costs, but reset/relearn cost and cooldown semantics are not mapped")
    if schematics_with_fail_output or schematics_with_crit_output:
        warnings.append("TradeskillSchematic2 rows include fail/crit outputs; current fixed-recipe handler only returns normal direct/loot output")
    if schematics_with_discovery:
        warnings.append("TradeskillSchematic2 rows include discovery vectors; fixed-recipe finish now reports Success, but hot/cold discovery math and unlock semantics remain blocked")
    additive_consumption_covered = (
        "TryBuildCraftingModifierItemCounts" in crafting_handler_text
        and "ValidateCraftingAdditive" in rune_handler_text
        and "TradeskillAdditiveCost" in crafting_handler_text
    )
    if (schematics_with_additives or schematics_with_catalyst_ordering) and not additive_consumption_covered:
        warnings.append("client additive/catalyst data exists; current additive handler stores transient request state and does not feed fixed-recipe output math")
    elif schematics_with_additives or schematics_with_catalyst_ordering:
        warnings.append("client additive/catalyst data exists; item validation/consumption is covered, but additive/catalyst output math remains blocked")
    if client_harvesting and "TradeskillHarvestingInfo" not in player_text + crafting_handler_text:
        warnings.append("client harvesting rows exist, but harvesting runtime behavior is not mapped to server code")
    if "Dictionary<ulong, List<RuneSlotState>>" in rune_handler_text:
        warnings.append("rune slot/additive state is currently in-memory handler state; item rune persistence remains blocked")

    lines.append("")
    lines.append(
        "- Result: INFO, profession persistence, learn/drop/talent/reset, fixed recipe crafting, material debits, additive/catalyst item validation and consumption, satchel updates, learned schematic state, craft/quest XP, non-discovery craft-finish Success hot/cold state, and rune packet surfaces are covered; hot/cold discovery math/unlocks, harvesting, additive/catalyst output math, fail/crit outputs, and durable rune state remain blocked."
    )
    lines.append("")

    return lines, failures, warnings


def build_lore_report(archive_dir: Path, client_sql_dir: Path, root: Path) -> tuple[list[str], list[str], list[str]]:
    failures: list[str] = []
    warnings: list[str] = []
    lines: list[str] = []

    wiki_facts = parse_wiki_lore_facts(archive_dir)
    datacube_rows = parse_client_datacube_rows(client_sql_dir)
    datacube_volume_rows = parse_client_datacube_volume_rows(client_sql_dir)
    archive_article_rows = parse_client_archive_article_rows(client_sql_dir)
    archive_entry_rows = parse_client_archive_entry_rows(client_sql_dir)
    archive_unlock_rule_rows = parse_client_archive_unlock_rule_rows(client_sql_dir)
    story_panel_rows = parse_client_story_panel_rows(client_sql_dir)

    wiki_markers = {
        "Lore page links Galactic Archive and Zone Lore": bool(wiki_facts["lore_page_links_archive_and_zone_lore"]),
        "Codex page lists log panes": bool(wiki_facts["codex_page_lists_logs"]),
        "Datacube page documents gameplay and Scientist path use": bool(wiki_facts["datacube_page_documents_gameplay"]),
        "Galactic Archive page documents unlockable articles": bool(wiki_facts["archive_page_documents_unlocks"]),
        "Zone Lore pages exist": int(wiki_facts["zone_lore_pages"]) > 0,
        "Datacube entry pages exist": int(wiki_facts["datacube_entry_pages"]) > 0,
        "Galactic Archive topic rows exist": int(wiki_facts["archive_topic_rows"]) > 0,
    }

    lines.append("## Lore Wiki Coverage")
    lines.append("")
    lines.append("| Wiki fact | Present | Result |")
    lines.append("| --- | --- | --- |")

    for marker, present in wiki_markers.items():
        if not present:
            failures.append(f"wiki lore fact missing marker: {marker}")
        lines.append(f"| {marker} | {'yes' if present else 'no'} | {'PASS' if present else 'FAIL'} |")

    lines.append(f"| Zone Lore pages found | {int(wiki_facts['zone_lore_pages'])} | PASS |")
    lines.append(f"| Datacube entry pages found | {int(wiki_facts['datacube_entry_pages'])} | PASS |")
    lines.append(f"| Datacube decryption pages found | {int(wiki_facts['datacube_decryption_pages'])} | PASS |")
    lines.append(f"| Galactic Archive topic rows found | {int(wiki_facts['archive_topic_rows'])} | PASS |")
    lines.append("")

    table_counts = {
        table_name: count_client_table_rows(client_sql_dir, table_name)
        for table_name in LORE_CORE_TABLES
    }

    lines.append("## Client Lore Table Coverage")
    lines.append("")
    lines.append("| Client table | Rows | Result |")
    lines.append("| --- | --- | --- |")

    for table_name, row_count in table_counts.items():
        if row_count == 0:
            failures.append(f"client lore table has no rows: {table_name}")
        lines.append(f"| {table_name} | {row_count} | {'PASS' if row_count else 'FAIL'} |")

    lines.append("")

    datacube_types = Counter(int(row["type"]) for row in datacube_rows)
    datacubes_with_audio = [row for row in datacube_rows if int(row["sound_event_id"]) > 0]
    datacubes_with_zone = [row for row in datacube_rows if int(row["world_zone_id"]) > 0]
    datacubes_with_location = [row for row in datacube_rows if int(row["world_location_id"]) > 0]
    datacubes_with_unlock_count = [row for row in datacube_rows if int(row["unlock_count"]) > 0]
    datacubes_with_direction = [row for row in datacube_rows if int(row["quest_direction_id"]) > 0]
    volume_datacube_refs = sum(len(row["datacube_ids"]) for row in datacube_volume_rows)
    archive_articles_with_entries = [row for row in archive_article_rows if row["entry_ids"]]
    archive_articles_with_title_rewards = [row for row in archive_article_rows if int(row["character_title_id_reward"]) > 0]
    archive_entries_with_scientist_text = [row for row in archive_entry_rows if row["scientist_text_ids"]]
    archive_entries_with_title_rewards = [row for row in archive_entry_rows if int(row["character_title_id_reward"]) > 0]
    archive_unlock_rule_types = Counter(int(row["type"]) for row in archive_unlock_rule_rows)
    story_panels_with_sound = [row for row in story_panel_rows if int(row["sound_event_id"]) > 0]
    story_panels_with_prereq = [row for row in story_panel_rows if int(row["prerequisite_id"]) > 0]

    lines.append("## Client Lore Data Details")
    lines.append("")
    lines.append(f"- Client Datacube rows: {len(datacube_rows)}")
    lines.append(f"- Datacube type ids: {', '.join(f'{key}:{value}' for key, value in sorted(datacube_types.items()))}")
    lines.append(f"- Datacubes with audio events: {len(datacubes_with_audio)}")
    lines.append(f"- Datacubes with world-zone ids: {len(datacubes_with_zone)}")
    lines.append(f"- Datacubes with world-location ids: {len(datacubes_with_location)}")
    lines.append(f"- Datacubes with unlock counts: {len(datacubes_with_unlock_count)}")
    lines.append(f"- Datacubes with quest-direction ids: {len(datacubes_with_direction)}")
    lines.append(f"- DatacubeVolume rows: {len(datacube_volume_rows)}")
    lines.append(f"- DatacubeVolume datacube references: {volume_datacube_refs}")
    lines.append(f"- ArchiveArticle rows: {len(archive_article_rows)}")
    lines.append(f"- ArchiveArticle rows with entry refs: {len(archive_articles_with_entries)}")
    lines.append(f"- ArchiveArticle rows with title rewards: {len(archive_articles_with_title_rewards)}")
    lines.append(f"- ArchiveEntry rows: {len(archive_entry_rows)}")
    lines.append(f"- ArchiveEntry rows with Scientist text: {len(archive_entries_with_scientist_text)}")
    lines.append(f"- ArchiveEntry rows with title rewards: {len(archive_entries_with_title_rewards)}")
    lines.append(f"- ArchiveEntryUnlockRule type ids: {', '.join(f'{key}:{value}' for key, value in sorted(archive_unlock_rule_types.items()))}")
    lines.append(f"- StoryPanel rows: {len(story_panel_rows)}")
    lines.append(f"- StoryPanel rows with sound events: {len(story_panels_with_sound)}")
    lines.append(f"- StoryPanel rows with prerequisites: {len(story_panels_with_prereq)}")
    lines.append("")

    datacube_manager_text = read_text(root / "Source" / "NexusForever.Game" / "Entity" / "DatacubeManager.cs")
    datacube_text = read_text(root / "Source" / "NexusForever.Game" / "Entity" / "Datacube.cs")
    simple_entity_text = read_text(root / "Source" / "NexusForever.Game" / "Entity" / "SimpleEntity.cs")
    character_datacube_model_text = read_text(root / "Source" / "NexusForever.Database.Character" / "Model" / "CharacterDatacubeModel.cs")
    character_archive_model_text = read_text(root / "Source" / "NexusForever.Database.Character" / "Model" / "CharacterGalacticArchiveModel.cs")
    character_context_text = read_text(root / "Source" / "NexusForever.Database.Character" / "CharacterContext.cs")
    archive_manager_text = read_text(root / "Source" / "NexusForever.Game" / "Entity" / "GalacticArchiveManager.cs")
    archive_unlock_rule_text = read_text(root / "Source" / "NexusForever.Game" / "Entity" / "GalacticArchiveUnlockRule.cs")
    server_datacube_update_list_text = read_text(root / "Source" / "NexusForever.Network.World" / "Message" / "Model" / "ServerDatacubeUpdateList.cs")
    shared_datacube_text = read_text(root / "Source" / "NexusForever.Network.World" / "Message" / "Model" / "Shared" / "Datacube.cs")
    archive_unlock_handler_text = read_text(root / "Source" / "NexusForever.WorldServer" / "Network" / "Message" / "Handler" / "GalacticArchive" / "ClientGalacticArchiveUnlockHandler.cs")
    archive_viewed_handler_text = read_text(root / "Source" / "NexusForever.WorldServer" / "Network" / "Message" / "Handler" / "GalacticArchive" / "ClientGalacticArchiveViewedHandler.cs")
    server_archive_update_text = read_text(root / "Source" / "NexusForever.Network.World" / "Message" / "Model" / "GalacticArchive" / "ServerGalacticArchiveUpdate.cs")
    server_archive_refresh_text = read_text(root / "Source" / "NexusForever.Network.World" / "Message" / "Model" / "GalacticArchive" / "ServerGalacticArchiveRefresh.cs")
    path_manager_text = read_text(root / "Source" / "NexusForever.Game" / "Entity" / "PathManager.cs")
    story_builder_text = read_text(root / "Source" / "NexusForever.Game" / "Story" / "StoryBuilder.cs")
    story_message_builder_text = read_text(root / "Source" / "NexusForever.Game" / "Story" / "StoryMessageBuilder.cs")
    archive_chat_formatter_text = read_text(root / "Source" / "NexusForever.Game" / "Chat" / "Format" / "Formatter" / "ArchiveArticleChatFormatter.cs")

    lore_surface_markers = {
        "Character datacube model persists type/id/progress": "public byte Type" in character_datacube_model_text
        and "public ushort Datacube" in character_datacube_model_text
        and "public uint Progress" in character_datacube_model_text,
        "CharacterContext maps CharacterDatacube rows": "CharacterDatacubeModel" in character_context_text
        and "entity.ToTable(\"character_datacube\")" in character_context_text,
        "DatacubeManager validates Datacube and DatacubeVolume rows": "GameTableManager.Instance.Datacube.GetEntry" in datacube_manager_text
        and "GameTableManager.Instance.DatacubeVolume.GetEntry" in datacube_manager_text,
        "DatacubeManager idempotently merges repeated progress": "existingDatacube.Progress |= progress" in datacube_manager_text,
        "DatacubeManager sends initial datacube and volume packets": "ServerDatacubeUpdateList" in datacube_manager_text
        and "DatacubeVolumeData" in server_datacube_update_list_text,
        "Datacube update packets write 14-bit ids and progress": "writer.Write(DatacubeId, 14u)" in shared_datacube_text
        and "writer.Write(Progress)" in shared_datacube_text,
        "SimpleEntity activation grants datacubes and journal volumes": "CreatureEntry.DatacubeId" in simple_entity_text
        and "CreatureEntry.DatacubeVolumeId" in simple_entity_text
        and "OnActivateCast" in simple_entity_text,
        "SimpleEntity datacube reveals advance Scientist datacube-discovery missions": "CompleteCurrentScientistDatacubeDiscoveryMission" in simple_entity_text
        and "PathScientistDatacubeDiscovery" in path_manager_text,
        "Datacube save tracks create/progress": "DatacubeSaveMask.Create" in datacube_text
        and "DatacubeSaveMask.Progress" in datacube_text,
        "Galactic Archive client unlock/view packets are parsed": "ClientGalacticArchiveUnlock" in archive_unlock_handler_text
        and "ClientGalacticArchiveViewed" in archive_viewed_handler_text,
        "Galactic Archive update/refresh packets exist": "ServerGalacticArchiveUpdate" in server_archive_update_text
        and "UnlockedFlags" in server_archive_update_text
        and "ServerGalacticArchiveRefresh" in server_archive_refresh_text,
        "Galactic Archive state persists unlock/view flags": "CharacterGalacticArchiveModel" in character_archive_model_text
        and "entity.ToTable(\"character_galactic_archive\")" in character_context_text
        and "ServerGalacticArchiveUpdate" in archive_manager_text
        and "MarkArticleViewed" in archive_manager_text,
        "Galactic Archive rule unlocks and title rewards have server hooks": "ArchiveEntryUnlockRule" in archive_manager_text
        and "GalacticArchiveUnlockRule.IsSatisfied" in archive_manager_text
        and "ArchiveEntryUnlockRuleEnum switch" in archive_unlock_rule_text
        and "RefreshRuleUnlocks" in archive_manager_text
        and "TitleManager.AddTitle" in archive_manager_text,
        "StoryPanel rows can drive story panels": "StoryPanel.GetEntry" in story_builder_text
        and "ServerStoryPanelShow" in story_builder_text,
        "Story messages support creature/localised/player actors": "CreatureActor" in story_message_builder_text
        and "LocalisedTextActor" in story_message_builder_text
        and "PlayerSelfActor" in story_message_builder_text,
        "Archive article chat formatter preserves article ids": "ArchiveArticleId" in archive_chat_formatter_text
        and "ChatFormatArchiveArticle" in archive_chat_formatter_text,
    }

    lines.append("## Server Lore Runtime Surface")
    lines.append("")
    lines.append("| Implementation surface | Present | Result |")
    lines.append("| --- | --- | --- |")

    for marker, present in lore_surface_markers.items():
        if not present:
            failures.append(f"server lore runtime surface missing marker: {marker}")
        lines.append(f"| {marker} | {'yes' if present else 'no'} | {'PASS' if present else 'FAIL'} |")

    if "GalacticArchiveManager.UnlockArticle" not in archive_unlock_handler_text:
        warnings.append("Galactic Archive unlock requests are diagnostic-only; archive state persistence and trusted unlock rules remain blocked")
    if "GalacticArchiveManager.MarkArticleViewed" not in archive_viewed_handler_text:
        warnings.append("Galactic Archive viewed requests are diagnostic-only; viewed flags are not persisted or echoed")
    if archive_unlock_rule_rows and (
        "GalacticArchiveUnlockRule.IsSatisfied" not in archive_manager_text
        or "ArchiveEntryUnlockRuleEnum switch" not in archive_unlock_rule_text
    ):
        warnings.append("ArchiveEntryUnlockRule rows exist, but unlock rule types/objects are not mapped to server-side progression")
    if archive_unlock_rule_rows and "1u => MatchObjects(rule, isPathMissionComplete)" in archive_unlock_rule_text and "HasCompletedPathMission" not in archive_manager_text:
        warnings.append("ArchiveEntryUnlockRule type 1 maps to PathMission ids, but server path-mission completion/progress persistence remains blocked; client 0x00F9 path progress reports carry the PathMission ObjectId/PowerMap id, not trusted completion state")
    if (archive_articles_with_title_rewards or archive_entries_with_title_rewards) and "TitleManager.AddTitle" not in archive_manager_text:
        warnings.append("ArchiveArticle/ArchiveEntry title rewards exist in client data; archive reward grants remain blocked")
    if datacubes_with_unlock_count:
        warnings.append("Datacube rows include unlock-count fields; server currently tracks progress flags but not client unlock-count semantics")
    if datacubes_with_direction:
        warnings.append("Datacube rows include quest-direction ids; activation routing/waypoint behavior remains blocked")
    if table_counts["PathScientistDatacubeDiscovery"] and (
        "CompleteCurrentScientistDatacubeDiscoveryMission" not in simple_entity_text
        or "PathScientistDatacubeDiscovery" not in path_manager_text
    ):
        warnings.append("PathScientistDatacubeDiscovery rows exist, but Scientist datacube discovery mission completion is not mapped to runtime code")
    if story_panels_with_prereq and "PrerequisiteManager" not in story_builder_text:
        warnings.append("StoryPanel rows include prerequisites; StoryBuilder does not enforce prerequisite ids")

    lines.append("")
    lines.append(
        "- Result: INFO, datacube/journal persistence, activation, progress packets, story-panel display, Galactic Archive unlock/view persistence, achievement and quest archive rules, archive title rewards, archive packet models, and Scientist discovery mission advancement are covered; PathMission-backed type-1 archive rules remain blocked on server-owned mission progress/completion state, while StoryPanel prerequisites and some datacube routing semantics remain blocked."
    )
    lines.append("")

    return lines, failures, warnings


def build_report(archive_dir: Path, client_sql_dir: Path, root: Path, domains: Iterable[str]) -> tuple[str, bool]:
    selected_domains = tuple(dict.fromkeys(domains))
    unknown_domains = sorted(set(selected_domains) - set(IMPLEMENTED_AUDIT_DOMAINS))
    if unknown_domains:
        raise ValueError(f"unsupported audit domain(s): {', '.join(unknown_domains)}")

    failures: list[str] = []
    warnings: list[str] = []
    lines: list[str] = []

    lines.append("# WildStar Wiki Archive Audit")
    lines.append("")
    lines.append(f"- Archive: `{archive_dir}`")
    lines.append(f"- Client SQL: `{client_sql_dir}`")
    lines.append(f"- Domains: {format_set(selected_domains)}")
    lines.append("")

    for domain in selected_domains:
        if domain == "character-creation":
            domain_lines, domain_failures, domain_warnings = build_character_creation_report(archive_dir, client_sql_dir, root)
        elif domain == "abilities":
            domain_lines, domain_failures, domain_warnings = build_abilities_report(archive_dir, client_sql_dir, root)
        elif domain == "amps":
            domain_lines, domain_failures, domain_warnings = build_amps_report(archive_dir, client_sql_dir, root)
        elif domain == "housing":
            domain_lines, domain_failures, domain_warnings = build_housing_report(archive_dir, client_sql_dir, root)
        elif domain == "quests":
            domain_lines, domain_failures, domain_warnings = build_quests_report(archive_dir, client_sql_dir, root)
        elif domain == "achievements":
            domain_lines, domain_failures, domain_warnings = build_achievements_report(archive_dir, client_sql_dir, root)
        elif domain == "tradeskills":
            domain_lines, domain_failures, domain_warnings = build_tradeskills_report(archive_dir, client_sql_dir, root)
        elif domain == "lore":
            domain_lines, domain_failures, domain_warnings = build_lore_report(archive_dir, client_sql_dir, root)
        else:
            raise ValueError(f"unsupported audit domain: {domain}")

        lines.extend(domain_lines)
        failures.extend(domain_failures)
        warnings.extend(domain_warnings)

    lines.append("## Summary")
    lines.append("")
    if failures:
        lines.append("Result: FAIL")
        for failure in failures:
            lines.append(f"- {failure}")
    else:
        lines.append("Result: PASS")
    if warnings:
        lines.append("")
        lines.append("Warnings:")
        for warning in warnings:
            lines.append(f"- {warning}")

    return "\n".join(lines) + "\n", not failures


def parse_args() -> argparse.Namespace:
    root = repo_root()
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--archive-dir", type=Path, default=root / "artifacts" / "wildstar-fandom-wiki-2026-05-18")
    parser.add_argument("--client-sql-dir", type=Path, default=root / "wildstar_client_mysql")
    parser.add_argument(
        "--domain",
        action="append",
        choices=sorted(IMPLEMENTED_AUDIT_DOMAINS),
        dest="domains",
        help="Audit domain to run. May be supplied more than once. Defaults to all implemented domains.",
    )
    parser.add_argument("--list-domains", action="store_true", help="List implemented and planned audit domains, then exit.")
    parser.add_argument("--output", type=Path)
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    root = repo_root()

    if args.list_domains:
        sys.stdout.write(format_domain_listing())
        return 0

    domains = tuple(args.domains) if args.domains else DEFAULT_AUDIT_DOMAINS
    archive_dir = args.archive_dir.resolve()
    client_sql_dir = args.client_sql_dir.resolve()

    try:
        validate_input_paths(archive_dir, client_sql_dir, root)
        report, ok = build_report(archive_dir, client_sql_dir, root, domains)
    except (FileNotFoundError, ValueError) as ex:
        print(f"error: {ex}", file=sys.stderr)
        return 2

    if args.output:
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text(report, encoding="utf-8")
    else:
        sys.stdout.write(report)

    return 0 if ok else 1


if __name__ == "__main__":
    raise SystemExit(main())
