#!/usr/bin/env python3
"""Extract WIP instance entity/event rows from selected LaughingWS SQL."""

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


SET_WORLD_RE = re.compile(r"\bSET\s+@WORLD\s*:?=\s*(\d+)\s*;", re.IGNORECASE)
INSERT_ENTITY_RE = re.compile(
    r"\bINSERT\s+INTO\s+`?entity`?\s*\((?P<columns>.*?)\)\s*"
    r"VALUE(?:S)?\s*(?P<values>.*?);",
    re.IGNORECASE | re.DOTALL,
)

BASE_INSTANCE_ENTITY_ID = 1_100_300_000

# These WIP gather-marker rows duplicate already-promoted static markers at the
# same visible coordinates. Keep spec ordering stable so existing high IDs do
# not shift, but omit the duplicate static copies from regenerated seeds.
SKIPPED_DUPLICATE_INSTANCE_ENTITY_LABELS = {
    "Protogames Academy gather marker Iruki Boldbeard",
    "Protogames Academy gather marker arena B",
    "Protogames Academy gather marker final arena",
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
class InstanceEntitySpec:
    source_file: str
    source_creature: int
    label: str
    event_id: int | None
    phase: int | None
    stats: tuple[tuple[int, int], ...]
    script_name: str | None
    evidence: str
    source_match_index: int = 0


@dataclass(frozen=True)
class OfficialEntityUpdateSpec:
    label: str
    world: int
    creature: int
    x: str
    y: str
    z: str
    updates: tuple[tuple[str, str], ...]
    evidence: str


# WIP/GUESSED: these rows are branch-authored instance placements. They are
# promoted only where the current workspace already has matching map/event/script
# support; exact encounter choreography, trigger timing, and manual smoke proof
# remain blocked in the audit.
INSTANCE_ENTITY_SPECS = (
    InstanceEntitySpec(
        "Map/Instances/Dungeons/Coldblood Citadel.sql",
        75508,
        "Coldblood Citadel Hailstone Gatecrasher",
        907,
        1,
        ((10, 50),),
        "HailstoneGatecrasherEntityScript",
        "Branch SQL supplies event 907 phase 1, level stat 50, and HailstoneGatecrasherEntityScript; current C# has the WIP-guessed event script and matching entity script.",
    ),
    InstanceEntitySpec(
        "Map/Instances/Dungeons/Coldblood Citadel.sql",
        75624,
        "Coldblood Citadel Gather Ring",
        907,
        0,
        (),
        None,
        "Branch SQL supplies event 907 phase 0; current C# tracks and removes PublicEventCreature.GatherRing during the WIP-guessed phase handoff.",
    ),
    InstanceEntitySpec(
        "Map/Instances/Dungeons/Coldblood Citadel.sql",
        75698,
        "Coldblood Citadel Coldblood Gate",
        907,
        2,
        (),
        None,
        "Branch SQL supplies event 907 phase 2; exact door choreography is still blocked, so this remains a WIP placement row.",
    ),
    InstanceEntitySpec(
        "Map/Instances/Event instances/Protostar SuperMall-In-The-Sky.sql",
        68636,
        "Protostar SuperMall Gather Marker",
        679,
        0,
        (),
        None,
        "Branch SQL supplies event 679 phase 0; current runtime only has the map binding, so event routing/cinematics remain blocked.",
    ),
    InstanceEntitySpec(
        "Map/Instances/Expeditions/Space Madness.sql",
        45900,
        "Space Madness Captain Tero",
        390,
        0,
        ((0, 1),),
        None,
        "Branch SQL supplies event 390 phase 0 and the current Space Madness event script starts at TalkToCaptainTero; placement is preserved as WIP/GUESSED pending manual expedition smoke.",
    ),
    InstanceEntitySpec(
        "Map/Instances/Expeditions/Space Madness.sql",
        45812,
        "Space Madness Major Lee Barmy",
        390,
        1,
        ((0, 1),),
        None,
        "Branch SQL supplies event 390 phase 1 and the current Space Madness event script advances to TalkToMajorLeeBarmy; placement is preserved as WIP/GUESSED pending manual expedition smoke.",
    ),
    InstanceEntitySpec(
        "Map/Instances/Expeditions/Space Madness.sql",
        45972,
        "Space Madness Observation Deck Computer",
        390,
        2,
        (),
        None,
        "Branch SQL supplies event 390 phase 2 for the observation-deck computer; the source comment says coordinates need fixing, so this remains WIP/GUESSED placement data.",
    ),
    InstanceEntitySpec(
        "Map/Instances/Expeditions/Space Madness.sql",
        46092,
        "Space Madness Hazmat Control Panel",
        390,
        3,
        (),
        None,
        "Branch SQL supplies event 390 phase 3 for the hazmat control panel; the source comment says coordinates need fixing, so this remains WIP/GUESSED placement data.",
    ),
    InstanceEntitySpec(
        "Map/Instances/Expeditions/Space Madness.sql",
        46483,
        "Space Madness Exact Change 3.0",
        390,
        4,
        ((0, 1), (10, 32)),
        None,
        "Branch SQL supplies event 390 phase 4 and source level/stat notes for Exact Change 3.0; current script only activates the livestock phase objective, so combat behavior remains blocked.",
    ),
    InstanceEntitySpec(
        "Map/Instances/Expeditions/Space Madness.sql",
        46714,
        "Space Madness Blazing Crewman A",
        390,
        4,
        ((0, 1), (10, 32)),
        None,
        "Branch SQL supplies the first event 390 phase 4 Blazing Crewman row; current script only activates phase objectives, so spawn/combat behavior remains WIP/GUESSED.",
        source_match_index=0,
    ),
    InstanceEntitySpec(
        "Map/Instances/Expeditions/Space Madness.sql",
        46714,
        "Space Madness Blazing Crewman B",
        390,
        4,
        ((0, 1), (10, 32)),
        None,
        "Branch SQL supplies the second event 390 phase 4 Blazing Crewman row; current script only activates phase objectives, so spawn/combat behavior remains WIP/GUESSED.",
        source_match_index=1,
    ),
    InstanceEntitySpec(
        "Map/Instances/Expeditions/Space Madness.sql",
        45981,
        "Space Madness Hazmat Suit",
        390,
        5,
        (),
        None,
        "Branch SQL supplies event 390 phase 5 for the hazmat suit; the source comment says coordinates need fixing, so this remains WIP/GUESSED placement data.",
    ),
    InstanceEntitySpec(
        "Map/Instances/Expeditions/Gauntlet.sql",
        48580,
        "Gauntlet Pilot Taboro",
        446,
        0,
        ((0, 1),),
        None,
        "Branch SQL supplies event 446 phase 0 and the current Gauntlet event script starts at TalkToPilotTaboro; placement is preserved as WIP/GUESSED pending manual expedition smoke.",
    ),
    InstanceEntitySpec(
        "Map/Instances/Expeditions/Gauntlet.sql",
        58979,
        "Gauntlet Gather Marker",
        None,
        None,
        (),
        None,
        "Branch SQL supplies a static gather marker with no entity_event row; current Gauntlet script creates gather triggers dynamically, so this placement stays WIP/GUESSED and choreography-blocked.",
    ),
    InstanceEntitySpec(
        "Map/Instances/Expeditions/Fragment Zero.sql",
        71762,
        "Fragment Zero Freight Supervisor Lola",
        None,
        None,
        ((0, 1), (20, 0), (22, 0)),
        None,
        "Branch SQL supplies Freight Supervisor Lola as a static Fragment Zero row; current script uses Supervisor Lola communicator ids, but exact NPC phase/timing proof remains blocked.",
    ),
    InstanceEntitySpec(
        "Map/Instances/Expeditions/Infestation.sql",
        71764,
        "Infestation Captain Tolben",
        None,
        None,
        ((0, 1),),
        None,
        "Branch SQL supplies Captain Tolben as a static Infestation row; current script starts the cargo-ship objective chain, but exact NPC interaction/timing proof remains blocked.",
    ),
    InstanceEntitySpec(
        "Map/Instances/Expeditions/Outpost M13.sql",
        41201,
        "Outpost M-13 Captain Milo",
        108,
        0,
        ((0, 1),),
        None,
        "Branch SQL supplies event 108 phase 0 and the current Outpost M-13 event script starts at TalkToCaptainMilo; placement is preserved as WIP/GUESSED pending manual expedition smoke.",
    ),
    InstanceEntitySpec(
        "Map/Instances/Expeditions/Evil from the Ether.sql",
        71847,
        "Evil from the Ether Drive Spark A",
        781,
        23,
        (),
        None,
        "Branch SQL supplies event 781 phase 23, which maps to the current PickUpDriveSchematics phase; official DB has no matching row, so this is WIP/GUESSED visual/objective-phase placement only.",
        source_match_index=0,
    ),
    InstanceEntitySpec(
        "Map/Instances/Expeditions/Evil from the Ether.sql",
        71847,
        "Evil from the Ether Drive Spark B",
        781,
        23,
        (),
        None,
        "Branch SQL supplies event 781 phase 23, which maps to the current PickUpDriveSchematics phase; official DB has no matching row, so this is WIP/GUESSED visual/objective-phase placement only.",
        source_match_index=1,
    ),
    InstanceEntitySpec(
        "Map/Instances/Expeditions/Evil from the Ether.sql",
        71847,
        "Evil from the Ether Drive Spark C",
        781,
        23,
        (),
        None,
        "Branch SQL supplies event 781 phase 23, which maps to the current PickUpDriveSchematics phase; official DB has no matching row, so this is WIP/GUESSED visual/objective-phase placement only.",
        source_match_index=2,
    ),
    InstanceEntitySpec(
        "Map/Instances/Expeditions/Evil from the Ether.sql",
        71847,
        "Evil from the Ether Drive Spark D",
        781,
        23,
        (),
        None,
        "Branch SQL supplies event 781 phase 23, which maps to the current PickUpDriveSchematics phase; official DB has no matching row, so this is WIP/GUESSED visual/objective-phase placement only.",
        source_match_index=3,
    ),
    InstanceEntitySpec(
        "Map/Instances/Expeditions/Evil from the Ether.sql",
        71847,
        "Evil from the Ether Drive Spark E",
        781,
        23,
        (),
        None,
        "Branch SQL supplies event 781 phase 23, which maps to the current PickUpDriveSchematics phase; official DB has no matching row, so this is WIP/GUESSED visual/objective-phase placement only.",
        source_match_index=4,
    ),
    InstanceEntitySpec(
        "Map/Instances/Expeditions/Evil from the Ether.sql",
        71847,
        "Evil from the Ether Drive Spark F",
        781,
        23,
        (),
        None,
        "Branch SQL supplies event 781 phase 23, which maps to the current PickUpDriveSchematics phase; official DB has no matching row, so this is WIP/GUESSED visual/objective-phase placement only.",
        source_match_index=5,
    ),
    InstanceEntitySpec(
        "Map/Instances/Expeditions/Evil from the Ether.sql",
        71847,
        "Evil from the Ether Drive Spark G",
        781,
        23,
        (),
        None,
        "Branch SQL supplies event 781 phase 23, which maps to the current PickUpDriveSchematics phase; official DB has no matching row, so this is WIP/GUESSED visual/objective-phase placement only.",
        source_match_index=6,
    ),
    InstanceEntitySpec(
        "Map/Instances/Expeditions/Evil from the Ether.sql",
        71847,
        "Evil from the Ether Drive Spark H",
        781,
        23,
        (),
        None,
        "Branch SQL supplies event 781 phase 23, which maps to the current PickUpDriveSchematics phase; official DB has no matching row, so this is WIP/GUESSED visual/objective-phase placement only.",
        source_match_index=7,
    ),
    InstanceEntitySpec(
        "Map/Instances/Expeditions/Evil from the Ether.sql",
        71847,
        "Evil from the Ether Drive Spark I",
        781,
        23,
        (),
        None,
        "Branch SQL supplies event 781 phase 23, which maps to the current PickUpDriveSchematics phase; official DB has no matching row, so this is WIP/GUESSED visual/objective-phase placement only.",
        source_match_index=8,
    ),
    InstanceEntitySpec(
        "Map/Instances/Expeditions/Evil from the Ether.sql",
        71847,
        "Evil from the Ether Drive Spark J",
        781,
        23,
        (),
        None,
        "Branch SQL supplies event 781 phase 23, which maps to the current PickUpDriveSchematics phase; official DB has no matching row, so this is WIP/GUESSED visual/objective-phase placement only.",
        source_match_index=9,
    ),
    InstanceEntitySpec(
        "Map/Instances/Expeditions/Evil from the Ether.sql",
        71847,
        "Evil from the Ether Drive Spark K",
        781,
        23,
        (),
        None,
        "Branch SQL supplies event 781 phase 23, which maps to the current PickUpDriveSchematics phase; official DB has no matching row, so this is WIP/GUESSED visual/objective-phase placement only.",
        source_match_index=10,
    ),
    InstanceEntitySpec(
        "Map/Instances/Dungeons/Protogames Academy.sql",
        62475,
        "Protogames Academy portal A",
        667,
        8,
        (),
        None,
        "Branch SQL supplies event 667 phase 8, but the source notes the portal may belong only to Ultimate Protogames; current C# uses a dynamic WIP teleporter trigger, so this static portal remains WIP/GUESSED.",
        source_match_index=0,
    ),
    InstanceEntitySpec(
        "Map/Instances/Dungeons/Protogames Academy.sql",
        62475,
        "Protogames Academy portal B",
        667,
        16,
        (),
        None,
        "Branch SQL supplies event 667 phase 16, but the source notes the portal may belong only to Ultimate Protogames; current C# uses a dynamic WIP teleporter trigger, so this static portal remains WIP/GUESSED.",
        source_match_index=1,
    ),
    InstanceEntitySpec(
        "Map/Instances/Dungeons/Protogames Academy.sql",
        67475,
        "Protogames Academy Invulnotron",
        667,
        3,
        ((0, 1), (10, 10)),
        "InvulnotronEntityScript",
        "Branch SQL supplies event 667 phase 3 and InvulnotronEntityScript; current C# has a WIP objective-credit script, while combat behavior and spawn timing remain blocked.",
    ),
    InstanceEntitySpec(
        "Map/Instances/Dungeons/Protogames Academy.sql",
        67539,
        "Protogames Academy Room 1 Launcher",
        667,
        4,
        (),
        None,
        "Branch SQL supplies event 667 phase 4 and notes the faction is uncertain; current C# uses WIP gather/teleporter triggers, so launcher interaction/choreography remains blocked.",
    ),
    InstanceEntitySpec(
        "Map/Instances/Dungeons/Protogames Academy.sql",
        67594,
        "Protogames Academy Gromka",
        667,
        5,
        ((0, 1), (10, 10)),
        "GromkaEntityScript",
        "Branch SQL supplies event 667 phase 5 and GromkaEntityScript; current C# has a WIP objective-credit script, while combat behavior and spawn timing remain blocked.",
    ),
    InstanceEntitySpec(
        "Map/Instances/Dungeons/Protogames Academy.sql",
        67663,
        "Protogames Academy Iruki Boldbeard",
        667,
        7,
        ((0, 1), (10, 10)),
        "IrukiBoldbeardEntityScript",
        "Branch SQL supplies event 667 phase 7 and IrukiBoldbeardEntityScript; current C# has a WIP objective-credit script, while combat behavior and spawn timing remain blocked.",
    ),
    InstanceEntitySpec(
        "Map/Instances/Dungeons/Protogames Academy.sql",
        67668,
        "Protogames Academy Seek-N-Slaughter",
        667,
        11,
        ((0, 1), (10, 10)),
        "SeekNSlaughterEntityScript",
        "Branch SQL supplies event 667 phase 11 and current C# activates DefeatSeekNSlaughter; this WIP objective-credit script only grants death credit, while combat behavior remains blocked.",
    ),
    InstanceEntitySpec(
        "Map/Instances/Dungeons/Protogames Academy.sql",
        67757,
        "Protogames Academy Icebox Mk. 2",
        667,
        13,
        ((0, 1), (10, 10)),
        "IceboxMk2EntityScript",
        "Branch SQL supplies event 667 phase 13 and current C# activates DefeatIceboxMk2; this WIP objective-credit script only grants death credit, while combat behavior remains blocked.",
    ),
    InstanceEntitySpec(
        "Map/Instances/Dungeons/Protogames Academy.sql",
        68339,
        "Protogames Academy Initiation Button",
        None,
        None,
        (),
        None,
        "Branch SQL supplies a static initiation button but no entity_event row, and the source questions the ActivePropId; current C# starts the initiation objective, so this remains WIP/GUESSED placement data.",
    ),
    InstanceEntitySpec(
        "Map/Instances/Dungeons/Protogames Academy.sql",
        70003,
        "Protogames Academy gather marker Invulnotron",
        667,
        1,
        (),
        None,
        "Branch SQL supplies the first event 667 gather marker; current C# creates WIP gather triggers dynamically, so the static placement remains WIP/GUESSED pending trigger cleanup proof.",
        source_match_index=0,
    ),
    InstanceEntitySpec(
        "Map/Instances/Dungeons/Protogames Academy.sql",
        70003,
        "Protogames Academy gather marker Gromka",
        667,
        4,
        (),
        None,
        "Branch SQL supplies the second event 667 gather marker; current C# creates WIP gather triggers dynamically, so the static placement remains WIP/GUESSED pending trigger cleanup proof.",
        source_match_index=1,
    ),
    InstanceEntitySpec(
        "Map/Instances/Dungeons/Protogames Academy.sql",
        70003,
        "Protogames Academy gather marker Iruki Boldbeard",
        667,
        6,
        (),
        None,
        "Branch SQL supplies the third event 667 gather marker; current C# creates WIP gather triggers dynamically, so the static placement remains WIP/GUESSED pending trigger cleanup proof.",
        source_match_index=2,
    ),
    InstanceEntitySpec(
        "Map/Instances/Dungeons/Protogames Academy.sql",
        70003,
        "Protogames Academy gather marker arena A",
        667,
        9,
        (),
        None,
        "Branch SQL supplies the fourth event 667 gather marker; current C# creates WIP gather triggers dynamically, so the static placement remains WIP/GUESSED pending trigger cleanup proof.",
        source_match_index=3,
    ),
    InstanceEntitySpec(
        "Map/Instances/Dungeons/Protogames Academy.sql",
        70003,
        "Protogames Academy gather marker arena B",
        667,
        12,
        (),
        None,
        "Branch SQL supplies the fifth event 667 gather marker; current C# creates WIP gather triggers dynamically, so the static placement remains WIP/GUESSED pending trigger cleanup proof.",
        source_match_index=4,
    ),
    InstanceEntitySpec(
        "Map/Instances/Dungeons/Protogames Academy.sql",
        70003,
        "Protogames Academy gather marker final arena",
        667,
        14,
        (),
        None,
        "Branch SQL supplies the sixth event 667 gather marker; current C# creates WIP gather triggers dynamically, so the static placement remains WIP/GUESSED pending trigger cleanup proof.",
        source_match_index=5,
    ),
    InstanceEntitySpec(
        "Map/Instances/Dungeons/Ruins of Kel Voreth.sql",
        32531,
        "Ruins of Kel Voreth Forgemaster Trogun",
        161,
        3,
        ((10, 25),),
        "ForgemasterTrogunEntityScript",
        "Branch SQL supplies event 161 phase 3 and ForgemasterTrogunEntityScript; current C# has a WIP objective-credit script, while exact boss combat and door choreography remain blocked.",
    ),
    InstanceEntitySpec(
        "Map/Instances/Dungeons/Ruins of Kel Voreth.sql",
        32534,
        "Ruins of Kel Voreth Grond the Corpsemaker",
        161,
        1,
        ((10, 25),),
        "GrondTheCorpsemakerEntityScript",
        "Branch SQL supplies event 161 phase 1 and GrondTheCorpsemakerEntityScript; current C# has a WIP objective-credit script, while exact boss combat and encounter choreography remain blocked.",
    ),
    InstanceEntitySpec(
        "Map/Instances/Dungeons/Ruins of Kel Voreth.sql",
        32536,
        "Ruins of Kel Voreth Slavemaster Drokk",
        161,
        2,
        ((10, 25),),
        "SlavemasterDrokkEntityScript",
        "Branch SQL supplies event 161 phase 2 and SlavemasterDrokkEntityScript; current C# has a WIP objective-credit script, while exact boss combat and faction-specific messaging remain blocked.",
    ),
    InstanceEntitySpec(
        "Map/Instances/Dungeons/Stormtalon's Lair.sql",
        32703,
        "Stormtalon's Lair Aethros",
        145,
        2,
        ((0, 1), (10, 50)),
        "AethrosVeteranEntityScript",
        "Branch SQL supplies event 145 phase 2 and AethrosVeteranEntityScript; current C# has a WIP objective-credit script, while exact boss combat and Stormtalon choreography remain blocked.",
    ),
    InstanceEntitySpec(
        "Map/Instances/Dungeons/Stormtalon's Lair.sql",
        33405,
        "Stormtalon's Lair Blade-Wind the Invoker",
        145,
        1,
        ((0, 1), (10, 50)),
        "BladeWindTheInvokerVeteranEntityScript",
        "Branch SQL supplies event 145 phase 1 and BladeWindTheInvokerVeteranEntityScript; current C# has a WIP objective-credit script, while exact boss combat and optional route availability remain blocked.",
    ),
    InstanceEntitySpec(
        "Map/Instances/Dungeons/Skullcano.sql",
        24475,
        "Skullcano Thunderfoot",
        148,
        0,
        ((10, 35),),
        "ThunderfootNormalEntityScript",
        "Branch SQL supplies event 148 phase 0 and ThunderfootNormalEntityScript; current C# has a WIP objective-credit script, while exact boss combat and route choreography remain blocked.",
    ),
    InstanceEntitySpec(
        "Map/Instances/Dungeons/Skullcano.sql",
        24493,
        "Skullcano Stew-Shaman Tugga",
        148,
        0,
        ((10, 35),),
        "StewShamanTuggaNormalEntityScript",
        "Branch SQL supplies event 148 phase 0 and StewShamanTuggaNormalEntityScript; current C# has a WIP objective-credit script, while exact boss combat and cave/chasm route selection remain blocked.",
    ),
    InstanceEntitySpec(
        "Map/Instances/Dungeons/Sanctuary of the Swordmaiden.sql",
        29254,
        "Sanctuary of the Swordmaiden Flame-Crazed Demon",
        166,
        4,
        ((10, 40),),
        "FlameCrazedDemonEntityScript",
        "Branch SQL supplies event 166 phase 4 and FlameCrazedDemonEntityScript; current C# has a WIP objective-credit script, while exact miniboss combat and path selection remain blocked.",
    ),
    InstanceEntitySpec(
        "Map/Instances/Raids/Genetic Archives.sql",
        49198,
        "Genetic Archives Experiment X-89",
        None,
        None,
        ((0, 1), (10, 50)),
        "ExperimentX-89EntityScript",
        "Branch SQL supplies ExperimentX-89EntityScript without an entity_event row; current C# has a WIP objective-credit script, while raid encounter choreography remains blocked.",
    ),
    InstanceEntitySpec(
        "Map/Instances/Raids/Genetic Archives.sql",
        52969,
        "Genetic Archives Kuralak the Defiler",
        None,
        None,
        ((0, 1), (10, 50)),
        "KuralakTheDefilerEntityScript",
        "Branch SQL supplies KuralakTheDefilerEntityScript without an entity_event row; current C# has a WIP objective-credit script, while raid encounter choreography remains blocked.",
    ),
    InstanceEntitySpec(
        "Map/Instances/Raids/Genetic Archives.sql",
        53031,
        "Genetic Archives Pillar of the Genetic Library",
        None,
        None,
        ((0, 1), (10, 50)),
        "KuralakPillarEntityScript",
        "Branch SQL supplies KuralakPillarEntityScript without an entity_event row; current C# intentionally keeps the script as a WIP no-op loader hook until Kuralak pillar mechanics are mapped.",
    ),
    InstanceEntitySpec(
        "Map/Instances/Dungeons/Ultimate Protogames.sql",
        61463,
        "Ultimate Protogames Bev-O-Rage",
        594,
        5,
        ((10, 50),),
        None,
        "Branch SQL supplies event 594 phase 5 and level stat 50; current runtime only has the map binding, so event routing, objective credit, combat behavior, and manual dungeon smoke remain blocked.",
    ),
    InstanceEntitySpec(
        "Map/Instances/Raids/Redmoon Terror.sql",
        65997,
        "Red Moon Terror Laveka",
        None,
        None,
        ((0, 1), (10, 50)),
        "LavekaTheDarkHeartedEntityScript",
        "Branch SQL supplies a static Laveka row with health marker and level stats; current C# has the WIP-guessed Red Moon Terror event chain and a WIP objective-credit script for the final Laveka objective, while boss choreography, awakening/challenge logic, and manual raid smoke remain blocked.",
    ),
    InstanceEntitySpec(
        "Map/Instances/Event instances/Shade's Eve.sql",
        62747,
        "Shade's Eve Etty Windsen",
        597,
        1,
        ((0, 1), (10, 50)),
        None,
        "Branch SQL supplies event 597 phase 1 and source level stat 50; current Shade's Eve script advances to SpeakWithEttyWindsen, but exact NPC interaction and cinematic/vote timing remain blocked.",
    ),
    InstanceEntitySpec(
        "Map/Instances/Event instances/Shade's Eve.sql",
        64820,
        "Shade's Eve Fountain Gathering Circle",
        597,
        0,
        (),
        None,
        "Branch SQL supplies event 597 phase 0 and the current Shade's Eve script starts at FindTheFountain; gather cleanup and exact cinematic timing remain WIP/GUESSED.",
    ),
    InstanceEntitySpec(
        "Map/Instances/Raids/Datascape.sql",
        61819,
        "Datascape Optimized Memory Probe ED-1",
        157,
        0,
        ((0, 15900000), (10, 50)),
        "OptimizedMemoryProbeED1EntityScript",
        "Branch SQL supplies placed ED-1 coordinates and commented event 157 phase 0; current C# activates DefeatOptimizedMemoryProbeED1, while exact mechanics remain blocked.",
    ),
    InstanceEntitySpec(
        "Map/Instances/Raids/Datascape.sql",
        61818,
        "Datascape Optimized Memory Probe TX-67",
        157,
        0,
        ((0, 15900000), (10, 50)),
        "OptimizedMemoryProbeTX-67EntityScript",
        "Branch SQL supplies placed TX-67 coordinates and commented event 157 phase 0; current C# has the WIP objective-credit script, while exact mechanics remain blocked.",
    ),
    InstanceEntitySpec(
        "Map/Instances/Raids/Datascape.sql",
        31667,
        "Datascape Optimized Memory Probe P2-Z",
        157,
        0,
        ((0, 15900000), (10, 50)),
        "OptimizedMemoryProbeP2ZEntityScript",
        "Branch SQL supplies placed P2-Z coordinates and commented event 157 phase 0; current C# activates DefeatOptimizedMemoryProbeP2Z, while exact mechanics remain blocked.",
    ),
    InstanceEntitySpec(
        "Map/Instances/Raids/Datascape.sql",
        30495,
        "Datascape Null System Daemon",
        157,
        0,
        ((0, 14400000), (10, 50)),
        "NullSystemDaemonEntityScript",
        "Branch SQL supplies event 157 phase 0 and current C# starts by activating DefeatTheSystemDaemons; exact daemon encounter mechanics remain blocked.",
    ),
    InstanceEntitySpec(
        "Map/Instances/Raids/Datascape.sql",
        30496,
        "Datascape Binary System Daemon",
        157,
        0,
        ((0, 14400000), (10, 50)),
        "BinarySystemDaemonEntityScript",
        "Branch SQL supplies event 157 phase 0 and current C# starts by activating DefeatTheSystemDaemons; exact daemon encounter mechanics remain blocked.",
    ),
    InstanceEntitySpec(
        "Map/Instances/Raids/Datascape.sql",
        31677,
        "Datascape First Frost Boulder Avalanche",
        157,
        3,
        ((0, 14500000), (10, 50)),
        "FrostBoulderAvalancheFirstEntityScript",
        "Branch SQL supplies placed Frost Boulder coordinates; phase 3 is WIP-guessed from the current Datascape event script's FirstFrostBoulder phase.",
    ),
    InstanceEntitySpec(
        "Map/Instances/Raids/Datascape.sql",
        56200,
        "Datascape Second Frost Boulder Avalanche",
        157,
        4,
        ((0, 14500000), (10, 50)),
        "FrostBoulderAvalancheSecondEntityScript",
        "Branch SQL supplies placed Frost Boulder coordinates; phase 4 is WIP-guessed from the current Datascape event script's SecondFrostBoulder phase.",
    ),
    InstanceEntitySpec(
        "Map/Instances/Raids/Datascape.sql",
        31674,
        "Datascape Frostbringer Warlock",
        157,
        5,
        ((0, 15900000), (10, 50)),
        "FrostbringerWarlockEntityScript",
        "Branch SQL supplies placed Frostbringer Warlock coordinates; phase 5 is WIP-guessed from the current Datascape event script's FrostbringerWarlock phase.",
    ),
    InstanceEntitySpec(
        "Map/Instances/Raids/Datascape.sql",
        31885,
        "Datascape Bio-Enhanced Broodmother",
        157,
        0,
        ((0, 18000000), (10, 50)),
        "BioEnhancedBroodmotherEntityScript",
        "Branch SQL supplies placed Bio-Enhanced Broodmother coordinates; current C# activates this objective from ED-1 completion without a mapped separate phase, so phase 0 is WIP-guessed.",
    ),
    InstanceEntitySpec(
        "Map/Instances/Raids/Datascape.sql",
        30498,
        "Datascape Gloomclaw",
        157,
        11,
        ((0, 21000000), (10, 50)),
        "GloomclawEntityScript",
        "Branch SQL supplies placed Gloomclaw coordinates; phase 11 is WIP-guessed from the current Datascape event script's Gloomclaw phase.",
    ),
    InstanceEntitySpec(
        "Map/Instances/Raids/Datascape.sql",
        48065,
        "Datascape Hyper-Accelerated Skeledroid",
        157,
        12,
        ((0, 21300000), (10, 50)),
        "HyperAcceleratedSkeledroidEntityScript",
        "Branch SQL supplies placed Hyper-Accelerated Skeledroid coordinates; current C# activates this sub-objective during LogicWingRoom1, so phase 12 is WIP-guessed.",
    ),
    InstanceEntitySpec(
        "Map/Instances/Raids/Datascape.sql",
        48374,
        "Datascape Augmented Herald of Avatus",
        157,
        14,
        ((0, 9100000), (10, 50)),
        "AugmentedHeraldOfAvatusEntityScript",
        "Branch SQL supplies placed Augmented Herald coordinates; current C# activates this sub-objective during LogicWingRoom3, so phase 14 is WIP-guessed.",
    ),
    InstanceEntitySpec(
        "Map/Instances/Raids/Datascape.sql",
        48295,
        "Datascape Warmonger Agratha",
        157,
        19,
        ((0, 12500000), (10, 50)),
        "WarmongerAgrathaEntityScript",
        "Branch SQL supplies placed Warmonger Agratha coordinates; phase 19 is WIP-guessed from the current Datascape event script.",
    ),
    InstanceEntitySpec(
        "Map/Instances/Raids/Datascape.sql",
        48916,
        "Datascape Warmonger Chuna",
        157,
        20,
        ((0, 12500000), (10, 50)),
        "WarmongerChunaEntityScript",
        "Branch SQL supplies placed Warmonger Chuna coordinates; phase 20 is WIP-guessed from the current Datascape event script.",
    ),
    InstanceEntitySpec(
        "Map/Instances/Raids/Datascape.sql",
        48917,
        "Datascape Warmonger Talarii",
        157,
        21,
        ((0, 12500000), (10, 50)),
        "WarmongerTalariiEntityScript",
        "Branch SQL supplies placed Warmonger Talarii coordinates; phase 21 is WIP-guessed from the current Datascape event script.",
    ),
    InstanceEntitySpec(
        "Map/Instances/Raids/Datascape.sql",
        48177,
        "Datascape Grand Warmonger Targresh",
        157,
        22,
        ((0, 14500000), (10, 50)),
        "GrandWarmongerTargreshEntityScript",
        "Branch SQL supplies placed Grand Warmonger Targresh coordinates; phase 22 is WIP-guessed from the current Datascape event script.",
    ),
    InstanceEntitySpec(
        "Map/Instances/Raids/Datascape.sql",
        30505,
        "Datascape Avatus",
        157,
        26,
        ((0, 72000000), (10, 50)),
        "DatascapeAvatusEntityScript",
        "Branch SQL supplies placed Avatus coordinates; phase 26 is WIP-guessed from the current Datascape event script's Avatus phase.",
    ),
)


# WIP/GUESSED: the official Evil from the Ether world DB already contains these
# rows, so duplicating them as new entity ids would be wrong. Setup applies
# narrow coordinate-keyed updates after the official world import instead. The
# area updates are branch-authored WIP data for current route/interactables, and
# the crew-log updates keep branch checklist ordering for CrewLogEntityScript.
OFFICIAL_ENTITY_UPDATE_SPECS = (
    OfficialEntityUpdateSpec(
        "Evil from the Ether Captain Weir area",
        3404,
        70999,
        "-422.1375",
        "-844.95026",
        "122.07739",
        (("area", "4836"),),
        "Branch SQL narrows Captain Weir to area 4836; official import has the same coordinate with area 0. WIP/GUESSED pending manual expedition smoke.",
    ),
    OfficialEntityUpdateSpec(
        "Evil from the Ether Gather Ring area",
        3404,
        71003,
        "-396.43555",
        "-840.7188",
        "119.74138",
        (("area", "4836"),),
        "Branch SQL narrows the gather ring used by the current WIP GoToAirlock trigger to area 4836; official import has the same coordinate with area 0.",
    ),
    OfficialEntityUpdateSpec(
        "Evil from the Ether Medbay Door Control area",
        3404,
        71283,
        "54.22043",
        "-848.04",
        "-159.99998",
        (("area", "4823"),),
        "Branch SQL narrows the medbay door control used by the current WIP RepairDoor cleanup to area 4823; official import has the same coordinate with area 0.",
    ),
    OfficialEntityUpdateSpec(
        "Evil from the Ether Spare Parts Crate 1 area",
        3404,
        71322,
        "92.72337",
        "-852.9272",
        "-110.27059",
        (("area", "4823"),),
        "Branch SQL narrows this ScavengeSpareParts crate to area 4823; official import has the same coordinate with area 0. Objective timing remains WIP/GUESSED.",
    ),
    OfficialEntityUpdateSpec(
        "Evil from the Ether Spare Parts Crate 2 area",
        3404,
        71322,
        "93.24837",
        "-852.92975",
        "-74.36311",
        (("area", "4823"),),
        "Branch SQL narrows this ScavengeSpareParts crate to area 4823; official import has the same coordinate with area 0. Objective timing remains WIP/GUESSED.",
    ),
    OfficialEntityUpdateSpec(
        "Evil from the Ether Spare Parts Crate 3 area",
        3404,
        71322,
        "65.68544",
        "-852.9225",
        "-140.38359",
        (("area", "4823"),),
        "Branch SQL narrows this ScavengeSpareParts crate to area 4823; official import has the same coordinate with area 0. Objective timing remains WIP/GUESSED.",
    ),
    OfficialEntityUpdateSpec(
        "Evil from the Ether Repaired Medbay Door Control area",
        3404,
        71323,
        "54.22043",
        "-848.0354",
        "-159.99869",
        (("area", "4823"),),
        "Branch SQL narrows the repaired medbay door control to area 4823; official import has the same coordinate with area 0. Repair-door cleanup remains WIP/GUESSED.",
    ),
    OfficialEntityUpdateSpec(
        "Evil from the Ether Crew Log 1",
        3404,
        71234,
        "31.47145",
        "-849.1181",
        "-152.4904",
        (("questChecklistIdx", "0"),),
        "Branch and official SQL agree on this row; it is included so verification can assert all seven crew logs.",
    ),
    OfficialEntityUpdateSpec(
        "Evil from the Ether Crew Log 2",
        3404,
        71234,
        "-65.33878",
        "-844.1473",
        "-150.08818",
        (("questChecklistIdx", "1"),),
        "Branch checklist index drives CrewLog2; official DB imports this coordinate as index 3.",
    ),
    OfficialEntityUpdateSpec(
        "Evil from the Ether Crew Log 3",
        3404,
        71234,
        "68.89046",
        "-849.1848",
        "-182.18137",
        (("questChecklistIdx", "2"),),
        "Branch checklist index drives CrewLog3; official DB imports this coordinate as index 1.",
    ),
    OfficialEntityUpdateSpec(
        "Evil from the Ether Crew Log 4",
        3404,
        71234,
        "44.647617",
        "-840.1598",
        "184.907",
        (("questChecklistIdx", "3"),),
        "Branch checklist index drives CrewLog4; official DB imports this coordinate as index 6.",
    ),
    OfficialEntityUpdateSpec(
        "Evil from the Ether Crew Log 5",
        3404,
        71234,
        "-32.94068",
        "-843.9746",
        "143.43298",
        (("questChecklistIdx", "4"),),
        "Branch checklist index drives CrewLog5; official DB imports this coordinate as index 5.",
    ),
    OfficialEntityUpdateSpec(
        "Evil from the Ether Crew Log 6",
        3404,
        71234,
        "18.839134",
        "-840.56036",
        "-113.37005",
        (("questChecklistIdx", "5"),),
        "Branch checklist index drives CrewLog6; official DB imports this coordinate as index 2.",
    ),
    OfficialEntityUpdateSpec(
        "Evil from the Ether Crew Log 7",
        3404,
        71234,
        "-17.037079",
        "-843.8909",
        "46.10919",
        (("questChecklistIdx", "6"),),
        "Branch checklist index drives CrewLog7; official DB imports this coordinate as index 4.",
    ),
)


@dataclass(frozen=True)
class EntityRow:
    source_file: str
    source_index: int
    values: dict[str, str]

    @property
    def creature(self) -> int:
        return int(self.values["creature"], 0)


@dataclass(frozen=True)
class InstanceEntityRow:
    entity_id: int
    spec: InstanceEntitySpec
    source_row: EntityRow
    values: dict[str, str]


def default_laughingws_path(repo_root: Path) -> Path:
    return repo_root / "artifacts" / "external" / "LaughingWS.WorldDatabase.New-Zones-and-more"


def normalise_source_column(column: str) -> str:
    name = normalise_column(column)
    return COLUMN_NAME_MAP.get(name.lower(), name)


def resolve_token(token: str, world_id: int | None) -> str:
    value = token.strip()
    if value.upper() == "@WORLD":
        if world_id is None:
            raise ValueError("@WORLD used before a SET @WORLD declaration")
        return str(world_id)
    return value


def parse_world_variable(text: str) -> int | None:
    match = SET_WORLD_RE.search(text)
    if match:
        return int(match.group(1))
    return None


def strip_sql_line_comments(text: str) -> str:
    return "\n".join(
        line
        for line in text.splitlines()
        if not line.lstrip().startswith("--")
    )


def parse_entity_rows(root: Path, relative_source: str) -> list[EntityRow]:
    path = root / Path(relative_source)
    text = strip_sql_line_comments(read_text(path))
    world_id = parse_world_variable(text)
    rows: list[EntityRow] = []

    for insert_match in INSERT_ENTITY_RE.finditer(text):
        columns = [
            normalise_source_column(column)
            for column in split_top_level_commas(insert_match.group("columns"))
        ]

        for row_sql in split_value_rows(insert_match.group("values")):
            raw_values = split_top_level_commas(row_sql)
            if len(raw_values) != len(columns):
                raise ValueError(
                    f"{relative_source}: value count {len(raw_values)} does not match column count {len(columns)}"
                )

            values = {
                column: resolve_token(raw_values[index], world_id)
                for index, column in enumerate(columns)
            }

            if "creature" not in values or "world" not in values:
                continue

            for column in OUTPUT_COLUMNS:
                values.setdefault(column, "0")

            rows.append(EntityRow(relative_source, len(rows), values))

    return rows


def build_instance_rows(root: Path) -> list[InstanceEntityRow]:
    rows_by_source: dict[str, list[EntityRow]] = {}
    instance_rows: list[InstanceEntityRow] = []

    for index, spec in enumerate(INSTANCE_ENTITY_SPECS):
        source_path = root / Path(spec.source_file)
        if not source_path.is_file():
            raise FileNotFoundError(f"LaughingWS source does not exist: {source_path}")

        if spec.source_file not in rows_by_source:
            rows_by_source[spec.source_file] = parse_entity_rows(root, spec.source_file)

        matches = [
            row
            for row in rows_by_source[spec.source_file]
            if row.creature == spec.source_creature
        ]
        if spec.source_match_index >= len(matches):
            raise ValueError(
                f"{spec.source_file}: expected row index {spec.source_match_index} for creature "
                f"{spec.source_creature}, found {len(matches)} matching row(s)"
            )
        if spec.label in SKIPPED_DUPLICATE_INSTANCE_ENTITY_LABELS:
            continue

        instance_rows.append(
            InstanceEntityRow(
                entity_id=BASE_INSTANCE_ENTITY_ID + index + 1,
                spec=spec,
                source_row=matches[spec.source_match_index],
                values=dict(matches[spec.source_match_index].values),
            )
        )

    return instance_rows


def sql_identifier(name: str) -> str:
    return "`" + name.replace("`", "``") + "`"


def sql_string(value: str) -> str:
    return "'" + value.replace("\\", "\\\\").replace("'", "''") + "'"


def sql_row(values: Iterable[str]) -> str:
    return "(" + ", ".join(values) + ")"


def duplicate_update_clause(columns: Iterable[str], key_columns: set[str]) -> str:
    column_list = list(columns)
    assignments = [
        f"{sql_identifier(column)} = VALUES({sql_identifier(column)})"
        for column in column_list
        if column not in key_columns
    ]
    if not assignments:
        first_column = column_list[0]
        assignments.append(f"{sql_identifier(first_column)} = VALUES({sql_identifier(first_column)})")
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


def append_official_entity_update_block(
    lines: list[str],
    updates: tuple[OfficialEntityUpdateSpec, ...],
) -> None:
    if not updates:
        return

    lines.append(f"-- official entity reconciliation: {len(updates)} row update(s)")
    for spec in updates:
        assignments = ", ".join(
            f"{sql_identifier(column)} = {value}"
            for column, value in spec.updates
        )
        lines.append(f"--   {spec.label}: {spec.evidence}")
        lines.append(f"UPDATE {sql_identifier('entity')}")
        lines.append(f"SET {assignments}")
        lines.append("WHERE " + " AND ".join((
            f"{sql_identifier('world')} = {spec.world}",
            f"{sql_identifier('creature')} = {spec.creature}",
            f"ABS({sql_identifier('x')} - ({spec.x})) < 0.001",
            f"ABS({sql_identifier('y')} - ({spec.y})) < 0.001",
            f"ABS({sql_identifier('z')} - ({spec.z})) < 0.001",
        )) + ";")
    lines.append("")


def build_entity_values(rows: list[InstanceEntityRow]) -> list[list[str]]:
    return [
        [str(row.entity_id)] + [row.values[column] for column in OUTPUT_COLUMNS[1:]]
        for row in rows
    ]


def build_entity_event_values(rows: list[InstanceEntityRow]) -> list[list[str]]:
    return [
        [str(row.entity_id), str(row.spec.event_id), str(row.spec.phase)]
        for row in rows
        if row.spec.event_id is not None and row.spec.phase is not None
    ]


def build_entity_script_values(rows: list[InstanceEntityRow]) -> list[list[str]]:
    return [
        [str(row.entity_id), sql_string(row.spec.script_name)]
        for row in rows
        if row.spec.script_name is not None
    ]


def build_entity_stat_values(rows: list[InstanceEntityRow]) -> list[list[str]]:
    output_rows: list[list[str]] = []
    for row in rows:
        for stat, value in row.spec.stats:
            output_rows.append([str(row.entity_id), str(stat), str(value)])
    return output_rows


def write_seed(output_path: Path, rows: list[InstanceEntityRow]) -> None:
    output_path.parent.mkdir(parents=True, exist_ok=True)

    lines = [
        "-- Generated by Tools/DataMapping/extract_laughingws_instance_entities.py.",
        "-- Source: selected LaughingWS instance entity/event/script rows with matching current WIP script support.",
        "-- Purpose: preserve useful instance-local rows without importing source world deletes or @GUID allocation.",
        "-- WIP/GUESSED: instance placement/choreography is branch-authored; manual client smoke is required before retail parity is claimed.",
        "",
        "USE `nexus_forever_world`;",
        "",
        "START TRANSACTION;",
        "",
        "-- Entity evidence and WIP scope:",
    ]

    for row in rows:
        event_scope = (
            f"event {row.spec.event_id} phase {row.spec.phase}"
            if row.spec.event_id is not None and row.spec.phase is not None
            else "no source entity_event row"
        )
        lines.append(
            f"--   {row.entity_id}: {row.spec.label}; source creature {row.spec.source_creature}; "
            f"{event_scope}; {row.spec.evidence}"
        )
    lines.append("")

    append_insert_block(lines, "entity", OUTPUT_COLUMNS, build_entity_values(rows), {"id"})
    append_insert_block(lines, "entity_event", ("id", "eventId", "phase"), build_entity_event_values(rows), {"id", "eventId", "phase"})
    append_insert_block(lines, "entity_script", ("id", "scriptName"), build_entity_script_values(rows), {"id", "scriptName"})
    append_insert_block(lines, "entity_stats", ("id", "stat", "value"), build_entity_stat_values(rows), {"id", "stat"})
    append_official_entity_update_block(lines, OFFICIAL_ENTITY_UPDATE_SPECS)

    lines.append("-- Sources:")
    for source in sorted({row.spec.source_file for row in rows}):
        lines.append(f"--   {source}")
    lines.append("--   I:/GIT/NexusForever.WorldDatabase/Instance/Expedition/Evil from the Ether.sql")
    lines.extend(["", "COMMIT;", ""])

    output_path.write_text("\n".join(lines), encoding="ascii", newline="\n")


def parse_args() -> argparse.Namespace:
    repo_root = Path(__file__).resolve().parents[2]
    parser = argparse.ArgumentParser(
        description="Extract WIP LaughingWS instance entity/event rows into a safe seed.",
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
        default=repo_root / "Tools" / "DataMapping" / "sql" / "laughingws_instance_entity_wip_seed.sql",
        help="Output SQL seed path.",
    )
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    laughingws_path = args.laughingws_path.resolve()
    if not laughingws_path.is_dir():
        raise SystemExit(f"LaughingWS path does not exist: {laughingws_path}")

    rows = build_instance_rows(laughingws_path)
    write_seed(args.output_sql, rows)

    print(
        f"Wrote {len(rows)} WIP instance entity row(s), "
        f"{sum(len(row.spec.stats) for row in rows)} entity_stats row(s), "
        f"{len([row for row in rows if row.spec.script_name is not None])} entity_script row(s), "
        f"{len(build_entity_event_values(rows))} entity_event row(s), "
        f"and {len(OFFICIAL_ENTITY_UPDATE_SPECS)} official row update statement(s) to {args.output_sql.resolve()}."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
