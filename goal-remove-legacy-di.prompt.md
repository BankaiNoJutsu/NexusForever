# Goal: Remove Legacy DI And Singleton Bridge

Work in `I:\GIT\NexusForever`. Remove the service-locator/singleton DI bridge
without changing runtime behavior.

Legacy scope:
- `LegacyServiceProvider` and `LegacyServiceProvider.Provider`.
- `Singleton<T>` inheritance and singleton-backed `*.Instance` calls.
- `AddSingletonLegacy` where it only exists for the bridge.
- tests that mutate the global legacy provider.

Do not include old protocol behavior, evidence-gated placeholders, or
`Obsolete` code unless they directly depend on this bridge.

First read `AGENTS.md`, `README.md`, `CURRENT_STATUS.md`, and relevant
registration files. Re-scan nested `AGENTS.md`. Preserve dirty worktree changes.

## Baseline

Run and record production/test counts before editing:

```powershell
git status --short
rg -n "LegacyServiceProvider" Source -g "*.cs"
rg -n "class\s+\w+\s*:\s*Singleton<" Source -g "*.cs"
rg -n "AddSingletonLegacy" Source -g "*.cs"
rg -n "\b[A-Z][A-Za-z0-9_]*\.Instance\b" Source -g "*.cs"
rg -n "LegacyServiceProviderCollection|LegacyServiceProviderScope|RunWithLegacyProvider" Source\NexusForever.Game.Tests -g "*.cs"
```

## Rules

- Preserve gameplay, packets, database, protocol, timing, persistence, and
  evidence-gated behavior.
- Prefer constructor injection of existing interfaces.
- Do not replace `LegacyServiceProvider` with another service locator.
- Use `IServiceProvider`, `IServiceScopeFactory`, or `ActivatorUtilities` only
  at true activation boundaries.
- Keep service lifetimes equivalent.
- Avoid giant constructors; use narrow domain contexts only when needed.
- Keep static helpers static if dependencies can be parameters.
- Do not edit generated/local artifacts.

## Order

1. Remove direct production `LegacyServiceProvider.Provider` calls using
   injected interfaces, parameter passing, factories, or activation services.
2. Migrate low-fanout `Singleton<T>` managers first. Replace `Manager.Instance`
   with injected interfaces or parameters, then remove inheritance.
3. Migrate `SharedConfiguration.Instance` to typed options or injected config.
4. Migrate `DatabaseManager.Instance` to `IDatabaseManager` or narrower stores;
   preserve transaction and save ordering.
5. Migrate `GameTableManager.Instance` by subsystem. Inject `IGameTableManager`
   into services; pass exact entries/tables where sufficient; extend
   entity/map/script factories or init contexts where required.
6. Migrate remaining singleton managers.
7. Replace test global-provider mutation with direct fakes, builders, normal
   non-global `ServiceProvider` fixtures, or focused fake options/game-table/db
   services.
8. When zero callers remain, delete `LegacyServiceProvider.cs`, `Singleton.cs`,
   legacy test scopes/collections/helpers, and `AddSingletonLegacy`.

## Verify

Run the narrowest useful command after each slice:

```powershell
dotnet build Source\NexusForever.Game\NexusForever.Game.csproj --no-restore -v minimal --nologo
dotnet build Source\NexusForever.WorldServer\NexusForever.WorldServer.csproj --no-restore -v minimal --nologo
dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj -v minimal --nologo
```

Build the owning project for auth, STS, network, database, map generator, or
game-table changes. Run full solution build after shared contracts move.

## Done

Complete only when:
- `rg -n "LegacyServiceProvider" Source -g "*.cs"` has no results.
- `rg -n "class\s+\w+\s*:\s*Singleton<" Source -g "*.cs"` has no results.
- `rg -n "AddSingletonLegacy" Source -g "*.cs"` has no results.
- singleton-backed `Manager.Instance` calls are gone, excluding unrelated
  framework/static members such as `BindingFlags.Instance`.
- tests no longer use legacy provider collections/scopes/helpers.
- bridge files are deleted.
- focused subsystem tests and `NexusForever.Game.Tests` pass, or exact blockers
  are reported.

Final response: files changed, behavior boundary, verification, blockers, next
slice.
