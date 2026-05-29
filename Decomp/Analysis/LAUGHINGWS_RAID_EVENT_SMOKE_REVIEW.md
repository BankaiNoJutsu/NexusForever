# LaughingWS Raid And Event-Instance Smoke Review

Date: 2026-05-27

## Decision

LWS-110 through LWS-117 remain mapped-only raid/event-instance smoke blockers.
Current WIP scaffolds are covered by focused tests, and
`Start-BlockerEvidenceHarness.ps1 -RaidEventSmoke` now creates a targeted proof
bundle for the required live-client pass, but exact mechanics, routing, doors,
cinematics, rewards, and full smoke remain unproven.

Verification:

```powershell
dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~InitializationCore|FullyQualifiedName~RedMoonTerror|FullyQualifiedName~GeneticArchives|FullyQualifiedName~Datascape|FullyQualifiedName~UltimateProtogames|FullyQualifiedName~ShadesEve|FullyQualifiedName~ProtostarsSuperMall|FullyQualifiedName~JourneyIntoOMNICore" -v minimal --nologo
```

Result: `169/169` passed.

Follow-up harness verification:

```powershell
.\Decomp\Analysis\Start-BlockerEvidenceHarness.ps1 -RaidEventSmoke -CreateBundleOnly -OutputRoot artifacts\blocker_evidence_test
```

Result: created `20260528-014548-LWS-110-117-raid-event-instances` with the
`lws-110-117-raid-event-targets.md` worksheet, default worlds/events, and
negative cases for premature triggers/target sets, premature
door/elevator/gate/room interactions, wrong-route weekly/wing/room selection,
completion-only cinematics, and early-finish rewards.
`Decomp/Analysis/test_blocker_evidence_harness_presets.py` now dry-runs the
preset with `-CreateBundleOnly` and verifies its manifest, target worksheet,
default world/public-event ids, helper files, and negative-case scaffold.

| Task | Closure |
| --- | --- |
| LWS-110 Initialization Core Y-83 | Mapped-only blocker. Trigger placement, door choreography, communicator/cinematic timing, target-set proof, real open-door cinematic payload, and raid smoke remain blocked. |
| LWS-111 Red Moon Terror | Mapped-only blocker. Ish'amel/engineering timing, Laveka choreography, awakening/challenge mechanics, door/elevator movement, and raid smoke remain blocked. |
| LWS-112 Genetic Archives | Mapped-only blocker. Experiment X-89, Kuralak, Kuralak pillar, Ohmna, weekly/random selection, boss choreography, communicator/cinematic timing, door/elevator movement, and raid smoke remain blocked. |
| LWS-113 Datascape | Mapped-only blocker. Hydroflux/Mnemesis mechanics, communicator/cinematic timing, encounter choreography, door/trigger placement, wing order, challenge mechanics, and raid smoke remain blocked. |
| LWS-114 Ultimate Protogames raid | Mapped-only blocker. Downsizer challenge semantics, boss mechanics, rewards, and raid smoke remain blocked. |
| LWS-115 Shade's Eve | Mapped-only blocker. Gather-ring cleanup, town-gate opening, communicator/cinematic timing, real cinematic payload, vote follow-up, Etty/fountain interaction smoke, and event smoke remain blocked. |
| LWS-116 Protostar SuperMall in the Sky | Mapped-only blocker. Store/room routing, encounter logic, real cinematic payload, rewards, and full smoke remain blocked. |
| LWS-117 Journey into OMNICore-1 | Mapped-only blocker. Real cinematic actor/camera/text/timing payload, event routing, rewards, and encounter behavior remain blocked. |
