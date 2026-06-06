# Goal: Implement Evidence-Backed Refactors

Work in `I:\GIT\NexusForever`. Implement the evidence-backed refactor
opportunities from the current codebase audit without changing gameplay,
packet, database, or runtime behavior except where a refactor requires an
equivalent adapter. Treat this as a behavior-preserving modernization goal, not
a feature-restoration or protocol-widening pass.

First read: `AGENTS.md`, `README.md`, `CURRENT_STATUS.md`, and
`Plan World Server Modularization.md`. Re-scan for nested `AGENTS.md` before
editing. Preserve existing user changes in the dirty worktree and do not revert
or normalize unrelated files.

## Evidence Baseline

Start by refreshing the audit facts before editing:

- `git status --short`
- `rg --files Source\NexusForever.Game -g "*.cs"` line-count scan for the
  largest gameplay files.
- `rg -n "LegacyServiceProvider\.Provider|DatabaseManager\.Instance|GameTableManager\.Instance|Singleton<" Source\NexusForever.Game Source\NexusForever.WorldServer Source\NexusForever.Script.Main -g "*.cs"`
- `rg -n "FireAndForgetAsync\(|WaitUnwrap\(|SaveBlocking" Source -g "*.cs"`
- `rg -n "\[Collection\(LegacyServiceProviderCollection\.Name\)\]" Source\NexusForever.Game.Tests -g "*.cs"`
- `rg -n "private sealed class TestInventory|private sealed class TestBag|class ServiceProviderScope|class LegacyProviderScope|class TestWorldSession|class StubAccountEntitlement" Source\NexusForever.Game.Tests -g "*.cs"`

Record the refreshed counts in the first progress update or in a small local
workboard before implementation. If the numbers have moved, let the current
source win.

## Refactor Streams

### 1. Move handler-owned runtime state into services

Target handler files that currently own business state, persistence, or durable
domain rules:

- `Source\NexusForever.WorldServer\Network\Message\Handler\Account\ClientCREDDExchangeHandlers.cs`
- `Source\NexusForever.WorldServer\Network\Message\Handler\Crafting\ClientCraftingRuneHandlers.cs`
- `Source\NexusForever.WorldServer\Network\Message\Handler\Crafting\ClientCraftingCraftHandlers.cs`
- `Source\NexusForever.WorldServer\Network\Message\Handler\Account\ClientStorefrontPurchaseHandlers.cs`

Goal:

- Keep packet handlers responsible for session validation, request/response
  conversion, and message emission.
- Move durable state and rules into injectable services in the narrowest
  existing owner, usually `NexusForever.Game` or a small world-server domain
  service when it still depends on world-session concepts.
- Prefer interfaces that can be unit-tested without constructing a full
  `IWorldSession`.

Initial service candidates:

- `ICREDDExchangeService` for CREDD order/history state and persistence.
- `ICraftingModifierSessionStore` or equivalent for per-character crafting
  additive/catalyst state.
- Storefront purchase planning/delivery helpers that can be tested without
  packet handlers.

Do not alter packet shapes, opcodes, timing, error codes, or evidence-gated
crafting semantics while extracting.

### 2. Reduce `LegacyServiceProvider` and singleton coupling

Use the bridge only where the containing design still requires it. Prefer
constructor injection or small resolver interfaces when touching code already
under refactor.

High-value targets:

- `Source\NexusForever.Game\Marketplace\GlobalMarketplaceManager.cs`
- `Source\NexusForever.Game\Spell\SpellEffectHandler.cs`
- `Source\NexusForever.Game\RealmBank\RealmBankManager.cs`
- `Source\NexusForever.Game\Entity\DatacubeManager.cs`
- `Source\NexusForever.Game\Map\BaseMap.cs`
- `Source\NexusForever.Game\Entity\WorldEntity.cs`

Rules:

- Do not attempt a repo-wide singleton purge in one pass.
- Replace globals only when the dependency boundary is clear and focused tests
  can prove equivalent behavior.
- Keep existing service-registration patterns consistent with nearby
  `ServiceCollectionExtensions` files.
- Watch for test parallelization: removing global provider requirements should
  let tests leave `LegacyServiceProviderCollection` only when no global state is
  still mutated.

### 3. Split large gameplay classes along existing responsibility boundaries

Use mechanical, behavior-preserving extractions first. Avoid speculative
redesign.

Primary candidates:

- `Source\NexusForever.Game\Spell\SpellEffectHandler.cs`
  - split effect families into partial files or narrow helper classes.
  - keep `[SpellEffectHandler]` discovery unchanged.
