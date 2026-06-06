from __future__ import annotations

import argparse
import json
import os
import re
import zipfile
from collections import Counter, defaultdict
from dataclasses import dataclass, field
from pathlib import Path
from typing import Iterable


DEFAULT_ADDON_DIR = Path(os.environ.get("WILDSTAR_ADDON_CORPUS", r"I:\Wildstar Addons"))
DEFAULT_OUTPUT_DIR = Path("artifacts/addon-corpus-audit")

API_NAMESPACES = (
    "Apollo",
    "GameLib",
    "GroupLib",
    "MatchingGameLib",
    "MatchMakingLib",
    "HousingLib",
    "MailSystemLib",
    "MarketplaceLib",
    "CraftingLib",
    "ChallengesLib",
    "PublicEventLib",
    "GuildLib",
    "ICCommLib",
)

API_PATTERN = re.compile(
    r"\b(" + "|".join(re.escape(namespace) for namespace in API_NAMESPACES) + r")\.([A-Za-z_][A-Za-z0-9_]*)"
)
EVENT_PATTERNS = (
    re.compile(r"\bApollo\.RegisterEventHandler\s*\(\s*['\"]([^'\"]+)['\"]"),
    re.compile(r"\bApollo\.FireEvent\s*\(\s*['\"]([^'\"]+)['\"]"),
)

CATEGORY_PATTERNS: dict[str, tuple[tuple[str, re.Pattern[str]], ...]] = {
    "Challenge": (
        ("Challenge", re.compile(r"\bChallenge[A-Za-z_]*\b", re.IGNORECASE)),
        ("ChallengesLib", re.compile(r"\bChallengesLib\.", re.IGNORECASE)),
    ),
    "Crafting": (
        ("CraftingLib", re.compile(r"\bCraftingLib\.", re.IGNORECASE)),
        ("Schematic", re.compile(r"\bSchematic\b", re.IGNORECASE)),
        ("Tradeskill", re.compile(r"\bTradeskill\b", re.IGNORECASE)),
        ("Rune", re.compile(r"\bRune[A-Za-z_]*\b", re.IGNORECASE)),
    ),
    "Events": (
        ("RegisterEventHandler", re.compile(r"\bRegisterEventHandler\s*\(", re.IGNORECASE)),
        ("FireEvent", re.compile(r"\bApollo\.FireEvent\s*\(", re.IGNORECASE)),
    ),
    "GameLib": (
        ("GameLib", re.compile(r"\bGameLib\.", re.IGNORECASE)),
    ),
    "Group": (
        ("GroupLib", re.compile(r"\bGroupLib\.", re.IGNORECASE)),
        ("Raid", re.compile(r"\bRaid[A-Za-z_]*\b", re.IGNORECASE)),
        ("ReadyCheck", re.compile(r"\bReadyCheck\b", re.IGNORECASE)),
    ),
    "Housing": (
        ("HousingLib", re.compile(r"\bHousingLib\.", re.IGNORECASE)),
        ("Residence", re.compile(r"\bResidence[A-Za-z_]*\b", re.IGNORECASE)),
        ("Decor", re.compile(r"\bDecor[A-Za-z_]*\b", re.IGNORECASE)),
    ),
    "Loot": (
        ("LootRoll", re.compile(r"\bLootRoll[A-Za-z_]*\b", re.IGNORECASE)),
        ("MasterLoot", re.compile(r"\bMasterLoot[A-Za-z_]*\b", re.IGNORECASE)),
    ),
    "Mail": (
        ("MailSystemLib", re.compile(r"\bMailSystemLib\.", re.IGNORECASE)),
        ("Mailbox", re.compile(r"\bMail[Bb]ox\b", re.IGNORECASE)),
        ("TakeAll", re.compile(r"\bTakeAll\b", re.IGNORECASE)),
    ),
    "Market": (
        ("MarketplaceLib", re.compile(r"\bMarketplaceLib\.", re.IGNORECASE)),
        ("Auction", re.compile(r"\bAuction[A-Za-z_]*\b", re.IGNORECASE)),
        ("Commodity", re.compile(r"\bCommodit(?:y|ies)\b", re.IGNORECASE)),
        ("CREDD", re.compile(r"\bCREDD\b", re.IGNORECASE)),
    ),
    "Matching": (
        ("MatchingGameLib", re.compile(r"\bMatchingGameLib\.", re.IGNORECASE)),
        ("MatchMakingLib", re.compile(r"\bMatchMakingLib\.", re.IGNORECASE)),
        ("MatchMaker", re.compile(r"\bMatchMaker\b", re.IGNORECASE)),
        ("GroupFinder", re.compile(r"\bGroupFinder\b", re.IGNORECASE)),
        ("Queue", re.compile(r"\bQueue\b", re.IGNORECASE)),
        ("VoteKick", re.compile(r"\bVoteKick\b", re.IGNORECASE)),
        ("Surrender", re.compile(r"\bSurrender\b", re.IGNORECASE)),
    ),
    "Network": (
        ("ICCommLib", re.compile(r"\bICCommLib\.", re.IGNORECASE)),
        ("InterAddon", re.compile(r"\bInterAddon\b", re.IGNORECASE)),
        ("JoinChannel", re.compile(r"\bJoinChannel\s*\(", re.IGNORECASE)),
    ),
}


