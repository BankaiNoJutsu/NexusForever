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
- Export-only label recovery now makes that helper cluster reproducible across
  future runs: `SpellService_ResolveSpellWrapper` at `1403acd90` resolves the
  shared spell wrapper/service object, `Game_Spell_IsSelfSpellDelegate` at
  `1403b4ec0` is the unresolved 56-byte bool helper behind the deeper
  `IsSelfSpell` branches, and `SpellService_ResolveTargetFlags` at `1407a0fd0`
  is the wrapper-flag accessor used by `IsFreeformTarget` after the type-`7`
  service lookup branch.
- Direct recheck of the labeled `SpellService_ResolveSpellWrapper` body now
  tightens that shared wrapper path further. Before falling back to
  `SpellService_ResolveTargetFlags(param_1)`, it checks whether `param_3`
  matches either `*(DAT_140c65898 + 0x78)` or `*(DAT_140c65898 + 0x6490)` and,
  on that narrow global-context match, returns `FUN_1405a5b90()`. That is
  documentation-grade evidence that the unresolved type-`0` fallback and the
  type-`7` service-tree re-entry share a common wrapper gate, even though the
  final `Game_Spell_IsSelfSpellDelegate` semantics remain undecoded.
- The wrapper layout evidence also sharpened. Type-`0` fallback branches still
  materialize bit pattern `0x85` at wrapper offset `+0x7c`, while the type-`7`
  tree path resolves a secondary wrapper from `DAT_140c65b70 + 0x788` and then
  reads targeting flags from `+0x108`. That is now the strongest current
  evidence that `+0x7c` is a target-shape selector and `+0x108` is the
  resolved targeting-flags word rather than an unrelated bookkeeping field.
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
  mechanics `1/2/44`), type `6` (`Spell4=11820`, mechanic `17`), and type `7`
  (`Spell4=339` and `Spell4=26813`, known mechanics `18/25/26/33/52/58/63/69`).
- A later cross-schema recheck corrected the concrete type-`6` witness. The
  earlier `Spell4=5157` food-buff candidate is actually `spell4TargetMechanicId
  = 1` / `targetType = 0` in both `nexus_spell_re` and `wildstar_client`.
  The only current mechanic-`17` / type-`6` spell is
  `Spell4=11820` (`[TEST] Recharge Item Batteries Full - Tier 1`), backed by
  `ItemSpecial=3777` with `spell4IdOnActivate=11820` and zero current
  `item2.itemSpecialId00` owners. Type `6` therefore remains real, but even
  narrower and more awkward to cast than the earlier note implied.

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

CC breakout client evidence sweep used for the next runtime pass:

- Export-string scans in `Houston64.exe` and `WildStar64.exe` surface explicit stun-breakout anchors: `StunBreakoutLeft`, `StunBreakoutRight`, `StunBreakoutUp`, `StunBreakoutDown`, `CodeEnumCCStateStunVictimGameplay`, `StunBreakoutGameplay`, `ActivateCCStateStun`, `RemoveCCStateStun`, `UpdateCCStateStun`, and `GetCCStateStunTimeRemaining`.
- Existing packet models already line up with that surface: `ServerCCStateStunDirection` names the required input direction, `ClientCCStateStunUpdate` reports pressed/held direction state, and `ClientCCStateKnockdownBreak` is explicitly documented as the dash breakout packet for `Knockdown`.
- Server-safe implementation boundary for now: wire player `ClientCCStateKnockdownBreak` through tracked `CCStateBreak` removal and conservative cast/movement restrictions; leave stun-direction gameplay, breakout cadence, and DR semantics open pending sniff validation.

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
- Source mapping now resolves that opcode: `0x00F2 == ClientEnteredWorld` in
  `GameMessageOpcode`. That strengthens the same conclusion because the
  neighboring staged method is signaling world-entry readiness, not dispatching
  a combat or raw unit-state update.
- Exported decompile references now make the owner pointer itself more concrete.
  `selected_decompiled.c` uses `DAT_140c65898` repeatedly as the first argument
  into shared scene/state helpers and dereferences stable fields such as
  `+0x78`, `+0x6490`, `+0x6718`, and `+0x7340`. Together with the vtable block
  around `140b668a8/140b668b0`, that is strong evidence that `DAT_140c65898`
  is the global pointer to the same scene lifecycle owner rather than an
  unrelated scratch global. This still does not reach the live raw state `4`
  producer; it only tightens ownership of the replay/apply path.
- Exported fragments now also show `DAT_140c65898` being treated as the same
  typed owner object as the scene lifecycle methods themselves, not merely as a
  loose container pointer. The decompiler reads fields such as
  `DAT_140c65898[0xf]`, `DAT_140c65898[0xc92]`, and tree state at
  `DAT_140c65898 + 0x7270/+0x7278`, which matches the same object shape seen in
  `SceneLifecycle_UpdateAndReplayQueuedStates`. That is strong enough to treat
  `DAT_140c65898` as the live global instance of the lifecycle owner even
  though its constructor or assignment site is still not recovered.
- A fresh inspect of `FUN_140215240` rules out one more false lead. It is a
  `MatchingGameMap` lookup/dispatch helper keyed by the `MatchingGameMap` DB
  string path and fallback table callbacks, so it does not explain the scene
  lifecycle owner or the live raw state `4` evade producer.
- The `MatchingGameMap` detour is now strong enough for durable generic labels:
  `FUN_140214fe0` carries the `MatchingGameMap` and `DB\\MatchingGameMap.tbl`
  string xrefs and is best treated as `ClientDB_RegisterMatchingGameMap`, while
  `FUN_140215240` reads as `MatchingGameMap_LookupById` because it resolves a
  table record through the registered callback or `DAT_140c641f0` fallback
  dispatcher before `ActionSet_CheckPvPRestriction` inspects the returned flag
  byte. That closes this detour without changing the lifecycle-owner or evade-
  producer conclusions.
- Cached export evidence also makes `FUN_1400ea3e0` safe to label generically as
  `ClientEvent_DispatchNamedEvent`. Across the selected decompile it dispatches
  string-named events such as `UnitEvaded`, `UnitEnteredCombat`,
  `TargetUnitChanged`, `GenericFloater`, `InvokeTaxiWindow`, and many other UI
  or gameplay hooks through the same sink pointer (`DAT_140c65898 + 0x7340`).
  That clarifies one more boundary in the evade path: `UnitState_MaybeDispatch`
  reaches a generic client event emitter, not a dedicated raw-state parser, so
  the live raw state `4` producer still remains upstream of the resolved-state
  branch.
- `FUN_1403db7f0` is now decompiled strongly enough for a generic cache-helper
  label: it resolves the target unit for `*param_2`, forwards `(state, extra)`
  through `FUN_14045bdc0(...)`, and when the cached raw value changes rewrites
  `unit + 0x108` before recomputing the derived classification at `unit +
  0x10c` through `FUN_14045a950(...)`. That pushes the live evade path one
  step farther: raw state `4` definitely enters the same live cache/update
  cluster, but a one-off caller trace of `FUN_1403db7f0` still only lands on
  unwind metadata at `140e02c94`, so the true dispatcher is still unrecovered.
- The neighboring `FUN_1403db9a0` block is now a closed false lead. Its only
  recovered string xrefs are `InvokeVendorWindow` and `VendorItemsUpdated`, so
  adjacency to the unit-state helpers does not make it part of the evade path.
- Two more upstream probes close out the simple adjacency-search branch around
  the unit-state helpers. `FUN_1403d8810` lazily allocates and copies a `0x1b0`
  record payload after `FUN_1403d8790(...)`, while `FUN_1403d8200` is a keyed
  tree find-or-insert helper around `FUN_1403d8140(...)` and `FUN_1403d8470(...)`.
  Neither function dispatches live unit-state updates, so the next productive
  evade hunt needs to pivot away from neighboring addresses and toward the code
  that creates or routes those `0x1b0` records into the live cache/apply
  cluster.
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
- Additional conservative runtime follow-up implemented from this pass:
  `CombatAI.SelectTarget()` now filters the threat list against the creature's
  home leash before promoting a hostile target and prunes invalid/out-of-leash
  hostiles behind a small reentrancy guard. That closes the remaining gap where
  `UnitEntity.TakeDamage(...)` could add threat first, `OnThreatAddTarget()`
  could immediately select a target, and only then would `CombatAI.AggroEntity()`
  reject the same attacker as already outside the creature's home leash.
- Additional conservative runtime follow-up implemented from this pass:
  `CombatAI` now consumes the existing `OnExitRange()` callback from the leash-
  backed range check and removes hostiles that cross out of the creature's home
  leash. Because `GridEntity` updates range checks on relocate and already fires
  `OnExitRange()` before any explicit vision loss is required, this is the
  smallest rooted fix for the remaining case where a target kites out of leash
  after valid initial aggro/selection but would otherwise stay on the threat
  list until some later visibility or threat event happened.
- Re-auditing the server lane after that `OnExitRange()` change does not justify
  another runtime patch yet. A separate `DoChase()` leash guard would only
  duplicate the existing range-exit hostile removal, and widening the `Evade`
  floater still fails the source-modeled player-local story floater semantics.

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
- Current SQL imports preserve an important data-boundary mismatch: as of
  2026-05-16, both `nexus_spell_re.spell4` and `wildstar_client.spell4` expose
  zero rows with `PropertyFlags & 0x20000000`, while
  `wildstar_client.Spell4ServiceTokenCost` still carries the 6 concrete service-
  token cost rows (`83146`, `83153`, `8568`, `38408`, `70864`, `70865`). Treat
  the flag gate as decompile-proven and the cost rows as the durable SQL-side
  witnesses; do not read the missing SQL flag bit as evidence that the client
  helper gate is wrong.
- The schema-qualified witness query now resolves that cost-row cohort to exact
  concrete spells: `8568` (`Teleporting to Eldan Stone - Recall Shard -
  Hearthstone - Global - Tier 1`, cost `10`), `38408` (`Teleport to house -
  Global - Tier 1`, cost `10`), `70864` (`Teleport - Exiles - Thayd - [10s
  cast] - SWC - Tier 1`, cost `10`), `70865` (`Teleport - Dominion - Illium -
  [10s cast] - SWC - Tier 1`, cost `10`), plus the two dev cooldown-removal
  rows `83146` (cost `2`) and `83153` (cost `5`). `GameFormula 1307`'s service-
  token rapid-transport spell `82956` is still absent from
  `wildstar_client.Spell4ServiceTokenCost`, so the durable SQL witness set for
  the current Area 3 transaction boundary remains recall, housing, capital
  teleport, and dev-test rows rather than rapid transport.
- Those six SQL-side witnesses also sort cleanly by effect family, which makes
  the next Area 3 transaction pass concrete: `8568`, `70864`, and `70865` are
  plain `Teleport` rows (`effectType = 65`), `38408` is a
  `HousingTeleport` plus delayed `Fluff` combination (`effectType = 22`, `17`),
  and `83146` / `83153` are compact `Fluff`-only target rows (`effectType = 17`,
  `targetFlags = 2`, `durationTime = 4000`). That is the right cohort for
  sorting `0x00C2` and `0x014B` behavior by recall, housing, capital-teleport,
  and dev-test spell family rather than treating the service-token boundary as
  one undifferentiated packet path.
- `ISpellParameters`/`SpellParameters` now carry `UseServiceTokenCost`, and the
  spell pipeline checks account service-token affordability during cast
  validation. Tokens are consumed only after prerequisite, cooldown, charge, and
  target checks pass; a final affordability recheck sends
  `CastResult.ServiceTokensInsufficentFunds` if the balance changed before
  consumption.
- The direct spell-entry surfaces that already decode native cast-context
  tokens now preserve them into runtime spell parameters and the structured
  spell evidence export instead of dropping them at the network-handler
  boundary. Current carried sources include `ClientCastSpell` (`0x009A`),
  `ClientCastSpellPosition` (`0x009B`), `ClientActivateUnitCast` (`0x0097`),
  `ClientSpellCastWithServiceToken` (`0x00C2`), `ClientRapidTransport`
  (`0x0141`), and spell-triggering `ClientItemUse` (`0x0943`). The server still
  does not validate or echo those tokens back into unknown packet fields, but
  runtime evidence artifacts can now correlate `ClientContextToken` plus
  `ClientRequestSource` against the generated server `CastingId`.
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

Thirty-third quest-interaction/audio ready-node follow-up implemented from this
pass:

- The next spell-side controller adjacent to the widened sender band is now
  directly decompiled. `14039c8a0` is not another cast sender; it is a
  quest-interaction side path reached from `SpellCast_ValidateAndDispatch` when
  the resolved cast record carries the non-zero branch at `+0x1b0`
  (`exports/WildStar64.exe/selected_decompiled_cache/.../1403998e0.fragment.c`).
  Direct decompile shows two bounded packet sends: `MOV EDX,0x0365` for a
  single-quest-id retry payload and `MOV EDX,0x035B` for the item-location plus
  quest-id accept payload (`logs/trace_callers_1403f4900.txt:163-180`,
  `InspectCodeAddress 14039c8a0`). Current source still maps those opcodes to
  `ClientQuestRetry` and `ClientQuestAccept`
  (`Source/NexusForever.Network/Message/GameMessageOpcode.cs:304,314`) with the
  paired models and handlers present in source. That is enough for the durable
  label `QuestInteraction_HandleAcceptOrRetry`.
- The inward audio pass now resolves the next small helper directly under
  `AudioSelection_ConfigureRuntimeAndLinkNode`. `140890430` does not drive the
  ready-state loop itself; it computes the accumulated timing offset for the
  runtime queue by summing queued node durations through `1408904a0(...)`, adds
  the current queue-base offset from `14088fda0(...)`, and then calls
  `14088d620(...)` to move all due nodes into the caller-provided list for the
  current scheduling timestamp (`InspectCodeAddress 140890430`,
  `InspectCodeAddress 1408904a0`, `InspectCodeAddress 14088d620`). That is
  strong enough for the generic label `AudioRuntime_ExtractReadyNodes`.
- This tightens both continuation fronts. On the spell side, the next remaining
  work near `14039bf20` is no longer “find the next sender” but to decide
  whether nearby smaller helpers are quest-interaction leaves or broader cast
  queue/state logic. On the audio side, the runtime-link step now has a named
  ready-node extraction subphase, so `140899180` remains the next larger inward
  ambiguity rather than `140890430`.
- Verification: the direct inspect passes for `14039c8a0`, `140890430`,
  `1408904a0`, and `14088d620` all completed successfully against the per-
  target Ghidra project, and the label-application count remained stable before
  the final export-only validation.

Thirty-fourth crafting request follow-up implemented from this pass:

- The native crafting request writer group is now mapped and labeled, not only
  the higher-level sender helpers. Opcode `0x0852` registers writer
  `ClientCraftingCraftItemAutoCraft_WritePayload` at `14007d4a0` with object
  size `0x10`; it serialises four 32-bit fields: generated context token,
  crafting station unit id, `TradeskillSchematic2` id, and schematic count
  (`exports/WildStar64.exe/selected_decompiled.c:71`, `:77`).
- Opcode `0x0851` registers writer `ClientCraftingCraftItem_WritePayload` at
  `1400a5a70` with object size `0x14`. Its body reuses the four-field
  auto-craft prefix and appends an 18-bit trailing catalyst/item id
  (`selected_decompiled.c:624`, `:631`, `:639`). This corrects the prior source
  model split: the extra 18-bit item field belongs to `ClientCraftingCraftItem`,
  not `ClientCraftingCraftItemAutoCraft`.
- Opcode `0x0850` registers writer `ClientCraftingSimpleCraft_WritePayload` at
  `1400a5b50` with object size `0x0C`, proving the simple craft payload as
  `{ ContextToken, CraftingStationUnitId, TradeskillSchematic2Id }`
  (`selected_decompiled.c:668`, `:674`). Opcode `0x084F` registers
  `ClientCraftingComplexCraft_WritePayload` at `1400a5d70` with object size
  `0x30`; it writes the simple-craft prefix, packed `CraftStats`, an 18-bit
  power-core item id, AP/SP split delta, and a 3-bit count plus charge-count
  array (`selected_decompiled.c:753`, `:759`, `:768`).
- The existing sender labels remain aligned: `Crafting_SendClientCraftItemAutoCraft`
  sends `0x0852`, and `Crafting_SendClientComplexOrSimpleCraft` sends `0x084F`
  when an extended craft payload is present or `0x0850` otherwise
  (`selected_decompiled.c:4109`, `:4172`). A separate client path can choose
  `0x0851` or `0x0852` from the same base payload, which is why both models are
  now included in the server receive boundary.
- Source now fixes the packet layouts for `ClientCraftingCraftItem` and
  `ClientCraftingCraftItemAutoCraft`: `0x0851` reads the 32-bit schematic count
  followed by the 18-bit catalyst/item id, while `0x0852` reads only the
  four-field prefix. The new crafting handlers validate the schematic and
  optional item ids against the `TradeskillSchematic2` and `Item2Entry` tables,
  log the request, and send a non-mutating `ServerCraftingFinish` failure.
- Remaining bounded uncertainty: NexusForever still has no complete crafting
  execution manager for material consumption, discovery rolls, schematic
  unlocks, output creation, or station constraints. The receive boundary is now
  mapped and safe, but real craft success remains blocked until that subsystem
  is designed from table/server evidence.
- Verification: the shared-layout export applied the updated label set
  (`applied=167, created=0, skipped=0, missing=0`) and refreshed
  `selected_decompiled.c` with 260 functions. `dotnet build
  Source\NexusForever.sln -v minimal --nologo` succeeds with the existing
  package warnings and `0 Error(s)`.

Thirty-fifth action-set request parity follow-up implemented from this pass:

- Existing client evidence around `Lua_ActionSetLib_RequestActionSetChanges`
  and `ActionSet_SendPendingActionSetChanges` is now carried through to source
  more tightly: opcode `0x00B1` is built against the current active spec, not
  an arbitrary target spec. NexusForever now rejects
  `ClientRequestActionSetChanges.ActionSetIndex` values that do not match the
  active action set with `InvalidSpecIndex` instead of mutating an inactive
  spec.
- The request handler now mirrors the proven client preflight that the current
  server can model safely: it returns `PlayerIsDead` when the player is dead
  and `InCombat` when the player is already in combat before mutating the LAS.
- Additional server-side validation now closes the remaining local exception
  paths in this packet flow. The handler rejects duplicate spell ids with
  `DuplicateSpell`, rejects tier updates for spells not present in the
  requested 12 slots with `LASChangeSpellFailed`, validates each requested tier
  against `SpellBaseInfo.GetSpellInfo(...)`, and returns
  `InsufficientAbilityPoints` when the requested LAS tier cost exceeds
  `ActionSet.MaxTierPoints`.
- AMP handling now stays additive like the mapped client request builder, but
  no longer trusts the packet blindly: incoming AMP ids are de-duplicated, each
  new id is validated against `EldanAugmentation`, and the handler returns
  `EldanAugmentationInvalidId` or `EldanAugmentationNotEnoughPower` instead of
  reaching `ArgumentException` or checked-overflow paths during
  `ActionSet.AddAmp`.
- Successful `ClientRequestActionSetChanges` handling now sends
  `ServerActionSetClearCache` before the rebuilt `ServerActionSet`, and
  ability points are refreshed whenever `TierPoints` actually changed rather
  than only when the packet included explicit `ActionTiers`. This fixes the
  stale ability-point UI case when swapping or removing already-tiered LAS
  spells changed tier cost without a separate tier-delta entry.
- Remaining bounded blocker: the client-side `RestrictedInPVP` and
  `UpdateSpellInProgress` branches are mapped through
  `ActionSet_CheckPvPRestriction` and `ActionSet_CheckUpdateSpellInProgress`,
  but the current server codebase still lacks a proven matching PvP-restriction
  state machine and a dedicated in-progress LAS/spell-update transaction state,
  so those result paths remain intentionally unimplemented rather than guessed.
- Verification: `dotnet build Source\NexusForever.sln --no-restore`
  succeeds.

Thirty-sixth stop-cast/audio linked-runtime follow-up implemented from this
pass:

- The next smaller spell-side helper adjacent to the quest-interaction branch
  is now directly decompiled as a stop-cast sender rather than another quest
  leaf. `14039cc30` emits bounded opcode `0x0801`
  (`logs/trace_callers_1403f4900.txt:653`) and direct decompile shows it packs
  the current stop-cast payload into a local record before calling
  `Network_SendOpcodePayloadHelper(param_1, 0x801, ...)`
  (`InspectCodeAddress 14039cc30`). Current source still maps `0x0801` to
  `ClientSpellStopCast`
  (`Source/NexusForever.Network/Message/GameMessageOpcode.cs:741`) with the
  paired packet model and spell handler already present in source. That is
  enough for the durable label `SpellCast_SendClientSpellStopCast`.
- The inward audio chain now resolves the next larger helper that remained
  below `AudioSelection_ConfigureRuntimeAndLinkNode`. Direct decompile of
  `140899180` shows a bounded recursion counter, node-resolution through
  `1408906b0(...)`, a cloned candidate runtime node via `140890370(...)`, and a
  linked-entry lookup through `140895d10(...)`; when a flagged linked entry is
  found, it increments the linked-node depth on the returned context, fetches
  the referenced child runtime through `140898590(...)`, and clears the cloned
  candidate node if that child runtime cannot be acquired
  (`InspectCodeAddress 140899180`, `140895d10`, `140898590`). That is strong
  enough for the conservative label `AudioRuntime_ResolveLinkedNode`.
- This tightens the current continuation boundary on both fronts. Spell-side
  work immediately after `QuestInteraction_HandleAcceptOrRetry` now splits into
  at least one true spell stop-cast sender (`14039cc30`) instead of a pure
  quest-only leaf cluster. Audio-side work below `14089ae40` now has named
  ready-node extraction and linked-node resolution phases, so the next deeper
  uncertainty moves away from `140899180` itself and toward the larger table
  and child-runtime helpers it delegates to.
- Verification: the shared-layout Ghidra project successfully handled the new
  direct inspections after the per-target project lock was avoided, and the
  durable label map remained internally consistent before the final export-only
  validation pass.

Thirty-seventh crafting/rune request follow-up implemented from this pass:

- The remaining client-to-server crafting and rune request writer group is now
  mapped and labeled. `ClientCraftingAdditive_WritePayload` at `1400a5f20`
  registers for opcode `0x084A` with object size `0x0C` and writes a 32-bit
  crafting station unit id followed by two 18-bit `Item2` ids for additive and
  catalyst (`selected_decompiled.c:1142`, `:1149`).
- `ClientCraftingAbandon_WritePayload` at `140001ba0` registers for opcode
  `0x084D` with object size `0x01` but is a no-op writer, confirming the
  current source model as a zero-field message (`selected_decompiled.c:7`,
  `:14`).
- The rune slot writers now match source packet boundaries:
  `ClientCraftingRuneSlotAdd_WritePayload` at `1400a53c0` writes a 64-bit item
  guid, a 1-bit non-fusion flag, and a 5-bit `RuneType`;
  `ClientCraftingRuneSlotClear_WritePayload` at `1400a5550` writes a 64-bit
  item guid, a 32-bit slot index, a 1-bit recover flag, and a 1-bit
  group-currency flag; `ClientCraftingRuneSlotReroll_WritePayload` at
  `1400a5780` writes a 64-bit item guid, a 32-bit slot index, and a 5-bit
  `RuneType` (`selected_decompiled.c:640`, `:708`, `:800`).
