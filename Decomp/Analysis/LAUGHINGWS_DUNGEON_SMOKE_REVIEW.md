# LaughingWS Dungeon Smoke Review

Date: 2026-05-27

## Decision

LWS-100 through LWS-106 remain mapped-only dungeon smoke blockers. The current
WIP dungeon scaffolds are covered by focused tests, and
`Start-BlockerEvidenceHarness.ps1 -DungeonSmoke` now creates a targeted proof
bundle for the required live-client pass, but exact boss mechanics, route
weights, trigger placement, door choreography, cinematics, rewards, and
full-route smoke remain unproven.

Verification:

```powershell
dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~ColdbloodCitadel|FullyQualifiedName~ProtogamesAcademy|FullyQualifiedName~RuinsOfKelVoreth|FullyQualifiedName~StormtalonsLair|FullyQualifiedName~Skullcano|FullyQualifiedName~SanctuaryOfTheSwordmaiden|FullyQualifiedName~UltimateProtogames" -v minimal --nologo
```

Result: `149/149` passed.

Follow-up harness verification:

```powershell
.\Decomp\Analysis\Start-BlockerEvidenceHarness.ps1 -DungeonSmoke -CreateBundleOnly -OutputRoot artifacts\blocker_evidence_test
```

Result: created `20260528-014236-LWS-100-106-dungeons` with the
`lws-100-106-dungeon-targets.md` worksheet, default worlds/events, and negative
cases for premature triggers, premature doors/platforms/launchers/teleporters,
wrong-route optional objectives, completion-only cinematics, and early-finish
rewards.
`Decomp/Analysis/test_blocker_evidence_harness_presets.py` now dry-runs the
preset with `-CreateBundleOnly` and verifies its manifest, target worksheet,
default world/public-event ids, helper files, and negative-case scaffold.

| Task | Closure |
| --- | --- |
| LWS-100 Coldblood Citadel | Current WIP event chain and optional-objective scaffolds are tested; optional route weights, objective trigger placement, door/choreography, Hailstone ability cadence, and full dungeon route smoke remain blocked. |
| LWS-101 Protogames Academy | Current WIP objective chain and branch hooks are tested; Invulnotron, Gromka, Iruki Boldbeard, Seek-N-Slaughter, Icebox Mk. 2 mechanics, trigger timing, communicator timing, platform/launcher cleanup, and entity-removal choreography remain blocked. |
| LWS-102 Ruins of Kel Voreth | Current WIP objective, trigger/message, Blood Pit cinematic placeholder, and death-credit scaffolds are tested; boss mechanics, trigger placement, doors, optional routes, faction communicator targeting, real cinematic payload, and duplicate-owner trigger split remain blocked. |
| LWS-103 Stormtalon's Lair | Current WIP boss/objective and Stormtalon cinematic placeholder scaffolds are tested; boss combat, trigger placement/count, real cinematic payload, optional weights/objectives, and boss spawn/version selection remain blocked. |
| LWS-104 Skullcano | Current WIP boss/objective, route, trigger, platform, and communicator scaffolds are tested; boss mechanics, route weights, Chief Kaskalak choreography, doors/platforms, communicator timing, optional objectives, and full smoke remain blocked. |
| LWS-105 Sanctuary of the Swordmaiden | Current WIP objective, route, trigger/message, and communicator scaffolds are tested; miniboss mechanics, route weights/objectives, trigger placement, doors, communicator timing, and full smoke remain blocked. |
| LWS-106 Ultimate Protogames dungeon | Current WIP entry scaffold stops at the coarse `RandomEvent1` gate; room randomization, objective routing, rewards, boss mechanics, and full smoke remain blocked. |
