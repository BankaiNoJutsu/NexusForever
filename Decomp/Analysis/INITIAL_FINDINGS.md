# Client64 Initial Reverse-Engineering Findings

Generated from the Ghidra headless project in `Decomp\Analysis\ghidra_projects`
and exports in `Decomp\Analysis\exports`.

## Export Coverage

| Binary | Functions | Strings | Interesting strings | Selected xrefs |
| --- | ---: | ---: | ---: | ---: |
| `WildStar64.exe` | 24,970 | 45,034 | 10,940 | 3,509 |
| `Houston64.exe` | 25,129 | 29,460 | 6,317 | 1,952 |
| `StsConnLib64.MT.dll` | 4,522 | 10,893 | 3,406 | 2,358 |

The selected C exports are biased toward protocol/data anchors, so they are a
starting point rather than a complete native source reconstruction.

## High-Value Anchors

### STS auth and transaction flow

`StsConnLib64.MT.dll` contains direct strings for the STS auth transaction names:
`KeyData`, `LoginStart`, `LoginFinish`, `RequestGameToken`, and `TokenKeyData`.
The selected decompile puts them together near
`exports\StsConnLib64.MT.dll\selected_decompiled.c:1115`.

This lines up with NexusForever's current STS handlers:

- `Source\NexusForever.StsServer\Network\Message\Handler\AuthenticationHandler.cs`
- `Source\NexusForever.Network.Sts\Model\ClientLoginStartMessage.cs`
- `Source\NexusForever.Network.Sts\Model\RequestGameTokenMessage.cs`

The same binary also exposes source-path strings for the socket layer:

- `Services\StsInetSocket\StsInetSocket.cpp` around `selected_decompiled.c:3622`
- `Services\SocketCrypt\SocketCrypt.cpp` around `selected_decompiled.c:4797`

Those are the best first targets for auth framing, transaction response codes,
timeouts, and STS socket crypto behavior.

Follow-up implemented from this pass:

- `LoginFinish` requests include optional `LongTermSession`,
  `SecondaryAuthToken`, and `RegisterVerifiedIp` fields around
  `exports\StsConnLib64.MT.dll\selected_decompiled.c:7`.
- Normal `LoginFinish` replies are parsed with `AuthType` first, then
  `LocationId`, `UserId`, `UserCenter`, `UserName`, and `AccessMask` when
  `AuthType == 0` around `selected_decompiled.c:1015`.
- `RequestGameToken` sends `UserId`, `GameCode`, and `AccountAlias` around
  `selected_decompiled.c:106`.
- `ListMyAccounts` sends `UserId` and `GameCode` around
  `selected_decompiled.c:56`.

These fields are now represented in the STS message models, and the STS server
uses the authenticated account when returning login/account metadata and when
issuing a game token.

Second follow-up implemented from this pass:

- `/Sts/Connect` construction writes `ConnType`, optional `ConnProductType`,
  `ConnAppIndex`, `ConnDeployment`, and `ConnEpoch`, followed by `Address`,
  `ProductType`, `AppIndex`, optional `Deployment`, `Epoch`, `Program`, `Build`,
  `Process`, optional `NotifyFlags`, and optional `VersionFlags` around
  `selected_decompiled.c:1540`.
- The client-side connect parser also accepts `Location` as a fallback source
  for address data around `selected_decompiled.c:1360`.
- `/Auth/LoginStart` sends `LoginName` and `NetAddress` around
  `selected_decompiled.c:392`.

The STS connect and login-start message models now parse those fields. The
export script high-value patterns were expanded so future export-only runs keep
these STS anchors in `selected_decompiled.c`.

Third follow-up implemented from this pass:

- Function names are now reproducible through `Decomp\Analysis\function_labels.csv`
  and `scripts\ApplyNexusForeverLabels.java`; export-only runs apply those names
  before writing `functions.csv` and `selected_decompiled.c`. Labeled functions
  are also included in `selected_decompiled.c` even when they do not have their
  own string anchors.
- `StsConn_ParseUserAccountFields` at `180008c40` reads `UserId`,
  `UserCenter`, `UserName`, `LoginName`, `UserStatus`, `Created`, optional
  `ExternalAccount`, and optional `ServiceTimeSchedule` around
  `selected_decompiled.c:553`.
- `StsConn_ParseAliases` at `18000d130` walks repeated `Alias` children under an
  `Aliases` node around `selected_decompiled.c:952`. `LoginFinish` calls it
  from the successful `AuthType == 0` branch around `selected_decompiled.c:1090`.
- The STS login-finish response model can now emit optional `Aliases/Alias`
  entries, and the STS server includes the authenticated account email as an
  alias alongside `UserId` and `UserName`.

Fourth follow-up implemented from this pass:

- `StsConn_SendConsumeGameToken` at `180004da0` writes `/Auth/ConsumeGameToken`
  request fields `GameCode`, `Token`, and `ClientNetAddress` around
  `selected_decompiled.c:157` and `selected_decompiled.c:228`.
- `StsConn_OnConsumeGameTokenResponse` at `18000b540` reads `GameAccountId`,
  `LoginName`, `UserId`, `UserName`, `UserCenter`, and optional `Roles` around
  `selected_decompiled.c:854`.
- `StsConn_ParseRoles` at `18000a150` reads repeated `RoleId` children under a
  `Roles` node around `selected_decompiled.c:633`.
- NexusForever now has STS models and a handler for `/Auth/ConsumeGameToken`.
  The handler normalises the supplied token, can resolve the account by token
  when the request is not tied to an already-authenticated STS session, and
  returns the account identity fields the client parser expects.
- STS account lookup now includes account RBAC roles. `LoginFinish` and
  `ConsumeGameToken` responses emit `Roles/RoleId` when the account has roles
  loaded, matching the shared `StsConn_ParseRoles` helper.

Fifth follow-up implemented from this pass:

- Additional STS functions are now named in `function_labels.csv`:
  `StsConn_SendPresenceLogin` at `180005010`,
  `StsConn_SendAssociateMyExternalAccount` at `1800067d0`,
  `StsConn_SendLoginStart` at `180006fb0`,
  `StsConn_ParseExternalAccount` at `180007560`, and
  `StsConn_OnAuthnTokenResponse` at `180008230`.
- `StsConn_SendPresenceLogin` writes `LoginName`, optional `UserName`, optional
  `Alias`, optional `GameCode`, optional `GameAccountId`, and
  `ClientNetAddress` around `selected_decompiled.c:244`.
- `StsConn_SendAssociateMyExternalAccount` writes `UserId`,
  `AuthProviderCode`, `AuthnToken`, and `AppId` around
  `selected_decompiled.c:308`.
- `StsConn_OnAuthnTokenResponse` reads the reply `AuthnToken` field around
  `selected_decompiled.c:509`. No NexusForever server implementation was added
  for these external-account/token-auth flows yet; the work here is mapping.

Sixth follow-up implemented from this pass:

- `StsConn_SendTokenKeyData` at `18000a730` is now named in
  `function_labels.csv`. It parses server key material from `ServerRand`,
  `ServerPublicKey`, and `ServerSignature` around `selected_decompiled.c:758`
  before sending transaction id `0x3b` / `TokenKeyData`.
- The same function writes binary `PremasterSecret` and `AuthnToken` request
  fields, optional `AuthProviderCode`, and `AppId` around
  `selected_decompiled.c:808`.
- The STS socket connect path now has separate reproducible labels for
  `StsInetSocket_ParseConnectFields` at `18002ec10`,
  `StsInetSocket_SendConnectEnvelope` at `18002f410`, and
  `StsInetSocket_DispatchConnectEnvelope` at `180030d80`.
- `ExportNexusForeverAnalysis.java` now treats high-value patterns as
  interesting strings even when they do not also match a broad keyword. This
  keeps narrow auth fields such as `PremasterSecret`, `AppId`, and
  `TokenKeyData` visible in `interesting_strings.csv` and `selected_xrefs.csv`.
- No server endpoint was added for `/Auth/TokenKeyData`; the decompile shows a
  crypto/token-login handshake and more evidence is needed before implementing
  that safely.

### World network send path

`WildStar64.exe` includes `Network_SendMessageById`, selected at
`exports\WildStar64.exe\selected_decompiled.c:260`. The same export includes
the surrounding socket/WorldSocket selection area at
`selected_decompiled.c:3091`.

The imports also show the expected WinSock path in `WildStar64.exe`:
`WSAStartup`, `WSASocketW`, `recv`, `send`, `WSARecv`, `WSASend`, `connect`,
`ioctlsocket`, `socket`, `closesocket`, `gethostbyname`, and `select`.

Follow-up implemented from this pass:

- The send path masks the target/socket selector with `0xffff` and resolves the
  message definition by message ID before dispatch, matching NexusForever's
  16-bit `GameMessageOpcode` packet header handling.
- The client logs invalid/foreign message IDs in this path; NexusForever's
  unknown packet warning now formats the opcode cleanly for the same kind of
  investigation.

Second WildStar network follow-up implemented from this pass:

- The select-based socket driver is now named in `function_labels.csv`:
  `NetworkSocket_CreateNonBlockingTcpSocket` at `140338f90`,
  `NetworkSocket_StartConnect` at `1403390d0`,
  `NetworkSocket_SelectDispatch` at `140339820`,
  `NetworkSocket_RecvAvailable` at `140339db0`,
  `NetworkSocket_SendQueued` at `14033a030`, and
  `NetworkDriver_SelectInitialise` at `14033a5a0`.
- The overlapped/IOCP-style path is also named:
  `NetworkIOCP_CreateSocket` at `14033bb50`,
  `NetworkIOCP_PostRecv` at `14033bf90`, and
  `NetworkIOCP_SendQueued` at `14033c7e0`.
- The client sets `TCP_NODELAY` in both socket creation paths around
  `selected_decompiled.c:1766` and `selected_decompiled.c:2464`. NexusForever now
  mirrors that on accepted server sockets with `Socket.NoDelay = true`.
- `NetworkSocket_SelectDispatch` builds read/write/exception `fd_set`s, calls
  `select`, and dispatches connect/read/write handling around
  `selected_decompiled.c:1898`. This maps the client-side state machine around
  socket states `2`, `3`, `4`, and `5` without needing a protocol change.
- Two diagnostic helpers are now named as well: `Network_IcmpPing` at
  `140333b90` and `Network_IcmpTraceRoute` at `140333e10`.

Third WildStar network follow-up implemented from this pass:

- `Network_SendResolvedMessage` at `140333fc0` is now labelled and selected at
  `selected_decompiled.c:505`. It calls the message serializer and queues the
  world socket for later send when serialisation succeeds.
- `Network_SerialiseResolvedMessage` at `1403355e0` is now labelled and selected
  at `selected_decompiled.c:749`. The normal send path writes the computed
  packet byte size as a 32-bit field, then writes the message id as a 16-bit
  field before invoking the generated payload serializer callback.
- `Network_SendMessageFailure` at `1403315a0` is now labelled and selected at
  `selected_decompiled.c:234`. `Network_SendResolvedMessage` calls it with
  failure reason `0xc` when serialisation returns an error.
- The serializer computes packet size in bits, rounds to bytes, applies
  per-message/default size limits, and emits warning/error telemetry around the
  90% and exceeded-size cases. NexusForever's `ServerGamePacket.Size` now keeps
  the computed total as a 32-bit value instead of truncating through `ushort`,
  matching the client's 32-bit size field.

Fourth WildStar network follow-up implemented from this pass:

- The packet-buffer/bit-writer helpers are now labelled:
  `NetworkPacketBuffer_Create` at `140335530`, `NetworkBitWriter_WriteBits` at
  `140336380`, `NetworkBitWriter_GetPayloadStartBit` at `1403366f0`, and
  `NetworkBitWriter_GetBitPosition` at `140336750`.
- The bucket-specific write helpers are also labelled:
  `NetworkBitWriter_WriteBits8` at `1403367d0`,
  `NetworkBitWriter_WriteBits16` at `140336860`,
  `NetworkBitWriter_WriteBits32` at `1403368f0`, and
  `NetworkBitWriter_WriteBits64` at `1400a7540`.
- The write helpers mask the requested low bits, shift them into the current
  byte/word at the current bit offset, advance the buffer pointer by
  `(bitOffset + bitCount) >> 3`, and retain `(bitOffset + bitCount) & 7` as the
  next bit offset. This confirms NexusForever's `GamePacketWriter` bit order is
  least-significant-bit first and already matches the client writer.
- The matching read helpers are now labelled:
  `NetworkBitReader_ReadString` at `140336980`,
  `NetworkBitReader_ReadWideString` at `140336a40`,
  `NetworkBitReader_LoadNextSegment` at `140336b00`,
  `NetworkBitReader_ReadBits` at `140336c60`, and
  `NetworkBitReader_ReadBitsAcrossSegments` at `140336d60`.
- `NetworkBitReader_ReadString` and `NetworkBitReader_ReadWideString` use the
  same one-bit extended-length flag and 7-bit/15-bit length fields that
  NexusForever's `GamePacketReader.ReadString` and `ReadWideString` use. The
  scalar bit reader extracts low bits from the current byte at the current bit
  offset, matching NexusForever's least-significant-bit-first `ReadBits` loop.

### Client DB table registration

Both `WildStar64.exe` and `Houston64.exe` contain table-registration patterns
that call into a common loader with the display table name and `DB\*.tbl` path.

Useful examples:

- `WildStar64.exe`: `RealmDataCenter` / `DB\RealmDataCenter.tbl` at `selected_decompiled.c:40`.
- `WildStar64.exe`: `WorldSocket` / `DB\WorldSocket.tbl` at `selected_decompiled.c:137`.
- `Houston64.exe`: `RealmDataCenter` / `DB\RealmDataCenter.tbl` at `selected_decompiled.c:110`.
- `Houston64.exe`: `WorldSocket` / `DB\WorldSocket.tbl` at `selected_decompiled.c:207`.

This supports using the decompile to validate `Tools\DataMapping\map_wildstar_data.py`
for table names and field consumers, especially `WorldSocket.tbl.sql` and
`RealmDataCenter.tbl.sql`.

### Public event and spell/data names

`WildStar64.exe` includes public-event enum/name registration around
`exports\WildStar64.exe\selected_decompiled.c:8975`, including names such as
`PublicEventObjectiveType_Script`, `PublicEventObjectiveType_ParticipantsInTriggerVolume`,
and `PublicEventObjectiveType_KillEventObjectiveUnit`.

This is useful evidence for checking:

- `Source\NexusForever.Game.Static\PublicEvent\PublicEventObjectiveType.cs`
- `Source\NexusForever.Game\PublicEvent\*.cs`
- current expedition scripts that update public-event objectives

The selected export also captures `tSpell4IdAbility` at
`exports\WildStar64.exe\selected_decompiled.c:9570` and `tUnitProperty` at
`selected_decompiled.c:22717`, which are good anchors for spell effect and unit
property interpretation work.

Follow-up implemented from this pass:

- `PublicEventStatus_*` values in the Lua registration match
  `Inactive = 0`, `Active = 1`, `Succeeded = 2`, and `Failed = 3` around
  `selected_decompiled.c:9039`.
- `PublicEventObjectiveNotificationMode_*` values match `Normal = 0`,
  `LowTime = 1`, `Warn = 2`, `Serious = 3`, `Critical = 4`, and
  `Achieving = 5` around `selected_decompiled.c:9075`.
- `PublicEventObjectiveCategory_*` values match `Main = 0`, `Optional = 1`,
  `PlayerPath = 2`, and `Challenge = 3` around `selected_decompiled.c:9129`.
- The client exposes `PublicEventObjectiveType_DefendObjectiveUnits` at value
  `12` around `selected_decompiled.c:9273`; NexusForever keeps the existing
  singular member and now has a plural alias for client-facing naming parity.

These constants now have explicit values in the static enums where they were
previously implicit, and the export script keeps these public-event constant
names as high-value anchors.

### Skill and spell binding follow-up

`Game.Spell` is now labelled as a focused WildStar64 cluster:
`Lua_GameSpell_Equals` at `1405e7fd0`,
`Lua_GameSpell_CreateOrWrap` at `1405e8070`,
`Lua_GameSpell_Gc` at `1405e81b0`, and
`Lua_RegisterGameSpellBindings` at `1405e8230`. Export-only runs apply these
names from `function_labels.csv`; the latest run reported 94 applied labels and
0 missing labels.

`Lua_RegisterGameSpellBindings` starts around
`exports\WildStar64.exe\selected_decompiled.c:4564`. The registration walks the
indirect `Game.Spell` method table, then installs spell-facing Lua enums:

- `CodeEnumCastMethod` around `selected_decompiled.c:4722` confirms
  `Normal = 0`, `Channeled = 1`, `PressHold = 2`,
  `ChanneledField = 3`, `UNUSED04 = 4`, `ClientSideInteraction = 5`,
  `RapidTap = 6`, `ChargeRelease = 7`, `Multiphase = 8`,
  `Transactional = 9`, and `Aura = 10`. `Aura` is referenced through
  `DAT_140b234a0` around `selected_decompiled.c:4822`; the local
  `WildStar64.exe` bytes at `0x140b234a0` decode to the ASCII string `Aura`.
- `CodeEnumSchool` around `selected_decompiled.c:4834` confirms
  `Spell = 0`, `Melee = 1`, `Ranged = 2`, and `Unarmed = 3`.
- `CodeEnumAOESelectionType` around `selected_decompiled.c:4986` confirms the
  existing `AoeSelectionType` order: `None = 0`, `Closest = 1`,
  `Furthest = 2`, `Random = 3`, `LowestAbsoluteHealth = 4`, and
  `MissingMostHealth = 5`.
- `CodeEnumCastResult`, `CodeEnumSpellEffectType`, `CodeEnumSpellTag`,
  `CombatMessageType`, and `CodeEnumSpellClass` are also registered here, but
  still need table-name extraction before adding more enum members safely.

The decoded method table at `140c5a480` has 46 `Game.Spell` entries. Besides the
basic identity and tooltip methods, it maps skill-system and LAS-facing methods
such as `GetCasterInnateRequirements`, `GetCasterInnateCosts`,
`GetTargetInnateRequirements`, `GetAbilityCharges`, `IsSelfSpell`,
`IsFreeformTarget`, `GetLasTierDesc`, `GetLasBonusEachTierDesc`, and
`GetSpellServiceTokenCost`. The direct callback labels now make those methods
available in `selected_decompiled.c` without guessing from string proximity.

`Lua_GameSpell_GetCastMethod` at `selected_decompiled.c:7028` and
`Lua_GameSpell_GetSchool` at `selected_decompiled.c:7067` both resolve the
`Game.Spell` userdata through the spell service and return numeric enum values.
`Lua_GameSpell_GetSpellServiceTokenCost` at `selected_decompiled.c:8932` checks
a spell metadata flag before returning a decoded 15-bit service-token value.

The LAS/action-set bridge is now labelled too. The decoded `ActionSetLib` table
at `140b75900` maps `IsSlotUnlocked` to `Lua_ActionSetLib_IsSlotUnlocked` at
`selected_decompiled.c:10484`, `RequestActionSetChanges` to
`Lua_ActionSetLib_RequestActionSetChanges` at `selected_decompiled.c:10529`, and
`GetCurrentActionSet` to `Lua_ActionSetLib_GetCurrentActionSet` at
`selected_decompiled.c:10724`.

`Lua_RegisterActionSetLib` at
`exports\WildStar64.exe\selected_decompiled.c:10789` installs the `ActionSetLib`
method table, `CodeEnumLimitedActionSetResult`, and `CodeEnumShortcutSet`. The
limited-action-set result table registers `0x2e` entries, matching the 46 values
already present in NexusForever's `LimitedActionSetResult` enum. The adjacent
client string table confirms the same names, with the Lua-facing Eldan
augmentation labels written as `EldanAugmentation_*`.

`CodeEnumShortcutSet` confirms `VehicleBar = 0`, `PrimaryPetBar = 1`,
`PetMiniBar0 = 2`, `PetMiniBar1 = 3`, `PetMiniBar2 = 4`, `PetMiniBar3 = 5`,
`PetMiniBar4 = 6`, `FloatingSpellBar = 7`, `FloatingDynamicSpellBar = 8`, and
`Count = 9` around `selected_decompiled.c:10913`.

`Lua_ActionSetLib_RequestActionSetChanges` builds a Lua result table containing
`eResult`. It rejects malformed action-set input unless the collected payload is
exactly `0x30` bytes, which is 12 32-bit entries, and returns
`LimitedActionSetResult` values already present in NexusForever, including
`InvalidUnit = 0x17`, `InvalidActionSetTable = 0x19`,
`PlayerIsDead = 0x1b`, `RestrictedInPVP = 0x1c`, `InCombat = 0x1e`, and
`UpdateSpellInProgress = 0x26`.

The deeper action-set callbacks are now labelled as well:
`ActionSet_CheckPvPRestriction` at `selected_decompiled.c:2757`,
`ActionSet_SendPendingActionSetChanges` at `selected_decompiled.c:2792`,
`ActionSet_CheckUpdateSpellInProgress` at `selected_decompiled.c:3043`, and
`ActionSet_ValidateRequestedChanges` at `selected_decompiled.c:3362`.
`ActionSet_SendPendingActionSetChanges` builds opcode `0xb1`
(`ClientRequestActionSetChanges`) with 12 actions, the current spec index,
changed spell/tier pairs, and AMP ids. `ActionSet_ValidateRequestedChanges`
returns `InvalidActionSetSize = 0x16` unless the list is exactly 12 entries, and
returns `InvalidSlot = 0x18` when the first eight LAS slots do not use
`Spell4BaseEntry.WeaponSlot == 5` or when slots 8-11 do use that value.

Follow-up implemented from this pass:

- Expanded the label map from the partial `Game.Spell` cluster to the full
  46-entry Lua method table, replacing the generic late-binding names with
  method-specific labels.
- Added `ActionSetLib` labels for `IsSlotUnlocked`, `RequestActionSetChanges`,
  and `GetCurrentActionSet`.
- Added labels for the action-set preflight/send helpers behind
  `RequestActionSetChanges`.
- Added `SpellCastMethod` and `SpellSchool` static enums using the client
  values above.
- `ISpellBaseInfo` and `SpellBaseInfo` now expose typed `CastMethod` and
  `School` values from `Spell4BaseEntry.CastMethod` and `Spell4BaseEntry.School`.
- `/spell inspect` and `/spell inspect4` now print cast method and school next
  to spell class, which makes skill-system and action-set investigation easier
  while comparing server data against the client-facing Lua model.
- `ClientRequestActionSetChangesHandler` now rejects 3-bit action-set indices
  outside the four server action sets with `LimitedActionSetResult.InvalidSpecIndex`
  instead of throwing before a client response can be sent.
- The same handler now applies the client-confirmed 12-action request size and
  LAS slot `WeaponSlot == 5` validation before mutating the server action set,
  returning the matching `LimitedActionSetResult` failure when the packet is
  malformed.

Second spell-binding follow-up implemented from this pass:

- `Lua_RegisterGameSpellBindings` registers `CodeEnumSpellTag` with 11 entries
  around `exports\WildStar64.exe\selected_decompiled.c:4663`, with the loop
  count visible at `selected_decompiled.c:4671`.
- `Tools\DataMapping\output\client_source_spell4tag_map.csv` maps the client
  spell tag rows as `Assault = 1`, `Support = 2`, `Path = 3`, `Misc = 4`,
  `Mount = 5`, `Utility = 7`, and unnamed entries at `6`, `8`, `9`, `10`,
  and `11`.
- `Source\NexusForever.Game.Static\Spell\SpellTag.cs` now carries those explicit
  values, and `/spell inspect` plus `/spell inspect4` print decoded spell tags
  from the five `Spell4TagId` fields to make follow-up spell-family analysis
  easier.
- The previously undecoded `CodeEnumCastMethod` value after `Transactional` is
  now mapped as `Aura = 10`; `Source\NexusForever.Game.Static\Spell\SpellCastMethod.cs`
  exposes it for spell metadata inspection and future Lua-parity checks.

Third spell-binding follow-up implemented from this pass:

- `Lua_GameSpell_ShouldHideCooldownInTooltip` at
  `exports\WildStar64.exe\selected_decompiled.c:8891` reads
  `Spell4.PropertyFlags & 0x200` and returns the one-bit result.
- `Lua_GameSpell_GetSpellServiceTokenCost` at
  `exports\WildStar64.exe\selected_decompiled.c:8932` checks
  `Spell4.PropertyFlags & 0x20000000` before resolving a spell-specific service
  token cost.
- `Tools\DataMapping\output\client_source_spell4servicetokencost_map.csv:1`
  currently maps 6 concrete `Spell4ServiceTokenCost` rows, including recall and
  transmat spells with a cost of `10`.
- `ISpellInfo` / `SpellInfo` now surface typed spell property flags,
  cooldown-tooltip suppression, and mapped service-token-cost rows. `/spell
  inspect` and `/spell inspect4` print the decoded property flag state and
  service-token cost for follow-up spell-system investigation.
- No spell runtime spending behavior was changed in this pass; the new server
  surface is diagnostic-only until a verified client/server use site is mapped.

Fourth spell/action-set follow-up implemented from this pass:

- `Lua_RegisterActionSetLib` is now named in `function_labels.csv`, keeping the
  client enum-registration evidence reproducible in future export-only runs.
- `Source\NexusForever.Game.Static\Spell\ShortcutSet.cs` now includes the
  client-confirmed `Count = 9` sentinel alongside the already-mapped pet mini
  bars.
- The existing `LimitedActionSetResult` values were checked against the
  client-side 46-entry registration table; no packet enum value changes were
  needed.

Fifth spell-binding follow-up implemented from this pass:

- `Lua_GameSpell_IsBeneficial` at
  `exports\WildStar64.exe\selected_decompiled.c:6654` reads
  `Spell4.PropertyFlags & 0x04000000` and returns the single-bit result.
- `Source\NexusForever.Game.Static\Spell\SpellPropertyFlags.cs` now maps that
  client-confirmed bit as `IsBeneficial = 0x04000000`, and `ISpellInfo` /
  `SpellInfo` surface it for diagnostics.
- `/spell inspect` and `/spell inspect4` now print the decoded `beneficial`
  state alongside the existing property-flag, cooldown-tooltip, and
  service-token-cost diagnostics.
- `GameFormula 1307` maps rapid transport to two concrete spells in the client
  tables: `82922` (`Rapid Transport - Global - Tier 1`, one-hour cooldown) and
  `82956` (`Rapid Transport - Global - Service Tokens - Tier 1`, zero
  cooldown).