- `ClientCraftingRuneInstall_WritePayload` at `1400a5900` registers for opcode
  `0x085B` with object size `0x18`; it writes a 64-bit target item guid, a
  32-bit rune item count, then copies `count * 4` raw bytes from the
  pointer-backed rune array. Source now validates that the count fits the
  remaining packet bytes before allocating or reading the `Item2` id array
  (`selected_decompiled.c:868`, `:874`).
- The corresponding send helpers are also labeled:
  `Crafting_SendClientCraftingAbandon` (`14051ba60`),
  `RuneCrafting_SendClientRuneSlotReroll` (`14051dc00`),
  `RuneCrafting_SendClientRuneSlotClear` (`14051e580`),
  `RuneCrafting_SendClientRuneSlotAdd` (`14051ed80`),
  `Crafting_SendClientCraftingAdditive` (`14059b7c0`), and
  `RuneCrafting_SendClientRuneInstall` (`14059d250`). These labels are backed
  by opcode immediates and local field staging around the
  `Network_SendOpcodePayloadHelper` call sites (`selected_decompiled.c:9610`,
  `:9636`, `:9669`, `:9704`, `:9989`, `:10133`).
- Source now adds conservative world handlers for additive, abandon, rune slot
  add/clear/reroll, and rune install. They validate table-backed `Item2` ids,
  target inventory items, and `RuneType` values where applicable, log the
  request, and leave inventory/currency/rune state untouched.
- Remaining bounded uncertainty: the request boundary is mapped, but the actual
  rune mutation subsystem is still absent. Installing, adding, clearing,
  recovering, rerolling, charging currency, and returning precise
  `ServerTradeskillSigilResult` statuses remain blocked until the item rune data
  model and server-side result rules are mapped from table/runtime evidence.
- Verification: the per-target WildStar64 export applied the expanded label set
  (`applied=182, created=0, skipped=0, missing=0`) and rendered
  `selected_decompiled.c` with 280 functions. `dotnet build
  Source\NexusForever.sln -v minimal --nologo` succeeds with the existing
  package warnings and `0 Error(s)`.

Thirty-eighth stop-cast/effect-cancel widening follow-up implemented from this
pass:

- The spell-side band immediately after `SpellCast_SendClientSpellStopCast`
  now proves to contain two more concrete client request senders rather than
  generic leaves. Direct decompile of `14039cce0` shows the same bounded
  `0x0801 ClientSpellStopCast` send as `14039cc30`, but with the trailing flag
  field set to `1` instead of `0`; the helper otherwise shares the same active
  casting id, stop result, and local stop-cast/UI side-effect preparation
  (`InspectCodeAddress 14039cce0`,
  `Source/NexusForever.Network/Message/GameMessageOpcode.cs:741`,
  `Source/NexusForever.Network.World/Message/Model/ClientSpellStopCast.cs`).
  That is enough for the durable label
  `SpellCast_SendClientSpellStopCastSetTrailingFlag`.
- Direct decompile of `14039cda0` proves the next sibling is a separate cancel-
  effect sender, not another stop-cast variant. When the resolved effect kind
  is `0x0E` or `0x24` and the local-target guard passes, it runs the effect
  callback at vtable slot `+8`, then sends opcode `0x0802 ClientCancelEffect`
  with the current effect server unique id from `param_2 + 0x5C`
  (`InspectCodeAddress 14039cda0`,
  `Source/NexusForever.Network/Message/GameMessageOpcode.cs:742`,
  `Source/NexusForever.Network.World/Message/Model/ClientCancelEffect.cs`,
  `Source/NexusForever.WorldServer/Network/Message/Handler/Spell/ClientCancelEffectHandler.cs`).
  That is enough for the durable label `SpellEffect_SendClientCancelEffect`.
- This widens the current spell-side picture from a single stop-cast helper to
  a small outbound interrupt/cancel cluster: `14039cc30` and `14039cce0`
  encode the `ClientSpellStopCast` trailing-flag variant bit, while `14039cda0`
  branches into `ClientCancelEffect` for specific local effect kinds. The next
  widening targets in this band are therefore the remaining nearby helpers such
  as `14039ce20`, `14039cee0`, and `14039cff0`, which are now more likely to be
  related interrupt/cancel or cleanup leaves than generic spell dispatchers.
- Verification: both direct inspects completed successfully against the shared
  Ghidra project layout, avoiding the per-target lock path.

Thirty-ninth tradeskill profession request follow-up implemented from this
pass:

- The client-to-server profession request group is now mapped and labeled.
  Opcode `0x0857 ClientTradeskillLearn` registers the reused two-UInt32 writer
  `ActivateUnitFamily_WriteTokenAndTargetFieldPair` at `14007ec70` with object
  size `0x08`, matching the existing source payload as learn tradeskill id plus
  optional drop tradeskill id (`selected_decompiled.c:233`, `:240`).
  `Tradeskill_SendClientTradeskillLearn` at `140593db0` stages those two ids
  from the profession UI path and suppresses sending when the target profession
  is already active (`selected_decompiled.c:10099`, `:10106`).
- Opcode `0x084E ClientTradeskillPickTalent` registers the reused three-UInt32
  writer `ClientCraftingSimpleCraft_WritePayload` at `1400a5b50` with object
  size `0x0C`, proving the source packet shape as `{ TradeskillId, Tier,
  TradeskillBonusId }` (`selected_decompiled.c:1019`, `:1026`). The sender
  `Tradeskill_SendClientTradeskillPickTalent` at `14059a900` writes the
  selected talent tier as `tier - 1`, checks the zero-based tier is below `10`,
  verifies an active profession/available-point path, and requires the selected
  `TradeskillBonus` to appear in one of the five slots for that tier
  (`selected_decompiled.c:10153`, `:10159`).
- Opcode `0x0858 ClientTradeskillResetTalents` now has a dedicated writer label,
  `ClientTradeskillResetTalents_WritePayload` at `14007d010`, with object size
  `0x04`; it serialises exactly one 32-bit tradeskill id
  (`selected_decompiled.c:87`, `:93`). The sender
  `Tradeskill_SendClientTradeskillResetTalents` at `14059acb0` sends only for an
  active profession that has at least one selected talent slot
  (`selected_decompiled.c:10229`, `:10236`).
- Source now adds conservative handlers for `ClientTradeskillLearn`,
  `ClientTradeskillPickTalent`, and `ClientTradeskillResetTalents`. They
  validate `Tradeskill`, zero-based tier range, and the
  `TradeskillBonus -> TradeskillTalentTier -> Tradeskill` relation against game
  tables, then log and return without mutating profession state.
- Remaining bounded uncertainty: NexusForever still lacks a mapped profession
  manager/persistence boundary for active tradeskills, talent point spending and
  refunding, reset costs, relearn cooldowns, and the exact
  `ServerProfessionUpdate`/`ServerTradeskillRelearnCooldown` response rules.
  Learning, dropping, picking, and resetting remain diagnostic-only until that
  subsystem is mapped.
- Verification: the per-target WildStar64 export applied the expanded label set
  (`applied=188, created=0, skipped=0, missing=0`) and rendered
  `selected_decompiled.c` with 300 functions. `dotnet build
  Source\NexusForever.sln -v minimal --nologo` succeeds with the existing
  package warnings and `0 Error(s)`.

Fortieth action-set PvP restriction follow-up implemented from this pass:

- The previously blocked `RestrictedInPVP` branch is now mapped tightly enough
  to carry through to source. A direct caller trace on
  `MatchingGameMap_LookupById` (`140215240`) shows
  `ActionSet_CheckPvPRestriction` loading `ECX` from `DAT_140c65b98 + 0x108`
  immediately before the lookup, confirming that field as the current
  `MatchingGameMap` id (`TraceFunctionCallers 140215240`,
  `ActionSet_CheckPvPRestriction@1403a11c0`). A matching trace on
  `FUN_140214e00` then shows the client feeding `MOV ECX,dword ptr [RAX + 0x10]`
  from the returned map object into the next lookup, confirming the chained
  `MatchingGameMap -> MatchingGameType` relation through the map record's game-
  type id field (`TraceFunctionCallers 140214e00`).
- Two adjacent helpers now anchor the remaining field semantics strongly enough
  to stop treating `lVar2 + 0x10` as an opaque runtime byte blob. Direct
  decompile of `1403a1140` returns `~(*(uint *)(lVar1 + 0x10) >> 1) & 1` and
  `1403a1230` returns `~(*(uint *)(lVar1 + 0x10) >> 4) & 1` after the same
  `MatchingGameMap -> MatchingGameType` chain. That is consistent with the
  client reading a `MatchingGameType` flag dword at `+0x10`, not an unrelated
  pointer or state object, so the `RestrictedInPVP` bit tests at `0x40` and
  `0x04` are now carried through against
  `MatchingGameTypeEntry.MatchingGameTypeEnumFlags` in source.
- The remaining selector at `DAT_140c65b98 + 0x114` now aligns best with live
  PvP phase rather than hardcoded match-type metadata: the client compares only
  values `1` and `2`, and current source already defines
  `PvpGameState.Preparation == 1` and `PvpGameState.InProgress == 2` while the
  match runtime broadcasts those exact phases through `PvpMatch.SetState(...)`.
  Source now exposes that runtime phase on `IPvpMatch.State`, and
  `ClientRequestActionSetChangesHandler` returns `RestrictedInPVP` when the
  player is in a PvP match whose `MatchingGameTypeEnumFlags` carry bit `0x40`
  during `Preparation` or bit `0x04` during `InProgress`.
- `UpdateSpellInProgress` remains blocked. The client still walks the linked
  transaction state rooted under `*(DAT_140c65898 + 0x6490) + 0x15c0` and keyed
  by config `0x41e`, and current server source still has no equivalent
  in-flight spell-update transaction model to map that branch safely.
- Verification: `dotnet build Source\NexusForever.sln --no-restore` succeeds
  after exposing `IPvpMatch.State` and wiring the new LAS restriction check.

Forty-first linked-entry/loot collect follow-up implemented from this pass:

- The next spell-side widening target now has bounded packet identity even
  without a fresh direct inspect. Existing caller trace for
  `Network_SendOpcodePayloadHelper` shows `14039cff0` writing opcode `0x014F`
  with `{ OwnerUnitId, LootUnitId, Request=0 }` into the local payload before
  the send (`logs/trace_callers_1403f4900.txt:681`, `:688`). Current source
  maps `0x014F` to `ClientLootItem`, whose packet model is
  `{ OwnerUnitId, LootUnitId, Request }` and documents `Request == 0` as the
  collect branch rather than a request
  (`Source/NexusForever.Network/Message/GameMessageOpcode.cs:132`,
  `Source/NexusForever.Network.World/Message/Model/Loot/ClientLootItem.cs`).
  Together with the adjacent `PendingLootInteract` string xref, that is enough
  for the durable label `Loot_SendClientLootItemCollect`.
- The adjacent `14039d0f0` helper now looks like the higher-level pending-loot
  interaction wrapper rather than another bare sender. Existing traces show it
  references the same `PendingLootInteract` string, calls
  `ActivateUnit_SendClientActivateUnitCast` (`14039c430`), conditionally routes
  through `1403acd90`, and also emits `0x014F ClientLootItem` with both unit
  ids sourced from `EBX`
  (`logs/trace_callers_1403acd90.txt:607`,
  `logs/trace_callers_1403f4900.txt:695`). That is enough to prioritize it as
  the next spell-side inspect, but not yet enough to freeze a durable name.
- The next inward audio table helper can now be named conservatively from the
  prior direct inspect plus the already-labeled `AudioRuntime_ResolveLinkedNode`
  call chain. `140899180` uses `140895d10(...)` as the linked-entry lookup step
  before `140898590(...)` child-runtime acquisition
  (`exports/WildStar64.exe/selected_decompiled.c:20080`, `:20084`); prior
  direct inspection of `140895d10` showed it walking the entry table between
  `param_1 + 0xE0` and `param_1 + 0xE8`, binary-searching selector ranges
  against the incoming ids, optionally falling back through `1408958a0(...)`,
  and returning the matched entry plus a continuation flag via `param_7`.
  That is enough for the durable label `AudioRuntimeTable_FindLinkedEntry`.
- This keeps both fronts moving despite the current Ghidra lock contention. The
  spell-side immediate next inspect is now the higher-level
  `PendingLootInteract` wrapper `14039d0f0`, while the audio side has narrowed
  to the child-runtime acquisition and fallback helpers below the newly named
  table lookup (`140898590`, `1408958a0`).
- Verification: direct re-inspection and export-only validation could not be
  rerun this pass because both the shared and per-target Ghidra projects were
  locked; packet identity and audio-table semantics were instead recovered from
  existing traces, source models, and prior direct-inspection notes.

Forty-second action-set update-in-progress follow-up implemented from this pass:

- The native meaning of `ActionSet_CheckUpdateSpellInProgress`
  (`1403bb8d0`) is now strong enough to narrow the blocker precisely even
  though it still does not map to source. The helper lazily resolves config
  `0x41e`, caches the integer at `*(config + 4)` in `DAT_140dc227c`, then walks
  the linked list rooted at `*(*(DAT_140c65898 + 0x6490) + 0x15c0)` and returns
  true when any node's virtual method at `vtable + 8` matches that cached key.
  Together with the surrounding `1403988d0` inspect and prior caller context,
  that still points to a client-side in-flight spell-update transaction list,
  not a simple cooldown or combat-state gate.
- The server-side architecture check now explains why the branch should remain
  findings-only. `WorldServer.Service.HostedService` runs
  `networkManager.Update(lastTick)` first on the world thread; `NetworkManager`
  then iterates sessions and calls `session.Update(lastTick)` serially;
  `GameSession.Update` drains each session's queued incoming packets in order
  and invokes message handlers synchronously inline. That means a single
  `ClientRequestActionSetChanges` request is already fully validated and applied
  before the same session can enter the next LAS handler invocation.
- Current spell/action-set source also has no deferred completion state to back
  a real `UpdateSpellInProgress` result. `ClientRequestActionSetChangesHandler`
  mutates the `ActionSet` immediately, calls `SpellManager.UpdateSpell(...)`
  inline, and then queues `ServerActionSetClearCache`, `ServerActionSet`, and
  any `ServerSpellUpdate` messages in the same handler path. `SpellManager`
  does not own a transaction table, pending spell-update list, or begin/end
  lifecycle comparable to the client's `+0x15c0` list.
- Decision: do not add a synthetic server-side `UpdateSpellInProgress` gate yet.
  Returning that result without a true deferred spell-update pipeline would
  invent behavior the current server does not have and could reject otherwise
  valid sequential LAS requests. This branch stays blocked until a real
  asynchronous spell-update transaction model is found or introduced.
- Verification: this pass only updated findings after confirming the packet
  dispatch path and spell-update flow in current source; no code changes or
  rebuild were required.

Forty-third P2P trading request follow-up implemented from this pass:

- The full client-to-server P2P trading request group is now mapped and
  labeled. Registration evidence shows the no-op writer
  `ClientCraftingAbandon_WritePayload` at `140001ba0` is reused by the
  zero-payload trade requests `0x018C ClientP2PTradingAcceptInvite`,
  `0x018F ClientP2PTradingCancelTrade`, `0x0190 ClientP2PTradingCommit`, and
  `0x0191 ClientP2PTradingDeclineInvite`, each with a registered one-byte
  object and no payload bits (`selected_decompiled.c:7`, `:14`).
- The remaining trade writers line up with the existing source models:
  `0x0192 ClientP2PTradingInitiateTrade` uses the shared raw-UInt32 writer at
  `14007d010` for target unit id (`selected_decompiled.c:87`, `:95`), while
  `0x018D ClientP2PTradingAddItem`, `0x0194 ClientP2PTradingRemoveItem`, and
  `0x0197 ClientP2PTradingSetMoney` use the newly labeled
  `ClientP2PTradingUInt64_WritePayload` at `14007d840` for one raw 64-bit
  field (`selected_decompiled.c:235`, `:242`). That confirms the item guid and
  credit-offer payloads are 64-bit client fields.
- Sender labels now cover the active-trade and UI paths: `14074ae30` accepts,
  `14074aed0` declines, `1403a6590`/`14074bb60` cancel,
  `1403a6e00`/`14074b160` commit, `1403a6a40` initiates against a target unit,
  `1403a6b50` adds an item, `1403a6c50` removes an item, and `1403a6ce0` sets
  the credit offer (`selected_decompiled.c:6991`, `:7072`, `:7138`, `:7181`,
  `:7224`, `:7306`, `:19201`, `:19240`, `:19273`, `:19305`).
- Source now corrects `ClientP2PTradingRemoveItem` and
  `ClientP2PTradingSetMoney` to read 64-bit payloads. It also adds
  conservative handlers for all eight P2P trading client requests; they validate
  visible target players, owned inventory items, and credit affordability where
  those fields exist, then log and return without changing trade state.
- Remaining bounded uncertainty: NexusForever still has no mapped server-side
  P2P trade state machine, invite/session partner model, offer locking,
  two-party commit handshake, inventory transfer, money transfer, or precise
  server update/result response rules. Trade mutation remains blocked until
  that state boundary is mapped.
- Verification: the per-target WildStar64 export applied the expanded label set
  (`applied=203, created=1, skipped=0, missing=0`) and rendered
  `selected_decompiled.c` with 300 functions. `dotnet build
  Source\NexusForever.sln -v minimal --nologo` succeeds with the existing
  package warnings and `0 Error(s)`.

Forty-fourth pending-loot wrapper follow-up implemented from this pass:

- A direct inspect of `14039d0f0` was attempted again against the split
  WildStar64 Ghidra project, but the project remained externally locked, so
  this pass had to complete from the existing caller traces plus prior helper
  decompiles rather than a fresh full decompile.
- Even without that fresh inspect, the bounded evidence is now strong enough to
  freeze a conservative high-level label. `14039d0f0` references the
  `PendingLootInteract` string directly
  (`exports/WildStar64.exe/string_xrefs.csv:2743`), calls the already-labeled
  `ActivateUnit_SendClientActivateUnitCast` (`logs/trace_callers_1403acd90.txt:607`),
  then conditionally routes through `1403acd90` and `1403a12a0` before exiting
  (`logs/trace_callers_1403acd90.txt:619`,
  `logs/trace_callers_1403f4900.txt:697`). That is enough for the durable label
  `Loot_HandlePendingLootInteract`.
- The two shared helpers are now bounded tightly enough to explain why this is
  a wrapper rather than another bare sender. Prior direct inspect of
  `1403acd90` shows it is a generic service/runtime resolver that returns a
  context object for the local-player path or the broader service tree, not a
  loot-specific leaf (`logs/inspect_1403acd90.txt:48`). Separate caller
  evidence from the rapid-transport path shows `1403a12a0` is reused as a
  post-send result/report helper after nontrivial request senders, again not a
  loot-only leaf (`logs/inspect_140520710.txt:113`, `:116`).
- `14039d0f0` also contains a bounded fallback `0x014F ClientLootItem` send.
  Existing caller trace for `Network_SendOpcodePayloadHelper` shows the helper
  loading opcode `0x014F`, zeroing the request bit, and writing the same
  returned dword into both payload unit-id fields before the send
  (`logs/trace_callers_1403f4900.txt:695`, `:702`). Source still maps `0x014F`
  to `ClientLootItem` with payload `{ OwnerUnitId, LootUnitId, Request }`
  (`Source/NexusForever.Network/Message/GameMessageOpcode.cs:132`,
  `Source/NexusForever.Network.World/Message/Model/Loot/ClientLootItem.cs`).
  That makes `14039d0f0` the higher-level pending-loot interaction wrapper
  above the already-labeled `Loot_SendClientLootItemCollect` leaf at
  `14039cff0`.
- This shifts the spell-side continuation boundary again. The
  `PendingLootInteract` pair is now split into the high-level wrapper
  `14039d0f0` and the direct collect sender `14039cff0`, so the next unresolved
  helpers in this local band are the remaining adjacent spell/loot interaction
  siblings such as `14039ce20`, `14039cee0`, `14039d230`, and `14039d4a0`.
- Verification: durable files were updated and textual diff validation remains
  the safe check for this pass. Export-only Ghidra revalidation is still blocked
  until the external project lock clears.

Forty-fifth shield-heal damage type follow-up implemented from this pass:

- The conservative `HealShields` runtime still carried one leftover semantic
  mismatch: `DamageCalculator.CalculateShieldHealing(...)` built its outgoing
  `DamageDescription` with `DamageType.Heal` even though the effect family,
  static enum surface, and combat log path already distinguish shield healing
  from health healing.
- A direct source follow-up narrowed the impact. The shield-heal handler still
  mutates `target.Shield` directly, so this field was not changing local shield
  behavior, but the same damage description is serialized into outgoing spell
  results. Leaving `DamageType.Heal` in place therefore preserved an avoidable
  on-the-wire mismatch for clients and later debugging.
- The runtime now stamps shield-heal damage descriptions as
  `DamageType.HealShields`. This is intentionally a narrow correctness fix
  only. Open questions from the broader shield/vital cluster remain unchanged:
  shield-heal packet cadence, full packet parity, and whether
  `HealingAbsorption` should consume shield healing still need sniff/runtime
  validation before any wider behavior changes.

Forty-sixth activate-unit/audio child-runtime follow-up implemented from this
pass:

- Audio-side direct inspect now resolves the child-runtime helper immediately
  below `AudioRuntimeTable_FindLinkedEntry`. `140898590(longlong *param_1, int
  param_2)` returns `param_1[0x10]` when the requested child id is zero,
  otherwise checks the local child table through `1408975e0(param_1 + 0x14,
  param_2)`, falls back to the runtime virtual method at slot `+0xC8` when the
  table path does not claim the id, and otherwise resolves the child through
  `140898620(...)`. If the returned child runtime still has a null linked state
  pointer at `+0x18`, it is discarded and zero is returned
  (`logs/WildStar64.InspectCodeAddress.ghidra.log:50` onward for
  `140898590`). That is enough for the durable label
  `AudioRuntime_ResolveChildById`.
- This tightens the audio boundary one layer deeper than the prior subagent
  estimate. `140898590` is not merely an allocator; it is the wrapper that
  resolves either the current child runtime, a table-backed child, or a virtual
  fallback child for the requested id before `AudioRuntime_ResolveLinkedNode`
  decides whether the cloned candidate node can survive. The next inward audio
  ambiguity now shifts to the larger table-backed constructor/helper path at
  `140898620` and the older fallback probe `1408958a0`.
- Spell-side direct inspect of `14039d4a0` also resolved the subagent's
  earlier UI-heavy guess into a concrete request wrapper. The helper first
  rejects a null record at `param_1 + 0x18`, optionally raises the local
  floater path via `140437a10(..., 0x15F, ...)` when the record flag at
  `param_1 + 0x34EC` is set, then tries `14046c580(param_1,
  *(DAT_140c65898 + 0x78))`. If that lookup returns zero, it sends opcode
  `0x00B3 ClientActivateUnit` with the unit id from `param_1 + 8`; otherwise it
  primes local activation state via `14055b0e0(...)`, falls back to
  `ActivateUnit_SendClientActivateUnitCast(...)`, and reports nontrivial cast
  failures through `1403acd90` and `1403a12a0`
  (`logs/WildStar64.InspectCodeAddress.ghidra.log:55` onward for `14039d4a0`,
  `Source/NexusForever.Network/Message/GameMessageOpcode.cs:28`). That is
  enough for the durable label `ActivateUnit_HandleDirectOrCastRequest`.
