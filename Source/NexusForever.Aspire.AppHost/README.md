# Aspire runtime assets

Asset preparation is opt-in. Existing deployments continue using their configured
table/map directories unless `Assets:Prepare` is enabled.

```powershell
dotnet run --project Source/NexusForever.Aspire.AppHost -- `
  --Assets:Prepare=true `
  --Assets:ClientPath="D:\WildStar\Client64" `
  --Assets:MaxParallelism=2
```

`Assets:Root` defaults to the repository's `.nexusforever-runtime/assets` directory.
Relative roots and client paths resolve against the repository directory; all
resource arguments and service paths use absolute paths. `Assets:ClientPath`
accepts a client installation or its `Patch` directory. Omit it to reuse a complete
validated cache. An invalid cache without a usable client fails preparation.
`Assets:MaxParallelism` accepts 0 through 8; 0 uses the generator's bounded automatic
selection. The cap applies to this opt-in orchestration workload.

The `asset-preparation` MapGenerator resource runs `--prepare`, validating the full
cache inventory before reuse and staging any repair. World, Chat, and Friendship
wait for its successful exit (code 0) and receive the shared `tbl` path. World also
receives the shared `map` path. Their existing database, migration, broker, and API
dependencies remain in place for both MySQL and SQLite configurations. Auth, STS,
APIs, and other services retain their existing startup dependencies.

These completion waits govern local Aspire execution. Deployment publishing does
not provide this startup gate: prepare/validate assets explicitly in deployment
automation before starting consumers.

The model regression tests run without starting Docker:

```powershell
dotnet test Source/NexusForever.Game.Tests --filter FullyQualifiedName~AssetPreparationCompositionTests
```