- `Source\NexusForever.Game\Marketplace\GlobalMarketplaceManager.cs`
  - extract commodity order book operations, persistence mapping, and delivery
    settlement helpers.
- `Source\NexusForever.Game\Loot\GlobalLootManager.cs`
  - extract salvage loot generation, generated-loot delivery preflight, and
    active loot-instance indexing if tests support it.
- `Source\NexusForever.Script.Main\AI\CombatAI.cs`
  - extract chase movement, target selection, special-attack scheduling, and
    aggro/assist rules where current tests can pin behavior.
- `Source\NexusForever.Game\Entity\Player.cs` and
  `Source\NexusForever.Game\Entity\UnitEntity.cs`
  - touch only for tightly scoped dependency extraction or helper moves; defer
    broad entity decomposition unless a focused change already needs it.

For each extraction, move tests or add focused regression tests around the
public behavior being preserved.

### 4. Consolidate repeated test support

Create shared test support only where at least two current tests duplicate the
same fake or scope.

Initial candidates:

- Loot `TestInventory` / `TestBag` duplicates across
  `LootInstanceDeliveryTests`, `LootInstanceResolutionTests`, and
  `LootBindOnPickupPolicyTests`.
- Repeated `LegacyServiceProvider` scope types in marketplace, mail, housing,
  and account tests.
- Repeated `TestWorldSession` and entitlement/account stubs where a shared
  builder would reduce unsupported-method boilerplate.

Keep fakes intentionally small. Do not introduce a heavyweight test framework or
mocking dependency unless the repo already uses one.

### 5. Centralize fire-and-forget/background persistence handling

Audit current `FireAndForgetAsync`, `SaveBlocking`, and synchronous wait sites.
Then introduce a small, explicit background-task or persistence-runner
abstraction only where it improves error handling, shutdown behavior, or test
control.

Rules:

- Do not turn all call sites async in one sweep.
- Do not change broker message ordering, player-save ordering, or database
  transaction semantics without focused tests.
- Start with low-risk support/leaderboard/admin or account-storefront
  persistence paths before touching hot gameplay loops.

## Phased Execution

1. Baseline current counts, dirty files, and build/test blockers.
2. Pick one narrow stream and state the intended behavior-preserving boundary
   before editing.
3. Implement the smallest extraction that compiles and keeps existing tests
   meaningful.
4. Add or adjust focused xUnit tests when moving rules out of handlers or
   globals.
5. Run the narrowest useful build/test after each slice.
6. Update this prompt or a focused tracker only if repeated friction or new
   evidence changes the refactor order.
7. Repeat until all five streams are either implemented or explicitly blocked
   with the next required evidence or design decision.

Suggested order:

1. Test-support consolidation for loot and provider scopes.
2. `CraftingRuneRequestHelper` state extraction.
3. `CREDDExchangeRuntime` service extraction.
4. Marketplace persistence/order-book split.
5. Spell effect handler family split.
6. CombatAI helper split.
7. Fire-and-forget/background persistence runner.
8. Remaining targeted `LegacyServiceProvider` reductions.

## Non-Goals

- Do not add new runtime processes.
- Do not rename evidence-gated packet fields, opcodes, spell data fields, or
  placeholder enums as part of this refactor goal.
- Do not broaden spell, crafting, marketplace, loot, movement, packet, or
  database behavior.
- Do not edit generated/local artifacts such as `bin`, `obj`, `.nexusforever-runtime`,
  `artifacts`, data-mapping outputs, decompile exports, or dump files.
- Do not refactor archival `Obsolete` code unless a focused task explicitly
  requires it.

## Verification

Use the narrowest useful command for each slice, then broaden when shared
contracts move:

```powershell
dotnet build Source\NexusForever.Game\NexusForever.Game.csproj --no-restore -v minimal --nologo
dotnet build Source\NexusForever.WorldServer\NexusForever.WorldServer.csproj --no-restore -v minimal --nologo
dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj -v minimal --nologo
```

When the world server is running locally, the world-server build may fail during
copy because `NexusForever.WorldServer` locks output DLLs. Report the PID and
either use a narrower build or ask before stopping the local runtime.

For each completed slice, final response must include:

- files changed;
- behavior boundary preserved;
- tests/builds run and exact result;
- any blocked refactor stream and why;
- the next safest slice.

Done only when all identified refactor streams are implemented with focused
verification, or each remaining stream has a concrete blocker and next action.
