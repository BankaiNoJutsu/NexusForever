#!/usr/bin/env python3
"""Compare mapped entity coordinates with generated NexusForever terrain height."""

from __future__ import annotations

import argparse
import csv
import math
import struct
import sys
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable


MAGIC = 0x504D464E
VERSION = 4
BUILD = 16042
WORLD_GRID_COUNT = 128
WORLD_GRID_ORIGIN = WORLD_GRID_COUNT // 2
GRID_SIZE = 512
GRID_CELL_COUNT = 16

FLAG_ZONE = 0x01
FLAG_HEIGHT = 0x02
FLAG_AURA = 0x04
FLAG_LIQUID = 0x08
FLAG_ZONE_BOUND = 0x10
KNOWN_CELL_FLAGS = FLAG_ZONE | FLAG_HEIGHT | FLAG_AURA | FLAG_LIQUID | FLAG_ZONE_BOUND


@dataclass(frozen=True)
class HeightResult:
    height: float | None
    reason: str | None = None


@dataclass
class MapCellIndex:
    x: int
    z: int
    flags: int
    height_offset: int | None
    height_map: list[list[float]] | None = None

    def get_terrain_height(self, map_path: Path, x: float, z: float) -> HeightResult:
        if (self.flags & FLAG_ZONE) == 0:
            return HeightResult(None, "cell_has_no_zone")
        if (self.flags & FLAG_HEIGHT) == 0 or self.height_offset is None:
            return HeightResult(None, "cell_has_no_height")

        if self.height_map is None:
            with map_path.open("rb") as stream:
                stream.seek(self.height_offset)
                self.height_map = _read_height_map(stream)

        true_x = x + WORLD_GRID_ORIGIN * GRID_SIZE
        true_z = z + WORLD_GRID_ORIGIN * GRID_SIZE

        vertex_x = math.floor(true_x / 2.0)
        local_vertex_x = int(vertex_x) & 15
        vertex_z = math.floor(true_z / 2.0)
        local_vertex_z = int(vertex_z) & 15

        p1 = self.height_map[local_vertex_x + 1][local_vertex_z]
        p2 = self.height_map[local_vertex_x][local_vertex_z + 1]

        sq_x = (true_x / 2.0) - vertex_x
        sq_z = (true_z / 2.0) - vertex_z

        if sq_x + sq_z < 1.0:
            p0 = self.height_map[local_vertex_x][local_vertex_z]
            height = p0
            height += (p1 - p0) * sq_x
            height += (p2 - p0) * sq_z
        else:
            p3 = self.height_map[local_vertex_x + 1][local_vertex_z + 1]
            height = p3
            height += (p1 - p3) * (1.0 - sq_z)
            height += (p2 - p3) * (1.0 - sq_x)

        return HeightResult(height)


@dataclass
class MapGrid:
    x: int
    z: int
    cells: dict[tuple[int, int], MapCellIndex]

    def get_cell(self, x: float, z: float) -> MapCellIndex | None:
        cell_x = math.floor(GRID_CELL_COUNT * (WORLD_GRID_ORIGIN + x / GRID_SIZE))
        cell_z = math.floor(GRID_CELL_COUNT * (WORLD_GRID_ORIGIN + z / GRID_SIZE))
        if cell_x < 0 or cell_x >= WORLD_GRID_COUNT * GRID_CELL_COUNT:
            return None
        if cell_z < 0 or cell_z >= WORLD_GRID_COUNT * GRID_CELL_COUNT:
            return None

        return self.cells.get((int(cell_x) & (GRID_CELL_COUNT - 1), int(cell_z) & (GRID_CELL_COUNT - 1)))


