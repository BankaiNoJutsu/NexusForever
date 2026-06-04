# Removed Marker Audit

Scope: branch working tree versus merge base
`f32b557a2adf40f380f76fc12d9ad007d145c848`.

Inventory command family:

```powershell
git diff --unified=0 --no-renames <merge-base> -- Source Decomp/Analysis/INITIAL_FINDINGS.md
```

The matching pattern is the branch-review pattern from the implementation pass:
`TODO|Blocked|unsupported|not implemented|partial`.

## Inventory Result

- Removed marker-pattern lines: 181.
- Real removed implementation markers: 171.
- Regex-only false positives: 10 C# `partial` declarations.
- Temporary audit CSV: `.nexusforever-runtime/todo-audit/removed_markers_current.csv`.

The 10 false positives are old `partial` class declarations in the migrated
guild, prerequisite, and spell classes:

- `Source/NexusForever.WorldServer/Game/Guild/Community.cs`
- `Source/NexusForever.WorldServer/Game/Guild/CommunityOperations.cs`
- `Source/NexusForever.WorldServer/Game/Guild/Guild.cs`
- `Source/NexusForever.WorldServer/Game/Guild/GuildBase.cs`
- `Source/NexusForever.WorldServer/Game/Guild/GuildBaseOperation.cs`
- `Source/NexusForever.WorldServer/Game/Guild/GuildOperations.cs`
- `Source/NexusForever.WorldServer/Game/Prerequisite/PrerequisiteChecks.cs`
- `Source/NexusForever.WorldServer/Game/Prerequisite/PrerequisiteManager.cs`
- `Source/NexusForever.WorldServer/Game/Spell/Spell.cs`
- `Source/NexusForever.WorldServer/Game/Spell/SpellEffectHandler.cs`

## Status Key

- Implemented: current branch contains matching server behavior.
- Evidence: removal is backed by table/client/source evidence, but there is no
  additional runtime mutation to add safely.
- Blocked: the old note described a real missing feature; this audit keeps the
  blocker explicit instead of silently losing it.
- Unsupported boundary: the current handler parses or validates the client
  surface and returns/logs a deterministic unsupported path.

## Per-File Audit

