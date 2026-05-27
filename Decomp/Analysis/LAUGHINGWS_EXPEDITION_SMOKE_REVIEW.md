# LaughingWS Expedition Smoke Review

Date: 2026-05-27

## Decision

LWS-090 through LWS-096 are closed as mapped-only expedition smoke blockers.
The current WIP expedition scaffolds are covered by focused tests, but exact
doors, triggers, cinematics, cleanup, rewards, and full route behavior remain
unproven.

Verification for current WIP scaffolds:

```powershell
dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj --filter "FullyQualifiedName~SpaceMadnessEventScriptTests|FullyQualifiedName~FragmentZeroEventScriptTests|FullyQualifiedName~GauntletEventScriptTests|FullyQualifiedName~InfestationEventScriptTests|FullyQualifiedName~EvilFromTheEtherEventScriptTests|FullyQualifiedName~InstanceMapBindingTests" -v minimal --nologo
```

Result: `75/75` passed.

## Row Closures

| Task | Closure |
| --- | --- |
| LWS-090 Outpost M-13 | Rejected until proof. The branch shuttle trigger id `5` is zero-vector and remains disabled until table/client proof maps a real shuttle world-location trigger. |
| LWS-091 Space Madness | Mapped-only blocker. Current objective chain and WIP triggers are tested; branch door ids, direct local teleport, airlock/research timing, real cinematic payload, combat/spawn behavior, and full smoke remain blocked. |
| LWS-092 Fragment Zero | Mapped-only blocker. Current conservative event chain, early triggers/callouts, and completion-only cinematics are tested; trigger timing, real cinematic payload, communicator timing, door behavior, and entity cleanup remain blocked. |
| LWS-093 Gauntlet | Mapped-only blocker. Current objective chain and WIP participant-gather triggers are tested; cinematics, announcer/communicator timing, arena doors, exact trigger timing, and full smoke remain blocked. |
| LWS-094 Infestation | Mapped-only blocker. Current event chain, WIP turnstile trigger, and completion-only cinematic are tested; cargo-ship placement/range, door/open-vent choreography, real cinematic payload, medical-bay attack timing, parasite objective activation, and full smoke remain blocked. |
| LWS-095 Evil from the Ether | Mapped-only blocker. Current drive-spark/crew-log/area WIP route pieces are tested; exact trigger rows/coordinates, medbay transition, cleanup, crew-log targeting/order, drive-spark visuals, Katja choreography, boss cadence/mechanics, cinematics, rewards, full smoke, and residual no-hook placements remain blocked. |
| LWS-096 Deep Space Exploration | Mapped-only blocker. Current map-only scaffold activates the branch crew objective and queues a completion-only cinematic placeholder; follow-up routing, doors, real cinematic payload, encounter order, rewards, and full smoke remain blocked. |