class MapFile:
    def __init__(self, path: Path):
        self.path = path
        self.asset = ""
        self.grids: dict[tuple[int, int], MapGrid] = {}
        self._read()

    def get_terrain_height(self, x: float, z: float) -> HeightResult:
        grid_x = WORLD_GRID_ORIGIN + math.floor(x / GRID_SIZE)
        grid_z = WORLD_GRID_ORIGIN + math.floor(z / GRID_SIZE)
        if grid_x < 0 or grid_x >= WORLD_GRID_COUNT:
            return HeightResult(None, "x_out_of_world_bounds")
        if grid_z < 0 or grid_z >= WORLD_GRID_COUNT:
            return HeightResult(None, "z_out_of_world_bounds")

        # MapFile.GetGrid in the C# runtime looks up (gridZ, gridX) against
        # the generated grid key. Keep that order here so the audit mirrors
        # runtime terrain lookup.
        grid = self.grids.get((int(grid_z), int(grid_x)))
        if grid is None:
            grid = self.grids.get((int(grid_x), int(grid_z)))
        if grid is None:
            return HeightResult(None, "grid_missing")

        cell = grid.get_cell(x, z)
        if cell is None:
            return HeightResult(None, "cell_missing")

        return cell.get_terrain_height(self.path, x, z)

    def _read(self) -> None:
        with self.path.open("rb") as stream:
            if _read_u32(stream) != MAGIC:
                raise ValueError(f"{self.path} is not an NFMP map file")
            version = _read_u32(stream)
            if version != VERSION:
                raise ValueError(f"{self.path} has unsupported map version {version}")
            build = _read_u32(stream)
            if build != BUILD:
                raise ValueError(f"{self.path} has unsupported client build {build}")

            self.asset = _read_dotnet_string(stream)
            grid_count = _read_u32(stream)
            for _ in range(grid_count):
                grid = _read_grid(stream)
                self.grids[(grid.x, grid.z)] = grid

            trailing = stream.read(1)
            if trailing:
                raise ValueError(f"{self.path} has trailing unread data")


def _read_u32(stream) -> int:
    data = stream.read(4)
    if len(data) != 4:
        raise EOFError("unexpected end of map file")
    return struct.unpack("<I", data)[0]


def _read_half(stream) -> float:
    data = stream.read(2)
    if len(data) != 2:
        raise EOFError("unexpected end of map file")
    return struct.unpack("<e", data)[0]


def _read_dotnet_string(stream) -> str:
    length = 0
    shift = 0
    while True:
        data = stream.read(1)
        if len(data) != 1:
            raise EOFError("unexpected end of map file")
        value = data[0]
        length |= (value & 0x7F) << shift
        if (value & 0x80) == 0:
            break
        shift += 7
        if shift > 35:
            raise ValueError("invalid .NET string length encoding")

    return stream.read(length).decode("utf-8")


def _read_grid(stream) -> MapGrid:
    grid_x = _read_u32(stream)
    grid_z = _read_u32(stream)
    cell_count = _read_u32(stream)
    cells: dict[tuple[int, int], MapCellIndex] = {}
    for _ in range(cell_count):
        cell = _read_cell_index(stream)
        cells[(cell.x, cell.z)] = cell
    return MapGrid(grid_x, grid_z, cells)


def _read_cell_index(stream) -> MapCellIndex:
    cell_x = _read_u32(stream)
    cell_z = _read_u32(stream)
    flags = _read_u32(stream)
    if flags & ~KNOWN_CELL_FLAGS:
        raise ValueError(f"unknown map cell flags: 0x{flags:08x}")

    height_offset: int | None = None
    for bit in range(32):
        flag = 1 << bit
        if (flags & flag) == 0:
            continue

        if flag == FLAG_ZONE:
            stream.seek(4 * 4, 1)
        elif flag == FLAG_HEIGHT:
            height_offset = stream.tell()
            stream.seek(17 * 17 * 2, 1)
        elif flag == FLAG_ZONE_BOUND:
            stream.seek(64 * 64, 1)
        elif flag in (FLAG_AURA, FLAG_LIQUID):
            pass

    return MapCellIndex(cell_x, cell_z, flags, height_offset)


def _read_height_map(stream) -> list[list[float]]:
    height_map = [[0.0 for _ in range(17)] for _ in range(17)]
    for y in range(17):
        for x in range(17):
            height_map[x][y] = _read_half(stream)
    return height_map


class MapCache:
    def __init__(self, map_dir: Path, world_assets: dict[str, str]):
        self.map_dir = map_dir
        self.world_assets = world_assets
        self.cache: dict[str, MapFile] = {}

    def get(self, world_id: str) -> tuple[MapFile | None, str]:
        asset = self.world_assets.get(world_id)
        if not asset:
            return None, "world_asset_missing"

        if world_id in self.cache:
            return self.cache[world_id], ""

        asset_name = Path(asset.replace("\\", "/")).name
        map_path = self.map_dir / f"{asset_name}.nfmap"
        if not map_path.exists():
            return None, f"map_file_missing:{map_path}"

        map_file = MapFile(map_path)
        self.cache[world_id] = map_file
        return map_file, ""