| Removed marker source | Count | Current owner | Status |
| --- | ---: | --- | --- |
| `Source/NexusForever.AuthServer/Network/Message/Handler/AuthenticationHandler.cs` | 1 | `Source/NexusForever.AuthServer/Network/Message/Handler/HelloAuthHandler.cs`, `Source/NexusForever.Database.Auth/AuthDatabase.cs` | Implemented. Initial realm selection now prefers account-inventory `TargetRealmId` values on online realms and falls back deterministically. |
| `Source/NexusForever.StsServer/Network/Message/Model/ServerErrorMessage.cs` | 4 | `Source/NexusForever.Network.Sts/Model/ServerErrorMessage.cs` | Implemented. STS error XML now writes code, server, module, line, and text values rather than zero placeholders. |
| `Source/NexusForever.WorldServer/Command/Handler/CurrencyCommandCategory.cs` | 1 | Same path, plus `ClientCharacterCreateHandler` | Implemented. `MaxLevelToken` is grantable and level-50 creation validates and consumes it. |
| `Source/NexusForever.WorldServer/Game/Account/AccountCurrencyManager.cs` | 2 | `Source/NexusForever.Game/Account/Currency/AccountCurrencyManager.cs` | Implemented for currency add/subtract and level-50 token spend. Evidence for caps is blocked: `AccountCurrencyTypeEntry` exposes id/text/icon/account item only, with no cap field to enforce. |
| `Source/NexusForever.WorldServer/Game/Achievement/BaseAchievementManager.cs` | 3 | `Source/NexusForever.Game/Achievement/BaseAchievementManager.cs` | Implemented. Achievement and checklist prerequisites now evaluate server, objective, and objective-alt prerequisite fields; script-only checklist rows remain guarded by object-id checks. |
| `Source/NexusForever.WorldServer/Game/Achievement/CharacterAchievementManager.cs` | 1 | `Source/NexusForever.Game/Achievement/CharacterAchievementManager.cs`, `GlobalAchievementManager.cs` | Implemented. Character completion grants titles and claims/broadcasts realm-first achievements. |
| `Source/NexusForever.WorldServer/Game/Achievement/Static/AchievementType.cs` | 1 | `Source/NexusForever.Game.Static/Achievement/AchievementType.cs` | Evidence plus implementation. Achievement type `116` is named `RealmFirst` from table/runtime usage and is consumed by realm-first tracking. |
| `Source/NexusForever.WorldServer/Game/Entity/BuybackManager.cs` | 1 | `Source/NexusForever.Game/Entity/BuybackManager.cs` | Implemented. Buyback storage is per character, expires entries, sends removed/updated messages, and supports partial-stack buyback snapshots. |
| `Source/NexusForever.WorldServer/Game/Entity/CostumeManager.cs` | 6 | `Source/NexusForever.Game/Entity/CostumeManager.cs`, `AccountCostumeManager.cs` | Mixed. Costume cap, index validation, item family/equippable checks, account unlock checks, dye channel/unlock validation, save responses, and item soulbinding on new unlock are implemented. Mannequins and costume save charging are blocked because mannequin residence state and retail charge formulas are not mapped. |
| `Source/NexusForever.WorldServer/Game/Entity/EntitlementManager.cs` | 2 | `Source/NexusForever.Game/Account/Entitlement`, `Source/NexusForever.Game/Entity/CharacterEntitlementManager.cs`, `RewardPropertyManager.cs` | Mixed. Account/character entitlement persistence and reward-property recalculation are implemented. Custom external entitlement feeds remain blocked until a backing service/source is defined. |
| `Source/NexusForever.WorldServer/Game/Entity/Inventory.cs` | 4 | `Source/NexusForever.Game/Entity/Inventory.cs`, `Item.cs`, `Bag.cs`, `IBag.cs`, `Player.cs` | Implemented. Bind-on-equip, persisted soulbound state, soulbound split/delete preservation, charge consumption delete reasons, item expiration load/create/tick/delete/network send, expiration-safe stack merges/splits, bag-capacity preflight, and snapshot rollback for failed multi-step moves are implemented. |
| `Source/NexusForever.WorldServer/Game/Entity/Item.cs` | 2 | `Source/NexusForever.Game/Entity/Item.cs` | Implemented/evidence. Item save masks now include expiration and soulbound state; remaining unknown item fields stay unmapped rather than guessed. |
| `Source/NexusForever.WorldServer/Game/Entity/ItemInfo.cs` | 1 | `Source/NexusForever.Game/Entity/ItemInfo.cs` | Evidence. Stackability remains derived from table max-stack data; no verified exception table for non-stackable max-stack rows was found. |
| `Source/NexusForever.WorldServer/Game/Entity/MailManager.cs` | 6 | `Source/NexusForever.Game/Entity/MailManager.cs`, `MailItem.cs` | Mixed. Expired mail cleanup, configurable expiry, mailbox distance checks, money handling, selected take-all, soulbound attachment rejection, and delete availability checks are implemented. Player-block, auction settlement, and GM-mail trust paths are blocked on friendship/auction/GM service semantics. |
| `Source/NexusForever.WorldServer/Game/Entity/Movement/MovementManager.cs` | 5 | `Source/NexusForever.Game/Entity/Movement`, `Source/NexusForever.Script.Main/AI/SplineAI.cs`, `ClientFlightPathPurchaseHandler.cs` | Mixed. Client movement validation, CC-state filtering, state keys, multi-spline command plumbing, follow speed fallback, negative-speed spline reversal, and paid flight-path route completion via destination taxi-node teleport are implemented. Retail taxi spline/vehicle embark visuals and full client set-state timing semantics remain blocked. |
| `Source/NexusForever.WorldServer/Game/Entity/Movement/Spline/Spline.cs` | 1 | `Source/NexusForever.Game/Entity/Movement/Spline` | Implemented/evidence. Spline modes, interpolation offsets, reverse modes, and spline templates are modeled in the split movement subsystem. |
| `Source/NexusForever.WorldServer/Game/Entity/NonPlayer.cs` | 1 | `Source/NexusForever.Game/Entity/NonPlayerEntity.cs` | Evidence. Non-player construction is creature/table driven; no deleted behavior marker remains without an owner. |
| `Source/NexusForever.WorldServer/Game/Entity/PathManager.cs` | 5 | `Source/NexusForever.Game/Entity/PathManager.cs`, path handlers | Mixed. Path rewards, prerequisite checks, unlock/change service-token costs, and activation cooldown timestamp math are implemented. Elder XP at rank 30 and preflight bag-space reservation are blocked pending retail reward overflow semantics. |
| `Source/NexusForever.WorldServer/Game/Entity/PetCustomisationManager.cs` | 1 | `Source/NexusForever.Game/Entity/PetCustomisationManager.cs` | Implemented. Pet flair unlocks evaluate prerequisites. |
| `Source/NexusForever.WorldServer/Game/Entity/Player.cs` | 6 | `Source/NexusForever.Game/Entity/Player.cs` | Mixed. Save interval config, selected-vendor range clearing, busy/interactive visible state, innate persistence, internal player-info hydration, and dependent cleanup are implemented. DB/custom proficiency sources remain blocked because no such source table/model is present. |
| `Source/NexusForever.WorldServer/Game/Entity/QuestManager.cs` | 9 | `Source/NexusForever.Game/Entity/QuestManager.cs`, `Quest.cs`, `QuestObjective.cs` | Mixed. Quest-item charge consumption, receiver/contact checks, communicator flow, quest/objective timers, optional-objective completion, objective guidance, and item/money rewards are implemented. Contract slot reward-property effects, virtual items, and unverified fixed/conditional reward forms remain blocked. |
| `Source/NexusForever.WorldServer/Game/Entity/Simple.cs` | 1 | `Source/NexusForever.Game/Entity/SimpleEntity.cs` | Implemented. Simple activation can cast the mapped activation spell path. |
| `Source/NexusForever.WorldServer/Game/Entity/SpellManager.cs` | 2 | `Source/NexusForever.Game/Entity/SpellManager.cs`, spell handlers | Mixed. Level-up spell grants and stop-cast validation/cancel handling are implemented. Other unmapped spell error cases remain diagnostic-only. |
| `Source/NexusForever.WorldServer/Game/Entity/Static/EntityCreateFlag.cs` | 1 | `Source/NexusForever.Game.Static/Entity/EntityCreateFlag.cs` | Evidence. Static names remain bounded to known create-flag behavior. |
| `Source/NexusForever.WorldServer/Game/Entity/Static/InventoryLocation.cs` | 1 | `Source/NexusForever.Game.Static/Entity/InventoryLocation.cs` | Evidence. Known locations are named; unknown locations are retained as unknown enum values rather than guessed. |
| `Source/NexusForever.WorldServer/Game/Entity/Static/StatType.cs` | 1 | `Source/NexusForever.Game.Static/Entity/StatType.cs` | Evidence. Stat names are table/client correlated; unknowns remain named as unknown. |
| `Source/NexusForever.WorldServer/Game/Entity/Vehicle.cs` | 4 | `Source/NexusForever.Game/Entity/VehicleEntity.cs`, mount/vehicle spell effects | Blocked/evidence. Passenger/mount basics exist, but vehicle UI fields and several vehicle packet details remain unmapped; no extra mutation was added. |
| `Source/NexusForever.WorldServer/Game/Entity/WorldEntity.cs` | 2 | `Source/NexusForever.Game/Entity/WorldEntity.cs`, entity activation handlers | Implemented. Activate versus activate-cast paths are split, target busy/range checks are shared, activation success/fail hooks fire, and busy state broadcasts are modeled. |
| `Source/NexusForever.WorldServer/Game/Entity/XpManager.cs` | 8 | `Source/NexusForever.Game/Entity/XpManager.cs`, `WorldConfig.cs` | Mixed. Rest XP accrual from housing logout, rest cap, rest kill spend, max-level config, and configurable signature XP rate are implemented. Elder gem rest bonuses, decor/home-city/town/sleeping-bag modifiers, spell/event XP bonuses, and exact active event modifiers are blocked until data sources are mapped. |
| `Source/NexusForever.WorldServer/Game/Guild/GlobalGuildManager.cs` | 2 | `Source/NexusForever.Game/Guild/GlobalGuildManager.cs` | Mixed. Guild save interval config is implemented. Unknown guild operations still have an explicit debug unsupported boundary. |
| `Source/NexusForever.WorldServer/Game/Guild/GuildBaseOperation.cs` | 3 | `Source/NexusForever.Game/Guild/GuildBaseOperation.cs` | Implemented/evidence. Rank permission masks are normalized, invalid bits are rejected, and `Disabled` is stripped from editable masks. |
| `Source/NexusForever.WorldServer/Game/Guild/GuildManager.cs` | 2 | `Source/NexusForever.Game/Guild/GuildManager.cs` | Mixed. Invite expiry is configurable and enforced. `Unknown10` remains an explicitly unmapped guild-member field. |
| `Source/NexusForever.WorldServer/Game/Guild/Static/GuildRankPermission.cs` | 1 | `Source/NexusForever.Game.Static/Guild/GuildRankPermission.cs` | Implemented. `All` and `Leader` exclude `Disabled`; community/warplot permission bits are present. |
| `Source/NexusForever.WorldServer/Game/Housing/GlobalResidenceManager.cs` | 1 | `Source/NexusForever.Game/Housing/GlobalResidenceManager.cs` | Implemented. Residence save interval is configurable. |
| `Source/NexusForever.WorldServer/Game/Housing/Plot.cs` | 2 | `Source/NexusForever.Game/Housing/Plot.cs` | Implemented. Default plug ids are applied from housing table data with safe casting and build state updates. |
| `Source/NexusForever.WorldServer/Game/Housing/Residence.cs` | 3 | `Source/NexusForever.Game/Housing/Residence.cs`, housing neighbor handlers | Mixed. Community construction-yard plug creation and community rank decor permissions are implemented. Roommate/neighbor decor mutation is blocked and now explicit through unsupported neighbor handlers because roommate persistence/permission state is not modeled. |
| `Source/NexusForever.WorldServer/Game/Housing/Static/DecorType.cs` | 1 | `Source/NexusForever.Game.Static/Housing/DecorType.cs` | Evidence. Decor type names preserve known/unknown values without speculative names. |
| `Source/NexusForever.WorldServer/Game/Housing/Static/HousingResult.cs` | 1 | `Source/NexusForever.Network.World/Message/Static/HousingResult.cs` | Evidence/implemented boundary. Housing result values are named where used by plug/community handlers; unknown result slots remain unmapped. |
| `Source/NexusForever.WorldServer/Game/Mail/MailItem.cs` | 1 | `Source/NexusForever.Game/Mail/MailItem.cs` | Implemented. Mail expiry uses configurable world config. |
| `Source/NexusForever.WorldServer/Game/Map/BaseMap.cs` | 2 | `Source/NexusForever.Game/Map/BaseMap.cs`, `Source/NexusForever.IO/Map/MapFile.cs` | Mixed. Map-file grid lookup, terrain height/world-zone lookup, active grid lifecycle, and safe add-to-grid checks are implemented. Water and prop collision remain blocked because the current map file reader exposes terrain/world-zone, not prop/water volumes. |
| `Source/NexusForever.WorldServer/Game/Map/MapInstance.cs` | 1 | `Source/NexusForever.Game/Map/Instance/MapInstance.cs` | Evidence. Instance unload/grid behavior is explicit; the old arbitrary-source note has no missing mutation target. |
| `Source/NexusForever.WorldServer/Game/Map/ResidenceMapInstance.cs` | 4 | `Source/NexusForever.Game/Map/Instance/ResidenceMapInstance.cs` | Mixed. Plug update errors, plug costs, contribution rejection, plot/cell/world origin math, and active-prop decor diagnostics are implemented. Community/warplot positioning and active-prop entity backing remain blocked where table/runtime semantics are incomplete. |
| `Source/NexusForever.WorldServer/Game/Prerequisite/PrerequisiteChecks.cs` | 1 | `Source/NexusForever.Game/Prerequisite/Check/PrerequisiteCheckVital.cs` | Implemented. Vital prerequisites evaluate health, shield capacity, and interrupt armor and warn on unsupported vitals/comparisons. |
| `Source/NexusForever.WorldServer/Game/Prerequisite/Static/PrerequisiteType.cs` | 1 | `Source/NexusForever.Game.Static/Prerequisite/PrerequisiteType.cs` | Evidence. Names are derived from table descriptions and implemented checks; unknown table entries remain unknown. |
| `Source/NexusForever.WorldServer/Game/Quest/CommunicatorMessage.cs` | 2 | `Source/NexusForever.Game/Quest/CommunicatorMessage.cs` | Implemented. Communicator visibility now checks zone, reputation, faction, class, and prerequisite conditions. |
| `Source/NexusForever.WorldServer/Game/Quest/Quest.cs` | 4 | `Source/NexusForever.Game/Quest/Quest.cs` | Mixed. Quest timers, objective gating, optional completion after required completion, and objective guidance are implemented. Exact client meaning for several legacy flag bits remains evidence-only. |
| `Source/NexusForever.WorldServer/Game/Quest/QuestObjective.cs` | 3 | `Source/NexusForever.Game/Quest/QuestObjective.cs` | Implemented. Objective target-id caches, dynamic/checklist handling, and objective timers are implemented and persisted. |
| `Source/NexusForever.WorldServer/Game/Quest/Static/QuestObjectiveType.cs` | 1 | `Source/NexusForever.Game.Static/Quest/QuestObjectiveType.cs` | Evidence. Additional objective names are table/client correlated; unknown objective types remain conservatively named. |
| `Source/NexusForever.WorldServer/Game/Reputation/FactionNode.cs` | 1 | `Source/NexusForever.Game/Reputation/FactionNode.cs` | Implemented/evidence. Duplicate friendship faction relationships are grouped by target faction before relationship lookup. |
| `Source/NexusForever.WorldServer/Game/Social/GlobalChatManager.cs` | 2 | `Source/NexusForever.Game/Chat/GlobalChatManager.cs`, `ItemGuidChatFormatter.cs` | Mixed. Item guid chat formatting is implemented from inventory item state. Unhandled chat operations still send/log an explicit "Currently not implemented" debug response. |
| `Source/NexusForever.WorldServer/Game/Spell/ActionSet.cs` | 1 | `Source/NexusForever.Game/Spell/ActionSet.cs`, spell action-set handlers | Implemented. Non-spell shortcuts and LAS validation are handled by the action-set model/handlers. |
| `Source/NexusForever.WorldServer/Game/Spell/CharacterSpell.cs` | 1 | `Source/NexusForever.Game/Spell/CharacterSpell.cs` | Implemented. Continuous-cast held state is tracked. |
| `Source/NexusForever.WorldServer/Game/Spell/Spell.cs` | 6 | `Source/NexusForever.Game/Spell/Spell.cs`, spell diagnostics/evidence files | Mixed. Central lifetime events, target selection diagnostics, target-info damage descriptions, CC state masks, persistence cleanup, and runtime evidence collection are implemented. Moving/rotating telegraph precision and non-player prerequisite semantics remain diagnostic-only where client/server evidence is insufficient. |
| `Source/NexusForever.WorldServer/Game/Spell/SpellEffectHandler.cs` | 3 | `Source/NexusForever.Game/Spell/SpellEffectHandler.cs`, `DamageCalculator.cs` | Mixed. Damage calculation, NPC/player mounting paths, duration-effect scheduling, and many spell-effect diagnostics are implemented. Unsupported vitals/effect modes remain explicit diagnostic boundaries. |
| `Source/NexusForever.WorldServer/Game/Spell/SpellTargetInfo.cs` | 1 | `Source/NexusForever.Game/Spell/SpellTargetInfo.cs` | Implemented/evidence. Target effect info is carried into `ServerSpellGo` with damage description data where available. |
| `Source/NexusForever.WorldServer/Game/Spell/Static/ShortcutType.cs` | 1 | `Source/NexusForever.Game.Static/Spell/ShortcutType.cs` | Evidence. Shortcut values are named conservatively and consumed by action-set handling. |
| `Source/NexusForever.WorldServer/Game/Spell/Telegraph.cs` | 1 | `Source/NexusForever.Game/Spell/Telegraph.cs` | Implemented. Telegraph hit testing now accounts for vertical delta versus target hit radius. |
| `Source/NexusForever.WorldServer/Network/Message/Handler/CharacterHandler.cs` | 10 | split handlers under `Source/NexusForever.WorldServer/Network/Message/Handler/Character` and related player/info paths | Mixed. Realm select down errors, character name/path validation, creation entitlement and level-token checks, starting stats, save failure result, deletion mail/residence handling, rapid transport, innate validation, and inspect/player-info pipeline are implemented. Aurin engineer entitlement specifics remain table-driven by creation entry requirements. |
| `Source/NexusForever.WorldServer/Network/Message/Handler/EntityHandler.cs` | 3 | split handlers under `Source/NexusForever.WorldServer/Network/Message/Handler/Entity` | Implemented. Activation, interaction, chair, busy, and range guards are shared and objective updates are targeted instead of disabling unrelated targets. |
| `Source/NexusForever.WorldServer/Network/Message/Handler/HousingHandler.cs` | 6 | split housing handlers, `ResidenceMapInstance.cs`, neighbor handlers | Mixed. Community removal, visit/privacy checks, plug error responses, remodel/plug validation, and vendor list handling are implemented. Neighbor/roommate and wallpaper state remain explicit unsupported boundaries. |
| `Source/NexusForever.WorldServer/Network/Message/Handler/ItemHandler.cs` | 2 | split item/account item handlers | Implemented/evidence. Item unlock/save paths return deterministic results and reject unsupported account-item groups without mutation. |
| `Source/NexusForever.WorldServer/Network/Message/Handler/MailHandler.cs` | 1 | split mail handlers | Implemented. Selected mail take-all is implemented through `MailManager`. |
| `Source/NexusForever.WorldServer/Network/Message/Handler/MiscHandler.cs` | 1 | `PlayerInfoResponseHandler` and info handlers | Implemented. Player info requests use the internal request/response pipeline. |
| `Source/NexusForever.WorldServer/Network/Message/Handler/PathHandler.cs` | 1 | `Source/NexusForever.WorldServer/Network/Message/Handler/Path` | Implemented. Path unlock/change service-token checks and charges are implemented. |
| `Source/NexusForever.WorldServer/Network/Message/Handler/SpellHandler.cs` | 6 | split spell handlers | Mixed. Cast result validation, stop-cast removal, ability/LAS validation, AMP/PvP/tier checks, reset costs, and shortcut validation are implemented. Selected-spell cast and ability-book activation remain explicit unsupported/diagnostic paths. |
| `Source/NexusForever.WorldServer/Network/Message/Handler/VendorHandler.cs` | 3 | split vendor handlers | Implemented. Purchase cost calculation, sell/buyback currency flow, partial-stack sell, buyback inventory-room checks, repair-vendor status validation, single-item repair, and repair-all credit charging/durability restoration are implemented from the mapped client repair helpers. |
| `Source/NexusForever.WorldServer/Network/Message/Model/ClientPackedWorld.cs` | 1 | `Source/NexusForever.Network.World/Message/Model/ClientPackedWorld.cs` | Evidence. Envelope types `11` and `19` are named as the only known accepted packed-world envelopes; payload bytes remain diagnostic. |
| `Source/NexusForever.WorldServer/Network/Message/Model/Shared/UpdateHealthMask.cs` | 1 | `Source/NexusForever.Network.World/Message/Model/Shared/UpdateHealthMask.cs` | Evidence. Health/shield/absorption mask bits are named where current network messages consume them. |