- `ClientRapidTransportHandler` still always casts `GameFormula 1307.Dataint0`,
  and `ClientRapidTransport` currently exposes only `TaxiNode` plus an opaque
  `Time` field. Because spell `82956` is not present in the six currently mapped
  `Spell4ServiceTokenCost` rows, the service-token rapid-transport payment path
  remains evidence-incomplete and was intentionally left unimplemented.

Sixth resurrection/service-token follow-up implemented from this pass:

- `Lua_RegisterAccountEntitlementEnums` at
  `exports\WildStar64.exe\selected_decompiled.c:3412` now has a reproducible
  label. It registers account currency/operation enums and the entitlement enum,
  including `WakeHereCooldownReduction` around `selected_decompiled.c:3664` and
  `selected_decompiled.c:3718`.
- `Lua_BuildResurrectionInfoTable` at
  `exports\WildStar64.exe\selected_decompiled.c:3972` builds the resurrection
  Lua table fields `bIsDead`, `bHasCasterRezRequest`, `fDeathPenalty`,
  `fWakeHereCooldown`, `fForceRezTimer`, `monRezCost`,
  `monRezServiceTokenCost`, `bWakeHere`, `bHolocrypt`, `bExitInstance`,
  `bAcceptCasterRez`, and `bWakeHereServiceToken`.
- The client constructs `monRezServiceTokenCost` from `GameFormula 0x523` around
  `selected_decompiled.c:4198`, matching NexusForever's resurrection
  service-token cost source. `ResurrectionManager` now uses a named
  `WakeHereServiceTokenCostGameFormulaId` constant instead of an opaque decimal
  literal.
- The service-token resurrection option is driven by the `WakeHereServiceToken`
  resurrection flag bit; `TimeUntilWakeHereMs` is the separate wake-here cooldown
  timer. `ServerResurrectionShow` now documents that distinction.

Seventh rapid-transport follow-up implemented from this pass:

- `Decomp\Analysis\scripts\ExportNexusForeverAnalysis.java` now treats
  `monAltCostRapidTransport`, `monCostRapidTransport`,
  `bRapidTransportAllowed`, and `GetRapidTransportCooldown` as high-value
  export anchors. The refreshed WildStar64 export pulled in the map rapid-
  transport node builder and preserved it through
  `function_labels.csv` as `Map_BuildRapidTransportNodeInfoTable`.
- `Map_BuildRapidTransportNodeInfoTable` at
  `exports\WildStar64.exe\selected_decompiled.c:9595` builds per-node map UI
  data including `idNode`, `eType`, `strName`, `bUnlocked`,
  `nRecommendedMinLevel`, `nRecommendedMaxLevel`, `nAutoUnlockLevel`,
  `bRapidTransportAllowed`, `monCostRapidTransport`,
  `monAltCostRapidTransport`, `bTransportAllowed`, `bTaxiAllowed`,
  `tLocation`, and `idMapZone`.
- Both rapid-transport cost fields are wrapped as `Game.Money` objects around
  `selected_decompiled.c:9830` and `selected_decompiled.c:9858`, which means
  the client map-destination pricing path is credit/money-based rather than a
  direct `Spell4ServiceTokenCost` lookup.
- The client DB schema separately exposes `TaxiRoute.tbl.sql` with a single
  `price` column, making taxi-route pricing a much stronger fit for
  `monCostRapidTransport` than the spell service-token table. The exact source
  of `monAltCostRapidTransport` is still not decoded.
- This narrows the remaining gap: `GameFormula 1307` still exposes the normal
  versus service-token rapid-transport spell pair, but the map destination UI is
  a separate money-pricing subsystem. `ClientRapidTransportHandler` therefore
  still should not implement payment or cooldown-bypass logic until the packet's
  `Time` semantics and the `GetRapidTransportCooldown` / spell-selection path
  are both confirmed.

Eighth rapid-transport follow-up implemented from this pass:

- `ExportNexusForeverAnalysis.java` now writes `string_xrefs.csv` for
  interesting string references. This exposed the map Lua registration table
  entries for `GetRapidTransportDestinationsForWorld`,
  `GetNearestRapidTransportNodeForWorldLocation`, and
  `GetRapidTransportCooldown` at `string_xrefs.csv:5462`.
- The map Lua callbacks are now reproducibly labelled:
  `Map_GetTaxisForWorld` at `140706f90`,
  `Map_GetNearestRapidTransportNodeForWorldLocation` at `140707020`,
  `Map_GetRapidTransportDestinationsForWorld` at `140707640`, and
  `Map_GetRapidTransportCooldown` at `1407076d0`. The refreshed export applied
  99 WildStar64 labels with 0 missing labels.
- `Map_GetRapidTransportDestinationsForWorld` calls the same destination-list
  builder as the per-node map UI path around
  `exports\WildStar64.exe\selected_decompiled.c:10381`, while
  `Map_GetNearestRapidTransportNodeForWorldLocation` consumes a Lua `position`
  table and returns the nearest rapid-transport node around
  `selected_decompiled.c:10109`.
- `Map_GetRapidTransportCooldown` resolves `GameFormula 0x51b` (`1307`) and
  uses the formula's first integer payload as the spell id around
  `selected_decompiled.c:10432`. It then walks the player's spell cooldown list
  and returns the remaining value in seconds around `selected_decompiled.c:10477`.
- `ClientRapidTransportHandler` now uses a named
  `RapidTransportSpellGameFormulaId` constant and validates that the
  client-confirmed normal spell id (`Dataint0`, currently `82922`) exists before
  casting it. `Dataint01` (`82956`) is still documented as the service-token
  spell candidate, but payment/cooldown-bypass behavior remains blocked because
  the packet `Time` field and service-token selection path are not mapped.
- Verification: refreshed `WildStar64.exe` with an export-only Ghidra pass using
  `-Targets WildStar64.exe -MaxDecompiledFunctions 520`, then ran
  `dotnet build Source\NexusForever.sln --no-restore`; the build succeeded with
  the existing `SharpCompress` NU1902 advisory warnings in
  `NexusForever.MapGenerator`.

Ninth GameLib follow-up implemented from this pass:

- A widened WildStar64 export finally surfaced `FUN_140709460`, and the Lua
  registration fanout in that export passed the function to the loader as
  `GameLib`. `function_labels.csv` now records it as `Lua_RegisterGameLib`, and
  a follow-up export-only verification pass with
  `-Targets WildStar64.exe -MaxDecompiledFunctions 300` keeps it in
  `exports\WildStar64.exe\selected_decompiled.c:10484` with `100` applied
  WildStar64 labels and `0` missing labels.
- `Lua_RegisterGameLib` begins by creating the `GameLib` table, then populates
  client-facing enums and constants rather than a simple pointer-table wrapper.
  Early in the body it registers the resurrection-facing `RezType` values,
  including `WakeHere = 1`, `Holocrypt = 2`, `SpellCasterLocation = 4`,
  `ExitInstance = 32`, and `WakeHereServiceToken = 64`, matching the previously
  mapped resurrection option strings.
- The same body also exposes broad client constants such as
  `CodeEnumMapOverlayType`, `CodeEnumEquipItemResult`, input-device/input-event
  enums, `CodeEnumCombatResult`, `CodeEnumRace`, `CodeEnumClass`,
  `CodeEnumRecallCommand`, `CodeEnumItemSlots`, and a live `MapZone` export.
- The still-blank `string_xrefs.csv` rows for `RequestRewardUpdate`,
  `GetWorldHeroismMenaceLevel`, `GetWorldMaxPrimeLevel`, and
  `GetWorldPrimeLevel` did not resolve just because `Lua_RegisterGameLib`
  surfaced. Those names still do not appear as direct string literals in the
  decompiled `GameLib` body, so they remain blocked on separate callback-table
  or helper-function mapping rather than this top-level registration routine.

Tenth GameLib follow-up implemented from this pass:

- A targeted walk of the data neighborhood around `140b73550` showed the
  unresolved `GameLib` names live in an alternating `string pointer` /
  `callback pointer` table rather than as direct xrefs inside
  `Lua_RegisterGameLib`. The same cluster also exposes
  `GetPrimeLevelAchieved`, `ConfirmPartialUnlock`, `GetUnlocksForType`, and
  `SalvageKeyCount` entries nearby.
- `function_labels.csv` now also records `Lua_GameLib_GetPrimeLevelAchieved`
  at `140708d50`, alongside the already-resolved callback-table entries for
  `Lua_GameLib_GetWorldPrimeLevel` (`140708da0`),
  `Lua_GameLib_GetWorldMaxPrimeLevel` (`140708dd0`),
  `Lua_GameLib_GetWorldHeroismMenaceLevel` (`140708e60`), and
  `Lua_GameLib_RequestRewardUpdate` (`1407091e0`). `string_xrefs.csv` now
  resolves those four previously blank rows to the correct callback addresses.
- `Lua_GameLib_GetWorldPrimeLevel` and
  `Lua_GameLib_GetWorldHeroismMenaceLevel` are short code stubs that Ghidra had
  not promoted to functions automatically. `ApplyNexusForeverLabels.java` now
  creates a function at an instruction address before naming it, which is why
  the durable labels survive export-only passes cleanly.
- The traced bodies match the callback names closely enough for implementation
  notes: `GetWorldPrimeLevel` reads a global world-prime value from the main
  client state block, `GetWorldMaxPrimeLevel` computes the remaining available
  prime level from active world data, `GetWorldHeroismMenaceLevel` reads the
  current world-state object at `DAT_140c65898 + 0x7258` and returns field
  `0x54` when present, and `RequestRewardUpdate` forwards to the existing
  `Reward_SendRewardUpdateRequest` helper at `140636ba0`.
- `Reward_SendRewardUpdateRequest` throttles indices below `7`, then sends
  world opcode `0x07CC` with that index. NexusForever now names that opcode
  `ClientRewardUpdateRequest`, reads the single `uint` index, and logs/ignores
  the unsupported request rather than treating it as an unknown packet. No
  reward rotation state is mutated because the server response/update semantics
  are still unmapped.
- Two small headless helpers, `DumpNearbyData.java` and
  `InspectCodeAddress.java`, were added under `Decomp\Analysis\scripts` to
  support future callback-table and raw-stub tracing. The
  `run_ghidra_analysis.ps1` wrapper now accepts `-ExtraPostScript` and
  `-ExtraPostScriptArgs`, so those helpers can run as part of repeatable
  export-only passes.
- Verification: `.\Decomp\Analysis\run_ghidra_analysis.ps1 -ExportOnly
  -Targets WildStar64.exe -MaxDecompiledFunctions 340 -ExtraPostScript
  DumpNearbyData.java -ExtraPostScriptArgs @('140b73540','20')` applied `106`
  WildStar64 labels with `0` missing labels. The refreshed export includes
  `Reward_SendRewardUpdateRequest` at
  `exports\WildStar64.exe\selected_decompiled.c:8975`,
  `Lua_GameLib_GetPrimeLevelAchieved` at `:10527`,
  `Lua_GameLib_GetWorldPrimeLevel` at `:10553`,
  `Lua_GameLib_GetWorldMaxPrimeLevel` at `:10576`,
  `Lua_GameLib_GetWorldHeroismMenaceLevel` at `:10617`, and
  `Lua_GameLib_RequestRewardUpdate` at `:10643`. `dotnet build
  Source\NexusForever.sln --no-restore` succeeded with the existing
  `SharpCompress` NU1902 advisories and `Spline.formation` CS0649 warning.

Eleventh rapid-transport pricing follow-up implemented from this pass:

- Targeted `InspectCodeAddress.java` traces on `FUN_1404adc50`,
  `FUN_1404af440`, and `FUN_1404af1e0` show that the map rapid-transport cost
  builders are formula-driven and not a plain `TaxiRoute.price` passthrough.
  `Map_BuildRapidTransportNodeInfoTable` still emits `Game.Money` objects for
  both `monCostRapidTransport` and `monAltCostRapidTransport`, but the amounts
  come from native helper logic that re-enters the formula system.
- `FUN_1404adc50`, the helper behind the first emitted cost object, re-reads
  `GameFormula 0x51b` (`1307`) and caches both `Dataint0` and `Dataint01`, the
  same rapid-transport spell pair already surfaced through
  `Map_GetRapidTransportCooldown`. One branch triggers when the route selector
  matches `Dataint01`; another triggers when the selector is `0` and the
  player's live list contains spell `Dataint0`.
- In that branch, the helper emits the exact same `Game.Money` tuple metadata
  used by the already-mapped resurrection `monRezServiceTokenCost` path:
  amount plus `0xf` and `0x900000000` before wrapping the value as
  `Game.Money`. Because `Lua_BuildResurrectionInfoTable` uses that tuple for the
  known `WakeHereServiceToken` pricing path, this is strong evidence that one
  rapid-transport pricing branch is service-token-tagged rather than purely a
  credit-side alternate vendor price.
- The service-token-tagged amount comes from `FUN_1404af440`, which follows the
  rapid-transport entry into linked route/world data and applies
  `GameFormula 0x51e` using `Dataint0`, `Dataint01`, and a `Datafloat02` term
  before clamping the result to the formula's `Dataint01` ceiling.
