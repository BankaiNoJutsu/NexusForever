# LaughingWS PvP And Adventure Smoke Review

Date: 2026-05-27

## Decision

LWS-080 through LWS-085 remain mapped-only smoke blockers. Existing
branch-derived scaffolds are covered by focused tests, but queue/match smoke,
scoring, rewards, stats, and deeper adventure routing remain unproven.

Evidence harness progress (2026-05-28): `Start-BlockerEvidenceHarness.ps1`
now has a `-PvpAdventureSmoke` preset that creates an LWS-080 through LWS-085
worksheet with the known branch worlds/events, queue/match lifecycle, objective,
scoreboard, reward, PvP stat, faction-start, vehicle-choice, and negative-case
capture requirements. Parser validation and create-only bundle generation were
verified after adding the preset.
`Decomp/Analysis/test_blocker_evidence_harness_presets.py` now dry-runs the
preset with `-CreateBundleOnly` and verifies its manifest, target worksheet,
default world/public-event ids, helper files, and negative-case scaffold.

Verification for the current scaffolds:

```powershell
dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~PvpAdventureBranchScriptTests|FullyQualifiedName~RageLogicEventScriptTests|FullyQualifiedName~InstanceMapBindingTests" -v minimal --nologo
```

Result: `29/29` passed.

Follow-up verification (2026-05-28): the same focused filter passed `29/29`
after adding the harness preset.

## LWS-080 Cryo-Plex Arena

Mapped-only blocker. Current code ports Cryo-Plex public event `581`, sub-event
`582`, world `3022`, the branch-derived arena phases, forcefield release,
death-stat update, timed holocrypt resurrection, and team-spawn reset as
WIP/GUESSED scaffold behavior.

Still required: queue/match smoke, score/capture/round flow, rewards, PvP stat
capture, and exact arena parity.

## LWS-081 Daggerstone Pass

Mapped-only blocker. The map binding creates/joins/finishes public events
`438`/`466` for world `2166` through the current PvP content-map base.

Still required: queue/start/end smoke, scoring, rewards, round/capture
objectives, and stats.

## LWS-082 Halls Of The Bloodsworn

Mapped-only blocker. The map binding creates/joins/finishes public events
`876`/`877` for world `3449` through the current PvP content-map base.

Still required: queue/start/end smoke, scoring, rewards, objective flow, and
stats.

## LWS-083 Walatiki Temple

Mapped-only blocker. The map binding creates/joins/finishes public events
`217`/`366` for world `797` through the current PvP content-map base.

Still required: mask capture/return/scoring, round end, rewards, stats, and
manual battleground smoke.

## LWS-084 War Of The Wilds

Mapped-only blocker. The branch-mapped base fight scaffold for public event
`158`, world `1393`, and the giant Moodie totem objectives is WIP/GUESSED and
covered by focused tests.

Still required: faction-specific start events `170` and `171`, end-delay
behavior, chat timing, rewards, adventure smoke, and optional objective parity.

## LWS-085 Rage Logic

Mapped-only blocker. The branch `ChooseAVehicle` phase is set as WIP/GUESSED
behavior and covered by the current map/event scaffold tests.

Still required: vehicle choice, objective routing, rewards, encounter behavior,
and full adventure smoke before widening beyond the current phase.