def load_world_assets(path: Path) -> dict[str, str]:
    with path.open(newline="", encoding="utf-8-sig") as stream:
        reader = csv.DictReader(stream)
        result = {}
        for row in reader:
            world_id = row.get("world_id") or row.get("World") or row.get("world")
            asset = row.get("asset_path") or row.get("AssetPath") or row.get("asset")
            if world_id and asset:
                result[world_id] = asset
        return result


def choose_field(row: dict[str, str], names: Iterable[str]) -> str:
    for name in names:
        if name in row and row[name] != "":
            return row[name]
    lower = {key.lower(): value for key, value in row.items()}
    for name in names:
        value = lower.get(name.lower())
        if value not in (None, ""):
            return value
    return ""


def parse_float(value: str) -> float | None:
    try:
        return float(value)
    except (TypeError, ValueError):
        return None


def row_matches(row: dict[str, str], args: argparse.Namespace) -> bool:
    name = choose_field(row, ["source_name", "creature2_name", "name", "Name"])
    creature = choose_field(row, ["Creature", "creature", "creature2_id", "creature_id"])
    world = choose_field(row, ["World", "world", "worldid", "world_id"])
    area = choose_field(row, ["Area", "area", "worldzoneid", "world_zone_id"])

    if args.name_contains and args.name_contains.lower() not in name.lower():
        return False
    if args.creature and creature not in args.creature:
        return False
    if args.world and world not in args.world:
        return False
    if args.area and area not in args.area:
        return False
    return True


def audit_rows(args: argparse.Namespace) -> tuple[list[dict[str, str]], dict[str, int]]:
    world_assets = load_world_assets(args.world_map)
    map_cache = MapCache(args.map_dir, world_assets)
    results: list[dict[str, str]] = []
    stats = {
        "rows_read": 0,
        "rows_filtered": 0,
        "rows_with_height": 0,
        "rows_reported": 0,
        "rows_missing_height": 0,
    }

    with args.entity_csv.open(newline="", encoding="utf-8-sig") as stream:
        reader = csv.DictReader(stream)
        for row in reader:
            stats["rows_read"] += 1
            if not row_matches(row, args):
                continue
            stats["rows_filtered"] += 1

            world = choose_field(row, ["World", "world", "worldid", "world_id"])
            x = parse_float(choose_field(row, ["X", "x"]))
            y = parse_float(choose_field(row, ["Y", "y"]))
            z = parse_float(choose_field(row, ["Z", "z"]))
            if x is None or y is None or z is None:
                stats["rows_missing_height"] += 1
                if args.include_missing:
                    results.append(build_missing_row(row, world, "invalid_coordinate"))
                continue

            map_file, map_error = map_cache.get(world)
            if map_file is None:
                stats["rows_missing_height"] += 1
                if args.include_missing:
                    results.append(build_missing_row(row, world, map_error))
                continue

            terrain = map_file.get_terrain_height(x, z)
            if terrain.height is None:
                stats["rows_missing_height"] += 1
                if args.include_missing:
                    results.append(build_missing_row(row, world, terrain.reason or "height_missing"))
                continue

            stats["rows_with_height"] += 1
            delta = y - terrain.height
            if not should_report_delta(delta, args):
                continue

            result = build_result_row(row, world, x, y, z, terrain.height, delta, map_file.asset)
            results.append(result)
            stats["rows_reported"] += 1

    results.sort(key=lambda r: abs(float(r.get("delta_y", "0"))), reverse=True)
    if args.limit:
        results = results[: args.limit]
    return results, stats


def should_report_delta(delta: float, args: argparse.Namespace) -> bool:
    if args.direction == "above" and delta < args.min_delta:
        return False
    if args.direction == "below" and -delta < args.min_delta:
        return False
    if args.direction == "both" and abs(delta) < args.min_delta:
        return False
    return True


def build_result_row(
    row: dict[str, str],
    world: str,
    x: float,
    y: float,
    z: float,
    terrain_y: float,
    delta: float,
    map_asset: str,
) -> dict[str, str]:
    return {
        "source_id": choose_field(row, ["source_coordinate_id", "entity_id", "id"]),
        "name": choose_field(row, ["source_name", "creature2_name", "name", "Name"]),
        "creature": choose_field(row, ["Creature", "creature", "creature2_id", "creature_id"]),
        "world": world,
        "area": choose_field(row, ["Area", "area", "worldzoneid", "world_zone_id"]),
        "x": format_float(x),
        "y": format_float(y),
        "z": format_float(z),
        "terrain_y": format_float(terrain_y),
        "delta_y": format_float(delta),
        "abs_delta_y": format_float(abs(delta)),
        "match_status": choose_field(row, ["match_status", "MatchStatus"]),
        "map_asset": map_asset,
        "status": "ok",
    }