- The fallback amount comes from `FUN_1404af1e0`, which uses the same linked
  route/world data but computes a distance-sensitive value from
  `GameFormula 0x51e` (`Datafloat0`, `Datafloat01`) plus `GameFormula 0x3b8`
  (`Datafloat0`). When the world/location selector matches, it adds live
  spatial distance to that base; otherwise it uses the alternate `0x51e`
  float term instead.
- This revises the earlier interpretation from the seventh rapid-transport
  follow-up: the client map pricing path is not yet proven to be a direct
  `TaxiRoute.price` projection, and it is not purely credit-based. The next
  remaining evidence gap is the exact selector argument passed into these
  helpers, which still blocks safe server-side payment or spell-selection
  changes for rapid transport.

Twelfth rapid-transport UI→send-chain follow-up implemented from this pass:

- Re-verified the Lua anchor chain for the rapid-transport map UI:
  `Map_GetRapidTransportDestinationsForWorld` (`140707640`) dispatches to
  `FUN_140706ce0(..., 0)` at
  `exports\WildStar64.exe\selected_decompiled.c:10445`, and that helper emits
  per-node map rows through `Map_BuildRapidTransportNodeInfoTable` around
  `:10399`.
- The destination row field tied to outbound destination selection is now
  explicitly evidenced: `Map_BuildRapidTransportNodeInfoTable` writes `idNode`
  from the node record base id (`*(uint *)*param_3`) at
  `selected_decompiled.c:9691`. This is the client-side UI field that aligns
  with the packet/model `TaxiNode` (`ClientRapidTransport.TaxiNode`) consumed by
  opcode `ClientRapidTransport` (`0x0141`) in
  `Source\NexusForever.Network.World\Message\Model\ClientRapidTransport.cs:5-16`.
- `Map_GetRapidTransportCooldown` still only maps cooldown presentation:
  it reads `GameFormula 0x51b` at `selected_decompiled.c:10475` and returns
  cooldown seconds as `uVar4 * 0.001` at `:10520`; this path does not surface
  the outbound packet `Time` write.
- Expanded export evidence now also shows the client `RapidTransportResult`
  receive/event bridge (`FUN_140520710` at
  `selected_decompiled.c:17835-17909`), including default cast result `0x14a`
  (`RapidTransportInvalid`) and event dispatch. This confirms receive-side
  handling, but still does not expose the outbound serializer for opcode
  `0x0141`.
- `Lua_RegisterGameLib` still only registers the `RapidTransport` enum token
  (`string_xrefs.csv:5501`, `strings.csv:41774`) rather than a directly named
  send callback. No new label was added for a send helper because the
  client-side opcode-`0x0141` serializer and the exact source of packet `Time`
  are still not uniquely proven from current exports.
- Conclusion for this todo scope: `TaxiNode` population is now mapped to the
  map-row `idNode` field, but packet `Time` semantics remain blocked/unverified
  pending a direct mapping of the `ClientRapidTransport` send serializer path.

Spell-targeting follow-up implemented from this pass:

- `Lua_GameSpell_IsMovingInterrupted` at
  `exports\WildStar64.exe\selected_decompiled.c:10802` reads
  `Spell4Base.TargetingFlags & 0x40` and returns the one-bit result.
- `Lua_GameSpell_IsFreeformTarget` at
  `exports\WildStar64.exe\selected_decompiled.c:10734` reads
  `Spell4Base.TargetingFlags & 0x400000` and returns the one-bit result.
- `Lua_GameSpell_GetLasTierDesc` at
  `exports\WildStar64.exe\selected_decompiled.c:10843` and
  `Lua_GameSpell_GetLasBonusEachTierDesc` at `:10943` resolve localized LAS
  description ids through the spell metadata object; this pass keeps that as a
  diagnostic-only server surface until the exact entry/base field pairing is
  fully proven.
- `Source\NexusForever.Game.Static\Spell\SpellTargetingFlags.cs` now names the
  two client-confirmed targeting bits as `InterruptOnMove = 0x40` and
  `FreeformTarget = 0x400000`.
- `ISpellBaseInfo` / `SpellBaseInfo` now surface typed targeting flags plus
  `IsFreeformTarget` and `IsMovingInterrupted`, and `/spell inspect` prints the
  targeting flags, those booleans, and the LAS description ids for follow-up
  skill/LAS work.
- `Spell.IsMovingInterrupted()` no longer uses the old `CastTime > 0`
  placeholder; it now follows the client-confirmed targeting-flag bit instead.

Spell self-targeting and LAS text follow-up implemented from this pass:

- `Lua_GameSpell_IsSelfSpell` at
  `exports\WildStar64.exe\selected_decompiled.c:10656` returns true for a
  proven self-AOE path (`TargetType == 2`) and also includes unproven target
  type `0`, a type-`7` service lookup, and a conditional target-AOE branch that
  depends on an undecoded target-mechanics field.
- `CharacterSpell.ResolvePrimaryTargetId()` now treats `TargetType == 2`
  (`self AOE`) as caster-targeted by returning the player/unit guid directly,
  which is the minimal safe server-side alignment with the proven client path.
- `Lua_GameSpell_GetLasTierDesc` and `Lua_GameSpell_GetLasBonusEachTierDesc`
  resolve localized text rather than returning raw ids. The decompiled accessors
  line up with `Spell4.LocalizedTextIdLASTier` and
  `Spell4Base.LocalizedTextIdLASTierPoint` before passing those ids into the
  client localization layer.
- `/spell inspect` now resolves those LAS ids through the server English text
  table and prints id plus resolved text, while preserving the raw ids inside
  the same diagnostic line.
- `TraceFunctionCallers` follow-up on `FUN_1403f4900` recovered a real caller at
  `logs\trace_callers_1403f4900.txt:2048` that loads `EDX = 0x788` before
  calling the helper, which confirms the type-`7` `IsSelfSpell` path is walking
  a live spell-id keyed tree rooted at `DAT_140c65b70 + 0x788` rather than dead
  defensive code. The decompiled branch compares node keys at `+0x20`, then
  dereferences secondary spell data from `+0x28` before re-entering
  `FUN_1403acd90` and `FUN_1403b4ec0`.
- Follow-up helper tracing sharpens that type-`7` picture further without yet
  making it implementation-safe. `FUN_1407a0fd0` appears to be the spell-wrapper
  lookup/resolution helper used after the service-tree branch, and
  `Lua_GameSpell_IsFreeformTarget` reads the resolved wrapper's flags from
  `+0x108` after that lookup. `FUN_1403b4ec0` remains an undecompiled 56-byte
  bool helper invoked on the resolved wrapper in `IsSelfSpell`, so the safest
  current server/runtime label is still "resolved-spell service-lookup branch"
  rather than a concrete mechanic name.
- `TraceFunctionCallers` follow-up on `FUN_1403acd90` also recovered multiple
  wrapper users that materialize `+0x9c` and read `[RAX + 0x80]`, which lines
  up with the unresolved `IsSelfSpell` conditional target-AOE gate at wrapper
  offset `0x9c`. Exact semantics remain unresolved, so this is still
  documentation-only evidence.
- A later caller trace against `FUN_1405cca00` turned out to be a false lead
  for spell targeting. That function sits in the already-tracked
  lifecycle/state replay chain (`FUN_1403e85d0 -> FUN_1405cc090 ->
  FUN_1405cca00/FUN_1405cd160 -> FUN_1405cd070/FUN_1405cd200`) and does not add
  new evidence for `Game.Spell:IsSelfSpell`, type `7`, or the `0x9c` gate.
- Two further nearby addresses also closed as false leads for spell targeting.
  Direct inspect/caller follow-up confirms `FUN_1403e85d0` stays in the same
  lifecycle/state replay chain with heavy transform/physics-style work, while
  `FUN_14089ae40` belongs to the already-tracked audio dispatch/configuration
  path downstream of `140899fd0`. Neither contributes new evidence for
  `Game.Spell:IsSelfSpell`, type `7`, or the `0x9c` gate.
- The type-`7` self-spell branch, type-`0` meaning, and the extra target-AOE
  condition remain evidence-only until the target-mechanics wrapper layout is
  decoded more completely.
- Verification: `dotnet build Source\NexusForever.sln -v minimal --nologo`
  succeeds, with only existing package-version/vulnerability warnings.

Innate spell accessor and unresolved self-spell-branch follow-up implemented from this pass:

- Rechecked the deeper `Lua_GameSpell_IsSelfSpell` branches against the client
  wrapper layout shared by `Lua_GameSpell_IsFreeformTarget`. The remaining
  target-type `0`, target-type `7`, and conditional target-AOE behavior all
  depend on undecoded runtime wrapper fields and a service lookup path, so no
  further server-side self-targeting change was made in this pass.
- The live `Spell4TargetMechanics` table confirms those unresolved target types
  are not dead code: type `0` appears on mechanic ids `1`, `2`, and `44`; type
  `6` appears on mechanic id `17`; type `7` appears on mechanic ids `18`, `25`,
  `26`, `33`, `52`, `58`, `63`, and `69`. Existing spell-system notes still put
  type-`0` at `16,453` bases when paired with flags `1`, which raises its
  priority while still not making it safe to implement blindly.
- `Lua_GameSpell_GetCasterInnateCosts` at
  `exports\WildStar64.exe\selected_decompiled.c:8245` returns an array of up
  to two `{ costType, costValue }` pairs sourced from the two innate-cost slots
  on `Spell4`, while preserving the extra `InnateCostEMMId` payload through the
  client runtime path.
- `Lua_GameSpell_GetCasterInnateRequirements` at
  `exports\WildStar64.exe\selected_decompiled.c:8884` returns up to two
  `{ requirementType, requirementValue, evaluationMode }` tuples from the two
  caster innate requirement slots on `Spell4`.
- `Lua_GameSpell_GetTargetInnateRequirements` at
  `exports\WildStar64.exe\selected_decompiled.c:8998` returns a single target
  innate requirement tuple from `Spell4.TargetBeginInnateRequirement`,
  `TargetBeginInnateRequirementValue`, and `TargetBeginInnateRequirementEval`.
- `/spell inspect` now decodes innate costs with `Vital` names where possible,
  decodes caster and target innate requirement types through
  `PrerequisiteType`, decodes evaluation modes through `EvaluationMode`, and
  keeps unresolved `InnateCostEMMId` values raw instead of overnaming them.
- Follow-up trace on `InnateCostEMMId` still does not prove a meaning or any
  downstream consumer. The
  strongest structural clue is the parallel `Spell4Effects.EmmComparison` /
  `EmmValue` pair, so inspection output now explicitly flags non-zero `emmId`
  values as unknown semantics rather than silently printing bare integers.
- SQL data confirms those EMM fields are not just zero-filled schema padding.
  Representative non-zero `Spell4.InnateCostEMMId` rows include concrete
  `Spell4` ids `284`, `405`, `1691`, `1798`, and `1851`, while representative
  non-zero `Spell4Effects.EmmComparison` / `EmmValue` rows include effect ids
  `947`, `2555`-`2558`, `2569`-`2572`, `2816`-`2819`, and `2878`-`2881`.
  That is still not enough to name the semantics, but it does prove the fields
  carry live data and justifies keeping raw EMM diagnostics visible.
- The sampled non-zero rows also show a reusable inspection pattern. For the
  deprecated ESPer and Medic witnesses above, the concrete `Spell4` row carries
  exactly one non-zero innate EMM slot while the paired `Spell4Effects` rows
  start from a zero-EMM baseline effect and then step through a four-row EMM
  ladder with `emmValue` `1` -> `4`; some groups keep `emmComparison=0`
  throughout, while others flip the final row to `emmComparison=1`. That is
  strong enough for runtime-fixture selection and documentation, but still not
  strong enough to assign meaning to EMM itself.
- `/spell inspect` now also prints effect-row per-tick innate costs from
  `Spell4Effects.InnateCostPerTickType0/1` and
  `Spell4Effects.InnateCostPerTick0/1`, plus raw
  `Spell4Effects.EmmComparison` / `EmmValue`, alongside effect timing so live
  casts can correlate base innate costs with per-tick drains without guessing
  what EMM means yet.
- `/spell inspect` now also prints `Spell4Thresholds` rows keyed by the concrete
  `Spell4.Id`, including follow-up `spell4IdToCast`, duration, tooltip/icon,
  and threshold vital costs. Current evidence keeps these rows separate from
  EMM semantics because `Spell4Thresholds` has no EMM fields at all.
- Runtime candidates now include concrete inspect-first witnesses for the
  unresolved client target-mechanic families: type `0` (`Spell4=305`, known
  mechanics `1/2/44`), type `6` (`Spell4=5157`, mechanic `17`), and type `7`
  (`Spell4=339` and `Spell4=26813`, known mechanics `18/25/26/33/52/58/63/69`).

Thirteenth rapid-transport selector/layout follow-up implemented from this pass:

- `TraceFunctionCallers.java` now closes the remaining selector-source gap for
  `Map_BuildRapidTransportNodeInfoTable` (`140706140`):
  `Map_GetRapidTransportDestinationsForWorld` still routes through
  `FUN_140706ce0(..., 0)`, while `Map_GetTaxisForWorld` (`140706f90`) is the
  only recovered nonzero caller and tail-calls the same helper as
  `FUN_140706ce0(param_1, lVar2, 1)`.
- The shared node-table builder does not use that selector for money layout.
  Inside `Map_BuildRapidTransportNodeInfoTable`, selector `0` only chooses
  `FUN_1404ada70(nodeId)` for `bUnlocked`, while selector `1` swaps in
  `FUN_1404ad9b0(DAT_140c659d0, nodeId)`; the `bRapidTransportAllowed`,
  `monCostRapidTransport`, `monAltCostRapidTransport`, `bTransportAllowed`,
  and `bTaxiAllowed` branches remain shared.