- This narrows the spell-side local band more sharply than the old string-only
  picture. `14039d4a0` is not just a quest/path overhead display helper even
  though the record it consumes carries many path/quest/spell presentation
  strings; it is a real activation wrapper that chooses between direct
  `ClientActivateUnit` and the existing activate-unit cast path based on the
  resolved activation target id. The remaining nearby unresolved spell helpers
  are now better concentrated in `14039ce20`, `14039cee0`, and `14039d230`.
- Verification: both direct inspects completed successfully against the shared
  Ghidra project layout during this pass; export-only validation should be
  rerun after landing the new labels so the durable map and exported artifacts
  are back in sync.

Forty-seventh marketplace request follow-up implemented from this pass:

- The marketplace request group is now mapped as one coherent client-to-server
  surface. Registration evidence labels the commodity/order writers for
  `0x03E6 ClientRequestCommodityInfo` at `140085420` with size `0x04`,
  `0x03EC ClientRequestOwnedCommodityOrders` and
  `0x03ED ClientRequestOwnedItemAuctions` through the shared no-op writer
  `140001ba0`, `0x0093 ClientCommodityOrderCancel` at `14009a020` with size
  `0x10`, `0x0094 ClientAuctionCancel` at `14009ab70` with size `0x10`,
  `0x055E ClientAuctionBuyOrderSubmit` at `140099e90` with size `0x18`,
  `0x06DA ClientCommoditySellOrderSubmit` at `140099e70` with size `0x38`,
  `0x06DC ClientAuctionSellOrderSubmit` at `140088e60` with size `0x18`, and
  `0x07DC ClientAuctionsByFilterRequest` at `14009a7d0` with size `0x38`.

Forty-eighth item-visual-swap duration restore follow-up implemented from this pass:

- The next safe spell-runtime widening outside the blocked shield/vital edge
  cases was `ItemVisualSwap` duration handling. Prior conservative runtime
  behavior already applied immediate visual-slot overrides through
  `IWorldEntity.AddVisual`, but duration-backed rows were only traced with
  `duration-restore-unimplemented` even though nearby appearance families
  (`DisguiseOutfit`, `MimicDisguise`) already had bounded previous-visual
  snapshot and restore paths.
- The runtime now uses the same bounded appearance-state pattern for
  `ItemVisualSwap`. `HandleEffectItemVisualSwap(...)` validates the packet-width
  fields as before, captures the current visual for the target slot when the
  effect has a duration, applies the override, and stores the previous slot
  visual by effect id. Spell lifetime removal now restores that prior visual on
  expiry, and tracked-state removal restores it through the generic
  force-remove/dispel cleanup path as well.
- This remains a conservative implementation rather than a full semantic close.
  The raw client slot ids are still preserved instead of being renamed to a
  complete equipment-slot map, `DataBits02/03` remain uninterpreted, and the
  duration fixtures still need runtime/sniff validation to confirm whether any
  rows restore only when unchanged or carry extra slot-specific behavior.
- Sender labels now cover the active marketplace/UI request paths:
  `14075f0d0` requests commodity info, `14075f110` requests owned commodity
  orders, `1407607f0` requests owned item auctions, `14075fd90` sends auction
  search filters, `1406a0740` submits an auction buy order, `140519a00`
  submits an auction sell order, `1406a0570` submits a commodity sell order,
  `1406a20c0` cancels an auction, and `1406a1270` cancels a commodity order.
  Existing send traces show the expected field staging for item ids, auction or
  order ids, min bid, buyout, quantity, unit price, and search selectors.
- Source now adds conservative handlers for all nine marketplace request
  models. Read-only requests validate table-backed item/search/filter fields
  and return empty result packets. Submit and auction-cancel requests validate
  item ids, owned inventory items, ids, prices, and quantities, then return the
  existing disabled marketplace result where a response packet is mapped.
  Commodity-order cancel currently validates and logs only because no precise
  cancel-failure response path has been mapped yet.
- Packet parsing is hardened at the receive boundary: auction search item and
  filter counts are bounded by remaining bytes and the known seven-filter
  client limit, and unknown auction-filter enum values now throw
  `InvalidPacketValueException` instead of falling into a server-side
  `NotImplementedException`.
- Remaining bounded uncertainty: NexusForever still has no mapped persisted
  marketplace/order-book service for fees, rake, listing duration, expiry,
  search matching, partial fills, mail settlement, or commodity/auction state
  mutation. The request surface is safe and labeled, but real marketplace
  mutation remains blocked until that service boundary is designed from
  client/runtime and server evidence.
- Verification: the WildStar64 export applied the expanded label set
  (`applied=221, created=2, skipped=0, missing=0`) and rendered
  `selected_decompiled.c` with 340 functions. `dotnet build
  Source\NexusForever.sln -v minimal --nologo` succeeds with the existing
  package warnings and `0 Error(s)`.

Forty-eighth fallback-node/bindcheck follow-up implemented from this pass:

- Audio-side direct inspect now resolves the larger helper behind the child
  resolver table path. `140898620(longlong *param_1, byte *param_2)` gates on
  the runtime state nibble at `param_1 + 0x62`, derives the active descriptor
  path through `140895bc0(...)`, and then either materializes a child runtime
  directly from that descriptor or builds a nested local descriptor record and
  refreshes the child through repeated `1408981f0(...)` calls. The helper also
  returns a low-bit mode flag through `param_2`, and falls back through the
  runtime virtual path when the refreshed child still lacks a linked state
  pointer. That is enough for the durable label
  `AudioRuntime_ResolveTableBackedChild`.
- The older fallback probe under `AudioRuntimeTable_FindLinkedEntry` is also
  now resolved directly. `1408958a0(...)` walks a fallback chain through each
  node's `+0x40` link, binary-searches the current node id at `+0x18` against
  the sorted id range stored in the supplied entry-local table, and returns the
  first matching node before the chain reaches the stop pointer. That is enough
  for the durable label `AudioRuntimeTable_FindFallbackNodeMatch`.
- Spell-side direct inspect of `14039cee0` showed that the helper is not
  another packet sender. Instead it looks up the keyed loot entry under the
  local tree rooted at `param_1 + 0x7D90` using the request key at `param_2 +
  4`, materializes the matched object set into a temporary container through
  `1404111e0(...)`, and forwards that container into `140430e00(...)`. A
  follow-up direct inspect of `140430e00` then proved the downstream semantics:
  it serializes the prepared object set into an `itemDrop` payload and emits
  the named client event `LootBindcheck` through
  `ClientEvent_DispatchNamedEvent(...)`. Together that is enough for the
  durable labels `Loot_PrepareAndDispatchBindcheck` and
  `Loot_DispatchBindcheckEvent`.
- This closes the three subagent-guided continuation branches from the prior
  pass. Audio-side ambiguity now shifts deeper to helpers such as `1408981f0`
  or `140895bc0` if tighter descriptor semantics are still needed, while the
  spell-side local band has narrowed further to the remaining adjacent helpers
  `14039ce20` and `14039d230`.
- Verification: all three direct target inspects plus the supporting
  `140430e00` dispatcher inspect completed successfully against the shared
  Ghidra project layout during this pass; export-only validation should be
  rerun after landing the new labels so the durable map and exported artifacts
  are synchronized again.

Forty-ninth progress-click/range-check follow-up implemented from this pass:

- Direct inspect now resolves the remaining helper immediately before the loot
  bindcheck wrapper. `14039ce20(param_1, param_2)` stores or replaces the
  object slot at `DAT_140c65898 + 0x7188`, records the replacement tick at
  `+0x7190`, retains/releases the swapped objects through virtual callbacks,
  and throttles rapid replacement when the incoming object's status code from
  virtual slot `+0x68` is one of `1..3` or `7` and less than `1000` ms have
  elapsed since the last store. Caller tracing then showed the only code caller
  in this pass is `1407798c0`, which stages five fields into an object, calls
  `14039ce20(param_1, param_1)`, and dispatches the named client event
  `ProgressClickWindowCompletionLevel` on success. That is enough for the
  durable label `ProgressClickWindow_SetActiveObject`.
- The other adjacent unresolved helper is now bounded as a range gate rather
  than another dispatcher. `14039d230(param_1, param_2)` lazily caches the
  float at offset `+0x18` from client table id `0x146`, defaulting to `10.0`
  when the lookup fails, and then calls `1403ad690(*(param_1 + 0x6490),
  param_2, 0, cachedFloat, 0)`. A direct inspect of `1403ad690` proved that it
  computes adjusted 3D separation between two entities and returns whether the
  resulting distance is within the supplied max range. Caller tracing then
  showed the sole code caller in this pass is `1403a64f0`, which first gates on
  a resolved loot-tree entry from `1403967f0(..., *(param_2 + 8))` before using
  `14039d230` as the final range check. That is enough for the durable label
  `Loot_CheckInteractionRange`.
- This narrows the spell-side local band one step further than the previous
  pass. The immediate local ambiguity is no longer centered on `14039ce20` or
  `14039d230`; the next useful spell-side targets move outward to helpers such
  as `14039d2c0` or the loot-side caller `1403a64f0` if tighter interaction
  semantics are still needed.
- Verification: direct inspects completed successfully for `14039ce20`,
  `14039d230`, `1403ad690`, `1403a64f0`, and `1407798c0`, and caller traces for
  both local helpers returned exactly one code caller each plus one data
  reference.

Fiftieth duel/PvP toggle request follow-up implemented from this pass:

- The client-to-server duel and PvP toggle request group is now mapped and
  labeled. Registration evidence from the world-message registration block
  shows `0x00E8 ClientDuelAccept`, `0x00E9 ClientDuelDecline`,
  `0x00EA ClientDuelForfeit`, and `0x00EC ClientDuelInitiate` all using the
  shared no-op writer at `140001ba0` with registered one-byte objects. The
  same pass labels `ClientBool_WritePayload` at `14007e610`; direct inspect
  shows it turns a nonzero 32-bit value into one emitted payload bit, matching
  both `0x0170 ClientSetIgnoreDuelRequests` and
  `0x0173 ClientPvpToggleFlags` as registered 4-byte bool objects
  (`selected_decompiled.c:11`, `:275`, `:282`).
- The GameLib sender surface is also now named. The Lua method table around the
  `InitiateDuel`, `AcceptDuel`, `DeclineDuel`, `ForfeitDuel`,
  `SetIgnoreDuelRequests`, and `TogglePvpFlags` strings points at
  `140701120`, `140701150`, `140701180`, `1407011b0`, `140701290`, and
  `140701090` respectively. Exported decompile now shows the zero-payload duel
  senders, the pending-request guard at local duel state `1`, the active-duel
  forfeit guard at local duel state `3`, the ignore-duel local flag bit `0x8`,
  and the PvP flag toggle preflight before opcode `0x0173`
  (`selected_decompiled.c:17907`, `:17952`, `:17972`, `:17994`, `:18016`,
  `:18038`).
- Source now adds conservative world handlers for all six request models. Duel
  initiation validates the selected target-player boundary already implied by
  the zero-payload packet shape, rejects missing/self targets with
  `InvalidDuelTarget`, rejects dead or in-combat participants with the mapped
  `DuelFailureReason` values, and otherwise returns `CannotDuelRightNow`
  because NexusForever has no duel session manager yet. A follow-up runtime
  increment now also maps `ClientSetIgnoreDuelRequests` onto
  `CharacterFlag.IgnoreDuelRequests` through the existing character-flag update
  path and rejects duel initiation against targets with
  `PlayerIsIgnoringDuels`. Duel accept, decline, forfeit, and PvP flag toggles
  still remain logged without mutating state.
- Remaining bounded uncertainty: the packet surface is mapped, but real duel
  behavior still needs a server-side duel request/session model, opponent state,
  countdown/start/result broadcasts, area-leave warnings, PvP flag persistence,
  cooldown handling, and the remaining PvP flag semantics. The handlers
  intentionally avoid synthesizing those states until the service boundary is
  mapped.
- Verification: the WildStar64 export applied the expanded label set
  (`applied=237, created=1, skipped=0, missing=0`) and rendered
  `selected_decompiled.c` with 360 functions. `dotnet build
  Source\NexusForever.sln -v minimal --nologo` succeeds with the existing
  package warnings and `0 Error(s)`.

Fifty-first shared target-interaction follow-up implemented from this pass:

- Direct inspect now resolves the larger local helper immediately above the
  activate-unit wrapper into a shared target-interaction attempt path.
  `14039d2c0(param_1, param_2)` first validates the local interaction state
  through `1403ad600(*(param_1 + 0x78))`, resolves the target object by id via
  `1403d90d0(DAT_140c65898, param_2)`, and reads the target interaction
  descriptor at `target + 0x1908`. It rejects descriptors that are disabled, do
  not expose a callback, use type `0x65`, or would duplicate the current active
  progress-click object already stored at `param_1 + 0x7188`.
- The core behavior boundary is now clear from the downstream helper chain.
  The function derives a default interaction distance from descriptor slot
  `piVar1[4]` with a fallback of `5.0`, optionally pulls an override family id
  for type `0x4d`, and then calls `1403ebe80(...)`. A direct inspect of
  `1403ebe80` proved that helper is not the action itself: it resolves the
  effective interaction family through `14046c580` and `1403acd90`, derives the
  active lower and upper distance bounds through `1403ad860` and `1403ad8f0`,
  and finally delegates to `1403ad690` for the adjusted 3D range check. Caller
  tracing then showed the sole code caller in this pass is
  `Interaction_AttemptTargetAction`, which is enough for the durable label
  `Interaction_CheckEffectiveRange`. If the attempt is out of range and the
  descriptor permits fallback, `14039d2c0`
  queues `1405592f0(param_1 + 0x70b0, target, 3)`; otherwise it runs the
  descriptor callback, updates active interaction state through `1403dd1c0`
  when configured, and flushes the pending action state through `1405598d0`
  when needed. That is enough for the durable label
  `Interaction_AttemptTargetAction`.
- Caller recovery also tightened the public boundary without forcing unsafe
  names onto the callers themselves. `14077e2c0` uses the current active object
  id at `*(DAT_140c65898 + 0x6490) + 0x108` as the fallback target when a
  special-case branch does not short-circuit, while `1404d9450` is a broader
  context-sensitive key handler that dispatches `CSIKeyPressed`, routes several
  stored action kinds through a local switch table, and falls back into
  `14039d2c0` when the stored target id at `param_1 + 0x6720` should be used.
  Both callers reinforce that `14039d2c0` is the shared attempt path rather
  than a loot-only leaf.
- This moves the spell-side ambiguity outward again. The nearby local band is
  now substantially labeled through `14039d2c0`, so the next useful targets are
  the still-unnamed caller families `1404d9450` and `14077e2c0` or the queue or
  state-update helpers `1405592f0` and `1403dd1c0` if tighter CSI/progress-click
  semantics are still needed.
- Verification: direct inspects completed successfully for `14039d2c0`,
  `1403ebe80`, `14077e2c0`, and `1404d9450`, and caller tracing for
  `14039d2c0` returned exactly two code callers plus six data references while
  `1403ebe80` returned exactly one code caller plus one data reference.

Fifty-second current-active-object interaction wrapper follow-up investigated
from this pass:

- Direct inspect on `14077e2c0(param_1, param_2)` tightened the current-object
  branch without yet exposing a stable public callback name. The function only
  acts when `param_2 == 0`, pulls the current active object id from
  `*(DAT_140c65898 + 0x6490) + 0x108`, checks a narrow special-case branch on a
  resolved type-`0x14` target plus several local state fields, and if that
  branch succeeds calls `140397ce0(DAT_140c65898)` before returning. Otherwise
  it falls back to `Interaction_AttemptTargetAction(DAT_140c65898, currentId)`.
- Caller tracing still stops short of a safe durable label. The recovered refs
  are five data references and zero code callers; three of those refs are only
  unwind metadata at `140c06138`, `140c06154`, and `140c06164`, while the one
  semantic-looking remaining slot at `140b6d550` sits in an otherwise unnamed
  function-pointer table next to `14077e3b0` and several `1405c8xxx` helpers.
  That is not enough evidence to force a stable public name for the callback
  family yet.
- This keeps `14077e2c0` on the bounded unresolved list, but narrows the next
  useful decomp step: inspect the sibling table entry `14077e3b0` or recover
  the owning function-pointer table around `140b6d540` before attempting to
  name the wrapper itself.
- Verification: direct inspect, caller trace, and nearby-data dump completed
  successfully for `14077e2c0` and the semantic table slot at `140b6d550`.

Fifty-third deferred interaction-queue follow-up implemented from this pass:

- Direct inspect plus caller tracing now resolve the shared queue worker beneath
  the target-interaction path as `140559920 = DeferredActionQueue_UpdateAndDispatch`.
  The helper is invoked each scene tick from `SceneLifecycle_UpdateAndReplayQueuedStates`
  and from several queue-arm helpers, clears queued state when the local actor or
  target becomes invalid, tracks position, delay, and retry state while the actor
  moves into range, and then redispatches queued action kinds once the range or
  state gates pass. The current decode shows queued kind `2` routes through the
  callback table at `DAT_140c89d80[subkind]`, queued kind `3` re-enters
  `SpellCast_ValidateAndDispatch`, queued kinds `5/6` send the `0x0096`
  activate-unit variant, and queued kind `7` reuses
  `SpellCast_SendClient0x009dVariant`.
- The smaller wrapper immediately below `Interaction_AttemptTargetAction` is now
  bounded enough for `1405592f0 = DeferredActionQueue_ArmInteractionAction`.
  It validates the profile-specific allow-mask branch, requires a live local
  interaction source and an interaction subkind below `0x24`, clears the queue
  block through `1405598d0`, stores queued mode `2`, target entity id,
  interaction range, and interaction subkind into the local queue state, and
  immediately hands off to `DeferredActionQueue_UpdateAndDispatch`. Caller
  tracing still shows `Interaction_AttemptTargetAction` as the only direct code
  caller of `1405592f0` seen in this pass.
- The post-callback branch through `1403dd1c0` remains intentionally unnamed.
  Direct inspect now shows a much larger target-feedback or state-transition
  routine that resolves localized text, builds a `"target"` payload or event,
  and flips multiple timed target-state fields, but the current evidence is not
  yet enough to distinguish notification, UI, and state-machine semantics
  safely.
- This narrows the remaining spell-side ambiguity to the broader caller families
  `1404d9450` and `14077e2c0`, or to a deeper dedicated pass on `1403dd1c0` if
  the post-callback feedback/state side must be named.
  `140559920`, and `1403dd1c0`; caller tracing for `1405592f0` returned one
  code caller plus one data reference, while `140559920` returned the scene-tick
  caller, several queue-arm helpers, and four data references.

Fifty-fifth direct interaction range-guard follow-up implemented from this
pass:

- The mapped client boundary around `Interaction_AttemptTargetAction` and
  `Interaction_CheckEffectiveRange` now has a conservative server counterpart
  on the direct world-interaction handlers. `ClientActivateUnit`,
  `ClientActivateUnitCast`, `ClientEntityInteract`, and
  `ClientEntityInteractChair` now reject out-of-range requests before invoking
  activation callbacks.
- The server-side guard uses `Creature2.ActivateSpellMinRange` and
  `Creature2.ActivateSpellMaxRange` when the creature row exposes them, with a
  conservative `5.0` fallback for the upper bound that matches the mapped
  client descriptor default when no explicit range is present. Vendor
  interaction reuses `GenericError.VendorTooFar`; the other direct handlers call
  `OnActivateFail` without inventing an unmapped generic error.
- This pass still leaves the client deferred retry path blocked. The native
  queue helpers `DeferredActionQueue_ArmInteractionAction` and
  `DeferredActionQueue_UpdateAndDispatch` remain mapped-only, so NexusForever now
  fails out-of-range direct requests conservatively instead of queueing movement
  and retry state.
- Chair interaction now shares the same busy/range guard shell as the other
  direct activation paths instead of bypassing those checks completely.
- Verification: `dotnet build Source\NexusForever.sln --no-restore` now passes
  with the existing warnings only.

Fifty-fourth current-object profile callback follow-up investigated from this
pass:

- Direct inspect now bounds the sibling table entry `14077e3b0(param_1)` as a
  current-object profile initializer rather than another action dispatcher. The
  function seeds a block of floats and state fields on `param_1`, disables the
  profile outright when the local actor is missing or when any populated local
  slot at `actor + 0x2d8` resolves table flags with bit `0x4`, and otherwise
  evaluates the current active object id from `actor + 0x108`.
- The remaining branch structure still stops short of a stable public name. For
  local type-`0x14` targets the callback reuses the same special-case gate seen
  in `14077e2c0` and keeps the profile enabled only when the resolved
  `1403acd90(...)` service object is non-null. For other targets it requires an
  enabled interaction descriptor at `target + 0x1908` and a non-null callback
  pointer at descriptor offset `+0x40`; otherwise it resets the profile back to
  its disabled defaults. That is enough to say the function configures a
  current-object interaction profile, but not enough to distinguish the owning
  callback family or UI surface safely.
- Caller tracing reinforces that this is still a table-driven callback rather
  than a normal helper. `14077e3b0` has zero recovered code callers and five
  data references: three unwind-metadata refs, one semantic slot at
  `140b6d540` in the same unnamed function-pointer table as `14077e2c0`, and a
  second data ref at `140e374a0` that could not yet be expanded because the
  shared Ghidra project was locked during the follow-up dump.
- This keeps `14077e3b0` blocked for naming, but narrows the next useful decomp
  step again: recover the owning table or inspect the paired callback entries
  around `140b6d540` before attempting to label either current-object callback.
- Verification: direct inspect and caller tracing completed successfully for
  `14077e3b0`.

Fifty-fourth audio table-child materialization follow-up implemented from this
pass:

- Direct inspect plus caller tracing now resolve the small selector beneath the
  table-backed child path as `140895bc0 = AudioRuntimeTable_SelectDescriptorEntry`.
  The helper walks the `0x68`-byte records between `param_1 + 0xe0` and
  `param_1 + 0xe8` from last to first, matches `param_2` against the record's
  primary sorted id range unless that range begins with `-1`, then matches
  `param_3` against the secondary range with the same wildcard handling, and
  returns the first record that satisfies both tests. If nothing more specific
  matches, it falls back to the default first record. Caller tracing still shows
  `AudioRuntime_ResolveTableBackedChild` as the only direct code caller in this
  pass, which keeps the durable boundary safely inside the runtime-table
  descriptor family.
- The heavier child-refresh helper immediately below that selector is now
  bounded enough for `1408981f0 = AudioRuntime_MaterializeTableBackedChild`.
  When an existing child runtime is supplied, it preserves accumulated timing
  from `param_3 + 8`, derives the descriptor timing offsets through
  `140891b60`, `140891630`, and `1408922f0`, and updates the child through
  `14088c9f0`. When a nonzero `param_4` is present it also resolves the related
  table object, prepares descriptor-local state through `140891aa0`, creates a
  child payload object through `1408917a0`, and feeds the result into
  `14088fb00(...)`. If that materialization succeeds, it applies the remaining
  descriptor-driven offset or modulation updates through `14088d6b0` and
  `14088e1d0`; otherwise it falls back to the runtime virtual child accessor.
  Caller tracing returned three direct code calls, all from distinct branches
  inside `AudioRuntime_ResolveTableBackedChild`, plus three data references.
