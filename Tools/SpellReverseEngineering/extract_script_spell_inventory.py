#!/usr/bin/env python3
"""
Extract explicit spell ids referenced by C# script files and optionally annotate
them from the local spell reverse-engineering database.

This is intended as a fallback when world creature spell linkage is incomplete in
`creature_spells`, but the content scripts still reference concrete Spell4 ids
through enum values, auto-attack arrays, and direct CastSpell(...) calls.
"""

from __future__ import annotations

import argparse
import pathlib
import re
import subprocess
import sys
from collections import defaultdict
from dataclasses import dataclass


ROOT = pathlib.Path(__file__).resolve().parents[2]
DEFAULT_SCRIPT_ROOT = ROOT / "Source" / "NexusForever.Script.Instance" / "Expedition" / "EvilFromTheEther"
DEFAULT_MYSQL = pathlib.Path(r"C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe")
SPELL_EFFECT_HANDLER = ROOT / "Source" / "NexusForever.Game" / "Spell" / "SpellEffectHandler.cs"

ENUM_SPELL_RE = re.compile(r"\benum\s+Spell\s*\{(?P<body>.*?)\}", re.DOTALL)
ENUM_MEMBER_RE = re.compile(r"(?P<name>[A-Za-z_][A-Za-z0-9_]*)\s*=\s*(?P<id>\d+)")
AUTO_ATTACK_RE = re.compile(r"\bautoAttacks\s*=\s*\[(?P<body>[^\]]*)\]", re.DOTALL)
CAST_LITERAL_RE = re.compile(r"\bCastSpell\(\s*(?P<id>\d+)\b")
INTEGER_RE = re.compile(r"\d+")
HANDLER_EFFECT_RE = re.compile(r"\[SpellEffectHandler\(SpellEffectType\.(?P<effect>[A-Za-z0-9_]+)\)\]")


@dataclass(frozen=True)
class SpellReference:
    spell_id: int
    file_path: pathlib.Path
    line_number: int
    kind: str
    symbol: str | None = None


@dataclass
class SpellMetadata:
    description: str | None = None
    effect_families: str | None = None
    proxy_targets: tuple[int, ...] = ()


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "paths",
        nargs="*",
        type=pathlib.Path,
        default=[DEFAULT_SCRIPT_ROOT],
        help="Files or directories to scan. Defaults to the Evil from the Ether script tree.",
    )
    parser.add_argument("--database", default="nexus_spell_re", help="MySQL database name.")
    parser.add_argument("--host", default="localhost", help="MySQL host.")
    parser.add_argument("--user", default="bankai", help="MySQL user.")
    parser.add_argument("--password", default="bankai", help="MySQL password.")
    parser.add_argument(
        "--mysql",
        type=pathlib.Path,
        default=DEFAULT_MYSQL if DEFAULT_MYSQL.exists() else pathlib.Path("mysql"),
        help="Path to mysql.exe.",
    )
    parser.add_argument(
        "--no-mysql",
        action="store_true",
        help="Skip database annotation and only print extracted spell ids.",
    )
    return parser.parse_args()


def iter_cs_files(paths: list[pathlib.Path]) -> list[pathlib.Path]:
    files: set[pathlib.Path] = set()
    for path in paths:
        resolved = path if path.is_absolute() else ROOT / path
        if resolved.is_file() and resolved.suffix.lower() == ".cs":
            files.add(resolved)
            continue

        if resolved.is_dir():
            for file_path in resolved.rglob("*.cs"):
                files.add(file_path)

    return sorted(files)


def line_number_for(text: str, offset: int) -> int:
    return text.count("\n", 0, offset) + 1


def extract_enum_spell_references(file_path: pathlib.Path, text: str) -> list[SpellReference]:
    references: list[SpellReference] = []
    for enum_match in ENUM_SPELL_RE.finditer(text):
        body = enum_match.group("body")
        enum_base = enum_match.start("body")
        for member_match in ENUM_MEMBER_RE.finditer(body):
            references.append(
                SpellReference(
                    spell_id=int(member_match.group("id")),
                    file_path=file_path,
                    line_number=line_number_for(text, enum_base + member_match.start()),
                    kind="enum",
                    symbol=member_match.group("name"),
                )
            )

    return references


def extract_auto_attack_references(file_path: pathlib.Path, text: str) -> list[SpellReference]:
    references: list[SpellReference] = []
    for array_match in AUTO_ATTACK_RE.finditer(text):
        body = array_match.group("body")
        body_base = array_match.start("body")
        for integer_match in INTEGER_RE.finditer(body):
            references.append(
                SpellReference(
                    spell_id=int(integer_match.group(0)),
                    file_path=file_path,
                    line_number=line_number_for(text, body_base + integer_match.start()),
                    kind="auto-attack",
                )
            )

    return references


def extract_cast_literal_references(file_path: pathlib.Path, text: str) -> list[SpellReference]:
    references: list[SpellReference] = []
    for cast_match in CAST_LITERAL_RE.finditer(text):
        references.append(
            SpellReference(
                spell_id=int(cast_match.group("id")),
                file_path=file_path,
                line_number=line_number_for(text, cast_match.start()),
                kind="cast-literal",
            )
        )

    return references


def extract_references(file_path: pathlib.Path) -> list[SpellReference]:
    text = file_path.read_text(encoding="utf-8", errors="ignore")
    references: list[SpellReference] = []
    references.extend(extract_enum_spell_references(file_path, text))
    references.extend(extract_auto_attack_references(file_path, text))
    references.extend(extract_cast_literal_references(file_path, text))
    return references