- `FUN_1404ada70` and `FUN_1404ad9b0` both fall back into
  `FUN_1404af6b0(...)`, but selector `1` first probes a tree rooted at
  `param_1 + 0x110` and passes fallback mode `1` instead of `2`. This makes
  the nonzero selector a taxi-list unlock path, not a separate rapid-transport
  pricing selector.
- A new instruction-level helper script,
  `Decomp\Analysis\scripts\FindImmediateInstructions.java`, recovered the
  native opcode-registration entry for `ClientRapidTransport` inside
  `FUN_14006c290`: `(**(code **)*puVar1)(puVar1, 0x141, 8, &LAB_14007ab70,
  ClientRapidTransport_WritePayload, 0, 0);`.
- `ClientRapidTransport_WritePayload` (`14007ab80`) writes two packed fields:
  the first is masked with `0x3fff` and emitted over `0x0e` bits, and the
  second is emitted over `0x20` bits. The paired tiny stub at `14007ab70`
  advances by `0x2e` bits, matching $14 + 32 = 46$ bits in an 8-byte envelope.
- This native registration matches the current wire reader in
  `Source\NexusForever.Network.World\Message\Model\ClientRapidTransport.cs`:
  `TaxiNode = reader.ReadUShort(14u); Time = reader.ReadUInt();`. No packet
  layout fix is indicated by this pass; the remaining packet-side gap is only
  the provenance/meaning of the raw 32-bit second field currently exposed as
  `Time`.

Fourteenth send-helper callgraph follow-up implemented from this pass:

- Refreshed focused artifacts with `InspectCodeAddress.java` and
  `TraceFunctionCallers.java` for `1403f4900`, `1403993c0`, and
  `Network_SendMessageById` (`140332580`); outputs are in
  `Decomp\Analysis\logs\inspect_*.txt`, `trace_callers_*.txt`, and
  `trace_callers_1403f4900_summary.txt`.
- `FUN_1403f4900` is now confirmed as a generic opcode+payload send helper:
  it forwards `(opcode,payload)` through `(*DAT_140c65808 + 0x108)` and then
  invokes `(*DAT_140c65808 + 0xf0)` for follow-up send-path processing
  (`logs\inspect_1403f4900.txt:48-86`).
- Concrete opcode `0x0141` sender is isolated: `FUN_1403993c0` issues
  `FUN_1403f4900(lVar1,0x141,param_3)` (`logs\inspect_1403993c0.txt:48-82`);
  instruction trace confirms `MOV EDX,0x141` at `1403994bf` immediately before
  `CALL 0x1403f4900` at `1403994ca`
  (`logs\trace_callers_1403f4900.txt:527-539`).
- Rapid-transport anchoring is explicit in the caller chain:
  `RapidTransport_HandleResult@140520710 -> FUN_1403993c0`
  (`logs\trace_callers_1403993c0.txt:47-60`, plus
  `exports\WildStar64.exe\selected_decompiled.c:4595`).
- Remaining unknown surface is bounded: `FUN_1403f4900` has `345` traced
  callsites total, `323` with immediate opcode setup, and `22` non-immediate
  callsites still requiring deeper argument-flow recovery
  (`logs\trace_callers_1403f4900_summary.txt:1-31`).
- `Network_SendMessageById` remains an indirect endpoint in static xrefs for
  this pass (`logs\trace_callers_140332580.txt:43-52`), consistent with virtual
  callback dispatch rather than direct callers.

Fifteenth rapid-transport close-time-semantics blocker pass:

- Verified durable evidence surfaces are aligned for opcode `0x0141`: the sender
  label (`RapidTransport_SendClientRapidTransport`), immediate-opcode trace
  (`MOV EDX,0x141` before `FUN_1403f4900`), and call-chain anchor
  (`RapidTransport_HandleResult -> RapidTransport_SendClientRapidTransport ->
  Network_SendOpcodePayloadHelper`) are all present in source-controlled
  findings/labels.
- This evidence proves sender identity and opcode routing, but still does not
  map the second payload dword (`ClientRapidTransport.Time`) to a stable
  semantic domain (selector/cooldown token/nonce/timestamp). No deterministic
  server-side interpretation is yet justified.
- Server behavior therefore remains intentionally conservative: rapid transport
  mutation continues to key off validated destination node, cooldown spell id,
  and route price only; `Time` remains read-only opaque packet data.
- Source comments were updated in
  `Source\NexusForever.Network.World\Message\Model\ClientRapidTransport.cs` and
  `Source\NexusForever.WorldServer\Network\Message\Handler\Entity\Player\ClientRapidTransportHandler.cs`
  to make this no-change rationale explicit for future implementers.

Sixteenth transport callback-registration follow-up implemented from this pass:

- Refreshed callback-table evidence now resolves `PurchaseFlightPath` and
  `SetSendMessageResultFunction` from string-only xrefs into concrete
  `(name,function)` pairs:
  `140c5b620 -> 140b2b390("PurchaseFlightPath"), 140c5b628 -> 14065e6c0` and
  `140c5d770 -> 140b357c0("SetSendMessageResultFunction"), 140c5d778 ->
  1406a4b60` (`exports\WildStar64.exe\string_xrefs.csv:4041,4856`;
  `logs\WildStar64.DumpNearbyData.140c5b620.log:49-77`;
  `logs\WildStar64.DumpNearbyData.140c5d770.log:49-77`).
- `PurchaseFlightPath` now maps to a send path that is *not* rapid-transport:
  the callback body (`FUN_14065e6c0`) validates inputs and calls
  `FlightPath_SendPurchaseRequest` (`1404acfa0`), which sends opcode `0x00FF`
  via `Network_SendOpcodePayloadHelper`, not `0x0141`
  (`logs\WildStar64.InspectCodeAddress.14065e6c0.log:54-72`;
  `logs\WildStar64.InspectCodeAddress.1404acfa0.log:145-164`).
- `SetSendMessageResultFunction` / `SetReceivedMessageFunction` now map to
  `FUN_1406a4b60` / `FUN_1406a4ab0`; both parse `"Game.ICComm"` userdata and
  forward to `FUN_1406a4280` to register callback state on the ICComm object
  (`logs\WildStar64.DumpNearbyData.140c5d770.log:72-77`;
  `logs\WildStar64.InspectCodeAddress.1406a4b60.log:54-77`).
- `InvokeTaxiWindow` remains an event/UI branch in `FUN_1403a71f0`, reached from
  higher-level interaction handlers and firing
  `FUN_1400ea3e0(...,"InvokeTaxiWindow",...)`; it does not register or emit the
  rapid-transport payload path directly
  (`exports\WildStar64.exe\string_xrefs.csv:3009`;
  `exports\WildStar64.exe\selected_decompiled.c:18114-18115`;
  `logs\WildStar64.TraceFunctionCallers.ghidra.log:49-61`).
- Bound on `0x0141` second dword semantics is therefore unchanged: this callback
  chain does not provide deterministic meaning for the `ClientRapidTransport`
  32-bit field (`Time`), and no widened server interpretation is justified.
  Confidence: **medium-high** for registration/call-chain mapping,
  **high** for the negative conclusion that this branch is not the `0x0141`
  producer.

Seventeenth nearby-opcode helper-cluster mapping follow-up implemented from this pass:

- Enumerated immediate-opcode callsites in the same `FUN_1403f4900` neighborhood
  as `RapidTransport_SendClientRapidTransport` (`1403993c0`): `0x0141`
  (`1403993c0`), `0x00C2` (`1403994f0`), `0x0852` (`140399630`), and
  `0x084F`/`0x0850` (`140399780`) from contiguous `1403986f0-1403998bb`
  dispatcher code (`logs\trace_callers_1403f4900.txt:527-595`).
- Correlated several mapped opcodes to known meanings via source enum/models:
  `0x0141=ClientRapidTransport`,
  `0x084F=ClientCraftingComplexCraft`,
  `0x0850=ClientCraftingSimpleCraft`,
  `0x0852=ClientCraftingCraftItemAutoCraft`,
  `0x094F=ClientCastGuildBossToken`,
  `0x068C=ClientPathScientistSetScannerName`
  (`Source\NexusForever.Network\Message\GameMessageOpcode.cs:121,628,766-769,840`;
  `Source\NexusForever.Network.World\Message\Model\Crafting\ClientCraftingComplexCraft.cs:9-22`;
  `Source\NexusForever.Network.World\Message\Model\Crafting\ClientCraftingSimpleCraft.cs:8-17`;
  `Source\NexusForever.Network.World\Message\Model\Crafting\ClientCraftingCraftItemAutoCraft.cs:8-20`;
  `Source\NexusForever.Network.World\Message\Model\Spell\ClientCastGuildBossToken.cs:9-18`).
- Shared field evidence is now stronger: the same post-`FUN_1403988d0` read
  (`*(local_res10 + 0x60)`) is copied into outgoing payloads in
  `1403993c0`, `1403994f0`, `140399630`, `140399780`, and `1403991b0`
  before send (`logs\inspect_1403993c0.txt:76-81`;
  `logs\inspect_1403994f0.txt:83-85`;
  `logs\inspect_140399630.txt:82-97`;
  `logs\inspect_140399780.txt:82-101`;
  `logs\inspect_1403991b0.txt:99-104`).
- Comparison with known crafting payloads constrains `0x0141` interpretation:
  in crafting opcodes, this shared value occupies the
  `ClientSpellcastUniqueId` slot (first dword in
  `ClientCrafting*` models), so the `0x0141` second dword fed from the same
  source is unlikely to be wall-clock time and is more likely a spellcast
  correlation token/unique id. This is still an inference, not full proof.
- No new function labels were added in this pass because `0x00C2` remains
  unmapped in current source enums and the surrounding helper names are not yet
  uniquely disambiguated beyond opcode-level behavior.
- Confidence: **high** that `+0x60` is a shared cast-context token reused across
  this helper cluster; **medium-high** that `ClientRapidTransport.Time` should
  be treated as an opaque cast-correlation id rather than literal time.
- Server modeling decision (safe/minimal): keep gameplay behavior unchanged and
  treat opcode `0x0141` second dword as an opaque context token in diagnostics;
  compatibility alias `Time` can remain while direct proof is still pending.

Eighteenth combat-log serializer field mapping follow-up implemented from this pass:

- Refreshed concrete order for `CombatLog_HandleCCStateBreak` (`14060bbc0`): helper `FUN_14060b0c0` executes first, then `eState` from `*(param_1 + 0x10)`, then derived `strState` (`selected_decompiled.c:11198-11214`).
- Concrete helper map for `FUN_14060b0c0` (`14060b0c0`):
  - `unitCaster` write path reads payload-backed `*(param_1 + 0x8)` (`selected_decompiled.c:22641-22646`).
  - `unitCasterOwner` is client-local derivation via caster entity lookup and owner dereference (`FUN_1403d90d0` -> `FUN_14047dca0`) with conditional emit only when owner differs (`selected_decompiled.c:22647-22654`).
- Wire vs local boundary for CCStateBreak payload parity:
  - Payload-backed: `unitCaster` (`CasterId` parity), `eState` (`State` parity).
  - Client-local derivation: `unitCasterOwner`, `strState`.
- Explicit confidence by field:
  - `unitCaster`: **Mapped** (concrete offset/write path; parity-correlated with server `CombatLogCCStateBreak.CasterId`).
  - `unitCasterOwner`: **Verified (client-local only)** (runtime owner derivation; not payload).
  - `eState`: **Verified** (direct payload read/write and parity with server `CombatLogCCStateBreak.State` write contract).
  - `strState`: **Verified (client-local only)** (derived display text from `FUN_14034bdd0`, not event payload bytes).
  - Overall server parity (`CasterId` + `State`): **Mapped** (not fully **Verified** at raw bit-schema level because `DAT_1409eb20c` internals remain opaque in selected export).
- Sibling serializer boundary confirmed: nearby `FUN_14060b380` remains the broader cast-data serializer (`unitCaster`/`unitTarget`/`eCombatResult`/`splCallingSpell`), while CCStateBreak intentionally uses caster-only `FUN_14060b0c0` (`selected_decompiled.c:22666-22725`, `11263`, `22778`; `selected_xrefs.csv:2210-2215`).
- Label policy outcome: confidence now supports stable helper naming; added `14060b0c0 -> CombatLog_WriteCasterContext` in `Decomp\\Analysis\\function_labels.csv`.

Nineteenth CCStateBreak server-safe follow-up (diagnostics/model surface only):

- Updated `/spell inspect4` CCStateBreak runtime note to reflect the verified boundary: combat-log writes caster-context + `eState` per removed state; `strState` is client-local display derivation.
- Added explicit wire-contract comments in `CombatLogCCStateBreak` documenting payload-backed `CasterId` and 5-bit `State`, with `strState` intentionally excluded from the server wire model.
- Model decision remains intentionally conservative: no speculative CCStateBreak payload fields were added.
- Verification note: `dotnet build Source\\NexusForever.sln -v minimal --nologo` succeeds after these updates.

Twentieth activate-unit/audio follow-up implemented from this pass:

- Native registration now closes the nearby activate-unit-family packet shapes.
  Fresh decompile of the registration block at `14006c290` shows:
  `(..., 0x97, 8, 0x14007d830, FUN_14007ec70, 0, 0)`,
  `(..., 0x98, 8, 0x14007d830, FUN_14007ec70, 0, 0)`, and
  `(..., 0x96, 0x1c, &LAB_140093d20, FUN_140093d30, 0, 0)`.
  Shared stub `14007d830` advances `0x40` bits and shared serializer
  `FUN_14007ec70` writes two raw `0x20`-bit fields from `uint *param_2`, fully
  wire-proving opcode `0x0097` as `{ ContextToken, ActivateUnitId }` and
  proving that `0x0098` uses the same 64-bit layout with the same second-field
  source offset `+0x158`.
- `0x0096` is now also structurally wire-proven. Paired stub `140093d20`
  advances `0xa8` bits, and serializer `FUN_140093d30` writes a 32-bit leading
  field, two packed 4-bit selectors, a 32-bit resolved target entity id, and a
  trailing three-float world-position vector, for a total payload width of
  `32 + 4 + 4 + 32 + 96 = 168` bits.
- Direct follow-up on sender `ActivateUnit_SendClient0x0096Variant`
  (`14039ac90`) plus helper `SpellTarget_ResolveTargetEntity` (`14055bdc0`)
  now identifies the concrete field sources behind that 168-bit packet. The
  send buffer rooted at `&local_138` contains generated `ContextToken`, low
  nibble of `param_4`, low nibble of `param_3`, resolved target entity id from
  `lVar6 + 8` (or zero when no target entity resolves), and a trailing world
  position vector from `lVar6 + 0x11e0/+0x11e4/+0x11e8` or fallback player
  state at `param_1 + 0x6d10/+0x6d14/+0x6d18` when `lVar6 == 0`.
  `SpellTarget_ResolveTargetEntity` itself chooses that target entity from spell
  metadata, explicit target hints, and short-range fallback/proximity checks
  before returning the resolved world-entity object through `FUN_1403d90d0`.
  This fully resolves the structural question for `0x0096` as
  `{ ContextToken, two 4-bit selectors, target entity id, world position xyz }`
  while leaving the selector semantics intentionally unnamed.
- The `+0x348` runtime chain is now reinforced as audio output/resampler state
  rather than generic interpolation. Existing init path `AudioOutput_Initialize`
  (`1408340b0`) selects a `24000` or `48000` sample rate, initializes COM, and
  seeds the global resampler state with a normalized channel mask before
  `AudioResampler_InitialiseState` (`140863e80`) allocates per-channel sample
  storage.
- Supporting binary evidence now lines up with that audio interpretation:
  exports include `XAUDIO2_E_*` and `XACTENGINE_E_*` error strings, channel-map
  and wavebank diagnostics (`"Invalid entry count for channel maps."`,
  `"Requested audio format unsupported."`,
  `"No wavebank exists for desired operation."`), and COM imports including
  `CoInitializeEx` and `CoCreateInstance`.
- Backend construction is partially mapped too, but still not far enough for
  backend-specific names: `AudioBackend_TryCreateConfiguredBackend`
  (`14085ca20`) attempts one of two configured implementations before
  `AudioBackend_CreateFallbackBackend` (`14085cb20`) allocates a small fallback
  interface object. That is strong enough for generic backend labels, but not
  yet strong enough to call either branch specifically XAudio2 or DirectSound.

Twenty-first service-token/audio follow-up implemented from this pass:

- The unlabeled `0x00C2` helper-cluster path is now strong enough for durable
  native labels even though the public source opcode/model mapping is still not
  present. Current exported decompile already preserves
  `ClientSpellCastWithServiceToken_WritePayload` (`140089570`),
  `ServiceToken_SendClientSpellCastWithServiceToken` (`1403994f0`), and
  `ServiceToken_HandleCastResult` (`140520c10`).
- `ClientSpellCastWithServiceToken_WritePayload` writes an 18-bit first field
  and a 32-bit second field from `uint *param_2`, proving world opcode `0x00C2`
  as `{ client spellcast id/context token, spell4 id }`
  (`selected_decompiled.c:151-205`).
- `ServiceToken_SendClientSpellCastWithServiceToken` obtains the shared cast
  context through `SpellCastContext_GetOrCreateByClientUniqueId`, packs
  `*(resolvedContext + 0x60)` with `**(undefined4 **)(param_2 + 0x70)`, and
  dispatches opcode `0x00C2` through `Network_SendOpcodePayloadHelper`
  (`selected_decompiled.c:3208-3262`). That places the sender squarely in the
  same cast-context helper cluster as rapid transport, crafting, and the
  activate-unit-family senders, but with a distinct service-token spell path.
- `ServiceToken_HandleCastResult` gates that sender on
  `*(spellInfo + 0x108) & 0x20000000` before emitting the
  `"ServiceTokenCastResult"` client event, which tightens the earlier service-
  token cost/runtime evidence into a concrete cast-result branch rather than a
  purely UI-facing cost path (`selected_decompiled.c:5980-6010`).
- Export high-value patterns now include `SpellCastWithServiceToken` and
  `ServiceTokenCastResult` so future export-only runs keep this path selected
  even if surrounding labels drift.
- Follow-up on the remaining `+0x348` consumer side still does **not** justify a
  new label for `140899fd0`. The additional sweep keeps it inside the already-
  identified audio/output chain, but the current evidence is still too weak to
  distinguish effect-chain processing, validation, or other internal audio
  bookkeeping safely. The durable boundary therefore remains: `+0x348` is audio
  output/resampler state, and `140899fd0` stays intentionally unnamed.

Twenty-second spell-cast/audio caller follow-up implemented from this pass:

- Direct decompile now upgrades `1403998e0` from string-adjacent suspicion to a
  real shared spell-cast entry path. It is called from multiple higher-level
  systems, including the previously inspected activate-unit callers
  `14039eaf0` and `140559920`, and it validates the current cast object,
  resolves target state, emits `PrereqFailureMessage` or `SpellCastFailed` on
  failure, and dispatches successful paths to specialized cast helpers such as
  `FUN_140398cc0` (`InspectCodeAddress 1403998e0`; `TraceFunctionCallers 1403998e0`).
- The same pass directly proves a new adjacent sender. `14039b340` resolves the
  selected cast entry id, target entity, and target/fallback world position,
  then packs `CONCAT44(param_2, *(local_res20 + 0x60))` plus target entity id
  and position vector before calling `Network_SendOpcodePayloadHelper(param_1,
  0x9d, &local_128)`. That is enough for a safe generic label
  `SpellCast_SendClient0x009dVariant` even though the public source-side opcode
  name is still unmapped.
- Its nearby wrapper `14039b930` now decompiles as a bitmask expander over cast
  entries. It iterates selected bits, routes each selected id through
  `14039b340`, raises `SpellCastFailed` for failing entries, and marks selected
  entries dirty/completed through the per-entry `+0x148` field. That supports a
  matching generic bitmask-dispatch label without guessing at gameplay naming.
- Export high-value patterns now include `SpellCastFailed` and
  `PrereqFailureMessage` so future export-only runs keep this failure band
  selected even if nearby labels drift.
- Verification: forced WildStar64 export applied `142` labels with `0` missing
  labels and refreshed `selected_decompiled.c`; the three new labels now appear
  at `selected_decompiled.c:3178`, `:3605`, and `:3846`.
- The deeper audio follow-up still stops short of a new `140899fd0` label, but
  the boundary is tighter than before. Caller tracing now shows a direct wrapper
  family at `140899420`, `1408998c0`, `14089b0a0`, `14089b140`, and
  `14089b430`, all invoking `140899fd0` with the same `(RCX object, EDX mode,
  R8 descriptor-out)` shape. Inspected wrapper `140899420` rebuilds an internal
  object array from `*(param_1 + 0x80) + 0x110`, computes a selector via
  `FUN_140899eb0`, acquires a worker object through `FUN_140898df0`, and then
  invokes `140899fd0` before a virtual callback. That strengthens the function's
  role as a common audio worker, but still does not justify a stable semantic
  label beyond the existing audio/output boundary.

Twenty-third item-use/audio-wrapper follow-up implemented from this pass:

- `FUN_140398cc0` is no longer an opaque spell helper. Direct decompile now
  shows it sends opcode `0x0943`, which matches
  `GameMessageOpcode.ClientItemUse` in source. The success path builds a payload
  with the shared generated context token from `*(local_108 + 0x60)`, a 64-bit
  item-location value from the virtual accessor at `(*param_2 + 0x20)`, a
  32-bit target unit id from `*(local_108 + 0x158)`, a 64-bit target-location
  value carried in `local_138`, and the fallback world-position vector from
  `param_1 + 0x6d10/+0x6d18` before calling
  `Network_SendOpcodePayloadHelper(param_1, 0x943, &local_130)`. That is strong
  enough for the stable label `ItemUse_SendClientItemUse` and lines up with the
  existing `ClientItemUse` model in source.
- This also sharpens the shared dispatcher picture around
  `SpellCast_ValidateAndDispatch` (`1403998e0`): successful cast attempts now
  demonstrably branch into at least three specialized send paths inside the same
  helper cluster, including `ItemUse_SendClientItemUse` (`0x0943`),
  `SpellCast_SendClient0x009dVariant` (`0x009D`), and the already-mapped
  activate-unit family (`0x0096` / `0x0097` / `0x0098`).
- The audio wrapper family also tightened again. `1408998c0` reuses the same
  `FUN_140899eb0 -> FUN_140898df0 -> 140899fd0 -> vtable[0x48]` worker path as
  `140899420`, but sits behind flag-gated pending work and post-dispatch linked
  callbacks. `14089b430` uses the same selector/worker/callback chain when a
  key/state transition changes, then finalizes through `FUN_14089b630`. Together
  with `140899420`, those wrappers prove `140899fd0` is a shared mode-driven
  audio worker/callback path. That still narrows the boundary, but it remains
  intentionally unnamed because the exact worker semantics are not yet direct.

Twenty-fourth monster aggro/combat-state follow-up mapped from this pass:

- `UnitEvent_HandleEnteredCombat` (`14042e120`) dispatches
  `UnitEnteredCombat` with the resolved unit id and in-combat flag through the
  shared event helper, then conditionally dispatches `EnteredCombat` when the
  flag is non-zero and the unit matches either the local player record or the
  current target while the combat UI singleton is active
  (`selected_decompiled.c:5028`).
- Immediate neighbor `UnitEvent_DispatchTargetUnitChanged` (`14042e1b0`)
  dispatches `TargetUnitChanged` from the local player's current target field,
  placing the combat-entry callback in the same target-driven event/UI cluster
  (`selected_decompiled.c:5054`).
- `UnitState_MaybeDispatchUnitEvaded` (`1403db920`) resolves the subject unit
  and emits `UnitEvaded` only when the raw state field `param_2[1]` equals `4`.
  The emitted event carries the subject unit id, the actor id from
  `*(param_1 + 0x78) + 8`, the raw state value, and a display/context object
  from `FUN_14034bdd0()` (`selected_decompiled.c:4660`).
- Neighbor `UnitState_ApplyResolvedState` (`1403db870`) shares the same entity
  lookup and raw state field, special-cases state value `0` for the local
  subject/UI path, and then routes the resolved unit plus `param_2[1]` through
  `FUN_14045e740(...)`. That tightens the consumer-side mapping: the evade path
  sits inside a broader raw unit-state apply branch rather than a standalone
  dedicated evade packet.
- `FUN_14045e740(...)` now has a second confirmed caller family beyond the raw
  state consumer. `FUN_140456960` also reaches it directly, carries the string
  xref `UnitCreated`, and is itself reached from both `FUN_1403d9760` and
  `FUN_14047f770`. That makes the shared state helper part of a broader unit
  lifecycle/state replay cluster, not only the live raw-state consumer path.
- Tracing farther up one of those branches recovers a queued dispatcher shape:
  `FUN_1405cd160` and `FUN_1405cd070` each iterate linked-list style record
  storage and forward entries into `FUN_1405cd200`, which jump-dispatches into
  `FUN_1405cef50` and `FUN_1405caf20` before reaching `FUN_1403d9760` and then
  the `UnitCreated`-linked `FUN_140456960` path. This is stronger evidence for
  batched lifecycle/state replay than for a direct live evade packet producer.
- The queue boundary is tighter after another pass. `FUN_1405cd160` is not only
  a caller of `FUN_1405cd200`; its address is installed as a callback by both
  `FUN_1405cc090` and `FUN_1405cdaf0`, while `FUN_1405cd070` is directly
  invoked by `FUN_1405cca00` and `FUN_1405ccf20` as they flush pending records.
  One level higher, `FUN_1403e85d0` directly calls `FUN_1405cc090`. That pushes
  the current best candidate path to
  `FUN_1403e85d0 -> FUN_1405cc090 -> FUN_1405cca00/FUN_1405cd160 -> FUN_1405cd070/FUN_1405cd200`
  before the already-mapped `FUN_1405cef50/FUN_1405caf20 -> FUN_1403d9760 -> FUN_140456960 -> FUN_14045e740(...)`
  replay/apply chain.
- The newest caller traces still stop short of a parser boundary. `FUN_1405cca00`
  only traces back to `FUN_1405cc090`, and `FUN_1405cc090` only traces back to
  `FUN_1403e85d0`. Combined with the callback-install and pending-record flush
  behavior above, that makes this branch read more like an entity
  lifecycle/state replay loop than a direct live network decode entry.