- This tightens the deeper audio chain without overnaming the lower runtime or
  asset helpers. The current ambiguity now shifts inward to constructors or
  descriptor workers such as `14088fb00`, `1408917a0`, or the `140891xxx`
  timing helpers if more precise child-runtime semantics are still needed,
  while the already-established `+0x348` boundary remains audio output or
  resampler state rather than general gameplay logic.
- Verification: direct inspects and caller traces completed successfully for
  both `140895bc0` and `1408981f0` against the shared Ghidra project.

Fifty-fifth support/report request follow-up implemented from this pass:

- The remaining customer-support/report client request family is now mapped and
  labeled. Registration evidence ties `0x033F ClientCustomerSurveySubmit` to
  `1400a6830` with a `0x18`-byte object, `0x06C5 ClientIncidentReport` to
  `14009fd30` with a `0x30`-byte object, `0x06D1 ClientSupportTicket` to
  `14007cc20` with a `0x28`-byte object, `0x0830 ClientReportBug` to
  `14007c990` with a `0x18`-byte object, `0x0831 ClientStuck` to
  `14007c7c0` with an `0x08`-byte object, and `0x0833 ClientSuggest` to the
  small string-writer thunk at `14007ae80`.
- Direct writer inspection now matches the existing NexusForever packet models:
  `ClientStuck_WritePayload` emits a 3-bit `UnstickType` plus 32-bit context
  token, `ClientReportBug_WritePayload` emits the 16-bit bug category, selected
  unit id, Quest2 id, and description string, `ClientSupportTicket_WritePayload`
  emits category/subcategory, player position vector, subject/body strings, and
  `Language`, and `ClientCustomerSurveySubmit_WritePayload` emits a 14-bit
  survey id followed by packed survey answers and comment text. The incident
  report writer covers the post-identity tail as 3-bit reason, 4-bit source,
  note string, object id, days-ago float, and permanent-ignore bit. Exported
  labels appear in `selected_decompiled.c` for all six writers and all six
  UI/Lua senders (`ClientSuggest_WritePayload` at `:87`,
  `ClientSupportTicket_WritePayload` at `:260`,
  `ClientIncidentReport_WritePayload` at `:1499`,
  `Support_SendClientSupportTicket` at `:11833`,
  `Support_SendClientStuck` at `:12100`, and
  `Lua_GameLib_ReportBug` at `:19037`).
- Source now handles the whole group conservatively. `ClientCustomerSurveySubmit`
  rejects unsupported survey ids with `InvalidPacketValueException` instead of
  falling into a null dereference. The survey, incident-report, bug-report,
  stuck, and suggestion handlers log only structured metadata and free-text
  lengths. Support tickets validate the language enum and return
  `ServerSupportTicketResult { Success = false }` because there is still no
  ticket backend to persist or route the request. Stuck requests are logged but
  do not teleport, kill, or cast until the server-side stuck spell/result path
  is mapped.
- Remaining bounded uncertainty: the packet surfaces are mapped, but real
  support behavior still needs moderation/report storage, ticket category data
  and persistence, survey storage plus the unsupported survey submodels, and
  validated stuck teleport/suicide server semantics. The implementation is
  therefore diagnostic plus explicit rejection where the current server lacks a
  durable backend.
- Verification: `dotnet build Source\NexusForever.sln` succeeds with the
  existing package warnings and `0 Error(s)`. The WildStar64 export applied the
  expanded label set (`applied=255, created=1, skipped=0, missing=0`) and
  rendered `selected_decompiled.c` with 400 functions; `functions.csv` confirms
  all twelve support/report labels.

Sixty-first current-target CSI leaf follow-up implemented from this pass:

- The next two callback-table leaves beside the current-object CSI pair are now
  bounded tightly enough for conservative behavior-first labels. Direct inspect
  of `14077e580(param_1, param_2)` shows it only acts on the pressed-key branch,
  validates the current active target against the same local subject and
  special type-`0x14` service gates used by the other CSI leaves, then arms
  `DeferredActionQueue_ArmTargetApproach` for that target and dispatches a
  localized target-name message. That is enough for the durable label
  `CSIAction_HandleCurrentTargetApproach`.
- Direct inspect of the adjacent leaf `14077e770(param_1)` is also now enough
  for a conservative availability label. The helper resets the same CSI action
  state block used by the neighboring callbacks, preserves the service-target
  profile only when the current active target is a valid type-`0x14` target
  that passes the narrow relation check, and otherwise disables the action by
  clearing the availability flag and replacing the default profile floats.
  That is enough for the durable label
  `CSIAction_UpdateCurrentTargetServiceAvailability`.
- Caller tracing confirms both methods remain internal CSI leaves rather than
  broader entry points. `14077e580` returned only data references at
  `140b6d610`, `140e374d0`, and unwind metadata, while `14077e770` returned
  only data references at `140b6d600` and `140e374f4`. Together with the
  earlier `14077e2c0` and `14077e3b0` work, that tightens the current-target
  side of the CSI callback family without forcing a speculative owning-class
  name onto the table itself.
- This shifts the remaining ambiguity inward again: the current-object and
  current-target CSI leaves are now behaviorally labeled, so the next useful
  decomp step is either the deeper callback-table family around `140e374xx` or
  another sibling leaf such as `14077e8b0` if tighter CSI-object semantics are
  still needed.
- Verification: direct inspect and caller trace completed successfully for both
  `14077e580` and `14077e770`, and nearby-data dump completed successfully for
  the owning table slot at `140b6d610`.

Fifty-sixth CSI key-handler follow-up implemented from this pass:

- Direct inspect plus caller tracing now resolve `1404d9450 = CSIKey_HandlePress`.
  The helper first gates on several local busy-state flags and an active cast
  record. If the current active object at `param_1 + 0x7188` exposes a positive
  handler through virtual slot `+0x60`, it emits the named client event
  `CSIKeyPressed` with the incoming key-state flag and returns. Otherwise, when
  no stored target id is present at `param_1 + 0x6720`, it routes the stored
  CSI action kind in `param_1 + 0x7f20` through a local switch table and calls
  several specialized helpers for the remembered action payload ids. When a
  stored target id is present, it refreshes stale local targeting through
  `14055b0e0(param_1)` and, on the pressed-key branch, falls back into
  `Interaction_AttemptTargetAction(param_1, storedTargetId)`. That is enough
  for a durable CSI-key handler label without overcommitting to any single
  switch case.
- Supporting inspect of the sibling callback-table leaf `14077e3b0` tightened
  the current-active-object family without justifying a public label yet. The
  helper initializes callback-local state fields, scans the local subject's
  slot-linked records for a flag-driven disable case, and then checks the
  current active target id plus either the special type-`0x14` path or the
  generic interaction descriptor callback pointer before deciding whether the
  initialized state should remain enabled. Together with the earlier
  `14077e2c0` pass, that reinforces the interpretation that both siblings are
  internal current-active-object callback leaves hanging off the table near
  `140b6d540`, not the right durable public boundary for this family.
- This shifts the remaining spell-side ambiguity away from the broader CSI key
  entry point and toward the still-unnamed post-callback helper `1403dd1c0` or,
  if callback-table semantics are needed later, the narrower current-object
  leaf pair `14077e2c0` and `14077e3b0`.
- Verification: direct inspect and caller trace completed successfully for
  `1404d9450`, and the supporting direct inspect plus caller trace completed
  successfully for `14077e3b0`.

Fifty-seventh post-interaction feedback follow-up implemented from this pass:

- Caller tracing now shows `1403dd1c0` has exactly one direct code caller in the
  current program: `Interaction_AttemptTargetAction`. The call sits strictly on
  the successful descriptor-callback path and is only taken when the current
  interaction descriptor requests the extra follow-up through its flag byte at
  `+0xd`. That bounds the helper as post-interaction behavior rather than a
  generic scene or unit-state routine.
- Combined with the earlier direct inspect, that is now enough for
  `1403dd1c0 = Interaction_HandlePostActionTargetFeedback`. The helper resolves
  the current target, derives localized or random-text response data through the
  target's descriptor tables, builds a small payload containing the `"target"`
  key for the text or event path, dispatches the resulting target-facing
  feedback through the local message helpers, and then arms the timed local
  target-state updates and follow-up toggles used after a successful
  interaction callback. The body is still broad internally, so the label stays
  intentionally generic at the feedback-and-state boundary rather than trying to
  guess a narrower UI or dialogue subsystem name.
- This leaves the nearby spell-side ambiguity concentrated in the current-active-
  object callback leaves `14077e2c0` and `14077e3b0` rather than in the broader
  interaction attempt, deferred-action queue, CSI key handler, or post-action
  feedback path.
- Verification: direct inspect and caller trace completed successfully for
  `1403dd1c0`, and the trace returned one code caller plus three data
  references.

Fifty-eighth deferred target-approach follow-up implemented from this pass:

- Direct inspect plus caller tracing now resolve `140559250 = DeferredActionQueue_ArmTargetApproach`.
  The helper clears the shared deferred-action queue block, resets any attached
  callback object through the queue slot at `+0x90`, rejects a null target, then
  stores mode `1`, the supplied target id, and a fixed range of `1.5` before
  immediately handing off to `DeferredActionQueue_UpdateAndDispatch`. That is a
  bounded queued target-approach behavior rather than another general dispatch
  helper.
- Caller tracing also tightens the remaining current-target callback family.
  `140559250` currently has exactly one direct code caller in the program:
  `14077e580`. Direct inspect of that leaf shows it only runs on a narrow
  current-target path, rejects invalid or self targets, arms the deferred queue
  for the resolved current target, and then builds a localized target-name
  message before dispatch. That is useful evidence for the family, but still not
  enough to force a stable public label onto `14077e580` itself.
- This keeps the unresolved spell-side edge confined to the callback-table leaf
  family around `140b6d540`, while the deferred queue beneath it is now labeled
  through both the target-approach and interaction-action arms.
- Verification: direct inspect and caller trace completed successfully for
  `140559250`, and the trace returned one code caller plus one data reference.

Sixtieth current-object CSI callback follow-up implemented from this pass:

- The callback-table leaf family around `140b6d540` is now bounded tightly
  enough to name the two core current-object methods. Existing table dumps show
  `14077e3b0` and `14077e2c0` are direct members of the same local method table,
  while fresh adjacent method inspection on `1405c8600` and `1405c9590` shows
  that table behaves like a CSI action object rather than an unrelated helper
  cluster: the neighboring methods resolve prompt text, refresh local action
  flags, and either dispatch a stored spell or forward the press into the active
  CSI object.
- Direct inspect of `14077e2c0` is now enough for
  `CSIAction_HandleCurrentObjectPress`. The method only acts on the pressed-key
  branch (`param_2 == 0`), resolves the active object id at
  `*(DAT_140c65898 + 0x6490) + 0x108`, takes a narrow special-case type-`0x14`
  path through `140397ce0(DAT_140c65898)` when the extra gates pass, and
  otherwise falls back to `Interaction_AttemptTargetAction` for that active
  object id.
- Direct inspect of `14077e3b0` is likewise now enough for
  `CSIAction_UpdateCurrentObjectAvailability`. The method resets the local CSI
  action state block, scans the local subject's slot-linked records for a
  flag-driven disable case, then checks the current active object id plus either
  the special type-`0x14` service path or the generic interaction descriptor
  callback pointer before deciding whether the action remains enabled.
- The nearby leaf `14077e580` still stays intentionally unnamed. It now has a
  clearer role as a current-target branch that arms
  `DeferredActionQueue_ArmTargetApproach` and dispatches a localized target-name
  message, but that is still not a stable enough public boundary to force a
  durable label without better table-family context.
- This moves the remaining spell-side ambiguity inward again: the broader
  interaction attempt, deferred queue, CSI key handler, post-action feedback,
  and the two main current-object CSI leaves are now labeled, leaving only the
  narrower sibling leaf methods such as `14077e580` or deeper table-family
  semantics if tighter CSI object naming is still needed.
- Verification: existing direct inspect and caller trace evidence for
  `14077e2c0` and `14077e3b0` was combined with new direct inspect evidence for
  adjacent table methods `1405c8600` and `1405c9590`.

Fifty-ninth option/combat-log request follow-up implemented from this pass:

- The client option and combat-log preference request cluster is now mapped and
  handled. Registration evidence in the world-message table ties
  `0x00D5 ClientCombatOptions` to writer `14008a450`, registered object size
  `0x0c`; `0x012B ClientOptions` to the existing two-uint writer `14007ec70`,
  registered object size `0x08`; and both `0x0248
  ClientCombatLogDisableOthers` and `0x0249 ClientCombatLogDisables` to the
  existing one-uint writer `14007d010`, registered object size `0x04`.
- Direct inspection of `14008a450` proves the custom combat-options packet
  layout: it writes the first dword as 4 bits, the second dword as one bit, and
  the third dword as 14 bits. That matches
  `ClientCombatOptions.CastingOptions`, `DisableOtherPlayersLogging`, and
  `CombatLogDisableFlags` in the source model. The existing `14007ec70` label
  comment now records its `ClientOptions` use as option type/value, while the
  existing `14007d010` label comment now records both combat-log one-uint
  update packets.
- One sender is now named: `1403f7480 =
  Options_SendClientOptionsCasting`. Direct inspect shows it mutates the local
  casting option mask at `DAT_140c65898 + 0x7ba0`, sends opcode `0x012B` with
  option type `0` (`OptionType.Casting`) and the current option mask as the
  value, then dispatches the `OpenOptions` client event when the surrounding
  UI event target exists. Exported labels appear in `selected_decompiled.c` at
  `ClientCombatOptions_WritePayload` (`:843`) and
  `Options_SendClientOptionsCasting` (`:10896`).
- Source now adds handlers for all four missing option models. The handlers
  validate known `CastingOptionFlags`, known `CombatLogOptions`, and the
  `OptionType.SharedChallenge` boolean shape before logging the structured
  setting values. They intentionally do not persist new state: NexusForever's
  current option persistence covers keybindings/InputKeySet, but there is no
  durable character/account store yet for combat-log masks, casting options, or
  shared-challenge preference.
- Remaining bounded uncertainty: the packet shapes and one casting-option sender
  are mapped, but persistent option storage and any server-side filtering based
  on combat-log preferences remain absent. If those preferences need to affect
  outbound combat-log delivery later, the next implementation step is a small
  account/character option store rather than more packet parsing.
- Verification: `dotnet build Source\NexusForever.sln` succeeds with the
  existing package warnings and `0 Error(s)`. The WildStar64 export applied the
  expanded label set (`applied=259, created=0, skipped=0, missing=0`) and
  rendered `selected_decompiled.c` with 420 functions; `functions.csv` confirms
  `ClientCombatOptions_WritePayload` and `Options_SendClientOptionsCasting`.

Sixtieth set-busy interaction follow-up implemented from this pass:

- Existing interaction evidence already bounded busy state as a front-door gate
  rather than a purely visual latch. The earlier direct inspect of
  `CSIKey_HandlePress` (`1404d9450`) showed it checks local busy-state flags
  before dispatching `CSIKeyPressed` or falling back to
  `Interaction_AttemptTargetAction`, and the activate-unit packet family is
  already closed around the direct `0x0097` and cast-backed `0x0098` request
  shapes.
- That is strong enough for a conservative server boundary. Source now rejects
  busy targets in `ClientActivateUnit`, `ClientActivateUnitCast`, and
  `ClientEntityInteract` before invoking the activation callback, sends
  `GenericError.TargetBusy`, and routes the failure through `OnActivateFail`.
- The same pass upgrades busy state from server-only bookkeeping to visible
  runtime state. `SetBusy` transitions now reuse `ServerUnitInUse`, and players
  who newly gain visibility on a busy unit receive the current in-use state on
  entry instead of waiting for a later toggle.
- The server-safe implementation boundary remains intentionally narrower than
  full CSI/path/object parity: direct activation and generic entity-interaction
  ingress are gated, while deeper deferred-action, current-active-object, and
  non-zero context semantics remain open pending sniff or stronger client proof.
- Verification: editor diagnostics were clean on the touched runtime files, and
  `dotnet build Source\NexusForever.WorldServer\NexusForever.WorldServer.csproj --no-restore -m:1`
  is the validation target for this pass because it covers the changed game and
  world-server surfaces without the unrelated static-web-assets failure path.

Sixty-first SapVital/vital-modifier follow-up implemented from this pass:

- `Decomp\Analysis\scripts\ExportNexusForeverAnalysis.java` now treats
  `sapvital`, `vitalmodifier`, `combatlogvitalmodifier`, and
  `cmbtlog.disablevitalmodifier` as high-value string anchors. The refreshed
  per-target WildStar64 export now keeps `SapVital`, `VitalModifier`, and the
  combat-log toggle string in `interesting_strings.csv` instead of dropping them
  out of the focused export set.
- The strongest new client anchor is now durable in
  `function_labels.csv`: `14060f170 = CombatLog_DispatchVitalModifierEvent`.
  Direct decompile evidence at `exports\WildStar64.exe\selected_decompiled.c`
  shows it building the `CombatLogVitalModifier` named-event payload, writing
  `nAmount`, resolving `bShowCombatLog`, and dispatching the event through the
  existing `ClientEvent_DispatchNamedEvent` sink.
- Targeted `DumpNearbyData.java` output on the raw `SapVital` string at
  `140abb710` now places it inside a dense contiguous spell-effect name block
  with neighboring entries such as `ActionBarSet`, `UnlockActionBar`,
  `SummonCreature`, `Scale`, `Resurrect`, `Disguise`, `ForceFacing`,
  `Absorption`, `CCStateBreak`, `ForcedAction`, and `ProxyChannel`. That is
  strong registration-table evidence even though the focused export still does
  not surface a direct gameplay-code xref for the `SapVital` string itself.
- This improves the SapVital evidence ladder without overstating coverage.
  `VitalModifier` is still visible in the Lua spell-registration path, and the
  combat-log dispatch path is now mapped, but the focused export still does not
  surface a direct gameplay-code xref for the `SapVital` enum string itself.
  Server implementation therefore stays conservative around unsupported alias
  vitals and unresolved client-side secondary semantics.
- Source follow-up from the same pass keeps the server runtime aligned with the
  client evidence: `TryModifyVital` now preserves the spell source for health
  changes, and health-vital drains only reuse the effect-level damage type when
  it is an explicit physical/tech/magic/fall/suffocate value. The mapped SapVital
  fixtures used for validation still commonly carry `damage_type = 0`, so the
  fallback remains physical for zero or unknown rows.

Sixty-second SapVital enum-registration follow-up implemented from this pass:

- `InspectCodeAddress.java` on `FUN_1400f06f0` shows a small generic helper
  that resolves a key string from its third argument, writes the supplied
  integer value, and inserts the pair into the active Lua or named-event table.
  After re-exporting the label set, the helper is now durably named
  `Lua_InsertStringIntPair` in `function_labels.csv` and `selected_decompiled.c`.
- In the already-labeled `Lua_RegisterGameSpellBindings` path, the
  `CodeEnumSpellEffectType` registration block calls this helper in a 0x96-entry
  loop. The refreshed decompile also shows `CombatLog_DispatchVitalModifierEvent`
  using the same helper for named payload fields such as `nAmount`, which proves
  the helper is generic table/object plumbing rather than spell-effect-specific
  logic. Combined with the `DumpNearbyData.java` output around `140abb710`, this
  is stronger evidence that the contiguous `SapVital` string block belongs to
  the client spell-effect enum registration surface even though a dedicated
  gameplay consumer for the `SapVital` entry remains unmapped.

Sixty-third friendship request group follow-up implemented from this pass:

- The remaining name/preference friendship request cluster is now mapped from
  the native world-message registration table. Opcode `0x00B8
  ClientFriendshipBlock` and `0x039E ClientFriendshipIgnoreStrangersState` both
  reuse `ClientBool_WritePayload` with registered 4-byte objects. Opcode
  `0x03B7 ClientFriendshipSetAutoResponseMessage` registers
  `ClientFriendshipSetAutoResponseMessage_WritePayload` with a 0x10-byte object;
  opcode `0x03C2 ClientFriendshipRemoveByName` registers
  `ClientFriendshipRemoveByName_WritePayload` with a 0x18-byte object; and
  opcode `0x03C8 ClientFriendshipSetNoteByName` registers
  `ClientFriendshipSetNoteByName_WritePayload` with a 0x18-byte object.
- Direct writer inspection proves the packet layouts already modeled in
  NexusForever: auto-response writes away and busy wide strings; remove-by-name
  writes name and realm wide strings followed by a 4-bit `FriendshipType`; and
  note-by-name writes name, realm, and note wide strings. The shared helpers are
  now named `NetworkBitWriter_WriteWideString` and
  `NetworkBitWriter_Write4BitValue` because those exact helper calls explain the
  friendship packet field shapes.
- The Lua and send-side anchors also line up with the source behavior.
  `Lua_FriendshipLib_SetAutoResponseMessages` reads two Lua strings and forwards
  them into `Friendship_SetAutoResponseMessagesAndSend`, which validates the
  strings, dispatches `FriendshipResult_InvalidAutoResponse` on failure, and
  sends opcode `0x03B7` when the friendship service is loaded.
  `Lua_FriendshipLib_SetPersonalIgnoreStrangersState` updates the local
  ignore-strangers bit and sends opcode `0x039E` on state changes, while
  `Friendship_SendClientFriendshipBlock` builds opcode `0x00B8` from the
  requested or toggled account friend-request block bit.
- Source now handles the full parsed cluster. `ClientFriendshipBlock` persists
  the account friend-request block flag through a new internal
  `FriendshipAccountBlockUpdateMessage` and republishes personal status.
  `ClientFriendshipRemoveByName` and `ClientFriendshipSetNoteByName` now resolve
  same-realm or explicit-realm names in the friendship server through new
  internal messages and handlers, then reuse the existing friend remove/note
  update semantics and friendship result publisher.
  `ClientFriendshipIgnoreStrangersState` updates the client with
  `ServerFriendshipIgnoreStrangersState` flags but remains transient because no
  server-side persistence model exists yet. `ClientFriendshipSetAutoResponseMessage`
  is parsed and logged with message lengths, but remains diagnostic-only because
  there is still no account/chat storage or auto-response delivery path in
  NexusForever.
- Verification: `dotnet build Source\NexusForever.sln` succeeds after stopping
  local NexusForever server processes that had locked build outputs; only the
  existing NuGet/package warnings remain. The refreshed WildStar64 export
  applied the expanded label set (`applied=276, created=0, skipped=0,
  missing=0`) and rendered `selected_decompiled.c` with 430 functions. The
  generated `functions.csv` confirms all new friendship writer, sender, Lua,
  and bit-writer labels.

Sixty-fourth ICComm request group follow-up implemented from this pass:

- The remaining client-to-world ICComm request cluster is now mapped from the
  native world-message registration table. Opcode `0x0546
  ClientICCommChannelJoin` registers `ClientICCommChannelJoin_WritePayload`
  at `1400877a0` with a 0x18-byte object. Opcode `0x0549
  ClientICCommChannelNotJoined` reuses `ClientP2PTradingUInt64_WritePayload`
  at `14007d840` with an 8-byte object. Opcode `0x054B ClientICCommMessage`
  registers `ClientICCommMessage_WritePayload` at `1400875e0` with a
  0x20-byte object.
- Direct writer inspection confirms the packet models already present in
  NexusForever. `ClientICCommChannelJoin_WritePayload` writes a 3-bit
  `ICCommChannelType`, a 64-bit guild id, and a wide channel name.
  `ClientICCommMessage_WritePayload` writes a 64-bit channel id, a 32-bit
  message id, an ASCII message string through the shared string writer, and a
  wide recipient name. The shared `ClientP2PTradingUInt64_WritePayload` path is
  now documented as the single-ulong writer also used by
  `ClientICCommChannelNotJoined`.
- Source now handles the full parsed client ICComm cluster in WorldServer.
  `ClientICCommChannelJoin` validates the 3-bit channel type and returns
  `ServerICCommChannelJoinResult` with `MissingEntitlement`, while
  `ClientICCommMessage` returns `ServerICCommMessageResult` with
  `InvalidText` for blank text or `NotInChannel` otherwise. The client
  not-joined notice is parsed and logged. This keeps client-visible failures
  deterministic without inventing ICComm channel storage, membership, message
  ordering, or routing semantics that NexusForever does not yet have.
- Verification: the refreshed WildStar64 export applied the expanded label set
  (`applied=278, created=0, skipped=0, missing=0`) and rendered
  `selected_decompiled.c` with 430 functions. The generated `functions.csv`
  confirms `ClientICCommMessage_WritePayload` and
  `ClientICCommChannelJoin_WritePayload`, and `selected_decompiled.c` includes
  them at lines 689 and 760. `dotnet build
  Source\NexusForever.WorldServer\NexusForever.WorldServer.csproj --no-restore
  -m:1 -p:BaseOutputPath=I:/GIT/NexusForever/.nexusforever-runtime/build-iccomm/`
  succeeds with only the existing `Spline.formation` warning; the alternate
  output path avoids interfering with the locally running world-server process
  that has the normal debug outputs locked.

Sixty-fifth duel/PvP state follow-up implemented from this pass:

- The previously mapped duel/PvP client request cluster now has a bounded
  runtime state implementation instead of diagnostic-only handlers. The evidence
  remains the labelled request surface from the fiftieth pass:
  `ClientDuelInitiate`, `ClientDuelAccept`, `ClientDuelDecline`,
  `ClientDuelForfeit`, `ClientSetIgnoreDuelRequests`, and
  `ClientPvpToggleFlags` are still driven by the zero-payload and one-bit bool
  request shapes at `selected_decompiled.c:11`, `:275`, `:282`, and the
  `GameLib` sender guards around `selected_decompiled.c:20414`.
- Source now adds `IDuelManager`/`DuelManager` as a transient world-runtime
  duel boundary. It tracks one pending/countdown/active duel per participant,
  sends `ServerDuelChallenge`, `ServerDuelCountdown`, `ServerDuelStart`, and
  `ServerDuelResult`, expires unanswered challenges, rejects busy/dead/in-combat
  participants through the already mapped `DuelFailureReason` values, and
  handles decline, forfeit, cancellation, and defeated outcomes. Active duels
  now also open the player-vs-player attack gate without making non-duel players
  attackable.
- `ClientPvpToggleFlags` now updates a player-local `PvPFlag`, broadcasts
  `ServerUnitPvpStateChange`, and clears the client cooldown on immediate
  disable. This intentionally remains a simple session/runtime flag: persisted
  PvP preference, cooldown countdown semantics, forced-map PvP, and wider PvP
  combat rules are still blocked pending stronger client/server state evidence.
- Remaining bounded uncertainty: the local client request and basic response
  flow is implemented, but observer-wide duel broadcasts, duel-area leash and
  leave warnings, exact countdown duration, PvP cooldown persistence, cross-map
  cleanup rules, duel-specific reward/stat side effects, and forced PvP realm
  behavior remain open. The new guide row calls this out so future passes can
  target those exact state gaps instead of re-mapping the request packets.
- Verification: `dotnet build
  Source\NexusForever.WorldServer\NexusForever.WorldServer.csproj --no-restore
  -m:1 -p:BaseOutputPath=I:/GIT/NexusForever/.nexusforever-runtime/build-duel-pvp/`
  succeeds with the existing `Spline.formation` warning and `0 Error(s)`.

Sixty-sixth P2P trading state follow-up implemented from this pass:

- The previously mapped P2P trading client request group now has a bounded
  runtime state implementation instead of diagnostic-only handlers. This builds
  directly on the forty-third pass packet evidence: the zero-payload accept,
  cancel, commit, and decline requests, the raw target-unit initiate request,
  and the shared 64-bit add-item/remove-item/set-money writer remain the mapped
  client request surface.
- Source now adds `ITradeManager`/`TradeManager` as the world-runtime trade
  boundary. It tracks one pending or active trade per participant, sends
  `ServerP2PTradeInvite`, handles accept/decline/cancel, expires unanswered
  invites, emits item/money offer updates, resets commit state when offers
  change, and sends initiator/target committed or uncommitted results.
- Settlement is implemented through the existing safe NexusForever item and
  currency APIs. Before mutation, the manager revalidates offered inventory
  items, credit affordability, recipient inventory capacity after both sides'
  outgoing items are removed, and known credit caps after outgoing money is
  debited. It then removes both sides' outgoing items, subtracts both credit
  offers, adds received items with `ItemUpdateReason.Trade`, adds received
  credits, and finishes with `FinishedSuccess`; empty double-commit returns
  `NothingToTrade`.
- Remaining bounded uncertainty: the client packet shapes and server mutation
  path are implemented, but exact item eligibility rules, trade-lock behavior
  against concurrent inventory mutation, account/character money-trade limits,
  the unknown fields in `ServerP2PTradeUpdateItem`, and exact
  `ServerP2PTradeResult.Cancelled` UI semantics still need sniff or runtime UI
  verification. The implementation therefore rejects non-inventory items and
  equippable bags conservatively and does not add new labels in this pass.
- Verification: `dotnet build
  Source\NexusForever.WorldServer\NexusForever.WorldServer.csproj --no-restore
  -m:1 -p:BaseOutputPath=I:/GIT/NexusForever/.nexusforever-runtime/build-trade-state/`
  succeeds with the existing `Spline.formation` warning and `0 Error(s)`.

Sixty-seventh ICComm routing follow-up implemented from this pass:

- The previously mapped ICComm client request group now has a bounded runtime
  implementation instead of deterministic client-visible failures. This builds
  directly on the sixty-fourth pass evidence: `ClientICCommChannelJoin` sends a
  3-bit `ICCommChannelType`, a 64-bit guild id, and a wide channel name;
  `ClientICCommMessage` sends a 64-bit channel id, a 32-bit message id, an
  ASCII message string, and a wide recipient name; and
  `ClientICCommChannelNotJoined` reuses the shared 64-bit writer.
- Source now adds `IICCommManager`/`ICCommManager` as an in-memory ICComm
  channel boundary. It creates or reuses transient channels keyed by global
  name, group association id, or guild id; validates no-group/no-guild/bad-name
  join cases; sends `ServerICCommChannelJoin` on success; and uses
  `ServerICCommChannelJoinResult` for known failure results.
- `ClientICCommMessage` now routes through the manager. Blank messages still
  return `InvalidText`, non-members still return `NotInChannel`, successful
  sends get `ServerICCommMessageResult.Sent`, the sender receives
  `ServerICCommOrderedMessage` with the client message id, and joined recipients
  receive `ServerICCommDirectedMessage` with sender name and message text.
  `ClientICCommChannelNotJoined` removes the player from the server-side
  membership view, and the manager's world tick clears stale members that have
  left world.
- Remaining bounded uncertainty: the client packet shapes and a safe transient
  routing boundary are implemented, but entitlement checks, throttle windows,
  exact ordered-vs-directed packet selection, persistent channel membership,
  explicit leave/logout UI packets, and guild/group lifecycle subscription
  semantics still require stronger client or sniff evidence. No new labels were
  needed for this pass.
- Verification: `dotnet build
  Source\NexusForever.WorldServer\NexusForever.WorldServer.csproj --no-restore
  -m:1 -p:BaseOutputPath=I:/GIT/NexusForever/.nexusforever-runtime/build-iccomm-routing/`
  succeeds with the existing `Spline.formation` warning and `0 Error(s)`.

Sixty-eighth multi-lane blocker review from this pass:

- Parallel focused reviews rechecked three tempting follow-up areas without
  adding speculative server state. No new function labels were needed.
- Rapid transport: `ClientRapidTransport_WritePayload` (`14007ab80`) still
  proves the wire order as a 14-bit taxi node followed by one raw 32-bit field,
  and `RapidTransport_SendClientRapidTransport` (`1403993c0`) still copies that
  second field from resolved cast context `+0x60` before sending opcode
  `0x0141`. Sibling spell/crafting sender correlation supports the current
  `ContextToken` source naming. It remains unsafe to validate, require
  monotonicity, echo, or charge/teleport from this value until its lifecycle or
  server-side consumer is observed.
- LAS/action set: `ActionSet_CheckUpdateSpellInProgress` (`1403bb8d0`) lazily
  resolves config id `0x41e`, caches the resolved key, and walks the local
  player-side pending update list before the client sends
  `ClientRequestActionSetChanges`. NexusForever currently mutates LAS requests
  synchronously through `ClientRequestActionSetChangesHandler`, so there is no
  matching server transaction list or begin/end spell-update lifecycle. A
  synthetic boolean gate would risk rejecting valid sequential requests.
- Support/report/survey: the mapped packet models and diagnostic handlers
  already cover the client request fields for incident reports, support tickets,
  bug reports, stuck requests, suggestions, and survey submits. Real persistence
  remains a backing-service design task because no schema, moderation lifecycle,
  category-to-queue routing, raw-text retention policy, or staff workflow is
  defined by the client evidence.
- Result: mapped-only/blocker update. The guide now treats rapid transport's
  second field as read-only `ContextToken` evidence rather than an unmapped
  `Time` target, keeps `UpdateSpellInProgress` blocked pending a real
  asynchronous spell-update transaction model, and keeps support persistence
  blocked pending an explicit support-case backend.
- Verification: `dotnet build
  Source\NexusForever.WorldServer\NexusForever.WorldServer.csproj --no-restore
  -m:1 -p:BaseOutputPath=I:/GIT/NexusForever/.nexusforever-runtime/build-continuation/`
  succeeds with the existing `Spline.formation` warning and `0 Error(s)`.

Sixty-ninth CC apply-rules enum and combat-log label follow-up mapped from this
pass:

- The WildStar combat-log event helpers around the existing
  `CombatLog_WriteCasterContext` anchor now have durable function labels in
  `function_labels.csv`: `14060b750 = CombatLog_DispatchCCStateEvent`,
  `14060bbc0 = CombatLog_DispatchCCStateBreakEvent`, and
  `14060e210 = CombatLog_DispatchModifyInterruptArmorEvent`. The decompile shape
  matches the previously labeled `CombatLog_DispatchVitalModifierEvent`: each
  helper builds a named-event payload object and dispatches it through
  `ClientEvent_DispatchNamedEvent`.
- `CombatLog_DispatchCCStateEvent` is the most useful new anchor for the
  pending interrupt-armor and CC apply-rules work. The decompile shows it
  reusing `CombatLog_WriteCasterContext`, writing payload-backed `eState`, then
  carrying additional CC-state fields plus client-local display keys such as
  `strState`, `strTriggerCapCategory`, and `bHideFloater` before dispatching
  `CombatLogCCState`. The two intermediate integer writes remain bounded but not
  fully recovered from the current export, so they stay mapping-only.
- Separate enum-registration evidence now pins the client-facing
  `CCStateApplyRulesResult` names without forcing speculative runtime behavior.
  The registration helper block at
  `exports\WildStar64.exe\selected_decompiled.c:43128-43192` includes
  `CodeEnumCCStateApplyRulesResult` values `InvalidCCState`,
  `NoTargetSpecified`, `Target_Immune`, `Target_InfiniteInterruptArmor`,
  `Target_InterruptArmorReduced`, and `Target_InterruptArmorBlocked`, which line
  up with the existing server enum surface.
- No server gameplay behavior changed in this pass. NexusForever still keeps
  `CCStateSet` conservative and `ModifyInterruptArmor.DataBits01`
  diagnostic-only because the client evidence does not yet show when interrupt
  armor should be consumed, when it should fully block a CC application, or how
  those results interact with breakout and apply-rules payload fields.
- Source-side inspection output now uses the new stable handler names for
  `CCStateSet`, `CCStateBreak`, and `ModifyInterruptArmor` so `/spell inspect4`
  and related diagnostics point back at the labeled client anchors directly.

Seventieth interrupt-armor threshold clamp follow-up implemented from this
pass:

- `HandleEffectModifyInterruptArmor` no longer adds raw unsigned interrupt armor
  directly up to `uint.MaxValue`. The handler now reuses the existing bounded
  vital path via `TryModifyVital(Vital.InterruptArmor, ...)`, which means the
  applied amount respects the target's current interrupt-armor max from
  `Property.InterruptArmorThreshold` and still flows through the existing stat
  update path.
- This keeps the family aligned with the already mapped entity model: interrupt
  armor is exposed as a normal vital in `UnitEntity.TryGetVitalMax` /
  `TryModifyVital`, while the combat-log payload already carries the applied
  amount rather than the requested amount. High-amount fixtures such as `79320`
  and `81579` can now safely hit the local cap instead of overfilling past the
  target's bounded interrupt-armor surface.
- The implementation remains intentionally conservative. `DataBits01` is still
  diagnostic-only as the likely consume/remove-on-interrupt flag, and
  `CCStateSet` still does not mutate apply-rules results or consume interrupt
  armor during CC application.
- Verification for this follow-up: `dotnet build
  Source\NexusForever.Game\NexusForever.Game.csproj --no-restore
  -p:BaseOutputPath=I:/GIT/NexusForever/.nexusforever-runtime/build-ia-clamp-game/`
  succeeds with only the existing `Spline.formation` warning. Full-solution
  verification remains temporarily blocked while the locally running
  `NexusForever.WorldServer`/`NexusForever.AuthServer` processes hold files open
  in their normal `bin\Debug` output trees.

Seventy-first in-game store purchase follow-up implemented from this pass:

- The storefront purchase client payloads now have durable native labels:
  `Storefront_RequestCatalog_WritePayload` (`14007a610`),
  `Storefront_PurchaseCharacter_WritePayload` (`1400acf80`), and
  `Storefront_PurchaseAccount_WritePayload` (`140080a20`), with helper labels
  for `NetworkBitWriter_WriteRaw32` (`14006bd80`) and
  `NetworkBitWriter_WriteIdentity` (`140085170`). Re-export confirmed all five
  labels in `exports\WildStar64.exe\functions.csv` and
  `selected_decompiled.c`.
- `ClientStorefrontRequestCatalog` was already correct as one 14-bit field.
  The character purchase model was corrected from the stale 20-bit layout to
  the mapped shared purchase payload: 32-bit offer id, 5-bit selector, 32-bit
  field, 14-bit currency id, 32-bit field, target identity, and trailing 32-bit
  field. `ClientStorefrontPurchaseAccount` was added as the same shared payload
  followed by one 32-bit field, one identity, and a wide string.
- `ClientStorefrontPurchaseCharacter` and `ClientStorefrontPurchaseAccount`
  handlers now validate the requested offer, currency, price, local target, and
  grantability before subtracting account currency. The implemented grant paths
  use existing NexusForever systems only: account currency grants, account or
  character entitlements based on the entitlement flags, and account generic
  unlock sets.
- The implementation intentionally rejects unmapped account-inventory, gift, and
  unsupported offer-item paths without charging the player. `ServerAccountItemAdd`
  and account item persistence remain blocked until the native result/add-item
  packet shape and server-side inventory semantics are mapped.
- Verification: `dotnet build
  Source\NexusForever.WorldServer\NexusForever.WorldServer.csproj --no-restore
  -m:1 -p:BaseOutputPath=I:/GIT/NexusForever/.nexusforever-runtime/build-storefront/`
  succeeds with `0 Warning(s)` and `0 Error(s)`. The Ghidra export refresh
  succeeded with the storefront labels applied.

Seventy-second account inventory follow-up implemented from this pass:

- Native receive labels now cover the account-inventory wire shapes:
  `ServerAccountItems_ReadPayload` (`140080630`) reads opcode `0x096D` as a
  32-bit count followed by 0x28-byte account item records;
  `AccountInventoryItem_ReadPayload` (`1400a9c20`) reads a 64-bit inventory id,
  32-bit `AccountItem` id, 5-bit claim state, one flag bit, and target
  identity; `ServerAccountItemCooldownSet_ReadPayload` (`14007a040`) reads
  opcode `0x0974` as two 32-bit fields; and
  `ServerAccountItemsPending_ReadPayload` (`140080510`) /
  `PendingAccountItemGroup_ReadPayload` (`1400a9b20`) map the pending group
  list enough to keep it blocked rather than guessed.
- `AccountItemLib:TakeAccountItem` resolves to
  `AccountItem_SendClientAccountItemTake` (`140006d00`), which sends opcode
  `0x0839` with one raw 64-bit account inventory id after local list
  validation. `AccountItemLib:ClaimPendingItemGroup` resolves to
  `AccountItem_SendClientClaimPendingItemGroup` (`140006ba0`), which sends
  opcode `0x0233` with one wide string group key. The shared single-u64 and
  wide-string writer labels were updated to include these account-item uses.
- `CodeEnumClaimItemState` registers `CanClaim = 0`, `CharacterMaxed = 1`,
  `AccountMaxed = 2`, and `AccountMaxedWithPending = 3`; NexusForever now has
  `AccountItemClaimState` and uses it for the 5-bit account inventory field.
- NexusForever now persists account inventory in `account_inventory`, loads it
  with the auth account model, sends `ServerAccountItems` on world/catalog
  entry, sends `ServerAccountItemAdd` for new purchases, and handles
  `ClientAccountItemTake` by validating target identity and applying supported
  `AccountItem` grants through existing item, account-currency, entitlement,
  and generic-unlock systems before removing the inventory row.
- Storefront purchases now place purchased `AccountItem` entries into account
  inventory instead of granting rewards immediately. Gifting, pending item
  groups, cooldown mutation, exact purchase-result UI packets, and unsupported
  account-item paths such as unmapped instant events remain blocked.
- Verification: Ghidra export refresh with `-MaxDecompiledFunctions 430`
  applied and selected all new account-item labels. `dotnet build
  Source\NexusForever.WorldServer\NexusForever.WorldServer.csproj --no-restore
  -m:1 -p:BaseOutputPath=I:/GIT/NexusForever/.nexusforever-runtime/build-account-inventory/`
  succeeds with only the existing `Spline.formation` warning.

Seventy-third account inventory blocked-path follow-up implemented from this pass:

- Native labels now cover the UI callbacks and extra pending-group senders
  behind the previously rejected account-item paths:
  `AccountItemUi_ClaimSelectedPendingItemGroup` (`140518b70`) sends the mapped
  claim request and raises `AccountPendingItemsClaimed`;
  `AccountItemUi_TakeSelectedAccountItem` (`140518be0`) sends the mapped take
  request and raises `AccountPendingItemTook`;
  `AccountItemUi_GiftSelectedPendingItemGroup` (`140519260`) dispatches one of
  two gift request payloads and raises `AccountPendingItemsGifted`; and
  `AccountItemUi_ReturnSelectedPendingItemGroup` (`140519440`) sends a return
  request and raises `AccountPendingItemsReturned`.
- `AccountItem_SendClientReturnPendingItemGroup` (`140006c50`) is now mapped as
  opcode `0x07C6` with the same single pending-group wide string shape used by
  the claim request. The gift helpers remain mapped only at the boundary:
  `AccountItem_SendClientGiftPendingItemGroupIdentityPayload` (`140006d60`)
  sends opcode `0x03F2`, and
  `AccountItem_SendClientGiftPendingItemGroupAlternatePayload` (`140006e50`)
  sends opcode `0x03F1`, but their exact semantic fields are still blocked.
- `StorefrontLib_PurchaseOffer` (`1404f1150`) confirms the Lua-facing purchase
  method routes normal purchases separately from recipient/extra-target flows.
  No server purchase-result UI packet was mapped from the result strings yet.
- NexusForever now names and parses `ClientAccountItemReturnPendingItemGroup`
  as opcode `0x07C6`, rejects it with a generic error like pending-group claim,
  and resends the authoritative empty pending list. The `ServerAccountItemsPending`
  model was corrected from unknown scalar fields to the mapped pending-group
  identity layout, but the server only emits an empty list until pending group
  storage, gift semantics, and non-empty source/target identity meanings are
  verified.
- Account-item cooldown mutation remains blocked: the native receive packet is
  mapped, but current generated static data only exposes
  `AccountItemCooldownGroupEntry.Id`, with no verified duration source to
  enforce or persist.
- Verification: Ghidra export refresh with `-MaxDecompiledFunctions 430`
  applied 302 WildStar64 labels and selected the new account-item callbacks.
  `dotnet build
  Source\NexusForever.WorldServer\NexusForever.WorldServer.csproj --no-restore
  -m:1 -p:BaseOutputPath=I:/GIT/NexusForever/.nexusforever-runtime/build-account-inventory-blocked/`
  succeeds with only the existing `Spline.formation` warning.

Seventy-fourth quest-log/GalacticArchive follow-up mapped from this pass:

- `ShowQuestLog` needed xref triage rather than another broad helper label.
  The earlier `1401074d0` site still decompiles as a large path/UI helper and
  remains intentionally unlabeled, but direct inspect of `14042e3b0` is stable
  enough for `QuestLog_DispatchShowQuestLogEvent`: it emits
  `ClientEvent_DispatchNamedEvent(..., "ShowQuestLog", ..., uVar2)` from a
  tighter event path, making it the current durable quest-log-open anchor.
- `Communicator_ShowQuestMsg` also had a false-positive string xref. Native
  inspect now shows `14093ec20` is setup-only
  `Communicator_RegisterSocietyChatChannels`, repeatedly registering
  `chat.society00` through `chat.society19` and never emitting a quest popup.
  The actual runtime branch is `Communicator_HandleQuestOrSpamMessage`
  (`14043bf30`), which routes spam payloads to `Communicator_ShowSpamMsg`,
  gates quest payloads through local profile/state checks, calls
  `FUN_140437a10(..., 0x3e, ...)`, dispatches `Communicator_ShowQuestMsg`, and
  arms the local follow-up timer fields at `param_1 + 0xfc/+0x100` when the
  global popup latch is clear.