def build_missing_row(row: dict[str, str], world: str, reason: str) -> dict[str, str]:
    return {
        "source_id": choose_field(row, ["source_coordinate_id", "entity_id", "id"]),
        "name": choose_field(row, ["source_name", "creature2_name", "name", "Name"]),
        "creature": choose_field(row, ["Creature", "creature", "creature2_id", "creature_id"]),
        "world": world,
        "area": choose_field(row, ["Area", "area", "worldzoneid", "world_zone_id"]),
        "x": choose_field(row, ["X", "x"]),
        "y": choose_field(row, ["Y", "y"]),
        "z": choose_field(row, ["Z", "z"]),
        "terrain_y": "",
        "delta_y": "",
        "abs_delta_y": "",
        "match_status": choose_field(row, ["match_status", "MatchStatus"]),
        "map_asset": "",
        "status": reason,
    }


def format_float(value: float) -> str:
    return f"{value:.3f}"


def write_results(results: list[dict[str, str]], output: Path | None) -> None:
    fields = [
        "source_id",
        "name",
        "creature",
        "world",
        "area",
        "x",
        "y",
        "z",
        "terrain_y",
        "delta_y",
        "abs_delta_y",
        "match_status",
        "map_asset",
        "status",
    ]
    if output:
        output.parent.mkdir(parents=True, exist_ok=True)
        stream = output.open("w", newline="", encoding="utf-8")
        close_stream = True
    else:
        stream = sys.stdout
        close_stream = False

    try:
        writer = csv.DictWriter(stream, fieldnames=fields, extrasaction="ignore")
        writer.writeheader()
        writer.writerows(results)
    finally:
        if close_stream:
            stream.close()


def default_map_dir() -> Path:
    candidates = [
        Path("Source/NexusForever.MapGenerator/bin/Debug/net10.0/map"),
        Path(".nexusforever-runtime/assets/map"),
        Path(".nexusforever-runtime/map"),
    ]
    for candidate in candidates:
        if candidate.exists():
            return candidate
    return candidates[0]


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--entity-csv", type=Path, default=Path("Tools/DataMapping/output/world_entity_candidate.csv"))
    parser.add_argument("--world-map", type=Path, default=Path("Tools/DataMapping/output/world_client_map.csv"))
    parser.add_argument("--map-dir", type=Path, default=default_map_dir())
    parser.add_argument("--name-contains", help="Only audit rows whose source/name contains this text.")
    parser.add_argument("--creature", action="append", help="Only audit a Creature/creature2 id. Can be passed more than once.")
    parser.add_argument("--world", action="append", help="Only audit a world id. Can be passed more than once.")
    parser.add_argument("--area", action="append", help="Only audit an area/worldzone id. Can be passed more than once.")
    parser.add_argument("--min-delta", type=float, default=2.0, help="Minimum vertical delta to report. Default: 2.0")
    parser.add_argument("--direction", choices=["above", "below", "both"], default="above")
    parser.add_argument("--include-missing", action="store_true", help="Include rows that could not be matched to terrain.")
    parser.add_argument("--limit", type=int, default=50, help="Maximum rows to print/write after sorting by abs delta. Use 0 for all.")
    parser.add_argument("--output", type=Path, help="Write CSV results to this path instead of stdout.")
    args = parser.parse_args()
    if args.limit == 0:
        args.limit = None
    return args


def main() -> int:
    args = parse_args()
    if not args.entity_csv.exists():
        print(f"entity CSV not found: {args.entity_csv}", file=sys.stderr)
        return 2
    if not args.world_map.exists():
        print(f"world map CSV not found: {args.world_map}", file=sys.stderr)
        return 2
    if not args.map_dir.exists():
        print(f"map directory not found: {args.map_dir}", file=sys.stderr)
        return 2

    results, stats = audit_rows(args)
    write_results(results, args.output)
    print(
        "terrain-height audit: "
        + ", ".join(f"{key}={value}" for key, value in stats.items()),
        file=sys.stderr,
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