- Direct decompile of `FUN_1403e85d0` strengthens that classification. It is a
  large virtual update method on a single owning object rather than a tiny
  dispatcher leaf: it snapshots current time and transform state from
  `param_1[0xeb1]`, pushes queued replay through `FUN_1405cc090(param_1[0xe33])`,
  iterates linked lists at `+0xde6`, `+0xdae`, and `+0xc68`, and then runs a
  broad sequence of world/entity maintenance helpers before returning. That is
  stronger evidence for a world/scene lifecycle update owner than for a narrow
  visibility callback or a direct packet parser.
- Dumping all current DATA refs to `FUN_1403e85d0` cleanly splits signal from
  noise. The refs at `140bcde20` and `140e034a4` are unwind metadata again, but
  `140b668b0` is a real function-pointer table entry bracketed by neighboring
  methods `FUN_1403e8000`, `FUN_1403e9aa0`, `FUN_1403ec2c0`, and
  `FUN_1403ec440`. That makes `FUN_1403e85d0` part of a virtualized lifecycle
  object, not a recovered message table or raw-state dispatcher.
- Inspecting the preceding table entry `FUN_1403e8000` tightens the owner shape
  further. It advances staged readiness bits at `+0x7b48`, validates dependent
  subsystems, seeds initial state on the object at `+0x6490`, drains another
  pending list at `+0x6d70`, and emits a one-shot
  `Network_SendOpcodePayloadHelper(param_1, 0x00F2, ...)` before the broader
  update loop is considered ready. The best current classification is therefore
  a world/scene lifecycle object with queued state replay, not the live raw
  state `4` producer.
- The two central replay helpers are now safe to label generically:
  `QueuedStateReplay_FlushPendingRecords` (`1405cd070`) directly flushes linked-
  list backed replay records into `QueuedStateReplay_DispatchRecord`
  (`1405cd200`), which then jump-dispatches a single queued replay entry into
  specialized worker helpers inside the `UnitCreated`-linked replay cluster.
- Re-checking the only caller-trace DATA target with `DumpNearbyData.java`
  confirms `140e02ca0` sits inside `_IMAGE_RUNTIME_FUNCTION_ENTRY[46407]`, so
  the refs surfaced for `1403db870` / `1403db920` are PE unwind metadata, not a
  recovered dispatcher table. The producer side therefore remains unresolved:
  the real state multiplexer still is not visible through direct caller tracing.
- Current NexusForever server behavior already matches the combat-entry side of
  this mapping: `UnitEntity.InCombat` emits `ServerUnitEnteredCombat`, retargets
  go through `ServerEntityTargetUnit`, creature-on-player aggro hints go through
  `ServerEntityAggroSwitch`, and player threat HUD refresh stays scoped through
  `ThreatManager` when the player targets the creature.
- Verification: the same forced WildStar64 export applied these labels with
  `0` missing labels and refreshed the selected decompile anchors above.
- Conservative runtime follow-up implemented from this pass: the existing
  `CombatAI.Reset()` leash-return path now sends the already-modeled localized
  `Evade` floater (`LocalisedTextId = 0x5F95C`) to the previous player target
  before the creature clears target, fully heals, and returns home. This does
  not claim a newly proven leash threshold or raw state producer; it only makes
  the current reset path visible with a client-confirmed evade message.
- Additional conservative runtime follow-up implemented from this pass:
  `CombatAI` now uses `LeashRange` for its initial range check and refuses the
  first aggro handoff when the hostile unit is already outside the creature's
  home-position leash radius. This keeps the existing reset-to-home model from
  immediately accepting out-of-bounds pulls without inventing a new mid-combat
  raw state producer or broader chase threshold.

Twenty-fifth spell-helper/audio-selector follow-up implemented from this pass:

- `FUN_1403988d0` now decompiles cleanly enough for a durable generic label.
  It is a shared spell-cast target-preparation helper upstream of multiple send
  paths, not a sender itself: it resolves the primary subject from `param_2[7]`,
  copies spell and resolved unit ids back into the working record, drives
  repeated target validation/expansion through `FUN_140398800(...)`, and
  conditionally emits `Network_SendOpcodePayloadHelper(param_1, 399, zero)`
  when the local subject, current target, and global trade state align. Source
  mapping confirms `0x018F == ClientP2PTradingCancelTrade`, but that zero-byte
  trade-cancel packet is only a side effect inside a broader shared helper.
  This is strong enough for the stable label
  `SpellCast_ResolveTargetsAndValidate`.
- That decompile also sharpens the spell helper cluster around
  `SpellCast_ValidateAndDispatch` (`1403998e0`): specialized senders such as
  `ItemUse_SendClientItemUse`, `RapidTransport_SendClientRapidTransport`,
  `SpellCast_SendClient0x009dVariant`, and the mapped activate-unit family now
  sit downstream of a common target-resolution/validation stage before they use
  the shared generated context field later read at `+0x60`.
- The audio side now has a firmer stop condition for wrapper comparison.
  Direct decompile of `FUN_140899eb0` shows it snapshots the dword at `+8` from
  each `0x18`-byte entry between `[param_1 + 0x88, param_1 + 0x90)` into a
  temporary list, then forwards that list plus current state
  `*( *(param_1 + 0x80) + 0x18 )` into
  `FUN_140834990(*(param_1 + 0x80) + 0x110, state, list, count)`. Since the
  wrappers feed the result of `140899eb0` into `FUN_140898df0` and then
  `140899fd0`, the shared worker's second argument is a dynamically computed
  selector/index derived from current entry state, not a fixed per-wrapper mode
  constant. That makes further wrapper-only comparison low value and pushes the
  next audio follow-up inward to `140834990` or `140899fd0` itself.
- Additional server-side follow-up from this pass stayed investigative only:
  widening the `Evade` floater beyond the previous player target is still not
  justified, because both `ServerGenericFloaterLocalised` and
  `ServerGenericFloaterComplex` are modeled in source as player-local floaters
  that appear over the receiving player's unit and are only sent through
  session-scoped story helpers.
- Re-checking those story floater models in source keeps the server lane at
  `no-change` for this pass: the packet comments still explicitly state that the
  floater appears over the receiving player's unit, so there is still no
  evidence-based case for widening the `Evade` floater to all visible players.
- Additional server-side follow-up from this pass also ruled out a second AI
  threat fix: `UnitEntity.TakeDamage(...)` already calls
  `ThreatManager.UpdateThreat(attacker, (int)damageDescription.RawDamage)`
  before `OnHealthChange(...)`, so there is no separate "new attacker never
  enters threat while already in combat" gap to patch in `CombatAI`.
- A further server-side check ruled out the remaining visibility-loss suspicion:
  `UnitEntity.RemoveVisible(...)` already clears the current target when the
  removed entity matches `TargetGuid` and then calls
  `ThreatManager.RemoveHostile(entity.Guid)` before script
  `OnRemoveVisibleEntity(...)` runs, so there is no additional `CombatAI`
  target-pruning patch to make on top of the existing leash/reset follow-ups.
- Remaining blocker is unchanged: the client-facing `UnitEvaded` signal is now
  mapped to raw state value `4`, but the newly recovered lifecycle/queue chain
  only proves another route into the shared state-apply helper, not the live raw
  state `4` producer that reaches `UnitState_MaybeDispatchUnitEvaded`. The
  upstream server-side packet/state producer and any broader leash/reset
  thresholds therefore still are not proven tightly enough to synthesize a new
  raw state update or widen `CombatAI` behavior beyond the existing reset path
  without guessing.

Twenty-sixth Game.Spell Lua accessor follow-up implemented from this pass:

- The remaining nearby `Game.Spell` accessor cluster is now mapped far enough
  for durable server diagnostics. `Lua_GameSpell_GetPrerequisites`
  (`1405ec320`) reads `Spell4Prerequisites.Flags` and emits the client field
  names `bTargetAvoided`, `bTargetBlocked`, `bTargetGlancing`, `bTargetFierce`,
  `bNOTUSED`, `bCasterAvoided`, `bCasterBlocked`, `bCasterGlancing`,
  `bCasterFierce`, `bNOTUSED1`, `bCasterSpellSuccess`, and
  `bLastCasterSpellSuccess` for bits `0x0001` through `0x0800`
  (`exports/WildStar64.exe/selected_decompiled.c:9538`, `:9604`, `:9635`,
  `:9665`, `:9695`, `:9725`, `:9755`, `:9785`, `:9815`, `:9845`, `:9875`,
  `:9905`, `:9935`). Source now carries this as typed
  `SpellPrerequisiteFlags`, exposes it through `ISpellBaseInfo`, and prints the
  decoded names in `/spell inspect`.
- `Lua_GameSpell_GetChannelData` (`1405ed640`) maps directly to
  `Spell4.ChannelMaxTime`, `ChannelInitialDelay`, and `ChannelPulseTime`,
  returned as seconds under `fMaxTime`, `fInitialDelay`, and `fPulseTime`
  (`selected_decompiled.c:10302`, `:10351`, `:10382`, `:10412`). The command
  diagnostics now print the same channel tuple, keeping the raw millisecond
  fields visible beside the client-style seconds.
- `Lua_GameSpell_GetProxyChannelData` (`1405eda10`) loops proxy effects, checks
  client effect type `0x1A` (`SpellEffectType.Proxy` in source), resolves the
  proxied `Spell4` child, and emits the same channel fields for the child spell
  (`selected_decompiled.c:10458`, `:10529`, `:10559`, `:10589`). The existing
  proxy-effect interpreter already models the child `Spell4Id`, so
  `/spell inspect` now reports any proxy child channel data without inventing a
  new runtime behavior.
- `Lua_GameSpell_GetAbilityCharges` (`1405edff0`) exposes the runtime charge
  table shape with `nChargesRemaining`, `nChargesMax`, `fRechargeTime`, and
  `fRechargePercentRemaining`; the two float fields are written directly in the
  selected decompile and the integer names are present in the export strings
  (`selected_decompiled.c:10686`, `:10770`, `:10814`;
  `exports/WildStar64.exe/strings.csv:37170`, `:37171`, `:37174`). Server
  `ICharacterSpell` now exposes recharge time remaining and percent remaining,
  and `/spell inspect` reports runtime charges when the invoker or target owns
  the inspected base spell. This pass also fixed the saved-spell constructor and
  tier setter ordering so charge data resolves against the actual tier.
- `Lua_GameSpell_GetAOETargetInfo` (`1405ecf20`) confirms the safe part of the
  AOE table: `fMinimumRange`, `fMaximumRange`, and `fAngle` correspond to
  `Spell4AoeTargetConstraints.MinRange`, `MaxRange`, and `Angle`
  (`selected_decompiled.c:10024`, `:10100`, `:10131`, `:10161`). The adjacent
  client booleans `bCanAffectDead`, `bClusterOnly`, and `bMustBeInCombat` are
  visible (`:10199`, `:10229`, `:10259`), but their server-source table fields
  are still not proven. Diagnostics therefore mark those booleans unresolved
  while continuing to enforce the already-modeled range, angle, target-count,
  and `AoeSelectionType` behavior. Runtime ordering now uses the typed
  `AoeSelectionType.LowestAbsoluteHealth` and `MissingMostHealth` enum names
  instead of raw selection values.
- Verification: `dotnet build Source\\NexusForever.sln -v minimal --nologo`
  succeeds after these updates. Remaining warnings are the existing package
  advisory/version warnings plus the existing unused `Spline.formation` field.

Twenty-seventh spell-cast sender-cluster follow-up implemented from this pass:

- Existing decompile artifacts were already enough to recover three more
  durable senders in the same helper band without another clean Ghidra import.
  `FUN_1403991b0` scans the guild-boss token table for an entry with type `3`,
  runs `FUN_1403986f0(...)` and `SpellCast_ResolveTargetsAndValidate`, then
  packs `{ GuildIdentity = *puVar1, Item2Id = *(DAT_140c635f0 + 0x1680),
  ContextToken = *(local_res20 + 0x60) }` before
  `Network_SendOpcodePayloadHelper(param_1, 0x94f, &local_148)`
  (`logs/inspect_1403991b0.txt:48-105`). That is strong enough for the stable
  label `GuildBossToken_SendClientCastGuildBossToken` and matches source model
  `ClientCastGuildBossToken`.
- `FUN_140399630` is now stable as `Crafting_SendClientCraftItemAutoCraft`.
  Caller tracing already shows `MOV EDX,0x852` immediately before
  `Network_SendOpcodePayloadHelper` (`logs/trace_callers_1403f4900.txt:565`),
  and its surrounding decompile resolves the shared context token through
  `SpellCast_ResolveTargetsAndValidate`, writes that token into `*param_3`,
  derives the crafting station unit id through
  `FUN_140245c00(param_3[2]) -> FUN_1403a0d20(...)`, and then sends the
  existing crafting payload buffer (`logs/inspect_140399630.txt:48-85`). That
  aligns with source `ClientCraftingCraftItemAutoCraft`.
- `FUN_140399780` is a paired crafting sender over the same setup path. Caller
  tracing shows two bounded dispatches into `Network_SendOpcodePayloadHelper`:
  `MOV EDX,0x84f` when `param_4` is non-null and `R8 = param_4`, and
  `MOV EDX,0x850` when `param_4` is null and `R8 = param_3`
  (`logs/trace_callers_1403f4900.txt:575-595`). Since both branches share the
  same context-token and station-id preparation (`logs/inspect_140399780.txt:48-84`),
  the safe durable label is `Crafting_SendClientComplexOrSimpleCraft`, covering
  source `ClientCraftingComplexCraft` and `ClientCraftingSimpleCraft` without
  overnaming the selector.