- Quest hand-in/completion evidence is now a tighter end-to-end chain rather
  than isolated anchors. Existing labels still hold: `Dialog_HandleResponseSelection`
  routes `DialogResponseType_QuestComplete` into
  `Dialog_SendClientQuestComplete`, that helper sends opcode `0x035D` with the
  active `{ QuestId, RewardSelection, communicator bit }` tuple,
  `Lua_GameContract_Complete` reuses the same `ClientQuestComplete` payload with
  the communicator bit cleared, and `ClientQuestComplete_WritePayload` still
  proves the packed 15-bit quest id plus 15-bit reward-selection layout.
  Reward refresh remains a separate but adjacent lane:
  `Lua_GameLib_RequestRewardUpdate` still forwards to
  `Reward_SendRewardUpdateRequest`, which throttles low indices and sends opcode
  `0x07CC`.
- The strongest current native surface for user-facing "Codex" behavior is
  Galactic Archive plus datacube content, not a literal `Codex` string.
  `GalacticArchive_ApplyUnlockMaskAndDispatchEvents` (`140499a20`) updates the
  local unlock bitmask and dispatches `GalacticArchiveRefresh`,
  `GalacticArchiveArticleAdded`, and `GalacticArchiveEntryAdded` when new bits
  appear. `GalacticArchive_HandleLinkClick` (`140431a00`) handles `Link`
  payloads with `Archive:`, `Location:`, and `Schematic:` prefixes, routing the
  location branch through the existing location helper, dispatching
  `WorkOrderLocate` for schematic links, and emitting
  `GalacticArchiveLinkClick` for resolved archive entries. The nearby
  `DB\Datacube.tbl`, `DB\DatacubeVolume.tbl`, and
  `DB\PathScientistDatacubeDiscovery.tbl` string xrefs keep
  datacube/archive work as the best continuation lane for deeper Codex
  recovery.
- Verification: `./Decomp/Analysis/run_ghidra_analysis.ps1 -ExportOnly -Targets
  WildStar64.exe -DecompileMode Skip` applied `315` WildStar64 labels with `0`
  missing labels and refreshed the export metadata without widening
  `selected_decompiled.c`.

Seventy-fifth quest-log/datacube continuation follow-up mapped from this pass:

- The broader `ShowQuestLog` family is a compact UI event cluster, not a newly
  exposed quest-state manager. Direct inspect now shows `14042e380` dispatches
  `ToggleQuestLog`, `14042e3b0` remains the stable `ShowQuestLog` dispatcher,
  and `14042e410` dispatches `HideQuestLog` through the same named-event sink.
  `DumpNearbyData.java` around `Event_ShowQuestLog` at `140b00110` tightens the
  same cluster further: the adjacent string block also contains
  `Event_HideQuestLog`, `Event_ToggleCodex`, and `Event_ToggleQuestLog`, so the
  client keeps these quest-log/Codex open-close toggles grouped as neighboring
  event names rather than scattering them across unrelated tables.
- The secondary `ToggleQuestLog` xref at `1404d7a10` is not another quest-log
  state helper. It sits inside a broad input/shortcut action switch that routes
  action kind `5` to `ToggleQuestLog` alongside other shell-style UI actions
  such as options, character, inventory, and CSI helpers. That makes it useful
  as an upstream input bridge, but not specific enough for a stable semantic
  label in this pass.
- The Codex-adjacent data boundary is now explicit in the client DB layer.
  `1401fc000`, `1401fc440`, and `14021fe20` are the lazy registration/load
  paths for `Datacube`, `DatacubeVolume`, and
  `PathScientistDatacubeDiscovery`, respectively. Each function follows the
  same client DB-table registration shape already used elsewhere: allocate a DB
  table wrapper, bind the typed descriptor, and load from the corresponding
  `DB\*.tbl` path or provider-backed cache.
- The client Lua surface for this same lane is now better bounded. `140679e50`
  registers `Game.PathMission` plus `PlayerPathType`, `PathMissionState`, and
  `PathMissionType_*` constants, including the concrete
  `PathMissionType_Scientist_DatacubeDiscovery` enum. `14066aa40` and
  `14066c710` register the `Game.GalacticArchiveArticle` and
  `Game.GalacticArchiveEntry` Lua bindings, respectively; the article path also
  exports `LinkQueryType_All`, `LinkQueryType_Parents`, and
  `LinkQueryType_Children`, while the entry path exports archive entry and entry
  header enums such as `ArchiveEntryEnum_EldanArchive` and
  `ArchiveEntryHeaderEnum_TextWithPortrait` / `TextWithIcon`.
- One concrete runtime-adjacent article method is now decoded cleanly enough to
  keep: `14066a490` compares two `Game.GalacticArchiveArticle` userdata values
  by the resolved underlying article object id and returns a boolean result.
  A neighboring `Game.GalacticArchiveEntry` boolean method at `14066d0c0` also
  clearly consults live archive state through the resolved entry object, but the
  exact predicate semantics are still too ambiguous for a stable label, so it
  remains intentionally unnamed.
- Net result for the user-facing "Codex" lane: this pass strengthens the
  interpretation that Codex behavior in WildStar lives across a combined
  Galactic Archive + datacube + scientist datacube-discovery surface. The
  client now has durable labels for the archive/article entry binding layer and
  the datacube table loaders, but the exact runtime handler behind
  `Event_ToggleCodex` is still unrecovered from current string/data evidence.
- Verification: `./Decomp/Analysis/run_ghidra_analysis.ps1 -ExportOnly -Targets
  WildStar64.exe -DecompileMode Skip` applied `324` WildStar64 labels with `0`
  missing labels and refreshed the export metadata without widening
  `selected_decompiled.c`.

Seventy-sixth Codex toggle/archive-entry follow-up mapped from this pass:

- `Event_ToggleCodex` is no longer blocked. Direct `DumpNearbyData.java`
  against the event table entry at `140c59120` shows a compact `{ event string,
  function pointer }` layout: `140c59120 -> 140b00128 (Event_ToggleCodex)` and
  `140c59128 -> 14042e350`. Direct inspect of `14042e350` then confirms the
  missing runtime leaf: it dispatches `ClientEvent_DispatchNamedEvent(...,
  "ToggleCodex", ...)` through the same UI event sink used by
  `ToggleQuestLog`, `ShowQuestLog`, and `HideQuestLog`.
- That same event table sharpens the surrounding UI cluster further. Nearby
  entries pair `Event_ToggleQuestLog` with `14042e380`,
  `Event_ShowQuestLog` with `14042e3b0`, `Event_HideQuestLog` with
  `14042e410`, and additional adjacent event-name/function pairs continue past
  the quest-log/Codex trio. This is now a documented event-registration table,
  not just an isolated string neighborhood.
- The live `Game.GalacticArchiveEntry` path is now tighter at the helper level.
  `14066dab0` resolves an entry-linked live archive state object by walking one
  current archive lookup tree with the entry metadata key, extracting a second
  id from the resolved metadata object, and then resolving that id through the
  active state tree under `DAT_140c65990`. This is stable enough for a generic
  resolver label even though the exact backing object type is still unnamed.
- One concrete entry accessor is now safe to keep. `14066d280` returns `100`
  when the resolved entry state already satisfies the primary player-state
  predicate, otherwise it delegates to `140499b40` to compute a bounded percent
  completion value from the entry's requirement cluster. `140499b40` itself
  confirms that this is a real progress-style calculation rather than a raw
  flag read: it short-circuits to `100.0` for already-satisfied entries and
  otherwise aggregates requirement progress before scaling to percent.
- Another neighboring entry accessor is now concrete enough to keep.
  `14066d360` resolves the same live entry state object, calls its virtual
  accessor at slot `0x18`, and then wraps the returned pointer through
  `140432f20`, which explicitly materializes a `Game.GalacticArchiveArticle`
  Lua userdata. That is strong evidence that this entry method returns the
  associated archive article object rather than an opaque helper token.
- Two adjacent boolean entry methods remain real but not fully named.
  `14066d0c0` and `14066d1a0` both resolve the same live state object and then
  dispatch to neighboring virtual predicates at slots `0x58` and `0x60`,
  respectively, returning one-bit Lua results. Additional adjacent accessors at
  `14066d430` and `14066d520` gate integer fields at entry offsets `+0x58` and
  `+0x50` behind the `0x58` predicate, but the exact semantic names of those
  state bits and gated fields remain intentionally unresolved.
- Net result: the Codex toggle half of this continuation is now resolved to a
  concrete client event dispatcher, while the archive-entry half has moved from
  a single ambiguous boolean into a documented mini-family: one state-object
  resolver, one progress-percent accessor, one article-wrapper accessor, and two
  still-unnamed player-state predicates.
- Verification: `./Decomp/Analysis/run_ghidra_analysis.ps1 -ExportOnly -Targets
  WildStar64.exe -DecompileMode Skip` applied `328` WildStar64 labels with `0`
  missing labels and refreshed the export metadata without widening
  `selected_decompiled.c`.

Seventy-seventh GalacticArchiveEntry binding-table follow-up mapped from this
pass:

- The `Game.GalacticArchiveEntry` Lua method table is now directly recoverable
  from the registrar. `InspectCodeAddress.java` on `14066c710` shows the
  binding registrar iterating `PTR_DAT_140c5be20`, and `DumpNearbyData.java`
  over that table resolves a clean `{ method name, function pointer }` layout
  for the live entry methods.
- That table converts the previously ambiguous runtime-adjacent accessors into
  authoritative client-authored names. The middle of the block binds
  `GetTitle -> 14066cb20`, `GetText -> 14066cc50`,
  `GetScientistText -> 14066ce50`, `IsUnlocked -> 14066d0c0`,
  `IsViewed -> 14066d1a0`, `GetProgress -> 14066d280`,
  `GetArticle -> 14066d360`, `GetHeaderStyle -> 14066d430`,
  `GetBodyStyle -> 14066d520`, `GetHeaderIcon -> 14066d610`,
  `GetHeaderCreature -> 14066d770`, `GetBodyImage -> 14066d860`, and
  `GetCompletionTitle -> 14066d9c0`.
- This resolves the earlier stop-boundary on the two boolean predicates and the
  gated integer accessors without needing speculative field names. The same
  helpers still decompile as live-state and metadata reads, but the registrar
  proves the user-facing Lua semantics are `IsUnlocked`, `IsViewed`,
  `GetHeaderStyle`, and `GetBodyStyle`, which is the stronger durable label.
- The progress helper also tightens slightly: although `14066d280` computes a
  bounded percentage-like completion value, the bound method name in the client
  surface is simply `GetProgress`, so the durable label now follows the Lua API
  name rather than an inferred suffix.
- Two table slots remain intentionally unnamed. The leading entries at
  `14066a730` and `14066a930` still point through `140b2f000` and `140b2efcc`,
  and those addresses are not currently defined as strings in the listing, so
  this pass leaves them unlabeled rather than guessing at the missing method
  names.
- Verification: `./Decomp/Analysis/run_ghidra_analysis.ps1 -ExportOnly -Targets
  WildStar64.exe -DecompileMode Skip` applied `339` WildStar64 labels with `0`
  missing labels and refreshed the export metadata without widening
  `selected_decompiled.c`.

Seventy-eighth GalacticArchive metatable/article-table follow-up mapped from
this pass:

- The last two leading `Game.GalacticArchiveEntry` table slots are now fully
  resolved rather than behavior-guessed. A new tiny helper,
  `DumpAsciiAtAddress.java`, dumps raw bytes and printable ASCII for undefined
  addresses; running it on `140b2f000` and `140b2efcc` proves the short inline
  strings are literally `__eq` and `__gc`. That promotes `14066a730` to the
  entry `__eq` metamethod and `14066a930` to the entry `__gc` metamethod.
- The same raw-string pass also fixes the neighboring article metatable block.
  `140b2ecf0` and `140b2ee1c` decode to `__eq` and `__gc`, which tightens the
  earlier article equality helper from a generic `Equals` label into the real
  `Game.GalacticArchiveArticle.__eq` Lua metamethod and identifies
  `14066a6b0` as the matching article `__gc` release path.
- With the metatable helpers pinned down, the adjacent
  `Game.GalacticArchiveArticle` method table can now be read directly as another
  `{ name, function }` block. This pass maps `GetTitle -> 14066b300`,
  `GetText -> 14066b3e0`, `GetLinkName -> 14066b5b0`,
  `GetEntries -> 14066b6f0`, `GetIcon -> 14066b920`,
  `GetCreaturePortrait -> 14066ba60`, and `GetWorldZone -> 14066bb20`.
- `14066b6f0` is now behavior-checked as well as table-named. It resolves the
  live article state, iterates the active child-entry container, filters out
  hidden/unavailable rows through the existing archive state checks, wraps each
  surviving child via `140433000`, and returns the accumulated Lua table. That
  is durable evidence that `GetEntries` returns a live entry list rather than a
  static metadata blob.
- Net result: the Galactic Archive Lua surface is no longer split between
  exact-named entry getters and half-guessed article helpers. Both the entry and
  article tables now have concrete `__eq` / `__gc` metamethod anchors, and the
  article side has its first stable getter cluster promoted into the label map.
- Verification: `./Decomp/Analysis/run_ghidra_analysis.ps1 -ExportOnly -Targets
  WildStar64.exe -DecompileMode Skip` applied `349` WildStar64 labels with `0`
  missing labels and refreshed the export metadata without widening
  `selected_decompiled.c`.

Seventy-ninth PathMission short-string table follow-up mapped from this pass:

- The new raw ASCII dumper also applies cleanly outside the Galactic Archive
  lane. `InspectCodeAddress.java` on `140679e50` shows
  `Game.PathMission` walking a `{ name, function }` table at `PTR_DAT_140c5c490`.
  `DumpNearbyData.java` over that table then revealed the same stall pattern as
  the archive bindings: the leading pointer cells exist, but several short names
  were not auto-defined in the listing.
- `DumpAsciiAtAddress.java` resolves those PathMission names directly from raw
  bytes. The first two slots decode to `__eq` and `__gc`, promoting
  `140678590` to the `Game.PathMission.__eq` metamethod and `140678790` to the
  matching `__gc` release path. Direct inspect confirms the same compare and
  reference-release shapes already seen in the Galactic Archive userdata types.
- The next PathMission table entries are now similarly concrete rather than
  anonymous callback pointers: `GetId -> 14067b7c0`, `GetName -> 14067b840`,
  `GetSummary -> 14067b9c0`, `GetType -> 14067bcb0`,
  `GetSubType -> 14067bd30`, `GetDisplayType -> 14067be10`,
  `GetRewardData -> 14067be90`, `GetRewardXp -> 14067c010`, and
  `GetDistance -> 14067c090`.
- This same pass also checked `Game.DialogResponse` as another possible target,
  but it does not represent the same stalled short-string problem. Its registrar
  starts from an already defined `GetText` string (`PTR_s_GetText_140c5ee40`),
  so the ASCII dumper is less valuable there than it is on the PathMission and
  Galactic Archive tables.
- Net result: the ASCII dumper is now proven as a general-purpose recovery tool
  for short undefined Lua method names, not just a one-off Galactic Archive
  trick. PathMission is the second clear success case, and DialogResponse is a
  useful counterexample showing when the extra raw-byte step is unnecessary.
- Verification: `./Decomp/Analysis/run_ghidra_analysis.ps1 -ExportOnly -Targets
  WildStar64.exe -DecompileMode Skip` applied `360` WildStar64 labels with `0`
  missing labels and refreshed the export metadata without widening
  `selected_decompiled.c`.

Eightieth PathMission continuation and Spell table-head audit mapped from this
pass:

- Extending `DumpNearbyData.java` past the previous PathMission cutoff at
  `PTR_DAT_140c5c490` cleanly promotes the next three `{ name, function }`
  pairs. `DumpAsciiAtAddress.java` resolves `140b30678` as `IsInArea`,
  `140b30688` as `IsComplete`, and `140b30658` as `IsOptional`, while the
  widened table dump pairs those names with `14067c190`, `14067c220`, and
  `14067c290` respectively.
- Those three callbacks are now stable `Game.PathMission` Lua methods rather
  than anonymous tail entries: `IsInArea -> 14067c190`,
  `IsComplete -> 14067c220`, and `IsOptional -> 14067c290`. This extends the
  PathMission surface beyond `GetDistance` without needing speculative behavior
  naming from decompiler-only control flow.
- The same audit on `Game.Spell` confirms the short-string pattern also exists
  at the head of `PTR_DAT_140c5a480`: raw-byte dumps of `140b1e524` and
  `140b1e254` decode to `__eq` and `__gc`. However, the surrounding table head
  is already covered by the current label map (`GetId`, `GetBaseSpellId`,
  `GetCastMethod`, `GetAOETargetInfo`, and adjacent accessors), so Spell does
  not presently need the same rescue pass that PathMission and GalacticArchive
  required.
- Net result: PathMission gains another stable trio of late-table boolean
  accessors, while Spell becomes a useful positive control showing that the
  ASCII-dumper heuristic can confirm short undefined slots even when the table
  is otherwise already well-labeled.
- Verification: `./Decomp/Analysis/run_ghidra_analysis.ps1 -ExportOnly -Targets
  WildStar64.exe -DecompileMode Skip` now applies the expanded WildStar64 label
  map after promoting the additional PathMission methods.

Eighty-first PathMission episodic tail follow-up mapped from this pass:

- Continuing the same `PTR_DAT_140c5c490` tail from the next unresolved short
  string at `140b30668` produces another clean registrar-backed pair. Raw-byte
  output from `DumpAsciiAtAddress.java` resolves `140b30668` as
  `GetEpisode`, and the widened `DumpNearbyData.java` table pairs it with
  `14067c300`.
- Extending one slot farther immediately yields another stable PathMission
  accessor rather than a speculative decompiler-only guess. Raw bytes at
  `140b30638` decode to `GetNumCompleted`, and the same table dump binds that
  name to `14067c370`.
- These callbacks are now durable `Game.PathMission` Lua methods:
  `GetEpisode -> 14067c300` and `GetNumCompleted -> 14067c370`. This keeps the
  PathMission continuation grounded in the table walk itself instead of inferred
  behavior naming.
- Net result: the post-`IsOptional` PathMission tail now extends into an
  episode/count accessor pair, and the same ASCII-dumper workflow continues to
  pay off on short undefined inline strings that Ghidra leaves unnamed.
- Verification: `./Decomp/Analysis/run_ghidra_analysis.ps1 -ExportOnly -Targets
  WildStar64.exe -DecompileMode Skip` applied `365` WildStar64 labels with `0`
  missing labels and refreshed the export metadata without widening
  `selected_decompiled.c`.

Eighty-second monster aggro/target threat-list follow-up implemented from this pass:

- `FUN_14042e1f0` is now safe to label
  `TargetThreatList_DispatchUpdatedEvent`. It clamps the cached target-threat
  pair count to five, zero-fills missing slots, and dispatches
  `TargetThreatListUpdated` with the format string `UiUiUiUiUi` through the
  shared client event sink from the targeted `InspectCodeAddress.java` pass.
  The threat HUD surface is therefore event-backed rather than directly bound
  to the raw packet buffer.
- `FUN_14055c0f0` is now safe to label `TargetThreatList_RebuildAndDispatch`.
  Its decompile shows the exact packet-shaped buffer the client consumes:
  `*param_2` is the source unit id, `param_2[1..5]` are threat unit ids, and
  `param_2[6..10]` are the matching threat values. The helper resolves the
  source unit, reads that unit's current target id, prepends the current target
  with its matching threat when present, appends the remaining non-zero entries
  without duplicating that current target, stores the rebuilt list at
  `param_1 + 0x66e8/+0x66f0`, and then dispatches
  `TargetThreatListUpdated` in the targeted `InspectCodeAddress.java` pass.
- That helper also exposes the client-side constraint that matters for
  NexusForever combat behavior: the rebuilt list can grow to six entries when
  the source unit's current target is absent from the incoming five-slot threat
  payload, but `TargetThreatList_DispatchUpdatedEvent` clamps the final HUD
  event back to five. In other words, omitting the creature's current target
  from the packet can force the client to show that target with threat `0`
  while dropping a real trailing hostile from the visible HUD list.
- `FUN_14055b0e0` is now safely mapped as
  `TargetSelection_ApplySelectionAndDispatch`. It clears the cached target-
  threat list count at `param_1 + 0x66f0`, immediately dispatches the cleared
  `TargetThreatListUpdated` event, sends opcode `0x0185 == ClientEntitySelect`
  with the new selection id, dispatches `TargetUnitChanged`, and then refreshes
  dependent selection and interaction state. Caller traces show this is the
  shared selection-apply path reached from scene replay, interaction/CSI,
  loot, activate-unit, and other local target changes from the targeted
  `InspectCodeAddress.java` and `TraceFunctionCallers.java` passes.
- Together these helpers bound the client behavior tightly enough for a narrow
  server fix. Source `ServerEntityThreatListUpdate` already matches the client
  `SrcUnitId + 5 ids + 5 threat values` buffer shape, and the threat HUD is
  intentionally cleared first on target change. The remaining mismatch was the
  packet builder: NexusForever was still sending the top five hostiles only.
- Runtime follow-up implemented from this pass:
  `ThreatManager.BuildServerThreatListUpdate()` now includes the creature's
  current target first when that target already exists on the threat list, then
  fills the remaining slots by descending threat. This keeps the client's
  current-target-first rebuild helper in-band and avoids the stale `current
  target with 0 threat` plus sixth-entry-drop case without widening any other
  aggro or combat-state behavior.
- Verification: `./Decomp/Analysis/run_ghidra_analysis.ps1 -ExportOnly -Targets
  WildStar64.exe -DecompileMode Skip` applies the expanded WildStar64 label map,
  and `dotnet build Source\\NexusForever.Game\\NexusForever.Game.csproj -v
  minimal --nologo` succeeds. A full solution build in this workspace is
  currently blocked by locked output files from running
  `NexusForever.Server.ChatServer`, `NexusForever.StsServer`,
  `NexusForever.AuthServer`, and `NexusForever.WorldServer` processes rather
  than by code errors in this pass.

Eighty-third PathMission full method-table tail follow-up mapped from this pass:

- The `Game.PathMission` Lua method table at `PTR_DAT_140c5c490` is now mapped
  through its end instead of stopping at `GetNumCompleted`. Direct
  `DumpNearbyData.java` passes over `140c5c490` and `140c5c700` show the
  continuing alternating `{ method name, function pointer }` layout, and the
  table terminates at the empty cells `140c5c7f0`/`140c5c7f8` before the next
  unrelated table starts at `140c5c800`.
- This pass adds durable labels for 38 previously anonymous PathMission Lua
  methods. The first continuation block binds core, map, scientist, and explorer
  accessors: `GetNumNeeded -> 14067c3f0`, `IsStarted -> 14067c4e0`,
  `GetMissionState -> 14067c470`, `GetMapIcon -> 14067c950`,
  `GetMapLocations -> 14067cb20`, `GetMapRegions -> 14067cfd0`,
  `GetUnlockString -> 14067c550`, `GetCompletedString -> 14067c750`,
  `GetScientistIcon -> 14067d4e0`, `GetScientistFieldStudy -> 14067d660`,
  `GetScientistSpecimenSurvey -> 14067d9a0`,
  `GetScientistDatacubeDiscoveryZone -> 14067dd40`,
  `GetScientistExperimentationInfo -> 14067de30`,
  `GetScientistExperimentationCurrentPatterns -> 14067e2d0`,
  `AttemptScientistExperimentation -> 14067e670`,
  `RefreshScientistExperimentation -> 14067e830`,
  `GetExplorerNodeInfo -> 14067e870`, `GetExplorerNodeCount -> 14067ed30`,
  `GetExplorerHuntStartCreature -> 14067edd0`,
  `GetExplorerHuntStartText -> 14067ee70`,
  `GetExplorerHuntSprite -> 14067ef90`,
  `GetExplorerClueStatus -> 14067f0e0`,
  `GetExplorerClueRatio -> 14067f1e0`, and
  `GetExplorerClueString -> 14067f2f0`.