@dataclass
class CorpusScan:
    source: Path
    archives: int = 0
    archives_with_lua: int = 0
    inventory: list[str] = field(default_factory=list)
    errors: list[str] = field(default_factory=list)
    category_addons: dict[str, set[str]] = field(default_factory=lambda: defaultdict(set))
    category_tokens: dict[str, Counter[str]] = field(default_factory=lambda: defaultdict(Counter))
    api_addons: dict[str, set[str]] = field(default_factory=lambda: defaultdict(set))
    event_addons: dict[str, set[str]] = field(default_factory=lambda: defaultdict(set))


def main() -> int:
    parser = argparse.ArgumentParser(description="Audit a WildStar addon zip corpus.")
    parser.add_argument("--addon-dir", type=Path, default=DEFAULT_ADDON_DIR, help="Directory containing addon .zip archives.")
    parser.add_argument("--output-dir", type=Path, default=DEFAULT_OUTPUT_DIR, help="Directory for generated reports.")
    parser.add_argument("--max-addons", type=int, default=0, help="Optional smoke limit for local testing.")
    args = parser.parse_args()

    addon_dir = args.addon_dir.resolve()
    output_dir = args.output_dir.resolve()

    if not addon_dir.exists():
        raise SystemExit(f"Addon directory not found: {addon_dir}")

    scan = scan_corpus(addon_dir, args.max_addons)
    if scan.archives == 0:
        raise SystemExit(f"No .zip archives found below {addon_dir}")

    output_dir.mkdir(parents=True, exist_ok=True)
    write_outputs(scan, output_dir)
    print(f"Scanned {scan.archives} archives ({scan.archives_with_lua} with Lua).")
    print(f"Reports written to {output_dir}")
    return 0


def scan_corpus(addon_dir: Path, max_addons: int) -> CorpusScan:
    scan = CorpusScan(source=addon_dir)
    zip_paths = sorted(addon_dir.rglob("*.zip"), key=lambda path: path.name.lower())
    if max_addons > 0:
        zip_paths = zip_paths[:max_addons]

    scan.archives = len(zip_paths)
    for zip_path in zip_paths:
        addon_name = zip_path.stem
        scan.inventory.append(addon_name)
        scan_archive(zip_path, addon_name, scan)

    return scan


def scan_archive(zip_path: Path, addon_name: str, scan: CorpusScan) -> None:
    addon_categories: set[str] = set()
    addon_category_tokens: dict[str, set[str]] = defaultdict(set)
    addon_apis: set[str] = set()
    addon_events: set[str] = set()

    try:
        with zipfile.ZipFile(zip_path) as archive:
            lua_members = [
                member
                for member in archive.infolist()
                if not member.is_dir() and member.filename.lower().endswith(".lua")
            ]

            if lua_members:
                scan.archives_with_lua += 1

            for member in lua_members:
                try:
                    text = decode_lua(archive.read(member))
                except Exception as exc:  # noqa: BLE001 - report and continue over mixed corpus encodings.
                    scan.errors.append(f"{zip_path.name}:{member.filename}: {exc}")
                    continue

                scan_text(text, addon_categories, addon_category_tokens, addon_apis, addon_events)
    except (OSError, zipfile.BadZipFile) as exc:
        scan.errors.append(f"{zip_path.name}: {exc}")
        return

    for category in addon_categories:
        scan.category_addons[category].add(addon_name)

    for category, tokens in addon_category_tokens.items():
        for token in tokens:
            scan.category_tokens[category][token] += 1

    for api in addon_apis:
        scan.api_addons[api].add(addon_name)

    for event in addon_events:
        scan.event_addons[event].add(addon_name)


def decode_lua(payload: bytes) -> str:
    for encoding in ("utf-8-sig", "utf-8", "cp1252", "latin-1"):
        try:
            return payload.decode(encoding)
        except UnicodeDecodeError:
            continue

    return payload.decode("utf-8", errors="replace")


def scan_text(
    text: str,
    addon_categories: set[str],
    addon_category_tokens: dict[str, set[str]],
    addon_apis: set[str],
    addon_events: set[str],
) -> None:
    for namespace, member in API_PATTERN.findall(text):
        addon_apis.add(f"{namespace}.{member}")

    for pattern in EVENT_PATTERNS:
        for match in pattern.findall(text):
            addon_events.add(match)

    for category, token_patterns in CATEGORY_PATTERNS.items():
        for token, pattern in token_patterns:
            if pattern.search(text):
                addon_categories.add(category)
                addon_category_tokens[category].add(token)