## Current Explicit Blockers

These are the removed-marker topics that did not receive speculative behavior
because the current branch still lacks enough evidence or backing state:

- Account currency caps: no cap field is exposed by `AccountCurrencyTypeEntry`.
- Costume mannequins and costume save charging: no mapped mannequin state or
  retail cost formula.
- Mail player-block, auction, and GM-mail paths: require friendship, auction,
  and GM trust/service semantics.
- Taxi embark and full movement state-key retail semantics: paid route
  completion now moves players to the destination taxi-node world location, but
  retail taxi spline/vehicle embark visuals remain unmapped.
- Path elder XP and path reward overflow reservation: reward routing exists,
  but overflow behavior is not mapped.
- Roommate/neighbor housing mutation and wallpaper state: request parsing and
  validation exist; roommate persistence/permissions are not modeled.
- Map water/prop collision and residence active-prop backing: current map/decor
  loaders do not expose enough runtime data.
- Quest contract slot reward properties, virtual item rewards, and remaining
  fixed/conditional reward forms: safe item/money rewards exist; other reward
  kinds need table/client confirmation.
- Vehicle UI/packet details: mount/passenger basics exist; UI-specific vehicle
  fields are unmapped.
- XP decor/home/town/sleeping-bag/spell/event modifiers and elder-gem rest
  bonuses: base rest/signature behavior exists; these sources need data mapping.
