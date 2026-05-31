# World Server Modularization Plan

## Goal
Reduce the size and coupling of the world-server codebase without prematurely
turning live gameplay into a distributed system.

Success means the world runtime remains easy to launch locally, compile times
and ownership boundaries improve, feature areas become easier to test in
isolation, and any future process split has an explicit state ownership and
message-ordering model.

## Current Shape

The repository is already partially service-oriented:

- `NexusForever.Aspire.AppHost` composes separate auth, STS, group, chat,
  friendship, character, account API, character API, database migration, and
  world server processes.
- `NexusForever.WorldServer` is the executable host for the client-facing world
  process. It wires hosted services, internal broker handlers, world networking,
  game tables, scripts, shared services, and gameplay state.
- `NexusForever.Game` already owns most authoritative gameplay domains: entity,
  map, combat, quest, spell, guild, housing, marketplace, matching, account,
  achievement, public event, story, storefront, and related managers.
- `NexusForever.Network.World` owns world packet models and protocol-level
  network pieces.
- `NexusForever.Script` and `NexusForever.Script.*` already model content/script
  modularity as separate assemblies.

The next modularization step should therefore focus on clearer assemblies and
dependency direction before adding more long-running executables.

## Guiding Principles

- Keep one authoritative world simulation process until a feature has a proven
  asynchronous boundary.
- Prefer class-library extraction over process extraction for tightly coupled
  gameplay state.
- Keep client packet handlers close to the world process unless their logic can
  be expressed as calls into a stable gameplay service.
- Move reusable domain logic out of `NexusForever.WorldServer` before moving it
  out of process.
- Avoid introducing cross-process calls on hot paths such as movement, combat,
  visibility, spell execution, quest progress, and entity create/update
  emission.
- Every extracted module should compile, test, and register services without
  requiring the full world executable when practical.

## Non-Goals

- Do not split map, entity, combat, spell, quest, movement, or visibility state
  into separate processes in the first pass.
- Do not change packet behavior as part of a mechanical project split.
- Do not redesign persistence, broker contracts, or database ownership unless a
  later process split requires it.
- Do not create artificial abstractions around every manager. Extract only where
  boundaries are already visible or repeated friction exists.

## Candidate Library Splits

### 1. World Host Shell

Keep `NexusForever.WorldServer` as the executable composition root.

It should own:

- process startup and configuration loading;
- hosted service registration;
- embedded web console hosting;
- world TCP endpoint hosting;
- process-level shutdown behavior;
- final dependency wiring across game, network, script, database, and internal
  broker services.

Acceptance:

- The executable project becomes small enough that most feature work happens in
  referenced libraries.
- Local setup and Aspire launch still start the same `world-server` process.

### 2. World Network Handlers Library

Extract client-facing world packet handlers from
`NexusForever.WorldServer.Network.Message.Handler` into a library such as
`NexusForever.WorldServer.Network.Handlers` or
`NexusForever.World.Handlers`.

The library should own:

- client opcode handler classes;
- handler registration extension methods;
- thin adapter logic that validates session state and calls game services;
- small packet-specific helper classes that are not reusable gameplay logic.

Keep in the executable or network infrastructure projects:

- socket/session transport;
- world endpoint startup;
- low-level packet model definitions already owned by `NexusForever.Network.World`.

Acceptance:

- Handler registration still discovers or registers all existing handlers.
- No handler behavior changes are required for the extraction.
- A focused build of the handler library catches handler compile errors without
  rebuilding the executable host.

### 3. World Internal Broker Handlers Library

Extract internal message broker handlers from
`NexusForever.WorldServer.Network.Internal.Handler` into a separate library if
their dependencies remain clean after the packet-handler split.

The library should own:

- group/chat/friendship/player internal message handlers consumed by the world
  process;
- identity and DTO mapping helpers used only by those handlers;
- registration of internal handlers.

Acceptance:

- Broker subscription startup remains in the world executable hosted service.
- Internal handlers depend on game/session abstractions rather than the full
  world host where practical.

### 4. World Admin And Support Library

Extract lower-risk world-server features that are not part of the hot gameplay
loop:

- command parsing and command contexts;
- embedded web-console command bridge;
- support submission store;
- leaderboard provider, score ingestion, and stores.

Suggested names:

- `NexusForever.WorldServer.Admin`
- `NexusForever.WorldServer.Support`
- `NexusForever.WorldServer.Leaderboard`

This can be one library first, then split further only if the dependency graph
justifies it.

Acceptance:

- Web console and in-game commands still work through the same user-facing
  interfaces.
- Leaderboard request handlers depend on a provider abstraction rather than
  concrete world-host types.

### 5. Gameplay Domain Libraries

After the world-server shell is thinner, consider splitting selected domains out
of `NexusForever.Game` into libraries. Start with domains whose dependencies are
already narrow and whose state ownership is clear.

Safer early candidates:

- achievement rules and progress helpers;
- prerequisite evaluation;
- retail rule helpers;
- fortune reward pools;
- leaderboard scoring rules if moved from world host;
- chat formatting;
- static data lookup helpers.

Higher-risk candidates to defer:

- entity manager and player state;
- map manager and visibility state;
- combat, threat, spell, and proc runtime;
- quest manager and public-event runtime;
- housing runtime while it is tightly coupled to entity/session state;
- marketplace and mail until persistence and service ownership are clarified.

Acceptance:

- Extracted gameplay libraries expose service-registration extension methods
  matching the existing `AddGame...` pattern.
- `NexusForever.Game` can remain an aggregator package temporarily while callers
  move to narrower references.

## Candidate Process Splits

Treat new executables as a later phase. A feature is process-ready only when it
has all of these properties:

- a single clear owner for persistent state;
- broker/API contracts that can tolerate retries and duplicate messages;
- no dependency on synchronous reads of mutable entity/session/map state;
- explicit failure behavior when the service is unavailable;
- startup ordering represented in Aspire and standalone setup scripts;
- tests or smoke scripts for the service boundary.

Likely candidates:

- leaderboard aggregation;
- marketplace or auction style services;
- mail and account reward delivery;
- support/admin ingestion;
- matchmaking coordinator;
- analytics, audit, and maintenance jobs.

Defer until much later:

- map workers;
- combat workers;
- quest workers;
- spell workers;
- visibility/entity replication workers.

Those features sit on hot, shared, ordering-sensitive state. Splitting them too
early would force a distributed simulation design before the emulator has stable
single-process behavior.

## Phased Implementation

### Phase 0: Baseline And Dependency Map

Tasks:

- Record a clean build baseline for `NexusForever.WorldServer` and relevant
  tests.
- Generate or manually document project references for `NexusForever.WorldServer`,
  `NexusForever.Game`, `NexusForever.Network.World`, `NexusForever.Script`, and
  existing service binaries.
- Identify namespaces in `NexusForever.WorldServer` that already have few
  references back to host-only types.
- Identify handlers that contain gameplay logic worth moving into
  `NexusForever.Game` before extraction.

Acceptance:

- A short dependency map exists in this plan or a follow-up note.
- The first extraction target is chosen based on low coupling and low runtime
  risk.

### Phase 1: Extract Low-Risk World Modules

Start with admin/support/leaderboard or packet handlers, whichever has the
cleaner dependency graph at the time.

Tasks:

- Create the new class-library project.
- Move files without changing namespaces unless a namespace cleanup is part of
  the same reviewed change.
- Add project references from the new library to the existing game/network/shared
  projects it actually uses.
- Update `NexusForever.WorldServer` references and service registration.
- Build the new library and then the world-server executable.

Acceptance:

- Runtime behavior is unchanged.
- Project references do not create cycles.
- The extracted module has a focused build command.

### Phase 2: Make Handlers Thin

Tasks:

- For packet handlers that contain durable gameplay rules, move rules into
  `NexusForever.Game` services or feature libraries.
- Keep packet handlers responsible for session validation, packet conversion,
  and response emission.
- Add focused tests around moved gameplay rules when the behavior is regression
  prone.

Acceptance:

- New handlers mostly orchestrate existing services.
- Gameplay behavior can be tested without constructing a network session where
  practical.

### Phase 3: Split Selected Game Domains

Tasks:

- Extract the safest `NexusForever.Game` subdomains one at a time.
- Preserve the existing `AddGame()` aggregate registration while adding narrower
  registration methods.
- Move tests with the extracted domain when helpful.

Acceptance:

- `NexusForever.Game` becomes more of an aggregator and shared gameplay surface.
- Feature libraries do not depend on `NexusForever.WorldServer`.

### Phase 4: Consider New Runtime Services

Only after the library boundaries are clear, choose one asynchronous process
candidate such as leaderboard aggregation or matchmaking coordination.

Tasks:

- Define ownership of database tables, messages, and retry behavior.
- Add broker/API contracts before moving runtime state.
- Add Aspire registration and standalone setup/launch script changes.
- Add smoke verification for unavailable-service and restart behavior.

Acceptance:

- The new service can be stopped and restarted without corrupting world state.
- Local developer startup remains documented and practical.
- The world process degrades or fails explicitly when the service is unavailable.

## Verification Strategy

For documentation-only updates:

- No build is required.

For library extraction changes:

- `dotnet build Source\NexusForever.WorldServer\NexusForever.WorldServer.csproj --no-restore -v minimal --nologo`
- Build the new owning project directly.
- Run focused tests when gameplay logic moves.

For process splits:

- Build the full solution.
- Run the affected service through Aspire.
- Run standalone setup/launch scripts if they were changed.
- Exercise at least one live world login-to-feature smoke for user-visible
  functionality.

## First Recommended Slice

The best first slice is a low-risk library extraction, not a runtime service.

Recommended order:

1. Extract support/leaderboard/admin console support from `NexusForever.WorldServer`.
2. Extract client packet handlers into a world-handler library once the service
   registration shape is clear.
3. Move durable gameplay rules found inside handlers into `NexusForever.Game` or
   narrower gameplay libraries.
4. Revisit process splits only after these boundaries are stable.

This gives the project immediate compile and ownership benefits while preserving
the single-process world simulation model that is safest for the current runtime.