def write_outputs(scan: CorpusScan, output_dir: Path) -> None:
    (output_dir / "addon_inventory.txt").write_text("\n".join(scan.inventory) + "\n", encoding="utf-8")
    (output_dir / "addon_category_map.tsv").write_text(build_category_map(scan), encoding="utf-8")
    (output_dir / "addon_event_frequency.tsv").write_text(build_event_frequency(scan), encoding="utf-8")
    (output_dir / "addon_api_summary.json").write_text(
        json.dumps(build_api_summary(scan), indent=2, sort_keys=True) + "\n",
        encoding="utf-8",
    )
    (output_dir / "addon_apollo_api_scan.json").write_text(
        json.dumps(build_api_scan(scan), indent=2, sort_keys=True) + "\n",
        encoding="utf-8",
    )
    (output_dir / "addon-corpus-summary.md").write_text(build_markdown_report(scan), encoding="utf-8")


def build_category_map(scan: CorpusScan) -> str:
    lines = ["Addon\tCategories"]
    addon_categories: dict[str, list[str]] = defaultdict(list)
    for category, addons in scan.category_addons.items():
        for addon in addons:
            addon_categories[addon].append(category)

    for addon in sorted(scan.inventory, key=str.lower):
        categories = ",".join(sorted(addon_categories.get(addon, []))) or "(none)"
        lines.append(f"{addon}\t{categories}")

    return "\n".join(lines) + "\n"


def build_event_frequency(scan: CorpusScan) -> str:
    lines = ["Event\tAddons"]
    for event, addons in sorted(scan.event_addons.items(), key=lambda item: (-len(item[1]), item[0].lower())):
        lines.append(f"{event}\t{len(addons)}")

    return "\n".join(lines) + "\n"


def build_api_summary(scan: CorpusScan) -> dict[str, object]:
    return {
        "source": str(scan.source),
        "archives": scan.archives,
        "scannedWithLua": scan.archives_with_lua,
        "errors": len(scan.errors),
        "errorSamples": scan.errors[:20],
        "categoryCounts": {
            category: len(scan.category_addons.get(category, set()))
            for category in sorted(CATEGORY_PATTERNS)
        },
        "topTokens": {
            category: [token for token, _ in counter.most_common(20)]
            for category, counter in sorted(scan.category_tokens.items())
        },
        "topEvents": [
            {"Event": event, "Addons": len(addons)}
            for event, addons in sorted(scan.event_addons.items(), key=lambda item: (-len(item[1]), item[0].lower()))[:50]
        ],
    }


def build_api_scan(scan: CorpusScan) -> dict[str, list[dict[str, object]]]:
    grouped: dict[str, list[tuple[str, int]]] = defaultdict(list)
    for api, addons in scan.api_addons.items():
        namespace = api.split(".", 1)[0].lower()
        grouped[namespace].append((api, len(addons)))

    return {
        namespace: [
            {"Api": api, "Addons": count}
            for api, count in sorted(entries, key=lambda item: (-item[1], item[0].lower()))
        ]
        for namespace, entries in sorted(grouped.items())
    }


def build_markdown_report(scan: CorpusScan) -> str:
    lines: list[str] = []
    lines.append("# WildStar Addon Corpus Audit")
    lines.append("")
    lines.append(f"- Source: `{scan.source}`")
    lines.append(f"- Archives: {scan.archives}")
    lines.append(f"- Archives with Lua: {scan.archives_with_lua}")
    lines.append(f"- Decode/read errors: {len(scan.errors)}")
    lines.append("")
    lines.append("## Generated Files")
    lines.append("")
    for file_name in (
        "addon_api_summary.json",
        "addon_apollo_api_scan.json",
        "addon_category_map.tsv",
        "addon_event_frequency.tsv",
        "addon_inventory.txt",
    ):
        lines.append(f"- `{file_name}`")

    lines.append("")
    lines.append("## Category Coverage")
    lines.append("")
    lines.append("| Category | Addons | Top markers |")
    lines.append("| --- | ---: | --- |")
    for category in sorted(CATEGORY_PATTERNS):
        markers = ", ".join(token for token, _ in scan.category_tokens.get(category, Counter()).most_common(8))
        lines.append(f"| {category} | {len(scan.category_addons.get(category, set()))} | {markers} |")

    lines.append("")
    lines.append("## Top Apollo Events")
    lines.append("")
    lines.append("| Event | Addons |")
    lines.append("| --- | ---: |")
    for event, addons in sorted(scan.event_addons.items(), key=lambda item: (-len(item[1]), item[0].lower()))[:30]:
        lines.append(f"| `{event}` | {len(addons)} |")

    lines.append("")
    lines.append("## Top APIs By Namespace")
    for namespace, entries in build_api_scan(scan).items():
        lines.append("")
        lines.append(f"### {namespace}")
        lines.append("")
        lines.append("| API | Addons |")
        lines.append("| --- | ---: |")
        for row in entries[:15]:
            lines.append(f"| `{row['Api']}` | {row['Addons']} |")

    if scan.errors:
        lines.append("")
        lines.append("## Error Samples")
        lines.append("")
        for error in scan.errors[:20]:
            lines.append(f"- {error}")

    lines.append("")
    return "\n".join(lines)


if __name__ == "__main__":
    raise SystemExit(main())
