# Aspire asset preparation reconciliation (#570)

Status: implemented with composition regression coverage. This adopts the asset
preparation dependency and shared-path portion of upstream PR #570 while retaining
the fork's MySQL and SQLite startup paths and existing runtime defaults.

## Implementation

`NexusForever.Aspire.AppHost/AssetPreparationContext.cs` creates an opt-in
MapGenerator project resource. `Assets:Prepare` defaults to false. Enabled runs use
`--prepare --output <absolute root> --maxParallelism <0..8>` and optional
`--patchPath <absolute client path>`. The default cache is the repository's
`.nexusforever-runtime/assets`; relative configured paths resolve against that
repository. A validated existing cache can be used without a client path.

World, Chat, and Friendship each wait for preparation completion with exit code 0
and use the shared `tbl` directory. World also uses the shared `map` directory.
Their database/migration, broker, and API dependencies remain intact. A typed
context is passed into each provider composition; no resource is retained in a
static field or shared between application builders. Auth, STS, APIs, Group, and
Character retain their existing asset-independent startup behavior.

MapGenerator's staged repair, cache inventory validation, marker/lock behavior,
and preservation of custom files are covered separately by `AssetPreparationTests`.
No entitlement configuration or defaults are changed by AppHost wiring.

## Evidence and verification

- Input: assessment snapshot `remaining-goal/feature-pr-570.diff`, especially its
  MapGenerator resource, completion waits, and table/map environment overrides.
- Local contracts: MapGenerator `Parameters.cs`, `AssetPreparation.cs`, existing
  World/Chat/Friendship example configuration, and Aspire.Hosting 13.4.6 package
  XML documentation for completion waits and execution-configuration gathering.
- `AssetPreparationCompositionTests`: 13 passing cases covering both providers,
  successful-completion dependency requirements, exact environment/argument
  configuration including paths with spaces, default-disabled behavior and
  migration waits, cache-only arguments, bounded parallelism, and independent
  application builders. Tests use the current `ExecutionConfigurationBuilder`
  API; no Docker containers or services are started.
- Coordinated `dotnet restore Source/NexusForever.slnx` passed. Focused test build
  and execution passed without warnings/errors. Parent owns the final combined
  solution build/test gate.

## Limits

This is a local Aspire startup gate. Aspire's `WaitForCompletion` has no deployment
publish effect; deployment automation must validate/prepare assets before starting
consumers. Docker-hosted execution remains a separate gate because Docker Desktop
is unavailable in this assessment environment. The isolated direct-process live
runtime smoke is recorded separately and is not an Aspire orchestration claim.
The full upstream bootstrap, world imports, account provisioning, and unrelated
settings/health endpoint changes are outside this asset wiring slice.