- The spell sender inventory immediately downstream of
  `SpellCast_ResolveTargetsAndValidate` is now wider: mapped opcodes in this
  band include `0x0141`, `0x00C2`, `0x084F`, `0x0850`, `0x0852`, `0x0943`,
  `0x094F`, `0x009D`, `0x0096`, `0x0097`, and `0x0098`. The remaining
  high-value unresolved sibling in the same band is `14039a040`, which still
  needs direct opcode proof before a durable label lands.

Twenty-eighth flight-path purchase packet follow-up implemented from this pass:

- `GameLib.PurchaseFlightPath` now has a complete client packet proof chain.
  The callback table still resolves to `Lua_GameLib_PurchaseFlightPath`
  (`14065e6c0`), which validates the Lua `Game.Unit` and destination/path input
  before forwarding to `FlightPath_SendPurchaseRequest` (`1404acfa0`). The send
  helper builds an ordered list of `TaxiRoute` ids and dispatches
  `Network_SendOpcodePayloadHelper(DAT_140c65898, 0xff, &local_88)`
  (`exports/WildStar64.exe/selected_decompiled.c:5773`, `:5829`, `:5868`).
- Native opcode registration now confirms the paired serializer: opcode
  `0x00FF` is registered with object size `0x10`, write function
  `140089490`, and advance stub `140089470`
  (`logs/WildStar64.FindImmediateInstructions.ghidra.log:336`,
  `:338`, `:340`, `:343`, `:344`). `140089470` advances the backing payload by
  count-dependent bits/bytes but is not a Ghidra function, so it was left
  unlabeled.
- `140089490` is now durably labeled
  `ClientFlightPathPurchase_WritePayload`. Its body writes a 32-bit route count
  and then copies `routeCount * 4` bytes from the route-id array pointer, so
  the wire payload is `uint routeCount` followed by that many raw 32-bit
  `TaxiRoute` ids (`exports/WildStar64.exe/selected_decompiled.c:151`,
  `:158`, `:185`; `exports/WildStar64.exe/functions.csv:1389`).
- Source now maps `GameMessageOpcode.ClientFlightPathPurchase = 0x00FF`, adds
  `ClientFlightPathPurchase` as a guarded packet model, and adds
  `ClientFlightPathPurchaseHandler` under the player entity handlers. The
  handler resolves each route id through `TaxiRoute`, rejects empty/missing or
  non-contiguous route chains, verifies the total credit price, and reports
  `VendorNotEnoughCash` or `EmbarkNoSplineForTaxi` as appropriate.
- The implementation intentionally does not deduct credits or start travel yet.
  The proven client work covers the purchase request payload and table-backed
  route chain, but the server-side taxi embark/spline movement response remains
  unmapped; until that path is recovered, the handler is a safe parser and
  validator with diagnostics rather than a speculative movement system.
- Verification: Ghidra export applied the new label
  (`ApplyNexusForeverLabels.java` reported `applied=149, created=0, skipped=0,
  missing=0`), and `dotnet build Source\\NexusForever.sln -v minimal --nologo`
  succeeds. Remaining warnings are the existing package advisory/version
  warnings plus the existing unused `Spline.formation` field.

Twenty-ninth spell-cast/audio selection follow-up implemented from this pass:

- The last remaining high-value unresolved sibling in the nearby spell sender
  band is now closed from existing artifacts. Caller tracing for `14039a040`
  shows a bounded `MOV EDX,0x9a` immediately before
  `Network_SendOpcodePayloadHelper` (`logs/trace_callers_1403f4900.txt:597-607`),
  and current source still maps `0x009A` to `ClientCastSpell` and `0x009B` to
  `ClientCastSpellPosition`
  (`Source/NexusForever.Network/Message/GameMessageOpcode.cs:16-18`) with the
  paired models/handlers present in source. The live durable label therefore
  stays conservative but broad as
  `SpellCast_SendClientCastSpellOrPosition`; this pass directly reconfirms the
  `0x009A` half from caller tracing and keeps the position-target half aligned
  with the current source-backed repo state.
- The audio selector chain is now directly decompiled past the wrapper layer.
  `FUN_140834990` is not a plain list lookup: it verifies the incoming count,
  resolves a candidate entry either from the embedded default path or the
  state/list-driven helper path, applies a per-entry probability gate using the
  table header and candidate fields, and returns the selected entry id from
  `*(entry + 4)` or zero on failure (`InspectCodeAddress 140834990`). That is
  strong enough for the generic label `AudioSelector_ResolveEntryId`.
- `FUN_140898df0` now decompiles as the bridge between that resolved id and the
  larger selection processor. When a non-zero resolved selection is already
  active it returns the existing runtime object through `FUN_140898ed0()`;
  otherwise it allocates a new `0xA0` runtime object, initializes it against
  the current audio context, validates it through `FUN_14088c3e0(...)` and
  `FUN_14088fb00(...)`, and returns the initialized object on success. That is
  sufficient for the generic label `AudioRuntime_AcquireOrCreate`.
- Direct decompile finally moves `140899fd0` past the earlier “common audio
  worker” boundary. The function takes the resolved entry id from the selector
  chain, compares it against the current active runtime selection, builds
  primary and optional secondary runtime objects, computes their timing and
  transition windows, dispatches them through `FUN_14089ae40(...)`, updates the
  active linked-list selection state, and clears or reschedules runtime objects
  as needed (`InspectCodeAddress 140899fd0`). That is still intentionally
  generic, but now strong enough for the durable label
  `AudioSelection_ScheduleAndDispatch` without guessing at a higher-level music,
  ambience, or effect semantic.
- This changes the practical audio follow-up priority. `140899fd0` is no longer
  the unresolved naming bottleneck; the next meaningful inward targets are the
  adjacent helpers it delegates to, especially `14089ae40`, `140899180`, and
  the selection sub-helpers under `140834990`, if tighter runtime semantics are
  still needed.

Thirtieth generic spellcast request follow-up implemented from this pass:

- The remaining generic spellcast sender sibling at `14039a040` is now mapped
  as `SpellCast_SendClientCastSpellOrPosition`. The function indexes the action
  slot through the local action bar table, resolves the active spell/cast
  context through the shared target-preparation path, calls
  `SpellCast_ResolveTargetsAndValidate`, and then sends either opcode `0x009A`
  or opcode `0x009B` through `Network_SendOpcodePayloadHelper`.
- The `0x009A` writer at `140093360` is now labeled
  `ClientCastSpell_WritePayload`. Its registration uses object size `0x10`, and
  the writer serialises a 32-bit cast context token, a 16-bit action slot, the
  resolved primary target entity id, and a trailing one-bit boolean. Source now
  treats the old `ClientUniqueId` and `CasterId` names as compatibility aliases
  for the proven `ContextToken` and `PrimaryTargetId` fields.
- The position-target branch is now labeled through the paired writer
  `ClientCastSpellPosition_WritePayload` at `140093950`. Its registration uses
  object size `0x14`, and the writer serialises the same 32-bit context token
  plus 16-bit action slot followed by three raw 32-bit position components. The
  source mapping adds `GameMessageOpcode.ClientCastSpellPosition = 0x009B`, a
  readable packet model, and a spell handler that passes the decoded position
  into `SpellParameters.Position`.
- The normal `ClientCastSpell` handler now consumes the proven client-resolved
  primary target id instead of re-resolving only from current server target
  state. The trailing boolean is exposed by the packet model but is not used to
  suppress normal casts here; the native proof establishes its encoding, while
  its exact UI/cast-mode meaning still needs a narrower caller-state pass before
  it should alter server behaviour.
- Remaining bounded uncertainty: `14039a040` still contains client-side cast
  queue/failure aggregation and current-position fallback details that are not
  mirrored server-side. The packet boundary and field order are now implemented,
  but the deeper client runtime state transitions remain a separate target.
- Verification: the shared-layout Ghidra export applied the updated labels
  (`ApplyNexusForeverLabels.java` reported `applied=157, created=0, skipped=0,
  missing=0`) and refreshed `selected_decompiled.c` with 240 functions after
  the locked per-target log was avoided.

Thirty-first service-token spellcast follow-up implemented from this pass:

- The service-token spellcast group is now carried through to source behaviour.
  The packet writer `ClientSpellCastWithServiceToken_WritePayload` at
  `140089570` serialises opcode `0x00C2` as an 18-bit generated cast-context
  token followed by a 32-bit `Spell4` id
  (`exports/WildStar64.exe/selected_decompiled.c:197`, `:201`, `:204`).
- The sender `ServiceToken_SendClientSpellCastWithServiceToken` at `1403994f0`
  performs a service-token balance check before dispatch. It returns result
  `0x014B` on insufficient funds, otherwise packs the resolved context token
  with the `Spell4` id and calls `Network_SendOpcodePayloadHelper(..., 0x00C2,
  ...)` (`selected_decompiled.c:3759`, `:3799`, `:3803`).
- The caller `ServiceToken_HandleCastResult` at `140520c10` now has a tighter
  durable label comment: it gates the path on `Spell4.PropertyFlags &
  0x20000000`, dispatches the `0x00C2` request only for service-token-cost
  spells, and reports `0x014B` through the local `ServiceTokenCastResult` event
  otherwise (`selected_decompiled.c:7692`, `:7697`, `:7706`, `:7720`).
- Source now adds `ClientSpellCastWithServiceTokenHandler` for opcode `0x00C2`.
  It resolves the requested `Spell4` id through `Spell4`/`GlobalSpellManager`,
  requires the mapped `HasServiceTokenCost` flag and matching
  `Spell4ServiceTokenCost` row, and casts the spell with
  `UseServiceTokenCost = true`.
- `ISpellParameters`/`SpellParameters` now carry `UseServiceTokenCost`, and the
  spell pipeline checks account service-token affordability during cast
  validation. Tokens are consumed only after prerequisite, cooldown, charge, and
  target checks pass; a final affordability recheck sends
  `CastResult.ServiceTokensInsufficentFunds` if the balance changed before
  consumption.
- Remaining bounded uncertainty: the client-side result event also carries a
  local context token, but the existing `ServerSpellCastResult.Unknown0` meaning
  is still unmapped, so the handler follows existing server cast-result practice
  and does not echo that token speculatively.
- Verification: the shared-layout Ghidra export reapplied all labels
  (`applied=157, created=0, skipped=0, missing=0`) and refreshed the service-
  token functions in `selected_decompiled.c`.

Thirty-second path-explorer/audio runtime follow-up implemented from this pass:

- Spell-side widening now extends past the old sender band into `14039bf20`.
  Existing caller tracing shows the function routes through
  `SpellCast_ResolveTargetsAndValidate`
  (`logs/trace_callers_1403988d0.txt:75-82`) and then emits a bounded
  `MOV EDX,0x99` immediately before `Network_SendOpcodePayloadHelper`
  (`logs/trace_callers_1403f4900.txt:443-449`). Cached decompile fragments for
  `14039bf20` already expose the stable local name
  `PathExplorer_SendClientCastSearching` and show the packet payload is the
  shared generated context token plus the search-radius band and scavenger clue
  id (`exports/WildStar64.exe/selected_decompiled_cache/.../14039bf20.fragment.c`).
  Current source still maps `0x0099` to `ClientCastPathExplorerSearching`
  (`Source/NexusForever.Network/Message/GameMessageOpcode.cs:18`) and exposes
  the paired readable model in
  `Source/NexusForever.Network.World/Message/Model/PlayerPath/ClientPathExplorerCastSearching.cs`.
  That is enough for the durable label `PathExplorer_SendClientCastSearching`
  without another spell-side inspect.
- Direct decompile of `14089ae40` moves the audio chain one layer deeper than
  `AudioSelection_ScheduleAndDispatch`. The helper consumes the timing and
  transition state prepared by `140899fd0`, updates runtime flags and
  timestamps on the active selection object, pushes state into the runtime
  through `FUN_140890430(...)` and the caller vtable callback until the runtime
  reports ready, and then links the prepared node into the active selection
  list (`InspectCodeAddress 14089ae40`). That is strong enough for the generic
  label `AudioSelection_ConfigureRuntimeAndLinkNode`.
- This splits the previously broad post-selection work into a cleaner two-stage
  boundary: `140899fd0` remains the scheduling/orchestration layer, while
  `14089ae40` now covers runtime configuration and active-list linking. The
  next remaining inward ambiguity is centered more cleanly on `140899180` and
  the `140890*` helpers it drives.
- Verification: the direct inspect completed against the per-target Ghidra
  project without lock failure, and the export pass again reported
  `applied=157, created=0, skipped=0, missing=0`.

## Practical Next Steps

1. Keep extending `Decomp\Analysis\function_labels.csv` as functions are
   confirmed. Re-run `.\Decomp\Analysis\run_ghidra_analysis.ps1 -ExportOnly`
   so the exported C becomes progressively easier to read.
2. Cross-check constants and field reads in the selected C against
   `GameMessageOpcode.cs`, the STS models, and generated `GameTable` models.
3. Add narrower high-value patterns to `ExportNexusForeverAnalysis.java` when a
   new subsystem becomes the focus.
