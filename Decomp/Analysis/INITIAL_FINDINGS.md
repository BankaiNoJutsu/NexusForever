# Client64 Initial Reverse-Engineering Findings

Generated from the Ghidra headless project in `Decomp\Analysis\ghidra_projects`
and exports in `Decomp\Analysis\exports`.

## Export Coverage

| Binary | Functions | Strings | Interesting strings | Selected xrefs |
| --- | ---: | ---: | ---: | ---: |
| `WildStar64.exe` | 24,968 | 45,034 | 10,925 | 3,318 |
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
`exports\WildStar64.exe\selected_decompiled.c:7987`, including names such as
`PublicEventObjectiveType_Script`, `PublicEventObjectiveType_ParticipantsInTriggerVolume`,
and `PublicEventObjectiveType_KillEventObjectiveUnit`.

This is useful evidence for checking:

- `Source\NexusForever.Game.Static\PublicEvent\PublicEventObjectiveType.cs`
- `Source\NexusForever.Game\PublicEvent\*.cs`
- current expedition scripts that update public-event objectives

The selected export also captures `tSpell4IdAbility` at
`exports\WildStar64.exe\selected_decompiled.c:8582` and `tUnitProperty` at
`selected_decompiled.c:20991`, which are good anchors for spell effect and unit
property interpretation work.

Follow-up implemented from this pass:

- `PublicEventStatus_*` values in the Lua registration match
  `Inactive = 0`, `Active = 1`, `Succeeded = 2`, and `Failed = 3` around
  `selected_decompiled.c:8051`.
- `PublicEventObjectiveNotificationMode_*` values match `Normal = 0`,
  `LowTime = 1`, `Warn = 2`, `Serious = 3`, `Critical = 4`, and
  `Achieving = 5` around `selected_decompiled.c:8087`.
- `PublicEventObjectiveCategory_*` values match `Main = 0`, `Optional = 1`,
  `PlayerPath = 2`, and `Challenge = 3` around `selected_decompiled.c:8141`.
- The client exposes `PublicEventObjectiveType_DefendObjectiveUnits` at value
  `12` around `selected_decompiled.c:8285`; NexusForever keeps the existing
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
names from `function_labels.csv`; the latest run reported 91 applied labels and
0 missing labels.

`Lua_RegisterGameSpellBindings` starts around
`exports\WildStar64.exe\selected_decompiled.c:3576`. The registration walks the
indirect `Game.Spell` method table, then installs spell-facing Lua enums:

- `CodeEnumCastMethod` around `selected_decompiled.c:3734` confirms
  `Normal = 0`, `Channeled = 1`, `PressHold = 2`,
  `ChanneledField = 3`, `UNUSED04 = 4`, `ClientSideInteraction = 5`,
  `RapidTap = 6`, `ChargeRelease = 7`, `Multiphase = 8`, and
  `Transactional = 9`. The next value is still named through an undecoded data
  pointer, so NexusForever intentionally leaves it unmapped for now.
- `CodeEnumSchool` around `selected_decompiled.c:3846` confirms
  `Spell = 0`, `Melee = 1`, `Ranged = 2`, and `Unarmed = 3`.
- `CodeEnumAOESelectionType` around `selected_decompiled.c:3998` confirms the
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

`Lua_GameSpell_GetCastMethod` at `selected_decompiled.c:6040` and
`Lua_GameSpell_GetSchool` at `selected_decompiled.c:6079` both resolve the
`Game.Spell` userdata through the spell service and return numeric enum values.
`Lua_GameSpell_GetSpellServiceTokenCost` at `selected_decompiled.c:7944` checks
a spell metadata flag before returning a decoded 15-bit service-token value.

The LAS/action-set bridge is now labelled too. The decoded `ActionSetLib` table
at `140b75900` maps `IsSlotUnlocked` to `Lua_ActionSetLib_IsSlotUnlocked` at
`selected_decompiled.c:8607`, `RequestActionSetChanges` to
`Lua_ActionSetLib_RequestActionSetChanges` at `selected_decompiled.c:8652`, and
`GetCurrentActionSet` to `Lua_ActionSetLib_GetCurrentActionSet` at
`selected_decompiled.c:8847`.

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

## Practical Next Steps

1. Keep extending `Decomp\Analysis\function_labels.csv` as functions are
   confirmed. Re-run `.\Decomp\Analysis\run_ghidra_analysis.ps1 -ExportOnly`
   so the exported C becomes progressively easier to read.
2. Cross-check constants and field reads in the selected C against
   `GameMessageOpcode.cs`, the STS models, and generated `GameTable` models.
3. Add narrower high-value patterns to `ExportNexusForeverAnalysis.java` when a
   new subsystem becomes the focus.