def mysql_args(args: argparse.Namespace) -> list[str]:
    command = [str(args.mysql), f"-u{args.user}", f"--host={args.host}", f"--database={args.database}", "--batch", "--raw"]
    if args.password:
        command.append(f"-p{args.password}")

    return command


def load_supported_effect_names() -> set[str]:
    text = SPELL_EFFECT_HANDLER.read_text(encoding="utf-8", errors="ignore")
    return {match.group("effect") for match in HANDLER_EFFECT_RE.finditer(text)}


def load_metadata_rows(args: argparse.Namespace, spell_ids: list[int]) -> dict[int, SpellMetadata]:
    if not spell_ids:
        return {}

    spell_list = ",".join(str(spell_id) for spell_id in spell_ids)
    query = (
        "SELECT s4.ID, COALESCE(s4.description, ''), "
        "COALESCE(GROUP_CONCAT(DISTINCT CONCAT(e.effectType, ':', et.effectName) ORDER BY e.effectType SEPARATOR ', '), '') "
        ", COALESCE(GROUP_CONCAT(DISTINCT CASE "
        "WHEN e.effectType IN (19, 25, 26, 94, 96) AND e.dataBits00 > 0 THEN e.dataBits00 "
        "ELSE NULL END ORDER BY e.dataBits00 SEPARATOR ','), '') "
        "FROM spell4 s4 "
        "LEFT JOIN spell4effects e ON e.spellId = s4.ID "
        "LEFT JOIN spell_effect_type_names et ON et.effectType = e.effectType "
        f"WHERE s4.ID IN ({spell_list}) "
        "GROUP BY s4.ID, s4.description "
        "ORDER BY s4.ID"
    )

    completed = subprocess.run(
        mysql_args(args) + ["-e", query],
        capture_output=True,
        text=True,
        check=False,
    )
    if completed.returncode != 0:
        raise RuntimeError(completed.stderr.strip() or "mysql query failed")

    metadata: dict[int, SpellMetadata] = {}
    lines = [line for line in completed.stdout.splitlines() if line.strip()]
    for row in lines[1:]:
        parts = row.split("\t")
        if len(parts) < 4:
            continue

        spell_id = int(parts[0])
        proxy_targets = tuple(int(value) for value in parts[3].split(",") if value)
        metadata[spell_id] = SpellMetadata(
            description=parts[1] or None,
            effect_families=parts[2] or None,
            proxy_targets=proxy_targets,
        )

    return metadata


def load_metadata(args: argparse.Namespace, spell_ids: list[int]) -> dict[int, SpellMetadata]:
    if args.no_mysql or not spell_ids:
        return {}

    metadata = load_metadata_rows(args, spell_ids)
    extra_spell_ids = sorted(
        {
            proxy_target
            for spell_metadata in metadata.values()
            for proxy_target in spell_metadata.proxy_targets
            if proxy_target not in metadata
        }
    )
    if extra_spell_ids:
        metadata.update(load_metadata_rows(args, extra_spell_ids))

    return metadata


def make_display_path(path: pathlib.Path) -> str:
    try:
        return str(path.relative_to(ROOT)).replace("\\", "/")
    except ValueError:
        return str(path)


def main() -> int:
    args = parse_args()
    files = iter_cs_files(args.paths)
    if not files:
        print("No C# files found.", file=sys.stderr)
        return 1

    references: list[SpellReference] = []
    for file_path in files:
        references.extend(extract_references(file_path))

    if not references:
        print("No spell references found.")
        return 0

    grouped: dict[int, list[SpellReference]] = defaultdict(list)
    for reference in references:
        grouped[reference.spell_id].append(reference)

    spell_ids = sorted(grouped)
    metadata = load_metadata(args, spell_ids)
    supported_effect_names = load_supported_effect_names()

    print(f"files={len(files)}")
    print(f"spell_ids={len(spell_ids)}")
    for spell_id in spell_ids:
        spell_metadata = metadata.get(spell_id, SpellMetadata())
        refs = sorted(grouped[spell_id], key=lambda reference: (make_display_path(reference.file_path), reference.line_number, reference.kind, reference.symbol or ""))
        files_summary = ", ".join(sorted({make_display_path(reference.file_path) for reference in refs}))
        kinds_summary = ", ".join(sorted({reference.kind for reference in refs}))
        description = spell_metadata.description or "<missing from spell4>"
        effect_families = spell_metadata.effect_families or "<no effect rows>"
        effect_names = [
            family.split(":", 1)[1].strip()
            for family in effect_families.split(",")
            if ":" in family
        ]
        unsupported_effects = sorted({name for name in effect_names if name not in supported_effect_names})
        proxy_targets = ",".join(str(proxy_target) for proxy_target in spell_metadata.proxy_targets) if spell_metadata.proxy_targets else ""
        unsupported_summary = ",".join(unsupported_effects) if unsupported_effects else ""
        print(
            f"spell4={spell_id}\tdescription={description}\teffects={effect_families}\trefs={len(refs)}\tkinds={kinds_summary}\tfiles={files_summary}"
            f"\tunsupported={unsupported_summary or '<none>'}\tproxyTargets={proxy_targets or '<none>'}"
        )
        for proxy_target in spell_metadata.proxy_targets:
            proxy_metadata = metadata.get(proxy_target, SpellMetadata())
            proxy_description = proxy_metadata.description or "<missing from spell4>"
            proxy_effects = proxy_metadata.effect_families or "<no effect rows>"
            print(f"    proxy-spell4={proxy_target}\tdescription={proxy_description}\teffects={proxy_effects}")
        for reference in refs:
            symbol = f"\tsymbol={reference.symbol}" if reference.symbol else ""
            print(f"  {make_display_path(reference.file_path)}:{reference.line_number}\tkind={reference.kind}{symbol}")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())