- The final PathMission table segment binds the remaining explorer, settler,
  soldier, hint, and spell bridge methods: `GetExplorerClueType -> 14067f4b0`,
  `GetExplorerPowerMapReadyText -> 14067f670`,
  `GetExplorerPowerMapInfo -> 14067f780`,
  `IsExplorerPowerMapActive -> 14067fbf0`,
  `IsExplorerPowerMapReady -> 14067fcb0`,
  `ShowExplorerClueHintArrow -> 14067fd70`,
  `GetSettlerMayorInfo -> 14067fe10`,
  `GetSettlerSheriffInfo -> 140680180`,
  `GetSettlerScoutInfo -> 1406805b0`,
  `GetSettlerResourceRegions -> 140680ad0`,
  `GetSoldierHoldout -> 140680e30`, `ShowHintArrow -> 140680f60`,
  `ShowPathChecklistHintArrow -> 14067dca0`, and `GetSpell -> 140680ec0`.
- No server implementation was added from these labels. The registrar proves the
  client-authored Lua API names, but the underlying mission-state object fields,
  side effects for `AttemptScientistExperimentation`/hint helpers, and server
  packets or script hooks still need separate behavior evidence before mutating
  NexusForever runtime state.
- Verification:
  `./Decomp/Analysis/run_ghidra_analysis.ps1 -ExportOnly -Targets
  WildStar64.exe -MaxDecompiledFunctions 500` applied `411` WildStar64 labels
  with `0` missing labels and rendered a fresh `selected_decompiled.c`.
  Focused checks confirm all 38 new labels appear in `functions.csv`; each also
  appears in `selected_decompiled.c` with label reasons, covering the new block
  around `selected_decompiled.c:26382` through `selected_decompiled.c:29850`.

Eighty-fourth CombatAI attackability follow-up implemented from this pass:

- This pass did not add new native labels; it builds on the already-labeled
  client combat/threat anchors. `UnitState_MaybeDispatchUnitEvaded`
  (`1403db920`) still bounds the client-facing evade event at raw unit-state
  value `4` around `exports\WildStar64.exe\selected_decompiled.c:10786`,
  `UnitEvent_HandleEnteredCombat` (`14042e120`) bounds the entered-combat event
  around `selected_decompiled.c:12685`, and the threat HUD handlers remain
  `TargetThreatList_DispatchUpdatedEvent` (`14042e1f0`) plus
  `TargetThreatList_RebuildAndDispatch` (`14055c0f0`) around
  `selected_decompiled.c:12736` and `selected_decompiled.c:16777`.
- The safe implementation gap was on the NexusForever side. Source already
  models `AggroImmune` as client spell-effect value `0x004D` and as a real
  unit state, and `UnitEntity.CanAttack(...)` already gates dead/invalid targets
  plus source or target aggro immunity. `CombatAI`, however, still accepted
  initial aggro from any hostile unit inside leash and kept existing threat
  targets as long as they were alive and in leash.
- Runtime follow-up implemented from this pass:
  `CombatAI` now uses `entity.CanAttack(target)` for initial aggro, target
  selection, current-target validation, autoattack, and chase decisions. If the
  current target becomes non-attackable or leaves the leash, the AI prunes that
  hostile and lets the existing threat-selection/reset path choose the next safe
  target or return home. This keeps the earlier leash and evade-floater behavior
  intact while preventing dead, invalid, aggro-immune, or otherwise non-
  attackable units from remaining active CombatAI targets.
- Still blocked: this does not synthesize the client raw state `4` producer,
  alter threat table math, or implement `MaxThreatVsCreature`/`ThreatMultiplier`
  behavior. The native strings for those threat properties are observed, but
  the current evidence does not yet prove safe server-side caps or multipliers.
- Verification:
  `dotnet build Source\NexusForever.Script.Main\NexusForever.Script.Main.csproj
  --no-restore -m:1
  -p:BaseOutputPath=I:/GIT/NexusForever/.nexusforever-runtime/build-combatai/
  -v minimal --nologo` succeeds with `0` warnings and `0` errors.

Eighty-fifth Rider's Reef combat projector follow-up implemented from this pass:

- This pass did not add new native labels. The runtime mismatch was correlated
  from the authoritative tutorial world SQL import: combat projector creature
  `73735` is imported as entity type `32` (`SimpleCollidable`), while the
  existing tutorial projector script was owned by `ISimpleEntity`, and
  `SimpleCollidableEntity` did not initialise entity scripts at all.
- Runtime follow-up implemented from this pass:
  `SimpleCollidableEntity` now initialises script collections for
  `ISimpleCollidableEntity` and preserves `QuestChecklistIdx` in its network
  model. `TutorialCombatProjectorEntityScript` is now owned by
  `ISimpleCollidableEntity`, so successful activation of imported creature
  `73735` can queue the novice combat-projector cinematic and delayed transport.
  The script gate now allows projector use after the hoverboard projector and
  ride objectives are complete, regardless of whether the finish objective was
  already credited by the interaction path.
- Recovery follow-up implemented from this pass:
  `InteractionObjectiveUpdater` queues a delayed combat-transition recovery
  after successful objective credit on creature `73735`. This is intentionally
  later than the scripted cinematic transport, so the normal script path wins
  when loaded, while already-stuck or script-missed activations can still
  recover through the existing player combat-transition logic. Follow-up log
  inspection showed a successful `ClientActivateUnitCast` for `73735` and spell
  `87061`, but no projector script activation lines because the live
  `NexusForever.Script.Main.dll` was stale. Direct project builds were writing
  to `I:\NexusForever.WorldServer\...` when `$(SolutionDir)` was empty, so the
  script project now uses its own file directory to target the actual
  WorldServer output folder. The projector-activation fallback also no longer
  requires the player to remain inside the small hoverboard finish volume once
  `73735` activation has succeeded.
- Still blocked: full Rider's Reef recovery still needs an in-client fresh
  Exile and Dominion validation pass for world `3460`, including projector
  activation, cinematic timing, transport target, and post-transition combat
  lane visibility.
- Verification:
  focused isolated-output builds succeed for
  `Source\NexusForever.Game\NexusForever.Game.csproj`,
  `Source\NexusForever.Script.Main\NexusForever.Script.Main.csproj`, and
  `Source\NexusForever.WorldServer\NexusForever.WorldServer.csproj`. The only
  warning observed is the existing `Spline.formation` CS0649 warning in the
  Game build dependency.

Eighty-sixth Game.Challenges method-table follow-up mapped from this pass:

- The next callback table after the completed `Game.PathMission` block is now
  mapped as the `Game.Challenges` userdata method table. Direct
  `DumpNearbyData.java` over `140c5c800` shows an alternating
  `{ method name, function pointer }` table from `140c5c800` through
  `140c5c9e8`, followed by empty cells at `140c5c9f0`/`140c5c9f8` and then the
  next unrelated PublicEvent table at `140c5ca00`. Raw ASCII dumps resolve the
  short head strings at `140b321c0` and `140b321c8` to `__eq` and `__gc`.
- This pass adds 31 durable `Game.Challenges` labels. The table binds the
  metamethod and core progress/tier accessors:
  `__eq -> 140684cd0`, `__gc -> 140684ed0`, `GetId -> 1406850e0`,
  `GetType -> 140685170`, `GetCurrentCount -> 140685210`,
  `GetTotalCount -> 140685330`, `GetCurrentTier -> 140685440`,
  `GetDisplayTier -> 140685540`, `GetCompletionCount -> 140685640`,
  `GetCompletionTotal -> 140685740`, `GetLastRewardTier -> 1406857d0`,
  `GetRewardTrack -> 1406858d0`, `IsTimeTiered -> 140685950`,
  `IsActivated -> 1406859e0`, `IsInCooldown -> 140685ad0`, and
  `IsFullyComplete -> 140685be0`.
- The same table binds the remaining display, zone, timer, map, hint, and
  distance accessors: `GetName -> 140685cf0`,
  `GetDescription -> 140685db0`, `GetZoneInfo -> 140685f70`,
  `GetZoneRestrictionInfo -> 1406860c0`,
  `GetStartLocationRestrictionId -> 140686240`, `GetTimer -> 1406862d0`,
  `GetDuration -> 1406863e0`, `GetAllTierCounts -> 1406864e0`,
  `NeedsHintArrow -> 140686730`, `GetMapStartLocation -> 1406867d0`,
  `GetMapStartRegions -> 140686b50`, `GetMapLocation -> 140686e20`,
  `GetMapRegions -> 1406871a0`,
  `ShouldDisplayPercentProgress -> 140687610`, and
  `GetDistance -> 1406876b0`.
- No server implementation was added. The labels prove the client-authored Lua
  API names for challenge objects, but they do not yet prove challenge runtime
  state mutation, `ServerChallengeUpdate` field semantics, `ClientChallengeChoice`
  handling, cooldown/tier progression, or the `ChallengesLib` global request
  surface. Those remain blocked until packet/state producers or stronger
  handler evidence are mapped.
- Verification:
  `./Decomp/Analysis/run_ghidra_analysis.ps1 -ExportOnly -Targets
  WildStar64.exe -MaxDecompiledFunctions 500` applied `442` WildStar64 labels
  with `0` missing labels and rendered a fresh `selected_decompiled.c`. Focused
  checks confirm all 31 new `Lua_Challenges_*` labels appear in `functions.csv`
  and in `selected_decompiled.c`, covering the selected block around
  `selected_decompiled.c:29920` through `selected_decompiled.c:31903`.
  `./Decomp/Analysis/Test-DecompileManifest.ps1 -FailOnMismatch` reports
  `WildStar64.exe ok`.

Eighty-seventh Game.PublicEvent method-table follow-up mapped from this pass:

- The callback table immediately after `Game.Challenges` is now mapped as the
  `Game.PublicEvent` userdata method table. Direct `DumpNearbyData.java` over
  `140c5ca00` shows the alternating `{ method name, function pointer }` layout
  from `140c5ca00` through `140c5cc18`, followed by empty cells at
  `140c5cc20`/`140c5cc28` and then the next `Game.PublicEventObjective` table
  at `140c5cc30`. Raw ASCII dumps resolve the short head strings at
  `140b324cc` and `140b324a0` to `__eq` and `__gc`.
- This pass adds 34 durable `Game.PublicEvent` labels. The table binds the
  metamethod and core event accessors:
  `__eq -> 140687820`, `__gc -> 140687a40`,
  `RequestScoreboard -> 140689580`, `GetName -> 1406896f0`,
  `GetId -> 140689920`, `IsActive -> 1406899b0`,
  `GetObjectives -> 140689a50`, `GetObjective -> 140689c70`,
  `GetElapsedTime -> 140689da0`, `GetTotalTime -> 140689e60`,
  `GetJoinedTeam -> 140689f20`, `GetTeamCount -> 140689fd0`,
  `GetMapZone -> 14068a090`, `GetLocations -> 14068a1a0`, and
  `GetMapRegions -> 14068a500`.
- The same table binds the remaining tracker, stat, reward, display, report,
  and custom-tracker helpers: `ShowHintArrow -> 14068ad40`,
  `GetTrackedUnits -> 14068adb0`, `GetLiveStats -> 14068afb0`,
  `HasLiveStats -> 14068b140`, `GetMyStats -> 14068b1f0`,
  `GetStatsToDisplay -> 14068b410`, `GetTrackedSpawns -> 14068b570`,
  `GetEventType -> 14068b750`, `GetParentEvent -> 14068b800`,
  `GetRewardType -> 14068b8d0`, `GetRewardThreshold -> 14068b990`,
  `ShouldShowOnMiniMapEdge -> 14050d090`, `GetLiveEvent -> 14068ba90`,
  `PrepareInfractionReport -> 14068bb80`, `GetStat -> 14068bd60`,
  `ShouldShowMedalsUI -> 14068be30`,
  `IsPriorityDisplay -> 14068bf30`, `GetEndMessage -> 14068bff0`, and
  `ShouldUseCustomTracker -> 14068c220`.
- No server implementation was added. These labels prove the client-authored
  Lua API names for public-event objects, but they do not yet prove safe
  NexusForever mutations for event participation, scoreboard requests, custom
  tracker state, objective progress, team membership, reward thresholds, or
  infraction-report side effects. Those remain blocked until packet handlers,
  state producers, or stronger runtime evidence are mapped.
- Verification:
  `./Decomp/Analysis/run_ghidra_analysis.ps1 -ExportOnly -Targets
  WildStar64.exe -MaxDecompiledFunctions 500` applied `476` WildStar64 labels,
  created the small `Lua_PublicEvent_ShouldShowOnMiniMapEdge` stub function,
  reported `0` missing labels, and rendered a fresh `selected_decompiled.c`.
  Focused checks confirm all 34 new `Lua_PublicEvent_*` labels appear in
  `functions.csv` and in `selected_decompiled.c`, covering the selected block
  around `selected_decompiled.c:14676` and `selected_decompiled.c:31986`
  through `selected_decompiled.c:34275`.
  `./Decomp/Analysis/Test-DecompileManifest.ps1 -FailOnMismatch` reports
  `WildStar64.exe ok`.

Eighty-eighth Game.PublicEventObjective method-table follow-up mapped from this
pass:

- The callback table immediately after `Game.PublicEvent` is now mapped as the
  `Game.PublicEventObjective` userdata method table. Direct
  `DumpNearbyData.java` over `140c5cc30` shows the alternating
  `{ method name, function pointer }` layout from `140c5cc30` through
  `140c5ce98`, followed by empty cells at `140c5cea0`/`140c5cea8` and then the
  next unrelated table at `140c5ceb0`. Raw ASCII dumps resolve the short head
  strings at `140b32e1c` and `140b32e24` to `__eq` and `__gc`; the one
  out-of-block method-name pointer at `1409f5b5c` resolves to `IsBusy`.
- This pass adds 39 durable `Game.PublicEventObjective` labels. The table binds
  the metamethod, description, identity, status, timer, count, and display
  accessors: `__eq -> 140687ac0`, `__gc -> 140687cc0`,
  `GetDescription -> 14068d5b0`, `GetShortDescription -> 14068d8a0`,
  `GetObjectiveId -> 14068db80`, `GetSpell -> 14068dc10`,
  `GetStatus -> 14068dd50`, `GetNotificationMode -> 14068de40`,
  `GetElapsedTime -> 14068df30`, `GetTotalTime -> 14068e020`,
  `GetCount -> 14068e110`, `ShowPercent -> 14068ed90`,
  `ShowHealthBar -> 14068ee80`, and `GetRequiredCount -> 14068ef30`.
- The same table binds team, map, event-link, visibility, parent/live-event,
  medal, message, contested-area, depot, and warplot helpers:
  `GetTeam -> 14068f140`, `GetOwningTeam -> 14068f200`,
  `GetLocations -> 14068f2f0`, `GetMapRegions -> 14068f700`,
  `ShowHintArrow -> 14068fff0`, `GetObjectiveType -> 1406900a0`,
  `GetEvent -> 140690160`, `IsBusy -> 140690250`,
  `ShouldShowOnMinimap -> 140690330`,
  `ShouldShowOnMinimapEdge -> 140690410`,
  `GetTrackedUnits -> 140690500`, `IsHidden -> 140690730`,
  `GetCategory -> 1406907f0`,
  `ShouldShowRequiredCount -> 1406908b0`,
  `GetParentObjective -> 140690990`,
  `GetLiveEventName -> 140690aa0`,
  `GetLiveEventSummary -> 140690c20`,
  `GetMedalPoints -> 140690e80`, `GetJoinMessage -> 140690f20`,
  `GetStartMessage -> 140691150`, `GetDisplayOrder -> 140691380`,
  `GetContestedAreaOwningTeam -> 14068e420`,
  `GetContestedAreaRatio -> 14068e520`,
  `GetVirtualDepotItems -> 14068e6c0`, and
  `GetWarplotLocation -> 14068e970`.
- No server implementation was added. These labels prove the client-authored
  Lua API names for public-event objective objects, but they do not yet prove
  safe NexusForever mutations for objective progress, required-count updates,
  hidden/visibility rules, contested-area ownership, virtual depot items,
  warplot objective locations, or live-event parent/summary state. Those remain
  blocked until packet handlers, state producers, or runtime captures map the
  underlying fields and update flow.
- Verification:
  `./Decomp/Analysis/run_ghidra_analysis.ps1 -ExportOnly -Targets
  WildStar64.exe -MaxDecompiledFunctions 500` applied `515` WildStar64 labels
  with `0` missing labels and rendered a fresh `selected_decompiled.c`.
  Focused checks confirm all 39 new `Lua_PublicEventObjective_*` labels appear
  in `functions.csv` and in `selected_decompiled.c`, covering the selected block
  around `selected_decompiled.c:32078` through
  `selected_decompiled.c:37680`.
  `./Decomp/Analysis/Test-DecompileManifest.ps1 -FailOnMismatch` reports
  `WildStar64.exe ok`.

Eighty-seventh Rider's Reef combat-lane follow-up implemented from this pass:

- This pass did not add new native labels. The runtime evidence came from the
  local worldserver log after the combat projector fix: the player was
  transported to Exile combat simulation world location `51739`, quest `10518`
  stayed accepted, and combat AI spell traces showed tutorial hostile units
  selecting themselves as valid attack targets.
- Runtime follow-up implemented from this pass:
  `UnitEntity.CanAttack(...)` now rejects null, dead, and self targets before
  disposition checks. This prevents Rider's Reef combat NPCs from entering
  self-hostile loops and dying before player threat can cleanly drive kill
  objective credit.
- Quest follow-up implemented from this pass:
  `InteractionObjectiveUpdater` now has a narrow Rider's Reef mine-credit path
  for the imported combat mine entities. The official world SQL places
  `73463`, `73667`, and `73668` at the mine objective locations, but the
  `10518`/`10524` `ActivateEntity` objective data does not consistently match
  those imported creature ids. The updater therefore credits the exact objective
  ids by quest, creature id, and world-location proximity:
  Exile `21287`/`21340`/`21318` at `51662`/`51663`/`51664`, and Dominion
  `21313`/`21314`/`21315` at `52899`/`52900`/`52901`.
- Diagnostics added:
  quest objective-update and objective-world-location logging now covers combat
  quests `10518` and `10524`, and mine-credit logging records both successful
  and location-skipped combat mine activations.
- Still blocked:
  fresh in-client validation still needs to walk through the combat lane after
  the projector transport on Exile, then repeat on Dominion. The next evidence
  target is whether first-wave kills for `73464`/`73465`, turret kills, and
  final target groups `14356`/`14402` advance all remaining objectives without
  further table remaps.
- Verification:
  `dotnet build Source\NexusForever.WorldServer\NexusForever.WorldServer.csproj
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build-riders-reef-combat\worldserver-serial2\
  -p:UseSharedCompilation=false -m:1 -v minimal --nologo` succeeds with `0`
  warnings and `0` errors after the initial parallel-build verifier attempts
  were rerun serially. `.\Tools\Setup\Restart-NexusForeverAuthWorldLocal.ps1
  -ClientDirectory "I:\WildStar"` restarted Auth/World, and the fresh
  WorldServer log shows the tutorial script assembly loading without startup
  errors.

Eighty-eighth Rider's Reef combat-lane follow-up implemented from this pass:

- This pass used the older `kirmmin/NexusForever` `latest` branch as a
  comparison source at commit `d9b4c0aa8b4f8ee111d799aa2f4d646d9304f1d1`.
  Its `WorldEntity` default leash range is `50f`, and its `UnitAI` leash reset
  checks the creature's distance from its spawn rather than immediately
  invalidating a player target outside a small target-vs-spawn radius. The
  local WorldServer log had shown Rider's Reef combat beasts taking player
  damage and then immediately removing/re-adding threat, healing, and resetting;
  that behavior correlates with the current 15m leash target check being too
  tight for the combat simulation layout.
- Runtime follow-up implemented from this pass:
  `CombatAI` now keeps the tutorial-only player-aggro guard from the previous
  pass but raises the Rider's Reef combat creature effective leash to `50f`,
  matching the older branch baseline without changing global AI behavior. The
  tutorial combat set now includes the final simulated enemies `73492`,
  `73567`, and `73566` in addition to the first-wave and turret creatures.
- Quest target-group follow-up implemented from this pass:
  `Tools/QuestTableInspector --target-groups` now prints target group
  membership recursively for table-backed checks. The table evidence for the
  final combat objectives is direct:
  `14356 -> CreatureIdGroup [73492,73567]` for Exile quest `10518` objective
  `21290`, and `14402 -> CreatureIdGroup [73473,73566]` for Dominion quest
  `10524` objective `21317`.
- Quest objective progress follow-up implemented from this pass:
  dynamic quest objectives now scale count-based updates with integer ceiling
  instead of float truncation. This prevents count-`3` objectives such as the
  Rider's Reef final target groups from landing at `999/1000` after three
  one-kill updates.
- World-content follow-up implemented from this pass:
  `TutorialMapScript` now dynamically spawns the missing Exile final combat
  wave at world location `51740` with three target-group members
  (`73492`, `73567`, `73492`) and expands the synthetic Dominion final wave at
  world location `53015` to three target-group members
  (`73473`, `73566`, `73473`). This matches the objective counts of `3` while
  staying scoped to map `3460`.
- Still blocked:
  fresh in-client validation needs to re-enter Rider's Reef after restart,
  activate the Combat Projector, kill the first wave, activate all three mines,
  destroy both turrets, and kill the final target group. The fresh post-restart
  WorldServer log confirms the tutorial map loaded the new script assembly and
  spawned the Exile final wave plus expanded Dominion final wave, but no new
  kill or mine objective updates had been observed yet during the verification
  window.
- Verification:
  `dotnet build Source\NexusForever.WorldServer\NexusForever.WorldServer.csproj
  -p:UseSharedCompilation=false -m:1 -v minimal --nologo` succeeds with `0`
  warnings and `0` errors after stopping the running WorldServer so output DLLs
  are unlocked. `dotnet build Source\NexusForever.Script.Main\NexusForever.Script.Main.csproj
  -p:UseSharedCompilation=false -m:1 -v minimal --nologo` also succeeds with
  `0` warnings and `0` errors and refreshes
  `Source\NexusForever.WorldServer\bin\Debug\net10.0\NexusForever.Script.Main.dll`.
  `dotnet run --project Tools\QuestTableInspector\QuestTableInspector.csproj --
  --target-groups .nexusforever-runtime\assets\tbl 14356 14402` prints the two
  expected creature-id groups. `.\Tools\Setup\Restart-NexusForeverAuthWorldLocal.ps1
  -ClientDirectory "I:\WildStar"` restarted Auth/World, and the final fresh
  WorldServer process came up responding with no `error`, `exception`,
  `failed`, `Fatal`, or `Unhandled` matches in
  `.nexusforever-runtime\logs\NexusForever.WorldServer.stdout.log`. After a
  character loaded world `3460`, the log confirmed
  `Spawned Rider's Reef Exile final combat wave ... legionnaires=3` and
  `Spawned Rider's Reef Dominion combat lane ... finalHostiles=3`.