- Selected spell cast, ability-book activation, and several unhandled
  guild/chat operations: current code keeps deterministic unsupported
  boundaries.

## Verification Notes

- The audit was regenerated with case-sensitive matching to mirror `rg` and
  avoid false hits such as `ToDouble`.
- The post-implementation solution build was rerun after the inventory rollback
  and resize fixes:
  `dotnet build Source\NexusForever.sln --no-restore -m:1 -v minimal
  --nologo -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\blocked-final-2\`
  succeeded with `0 Warning(s)` and `0 Error(s)`.
- 2026-06-04 scoped recheck: `rg -n "\bTODO\b|\bFIXME\b|NotImplemented|NotImplementedException" Source -g "*.cs"`
  returns only test doubles with deliberate `NotImplementedException` guards after
  the Ruins of Kel Voreth WIP-guessed faction-routing comment was rewritten to
  remove its stale `TODO` marker. Production source has no active TODO/FIXME or
  NotImplemented marker.
- 2026-06-04 scoped recheck: `rg -n "throw new NotSupportedException" Source -g "*.cs"`
  reports production guardrails only: unsupported database provider selection,
  unsupported packet-source-generator field/string encodings, and the
  `PositionMultiSplineCommand` mixed-spline-type invariant. Test-project hits are
  deliberately unreachable fixture members. These are explicit unsupported
  boundaries, not unimplemented feature claims.