Eighty-ninth kirmmin/latest quest target-group follow-up implemented from this
pass:

- This pass refreshed the older `kirmmin/NexusForever` `latest` comparison
  branch and confirmed it remains at commit
  `d9b4c0aa8b4f8ee111d799aa2f4d646d9304f1d1`. The useful portable behavior in
  this pass is quest target-group expansion and objective matching. The older
  branch also has a larger loot subsystem, but that path touches loot managers,
  corpse state, packets, and persistence enough that it remains a separate
  blocked follow-up rather than a safe quick port.
- Quest target-group runtime follow-up implemented from this pass:
  `AssetManager` now caches quest-objective target ids by recursively expanding
  table-backed `CreatureIdGroup`, nested `OtherTargetGroup`, and the older
  branch's type-`11` `OtherTargetGroupCreatures` entries for target-group
  objective types. `IAssetManager` exposes
  `GetQuestObjectiveTargetIds(...)` so quest objectives can resolve concrete
  targets without every call site understanding the table hierarchy.
- Quest objective follow-up implemented from this pass:
  `QuestObjective` now builds its target-id list for checklist and target-group
  objectives, exposes `IsTarget(...)`, treats checklist progress as a bitmask,
  and completes checklist objectives by filling the expected bits. This lets
  target-group objectives match concrete creature ids while keeping the earlier
  dynamic-objective integer-ceiling fix for count-based progress.
- Objective-credit follow-up implemented from this pass:
  kill, activate, talk, and the `quest kill` debug command now credit
  target-group quest objectives with the concrete creature id/objective target.
  Activation checklist credit uses the activated entity's `QuestChecklistIdx`,
  matching the table-driven checklist model instead of repeatedly crediting the
  enclosing target-group id.
- Still blocked:
  the older branch's full loot implementation is worth mining next, but it
  should be ported as a dedicated subsystem pass with packet, corpse-state,
  item-generation, and database checks. The current target-group cache still
  intentionally avoids semantic guesses for non-creature filters such as
  faction/race/exclusion groups until those have fresh table evidence.
- Verification:
  `dotnet build Source\NexusForever.WorldServer\NexusForever.WorldServer.csproj
  -p:UseSharedCompilation=false -m:1 -v minimal --nologo` succeeds with only
  the pre-existing `Spline.formation` warning. `dotnet run --project
  Tools\QuestTableInspector\QuestTableInspector.csproj -- --target-groups
  .nexusforever-runtime\assets\tbl 14356 14402` still prints the expected
  Rider's Reef final-combat creature groups. `.\Tools\Setup\Restart-NexusForeverAuthWorldLocal.ps1
  -ClientDirectory "I:\WildStar" -SkipClientLaunch` rebuilds and restarts
  Auth/World, both processes report responding, and the fresh WorldServer
  stdout log has no `error`, `exception`, `failed`, `Fatal`, or `Unhandled`
  matches.

Ninetieth kirmmin/latest loot subsystem follow-up implemented from this pass:

- This pass refreshed the older `kirmmin/NexusForever` `latest` comparison
  branch and confirmed local `HEAD` and `origin/latest` both remain at commit
  `d9b4c0aa8b4f8ee111d799aa2f4d646d9304f1d1`. The old branch's portable loot
  behavior is server-authored rather than decompiled client code: cached
  creature/item loot tables, per-kill loot instances, `ClientLootItem`
  request/collect handling, `ClientLootVacuum`, static item/cash/account
  currency/virtual item delivery, quest-objective loot conditions, and kill XP.
- Database follow-up implemented from this pass:
  the current world context now maps the old branch loot tables
  `entity_loot`, `item_loot`, `loot_group`, and `loot_item`, plus the current
  data-mapping table `creature_loot`. Migration
  `20260517013000_LootTables` creates all five tables with `IF NOT EXISTS` so
  existing safe-import loot data is preserved.
- Runtime loot follow-up implemented from this pass:
  `GlobalLootManager` now initialises after items/quests, updates each world
  tick, rolls both old-table loot groups and imported direct `creature_loot`
  rows, keeps per-owner loot instances, sends `ServerLootNotify`, grants
  selected loot via `ServerLootGrant`, removes exhausted loot with
  `ServerLootRemove`, and re-notifies a player when a visible loot owner enters
  scope. Imported `creature_loot.chance` is treated as the mapped `0..1`
  probability and direct imported drops currently grant one static item per
  successful roll to avoid over-interpreting aggregate-count columns.
- Client handler follow-up implemented from this pass:
  `ClientLootItem` now treats `OwnerUnitId` as the visible corpse/owner and
  `LootUnitId` as the generated loot-item id. `Request=true` resends notify,
  `Request=false` grants the selected loot item, and `ClientLootVacuum` grants
  all permitted loot within the manager's 35m range.
- Reward follow-up implemented from this pass:
  non-player kill rewards now call `GrantXpForCreatureKill(...)` and
  `GlobalLootManager.DropLoot(...)`. Rest XP for kill rewards is capped by the
  player's available `RestBonusXp` and consumed when awarded, matching the old
  branch behavior. `IQuestManager.IsActiveObjectiveId(...)` was added for old
  loot group condition type `QuestObjectiveActive`.
- Still blocked:
  group/raid/master-loot rolls, loot-bag spell effects, offline mail delivery,
  account-item delivery, random item stat generation, and exact retail stack
  quantity semantics remain unmapped. The old branch's random Omnibit kill
  shower was not ported because it has no table-backed evidence in this pass
  and would mutate account currency on every eligible creature kill.
- Verification:
  `dotnet build Source\NexusForever.WorldServer\NexusForever.WorldServer.csproj
  -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\loot-port\
  -m:1 -v minimal --nologo` succeeds with only the pre-existing
  `Spline.formation` warning. The normal-output build initially failed only
  because the running `NexusForever.WorldServer` process locked its DLLs.
  `.\Tools\Setup\Restart-NexusForeverAuthWorldLocal.ps1 -ClientDirectory
  "I:\WildStar" -SkipClientLaunch` then stopped Auth/World, rebuilt normal
  output, restarted both servers, and the fresh WorldServer log shows
  `20260517013000_LootTables` applied and `GlobalLootManager` loaded loot for
  `0` old-table creatures, `0` item tables, and `3795` imported creatures in
  `924ms`. The fresh WorldServer process is running and the final log scan
  shows no startup `exception`, `fatal`, or `unhandled` failures.

Ninety-first loot table and loot-bag follow-up implemented from this pass:

- This pass used `Various SQL\Drop_PR.sql` as the concrete old-table seed for
  `Bag of OmniBits`: group probabilities `100 -> 50 -> 25`, loot item type `9`
  (`AccountCurrency`) with static id `6` (`Omnibit`), and item loot id
  `84623`. Local MySQL client data verified `wildstar_client.item2` row
  `84623` is named `Bag of OmniBits`, has `item2CategoryId = 138`, and
  `wildstar_client.item2category` row `138` is named `Loot Bag`.
- Database follow-up implemented from this pass:
  migration `20260517023000_OmnibitLootBagSeed` seeds the Bag of OmniBits loot
  table using non-conflicting group ids `84623001..84623003`. The migration
  keeps the `Drop_PR.sql` probabilities and amounts while avoiding reuse of
  low group ids `1..3` that could collide with user-imported loot data.
- Runtime loot follow-up implemented from this pass:
  `GlobalLootManager` now ports the older branch's random Omnibit creature-kill
  reward using the same `35%` chance and `7..(25 + player level)` amount range,
  delivered through the current `ServerLootGrant` path. Loot validation now
  accepts `LootItemType.AccountItem` when the current client account-item table
  contains the static id.
- Account-item delivery follow-up implemented from this pass:
  `LootInstanceItem.DeliverItem(...)` now handles `LootItemType.AccountItem` by
  adding the requested number of account inventory entries through
  `IAccount.InventoryManager.AddItem(...)`, reusing the current account
  inventory notification and persistence path.
- Loot-bag packet follow-up implemented from this pass:
  `ClientItemUseLootBag` is now parsed for opcode `0x015E` as item location
  followed by item guid, matching `kirmmin/latest`. `ClientItemUseLootBagHandler`
  validates the item exists, the guid matches, and the item is category `138`
  before consuming the bag and dropping item-table loot. `ClientLootItem` also
  now accepts `OwnerUnitId == player.Guid`, which is required because inventory
  loot bags use the player as the loot owner rather than a visible corpse.
- Superseded by later work:
  group/master/roll loot remained diagnostic-only at the end of this pass. The
  later ninety-fourth pass added the missing world-side group snapshot,
  eligibility, loot-rule, roll-state, and master-assignment design.
- Verification:
  `dotnet build Source\NexusForever.WorldServer\NexusForever.WorldServer.csproj
  -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\loot-followup\
  -m:1 -v minimal --nologo` succeeds with `0` warnings and `0` errors.
  `.\Tools\Setup\Restart-NexusForeverAuthWorldLocal.ps1 -ClientDirectory
  "I:\WildStar" -SkipClientLaunch` rebuilt and restarted Auth/World, both
  processes are responding, and the fresh WorldServer log shows migration
  `20260517023000_OmnibitLootBagSeed` applied, `ClientItemUseLootBagHandler`
  registered, and `GlobalLootManager` loaded loot for `0` old-table creatures,
  `1` item, and `3795` imported creatures. Direct MySQL verification shows
  `3` seeded `loot_group` rows, `3` seeded `loot_item` rows, `1`
  `item_loot` row for item `84623`, and `198735` live `creature_loot` rows.
  Fresh Auth/World log scans show no `[ERROR]`, `[FATAL]`, unhandled exception,
  or `System.` exception output.

Ninety-second loot client-event dispatcher follow-up mapped from this pass:

- This pass stayed on the native client side and mapped the anonymous loot
  client-event cluster around `140427d40..140430f60`. Direct
  `InspectCodeAddress.java` passes show these helpers build Lua-style payload
  tables and dispatch named client events through `ClientEvent_DispatchNamedEvent`
  or the same shared event-dispatch thunk path already used elsewhere.
- Ten durable WildStar64 labels were added. The full payload builders are:
  `Loot_DispatchLootTakenByEvent -> 140427d40`, which builds `nLootId`,
  `itemLoot`, and `unitLooter`; `Loot_DispatchChannelUpdateLootEvent ->
  140427fa0`, which builds `ChannelUpdate_Loot` for item, currency, and
  destroyed-item loot updates; `Loot_DispatchLootRollSelectedEvent ->
  140428500`, which carries `nLootId`, `bNeed`, and `itemLoot`;
  `Loot_DispatchLootRollPassedEvent -> 140428840`, which carries `nLootId`
  and `itemLoot`; `Loot_DispatchLootRollEvent -> 140428ac0`, which carries
  `nLootId`, roll state, and `itemLoot`; `Loot_DispatchLootRollWonEvent ->
  140428e40`, which carries `nLootId`, `bNeed`, winner state, and `itemLoot`;
  `Loot_DispatchLootAssignedEvent -> 140429180`; and
  `Loot_DispatchLootRollAllPassedEvent -> 140429400`.
- The same inspection pass also mapped two tiny event thunks that Ghidra had not
  promoted to functions before the label pass:
  `Loot_DispatchLootRollUpdateEventThunk -> 140430de0`, forwarding the
  `LootRollUpdate` event name to the generic dispatcher, and
  `Loot_DispatchMasterLootUpdateEventThunk -> 140430f60`, forwarding the
  `MasterLootUpdate` event name. `ApplyNexusForeverLabels.java` created both
  small functions on re-export.
- No server implementation was added. These labels improve the client-side
  event map for loot notifications, loot rolls, master loot, and channel-update
  payloads, but they do not by themselves prove group loot eligibility, roll
  resolution, master-loot assignment authority, or packet field semantics beyond
  the already-implemented server loot flow. Group/master/roll loot remains
  blocked on packet-state and group-membership evidence.
- Verification:
  `./Decomp/Analysis/run_ghidra_analysis.ps1 -ExportOnly -Targets
  WildStar64.exe -MaxDecompiledFunctions 600` applied `525` WildStar64 labels,
  created the two small loot event thunk functions, reported `0` missing labels,
  and rendered a fresh `selected_decompiled.c`. Focused checks confirm all 10
  new loot event labels appear in `functions.csv` and `selected_decompiled.c`,
  covering the selected block around `selected_decompiled.c:12685` through
  `selected_decompiled.c:13975`.
  `./Decomp/Analysis/Test-DecompileManifest.ps1 -FailOnMismatch` reports
  `WildStar64.exe ok`.

Ninety-third loot party/drop follow-up mapped from this pass:

- This pass widened the loot surface into the nearby party/group and item-drop
  request boundary. Focused `DumpNearbyData.java` and `InspectCodeAddress.java`
  passes mapped the `GameLib` loot-roll/master-loot Lua binding table at
  `140b72f60..140b72fb8`, the `GroupLib` loot-rule table at
  `140b74ff0..140b75008`, and the direct `LootItemRequested` request sender.
- Eleven durable WildStar64 labels were added. The loot request labels are:
  `Loot_SendClientLootItemRequest -> 140516ae0`, which sends opcode `0x014F`
  `ClientLootItem` with owner unit id, loot id, and the request bit set before
  dispatching `LootItemRequested`; `Lua_GameLib_BuildLootRollEntry ->
  1406fed70`, which builds Lua roll/master-loot entries with `nLootId`,
  optional `nTimeLeft`, `itemDrop`, optional `bIsMaster`, `tLooters`, and
  `tLootersOutOfRange`; `Lua_GameLib_GetLootRolls -> 1406ff290`;
  `Lua_GameLib_RollOnLoot -> 1406ff680`, which sends opcode `0x015D`
  `ClientLootRollAction` as Need or Greed; `Lua_GameLib_PassOnLoot ->
  1406ff820`, which sends the same opcode with Pass; `Lua_GameLib_GetMasterLoot
  -> 1406ff970`; and `Lua_GameLib_AssignMasterLoot -> 140700b30`, which sends
  opcode `0x00B5` `ClientLootAssignMaster` with owner unit id, loot id, and the
  assignee identity.
- The party/group loot-rule side is now mapped as
  `Group_HandleLootRulesChanged -> 140603560`, which applies incoming group
  loot-rule state and dispatches `Group_LootRulesChanged` for the active group;
  `Lua_GroupLib_SetLootRules -> 140743ff0`, which sends opcode `0x042E`
  `ClientGroupLootRulesChange` with current group id plus four rule fields;
  `Lua_GroupLib_GetLootRules -> 140744210`, which returns `eNormalRule`,
  `eThresholdRule`, `eThresholdQuality`, and `eHarvestRule`; and
  `Lua_RegisterGroupLib -> 140744d60`, which confirms the client-facing
  `LootThreshold`, `LootRule`, and `HarvestLootRule` constant tables. The
  observed constants match the current NexusForever enum values:
  `LootRule.FreeForAll=0`, `RoundRobin=1`, `NeedBeforeGreed=2`, `Master=3`;
  `HarvestLootRule.RoundRobin=0`, `FirstTagger=1`; and threshold qualities
  `Inferior=1` through `Artifact=7`.
- No new server implementation was added in this pass. The current network
  models and handlers already line up with the mapped client request boundaries:
  `ClientLootItem` uses the `0x014F` owner/loot/request-bit shape,
  `ClientLootRollAction` uses the `0x015D` owner/loot/action shape,
  `ClientLootAssignMaster` uses the `0x00B5` owner/loot/identity shape, and
  `ClientGroupLootRulesChange` uses the `0x042E` group id and four-rule-field
  shape. `ServerGroupLootRulesChange` still contains an unknown dword that the
  client handler does not use for local rule fields in this mapping.
- Still blocked:
  exact retail group-loot eligibility, master-looter authority, round-robin
  ordering under membership churn, `tLootersOutOfRange` UI behavior, and the
  unknown `ServerGroupLootRulesChange` dword need runtime/sniff validation
  before further behavior is treated as verified.
- Verification:
  `./Decomp/Analysis/run_ghidra_analysis.ps1 -ExportOnly -Targets
  WildStar64.exe -MaxDecompiledFunctions 620` applied `536` WildStar64 labels,
  reported `0` missing labels, and rendered a fresh `selected_decompiled.c`.
  Focused checks confirmed all 11 new labels appear in both `functions.csv` and
  `selected_decompiled.c`, covering the selected blocks around
  `selected_decompiled.c:16152`, `23715`, `39276..39999`, and
  `42867..43085`. `./Decomp/Analysis/Test-DecompileManifest.ps1
  -FailOnMismatch` reports `WildStar64.exe ok` with `selectedCount=620`.

Ninety-fourth mapped loot-table and group/master loot runtime pass:

- Data mapping implementation:
  `Tools\DataMapping\sql\apply_safe_world_imports_from_staging.sql` now
  materialises safe `creature_loot` rows into the older runtime loot-table
  shape. It creates/updates one flat `loot_group` per mapped Creature2 id using
  the owned id range starting at `100000000000`, links it through
  `entity_loot`, and inserts each mapped Item2 drop into `loot_item` as
  `LootItemType.StaticItem`. The default count policy remains conservative
  (`minCount = maxCount = 1`) to match the first live import; aggregate-derived
  counts are available behind
  `@nf_safe_import_creature_loot_counts_from_aggregates = 1`.
- Runtime loot loading implementation:
  `GlobalLootManager` now skips direct `creature_loot` rows for creatures that
  already have mapped `loot_group` rows, preventing duplicate drops when both
  representations are present. Mapped flat loot groups skip per-group child
  lookups, reducing the fresh WorldServer load from about `25547ms` to roughly
  `2.4..2.7s` for `3795` mapped creature groups plus the seeded Bag of
  OmniBits item loot table.
- Group manager implementation:
  added a world-side `IGroupStateManager` / `GroupStateManager` cache. Internal
  group messages now update that cache for joins, adds, removals, leaves,
  promotions, loot-rule changes, disbands, and player group-association
  updates. The cache resolves group members and maintains a per-group
  round-robin cursor for loot assignment.
- Group/master/roll loot implementation:
  creature loot generation now builds an eligible group-looter set from online
  same-map members within `35f` of the corpse. `LootInstanceItem` can now be
  configured as free-for-all, assigned/round-robin, master-loot-only, or
  need-before-greed roll loot. Roll items send `ServerLootRoll`, resolve after
  all eligible players roll or after the roll timer, send `ServerLootWinner`,
  and deliver to the winning online player. Master-loot items expose a
  `MasterList`, validate the master and assignee, send an assigned
  `ServerLootWinner`, and deliver through the existing grant path.
- Packet handlers implemented:
  `ClientLootRollActionHandler` and `ClientLootAssignMasterHandler` no longer
  only log diagnostics; they now validate the session player and delegate to
  `IGlobalLootManager.RollLoot(...)` and
  `IGlobalLootManager.AssignMasterLoot(...)`.
- Data verification:
  applying the safe SQL import in refresh mode on localhost produced
  `198735` `creature_loot` rows, `3795` mapped `loot_group` rows, `3795`
  mapped `entity_loot` rows, and `198735` mapped `loot_item` rows. The same
  verification run reported `95` `nf_map_*` staging tables, `17520` safe vendor
  rows, and `17612` safe mapped creature rows.
- Runtime verification:
  `dotnet build Source\NexusForever.WorldServer\NexusForever.WorldServer.csproj
  -p:UseSharedCompilation=false
  -p:BaseOutputPath=I:\GIT\NexusForever\.nexusforever-runtime\build\group-loot\
  -m:1 -v minimal --nologo` succeeds; the only warning is the pre-existing
  `Spline.formation` field warning. `.\Tools\Setup\Restart-NexusForeverAuthWorldLocal.ps1
  -ClientDirectory "I:\WildStar" -SkipClientLaunch` rebuilt and restarted the
  local Auth/World stack. The fresh WorldServer log shows loot loaded for
  `3795` old-table creatures, `1` item table, and `0` direct imported
  creatures in `2729ms`; it also registers `ClientLootRollActionHandler`,
  `ClientLootAssignMasterHandler`, and the `GroupDisbandedMessage`
  subscription. A fresh scan found no `ERROR`, `FATAL`, unhandled exception, or
  `System.` exception output.

Ninety-fifth retail group-loot semantics pass:

- Client evidence from the new labels:
  `Lua_GameLib_BuildLootRollEntry` builds roll/master-loot Lua entries with
  `nLootId`, optional `nTimeLeft`, `itemDrop`, `bIsMaster`, `tLooters`, and
  `tLootersOutOfRange`. `Lua_GameLib_GetMasterLoot` passes a looter identity
  list to that builder, and the client itself splits the list into in-range and
  out-of-range entries. `Lua_GameLib_RollOnLoot` / `PassOnLoot` confirm
  `LootRollAction` values `0=Need`, `1=Greed`, `2=Pass`.
- Group-rule evidence:
  `Lua_GroupLib_SetLootRules` sends the current group id plus the four rule
  fields exposed by `Lua_GroupLib_GetLootRules`: `eNormalRule`,
  `eThresholdRule`, `eThresholdQuality`, and `eHarvestRule`.
  `Group_HandleLootRulesChanged` updates the same four decoded fields and only
  dispatches `Group_LootRulesChanged` for the active group.
- Runtime fixes:
  `GroupStateManager.NextRoundRobinWinner` now advances through the full group
  slot order and stores the last served group index, so temporary eligible-set
  churn does not make the cursor rotate over a shrinking subset and then repeat
  a member when the full group becomes eligible again.
- Master-loot fixes:
  loot generation now keeps corpse-range eligibility separate from same-map
  master-loot candidates. The server still only permits in-range eligible
  assignees, but the `MasterList` sent to the client includes same-map group
  candidates so the retail UI path can populate `tLootersOutOfRange` itself.
- Roll-timer note:
  the client Lua table reports `nTimeLeft` as an expiry field minus its current
  time counter. No server change was made yet because the labeled functions do
  not prove whether `ServerLootNotify.RollTime` is decoded as a duration and
  converted to an expiry, or decoded as an absolute expiry. Current server
  behavior keeps sending the remaining duration, which is consistent with the
  common decoded-duration path and avoids inventing a client clock.
- Verification:
  `dotnet build Source\NexusForever.Game\NexusForever.Game.csproj --no-restore`
  succeeds with only the pre-existing `Spline.formation` unused-field warning.
  A full-solution build is still blocked by live local server processes locking
  their bin outputs. The fast restart script rebuilt and restarted Auth/World,
  and the fresh WorldServer log loaded `3795` old-table creature loot groups
  plus `1` item table in `2539ms`, registered the group/loot packet handlers,
  and had no post-restart `ERROR`, `FATAL`, unhandled exception, or exception
  log entries.

## Practical Next Steps

1. Keep extending `Decomp\Analysis\function_labels.csv` as functions are
   confirmed. Re-run `.\Decomp\Analysis\run_ghidra_analysis.ps1 -ExportOnly`
   so the exported C becomes progressively easier to read.
2. Cross-check constants and field reads in the selected C against
   `GameMessageOpcode.cs`, the STS models, and generated `GameTable` models.
3. Add narrower high-value patterns to `ExportNexusForeverAnalysis.java` when a
   new subsystem becomes the focus.
